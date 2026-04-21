#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Editor tool: tự động tạo prefabs cho hệ thống bạn bè.
/// Menu: GameTools → Friends → Create Friend Prefabs
/// </summary>
public static class FriendPrefabCreator
{
    private const string PREFAB_DIR = "Assets/Resources/Prefabs/Chat";

    [MenuItem("GameTools/Friends/Create Friend Prefabs")]
    public static void CreateAll()
    {
        EnsureDirectory(PREFAB_DIR);
        CreateFriendListPanelPrefab();
        CreateFriendRowEntryPrefab();
        CreatePlayerProfilePanelPrefab();
        AssetDatabase.Refresh();
        Debug.Log("[FriendPrefabCreator] ✓ Đã tạo prefabs bạn bè trong " + PREFAB_DIR);
    }

    // ── 1. FriendListPanel ────────────────────────────────────────────────────

    private static void CreateFriendListPanelPrefab()
    {
        // Root
        var root = new GameObject("FriendListPanel");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(360, 500);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.10f, 0.04f, 0.97f);

        // ── Header ──────────────────────────────────────────────
        var header = MakeChild(root, "Header");
        var hRt    = header.GetComponent<RectTransform>();
        SetAnchors(hRt, 0, 1, 1, 1, 0, -40, 0, 0);
        header.AddComponent<Image>().color = new Color(0.55f, 0.35f, 0f, 1f);

