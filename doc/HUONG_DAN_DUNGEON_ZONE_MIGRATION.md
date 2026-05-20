# Chuyển đổi Phó Bản: Multi-Host → Zone-Based

## Tổng quan

Phó bản (dungeon) đã được chuyển từ kiến trúc **multi-host** (Shutdown + StartHost/StartClient) sang kiến trúc **zone-based** (cùng server, dùng ZoneTransitionController — giống chuyển map thường).

### Trước đây (Multi-Host)
```
Client → DungeonNetworkBridge.RequestDungeonEntryServerRpc
Host chính → kiểm tra DB session → gửi DungeonCommandClientRpc
Client → Shutdown() → LoadScene → StartHost() hoặc StartClient(ip:port)
```
**Vấn đề:** disconnect/reconnect, mất state, phức tạp, cần quản lý session DB.

### Bây giờ (Zone-Based)
```
Client → ZoneTransitionController.RequestDungeonEntryServerRpc(mapId, configId)
Server → CreateCustomRoom(dungeonMapId) → ExecuteTransferToRoom(clientId, room)
Client → nhận TeleportToZoneClientRpc → load scene additive (không disconnect)
```
**Ưu điểm:** instant transfer, giữ nguyên connection, dùng chung hạ tầng zone, tự cleanup khi empty.

---

## Các file đã thay đổi

### 1. `ZoneTransitionController.cs` — Thêm 3 ServerRpc mới

| RPC | Mô tả |
|-----|-------|
| `RequestDungeonEntryServerRpc(mapId, configId)` | Solo: tạo custom room trên dungeon map, transfer client |
| `RequestPartyDungeonEntryServerRpc(mapId, configId, userIdsCsv)` | Party: tạo 1 room, resolve userId→clientId, transfer tất cả |
| `RequestDungeonExitServerRpc(returnMapId)` | Exit: transfer client về overworld (FindLeastLoadedZone) |

Thêm 2 ClientRpc thông báo trạng thái:
- `NotifyDungeonEnteredClientRpc` → cập nhật DungeonManager state
- `NotifyDungeonExitedClientRpc` → reset DungeonManager state

### 2. `DungeonManager.cs` — Viết lại hoàn toàn (~160 dòng, trước đây ~500+)

**Đã xóa:**
- `DoShutdownAndStartHost()`, `DoShutdownAndStartClient()` — không còn Shutdown
- `ExecuteDungeonCommand()`, `FetchConfigThenExecute()` — không còn nhận lệnh từ bridge
- `RegisterMultiSession()` — không còn session DB
- Tất cả HTTP helpers (`CreateDungeonSessionDirect`, `EndDungeonSessionDirect`, `LeaveDungeonSessionDirect`)
- `OnSoloDungeonApproved()`, `OnMultiSessionReady()`, `OnClientRequestedMultiHost()`
- Properties: `ActiveSessionId`, `IsHostingDungeon`

**Giữ lại / thêm mới:**
- `EnterDungeon(DungeonConfigData)` → gọi `RequestDungeonEntryServerRpc`
- `EnterPartyDungeon(DungeonConfigData, string[] userIds)` → gọi `RequestPartyDungeonEntryServerRpc`
- `ExitDungeon(int returnMapId)` → gọi `RequestDungeonExitServerRpc`
- `OnZoneDungeonEntered()` / `OnZoneDungeonExited()` — callbacks từ ClientRpc
- Events: `OnDungeonEntered`, `OnDungeonExited`, `OnDungeonStatusMessage`
- State: `IsInDungeon`, `ActiveDungeonId`, `ActiveDungeonMapId`

### 3. `DungeonNetworkBridge.cs` — Xóa toàn bộ code

File giữ lại dạng comment (tránh lỗi .meta Unity), toàn bộ code đã xóa:
- `RequestDungeonEntryServerRpc`, `HandleDungeonEntryOnServer`
- `DungeonCommandClientRpc`, `DungeonSoloReadyClientRpc`, `DungeonMultiSessionReadyClientRpc`
- Session HTTP helpers, `KickClient`, `SendCommandToClient`
- Enum `DungeonCommand` (StartSoloHost, JoinHost)

