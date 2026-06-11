#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor tool — tạo tự động prefab NpcDynamicMenuPanel + NpcMenuItemRow.
// Chạy từ menu Unity:
// Tools ▸ DoAn ▸ Create NPC Dynamic Menu Prefabs
// Tạo 2 file:
// Assets/Resources/Prefabs/UI/NPC/NpcDynamicMenuPanel.prefab
// Assets/Resources/Prefabs/UI/NPC/NpcMenuItemRow.prefab
// Prefab được wire sẵn tất cả SerializeField — kéo vào Canvas là dùng được.
public static class CreateNpcDynamicMenuPrefabs
{
    private const string NpcPrefabFolder  = "Assets/Resources/Prefabs/UI/NPC";
    private const string PanelPrefabPath  = NpcPrefabFolder + "/NpcDynamicMenuPanel.prefab";
    private const string RowPrefabPath    = NpcPrefabFolder + "/NpcMenuItemRow.prefab";

    // Bảng màu gỗ (khớp DungeonUI & ảnh mẫu LangLa)
    private static readonly Color WoodOuter   = new Color(0.25f, 0.13f, 0.04f, 1.00f); // viền ngoài tối
    private static readonly Color WoodFrame   = new Color(0.36f, 0.20f, 0.07f, 1.00f); // frame chính
    private static readonly Color WoodInner   = new Color(0.56f, 0.33f, 0.11f, 1.00f); // nền gỗ sáng
    private static readonly Color WoodHeader  = new Color(0.44f, 0.26f, 0.08f, 1.00f); // header tối
    private static readonly Color WoodBtn     = new Color(0.55f, 0.32f, 0.08f, 1.00f); // nút "Cáo từ"
    private static readonly Color WoodBtnHov  = new Color(0.72f, 0.44f, 0.13f, 1.00f);
    private static readonly Color WoodBtnPrss = new Color(0.38f, 0.20f, 0.04f, 1.00f);
    private static readonly Color GoldTrim    = new Color(0.87f, 0.68f, 0.22f, 1.00f); // viền vàng
    private static readonly Color GoldText    = new Color(1.00f, 0.91f, 0.52f, 1.00f); // chữ vàng nhạt
    private static readonly Color RowHover    = new Color(0.78f, 0.49f, 0.16f, 0.55f);
    private static readonly Color RowPressed  = new Color(0.90f, 0.58f, 0.18f, 0.75f);
    private static readonly Color RowSep      = new Color(0.42f, 0.24f, 0.07f, 1.00f); // gạch kẻ phân cách
    private static readonly Color ScrollBar   = new Color(0.38f, 0.22f, 0.07f, 0.80f);

    [MenuItem("Tools/DoAn/Create NPC Dynamic Menu Prefabs")]
    public static void CreateAll()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Prefabs");
        EnsureFolder("Assets/Resources/Prefabs/UI");
        EnsureFolder(NpcPrefabFolder);

        bool rowCreated   = CreateRowPrefab();
        bool panelCreated = CreatePanelPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = "";
        if (panelCreated) msg += $"✓ NpcDynamicMenuPanel\n    {PanelPrefabPath}\n\n";
        else              msg += $"— NpcDynamicMenuPanel: đã tồn tại, bỏ qua.\n\n";
        if (rowCreated)   msg += $"✓ NpcMenuItemRow\n    {RowPrefabPath}\n\n";
        else              msg += $"— NpcMenuItemRow: đã tồn tại, bỏ qua.\n\n";
        msg += "Kéo NpcDynamicMenuPanel vào Canvas trong scene rồi để inactive.";

        EditorUtility.DisplayDialog("NPC Dynamic Menu Prefabs", msg, "OK");

