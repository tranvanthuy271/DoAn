using System.Collections.Concurrent;
using System.Data;
using System.Globalization;
using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace GameServerApi.Services;

internal static class EnemySpawnDataCompat
{
    private static readonly ConcurrentDictionary<string, bool> TableExistenceCache = new(StringComparer.OrdinalIgnoreCase);

    internal sealed class ResolvedEnemySpawn
    {
        public int SpawnId { get; init; }
        public int EnemyTypeId { get; init; }
        public float SpawnX { get; init; }
        public float SpawnY { get; init; }
        public int MaxSpawnCount { get; init; }
        public int RespawnTime { get; init; }
        public int OverrideHp { get; init; }
        public int OverrideExp { get; init; }
        public bool IsBoss { get; init; }
        public int Level { get; init; }
        public Enemy? Enemy { get; init; }
    }

    private sealed class LegacySpawnEntry
    {
        public int EnemyId { get; init; }
        public int Hp { get; init; }
        public int Exp { get; init; }
        public float Cx { get; init; }
        public float Cy { get; init; }
        public bool IsBoss { get; init; }
        public int Count { get; init; }
        public int RespawnTime { get; init; }
        public int Level { get; init; }
    }

    public static async Task<IReadOnlyList<ResolvedEnemySpawn>> LoadResolvedSpawnsAsync(
        GameDbContext db,
        int mapId,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        if (!await TableExistsAsync(db, "enemy_spawns", cancellationToken))
        {
            logger?.LogInformation(
                "[EnemySpawnDataCompat] Không có bảng enemy_spawns, fallback map_spawn_config cho mapId={MapId}.",
                mapId);
            return await LoadLegacySpawnsAsync(db, mapId, logger, cancellationToken);
        }

        try
        {
            var rows = await db.EnemySpawns
                .AsNoTracking()
                .Where(e => e.MapId == mapId)
                .Include(e => e.Enemy)
                .OrderBy(e => e.SpawnId)
                .ToListAsync(cancellationToken);

            if (rows.Count > 0)
                return rows.Select(MapResolvedSpawn).ToArray();

            logger?.LogInformation(
                "[EnemySpawnDataCompat] enemy_spawns không có dữ liệu cho mapId={MapId}, thử fallback map_spawn_config.",
                mapId);
        }
        catch (MySqlException ex) when (IsMissingTable(ex, "enemy_spawns"))
        {
            logger?.LogWarning(
                ex,
                "[EnemySpawnDataCompat] Thiếu bảng enemy_spawns, fallback sang map_spawn_config cho mapId={MapId}.",
                mapId);
        }

        return await LoadLegacySpawnsAsync(db, mapId, logger, cancellationToken);
    }

    private static ResolvedEnemySpawn MapResolvedSpawn(EnemySpawn row)
    {
        return new ResolvedEnemySpawn
        {
            SpawnId = row.SpawnId,
            EnemyTypeId = row.EnemyTypeId,
            SpawnX = row.SpawnX,
            SpawnY = row.SpawnY,
            MaxSpawnCount = row.MaxSpawnCount > 0 ? row.MaxSpawnCount : 1,
            RespawnTime = row.RespawnTime > 0 ? row.RespawnTime : 30,
            OverrideHp = 0,
            OverrideExp = 0,
            IsBoss = string.Equals(row.Enemy?.EnemyType, "Boss", StringComparison.OrdinalIgnoreCase),
            Level = row.Enemy?.Level ?? 1,
            Enemy = row.Enemy
        };
    }

    private static async Task<IReadOnlyList<ResolvedEnemySpawn>> LoadLegacySpawnsAsync(
        GameDbContext db,
        int mapId,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        string? spawnJson = await LoadLegacySpawnJsonAsync(db, mapId, logger, cancellationToken);
        if (string.IsNullOrWhiteSpace(spawnJson))
            return Array.Empty<ResolvedEnemySpawn>();

        var legacyEntries = ParseLegacySpawns(spawnJson);
        if (legacyEntries.Count == 0)
            return Array.Empty<ResolvedEnemySpawn>();

        int[] enemyIds = legacyEntries
            .Select(entry => entry.EnemyId)
            .Where(enemyId => enemyId > 0)
            .Distinct()
            .ToArray();

        var enemyLookup = await db.Enemies
            .AsNoTracking()
            .Where(enemy => enemyIds.Contains(enemy.EnemyId))
            .ToDictionaryAsync(enemy => enemy.EnemyId, cancellationToken);

        var result = new List<ResolvedEnemySpawn>(legacyEntries.Count);
        for (int index = 0; index < legacyEntries.Count; index++)
        {
            LegacySpawnEntry entry = legacyEntries[index];
            enemyLookup.TryGetValue(entry.EnemyId, out Enemy? enemy);

            result.Add(new ResolvedEnemySpawn
            {
                SpawnId = -(((mapId + 1) * 100000) + index + 1),
                EnemyTypeId = entry.EnemyId,
                SpawnX = entry.Cx,
                SpawnY = entry.Cy,
                MaxSpawnCount = entry.Count > 0 ? entry.Count : 1,
                RespawnTime = entry.RespawnTime > 0 ? entry.RespawnTime : 30,
                OverrideHp = Math.Max(0, entry.Hp),
                OverrideExp = Math.Max(0, entry.Exp),
                IsBoss = entry.IsBoss || string.Equals(enemy?.EnemyType, "Boss", StringComparison.OrdinalIgnoreCase),
                Level = entry.Level > 0 ? entry.Level : enemy?.Level ?? 1,
                Enemy = enemy
            });
        }

        logger?.LogInformation(
            "[EnemySpawnDataCompat] Đã nạp {Count} spawn legacy từ map_spawn_config cho mapId={MapId}.",
            result.Count,
            mapId);

        return result;
    }

