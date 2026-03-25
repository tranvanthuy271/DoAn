using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// ItemDetailPanel - Hiển thị chi tiết item khi nhấn vào slot trong inventory
/// Bao gồm: hình ảnh, tên, mô tả, nút sử dụng
///
/// Setup Prefab trong Unity:
/// 1. Tạo Panel trong Canvas (đặt tên "ItemDetailPanel")
/// 2. Thêm các UI con: Image (icon), TMP_Text (tên), TMP_Text (mô tả), Button (sử dụng)
/// 3. Gắn script này lên Panel và kéo các reference vào Inspector
/// 4. Kéo thả Panel này vào thư mục Assets/Prefabs để tạo Prefab
/// 5. Xóa Panel gốc ra khỏi scene (chỉ giữ Prefab)
/// 6. Trong InventoryUI → gán Prefab vào slot "Item Detail Panel Prefab"
/// 7. (Tùy chọn) Gán Canvas gốc vào "Item Detail Panel Parent" để panel render đúng layer
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

    [Tooltip("Nút đóng panel")]
    [SerializeField] private Button btnClose;

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
            useButton.onClick.AddListener(OnUseButtonPressed);

        if (btnClose != null)
            btnClose.onClick.AddListener(Hide);

        // Đảm bảo panel render đè lên toàn bộ UI khác
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200;

        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    private void Start()
    {
        if (hideOnStart)
        {
            Hide();
        }
    }

    public void ShowItem(InventorySlotDto slotData)
    {
        if (slotData == null || slotData.quantity <= 0)
        {
            Hide();
            return;
        }

        currentSlotData = slotData;
        currentTemplate = null;

        if (ItemTemplateManager.Instance != null)
        {
            if (slotData.itemTemplateId > 0)
                currentTemplate = ItemTemplateManager.Instance.GetItemTemplate(slotData.itemTemplateId);
            if (currentTemplate == null && !string.IsNullOrEmpty(slotData.itemCode))
                currentTemplate = ItemTemplateManager.Instance.GetItemTemplateByCode(slotData.itemCode);
        }

        // 1. Icon
        if (itemIcon != null)
        {
            Sprite icon = null;
            if (IconDatabase.Instance != null && !string.IsNullOrEmpty(slotData.iconId))
                icon = IconDatabase.Instance.GetIcon(slotData.iconId);
            itemIcon.sprite  = icon;
            itemIcon.enabled = icon != null;
        }

        // 2. Tên item (bold, góc trên-trái)
        if (itemNameText != null)
        {
            string rawName = (currentTemplate != null && !string.IsNullOrEmpty(currentTemplate.name))
                ? currentTemplate.name
                : (!string.IsNullOrEmpty(slotData.itemCode) ? slotData.itemCode : "Unknown Item");
            itemNameText.text = rawName;
        }

        // 3. Phần thân: cấp yêu cầu • khóa • xếp chồng • giá bán • mô tả
        if (itemDescriptionText != null)
        {
            var sb = new System.Text.StringBuilder();

            if (currentTemplate != null)
            {
                // Cấp yêu cầu
                if (currentTemplate.levelNeed > 0)
                    sb.AppendLine($"Yêu cầu cấp: {currentTemplate.levelNeed}");

                // Trạng thái khóa (từ template)
                sb.AppendLine(currentTemplate.isLock ? "Đã khóa" : "Không khóa");

                // Xếp chồng
                sb.AppendLine(currentTemplate.isXepChong ? "Có thể xếp chồng" : "Không thể xếp chồng");

                // Giá bán (từ template)
                string priceUnit = currentTemplate.isLock ? "bạc khóa" : "bạc";
                sb.AppendLine($"Giá bán: {currentTemplate.sellPrice} {priceUnit}");

                // Mô tả
                if (!string.IsNullOrWhiteSpace(currentTemplate.detail))
                {
                    sb.AppendLine();
                    sb.Append(currentTemplate.detail);
                }
            }
            else
            {
                sb.Append("Không có thông tin.");
            }

            itemDescriptionText.text = sb.ToString().TrimEnd();
        }

        // 4. Text nút sử dụng
        if (useButtonText != null && currentTemplate != null)
        {
            useButtonText.text = currentTemplate.category == 1 ? "Trang bị" : "Sử dụng";
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
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

        // Fire event (các listener khác có thể xử lý thêm)
        OnUseItemClicked?.Invoke(currentSlotData);

        // Ưu tiên dùng ItemUseHandler (singleton)
        if (ItemUseHandler.Instance != null)
        {
            ItemUseHandler.Instance.RequestUseItem(currentSlotData);
        }
        else
        {
            // Fallback: gọi trực tiếp bridge
            var bridge = FindObjectOfType<InventoryNetworkBridge>();
            if (bridge != null)
                bridge.RequestUseItem(currentSlotData.slotIndex, currentSlotData.itemCode, currentSlotData.itemTemplateId);
            else
                Debug.LogWarning("[ItemDetailPanel] Không tìm thấy ItemUseHandler và InventoryNetworkBridge!");
        }
    }

    private void OnDestroy()
    {
        if (useButton != null)
            useButton.onClick.RemoveListener(OnUseButtonPressed);
        if (btnClose != null)
            btnClose.onClick.RemoveListener(Hide);
    }
}
