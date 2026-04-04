using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/upgrade")]
    [Authorize]
    public class UpgradeController : ControllerBase
    {
        private const int LuckyStoneItemId = 8;
        private const int ProtectionStoneItemId = 9;
        private const float LuckyStoneBonusRate = 0.15f;

        private readonly GameDbContext _db;

        public UpgradeController(GameDbContext db)
        {
            _db = db;
        }

        // ──────────────────────────────────────────────────────────────
        //  HARDCODED DATA
        // ──────────────────────────────────────────────────────────────

        // Option templates: id, name, type, level, strOption (20 values sep by ';')
        // level = item.upgradeLevel tối thiểu để activate
        // strOption[N] = stat value khi item ở bậc +N
        // strOption có 24 giá trị: index 0 = bậc +0, index 23 = bậc +23 (tối đa)
        // Từ +21→+24: mở rộng theo LangLa 5x-6x, yêu cầu đá cấp 8-9 (item 42-43)
        public static readonly List<Dictionary<string, object>> OptionTemplates = new()
        {
            new() { ["id"]=1, ["name"]="Tấn công: +#",  ["type"]=0, ["level"]=0, ["strOption"]="10;12;14;16;18;20;23;26;29;32;36;40;44;48;52;56;60;65;70;75;81;87;93;100" },
            new() { ["id"]=2, ["name"]="Phòng thủ: +#", ["type"]=2, ["level"]=0, ["strOption"]="8;9;10;11;12;13;14;15;16;17;18;19;20;22;24;26;28;30;32;35;38;41;44;48" },
            new() { ["id"]=3, ["name"]="HP tối đa: +#",  ["type"]=2, ["level"]=0, ["strOption"]="30;33;36;39;42;45;48;51;54;57;60;63;66;69;72;75;78;81;84;90;97;104;112;120" },
            new() { ["id"]=4, ["name"]="Tốc độ: +#",    ["type"]=2, ["level"]=0, ["strOption"]="5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23;25;27;29;31;33" },
            new() { ["id"]=5, ["name"]="Tấn công: +#",  ["type"]=3, ["level"]=4, ["strOption"]="0;0;0;0;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;22;24;26;28" },
            new() { ["id"]=6, ["name"]="Phòng thủ: +#", ["type"]=4, ["level"]=8, ["strOption"]="0;0;0;0;0;0;0;0;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20" },
        };

        // strOptions mặc định ở bậc +0 cho từng item template
        // format: "optId,value;optId,value;..."
        // Giá trị khớp với OptionTemplates[id-1].strOption[0]
        public static readonly Dictionary<int, string> DefaultStrOptions = new()
        {
            // ── Trang bị cơ bản lv1 ──
            [100] = "3,30",       // Mũ Da Nam:          HP+30
            [105] = "3,30",       // Mũ Lụa Nữ:          HP+30
            [110] = "2,8;3,30",   // Áo Da Nam:          DEF+8, HP+30
            [115] = "2,8;3,30",   // Áo Lụa Nữ:          DEF+8, HP+30
            [130] = "2,8",        // Quần Da Nam:        DEF+8
            [135] = "2,8",        // Quần Lụa Nữ:        DEF+8
            [140] = "3,30",       // Nhẫn Đá:            HP+30
            [150] = "4,5",        // Giày Da Nam:        SPD+5
            [155] = "4,5",        // Giày Lụa Nữ:        SPD+5
            // ── Vũ khí lv1 (tất cả hệ) ──
            [200] = "1,10",       // Kiếm Hỏa Sơ Cấp:   ATK+10
            [205] = "1,10",       // Cung Thủy Sơ Cấp:   ATK+10
            [210] = "1,10",       // Chùy Thổ Sơ Cấp:   ATK+10
            [215] = "1,10",       // Đao Kim Sơ Cấp:     ATK+10
            [220] = "1,10",       // Gậy Mộc Sơ Cấp:     ATK+10
            [225] = "1,10",       // Thương Phong Sơ Cấp: ATK+10
            // ── Trang bị 3x (lv30 — Tier Ngân Tinh) ──
            [300] = "3,30",       // Mũ Ngân Tinh Nam:   HP+30
            [301] = "3,30",       // Mũ Ngân Tinh Nữ:    HP+30
            [302] = "2,8;3,30",   // Áo Ngân Tinh Nam:   DEF+8, HP+30
            [303] = "2,8;3,30",   // Áo Ngân Tinh Nữ:    DEF+8, HP+30
            [304] = "2,8",        // Quần Ngân Tinh Nam: DEF+8
            [305] = "2,8",        // Quần Ngân Tinh Nữ:  DEF+8
            [306] = "3,30",       // Nhẫn Bạch Kim:      HP+30
            [307] = "4,5",        // Giày Ngân Tinh Nam: SPD+5
            [308] = "4,5",        // Giày Ngân Tinh Nữ:  SPD+5
            // ── Trang bị 4x (lv40 — Tier Thiên Mệnh) ──
            [400] = "3,30",       // Mũ Thiên Mệnh Nam:  HP+30
            [401] = "3,30",       // Mũ Thiên Mệnh Nữ:   HP+30
            [402] = "2,8;3,30",   // Áo Thiên Mệnh Nam:  DEF+8, HP+30
            [403] = "2,8;3,30",   // Áo Thiên Mệnh Nữ:   DEF+8, HP+30
            [404] = "2,8",        // Quần Thiên Mệnh Nam: DEF+8
            [405] = "2,8",        // Quần Thiên Mệnh Nữ:  DEF+8
            [406] = "3,30",       // Nhẫn Huyết Long:    HP+30
            [407] = "4,5",        // Giày Thiên Mệnh Nam: SPD+5
            [408] = "4,5",        // Giày Thiên Mệnh Nữ:  SPD+5
        };

        // ──────────────────────────────────────────────────────────────
        //  GET /api/upgrade/options
        //  Trả về toàn bộ option templates
        // ──────────────────────────────────────────────────────────────
        [HttpGet("options")]
        public IActionResult GetOptions()
        {
            return Ok(new { options = OptionTemplates });
        }

        // ──────────────────────────────────────────────────────────────
        //  GET /api/upgrade/config?itemId=X&targetLevel=Y
        //  Trả về config nâng cấp cho bậc cụ thể
        // ──────────────────────────────────────────────────────────────
        [HttpGet("config")]
        public async System.Threading.Tasks.Task<IActionResult> GetConfig([FromQuery] int itemId, [FromQuery] int targetLevel)
        {
            if (targetLevel < 1 || targetLevel > 24)
                return BadRequest("targetLevel phải từ 1 đến 24.");

            var cfg = await _db.EquipmentUpgradeConfigs.FindAsync(targetLevel);
            if (cfg == null)
                return NotFound($"Không có config cho bậc +{targetLevel}.");

            // Lấy tên đá từ item_template
            var stone = await _db.ItemTemplates.FindAsync(cfg.StoneId);
            string stoneName = stone?.Name ?? $"Đá Cấp {cfg.StoneId}";

            return Ok(new
            {
                targetLevel,
                silverCost      = cfg.SilverCost,
                stoneId         = cfg.StoneId,
                stoneName,
                stoneNeeded     = cfg.StoneNeeded,
                stoneMin        = cfg.StoneMin,
                baseSuccessRate = cfg.BaseSuccessRate,
                failPolicy      = cfg.FailPolicy
            });
        }

        // ──────────────────────────────────────────────────────────────
        //  POST /api/upgrade/equipment
        //  Body: { playerId, slotKey, isFromInventory, stoneSlotIndices }
        // ──────────────────────────────────────────────────────────────
        [HttpPost("equipment")]
        public async System.Threading.Tasks.Task<IActionResult> UpgradeEquipment([FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("playerId",        out var pidProp))    return BadRequest("Thiếu playerId.");
                if (!body.TryGetProperty("slotKey",         out var slotKeyProp)) return BadRequest("Thiếu slotKey.");
                if (!body.TryGetProperty("isFromInventory", out var fromInvProp)) return BadRequest("Thiếu isFromInventory.");

                int    playerId       = pidProp.GetInt32();
                string slotKey        = slotKeyProp.GetString() ?? "";
                bool   isFromInventory = fromInvProp.GetBoolean();
                int clientRatePercent = body.TryGetProperty("clientRatePercent", out var rateProp)
                    ? rateProp.GetInt32()
                    : -1;

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null) return NotFound("Player không tồn tại.");

                // Parse inventory
                var inventory = ParseJsonList(player.InventoryJson);

                // Lấy item cần nâng cấp
                Dictionary<string, object>? itemDict;
                Dictionary<string, object>? equipment = null;

                if (isFromInventory)
                {
                    if (!int.TryParse(slotKey, out int slotIdx))
                        return BadRequest("slotKey không hợp lệ khi isFromInventory=true.");
                    itemDict = inventory.FirstOrDefault(s =>
                        s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == slotIdx);
                    if (itemDict == null) return BadRequest($"Không tìm thấy item ở slot {slotIdx}.");
                }
                else
                {
                    equipment = ParseEquipJson(player.EquipmentJson);
                    if (!equipment.ContainsKey(slotKey) || equipment[slotKey] == null)
                        return BadRequest($"Slot trang bị '{slotKey}' đang trống.");
                    itemDict = equipment[slotKey] as Dictionary<string, object>;
                    if (itemDict == null) return BadRequest("Dữ liệu trang bị không hợp lệ.");
                }

                int currentLevel    = itemDict.ContainsKey("upgradeLevel") ? Convert.ToInt32(itemDict["upgradeLevel"]) : 0;
                int targetLevel     = currentLevel + 1;
                int itemTemplateId  = itemDict.ContainsKey("itemTemplateId") ? Convert.ToInt32(itemDict["itemTemplateId"]) : 0;

                if (targetLevel > 24)
                    return BadRequest("Trang bị đã đạt bậc tối đa (+24).");

                // Lấy config từ DB
                var cfg = await _db.EquipmentUpgradeConfigs.FindAsync(targetLevel);
                if (cfg == null)
                    return BadRequest($"Không có config nâng cấp cho bậc +{targetLevel}.");

                var stoneRequests = TryBuildRequestedSlotCountMap(
                    body,
                    usagePropertyName: "stoneUsages",
                    indexPropertyName: "stoneSlotIndices",
                    out string? stoneRequestError);
                if (stoneRequestError != null)
                    return BadRequest(stoneRequestError);

                var charmRequests = TryBuildRequestedSlotCountMap(
                    body,
                    usagePropertyName: "charmUsages",
                    indexPropertyName: "charmSlotIndices",
                    out string? charmRequestError);
                if (charmRequestError != null)
                    return BadRequest(charmRequestError);

                int upgradeStoneCount = 0;
                int luckyStoneCount   = 0;
                int protectionStoneCount = 0;

                foreach (var request in stoneRequests)
                {
                    var stone = FindInventorySlotByIndex(inventory, request.Key);
                    if (stone == null)
                        return BadRequest($"Không tìm thấy vật liệu ở slot {request.Key}.");

                    int amount = GetSlotAmount(stone);
                    if (amount < request.Value)
                        return BadRequest($"Slot {request.Key} không đủ số lượng vật liệu. Cần {request.Value}, hiện có {amount}.");

                    int stoneItemId = GetSlotItemTemplateId(stone);
                    if (stoneItemId == cfg.StoneId)
                        upgradeStoneCount += request.Value;
                    else if (stoneItemId == ProtectionStoneItemId)
                        protectionStoneCount += request.Value;
                    else
                        return BadRequest($"Vật liệu ở slot {request.Key} không hợp lệ cho bậc +{targetLevel}.");
                }

                foreach (var request in charmRequests)
                {
                    var charm = FindInventorySlotByIndex(inventory, request.Key);
                    if (charm == null)
                        return BadRequest($"Không tìm thấy bùa ở slot {request.Key}.");

                    int amount = GetSlotAmount(charm);
                    if (amount < request.Value)
                        return BadRequest($"Slot {request.Key} không đủ số lượng Đá May Mắn. Cần {request.Value}, hiện có {amount}.");

                    int charmItemId = GetSlotItemTemplateId(charm);
                    if (charmItemId != LuckyStoneItemId)
                        return BadRequest($"Item ở slot {request.Key} không phải Đá May Mắn.");

                    luckyStoneCount += request.Value;
                }

                if (luckyStoneCount > 1)
                    return BadRequest("Chỉ được dùng 1 Đá May Mắn mỗi lần cường hóa.");

                if (protectionStoneCount > 1)
                    return BadRequest("Chỉ được dùng 1 Đá Bảo Vệ mỗi lần cường hóa.");

                if (upgradeStoneCount < cfg.StoneMin)
                    return BadRequest($"Cần ít nhất {cfg.StoneMin} đá nâng cấp. Hiện có: {upgradeStoneCount}.");

                // Tỉ lệ thành công
                float stoneRatio = Math.Min((float)upgradeStoneCount / cfg.StoneNeeded, 1f);
                float rate       = cfg.BaseSuccessRate * stoneRatio + luckyStoneCount * LuckyStoneBonusRate;
                rate = Math.Clamp(rate, 0f, 1f);

                int serverRatePercent = (int)Math.Round(rate * 100f, MidpointRounding.AwayFromZero);
                if (clientRatePercent >= 0 && Math.Abs(clientRatePercent - serverRatePercent) > 1)
                    return BadRequest($"Tỉ lệ client không khớp server. Client={clientRatePercent}% | Server={serverRatePercent}%.");

                bool success    = new Random().NextDouble() < rate;
                bool downgraded = false;
                int  newLevel   = currentLevel;
                bool hasProtection = protectionStoneCount > 0;

                if (success)
                {
                    newLevel = targetLevel;
                }
                else if (!hasProtection)
                {
                    if      (cfg.FailPolicy == 1 && currentLevel > 0) { newLevel = currentLevel - 1; downgraded = true; }
                    else if (cfg.FailPolicy == 2 && currentLevel > 0) { newLevel = 0;                downgraded = true; }
                }

                // Trừ bạc (silver) khỏi info_char
                var info = player.GetInfoChar();
                if (info.Silver < cfg.SilverCost)
                    return BadRequest($"Không đủ bạc. Cần {cfg.SilverCost:N0}, hiện có {info.Silver:N0}.");
                info.Silver -= cfg.SilverCost;
                player.SetInfoChar(info);

                ConsumeRequestedInventorySlots(inventory, stoneRequests);
                ConsumeRequestedInventorySlots(inventory, charmRequests);

                // Tính lại strOptions theo bậc mới
                string currentStrOptions = itemDict.ContainsKey("strOptions") ? itemDict["strOptions"]?.ToString() ?? "" : "";
                if (string.IsNullOrEmpty(currentStrOptions) && DefaultStrOptions.ContainsKey(itemTemplateId))
                    currentStrOptions = DefaultStrOptions[itemTemplateId];

                string newStrOptions = RecalcStrOptions(currentStrOptions, newLevel);

                // Cập nhật item
                itemDict["upgradeLevel"] = newLevel;
                itemDict["strOptions"]   = newStrOptions;

                // Lưu vào đúng chỗ
                if (isFromInventory)
                {
                    // itemDict đã là ref vào phần tử trong list → inventory đã được cập nhật
                }
                else
                {
                    equipment![slotKey] = itemDict;
                    player.EquipmentJson = JsonSerializer.Serialize(equipment);
                }

                player.InventoryJson = JsonSerializer.Serialize(inventory);
                player.UpdatedAt     = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Tính final_stats sau khi trang bị thay đổi — client dùng để update UI
                var upgFs = StatCalculator.Compute(player.GetInfoChar(), player.EquipmentJson, player.PotentialStatsJson);

                string msg = success
                    ? $"✨ Thành công! Đạt +{newLevel}"
                    : downgraded
                        ? $"💔 Thất bại! Về +{newLevel}"
                        : "😞 Thất bại! Trang bị không đổi.";

                var templateIds = inventory
                    .Select(GetSlotItemTemplateId)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                var templateMap = await _db.ItemTemplates
                    .Where(t => templateIds.Contains(t.Id))
                    .ToDictionaryAsync(t => t.Id);

                var updatedInv = inventory
                    .OrderBy(GetSlotIndex)
                    .Select(s =>
                    {
                        int templateId = GetSlotItemTemplateId(s);
                        templateMap.TryGetValue(templateId, out var template);
                        string iconId = template != null && template.IdIcon > 0
                            ? template.IdIcon.ToString()
                            : GetSlotString(s, "iconId", "");

                        string itemCode = GetSlotString(s, "itemCode", template?.Name ?? $"Item #{templateId}");

                        return new
                        {
                            slotIndex = GetSlotIndex(s),
                            id = templateId,
                            amount = GetSlotAmount(s),
                            isEquipped = GetSlotBool(s, "isEquipped"),
                            isLocked = GetSlotBool(s, "isLocked") || (template?.IsLock ?? false),
                            upgradeLevel = GetSlotInt(s, "upgradeLevel"),
                            strOptions = GetSlotString(s, "strOptions", ""),
                            itemCode,
                            iconId,
                        };
                    })
                    .ToList();

                return Ok(new
                {
                    success,
                    downgraded,
                    newUpgradeLevel   = newLevel,
                    updatedStrOptions = newStrOptions,
                    silver            = info.Silver,
                    message           = msg,
                    actualRatePercent = serverRatePercent,
                    final_stats = new
                    {
                        hp         = upgFs.Hp,
                        max_hp     = upgFs.MaxHp,
                        mp         = upgFs.Mp,
                        max_mp     = upgFs.MaxMp,
                        attack     = upgFs.Attack,
                        defense    = upgFs.Defense,
                        move_speed = upgFs.MoveSpeed,
                    },
                    updatedInventory  = updatedInv
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi nâng cấp: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  HELPERS
        // ──────────────────────────────────────────────────────────────
        private static int GetOptionValueAt(Dictionary<string, object> opt, int upgradeLevel)
        {
            string strOpt = opt.ContainsKey("strOption") ? opt["strOption"]?.ToString() ?? "" : "";
            var parts = strOpt.Split(';');
            int idx = Math.Clamp(upgradeLevel, 0, parts.Length - 1);
            return int.TryParse(parts[idx], out int v) ? v : 0;
        }

        private static string RecalcStrOptions(string currentStrOptions, int newLevel)
        {
            if (string.IsNullOrEmpty(currentStrOptions)) return "";
            var pairs  = currentStrOptions.Split(';');
            var result = new List<string>();
            foreach (var pair in pairs)
            {
                var kv = pair.Split(',');
                if (kv.Length != 2 || !int.TryParse(kv[0], out int optId)) continue;

                var opt = OptionTemplates.FirstOrDefault(t => Convert.ToInt32(t["id"]) == optId);
                if (opt != null)
                    result.Add($"{optId},{GetOptionValueAt(opt, newLevel)}");
                else
                    result.Add(pair); // giữ nguyên nếu không tìm thấy template
            }
            return string.Join(";", result);
        }

        private static Dictionary<int, int> BuildRequestedSlotCountMap(IEnumerable<int> indices)
        {
            var result = new Dictionary<int, int>();
            if (indices == null)
                return result;

            foreach (int slotIndex in indices)
            {
                if (!result.ContainsKey(slotIndex))
                    result[slotIndex] = 0;
                result[slotIndex]++;
            }

            return result;
        }

        private static Dictionary<int, int> TryBuildRequestedSlotCountMap(
            JsonElement body,
            string usagePropertyName,
            string indexPropertyName,
            out string? error)
        {
            error = null;

            if (body.TryGetProperty(usagePropertyName, out var usagesProp) && usagesProp.ValueKind == JsonValueKind.Array)
            {
                var usageMap = new Dictionary<int, int>();
                foreach (var usage in usagesProp.EnumerateArray())
                {
                    if (usage.ValueKind != JsonValueKind.Object)
                    {
                        error = $"{usagePropertyName} phải là mảng object {{ slotIndex, count }}.";
                        return new Dictionary<int, int>();
                    }

                    if (!usage.TryGetProperty("slotIndex", out var slotProp) || !slotProp.TryGetInt32(out int slotIndex))
                    {
                        error = $"{usagePropertyName} đang thiếu slotIndex hợp lệ.";
                        return new Dictionary<int, int>();
                    }

                    int count = 1;
                    if (usage.TryGetProperty("count", out var countProp))
                    {
                        if (!countProp.TryGetInt32(out count) || count <= 0)
                        {
                            error = $"{usagePropertyName} có count không hợp lệ ở slot {slotIndex}.";
                            return new Dictionary<int, int>();
                        }
                    }

                    if (!usageMap.ContainsKey(slotIndex))
                        usageMap[slotIndex] = 0;
                    usageMap[slotIndex] += count;
                }

                return usageMap;
            }

            var indices = new List<int>();
            if (body.TryGetProperty(indexPropertyName, out var indicesProp) && indicesProp.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in indicesProp.EnumerateArray())
                {
                    if (!el.TryGetInt32(out int slotIndex))
                    {
                        error = $"{indexPropertyName} chứa slotIndex không hợp lệ.";
                        return new Dictionary<int, int>();
                    }
                    indices.Add(slotIndex);
                }
            }

            return BuildRequestedSlotCountMap(indices);
        }

        private static Dictionary<string, object>? FindInventorySlotByIndex(List<Dictionary<string, object>> inventory, int slotIndex)
        {
            return inventory.FirstOrDefault(s => GetSlotIndex(s) == slotIndex);
        }

        private static int GetSlotIndex(Dictionary<string, object> slot)
        {
            return GetSlotInt(slot, "slotIndex");
        }

        private static int GetSlotItemTemplateId(Dictionary<string, object> slot)
        {
            if (slot.ContainsKey("itemTemplateId"))
                return Convert.ToInt32(slot["itemTemplateId"]);
            if (slot.ContainsKey("id"))
                return Convert.ToInt32(slot["id"]);
            return 0;
        }

        private static int GetSlotAmount(Dictionary<string, object> slot)
        {
            if (slot.ContainsKey("amount"))
                return Convert.ToInt32(slot["amount"]);
            if (slot.ContainsKey("quantity"))
                return Convert.ToInt32(slot["quantity"]);
            return 1;
        }

        private static int GetSlotInt(Dictionary<string, object> slot, string key)
        {
            return slot.ContainsKey(key) ? Convert.ToInt32(slot[key]) : 0;
        }

        private static bool GetSlotBool(Dictionary<string, object> slot, string key)
        {
            return slot.ContainsKey(key) && Convert.ToBoolean(slot[key]);
        }

        private static string GetSlotString(Dictionary<string, object> slot, string key, string fallback = "")
        {
            if (!slot.ContainsKey(key) || slot[key] == null)
                return fallback;
            return slot[key]?.ToString() ?? fallback;
        }

        private static void ConsumeRequestedInventorySlots(List<Dictionary<string, object>> inventory, Dictionary<int, int> requests)
        {
            foreach (var request in requests)
            {
                var slot = FindInventorySlotByIndex(inventory, request.Key);
                if (slot == null)
                    continue;

                int remaining = GetSlotAmount(slot) - request.Value;
                if (remaining <= 0)
                {
                    inventory.Remove(slot);
                    continue;
                }

                slot["amount"] = remaining;
                slot["quantity"] = remaining;
            }
        }

        private static List<Dictionary<string, object>> ParseJsonList(string json)
        {
            var result = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(json) || json == "[]") return result;
            try
            {
                var raw = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json);
                if (raw == null) return result;
                foreach (var item in raw)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (var kvp in item)
                        dict[kvp.Key] = kvp.Value.ValueKind switch
                        {
                            JsonValueKind.Number => kvp.Value.TryGetInt32(out var iv) ? (object)iv : kvp.Value.GetDouble(),
                            JsonValueKind.String => kvp.Value.GetString() ?? "",
                            JsonValueKind.True   => true,
                            JsonValueKind.False  => false,
                            _                    => kvp.Value.ToString()
                        };
                    result.Add(dict);
                }
            }
            catch { }
            return result;
        }

        private static Dictionary<string, object> ParseEquipJson(string json)
        {
            var result = new Dictionary<string, object>();
            if (string.IsNullOrEmpty(json) || json == "{}") return result;
            try
            {
                var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (raw == null) return result;
                foreach (var kvp in raw)
                {
                    if (kvp.Value.ValueKind == JsonValueKind.Null) { result[kvp.Key] = null!; continue; }
                    if (kvp.Value.ValueKind == JsonValueKind.Object)
                    {
                        var d = new Dictionary<string, object>();
                        foreach (var p in kvp.Value.EnumerateObject())
                            d[p.Name] = p.Value.ValueKind switch
                            {
                                JsonValueKind.Number => p.Value.TryGetInt32(out var iv) ? (object)iv : p.Value.GetDouble(),
                                JsonValueKind.String => p.Value.GetString() ?? "",
                                JsonValueKind.True   => true,
                                JsonValueKind.False  => false,
                                _                    => p.Value.ToString()
                            };
                        result[kvp.Key] = d;
                    }
                }
            }
            catch { }
            return result;
        }
    }
}
