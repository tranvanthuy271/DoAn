using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PartyManager : MonoBehaviour
{
    private const string LogPrefix = "[PartyManager]";
    private const string ChatManagerResourcePath = "Prefabs/Chat/ChatManager";

    public static PartyManager Instance { get; private set; }

    public static PartyManager EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.AutoConnect();
            return Instance;
        }

        var existing = FindObjectOfType<PartyManager>(includeInactive: true);
        if (existing != null)
        {
            Instance = existing;

            if (existing.transform.parent != null)
                existing.transform.SetParent(null, true);

            if (!existing.gameObject.activeSelf)
                existing.gameObject.SetActive(true);

            DontDestroyOnLoad(existing.gameObject);
            existing.AutoConnect();
            { /* {LogPrefix} EnsureInstance resolved existing scene object '{existing.gameObject.name}' active={existing.gameObject.activeInHierarchy} scene='{existing.gameObject.scene.name}' */ }
            return Instance;
        }

        var chatManager = FindObjectOfType<ChatManager>(includeInactive: true);
        if (chatManager != null)
        {
            Instance = chatManager.GetComponent<PartyManager>();
            if (Instance == null)
                Instance = chatManager.gameObject.AddComponent<PartyManager>();

            if (chatManager.transform.parent != null)
                chatManager.transform.SetParent(null, true);

            if (!chatManager.gameObject.activeSelf)
                chatManager.gameObject.SetActive(true);

            DontDestroyOnLoad(chatManager.gameObject);
            Instance.AutoConnect();
            { /* {LogPrefix} EnsureInstance attached to existing ChatManager '{chatManager.gameObject.name}' */ }
            return Instance;
        }

        var prefab = Resources.Load<GameObject>(ChatManagerResourcePath);
        if (prefab != null)
        {
            var instanceGo = Instantiate(prefab);
            instanceGo.name = prefab.name;

            Instance = instanceGo.GetComponent<PartyManager>();
            if (Instance == null)
                Instance = instanceGo.AddComponent<PartyManager>();

            Instance.AutoConnect();
            { /* {LogPrefix} EnsureInstance instantiated prefab '{ChatManagerResourcePath}' -> hasPartyManager={Instance != null} */ }
            return Instance;
        }

        var go = new GameObject("PartyManager [Auto]");
        Instance = go.AddComponent<PartyManager>();
        Instance.AutoConnect();
        { /* Cảnh báo: {LogPrefix} EnsureInstance created standalone fallback GameObject because ChatManager prefab was not found */ }
        return Instance;
    }

    public event Action<PartyStatePayload> OnPartyStateChanged;
    public event Action<PartyInvitePayload> OnInviteReceived;
    public event Action<PartyJoinRequestPayload> OnJoinRequestReceived;
    public event Action<PartySearchResultPayload> OnSearchResultsUpdated;
    public event Action<NearbyPlayersPayload> OnNearbyPlayersUpdated;
    public event Action<PartyDungeonRequestPayload> OnPartyDungeonRequested;
    public event Action<string> OnError;
    public event Action<bool> OnConnectionChanged;

    public PartyStatePayload CurrentParty { get; private set; }
    public PartySearchResultPayload LatestSearchResults { get; private set; } = new PartySearchResultPayload();
    public NearbyPlayersPayload LatestNearbyPlayers { get; private set; } = new NearbyPlayersPayload();
    public bool IsConnected => _client != null && _client.IsConnected;
    public bool HasParty => CurrentParty != null && !string.IsNullOrWhiteSpace(CurrentParty.partyId);
    public bool IsLeader => HasParty && string.Equals(CurrentParty.leaderUserId, ResolveLocalUserId(), StringComparison.Ordinal);

    private SignalRClient _client;
    private string _hubUrl = string.Empty;
    private bool _isConnecting;
    private Coroutine _presenceCoroutine;
    private string _joinedChatGroupId = string.Empty;
    private readonly Dictionary<string, Action> _pendingConnectedActions = new Dictionary<string, Action>(StringComparer.Ordinal);

    private void Awake()
    {
        if (transform.parent != null)
            transform.SetParent(null, true);

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        GameManager.OnPlayerDataSet += OnPlayerDataSet;
        AutoConnect();
        StartCoroutine(PeriodicConnectionCheck());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        GameManager.OnPlayerDataSet -= OnPlayerDataSet;
    }

    public void CreateParty()
    {
        InvokeOrQueue(nameof(CreateParty), () =>
        {
            { /* {LogPrefix} Invoke CreateParty | user={ResolveLocalUserId()} map={ResolveCurrentMapId()} zone={ResolveCurrentZoneId()} */ }
            _client.Invoke("CreateParty");
        });
    }

    public void InviteMember(string targetUserId)
    {
        if (string.IsNullOrWhiteSpace(targetUserId)) return;

        string trimmedTargetUserId = targetUserId.Trim();
        InvokeOrQueue($"{nameof(InviteMember)}:{trimmedTargetUserId}", () =>
        {
            { /* {LogPrefix} Invoke InviteMember | targetUserId={trimmedTargetUserId} */ }
            _client.Invoke("InviteMember", trimmedTargetUserId);
        });
    }

    public void RequestJoinParty(string partyId)
    {
        if (string.IsNullOrWhiteSpace(partyId)) return;

        string trimmedPartyId = partyId.Trim();
        InvokeOrQueue($"{nameof(RequestJoinParty)}:{trimmedPartyId}", () =>
        {
            { /* {LogPrefix} Invoke RequestJoinParty | partyId={trimmedPartyId} */ }
            _client.Invoke("RequestJoinParty", trimmedPartyId);
        });
    }

    public void AcceptJoinRequest(string partyId, string requesterUserId)
    {
        string safePartyId = partyId ?? string.Empty;
        string safeRequesterUserId = requesterUserId ?? string.Empty;
        InvokeOrQueue($"{nameof(AcceptJoinRequest)}:{safePartyId}:{safeRequesterUserId}", () =>
        {
            { /* {LogPrefix} Invoke AcceptJoinRequest | partyId={safePartyId} requesterUserId={safeRequesterUserId} */ }
            _client.Invoke("AcceptJoinRequest", safePartyId, safeRequesterUserId);
        });
    }

    public void RejectJoinRequest(string partyId, string requesterUserId)
    {
        string safePartyId = partyId ?? string.Empty;
        string safeRequesterUserId = requesterUserId ?? string.Empty;
        InvokeOrQueue($"{nameof(RejectJoinRequest)}:{safePartyId}:{safeRequesterUserId}", () =>
        {
            { /* {LogPrefix} Invoke RejectJoinRequest | partyId={safePartyId} requesterUserId={safeRequesterUserId} */ }
            _client.Invoke("RejectJoinRequest", safePartyId, safeRequesterUserId);
        });
    }

    public void LeaveParty()
    {
        InvokeOrQueue(nameof(LeaveParty), () =>
        {
            { /* {LogPrefix} Invoke LeaveParty | currentPartyId={CurrentParty?.partyId} */ }
            _client.Invoke("LeaveParty");
        });
    }

    public void DisbandParty()
    {
        InvokeOrQueue(nameof(DisbandParty), () =>
        {
            { /* {LogPrefix} Invoke DisbandParty | currentPartyId={CurrentParty?.partyId} */ }
            _client.Invoke("DisbandParty");
        });
    }

    public void SetLock(bool locked)
    {
        InvokeOrQueue(nameof(SetLock), () =>
        {
            { /* {LogPrefix} Invoke SetLock | currentPartyId={CurrentParty?.partyId} locked={locked} */ }
            _client.Invoke("SetLock", locked ? "true" : "false");
        });
    }

    public void SetAutoAccept(bool autoAccept)
    {
        InvokeOrQueue(nameof(SetAutoAccept), () =>
        {
            { /* {LogPrefix} Invoke SetAutoAccept | currentPartyId={CurrentParty?.partyId} autoAccept={autoAccept} */ }
            _client.Invoke("SetAutoAccept", autoAccept ? "true" : "false");
        });
    }

    public void RefreshPartiesInCurrentZone()
    {
        InvokeOrQueue(nameof(RefreshPartiesInCurrentZone), () =>
        {
            int mapId = ResolveCurrentMapId();
            int zoneId = ResolveCurrentZoneId();
            { /* {LogPrefix} Invoke GetPartiesInZone | map={mapId} zone={zoneId} */ }
            _client.Invoke("GetPartiesInZone", mapId.ToString(), zoneId.ToString());
        });
    }

    public void RefreshNearbyPlayers()
    {
        InvokeOrQueue(nameof(RefreshNearbyPlayers), () =>
        {
            int mapId = ResolveCurrentMapId();
            int zoneId = ResolveCurrentZoneId();
            { /* {LogPrefix} Invoke GetNearbyPlayers | map={mapId} zone={zoneId} localName={ResolveCharacterName()} level={ResolveLevel()} */ }
            _client.Invoke("GetNearbyPlayers", mapId.ToString(), zoneId.ToString());
        });
    }

    public void StartPartyDungeon(int dungeonId, int mapId, string dungeonType)
    {
        string safeDungeonType = string.IsNullOrWhiteSpace(dungeonType) ? "multi" : dungeonType;
        InvokeOrQueue($"{nameof(StartPartyDungeon)}:{dungeonId}:{mapId}:{safeDungeonType}", () =>
        {
            { /* {LogPrefix} Invoke StartPartyDungeon | dungeonId={dungeonId} mapId={mapId} dungeonType={safeDungeonType} */ }
            _client.Invoke("StartPartyDungeon", dungeonId.ToString(), mapId.ToString(), safeDungeonType);
        });
    }

    private void OnPlayerDataSet(PlayerDataResponse _) => AutoConnect();

    private void AutoConnect()
    {
        if (IsConnected || _isConnecting)
            return;

        string token = ResolveJwtToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            { /* Cảnh báo: {LogPrefix} AutoConnect skipped because JWT token is empty */ }
            return;
        }

        _hubUrl = ServerAddressConfig.Instance.ApiRoot.TrimEnd('/') + "/partyhub";
        _isConnecting = true;
        { /* {LogPrefix} AutoConnect -> {_hubUrl} */ }
        ConnectToHub(token);
    }

    private void ConnectToHub(string jwtToken)
    {
        if (_client != null)
        {
            _client.OnConnected -= HandleConnected;
            _client.OnDisconnected -= HandleDisconnected;
            _client.OnError -= HandleError;
            Destroy(_client);
        }

        _client = gameObject.AddComponent<SignalRClient>();
        _client.OnConnected += HandleConnected;
        _client.OnDisconnected += HandleDisconnected;
        _client.OnError += HandleError;

        _client.On("PartyStateUpdated", json =>
        {
            CurrentParty = PartyStatePayload.FromJson(json);
            SyncChatGroup();
            OnPartyStateChanged?.Invoke(CurrentParty);
        });
        _client.On("PartyInviteReceived", json =>
        {
            var payload = PartyInvitePayload.FromJson(json);
            OnInviteReceived?.Invoke(payload);
        });
        _client.On("PartyJoinRequestReceived", json =>
        {
            var payload = PartyJoinRequestPayload.FromJson(json);
            OnJoinRequestReceived?.Invoke(payload);
        });
        _client.On("PartySearchResults", json =>
        {
            LatestSearchResults = PartySearchResultPayload.FromJson(json) ?? new PartySearchResultPayload();
            OnSearchResultsUpdated?.Invoke(LatestSearchResults);
        });
        _client.On("NearbyPlayersUpdated", json =>
        {
            LatestNearbyPlayers = NearbyPlayersPayload.FromJson(json) ?? new NearbyPlayersPayload();
            OnNearbyPlayersUpdated?.Invoke(LatestNearbyPlayers);
        });
        _client.On("PartyDisbanded", _ =>
        {
            { /* {LogPrefix} Event PartyDisbanded | clearing current party */ }
            CurrentParty = null;
            SyncChatGroup();
            OnPartyStateChanged?.Invoke(null);
        });
        _client.On("PartyDungeonRequested", json =>
        {
            var payload = PartyDungeonRequestPayload.FromJson(json);
            OnPartyDungeonRequested?.Invoke(payload);
            // Transfer thực tế do leader gửi RequestPartyDungeonEntryServerRpc — server tự transfer tất cả members
            { /* {LogPrefix} PartyDungeonRequested received | dungeonId={payload?.dungeonId}  server sẽ transfer */ }
        });
        _client.On("PartyError", json =>
        {
            string message = PartyErrorPayload.FromJson(json).message;
            { /* Cảnh báo: {LogPrefix} Event PartyError | message={(string.IsNullOrWhiteSpace(message) ? json : message)} raw={json} */ }
            OnError?.Invoke(string.IsNullOrWhiteSpace(message) ? json : message);
        });

        { /* {LogPrefix} ConnectToHub called */ }
        _client.Connect(_hubUrl, jwtToken);
    }

    private void HandleConnected()
    {
        _isConnecting = false;
        { /* {LogPrefix} Connected to party hub */ }
        OnConnectionChanged?.Invoke(true);

        FlushPendingConnectedActions();

        UpdatePresence();
        RefreshPartiesInCurrentZone();
        RefreshNearbyPlayers();

        if (_presenceCoroutine != null)
            StopCoroutine(_presenceCoroutine);
        _presenceCoroutine = StartCoroutine(PresenceHeartbeatCoroutine());
    }

    private void HandleDisconnected(string reason)
    {
        _isConnecting = false;
        { /* Cảnh báo: {LogPrefix} Disconnected from party hub. reason={reason} */ }
        OnConnectionChanged?.Invoke(false);

        if (_presenceCoroutine != null)
        {
            StopCoroutine(_presenceCoroutine);
            _presenceCoroutine = null;
        }

        StartCoroutine(ReconnectAfterDelay(4f));
    }

    private void HandleError(string error)
    {
        _isConnecting = false;
        { /* Lỗi: {LogPrefix} SignalR error: {error} */ }
        OnError?.Invoke(error);
        StartCoroutine(ReconnectAfterDelay(6f));
    }

    private IEnumerator PresenceHeartbeatCoroutine()
    {
        var wait = new WaitForSeconds(5f);
        while (true)
        {
            UpdatePresence();
            yield return wait;
        }
    }

    private IEnumerator ReconnectAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        AutoConnect();
    }

    private IEnumerator PeriodicConnectionCheck()
    {
        var wait = new WaitForSeconds(3f);
        while (true)
        {
            yield return wait;
            if (!IsConnected && !_isConnecting)
                AutoConnect();
        }
    }

    private void UpdatePresence()
    {
        if (!IsConnected) return;

        int mapId = ResolveCurrentMapId();
        int zoneId = ResolveCurrentZoneId();
        int level = ResolveLevel();
        string characterName = ResolveCharacterName();
        string className = ResolveClassName();
        string elementType = ResolveElementType();

        { /* {LogPrefix} UpdatePresence | map={mapId} zone={zoneId} level={level} name={characterName} class={className} element={elementType} */ }

        _client.Invoke(
            "UpdatePresence",
            mapId.ToString(),
            zoneId.ToString(),
            level.ToString(),
            characterName,
            className,
            elementType);
    }

    private void SyncChatGroup()
    {
        string newGroupId = HasParty ? CurrentParty.partyId : string.Empty;
        if (_joinedChatGroupId == newGroupId)
            return;

        { /* {LogPrefix} SyncChatGroup | old={_joinedChatGroupId} new={newGroupId} */ }

        if (ChatManager.Instance != null)
        {
            if (!string.IsNullOrWhiteSpace(_joinedChatGroupId))
                ChatManager.Instance.LeaveGroup(_joinedChatGroupId);

            if (!string.IsNullOrWhiteSpace(newGroupId))
            {
                ChatManager.Instance.JoinGroup(newGroupId);
                ChatManager.Instance.CurrentSendChannel = ChatChannel.Group;
            }
        }

        _joinedChatGroupId = newGroupId;
    }

    private void InvokeOrQueue(string actionName, Action invokeAction)
    {
        if (invokeAction == null)
            return;

        if (!EnsureConnected(actionName, invokeAction))
            return;

        invokeAction();
    }

    private bool EnsureConnected(string actionName, Action deferredAction = null)
    {
        if (IsConnected)
            return true;

        if (deferredAction != null)
        {
            bool replaced = _pendingConnectedActions.ContainsKey(actionName);
            _pendingConnectedActions[actionName] = deferredAction;
            { /* {LogPrefix} {(replaced ? */ }
        }

        { /* Cảnh báo: {LogPrefix} '{actionName}' skipped because PartyManager is not connected. connecting={_isConnecting} hubUrl={_hubUrl} jwtExists={!string.IsNullOrWhiteSpace(ResolveJwtToken())} */ }

        if (!_isConnecting)
            AutoConnect();

        OnError?.Invoke($"PartyManager chưa kết nối khi gọi {actionName}.");
        return false;
    }

    private void FlushPendingConnectedActions()
    {
        if (_pendingConnectedActions.Count == 0)
            return;

        var pendingActions = _pendingConnectedActions.ToArray();
        _pendingConnectedActions.Clear();

        foreach (var pendingAction in pendingActions)
        {
            try
            {
                { /* {LogPrefix} Flushing deferred action '{pendingAction.Key}' */ }
                pendingAction.Value?.Invoke();
            }
            catch (Exception ex)
            {
                { /* Lỗi: {LogPrefix} Deferred action '{pendingAction.Key}' failed: {ex} */ }
            }
        }
    }

    private IEnumerator RequestDungeonEntryCoroutine(PartyDungeonRequestPayload payload)
    {
        if (payload == null || payload.dungeonId <= 0)
            yield break;

        if (DungeonManager.Instance == null || GameplayCommandService.Instance == null)
            yield break;

        bool done = false;
        DungeonConfigData found = null;

        void HandleDungeonList(string json)
        {
            GameplayCommandService.OnDungeonListReceived -= HandleDungeonList;
            if (!string.IsNullOrWhiteSpace(json) && !json.Contains("\"error\""))
            {
                var response = JsonUtility.FromJson<DungeonListResponse>(json);
                if (response?.dungeons != null)
                    found = response.dungeons.FirstOrDefault(d => d.dungeon_id == payload.dungeonId);
            }

            done = true;
        }

        GameplayCommandService.OnDungeonListReceived -= HandleDungeonList;
        GameplayCommandService.OnDungeonListReceived += HandleDungeonList;
        GameplayCommandService.Instance.GetDungeonListServerRpc();

        yield return new WaitUntil(() => done);
        if (found != null)
            DungeonManager.Instance.EnterDungeon(found);
    }

    private static string ResolveLocalUserId()
    {
        // GameManager takes priority — PlayerPrefs is shared across ParrelSync clones on Windows
        if (GameManager.Instance?.currentPlayerData != null && GameManager.Instance.currentPlayerData.user_id > 0)
            return GameManager.Instance.currentPlayerData.user_id.ToString();

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        return userId > 0 ? userId.ToString() : "0";
    }

    private static string ResolveJwtToken()
    {
        if (APIClient.Instance != null)
        {
            string runtimeToken = APIClient.Instance.GetToken();
            if (!string.IsNullOrWhiteSpace(runtimeToken))
                return runtimeToken;
        }

        return AuthHelper.GetToken();
    }

    private static int ResolveCurrentMapId()
    {
        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentMapId >= 0)
            return ClientSceneController.Instance.CurrentMapId;

        if (MapManager.Instance != null && MapManager.Instance.GetMapId() >= 0)
            return MapManager.Instance.GetMapId();

        if (GameManager.Instance?.currentPlayerData != null)
            return GameManager.Instance.currentPlayerData.map_id;

        return 0;
    }

    private static int ResolveCurrentZoneId()
    {
        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentZoneId >= 0)
            return ClientSceneController.Instance.CurrentZoneId;

        return 0;
    }

    private static string ResolveCharacterName()
    {
        if (!string.IsNullOrWhiteSpace(GameManager.Instance?.currentPlayerData?.character_name))
            return GameManager.Instance.currentPlayerData.character_name;

        return PlayerPrefs.GetString("USERNAME", "Người chơi");
    }

    private static string ResolveClassName()
    {
        if (!string.IsNullOrWhiteSpace(ChatManager.Instance?.CurrentClassId))
            return ChatManager.Instance.CurrentClassId;

        if (!string.IsNullOrWhiteSpace(GameManager.Instance?.currentPlayerData?.element_type))
            return GameManager.Instance.currentPlayerData.element_type;

        return "Khác";
    }

    private static string ResolveElementType()
    {
        if (!string.IsNullOrWhiteSpace(GameManager.Instance?.currentPlayerData?.element_type))
            return GameManager.Instance.currentPlayerData.element_type;

        if (!string.IsNullOrWhiteSpace(ChatManager.Instance?.CurrentClassId))
            return ChatManager.Instance.CurrentClassId;

        return string.Empty;
    }

    private static int ResolveLevel()
    {
        if (GameManager.Instance?.currentPlayerData != null)
            return Mathf.Max(1, GameManager.Instance.currentPlayerData.level);

        return 1;
    }
}