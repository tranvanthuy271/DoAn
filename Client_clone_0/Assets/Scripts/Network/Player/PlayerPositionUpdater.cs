using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Script để update position của player lên server khi disconnect hoặc out game
/// </summary>
public class PlayerPositionUpdater : NetworkBehaviour
{
    /// <summary>
    /// Update position lên server
    /// </summary>
    private void UpdatePositionToServer(bool useCoroutine = true)
    {
        // Hybrid architecture: server persists player position on disconnect via
        // ZonePlayerSessionManager/PeriodicSyncService. Client-side REST position
        // pushes are no longer used.
    }

    /// <summary>
    /// Update position trực tiếp không dùng coroutine (cho OnDestroy)
    /// </summary>
    private void UpdatePositionDirect(int userId, int mapId, float positionX, float positionY)
    {
        return;
    }

    /// <summary>
    /// Coroutine để update position
    /// </summary>
    private IEnumerator UpdatePositionCoroutine(int userId, int mapId, float positionX, float positionY)
    {
        yield break;
    }

    /// <summary>
    /// Update position ngay lập tức (gọi khi disconnect)
    /// </summary>
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
