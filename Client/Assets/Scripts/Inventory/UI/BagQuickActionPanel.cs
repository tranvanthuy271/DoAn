using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// BagQuickActionPanel — Panel nhanh khi click vào BagQuickSlot trên HUD.
/// Có thể dùng theo 2 cách:
///   1. Runtime (ItemUseHandler): gọi BagQuickActionPanel.Create(parent) → tự xây UI.
///   2. Prefab: Instantiate prefab → gán các reference trong Inspector → gọi Show(..., slotRect).
/// </summary>
public class BagQuickActionPanel : MonoBehaviour
{
    private const string OverlayCanvasName = "[BagQuickActionOverlayCanvas]";
    private const int OverlaySortingOrder = 500;
    // ── Prefab-mode references (gán trong Inspector trên prefab) ──────────────
    [Header("Prefab References (leave null if using Create())")]
    [SerializeField] private Button overlayButton;
    [SerializeField] private RectTransform dialogRect;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button detachButton;
    [SerializeField] private Button viewButton;

    // ── Runtime-build references (dùng khi Create() ────────────────────────
    private Image _overlayImage;
    private Button _overlayButtonRuntime;
    private RectTransform _dialogRectRuntime;
    private TMP_Text _titleTextRuntime;
    private Button _detachButtonRuntime;
    private TMP_Text _detachLabel;
    private Button _viewButtonRuntime;
    private TMP_Text _viewLabel;
    private bool _builtRuntime;
    private int _lastShownFrame = -1;

    private Action _onDetach;
    private Action _onView;

    // ── Properties that resolve whichever mode is active ────────────────────
    private RectTransform DialogRect      => dialogRect != null ? dialogRect      : _dialogRectRuntime;
    private TMP_Text      TitleText       => titleText  != null ? titleText       : _titleTextRuntime;
    private Button        DetachBtn       => detachButton != null ? detachButton  : _detachButtonRuntime;
    private Button        ViewBtn         => viewButton   != null ? viewButton    : _viewButtonRuntime;

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        // Prefab mode: wire overlay close button
        if (overlayButton != null)
            overlayButton.onClick.AddListener(HandleOverlayClick);

        if (dialogRect != null)
            UIDraggablePanel.Ensure(dialogRect.gameObject);

