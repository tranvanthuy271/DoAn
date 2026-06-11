#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor tool – tự động tạo prefabs cho:
// • CharacterMenuPanel      (avatar + tên + level + exp% + nút chức năng, neo trái)
// • CharacterMenuHudButton  (nút HUD góc trên trái → mở CharacterMenuPanel)
// • SocialPanel             (4 tab ngoài: Đồng đội / Bạn bè / Kẻ thù / Tin nhắn)
// • PartyPanel              (nội dung "Đồng đội": 3 sub-tab)
// • Entry prefabs           (MemberEntry, SearchEntry, NearbyEntry)
// Menu: GameTools → CharacterMenu → Create Character &amp; Social Prefabs
public static class CharacterMenuPrefabCreator
{
    private const string PREFAB_DIR        = "Assets/Resources/Prefabs/UI";
    private const string PARTY_PREFAB_DIR  = "Assets/Resources/Prefabs/UI/Party";
    private const string NOTO_SANS_PATH    = "Assets/TextMesh Pro/Resources/Fonts & Materials/NotoSans-Regular SDF.asset";
    private const string DEFAULT_ELEMENT_ICON_CONFIG_PATH = "Assets/Resources/ScriptableObjects/ElementIconConfig.asset";
    private const string LEGACY_ELEMENT_ICON_CONFIG_PATH  = "Assets/ScriptableObjects/ElementIconConfig.asset";

    private static TMP_FontAsset _cachedNotoSans;

    [MenuItem("GameTools/CharacterMenu/Create Character & Social Prefabs")]
    public static void CreateAll()
    {
        EnsureDirectory(PREFAB_DIR);
        EnsureDirectory(PARTY_PREFAB_DIR);

        CreateMemberEntryPrefab();
        CreateSearchEntryPrefab();
        CreateNearbyEntryPrefab();
        CreatePartyPanelPrefab();
        CreateSocialPanelPrefab();
        CreateCharacterMenuPanelPrefab();
        CreateCharacterMenuHudButtonPrefab();

        AssetDatabase.Refresh();
        { /* ✓ Tạo xong tất cả prefabs trong */ }
    }

    // ENTRY PREFABS

    // Hàng thành viên trong Tab "Nhóm riêng".
    private static void CreateMemberEntryPrefab()
    {
        var root = new GameObject("MemberEntryPrefab");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 52);

        root.AddComponent<Image>().color = new Color(0.22f, 0.16f, 0.07f, 0.9f);

