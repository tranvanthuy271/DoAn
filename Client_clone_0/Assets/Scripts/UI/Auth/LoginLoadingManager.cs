using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

/// <summary>
/// Quản lý Loading Panel và Error Notify Panel sau khi đăng nhập thành công.
///
/// Cách dùng (được gọi tự động bởi LoginController):
///   LoginLoadingManager.Instance.BeginLoading(userId);
///
/// Prefab setup (tuỳ chọn — nếu không assign sẽ tự build):
///  1. Chạy Tools ▸ DoAn ▸ Create Login UI Prefabs một lần trong Editor
///  2. Kéo LoadingPanel.prefab  → loadingPanelPrefab
///     Kéo ErrorNotifyPanel.prefab → errorPanelPrefab
///  3. Thay sprite Image bằng art thật của game trong Prefab
///
/// Progress animation: 0 → giả lập đến ~80% trong lúc chờ API → 100% khi xong.
/// </summary>
public class LoginLoadingManager : MonoBehaviour
{
    public static LoginLoadingManager Instance { get; private set; }

    // ── Inspector / Prefab refs ───────────────────────────────────────────
    [Header("Prefab Instances (kéo prefab vào sau khi chạy Tools ▸ DoAn ▸ Create Login UI Prefabs)")]
    [SerializeField] private GameObject loadingPanelPrefab;
    [SerializeField] private GameObject errorPanelPrefab;

    // ── Runtime resolved children ─────────────────────────────────────────
    // Loading panel
    private GameObject _loadingPanel;
    private TMP_Text   _statusText;
    private Image      _progressFill;   // Image.fillAmount  0→1
    private TMP_Text   _percentText;    // "0%"→"100%"

    // Error panel
    private GameObject _errorPanel;
    private TMP_Text   _errorDetailText;
    private Button     _confirmBtn;     // "Xác nhận"
    private Button     _closeBtn;       // X
    private Canvas     _overlayCanvas;

    // ── Internal state  ───────────────────────────────────────────────────
    private System.Action _retryAction;
    private bool          _uiReady;
    private Coroutine     _activeLoadRoutine;

    // Fake-progress speed (units/sec) while API is loading
    private const float FakeProgressSpeed  = 0.25f;   // approaches 80% slowly
    private const float FakeProgressTarget = 0.80f;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (Instance == this)
            Instance = null;
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Hiện ErrorNotifyPanel ngay lập tức (ví dụ: login REST API thất bại).
    /// onDismiss được gọi khi người chơi bấm "Xác nhận" (nullable).
    /// </summary>
    public void ShowError(string rawMessage, System.Action onDismiss = null)
    {
        EnsureUI();
        _retryAction = onDismiss;
        WireErrorPanelButtons();
        if (_loadingPanel != null) _loadingPanel.SetActive(false);
        ShowErrorPanel(BuildErrorMessage(rawMessage));
    }

    /// <summary>
    /// Tĩnh — tự tạo Instance nếu chưa có rồi gọi ShowError.
    /// </summary>
    public static void ShowErrorStatic(string rawMessage, System.Action onDismiss = null)
    {
        if (Instance == null)
            new GameObject("[LoginLoadingManager]").AddComponent<LoginLoadingManager>();
        Instance.ShowError(rawMessage, onDismiss);
    }

    public void HideLoading(bool hideErrorPanel = false)
    {
        if (_loadingPanel != null)
            _loadingPanel.SetActive(false);

        if (hideErrorPanel && _errorPanel != null)
            _errorPanel.SetActive(false);
    }

    public static void HideLoadingStatic(bool hideErrorPanel = false)
    {
        if (Instance != null)
            Instance.HideLoading(hideErrorPanel);
    }