    private static async Task<string?> LoadLegacySpawnJsonAsync(
        GameDbContext db,
        int mapId,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(db, "map_spawn_config", cancellationToken))
        {
            logger?.LogWarning(
                "[EnemySpawnDataCompat] Thiếu cả bảng map_spawn_config khi nạp mapId={MapId}.",
                mapId);
            return null;
        }

        var connection = db.Database.GetDbConnection();
        bool shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT spawn_json
FROM map_spawn_config
WHERE map_id = @mapId
ORDER BY updated_at DESC
LIMIT 1";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@mapId";
            parameter.DbType = DbType.Int32;
            parameter.Value = mapId;
            command.Parameters.Add(parameter);

            object? value = await command.ExecuteScalarAsync(cancellationToken);
            if (value == null || value == DBNull.Value)
                return null;

            return Convert.ToString(value);
        }
        catch (MySqlException ex) when (IsMissingTable(ex, "map_spawn_config"))
        {
            logger?.LogWarning(
                ex,
                "[EnemySpawnDataCompat] Thiếu cả bảng map_spawn_config khi nạp mapId={MapId}.",
                mapId);
            return null;
        }
        finally
        {
            if (shouldCloseConnection)
                await connection.CloseAsync();
        }
    }

    private static async Task<bool> TableExistsAsync(
        GameDbContext db,
        string tableName,
        CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        string cacheKey = $"{connection.Database}:{tableName}";

        if (TableExistenceCache.TryGetValue(cacheKey, out bool exists))
            return exists;

        bool shouldCloseConnection = connection.State != ConnectionState.Open;
        if (shouldCloseConnection)
            await connection.OpenAsync(cancellationToken);

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = @"
SELECT 1
FROM information_schema.tables
WHERE table_schema = @schemaName
  AND table_name = @tableName
LIMIT 1";

            var schemaParameter = command.CreateParameter();
            schemaParameter.ParameterName = "@schemaName";
            schemaParameter.DbType = DbType.String;
            schemaParameter.Value = connection.Database;
            command.Parameters.Add(schemaParameter);

            var tableParameter = command.CreateParameter();
            tableParameter.ParameterName = "@tableName";
            tableParameter.DbType = DbType.String;
            tableParameter.Value = tableName;
            command.Parameters.Add(tableParameter);

            object? value = await command.ExecuteScalarAsync(cancellationToken);
            exists = value != null && value != DBNull.Value;
            TableExistenceCache[cacheKey] = exists;
            return exists;
        }
        finally
        {
            if (shouldCloseConnection)
                await connection.CloseAsync();
        }
    }

    private static List<LegacySpawnEntry> ParseLegacySpawns(string spawnJson)
    {
        try
        {
            using var document = JsonDocument.Parse(spawnJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return new List<LegacySpawnEntry>();

            var result = new List<LegacySpawnEntry>();
            foreach (JsonElement element in document.RootElement.EnumerateArray())
            {
                int enemyId = GetIntValueOrDefault(element, "enemy_id", 0);
                if (enemyId <= 0)
                    continue;

                result.Add(new LegacySpawnEntry
                {
                    EnemyId = enemyId,
                    Hp = GetIntValueOrDefault(element, "hp", 0),
                    Exp = GetIntValueOrDefault(element, "exp", 0),
                    Cx = GetFloatValueOrDefault(element, "cx", 0f),
                    Cy = GetFloatValueOrDefault(element, "cy", 0f),
                    IsBoss = GetBoolValueOrDefault(element, "is_boss", false),
                    Count = GetIntValueOrDefault(element, "count", 1),
                    RespawnTime = GetIntValueOrDefault(element, "respawn_time", 30),
                    Level = GetIntValueOrDefault(element, "level", 1)
                });
            }

            return result;
        }
        catch (JsonException)
        {
            return new List<LegacySpawnEntry>();
        }
    }

    private static bool IsMissingTable(MySqlException exception, string tableName)
    {
        return exception.Number == 1146
            && exception.Message.Contains(tableName, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetIntValueOrDefault(JsonElement element, string propertyName, int defaultValue)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property))
        {
            return defaultValue;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out int numberValue) => numberValue,
            JsonValueKind.String when int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stringValue) => stringValue,
            _ => defaultValue
        };
    }

    private static float GetFloatValueOrDefault(JsonElement element, string propertyName, float defaultValue)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property))
        {
            return defaultValue;
        }

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetSingle(out float numberValue) => numberValue,
            JsonValueKind.String when float.TryParse(property.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float stringValue) => stringValue,
            _ => defaultValue
        };
    }

    private static bool GetBoolValueOrDefault(JsonElement element, string propertyName, bool defaultValue)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(propertyName, out JsonElement property))
        {
            return defaultValue;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number when property.TryGetInt32(out int numberValue) => numberValue != 0,
            JsonValueKind.String when bool.TryParse(property.GetString(), out bool boolValue) => boolValue,
            JsonValueKind.String when int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stringNumberValue) => stringNumberValue != 0,
            _ => defaultValue
        };
    }
}