using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UpgradeStoneConfig – ScriptableObject cấu hình tỉ lệ cường hóa.
///
/// ══════════════════════════════════════════════════════════
/// TẠO ASSET: Project → Create → Upgrade / Stone Config
/// ══════════════════════════════════════════════════════════
/// Dữ liệu nên mirror từ DB equipment_upgrade_config để client hiển thị
/// đúng cùng tỉ lệ mà server dùng để validate.
/// ══════════════════════════════════════════════════════════
/// </summary>
[CreateAssetMenu(menuName = "Upgrade/Stone Config", fileName = "UpgradeStoneConfig")]
public class UpgradeStoneConfig : ScriptableObject
{
    // ─── Đá nâng cấp ──────────────────────────────────────────────

    [Serializable]
    public class StoneEntry
    {
        [Tooltip("item_template.id của viên đá")]
        public int itemId;

        [Tooltip("Tên hiển thị (UI)")]
        public string stoneName;

        [Tooltip("Điểm tỉ lệ mỗi viên đem lại (int). Ví dụ: 5 = +5 điểm tỉ lệ)")]
        public int ratePointPerStone;

        [Tooltip("Số điểm tỉ lệ tối đa mà loại đá này đóng góp được (0 = không giới hạn)")]
        public int maxRatePointFromThisStone;
    }

    [Header("Danh sách đá (type=21 + đặc biệt)")]
    public StoneEntry[] stones;

    // ─── Bùa cường hóa ────────────────────────────────────────────

    [Header("Bùa cường hóa (itemId=8)")]
    [Tooltip("item_template.id của bùa cường hóa")]
    public int charmItemId = 8;

    [Tooltip("% tỉ lệ cộng thêm khi dùng Đá May Mắn. DB hiện tại dùng 15 = +15%.")]
    public int charmBonusPercent = 15;

    [Header("Đá bảo vệ (itemId=9)")]
    [Tooltip("item_template.id của đá bảo vệ. Không tăng tỉ lệ, chỉ chặn fail policy.")]
    public int protectionItemId = 9;

    // ─── Cấu hình từng món đồ ────────────────────────────────────

    [Serializable]
    public class ItemUpgradeEntry
    {
        [Tooltip("item_template.id của trang bị")]
        public int itemTemplateId;

        [Tooltip("Tên (debug)")]
        public string itemName;

        [Tooltip("Tỉ lệ thành công cơ bản ở +0 → +1 (int, e.g. 80 = 80%)")]
        public int baseSuccessPercent = 80;

        [Tooltip("Giảm bao nhiêu % tỉ lệ mỗi bậc nâng (ví dụ 5 = -5% mỗi bậc)")]
        public int successDecreasePerLevel = 5;

        [Tooltip("Bậc tối đa item này có thể nâng")]
        public int maxUpgradeLevel = 15;

        [Tooltip("Số đá tối thiểu cần để bắt đầu tính tỉ lệ (overrides server nếu > 0)")]
        public int stoneMinOverride = 0;

        [Tooltip("Số đá để đạt tỉ lệ base đầy đủ (overrides server nếu > 0)")]
        public int stoneNeededOverride = 0;
    }

    [Header("Cấu hình tỉ lệ từng món đồ (nếu không có sẽ dùng giá trị từ server DB)")]
    public ItemUpgradeEntry[] itemConfigs;

    [Serializable]
    public class LevelRateEntry
    {
        [Tooltip("Bậc muốn đạt tới, ví dụ 1 = +0 → +1")]
        public int targetLevel;

        [Tooltip("Bạc cần cho bậc này")]
        public int silverCost;

        [Tooltip("ID đá nâng cấp chính cần dùng cho bậc này")]
        public int stoneId;

        [Tooltip("Tên đá chính, để debug/Inspector dễ đọc")]
        public string stoneName;

        [Tooltip("Tỉ lệ tối đa ở bậc này khi đủ stoneNeeded viên, tính theo %")]
        public int baseSuccessPercent;

        [Tooltip("Số đá chính để đạt baseSuccessPercent")]
        public int stoneNeeded;

        [Tooltip("Số đá chính tối thiểu mới cho phép nâng")]
        public int stoneMin;

        [Tooltip("0 = an toàn, 1 = -1 bậc, 2 = về +0")]
        public int failPolicy;
    }

    [Header("Dữ liệu theo từng bậc nâng (mirror equipment_upgrade_config)")]
    public LevelRateEntry[] levelConfigs;

    // ─── Cài đặt chung ────────────────────────────────────────────

