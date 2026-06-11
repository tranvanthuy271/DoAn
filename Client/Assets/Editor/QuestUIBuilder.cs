#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor tool: tự động tạo toàn bộ Canvas UI cho hệ thống nhiệm vụ.
// Menu: DoAn → Quest → Create All Quest UI
public static class QuestUIBuilder
{
    private const string PREFAB_DIR = "Assets/Resources/UI";

    //  Entry Points

    [MenuItem("DoAn/Quest/Create All Quest UI")]
    public static void CreateAll()
    {
        EnsureDir(PREFAB_DIR);
        CreateQuestDialogueUI();
        CreateQuestNpcPanel();
        CreateQuestHudWidget();
        CreateQuestListItemPrefab();
        AssetDatabase.Refresh();
        Debug.Log("[QuestUIBuilder] ✓ Đã tạo toàn bộ Quest UI. Xem hướng dẫn trong Console.");
        PrintGuide();
    }

    [MenuItem("DoAn/Quest/Create Quest Dialogue UI")]
    public static void CreateQuestDialogueUI()
    {
        EnsureDir(PREFAB_DIR);
        var go = BuildDialogueCanvas();
        SavePrefab(go, PREFAB_DIR, "QuestDialogueUI");
        Debug.Log("[QuestUIBuilder] ✓ QuestDialogueUI prefab tạo xong.");
    }

    [MenuItem("DoAn/Quest/Create Quest NPC Panel")]
    public static void CreateQuestNpcPanel()
    {
        EnsureDir(PREFAB_DIR);
        var go = BuildNpcPanel();
        SavePrefab(go, PREFAB_DIR, "QuestNpcPanel");
        Debug.Log("[QuestUIBuilder] ✓ QuestNpcPanel prefab tạo xong.");
    }

    [MenuItem("DoAn/Quest/Create Quest HUD Widget")]
    public static void CreateQuestHudWidget()
    {
        EnsureDir(PREFAB_DIR);
        var go = BuildHudWidget();
        SavePrefab(go, PREFAB_DIR, "QuestHudWidget");
        Debug.Log("[QuestUIBuilder] ✓ QuestHudWidget prefab tạo xong.");
    }

    [MenuItem("DoAn/Quest/Create Quest List Item Prefab")]
    public static void CreateQuestListItemPrefab()
    {
        EnsureDir(PREFAB_DIR + "/Quest");
        var go = BuildQuestListItem();
        SavePrefab(go, PREFAB_DIR + "/Quest", "QuestListItem");
        Debug.Log("[QuestUIBuilder] ✓ QuestListItem prefab tạo xong.");
    }

    //  QuestDialogueUI Canvas
    //  Full-screen dark overlay + bottom dialogue box + NPC portrait

