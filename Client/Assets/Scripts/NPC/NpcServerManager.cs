using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

/// <summary>
/// Server-authoritative NPC manager — thay thế NpcSpawner.cs.
///
/// Chỉ chạy spawn logic khi IsServer (đúng với cả Host và dedicated server sau này).
/// Client không cần làm gì — NGO tự replicate NetworkObject sang client khi server Spawn().
///
/// Setup Inspector:
///   apiBase  = "http://localhost:5000"
///   mapId    = mapId của scene này (đặt số cụ thể, ví dụ: 1)
///   npcPrefabs[0] = NPC_Shop_Prefab        (npc_type = "shop")
///   npcPrefabs[1] = NPC_Blacksmith_Prefab  (npc_type = "blacksmith")
///   npcPrefabs[2] = NPC_Quest_Prefab       (npc_type = "quest")
///   npcPrefabs[3] = NPC_Exchange_Prefab    (npc_type = "exchange")
///   npcPrefabs[4] = NPC_Event_Prefab       (npc_type = "event")
///
/// BẮT BUỘC: tất cả NPC prefab phải có NetworkObject component
///           VÀ phải đăng ký trong NetworkManager → NetworkPrefabs list.
/// </summary>
public class NpcServerManager : MonoBehaviour
{
    public static NpcServerManager Instance { get; private set; }

    [Header("API")]
    [SerializeField] private string apiBase = "";

    [Tooltip("MapId của scene này. Để 0 → tự lấy từ MapManager (có thể race condition nếu MapManager chưa fetch xong).")]
    [SerializeField] private int mapId = 0;

    [Header("NPC Prefabs theo type — shop=0, blacksmith=1, quest=2, exchange=3, event=4")]
    [Tooltip("Element 0=shop, 1=blacksmith, 2=quest, 3=exchange, 4=event")]
    [SerializeField] private GameObject[] npcPrefabs;

    [Header("NPC Prefabs theo ID (ưu tiên hơn type — dùng khi cùng type nhưng khác prefab)")]
    [SerializeField] private NpcIdPrefabEntry[] npcPrefabsById;

    /// <summary>Server-side cache: NetworkObjectId → NpcData (dùng để validate trong NpcInteraction).</summary>
    private readonly Dictionary<ulong, NpcData> _npcCache = new();
    private readonly HashSet<string> _spawnedNpcKeys = new();
    private bool _hasSpawned;

