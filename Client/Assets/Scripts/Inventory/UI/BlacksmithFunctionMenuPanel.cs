using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// BlacksmithFunctionMenuPanel
// CÁCH CHUYỂN SANG PREFAB (làm một lần trong Unity Editor):
// 1. Tạo một GameObject rỗng trong scene, đặt tên "BlacksmithFunctionMenuCanvas".
// 2. Gắn component này lên GameObject đó.
// 3. Trong Inspector → chuột phải vào tên component → "Tạo UI Trong Editor".
// 4. Hierarchy con tự động tạo ra; kiểm tra + chỉnh sửa trực tiếp trong Scene view.
// 5. Kéo "BlacksmithFunctionMenuCanvas" vào thư mục Assets/Prefabs/UI để lưu thành prefab.
// 6. Xoá GameObject tạm trong scene; dùng prefab từ đây trở đi.
// Các SerializeField bên dưới tự động được gán khi chạy "Tạo UI Trong Editor".
// Sau khi có prefab, có thể đổi màu / thêm ảnh / đổi font thoải mái mà không cần sửa code.
public class BlacksmithFunctionMenuPanel : MonoBehaviour
{
    public static BlacksmithFunctionMenuPanel Instance { get; private set; }

    // Màu mặc định (chỉ dùng khi tự xây UI lần đầu)
    private readonly Color backdropColor = new(0.03f, 0.04f, 0.07f, 0.78f);
    private readonly Color cardColor     = new(0.08f, 0.09f, 0.12f, 0.96f);
    private readonly Color titleColor    = new(1f, 0.92f, 0.72f, 1f);
    private readonly Color bodyColor     = new(0.92f, 0.95f, 1f, 1f);
    private readonly Color statusColor   = new(0.76f, 0.83f, 0.95f, 1f);

    // SerializeField references
    // Các field này được điền tự động khi chạy Context Menu "Tạo UI Trong Editor".
    // Nếu prefab đã được thiết lập, Awake() sẽ dùng trực tiếp các references này.
    [Header("Card")]
    [SerializeField] private RectTransform cardTransform;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button equipmentUpgradeButton;
    [SerializeField] private Button primaryGeneUpgradeButton;
    [SerializeField] private Button secondarySelectButton;
    [SerializeField] private Button secondaryUpgradeButton;
    [SerializeField] private Button hybridFusionButton;
    [SerializeField] private Button closeButton;

    public static BlacksmithFunctionMenuPanel GetOrCreate()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindObjectOfType<BlacksmithFunctionMenuPanel>(true);
        if (Instance != null)
        {
            return Instance;
        }

        // Load customized prefab from Resources (created by Editor tool)
        var prefabGO = Resources.Load<GameObject>("UI/BlacksmithFunctionMenuCanvas");
        if (prefabGO != null)
        {
            var go = Object.Instantiate(prefabGO);
            go.name = "BlacksmithFunctionMenuCanvas";
            // Awake() sets Instance; if it ran synchronously we're done
            if (Instance != null) return Instance;
            // Fallback: grab component directly
            Instance = go.GetComponent<BlacksmithFunctionMenuPanel>();
            if (Instance != null) return Instance;
        }

