using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// UpgradeStoneSlot – 1 ô trong ma trận đá nâng cấp (max 16 ô).
// Gắn vào: mỗi StoneSlot_XX trong UpgradePanel.
// Click ô trống  → báo lên UpgradePanel.OnStoneSlotClicked(this)   → mở stone picker
// Click ô có đá → báo lên UpgradePanel.OnStoneSlotRemoved(this)   → tháo đá ra
public class UpgradeStoneSlot : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image     iconImage;
    [SerializeField] private TMP_Text  quantityText;
    [SerializeField] private GameObject emptyIndicator;   // placeholder/background khi trống
    [SerializeField] private Image     highlightBorder;   // border khi hover (tuỳ chọn)

    // Trạng thái
    public bool             IsEmpty          { get; private set; } = true;
    public InventorySlotDto ItemData         { get; private set; }
    public int              InventorySlotIndex => ItemData != null ? ItemData.slotIndex : -1;

    private UpgradePanel panel;

    private void Awake()
    {
        ResolveReferences();
        panel = GetComponentInParent<UpgradePanel>();
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Đặt đá vào ô này (gọi từ UpgradePanel khi chọn từ picker)
    public void SetItem(InventorySlotDto slot)
    {
        ResolveReferences();
        ItemData = slot;
        IsEmpty  = false;

        // Hiển thị icon đá
        if (iconImage != null)
        {
            if (!iconImage.gameObject.activeSelf)
                iconImage.gameObject.SetActive(true);

            Sprite sprite = null;
            if (IconDatabase.Instance != null)
            {
                if (!string.IsNullOrEmpty(slot.iconId))
                    sprite = IconDatabase.Instance.GetIcon(slot.iconId);

                if (sprite == null)
                {
                    var tmpl = ItemTemplateManager.Instance != null
                        ? ItemTemplateManager.Instance.GetItemTemplate(slot.id)
                        : null;
                    if (tmpl != null)
                        sprite = IconDatabase.Instance.GetIcon(tmpl.idIcon.ToString());
                }
            }

            iconImage.sprite  = sprite;
            iconImage.enabled = sprite != null;
        }

        if (quantityText) quantityText.text = "1";
        SetEmptyIndicatorVisible(false);
        if (highlightBorder) highlightBorder.enabled = false;
    }

    // Xoá đá khỏi ô này
    public void Clear()
    {
        ResolveReferences();
        ItemData = null;
        IsEmpty  = true;

        if (iconImage)
        {
            if (!iconImage.gameObject.activeSelf)
                iconImage.gameObject.SetActive(true);
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
        if (quantityText)   quantityText.text = "";
        SetEmptyIndicatorVisible(true);
        if (highlightBorder) highlightBorder.enabled = false;
    }

    private void ResolveReferences()
    {
        if (iconImage == null)
        {
            var iconTransform = transform.Find("IconImage");
            if (iconTransform != null)
                iconImage = iconTransform.GetComponent<Image>();
        }

        if (quantityText == null)
        {
            var quantityTransform = transform.Find("QuantityText");
            if (quantityTransform != null)
                quantityText = quantityTransform.GetComponent<TMP_Text>();
        }

        if (emptyIndicator == null || (iconImage != null && emptyIndicator == iconImage.gameObject))
        {
            var placeholderTransform = transform.Find("Image");
            if (placeholderTransform != null)
                emptyIndicator = placeholderTransform.gameObject;
        }

        if (iconImage != null)
            iconImage.raycastTarget = false;

        if (quantityText != null)
            quantityText.raycastTarget = false;

        if (emptyIndicator != null)
        {
            var backgroundImage = emptyIndicator.GetComponent<Image>();
            if (backgroundImage != null && string.Equals(emptyIndicator.name, "Image", StringComparison.OrdinalIgnoreCase))
                backgroundImage.raycastTarget = true;
        }
    }

    private void SetEmptyIndicatorVisible(bool visible)
    {
        if (emptyIndicator == null)
            return;

        bool isBackgroundImage = string.Equals(emptyIndicator.name, "Image", StringComparison.OrdinalIgnoreCase);
        if (isBackgroundImage)
        {
            if (!emptyIndicator.activeSelf)
                emptyIndicator.SetActive(true);

            var backgroundImage = emptyIndicator.GetComponent<Image>();
            if (backgroundImage != null)
            {
                backgroundImage.enabled = true;
                backgroundImage.raycastTarget = true;
            }
            return;
        }

        emptyIndicator.SetActive(visible);
    }

    // Click handling

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
