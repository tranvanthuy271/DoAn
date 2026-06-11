# Full Project Flow

Ngay cap nhat: 2026-06-11

Tai lieu nay mo ta luong chay chinh cua du an va chi ra file `.cs` dang thuc hien tung viec. Pham vi doc code: `Client/Assets/Scripts` va `GameServerApi`. Thu muc `Client_clone_0` la clone ParrelSync nen khong tinh la source chinh.

## 1. Tong quan kien truc

Du an gom 3 lop chinh:

| Lop | Vai tro | File chinh |
|---|---|---|
| Unity Client | UI, gameplay 2D, Netcode client/server runtime, dungeon runtime | `Client/Assets/Scripts/**` |
| ASP.NET Core API | Account, JWT, player data, config, dungeon, inventory, quest, leaderboard | `GameServerApi/Program.cs`, `GameServerApi/Controllers/**` |
| Realtime hub | Chat va to doi qua SignalR | `GameServerApi/Hubs/ChatHub.cs`, `GameServerApi/Hubs/PartyHub.cs` |

Luong runtime tong quat:

1. API start trong `GameServerApi/Program.cs`.
2. API dang ky JWT, controller, SignalR hubs, EF Core MySQL.
3. Unity doc dia chi API/game server tu `ServerAddressConfig` va `server_config.json`.
4. Nguoi choi login/register qua REST API.
5. Client luu `JWT_TOKEN`, `USER_ID`, `USERNAME` vao `PlayerPrefs`.
6. Client dung JWT cho request HTTP, SignalR hub, va payload Netcode connection.
7. Game server/host xac thuc connection, tao player session, spawn player/enemy/NPC.
8. Dungeon/party/map transition chay qua `ZoneTransitionController` va `ZoneRoomRegistry`.

## 2. Luong login

### 2.1 Client UI

File chinh:

| Viec | File / method |
|---|---|
| Man hinh login, validate input, bam nut dang nhap | `Client/Assets/Scripts/UI/Auth/LoginController.cs` - `OnLoginClicked()` |
| Goi API login | `Client/Assets/Scripts/Services/Api/APIClient.cs` - `Login()`, `LoginCoroutine()` |
| Luu JWT | `APIClient.SetToken()` |
| Luu user local | `LoginController.OnLoginClicked()` luu `USER_ID`, `USERNAME` |
| Loading sau login | `Client/Assets/Scripts/UI/Auth/LoginLoadingManager.cs` |
| Luu account de chon nhanh | `LoginSavedAccountStore.cs`, `LoginSavedAccountRow.cs` |

Flow:

1. `LoginController.OnLoginClicked()` doc username/password tu UI.
2. Client validate rong, do dai username, do dai password.
3. `APIClient.Login()` POST JSON len `/api/auth/login`.
4. API tra ve `token`, `user_id`, `username`.
5. `APIClient.SetToken()` luu token vao memory va `PlayerPrefs["JWT_TOKEN"]`.
6. `LoginController` luu `USER_ID`, `USERNAME`.
7. `LoginLoadingManager.BeginLoading()` tiep tuc load player data/gene/scene tiep theo.

### 2.2 Backend

File chinh:

| Viec | File / method |
|---|---|
| Endpoint login | `GameServerApi/Controllers/AuthController.cs` - `Login()` |
| Rate limit login | `AuthController.Login()` gan `[EnableRateLimiting("login")]`, policy o `Program.cs` |
| Verify password | `GameServerApi/Services/AuthService.cs` - `VerifyPassword()` |
| Sinh JWT | `AuthService.GenerateJwtToken()` |
| JWT middleware | `GameServerApi/Program.cs` - `AddJwtBearer()` |

Flow backend:

