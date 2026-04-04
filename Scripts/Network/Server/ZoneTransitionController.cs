using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Xử lý chuyển vùng (zone/map) mà KHÔNG cần disconnect.
/// Giống hệt LangLa: zone.removeChar() + Map.maps[newId].addChar() — in-process, instant.
///
/// Gắn vào: "ServerBootstrap" GameObject cùng với MapWorldBootstrap.
/// Dependencies: ZoneRoomRegistry, ClientSceneController (client-side), NetworkVisibilityZoneFilter
/// </summary>
[DisallowMultipleComponent]
public class ZoneTransitionController : NetworkBehaviour
{
    [Header("Security")]
    [Tooltip("Cooldown giữa 2 lần transfer liên tiếp (chống race condition / spam)")]
    [SerializeField] private float _transferCooldown = 2f;

    // Endpoint: PUT /api/player/{playerId}/position  (dùng X-Zone-Api-Key)
    // Đúng URL theo PlayerController thực tế trong GameServerApi

    private ZoneRoomRegistry _registry;
    private MapWorldConfig   _config;

    // Rate-limit: clientId → serverTime lần transfer gần nhất
    private readonly Dictionary<ulong, float> _lastTransferTime = new();

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        _registry = ZoneRoomRegistry.Instance;
        _config   = _registry?.Config;

