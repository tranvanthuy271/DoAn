using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Network Enemy Spawner - Load enemy spawns từ API và spawn enemy trong map
/// </summary>
public class NetworkEnemySpawner : NetworkBehaviour
{
    [Header("Configuration")]
    [Tooltip("Enemy Prefab Manager reference")]
    public EnemyPrefabManager enemyPrefabManager;
    
    [Tooltip("API Base URL")]
    public string apiBaseURL = "";
    
    [Tooltip("Map ID (sẽ lấy từ MapManager nếu không set)")]
    public int mapId = 0;

    [Header("Spawn Settings")]
    [Tooltip("Chỉ spawn trên server")]
    public bool spawnOnServerOnly = true;

    private bool hasStartedLoading = false;
    private readonly HashSet<int> loadedMapIds = new HashSet<int>();
    private readonly HashSet<string> _spawnedEnemyKeys = new HashSet<string>();
    private readonly Dictionary<int, Dictionary<int, List<DropItemEntry>>> _mapDropLookup = new Dictionary<int, Dictionary<int, List<DropItemEntry>>>();
    private Dictionary<int, GameObject> spawnedEnemies = new Dictionary<int, GameObject>(); // spawn_id -> GameObject
    private Dictionary<int, float> lastRespawnTime = new Dictionary<int, float>(); // spawn_id -> last respawn time

    private void Start()
    {
        apiBaseURL = ServerAddressConfig.Instance.ResolveApiUrl(apiBaseURL);

        // Lấy Map ID từ MapManager nếu chưa set
        if (mapId == 0 && MapManager.Instance != null && !IsDedicatedWorldServer())
        {
            mapId = MapManager.Instance.GetMapId();
        }

        // Chỉ server spawn enemy — nếu IsServer đã sẵn sàng
        TryStartLoading();
    }

