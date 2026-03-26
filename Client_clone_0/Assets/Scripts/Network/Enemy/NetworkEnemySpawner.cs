using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Network Enemy Spawner - Load enemy spawns từ API và spawn enemy trong map
/// </summary>
public class NetworkEnemySpawner : NetworkBehaviour
{
    [Header("Configuration")]
    [Tooltip("Enemy Prefab Manager reference")]
    public EnemyPrefabManager enemyPrefabManager;
    
    [Tooltip("API Base URL")]
    public string apiBaseURL = "http://localhost:5000/api";
    
    [Tooltip("Map ID (sẽ lấy từ MapManager nếu không set)")]
    public int mapId = 0;

    [Header("Spawn Settings")]
    [Tooltip("Chỉ spawn trên server")]
    public bool spawnOnServerOnly = true;

    private bool hasLoadedSpawns = false;
    private Dictionary<int, GameObject> spawnedEnemies = new Dictionary<int, GameObject>(); // spawn_id -> GameObject
    private Dictionary<int, float> lastRespawnTime = new Dictionary<int, float>(); // spawn_id -> last respawn time

    private void Start()
    {
        // Lấy Map ID từ MapManager nếu chưa set
        if (mapId == 0 && MapManager.Instance != null)
        {
            mapId = MapManager.Instance.GetMapId();
        }

        // Chỉ server spawn enemy
        if (IsServer)
        {
            StartCoroutine(LoadAndSpawnEnemies());
        }
    }

    /// <summary>
    /// Load enemy spawns từ API và spawn
    /// </summary>
    private IEnumerator LoadAndSpawnEnemies()
    {
        if (hasLoadedSpawns)
        {
            yield break;
        }

        Debug.Log($"[NetworkEnemySpawner] Loading enemy spawns for map {mapId}...");

        string url = $"{apiBaseURL}/enemyspawn/{mapId}/spawns";
        
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string jsonResponse = www.downloadHandler.text;
                Debug.Log($"[NetworkEnemySpawner] Enemy spawns loaded: {jsonResponse}");

                // Parse JSON response
                EnemySpawnResponse response = JsonUtility.FromJson<EnemySpawnResponse>(jsonResponse);
                
                if (response != null && response.enemy_spawns != null && response.enemy_spawns.Length > 0)
                {
                    Debug.Log($"[NetworkEnemySpawner] Found {response.enemy_spawns.Length} enemy spawn points");
                    
                    foreach (var spawnData in response.enemy_spawns)
                    {
                        SpawnEnemyAtPoint(spawnData);
                    }
                    
                    hasLoadedSpawns = true;
                }
                else
                {
                    Debug.LogWarning($"[NetworkEnemySpawner] No enemy spawns found for map {mapId}");
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
    private void SpawnEnemyAtPoint(EnemySpawnData spawnData)
    {
        if (enemyPrefabManager == null)
        {
            Debug.LogError("[NetworkEnemySpawner] EnemyPrefabManager is null!");
            return;
        }

        // Lấy enemy prefab theo enemy_type_id
        GameObject enemyPrefab = enemyPrefabManager.GetEnemyPrefab(spawnData.enemy_type_id);
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[NetworkEnemySpawner] Enemy prefab not found for enemy_type_id {spawnData.enemy_type_id} (spawn_id: {spawnData.spawn_id})");
            return;
        }

        // Spawn enemy tại vị trí
        Vector3 spawnPosition = new Vector3(spawnData.spawn_x, spawnData.spawn_y, 0f);
        
        for (int i = 0; i < spawnData.max_spawn_count; i++)
        {
            GameObject enemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            NetworkObject networkObj = enemyObj.GetComponent<NetworkObject>();
            
            if (networkObj != null)
            {
                networkObj.Spawn();

                // Áp dụng HP từ DB — ghi đè giá trị mặc định (maxHealth=10)
                if (spawnData.enemy != null && spawnData.enemy.base_hp > 0)
                {
                    var health = enemyObj.GetComponent<NetworkEnemyHealth>();
                    if (health != null)
                        health.InitHealth(spawnData.enemy.base_hp);
                }

                Debug.Log($"[NetworkEnemySpawner] Spawned enemy {spawnData.enemy?.enemy_name ?? "Unknown"} (HP={spawnData.enemy?.base_hp ?? 0}) at ({spawnData.spawn_x}, {spawnData.spawn_y})");
            }
            else
            {
                Debug.LogError($"[NetworkEnemySpawner] Enemy prefab missing NetworkObject component!");
                Destroy(enemyObj);
            }
        }

        // Lưu thông tin respawn
        lastRespawnTime[spawnData.spawn_id] = Time.time;
    }

    /// <summary>
    /// Respawn enemy sau khi bị kill
    /// </summary>
    public void RespawnEnemy(int spawnId, EnemySpawnData spawnData)
    {
        if (!IsServer) return;

        if (Time.time - lastRespawnTime[spawnId] >= spawnData.respawn_time)
        {
            SpawnEnemyAtPoint(spawnData);
            lastRespawnTime[spawnId] = Time.time;
        }
    }

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
    }
}