1. `AuthController.Login()` nhan username/password.
2. Tim `User` theo username trong DB.
3. `AuthService.VerifyPassword()` dung BCrypt so sanh password.
4. Cap nhat `LastLogin`.
5. Ghi diem danh ngay qua `RecordDailyAttendanceAsync()`.
6. `AuthService.GenerateJwtToken()` tao JWT gom `sub`, `unique_name`, `user_id`, role.
7. Tra token ve client.

## 3. Luong register

| Viec | File / method |
|---|---|
| UI dang ky | `Client/Assets/Scripts/UI/Auth/RegisterController.cs` - `OnRegisterClicked()` |
| Goi API register | `APIClient.Register()`, `RegisterCoroutine()` |
| Backend register | `GameServerApi/Controllers/AuthController.cs` - `Register()` |
| Hash password | `GameServerApi/Services/AuthService.cs` - `HashPassword()` |

Flow:

1. `RegisterController` validate username, email, password, confirm password.
2. `APIClient.Register()` POST `/api/auth/register`.
3. `AuthController.Register()` check trung username/email.
4. Password duoc hash bang BCrypt.
5. Tao `User`, save DB, sinh JWT.
6. UI hien thanh cong va quay lai scene `Login`.

## 4. Luong logout

File chinh:

| Viec | File / method |
|---|---|
| Nut logout menu chinh | `Client/Assets/Scripts/UI/Menu/MainMenuController.cs` - `OnLogoutClicked()` |
| Reset vi tri ve map dau | `APIClient.ResetPlayerToStartMap()` |
| Tat ket noi Netcode | `MainMenuController.DisconnectNetwork()` |
| Xoa session local | `MainMenuController.ResetLocalSessionState()` |
| Xoa JWT | `APIClient.ClearToken()` |
| Chan popup disconnect khi logout chu dong | `Client/Assets/Scripts/Network/Managers/GameErrorNotifier.cs` |

Flow:

1. Nguoi choi bam logout.
2. `GameErrorNotifier.SuppressDisconnectNotifications()` chan thong bao mat ket noi vi day la logout chu dong.
3. Neu co JWT va playerId, client goi `ResetPlayerToStartMap()` de dua vi tri server ve map mac dinh.
4. `NetworkManagerCustom.Disconnect()` va `NetworkManager.Singleton.Shutdown()`.
5. `GameManager.ClearPlayerData()`, `ClientSceneController.ResetZoneState()`, `MapManager.ResetRuntimeState()`.
6. Xoa `JWT_TOKEN`, `USER_ID`, `USERNAME`, `PLAYER_ZONE_ID`, `SelectedMapId`, `CONNECT_TO_SERVER`.
7. Load lai scene `Login`.

Ngoai luong chinh nay, mot so UI khac cung co nut/xu ly xoa token nhu `CharacterMenuPanelUI.cs` va `SelectGeneController.cs`. Luong logout nen uu tien gom ve `MainMenuController` de tranh thieu cleanup network.

## 5. Luong ket noi game server va player session

| Viec | File / method |
|---|---|
| Khoi tao scene game | `MainSceneNetworkInitializer.cs`, `GameSceneNetworkInitializer.cs`, `GameSceneClientInitializer.cs` |
| Custom NetworkManager | `Client/Assets/Scripts/Network/Managers/NetworkManagerCustom.cs` |
| Connection approval moi | `Client/Assets/Scripts/Network/Server/ZoneConnectionApproval.cs` |
| Luu session clientId/userId/map/zone | `Client/Assets/Scripts/Network/Server/ZonePlayerSessionManager.cs` |
| Validate JWT phia Unity server | `Client/Assets/Scripts/Network/Shared/JwtValidator.cs` |
| Room/zone registry | `Client/Assets/Scripts/Network/Server/ZoneRoomRegistry.cs`, `ZoneRoom.cs` |

Flow:

1. Client co `JWT_TOKEN` va `USER_ID`.
2. Khi connect Netcode, client gui payload gom JWT, userId, mapId/zoneId.
3. `ZoneConnectionApproval` parse payload, validate token, chon room.
4. `ZonePlayerSessionManager` gan `clientId -> userId/playerId/map/zone/jwt`.
5. Server spawn player va chi sync object cung map/zone bang `NetworkVisibilityZoneFilter`.