    /// <summary>
    /// Hiện loading panel, load player data, rồi:
    ///  - Thành công → load targetScene (mặc định GameScene)
    ///  - 404 nhân vật chưa tạo → load SelectElement
    ///  - Lỗi khác → hiện ErrorNotifyPanel
    /// </summary>
    public void BeginLoading(int userId, string targetScene = "GameScene")
    {
        EnsureUI();
        _retryAction = () => BeginLoading(userId, targetScene);
        WireErrorPanelButtons();

        if (_activeLoadRoutine != null)
            StopCoroutine(_activeLoadRoutine);

        // Reset progress
        SetProgress(0f);
        SetStatus("Đang kết nối đến máy chủ...");

        if (_errorPanel != null)  _errorPanel.SetActive(false);
        if (_loadingPanel != null) _loadingPanel.SetActive(true);

        _activeLoadRoutine = StartCoroutine(LoadCoroutine(userId, targetScene));
    }

    // ── Coroutine ─────────────────────────────────────────────────────────

    private IEnumerator LoadCoroutine(int userId, string targetScene)
    {
        var apiClient = APIClient.Instance;
        if (apiClient == null)
            apiClient = new GameObject("APIClient").AddComponent<APIClient>();

        bool   done           = false;
        bool   success        = false;
        bool   isNewCharacter = false;
        string errorMsg       = "";

        // Fire the actual API call
        apiClient.LoadPlayerData(
            userId,
            onSuccess: data =>
            {
                if (GameManager.Instance == null)
                    new GameObject("GameManager").AddComponent<GameManager>();
                GameManager.Instance?.SetPlayerData(data);
                success = true;
                done    = true;
            },
            onError: err =>
            {
                errorMsg = err ?? "";
                isNewCharacter =
                    errorMsg.Contains("404")             ||
                    errorMsg.Contains("Not Found")       ||
                    errorMsg.Contains("not found")       ||
                    errorMsg.Contains("Player không tồn tại");
                done = true;
            }
        );

        // Run fake progress (0 → 80%) while waiting for API
        float fakeProgress = 0f;
        while (!done)
        {
            fakeProgress = Mathf.MoveTowards(fakeProgress, FakeProgressTarget,
                                             FakeProgressSpeed * Time.deltaTime);
            SetProgress(fakeProgress);
            yield return null;
        }

        if (success)
        {
            // Animate 80 → 100%
            yield return AnimateTo(fakeProgress, 1f, 0.35f, "Thành công! Đang chuyển màn...");
            yield return new WaitForSeconds(0.2f);
            SetStatus("Đang kết nối vào game...");
            SceneManager.LoadScene(targetScene);
        }
        else if (isNewCharacter)
        {
            yield return AnimateTo(fakeProgress, 1f, 0.25f, "Chưa có nhân vật. Đang mở màn chọn nhân vật...");
            yield return new WaitForSeconds(0.25f);
            HideLoading(hideErrorPanel: true);
            SceneManager.LoadScene("SelectElement");
        }
        else
        {
            // Stop at current fake progress (show as failed)
            SetStatus("Kết nối thất bại.");
            yield return new WaitForSeconds(0.3f);
            ShowErrorPanel(BuildErrorMessage(errorMsg));
        }

        _activeLoadRoutine = null;
    }

