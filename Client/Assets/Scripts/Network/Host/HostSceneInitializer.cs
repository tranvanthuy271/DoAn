using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Host-side script: Start host khi vào HostScene và đợi client connect
/// Chỉ xử lý logic cho HOST
/// </summary>
public class HostSceneInitializer : MonoBehaviour
{
    [Header("Server Config")]
    public ushort serverPort = 2003;

    [Header("Auth Sender Prefab (Optional)")]
    [Tooltip("Prefab cho AuthSenderNetworkObject. Nếu để trống, sẽ tự động tạo một prefab tạm.")]
    public GameObject authSenderPrefab;

    [Header("References")]
    private NetworkManagerCustom networkManager;

    private void Start()
    {
        Debug.Log("[HostSceneInitializer] Initializing Host Scene...");

        // QUAN TRỌNG: Đăng ký prefab TRƯỚC KHI start host
        RegisterNetworkPrefabs();

        // Đảm bảo có NetworkManager
        var networkManagerSingleton = NetworkManager.Singleton;
        if (networkManagerSingleton == null)
        {
            Debug.LogError("[HostSceneInitializer] NetworkManager not found in HostScene! Make sure NetworkManager is in the scene.");
            return;
        }

        // Đảm bảo có NetworkManagerCustom
        networkManager = FindObjectOfType<NetworkManagerCustom>();
        if (networkManager == null)
        {
            GameObject networkManagerObj = new GameObject("NetworkManagerCustom");
            networkManager = networkManagerObj.AddComponent<NetworkManagerCustom>();
        }

        // Đảm bảo có ServerConnectionApproval TRƯỚC KHI start host
        ServerConnectionApproval connectionApproval = FindObjectOfType<ServerConnectionApproval>();
        if (connectionApproval == null)
        {
            GameObject approvalObj = new GameObject("ServerConnectionApproval");
            approvalObj.AddComponent<ServerConnectionApproval>();
            Debug.Log("[HostSceneInitializer] Created ServerConnectionApproval.");
        }
        else
        {
            Debug.Log("[HostSceneInitializer] ServerConnectionApproval found in scene.");
        }

        // Đảm bảo có ServerPlayerDataManager
        if (ServerPlayerDataManager.Instance == null)
        {
            GameObject serverDataManagerObj = new GameObject("ServerPlayerDataManager");
            serverDataManagerObj.AddComponent<ServerPlayerDataManager>();
            Debug.Log("[HostSceneInitializer] Created ServerPlayerDataManager.");
        }

        // Đảm bảo có NetworkPlayerSpawner
        NetworkPlayerSpawner spawner = FindObjectOfType<NetworkPlayerSpawner>();
        if (spawner == null)
        {
            Debug.LogWarning("[HostSceneInitializer] NetworkPlayerSpawner not found in HostScene! Make sure NetworkPlayerSpawner is in the scene.");
        }

        // Setup server port
        networkManager.serverPort = serverPort;

        // Đợi một frame để đảm bảo ServerConnectionApproval đã register callback
        StartCoroutine(StartHostAfterDelay());
    }

    /// <summary>
    /// Đăng ký tất cả NetworkPrefab trước khi start host
    /// </summary>
    private void RegisterNetworkPrefabs()
    {
        // Tìm NetworkPrefabRegistrar trong scene
        NetworkPrefabRegistrar registrar = FindObjectOfType<NetworkPrefabRegistrar>();
        if (registrar == null)
        {
            // Tạo NetworkPrefabRegistrar nếu chưa có
            GameObject registrarObj = new GameObject("NetworkPrefabRegistrar");
            registrar = registrarObj.AddComponent<NetworkPrefabRegistrar>();
            Debug.Log("[HostSceneInitializer] Created NetworkPrefabRegistrar.");
        }
        
        // Đăng ký prefab
        registrar.ReRegisterPrefabs();
        
        // Nếu có authSenderPrefab, đảm bảo nó đã được đăng ký
        if (authSenderPrefab != null)
        {
            RegisterAuthSenderPrefab(authSenderPrefab);
        }
        else
        {
            Debug.LogWarning("[HostSceneInitializer] ⚠️ No authSenderPrefab assigned!");
            Debug.LogWarning("[HostSceneInitializer] ⚠️ AuthSenderNetworkObject will NOT be spawned.");
            Debug.LogWarning("[HostSceneInitializer] ⚠️ Client will need to wait for player spawn to send auth.");
            Debug.LogWarning("[HostSceneInitializer] ⚠️ RECOMMENDED: Create a prefab with NetworkObject component and assign it to authSenderPrefab field.");
        }
        
        Debug.Log("[HostSceneInitializer] NetworkPrefabs registered.");
    }

