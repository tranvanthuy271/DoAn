using System.Collections.Generic;
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

    [Header("Icon Layout")]
    [Tooltip("Kích thước fallback nếu icon chưa có RectTransform hợp lệ ở frame đầu.")]
    [SerializeField] private Vector2 fallbackIconMaxSize = new Vector2(128f, 128f);

    // Dữ liệu item đang hiển thị
    private InventorySlotDto currentSlotData;
    private ItemTemplateDto currentTemplate;
    private Vector2 itemIconMaxSize;
    private Action _primaryButtonActionOverride;

    // Cờ để Start() biết ShowItem/ShowEquipmentItem đã được gọi trước khi Start() chạy
    private bool _hasBeenShown;

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

        itemIconMaxSize = ResolveItemIconMaxSize();
        UIRuntimeAssetHelper.ApplyNotoSans(itemNameText, itemDescriptionText, useButtonText);
    }

    private void Start()
    {
        // Chỉ ẩn nếu ShowItem/ShowEquipmentItem chưa được gọi trước Start().
        // Trường hợp: panel được Instantiate() rồi ShowItem() gọi ngay cùng frame,
        // Start() chạy frame sau → không được ẩn panel đang hiển thị.
        if (hideOnStart && !_hasBeenShown)
        {
            Hide();
        }
    }

    /// <param name="showUseButton">false khi không muốn hiện nút hành động.</param>
    /// <param name="buttonTextOverride">Cho phép đổi text nút thành "Mua" hoặc text khác.</param>
    /// <param name="primaryButtonAction">Callback tùy biến cho nút hành động; null = dùng luồng mặc định của inventory.</param>
    public void ShowItem(InventorySlotDto slotData, bool showUseButton = true,
                         string buttonTextOverride = null, Action primaryButtonAction = null)
    {
        if (slotData == null || slotData.quantity <= 0)
        {
            Hide();
            return;
        }

        currentSlotData = slotData;
        currentTemplate = null;
        _primaryButtonActionOverride = primaryButtonAction;

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
            UIRuntimeAssetHelper.SetSpriteWithNativeFit(itemIcon, icon, itemIconMaxSize);
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

        // 4. Nút hành động
        if (useButton != null)
            useButton.gameObject.SetActive(showUseButton);

        if (showUseButton && useButtonText != null)
        {
            if (!string.IsNullOrEmpty(buttonTextOverride))
            {
                useButtonText.text = buttonTextOverride;
            }
            else if (currentTemplate != null)
            {
                useButtonText.text = currentTemplate.category == 1 ? "Trang bị" : "Sử dụng";
            }
            else
            {
                useButtonText.text = "Sử dụng";
            }
        }

        _hasBeenShown = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    /// <summary>
    /// Hiển thị chi tiết trang bị (EquipmentItemDto) kèm các chỉ số strOptions.
    /// Gọi từ UpgradePanel khi click ô trang bị, bùa, hoặc nút "Xem TT".
    /// </summary>
    public void ShowEquipmentItem(EquipmentItemDto item, List<OptionTemplateDto> optTemplates = null)
    {
        if (item == null) return;

        _primaryButtonActionOverride = null;
        var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(item.id);

        // Icon
        if (itemIcon != null && tmpl != null && IconDatabase.Instance != null)
        {
            var sp = IconDatabase.Instance.GetIcon(tmpl.idIcon.ToString());
            UIRuntimeAssetHelper.SetSpriteWithNativeFit(itemIcon, sp, itemIconMaxSize);
        }
        else if (itemIcon != null)
        {
            itemIcon.enabled = false;
        }

        // Tên + cấp nâng
        if (itemNameText != null)
        {
            string n = tmpl != null ? tmpl.name : $"Item #{item.id}";
            itemNameText.text = item.upgradeLevel > 0 ? $"{n}  +{item.upgradeLevel}" : n;
        }

        // Mô tả + chỉ số
        if (itemDescriptionText != null)
        {
            var sb = new System.Text.StringBuilder();

            if (tmpl != null)
            {
                if (tmpl.levelNeed > 0)
                    sb.AppendLine($"<color=#ff6060>Yêu cầu cấp: {tmpl.levelNeed}</color>");

                if (tmpl.gioiTinh == 0)
                    sb.AppendLine("<color=#ff6060>Yêu cầu giới tính: Nam</color>");
                else if (tmpl.gioiTinh == 1)
                    sb.AppendLine("<color=#ff6060>Yêu cầu giới tính: Nữ</color>");

                if (tmpl.idClass > 0)
                {
                    string[] elements = { "", "Hỏa", "Thủy", "Thổ", "Kim", "Mộc" };
                    string elem = tmpl.idClass < elements.Length ? elements[tmpl.idClass] : tmpl.idClass.ToString();
                    sb.AppendLine($"<color=#ff6060>Hệ: {elem}</color>");
                }

                if (tmpl.isLock)
                    sb.AppendLine("Đã khóa");

                if (!string.IsNullOrWhiteSpace(tmpl.detail))
                {
                    sb.AppendLine();
                    sb.AppendLine(tmpl.detail);
                }
            }

            // strOptions stats
            if (!string.IsNullOrEmpty(item.strOptions))
            {
                if (sb.Length > 0) sb.AppendLine("───────────────");
                var opts = EquippedOptionDisplay.ParseAll(item.strOptions);
                foreach (var opt in opts)
                {
                    OptionTemplateDto ot = optTemplates?.Find(t => t.id == opt.optionId);
                    string line = ot != null
                        ? ot.BuildLabel(opt.value)
                        : $"[{opt.optionId}] +{opt.value}";
                    // Màu: trắng nếu active, xám nếu chưa đạt cấp
                    bool active = ot == null || ot.IsActive(item.upgradeLevel);
                    // TMP rich text không cần ở đây vì itemDescriptionText không set color per-line
                    sb.AppendLine(line);
                }
            }

            itemDescriptionText.text = sb.ToString().TrimEnd();
        }

        // Ẩn nút sử dụng (trang bị đang ở ô, không dùng được)
        if (useButton != null) useButton.gameObject.SetActive(false);

        _hasBeenShown = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        currentSlotData = null;
        currentTemplate = null;
        _primaryButtonActionOverride = null;
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
        if (_primaryButtonActionOverride != null)
        {
            _primaryButtonActionOverride.Invoke();
            return;
        }

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

    private Vector2 ResolveItemIconMaxSize()
    {
        if (itemIcon == null)
        {
            return fallbackIconMaxSize;
        }

        Vector2 currentSize = itemIcon.rectTransform.sizeDelta;
        if (currentSize.x > 0f && currentSize.y > 0f)
        {
            return currentSize;
        }

        Vector2 rectSize = itemIcon.rectTransform.rect.size;
        if (rectSize.x > 0f && rectSize.y > 0f)
        {
            return rectSize;
        }

        return fallbackIconMaxSize;
    }
}
