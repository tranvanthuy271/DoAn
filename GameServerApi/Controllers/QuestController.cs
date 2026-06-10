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
        private readonly ILogger<QuestController> _logger;
        private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
        private const int DefaultBagSlots = 20;
        private const int BagExpandBy = 5;
        private const int MaxEquippedBagQuickSlots = 3;

        public QuestController(GameDbContext db, ILogger<QuestController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ═════════════════════════════════════════════════════════════════════
        //  GET /api/quest/list?npcId=2
        //  Trả về danh sách quest của NPC, kèm trạng thái của player hiện tại.
        //  Status: available | active | completed | locked
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet("list")]
        public async Task<IActionResult> GetQuestList([FromQuery] int npcId)
        {
            int playerId = GetPlayerId();
            if (playerId <= 0) return Unauthorized();

            var pdata = await _db.PlayerData.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");

            var info = pdata.GetInfoChar() ?? new InfoChar();
            int playerLevel = info.Level;

            var quests = await _db.QuestConfigs.AsNoTracking()
                .Where(q => q.IsActive && q.NpcId == npcId)
                .OrderBy(q => q.SortOrder)
                .ToListAsync();

            // Lấy vị trí NPC một lần (join)
            var npcIds = quests.Select(q => q.NpcId).Distinct().ToList();
            npcIds.Add(npcId);
            var npcMap = await _db.NpcConfigs.AsNoTracking()
                .Where(n => npcIds.Contains(n.NpcId))
                .ToDictionaryAsync(n => n.NpcId);

            var mapIds = npcMap.Values.Select(n => n.MapId).Distinct().ToList();
            var mapNameById = await _db.MapConfigs.AsNoTracking()
                .Where(m => mapIds.Contains(m.MapId))
                .ToDictionaryAsync(m => m.MapId, m => m.MapName);

            var result = quests.Select(q =>
            {
                string status;
                if      (info.CompletedQuests.Contains(q.Id))   status = "completed";
                else if (info.ActiveQuestId == q.Id)            status = "active";
                else if (info.ActiveQuestId >= 0)               status = "locked";
                else if (playerLevel < q.LevelNeed)             status = "locked";
                else                                            status = "available";

                string progressJson = (status == "active")
                    ? JsonSerializer.Serialize(info.QuestProgress)
                    : "{}";

                npcMap.TryGetValue(q.NpcId, out var npcData);
                string npcMapName = (npcData != null && mapNameById.TryGetValue(npcData.MapId, out var mapName))
                    ? mapName
                    : "";
                return new
                {
                    quest_id             = q.Id,
                    name                 = q.Name,
                    level_need           = q.LevelNeed,
                    npc_id               = q.NpcId,
                    npc_name             = npcData?.NpcName ?? "",
                    npc_map_id           = npcData?.MapId ?? -1,
                    npc_map_name         = npcMapName,
                    npc_pos_x            = npcData?.PosX  ?? 0f,
                    npc_pos_y            = npcData?.PosY  ?? 0f,
                    str1                 = q.Str1,
                    str2                 = q.Str2,
                    str3                 = q.Str3,
                    exp_reward           = q.ExpReward,
                    gold_reward          = q.GoldReward,
                    silver_reward        = q.SilverReward,
                    item_reward          = q.ItemReward,
                    steps_json           = q.StepJson,
                    status,
                    current_step_index   = (status == "active") ? info.QuestStep : 0,
                    quest_progress_json  = progressJson,
                };
            }).ToList();

            return Ok(result);
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST /api/quest/accept
        //  Body: { "questId": 1 }
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

            var pdata = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");

            var info = pdata.GetInfoChar() ?? new InfoChar();

            if (info.Level < quest.LevelNeed)
                return BadRequest($"Cần cấp {quest.LevelNeed} để nhận nhiệm vụ này.");

            if (info.ActiveQuestId >= 0)
                return BadRequest("Bạn đang có nhiệm vụ chưa hoàn thành. Hoàn thành hoặc bỏ nhiệm vụ hiện tại trước.");

            if (info.CompletedQuests.Contains(questId))
                return BadRequest("Bạn đã hoàn thành nhiệm vụ này rồi.");

            info.ActiveQuestId  = questId;
            info.QuestStep      = 0;
            info.QuestProgress  = new Dictionary<string, int>();
            pdata.SetInfoChar(info);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                message            = "Đã nhận nhiệm vụ!",
                quest_id           = questId,
                quest_name         = quest.Name,
                current_step_index = 0,
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST /api/quest/abandon
        //  Body: {} (dùng JWT để xác định player)
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost("abandon")]
        public async Task<IActionResult> Abandon()
        {
            int playerId = GetPlayerId();
            if (playerId <= 0) return Unauthorized();

            var pdata = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");

            var info = pdata.GetInfoChar() ?? new InfoChar();
            if (info.ActiveQuestId < 0)
                return Ok(new { message = "Không có nhiệm vụ đang làm." });

            info.ActiveQuestId = -1;
            info.QuestStep     = 0;
            info.QuestProgress = new Dictionary<string, int>();
            pdata.SetInfoChar(info);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Đã bỏ nhiệm vụ." });
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST /api/quest/progress-by-event
        //  Body: { "playerId": 1, "type": "kill"|"collect"|"talk"|"reach", "targetId": 5, "delta": 1 }
        //  Gọi từ Unity game server sau sự kiện: kill mob / nhặt item / nói chuyện / đến map.
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
            int    delta = deltaProp.ValueKind == JsonValueKind.Number ? deltaProp.GetInt32() : 1;
            string type  = typeProp.GetString() ?? "";

            var pdata = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");

            var info = pdata.GetInfoChar() ?? new InfoChar();
            if (info.ActiveQuestId < 0)
                return Ok(new { message = "Không có quest active." });

            var quest = await _db.QuestConfigs.AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == info.ActiveQuestId && q.IsActive);
            if (quest == null)
                return Ok(new { message = "Quest config không tồn tại." });

            var steps = quest.Steps;
            if (info.QuestStep >= steps.Count)
                return Ok(new { message = "Tất cả bước đã hoàn thành, chờ nộp nhiệm vụ." });

            var step = steps[info.QuestStep];

            bool matches = type switch
            {
                "kill"    => step.Id == 0 && step.IdMob  == targetId,
                "collect" => step.Id == 1 && step.IdItem == targetId,
                "talk"    => step.Id == 5 && step.IdNpc  == targetId,
                "reach"   => step.Id == 9 && step.IdMap  == targetId,
                _         => false,
            };

            if (!matches)
                return Ok(new { message = "Sự kiện không khớp với bước hiện tại." });

            string key = info.QuestStep.ToString();
            info.QuestProgress.TryGetValue(key, out int current);
            current = Math.Min(current + delta, step.Require);
            info.QuestProgress[key] = current;

            bool stepDone = current >= step.Require;
            if (stepDone) info.QuestStep++;

            pdata.SetInfoChar(info);
            await _db.SaveChangesAsync();

            bool allDone = info.QuestStep >= steps.Count;
            return Ok(new
            {
                current_step_index  = info.QuestStep,
                step_progress       = current,
                step_required       = step.Require,
                step_done           = stepDone,
                all_steps_done      = allDone,
                quest_progress_json = JsonSerializer.Serialize(info.QuestProgress),
            });
        }

        // ═════════════════════════════════════════════════════════════════════
        //  POST /api/quest/complete
        //  Body: { "questId": 1 }
        // ═════════════════════════════════════════════════════════════════════
        [HttpPost("complete")]
        public async Task<IActionResult> Complete([FromBody] JsonElement body)
        {
            int playerId = GetPlayerId();
            if (playerId <= 0) return Unauthorized();

            if (!body.TryGetProperty("questId", out var qidProp) || !qidProp.TryGetInt32(out int questId))
                return BadRequest("Thiếu questId.");

            var pdata = await _db.PlayerData.FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");

            var info = pdata.GetInfoChar() ?? new InfoChar();
            if (info.ActiveQuestId != questId)
                return BadRequest("Nhiệm vụ này không phải quest đang active.");

            var quest = await _db.QuestConfigs.AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == questId && q.IsActive);
            if (quest == null) return NotFound("Quest config không tồn tại.");

            var steps = quest.Steps;
            // Validate tất cả bước đã hoàn thành
            for (int i = 0; i < steps.Count; i++)
            {
                info.QuestProgress.TryGetValue(i.ToString(), out int done);
                if (done < steps[i].Require)
                    return BadRequest($"Chưa hoàn thành bước {i + 1}: {steps[i].Name} ({done}/{steps[i].Require}).");
            }

            var rewardItems = await ResolveQuestRewardItemsAsync(quest.ItemReward);
            if (rewardItems.Count > 0)
            {
                int maxSlots = ResolveBagSlotLimit(info);
                var inventory = ParseMutableInventory(pdata.InventoryJson);
                NormalizeInventory(inventory, maxSlots);

                if (!TryGrantRewardItemsToInventory(inventory, rewardItems, maxSlots, out string inventoryError))
                    return BadRequest(inventoryError);

                pdata.InventoryJson = JsonSerializer.Serialize(inventory);
            }

            // Cộng thưởng
            info.Experience   += quest.ExpReward;
            info.Gold         += quest.GoldReward;
            info.Silver       += quest.SilverReward;
            info.QuestCompletedCount++;

            // Đánh dấu hoàn thành, xoá trạng thái active
            if (!info.CompletedQuests.Contains(questId))
                info.CompletedQuests.Add(questId);
            info.ActiveQuestId = -1;
            info.QuestStep     = 0;
            info.QuestProgress = new Dictionary<string, int>();

            pdata.SetInfoChar(info);
            pdata.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            try
            {
                _db.PlayerQuestLogs.Add(new PlayerQuestLog
                {
                    CharacterId = playerId,
                    QuestId     = questId,
                    QuestName   = quest.Name,
                    CompletedAt = DateTime.UtcNow,
                });

                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[Quest] Failed to write player_quest_log for playerId={PlayerId}, questId={QuestId}. Quest completion has already been committed.",
                    playerId,
                    questId);
                _db.ChangeTracker.Clear();
            }

            return Ok(new
            {
                message       = $"Hoàn thành nhiệm vụ '{quest.Name}'!",
                reward_exp    = quest.ExpReward,
                reward_gold   = quest.GoldReward,
                reward_silver = quest.SilverReward,
                item_reward   = quest.ItemReward,
                reward_items  = rewardItems.Select(item => new
                {
                    item_template_id = item.ItemTemplateId,
                    item_name = item.ItemName,
                    quantity = item.Quantity,
                }).ToArray(),
            });
        }

        private async Task<List<QuestRewardItemGrant>> ResolveQuestRewardItemsAsync(string itemReward)
        {
            var requests = ParseQuestRewardSpec(itemReward);
            if (requests.Count == 0)
                return new List<QuestRewardItemGrant>();

            var templateIds = requests
                .Select(request => request.ItemTemplateId)
                .Distinct()
                .ToList();

            var templates = await _db.ItemTemplates.AsNoTracking()
                .Where(item => templateIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id);

            var rewards = new List<QuestRewardItemGrant>();
            foreach (var request in requests)
            {
                if (!templates.TryGetValue(request.ItemTemplateId, out var template))
                    continue;

                rewards.Add(new QuestRewardItemGrant
                {
                    ItemTemplateId = template.Id,
                    ItemName = template.Name,
                    Quantity = request.Quantity,
                    UpgradeLevel = 0,
                    StrOptions = GetDefaultStrOptions(template.Id),
                    IsStackable = string.Equals(template.IsXepChong, "True", StringComparison.OrdinalIgnoreCase),
                });
            }

            return rewards;
        }

        private static List<QuestRewardItemRequest> ParseQuestRewardSpec(string itemReward)
        {
            var items = new List<QuestRewardItemRequest>();
            if (string.IsNullOrWhiteSpace(itemReward))
                return items;

            foreach (var rawEntry in itemReward.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = rawEntry.Split('@', StringSplitOptions.TrimEntries);
                if (parts.Length == 0)
                    continue;

                if (!int.TryParse(parts[0], out int itemTemplateId) || itemTemplateId <= 0)
                    continue;

                int quantity = 1;
                if (parts.Length > 1 && int.TryParse(parts[1], out int parsedQty) && parsedQty > 0)
                    quantity = parsedQty;

                items.Add(new QuestRewardItemRequest
                {
                    ItemTemplateId = itemTemplateId,
                    Quantity = quantity,
                });
            }

            return items;
        }

        private static List<Dictionary<string, object>> ParseMutableInventory(string inventoryJson)
        {
            var inventory = new List<Dictionary<string, object>>();
            if (string.IsNullOrWhiteSpace(inventoryJson) || inventoryJson == "[]")
                return inventory;

            var parsed = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(inventoryJson);
            if (parsed == null)
                return inventory;

            foreach (var item in parsed)
            {
                var dict = new Dictionary<string, object>();
                foreach (var kvp in item)
                {
                    dict[kvp.Key] = kvp.Value.ValueKind switch
                    {
                        JsonValueKind.Number => kvp.Value.TryGetInt32(out var intVal) ? intVal : kvp.Value.GetDouble(),
                        JsonValueKind.String => kvp.Value.GetString() ?? string.Empty,
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        _ => kvp.Value.ToString()
                    };
                }

                inventory.Add(dict);
            }

            return inventory;
        }

        private static void NormalizeInventory(List<Dictionary<string, object>> inventory, int maxSlots)
        {
            int autoSlot = 0;
            foreach (var item in inventory)
            {
                if (!item.ContainsKey("slotIndex"))
                {
                    while (autoSlot < maxSlots && inventory.Any(slot => slot.ContainsKey("slotIndex") && Convert.ToInt32(slot["slotIndex"]) == autoSlot))
                        autoSlot++;

                    if (autoSlot < maxSlots)
                    {
                        item["slotIndex"] = autoSlot;
                        autoSlot++;
                    }
                }

                item.Remove("iconId");
                item.Remove("isEquipped");
                item.Remove("itemCode");
                if (!item.ContainsKey("strOptions"))
                    item["strOptions"] = string.Empty;
            }
        }

        private static bool TryGrantRewardItemsToInventory(
            List<Dictionary<string, object>> inventory,
            List<QuestRewardItemGrant> rewardItems,
            int maxSlots,
            out string error)
        {
            foreach (var reward in rewardItems)
            {
                if (reward.IsStackable && reward.UpgradeLevel == 0)
                {
                    var existingSlot = inventory.FirstOrDefault(slot =>
                        slot.ContainsKey("itemTemplateId") &&
                        Convert.ToInt32(slot["itemTemplateId"]) == reward.ItemTemplateId);

                    if (existingSlot != null)
                    {
                        int currentQty = existingSlot.ContainsKey("quantity")
                            ? Convert.ToInt32(existingSlot["quantity"]) : 0;
                        existingSlot["quantity"] = currentQty + reward.Quantity;
                        continue;
                    }
                }

                int emptySlotIndex = -1;
                for (int slotIndex = 0; slotIndex < maxSlots; slotIndex++)
                {
                    var existingSlot = inventory.FirstOrDefault(slot =>
                        slot.ContainsKey("slotIndex") && Convert.ToInt32(slot["slotIndex"]) == slotIndex);

                    if (existingSlot == null || !existingSlot.ContainsKey("quantity") || Convert.ToInt32(existingSlot["quantity"]) <= 0)
                    {
                        emptySlotIndex = slotIndex;
                        break;
                    }
                }

                if (emptySlotIndex < 0)
                {
                    error = $"Túi đồ không đủ chỗ để nhận {reward.ItemName} x{reward.Quantity}.";
                    return false;
                }

                inventory.RemoveAll(slot => slot.ContainsKey("slotIndex") && Convert.ToInt32(slot["slotIndex"]) == emptySlotIndex);
                inventory.Add(new Dictionary<string, object>
                {
                    ["slotIndex"] = emptySlotIndex,
                    ["itemTemplateId"] = reward.ItemTemplateId,
                    ["quantity"] = reward.Quantity,
                    ["upgradeLevel"] = reward.UpgradeLevel,
                    ["strOptions"] = reward.StrOptions,
                });
            }

            error = string.Empty;
            return true;
        }

        private static string GetDefaultStrOptions(int itemTemplateId) =>
            UpgradeController.DefaultStrOptions.TryGetValue(itemTemplateId, out var value) ? value : string.Empty;

        private static int ResolveBagSlotLimit(InfoChar? info)
        {
            int maxPossible = DefaultBagSlots + MaxEquippedBagQuickSlots * BagExpandBy;
            int actual = info?.BagSlots > 0 ? info.BagSlots : DefaultBagSlots;
            return Math.Min(actual, maxPossible);
        }

        private sealed class QuestRewardItemRequest
        {
            public int ItemTemplateId { get; set; }
            public int Quantity { get; set; }
        }

        private sealed class QuestRewardItemGrant
        {
            public int ItemTemplateId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public int UpgradeLevel { get; set; }
            public string StrOptions { get; set; } = string.Empty;
            public bool IsStackable { get; set; }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  GET /api/quest/player-overview
        //  Trả về quest đang active (hoặc quest available đầu tiên) để HUD hiện gợi ý.
        // ═════════════════════════════════════════════════════════════════════
        [HttpGet("player-overview")]
        public async Task<IActionResult> PlayerOverview()
        {
            int playerId = GetPlayerId();
            if (playerId <= 0) return Unauthorized();

            var pdata = await _db.PlayerData.AsNoTracking()
                .FirstOrDefaultAsync(p => p.PlayerId == playerId);
            if (pdata == null) return NotFound("Player không tồn tại.");

            var info = pdata.GetInfoChar() ?? new InfoChar();
            int playerLevel = info.Level;

            // 1. Quest đang active
            if (info.ActiveQuestId > 0)
            {
                var quest = await _db.QuestConfigs.AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Id == info.ActiveQuestId && q.IsActive);
                if (quest != null)
                {
                    var npc = await _db.NpcConfigs.AsNoTracking()
                        .FirstOrDefaultAsync(n => n.NpcId == quest.NpcId);
                    string npcMapName = "";
                    if (npc != null)
                    {
                        npcMapName = await _db.MapConfigs.AsNoTracking()
                            .Where(m => m.MapId == npc.MapId)
                            .Select(m => m.MapName)
                            .FirstOrDefaultAsync()
                            ?? "";
                    }

                    return Ok(BuildOverviewDto(quest, npc, npcMapName, "active", info));
                }
            }

            // 2. Quest available đầu tiên theo level
            var available = await _db.QuestConfigs.AsNoTracking()
                .Where(q => q.IsActive
                         && !info.CompletedQuests.Contains(q.Id)
                         && q.Id != info.ActiveQuestId
                         && q.LevelNeed <= playerLevel)
                .OrderBy(q => q.SortOrder).ThenBy(q => q.Id)
                .FirstOrDefaultAsync();

            if (available != null)
            {
                var npc = await _db.NpcConfigs.AsNoTracking()
                    .FirstOrDefaultAsync(n => n.NpcId == available.NpcId);
                string npcMapName = "";
                if (npc != null)
                {
                    npcMapName = await _db.MapConfigs.AsNoTracking()
                        .Where(m => m.MapId == npc.MapId)
                        .Select(m => m.MapName)
                        .FirstOrDefaultAsync()
                        ?? "";
                }

                return Ok(BuildOverviewDto(available, npc, npcMapName, "available", info));
            }

            return Ok(null);
        }

        private static object BuildOverviewDto(
            QuestConfig q, NpcConfig? npc, string npcMapName, string status, InfoChar info)
        {
            return new
            {
                quest_id            = q.Id,
                name                = q.Name,
                status,
                npc_id              = q.NpcId,
                npc_name            = npc?.NpcName ?? "",
                npc_map_id          = npc?.MapId   ?? -1,
                npc_map_name        = npcMapName,
                npc_pos_x           = npc?.PosX    ?? 0f,
                npc_pos_y           = npc?.PosY    ?? 0f,
                str1                = q.Str1,
                str2                = q.Str2,
                str3                = q.Str3,
                level_need          = q.LevelNeed,
                exp_reward          = q.ExpReward,
                gold_reward         = q.GoldReward,
                silver_reward       = q.SilverReward,
                item_reward         = q.ItemReward,
                steps_json          = q.StepJson,
                current_step_index  = (status == "active") ? info.QuestStep : 0,
                quest_progress_json = (status == "active")
                    ? JsonSerializer.Serialize(info.QuestProgress)
                    : "{}",
            };
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
