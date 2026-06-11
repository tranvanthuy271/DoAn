using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Debug tool: Kiểm tra tình trạng đăng ký prefab và log chi tiết
// Attach vào NetworkManager GameObject để debug
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
            { /* Lỗi: NetworkManager.Singleton is null */ }
            return;
        }

        if (networkManager.NetworkConfig == null)
        {
            { /* Lỗi: NetworkManager.NetworkConfig is null */ }
            return;
        }

        var prefabsList = networkManager.NetworkConfig.Prefabs;
        if (prefabsList == null || prefabsList.Prefabs == null)
        {
            { /* Lỗi: NetworkConfig.Prefabs is null */ }
            return;
        }

        { /* ========== NETWORK PREFABS DEBUG ========== */ }
        { /* Total Registered Prefabs: {prefabsList.Prefabs.Count} */ }
        { /* IsServer: {networkManager.IsServer}, IsClient: {networkManager.IsClient}, IsHost: {networkManager.IsHost} */ }
        
        foreach (var registeredPrefab in prefabsList.Prefabs)
        {
            if (registeredPrefab != null && registeredPrefab.Prefab != null)
            {
                NetworkObject netObj = registeredPrefab.Prefab.GetComponent<NetworkObject>();
                if (netObj != null && enableDetailedLogs)
                {
                    { /* ✓ Prefab: '{registeredPrefab.Prefab.name}' (has NetworkObject) */ }
                }
                else
                {
                    { /* ✓ Prefab: '{registeredPrefab.Prefab.name}' */ }
                }
            }
            else
            {
                { /* Cảnh báo: ✗ NULL prefab entry found */ }
            }
        }
        
        { /* ========================================== */ }
    }

    [ContextMenu("Check NetworkPlayerSpawner Prefabs")]
    public void CheckNetworkPlayerSpawnerPrefabs()
    {
        NetworkPlayerSpawner spawner = FindObjectOfType<NetworkPlayerSpawner>(true); // true = include inactive
        if (spawner == null)
        {
            { /* Lỗi: NetworkPlayerSpawner not found in scene */ }
            return;
        }

        { /* ========== NETWORKPLAYERSPAWNER DEBUG ========== */ }
        { /* NetworkPlayerSpawner found: {spawner.gameObject.name} */ }
        { /* Component enabled: {spawner.enabled} */ }
        
        var prefabs = spawner.GetAllPlayerPrefabs();
        { /* Total prefabs in spawner: {prefabs.Count} */ }
        
        if (prefabs.Count == 0)
        {
            { /* Lỗi: NetworkPlayerSpawner has NO prefabs assigned */ }
            { /* Lỗi: Please assign player prefabs in the Inspector */ }
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
                        { /* ✓ Prefab: '{prefab.name}' (has NetworkObject) */ }
                    }
                    else
                    {
                        { /* Cảnh báo: ⚠ Prefab: '{prefab.name}' (NO NetworkObject component!) */ }
                    }
                }
            }
        }
        
        { /* =============================================== */ }
    }

    [ContextMenu("Force Re-Register All Prefabs")]
    public void ForceReRegisterPrefabs()
    {
        NetworkPrefabRegistrar registrar = FindObjectOfType<NetworkPrefabRegistrar>(true);
        if (registrar == null)
        {
            { /* Cảnh báo: NetworkPrefabRegistrar not found! Creating one */ }
            GameObject obj = new GameObject("NetworkPrefabRegistrar");
            registrar = obj.AddComponent<NetworkPrefabRegistrar>();
        }

        { /* Force re-registering prefabs */ }
        registrar.ReRegisterPrefabs();
        
        // Log sau khi đăng ký
        Invoke(nameof(LogRegisteredPrefabs), 0.1f);
    }
}
