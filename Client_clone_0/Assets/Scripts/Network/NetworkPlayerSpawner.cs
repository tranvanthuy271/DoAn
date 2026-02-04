using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerSpawner : MonoBehaviour
{
    [Header("Default Player Prefab (Fallback)")]
    [SerializeField] private GameObject networkPlayerPrefab;

    [Header("Element Prefabs (Based on element_type + gender)")]
    [SerializeField] private GameObject fireMalePrefab;
    [SerializeField] private GameObject fireFemalePrefab;
    [SerializeField] private GameObject waterMalePrefab;
    [SerializeField] private GameObject waterFemalePrefab;
    [SerializeField] private GameObject earthMalePrefab;
    [SerializeField] private GameObject earthFemalePrefab; // Không có nhưng để dành
    [SerializeField] private GameObject woodMalePrefab;
    [SerializeField] private GameObject woodFemalePrefab;
    [SerializeField] private GameObject metalMalePrefab;
    [SerializeField] private GameObject metalFemalePrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager networkManager;
    private readonly System.Collections.Generic.HashSet<ulong> spawnedClients = new System.Collections.Generic.HashSet<ulong>();
    private bool hasSubscribed = false;

    private void Awake()
    {
        Debug.Log("[NetworkPlayerSpawner] Awake called!");
    }

    private void Start()
    {
        Debug.Log("[NetworkPlayerSpawner] Start called!");
        
        networkManager = NetworkManager.Singleton;
        
        if (networkManager == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] NetworkManager not found! Make sure NetworkManager exists in scene.");
            return;
        }

        Debug.Log($"[NetworkPlayerSpawner] Start: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}, IsHost={networkManager.IsHost}");

        // Subscribe events ngay lập tức nếu đã là server
        if (networkManager.IsServer)
        {
            Debug.Log("[NetworkPlayerSpawner] Server already started, subscribing to events...");
            SubscribeToEvents();
        }
        else
        {
            Debug.Log("[NetworkPlayerSpawner] Not server yet, subscribing to OnServerStarted event...");
            networkManager.OnServerStarted += OnServerStarted;
        }
    }

    private void Update()
    {
        // Đảm bảo subscribe nếu server start sau khi Start() chạy
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        if (networkManager != null && networkManager.IsServer && !hasSubscribed)
        {
            Debug.Log("[NetworkPlayerSpawner] Update: Server detected, subscribing to events...");
            SubscribeToEvents();
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("[NetworkPlayerSpawner] OnServerStarted called! Subscribing to events...");
        if (!hasSubscribed)
        {
            SubscribeToEvents();
        }
    }

    private void OnEnable()
    {
        // Nếu NetworkManager đã start server trước khi script enable
        if (networkManager != null && networkManager.IsServer && !hasSubscribed)
        {
            Debug.Log("[NetworkPlayerSpawner] OnEnable: Server detected, subscribing to events");
            SubscribeToEvents();
        }
        else if (networkManager == null)
        {
            // Lấy NetworkManager nếu chưa có
            networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsServer && !hasSubscribed)
            {
                Debug.Log("[NetworkPlayerSpawner] OnEnable: Got NetworkManager, server detected, subscribing");
                SubscribeToEvents();
            }
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
        Debug.Log($"[NetworkPlayerSpawner] SpawnPlayer called for clientId: {clientId}");
        
        if (networkManager == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] NetworkManager is null!");
            return;
        }
        
        if (!networkManager.IsServer)
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] Not server! IsServer={networkManager.IsServer}, cannot spawn player");
            return;
        }

        // Tránh spawn trùng cho cùng một client
        if (spawnedClients.Contains(clientId))
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] Client {clientId} already has a spawned player. Skipping duplicate spawn.");
            return;
        }

        // Chọn spawn point (round-robin hoặc random)
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[NetworkPlayerSpawner] No spawn points assigned!");
            return;
        }

        if (networkPlayerPrefab == null)
        {
            Debug.LogError("[NetworkPlayerSpawner] NetworkPlayerPrefab is not assigned!");
            return;
        }

        // clientId là ulong, spawnPoints.Length là int -> ép kiểu rõ ràng để tránh CS0034
        int spawnIndex = (int)(clientId % (ulong)spawnPoints.Length);
        Vector3 spawnPos = spawnPoints[spawnIndex].position;

        // Chọn prefab dựa trên player data từ GameManager
        GameObject prefabToSpawn = GetPlayerPrefabForClient(clientId);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[NetworkPlayerSpawner] Could not find prefab for client {clientId}! Using default prefab.");
            prefabToSpawn = networkPlayerPrefab;
        }

        Debug.Log($"[NetworkPlayerSpawner] Instantiating player prefab '{prefabToSpawn.name}' for client {clientId} at spawn point {spawnIndex} ({spawnPos})");

        // Spawn player
        GameObject playerObj = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        NetworkObject networkObj = playerObj.GetComponent<NetworkObject>();
        
        if (networkObj != null)
        {
            Debug.Log($"[NetworkPlayerSpawner] NetworkObject found, spawning with ownership for client {clientId}");
            networkObj.SpawnWithOwnership(clientId); // Owner là client vừa connect
            spawnedClients.Add(clientId);
            Debug.Log($"[NetworkPlayerSpawner] ✓✓✓ Successfully spawned player for client {clientId} at {spawnPos} ✓✓✓");
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

        // Tìm tất cả NetworkObject trong scene
        NetworkObject[] networkObjects = FindObjectsOfType<NetworkObject>();
        
        foreach (NetworkObject netObj in networkObjects)
        {
            // Kiểm tra xem NetworkObject này có phải là player của client đang disconnect không
            if (netObj.OwnerClientId == clientId && netObj.IsSpawned)
            {
                Debug.Log($"Despawning player for client {clientId}");
                
                // Despawn và destroy player
                if (netObj.IsSpawned)
                {
                    netObj.Despawn(true); // true = destroy object sau khi despawn
                }
                else
                {
                    Destroy(netObj.gameObject);
                }
                
                // Chỉ xóa 1 player (player đầu tiên tìm được)
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            if (hasSubscribed)
            {
                networkManager.OnClientConnectedCallback -= SpawnPlayer;
                networkManager.OnClientDisconnectCallback -= DespawnPlayer;
                hasSubscribed = false;
            }
            // Unsubscribe OnServerStarted
            networkManager.OnServerStarted -= OnServerStarted;
        }
    }

    /// <summary>
    /// Chọn prefab dựa trên element_type + gender từ GameManager
    /// </summary>
    private GameObject GetPlayerPrefabForClient(ulong clientId)
    {
        // Lấy player data từ GameManager
        // Lưu ý: GameManager.Instance có thể chứa data của local player
        // Nếu có nhiều players, cần map clientId với user_id
        if (GameManager.Instance == null || !GameManager.Instance.HasPlayerData())
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] GameManager.Instance is null or no player data for client {clientId}! Using default prefab.");
            return networkPlayerPrefab;
        }

        PlayerDataResponse playerData = GameManager.Instance.GetPlayerData();
        if (playerData == null)
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] PlayerDataResponse is null for client {clientId}! Using default prefab.");
            return networkPlayerPrefab;
        }

        string elementType = playerData.element_type ?? "Fire";
        string gender = playerData.gender ?? "Male";

        Debug.Log($"[NetworkPlayerSpawner] Client {clientId} - Element: {elementType}, Gender: {gender}");

        // Chọn prefab dựa trên element_type + gender
        GameObject selectedPrefab = null;
        string prefabKey = $"{elementType}_{gender}";

        switch (prefabKey)
        {
            case "Fire_Male":
                selectedPrefab = fireMalePrefab;
                break;
            case "Fire_Female":
                selectedPrefab = fireFemalePrefab;
                break;
            case "Water_Male":
                selectedPrefab = waterMalePrefab;
                break;
            case "Water_Female":
                selectedPrefab = waterFemalePrefab;
                break;
            case "Earth_Male":
                selectedPrefab = earthMalePrefab;
                break;
            case "Earth_Female":
                selectedPrefab = earthFemalePrefab;
                break;
            case "Wood_Male":
                selectedPrefab = woodMalePrefab;
                break;
            case "Wood_Female":
                selectedPrefab = woodFemalePrefab;
                break;
            case "Metal_Male":
                selectedPrefab = metalMalePrefab;
                break;
            case "Metal_Female":
                selectedPrefab = metalFemalePrefab;
                break;
            default:
                Debug.LogWarning($"[NetworkPlayerSpawner] Unknown element/gender combination: {prefabKey}. Using default prefab.");
                selectedPrefab = networkPlayerPrefab;
                break;
        }

        if (selectedPrefab == null)
        {
            Debug.LogWarning($"[NetworkPlayerSpawner] Prefab for {prefabKey} is not assigned in Inspector! Using default prefab.");
            selectedPrefab = networkPlayerPrefab;
        }

        return selectedPrefab;
    }
}




