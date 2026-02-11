using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Shared script: Spawn player với đúng prefab dựa trên element_type + gender
/// Chỉ chạy trên server/host
/// </summary>
public class NetworkPlayerSpawner : MonoBehaviour
{
    // Singleton pattern để đảm bảo chỉ có 1 instance
    private static NetworkPlayerSpawner _instance;
    public static NetworkPlayerSpawner Instance => _instance;

    [Header("Default Player Prefab (Fallback)")]
    [SerializeField] private GameObject networkPlayerPrefab;

    [Header("Element Prefabs (Based on element_type + gender)")]
    [SerializeField] private GameObject fireMalePrefab;
    [SerializeField] private GameObject fireFemalePrefab;
    [SerializeField] private GameObject waterMalePrefab;
    [SerializeField] private GameObject waterFemalePrefab;
    [SerializeField] private GameObject earthMalePrefab;
    [SerializeField] private GameObject earthFemalePrefab;
    [SerializeField] private GameObject woodMalePrefab;
    [SerializeField] private GameObject woodFemalePrefab;
    [SerializeField] private GameObject metalMalePrefab;
    [SerializeField] private GameObject metalFemalePrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager networkManager;
    private readonly System.Collections.Generic.HashSet<ulong> spawnedClients = new System.Collections.Generic.HashSet<ulong>();
    private readonly System.Collections.Generic.HashSet<ulong> spawningClients = new System.Collections.Generic.HashSet<ulong>(); // Đang trong quá trình spawn
    private bool hasSubscribed = false;

    private void Awake()
    {
        Debug.Log("[NetworkPlayerSpawner] Awake called!");
        
        // Singleton: Nếu đã có instance khác, destroy instance này
        if (_instance != null && _instance != this)
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ Multiple instances detected! Destroying duplicate instance on '{gameObject.name}'");
            Destroy(this);
            return;
        }
        
        _instance = this;
        Debug.Log("[NetworkPlayerSpawner] ✓ Instance set as singleton");
        