    public string ApiBase => apiBase;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        apiBase = ServerAddressConfig.Instance.ResolveApiRoot(apiBase);
    }

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        if (nm.IsServer)
        {
            // Host/Server đã start trước khi scene này load
            SpawnAll();
        }
        else
        {
            // Client: đăng ký để spawn khi server start (trường hợp scene load trước StartHost)
            nm.OnServerStarted += OnServerStarted;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
    }

    private void OnServerStarted()
    {
        NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        SpawnAll();
    }

    private void SpawnAll()
    {
        if (_hasSpawned)
            return;

        _hasSpawned = true;
        StartCoroutine(LoadAndSpawnConfiguredMaps());
    }

    // ─────────────────────────────────────────────────────────

    private IEnumerator LoadAndSpawnConfiguredMaps()
    {
        foreach (int targetMapId in ResolveTargetMapIds())
            yield return StartCoroutine(LoadAndSpawnNpcsForMap(targetMapId));
    }

    private IEnumerable<int> ResolveTargetMapIds()
    {
        var yielded = new HashSet<int>();

        if (mapId >= 0)
        {
            int resolvedMapId = ResolveSingleMapId(mapId);
            if (yielded.Add(resolvedMapId))
                yield return resolvedMapId;
            yield break;
        }

        MapWorldConfig config = ZoneRoomRegistry.Instance?.Config;
        if (config != null && config.maps != null)
        {
            foreach (var mapDef in config.maps)
            {
                if (yielded.Add(mapDef.mapId))
                    yield return mapDef.mapId;
            }
            yield break;
        }

        if (MapManager.Instance != null)
        {
            int currentMapId = MapManager.Instance.GetMapId();
            if (yielded.Add(currentMapId))
                yield return currentMapId;
        }
    }

    private int ResolveSingleMapId(int configuredMapId)
    {
        if (configuredMapId == 0 && MapManager.Instance != null && !IsDedicatedWorldServer())
            return MapManager.Instance.GetMapId();

        return configuredMapId;
    }

    private IEnumerator LoadAndSpawnNpcsForMap(int targetMapId)
    {
        string url = $"{apiBase}/api/npc/list?mapId={targetMapId}";
        using var req = UnityWebRequest.Get(url);
        if (IsDedicatedWorldServer())
        {
            string apiKey = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey();
            if (!string.IsNullOrWhiteSpace(apiKey))
                req.SetRequestHeader("X-Zone-Api-Key", apiKey);
        }
        else
        {
            req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        }
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[NpcServerManager] GET {url} lỗi: {req.error}");
            yield break;
        }

        NpcListWrapper resp;
        try
        {
            // API trả array JSON thô → bọc lại để JsonUtility parse
            resp = JsonUtility.FromJson<NpcListWrapper>("{\"npcs\":" + req.downloadHandler.text + "}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NpcServerManager] Parse lỗi: {ex.Message}");
            yield break;
        }

        if (resp?.npcs == null)
        {
            Debug.LogWarning($"[NpcServerManager] Không có NPC nào cho mapId={targetMapId}.");
            yield break;
        }

        // Spawn MỖI NPC đúng 1 lần cho cả map — KHÔNG nhân bản theo zone.
        // Visibility sẽ filter theo MAP (tất cả player cùng map thấy NPC, bất kể zone nào).
        foreach (var npc in resp.npcs)
        {
            var prefab = GetPrefab(npc);
            if (prefab == null)
            {
                Debug.LogWarning($"[NpcServerManager] Không tìm được prefab cho npc_type='{npc.npc_type}'. Bỏ qua '{npc.npc_name}'.");
                continue;
            }

            SpawnNpcInstance(prefab, npc, targetMapId);
        }

        Debug.Log($"[NpcServerManager] Đã spawn {resp.npcs.Length} NPC(s) trên mapId={targetMapId}.");
    }

    private void SpawnNpcInstance(GameObject prefab, NpcData npc, int targetMapId)
    {
        string spawnKey = $"map{targetMapId}_npc{npc.npc_id}";
        if (!_spawnedNpcKeys.Add(spawnKey))
            return;

        var obj = Instantiate(prefab, new Vector3(npc.pos_x, npc.pos_y, 0f), Quaternion.identity);
        var netObj = obj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"[NpcServerManager] Prefab '{prefab.name}' thiếu NetworkObject! Destroy.");
            Destroy(obj);
            return;
        }

        // Server-side: tắt physics (NPC là static, không cần Rigidbody2D trên server)
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        // Gắn map-based visibility (visible cho TẤT CẢ player cùng map, bất kể zone)
        ApplyMapVisibility(obj, targetMapId);

        netObj.Spawn();
        StartCoroutine(DelayedRefreshVisibility(obj));

        var inter = obj.GetComponent<NpcInteraction>();
        if (inter != null)
            inter.InitOnServer(npc);

        _npcCache[netObj.NetworkObjectId] = npc;

        Debug.Log($"[NpcServerManager] Spawned '{npc.npc_name}' ({npc.npc_type}) tại ({npc.pos_x}, {npc.pos_y}) map={targetMapId}");
    }

    /// <summary>
    /// Gắn map-based visibility: NPC visible cho TẤT CẢ player cùng map (bất kể zone).
    /// Zone chỉ dùng để isolate player-to-player, không dùng cho NPC/Enemy.
    /// </summary>
    private static void ApplyMapVisibility(GameObject obj, int targetMapId)
    {
        var zoneTag = obj.GetComponent<ZoneOwnerTag>() ?? obj.AddComponent<ZoneOwnerTag>();
        zoneTag.SetZone(targetMapId, 0);

        var filter = obj.GetComponent<NetworkVisibilityZoneFilter>() ?? obj.AddComponent<NetworkVisibilityZoneFilter>();
        filter.InitializeForServer();
    }

    private IEnumerator DelayedRefreshVisibility(GameObject obj)
    {
        yield return null;
        if (obj != null)
            obj.GetComponent<NetworkVisibilityZoneFilter>()?.RefreshVisibility();
    }

    private static bool IsDedicatedWorldServer()
        => FindAnyObjectByType<MapWorldBootstrap>() != null;

    private GameObject GetPrefab(NpcData npc)
    {
        // Ưu tiên map theo npc_id trước
        if (npcPrefabsById != null)
        {
            foreach (var entry in npcPrefabsById)
            {
                if (entry.npcId == npc.npc_id && entry.prefab != null)
                    return entry.prefab;
            }
        }

        // Fallback: map theo npc_type
        int idx = npc.npc_type switch
        {
            "shop"       => 0,
            "blacksmith" => 1,
            "quest"      => 2,
            "exchange"   => 3,
            "event"      => 4,
            _            => 0
        };
        return npcPrefabs != null && idx < npcPrefabs.Length ? npcPrefabs[idx] : null;
    }

    /// <summary>Được dùng bởi NpcInteraction để validate và lấy NpcData từ server cache.</summary>
    public bool TryGetNpcData(ulong networkObjectId, out NpcData data)
        => _npcCache.TryGetValue(networkObjectId, out data);
}
