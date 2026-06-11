using UnityEngine;
using Unity.Netcode;
using System;

// Connection Approval Handler: Verify JWT token và map clientId -> userId
// Chỉ chạy trên server
public class ServerConnectionApproval : MonoBehaviour
{
    private NetworkManager networkManager;
    private ServerPlayerDataManager playerDataManager;
    private bool callbackRegistered = false;

    private bool HasMapWorldBootstrap()
    {
        bool hasMapWorld = FindObjectOfType<MapWorldBootstrap>() != null;
        if (hasMapWorld)
        {
            enabled = false;
        }
        return hasMapWorld;
    }

    private void Awake()
    {
        if (HasMapWorldBootstrap())
        {
            { /* MapWorldBootstrap detected  disabling legacy approval handler */ }
            return;
        }

        // Đăng ký callback trong Awake() để đảm bảo có sẵn trước khi StartHost() được gọi
        RegisterCallback();
    }

    private void OnEnable()
    {
        if (HasMapWorldBootstrap())
            return;

        // Đăng ký lại khi object được enable (nếu chưa đăng ký)
        RegisterCallback();
    }

    private void RegisterCallback()
    {
        if (HasMapWorldBootstrap())
            return;

        if (callbackRegistered)
        {
            // Debug.Log("[ServerConnectionApproval] Callback already registered, skipping...");
            return;
        }

        networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            // Debug.LogWarning("[ServerConnectionApproval] NetworkManager.Singleton is null. Will retry in Start().");
            return;
        }

        // Kiểm tra callback hiện tại
        var currentCallback = networkManager.ConnectionApprovalCallback;
        // Debug.Log($"[ServerConnectionApproval] Current callback before register: {(currentCallback == null ? "NULL" : "EXISTS")}");
        
        // Unsubscribe trước để tránh duplicate
        networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        
        // Subscribe
        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        callbackRegistered = true;
        
        // Verify sau khi đăng ký
        var verifyCallback = networkManager.ConnectionApprovalCallback;
        // Debug.Log($"[ServerConnectionApproval] ✓ Connection approval callback registered. Verify: {(verifyCallback == null ? "NULL (ERROR!)" : "EXISTS (OK)")}");
        
        if (verifyCallback == null)
        {
            // Debug.LogError("[ServerConnectionApproval] ✗ CRITICAL: Callback is NULL after registration! This will cause timeout!");
        }
    }

    private void Start()
    {
        if (HasMapWorldBootstrap())
            return;

        // Đảm bảo callback được đăng ký (nếu Awake()/OnEnable() chưa kịp chạy)
        RegisterCallback();
        
        if (!callbackRegistered)
        {
            // Debug.LogError("[ServerConnectionApproval] ✗ Failed to register ConnectionApprovalCallback! NetworkManager may not be initialized.");
        }
        
        // Kiểm tra NetworkManager có Connection Approval enabled không
        if (networkManager != null)
        {
            if (!networkManager.NetworkConfig.ConnectionApproval)
            {
                // Debug.LogError("[ServerConnectionApproval] ✗✗✗ CRITICAL: NetworkManager.NetworkConfig.ConnectionApproval is FALSE! ✗✗✗");
                // Debug.LogError("[ServerConnectionApproval] Connection Approval MUST be enabled in NetworkManager Inspector!");
                // Debug.LogError("[ServerConnectionApproval] Go to NetworkManager > Network Config > Connection Approval > CHECK the checkbox!");
            }
            else
            {
                // Debug.Log("[ServerConnectionApproval] ✓ NetworkManager.NetworkConfig.ConnectionApproval is enabled.");
            }
            
            // Verify callback một lần nữa
            if (networkManager.ConnectionApprovalCallback == null)
            {
                // Debug.LogError("[ServerConnectionApproval] ✗✗✗ CRITICAL: ConnectionApprovalCallback is NULL in Start()! ✗✗✗");
                // Debug.LogError("[ServerConnectionApproval] This will cause connection timeout!");
            }
            else
            {
                // Debug.Log("[ServerConnectionApproval] ✓ ConnectionApprovalCallback verified in Start().");
            }
        }
    }

    private void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        }
    }

    // Connection Approval Callback: Approve connection, client sẽ gửi user_id qua ServerRpc sau khi connect
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        try
        {
            // ConnectionApprovalRequest và ConnectionApprovalResponse là struct, không thể null
            // Nhưng có thể kiểm tra bằng cách check default value hoặc các field quan trọng
            ulong clientId = request.ClientNetworkId;
            
            // Debug.Log($"[ServerConnectionApproval] ===== CONNECTION APPROVAL REQUEST =====");
            // Debug.Log($"[ServerConnectionApproval] ClientId: {clientId}");
            // Debug.Log($"[ServerConnectionApproval] Request received (struct, cannot be null)");
            // Debug.Log($"[ServerConnectionApproval] Response received (struct, cannot be null)");
            // Debug.Log($"[ServerConnectionApproval] Current time: {Time.time}");

            // Approve connection ngay lập tức
            // Client sẽ gửi user_id qua ClientAuthSender.SendAuthServerRpc() sau khi connect
            // ServerPlayerDataManager sẽ load player data khi nhận được user_id từ ClientAuthSender
            response.Approved = true;
            response.CreatePlayerObject = false; // NetworkPlayerSpawner sẽ spawn player
            response.Position = Vector3.zero; // Sẽ được set bởi NetworkPlayerSpawner
            response.Rotation = Quaternion.identity;

            // Debug.Log($"[ServerConnectionApproval] ✓✓✓ Connection APPROVED for clientId: {clientId} ✓✓✓");
            // Debug.Log($"[ServerConnectionApproval] Approved: {response.Approved}, CreatePlayerObject: {response.CreatePlayerObject}");
            // Debug.Log($"[ServerConnectionApproval] Client will send user_id via ServerRpc after connection (ClientAuthSender)");
        }
        catch (System.Exception)
        {
            // Debug.LogError($"[ServerConnectionApproval] ✗ EXCEPTION in ApprovalCheck: {ex.Message}");
            // Debug.LogError($"[ServerConnectionApproval] Stack trace: {ex.StackTrace}");
            
            // Vẫn approve để không block connection
            try
            {
                ulong clientId = request.ClientNetworkId;
                response.Approved = true;
                response.CreatePlayerObject = false;
                // Debug.LogError($"[ServerConnectionApproval] ✓ Approved connection for clientId {clientId} despite exception (fallback)");
            }
            catch (System.Exception)
            {
                // Debug.LogError($"[ServerConnectionApproval] ✗✗✗ CRITICAL: Cannot approve connection even in fallback! {ex2.Message}");
            }
        }
    }

    // Approval data structure (client gửi lên)
    [System.Serializable]
    public class ApprovalData
    {
        public string token;
        public int user_id;
    }
}
