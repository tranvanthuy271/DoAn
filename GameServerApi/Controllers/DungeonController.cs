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
                .AsNoTracking()
                .Where(d => d.IsActive)
                .OrderBy(d => d.MinLevelRequired)
                .ThenBy(d => d.DungeonId)
                .Select(d => new
                {
                    dungeon_id          = d.DungeonId,
                    dungeon_name        = d.DungeonName ?? "",
                    dungeon_type        = d.DungeonType ?? "multi",
                    map_id              = d.MapId,
                    map_name            = d.Map != null ? (d.Map.MapName ?? "") : "",
                    scene_name          = d.SceneName ?? "",
                    max_players         = d.MaxPlayers,
                    min_level_required  = d.MinLevelRequired,
                    time_limit_seconds  = d.TimeLimitSeconds,
                    description         = d.Description ?? "",
                    thumbnail_icon_id   = d.ThumbnailIconId ?? "",
                    boss_enemy_id       = d.BossEnemyId,
                    reward_json         = d.RewardJson ?? "{}"
                })
                .ToListAsync();

            return Ok(new { dungeons });
        }

        /// <summary>
        /// GET /api/dungeon/{dungeonId}
        /// Lấy chi tiết một phó bản (kèm danh sách enemy spawns thuộc map đó).
        /// </summary>
        [HttpGet("{dungeonId:int}")]
        public async Task<IActionResult> GetDungeonDetail(int dungeonId)
        {
            var d = await _db.DungeonConfigs
                .AsNoTracking()
                .Where(x => x.DungeonId == dungeonId)
                .Select(x => new
                {
                    dungeon_id          = x.DungeonId,
                    dungeon_name        = x.DungeonName ?? "",
                    dungeon_type        = x.DungeonType ?? "multi",
                    map_id              = x.MapId,
                    map_name            = x.Map != null ? (x.Map.MapName ?? "") : "",
                    scene_name          = x.SceneName ?? "",
                    max_players         = x.MaxPlayers,
                    min_level_required  = x.MinLevelRequired,
                    time_limit_seconds  = x.TimeLimitSeconds,
                    description         = x.Description ?? "",
                    thumbnail_icon_id   = x.ThumbnailIconId ?? "",
                    reward_json         = x.RewardJson ?? "{}",
                    boss_enemy = x.BossEnemy == null ? null : new
                    {
                        enemy_id   = x.BossEnemy.EnemyId,
                        enemy_name = x.BossEnemy.EnemyName ?? "",
                        level      = x.BossEnemy.Level,
                        base_hp    = x.BossEnemy.BaseHp
                    },
                    player_spawn_points = x.Map != null ? (x.Map.SpawnPointsJson ?? "[]") : "[]"
                })
                .FirstOrDefaultAsync();

            if (d == null) return NotFound(new { message = "Dungeon không tồn tại." });

            // Lấy enemy spawns của map này
            var spawns = await EnemySpawnDataCompat.LoadResolvedSpawnsAsync(
                _db,
                d.map_id,
                _logger,
                HttpContext.RequestAborted);

            return Ok(new
            {
                dungeon_id          = d.dungeon_id,
                dungeon_name        = d.dungeon_name,
                dungeon_type        = d.dungeon_type,
                map_id              = d.map_id,
                map_name            = d.map_name,
                scene_name          = d.scene_name,
                max_players         = d.max_players,
                min_level_required  = d.min_level_required,
                time_limit_seconds  = d.time_limit_seconds,
                description         = d.description,
                thumbnail_icon_id   = d.thumbnail_icon_id,
                reward_json         = d.reward_json,
                boss_enemy          = d.boss_enemy,
                // Vị trí spawn cầu thủ khi vào phó bản
                player_spawn_points = d.player_spawn_points,
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
        /// GET /api/dungeon/wave/{dungeonId}/config
        /// Runtime config chuyên biệt cho WaveDungeonRuntime.
        /// Trả về flow config từ dungeon_wave_config và spawn/stat data đã resolve từ DB.
        /// </summary>
        [HttpGet("wave/{dungeonId:int}/config")]
        public async Task<IActionResult> GetWaveRuntimeConfig(int dungeonId)
        {
            var dungeon = await _db.DungeonConfigs
                .AsNoTracking()
                .Where(d => d.DungeonId == dungeonId)
                .Select(d => new
                {
                    dungeon_id = d.DungeonId,
                    dungeon_name = d.DungeonName ?? "",
                    map_id = d.MapId,
                    scene_name = d.SceneName ?? ""
                })
                .FirstOrDefaultAsync();

            if (dungeon == null)
                return NotFound(new { message = "Dungeon không tồn tại." });

            var waveConfig = await _db.Database
                .SqlQuery<WaveRuntimeConfigProjection>($"SELECT max_waves, wave_time_seconds, enemy_scale_percent, boss_scale_percent, exp_gold_scale_percent, daily_entry_limit, entry_item_plus1_id, entry_item_plus2_id, milestone_reward_json FROM dungeon_wave_config WHERE dungeon_id = {dungeonId}")
                .FirstOrDefaultAsync();

            var resolvedSpawns = await EnemySpawnDataCompat.LoadResolvedSpawnsPreferLegacyAsync(
                _db,
                dungeon.map_id,
                _logger,
                HttpContext.RequestAborted);

            var orderedSpawns = resolvedSpawns.OrderBy(spawn => spawn.SpawnId).ToArray();
            object[] normalSpawns = orderedSpawns
                .Where(spawn => !spawn.IsBoss)
                .Select(CreateWaveSpawnPayload)
                .ToArray();

            object? bossSpawn = orderedSpawns
                .Where(spawn => spawn.IsBoss)
                .Select(CreateWaveSpawnPayload)
                .FirstOrDefault();

            return Ok(new
            {
                dungeon_id = dungeon.dungeon_id,
                dungeon_name = dungeon.dungeon_name,
                map_id = dungeon.map_id,
                scene_name = dungeon.scene_name,
                max_waves = waveConfig?.max_waves ?? 20,
                wave_time_seconds = waveConfig?.wave_time_seconds ?? 300,
                enemy_scale_percent = waveConfig?.enemy_scale_percent ?? 10f,
                boss_scale_percent = waveConfig?.boss_scale_percent ?? 15f,
                exp_gold_scale_percent = waveConfig?.exp_gold_scale_percent ?? 10f,
                daily_entry_limit = waveConfig?.daily_entry_limit ?? 1,
                entry_item_plus1_id = waveConfig?.entry_item_plus1_id ?? 409,
                entry_item_plus2_id = waveConfig?.entry_item_plus2_id ?? 410,
                milestone_rewards = ParseMilestoneRewards(waveConfig?.milestone_reward_json),
                enemy_spawns = normalSpawns,
                boss_spawn = bossSpawn
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
                // Phase config (raw JSON — BossAI deserialize phía client)
                phases_json       = enemy.PhasesJson,
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
        //  WAVE ENTRY – giới hạn lượt vào + vé phó bản
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/dungeon/wave/{dungeonId}/entry-status/{playerId}
        /// Trả về entries_used, entries_limit và seconds_remaining_in_wave (nếu có session).
        /// Client hiển thị để player biết còn bao nhiêu lượt.
        /// </summary>
        [HttpGet("wave/{dungeonId:int}/entry-status/{playerId:int}")]
        public async Task<IActionResult> GetWaveEntryStatus(int dungeonId, int playerId)
        {
            var today = DateTime.UtcNow.Date;
            var entry = await _db.DungeonWaveEntries
                .FirstOrDefaultAsync(e => e.PlayerId == playerId
                                       && e.DungeonId == dungeonId
                                       && e.EntryDate == today);

            int used  = entry?.EntriesUsed  ?? 0;
            int limit = entry?.EntriesLimit ?? 1;

            // Nếu đang có session active, tính giây còn lại của vòng hiện tại
            var session = await _db.DungeonWaveSessions
                .FirstOrDefaultAsync(s => s.PlayerId == playerId
                                       && s.DungeonId == dungeonId
                                       && s.IsActive);

            int? secondsRemaining = null;
            if (session != null)
            {
                var waveConfig = await _db.Database
                    .SqlQuery<WaveConfigProjection>(
                        $"SELECT wave_time_seconds FROM dungeon_wave_config WHERE dungeon_id = {dungeonId}")
                    .FirstOrDefaultAsync();

                if (waveConfig != null)
                {
                    var elapsed = (int)(DateTime.UtcNow - session.WaveStartedAt).TotalSeconds;
                    secondsRemaining = Math.Max(0, waveConfig.wave_time_seconds - elapsed);
                }
            }

            return Ok(new
            {
                player_id        = playerId,
                dungeon_id       = dungeonId,
                entries_used     = used,
                entries_limit    = limit,
                entries_remaining = Math.Max(0, limit - used),
                has_active_session = session != null,
                active_wave        = session?.CurrentWave,
                active_phase       = session?.CurrentPhase,
                seconds_remaining_in_wave = secondsRemaining
            });
        }

        /// <summary>
        /// POST /api/dungeon/wave/{dungeonId}/enter
        /// Validate lượt vào, optionally dùng vé (+1 hoặc +2), tạo session.
        /// Body: { player_id, use_ticket_item_id? }
        ///   use_ticket_item_id = 409 (vé +1) hoặc 410 (vé +2) hoặc 0 (không dùng vé)
        /// </summary>
        [HttpPost("wave/{dungeonId:int}/enter")]
        public async Task<IActionResult> WaveEnter(int dungeonId, [FromBody] System.Text.Json.JsonElement body)
        {
            if (!body.TryGetProperty("player_id", out var pidEl) || pidEl.ValueKind != System.Text.Json.JsonValueKind.Number)
                return BadRequest(new { message = "Thiếu player_id." });
            int playerId = pidEl.GetInt32();

            int ticketItemId = 0;
            if (body.TryGetProperty("use_ticket_item_id", out var ticketEl) && ticketEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                ticketItemId = ticketEl.GetInt32();

            var today = DateTime.UtcNow.Date;

            // Load wave config (giới hạn lượt, id vé)
            var waveConf = await _db.Database
                .SqlQuery<WaveConfigProjection>(
                    $"SELECT wave_time_seconds, daily_entry_limit, entry_item_plus1_id, entry_item_plus2_id FROM dungeon_wave_config WHERE dungeon_id = {dungeonId}")
                .FirstOrDefaultAsync();

            int baseLimit = waveConf?.daily_entry_limit ?? 1;

            // Load/create entry record (upsert pattern)
            var entry = await _db.DungeonWaveEntries
                .FirstOrDefaultAsync(e => e.PlayerId == playerId
                                       && e.DungeonId == dungeonId
                                       && e.EntryDate == today);

            if (entry == null)
            {
                entry = new GameServerApi.Models.Entities.DungeonWaveEntry
                {
                    PlayerId     = playerId,
                    DungeonId    = dungeonId,
                    EntryDate    = today,
                    EntriesUsed  = 0,
                    EntriesLimit = baseLimit
                };
                _db.DungeonWaveEntries.Add(entry);
                await _db.SaveChangesAsync();
            }

            // Nếu player muốn dùng vé → validate có item + trừ item + tăng limit
            if (ticketItemId != 0)
            {
                int bonus = 0;
                if (waveConf != null && ticketItemId == waveConf.entry_item_plus1_id) bonus = 1;
                else if (waveConf != null && ticketItemId == waveConf.entry_item_plus2_id) bonus = 2;
                else return BadRequest(new { message = "Item không phải vé phó bản hợp lệ." });

                // Validate và tiêu hao item trong inventory (JSON-based)
                var player = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
                if (player == null) return NotFound(new { message = "Player không tồn tại." });

                var inv = string.IsNullOrEmpty(player.InventoryJson) || player.InventoryJson == "[]"
                    ? new List<Dictionary<string, System.Text.Json.JsonElement>>()
                    : System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, System.Text.Json.JsonElement>>>(player.InventoryJson)
                      ?? new List<Dictionary<string, System.Text.Json.JsonElement>>();

                var ticketSlot = inv.FirstOrDefault(slot =>
                    slot.TryGetValue("itemTemplateId", out var idEl) &&
                    idEl.ValueKind == System.Text.Json.JsonValueKind.Number &&
                    idEl.GetInt32() == ticketItemId &&
                    slot.TryGetValue("quantity", out var qEl) &&
                    qEl.ValueKind == System.Text.Json.JsonValueKind.Number &&
                    qEl.GetInt32() > 0);

                if (ticketSlot == null)
                    return BadRequest(new { message = "Không đủ vé phó bản trong túi đồ." });

                // Trừ 1 vé
                int currentQty = ticketSlot["quantity"].GetInt32();
                ticketSlot["quantity"] = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>((currentQty - 1).ToString());
                player.InventoryJson = System.Text.Json.JsonSerializer.Serialize(inv);

                entry.EntriesLimit += bonus;
                entry.UpdatedAt     = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // Kiểm tra còn lượt không
            if (entry.EntriesUsed >= entry.EntriesLimit)
                return BadRequest(new { message = $"Đã dùng hết {entry.EntriesLimit} lượt hôm nay. Dùng vé để thêm lượt." });

            // Đóng session active cũ nếu có (bị bỏ dở)
            var oldSession = await _db.DungeonWaveSessions
                .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.DungeonId == dungeonId && s.IsActive);
            if (oldSession != null)
            {
                oldSession.IsActive   = false;
                oldSession.ExitReason = "left";
                oldSession.UpdatedAt  = DateTime.UtcNow;
            }

            // Tạo session mới
            var newSession = new GameServerApi.Models.Entities.DungeonWaveSession
            {
                PlayerId        = playerId,
                DungeonId       = dungeonId,
                CurrentWave     = 1,
                CurrentPhase    = "enemy",
                SessionStartedAt = DateTime.UtcNow,
                WaveStartedAt    = DateTime.UtcNow,
                IsActive         = true,
                ExitReason       = ""
            };
            _db.DungeonWaveSessions.Add(newSession);

            // Tăng entries_used
            entry.EntriesUsed++;
            entry.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success      = true,
                session_id   = newSession.SessionId,
                entries_used = entry.EntriesUsed,
                entries_limit = entry.EntriesLimit
            });
        }

        /// <summary>
        /// POST /api/dungeon/wave/{dungeonId}/session/update
        /// Unity host gọi mỗi khi bắt đầu vòng mới để server lưu trạng thái reconnect.
        /// Body: { player_id, current_wave, current_phase }
        /// </summary>
        [HttpPost("wave/{dungeonId:int}/session/update")]
        public async Task<IActionResult> UpdateWaveSession(int dungeonId, [FromBody] System.Text.Json.JsonElement body)
        {
            if (!body.TryGetProperty("player_id", out var pidEl)) return BadRequest(new { message = "Thiếu player_id." });
            int playerId = pidEl.GetInt32();

            var session = await _db.DungeonWaveSessions
                .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.DungeonId == dungeonId && s.IsActive);
            if (session == null) return NotFound(new { message = "Không có session active." });

            if (body.TryGetProperty("current_wave", out var waveEl)) session.CurrentWave  = waveEl.GetInt32();
            if (body.TryGetProperty("current_phase", out var phaseEl)) session.CurrentPhase = phaseEl.GetString() ?? "enemy";
            session.WaveStartedAt = DateTime.UtcNow;
            session.UpdatedAt     = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return Ok(new { success = true, current_wave = session.CurrentWave, current_phase = session.CurrentPhase });
        }

        /// <summary>
        /// POST /api/dungeon/wave/{dungeonId}/session/end
        /// Unity host gọi khi phó bản kết thúc (hoàn thành / timeout / rời).
        /// Body: { player_id, exit_reason } — exit_reason: "completed" | "timeout" | "left"
        /// </summary>
        [HttpPost("wave/{dungeonId:int}/session/end")]
        public async Task<IActionResult> EndWaveSession(int dungeonId, [FromBody] System.Text.Json.JsonElement body)
        {
            if (!body.TryGetProperty("player_id", out var pidEl)) return BadRequest(new { message = "Thiếu player_id." });
            int playerId = pidEl.GetInt32();

            string reason = "left";
            if (body.TryGetProperty("exit_reason", out var reasonEl)) reason = reasonEl.GetString() ?? "left";

            var session = await _db.DungeonWaveSessions
                .FirstOrDefaultAsync(s => s.PlayerId == playerId && s.DungeonId == dungeonId && s.IsActive);
            if (session == null) return Ok(new { success = true, message = "Không có session active để đóng." });

            int reachedWave = session.CurrentWave;
            session.IsActive   = false;
            session.ExitReason = reason;
            session.UpdatedAt  = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // ── Cập nhật kỷ lục phó bản (best_wave) ───────────────────────
            await UpdateDungeonRecordAsync(playerId, dungeonId, reachedWave);

            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────────────
        //  PRIVATE HELPERS
        // ─────────────────────────────────────────────────────────────

        // Keyless projection cho dungeon_wave_config raw query
        private class WaveConfigProjection
        {
            public int wave_time_seconds   { get; set; }
            public int daily_entry_limit   { get; set; }
            public int entry_item_plus1_id { get; set; }
            public int entry_item_plus2_id { get; set; }
        }

        private sealed class WaveRuntimeConfigProjection
        {
            public int max_waves { get; set; }
            public int wave_time_seconds { get; set; }
            public float enemy_scale_percent { get; set; }
            public float boss_scale_percent { get; set; }
            public float exp_gold_scale_percent { get; set; }
            public int daily_entry_limit { get; set; }
            public int entry_item_plus1_id { get; set; }
            public int entry_item_plus2_id { get; set; }
            public string milestone_reward_json { get; set; } = "[]";
        }

        // ── Cập nhật kỷ lục phó bản ───────────────────────────────────────────
        private async Task UpdateDungeonRecordAsync(int characterId, int dungeonId, int reachedWave)
        {
            try
            {
                var player = await _db.PlayerData.FindAsync(characterId);
                if (player == null) return;

                var info = player.GetInfoChar();
                info.DungeonBestWaves ??= new System.Collections.Generic.Dictionary<int, int>();

                if (!info.DungeonBestWaves.TryGetValue(dungeonId, out int existing)
                    || reachedWave > existing)
                {
                    info.DungeonBestWaves[dungeonId] = reachedWave;
                    player.SetInfoChar(info);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Dungeon] Không thể cập nhật kỷ lục cho characterId={Id}: {Msg}", characterId, ex.Message);
            }
        }

        private static object CreateWaveSpawnPayload(EnemySpawnDataCompat.ResolvedEnemySpawn spawn)
        {
            int resolvedHp = spawn.OverrideHp > 0 ? spawn.OverrideHp : spawn.Enemy?.BaseHp ?? 1;
            int resolvedExp = spawn.OverrideExp > 0 ? spawn.OverrideExp : spawn.Enemy?.ExpReward ?? 0;

            return new
            {
                enemy_id = spawn.EnemyTypeId,
                enemy_name = spawn.Enemy?.EnemyName ?? "",
                spawn_x = spawn.SpawnX,
                spawn_y = spawn.SpawnY,
                is_boss = spawn.IsBoss,
                level = spawn.Level > 0 ? spawn.Level : spawn.Enemy?.Level ?? 1,
                max_hp = resolvedHp,
                max_mp = spawn.Enemy?.BaseMp ?? 0,
                base_damage = spawn.Enemy?.BaseDamage ?? 1,
                base_defense = spawn.Enemy?.BaseDefense ?? 0,
                exp_reward = resolvedExp,
                respawn_time = Math.Max(0, spawn.RespawnTime),
                move_speed = spawn.Enemy?.MoveSpeed ?? 2f,
                can_fly = false,
                element_type = spawn.Enemy?.ElementType ?? "None",
                drops = ParseDropItems(spawn.Enemy?.DropItemsJson)
            };
        }

        private static object[] ParseMilestoneRewards(string? milestoneRewardJson)
        {
            if (string.IsNullOrWhiteSpace(milestoneRewardJson))
                return Array.Empty<object>();

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(milestoneRewardJson);
                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                    return Array.Empty<object>();

                var milestones = new List<object>();
                foreach (var milestone in doc.RootElement.EnumerateArray())
                {
                    int wave = GetIntValueOrDefault(milestone, "wave", GetIntValueOrDefault(milestone, "atWave", 0));
                    long bonusExp = GetLongValueOrDefault(milestone, "bonus_exp", GetLongValueOrDefault(milestone, "exp", 0));
                    long bonusGold = GetLongValueOrDefault(milestone, "bonus_gold", GetLongValueOrDefault(milestone, "gold", 0));

                    object[] items = Array.Empty<object>();
                    if (milestone.ValueKind == System.Text.Json.JsonValueKind.Object
                        && milestone.TryGetProperty("items", out var itemsElement)
                        && itemsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        var itemList = new List<object>();
                        foreach (var item in itemsElement.EnumerateArray())
                        {
                            int itemTemplateId = GetIntValueOrDefault(item, "item_template_id", 0);
                            if (itemTemplateId <= 0)
                                continue;

                            itemList.Add(new
                            {
                                item_template_id = itemTemplateId,
                                quantity = Math.Max(1, GetIntValueOrDefault(item, "quantity", GetIntValueOrDefault(item, "qty", 1))),
                                upgrade_level = Math.Max(0, GetIntValueOrDefault(item, "upgrade_level", 0)),
                                str_options = GetStringValueOrDefault(item, "str_options", "")
                            });
                        }

                        items = itemList.ToArray();
                    }

                    milestones.Add(new
                    {
                        wave,
                        bonus_exp = bonusExp,
                        bonus_gold = bonusGold,
                        items
                    });
                }

                return milestones.ToArray();
            }
            catch (System.Text.Json.JsonException)
            {
                return Array.Empty<object>();
            }
        }

        private static object[] ParseDropItems(string? dropItemsJson)
        {
            if (string.IsNullOrWhiteSpace(dropItemsJson))
                return Array.Empty<object>();

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(dropItemsJson);
                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                    return Array.Empty<object>();

                var drops = new List<object>();
                foreach (var drop in doc.RootElement.EnumerateArray())
                {
                    int itemId = GetIntValueOrDefault(drop, "item_id", 0);
                    if (itemId <= 0)
                        continue;

                    drops.Add(new
                    {
                        item_id = itemId,
                        rate = GetDoubleValueOrDefault(drop, "drop_chance", GetDoubleValueOrDefault(drop, "rate", 0d)),
                        qty_min = Math.Max(1, GetIntValueOrDefault(drop, "qty_min", 1)),
                        qty_max = Math.Max(1, GetIntValueOrDefault(drop, "qty_max", GetIntValueOrDefault(drop, "qty_min", 1)))
                    });
                }

                return drops.ToArray();
            }
            catch (System.Text.Json.JsonException)
            {
                return Array.Empty<object>();
            }
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
                System.Text.Json.JsonValueKind.String when int.TryParse(property.GetString(), out int stringValue) => stringValue,
                _ => defaultValue
            };
        }

        private static long GetLongValueOrDefault(System.Text.Json.JsonElement element, string propertyName, long defaultValue)
        {
            if (element.ValueKind != System.Text.Json.JsonValueKind.Object
                || !element.TryGetProperty(propertyName, out var property))
            {
                return defaultValue;
            }

            return property.ValueKind switch
            {
                System.Text.Json.JsonValueKind.Number when property.TryGetInt64(out long numberValue) => numberValue,
                System.Text.Json.JsonValueKind.String when long.TryParse(property.GetString(), out long stringValue) => stringValue,
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
                System.Text.Json.JsonValueKind.String when double.TryParse(property.GetString(), out double stringValue) => stringValue,
                _ => defaultValue
            };
        }

        private static string GetStringValueOrDefault(System.Text.Json.JsonElement element, string propertyName, string defaultValue)
        {
            if (element.ValueKind != System.Text.Json.JsonValueKind.Object
                || !element.TryGetProperty(propertyName, out var property))
            {
                return defaultValue;
            }

            return property.ValueKind == System.Text.Json.JsonValueKind.String
                ? property.GetString() ?? defaultValue
                : defaultValue;
        }

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