        // Chọn panel trong Project window
        var panel = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath);
        if (panel != null) Selection.activeObject = panel;
    }

    //  NpcMenuItemRow — một hàng menu (icon bubble + text + Button)
    private static bool CreateRowPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath) != null)
        { Debug.Log("[CreateNpcMenu] NpcMenuItemRow đã tồn tại → bỏ qua."); return false; }

        // Root (HorizontalLayoutGroup + Button + NpcMenuItemRow)
        var root = NewGO("NpcMenuItemRow");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(300f, 44f);

        // Nền trong suốt mặc định; hover → gỗ sáng
        var rootImg = root.AddComponent<Image>();
        rootImg.color = new Color(0f, 0f, 0f, 0f);

        var btn = root.AddComponent<Button>();
        var bc  = btn.colors;
        bc.normalColor      = new Color(0f, 0f, 0f, 0f);
        bc.highlightedColor = RowHover;
        bc.pressedColor     = RowPressed;
        bc.selectedColor    = RowHover;
        bc.fadeDuration     = 0.08f;
        btn.colors = bc;
        btn.targetGraphic = rootImg;

        var row = root.AddComponent<NpcMenuItemRow>();

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing           = 8f;
        hlg.padding           = new RectOffset(8, 8, 6, 6);
        hlg.childAlignment    = TextAnchor.MiddleLeft;
        hlg.childControlWidth  = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Chat bubble icon
        var iconGo = NewUIGO("BubbleIcon", root.transform);
        var iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.sizeDelta = new Vector2(28f, 28f);

        // Outer circle (putih)
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = new Color(0.88f, 0.82f, 0.72f, 1f);
        iconImg.raycastTarget = false;

        // Inner dot (màu gỗ tối) — tạo ảo giác chat bubble với dấu "..."
        for (int d = 0; d < 3; d++)
        {
            var dot = NewUIGO($"Dot{d}", iconGo.transform);
            var dr = dot.GetComponent<RectTransform>();
            float xOff = -7f + d * 7f;
            dr.anchorMin = new Vector2(0.5f, 0.5f);
            dr.anchorMax = new Vector2(0.5f, 0.5f);
            dr.pivot     = new Vector2(0.5f, 0.5f);
            dr.anchoredPosition = new Vector2(xOff, 0f);
            dr.sizeDelta = new Vector2(5f, 5f);
            var dotImg = dot.AddComponent<Image>();
            dotImg.color = WoodFrame;
            dotImg.raycastTarget = false;
        }

        // Đuôi bubble (tam giác nhỏ ở góc dưới trái)
        var tail = NewUIGO("BubbleTail", iconGo.transform);
        var tailRt = tail.GetComponent<RectTransform>();
        tailRt.anchorMin = new Vector2(0.1f, 0.0f);
        tailRt.anchorMax = new Vector2(0.1f, 0.0f);
        tailRt.pivot     = new Vector2(0f, 1f);
        tailRt.anchoredPosition = new Vector2(0f, 2f);
        tailRt.sizeDelta = new Vector2(8f, 7f);
        var tailImg = tail.AddComponent<Image>();
        tailImg.color = new Color(0.88f, 0.82f, 0.72f, 1f);
        tailImg.raycastTarget = false;

        // Separator line bên phải icon
        var sep = NewUIGO("Separator", root.transform);
        var sepRt = sep.GetComponent<RectTransform>();
        sepRt.sizeDelta = new Vector2(2f, 28f);
        var sepImg = sep.AddComponent<Image>();
        sepImg.color = RowSep;
        sepImg.raycastTarget = false;

        // Label text
        var txtGo = NewUIGO("LabelText", root.transform);
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.sizeDelta = new Vector2(220f, 32f);

        var le = txtGo.AddComponent<LayoutElement>();
        le.flexibleWidth = 1f;

        var tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Tên chức năng";
        tmp.fontSize  = 15f;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Normal;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;

        // Wire NpcMenuItemRow fields
        var so = new SerializedObject(row);
        so.FindProperty("labelText").objectReferenceValue = tmp;
        so.FindProperty("iconImage").objectReferenceValue = iconImg;
        so.ApplyModifiedPropertiesWithoutUndo();

        bool ok = SavePrefab(root, RowPrefabPath);
        Object.DestroyImmediate(root);
        if (ok) Debug.Log($"[CreateNpcMenu] ✓ NpcMenuItemRow → {RowPrefabPath}");
        return ok;
    }

    //  NpcDynamicMenuPanel — panel chính (nền gỗ, tiêu đề, scroll list)
    private static bool CreatePanelPrefab()
    {
        bool overwrite = false;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PanelPrefabPath) != null)
        {
            overwrite = EditorUtility.DisplayDialog(
                "NPC Dynamic Menu Panel",
                $"Prefab đã tồn tại:\n{PanelPrefabPath}\n\nGhi đè?",
                "Ghi đè", "Hủy");
            if (!overwrite) { Debug.Log("[CreateNpcMenu] NpcDynamicMenuPanel đã tồn tại → bỏ qua."); return false; }
        }

        // Root: NpcDynamicMenuPanel
        var root = NewGO("NpcDynamicMenuPanel");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0.5f);
        rootRt.anchorMax = new Vector2(0.5f, 0.5f);
        rootRt.pivot     = new Vector2(0.5f, 0.5f);
        rootRt.sizeDelta = new Vector2(300f, 430f);

        // Canvas Group (cho fade-in nếu cần)
        root.AddComponent<CanvasGroup>();

        // Nền ngoài (viền tối nhất)
        var outerImg = root.AddComponent<Image>();
        outerImg.color = WoodOuter;

        // Viền vàng
        var outerOutline = root.AddComponent<Outline>();
        outerOutline.effectColor    = GoldTrim;
        outerOutline.effectDistance = new Vector2(3f, -3f);

        // Frame gỗ (nền chính)
        var frame = NewUIGO("WoodFrame", root.transform);
        AnchorRect(frame, 0.025f, 0.015f, 0.975f, 0.985f);
        var frameImg = frame.AddComponent<Image>();
        frameImg.color = WoodFrame;

        // Inner gỗ sáng
        var inner = NewUIGO("WoodInner", root.transform);
        AnchorRect(inner, 0.04f, 0.025f, 0.96f, 0.97f);
        var innerImg = inner.AddComponent<Image>();
        innerImg.color = WoodInner;

        // Header bar (tối, tiêu đề NPC)
        var header = NewUIGO("HeaderBar", root.transform);
        AnchorRect(header, 0.04f, 0.88f, 0.96f, 0.97f);
        var headerImg = header.AddComponent<Image>();
        headerImg.color = WoodHeader;
        // Viền vàng nhạt ở đáy header
        var headerOutline = header.AddComponent<Outline>();
        headerOutline.effectColor    = new Color(0.70f, 0.52f, 0.12f, 0.7f);
        headerOutline.effectDistance = new Vector2(0f, -2f);

        // NPC icon placeholder (ảnh NPC nhỏ bên trái title)
        var npcIcon = NewUIGO("NpcIconPlaceholder", header.transform);
        var niRt = npcIcon.GetComponent<RectTransform>();
        niRt.anchorMin = new Vector2(0f, 0.05f);
        niRt.anchorMax = new Vector2(0f, 0.95f);
        niRt.pivot     = new Vector2(0f, 0.5f);
        niRt.offsetMin = new Vector2(6f, 0f);
        niRt.offsetMax = new Vector2(42f, 0f);
        var niImg = npcIcon.AddComponent<Image>();
        niImg.color = new Color(0.62f, 0.47f, 0.20f, 0.6f);
        // Ký hiệu "NPC" nhỏ
        var niLbl = NewUIGO("NpcIconLabel", npcIcon.transform);
        StretchFill(niLbl);
        var niTxt = niLbl.AddComponent<TextMeshProUGUI>();
        niTxt.text      = "NPC";
        niTxt.fontSize  = 9f;
        niTxt.color     = new Color(1f, 1f, 1f, 0.5f);
        niTxt.alignment = TextAlignmentOptions.Center;
        niTxt.raycastTarget = false;

        // TitleText "Xin chào Người chơi"
        var titleGo = NewUIGO("TitleText", header.transform);
        AnchorRect(titleGo, 0f, 0f, 1f, 1f, 48f, 0f, -6f, 0f);
        var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = "Xin chào Người chơi";
        titleTmp.fontSize  = 16f;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color     = GoldText;
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        titleTmp.raycastTarget = false;

        // Đường kẻ phân cách
        var sepLine = NewUIGO("SeparatorLine", root.transform);
        AnchorRect(sepLine, 0.04f, 0.875f, 0.96f, 0.88f);
        var sepImg = sepLine.AddComponent<Image>();
        sepImg.color = GoldTrim;
        sepImg.raycastTarget = false;

        // ScrollView (danh sách menu)
        var sv = NewUIGO("ScrollView", root.transform);
        AnchorRect(sv, 0.04f, 0.10f, 0.96f, 0.875f);
        sv.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        var scrollRect = sv.AddComponent<ScrollRect>();
        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType      = ScrollRect.MovementType.Clamped;

        // Viewport
        var vp = NewUIGO("Viewport", sv.transform);
        StretchFill(vp);
        vp.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        vp.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = vp.GetComponent<RectTransform>();

        // Content (VerticalLayoutGroup + ContentSizeFitter)
        var content = NewUIGO("Content", vp.transform);
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot     = new Vector2(0.5f, 1f);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing           = 2f;
        vlg.padding           = new RectOffset(4, 4, 4, 4);
        vlg.childAlignment    = TextAnchor.UpperCenter;
        vlg.childControlWidth  = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = contentRt;

        // Scrollbar dọc
        var sbGo = NewUIGO("Scrollbar Vertical", sv.transform);
        var sbRt = sbGo.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(1f, 0f);
        sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.pivot     = new Vector2(1f, 0.5f);
        sbRt.offsetMin = new Vector2(-14f, 0f);
        sbRt.offsetMax = Vector2.zero;
        var sbImg = sbGo.AddComponent<Image>();
        sbImg.color = new Color(0.22f, 0.12f, 0.04f, 0.5f);
        var sb = sbGo.AddComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        // SlidingArea > Handle
        var slideArea = NewUIGO("Sliding Area", sbGo.transform);
        StretchFill(slideArea);
        var handle = NewUIGO("Handle", slideArea.transform);
        StretchFill(handle);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = ScrollBar;
        sb.handleRect = handle.GetComponent<RectTransform>();
        sb.targetGraphic = handleImg;
        var sbc = sb.colors;
        sbc.normalColor      = ScrollBar;
        sbc.highlightedColor = new Color(0.55f, 0.33f, 0.10f, 0.90f);
        sb.colors = sbc;
        scrollRect.verticalScrollbar = sb;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // Nút "Cáo từ"
        var btnClose = NewUIGO("BtnClose", root.transform);
        AnchorRect(btnClose, 0.20f, 0.018f, 0.80f, 0.090f);
        var btnImg = btnClose.AddComponent<Image>();
        btnImg.color = WoodBtn;
        // Outline vàng
        var btnOl = btnClose.AddComponent<Outline>();
        btnOl.effectColor    = GoldTrim;
        btnOl.effectDistance = new Vector2(2f, -2f);
        var btnComp = btnClose.AddComponent<Button>();
        var btnCols = btnComp.colors;
        btnCols.normalColor      = WoodBtn;
        btnCols.highlightedColor = WoodBtnHov;
        btnCols.pressedColor     = WoodBtnPrss;
        btnComp.colors = btnCols;
        btnComp.targetGraphic = btnImg;
        // Label "Cáo từ"
        var closeLblGo = NewUIGO("Text", btnClose.transform);
        StretchFill(closeLblGo);
        var closeTmp = closeLblGo.AddComponent<TextMeshProUGUI>();
        closeTmp.text      = "Cáo từ";
        closeTmp.fontSize  = 15f;
        closeTmp.fontStyle = FontStyles.Bold;
        closeTmp.color     = Color.white;
        closeTmp.alignment = TextAlignmentOptions.Center;
        closeTmp.raycastTarget = false;

        // Dấu trang trí (◄ ►) hai bên nút Cáo từ
        string[] decorSymbols = { "◄", "►" };
        float[]  decorX       = { 0.07f, 0.93f };
        for (int d = 0; d < 2; d++)
        {
            var decGo = NewUIGO($"DecorArrow{d}", root.transform);
            AnchorRect(decGo, decorX[d] - 0.06f, 0.025f, decorX[d] + 0.06f, 0.085f);
            var decTmp = decGo.AddComponent<TextMeshProUGUI>();
            decTmp.text      = decorSymbols[d];
            decTmp.fontSize  = 12f;
            decTmp.color     = GoldTrim;
            decTmp.alignment = TextAlignmentOptions.Center;
            decTmp.raycastTarget = false;
        }

        // NpcDynamicMenuUI component + wire fields
        var comp = root.AddComponent<NpcDynamicMenuUI>();
        var so = new SerializedObject(comp);
        so.FindProperty("mainPanel").objectReferenceValue       = root;
        so.FindProperty("titleText").objectReferenceValue       = titleTmp;
        so.FindProperty("menuListContent").objectReferenceValue = contentRt;
        // menuItemRowPrefab — load prefab vừa tạo
        var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RowPrefabPath);
        if (rowPrefab != null)
            so.FindProperty("menuItemRowPrefab").objectReferenceValue = rowPrefab;
        so.FindProperty("btnClose").objectReferenceValue = btnComp;
        so.ApplyModifiedPropertiesWithoutUndo();

        bool ok = SavePrefab(root, PanelPrefabPath);
        Object.DestroyImmediate(root);
        if (ok) Debug.Log($"[CreateNpcMenu] ✓ NpcDynamicMenuPanel → {PanelPrefabPath}");
        return ok;
    }

    // Helpers — layout

    private static void AnchorRect(GameObject go,
        float xMin, float yMin, float xMax, float yMax,
        float offL = 0, float offB = 0, float offR = 0, float offT = 0)
    {
        // Use explicit if-null: Unity's ?? does not detect Unity-null objects
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = new Vector2(offL, offB);
        rt.offsetMax = new Vector2(offR, offT);
    }

    private static void StretchFill(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null) rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static GameObject NewGO(string name) => new GameObject(name);

    // Tạo child UI GameObject với RectTransform được add TRƯỚC khi SetParent.
    // Tránh lỗi MissingComponentException do Unity partial-init RT khi SetParent.
    private static GameObject NewUIGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>(); // MUST be before SetParent
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go;
    }

    // Asset helpers

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static bool SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path, out bool ok);
        return ok;
    }
}
#endif
