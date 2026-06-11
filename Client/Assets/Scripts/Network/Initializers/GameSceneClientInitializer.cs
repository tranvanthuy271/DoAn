using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

// Client-side script: Load player data từ API khi vào GameScene và connect đến host
// Chỉ xử lý logic cho CLIENT
public class GameSceneClientInitializer : MonoBehaviour
{
    private const ushort ModernZoneServerPort = 7777;

    [Header("Server Config")]
    public string serverIP = "";
    public ushort serverPort = 0;

    [Header("References")]
    private APIClient apiClient;
    private NetworkManagerCustom networkManager;

    private bool playerDataLoaded = false;
    private bool isInitializing = false;
    private bool isConnectQueued = false;

    private void Awake()
    {
        if (FindObjectOfType<GameSceneNetworkInitializer>() != null)
        {
            { /* GameSceneNetworkInitializer đã tồn tại  tắt initializer cũ để tránh double-connect */ }
            enabled = false;
        }
    }

    private void Start()
    {
        // QUAN TRỌNG: Đăng ký prefab TRƯỚC KHI connect
        RegisterNetworkPrefabs();

        apiClient = APIClient.Instance;
        networkManager = FindObjectOfType<NetworkManagerCustom>();

        // Kiểm tra xem đã có player data chưa
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            // Debug.Log("[GameSceneClientInitializer] Player data already loaded from previous scene.");
            playerDataLoaded = true;

            QueueConnectToHost(GameManager.Instance.GetPlayerData());
        }
        else
        {
            // Load player data từ API
            LoadPlayerDataFromAPI();
        }
    }

    // Đăng ký tất cả NetworkPrefab trước khi connect
    private void RegisterNetworkPrefabs()
    {
        // Tìm NetworkPrefabRegistrar trong scene
        NetworkPrefabRegistrar registrar = FindObjectOfType<NetworkPrefabRegistrar>();
        if (registrar == null)
        {
            // Tạo NetworkPrefabRegistrar nếu chưa có
            GameObject registrarObj = new GameObject("NetworkPrefabRegistrar");
            registrar = registrarObj.AddComponent<NetworkPrefabRegistrar>();
            // Debug.Log("[GameSceneClientInitializer] Created NetworkPrefabRegistrar.");
        }
        
        // Đăng ký prefab
        registrar.ReRegisterPrefabs();
        // Debug.Log("[GameSceneClientInitializer] NetworkPrefabs registered.");
    }

    // Load player data từ API
    private void LoadPlayerDataFromAPI()
    {
        if (isInitializing)
        {
            // Debug.LogWarning("[GameSceneClientInitializer] Already loading player data...");
            return;
        }

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (userId == 0)
        {
            // Debug.LogError("[GameSceneClientInitializer] USER_ID not found in PlayerPrefs! Returning to Login scene.");
            SceneManager.LoadScene("Login");
            return;
        }

        isInitializing = true;
        // Debug.Log($"[GameSceneClientInitializer] Loading player data for user ID: {userId}");

        if (apiClient == null)
        {
            // Debug.LogError("[GameSceneClientInitializer] APIClient.Instance is null!");
            isInitializing = false;
            return;
        }

        apiClient.LoadPlayerData(
            userId,
            onSuccess: (playerData) =>
            {
                // Debug.Log($"[GameSceneClientInitializer] Player data loaded successfully: {playerData.character_name} ({playerData.element_type} - {playerData.gender}), Level {playerData.level}");
                
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
                    // Debug.Log("[GameSceneClientInitializer] Player data saved to GameManager.");
                }
                else
                {
                    // Debug.LogError("[GameSceneClientInitializer] GameManager.Instance is null! Cannot save player data.");
                }

                QueueConnectToHost(playerData);

                isInitializing = false;
            },
            onError: (error) =>
            {
                // Debug.LogError($"[GameSceneClientInitializer] Failed to load player data: {error}");
                
                // Nếu lỗi 404 (chưa có player), chuyển về SelectElement
                if (error.Contains("404") || error.Contains("not found") || error.Contains("Player không tồn tại"))
                {
                    // Debug.Log("[GameSceneClientInitializer] Player data not found. Returning to SelectElement scene.");
                    SceneManager.LoadScene("SelectElement");
                }
                else
                {
                    // Lỗi khác: quay về Login
                    // Debug.Log("[GameSceneClientInitializer] Error loading player data. Returning to Login.");
                    SceneManager.LoadScene("Login");
                }

                isInitializing = false;
            }
        );
    }

    private void QueueConnectToHost(PlayerDataResponse playerData)
    {
        if (isConnectQueued)
            return;

        if (playerData != null && ClientSceneController.Instance != null)
        {
            // API PlayerDataResponse on client build does not include `zone_id` field.
            // Read zone from PlayerPrefs as the canonical client-side stored zone (fallback 0).
            int zoneId = PlayerPrefs.GetInt("PLAYER_ZONE_ID", 0);
            ClientSceneController.Instance.SetCurrentZoneState(playerData.map_id, zoneId);
        }

        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            return;
        }

        isConnectQueued = true;
        StartCoroutine(ConnectToHostAfterDelay());
    }

    private System.Collections.IEnumerator ConnectToHostAfterDelay()
    {
        yield return null;
        yield return new WaitForSeconds(0.5f);

        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            isConnectQueued = false;
            yield break;
        }

        ConnectToHost();
        isConnectQueued = false;
    }

    // Connect đến host
    private void ConnectToHost()
    {
        // Debug.Log($"[GameSceneClientInitializer] Connecting to host at {serverIP}:{serverPort}...");

        // Đảm bảo có NetworkManager
        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton == null)
        {
            // Debug.LogError("[GameSceneClientInitializer] NetworkManager not found in GameScene! Make sure NetworkManager is in the scene.");
            return;
        }

        // Đảm bảo có NetworkManagerCustom
        if (networkManager == null)
        {
            GameObject networkManagerObj = new GameObject("NetworkManagerCustom");
            networkManager = networkManagerObj.AddComponent<NetworkManagerCustom>();
        }

        // Setup server IP và port
        string configuredIp = ServerAddressConfig.Instance.gameServerIp;
        networkManager.serverIP = (string.IsNullOrWhiteSpace(serverIP) || serverIP == "127.0.0.1" || serverIP == "localhost")
            ? (string.IsNullOrWhiteSpace(configuredIp) ? "127.0.0.1" : configuredIp)
            : serverIP;

        ushort configuredPort = ServerAddressConfig.Instance.gameServerPort;
        networkManager.serverPort = (serverPort == 0 || serverPort == 2003 || serverPort == 7777)
            ? (configuredPort == 0 ? (ushort)7777 : configuredPort)
            : serverPort;

        // Connect to host
        networkManager.ConnectToServer();
    }

    // Kiểm tra xem player data đã được load chưa (để các script khác có thể check)
    public bool IsPlayerDataLoaded()
    {
        return playerDataLoaded && GameManager.Instance != null && GameManager.Instance.HasPlayerData();
    }

    // Get player data (nếu đã load)
    public PlayerDataResponse GetPlayerData()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            return GameManager.Instance.GetPlayerData();
        }
        return null;
    }
}