    [Header("Tổng điểm tỉ lệ cần để coi là 100% thành công")]
    [Tooltip("Điểm tỉ lệ đầy đủ (100%). Ví dụ 100 = cần 100 điểm để đạt 100%")]
    public int fullRatePoints = 100;

    [Header("Giới hạn tỉ lệ tối đa (%)")]
    [Tooltip("Không cho phép vượt quá giới hạn này dù đá nhiều (0 = không giới hạn)")]
    public int maxSuccessPercent = 95;

    // ─── Helpers ─────────────────────────────────────────────────

    /// <summary>Lấy entry đá theo itemId. Trả null nếu không tìm thấy.</summary>
    public StoneEntry GetStone(int itemId)
    {
        if (stones == null) return null;
        foreach (var s in stones)
            if (s.itemId == itemId) return s;
        return null;
    }

    /// <summary>Lấy config nâng cấp của item theo itemTemplateId.</summary>
    public ItemUpgradeEntry GetItemConfig(int itemTemplateId)
    {
        if (itemConfigs == null) return null;
        foreach (var ic in itemConfigs)
            if (ic.itemTemplateId == itemTemplateId) return ic;
        return null;
    }

    public LevelRateEntry GetLevelConfig(int targetLevel)
    {
        if (levelConfigs == null) return null;
        foreach (var cfg in levelConfigs)
            if (cfg.targetLevel == targetLevel) return cfg;
        return null;
    }

    /// <summary>
    /// Tính tỉ lệ % từ danh sách đá đã chọn + charm + level hiện tại của item.
    /// Trả về giá trị 0-100 (int).
    /// </summary>
    /// <param name="itemTemplateId">ID item cần nâng</param>
    /// <param name="currentLevel">Bậc hiện tại của item</param>
    /// <param name="stoneIds">Danh sách itemId của các viên đá đặt vào 16 ô</param>
    /// <param name="hasCharm">Có đặt bùa hay không</param>
    public int CalcSuccessPercent(int itemTemplateId, int currentLevel, List<int> stoneIds, bool hasCharm)
    {
        int targetLevel = currentLevel + 1;
        var levelCfg = GetLevelConfig(targetLevel);
        if (levelCfg != null)
        {
            int mainStoneCount = 0;
            if (stoneIds != null)
            {
                foreach (int stoneId in stoneIds)
                {
                    if (stoneId == levelCfg.stoneId)
                        mainStoneCount++;
                }
            }

            if (mainStoneCount < levelCfg.stoneMin)
                return 0;

            float ratio = levelCfg.stoneNeeded > 0
                ? Mathf.Min((float)mainStoneCount / levelCfg.stoneNeeded, 1f)
                : 0f;

            int levelTotal = Mathf.RoundToInt(levelCfg.baseSuccessPercent * ratio);
            if (hasCharm)
                levelTotal += charmBonusPercent;

            if (maxSuccessPercent > 0)
                levelTotal = Mathf.Min(levelTotal, maxSuccessPercent);
            return Mathf.Clamp(levelTotal, 0, 100);
        }

        // 1. Tỉ lệ cơ bản theo item + level
        int basePercent = 80;
        int decrease    = 5;
        var cfg = GetItemConfig(itemTemplateId);
        if (cfg != null)
        {
            basePercent = cfg.baseSuccessPercent;
            decrease    = cfg.successDecreasePerLevel;
        }
        int levelPenalty = currentLevel * decrease;
        int rateFromItem = Mathf.Max(0, basePercent - levelPenalty);

        // 2. Điểm từ đá
        // Đếm số lần xuất hiện từng itemId
        var countMap = new Dictionary<int, int>();
        if (stoneIds != null)
        {
            foreach (int sid in stoneIds)
            {
                if (!countMap.ContainsKey(sid)) countMap[sid] = 0;
                countMap[sid]++;
            }
        }

        int totalStonePoints = 0;
        foreach (var kv in countMap)
        {
            var entry = GetStone(kv.Key);
            if (entry == null) continue;
            int pts = entry.ratePointPerStone * kv.Value;
            if (entry.maxRatePointFromThisStone > 0)
                pts = Mathf.Min(pts, entry.maxRatePointFromThisStone);
            totalStonePoints += pts;
        }

        // Chuyển điểm đá thành %
        float stonePct = fullRatePoints > 0
            ? (float)totalStonePoints / fullRatePoints * 100f
            : 0f;

        // 3. Bùa
        int charmBonus = hasCharm ? charmBonusPercent : 0;

        // 4. Tổng
        int total = rateFromItem + Mathf.RoundToInt(stonePct) + charmBonus;
        if (maxSuccessPercent > 0)
            total = Mathf.Min(total, maxSuccessPercent);
        return Mathf.Clamp(total, 0, 100);
    }
}
