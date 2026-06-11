#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Tools > DoAn > [BXH] Tao Skeleton + HUD Button
// Tao panel skeleton (chi neu chua co) + HUD button.
// User tu config UI trong Unity Editor, khong bi overwrite.
// Tools > DoAn > [BXH] Reset Panel (xoa tao lai)
// Xoa va tao lai panel hoan toan tu code (mat config thu cong).
public static class LeaderboardUiBuilder
{
    private const string PanelPath  = "Assets/Resources/Prefabs/UI/LeaderboardPanel.prefab";
    private const string RowPath    = "Assets/Resources/Prefabs/UI/Leaderboard/LeaderboardRowEntry.prefab";
    private const string HudBtnPath = "Assets/Resources/Prefabs/UI/LeaderboardHudButton.prefab";
    private const string NOTO_FONT  = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSans-Regular SDF.asset";

    private static TMP_FontAsset _font;

    // kich thuoc
    private const float W     = 660f, H = 500f;
    private const float TAB1H = 46f;
    private const float TAB2H = 38f;
    private const float HDRH  = 32f;
    private const float DIVH  = 2f;

    // mau
    private static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
    private static readonly Color BgPanel    = C("3D1A06");
    private static readonly Color BgTab1     = C("1C0A02");
    private static readonly Color BgTab2     = C("5A2C0A");
    private static readonly Color BgHdr      = C("2A1004");
    private static readonly Color BgScroll   = C("3A1804");
    private static readonly Color BgEmpty    = C("3D1A06");
    private static readonly Color ColDiv     = C("C07828");
    private static readonly Color ColClose   = C("6E1A0A");
    private static readonly Color ColActMain = C("FFD840");
    private static readonly Color ColNrmMain = C("B87840");
    private static readonly Color ColActSub  = C("FFE050");
    private static readonly Color ColNrmSub  = C("C89858");
    private static readonly Color ColHdr     = C("FFE882");
    private static readonly Color ColVal     = C("78FF78");
    private static readonly Color ColWhite   = Color.white;
    private static readonly Color ColYellow  = C("FFD840");

    // =========================================================================
    // MENU 1 — Skeleton + HUD button (KHONG xoa panel neu da co)
    // =========================================================================
    [MenuItem("Tools/DoAn/[BXH] Tao Skeleton + HUD Button")]
    public static void Build()
    {
        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NOTO_FONT);
        if (_font == null) { /* Cảnh báo: Khong tim thay NotoSans font */ }
        Folder("Assets/Resources/Prefabs/UI/Leaderboard");

        // Row prefab: luon tao moi
        AssetDatabase.DeleteAsset(RowPath);
        MakeRowPrefab();

