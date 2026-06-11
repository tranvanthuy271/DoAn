using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Panel thông báo toàn cục — dùng cho mọi luồng (lỗi, cảnh báo, xác nhận đơn giản).
// Gọi từ bất kỳ đâu:
// GlobalNotificationUI.Show("Cần phải có nhóm mới có thể tham gia phó bản này.");
// GlobalNotificationUI.Show("Tin nhắn", "Nhắc nhở", autoHideSeconds: 3f);
// Prefab: Assets/Resources/Prefabs/UI/GlobalNotificationPanel.prefab
// — Tạo tự động bằng menu: Tools ▸ Create Dungeon UI Prefabs
public class GlobalNotificationUI : MonoBehaviour
{
    private const string LogPrefix = "[GlobalNotificationUI]";
    private const string PrefabResourcesPath = "Prefabs/UI/GlobalNotificationPanel";
    private static readonly Vector2 DefaultPanelSize = new(420f, 220f);

    public static GlobalNotificationUI Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text   titleText;
    [SerializeField] private TMP_Text   messageText;
    [SerializeField] private Button     btnOk;
    [SerializeField] private TMP_Text   btnOkLabel;   // text trên nút (mặc định "Xác nhận")

    private Coroutine _autoHideCoroutine;
    private bool _hasBeenShown;