## 6. Luong map, zone, teleport

| Viec | File / method |
|---|---|
| Config map runtime | `Client/Assets/Scripts/Network/Shared/MapWorldConfig.cs` |
| Quan ly room | `ZoneRoomRegistry.cs`, `ZoneRoom.cs` |
| Chuyen map/zone | `Client/Assets/Scripts/Network/Server/ZoneTransitionController.cs` |
| Client doi scene/teleport | `Client/Assets/Scripts/Network/Client/ClientSceneController.cs` |
| Trigger map/zone | `MapTransitionButton.cs`, `MapEdgeTrigger.cs`, `ZoneTransitionTrigger.cs` |
| API map/portal/spawn config | `GameServerApi/Controllers/MapController.cs` |

Flow:

1. Player cham portal/trigger hoac bam UI.
2. Client goi API map/portal de lay dich den, hoac goi ServerRpc transition.
3. `ZoneTransitionController` kiem tra cooldown, room hop le.
4. Server cap nhat session va goi `TeleportToZoneClientRpc()`.
5. Client load scene dich qua `ClientSceneController`.
6. `NetworkVisibilityZoneFilter` refresh de client chi thay object cung room.

## 7. Luong phó bản

### 7.1 Danh sach phó bản

| Viec | File / method |
|---|---|
| UI NPC phó bản | `Client/Assets/Scripts/UI/HUD/DungeonNpcMenuUI.cs` |
| Entry UI | `DungeonNpcMenuEntryUI.cs`, `DungeonListUI.cs`, `DungeonButtonItem.cs` |
| API list/detail | `GameServerApi/Controllers/DungeonController.cs` - `GetDungeonList()`, `GetDungeonDetail()` |

Flow:

1. NPC loai dungeon mo panel.
2. UI goi `/api/dungeon/list`.
3. Render danh sach phó bản, gom `dungeon_id`, `dungeon_type`, `map_id`, `scene_name`, level, max players.

### 7.2 Phó bản solo / wave

| Viec | File / method |
|---|---|
| Client xin vao dungeon | `DungeonManager.EnterDungeon()` |
| Server tao room instance | `ZoneTransitionController.RequestDungeonEntryServerRpc()` |
| Runtime wave | `Client/Assets/Scripts/Dungeon/Runtime/WaveDungeonRuntime.cs` |
| Quan ly lượt/ngay | `Client/Assets/Scripts/Network/Dungeon/WaveSessionManager.cs`, API `DungeonController` wave endpoints |
| HUD wave | `Client/Assets/Scripts/UI/HUD/WaveHUD.cs` |

Flow:

1. UI xac nhan phó bản solo.
2. `DungeonManager.EnterDungeon()` goi `RequestDungeonEntryServerRpc(mapId, dungeonId)`.
3. Server tao custom room cho dungeon map.
4. Server bat dau `WaveDungeonRuntime.BeginEncounter()`.
5. Runtime load config tu `/api/dungeon/wave/{dungeonId}/config`.
6. Moi wave spawn minion, het minion spawn boss, het boss sang wave tiep theo.
7. Het wave/thoi gian thi reward va return flow.

### 7.3 Phó bản tổ đội

| Viec | File / method |
|---|---|
| Tao/quan ly tổ đội client | `Client/Assets/Scripts/Party/PartyManager.cs` |
| Hub tổ đội backend | `GameServerApi/Hubs/PartyHub.cs` |
| UI check leader/cung map/zone | `DungeonNpcMenuUI.OnConfirmJoinClicked()` |
| Client xin vao dungeon party | `DungeonManager.EnterPartyDungeon()` |
| Server transfer ca party | `ZoneTransitionController.RequestPartyDungeonEntryServerRpc()` |
| Runtime party | `Client/Assets/Scripts/Dungeon/Runtime/PartyDungeonRuntime.cs` |
| Config party | `Client/Assets/Scripts/Dungeon/Config/PartyDungeonConfig.cs`, asset `Assets/Resources/ScriptableObjects/DungeonPartyConfig.asset` |

