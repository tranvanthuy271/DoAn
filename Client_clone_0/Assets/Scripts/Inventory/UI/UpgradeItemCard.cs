using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UpgradeItemCard – Hiển thị trang bị (hiện tại hoặc preview sau nâng cấp).
/// 
/// Gắn vào: CurrentCard  và PreviewCard trong UpgradePanel.
/// 
/// isPreview = false → hiển thị bậc hiện tại, stat màu trắng/xám
/// isPreview = true  → hiển thị bậc +N+1, stat tăng màu vàng, stat mở khoá màu xanh
/// </summary>
public class UpgradeItemCard : MonoBehaviour
{
    [Header("Header")]
    [SerializeField] private Image    itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text upgradeLevelText;

    [Header("Stats")]
    [SerializeField] private Transform    statsContainer;  // VerticalLayoutGroup
    [SerializeField] private StatRowEntry statRowPrefab;   // prefab 1 dòng stat

    // ── Màu sắc ──────────────────────────────────────────────────
    private static readonly Color ColorActive   = Color.white;
    private static readonly Color ColorDim      = new Color(0.55f, 0.55f, 0.55f);
    private static readonly Color ColorUpgraded = new Color(1f, 0.85f, 0f);   // vàng – stat tăng
    private static readonly Color ColorNewUnlock = new Color(0.4f, 1f, 0.5f); // xanh – vừa mở khoá

    // ── Public API ────────────────────────────────────────────────

    /// <summary>
    /// Hiển thị card với dữ liệu item và danh sách option templates.
    /// </summary>
    /// <param name="item">Item đang xét (hiện tại hoặc base cho preview)</param>
    /// <param name="templates">Option templates từ server (đã cache)</param>
    /// <param name="isPreview">true = hiển thị bậc +1 so với item.upgradeLevel</param>
    public void Display(EquipmentItemDto item, List<OptionTemplateDto> templates, bool isPreview)
    {
        if (item == null) return;

        // ── Header ───────────────────────────────────────────────
        var tmpl = ItemTemplateManager.Instance != null
            ? ItemTemplateManager.Instance.GetItemTemplate(item.id)
            : null;

        if (tmpl != null)
        {
            itemNameText.text = tmpl.name;
            if (itemIcon != null && IconDatabase.Instance != null)
                itemIcon.sprite = IconDatabase.Instance.GetIcon(tmpl.idIcon.ToString());
        }
        else
        {
            itemNameText.text = $"Item #{item.id}";
        }

        int displayLevel = isPreview ? item.upgradeLevel + 1 : item.upgradeLevel;
        upgradeLevelText.text = $"+{displayLevel}";

        // ── Xoá rows cũ ──────────────────────────────────────────
        foreach (Transform child in statsContainer)
            Destroy(child.gameObject);

        if (string.IsNullOrEmpty(item.strOptions)) return;

        // ── Parse options ─────────────────────────────────────────
        var equipped = EquippedOptionDisplay.ParseAll(item.strOptions);
        foreach (var opt in equipped)
        {
            var optTmpl = templates?.Find(t => t.id == opt.optionId);
            if (optTmpl == null) continue;

            int currentValue = opt.value;
            int previewValue = optTmpl.GetValueAt(displayLevel);
            int showValue    = isPreview ? previewValue : currentValue;
            int delta        = previewValue - currentValue;

            // ── Label ─────────────────────────────────────────────
            string label = optTmpl.BuildLabel(showValue);
            if (isPreview && delta > 0)
                label += $"  <color=#88cc88>(+{delta})</color>";

            // ── Màu ──────────────────────────────────────────────
            Color color;
            if (!isPreview)
            {
                color = optTmpl.IsActive(item.upgradeLevel) ? ColorActive : ColorDim;
            }
            else
            {
                bool wasActive    = optTmpl.IsActive(item.upgradeLevel);
                bool willBeActive = optTmpl.IsActive(displayLevel);

                if (!wasActive && willBeActive) color = ColorNewUnlock;
                else if (delta > 0)             color = ColorUpgraded;
                else if (willBeActive)          color = ColorActive;
                else                            color = ColorDim;
            }

            // ── Tạo row ───────────────────────────────────────────
            var row = Instantiate(statRowPrefab, statsContainer);
            row.Set(label, color);
        }
    }

    /// <summary>Xoá toàn bộ hiển thị (dùng khi panel chưa chọn item)</summary>
    public void Clear()
    {
        if (itemNameText)    itemNameText.text     = "—";
        if (upgradeLevelText) upgradeLevelText.text = "+0";
        if (itemIcon)        itemIcon.sprite       = null;
        foreach (Transform child in statsContainer) Destroy(child.gameObject);
    }
}
