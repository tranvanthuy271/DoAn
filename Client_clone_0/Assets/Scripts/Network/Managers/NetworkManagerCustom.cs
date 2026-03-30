using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;

/// <summary>
/// Shared NetworkManager wrapper: Tách logic Host và Client
/// StartHost() chỉ dùng trong HostScene
/// ConnectToServer() chỉ dùng trong GameScene (Client)
/// Auth flow: Client gửi auth ngay khi connect qua CustomMessagingManager (Named Messages)
/// </summary>
public class NetworkManagerCustom : MonoBehaviour
{
    [Header("Server Config")]
    public string serverIP = "127.0.0.1"; // localhost (cho client)
    public ushort serverPort = 2003;

    private const string AUTH_MESSAGE_NAME = "ClientAuth";
    private NetworkManager networkManager;
    private bool callbacksSubscribed = false;
    private bool authMessageHandlerRegistered = false;

    void Start()
    {
        EnsureCallbacksSubscribed();
    }

    /// <summary>
    /// Đảm bảo callbacks đã được subscribe. Gọi trong Start() và trước ConnectToServer().
    /// </summary>
    private void EnsureCallbacksSubscribed()
    {
        if (callbacksSubscribed) return;

        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogWarning("[NetworkManagerCustom] EnsureCallbacksSubscribed: NetworkManager.Singleton is null, will retry before connect.");
            return;
        }

        // Unsubscribe trước để tránh duplicate subscription
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;

        // Setup callbacks
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        callbacksSubscribed = true;

        Debug.Log("[NetworkManagerCustom] ✓ Callbacks subscribed (OnClientConnected + OnClientDisconnected)");
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

