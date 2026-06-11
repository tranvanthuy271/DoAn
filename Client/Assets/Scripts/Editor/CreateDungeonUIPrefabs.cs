#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Editor tool — tạo tự động các prefab UI phó bản + thông báo toàn cục.
// Chạy từ menu Unity:
// Tools ▸ Create Dungeon UI Prefabs
// Visual style khớp 3 ảnh mẫu:
// Panel 1 (list) + Panel 2 (confirm): nền gỗ, chữ trắng, icon bubble, nút "Cáo từ"
// Panel 3 (notification): viền vàng, title bar vàng "Nhắc nhở", nút cam "Xác nhận"
public static class CreateDungeonUIPrefabs
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string ResourcesUiPrefabFolder = "Assets/Resources/Prefabs/UI";

    // Màu gỗ (panel 1 & 2) — khớp ảnh mẫu
    private static readonly Color WoodOuter   = new Color(0.36f, 0.20f, 0.07f, 1f); // viền ngoài tối
    private static readonly Color WoodInner   = new Color(0.62f, 0.37f, 0.12f, 1f); // nền gỗ sáng
    private static readonly Color WoodHeader  = new Color(0.44f, 0.26f, 0.09f, 1f); // header gỗ tối hơn
    private static readonly Color WoodButton  = new Color(0.55f, 0.32f, 0.08f, 1f); // nút "Cáo từ"
    private static readonly Color WoodBtnHover= new Color(0.70f, 0.42f, 0.12f, 1f);

    // Màu thông báo (panel 3) — khớp ảnh mẫu
    private static readonly Color NotifOuter  = new Color(0.25f, 0.15f, 0.04f, 0.97f);
    private static readonly Color NotifTitle  = new Color(0.55f, 0.42f, 0.05f, 1f); // thanh vàng
    private static readonly Color NotifBody   = new Color(0.35f, 0.20f, 0.06f, 1f); // nội dung nâu đậm
    private static readonly Color NotifBtn    = new Color(0.80f, 0.38f, 0.05f, 1f); // cam
    private static readonly Color NotifBtnHov = new Color(0.95f, 0.50f, 0.08f, 1f);
    private static readonly Color Gold        = new Color(1.00f, 0.90f, 0.40f, 1f);

    [MenuItem("Tools/Create Dungeon UI Prefabs")]
    public static void CreateAll()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(ResourcesUiPrefabFolder);

        bool any = false;
        any |= CreateDungeonNpcMenuPanel();
        any |= CreateDungeonNpcMenuEntryPrefab();
        any |= CreateGlobalNotificationPanel();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Tạo prefab UI phó bản",
            any ? "Hoàn tất! Kéo prefab vào Canvas trong scene."
                : "Tất cả prefab đã tồn tại, không có gì thay đổi.",
            "OK");
    }

    // 1. DungeonNpcMenuPanel (list + confirm — gỗ)
    private static bool CreateDungeonNpcMenuPanel()
    {
        const string path = PrefabFolder + "/DungeonNpcMenuPanel.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        { Debug.Log("[CreateDungeonUI] DungeonNpcMenuPanel đã tồn tại → bỏ qua."); return false; }

        var root = NewGO("DungeonNpcMenuPanel");
        SizeRect(root, 340, 500);
        root.AddComponent<DungeonNpcMenuUI>();

        // ListPanel
        var lp = WoodPanel(root.transform, "ListPanel", 340, 500);

        // X button (đóng) góc trên phải
        var btnX = CloseXButton(lp.transform);

        // Greeting text
        var greeting = TMPLabel(lp.transform, "GreetingText", "Xin chào Người chơi", 17, false);
        AnchorRect(greeting, 0.04f, 0.86f, 0.86f, 0.96f);
        greeting.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        // Scroll view (danh sách phó bản)
        var scroll = WoodScrollView(lp.transform, "DungeonScrollView",
            new Vector2(0f, 0.09f), new Vector2(1f, 0.85f));

        // Nút "Cáo từ" — góc dưới phải
        var btnClose = WoodBtn(lp.transform, "BtnCloseList", "Cáo từ",
            new Vector2(0.52f, 0.01f), new Vector2(0.96f, 0.08f));

        // ConfirmPanel
        var cp = WoodPanel(root.transform, "ConfirmPanel", 340, 500);
        cp.SetActive(false);

        // X button
        CloseXButton(cp.transform);

        // Info text (giữa panel)
        var confirmInfo = TMPLabel(cp.transform, "ConfirmInfoText",
            "Hãy tập hợp tất cả đồng đội trong nhóm tại đây", 16, false);
        AnchorRect(confirmInfo, 0.06f, 0.60f, 0.94f, 0.90f);
        var ct = confirmInfo.GetComponent<TextMeshProUGUI>();
        ct.alignment = TextAlignmentOptions.Center;
        ct.enableWordWrapping = true;

        // Option root (vertical — sẽ chứa row "Tham gia" động)
        var optRoot = NewGO("ConfirmOptionRoot");
        optRoot.transform.SetParent(cp.transform, false);
        AnchorRect(optRoot, 0.04f, 0.38f, 0.96f, 0.58f);
        var vlg = optRoot.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 6; vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childAlignment = TextAnchor.UpperCenter;
        optRoot.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Nút "Tham gia" cố định (tùy chọn, inspector có thể dùng confirmOptionRoot thay thế)
        var btnJoin = WoodBtn(cp.transform, "BtnConfirmJoin", "Tham gia",
            new Vector2(0.04f, 0.25f), new Vector2(0.96f, 0.34f),
            new Color(0.25f, 0.45f, 0.12f), new Color(0.35f, 0.58f, 0.16f));

        // Nút "Cáo từ"
        var btnBack = WoodBtn(cp.transform, "BtnBackToList", "Cáo từ",
            new Vector2(0.52f, 0.01f), new Vector2(0.96f, 0.08f));

        bool ok = SavePrefab(root, path);
        Object.DestroyImmediate(root);
        if (ok) Debug.Log($"[CreateDungeonUI] ✓ DungeonNpcMenuPanel → {path}");
        return ok;
    }

    // 2. DungeonNpcMenuEntryPrefab (row — gỗ, icon bubble + tên)
    private static bool CreateDungeonNpcMenuEntryPrefab()
    {
        const string path = PrefabFolder + "/DungeonNpcMenuEntryPrefab.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        { Debug.Log("[CreateDungeonUI] DungeonNpcMenuEntryPrefab đã tồn tại → bỏ qua."); return false; }

        var root = NewGO("DungeonNpcMenuEntryPrefab");
        var rt = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(310, 46);
        root.AddComponent<DungeonNpcMenuEntryUI>();

        // Row background (trong suốt khi không hover — gỗ nhạt)
        var rowImg = root.AddComponent<Image>();
        rowImg.color = new Color(0.50f, 0.30f, 0.10f, 0f); // ẩn mặc định
        var btn = root.AddComponent<Button>();
        var bc = btn.colors;
        bc.normalColor      = new Color(0.50f, 0.30f, 0.10f, 0f);
        bc.highlightedColor = new Color(0.70f, 0.45f, 0.15f, 0.6f);
        bc.pressedColor     = new Color(0.85f, 0.55f, 0.18f, 0.8f);
        bc.selectedColor    = new Color(0.70f, 0.45f, 0.15f, 0.6f);
        btn.colors = bc;

        // Chat bubble icon (tròn — màu trắng/xám nhạt, khớp ảnh)
        var iconGO = NewGO("ChatBubbleIcon");
        iconGO.transform.SetParent(root.transform, false);
        var ir = iconGO.AddComponent<RectTransform>();
        ir.anchorMin = new Vector2(0f, 0.1f);
        ir.anchorMax = new Vector2(0f, 0.9f);
        ir.pivot     = new Vector2(0f, 0.5f);
        ir.offsetMin = new Vector2(10, 0);
        ir.offsetMax = new Vector2(46, 0);
        var iconImg = iconGO.AddComponent<Image>();
        iconImg.color = new Color(0.85f, 0.85f, 0.85f, 1f);
        iconImg.raycastTarget = false;

        // Inner dot (bubble detail)
        var dotGO = NewGO("BubbleDot");
        dotGO.transform.SetParent(iconGO.transform, false);
        var dr = dotGO.AddComponent<RectTransform>();
        dr.anchorMin = new Vector2(0.2f, 0.15f);
        dr.anchorMax = new Vector2(0.8f, 0.65f);
        dr.offsetMin = Vector2.zero; dr.offsetMax = Vector2.zero;
        var dotImg = dotGO.AddComponent<Image>();
        dotImg.color = new Color(0.35f, 0.22f, 0.08f, 1f);
        dotImg.raycastTarget = false;

        // Dungeon name text
        var txtGO = NewGO("DungeonNameText");
        txtGO.transform.SetParent(root.transform, false);
        var tr = txtGO.AddComponent<RectTransform>();
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(1f, 1f);
        tr.offsetMin = new Vector2(54, 4);
        tr.offsetMax = new Vector2(-6, -4);
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = "Tên Phó Bản";
        tmp.fontSize  = 16;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;

        bool ok = SavePrefab(root, path);
        Object.DestroyImmediate(root);
        if (ok) Debug.Log($"[CreateDungeonUI] ✓ DungeonNpcMenuEntryPrefab → {path}");
        return ok;
    }

    // 3. GlobalNotificationPanel (nhắc nhở — viền vàng, titlebar vàng)
    private static bool CreateGlobalNotificationPanel()
    {
        const string path = ResourcesUiPrefabFolder + "/GlobalNotificationPanel.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        { Debug.Log("[CreateDungeonUI] GlobalNotificationPanel đã tồn tại → bỏ qua."); return false; }

        var root = NewGO("GlobalNotificationPanel");
        root.AddComponent<RectTransform>().sizeDelta = new Vector2(360, 240);
        root.AddComponent<GlobalNotificationUI>();

        // Outer panel (tối, viền vàng)
        var panel = NewGO("Panel");
        panel.transform.SetParent(root.transform, false);
        StretchFill(panel);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = NotifOuter;
        var outline = panel.AddComponent<Outline>();
        outline.effectColor    = Gold;
        outline.effectDistance = new Vector2(3, -3);

        // Title bar (vàng, bold)
        var titleBar = NewGO("TitleBar");
        titleBar.transform.SetParent(panel.transform, false);
        var tbr = titleBar.AddComponent<RectTransform>();
        tbr.anchorMin = new Vector2(0f, 0.78f);
        tbr.anchorMax = new Vector2(1f, 1.00f);
        tbr.offsetMin = Vector2.zero;
        tbr.offsetMax = Vector2.zero;
        var tbImg = titleBar.AddComponent<Image>();
        tbImg.color = NotifTitle;

        // TitleText (trên thanh vàng)
        var titleGO = NewGO("TitleText");
        titleGO.transform.SetParent(titleBar.transform, false);
        StretchFill(titleGO);
        var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "Nhắc nhở";
        titleTMP.fontSize  = 20;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = Gold;
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.raycastTarget = false;

        // Body background
        var body = NewGO("BodyArea");
        body.transform.SetParent(panel.transform, false);
        AnchorRect(body, 0.04f, 0.22f, 0.96f, 0.77f);
        var bodyImg = body.AddComponent<Image>();
        bodyImg.color = NotifBody;
        var bodyOutline = body.AddComponent<Outline>();
        bodyOutline.effectColor    = new Color(0.60f, 0.45f, 0.05f, 1f);
        bodyOutline.effectDistance = new Vector2(2, -2);

        // MessageText (trong body)
        var msgGO = NewGO("MessageText");
        msgGO.transform.SetParent(body.transform, false);
        StretchFill(msgGO);
        var msgTMP = msgGO.AddComponent<TextMeshProUGUI>();
        msgTMP.text      = "Nội dung thông báo.";
        msgTMP.fontSize  = 16;
        msgTMP.color     = Color.white;
        msgTMP.alignment = TextAlignmentOptions.Center;
        msgTMP.enableWordWrapping = true;
        msgTMP.margin    = new Vector4(12, 8, 12, 8);
        msgTMP.raycastTarget = false;

        // Xác nhận button (cam)
        var btnGO = NewGO("BtnOk");
        btnGO.transform.SetParent(panel.transform, false);
        AnchorRect(btnGO, 0.22f, 0.04f, 0.78f, 0.19f);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = NotifBtn;
        var btn = btnGO.AddComponent<Button>();
        var bc = btn.colors;
        bc.normalColor      = NotifBtn;
        bc.highlightedColor = NotifBtnHov;
        bc.pressedColor     = new Color(0.65f, 0.30f, 0.02f, 1f);
        btn.colors = bc;
        // Outline cam
        var btnOutline = btnGO.AddComponent<Outline>();
        btnOutline.effectColor    = new Color(1f, 0.75f, 0.20f, 1f);
        btnOutline.effectDistance = new Vector2(2, -2);

        // Label "Xác nhận"
        var lblGO = NewGO("Text");
        lblGO.transform.SetParent(btnGO.transform, false);
        StretchFill(lblGO);
        var lblTMP = lblGO.AddComponent<TextMeshProUGUI>();
        lblTMP.text      = "Xác nhận";
        lblTMP.fontSize  = 17;
        lblTMP.fontStyle = FontStyles.Bold;
        lblTMP.color     = Color.white;
        lblTMP.alignment = TextAlignmentOptions.Center;
        lblTMP.raycastTarget = false;

        bool ok = SavePrefab(root, path);
        Object.DestroyImmediate(root);
        if (ok) Debug.Log($"[CreateDungeonUI] ✓ GlobalNotificationPanel → {path}");
        return ok;
    }

    // Helpers — UI

    // Tạo panel nền gỗ với viền và close-X (ảnh 1 & 2)
    private static GameObject WoodPanel(Transform parent, string name, float w, float h)
    {
        var go = NewGO(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Outer border
        var bg = go.AddComponent<Image>();
        bg.color = WoodOuter;

        // Inner wood
        var inner = NewGO("WoodInner");
        inner.transform.SetParent(go.transform, false);
        AnchorRect(inner, 0.02f, 0.02f, 0.98f, 0.98f);
        inner.AddComponent<Image>().color = WoodInner;

        // Top header strip (tối hơn)
        var header = NewGO("HeaderStrip");
        header.transform.SetParent(go.transform, false);
        AnchorRect(header, 0.02f, 0.90f, 0.98f, 0.98f);
        header.AddComponent<Image>().color = WoodHeader;

        return go;
    }

    // Nút chữ X góc trên phải (đóng panel)
    private static Button CloseXButton(Transform parent)
    {
        var go = NewGO("BtnClose_X");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.88f, 0.91f);
        rt.anchorMax = new Vector2(0.98f, 0.99f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.70f, 0.18f, 0.10f, 1f);
        var btn = go.AddComponent<Button>();
        var bc = btn.colors;
        bc.highlightedColor = new Color(0.90f, 0.25f, 0.15f, 1f);
        btn.colors = bc;

        var lbl = NewGO("X");
        lbl.transform.SetParent(go.transform, false);
        StretchFill(lbl);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = "×"; t.fontSize = 20; t.fontStyle = FontStyles.Bold;
        t.color = Color.white; t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return btn;
    }

    // Scroll view kiểu gỗ (trong suốt, nội dung cuộn dọc)
    private static GameObject WoodScrollView(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var sv = NewGO(name);
        sv.transform.SetParent(parent, false);
        AnchorRect(sv, anchorMin.x, anchorMin.y, anchorMax.x, anchorMax.y);
        var img = sv.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        var scrollRect = sv.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical   = true;
        scrollRect.scrollSensitivity = 30;

        var vp = NewGO("Viewport");
        vp.transform.SetParent(sv.transform, false);
        StretchFill(vp);
        vp.AddComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        vp.AddComponent<Mask>().showMaskGraphic = false;
        scrollRect.viewport = vp.GetComponent<RectTransform>();

        var content = NewGO("Content");
        content.transform.SetParent(vp.transform, false);
        var cr = content.AddComponent<RectTransform>();
        cr.anchorMin = new Vector2(0, 1);
        cr.anchorMax = new Vector2(1, 1);
        cr.pivot     = new Vector2(0.5f, 1);
        cr.offsetMin = Vector2.zero;
        cr.offsetMax = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 4; vlg.padding = new RectOffset(6, 6, 6, 6);
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scrollRect.content = cr;

        return sv;
    }

    // Nút kiểu gỗ
    private static Button WoodBtn(Transform parent, string name, string label,
        Vector2 ancMin, Vector2 ancMax,
        Color? normalColor = null, Color? hoverColor = null)
    {
        var go = NewGO(name);
        go.transform.SetParent(parent, false);
        AnchorRect(go, ancMin.x, ancMin.y, ancMax.x, ancMax.y);
        var img = go.AddComponent<Image>();
        img.color = normalColor ?? WoodButton;
        var btn = go.AddComponent<Button>();
        var bc = btn.colors;
        bc.normalColor      = normalColor ?? WoodButton;
        bc.highlightedColor = hoverColor  ?? WoodBtnHover;
        bc.pressedColor     = new Color(0.40f, 0.22f, 0.05f);
        btn.colors = bc;
        // Outline vàng nhạt
        var ol = go.AddComponent<Outline>();
        ol.effectColor    = new Color(0.90f, 0.70f, 0.25f, 0.8f);
        ol.effectDistance = new Vector2(2, -2);

        var lbl = NewGO("Text");
        lbl.transform.SetParent(go.transform, false);
        StretchFill(lbl);
        var t = lbl.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 16; t.fontStyle = FontStyles.Bold;
        t.color = Color.white; t.alignment = TextAlignmentOptions.Center;
        t.raycastTarget = false;
        return btn;
    }

    // Rect helpers

    private static GameObject TMPLabel(Transform parent, string name, string text,
        float size, bool bold = false)
    {
        var go = NewGO(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = Color.white;
        t.alignment = TextAlignmentOptions.MidlineLeft;
        if (bold) t.fontStyle = FontStyles.Bold;
        t.raycastTarget = false;
        return go;
    }

    private static void AnchorRect(GameObject go,
        float xMin, float yMin, float xMax, float yMax,
        float offL = 0, float offB = 0, float offR = 0, float offT = 0)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(xMin, yMin);
        rt.anchorMax = new Vector2(xMax, yMax);
        rt.offsetMin = new Vector2(offL, offB);
        rt.offsetMax = new Vector2(offR, offT);
    }

    private static void StretchFill(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SizeRect(GameObject go, float w, float h)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
    }

    private static GameObject NewGO(string name)
    {
        var go = new GameObject(name);
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