        if (_registry == null)
            Debug.LogError("[ZoneTransitionController] ZoneRoomRegistry chưa khởi tạo!");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API (gọi từ ZoneTransitionTrigger)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Server-side direct call — dùng khi server muốn force-teleport một client.
    /// </summary>
    public void ServerTransferClient(ulong clientId, int targetMapId, int targetZoneId, int entryPointId = 0)
    {
        if (!IsServer) return;
        ExecuteTransfer(clientId, targetMapId, targetZoneId, entryPointId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ServerRpc — client trigger khi bước vào ZoneTransitionTrigger
    // ─────────────────────────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    public void RequestZoneTransferServerRpc(
        int targetMapId,
        int targetZoneId,
        int entryPointId,
        ServerRpcParams rpc = default)
    {
        ulong clientId = rpc.Receive.SenderClientId;

        // 1. Rate-limit (chống double-trigger và race condition)
        if (_lastTransferTime.TryGetValue(clientId, out float last) &&
            Time.time - last < _transferCooldown)
        {
            Debug.Log($"[ZoneTransitionController] Client {clientId} request quá nhanh (cooldown). Bỏ qua.");
            return;
        }

        ExecuteTransfer(clientId, targetMapId, targetZoneId, entryPointId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core transfer logic (server-side only)
    // ─────────────────────────────────────────────────────────────────────────

    private void ExecuteTransfer(ulong clientId, int targetMapId, int targetZoneId, int entryPointId)
    {
        // 2. Validate destination
        ZoneRoom targetRoom = _registry.GetRoom(targetMapId, targetZoneId);
        if (targetRoom == null)
        {
            Debug.LogWarning($"[ZoneTransitionController] Zone ({targetMapId},{targetZoneId}) không tồn tại!");
            return;
        }

        // 3. Zone capacity → fallback to least loaded zone trên cùng map (giải quyết issue #5)
        if (targetRoom.IsFull)
        {
            ZoneRoom fallback = _registry.FindLeastLoadedZone(targetMapId);
            if (fallback == null || fallback.IsFull)
            {
                Debug.LogWarning($"[ZoneTransitionController] Map {targetMapId} đầy hết zone. Từ chối transfer.");
                SendTransferFailedClientRpc("MAP_FULL", BuildSingleClientRpcParams(clientId));
                return;
            }
            Debug.Log($"[ZoneTransitionController] Zone {targetZoneId} đầy, fallback → zone {fallback.ZoneId}");
            targetRoom = fallback;
        }

        // 4. Update rate-limit
        _lastTransferTime[clientId] = Time.time;

        // 5. Lấy entry point
        Vector2 entry = targetRoom.GetEntryPoint(entryPointId);

        // 6. Lấy scene tương ứng map
        MapDefinition mapDef = _config?.GetMap(targetRoom.MapId);
        string sceneName = mapDef?.sceneName ?? "";

        // 7. In-process room reassignment (giống LangLa: zone.removeChar + Map.maps[id].addChar)
        _registry.AssignClientToRoom(clientId, targetRoom);
        Debug.Log($"[ZoneTransitionController] Client {clientId} → map{targetRoom.MapId}_zone{targetRoom.ZoneId} ({entry})");

        // 8. Refresh NGO visibility (players, enemies, items trong zone cũ/mới)
        RefreshVisibilityForClient(clientId);

        // 9. Gửi ClientRpc ĐẾN ĐÚNG CLIENT (không disconnect/reconnect)
        TeleportToZoneClientRpc(
            targetRoom.MapId,
            targetRoom.ZoneId,
            sceneName,
            entry.x,
            entry.y,
            BuildSingleClientRpcParams(clientId));

        // 10. Save vị trí mới vào API (fire-and-forget, không block)
        StartCoroutine(SavePositionFireAndForget(clientId, targetRoom.MapId, targetRoom.ZoneId, entry));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ClientRpc — gửi đến đúng 1 client (NO broadcast)
    // ─────────────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void TeleportToZoneClientRpc(
        int mapId,
        int zoneId,
        string sceneName,
        float x,
        float y,
        ClientRpcParams rpcParams = default)
    {
        // NGO chỉ gửi ClientRpc đến TargetClientIds — không cần guard thêm
        Debug.Log($"[ZoneTransitionController] Nhận TeleportToZone → scene={sceneName} ({x},{y})");
        ClientSceneController.Instance?.HandleZoneTeleport(sceneName, x, y, mapId, zoneId);
    }

    [ClientRpc]
    private void SendTransferFailedClientRpc(string reason, ClientRpcParams rpcParams = default)
    {
        Debug.LogWarning($"[ZoneTransitionController] Zone transfer thất bại: {reason}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Refresh NGO CheckObjectVisibility cho tất cả NetworkObjects liên quan đến client này.
    /// Gọi sau mỗi lần thay đổi zone.
    /// </summary>
    private void RefreshVisibilityForClient(ulong movedClientId)
    {
        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (!netObj.IsSpawned) continue;
            var filter = netObj.GetComponent<NetworkVisibilityZoneFilter>();
            if (filter != null)
            {
                netObj.NetworkShow(movedClientId);  // NGO sẽ re-evaluate CheckObjectVisibility
                // NetworkHide/NetworkShow triggers OnNetworkObjectVisibilityChanged
            }
        }
    }

    private static ClientRpcParams BuildSingleClientRpcParams(ulong clientId) =>
        new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };

    private IEnumerator SavePositionFireAndForget(ulong clientId, int mapId, int zoneId, Vector2 pos)
    {
        if (_config == null) yield break;

        // Lấy playerId từ session manager (nếu có)
        string playerId = ZonePlayerSessionManager.Instance?.GetPlayerId(clientId) ?? clientId.ToString();
        if (!int.TryParse(playerId, out int playerIdInt)) yield break;

        // body theo PUT /api/player/{id}/position
        string body = $"{{\"map_id\":{mapId},\"zone_id\":{zoneId},"
                      + $"\"position_x\":{pos.x:F2},\"position_y\":{pos.y:F2}}}";

        string url = $"{_config.apiBaseUrl.TrimEnd('/')}/player/{playerIdInt}/position";
        using var req = new UnityEngine.Networking.UnityWebRequest(url, "PUT")
        {
            uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
            downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-Zone-Api-Key", _config.GetZoneApiKey());
        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            Debug.LogWarning($"[ZoneTransitionController] SavePosition failed (non-critical): {req.error}");
    }

    private static string EscapeJson(string s) =>
        s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
}
