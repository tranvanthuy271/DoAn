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
    [Authorize]
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

                if (ResolveAuthorizedPlayerId(pidProp.GetInt32(), out int playerId) is { } authError)
                    return authError;
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
                bool success = Random.Shared.NextDouble() < successRate;

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

        // ══════════════════════════════════════════════════════════════
        //  GET /api/gene/list
        //  Trả về tất cả gene của player: primary, secondary, hybrid status
        // ══════════════════════════════════════════════════════════════
        [HttpGet("list")]
        public async System.Threading.Tasks.Task<IActionResult> GetGeneList([FromQuery] int playerId)
        {
            if (ResolveAuthorizedPlayerId(playerId, out playerId) is { } authError)
                return authError;

            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();

            return Ok(new
            {
                primaryElement   = info.ElementType,
                primaryTier      = info.GeneTier,
                primaryExp       = info.GeneExp,
                secondaryElement = info.SecondaryElement,
                secondaryTier    = info.SecondaryGeneTier,
                secondaryExp     = info.SecondaryGeneExp,
                isHybrid         = info.IsHybrid,
                hybridName       = info.IsHybrid
                    ? GetHybridName(info.HybridElementA!, info.HybridElementB!, _db)
                    : null,
                hybridBonusTargets   = info.HybridBonusTargets,
                hybridImmuneElements = info.HybridImmuneElements,
                hybridAtkBonusPct    = info.HybridAtkBonusPct,
                canFuse = !info.IsHybrid
                    && info.GeneTier >= 5
                    && info.SecondaryElement != null
                    && info.SecondaryGeneTier >= 5,
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/gene/secondary/select
        //  Body: { "playerId": 1, "secondaryElement": "Water" }
        //  Chọn hệ gene thứ 2 lần đầu (chỉ được chọn 1 lần)
        // ══════════════════════════════════════════════════════════════
        [HttpPost("secondary/select")]
        public async System.Threading.Tasks.Task<IActionResult> SelectSecondaryGene([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("playerId", out var pidProp))
                    return BadRequest("Thiếu playerId.");
                if (!body.TryGetProperty("secondaryElement", out var elProp))
                    return BadRequest("Thiếu secondaryElement.");

                if (ResolveAuthorizedPlayerId(pidProp.GetInt32(), out int playerId) is { } authError)
                    return authError;
                string secondary = elProp.GetString() ?? "";

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null) return NotFound("Player không tồn tại.");

                var info = player.GetInfoChar();

                if (info.SecondaryElement != null)
                    return BadRequest($"Đã chọn hệ phụ: {info.SecondaryElement}. Không thể thay đổi.");

                // Kiểm tra hệ phụ phải đúng đối tác cố định (Hỏa↔Thổ | Thủy↔Mộc | Kim↔Phong)
                if (!PartnerMap.TryGetValue(info.ElementType, out var expectedPartner))
                    return BadRequest($"Hệ chính {info.ElementType} không hỗ trợ Hybrid.");

                if (!secondary.Equals(expectedPartner, StringComparison.OrdinalIgnoreCase))
                    return BadRequest($"Hệ {info.ElementType} chỉ có thể kết hợp với hệ {expectedPartner}. Không thể chọn {secondary}.");

                info.SecondaryElement    = secondary;
                info.SecondaryGeneTier   = 1;
                info.SecondaryGeneExp    = 0;

                player.SetInfoChar(info);
                player.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                return Ok(new
                {
                    success          = true,
                    primaryElement   = info.ElementType,
                    secondaryElement = info.SecondaryElement,
                    secondaryTier    = info.SecondaryGeneTier,
                    message          = $"🌟 Đã chọn hệ phụ: {secondary}! Bắt đầu nâng cấp hệ phụ.",
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi chọn hệ phụ: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/gene/multi/config
        //  Lấy config nâng cấp hệ phụ
        // ══════════════════════════════════════════════════════════════
        [HttpGet("multi/config")]
        public async System.Threading.Tasks.Task<IActionResult> GetMultiConfig(
            [FromQuery] string elementType,
            [FromQuery] int    tier)
        {
            if (string.IsNullOrWhiteSpace(elementType))
                return BadRequest("Thiếu elementType.");
            if (tier < 1 || tier > 4)
                return BadRequest("tier phải từ 1 đến 4.");

            var cfg = await _db.GeneMultiConfigs
                .FirstOrDefaultAsync(c => c.TierFrom == tier && c.ElementType == elementType);

            if (cfg == null)
                return NotFound($"Không có config nâng cấp hệ phụ {elementType} tier {tier}.");

            var item = await _db.ItemTemplates.FindAsync(cfg.ItemId);

            int nextTier = tier + 1;
            var tierStat = await _db.GeneTierStatConfigs
                .FirstOrDefaultAsync(g => g.ElementType == elementType && g.TierTo == nextTier);

            return Ok(new
            {
                tierFrom        = tier,
                tierTo          = nextTier,
                elementType,
                geneExpRequired = cfg.GeneExpRequired,
                goldCost        = cfg.GoldCost,
                itemId          = cfg.ItemId,
                itemName        = item?.Name ?? $"Item #{cfg.ItemId}",
                itemIcon        = item?.IdIcon ?? 0,
                itemsMin        = cfg.ItemsMin,
                itemsNeeded     = cfg.ItemsNeeded,
                baseSuccessRate = cfg.BaseSuccessRate,
                note            = "Chi phí hệ phụ cao hơn hệ chính khoảng 20%",
                statBonus = tierStat != null ? new
                {
                    hp      = tierStat.HpBonus,
                    mp      = tierStat.MpBonus,
                    attack  = tierStat.AttackBonus,
                    defense = tierStat.DefenseBonus,
                } : new { hp = 0, mp = 0, attack = 0, defense = 0 },
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/gene/secondary/upgrade
        //  Body: { "playerId": 1, "itemCount": 3 }
        //  Nâng cấp hệ gene thứ 2 (secondary)
        // ══════════════════════════════════════════════════════════════
        [HttpPost("secondary/upgrade")]
        public async System.Threading.Tasks.Task<IActionResult> UpgradeSecondaryGene([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("playerId", out var pidProp))
                    return BadRequest("Thiếu playerId.");

                if (ResolveAuthorizedPlayerId(pidProp.GetInt32(), out int playerId) is { } authError)
                    return authError;
                int itemCount = body.TryGetProperty("itemCount", out var icProp) ? icProp.GetInt32() : 1;

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null) return NotFound("Player không tồn tại.");

                var info = player.GetInfoChar();

                if (info.SecondaryElement == null)
                    return BadRequest("Chưa chọn hệ phụ. Gọi POST /api/gene/secondary/select trước.");

                string secondary    = info.SecondaryElement;
                int    currentTier  = info.SecondaryGeneTier ?? 1;
                int    currentExp   = info.SecondaryGeneExp  ?? 0;

                if (currentTier >= 5)
                    return BadRequest($"Hệ phụ {secondary} đã đạt Tier 5 tối đa.");

                var cfg = await _db.GeneMultiConfigs
                    .FirstOrDefaultAsync(c => c.TierFrom == currentTier && c.ElementType == secondary);

                if (cfg == null)
                    return BadRequest($"Không có config nâng cấp cho {secondary} tier {currentTier}.");

                if (currentExp < cfg.GeneExpRequired)
                    return BadRequest($"Cần {cfg.GeneExpRequired} gene exp hệ phụ. Hiện có: {currentExp}.");

                if (info.Gold < cfg.GoldCost)
                    return BadRequest($"Không đủ vàng. Cần {cfg.GoldCost:N0}, hiện có {info.Gold:N0}.");

                itemCount = Math.Clamp(itemCount, cfg.ItemsMin, cfg.ItemsNeeded);

                var inventory = ParseJsonList(player.InventoryJson);
                int available = inventory
                    .Where(s => s.ContainsKey("itemTemplateId") &&
                                Convert.ToInt32(s["itemTemplateId"]) == cfg.ItemId)
                    .Sum(s => s.ContainsKey("quantity") ? Convert.ToInt32(s["quantity"]) : 0);

                if (available < cfg.ItemsMin)
                    return BadRequest($"Không đủ item (id={cfg.ItemId}). Cần {cfg.ItemsMin}, có {available}.");

                itemCount = Math.Min(itemCount, available);

                float successRate = cfg.BaseSuccessRate * Math.Min((float)itemCount / cfg.ItemsNeeded, 1f);
                successRate = Math.Clamp(successRate, 0f, 1f);
                bool  success = Random.Shared.NextDouble() < successRate;

                info.Gold         -= cfg.GoldCost;
                info.SecondaryGeneExp = Math.Max(0, currentExp - cfg.GeneExpRequired);

                // Trừ item
                int toConsume = itemCount;
                foreach (var s in inventory)
                {
                    if (toConsume <= 0) break;
                    if (!s.ContainsKey("itemTemplateId")) continue;
                    if (Convert.ToInt32(s["itemTemplateId"]) != cfg.ItemId) continue;
                    int amt = s.ContainsKey("quantity") ? Convert.ToInt32(s["quantity"]) : 0;
                    int use = Math.Min(amt, toConsume);
                    s["quantity"] = amt - use;
                    toConsume    -= use;
                }
                inventory.RemoveAll(s =>
                    s.ContainsKey("quantity") &&
                    Convert.ToInt32(s["quantity"]) <= 0 &&
                    (!s.ContainsKey("isEquipped") || s["isEquipped"] is bool eq && !eq));

                if (success)
                {
                    int newTier = currentTier + 1;
                    info.SecondaryGeneTier = newTier;

                    // stat bonus từ hệ phụ — nhỏ hơn hệ chính (50%)
                    var tierStat = await _db.GeneTierStatConfigs
                        .FirstOrDefaultAsync(g => g.ElementType == secondary && g.TierTo == newTier);

                    if (tierStat != null)
                    {
                        info.MaxHp   += tierStat.HpBonus     / 2;
                        info.Hp       = info.MaxHp;
                        info.MaxMp   += tierStat.MpBonus     / 2;
                        info.Mp       = info.MaxMp;
                        info.Attack  += tierStat.AttackBonus / 2;
                        info.Defense += tierStat.DefenseBonus/ 2;
                    }
                }

                player.SetInfoChar(info);
                player.InventoryJson = System.Text.Json.JsonSerializer.Serialize(inventory);
                player.UpdatedAt     = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var finalStats = GameServerApi.Models.Services.StatCalculator
                    .Compute(info, player.EquipmentJson, player.PotentialStatsJson);

                bool canFuse = !info.IsHybrid
                    && info.GeneTier >= 5
                    && info.SecondaryGeneTier >= 5;

                string msg = success
                    ? $"✨ Hệ phụ {secondary} đã lên Tier {info.SecondaryGeneTier}!"
                      + (canFuse ? " 🔥 Đủ điều kiện Hybrid Fusion!" : "")
                    : $"😞 Thất bại! Gene exp hệ phụ reset về 0.";

                return Ok(new
                {
                    success,
                    secondaryElement = secondary,
                    newSecondaryTier = info.SecondaryGeneTier,
                    newSecondaryExp  = info.SecondaryGeneExp,
                    gold             = info.Gold,
                    message          = msg,
                    canFuse,
                    final_stats = new
                    {
                        hp      = finalStats.Hp,      max_hp  = finalStats.MaxHp,
                        mp      = finalStats.Mp,      max_mp  = finalStats.MaxMp,
                        attack  = finalStats.Attack,  defense = finalStats.Defense,
                    },
                    updatedInventory = inventory.Select(s => new
                    {
                        slotIndex      = s.ContainsKey("slotIndex")      ? Convert.ToInt32(s["slotIndex"])      : 0,
                        itemTemplateId = s.ContainsKey("itemTemplateId") ? Convert.ToInt32(s["itemTemplateId"]) : 0,
                        quantity       = s.ContainsKey("quantity")       ? Convert.ToInt32(s["quantity"])       : 0,
                    }).ToList(),
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi nâng cấp hệ phụ: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/gene/hybrid/config?playerId=1
        //  Lấy config hybrid + kiểm tra điều kiện fusion
        // ══════════════════════════════════════════════════════════════
        [HttpGet("hybrid/config")]
        public async System.Threading.Tasks.Task<IActionResult> GetHybridConfig([FromQuery] int playerId)
        {
            if (ResolveAuthorizedPlayerId(playerId, out playerId) is { } authError)
                return authError;

            var player = await _db.PlayerData.FindAsync(playerId);
            if (player == null) return NotFound("Player không tồn tại.");

            var info = player.GetInfoChar();

            if (info.IsHybrid)
                return BadRequest("Player đã là Hybrid Gene rồi.");

            if (info.SecondaryElement == null)
                return BadRequest("Chưa chọn hệ phụ.");

            // Validate cặp kết hợp hợp lệ (chỉ 3 cặp: Hỏa↔Thổ | Thủy↔Mộc | Kim↔Phong)
            if (!IsValidPair(info.ElementType, info.SecondaryElement))
                return BadRequest($"Cặp {info.ElementType} + {info.SecondaryElement} không phải cặp Hybrid hợp lệ. Chỉ cho phép: Hỏa↔Thổ, Thủy↔Mộc, Kim↔Phong.");

            if (info.GeneTier < 5)
                return BadRequest($"Hệ chính {info.ElementType} cần đạt Tier 5. Hiện tại: Tier {info.GeneTier}.");

            if ((info.SecondaryGeneTier ?? 0) < 5)
                return BadRequest($"Hệ phụ {info.SecondaryElement} cần đạt Tier 5. Hiện tại: Tier {info.SecondaryGeneTier}.");

            var (a, b) = GeneHybridConfig.NormalizeKey(info.ElementType, info.SecondaryElement);
            var cfg = await _db.GeneHybridConfigs
                .FirstOrDefaultAsync(h => h.ElementA == a && h.ElementB == b);

            if (cfg == null)
                return NotFound($"Không tìm thấy config hybrid cho {info.ElementType} + {info.SecondaryElement}.");

            var inventory = ParseJsonList(player.InventoryJson);
            // Dùng lõi đột biến theo hệ CHÍNH (primary element) vì đây là gene mutation của nhân vật
            int fusionItemId = GetFusionItemId(info.ElementType!);
            var fusionItem   = await _db.ItemTemplates.FindAsync(fusionItemId);
            int available = inventory
                .Where(s => s.ContainsKey("itemTemplateId") &&
                            Convert.ToInt32(s["itemTemplateId"]) == fusionItemId)
                .Sum(s => s.ContainsKey("quantity") ? Convert.ToInt32(s["quantity"]) : 0);

            return Ok(new
            {
                hybridName        = cfg.HybridName,
                hybridDescription = cfg.HybridDescription,
                elementA          = info.ElementType,
                elementB          = info.SecondaryElement,
                elementATier      = info.GeneTier,
                elementBTier      = info.SecondaryGeneTier ?? 0,
                bonusTargets      = cfg.GetBonusTargets(),
                immuneElements    = cfg.GetImmuneElements(),
                atkBonusPercent   = cfg.AtkBonusPercent,
                fusionGoldCost    = cfg.FusionGoldCost,
                fusionItemId,
                fusionItemName    = fusionItem?.Name ?? $"Lõi Đột Biến ({info.ElementType})",
                fusionItemIcon    = fusionItem?.IdIcon ?? 0,
                fusionItemCount   = cfg.FusionItemCount,
                availableItems    = available,
                itemSufficient    = available >= cfg.FusionItemCount,
                goldSufficient    = info.Gold >= cfg.FusionGoldCost,
                playerGold        = info.Gold,
                canFuse           = available >= cfg.FusionItemCount && info.Gold >= cfg.FusionGoldCost,
                statBonus = new
                {
                    hp      = cfg.StatBonusHp,
                    mp      = cfg.StatBonusMp,
                    attack  = cfg.StatBonusAtk,
                    defense = cfg.StatBonusDef,
                },
            });
        }

        // ══════════════════════════════════════════════════════════════
        //  POST /api/gene/hybrid/fuse
        //  Body: { "playerId": 1, "itemCount": 5 }
        //  Fusion 2 hệ gene Tier 5 thành Hybrid Gene
        // ══════════════════════════════════════════════════════════════
        [HttpPost("hybrid/fuse")]
        public async System.Threading.Tasks.Task<IActionResult> FuseHybridGene([FromBody] System.Text.Json.JsonElement body)
        {
            try
            {
                if (!body.TryGetProperty("playerId", out var pidProp))
                    return BadRequest("Thiếu playerId.");

                if (ResolveAuthorizedPlayerId(pidProp.GetInt32(), out int playerId) is { } authError)
                    return authError;
                int itemCount = body.TryGetProperty("itemCount", out var icProp) ? icProp.GetInt32() : 0;

                var player = await _db.PlayerData.FindAsync(playerId);
                if (player == null) return NotFound("Player không tồn tại.");

                var info = player.GetInfoChar();

                if (info.IsHybrid)
                    return BadRequest("Player đã là Hybrid Gene rồi.");

                if (info.SecondaryElement == null)
                    return BadRequest("Chưa chọn hệ phụ.");

                // Validate cặp kết hợp hợp lệ (chỉ 3 cặp: Hỏa↔Thổ | Thủy↔Mộc | Kim↔Phong)
                if (!IsValidPair(info.ElementType, info.SecondaryElement))
                    return BadRequest($"Cặp {info.ElementType} + {info.SecondaryElement} không phải cặp Hybrid hợp lệ. Chỉ cho phép: Hỏa↔Thổ, Thủy↔Mộc, Kim↔Phong.");

                if (info.GeneTier < 5)
                    return BadRequest($"Hệ chính {info.ElementType} cần Tier 5. Hiện: Tier {info.GeneTier}.");

                if ((info.SecondaryGeneTier ?? 0) < 5)
                    return BadRequest($"Hệ phụ {info.SecondaryElement} cần Tier 5. Hiện: Tier {info.SecondaryGeneTier}.");

                var (a, b) = GeneHybridConfig.NormalizeKey(info.ElementType, info.SecondaryElement);
                var cfg = await _db.GeneHybridConfigs
                    .FirstOrDefaultAsync(h => h.ElementA == a && h.ElementB == b);

                if (cfg == null)
                    return NotFound($"Không tìm thấy config hybrid cho {info.ElementType} + {info.SecondaryElement}.");

                // Kiểm tra vàng
                if (info.Gold < cfg.FusionGoldCost)
                    return BadRequest($"Không đủ vàng. Cần {cfg.FusionGoldCost:N0}, có {info.Gold:N0}.");

                // Kiểm tra item — lõi đột biến theo hệ CHÍNH (primary element)
                var inventory = ParseJsonList(player.InventoryJson);
                int fusionItemId = GetFusionItemId(info.ElementType!);
                int available = inventory
                    .Where(s => s.ContainsKey("itemTemplateId") &&
                                Convert.ToInt32(s["itemTemplateId"]) == fusionItemId)
                    .Sum(s => s.ContainsKey("quantity") ? Convert.ToInt32(s["quantity"]) : 0);

                if (available < cfg.FusionItemCount)
                    return BadRequest($"Cần {cfg.FusionItemCount}x lõi đột biến hệ {info.ElementType} (id={fusionItemId}), có {available}.");

                // Trừ vàng
                info.Gold -= cfg.FusionGoldCost;

                // Trừ item — dùng đúng id lõi theo hệ phụ
                int toConsume = cfg.FusionItemCount;
                foreach (var s in inventory)
                {
                    if (toConsume <= 0) break;
                    if (!s.ContainsKey("itemTemplateId")) continue;
                    if (Convert.ToInt32(s["itemTemplateId"]) != fusionItemId) continue;
                    int amt = s.ContainsKey("quantity") ? Convert.ToInt32(s["quantity"]) : 0;
                    int use = Math.Min(amt, toConsume);
                    s["quantity"] = amt - use;
                    toConsume    -= use;
                }
                inventory.RemoveAll(s =>
                    s.ContainsKey("quantity") &&
                    Convert.ToInt32(s["quantity"]) <= 0 &&
                    (!s.ContainsKey("isEquipped") || s["isEquipped"] is bool eq && !eq));

                // Áp dụng Hybrid
                info.IsHybrid            = true;
                info.HybridElementA      = info.ElementType;
                info.HybridElementB      = info.SecondaryElement;
                info.HybridBonusTargets  = cfg.BonusTargetElements;  // CSV string
                info.HybridImmuneElements= cfg.ImmuneElements;       // CSV string
                info.HybridAtkBonusPct   = cfg.AtkBonusPercent;
                info.HybridId            = cfg.HybridId;
                info.HybridPrefabPath    = cfg.PrefabPath;

                // Stat bonus fusion
                info.MaxHp   += cfg.StatBonusHp;
                info.Hp       = info.MaxHp;
                info.MaxMp   += cfg.StatBonusMp;
                info.Mp       = info.MaxMp;
                info.Attack  += cfg.StatBonusAtk;
                info.Defense += cfg.StatBonusDef;

                // Cập nhật skills: giữ tối đa cfg.PrimarySkillKeepCount skill của hệ chính + thêm combo skill
                var hybridSkillRow = await _db.GeneHybridSkills
                    .FirstOrDefaultAsync(hs => hs.HybridId == cfg.HybridId);

                if (hybridSkillRow != null)
                {
                    // Lấy tất cả skill_id thuộc hệ chính từ skill_template
                    var primaryElementSkillIds = await _db.SkillTemplates
                        .Where(st => st.ElementType == info.ElementType)
                        .Select(st => st.SkillId)
                        .ToListAsync();

                    // Parse skills hiện tại của player (dict: skill_id → level)
                    var playerSkills = ParsePlayerSkills(player.SkillsJson);

                    // Lọc chỉ giữ skill thuộc hệ chính, tối đa PrimarySkillKeepCount (=3)
                    var keptPrimarySkills = playerSkills
                        .Where(kv => primaryElementSkillIds.Contains(kv.Key))
                        .OrderBy(kv => kv.Key)
                        .Take(cfg.PrimarySkillKeepCount)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);

                    // Tìm skill_id của hybrid skill trong skill_template
                    var hybridTemplate = await _db.SkillTemplates
                        .FirstOrDefaultAsync(st => st.SkillCode == hybridSkillRow.SkillCode);
                    if (hybridTemplate != null)
                        keptPrimarySkills[hybridTemplate.SkillId] = 1;

                    player.SkillsJson = SerializeSkills(keptPrimarySkills);
                }

                player.SetInfoChar(info);
                player.InventoryJson = System.Text.Json.JsonSerializer.Serialize(inventory);
                player.UpdatedAt     = DateTime.UtcNow;
                await _db.SaveChangesAsync();

                var finalStats = GameServerApi.Models.Services.StatCalculator
                    .Compute(info, player.EquipmentJson, player.PotentialStatsJson);

                string comboSkillCode = hybridSkillRow?.SkillCode ?? "";

                return Ok(new
                {
                    success           = true,
                    hybridName        = cfg.HybridName,
                    hybridDescription = cfg.HybridDescription,
                    hybridId          = cfg.HybridId,
                    hybridElementA    = info.HybridElementA,
                    hybridElementB    = info.HybridElementB,
                    prefabPath        = cfg.PrefabPath,
                    comboSkillCode,
                    bonusTargets      = cfg.GetBonusTargets(),
                    immuneElements    = cfg.GetImmuneElements(),
                    atkBonusPercent   = cfg.AtkBonusPercent,
                    statBonus = new
                    {
                        hp      = cfg.StatBonusHp,
                        mp      = cfg.StatBonusMp,
                        attack  = cfg.StatBonusAtk,
                        defense = cfg.StatBonusDef,
                    },
                    gold      = info.Gold,
                    message   = $"🌟🔥 HYBRID FUSION THÀNH CÔNG! {cfg.HybridName} đã thức tỉnh!",
                    final_stats = new
                    {
                        hp      = finalStats.Hp,      max_hp  = finalStats.MaxHp,
                        mp      = finalStats.Mp,      max_mp  = finalStats.MaxMp,
                        attack  = finalStats.Attack,  defense = finalStats.Defense,
                    },
                    updatedInventory = inventory.Select(s => new
                    {
                        slotIndex      = s.ContainsKey("slotIndex")      ? Convert.ToInt32(s["slotIndex"])      : 0,
                        itemTemplateId = s.ContainsKey("itemTemplateId") ? Convert.ToInt32(s["itemTemplateId"]) : 0,
                        quantity       = s.ContainsKey("quantity")       ? Convert.ToInt32(s["quantity"])       : 0,
                    }).ToList(),
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Lỗi Hybrid Fusion: {ex.Message}");
            }
        }

        // ══════════════════════════════════════════════════════════════
        //  GET /api/gene/ultimate/config?elementType=Fire&playerId=1
        //  Trả về config Gene Tối Thượng + tiến trình hiện tại của player (nếu có playerId).
        // ══════════════════════════════════════════════════════════════
        [HttpGet("ultimate/config")]
        public async System.Threading.Tasks.Task<IActionResult> GetUltimateConfig(
            [FromQuery] string? elementType,
            [FromQuery] int?    playerId)
        {
            var cfg = GameServerApi.Models.Services.GeneUltimateService
                .GetConfig(elementType);

            int  currentExp = 0;
            bool isUltimate = false;
            bool isHybrid   = false;
            if (playerId.HasValue)
            {
                if (ResolveAuthorizedPlayerId(playerId.Value, out int authorizedPlayerId) is { } authError)
                    return authError;

                var player = await _db.PlayerData.FindAsync(authorizedPlayerId);
                if (player != null)
                {
                    var info = player.GetInfoChar();
                    currentExp = info.UltimateGeneExp;
                    isUltimate = info.IsUltimate;
                    isHybrid   = info.IsHybrid;
                }
            }

            return Ok(new
            {
                ultimateExpRequired = cfg.UltimateExpRequired,
                statMultiplier      = cfg.StatMultiplier,
                auraPrefabPath      = cfg.AuraPrefabPath,
                currentUltimateExp  = currentExp,
                isUltimate,
                isHybrid,
            });
        }

        // ──────────────────────────────────────────────────────────────
        //  HELPERS
        // ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Bảng cặp kết hợp hợp lệ (bidirectional): chỉ 3 cặp được phép Hybrid Fusion.
        /// Hỏa↔Thổ | Thủy↔Mộc | Kim↔Phong
        /// </summary>
        private IActionResult? ResolveAuthorizedPlayerId(int requestedPlayerId, out int playerId)
        {
            playerId = requestedPlayerId;

            if (User.IsInRole("GameServer"))
                return null;

            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "user_id")?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            if (requestedPlayerId != userId)
                return Forbid();

            playerId = userId;
            return null;
        }

        private static readonly Dictionary<string, string> PartnerMap
            = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Fire"]  = "Earth", ["Earth"] = "Fire",
            ["Water"] = "Wood",  ["Wood"]  = "Water",
            ["Metal"] = "Wind",  ["Wind"]  = "Metal",
        };

        /// <summary>Kiểm tra cặp (primary, secondary) có hợp lệ không.</summary>
        private static bool IsValidPair(string primary, string secondary)
            => PartnerMap.TryGetValue(primary, out var expected)
               && expected.Equals(secondary, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Map: tên hệ (secondary element) → item_id lõi đột biến tương ứng.
        /// Item 31 (generic) chỉ là fallback kế thừa cũ.
        /// </summary>
        private static readonly Dictionary<string, int> ElementFusionItemMap
            = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Fire"]  = 47,
            ["Water"] = 48,
            ["Earth"] = 49,
            ["Metal"] = 50,
            ["Wood"]  = 51,
            ["Wind"]  = 52,
        };

        /// <summary>Trả về item_id lõi đột biến của hệ secondaryElement.</summary>
        private static int GetFusionItemId(string secondaryElement)
            => ElementFusionItemMap.TryGetValue(secondaryElement, out var id) ? id : 31;

        private static string GetHybridName(string elemA, string elemB, GameServerApi.Data.GameDbContext db)
        {
            var (a, b) = GeneHybridConfig.NormalizeKey(elemA, elemB);
            var cfg = db.GeneHybridConfigs.FirstOrDefault(h => h.ElementA == a && h.ElementB == b);
            return cfg?.HybridName ?? $"{elemA}+{elemB} Hybrid";
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
                {
                    if (!elem.TryGetProperty("skill_id", out var idP)) continue;
                    int skillId = idP.GetInt32();
                    int level = 0;
                    if (elem.TryGetProperty("current_level", out var lvP))
                        level = lvP.GetInt32();
                    else if (elem.TryGetProperty("lv", out var lvP2))
                        level = lvP2.GetInt32();
                    result[skillId] = level;
                }
            }
            catch { }
            return result;
        }

        /// <summary>Parse skills_json (list of {skillCode, currentLevel, isEquipped, slotIndex}) gốc.</summary>
        private static List<Dictionary<string, object>> ParseSkillsJsonRaw(string json)
            => ParseJsonList(json);

        private static string SerializeSkills(Dictionary<int, int> skills)
        {
            var list = skills.Select(kv => new { skill_id = kv.Key, current_level = kv.Value }).ToList();
            return JsonSerializer.Serialize(list);
        }
    }
}
