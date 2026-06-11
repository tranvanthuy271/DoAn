using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    // Admin management API — yêu cầu JWT với role "Admin".
    // Tất cả endpoint ở đây đều được bảo vệ bằng [Authorize(Roles = "Admin")].
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly ILogger<AdminController> _logger;

        public AdminController(GameDbContext db, ILogger<AdminController> logger)
        {
            _db     = db;
            _logger = logger;
        }

        // GET /api/admin/overview
        // Thống kê tổng quan hệ thống cho dashboard.
        [HttpGet("overview")]
        public async Task<IActionResult> Overview()
        {
            var playerCount              = await _db.Users.CountAsync(u => u.Role == "Player");
            var activeDungeonCount       = await _db.DungeonConfigs.CountAsync(d => d.IsActive);
            var mapCount                 = await _db.MapConfigs.CountAsync();
            var leaderboardCategoryCount = await _db.LeaderboardCaches.CountAsync();
            var zoneServerCount          = ZoneServerRegistry.GetAll().Count;

            return Ok(new
            {
                playerCount,
                zoneServerCount,
                activeDungeonCount,
                mapCount,
                leaderboardCategoryCount
            });
        }

        // GET /api/admin/zone-servers
        // Danh sách zone server đang hoạt động (in-memory registry).
        [HttpGet("zone-servers")]
        public IActionResult GetZoneServers()
        {
            var servers = ZoneServerRegistry.GetAll().Select(s => new
            {
                ip            = s.Ip,
                port          = s.Port,
                playerCount   = s.PlayerCount,
                mapCount      = s.MapCount,
                zoneCount     = s.ZoneStats.Count,
                registeredAt  = s.RegisteredAtUtc,
                lastHeartbeat = s.LastHeartbeatUtc
            });
            return Ok(servers);
        }

        // GET /api/admin/maps
        // Danh sách tất cả map config.
        [HttpGet("maps")]
        public async Task<IActionResult> GetMaps()
        {
            var maps = await _db.MapConfigs
                .AsNoTracking()
                .OrderBy(m => m.MapId)
                .Select(m => new
                {
                    mapId          = m.MapId,
                    mapName        = m.MapName,
                    sceneName      = m.SceneName,
                    minLevel       = m.MinLevel,
                    maxLevel       = m.MaxLevel,
                    requiredQuestId = m.RequiredQuestId
                })
                .ToListAsync();
            return Ok(maps);
        }

        // GET /api/admin/maps/{mapId}
        // Chi tiết một map (bao gồm spawnPointsJson).
        [HttpGet("maps/{mapId:int}")]
        public async Task<IActionResult> GetMap(int mapId)
        {
            var m = await _db.MapConfigs.FindAsync(mapId);
            if (m == null) return NotFound(new { message = $"Map {mapId} không tồn tại." });
            return Ok(new
            {
                mapId          = m.MapId,
                mapName        = m.MapName,
                sceneName      = m.SceneName,
                minLevel       = m.MinLevel,
                maxLevel       = m.MaxLevel,
                requiredQuestId = m.RequiredQuestId,
                spawnPointsJson = m.SpawnPointsJson
            });
        }

        // PUT /api/admin/maps/{mapId}
        // Cập nhật cấu hình map (level range, required quest, spawn points).
        [HttpPut("maps/{mapId:int}")]
        public async Task<IActionResult> UpdateMap(int mapId, [FromBody] AdminMapUpdateRequest req)
        {
            var m = await _db.MapConfigs.FindAsync(mapId);
            if (m == null) return NotFound(new { message = $"Map {mapId} không tồn tại." });

            if (req.MinLevel.HasValue) m.MinLevel = req.MinLevel.Value;
            if (req.MaxLevel.HasValue) m.MaxLevel = req.MaxLevel.Value;
            if (req.RequiredQuestId.HasValue) m.RequiredQuestId = req.RequiredQuestId == 0 ? null : req.RequiredQuestId;
            if (req.SpawnPointsJson != null)
            {
                // Validate JSON trước khi lưu
                try { JsonDocument.Parse(req.SpawnPointsJson); }
                catch { return BadRequest(new { message = "spawnPointsJson không hợp lệ." }); }
                m.SpawnPointsJson = req.SpawnPointsJson;
            }

            m.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin updated map {MapId}.", mapId);
            return Ok(new { message = "Đã cập nhật map.", mapId });
        }

        // GET /api/admin/dungeons
        // Danh sách tất cả phó bản (active và inactive).
        [HttpGet("dungeons")]
        public async Task<IActionResult> GetDungeons()
        {
            var dungeons = await _db.DungeonConfigs
                .AsNoTracking()
                .OrderBy(d => d.DungeonId)
                .Select(d => new
                {
                    dungeonId        = d.DungeonId,
                    dungeonName      = d.DungeonName,
                    dungeonType      = d.DungeonType,
                    mapId            = d.MapId,
                    sceneName        = d.SceneName,
                    maxPlayers       = d.MaxPlayers,
                    minLevelRequired = d.MinLevelRequired,
                    timeLimitSeconds = d.TimeLimitSeconds,
                    description      = d.Description,
                    rewardJson       = d.RewardJson,
                    isActive         = d.IsActive
                })
                .ToListAsync();
            return Ok(dungeons);
        }

        // GET /api/admin/dungeons/{id}
        // Chi tiết một phó bản.
        [HttpGet("dungeons/{id:int}")]
        public async Task<IActionResult> GetDungeon(int id)
        {
            var d = await _db.DungeonConfigs.FindAsync(id);
            if (d == null) return NotFound(new { message = $"Dungeon {id} không tồn tại." });
            return Ok(new
            {
                dungeonId        = d.DungeonId,
                dungeonName      = d.DungeonName,
                dungeonType      = d.DungeonType,
                mapId            = d.MapId,
                sceneName        = d.SceneName,
                maxPlayers       = d.MaxPlayers,
                minLevelRequired = d.MinLevelRequired,
                timeLimitSeconds = d.TimeLimitSeconds,
                description      = d.Description,
                rewardJson       = d.RewardJson,
                isActive         = d.IsActive
            });
        }

        // PUT /api/admin/dungeons/{id}
        // Cập nhật cấu hình phó bản.
        [HttpPut("dungeons/{id:int}")]
        public async Task<IActionResult> UpdateDungeon(int id, [FromBody] AdminDungeonUpdateRequest req)
        {
            var d = await _db.DungeonConfigs.FindAsync(id);
            if (d == null) return NotFound(new { message = $"Dungeon {id} không tồn tại." });

            if (req.DungeonName      != null) d.DungeonName      = req.DungeonName;
            if (req.Description      != null) d.Description      = req.Description;
            if (req.MaxPlayers.HasValue)      d.MaxPlayers       = req.MaxPlayers.Value;
            if (req.MinLevelRequired.HasValue) d.MinLevelRequired = req.MinLevelRequired.Value;
            if (req.TimeLimitSeconds.HasValue) d.TimeLimitSeconds = req.TimeLimitSeconds.Value;
            if (req.RewardJson != null)
            {
                try { JsonDocument.Parse(req.RewardJson); }
                catch { return BadRequest(new { message = "rewardJson không hợp lệ." }); }
                d.RewardJson = req.RewardJson;
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin updated dungeon {DungeonId}.", id);
            return Ok(new { message = "Đã cập nhật phó bản.", dungeonId = id });
        }

        // PUT /api/admin/dungeons/{id}/active
        // Bật/tắt phó bản.
        [HttpPut("dungeons/{id:int}/active")]
        public async Task<IActionResult> SetDungeonActive(int id, [FromBody] AdminActiveRequest req)
        {
            var d = await _db.DungeonConfigs.FindAsync(id);
            if (d == null) return NotFound(new { message = $"Dungeon {id} không tồn tại." });

            d.IsActive = req.IsActive;
            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin set dungeon {DungeonId} active={Active}.", id, req.IsActive);
            return Ok(new { message = req.IsActive ? "Phó bản đã bật." : "Phó bản đã tắt.", dungeonId = id, isActive = req.IsActive });
        }

        // GET /api/admin/players
        // Danh sách người chơi (phân trang, tìm kiếm theo username).
        [HttpGet("players")]
        public async Task<IActionResult> GetPlayers(
            [FromQuery] int    page     = 1,
            [FromQuery] int    pageSize = 20,
            [FromQuery] string? search  = null)
        {
            page     = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            IQueryable<User> query = _db.Users
                .AsNoTracking()
                .Where(u => u.Role == "Player");

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(u => u.Username.Contains(search));

            int total = await query.CountAsync();

            var users = await query
                .OrderBy(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Fetch PlayerData cho các user này để lấy level/gold từ InfoChar JSON
            var userIds = users.Select(u => u.UserId).ToList();
            var playerDataMap = await _db.PlayerData
                .AsNoTracking()
                .Where(pd => userIds.Contains(pd.PlayerId))
                .ToDictionaryAsync(pd => pd.PlayerId, pd => pd);

            var items = users.Select(u =>
            {
                int? level = null;
                int? gold  = null;
                if (playerDataMap.TryGetValue(u.UserId, out var pd))
                {
                    try
                    {
                        var info = pd.GetInfoChar();
                        level = info.Level;
                        gold  = info.Gold;
                    }
                    catch { /* ignore deserialization errors */ }
                }
                return new
                {
                    userId    = u.UserId,
                    username  = u.Username,
                    email     = u.Email,
                    createdAt = u.CreatedAt,
                    level,
                    gold
                };
            }).ToList();

            return Ok(new { total, page, pageSize, items });
        }

    }

    // Request DTOs

    public sealed class AdminMapUpdateRequest
    {
        public int?    MinLevel        { get; set; }
        public int?    MaxLevel        { get; set; }
        public int?    RequiredQuestId { get; set; }
        public string? SpawnPointsJson { get; set; }
    }

    public sealed class AdminDungeonUpdateRequest
    {
        public string? DungeonName      { get; set; }
        public string? Description      { get; set; }
        public int?    MaxPlayers       { get; set; }
        public int?    MinLevelRequired { get; set; }
        public int?    TimeLimitSeconds { get; set; }
        public string? RewardJson       { get; set; }
    }

    public sealed class AdminActiveRequest
    {
        public bool IsActive { get; set; }
    }
}