        // CRITICAL: Đảm bảo callbacks đã subscribe TRƯỚC khi StartClient
        EnsureCallbacksSubscribed();

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (userId == 0)
        {
            Debug.LogError("[NetworkManagerCustom] USER_ID not found in PlayerPrefs! Cannot connect.");
            return;
        }

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkManagerCustom] UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = serverIP;
        transport.ConnectionData.Port = serverPort;

        Debug.Log($"[NetworkManagerCustom] ConnectToServer: callbacksSubscribed={callbacksSubscribed}, address={serverIP}:{serverPort}, userId={userId}");

        try
        {
            if (networkManager.StartClient())
            {
                Debug.Log($"[NetworkManagerCustom] ✓ StartClient() OK. Connecting to {serverIP}:{serverPort}");
            }
            else
            {
                Debug.LogError("[NetworkManagerCustom] ✗ StartClient() returned false! Check NetworkManager config.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkManagerCustom] ✗ Exception in StartClient: {ex.Message}\n{ex.StackTrace}");
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
            // Debug.LogError("[NetworkManagerCustom] UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = serverPort;
        
        if (networkManager.StartHost())
        {
            // Debug.Log($"[NetworkManagerCustom] Host started on port {serverPort}");
        }
        else
        {
            // Debug.LogError("[NetworkManagerCustom] Failed to start host!");
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
            // Debug.LogError("[NetworkManagerCustom] UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = serverPort;
        
        if (networkManager.StartServer())
        {
            // Debug.Log($"[NetworkManagerCustom] Server started on port {serverPort}");
        }
        else
        {
            // Debug.LogError("[NetworkManagerCustom] Failed to start server!");
        }
    }

    public void Disconnect()
    {
        if (networkManager != null && networkManager.IsClient)
        {
            networkManager.Shutdown();
            // Debug.Log("[NetworkManagerCustom] Disconnected from server");
        }
    }

    /// <summary>
    /// Đăng ký Named Message handler trên server để nhận auth từ client.
    /// Gọi SAU KHI StartHost() hoặc StartServer() thành công.
    /// </summary>
    public void RegisterAuthMessageHandler()
    {
        if (networkManager == null) networkManager = NetworkManager.Singleton;
        
        if (networkManager == null)
        {
            Debug.LogError("[NetworkManagerCustom] RegisterAuthMessageHandler: NetworkManager is NULL!");
            return;
        }
        
        if (!networkManager.IsServer)
        {
            Debug.LogWarning($"[NetworkManagerCustom] RegisterAuthMessageHandler: Not server (IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient})");
            return;
        }
        
        if (authMessageHandlerRegistered)
        {
            Debug.Log("[NetworkManagerCustom] Auth handler already registered, skipping...");
            return;
        }

        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(AUTH_MESSAGE_NAME, OnAuthMessageReceived);
        authMessageHandlerRegistered = true;
        Debug.Log("[NetworkManagerCustom] ✓ Registered Named Message handler for ClientAuth");
    }

    /// <summary>
    /// Server nhận auth message từ client qua CustomMessagingManager
    /// </summary>
    private void OnAuthMessageReceived(ulong senderClientId, FastBufferReader reader)
    {
        reader.ReadValueSafe(out int userId);
        reader.ReadValueSafe(out ForceNetworkSerializeByMemcpy<FixedString512Bytes> tokenWrapper);
        string token = tokenWrapper.Value.ToString();

        Debug.Log($"[NetworkManagerCustom] ===== AUTH MESSAGE RECEIVED =====");
        Debug.Log($"[NetworkManagerCustom] SenderClientId: {senderClientId}");
        Debug.Log($"[NetworkManagerCustom] UserId: {userId}");
        Debug.Log($"[NetworkManagerCustom] Token length: {token?.Length ?? 0}");

        if (ServerPlayerDataManager.Instance != null)
        {
            // Lưu JWT của client để dùng khi sync DB
            ServerPlayerDataManager.Instance.StoreClientJwt(senderClientId, token);

            ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                senderClientId,
                userId,
                onSuccess: (playerData) =>
                {
                    Debug.Log($"[NetworkManagerCustom] ✓ Player data loaded for client {senderClientId}: {playerData.character_name}");
                },
                onError: (error) =>
                {
                    Debug.LogError($"[NetworkManagerCustom] ✗ Failed to load player data for client {senderClientId}: {error}");
                }
            );
        }
        else
        {
            Debug.LogError($"[NetworkManagerCustom] ✗ ServerPlayerDataManager.Instance is null! Cannot load data for client {senderClientId}");
        }
    }

    /// <summary>
    /// Client gửi auth message lên server qua CustomMessagingManager.
    /// Không cần NetworkObject - hoạt động ngay khi client connected.
    /// </summary>
    private void SendAuthToServer()
    {
        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");

        if (userId == 0 || string.IsNullOrEmpty(token))
        {
            Debug.LogError($"[NetworkManagerCustom] ✗ Cannot send auth: userId={userId}, token empty={string.IsNullOrEmpty(token)}");
            return;
        }

        Debug.Log($"[NetworkManagerCustom] ===== SENDING AUTH VIA NAMED MESSAGE =====");
        Debug.Log($"[NetworkManagerCustom] UserId: {userId}, Token length: {token.Length}");

        // Serialize userId + token vào FastBufferWriter
        var tokenFixed = new FixedString512Bytes(token);
        int size = FastBufferWriter.GetWriteSize(userId) + FastBufferWriter.GetWriteSize(new ForceNetworkSerializeByMemcpy<FixedString512Bytes>(tokenFixed));
        
        using (var writer = new FastBufferWriter(size, Allocator.Temp))
        {
            writer.WriteValueSafe(userId);
            writer.WriteValueSafe(new ForceNetworkSerializeByMemcpy<FixedString512Bytes>(tokenFixed));
            networkManager.CustomMessagingManager.SendNamedMessage(AUTH_MESSAGE_NAME, NetworkManager.ServerClientId, writer);
        }

        Debug.Log($"[NetworkManagerCustom] ✓ Auth message sent to server");
    }

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager != null && networkManager.IsHost && clientId == networkManager.LocalClientId)
        {
            // Host: Load player data trực tiếp
            Debug.Log($"[NetworkManagerCustom] Host-side: Loading player data directly for local client {clientId}...");
            
            int userId = PlayerPrefs.GetInt("USER_ID", 0);
            string token = PlayerPrefs.GetString("JWT_TOKEN", "");
            
            if (userId == 0 || string.IsNullOrEmpty(token))
            {
                Debug.LogError($"[NetworkManagerCustom] Host authentication failed: userId={userId}, token empty={string.IsNullOrEmpty(token)}");
                return;
            }
            
            if (ServerPlayerDataManager.Instance != null)
            {
                // Lưu JWT của HOST
                ServerPlayerDataManager.Instance.StoreClientJwt(clientId, token);

                ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                    clientId,
                    userId,
                    onSuccess: (playerData) =>
                    {
                        Debug.Log($"[NetworkManagerCustom] ✓ Host player data loaded: {playerData.character_name}");
                    },
                    onError: (error) =>
                    {
                        Debug.LogError($"[NetworkManagerCustom] ✗ Failed to load host player data: {error}");
                    }
                );
            }
            else
            {
                Debug.LogError("[NetworkManagerCustom] ServerPlayerDataManager.Instance is null!");
            }
        }
        else if (networkManager != null && networkManager.IsClient && !networkManager.IsServer)
        {
            // Client: Gửi auth NGAY LẬP TỨC qua Named Message (không cần đợi player spawn)
            Debug.Log($"[NetworkManagerCustom] Client-side: Sending auth immediately via Named Message for clientId {clientId}...");
            SendAuthToServer();
        }
        else if (networkManager != null && networkManager.IsServer && clientId != networkManager.LocalClientId)
        {
            // Server-side: Remote client connected, auth sẽ đến qua Named Message
            Debug.Log($"[NetworkManagerCustom] Server-side: Remote client {clientId} connected, waiting for auth via Named Message...");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (networkManager != null && !networkManager.IsServer)
        {
            Debug.LogWarning($"[NetworkManagerCustom] Client disconnected! clientId={clientId}. Có thể host chưa chạy hoặc bị reject.");
        }
        else if (networkManager != null && networkManager.IsServer)
        {
            Debug.Log($"[NetworkManagerCustom] Remote client {clientId} disconnected.");
        }
    }

    void OnDestroy()
    {
        if (networkManager != null && callbacksSubscribed)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            callbacksSubscribed = false;

            if (authMessageHandlerRegistered && networkManager.CustomMessagingManager != null)
            {
                networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(AUTH_MESSAGE_NAME);
                authMessageHandlerRegistered = false;
            }
        }
    }
}
