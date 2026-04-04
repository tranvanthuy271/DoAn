# HƯỚNG DẪN HỆ THỐNG BUFF ITEM (DoAn – Unity NGO)

> Tham khảo từ LangLaServer (LangLa MMO) và áp dụng vào dự án DoAn (Unity Netcode for GameObjects).

---

## 1. Tổng quan kiến trúc

```
[Nhấn "Sử dụng" trong UI]
        │
        ▼
ItemDetailPanel.OnUseButtonPressed()
        │
        ▼
ItemUseHandler.DoUseConsumableItem(slot)
        │
        ├─► POST /api/player/{id}/inventory/use-item   ← REST API (DB persist)
        │         ├── HP/MP Potion → hồi ngay, lưu hp/mp vào InfoChar
        │         └── Timed Buff   → thêm vào player_data.active_buffs
        │
        ▼  (callback onSuccess)
        ├─► NetworkInventory.ApplyConsumableStatServerRpc()   ← NGO (server-auth HP/MP)
        ├─► ActiveBuffManager.OnBuffsReceived(active_buffs)   ← cập nhật HUD client
        └─► InventoryNetworkBridge.RequestSyncBuffBonuses()   ← NGO sync % bonus stat
```

---

## 2. Cấu trúc DB mới

Chạy file migration:
```
GameServerApi/migration_item_buff_system.sql
```

### Bảng `item_effect_template`

| Cột            | Kiểu        | Ý nghĩa |
|----------------|-------------|---------|
| `id`           | INT PK      | Auto increment |
| `item_template_id` | INT    | FK → `item_template.id` |
| `effect_type`  | VARCHAR(50) | Loại effect (xem bảng dưới) |
| `value`        | INT         | Giá trị: số HP hồi HOẶC % tăng stat |
| `duration_sec` | INT         | 0 = instant; >0 = buff có thời hạn (giây) |
| `icon_id`      | INT         | ID icon trong IconDatabase (Unity) |
| `display_name` | VARCHAR(200)| Tên ngắn hiện trong tooltip ("EXP Gene +20%") |
| `detail`       | VARCHAR(500)| Mô tả dài ("+20% EXP Gene trong 30 phút") |

### Bảng `active_buffs` (cột JSON trong `player_data`)

```json
[
  {
    "effectType": "GeneExpBuff",
    "value": 20,
    "iconId": 121,
    "name": "Nhân Sâm Tâm Linh",
    "detail": "+20% EXP Gene (30 phút)",
    "expireAt": "2026-04-01T10:30:00Z"
  }
]
```

---

## 3. Các loại effect hỗ trợ

| `effect_type`  | `duration_sec` | Mô tả |
|----------------|:--------------:|-------|
| `HpRestore`    | **0** (instant) | Hồi `value` HP ngay lập tức |
| `MpRestore`    | **0** (instant) | Hồi `value` MP ngay lập tức |
| `HpBuff`       | > 0 (timed)    | Tăng max HP thêm `value` |
| `MpBuff`       | > 0 (timed)    | Tăng max MP thêm `value` |
| `AttackBuff`   | > 0 (timed)    | Tăng `value`% sát thương |
| `DefenseBuff`  | > 0 (timed)    | Tăng `value`% phòng thủ |
| `GeneExpBuff`  | > 0 (timed)    | Tăng `value`% EXP gene nạp vào |
| `ExpBuff`      | > 0 (timed)    | Tăng `value`% EXP khi kill enemy |
| `PhucBuff`     | > 0 (timed)    | Tăng `value`% vàng + EXP drop |

---

## 4. Loại item (`item_template.type`)

| Type | Tên         | Xử lý |
|:----:|-------------|-------|
| 22   | HP Potion   | Instant HP restore |
| 23   | MP Potion   | Instant MP restore |
| 24   | Timed Buff  | Thêm active buff có thời hạn |
| 30   | Bag Expand  | +5 ô túi đồ |

---

## 5. Hướng dẫn thêm item mới vào DB

### Ví dụ: Thêm "Đại Hồi Đan" (hồi 2000 HP)

```sql
-- Bước 1: Thêm vào item_template
INSERT INTO item_template
  (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed, sellPrice)
VALUES
  (105, 'Đại Hồi Đan', 'Hồi 2000 HP ngay lập tức.', 'True', 2, 22, 0, 105, 40, 500);

-- Bước 2: Thêm effect config
INSERT INTO item_effect_template
  (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail)
VALUES
  (105, 'HpRestore', 2000, 0, 105, 'Hồi máu', '+2000 HP');
```

