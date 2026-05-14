using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using GameServerApi.Auth;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/quest")]
    [Authorize]
    public class QuestController : ControllerBase
    {
        private readonly GameDbContext _db;

        public QuestController(GameDbContext db) => _db = db;

        // ═════════════════════════════════════════════════════════════════════
        //  GET /api/quest/list?npcId=2
        //  Trả về danh sách quest của NPC đó, kèm trạng thái của player hiện tại.
        //  available  = chưa nhận, đủ cấp
        //  active     = đang làm (đã nhận)
        //  completed  = đã hoàn thành
        //  locked     = chưa đủ cấp
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet("list")]
        public async Task<IActionResult> GetQuestList([FromQuery] int npcId)
        {
            int playerId = GetPlayerId();
            if (playerId <= 0) return Unauthorized();

            // Lấy level player
            var pdata = await _db.PlayerData.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");

            var infoChar = pdata.GetInfoChar();
            int playerLevel = infoChar?.Level ?? 1;

            // Quests của NPC này (giver hoặc receiver)
            var quests = await _db.QuestConfigs.AsNoTracking()
                .Where(q => q.IsActive && (q.NpcGiverId == npcId || q.NpcReceiverId == npcId))
                .OrderBy(q => q.SortOrder)
                .ToListAsync();

            // Trạng thái player cho từng quest
            var questIds = quests.Select(q => q.Id).ToList();
            var playerQuests = await _db.PlayerQuests.AsNoTracking()
                .Where(pq => pq.PlayerId == playerId && questIds.Contains(pq.QuestConfigId))
                .ToDictionaryAsync(pq => pq.QuestConfigId);

            var result = quests.Select(q =>
            {
                playerQuests.TryGetValue(q.Id, out var pq);
                string displayStatus;
                if      (pq == null && playerLevel < q.LevelNeed) displayStatus = "locked";
                else if (pq == null)                               displayStatus = "available";
                else                                               displayStatus = pq.Status;  // active | completed

                return new
                {
                    quest_id             = q.Id,
                    name                 = q.Name,
                    description          = q.Description,
                    level_need           = q.LevelNeed,
                    npc_giver_id         = q.NpcGiverId,
                    npc_receiver_id      = q.NpcReceiverId,
                    steps                = q.Steps,
                    rewards              = q.Reward,
                    status               = displayStatus,
                    current_step_index   = pq?.CurrentStepIndex ?? 0,
                    progress_json        = pq?.ProgressJson ?? "{}",
                };
            }).ToList();

            return Ok(result);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST /api/quest/accept
        //  Body: { "questId": 1 }
        //  Tạo row player_quest với status=active.
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost("accept")]
        public async Task<IActionResult> Accept([FromBody] JsonElement body)
        {
            int playerId = GetPlayerId();
            if (playerId <= 0) return Unauthorized();

            if (!body.TryGetProperty("questId", out var qidProp) || !qidProp.TryGetInt32(out int questId))
                return BadRequest("Thiếu questId.");

            var quest = await _db.QuestConfigs.AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == questId && q.IsActive);
            if (quest == null) return NotFound("Quest không tồn tại.");

            // Kiểm tra level
            var pdata = await _db.PlayerData.AsNoTracking().FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");
            int playerLevel = pdata.GetInfoChar()?.Level ?? 1;
            if (playerLevel < quest.LevelNeed)
                return BadRequest($"Cần cấp {quest.LevelNeed} để nhận nhiệm vụ này.");

            // Chỉ được 1 quest active cùng lúc
            bool hasActive = await _db.PlayerQuests
                .AnyAsync(pq => pq.PlayerId == playerId && pq.Status == "active");
            if (hasActive)
                return BadRequest("Bạn đang có nhiệm vụ chưa hoàn thành. Hoàn thành hoặc bỏ nhiệm vụ hiện tại trước.");

            // Kiểm tra đã nhận chưa
            bool alreadyTaken = await _db.PlayerQuests
                .AnyAsync(pq => pq.PlayerId == playerId && pq.QuestConfigId == questId);
            if (alreadyTaken)
                return BadRequest("Bạn đã nhận nhiệm vụ này trước đó.");

            var pq = new PlayerQuest
            {
                PlayerId       = playerId,
                QuestConfigId  = questId,
                Status         = "active",
                CurrentStepIndex = 0,
                ProgressJson   = "{}",
                AcceptedAt     = DateTime.UtcNow,
            };
            _db.PlayerQuests.Add(pq);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message            = "Đã nhận nhiệm vụ!",
                quest_id           = questId,
                quest_name         = quest.Name,
                current_step_index = 0,
                current_step       = quest.Steps.Count > 0 ? quest.Steps[0] : null,
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST /api/quest/progress
        //  Body: { "playerId": 1, "questId": 1, "stepIndex": 0, "delta": 1 }
        //  Cập nhật bước cụ thể theo questId + stepIndex.
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost("progress")]
        [Authorize(AuthenticationSchemes = ZoneApiKeyAuthenticationHandler.SchemeName)]
        public async Task<IActionResult> UpdateProgress([FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("playerId",  out var pidProp)  || !pidProp.TryGetInt32(out int playerId))
                return BadRequest("Thiếu playerId.");
            if (!body.TryGetProperty("questId",   out var qidProp)  || !qidProp.TryGetInt32(out int questId))
                return BadRequest("Thiếu questId.");
            if (!body.TryGetProperty("stepIndex", out var siProp)   || !siProp.TryGetInt32(out int stepIndex))
                return BadRequest("Thiếu stepIndex.");
            body.TryGetProperty("delta", out var deltaProp);
            int delta = deltaProp.ValueKind == JsonValueKind.Number ? deltaProp.GetInt32() : 1;

            return await ApplyProgressToQuest(playerId, questId, stepIndex, delta);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST /api/quest/progress-by-event
        //  Body: { "playerId": 1, "type": "kill"|"collect"|"talk", "targetId": 5, "delta": 1 }
        //  Tìm quest active của player, nếu bước hiện tại khớp type+targetId → cộng tiến trình.
        //  Gọi từ Unity game server sau khi kill mob / pickup item / talk NPC.
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost("progress-by-event")]
        [Authorize(AuthenticationSchemes = ZoneApiKeyAuthenticationHandler.SchemeName)]
        public async Task<IActionResult> ProgressByEvent([FromBody] JsonElement body)
        {
            if (!body.TryGetProperty("playerId", out var pidProp)  || !pidProp.TryGetInt32(out int playerId))
                return BadRequest("Thiếu playerId.");
            if (!body.TryGetProperty("type",     out var typeProp))
                return BadRequest("Thiếu type.");
            if (!body.TryGetProperty("targetId", out var tidProp)  || !tidProp.TryGetInt32(out int targetId))
                return BadRequest("Thiếu targetId.");
            body.TryGetProperty("delta", out var deltaProp);
            int delta  = deltaProp.ValueKind == JsonValueKind.Number ? deltaProp.GetInt32() : 1;
            string type = typeProp.GetString() ?? "";

            // Lấy quest active duy nhất của player
            var pq = await _db.PlayerQuests
                .Include(x => x.Quest)
                .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.Status == "active");

            if (pq == null || pq.Quest == null)
                return Ok(new { message = "Không có quest active." });

            var steps = pq.Quest.Steps;
            if (pq.CurrentStepIndex >= steps.Count)
                return Ok(new { message = "Tất cả bước đã hoàn thành, chờ nộp nhiệm vụ." });

            var step = steps[pq.CurrentStepIndex];

            // Chỉ cộng tiến trình nếu type và targetId khớp
            if (!string.Equals(step.Type, type, StringComparison.OrdinalIgnoreCase) || step.TargetId != targetId)
                return Ok(new { message = "Sự kiện không khớp với bước hiện tại." });

            return await ApplyProgressToQuest(playerId, pq.QuestConfigId, pq.CurrentStepIndex, delta);
        }

        private async Task<IActionResult> ApplyProgressToQuest(int playerId, int questId, int stepIndex, int delta)
        {
            var pq = await _db.PlayerQuests
                .Include(x => x.Quest)
                .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.QuestConfigId == questId && x.Status == "active");
            if (pq == null) return NotFound("Không tìm thấy nhiệm vụ đang active.");
            if (pq.Quest == null) return NotFound("Quest config không tồn tại.");

            var steps = pq.Quest.Steps;
            if (stepIndex < 0 || stepIndex >= steps.Count)
                return BadRequest("stepIndex không hợp lệ.");
            if (pq.CurrentStepIndex != stepIndex)
                return Ok(new { message = "Không phải bước hiện tại.", current_step_index = pq.CurrentStepIndex });

            var progress = JsonSerializer.Deserialize<Dictionary<string, int>>(pq.ProgressJson)
                           ?? new Dictionary<string, int>();
            string key = stepIndex.ToString();
            progress.TryGetValue(key, out int current);
            current += delta;

            int required  = steps[stepIndex].RequiredCount;
            bool stepDone = current >= required;
            if (stepDone) current = required;

            progress[key]   = current;
            pq.ProgressJson = JsonSerializer.Serialize(progress);
            if (stepDone)
                pq.CurrentStepIndex++;

            await _db.SaveChangesAsync();

            bool allDone    = pq.CurrentStepIndex >= steps.Count;
            QuestStep? next = (!allDone && pq.CurrentStepIndex < steps.Count)
                ? steps[pq.CurrentStepIndex] : null;

            return Ok(new
            {
                current_step_index = pq.CurrentStepIndex,
                step_progress      = current,
                step_required      = required,
                step_done          = stepDone,
                all_steps_done     = allDone,
                next_step          = next,
                progress_json      = pq.ProgressJson,
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST /api/quest/complete
        //  Body: { "questId": 1 }
        //  Validate tất cả bước đã xong → cộng phần thưởng → status=completed.
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] JsonElement body)
        {
            int playerId = GetPlayerId();
            if (playerId <= 0) return Unauthorized();

            if (!body.TryGetProperty("questId", out var qidProp) || !qidProp.TryGetInt32(out int questId))
                return BadRequest("Thiếu questId.");

            var pq = await _db.PlayerQuests
                .Include(x => x.Quest)
                .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.QuestConfigId == questId && x.Status == "active");
            if (pq == null) return NotFound("Không tìm thấy nhiệm vụ đang active.");
            if (pq.Quest == null) return NotFound("Quest config không tồn tại.");

            var steps = pq.Quest.Steps;
            // Validate tất cả bước đã hoàn thành
            if (pq.CurrentStepIndex < steps.Count)
            {
                var progress = JsonSerializer.Deserialize<Dictionary<string, int>>(pq.ProgressJson)
                               ?? new Dictionary<string, int>();
                int cur = pq.CurrentStepIndex;
                int req = steps[cur].RequiredCount;
                progress.TryGetValue(cur.ToString(), out int done);
                if (done < req)
                    return BadRequest($"Chưa hoàn thành bước {cur + 1}: {steps[cur].TargetName} ({done}/{req}).");
            }

            // Cộng thưởng
            var reward = pq.Quest.Reward;
            var pdata  = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");

            var info = pdata.GetInfoChar() ?? new InfoChar();
            info.Experience         += reward.Exp;
            info.Gold               += reward.Gold;
            info.Silver             += reward.Silver;
            info.QuestCompletedCount++;
            pdata.SetInfoChar(info);

            // Đánh dấu hoàn thành
            pq.Status      = "completed";
            pq.CompletedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message       = $"Hoàn thành nhiệm vụ '{pq.Quest.Name}'!",
                reward_exp    = reward.Exp,
                reward_gold   = reward.Gold,
                reward_silver = reward.Silver,
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        //  Helpers
        // ═════════════════════════════════════════════════════════════════════
        private int GetPlayerId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
            return int.TryParse(claim, out int id) ? id : 0;
        }
    }
}
