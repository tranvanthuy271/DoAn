# Hướng Dẫn Config Quest System (LangLa Style)

## Tổng Quan

Quest system đã được code đầy đủ. Phần này hướng dẫn **config cơ sở dữ liệu + setup Unity + test**.

---

## PHẦN 1: Database Setup

### 1.1 Chạy Migration SQL

```bash
cd c:\Hub\DoAn\SQL
mysql -u root -p gamedb < migrate_quest_system.sql
```

**Kết quả:**
- Xoá bảng `player_quest` (cũ)
- Tạo bảng `quest_config` mới với 10 quest mẫu:
  - Quest 1-3: làm quen tại Làng Khởi Đầu với Đại Tướng Lan
  - Quest 4-5: dọn Goblin và thu thập nguyên liệu tại map 1 với Hướng Dẫn Viên
  - Quest 6-10: mở tuyến Cửa Phía Đông, săn Orc Warrior và gom vật liệu gene/rèn đồ

### 1.2 Cấu Trúc Bảng `quest_config`

| Cột | Kiểu | Mô Tả |
|-----|------|-------|
| `id` | INT | ID quest |
| `name` | VARCHAR | Tên quest (VD: "Diệt quái vật") |
| `level_need` | INT | Cấp tối thiểu để nhận (VD: 1) |
| `npc_id` | INT | ID NPC nhận/giao quest (VD: 2) |
| `str1` | TEXT | Hội thoại khi **nhận** quest |
| `str2` | TEXT | Hội thoại khi **nộp/hoàn thành** quest |
| `str3` | TEXT | Ghi chú/hướng dẫn cho player |
| `exp_reward` | INT | EXP thưởng |
| `gold_reward` | INT | Vàng thưởng |
| `silver_reward` | INT | Bạc thưởng |
| `item_reward` | VARCHAR | Vật phẩm thưởng (format: `itemId@qty,itemId@qty`) |
| `step` | LONGTEXT | JSON array của các bước quest (xem dưới) |
| `sort_order` | INT | Thứ tự hiển thị |
| `is_active` | TINYINT | 1=hoạt động, 0=ẩn |

### 1.3 Định Dạng JSON Các Bước Quest

**Cơ cấu mỗi bước:**
```json
{
  "id": 0,              // Type: 0=kill mob, 1=collect item, 5=talk to NPC, 9=reach map
  "name": "Tiêu diệt Goblin",  // Tên hiển thị trong HUD
  "idMob": 2,           // ID quái cần diệt (-1 = không cần)
  "idNpc": -1,          // ID NPC cần nói chuyện (-1 = không cần)
  "idItem": -1,         // ID vật phẩm cần thu thập (-1 = không cần)
  "idMap": -1,          // ID bản đồ (-1 = bất kỳ map)
  "x": 0, "y": 0,       // Toạ độ mục tiêu (0,0 = bất kỳ)
  "require": 5,         // Số lần/số lượng cần hoàn thành
  "STR": ""             // Hội thoại phụ (cho NPC type)
}
```

**Ví dụ quest đa bước:**
```sql
[
  {
    "id": 5,
    "name": "Nói chuyện với Hướng Dẫn Viên",
    "idMob": -1,
    "idNpc": 14,
    "idItem": -1,
    "idMap": 1,
    "x": 0, "y": 0,
    "require": 1,
    "STR": "14@Chào dũng sĩ!"
  },
  {
    "id": 0,
    "name": "Tiêu diệt Goblin",
    "idMob": 2,
    "idNpc": -1,
    "idItem": -1,
    "idMap": -1,
    "x": 0, "y": 0,
    "require": 10,
    "STR": ""
  }
]
```

### 1.4 Thêm NPC Mới (Nếu Cần)

```sql
-- Kiểm tra NPC quest đã tồn tại
SELECT * FROM npc_config WHERE npc_type = 'quest' AND is_active = 1;

-- Thêm NPC quest mới
INSERT INTO npc_config 
  (npc_id, npc_name, npc_type, map_id, pos_x, pos_y, is_active)
VALUES 
  (100, 'Tên NPC', 'quest', 0, 5.0, 0.0, 1)
ON DUPLICATE KEY UPDATE npc_type='quest', is_active=1;
```

### 1.5 Thêm Quest Mới

