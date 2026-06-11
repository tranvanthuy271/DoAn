# Tổng hợp luồng dự án và đánh giá kỹ thuật

Ngày rà soát: 2026-06-10  
Phạm vi: `Client/Assets/Scripts`, `GameServerApi`, `Scripts`. Bỏ qua `Client_clone_0` vì là bản clone/ParrelSync và sẽ làm trùng kết quả.

Phụ lục tra cứu tự động: [`docs/CS_FUNCTION_INDEX.md`](CS_FUNCTION_INDEX.md) chứa 418 file `.cs` với path, số dòng, class/type và hàm/RPC tương ứng.

## 1. Kết luận nhanh

Dự án đang có kiến trúc MMO/action RPG 2D khá đầy đủ: Unity client, Netcode for GameObjects cho realtime, ASP.NET Core API cho account/player/config, EF Core MySQL cho dữ liệu, SignalR cho chat/party. `GameServerApi` build được: `dotnet build GameServerApi/GameServerApi.csproj` thành công, 0 warning, 0 error.

Đánh giá ổn định hiện tại: **chưa nên coi là production-ready**, nhưng nền tảng đã chạy được theo hướng prototype/đồ án mở rộng. Điểm mạnh là đã tách API, netcode server, spawn config DB, gene/skill/inventory/dungeon tương đối rõ. Điểm rủi ro lớn là có nhiều luồng cũ và luồng mới tồn tại song song, nhiều controller quá lớn, một số parse JSON thủ công, cấu hình public IP/HTTP hardcode, và Unity client chưa được compile/test bằng Unity Editor trong lần rà này.

## 2. Công nghệ/công cụ

| Mảng | Công cụ | File/dòng |
|---|---|---|
| Unity client | Unity 2D, UGUI, TextMeshPro, Timeline, Visual Scripting | `Client/Packages/manifest.json` |
| Netcode | `com.unity.netcode.gameobjects` 1.15.0, Unity Transport | `Client/Packages/manifest.json`, `Client/Assets/Scripts/Network/*` |
| Backend API | ASP.NET Core `.NET 9` | `GameServerApi/GameServerApi.csproj` |
| Database | EF Core 9 + Pomelo MySQL | `GameServerApi/GameServerApi.csproj`, `GameServerApi/Program.cs:86` |
| Auth | JWT Bearer + BCrypt + Zone API Key | `GameServerApi/Program.cs:114`, `GameServerApi/Services/AuthService.cs:12`, `GameServerApi/Auth/ZoneApiKeyAuthenticationHandler.cs:10` |
| Realtime xã hội | SignalR chat/party | `GameServerApi/Program.cs:30`, `GameServerApi/Program.cs:338`, `GameServerApi/Hubs/ChatHub.cs:16`, `GameServerApi/Hubs/PartyHub.cs:13` |
| Config client | `server_config.json` + `ServerAddressConfig.asset` | `Client/Assets/server_config.json`, `Client/Assets/Scripts/Config/ServerAddressConfig.cs:10` |
| Docker/deploy | `docker-compose.yml`, `deploy.sh` | root repo |

## 3. Luồng tổng thể

1. API khởi động trong `GameServerApi/Program.cs:17`.
2. Kestrel listen `0.0.0.0:5000` nếu không override ở `Program.cs:22`.
3. API đăng ký controller, SignalR, CORS, memory cache, rate limit, auth, EF MySQL ở `Program.cs:26`, `Program.cs:31`, `Program.cs:41`, `Program.cs:61`, `Program.cs:66`, `Program.cs:149`.
4. Startup tự `EnsureCreated`, seed admin, repair một số dữ liệu cũ, seed dungeon mặc định nếu thiếu ở `Program.cs:196`.
5. Unity client đọc API/game server address qua `ServerAddressConfig` và `server_config.json`; API wrapper chính nằm ở `APIClient` từ `Client/Assets/Scripts/Services/Api/APIClient.cs:361`.
6. Người chơi login/register qua `APIClient.Login`/`Register` ở `APIClient.cs:532` và `APIClient.cs:634`, gọi `AuthController.Register`/`Login` ở `GameServerApi/Controllers/AuthController.cs:26` và `AuthController.cs:68`.
7. Sau khi có JWT, client load player/gene/skill/inventory qua các controller `PlayerController`, `GeneController`, `ItemController`, `QuestController`, `DungeonController`.
8. Client vào netcode server bằng Unity Transport/NGO. Luồng mới dùng `ZoneConnectionApproval` và `ZonePlayerSessionManager`; luồng legacy vẫn còn `ServerConnectionApproval`, `NetworkPlayerSpawner`, `NetworkEnemySpawner`.

