using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using GameServerApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GameServerApi.Controllers
{
    //  In-memory registry: track which player is hosting each world map.
    //  Mỗi map chỉ có 1 host tại một thời điểm.
    //  Entry tự hết hạn sau HostTimeoutSeconds giây (phòng host crash không unregister).
    internal static class MapHostRegistry
    {
        private record HostEntry(string Ip, ushort Port, int PlayerId, DateTime RegisteredAt);

        private static readonly ConcurrentDictionary<int, HostEntry> _registry = new();
        private const int HostTimeoutSeconds = 120; // host có 120s không heartbeat → bị xoá

        // Lấy thông tin host hiện tại của map (null nếu không có hoặc đã hết hạn).
        public static (bool hasHost, string ip, ushort port, int playerId) Check(int mapId)
        {
            if (_registry.TryGetValue(mapId, out var entry))
            {
                if ((DateTime.UtcNow - entry.RegisteredAt).TotalSeconds < HostTimeoutSeconds)
                    return (true, entry.Ip, entry.Port, entry.PlayerId);
                // Hết hạn → xoá
                _registry.TryRemove(mapId, out _);
            }
            return (false, "", 0, 0);
        }

        // Thử đăng ký làm host cho map.
        // - Nếu chưa có host (hoặc entry đã hết hạn): đăng ký thành công → youAreHost=true.
        // - Nếu đã có host: trả về thông tin host hiện tại → youAreHost=false.
        public static (bool youAreHost, string hostIp, ushort hostPort) Register(
            int mapId, string ip, ushort port, int playerId)
        {
            // Xoá entry hết hạn trước khi thêm mới
            Check(mapId);

            var newEntry = new HostEntry(ip, port, playerId, DateTime.UtcNow);

            // TryAdd: thành công nếu chưa có key, thất bại nếu đã có
            if (_registry.TryAdd(mapId, newEntry))
                return (true, ip, port);

            // Ai đó đã đăng ký trước — trả về thông tin của họ
            if (_registry.TryGetValue(mapId, out var existing))
                return (false, existing.Ip, existing.Port);

            // Race hiếm: entry vừa bị xoá đúng lúc → thử lại
            if (_registry.TryAdd(mapId, newEntry))
                return (true, ip, port);

            return (false, ip, port);
        }

        // Huỷ đăng ký host. Chỉ thành công nếu player_id khớp với host hiện tại.
        public static bool Unregister(int mapId, int playerId)
        {
            if (_registry.TryGetValue(mapId, out var entry) && entry.PlayerId == playerId)
                return _registry.TryRemove(mapId, out _);
            return false;
        }

        // Heartbeat: reset timer để tránh hết hạn. Gọi mỗi ~30s từ client.
        public static void Heartbeat(int mapId, int playerId)
        {
            if (_registry.TryGetValue(mapId, out var entry) && entry.PlayerId == playerId)
                _registry[mapId] = entry with { RegisteredAt = DateTime.UtcNow };
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly IMemoryCache  _cache;
        private readonly ILogger<MapController> _logger;

        // Thời gian cache spawn-config — tránh gọi DB liên tục mỗi lần host join map
        private static readonly TimeSpan SpawnConfigCacheTtl = TimeSpan.FromMinutes(5);

        public MapController(GameDbContext db, IMemoryCache cache, ILogger<MapController> logger)
        {
            _db    = db;
            _cache = cache;
            _logger = logger;
        }

        private sealed class RuntimeEntryPointDto
        {
            public float x { get; set; }
            public float y { get; set; }
        }

        // GET /api/map/{mapId}/config
        // Lấy thông tin cấu hình map: spawn points, scene name, level range và yêu cầu nhiệm vụ.
        [HttpGet("{mapId}/config")]
        public async Task<IActionResult> GetMapConfig(int mapId)
        {
            var mapConfig = await _db.MapConfigs.FirstOrDefaultAsync(m => m.MapId == mapId);

            if (mapConfig == null)
            {
                var defaultSpawnPoints = new[] { new { x = 0f, y = 0f }, new { x = 5f, y = 0f }, new { x = -5f, y = 0f } };
                return Ok(new { map_id = mapId, map_name = "Default Map", scene_name = "", spawn_points = defaultSpawnPoints });
            }

            try
            {
                var spawnPoints = JsonSerializer.Deserialize<object[]>(mapConfig.SpawnPointsJson) ?? Array.Empty<object>();
                return Ok(new
                {
                    map_id         = mapConfig.MapId,
                    map_name       = mapConfig.MapName,
                    scene_name     = mapConfig.SceneName,
                    spawn_points   = spawnPoints,
                    min_level      = mapConfig.MinLevel,
                    max_level      = mapConfig.MaxLevel,
                    required_quest_id = NormalizeRequiredQuestId(mapConfig.RequiredQuestId)
                });
            }
            catch (JsonException)
            {
                return Ok(new { map_id = mapId, map_name = mapConfig.MapName, scene_name = mapConfig.SceneName, spawn_points = new[] { new { x = 0f, y = 0f } } });
            }
        }

        // GET /api/map/runtime-bootstrap
        // Dedicated ServerScene dùng endpoint này để nạp runtime map definitions từ DB
        // thay vì phụ thuộc hoàn toàn vào MapWorldConfig.asset tĩnh.
        // Quy ước hiện tại:
        // - map open world     -> zone_topology = 0 (SharedPublic)
        // - map dungeon/room   -> zone_topology = 1 (InstanceOnly)
        // Việc phân loại dungeon map được suy ra từ dungeon_config + map_portal.
        [HttpGet("runtime-bootstrap")]
        public async Task<IActionResult> GetRuntimeBootstrap()
        {
            var maps = await _db.MapConfigs
                .OrderBy(m => m.MapId)
                .ToListAsync();

            var dungeonConfigs = await _db.DungeonConfigs
                .Where(d => d.IsActive)
                .Select(d => new { d.DungeonId, d.MapId, d.MaxPlayers })
                .ToListAsync();

            var dungeonPortals = await _db.MapPortals
                .Where(p => p.IsActive && p.DungeonId != null)
                .Select(p => new
                {
                    p.SourceMapId,
                    p.DestMapId,
                    p.PortalType,
                    DungeonId = p.DungeonId!.Value
                })
                .ToListAsync();

            var instanceMapIds = new HashSet<int>(dungeonConfigs.Select(d => d.MapId));
            var mapToDungeonId = new Dictionary<int, int>();
            var dungeonMaxPlayersById = dungeonConfigs
                .GroupBy(d => d.DungeonId)
                .ToDictionary(g => g.Key, g => g.Max(x => x.MaxPlayers));

            foreach (var dungeon in dungeonConfigs)
                mapToDungeonId.TryAdd(dungeon.MapId, dungeon.DungeonId);

            foreach (var portal in dungeonPortals)
            {
                if (portal.PortalType == "enter_dungeon" || portal.PortalType == "room_transition")
                {
                    instanceMapIds.Add(portal.DestMapId);
                    mapToDungeonId.TryAdd(portal.DestMapId, portal.DungeonId);
                }

                if (portal.PortalType == "room_transition" || portal.PortalType == "exit_dungeon")
                {
                    instanceMapIds.Add(portal.SourceMapId);
                    mapToDungeonId.TryAdd(portal.SourceMapId, portal.DungeonId);
                }
            }

            var response = maps.Select(map =>
            {
                bool isInstanceMap = instanceMapIds.Contains(map.MapId);
                int customZoneMaxPlayers = 0;

                if (isInstanceMap &&
                    mapToDungeonId.TryGetValue(map.MapId, out int dungeonId) &&
                    dungeonMaxPlayersById.TryGetValue(dungeonId, out int dungeonMaxPlayers))
                {
                    customZoneMaxPlayers = dungeonMaxPlayers;
                }

                return new
                {
                    map_id = map.MapId,
                    map_name = map.MapName,
                    scene_name = map.SceneName,
                    zone_topology = isInstanceMap ? 1 : 0,
                    allow_custom_zones = isInstanceMap,
                    public_zone_count_override = 0,
                    public_zone_max_players_override = 0,
                    custom_zone_max_players_override = customZoneMaxPlayers,
                    allow_player_zone_switch = !isInstanceMap,
                    entry_points = ParseRuntimeEntryPoints(map.SpawnPointsJson)
                };
            }).ToArray();

            return Ok(new { maps = response });
        }

        private static RuntimeEntryPointDto[] ParseRuntimeEntryPoints(string? spawnPointsJson)
        {
            if (string.IsNullOrWhiteSpace(spawnPointsJson))
                return new[] { new RuntimeEntryPointDto { x = 0f, y = 0f } };

            try
            {
                var points = JsonSerializer.Deserialize<RuntimeEntryPointDto[]>(spawnPointsJson);
                if (points != null && points.Length > 0)
                    return points;
            }
            catch (JsonException)
            {
            }

            return new[] { new RuntimeEntryPointDto { x = 0f, y = 0f } };
        }

        // GET /api/map/{mapId}/portals
        // Lấy danh sách cổng dịch chuyển đang hoạt động trên map.
        // Client dùng dữ liệu này để spawn MapPortalTrigger đúng vị trí.
        [HttpGet("{mapId}/portals")]
        public async Task<IActionResult> GetMapPortals(int mapId)
        {
            var portals = await _db.MapPortals
                .Where(p => p.SourceMapId == mapId && p.IsActive)
                .ToListAsync();

            return Ok(new
            {
                map_id  = mapId,
                portals = portals.Select(p => new
                {
                    portal_id        = p.PortalId,
                    portal_name      = p.PortalName,
                    src_x            = p.SrcX,
                    src_y            = p.SrcY,
                    src_radius       = p.SrcRadius,
                    dest_map_id      = p.DestMapId,
                    dest_scene_name  = p.DestSceneName,
                    dest_x           = p.DestX,
                    dest_y           = p.DestY,
                    portal_type      = p.PortalType,
                    required_item_id = p.RequiredItemId,
                    required_level   = NormalizeRequiredLevel(p.RequiredLevel),
                    required_quest_id = NormalizeRequiredQuestId(p.RequiredQuestId),
                    dungeon_id       = p.DungeonId
                })
            });
        }

        // POST /api/map/travel
        // Server validate và cấp phép dịch chuyển.
        // Client gọi khi player chạm trigger zone của portal.
        // Body: { portal_id, player_id, current_map_id, player_x, player_y }
        [HttpPost("travel")]
        public async Task<IActionResult> TravelPortal([FromBody] TravelRequest req)
        {
            var portal = await _db.MapPortals.FindAsync(req.PortalId);
            if (portal == null || !portal.IsActive)
                return BadRequest(new { success = false, message = "Cổng dịch chuyển không tồn tại hoặc đã bị khoá." });

            // Player phải đang ở đúng map nguồn của portal.
            if (portal.SourceMapId != req.CurrentMapId)
                return BadRequest(new { success = false, message = "Vị trí không hợp lệ." });

            // Validate khoảng cách giữa player và portal (chống teleport hack)
            // Biên map (left/right) dùng BoxCollider2D vật lý làm validator — bỏ qua dist check.
            // Chỉ validate với dungeon portal (enter_dungeon / exit_dungeon / room_transition).
            bool isEdgePortal = portal.PortalDirection == "left" || portal.PortalDirection == "right";
            if (!isEdgePortal)
            {
                float dx = req.PlayerX - portal.SrcX;
                float dy = req.PlayerY - portal.SrcY;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist > portal.SrcRadius * 2f)  // leniency x2 cho độ trễ mạng
                    return BadRequest(new { success = false, message = "Bạn không ở gần cổng." });
            }

            // Kiểm tra item bắt buộc nếu portal yêu cầu.
            if (portal.RequiredItemId.HasValue)
            {
                var player = await _db.PlayerData.FindAsync(req.PlayerId);
                if (player == null)
                    return BadRequest(new { success = false, message = "Player không tồn tại." });

                // InventoryJson là mảng slot; chỉ cần có required_item_id trong một slot là hợp lệ.
                bool hasItem = false;
                if (!string.IsNullOrEmpty(player.InventoryJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(player.InventoryJson);
                        foreach (var slot in doc.RootElement.EnumerateArray())
                        {
                            if (slot.TryGetProperty("item_id", out var idProp) &&
                                idProp.GetInt32() == portal.RequiredItemId.Value)
                            {
                                hasItem = true;
                                break;
                            }
                        }
                    }
                    catch (JsonException) { /* inventory malformed - deny */ }
                }

                if (!hasItem)
                    return BadRequest(new { success = false, message = $"Cần có Chìa Khóa (item #{portal.RequiredItemId}) để vào đây." });
            }
            // Kiểm tra yêu cầu của portal và map đích (level + required_quest_id).
            // required_quest_id = 0 là seed cũ, coi như NULL để tránh khóa map vĩnh viễn.
            var destMap = await _db.MapConfigs.FindAsync(portal.DestMapId);
            int requiredLevel = Math.Max(
                NormalizeRequiredLevel(portal.RequiredLevel),
                destMap?.MinLevel ?? 1);
            int? portalRequiredQuestId = NormalizeRequiredQuestId(portal.RequiredQuestId);
            int? mapRequiredQuestId = NormalizeRequiredQuestId(destMap?.RequiredQuestId);
            bool hasAccessRequirement = requiredLevel > 1
                                     || portalRequiredQuestId.HasValue
                                     || mapRequiredQuestId.HasValue;

            if (hasAccessRequirement)
            {
                var player = await _db.PlayerData.FindAsync(req.PlayerId);
                if (player == null)
                    return BadRequest(new { success = false, message = "Player kh\u00f4ng t\u1ed3n t\u1ea1i." });

                var info = player.GetInfoChar();
                string destName = destMap?.MapName ?? portal.PortalName;

                // Ki\u1ec3m tra level t\u1ed1i thi\u1ec3u
                if (info.Level < requiredLevel)
                    return BadRequest(new
                    {
                        success = false,
                        message = $"B\u1ea1n c\u1ea7n \u0111\u1ea1t Level {requiredLevel} \u0111\u1ec3 v\u00e0o {destName}. (Level hi\u1ec7n t\u1ea1i: {info.Level})"
                    });

                // Ki\u1ec3m tra nhi\u1ec7m v\u1ee5 b\u1eaft bu\u1ed9c
                foreach (int requiredQuestId in BuildRequiredQuestIds(portalRequiredQuestId, mapRequiredQuestId))
                {
                    bool hasQuest = info.CompletedQuests != null &&
                                    info.CompletedQuests.Contains(requiredQuestId);

                    if (!hasQuest)
                    {
                        var quest = await _db.QuestConfigs.FindAsync(requiredQuestId);
                        string questName = quest?.Name ?? $"#{requiredQuestId}";
                        return BadRequest(new
                        {
                            success = false,
                            message = $"B\u1ea1n c\u1ea7n ho\u00e0n th\u00e0nh nhi\u1ec7m v\u1ee5 \"{questName}\" tr\u01b0\u1edbc khi v\u00e0o {destName}."
                        });
                    }
                }
            }

            return Ok(new
            {
                success         = true,
                dest_map_id     = portal.DestMapId,
                dest_scene_name = portal.DestSceneName,
                dest_x          = portal.DestX,
                dest_y          = portal.DestY,
                portal_type     = portal.PortalType,
                portal_name     = portal.PortalName
            });
        }

        // GET /api/map/by-scene?scene=GameScene
        // Tìm map_config theo scene_name (dùng cho MapManager.cs trên client).
        [HttpGet("by-scene")]
        public async Task<IActionResult> GetMapByScene([FromQuery] string scene)
        {
            if (string.IsNullOrWhiteSpace(scene))
                return BadRequest(new { message = "scene param required" });

            var map = await _db.MapConfigs.FirstOrDefaultAsync(m => m.SceneName == scene);
            if (map == null)
                return NotFound(new { message = $"Scene '{scene}' không tìm thấy trong map_config." });

            return Ok(new
            {
                map_id     = map.MapId,
                map_name   = map.MapName,
                scene_name = map.SceneName,
                min_level  = map.MinLevel,
                max_level  = map.MaxLevel
            });
        }

        // GET /api/map/portal/direction?mapId=1&amp;direction=right
        // Lấy portal trái hoặc phải của map (dùng cho MapTransitionButton.cs).
        // portal_direction trong DB được set trước khi INSERT (left | right | none).
        [HttpGet("portal/direction")]
        public async Task<IActionResult> GetPortalByDirection(
            [FromQuery] int mapId,
            [FromQuery] string direction)
        {
            if (direction != "left" && direction != "right")
                return BadRequest(new { message = "direction phải là 'left' hoặc 'right'." });

            var portal = await _db.MapPortals
                .Where(p => p.SourceMapId == mapId && p.PortalDirection == direction && p.IsActive)
                .FirstOrDefaultAsync();

            if (portal == null)
                return NotFound(new { message = $"Map {mapId} không có portal hướng '{direction}'." });

            return Ok(new
            {
                portal_id       = portal.PortalId,
                portal_name     = portal.PortalName,
                src_x           = portal.SrcX,
                src_y           = portal.SrcY,
                src_radius      = portal.SrcRadius,
                dest_map_id     = portal.DestMapId,
                dest_scene_name = portal.DestSceneName,
                dest_x          = portal.DestX,
                dest_y          = portal.DestY,
                required_level  = NormalizeRequiredLevel(portal.RequiredLevel),
                required_quest_id = NormalizeRequiredQuestId(portal.RequiredQuestId)
            });
        }

        // Host Registry endpoints

        // GET /api/map/host/check?mapId=1
        // Kiểm tra có host nào đang chạy cho map này không.
        // Response: { has_host, host_ip, host_port, player_id }
        [HttpGet("host/check")]
        public IActionResult CheckHost([FromQuery] int mapId)
        {
            var (hasHost, ip, port, playerId) = MapHostRegistry.Check(mapId);
            return Ok(new { has_host = hasHost, host_ip = ip, host_port = (int)port, player_id = playerId });
        }

        // POST /api/map/host/register
        // Đăng ký làm host cho map (atomic: race-safe).
        // - Nếu chưa có host: đăng ký thành công → { you_are_host: true, host_ip, host_port }
        // - Nếu đã có host: → { you_are_host: false, host_ip: &lt;existing>, host_port: &lt;existing> }
        // Body: { map_id, host_ip, host_port, player_id }
        [HttpPost("host/register")]
        public IActionResult RegisterHost([FromBody] MapHostRegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.HostIp) || req.HostPort == 0)
                return BadRequest(new { message = "host_ip và host_port không được để trống." });

            var (youAreHost, hostIp, hostPort) = MapHostRegistry.Register(
                req.MapId, req.HostIp, (ushort)req.HostPort, req.PlayerId);

            return Ok(new
            {
                success      = true,
                you_are_host = youAreHost,
                host_ip      = hostIp,
                host_port    = (int)hostPort
            });
        }

        // POST /api/map/host/unregister
        // Huỷ đăng ký host khi player rời map.
        // Chỉ thành công nếu player_id khớp với host hiện tại.
        // Body: { map_id, player_id }
        [HttpPost("host/unregister")]
        public IActionResult UnregisterHost([FromBody] MapHostUnregisterRequest req)
        {
            bool removed = MapHostRegistry.Unregister(req.MapId, req.PlayerId);
            return Ok(new { success = true, removed });
        }

        // POST /api/map/host/heartbeat
        // Reset timer của host entry để tránh hết hạn (120s timeout).
        // Gọi mỗi ~30s từ host client.
        // Body: { map_id, player_id }
        [HttpPost("host/heartbeat")]
        public IActionResult HostHeartbeat([FromBody] MapHostUnregisterRequest req)
        {
            MapHostRegistry.Heartbeat(req.MapId, req.PlayerId);
            return Ok(new { success = true });
        }

        //  Spawn Config — JSON-based enemy spawn + drop configuration

        // GET /api/map/{mapId}/spawn-config
        // Lấy cấu hình spawn enemy cho map.
        // Unity host gọi endpoint này khi scene load để fetch toàn bộ spawn data.
        // Response:
        // {
        // "map_id": 0,
        // "spawns": [{enemy_id, cx, cy, is_boss, count, respawn_time, level}, ...],
        // "enemy_skills": [{enemy_id, enemy_name, base_hp, base_damage, element_type,
        // exp_reward, gold_reward, silver_reward,
        // drops:[{item_id, rate, qty_min, qty_max}],
        // skills:[...]}]
        // }
        // HP, EXP, drop → lấy từ enemy_skills (nguồn là bảng enemy cột trực tiếp).
        // Nếu chưa có config → trả về spawns:[] (không lỗi).
        [HttpGet("{mapId}/spawn-config")]
        public async Task<IActionResult> GetSpawnConfig(int mapId)
        {
            // —— Hit cache trước ——
            string cacheKey = $"spawn_config_{mapId}";
            if (_cache.TryGetValue(cacheKey, out object? cached))
                return Ok(cached);

            // Prefer map_spawn_config (has correct ground-level cy positions from ground_spawns.sql).
            // enemy_spawns.spawn_y often defaults to 0 which causes enemies to float mid-air on server.
            var spawnRows = await EnemySpawnDataCompat.LoadResolvedSpawnsPreferLegacyAsync(
                _db,
                mapId,
                _logger,
                HttpContext.RequestAborted);

            if (spawnRows.Count == 0)
            {
                var empty = new
                {
                    map_id       = mapId,
                    spawns       = Array.Empty<object>(),
                    enemy_skills = Array.Empty<object>()
                };
                _cache.Set(cacheKey, empty, TimeSpan.FromSeconds(30));
                return Ok(empty);
            }

            var spawnsObj = spawnRows.Select(spawn => (object)new
            {
                enemy_id     = spawn.EnemyTypeId,
                cx           = spawn.SpawnX,
                cy           = spawn.SpawnY,
                is_boss      = spawn.IsBoss,
                count        = spawn.MaxSpawnCount,
                respawn_time = spawn.RespawnTime,
                level        = spawn.Level,
                // ✅ FIX: Truyền override HP/EXP từ map_spawn_config (legacy) nếu có
                override_hp  = spawn.OverrideHp,
                override_exp = spawn.OverrideExp
            }).ToArray();

            var enemySkillsObj = await BuildEnemySkillsResponseAsync(
                spawnRows.Select(spawn => spawn.EnemyTypeId).Where(id => id > 0).Distinct().ToArray());

            var result = new
            {
                map_id       = mapId,
                spawns       = spawnsObj,
                enemy_skills = enemySkillsObj
            };

            _cache.Set(cacheKey, result, SpawnConfigCacheTtl);

            return Ok(result);
        }

        // Lấy unique enemy_id từ enemy_spawns, sau đó query bảng enemy lấy
        // base_hp, base_damage, element_type, skills_json, reward_json cho từng loại quái.
        // reward_json được parse và flatten (drop_chance → rate) để client dùng trực tiếp.
        private async Task<object[]> BuildEnemySkillsResponseAsync(IEnumerable<int> enemyIds)
        {
            int[] ids = enemyIds.Where(id => id > 0).Distinct().ToArray();
            if (ids.Length == 0) return Array.Empty<object>();

            var rows = await _db.Enemies
                .AsNoTracking()
                .Where(e => ids.Contains(e.EnemyId))
                .OrderBy(e => e.EnemyId)
                .ToListAsync();

            return rows.Select(e =>
            {
                // Parse drop_items_json → flatten drops
                var dropsList = new List<object>();
                if (!string.IsNullOrWhiteSpace(e.DropItemsJson))
                {
                    try
                    {
                        using var rdoc = JsonDocument.Parse(e.DropItemsJson);
                        if (rdoc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var d in rdoc.RootElement.EnumerateArray())
                            {
                                int    item_id = GetIntValueOrDefault(d, "item_id", 0);
                                double rate    = GetDoubleValueOrDefault(d, "drop_chance", 0);
                                int    qty_min = GetIntValueOrDefault(d, "qty_min", 1);
                                int    qty_max = GetIntValueOrDefault(d, "qty_max", qty_min);
                                if (item_id > 0)
                                    dropsList.Add(new { item_id, rate, qty_min, qty_max });
                            }
                        }
                    }
                    catch (JsonException) { }
                }

                return (object)new
                {
                    enemy_id     = e.EnemyId,
                    enemy_name   = e.EnemyName,
                    base_hp      = e.BaseHp,
                    base_damage  = e.BaseDamage,
                    element_type = e.ElementType ?? "None",
                    exp_reward   = e.ExpReward,
                    gold_reward  = e.GoldReward,
                    silver_reward= e.SilverReward,
                    drops        = dropsList,
                    skills       = ParseJsonOrEmpty(e.SkillsJson)
                };
            }).ToArray();
        }

        private static object ParseJsonOrEmpty(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<object>();
            try { return JsonSerializer.Deserialize<object>(json) ?? Array.Empty<object>(); }
            catch (JsonException) { return Array.Empty<object>(); }
        }

        private static int NormalizeRequiredLevel(int? requiredLevel) =>
            requiredLevel.HasValue && requiredLevel.Value > 1 ? requiredLevel.Value : 1;

        private static int? NormalizeRequiredQuestId(int? requiredQuestId) =>
            requiredQuestId.HasValue && requiredQuestId.Value > 0 ? requiredQuestId.Value : null;

        private static IEnumerable<int> BuildRequiredQuestIds(params int?[] questIds) =>
            questIds.Where(id => id.HasValue && id.Value > 0)
                    .Select(id => id!.Value)
                    .Distinct();

        private static int GetIntValueOrDefault(JsonElement element, string propertyName, int defaultValue)
        {
            if (!TryGetPropertyValue(element, propertyName, out var property))
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
            if (!TryGetPropertyValue(element, propertyName, out var property))
                return defaultValue;

            return property.ValueKind switch
            {
                JsonValueKind.Number when property.TryGetSingle(out float numberValue) => numberValue,
                JsonValueKind.String when float.TryParse(property.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float stringValue) => stringValue,
                _ => defaultValue
            };
        }

        private static double GetDoubleValueOrDefault(JsonElement element, string propertyName, double defaultValue)
        {
            if (!TryGetPropertyValue(element, propertyName, out var property))
                return defaultValue;

            return property.ValueKind switch
            {
                JsonValueKind.Number when property.TryGetDouble(out double numberValue) => numberValue,
                JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double stringValue) => stringValue,
                _ => defaultValue
            };
        }

        // PUT /api/map/{mapId}/spawn-config
        // Cập nhật cấu hình spawn JSON cho map (admin/tool use).
        // Body: { spawn_json: "[{enemy_id, cx, cy, is_boss, count, respawn_time, level}...]" }
        // Drop config chỉnh trong enemy.reward_json, không cần gửi kèm đây.
        [HttpPut("{mapId}/spawn-config")]
        public async Task<IActionResult> UpsertSpawnConfig(int mapId,
            [FromBody] SpawnConfigUpsertRequest req)
        {
            if (!TryParseSpawnEntries(req.SpawnJson, out var spawnEntries))
                return BadRequest(new { message = "spawn_json không hợp lệ." });

            var mapExists = await _db.MapConfigs.AnyAsync(m => m.MapId == mapId);
            if (!mapExists)
                return NotFound(new { message = $"Map {mapId} không tồn tại trong map_config." });

            int[] enemyIds = spawnEntries
                .Where(entry => entry.EnemyId > 0)
                .Select(entry => entry.EnemyId)
                .Distinct()
                .ToArray();

            int existingEnemyCount = await _db.Enemies
                .CountAsync(enemy => enemyIds.Contains(enemy.EnemyId));

            if (existingEnemyCount != enemyIds.Length)
                return BadRequest(new { message = "spawn_json chứa enemy_id không tồn tại trong bảng enemy." });

            await using var transaction = await _db.Database.BeginTransactionAsync();

            var existingSpawns = await _db.EnemySpawns
                .Where(spawn => spawn.MapId == mapId)
                .ToListAsync();

            if (existingSpawns.Count > 0)
                _db.EnemySpawns.RemoveRange(existingSpawns);

            foreach (var entry in spawnEntries)
            {
                if (entry.EnemyId <= 0)
                    continue;

                _db.EnemySpawns.Add(new EnemySpawn
                {
                    MapId         = mapId,
                    EnemyTypeId   = entry.EnemyId,
                    SpawnX        = entry.Cx,
                    SpawnY        = entry.Cy,
                    MaxSpawnCount = entry.Count > 0 ? entry.Count : 1,
                    RespawnTime   = entry.RespawnTime > 0 ? entry.RespawnTime : 30,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            await UpsertLegacyMapSpawnConfigAsync(mapId, spawnEntries);
            await transaction.CommitAsync();
            _cache.Remove($"spawn_config_{mapId}");

            return Ok(new { success = true, map_id = mapId, spawn_count = spawnEntries.Count });
        }

        private async Task UpsertLegacyMapSpawnConfigAsync(int mapId, IReadOnlyList<SpawnConfigPersistEntry> spawnEntries)
        {
            string canonicalSpawnJson = BuildLegacyMapSpawnJson(spawnEntries);

            int updatedRows = await _db.Database.ExecuteSqlRawAsync(
                "UPDATE map_spawn_config SET spawn_json = {0}, updated_at = UTC_TIMESTAMP() WHERE map_id = {1}",
                canonicalSpawnJson,
                mapId);

            if (updatedRows > 0)
                return;

            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO map_spawn_config (map_id, spawn_json, drop_json) VALUES ({0}, {1}, {2})",
                mapId,
                canonicalSpawnJson,
                "[]");
        }

        private static string BuildLegacyMapSpawnJson(IReadOnlyList<SpawnConfigPersistEntry> spawnEntries)
        {
            var payload = new
            {
                spawns = spawnEntries.Select(entry => new
                {
                    enemy_id = entry.EnemyId,
                    hp = Math.Max(0, entry.Hp),
                    exp = Math.Max(0, entry.Exp),
                    x = entry.Cx,
                    y = entry.Cy,
                    is_boss = entry.IsBoss,
                    count = entry.Count > 0 ? entry.Count : 1,
                    respawn_time = Math.Max(0, entry.RespawnTime),
                    level = entry.Level > 0 ? entry.Level : 1
                }).ToArray()
            };

            return JsonSerializer.Serialize(payload);
        }

        private static bool TryParseSpawnEntries(string json, out List<SpawnConfigPersistEntry> spawnEntries)
        {
            spawnEntries = new List<SpawnConfigPersistEntry>();
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                using var document = JsonDocument.Parse(json);
                if (!TryGetSpawnEntriesElement(document.RootElement, out JsonElement entriesElement))
                    return false;

                foreach (JsonElement entryElement in entriesElement.EnumerateArray())
                {
                    int enemyId = GetIntValueByAliasesOrDefault(entryElement, 0, "enemy_id");
                    if (enemyId <= 0)
                        continue;

                    spawnEntries.Add(new SpawnConfigPersistEntry
                    {
                        EnemyId = enemyId,
                        Hp = GetIntValueByAliasesOrDefault(entryElement, 0, "hp", "max_hp", "base_hp"),
                        Exp = GetIntValueByAliasesOrDefault(entryElement, 0, "exp", "exp_reward"),
                        Cx = GetFloatValueByAliasesOrDefault(entryElement, 0f, "cx", "x", "spawn_x"),
                        Cy = GetFloatValueByAliasesOrDefault(entryElement, 0f, "cy", "y", "spawn_y"),
                        IsBoss = GetBoolValueByAliasesOrDefault(entryElement, false, "is_boss", "isBoss"),
                        Count = GetIntValueByAliasesOrDefault(entryElement, 1, "count", "max_spawn_count"),
                        RespawnTime = GetIntValueByAliasesOrDefault(entryElement, 30, "respawn_time"),
                        Level = GetIntValueByAliasesOrDefault(entryElement, 1, "level")
                    });
                }

                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool TryGetSpawnEntriesElement(JsonElement rootElement, out JsonElement spawnEntriesElement)
        {
            if (rootElement.ValueKind == JsonValueKind.Array)
            {
                spawnEntriesElement = rootElement;
                return true;
            }

            if (rootElement.ValueKind == JsonValueKind.Object)
            {
                if (TryGetPropertyValue(rootElement, "spawns", out JsonElement wrappedSpawns)
                    && wrappedSpawns.ValueKind == JsonValueKind.Array)
                {
                    spawnEntriesElement = wrappedSpawns;
                    return true;
                }

                if (TryGetPropertyValue(rootElement, "enemy_spawns", out JsonElement wrappedEnemySpawns)
                    && wrappedEnemySpawns.ValueKind == JsonValueKind.Array)
                {
                    spawnEntriesElement = wrappedEnemySpawns;
                    return true;
                }
            }

            spawnEntriesElement = default;
            return false;
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

        private static bool GetBoolValueOrDefault(JsonElement element, string propertyName, bool defaultValue)
        {
            if (!TryGetPropertyValue(element, propertyName, out var property))
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

        private sealed class SpawnConfigPersistEntry
        {
            public int EnemyId { get; set; }
            public int Hp { get; set; }
            public int Exp { get; set; }
            public float Cx { get; set; }
            public float Cy { get; set; }
            public bool IsBoss { get; set; }
            public int Count { get; set; }
            public int RespawnTime { get; set; }
            public int Level { get; set; }
        }
    }

    public class TravelRequest
    {
        [JsonPropertyName("portal_id")]
        public int PortalId { get; set; }

        [JsonPropertyName("player_id")]
        public int PlayerId { get; set; }

        [JsonPropertyName("current_map_id")]
        public int CurrentMapId { get; set; }

        [JsonPropertyName("player_x")]
        public float PlayerX { get; set; }

        [JsonPropertyName("player_y")]
        public float PlayerY { get; set; }
    }

    public class MapHostRegisterRequest
    {
        [JsonPropertyName("map_id")]   public int    MapId    { get; set; }
        [JsonPropertyName("host_ip")]  public string HostIp   { get; set; } = "";
        [JsonPropertyName("host_port")] public int   HostPort { get; set; }
        [JsonPropertyName("player_id")] public int   PlayerId { get; set; }
    }

    public class MapHostUnregisterRequest
    {
        [JsonPropertyName("map_id")]    public int MapId    { get; set; }
        [JsonPropertyName("player_id")] public int PlayerId { get; set; }
    }

    public class SpawnConfigUpsertRequest
    {
        [JsonPropertyName("spawn_json")] public string SpawnJson { get; set; } = "[]";
    }
}


