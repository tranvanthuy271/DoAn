using Unity.Netcode;
using UnityEngine;

// Debug script để verify NetworkPlayerDataSync hoạt động
// Attach vào player prefab để test
public class NetworkPlayerDataSyncDebug : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        { /* ================================================= */ }
        { /* NetworkPlayerDataSyncDebug.OnNetworkSpawn() */ }
        { /* IsServer: {IsServer} */ }
        { /* IsClient: {IsClient} */ }
        { /* IsOwner: {IsOwner} */ }
        { /* OwnerClientId: {OwnerClientId} */ }
        { /* LocalClientId: {NetworkManager.Singleton.LocalClientId} */ }
        
        // Check if NetworkPlayerDataSync exists
        var dataSync = GetComponent<NetworkPlayerDataSync>();
        if (dataSync != null)
        {
            { /* ✓ NetworkPlayerDataSync component found */ }
        }
        else
        {
            { /* Lỗi: ✗ NetworkPlayerDataSync component NOT FOUND */ }
        }
        
        { /* ================================================= */ }
    }
}