        // Fallback: build procedurally (only if no prefab found in Resources)
        GameObject root = new(
            "BlacksmithFunctionMenuCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform rootTransform = root.GetComponent<RectTransform>();
        rootTransform.anchorMin = Vector2.zero;
        rootTransform.anchorMax = Vector2.one;
        rootTransform.offsetMin = Vector2.zero;
        rootTransform.offsetMax = Vector2.zero;

        Instance = root.AddComponent<BlacksmithFunctionMenuPanel>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureCanvasSetup();
        BuildUiIfNeeded();   // chỉ thực sự xây nếu cardTransform == null (không có prefab)
        WireListeners();     // luôn gắn lại listener sau mỗi lần Awake (listener không serialize được)
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Open()
    {
        WireListeners();     // đảm bảo listener luôn có khi mở
        HideOtherBlacksmithFlows();
        RefreshState();
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    // Gắn lại tất cả onClick listener cho các button.
    // Gọi trong Awake và Open vì UnityAction không được serialize trong prefab.
    private void WireListeners()
    {
        if (equipmentUpgradeButton  != null) { equipmentUpgradeButton.onClick.RemoveAllListeners();  equipmentUpgradeButton.onClick.AddListener(OpenEquipmentUpgrade); }
        if (primaryGeneUpgradeButton!= null) { primaryGeneUpgradeButton.onClick.RemoveAllListeners(); primaryGeneUpgradeButton.onClick.AddListener(OpenPrimaryGeneUpgrade); }
        if (secondarySelectButton   != null) { secondarySelectButton.onClick.RemoveAllListeners();   secondarySelectButton.onClick.AddListener(OpenSecondarySelect); }
        if (secondaryUpgradeButton  != null) { secondaryUpgradeButton.onClick.RemoveAllListeners();  secondaryUpgradeButton.onClick.AddListener(OpenSecondaryUpgrade); }
        if (hybridFusionButton       != null) { hybridFusionButton.onClick.RemoveAllListeners();       hybridFusionButton.onClick.AddListener(OpenHybridFusion); }
        if (closeButton             != null) { closeButton.onClick.RemoveAllListeners();             closeButton.onClick.AddListener(Close); }
    }

    private void EnsureCanvasSetup()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        RectTransform rootTransform = GetComponent<RectTransform>();
        rootTransform.anchorMin = Vector2.zero;
        rootTransform.anchorMax = Vector2.one;
        rootTransform.offsetMin = Vector2.zero;
        rootTransform.offsetMax = Vector2.zero;
        rootTransform.localScale = Vector3.one;
    }

    //  UI Setup — gọi tự động khi chưa có prefab, hoặc thủ công qua Context Menu

    // Xây dựng toàn bộ cây UI con dưới GameObject này (Canvas root).
    // Sau khi chạy, tất cả SerializeField được điền sẵn.
    // Kéo GameObject vào Prefabs để lưu và chỉnh sửa trực quan.
    [ContextMenu("Tạo UI Trong Editor")]
    private void BuildUiIfNeeded()
    {
        if (cardTransform != null)
        {
            // Prefab đã có đủ references → không cần xây lại
            return;
        }

        // Xoá children cũ nếu có (tránh duplicate khi gọi lại ContextMenu)
        for (int i = transform.childCount - 1; i >= 0; i--)
            DestroyImmediate(transform.GetChild(i).gameObject);

        RectTransform backdrop = CreateRectTransform("Backdrop", transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.one);
        backdrop.offsetMin = Vector2.zero;
        backdrop.offsetMax = Vector2.zero;
        Image backdropImage = backdrop.gameObject.AddComponent<Image>();
        backdropImage.color = backdropColor;

        cardTransform = CreateRectTransform(
            "BlacksmithFunctionCard",
            transform,
            new Vector2(900f, 730f),
            Vector2.zero,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));
        Image cardImage = cardTransform.gameObject.AddComponent<Image>();
        cardImage.color = cardColor;

        Outline outline = cardTransform.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.95f, 0.76f, 0.32f, 0.45f);
        outline.effectDistance = new Vector2(2f, -2f);

        titleText = CreateText(
            "Title",
            cardTransform,
            "Thợ Rèn Hắc Long",
            44,
            FontStyles.Bold,
            titleColor,
            new Vector2(0f, 245f),
            new Vector2(720f, 60f));

        subtitleText = CreateText(
            "Subtitle",
            cardTransform,
            "Chọn chức năng muốn dùng khi nói chuyện với thợ rèn.",
            25,
            FontStyles.Normal,
            bodyColor,
            new Vector2(0f, 185f),
            new Vector2(760f, 72f));

        equipmentUpgradeButton = CreateMenuButton(
            "EquipmentUpgradeButton",
            cardTransform,
            new Vector2(0f, 78f),
            new Color(0.72f, 0.42f, 0.14f, 0.94f),
            "Cường Hóa Trang Bị",
            null);   // listeners được gắn qua WireListeners()

        primaryGeneUpgradeButton = CreateMenuButton(
            "PrimaryGeneUpgradeButton",
            cardTransform,
            new Vector2(0f, -4f),
            new Color(0.58f, 0.24f, 0.1f, 0.94f),
            "Nâng Tier Gene Chính",
            null);

