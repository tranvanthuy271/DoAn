using System.Security.Claims;
using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.DTOs;
using GameServerApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/leaderboard")]
    [Authorize]
    public class LeaderboardController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly ILogger<LeaderboardController> _logger;

        private const int CacheTtlSeconds = 300; // 5 phút
        private const int TopN = 50;

        private const int CatLevel      = 1;
        private const int CatQuest      = 2;
        private const int CatAttendance = 3;
        private const int CatDungeon    = 4;
        private const int CatGold       = 5;

        public LeaderboardController(GameDbContext db, ILogger<LeaderboardController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /api/leaderboard/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var row = await _db.LeaderboardCaches.FindAsync(id);
            if (row == null) return NotFound($"Không tìm thấy danh mục BXH id={id}.");

            bool stale = row.ListJson == "[]" ||
                         (DateTime.UtcNow - row.UpdatedAt).TotalSeconds > CacheTtlSeconds;
            if (stale) await RefreshAllAsync();

            await _db.Entry(row).ReloadAsync();
            return Ok(new { id = row.Id, name = row.Name, list = row.ListJson });
        }

        // GET /api/leaderboard/all
        [HttpGet("all")]
        public async Task<IActionResult> GetAll()
        {
            var rows = await _db.LeaderboardCaches.OrderBy(r => r.Id).ToListAsync();
            bool anyStale = rows.Count == 0 || rows.Any(r =>
                r.ListJson == "[]" ||
                (DateTime.UtcNow - r.UpdatedAt).TotalSeconds > CacheTtlSeconds);

            if (anyStale)
            {
                await RefreshAllAsync();
                rows = await _db.LeaderboardCaches.OrderBy(r => r.Id).ToListAsync();
            }

            return Ok(rows.Select(r => new { id = r.Id, name = r.Name, list = r.ListJson }));
        }

        // POST /api/leaderboard/refresh (Admin only)
        [HttpPost("refresh")]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public async Task<IActionResult> Refresh()
        {
            await RefreshAllAsync();
            return Ok(new { message = "Bảng xếp hạng đã được cập nhật." });
        }

        // Core refresh: tính rankings từ player_data.info_char
        public async Task RefreshAllAsync()
        {
            try
            {
                var players = await _db.PlayerData
                    .AsNoTracking()
                    .Select(p => new { p.CharacterName, p.InfoCharJson })
                    .ToListAsync();

                var parsed = players.Select(p =>
                {
                    var ic = SafeParseInfoChar(p.InfoCharJson);
                    int bestWave = ic.DungeonBestWaves?.Count > 0
                        ? ic.DungeonBestWaves.Values.Max()
                        : 0;
                    return new
                    {
                        Name        = p.CharacterName ?? "?",
                        Level       = ic.Level,
                        Gold        = (long)ic.Gold,
                        QuestCount  = (long)ic.QuestCompletedCount,
                        AttendCount = (long)ic.AttendanceCount,
                        BestWave    = bestWave,
                    };
                }).ToList();

                var lists = new (int Id, IEnumerable<(string Name, long Value, string Extra)> Entries)[]
                {
                    (CatLevel,
                        parsed.OrderByDescending(p => p.Level).Take(TopN)
                              .Select(p => (p.Name, (long)p.Level, $"Cấp {p.Level}"))),

                    (CatQuest,
                        parsed.OrderByDescending(p => p.QuestCount).Take(TopN)
                              .Select(p => (p.Name, p.QuestCount, $"{p.QuestCount} nhiệm vụ"))),

                    (CatAttendance,
                        parsed.OrderByDescending(p => p.AttendCount).Take(TopN)
                              .Select(p => (p.Name, p.AttendCount, $"{p.AttendCount} ngày"))),

                    (CatDungeon,
                        parsed.OrderByDescending(p => p.BestWave).Take(TopN)
                              .Select(p => (p.Name, (long)p.BestWave, $"Wave {p.BestWave}"))),

                    (CatGold,
                        parsed.OrderByDescending(p => p.Gold).Take(TopN)
                              .Select(p => (p.Name, p.Gold, $"{FmtNum(p.Gold)} vàng"))),
                };

                foreach (var (catId, entries) in lists)
                {
                    var ranked = entries.Select((e, i) => new LeaderboardEntryDto
                    {
                        Rank          = i + 1,
                        CharacterName = e.Name,
                        Value         = e.Value,
                        Extra         = e.Extra
                    }).ToList();

                    var cacheRow = await _db.LeaderboardCaches.FindAsync(catId);
                    if (cacheRow == null) continue;

                    cacheRow.ListJson  = JsonSerializer.Serialize(ranked);
                    cacheRow.UpdatedAt = DateTime.UtcNow;
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("[Leaderboard] Refresh hoàn tất ({Count} players).", players.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Leaderboard] Lỗi khi refresh.");
            }
        }

        private static InfoChar SafeParseInfoChar(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new InfoChar();
            try
            {
                return JsonSerializer.Deserialize<InfoChar>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new InfoChar();
            }
            catch { return new InfoChar(); }
        }

        private static string FmtNum(long v)
        {
            if (v >= 1_000_000_000L) return $"{v / 1_000_000_000.0:0.##}B";
            if (v >= 1_000_000L)     return $"{v / 1_000_000.0:0.##}M";
            if (v >= 1_000L)         return $"{v / 1_000.0:0.##}K";
            return v.ToString();
        }
    }
}
