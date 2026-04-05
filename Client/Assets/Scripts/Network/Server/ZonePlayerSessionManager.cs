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
/// Dependencies: ZoneConnectionApprovalV2 (RegisterSession), MapWorldConfig
/// </summary>
[DisallowMultipleComponent]
public class ZonePlayerSessionManager : NetworkBehaviour
{
    public static ZonePlayerSessionManager Instance { get; private set; }

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
    private readonly object _lock = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

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

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ ZoneConnectionApprovalV2 khi client được approve.
    /// </summary>
    public void RegisterSession(ulong clientId, string userId, string username, int mapId, int zoneId, string jwtToken = null)
    {
        lock (_lock)
        {
            _approvedUsers[clientId] = new ApprovedUserInfo
            {
                UserId   = userId,
                Username = username,
                MapId    = mapId,
                ZoneId   = zoneId,
                JwtToken = jwtToken ?? string.Empty
            };
        }
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

    // ── Internal: Load & Spawn ────────────────────────────────────────────────

    private IEnumerator LoadAndSpawnPlayer(ulong clientId, ApprovedUserInfo userInfo)
    {
        // 1 — Fetch PlayerData từ API: GET /api/player/{id}/data
        string apiBase = _config != null ? _config.apiBaseUrl : "http://localhost:5000/api";
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
        PlayerDataResponse playerData = null;
        try
        {
            playerData = JsonUtility.FromJson<PlayerDataResponse>(request.downloadHandler.text);
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
                JwtToken      = userInfo.JwtToken,
                NetworkObject = netObj,
                MapId         = userInfo.MapId,
                ZoneId        = userInfo.ZoneId
            };
        }

        _approvedUsers.Remove(clientId);

        foreach (var filter in FindObjectsByType<NetworkVisibilityZoneFilter>(FindObjectsSortMode.None))
            filter.RefreshVisibility();

        Debug.Log($"[ZonePlayerSessionManager] ✓ Spawned player clientId={clientId} " +
                  $"userId={userInfo.UserId} at {spawnPos}");
    }

    /// <summary>
    /// Chọn prefab phù hợp dựa vào element_type, gender, is_hybrid của player.
    /// Thứ tự ưu tiên:
    ///   1. Hybrid: Resources.Load(hybrid_prefab_path) nếu field đó có giá trị
    ///   2. Khớp chính xác element + gender + isHybrid trong _playerPrefabs
    ///   3. Khớp element + isHybrid (bỏ qua gender)
    ///   4. Fallback: prefab đầu tiên trong mảng không null
    /// </summary>
    private GameObject ResolvePlayerPrefab(PlayerDataResponse data)
    {
        // Hybrid với Resources path từ DB
        if (data.is_hybrid && !string.IsNullOrEmpty(data.hybrid_prefab_path))
        {
            var res = Resources.Load<GameObject>(data.hybrid_prefab_path);
            if (res != null) return res;
            Debug.LogWarning($"[ZonePlayerSessionManager] Resources.Load thất bại: {data.hybrid_prefab_path} — thử fallback prefab array.");
        }

        // Pass 1: khớp hoàn toàn element + gender + isHybrid
        foreach (var e in _playerPrefabs)
        {
            if (e.prefab == null) continue;
            if (e.isHybrid != data.is_hybrid) continue;
            if (!string.IsNullOrEmpty(e.elementType) &&
                !string.Equals(e.elementType, data.element_type, StringComparison.OrdinalIgnoreCase)) continue;
            if (!string.IsNullOrEmpty(e.gender) &&
                !string.Equals(e.gender, data.gender, StringComparison.OrdinalIgnoreCase)) continue;
            return e.prefab;
        }

        // Pass 2: bỏ qua gender, khớp element + isHybrid
        foreach (var e in _playerPrefabs)
        {
            if (e.prefab == null) continue;
            if (e.isHybrid != data.is_hybrid) continue;
            if (!string.IsNullOrEmpty(e.elementType) &&
                !string.Equals(e.elementType, data.element_type, StringComparison.OrdinalIgnoreCase)) continue;
            return e.prefab;
        }

        // Pass 3: fallback — prefab không null đầu tiên
        foreach (var e in _playerPrefabs)
            if (e.prefab != null) return e.prefab;

        return null;
    }

    private static Vector3 GetEntryPoint(int mapId, int zoneId, PlayerDataResponse data)
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
        string apiBase = _config != null ? _config.apiBaseUrl : "http://localhost:5000/api";
        string url = $"{apiBase}/player/{session.UserId}/position";
        // Body theo PUT /api/player/{id}/position (PlayerController thực tế)
        string body = $"{{\"map_id\":{session.MapId},\"zone_id\":{session.ZoneId}," +
                      $"\"position_x\":{pos.x:F2},\"position_y\":{pos.y:F2}}}";

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
            Debug.LogWarning($"[ZonePlayerSessionManager] Save position thất bại user={session.UserId}: {req.error}");
    }

    // ── Inner types ───────────────────────────────────────────────────────────

    private class ApprovedUserInfo
    {
        public string UserId;
        public string Username;
        public int    MapId;
        public int    ZoneId;
        public string JwtToken;
    }

    public class PlayerSession
    {
        public ulong         ClientId;
        public string        UserId;
        public string        JwtToken;
        public NetworkObject NetworkObject;
        public int           MapId;
        public int           ZoneId;
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
    void OnPlayerDataLoaded(ZonePlayerSessionManager.PlayerDataResponse data, ulong clientId);
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
