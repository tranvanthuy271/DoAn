# Hướng dẫn cấu hình Prefab Phó Bản & Tổ Đội trong Unity

## Tổng quan

Hệ thống gồm 2 panel chính:
- **DungeonNpcMenuPanel** — Bảng menu NPC phó bản (2 màn hình: danh sách → xác nhận)
- **PartyPanel** — Bảng tổ đội 3 tab (Nhóm riêng / Tìm nhóm / Gần đây)

---

## 1. Chuẩn bị entry prefabs

### Đã tạo sẵn (kéo từ Project window):
| Prefab | Đường dẫn | Mục đích |
|--------|-----------|----------|
| `MemberEntryPrefab` | `Assets/Prefabs/UI/Party/` | Hàng thành viên Tab 1 |
| `PartySearchEntryPrefab` | `Assets/Prefabs/UI/Party/` | Hàng tìm nhóm Tab 2 |
| `NearbyPlayerEntryPrefab` | `Assets/Prefabs/UI/Party/` | Hàng gần đây Tab 3 |
| `DungeonNpcMenuEntryPrefab` | `Assets/Prefabs/UI/` | Hàng phó bản trong NPC menu |

---

## 2. Tạo PartyPanel trong Canvas

### Bước 1 – Tạo GameObject trong Canvas
1. Trong Hierarchy → chuột phải vào Canvas → **UI → Panel**
2. Đặt tên: `PartyPanel`
3. Thêm component: **PartyPanelUI** (từ `Scripts/UI/Party/PartyPanelUI.cs`)

### Bước 2 – Cấu trúc con (tạo thủ công hoặc dùng prefab)
```
PartyPanel (PartyPanelUI)
├── BtnClose              [Button]
├── TabBar                [HorizontalLayoutGroup]
│   ├── BtnTabParty       [Button] "Nhóm riêng"
│   ├── BtnTabSearch      [Button] "Tìm nhóm"  
│   └── BtnTabNearby      [Button] "Gần đây"
├── PanelParty            [VerticalLayoutGroup] — Tab 1
│   ├── ScrollView
│   │   └── Viewport/Content  ← memberListRoot
│   ├── ToggleLock            [Toggle] — lockToggle
│   ├── ToggleAutoAccept      [Toggle] — autoAcceptToggle
│   ├── BtnAction             [Button] — actionButton (Tạo/Giải tán/Rời)
│   │   └── TxtAction         [TMP_Text] — actionButtonLabel
│   └── BtnChatGroup          [Button] — chatGroupButton
├── PanelSearch           — Tab 2
│   ├── ScrollView
│   │   └── Viewport/Content  ← searchListRoot
│   └── BtnRefreshSearch  [Button]
└── PanelNearby           — Tab 3
    ├── TxtPopulation     [TMP_Text] — nearbyPopulationText "Dân số: 0"
    ├── ScrollView
    │   └── Viewport/Content  ← nearbyListRoot
    └── BtnRefreshNearby  [Button]
```

### Bước 3 – Gán SerializedField trong Inspector
| Field | GameObject |
|-------|------------|
| `closeButton` | BtnClose |
| `tabPartyButton` | BtnTabParty |
| `tabSearchButton` | BtnTabSearch |
| `tabNearbyButton` | BtnTabNearby |
| `partyTabPanel` | PanelParty |
| `searchTabPanel` | PanelSearch |
| `nearbyTabPanel` | PanelNearby |
| `memberListRoot` | Content trong ScrollView của PanelParty |
| `memberEntryPrefab` | `MemberEntryPrefab` (kéo từ Project) |
| `lockToggle` | ToggleLock |
| `autoAcceptToggle` | ToggleAutoAccept |
| `actionButton` | BtnAction |
| `actionButtonLabel` | TxtAction |
| `chatGroupButton` | BtnChatGroup |
| `statusText` | TxtStatus (thêm nếu cần) |
| `searchListRoot` | Content trong ScrollView của PanelSearch |
| `searchEntryPrefab` | `PartySearchEntryPrefab` |
| `refreshSearchButton` | BtnRefreshSearch |
| `nearbyListRoot` | Content trong ScrollView của PanelNearby |
| `nearbyEntryPrefab` | `NearbyPlayerEntryPrefab` |
| `refreshNearbyButton` | BtnRefreshNearby |
| `nearbyPopulationText` | TxtPopulation |

### Bước 4 – Ẩn các tab panel
- Mặc định chỉ `PanelParty` active, `PanelSearch` và `PanelNearby` tắt

---

## 3. Tạo DungeonNpcMenuPanel trong Canvas

### Bước 1 – Tạo GameObject
1. Canvas → chuột phải → **UI → Panel**
2. Đặt tên: `DungeonNpcMenuPanel`
3. Thêm component: **DungeonNpcMenuUI**

