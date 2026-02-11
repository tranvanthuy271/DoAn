using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var enemySpawns = await _db.EnemySpawns
                .Where(e => e.MapId == mapId)
                .Include(e => e.Enemy) // Join với bảng enemy để lấy thông tin chi tiết
                .ToListAsync();

            if (enemySpawns == null || enemySpawns.Count == 0)
            {
                // Trả về empty array nếu không có enemy spawns
                return Ok(new
                {
                    map_id = mapId,
                    enemy_spawns = new object[0]
                });
            }

            var spawns = enemySpawns.Select(e => new
            {
                spawn_id = e.SpawnId,
                enemy_type_id = e.EnemyTypeId,
                spawn_x = e.SpawnX,
                spawn_y = e.SpawnY,
                max_spawn_count = e.MaxSpawnCount,
                respawn_time = e.RespawnTime,
                // Thông tin enemy chi tiết
                enemy = e.Enemy != null ? new
                {
                    enemy_id = e.Enemy.EnemyId,
                    enemy_name = e.Enemy.EnemyName,
                    enemy_description = e.Enemy.EnemyDescription,
                    level = e.Enemy.Level,
                    base_hp = e.Enemy.BaseHp,
                    base_mp = e.Enemy.BaseMp,
                    base_damage = e.Enemy.BaseDamage,
                    base_defense = e.Enemy.BaseDefense,
                    move_speed = e.Enemy.MoveSpeed,
                    attack_speed = e.Enemy.AttackSpeed,
                    exp_reward = e.Enemy.ExpReward,
                    gold_reward = e.Enemy.GoldReward,
                    drop_items_json = e.Enemy.DropItemsJson,
                    element_type = e.Enemy.ElementType,
                    enemy_type = e.Enemy.EnemyType
                } : null
            }).ToArray();

            return Ok(new
            {
                map_id = mapId,
                enemy_spawns = spawns
            });
        }
    }
}
