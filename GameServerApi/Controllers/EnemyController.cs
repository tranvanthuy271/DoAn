using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EnemyController : ControllerBase
    {
        private readonly GameDbContext _db;

        public EnemyController(GameDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// GET /api/enemy
        /// Lấy danh sách tất cả enemy
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllEnemies()
        {
            var enemies = await _db.Enemies.ToListAsync();

            var result = enemies.Select(e => new
            {
                enemy_id = e.EnemyId,
                enemy_name = e.EnemyName,
                enemy_description = e.EnemyDescription,
                level = e.Level,
                base_hp = e.BaseHp,
                base_mp = e.BaseMp,
                base_damage = e.BaseDamage,
                base_defense = e.BaseDefense,
                move_speed = e.MoveSpeed,
                attack_speed = e.AttackSpeed,
                exp_reward = e.ExpReward,
                gold_reward = e.GoldReward,
                drop_items_json = e.DropItemsJson,
                element_type = e.ElementType,
                enemy_type = e.EnemyType
            }).ToArray();

            return Ok(new
            {
                enemies = result
            });
        }

        /// <summary>
        /// GET /api/enemy/{enemyId}
        /// Lấy thông tin chi tiết của một enemy
        /// </summary>
        [HttpGet("{enemyId}")]
        public async Task<IActionResult> GetEnemy(int enemyId)
        {
            var enemy = await _db.Enemies.FindAsync(enemyId);
            
            if (enemy == null)
            {
                return NotFound("Enemy không tồn tại.");
            }

            return Ok(new
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
            });
        }

        /// <summary>
        /// GET /api/enemy/by-level/{level}
        /// Lấy danh sách enemy theo level
        /// </summary>
        [HttpGet("by-level/{level}")]
        public async Task<IActionResult> GetEnemiesByLevel(int level)
        {
            var enemies = await _db.Enemies
                .Where(e => e.Level == level)
                .ToListAsync();

            var result = enemies.Select(e => new
            {
                enemy_id = e.EnemyId,
                enemy_name = e.EnemyName,
                enemy_description = e.EnemyDescription,
                level = e.Level,
                base_hp = e.BaseHp,
                base_mp = e.BaseMp,
                base_damage = e.BaseDamage,
                base_defense = e.BaseDefense,
                move_speed = e.MoveSpeed,
                attack_speed = e.AttackSpeed,
                exp_reward = e.ExpReward,
                gold_reward = e.GoldReward,
                drop_items_json = e.DropItemsJson,
                element_type = e.ElementType,
                enemy_type = e.EnemyType
            }).ToArray();

            return Ok(new
            {
                level = level,
                enemies = result
            });
        }
    }
}