Flow:

1. Player tao party qua `PartyManager.CreateParty()`; backend luu state memory trong `PartyHub`.
2. Leader mo NPC phó bản va chon dungeon `multi`.
3. `DungeonNpcMenuUI` bat buoc co party, bat buoc leader, va check member cung map/zone.
4. `DungeonManager.EnterPartyDungeon()` gui danh sach userId cua member len server.
5. `ZoneTransitionController.RequestPartyDungeonEntryServerRpc()` tao mot custom room cho ca party.
6. Server resolve userId thanh clientId, transfer leader va member vao cung room.
7. Server goi `PartyDungeonRuntime.BeginEncounter(dungeonId, mapId, zoneId)`.
8. `PartyDungeonRuntime` spawn minion theo `PartyDungeonConfig.enemySpawns`; neu khong co minion thi spawn boss ngay.
9. Boss chet thi `CompleteDungeonCoroutine()` phat reward va goi return flow.

Ghi chu da sua ngay 2026-06-11:

- Truoc day block party trong `ZoneTransitionController` chi tim `WaveDungeonRuntime`, nen party dungeon co the khong khoi dong dung runtime party.
- Hien tai party block uu tien `PartyDungeonRuntime.BeginEncounter()`, fallback sang wave runtime neu scene khong co party runtime.
- `PartyDungeonRuntime` nhan dung `zoneId` cua custom room, nen enemy/boss co visibility dung cho to doi.

## 8. Luong boss AI va multi-phase HP

### 8.1 File chinh

| Viec | File / method |
|---|---|
| Boss AI chinh | `Client/Assets/Scripts/Enemy/BossAI.cs` |
| Load config boss tu API | `BossAI.LoadConfigFromServer()` |
| Check phase theo HP | `BossAI.CheckPhases()` |
| Thuc thi phase | `BossAI.ExecutePhase()` |
| Fallback phase 60/30 | `BossAI.EnsureBossPhasesConfigured()` |
| HP network/death | `Client/Assets/Scripts/Network/Enemy/NetworkEnemyHealth.cs` |
| API boss config | `GameServerApi/Controllers/DungeonController.cs` - `GetBossConfig()` |
| Entity DB enemy | `GameServerApi/Models/Entities/Enemy.cs` - `PhasesJson`, `SkillsJson` |

### 8.2 Trang thai hien tai

Da co boss multi-phase theo phan tram HP.

`BossAI.CheckPhases()` lay:

- `currentHealth`
- `maxHealth`
- tinh `hpPct = current / max * 100`
- neu `hpPct <= hp_pct_threshold` va phase chua trigger thi goi `ExecutePhase()`

Phase co the cau hinh trong DB bang `enemy.phases_json`:

```json
[
  {
    "hp_pct_threshold": 60,
    "action": "enrage",
    "damage_multiplier": 1.25,
    "speed_multiplier": 1.1,
    "message": "Boss vao Phase 2"
  },
  {
    "hp_pct_threshold": 30,
    "action": "berserk",
    "damage_multiplier": 1.6,
    "speed_multiplier": 1.2,
    "skill_cooldown_multiplier": 0.65,
    "message": "Boss vao Phase 3"
  }
]
```

Phase 1 la trang thai mac dinh tu 100% HP den tren 60%. Phase 2 kich hoat khi HP <= 60%. Phase 3 kich hoat khi HP <= 30%.

### 8.3 Action phase hien co