### 4. `BaseDungeonInstance.cs` — Cập nhật `LocalReturnCountdownCoroutine`

- `DungeonManager.Instance.ExitDungeon(returnMapId)` thay vì `ExitDungeon(returnSceneName)`
- Bỏ `MapManager.ResetRuntimeState()` và `ClientSceneController.ResetZoneState()` (zone transition tự xử lý)

### 5. `DungeonNpcMenuUI.cs` — Cập nhật `OnConfirmJoinClicked()`

- **Solo:** Giữ nguyên `DungeonManager.Instance.EnterDungeon(config)` (nội bộ đã chuyển sang zone RPC)
- **Party:** Thay `partyManager.StartPartyDungeon()` (SignalR) → `DungeonManager.Instance.EnterPartyDungeon(config, memberUserIds)` (trực tiếp ServerRpc)

### 6. `PartyManager.cs` — Cập nhật handler `PartyDungeonRequested`

- Bỏ `StartCoroutine(RequestDungeonEntryCoroutine(payload))` — transfer do server xử lý trực tiếp
- `PartyDungeonRequested` event vẫn fire (cho UI) nhưng không trigger enter nữa

### 7. `GameServerApi/Program.cs` — DB auto-repair

- Thêm SQL tự đồng bộ `map_config.scene_name` từ `dungeon_config.scene_name` (fix collation mismatch với COLLATE)
- Sửa dungeon seed map_ids: 100,101 → 110,111

---

## Kiến trúc mới — Flow diagram

### Solo Dungeon
```
┌─────────┐    RequestDungeonEntryServerRpc     ┌──────────────────────────┐
│  Client  │ ──────────────────────────────────→ │  ZoneTransitionController │
│ (UI/Mgr) │                                    │      (server-side)        │
└─────────┘                                     └──────────────────────────┘
                                                          │
                                                 1. Validate map (InstanceOnly)
                                                 2. CreateCustomRoom(mapId)
                                                 3. NotifyDungeonEnteredClientRpc
                                                 4. ExecuteTransferToRoom
                                                          │
    ┌─────────┐   TeleportToZoneClientRpc       ┌─────────┴──────────┐
    │  Client  │ ←────────────────────────────── │  ZoneRoomRegistry   │
    │ (update) │                                 │  (custom room -1)   │
    └─────────┘                                  └────────────────────┘
```

### Party Dungeon
```
Leader → RequestPartyDungeonEntryServerRpc(mapId, configId, "16,17,18")
Server:
  1. Validate map
  2. CreateCustomRoom(mapId, maxPlayers=partySize)
  3. Resolve userIds → clientIds via ZonePlayerSessionManager
  4. NotifyDungeonEnteredClientRpc → mỗi member
  5. ExecuteTransferToRoom → mỗi member (cùng 1 room)
```

### Exit Dungeon
```
Client → RequestDungeonExitServerRpc(returnMapId)
Server:
  1. FindLeastLoadedZone(returnMapId) hoặc fallback map 0
  2. NotifyDungeonExitedClientRpc
  3. ExecuteTransferToRoom → overworld
  4. Custom room tự cleanup khi empty (ZoneRoomRegistry auto-cleanup)
```

---

## Lưu ý khi test

1. **Solo dungeon:** Vào NPC phó bản → chọn phó bản solo → Confirm → phải chuyển scene mà không disconnect
2. **Party dungeon:** Tạo party → leader vào NPC phó bản → chọn phó bản multi → Confirm → tất cả members phải được transfer vào cùng room
3. **Exit dungeon:** Khi countdown hết (hoàn thành hoặc thất bại) → phải quay về overworld đúng map
4. **Custom room cleanup:** Khi tất cả player rời dungeon room → room phải tự xóa
5. **Interest management:** Player trong dungeon không thấy player ở overworld (NetworkVisibilityZoneFilter vẫn hoạt động)