```sql
INSERT INTO quest_config 
  (name, level_need, npc_id, str1, str2, str3, 
   exp_reward, gold_reward, silver_reward, item_reward, step, sort_order, is_active)
VALUES 
  (
    'Tên Quest',
    5,                                    -- level_need
    100,                                  -- npc_id
    'Hội thoại khi nhận quest',
    'Hội thoại khi nộp quest',
    'Ghi chú cho player',
    1000,                                 -- exp_reward
    100,                                  -- gold_reward
    50,                                   -- silver_reward
    '1@5,2@3',                            -- itemId@qty,itemId@qty
    '[{"id":0,"name":"Diệt 5 quái","idMob":2,"require":5,"idNpc":-1,"idItem":-1,"idMap":-1,"x":0,"y":0,"STR":""}]',
    1,                                    -- sort_order
    1                                     -- is_active
  );
```

---

## PHẦN 2: Unity Setup

### 2.1 Tạo Canvas UI

**Menu: DoAn → Quest → Create All Quest UI**

Lệnh này sẽ tạo 4 prefab:
- ✓ `Assets/Resources/UI/QuestDialogueUI.prefab` (hộp hội thoại màn đen)
- ✓ `Assets/Resources/UI/QuestNpcPanel.prefab` (danh sách quest khi nói với NPC)
- ✓ `Assets/Resources/UI/QuestHudWidget.prefab` (tracker góc màn hình)
- ✓ `Assets/Resources/UI/Quest/QuestListItem.prefab` (item trong danh sách)

### 2.2 Đặt Prefabs Vào Scene Game

Trong scene `MainGame` (hoặc scene game chính):

1. **Tìm hoặc tạo GameObject `Canvas` chính** (nếu chưa có)
2. **Kéo vào 3 prefab từ Resources:**
   ```
   Canvas
   ├── QuestDialogueUI (từ Resources/UI/QuestDialogueUI.prefab)
   ├── QuestNpcPanel (từ Resources/UI/QuestNpcPanel.prefab)
   └── QuestHudWidget (từ Resources/UI/QuestHudWidget.prefab)
   ```

### 2.3 Gán Tham Chiếu cho QuestNpcPanel

1. **Chọn GameObject `QuestNpcPanel` trong Hierarchy**
2. **Inspector → QuestNpcPanel component**
3. **Field "Quest Item Prefab"** → Kéo `QuestListItem.prefab` từ `Assets/Resources/UI/Quest/` vào

### 2.4 Đảm Bảo QuestManager Có Trong Scene

1. **Tìm hoặc tạo GameObject `QuestManager`**
   - Nếu chưa có: **Ctrl+Shift+N** → Tạo empty GameObject tên `QuestManager`
2. **Add Component: `Quest/QuestManager.cs`**
3. **Config:**
   - API Endpoint: (để mặc định, sử dụng `GameClient.baseUrl`)
   - Authorization: (tự động từ `AuthHelper.AddAuthHeader`)

### 2.5 Kiểm Tra NpcMenuUI Config

**File: `Client/Assets/Scripts/UI/NPC/NpcMenuUI.cs`**

Đảm bảo hàm `Open(NpcData npc)` có xử lý `npc_type == "quest"`:

```csharp
public void Open(NpcData npc)
{
    _currentNpc = npc;
    
    if (npc.npc_type == "quest")
    {
        QuestNpcPanel.GetOrCreate()?.Open(npc);
        return;
    }
    
    // ... xử lý các type khác (shop, etc.)
}
```

---

## PHẦN 3: Test Toàn Bộ Flow

### 3.1 Chuẩn Bị Test

1. **Chạy server** (dotnet run)
2. **Tạo character trong game**
3. **Load main game scene**

### 3.2 Test Scenario 1: Pre-Accept (HUD Hint)

**Kỳ vọng:**
- HUD góc trên-trái hiện: `"Chính: Diệt quái vật đầu tiên\n- Tìm Đại Tướng Lan để nhận nhiệm vụ"`
- Nút "→" active

**Bước test:**
1. Vào game → chưa có quest active
2. HUD phải hiện hint quest (quest đầu tiên chưa nhận)
3. Click nút "→" → auto-move đến NPC Đại Tướng Lan

### 3.3 Test Scenario 2: NPC Menu & Accept