    private IEnumerator AnimateTo(float from, float to, float duration, string status)
    {
        SetStatus(status);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            SetProgress(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetProgress(to);
    }

    private static string BuildErrorMessage(string raw)
    {
        if (raw.Contains("401") || raw.Contains("Unauthorized"))
            return "Phiên đăng nhập hết hạn.\nVui lòng đăng nhập lại.";
        if (raw.Contains("timeout") || raw.Contains("Cannot connect") || raw.Contains("Unable to connect"))
            return "Không thể kết nối đến máy chủ.\nĐường truyền Internet có vấn đề hoặc\nmáy chủ đang bảo trì.";
        return string.IsNullOrEmpty(raw) ? "Lỗi không xác định." : $"Lỗi: {raw}";
    }

    // ── UI helpers ────────────────────────────────────────────────────────

    private void SetProgress(float t)
    {
        if (_progressFill != null) _progressFill.fillAmount = t;
        if (_percentText  != null) _percentText.text = Mathf.RoundToInt(t * 100f) + "%";
    }

    private void SetStatus(string msg)
    {
        if (_statusText != null) _statusText.text = msg;
    }

    private void ShowErrorPanel(string msg)
    {
        if (_loadingPanel != null) _loadingPanel.SetActive(false);
        if (_errorDetailText != null) _errorDetailText.text = msg;
        if (_errorPanel != null) _errorPanel.SetActive(true);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Login" || scene.name == "SelectElement")
            HideLoading(hideErrorPanel: true);
    }

    private void WireErrorPanelButtons()
    {
        if (_confirmBtn != null)
        {
            _confirmBtn.onClick.RemoveAllListeners();
            _confirmBtn.onClick.AddListener(() =>
            {
                if (_errorPanel != null) _errorPanel.SetActive(false);
                _retryAction?.Invoke();
            });
        }
        if (_closeBtn != null)
        {
            _closeBtn.onClick.RemoveAllListeners();
            _closeBtn.onClick.AddListener(() => SceneManager.LoadScene("Login"));
        }
    }

    // ── Runtime UI construction ───────────────────────────────────────────

    private void EnsureUI()
    {
        if (_uiReady) return;
        _uiReady = true;

        // Tạo một overlay canvas riêng có sortingOrder cao hơn scene để panel luôn nổi lên trên cùng.
        Transform canvasRoot = GetOrCreateOverlayCanvasRoot();

        // ── Instantiate from prefab OR build fallback ──────────────────
        if (loadingPanelPrefab != null)
        {
            _loadingPanel = Instantiate(loadingPanelPrefab, canvasRoot, false);
            StretchRT(_loadingPanel);
            ResolveLoadingPanelChildren(_loadingPanel.transform);
        }
        else
        {
            _loadingPanel = BuildLoadingPanel(canvasRoot);
        }

        if (errorPanelPrefab != null)
        {
            _errorPanel = Instantiate(errorPanelPrefab, canvasRoot, false);
            StretchRT(_errorPanel);
            ResolveErrorPanelChildren(_errorPanel.transform);
        }
        else
        {
            _errorPanel = BuildErrorPanel(canvasRoot);
        }

        // Đặt cuối cùng để render trên hết các UI khác trong cùng Canvas
        _loadingPanel.transform.SetAsLastSibling();
        _errorPanel.transform.SetAsLastSibling();

        _loadingPanel.SetActive(false);
        _errorPanel.SetActive(false);
    }

    /// <summary>
    /// Tạo canvas overlay riêng cho loading/error popup.
    /// Canvas này luôn nằm trên tất cả root canvas trong scene để tránh bị HUD hoặc panel khác che mất.
    /// </summary>
    private Transform GetOrCreateOverlayCanvasRoot()
    {
        if (_overlayCanvas != null)
            return _overlayCanvas.transform;

        Canvas bestOverlay = null;
        Canvas anyRoot = null;
        int highestSortingOrder = 0;

        foreach (var c in FindObjectsOfType<Canvas>())
        {
            if (!c.isRootCanvas) continue;

            if (c.sortingOrder > highestSortingOrder)
                highestSortingOrder = c.sortingOrder;

            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                if (bestOverlay == null || c.sortingOrder > bestOverlay.sortingOrder)
                    bestOverlay = c;
            }

            if (anyRoot == null)
                anyRoot = c;
        }

        var cgo = new GameObject("[LoginOverlayCanvas]");
        cgo.transform.SetParent(transform, false);
        cgo.layer = LayerMask.NameToLayer("UI");

        var rt = cgo.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        _overlayCanvas = cgo.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.overrideSorting = true;
        _overlayCanvas.sortingOrder = highestSortingOrder + 500;
        _overlayCanvas.pixelPerfect = false;

        var scaler = cgo.AddComponent<CanvasScaler>();
        var templateScaler = (bestOverlay != null ? bestOverlay : anyRoot) != null
            ? (bestOverlay != null ? bestOverlay : anyRoot).GetComponent<CanvasScaler>()
            : null;

        if (templateScaler != null)
        {
            scaler.uiScaleMode = templateScaler.uiScaleMode;
            scaler.referencePixelsPerUnit = templateScaler.referencePixelsPerUnit;
            scaler.scaleFactor = templateScaler.scaleFactor;
            scaler.referenceResolution = templateScaler.referenceResolution;
            scaler.screenMatchMode = templateScaler.screenMatchMode;
            scaler.matchWidthOrHeight = templateScaler.matchWidthOrHeight;
            scaler.physicalUnit = templateScaler.physicalUnit;
            scaler.fallbackScreenDPI = templateScaler.fallbackScreenDPI;
            scaler.defaultSpriteDPI = templateScaler.defaultSpriteDPI;
            scaler.dynamicPixelsPerUnit = templateScaler.dynamicPixelsPerUnit;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        cgo.AddComponent<GraphicRaycaster>();
        return _overlayCanvas.transform;
    }

    // Walk prefab hierarchy and resolve named children
    private void ResolveLoadingPanelChildren(Transform root)
    {
        _statusText   = FindTmpByName(root, "StatusText");
        _percentText  = FindTmpByName(root, "PercentText");
        var fillGO    = FindByName(root, "ProgressBarFill");
        if (fillGO != null) _progressFill = fillGO.GetComponent<Image>();
    }

    private void ResolveErrorPanelChildren(Transform root)
    {
        _errorDetailText = FindTmpByName(root, "MessageText");
        var confirmGO  = FindByName(root, "ConfirmButton");
        if (confirmGO != null) _confirmBtn = confirmGO.GetComponent<Button>();
        var closeGO    = FindByName(root, "CloseButton");
        if (closeGO != null) _closeBtn = closeGO.GetComponent<Button>();
    }

    // ── Fallback builders (used when prefab is not assigned) ──────────────
    //  Colors match LoginPanelPrefabCreator to stay consistent.

    private static readonly Color C_Overlay     = new Color(0f,     0f,    0f,    0.65f);
    private static readonly Color C_DialogBg    = new Color(0.42f,  0.24f, 0.10f);
    private static readonly Color C_HeaderBg    = new Color(0.30f,  0.16f, 0.06f);
    private static readonly Color C_InnerBg     = new Color(0.23f,  0.13f, 0.05f);
    private static readonly Color C_ButtonBg    = new Color(0.85f,  0.50f, 0.08f);
    private static readonly Color C_CloseBg     = new Color(0.65f,  0.10f, 0.10f);
    private static readonly Color C_Title       = new Color(1.00f,  0.90f, 0.30f);
    private static readonly Color C_FillBar     = new Color(0.95f,  0.78f, 0.18f);
    private static readonly Color C_ProgressBg  = new Color(0.15f,  0.08f, 0.03f);

    private GameObject BuildLoadingPanel(Transform canvasRoot)
    {
        var root = MakePanel(canvasRoot, "LoadingPanel", C_Overlay); Stretch(root);

        var dialog = MakePanel(root.transform, "DialogBox", C_DialogBg);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.sizeDelta = new Vector2(520, 300);
        drt.anchoredPosition = Vector2.zero;

        // Header
        var header = MakePanel(dialog.transform, "HeaderBg", C_HeaderBg);
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = Vector2.one;
        hrt.pivot = new Vector2(0.5f, 1f); hrt.sizeDelta = new Vector2(0, 56);
        hrt.anchoredPosition = Vector2.zero;
        MakeTmp(header.transform, "TitleText", "Đang tải", C_Title, 26, FontStyles.Bold, stretch: true);

        // Inner box
        var inner = MakePanel(dialog.transform, "ContentArea", C_InnerBg);
        var irt = inner.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.04f, 0.10f); irt.anchorMax = new Vector2(0.96f, 0.76f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;

        _statusText = MakeTmp(inner.transform, "StatusText", "Đang kết nối...", Color.white, 17)
                          .GetComponent<TMP_Text>();
        var srt = _statusText.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(0.02f, 0.52f); srt.anchorMax = new Vector2(0.98f, 0.98f);
        srt.offsetMin = srt.offsetMax = Vector2.zero;

        // Progress bg + fill
        var pBg = MakePanel(inner.transform, "ProgressBarBg", C_ProgressBg);
        var prt = pBg.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.02f, 0.10f); prt.anchorMax = new Vector2(0.98f, 0.44f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;

        var pfGO = MakePanel(pBg.transform, "ProgressBarFill", C_FillBar);
        _progressFill = pfGO.GetComponent<Image>();
        _progressFill.type         = Image.Type.Filled;
        _progressFill.fillMethod   = Image.FillMethod.Horizontal;
        _progressFill.fillOrigin   = (int)Image.OriginHorizontal.Left;
        _progressFill.fillAmount   = 0f;
        Stretch(pfGO);

        _percentText = MakeTmp(pBg.transform, "PercentText", "0%", Color.white, 14, FontStyles.Bold, stretch: true)
                           .GetComponent<TMP_Text>();

        return root;
    }

    private GameObject BuildErrorPanel(Transform canvasRoot)
    {
        var root = MakePanel(canvasRoot, "ErrorNotifyPanel", C_Overlay); Stretch(root);

        var dialog = MakePanel(root.transform, "DialogBox", C_DialogBg);
        var drt = dialog.GetComponent<RectTransform>();
        drt.anchorMin = drt.anchorMax = new Vector2(0.5f, 0.5f);
        drt.sizeDelta = new Vector2(480, 290);
        drt.anchoredPosition = Vector2.zero;

        // Close X button (top-right)
        var closeGO = MakePanel(dialog.transform, "CloseButton", C_CloseBg);
        _closeBtn = closeGO.AddComponent<Button>();
        var crt = closeGO.GetComponent<RectTransform>();
        crt.anchorMin = crt.anchorMax = Vector2.one; crt.pivot = Vector2.one;
        crt.sizeDelta = new Vector2(40, 40); crt.anchoredPosition = new Vector2(6, 6);
        MakeTmp(closeGO.transform, "X", "✕", Color.white, 18, stretch: true);

        // Header
        var header = MakePanel(dialog.transform, "HeaderBg", C_HeaderBg);
        var hrt = header.GetComponent<RectTransform>();
        hrt.anchorMin = new Vector2(0, 1); hrt.anchorMax = Vector2.one;
        hrt.pivot = new Vector2(0.5f, 1f); hrt.sizeDelta = new Vector2(0, 54);
        hrt.anchoredPosition = Vector2.zero;
        MakeTmp(header.transform, "TitleText", "Nhắc nhở", C_Title, 26, FontStyles.Bold, stretch: true);

        // Inner box
        var inner = MakePanel(dialog.transform, "ContentArea", C_InnerBg);
        var irt = inner.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0.06f, 0.24f); irt.anchorMax = new Vector2(0.94f, 0.76f);
        irt.offsetMin = irt.offsetMax = Vector2.zero;

        var msgGO = MakeTmp(inner.transform, "MessageText", "", Color.white, 17, stretch: true);
        var msgTmp = msgGO.GetComponent<TextMeshProUGUI>();
        msgTmp.enableWordWrapping = true;
        msgTmp.alignment = TextAlignmentOptions.Center;
        _errorDetailText = msgTmp;

        // Confirm button
        var btnGO = MakePanel(dialog.transform, "ConfirmButton", C_ButtonBg);
        _confirmBtn = btnGO.AddComponent<Button>();
        var brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f); brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(180, 46); brt.anchoredPosition = new Vector2(0, 14);
        MakeTmp(btnGO.transform, "Label", "Xác nhận", C_Title, 20, FontStyles.Bold, stretch: true);

        return root;
    }

    // ── Generic UI builder helpers ────────────────────────────────────────

    private static GameObject MakePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        go.layer = LayerMask.NameToLayer("UI");
        return go;
    }

    private static GameObject MakeTmp(Transform parent, string name, string text, Color color,
        float fontSize, FontStyles style = FontStyles.Normal, bool stretch = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.color     = color;
        t.fontSize  = fontSize;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        go.layer = LayerMask.NameToLayer("UI");
        if (stretch) Stretch(go);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    /// <summary>Force a prefab root that was just Instantiate()'d into a Canvas to fill its parent.</summary>
    private static void StretchRT(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = Vector2.zero;
    }

    // ── Prefab hierarchy search ───────────────────────────────────────────

    private static Transform FindByName(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            var found = FindByName(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static TMP_Text FindTmpByName(Transform root, string name)
    {
        var t = FindByName(root, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }
}

