using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Server-side: quản lý vòng đời player trong server 1-port.
///
/// Trách nhiệm:
///   - Lưu session (userId, username, zone) của từng clientId đã approved
///   - Khi client kết nối hoàn tất → fetch PlayerData từ API → spawn NetworkObject
///   - Khi client ngắt kết nối → save vị trí cuối → xóa khỏi session
///   - UpdateZone() sau mỗi lần zone transfer
///
/// Dependencies: ZoneConnectionApproval (RegisterSession), MapWorldConfig
/// </summary>
[DisallowMultipleComponent]
public class ZonePlayerSessionManager : NetworkBehaviour
{
    public static ZonePlayerSessionManager Instance { get; private set; }

    private static readonly Dictionary<ulong, ApprovedUserInfo> PendingApprovedUsers = new();
    private static readonly object PendingLock = new();

    [Header("Config")]
    [SerializeField] private MapWorldConfig _config;

    [Header("Player Prefabs — 1 entry mỗi hệ/giới tính")]
    [Tooltip("Mỗi entry map element_type + gender → prefab tương ứng.\nHybrid dùng hybrid_prefab_path từ DB (Resources.Load) nếu có, fallback vào entry isHybrid=true.")]
    [SerializeField] private PlayerPrefabEntry[] _playerPrefabs = Array.Empty<PlayerPrefabEntry>();

    [Tooltip("Số giây chờ API trả về PlayerData trước khi kick client")]
    [SerializeField] private float _dataLoadTimeout = 10f;

