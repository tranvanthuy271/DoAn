using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Shared NetworkManager wrapper: Tách logic Host và Client
/// StartHost() chỉ dùng trong HostScene
/// ConnectToServer() chỉ dùng trong GameScene (Client)
/// </summary>
public class NetworkManagerCustom : MonoBehaviour
{
    [Header("Server Config")]
    public string serverIP = "127.0.0.1"; // localhost (cho client)
    public ushort serverPort = 2003;

    private NetworkManager networkManager;
    private bool callbacksSubscribed = false;

    void Start()
    {
        networkManager = NetworkManager.Singleton;
        
        // Chỉ setup callbacks nếu NetworkManager tồn tại và chưa subscribe
        if (networkManager != null && !callbacksSubscribed)
        {
            // Unsubscribe trước để tránh duplicate subscription
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            
            // Setup callbacks
            networkManager.OnClientConnectedCallback += OnClientConnected;
            networkManager.OnClientDisconnectCallback += OnClientDisconnected;
            callbacksSubscribed = true;
            Debug.Log("[NetworkManagerCustom] ✓ Callbacks subscribed");
            
            // Subscribe to scene management events để debug
            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
                networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
                networkManager.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
                networkManager.SceneManager.OnLoadComplete += OnSceneLoadComplete;
            }
        }
        else if (networkManager == null)
        {
            Debug.LogWarning("[NetworkManagerCustom] NetworkManager.Singleton is null! NetworkManager may not be in the scene.");
        }
        else if (callbacksSubscribed)
        {
            Debug.Log("[NetworkManagerCustom] Callbacks already subscribed, skipping...");
        }
    }

    private void OnSceneLoadCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, System.Collections.Generic.List<ulong> clientsCompleted, System.Collections.Generic.List<ulong> clientsTimedOut)
    {
        Debug.Log($"[NetworkManagerCustom] Scene load completed: {sceneName}, Clients completed: {clientsCompleted.Count}, Timed out: {clientsTimedOut.Count}");
    }

    private void OnSceneLoadComplete(ulong clientId, string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode)
    {
        Debug.Log($"[NetworkManagerCustom] Scene load complete for client {clientId}: {sceneName}");
    }

    /// <summary>
    /// Connect to host (chỉ dùng trong GameScene - Client)
    /// </summary>
    public void ConnectToServer()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError("[NetworkManagerCustom] NetworkManager.Singleton is null! Cannot connect.");
            return;
        }

        // Kiểm tra NetworkConfig
        if (networkManager.NetworkConfig == null)
        {
            Debug.LogWarning("[NetworkManagerCustom] NetworkManager.NetworkConfig is null! Có thể gây lỗi khi connect.");
            Debug.LogWarning("[NetworkManagerCustom] Vui lòng đảm bảo NetworkManager được config đúng trong Inspector.");
        }

        // Log scene info trước khi connect
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"[NetworkManagerCustom] Current scene before connect: '{currentScene}'");

        // Lấy user_id từ PlayerPrefs (sẽ gửi lên host sau khi connect qua ServerRpc)
        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (userId == 0)
        {
            Debug.LogError("[NetworkManagerCustom] USER_ID not found in PlayerPrefs! Cannot connect. Please login first.");
            return;
        }
        Debug.Log($"[NetworkManagerCustom] User ID {userId} will be sent to host after connection via ServerRpc");

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkManagerCustom] UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = serverIP;
        transport.ConnectionData.Port = serverPort;
        
        try
        {
            if (networkManager.StartClient())
            {
                Debug.Log($"[NetworkManagerCustom] ✓ StartClient() called successfully. Connecting to {serverIP}:{serverPort} with userId: {userId}");
                Debug.Log($"[NetworkManagerCustom] IsClient: {networkManager.IsClient}, IsServer: {networkManager.IsServer}, IsHost: {networkManager.IsHost}");
            }
            else
            {
                Debug.LogError("[NetworkManagerCustom] Failed to start client! Check NetworkManager configuration.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkManagerCustom] Exception when starting client: {ex.Message}");
            Debug.LogError($"[NetworkManagerCustom] Stack trace: {ex.StackTrace}");
        }
    }

    /// <summary>
    /// Start host (chỉ dùng trong HostScene)
    /// </summary>
    public void StartHost()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkManagerCustom] UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = serverPort;
        
        if (networkManager.StartHost())
        {
            Debug.Log($"[NetworkManagerCustom] Host started on port {serverPort}");
        }
        else
        {
            Debug.LogError("[NetworkManagerCustom] Failed to start host!");
        }
    }

    /// <summary>
    /// Start server only (headless - không dùng trong plan này nhưng giữ lại để tương lai)
    /// </summary>
    public void StartServer()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkManagerCustom] UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = serverPort;
        
        if (networkManager.StartServer())
        {
            Debug.Log($"[NetworkManagerCustom] Server started on port {serverPort}");
        }
        else
        {
            Debug.LogError("[NetworkManagerCustom] Failed to start server!");
        }
    }

    public void Disconnect()
    {
        if (networkManager != null && networkManager.IsClient)
        {
            networkManager.Shutdown();
            Debug.Log("[NetworkManagerCustom] Disconnected from server");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[NetworkManagerCustom] ✓✓✓ OnClientConnectedCallback: Client {clientId} connected ✓✓✓");
        Debug.Log($"[NetworkManagerCustom] Current scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
        Debug.Log($"[NetworkManagerCustom] IsClient: {networkManager.IsClient}, IsServer: {networkManager.IsServer}, IsHost: {networkManager.IsHost}");
        
        // QUAN TRỌNG: Gửi auth ngay sau khi client connect (client-side)
        // Điều này đảm bảo player data được load TRƯỚC KHI NetworkPlayerSpawner spawn player
        if (networkManager != null && networkManager.IsClient && !networkManager.IsServer)
        {
            // Chỉ client (không phải host) mới cần gửi auth ở đây
            Debug.Log($"[NetworkManagerCustom] Client-side: Sending auth immediately after connection...");
            ClientAuthSender.SendAuthAfterConnection(clientId);
        }
        else if (networkManager != null && networkManager.IsHost && clientId == networkManager.LocalClientId)
        {
            // Host: Gửi auth cho local client (client 0) ngay sau khi connect
            Debug.Log($"[NetworkManagerCustom] Host-side: Sending auth for local client {clientId} immediately after connection...");
            ClientAuthSender.SendAuthAfterConnection(clientId);
        }
        
        // Log connected clients - CHỈ trên Server/Host
        bool isServerOrHost = networkManager != null && (networkManager.IsServer || networkManager.IsHost);
        
        if (isServerOrHost)
        {
            try
            {
                if (networkManager.IsServer || networkManager.IsHost)
                {
                    var connectedClients = networkManager.ConnectedClients;
                    if (connectedClients != null)
                    {
                        Debug.Log($"[NetworkManagerCustom] Total connected clients: {connectedClients.Count}");
                        foreach (var client in connectedClients)
                        {
                            Debug.Log($"[NetworkManagerCustom]   - ClientId: {client.Key}, IsLocalClient: {client.Value.PlayerObject != null}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[NetworkManagerCustom] Cannot access ConnectedClients (this is normal on client): {ex.Message}");
            }
        }
        else
        {
            Debug.Log($"[NetworkManagerCustom] Client connected (not server/host, skipping ConnectedClients access)");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkManagerCustom] Client {clientId} disconnected from server");
    }

    void OnDestroy()
    {
        if (networkManager != null && callbacksSubscribed)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            callbacksSubscribed = false;
            
            if (networkManager.SceneManager != null)
            {
                networkManager.SceneManager.OnLoadEventCompleted -= OnSceneLoadCompleted;
                networkManager.SceneManager.OnLoadComplete -= OnSceneLoadComplete;
            }
        }
    }
}