**Kỳ vọng:**
- Nói chuyện với NPC → QuestNpcPanel mở
- Danh sách 1 quest: `"? Diệt quái vật đầu tiên"`

**Bước test:**
1. Click NPC → QuestNpcPanel mở
2. Click quest trong danh sách
3. QuestDialogueUI mở (màn đen + hội thoại str1)
4. Click "Nhận" → quest được accept
5. HUD cập nhật: `"Chính: Diệt quái vật đầu tiên\n- Tiêu diệt Goblin: 0/5"`

### 3.4 Test Scenario 3: Quest In Progress

**Kỳ vọng:**
- Đi diệt Goblin → quest_progress tăng
- HUD: `"- Tiêu diệt Goblin: 2/5"` (khi diệt được 2 con)

**Bước test:**
1. Diệt Goblin
2. Server gọi `/api/quest/progress-by-event` → quest_progress cập nhật
3. Refocus game hoặc trigger HUD refresh
4. HUD hiện số mới

### 3.5 Test Scenario 4: Complete Quest

**Kỳ vọng:**
- Sau khi `done == require` (5/5)
- HUD: `"Chính: Diệt quái vật đầu tiên\n- ✓ Tìm Đại Tướng Lan để nộp nhiệm vụ"`
- Nút "→" → auto-move đến NPC

**Bước test:**
1. Diệt đủ 5 Goblin
2. HUD tự cập nhật (hoặc manual refresh)
3. Nói chuyện với NPC
4. Click quest (giờ status = "completed" hoặc "submittable")
5. QuestDialogueUI hiện str2 (hội thoại nộp quest)
6. Click "Nhận thưởng" → quest complete, EXP/Gold/Item cộng vào inventory

---

## PHẦN 4: Thêm Quest Mới (Step-by-Step)

### 4.1 Thêm Vào Database

```sql
INSERT INTO quest_config 
  (id, name, level_need, npc_id, str1, str2, str3,
   exp_reward, gold_reward, silver_reward, item_reward, step, sort_order, is_active)
VALUES 
  (
    10,                              -- Unique ID
    'Thu thập mảnh vàng',            -- Tên
    15,                              -- Cấp tối thiểu
    100,                             -- NPC ID (phải tồn tại trong npc_config!)
    'Dũng sĩ, ta cần mảnh vàng để nấu thuốc. Hãy thu thập 3 mảnh vàng từ quái.',
    'Cảm ơn! Đây chính là những gì ta cần.',
    '',                              -- str3 (ghi chú)
    2000,                            -- exp_reward
    200,                             -- gold_reward
    100,                             -- silver_reward
    '100@3',                         -- Item reward: item_id=100, qty=3
    '[{"id":1,"name":"Thu thập mảnh vàng","idItem":100,"idMob":-1,"idNpc":-1,"idMap":-1,"x":0,"y":0,"require":3,"STR":""}]',
    10,                              -- sort_order
    1                                -- is_active
  );
```

### 4.2 Reset & Reload Client Data

1. **Clear PlayerPrefs:** 
   ```csharp
   PlayerPrefs.DeleteAll();
   ```
2. **Logout → Login** để server resync player_data

---

## PHẦN 5: Xử Sự Cố (Troubleshooting)

### Vấn Đề: HUD không hiện quest

**Nguyên nhân & Cách Sửa:**

| Triệu Chứng | Nguyên Nhân | Cách Sửa |
|-------------|-----------|---------|
| HUD không hiện gì | `QuestHudWidget` chưa được thêm vào scene | Kéo `QuestHudWidget.prefab` vào Canvas |
| HUD hiện nhưng không update | `RefreshPlayerOverview()` không được gọi | Kiểm tra `QuestManager.Start()` gọi hay không |
| API error 401 | Token expired hoặc missing | Kiểm tra `AuthHelper.AddAuthHeader()` |
| Quest không xuất hiện trong NPC menu | NPC `npc_type != 'quest'` hoặc NPC không active | Chạy SQL kiểm tra: `SELECT * FROM npc_config WHERE npc_id = ?` |

### Vấn Đề: Click NPC không mở QuestNpcPanel

**Nguyên nhân & Cách Sửa:**

