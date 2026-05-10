#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool – tự động tạo prefabs cho hệ thống kỹ năng:
///   • SkillRowPrefab       (hàng kỹ năng: icon + tên + level / "Đã đạt cấp tối đa")
///   • SkillDetailPanel     (panel thông tin kỹ năng: scroll view + nút Cộng)
///
/// Menu: GameTools → Skill → Create Skill Prefabs
/// </summary>
public static class SkillPrefabCreator
{
    private const string PREFAB_DIR   = "Assets/Resources/Prefabs/UI";
    private const string NOTO_SANS    = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSans-Regular SDF.asset";

    private static TMP_FontAsset _font;

    // ──────────────────────────────────────────────────────────────────────────
    [MenuItem("GameTools/Skill/Create Skill Prefabs")]
    public static void CreateAll()
    {
        EnsureDir(PREFAB_DIR);
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NOTO_SANS);

        CreateSkillRowPrefab();
        CreateSkillDetailPanelPrefab();

        AssetDatabase.Refresh();
        Debug.Log("[SkillPrefabCreator] ✓ Tạo xong SkillRowPrefab + SkillDetailPanel tại " + PREFAB_DIR);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SKILL ROW PREFAB
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Hàng kỹ năng gọn: [Icon] [Tên skill / Lv x/max hoặc "Đã đạt cấp tối đa"]
    /// Toàn bộ row là Button – click → hiện SkillDetailPanel.
    /// </summary>
    private static void CreateSkillRowPrefab()
    {
        var root = new GameObject("SkillRowPrefab");
        root.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 52f);

