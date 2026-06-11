#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Editor tool: tự động tạo Chat Panel prefab + Friend List Panel prefab + Message Entry prefab.
// Menu: GameTools → Chat → Create Chat Prefabs
public static class ChatPrefabCreator
{
    private const string PREFAB_DIR = "Assets/Resources/Prefabs/Chat";

    [MenuItem("GameTools/Chat/Create Chat Prefabs")]
    public static void CreateAll()
    {
        EnsureDirectory(PREFAB_DIR);

        CreateMessageEntryPrefab();
        CreateChatPanelPrefab();
        CreateFriendListPanelPrefab();
        CreateProximityChatManagerPrefab();
        CreateChatManagerPrefab();
        CreateChatHudButtonPrefab();
        CreateFriendHudButtonPrefab();

        AssetDatabase.Refresh();
        { /* ✓ Đã tạo tất cả prefab trong */ }
    }

    // 1. ChatMessageEntry prefab

    private static void CreateMessageEntryPrefab()
    {
        var root = new GameObject("ChatMessageEntry");
        root.AddComponent<RectTransform>();

        var hlg = root.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment      = TextAnchor.UpperLeft;
        hlg.padding             = new RectOffset(4, 4, 2, 2);
        hlg.spacing             = 6;
        hlg.childForceExpandWidth  = false;
        hlg.childForceExpandHeight = false;

        var csf = root.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

        // Timestamp
        var ts = MakeTmpText(root, "TimestampText", "[HH:mm]", 11, new Color(0.5f, 0.5f, 0.5f));
        var tsLayout = ts.AddComponent<LayoutElement>();
        tsLayout.minWidth     = 40;
        tsLayout.preferredWidth = 40;

        // Sender
        var sender = MakeTmpText(root, "SenderText", "[Tên]", 13, new Color(1f, 0.9f, 0.3f));
        sender.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        var sLayout = sender.AddComponent<LayoutElement>();
        sLayout.minWidth     = 60;
        sLayout.preferredWidth = 90;

        // Message
        var msg = MakeTmpText(root, "MessageText", "Nội dung tin nhắn...", 13, Color.white);
        var mLayout = msg.AddComponent<LayoutElement>();
        mLayout.flexibleWidth = 1;
        msg.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;

        root.AddComponent<ChatMessageEntryUI>();

        var script = root.GetComponent<ChatMessageEntryUI>();
        var so = new SerializedObject(script);
        SetPrivateField(so, "timestampText", ts.GetComponent<TextMeshProUGUI>());
        SetPrivateField(so, "senderText",    sender.GetComponent<TextMeshProUGUI>());
        SetPrivateField(so, "messageText",   msg.GetComponent<TextMeshProUGUI>());
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "ChatMessageEntry");
        Object.DestroyImmediate(root);
    }

    // 2. ChatPanel prefab

    private static void CreateChatPanelPrefab()
    {
        var root = new GameObject("ChatPanel");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(500, 300);

        // Background
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.18f, 0.12f, 0.06f, 0.95f);

        // Header
        var header   = MakeChild<RectTransform>(root, "Header");
        var headerRt = header.GetComponent<RectTransform>();
        SetAnchors(headerRt, 0, 1, 0, 1, 0, -30, 0, 0);
        headerRt.sizeDelta = new Vector2(0, 30);

        var headerBg = header.AddComponent<Image>();
        headerBg.color = new Color(0.55f, 0.35f, 0.0f, 1f);

        // Title
        var titleGo = MakeTmpText(header, "TitleText", "Tin nhắn", 16, new Color(1f, 0.95f, 0.3f));
        var titleRt = titleGo.GetComponent<RectTransform>();
        SetAnchors(titleRt, 0, 1, 0, 1, 0, 0, 0, 0);
        titleGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Close button
        var closeGo  = MakeChild<RectTransform>(header, "CloseButton");
        var closeRt  = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin  = new Vector2(1, 0);
        closeRt.anchorMax  = new Vector2(1, 1);
        closeRt.offsetMin  = new Vector2(-30, 2);
        closeRt.offsetMax  = new Vector2(-4, -2);
        var closeImg = closeGo.AddComponent<Image>();
        closeImg.color     = new Color(0.8f, 0.2f, 0.1f);
        var closeBtn = closeGo.AddComponent<Button>();
        var closeTxt = MakeTmpText(closeGo, "X", "X", 14, Color.white);
        var closeTxtRt = closeTxt.GetComponent<RectTransform>();
        SetAnchors(closeTxtRt, 0, 1, 0, 1, 0, 0, 0, 0);
        closeTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Tab Bar
        var tabBar   = MakeChild<RectTransform>(root, "TabBar");
        var tabBarRt = tabBar.GetComponent<RectTransform>();
        SetAnchors(tabBarRt, 0, 1, 0, 0, 0, 30, 0, 60);
        var tabBg = tabBar.AddComponent<Image>();
        tabBg.color = new Color(0.12f, 0.08f, 0.04f, 1f);

        var tabHlg = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHlg.childAlignment      = TextAnchor.MiddleCenter;
        tabHlg.childForceExpandWidth  = true;
        tabHlg.childForceExpandHeight = true;
        tabHlg.padding = new RectOffset(4, 4, 2, 2);
        tabHlg.spacing = 2;

        string[] tabNames   = { "Chung",  "Riêng",  "Gia tộc", "Nhóm",  "Lớp" };
        Color[]  tabColors  =
        {
            new Color(1f,0.9f,0.3f), new Color(1f,0.5f,0.7f),
            new Color(0.5f,1f,0.5f), new Color(1f,0.65f,0.2f), new Color(0.5f,0.7f,1f)
        };
        foreach (var tName in tabNames)
        {
            var tb = MakeChild<RectTransform>(tabBar, $"Tab_{tName}");
            tb.AddComponent<Image>().color = new Color(0.4f, 0.3f, 0.1f);
            tb.AddComponent<Button>();
            var tTxt = MakeTmpText(tb, "Label", tName, 13, Color.white);
            var tTxtRt = tTxt.GetComponent<RectTransform>();
            SetAnchors(tTxtRt, 0, 1, 0, 1, 0, 0, 0, 0);
            tTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        }

        tabBar.AddComponent<ChatTabUI>();

        // Input Bar
        var inputBar   = MakeChild<RectTransform>(root, "InputBar");
        var inputBarRt = inputBar.GetComponent<RectTransform>();
        SetAnchors(inputBarRt, 0, 1, 0, 0, 0, 0, 0, 30);
        inputBar.AddComponent<Image>().color = new Color(0.1f, 0.07f, 0.03f, 1f);

        var inputHlg = inputBar.AddComponent<HorizontalLayoutGroup>();
        inputHlg.childAlignment      = TextAnchor.MiddleLeft;
        inputHlg.padding             = new RectOffset(4, 4, 3, 3);
        inputHlg.spacing             = 4;
        inputHlg.childForceExpandHeight = true;
        inputHlg.childForceExpandWidth  = false;

        // Channel icon button [LC]
        var chBtnGo = MakeChild<RectTransform>(inputBar, "ChannelIconButton");
        chBtnGo.AddComponent<Image>().color = new Color(0.2f, 0.5f, 1f);
        chBtnGo.AddComponent<Button>();
        var chLayout = chBtnGo.AddComponent<LayoutElement>();
        chLayout.minWidth = 36; chLayout.preferredWidth = 36;
        var chLbl = MakeTmpText(chBtnGo, "ChannelIconLabel", "TG", 11, Color.white);
        var chLblRt = chLbl.GetComponent<RectTransform>();
        SetAnchors(chLblRt, 0, 1, 0, 1, 0, 0, 0, 0);
        chLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        chLbl.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        var chIconImage = MakeChild<RectTransform>(chBtnGo, "ChannelIconImage");
        SetAnchors(chIconImage.GetComponent<RectTransform>(), 0, 1, 0, 1, 6, -6, 6, -6);
        var chIconGraphic = chIconImage.gameObject.AddComponent<Image>();
        chIconGraphic.raycastTarget = false;
        chIconGraphic.preserveAspect = true;
        chIconGraphic.color = Color.white;
        chIconGraphic.enabled = false;

        // Channel name label
        var chNameGo = MakeTmpText(inputBar, "ChannelNameLabel", "Thế giới", 11, new Color(0.8f, 0.8f, 0.8f));
        var chNameLayout = chNameGo.AddComponent<LayoutElement>();
        chNameLayout.minWidth = 55; chNameLayout.preferredWidth = 55;
        chNameGo.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;

        // Input field
        var inputField = CreateTMPInputField(inputBar, "ChatInputField", "Nhập tin nhắn...");
        var ifLayout   = inputField.AddComponent<LayoutElement>();
        ifLayout.flexibleWidth = 1;

        // Send button
        var sendGo = MakeChild<RectTransform>(inputBar, "SendButton");
        sendGo.AddComponent<Image>().color = new Color(0.6f, 0.4f, 0f);
        sendGo.AddComponent<Button>();
        var sendLayout = sendGo.AddComponent<LayoutElement>();
        sendLayout.minWidth = 40; sendLayout.preferredWidth = 40;
        var sendTxt = MakeTmpText(sendGo, "Label", "Gửi", 12, Color.white);
        SetAnchors(sendTxt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        sendTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Channel Dropdown Panel
        var dropdown   = MakeChild<RectTransform>(root, "ChannelDropdown");
        var dropdownRt = dropdown.GetComponent<RectTransform>();
        dropdownRt.anchorMin  = new Vector2(0, 0);
        dropdownRt.anchorMax  = new Vector2(0, 0);
        dropdownRt.pivot      = new Vector2(0, 0);
        dropdownRt.anchoredPosition = new Vector2(4, 60);
        dropdownRt.sizeDelta  = new Vector2(190, 220);
        dropdown.AddComponent<Image>().color = new Color(0.12f, 0.08f, 0.04f, 0.97f);
        var dropVlg = dropdown.AddComponent<VerticalLayoutGroup>();
        dropVlg.childForceExpandWidth  = true;
        dropVlg.childForceExpandHeight = false;
        dropVlg.padding = new RectOffset(4, 4, 4, 4);
        dropVlg.spacing = 2;
        var dropCsf = dropdown.AddComponent<ContentSizeFitter>();
        dropCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        dropdown.AddComponent<ChatChannelDropdownUI>();

        // Message ScrollView
        var scrollGo = MakeChild<RectTransform>(root, "MessageScrollView");
        var scrollRt = scrollGo.GetComponent<RectTransform>();
        SetAnchors(scrollRt, 0, 1, 0, 1, 0, 60, 0, -30);  // between header and tabbar+inputbar
        var scrollImg = scrollGo.AddComponent<Image>();
        scrollImg.color = new Color(0.55f, 0.35f, 0.1f, 0.4f);

        var viewport = MakeChild<RectTransform>(scrollGo, "Viewport");
        var vpRt = viewport.GetComponent<RectTransform>();
        SetAnchors(vpRt, 0, 1, 0, 1, 0, 0, 0, 0);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = MakeChild<RectTransform>(viewport, "Content");
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 0);
        contentRt.anchorMax = new Vector2(1, 1);
        contentRt.pivot     = new Vector2(0, 0);
        contentRt.offsetMin = Vector2.zero;
        contentRt.offsetMax = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment      = TextAnchor.LowerLeft;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.padding  = new RectOffset(6, 6, 4, 4);
        vlg.spacing  = 2;
        var csf2 = content.AddComponent<ContentSizeFitter>();
        csf2.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = scrollGo.AddComponent<ScrollRect>();
        scrollRect.content         = content.GetComponent<RectTransform>();
        scrollRect.viewport        = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal      = false;
        scrollRect.vertical        = true;
        scrollRect.scrollSensitivity = 30;
        scrollRect.movementType    = ScrollRect.MovementType.Clamped;

        // ChatPanelUI script
        var panelScript = root.AddComponent<ChatPanelUI>();
        var so = new SerializedObject(panelScript);
        var messageEntryPrefab = LoadMessageEntryPrefabAsset();
        SetPrivateField(so, "messageScrollRect",  scrollRect);
        SetPrivateField(so, "messageContent",     content);
        SetPrivateField(so, "messageEntryPrefab", messageEntryPrefab);
        SetPrivateField(so, "chatInputField",     inputField.GetComponent<TMP_InputField>());
        SetPrivateField(so, "sendButton",         sendGo.GetComponent<Button>());
        SetPrivateField(so, "channelIconButton",  chBtnGo.GetComponent<Button>());
        SetPrivateField(so, "channelIconImage",   chIconGraphic);
        SetPrivateField(so, "channelIconLabel",   chLbl.GetComponent<TextMeshProUGUI>());
        SetPrivateField(so, "channelNameLabel",   chNameGo.GetComponent<TextMeshProUGUI>());
        SetPrivateField(so, "channelDropdown",    dropdown.GetComponent<ChatChannelDropdownUI>());
        SetPrivateField(so, "tabBar",             tabBar.GetComponent<ChatTabUI>());
        SetPrivateField(so, "closeButton",        closeBtn);
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "ChatPanel");
        Object.DestroyImmediate(root);
    }

    // 3. FriendListPanel prefab

    private static void CreateFriendListPanelPrefab()
    {
        var root = new GameObject("FriendListPanel");
        var rootRt = root.AddComponent<RectTransform>();
        rootRt.sizeDelta = new Vector2(360, 450);
        root.AddComponent<Image>().color = new Color(0.18f, 0.12f, 0.06f, 0.95f);

        // Header
        var header = MakeChild<RectTransform>(root, "Header");
        SetAnchors(header.GetComponent<RectTransform>(), 0, 1, 1, 1, 0, -30, 0, 0);
        header.AddComponent<Image>().color = new Color(0.55f, 0.35f, 0f);
        var titleTxt = MakeTmpText(header, "TitleText", "Bạn bè", 16, new Color(1f, 0.95f, 0.3f));
        SetAnchors(titleTxt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        titleTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var closeGo = MakeChild<RectTransform>(header, "CloseButton");
        var closeRt = closeGo.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1, 0);
        closeRt.anchorMax = new Vector2(1, 1);
        closeRt.offsetMin = new Vector2(-30, 2);
        closeRt.offsetMax = new Vector2(-4, -2);
        closeGo.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.1f);
        var closeBtn = closeGo.AddComponent<Button>();
        var cTxt = MakeTmpText(closeGo, "X", "X", 14, Color.white);
        SetAnchors(cTxt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        cTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Search Bar
        var searchBar = MakeChild<RectTransform>(root, "SearchBar");
        SetAnchors(searchBar.GetComponent<RectTransform>(), 0, 1, 1, 1, 0, -70, 0, -30);
        searchBar.AddComponent<Image>().color = new Color(0.12f, 0.08f, 0.03f);
        var searchHlg = searchBar.AddComponent<HorizontalLayoutGroup>();
        searchHlg.childForceExpandWidth = false;
        searchHlg.childForceExpandHeight = true;
        searchHlg.padding  = new RectOffset(4, 4, 4, 4);
        searchHlg.spacing  = 4;

        var searchInput = CreateTMPInputField(searchBar, "SearchInput", "Tìm theo tên...");
        searchInput.AddComponent<LayoutElement>().flexibleWidth = 1;

        var addBtn = MakeChild<RectTransform>(searchBar, "SearchButton");
        addBtn.AddComponent<Image>().color = new Color(0.3f, 0.55f, 0.2f);
        addBtn.AddComponent<Button>();
        addBtn.AddComponent<LayoutElement>().minWidth = 60;
        var addTxt = MakeTmpText(addBtn, "Label", "Tìm", 13, Color.white);
        SetAnchors(addTxt.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        addTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Friend scroll
        var scrollGo = BuildSimpleScrollView(root, "FriendScrollView");
        SetAnchors(scrollGo.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, -70);

        // FriendListUI script
        var script = root.AddComponent<FriendListUI>();
        var so = new SerializedObject(script);
        SetPrivateField(so, "closeButton",    closeBtn);
        SetPrivateField(so, "searchInput",    searchInput.GetComponent<TMP_InputField>());
        SetPrivateField(so, "searchButton",   addBtn.GetComponent<Button>());
        SetPrivateField(so, "friendListContent",
            scrollGo.transform.Find("Viewport/Content"));
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "FriendListPanel");
        Object.DestroyImmediate(root);
    }

    // 4. ProximityChatBubble / ChatManager prefabs

    private static void CreateProximityChatManagerPrefab()
    {
        // ProximityChatBubble is added at runtime to player prefab;
        // no standalone prefab needed (script self-builds its own canvas).
    }

    private static void CreateChatManagerPrefab()
    {
        var go = new GameObject("ChatManager");
        go.AddComponent<ChatManager>();
        go.AddComponent<FriendManager>();
        // SignalRClient is added dynamically by ChatManager

        SavePrefab(go, "ChatManager");
        Object.DestroyImmediate(go);
    }

    // 5. ChatHudButton prefab

    private static void CreateChatHudButtonPrefab()
    {
        // Root button
        var root = new GameObject("ChatHudButton", typeof(RectTransform));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(46, 46);
        var btnImg = root.AddComponent<Image>();
        btnImg.color = new Color(0.55f, 0.35f, 0f, 1f);
        root.AddComponent<Button>();
        root.AddComponent<ChatToggleButton>();

        // Badge background
        var badge = new GameObject("BadgeRoot", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(root.transform, false);
        var badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin        = new Vector2(1, 1);
        badgeRt.anchorMax        = new Vector2(1, 1);
        badgeRt.pivot            = new Vector2(1, 1);
        badgeRt.anchoredPosition = new Vector2(4, 4);
        badgeRt.sizeDelta        = new Vector2(18, 18);
        badge.GetComponent<Image>().color = new Color(0.9f, 0.1f, 0.1f);
        badge.SetActive(false);

        var badgeTxt = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeTxt.transform.SetParent(badge.transform, false);
        var btRt = badgeTxt.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero; btRt.offsetMax = Vector2.zero;
        var btTmp = badgeTxt.GetComponent<TextMeshProUGUI>();
        btTmp.text      = "0";
        btTmp.fontSize  = 10;
        btTmp.color     = Color.white;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.fontStyle = FontStyles.Bold;

        // Icon label "Chat"
        var lbl = MakeTmpText(root, "IconLabel", "Chat", 12, Color.white);
        SetAnchors(lbl.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        lbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        // Assign badge to script
        var so = new SerializedObject(root.GetComponent<ChatToggleButton>());
        SetPrivateField(so, "badgeRoot", badge);
        SetPrivateField(so, "badgeText", badgeTxt.GetComponent<TextMeshProUGUI>());
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "ChatHudButton");
        Object.DestroyImmediate(root);
    }

    // 6. FriendHudButton prefab

    private static void CreateFriendHudButtonPrefab()
    {
        var root = new GameObject("FriendHudButton", typeof(RectTransform));
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(46, 46);
        var btnImg = root.AddComponent<Image>();
        btnImg.color = new Color(0.2f, 0.55f, 0.2f, 1f);
        root.AddComponent<Button>();
        root.AddComponent<FriendToggleButton>();

        // Badge
        var badge = new GameObject("BadgeRoot", typeof(RectTransform), typeof(Image));
        badge.transform.SetParent(root.transform, false);
        var badgeRt = badge.GetComponent<RectTransform>();
        badgeRt.anchorMin        = new Vector2(1, 1);
        badgeRt.anchorMax        = new Vector2(1, 1);
        badgeRt.pivot            = new Vector2(1, 1);
        badgeRt.anchoredPosition = new Vector2(4, 4);
        badgeRt.sizeDelta        = new Vector2(18, 18);
        badge.GetComponent<Image>().color = new Color(0.9f, 0.1f, 0.1f);
        badge.SetActive(false);

        var badgeTxt = new GameObject("BadgeText", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeTxt.transform.SetParent(badge.transform, false);
        var btRt = badgeTxt.GetComponent<RectTransform>();
        btRt.anchorMin = Vector2.zero; btRt.anchorMax = Vector2.one;
        btRt.offsetMin = Vector2.zero; btRt.offsetMax = Vector2.zero;
        var btTmp = badgeTxt.GetComponent<TextMeshProUGUI>();
        btTmp.text      = "0";
        btTmp.fontSize  = 10;
        btTmp.color     = Color.white;
        btTmp.alignment = TextAlignmentOptions.Center;
        btTmp.fontStyle = FontStyles.Bold;

        var lbl = MakeTmpText(root, "IconLabel", "Bạn bè", 11, Color.white);
        SetAnchors(lbl.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        lbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var so = new SerializedObject(root.GetComponent<FriendToggleButton>());
        SetPrivateField(so, "badgeRoot", badge);
        SetPrivateField(so, "badgeText", badgeTxt.GetComponent<TextMeshProUGUI>());
        so.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "FriendHudButton");
        Object.DestroyImmediate(root);
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private static void EnsureDirectory(string path)
    {
        var parts  = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    private static void SavePrefab(GameObject go, string name)
    {
        var path = $"{PREFAB_DIR}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        { /* Đã tạo: {path} */ }
    }

    private static GameObject LoadMessageEntryPrefabAsset()
    {
        var preferred = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/MsgEntry.prefab");
        if (preferred != null)
            return preferred;

        return AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFAB_DIR}/ChatMessageEntry.prefab");
    }

    private static GameObject MakeChild<T>(GameObject parent, string name) where T : Component
    {
        var go = new GameObject(name, typeof(T));
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static GameObject MakeChild<T>(Transform parent, string name) where T : Component
    {
        var go = new GameObject(name, typeof(T));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject MakeTmpText(GameObject parent, string childName,
        string text, float size, Color color)
    {
        var go = new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.enableWordWrapping = false;
        tmp.overflowMode      = TextOverflowModes.Ellipsis;
        return go;
    }

    private static GameObject MakeTmpText(Transform parent, string childName,
        string text, float size, Color color)
        => MakeTmpText(parent.gameObject, childName, text, size, color);

    private static GameObject CreateTMPInputField(GameObject parent, string name, string placeholder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 26);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.05f, 0.02f, 1f);

        var inputField = go.AddComponent<TMP_InputField>();

        // Text area
        var textAreaGo = new GameObject("Text Area", typeof(RectTransform));
        textAreaGo.transform.SetParent(go.transform, false);
        var taRt = textAreaGo.GetComponent<RectTransform>();
        taRt.anchorMin  = Vector2.zero;
        taRt.anchorMax  = Vector2.one;
        taRt.offsetMin  = new Vector2(4, 2);
        taRt.offsetMax  = new Vector2(-4, -2);
        textAreaGo.AddComponent<RectMask2D>();

        // Placeholder
        var phGo  = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGo.transform.SetParent(textAreaGo.transform, false);
        SetAnchors(phGo.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        var phTmp = phGo.GetComponent<TextMeshProUGUI>();
        phTmp.text      = placeholder;
        phTmp.fontSize  = 13;
        phTmp.color     = new Color(0.5f, 0.5f, 0.5f, 0.8f);
        phTmp.fontStyle = FontStyles.Italic;

        // Text
        var txtGo  = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(textAreaGo.transform, false);
        SetAnchors(txtGo.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        var txtTmp = txtGo.GetComponent<TextMeshProUGUI>();
        txtTmp.fontSize = 13;
        txtTmp.color    = Color.white;

        inputField.textViewport       = textAreaGo.GetComponent<RectTransform>();
        inputField.textComponent      = txtTmp;
        inputField.placeholder        = phTmp;
        inputField.caretColor         = Color.white;
        inputField.selectionColor     = new Color(0.5f, 0.7f, 1f, 0.4f);

        return go;
    }

    private static GameObject CreateTMPInputField(Transform parent, string name, string placeholder)
        => CreateTMPInputField(parent.gameObject, name, placeholder);

    private static GameObject BuildSimpleScrollView(GameObject parent, string name)
    {
        var go = MakeChild<RectTransform>(parent, name);

        var viewport = MakeChild<RectTransform>(go, "Viewport");
        SetAnchors(viewport.GetComponent<RectTransform>(), 0, 1, 0, 1, 0, 0, 0, 0);
        viewport.AddComponent<Image>().color = new Color(0, 0, 0, 0);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        var content = MakeChild<RectTransform>(viewport, "Content");
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin  = new Vector2(0, 1);
        crt.anchorMax  = new Vector2(1, 1);
        crt.pivot      = new Vector2(0, 1);
        crt.offsetMin  = Vector2.zero;
        crt.offsetMax  = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 2;
        vlg.padding = new RectOffset(4, 4, 4, 4);
        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = go.AddComponent<ScrollRect>();
        sr.content    = crt;
        sr.viewport   = viewport.GetComponent<RectTransform>();
        sr.horizontal = false;
        sr.vertical   = true;
        sr.scrollSensitivity = 30;

        return go;
    }

    private static void SetAnchors(RectTransform rt,
        float aMinX, float aMaxX, float aMinY, float aMaxY,
        float offMinX, float offMinY, float offMaxX, float offMaxY)
    {
        rt.anchorMin  = new Vector2(aMinX, aMinY);
        rt.anchorMax  = new Vector2(aMaxX, aMaxY);
        rt.offsetMin  = new Vector2(offMinX, offMinY);
        rt.offsetMax  = new Vector2(offMaxX, offMaxY);
    }

    private static void SetPrivateField(SerializedObject so, string fieldName, Object value)
    {
        var prop = so.FindProperty(fieldName);
        if (prop != null) prop.objectReferenceValue = value;
    }

    private static void SetPrivateField(SerializedObject so, string fieldName, Transform value)
        => SetPrivateField(so, fieldName, (Object)value);
}
#endif
