#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CreateItemDetailPanelPrefab
{
    private const string PrefabPath = "Assets/Prefabs/UI/ItemDetailPanel.prefab";
    private const string NotoSansPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSans-Regular SDF.asset";

    private static TMP_FontAsset _font;

    [MenuItem("GameTools/UI/Rebuild Item Detail Panel")]
    public static void Rebuild()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoSansPath);

        GameObject root = new GameObject("ItemDetailPanel", typeof(RectTransform));
        root.layer = 5;

        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(490f, 292f);
        rootRt.pivot = new Vector2(0.5f, 0.5f);

        Image bg = root.AddComponent<Image>();
        bg.color = new Color(0.52f, 0.24f, 0.08f, 0.98f);
        bg.raycastTarget = true;

        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = new Color(0.93f, 0.66f, 0.28f, 0.9f);
        outline.effectDistance = new Vector2(1f, -1f);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 200;
        root.AddComponent<GraphicRaycaster>();

        TMP_Text title = MakeText(root.transform, "ItemNameText", "Nhân sâm (thượng hạng)",
            21f, FontStyles.Bold, new Color(1f, 0.96f, 0.86f, 1f), TextAlignmentOptions.Left);
        RectTransform titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(18f, -10f);
        titleRt.sizeDelta = new Vector2(-64f, 32f);
        title.enableWordWrapping = false;
        title.overflowMode = TextOverflowModes.Ellipsis;

        Button closeButton = MakeSmallTextButton(root.transform, "CloseButton", "X",
            new Color(0.34f, 0.12f, 0.03f, 0.95f), new Color(1f, 0.72f, 0.32f, 1f));
        RectTransform closeRt = closeButton.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-7f, -7f);
        closeRt.sizeDelta = new Vector2(28f, 28f);

        ScrollRect scrollRect = MakeScrollView(root.transform, out TMP_Text bodyText);
        RectTransform scrollRt = scrollRect.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(12f, 60f);
        scrollRt.offsetMax = new Vector2(-12f, -46f);

        GameObject buttonRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        buttonRow.layer = 5;
        buttonRow.transform.SetParent(root.transform, false);
        RectTransform rowRt = buttonRow.GetComponent<RectTransform>();
        rowRt.anchorMin = new Vector2(0f, 0f);
        rowRt.anchorMax = new Vector2(1f, 0f);
        rowRt.pivot = new Vector2(0.5f, 0f);
        rowRt.anchoredPosition = new Vector2(0f, 8f);
        rowRt.sizeDelta = new Vector2(-34f, 42f);

        HorizontalLayoutGroup row = buttonRow.GetComponent<HorizontalLayoutGroup>();
        row.spacing = 12f;
        row.childAlignment = TextAnchor.MiddleRight;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;

        Button shortcutButton = MakeActionButton(buttonRow.transform, "ShortcutButton", "Phím tắt", 82f);
        Button splitButton = MakeActionButton(buttonRow.transform, "SplitButton", "Tách", 82f);
        Button dropButton = MakeActionButton(buttonRow.transform, "DropButton", "Vứt bỏ", 82f);
        Button useManyButton = MakeActionButton(buttonRow.transform, "UseManyButton", "SD nhiều", 90f);
        Button useButton = MakeActionButton(buttonRow.transform, "UseButton", "Sử dụng", 90f);

        ItemDetailPanel panel = root.AddComponent<ItemDetailPanel>();
        SerializedObject so = new SerializedObject(panel);
        so.FindProperty("itemNameText").objectReferenceValue = title;
        so.FindProperty("itemDescriptionText").objectReferenceValue = bodyText;
        so.FindProperty("useButton").objectReferenceValue = useButton;
        so.FindProperty("useButtonText").objectReferenceValue = useButton.GetComponentInChildren<TMP_Text>(true);
        so.FindProperty("btnClose").objectReferenceValue = closeButton;
        so.FindProperty("shortcutButton").objectReferenceValue = shortcutButton;
        so.FindProperty("shortcutButtonText").objectReferenceValue = shortcutButton.GetComponentInChildren<TMP_Text>(true);
        so.FindProperty("splitButton").objectReferenceValue = splitButton;
        so.FindProperty("splitButtonText").objectReferenceValue = splitButton.GetComponentInChildren<TMP_Text>(true);
        so.FindProperty("dropButton").objectReferenceValue = dropButton;
        so.FindProperty("dropButtonText").objectReferenceValue = dropButton.GetComponentInChildren<TMP_Text>(true);
        so.FindProperty("useManyButton").objectReferenceValue = useManyButton;
        so.FindProperty("useManyButtonText").objectReferenceValue = useManyButton.GetComponentInChildren<TMP_Text>(true);
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        UnityEngine.Object.DestroyImmediate(root);
        AssetDatabase.Refresh();
        { /* Rebuilt */ }
    }

    private static ScrollRect MakeScrollView(Transform parent, out TMP_Text bodyText)
    {
        GameObject root = new GameObject("InfoScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        root.layer = 5;
        root.transform.SetParent(parent, false);

        Image rootImage = root.GetComponent<Image>();
        rootImage.color = new Color(0.38f, 0.15f, 0.04f, 0.62f);
        rootImage.raycastTarget = false;

        ScrollRect scrollRect = root.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 22f;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.layer = 5;
        viewport.transform.SetParent(root.transform, false);
        Fill(viewport.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(-10f, 0f));

        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.01f);
        viewportImage.raycastTarget = true;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.layer = 5;
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(8, 8, 8, 8);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        bodyText = MakeText(content.transform, "ItemDescriptionText",
            "Yêu cầu cấp: 20\nKhông khóa\nCó thể xếp chồng\nGiá bán: 0 bạc",
            16f, FontStyles.Bold, Color.white, TextAlignmentOptions.TopLeft);
        bodyText.enableWordWrapping = true;
        bodyText.overflowMode = TextOverflowModes.Overflow;
        bodyText.lineSpacing = 8f;
        bodyText.raycastTarget = false;
        bodyText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        GameObject scrollbar = new GameObject("VerticalScrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbar.layer = 5;
        scrollbar.transform.SetParent(root.transform, false);
        Fill(scrollbar.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-8f, 0f), new Vector2(0f, 0f));
        scrollbar.GetComponent<RectTransform>().sizeDelta = new Vector2(8f, 0f);
        scrollbar.GetComponent<Image>().color = new Color(0.20f, 0.08f, 0.02f, 0.85f);

        GameObject slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.layer = 5;
        slidingArea.transform.SetParent(scrollbar.transform, false);
        Fill(slidingArea.GetComponent<RectTransform>());

        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.layer = 5;
        handle.transform.SetParent(slidingArea.transform, false);
        Fill(handle.GetComponent<RectTransform>());
        Image handleImage = handle.GetComponent<Image>();
        handleImage.color = new Color(1f, 0.74f, 0.26f, 1f);

        Scrollbar bar = scrollbar.GetComponent<Scrollbar>();
        bar.direction = Scrollbar.Direction.BottomToTop;
        bar.targetGraphic = handleImage;
        bar.handleRect = handle.GetComponent<RectTransform>();

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content = contentRt;
        scrollRect.verticalScrollbar = bar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

        return scrollRect;
    }

    private static Button MakeActionButton(Transform parent, string name, string label, float width)
    {
        Button button = MakeSmallTextButton(parent, name, label,
            new Color(0.77f, 0.32f, 0.07f, 1f), new Color(1f, 0.84f, 0.44f, 1f));

        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = 38f;
        layout.preferredHeight = 38f;
        return button;
    }

    private static Button MakeSmallTextButton(Transform parent, string name, string label, Color bgColor, Color outlineColor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        go.layer = 5;
        go.transform.SetParent(parent, false);

        Image image = go.GetComponent<Image>();
        image.color = bgColor;
        image.raycastTarget = true;

        Outline outline = go.GetComponent<Outline>();
        outline.effectColor = outlineColor;
        outline.effectDistance = new Vector2(1f, -1f);

        Button button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;

        TMP_Text text = MakeText(go.transform, "Text (TMP)", label,
            16f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        Fill(text.rectTransform);
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return button;
    }

    private static TMP_Text MakeText(Transform parent, string name, string text, float size,
                                     FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = 5;
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.richText = true;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        if (_font != null)
            tmp.font = _font;
        return tmp;
    }

    private static void Fill(RectTransform rectTransform)
    {
        Fill(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static void Fill(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax,
                             Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
        rectTransform.localScale = Vector3.one;
    }
}
#endif
