using UnityEngine;

// ScriptableObject config cho các mức viền + background trang bị.
// Tạo: Right-click > Create > Equipment > Tier Config
[CreateAssetMenu(fileName = "EquipmentTierConfig", menuName = "Equipment/Tier Config")]
public class EquipmentTierConfig : ScriptableObject
{
    [System.Serializable]
    public class TierEntry
    {
        [Tooltip("Level tối thiểu để kích hoạt tier này (VD: 4, 8, 12, 14)")]
        public int minLevel;

        [Tooltip("Sprite viền slot")]
        public Sprite borderSprite;

        [Tooltip("Sprite background slot")]
        public Sprite bgSprite;

        [Tooltip("Animator Controller cho viền (nullable)")]
        public RuntimeAnimatorController borderAnimator;

        [Tooltip("Animator Controller cho background (nullable)")]
        public RuntimeAnimatorController bgAnimator;

        [Tooltip("Màu tint cho viền")]
        public Color borderColor = Color.white;

        [Tooltip("Màu tint cho background")]
        public Color bgColor = Color.white;
    }

    [Header("Tier mặc định (level < tier đầu tiên)")]
    public TierEntry defaultTier;

    [Header("Các mức tier — sắp theo minLevel tăng dần")]
    [Tooltip("VD: 4, 8, 12, 14")]
    public TierEntry[] tiers;

    // Tìm TierEntry phù hợp cho upgradeLevel.
    // Duyệt ngược mảng, trả tier có minLevel <= level.
    public TierEntry GetTier(int upgradeLevel)
    {
        if (tiers == null || tiers.Length == 0) return defaultTier;

        for (int i = tiers.Length - 1; i >= 0; i--)
        {
            if (upgradeLevel >= tiers[i].minLevel)
                return tiers[i];
        }
        return defaultTier;
    }
}
