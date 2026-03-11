using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI cho một slot skill trong Hotbar.
/// 
/// Cấu trúc Hierarchy của Prefab "SkillSlot":
///   SkillSlot (Image + Button)
///   ├── IconImage        (Image)        — icon của skill
///   ├── CooldownOverlay  (Image)        — fillAmount overlay khi đang cooldown, đặt Image Type = Filled, Fill Method = Radial360
///   └── CooldownText     (TMP_Text)     — hiển thị số giây còn lại ("2.4s"), ẩn khi sẵn sàng
/// </summary>
public class SkillSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image hiển thị icon của skill")]
    public Image iconImage;

    [Tooltip("Image dạng Fill (Radial360) phủ lên icon khi skill đang cooldown. Image Type phải là Filled.")]
    public Image cooldownOverlay;

    [Tooltip("Text hiển thị thời gian cooldown còn lại (ví dụ: '2.4s'). Tự động ẩn khi skill sẵn sàng.")]
    public TMP_Text cooldownText;

    [Tooltip("Button để nhấn kích hoạt skill")]
    public Button skillButton;

    [Header("Settings")]
    [Tooltip("Màu icon khi skill sẵn sàng")]
    public Color readyColor = Color.white;

    [Tooltip("Màu icon khi skill đang cooldown")]
    public Color cooldownColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    // ── Internal state ───────────────────────────────────────────────────────
    private SkillData boundSkill;
    private PlayerSkillManager skillManager;
    private int slotIndex = -1;

    // ════════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gắn slot này vào một SkillData cụ thể và PlayerSkillManager tương ứng.
    /// Gọi từ SkillHotbarUI sau khi tìm thấy PlayerSkillManager của owner.
    /// </summary>
    public void Bind(SkillData skill, PlayerSkillManager manager, int index, Sprite icon = null)
    {
        boundSkill = skill;
        skillManager = manager;
        slotIndex = index;

        // Hiển thị icon (nếu có sprite được truyền vào)
        if (iconImage != null)
        {
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            else
            {
                // Không có icon → ẩn image / để nguyên sprite mặc định
                iconImage.enabled = (iconImage.sprite != null);
            }
        }

        // Ẩn overlay ban đầu
        if (cooldownOverlay != null)
        {
            cooldownOverlay.fillAmount = 0f;
            cooldownOverlay.gameObject.SetActive(false);
        }

        if (cooldownText != null)
            cooldownText.gameObject.SetActive(false);

        // Đăng ký button click
        if (skillButton != null)
        {
            skillButton.onClick.RemoveAllListeners();
            skillButton.onClick.AddListener(OnButtonClicked);
        }
    }

    /// <summary>
    /// Xóa binding (slot trống / chưa có skill)
    /// </summary>
    public void Unbind()
    {
        boundSkill = null;
        skillManager = null;
        slotIndex = -1;

        if (iconImage != null) iconImage.enabled = false;
        if (cooldownOverlay != null) cooldownOverlay.gameObject.SetActive(false);
        if (cooldownText != null) cooldownText.gameObject.SetActive(false);
        if (skillButton != null) skillButton.interactable = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ════════════════════════════════════════════════════════════════════════

    private void Update()
    {
        if (boundSkill == null) return;

        bool onCooldown = !boundSkill.CanUse();
        float remaining = boundSkill.GetCooldownRemaining();

        // ── Overlay fill (đếm ngược hình tròn) ─────────────────────────────
        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(onCooldown);
            // fillAmount = 1 → full đen (vừa dùng), 0 → sẵn sàng
            cooldownOverlay.fillAmount = onCooldown
                ? 1f - boundSkill.GetCooldownPercent()
                : 0f;
        }

        // ── Countdown text ──────────────────────────────────────────────────
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(onCooldown);
            if (onCooldown)
                cooldownText.text = remaining >= 1f
                    ? Mathf.CeilToInt(remaining).ToString()
                    : remaining.ToString("F1") + "s";
        }

        // ── Icon tint ───────────────────────────────────────────────────────
        if (iconImage != null)
            iconImage.color = onCooldown ? cooldownColor : readyColor;

        // ── Button interactable ─────────────────────────────────────────────
        if (skillButton != null)
            skillButton.interactable = !onCooldown;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════════════════════════════

    private void OnButtonClicked()
    {
        if (skillManager == null || slotIndex < 0) return;
        skillManager.TryUseSkillByIndex(slotIndex);
    }
}
