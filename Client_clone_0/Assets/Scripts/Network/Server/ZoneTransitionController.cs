using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
        ExecuteTransferToRoom(clientId, _registry?.GetRoom(targetMapId, targetZoneId), entryPointId);
    }

    /// <summary>
    /// Tạo custom/private zone runtime và đưa client vào đó.
    /// Dùng cho phó bản/party-room thay vì để client tự chọn zone.
    /// </summary>
    public ZoneRoom ServerTransferClientToCustomRoom(
        ulong clientId,
        int targetMapId,
        int entryPointId = 0,
        string customZoneName = null,
        int? maxPlayersOverride = null)
    {
        if (!IsServer || _registry == null) return null;

        var room = _registry.CreateCustomRoom(targetMapId, customZoneName, maxPlayersOverride);
        if (room != null)
            ExecuteTransferToRoom(clientId, room, entryPointId);

        return room;
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

        if (!CanProcessTransferRequest(clientId))
            return;

        if (targetZoneId < 0)
        {
            SendTransferFailedClientRpc("PRIVATE_ZONE_SERVER_ONLY", BuildSingleClientRpcParams(clientId));
            return;
        }

        if (!_registry.CanPlayerChangePublicZone(targetMapId))
        {
            Debug.LogWarning($"[ZoneTransitionController] Map {targetMapId} không cho phép client tự đổi khu.");
            SendTransferFailedClientRpc("ZONE_SWITCH_DISABLED", BuildSingleClientRpcParams(clientId));
            return;
        }

        ZoneRoom requestedRoom = _registry.GetRoom(targetMapId, targetZoneId);
        if (requestedRoom == null)
        {
            SendTransferFailedClientRpc("ZONE_NOT_FOUND", BuildSingleClientRpcParams(clientId));
            return;
        }

        ExecuteTransferToRoom(clientId, requestedRoom, entryPointId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestMapPortalTransferServerRpc(
        int targetMapId,
        int preferredZoneId,
        float targetX,
        float targetY,
        ServerRpcParams rpc = default)
    {
        ulong clientId = rpc.Receive.SenderClientId;

        if (!CanProcessTransferRequest(clientId))
            return;

        ZoneRoom requestedRoom = ResolvePortalTargetRoom(targetMapId, preferredZoneId);
        if (requestedRoom == null)
        {
            Debug.LogWarning($"[ZoneTransitionController] Portal transfer thất bại: map={targetMapId}, zone={preferredZoneId}");
            SendTransferFailedClientRpc("MAP_NOT_FOUND", BuildSingleClientRpcParams(clientId));
            return;
        }

        ExecuteTransferToRoom(clientId, requestedRoom, explicitPosition: new Vector2(targetX, targetY));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ServerRpc — Dungeon entry/exit (zone-based, không disconnect)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Client yêu cầu vào phó bản solo.
    /// Server tạo custom room trên dungeon map rồi transfer client vào.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDungeonEntryServerRpc(
        int dungeonMapId,
        int dungeonConfigId,
        ServerRpcParams rpc = default)
    {
        ulong clientId = rpc.Receive.SenderClientId;

        if (!CanProcessTransferRequest(clientId))
            return;

        MapDefinition mapDef = _config?.GetMap(dungeonMapId);
        if (mapDef == null || mapDef.zoneTopology != MapZoneTopology.InstanceOnly)
        {
            Debug.LogWarning($"[ZoneTransitionController] Dungeon map {dungeonMapId} không hợp lệ hoặc không phải InstanceOnly.");
            SendTransferFailedClientRpc("DUNGEON_MAP_INVALID", BuildSingleClientRpcParams(clientId));
            return;
        }

        var room = _registry.CreateCustomRoom(dungeonMapId);
        if (room == null)
        {
            SendTransferFailedClientRpc("DUNGEON_ROOM_CREATE_FAILED", BuildSingleClientRpcParams(clientId));
            return;
        }

        Debug.Log($"[ZoneTransitionController] Dungeon entry | client={clientId} dungeonConfigId={dungeonConfigId} map={dungeonMapId} zone={room.ZoneId}");

        WaveDungeonRuntime waveRuntime = FindAnyObjectByType<WaveDungeonRuntime>();
        if (waveRuntime != null)
        {
            waveRuntime.BeginEncounter(dungeonConfigId, dungeonMapId, room.ZoneId);
        }
        else
        {
            Debug.LogWarning($"[ZoneTransitionController] WaveDungeonRuntime not found on server. dungeonConfigId={dungeonConfigId}, map={dungeonMapId}, zone={room.ZoneId}");
        }

        // Thông báo client đã vào dungeon (trước khi transfer)
        NotifyDungeonEnteredClientRpc(dungeonConfigId, dungeonMapId, room.ZoneId, BuildSingleClientRpcParams(clientId));

        ExecuteTransferToRoom(clientId, room);
    }

    /// <summary>
    /// Party leader yêu cầu cả tổ đội vào phó bản.
    /// Server tạo 1 custom room, tra userId → clientId rồi transfer tất cả vào cùng room.
    /// partyMemberUserIdsCsv: chuỗi userId ngăn cách bởi dấu phẩy, ví dụ "16,17,18"
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestPartyDungeonEntryServerRpc(
        int dungeonMapId,
        int dungeonConfigId,
        string partyMemberUserIdsCsv,
        ServerRpcParams rpc = default)
    {
        ulong leaderId = rpc.Receive.SenderClientId;

        if (!CanProcessTransferRequest(leaderId))
            return;

        MapDefinition mapDef = _config?.GetMap(dungeonMapId);
        if (mapDef == null || mapDef.zoneTopology != MapZoneTopology.InstanceOnly)
        {
            SendTransferFailedClientRpc("DUNGEON_MAP_INVALID", BuildSingleClientRpcParams(leaderId));
            return;
        }

        // Resolve userIds → clientIds
        var memberClientIds = new List<ulong>();
        var sessionMgr = ZonePlayerSessionManager.Instance;
        if (sessionMgr != null && !string.IsNullOrEmpty(partyMemberUserIdsCsv))
        {
            foreach (string uid in partyMemberUserIdsCsv.Split(','))
            {
                string trimmed = uid.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
                {
                    string clientUserId = sessionMgr.GetPlayerId(client.ClientId);
                    if (string.Equals(clientUserId, trimmed, System.StringComparison.Ordinal))
                    {
                        memberClientIds.Add(client.ClientId);
                        break;
                    }
                }
            }
        }

        // Tạo 1 room duy nhất cho cả party
        int maxPlayers = System.Math.Max(memberClientIds.Count + 1, 4);
        var room = _registry.CreateCustomRoom(dungeonMapId, null, maxPlayers);
        if (room == null)
        {
            SendTransferFailedClientRpc("DUNGEON_ROOM_CREATE_FAILED", BuildSingleClientRpcParams(leaderId));
            return;
        }

        Debug.Log($"[ZoneTransitionController] Party dungeon entry | leader={leaderId} map={dungeonMapId} zone={room.ZoneId} members={memberClientIds.Count}");

        WaveDungeonRuntime waveRuntime = FindAnyObjectByType<WaveDungeonRuntime>();
        if (waveRuntime != null)
        {
            waveRuntime.BeginEncounter(dungeonConfigId, dungeonMapId, room.ZoneId);
        }
        else
        {
            Debug.LogWarning($"[ZoneTransitionController] WaveDungeonRuntime not found on server. dungeonConfigId={dungeonConfigId}, map={dungeonMapId}, zone={room.ZoneId}");
        }

        // Transfer leader trước
        NotifyDungeonEnteredClientRpc(dungeonConfigId, dungeonMapId, room.ZoneId, BuildSingleClientRpcParams(leaderId));
        ExecuteTransferToRoom(leaderId, room);

        // Transfer từng member
        foreach (ulong memberId in memberClientIds)
        {
            if (memberId == leaderId) continue;

            _lastTransferTime[memberId] = Time.time;
            NotifyDungeonEnteredClientRpc(dungeonConfigId, dungeonMapId, room.ZoneId, BuildSingleClientRpcParams(memberId));
            ExecuteTransferToRoom(memberId, room);
        }
    }

    /// <summary>
    /// Client yêu cầu rời phó bản, quay về overworld map.
    /// Server transfer client về map mặc định (map 0) hoặc map lưu trước đó.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDungeonExitServerRpc(
        int returnMapId,
        ServerRpcParams rpc = default)
    {
        ulong clientId = rpc.Receive.SenderClientId;

        if (!CanProcessTransferRequest(clientId))
            return;

        // Tìm zone ít người nhất trên map trả về
        int safeReturnMapId = returnMapId > 0 ? returnMapId : 0;
        ZoneRoom targetRoom = _registry.FindLeastLoadedZone(safeReturnMapId);
        if (targetRoom == null)
        {
            // Fallback về map 0
            targetRoom = _registry.FindLeastLoadedZone(0);
        }
        if (targetRoom == null)
        {
            targetRoom = _registry.GetFallbackRoom();
        }

        if (targetRoom == null)
        {
            Debug.LogError($"[ZoneTransitionController] Không tìm được room nào để return! client={clientId}");
            SendTransferFailedClientRpc("NO_RETURN_ROOM", BuildSingleClientRpcParams(clientId));
            return;
        }

        Debug.Log($"[ZoneTransitionController] Dungeon exit | client={clientId} → map{targetRoom.MapId}_zone{targetRoom.ZoneId}");

        NotifyDungeonExitedClientRpc(BuildSingleClientRpcParams(clientId));
        ExecuteTransferToRoom(clientId, targetRoom);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core transfer logic (server-side only)
    // ─────────────────────────────────────────────────────────────────────────

    private void ExecuteTransferToRoom(
        ulong clientId,
        ZoneRoom targetRoom,
        int entryPointId = 0,
        Vector2? explicitPosition = null)
    {
        if (targetRoom == null)
        {
            Debug.LogWarning("[ZoneTransitionController] Target room null.");
            return;
        }

        Debug.Log($"[ZoneTransitionController] ExecuteTransferToRoom client={clientId} targetMap={targetRoom.MapId} targetZone={targetRoom.ZoneId} entryPointId={entryPointId} explicitPosition={(explicitPosition.HasValue ? explicitPosition.Value.ToString() : "null")}");

        // 3. Zone capacity → fallback to least loaded zone trên cùng map (giải quyết issue #5)
        if (targetRoom.IsFull)
        {
            ZoneRoom fallback = targetRoom.IsCustom ? null : _registry.FindLeastLoadedZone(targetRoom.MapId, targetRoom.ZoneId);
            if (fallback == null || fallback.IsFull)
            {
                Debug.LogWarning($"[ZoneTransitionController] Map {targetRoom.MapId} đầy hết zone. Từ chối transfer.");
                SendTransferFailedClientRpc("MAP_FULL", BuildSingleClientRpcParams(clientId));
                return;
            }
            Debug.Log($"[ZoneTransitionController] Zone {targetRoom.ZoneId} đầy, fallback → zone {fallback.ZoneId}");
            targetRoom = fallback;
        }

        // 4. Update rate-limit
        _lastTransferTime[clientId] = Time.time;

        // 5. Lấy entry point
        Vector2 entry = explicitPosition ?? targetRoom.GetEntryPoint(entryPointId);

        // 6. Lấy scene tương ứng map
        MapDefinition mapDef = _config?.GetMap(targetRoom.MapId);
        string sceneName = mapDef?.sceneName ?? "";

        // 7. In-process room reassignment (giống LangLa: zone.removeChar + Map.maps[id].addChar)
        _registry.AssignClientToRoom(clientId, targetRoom);
        ZonePlayerSessionManager.Instance?.UpdateZone(clientId, targetRoom.MapId, targetRoom.ZoneId);
        Debug.Log($"[ZoneTransitionController] Client {clientId} → map{targetRoom.MapId}_zone{targetRoom.ZoneId} ({entry})");

        // 7b. Di chuyển player NetworkObject server-side đến vị trí mới
        var session = ZonePlayerSessionManager.Instance?.GetSession(clientId);
        if (session?.NetworkObject != null)
        {
            session.NetworkObject.transform.position = new Vector3(entry.x, entry.y, 0);

            // Di chuyển player vào physics scene của map mới
            MapSceneManager.Instance?.MoveToMapScene(session.NetworkObject.gameObject, targetRoom.MapId);
            Debug.Log($"[ZoneTransitionController] Moved client {clientId} player object to {entry.x:F2},{entry.y:F2} in map {targetRoom.MapId}.");
        }

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
        StartCoroutine(SavePositionFireAndForget(clientId, targetRoom, entry));
    }

    private bool CanProcessTransferRequest(ulong clientId)
    {
        if (_lastTransferTime.TryGetValue(clientId, out float last) &&
            Time.time - last < _transferCooldown)
        {
            Debug.Log($"[ZoneTransitionController] Client {clientId} request quá nhanh (cooldown). Bỏ qua.");
            return false;
        }

        if (_registry == null)
        {
            Debug.LogError("[ZoneTransitionController] ZoneRoomRegistry chưa khởi tạo!");
            return false;
        }

        return true;
    }

    private ZoneRoom ResolvePortalTargetRoom(int targetMapId, int preferredZoneId)
    {
        if (_registry == null)
            return null;

        ZoneRoom directRoom = _registry.GetRoom(targetMapId, preferredZoneId);
        if (directRoom != null)
            return directRoom;

        return _registry.FindLeastLoadedZone(targetMapId, preferredZoneId < 0 ? 0 : preferredZoneId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ClientRpc — gửi đến đúng 1 client (NO broadcast)
    // ─────────────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void NotifyDungeonEnteredClientRpc(int dungeonConfigId, int mapId, int zoneId, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[ZoneTransitionController] NotifyDungeonEntered | dungeonConfigId={dungeonConfigId} map={mapId} zone={zoneId}");
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.OnZoneDungeonEntered(dungeonConfigId, mapId, zoneId);
    }

    [ClientRpc]
    private void NotifyDungeonExitedClientRpc(ClientRpcParams rpcParams = default)
    {
        Debug.Log("[ZoneTransitionController] NotifyDungeonExited");
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.OnZoneDungeonExited();
    }

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
        foreach (var filter in FindObjectsByType<NetworkVisibilityZoneFilter>(FindObjectsSortMode.None))
            filter.RefreshVisibility();
    }

    private static ClientRpcParams BuildSingleClientRpcParams(ulong clientId) =>
        new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };

    private IEnumerator SavePositionFireAndForget(ulong clientId, ZoneRoom room, Vector2 pos)
    {
        if (_config == null) yield break;

        // Lấy playerId từ session manager (nếu có)
        string playerId = ZonePlayerSessionManager.Instance?.GetPlayerId(clientId) ?? clientId.ToString();
        if (!int.TryParse(playerId, out int playerIdInt)) yield break;

        // body theo PUT /api/player/{id}/position (dùng InvariantCulture tránh locale dùng dấu phẩy)
        string body = $"{{\"map_id\":{room.MapId},\"zone_id\":{room.ZoneId},"
                      + $"\"position_x\":{pos.x.ToString("F2", CultureInfo.InvariantCulture)},"
                      + $"\"position_y\":{pos.y.ToString("F2", CultureInfo.InvariantCulture)}}}";

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