        var titleGo = MakeTmp(header, "TitleText", "Bạn Bè", 16, new Color(1f, 0.95f, 0.3f));
        SetAnchors(titleGo.GetComponent<RectTransform>(), 0, 1, 0, 1, 40, 0, -44, 0);
        titleGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var closeGo = MakeChild(header, "CloseButton");
        var cRt     = closeGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1, 0); cRt.anchorMax = new Vector2(1, 1);
        cRt.offsetMin = new Vector2(-38, 3); cRt.offsetMax = new Vector2(-4, -3);
        closeGo.AddComponent<Image>().color = new Color(0.75f, 0.18f, 0.08f);
        closeGo.AddComponent<Button>();
        var cTxt = MakeTmp(closeGo, "X", "✕", 14, Color.white);
        SetAnchors(cTxt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        cTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // ── Tab Bar ──────────────────────────────────────────────
        var tabBar = MakeChild(root, "TabBar");
        var tbRt   = tabBar.GetComponent<RectTransform>();
        SetAnchors(tbRt, 0, 1, 1, 1, 0, -76, 0, -40);
        tabBar.AddComponent<Image>().color = new Color(0.11f, 0.07f, 0.03f, 1f);
        var hlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(2, 2, 2, 2);
        hlg.spacing = 2;

        MakeTabButton(tabBar, "TabFriendsBtn",  "Bạn Bè",      true);
        MakeTabButton(tabBar, "TabAddBtn",      "Kết Bạn Mới", false);
        var pendingTab = MakeTabButton(tabBar, "TabPendingBtn", "Lời Mời", false);

        // Badge trên tab lời mời
        var badge = MakeChild(pendingTab, "TabPendingBadge");
        var badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin = new Vector2(1, 1); badgeRt.anchorMax = new Vector2(1, 1);
        badgeRt.pivot     = new Vector2(1, 1);
        badgeRt.sizeDelta = new Vector2(20, 20);
        badgeRt.anchoredPosition = new Vector2(-2, -2);
        badge.AddComponent<Image>().color = new Color(0.9f, 0.1f, 0.1f);
        var badgeTxt = MakeTmp(badge, "Text", "0", 11, Color.white);
        SetAnchors(badgeTxt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        badgeTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        badge.SetActive(false);

        float contentTop = -76f;

        // ── Panel Bạn Bè ─────────────────────────────────────────
        var panelFriends = MakeChild(root, "PanelFriends");
        SetAnchors(panelFriends.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, contentTop);

        var emptyFriend = MakeTmp(panelFriends, "EmptyFriendLabel", "Chưa có bạn bè nào.", 13, new Color(0.6f, 0.6f, 0.6f));
        SetAnchors(emptyFriend.GetComponent<RectTransform>(), 0, 1, 0.5f, 0.5f, 10, -20, -10, 20);
        emptyFriend.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var (friendScroll, friendContent) = MakeScrollView(panelFriends, "FriendScrollView");
        SetAnchors(friendScroll.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);

        // ── Panel Kết Bạn Mới ────────────────────────────────────
        var panelAdd = MakeChild(root, "PanelAdd");
        SetAnchors(panelAdd.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, contentTop);
        panelAdd.SetActive(false);

        // Search bar
        var searchBar = MakeChild(panelAdd, "SearchBar");
        var sbRt      = searchBar.GetComponent<RectTransform>();
        SetAnchors(sbRt, 0, 1, 1, 1, 4, -44, -4, 0);
        searchBar.AddComponent<Image>().color = new Color(0.1f, 0.07f, 0.03f, 0.8f);
        var sbHlg = searchBar.AddComponent<HorizontalLayoutGroup>();
        sbHlg.padding  = new RectOffset(4, 4, 3, 3);
        sbHlg.spacing  = 4;
        sbHlg.childForceExpandHeight = true;
        sbHlg.childForceExpandWidth  = false;

        var searchInput = CreateInputField(searchBar, "SearchInput", "Nhập tên người chơi...");
        searchInput.AddComponent<LayoutElement>().flexibleWidth = 1;

        var searchBtn = MakeChild(searchBar, "SearchButton");
        searchBtn.AddComponent<Image>().color = new Color(0.5f, 0.35f, 0f);
        searchBtn.AddComponent<Button>();
        searchBtn.AddComponent<LayoutElement>().minWidth = 60;
        var sbTxt = MakeTmp(searchBtn, "Label", "Tìm", 13, Color.white);
        SetAnchors(sbTxt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        sbTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var hintLbl = MakeTmp(panelAdd, "SearchHintLabel", "Nhập tên để tìm người chơi.", 12, new Color(0.6f, 0.6f, 0.6f));
        SetAnchors(hintLbl.GetComponent<RectTransform>(), 0, 1, 1, 1, 8, -80, -8, -44);
        hintLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        var (searchScroll, searchResultContent) = MakeScrollView(panelAdd, "SearchResultScrollView");
        SetAnchors(searchScroll.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -44);

        // ── Panel Lời Mời ────────────────────────────────────────
        var panelPending = MakeChild(root, "PanelPending");
        SetAnchors(panelPending.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, contentTop);
        panelPending.SetActive(false);

        var emptyPending = MakeTmp(panelPending, "EmptyPendingLabel", "Không có lời mời nào.", 13, new Color(0.6f, 0.6f, 0.6f));
        SetAnchors(emptyPending.GetComponent<RectTransform>(), 0, 1, 0.5f, 0.5f, 10, -20, -10, 20);
        emptyPending.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var (pendingScroll, pendingContent) = MakeScrollView(panelPending, "PendingScrollView");
        SetAnchors(pendingScroll.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);

        // ── FriendListUI component ───────────────────────────────
        var friendListUI = root.AddComponent<FriendListUI>();
        var so = new SerializedObject(friendListUI);

        so.FindProperty("closeButton")          .objectReferenceValue = closeGo.GetComponent<Button>();
        so.FindProperty("titleLabel")           .objectReferenceValue = titleGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("tabFriendsBtn")        .objectReferenceValue = tabBar.transform.Find("TabFriendsBtn")?.GetComponent<Button>();
        so.FindProperty("tabAddBtn")            .objectReferenceValue = tabBar.transform.Find("TabAddBtn")?.GetComponent<Button>();
        so.FindProperty("tabPendingBtn")        .objectReferenceValue = tabBar.transform.Find("TabPendingBtn")?.GetComponent<Button>();
        so.FindProperty("tabPendingBadge")      .objectReferenceValue = badge.GetComponentInChildren<TextMeshProUGUI>();
        so.FindProperty("panelFriends")         .objectReferenceValue = panelFriends;
        so.FindProperty("panelAdd")             .objectReferenceValue = panelAdd;
        so.FindProperty("panelPending")         .objectReferenceValue = panelPending;
        so.FindProperty("friendListContent")    .objectReferenceValue = friendContent;
        so.FindProperty("emptyFriendLabel")     .objectReferenceValue = emptyFriend.GetComponent<TextMeshProUGUI>();
        so.FindProperty("searchInput")          .objectReferenceValue = searchInput.GetComponent<TMP_InputField>();
        so.FindProperty("searchButton")         .objectReferenceValue = searchBtn.GetComponent<Button>();
        so.FindProperty("searchResultContent")  .objectReferenceValue = searchResultContent;
        so.FindProperty("searchHintLabel")      .objectReferenceValue = hintLbl.GetComponent<TextMeshProUGUI>();
        so.FindProperty("pendingContent")       .objectReferenceValue = pendingContent;
        so.FindProperty("emptyPendingLabel")    .objectReferenceValue = emptyPending.GetComponent<TextMeshProUGUI>();

        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "FriendListPanel");
        Object.DestroyImmediate(root);
    }

    // ── 2. FriendRowEntry ─────────────────────────────────────────────────────

    private static void CreateFriendRowEntryPrefab()
    {
        var root = new GameObject("FriendRowEntry");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 44);

        var bg  = root.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.10f, 0.05f, 0.88f);

        var le = root.AddComponent<LayoutElement>();
        le.minHeight = 44;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.padding               = new RectOffset(8, 8, 4, 4);
        hlg.spacing               = 6;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Name (flexible)
        var nameGo = MakeTmp(root, "NameText", "Tên người chơi", 14, Color.white);
        nameGo.AddComponent<LayoutElement>().flexibleWidth = 1;

        // Chat
        MakeIconBtn(root, "ChatButton",    "💬", new Color(0.18f, 0.45f, 0.88f));
        // Profile
        MakeIconBtn(root, "ProfileButton", "👁",  new Color(0.35f, 0.35f, 0.6f));
        // Accept
        MakeIconBtn(root, "AcceptButton",  "✓",  new Color(0.20f, 0.65f, 0.2f));
        // Add (Kết bạn)
        var addGo = MakeIconBtn(root, "AddButton", "➕", new Color(0.3f, 0.6f, 0.15f));
        MakeTmp(addGo, "Label2", "Kết Bạn", 11, Color.white)
            .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        // Delete
        MakeIconBtn(root, "DeleteButton",  "✕",  new Color(0.70f, 0.15f, 0.1f));

        SavePrefab(root, "FriendRowEntry");
        Object.DestroyImmediate(root);
    }

    // ── 3. PlayerProfilePanel ─────────────────────────────────────────────────

    private static void CreatePlayerProfilePanelPrefab()
    {
        var root = new GameObject("PlayerProfilePanel");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(400, 520);
        root.AddComponent<Image>().color = new Color(0.15f, 0.10f, 0.04f, 0.97f);

        // Header
        var header = MakeChild(root, "Header");
        SetAnchors(header.GetComponent<RectTransform>(), 0, 1, 1, 1, 0, -50, 0, 0);
        header.AddComponent<Image>().color = new Color(0.55f, 0.35f, 0f);

        var nameGo    = MakeTmp(header, "NameLabel",    "Tên nhân vật", 17, new Color(1f, 0.95f, 0.3f));
        SetAnchors(nameGo.GetComponent<RectTransform>(), 0.05f, 0.65f, 0, 1, 0, 0, 0, 0);

        var elementGo = MakeTmp(header, "ElementLabel", "Nguyên tố", 13, new Color(0.7f, 1f, 0.7f));
        SetAnchors(elementGo.GetComponent<RectTransform>(), 0.65f, 0.85f, 0, 1, 0, 0, 0, 0);

        var levelGo   = MakeTmp(header, "LevelLabel",   "Lv ?", 13, new Color(0.9f, 0.75f, 0.2f));
        SetAnchors(levelGo.GetComponent<RectTransform>(), 0.85f, 1f, 0, 1, 0, 0, -44, 0);

        var closeGo = MakeChild(header, "CloseButton");
        var cRt     = closeGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1, 0); cRt.anchorMax = new Vector2(1, 1);
        cRt.offsetMin = new Vector2(-40, 4); cRt.offsetMax = new Vector2(-4, -4);
        closeGo.AddComponent<Image>().color = new Color(0.75f, 0.18f, 0.08f);
        closeGo.AddComponent<Button>();
        var cTxt = MakeTmp(closeGo, "X", "✕", 14, Color.white);
        SetAnchors(cTxt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        cTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Tab bar
        var tabBar = MakeChild(root, "TabBar");
        SetAnchors(tabBar.GetComponent<RectTransform>(), 0, 1, 1, 1, 0, -86, 0, -50);
        tabBar.AddComponent<Image>().color = new Color(0.10f, 0.06f, 0.02f);
        var tbHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tbHlg.childForceExpandWidth = true; tbHlg.childForceExpandHeight = true;
        tbHlg.padding = new RectOffset(2, 2, 2, 2); tbHlg.spacing = 2;

        MakeTabButton(tabBar, "TabEquipBtn",    "Trang Bị",  true);
        MakeTabButton(tabBar, "TabSkillBtn",    "Kỹ Năng",   false);
        MakeTabButton(tabBar, "TabPotentialBtn","Tiềm Năng", false);

        // Content area
        var contentArea = MakeChild(root, "ContentArea");
        SetAnchors(contentArea.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -86);

        GameObject MakeTabPanel(string name)
        {
            var p = MakeChild(contentArea, name);
            SetAnchors(p.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
            var (sv, content) = MakeScrollView(p, "ScrollView");
            SetAnchors(sv.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
            return content.gameObject;
        }

        var equipContent     = MakeTabPanel("PanelEquip");
        var skillContent     = MakeTabPanel("PanelSkill");
        var potentialContent = MakeTabPanel("PanelPotential");

        skillContent.transform.parent.parent.gameObject.SetActive(false);
        potentialContent.transform.parent.parent.gameObject.SetActive(false);

        // Loading overlay
        var loading = MakeChild(root, "LoadingOverlay");
        SetAnchors(loading.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        loading.AddComponent<Image>().color = new Color(0, 0, 0, 0.65f);
        MakeTmp(loading, "Txt", "Đang tải...", 15, Color.white)
            .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        loading.SetActive(false);

        // PlayerProfilePanelUI component
        var ui = root.AddComponent<PlayerProfilePanelUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("closeButton")     .objectReferenceValue = closeGo.GetComponent<Button>();
        so.FindProperty("nameLabel")       .objectReferenceValue = nameGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("elementLabel")    .objectReferenceValue = elementGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("levelLabel")      .objectReferenceValue = levelGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("tabEquipBtn")     .objectReferenceValue = tabBar.transform.Find("TabEquipBtn")?.GetComponent<Button>();
        so.FindProperty("tabSkillBtn")     .objectReferenceValue = tabBar.transform.Find("TabSkillBtn")?.GetComponent<Button>();
        so.FindProperty("tabPotentialBtn") .objectReferenceValue = tabBar.transform.Find("TabPotentialBtn")?.GetComponent<Button>();
        so.FindProperty("panelEquip")      .objectReferenceValue = equipContent.transform.parent.parent.gameObject;
        so.FindProperty("panelSkill")      .objectReferenceValue = skillContent.transform.parent.parent.gameObject;
        so.FindProperty("panelPotential")  .objectReferenceValue = potentialContent.transform.parent.parent.gameObject;
        so.FindProperty("equipContent")    .objectReferenceValue = equipContent.transform;
        so.FindProperty("skillContent")    .objectReferenceValue = skillContent.transform;
        so.FindProperty("potentialContent").objectReferenceValue = potentialContent.transform;
        so.FindProperty("loadingOverlay")  .objectReferenceValue = loading;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "PlayerProfilePanel");
        Object.DestroyImmediate(root);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (GameObject scroll, Transform content) MakeScrollView(GameObject parent, string name)
    {
        var sv = MakeChild(parent, name);
        sv.AddComponent<Image>().color = new Color(0, 0, 0, 0.15f);

        var vp    = MakeChild(sv, "Viewport");
        var vpRt  = vp.GetComponent<RectTransform>();
        SetAnchors(vpRt, 0, 1, 0, 1, 0, 0, 0, 0);
        vp.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        vp.AddComponent<Mask>().showMaskGraphic = false;

        var content = MakeChild(vp, "Content");
        var cRt     = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1);
        cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot     = new Vector2(0, 1);
        cRt.offsetMin = Vector2.zero;
        cRt.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 3;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = sv.AddComponent<ScrollRect>();
        sr.content    = content.GetComponent<RectTransform>();
        sr.viewport   = vpRt;
        sr.horizontal = false;
        sr.vertical   = true;

        return (sv, content.transform);
    }

    private static GameObject MakeTabButton(GameObject parent, string name, string label, bool active)
    {
        var go = MakeChild(parent, name);
        go.AddComponent<Image>().color = active
            ? new Color(0.7f, 0.5f, 0.1f)
            : new Color(0.25f, 0.18f, 0.07f);
        go.AddComponent<Button>();
        var txt = MakeTmp(go, "Label", label, 13, Color.white);
        SetAnchors(txt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        txt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static GameObject MakeIconBtn(GameObject parent, string name, string icon, Color bg)
    {
        var go = MakeChild(parent, name);
        go.AddComponent<Image>().color = bg;
        go.AddComponent<Button>();
        go.AddComponent<LayoutElement>().minWidth = 34;

        var lbl = MakeTmp(go, "Label", icon, 14, Color.white);
        SetAnchors(lbl.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        lbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static GameObject CreateInputField(GameObject parent, string name, string placeholder)
    {
        var go      = MakeChild(parent, name);
        var goImage = go.AddComponent<Image>();
        goImage.color = new Color(0.08f, 0.06f, 0.03f, 0.9f);

        var inputField = go.AddComponent<TMP_InputField>();
        inputField.transition = Selectable.Transition.ColorTint;

        // Viewport
        var vp      = MakeChild(go, "Text Area");
        var vpRt    = vp.GetComponent<RectTransform>();
        SetAnchors(vpRt, 0, 1, 0, 1, 6, 2, -6, -2);
        vp.AddComponent<RectMask2D>();

        // Placeholder
        var phGo  = MakeTmp(vp, "Placeholder", placeholder, 13, new Color(0.5f, 0.5f, 0.5f));
        var phRt  = phGo.GetComponent<RectTransform>();
        SetAnchors(phRt, 0, 1, 0, 1, 0, 0, 0, 0);
        phGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;

        // Text
        var txtGo = MakeTmp(vp, "Text", "", 13, Color.white);
        var txtRt = txtGo.GetComponent<RectTransform>();
        SetAnchors(txtRt, 0, 1, 0, 1, 0, 0, 0, 0);

        inputField.textComponent     = txtGo.GetComponent<TextMeshProUGUI>();
        inputField.placeholder       = phGo.GetComponent<TextMeshProUGUI>();
        inputField.textViewport      = vpRt;

        return go;
    }

    private static GameObject MakeChild(GameObject parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static GameObject MakeTmp(GameObject parent, string name, string text, float size, Color color)
    {
        var go  = MakeChild(parent, name);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text     = text;
        tmp.fontSize = size;
        tmp.color    = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode       = TextOverflowModes.Ellipsis;
        return go;
    }

    /// <summary>offsetMin/Max correspond to: left, bottom, right, top offsets from anchors.</summary>
    private static void SetAnchors(RectTransform rt,
        float anchorMinX, float anchorMaxX, float anchorMinY, float anchorMaxY,
        float left, float bottom, float right, float top)
    {
        rt.anchorMin  = new Vector2(anchorMinX, anchorMinY);
        rt.anchorMax  = new Vector2(anchorMaxX, anchorMaxY);
        rt.offsetMin  = new Vector2(left, bottom);
        rt.offsetMax  = new Vector2(-right, -top);
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            var parts  = path.Split('/');
            var parent = "";
            foreach (var p in parts)
            {
                var full = parent.Length == 0 ? p : $"{parent}/{p}";
                if (!AssetDatabase.IsValidFolder(full))
                    AssetDatabase.CreateFolder(parent, p);
                parent = full;
            }
        }
    }

    private static void SavePrefab(GameObject root, string prefabName)
    {
        var path = $"{PREFAB_DIR}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Debug.Log($"[FriendPrefabCreator] ✓ {prefabName} → {path}");
    }
}
#endif
