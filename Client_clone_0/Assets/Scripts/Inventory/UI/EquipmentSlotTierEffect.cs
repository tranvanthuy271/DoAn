using UnityEngine;
using UnityEngine.UI;

// Gắn lên mỗi EquipmentSlotUI prefab.
// Tự đổi viền + background + animation theo upgrade level.
// Hierarchy yêu cầu bên trong slot prefab:
// SlotRoot
// ├─ BG        (Image) ← kéo vào bgImage
// ├─ Border    (Image) ← kéo vào borderImage
// └─ ... (icon, label, v.v.)
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

    // Gọi khi item thay đổi (từ EquipmentSlotUI.SetItem).
    // upgradeLevel = 0 → reset về default.
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

        // Border
        if (borderImage != null)
        {
            borderImage.sprite = tier.borderSprite;
            borderImage.color = tier.borderColor;
            borderImage.enabled = tier.borderSprite != null || tier.borderAnimator != null;
            ApplyAnimator(borderImage.gameObject, ref borderAnimator, tier.borderAnimator);
        }

        // Background
        if (bgImage != null)
        {
            bgImage.sprite = tier.bgSprite;
            bgImage.color = tier.bgColor;
            bgImage.enabled = tier.bgSprite != null || tier.bgAnimator != null;
            ApplyAnimator(bgImage.gameObject, ref bgAnimator, tier.bgAnimator);
        }
    }

    // Reset hiệu ứng khi slot trống.
    public void ResetToDefault()
    {
        ApplyLevel(0);
    }

    private void ApplyAnimator(GameObject target, ref Animator anim, RuntimeAnimatorController controller)
    {
        if (controller == null)
        {
            if (anim != null)
            {
                anim.runtimeAnimatorController = null;
                anim.enabled = false;
            }
            return;
        }

        if (target == null)
            return;

        if (!target.activeSelf)
            target.SetActive(true);

        if (anim == null || anim.gameObject != target)
        {
            anim = target.GetComponent<Animator>();
            if (anim == null)
                anim = target.AddComponent<Animator>();
        }

        anim.enabled = false;
        anim.runtimeAnimatorController = controller;
        anim.updateMode = AnimatorUpdateMode.UnscaledTime;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        anim.enabled = true;
        if (target.activeInHierarchy)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }
}
