using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GameServerApi.Controllers
{
    // ═══════════════════════════════════════════════════════════════════════
    //  In-memory registry: track which player is hosting each world map.
    //  Mỗi map chỉ có 1 host tại một thời điểm.
    //  Entry tự hết hạn sau HostTimeoutSeconds giây (phòng host crash không unregister).
    // ═══════════════════════════════════════════════════════════════════════
    internal static class MapHostRegistry
    {
        private record HostEntry(string Ip, ushort Port, int PlayerId, DateTime RegisteredAt);

        private static readonly ConcurrentDictionary<int, HostEntry> _registry = new();
        private const int HostTimeoutSeconds = 120; // host có 120s không heartbeat → bị xoá

        /// <summary>Lấy thông tin host hiện tại của map (null nếu không có hoặc đã hết hạn).</summary>
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

        /// <summary>
        /// Thử đăng ký làm host cho map.
        /// - Nếu chưa có host (hoặc entry đã hết hạn): đăng ký thành công → youAreHost=true.
        /// - Nếu đã có host: trả về thông tin host hiện tại → youAreHost=false.
        /// </summary>
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

        /// <summary>Huỷ đăng ký host. Chỉ thành công nếu player_id khớp với host hiện tại.</summary>
        public static bool Unregister(int mapId, int playerId)
        {
            if (_registry.TryGetValue(mapId, out var entry) && entry.PlayerId == playerId)
                return _registry.TryRemove(mapId, out _);
            return false;
        }

        /// <summary>Heartbeat: reset timer để tránh hết hạn. Gọi mỗi ~30s từ client.</summary>
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

        // Thời gian cache spawn-config — tránh gọi DB liên tục mỗi lần host join map
        private static readonly TimeSpan SpawnConfigCacheTtl = TimeSpan.FromMinutes(5);

        public MapController(GameDbContext db, IMemoryCache cache)
        {
            _db    = db;
            _cache = cache;
        }

        /// <summary>
        /// GET /api/map/{mapId}/config
        /// Láº¥y thÃ´ng tin cáº¥u hÃ¬nh map (spawn points, scene name, level range)
        /// </summary>
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
                    max_level      = mapConfig.MaxLevel
                });
            }
            catch (JsonException)
            {
                return Ok(new { map_id = mapId, map_name = mapConfig.MapName, scene_name = mapConfig.SceneName, spawn_points = new[] { new { x = 0f, y = 0f } } });
            }
        }

        /// <summary>
        /// GET /api/map/{mapId}/portals
        /// Láº¥y danh sÃ¡ch cÃ¡c cá»•ng dá»‹ch chuyá»ƒn trÃªn map nÃ y
        /// Client dÃ¹ng Ä‘á»ƒ spawn MapPortalTrigger Ä‘Ãºng vá»‹ trÃ­
        /// </summary>
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
                    dungeon_id       = p.DungeonId
                })
            });
        }

        /// <summary>
        /// POST /api/map/travel
        /// Server validate vÃ  cáº¥p phÃ©p dá»‹ch chuyá»ƒn.
        /// Client gá»i khi player cháº¡m trigger zone cá»§a portal.
        /// Body: { portal_id, player_id, current_map_id, player_x, player_y }
        /// </summary>
        [HttpPost("travel")]
        public async Task<IActionResult> TravelPortal([FromBody] TravelRequest req)
        {
            var portal = await _db.MapPortals.FindAsync(req.PortalId);
            if (portal == null || !portal.IsActive)
                return BadRequest(new { success = false, message = "Cá»•ng dá»‹ch chuyá»ƒn khÃ´ng tá»“n táº¡i hoáº·c Ä‘Ã£ bá»‹ khoÃ¡." });

            // Validate player Ä‘ang á»Ÿ Ä‘Ãºng source map
            if (portal.SourceMapId != req.CurrentMapId)
                return BadRequest(new { success = false, message = "Vá»‹ trÃ­ khÃ´ng há»£p lá»‡." });

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

            // Kiá»ƒm tra item cáº§n thiáº¿t (náº¿u cÃ³)
            if (portal.RequiredItemId.HasValue)
            {
                var player = await _db.PlayerData.FindAsync(req.PlayerId);
                if (player == null)
                    return BadRequest(new { success = false, message = "Player khÃ´ng tá»“n táº¡i." });

                // Kiá»ƒm tra inventory JSON cÃ³ chá»©a required_item_id khÃ´ng
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
                    return BadRequest(new { success = false, message = $"Cáº§n cÃ³ ChÃ¬a KhÃ³a (item #{portal.RequiredItemId}) Ä‘á»ƒ vÃ o Ä‘Ã¢y." });
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

        /// <summary>
        /// GET /api/map/by-scene?scene=GameScene
        /// Tìm map_config theo scene_name (dùng cho MapManager.cs trên client).
        /// </summary>
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

        /// <summary>
        /// GET /api/map/portal/direction?mapId=1&amp;direction=right
        /// Lấy portal trái hoặc phải của map (dùng cho MapTransitionButton.cs).
        /// portal_direction trong DB được set trước khi INSERT (left | right | none).
        /// </summary>
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
                dest_y          = portal.DestY
            });
        }

        // ── Host Registry endpoints ──────────────────────────────────────────

        /// <summary>
        /// GET /api/map/host/check?mapId=1
        /// Kiểm tra có host nào đang chạy cho map này không.
        /// Response: { has_host, host_ip, host_port, player_id }
        /// </summary>
        [HttpGet("host/check")]
        public IActionResult CheckHost([FromQuery] int mapId)
        {
            var (hasHost, ip, port, playerId) = MapHostRegistry.Check(mapId);
            return Ok(new { has_host = hasHost, host_ip = ip, host_port = (int)port, player_id = playerId });
        }

        /// <summary>
        /// POST /api/map/host/register
        /// Đăng ký làm host cho map (atomic: race-safe).
        /// - Nếu chưa có host: đăng ký thành công → { you_are_host: true, host_ip, host_port }
        /// - Nếu đã có host: → { you_are_host: false, host_ip: &lt;existing>, host_port: &lt;existing> }
        /// Body: { map_id, host_ip, host_port, player_id }
        /// </summary>
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

        /// <summary>
        /// POST /api/map/host/unregister
        /// Huỷ đăng ký host khi player rời map.
        /// Chỉ thành công nếu player_id khớp với host hiện tại.
        /// Body: { map_id, player_id }
        /// </summary>
        [HttpPost("host/unregister")]
        public IActionResult UnregisterHost([FromBody] MapHostUnregisterRequest req)
        {
            bool removed = MapHostRegistry.Unregister(req.MapId, req.PlayerId);
            return Ok(new { success = true, removed });
        }

        /// <summary>
        /// POST /api/map/host/heartbeat
        /// Reset timer của host entry để tránh hết hạn (120s timeout).
        /// Gọi mỗi ~30s từ host client.
        /// Body: { map_id, player_id }
        /// </summary>
        [HttpPost("host/heartbeat")]
        public IActionResult HostHeartbeat([FromBody] MapHostUnregisterRequest req)
        {
            MapHostRegistry.Heartbeat(req.MapId, req.PlayerId);
            return Ok(new { success = true });
        }

        // ──────────────────────────────────────────────────────────────────
        //  Spawn Config — JSON-based enemy spawn + drop configuration
        // ──────────────────────────────────────────────────────────────────

        /// <summary>
        /// GET /api/map/{mapId}/spawn-config
        /// Lấy cấu hình spawn enemy và tỉ lệ drop item cho map.
        /// Unity host gọi endpoint này khi scene load để fetch toàn bộ spawn data.
        ///
        /// Response:
        /// {
        ///   "map_id": 0,
        ///   "spawns": [{enemy_id, hp, exp, cx, cy, is_boss, count, respawn_time}, ...],
        ///   "drops":  [{enemy_id, items:[{item_id, rate, qty_min, qty_max}]}, ...]
        /// }
        ///
        /// Nếu chưa có config → trả về spawns:[], drops:[] (không lỗi).
        /// Host có thể fallback sang endpoint /enemyspawn/{mapId}/spawns cũ.
        /// </summary>
        [HttpGet("{mapId}/spawn-config")]
        public async Task<IActionResult> GetSpawnConfig(int mapId)
        {
            // —— Hit cache trước ——
            string cacheKey = $"spawn_config_{mapId}";
            if (_cache.TryGetValue(cacheKey, out object? cached))
                return Ok(cached);

            var config = await _db.MapSpawnConfigs
                .FirstOrDefaultAsync(c => c.MapId == mapId);

            if (config == null)
            {
                var empty = new
                {
                    map_id       = mapId,
                    spawns       = Array.Empty<object>(),
                    drops        = Array.Empty<object>(),
                    enemy_skills = Array.Empty<object>()
                };
                // Cache empty result ngắn hơn (30 giây) để tự refresh khi admin thêm data
                _cache.Set(cacheKey, empty, TimeSpan.FromSeconds(30));
                return Ok(empty);
            }

            // Deserialize JSONs để trả về đã parse (tránh double-encode string)
            object spawnsObj;
            object dropsObj;
            try { spawnsObj = JsonSerializer.Deserialize<object>(config.SpawnJson) ?? Array.Empty<object>(); }
            catch (JsonException) { spawnsObj = Array.Empty<object>(); }

            try { dropsObj = JsonSerializer.Deserialize<object>(config.DropJson) ?? Array.Empty<object>(); }
            catch (JsonException) { dropsObj = Array.Empty<object>(); }

            // Lấy skills từ bảng enemy cho tất cả enemy_id có mặt trong spawn_json.
            var enemySkillsObj = await BuildEnemySkillsResponseAsync(config.SpawnJson);

            var result = new
            {
                map_id       = config.MapId,
                spawns       = spawnsObj,
                drops        = dropsObj,
                enemy_skills = enemySkillsObj
            };

            // Lưu vào cache
            _cache.Set(cacheKey, result, SpawnConfigCacheTtl);

            return Ok(result);
        }

        /// <summary>
        /// Parse spawn_json để lấy unique enemy_id, sau đó query bảng enemy lấy
        /// base_damage, element_type, skills_json cho từng loại quái.
        /// </summary>
        private async Task<object[]> BuildEnemySkillsResponseAsync(string spawnJson)
        {
            // Lấy unique enemy_ids từ spawn_json
            var enemyIds = new HashSet<int>();
            try
            {
                using var doc = JsonDocument.Parse(spawnJson);
                foreach (var elem in doc.RootElement.EnumerateArray())
                {
                    if (elem.TryGetProperty("enemy_id", out var idProp)
                        && idProp.TryGetInt32(out int eid) && eid > 0)
                    {
                        enemyIds.Add(eid);
                    }
                }
            }
            catch (JsonException) { return Array.Empty<object>(); }

            if (enemyIds.Count == 0) return Array.Empty<object>();

            var rows = await _db.Enemies
                .Where(e => enemyIds.Contains(e.EnemyId))
                .ToListAsync();

            return rows.Select(e => (object)new
            {
                enemy_id     = e.EnemyId,
                enemy_name   = e.EnemyName,   // ← thêm để client hiển thị đúng tên
                base_damage  = e.BaseDamage,
                element_type = e.ElementType ?? "None",
                // skills_json được parse sẵn để client không cần double-decode
                skills       = ParseJsonOrEmpty(e.SkillsJson)
            }).ToArray();
        }

        private static object ParseJsonOrEmpty(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return Array.Empty<object>();
            try { return JsonSerializer.Deserialize<object>(json) ?? Array.Empty<object>(); }
            catch (JsonException) { return Array.Empty<object>(); }
        }

        /// <summary>
        /// PUT /api/map/{mapId}/spawn-config
        /// Cập nhật cấu hình spawn JSON cho map (admin/tool use).
        /// Body: { spawn_json: "...", drop_json: "..." }
        /// </summary>
        [HttpPut("{mapId}/spawn-config")]
        public async Task<IActionResult> UpsertSpawnConfig(int mapId,
            [FromBody] SpawnConfigUpsertRequest req)
        {
            // Validate JSON strings trước khi lưu
            if (!IsValidJson(req.SpawnJson))
                return BadRequest(new { message = "spawn_json không hợp lệ." });
            if (!IsValidJson(req.DropJson))
                return BadRequest(new { message = "drop_json không hợp lệ." });

            var mapExists = await _db.MapConfigs.AnyAsync(m => m.MapId == mapId);
            if (!mapExists)
                return NotFound(new { message = $"Map {mapId} không tồn tại trong map_config." });

            var existing = await _db.MapSpawnConfigs
                .FirstOrDefaultAsync(c => c.MapId == mapId);

            if (existing == null)
            {
                _db.MapSpawnConfigs.Add(new Models.Entities.MapSpawnConfig
                {
                    MapId     = mapId,
                    SpawnJson = req.SpawnJson,
                    DropJson  = req.DropJson,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                existing.SpawnJson = req.SpawnJson;
                existing.DropJson  = req.DropJson;
                existing.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            // Xóa cache để lần đọc tiếp theo lấy data mới từ DB
            _cache.Remove($"spawn_config_{mapId}");

            return Ok(new { success = true, map_id = mapId });
        }

        private static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;
            try { JsonSerializer.Deserialize<object>(json); return true; }
            catch (JsonException) { return false; }
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
        [JsonPropertyName("drop_json")]  public string DropJson  { get; set; } = "[]";
    }
}

