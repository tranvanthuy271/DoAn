using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Script để update position của player lên server khi disconnect hoặc out game
/// </summary>
public class PlayerPositionUpdater : NetworkBehaviour
{
    private APIClient apiClient;
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 5f; // Update mỗi 5 giây (hoặc khi disconnect)

    private void Start()
    {
        apiClient = APIClient.Instance;
    }

    private void Update()
    {
        // Chỉ update nếu là owner của object này
        if (!IsOwner) return;

        // Update position định kỳ (mỗi UPDATE_INTERVAL giây)
        if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
        {
            UpdatePositionToServer();
            lastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Update position lên server
    /// </summary>
    private void UpdatePositionToServer()
    {
        if (apiClient == null)
        {
            Debug.LogWarning("[PlayerPositionUpdater] APIClient is null!");
            return;
        }

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (userId == 0)
        {
            Debug.LogWarning("[PlayerPositionUpdater] USER_ID not found!");
            return;
        }

        // Lấy map_id từ MapManager
        int mapId = 0;
        if (MapManager.Instance != null)
        {
            mapId = MapManager.Instance.GetMapId();
        }

        // Lấy vị trí hiện tại
        Vector3 currentPos = transform.position;
        float positionX = currentPos.x;
        float positionY = currentPos.y;

        // Gọi API để update position
        StartCoroutine(UpdatePositionCoroutine(userId, mapId, positionX, positionY));
    }

    /// <summary>
    /// Coroutine để update position
    /// </summary>
    private IEnumerator UpdatePositionCoroutine(int userId, int mapId, float positionX, float positionY)
    {
        bool completed = false;
        bool success = false;
        
        apiClient.UpdatePlayerPosition(
            userId,
            mapId,
            positionX,
            positionY,
            onSuccess: () =>
            {
                success = true;
                completed = true;
            },
            onError: (error) =>
            {
                Debug.LogError($"[PlayerPositionUpdater] Failed to update position: {error}");
                completed = true;
            }
        );
        
        // Đợi cho đến khi request hoàn thành
        while (!completed)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        if (success)
        {
            Debug.Log($"[PlayerPositionUpdater] Position updated successfully: Map={mapId}, X={positionX}, Y={positionY}");
        }
    }

    /// <summary>
    /// Update position ngay lập tức (gọi khi disconnect)
    /// </summary>
    public void UpdatePositionOnDisconnect()
    {
        UpdatePositionToServer();
    }

    private void OnDestroy()
    {
        // Update position khi object bị destroy (disconnect)
        if (IsOwner)
        {
            UpdatePositionOnDisconnect();
        }
    }
}