        // Panel: chi tao neu chua ton tai
        bool panelExists = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath) != null;
        if (!panelExists)
            MakePanelSkeleton();
        else
            { /* Panel da ton tai, giu nguyen. Dung menu Reset neu muon tao lai */ }

        // HUD button: luon tao moi
        AssetDatabase.DeleteAsset(HudBtnPath);
        MakeHudButton();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string msg = panelExists
            ? "HUD Button cap nhat xong.\n\nPanel giu nguyen — ban tu config trong Editor."
            : "Tao xong!\n\nMo Assets/Resources/Prefabs/UI/LeaderboardPanel.prefab\nde config UI thu cong, sau do keo vao Canvas HUD.";
        EditorUtility.DisplayDialog("BXH Builder", msg, "OK");
    }

    // =========================================================================
    // MENU 2 — Reset hoan toan (xoa va tao lai panel tu code)
    // =========================================================================
    [MenuItem("Tools/DoAn/[BXH] Reset Panel (xoa tao lai)")]
    public static void BuildReset()
    {
        if (!EditorUtility.DisplayDialog("Xac nhan Reset",
                "Se XOA va tao lai LeaderboardPanel.prefab!\nMoi config thu cong se mat!",
                "Xoa & Tao lai", "Huy"))
            return;

        _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NOTO_FONT);
        if (_font == null) { /* Cảnh báo: Khong tim thay NotoSans font */ }
        Folder("Assets/Resources/Prefabs/UI/Leaderboard");

        AssetDatabase.DeleteAsset(PanelPath);
        AssetDatabase.DeleteAsset(RowPath);
        AssetDatabase.DeleteAsset(HudBtnPath);

        MakeRowPrefab();
        MakePanelPrefab();
        MakeHudButton();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("BXH Builder", "Reset xong! Panel da duoc tao lai hoan toan.", "OK");
    }

    // =========================================================================
    //  ROW PREFAB
    // =========================================================================
    static void MakeRowPrefab()
    {
        var r = new GameObject("LeaderboardRowEntry");
        SetRect(r, sizeDelta: new Vector2(W - 16f, 40f));
        r.AddComponent<Image>().color = Color.clear;

        var h = r.AddComponent<HorizontalLayoutGroup>();
        h.childForceExpandWidth = false; h.childForceExpandHeight = true;
        h.padding = new RectOffset(6, 6, 0, 0); h.spacing = 2;

        AddCell(r, "RankText",  55f,  TextAlignmentOptions.Center, ColWhite);
        AddCell(r, "NameText",  200f, TextAlignmentOptions.Left,   ColWhite);
        AddCell(r, "ValueText", 70f,  TextAlignmentOptions.Center, ColVal);
        AddCell(r, "ExtraText", 300f, TextAlignmentOptions.Left,   ColNrmSub);

        r.AddComponent<LeaderboardRowEntryUI>();
        PrefabUtility.SaveAsPrefabAsset(r, RowPath);
        Object.DestroyImmediate(r);
    }

    static void AddCell(GameObject parent, string name, float w, TextAlignmentOptions a, Color col)
    {
        var g  = new GameObject(name); g.transform.SetParent(parent.transform, false);
        var le = g.AddComponent<LayoutElement>(); le.preferredWidth = w; le.minWidth = w;
        var t  = g.AddComponent<TextMeshProUGUI>(); t.fontSize = 17; t.color = col; t.alignment = a;
        if (_font != null) t.font = _font;
    }

    // =========================================================================
    //  PANEL SKELETON — chi tao root + component, khong co child
    //  User tu them child va wire SerializedFields trong Inspector.
    //
    //  Cac field can wire trong LeaderboardPanelUI:
    //    mainTabs[4]        — 4 Button (MainTabBar)
    //    subTabs[5]         — 5 Button (SubTabBar)
    //    contentGroup       — GameObject bao gom SubTabBar+Header+Scroll
    //    emptyStateGroup    — GameObject hien khi tab 1/2/3
    //    emptyStateText     — TMP_Text trong emptyStateGroup
    //    headerCells[4]     — 4 TMP_Text header (Hang|Ten|Gia tri|Thong tin)
    //    rowContent         — RectTransform Content cua ScrollView
    //    loadingText        — TMP_Text "Dang tai..."
    //    closeButton        — Button close
    //    rowPrefab          — LeaderboardRowEntry prefab
    // =========================================================================
    static void MakePanelSkeleton()
    {
        var root = new GameObject("LeaderboardPanel");
        SetRect(root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(W, H), Vector2.zero);
        root.AddComponent<Image>().color = BgPanel;
        root.AddComponent<CanvasGroup>();
        root.AddComponent<LeaderboardPanelUI>();

        var saved = PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = saved;
        { /* Skeleton saved */ }
    }

    // =========================================================================
    //  PANEL PREFAB (day du — dung cho Reset)
    // =========================================================================
    static void MakePanelPrefab()
    {
        // Root
        var root = new GameObject("LeaderboardPanel");
        SetRect(root, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(W, H), Vector2.zero);
        root.AddComponent<Image>().color = BgPanel;
        root.AddComponent<CanvasGroup>();
        var panel = root.AddComponent<LeaderboardPanelUI>();

        // Main Tab Bar
        var tab1 = Stripe(root, "MainTabBar", 0, TAB1H);
        tab1.AddComponent<Image>().color = BgTab1;
        var hlg1 = tab1.AddComponent<HorizontalLayoutGroup>();
        hlg1.childForceExpandWidth = true; hlg1.childForceExpandHeight = true;
        hlg1.padding = new RectOffset(2, 2, 3, 3); hlg1.spacing = 2;

        string[] m = { "Dua Top", "Su kien", "Tuan&Thang", "Thuong" };
        var mainBtns = new Button[4];
        for (int i = 0; i < 4; i++)
            mainBtns[i] = TabBtn(tab1, m[i], i == 0 ? ColActMain : ColNrmMain, BgTab1, 15);

        // Sub Tab Bar
        var tab2 = Stripe(root, "SubTabBar", TAB1H, TAB2H);
        tab2.AddComponent<Image>().color = BgTab2;
        var hlg2 = tab2.AddComponent<HorizontalLayoutGroup>();
        hlg2.childForceExpandWidth = true; hlg2.childForceExpandHeight = true;
        hlg2.padding = new RectOffset(3, 3, 3, 3); hlg2.spacing = 2;

        string[] s = { "Cao thu", "Nap vang", "Hoa chi", "Chuyen can", "Gia toc" };
        var subBtns = new Button[5];
        for (int i = 0; i < 5; i++)
            subBtns[i] = TabBtn(tab2, s[i], i == 0 ? ColActSub : ColNrmSub, BgTab2, 13);

        // Header Row
        var hdr = Stripe(root, "HeaderRow", TAB1H + TAB2H, HDRH);
        hdr.AddComponent<Image>().color = BgHdr;
        var hhlg = hdr.AddComponent<HorizontalLayoutGroup>();
        hhlg.childForceExpandWidth = false; hhlg.childForceExpandHeight = true;
        hhlg.padding = new RectOffset(6, 6, 0, 0); hhlg.spacing = 2;

        (string l, float w)[] hds = { ("Hang", 55), ("Ten", 200), ("Cap", 70), ("Thong tin", 300) };
        var hdrCells = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var g  = new GameObject($"H{i}"); g.transform.SetParent(hdr.transform, false);
            var le = g.AddComponent<LayoutElement>(); le.preferredWidth = hds[i].w; le.minWidth = hds[i].w;
            var t  = g.AddComponent<TextMeshProUGUI>();
            t.text = hds[i].l; t.fontSize = 14; t.fontStyle = FontStyles.Bold;
            t.color = ColHdr; t.alignment = i == 0 ? TextAlignmentOptions.Center : TextAlignmentOptions.Left;
            if (_font != null) t.font = _font;
            hdrCells[i] = t;
        }

        // Divider
        var div = Stripe(root, "Divider", TAB1H + TAB2H + HDRH, DIVH);
        div.AddComponent<Image>().color = ColDiv;

        // Content Group
        var cg = new GameObject("ContentGroup"); cg.transform.SetParent(root.transform, false);
        SetRect(cg, Vector2.zero, Vector2.one, new Vector2(0, .5f), Vector2.zero, Vector2.zero);

        tab2.transform.SetParent(cg.transform, false);
        hdr.transform.SetParent(cg.transform, false);
        div.transform.SetParent(cg.transform, false);
        SetStripe(tab2, 0,               TAB2H);
        SetStripe(hdr,  TAB2H,           HDRH);
        SetStripe(div,  TAB2H + HDRH,    DIVH);

        // Scroll View
        var sv   = new GameObject("ScrollView"); sv.transform.SetParent(cg.transform, false);
        var svRt = sv.AddComponent<RectTransform>();
        svRt.anchorMin = Vector2.zero; svRt.anchorMax = Vector2.one;
        svRt.offsetMin = Vector2.zero;
        svRt.offsetMax = new Vector2(0, -(TAB2H + HDRH + DIVH));
        sv.AddComponent<Image>().color = BgScroll;

        var sr = sv.AddComponent<ScrollRect>();
        sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 40;

        var vp = new GameObject("Viewport"); vp.transform.SetParent(sv.transform, false);
        FullStretch(vp);
        vp.AddComponent<RectMask2D>();
        sr.viewport = vp.GetComponent<RectTransform>();

        var ct   = new GameObject("Content"); ct.transform.SetParent(vp.transform, false);
        var ctRt = ct.AddComponent<RectTransform>();
        ctRt.anchorMin = new Vector2(0, 1); ctRt.anchorMax = new Vector2(1, 1);
        ctRt.pivot = new Vector2(0.5f, 1); ctRt.sizeDelta = Vector2.zero; ctRt.anchoredPosition = Vector2.zero;
        var cvlg = ct.AddComponent<VerticalLayoutGroup>();
        cvlg.childForceExpandWidth = true; cvlg.childForceExpandHeight = false; cvlg.spacing = 1;
        var csf = ct.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = ctRt;

        // Empty State
        var empty = new GameObject("EmptyStateGroup"); empty.transform.SetParent(root.transform, false);
        var eRt   = empty.AddComponent<RectTransform>();
        eRt.anchorMin = Vector2.zero; eRt.anchorMax = Vector2.one;
        eRt.offsetMin = Vector2.zero; eRt.offsetMax = new Vector2(0, -TAB1H);
        empty.AddComponent<Image>().color = BgEmpty;
        empty.SetActive(false);

        var eTxt = new GameObject("EmptyText"); eTxt.transform.SetParent(empty.transform, false);
        FullStretch(eTxt);
        var eTmp = eTxt.AddComponent<TextMeshProUGUI>();
        eTmp.text = "Su kien dua top nay chua mo";
        eTmp.fontSize = 22; eTmp.color = ColYellow;
        eTmp.alignment = TextAlignmentOptions.Center; eTmp.fontStyle = FontStyles.Bold;
        if (_font != null) eTmp.font = _font;

        // Loading Text
        var loadGO = new GameObject("LoadingText"); loadGO.transform.SetParent(root.transform, false);
        var lRt    = loadGO.AddComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero; lRt.anchorMax = Vector2.one;
        lRt.offsetMin = Vector2.zero; lRt.offsetMax = new Vector2(0, -TAB1H);
        var lTmp = loadGO.AddComponent<TextMeshProUGUI>();
        lTmp.text = "Dang tai..."; lTmp.fontSize = 20;
        lTmp.color = ColYellow; lTmp.alignment = TextAlignmentOptions.Center;
        if (_font != null) lTmp.font = _font;
        loadGO.SetActive(false);

        // Close Button
        var closeGO = new GameObject("CloseButton"); closeGO.transform.SetParent(root.transform, false);
        var cRt     = closeGO.AddComponent<RectTransform>();
        cRt.anchorMin = cRt.anchorMax = new Vector2(1, 1); cRt.pivot = new Vector2(1, 1);
        cRt.sizeDelta = new Vector2(34, 34); cRt.anchoredPosition = new Vector2(-2, -2);
        closeGO.AddComponent<Image>().color = ColClose;
        var closeBtn = closeGO.AddComponent<Button>();
        var cn = closeBtn.navigation; cn.mode = Navigation.Mode.None; closeBtn.navigation = cn;
        var cLbl = new GameObject("X"); cLbl.transform.SetParent(closeGO.transform, false);
        FullStretch(cLbl);
        var cT = cLbl.AddComponent<TextMeshProUGUI>();
        cT.text = "X"; cT.fontSize = 18; cT.color = ColWhite; cT.alignment = TextAlignmentOptions.Center;
        if (_font != null) cT.font = _font;

        // Wire
        var so = new SerializedObject(panel);
        SetArr(so, "mainTabs",        mainBtns);
        SetArr(so, "subTabs",         subBtns);
        SetProp(so, "contentGroup",    cg);
        SetProp(so, "emptyStateGroup", empty);
        SetProp(so, "emptyStateText",  eTmp);
        SetArr(so, "headerCells",     hdrCells);
        SetProp(so, "rowContent",     ctRt);
        SetProp(so, "loadingText",    lTmp);
        SetProp(so, "closeButton",    closeBtn);
        var rowAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RowPath);
        if (rowAsset) SetProp(so, "rowPrefab", rowAsset);
        so.ApplyModifiedPropertiesWithoutUndo();

        var saved = PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
        Object.DestroyImmediate(root);
        Selection.activeObject = saved;
    }

    // =========================================================================
    //  HUD BUTTON PREFAB
    // =========================================================================
    static void MakeHudButton()
    {
        var g = new GameObject("LeaderboardHudButton");
        SetRect(g, new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0),
                new Vector2(70, 70), new Vector2(-10, 10));
        g.AddComponent<Image>().color = C("5A2800");
        var btn = g.AddComponent<Button>();
        var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
        g.AddComponent<LeaderboardToggleButton>();

        var lbl = new GameObject("Label"); lbl.transform.SetParent(g.transform, false);
        FullStretch(lbl);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = "BXH"; t.fontSize = 16; t.color = ColYellow;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        if (_font != null) t.font = _font;

        var sub = new GameObject("Sub"); sub.transform.SetParent(g.transform, false);
        var sRt = sub.AddComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0, 0); sRt.anchorMax = new Vector2(1, 0);
        sRt.pivot = new Vector2(.5f, 0); sRt.sizeDelta = new Vector2(0, 20); sRt.anchoredPosition = new Vector2(0, 4);
        var st = sub.AddComponent<TextMeshProUGUI>();
        st.text = "Bang XH"; st.fontSize = 9; st.color = ColNrmSub; st.alignment = TextAlignmentOptions.Center;
        if (_font != null) st.font = _font;

        var saved = PrefabUtility.SaveAsPrefabAsset(g, HudBtnPath);
        Object.DestroyImmediate(g);

        var canvas = Object.FindObjectOfType<Canvas>();
        if (canvas != null)
        {
            var inst = PrefabUtility.InstantiatePrefab(saved, canvas.transform) as GameObject;
            if (inst != null)
            {
                var irt = inst.GetComponent<RectTransform>();
                irt.anchorMin = irt.anchorMax = new Vector2(1, 0);
                irt.pivot = new Vector2(1, 0); irt.anchoredPosition = new Vector2(-10, 10);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }
        }
    }

    // =========================================================================
    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.
    // =========================================================================
    static GameObject Stripe(GameObject parent, string name, float yOffset, float height)
    {
        var g = new GameObject(name); g.transform.SetParent(parent.transform, false);
        SetStripe(g, yOffset, height);
        return g;
    }

    static void SetStripe(GameObject g, float yOffset, float height)
    {
        var rt = g.GetComponent<RectTransform>();
        if (rt == null) rt = g.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(.5f, 1);
        rt.sizeDelta = new Vector2(0, height);
        rt.anchoredPosition = new Vector2(0, -yOffset);
    }

    static Button TabBtn(GameObject parent, string label, Color textColor, Color bgColor, int fs)
    {
        var nm = label.Replace(" ", "").Replace("&", "");
        var g  = new GameObject(nm); g.transform.SetParent(parent.transform, false);
        g.AddComponent<Image>().color = bgColor;
        var btn = g.AddComponent<Button>();
        var nav = btn.navigation; nav.mode = Navigation.Mode.None; btn.navigation = nav;
        var lbl = new GameObject("L"); lbl.transform.SetParent(g.transform, false);
        FullStretch(lbl);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = fs; t.color = textColor;
        t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.Center;
        if (_font != null) t.font = _font;
        return btn;
    }

    static void FullStretch(GameObject g)
    {
        var rt = g.GetComponent<RectTransform>();
        if (rt == null) rt = g.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void SetRect(GameObject g,
        Vector2 ancMin, Vector2 ancMax, Vector2 piv, Vector2 size, Vector2 pos)
    {
        var rt = g.GetComponent<RectTransform>();
        if (rt == null) rt = g.AddComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = piv;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
    }

    static void SetRect(GameObject g, Vector2 sizeDelta = default)
    {
        var rt = g.GetComponent<RectTransform>();
        if (rt == null) rt = g.AddComponent<RectTransform>();
        rt.sizeDelta = sizeDelta;
    }

    static void SetProp(SerializedObject so, string name, Object val)
    {
        var p = so.FindProperty(name);
        if (p != null) p.objectReferenceValue = val;
        else { /* Cảnh báo: prop not found */ }
    }

    static void SetArr<T>(SerializedObject so, string name, T[] arr) where T : Object
    {
        var p = so.FindProperty(name);
        if (p == null) { { /* Cảnh báo: arr prop not found */ } return; }
        p.arraySize = arr.Length;
        for (int i = 0; i < arr.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = arr[i];
    }

    static void Folder(string path)
    {
        var parts = path.Split('/'); var cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
#endif