### Ví dụ: Thêm "Bùa Thánh Thần" (tăng 30% EXP trong 2 giờ)

```sql
-- item_template
INSERT INTO item_template
  (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed, sellPrice)
VALUES
  (160, 'Bùa Thánh Thần', 'Tăng 30% EXP trong 2 giờ.', 'True', 2, 24, 0, 160, 30, 1000);

-- item_effect_template
INSERT INTO item_effect_template
  (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail)
VALUES
  (160, 'ExpBuff', 30, 7200, 160, 'EXP +30%', '+30% EXP (2 giờ)');
```

### Ví dụ: Item có nhiều effect (Vừa hồi HP vừa buff ATK)

```sql
INSERT INTO item_template VALUES (170, 'Chiến Đấu Đan', '...', 'True', 2, 24, 0, 170, 50, 2000);

-- Effect 1: instant HP restore
INSERT INTO item_effect_template
  (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail, sort_order)
VALUES
  (170, 'HpRestore', 1000, 0, 170, 'Hồi máu', '+1000 HP', 0);

-- Effect 2: timed ATK buff
INSERT INTO item_effect_template
  (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail, sort_order)
VALUES
  (170, 'AttackBuff', 20, 1800, 170, 'Công +20%', '+20% sát thương (30 phút)', 1);
```

---

## 6. Hướng dẫn cấu hình icon trong Unity

### 6.1 Thêm icon vào IconDatabase

1. Import ảnh icon vào `Assets/Resources/Icons/` hoặc folder của project.
2. Đặt tên file = ID (vd: `121.png` = icon ID 121).
3. Mở **IconDatabase** ScriptableObject trong Inspector.
4. Thêm entry: `ID = 121`, `Sprite = <kéo file 121.png vào>`.

> **Quy ước**: icon_id trong bảng DB phải khớp với ID trong IconDatabase Unity.

### 6.2 Tạo BuffIconPrefab

1. Tạo GameObject trong Canvas > HUD.
2. Thêm các con:
   ```
   BuffIcon (100×100 RectTransform)
   ├── IconImage    [Image]            ← gắn trường iconImage
   ├── TimerFill    [Image]            ← gắn trường timerFill
   │     Image Type = Filled
   │     Fill Method = Radial 360
   │     Fill Clockwise = false  (giảm ngược chiều kim đồng hồ = hết dần)
   │     Fill Origin = Top
   ├── TimeLabel    [TMP_Text]         ← gắn trường timeLabel
   │     Anchor = bottom center, size 24
   └── TooltipRoot  [GameObject]       ← gắn trường tooltipRoot (ẩn mặc định)
         ├── NameLabel   [TMP_Text]
         └── DetailLabel [TMP_Text]
   ```
3. Gắn **BuffIconUI** component.
4. Kéo thả các con vào các trường tương ứng trong Inspector.
5. Drag prefab này vào field `buffIconPrefab` của **BuffHUDPanel**.

### 6.3 Tạo BuffHUDPanel

1. Tạo một GameObject trong Canvas > HUD, đặt tên `BuffHUDPanel`.
2. Thêm **HorizontalLayoutGroup** (spacing = 4, child alignment = Middle Left).
3. Gắn **BuffHUDPanel** component.
4. Gán:
   - `buffIconPrefab` → prefab vừa tạo ở 6.2
   - `iconContainer` → Transform của chính `BuffHUDPanel` (hoặc một Child ScrollView)

### 6.4 Thêm ActiveBuffManager vào scene

1. Tạo GameObject `[BuffSystem]` trong scene (DontDestroyOnLoad).
2. Gắn **ActiveBuffManager** component.
3. Panel sẽ tự đăng ký sự kiện khi Enable.

---

## 7. Hiển thị thông tin tooltip

Khi hover vào icon, `BuffIconUI.OnPointerEnter` sẽ bật `TooltipRoot`.

**Dữ liệu hiển thị:**
- `name` → NameLabel ("EXP Gene +20%")
- `detail` → DetailLabel ("+20% EXP Gene trong 30 phút")
- Còn thiếu: `timeLabel` hiện thời gian còn lại realtime

---

## 8. Áp dụng buff vào combat

### Gene EXP (khi nạp gene)
```csharp
// Trong GeneManager hoặc PlayerExperienceManager
float bonus = 1f + (dataSync.networkGeneExpBonusPct.Value / 100f);
int finalGeneExp = Mathf.RoundToInt(baseGeneExp * bonus);
```

