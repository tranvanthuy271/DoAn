using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameServerApi.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/upgrade")]
    [AllowAnonymous]
    public class UpgradeController : ControllerBase
    {
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
        public static readonly List<Dictionary<string, object>> OptionTemplates = new()
        {
            new() { ["id"]=1, ["name"]="Tấn công: +#",  ["type"]=0, ["level"]=0, ["strOption"]="10;12;14;16;18;20;23;26;29;32;36;40;44;48;52;56;60;65;70;75" },
            new() { ["id"]=2, ["name"]="Phòng thủ: +#", ["type"]=2, ["level"]=0, ["strOption"]="8;9;10;11;12;13;14;15;16;17;18;19;20;22;24;26;28;30;32;35" },
            new() { ["id"]=3, ["name"]="HP tối đa: +#",  ["type"]=2, ["level"]=0, ["strOption"]="30;33;36;39;42;45;48;51;54;57;60;63;66;69;72;75;78;81;84;90" },
            new() { ["id"]=4, ["name"]="Tốc độ: +#",    ["type"]=2, ["level"]=0, ["strOption"]="5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20;21;22;23;25" },
            new() { ["id"]=5, ["name"]="Tấn công: +#",  ["type"]=3, ["level"]=4, ["strOption"]="0;0;0;0;5;6;7;8;9;10;11;12;13;14;15;16;17;18;19;20" },
            new() { ["id"]=6, ["name"]="Phòng thủ: +#", ["type"]=4, ["level"]=8, ["strOption"]="0;0;0;0;0;0;0;0;5;6;7;8;9;10;11;12;13;14;15;16" },
        };

        // strOptions mặc định ở bậc +0 cho từng item template
        // format: "optId,value;optId,value;..."
        public static readonly Dictionary<int, string> DefaultStrOptions = new()
        {
            [100] = "3,30",       // Mũ Da Nam:      HP+30
            [200] = "1,10",       // Kiếm Hỏa Sơ:   ATK+10
            [110] = "2,8;3,30",   // Áo Da Nam:      DEF+8, HP+30
            [130] = "2,8",        // Quần Da Nam:    DEF+8
            [150] = "4,5",        // Giày Da Nam:    SPD+5
            [140] = "3,30",       // Nhẫn Đá:        HP+30
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
            if (targetLevel < 1 || targetLevel > 20)
                return BadRequest("targetLevel phải từ 1 đến 20.");

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

                if (targetLevel > 20)
                    return BadRequest("Trang bị đã đạt bậc tối đa (+20).");

                // Lấy config từ DB
                var cfg = await _db.EquipmentUpgradeConfigs.FindAsync(targetLevel);
                if (cfg == null)
                    return BadRequest($"Không có config nâng cấp cho bậc +{targetLevel}.");

                // Đọc stoneSlotIndices từ request
                var stoneIndices = new List<int>();
                if (body.TryGetProperty("stoneSlotIndices", out var stonesProp))
                    foreach (var el in stonesProp.EnumerateArray())
                        stoneIndices.Add(el.GetInt32());

                // Đếm số lượng từng loại đá
                int upgradeStoneCount = 0;
                int luckyStoneCount   = 0;
                bool hasProtection    = false;

                foreach (int idx in stoneIndices)
                {
                    var stone = inventory.FirstOrDefault(s =>
                        s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == idx);
                    if (stone == null) continue;

                    int stoneItemId = stone.ContainsKey("itemTemplateId") ? Convert.ToInt32(stone["itemTemplateId"]) : 0;
                    if (stoneItemId == cfg.StoneId) upgradeStoneCount++;
                    else if (stoneItemId == 8)      luckyStoneCount++;
                    else if (stoneItemId == 9)      hasProtection = true;
                }

                if (upgradeStoneCount < cfg.StoneMin)
                    return BadRequest($"Cần ít nhất {cfg.StoneMin} đá nâng cấp. Hiện có: {upgradeStoneCount}.");

                // Tỉ lệ thành công
                float stoneRatio = Math.Min((float)upgradeStoneCount / cfg.StoneNeeded, 1f);
                float rate       = cfg.BaseSuccessRate * stoneRatio + luckyStoneCount * 0.15f;
                rate = Math.Clamp(rate, 0f, 1f);

                bool success    = new Random().NextDouble() < rate;
                bool downgraded = false;
                int  newLevel   = currentLevel;

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

                // Trừ đá khỏi inventory (giảm amount từng cái, xóa khi hết)
                foreach (int idx in stoneIndices)
                {
                    var stone = inventory.FirstOrDefault(s => s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == idx);
                    if (stone == null) continue;
                    int amt = stone.ContainsKey("amount") ? Convert.ToInt32(stone["amount"]) : 1;
                    if (amt <= 1)
                        inventory.RemoveAll(s => s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == idx);
                    else
                        stone["amount"] = amt - 1;
                }

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

                string msg = success
                    ? $"✨ Thành công! Đạt +{newLevel}"
                    : downgraded
                        ? $"💔 Thất bại! Về +{newLevel}"
                        : "😞 Thất bại! Trang bị không đổi.";

                // Build inventory response (chỉ fields client cần)
                var updatedInv = inventory.Select(s => new Dictionary<string, object>
                {
                    ["slotIndex"]      = s.ContainsKey("slotIndex")      ? s["slotIndex"]      : 0,
                    ["itemTemplateId"] = s.ContainsKey("itemTemplateId") ? s["itemTemplateId"] : 0,
                    ["upgradeLevel"]   = s.ContainsKey("upgradeLevel")   ? s["upgradeLevel"]   : 0,
                    ["strOptions"]     = s.ContainsKey("strOptions")     ? s["strOptions"] ?? "" : "",
                    ["amount"]         = s.ContainsKey("amount")         ? s["amount"] : (s.ContainsKey("quantity") ? s["quantity"] : 1),
                    ["isEquipped"]     = s.ContainsKey("isEquipped")     ? s["isEquipped"]     : false,
                }).ToList();

                return Ok(new
                {
                    success,
                    downgraded,
                    newUpgradeLevel   = newLevel,
                    updatedStrOptions = newStrOptions,
                    silver            = info.Silver,
                    message           = msg,
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
