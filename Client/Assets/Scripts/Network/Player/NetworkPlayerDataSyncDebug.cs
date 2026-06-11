using Unity.Netcode;
using UnityEngine;

// Debug script để verify NetworkPlayerDataSync hoạt động
// Attach vào player prefab để test
public class NetworkPlayerDataSyncDebug : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        Debug.Log("=================================================");
        Debug.Log($"[DEBUG] NetworkPlayerDataSyncDebug.OnNetworkSpawn()");
        Debug.Log($"[DEBUG] IsServer: {IsServer}");
        Debug.Log($"[DEBUG] IsClient: {IsClient}");
        Debug.Log($"[DEBUG] IsOwner: {IsOwner}");
        Debug.Log($"[DEBUG] OwnerClientId: {OwnerClientId}");
        Debug.Log($"[DEBUG] LocalClientId: {NetworkManager.Singleton.LocalClientId}");
        
        // Check if NetworkPlayerDataSync exists
        var dataSync = GetComponent<NetworkPlayerDataSync>();
        if (dataSync != null)
        {
            Debug.Log($"[DEBUG] ✓ NetworkPlayerDataSync component found!");
        }
        else
        {
            Debug.LogError($"[DEBUG] ✗ NetworkPlayerDataSync component NOT FOUND!");
        }
        
        Debug.Log("=================================================");
    }
}