### EXP khi kill
```csharp
float expBonus = 1f + (dataSync.networkExpBonusPct.Value / 100f);
float phucBonus = 1f + (dataSync.networkPhucBonusPct.Value / 100f);
int finalExp = Mathf.RoundToInt(baseExp * expBonus * phucBonus);
```

### Vàng drop (PhucBuff)
```csharp
float goldBonus = 1f + (dataSync.networkPhucBonusPct.Value / 100f);
int finalGold = Mathf.RoundToInt(baseGold * goldBonus);
```

### Sát thương (AttackBuff / DefenseBuff)
```csharp
// AttackBuff: trong DamageCalculator khi tính dame deal
float atkMultiplier = 1f + (attackerDataSync.networkAttackBonusPct.Value / 100f);

// DefenseBuff: khi tính dame nhận
float defMultiplier = 1f - (defenderDataSync.networkDefenseBonusPct.Value / 100f);
int finalDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage * atkMultiplier * defMultiplier));
```

---

## 9. Buff biến mất khi hết thời gian

**Client-side:** `ActiveBuffManager` có coroutine `TrimExpiredBuffsLoop()` chạy mỗi giây:
- Xóa buff hết hạn khỏi `_activeBuffs`
- Fire `OnBuffListChanged` → `BuffHUDPanel.RefreshIcons()` → icon tự ẩn

**Server-side sync:** Buff % bonus trong `NetworkPlayerDataSync` sẽ bị reset về 0 khi:
- Server gọi lại `SyncBuffBonusesServerRpc(0,0,0,0,0)` sau khi buff expire

> **Hiện tại:** Server không tự detect expire buff. Để đầy đủ, thêm background service trong GameServerApi kiểm tra `active_buffs` định kỳ, hoặc client gọi `RequestSyncBuffBonuses()` sau khi detect buff expire.

---

## 10. Item có sẵn (từ migration)

| ID  | Tên                     | Type | Effect |
|:---:|-------------------------|:----:|--------|
| 101 | Thuốc Hồi Máu Nhỏ      | 22   | +200 HP |
| 102 | Thuốc Hồi Máu Vừa      | 22   | +500 HP |
| 103 | Thuốc Hồi Máu Lớn      | 22   | +1200 HP |
| 104 | Đan Hồi Sinh            | 22   | HP full |
| 111 | Thuốc Hồi Linh Nhỏ     | 23   | +150 MP |
| 112 | Thuốc Hồi Linh Vừa     | 23   | +400 MP |
| 113 | Thuốc Hồi Linh Lớn     | 23   | +1000 MP |
| 121 | Nhân Sâm Tâm Linh      | 24   | GeneExp+20% (30 phút) |
| 122 | Nhân Sâm Thần Thánh    | 24   | GeneExp+50% (30 phút) |
| 123 | Nhân Sâm Thiên Hạ      | 24   | GeneExp+100% (1 giờ) |
| 131 | Nén Hương Kinh Nghiệm  | 24   | Exp+25% (30 phút) |
| 132 | Nén Hương Thần Thánh   | 24   | Exp+50% (1 giờ) |
| 141 | Bùa Phúc Nhỏ           | 24   | Phúc+10% vàng+EXP (1 giờ) |
| 142 | Bùa Phúc Lớn           | 24   | Phúc+25% vàng+EXP (2 giờ) |
| 151 | Bùa Tăng Công Nhỏ      | 24   | ATK+15% (30 phút) |
| 152 | Bùa Phòng Thủ Nhỏ      | 24   | DEF+15% (30 phút) |

---

## 11. Checklist vận hành

- [ ] Chạy `migration_item_buff_system.sql` trên DB
- [ ] Thêm icon vào **IconDatabase** (ID 101-152 tối thiểu)
- [ ] Tạo **BuffIconPrefab** với radial TimerFill
- [ ] Đặt **BuffHUDPanel** trong Canvas HUD (trên / dưới thanh HP)
- [ ] Thêm `[BuffSystem]` GameObject với **ActiveBuffManager** vào scene
- [ ] Gọi `ActiveBuffManager.Instance.LoadFromServer()` sau khi vào game/đăng nhập thành công
- [ ] Tích hợp `networkGeneExpBonusPct` / `networkExpBonusPct` vào logic cộng EXP
- [ ] Tích hợp `networkPhucBonusPct` vào drop vàng
- [ ] Tích hợp `networkAttackBonusPct` / `networkDefenseBonusPct` vào DamageCalculator