## 4. Luồng netcode

### 4.1 Khởi động server

Có hai kiểu khởi động:

| Kiểu | File/hàm |
|---|---|
| Dedicated server launcher tự start API exe rồi start Netcode server | `Client/Assets/Scripts/Network/Bootstrap/DedicatedServerLauncher.cs:63`, `StartAPIServer` ở dòng 82, `StartNetcodeServer` ở dòng 133 |
| Bootstrap/server scene start network trực tiếp | `Client/Assets/Scripts/Network/Bootstrap/ServerBootstrap.cs:8`, `DedicatedServerStarter.cs:7` |

Rủi ro: `DedicatedServerLauncher` đang có trách nhiệm start cả API process lẫn Netcode server (`DedicatedServerLauncher.cs:73` và `:78`). Khi deploy thật nên tách process lifecycle: API chạy bằng systemd/container, Unity dedicated server chỉ lo netcode.

### 4.2 Connection approval

Luồng mới:

| Vai trò | File/hàm |
|---|---|
| Duyệt payload kết nối, parse JWT/map/zone/gene slot, gán room | `Client/Assets/Scripts/Network/Server/ZoneConnectionApproval.cs:16`, `HandleApproval` dòng 34 |
| Quản lý session player theo clientId/userId/map/zone | `Client/Assets/Scripts/Network/Server/ZonePlayerSessionManager.cs` |
| Registry room/zone | `Client/Assets/Scripts/Network/Server/ZoneRoomRegistry.cs`, `ZoneRoom.cs` |
| Chuyển zone/map bằng ServerRpc | `Client/Assets/Scripts/Network/Server/ZoneTransitionController.cs` |

Luồng cũ vẫn còn:

| Vai trò | File/hàm |
|---|---|
| Approval legacy approve connection rồi chờ client gửi user_id | `Client/Assets/Scripts/Network/Auth/ServerConnectionApproval.cs:7`, `ApprovalCheck` dòng 132 |
| Controller wrapper start host/client/server | `Client/Assets/Scripts/Network/Managers/NetworkManagerController.cs:76`, `:89`, `:99` |
| Spawn player legacy | `Client/Assets/Scripts/Network/Player/NetworkPlayerSpawner.cs` |

Đánh giá: thiết kế room/zone mới đúng hướng hơn legacy, vì tránh broadcast toàn map và có `RoomBroadcast` lọc ClientRpc theo zone ở `Client/Assets/Scripts/Map/RoomBroadcast.cs:13`. Nhưng codebase còn cả hai approval path nên dễ bị conflict nếu scene gắn nhầm component. Comment trong `NetworkManagerController.cs:33` cũng cho thấy đã từng có conflict callback.

### 4.3 Đồng bộ player

| Chức năng | File/hàm |
|---|---|
| Movement input/control network | `Client/Assets/Scripts/Network/Player/NetworkPlayerController.cs` |
| Sync data player, HP/MP/stat/skill | `Client/Assets/Scripts/Network/Player/NetworkPlayerDataSync.cs` |
| Sync health riêng | `Client/Assets/Scripts/Network/Player/NetworkPlayerHealth.cs` |
| Update vị trí về server/API | `Client/Assets/Scripts/Network/Player/PlayerPositionUpdater.cs` |
| Save queue sang API | `Client/Assets/Scripts/Services/Player/PlayerDataSaveService.cs:7`, `QueueSave` dòng 46, `BatchSaveToAPI` dòng 66 |

