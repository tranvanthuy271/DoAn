using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
#if UNITY_UI
using UnityEngine.UI;
#endif
#if TMPro
using TMPro;
#endif

// Script chung cho GameScene: Xử lý cả Host và Client
// - Host: Bấm button StartHost để start host
// - Client: Tự động StartClient sau khi login/tạo nhân vật thành công
public class GameSceneNetworkInitializer : MonoBehaviour
{
    private const ushort ModernZoneServerPort = 7777;

    [Header("Server Config")]
    public string serverIP = "";
    public ushort serverPort = 0;

    [Header("UI Elements (Optional - cho Host)")]
    [SerializeField] private UnityEngine.UI.Button startHostButton;
    [SerializeField] private UnityEngine.UI.Button startClientButton;
    [SerializeField] private TMPro.TMP_Text statusText;

    [Header("Auth Sender Prefab (Optional - cho Host)")]
    [Tooltip("Prefab cho AuthSenderNetworkObject. Nếu để trống, sẽ tự động tạo một prefab tạm.")]
    [SerializeField] private GameObject authSenderPrefab;

    [Header("References")]
    private NetworkManagerCustom networkManager;
    private APIClient apiClient;
    private bool playerDataLoaded = false;
    private bool isInitializing = false;
    private bool isHostMode = false;
    private bool isWaitingToConnect = false;

    private static bool HasActiveClientSession()
    {
        var nm = NetworkManager.Singleton;
        return nm != null && nm.IsListening && nm.IsClient && !nm.ShutdownInProgress;
    }

    private void Start()
    {
        // Debug.Log("[GameSceneNetworkInitializer] Initializing GameScene...");

        // QUAN TRỌNG: Đăng ký prefab TRƯỚC KHI start host/client
        RegisterNetworkPrefabs();

        // Setup UI buttons
        SetupUI();

        // Đảm bảo có NetworkManager
        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton == null)
        {
            // Debug.LogError("[GameSceneNetworkInitializer] NetworkManager not found in GameScene! Make sure NetworkManager is in the scene.");
            return;
        }

        // Đảm bảo có NetworkManagerCustom
        networkManager = FindObjectOfType<NetworkManagerCustom>();
        if (networkManager == null)
        {
            GameObject networkManagerObj = new GameObject("NetworkManagerCustom");
            networkManager = networkManagerObj.AddComponent<NetworkManagerCustom>();
        }

        // Setup server IP và port
        serverIP = ResolveServerIp();
        serverPort = ResolveServerPort();
        networkManager.serverIP = serverIP;
        networkManager.serverPort = serverPort;

        // Đăng ký callback xử lý mất kết nối / kết nối thất bại
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnNetworkClientDisconnected;

