using UnityEngine;
using Unity.Netcode;

public class NetworkManagerController : MonoBehaviour
{
    private NetworkManager networkManager;

    private void Awake()
    {
        // Đợi một frame để NetworkManager được khởi tạo
        // NetworkManager có thể chưa sẵn sàng ngay trong Awake
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
        
        if (networkManager == null)
        {
            Debug.LogError("[NetworkManagerController] NetworkManager not found! Make sure NetworkManager GameObject exists in scene with NetworkManager component.");
            return;
        }

        // Unsubscribe trước để tránh duplicate registration
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        networkManager.ConnectionApprovalCallback -= ApprovalCheck;

        // Setup callbacks
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
        
        // Setup Connection Approval (quan trọng để tránh NullReferenceException)
        // Kiểm tra xem đã có callback chưa trước khi đăng ký
        if (networkManager.ConnectionApprovalCallback == null)
        {
            networkManager.ConnectionApprovalCallback += ApprovalCheck;
            Debug.Log("[NetworkManagerController] ConnectionApprovalCallback registered");
        }
        else
        {
            Debug.LogWarning("[NetworkManagerController] ConnectionApprovalCallback already registered, skipping...");
        }
    }
    
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // Approve tất cả connections (có thể thêm logic kiểm tra sau)
        response.Approved = true;
        response.CreatePlayerObject = false; // Không tự động tạo player, để NetworkPlayerSpawner xử lý
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void UnsubscribeEvents()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        }
    }

    // Start Host (Server + Client)
    public void StartHost()
    {
        if (networkManager != null && !networkManager.IsServer)
        {
            networkManager.StartHost();
            Debug.Log("Started as HOST (Server + Client)");
        }
    }

    // Start Client only
    public void StartClient()
    {
        if (networkManager != null && !networkManager.IsClient)
        {
            networkManager.StartClient();
            Debug.Log("Started as CLIENT");
        }
    }

    // Start Server only (headless)
    public void StartServer()
    {
        if (networkManager != null && !networkManager.IsServer)
        {
            networkManager.StartServer();
            Debug.Log("Started as SERVER");
        }
    }

    // Shutdown
    public void Shutdown()
    {
        if (networkManager != null)
        {
            networkManager.Shutdown();
            Debug.Log("Network shutdown");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected!");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected!");
    }
}







