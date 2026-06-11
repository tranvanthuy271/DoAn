using UnityEngine;
using UnityEngine.UI;

public class UtilityDrawerController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private GameObject boxRoot;
    [SerializeField] private GameObject contentRoot;
    [SerializeField] private RectTransform boxRect;

    [Header("Buttons")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button showButton;

    [Header("Layout")]
    [SerializeField] private RectTransform toggleButtonRect;
    [SerializeField] private RectTransform toggleGraphic;
    [SerializeField] private RectTransform expandedButtonAnchor;
    [SerializeField] private RectTransform collapsedButtonAnchor;
    [SerializeField] private bool hideBoxWhenCollapsed;
    [SerializeField] private bool bringToggleButtonToFront = true;
    [SerializeField] private float expandedBoxHeight = -1f;
    [SerializeField] private float collapsedBoxHeight = -1f;

    [Header("State")]
    [SerializeField] private bool startExpanded = true;
    [SerializeField] private float expandedArrowRotationZ = 0f;
    [SerializeField] private float collapsedArrowRotationZ = 180f;

    private bool _isExpanded;

    public bool IsExpanded => _isExpanded;

    private void Awake()
    {
        ResolveReferences();
        BindListeners();
    }

    private void Start()
    {
        SetExpanded(startExpanded, true);
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleUtilities);

        if (showButton != null)
            showButton.onClick.RemoveListener(ShowUtilities);
    }

    public void ToggleUtilities()
    {
        SetExpanded(!_isExpanded);
    }

    public void ShowUtilities()
    {
        SetExpanded(true);
    }

    public void HideUtilities()
    {
        SetExpanded(false);
    }

    public void ConfigureRuntime(
        GameObject runtimeBoxRoot,
        GameObject runtimeContentRoot,
        Button runtimeToggleButton,
        Button runtimeShowButton,
        RectTransform runtimeToggleButtonRect,
        RectTransform runtimeToggleGraphic,
        RectTransform runtimeExpandedButtonAnchor,
        RectTransform runtimeCollapsedButtonAnchor,
        RectTransform runtimeBoxRect = null,
        bool runtimeHideBoxWhenCollapsed = false,
        float runtimeExpandedBoxHeight = -1f,
        float runtimeCollapsedBoxHeight = -1f,
        bool runtimeStartExpanded = true)
    {
        boxRoot = runtimeBoxRoot;
        contentRoot = runtimeContentRoot;
        toggleButton = runtimeToggleButton;
        showButton = runtimeShowButton;
        toggleButtonRect = runtimeToggleButtonRect;
        toggleGraphic = runtimeToggleGraphic;
        expandedButtonAnchor = runtimeExpandedButtonAnchor;
        collapsedButtonAnchor = runtimeCollapsedButtonAnchor;
        boxRect = runtimeBoxRect;
        hideBoxWhenCollapsed = runtimeHideBoxWhenCollapsed;
        expandedBoxHeight = runtimeExpandedBoxHeight;
        collapsedBoxHeight = runtimeCollapsedBoxHeight;
        startExpanded = runtimeStartExpanded;

        ResolveReferences();
        BindListeners();
        SetExpanded(startExpanded, true);
    }

    private void SetExpanded(bool expanded, bool instant = false)
    {
        _isExpanded = expanded;

        bool keepBoxVisible = expanded || !hideBoxWhenCollapsed || showButton == null;

        if (boxRoot != null)
            boxRoot.SetActive(keepBoxVisible);

        if (contentRoot != null)
            contentRoot.SetActive(expanded);

        if (showButton != null)
            showButton.gameObject.SetActive(!expanded);

        if (toggleButton != null)
            toggleButton.gameObject.SetActive(expanded || showButton == null || !hideBoxWhenCollapsed);

        RectTransform targetAnchor = expanded ? expandedButtonAnchor : collapsedButtonAnchor;
        SnapToggleButton(targetAnchor);
        ApplyArrowRotation(expanded);
        ApplyBoxHeight(expanded);

        if (!instant)
            { /* State changed expanded={expanded} hideBoxWhenCollapsed={hideBoxWhenCollapsed} */ }
    }

    private void ResolveReferences()
    {
        if (boxRoot == null)
            boxRoot = gameObject;

        if (boxRect == null && boxRoot != null)
            boxRect = boxRoot.transform as RectTransform;

        if (toggleButton == null)
            toggleButton = GetComponent<Button>();

        if (toggleButtonRect == null && toggleButton != null)
            toggleButtonRect = toggleButton.transform as RectTransform;

        if (toggleGraphic == null)
            toggleGraphic = toggleButtonRect;
    }

    private void BindListeners()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleUtilities);
            toggleButton.onClick.AddListener(ToggleUtilities);
        }

        if (showButton != null)
        {
            showButton.onClick.RemoveListener(ShowUtilities);
            showButton.onClick.AddListener(ShowUtilities);
        }
    }

    private void SnapToggleButton(RectTransform targetAnchor)
    {
        if (toggleButtonRect == null || targetAnchor == null)
            return;

        if (toggleButtonRect.parent != targetAnchor.parent)
            toggleButtonRect.SetParent(targetAnchor.parent, false);

        toggleButtonRect.anchorMin = targetAnchor.anchorMin;
        toggleButtonRect.anchorMax = targetAnchor.anchorMax;
        toggleButtonRect.pivot = targetAnchor.pivot;
        toggleButtonRect.sizeDelta = targetAnchor.sizeDelta;
        toggleButtonRect.anchoredPosition = targetAnchor.anchoredPosition;
        toggleButtonRect.localScale = Vector3.one;

        if (bringToggleButtonToFront)
            toggleButtonRect.SetAsLastSibling();

        LayoutRebuilder.MarkLayoutForRebuild(toggleButtonRect);
    }

    private void ApplyArrowRotation(bool expanded)
    {
        if (toggleGraphic == null)
            return;

        float rotationZ = expanded ? expandedArrowRotationZ : collapsedArrowRotationZ;
        toggleGraphic.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
    }

    private void ApplyBoxHeight(bool expanded)
    {
        if (boxRect == null)
            return;

        float targetHeight = expanded ? expandedBoxHeight : collapsedBoxHeight;
        if (targetHeight <= 0f)
            return;

        Vector2 size = boxRect.sizeDelta;
        size.y = targetHeight;
        boxRect.sizeDelta = size;
        LayoutRebuilder.MarkLayoutForRebuild(boxRect);
    }
}