        // Tự động start client sau khi có player data
        CheckAutoStartClient();
    }

    // Setup UI buttons
    private void SetupUI()
    {
        if (startHostButton != null)
        {
            startHostButton.onClick.AddListener(OnStartHostButtonClicked);
        }

        if (startClientButton != null)
        {
            startClientButton.onClick.AddListener(OnStartClientButtonClicked);
        }

        UpdateUI();
    }

    // Update UI state
    private void UpdateUI()
    {
        bool isConnected = NetworkManager.Singleton != null && 
                          (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer);

        if (startHostButton != null)
            startHostButton.interactable = !isConnected;

        if (startClientButton != null)
            startClientButton.interactable = !isConnected;

        if (statusText != null)
        {
            if (isConnected)
            {
                if (NetworkManager.Singleton.IsHost)
                    statusText.text = "Status: HOST";
                else if (NetworkManager.Singleton.IsClient)
                    statusText.text = "Status: CLIENT";
            }
            else
            {
                statusText.text = "Status: Disconnected";
            }
        }
    }

    private void Update()
    {
        UpdateUI();
    }

    // Đăng ký tất cả NetworkPrefab trước khi start host/client
    private void RegisterNetworkPrefabs()
    {
        // Tìm NetworkPrefabRegistrar trong scene
        NetworkPrefabRegistrar registrar = FindObjectOfType<NetworkPrefabRegistrar>();
        if (registrar == null)
        {
            // Tạo NetworkPrefabRegistrar nếu chưa có
            GameObject registrarObj = new GameObject("NetworkPrefabRegistrar");
            registrar = registrarObj.AddComponent<NetworkPrefabRegistrar>();
            // Debug.Log("[GameSceneNetworkInitializer] Created NetworkPrefabRegistrar.");
        }
        
        // Đăng ký prefab
        registrar.ReRegisterPrefabs();
        // Debug.Log("[GameSceneNetworkInitializer] NetworkPrefabs registered.");
    }

    // Setup các component cần thiết cho Host
    private void SetupHostComponents()
    {
        { /* Setting up host components */ }
        
        // Đảm bảo có ServerConnectionApproval
        ServerConnectionApproval connectionApproval = FindObjectOfType<ServerConnectionApproval>();
        if (connectionApproval == null)
        {
            GameObject approvalObj = new GameObject("ServerConnectionApproval");
            approvalObj.AddComponent<ServerConnectionApproval>();
            { /* Created ServerConnectionApproval */ }
        }
        else
        {
            { /* ServerConnectionApproval already exists */ }
        }

        // Đảm bảo có ServerPlayerDataManager
        if (ServerPlayerDataManager.Instance == null)
        {
            { /* Creating ServerPlayerDataManager */ }
            GameObject serverDataManagerObj = new GameObject("ServerPlayerDataManager");
            serverDataManagerObj.AddComponent<ServerPlayerDataManager>();
            { /* ✓ ServerPlayerDataManager created */ }
        }
        else
        {
            { /* ServerPlayerDataManager instance already exists */ }
        }

        // Đảm bảo có NetworkPlayerSpawner
        NetworkPlayerSpawner spawner = FindObjectOfType<NetworkPlayerSpawner>();
        if (spawner == null)
        {
            // Debug.LogWarning("[GameSceneNetworkInitializer] NetworkPlayerSpawner not found in GameScene! Make sure NetworkPlayerSpawner is in the scene.");
        }
    }

    // Load player data nếu chưa có, rồi tự động StartClient không cần bấm nút.
    private void CheckAutoStartClient()
    {
        if (HasActiveClientSession())
        {
            playerDataLoaded = GameManager.Instance != null && GameManager.Instance.HasPlayerData();
            LoginLoadingManager.HideLoadingStatic();
            GameErrorNotifier.MarkClientConnected();
            { /* Active client session detected. Skip auto StartClient when re-entering GameScene/mapId=0 */ }
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerDataLoaded = true;
            // Auto-start client ngay lập tức
            StartClientMode();
        }
        else
        {
            // Đánh dấu để khi LoadPlayerDataFromAPI hoàn thành sẽ tự connect
            isWaitingToConnect = true;
            LoadPlayerDataFromAPI();
        }
    }

    // Load player data từ API
    private void LoadPlayerDataFromAPI()
    {
        if (isInitializing)
        {
            // Debug.LogWarning("[GameSceneNetworkInitializer] Already loading player data...");
            return;
        }

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (userId == 0)
        {
            // Debug.LogWarning("[GameSceneNetworkInitializer] USER_ID not found in PlayerPrefs. Waiting for manual start (Host mode) or login...");
            return;
        }

        isInitializing = true;
        // Debug.Log($"[GameSceneNetworkInitializer] Loading player data for user ID: {userId}");

        apiClient = APIClient.Instance;
        if (apiClient == null)
        {
            // Debug.LogError("[GameSceneNetworkInitializer] APIClient.Instance is null!");
            isInitializing = false;
            return;
        }

        apiClient.LoadPlayerData(
            userId,
            onSuccess: (playerData) =>
            {
                // Debug.Log($"[GameSceneNetworkInitializer] Player data loaded successfully: {playerData.character_name} ({playerData.element_type} - {playerData.gender}), Level {playerData.level}");
                
                // Lưu vào GameManager
                if (GameManager.Instance == null)
                {
                    GameObject gameManagerObj = new GameObject("GameManager");
                    gameManagerObj.AddComponent<GameManager>();
                }

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPlayerData(playerData);
                    playerDataLoaded = true;
                    // Debug.Log("[GameSceneNetworkInitializer] Player data saved to GameManager.");
                    // Debug.Log("[GameSceneNetworkInitializer] User can now click 'Start Client' button to connect.");
                }

                // KHÔNG tự động start client - để user tự bấm button
                // Nếu đang trong quá trình StartClientMode() thì tiếp tục connect (với delay)
                if (isWaitingToConnect)
                {
                    { /* Player data loaded, continuing client connection with delay */ }
                    isWaitingToConnect = false;
                    StartCoroutine(StartClientAfterDelay());
                }

                isInitializing = false;
            },
            onError: (error) =>
            {
                // Debug.LogError($"[GameSceneNetworkInitializer] Failed to load player data: {error}");
                
                // Nếu lỗi 404 (chưa có player), chuyển về SelectElement
                if (error.Contains("404") || error.Contains("not found") || error.Contains("Player không tồn tại"))
                {
                    // Debug.Log("[GameSceneNetworkInitializer] Player data not found. Returning to SelectElement scene.");
                    SceneManager.LoadScene("SelectElement");
                }
                else
                {
                    // Lỗi khác: quay về Login
                    // Debug.Log("[GameSceneNetworkInitializer] Error loading player data. Returning to Login.");
                    SceneManager.LoadScene("Login");
                }

                isInitializing = false;
            }
        );
    }

    // Start Client Mode: Connect đến host (sẽ tự động gửi userid sau khi connect)
    private void StartClientMode()
    {
        if (isHostMode)
        {
            // Debug.LogWarning("[GameSceneNetworkInitializer] Already in Host mode, skipping client start.");
            return;
        }

        if (HasActiveClientSession())
        {
            LoginLoadingManager.HideLoadingStatic();
            GameErrorNotifier.MarkClientConnected();
            { /* Client is already connected. Skip StartClientMode() */ }
            return;
        }

        // Kiểm tra có player data chưa
        if (!playerDataLoaded)
        {
            int userId = PlayerPrefs.GetInt("USER_ID", 0);
            if (userId == 0)
            {
                // Debug.LogError("[GameSceneNetworkInitializer] USER_ID not found! Please login first.");
                return;
            }

            // Thử load player data trước khi connect
            // Debug.Log("[GameSceneNetworkInitializer] Loading player data before connecting...");
            isWaitingToConnect = true;
            LoadPlayerDataFromAPI();
            return; // Sẽ connect sau khi load xong
        }

        // Có player data rồi, đợi một chút để prefabs được đăng ký trước khi connect
        { /* ===== STARTING CLIENT MODE ===== */ }
        StartCoroutine(StartClientAfterDelay());
    }

    // Thực hiện connect đến host (sau khi đã có player data)
    private void StartClientConnection()
    {
        if (HasActiveClientSession())
        {
            LoginLoadingManager.HideLoadingStatic();
            GameErrorNotifier.MarkClientConnected();
            { /* Client session is already active. Skip StartClientConnection() */ }
            return;
        }

        { /* Starting CLIENT mode, connecting to {serverIP}:{serverPort} */ }
        { /* Auth sẽ đi trong ConnectionData payload (JWT + mapId + zoneId) */ }

        // Connect to host - auth sẽ được gửi tự động qua Named Message trong OnClientConnected
        networkManager.ConnectToServer();
    }

    // Đợi một chút để đảm bảo tất cả prefabs đã được đăng ký trước khi start client
    private System.Collections.IEnumerator StartClientAfterDelay()
    {
        // Đợi một frame để NetworkPlayerSpawner và NetworkPrefabRegistrar có thời gian đăng ký prefabs
        { /* Waiting for prefabs to be registered before starting client */ }
        yield return null;
        
        // Đợi thêm 0.5s để đảm bảo tất cả prefabs đã được đăng ký
        yield return new WaitForSeconds(0.5f);

        if (HasActiveClientSession())
        {
            LoginLoadingManager.HideLoadingStatic();
            GameErrorNotifier.MarkClientConnected();
            { /* Client connected during delay. Abort duplicate StartClient */ }
            yield break;
        }
        
        { /* ✓ Prefabs should be registered now, starting client connection */ }
        StartClientConnection();
    }

    // Button click: Start Host
    public void OnStartHostButtonClicked()
    {
        if (isHostMode)
        {
            // Debug.LogWarning("[GameSceneNetworkInitializer] Already in Host mode!");
            return;
        }

        // Debug.Log("[GameSceneNetworkInitializer] ===== STARTING HOST MODE =====");
        isHostMode = true;

        SetupHostComponents();

        if (authSenderPrefab != null)
        {
            RegisterAuthSenderPrefab(authSenderPrefab);
        }

        // Đảm bảo ConnectionApprovalCallback đã được register
        StartCoroutine(StartHostAfterDelay());
    }

    private string ResolveServerIp()
    {
        string configuredIp = ServerAddressConfig.Instance.gameServerIp;
        if (string.IsNullOrWhiteSpace(serverIP) || serverIP == "127.0.0.1" || serverIP == "localhost")
            return string.IsNullOrWhiteSpace(configuredIp) ? "127.0.0.1" : configuredIp;
        return serverIP;
    }

    private ushort ResolveServerPort()
    {
        ushort configuredPort = ServerAddressConfig.Instance.gameServerPort;
        if (serverPort == 0 || serverPort == 2003 || serverPort == ModernZoneServerPort)
            return configuredPort == 0 ? ModernZoneServerPort : configuredPort;
        return serverPort;
    }

    // Button click: Start Client (manual)
    // Sau khi connect, sẽ tự động gửi userid lên host qua ClientAuthSender
    public void OnStartClientButtonClicked()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            // Debug.LogWarning("[GameSceneNetworkInitializer] Already connected as client!");
            return;
        }

        // Kiểm tra có USER_ID chưa
        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (userId == 0)
        {
            // Debug.LogError("[GameSceneNetworkInitializer] USER_ID not found in PlayerPrefs! Please login first.");
            return;
        }

        // Debug.Log($"[GameSceneNetworkInitializer] User clicked 'Start Client' button. UserId: {userId}");
        // Debug.Log("[GameSceneNetworkInitializer] After connection, userid will be automatically sent to host.");

        StartClientMode();
    }

    // Đợi một frame để đảm bảo ServerConnectionApproval đã register callback trước khi start host
    private System.Collections.IEnumerator StartHostAfterDelay()
    {
        // Đợi một frame để ServerConnectionApproval có thời gian register callback
        yield return null;
        
        // Verify ConnectionApprovalCallback đã được register
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.ConnectionApprovalCallback == null)
            {
                // Debug.LogWarning("[GameSceneNetworkInitializer] ConnectionApprovalCallback is still null! Waiting a bit more...");
                yield return new WaitForSeconds(0.1f);
                
                if (NetworkManager.Singleton.ConnectionApprovalCallback == null)
                {
                    // Debug.LogError("[GameSceneNetworkInitializer] ✗ ConnectionApprovalCallback is NULL! Connection will timeout!");
                }
            }
            else
            {
                // Debug.Log("[GameSceneNetworkInitializer] ✓ ConnectionApprovalCallback is registered.");
            }
        }
        
        // Start host
        StartHost();
    }

    // Start host
    private void StartHost()
    {
        // Debug.Log($"[GameSceneNetworkInitializer] Starting HOST on port {serverPort}...");

        if (networkManager == null)
        {
            // Debug.LogError("[GameSceneNetworkInitializer] NetworkManagerCustom is null! Cannot start host.");
            return;
        }

        // Subscribe OnServerStarted trước khi start host
        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton != null)
        {
            networkManagerSingleton.OnServerStarted += OnServerStarted;
        }

        // Start host
        networkManager.StartHost();

        // CRITICAL: Đăng ký Named Message handler NGAY sau StartHost()
        // Vì OnServerStarted có thể không được gọi hoặc gọi quá nhanh
        if (networkManagerSingleton != null && networkManagerSingleton.IsServer)
        {
            { /* Server started (inline check). Registering auth handler */ }
            networkManager.RegisterAuthMessageHandler();
        }

        // Debug.Log("[GameSceneNetworkInitializer] Host started. Waiting for clients to connect...");
    }

    // Callback khi server đã start - spawn NetworkObject để làm auth sender
    private void OnServerStarted()
    {
        { /* Server started. Host is ready to accept clients */ }
        
        // Đăng ký Named Message handler để nhận auth từ client (thay thế AuthSenderNetworkObject)
        if (networkManager != null)
        {
            networkManager.RegisterAuthMessageHandler();
        }
        // NOTE: AuthSenderNetworkObject không còn cần thiết vì auth giờ dùng Named Messages
    }

    // Đăng ký authSenderPrefab với NetworkManager
    private void RegisterAuthSenderPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            // Debug.LogWarning("[GameSceneNetworkInitializer] authSenderPrefab is null, skipping registration.");
            return;
        }

        NetworkObject netObj = prefab.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            // Debug.LogError("[GameSceneNetworkInitializer] authSenderPrefab does not have NetworkObject component!");
            return;
        }

        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton == null)
        {
            // Debug.LogError("[GameSceneNetworkInitializer] NetworkManager.Singleton is null! Cannot register authSenderPrefab.");
            return;
        }

        // Kiểm tra xem prefab đã được đăng ký chưa
        bool alreadyRegistered = false;
        foreach (var registeredPrefab in networkManagerSingleton.NetworkConfig.Prefabs.Prefabs)
        {
            if (registeredPrefab.Prefab == prefab)
            {
                alreadyRegistered = true;
                break;
            }
        }

        if (!alreadyRegistered)
        {
            networkManagerSingleton.AddNetworkPrefab(prefab);
            // Debug.Log($"[GameSceneNetworkInitializer] ✓ Registered authSenderPrefab: '{prefab.name}'");
        }
        else
        {
            // Debug.Log($"[GameSceneNetworkInitializer] authSenderPrefab '{prefab.name}' already registered.");
        }
    }

    // Spawn AuthSenderNetworkObject khi server start
    private void SpawnAuthSenderNetworkObject()
    {
        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton == null || !networkManagerSingleton.IsServer)
        {
            { /* Lỗi: NetworkManager.Singleton is null or not server! Cannot spawn AuthSenderNetworkObject */ }
            return;
        }

        if (authSenderPrefab == null)
        {
            // NOTE:
            // - Với NetworkManager.NetworkConfig.ForceSamePrefabs = true (đang bật trong scene),
            //   tất cả prefabs phải được đăng ký TRƯỚC khi StartHost/StartClient.
            // - Vì vậy KHÔNG được tạo prefab tạm + AddNetworkPrefab ở đây (OnServerStarted).
            { /* Lỗi: ✗ authSenderPrefab is NOT assigned */ }
            { /* Lỗi: ✗ ForceSamePrefabs is enabled, so you must create a prefab asset with NetworkObject + ClientAuthSender (NetworkBehaviour) and assign it to authSenderPrefab BEFORE starting host */ }
            { /* Lỗi: ✗ Host will still run, but clients may not be able to send auth until a server-spawned NetworkObject exists */ }
            return;
        }

        { /* Spawning AuthSenderNetworkObject from prefab: {authSenderPrefab.name} */ }
        
        // Spawn từ prefab đã được assign
        GameObject authSenderObj = Instantiate(authSenderPrefab);
        authSenderObj.name = "AuthSenderNetworkObject";
        NetworkObject authNetObj = authSenderObj.GetComponent<NetworkObject>();
        var authSenderComponent = authSenderObj.GetComponent<ClientAuthSender>();
        
        if (authNetObj != null)
        {
            if (authSenderComponent == null)
            {
                { /* Lỗi: ✗ authSenderPrefab '{authSenderPrefab.name}' is missing ClientAuthSender (NetworkBehaviour) */ }
                { /* Lỗi: ✗ Fix: Open prefab asset and add ClientAuthSender (NetworkBehaviour) to the same GameObject as NetworkObject */ }
                { /* Lỗi: ✗ Do NOT add this component at runtime; it must exist on the prefab for Netcode to synchronize behaviours */ }
            }

            authNetObj.Spawn();
            { /* ✓ Spawned AuthSenderNetworkObject from prefab: '{authSenderPrefab.name}' */ }
            { /* AuthSenderNetworkObject IsSpawned={authNetObj.IsSpawned}, NetworkObjectId={authNetObj.NetworkObjectId}, HasClientAuthSender={(authSenderComponent != null)} */ }

            // Log components for quick debugging
            var comps = authSenderObj.GetComponents<Component>();
            if (comps != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("[GameSceneNetworkInitializer] AuthSenderNetworkObject components: ");
                for (int i = 0; i < comps.Length; i++)
                {
                    if (comps[i] == null) continue;
                    sb.Append(comps[i].GetType().Name);
                    if (i < comps.Length - 1) sb.Append(", ");
                }
                { /* Ghi nhận: sb.ToString() */ }
            }
        }
        else
        {
            { /* Lỗi: ✗ authSenderPrefab '{authSenderPrefab.name}' does not have NetworkObject component */ }
            Destroy(authSenderObj);
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnNetworkClientDisconnected;
        }
    }

    // Connection error panel

    private bool _connectionErrorShown; // guard: only show once per session

    // Callback khi client bị ngắt kết nối hoặc socket thất bại.
    // NGO fires callback với:
    // - clientId = LocalClientId   → khách nhận disconnect sau khi đã kết nối
    // - clientId = 0               → socket fail trước khi được cấp id
    // - clientId = ulong.MaxValue  → client không bao giờ connect được (connection refused / timeout)
    // Khi IsHost = true → callback nhận ID của các client khác — bỏ qua.
    private void OnNetworkClientDisconnected(ulong clientId)
    {
        if (_connectionErrorShown) return;
        if (GameErrorNotifier.IsDisconnectNotificationSuppressed) return;

        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        // Nếu đang làm host → callback này thuộc về một client khác — không hiện panel
        if (isHostMode || nm.IsHost) return;

        _connectionErrorShown = true;

        string reason = "Không thể kết nối đến máy chủ game.\n"
                      + "Đường truyền Internet có vấn đề hoặc máy chủ đang bảo trì.";
        ShowConnectionErrorPanel(reason);
    }

    private void ShowConnectionErrorPanel(string message)
    {
        GameErrorNotifier.Show(message, onDismiss: () =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
        });
    }
}

