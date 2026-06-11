using System.Text;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;
using Unity.Collections;

// Shared NetworkManager wrapper: Tách logic Host và Client
// StartHost() chỉ dùng trong HostScene
// ConnectToServer() chỉ dùng trong GameScene (Client)
// Auth flow: Client gửi auth ngay khi connect qua CustomMessagingManager (Named Messages)
public class NetworkManagerCustom : MonoBehaviour
{
    private const ushort ModernZoneServerPort = 7777;

    [Header("Server Config")]
    public string serverIP = "127.0.0.1";
    public ushort serverPort = ModernZoneServerPort;

    private void InitFromConfig()
    {
        var cfg = ServerAddressConfig.Instance;
        if (serverIP == "127.0.0.1" || string.IsNullOrWhiteSpace(serverIP))
            serverIP = cfg.gameServerIp;
        if (serverPort == 0 || serverPort == ModernZoneServerPort)
            serverPort = cfg.gameServerPort;
    }

    private const string AUTH_MESSAGE_NAME = "ClientAuth";
    private NetworkManager networkManager;
    private bool callbacksSubscribed = false;
    private bool authMessageHandlerRegistered = false;
    private bool useConnectionApprovalPayload = true;

    void Start()
    {
        Debug.Log("==== [GENE2_DEBUG] NetworkManagerCustom.Start() ACTIVE_GENE_SLOT=" + PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1));
        InitFromConfig();
        EnsureCallbacksSubscribed();
    }

    // Đảm bảo callbacks đã được subscribe. Gọi trong Start() và trước ConnectToServer().
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

    // Connect to host (chỉ dùng trong GameScene - Client)
    public void ConnectToServer()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        if (networkManager == null)
        {
            Debug.LogError("[NetworkManagerCustom] NetworkManager.Singleton is null! Cannot connect.");
            return;
        }

        if (networkManager.IsListening && !networkManager.ShutdownInProgress)
        {
            Debug.LogWarning($"[NetworkManagerCustom] ConnectToServer() skipped because NetworkManager is already listening. IsClient={networkManager.IsClient}, IsServer={networkManager.IsServer}, IsHost={networkManager.IsHost}");
            GameErrorNotifier.MarkClientConnected();
            LoginLoadingManager.HideLoadingStatic();
            return;
        }

        // CRITICAL: Đảm bảo callbacks đã subscribe TRƯỚC khi StartClient
        EnsureCallbacksSubscribed();

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogError("[NetworkManagerCustom] JWT_TOKEN not found in PlayerPrefs! Cannot connect.");
            return;
        }

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[NetworkManagerCustom] UnityTransport not found!");
            return;
        }

        string effectiveIp = ResolveServerIp();
        ushort effectivePort = ResolveServerPort();
        int mapId = ResolveInitialMapId();
        int zoneId = ResolveInitialZoneId();
        int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1);
        string payload = BuildConnectionPayload(token, mapId, zoneId, geneSlot);
        Debug.Log($"==== [GENE2_DEBUG] NetworkManagerCustom.ConnectToServer: ACTIVE_GENE_SLOT={geneSlot} included in payload ====");

        transport.ConnectionData.Address = effectiveIp;
        transport.ConnectionData.Port = effectivePort;
        networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);

        LoginLoadingManager.ShowLoadingStatic("Đang kết nối vào game...");
        GameErrorNotifier.WatchClientConnection();

        serverIP = effectiveIp;
        serverPort = effectivePort;

        Debug.Log($"[NetworkManagerCustom] ConnectToServer: callbacksSubscribed={callbacksSubscribed}, " +
                  $"address={effectiveIp}:{effectivePort}, userId={userId}, mapId={mapId}, zoneId={zoneId}, tokenLength={token.Length}");

        try
        {
            if (networkManager.StartClient())
            {
                Debug.Log($"[NetworkManagerCustom] ✓ StartClient() OK. Connecting to {effectiveIp}:{effectivePort} with approval payload.");
            }
            else
            {
                Debug.LogError("[NetworkManagerCustom] ✗ StartClient() returned false! Check NetworkManager config.");
                GameErrorNotifier.Show(GameErrorNotifier.ErrorType.CannotConnect);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkManagerCustom] ✗ Exception in StartClient: {ex.Message}\n{ex.StackTrace}");
            GameErrorNotifier.Show(GameErrorNotifier.ErrorType.CannotConnect);
        }
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

    private static int ResolveInitialMapId()
    {
        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentMapId >= 0)
            return ClientSceneController.Instance.CurrentMapId;

        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var playerData = GameManager.Instance.GetPlayerData();
            if (playerData != null)
                return playerData.map_id;
        }

        if (MapManager.Instance != null)
            return MapManager.Instance.GetMapId();

        return PlayerPrefs.GetInt("SelectedMapId", 0);
    }

    private static int ResolveInitialZoneId()
    {
        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentZoneId >= 0)
            return ClientSceneController.Instance.CurrentZoneId;

        return 0;
    }

    private static string BuildConnectionPayload(string token, int mapId, int zoneId, int geneSlot = 1)
    {
        string escapedToken = token.Replace("\\", "\\\\").Replace("\"", "\\\"");
        return $"{{\"token\":\"{escapedToken}\",\"mapId\":{mapId},\"zoneId\":{zoneId},\"geneSlot\":{geneSlot}}}";
    }

    // Start host (chỉ dùng trong HostScene)
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

    // Start server only (headless - không dùng trong plan này nhưng giữ lại để tương lai)
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

    // Đăng ký Named Message handler trên server để nhận auth từ client.
    // Gọi SAU KHI StartHost() hoặc StartServer() thành công.
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

    // Server nhận auth message từ client qua CustomMessagingManager
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

    // Client gửi auth message lên server qua CustomMessagingManager.
    // Không cần NetworkObject - hoạt động ngay khi client connected.
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
        Debug.Log($"[NetworkManagerCustom] OnClientConnected snapshot: {BuildConnectionSnapshot(clientId)}");

        if (networkManager != null && networkManager.IsHost && clientId == networkManager.LocalClientId)
        {
            // Host: Load player data trực tiếp
            Debug.Log($"[NetworkManagerCustom] Host-side: Loading player data directly for local client {clientId}...");
            
            int userId = PlayerPrefs.GetInt("USER_ID", 0);
            string token = PlayerPrefs.GetString("JWT_TOKEN", "");
            int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1);
            Debug.Log($"==== [GENE2_DEBUG] NetworkManagerCustom HOST path: ACTIVE_GENE_SLOT = {geneSlot} ====");
            
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
                        Debug.Log($"[NetworkManagerCustom] ✓ Host player data loaded: {playerData.character_name} (slot {geneSlot})");
                    },
                    onError: (error) =>
                    {
                        Debug.LogError($"[NetworkManagerCustom] ✗ Failed to load host player data: {error}");
                    },
                    geneSlot: geneSlot
                );
            }
            else
            {
                Debug.LogError("[NetworkManagerCustom] ServerPlayerDataManager.Instance is null!");
            }
        }
        else if (networkManager != null && networkManager.IsClient && !networkManager.IsServer)
        {
            if (useConnectionApprovalPayload)
            {
                GameErrorNotifier.MarkClientConnected();
                LoginLoadingManager.HideLoadingStatic();
                Debug.Log($"[NetworkManagerCustom] Client-side: approved via ConnectionData payload for clientId {clientId}. Skipping legacy Named Message auth.");
                return;
            }

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

    private string BuildConnectionSnapshot(ulong callbackClientId)
    {
        if (networkManager == null)
            return $"callbackClientId={callbackClientId}, networkManager=null";

        var playerObject = networkManager.LocalClient?.PlayerObject;
        string playerObjectSummary = playerObject != null
            ? $"{playerObject.name}(netId={playerObject.NetworkObjectId}, owner={playerObject.OwnerClientId})"
            : "null";
        int spawnedCount = networkManager.SpawnManager?.SpawnedObjectsList?.Count ?? -1;

        return $"callbackClientId={callbackClientId}, localClientId={networkManager.LocalClientId}, isServer={networkManager.IsServer}, isClient={networkManager.IsClient}, isHost={networkManager.IsHost}, spawnedCount={spawnedCount}, playerObject={playerObjectSummary}, useConnectionApprovalPayload={useConnectionApprovalPayload}";
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (networkManager != null && !networkManager.IsServer)
        {
            string disconnectReason = string.IsNullOrWhiteSpace(networkManager.DisconnectReason)
                ? "<empty>"
                : networkManager.DisconnectReason;
            Debug.LogWarning($"[NetworkManagerCustom] Client disconnected! clientId={clientId}. DisconnectReason={disconnectReason}. Có thể host chưa chạy hoặc bị reject.");
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
