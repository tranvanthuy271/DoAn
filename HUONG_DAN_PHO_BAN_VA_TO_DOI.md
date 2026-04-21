# HƯỚNG DẪN PHÓ BẢN VÀ TỔ ĐỘI

## 1. File đã tạo thật

### Client
- `Client/Assets/Scripts/Party/PartyDtos.cs`
- `Client/Assets/Scripts/Party/PartyManager.cs`
- `Client/Assets/Scripts/UI/Party/PartyPanelUI.cs`
- `Client/Assets/Scripts/UI/Party/PartyMemberEntryUI.cs`
- `Client/Assets/Scripts/UI/Party/PartySearchEntryUI.cs`
- `Client/Assets/Scripts/UI/Party/PartyNearbyEntryUI.cs`
- `Client/Assets/Scripts/Dungeon/Config/DungeonEncounterConfigs.cs`
- `Client/Assets/Scripts/Dungeon/Runtime/BaseDungeonInstance.cs`
- `Client/Assets/Scripts/Dungeon/Runtime/WaveDungeonRuntime.cs`
- `Client/Assets/Scripts/Dungeon/Runtime/PartyDungeonRuntime.cs`
- `Client/Assets/Scripts/Dungeon/Runtime/DungeonEnemyRuntimeStats.cs`
- `Client/Assets/Scripts/Dungeon/Runtime/DungeonRewardGrantService.cs`

### Server
- `GameServerApi/Hubs/PartyHub.cs`
- `GameServerApi/Models/Realtime/PartyRealtimeModels.cs`
- `GameServerApi/Controllers/DungeonRewardController.cs`

### File đã vá để chạy cùng hệ cũ
- `Client/Assets/Scripts/UI/Chat/ChatPanelUI.cs`
- `Client/Assets/Scripts/Core/GameManager.cs`
- `Client/Assets/Scripts/Enemy/EnemyAI.cs`
- `Client/Assets/Scripts/Enemy/BossAI.cs`
- `Client/Assets/Scripts/Enemy/EnemyHealth.cs`
- `Client/Assets/Scripts/Network/Enemy/NetworkEnemyHealth.cs`
- `GameServerApi/Program.cs`

## 2. Tổ đội

### Logic đã có
- Tạo nhóm, mời, xin vào nhóm, rời nhóm, giải tán nhóm.
- Tìm nhóm theo `mapId + zoneId`.
- Tab gần đây lấy danh sách người chơi cùng khu.
- Chat nhóm dùng luôn kênh `group` của chat hiện tại.
- Khi leader bấm vào phó bản, cả tổ đội nhận event và tự gọi `DungeonManager.EnterDungeon(...)`.

### Setup Unity
1. Tạo `GameObject` mới, gắn `PartyManager` trong scene bootstrap hoặc để con của `GameManager`.
2. Tạo `PartyPanel` trong Canvas, gắn `PartyPanelUI`.
3. Tạo 3 panel con: `TabParty`, `TabSearch`, `TabNearby`.
4. Tạo 3 prefab UI:
   - `MemberEntryPrefab` gắn `PartyMemberEntryUI`
   - `SearchEntryPrefab` gắn `PartySearchEntryUI`
   - `NearbyEntryPrefab` gắn `PartyNearbyEntryUI`
5. Kéo đủ reference vào `PartyPanelUI`.

### Hành vi UI
- Tab 1 `Tổ đội`: leader đứng đầu, tên leader đậm hơn.
- `Khóa nhóm`: chỉ leader chỉnh.
- `Tự cho vào`: chỉ leader chỉnh.
- Nút hành động:
  - chưa có nhóm: tạo nhóm
  - leader: giải tán
  - member: rời nhóm
  - nếu có request chờ duyệt: leader bấm nút này để chấp nhận nhanh
- `Chat nhóm`: mở `ChatPanel` sang tab `Nhóm`.
- Mở panel sẽ chặn di chuyển và skill qua `InputManager.SetGameplayInputBlocked(...)`.

## 3. Phó bản 1

### Script dùng
- `DungeonWaveConfig` trong `DungeonEncounterConfigs.cs`
- `WaveDungeonRuntime.cs`

### Tính năng đã có
- Quái spawn tại vị trí cố định theo config trong Unity.
- Boss spawn đúng vị trí sau khi giết hết quái.
- Mỗi vòng có thời gian `roundTimeSeconds`, mặc định 300 giây.
- Boss chết thì sang vòng mới.
- Mỗi vòng tăng stat theo `roundScalingPercent`, mặc định `10%`.
- Đến `maxRounds = 20` thì hoàn thành, phát thưởng, đếm ngược rồi trả về map gốc.
- Có text `round`, `timer`, `countdown`, `status` để hiển thị trực tiếp trên UI.

