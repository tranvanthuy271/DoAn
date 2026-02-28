using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// EquipmentSlotUI - Hiển thị 1 ô trang bị trong UI Equipment Panel
/// Mỗi slot đại diện cho 1 loại trang bị (Weapon, Helmet, Armor, Pants, Boots, Accessory)
/// 
/// Setup trong Unity:
/// 1. Tạo prefab với Image (icon), TMP_Text (slot label), Button (click)
/// 2. Gắn script này lên prefab
/// 3. Kéo reference vào Inspector
/// </summary>
public class EquipmentSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image hiển thị icon của item đang trang bị")]
    [SerializeField] private Image iconImage;

    [Tooltip("Image placeholder khi chưa có item (icon mờ)")]
    [SerializeField] private Image placeholderImage;

    [Tooltip("Text hiển thị tên loại slot (Vũ khí, Mũ, Giáp, ...)")]
    [SerializeField] private TMP_Text slotLabelText;

    [Tooltip("Text hiển thị tên item đang trang bị")]
    [SerializeField] private TMP_Text itemNameText;

    [Header("Settings")]
    [Tooltip("Loại slot trang bị")]
    [SerializeField] private EquipmentSlotType slotType = EquipmentSlotType.Weapon;

    // Data hiện tại
    private EquipmentItemDto currentItem;

    /// <summary>
    /// Event khi click vào slot (để mở chi tiết hoặc tháo trang bị)
    /// </summary>
    public event Action<EquipmentSlotType, EquipmentItemDto> OnSlotClicked;

    public EquipmentSlotType SlotType => slotType;

    private void Awake()
    {
        // Gán label theo slot type
        UpdateSlotLabel();
    }

    /// <summary>
    /// Khởi tạo slot với loại trang bị
    /// </summary>
    public void Init(EquipmentSlotType type)
    {
        slotType = type;
        UpdateSlotLabel();
        Clear();
    }

    /// <summary>
    /// Cập nhật label của slot
    /// </summary>
    private void UpdateSlotLabel()
    {
        if (slotLabelText != null)
        {
            slotLabelText.text = PlayerEquipmentDto.GetSlotDisplayName(slotType);
        }
    }

    /// <summary>
    /// Xóa item khỏi slot (hiển thị trống)
    /// </summary>
    public void Clear()
    {
        currentItem = null;

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }

        if (placeholderImage != null)
        {
            placeholderImage.enabled = true;
        }

        if (itemNameText != null)
        {
            itemNameText.text = "";
        }
    }

    /// <summary>
    /// Gán item vào slot
    /// </summary>
    public void SetItem(EquipmentItemDto item)
    {
        currentItem = item;

        if (item == null || item.itemTemplateId <= 0)
        {
            Clear();
            return;
        }

        // Hiển thị icon
        if (iconImage != null)
        {
            Sprite icon = null;
            if (IconDatabase.Instance != null && !string.IsNullOrEmpty(item.iconId))
            {
                icon = IconDatabase.Instance.GetIcon(item.iconId);
            }

            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
            }
            else
            {
                iconImage.enabled = false;
                Debug.LogWarning($"[EquipmentSlotUI] Không tìm thấy icon: {item.iconId} cho slot {slotType}");
            }
        }

        // Ẩn placeholder khi có item
        if (placeholderImage != null)
        {
            placeholderImage.enabled = false;
        }

        // Hiển thị tên item
        if (itemNameText != null)
        {
            itemNameText.text = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.itemCode;
        }

        Debug.Log($"[EquipmentSlotUI] Slot {slotType}: Đã gán {item.itemName} (id={item.itemTemplateId})");
    }

    /// <summary>
    /// Lấy item đang trang bị
    /// </summary>
    public EquipmentItemDto GetCurrentItem()
    {
        return currentItem;
    }

    /// <summary>
    /// Kiểm tra slot có item không
    /// </summary>
    public bool HasItem()
    {
        return currentItem != null && currentItem.itemTemplateId > 0;
    }

    /// <summary>
    /// Gọi từ Button OnClick trên prefab
    /// </summary>
    public void OnClick()
    {
        Debug.Log($"[EquipmentSlotUI] Click slot {slotType}, hasItem={HasItem()}");
        OnSlotClicked?.Invoke(slotType, currentItem);
    }
}
