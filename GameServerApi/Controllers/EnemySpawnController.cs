using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnemySpawnController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly ILogger<EnemySpawnController> _logger;

        public EnemySpawnController(GameDbContext db, ILogger<EnemySpawnController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /api/enemyspawn/{mapId}/spawns
        // Lấy danh sách enemy spawns cho map (kèm thông tin enemy chi tiết)
        [HttpGet("{mapId}/spawns")]
        public async Task<IActionResult> GetEnemySpawns(int mapId)
        {
            var enemySpawns = await EnemySpawnDataCompat.LoadResolvedSpawnsAsync(
                _db,
                mapId,
                _logger,
                HttpContext.RequestAborted);

            if (enemySpawns.Count > 0)
            {
                return Ok(new
                {
                    map_id = mapId,
                    enemy_spawns = enemySpawns.Select(spawn => new
                    {
                        spawn_id = spawn.SpawnId,
                        enemy_type_id = spawn.EnemyTypeId,
                        spawn_x = spawn.SpawnX,
                        spawn_y = spawn.SpawnY,
                        max_spawn_count = spawn.MaxSpawnCount,
                        respawn_time = spawn.RespawnTime,
                        override_hp = spawn.OverrideHp,
                        override_exp = spawn.OverrideExp,
                        is_boss = spawn.IsBoss,
                        level = spawn.Level,
                        enemy = CreateEnemyPayload(spawn.Enemy)
                    }).ToArray()
                });
            }

            return Ok(new
            {
                map_id = mapId,
                enemy_spawns = Array.Empty<object>()
            });
        }

        private static object? CreateEnemyPayload(Enemy? enemy)
        {
            if (enemy == null)
                return null;

            var drops = new List<object>();

            if (!string.IsNullOrWhiteSpace(enemy.DropItemsJson))
            {
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(enemy.DropItemsJson);
                    if (doc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        foreach (var drop in doc.RootElement.EnumerateArray())
                        {
                            int itemId = GetIntValueOrDefault(drop, "item_id", 0);
                            double rate = GetDoubleValueOrDefault(drop, "drop_chance", 0d);
                            int qtyMin = GetIntValueOrDefault(drop, "qty_min", 1);
                            int qtyMax = GetIntValueOrDefault(drop, "qty_max", qtyMin);
                            if (itemId > 0)
                                drops.Add(new { item_id = itemId, rate, qty_min = qtyMin, qty_max = qtyMax });
                        }
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                }
            }

            return new
            {
                enemy_id      = enemy.EnemyId,
                enemy_name    = enemy.EnemyName,
                enemy_description = enemy.EnemyDescription,
                level         = enemy.Level,
                base_hp       = enemy.BaseHp,
                base_mp       = enemy.BaseMp,
                base_damage   = enemy.BaseDamage,
                base_defense  = enemy.BaseDefense,
                move_speed    = enemy.MoveSpeed,
                attack_speed  = enemy.AttackSpeed,
                exp_reward    = enemy.ExpReward,
                gold_reward   = enemy.GoldReward,
                silver_reward = enemy.SilverReward,
                drops         = drops,
                element_type  = enemy.ElementType,
                enemy_type    = enemy.EnemyType
            };
        }

        private static int GetIntValueOrDefault(System.Text.Json.JsonElement element, string propertyName, int defaultValue)
        {
            if (element.ValueKind != System.Text.Json.JsonValueKind.Object
                || !element.TryGetProperty(propertyName, out var property))
            {
                return defaultValue;
            }

            return property.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number when property.TryGetInt32(out int numberValue) => numberValue,
                System.Text.Json.JsonValueKind.String when int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int stringValue) => stringValue,
                _ => defaultValue
            };
        }

        private static double GetDoubleValueOrDefault(System.Text.Json.JsonElement element, string propertyName, double defaultValue)
        {
            if (element.ValueKind != System.Text.Json.JsonValueKind.Object
                || !element.TryGetProperty(propertyName, out var property))
            {
                return defaultValue;
            }

            return property.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number when property.TryGetDouble(out double numberValue) => numberValue,
                System.Text.Json.JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double stringValue) => stringValue,
                _ => defaultValue
            };
        }
    }
}
