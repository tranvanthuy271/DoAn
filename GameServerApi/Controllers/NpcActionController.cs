using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GameServerApi.Controllers
{
    /// <summary>
    /// NPC Action API — xử lý các chức năng NPC đặc biệt (tẩy tiềm năng, tẩy kỹ năng, v.v.)
    ///
    /// Tất cả endpoint đều yêu cầu JWT [Authorize].
    /// Chỉ có Unity Server mới gọi các endpoint này (không phải client trực tiếp).
    ///
    /// Route: POST /api/npc/action/{action}
    /// Body:  { "playerId": 1, "npcId": 3 }
    /// Response: { "success": bool, "message": string, "playerData": { gold, silver, skillPoints, potentialPoints, level } }
    /// </summary>
    [ApiController]
    [Route("api/npc/action")]
    [Authorize]
    public class NpcActionController : ControllerBase
    {
        private readonly GameDbContext _db;

        // ── Chi phí cho từng chức năng (bạc hoặc item) ─────────────────────
        // Điều chỉnh ở đây nếu muốn thay đổi cost, KHÔNG cần sửa Unity client.
        private const int CostResetPotential = 250_000;  // bạc
        private const int CostResetSkill     = 250_000;  // bạc
        private const int CostLearnSkill     = 100_000;  // bạc  (+ có thể kiểm tra item skill book sau)
        private const int CostExchangeSkill  = 500_000;  // bạc
        private const int CostExchangeCharm  = 300_000;  // bạc
        private const int CostLockLevel      = 250_000;  // bạc

        public NpcActionController(GameDbContext db) => _db = db;

        // ══════════════════════════════════════════════════════════════════════
        //  POST /api/npc/action/reset-potential
        //  Tẩy toàn bộ điểm tiềm năng của nhân vật → trả lại potentialPoints
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("reset-potential")]
        public async Task<IActionResult> ResetPotential([FromBody] NpcActionRequest req)
        {
            var (player, error) = await ResolvePlayer(req);
            if (error != null) return error;

            var info = player!.GetInfoChar();

            if (info.Silver < CostResetPotential)
                return Ok(Fail($"Không đủ bạc. Cần {CostResetPotential:N0} bạc."));

            // Số điểm tiềm năng đã phân bổ = PotentialPoints ban đầu theo level (5 * level) - điểm còn lại
            // Đơn giản: reset về PotentialPoints = MaxPotentialPoints(level), trả lại tất cả điểm đã dùng
            int maxPoints        = info.Level * 5;
            info.Silver         -= CostResetPotential;
            info.PotentialPoints = maxPoints;

            // TODO: xoá các chỉ số potential stats trong PotentialStatsJson nếu có
            // player.PotentialStatsJson = "{}";

            player.SetInfoChar(info);
            player.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(Success("Đã tẩy tiềm năng thành công! Bạn nhận lại tất cả điểm tiềm năng.", info));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /api/npc/action/reset-skill
        //  Tẩy toàn bộ bí kíp kỹ năng đã học → trả lại skillPoints
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("reset-skill")]
        public async Task<IActionResult> ResetSkill([FromBody] NpcActionRequest req)
        {
            var (player, error) = await ResolvePlayer(req);
            if (error != null) return error;

            var info = player!.GetInfoChar();

            if (info.Silver < CostResetSkill)
                return Ok(Fail($"Không đủ bạc. Cần {CostResetSkill:N0} bạc."));

            // Số skillPoints đã dùng = đọc từ SkillsJson
            int usedPoints = CountSkillPointsUsed(player.SkillsJson);
            int maxPoints  = info.Level * 3;

            info.Silver     -= CostResetSkill;
            info.SkillPoints = maxPoints;

            // Xoá tất cả kỹ năng đã học
            player.SkillsJson = "[]";

            player.SetInfoChar(info);
            player.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(Success($"Đã tẩy bí kíp thành công! Bạn nhận lại {maxPoints} điểm kỹ năng.", info));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /api/npc/action/learn-skill
        //  Học bí kíp — dùng 1 skillPoint, phải có cấp phù hợp
        //  Body mở rộng: { "playerId": 1, "npcId": 8, "skillId": 101 }
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("learn-skill")]
        public async Task<IActionResult> LearnSkill([FromBody] JsonElement bodyRaw)
        {
            if (!bodyRaw.TryGetProperty("playerId", out var pidProp) ||
                !bodyRaw.TryGetProperty("skillId",  out var sidProp))
                return BadRequest("Thiếu playerId hoặc skillId.");

            int playerId = pidProp.GetInt32();
            int skillId  = sidProp.GetInt32();

            // Đảm bảo chỉ playerId từ JWT mới được xử lý
            string? claimId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (int.TryParse(claimId, out int jwtId) && jwtId > 0)
                playerId = jwtId;

            var player = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();

            if (info.SkillPoints <= 0)
                return Ok(Fail("Không đủ điểm kỹ năng."));

            if (info.Silver < CostLearnSkill)
                return Ok(Fail($"Không đủ bạc. Cần {CostLearnSkill:N0} bạc."));

            // Kiểm tra đã học chưa
            var skills = ParseJsonArray(player.SkillsJson);
            if (skills.Contains(skillId.ToString()))
                return Ok(Fail("Bạn đã học bí kíp này rồi."));

            info.SkillPoints--;
            info.Silver -= CostLearnSkill;
            skills.Add(skillId.ToString());
            player.SkillsJson = System.Text.Json.JsonSerializer.Serialize(skills);

            player.SetInfoChar(info);
            player.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(Success($"Đã học bí kíp thành công!", info));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /api/npc/action/exchange-skill
        //  Đổi bí kíp — đổi 1 kỹ năng lấy 1 kỹ năng khác + trả bạc
        //  Body: { "playerId": 1, "npcId": 8, "oldSkillId": 101, "newSkillId": 102 }
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("exchange-skill")]
        public async Task<IActionResult> ExchangeSkill([FromBody] JsonElement bodyRaw)
        {
            if (!bodyRaw.TryGetProperty("playerId",   out var pidProp)  ||
                !bodyRaw.TryGetProperty("oldSkillId", out var oldProp) ||
                !bodyRaw.TryGetProperty("newSkillId", out var newProp))
                return BadRequest("Thiếu playerId, oldSkillId hoặc newSkillId.");

            int playerId  = pidProp.GetInt32();
            int oldSkillId = oldProp.GetInt32();
            int newSkillId = newProp.GetInt32();

            string? claimId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (int.TryParse(claimId, out int jwtId) && jwtId > 0)
                playerId = jwtId;

            var player = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();

            if (info.Silver < CostExchangeSkill)
                return Ok(Fail($"Không đủ bạc. Cần {CostExchangeSkill:N0} bạc."));

            var skills = ParseJsonArray(player.SkillsJson);
            if (!skills.Contains(oldSkillId.ToString()))
                return Ok(Fail("Bạn không có bí kíp cần đổi."));
            if (skills.Contains(newSkillId.ToString()))
                return Ok(Fail("Bạn đã có bí kíp muốn đổi lấy rồi."));

            skills.Remove(oldSkillId.ToString());
            skills.Add(newSkillId.ToString());
            player.SkillsJson = System.Text.Json.JsonSerializer.Serialize(skills);
            info.Silver      -= CostExchangeSkill;

            player.SetInfoChar(info);
            player.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(Success("Đã đổi bí kíp thành công!", info));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /api/npc/action/exchange-charm
        //  Đổi bùa nổ — trả bạc
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("exchange-charm")]
        public async Task<IActionResult> ExchangeCharm([FromBody] NpcActionRequest req)
        {
            var (player, error) = await ResolvePlayer(req);
            if (error != null) return error;

            var info = player!.GetInfoChar();

            if (info.Silver < CostExchangeCharm)
                return Ok(Fail($"Không đủ bạc. Cần {CostExchangeCharm:N0} bạc."));

            // TODO: thêm logic đổi bùa nổ cụ thể (kiểm tra item trong inventory)
            info.Silver -= CostExchangeCharm;

            player.SetInfoChar(info);
            player.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            return Ok(Success("Đã đổi bùa nổ thành công!", info));
        }

        // ══════════════════════════════════════════════════════════════════════
        //  POST /api/npc/action/lock-level
        //  Khoá / mở cấp nhân vật — trả bạc
        // ══════════════════════════════════════════════════════════════════════
        [HttpPost("lock-level")]
        public async Task<IActionResult> LockLevel([FromBody] NpcActionRequest req)
        {
            var (player, error) = await ResolvePlayer(req);
            if (error != null) return error;

            var info = player!.GetInfoChar();

            if (info.Silver < CostLockLevel)
                return Ok(Fail($"Không đủ bạc. Cần {CostLockLevel:N0} bạc."));

            bool isLocked = info.IsLevelLocked;
            info.IsLevelLocked = !isLocked;
            info.Silver       -= CostLockLevel;

            player.SetInfoChar(info);
            player.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            string msg = info.IsLevelLocked
                ? "Đã khoá cấp nhân vật. Bạn sẽ không lên cấp khi nhận kinh nghiệm."
                : "Đã mở khoá cấp nhân vật. Bạn có thể lên cấp bình thường.";
            return Ok(Success(msg, info));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private async Task<(PlayerData? player, IActionResult? error)> ResolvePlayer(NpcActionRequest req)
        {
            if (req == null || req.PlayerId <= 0)
                return (null, BadRequest("Thiếu playerId."));

            int playerId = req.PlayerId;

            // Đảm bảo chỉ chủ nhân JWT mới được thực thi
            string? claimId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (int.TryParse(claimId, out int jwtId) && jwtId > 0)
                playerId = jwtId;

            var player = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (player == null)
                return (null, NotFound("Player không tồn tại."));

            return (player, null);
        }

        private static object Success(string message, InfoChar info) => new
        {
            success = true,
            message,
            playerData = new
            {
                gold           = info.Gold,
                silver         = info.Silver,
                skillPoints    = info.SkillPoints,
                potentialPoints= info.PotentialPoints,
                level          = info.Level,
            }
        };

        private static object Fail(string message) => new
        {
            success = false,
            message,
            playerData = (object?)null,
        };

        private static int CountSkillPointsUsed(string skillsJson)
        {
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<string[]>(skillsJson);
                return arr?.Length ?? 0;
            }
            catch { return 0; }
        }

        private static List<string> ParseJsonArray(string json)
        {
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return new List<string>();
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch { return new List<string>(); }
        }
    }

    // ── Request DTO ────────────────────────────────────────────────────────────
    public class NpcActionRequest
    {
        public int PlayerId { get; set; }
        public int NpcId    { get; set; }
    }
}