| `action` | Hanh vi |
|---|---|
| `enrage` | Tang damage multiplier va speed multiplier |
| `summon` | Spawn add bang `addSpawnPrefab` |
| `heal` | Hoi mau theo `% max HP` |
| `berserk` | Tang damage/speed va giam cooldown skill |

### 8.4 Sua bo sung cho phó bản tổ đội

Da bo sung:

1. `DungeonEnemyRuntimeStats.Apply()` set `BossAI.bossId = DungeonEnemyUnitConfig.enemyId`.
2. `BossAI` co fallback phase neu API/DB chua co `phases_json`:
   - Phase 1: mac dinh 100% -> tren 60%.
   - Phase 2: HP <= 60%, `enrage`.
   - Phase 3: HP <= 30%, `berserk`.
3. `PartyDungeonRuntime.BeginEncounter()` spawn boss dung map/zone cua custom room party.
4. `ZoneTransitionController.RequestPartyDungeonEntryServerRpc()` goi dung `PartyDungeonRuntime`.

Ket qua: boss trong `DungeonPartyConfig.asset` voi `enemyId: 13` se load boss config ID 13. Neu DB co `phases_json`, dung DB. Neu DB rong, fallback 60/30 van chay.

## 9. Luong spawn enemy / boss

| Viec | File / method |
|---|---|
| Spawn enemy theo DB map | `Client/Assets/Scripts/Network/Enemy/HostSpawnConfigLoader.cs` |
| Fallback/legacy spawn | `Client/Assets/Scripts/Network/Enemy/NetworkEnemySpawner.cs` |
| Prefab lookup | `Client/Assets/Scripts/Enemy/EnemyPrefabManager.cs` |
| Apply stat dungeon runtime | `DungeonEnemyRuntimeStats.cs` |
| Spawn enemy trong dungeon | `BaseDungeonInstance.SpawnConfiguredEnemy()` |
| Health/death/drop/EXP | `NetworkEnemyHealth.cs`, `EnemyHealth.cs`, `EnemyItemDrop.cs` |

Flow dungeon spawn:

1. Runtime dungeon goi `SpawnConfiguredEnemy(config, scale, isBoss)`.
2. `EnemyPrefabManager` lay prefab theo `enemyId`.
3. `DungeonEnemyRuntimeStats.Apply()` gan HP/MP/attack/defense/speed/level/drop.
4. Neu la boss, set `BossAI.bossId` va override damage/speed.
5. `NetworkEnemyHealth.PreInitMaxHp()` dat HP truoc khi `NetworkObject.Spawn()`.
6. Object duoc move vao physics scene map va gan `ZoneOwnerTag`.
7. Server spawn Netcode object.

## 10. Luong player data, gene, skill

| Mang | File chinh |
|---|---|
| GameManager data local | `Client/Assets/Scripts/Core/GameManager.cs` |
| Player API | `GameServerApi/Controllers/PlayerController.cs` |
| Load/sync player data network | `Client/Assets/Scripts/Network/Player/NetworkPlayerDataSync.cs` |
| Player movement | `NetworkPlayerController.cs`, `PlayerMovement.cs`, `PlayerDash.cs` |
| Player HP | `NetworkPlayerHealth.cs`, `PlayerHealth.cs` |
| Skill hotbar/runtime | `PlayerSkillManager.cs`, `SkillRuntimeLoader.cs` |
| Gene API | `GameServerApi/Controllers/GeneController.cs` |
| Gene UI | `GeneUpgradePanel.cs`, `SecondaryGeneUpgradePanel.cs`, `HybridFusionPanel.cs` |

Flow:

1. Sau login, client load player data theo `USER_ID`.
2. `GameManager.SetPlayerData()` giu data cho UI/client.
3. Netcode player object sync stat/health/skill qua `NetworkPlayerDataSync`.
4. Skill runtime doc DB/API de cap nhat damage/cooldown/unlock.
5. Gene upgrade/fuse di qua `GeneController` va UI inventory/character.

## 11. Luong inventory, item, reward

