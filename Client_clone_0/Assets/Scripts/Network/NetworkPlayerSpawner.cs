using UnityEngine;
using Unity.Netcode;

public class NetworkPlayerSpawner : MonoBehaviour
{
    [Header("Player Prefab")]
    [SerializeField] private GameObject networkPlayerPrefab;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager networkManager;
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
            // Spawn player cho host (server) luôn
            Debug.Log($"[NetworkPlayerSpawner] Spawning player for host (LocalClientId: {networkManager.LocalClientId})");
            SpawnPlayer(networkManager.LocalClientId);
        }
        else
        {
            Debug.Log("[NetworkPlayerSpawner] Not server yet, subscribing to OnServerStarted event...");
            // Subscribe để được notify khi server start
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
            // Spawn player cho host nếu chưa spawn
            if (networkManager.LocalClientId != 0) // Host có LocalClientId = 0 khi start
            {
                Debug.Log($"[NetworkPlayerSpawner] Update: Spawning player for host (LocalClientId: {networkManager.LocalClientId})");
                SpawnPlayer(networkManager.LocalClientId);
            }
        }
    }

    private void OnServerStarted()
    {
        Debug.Log("[NetworkPlayerSpawner] OnServerStarted called! Subscribing to events...");
        if (!hasSubscribed)
        {
            SubscribeToEvents();
            // Spawn player cho host
            if (networkManager != null)
            {
                Debug.Log($"[NetworkPlayerSpawner] Spawning player for host (LocalClientId: {networkManager.LocalClientId})");
                SpawnPlayer(networkManager.LocalClientId);
            }
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

        Debug.Log($"[NetworkPlayerSpawner] Instantiating player prefab for client {clientId} at spawn point {spawnIndex} ({spawnPos})");

        // Spawn player
        GameObject playerObj = Instantiate(networkPlayerPrefab, spawnPos, Quaternion.identity);
        NetworkObject networkObj = playerObj.GetComponent<NetworkObject>();
        
        if (networkObj != null)
        {
            Debug.Log($"[NetworkPlayerSpawner] NetworkObject found, spawning with ownership for client {clientId}");
            networkObj.SpawnWithOwnership(clientId); // Owner là client vừa connect
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
}




