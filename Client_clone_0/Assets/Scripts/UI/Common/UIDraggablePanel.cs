using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class UIDraggablePanel : MonoBehaviour, IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float CanvasEdgePadding = 8f;

    private static readonly Vector3[] PanelWorldCorners = new Vector3[4];
    private static readonly Vector3[] CanvasWorldCorners = new Vector3[4];
    private static readonly Vector3[] PanelCanvasCorners = new Vector3[4];
    private static readonly Vector3[] CanvasLocalCorners = new Vector3[4];

    private RectTransform _rectTransform;
    private RectTransform _rootCanvasRect;
    private Vector2 _dragOffsetInCanvasSpace;
    private bool _canDragCurrentGesture;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        CacheRootCanvas();
    }

    private void OnEnable()
    {
        ClampToRootCanvas(_rectTransform);
    }

    private void LateUpdate()
    {
        if (_rectTransform == null || !_rectTransform.gameObject.activeInHierarchy)
            return;

        if (_rootCanvasRect == null && !CacheRootCanvas())
            return;

        ClampToRootCanvas(_rectTransform, _rootCanvasRect);
    }

    public static UIDraggablePanel Ensure(GameObject target)
    {
        if (target == null)
            return null;

        if (target.TryGetComponent(out UIDraggablePanel existing))
            return existing;

        return target.AddComponent<UIDraggablePanel>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        BringToFront();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        _canDragCurrentGesture = TryStartDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_canDragCurrentGesture || _rectTransform == null || _rootCanvasRect == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerCanvasPosition))
        {
            return;
        }

        SetPivotPositionInCanvasSpace(pointerCanvasPosition + _dragOffsetInCanvasSpace);
        ClampToRootCanvas(_rectTransform, _rootCanvasRect);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_rectTransform != null && _rootCanvasRect != null)
            ClampToRootCanvas(_rectTransform, _rootCanvasRect);

        _canDragCurrentGesture = false;
    }

    public static bool ClampToRootCanvas(RectTransform rectTransform)
    {
        if (!TryResolveRootCanvasRect(rectTransform, out RectTransform rootCanvasRect))
            return false;

        return ClampToRootCanvas(rectTransform, rootCanvasRect);
    }

    internal static bool TryResolveRootCanvasRect(RectTransform rectTransform, out RectTransform rootCanvasRect)
    {
        rootCanvasRect = null;
        if (rectTransform == null)
            return false;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
            return false;

        Canvas rootCanvas = canvas.rootCanvas != null ? canvas.rootCanvas : canvas;
        if (rootCanvas == null || rootCanvas.renderMode == RenderMode.WorldSpace)
            return false;

        rootCanvasRect = rootCanvas.transform as RectTransform;
        return rootCanvasRect != null;
    }

    internal static bool ClampToRootCanvas(RectTransform rectTransform, RectTransform rootCanvasRect)
    {
        if (rectTransform == null || rootCanvasRect == null)
            return false;

        Canvas.ForceUpdateCanvases();

        rectTransform.GetWorldCorners(PanelWorldCorners);
        rootCanvasRect.GetWorldCorners(CanvasWorldCorners);

        for (int i = 0; i < 4; i++)
        {
            PanelCanvasCorners[i] = rootCanvasRect.InverseTransformPoint(PanelWorldCorners[i]);
            CanvasLocalCorners[i] = rootCanvasRect.InverseTransformPoint(CanvasWorldCorners[i]);
        }

        float panelMinX = MinX(PanelCanvasCorners);
        float panelMaxX = MaxX(PanelCanvasCorners);
        float panelMinY = MinY(PanelCanvasCorners);
        float panelMaxY = MaxY(PanelCanvasCorners);
        float canvasMinX = MinX(CanvasLocalCorners) + CanvasEdgePadding;
        float canvasMaxX = MaxX(CanvasLocalCorners) - CanvasEdgePadding;
        float canvasMinY = MinY(CanvasLocalCorners) + CanvasEdgePadding;
        float canvasMaxY = MaxY(CanvasLocalCorners) - CanvasEdgePadding;

        if (canvasMaxX <= canvasMinX || canvasMaxY <= canvasMinY)
            return false;

        Vector2 adjustment = Vector2.zero;
        float panelWidth = panelMaxX - panelMinX;
        float panelHeight = panelMaxY - panelMinY;
        float canvasWidth = canvasMaxX - canvasMinX;
        float canvasHeight = canvasMaxY - canvasMinY;

        if (panelWidth >= canvasWidth)
        {
            adjustment.x = (canvasMinX + canvasMaxX - panelMinX - panelMaxX) * 0.5f;
        }
        else if (panelMinX < canvasMinX)
        {
            adjustment.x = canvasMinX - panelMinX;
        }
        else if (panelMaxX > canvasMaxX)
        {
            adjustment.x = canvasMaxX - panelMaxX;
        }

        if (panelHeight >= canvasHeight)
        {
            adjustment.y = (canvasMinY + canvasMaxY - panelMinY - panelMaxY) * 0.5f;
        }
        else if (panelMinY < canvasMinY)
        {
            adjustment.y = canvasMinY - panelMinY;
        }
        else if (panelMaxY > canvasMaxY)
        {
            adjustment.y = canvasMaxY - panelMaxY;
        }

        if (adjustment.sqrMagnitude <= 0.0001f)
            return false;

        Vector3 currentCanvasPosition = rootCanvasRect.InverseTransformPoint(rectTransform.position);
        Vector2 targetCanvasPosition = new Vector2(
            currentCanvasPosition.x + adjustment.x,
            currentCanvasPosition.y + adjustment.y);

        SetPivotPositionInCanvasSpace(rectTransform, rootCanvasRect, targetCanvasPosition);
        return true;
    }

    private static float MinX(Vector3[] corners)
    {
        float value = corners[0].x;
        for (int i = 1; i < corners.Length; i++)
            value = Mathf.Min(value, corners[i].x);
        return value;
    }

    private static float MaxX(Vector3[] corners)
    {
        float value = corners[0].x;
        for (int i = 1; i < corners.Length; i++)
            value = Mathf.Max(value, corners[i].x);
        return value;
    }

    private static float MinY(Vector3[] corners)
    {
        float value = corners[0].y;
        for (int i = 1; i < corners.Length; i++)
            value = Mathf.Min(value, corners[i].y);
        return value;
    }

    private static float MaxY(Vector3[] corners)
    {
        float value = corners[0].y;
        for (int i = 1; i < corners.Length; i++)
            value = Mathf.Max(value, corners[i].y);
        return value;
    }

    private bool TryStartDrag(PointerEventData eventData)
    {
        if (_rectTransform == null || !CacheRootCanvas())
            return false;

        GameObject source = eventData.pointerPressRaycast.gameObject != null
            ? eventData.pointerPressRaycast.gameObject
            : eventData.pointerCurrentRaycast.gameObject;

        if (ShouldBlockDragFrom(source))
            return false;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rootCanvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 pointerCanvasPosition))
        {
            return false;
        }

        _dragOffsetInCanvasSpace = GetPivotPositionInCanvasSpace() - pointerCanvasPosition;
        BringToFront();
        return true;
    }

    private bool CacheRootCanvas()
    {
        return TryResolveRootCanvasRect(_rectTransform, out _rootCanvasRect);
    }

    private Vector2 GetPivotPositionInCanvasSpace()
    {
        Vector3 local = _rootCanvasRect.InverseTransformPoint(_rectTransform.position);
        return new Vector2(local.x, local.y);
    }

    private void SetPivotPositionInCanvasSpace(Vector2 canvasLocalPosition)
    {
        SetPivotPositionInCanvasSpace(_rectTransform, _rootCanvasRect, canvasLocalPosition);
    }

    private static void SetPivotPositionInCanvasSpace(RectTransform rectTransform, RectTransform rootCanvasRect, Vector2 canvasLocalPosition)
    {
        Vector3 worldPosition = rootCanvasRect.TransformPoint(new Vector3(canvasLocalPosition.x, canvasLocalPosition.y, 0f));
        worldPosition.z = rectTransform.position.z;
        rectTransform.position = worldPosition;
    }

    private bool ShouldBlockDragFrom(GameObject source)
    {
        if (source == null)
            return false;

        Transform current = source.transform;
        while (current != null)
        {
            if (current == transform)
                return false;

            if (current.GetComponent<TMP_InputField>() != null ||
                current.GetComponent<InputField>() != null ||
                current.GetComponent<ScrollRect>() != null ||
                current.GetComponent<Scrollbar>() != null ||
                current.GetComponent<Slider>() != null ||
                current.GetComponent<Dropdown>() != null ||
                current.GetComponent<TMP_Dropdown>() != null ||
                current.GetComponent<Toggle>() != null ||
                current.GetComponent<Selectable>() != null)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void BringToFront()
    {
        if (transform.parent != null)
            transform.SetAsLastSibling();
    }
}

[DefaultExecutionOrder(-9000)]
public sealed class UIPanelDragRuntimeInstaller : MonoBehaviour
{
    private static readonly Vector2 ReferenceResolution = new(1920f, 1080f);
    private const float RescanIntervalSeconds = 0.5f;

    private static bool _installed;
    private float _nextRescanAt;
    private Vector2Int _lastScreenSize;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (_installed)
            return;

        UIPanelDragRuntimeInstaller existing = FindObjectOfType<UIPanelDragRuntimeInstaller>(true);
        if (existing != null)
        {
            _installed = true;
            return;
        }

        GameObject installer = new(nameof(UIPanelDragRuntimeInstaller));
        installer.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(installer);
        installer.AddComponent<UIPanelDragRuntimeInstaller>();
        _installed = true;
    }

    private void Awake()
    {
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        ApplyToKnownPanels();
        _nextRescanAt = Time.unscaledTime + RescanIntervalSeconds;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _installed = false;
    }

    private void Update()
    {
        bool screenSizeChanged = _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height;
        if (!screenSizeChanged && Time.unscaledTime < _nextRescanAt)
            return;

        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        _nextRescanAt = Time.unscaledTime + RescanIntervalSeconds;
        ApplyToKnownPanels();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToKnownPanels();
        _nextRescanAt = Time.unscaledTime + RescanIntervalSeconds;
    }

    private static void ApplyToKnownPanels()
    {
        NormalizeCanvasScalers();

        RectTransform[] rectTransforms = Resources.FindObjectsOfTypeAll<RectTransform>();
        List<RectTransform> candidates = new();

        foreach (RectTransform rectTransform in rectTransforms)
        {
            if (IsDraggablePanelCandidate(rectTransform))
                candidates.Add(rectTransform);
        }

        candidates.Sort((left, right) => GetHierarchyDepth(left).CompareTo(GetHierarchyDepth(right)));

        foreach (RectTransform candidate in candidates)
        {
            if (candidate == null || HasDraggableAncestor(candidate))
                continue;

            UIDraggablePanel.Ensure(candidate.gameObject);
        }

        ClampVisiblePanels(candidates);
    }

    private static void NormalizeCanvasScalers()
    {
        foreach (Canvas canvas in Resources.FindObjectsOfTypeAll<Canvas>())
        {
            if (!ShouldNormalizeCanvas(canvas))
                continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
            scaler.dynamicPixelsPerUnit = 1f;
        }
    }

    private static bool ShouldNormalizeCanvas(Canvas canvas)
    {
        if (canvas == null || !canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace)
            return false;

        GameObject go = canvas.gameObject;
        return go.scene.IsValid() && go.hideFlags == HideFlags.None;
    }

    private static void ClampVisiblePanels(List<RectTransform> candidates)
    {
        foreach (RectTransform candidate in candidates)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
                continue;

            UIDraggablePanel.ClampToRootCanvas(candidate);
        }
    }

    private static bool IsDraggablePanelCandidate(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return false;

        GameObject go = rectTransform.gameObject;
        if (!go.scene.IsValid() || go.hideFlags != HideFlags.None)
            return false;

        if (!UIDraggablePanel.TryResolveRootCanvasRect(rectTransform, out RectTransform rootCanvasRect))
            return false;

        if (rectTransform.rect.width < 120f || rectTransform.rect.height < 80f)
            return false;

        if (!HasPanelIdentity(go))
            return false;

        if (IsBackdropLike(go.name) || IsFullscreenOverlay(rectTransform, rootCanvasRect))
            return false;

        return true;
    }

    private static bool HasPanelIdentity(GameObject go)
    {
        string name = go.name ?? string.Empty;
        if (name.Equals("Panel", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Viewport", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("Content", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (EndsWithPanelMarker(name))
            return true;

        MonoBehaviour[] behaviours = go.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null)
                continue;

            if (EndsWithPanelMarker(behaviour.GetType().Name))
                return true;
        }

        return false;
    }

    private static bool EndsWithPanelMarker(string value)
    {
        return value.EndsWith("Panel", StringComparison.OrdinalIgnoreCase) ||
               value.EndsWith("PanelUI", StringComparison.OrdinalIgnoreCase) ||
               value.EndsWith("Popup", StringComparison.OrdinalIgnoreCase) ||
               value.EndsWith("PopupUI", StringComparison.OrdinalIgnoreCase) ||
               value.EndsWith("Dialog", StringComparison.OrdinalIgnoreCase) ||
               value.EndsWith("Window", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsBackdropLike(string name)
    {
        return name.IndexOf("Backdrop", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Overlay", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Mask", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsFullscreenOverlay(RectTransform rectTransform, RectTransform rootCanvasRect)
    {
        if (rectTransform.anchorMin == Vector2.zero &&
            rectTransform.anchorMax == Vector2.one &&
            rectTransform.offsetMin == Vector2.zero &&
            rectTransform.offsetMax == Vector2.zero)
        {
            return true;
        }

        Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(rootCanvasRect, rectTransform);
        Rect canvasRect = rootCanvasRect.rect;
        return bounds.size.x >= canvasRect.width * 0.95f &&
               bounds.size.y >= canvasRect.height * 0.95f;
    }

    private static bool HasDraggableAncestor(Transform transform)
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.GetComponent<UIDraggablePanel>() != null)
                return true;

            current = current.parent;
        }

        return false;
    }

    private static int GetHierarchyDepth(Transform transform)
    {
        int depth = 0;
        Transform current = transform;
        while (current != null)
        {
            depth++;
            current = current.parent;
        }

        return depth;
    }
}
