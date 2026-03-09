using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// UpgradeStoneSlot – 1 ô trong ma trận đá nâng cấp (max 16 ô).
/// 
/// Gắn vào: mỗi StoneSlot_XX trong UpgradePanel.
/// 
/// Click ô trống  → báo lên UpgradePanel.OnStoneSlotClicked(this)   → mở stone picker
/// Click ô có đá → báo lên UpgradePanel.OnStoneSlotRemoved(this)   → tháo đá ra
/// </summary>
public class UpgradeStoneSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image     iconImage;
    [SerializeField] private TMP_Text  quantityText;
    [SerializeField] private GameObject emptyIndicator;   // icon "+" hoặc placeholder khi trống
    [SerializeField] private Image     highlightBorder;   // border khi hover (tuỳ chọn)

    // ── Trạng thái ───────────────────────────────────────────────
    public bool             IsEmpty          { get; private set; } = true;
    public InventorySlotDto ItemData         { get; private set; }
    public int              InventorySlotIndex => ItemData != null ? ItemData.slotIndex : -1;

    private UpgradePanel panel;

    private void Awake()
    {
        panel = GetComponentInParent<UpgradePanel>();
    }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>Đặt đá vào ô này (gọi từ UpgradePanel khi chọn từ picker)</summary>
    public void SetItem(InventorySlotDto slot)
    {
        ItemData = slot;
        IsEmpty  = false;

        // Hiển thị icon đá
        if (iconImage != null && IconDatabase.Instance != null)
        {
            var tmpl = ItemTemplateManager.Instance != null
                ? ItemTemplateManager.Instance.GetItemTemplate(slot.id)
                : null;

            if (tmpl != null)
            {
                var sprite = IconDatabase.Instance.GetIcon(tmpl.idIcon.ToString());
                iconImage.sprite  = sprite;
                iconImage.enabled = sprite != null;
            }
        }

        if (quantityText) quantityText.text = "1";
        if (emptyIndicator) emptyIndicator.SetActive(false);
        if (highlightBorder) highlightBorder.enabled = false;
    }

    /// <summary>Xoá đá khỏi ô này</summary>
    public void Clear()
    {
        ItemData = null;
        IsEmpty  = true;

        if (iconImage)      { iconImage.sprite = null; iconImage.enabled = false; }
        if (quantityText)   quantityText.text = "";
        if (emptyIndicator) emptyIndicator.SetActive(true);
        if (highlightBorder) highlightBorder.enabled = false;
    }

    // ── Click handling ────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (panel == null)
        {
            Debug.LogWarning("[UpgradeStoneSlot] Không tìm thấy UpgradePanel cha.");
            return;
        }

        if (IsEmpty)
            panel.OnStoneSlotClicked(this);
        else
            panel.OnStoneSlotRemoved(this);
    }
}
