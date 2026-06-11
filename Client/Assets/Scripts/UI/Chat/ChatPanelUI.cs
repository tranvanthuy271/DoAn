using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI chính cho Chat Panel.
// Layout: Header | ScrollView (messages) | TabBar (Chung/Riêng/GiaToc/Nhom/Lop) | InputBar
// Attach script này lên root GameObject "ChatPanel".
public class ChatPanelUI : MonoBehaviour
{
    public static ChatPanelUI Instance { get; private set; }

    // Inspector Refs

    [Header("Message Area")]
    [SerializeField] private ScrollRect        messageScrollRect;
    [SerializeField] private Transform         messageContent;      // VerticalLayoutGroup
    [SerializeField] private GameObject        messageEntryPrefab;  // ChatMessageEntryUI prefab

    [Header("Input Bar")]
    [SerializeField] private TMP_InputField    chatInputField;
    [SerializeField] private Button            sendButton;
    [SerializeField] private Button            channelIconButton;   // LC icon
    [SerializeField] private Image             channelIconImage;    // optional sprite icon
    [SerializeField] private TextMeshProUGUI   channelIconLabel;    // text "LC"
    [SerializeField] private TextMeshProUGUI   channelNameLabel;    // "Lân cận"

    [Header("Channel Dropdown")]
    [SerializeField] private ChatChannelDropdownUI channelDropdown;

    [Header("Tab Bar")]
    [SerializeField] private ChatTabUI         tabBar;

    [Header("Header")]
    [SerializeField] private Button            closeButton;

    [Header("Friend Panel")]
    [SerializeField] private GameObject        friendListPanel;

    // State

    private const int MAX_DISPLAYED = 80;
    private const string DefaultMessageEntryResourcesPath = "Prefabs/Chat/MsgEntry";
    private const string LegacyMessageEntryResourcesPath = "Prefabs/Chat/ChatMessageEntry";
    private static readonly Color FallbackTimestampColor = new Color32(0x7C, 0x67, 0x55, 0xFF);
    private static readonly Color FallbackMessageTextColor = new Color32(0x3E, 0x29, 0x18, 0xFF);
    private readonly Queue<GameObject> _msgObjects = new Queue<GameObject>();
    private bool _chatManagerSubscribed;
    private bool _isConnected;  // theo dõi trạng thái kết nối để hiển thị UI
    private bool _attemptedMessageEntryPrefabResolve;
    private const string GameplayBlockSource = "ChatPanelUI";

    // MonoBehaviour

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        UIPanelManager.Register(gameObject, () => gameObject.SetActive(false));

        sendButton?.onClick.AddListener(OnSendClicked);
        closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        channelIconButton?.onClick.AddListener(OnChannelIconClicked);
        EnsureChannelIconGraphic();
        ResolveMessageEntryPrefab();