| Triệu Chứng | Nguyên Nhân | Cách Sửa |
|-------------|-----------|---------|
| Panel không mở | `QuestNpcPanel.prefab` chưa được thêm vào Resources/ | Tạo prefab: **DoAn → Quest → Create Quest NPC Panel** |
| Panel mở nhưng trống | Quest không được gán cho NPC (`npc_id` sai) | Kiểm tra DB: `SELECT * FROM quest_config WHERE npc_id = ?` |
| Panel mở nhưng không có nút quest | `questListContent` không được gán | Trong Inspector của `QuestNpcPanel`, gán Transform `Content` từ ScrollRect |

### Vấn Đề: Dialog không mở khi click quest

**Nguyên nhân & Cách Sửa:**

| Triệu Chứng | Nguyên Nhân | Cách Sửa |
|-------------|-----------|---------|
| Dialog không hiện | `QuestDialogueUI.prefab` chưa được tạo | Tạo: **DoAn → Quest → Create Quest Dialogue UI** |
| Dialog hiện nhưng chữ trống | `str1` hoặc `str2` bị NULL trong DB | Update quest: `UPDATE quest_config SET str1 = '...' WHERE id = ?` |
| Dialog không có button | `BtnAccept` / `BtnDecline` không được gán | Kiểm tra prefab hierarchy có đúng 4 button không |

### Vấn Đề: Quest không lưu vào Database

**Nguyên nhân & Cách Sửa:**

| Triệu Chứng | Nguyên Nhân | Cách Sửa |
|-------------|-----------|---------|
| Accept nhưng quest không active | `POST /api/quest/accept` thất bại | Kiểm tra Server console có error không, HTTP status |
| Progress không update | Server không nhận event từ Zone API | Kiểm tra Zone API gọi `/api/quest/progress-by-event` đúng header không |
| Quest không xuất hiện trong player_data | `active_quest_id` không được set | Server logic sai, check `QuestController.AcceptQuest()` |

---

## PHẦN 6: Cheat/Debug Commands (Optional)

### Reset Player Quest State
```csharp
// Trong QuestManager hoặc Debug console
var info = JsonSerializer.Deserialize<InfoChar>(PlayerPrefs.GetString("INFO_CHAR"));
info.ActiveQuestId = -1;
info.QuestStep = 0;
info.QuestProgress = new Dictionary<string, int>();
info.CompletedQuests = new List<int>();
PlayerPrefs.SetString("INFO_CHAR", JsonSerializer.Serialize(info));
```

### Force Refresh Quest HUD
```csharp
QuestManager.Instance?.RefreshPlayerOverview(() => {
    QuestHudWidget.Instance?.Refresh();
});
```

### Check Quest Config in DB
```bash
mysql -u root -p gamedb -e "SELECT id, name, npc_id, level_need, is_active FROM quest_config;"
```

---

## Checklist Hoàn Thành

- [ ] SQL migration chạy xong (10 quest mẫu có trong DB)
- [ ] NPC config: `npc_type = 'quest'`, `is_active = 1`
- [ ] Editor: DoAn → Quest → Create All Quest UI (4 prefabs tạo xong)
- [ ] Scene game: 3 prefab (Dialogue, NpcPanel, HudWidget) được thêm vào Canvas
- [ ] QuestNpcPanel: field "Quest Item Prefab" được gán
- [ ] QuestManager: Component được add vào scene
- [ ] Server build: 0 errors
- [ ] Client build: 0 errors
- [ ] Test Scenario 1: HUD hint quest hiện đúng
- [ ] Test Scenario 2: NPC menu mở, accept quest OK
- [ ] Test Scenario 3: HUD cập nhật progress khi làm quest
- [ ] Test Scenario 4: Hoàn thành quest, nộp, lấy thưởng OK

---

## Tham Khảo Thêm

- **Quest Config Details:** [HUONG_DAN_CONFIG_SKILL_SYSTEM.md](HUONG_DAN_CONFIG_SKILL_SYSTEM.md) (pattern tương tự)
- **NPC Config Guide:** [HUONG_DAN_CONFIG_NPC_DYNAMIC_MENU.md](HUONG_DAN_CONFIG_NPC_DYNAMIC_MENU.md)
- **Server API:** `GameServerApi/Controllers/QuestController.cs`
- **Client Code:** `Client/Assets/Scripts/Quest/` (QuestManager, QuestDialogueUI, QuestNpcPanel, QuestHudWidget)

