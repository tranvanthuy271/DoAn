# Hướng Dẫn UI Phó Bản — Config & Luồng Đầy Đủ

## Tổng quan hệ thống

```
NPC (npc_type="dungeon")
  └─ Click → NpcMenuUI.Open()
       └─ npc_type == "dungeon" → DungeonNpcMenuUI.Open()
            ├─ ListPanel: chào + danh sách phó bản
            │    └─ Chọn 1 row → ShowConfirm()
            └─ ConfirmPanel: xác nhận tham gia
                 ├─ Solo     → DungeonManager.EnterDungeon()
                 ├─ Có nhóm, tất cả cùng zone → PartyManager.StartPartyDungeon()
                 └─ Có nhóm, chưa tập hợp → GlobalNotificationUI.Show("chưa sẵn sàng")
```

---

## 1. File đã tạo / chỉnh sửa

| File | Vị trí | Vai trò |
|---|---|---|
| `GlobalNotificationUI.cs` | `Scripts/UI/` | Panel thông báo toàn cục |
| `DungeonNpcMenuUI.cs` | `Scripts/UI/HUD/` | Thêm zone/map check trước khi vào phó bản |
| `CreateDungeonUIPrefabs.cs` | `Scripts/Editor/` | Editor tool tạo prefab qua Tools menu |

---

## 2. Tạo Prefab trong Unity (một lần duy nhất)

**Menu Unity:** `Tools ▸ Create Dungeon UI Prefabs`

Sẽ tạo ra (nếu chưa tồn tại):

| Prefab | Mô tả |
|---|---|
| `Assets/Prefabs/UI/DungeonNpcMenuPanel.prefab` | Panel NPC phó bản (list + confirm 2 màn hình) |
| `Assets/Prefabs/UI/DungeonNpcMenuEntryPrefab.prefab` | Mỗi row danh sách phó bản |
| `Assets/Prefabs/UI/GlobalNotificationPanel.prefab` | Thông báo toàn cục |

---

## 3. Gắn vào Scene

1. Mở scene game (ví dụ `GameScene`)
2. Tìm Canvas chính (cùng Canvas chứa `PartyPanel`)
3. Kéo **`DungeonNpcMenuPanel.prefab`** vào Canvas → Inspector assign:
   - `listPanel` → child `ListPanel`
   - `greetingText` → `ListPanel/GreetingText`
   - `dungeonListRoot` → `ListPanel/DungeonScrollView/Viewport/Content`
   - `dungeonEntryPrefab` → `Assets/Prefabs/UI/DungeonNpcMenuEntryPrefab`
   - `btnCloseList` → `ListPanel/BtnCloseList`
   - `confirmPanel` → child `ConfirmPanel`
   - `confirmInfoText` → `ConfirmPanel/ConfirmInfoText`
   - `confirmOptionRoot` → `ConfirmPanel/ConfirmOptionRoot`
   - `btnConfirmJoin` → `ConfirmPanel/BtnConfirmJoin`
   - `btnBackToList` → `ConfirmPanel/BtnBackToList`
4. Kéo **`GlobalNotificationPanel.prefab`** vào Canvas → assign:
   - `panel` → child `Panel`
   - `titleText` → `Panel/TitleText`
   - `messageText` → `Panel/MessageText`
   - `btnOk` → `Panel/BtnOk`
5. `GlobalNotificationPanel` nên đặt **Sort Order cao nhất** (trên tất cả panel) hoặc đặt trong Canvas riêng với `sortingOrder` cao hơn.

---

## 4. Config NPC Phó Bản

Để 1 NPC mở menu toàn bộ phó bản, set trong database hoặc NPC config:

```
npc_type  = "dungeon"
npc_name  = "Thủ môn Phó Bản"  (hoặc tên tùy chọn)
dialogue_text = "Xin chào dũng sĩ! Ta có thể đưa ngươi vào các vùng nguy hiểm."
```

Danh sách phó bản được lấy tự động từ API (`GetDungeonListServerRpc`) — không cần config thêm trên NPC.

Backend hiện đã tự sửa dữ liệu `NULL` cũ trong `dungeon_config` khi khởi động. Nếu bảng phó bản đang trống và `map_config` đã có `map_id = 100` và `101`, API sẽ tự seed 2 phó bản mặc định.

---

## 5. Config Phó Bản (DungeonWaveConfig / PartyDungeonConfig)

**Phó bản sóng** — `DoAn/Dungeon/Wave Config`:
```
dungeonId       = 6           ← phải match với DB dungeon_id / API list
returnSceneName = "GameScene"
returnMapId     = 0
maxRounds       = 20
sceneName       = "DungeonWaveScene"  ← Unity scene name để LoadScene
```