Đánh giá: có đủ cơ chế NetworkVariable/ServerRpc/API sync, nhưng cần test kỹ race condition giữa save định kỳ, position updater, và zone transition.

## 5. Luồng map/zone/generation

| Chức năng | File/hàm |
|---|---|
| Load map config theo scene/mapId từ API | `Client/Assets/Scripts/Map/MapManager.cs:103`, `FetchMapConfigById` dòng 123, `FetchMapConfigByScene` dòng 145 |
| Portal travel server-side/API | `GameServerApi/Controllers/MapController.cs:276` |
| Resolve map theo scene | `MapController.cs:392` |
| Portal theo direction | `MapController.cs:415` |
| Runtime bootstrap map/world | `MapController.cs:144` |
| Spawn config per map | `MapController.cs:519`, upsert ở `MapController.cs:692` |
| Trigger chuyển zone in-game | `Client/Assets/Scripts/Map/ZoneTransitionTrigger.cs:48` |
| Button chuyển map/portal | `Client/Assets/Scripts/Map/MapTransitionButton.cs:14` |
| Sinh collider từ sprite | `Client/Assets/Scripts/Map/MapColliderGenerator.cs:41` |
| Bake collider server-side | `Client/Assets/Scripts/Editor/ServerGroundColliderDatabaseBaker.cs` |
| Physics query server theo map | `Client/Assets/Scripts/Network/Shared/MapPhysicsQuery2D.cs` |

"Generation" trong dự án hiện không phải procedural map generation đầy đủ. Cái đang có là:

1. Sinh/cấu hình collider từ sprite terrain (`MapColliderGenerator.cs:41`).
2. Bake ground collider database để server dùng cho snap-to-ground và physics proxy.
3. Sinh enemy theo spawn config từ DB/API.
4. Sinh dungeon wave runtime theo config.

Đánh giá: hướng này ổn cho game 2D map cố định. Nếu muốn procedural map thật, hiện chưa thấy pipeline generator map tile/room hoàn chỉnh.

## 6. Luồng AI quái, spawn, boss

### 6.1 Spawn quái thường

Luồng mới ưu tiên DB spawn config:

1. `HostSpawnConfigLoader.OnNetworkSpawn` chạy server-side ở `Client/Assets/Scripts/Network/Enemy/HostSpawnConfigLoader.cs:75`.
2. Gọi `LoadAndApplyConfig` ở dòng 102 để fetch `/api/map/{mapId}/spawn-config`.
3. API trả config từ `MapController.GetSpawnConfig` ở `GameServerApi/Controllers/MapController.cs:519`.
4. Loader validate spawn/drop/skill, instantiate prefab, move sang physics scene đúng map, apply visibility, apply stat override/drop/skill.
5. Nếu lỗi/rỗng có fallback sang `NetworkEnemySpawner` ở `HostSpawnConfigLoader.cs:527`.

Luồng legacy:

1. `NetworkEnemySpawner.OnNetworkSpawn` ở `Client/Assets/Scripts/Network/Enemy/NetworkEnemySpawner.cs:62`.
2. `LoadAndSpawnEnemies` ở dòng 105, `LoadAndSpawnEnemiesForMap` dòng 204.
3. Spawn từng điểm bằng `SpawnEnemyAtPoint` dòng 269.
4. Respawn loop ở `CheckRespawnLoop` dòng 408.

Đánh giá: có xử lý tránh double spawn qua `HostSpawnConfigLoader.IsMapClaimed` và skip dungeon runtime. Đây là điểm tốt. Nhưng vì vẫn giữ fallback lớn, nên cần checklist scene để đảm bảo không gắn cả hai theo cách gây spawn trùng.

### 6.2 AI quái

