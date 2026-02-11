using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Script để load player data từ API khi vào Main scene và connect đến server
/// Main scene là scene game chính với network sync
/// </summary>
public class MainSceneNetworkInitializer : MonoBehaviour
{
    [Header("References")]
    private APIClient apiClient;
    private NetworkManagerCustom networkManager;

    private bool playerDataLoaded = false;
    private bool isInitializing = false;
    private bool isConnecting = false;
    private bool connectionSuccess = false;

    private void Start()
    {
        // Debug: Kiểm tra scene name
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"[MainSceneNetworkInitializer] Start() called in scene: '{currentSceneName}'");
        
        // Chấp nhận cả "Main" và "GameScene"
        if (currentSceneName != "Main" && currentSceneName != "GameScene")
        {
            Debug.LogWarning($"[MainSceneNetworkInitializer] ⚠️ WARNING: Current scene is '{currentSceneName}', expected 'Main' or 'GameScene'! This script should only be in game scene.");
        }
        
        apiClient = APIClient.Instance;
        networkManager = FindObjectOfType<NetworkManagerCustom>();

        // Kiểm tra xem đã có player data chưa
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            Debug.Log("[MainSceneNetworkInitializer] Player data already loaded from previous scene.");
            playerDataLoaded = true;
            
            // Nếu cần connect đến server, thử connect
            TryConnectToServer();
        }
        else
        {
            // Load player data từ API
            LoadPlayerDataFromAPI();
        }
    }

    /// <summary>
    /// Load player data từ API
    /// </summary>
    private void LoadPlayerDataFromAPI()
    {
        if (isInitializing)
        {
            Debug.LogWarning("[MainSceneNetworkInitializer] Already loading player data...");
            return;
        }

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (userId == 0)
        {
            Debug.LogError("[MainSceneNetworkInitializer] USER_ID not found in PlayerPrefs! Returning to Login scene.");
            SceneManager.LoadScene("Login");
            return;
        }

        isInitializing = true;
        Debug.Log($"[MainSceneNetworkInitializer] Loading player data for user ID: {userId}");

        if (apiClient == null)
        {
            Debug.LogError("[MainSceneNetworkInitializer] APIClient.Instance is null!");
            isInitializing = false;
            return;
        }

        apiClient.LoadPlayerData(
            userId,
            onSuccess: (playerData) =>
            {
                Debug.Log($"[MainSceneNetworkInitializer] Player data loaded successfully: {playerData.character_name} ({playerData.element_type} - {playerData.gender}), Level {playerData.level}");
                
                // Đảm bảo GameManager tồn tại
                if (GameManager.Instance == null)
                {
                    GameObject gameManagerObj = new GameObject("GameManager");
                    gameManagerObj.AddComponent<GameManager>();
                }
                
                // Lưu vào GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPlayerData(playerData);
                    playerDataLoaded = true;
                    Debug.Log("[MainSceneNetworkInitializer] Player data saved to GameManager.");
                }
                else
                {
                    Debug.LogError("[MainSceneNetworkInitializer] GameManager.Instance is null! Cannot save player data.");
                }

                // Thử connect đến server sau khi load player data
                TryConnectToServer();

                isInitializing = false;
            },
            onError: (error) =>
            {
                Debug.LogError($"[MainSceneNetworkInitializer] Failed to load player data: {error}");
                
                // Nếu lỗi 404 (chưa có player), chuyển về SelectElement
                if (error.Contains("404") || error.Contains("not found") || error.Contains("Player không tồn tại"))
                {
                    Debug.Log("[MainSceneNetworkInitializer] Player data not found. Returning to SelectElement scene.");
                    SceneManager.LoadScene("SelectElement");
                }
                else
                {
                    // Lỗi khác: quay về Login
                    Debug.Log("[MainSceneNetworkInitializer] Error loading player data. Returning to Login.");
                    SceneManager.LoadScene("Login");
                }

                isInitializing = false;
            }
        );
    }

    /// <summary>
    /// Kiểm tra xem player data đã được load chưa (để các script khác có thể check)
    /// </summary>
    public bool IsPlayerDataLoaded()
    {
        return playerDataLoaded && GameManager.Instance != null && GameManager.Instance.HasPlayerData();
    }

    /// <summary>
    /// Get player data (nếu đã load)
    /// </summary>
    public PlayerDataResponse GetPlayerData()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            return GameManager.Instance.GetPlayerData();
        }
        return null;
    }

    /// <summary>
    /// Thử connect đến server nếu cần
    /// </summary>
    private void TryConnectToServer()
    {
        // Kiểm tra xem có cần connect không
        string needConnect = PlayerPrefs.GetString("CONNECT_TO_SERVER", "");
        if (string.IsNullOrEmpty(needConnect) || needConnect != "true")
        {
            Debug.Log("[MainSceneNetworkInitializer] Không cần connect đến server.");
            return;
        }

        // Xóa flag
        PlayerPrefs.DeleteKey("CONNECT_TO_SERVER");

        // Lấy server IP và port
        string serverIP = PlayerPrefs.GetString("SERVER_IP", "127.0.0.1");
        int serverPort = PlayerPrefs.GetInt("SERVER_PORT", 2003);

        Debug.Log($"[MainSceneNetworkInitializer] Đang kết nối đến server {serverIP}:{serverPort}...");

        // Đảm bảo có NetworkManager
        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton == null)
        {
            Debug.LogError("[MainSceneNetworkInitializer] NetworkManager not found in Main scene! Make sure NetworkManager is in the scene.");
            return;
        }

        // Đảm bảo có NetworkManagerCustom
        if (networkManager == null)
        {
            GameObject networkManagerObj = new GameObject("NetworkManagerCustom");
            networkManager = networkManagerObj.AddComponent<NetworkManagerCustom>();
        }

        // Setup server IP và port
        networkManager.serverIP = serverIP;
        networkManager.serverPort = (ushort)serverPort;

        // Subscribe to connection events (networkManagerSingleton đã được khai báo ở trên)
        if (networkManagerSingleton != null)
        {
            networkManagerSingleton.OnClientConnectedCallback += OnClientConnectedSuccess;
            networkManagerSingleton.OnClientDisconnectCallback += OnClientDisconnected;
        }

        // Connect to server
        isConnecting = true;
        networkManager.ConnectToServer();
    }

    /// <summary>
    /// Callback khi client connect thành công
    /// </summary>
    private void OnClientConnectedSuccess(ulong clientId)
    {
        Debug.Log($"[MainSceneNetworkInitializer] ✓✓✓ Client {clientId} connected successfully! ✓✓✓");
        connectionSuccess = true;
        isConnecting = false;
        
        // Kiểm tra xem có phải local client không
        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton != null)
        {
            // Nếu là local client (clientId == LocalClientId), đã connect thành công
            if (networkManagerSingleton.LocalClientId == clientId)
            {
                Debug.Log($"[MainSceneNetworkInitializer] ✓ This is LOCAL client {clientId}. Connection established!");
                Debug.Log($"[MainSceneNetworkInitializer] IsClient: {networkManagerSingleton.IsClient}, IsServer: {networkManagerSingleton.IsServer}");
                
                // Gửi auth NGAY LẬP TỨC sau khi connect thành công
                // ClientAuthSender sẽ gửi user_id lên host qua ServerRpc
                // Host sẽ query ServerAPI để lấy player data và spawn player
                Debug.Log($"[MainSceneNetworkInitializer] Sending user_id to host IMMEDIATELY for client {clientId}...");
                ClientAuthSender.SendAuthAfterConnection(clientId);
            }
        }
        
        // Unsubscribe để tránh gọi nhiều lần
        var networkManagerSingleton2 = NetworkManager.Singleton;
        if (networkManagerSingleton2 != null)
        {
            networkManagerSingleton2.OnClientConnectedCallback -= OnClientConnectedSuccess;
        }
    }


    /// <summary>
    /// Callback khi client disconnect
    /// </summary>
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogWarning($"[MainSceneNetworkInitializer] Client {clientId} disconnected from server");
        connectionSuccess = false;
        isConnecting = false;
    }

    private void OnDestroy()
    {
        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton != null)
        {
            networkManagerSingleton.OnClientConnectedCallback -= OnClientConnectedSuccess;
            networkManagerSingleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
