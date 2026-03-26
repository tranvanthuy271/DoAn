using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MapController : ControllerBase
    {
        private readonly GameDbContext _db;

        public MapController(GameDbContext db)
        {
            _db = db;
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

            // Validate khoáº£ng cÃ¡ch giá»¯a player vÃ  portal (chá»‘ng teleport hack)
            float dx = req.PlayerX - portal.SrcX;
            float dy = req.PlayerY - portal.SrcY;
            float dist = MathF.Sqrt(dx * dx + dy * dy);
            if (dist > portal.SrcRadius * 2f)  // leniency x2 cho Ä‘á»™ trá»… máº¡ng
                return BadRequest(new { success = false, message = "Báº¡n khÃ´ng á»Ÿ gáº§n cá»•ng." });

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
        /// GET /api/map/zone?mapId=1&amp;zoneIndex=2
        /// Lấy room_id của zone (dùng server 1 port duy nhất).
        /// Client không cần reconnect, chỉ cần gửi ServerRpc với room_id này.
        /// </summary>
        [HttpGet("zone")]
        public async Task<IActionResult> GetZoneConfig([FromQuery] int mapId, [FromQuery] int zoneIndex)
        {
            var zone = await _db.MapZoneConfigs
                .FirstOrDefaultAsync(z => z.MapId == mapId && z.ZoneIndex == zoneIndex && z.IsActive);

            if (zone == null)
                return NotFound(new { message = $"Không tìm thấy zone {zoneIndex} trong map {mapId}." });

            return Ok(new
            {
                zone_id   = zone.ZoneId,
                zone_name = zone.ZoneName,
                room_id   = zone.RoomId,
                // Host ngày luôn cố định — tất cả zone dùng cùng 1 NGO server
                host_ip   = zone.HostIp,
                host_port = 7777
            });
        }

        /// <summary>
        /// GET /api/map/portal/direction?mapId=1&amp;direction=right
        /// Lấy portal trái hoặc phải của map (dùng cho MapTransitionButton.cs).
        /// Quy ước: portal bên phải có src_x lớn nhất, bên trái có src_x nhỏ nhất.
        /// </summary>
        [HttpGet("portal/direction")]
        public async Task<IActionResult> GetPortalByDirection(
            [FromQuery] int mapId,
            [FromQuery] string direction)
        {
            if (direction != "left" && direction != "right")
                return BadRequest(new { message = "direction phải là 'left' hoặc 'right'." });

            var portals = await _db.MapPortals
                .Where(p => p.SourceMapId == mapId && p.PortalType == "world_travel" && p.IsActive)
                .ToListAsync();

            if (!portals.Any())
                return NotFound(new { message = $"Map {mapId} không có world_travel portal." });

            var portal = direction == "right"
                ? portals.OrderByDescending(p => p.SrcX).First()
                : portals.OrderBy(p => p.SrcX).First();

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
    }

    public class TravelRequest
    {
        public int PortalId { get; set; }
        public int PlayerId { get; set; }
        public int CurrentMapId { get; set; }
        public float PlayerX { get; set; }
        public float PlayerY { get; set; }
    }
}