    private static GameObject BuildDialogueCanvas()
    {
        // Root: Canvas
        var root   = new GameObject("QuestDialogueUI");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // Overlay: full-screen dark image
        var overlay    = MakeImage(root, "Overlay", new Color(0, 0, 0, 0.75f));
        SetStretch(overlay);

        // BlockInput: transparent raycast blocker so clicks don't bleed through
        var blocker  = MakeImage(overlay, "BlockInput", Color.clear);
        blocker.GetComponent<Image>().raycastTarget = true;
        SetStretch(blocker);

        // DialoguePanel: bottom strip, dark wood tone
        var panel = MakeImage(overlay, "DialoguePanel", new Color(0.12f, 0.08f, 0.04f, 0.96f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot     = new Vector2(0.5f, 0);
        panelRect.offsetMin = new Vector2(10,  10);
        panelRect.offsetMax = new Vector2(-10, 220); // height ≈ 210px

        // NPC Portrait — left circle
        var portrait = MakeImage(panel, "NpcPortrait", new Color(0.35f, 0.30f, 0.25f));
        var portRect  = portrait.GetComponent<RectTransform>();
        portRect.anchorMin        = new Vector2(0, 0.5f);
        portRect.anchorMax        = new Vector2(0, 0.5f);
        portRect.pivot            = new Vector2(0.5f, 0.5f);
        portRect.anchoredPosition = new Vector2(80, 20);
        portRect.sizeDelta        = new Vector2(130, 130);

        // NPC Name above portrait
        var npcName = MakeTMPText(panel, "NpcName", "NPC", 16, new Color(1f, 0.85f, 0.3f));
        npcName.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        npcName.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        var nnRect = npcName.GetComponent<RectTransform>();
        nnRect.anchorMin        = new Vector2(0, 1);
        nnRect.anchorMax        = new Vector2(0, 1);
        nnRect.pivot            = new Vector2(0.5f, 1);
        nnRect.anchoredPosition = new Vector2(80, -10);
        nnRect.sizeDelta        = new Vector2(160, 28);

        // Dialogue Text — main area
        var dText = MakeTMPText(panel, "DialogueText", "", 20, Color.white);
        var dTxt  = dText.GetComponent<TextMeshProUGUI>();
        dTxt.alignment          = TextAlignmentOptions.TopLeft;
        dTxt.enableWordWrapping = true;
        var dtRect = dText.GetComponent<RectTransform>();
        dtRect.anchorMin = new Vector2(0, 0);
        dtRect.anchorMax = new Vector2(1, 1);
        dtRect.offsetMin = new Vector2(170, 55); // room for portrait + buttons
        dtRect.offsetMax = new Vector2(-20, -15);

        // ContinueHint — bottom-right "▼ Nhấn để tiếp"
        var hint = MakeTMPText(panel, "ContinueHint", "▼ Nhấn để tiếp", 13, new Color(0.75f, 0.75f, 0.75f));
        hint.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.BottomRight;
        var hRect = hint.GetComponent<RectTransform>();
        hRect.anchorMin        = new Vector2(1, 0);
        hRect.anchorMax        = new Vector2(1, 0);
        hRect.pivot            = new Vector2(1, 0);
        hRect.anchoredPosition = new Vector2(-20, 18);
        hRect.sizeDelta        = new Vector2(220, 28);

        // ActionButtons container (hidden initially)
        var actions     = new GameObject("ActionButtons");
        actions.transform.SetParent(panel.transform, false);
        var actRect = actions.AddComponent<RectTransform>();
        actRect.anchorMin        = new Vector2(0.5f, 0);
        actRect.anchorMax        = new Vector2(0.5f, 0);
        actRect.pivot            = new Vector2(0.5f, 0);
        actRect.anchoredPosition = new Vector2(0, 18);
        actRect.sizeDelta        = new Vector2(300, 52);
        var hlg = actions.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing             = 20;
        hlg.childAlignment      = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        MakeButton(actions, "BtnAccept",  "Nhận",       new Color(0.85f, 0.42f, 0.08f), 130, 48);
        MakeButton(actions, "BtnDecline", "Hủy",        new Color(0.35f, 0.35f, 0.35f), 130, 48);

        // Add component
        var comp = root.AddComponent<QuestDialogueUI>();

        root.SetActive(false); // prefab starts hidden
        return root;
    }

    //  QuestNpcPanel Canvas
    //  Wood-style list panel (like NPC menu in screenshots)

    private static GameObject BuildNpcPanel()
    {
        var root   = new GameObject("QuestNpcPanel");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();

        // Add the component
        root.AddComponent<QuestNpcPanel>();

        // Panel background (wood tone)
        var panelBg = MakeImage(root, "QuestNpcPanelRoot", new Color(0.22f, 0.15f, 0.08f, 0.97f));
        var bgRect   = panelBg.GetComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0, 0.5f);
        bgRect.anchorMax        = new Vector2(0, 0.5f);
        bgRect.pivot            = new Vector2(0, 0.5f);
        bgRect.anchoredPosition = new Vector2(20, 0);
        bgRect.sizeDelta        = new Vector2(320, 490);

        // Outline/border (optional darker image behind)
        var outline = MakeImage(panelBg, "Outline", new Color(0.55f, 0.38f, 0.15f));
        var outRect  = outline.GetComponent<RectTransform>();
        outRect.anchorMin = Vector2.zero; outRect.anchorMax = Vector2.one;
        outRect.offsetMin = new Vector2(-3, -3); outRect.offsetMax = new Vector2(3, 3);
        outline.transform.SetAsFirstSibling();

        // BtnClose (X — top right)
        var closeBtn = MakeButton(panelBg, "BtnClose", "✕", new Color(0.7f, 0.2f, 0.1f), 36, 36);
        var closeRect = closeBtn.GetComponent<RectTransform>();
        closeRect.anchorMin        = new Vector2(1, 1);
        closeRect.anchorMax        = new Vector2(1, 1);
        closeRect.pivot            = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-6, -6);

        // Header text
        var header = MakeTMPText(panelBg, "Header", "Xin chào Dũng Sĩ", 18, new Color(1f, 0.9f, 0.6f));
        header.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        header.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        var hRect = header.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0, 1); hRect.anchorMax = new Vector2(1, 1);
        hRect.pivot     = new Vector2(0.5f, 1);
        hRect.anchoredPosition = new Vector2(0, -12);
        hRect.sizeDelta        = new Vector2(-80, 38);

