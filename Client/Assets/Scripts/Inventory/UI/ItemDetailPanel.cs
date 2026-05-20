using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ItemDetailPanel - hiển thị chi tiết vật phẩm/trang bị khi nhấn vào slot inventory.
/// </summary>
public class ItemDetailPanel : MonoBehaviour
{
    private const string Red = "#ff4040";
    private const string White = "#ffffff";
    private const string Dim = "#9a9a9a";
    private const string Gold = "#ffd24a";
    private const string Green = "#b7e34d";

    [Header("UI References")]
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Button useButton;
    [SerializeField] private TMP_Text useButtonText;
    [SerializeField] private Button btnClose;

    [Header("Action Buttons")]
    [SerializeField] private Button shortcutButton;
    [SerializeField] private TMP_Text shortcutButtonText;
    [SerializeField] private Button splitButton;
    [SerializeField] private TMP_Text splitButtonText;
    [SerializeField] private Button dropButton;
    [SerializeField] private TMP_Text dropButtonText;
    [SerializeField] private Button useManyButton;
    [SerializeField] private TMP_Text useManyButtonText;

    [Header("Settings")]
    [SerializeField] private bool hideOnStart = true;

    [Header("Icon Layout")]
    [SerializeField] private Vector2 fallbackIconMaxSize = new Vector2(48f, 48f);

    private static List<OptionTemplateDto> s_optionTemplates;
    private static bool s_optionTemplatesLoading;

    private InventorySlotDto currentSlotData;
    private EquipmentItemDto currentEquipmentData;
    private ItemTemplateDto currentTemplate;
    private Vector2 itemIconMaxSize;
    private Action _primaryButtonActionOverride;
    private int? _requiredLevelOverride;
    private bool _hasBeenShown;
    private Coroutine _refreshAfterOptionsRoutine;

    public event Action<InventorySlotDto> OnUseItemClicked;

    private void Awake()
    {
        UIDraggablePanel.Ensure(gameObject);

        if (useButton != null)
            useButton.onClick.AddListener(OnUseButtonPressed);
        if (shortcutButton != null)
            shortcutButton.onClick.AddListener(OnShortcutButtonPressed);
        if (splitButton != null)
            splitButton.onClick.AddListener(OnSplitButtonPressed);
        if (dropButton != null)
            dropButton.onClick.AddListener(OnDropButtonPressed);
        if (useManyButton != null)
            useManyButton.onClick.AddListener(OnUseManyButtonPressed);
        if (btnClose != null)
            btnClose.onClick.AddListener(Hide);

        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        itemIconMaxSize = ResolveItemIconMaxSize();
        UIRuntimeAssetHelper.ApplyNotoSans(
            itemNameText,
            itemDescriptionText,
            useButtonText,
            shortcutButtonText,
            splitButtonText,
            dropButtonText,
            useManyButtonText);

        if (itemDescriptionText != null)
        {
            itemDescriptionText.richText = true;
            itemDescriptionText.enableWordWrapping = true;
        }
    }

    private void Start()
    {
        if (hideOnStart && !_hasBeenShown)
            Hide();
    }

    public void ShowItem(InventorySlotDto slotData, bool showUseButton = true,
                         string buttonTextOverride = null, Action primaryButtonAction = null,
                         int? requiredLevelOverride = null)
    {
        if (slotData == null || slotData.quantity <= 0)
        {
            Hide();
            return;
        }

        currentSlotData = slotData;
        currentEquipmentData = null;
        currentTemplate = ResolveTemplate(slotData.itemTemplateId, slotData.itemCode);
        _primaryButtonActionOverride = primaryButtonAction;
        _requiredLevelOverride = requiredLevelOverride;

        SetIcon(slotData.iconId, currentTemplate);

        bool isEquipment = IsEquipment(slotData, currentTemplate);
        if (itemNameText != null)
            itemNameText.text = BuildDisplayName(slotData, currentTemplate, isEquipment);

        if (itemDescriptionText != null)
            itemDescriptionText.text = isEquipment
                ? BuildEquipmentBody(slotData, currentTemplate, s_optionTemplates)
                : BuildRegularItemBody(slotData, currentTemplate);

        ConfigureButtons(isEquipment, showUseButton, buttonTextOverride);

        if (isEquipment && HasOptions(slotData.strOptions) && s_optionTemplates == null)
            RefreshWhenOptionTemplatesLoaded();

        ShowPanel();
    }

