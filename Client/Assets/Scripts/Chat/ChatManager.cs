using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Singleton quản lý toàn bộ chat:
// - Kết nối SignalR ChatHub
// - Gửi/nhận tin theo từng kênh
// - Phát event cho UI đăng ký lắng nghe
public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    // Đăng ký và xử lý sự kiện phát sinh trong runtime.

    // Khi nhận được tin nhắn bất kỳ.
    public event Action<ChatMessageDto> OnMessageReceived;

    // Khi nhận tin riêng.
    public event Action<ChatMessageDto> OnPrivateMessageReceived;

    // Khi kết nối thay đổi trạng thái.
    public event Action<bool> OnConnectionChanged;   // true = connected

    // State

    public bool IsConnected => _client != null && _client.IsConnected;

    // Channel đang chọn để gửi tin.
    public ChatChannel CurrentSendChannel { get; set; } = ChatChannel.World;

    // Khi chat riêng: userId của người đang chat.
    public string PrivateChatTargetId   { get; set; } = "";
    public string PrivateChatTargetName { get; set; } = "";

    // Context: tự động join group khi thay đổi
    public string CurrentMapId   { get; private set; } = "";
    public string CurrentClanId  { get; private set; } = "";
    public string CurrentClassId { get; private set; } = "";
    public string CurrentGroupId { get; private set; } = "";

    // Lịch sử tin nhắn (tối đa 100 / kênh)
    private readonly Dictionary<ChatChannel, List<ChatMessageDto>> _history
        = new Dictionary<ChatChannel, List<ChatMessageDto>>();

    private SignalRClient _client;
    private string        _hubUrl;
    private bool          _isConnecting;  // tránh gọi ConnectToHub song song

    // MonoBehaviour

    private void Awake()
    {
        if (transform.parent != null)
            transform.SetParent(null, true);

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        { /* Awake: root='{gameObject.name}' active={gameObject.activeInHierarchy} */ }

        foreach (ChatChannel ch in Enum.GetValues(typeof(ChatChannel)))
            _history[ch] = new List<ChatMessageDto>();
    }

    private void Start()
    {
        { /* Start: chuẩn bị kết nối ChatHub */ }
        // Đăng ký sự kiện player data (cho reconnect / scenario đăng nhập muộn)
        GameManager.OnPlayerDataSet += OnPlayerDataSet;

        // Thử kết nối NGAY khi start nếu JWT_TOKEN đã có trong PlayerPrefs
        // KHÔNG đợi currentPlayerData — JWT_TOKEN có trước khi player data load xong
        AutoConnect();

        // Kiểm tra định kỳ — nếu chưa kết nối thì tự thử lại
        StartCoroutine(PeriodicConnectionCheck());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        { /* OnDestroy: '{gameObject.name}' */ }
        GameManager.OnPlayerDataSet -= OnPlayerDataSet;
    }

    // Connection

    private void OnPlayerDataSet(PlayerDataResponse data)
    {
        AutoConnect();
        SyncDisplayName();
    }

    private void AutoConnect()
    {
        if (IsConnected || _isConnecting) return;

        string token = AuthHelper.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            // JWT_TOKEN chưa có — im lặng, PeriodicCheck sẽ thử lại
            return;
        }

        { /* Đang kết nối ChatHub */ }

        string apiRoot = ServerAddressConfig.Instance != null
            ? ServerAddressConfig.Instance.ApiRoot
            : "http://localhost:5000";

        _hubUrl = apiRoot.TrimEnd('/') + "/chathub";
        _isConnecting = true;
        ConnectToHub(token);
    }

    public void ConnectToHub(string jwtToken)
    {
        if (_client != null)
        {
            _client.OnConnected    -= HandleConnected;
            _client.OnDisconnected -= HandleDisconnected;
            _client.OnError        -= HandleError;
            Destroy(_client);
        }

        _client = gameObject.AddComponent<SignalRClient>();
        _client.OnConnected    += HandleConnected;
        _client.OnDisconnected += HandleDisconnected;
        _client.OnError        += HandleError;

        // Đăng ký tất cả kênh nhận
        _client.On("ReceiveWorldMessage",     json => ReceiveMessage(json, ChatChannel.World));
        _client.On("ReceiveProximityMessage", json => ReceiveMessage(json, ChatChannel.Proximity));
        _client.On("ReceiveClanMessage",      json => ReceiveMessage(json, ChatChannel.Clan));
        _client.On("ReceiveClassMessage",     json => ReceiveMessage(json, ChatChannel.Class));
        _client.On("ReceiveGroupMessage",     json => ReceiveMessage(json, ChatChannel.Group));
        _client.On("ReceivePrivateMessage",   json => ReceivePrivateMessage(json));
        _client.On("ReceiveSystemMessage",    json => ReceiveSystemMessage(json));

        _client.Connect(_hubUrl, jwtToken);
    }

    // Hub callbacks

    private void HandleConnected()
    {
        _isConnecting = false;
        { /* Đã kết nối ChatHub */ }
        OnConnectionChanged?.Invoke(true);
        SyncDisplayName();

        // Join các group hiện tại
        if (!string.IsNullOrEmpty(CurrentMapId))   JoinMap(CurrentMapId);
        if (!string.IsNullOrEmpty(CurrentClanId))  JoinClan(CurrentClanId);
        if (!string.IsNullOrEmpty(CurrentClassId)) JoinClass(CurrentClassId);
        if (!string.IsNullOrEmpty(CurrentGroupId)) JoinGroup(CurrentGroupId);
    }

    private void HandleDisconnected(string reason)
    {
        _isConnecting = false;
        { /* Cảnh báo: Ngắt kết nối: {reason} */ }
        OnConnectionChanged?.Invoke(false);
        StartCoroutine(ReconnectAfterDelay(5f));
    }

    private void HandleError(string err)
    {
        _isConnecting = false;
        { /* Lỗi: Lỗi: {err} */ }
        StartCoroutine(ReconnectAfterDelay(8f));
    }

    // Thử kết nối lại sau một khoảng thời gian.
    private IEnumerator ReconnectAfterDelay(float delaySec)
    {
        yield return new WaitForSeconds(delaySec);
        AutoConnect();
    }

    // Cứ 3 giây kiểm tra một lần — nếu chưa kết nối và có JWT thì thử lại.
    private IEnumerator PeriodicConnectionCheck()
    {
        var wait = new WaitForSeconds(3f);
        while (true)
        {
            yield return wait;
            if (!IsConnected && !_isConnecting)
            {
                string t = AuthHelper.GetToken();
                if (!string.IsNullOrEmpty(t))
                {
                    { /* PeriodicCheck: chưa kết nối, đang thử lại */ }
                    AutoConnect();
                }
            }
        }
    }

    // Receive

    private void ReceiveMessage(string json, ChatChannel ch)
    {
        var msg = ChatMessageDto.FromJson(json);
        msg.channel = ch.ToString().ToLower();
        { /* Receive {ch}: from='{msg.senderName}' text='{msg.message}' */ }
        AddHistory(ch, msg);
        OnMessageReceived?.Invoke(msg);
    }

    private void ReceivePrivateMessage(string json)
    {
        var msg = ChatMessageDto.FromJson(json);
        msg.channel = "private";
        AddHistory(ChatChannel.Private, msg);
        OnMessageReceived?.Invoke(msg);
        OnPrivateMessageReceived?.Invoke(msg);
    }

    private void ReceiveSystemMessage(string json)
    {
        var msg = ChatMessageDto.FromJson(json);
        msg.channel = "system";
        // Hiển thị trong tab Proximity (lân cận) và World để dễ thấy
        AddHistory(ChatChannel.Proximity, msg);
        OnMessageReceived?.Invoke(msg);

        // INVALIDATE CACHE: If the system message is about adding item
        // e.g. "Đã thêm ... vào túi đồ"
        if (!string.IsNullOrEmpty(msg.message) && msg.message.Contains("Đã thêm") && msg.message.Contains("vào túi đồ"))
        {
            var bridge = InventoryNetworkBridge.GetExisting(true);
            if (bridge != null)
            {
                { /* Phát hiện thông báo thêm item, tiến hành vô hiệu hóa cache túi đồ */ }
                bridge.InvalidateInventoryCache();
                
                // Nếu UI đang mở thì force tải lại
                var inventoryUI = FindObjectOfType<InventoryUI>(true);
                if (inventoryUI != null && inventoryUI.gameObject.activeInHierarchy)
                {
                    bridge.RefreshInventoryFromDB();
                }
            }
        }
    }

    private void AddHistory(ChatChannel ch, ChatMessageDto msg)
    {
        var list = _history[ch];
        list.Add(msg);
        if (list.Count > 100) list.RemoveAt(0);
    }

    private void SyncDisplayName()
    {
        if (!IsConnected || _client == null)
            return;

        string displayName = GameManager.Instance?.currentPlayerData?.character_name;
        if (string.IsNullOrWhiteSpace(displayName))
            return;

        _client.Invoke("UpdateDisplayName", displayName.Trim());
    }

    // Send

    public void SendChatMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        text = text.Trim();

        { /* Send: channel={CurrentSendChannel} connected={IsConnected} text='{text}' */ }

        // Khi đã kết nối server: chỉ gửi lên server, server sẽ tự echo lại → tránh hiển thị 2 lần
        if (IsConnected)
        {
            // Lệnh đặc biệt (bắt đầu bằng "item ") → luôn gửi qua WorldMessage
            // để không bị chặn bởi điều kiện CurrentMapId/ClanId...
            bool isCommand = text.StartsWith("item ", System.StringComparison.OrdinalIgnoreCase);
            if (isCommand)
            {
                { /* Detected command, routing via SendWorldMessage: '{text}' */ }
                _client.Invoke("SendWorldMessage", text);
                return;
            }

            switch (CurrentSendChannel)
            {
                case ChatChannel.World:
                    _client.Invoke("SendWorldMessage", text);
                    break;
                case ChatChannel.Proximity:
                    if (!string.IsNullOrEmpty(CurrentMapId))
                        _client.Invoke("SendProximityMessage", CurrentMapId, text);
                    else
                        _client.Invoke("SendWorldMessage", text); // fallback
                    break;
                case ChatChannel.Clan:
                    if (!string.IsNullOrEmpty(CurrentClanId))
                        _client.Invoke("SendClanMessage", CurrentClanId, text);
                    break;
                case ChatChannel.Class:
                    if (!string.IsNullOrEmpty(CurrentClassId))
                        _client.Invoke("SendClassMessage", CurrentClassId, text);
                    break;
                case ChatChannel.Group:
                    if (!string.IsNullOrEmpty(CurrentGroupId))
                        _client.Invoke("SendGroupMessage", CurrentGroupId, text);
                    break;
                case ChatChannel.Private:
                    if (!string.IsNullOrEmpty(PrivateChatTargetId))
                        _client.Invoke("SendPrivateMessage", PrivateChatTargetId, text);
                    break;
            }
            return;
        }

        // Offline: hiển thị local echo để người dùng biết tin đã gửa (không có server echo)
        string myId   = GameManager.Instance?.currentPlayerData?.player_id.ToString() ?? "me";
        string myName = GameManager.Instance?.currentPlayerData?.character_name ?? "Bạn";
        var echo = new ChatMessageDto
        {
            senderId   = myId,
            senderName = myName,
            channel    = CurrentSendChannel.ToString().ToLower(),
            targetId   = CurrentSendChannel == ChatChannel.Private ? PrivateChatTargetId : "",
            message    = text,
            timestamp  = System.DateTime.Now.ToString("HH:mm")
        };
        AddHistory(CurrentSendChannel, echo);
        OnMessageReceived?.Invoke(echo);
    }

    public void SendPrivateTo(string targetUserId, string targetUsername, string text)
    {
        if (!IsConnected || string.IsNullOrWhiteSpace(text)) return;
        PrivateChatTargetId   = targetUserId;
        PrivateChatTargetName = targetUsername;
        CurrentSendChannel    = ChatChannel.Private;
        _client.Invoke("SendPrivateMessage", targetUserId, text.Trim());
    }

    // Group Management

    public void JoinMap(string mapId)
    {
        CurrentMapId = mapId;
        if (IsConnected) _client.Invoke("JoinMap", mapId);
    }

    public void LeaveMap(string mapId)
    {
        if (IsConnected) _client.Invoke("LeaveMap", mapId);
        if (CurrentMapId == mapId) CurrentMapId = "";
    }

    public void JoinClan(string clanId)
    {
        CurrentClanId = clanId;
        if (IsConnected) _client.Invoke("JoinClan", clanId);
    }

    public void JoinClass(string classType)
    {
        CurrentClassId = classType;
        if (IsConnected) _client.Invoke("JoinClass", classType);
    }

    public void JoinGroup(string groupId)
    {
        CurrentGroupId = groupId;
        if (IsConnected) _client.Invoke("JoinGroup", groupId);
    }

    public void LeaveGroup(string groupId)
    {
        if (IsConnected) _client.Invoke("LeaveGroup", groupId);
        if (CurrentGroupId == groupId) CurrentGroupId = "";
    }

    // History

    public IReadOnlyList<ChatMessageDto> GetHistory(ChatChannel ch)
        => _history.TryGetValue(ch, out var list) ? list : Array.AsReadOnly(Array.Empty<ChatMessageDto>());
}
