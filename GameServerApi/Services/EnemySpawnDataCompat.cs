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

    public static Task<IReadOnlyList<ResolvedEnemySpawn>> LoadResolvedSpawnsAsync(
        GameDbContext db,
        int mapId,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        return LoadResolvedSpawnsCoreAsync(
            db,
            mapId,
            logger,
            preferLegacyMapSpawnConfig: false,
            cancellationToken);
    }

    public static Task<IReadOnlyList<ResolvedEnemySpawn>> LoadResolvedSpawnsPreferLegacyAsync(
        GameDbContext db,
        int mapId,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        return LoadResolvedSpawnsCoreAsync(
            db,
            mapId,
            logger,
            preferLegacyMapSpawnConfig: true,
            cancellationToken);
    }

    private static async Task<IReadOnlyList<ResolvedEnemySpawn>> LoadResolvedSpawnsCoreAsync(
        GameDbContext db,
        int mapId,
        ILogger? logger,
        bool preferLegacyMapSpawnConfig,
        CancellationToken cancellationToken)
    {
        if (preferLegacyMapSpawnConfig)
        {
            IReadOnlyList<ResolvedEnemySpawn> legacySpawns = await LoadLegacySpawnsAsync(db, mapId, logger, cancellationToken);
            if (legacySpawns.Count > 0)
            {
                logger?.LogInformation(
                    "[EnemySpawnDataCompat] mapId={MapId} is using map_spawn_config as the preferred source.",
                    mapId);
                return legacySpawns;
            }

            logger?.LogInformation(
                "[EnemySpawnDataCompat] mapId={MapId} has no valid map_spawn_config entries. Falling back to enemy_spawns.",
                mapId);
        }

        return await LoadEnemySpawnsOrFallbackAsync(db, mapId, logger, cancellationToken);
    }

    private static async Task<IReadOnlyList<ResolvedEnemySpawn>> LoadEnemySpawnsOrFallbackAsync(
        GameDbContext db,
        int mapId,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        if (!await TableExistsAsync(db, "enemy_spawns", cancellationToken))
        {
            logger?.LogInformation(
                "[EnemySpawnDataCompat] enemy_spawns table missing, using map_spawn_config for mapId={MapId}.",
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
                return NormalizeBossEnemyTypeMismatch(rows.Select(MapResolvedSpawn).ToArray(), mapId, logger);

            logger?.LogInformation(
                "[EnemySpawnDataCompat] enemy_spawns has no rows for mapId={MapId}. Falling back to map_spawn_config.",
                mapId);
        }
        catch (MySqlException ex) when (IsMissingTable(ex, "enemy_spawns"))
        {
            logger?.LogWarning(
                ex,
                "[EnemySpawnDataCompat] enemy_spawns table missing at runtime, using map_spawn_config for mapId={MapId}.",
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
            RespawnTime = Math.Max(0, row.RespawnTime),
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

        List<LegacySpawnEntry> legacyEntries = ParseLegacySpawns(spawnJson);
        if (legacyEntries.Count == 0)
        {
            logger?.LogWarning(
                "[EnemySpawnDataCompat] map_spawn_config.map_id={MapId} has spawn_json but it could not be parsed. Supported formats: [...] or {{\"spawns\":[...]}} with cx/cy or x/y.",
                mapId);
            return Array.Empty<ResolvedEnemySpawn>();
        }

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
                RespawnTime = Math.Max(0, entry.RespawnTime),
                OverrideHp = Math.Max(0, entry.Hp),
                OverrideExp = Math.Max(0, entry.Exp),
                IsBoss = entry.IsBoss,
                Level = entry.Level > 0 ? entry.Level : enemy?.Level ?? 1,
                Enemy = enemy
            });
        }

        logger?.LogInformation(
            "[EnemySpawnDataCompat] Loaded {Count} legacy spawn entries from map_spawn_config for mapId={MapId}.",
            result.Count,
            mapId);

        return NormalizeBossEnemyTypeMismatch(result, mapId, logger);
    }

    private static IReadOnlyList<ResolvedEnemySpawn> NormalizeBossEnemyTypeMismatch(
        IReadOnlyList<ResolvedEnemySpawn> spawns,
        int mapId,
        ILogger? logger)
    {
        if (spawns.Count == 0)
            return spawns;

        var bossSpawns = spawns.Where(spawn => spawn.IsBoss).ToArray();
        var normalSpawns = spawns.Where(spawn => !spawn.IsBoss).ToArray();

        if (bossSpawns.Length == 0 || normalSpawns.Length == 0)
            return spawns;

        bool normalsUseBossTemplate = normalSpawns.All(spawn =>
            string.Equals(spawn.Enemy?.EnemyType, "Boss", StringComparison.OrdinalIgnoreCase));
        bool bossUsesNormalTemplate = bossSpawns.All(spawn =>
            !string.Equals(spawn.Enemy?.EnemyType, "Boss", StringComparison.OrdinalIgnoreCase));

        if (!normalsUseBossTemplate || !bossUsesNormalTemplate)
            return spawns;

        int[] normalEnemyIds = normalSpawns.Select(spawn => spawn.EnemyTypeId).Distinct().ToArray();
        int[] bossEnemyIds = bossSpawns.Select(spawn => spawn.EnemyTypeId).Distinct().ToArray();

        if (normalEnemyIds.Length != 1 || bossEnemyIds.Length != 1 || normalEnemyIds[0] == bossEnemyIds[0])
            return spawns;

        Enemy? correctedNormalEnemy = bossSpawns[0].Enemy;
        Enemy? correctedBossEnemy = normalSpawns[0].Enemy;
        int correctedNormalEnemyId = bossEnemyIds[0];
        int correctedBossEnemyId = normalEnemyIds[0];

        logger?.LogWarning(
            "[EnemySpawnDataCompat] mapId={MapId} appears to have normal/boss enemy IDs swapped. Auto-correcting normalEnemyId={NormalEnemyId}, bossEnemyId={BossEnemyId}.",
            mapId,
            correctedNormalEnemyId,
            correctedBossEnemyId);

        return spawns
            .Select(spawn => spawn.IsBoss
                ? CloneResolvedSpawn(spawn, correctedBossEnemyId, correctedBossEnemy)
                : CloneResolvedSpawn(spawn, correctedNormalEnemyId, correctedNormalEnemy))
            .ToArray();
    }

    private static ResolvedEnemySpawn CloneResolvedSpawn(ResolvedEnemySpawn spawn, int enemyTypeId, Enemy? enemy)
    {
        return new ResolvedEnemySpawn
        {
            SpawnId = spawn.SpawnId,
            EnemyTypeId = enemyTypeId,
            SpawnX = spawn.SpawnX,
            SpawnY = spawn.SpawnY,
            MaxSpawnCount = spawn.MaxSpawnCount,
            RespawnTime = spawn.RespawnTime,
            OverrideHp = spawn.OverrideHp,
            OverrideExp = spawn.OverrideExp,
            IsBoss = spawn.IsBoss,
            Level = spawn.Level,
            Enemy = enemy
        };
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
                "[EnemySpawnDataCompat] map_spawn_config table is missing while loading mapId={MapId}.",
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
                "[EnemySpawnDataCompat] map_spawn_config table is missing while loading mapId={MapId}.",
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
            if (!TryGetSpawnEntriesElement(document.RootElement, out JsonElement spawnEntries))
                return new List<LegacySpawnEntry>();

            var result = new List<LegacySpawnEntry>();
            foreach (JsonElement element in spawnEntries.EnumerateArray())
            {
                int enemyId = GetIntValueByAliasesOrDefault(element, 0, "enemy_id");
                if (enemyId <= 0)
                    continue;

                result.Add(new LegacySpawnEntry
                {
                    EnemyId = enemyId,
                    Hp = GetIntValueByAliasesOrDefault(element, 0, "hp", "max_hp", "base_hp"),
                    Exp = GetIntValueByAliasesOrDefault(element, 0, "exp", "exp_reward"),
                    Cx = GetFloatValueByAliasesOrDefault(element, 0f, "cx", "x", "spawn_x"),
                    Cy = GetFloatValueByAliasesOrDefault(element, 0f, "cy", "y", "spawn_y"),
                    IsBoss = GetBoolValueByAliasesOrDefault(element, false, "is_boss", "isBoss"),
                    Count = GetIntValueByAliasesOrDefault(element, 1, "count", "max_spawn_count"),
                    RespawnTime = GetIntValueByAliasesOrDefault(element, 30, "respawn_time"),
                    Level = GetIntValueByAliasesOrDefault(element, 1, "level")
                });
            }

            return result;
        }
        catch (JsonException)
        {
            return new List<LegacySpawnEntry>();
        }
    }

    private static bool TryGetSpawnEntriesElement(JsonElement rootElement, out JsonElement spawnEntries)
    {
        if (rootElement.ValueKind == JsonValueKind.Array)
        {
            spawnEntries = rootElement;
            return true;
        }

        if (rootElement.ValueKind == JsonValueKind.Object)
        {
            if (TryGetPropertyValue(rootElement, "spawns", out JsonElement wrappedSpawns)
                && wrappedSpawns.ValueKind == JsonValueKind.Array)
            {
                spawnEntries = wrappedSpawns;
                return true;
            }

            if (TryGetPropertyValue(rootElement, "enemy_spawns", out JsonElement wrappedEnemySpawns)
                && wrappedEnemySpawns.ValueKind == JsonValueKind.Array)
            {
                spawnEntries = wrappedEnemySpawns;
                return true;
            }
        }

        spawnEntries = default;
        return false;
    }

    private static bool IsMissingTable(MySqlException exception, string tableName)
    {
        return exception.Number == 1146
            && exception.Message.Contains(tableName, StringComparison.OrdinalIgnoreCase);
    }

    private static int GetIntValueOrDefault(JsonElement element, string propertyName, int defaultValue)
    {
        if (!TryGetPropertyValue(element, propertyName, out JsonElement property))
            return defaultValue;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetInt32(out int numberValue) => numberValue,
            JsonValueKind.String when int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stringValue) => stringValue,
            _ => defaultValue
        };
    }

    private static float GetFloatValueOrDefault(JsonElement element, string propertyName, float defaultValue)
    {
        if (!TryGetPropertyValue(element, propertyName, out JsonElement property))
            return defaultValue;

        return property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetSingle(out float numberValue) => numberValue,
            JsonValueKind.String when float.TryParse(property.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float stringValue) => stringValue,
            _ => defaultValue
        };
    }

    private static bool GetBoolValueOrDefault(JsonElement element, string propertyName, bool defaultValue)
    {
        if (!TryGetPropertyValue(element, propertyName, out JsonElement property))
            return defaultValue;

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

    private static int GetIntValueByAliasesOrDefault(JsonElement element, int defaultValue, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            int value = GetIntValueOrDefault(element, propertyName, int.MinValue);
            if (value != int.MinValue)
                return value;
        }

        return defaultValue;
    }

    private static float GetFloatValueByAliasesOrDefault(JsonElement element, float defaultValue, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            float value = GetFloatValueOrDefault(element, propertyName, float.NaN);
            if (!float.IsNaN(value))
                return value;
        }

        return defaultValue;
    }

    private static bool GetBoolValueByAliasesOrDefault(JsonElement element, bool defaultValue, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            if (TryGetPropertyValue(element, propertyName, out _))
                return GetBoolValueOrDefault(element, propertyName, defaultValue);
        }

        return defaultValue;
    }

    private static bool TryGetPropertyValue(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }
}
