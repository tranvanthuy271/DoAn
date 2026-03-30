using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// InventorySlotUI - Hiển thị 1 ô item trong UI túi đồ (client-side)
/// - Nhận dữ liệu từ InventorySlotDto (JSON từ server đã parse).
/// - Gắn script này lên prefab Slot (Image icon + Text số lượng + optional highlight equip).
/// </summary>
public class InventorySlotUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private GameObject equippedMark;
    [Tooltip("Image/GameObject hiển thị khi item bị khóa (isLocked = true)")]
    [SerializeField] private GameObject lockMark;

    [Header("Select Mode (Blacksmith – chọn đá/bùa)")]
    [Tooltip("Button 'Chọn' hiện khi ô khớp filter. Thêm vào prefab slot, ẩn mặc định.")]
    [SerializeField] private Button chooseButton;
    [Tooltip("CanvasGroup để mờ ô không khớp filter. Có thể dùng CanvasGroup trên root slot.")]
    [SerializeField] private CanvasGroup canvasGroup;

    private int slotIndex;
    private InventorySlotDto currentData;

    /// <summary>
    /// Event khi người chơi click vào slot có item
    /// </summary>
    public event Action<InventorySlotDto> OnSlotClicked;

    /// <summary>
    /// Khởi tạo ô với index. Gọi 1 lần khi tạo grid.
    /// </summary>
    public void Init(int index)
    {
        slotIndex = index;
        Clear();
    }

    /// <summary>
    /// Xóa dữ liệu hiển thị
    /// </summary>
    public void Clear()
    {
        currentData = null;

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        if (quantityText != null)
        {
            quantityText.text = string.Empty;
        }

        if (equippedMark != null)
        {
            equippedMark.SetActive(false);
        }

        if (lockMark != null)
        {
            lockMark.SetActive(false);
        }

        // Reset select mode
        SetSelectMode(false, false, null);
    }

    /// <summary>
    /// Cập nhật hiển thị theo dữ liệu inventory slot từ server
    /// </summary>
    public void SetSlot(InventorySlotDto slot)
    {
        currentData = slot;

        if (slot == null || slot.quantity <= 0)
        {
            if (slot != null && slot.quantity == 0)
            {
                Debug.Log($"[InventorySlotUI] SetSlot: Slot {slotIndex} trống (quantity = 0)");
            }
            Clear();
            return;
        }

        Debug.Log($"[InventorySlotUI] SetSlot: Slot {slotIndex} - itemCode={slot.itemCode}, iconId={slot.iconId}, qty={slot.quantity}");

        // Set icon theo iconId (trùng tên sprite trong Resources/ItemIcons hoặc key Addressables)
        if (iconImage != null)
        {
            if (IconDatabase.Instance == null)
            {
                Debug.LogWarning($"[InventorySlotUI] SetSlot: IconDatabase.Instance is null! Không thể load icon cho slot {slotIndex}");
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
            else
            {
                Sprite icon = IconDatabase.Instance.GetIcon(slot.iconId);

                if (icon != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = icon;
                    Debug.Log($"[InventorySlotUI] SetSlot: Slot {slotIndex} - Đã load icon thành công: {slot.iconId}");
                }
                else
                {
                    iconImage.enabled = false;
                    iconImage.sprite = null;
                    Debug.LogWarning($"[InventorySlotUI] SetSlot: Slot {slotIndex} - KHÔNG tìm thấy icon với iconId='{slot.iconId}' trong IconDatabase!");
                }
            }
        }
        else
        {
            Debug.LogWarning($"[InventorySlotUI] SetSlot: Slot {slotIndex} - iconImage is null! Chưa gán trong Inspector.");
        }

        if (quantityText != null)
        {
            // Đơn giản: nếu quantity > 1 thì hiển thị số
            quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : string.Empty;
        }
        else
        {
            Debug.LogWarning($"[InventorySlotUI] SetSlot: Slot {slotIndex} - quantityText is null! Chưa gán trong Inspector.");
        }

        if (equippedMark != null)
        {
            equippedMark.SetActive(slot.isEquipped);
        }

        if (lockMark != null)
        {
            lockMark.SetActive(slot.isLocked);
        }
    }

    /// <summary>
    /// Lấy dữ liệu slot hiện tại
    /// </summary>
    public InventorySlotDto GetCurrentData()
    {
        return currentData;
    }

    /// <summary>
    /// Bật/tắt chế độ chọn item (dùng khi cần chọn đá / bùa cho cường hóa).
    /// - inSelectMode = true  → ô khớp filter hiện btn "Chọn"; ô không khớp bị mờ
    /// - canSelect = true     → ô này khớp filter, hiện btn "Chọn"
    /// - onSelect             → callback khi nhấn "Chọn"
    /// </summary>
    public void SetSelectMode(bool inSelectMode, bool canSelect, System.Action onSelect)
    {
        if (chooseButton != null)
        {
            chooseButton.gameObject.SetActive(inSelectMode && canSelect);
            chooseButton.onClick.RemoveAllListeners();
            if (inSelectMode && canSelect && onSelect != null)
                chooseButton.onClick.AddListener(() => onSelect());
        }

        if (canvasGroup != null)
            canvasGroup.alpha = (inSelectMode && !canSelect) ? 0.35f : 1f;

        // Khi đang ở select mode, không cho click thường (sẽ mở ItemDetailPanel)
        var mainBtn = GetComponent<UnityEngine.UI.Button>();
        if (mainBtn != null)
            mainBtn.interactable = !inSelectMode || canSelect;
    }

    /// <summary>
    /// Gọi từ Button OnClick trên prefab Slot.
    /// Hiển thị panel chi tiết item khi nhấn vào.
    /// </summary>
    public void OnClick()
    {
        if (currentData == null || currentData.quantity <= 0)
            return;

        Debug.Log($"[InventorySlotUI] Clicked slot {slotIndex} - itemCode={currentData.itemCode}, qty={currentData.quantity}");

        // Fire event để InventoryUI mở panel chi tiết
        OnSlotClicked?.Invoke(currentData);
    }
}