| Chức năng | File/hàm |
|---|---|
| AI chính: tìm player, patrol/chase/attack/projectile/fly/ground fail-safe | `Client/Assets/Scripts/Enemy/EnemyAI.cs:10` |
| Init component | `EnemyAI.cs:148`, `Start` dòng 196 |
| Vòng AI | `EnemyAI.cs:418` |
| Melee | `StartMeleeAttack` dòng 683, `MeleeHitCoroutine` dòng 696 |
| Patrol | `PatrolLoop` dòng 720 |
| Projectile skill DB | `UseSkillCoroutine` dòng 1283, `TrySpawnProjectileSkill` dòng 1466 |
| Damage target | `ApplyDamageToTarget` dòng 1430 |
| Snap/failsafe ground | `TrySnapToGround` dòng 1926, `MaintainGroundFailSafe` dòng 1961 |
| AI patrol cũ/nâng cao khác | `Client/Assets/Scripts/Enemy/MobPatrolAI.cs:22`, state machine dòng 110-160 |
| Skill config per enemy | `Client/Assets/Scripts/Enemy/EnemySkillSet.cs:39`, `TryGetReadySkill` dòng 74 |
| Stat override từ DB | `Client/Assets/Scripts/Enemy/EnemyStatOverride.cs:36` |
| Health/die/EXP/drop | `Client/Assets/Scripts/Network/Enemy/NetworkEnemyHealth.cs:9`, damage RPC dòng 169, death dòng 281 |

Đánh giá: AI có nhiều case thật sự cần cho game 2D networked: bay, ground, projectile, map visibility, ground snap. Nhưng `EnemyAI.cs` đã hơn 2000 dòng, quá lớn; khi fix bug sẽ khó kiểm soát. Nên tách sau: target acquisition, movement, combat, projectile, ground snap.

### 6.3 Boss

| Chức năng | File/hàm |
|---|---|
| State machine boss | `Client/Assets/Scripts/Boss/BossController.cs:10`, `RunStateMachine` dòng 158 |
| Chase/fly/jump/ground | `BossController.cs:199`, `:218`, `:233`, `:241` |
| Normal attack | `BossController.cs:282`, coroutine dòng 290 |
| Fireball rain | `BossController.cs:328` |
| Lightning strike | `BossController.cs:376` |
| Stealth/dodge | `BossController.cs:425`, `TryDodge` dòng 455 |
| Damage hook/return damage/regen | `BossController.cs:486`, `:499`, `:525`, `:578` |
| Network HP/death/EXP | `Client/Assets/Scripts/Boss/NetworkBossHealth.cs:15`, `TakeDamageServerRpc` dòng 89, `HandleDeath` dòng 136 |
| Network animation/scale/alpha | `Client/Assets/Scripts/Boss/NetworkBossController.cs:18` |

Đánh giá: boss flow đã có state machine và network health riêng, ổn cho prototype. Cần đảm bảo boss config DB (`GameServerApi/Models/Entities/BossConfig.cs`) và dungeon config không lệch với prefab/skill key.

## 7. Luồng gene/skill/player progression

### 7.1 Tạo và chọn gene

| Chức năng | File/hàm |
|---|---|
| Scene chọn gene | `Client/Assets/Scripts/UI/SelectGene/SelectGeneController.cs:10` |
| Load slot gene | `SelectGeneController.cs:96`, API wrapper `APIClient.LoadGeneSlots` dòng 761 |
| Tạo player slot 1 | `APIClient.CreatePlayer` dòng 708, `GameServerApi/Controllers/PlayerController.cs` |
| Tạo player slot 2 | `APIClient.CreatePlayer2` dòng 780, API `PlayerController.CreatePlayer2` dòng 3608 |
| Lấy slot gene | `PlayerController.GetGeneSlots` dòng 3524 |
| Lấy data/skill slot 2 | `PlayerController.GetPlayer2Data` dòng 3699, `GetPlayer2Skills` dòng 3784 |

### 7.2 Upgrade, secondary, hybrid, ultimate