        var le  = root.AddComponent<LayoutElement>();
        le.minHeight = 52;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(6, 6, 4, 4);
        hlg.spacing               = 6;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Avatar placeholder
        var avatar = MakeChild(root, "AvatarImage");
        avatar.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.6f);
        var ale = avatar.AddComponent<LayoutElement>();
        ale.minWidth = 40; ale.preferredWidth = 40;

        // Element icon overlay (child of AvatarImage)
        var elemIconGo = MakeChild(avatar, "ElementIconImage");
        var elemRt = elemIconGo.GetComponent<RectTransform>();
        elemRt.anchorMin = Vector2.zero; elemRt.anchorMax = Vector2.one;
        elemRt.offsetMin = new Vector2(4, 4); elemRt.offsetMax = new Vector2(-4, -4);
        var elemImg = elemIconGo.AddComponent<Image>();
        elemImg.preserveAspect = true;
        elemImg.color = Color.white;

        // Info column
        var info    = MakeChild(root, "InfoColumn");
        info.AddComponent<LayoutElement>().flexibleWidth = 1;
        var vlg     = info.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.MiddleLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2;

        var nameGo   = MakeTmp(info, "CharacterNameText", "Tên: Asasin",         14, Color.white);
        var detailGo = MakeTmp(info, "DetailText",         "Cấp: 54, Lớp: Dao găm", 11, new Color(0.75f, 0.75f, 0.75f));
        nameGo.GetComponent<TextMeshProUGUI>().fontStyle   = FontStyles.Bold;
        detailGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

        // Leader badge
        var badge = MakeChild(root, "LeaderBadge");
        badge.AddComponent<Image>().color = new Color(1f, 0.75f, 0.1f);
        var badgeLe = badge.AddComponent<LayoutElement>();
        badgeLe.minWidth = 70; badgeLe.preferredWidth = 70;
        MakeTmp(badge, "Label", "(Nhóm trưởng)", 11, Color.black)
            .GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        badge.SetActive(false);

        // Offline mask (semi-transparent overlay)
        var mask = MakeChild(root, "OfflineMask");
        var maskRt = mask.GetComponent<RectTransform>();
        maskRt.anchorMin = Vector2.zero; maskRt.anchorMax = Vector2.one;
        maskRt.offsetMin = Vector2.zero; maskRt.offsetMax = Vector2.zero;
        mask.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        mask.SetActive(false);

        // Wire PartyMemberEntryUI
        var ui = root.AddComponent<PartyMemberEntryUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("characterNameText").objectReferenceValue = nameGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("detailText")       .objectReferenceValue = detailGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("leaderBadge")      .objectReferenceValue = badge.GetComponent<Image>();
        so.FindProperty("offlineMask")      .objectReferenceValue = mask.GetComponent<Image>();
        so.FindProperty("elementIcon")      .objectReferenceValue = elemImg;
        var memberIconConfig = LoadElementIconConfigAsset();
        if (memberIconConfig != null)
            so.FindProperty("elementIconConfig").objectReferenceValue = memberIconConfig;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "MemberEntryPrefab", PARTY_PREFAB_DIR);
        Object.DestroyImmediate(root);
    }

    // Hàng kết quả tìm nhóm trong Tab "Tìm nhóm".
    private static void CreateSearchEntryPrefab()
    {
        var root = new GameObject("PartySearchEntryPrefab");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 52);
        root.AddComponent<Image>().color = new Color(0.20f, 0.14f, 0.06f, 0.88f);
        root.AddComponent<LayoutElement>().minHeight = 52;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(6, 6, 4, 4);
        hlg.spacing               = 6;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Avatar
        var avatar = MakeChild(root, "AvatarImage");
        avatar.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.6f);
        avatar.AddComponent<LayoutElement>().minWidth = 40;

        // Element icon overlay
        var searchElemGo  = MakeChild(avatar, "ElementIconImage");
        var searchElemRt  = searchElemGo.GetComponent<RectTransform>();
        searchElemRt.anchorMin = Vector2.zero; searchElemRt.anchorMax = Vector2.one;
        searchElemRt.offsetMin = new Vector2(4, 4); searchElemRt.offsetMax = new Vector2(-4, -4);
        var searchElemImg = searchElemGo.AddComponent<Image>();
        searchElemImg.preserveAspect = true;
        searchElemImg.color = Color.white;

        // Info
        var info = MakeChild(root, "InfoColumn");
        info.AddComponent<LayoutElement>().flexibleWidth = 1;
        var vlg  = info.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2;

        var nameGo  = MakeTmp(info, "InfoText", "Tên: Asasin, Cấp: 54, Lớp: Dao găm", 12, Color.white);
        nameGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        // Lock icon (Image)
        var lockImgGo = MakeChild(root, "LockIcon");
        lockImgGo.AddComponent<Image>().color = new Color(1f, 0.8f, 0.2f);
        lockImgGo.AddComponent<LayoutElement>().minWidth = 20;
        lockImgGo.SetActive(false);

        // Member count
        var countGo = MakeTmp(root, "MemberCountText", "(1 thành viên)", 11, new Color(0.8f, 0.8f, 0.8f));
        countGo.AddComponent<LayoutElement>().minWidth = 80;
        countGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;

        // Join button
        var joinBtn = MakeChild(root, "JoinButton");
        joinBtn.AddComponent<Image>().color = new Color(0.2f, 0.55f, 0.18f);
        joinBtn.AddComponent<Button>();
        joinBtn.AddComponent<LayoutElement>().minWidth = 64;
        var jLbl = MakeTmp(joinBtn, "Label", "Xin vào", 12, Color.white);
        SetFill(jLbl.GetComponent<RectTransform>());
        jLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Wire PartySearchEntryUI
        // infoText shows "Tên: X, Cấp: Y, Lớp: Z" (combined), lockIcon is Image
        var ui = root.AddComponent<PartySearchEntryUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("infoText")       .objectReferenceValue = nameGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("memberCountText").objectReferenceValue = countGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("lockIcon")       .objectReferenceValue = lockImgGo.GetComponent<Image>();
        so.FindProperty("joinButton")     .objectReferenceValue = joinBtn.GetComponent<Button>();
        so.FindProperty("elementIcon")    .objectReferenceValue = searchElemImg;
        var searchIconConfig = LoadElementIconConfigAsset();
        if (searchIconConfig != null)
            so.FindProperty("elementIconConfig").objectReferenceValue = searchIconConfig;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "PartySearchEntryPrefab", PARTY_PREFAB_DIR);
        Object.DestroyImmediate(root);
    }

    // Hàng người chơi gần đây trong Tab "Gần đây".
    private static void CreateNearbyEntryPrefab()
    {
        var root = new GameObject("NearbyPlayerEntryPrefab");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 52);
        root.AddComponent<Image>().color = new Color(0.20f, 0.14f, 0.06f, 0.88f);
        root.AddComponent<LayoutElement>().minHeight = 52;

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.padding               = new RectOffset(6, 6, 4, 4);
        hlg.spacing               = 6;
        hlg.childAlignment        = TextAnchor.MiddleLeft;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = true;

        // Avatar
        var avatar = MakeChild(root, "AvatarImage");
        avatar.AddComponent<Image>().color = new Color(0.4f, 0.4f, 0.6f);
        avatar.AddComponent<LayoutElement>().minWidth = 40;

        // Element icon overlay
        var nearbyElemGo  = MakeChild(avatar, "ElementIconImage");
        var nearbyElemRt  = nearbyElemGo.GetComponent<RectTransform>();
        nearbyElemRt.anchorMin = Vector2.zero; nearbyElemRt.anchorMax = Vector2.one;
        nearbyElemRt.offsetMin = new Vector2(4, 4); nearbyElemRt.offsetMax = new Vector2(-4, -4);
        var nearbyElemImg = nearbyElemGo.AddComponent<Image>();
        nearbyElemImg.preserveAspect = true;
        nearbyElemImg.color = Color.white;

        // Info
        var info = MakeChild(root, "InfoColumn");
        info.AddComponent<LayoutElement>().flexibleWidth = 1;
        var vlg  = info.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2;

        var nameGo = MakeTmp(info, "InfoText", "Tên: Asasin, Cấp: 54, Lớp: Dao găm", 12, Color.white);
        nameGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        // Invite button
        var invBtn = MakeChild(root, "InviteButton");
        invBtn.AddComponent<Image>().color = new Color(0.55f, 0.35f, 0.05f);
        invBtn.AddComponent<Button>();
        invBtn.AddComponent<LayoutElement>().minWidth = 52;
        var iLbl = MakeTmp(invBtn, "Label", "Mời", 12, Color.white);
        SetFill(iLbl.GetComponent<RectTransform>());
        iLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Wire PartyNearbyEntryUI
        // infoText shows "Tên: X, Cấp: Y, Lớp: Z" (combined)
        var ui = root.AddComponent<PartyNearbyEntryUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("infoText")          .objectReferenceValue = nameGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("inviteButton")       .objectReferenceValue = invBtn.GetComponent<Button>();
        so.FindProperty("elementIcon")        .objectReferenceValue = nearbyElemImg;
        var nearbyIconConfig = LoadElementIconConfigAsset();
        if (nearbyIconConfig != null)
            so.FindProperty("elementIconConfig").objectReferenceValue = nearbyIconConfig;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "NearbyPlayerEntryPrefab", PARTY_PREFAB_DIR);
        Object.DestroyImmediate(root);
    }

    // PARTY PANEL (nội dung Đồng đội – 3 sub-tab)

    private static void CreatePartyPanelPrefab()
    {
        var root = new GameObject("PartyPanel");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(400, 420);
        root.AddComponent<Image>().color = new Color(0.13f, 0.09f, 0.03f, 0.98f);

        // Sub-tab bar
        var subTabBar = MakeChild(root, "SubTabBar");
        SetAnchors(subTabBar.GetComponent<RectTransform>(), 0, 1, 1, 1, 0, -36, 0, 0);
        subTabBar.AddComponent<Image>().color = new Color(0.10f, 0.07f, 0.02f);
        var hlg = subTabBar.AddComponent<HorizontalLayoutGroup>();
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.padding = new RectOffset(2, 2, 2, 2);
        hlg.spacing = 2;

        var btnParty  = MakeTabButton(subTabBar, "BtnTabParty",  "Nhóm riêng", true);
        var btnSearch = MakeTabButton(subTabBar, "BtnTabSearch", "Tìm nhóm",  false);
        var btnNearby = MakeTabButton(subTabBar, "BtnTabNearby", "Gần đây",   false);

        // Content area
        float footerH = 48f;
        float tabH    = 36f;

        // PanelParty (Tab 0)
        var panelParty = MakeChild(root, "PanelParty");
        SetAnchors(panelParty.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -tabH);

        var (svParty, contentParty) = MakeScrollView(panelParty, "ScrollView");
        SetAnchors(svParty.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, footerH, 0, 0);

        // Footer Tab 0
        var footerParty = MakeChild(panelParty, "Footer");
        SetAnchors(footerParty.GetComponent<RectTransform>(), 0, 1, 0, 0, 0, 0, 0, footerH);
        footerParty.AddComponent<Image>().color = new Color(0.10f, 0.07f, 0.02f);
        var fHlg = footerParty.AddComponent<HorizontalLayoutGroup>();
        fHlg.padding = new RectOffset(6, 6, 4, 4);
        fHlg.spacing = 6;
        fHlg.childAlignment = TextAnchor.MiddleLeft;
        fHlg.childForceExpandHeight = true;
        fHlg.childForceExpandWidth  = false;

        // Khóa nhóm Toggle
        var lockGo = MakeChild(footerParty, "ToggleLock");
        lockGo.AddComponent<Image>().color = new Color(0.18f, 0.12f, 0.04f, 1f);
        lockGo.AddComponent<LayoutElement>().minWidth = 100;
        var lockToggle = lockGo.AddComponent<Toggle>();
        lockToggle.isOn = false;
        // Chấm đánh dấu: Image con, ẩn mặc định (chỉ hiện khi tick = on)
        var lockCheck = MakeChild(lockGo, "Checkmark");
        var lockCheckRt = lockCheck.GetComponent<RectTransform>();
        lockCheckRt.anchorMin = new Vector2(0f, 0.5f); lockCheckRt.anchorMax = new Vector2(0f, 0.5f);
        lockCheckRt.pivot = new Vector2(0.5f, 0.5f);
        lockCheckRt.anchoredPosition = new Vector2(10f, 0f);
        lockCheckRt.sizeDelta = new Vector2(16f, 16f);
        var lockCheckImg = lockCheck.AddComponent<Image>();
        lockCheckImg.color = new Color(0.2f, 1f, 0.3f);
        lockCheckImg.enabled = false; // toggle indicator bắt đầu tắt
        var lockLbl = MakeTmp(lockGo, "Label", "Khóa nhóm", 12, Color.white);
        SetFill(lockLbl.GetComponent<RectTransform>());
        lockLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Tự cho vào nhóm Toggle
        var autoGo = MakeChild(footerParty, "ToggleAutoAccept");
        autoGo.AddComponent<Image>().color = new Color(0.18f, 0.12f, 0.04f, 1f);
        autoGo.AddComponent<LayoutElement>().minWidth = 120;
        var autoToggle = autoGo.AddComponent<Toggle>();
        autoToggle.isOn = false;
        var autoCheck = MakeChild(autoGo, "Checkmark");
        var autoCheckRt = autoCheck.GetComponent<RectTransform>();
        autoCheckRt.anchorMin = new Vector2(0f, 0.5f); autoCheckRt.anchorMax = new Vector2(0f, 0.5f);
        autoCheckRt.pivot = new Vector2(0.5f, 0.5f);
        autoCheckRt.anchoredPosition = new Vector2(10f, 0f);
        autoCheckRt.sizeDelta = new Vector2(16f, 16f);
        var autoCheckImg = autoCheck.AddComponent<Image>();
        autoCheckImg.color = new Color(0.2f, 1f, 0.3f);
        autoCheckImg.enabled = false;
        var autoLbl = MakeTmp(autoGo, "Label", "Tự cho vào nhóm", 12, Color.white);
        SetFill(autoLbl.GetComponent<RectTransform>());
        autoLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Spacer
        var spacer = MakeChild(footerParty, "Spacer");
        spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

        // Giải tán / Tạo / Rời Button
        var actionGo  = MakeActionButton(footerParty, "BtnAction", "Giải tán",
            new Color(0.65f, 0.15f, 0.05f));
        var actionLbl = actionGo.GetComponentInChildren<TextMeshProUGUI>();

        // Chat nhóm Button
        var chatGo = MakeActionButton(footerParty, "BtnChatGroup", "Chat nhóm",
            new Color(0.18f, 0.45f, 0.78f));

        // PanelSearch (Tab 1)
        var panelSearch = MakeChild(root, "PanelSearch");
        SetAnchors(panelSearch.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -tabH);
        panelSearch.SetActive(false);

        var (svSearch, contentSearch) = MakeScrollView(panelSearch, "ScrollView");
        SetAnchors(svSearch.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, footerH, 0, 0);

        var footerSearch = MakeChild(panelSearch, "Footer");
        SetAnchors(footerSearch.GetComponent<RectTransform>(), 0, 1, 0, 0, 0, 0, 0, footerH);
        footerSearch.AddComponent<Image>().color = new Color(0.10f, 0.07f, 0.02f);
        var refreshSearchGo = MakeActionButton(footerSearch, "BtnRefreshSearch", "Tìm",
            new Color(0.55f, 0.35f, 0f));
        var rsRt = refreshSearchGo.GetComponent<RectTransform>();
        SetAnchors(rsRt, 1, 1, 0, 1, -90, 4, -4, -4);

        // PanelNearby (Tab 2)
        var panelNearby = MakeChild(root, "PanelNearby");
        SetAnchors(panelNearby.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -tabH);
        panelNearby.SetActive(false);

        var (svNearby, contentNearby) = MakeScrollView(panelNearby, "ScrollView");
        SetAnchors(svNearby.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, footerH, 0, 0);

        var footerNearby = MakeChild(panelNearby, "Footer");
        SetAnchors(footerNearby.GetComponent<RectTransform>(), 0, 1, 0, 0, 0, 0, 0, footerH);
        footerNearby.AddComponent<Image>().color = new Color(0.10f, 0.07f, 0.02f);
        var popGo = MakeTmp(footerNearby, "TxtPopulation", "Dân số: 0", 13, new Color(0.85f, 0.85f, 0.85f));
        SetAnchors(popGo.GetComponent<RectTransform>(), 0, 0.5f, 0, 1, 6, 0, 0, 0);
        popGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        var refreshNearbyGo = MakeActionButton(footerNearby, "BtnRefreshNearby", "Làm mới",
            new Color(0.3f, 0.5f, 0.1f));
        var rnRt = refreshNearbyGo.GetComponent<RectTransform>();
        SetAnchors(rnRt, 1, 1, 0, 1, -90, 4, -4, -4);

        // Wire PartyPanelUI
        var ui = root.AddComponent<PartyPanelUI>();
        var so = new SerializedObject(ui);

        so.FindProperty("closeButton")         .objectReferenceValue = null; // Đóng do SocialPanel
        so.FindProperty("tabPartyButton")      .objectReferenceValue = btnParty.GetComponent<Button>();
        so.FindProperty("tabSearchButton")     .objectReferenceValue = btnSearch.GetComponent<Button>();
        so.FindProperty("tabNearbyButton")     .objectReferenceValue = btnNearby.GetComponent<Button>();
        so.FindProperty("partyTabPanel")       .objectReferenceValue = panelParty;
        so.FindProperty("searchTabPanel")      .objectReferenceValue = panelSearch;
        so.FindProperty("nearbyTabPanel")      .objectReferenceValue = panelNearby;
        so.FindProperty("memberListRoot")      .objectReferenceValue = contentParty;
        so.FindProperty("searchListRoot")      .objectReferenceValue = contentSearch;
        so.FindProperty("nearbyListRoot")      .objectReferenceValue = contentNearby;
        so.FindProperty("lockToggle")               .objectReferenceValue = lockToggle;
        so.FindProperty("lockToggleIndicatorImage")   .objectReferenceValue = lockCheckImg;
        so.FindProperty("autoAcceptToggle")           .objectReferenceValue = autoToggle;
        so.FindProperty("autoAcceptToggleIndicatorImage").objectReferenceValue = autoCheckImg;
        so.FindProperty("actionButton")               .objectReferenceValue = actionGo.GetComponent<Button>();
        so.FindProperty("actionButtonLabel")          .objectReferenceValue = actionLbl;
        so.FindProperty("chatGroupButton")     .objectReferenceValue = chatGo.GetComponent<Button>();
        so.FindProperty("refreshSearchButton") .objectReferenceValue = refreshSearchGo.GetComponent<Button>();
        so.FindProperty("refreshNearbyButton") .objectReferenceValue = refreshNearbyGo.GetComponent<Button>();
        so.FindProperty("nearbyPopulationText").objectReferenceValue = popGo.GetComponent<TextMeshProUGUI>();

        // Load entry prefabs từ Resources nếu đã tồn tại
        var memberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PARTY_PREFAB_DIR}/MemberEntryPrefab.prefab");
        var searchPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PARTY_PREFAB_DIR}/PartySearchEntryPrefab.prefab");
        var nearbyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PARTY_PREFAB_DIR}/NearbyPlayerEntryPrefab.prefab");

        if (memberPrefab != null) so.FindProperty("memberEntryPrefab").objectReferenceValue = memberPrefab;
        if (searchPrefab != null) so.FindProperty("searchEntryPrefab").objectReferenceValue = searchPrefab;
        if (nearbyPrefab != null) so.FindProperty("nearbyEntryPrefab").objectReferenceValue = nearbyPrefab;

        // Load PartyJoinRequestPopup prefab nếu đã tồn tại
        var notifPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PARTY_PREFAB_DIR}/PartyJoinRequestPopup.prefab");
        if (notifPrefab != null)
        {
            var notifComp = notifPrefab.GetComponent<PartyJoinRequestPopupUI>();
            if (notifComp != null)
                so.FindProperty("joinRequestPopup").objectReferenceValue = notifComp;
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "PartyPanel", PREFAB_DIR);
        Object.DestroyImmediate(root);
    }

    // Tạo popup thông báo khi có người chơi gửi yêu cầu xin vào nhóm.

    // Panel thông báo xin vào nhóm: hiển thị tên/level/hệ người xin + nút Đồng ý / Từ chối.
    // Tư động ẩn khi khởi tạo. Gắn vào PartyPanel trong scene.
    // JOIN REQUEST NOTIFICATION PANEL (menu riêng — không nằm trong CreateAll)

    // Tạo prefab PartyJoinRequestPopup độc lập.
    // Menu riêng để không đè lên prefab đã cài đặt tay trong scene.
    [MenuItem("GameTools/CharacterMenu/Create Party Join Request Popup")]
    public static void CreateJoinRequestPopupStandalone()
    {
        EnsureDirectory(PARTY_PREFAB_DIR);
        CreateJoinRequestNotificationPanelPrefab();
        AssetDatabase.Refresh();
        { /* ✓ Tạo xong PartyJoinRequestPopup */ }
    }

    private static void CreateJoinRequestNotificationPanelPrefab()
    {
        var root = new GameObject("PartyJoinRequestPopup");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(280, 130);
        root.AddComponent<Image>().color = new Color(0.10f, 0.07f, 0.02f, 0.98f);

        // Tiêu đề
        var titleGo = MakeTmp(root, "TitleText", "Xin vào nhóm", 14, new Color(1f, 0.82f, 0.2f));
        var titleRt = titleGo.GetComponent<RectTransform>();
        SetAnchors(titleRt, 0, 1, 1, 1, 8, -28, -8, 0);
        titleGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        titleGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Rào ngăn
        var divider = MakeChild(root, "Divider");
        SetAnchors(divider.GetComponent<RectTransform>(), 0, 1, 1, 1, 4, -30, -4, -28);
        divider.AddComponent<Image>().color = new Color(0.5f, 0.35f, 0.05f);

        // Icon hệ (trái)
        var iconGo = MakeChild(root, "ElementIconImage");
        var iconRt = iconGo.GetComponent<RectTransform>();
        SetAnchors(iconRt, 0, 0, 0.5f, 1f, 8, -10, 52, 0);
        iconRt.offsetMax = new Vector2(52, 0);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.preserveAspect = true;
        iconImg.color = new Color(0.4f, 0.4f, 0.6f);

        // Info text (giữa)
        var infoGo = MakeTmp(root, "RequesterInfoText", "Tên NV\nCấp 1 – Hệ Không rõ", 12, Color.white);
        var infoRt = infoGo.GetComponent<RectTransform>();
        SetAnchors(infoRt, 0, 1, 0, 1, 58, 40, -8, -30);
        var infoTmp = infoGo.GetComponent<TextMeshProUGUI>();
        infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
        infoTmp.enableWordWrapping = true;

        // Footer buttons row
        var btnRow = MakeChild(root, "ButtonRow");
        SetAnchors(btnRow.GetComponent<RectTransform>(), 0, 1, 0, 0, 8, 0, -8, 36);
        var bHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        bHlg.spacing = 8;
        bHlg.childForceExpandWidth  = true;
        bHlg.childForceExpandHeight = true;
        bHlg.padding = new RectOffset(0, 0, 0, 0);

        var acceptGo = MakeChild(btnRow, "BtnAccept");
        acceptGo.AddComponent<Image>().color = new Color(0.15f, 0.60f, 0.15f);
        var acceptBtn = acceptGo.AddComponent<Button>();
        var acceptLbl = MakeTmp(acceptGo, "Label", "Đồng ý", 13, Color.white);
        SetFill(acceptLbl.GetComponent<RectTransform>());
        acceptLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var declineGo = MakeChild(btnRow, "BtnDecline");
        declineGo.AddComponent<Image>().color = new Color(0.65f, 0.12f, 0.08f);
        var declineBtn = declineGo.AddComponent<Button>();
        var declineLbl = MakeTmp(declineGo, "Label", "Từ chối", 13, Color.white);
        SetFill(declineLbl.GetComponent<RectTransform>());
        declineLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Wire PartyJoinRequestPopupUI
        var ui = root.AddComponent<PartyJoinRequestPopupUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("requesterInfoText").objectReferenceValue = infoTmp;
        so.FindProperty("elementIcon")      .objectReferenceValue = iconImg;
        so.FindProperty("acceptButton")     .objectReferenceValue = acceptBtn;
        so.FindProperty("declineButton")    .objectReferenceValue = declineBtn;
        var notifIconConfig = LoadElementIconConfigAsset();
        if (notifIconConfig != null)
            so.FindProperty("elementIconConfig").objectReferenceValue = notifIconConfig;
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "PartyJoinRequestPopup", PARTY_PREFAB_DIR);
        Object.DestroyImmediate(root);
    }

    // Tạo panel xã hội gồm các tab bạn bè, nhóm và thông báo liên quan.

    private static void CreateSocialPanelPrefab()
    {
        var root = new GameObject("SocialPanel");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(420, 520);
        root.AddComponent<Image>().color = new Color(0.15f, 0.10f, 0.04f, 0.97f);

        // Header
        var header = MakeChild(root, "Header");
        SetAnchors(header.GetComponent<RectTransform>(), 0, 1, 1, 1, 0, -44, 0, 0);
        header.AddComponent<Image>().color = new Color(0.50f, 0.32f, 0f, 1f);

        // Outer tab bar inside header
        var outerTabBar = MakeChild(header, "OuterTabBar");
        SetAnchors(outerTabBar.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, -44, 0);
        var otHlg = outerTabBar.AddComponent<HorizontalLayoutGroup>();
        otHlg.childForceExpandWidth  = true;
        otHlg.childForceExpandHeight = true;
        otHlg.padding = new RectOffset(2, 2, 2, 2);
        otHlg.spacing = 2;

        var btnParty   = MakeOuterTabButton(outerTabBar, "BtnTabParty",   "Đồng đội",  true);
        var btnFriend  = MakeOuterTabButton(outerTabBar, "BtnTabFriend",  "Bạn bè",    false);
        var btnEnemy   = MakeOuterTabButton(outerTabBar, "BtnTabEnemy",   "Kẻ thù",    false);
        var btnMessage = MakeOuterTabButton(outerTabBar, "BtnTabMessage", "Tin nhắn",  false);

        // Close button
        var closeGo = MakeChild(header, "CloseButton");
        var cRt     = closeGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1, 0); cRt.anchorMax = new Vector2(1, 1);
        cRt.offsetMin = new Vector2(-40, 3); cRt.offsetMax = new Vector2(-4, -3);
        closeGo.AddComponent<Image>().color = new Color(0.72f, 0.15f, 0.07f);
        closeGo.AddComponent<Button>();
        var cTxt = MakeTmp(closeGo, "X", "X", 14, Color.white);
        SetFill(cTxt.GetComponent<RectTransform>());
        cTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Content area
        // PanelParty – load PartyPanel prefab content or placeholder
        var panelParty = MakeChild(root, "PanelParty");
        SetAnchors(panelParty.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -44);
        panelParty.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        // Inner placeholder text until PartyPanel prefab is loaded into scene
        var partyPlaceholder = MakeTmp(panelParty, "PartyContent",
            "[PartyPanel sẽ được thêm vào đây trong scene]", 12, new Color(0.55f, 0.55f, 0.55f));
        SetFill(partyPlaceholder.GetComponent<RectTransform>());
        partyPlaceholder.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // PanelFriend – placeholder
        var panelFriend = MakeChild(root, "PanelFriend");
        SetAnchors(panelFriend.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -44);
        panelFriend.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var fpLbl = MakeTmp(panelFriend, "Placeholder", "[Bạn bè – FriendListUI]", 13, new Color(0.55f, 0.55f, 0.55f));
        SetFill(fpLbl.GetComponent<RectTransform>());
        fpLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        panelFriend.SetActive(false);

        // PanelEnemy – placeholder
        var panelEnemy = MakeChild(root, "PanelEnemy");
        SetAnchors(panelEnemy.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -44);
        panelEnemy.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var epLbl = MakeTmp(panelEnemy, "Placeholder", "[Kẻ thù – chưa triển khai]", 13, new Color(0.55f, 0.55f, 0.55f));
        SetFill(epLbl.GetComponent<RectTransform>());
        epLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        panelEnemy.SetActive(false);

        // PanelMessage – placeholder
        var panelMessage = MakeChild(root, "PanelMessage");
        SetAnchors(panelMessage.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -44);
        panelMessage.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        var mpLbl = MakeTmp(panelMessage, "Placeholder", "[Tin nhắn – ChatPanelUI]", 13, new Color(0.55f, 0.55f, 0.55f));
        SetFill(mpLbl.GetComponent<RectTransform>());
        mpLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        panelMessage.SetActive(false);

        // Wire SocialPanelUI
        var ui = root.AddComponent<SocialPanelUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("closeButton")  .objectReferenceValue = closeGo.GetComponent<Button>();
        so.FindProperty("tabPartyBtn")  .objectReferenceValue = btnParty.GetComponent<Button>();
        so.FindProperty("tabFriendBtn") .objectReferenceValue = btnFriend.GetComponent<Button>();
        so.FindProperty("tabEnemyBtn")  .objectReferenceValue = btnEnemy.GetComponent<Button>();
        so.FindProperty("tabMessageBtn").objectReferenceValue = btnMessage.GetComponent<Button>();
        so.FindProperty("panelParty")   .objectReferenceValue = panelParty;
        so.FindProperty("panelFriend")  .objectReferenceValue = panelFriend;
        so.FindProperty("panelEnemy")   .objectReferenceValue = panelEnemy;
        so.FindProperty("panelMessage") .objectReferenceValue = panelMessage;

        var partyPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PartyPanel.prefab");
        if (partyPanelPrefab != null)
            so.FindProperty("partyPanelPrefab").objectReferenceValue = partyPanelPrefab;

        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "SocialPanel", PREFAB_DIR);
        Object.DestroyImmediate(root);
    }

    // CHARACTER MENU PANEL

    private static void CreateCharacterMenuPanelPrefab()
    {
        var root = new GameObject("CharacterMenuPanel");
        var rt   = root.AddComponent<RectTransform>();
        // Neo cạnh trái màn hình: anchor = left-center, pivot = left-center
        rt.anchorMin       = new Vector2(0f, 0.5f);
        rt.anchorMax       = new Vector2(0f, 0.5f);
        rt.pivot           = new Vector2(0f, 0.5f);
        rt.sizeDelta       = new Vector2(280, 400);
        rt.anchoredPosition = new Vector2(10f, 0f); // offset 10px từ mép trái
        root.AddComponent<Image>().color = new Color(0.15f, 0.10f, 0.04f, 0.97f);

        // Header / avatar area
        var headerArea = MakeChild(root, "AvatarArea");
        SetAnchors(headerArea.GetComponent<RectTransform>(), 0, 1, 1, 1, 0, -110, 0, 0);
        headerArea.AddComponent<Image>().color = new Color(0.22f, 0.14f, 0.04f, 1f);

        // Avatar image (placeholder circle)
        var avatarGo = MakeChild(headerArea, "AvatarImage");
        var aRt      = avatarGo.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.05f, 0.1f);
        aRt.anchorMax = new Vector2(0.35f, 0.92f);
        aRt.offsetMin = aRt.offsetMax = Vector2.zero;
        avatarGo.AddComponent<Image>().color = new Color(0.3f, 0.4f, 0.65f, 1f);

        // Account name
        var accGo = MakeTmp(headerArea, "AccountNameText", "Tài khoản: ---", 12, new Color(0.8f, 0.8f, 0.8f));
        var accRt = accGo.GetComponent<RectTransform>();
        SetAnchors(accRt, 0.37f, 1f, 0.65f, 1f, 0, -4, -4, 0);

        // Character name
        var charGo = MakeTmp(headerArea, "CharacterNameText", "Nhân vật: ---", 15, new Color(1f, 0.95f, 0.3f));
        var charRt = charGo.GetComponent<RectTransform>();
        SetAnchors(charRt, 0.37f, 1f, 0.35f, 0.65f, 0, -4, -4, 0);
        charGo.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // Level text
        var lvlGo = MakeTmp(headerArea, "LevelText", "Cấp: 1  (0%)", 13, new Color(0.9f, 0.75f, 0.2f));
        var lvlRt = lvlGo.GetComponent<RectTransform>();
        SetAnchors(lvlRt, 0.37f, 1f, 0.1f, 0.38f, 0, -4, -4, 0);

        // EXP bar
        var expBarGo = MakeChild(root, "ExpBar");
        SetAnchors(expBarGo.GetComponent<RectTransform>(), 0, 1, 1, 1, 6, -120, -6, -110);
        expBarGo.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
        var slider = expBarGo.AddComponent<Slider>();
        slider.minValue = 0; slider.maxValue = 1; slider.value = 0.62f;

        var fillArea    = MakeChild(expBarGo, "Fill Area");
        var fillAreaRt  = fillArea.GetComponent<RectTransform>();
        SetAnchors(fillAreaRt, 0, 1, 0, 1, 0, 0, 0, 0);
        var fill        = MakeChild(fillArea, "Fill");
        fill.AddComponent<Image>().color = new Color(0.2f, 0.7f, 0.2f);
        var fillRt      = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = new Vector2(0, 0); fillRt.anchorMax = new Vector2(0, 1);
        fillRt.offsetMin = fillRt.offsetMax = Vector2.zero;
        slider.fillRect = fillRt;

        // EXP detail text
        var expTxtGo = MakeTmp(root, "ExpDetailText", "0 / 0 EXP", 11, new Color(0.7f, 0.7f, 0.7f));
        SetAnchors(expTxtGo.GetComponent<RectTransform>(), 0, 1, 1, 1, 6, -136, -6, -120);
        expTxtGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Menu buttons
        var menuArea = MakeChild(root, "MenuArea");
        SetAnchors(menuArea.GetComponent<RectTransform>(), 0, 1, 0, 1, 6, 6, -6, -140);
        var vlg = menuArea.AddComponent<VerticalLayoutGroup>();
        vlg.padding               = new RectOffset(4, 4, 4, 4);
        vlg.spacing               = 5;
        vlg.childAlignment        = TextAnchor.UpperCenter;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        var btnQuest       = MakeMenuButton(menuArea, "BtnQuest",      "Nhiệm vụ",     new Color(0.55f, 0.35f, 0.05f));
        var btnRelation    = MakeMenuButton(menuArea, "BtnRelation",   "Quan hệ",       new Color(0.18f, 0.48f, 0.18f));
        var btnClan        = MakeMenuButton(menuArea, "BtnClan",       "Gia tộc",       new Color(0.25f, 0.25f, 0.25f));
        btnClan.GetComponent<Button>().interactable = false;
        var btnSetting     = MakeMenuButton(menuArea, "BtnSetting",    "Setting",       new Color(0.3f, 0.3f, 0.45f));
        var btnChangeChar  = MakeMenuButton(menuArea, "BtnChangeChar", "Đổi nhân vật", new Color(0.4f, 0.25f, 0.05f));
        var btnQuit        = MakeMenuButton(menuArea, "BtnQuit",       "Thoát game",   new Color(0.65f, 0.12f, 0.07f));

        // Close button (top-right X)
        var closeGo = MakeChild(root, "CloseButton");
        var cRt     = closeGo.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(1, 1); cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot     = new Vector2(1, 1);
        cRt.sizeDelta = new Vector2(36, 36);
        cRt.anchoredPosition = new Vector2(-4, -4);
        closeGo.AddComponent<Image>().color = new Color(0.72f, 0.15f, 0.07f);
        closeGo.AddComponent<Button>();
        var closeTxt = MakeTmp(closeGo, "X", "X", 14, Color.white);
        SetFill(closeTxt.GetComponent<RectTransform>());
        closeTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Wire CharacterMenuPanelUI
        var ui = root.AddComponent<CharacterMenuPanelUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("avatarImage")       .objectReferenceValue = avatarGo.GetComponent<Image>();
        so.FindProperty("accountNameText")   .objectReferenceValue = accGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("characterNameText") .objectReferenceValue = charGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("levelText")         .objectReferenceValue = lvlGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("expSlider")         .objectReferenceValue = slider;
        so.FindProperty("expDetailText")     .objectReferenceValue = expTxtGo.GetComponent<TextMeshProUGUI>();
        so.FindProperty("closeButton")       .objectReferenceValue = closeGo.GetComponent<Button>();
        so.FindProperty("questButton")       .objectReferenceValue = btnQuest.GetComponent<Button>();
        so.FindProperty("relationButton")    .objectReferenceValue = btnRelation.GetComponent<Button>();
        so.FindProperty("clanButton")        .objectReferenceValue = btnClan.GetComponent<Button>();
        so.FindProperty("settingButton")     .objectReferenceValue = btnSetting.GetComponent<Button>();
        so.FindProperty("changeCharButton")  .objectReferenceValue = btnChangeChar.GetComponent<Button>();
        so.FindProperty("quitButton")        .objectReferenceValue = btnQuit.GetComponent<Button>();

        var partyPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/PartyPanel.prefab");
        if (partyPanelPrefab != null)
            so.FindProperty("partyPanel").objectReferenceValue = partyPanelPrefab;

        var elementIconConfig = LoadElementIconConfigAsset();
        if (elementIconConfig != null)
            so.FindProperty("elementIconConfig").objectReferenceValue = elementIconConfig;

        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "CharacterMenuPanel", PREFAB_DIR);
        Object.DestroyImmediate(root);
    }

    // CHARACTER MENU HUD BUTTON

    // Nút HUD góc trên trái → click mở / đóng CharacterMenuPanel.
    // Neo anchor = top-left, cạnh trên canvas HUD.
    private static void CreateCharacterMenuHudButtonPrefab()
    {
        var root = new GameObject("CharacterMenuHudButton");
        var rt   = root.AddComponent<RectTransform>();
        // Neo góc trên trái canvas
        rt.anchorMin        = new Vector2(0f, 1f);
        rt.anchorMax        = new Vector2(0f, 1f);
        rt.pivot            = new Vector2(0f, 1f);
        rt.sizeDelta        = new Vector2(54, 54);
        rt.anchoredPosition = new Vector2(10f, -10f); // 10px từ mép trên-trái

        root.AddComponent<Image>().color = new Color(0.45f, 0.28f, 0.04f, 1f);
        root.AddComponent<Button>();
        root.AddComponent<CharacterMenuToggleButton>();

        // Icon text
        var lbl = MakeTmp(root, "IconLabel", "NV", 18, Color.white);
        SetFill(lbl.GetComponent<RectTransform>());
        lbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Sub-label
        var sub = MakeTmp(root, "SubLabel", "Nhân vật", 9, new Color(1f, 0.92f, 0.6f));
        var subRt = sub.GetComponent<RectTransform>();
        subRt.anchorMin = new Vector2(0, 0); subRt.anchorMax = new Vector2(1, 0);
        subRt.pivot     = new Vector2(0.5f, 0);
        subRt.offsetMin = new Vector2(0, 2); subRt.offsetMax = new Vector2(0, 16);
        sub.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        SavePrefab(root, "CharacterMenuHudButton", PREFAB_DIR);
        Object.DestroyImmediate(root);
    }

    private static (GameObject scroll, Transform content) MakeScrollView(GameObject parent, string name)
    {
        var sv   = MakeChild(parent, name);
        sv.AddComponent<Image>().color = new Color(0, 0, 0, 0.12f);

        var vp   = MakeChild(sv, "Viewport");
        SetAnchors(vp.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        vp.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        vp.AddComponent<Mask>().showMaskGraphic = false;

        var content = MakeChild(vp, "Content");
        var cRt     = content.GetComponent<RectTransform>();
        cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
        cRt.pivot     = new Vector2(0, 1);
        cRt.offsetMin = cRt.offsetMax = Vector2.zero;

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        vlg.spacing = 3;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr      = sv.AddComponent<ScrollRect>();
        sr.content  = content.GetComponent<RectTransform>();
        sr.viewport = vp.GetComponent<RectTransform>();
        sr.horizontal = false;
        sr.vertical   = true;

        return (sv, content.transform);
    }

    private static GameObject MakeTabButton(GameObject parent, string name, string label, bool active)
    {
        var go  = MakeChild(parent, name);
        go.AddComponent<Image>().color = active
            ? new Color(0.60f, 0.40f, 0.07f)
            : new Color(0.22f, 0.15f, 0.05f);
        go.AddComponent<Button>();
        var txt = MakeTmp(go, "Label", label, 12, active ? new Color(1f, 0.92f, 0.25f) : Color.white);
        SetFill(txt.GetComponent<RectTransform>());
        txt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static GameObject MakeOuterTabButton(GameObject parent, string name, string label, bool active)
    {
        var go  = MakeChild(parent, name);
        go.AddComponent<Image>().color = active
            ? new Color(0.62f, 0.42f, 0.08f)
            : new Color(0.22f, 0.15f, 0.05f);
        go.AddComponent<Button>();
        var txt = MakeTmp(go, "Label", label, 13, active ? new Color(1f, 0.92f, 0.25f) : Color.white);
        SetFill(txt.GetComponent<RectTransform>());
        txt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        txt.GetComponent<TextMeshProUGUI>().fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
        return go;
    }

    private static GameObject MakeMenuButton(GameObject parent, string name, string label, Color bg)
    {
        var go  = MakeChild(parent, name);
        go.AddComponent<Image>().color = bg;
        go.AddComponent<Button>();
        var le  = go.AddComponent<LayoutElement>();
        le.minHeight = 36; le.preferredHeight = 36;
        var txt = MakeTmp(go, "Label", label, 14, Color.white);
        SetFill(txt.GetComponent<RectTransform>());
        txt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static ElementIconConfig LoadElementIconConfigAsset()
    {
        var config = AssetDatabase.LoadAssetAtPath<ElementIconConfig>(DEFAULT_ELEMENT_ICON_CONFIG_PATH);
        if (config != null)
            return config;

        return AssetDatabase.LoadAssetAtPath<ElementIconConfig>(LEGACY_ELEMENT_ICON_CONFIG_PATH);
    }

    private static GameObject MakeActionButton(GameObject parent, string name, string label, Color bg)
    {
        var go  = MakeChild(parent, name);
        go.AddComponent<Image>().color = bg;
        go.AddComponent<Button>();
        go.AddComponent<LayoutElement>().minWidth = 80;
        var txt = MakeTmp(go, "Label", label, 13, Color.white);
        SetFill(txt.GetComponent<RectTransform>());
        txt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static GameObject MakeChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.AddComponent<RectTransform>();
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static GameObject MakeTmp(GameObject parent, string name, string text, int size, Color color)
    {
        var go  = MakeChild(parent, name);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset fontAsset = GetNotoSansFont();
        if (fontAsset != null)
        {
            tmp.font = fontAsset;
            if (fontAsset.material != null)
                tmp.fontSharedMaterial = fontAsset.material;
        }

        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.raycastTarget = false;
        return go;
    }

    private static TMP_FontAsset GetNotoSansFont()
    {
        if (_cachedNotoSans != null)
            return _cachedNotoSans;

        _cachedNotoSans = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NOTO_SANS_PATH);
        if (_cachedNotoSans == null)
            { /* Cảnh báo: Khong tim thay NotoSans font asset tai */ }

        return _cachedNotoSans;
    }

    private static void SetAnchors(RectTransform rt,
        float anchorMinX, float anchorMaxX, float anchorMinY, float anchorMaxY,
        float offsetMinX, float offsetMinY, float offsetMaxX, float offsetMaxY)
    {
        rt.anchorMin = new Vector2(anchorMinX, anchorMinY);
        rt.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
        rt.offsetMin = new Vector2(offsetMinX, offsetMinY);
        rt.offsetMax = new Vector2(offsetMaxX, offsetMaxY);
    }

    private static void SetFill(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string folder = System.IO.Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, folder);
        }
    }

    private static void SavePrefab(GameObject root, string prefabName, string dir)
    {
        string path = $"{dir}/{prefabName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
    }
}
#endif
