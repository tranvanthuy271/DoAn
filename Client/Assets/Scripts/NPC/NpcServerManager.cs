using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

// Server-authoritative NPC manager — thay thế NpcSpawner.cs.
// Chỉ chạy spawn logic khi IsServer (đúng với cả Host và dedicated server sau này).
// Client không cần làm gì — NGO tự replicate NetworkObject sang client khi server Spawn().
// Setup Inspector:
// apiBase  = "http://localhost:5000"
// mapId    = mapId của scene này (đặt số cụ thể, ví dụ: 1)
// npcPrefabs[0] = NPC_Shop_Prefab        (npc_type = "shop")
// npcPrefabs[1] = NPC_Blacksmith_Prefab  (npc_type = "blacksmith")
// npcPrefabs[2] = NPC_Quest_Prefab       (npc_type = "quest")
// npcPrefabs[3] = NPC_Exchange_Prefab    (npc_type = "exchange")
// npcPrefabs[4] = NPC_Event_Prefab       (npc_type = "event")
// BẮT BUỘC: tất cả NPC prefab phải có NetworkObject component
// VÀ phải đăng ký trong NetworkManager → NetworkPrefabs list.
public class NpcServerManager : MonoBehaviour
{
    private const string LogPrefix = "[NpcServerManager]";

    public static NpcServerManager Instance { get; private set; }

    [Header("API")]
    [SerializeField] private string apiBase = "";

    [Tooltip("MapId của scene này. Để -1 → auto-detect qua registry/MapManager.")]
    [SerializeField] private int mapId = -1;

    [Header("NPC Prefab Config")]
    [Tooltip("Ưu tiên ScriptableObject. Nếu bỏ trống sẽ tự load Resources/ScriptableObjects/NpcPrefabConfig.")]
    [SerializeField] private NpcPrefabConfig npcPrefabConfig;

    [Header("NPC Prefabs theo type — shop=0, blacksmith=1, quest=2, exchange=3, event=4")]
    [Tooltip("Element 0=shop, 1=blacksmith, 2=quest, 3=exchange, 4=event")]
    [SerializeField] private GameObject[] npcPrefabs;

    [Header("NPC Prefabs theo ID (ưu tiên hơn type — dùng khi cùng type nhưng khác prefab)")]
    [SerializeField] private NpcIdPrefabEntry[] npcPrefabsById;

    // Server-side cache: NetworkObjectId → NpcData (dùng để validate trong NpcInteraction).
    private readonly Dictionary<ulong, NpcData> _npcCache = new();
    private readonly HashSet<string> _spawnedNpcKeys = new();
    private NpcPrefabConfig _resolvedPrefabConfig;
    private bool _hasSpawned;

    public string ApiBase => apiBase;


    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        apiBase = ServerAddressConfig.Instance.ResolveApiRoot(apiBase);
        _resolvedPrefabConfig = NpcPrefabConfig.Resolve(npcPrefabConfig, this, nameof(NpcServerManager));
        if (_resolvedPrefabConfig != null)
        {
            { /* {LogPrefix} Using ScriptableObject prefab config '{_resolvedPrefabConfig.name}' */ }
        }
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
            if (yielded.Add(mapId))
                yield return mapId;
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
            if (currentMapId >= 0 && yielded.Add(currentMapId))
                yield return currentMapId;
        }
    }

    private IEnumerator LoadAndSpawnNpcsForMap(int targetMapId)
    {
        string url = $"{apiBase}/api/npc/list?mapId={targetMapId}";
        { /* {LogPrefix} Load NPC list | map={targetMapId} url={url} dedicated={IsDedicatedWorldServer()} */ }
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
            { /* Lỗi: GET {url} lỗi: {req.error} */ }
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
            { /* Lỗi: Parse lỗi: {ex.Message} */ }
            yield break;
        }

        if (resp?.npcs == null)
        {
            { /* Cảnh báo: {LogPrefix} Không có NPC nào cho mapId={targetMapId} */ }
            yield break;
        }

        { /* {LogPrefix} API returned {resp.npcs.Length} NPC(s) for map={targetMapId} */ }

        // Spawn MỖI NPC đúng 1 lần cho cả map — KHÔNG nhân bản theo zone.
        // Visibility sẽ filter theo MAP (tất cả player cùng map thấy NPC, bất kể zone nào).
        foreach (var npc in resp.npcs)
        {
            var prefab = GetPrefab(npc);
            if (prefab == null)
            {
                { /* Cảnh báo: {LogPrefix} Không tìm được prefab | npcId={npc.npc_id} type='{npc.npc_type}' name='{npc.npc_name}' */ }
                continue;
            }

            SpawnNpcInstance(prefab, npc, targetMapId);
        }

        { /* {LogPrefix} Đã spawn {resp.npcs.Length} NPC(s) trên mapId={targetMapId} */ }
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
            { /* Lỗi: Prefab '{prefab.name}' thiếu NetworkObject! Destroy */ }
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

        // Di chuyển vào physics scene riêng của map — TRƯỚC Spawn()
        MapSceneManager.Instance?.MoveToMapScene(obj, targetMapId);

        // Gắn map-based visibility (visible cho TẤT CẢ player cùng map, bất kể zone)
        ApplyMapVisibility(obj, targetMapId);

        netObj.Spawn();
        StartCoroutine(DelayedRefreshVisibility(obj));

        var inter = obj.GetComponent<NpcInteraction>();
        if (inter != null)
            inter.InitOnServer(npc);

        _npcCache[netObj.NetworkObjectId] = npc;

        { /* {LogPrefix} Spawned | npcId={npc.npc_id} name='{npc.npc_name}' type='{npc.npc_type}' prefab='{prefab.name}' map={targetMapId} pos=({npc.pos_x:F2}, {npc.pos_y:F2}) netId={netObj.NetworkObjectId} */ }
    }

    // Gắn map-based visibility: NPC visible cho TẤT CẢ player cùng map (bất kể zone).
    // Zone chỉ dùng để isolate player-to-player, không dùng cho NPC/Enemy.
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
        _resolvedPrefabConfig = NpcPrefabConfig.Resolve(npcPrefabConfig, this, nameof(NpcServerManager));
        GameObject configPrefab = _resolvedPrefabConfig?.ResolvePrefab(npc);
        if (configPrefab != null)
        {
            return configPrefab;
        }

        return GetLegacyPrefab(npc);
    }

    public void CollectConfiguredPrefabs(HashSet<GameObject> results)
    {
        _resolvedPrefabConfig = NpcPrefabConfig.Resolve(npcPrefabConfig, this, nameof(NpcServerManager));
        _resolvedPrefabConfig?.AppendAllPrefabs(results);

        if (npcPrefabs != null)
        {
            foreach (GameObject prefab in npcPrefabs)
            {
                if (prefab != null)
                {
                    results.Add(prefab);
                }
            }
        }

        if (npcPrefabsById != null)
        {
            foreach (NpcIdPrefabEntry entry in npcPrefabsById)
            {
                if (entry.prefab != null)
                {
                    results.Add(entry.prefab);
                }
            }
        }
    }

    private GameObject GetLegacyPrefab(NpcData npc)
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

    // Được dùng bởi NpcInteraction để validate và lấy NpcData từ server cache.
    public bool TryGetNpcData(ulong networkObjectId, out NpcData data)
        => _npcCache.TryGetValue(networkObjectId, out data);
}