| Chức năng | File/hàm |
|---|---|
| Config gene | `GameServerApi/Controllers/GeneController.cs:30` |
| Upgrade gene chính | `GeneController.cs:90` |
| List secondary gene | `GeneController.cs:283` |
| Select secondary gene | `GeneController.cs:319` |
| Multi config | `GeneController.cs:373` |
| Upgrade secondary | `GeneController.cs:422` |
| Hybrid config | `GeneController.cs:566` |
| Fuse hybrid | `GeneController.cs:643` |
| Ultimate config | `GeneController.cs:821` |
| Ultimate service | `GameServerApi/Models/Services/GeneUltimateService.cs:9`, `TryAccumulateAndActivate` dòng 19 |
| Stat final | `GameServerApi/Models/Services/StatCalculator.cs:32` |
| UI upgrade gene | `Client/Assets/Scripts/Inventory/UI/GeneUpgradePanel.cs` |
| UI secondary upgrade/select | `SecondaryGeneUpgradePanel.cs`, `SecondaryGeneSelectPanel.cs` |
| UI hybrid fusion | `Client/Assets/Scripts/Inventory/UI/HybridFusionPanel.cs:29` |

Đánh giá: nghiệp vụ gene khá phong phú và đã tách một phần sang controller/service/config entity. Tuy nhiên `GeneController` vẫn xử lý nhiều nghiệp vụ trực tiếp, nên về lâu dài nên tách service layer cho upgrade, secondary, hybrid, ultimate.

### 7.3 Skill runtime

| Chức năng | File/hàm |
|---|---|
| Hotbar/use skill | `Client/Assets/Scripts/Player/Combat/PlayerSkillManager.cs:9`, `UseSkill` dòng 468 |
| Skill ServerRpc | `PlayerSkillManager.cs:718`, `:733`, `:767` |
| Projectile spawn | `PlayerSkillManager.cs:1063`, `:1070` |
| Load skill stat từ server | `Client/Assets/Scripts/Player/Skills/SkillRuntimeLoader.cs:119`, `ApplySkillStats` dòng 166 |
| Hybrid base RPC | `Client/Assets/Scripts/Player/Skills/Hybrid/HybridSkillBase.cs:77`, ClientRpc dòng 84 |
| Hybrid projectile examples | `HybridWaterWoodVenomSkill.cs:52`, `HybridMetalWindBarrageSkill.cs:52`, `HybridFireEarthLavaAuraSkill.cs:64` |

Đánh giá: hệ thống skill dùng cả ScriptableObject/Inspector và dữ liệu runtime từ API. Đây là hướng linh hoạt, nhưng cần kiểm tra mapping `skill_code` giữa DB và prefab vì `SkillRuntimeLoader` đã phải có alias/fallback.

## 8. Inventory, item, quest, dungeon, social

| Mảng | File/hàm chính |
|---|---|
| Inventory network bridge | `Client/Assets/Scripts/Inventory/Network/InventoryNetworkBridge.cs` |
| Network inventory | `Client/Assets/Scripts/Inventory/Network/NetworkInventory.cs` |
| Item pickup | `Client/Assets/Scripts/Inventory/Pickup/ItemPickup.cs:8`, click RPC dòng 275, auto pickup RPC dòng 309 |
| Item template manager | `Client/Assets/Scripts/Inventory/Managers/ItemTemplateManager.cs` |
| Upgrade equipment | `GameServerApi/Controllers/UpgradeController.cs`, UI `UpgradePanel.cs` |
| Quest API | `GameServerApi/Controllers/QuestController.cs:20`, accept dòng 116, progress dòng 186, complete dòng 254 |
| Quest client | `Client/Assets/Scripts/Quest/QuestManager.cs`, `QuestProgressReporter.cs:20` |
| Dungeon API | `GameServerApi/Controllers/DungeonController.cs:13`, config dòng 310, enter wave dòng 477, update/end session dòng 602/624 |
| Dungeon runtime | `Client/Assets/Scripts/Dungeon/Runtime/WaveDungeonRuntime.cs`, `PartyDungeonRuntime.cs`, `BaseDungeonInstance.cs` |
| Dungeon network | `Client/Assets/Scripts/Network/Dungeon/DungeonManager.cs`, `DungeonNetworkBridge.cs`, `WaveSessionManager.cs` |
| Chat | `GameServerApi/Hubs/ChatHub.cs:16`, client `ChatManager.cs:10`, `SignalRClient.cs:15` |
| Party | `GameServerApi/Hubs/PartyHub.cs:13`, client `PartyManager.cs:7` |
| Leaderboard | `GameServerApi/Controllers/LeaderboardController.cs:16`, client `LeaderboardService.cs` |
| NPC | `GameServerApi/Controllers/NpcController.cs`, `NpcActionController.cs`, client `NpcServerManager.cs`, `NpcInteraction.cs` |