    /// <summary>
    /// Gọi bởi NGO sau khi NetworkObject được spawn.
    /// Trên dedicated server, Start() chạy trước StartServer() nên IsServer = false.
    /// OnNetworkSpawn() chạy SAU StartServer() → IsServer = true → spawn enemy.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TryStartLoading();
    }

    private void TryStartLoading()
    {
        if (!IsServer || hasStartedLoading) return;
        hasStartedLoading = true;
        StartCoroutine(LoadAndSpawnEnemies());
    }

    /// <summary>
    /// Load enemy spawns từ API và spawn
    /// </summary>
    private IEnumerator LoadAndSpawnEnemies()
    {
        foreach (int targetMapId in ResolveTargetMapIds())
        {
            if (loadedMapIds.Contains(targetMapId))
                continue;

            yield return StartCoroutine(LoadAndSpawnEnemiesForMap(targetMapId));
        }
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

    private IEnumerator LoadAndSpawnEnemiesForMap(int targetMapId)
    {
        Debug.Log($"[NetworkEnemySpawner] Loading enemy spawns for map {targetMapId}...");

        yield return StartCoroutine(LoadMapDropConfig(targetMapId));

        string url = $"{apiBaseURL}/enemyspawn/{targetMapId}/spawns";
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            if (IsDedicatedWorldServer())
            {
                string apiKey = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey();
                if (!string.IsNullOrWhiteSpace(apiKey))
                    www.SetRequestHeader("X-Zone-Api-Key", apiKey);
            }

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"[NetworkEnemySpawner] Enemy spawns loaded: {jsonResponse}");

                // Parse JSON response
                EnemySpawnResponse response = JsonUtility.FromJson<EnemySpawnResponse>(jsonResponse);
                
                if (response != null && response.enemy_spawns != null && response.enemy_spawns.Length > 0)
                {
                    Debug.Log($"[NetworkEnemySpawner] Found {response.enemy_spawns.Length} enemy spawn points for map {targetMapId}");
                    
                    // Spawn MỖI enemy đúng 1 lần cho cả map — KHÔNG nhân bản theo zone.
                    foreach (var spawnData in response.enemy_spawns)
                    {
                        SpawnEnemyAtPoint(spawnData, targetMapId);
                    }
                    
                    loadedMapIds.Add(targetMapId);
                }
                else
                {
                    Debug.LogWarning($"[NetworkEnemySpawner] No enemy spawns found for map {targetMapId}");
                }
            }
            else
            {
                Debug.LogError($"[NetworkEnemySpawner] Failed to load enemy spawns: {www.error}");
            }
        }
    }

    private IEnumerator LoadMapDropConfig(int targetMapId)
    {
        string url = $"{apiBaseURL}/map/{targetMapId}/spawn-config";

        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            if (IsDedicatedWorldServer())
            {
                string apiKey = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey();
                if (!string.IsNullOrWhiteSpace(apiKey))
                    www.SetRequestHeader("X-Zone-Api-Key", apiKey);
            }

            yield return www.SendWebRequest();

            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                _mapDropLookup.Remove(targetMapId);
                Debug.LogWarning($"[NetworkEnemySpawner] Không load được map_spawn_config cho map {targetMapId}: {www.error}. Sẽ fallback về enemy.drop_items_json.");
                yield break;
            }

            try
            {
                var response = JsonUtility.FromJson<MapSpawnConfigResponse>(www.downloadHandler.text);
                var lookup = BuildDropLookup(response?.drops);
                _mapDropLookup[targetMapId] = lookup;
                Debug.Log($"[NetworkEnemySpawner] Map {targetMapId}: loaded {lookup.Count} enemy drop configs từ map_spawn_config.");
            }
            catch (Exception ex)
            {
                _mapDropLookup.Remove(targetMapId);
                Debug.LogWarning($"[NetworkEnemySpawner] Parse map_spawn_config thất bại cho map {targetMapId}: {ex.Message}. Sẽ fallback về enemy.drop_items_json.");
            }
        }
    }

    private static Dictionary<int, List<DropItemEntry>> BuildDropLookup(DropEntry[] drops)
    {
        var lookup = new Dictionary<int, List<DropItemEntry>>();
        if (drops == null)
            return lookup;

        foreach (var dropEntry in drops)
        {
            if (dropEntry.enemy_id <= 0 || dropEntry.items == null || dropEntry.items.Length == 0)
                continue;

            var validatedItems = new List<DropItemEntry>();
            foreach (var item in dropEntry.items)
            {
                if (item.item_id <= 0)
                    continue;

                int minQty = Mathf.Max(1, item.qty_min);
                int maxQty = Mathf.Max(minQty, item.qty_max);
                float clampedRate = Mathf.Clamp01(item.rate);

                validatedItems.Add(new DropItemEntry
                {
                    item_id = item.item_id,
                    rate = clampedRate,
                    qty_min = minQty,
                    qty_max = maxQty
                });
            }

            if (validatedItems.Count > 0)
                lookup[dropEntry.enemy_id] = validatedItems;
        }

        return lookup;
    }

    /// <summary>
    /// Spawn enemy tại một spawn point
    /// </summary>
    private void SpawnEnemyAtPoint(EnemySpawnData spawnData, int targetMapId)
    {
        if (enemyPrefabManager == null)
        {
            Debug.LogError("[NetworkEnemySpawner] EnemyPrefabManager is null!");
            return;
        }

        GameObject enemyPrefab = enemyPrefabManager.GetEnemyPrefab(spawnData.enemy_type_id);
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[NetworkEnemySpawner] Enemy prefab not found for enemy_type_id {spawnData.enemy_type_id} (spawn_id: {spawnData.spawn_id})");
            return;
        }

        Vector3 spawnPosition = new Vector3(spawnData.spawn_x, spawnData.spawn_y, 0f);
        
        for (int i = 0; i < spawnData.max_spawn_count; i++)
        {
            string spawnKey = $"map{targetMapId}_spawn{spawnData.spawn_id}_i{i}";
            if (!_spawnedEnemyKeys.Add(spawnKey))
            {
                Debug.LogWarning($"[NetworkEnemySpawner] Dedup skip: {spawnKey}");
                continue;
            }

            GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            NetworkObject networkObj = enemyObj.GetComponent<NetworkObject>();
            
            if (networkObj != null)
            {
                // Server-side: tắt gravity (ServerScene không có ground)
                var rb = enemyObj.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.gravityScale = 0f;

                // Map-based visibility: visible cho TẤT CẢ player cùng map
                ApplyMapVisibility(enemyObj, targetMapId);
                networkObj.Spawn();
                StartCoroutine(DelayedRefreshVisibility(enemyObj));

                ApplyEnemyOverrides(enemyObj, spawnData, targetMapId);

                Debug.Log($"[NetworkEnemySpawner] Spawned '{spawnData.enemy?.enemy_name ?? "Unknown"}' at ({spawnData.spawn_x}, {spawnData.spawn_y}) map={targetMapId}");
            }
            else
            {
                Debug.LogError($"[NetworkEnemySpawner] Enemy prefab missing NetworkObject!");
                Destroy(enemyObj);
            }
        }

        lastRespawnTime[spawnData.spawn_id] = Time.time;
    }

    private IEnumerator DelayedRefreshVisibility(GameObject obj)
    {
        yield return null; // chờ 1 frame
        if (obj != null)
            obj.GetComponent<NetworkVisibilityZoneFilter>()?.RefreshVisibility();
    }

    /// <summary>
    /// Respawn enemy sau khi bị kill
    /// </summary>
    public void RespawnEnemy(int spawnId, EnemySpawnData spawnData)
    {
        if (!IsServer) return;

        if (Time.time - lastRespawnTime[spawnId] >= spawnData.respawn_time)
        {
            SpawnEnemyAtPoint(spawnData, mapId);
            lastRespawnTime[spawnId] = Time.time;
        }
    }

    private void ApplyEnemyOverrides(GameObject enemyObj, EnemySpawnData spawnData, int targetMapId)
    {
        int resolvedHp = spawnData.override_hp > 0
            ? spawnData.override_hp
            : spawnData.enemy?.base_hp ?? 0;

        int resolvedExp = spawnData.override_exp > 0
            ? spawnData.override_exp
            : spawnData.enemy?.exp_reward ?? 0;

        bool isBoss = spawnData.is_boss;
        if (!isBoss && spawnData.enemy != null)
            isBoss = string.Equals(spawnData.enemy.enemy_type, "Boss", StringComparison.OrdinalIgnoreCase);

        int resolvedLevel = spawnData.level > 0
            ? spawnData.level
            : spawnData.enemy?.level ?? 1;

        var statOverride = enemyObj.GetComponent<EnemyStatOverride>() ?? enemyObj.AddComponent<EnemyStatOverride>();
        statOverride.Apply(
            resolvedHp,
            resolvedExp,
            isBoss,
            spawnData.respawn_time,
            resolvedLevel,
            spawnData.enemy?.enemy_name ?? string.Empty);

        var itemDrop = enemyObj.GetComponent<EnemyItemDrop>();
        int resolvedEnemyId = spawnData.enemy?.enemy_id > 0 ? spawnData.enemy.enemy_id : spawnData.enemy_type_id;

        if (itemDrop != null
            && _mapDropLookup.TryGetValue(targetMapId, out var mapDrops)
            && mapDrops.TryGetValue(resolvedEnemyId, out var configuredDrops)
            && configuredDrops.Count > 0)
        {
            itemDrop.SetDropsFromConfig(configuredDrops);
            Debug.Log($"[NetworkEnemySpawner] map={targetMapId} enemy_id={resolvedEnemyId}: dùng {configuredDrops.Count} drop rules từ map_spawn_config.");
            return;
        }

        // Apply drop rules từ drop_items_json của enemy (fallback spawner)
        string dropJson = spawnData.enemy?.drop_items_json;
        if (itemDrop != null && !string.IsNullOrEmpty(dropJson))
        {
            var drops = ParseDropItemsJson(dropJson);
            if (drops != null && drops.Count > 0)
            {
                itemDrop.SetDropsFromConfig(drops);
                Debug.Log($"[NetworkEnemySpawner] map={targetMapId} enemy_id={resolvedEnemyId}: fallback drop rules từ enemy.drop_items_json.");
            }
        }
    }

    /// <summary>
    /// Parse enemy.drop_items_json (format: [{item_id, drop_chance, qty_min, qty_max}])
    /// thành List&lt;DropItemEntry&gt; để dùng với EnemyItemDrop.SetDropsFromConfig.
    /// </summary>
    private static List<DropItemEntry> ParseDropItemsJson(string json)
    {
        var result = new List<DropItemEntry>();
        if (string.IsNullOrWhiteSpace(json)) return result;
        try
        {
            // JsonUtility không hỗ trợ array trực tiếp — dùng wrapper
            var wrapper = JsonUtility.FromJson<DropJsonWrapper>("{\"items\":" + json + "}");
            if (wrapper?.items == null) return result;
            foreach (var item in wrapper.items)
            {
                if (item.item_id <= 0) continue;
                result.Add(new DropItemEntry
                {
                    item_id = item.item_id,
                    rate    = item.drop_chance,   // 0–1 (API format)
                    qty_min = Mathf.Max(1, item.qty_min),
                    qty_max = Mathf.Max(1, item.qty_max)
                });
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[NetworkEnemySpawner] ParseDropItemsJson thất bại: {ex.Message}");
        }
        return result;
    }

    [System.Serializable] private class DropJsonWrapper { public DropItemsEntry[] items; }
    [System.Serializable] private class DropItemsEntry
    {
        public int   item_id;
        public float drop_chance;
        public int   qty_min;
        public int   qty_max;
    }

    /// <summary>
    /// Gắn map-based visibility: enemy visible cho TẤT CẢ player cùng map.
    /// </summary>
    private static void ApplyMapVisibility(GameObject enemyObj, int targetMapId)
    {
        var zoneTag = enemyObj.GetComponent<ZoneOwnerTag>() ?? enemyObj.AddComponent<ZoneOwnerTag>();
        zoneTag.SetZone(targetMapId, 0);

        var filter = enemyObj.GetComponent<NetworkVisibilityZoneFilter>() ?? enemyObj.AddComponent<NetworkVisibilityZoneFilter>();
        filter.InitializeForServer();
    }

    private static bool IsDedicatedWorldServer()
        => FindAnyObjectByType<MapWorldBootstrap>() != null;

    // JSON Response Classes
    [System.Serializable]
    public class EnemySpawnResponse
    {
        public int map_id;
        public EnemySpawnData[] enemy_spawns;
    }

    [System.Serializable]
    public class EnemySpawnData
    {
        public int spawn_id;
        public int enemy_type_id;
        public float spawn_x;
        public float spawn_y;
        public int max_spawn_count;
        public int respawn_time;
        public int override_hp;
        public int override_exp;
        public bool is_boss;
        public int level;
        public EnemyData enemy;
    }

    [System.Serializable]
    public class EnemyData
    {
        public int enemy_id;
        public string enemy_name;
        public string enemy_description;
        public int level;
        public int base_hp;
        public int base_mp;
        public int base_damage;
        public int base_defense;
        public float move_speed;
        public float attack_speed;
        public int exp_reward;
        public int gold_reward;
        public string element_type;
        public string enemy_type;
        public string drop_items_json;   // JSON drop rules từ enemy.drop_items_json trong DB
    }
}
