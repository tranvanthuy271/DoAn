using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private int slotIndex;
    private InventorySlotDto currentData;

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
    }

    /// <summary>
    /// Gọi từ Button OnClick trên prefab Slot.
    /// Ở đây tạm thời chỉ log; phần gửi request UseItem/EquipItem sẽ do lớp network đảm nhiệm.
    /// </summary>
    public void OnClick()
    {
        if (currentData == null || currentData.quantity <= 0)
            return;

        Debug.Log($"[InventorySlotUI] Clicked slot {slotIndex} - itemCode={currentData.itemCode}, qty={currentData.quantity}");
        // TODO: Gọi hàm UseItem/EquipItem client → server tại đây, ví dụ:
        // InventoryNetworkClient.Instance.RequestUseItem(currentData.slotIndex);
    }
}