## 9. Những điểm đang ổn

1. Backend build sạch, không warning/error.
2. API có JWT, Zone API key, rate limit login, BCrypt password.
3. Có phân tầng dữ liệu: entity/config/cache/controller, dù service layer chưa đều.
4. Netcode đã tính đến map/zone, physics scene riêng, visibility filter, room broadcast.
5. Spawn quái đã chuyển dần sang DB-driven config, có fallback và anti double-spawn.
6. Gene/skill/inventory/dungeon/social đã có đủ luồng end-to-end.

## 10. Rủi ro cần xử lý

| Mức | Vấn đề | Vì sao quan trọng |
|---|---|---|
| Cao | Tồn tại nhiều luồng legacy song song: `ServerConnectionApproval` vs `ZoneConnectionApproval`, `NetworkEnemySpawner` vs `HostSpawnConfigLoader`, root `Scripts/Network` vs `Client/Assets/Scripts/Network` | Dễ gắn nhầm scene/component và sinh lỗi khó debug |
| Cao | `PlayerController`, `EnemyAI`, `InventoryNetworkBridge` rất lớn | Khó review, test, sửa bug; dễ regress |
| Cao | `Client/Assets/server_config.json` đang hardcode `http://98.70.26.19:5000` và port `7777` | Dễ leak môi trường thật, thiếu HTTPS, khó chuyển dev/prod |
| Trung bình | Một số parse JSON thủ công trong client như `APIClient.ParseUserIdFromJWT` dòng 454 và login fallback dòng 574 | Dễ sai khi response format đổi |
| Trung bình | Terminal cho thấy comment tiếng Việt bị mojibake ở nhiều file | Rủi ro maintainability/encoding, không ảnh hưởng runtime trực tiếp nhưng gây khó đọc |
| Trung bình | `Program.cs` tự sửa/seed DB khi startup | Tiện dev nhưng production nên migrate/seed có kiểm soát |
| Trung bình | Chưa chạy Unity compile/test trong đợt rà này | API build sạch chưa đảm bảo client không lỗi compile/runtime |

## 11. Đề xuất ưu tiên

1. Chốt một kiến trúc netcode chính: ưu tiên `ZoneConnectionApproval` + `ZonePlayerSessionManager` + `HostSpawnConfigLoader`, rồi đánh dấu hoặc gỡ legacy khỏi scene.
2. Tách `EnemyAI.cs` thành module nhỏ: target, movement, combat, projectile, ground snap.
3. Tách nghiệp vụ `GeneController` và `PlayerController` sang service để test được bằng unit/integration test.
4. Chuẩn hóa config môi trường: không commit IP prod trực tiếp, dùng `server_config.example.json` + runtime override/env.
5. Bổ sung test API cho auth, map spawn config, gene upgrade/fuse, dungeon wave enter/end.
6. Chạy Unity batchmode compile/test nếu máy có Unity Editor: client compile mới xác nhận toàn bộ `.cs` ổn.

## 12. Kết quả kiểm tra

Đã chạy:

```powershell
dotnet build GameServerApi/GameServerApi.csproj
```

Kết quả: build thành công, 0 warning, 0 error.

Chưa chạy được trong lần rà này:

```powershell
Unity.exe -batchmode -projectPath Client -runTests
```

Lý do: chưa xác định Unity Editor executable trong terminal. Nếu cần xác minh client, bước này nên chạy tiếp.

