using UnityEngine;

// Lớp tiện ích tính toán sát thương tập trung cho toàn bộ nhánh combat.
// Static-only, không cần instance. Mỗi component gọi method tương ứng,
// truyền tham số đã có sẵn — tránh trùng lặp công thức giữa các handler.
// Các nhánh sử dụng:
// CalcPlayerAttackDamage        — PlayerCombat, FireballDamage
// CalcEnemyReceivedDamage       — MobPatrolAI.TakeDamageWithElement
// CalcBossReceivedDamage        — BossController.HandleBeforeTakeDamage
// CalcDungeonEnemyReceivedDamage — DungeonEnemyRuntimeStats.ResolveIncomingDamage
// CalcPlayerReceivedElementDamage — NetworkPlayerHealth.TakeDamageWithElementInternal
public static class DamageCalculator
{
    //  Người chơi TẤN CÔNG enemy

    // Tính sát thương người chơi gây ra (đánh thường hoặc projectile), có xét:
    // (1) AttackBuff: nhân (1 + attackBonusPct)
    // (2) Hybrid Gene bonus: nếu người chơi là Hybrid và hệ mục tiêu nằm
    // trong hybrid_bonus_targets, nhân thêm (1 + hybrid_atk_bonus_pct/100).
    // Công thức:
    // damage = Round(baseDamage × (1 + attackBonusPct))
    // nếu Hybrid và hệ mục tiêu trong bonusTargets:
    // damage = Round(damage × (1 + hybrid_atk_bonus_pct/100))
    // Tham số baseDamage: Sát thương gốc (từ PlayerStats hoặc SkillData effectValue).
    // Tham số attackBonusPct: Hệ số AttackBuff dạng thập phân (0–1), ví dụ 0.15 = +15%. Truyền 0 nếu không có buff.
    // Tham số attackerData: PlayerDataResponse của người tấn công. Null = bỏ qua Hybrid bonus.
    // Tham số targetElementType: English key hệ của mục tiêu (từ NetworkEnemyHealth.ElementType). Truyền "None" nếu không rõ.
    public static int CalcPlayerAttackDamage(
        int baseDamage,
        float attackBonusPct,
        PlayerDataResponse attackerData,
        string targetElementType)
    {
        int damage = baseDamage;

        // (1) AttackBuff
        if (attackBonusPct > 0f)
            damage = Mathf.RoundToInt(damage * (1f + attackBonusPct));

        // (2) Hybrid Gene bonus vs specific element targets
        if (attackerData != null
            && attackerData.is_hybrid
            && attackerData.hybrid_atk_bonus_pct > 0f
            && !string.IsNullOrEmpty(attackerData.hybrid_bonus_targets)
            && !string.IsNullOrEmpty(targetElementType)
            && targetElementType != "None")
        {
            if (ElementHelper.IsInCsvList(targetElementType, attackerData.hybrid_bonus_targets))
            {
                int boosted = Mathf.RoundToInt(damage * (1f + attackerData.hybrid_atk_bonus_pct / 100f));
                { /* Hybrid bonus vs {targetElementType}: {damage} → {boosted} */ }
                damage = boosted;
            }
        }

        return damage;
    }

    //  Quái/Boss NHẬN sát thương

    // Quái thường nhận sát thương nguyên tố (MobPatrolAI):
    // actual = Max(1, Round(rawDamage × (1 − resistPct/100)))
    // nếu isWeakened: actual = Round(actual × 1.3)
    // Tham số rawDamage: Sát thương thô đầu vào.
    // Tham số resistPct: Chỉ số kháng nguyên tố (0–100). Lấy từ GetResistance() trên MobPatrolAI.
    // Tham số isWeakened: Trạng thái Weaken: true → nhân thêm 30%.
    public static int CalcEnemyReceivedDamage(int rawDamage, float resistPct, bool isWeakened)
    {
        int actual = Mathf.Max(1, Mathf.RoundToInt(rawDamage * (1f - resistPct / 100f)));
        if (isWeakened)
            actual = Mathf.RoundToInt(actual * 1.3f);
        return actual;
    }

    // Boss nhận sát thương nguyên tố (BossController). Dodge được xử lý trước khi gọi hàm này:
    // finalDamage = Max(1, Round(rawDamage × (1 − resistPct/100)))
    // Tham số rawDamage: Sát thương thô sau khi dodge thất bại.
    // Tham số resistPct: Chỉ số kháng lấy từ BossData theo elementType.
    public static int CalcBossReceivedDamage(int rawDamage, float resistPct)
    {
        return Mathf.Max(1, Mathf.RoundToInt(rawDamage * (1f - resistPct / 100f)));
    }

    // Dungeon enemy nhận sát thương theo phòng thủ tuyến tính (DungeonEnemyRuntimeStats):
    // damage = Max(1, rawDamage − defense)
    // Tham số rawDamage: Sát thương thô từ người chơi.
    // Tham số defense: Giáp của enemy dungeon (từ DungeonEnemyRuntimeStats.Defense).
    public static int CalcDungeonEnemyReceivedDamage(int rawDamage, int defense)
    {
        return Mathf.Max(1, rawDamage - Mathf.Max(0, defense));
    }

    //  Người chơi NHẬN sát thương nguyên tố

    // Tính sát thương người chơi nhận khi nguồn tấn công có gắn hệ nguyên tố
    // (NetworkPlayerHealth.TakeDamageWithElementInternal):
    // nếu attackerElement khắc hệ người chơi (theo Ngũ Hành):
    // nếu Hybrid miễn → finalDamage = rawDamage
    // nếu không        → finalDamage = Round(rawDamage × 1.3)
    // ngược lại            → finalDamage = rawDamage
    // Tham số rawDamage: Sát thương thô trước xét hệ.
    // Tham số attackerElement: English key hệ kẻ tấn công (ví dụ: "Water", "Fire").
    // Tham số targetPlayerData: PlayerDataResponse của người chơi nhận sát thương.
    public static int CalcPlayerReceivedElementDamage(
        int rawDamage,
        string attackerElement,
        PlayerDataResponse targetPlayerData)
    {
        if (string.IsNullOrEmpty(attackerElement) || attackerElement == "None")
            return rawDamage;
        if (targetPlayerData == null)
            return rawDamage;

        string counterOf = ElementHelper.GetElementThatCounters(targetPlayerData.element_type);
        bool isCountered = string.Equals(attackerElement, counterOf,
                                          System.StringComparison.OrdinalIgnoreCase);
        if (!isCountered)
            return rawDamage;

        // Hybrid miễn khắc hệ → sát thương gốc
        if (ElementHelper.IsImmuneToCounter(attackerElement, targetPlayerData))
        {
            { /* Hybrid Immune: {attackerElement} khắc {targetPlayerData.element_type} nhưng bị chặn */ }
            return rawDamage;
        }

        // Kẻ tấn công khắc hệ người chơi → +30%
        int final = Mathf.RoundToInt(rawDamage * 1.3f);
        { /* Counter {attackerElement}→{targetPlayerData.element_type}: {rawDamage} → {final} */ }
        return final;
    }
}
