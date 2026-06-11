using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.Netcode;
using UnityEngine;

// Xử lý chuyển vùng (zone/map) mà KHÔNG cần disconnect.
// Giống hệt LangLa: zone.removeChar() + Map.maps[newId].addChar() — in-process, instant.
// Gắn vào: "ServerBootstrap" GameObject cùng với MapWorldBootstrap.
// Dependencies: ZoneRoomRegistry, ClientSceneController (client-side), NetworkVisibilityZoneFilter
[DisallowMultipleComponent]
public class ZoneTransitionController : NetworkBehaviour
{
    [Header("Security")]
    [Tooltip("Cooldown giữa 2 lần transfer liên tiếp (chống race condition / spam)")]
    [SerializeField] private float _transferCooldown = 0.35f;

    // Endpoint: PUT /api/player/{playerId}/position  (dùng X-Zone-Api-Key)
    // Đúng URL theo PlayerController thực tế trong GameServerApi

    private ZoneRoomRegistry _registry;
    private MapWorldConfig   _config;

    // Rate-limit: clientId → serverTime lần transfer gần nhất
    private readonly Dictionary<ulong, float> _lastTransferTime = new();

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        _registry = ZoneRoomRegistry.Instance;
        _config   = _registry?.Config;

        if (_registry == null)
            Debug.LogError("[ZoneTransitionController] ZoneRoomRegistry chưa khởi tạo!");
    }

    public void BroadcastDungeonStatusToZone(int mapId, int zoneId, string message)
    {
        if (!IsServer)
            return;

        ClientRpcParams rpcParams = BuildZoneClientRpcParams(mapId, zoneId, out int clientCount);
        Debug.Log($"[ZoneTransitionController] BroadcastDungeonStatusToZone | map={mapId} zone={zoneId} clients={clientCount} message='{message}'");
        if (clientCount <= 0)
            return;

        SyncDungeonStatusClientRpc(message ?? string.Empty, rpcParams);
    }

    public void BroadcastWaveStateToZone(int mapId, int zoneId, int currentRound, int maxRounds, int remainingSeconds)
    {
        if (!IsServer)
            return;

        ClientRpcParams rpcParams = BuildZoneClientRpcParams(mapId, zoneId, out int clientCount);
        Debug.Log($"[ZoneTransitionController] BroadcastWaveStateToZone | map={mapId} zone={zoneId} clients={clientCount} round={currentRound}/{maxRounds} remaining={remainingSeconds}s");
        if (clientCount <= 0)
            return;

        SyncWaveStateClientRpc(currentRound, maxRounds, remainingSeconds, rpcParams);
    }

    public void SyncWaveStateToClient(ulong clientId, int currentRound, int maxRounds, int remainingSeconds)
    {
        if (!IsServer)
            return;

        SyncWaveStateClientRpc(currentRound, maxRounds, remainingSeconds, BuildSingleClientRpcParams(clientId));
    }

    public void ShowGlobalNotificationToZone(int mapId, int zoneId, string title, string message, float autoHideSeconds = 0f, string confirmLabel = "Xác nhận")
    {
        if (!IsServer)
            return;

        ClientRpcParams rpcParams = BuildZoneClientRpcParams(mapId, zoneId, out int clientCount);
        Debug.Log($"[ZoneTransitionController] ShowGlobalNotificationToZone | map={mapId} zone={zoneId} clients={clientCount} title='{title}'");
        if (clientCount <= 0)
            return;

        ShowGlobalNotificationClientRpc(title ?? string.Empty, message ?? string.Empty, autoHideSeconds, confirmLabel ?? "Xác nhận", rpcParams);
    }

    public void BeginDungeonReturnFlowToZone(int mapId, int zoneId, bool completed, int countdownSeconds, int returnMapId, string returnSceneName)
    {
        if (!IsServer)
            return;

        ClientRpcParams rpcParams = BuildZoneClientRpcParams(mapId, zoneId, out int clientCount);
        Debug.Log($"[ZoneTransitionController] BeginDungeonReturnFlowToZone | map={mapId} zone={zoneId} clients={clientCount} completed={completed} countdown={countdownSeconds} returnMap={returnMapId}");
        if (clientCount <= 0)
            return;

        BeginDungeonReturnFlowClientRpc(completed, countdownSeconds, returnMapId, string.IsNullOrWhiteSpace(returnSceneName) ? "GameScene" : returnSceneName, rpcParams);
    }

    // Public API (gọi từ ZoneTransitionTrigger)

    // Server-side direct call — dùng khi server muốn force-teleport một client.
    public void ServerTransferClient(ulong clientId, int targetMapId, int targetZoneId, int entryPointId = 0)
    {
        if (!IsServer) return;
        ExecuteTransferToRoom(clientId, _registry?.GetRoom(targetMapId, targetZoneId), entryPointId);
    }

    public bool TryRespawnClientAfterDeath(ulong clientId, NetworkObject playerObject, out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;

        if (!IsServer || _registry == null)
            return false;

        ZoneRoom currentRoom = _registry.GetClientRoom(clientId);
        ZoneRoom targetRoom = currentRoom != null && currentRoom.IsCustom
            ? currentRoom
            : _registry.GetFallbackRoom();

        if (targetRoom == null)
            targetRoom = currentRoom;

        if (targetRoom == null)
            return false;

        Vector2 entry = targetRoom.GetEntryPoint(0);
        spawnPosition = new Vector3(entry.x, entry.y, 0f);

        bool changedRoom = currentRoom == null || currentRoom.ZoneKey != targetRoom.ZoneKey;
        if (changedRoom)
            _registry.AssignClientToRoom(clientId, targetRoom);

        ZonePlayerSessionManager.Instance?.UpdateZone(clientId, targetRoom.MapId, targetRoom.ZoneId);

        if (playerObject != null)
        {
            playerObject.transform.position = spawnPosition;
            MapSceneManager.Instance?.MoveToMapScene(playerObject.gameObject, targetRoom.MapId);
        }

        RefreshVisibilityForClient(clientId);

        MapDefinition mapDef = _config?.GetMap(targetRoom.MapId);
        string sceneName = mapDef?.sceneName ?? targetRoom.SceneName ?? string.Empty;
        TeleportToZoneClientRpc(
            targetRoom.MapId,
            targetRoom.ZoneId,
            sceneName,
            entry.x,
            entry.y,
            BuildSingleClientRpcParams(clientId));

        StartCoroutine(SavePositionFireAndForget(clientId, targetRoom, entry));
        Debug.Log($"[ZoneTransitionController] Death respawn client {clientId} -> {targetRoom.ZoneKey} ({entry}) custom={targetRoom.IsCustom}");
        return true;
    }

    // Tạo custom/private zone runtime và đưa client vào đó.
    // Dùng cho phó bản/party-room thay vì để client tự chọn zone.
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

    // ServerRpc — client trigger khi bước vào ZoneTransitionTrigger

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

    // ServerRpc — Dungeon entry/exit (zone-based, không disconnect)

    // Client yêu cầu vào phó bản solo.
    // Server tạo custom room trên dungeon map rồi transfer client vào.
    [ServerRpc(RequireOwnership = false)]
    public void RequestDungeonEntryServerRpc(
        int dungeonMapId,
        int dungeonConfigId,
        ServerRpcParams rpc = default)
    {
        ulong clientId = rpc.Receive.SenderClientId;

        if (!CanProcessTransferRequest(clientId))
            return;

        // Wave session management
        var sessionMgrWave = ZonePlayerSessionManager.Instance;
        var waveMgr        = WaveSessionManager.GetOrCreateInstance(gameObject);
        string userId      = sessionMgrWave?.GetPlayerId(clientId);

        // ── [RECONNECT-DEBUG] Bước 4: log khi client gọi RequestDungeonEntry ─
        bool dbgHasWaveMgr    = waveMgr != null;
        bool dbgHasUserId     = !string.IsNullOrEmpty(userId);
        bool dbgHasActive     = dbgHasWaveMgr && dbgHasUserId && waveMgr.HasActiveSession(userId);
        var  dbgExisting      = dbgHasActive ? waveMgr.GetSession(userId) : null;
        Debug.Log($"[RECONNECT-DEBUG][4-EntryRpc] clientId={clientId} userId={userId ?? "null"} " +
                  $"dungeonMapId={dungeonMapId} dungeonConfigId={dungeonConfigId} " +
                  $"waveMgr={dbgHasWaveMgr} hasActiveSession={dbgHasActive} " +
                  $"existingZoneRoom={(dbgExisting?.ZoneRoom != null ? dbgExisting.ZoneRoom.ZoneKey : "null")} " +
                  $"existingMapId={dbgExisting?.MapId} existingZoneId={dbgExisting?.ZoneId} " +
                  $"existingRound={dbgExisting?.CurrentRound} existingRemaining={dbgExisting?.RemainingSeconds}");

        // 1. Kiểm tra phiên đang hoạt động (reconnect restore)
        if (!string.IsNullOrEmpty(userId) && waveMgr != null && waveMgr.HasActiveSession(userId))
        {
            var existing = waveMgr.GetSession(userId);
            Debug.Log($"[ZoneTransitionController] Restore wave session userId={userId} dungeonId={existing.DungeonId} zone={existing.ZoneId} round={existing.CurrentRound} remaining={existing.RemainingSeconds}s");
            ZoneRoom restoredRoom = ResolveRestorableWaveRoom(existing);
            existing.ZoneRoom = restoredRoom;

            Debug.Log($"[RECONNECT-DEBUG][4a-Restore] userId={userId} " +
                      $"ResolveRestorableWaveRoom → {(restoredRoom != null ? restoredRoom.ZoneKey : "NULL → sẽ rơi vào fresh entry!")} " +
                      $"ZoneRoom.IsCustom={restoredRoom?.IsCustom} " +
                      $"registry.GetRoom({existing.MapId},{existing.ZoneId})={(ZoneRoomRegistry.Instance?.GetRoom(existing.MapId, existing.ZoneId)?.ZoneKey ?? "null")}");

            if (restoredRoom != null)
            {
                Debug.Log($"[RECONNECT-DEBUG][4b-RestoreOK] userId={userId} restore thành công → transfer vào {restoredRoom.ZoneKey}");
                NotifyDungeonEnteredClientRpc(existing.DungeonId, existing.MapId, existing.ZoneId, BuildSingleClientRpcParams(clientId));
                ExecuteTransferToRoom(clientId, restoredRoom);
                SyncWaveStateToClient(clientId, existing.CurrentRound, existing.MaxRounds, existing.RemainingSeconds);
                return;
            }
            Debug.LogWarning($"[RECONNECT-DEBUG][4c-RestoreFail] userId={userId} restoredRoom=null → EndSession và fresh entry!");
            Debug.LogWarning($"[ZoneTransitionController] Session userId={userId} có ZoneRoom null — bắt đầu fresh entry.");
            waveMgr.EndSession(userId);
        }
        else if (dbgHasUserId && !dbgHasActive)
        {
            Debug.Log($"[RECONNECT-DEBUG][4d-NoActive] userId={userId} không có active wave session → sẽ vào fresh entry.");
        }

        // 2. Kiểm tra lượt hàng ngày
        if (!string.IsNullOrEmpty(userId) && waveMgr != null && !waveMgr.CheckDailyLimit(userId, dungeonConfigId))
        {
            int used = waveMgr.GetDailyUsedCount(userId, dungeonConfigId);
            int allowed = waveMgr.GetDailyAllowedCount(userId);
            Debug.Log($"[ZoneTransitionController] Hết lượt hôm nay userId={userId} dungeonId={dungeonConfigId} usedToday={used}");
            ShowGlobalNotificationClientRpc(
                "Đã Hết Lượt Hôm Nay",
                $"Bạn đã sử dụng hết lượt tham gia phó bản hôm nay ({used}/{allowed} lượt). Hãy quay lại sau 00:00 hoặc dùng Vé Phó Bản 409/410 để cộng thêm lượt.",
                5f, "Xác nhận",
                BuildSingleClientRpcParams(clientId));
            return;
        }

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

        Debug.Log($"[ZoneTransitionController] Dungeon entry | client={clientId} userId={userId} dungeonConfigId={dungeonConfigId} map={dungeonMapId} zone={room.ZoneId}");

        // Đăng ký phiên và tiêu thụ lượt
        if (!string.IsNullOrEmpty(userId) && waveMgr != null)
        {
            waveMgr.BeginSession(userId, dungeonConfigId, dungeonMapId, room.ZoneId, room);
            waveMgr.ConsumeEntry(userId, dungeonConfigId);
            Debug.Log($"[ZoneTransitionController] Wave session started userId={userId} dungeonId={dungeonConfigId} zone={room.ZoneId} used={waveMgr.GetDailyUsedCount(userId, dungeonConfigId)} remaining={waveMgr.GetDailyRemainingCount(userId, dungeonConfigId)}");
        }

        // Thông báo client đã vào dungeon (trước khi transfer)
        NotifyDungeonEnteredClientRpc(dungeonConfigId, dungeonMapId, room.ZoneId, BuildSingleClientRpcParams(clientId));

        ExecuteTransferToRoom(clientId, room);

        WaveDungeonRuntime waveRuntime = FindAnyObjectByType<WaveDungeonRuntime>();
        if (waveRuntime != null)
        {
            waveRuntime.BeginEncounter(dungeonConfigId, dungeonMapId, room.ZoneId);
        }
        else
        {
            Debug.LogWarning($"[ZoneTransitionController] WaveDungeonRuntime not found on server. dungeonConfigId={dungeonConfigId}, map={dungeonMapId}, zone={room.ZoneId}");
        }
    }

    // Party leader yêu cầu cả tổ đội vào phó bản.
    // Server tạo 1 custom room, tra userId → clientId rồi transfer tất cả vào cùng room.
    // partyMemberUserIdsCsv: chuỗi userId ngăn cách bởi dấu phẩy, ví dụ "16,17,18"
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

        PartyDungeonRuntime partyRuntime = FindAnyObjectByType<PartyDungeonRuntime>();
        if (partyRuntime != null)
        {
            partyRuntime.BeginEncounter(dungeonConfigId, dungeonMapId, room.ZoneId);
            return;
        }

        WaveDungeonRuntime waveRuntime = FindAnyObjectByType<WaveDungeonRuntime>();
        if (waveRuntime != null)
        {
            waveRuntime.BeginEncounter(dungeonConfigId, dungeonMapId, room.ZoneId);
        }
        else
        {
            Debug.LogWarning($"[ZoneTransitionController] Dungeon runtime not found on server. dungeonConfigId={dungeonConfigId}, map={dungeonMapId}, zone={room.ZoneId}");
        }
    }

    // Client yêu cầu rời phó bản, quay về overworld map.
    // Server transfer client về map mặc định (map 0) hoặc map lưu trước đó.
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

        // Kết thúc wave session khi người chơi thoát chủ động
        string exitUserId = ZonePlayerSessionManager.Instance?.GetPlayerId(clientId);
        WaveSessionManager.Instance?.EndSession(exitUserId);
        Debug.Log($"[ZoneTransitionController] Wave session ended on exit userId={exitUserId}");

        NotifyDungeonExitedClientRpc(BuildSingleClientRpcParams(clientId));
        ExecuteTransferToRoom(clientId, targetRoom);
    }

    // Core transfer logic (server-side only)

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
        ReportReachQuestProgress(clientId, targetRoom.MapId);
    }

    private void ReportReachQuestProgress(ulong clientId, int mapId)
    {
        int playerId = 0;
        string playerIdText = ZonePlayerSessionManager.Instance?.GetPlayerId(clientId);
        if (!string.IsNullOrWhiteSpace(playerIdText))
            int.TryParse(playerIdText, out playerId);

        if (playerId <= 0 && ServerPlayerDataManager.Instance != null)
            playerId = ServerPlayerDataManager.Instance.GetUserIdFromClientId(clientId);

        if (playerId <= 0)
            return;

        QuestProgressReporter.Report(
            this,
            playerId,
            QuestProgressReporter.ProgressType.Reach,
            mapId,
            1,
            () => NotifyQuestProgressClient(clientId, "reach"));
    }

    private void NotifyQuestProgressClient(ulong clientId, string source)
    {
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            if (kvp.Value.OwnerClientId != clientId)
                continue;

            var sync = kvp.Value.GetComponent<NetworkPlayerDataSync>();
            if (sync != null)
            {
                sync.NotifyQuestProgressOnServer(source);
                return;
            }
        }
    }

    private bool CanProcessTransferRequest(ulong clientId)
    {
        if (_lastTransferTime.TryGetValue(clientId, out float last) &&
            Time.time - last < _transferCooldown)
        {
            float remaining = _transferCooldown - (Time.time - last);
            Debug.Log($"[ZoneTransitionController] Client {clientId} request too fast. Remaining cooldown={remaining:0.00}s");
            SendTransferFailedClientRpc("TRANSFER_COOLDOWN", BuildSingleClientRpcParams(clientId));
            return false;
        }

        if (_registry == null)
        {
            Debug.LogError("[ZoneTransitionController] ZoneRoomRegistry chưa khởi tạo!");
            SendTransferFailedClientRpc("TRANSFER_SYSTEM_NOT_READY", BuildSingleClientRpcParams(clientId));
            return false;
        }

        return true;
    }

    private ZoneRoom ResolveRestorableWaveRoom(WaveSessionManager.PlayerWaveSession session)
    {
        if (session == null)
            return null;

        if (_registry == null)
            return session.ZoneRoom;

        if (session.ZoneRoom != null)
            return _registry.EnsureRoomRegistered(session.ZoneRoom);

        return _registry.EnsureCustomRoomRegistered(session.MapId, session.ZoneId);
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

    // ClientRpc — gửi đến đúng 1 client (NO broadcast)

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
    private void SyncDungeonStatusClientRpc(string message, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[ZoneTransitionController] SyncDungeonStatus | message='{message}'");
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.OnDungeonRuntimeStatusUpdated(message);
    }

    [ClientRpc]
    private void SyncWaveStateClientRpc(int currentRound, int maxRounds, int remainingSeconds, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[ZoneTransitionController][CLIENT] SyncWaveStateClientRpc RECEIVED | round={currentRound}/{maxRounds} remaining={remainingSeconds}s dmInstance={(DungeonManager.Instance != null ? "OK" : "NULL")} ");
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.OnWaveStateUpdated(currentRound, maxRounds, remainingSeconds);
        else
            Debug.LogError("[ZoneTransitionController][CLIENT] DungeonManager.Instance is NULL \u2014 HUD s\u1ebd kh\u00f4ng c\u1eadp nh\u1eadt!");
    }

    [ClientRpc]
    private void ShowGlobalNotificationClientRpc(string title, string message, float autoHideSeconds, string confirmLabel, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[ZoneTransitionController] ShowGlobalNotification | title='{title}'");
        GlobalNotificationUI.Show(message, title, autoHideSeconds, confirmLabel);
    }

    [ClientRpc]
    private void BeginDungeonReturnFlowClientRpc(bool completed, int countdownSeconds, int returnMapId, string returnSceneName, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[ZoneTransitionController] BeginDungeonReturnFlow | completed={completed} countdown={countdownSeconds} returnMap={returnMapId}");
        if (DungeonManager.Instance != null)
            DungeonManager.Instance.StartCoroutine(LocalDungeonReturnFlowCoroutine(completed, countdownSeconds, returnMapId, returnSceneName));
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
        bool suppressCooldownFeedback = reason == "TRANSFER_COOLDOWN" &&
                                        ClientSceneController.ShouldSuppressTransferCooldownFeedback();
        if (suppressCooldownFeedback)
        {
            ClientSceneController.MarkTransferRequestStarted();
            LoginLoadingManager.ShowLoadingStatic();
            Debug.Log("[ZoneTransitionController] Suppressed duplicate cooldown feedback while a valid transfer is already in-flight.");
            return;
        }

        ClientSceneController.MarkTransferRequestFinished();
        LoginLoadingManager.HideLoadingStatic();
        GlobalNotificationUI.Show(MapTransferFailureMessage(reason), "Không thể chuyển map", 2.5f, "Đóng");
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    // Refresh NGO CheckObjectVisibility cho tất cả NetworkObjects liên quan đến client này.
    // Gọi sau mỗi lần thay đổi zone.
    private void RefreshVisibilityForClient(ulong movedClientId)
    {
        foreach (var filter in FindObjectsByType<NetworkVisibilityZoneFilter>(FindObjectsSortMode.None))
            filter.RefreshVisibility();
    }

    private static string MapTransferFailureMessage(string reason)
    {
        return reason switch
        {
            "TRANSFER_COOLDOWN" => "Thao tác chuyển map đang diễn ra quá nhanh. Vui lòng chờ một nhịp rồi thử lại.",
            "TRANSFER_SYSTEM_NOT_READY" => "Hệ thống chuyển map chưa sẵn sàng. Vui lòng thử lại sau.",
            "MAP_FULL" => "Map đích đang đầy. Vui lòng thử lại sau.",
            "MAP_NOT_FOUND" => "Không tìm thấy map đích.",
            "ZONE_NOT_FOUND" => "Không tìm thấy khu đích.",
            "ZONE_SWITCH_DISABLED" => "Map hiện tại không cho phép đổi khu thủ công.",
            "PRIVATE_ZONE_SERVER_ONLY" => "Khu riêng chỉ có thể được mở từ server hoặc phó bản.",
            "DUNGEON_MAP_INVALID" => "Phó bản này chưa được cấu hình hợp lệ.",
            "DUNGEON_ROOM_CREATE_FAILED" => "Không thể tạo phòng phó bản lúc này.",
            "NO_RETURN_ROOM" => "Không tìm thấy vị trí phù hợp để quay về.",
            _ => "Chuyển map thất bại. Vui lòng thử lại."
        };
    }

    private static ClientRpcParams BuildSingleClientRpcParams(ulong clientId) =>
        new()
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };

    private ClientRpcParams BuildZoneClientRpcParams(int mapId, int zoneId, out int clientCount)
    {
        clientCount = 0;
        ulong[] targetClientIds = _registry?.GetRoom(mapId, zoneId)?.GetClientSnapshot();
        if (targetClientIds == null || targetClientIds.Length == 0)
            return default;

        clientCount = targetClientIds.Length;
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = targetClientIds
            }
        };
    }

    private IEnumerator LocalDungeonReturnFlowCoroutine(bool completed, int countdownSeconds, int returnMapId, string returnSceneName)
    {
        int seconds = Mathf.Max(1, countdownSeconds);
        string prefix = completed ? "Hoàn thành! Trở về sau" : "Thất bại! Trở về sau";

        for (int remaining = seconds; remaining > 0; remaining--)
        {
            if (DungeonManager.Instance != null)
                DungeonManager.Instance.OnDungeonRuntimeStatusUpdated($"{prefix}: {remaining}s");
            yield return new WaitForSeconds(1f);
        }

        PlayerPrefs.SetInt("SelectedMapId", returnMapId);

        if (DungeonManager.Instance != null)
            DungeonManager.Instance.ExitDungeon(returnMapId);
        else
            UnityEngine.SceneManagement.SceneManager.LoadScene(string.IsNullOrWhiteSpace(returnSceneName) ? "GameScene" : returnSceneName);
    }

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
