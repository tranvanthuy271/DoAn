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

    [Header("Auto-register Item Pickup Prefab")]
    [SerializeField] private bool autoRegisterItemPickup = true;
    [Tooltip("Direct reference to ItemPickup prefab (drag from Prefabs/UI folder)")]
    [SerializeField] private GameObject itemPickupPrefab;

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
            // Debug.LogWarning("[NetworkPrefabRegistrar] NetworkManager.Singleton is null in Start(). Prefabs will be registered when ReRegisterPrefabs() is called.");
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
            // Debug.LogError("[NetworkPrefabRegistrar] NetworkManager.Singleton is null! Cannot register prefabs.");
            return;
        }

        if (networkManager.NetworkConfig == null)
        {
            // Debug.LogError("[NetworkPrefabRegistrar] NetworkManager.NetworkConfig is null! Cannot register prefabs.");
            return;
        }

        int registeredCount = 0;

        // Tự động lấy prefab từ NetworkPlayerSpawner
        if (autoRegisterFromSpawner)
        {
            // Tìm NetworkPlayerSpawner (bao gồm cả disabled components)
            NetworkPlayerSpawner spawner = FindObjectOfType<NetworkPlayerSpawner>(true); // true = include inactive
            if (spawner != null)
            {
                Debug.Log("[NetworkPrefabRegistrar] ✓ Found NetworkPlayerSpawner, registering prefabs...");
                
                // Lấy tất cả prefab từ spawner
                RegisterPrefabFromSpawner(spawner, networkManager, ref registeredCount);
            }
            else
            {
                Debug.LogError("[NetworkPrefabRegistrar] ❌ NetworkPlayerSpawner NOT FOUND in scene!");
                Debug.LogError("[NetworkPrefabRegistrar] ❌ This will cause connection errors! Add NetworkPlayerSpawner to scene or use manual prefabs.");
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

        // Tự động đăng ký ItemPickup prefab nếu được bật
        if (autoRegisterItemPickup)
        {
            RegisterItemPickupPrefab(networkManager, ref registeredCount);
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
                    NetworkObject netObj = registeredPrefab.Prefab.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        Debug.Log($"[NetworkPrefabRegistrar]   - Prefab: '{registeredPrefab.Prefab.name}' (has NetworkObject)");
                    }
                    else
                    {
                        Debug.Log($"[NetworkPrefabRegistrar]   - Prefab: '{registeredPrefab.Prefab.name}'");
                    }
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
        // Lấy tất cả prefab từ spawner (tìm cả disabled components)
        var prefabs = spawner.GetAllPlayerPrefabs();
        
        if (prefabs.Count == 0)
        {
            Debug.LogWarning($"[NetworkPrefabRegistrar] ⚠️ NetworkPlayerSpawner.GetAllPlayerPrefabs() returned EMPTY list!");
            Debug.LogWarning($"[NetworkPrefabRegistrar] ⚠️ Make sure player prefabs are assigned in NetworkPlayerSpawner Inspector!");
            Debug.LogWarning($"[NetworkPrefabRegistrar] ⚠️ This will cause 'NetworkPrefab could not be found' errors when connecting!");
        }
        
        foreach (GameObject prefab in prefabs)
        {
            if (prefab != null)
            {
                RegisterPrefab(prefab, networkManager, ref registeredCount);
            }
        }
    }

    /// <summary>
    /// Tự động đăng ký ItemPickup prefab
    /// </summary>
    private void RegisterItemPickupPrefab(NetworkManager networkManager, ref int registeredCount)
    {
        GameObject prefabToRegister = null;

        // 1. Thử dùng direct reference nếu đã được gán
        if (itemPickupPrefab != null)
        {
            prefabToRegister = itemPickupPrefab;
            Debug.Log($"[NetworkPrefabRegistrar] Using direct reference for ItemPickup prefab: {itemPickupPrefab.name}");
        }
        // 2. Fallback: Tìm trong scene từ ItemSpawner hoặc EnemyItemDrop
        else
        {
            // Tìm ItemSpawner trong scene
            ItemSpawner itemSpawner = FindObjectOfType<ItemSpawner>();
            if (itemSpawner != null)
            {
                // Dùng reflection để lấy itemPickupPrefab private field
                var field = typeof(ItemSpawner).GetField("itemPickupPrefab", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    prefabToRegister = field.GetValue(itemSpawner) as GameObject;
                    if (prefabToRegister != null)
                    {
                        Debug.Log($"[NetworkPrefabRegistrar] Found ItemPickup prefab from ItemSpawner: {prefabToRegister.name}");
                    }
                }
            }

            // Nếu không tìm thấy, thử tìm từ EnemyItemDrop
            if (prefabToRegister == null)
            {
                EnemyItemDrop enemyItemDrop = FindObjectOfType<EnemyItemDrop>();
                if (enemyItemDrop != null)
                {
                    var field = typeof(EnemyItemDrop).GetField("itemPickupPrefab", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        prefabToRegister = field.GetValue(enemyItemDrop) as GameObject;
                        if (prefabToRegister != null)
                        {
                            Debug.Log($"[NetworkPrefabRegistrar] Found ItemPickup prefab from EnemyItemDrop: {prefabToRegister.name}");
                        }
                    }
                }
            }
        }

        // Đăng ký prefab nếu tìm thấy
        if (prefabToRegister != null)
        {
            RegisterPrefab(prefabToRegister, networkManager, ref registeredCount);
        }
        else
        {
            Debug.LogWarning($"[NetworkPrefabRegistrar] ItemPickup prefab not found! Please assign it in Inspector or make sure ItemSpawner/EnemyItemDrop exists in scene.");
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
            // Debug.LogWarning($"[NetworkPrefabRegistrar] Prefab '{prefab.name}' does not have NetworkObject component! Skipping...");
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
                        // Debug.Log($"[NetworkPrefabRegistrar] Prefab '{prefab.name}' already registered, skipping...");
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
            
            // Log chi tiết để debug
            NetworkObject netObj = prefab.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                Debug.Log($"[NetworkPrefabRegistrar] ✓ Registered prefab: '{prefab.name}' (has NetworkObject)");
            }
            else
            {
                Debug.Log($"[NetworkPrefabRegistrar] ✓ Registered prefab: '{prefab.name}' (no NetworkObject?)");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NetworkPrefabRegistrar] ❌ Failed to register prefab '{prefab.name}': {ex.Message}");
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
