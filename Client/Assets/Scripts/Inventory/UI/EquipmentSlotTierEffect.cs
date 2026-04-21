using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn lên mỗi EquipmentSlotUI prefab.
/// Tự đổi viền + background + animation theo upgrade level.
///
/// Hierarchy yêu cầu bên trong slot prefab:
///   SlotRoot
///     ├─ BG        (Image) ← kéo vào bgImage
///     ├─ Border    (Image) ← kéo vào borderImage
///     └─ ... (icon, label, v.v.)
/// </summary>
public class EquipmentSlotTierEffect : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Kéo EquipmentTierConfig SO vào đây")]
    [SerializeField] private EquipmentTierConfig tierConfig;

    [Header("UI References")]
    [Tooltip("Image hiển thị viền slot")]
    [SerializeField] private Image borderImage;

    [Tooltip("Image hiển thị background slot")]
    [SerializeField] private Image bgImage;

    [Header("Animators (tự tạo nếu cần)")]
    [Tooltip("Animator trên borderImage (hoặc null — script tự thêm)")]
    [SerializeField] private Animator borderAnimator;

    [Tooltip("Animator trên bgImage (hoặc null — script tự thêm)")]
    [SerializeField] private Animator bgAnimator;

    private int currentLevel = -1;

    /// <summary>
    /// Gọi khi item thay đổi (từ EquipmentSlotUI.SetItem).
    /// upgradeLevel = 0 → reset về default.
    /// </summary>
    public void ApplyLevel(int upgradeLevel)
    {
        if (tierConfig == null)
        {
            Debug.LogWarning("[TierEffect] tierConfig chưa gán!");
            return;
        }

        if (upgradeLevel == currentLevel) return;
        currentLevel = upgradeLevel;

        var tier = tierConfig.GetTier(upgradeLevel);
        if (tier == null) tier = tierConfig.defaultTier;
        if (tier == null) return;

        // --- Border ---
        if (borderImage != null)
        {
            borderImage.sprite = tier.borderSprite;
            borderImage.color = tier.borderColor;
            borderImage.enabled = tier.borderSprite != null;
            ApplyAnimator(borderImage.gameObject, ref borderAnimator, tier.borderAnimator);
        }

        // --- Background ---
        if (bgImage != null)
        {
            bgImage.sprite = tier.bgSprite;
            bgImage.color = tier.bgColor;
            bgImage.enabled = tier.bgSprite != null;
            ApplyAnimator(bgImage.gameObject, ref bgAnimator, tier.bgAnimator);
        }
    }

    /// <summary>
    /// Reset hiệu ứng khi slot trống.
    /// </summary>
    public void ResetToDefault()
    {
        ApplyLevel(0);
    }

    private void ApplyAnimator(GameObject target, ref Animator anim, RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            // Xóa animator nếu không cần
            if (anim != null)
            {
                anim.runtimeAnimatorController = null;
                anim.enabled = false;
            }
            return;
        }

        // Tạo Animator nếu chưa có
        if (anim == null)
        {
            anim = target.GetComponent<Animator>();
            if (anim == null)
                anim = target.AddComponent<Animator>();
        }

        anim.enabled = true;
        anim.runtimeAnimatorController = controller;
        anim.Play(0, -1, 0f); // reset animation từ đầu
    }
}
