using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Overlay loading dùng chung cho login, reconnect và chuyển map.
/// Ưu tiên dùng prefab được gán trong Inspector.
/// Nếu prefab cũ vẫn là dạng progress/panel, script sẽ tự fallback sang spinner runtime.
/// </summary>
public class LoginLoadingManager : MonoBehaviour
{
    public static LoginLoadingManager Instance { get; private set; }

    [Header("Prefab loader dùng chung")]
    [SerializeField] private GameObject loadingPanelPrefab;

    [Header("Legacy / Optional")]
    [Tooltip("Giữ lại để tương thích scene cũ. Luồng loader mới không còn dùng panel lỗi này.")]
    [SerializeField] private GameObject errorPanelPrefab;

    private const string DefaultLoginStatus = "Đang đăng nhập...";
    private const string RuntimeSpinnerResourceFolder = "Loading";

    private GameObject _loadingPanel;
    private LoadingOverlayView _loadingView;
    private Canvas _overlayCanvas;
    private bool _uiReady;
    private Coroutine _activeLoadRoutine;
    private System.Action<string> _loadFailedCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void ShowLoadingStatic(string status = null)
    {
        EnsureInstance();
        Instance.ShowLoading(status);
    }

    public static void UpdateStatusStatic(string status)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.SetStatus(status);
    }

    public static void HideLoadingStatic(bool hideErrorPanel = false)
    {
        if (Instance == null)
        {
            return;
        }

        Instance.HideLoading(hideErrorPanel);
    }

    public static string BuildUserFacingMessage(string rawMessage)
    {
        return BuildErrorMessage(rawMessage);
    }

    public void ShowLoading(string status = null)
    {
        EnsureUI();

        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(true);
            _loadingPanel.transform.SetAsLastSibling();
        }

        SetStatus(status);
    }

    public void HideLoading(bool hideErrorPanel = false)
    {
        if (_loadingPanel != null)
        {
            _loadingPanel.SetActive(false);
        }
    }

    public void BeginLoading(int userId, string targetScene = "GameScene", System.Action<string> onFailed = null)
    {
        EnsureUI();
        _loadFailedCallback = onFailed;

        if (_activeLoadRoutine != null)
        {
            StopCoroutine(_activeLoadRoutine);
        }

        ShowLoading(DefaultLoginStatus);
        _activeLoadRoutine = StartCoroutine(LoadCoroutine(userId, targetScene));
    }

    private IEnumerator LoadCoroutine(int userId, string targetScene)
    {
        APIClient apiClient = APIClient.Instance;
        if (apiClient == null)
        {
            apiClient = new GameObject("APIClient").AddComponent<APIClient>();
        }

        bool done = false;
        bool success = false;
        bool isNewCharacter = false;
        string errorMessage = string.Empty;

        SetStatus("Đang tải dữ liệu nhân vật...");

        apiClient.LoadPlayerData(
            userId,
            onSuccess: data =>
            {
                if (GameManager.Instance == null)
                {
                    new GameObject("GameManager").AddComponent<GameManager>();
                }

                GameManager.Instance?.SetPlayerData(data);
                success = true;
                done = true;
            },
            onError: err =>
            {
                errorMessage = err ?? string.Empty;
                isNewCharacter =
                    errorMessage.Contains("404") ||
                    errorMessage.Contains("Not Found") ||
                    errorMessage.Contains("not found") ||
                    errorMessage.Contains("Player không tồn tại");
                done = true;
            });

        while (!done)
        {
            yield return null;
        }

        _activeLoadRoutine = null;

        if (success)
        {
            ShowLoading("Đang kết nối vào game...");
            yield return null;
            SceneManager.LoadScene(targetScene);
            yield break;
        }

        if (isNewCharacter)
        {
            ShowLoading("Chưa có nhân vật. Đang mở màn chọn nhân vật...");
            yield return new WaitForSecondsRealtime(0.2f);
            SceneManager.LoadScene("SelectElement");
            yield break;
        }

        HideLoading(hideErrorPanel: true);

        string friendlyMessage = BuildErrorMessage(errorMessage);
        if (_loadFailedCallback != null)
        {
            _loadFailedCallback.Invoke(friendlyMessage);
        }
        else
        {
            GlobalNotificationUI.Show(friendlyMessage, "Không thể vào game", 2.5f, "Đóng");
        }
    }

    private void SetStatus(string message)
    {
        if (_loadingView == null)
        {
            return;
        }

        _loadingView.SetStatus(message);
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Login" || scene.name == "SelectElement")
        {
            HideLoading(hideErrorPanel: true);
        }

        if (scene.name == "Login")
        {
            _loadFailedCallback = null;
        }
    }

    private void EnsureUI()
    {
        if (_uiReady && _loadingPanel != null)
        {
            return;
        }

        _uiReady = true;
        Transform canvasRoot = GetOrCreateOverlayCanvasRoot();

        if (_loadingPanel == null)
        {
            _loadingPanel = TryInstantiateConfiguredPrefab(canvasRoot);
        }

        if (_loadingPanel == null)
        {
            _loadingPanel = BuildRuntimeLoadingPanel(canvasRoot);
        }

        if (_loadingPanel != null)
        {
            _loadingPanel.transform.SetAsLastSibling();
            _loadingPanel.SetActive(false);
            _loadingView = _loadingPanel.GetComponent<LoadingOverlayView>() ?? _loadingPanel.AddComponent<LoadingOverlayView>();
            _loadingView.ResolveReferences();
        }
    }

    private GameObject TryInstantiateConfiguredPrefab(Transform canvasRoot)
    {
        if (loadingPanelPrefab == null)
        {
            return null;
        }

        GameObject instance = Instantiate(loadingPanelPrefab, canvasRoot, false);
        StretchRT(instance);

        if (IsLegacyProgressPrefab(instance.transform))
        {
            Destroy(instance);
            return null;
        }

        return instance;
    }

    private Transform GetOrCreateOverlayCanvasRoot()
    {
        if (_overlayCanvas != null)
        {
            return _overlayCanvas.transform;
        }

        Canvas bestOverlay = null;
        Canvas anyRootCanvas = null;
        int highestSortingOrder = 0;

        foreach (Canvas canvas in FindObjectsOfType<Canvas>())
        {
            if (!canvas.isRootCanvas)
            {
                continue;
            }

            if (canvas.sortingOrder > highestSortingOrder)
            {
                highestSortingOrder = canvas.sortingOrder;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                (bestOverlay == null || canvas.sortingOrder > bestOverlay.sortingOrder))
            {
                bestOverlay = canvas;
            }

            if (anyRootCanvas == null)
            {
                anyRootCanvas = canvas;
            }
        }

        GameObject canvasObject = new GameObject("[LoadingOverlayCanvas]");
        canvasObject.transform.SetParent(transform, false);
        SetUiLayer(canvasObject);

        RectTransform rectTransform = canvasObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        _overlayCanvas = canvasObject.AddComponent<Canvas>();
        _overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _overlayCanvas.overrideSorting = true;
        _overlayCanvas.sortingOrder = highestSortingOrder + 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        CanvasScaler sourceScaler = (bestOverlay != null ? bestOverlay : anyRootCanvas) != null
            ? (bestOverlay != null ? bestOverlay : anyRootCanvas).GetComponent<CanvasScaler>()
            : null;

        if (sourceScaler != null)
        {
            scaler.uiScaleMode = sourceScaler.uiScaleMode;
            scaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
            scaler.scaleFactor = sourceScaler.scaleFactor;
            scaler.referenceResolution = sourceScaler.referenceResolution;
            scaler.screenMatchMode = sourceScaler.screenMatchMode;
            scaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
            scaler.physicalUnit = sourceScaler.physicalUnit;
            scaler.fallbackScreenDPI = sourceScaler.fallbackScreenDPI;
            scaler.defaultSpriteDPI = sourceScaler.defaultSpriteDPI;
            scaler.dynamicPixelsPerUnit = sourceScaler.dynamicPixelsPerUnit;
        }
        else
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        canvasObject.AddComponent<GraphicRaycaster>();
        return _overlayCanvas.transform;
    }

    private GameObject BuildRuntimeLoadingPanel(Transform canvasRoot)
    {
        GameObject root = new GameObject("LoadingPanel", typeof(RectTransform));
        root.transform.SetParent(canvasRoot, false);
        SetUiLayer(root);
        StretchRT(root);

        GameObject dim = MakePanel(root.transform, "Dim", new Color(0f, 0f, 0f, 0.55f));
        Stretch(dim);

        GameObject spinnerRoot = new GameObject("SpinnerRoot", typeof(RectTransform));
        spinnerRoot.transform.SetParent(root.transform, false);
        SetUiLayer(spinnerRoot);
        RectTransform spinnerRootRt = spinnerRoot.GetComponent<RectTransform>();
        spinnerRootRt.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRootRt.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRootRt.pivot = new Vector2(0.5f, 0.5f);
        spinnerRootRt.anchoredPosition = new Vector2(0f, 40f);
        spinnerRootRt.sizeDelta = new Vector2(160f, 160f);

        GameObject spinnerImage = new GameObject("SpinnerImage", typeof(RectTransform), typeof(Image), typeof(LoadingSpinnerAnimator));
        spinnerImage.transform.SetParent(spinnerRoot.transform, false);
        SetUiLayer(spinnerImage);
        RectTransform spinnerRt = spinnerImage.GetComponent<RectTransform>();
        spinnerRt.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRt.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRt.pivot = new Vector2(0.5f, 0.5f);
        spinnerRt.anchoredPosition = Vector2.zero;
        spinnerRt.sizeDelta = new Vector2(140f, 140f);

        Image spinner = spinnerImage.GetComponent<Image>();
        Sprite[] frames = Resources.LoadAll<Sprite>(RuntimeSpinnerResourceFolder);
        if (frames != null && frames.Length > 0)
        {
            spinner.sprite = frames[0];
            spinner.preserveAspect = true;
        }
        spinner.color = Color.white;

        GameObject statusGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGo.transform.SetParent(root.transform, false);
        SetUiLayer(statusGo);
        RectTransform statusRt = statusGo.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0.5f, 0.5f);
        statusRt.anchorMax = new Vector2(0.5f, 0.5f);
        statusRt.pivot = new Vector2(0.5f, 0.5f);
        statusRt.anchoredPosition = new Vector2(0f, -70f);
        statusRt.sizeDelta = new Vector2(720f, 70f);

        TextMeshProUGUI statusText = statusGo.GetComponent<TextMeshProUGUI>();
        statusText.text = string.Empty;
        statusText.fontSize = 24f;
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.color = Color.white;
        statusText.enableWordWrapping = true;
        statusText.raycastTarget = false;
        UIRuntimeAssetHelper.ApplyNotoSans(statusText);

        LoadingOverlayView view = root.AddComponent<LoadingOverlayView>();
        view.ResolveReferences();
        view.SetStatus(null);
        return root;
    }

    public static void EnsureInstance()
    {
        if (Instance != null)
        {
            return;
        }

        new GameObject("[LoginLoadingManager]").AddComponent<LoginLoadingManager>();
    }

    private static string BuildErrorMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "Không thể hoàn tất thao tác. Vui lòng thử lại.";
        }

        if (raw.Contains("401") || raw.Contains("Unauthorized"))
        {
            return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
        }

        if (raw.Contains("timeout") ||
            raw.Contains("Cannot connect") ||
            raw.Contains("Unable to connect") ||
            raw.Contains("connection") ||
            raw.Contains("connect"))
        {
            return "Không thể kết nối đến máy chủ. Vui lòng kiểm tra mạng hoặc thử lại sau.";
        }

        return raw;
    }

    private static bool IsLegacyProgressPrefab(Transform root)
    {
        return FindByName(root, "ProgressBarFill") != null ||
               FindByName(root, "PercentText") != null ||
               FindByName(root, "ProgressBarBg") != null;
    }

    private static Transform FindByName(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static GameObject MakePanel(Transform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        SetUiLayer(go);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rectTransform = go.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    private static void StretchRT(GameObject go)
    {
        RectTransform rectTransform = go.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = go.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private static void SetUiLayer(GameObject go)
    {
        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0)
        {
            go.layer = uiLayer;
        }
    }
}
