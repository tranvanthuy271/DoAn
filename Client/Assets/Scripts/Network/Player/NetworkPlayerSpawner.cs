using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Element Prefabs (index theo hệ: 0=Kim, 1=Mộc, 2=Thủy, 3=Hỏa, 4=Thổ, 5=Phong)")]
    [SerializeField] private GameObject metalPrefab;   // 0 Kim
    [SerializeField] private GameObject woodPrefab;    // 1 Mộc
    [SerializeField] private GameObject waterPrefab;   // 2 Thủy
    [SerializeField] private GameObject firePrefab;    // 3 Hỏa
    [SerializeField] private GameObject earthPrefab;   // 4 Thổ
    [SerializeField] private GameObject windPrefab;    // 5 Phong

    [Header("Hybrid Prefabs — Hỏa+Thổ (hybrid_id=1)")]
    [SerializeField] private GameObject hybridEarthFirePrefab_FirePrimary;   // Hệ chính = Hỏa (Fire)
    [SerializeField] private GameObject hybridEarthFirePrefab_EarthPrimary;  // Hệ chính = Thổ (Earth)

    [Header("Hybrid Prefabs — Thủy+Mộc (hybrid_id=10)")]
    [SerializeField] private GameObject hybridWaterWoodPrefab_WaterPrimary;  // Hệ chính = Thủy (Water)
    [SerializeField] private GameObject hybridWaterWoodPrefab_WoodPrimary;   // Hệ chính = Mộc (Wood)

    [Header("Hybrid Prefabs — Kim+Phong (hybrid_id=13)")]
    [SerializeField] private GameObject hybridMetalWindPrefab_MetalPrimary;  // Hệ chính = Kim (Metal)
    [SerializeField] private GameObject hybridMetalWindPrefab_WindPrimary;   // Hệ chính = Phong (Wind)

    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;

    private NetworkManager networkManager;
    private readonly System.Collections.Generic.HashSet<ulong> spawnedClients = new System.Collections.Generic.HashSet<ulong>();
    private readonly System.Collections.Generic.HashSet<ulong> spawningClients = new System.Collections.Generic.HashSet<ulong>(); // Đang trong quá trình spawn
    private bool hasSubscribed = false;

    private void Awake()
    {
        if (FindObjectOfType<MapWorldBootstrap>() != null)
        {
            Debug.Log("[NetworkPlayerSpawner] MapWorldBootstrap detected — disabling legacy player spawner.");
            enabled = false;
            return;
        }

        // Debug.Log("[NetworkPlayerSpawner] Awake called!");
        
        // Singleton: Nếu đã có instance khác, destroy instance này
        if (_instance != null && _instance != this)
        {
            // Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ Multiple instances detected! Destroying duplicate instance on '{gameObject.name}'");
            Destroy(this);
            return;
        }
        
        _instance = this;
        if (transform.parent != null)
            transform.SetParent(null, true);
        // Persist qua scene load — prefab references được gán trong GameScene Inspector
        DontDestroyOnLoad(gameObject);
        
        // Subscribe OnServerStarted trong Awake() để đảm bảo nhận được event ngay cả khi script bị disable
        networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.OnServerStarted -= OnServerStarted; // tránh double-sub
            networkManager.OnServerStarted += OnServerStarted;
        }

        // DDOL persist: khi scene mới load, NetworkManager.Singleton là object MỚI.
        // Cần re-grab và re-subscribe OnServerStarted để SpawnPlayer hoạt động sau travel.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        // Debug.Log("[NetworkPlayerSpawner] Start called!");
        
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }
        
        if (networkManager == null)
        {
            // Debug.LogError("[NetworkPlayerSpawner] NetworkManager not found! Make sure NetworkManager exists in scene.");
            return;
        }

        // Debug.Log($"[NetworkPlayerSpawner] Start: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}, IsHost={networkManager.IsHost}");

        // QUAN TRỌNG: NetworkPlayerSpawner CHỈ chạy trên SERVER
        // Nếu đang là client (không phải server), disable script này
        if (!networkManager.IsServer && !networkManager.IsHost)
        {
            // Debug.Log("[NetworkPlayerSpawner] ⚠️ This is a CLIENT instance. NetworkPlayerSpawner only runs on SERVER. Disabling this component.");
            // Debug.Log("[NetworkPlayerSpawner] ⚠️ NOTE: If you plan to Start Host, the script will be re-enabled when server starts.");
            // Debug.Log("[NetworkPlayerSpawner] ⚠️ NOTE: OnServerStarted event is already subscribed in Awake(), will re-enable when server starts.");
            this.enabled = false;
            return;
        }

        // Subscribe events ngay lập tức nếu đã là server
        if (networkManager.IsServer || networkManager.IsHost)
        {
            // Debug.Log("[NetworkPlayerSpawner] ✓ Server/Host detected, subscribing to events...");
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
            // Debug.Log("[NetworkPlayerSpawner] Update: Server detected, subscribing to events...");
            SubscribeToEvents();
        }
    }

    /// <summary>
    /// Được gọi bởi SceneManager.sceneLoaded sau mỗi lần load scene.
    /// Khi object này là DDOL, NetworkManager.Singleton sau scene change là object MỚI.
    /// Cần re-grab và re-subscribe OnServerStarted để SpawnPlayer hoạt động.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        var newNm = NetworkManager.Singleton;
        if (newNm == null || newNm == networkManager) return;

        // Unsubscribe khỏi NM cũ (có thể đã bị destroy)
        if (networkManager != null)
        {
            networkManager.OnServerStarted -= OnServerStarted;
        }

        networkManager = newNm;
        networkManager.OnServerStarted -= OnServerStarted;
        networkManager.OnServerStarted += OnServerStarted;
        hasSubscribed = false; // reset để SubscribeToEvents() chạy lại khi OnServerStarted

        Debug.Log($"[NetworkPlayerSpawner] Scene '{scene.name}' loaded — re-subscribed OnServerStarted to new NetworkManager");
    }

    private void OnServerStarted()
    {
        if (this == null) return;

        Debug.Log("[NetworkPlayerSpawner] OnServerStarted — reset state & re-subscribe");

        // Reset lại để dùng cho session mới (quan trọng khi persist qua scene)
        spawnedClients.Clear();
        spawningClients.Clear();
        hasSubscribed = false;

        // Re-enable script nếu đã bị disable
        this.enabled = true;
        
        SubscribeToEvents();
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
            // Debug.Log("[NetworkPlayerSpawner] OnEnable: Server/Host detected, subscribing to events");
            SubscribeToEvents();
        }
        else if (networkManager != null && !networkManager.IsServer && !networkManager.IsHost)
        {
            // Nếu không phải server, subscribe to OnServerStarted để enable lại sau
            networkManager.OnServerStarted -= OnServerStarted; // tránh double-sub
            networkManager.OnServerStarted += OnServerStarted;
        }
    }

    private void SubscribeToEvents()
    {
        if (hasSubscribed)
        {
            // Debug.Log("[NetworkPlayerSpawner] Already subscribed to events");
            return;
        }
        
        // Debug.Log("[NetworkPlayerSpawner] Subscribing to OnClientConnectedCallback and OnClientDisconnectCallback");
        networkManager.OnClientConnectedCallback += SpawnPlayer;
        networkManager.OnClientDisconnectCallback += DespawnPlayer;
        hasSubscribed = true;
        // Debug.Log("[NetworkPlayerSpawner] Successfully subscribed to events");
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
            // Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ SpawnPlayer called but not on server! IsServer={networkManager?.IsServer}, IsHost={networkManager?.IsHost}");
            return;
        }

        // Debug.Log($"[NetworkPlayerSpawner] ✓✓✓ SpawnPlayer called for clientId: {clientId} ✓✓✓");

        // Tránh spawn trùng cho cùng một client
        if (spawnedClients.Contains(clientId))
        {
            // Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ Client {clientId} already has a spawned player. Skipping duplicate spawn.");
            return;
        }

        // Tránh spawn đang trong quá trình spawn
        if (spawningClients.Contains(clientId))
        {
            // Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ Client {clientId} is already being spawned. Skipping duplicate spawn request.");
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

        Debug.Log($"[NetworkPlayerSpawner] ===== WAITING FOR PLAYER DATA =====");
        Debug.Log($"[NetworkPlayerSpawner] ClientId: {clientId}");
        Debug.Log($"[NetworkPlayerSpawner] Max retries: {maxRetries}, Interval: {retryInterval}s");

        for (int i = 0; i < maxRetries; i++)
        {
            // Kiểm tra player data đã được load chưa
            PlayerDataResponse playerData = null;
            if (ServerPlayerDataManager.Instance != null)
            {
                playerData = ServerPlayerDataManager.Instance.GetPlayerDataForClient(clientId);
                
                if (i % 10 == 0) // Log mỗi 10 lần để không spam
                {
                    Debug.Log($"[NetworkPlayerSpawner] Attempt {i + 1}/{maxRetries} - Checking ServerPlayerDataManager for clientId {clientId}...");
                    Debug.Log($"[NetworkPlayerSpawner] ServerPlayerDataManager.Instance exists: {ServerPlayerDataManager.Instance != null}");
                    Debug.Log($"[NetworkPlayerSpawner] PlayerData found: {playerData != null}");
                    if (playerData != null)
                    {
                        Debug.Log($"[NetworkPlayerSpawner] ✓ PlayerData preview - Character: {playerData.character_name}, Element: {playerData.element_type}");
                    }
                }
            }
            else
            {
                if (i % 20 == 0)
                {
                    Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ ServerPlayerDataManager.Instance is NULL at attempt {i + 1}");
                }
            }
            
            if (playerData != null)
            {
                Debug.Log($"[NetworkPlayerSpawner] ===== PLAYER DATA READY =====");
                Debug.Log($"[NetworkPlayerSpawner] ✓ Player data ready for client {clientId} after {i + 1} attempts ({(i + 1) * retryInterval}s)");
                Debug.Log($"[NetworkPlayerSpawner] ✓ Character: {playerData.character_name}");
                Debug.Log($"[NetworkPlayerSpawner] ✓ Element: {playerData.element_type}");
                Debug.Log($"[NetworkPlayerSpawner] ✓ Gender: {playerData.gender}");
                Debug.Log($"[NetworkPlayerSpawner] ✓ Spawning player now...");
                SpawnPlayerNow(clientId, playerData);
                spawningClients.Remove(clientId); // Remove khỏi danh sách đang spawn
                yield break;
            }

            yield return new WaitForSeconds(retryInterval);
        }

        // Nếu sau 12 giây vẫn chưa có data, spawn với default prefab
        Debug.LogError($"[NetworkPlayerSpawner] ===== PLAYER DATA TIMEOUT =====");
        Debug.LogError($"[NetworkPlayerSpawner] ✗ Player data NOT loaded after {maxRetries} attempts ({maxRetries * retryInterval} seconds) for clientId {clientId}");
        Debug.LogError($"[NetworkPlayerSpawner] ✗ Possible issues:");
        Debug.LogError($"[NetworkPlayerSpawner]   1. Client did not send auth (check ClientAuthSender logs)");
        Debug.LogError($"[NetworkPlayerSpawner]   2. Server failed to load player data from DB (check ServerPlayerDataManager logs)");
        Debug.LogError($"[NetworkPlayerSpawner]   3. Player data was loaded but not cached correctly");
        Debug.LogError($"[NetworkPlayerSpawner] ✗ Spawning with DEFAULT prefab as fallback");
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
            // Debug.LogError("[NetworkPlayerSpawner] NetworkPlayerPrefab is not assigned!");
            return;
        }

        Vector3 spawnPos = Vector3.zero;
        int spawnIndex = -1;

        // Ưu tiên 1: PortalArrivalHandler (khi vừa chuyển map qua trigger)
        if (PortalArrivalHandler.PendingMapId >= 0)
        {
            spawnPos = new Vector3(PortalArrivalHandler.PendingDestX, PortalArrivalHandler.PendingDestY, 0f);
            Debug.Log($"[NetworkPlayerSpawner] Spawn tại cổng đến: {spawnPos}");
        }
        // Ưu tiên 2: vị trí đã lưu trong DB (playerData)
        else if (playerData != null && (playerData.position_x != 0 || playerData.position_y != 0))
        {
            spawnPos = new Vector3(playerData.position_x, playerData.position_y, 0f);
        }
        // Fallback: spawn point trong scene
        else
        {
            bool foundSpawnPoint = false;
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                spawnIndex = (int)(clientId % (ulong)spawnPoints.Length);
                if (spawnPoints[spawnIndex] != null)
                {
                    spawnPos = spawnPoints[spawnIndex].position;
                    foundSpawnPoint = true;
                }
                else
                {
                    Debug.LogWarning($"[NetworkPlayerSpawner] spawnPoints[{spawnIndex}] is null — check Inspector assignments!");
                }
            }
            if (!foundSpawnPoint)
            {
                // Thử tìm qua tag "Respawn" trong scene
                var taggedSpawn = GameObject.FindWithTag("Respawn");
                if (taggedSpawn != null)
                {
                    spawnPos = taggedSpawn.transform.position;
                    Debug.LogWarning("[NetworkPlayerSpawner] Dùng GameObject có tag 'Respawn' làm spawn point.");
                }
                else
                {
                    spawnPos = Vector3.zero;
                    Debug.LogWarning("[NetworkPlayerSpawner] Không có spawn point hợp lệ — spawn tại (0,0,0). Hãy gán spawnPoints trong Inspector!");
                }
            }
        }

        // --- Chọn prefab TRỰC TIẾP từ playerData được truyền vào ---
        // (tránh query lại ServerPlayerDataManager — có thể bị race condition)
        GameObject prefabToSpawn = GetPrefabForPlayerData(playerData) ?? GetPlayerPrefabForClient(clientId);
        if (prefabToSpawn == null)
        {
            Debug.LogError($"[NetworkPlayerSpawner] Không tìm được prefab cho client {clientId}! Dùng default.");
            prefabToSpawn = networkPlayerPrefab;
        }
        Debug.Log($"[NetworkPlayerSpawner] Spawn: element={playerData?.element_type} → prefab='{prefabToSpawn.name}' pos={spawnPos}");

        if (spawnIndex >= 0)
        {
            // Debug.Log($"[NetworkPlayerSpawner] Instantiating player prefab '{prefabToSpawn.name}' for client {clientId} at spawn point {spawnIndex} ({spawnPos})");
        }
        else
        {
            // Debug.Log($"[NetworkPlayerSpawner] Instantiating player prefab '{prefabToSpawn.name}' for client {clientId} at saved position ({spawnPos})");
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
            
            // Debug.Log($"[NetworkPlayerSpawner] ✓✓✓ Successfully spawned player for client {clientId} at {spawnPos} ✓✓✓");
            // Debug.Log($"[NetworkPlayerSpawner] Player '{prefabToSpawn.name}' is now visible to ALL connected clients");
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

        // Debug.Log($"[NetworkPlayerSpawner] Despawning player for client {clientId}");

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
                // Debug.Log($"[NetworkPlayerSpawner] Found player NetworkObject for client {clientId}, despawning...");
                
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
        // Debug.Log($"[NetworkPlayerSpawner] ✓ Removed client {clientId} from spawned clients list");
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        
        // Dùng Singleton làm fallback phòng trường hợp networkManager field bị null
        var nm = networkManager ?? NetworkManager.Singleton;
        if (nm != null)
        {
            nm.OnServerStarted -= OnServerStarted;
            if (hasSubscribed)
            {
                nm.OnClientConnectedCallback -= SpawnPlayer;
                nm.OnClientDisconnectCallback -= DespawnPlayer;
                hasSubscribed = false;
            }
        }
    }

    /// <summary>
    /// Chuyển element_type → prefab trực tiếp từ playerData (không query lại DB/cache).
    /// Dùng khi đã có playerData sẵn để tránh race condition.
    /// </summary>
    private GameObject GetPrefabForPlayerData(PlayerDataResponse playerData)
    {
        if (playerData == null) return null;

        // Hybrid trước
        if (playerData.is_hybrid && playerData.hybrid_id > 0)
        {
            string primary = playerData.element_type ?? "";
            GameObject hybridPrefab = playerData.hybrid_id switch
            {
                1  => primary == "Fire"  ? hybridEarthFirePrefab_FirePrimary  : hybridEarthFirePrefab_EarthPrimary,
                10 => primary == "Water" ? hybridWaterWoodPrefab_WaterPrimary : hybridWaterWoodPrefab_WoodPrimary,
                13 => primary == "Metal" ? hybridMetalWindPrefab_MetalPrimary : hybridMetalWindPrefab_WindPrimary,
                _  => null
            };
            if (hybridPrefab != null) return hybridPrefab;
        }

        return (playerData.element_type ?? "") switch
        {
            "Metal" => metalPrefab,
            "Wood"  => woodPrefab,
            "Water" => waterPrefab,
            "Fire"  => firePrefab,
            "Earth" => earthPrefab,
            "Wind"  => windPrefab,
            _       => null
        };
    }

    /// <summary>
    /// Chọn prefab dựa trên element_type + gender từ ServerPlayerDataManager
    /// </summary>
    private GameObject GetPlayerPrefabForClient(ulong clientId)
    {
        // Lấy player data từ ServerPlayerDataManager (server-side)
        PlayerDataResponse playerData = null;

        Debug.Log($"[NetworkPlayerSpawner] ===== GET PLAYER PREFAB FOR CLIENT =====");
        Debug.Log($"[NetworkPlayerSpawner] ClientId: {clientId}");

        if (ServerPlayerDataManager.Instance != null)
        {
            Debug.Log($"[NetworkPlayerSpawner] Calling ServerPlayerDataManager.GetPlayerDataForClient({clientId})...");
            playerData = ServerPlayerDataManager.Instance.GetPlayerDataForClient(clientId);
            
            if (playerData != null)
            {
                Debug.Log($"[NetworkPlayerSpawner] ✓ Got PlayerData from ServerPlayerDataManager");
                Debug.Log($"[NetworkPlayerSpawner] ✓ Character: {playerData.character_name}");
                Debug.Log($"[NetworkPlayerSpawner] ✓ Element: {playerData.element_type}");
                Debug.Log($"[NetworkPlayerSpawner] ✓ Gender: {playerData.gender}");
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ ServerPlayerDataManager returned NULL for clientId {clientId}");
            }
        }
        else
        {
            Debug.LogError($"[NetworkPlayerSpawner] ✗ ServerPlayerDataManager.Instance is NULL!");
        }

        // Fallback: Nếu không có ServerPlayerDataManager, dùng GameManager (cho local player)
        if (playerData == null && GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerData = GameManager.Instance.GetPlayerData();
            // Debug.LogWarning($"[NetworkPlayerSpawner] Using GameManager fallback for client {clientId}");
        }

        if (playerData == null)
        {
            // Debug.LogWarning($"[NetworkPlayerSpawner] No player data found for client {clientId}! Using default prefab.");
            return networkPlayerPrefab;
        }

        // Kiểm tra hybrid trước — nếu đã fusion thì dùng hybrid prefab theo chiều (hệ chính)
        if (playerData.is_hybrid && playerData.hybrid_id > 0)
        {
            string primary = playerData.element_type ?? "";

            GameObject hybridPrefab = playerData.hybrid_id switch
            {
                // Hỏa+Thổ: element_type lưu hệ chính khi fusion
                1  => primary == "Fire"  ? hybridEarthFirePrefab_FirePrimary  : hybridEarthFirePrefab_EarthPrimary,
                // Thủy+Mộc
                10 => primary == "Water" ? hybridWaterWoodPrefab_WaterPrimary : hybridWaterWoodPrefab_WoodPrimary,
                // Kim+Phong
                13 => primary == "Metal" ? hybridMetalWindPrefab_MetalPrimary : hybridMetalWindPrefab_WindPrimary,
                _  => null
            };

            if (hybridPrefab != null)
            {
                Debug.Log($"[NetworkPlayerSpawner] ✓ Hybrid player: hybrid_id={playerData.hybrid_id}, primary={primary} → prefab '{hybridPrefab.name}'");
                return hybridPrefab;
            }

            Debug.LogWarning($"[NetworkPlayerSpawner] ⚠️ hybrid_id={playerData.hybrid_id} primary={primary} chưa có prefab được gán. Fallback sang prefab hệ đơn.");
        }

        string elementType = playerData.element_type ?? "Fire";

        // Chọn prefab theo element_type (giới tính đã gắn liền với hệ)
        GameObject selectedPrefab = elementType switch
        {
            "Metal" => metalPrefab,
            "Wood"  => woodPrefab,
            "Water" => waterPrefab,
            "Fire"  => firePrefab,
            "Earth" => earthPrefab,
            "Wind"  => windPrefab,
            _       => networkPlayerPrefab
        };

        if (selectedPrefab == null)
        {
            // Debug.LogError($"[NetworkPlayerSpawner] Prefab hệ '{elementType}' chưa được gán trong Inspector! Dùng default prefab.");
            selectedPrefab = networkPlayerPrefab;
        }

        return selectedPrefab;
    }

    /// <summary>
    /// Lấy tất cả player prefab (để đăng ký vào NetworkManager)
    /// </summary>
    public System.Collections.Generic.List<GameObject> GetAllPlayerPrefabs()
    {
        var prefabs = new System.Collections.Generic.List<GameObject>();

        if (networkPlayerPrefab                  != null) prefabs.Add(networkPlayerPrefab);
        if (metalPrefab                          != null) prefabs.Add(metalPrefab);
        if (woodPrefab                           != null) prefabs.Add(woodPrefab);
        if (waterPrefab                          != null) prefabs.Add(waterPrefab);
        if (firePrefab                           != null) prefabs.Add(firePrefab);
        if (earthPrefab                          != null) prefabs.Add(earthPrefab);
        if (windPrefab                           != null) prefabs.Add(windPrefab);
        if (hybridEarthFirePrefab_FirePrimary    != null) prefabs.Add(hybridEarthFirePrefab_FirePrimary);
        if (hybridEarthFirePrefab_EarthPrimary   != null) prefabs.Add(hybridEarthFirePrefab_EarthPrimary);
        if (hybridWaterWoodPrefab_WaterPrimary   != null) prefabs.Add(hybridWaterWoodPrefab_WaterPrimary);
        if (hybridWaterWoodPrefab_WoodPrimary    != null) prefabs.Add(hybridWaterWoodPrefab_WoodPrimary);
        if (hybridMetalWindPrefab_MetalPrimary   != null) prefabs.Add(hybridMetalWindPrefab_MetalPrimary);
        if (hybridMetalWindPrefab_WindPrimary    != null) prefabs.Add(hybridMetalWindPrefab_WindPrimary);

        return prefabs;
    }
}
