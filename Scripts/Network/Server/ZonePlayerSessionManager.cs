using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

// Server-side: quản lý vòng đời player trong server 1-port.
// Trách nhiệm:
// - Lưu session (userId, username, zone) của từng clientId đã approved
// - Khi client kết nối hoàn tất → fetch PlayerData từ API → spawn NetworkObject
// - Khi client ngắt kết nối → save vị trí cuối → xóa khỏi session
// - UpdateZone() sau mỗi lần zone transfer
// Dependencies: ZoneConnectionApprovalV2 (RegisterSession), MapWorldConfig
[DisallowMultipleComponent]
public class ZonePlayerSessionManager : NetworkBehaviour
{
    public static ZonePlayerSessionManager Instance { get; private set; }

    [Header("Config")]
    [SerializeField] private MapWorldConfig _config;
    [SerializeField] private GameObject _playerPrefab;
    [Tooltip("Số giây chờ API trả về PlayerData trước khi kick client")]
    [SerializeField] private float _dataLoadTimeout = 10f;

    // clientId → ApprovedUser info (trước khi player data được load)
    private readonly Dictionary<ulong, ApprovedUserInfo> _approvedUsers = new();
    // clientId → PlayerSession (sau khi spawn xong)
    private readonly Dictionary<ulong, PlayerSession> _activeSessions = new();
    private readonly object _lock = new();

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback    += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   += OnClientDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback    -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   -= OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Gọi từ ZoneConnectionApprovalV2 khi client được approve.
    public void RegisterSession(ulong clientId, string userId, string username, int mapId, int zoneId)
    {
        lock (_lock)
        {
            _approvedUsers[clientId] = new ApprovedUserInfo
            {
                UserId   = userId,
                Username = username,
                MapId    = mapId,
                ZoneId   = zoneId
            };
        }
    }

    // Cập nhật zone sau khi player transfer. Gọi từ ZoneTransitionController.
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

    // Trả về PlayerSession của client nếu đang active. Null nếu chưa spawn.
    public PlayerSession GetSession(ulong clientId)
    {
        lock (_lock)
            return _activeSessions.TryGetValue(clientId, out var s) ? s : null;
    }

    // Đăng ký và xử lý sự kiện phát sinh trong runtime.

    private void OnClientConnected(ulong clientId)
    {
        if (!_approvedUsers.TryGetValue(clientId, out var userInfo))
        {
            Debug.LogWarning($"[ZonePlayerSessionManager] Client {clientId} kết nối nhưng không có " +
                             "approved user info — kick.");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return;
        }

        StartCoroutine(LoadAndSpawnPlayer(clientId, userInfo));
    }

    private void OnClientDisconnected(ulong clientId)
    {
        _approvedUsers.Remove(clientId);

        if (_activeSessions.TryGetValue(clientId, out var session))
        {
            // Save vị trí cuối trước khi xóa session
            StartCoroutine(SavePlayerPosition(session));
            _activeSessions.Remove(clientId);
            Debug.Log($"[ZonePlayerSessionManager] Client {clientId} (userId={session.UserId}) đã ngắt kết nối.");
        }
    }

    // Internal: Load & Spawn

    private IEnumerator LoadAndSpawnPlayer(ulong clientId, ApprovedUserInfo userInfo)
    {
        // 1 — Fetch PlayerData từ API: GET /api/player/{id}/data
        string apiBase = _config != null ? _config.apiBaseUrl : "http://localhost:5247/api";
        string url = $"{apiBase}/player/{userInfo.UserId}/data";
        string apiKey = _config != null ? _config.GetZoneApiKey() : "dev-zone-key";

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

        // 3 — Tính spawn position
        Vector3 spawnPos = GetEntryPoint(userInfo.MapId, userInfo.ZoneId, playerData);

        // 4 — Spawn player NetworkObject
        if (_playerPrefab == null)
        {
            Debug.LogError("[ZonePlayerSessionManager] _playerPrefab chưa gán!");
            yield break;
        }

        GameObject playerGo = Instantiate(_playerPrefab, spawnPos, Quaternion.identity);
        var netObj = playerGo.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[ZonePlayerSessionManager] Player prefab thiếu NetworkObject component!");
            Destroy(playerGo);
            yield break;
        }