**Phó bản tổ đội** — `DoAn/Dungeon/Party Config`:
```
dungeonId       = 7
returnSceneName = "GameScene"
returnMapId     = 0
sceneName       = "DungeonPartyScene"
```

### Thêm phó bản mới vào Database (server)
```sql
-- map_id phải tồn tại trong map_config
INSERT INTO dungeon_config (dungeon_name, dungeon_type, map_id, scene_name, max_players, min_level_required)
VALUES ('Mê Cung Phong', 'multi', 100, 'DungeonPartyScene', 4, 1);
```

Nếu chưa có map dungeon riêng trong `map_config`, hãy tạo map đó trước rồi mới insert vào `dungeon_config`.

---

## 6. Chuyển Player sang Scene Phó Bản

Luồng `DungeonManager.EnterDungeon(config)`:
1. Lấy `config.scene_name` (ví dụ `"DungeonWaveScene"`)
2. Shutdown NetworkManager hiện tại (nếu đang là client)
3. Start Host mới trong scene đó
4. Load scene bằng `NetworkManager.SceneManager.LoadScene(config.scene_name, LoadSceneMode.Single)`

**Đối với Tổ Đội** (`PartyDungeonRequested` event):
- Tất cả thành viên nhận `PartyDungeonRequested` từ server
- Mỗi client gọi `DungeonManager.Instance.HandlePartyDungeonRequest(payload)`
- Leader tạo Host → các thành viên Join vào IP/Port của Leader

---

## 7. Đồng bộ riêng từng Phó Bản (Multi-Instance)

> **Câu hỏi: Phó bản hiện tại có đồng bộ riêng không?**

Có — mỗi lần `DungeonManager.EnterDungeon()` được gọi, nó:
- Shutdown NetworkManager cũ
- Start một Host mới **hoàn toàn độc lập**
- Scene được load riêng → state phó bản không chia sẻ với map chính

⚠️ **Giới hạn hiện tại:** Chỉ có 1 instance phó bản / máy chủ Unity (vì chạy trên cùng process). Để scale nhiều nhóm cùng lúc, cần:
- Mỗi nhóm chạy trên một **Unity Dedicated Server process** riêng, hoặc
- Dùng **scene additive** với zone isolation (xem `HUONG_DAN_MAP_ADDITIVE_PHYSICS_ISOLATION.md`)

---

## 8. Kiểm tra Zone trước khi vào Phó Bản (logic mới)

`DungeonNpcMenuUI.FindMemberNotInSameZone()` sẽ:
1. Lấy `partyManager.LatestNearbyPlayers` (danh sách người cùng zone/map)
2. So sánh với `ClientSceneController.CurrentMapId` + `CurrentZoneId` của local player
3. Nếu có thành viên online KHÔNG trong cùng zone → hiện `GlobalNotificationUI`:
   > *"Thành viên 'XYZ' chưa ở cùng khu vực. Hãy tập hợp đầy đủ trước khi vào phó bản!"*

**Để refresh NearbyPlayers trước khi check**, gọi:
```csharp
PartyManager.Instance?.RefreshNearbyPlayers(); // (optional - dữ liệu tự refresh định kỳ)
```

---

## 9. Sử dụng GlobalNotificationUI ở các luồng khác

Dùng cho bất kỳ thông báo lỗi / cảnh báo nào trong game:
```csharp
// Hiện thông báo thường (nhấn OK để đóng)
GlobalNotificationUI.Show("Túi đồ đầy, không thể nhặt thêm!");

// Với tiêu đề tùy chỉnh
GlobalNotificationUI.Show("Level không đủ để vào phó bản này.", "Yêu cầu level");

// Tự động ẩn sau 3 giây
GlobalNotificationUI.Show("Kết nối thành công!", "Thông báo", autoHideSeconds: 3f);
```

---

## 10. Checklist khi setup lần đầu

- [ ] Chạy `Tools ▸ Create Dungeon UI Prefabs` trong Unity Editor
- [ ] Kéo `DungeonNpcMenuPanel` vào Canvas trong `GameScene`, assign tất cả field Inspector
- [ ] Kéo `GlobalNotificationPanel` vào Canvas (hoặc Canvas riêng sortOrder cao hơn)
- [ ] Set `npc_type = "dungeon"` cho NPC cổng phó bản trong database
- [ ] Đảm bảo tên scene trong `dungeon_config.scene_name` khớp với tên scene trong Unity Build Settings
- [ ] Thêm tất cả DungeonScene vào `File ▸ Build Settings` trong Unity
