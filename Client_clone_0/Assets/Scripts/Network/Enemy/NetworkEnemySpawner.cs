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
    private Dictionary<int, GameObject> spawnedEnemies = new Dictionary<int, GameObject>(); // spawn_id -> GameObject
    private Dictionary<int, float> lastRespawnTime = new Dictionary<int, float>(); // spawn_id -> last respawn time

    private void Awake()
    {
        if (!spawnOnServerOnly)
            return;

        var networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
            return;

        networkObject.SpawnWithObservers = false;
        MapSceneManager.ConfigureNetworkObjectForServerOnlyScene(networkObject);
    }

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
        if (!IsServer || hasStartedLoading)
        {
            if (!IsServer)
                Debug.Log($"[NetworkEnemySpawner] TryStartLoading skipped because IsServer=false. scene={gameObject.scene.name}, mapId={mapId}");
            return;
        }

        // Chỉ bỏ qua nếu spawner này đang nằm trong scene dungeon wave thật.
        // ServerScene có thể chứa WaveDungeonRuntime để điều phối dungeon,
        // nhưng map thường vẫn phải spawn theo map_spawn_config.
        if (gameObject.scene.name.Contains("DungeonWaveScene", StringComparison.OrdinalIgnoreCase) &&
            FindObjectOfType<WaveDungeonRuntime>() != null)
        {
            Debug.Log($"[NetworkEnemySpawner] WaveDungeonRuntime detected in dungeon scene — skipping. scene={gameObject.scene.name} mapId={mapId} isServer={IsServer}");
            return;
        }

        if (mapId <= 0)
        {
            Debug.LogWarning($"[NetworkEnemySpawner] mapId invalid ({mapId}) in scene={gameObject.scene.name}. Try using MapWorldConfig fallback or MapManager.");
        }

        if (FindObjectOfType<WaveDungeonRuntime>() != null)
        {
            Debug.Log($"[NetworkEnemySpawner] WaveDungeonRuntime exists but scene is not dungeon wave. Continuing normal map spawn. scene={gameObject.scene.name} mapId={mapId}");
        }

        Debug.Log($"[NetworkEnemySpawner] Start loading. scene={gameObject.scene.name}, mapId={mapId}, isServer={IsServer}, hasStartedLoading={hasStartedLoading}, apiBaseURL={apiBaseURL}");

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

        if (mapId > 0)
        {
            int resolvedMapId = ResolveSingleMapId(mapId);
            if (yielded.Add(resolvedMapId))
            {
                bool canSpawn = ShouldAutoSpawnForMap(resolvedMapId);
                Debug.Log($"[NetworkEnemySpawner] ResolveSingleMapId={resolvedMapId} canSpawn={canSpawn} scene={gameObject.scene.name}");
                if (canSpawn)
                    yield return resolvedMapId;
            }
            yield break;
        }

        if (MapManager.Instance != null)
        {
            int currentMapId = MapManager.Instance.GetMapId();
            if (yielded.Add(currentMapId))
            {
                bool canSpawn = ShouldAutoSpawnForMap(currentMapId);
                Debug.Log($"[NetworkEnemySpawner] MapManager fallback currentMapId={currentMapId} canSpawn={canSpawn} scene={gameObject.scene.name}");
                if (canSpawn)
                    yield return currentMapId;
            }
            yield break;
        }

        MapWorldConfig config = ZoneRoomRegistry.Instance?.Config;
        if (config != null && config.maps != null)
        {
            foreach (var mapDef in config.maps)
            {
                if (mapDef == null)
                    continue;

                if (!ShouldAutoSpawnForMap(mapDef.mapId))
                    continue;

                if (yielded.Add(mapDef.mapId))
                {
                    bool canSpawn = ShouldAutoSpawnForMap(mapDef.mapId);
                    Debug.Log($"[NetworkEnemySpawner] MapManager resolved mapId={mapDef.mapId} canSpawn={canSpawn} scene={gameObject.scene.name}");
                    if (canSpawn)
                        yield return mapDef.mapId;
                }
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

    private bool ShouldAutoSpawnForMap(int targetMapId)
    {
        MapWorldConfig config = ZoneRoomRegistry.Instance?.Config;
        MapDefinition mapDef = config != null ? config.GetMap(targetMapId) : null;
        if (mapDef != null && mapDef.zoneTopology == MapZoneTopology.InstanceOnly)
        {
            Debug.Log($"[NetworkEnemySpawner] Skip auto-spawn for map {targetMapId} because it is InstanceOnly. scene={gameObject.scene.name}, mapName={mapDef.mapName}, zoneTopology={mapDef.zoneTopology}");
            return false;
        }

        return true;
    }

    private IEnumerator LoadAndSpawnEnemiesForMap(int targetMapId)
    {
        if (!ShouldAutoSpawnForMap(targetMapId))
            yield break;

        Debug.Log($"[NetworkEnemySpawner] Loading enemy spawns for map {targetMapId}... scene={gameObject.scene.name}, apiBaseURL={apiBaseURL}, isServer={IsServer}, dedicated={IsDedicatedWorldServer()}");

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
                Debug.Log($"[NetworkEnemySpawner] Response length={jsonResponse?.Length ?? 0}, url={url}");

                // Parse JSON response
                EnemySpawnResponse response = JsonUtility.FromJson<EnemySpawnResponse>(jsonResponse);
                
                if (response != null && response.enemy_spawns != null && response.enemy_spawns.Length > 0)
                {
                    Debug.Log($"[NetworkEnemySpawner] Found {response.enemy_spawns.Length} enemy spawn points for map {targetMapId}");
                    
                    // Spawn MỖI enemy đúng 1 lần cho cả map — KHÔNG nhân bản theo zone.
                    foreach (var spawnData in response.enemy_spawns)
                    {
                        Debug.Log($"[NetworkEnemySpawner] Spawn entry spawn_id={spawnData.spawn_id} enemy_type_id={spawnData.enemy_type_id} max_spawn_count={spawnData.max_spawn_count} is_boss={spawnData.is_boss} respawn={spawnData.respawn_time}");
                        SpawnEnemyAtPoint(spawnData, targetMapId);
                    }
                    
                    loadedMapIds.Add(targetMapId);
                }
                else
                {
                    Debug.LogWarning($"[NetworkEnemySpawner] No enemy spawns found for map {targetMapId}; response={(response == null ? "null" : "non-null")}");
                }
            }
            else
            {
                Debug.LogError($"[NetworkEnemySpawner] Failed to load enemy spawns: {www.error}");
            }
        }
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

        bool watchBoss25 = spawnData.enemy_type_id == 25
            || (spawnData.enemy != null && spawnData.enemy.enemy_id == 25)
            || spawnData.is_boss
            || enemyPrefab.GetComponent<BossAI>() != null
            || enemyPrefab.name.Contains("Enemy 25");
        if (watchBoss25)
        {
            BossAI prefabBossAI = enemyPrefab.GetComponent<BossAI>();
            EnemyAI prefabEnemyAI = enemyPrefab.GetComponent<EnemyAI>();
            Debug.LogWarning(
                $"[BOSS25][NetworkEnemySpawner] Resolve prefab spawn_id={spawnData.spawn_id} enemy_type_id={spawnData.enemy_type_id} enemy.enemy_id={(spawnData.enemy != null ? spawnData.enemy.enemy_id : 0)} enemyName='{(spawnData.enemy != null ? spawnData.enemy.enemy_name : "")}' prefab={enemyPrefab.name} spawnData.is_boss={spawnData.is_boss} enemy_type='{(spawnData.enemy != null ? spawnData.enemy.enemy_type : "")}' prefabHasBossAI={(prefabBossAI != null)} prefabBossEnabled={(prefabBossAI != null && prefabBossAI.enabled)} prefabEnemyAIEnabled={(prefabEnemyAI != null && prefabEnemyAI.enabled)} map={targetMapId}",
                this);
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
            if (watchBoss25)
            {
                BossAI instanceBossAI = enemyObj.GetComponent<BossAI>();
                EnemyAI instanceEnemyAI = enemyObj.GetComponent<EnemyAI>();
                Debug.LogWarning(
                    $"[BOSS25][NetworkEnemySpawner] Instantiate spawn_id={spawnData.spawn_id} instance={enemyObj.name} pos={spawnPosition} hasBossAI={(instanceBossAI != null)} bossAIEnabled={(instanceBossAI != null && instanceBossAI.enabled)} enemyAIEnabled={(instanceEnemyAI != null && instanceEnemyAI.enabled)}",
                    enemyObj);
            }

            NetworkObject networkObj = enemyObj.GetComponent<NetworkObject>();
            
            if (networkObj != null)
            {
                // Server-side: tắt gravity (ServerScene không có ground)
                var rb = enemyObj.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.gravityScale = 0f;

                // Di chuyển vào physics scene riêng của map — TRƯỚC Spawn()
                MapSceneManager.Instance?.MoveToMapScene(enemyObj, targetMapId);

                // Map-based visibility: visible cho TẤT CẢ player cùng map
                ApplyMapVisibility(enemyObj, targetMapId);
                networkObj.Spawn();
                StartCoroutine(DelayedRefreshVisibility(enemyObj));

                ApplyEnemyOverrides(enemyObj, spawnData, targetMapId);

                if (watchBoss25)
                {
                    BossAI instanceBossAI = enemyObj.GetComponent<BossAI>();
                    EnemyAI instanceEnemyAI = enemyObj.GetComponent<EnemyAI>();
                    Debug.LogWarning(
                        $"[BOSS25][NetworkEnemySpawner] After overrides spawn_id={spawnData.spawn_id} netSpawned={networkObj.IsSpawned} scene={enemyObj.scene.name} bossAIEnabled={(instanceBossAI != null && instanceBossAI.enabled)} enemyAIEnabled={(instanceEnemyAI != null && instanceEnemyAI.enabled)}",
                        enemyObj);
                }

                Debug.Log($"[NetworkEnemySpawner] Spawned '{spawnData.enemy?.enemy_name ?? "Unknown"}' at ({spawnData.spawn_x}, {spawnData.spawn_y}) [copy {i+1}/{spawnData.max_spawn_count}] map={targetMapId}");
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
        bool forceBossMode = spawnData.enemy_type_id == 25
            || (spawnData.enemy != null && spawnData.enemy.enemy_id == 25)
            || enemyObj.GetComponent<BossAI>() != null;
        if (forceBossMode && !isBoss)
        {
            Debug.LogWarning(
                $"[BOSS25][NetworkEnemySpawner] Force boss mode spawn_id={spawnData.spawn_id} enemy_type_id={spawnData.enemy_type_id} enemy.enemy_id={(spawnData.enemy != null ? spawnData.enemy.enemy_id : 0)}",
                enemyObj);
            isBoss = true;
        }

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

                // Gán element_type vào EnemySkillSet (server-only, cho AI/Host đọc)
                var skillSet = enemyObj.GetComponent<EnemySkillSet>();
                if (skillSet == null)
                    skillSet = enemyObj.AddComponent<EnemySkillSet>();
                if (spawnData.enemy != null && !string.IsNullOrEmpty(spawnData.enemy.element_type))
                {
                    var entry = new EnemySkillsEntry { element_type = spawnData.enemy.element_type };
                    skillSet.SetSkillsFromConfig(entry);
                }

                // Sync tên, hệ, level cho TẤT CẢ client qua NetworkEnemyHealth ClientRpc
                var netHealth = enemyObj.GetComponent<NetworkEnemyHealth>();
                if (netHealth != null)
                {
                    netHealth.SetEnemyInfo(
                        spawnData.enemy?.enemy_name ?? enemyObj.name,
                        spawnData.enemy?.element_type ?? "None",
                        resolvedLevel,
                        spawnData.enemy?.enemy_id ?? 0);
                }

        if (spawnData.enemy != null && spawnData.enemy.base_damage > 0)
        {
            var bossAI = enemyObj.GetComponent<BossAI>();
            if (bossAI != null)
                bossAI.ApplyRuntimeOverride(spawnData.enemy.base_damage, spawnData.enemy.move_speed);
        }

        var itemDrop = enemyObj.GetComponent<EnemyItemDrop>();
        int resolvedEnemyId = spawnData.enemy?.enemy_id > 0 ? spawnData.enemy.enemy_id : spawnData.enemy_type_id;

        if (itemDrop != null && spawnData.enemy?.drops != null && spawnData.enemy.drops.Length > 0)
        {
            var configuredDrops = new List<DropItemEntry>();
            foreach (var drop in spawnData.enemy.drops)
            {
                if (drop.item_id <= 0)
                    continue;

                int qtyMin = Mathf.Max(1, drop.qty_min);
                int qtyMax = Mathf.Max(qtyMin, drop.qty_max);

                configuredDrops.Add(new DropItemEntry
                {
                    item_id = drop.item_id,
                    rate = Mathf.Clamp01(drop.rate),
                    qty_min = qtyMin,
                    qty_max = qtyMax
                });
            }

            if (configuredDrops.Count > 0)
            {
                itemDrop.SetDropsFromConfig(configuredDrops);
                Debug.Log($"[NetworkEnemySpawner] map={targetMapId} enemy_id={resolvedEnemyId}: dùng {configuredDrops.Count} drop rules từ reward_json.");
            }
        }
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
        public int silver_reward;
        public DropItemEntry[] drops;
        public string element_type;
        public string enemy_type;
    }
}
