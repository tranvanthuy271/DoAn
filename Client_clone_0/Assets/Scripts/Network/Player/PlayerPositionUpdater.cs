using UnityEngine;
using Unity.Netcode;
using System.Collections;

// Script để update position của player lên server khi disconnect hoặc out game
public class PlayerPositionUpdater : NetworkBehaviour
{
    // Update position lên server
    private void UpdatePositionToServer(bool useCoroutine = true)
    {
        // Hybrid architecture: server persists player position on disconnect via
        // ZonePlayerSessionManager/PeriodicSyncService. Client-side REST position
        // pushes are no longer used.
    }

    // Update position trực tiếp không dùng coroutine (cho OnDestroy)
    private void UpdatePositionDirect(int userId, int mapId, float positionX, float positionY)
    {
        return;
    }

    // Coroutine để update position
    private IEnumerator UpdatePositionCoroutine(int userId, int mapId, float positionX, float positionY)
    {
        yield break;
    }

    // Update position ngay lập tức (gọi khi disconnect)
    public void UpdatePositionOnDisconnect()
    {
        UpdatePositionToServer(useCoroutine: false); // Không dùng coroutine khi disconnect
    }

    public override void OnDestroy()
    {
        // Update position khi object bị destroy (disconnect)
        // QUAN TRỌNG: Không dùng coroutine vì GameObject đang bị destroy
        if (IsOwner)
        {
            UpdatePositionOnDisconnect();
        }
        base.OnDestroy();
    }
}
