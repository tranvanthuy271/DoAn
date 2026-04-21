---
name: doan-unity-ngo-auditor
description: "Dùng khi user nói về Unity NGO/Netcode, dedicated server, ServerRpc/ClientRpc, NetworkVariable, host-client desync, prefab mạng, spawn enemy, scene-map-zone transfer, build fail, hoặc config multiplayer của dự án DoAn. Skill này bám đúng repo hiện tại: Unity 2022.3.61f1, NGO 1.15.0, UnityTransport, Physics2D, MapWorldConfig, ServerAddressConfig, GameServerApi."
---

# DoAn Unity NGO Auditor

## Mục tiêu

Audit, fix, verify theo đúng kiến trúc đang chạy của repo này, không dùng checklist generic cho Mirror, FishNet hay mô hình NGO mặc định không tồn tại trong dự án.

Ưu tiên:

1. Fix đúng nguyên nhân gốc.
2. Bám đúng config/runtime flow đang dùng thật.
3. Verify sau sửa.
4. Báo cáo rõ nguyên nhân -> hậu quả -> cách sửa -> trạng thái.

Không hỏi lại nếu còn có thể suy luận từ code, docs, asset text, csproj, repo memory.

## Hồ sơ dự án hiện tại

- Framework mạng: Unity Netcode for GameObjects 1.15.0.
- Unity version: 2022.3.61f1.
- Transport: UnityTransport.
- Physics: Physics2D là luồng chính.
- Render pipeline: Built-in pipeline. Không thấy package URP hoặc HDRP trong manifest.
- Kiến trúc mạng: 1 Unity server process, 1 port, nhiều map và zone.
- Scene sync: không lấy `NetworkSceneManager` làm nguồn sự thật; client tự chuyển scene bằng `SceneManager` qua `ClientSceneController`.
- Runtime config: `MapWorldConfig`, `ServerAddressConfig`, API runtime bootstrap, spawn-config API.
- Backend liên quan trực tiếp: `GameServerApi`.
- Dedicated server runtime hiện tại spawn enemy qua `NetworkEnemySpawner`; không mặc định đi qua `HostSpawnConfigLoader`.
- `mapId = 0` là map thật, không phải sentinel unset. Sentinel auto-detect đúng trong repo này là `-1`.

## Source Of Truth

Luôn đọc và ưu tiên các nhóm file sau trước khi kết luận:

### 1. Môi trường và package

- `Client/ProjectSettings/ProjectVersion.txt`
- `Client/Packages/manifest.json`
- `Client/ProjectSettings/Physics2DSettings.asset`
- `Client/Assembly-CSharp.csproj`
- `GameServerApi/GameServerApi.csproj`

### 2. Config và bootstrap mạng

- `Client/Assets/Scripts/Config/ServerAddressConfig.cs`
- `Client/Assets/Scripts/Network/Shared/MapWorldConfig.cs`
- `Client/Assets/Scripts/Network/Server/MapWorldBootstrap.cs`
- `Client/Assets/Scripts/Network/Bootstrap/DedicatedServerLauncher.cs`
- `Client/Assets/Scripts/Network/Managers/NetworkManagerCustom.cs`
- `Client/Assets/Scripts/Network/Managers/NetworkPrefabRegistrar.cs`

### 3. Luồng zone, scene, visibility

- `Client/Assets/Scripts/Network/Server/ZoneRoomRegistry.cs`
- `Client/Assets/Scripts/Network/Server/ZoneConnectionApprovalV2.cs`
- `Client/Assets/Scripts/Network/Server/ZoneTransitionController.cs`
- `Client/Assets/Scripts/Network/Server/ZonePlayerSessionManager.cs`
- `Client/Assets/Scripts/Network/Client/ClientSceneController.cs`
- `Client/Assets/Scripts/Network/Shared/NetworkVisibilityZoneFilter.cs`
- `Client/Assets/Scripts/Network/Server/MapSceneManager.cs`

### 4. Player, enemy, spawn, authority