        // Background + outline
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.20f, 0.10f, 0.04f, 0.82f);
        bg.raycastTarget = true;

        var outline = root.AddComponent<Outline>();
        outline.effectColor    = new Color(1f, 0.78f, 0.32f, 0.72f);
        outline.effectDistance = new Vector2(1f, -1f);

        // Row is the Button
        var btn = root.AddComponent<Button>();
        btn.targetGraphic = bg;
        btn.transition    = Selectable.Transition.ColorTint;

        // Layout
        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(6, 8, 5, 5);
        hlg.spacing               = 7f;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childControlWidth     = true;
        hlg.childControlHeight    = true;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        var le = root.AddComponent<LayoutElement>();
        le.minHeight       = 52f;
        le.preferredHeight = 52f;
        le.flexibleWidth   = 1f;

        // ── Icon frame ────────────────────────────────────────────────────────
        var iconFrame = MakeChild(root, "IconFrame");
        iconFrame.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 1f);

        var iconOutline = iconFrame.AddComponent<Outline>();
        iconOutline.effectColor    = new Color(1f, 0.88f, 0.55f, 0.95f);
        iconOutline.effectDistance = new Vector2(1f, -1f);

        var iconFrameLe = iconFrame.AddComponent<LayoutElement>();
        iconFrameLe.minWidth       = 42f;
        iconFrameLe.preferredWidth = 42f;
        iconFrameLe.minHeight      = 42f;
        iconFrameLe.preferredHeight = 42f;
        iconFrameLe.flexibleWidth  = 0f;

        // Icon image (child of frame)
        var iconImgGo = MakeChild(iconFrame, "IconImage");
        var iconImgRt = iconImgGo.GetComponent<RectTransform>();
        Fill(iconImgRt);
        var iconImg = iconImgGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.raycastTarget  = false;

        // ── Text block ────────────────────────────────────────────────────────
        var textBlock = MakeChild(root, "TextBlock");
        var textLe    = textBlock.AddComponent<LayoutElement>();
        textLe.flexibleWidth = 1f;
        textLe.minWidth      = 110f;

        var vlg = textBlock.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 1f;
        vlg.childAlignment        = TextAnchor.MiddleLeft;
        vlg.childControlWidth     = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // Skill name
        var txtName = MakeTmp(textBlock, "TxtSkillName", "Kiếm thuật cơ bản", 16f, FontStyles.Bold, new Color(0f, 1f, 0.62f, 1f));
        txtName.enableWordWrapping = false;
        txtName.overflowMode       = TextOverflowModes.Ellipsis;
        var nameLe = txtName.gameObject.AddComponent<LayoutElement>();
        nameLe.minHeight       = 20f;
        nameLe.preferredHeight = 20f;
        nameLe.flexibleWidth   = 1f;

        // Level / max label
        var txtLevel = MakeTmp(textBlock, "TxtLevel", "Lv 1/5", 13f, FontStyles.Bold, Color.white);
        txtLevel.enableWordWrapping = false;
        txtLevel.overflowMode       = TextOverflowModes.Ellipsis;
        var levelLe = txtLevel.gameObject.AddComponent<LayoutElement>();
        levelLe.minHeight       = 16f;
        levelLe.preferredHeight = 16f;
        levelLe.flexibleWidth   = 1f;

        // Wire SkillRowUI
        var rowUI = root.AddComponent<SkillRowUI>();
        var so    = new SerializedObject(rowUI);
        so.FindProperty("txtSkillName").objectReferenceValue = txtName;
        so.FindProperty("txtLevel")    .objectReferenceValue = txtLevel;
        so.FindProperty("iconImage")   .objectReferenceValue = iconImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "SkillRowPrefab");
        Object.DestroyImmediate(root);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SKILL DETAIL PANEL PREFAB
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Panel thông tin kỹ năng (hiện khi chọn một row):
    ///   Header   – icon + tên kỹ năng
    ///   ScrollView – mô tả, cấp tối đa, level yêu cầu, MP, hồi chiêu,
    ///                buff từng cấp
    ///   BtnUpgrade – chỉ 1 nút "Cộng" (góc phải dưới)
    /// </summary>
    private static void CreateSkillDetailPanelPrefab()
    {
        var root   = new GameObject("SkillDetailPanel");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(280f, 400f);

        // Background
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.43f, 0.19f, 0.06f, 0.96f);
        bg.raycastTarget = true;

        var outline = root.AddComponent<Outline>();
        outline.effectColor    = new Color(0.93f, 0.78f, 0.48f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);

        // ── Header ────────────────────────────────────────────────────────────
        var header   = MakeChild(root, "Header");
        var headerRt = header.GetComponent<RectTransform>();
        headerRt.anchorMin       = new Vector2(0f, 1f);
        headerRt.anchorMax       = new Vector2(1f, 1f);
        headerRt.pivot           = new Vector2(0.5f, 1f);
        headerRt.anchoredPosition = new Vector2(0f, -6f);
        headerRt.sizeDelta       = new Vector2(-12f, 48f);

        header.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);   // transparent bg

        var headerHlg = header.AddComponent<HorizontalLayoutGroup>();
        headerHlg.padding               = new RectOffset(8, 8, 3, 3);
        headerHlg.spacing               = 8f;
        headerHlg.childAlignment        = TextAnchor.MiddleLeft;
        headerHlg.childControlWidth     = true;
        headerHlg.childControlHeight    = true;
        headerHlg.childForceExpandWidth  = false;
        headerHlg.childForceExpandHeight = false;

        // Header: icon frame
        var hIconFrame = MakeChild(header, "IconFrame");
        hIconFrame.AddComponent<Image>().color = new Color(0.05f, 0.04f, 0.03f, 1f);

        var hIconOutline = hIconFrame.AddComponent<Outline>();
        hIconOutline.effectColor    = new Color(1f, 0.88f, 0.55f, 0.95f);
        hIconOutline.effectDistance = new Vector2(1f, -1f);

        var hIconLe = hIconFrame.AddComponent<LayoutElement>();
        hIconLe.minWidth        = 42f;
        hIconLe.preferredWidth  = 42f;
        hIconLe.minHeight       = 42f;
        hIconLe.preferredHeight = 42f;
        hIconLe.flexibleWidth   = 0f;

        var hIconImgGo = MakeChild(hIconFrame, "IconImage");
        Fill(hIconImgGo.GetComponent<RectTransform>());
        var hIconImg = hIconImgGo.AddComponent<Image>();
        hIconImg.preserveAspect = true;
        hIconImg.raycastTarget  = false;

        // Header: title
        var txtTitle = MakeTmp(header, "TxtTitle", "Kiếm thuật cơ bản", 17f, FontStyles.Bold, new Color(0f, 1f, 0.62f, 1f));
        txtTitle.enableWordWrapping = false;
        txtTitle.overflowMode       = TextOverflowModes.Ellipsis;
        var titleLe = txtTitle.gameObject.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;
        titleLe.minHeight     = 38f;

        // ── ScrollView ────────────────────────────────────────────────────────
        var scrollRoot   = MakeChild(root, "SkillInfoScrollView");
        var scrollBg     = scrollRoot.AddComponent<Image>();
        scrollBg.color   = new Color(0.33f, 0.14f, 0.04f, 0.72f);
        scrollBg.raycastTarget = false;

        var scrollRt = scrollRoot.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(8f, 50f);
        scrollRt.offsetMax = new Vector2(-22f, -60f);

        var scrollRect = scrollRoot.AddComponent<ScrollRect>();
        scrollRect.horizontal         = false;
        scrollRect.vertical           = true;
        scrollRect.movementType       = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity  = 20f;

        // Viewport
        var viewport     = MakeChild(scrollRoot, "Viewport");
        var viewportImg  = viewport.AddComponent<Image>();
        viewportImg.color = new Color(0f, 0f, 0f, 0.01f);  // near-transparent for Mask
        viewportImg.raycastTarget = true;
        Fill(viewport.GetComponent<RectTransform>());
        var mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        var content   = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin       = new Vector2(0f, 1f);
        contentRt.anchorMax       = new Vector2(1f, 1f);
        contentRt.pivot           = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta       = Vector2.zero;

        var contentVlg = content.AddComponent<VerticalLayoutGroup>();
        contentVlg.padding               = new RectOffset(8, 8, 7, 7);
        contentVlg.spacing               = 4f;
        contentVlg.childControlWidth     = true;
        contentVlg.childControlHeight    = true;
        contentVlg.childForceExpandWidth  = true;
        contentVlg.childForceExpandHeight = false;

        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Body text (description + stats per level)
        var txtBody = MakeTmp(content, "TxtBody", "Mô tả kỹ năng...", 15f, FontStyles.Normal, Color.white);
        txtBody.enableWordWrapping = true;
        txtBody.overflowMode       = TextOverflowModes.Overflow;
        txtBody.lineSpacing        = 4f;
        var bodyLe = txtBody.gameObject.AddComponent<LayoutElement>();
        bodyLe.flexibleWidth = 1f;

        // Wire ScrollRect
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content  = contentRt;

        // ── Vertical Scrollbar ────────────────────────────────────────────────
        var scrollbar   = MakeChild(root, "VerticalScrollbar");
        scrollbar.AddComponent<Image>().color = new Color(0.24f, 0.11f, 0.04f, 0.9f);

        var sbRt = scrollbar.GetComponent<RectTransform>();
        sbRt.anchorMin       = new Vector2(1f, 0f);
        sbRt.anchorMax       = new Vector2(1f, 1f);
        sbRt.pivot           = new Vector2(1f, 0.5f);
        sbRt.anchoredPosition = new Vector2(-8f, -5f);
        sbRt.sizeDelta       = new Vector2(10f, -108f);

        var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbar.transform, false);
        Fill(slidingArea.GetComponent<RectTransform>());

        var handle   = MakeChild(slidingArea, "Handle");
        handle.AddComponent<Image>().color = new Color(1f, 0.78f, 0.35f, 1f);
        Fill(handle.GetComponent<RectTransform>());

        var sbComp = scrollbar.AddComponent<Scrollbar>();
        sbComp.direction      = Scrollbar.Direction.BottomToTop;
        sbComp.targetGraphic  = handle.GetComponent<Image>();
        sbComp.handleRect     = handle.GetComponent<RectTransform>();

        scrollRect.verticalScrollbar            = sbComp;
        scrollRect.verticalScrollbarVisibility  = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing     = -3f;

        // ── Upgrade button "Cộng" ─────────────────────────────────────────────
        var btnGo = MakeChild(root, "BtnUpgrade");
        var btnBg = btnGo.AddComponent<Image>();
        btnBg.color = new Color(0.84f, 0.41f, 0.10f, 1f);

        var btnOutline = btnGo.AddComponent<Outline>();
        btnOutline.effectColor    = new Color(1f, 0.86f, 0.52f, 0.9f);
        btnOutline.effectDistance = new Vector2(1f, -1f);

        var btn = btnGo.AddComponent<Button>();
        btn.targetGraphic = btnBg;

        var btnRt = btnGo.GetComponent<RectTransform>();
        btnRt.anchorMin       = new Vector2(1f, 0f);
        btnRt.anchorMax       = new Vector2(1f, 0f);
        btnRt.pivot           = new Vector2(1f, 0f);
        btnRt.anchoredPosition = new Vector2(-8f, 8f);
        btnRt.sizeDelta       = new Vector2(86f, 34f);

        var btnLabel = MakeTmp(btnGo, "Text (TMP)", "Cộng", 16f, FontStyles.Bold, Color.white);
        btnLabel.alignment = TextAlignmentOptions.Center;
        Fill(btnLabel.rectTransform);

        // ── Wire SkillDetailPanelUI ───────────────────────────────────────────
        var panelUI = root.AddComponent<SkillDetailPanelUI>();
        var so      = new SerializedObject(panelUI);
        so.FindProperty("iconImage") .objectReferenceValue = hIconImg;
        so.FindProperty("txtTitle")  .objectReferenceValue = txtTitle;
        so.FindProperty("txtBody")   .objectReferenceValue = txtBody;
        so.FindProperty("btnUpgrade").objectReferenceValue = btn;
        so.FindProperty("txtUpgrade").objectReferenceValue = btnLabel;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "SkillDetailPanel");
        Object.DestroyImmediate(root);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════════

    private static GameObject MakeChild(GameObject parent, string childName)
    {
        var go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        go.layer = parent.layer;
        return go;
    }

    private static GameObject MakeChild(GameObject parent, string childName, RectTransform dummyOut) => MakeChild(parent, childName);

    private static TMP_Text MakeTmp(GameObject parent, string childName, string defaultText, float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        go.layer = parent.layer;

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text       = defaultText;
        tmp.fontSize   = fontSize;
        tmp.fontStyle  = style;
        tmp.color      = color;
        tmp.alignment  = TextAlignmentOptions.Left;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        if (_font != null)
            tmp.font = _font;

        return tmp;
    }

    private static void Fill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }

    private static void EnsureDir(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void SavePrefab(GameObject go, string prefabName)
    {
        string path = $"{PREFAB_DIR}/{prefabName}.prefab";
        var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        if (saved != null)
            Debug.Log($"[SkillPrefabCreator] ✓ Đã lưu: {path}");
        else
            Debug.LogError($"[SkillPrefabCreator] ✗ Lưu thất bại: {path}");
    }
}
#endif