    /// <summary>
    /// Hiển thị trang bị đang mặc hoặc trang bị từ flow nâng cấp.
    /// </summary>
    public void ShowEquipmentItem(EquipmentItemDto item, List<OptionTemplateDto> optTemplates = null)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        currentSlotData = null;
        currentEquipmentData = item;
        currentTemplate = ResolveTemplate(item.id, item.itemCode);
        _primaryButtonActionOverride = null;
        _requiredLevelOverride = null;

        SetIcon(item.iconId, currentTemplate);

        if (itemNameText != null)
            itemNameText.text = BuildDisplayName(item, currentTemplate);

        if (optTemplates != null)
            s_optionTemplates = optTemplates;

        if (itemDescriptionText != null)
            itemDescriptionText.text = BuildEquipmentBody(item, currentTemplate, optTemplates ?? s_optionTemplates);

        ConfigureButtons(isEquipment: true, showUseButton: false, buttonTextOverride: null);

        if (HasOptions(item.strOptions) && s_optionTemplates == null)
            RefreshWhenOptionTemplatesLoaded();

        ShowPanel();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        currentSlotData = null;
        currentEquipmentData = null;
        currentTemplate = null;
        _primaryButtonActionOverride = null;
        _requiredLevelOverride = null;
    }

    public bool IsVisible()
    {
        return gameObject.activeSelf;
    }

    private void ShowPanel()
    {
        _hasBeenShown = true;
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void SetIcon(string iconId, ItemTemplateDto template)
    {
        if (itemIcon == null)
            return;

        Sprite icon = null;
        string resolvedIconId = !string.IsNullOrEmpty(iconId)
            ? iconId
            : template != null && template.idIcon > 0 ? template.idIcon.ToString() : null;

        if (IconDatabase.Instance != null && !string.IsNullOrEmpty(resolvedIconId))
            icon = IconDatabase.Instance.GetIcon(resolvedIconId);

        UIRuntimeAssetHelper.SetSpriteWithNativeFit(itemIcon, icon, itemIconMaxSize);
    }

    private void ConfigureButtons(bool isEquipment, bool showUseButton, string buttonTextOverride)
    {
        bool stackable = currentTemplate != null && currentTemplate.isXepChong;
        int quantity = currentSlotData?.quantity ?? 1;
        bool hasInventorySlot = currentSlotData != null && currentSlotData.slotIndex >= 0;

        SetButton(shortcutButton, shortcutButtonText, hasInventorySlot, "Phím tắt");
        SetButton(splitButton, splitButtonText, hasInventorySlot && !isEquipment && stackable && quantity > 1, "Tách");
        SetButton(dropButton, dropButtonText, hasInventorySlot, "Vứt bỏ");
        SetButton(useManyButton, useManyButtonText, hasInventorySlot && !isEquipment && showUseButton && stackable && quantity > 1, "SD nhiều");

        if (useButton != null)
            useButton.gameObject.SetActive(showUseButton);

        if (showUseButton && useButtonText != null)
        {
            if (!string.IsNullOrEmpty(buttonTextOverride))
                useButtonText.text = buttonTextOverride;
            else if (isEquipment)
                useButtonText.text = "Trang bị";
            else
                useButtonText.text = "Sử dụng";
        }
    }

    private static void SetButton(Button button, TMP_Text label, bool active, string text)
    {
        if (button != null)
            button.gameObject.SetActive(active);
        if (label != null)
            label.text = text;
    }

    private string BuildRegularItemBody(InventorySlotDto slot, ItemTemplateDto template)
    {
        if (template == null)
            return "Không có thông tin.";

        var sb = new StringBuilder();
        int requiredLevel = ResolveRequiredLevel(template);

        if (requiredLevel > 0)
            AppendLine(sb, $"Yêu cầu cấp: {requiredLevel}", White);

        AppendLine(sb, ResolveLocked(slot, template) ? "Đã khóa" : "Không khóa", White);
        AppendLine(sb, template.isXepChong ? "Có thể xếp chồng" : "Không thể xếp chồng", White);
        AppendLine(sb, BuildSellPriceLine(slot, template), White);

        if (!string.IsNullOrWhiteSpace(template.detail))
            AppendLine(sb, template.detail.Trim(), White);

        return sb.ToString().TrimEnd();
    }

    private string BuildEquipmentBody(InventorySlotDto slot, ItemTemplateDto template, List<OptionTemplateDto> optionTemplates)
    {
        int upgradeLevel = slot?.upgradeLevel ?? 0;
        string strOptions = slot?.strOptions;
        bool locked = ResolveLocked(slot, template);
        return BuildEquipmentBodyCore(template, upgradeLevel, strOptions, locked, optionTemplates);
    }

    private string BuildEquipmentBody(EquipmentItemDto item, ItemTemplateDto template, List<OptionTemplateDto> optionTemplates)
    {
        int upgradeLevel = item?.upgradeLevel ?? 0;
        string strOptions = item?.strOptions;
        bool locked = template != null && template.isLock;
        return BuildEquipmentBodyCore(template, upgradeLevel, strOptions, locked, optionTemplates);
    }

    private string BuildEquipmentBodyCore(ItemTemplateDto template, int upgradeLevel, string strOptions,
                                          bool locked, List<OptionTemplateDto> optionTemplates)
    {
        var sb = new StringBuilder();

        if (template != null)
        {
            int requiredLevel = ResolveRequiredLevel(template);
            if (requiredLevel > 0)
                AppendLine(sb, $"Yêu cầu cấp: {requiredLevel}", Red);

            if (template.gioiTinh == 0)
                AppendLine(sb, "Yêu cầu giới tính: Nam", Red);
            else if (template.gioiTinh == 1)
                AppendLine(sb, "Yêu cầu giới tính: Nữ", Red);

            if (template.idClass > 0)
                AppendLine(sb, $"Hệ: {GetElementName(template.idClass)}", Red);

            AppendLine(sb, locked ? "Đã khóa" : "Không khóa", White);
            AppendLine(sb, BuildSellPriceLine(locked, template.sellPrice), White);

            if (!string.IsNullOrWhiteSpace(template.detail))
                AppendLine(sb, template.detail.Trim(), Green);
        }
        else
        {
            AppendLine(sb, "Không có thông tin template.", Dim);
        }

        if (HasOptions(strOptions))
        {
            if (sb.Length > 0)
                sb.AppendLine();

            var opts = EquippedOptionDisplay.ParseAll(strOptions);
            foreach (var opt in opts)
            {
                OptionTemplateDto templateOption = optionTemplates?.Find(t => t.id == opt.optionId);
                string line = templateOption != null
                    ? templateOption.BuildLabel(opt.value)
                    : $"Thuộc tính {opt.optionId}: +{opt.value}";

                bool active = templateOption == null || templateOption.IsActive(upgradeLevel);
                AppendLine(sb, line, active ? Gold : Dim);
            }
        }
        else
        {
            if (sb.Length > 0)
                sb.AppendLine();
            AppendLine(sb, "Không có thuộc tính.", Dim);
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendLine(StringBuilder sb, string text, string color)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        sb.Append("<color=");
        sb.Append(color);
        sb.Append('>');
        sb.Append(text);
        sb.AppendLine("</color>");
    }

    private int ResolveRequiredLevel(ItemTemplateDto template)
    {
        if (_requiredLevelOverride.HasValue)
            return Mathf.Max(0, _requiredLevelOverride.Value);

        return template != null ? Mathf.Max(0, template.levelNeed) : 0;
    }

    private static string BuildSellPriceLine(InventorySlotDto slot, ItemTemplateDto template)
    {
        return BuildSellPriceLine(ResolveLocked(slot, template), template != null ? template.sellPrice : 0);
    }

    private static string BuildSellPriceLine(bool locked, int sellPrice)
    {
        return $"Giá bán: {Mathf.Max(0, sellPrice)} {(locked ? "bạc khóa" : "bạc")}";
    }

    private static bool ResolveLocked(InventorySlotDto slot, ItemTemplateDto template)
    {
        return (slot != null && slot.isLocked) || (template != null && template.isLock);
    }

    private static bool IsEquipment(InventorySlotDto slot, ItemTemplateDto template)
    {
        if (template != null && template.category == 1)
            return true;

        if (slot == null)
            return false;

        return slot.upgradeLevel > 0 || HasOptions(slot.strOptions);
    }

    private static bool HasOptions(string strOptions)
    {
        return !string.IsNullOrWhiteSpace(strOptions);
    }

    private static string BuildDisplayName(InventorySlotDto slot, ItemTemplateDto template, bool isEquipment)
    {
        string name = template != null && !string.IsNullOrEmpty(template.name)
            ? template.name
            : !string.IsNullOrEmpty(slot.itemCode) ? slot.itemCode : "Unknown Item";

        return isEquipment && slot.upgradeLevel > 0 ? $"{name} (+{slot.upgradeLevel})" : name;
    }

    private static string BuildDisplayName(EquipmentItemDto item, ItemTemplateDto template)
    {
        string name = template != null && !string.IsNullOrEmpty(template.name)
            ? template.name
            : !string.IsNullOrEmpty(item.itemName) ? item.itemName : $"Item #{item.id}";

        return item.upgradeLevel > 0 ? $"{name} (+{item.upgradeLevel})" : name;
    }

    private static string GetElementName(int idClass)
    {
        return idClass switch
        {
            1 => "Hỏa",
            2 => "Thủy",
            3 => "Thổ",
            4 => "Kim",
            5 => "Mộc",
            6 => "Phong",
            _ => idClass.ToString()
        };
    }

    private static ItemTemplateDto ResolveTemplate(int templateId, string itemCode)
    {
        if (ItemTemplateManager.Instance == null)
            return null;

        ItemTemplateDto template = null;
        if (templateId > 0)
            template = ItemTemplateManager.Instance.GetItemTemplate(templateId);
        if (template == null && !string.IsNullOrEmpty(itemCode))
            template = ItemTemplateManager.Instance.GetItemTemplateByCode(itemCode);
        return template;
    }

    private void RefreshWhenOptionTemplatesLoaded()
    {
        RequestOptionTemplatesIfNeeded();
        if (!s_optionTemplatesLoading)
            return;

        if (_refreshAfterOptionsRoutine != null)
            StopCoroutine(_refreshAfterOptionsRoutine);
        _refreshAfterOptionsRoutine = StartCoroutine(RefreshCurrentDescriptionAfterOptions());
    }

    private IEnumerator RefreshCurrentDescriptionAfterOptions()
    {
        float timeoutAt = Time.realtimeSinceStartup + 6f;
        while (s_optionTemplatesLoading && Time.realtimeSinceStartup < timeoutAt)
            yield return null;

        if (s_optionTemplatesLoading)
            s_optionTemplatesLoading = false;

        _refreshAfterOptionsRoutine = null;
        RefreshCurrentEquipmentDescription();
    }

    private void RefreshCurrentEquipmentDescription()
    {
        if (!isActiveAndEnabled || itemDescriptionText == null || s_optionTemplates == null)
            return;

        if (currentSlotData != null && IsEquipment(currentSlotData, currentTemplate))
        {
            itemDescriptionText.text = BuildEquipmentBody(currentSlotData, currentTemplate, s_optionTemplates);
            return;
        }

        if (currentEquipmentData != null)
            itemDescriptionText.text = BuildEquipmentBody(currentEquipmentData, currentTemplate, s_optionTemplates);
    }

    private static void RequestOptionTemplatesIfNeeded()
    {
        if (s_optionTemplates != null || s_optionTemplatesLoading || GameplayCommandService.Instance == null)
            return;

        s_optionTemplatesLoading = true;
        GameplayCommandService.OnOptionTemplatesReceived -= HandleOptionTemplatesReceived;
        GameplayCommandService.OnOptionTemplatesReceived += HandleOptionTemplatesReceived;
        GameplayCommandService.Instance.GetOptionTemplatesServerRpc();
    }

    private static void HandleOptionTemplatesReceived(string json)
    {
        GameplayCommandService.OnOptionTemplatesReceived -= HandleOptionTemplatesReceived;
        s_optionTemplatesLoading = false;

        if (string.IsNullOrWhiteSpace(json) || json.Contains("\"error\""))
        {
            s_optionTemplates = new List<OptionTemplateDto>();
            return;
        }

        try
        {
            var wrapper = JsonUtility.FromJson<OptionTemplateListWrapper>(json);
            s_optionTemplates = wrapper?.options != null
                ? new List<OptionTemplateDto>(wrapper.options)
                : new List<OptionTemplateDto>();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ItemDetailPanel] Parse option templates failed: {ex.Message}");
            s_optionTemplates = new List<OptionTemplateDto>();
        }
    }

    private void OnUseButtonPressed()
    {
        if (_primaryButtonActionOverride != null)
        {
            _primaryButtonActionOverride.Invoke();
            return;
        }

        if (currentSlotData == null)
        {
            Debug.LogWarning("[ItemDetailPanel] OnUseButtonPressed: không có item đang chọn.");
            return;
        }

        OnUseItemClicked?.Invoke(currentSlotData);

        if (ItemUseHandler.Instance != null)
        {
            ItemUseHandler.Instance.RequestUseItem(currentSlotData);
            return;
        }

        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge != null)
            bridge.RequestUseItem(currentSlotData.slotIndex, currentSlotData.itemCode, currentSlotData.itemTemplateId);
        else
            Debug.LogWarning("[ItemDetailPanel] Không tìm thấy ItemUseHandler hoặc InventoryNetworkBridge.");
    }

    private void OnShortcutButtonPressed()
    {
        Debug.Log("[ItemDetailPanel] Nút Phím tắt đã được hiển thị. Chưa có flow gán phím tắt trong client hiện tại.");
    }

    private void OnSplitButtonPressed()
    {
        Debug.Log("[ItemDetailPanel] Nút Tách đã được hiển thị. Chưa có flow tách stack trong client hiện tại.");
    }

    private void OnDropButtonPressed()
    {
        Debug.Log("[ItemDetailPanel] Nút Vứt bỏ đã được hiển thị. Chưa có API vứt bỏ item trong client hiện tại.");
    }

    private void OnUseManyButtonPressed()
    {
        Debug.Log("[ItemDetailPanel] Nút SD nhiều đã được hiển thị. Chưa có flow dùng nhiều trong client hiện tại.");
    }

    private void OnDestroy()
    {
        if (useButton != null)
            useButton.onClick.RemoveListener(OnUseButtonPressed);
        if (shortcutButton != null)
            shortcutButton.onClick.RemoveListener(OnShortcutButtonPressed);
        if (splitButton != null)
            splitButton.onClick.RemoveListener(OnSplitButtonPressed);
        if (dropButton != null)
            dropButton.onClick.RemoveListener(OnDropButtonPressed);
        if (useManyButton != null)
            useManyButton.onClick.RemoveListener(OnUseManyButtonPressed);
        if (btnClose != null)
            btnClose.onClick.RemoveListener(Hide);
    }

    private Vector2 ResolveItemIconMaxSize()
    {
        if (itemIcon == null)
            return fallbackIconMaxSize;

        Vector2 currentSize = itemIcon.rectTransform.sizeDelta;
        if (currentSize.x > 0f && currentSize.y > 0f)
            return currentSize;

        Vector2 rectSize = itemIcon.rectTransform.rect.size;
        if (rectSize.x > 0f && rectSize.y > 0f)
            return rectSize;

        return fallbackIconMaxSize;
    }
}