        // Divider line
        var div = MakeImage(panelBg, "Divider", new Color(0.55f, 0.40f, 0.15f));
        var divRect = div.GetComponent<RectTransform>();
        divRect.anchorMin        = new Vector2(0, 1);
        divRect.anchorMax        = new Vector2(1, 1);
        divRect.pivot            = new Vector2(0.5f, 1);
        divRect.anchoredPosition = new Vector2(0, -55);
        divRect.sizeDelta        = new Vector2(-20, 2);

        // ScrollRect — quest list
        var scrollGO   = new GameObject("QuestListScroll");
        scrollGO.transform.SetParent(panelBg.transform, false);
        var scroll     = scrollGO.AddComponent<ScrollRect>();
        scroll.horizontal   = false;
        scroll.vertical     = true;
        var scrollRect = scrollGO.GetComponent<RectTransform>();
        scrollRect.anchorMin        = new Vector2(0, 0);
        scrollRect.anchorMax        = new Vector2(1, 1);
        scrollRect.offsetMin        = new Vector2(10, 68);  // bottom: room for BtnCaoTu
        scrollRect.offsetMax        = new Vector2(-10, -62); // top: room for header

        // Viewport — dùng RectMask2D thay Mask: Mask + Color.clear = stencil không được ghi
        // → TMP_Text (stencil Comp=Equal,Id=1) không pass → text invisible hoàn toàn.
        var vpGO = new GameObject("Viewport");
        vpGO.transform.SetParent(scrollGO.transform, false);
        vpGO.AddComponent<RectMask2D>();
        SetStretch(vpGO);
        scroll.viewport = vpGO.GetComponent<RectTransform>();