### Setup Unity
1. Tạo asset: `Create -> DoAn -> Dungeon -> Wave Config`.
2. Điền:
   - `returnMapId = 0`
   - `returnSceneName = GameScene`
   - `roundTimeSeconds = 300`
   - `maxRounds = 20`
   - `roundScalingPercent = 10`
3. Thêm `enemySpawns[]` và `bossSpawn`.
4. Trong mỗi entry cấu hình trực tiếp:
   - `enemyId`
   - `spawnPosition`
   - `maxHp`
   - `maxMp`
   - `attack`
   - `defense`
   - `moveSpeed`
   - `expReward`
   - `level`
   - `drops`
5. Trong scene dungeon, tạo `GameObject` gắn `WaveDungeonRuntime` rồi kéo `config` và các `TMP_Text` vào.

## 4. Phó bản 2

### Script dùng
- `PartyDungeonConfig` trong `DungeonEncounterConfigs.cs`
- `PartyDungeonRuntime.cs`

### Tính năng đã có
- Spawn quái cố định trong scene.
- Giết hết quái thì spawn boss.
- Boss có reward rơi trực tiếp từ `bossSpawn.drops`.
- Sau khi boss chết, toàn bộ người chơi trong dungeon nhận `completionRewards` vào túi đồ.
- Đếm ngược rồi trả toàn đội về map gốc.

### Setup Unity
1. Tạo asset: `Create -> DoAn -> Dungeon -> Party Config`.
2. Điền `returnMapId`, `returnSceneName`, `returnCountdownSeconds`.
3. Thêm `enemySpawns[]`, `bossSpawn`, `completionRewards[]`.
4. Trong scene dungeon, tạo `GameObject` gắn `PartyDungeonRuntime` và kéo `config`, `statusText`, `countdownText` vào.

## 5. Stat quái và boss

### Cách hoạt động
- `DungeonEnemyRuntimeStats.cs` áp stat từ config vào runtime.
- `EnemyAI.cs` đã thêm `ApplyRuntimeOverride(...)` để ăn `attack`, `moveSpeed`, `canFly`.
- `BossAI.cs` đã thêm `ApplyRuntimeOverride(...)` để ăn `base damage` và `chaseSpeed`.
- `EnemyHealth.cs` và `NetworkEnemyHealth.cs` đã trừ damage theo `defense` của config.

### Lưu ý
- `maxMp` hiện đã lưu ở runtime để dùng tiếp cho skill logic nếu cần, nhưng hệ AI cũ chưa tiêu hao MP thật.
- Drop item của từng quái/boss lấy trực tiếp từ `drops` trong config Unity.

## 6. Phần thưởng và bảo mật

### Luồng phát thưởng
- `DungeonRewardGrantService.cs` tự chọn 1 trong 2 cách:
  - có `ZoneApiKey`: gọi `POST /api/dungeonreward/grant`
  - không có `ZoneApiKey`: fallback sang `POST /api/player/{id}/inventory/add` bằng JWT local

### Bảo mật
- `PartyHub` dùng JWT và lấy user từ `Context.UserIdentifier`, không tin dữ liệu user do client tự gửi.
- `DungeonRewardController` chỉ cho phép gọi bằng `X-Zone-Api-Key`.
- `Program.cs` đã mở thêm `/partyhub` và cho phép JWT qua query param giống chat hub.

## 7. Chỗ cần kéo tay trong Unity

1. Tạo prefab UI cho 3 loại row của tổ đội.
2. Gắn `PartyPanelUI` vào panel tổ đội.
3. Tạo 2 asset config dungeon.
4. Gắn `WaveDungeonRuntime` hoặc `PartyDungeonRuntime` vào scene phó bản tương ứng.
5. Đảm bảo prefab quái và boss có sẵn `NetworkObject`, `EnemyAI` hoặc `BossAI`, `EnemyHealth` hoặc `NetworkEnemyHealth`, `EnemyItemDrop`.
6. Thêm `PartyManager` vào bootstrap scene để nó tự kết nối `partyhub` sau login.

## 8. Gọi phó bản theo tổ đội

Leader gọi:

```csharp
PartyManager.Instance.StartPartyDungeon(dungeonId, mapId, "multi");
```

Khi đó:
1. `PartyHub` broadcast `PartyDungeonRequested` cho cả nhóm.
2. `PartyManager` trên từng client tự lấy `DungeonConfigData` hiện có.
3. Mỗi client tự gọi `DungeonManager.EnterDungeon(...)`.
4. Dungeon session hiện có sẽ tự gom cả tổ đội vào cùng một phiên theo flow sẵn của `DungeonManager + DungeonNetworkBridge`.