        chatInputField?.onEndEdit.AddListener(text =>
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                OnSendClicked();
        });

        // Khi focus vào ô nhập chat → chặn tất cả input game (di chuyển, skill, v.v.)
        chatInputField?.onSelect.AddListener(_ => InputManager.Instance?.SetInputEnabled(false));
        chatInputField?.onDeselect.AddListener(_ => InputManager.Instance?.SetInputEnabled(true));
    }

    private void Start()
    {
        ResolveFriendPanel();
        EnsureSubscribed();
        UpdateChannelLabel();
    }

    private void Update()
    {
        if (!_chatManagerSubscribed)
            EnsureSubscribed();
    }

    private void OnEnable()
    {
        // Setup tabs luôn chạy — không phụ thuộc ChatManager
        UIPanelManager.CloseOthers(gameObject);
        UIPanelManager.NotifyOpened(gameObject);
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, true);
        InputManager.Instance?.CancelAutoMove();
        ResolveFriendPanel();
        tabBar?.SetupTabs(OnTabSelected);
        UpdateChannelLabel();
        EnsureSubscribed();
        ReloadHistory();
    }

    private void OnDisable()
    {
        UIPanelManager.NotifyClosed(gameObject);
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, false);
        InputManager.Instance?.SetInputEnabled(true);

        if (_chatManagerSubscribed && ChatManager.Instance != null)
        {
            ChatManager.Instance.OnMessageReceived   -= OnMessageReceived;
            ChatManager.Instance.OnConnectionChanged -= OnConnectionChanged;
        }
        _chatManagerSubscribed = false;
    }

    private void OnDestroy()
    {
        UIPanelManager.Unregister(gameObject);
        if (Instance == this)
            Instance = null;
    }

    // Đăng ký nhận event từ ChatManager. Gọi nhiều lần được — chỉ đăng ký 1 lần.
    // Cần gọi cả trong OnEnable lẫn trước khi gửi vì ChatManager có thể khởi tạo muộn.
    private void EnsureSubscribed()
    {
        if (_chatManagerSubscribed) return;
        if (ChatManager.Instance == null) return;
        ChatManager.Instance.OnMessageReceived   += OnMessageReceived;
        ChatManager.Instance.OnConnectionChanged += OnConnectionChanged;
        _chatManagerSubscribed = true;
        _isConnected = ChatManager.Instance.IsConnected;  // đồng bộ trạng thái ngay khi subscribe
        ReloadHistory();
    }

    // Tab

    private void OnTabSelected(ChatChannel ch)
    {
        // Nếu tab "Riêng" không có target thì yêu cầu chọn bạn bè
        if (ch == ChatChannel.Private && string.IsNullOrEmpty(ChatManager.Instance?.PrivateChatTargetId))
        {
            var friendUi = ResolveFriendPanel();
            if (friendUi != null)
            {
                { /* Private tab selected without target. Opening FriendListUI */ }
                friendUi.ShowPanel("ChatPanelUI.PrivateTab");
            }
            else
            {
                { /* Cảnh báo: Could not resolve FriendListUI from loaded scenes */ }
            }
            return;
        }
        if (ChatManager.Instance != null)
            ChatManager.Instance.CurrentSendChannel = ch;
        UpdateChannelLabel();
        ReloadHistory();
    }

    // Channel Icon (Dropdown)

    private void OnChannelIconClicked()
    {
        if (channelDropdown != null)
            channelDropdown.Toggle(OnChannelDropdownSelected);
    }

    private void OnChannelDropdownSelected(ChatChannel ch)
    {
        if (ChatManager.Instance != null)
            ChatManager.Instance.CurrentSendChannel = ch;
        UpdateChannelLabel();
        tabBar?.SelectTab(ch);
        ReloadHistory();
    }

    private void UpdateChannelLabel()
    {
        var ch = ChatManager.Instance?.CurrentSendChannel ?? ChatChannel.World;
        if (channelIconLabel != null) channelIconLabel.text = ch.ShortCode();
        if (channelNameLabel != null)
        {
            channelNameLabel.text = _isConnected
                ? ch.DisplayName()
                : ch.DisplayName() + " (offline)";
        }
        UpdateChannelIconGraphic(ch);
    }

    private void EnsureChannelIconGraphic()
    {
        if (channelIconImage != null || channelIconButton == null) return;

        var iconTransform = channelIconButton.transform.Find("ChannelIconImage");
        if (iconTransform != null)
        {
            channelIconImage = iconTransform.GetComponent<Image>();
            if (channelIconImage == null)
                channelIconImage = iconTransform.gameObject.AddComponent<Image>();
        }
        else
        {
            var iconGo = new GameObject("ChannelIconImage", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(channelIconButton.transform, false);

            var rt = iconGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(6f, 6f);
            rt.offsetMax = new Vector2(-6f, -6f);

            channelIconImage = iconGo.GetComponent<Image>();
        }

        channelIconImage.raycastTarget = false;
        channelIconImage.preserveAspect = true;
        channelIconImage.color = Color.white;
        channelIconImage.enabled = false;
        channelIconImage.transform.SetAsLastSibling();
    }

    private void UpdateChannelIconGraphic(ChatChannel ch)
    {
        EnsureChannelIconGraphic();

        if (channelIconImage == null)
        {
            if (channelIconLabel != null) channelIconLabel.gameObject.SetActive(true);
            return;
        }

        if (channelDropdown != null && channelDropdown.TryGetChannelItem(ch, out var item) && item.icon != null)
        {
            channelIconImage.sprite = item.icon;
            channelIconImage.color = Color.white;
            channelIconImage.enabled = true;
            channelIconImage.gameObject.SetActive(true);
            channelIconImage.transform.SetAsLastSibling();
            if (channelIconLabel != null) channelIconLabel.gameObject.SetActive(false);
            return;
        }

        channelIconImage.enabled = false;
        if (channelIconLabel != null) channelIconLabel.gameObject.SetActive(true);
    }

    private FriendListUI ResolveFriendPanel()
    {
        if (friendListPanel != null && friendListPanel.scene.IsValid() && friendListPanel.scene.isLoaded)
        {
            var sceneFriendPanel = friendListPanel.GetComponent<FriendListUI>();
            if (sceneFriendPanel != null)
                return sceneFriendPanel;

            { /* Cảnh báo: friendListPanel points to a loaded scene object without FriendListUI. Re-resolving scene instance */ }
        }
        else if (friendListPanel != null)
        {
            { /* Cảnh báo: Ignoring friendListPanel reference that is not part of a loaded scene. Re-resolving scene instance */ }
        }

        var resolved = FindObjectOfType<FriendListUI>(includeInactive: true);
        friendListPanel = resolved != null ? resolved.gameObject : null;
        return resolved;
    }

    // Receiving

    private void OnMessageReceived(ChatMessageDto msg)
    {
        var currentCh = ChatManager.Instance?.CurrentSendChannel ?? ChatChannel.World;

        // Hiển thị nếu tin thuộc kênh đang xem
        if (msg.GetChannel() == currentCh || currentCh == ChatChannel.Private && msg.GetChannel() == ChatChannel.Private)
            AppendMessage(msg);
    }

    private void OnConnectionChanged(bool connected)
    {
        _isConnected = connected;
        UpdateChannelLabel();
    }

    // Message Display

    private void AppendMessage(ChatMessageDto msg)
    {
        if (messageContent == null) return;

        var entryPrefab = ResolveMessageEntryPrefab();
        if (entryPrefab == null)
            return;

        GameObject go = Instantiate(entryPrefab, messageContent, false);

        InitializeMessageEntry(go, msg);
        _msgObjects.Enqueue(go);
        if (_msgObjects.Count > MAX_DISPLAYED)
            Destroy(_msgObjects.Dequeue());

        Canvas.ForceUpdateCanvases();
        if (messageContent is RectTransform contentRect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        if (messageScrollRect != null)
            messageScrollRect.normalizedPosition = new Vector2(0, 0);
    }

    private void ReloadHistory()
    {
        if (messageContent == null) return;

        // Xóa tin cũ
        foreach (var go in _msgObjects) Destroy(go);
        _msgObjects.Clear();

        // Load lại từ history
        var ch      = ChatManager.Instance?.CurrentSendChannel ?? ChatChannel.World;
        var history = ChatManager.Instance?.GetHistory(ch);
        if (history != null)
            foreach (var msg in history)
                AppendMessage(msg);
    }

    // Sending

    private void OnSendClicked()
    {
        if (chatInputField == null) return;
        var text = chatInputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        // Đảm bảo subscribe trước khi gửi (ChatManager có thể vừa khởi tạo)
        EnsureSubscribed();

        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.SendChatMessage(text);
        }
        else
        {
            // ChatManager chưa sẵn sàng — hiển thị local echo trực tiếp
            var echo = new ChatMessageDto
            {
                senderId   = "me",
                senderName = "Bạn",
                channel    = "world",
                message    = text,
                timestamp  = System.DateTime.Now.ToString("HH:mm")
            };
            AppendMessage(echo);
        }

        chatInputField.text = "";
        chatInputField.ActivateInputField();
    }

    // Toggle visibility

    public void Toggle() => gameObject.SetActive(!gameObject.activeSelf);

    public void OpenOnGroupTab()
    {
        gameObject.SetActive(true);
        if (ChatManager.Instance != null)
            ChatManager.Instance.CurrentSendChannel = ChatChannel.Group;

        tabBar?.SelectTab(ChatChannel.Group);
        UpdateChannelLabel();
        ReloadHistory();
    }

    // Mở thẳng tab chat riêng với người dùng chỉ định.
    public void OpenPrivateChat(int targetUserId, string targetUsername)
    {
        gameObject.SetActive(true);
        if (ChatManager.Instance != null)
        {
            ChatManager.Instance.PrivateChatTargetId   = targetUserId.ToString();
            ChatManager.Instance.PrivateChatTargetName = targetUsername;
            ChatManager.Instance.CurrentSendChannel    = ChatChannel.Private;
        }
        tabBar?.SelectTab(ChatChannel.Private);
        UpdateChannelLabel();
        ReloadHistory();
    }

    private void InitializeMessageEntry(GameObject go, ChatMessageDto msg)
    {
        if (go == null) return;

        var richEntry = go.GetComponent<ChatMessageEntryUI>();
        if (richEntry != null)
        {
            richEntry.Setup(msg);
            return;
        }

        var tmp = go.GetComponent<TextMeshProUGUI>() ?? go.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp == null) return;

        // Đặt text trước khi cấu hình layout để tránh trường hợp layout setup
        // ném exception (ví dụ trên prefab đã có sẵn Graphic) làm text gốc
        // của prefab không bị ghi đè.
        tmp.text = BuildMessageMarkup(msg);
        ConfigureSimpleEntryLayout(go, tmp);
    }

    private GameObject ResolveMessageEntryPrefab()
    {
        if (messageEntryPrefab != null && !messageEntryPrefab.scene.IsValid())
            return messageEntryPrefab;

        if (messageEntryPrefab != null && messageEntryPrefab.scene.IsValid())
        {
            { /* Cảnh báo: messageEntryPrefab đang trỏ tới scene object. Sẽ nạp lại prefab từ Resources */ }
            messageEntryPrefab = null;
        }

        if (_attemptedMessageEntryPrefabResolve)
            return messageEntryPrefab;

        _attemptedMessageEntryPrefabResolve = true;
        messageEntryPrefab = TryLoadMessageEntryPrefab(DefaultMessageEntryResourcesPath)
                             ?? TryLoadMessageEntryPrefab(LegacyMessageEntryResourcesPath);

        if (messageEntryPrefab == null)
        {
            { /* Lỗi: Không tìm thấy message entry prefab tại Resources/{DefaultMessageEntryResourcesPath} hoặc Resources/{LegacyMessageEntryResourcesPath} */ }
        }

        return messageEntryPrefab;
    }

    private GameObject TryLoadMessageEntryPrefab(string resourcesPath)
    {
        var prefab = Resources.Load<GameObject>(resourcesPath);
        if (prefab != null)
        {
            { /* Cảnh báo: messageEntryPrefab chưa được gán trên ChatPanel instance. Đã nạp fallback prefab từ Resources/{resourcesPath} */ }
        }

        return prefab;
    }

    private static string BuildMessageMarkup(ChatMessageDto msg)
    {
        var channel = msg.GetChannel();
        string colorHex = ColorUtility.ToHtmlStringRGB(channel.MessageColor());
        string timestampHex = ColorUtility.ToHtmlStringRGB(FallbackTimestampColor);
        string messageHex = ColorUtility.ToHtmlStringRGB(FallbackMessageTextColor);
        return $"<color=#{timestampHex}>{msg.timestamp}</color>  " +
               $"<color=#{colorHex}>[{msg.senderName}]</color>  " +
               $"<color=#{messageHex}>{msg.message}</color>";
    }

    private static void ConfigureSimpleEntryLayout(GameObject go, TextMeshProUGUI tmp)
    {
        var rect = go.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        var layout = go.GetComponent<LayoutElement>();
        if (layout == null)
            layout = go.AddComponent<LayoutElement>();

        layout.minHeight = 34f;
        layout.preferredHeight = -1f;
        layout.flexibleWidth = 1f;
        layout.flexibleHeight = -1f;
        layout.layoutPriority = 1;

        var bg = go.GetComponent<Image>();
        // Một GameObject chỉ chứa được 1 Graphic. Nếu prefab đã đặt
        // TextMeshProUGUI trên root thì không thể (và không cần) thêm Image
        // làm background tại đây — bỏ qua để tránh NullReferenceException.
        if (bg == null)
        {
            var existingGraphic = go.GetComponent<Graphic>();
            if (existingGraphic == null)
                bg = go.AddComponent<Image>();
        }

        if (bg != null)
        {
            bg.raycastTarget = false;
            bg.color = new Color32(0xFF, 0xF6, 0xEA, 0x78);
        }

        tmp.fontSize = Mathf.Max(tmp.fontSize, 18f);
        tmp.enableAutoSizing = false;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;
        tmp.margin = new Vector4(8f, 4f, 8f, 6f);
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.overflowMode = TextOverflowModes.Overflow;
    }

}