        netObj.SpawnWithOwnership(clientId);

        // 5 — Init player với data
        var playerInit = playerGo.GetComponent<IPlayerDataReceiver>();
        playerInit?.OnPlayerDataLoaded(playerData, clientId);

        // 6 — Lưu session
        lock (_lock)
        {
            _activeSessions[clientId] = new PlayerSession
            {
                ClientId      = clientId,
                UserId        = userInfo.UserId,
                NetworkObject = netObj,
                MapId         = userInfo.MapId,
                ZoneId        = userInfo.ZoneId
            };
        }

        _approvedUsers.Remove(clientId);
        Debug.Log($"[ZonePlayerSessionManager] ✓ Spawned player clientId={clientId} " +
                  $"userId={userInfo.UserId} at {spawnPos}");
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
        if (session.NetworkObject == null) yield break;

        Vector3 pos = session.NetworkObject.transform.position;
        string apiBase = _config != null ? _config.apiBaseUrl : "http://localhost:5247/api";

        // Lấy HP/MP hiện tại từ NetworkPlayerHealth và NetworkPlayerDataSync trên PlayerObject
        int hp = 0, maxHp = 0, mp = 0, maxMp = 0;
        var playerHealth = session.NetworkObject.GetComponent<NetworkPlayerHealth>();
        if (playerHealth != null)
        {
            hp = playerHealth.GetCurrentHealth();
            maxHp = playerHealth.GetMaxHealth();
        }
        var dataSync = session.NetworkObject.GetComponent<NetworkPlayerDataSync>();
        if (dataSync != null)
        {
            mp = dataSync.networkMp.Value;
            maxMp = dataSync.networkMaxMp.Value;
            // Nếu chưa có NetworkPlayerHealth, lấy HP từ dataSync
            if (playerHealth == null)
            {
                hp = dataSync.networkHp.Value;
                maxHp = dataSync.networkMaxHp.Value;
            }
        }

        // Gọi PUT /api/player/{id}/data để save cả HP/MP cùng position
        string dataUrl = $"{apiBase}/player/{session.UserId}/data";
        string dataBody = $"{{\"map_id\":{session.MapId},\"zone_id\":{session.ZoneId}," +
                          $"\"position_x\":{pos.x:F2},\"position_y\":{pos.y:F2}," +
                          $"\"hp\":{hp},\"max_hp\":{maxHp},\"mp\":{mp},\"max_mp\":{maxMp}}}";

        using var req = new UnityWebRequest(dataUrl, "PUT")
        {
            uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(dataBody)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        string apiKey = _config != null ? _config.GetZoneApiKey() : "dev-zone-key";
        req.SetRequestHeader("X-Zone-Api-Key", apiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
            Debug.LogWarning($"[ZonePlayerSessionManager] Save player data thất bại user={session.UserId}: {req.error}");
        else
            Debug.Log($"[ZonePlayerSessionManager] Saved player data user={session.UserId} hp={hp}/{maxHp} mp={mp}/{maxMp} pos=({pos.x:F2},{pos.y:F2})");
    }

    // Inner types

    private class ApprovedUserInfo
    {
        public string UserId;
        public string Username;
        public int    MapId;
        public int    ZoneId;
    }

    public class PlayerSession
    {
        public ulong         ClientId;
        public string        UserId;
        public NetworkObject NetworkObject;
        public int           MapId;
        public int           ZoneId;
    }

    // DTO map với JSON response của GET /api/player/{id}/data.
    // Field names dùng snake_case để khớp với JsonUtility serialization.
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
        // final_stats sub-object không serialize tự động với JsonUtility
        // → dùng custom parse nếu cần, hoặc chỉ đọc raw fields ở đây
        public int    hp;
        public int    max_hp;
        public int    mp;
        public int    max_mp;
    }
}

// Interface để player prefab nhận data sau khi spawn.
// Implement trên PlayerController hoặc PlayerDataSync.
public interface IPlayerDataReceiver
{
    void OnPlayerDataLoaded(global::PlayerDataResponse data, ulong clientId);
}