    // Unity lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureCanvasSetup();
        BuildUiIfNeeded();
        UIDraggablePanel.Ensure(gameObject);
        EnsureReferences();
        BindListeners();
        if (panel) panel.SetActive(false);
        if (transform.parent == null)
            DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (Instance == null) Instance = this;
        EnsureReferences();
        BindListeners();
    }

    private void Start()
    {
        EnsureReferences();
        BindListeners();

        // Nếu Show() đã được gọi ngay sau khi Instantiate prefab/runtime instance,
        // Start() không được ẩn panel thêm lần nữa.
        if (!_hasBeenShown && panel)
            panel.SetActive(false);
    }

    // Static API

    // Hiển thị thông báo.
    // Tham số message: Nội dung thông báo.
    // Tham số title: Tiêu đề (mặc định "Nhắc nhở").
    // Tham số autoHideSeconds: 0 = chỉ ẩn khi nhấn Xác nhận.
    // Tham số confirmLabel: Nhãn nút xác nhận (mặc định "Xác nhận").
    public static void Show(string message, string title = "Nhắc nhở",
                            float autoHideSeconds = 0f, string confirmLabel = "Xác nhận")
    {
        var inst = GetOrFind();
        if (inst == null)
        {
            { /* Cảnh báo: Không tìm thấy instance trong scene */ }
            return;
        }
        inst.InternalShow(message, title, autoHideSeconds, confirmLabel);
    }

    // Lazy singleton — tìm trong scene kể cả inactive.
    public static GlobalNotificationUI GetOrFind()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<GlobalNotificationUI>(true);
        if (Instance != null) return Instance;

        GameObject prefab = Resources.Load<GameObject>(PrefabResourcesPath);
        if (prefab != null)
        {
            GameObject runtimeRoot = CreateRuntimeCanvasRoot("GlobalNotificationCanvasFromPrefab");
            DontDestroyOnLoad(runtimeRoot);

            GameObject prefabInstance = Instantiate(prefab, runtimeRoot.transform, false);
            prefabInstance.name = "GlobalNotificationPanel_FromResourcesPrefab";

            Instance = prefabInstance.GetComponent<GlobalNotificationUI>();
            if (Instance != null)
            {
                { /* {LogPrefix} Using prefab from Resources/{PrefabResourcesPath} */ }
                return Instance;
            }

            { /* Cảnh báo: {LogPrefix} Prefab Resources/{PrefabResourcesPath} không có GlobalNotificationUI component. Dùng fallback runtime UI */ }
            Destroy(runtimeRoot);
        }

        GameObject root = CreateRuntimeCanvasRoot("GlobalNotificationFallbackCanvas");

        Instance = root.AddComponent<GlobalNotificationUI>();
        return Instance;
    }

    // Xử lý nội bộ phục vụ các hàm public.

    private void InternalShow(string message, string title, float autoHideSeconds, string confirmLabel)
    {
        EnsureCanvasSetup();
        BuildUiIfNeeded();
        EnsureReferences();
        BindListeners();

        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (panel) panel.SetActive(true);
        _hasBeenShown = true;

        if (titleText)   titleText.text   = title   ?? "Nhắc nhở";
        if (messageText) messageText.text = message ?? string.Empty;
        if (btnOkLabel)  btnOkLabel.text  = !string.IsNullOrEmpty(confirmLabel) ? confirmLabel : "Xác nhận";

        { /* {LogPrefix} Show | title='{title}' autoHide={autoHideSeconds:F1}s message='{message}' */ }

        if (_autoHideCoroutine != null) StopCoroutine(_autoHideCoroutine);
        if (autoHideSeconds > 0f)
            _autoHideCoroutine = StartCoroutine(AutoHideRoutine(autoHideSeconds));
    }

    private IEnumerator AutoHideRoutine(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        Hide();
    }

    public void Hide()
    {
        if (_autoHideCoroutine != null)
        {
            StopCoroutine(_autoHideCoroutine);
            _autoHideCoroutine = null;
        }
        if (panel) panel.SetActive(false);
    }

    private void BindListeners()
    {
        if (btnOk == null)
            return;

        btnOk.onClick.RemoveListener(Hide);
        btnOk.onClick.AddListener(Hide);
    }

    private void EnsureCanvasSetup()
    {
        Canvas parentCanvas = transform.parent != null ? transform.parent.GetComponentInParent<Canvas>() : null;
        if (parentCanvas != null)
            return;

        var canvas = GetComponent<Canvas>();
        if (canvas == null)
            return;

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        var scaler = GetComponent<CanvasScaler>() ?? gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        if (GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();
    }

    private void BuildUiIfNeeded()
    {
        if (transform.Find("Panel") != null)
            return;

        GameObject panelGO = new("Panel", typeof(RectTransform), typeof(Image), typeof(Outline));
        panelGO.transform.SetParent(transform, false);
        CenterRect((RectTransform)panelGO.transform, DefaultPanelSize);
        panelGO.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.05f, 0.95f);
        var panelOutline = panelGO.GetComponent<Outline>();
        panelOutline.effectColor = new Color(1f, 0.82f, 0.18f, 1f);
        panelOutline.effectDistance = new Vector2(3f, -3f);

        GameObject titleBarGO = new("TitleBar", typeof(RectTransform), typeof(Image));
        titleBarGO.transform.SetParent(panelGO.transform, false);
        AnchorRect((RectTransform)titleBarGO.transform, 0f, 0.78f, 1f, 1f);
        titleBarGO.GetComponent<Image>().color = new Color(0.35f, 0.23f, 0.06f, 1f);

        GameObject titleGO = new("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleGO.transform.SetParent(titleBarGO.transform, false);
        StretchFill((RectTransform)titleGO.transform);
        var title = titleGO.GetComponent<TextMeshProUGUI>();
        title.text = "Nhắc nhở";
        title.fontSize = 20f;
        title.fontStyle = FontStyles.Bold;
        title.color = new Color(1f, 0.88f, 0.33f, 1f);
        title.alignment = TextAlignmentOptions.Center;
        title.raycastTarget = false;

        GameObject bodyGO = new("BodyArea", typeof(RectTransform), typeof(Image), typeof(Outline));
        bodyGO.transform.SetParent(panelGO.transform, false);
        AnchorRect((RectTransform)bodyGO.transform, 0.04f, 0.22f, 0.96f, 0.77f);
        bodyGO.GetComponent<Image>().color = new Color(0.22f, 0.18f, 0.10f, 0.96f);
        var bodyOutline = bodyGO.GetComponent<Outline>();
        bodyOutline.effectColor = new Color(0.60f, 0.45f, 0.05f, 1f);
        bodyOutline.effectDistance = new Vector2(2f, -2f);

        GameObject messageGO = new("MessageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        messageGO.transform.SetParent(bodyGO.transform, false);
        StretchFill((RectTransform)messageGO.transform);
        var message = messageGO.GetComponent<TextMeshProUGUI>();
        message.text = "Nội dung thông báo.";
        message.fontSize = 16f;
        message.color = Color.white;
        message.alignment = TextAlignmentOptions.Center;
        message.enableWordWrapping = true;
        message.margin = new Vector4(12f, 8f, 12f, 8f);
        message.raycastTarget = false;

        GameObject buttonGO = new("BtnOk", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        buttonGO.transform.SetParent(panelGO.transform, false);
        AnchorRect((RectTransform)buttonGO.transform, 0.22f, 0.04f, 0.78f, 0.19f);
        buttonGO.GetComponent<Image>().color = new Color(0.82f, 0.41f, 0.08f, 1f);
        var buttonOutline = buttonGO.GetComponent<Outline>();
        buttonOutline.effectColor = new Color(1f, 0.75f, 0.20f, 1f);
        buttonOutline.effectDistance = new Vector2(2f, -2f);

        var button = buttonGO.GetComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.82f, 0.41f, 0.08f, 1f);
        colors.highlightedColor = new Color(0.92f, 0.52f, 0.10f, 1f);
        colors.pressedColor = new Color(0.65f, 0.30f, 0.02f, 1f);
        button.colors = colors;

        GameObject buttonTextGO = new("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        StretchFill((RectTransform)buttonTextGO.transform);
        var buttonText = buttonTextGO.GetComponent<TextMeshProUGUI>();
        buttonText.text = "Xác nhận";
        buttonText.fontSize = 17f;
        buttonText.fontStyle = FontStyles.Bold;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.raycastTarget = false;

        UIRuntimeAssetHelper.ApplyNotoSans(title, message, buttonText);
        { /* {LogPrefix} Built runtime fallback UI */ }
    }

    private void EnsureReferences()
    {
        if (panel == null)
            panel = FindChild("Panel");

        if (titleText == null)
            titleText = FindChildComponent<TMP_Text>("TitleText");

        if (messageText == null)
            messageText = FindChildComponent<TMP_Text>("MessageText");

        if (btnOk == null)
            btnOk = FindChildComponent<Button>("BtnOk");

        if (btnOkLabel == null && btnOk != null)
            btnOkLabel = btnOk.GetComponentInChildren<TMP_Text>(true);

        if (panel != null)
            UIRuntimeAssetHelper.ApplyNotoSans(panel.GetComponentsInChildren<TMP_Text>(true));
    }

    private GameObject FindChild(string childName)
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child.GetComponent<T>();
        }

        return null;
    }

    private static void StretchFill(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void CenterRect(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
    }

    private static void AnchorRect(RectTransform rect, float minX, float minY, float maxX, float maxY)
    {
        rect.anchorMin = new Vector2(minX, minY);
        rect.anchorMax = new Vector2(maxX, maxY);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static GameObject CreateRuntimeCanvasRoot(string rootName)
    {
        GameObject root = new(
            rootName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        var rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;

        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return root;
    }
}