    /// <summary>
    /// Đăng ký AuthSender prefab vào NetworkManager
    /// </summary>
    private void RegisterAuthSenderPrefab(GameObject prefab)
    {
        if (prefab == null)
        {
            return;
        }

        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[HostSceneInitializer] NetworkManager.Singleton is null! Cannot register authSenderPrefab.");
            return;
        }

        NetworkObject netObj = prefab.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("[HostSceneInitializer] AuthSender prefab is missing NetworkObject component!");
            return;
        }

        // Kiểm tra đã đăng ký chưa
        var prefabsList = networkManager.NetworkConfig.Prefabs;
        if (prefabsList != null)
        {
            foreach (var registeredPrefab in prefabsList.Prefabs)
            {
                if (registeredPrefab != null && registeredPrefab.Prefab != null)
                {
                    if (registeredPrefab.Prefab == prefab || registeredPrefab.Prefab.name == prefab.name)
                    {
                        Debug.Log($"[HostSceneInitializer] AuthSender prefab '{prefab.name}' already registered.");
                        return;
                    }
                }
            }
        }

        // Đăng ký prefab
        try
        {
            networkManager.AddNetworkPrefab(prefab);
            Debug.Log($"[HostSceneInitializer] ✓ Registered AuthSender prefab: {prefab.name}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HostSceneInitializer] Failed to register AuthSender prefab: {ex.Message}");
        }
    }

    /// <summary>
    /// Đợi một frame để đảm bảo ServerConnectionApproval đã register callback trước khi start host
    /// </summary>
    private System.Collections.IEnumerator StartHostAfterDelay()
    {
        // Đợi một frame để ServerConnectionApproval có thời gian register callback
        yield return null;
        
        // Verify ConnectionApprovalCallback đã được register
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.ConnectionApprovalCallback == null)
            {
                Debug.LogWarning("[HostSceneInitializer] ConnectionApprovalCallback is still null! Waiting a bit more...");
                yield return new WaitForSeconds(0.1f);
                
                if (NetworkManager.Singleton.ConnectionApprovalCallback == null)
                {
                    Debug.LogError("[HostSceneInitializer] ✗ ConnectionApprovalCallback is NULL! Connection will timeout!");
                }
            }
            else
            {
                Debug.Log("[HostSceneInitializer] ✓ ConnectionApprovalCallback is registered.");
            }
        }
        
        // Start host
        StartHost();
    }

    /// <summary>
    /// Start host
    /// </summary>
    private void StartHost()
    {
        Debug.Log($"[HostSceneInitializer] Starting host on port {serverPort}...");

        if (networkManager == null)
        {
            Debug.LogError("[HostSceneInitializer] NetworkManagerCustom is null! Cannot start host.");
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

        Debug.Log("[HostSceneInitializer] Host started. Waiting for clients to connect...");
    }

    /// <summary>
    /// Callback khi server đã start - spawn NetworkObject để làm auth sender
    /// </summary>
    private void OnServerStarted()
    {
        Debug.Log("[HostSceneInitializer] Server started. Creating auth sender NetworkObject...");
        
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("[HostSceneInitializer] ✗ Cannot spawn auth sender - NetworkManager is not server!");
            return;
        }

        // Chỉ spawn nếu có prefab được assign
        if (authSenderPrefab == null)
        {
            Debug.LogWarning("[HostSceneInitializer] ⚠️ No authSenderPrefab assigned. Skipping AuthSenderNetworkObject spawn.");
            Debug.LogWarning("[HostSceneInitializer] ⚠️ Client will need to wait for player spawn to send auth via player NetworkObject.");
            return;
        }

        // Spawn từ prefab
        GameObject authSenderObj = Instantiate(authSenderPrefab);
        NetworkObject netObj = authSenderObj.GetComponent<NetworkObject>();
        
        if (netObj == null)
        {
            Debug.LogError("[HostSceneInitializer] ✗ AuthSender prefab is missing NetworkObject component!");
            Destroy(authSenderObj);
            return;
        }

        // Spawn NetworkObject này để client có thể gửi ServerRpc
        try
        {
            netObj.Spawn();
            Debug.Log("[HostSceneInitializer] ✓ Auth sender NetworkObject spawned successfully from prefab.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[HostSceneInitializer] ✗ Failed to spawn auth sender: {ex.Message}");
            if (authSenderObj != null)
            {
                Destroy(authSenderObj);
            }
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
        Debug.Log("[HostSceneInitializer] Host Scene destroyed.");
    }
}