    // clientId → ApprovedUser info (trước khi player data được load)
    private readonly Dictionary<ulong, ApprovedUserInfo> _approvedUsers = new();
    // clientId → PlayerSession (sau khi spawn xong)
    private readonly Dictionary<ulong, PlayerSession> _activeSessions = new();
    // clientId đang load data/spawn để tránh chạy coroutine trùng.
    private readonly HashSet<ulong> _spawningClients = new();
    private bool _prefabConfigLogged;
    private readonly object _lock = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[ZonePlayerSessionManager] Duplicate instance detected on '{gameObject.name}' (existing='{Instance.gameObject.name}') — destroying duplicate COMPONENT only.");
            Destroy(this); // Destroy only this component, NOT the whole gameObject
            return;
        }
        Instance = this;

        Debug.Log($"[ZonePlayerSessionManager] Awake: object={gameObject.name}, scene={gameObject.scene.name}, configAssigned={_config != null}, prefabEntries={_playerPrefabs?.Length ?? 0}");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        RestorePendingApprovedUsers();

        NetworkManager.Singleton.OnClientConnectedCallback    -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   -= OnClientDisconnected;
        NetworkManager.Singleton.OnClientConnectedCallback    += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   += OnClientDisconnected;

        LogPlayerPrefabConfiguration();
        Debug.Log($"[ZonePlayerSessionManager] OnNetworkSpawn: approved={_approvedUsers.Count}, active={_activeSessions.Count}");
        TrySpawnPendingConnectedClients("OnNetworkSpawn");
        StartCoroutine(DrainPendingLoop());
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback    -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   -= OnClientDisconnected;
        Debug.LogWarning($"[ZonePlayerSessionManager] OnNetworkDespawn: object={gameObject.name}");
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
            Debug.LogWarning($"[ZonePlayerSessionManager] OnDestroy: Instance cleared (object={gameObject.name}).");
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ ZoneConnectionApproval khi client được approve.
    /// </summary>
    public void RegisterSession(ulong clientId, string userId, string username, int mapId, int zoneId, string jwtToken = null, int geneSlot = 1)
    {
        lock (_lock)
        {
            _approvedUsers[clientId] = new ApprovedUserInfo
            {
                UserId   = userId,
                Username = username,
                MapId    = mapId,
                ZoneId   = zoneId,
                JwtToken = jwtToken ?? string.Empty,
                GeneSlot = geneSlot
            };
        }

        Debug.Log($"[ZonePlayerSessionManager] RegisterSession: clientId={clientId}, userId={userId}, room=map{mapId}_zone{zoneId}, geneSlot={geneSlot}, isSpawned={IsSpawned}");
        TrySpawnPendingConnectedClients("RegisterSession");
    }

    public static void RegisterSessionOrQueue(ulong clientId, string userId, string username, int mapId, int zoneId, string jwtToken = null, int geneSlot = 1)
    {
        if (Instance != null)
        {
            Instance.RegisterSession(clientId, userId, username, mapId, zoneId, jwtToken, geneSlot);
            return;
        }

        lock (PendingLock)
        {
            PendingApprovedUsers[clientId] = new ApprovedUserInfo
            {
                UserId = userId,
                Username = username,
                MapId = mapId,
                ZoneId = zoneId,
                JwtToken = jwtToken ?? string.Empty,
                GeneSlot = geneSlot
            };
        }

        Debug.LogWarning($"[ZonePlayerSessionManager] Instance chưa sẵn sàng tại approval. Queue session cho clientId={clientId}, userId={userId}, room=map{mapId}_zone{zoneId}, geneSlot={geneSlot}.");
    }

    /// <summary>
    /// Cập nhật zone sau khi player transfer. Gọi từ ZoneTransitionController.
    /// </summary>
    public void UpdateZone(ulong clientId, int mapId, int zoneId)
    {
        lock (_lock)
        {
            if (_activeSessions.TryGetValue(clientId, out var session))
            {
                session.MapId  = mapId;
                session.ZoneId = zoneId;
            }
        }
    }

    public string GetPlayerId(ulong clientId)
    {
        lock (_lock)
            return _activeSessions.TryGetValue(clientId, out var s) ? s.UserId : null;
    }

    /// <summary>
    /// Trả về JWT token của client (để game server gọi REST API thay mặt client).
    /// </summary>
    public string GetClientJwt(ulong clientId)
    {
        lock (_lock)
            return _activeSessions.TryGetValue(clientId, out var s) ? s.JwtToken : null;
    }

    /// <summary>
    /// Trả về gene slot (1 hoặc 2) của client đang active.
    /// </summary>
    public int GetClientGeneSlot(ulong clientId)
    {
        lock (_lock)
            return _activeSessions.TryGetValue(clientId, out var s) ? s.GeneSlot : 1;
    }

    /// <summary>
    /// Trả về PlayerSession của client nếu đang active. Null nếu chưa spawn.
    /// </summary>
    public PlayerSession GetSession(ulong clientId)
    {
        lock (_lock)
            return _activeSessions.TryGetValue(clientId, out var s) ? s : null;
    }

    // ── Event Handlers ────────────────────────────────────────────────────────

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer)
            return;

        // Drain any sessions that were queued before this instance was ready
        RestorePendingApprovedUsers();

        lock (_lock)
        {
            if (_activeSessions.ContainsKey(clientId))
            {
                Debug.Log($"[ZonePlayerSessionManager] OnClientConnected skip: client {clientId} đã có active session.");
                return;
            }

            if (!_spawningClients.Add(clientId))
            {
                Debug.Log($"[ZonePlayerSessionManager] OnClientConnected skip: client {clientId} đang spawn.");
                return;
            }
        }

        if (!_approvedUsers.TryGetValue(clientId, out var userInfo))
        {
            ClearSpawnInProgress(clientId);
            Debug.LogWarning($"[ZonePlayerSessionManager] Client {clientId} kết nối nhưng không có " +
                             "approved user info — kick.");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        Debug.Log($"[ZonePlayerSessionManager] OnClientConnected: bắt đầu load/spawn cho client {clientId}, userId={userInfo.UserId}, room=map{userInfo.MapId}_zone{userInfo.ZoneId}");
        StartCoroutine(LoadAndSpawnPlayerTracked(clientId, userInfo));
    }

    private void OnClientDisconnected(ulong clientId)
    {
        _approvedUsers.Remove(clientId);
        ClearSpawnInProgress(clientId);

        PlayerSession session = null;
        lock (_lock)
            _activeSessions.TryGetValue(clientId, out session);

        // ── [RECONNECT-DEBUG] Bước 1: log trạng thái đầu vào ─────────────────
        var waveMgrDbg    = WaveSessionManager.GetOrCreateInstance(gameObject);
        var registryDbg   = ZoneRoomRegistry.Instance;
        string userIdDbg  = session?.UserId ?? "(null session)";
        bool hasWaveMgr   = waveMgrDbg != null;
        bool hasRegistry  = registryDbg != null;
        bool hasActiveWave = hasWaveMgr && waveMgrDbg.HasActiveSession(session?.UserId ?? string.Empty);
        var clientRoomDbg  = registryDbg?.GetClientRoom(clientId);

        Debug.Log($"[RECONNECT-DEBUG][1-Disconnect] clientId={clientId} userId={userIdDbg} " +
                  $"hasSession={session != null} hasWaveMgr={hasWaveMgr} hasRegistry={hasRegistry} " +
                  $"hasActiveWaveSession={hasActiveWave} " +
                  $"clientRoom={clientRoomDbg?.ZoneKey ?? "null"} " +
                  $"clientRoomIsCustom={clientRoomDbg?.IsCustom} " +
                  $"clientRoomPlayerCount={clientRoomDbg?.PlayerCount}");
        // ──────────────────────────────────────────────────────────────────────

        // Bước 2: preserve wave session TRƯỚC khi unregister client khỏi registry
        if (session != null)
        {
            if (hasWaveMgr)
            {
                Debug.Log($"[RECONNECT-DEBUG][2-PreserveWave] Gọi OnPlayerDisconnect userId={session.UserId}");
                waveMgrDbg.OnPlayerDisconnect(session.UserId);
            }
            else
            {
                Debug.LogWarning($"[RECONNECT-DEBUG][2-PreserveWave] WaveSessionManager.Instance == null → KHÔNG preserve wave session! userId={session.UserId}");
                // Fallback: nếu WaveSessionManager không sẵn sàng (null), cố gắng preserve direct room reference
                if (registryDbg != null && clientRoomDbg != null && clientRoomDbg.IsCustom)
                {
                    registryDbg.MarkRoomPreserved(clientRoomDbg, $"disconnect-fallback userId={session.UserId}");
                    Debug.Log($"[RECONNECT-DEBUG][2-PreserveFallback] MarkRoomPreserved fallback for {clientRoomDbg.ZoneKey} userId={session.UserId}");
                }
            }
        }

        // Bước 3: giờ mới xóa client khỏi room (CleanupRoomIfEmpty sẽ thấy preserved flag)
        Debug.Log($"[RECONNECT-DEBUG][3-UnregisterClient] Gọi UnregisterClient clientId={clientId}");
        registryDbg?.UnregisterClient(clientId);

        if (session != null)
        {
            // Reset player về làng (map 0) khi disconnect — đúng yêu cầu "quay về làng khi out game"
            StartCoroutine(SavePlayerPosition(session));
            lock (_lock)
                _activeSessions.Remove(clientId);
            Debug.Log($"[ZonePlayerSessionManager] Client {clientId} (userId={session.UserId}) đã ngắt kết nối.");
        }
    }

    // ── Internal: Load & Spawn ────────────────────────────────────────────────

    private IEnumerator LoadAndSpawnPlayerTracked(ulong clientId, ApprovedUserInfo userInfo)
    {
        try
        {
            yield return StartCoroutine(LoadAndSpawnPlayer(clientId, userInfo));
        }
        finally
        {
            ClearSpawnInProgress(clientId);
        }
    }

    private IEnumerator LoadAndSpawnPlayer(ulong clientId, ApprovedUserInfo userInfo)
    {
        // 1 — Fetch PlayerData từ API: GET /api/player/{id}/data or /data2
        string apiBase = _config != null ? _config.apiBaseUrl : ServerAddressConfig.Instance.ApiUrl;
        string dataEndpoint = userInfo.GeneSlot == 2 ? "data2" : "data";
        string url = $"{apiBase}/player/{userInfo.UserId}/{dataEndpoint}";
        string apiKey = _config != null ? _config.GetZoneApiKey() : "dev-zone-key";

        Debug.Log($"==== [GENE2_DEBUG] ZonePlayerSessionManager.LoadAndSpawnPlayer: clientId={clientId}, userId={userInfo.UserId}, geneSlot={userInfo.GeneSlot}, url={url} ====");

        using var request = UnityWebRequest.Get(url);
        request.SetRequestHeader("X-Zone-Api-Key", apiKey);

        float elapsed = 0f;
        var send = request.SendWebRequest();

        while (!send.isDone)
        {
            elapsed += Time.deltaTime;
            if (elapsed > _dataLoadTimeout)
            {
                Debug.LogWarning($"[ZonePlayerSessionManager] Timeout load data client {clientId} → kick.");
                NetworkManager.Singleton.DisconnectClient(clientId);
                yield break;
            }
            yield return null;
        }

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ZonePlayerSessionManager] Không load được player data " +
                             $"(userId={userInfo.UserId}): {request.error} → kick client {clientId}.");
            if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
                NetworkManager.Singleton.DisconnectClient(clientId);
            _approvedUsers.Remove(clientId);
            yield break;
        }

        Debug.Log($"[ZonePlayerSessionManager] PlayerData raw response clientId={clientId}, length={request.downloadHandler.text?.Length ?? 0}: {TruncateForLog(request.downloadHandler.text, 1200)}");

        // 2 — Parse PlayerData (đơn giản — adapt theo PlayerDataResponse thực tế của dự án)
        global::PlayerDataResponse playerData = null;
        try
        {
            playerData = JsonUtility.FromJson<global::PlayerDataResponse>(request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ZonePlayerSessionManager] Parse PlayerData thất bại: {ex.Message}");
            NetworkManager.Singleton.DisconnectClient(clientId);
            yield break;
        }

        if (playerData == null)
        {
            Debug.LogError($"[ZonePlayerSessionManager] PlayerData null sau parse — kick client {clientId}.");
            NetworkManager.Singleton.DisconnectClient(clientId);
            yield break;
        }

        Debug.Log($"[ZonePlayerSessionManager] Parsed PlayerData clientId={clientId}: {DescribePlayerData(playerData)}");

        // 3 — Tính spawn position
        Vector3 spawnPos = GetEntryPoint(userInfo.MapId, userInfo.ZoneId, playerData);

        // 4 — Spawn player NetworkObject
        GameObject template = ResolvePlayerPrefab(playerData);
        if (template == null)
        {
            Debug.LogError($"[ZonePlayerSessionManager] Không tìm thấy prefab cho " +
                           $"element={playerData.element_type} gender={playerData.gender} " +
                           $"hybrid={playerData.is_hybrid}. Kiểm tra mảng Player Prefabs trong Inspector.");
            NetworkManager.Singleton.DisconnectClient(clientId);
            yield break;
        }

        GameObject playerGo = Instantiate(template, spawnPos, Quaternion.identity);
        var netObj = playerGo.GetComponent<NetworkObject>();
        var visibilityFilter = playerGo.GetComponent<NetworkVisibilityZoneFilter>();

        Debug.Log($"[ZonePlayerSessionManager] Instantiated player template for clientId={clientId}: template={DescribePrefab(template)}, scene={playerGo.scene.name}, hasNetworkObject={netObj != null}, hasController={playerGo.GetComponent<NetworkPlayerController>() != null}, hasDataSync={playerGo.GetComponent<NetworkPlayerDataSync>() != null}, hasInventory={playerGo.GetComponent<NetworkInventory>() != null}, hasVisibilityFilter={visibilityFilter != null}");

        if (netObj == null)
        {
            Debug.LogError("[ZonePlayerSessionManager] Player prefab thiếu NetworkObject component!");
            Destroy(playerGo);
            yield break;
        }

        visibilityFilter?.InitializeForServer();

        // Di chuyển player vào physics scene của map khởi đầu — TRƯỚC SpawnWithOwnership
        MapSceneManager.Instance?.MoveToMapScene(playerGo, userInfo.MapId);
        Debug.Log($"[ZonePlayerSessionManager] Player moved to scene before spawn: clientId={clientId}, scene={playerGo.scene.name}, mapId={userInfo.MapId}, position={playerGo.transform.position}");

        netObj.SpawnWithOwnership(clientId);
        visibilityFilter?.RefreshVisibility();

        Debug.Log($"[ZonePlayerSessionManager] SpawnWithOwnership complete: clientId={clientId}, netId={netObj.NetworkObjectId}, owner={netObj.OwnerClientId}, isSpawned={netObj.IsSpawned}, scene={playerGo.scene.name}, hasPlayerObject={netObj.IsPlayerObject}");

        // 5 — Init player với data
        var playerInit = playerGo.GetComponent<IPlayerDataReceiver>();
        if (playerInit != null)
        {
            Debug.Log($"[ZonePlayerSessionManager] Calling IPlayerDataReceiver={playerInit.GetType().Name} for clientId={clientId}.");
            playerInit.OnPlayerDataLoaded(playerData, clientId);
        }
        else
        {
            Debug.LogWarning($"[ZonePlayerSessionManager] Player prefab {template.name} không có IPlayerDataReceiver. Data sẽ không được đẩy trực tiếp sau spawn.");
        }

        // 6 — Lưu session
        lock (_lock)
        {
            _activeSessions[clientId] = new PlayerSession
            {
                ClientId      = clientId,
                UserId        = userInfo.UserId,
                JwtToken      = userInfo.JwtToken,
                NetworkObject = netObj,
                MapId         = userInfo.MapId,
                ZoneId        = userInfo.ZoneId,
                GeneSlot      = userInfo.GeneSlot
            };
        }

        _approvedUsers.Remove(clientId);

        foreach (var filter in FindObjectsByType<NetworkVisibilityZoneFilter>(FindObjectsSortMode.None))
            filter.RefreshVisibility();

        StartCoroutine(RefreshVisibilityAfterClientReady(clientId));
        SendInitialZoneSync(clientId, userInfo, spawnPos);

        // 7 — Đẩy skill data về client ngay lúc spawn (client cache, không cần request lại khi mở tab)
        if (GameplayCommandService.Instance != null && int.TryParse(userInfo.UserId, out int pushPlayerId))
        {
            GameplayCommandService.Instance.PushSkillsToClient(clientId, pushPlayerId, userInfo.JwtToken, userInfo.GeneSlot);
            Debug.Log($"[ZonePlayerSessionManager] PushSkillsToClient đã gửi cho clientId={clientId} playerId={pushPlayerId} geneSlot={userInfo.GeneSlot}");
        }
        else
        {
            Debug.LogWarning($"[ZonePlayerSessionManager] Bỏ qua PushSkillsToClient – GameplayCommandService={(GameplayCommandService.Instance == null ? "NULL" : "ok")}, userId='{userInfo.UserId}'");
        }

        Debug.Log($"[ZonePlayerSessionManager] ✓ Spawned player clientId={clientId} " +
                  $"userId={userInfo.UserId} at {spawnPos}");
    }

    private void SendInitialZoneSync(ulong clientId, ApprovedUserInfo userInfo, Vector3 spawnPos)
    {
        MapDefinition mapDef = _config?.GetMap(userInfo.MapId);
        string sceneName = mapDef?.sceneName ?? string.Empty;

        Debug.Log($"[ZonePlayerSessionManager] Initial zone sync queued for clientId={clientId} map={userInfo.MapId} zone={userInfo.ZoneId} scene='{sceneName}' pos={spawnPos}");

        SendInitialZoneSyncClientRpc(
            userInfo.MapId,
            userInfo.ZoneId,
            sceneName,
            spawnPos.x,
            spawnPos.y,
            BuildSingleClientRpcParams(clientId));
    }

    private static ClientRpcParams BuildSingleClientRpcParams(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };
    }

    [ClientRpc]
    private void SendInitialZoneSyncClientRpc(
        int mapId,
        int zoneId,
        string sceneName,
        float x,
        float y,
        ClientRpcParams rpcParams = default)
    {
        PlayerPrefs.SetInt("SelectedMapId", mapId);
        PlayerPrefs.SetInt("PLAYER_ZONE_ID", zoneId);
        PlayerPrefs.Save();

        Debug.Log($"[ZonePlayerSessionManager] Initial zone sync received: map={mapId} zone={zoneId} scene='{sceneName}' pos=({x:F2}, {y:F2})");
        ClientSceneController.Instance?.HandleZoneTeleport(sceneName, x, y, mapId, zoneId);
    }

    private void LogPlayerPrefabConfiguration()
    {
        if (_prefabConfigLogged)
            return;

        _prefabConfigLogged = true;

        int heEntries = 0;
        int nuEntries = 0;
        if (_playerPrefabs != null)
        {
            foreach (var entry in _playerPrefabs)
            {
                if (entry == null)
                    continue;

                if (string.Equals(entry.gender, "He", StringComparison.OrdinalIgnoreCase))
                    heEntries++;
                if (string.Equals(entry.gender, "Nu", StringComparison.OrdinalIgnoreCase))
                    nuEntries++;
            }
        }

        Debug.Log($"[ZonePlayerSessionManager] Prefab config dump: configAssigned={_config != null}, entries={_playerPrefabs?.Length ?? 0}, heEntries={heEntries}, nuEntries={nuEntries}, mappings={GetPlayerPrefabMappingsSummary()}");
        if (nuEntries == 0)
            Debug.LogWarning("[ZonePlayerSessionManager] Prefab config hiện không có mapping gender=Nu. Nếu DB trả gender=Nu thì resolver sẽ fallback bỏ qua gender hoặc dùng prefab mặc định.");
    }

    /// <summary>
    /// Safety-net: drain PendingApprovedUsers every 0.5 s for the first 30 s after spawn.
    /// Catches sessions that arrive after OnNetworkSpawn in edge-case timing.
    /// </summary>
    private IEnumerator DrainPendingLoop()
    {
        float elapsed = 0f;
        while (elapsed < 30f && IsServer && IsSpawned)
        {
            yield return new WaitForSeconds(0.5f);
            elapsed += 0.5f;
            bool hasPending;
            lock (PendingLock) { hasPending = PendingApprovedUsers.Count > 0; }
            if (hasPending)
            {
                RestorePendingApprovedUsers();
                TrySpawnPendingConnectedClients("DrainLoop");
            }
        }
    }

    private void RestorePendingApprovedUsers()
    {
        lock (PendingLock)
        {
            if (PendingApprovedUsers.Count == 0)
                return;

            lock (_lock)
            {
                foreach (var kvp in PendingApprovedUsers)
                    _approvedUsers[kvp.Key] = kvp.Value;
            }

            Debug.Log($"[ZonePlayerSessionManager] Restored {PendingApprovedUsers.Count} queued approved session(s).");
            PendingApprovedUsers.Clear();
        }
    }

    private void TrySpawnPendingConnectedClients(string reason)
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            return;

        List<ulong> clientsToSpawn = new();
        List<string> blockedClients = new();
        lock (_lock)
        {
            foreach (var clientId in _approvedUsers.Keys)
            {
                if (clientId == NetworkManager.ServerClientId)
                {
                    blockedClients.Add($"{clientId}:server-client");
                    continue;
                }
                if (!networkManager.ConnectedClients.ContainsKey(clientId))
                {
                    blockedClients.Add($"{clientId}:not-connected-yet");
                    continue;
                }
                if (_activeSessions.ContainsKey(clientId))
                {
                    blockedClients.Add($"{clientId}:already-active");
                    continue;
                }
                if (_spawningClients.Contains(clientId))
                {
                    blockedClients.Add($"{clientId}:spawn-in-progress");
                    continue;
                }
                clientsToSpawn.Add(clientId);
            }

            if (_approvedUsers.Count > 0)
            {
                string blockedSummary = blockedClients.Count > 0
                    ? string.Join(", ", blockedClients)
                    : "none";
                Debug.Log($"[ZonePlayerSessionManager] TrySpawnPendingConnectedClients({reason}) | approved={_approvedUsers.Count} active={_activeSessions.Count} spawning={_spawningClients.Count} connected={networkManager.ConnectedClients.Count} runnable={clientsToSpawn.Count} blocked={blockedSummary}");
            }
        }

        foreach (var clientId in clientsToSpawn)
        {
            Debug.Log($"[ZonePlayerSessionManager] Backfill spawn trigger ({reason}) cho client {clientId}.");
            OnClientConnected(clientId);
        }
    }

    private void ClearSpawnInProgress(ulong clientId)
    {
        lock (_lock)
            _spawningClients.Remove(clientId);
    }

    private IEnumerator RefreshVisibilityAfterClientReady(ulong clientId)
    {
        yield return null;
        yield return null;

        var networkManager = NetworkManager.Singleton;
        if (networkManager == null || !networkManager.IsServer)
            yield break;

        if (!networkManager.ConnectedClients.ContainsKey(clientId))
            yield break;

        foreach (var filter in FindObjectsByType<NetworkVisibilityZoneFilter>(FindObjectsSortMode.None))
            filter.RefreshVisibility();
    }

    /// <summary>
    /// Chọn prefab phù hợp dựa vào element_type, gender, is_hybrid của player.
    /// Thứ tự ưu tiên:
    ///   1. Hybrid: Resources.Load(hybrid_prefab_path) nếu field đó có giá trị
    ///   2. Khớp chính xác element + gender + isHybrid trong _playerPrefabs
    ///   3. Khớp element + isHybrid (bỏ qua gender)
    ///   4. Fallback: prefab đầu tiên trong mảng không null
    /// </summary>
    private GameObject ResolvePlayerPrefab(global::PlayerDataResponse data)
    {
        Debug.Log($"[ZonePlayerSessionManager] ResolvePlayerPrefab begin: {DescribePlayerData(data)}");
        Debug.Log($"[ZonePlayerSessionManager] ResolvePlayerPrefab mappings: {GetPlayerPrefabMappingsSummary()}");

        // Hybrid với Resources path từ DB
        if (data.is_hybrid && !string.IsNullOrEmpty(data.hybrid_prefab_path))
        {
            Debug.Log($"[ZonePlayerSessionManager] ResolvePlayerPrefab: trying Resources.Load('{data.hybrid_prefab_path}') for hybrid player.");
            var res = Resources.Load<GameObject>(data.hybrid_prefab_path);
            if (res != null)
            {
                Debug.Log($"[ZonePlayerSessionManager] ResolvePlayerPrefab: selected hybrid resource prefab {DescribePrefab(res)}.");
                return res;
            }
            Debug.LogWarning($"[ZonePlayerSessionManager] Resources.Load thất bại: {data.hybrid_prefab_path} — thử fallback prefab array.");
        }

        // Pass 1: khớp hoàn toàn element + gender + isHybrid
        foreach (var e in _playerPrefabs)
        {
            if (e == null) continue;
            if (e.prefab == null) continue;
            if (e.isHybrid != data.is_hybrid) continue;
            if (!string.IsNullOrEmpty(e.elementType) &&
                !string.Equals(e.elementType, data.element_type, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(e.gender) &&
                !string.Equals(e.gender, data.gender, StringComparison.OrdinalIgnoreCase)) continue;

            Debug.Log($"[ZonePlayerSessionManager] ResolvePlayerPrefab: exact match -> {DescribePrefabEntry(e)}");
            return e.prefab;
        }

        var sameElementEntries = new StringBuilder();
        bool hasSameElementEntries = false;
        foreach (var e in _playerPrefabs)
        {
            if (e == null || e.prefab == null) continue;
            if (e.isHybrid != data.is_hybrid) continue;
            if (!string.IsNullOrEmpty(e.elementType) &&
                !string.Equals(e.elementType, data.element_type, StringComparison.OrdinalIgnoreCase)) continue;

            if (hasSameElementEntries) sameElementEntries.Append(" | ");
            sameElementEntries.Append(DescribePrefabEntry(e));
            hasSameElementEntries = true;
        }

        if (hasSameElementEntries)
            Debug.LogWarning($"[ZonePlayerSessionManager] ResolvePlayerPrefab: không có exact gender match cho element={data.element_type}, gender={data.gender}, hybrid={data.is_hybrid}. Same-element entries: {sameElementEntries}");
        else
            Debug.LogWarning($"[ZonePlayerSessionManager] ResolvePlayerPrefab: không có entry nào khớp element={data.element_type}, hybrid={data.is_hybrid}. Sẽ thử fallback rộng hơn.");

        // Pass 2: bỏ qua gender, khớp element + isHybrid
        foreach (var e in _playerPrefabs)
        {
            if (e == null) continue;
            if (e.prefab == null) continue;
            if (e.isHybrid != data.is_hybrid) continue;
            if (!string.IsNullOrEmpty(e.elementType) &&
                !string.Equals(e.elementType, data.element_type, StringComparison.OrdinalIgnoreCase)) continue;

            Debug.LogWarning($"[ZonePlayerSessionManager] ResolvePlayerPrefab: fallback bỏ qua gender -> {DescribePrefabEntry(e)}");
            return e.prefab;
        }

        // Pass 3: fallback — prefab không null đầu tiên
        foreach (var e in _playerPrefabs)
        {
            if (e == null || e.prefab == null) continue;

            Debug.LogWarning($"[ZonePlayerSessionManager] ResolvePlayerPrefab: fallback cuối cùng dùng prefab đầu tiên -> {DescribePrefabEntry(e)}");
            return e.prefab;
        }

        Debug.LogError($"[ZonePlayerSessionManager] ResolvePlayerPrefab thất bại hoàn toàn. mappings={GetPlayerPrefabMappingsSummary()}");

        return null;
    }

    private string GetPlayerPrefabMappingsSummary()
    {
        if (_playerPrefabs == null || _playerPrefabs.Length == 0)
            return "(empty)";

        var builder = new StringBuilder();
        for (int index = 0; index < _playerPrefabs.Length; index++)
        {
            if (index > 0)
                builder.Append(" || ");

            builder.Append('#').Append(index).Append(':').Append(DescribePrefabEntry(_playerPrefabs[index]));
        }

        return builder.ToString();
    }

    private static string DescribePlayerData(global::PlayerDataResponse data)
    {
        if (data == null)
            return "(null PlayerDataResponse)";

        return $"playerId={data.player_id}, name={data.character_name}, element={data.element_type}, gender={data.gender}, hybrid={data.is_hybrid}, hybridPath={data.hybrid_prefab_path}, map={data.map_id}, zone={data.zone_id}, pos=({data.position_x},{data.position_y}), hp={data.GetHp()}/{data.GetMaxHp()}, mp={data.GetMp()}/{data.GetMaxMp()}, atk={data.GetAttack()}, def={data.GetDefense()}, move={data.GetMoveSpeed()}";
    }

    private static string DescribePrefabEntry(PlayerPrefabEntry entry)
    {
        if (entry == null)
            return "(null entry)";

        string element = string.IsNullOrWhiteSpace(entry.elementType) ? "*" : entry.elementType;
        string gender = string.IsNullOrWhiteSpace(entry.gender) ? "*" : entry.gender;
        return $"element={element},gender={gender},hybrid={entry.isHybrid},prefab={DescribePrefab(entry.prefab)}";
    }

    private static string DescribePrefab(GameObject prefab)
    {
        if (prefab == null)
            return "null";

        return $"{prefab.name}(hasNetworkObject={prefab.GetComponent<NetworkObject>() != null})";
    }

    private static string TruncateForLog(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value ?? string.Empty;

        return value.Substring(0, maxLength) + "...(truncated)";
    }

    private static Vector3 GetEntryPoint(int mapId, int zoneId, global::PlayerDataResponse data)
    {
        // Nếu vị trí trong DB hợp lệ, dùng vị trí đó (lưu trong info_char, trả về qua API)
        if (data.position_x != 0 || data.position_y != 0)
            return new Vector3(data.position_x, data.position_y, 0f);

        // Fallback: entry point 0 của zone từ ZoneRoomRegistry
        var room = ZoneRoomRegistry.Instance?.GetRoom(mapId, zoneId);
        if (room != null)
        {
            Vector2 ep = room.GetEntryPoint(0);
            return new Vector3(ep.x, ep.y, 0f);
        }

        return Vector3.zero;
    }

    private IEnumerator SavePlayerPosition(PlayerSession session)
    {
        if (session == null || string.IsNullOrWhiteSpace(session.UserId))
            yield break;

        // ✅ Luôn reset player về làng (map 0) khi disconnect.
        // Lý do: client đã load GameScene (mapId=0) khi login lại,
        // nếu server lưu mapId cũ (vd: 3) → lần sau login API trả map_id=3
        // → client gửi mapId=3 trong payload → server gán room map3
        // → enemy map3 visible cho client đang ở GameScene (map0) → BUG.
        string apiBase = _config != null ? _config.apiBaseUrl : ServerAddressConfig.Instance.ApiUrl;
        string url = $"{apiBase}/player/{session.UserId}/position";
        string body = "{\"reset_to_start_map\":true}";

        Debug.Log($"[ZonePlayerSessionManager] Reset player to start map on disconnect: user={session.UserId} (was map={session.MapId})");

        using var req = new UnityWebRequest(url, "PUT")
        {
            uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        string apiKey = _config != null ? _config.GetZoneApiKey() : "dev-zone-key";
        req.SetRequestHeader("X-Zone-Api-Key", apiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"[ZonePlayerSessionManager] Reset start map thất bại user={session.UserId}: {req.error}");
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    private class ApprovedUserInfo
    {
        public string UserId;
        public string Username;
        public int    MapId;
        public int    ZoneId;
        public string JwtToken;
        public int    GeneSlot;
    }

    public class PlayerSession
    {
        public ulong         ClientId;
        public string        UserId;
        public string        JwtToken;
        public NetworkObject NetworkObject;
        public int           MapId;
        public int           ZoneId;
        public int           GeneSlot;
    }

    /// <summary>
    /// DTO map với JSON response của GET /api/player/{id}/data.
    /// Field names dùng snake_case để khớp với JsonUtility serialization.
    /// </summary>
    [Serializable]
    public class PlayerDataResponse
    {
        public int    player_id;
        public string character_name;
        public int    level;
        public int    experience;
        public float  position_x;
        public float  position_y;
        public int    map_id;
        public int    zone_id;
        public string element_type;
        public string gender;
        public int    gene_tier;
        public int    gene_exp;
        public bool   is_hybrid;
        public string hybrid_prefab_path;
        public int    bag_slots;
        public BagEquippedItemData[] bag_equipped_items;

        // Nested sub-objects — JsonUtility hỗ trợ [Serializable] class
        public FinalStatsDto final_stats;
        public BaseStatsDto  base_stats;

        // Flat fields cho backward compat
        public int    hp;
        public int    max_hp;
        public int    mp;
        public int    max_mp;

        /// <summary>Lấy max_hp đúng: ưu tiên final_stats, fallback flat field.</summary>
        public int GetMaxHp() => final_stats != null && final_stats.max_hp > 0 ? final_stats.max_hp : max_hp;
        public int GetMaxMp() => final_stats != null && final_stats.max_mp > 0 ? final_stats.max_mp : max_mp;
        public int GetHp()    => final_stats != null && final_stats.hp > 0 ? final_stats.hp : hp;
        public int GetMp()    => final_stats != null && final_stats.mp > 0 ? final_stats.mp : mp;
        public int GetAttack()   => final_stats != null ? final_stats.attack : 10;
        public int GetDefense()  => final_stats != null ? final_stats.defense : 0;
        public float GetMoveSpeed() => final_stats != null && final_stats.move_speed > 0 ? final_stats.move_speed : 5f;
    }

    [Serializable]
    public class FinalStatsDto
    {
        public int   hp;
        public int   max_hp;
        public int   mp;
        public int   max_mp;
        public int   attack;
        public int   defense;
        public float move_speed;
    }

    [Serializable]
    public class BaseStatsDto
    {
        public int hp;
        public int max_hp;
        public int mp;
        public int max_mp;
        public int attack;
        public int defense;
    }
}

/// <summary>
/// Interface để player prefab nhận data sau khi spawn.
/// Implement trên PlayerController hoặc PlayerDataSync.
/// </summary>
public interface IPlayerDataReceiver
{
    void OnPlayerDataLoaded(global::PlayerDataResponse data, ulong clientId);
}

/// <summary>
/// Map 1 element_type + gender → prefab player tương ứng.
/// Dùng trong Inspector của ZonePlayerSessionManager.
/// </summary>
[Serializable]
public class PlayerPrefabEntry
{
    [Tooltip("element_type từ DB: Fire/Water/Earth/Wood/Metal/Wind.\nĐể trống = khớp mọi hệ (dùng làm fallback).")]
    public string elementType;

    [Tooltip("gender từ DB: He / Nu.\nĐể trống = khớp mọi giới tính.")]
    public string gender;

    [Tooltip("Tick nếu entry này dùng cho Hybrid/Fusion. Bỏ tick = nhân vật hệ thường.")]
    public bool isHybrid;

    [Tooltip("Prefab tương ứng. Phải có NetworkObject component.")]
    public GameObject prefab;
}
