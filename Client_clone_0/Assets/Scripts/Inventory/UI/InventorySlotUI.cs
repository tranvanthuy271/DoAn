using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// InventorySlotUI - Hiển thị 1 ô item trong UI túi đồ (client-side)
// - Nhận dữ liệu từ InventorySlotDto (JSON từ server đã parse).
// - Gắn script này lên prefab Slot (Image icon + Text số lượng + optional highlight equip).
public class InventorySlotUI : MonoBehaviour
{
    private const string RuntimeIconObjectName = "RuntimeItemIcon";

    [Header("UI References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private GameObject equippedMark;
    [Tooltip("Image/GameObject hiển thị khi item bị khóa (isLocked = true)")]
    [SerializeField] private GameObject lockMark;

    [Tooltip("Image nền tối (90% kích thước) – bật khi slot có item, tắt khi trống. Gán 'ItemBg' child từ prefab.")]
    [SerializeField] private Image itemBgImage;

    [Header("Icon Layout")]
    [Tooltip("Padding để icon không chạm viền slot.")]
    [SerializeField] private Vector2 iconPadding = new Vector2(20f, 20f);
    [Tooltip("Kích thước fallback nếu RectTransform slot chưa có size ở frame đầu.")]
    [SerializeField] private Vector2 fallbackIconMaxSize = new Vector2(80f, 80f);

    [Header("Select Mode (Blacksmith – chọn đá/bùa)")]
    [Tooltip("Button 'Chọn' hiện khi ô khớp filter. Thêm vào prefab slot, ẩn mặc định.")]
    [SerializeField] private Button chooseButton;
    [Tooltip("CanvasGroup để mờ ô không khớp filter. Có thể dùng CanvasGroup trên root slot.")]
    [SerializeField] private CanvasGroup canvasGroup;

    private int slotIndex;
    private InventorySlotDto currentData;
    private bool _inSelectMode;
    private bool _canSelectInSelectMode;
    private Action _selectModeCallback;

    private void Awake()
    {
        EnsureRuntimeReferences();
        ApplyTheme();
    }

    // Event khi người chơi click vào slot có item
    public event Action<InventorySlotDto> OnSlotClicked;

    // Khởi tạo ô với index. Gọi 1 lần khi tạo grid.
    public void Init(int index)
    {
        slotIndex = index;

        EnsureRuntimeReferences();
        ApplyTheme();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        Clear();
    }

    // Xóa dữ liệu hiển thị
    public void Clear()
    {
        EnsureRuntimeReferences();
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

        if (itemBgImage != null)
            itemBgImage.enabled = false;

        // Reset select mode
        SetSelectMode(false, false, null);
    }

    // Cập nhật hiển thị theo dữ liệu inventory slot từ server
    public void SetSlot(InventorySlotDto slot)
    {
        EnsureRuntimeReferences();
        currentData = slot;

        if (slot == null || slot.quantity <= 0)
        {
            if (slot != null && slot.quantity == 0)
            {
                //Debug.Log($"[InventorySlotUI] SetSlot: Slot {slotIndex} trống (quantity = 0)");
            }
            Clear();
            return;
        }

      //  Debug.Log($"[InventorySlotUI] SetSlot: Slot {slotIndex} - itemCode={slot.itemCode}, iconId={slot.iconId}, qty={slot.quantity}");

        // Set icon theo iconId (trùng tên sprite trong Resources/ItemIcons hoặc key Addressables)
        if (iconImage != null)
        {
            if (IconDatabase.Instance == null)
            {
              //  Debug.LogWarning($"[InventorySlotUI] SetSlot: IconDatabase.Instance is null! Không thể load icon cho slot {slotIndex}");
                iconImage.enabled = false;
                iconImage.sprite = null;
            }
            else
            {
                Sprite icon = null;
                string resolvedIconId = slot.iconId;

                if (!string.IsNullOrEmpty(resolvedIconId))
                    icon = IconDatabase.Instance.GetIcon(resolvedIconId);

                if (icon == null && slot.id > 0)
                {
                    var template = ItemTemplateManager.Instance?.GetItemTemplate(slot.id);
                    if (template != null && template.idIcon > 0)
                    {
                        resolvedIconId = template.idIcon.ToString();
                        icon = IconDatabase.Instance.GetIcon(resolvedIconId);
                    }
                }

                if (icon != null)
                {
                    UIRuntimeAssetHelper.SetSpriteWithNativeFit(iconImage, icon, GetMaxIconSize());
                   // Debug.Log($"[InventorySlotUI] SetSlot: Slot {slotIndex} - Đã load icon thành công: {resolvedIconId}");
                }
                else
                {
                    iconImage.enabled = false;
                    iconImage.sprite = null;
                  //  Debug.LogWarning($"[InventorySlotUI] SetSlot: Slot {slotIndex} - KHÔNG tìm thấy icon với iconId='{resolvedIconId}' trong IconDatabase!");
                }
            }
        }
        else
        {
          //  Debug.LogWarning($"[InventorySlotUI] SetSlot: Slot {slotIndex} - iconImage is null! Chưa gán trong Inspector.");
        }

        if (quantityText != null)
        {
            // Đơn giản: nếu quantity > 1 thì hiển thị số
            quantityText.text = slot.quantity > 1 ? slot.quantity.ToString() : string.Empty;
        }
        else
        {
          //  Debug.LogWarning($"[InventorySlotUI] SetSlot: Slot {slotIndex} - quantityText is null! Chưa gán trong Inspector.");
        }

        if (equippedMark != null)
        {
            equippedMark.SetActive(slot.isEquipped);
        }

        if (lockMark != null)
        {
            lockMark.SetActive(slot.isLocked);
        }

        if (itemBgImage != null)
            itemBgImage.enabled = true;
    }

    // Lấy dữ liệu slot hiện tại
    public InventorySlotDto GetCurrentData()
    {
        return currentData;
    }

    // Bật/tắt chế độ chọn item (dùng khi cần chọn đá / bùa cho cường hóa).
    // - inSelectMode = true  → ô khớp filter hiện btn "Chọn"; ô không khớp bị mờ
    // - canSelect = true     → ô này khớp filter, hiện btn "Chọn"
    // - onSelect             → callback khi nhấn "Chọn"
    public void SetSelectMode(bool inSelectMode, bool canSelect, System.Action onSelect)
    {
        _inSelectMode = inSelectMode;
        _canSelectInSelectMode = canSelect;
        _selectModeCallback = onSelect;

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

    // Gọi từ Button OnClick trên prefab Slot.
    // Hiển thị panel chi tiết item khi nhấn vào.
    public void OnClick()
    {
        if (currentData == null || currentData.quantity <= 0)
            return;

        // Fallback cho prefab cũ không có ChooseButton:
        // nếu đang ở select mode của Blacksmith thì click trực tiếp vào slot để chọn item.
        if (_inSelectMode)
        {
            if (_canSelectInSelectMode)
                _selectModeCallback?.Invoke();
            return;
        }

      //  Debug.Log($"[InventorySlotUI] Clicked slot {slotIndex} - itemCode={currentData.itemCode}, qty={currentData.quantity}");

        // Fire event để InventoryUI mở panel chi tiết
        OnSlotClicked?.Invoke(currentData);
    }

    private void ApplyTheme()
    {
        UIRuntimeAssetHelper.ApplyNotoSans(quantityText);
    }

    private void EnsureRuntimeReferences()
    {
        bool iconUsesRootGraphic = iconImage == null || iconImage.gameObject == gameObject;
        if (!iconUsesRootGraphic)
        {
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            return;
        }

        Transform existingTransform = transform.Find(RuntimeIconObjectName);
        RectTransform iconRect;
        Image dedicatedIconImage;

        if (existingTransform == null)
        {
            GameObject iconObject = new GameObject(RuntimeIconObjectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.SetParent(transform, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = fallbackIconMaxSize;
            iconRect.SetAsFirstSibling();

            dedicatedIconImage = iconObject.GetComponent<Image>();
        }
        else
        {
            iconRect = existingTransform as RectTransform;
            dedicatedIconImage = existingTransform.GetComponent<Image>();
            if (dedicatedIconImage == null)
            {
                dedicatedIconImage = existingTransform.gameObject.AddComponent<Image>();
            }

            iconRect.SetAsFirstSibling();
        }

        dedicatedIconImage.raycastTarget = false;
        dedicatedIconImage.preserveAspect = true;
        dedicatedIconImage.enabled = false;
        iconImage = dedicatedIconImage;
    }

    private Vector2 GetMaxIconSize()
    {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null)
        {
            return fallbackIconMaxSize;
        }

        Vector2 slotSize = rootRect.rect.size;
        if (slotSize.x <= 0f || slotSize.y <= 0f)
        {
            return fallbackIconMaxSize;
        }

        return new Vector2(
            Mathf.Max(0f, slotSize.x - iconPadding.x),
            Mathf.Max(0f, slotSize.y - iconPadding.y));
    }
}

