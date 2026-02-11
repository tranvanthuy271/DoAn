using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Tự động đăng ký tất cả player prefab vào NetworkManager
/// Script này đảm bảo cả host và client đều có cùng prefab list
/// </summary>
public class NetworkPrefabRegistrar : MonoBehaviour
{
    [Header("Auto-register from NetworkPlayerSpawner")]
    [SerializeField] private bool autoRegisterFromSpawner = true;

    [Header("Manual Prefab List (if not using auto-register)")]
    [SerializeField] private GameObject[] manualPrefabs;

    private void Awake()
    {
        // Đợi NetworkManager sẵn sàng trước khi đăng ký
        // Nếu NetworkManager chưa có, sẽ đăng ký trong Start()
    }

    private void Start()
    {
        // Đảm bảo NetworkManager đã sẵn sàng
        if (NetworkManager.Singleton != null)
        {
            RegisterPrefabs();
        }
        else
        {
            Debug.LogWarning("[NetworkPrefabRegistrar] NetworkManager.Singleton is null in Start(). Prefabs will be registered when ReRegisterPrefabs() is called.");
        }
    }

    /// <summary>
    /// Đăng ký tất cả prefab vào NetworkManager
    /// </summary>
    private void RegisterPrefabs()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[NetworkPrefabRegistrar] NetworkManager.Singleton is null! Cannot register prefabs.");
            return;
        }

        if (networkManager.NetworkConfig == null)
        {
            Debug.LogError("[NetworkPrefabRegistrar] NetworkManager.NetworkConfig is null! Cannot register prefabs.");
            return;
        }

        int registeredCount = 0;

        // Tự động lấy prefab từ NetworkPlayerSpawner
        if (autoRegisterFromSpawner)
        {
            NetworkPlayerSpawner spawner = FindObjectOfType<NetworkPlayerSpawner>();
            if (spawner != null)
            {
                Debug.Log("[NetworkPrefabRegistrar] Found NetworkPlayerSpawner, registering prefabs...");
                
                // Lấy tất cả prefab từ spawner bằng reflection (vì các field là private)
                RegisterPrefabFromSpawner(spawner, networkManager, ref registeredCount);
            }
            else
            {
                Debug.LogWarning("[NetworkPrefabRegistrar] NetworkPlayerSpawner not found in scene! Using manual prefabs if available.");
            }
        }

        // Đăng ký manual prefabs nếu có
        if (manualPrefabs != null && manualPrefabs.Length > 0)
        {
            foreach (GameObject prefab in manualPrefabs)
            {
                if (prefab != null)
                {
                    RegisterPrefab(prefab, networkManager, ref registeredCount);
                }
            }
        }

        Debug.Log($"[NetworkPrefabRegistrar] ✓ Registered {registeredCount} prefab(s) to NetworkManager");
        
        // Log tất cả prefab đã đăng ký để debug
        LogRegisteredPrefabs(networkManager);
    }

    /// <summary>
    /// Log tất cả prefab đã đăng ký để debug (chỉ theo tên, không dùng hash nội bộ)
    /// </summary>
    private void LogRegisteredPrefabs(NetworkManager networkManager)
    {
        var prefabsList = networkManager.NetworkConfig.Prefabs;
        if (prefabsList != null && prefabsList.Prefabs != null)
        {
            Debug.Log("[NetworkPrefabRegistrar] ===== REGISTERED PREFABS LIST =====");
            Debug.Log($"[NetworkPrefabRegistrar] Total registered prefabs: {prefabsList.Prefabs.Count}");
            foreach (var registeredPrefab in prefabsList.Prefabs)
            {
                if (registeredPrefab != null && registeredPrefab.Prefab != null)
                {
                    Debug.Log($"[NetworkPrefabRegistrar]   - Prefab: '{registeredPrefab.Prefab.name}'");
                }
            }
            Debug.Log("[NetworkPrefabRegistrar] ===== END PREFABS LIST =====");
        }
    }

    /// <summary>
    /// Đăng ký prefab từ NetworkPlayerSpawner
    /// </summary>
    private void RegisterPrefabFromSpawner(NetworkPlayerSpawner spawner, NetworkManager networkManager, ref int registeredCount)
    {
        // Lấy tất cả prefab từ spawner
        var prefabs = spawner.GetAllPlayerPrefabs();
        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
            {
                RegisterPrefab(prefab, networkManager, ref registeredCount);
            }
        }
    }

    /// <summary>
    /// Đăng ký một prefab vào NetworkManager (nếu chưa có)
    /// </summary>
    private void RegisterPrefab(GameObject prefab, NetworkManager networkManager, ref int registeredCount)
    {
        if (prefab == null)
        {
            return;
        }

        // Kiểm tra prefab có NetworkObject component không
        NetworkObject networkObject = prefab.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogWarning($"[NetworkPrefabRegistrar] Prefab '{prefab.name}' does not have NetworkObject component! Skipping...");
            return;
        }

        // Kiểm tra prefab đã được đăng ký chưa
        var prefabsList = networkManager.NetworkConfig.Prefabs;
        if (prefabsList != null)
        {
            foreach (var registeredPrefab in prefabsList.Prefabs)
            {
                if (registeredPrefab != null && registeredPrefab.Prefab != null)
                {
                    // So sánh GameObject reference hoặc name
                    if (registeredPrefab.Prefab == prefab || registeredPrefab.Prefab.name == prefab.name)
                    {
                        Debug.Log($"[NetworkPrefabRegistrar] Prefab '{prefab.name}' already registered, skipping...");
                        return;
                    }
                }
            }
        }

        // Đăng ký prefab
        try
        {
            networkManager.AddNetworkPrefab(prefab);
            registeredCount++;
            Debug.Log($"[NetworkPrefabRegistrar] ✓ Registered prefab: '{prefab.name}'");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkPrefabRegistrar] Failed to register prefab '{prefab.name}': {ex.Message}");
        }
    }

    /// <summary>
    /// Đăng ký lại prefab (có thể gọi từ code khác nếu cần)
    /// </summary>
    public void ReRegisterPrefabs()
    {
        RegisterPrefabs();
    }
}