        secondarySelectButton = CreateMenuButton(
            "SecondarySelectButton",
            cardTransform,
            new Vector2(0f, -86f),
            new Color(0.14f, 0.39f, 0.52f, 0.94f),
            "Chọn Hệ Thứ 2",
            null);

        secondaryUpgradeButton = CreateMenuButton(
            "SecondaryUpgradeButton",
            cardTransform,
            new Vector2(0f, -168f),
            new Color(0.15f, 0.46f, 0.28f, 0.94f),
            "Cường Hóa Tier Hệ Thứ 2",
            null);

        hybridFusionButton = CreateMenuButton(
            "HybridFusionButton",
            cardTransform,
            new Vector2(0f, -250f),
            new Color(0.38f, 0.18f, 0.52f, 0.94f),
            "Hợp Nhất Hybrid",
            null);

        statusText = CreateText(
            "Status",
            cardTransform,
            "Mang gene và nguyên liệu tới đây, ta lo phần còn lại.",
            24,
            FontStyles.Normal,
            statusColor,
            new Vector2(0f, -334f),
            new Vector2(780f, 72f));

        closeButton = CreateMenuButton(
            "CloseButton",
            cardTransform,
            new Vector2(395f, 348f),
            new Color(0.26f, 0.28f, 0.34f, 0.96f),
            "X",
            null,
            new Vector2(58f, 58f),
            26);

        UIRuntimeAssetHelper.ApplyNotoSans(cardTransform.GetComponentsInChildren<TMP_Text>(true));

