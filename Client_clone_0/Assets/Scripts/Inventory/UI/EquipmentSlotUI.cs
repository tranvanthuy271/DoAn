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

    [Tooltip("Nút mở panel nâng cấp (ẩn khi slot trống)")]
    [SerializeField] private Button upgradeButton;

    [Header("Settings")]
    [Tooltip("Loại slot trang bị")]
    [SerializeField] private EquipmentSlotType slotType = EquipmentSlotType.Weapon;

    [Header("Icon Layout")]
    [Tooltip("Padding để icon không chạm viền slot.")]
    [SerializeField] private Vector2 iconPadding = new Vector2(16f, 16f);
    [Tooltip("Kích thước fallback nếu RectTransform icon chưa sẵn sàng.")]
    [SerializeField] private Vector2 fallbackIconMaxSize = new Vector2(84f, 84f);

    // Data hiện tại
    private EquipmentItemDto currentItem;
    private Vector2 iconMaxSize;

    /// <summary>
    /// Event khi click vào slot (để mở chi tiết hoặc tháo trang bị)
    /// </summary>
    public event Action<EquipmentSlotType, EquipmentItemDto> OnSlotClicked;

    public EquipmentSlotType SlotType => slotType;

    private void Awake()
    {
        CacheIconBounds();
        ApplyTheme();

        // Gán label theo slot type
        UpdateSlotLabel();
    }

    /// <summary>
    /// Khởi tạo slot với loại trang bị
    /// </summary>
    public void Init(EquipmentSlotType type)
    {
        slotType = type;
        CacheIconBounds();
        ApplyTheme();
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
            iconImage.preserveAspect = true;
        }

        if (placeholderImage != null)
        {
            placeholderImage.enabled = true;
        }

        if (itemNameText != null)
        {
            itemNameText.text = "";
        }

        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(false);
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
                UIRuntimeAssetHelper.SetSpriteWithNativeFit(iconImage, icon, iconMaxSize);
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

        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(true);

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

    /// <summary>
    /// Gọi từ nút "Nâng Cấp" – mở UpgradePanel cho item đang trang bị
    /// </summary>
    public void OnUpgradeClick()
    {
        if (!HasItem()) return;
        if (UpgradePanel.Instance == null)
        {
            Debug.LogWarning("[EquipmentSlotUI] UpgradePanel.Instance chưa được tạo!");
            return;
        }

        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        var inventory = bridge != null ? bridge.CurrentInventory : null;

        // slotKey phải khớp với key server lưu trong DB (weapon/helmet/armor/pants/boots/accessory)
        string slotKey = slotType.ToString().ToLower();

        UpgradePanel.Instance.OpenForEquipped(currentItem, slotKey, inventory);
    }

    private void ApplyTheme()
    {
        UIRuntimeAssetHelper.ApplyNotoSans(slotLabelText, itemNameText);
    }

    private void CacheIconBounds()
    {
        if (iconImage == null)
        {
            iconMaxSize = fallbackIconMaxSize;
            return;
        }

        iconImage.preserveAspect = true;

        Vector2 rectSize = iconImage.rectTransform.rect.size;
        if (rectSize.x <= 0f || rectSize.y <= 0f)
        {
            rectSize = iconImage.rectTransform.sizeDelta;
        }

        if (rectSize.x <= 0f || rectSize.y <= 0f)
        {
            rectSize = fallbackIconMaxSize;
        }

        iconMaxSize = new Vector2(
            Mathf.Max(0f, rectSize.x - iconPadding.x),
            Mathf.Max(0f, rectSize.y - iconPadding.y));

        if (iconMaxSize.x <= 0f || iconMaxSize.y <= 0f)
        {
            iconMaxSize = fallbackIconMaxSize;
        }
    }
}
