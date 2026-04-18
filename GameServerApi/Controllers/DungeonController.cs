using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.DTOs;
using GameServerApi.Models.Entities;
using GameServerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DungeonController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly ILogger<DungeonController> _logger;

        public DungeonController(GameDbContext db, ILogger<DungeonController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────
        //  CONFIG ENDPOINTS
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/dungeon/list
        /// Lấy danh sách tất cả phó bản đang active, sắp xếp theo level yêu cầu.
        /// Client dùng để render danh sách nút bấm vào phó bản.
        /// </summary>
        [HttpGet("list")]
        public async Task<IActionResult> GetDungeonList()
        {
            var dungeons = await _db.DungeonConfigs
                .Include(d => d.Map)
                .Where(d => d.IsActive)
                .OrderBy(d => d.MinLevelRequired)
                .ThenBy(d => d.DungeonId)
                .ToListAsync();

            return Ok(new
            {
                dungeons = dungeons.Select(d => new
                {
                    dungeon_id          = d.DungeonId,
                    dungeon_name        = d.DungeonName,
                    dungeon_type        = d.DungeonType,      // "solo" | "multi"
                    map_id              = d.MapId,
                    map_name            = d.Map?.MapName ?? "",
                    scene_name          = d.SceneName,
                    max_players         = d.MaxPlayers,
                    min_level_required  = d.MinLevelRequired,
                    time_limit_seconds  = d.TimeLimitSeconds,
                    description         = d.Description,
                    thumbnail_icon_id   = d.ThumbnailIconId,
                    boss_enemy_id       = d.BossEnemyId,
                    reward_json         = d.RewardJson
                })
            });
        }

        /// <summary>
        /// GET /api/dungeon/{dungeonId}
        /// Lấy chi tiết một phó bản (kèm danh sách enemy spawns thuộc map đó).
        /// </summary>
        [HttpGet("{dungeonId:int}")]
        public async Task<IActionResult> GetDungeonDetail(int dungeonId)
        {
            var d = await _db.DungeonConfigs
                .Include(x => x.Map)
                .Include(x => x.BossEnemy)
                .FirstOrDefaultAsync(x => x.DungeonId == dungeonId);

            if (d == null) return NotFound(new { message = "Dungeon không tồn tại." });

            // Lấy enemy spawns của map này
            var spawns = await EnemySpawnDataCompat.LoadResolvedSpawnsAsync(
                _db,
                d.MapId,
                _logger,
                HttpContext.RequestAborted);

            return Ok(new
            {
                dungeon_id          = d.DungeonId,
                dungeon_name        = d.DungeonName,
                dungeon_type        = d.DungeonType,
                map_id              = d.MapId,
                map_name            = d.Map?.MapName ?? "",
                scene_name          = d.SceneName,
                max_players         = d.MaxPlayers,
                min_level_required  = d.MinLevelRequired,
                time_limit_seconds  = d.TimeLimitSeconds,
                description         = d.Description,
                thumbnail_icon_id   = d.ThumbnailIconId,
                reward_json         = d.RewardJson,
                boss_enemy = d.BossEnemy == null ? null : new
                {
                    enemy_id   = d.BossEnemy.EnemyId,
                    enemy_name = d.BossEnemy.EnemyName,
                    level      = d.BossEnemy.Level,
                    base_hp    = d.BossEnemy.BaseHp
                },
                // Vị trí spawn cầu thủ khi vào phó bản
                player_spawn_points = d.Map?.SpawnPointsJson,
                // Danh sách quái cùng map_id (dùng để enemy_spawner trên host init)
                enemy_spawns = spawns.Select(spawn => new
                {
                    spawn_id        = spawn.SpawnId,
                    enemy_type_id   = spawn.EnemyTypeId,
                    spawn_x         = spawn.SpawnX,
                    spawn_y         = spawn.SpawnY,
                    max_spawn_count = spawn.MaxSpawnCount,
                    respawn_time    = spawn.RespawnTime,
                    enemy = spawn.Enemy == null ? null : new
                    {
                        enemy_id    = spawn.Enemy.EnemyId,
                        enemy_name  = spawn.Enemy.EnemyName,
                        level       = spawn.Enemy.Level,
                        base_hp     = spawn.Enemy.BaseHp,
                        base_damage = spawn.Enemy.BaseDamage,
                        base_defense= spawn.Enemy.BaseDefense,
                        exp_reward  = spawn.Enemy.ExpReward,
                        gold_reward = spawn.Enemy.GoldReward,
                        silver_reward = spawn.Enemy.SilverReward,
                        drop_items_json = spawn.Enemy.DropItemsJson,
                        element_type= spawn.Enemy.ElementType,
                        enemy_type  = spawn.Enemy.EnemyType
                    }
                })
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  SESSION ENDPOINTS (chỉ dùng cho phó bản "multi")
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/dungeon/session/active/{dungeonConfigId}
        /// Lấy session đang chờ/active cho phó bản multi.
        /// Client dùng để check trước khi vào: còn chỗ không? Host ở đâu?
        /// Trả về has_session=false nếu chưa có ai tạo session.
        /// </summary>
        [HttpGet("session/active/{dungeonConfigId:int}")]
        public async Task<IActionResult> GetActiveSession(int dungeonConfigId)
        {
            var session = await _db.DungeonSessions
                .Where(s => s.DungeonConfigId == dungeonConfigId
                         && s.Status != "ended"
                         && s.CurrentPlayers < s.MaxPlayers)
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync();

            if (session == null)
                return Ok(new { has_session = false, session = (object?)null });

            return Ok(new
            {
                has_session = true,
                session = MapSession(session)
            });
        }

        /// <summary>
        /// POST /api/dungeon/session/create
        /// Host Unity gọi endpoint này ngay sau khi StartHost() thành công để đăng ký session.
        /// Body: { dungeon_config_id, host_ip, host_port }
        /// </summary>
        [HttpPost("session/create")]
        public async Task<IActionResult> CreateSession([FromBody] CreateDungeonSessionDto dto)
        {
            var config = await _db.DungeonConfigs.FindAsync(dto.DungeonConfigId);
            if (config == null)
                return NotFound(new { message = "DungeonConfig không tồn tại." });
            if (config.DungeonType == "solo")
                return BadRequest(new { message = "Phó bản solo không cần đăng ký session." });

            // Đóng các session cũ bị bỏ quên (orphan sessions) quá 1 giờ
            var staleTime = DateTime.UtcNow.AddHours(-1);
            var staleSessions = _db.DungeonSessions
                .Where(s => s.DungeonConfigId == dto.DungeonConfigId
                         && s.Status != "ended"
                         && s.UpdatedAt < staleTime);
            foreach (var s in staleSessions) s.Status = "ended";

            var session = new DungeonSession
            {
                DungeonConfigId = dto.DungeonConfigId,
                HostIp          = dto.HostIp,
                HostPort        = dto.HostPort,
                CurrentPlayers  = 1,   // Host đã vào
                MaxPlayers      = config.MaxPlayers,
                Status          = "waiting"
            };

            _db.DungeonSessions.Add(session);
            await _db.SaveChangesAsync();

            return Ok(MapSession(session));
        }

        /// <summary>
        /// POST /api/dungeon/session/{sessionId}/join
        /// Client gọi khi chuẩn bị connect tới host của session đã có sẵn.
        /// Server tăng current_players và chuyển status → active nếu đầy.
        /// </summary>
        [HttpPost("session/{sessionId:int}/join")]
        public async Task<IActionResult> JoinSession(int sessionId)
        {
            var session = await _db.DungeonSessions.FindAsync(sessionId);
            if (session == null) return NotFound(new { message = "Session không tồn tại." });
            if (session.Status == "ended") return BadRequest(new { message = "Session đã kết thúc." });
            if (session.CurrentPlayers >= session.MaxPlayers) return BadRequest(new { message = "Phó bản đã đầy." });

            session.CurrentPlayers++;
            if (session.CurrentPlayers >= session.MaxPlayers) session.Status = "active";
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, current_players = session.CurrentPlayers, session = MapSession(session) });
        }

        /// <summary>
        /// POST /api/dungeon/session/{sessionId}/leave
        /// Client gọi khi rời phó bản. Nếu không còn ai → session "ended".
        /// </summary>
        [HttpPost("session/{sessionId:int}/leave")]
        public async Task<IActionResult> LeaveSession(int sessionId)
        {
            var session = await _db.DungeonSessions.FindAsync(sessionId);
            if (session == null) return NotFound(new { message = "Session không tồn tại." });

            session.CurrentPlayers = Math.Max(0, session.CurrentPlayers - 1);
            if (session.CurrentPlayers == 0) session.Status = "ended";
            else if (session.Status == "active" && session.CurrentPlayers < session.MaxPlayers)
                session.Status = "waiting";
            session.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true, current_players = session.CurrentPlayers });
        }

        /// <summary>
        /// POST /api/dungeon/session/{sessionId}/end
        /// Host gọi khi phó bản kết thúc (boss chết / timeout / host disconnect).
        /// </summary>
        [HttpPost("session/{sessionId:int}/end")]
        public async Task<IActionResult> EndSession(int sessionId)
        {
            var session = await _db.DungeonSessions.FindAsync(sessionId);
            if (session == null) return NotFound(new { message = "Session không tồn tại." });

            session.Status     = "ended";
            session.UpdatedAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────────────
        //  MAP CONFIG ENDPOINT (player spawn points + enemy spawns)
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/dungeon/map/{mapId}/setup
        /// Unity host gọi ngay sau StartHost() để lấy:
        ///   - Vị trí spawn cầu thủ (spawn_points của map)
        ///   - Danh sách quái và vị trí spawn theo DB (enemy_spawns)
        /// Dùng để EnemySpawner tự động init theo config DB thay vì hard-code trong scene.
        /// </summary>
        [HttpGet("map/{mapId:int}/setup")]
        public async Task<IActionResult> GetMapSetup(int mapId)
        {
            var map = await _db.MapConfigs.FirstOrDefaultAsync(m => m.MapId == mapId);

            var enemySpawns = await EnemySpawnDataCompat.LoadResolvedSpawnsAsync(
                _db,
                mapId,
                _logger,
                HttpContext.RequestAborted);

            return Ok(new
            {
                map_id   = mapId,
                map_name = map?.MapName ?? $"Map {mapId}",
                // Player spawn positions từ map_config.spawn_points_json
                player_spawn_points_json = map?.SpawnPointsJson ?? "[]",
                // Enemy spawn config từ enemy_spawns
                enemy_spawns = enemySpawns.Select(spawn => new
                {
                    spawn_id        = spawn.SpawnId,
                    enemy_type_id   = spawn.EnemyTypeId,
                    spawn_x         = spawn.SpawnX,
                    spawn_y         = spawn.SpawnY,
                    max_spawn_count = spawn.MaxSpawnCount,
                    respawn_time    = spawn.RespawnTime,
                    enemy_name      = spawn.Enemy?.EnemyName ?? "",
                    enemy_level     = spawn.Level,
                    base_hp         = spawn.OverrideHp > 0 ? spawn.OverrideHp : spawn.Enemy?.BaseHp ?? 100,
                    base_damage     = spawn.Enemy?.BaseDamage ?? 10,
                    base_defense    = spawn.Enemy?.BaseDefense ?? 0,
                    exp_reward      = spawn.OverrideExp > 0 ? spawn.OverrideExp : spawn.Enemy?.ExpReward ?? 0,
                    gold_reward     = spawn.Enemy?.GoldReward ?? 0,
                    silver_reward   = spawn.Enemy?.SilverReward ?? 0,
                    drop_items_json = spawn.Enemy?.DropItemsJson,
                    element_type    = spawn.Enemy?.ElementType ?? "",
                    enemy_type      = spawn.IsBoss ? "Boss" : spawn.Enemy?.EnemyType ?? ""
                })
            });
        }

        /// <summary>
        /// GET /api/dungeon/boss/{bossId}/config
        /// Lấy cấu hình đầy đủ của boss (chỉ số, kỹ năng, giai đoạn, spawn config)
        /// BossAI.cs trong Unity gọi sau khi spawn boss để load config.
        /// </summary>
        [HttpGet("boss/{bossId:int}/config")]
        public async Task<IActionResult> GetBossConfig(int bossId)
        {
            var enemy = await _db.Enemies.FindAsync(bossId);
            if (enemy == null || enemy.EnemyType != "Boss")
                return NotFound(new { message = $"Boss #{bossId} không tồn tại." });

            var bossConfig = await _db.BossConfigs.FindAsync(bossId);

            return Ok(new
            {
                boss_id           = enemy.EnemyId,
                boss_name         = enemy.EnemyName,
                level             = enemy.Level,
                base_hp           = enemy.BaseHp,
                base_mp           = enemy.BaseMp,
                base_damage       = enemy.BaseDamage,
                base_defense      = enemy.BaseDefense,
                move_speed        = enemy.MoveSpeed,
                attack_speed      = enemy.AttackSpeed,
                element_type      = enemy.ElementType,
                exp_reward        = enemy.ExpReward,
                gold_reward       = enemy.GoldReward,
                silver_reward     = enemy.SilverReward,
                drop_items_json   = enemy.DropItemsJson,
                // Kỹ năng (raw JSON — BossAI deserialize phía client)
                skills_json       = enemy.SkillsJson,
                // Spawn config
                spawn_config = bossConfig == null ? null : new
                {
                    map_id           = bossConfig.MapId,
                    spawn_x          = bossConfig.SpawnX,
                    spawn_y          = bossConfig.SpawnY,
                    min_spawn_hour   = bossConfig.MinSpawnHour,
                    max_spawn_hour   = bossConfig.MaxSpawnHour,
                    respawn_minutes  = bossConfig.RespawnMinutes
                }
            });
        }

        // ─────────────────────────────────────────────────────────────
        //  PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────

        private static object MapSession(DungeonSession s) => new
        {
            session_id        = s.SessionId,
            dungeon_config_id = s.DungeonConfigId,
            host_ip           = s.HostIp,
            host_port         = s.HostPort,
            current_players   = s.CurrentPlayers,
            max_players       = s.MaxPlayers,
            status            = s.Status
        };
    }
}