### Bước 2 – Cấu trúc con
```
DungeonNpcMenuPanel (DungeonNpcMenuUI)
├── PanelList                  — listPanel (Màn hình 1)
│   ├── TxtGreeting [TMP_Text] — greetingText "Xin chào {name}"
│   ├── ScrollView
│   │   └── Viewport/Content  ← dungeonListRoot
│   └── BtnCloseList [Button] "Cáo từ" — btnCloseList
└── PanelConfirm               — confirmPanel (Màn hình 2, inactive mặc định)
    ├── TxtConfirmInfo [TMP_Text] — confirmInfoText
    ├── ConfirmOptionRoot [VerticalLayoutGroup] ← confirmOptionRoot
    │   └── (spawn DungeonNpcMenuEntryPrefab tại runtime)
    ├── BtnConfirmJoin [Button] "Tham gia" — btnConfirmJoin
    └── BtnBackToList  [Button] "Cáo từ"  — btnBackToList
```

### Bước 3 – Gán SerializedField
| Field | GameObject |
|-------|------------|
| `listPanel` | PanelList |
| `greetingText` | TxtGreeting |
| `dungeonListRoot` | Content trong ScrollView của PanelList |
| `dungeonEntryPrefab` | `DungeonNpcMenuEntryPrefab` |
| `btnCloseList` | BtnCloseList |
| `confirmPanel` | PanelConfirm |
| `confirmInfoText` | TxtConfirmInfo |
| `confirmOptionRoot` | ConfirmOptionRoot |
| `confirmOptionPrefab` | `DungeonNpcMenuEntryPrefab` (hoặc prefab riêng) |
| `btnConfirmJoin` | BtnConfirmJoin |
| `btnBackToList` | BtnBackToList |

### Bước 4 – Ẩn PanelConfirm mặc định
- Tắt active của `PanelConfirm` trong Inspector

---

## 4. Tạo NPC dungeon trong scene

1. Tạo một NPC GameObject với script `NpcController` (hoặc tương đương)
2. Thiết lập `npc_type = "dungeon"` trong NpcData
3. Khi tương tác, `NpcMenuUI.Open(npc)` sẽ tự route sang `DungeonNpcMenuUI`

Hoặc thêm NPC vào DB (xem phần migration dưới):

---

## 5. Chạy migration SQL

```bash
# Từ thư mục gốc dự án
mysql -u root -p gamedb < GameServerApi/sql/030_dungeon_npc.sql
```

Lệnh này sẽ:
- Insert 2 NPC có `npc_type = 'dungeon'` vào `npc_config`
- Insert 3 phó bản mẫu vào `dungeon_config`

---

## 6. Checklist kiểm tra

### Client Unity
- [ ] `PartyManager` Singleton có trong scene (DontDestroyOnLoad)
- [ ] `InputManager` Singleton có trong scene
- [ ] Canvas chứa cả `PartyPanel` và `DungeonNpcMenuPanel`
- [ ] Tất cả SerializedField đã gán trong Inspector (không có null)
- [ ] `PanelSearch`, `PanelNearby`, `PanelConfirm` đang inactive mặc định
- [ ] Entry prefabs đã gán đúng script component (kiểm tra trong Inspector của prefab)

### Server
- [ ] `PartyHub.cs` đã đăng ký trong `Program.cs` (`app.MapHub<PartyHub>(...)`)
- [ ] JWT auth đang hoạt động với SignalR (`?access_token=...`)
- [ ] `dungeon_config` table tồn tại trong DB
- [ ] Migration `030_dungeon_npc.sql` đã chạy thành công

### Luồng test
1. Mở game → tương tác NPC dungeon → **màn hình danh sách** hiện ra với lời chào
2. Click vào tên phó bản → **màn hình xác nhận** hiện ra với thông tin "Hãy tập hợp đồng đội"
3. Click "Tham gia" → vào phó bản (solo hoặc cùng party)
4. Click "Cáo từ" → đóng panel, gameplay input được mở lại
5. Mở PartyPanel → 3 tab hoạt động đúng
6. Tab "Tìm nhóm" → Refresh → hiện danh sách → Click "Xin vào" → gửi request
7. Tab "Gần đây" → Refresh → hiện người chơi gần → Click "Mời" → gửi invite

---

## 7. Debug thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| Panel không đóng khi nhấn "Cáo từ" | InputManager source key sai | Kiểm tra `"DungeonNpcMenuUI"` khớp trong Open/Close |
| Danh sách phó bản trống | API `/api/dungeon/list` chưa trả về data | Kiểm tra DB có dữ liệu, kiểm tra JWT |
| Không thể Mời/Xin vào | `PartyManager` chưa kết nối SignalR | Kiểm tra kết nối hub, log lỗi trong Console |
| NPC không mở DungeonNpcMenuUI | `npc_type` không phải `"dungeon"` | Kiểm tra data NPC trong DB hoặc NpcConfig |