- `Client/Assets/Scripts/Network/Player/NetworkPlayerController.cs`
- `Client/Assets/Scripts/Network/Player/NetworkPlayerSpawner.cs`
- `Client/Assets/Scripts/Network/Enemy/NetworkEnemySpawner.cs`
- `Client/Assets/Scripts/Network/Enemy/HostSpawnConfigLoader.cs`
- `Client/Assets/Scripts/Network/Enemy/NetworkEnemyController.cs`
- `Client/Assets/Scripts/Network/Enemy/NetworkEnemyHealth.cs`
- `Client/Assets/Scripts/Enemy/EnemyAI.cs`

### 5. API phụ thuộc trực tiếp vào multiplayer

- `GameServerApi/Controllers/MapController.cs`
- `GameServerApi/Controllers/PlayerController.cs`
- `GameServerApi/Controllers/DungeonController.cs`

### 6. Tài liệu nội bộ repo

- `HUONG_DAN_SETUP_UNITY_SERVER_CLIENT.md`
- `HUONG_DAN_KIEN_TRUC_SERVER_CLIENT.md`
- `HUONG_DAN_MAP_SPAWN_CONFIG.md`
- `HUONG_DAN_MAP_ADDITIVE_PHYSICS_ISOLATION.md`
- `HUONG_DAN_MAP_MULTI_ZONE_NPC.md`
- `HUONG_DAN_NPC_NETCODE.md`

## Phạm vi đọc và bỏ qua

Ưu tiên đọc:

- `Client/`
- `GameServerApi/`
- root docs `.md`

Bỏ qua mặc định nếu user không nói rõ:

- `Client_clone_0/`
- `Client/Library/`, `Client/Temp/`, `Client/Logs/`
- `GameServerApi/bin/`, `GameServerApi/obj/`, `GameServerApi/artifacts/`
- thư mục build tạm, output, generated files

Nếu có file trùng giữa `Client/` và `Client_clone_0/`, coi `Client/` là nguồn sự thật trừ khi bug chỉ tái hiện trên clone.

## Invariants đặc thù của dự án

### 1. Không áp dụng tư duy generic ngoài repo

- Không đề xuất Mirror hoặc FishNet patterns.
- Không giả định dự án dùng `NetworkSceneManager`.
- Không giả định mỗi zone là một process hoặc một port riêng.
- Không tự bật lại `NetworkTransform` trên player nếu chưa kiểm tra `NetworkPlayerController` vì dự án đang dùng custom owner prediction.

### 2. Connection flow đúng của repo

Luồng đúng cần audit theo thứ tự:

`ServerAddressConfig` -> transport config -> `MapWorldBootstrap` -> `ZoneConnectionApprovalV2` -> `ZoneRoomRegistry.AssignClientToRoom()` -> `ZonePlayerSessionManager` -> `ClientSceneController` -> local player reposition.

Payload approval chuẩn là JSON UTF-8:

```json
{ "token": "...", "mapId": 0, "zoneId": 0 }
```

### 3. Scene và zone flow đúng của repo

- Chuyển map hoặc zone do client tự load additive rồi unload scene cũ.
- NetworkObject phải được move sang scene mới trước khi unload scene cũ nếu còn cần sống tiếp.
- Visibility được lọc bằng `NetworkVisibilityZoneFilter`, không audit theo kiểu full-broadcast mặc định.
- Server-only physics scene phải được coi là một phần của fix nếu bug liên quan va chạm chéo map hoặc object server-only bị sync sai scene.

### 4. Prefab và spawn flow đúng của repo

- Prefab mạng có thể được validate theo shared prefab list và cũng có thể được đăng ký runtime qua `NetworkPrefabRegistrar`.
- `ForceSamePrefabs` có ý nghĩa trong repo này; đừng kết luận thiếu prefab chỉ vì không thấy ở một chỗ.
- Dedicated server runtime hiện tại spawn enemy bằng `NetworkEnemySpawner`.
- `HostSpawnConfigLoader` là luồng config DB-driven có tồn tại, nhưng không phải mặc định của dedicated runtime.
- Nếu bug liên quan drop hoặc spawn trên dedicated server, kiểm tra `NetworkEnemySpawner` và server-side death path trước khi đụng `HostSpawnConfigLoader`.

