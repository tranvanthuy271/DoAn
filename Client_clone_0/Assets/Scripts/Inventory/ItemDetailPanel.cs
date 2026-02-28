using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// ItemDetailPanel - Hiển thị chi tiết item khi nhấn vào slot trong inventory
/// Bao gồm: hình ảnh, tên, mô tả, nút sử dụng
/// 
/// Setup trong Unity:
/// 1. Tạo Panel con bên trong Inventory Panel (đặt tên "ItemDetailPanel")
/// 2. Thêm các UI con: Image (icon), TMP_Text (tên), TMP_Text (mô tả), Button (sử dụng)
/// 3. Gắn script này lên Panel và kéo các reference vào Inspector
/// 4. Panel mặc định ẩn, sẽ hiện khi click vào item
/// </summary>
public class ItemDetailPanel : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image hiển thị icon của item")]
    [SerializeField] private Image itemIcon;

    [Tooltip("Text hiển thị tên item")]
    [SerializeField] private TMP_Text itemNameText;

    [Tooltip("Text hiển thị mô tả item")]
    [SerializeField] private TMP_Text itemDescriptionText;

    [Tooltip("Nút sử dụng item")]
    [SerializeField] private Button useButton;

    [Tooltip("Text trên nút sử dụng (tuỳ chọn)")]
    [SerializeField] private TMP_Text useButtonText;

    [Header("Settings")]
    [Tooltip("Ẩn panel khi Start")]
    [SerializeField] private bool hideOnStart = true;

    // Dữ liệu item đang hiển thị
    private InventorySlotDto currentSlotData;
    private ItemTemplateDto currentTemplate;

    // Event khi nhấn nút sử dụng - các script khác có thể subscribe
    public event Action<InventorySlotDto> OnUseItemClicked;

    private void Awake()
    {
        if (useButton != null)
        {
            useButton.onClick.AddListener(OnUseButtonPressed);
        }
    }

    private void Start()
    {
        if (hideOnStart)
        {
            Hide();
        }
    }

    /// <summary>
    /// Hiển thị chi tiết item từ InventorySlotDto
    /// Tự động tra cứu ItemTemplateManager để lấy tên + mô tả
    /// </summary>
    public void ShowItem(InventorySlotDto slotData)
    {
        if (slotData == null || slotData.quantity <= 0)
        {
            Debug.LogWarning("[ItemDetailPanel] ShowItem: slotData is null hoặc quantity <= 0");
            Hide();
            return;
        }

        currentSlotData = slotData;
        currentTemplate = null;

        // Lấy thông tin item template từ ItemTemplateManager
        if (ItemTemplateManager.Instance != null)
        {
            // Thử tìm theo itemTemplateId trước
            if (slotData.itemTemplateId > 0)
            {
                currentTemplate = ItemTemplateManager.Instance.GetItemTemplate(slotData.itemTemplateId);
            }

            // Nếu không tìm được, thử theo itemCode
            if (currentTemplate == null && !string.IsNullOrEmpty(slotData.itemCode))
            {
                currentTemplate = ItemTemplateManager.Instance.GetItemTemplateByCode(slotData.itemCode);
            }
        }
        else
        {
            Debug.LogWarning("[ItemDetailPanel] ItemTemplateManager.Instance is null! Không thể lấy tên/mô tả item.");
        }

        // --- Cập nhật UI ---

        // 1. Icon
        if (itemIcon != null)
        {
            Sprite icon = null;
            if (IconDatabase.Instance != null && !string.IsNullOrEmpty(slotData.iconId))
            {
                icon = IconDatabase.Instance.GetIcon(slotData.iconId);
            }

            if (icon != null)
            {
                itemIcon.sprite = icon;
                itemIcon.enabled = true;
            }
            else
            {
                itemIcon.enabled = false;
                Debug.LogWarning($"[ItemDetailPanel] Không tìm thấy icon: {slotData.iconId}");
            }
        }

        // 2. Tên item
        if (itemNameText != null)
        {
            if (currentTemplate != null && !string.IsNullOrEmpty(currentTemplate.name))
            {
                itemNameText.text = currentTemplate.name;
            }
            else
            {
                // Fallback: dùng itemCode nếu không có template
                itemNameText.text = !string.IsNullOrEmpty(slotData.itemCode) ? slotData.itemCode : "Unknown Item";
            }
        }

        // 3. Mô tả item
        if (itemDescriptionText != null)
        {
            if (currentTemplate != null && !string.IsNullOrEmpty(currentTemplate.description))
            {
                itemDescriptionText.text = currentTemplate.description;
            }
            else
            {
                itemDescriptionText.text = "Không có mô tả.";
            }
        }

        // 4. Nút sử dụng - cập nhật text tuỳ loại item
        if (useButtonText != null)
        {
            if (currentTemplate != null)
            {
                // item_type: dựa theo enum hoặc category để đổi text nút
                // Ví dụ: item_type == 1 (Consumable) → "Sử dụng", == 0 (Weapon/Armor) → "Trang bị"
                switch (currentTemplate.item_type)
                {
                    case 0: // Weapon
                        useButtonText.text = "Trang bị";
                        break;
                    case 1: // Consumable
                        useButtonText.text = "Sử dụng";
                        break;
                    case 3: // Armor
                        useButtonText.text = "Trang bị";
                        break;
                    default:
                        useButtonText.text = "Sử dụng";
                        break;
                }
            }
            else
            {
                useButtonText.text = "Sử dụng";
            }
        }

        // Hiện panel
        gameObject.SetActive(true);

        Debug.Log($"[ItemDetailPanel] Hiển thị chi tiết: {itemNameText?.text} (code={slotData.itemCode}, qty={slotData.quantity})");
    }

    /// <summary>
    /// Ẩn panel chi tiết
    /// </summary>
    public void Hide()
    {
        gameObject.SetActive(false);
        currentSlotData = null;
        currentTemplate = null;
    }

    /// <summary>
    /// Kiểm tra panel có đang hiển thị không
    /// </summary>
    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }

    /// <summary>
    /// Callback khi nhấn nút sử dụng
    /// </summary>
    private void OnUseButtonPressed()
    {
        if (currentSlotData == null)
        {
            Debug.LogWarning("[ItemDetailPanel] OnUseButtonPressed: Không có item nào đang được chọn!");
            return;
        }

        Debug.Log($"[ItemDetailPanel] Nhấn sử dụng item: code={currentSlotData.itemCode}, slot={currentSlotData.slotIndex}");

        // Fire event để các script khác xử lý (InventoryNetworkBridge, v.v.)
        OnUseItemClicked?.Invoke(currentSlotData);

        // Gọi trực tiếp InventoryNetworkBridge nếu có
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge != null)
        {
            bridge.RequestUseItem(currentSlotData.slotIndex, currentSlotData.itemCode);
        }
        else
        {
            Debug.LogWarning("[ItemDetailPanel] Không tìm thấy InventoryNetworkBridge để gửi request sử dụng item!");
        }
    }

    private void OnDestroy()
    {
        if (useButton != null)
        {
            useButton.onClick.RemoveListener(OnUseButtonPressed);
        }
    }
}