        WireListeners();

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        Debug.Log("[BlacksmithFunctionMenuPanel] UI đã được tạo. Kéo vào Prefabs để lưu.");
#endif
    }

    private void RefreshState()
    {
        PlayerDataResponse playerData = GameManager.Instance?.GetPlayerData();
        if (playerData == null)
        {
            subtitleText.text = "Chưa tải được dữ liệu nhân vật. Vẫn có thể vào cường hóa trang bị.";
            statusText.text = "Mở lại menu sau khi nhân vật đồng bộ xong để dùng các chức năng gene.";
            statusText.color = new Color(1f, 0.74f, 0.42f, 1f);

            SetButtonState(equipmentUpgradeButton, "Cường Hóa Trang Bị", true);
            SetButtonState(primaryGeneUpgradeButton, "Nâng Tier Gene Chính", false);
            SetButtonState(secondarySelectButton, "Chọn Hệ Thứ 2", false);
            SetButtonState(secondaryUpgradeButton, "Cường Hóa Tier Hệ Thứ 2", false);
            SetButtonState(hybridFusionButton, "Hợp Nhất Hybrid", true);  // luôn bật
            return;
        }

        string primaryElementName = ElementHelper.ToVietnamese(playerData.element_type);
        int primaryTier = Mathf.Max(playerData.gene_tier, 1);

        string secondaryElement = playerData.secondary_element;
        bool hasSecondary = !string.IsNullOrEmpty(secondaryElement);
        string secondaryElementName = hasSecondary
            ? ElementHelper.ToVietnamese(secondaryElement)
            : "Chưa mở";
        int secondaryTier = Mathf.Max(playerData.secondary_gene_tier, 1);

        subtitleText.text = hasSecondary
            ? $"Hệ chính: {primaryElementName} Tier {primaryTier}   •   Hệ phụ: {secondaryElementName} Tier {secondaryTier}"
            : $"Hệ chính: {primaryElementName} Tier {primaryTier}   •   Hệ phụ: Chưa mở";

        SetButtonState(equipmentUpgradeButton, "Cường Hóa Trang Bị", true);
        SetButtonState(primaryGeneUpgradeButton, $"Nâng Tier Gene Chính ({primaryElementName})", true);

        bool canHybrid = hasSecondary && primaryTier >= 5 && secondaryTier >= 5;
        SetButtonState(hybridFusionButton, "Hợp Nhất Hybrid", true);  // luôn bật, panel tự hiển thị yêu cầu

        if (hasSecondary)
        {
            SetButtonState(secondarySelectButton, $"Hệ Thứ 2: {secondaryElementName}", false);
            SetButtonState(secondaryUpgradeButton, $"Cường Hóa Tier Hệ Thứ 2 ({secondaryElementName})", true);
            statusText.text = canHybrid
                ? $"Cả hai hệ đã đạt Tier 5. Có thể Hybrid Fusion ngay!"
                : $"Hệ phụ hiện tại là {secondaryElementName}. Có thể tiếp tục nâng tier hệ phụ ngay tại đây.";
            statusText.color = statusColor;
            return;
        }

        string fixedSecondary = ElementHelper.GetFixedSecondary(playerData.element_type);
        if (string.IsNullOrEmpty(fixedSecondary))
        {
            SetButtonState(secondarySelectButton, "Chọn Hệ Thứ 2", false);
            SetButtonState(secondaryUpgradeButton, "Cường Hóa Tier Hệ Thứ 2", false);
            SetButtonState(hybridFusionButton, "Hợp Nhất Hybrid", true);  // luôn bật
            statusText.text = "Hệ chính hiện tại chưa có cặp hệ phụ cố định để mở khóa.";
            statusText.color = new Color(1f, 0.74f, 0.42f, 1f);
            return;
        }

        string fixedSecondaryName = ElementHelper.ToVietnamese(fixedSecondary);
        SetButtonState(secondarySelectButton, $"Chọn Hệ Thứ 2 ({fixedSecondaryName})", true);
        SetButtonState(secondaryUpgradeButton, "Cường Hóa Tier Hệ Thứ 2", false);
        SetButtonState(hybridFusionButton, "Hợp Nhất Hybrid", true);  // luôn bật
        statusText.text = $"Hệ phụ cố định của {primaryElementName} là {fixedSecondaryName}. Chọn hệ thứ 2 trước khi nâng tier hệ phụ.";
        statusText.color = statusColor;
    }

    private void OpenEquipmentUpgrade()
    {
        BlacksmithTabPanel tabPanel = ResolvePanel<BlacksmithTabPanel>();
        if (tabPanel != null)
        {
            Close();
            tabPanel.Open(0);
            return;
        }

        UpgradePanel upgradePanel = ResolvePanel<UpgradePanel>();
        if (upgradePanel != null)
        {
            InventoryNetworkBridge bridge = FindObjectOfType<InventoryNetworkBridge>();
            Close();
            upgradePanel.OpenEmpty(bridge != null ? bridge.CurrentInventory : null);
            return;
        }

        SetTemporaryError("Không tìm thấy panel cường hóa trang bị trong scene.");
    }

    private void OpenPrimaryGeneUpgrade()
    {
        GeneUpgradePanel panel = ResolvePanel<GeneUpgradePanel>();
        if (panel == null)
        {
            SetTemporaryError("Không tìm thấy panel nâng tier gene chính.");
            return;
        }

        Close();
        panel.Open();
    }

    private void OpenSecondarySelect()
    {
        PlayerDataResponse playerData = GameManager.Instance?.GetPlayerData();
        if (playerData == null)
        {
            SetTemporaryError("Dữ liệu nhân vật chưa sẵn sàng để chọn hệ thứ 2.");
            return;
        }

        if (!string.IsNullOrEmpty(playerData.secondary_element))
        {
            SetTemporaryError($"Đã mở hệ thứ 2: {ElementHelper.ToVietnamese(playerData.secondary_element)}.");
            RefreshState();
            return;
        }

        SecondaryGeneSelectPanel panel = ResolvePanel<SecondaryGeneSelectPanel>();
        if (panel == null)
        {
            SetTemporaryError("Không tìm thấy panel chọn hệ thứ 2.");
            return;
        }

        Close();
        panel.Open();
    }

    private void OpenSecondaryUpgrade()
    {
        PlayerDataResponse playerData = GameManager.Instance?.GetPlayerData();
        if (playerData == null || string.IsNullOrEmpty(playerData.secondary_element))
        {
            SetTemporaryError("Cần chọn hệ thứ 2 trước khi nâng tier hệ phụ.");
            RefreshState();
            return;
        }

        GeneUpgradePanel panel = ResolvePanel<GeneUpgradePanel>();
        if (panel == null)
        {
            SetTemporaryError("Không tìm thấy GeneUpgradePanel trong scene.");
            return;
        }

        Close();
        panel.OpenForSecondary();
    }

    private void OpenHybridFusion()
    {
        HybridFusionPanel panel = ResolvePanel<HybridFusionPanel>();
        if (panel == null)
        {
            var prefabGO = Resources.Load<GameObject>("UI/HybridFusionCanvas");
            if (prefabGO != null)
            {
                var go = Object.Instantiate(prefabGO);
                go.name = "HybridFusionCanvas";
                // Đảm bảo scale đúng (prefab root có thể bị scale=0)
                go.transform.localScale = Vector3.one;
                panel = go.GetComponentInChildren<HybridFusionPanel>(true);
            }
        }

        if (panel == null)
        {
            SetTemporaryError("Không tìm thấy HybridFusionCanvas trong Resources/UI.");
            return;
        }

        Close();
        panel.Open();
    }

    private void HideOtherBlacksmithFlows()
    {
        ResolvePanel<BlacksmithTabPanel>()?.Close();

        GeneUpgradePanel geneUpgradePanel = ResolvePanel<GeneUpgradePanel>();
        if (geneUpgradePanel != null)
        {
            geneUpgradePanel.gameObject.SetActive(false);
        }

        SecondaryGeneSelectPanel secondarySelectPanel = ResolvePanel<SecondaryGeneSelectPanel>();
        if (secondarySelectPanel != null)
        {
            secondarySelectPanel.gameObject.SetActive(false);
        }

        SecondaryGeneUpgradePanel secondaryUpgradePanel = ResolvePanel<SecondaryGeneUpgradePanel>();
        if (secondaryUpgradePanel != null)
        {
            GameObject root = secondaryUpgradePanel.transform.root.gameObject;
            root.SetActive(false);
        }

        HybridFusionPanel hybridFusionPanel = ResolvePanel<HybridFusionPanel>();
        if (hybridFusionPanel != null)
        {
            hybridFusionPanel.gameObject.SetActive(false);
        }
    }

    private void SetTemporaryError(string message)
    {
        statusText.text = message;
        statusText.color = new Color(1f, 0.48f, 0.48f, 1f);
    }

    private void SetButtonState(Button button, string label, bool enabled)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text labelText = button.GetComponentInChildren<TMP_Text>(true);
        if (labelText != null)
        {
            labelText.text = label;
        }

        button.interactable = enabled;
    }

    private static T ResolvePanel<T>() where T : Component
    {
        return FindObjectOfType<T>(true);
    }

    private Button CreateMenuButton(
        string objectName,
        RectTransform parent,
        Vector2 anchoredPosition,
        Color baseColor,
        string label,
        UnityAction onClick,
        Vector2? sizeOverride = null,
        int fontSize = 30)
    {
        Vector2 size = sizeOverride ?? new Vector2(650f, 68f);
        RectTransform buttonTransform = CreateRectTransform(
            objectName,
            parent,
            size,
            anchoredPosition,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));

        Image image = buttonTransform.gameObject.AddComponent<Image>();
        image.color = baseColor;

        Button button = buttonTransform.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = baseColor * 1.08f;
        colors.pressedColor = baseColor * 0.9f;
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.28f, 0.3f, 0.34f, 0.85f);
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.targetGraphic = image;
        if (onClick != null) button.onClick.AddListener(onClick);

        TMP_Text labelText = CreateText(
            "Label",
            buttonTransform,
            label,
            fontSize,
            FontStyles.Bold,
            Color.white,
            Vector2.zero,
            size - new Vector2(30f, 0f));
        labelText.alignment = TextAlignmentOptions.Center;

        return button;
    }

    private TMP_Text CreateText(
        string objectName,
        RectTransform parent,
        string content,
        int fontSize,
        FontStyles fontStyle,
        Color color,
        Vector2 anchoredPosition,
        Vector2 size)
    {
        RectTransform textTransform = CreateRectTransform(
            objectName,
            parent,
            size,
            anchoredPosition,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f));

        TextMeshProUGUI text = textTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.richText = true;
        return text;
    }

    private static RectTransform CreateRectTransform(
        string objectName,
        Transform parent,
        Vector2 size,
        Vector2 anchoredPosition,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }
}