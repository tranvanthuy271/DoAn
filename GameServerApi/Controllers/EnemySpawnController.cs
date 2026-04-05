using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnemySpawnController : ControllerBase
    {
        private readonly GameDbContext _db;

        public EnemySpawnController(GameDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/enemyspawn/{mapId}/spawns
        /// Lấy danh sách enemy spawns cho map (kèm thông tin enemy chi tiết)
        /// </summary>
        [HttpGet("{mapId}/spawns")]
        public async Task<IActionResult> GetEnemySpawns(int mapId)
        {
            var mapSpawnConfig = await _db.MapSpawnConfigs
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.MapId == mapId);

            if (mapSpawnConfig != null)
            {
                object[] configuredSpawns = await BuildSpawnsFromMapSpawnConfigAsync(mapSpawnConfig.SpawnJson);
                if (configuredSpawns.Length > 0)
                {
                    return Ok(new
                    {
                        map_id = mapId,
                        enemy_spawns = configuredSpawns
                    });
                }
            }

            var enemySpawns = await _db.EnemySpawns
                .Where(e => e.MapId == mapId)
                .Include(e => e.Enemy) // Join với bảng enemy để lấy thông tin chi tiết
                .ToListAsync();

            if (enemySpawns != null && enemySpawns.Count > 0)
            {
                return Ok(new
                {
                    map_id = mapId,
                    enemy_spawns = enemySpawns.Select(e => new
                    {
                        spawn_id = e.SpawnId,
                        enemy_type_id = e.EnemyTypeId,
                        spawn_x = e.SpawnX,
                        spawn_y = e.SpawnY,
                        max_spawn_count = e.MaxSpawnCount,
                        respawn_time = e.RespawnTime,
                        override_hp = 0,
                        override_exp = 0,
                        is_boss = false,
                        level = 0,
                        enemy = CreateEnemyPayload(e.Enemy)
                    }).ToArray()
                });
            }

            return Ok(new
            {
                map_id = mapId,
                enemy_spawns = Array.Empty<object>()
            });
        }

        private async Task<object[]> BuildSpawnsFromMapSpawnConfigAsync(string spawnJson)
        {
            if (string.IsNullOrWhiteSpace(spawnJson))
                return Array.Empty<object>();

            List<SpawnConfigEntry>? entries;
            try
            {
                entries = JsonSerializer.Deserialize<List<SpawnConfigEntry>>(spawnJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                return Array.Empty<object>();
            }

            if (entries == null || entries.Count == 0)
                return Array.Empty<object>();

            int[] enemyIds = entries
                .Where(entry => entry.EnemyId > 0)
                .Select(entry => entry.EnemyId)
                .Distinct()
                .ToArray();

            var enemyLookup = await _db.Enemies
                .Where(enemy => enemyIds.Contains(enemy.EnemyId))
                .ToDictionaryAsync(enemy => enemy.EnemyId);

            return entries
                .Where(entry => entry.EnemyId > 0)
                .Select((entry, index) => (object)new
                {
                    spawn_id = index + 1,
                    enemy_type_id = entry.EnemyId,
                    spawn_x = entry.Cx,
                    spawn_y = entry.Cy,
                    max_spawn_count = entry.Count > 0 ? entry.Count : 1,
                    respawn_time = entry.RespawnTime > 0 ? entry.RespawnTime : 30,
                    override_hp = entry.Hp,
                    override_exp = entry.Exp,
                    is_boss = entry.IsBoss,
                    level = entry.Level,
                    enemy = enemyLookup.TryGetValue(entry.EnemyId, out var enemy)
                        ? CreateEnemyPayload(enemy)
                        : null
                })
                .ToArray();
        }

        private static object? CreateEnemyPayload(Enemy? enemy)
        {
            if (enemy == null)
                return null;

            return new
            {
                enemy_id = enemy.EnemyId,
                enemy_name = enemy.EnemyName,
                enemy_description = enemy.EnemyDescription,
                level = enemy.Level,
                base_hp = enemy.BaseHp,
                base_mp = enemy.BaseMp,
                base_damage = enemy.BaseDamage,
                base_defense = enemy.BaseDefense,
                move_speed = enemy.MoveSpeed,
                attack_speed = enemy.AttackSpeed,
                exp_reward = enemy.ExpReward,
                gold_reward = enemy.GoldReward,
                drop_items_json = enemy.DropItemsJson,
                element_type = enemy.ElementType,
                enemy_type = enemy.EnemyType
            };
        }

        private sealed class SpawnConfigEntry
        {
            [JsonPropertyName("enemy_id")]
            public int EnemyId { get; set; }

            [JsonPropertyName("hp")]
            public int Hp { get; set; }

            [JsonPropertyName("exp")]
            public int Exp { get; set; }

            [JsonPropertyName("cx")]
            public float Cx { get; set; }

            [JsonPropertyName("cy")]
            public float Cy { get; set; }

            [JsonPropertyName("is_boss")]
            public bool IsBoss { get; set; }

            [JsonPropertyName("count")]
            public int Count { get; set; }

            [JsonPropertyName("respawn_time")]
            public int RespawnTime { get; set; }

            [JsonPropertyName("level")]
            public int Level { get; set; }
        }
    }
}