        // Subscribe OnServerStarted trong Awake() để đảm bảo nhận được event ngay cả khi script bị disable
        networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            Debug.Log("[NetworkPlayerSpawner] Subscribing to OnServerStarted in Awake()...");
            networkManager.OnServerStarted += OnServerStarted;
        }
    }

    private void Start()
    {
        Debug.Log("[NetworkPlayerSpawner] Start called!");
        
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }
        
        if (networkManager == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] NetworkManager not found! Make sure NetworkManager exists in scene.");
            return;
        }

        Debug.Log($"[NetworkPlayerSpawner] Start: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}, IsHost={networkManager.IsHost}");

        // QUAN TRỌNG: NetworkPlayerSpawner CHỈ chạy trên SERVER
        // Nếu đang là client (không phải server), disable script này
        if (!networkManager.IsServer && !networkManager.IsHost)
        {
            Debug.Log("[NetworkPlayerSpawner] ⚠️ This is a CLIENT instance. NetworkPlayerSpawner only runs on SERVER. Disabling this component.");
            Debug.Log("[NetworkPlayerSpawner] ⚠️ NOTE: If you plan to Start Host, the script will be re-enabled when server starts.");
            Debug.Log("[NetworkPlayerSpawner] ⚠️ NOTE: OnServerStarted event is already subscribed in Awake(), will re-enable when server starts.");
            this.enabled = false;
            return;
        }

        // Subscribe events ngay lập tức nếu đã là server
        if (networkManager.IsServer || networkManager.IsHost)
        {
            Debug.Log("[NetworkPlayerSpawner] ✓ Server/Host detected, subscribing to events...");
            SubscribeToEvents();
        }
    }

    private void Update()
    {
        // Chỉ chạy nếu đã enable (tức là đang ở server)
        if (!this.enabled) return;

        // Đảm bảo subscribe nếu server start sau khi Start() chạy
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        // Double check: chỉ chạy trên server
        if (networkManager != null && (networkManager.IsServer || networkManager.IsHost) && !hasSubscribed)
        {
            Debug.Log("[NetworkPlayerSpawner] Update: Server detected, subscribing to events...");
            SubscribeToEvents();
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("[NetworkPlayerSpawner] ✓✓✓ OnServerStarted called! Subscribing to events...");
        
        // Re-enable script nếu đã bị disable
        if (!this.enabled)
        {
            Debug.Log("[NetworkPlayerSpawner] Re-enabling script after server started...");
            this.enabled = true;
        }
        
        if (!hasSubscribed)
        {
            SubscribeToEvents();
        }
        
        // QUAN TRỌNG: KHÔNG spawn player trong OnServerStarted() nữa
        // Vì OnClientConnectedCallback đã tự động spawn player khi client connect
        // Nếu spawn ở đây sẽ gây duplicate khi host start (host tự động start client)
        // Chỉ cần đảm bảo events đã được subscribe là đủ
        Debug.Log("[NetworkPlayerSpawner] OnServerStarted: Events subscribed. Players will be spawned via OnClientConnectedCallback.");
    }

    private void OnEnable()
    {
        // Lấy NetworkManager nếu chưa có
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }
        
        // Nếu NetworkManager đã start server trước khi script enable
        if (networkManager != null && (networkManager.IsServer || networkManager.IsHost) && !hasSubscribed)
        {
            Debug.Log("[NetworkPlayerSpawner] OnEnable: Server/Host detected, subscribing to events");
            SubscribeToEvents();
        }
        else if (networkManager != null && !networkManager.IsServer && !networkManager.IsHost)
        {
            // Nếu không phải server, subscribe to OnServerStarted để enable lại sau
            networkManager.OnServerStarted += OnServerStarted;
        }
    }

    private void SubscribeToEvents()
    {
        if (hasSubscribed)
        {
            Debug.Log("[NetworkPlayerSpawner] Already subscribed to events");
            return;
        }
        
        Debug.Log("[NetworkPlayerSpawner] Subscribing to OnClientConnectedCallback and OnClientDisconnectCallback");
        networkManager.OnClientConnectedCallback += SpawnPlayer;
        networkManager.OnClientDisconnectCallback += DespawnPlayer;
        hasSubscribed = true;
        Debug.Log("[NetworkPlayerSpawner] Successfully subscribed to events");
    }

    private void OnDisable()
    {
        if (networkManager != null && hasSubscribed)
        {
            networkManager.OnClientConnectedCallback -= SpawnPlayer;
            networkManager.OnClientDisconnectCallback -= DespawnPlayer;
            hasSubscribed = false;
        }
    }

    private void SpawnPlayer(ulong clientId)
    {
        // Double check: chỉ chạy trên server
        if (!this.enabled || networkManager == null || (!networkManager.IsServer && !networkManager.IsHost))
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ SpawnPlayer called but not on server! IsServer={networkManager?.IsServer}, IsHost={networkManager?.IsHost}");
            return;
        }

        Debug.Log($"[NetworkPlayerSpawner] ✓✓✓ SpawnPlayer called for clientId: {clientId} ✓✓✓");

        // Tránh spawn trùng cho cùng một client
        if (spawnedClients.Contains(clientId))
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ Client {clientId} already has a spawned player. Skipping duplicate spawn.");
            return;
        }

        // Tránh spawn đang trong quá trình spawn
        if (spawningClients.Contains(clientId))
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ Client {clientId} is already being spawned. Skipping duplicate spawn request.");
            return;
        }

        // Đánh dấu đang spawn
        spawningClients.Add(clientId);

        // Đợi player data được load (từ ClientAuthSenderComponent)
        StartCoroutine(SpawnPlayerWhenDataReady(clientId));
    }

    /// <summary>
    /// Coroutine: Đợi player data được load, sau đó spawn player
    /// </summary>
    private System.Collections.IEnumerator SpawnPlayerWhenDataReady(ulong clientId)
    {
        // Đợi auth được gửi và ServerAPI response (120 attempts = 12 giây)
        // Tăng thời gian chờ để đảm bảo auth message và API response có đủ thời gian
        int maxRetries = 120;
        float retryInterval = 0.1f; // 0.1 giây mỗi lần = 12 giây tổng cộng

        for (int i = 0; i < maxRetries; i++)
        {
            // Kiểm tra player data đã được load chưa
            PlayerDataResponse playerData = null;
            if (ServerPlayerDataManager.Instance != null)
            {
                playerData = ServerPlayerDataManager.Instance.GetPlayerDataForClient(clientId);
            }

            if (playerData != null)
            {
                Debug.Log($"[NetworkPlayerSpawner] ✓ Player data ready for client {clientId} after {i + 1} attempts, spawning player...");
                SpawnPlayerNow(clientId, playerData);
                spawningClients.Remove(clientId); // Remove khỏi danh sách đang spawn
                yield break;
            }

            Debug.Log($"[NetworkPlayerSpawner] Waiting for player data for client {clientId}... (attempt {i + 1}/{maxRetries})");
            yield return new WaitForSeconds(retryInterval);
        }

        // Nếu sau 6 giây vẫn chưa có data, spawn với default prefab
        Debug.LogWarning($"[NetworkPlayerSpawner] Player data not loaded after {maxRetries} attempts ({maxRetries * retryInterval} seconds). Spawning with default prefab.");
        SpawnPlayerNow(clientId, null);
        spawningClients.Remove(clientId); // Remove khỏi danh sách đang spawn
    }

    /// <summary>
    /// Spawn player ngay lập tức (đã có data hoặc dùng default)
    /// </summary>
    private void SpawnPlayerNow(ulong clientId, PlayerDataResponse playerData)
    {
        if (networkPlayerPrefab == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] NetworkPlayerPrefab is not assigned!");
            return;
        }

        Vector3 spawnPos;
        int spawnIndex = -1;
        
        // Kiểm tra position_x, position_y từ player data
        if (playerData != null && (playerData.position_x != 0 || playerData.position_y != 0))
        {
            spawnPos = new Vector3(playerData.position_x, playerData.position_y, 0f);
            Debug.Log($"[NetworkPlayerSpawner] Spawning at saved position: ({playerData.position_x}, {playerData.position_y})");
        }
        else
        {
            // Chọn spawn point
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[NetworkPlayerSpawner] No spawn points assigned and no saved position!");
                return;
            }

            spawnIndex = (int)(clientId % (ulong)spawnPoints.Length);
            spawnPos = spawnPoints[spawnIndex].position;
            Debug.Log($"[NetworkPlayerSpawner] Spawning at spawn point {spawnIndex}: {spawnPos}");
        }

        // Chọn prefab dựa trên player data (element_type + gender)
        GameObject prefabToSpawn = GetPlayerPrefabForClient(clientId);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[NetworkPlayerSpawner] Could not find prefab for client {clientId}! Using default prefab.");
            prefabToSpawn = networkPlayerPrefab;
        }

        if (spawnIndex >= 0)
        {
            Debug.Log($"[NetworkPlayerSpawner] Instantiating player prefab '{prefabToSpawn.name}' for client {clientId} at spawn point {spawnIndex} ({spawnPos})");
        }
        else
        {
            Debug.Log($"[NetworkPlayerSpawner] Instantiating player prefab '{prefabToSpawn.name}' for client {clientId} at saved position ({spawnPos})");
        }

        // Spawn player
        GameObject playerObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        NetworkObject networkObj = playerObj.GetComponent<NetworkObject>();
        
        if (networkObj != null)
        {
            Debug.Log($"[NetworkPlayerSpawner] NetworkObject found, spawning with ownership for client {clientId}");
            
            // Spawn với ownership của client này
            networkObj.SpawnWithOwnership(clientId);
            spawnedClients.Add(clientId);
            
            Debug.Log($"[NetworkPlayerSpawner] ✓✓✓ Successfully spawned player for client {clientId} at {spawnPos} ✓✓✓");
            Debug.Log($"[NetworkPlayerSpawner] Player '{prefabToSpawn.name}' is now visible to ALL connected clients");
        }
        else
        {
            Debug.LogError("[NetworkPlayerSpawner] NetworkPlayer prefab missing NetworkObject component!");
            Destroy(playerObj);
        }
    }

    private void DespawnPlayer(ulong clientId)
    {
        if (networkManager == null || !networkManager.IsServer) return;

        Debug.Log($"[NetworkPlayerSpawner] Despawning player for client {clientId}");

        // Remove player data từ ServerPlayerDataManager
        if (ServerPlayerDataManager.Instance != null)
        {
            ServerPlayerDataManager.Instance.RemovePlayerData(clientId);
        }

        // Tìm tất cả NetworkObject trong scene
        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        
        foreach (NetworkObject netObj in networkObjects)
        {
            if (netObj.OwnerClientId == clientId && netObj.IsSpawned)
            {
                Debug.Log($"[NetworkPlayerSpawner] Found player NetworkObject for client {clientId}, despawning...");
                
                if (netObj.IsSpawned)
                {
                    netObj.Despawn(true);
                }
                else
                {
                    Destroy(netObj.gameObject);
                }
                
                break;
            }
        }

        // Remove khỏi spawnedClients set và spawningClients set
        spawnedClients.Remove(clientId);
        spawningClients.Remove(clientId);
        Debug.Log($"[NetworkPlayerSpawner] ✓ Removed client {clientId} from spawned clients list");
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
        
        if (networkManager != null)
        {
            if (hasSubscribed)
            {
                networkManager.OnClientConnectedCallback -= SpawnPlayer;
                networkManager.OnClientDisconnectCallback -= DespawnPlayer;
                hasSubscribed = false;
            }
            networkManager.OnServerStarted -= OnServerStarted;
        }
    }

    /// <summary>
    /// Chọn prefab dựa trên element_type + gender từ ServerPlayerDataManager
    /// </summary>
    private GameObject GetPlayerPrefabForClient(ulong clientId)
    {
        // Lấy player data từ ServerPlayerDataManager (server-side)
        PlayerDataResponse playerData = null;

        if (ServerPlayerDataManager.Instance != null)
        {
            playerData = ServerPlayerDataManager.Instance.GetPlayerDataForClient(clientId);
        }

        // Fallback: Nếu không có ServerPlayerDataManager, dùng GameManager (cho local player)
        if (playerData == null && GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerData = GameManager.Instance.GetPlayerData();
            Debug.LogWarning($"[NetworkPlayerSpawner] Using GameManager fallback for client {clientId}");
        }

        if (playerData == null)
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] No player data found for client {clientId}! Using default prefab.");
            return networkPlayerPrefab;
        }

        string elementType = playerData.element_type ?? "Fire";
        string gender = playerData.gender ?? "Male";

        Debug.Log($"[NetworkPlayerSpawner] Client {clientId} - Element: {elementType}, Gender: {gender}, Character: {playerData.character_name ?? "Unknown"}");

        // Chọn prefab dựa trên element_type + gender
        GameObject selectedPrefab = null;
        string prefabKey = $"{elementType}_{gender}";
        
        Debug.Log($"[NetworkPlayerSpawner] Looking for prefab with key: '{prefabKey}'");

        switch (prefabKey)
        {
            case "Fire_Male":
                selectedPrefab = fireMalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Fire_Male prefab: {(fireMalePrefab != null ? fireMalePrefab.name : "NULL")}");
                break;
            case "Fire_Female":
                selectedPrefab = fireFemalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Fire_Female prefab: {(fireFemalePrefab != null ? fireFemalePrefab.name : "NULL")}");
                break;
            case "Water_Male":
                selectedPrefab = waterMalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Water_Male prefab: {(waterMalePrefab != null ? waterMalePrefab.name : "NULL")}");
                break;
            case "Water_Female":
                selectedPrefab = waterFemalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Water_Female prefab: {(waterFemalePrefab != null ? waterFemalePrefab.name : "NULL")}");
                break;
            case "Earth_Male":
                selectedPrefab = earthMalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Earth_Male prefab: {(earthMalePrefab != null ? earthMalePrefab.name : "NULL")}");
                break;
            case "Earth_Female":
                selectedPrefab = earthFemalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Earth_Female prefab: {(earthFemalePrefab != null ? earthFemalePrefab.name : "NULL")}");
                break;
            case "Wood_Male":
                selectedPrefab = woodMalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Wood_Male prefab: {(woodMalePrefab != null ? woodMalePrefab.name : "NULL")}");
                break;
            case "Wood_Female":
                selectedPrefab = woodFemalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Wood_Female prefab: {(woodFemalePrefab != null ? woodFemalePrefab.name : "NULL")}");
                break;
            case "Metal_Male":
                selectedPrefab = metalMalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Metal_Male prefab: {(metalMalePrefab != null ? metalMalePrefab.name : "NULL")}");
                break;
            case "Metal_Female":
                selectedPrefab = metalFemalePrefab;
                Debug.Log($"[NetworkPlayerSpawner] Selected Metal_Female prefab: {(metalFemalePrefab != null ? metalFemalePrefab.name : "NULL")}");
                break;
            default:
                Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ Unknown element/gender combination: '{prefabKey}'. Using default prefab.");
                selectedPrefab = networkPlayerPrefab;
                break;
        }

        if (selectedPrefab == null)
        {
            Debug.LogError($"[NetworkPlayerSpawner] ❌ Prefab for '{prefabKey}' is NULL in Inspector! Using default prefab '{networkPlayerPrefab?.name ?? "NULL"}'.");
            Debug.LogError($"[NetworkPlayerSpawner] ❌ Please check NetworkPlayerSpawner in Inspector and assign the '{prefabKey}' prefab!");
            selectedPrefab = networkPlayerPrefab;
        }
        else
        {
            Debug.Log($"[NetworkPlayerSpawner] ✓ Successfully selected prefab: '{selectedPrefab.name}' for key '{prefabKey}'");
        }

        return selectedPrefab;
    }

    /// <summary>
    /// Lấy tất cả player prefab (để đăng ký vào NetworkManager)
    /// </summary>
    public System.Collections.Generic.List<GameObject> GetAllPlayerPrefabs()
    {
        var prefabs = new System.Collections.Generic.List<GameObject>();
        
        if (networkPlayerPrefab != null)
            prefabs.Add(networkPlayerPrefab);
        if (fireMalePrefab != null)
            prefabs.Add(fireMalePrefab);
        if (fireFemalePrefab != null)
            prefabs.Add(fireFemalePrefab);
        if (waterMalePrefab != null)
            prefabs.Add(waterMalePrefab);
        if (waterFemalePrefab != null)
            prefabs.Add(waterFemalePrefab);
        if (earthMalePrefab != null)
            prefabs.Add(earthMalePrefab);
        if (earthFemalePrefab != null)
            prefabs.Add(earthFemalePrefab);
        if (woodMalePrefab != null)
            prefabs.Add(woodMalePrefab);
        if (woodFemalePrefab != null)
            prefabs.Add(woodFemalePrefab);
        if (metalMalePrefab != null)
            prefabs.Add(metalMalePrefab);
        if (metalFemalePrefab != null)
            prefabs.Add(metalFemalePrefab);
        
        return prefabs;
    }
}