        gameObject.SetActive(false);
    }

    // ── Public API ──────────────────────────────────────────────────────────

    /// <summary>
    /// <summary>
    /// Hiển thị panel tại vị trí screenPos (vị trí chuột khi click).
    /// Nếu không truyền screenPos, hiển thị giữa màn hình.
    /// </summary>
    public void Show(string itemName, Action detachAction, Action viewAction, Vector2? screenPos = null)
    {
        // Đảm bảo panel nằm dưới root Canvas (không bị ẩn theo cha)
        EnsureUnderRootCanvas();

        if (!_builtRuntime && overlayButton == null)
            BuildRuntime();

        _onDetach = detachAction;
        _onView   = viewAction;

        if (TitleText != null)
            TitleText.text = string.IsNullOrEmpty(itemName) ? "Vật phẩm" : itemName;

        if (DetachBtn != null)
        {
            DetachBtn.onClick.RemoveAllListeners();
            DetachBtn.onClick.AddListener(() => { _onDetach?.Invoke(); Hide(); });
        }

        if (ViewBtn != null)
        {
            ViewBtn.onClick.RemoveAllListeners();
            ViewBtn.onClick.AddListener(() => { _onView?.Invoke(); Hide(); });
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        _lastShownFrame = Time.frameCount;

        // Định vị dialog tại vị trí click
        if (DialogRect != null)
        {
            Canvas.ForceUpdateCanvases();
            if (screenPos.HasValue)
                PositionAtScreenPoint(screenPos.Value);

            UIDraggablePanel.ClampToRootCanvas(DialogRect);
        }
    }

    /// <summary>
    /// Nếu panel đang nằm trong 1 parent bị inactive (hoặc trong InventoryPanel đang ẩn),
    /// tự động chuyển nó ra root Canvas để SetActive(true) hoạt động được.
    /// </summary>
    private void EnsureUnderRootCanvas()
    {
        Canvas bestCanvas = ResolveBestRootCanvas();
        if (bestCanvas == null)
            bestCanvas = CreateOverlayCanvas();

        if (transform.parent != bestCanvas.transform)
        {
            transform.SetParent(bestCanvas.transform, false);
            transform.SetAsLastSibling();
            Debug.Log($"[BagQuickActionPanel] Đã chuyển parent → '{bestCanvas.name}' để tránh lệch canvas/inactive parent.");
        }
    }

    private static Canvas ResolveBestRootCanvas()
    {
        Canvas bestCanvas = null;
        int bestOrder = int.MinValue;
        foreach (var c in FindObjectsOfType<Canvas>(true))
        {
            if (!c.isRootCanvas) continue;
            if (c.renderMode == RenderMode.WorldSpace) continue;
            if (!c.gameObject.activeInHierarchy) continue;
            if (c.sortingOrder > bestOrder) { bestOrder = c.sortingOrder; bestCanvas = c; }
        }

        return bestCanvas;
    }

    private static Canvas CreateOverlayCanvas()
    {
        GameObject existing = GameObject.Find(OverlayCanvasName);
        if (existing != null && existing.TryGetComponent(out Canvas existingCanvas))
            return existingCanvas;

        GameObject canvasGo = new GameObject(
            OverlayCanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        DontDestroyOnLoad(canvasGo);

        RectTransform rectTransform = canvasGo.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Canvas canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }


    public void Hide()
    {
        _onDetach = null;
        _onView   = null;
        _lastShownFrame = -1;
        gameObject.SetActive(false);
    }

    private void HandleOverlayClick()
    {
        if (Time.frameCount <= _lastShownFrame)
            return;

        Hide();
    }

    // ── Positioning ─────────────────────────────────────────────────────────

    /// <summary>
    /// Đặt dialogRect tại vị trí screenPos (tọa độ màn hình từ Input.mousePosition), clamp trong Canvas.
    /// </summary>
    private void PositionAtScreenPoint(Vector2 screenPos)
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null) return;
        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();

        // Chuyển screen position → local position trong Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, cam, out Vector2 localPoint);

        // Pivot top-left của dialog = điểm click
        DialogRect.pivot       = new Vector2(0f, 1f);
        DialogRect.anchorMin   = new Vector2(0.5f, 0.5f);
        DialogRect.anchorMax   = new Vector2(0.5f, 0.5f);
        DialogRect.anchoredPosition = localPoint;

        ClampToParent();
    }

    private void ClampToParent()
    {
        if (DialogRect == null) return;
        Canvas.ForceUpdateCanvases();
        RectTransform parentRect = DialogRect.parent as RectTransform;
        if (parentRect == null) return;

        Vector2 size   = DialogRect.rect.size;
        Vector2 parent = parentRect.rect.size;
        Vector2 pos    = DialogRect.anchoredPosition;

        pos.x = Mathf.Clamp(pos.x, -parent.x * 0.5f, parent.x * 0.5f - size.x);
        float halfH = size.y * 0.5f;
        pos.y = Mathf.Clamp(pos.y, -parent.y * 0.5f + halfH, parent.y * 0.5f - halfH);
        DialogRect.anchoredPosition = pos;
    }

    // ── Runtime build (Create() path) ───────────────────────────────────────

    /// <summary>
    /// Factory dùng cho ItemUseHandler (runtime-only, không cần prefab).
    /// </summary>
    public static BagQuickActionPanel Create(Transform parent)
    {
        GameObject root = new GameObject("BagQuickActionPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(BagQuickActionPanel));
        root.transform.SetParent(parent, false);
        return root.GetComponent<BagQuickActionPanel>();
    }

    private void BuildRuntime()
    {
        if (_builtRuntime) return;
        _builtRuntime = true;

        RectTransform rootRect = (RectTransform)transform;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        _overlayImage = GetComponent<Image>();
        _overlayImage.color = new Color(0f, 0f, 0f, 0.68f);
        _overlayImage.raycastTarget = true;

        _overlayButtonRuntime = GetComponent<Button>();
        _overlayButtonRuntime.targetGraphic = _overlayImage;
        _overlayButtonRuntime.onClick.RemoveAllListeners();
        _overlayButtonRuntime.onClick.AddListener(HandleOverlayClick);

        GameObject dialog = new GameObject("Dialog",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dialog.transform.SetParent(transform, false);
        UIDraggablePanel.Ensure(dialog);
        _dialogRectRuntime = dialog.GetComponent<RectTransform>();
        _dialogRectRuntime.anchorMin = new Vector2(0.5f, 0.5f);
        _dialogRectRuntime.anchorMax = new Vector2(0.5f, 0.5f);
        _dialogRectRuntime.pivot     = new Vector2(0.5f, 0.5f);
        _dialogRectRuntime.sizeDelta = new Vector2(320f, 180f);

        Image dialogImage = dialog.GetComponent<Image>();
        dialogImage.color = new Color(0.11f, 0.12f, 0.16f, 0.98f);
        dialogImage.raycastTarget = true;

        _titleTextRuntime = CreateLabel(_dialogRectRuntime, "TitleText",
            new Vector2(0f, 44f), new Vector2(280f, 52f), 24f, FontStyles.Bold);
        _titleTextRuntime.alignment = TextAlignmentOptions.Center;
        _titleTextRuntime.color = Color.white;
        _titleTextRuntime.text  = "Tui mo rong";

        _detachButtonRuntime = CreateButton(_dialogRectRuntime, "DetachButton",
            new Vector2(-68f, -20f), "Cất vào", out _detachLabel);
        _detachButtonRuntime.onClick.AddListener(() => { _onDetach?.Invoke(); Hide(); });

        _viewButtonRuntime = CreateButton(_dialogRectRuntime, "ViewButton",
            new Vector2(68f, -20f), "Xem", out _viewLabel);
        _viewButtonRuntime.onClick.AddListener(() => { _onView?.Invoke(); Hide(); });

        UIRuntimeAssetHelper.ApplyNotoSans(_titleTextRuntime, _detachLabel, _viewLabel);
    }

    private static TMP_Text CreateLabel(Transform parent, string name,
        Vector2 pos, Vector2 size, float fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        TMP_Text t = go.GetComponent<TextMeshProUGUI>();
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.enableWordWrapping = true;
        t.text = string.Empty;
        return t;
    }

    private static Button CreateButton(Transform parent, string name,
        Vector2 pos, string label, out TMP_Text labelText)
    {
        GameObject go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform r = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(120f, 42f);
        Image img = go.GetComponent<Image>();
        img.color = new Color(0.84f, 0.67f, 0.22f, 1f);
        Button btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        labelText = CreateLabel(go.transform, "Label", Vector2.zero, r.sizeDelta, 20f, FontStyles.Bold);
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = new Color(0.14f, 0.1f, 0.04f, 1f);
        labelText.text = label;
        return btn;
    }
}
