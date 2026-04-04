using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Service để periodic sync (checkpoint) player data lên API Server
/// Chạy mỗi 30-60 giây để đảm bảo data consistency
/// </summary>
public class PeriodicSyncService : NetworkBehaviour
{
    public static PeriodicSyncService Instance { get; private set; }

    [Header("Sync Settings")]
    [Tooltip("Thời gian giữa mỗi lần checkpoint (giây)")]
    public float checkpointInterval = 30f;

    [Tooltip("Có tự động sync không")]
    public bool autoSync = true;

    private float checkpointTimer = 0f;
    private bool isSyncing = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Chỉ server mới sync
        if (!IsServer) return;
        if (!autoSync) return;

        checkpointTimer += Time.deltaTime;
        if (checkpointTimer >= checkpointInterval && !isSyncing)
        {
            SaveCheckpoint();
            checkpointTimer = 0f;
        }
    }

    /// <summary>
    /// Save checkpoint cho tất cả players đang online
    /// </summary>
    private void SaveCheckpoint()
    {
        if (isSyncing) return;

        isSyncing = true;
        // Debug.Log("[PeriodicSyncService] Starting periodic checkpoint...");

        // Collect critical data từ tất cả players
        var allPlayers = FindObjectsOfType<NetworkPlayerDataSync>();
        
        if (allPlayers.Length == 0)
        {
            // Debug.LogWarning("[PeriodicSyncService] No players found for checkpoint");
            isSyncing = false;
            return;
        }

        int savedCount = 0;
        foreach (var player in allPlayers)
        {
            if (player == null) continue;

            int playerId = player.networkPlayerId.Value;
            if (playerId == 0) continue; // Skip invalid playerId

            // Lấy critical data
            Vector3 position = player.transform.position;
            int mapId = GetMapId(); // Lấy từ MapManager hoặc GameManager
            int hp = player.networkHp.Value;
            int mp = player.networkMp.Value;
            int maxHp = player.networkMaxHp.Value;
            int maxMp = player.networkMaxMp.Value;
            int level = player.networkLevel.Value;

            string jwtOverride = "";
            if (ServerPlayerDataManager.Instance != null)
                jwtOverride = ServerPlayerDataManager.Instance.GetClientJwt(player.OwnerClientId);
            if (string.IsNullOrEmpty(jwtOverride) && APIClient.Instance != null)
                jwtOverride = APIClient.Instance.GetToken();

            SavePlayerCheckpoint(playerId, mapId, position.x, position.y, hp, mp, maxHp, maxMp, level, jwtOverride,
                onSuccess: () => {
                    savedCount++;
                    // Debug.Log($"[PeriodicSyncService] ✓ Checkpoint saved for player {playerId}");
                },
                onError: (error) => {
                    // Debug.LogError($"[PeriodicSyncService] ✗ Checkpoint failed for player {playerId}: {error}");
                });
        }

        // Debug.Log($"[PeriodicSyncService] Checkpoint completed: {savedCount}/{allPlayers.Length} players saved");
        isSyncing = false;
    }

    /// <summary>
    /// Save checkpoint cho một player cụ thể
    /// </summary>
    private void SavePlayerCheckpoint(int playerId, int mapId, float posX, float posY,
        int hp, int mp, int maxHp, int maxMp, int level, string jwtOverride,
        System.Action onSuccess = null, System.Action<string> onError = null)
    {
        if (APIClient.Instance == null)
        {
            onError?.Invoke("APIClient.Instance is null");
            return;
        }

        string jsonData =
            $"{{\"map_id\":{mapId},\"position_x\":{posX},\"position_y\":{posY},\"hp\":{hp},\"max_hp\":{maxHp},\"mp\":{mp},\"max_mp\":{maxMp},\"level\":{level}}}";

        APIClient.Instance.UpdatePlayerData(
            playerId,
            jsonData,
            onSuccess: () => { onSuccess?.Invoke(); },
            onError: (error) => { onError?.Invoke(error); },
            jwtOverride: jwtOverride
        );
    }

    /// <summary>
    /// Lấy MapId từ MapManager hoặc GameManager
    /// </summary>
    private int GetMapId()
    {
        // Ưu tiên: Lấy từ GameManager
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var playerData = GameManager.Instance.GetPlayerData();
            if (playerData != null && playerData.map_id > 0)
            {
                return playerData.map_id;
            }
        }

        // Fallback: Lấy từ MapManager (nếu có)
        MapManager mapManager = FindObjectOfType<MapManager>();
        if (mapManager != null)
        {
            // Giả sử MapManager có property currentMapId
            // return mapManager.currentMapId;
        }

        // Default: Map 1
        return 1;
    }

    /// <summary>
    /// Force sync ngay lập tức (không đợi interval)
    /// </summary>
    public void ForceSync()
    {
        if (!IsServer) return;
        if (isSyncing) return;

        checkpointTimer = checkpointInterval; // Trigger sync ngay
    }

    /// <summary>
    /// Sync cho một player cụ thể
    /// </summary>
    public void SyncPlayer(int playerId, Vector3 position, int mapId, int hp, int level)
    {
        if (!IsServer) return;

        int mp = 0;
        int maxHp = hp;
        int maxMp = 0;
        string jwtOverride = APIClient.Instance != null ? APIClient.Instance.GetToken() : "";

        foreach (var player in FindObjectsOfType<NetworkPlayerDataSync>())
        {
            if (player == null || player.networkPlayerId.Value != playerId) continue;

            mp = player.networkMp.Value;
            maxHp = player.networkMaxHp.Value;
            maxMp = player.networkMaxMp.Value;

            if (ServerPlayerDataManager.Instance != null)
            {
                string playerJwt = ServerPlayerDataManager.Instance.GetClientJwt(player.OwnerClientId);
                if (!string.IsNullOrEmpty(playerJwt))
                    jwtOverride = playerJwt;
            }
            break;
        }

        SavePlayerCheckpoint(
            playerId, 
            mapId, 
            position.x, 
            position.y, 
            hp, 
            mp,
            maxHp,
            maxMp,
            level,
            jwtOverride,
            onSuccess: () => { /* Debug.Log($"[PeriodicSyncService] Player {playerId} synced") */ },
            onError: (error) => { /* Debug.LogError($"[PeriodicSyncService] Player {playerId} sync failed: {error}") */ }
        );
    }

    /// <summary>
    /// Set checkpoint interval (có thể config runtime)
    /// </summary>
    public void SetCheckpointInterval(float interval)
    {
        if (interval < 5f) interval = 5f; // Minimum 5 seconds
        if (interval > 300f) interval = 300f; // Maximum 5 minutes

        checkpointInterval = interval;
        // Debug.Log($"[PeriodicSyncService] Checkpoint interval set to {interval} seconds");
    }

    /// <summary>
    /// Enable/disable auto sync
    /// </summary>
    public void SetAutoSync(bool enabled)
    {
        autoSync = enabled;
        // Debug.Log($"[PeriodicSyncService] Auto sync {(enabled ? "enabled" : "disabled")}");
    }
}