        // Content (VerticalLayoutGroup)
        var contentGO   = new GameObject("Content");
        contentGO.transform.SetParent(vpGO.transform, false);
        var contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot     = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);
        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(6, 6, 4, 4);
        vlg.spacing             = 4;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = false;
        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = contentRect;

        // BtnCaoTu — bottom "Cáo từ"
        var caoTuBtn  = MakeButton(panelBg, "BtnCaoTu", "Cáo từ", new Color(0.55f, 0.35f, 0.10f), 200, 46);
        var caoTuRect = caoTuBtn.GetComponent<RectTransform>();
        caoTuRect.anchorMin        = new Vector2(0.5f, 0);
        caoTuRect.anchorMax        = new Vector2(0.5f, 0);
        caoTuRect.pivot            = new Vector2(0.5f, 0);
        caoTuRect.anchoredPosition = new Vector2(0, 12);

        root.SetActive(false);
        return root;
    }

    //  QuestHudWidget Canvas  (compact tracker top-left)

    private static GameObject BuildHudWidget()
    {
        var root   = new GameObject("QuestHudWidget");
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight  = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<QuestHudWidget>();

        // Inner widget panel
        var panel    = MakeImage(root, "QuestHudPanel", new Color(0.08f, 0.15f, 0.05f, 0.82f));
        var panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin        = new Vector2(0, 1);
        panelRect.anchorMax        = new Vector2(0, 1);
        panelRect.pivot            = new Vector2(0, 1);
        panelRect.anchoredPosition = new Vector2(12, -214); // below health bars area
        panelRect.sizeDelta        = new Vector2(360, 104);

        // Left gold border line
        var border = MakeImage(panel, "BorderLeft", new Color(0.85f, 0.70f, 0.10f));
        var bRect  = border.GetComponent<RectTransform>();
        bRect.anchorMin = new Vector2(0, 0); bRect.anchorMax = new Vector2(0, 1);
        bRect.offsetMin = new Vector2(0, 2); bRect.offsetMax = new Vector2(4, -2);

        // QuestName text — "Chính: ..."
        var nameGO = MakeTMPText(panel, "QuestName", "Chính: ...", 18, new Color(1f, 0.9f, 0.3f));
        nameGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        nameGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
        var nameRect = nameGO.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.52f);
        nameRect.anchorMax = new Vector2(1, 1);
        nameRect.offsetMin = new Vector2(14, 4);
        nameRect.offsetMax = new Vector2(-56, -6);

        // QuestStep text — "- ..."
        var stepGO = MakeTMPText(panel, "QuestStep", "- ...", 16, Color.white);
        stepGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;
        var stepRect = stepGO.GetComponent<RectTransform>();
        stepRect.anchorMin = new Vector2(0, 0);
        stepRect.anchorMax = new Vector2(1, 0.52f);
        stepRect.offsetMin = new Vector2(14, 8);
        stepRect.offsetMax = new Vector2(-56, -2);

        // BtnNavigate — right side "→"
        var navBtn  = MakeButton(panel, "BtnNavigate", "→", new Color(0.85f, 0.55f, 0.10f), 44, 86);
        var navRect = navBtn.GetComponent<RectTransform>();
        navRect.anchorMin = new Vector2(1, 0.5f);
        navRect.anchorMax = new Vector2(1, 0.5f);
        navRect.pivot     = new Vector2(1, 0.5f);
        navRect.anchoredPosition = new Vector2(-4, 0);
        navBtn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 22;

        return root;
    }

    //  QuestListItem Prefab  (for QuestNpcPanel's scroll list)

    private static GameObject BuildQuestListItem()
    {
        var item    = new GameObject("QuestListItem");
        var rt      = item.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 46);

        var img   = item.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.04f);

        var btn   = item.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor      = new Color(1, 1, 1, 0.04f);
        colors.highlightedColor = new Color(0.95f, 0.78f, 0.25f, 0.25f);
        colors.pressedColor     = new Color(0.85f, 0.60f, 0.10f, 0.35f);
        btn.colors = colors;

        var textGO = MakeTMPText(item, "Label", "? Nhiệm vụ", 17, Color.white);
        var tTxt   = textGO.GetComponent<TextMeshProUGUI>();
        tTxt.alignment = TextAlignmentOptions.MidlineLeft;
        var tRect  = textGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
        tRect.offsetMin = new Vector2(16, 4); tRect.offsetMax = new Vector2(-10, -4);

        return item;
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private static GameObject MakeImage(GameObject parent, string name, Color color)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        go.AddComponent<RectTransform>();
        return go;
    }

    private static GameObject MakeTMPText(GameObject parent, string name, string text, float size, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.enableWordWrapping = true;
        return go;
    }

    private static GameObject MakeButton(GameObject parent, string name, string label, Color bgColor, float w, float h)
    {
        var go   = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var rt   = go.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(w, h);
        var img  = go.AddComponent<Image>();
        img.color = bgColor;
        var btn  = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = Color.white * 1.2f;
        btn.colors = colors;

        var textGO = MakeTMPText(go, "Label", label, 16, Color.white);
        textGO.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        textGO.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        var tRect = textGO.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;

        return go;
    }

    private static void SetStretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    private static void EnsureDir(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts = path.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }
    }

    private static void SavePrefab(GameObject go, string dir, string prefabName)
    {
        string path = $"{dir}/{prefabName}.prefab";
        bool success;
        PrefabUtility.SaveAsPrefabAsset(go, path, out success);
        Object.DestroyImmediate(go);
        if (!success) Debug.LogError($"[QuestUIBuilder] Tạo prefab thất bại: {path}");
    }

    private static void PrintGuide()
    {
        Debug.Log(
@"[QuestUIBuilder] === HƯỚNG DẪN CONFIG ===

Prefabs đã tạo trong Assets/Resources/UI/:
  QuestDialogueUI.prefab  — hộp hội thoại màn đen + NPC
  QuestNpcPanel.prefab    — danh sách nhiệm vụ khi nói chuyện với NPC
  QuestHudWidget.prefab   — tracker góc màn hình
  Quest/QuestListItem.prefab — item trong danh sách

BƯỚC 1 — Thêm vào Scene:
  Kéo 3 prefab (QuestDialogueUI, QuestNpcPanel, QuestHudWidget) vào Hierarchy của scene game chính.
  Chúng tự DontDestroyOnLoad / tự tìm nhau qua GetOrCreate().

BƯỚC 2 — Gán QuestListItem prefab:
  Chọn QuestNpcPanel → Inspector → Quest Item Prefab → kéo QuestListItem.prefab vào.

BƯỚC 3 — Đảm bảo QuestManager có trong scene:
  Nếu chưa có, tạo GameObject 'QuestManager' và gắn component QuestManager.

BƯỚC 4 — Chạy SQL migration:
  mysql -u root -p gamedb < SQL/migrate_quest_system.sql

BƯỚC 5 — Thêm NPC vào DB:
  INSERT INTO npc_config (npc_id, npc_name, npc_type, map_id, pos_x, pos_y, is_active)
  VALUES (2, 'Đại Tướng Lan', 'quest', 0, 5.0, 0.0, 1)
  ON DUPLICATE KEY UPDATE npc_type='quest', is_active=1;

BƯỚC 6 — Test:
  Đứng gần NPC quest → nói chuyện → QuestNpcPanel mở
  Click tên quest → hội thoại màn đen xuất hiện → Nhận
  HUD góc màn hình hiện 'Chính: [tên quest]'
  Click nút '→' để auto-di chuyển đến mục tiêu
");
    }
}
#endif
