using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/gene")]
    [AllowAnonymous]
    public class GeneController : ControllerBase
    {
        private readonly GameDbContext _db;

        public GeneController(GameDbContext db)
        {
            _db = db;
        }

        // ──────────────────────────────────────────────────────────────
        //  Stat boost per tier đọc từ bảng gene_tier_stat_config (DB)
        //  Không còn hardcode — config qua DB/SQL migration.
        // ──────────────────────────────────────────────────────────────
        //  GET /api/gene/config?elementType=Fire&tier=1
        //  Trả về config nâng cấp gene cho (tier, elementType) hiện tại
        // ──────────────────────────────────────────────────────────────
        [HttpGet("config")]
        public async System.Threading.Tasks.Task<IActionResult> GetConfig(
            [FromQuery] string elementType,
            [FromQuery] int    tier)
        {
            if (string.IsNullOrWhiteSpace(elementType))
                return BadRequest("Thiếu elementType.");
            if (tier < 1 || tier > 4)
                return BadRequest("tier phải từ 1 đến 4.");

            var cfg = await _db.GeneUpgradeConfigs
                .FirstOrDefaultAsync(c => c.TierFrom == tier && c.ElementType == elementType);

            if (cfg == null)
                return NotFound($"Không có config gene cho {elementType} tier {tier}.");

            // Lấy tên + icon item từ item_template
            var item = await _db.ItemTemplates.FindAsync(cfg.ItemId);
            string itemName = item?.Name     ?? $"Item #{cfg.ItemId}";
            int    itemIcon = item?.IdIcon   ?? 0;

            // Stat bonus sẽ được thêm vào khi lên tier tiếp theo (đọc từ DB)
            int nextTier = tier + 1;
            var tierStat = await _db.GeneTierStatConfigs
                .FirstOrDefaultAsync(g => g.ElementType == elementType && g.TierTo == nextTier);

            // Skills sẽ được mở khoá ở tier mới
            var unlockSkills = await _db.SkillTemplates
                .Where(s => s.GeneTierRequired == nextTier &&
                            (s.ElementType == null || s.ElementType == elementType))
                .Select(s => new { s.SkillId, s.SkillName, s.ElementType, s.IconId })
                .ToListAsync();

            return Ok(new
            {
                tierFrom         = tier,
                tierTo           = nextTier,
                elementType,
                geneExpRequired  = cfg.GeneExpRequired,
                goldCost         = cfg.GoldCost,
                itemId           = cfg.ItemId,
                itemName,
                itemIcon,
                itemsMin         = cfg.ItemsMin,
                itemsNeeded      = cfg.ItemsNeeded,
                baseSuccessRate  = cfg.BaseSuccessRate,
                statBonus = tierStat != null ? new
                {
                    hp      = tierStat.HpBonus,
                    mp      = tierStat.MpBonus,
                    attack  = tierStat.AttackBonus,
                    defense = tierStat.DefenseBonus,
                } : new { hp = 0, mp = 0, attack = 0, defense = 0 },
                skillsToUnlock = unlockSkills,
            });
        }

        // ──────────────────────────────────────────────────────────────
        //  POST /api/gene/upgrade
        //  Body: { "playerId": 1, "itemCount": 3 }
        //  itemCount: số item muốn dùng (>= itemsMin, <= itemsNeeded)
        // ──────────────────────────────────────────────────────────────
        [HttpPost("upgrade")]
        public async System.Threading.Tasks.Task<IActionResult> UpgradeGene([FromBody] JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("playerId", out var pidProp))
                    return BadRequest("Thiếu playerId.");

                int playerId  = pidProp.GetInt32();
                int itemCount = body.TryGetProperty("itemCount", out var icProp) ? icProp.GetInt32() : 1;

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null)
                    return NotFound("Player không tồn tại.");

                var info = player.GetInfoChar();

                string elementType = info.ElementType;
                int    currentTier = info.GeneTier;

                if (currentTier >= 5)
                    return BadRequest("Gene đã đạt bậc tối đa (Tier 5).");

                // Lấy config từ DB
                var cfg = await _db.GeneUpgradeConfigs
                    .FirstOrDefaultAsync(c => c.TierFrom == currentTier && c.ElementType == elementType);

                if (cfg == null)
                    return BadRequest($"Không có config gene cho {elementType} tier {currentTier}.");

                // Kiểm tra gene_exp đủ chưa
                if (info.GeneExp < cfg.GeneExpRequired)
                    return BadRequest($"Cần {cfg.GeneExpRequired} gene exp. Hiện có: {info.GeneExp}.");

                // Kiểm tra đủ vàng
                if (info.Gold < cfg.GoldCost)
                    return BadRequest($"Không đủ vàng. Cần {cfg.GoldCost:N0}, hiện có {info.Gold:N0}.");

                // Clamp itemCount
                itemCount = Math.Clamp(itemCount, cfg.ItemsMin, cfg.ItemsNeeded);

                // Tìm item trong inventory
                var inventory = ParseJsonList(player.InventoryJson);
                int availableItems = inventory
                    .Where(s => s.ContainsKey("itemTemplateId") &&
                                Convert.ToInt32(s["itemTemplateId"]) == cfg.ItemId)
                    .Sum(s => s.ContainsKey("quantity") ? Convert.ToInt32(s["quantity"]) : 0);

                // Nếu không đủ, giảm về số có
                if (availableItems < cfg.ItemsMin)
                    return BadRequest($"Không đủ item (id={cfg.ItemId}). Cần {cfg.ItemsMin}, có {availableItems}.");

                itemCount = Math.Min(itemCount, availableItems);

                // Tỉ lệ thành công
                float successRate = cfg.BaseSuccessRate * Math.Min((float)itemCount / cfg.ItemsNeeded, 1f);
                successRate = Math.Clamp(successRate, 0f, 1f);
                bool success = new Random().NextDouble() < successRate;

                // Trừ vàng
                info.Gold -= cfg.GoldCost;

                // Trừ item đúng số lượng sử dụng — chỉ xoá slot nếu cạn hết (quantity→0)
                int toConsume = itemCount;
                foreach (var s in inventory)
                {
                    if (toConsume <= 0) break;
                    if (!s.ContainsKey("itemTemplateId")) continue;
                    if (Convert.ToInt32(s["itemTemplateId"]) != cfg.ItemId) continue;

                    int amt = s.ContainsKey("quantity") ? Convert.ToInt32(s["quantity"]) : 0;
                    int use = Math.Min(amt, toConsume);
                    s["quantity"] = amt - use;  // cập nhật quantity, có thể về 0
                    toConsume -= use;
                }
                // Chỉ xoá slot không được trang bị (isEquipped != true) và quantity <= 0
                inventory.RemoveAll(s =>
                    s.ContainsKey("quantity") &&
                    Convert.ToInt32(s["quantity"]) <= 0 &&
                    (!s.ContainsKey("isEquipped") || s["isEquipped"] is bool eq && !eq));

                // gene_exp: luôn trừ đúng số điểm cần dùng (không reset về 0)
                info.GeneExp = Math.Max(0, info.GeneExp - cfg.GeneExpRequired);

                var newlyUnlockedSkills = new List<object>();

                if (success)
                {
                    int newTier = currentTier + 1;
                    info.GeneTier = newTier;

                    // Đọc stat bonus từ DB (gene_tier_stat_config)
                    var tierStat = await _db.GeneTierStatConfigs
                        .FirstOrDefaultAsync(g => g.ElementType == elementType && g.TierTo == newTier);

                    if (tierStat != null)
                    {
                        info.MaxHp   += tierStat.HpBonus;
                        info.Hp       = info.MaxHp;   // hồi máu đầy khi lên tier
                        info.MaxMp   += tierStat.MpBonus;
                        info.Mp       = info.MaxMp;
                        info.Attack  += tierStat.AttackBonus;
                        info.Defense += tierStat.DefenseBonus;
                    }

                    // Mở khoá skills theo gene tier mới
                    var skillsToUnlock = await _db.SkillTemplates
                        .Where(sk => sk.GeneTierRequired == newTier &&
                                     (sk.ElementType == null || sk.ElementType == elementType))
                        .ToListAsync();

                    // Parse skills JSON hiện tại
                    var playerSkills = ParsePlayerSkills(player.SkillsJson);
                    foreach (var sk in skillsToUnlock)
                    {
                        if (!playerSkills.ContainsKey(sk.SkillId))
                        {
                            playerSkills[sk.SkillId] = 0;  // unlocked, level 0 (chưa nâng)
                            newlyUnlockedSkills.Add(new
                            {
                                skill_id   = sk.SkillId,
                                skill_name = sk.SkillName,
                                icon_id    = sk.IconId,
                            });
                        }
                    }
                    player.SkillsJson = SerializeSkills(playerSkills);
                }

                int newGoldAmount = info.Gold;
                player.SetInfoChar(info);
                player.InventoryJson = JsonSerializer.Serialize(inventory);
                player.UpdatedAt     = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                // Tính final_stats sau khi đã lưu (bao gồm cả equipment + potential)
                var finalStats = StatCalculator.Compute(info, player.EquipmentJson, player.PotentialStatsJson);

                // Lấy tierStat vừa được áp dụng (nếu success) để trả về cho client
                GeneTierStatConfig? appliedTierStat = null;
                if (success)
                    appliedTierStat = await _db.GeneTierStatConfigs
                        .FirstOrDefaultAsync(g => g.ElementType == elementType && g.TierTo == info.GeneTier);

                string message = success
                    ? $"✨ Gene {elementType} đã lên Tier {info.GeneTier}!"
                    : $"😞 Thất bại! Đã trừ {cfg.GeneExpRequired} gene exp.";

                return Ok(new
                {
                    success,
                    newGeneTier      = info.GeneTier,
                    newGeneExp       = info.GeneExp,
                    gold             = newGoldAmount,
                    message,
                    statBonus = appliedTierStat != null ? new
                    {
                        hp      = appliedTierStat.HpBonus,
                        mp      = appliedTierStat.MpBonus,
                        attack  = appliedTierStat.AttackBonus,
                        defense = appliedTierStat.DefenseBonus,
                    } : (object?)null,
                    // final_stats bao gồm base + equipment + potential — client dùng để update UI
                    final_stats = new
                    {
                        hp         = finalStats.Hp,
                        max_hp     = finalStats.MaxHp,
                        mp         = finalStats.Mp,
                        max_mp     = finalStats.MaxMp,
                        attack     = finalStats.Attack,
                        defense    = finalStats.Defense,
                        move_speed = finalStats.MoveSpeed,
                    },
                    newlyUnlockedSkills,
                    updatedInventory = inventory.Select(s => new
                    {
                        slotIndex      = s.ContainsKey("slotIndex")      ? Convert.ToInt32(s["slotIndex"])      : 0,
                        itemTemplateId = s.ContainsKey("itemTemplateId") ? Convert.ToInt32(s["itemTemplateId"]) : 0,
                        quantity       = s.ContainsKey("quantity")       ? Convert.ToInt32(s["quantity"])       : 0,
                        itemCode       = s.ContainsKey("itemCode")       ? s["itemCode"]?.ToString()            : "",
                        iconId         = s.ContainsKey("iconId")         ? s["iconId"]?.ToString()              : "",
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi nâng cấp gene: {ex.Message}");
            }
        }

        // ──────────────────────────────────────────────────────────────
        //  HELPERS
        // ──────────────────────────────────────────────────────────────

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
                            JsonValueKind.Number  => (object)kvp.Value.GetDouble(),
                            JsonValueKind.True    => true,
                            JsonValueKind.False   => false,
                            JsonValueKind.String  => kvp.Value.GetString() ?? "",
                            JsonValueKind.Null    => null!,
                            _                    => kvp.Value.GetRawText()
                        };
                    result.Add(dict);
                }
            }
            catch { }
            return result;
        }

        private static Dictionary<int, int> ParsePlayerSkills(string json)
        {
            var result = new Dictionary<int, int>();
            if (string.IsNullOrEmpty(json) || json == "[]") return result;
            try
            {
                var arr = JsonSerializer.Deserialize<List<JsonElement>>(json);
                if (arr == null) return result;
                foreach (var elem in arr)
                    if (elem.TryGetProperty("skill_id",      out var idP) &&
                        elem.TryGetProperty("current_level", out var lvP))
                        result[idP.GetInt32()] = lvP.GetInt32();
            }
            catch { }
            return result;
        }

        private static string SerializeSkills(Dictionary<int, int> skills)
        {
            var list = skills.Select(kv => new { skill_id = kv.Key, current_level = kv.Value }).ToList();
            return JsonSerializer.Serialize(list);
        }
    }
}