### 5. Config API và runtime override đúng của repo

- Không tin tuyệt đối serialized localhost trong scene hoặc prefab.
- Bất kỳ component nào gọi API nên được audit xem đã dùng `ServerAddressConfig.ResolveApiUrl()` hoặc `ResolveApiRoot()` chưa.
- Nếu request HTTP = 0 hoặc server không nhận log, nghi ngờ runtime override trước khi sửa endpoint.
- `MapWorldConfig.asset` vẫn là nguồn dữ liệu khởi tạo map count trên server boot; SQL hoặc API không tự thay thế asset này nếu bootstrap chưa áp dụng runtime override phù hợp.

### 6. Build và compile audit đúng của repo

- `Assembly-CSharp.csproj` có thể dùng explicit `Compile Include` entries.
- Script mới có thể compile trong Unity nhưng chưa hiện với external build cho đến khi Unity regenerate csproj.
- Khi static compile check, phải kiểm tra csproj trước khi kết luận namespace hay symbol bị thiếu.

## Quy trình audit chuẩn

## Phase 0 - Xác định môi trường thật

Xác định rõ:

1. Unity version.
2. NGO version.
3. Built-in hay SRP.
4. Physics2D hay Physics 3D.
5. Có dedicated server path, clone project, csproj, API backend hay không.

Nếu có nhiều bản sao project trong workspace, chỉ chọn một source of truth rồi bám theo đến hết.

## Phase 1 - Đọc trước, kết luận sau

Không sửa từng file ngay khi vừa đọc lẻ tẻ. Đọc đủ luồng liên quan rồi mới kết luận.

### 1A. Network foundation

Kiểm tra:

- `NetworkObject`, `NetworkBehaviour`, `OnNetworkSpawn()`, `OnNetworkDespawn()`.
- `NetworkVariable` permission và lifecycle.
- `ServerRpc` và `ClientRpc` caller role có đúng không.
- Ownership guard: `IsOwner`, `IsServer`, `IsHost`, `RequireOwnership = false`.
- `CreatePlayerObject = false` hoặc manual spawn path nếu issue liên quan player object.

### 1B. Connection và zone lifecycle

Kiểm tra:

- Transport bind config có dùng đúng `publicIp`, `port`, `listenAddress`.
- Approval có parse đúng payload và reject đúng reason.
- Room resolve có fallback đúng map và zone.
- Session registration có khớp map, zone, token.
- Disconnect hoặc logout có reset state local và state persisted đúng không.

### 1C. Scene migration và client travel

Kiểm tra:

- additive load, move root network objects, unload scene cũ.
- duplicate EventSystem hoặc UI persistence.
- reposition local player sau teleport.
- map 0 phải được coi là map hợp lệ, không treat như unset.

### 1D. Physics2D và authority

Kiểm tra:

- `MapSceneManager` và physics scene riêng theo map.
- rigidbody authority, gravity, simulated state, collision layers.
- movement owner prediction và sync visual position.
- desync do double authority hoặc do object server-only bị sync sang client.

### 1E. Spawn, enemy, reward, config

Kiểm tra:

- API `/api/enemyspawn/{mapId}/spawns`.
- API `/api/map/{mapId}/spawn-config`.
- compatibility giữa `enemy_spawns` và `map_spawn_config`.
- mapping `enemy_id` -> prefab.
- server-side death -> drop -> despawn flow.

### 1F. API coupling

Khi bug chạm ranh giới client-server, audit luôn Unity và API cùng lúc:

- `MapController` cho runtime bootstrap và spawn config.
- `PlayerController` cho position persist hoặc reset-to-start-map.
- `DungeonController` nếu lỗi map instance hoặc room riêng.

## Phase 2 - Build check

Nếu có Unity CLI và môi trường cho phép, ưu tiên batchmode compile hoặc build để lấy lỗi thật.

Nếu không có Unity CLI:

1. Static compile check ở `Client/Assembly-CSharp.csproj`.
2. Static compile check ở `GameServerApi/GameServerApi.csproj` nếu bug có dính API.
3. Search symbol usages, RPC call sites, and preprocessor guards.
4. Kiểm tra script mới đã được csproj include chưa.