| Viec | File chinh |
|---|---|
| Inventory API | `GameServerApi/Controllers/PlayerController.cs`, `ItemController.cs`, `UpgradeController.cs` |
| Inventory network | `InventoryNetworkBridge.cs`, `NetworkInventory.cs` |
| Item template cache | `ItemTemplateManager.cs`, backend `ItemController.cs` |
| Pickup item | `ItemPickup.cs`, `PlayerPickup.cs`, `ItemSpawner.cs` |
| Dung item | `ItemUseHandler.cs` |
| Reward dungeon | `DungeonRewardGrantService.cs`, `DungeonRewardController.cs` |

Flow:

1. Client load item templates.
2. Inventory sync theo playerId/JWT.
3. Pickup/dung item gui ServerRpc/API.
4. Dungeon reward duoc grant cho client qua `DungeonRewardGrantService`.

## 12. Luong quest

| Viec | File chinh |
|---|---|
| Quest API | `GameServerApi/Controllers/QuestController.cs` |
| Quest client manager | `Client/Assets/Scripts/Quest/QuestManager.cs` |
| Progress reporter | `QuestProgressReporter.cs` |
| Quest UI | `QuestHudWidget.cs`, `QuestNpcPanel.cs`, `QuestDialogueUI.cs` |

Flow:

1. Client lay quest theo player.
2. NPC/monster/item action goi progress reporter.
3. API cap nhat progress/complete.
4. Quest UI refresh HUD va dialog.

## 13. Luong chat, friend, party

| Mang | File chinh |
|---|---|
| Chat backend | `GameServerApi/Hubs/ChatHub.cs` |
| Chat client | `ChatManager.cs`, `SignalRClient.cs` |
| Friend backend | `GameServerApi/Controllers/FriendController.cs` |
| Friend client | `FriendManager.cs`, `FriendListUI.cs` |
| Party backend | `GameServerApi/Hubs/PartyHub.cs` |
| Party client | `PartyManager.cs`, `PartyPanelUI.cs` |

Flow chat/party:

1. Client ket noi SignalR bang JWT.
2. Backend hub doc userId tu JWT claim.
3. Chat join map/group/clan/class channel.
4. Party hub luu party state va presence trong memory.
5. Party dungeon request duoc broadcast ve group party, nhung transfer that su do Unity server `ZoneTransitionController` thuc hien.

## 14. Checklist khi test luong boss phase trong phó bản tổ đội

1. Mo `Assets/Resources/ScriptableObjects/DungeonPartyConfig.asset`.
2. Xac nhan `bossSpawn.enemyId` la enemy DB co `EnemyType = Boss`.
3. Xac nhan prefab enemy do co `BossAI`, `NetworkObject`, `NetworkEnemyHealth` hoac `EnemyHealth`.
4. Vao dungeon multi bang leader party.
5. Console server phai co log:
   - `[ZoneTransitionController] Party dungeon entry`
   - `[PartyDungeonRuntime] SpawnBoss`
   - `[BaseDungeonInstance] Enemy spawned`
6. Danh boss xuong duoi 60% HP, phase 2 kich hoat.
7. Danh boss xuong duoi 30% HP, phase 3 kich hoat.

## 15. Ghi chu rui ro

1. `PartyHub` luu party trong memory, restart API se mat party.
2. `PartyDungeonRuntime` hien la runtime theo scene/object, chua phai multi-instance runtime hoan chinh cho nhieu party dong thoi tren cung mot dedicated server.
3. Code van con luong legacy song song: `ServerConnectionApproval`, `NetworkEnemySpawner`, root `Scripts/Network`. Khi gan scene nen uu tien luong moi: `ZoneConnectionApproval`, `ZonePlayerSessionManager`, `HostSpawnConfigLoader`, `ZoneTransitionController`.
4. Mot so comment/file dang bi mojibake, nen sua encoding rieng sau de de bao tri.

