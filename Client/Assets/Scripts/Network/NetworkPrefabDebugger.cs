using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Debug tool: Kiểm tra tình trạng đăng ký prefab và log chi tiết
/// Attach vào NetworkManager GameObject để debug
/// </summary>
public class NetworkPrefabDebugger : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool enableDetailedLogs = true;
    [SerializeField] private bool logOnStart = true;
    
    private void Start()
    {
        if (logOnStart)
        {
            Invoke(nameof(LogRegisteredPrefabs), 0.5f); // Delay để đảm bảo prefabs đã được đăng ký
        }
    }

    [ContextMenu("Log All Registered Prefabs")]
    public void LogRegisteredPrefabs()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("[PrefabDebugger] NetworkManager.Singleton is null!");
            return;
        }

        if (networkManager.NetworkConfig == null)
        {
            Debug.LogError("[PrefabDebugger] NetworkManager.NetworkConfig is null!");
            return;
        }

        var prefabsList = networkManager.NetworkConfig.Prefabs;
        if (prefabsList == null || prefabsList.Prefabs == null)
        {
            Debug.LogError("[PrefabDebugger] NetworkConfig.Prefabs is null!");
            return;
        }

        Debug.Log($"========== NETWORK PREFABS DEBUG ==========");
        Debug.Log($"[PrefabDebugger] Total Registered Prefabs: {prefabsList.Prefabs.Count}");
        Debug.Log($"[PrefabDebugger] IsServer: {networkManager.IsServer}, IsClient: {networkManager.IsClient}, IsHost: {networkManager.IsHost}");
        
        foreach (var registeredPrefab in prefabsList.Prefabs)
        {
            if (registeredPrefab != null && registeredPrefab.Prefab != null)
            {
                NetworkObject netObj = registeredPrefab.Prefab.GetComponent<NetworkObject>();
                if (netObj != null && enableDetailedLogs)
                {
                    Debug.Log($"[PrefabDebugger]   ✓ Prefab: '{registeredPrefab.Prefab.name}' (has NetworkObject)");
                }
                else
                {
                    Debug.Log($"[PrefabDebugger]   ✓ Prefab: '{registeredPrefab.Prefab.name}'");
                }
            }
            else
            {
                Debug.LogWarning($"[PrefabDebugger]   ✗ NULL prefab entry found!");
            }
        }
        
        Debug.Log($"==========================================");
    }

    [ContextMenu("Check NetworkPlayerSpawner Prefabs")]
    public void CheckNetworkPlayerSpawnerPrefabs()
    {
        NetworkPlayerSpawner spawner = FindObjectOfType<NetworkPlayerSpawner>(true); // true = include inactive
        if (spawner == null)
        {
            Debug.LogError("[PrefabDebugger] NetworkPlayerSpawner not found in scene!");
            return;
        }

        Debug.Log($"========== NETWORKPLAYERSPAWNER DEBUG ==========");
        Debug.Log($"[PrefabDebugger] NetworkPlayerSpawner found: {spawner.gameObject.name}");
        Debug.Log($"[PrefabDebugger] Component enabled: {spawner.enabled}");
        
        var prefabs = spawner.GetAllPlayerPrefabs();
        Debug.Log($"[PrefabDebugger] Total prefabs in spawner: {prefabs.Count}");
        
        if (prefabs.Count == 0)
        {
            Debug.LogError("[PrefabDebugger] ❌ NetworkPlayerSpawner has NO prefabs assigned!");
            Debug.LogError("[PrefabDebugger] ❌ Please assign player prefabs in the Inspector!");
        }
        else
        {
            foreach (var prefab in prefabs)
            {
                if (prefab != null)
                {
                    NetworkObject netObj = prefab.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        Debug.Log($"[PrefabDebugger]   ✓ Prefab: '{prefab.name}' (has NetworkObject)");
                    }
                    else
                    {
                        Debug.LogWarning($"[PrefabDebugger]   ⚠ Prefab: '{prefab.name}' (NO NetworkObject component!)");
                    }
                }
            }
        }
        
        Debug.Log($"===============================================");
    }

    [ContextMenu("Force Re-Register All Prefabs")]
    public void ForceReRegisterPrefabs()
    {
        NetworkPrefabRegistrar registrar = FindObjectOfType<NetworkPrefabRegistrar>(true);
        if (registrar == null)
        {
            Debug.LogWarning("[PrefabDebugger] NetworkPrefabRegistrar not found! Creating one...");
            GameObject obj = new GameObject("NetworkPrefabRegistrar");
            registrar = obj.AddComponent<NetworkPrefabRegistrar>();
        }

        Debug.Log("[PrefabDebugger] Force re-registering prefabs...");
        registrar.ReRegisterPrefabs();
        
        // Log sau khi đăng ký
        Invoke(nameof(LogRegisteredPrefabs), 0.1f);
    }
}