## Phase 3 - Fix

Thứ tự fix bắt buộc:

1. Compile error.
2. Startup fail hoặc bind fail.
3. Approval, connect, disconnect fail.
4. Crash hoặc despawn/scene migration exception.
5. Authority bug hoặc desync.
6. Spawn, drop, runtime config bug.
7. Performance và cleanup.

Nguyên tắc fix:

- Fix tận gốc, không vá hờ.
- Không đổi public flow nếu chưa chứng minh được luồng cũ sai.
- Không sửa clone project nếu bug nằm ở project chính.
- Không hallucinate inspector data, prefab state, hoặc asset references không thấy trong repo.
- Nếu phải để manual fix trong Editor, ghi rõ asset, component, field, và thao tác cần làm.

## Phase 4 - Verify

Sau mỗi vòng fix:

1. Re-scan file vừa sửa.
2. Cross-check các class gọi tới nó hoặc bị nó gọi tới.
3. Re-check RPC direction và ownership guards.
4. Re-check API URL resolution nếu có request mạng.
5. Re-run build hoặc static verification phù hợp.

Lặp tối đa 3 vòng audit -> fix -> verify nếu vẫn còn lỗi.

## Các bẫy đặc thù phải nhớ

- `mapId = 0` là map thật. Dùng `-1` mới là auto-detect.
- Client đang trong session mà load lại `GameScene` không đồng nghĩa login mới; đừng tự khởi động client lần hai.
- Localhost serialized trong Inspector có thể bị runtime override, nên đừng sửa nhầm endpoint chỉ vì nhìn thấy giá trị cũ.
- `MapWorldConfig.asset` quyết định số map và topology khởi tạo trên server boot.
- Dedicated server không đáng tin cho client-only death flow; drop và despawn cần được sở hữu bởi server path.
- Thứ tự tham số `UnityTransport.SetConnectionData(ip, port, listenAddress)` rất quan trọng; đảo `publicIp` và `listenAddress` có thể làm bind fail.
- Validation runtime prefab có thể false-negative nếu prefab nằm trong shared prefab list asset.

## Khi nào được hỏi user

Chỉ hỏi nếu cả code, docs, asset text, csproj, và repo memory đều không đủ để suy ra.

Ưu tiên hơn việc hỏi:

- đọc thêm file liên quan,
- search usages,
- đọc docs nội bộ,
- suy luận từ runtime flow hiện có,
- nêu rõ assumption nếu vẫn cần tiếp tục.

Không tạo yêu cầu mơ hồ kiểu "thiếu config thì hỏi user". Nếu thiếu dữ liệu editor-only, hãy báo manual checklist chính xác thay vì đoán.

## Mẫu báo cáo bắt buộc

### Bảng 1 - Tổng quan

| Mục | Số lượng |
|-----|----------|
| File được audit | X |
| Lỗi compile | X |
| Lỗi runtime hoặc logic | X |
| Cảnh báo | X |
| Đã fix | X |
| Cần fix thủ công | X |
| Trạng thái verify | Pass / Fail / Warn |

### Bảng 2 - Chi tiết lỗi

| # | Layer | File hoặc Asset | Dòng hoặc vị trí | Loại lỗi | Nguyên nhân gốc | Hậu quả nếu không fix | Cách đã sửa | Trạng thái |
|---|-------|-----------------|------------------|----------|-----------------|----------------------|-------------|-----------|
| 1 | Unity | Client/... | 47 | Logic Bug | ... | ... | ... | Fixed |

Loại lỗi hợp lệ:

- Compile Error
- Runtime Crash
- Logic Bug
- Desync
- Authority Bug
- Config Bug
- API Contract Bug
- Performance
- Warning

### Bảng 3 - Việc cần làm thủ công

| # | Vấn đề | Lý do không auto-fix | Hướng dẫn fix thủ công |
|---|--------|----------------------|------------------------|
| 1 | ... | ... | ... |

### Kết thúc báo cáo

Luôn ghi thêm:

- danh sách file đã thay đổi,
- assumption đã dùng,
- cách verify đã chạy,
- test case khuyến nghị tiếp theo.