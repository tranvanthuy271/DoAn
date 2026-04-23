# Hướng Dẫn Config Bag Quick Slots – Phiên Bản Đầy Đủ

## Mục tiêu sau khi config xong

| Tính năng | Trạng thái |
|-----------|-----------|
| Dùng item túi → tăng `bag_slots` và lưu vào `bag_equipped_items` | ✅ |
| 3 ô nhanh (Quick Slots) ở góc trái dưới InventoryPanel hiển thị icon item túi đang mang | ✅ |
| Click ô nhanh → panel **"Tên túi – Cất vào / Xem"** | ✅ |
| "Cất vào" trả item về túi đồ + giảm `bag_slots` đúng với item đã dùng | ✅ |
| Số ô hiển thị = `bag_slots` của player (không phải `maxSlotCount` cố định) | ✅ |
| Max `bag_slots` = 20 + 3 × 5 = **35** (3 túi, mỗi túi +5 ô) | ✅ |
| Lệnh chat `item <id> <số>` để thêm item vào túi (debug/GM) | ✅ |

---

## 1. DB Migration – Thêm item túi mở rộng

Chạy file `SQL/migrate_bag_expansion_items.sql` trên DB của bạn **một lần duy nhất**.

File này:
1. Đổi comment column `type` trong `item_template` (thêm `32=BagExpansion`).
2. Thêm 4 item túi mở rộng (ID 61–64, type = **32**).

> **Tại sao type 32 không phải type 30?**  
> `type = 30` đã được dùng cho vật liệu thông thường (Quặng Sắt, Thảo Dược…).  
> Nếu dùng chung type 30 cho túi mở rộng, bất kỳ vật liệu nào cũng sẽ kích hoạt mở túi khi player sử dụng → bug nghiêm trọng.  
> `type = 32` là loại riêng biệt, chỉ dành cho túi mở rộng.

### Bảng item túi mở rộng sau migration

| ID | Tên | Level cần | Loại | Bonus |
|----|-----|-----------|------|-------|
| 61 | Túi Mở Rộng Cấp 1 | 1 | 32 | +5 ô |
| 62 | Túi Mở Rộng Cấp 2 | 10 | 32 | +5 ô |
| 63 | Túi Mở Rộng Cấp 3 | 25 | 32 | +5 ô |
| 64 | Túi Mở Rộng Cấp 4 | 40 | 32 | +5 ô |

### Thêm icon item túi

`idIcon = 0` trong migration = chưa có sprite. Sau khi chạy migration:

```sql
-- Thay <ID_ICON_CẤP_N> bằng số thực trong atlas
UPDATE item_template SET idIcon = <ID_ICON_CẤP_1> WHERE id = 61;
UPDATE item_template SET idIcon = <ID_ICON_CẤP_2> WHERE id = 62;
UPDATE item_template SET idIcon = <ID_ICON_CẤP_3> WHERE id = 63;
UPDATE item_template SET idIcon = <ID_ICON_CẤP_4> WHERE id = 64;
```

**Tìm icon ID hợp lệ:**  
Mở `Assets/Resources/ItemIcons/` trong Unity Project window → các sprite ở đây được đặt tên bằng số (vd. `409`, `246`…).  
`idIcon = 409` nghĩa là dùng sprite tên `"409"` trong thư mục đó.

**Ưu tiên cách 2 – gán `defaultBagIcon` trong Inspector (không cần DB):**  
Xem mục **2 → ItemUseHandler Inspector** bên dưới.

### Thêm vào NPC Shop (tuỳ chọn)

Bỏ comment phần `INSERT INTO npc_shop_item` ở cuối file migration rồi chỉnh `npc_id`, giá theo nhu cầu.

---

## 2. Config InventoryPanel trong Unity – GameScene

### Mở scene

Mở `Assets/Scenes/GameScene.unity`.

### InventoryUI – Inspector

Tìm object **InventoryPanel**, chọn component **InventoryUI**.

| Field | Giá trị đúng |
|-------|-------------|
| `inventoryRoot` | Chính InventoryPanel (hoặc sub-panel chứa grid) |
| `slotContainer` | Transform có Grid Layout Group chứa ô slot |
| `slotPrefab` | Prefab `InventorySlotUI` |
| `itemDetailPanelPrefab` | Prefab `ItemDetailPanel` |
| `itemDetailPanelParent` | Root Canvas (ScreenSpaceCanvas) – để trống sẽ tự tìm |
| `maxSlotCount` | **35** (= 20 base + 3 túi × 5 ô tối đa) |

> ⚠️ **QUAN TRỌNG về `maxSlotCount`:**  
> Đây là **kích thước POOL** (số slot UI tạo ra), **KHÔNG** phải số ô thực tế hiển thị.  
> Số ô hiển thị thực sự do `bag_slots` từ server quyết định (`SetVisibleSlotCount(bag_slots)`).  
> **Không đặt giá trị vượt quá 35** – max thực tế = 20 + 3×5 = 35.

### BagQuickSlots – Cấu trúc Hierarchy

```
InventoryPanel
  └── BagQuickSlots
        ├── BagSlot0   (index 0 – trái nhất)
        ├── BagSlot1   (index 1)
        └── BagSlot2   (index 2)  ← đổi tên nếu đang là "BagSlot3"
```

Mỗi `BagSlotN` cần:

```
BagSlotN
  ├── Icon       (Image – hiển thị icon item túi)
  └── CountText  (TMP_Text – hiển thị "+cấp", vd. "+4")
```

Không cần thêm Button thủ công – `ItemUseHandler.SetupBagQuickSlotButtons()` tự `AddComponent<Button>()` lúc runtime.

### ItemUseHandler (InventoryManager) – Inspector

| Field | Gán gì |
|-------|--------|
| `inventoryBridge` | `InventoryNetworkBridge` trong scene |
| `inventoryUI` | Component `InventoryUI` của InventoryPanel |
| `goldText` | TMP_Text hiển thị vàng |
| `silverText` | TMP_Text hiển thị bạc |
| `bagSlotCountText` | TMP_Text "X/Y ô túi" |
| `bagQuickSlotIcons[0]` | Image **Icon** trong BagSlot0 |
| `bagQuickSlotIcons[1]` | Image **Icon** trong BagSlot1 |
| `bagQuickSlotIcons[2]` | Image **Icon** trong BagSlot2 |
| `bagQuickSlotCounts[0]` | TMP_Text **CountText** trong BagSlot0 |
| `bagQuickSlotCounts[1]` | TMP_Text **CountText** trong BagSlot1 |
| `bagQuickSlotCounts[2]` | TMP_Text **CountText** trong BagSlot2 |
| `emptySlotSprite` | Sprite nền khi ô trống (tuỳ chọn) |
| **`defaultBagIcon`** | **Sprite hiện trên quick slot khi túi chưa có icon riêng (idIcon = 0). Gán bất kỳ sprite "túi" nào từ atlas.** |
| `lockIcon` | Sprite khoá item |
| `sortButton` | Button **Sắp xếp** trong InventoryPanel |

> ℹ️ **Về `defaultBagIcon`:**  
> Đây là fallback icon cho bag quick slot khi item túi chưa có `idIcon` được cấu hình trong DB.  
> Thứ tự ưu tiên icon:  
> `icon_id` từ `bag_equipped_items` → `idIcon` trong `item_template` → **`defaultBagIcon`** → `emptySlotSprite`  
> → Nếu bạn không muốn set `idIcon` cho từng item trong DB, chỉ cần gán **`defaultBagIcon`** là quick slot sẽ có hình.

---

## 3. Luồng hoạt động

### Dùng item túi (type 32)

```
Player click "Sử dụng" trên item túi
  → ItemUseHandler.RequestUseItem → itemType == 32 → DoUseBagItem()
  → GameplayCommandService.UseInventoryItemServerRpc
  → Server: type == 32 → bag_slots += 5, lưu bag_equipped_items
  → Client nhận JSON: bag_slots + bag_equipped_items
  → UpdateBagQuickSlots() → icon hiện trên ô nhanh
  → SetVisibleSlotCount(bag_slots) → thêm ô trong lưới
  → bagSlotCountText cập nhật
```

### Click ô nhanh có item túi

```
Click BagSlot0/1/2
  → BagQuickActionPanel.Show("Túi Mở Rộng Cấp X")
  → Panel hiện:
      ┌──────────────────────────────┐
      │   Túi Mở Rộng Cấp 4         │
      │  [Cất vào]     [Xem]        │
      └──────────────────────────────┘

"Cất vào":
  → RequestUnequipBagQuickSlot → server giảm bag_slots, trả item về túi
  → Nếu túi đầy → server từ chối, thông báo lỗi

"Xem":
  → ItemDetailPanel hiện thông tin item (không có nút Sử dụng)
```

---

## 4. Fix lỗi hiển thị nhiều ô hơn bag_slots

### Nguyên nhân phổ biến

| Nguyên nhân | Giải pháp |
|-------------|----------|
| `maxSlotCount = 52` trong Inspector | Đổi thành **35** |
| `bag_slots > 35` trong DB do bug | Xem SQL fix bên dưới |
| `SetVisibleSlotCount` chưa được gọi sau refresh | Đã fix: `SyncVisibleSlotCountFromPlayerData()` gọi khi mở túi |

### SQL fix bag_slots > 35

```sql
UPDATE player_data
SET info_char = JSON_SET(
      JSON_SET(info_char, '$.bag_slots', 20),
      '$.bag_equipped_items', JSON_ARRAY()
    )
WHERE JSON_EXTRACT(info_char, '$.bag_slots') > 35;
```

---

## 5. Dữ liệu server – bag_equipped_items

`info_char.bag_equipped_items` là mảng JSON:

```json
[
  {
    "quick_slot_index": 0,
    "item_template_id": 64,
    "item_code": "",
    "item_name": "Túi Mở Rộng Cấp 4",
    "icon_id": 583,
    "upgrade_level": 4,
    "str_options": "",
    "slot_bonus": 5,
    "is_locked": false
  }
]
```

---

## 6. Xử lý dữ liệu cũ (player đã có bag_slots > 20 nhưng bag_equipped_items rỗng)

### Cách A – Reset về 20 (đơn giản nhất)

```sql
UPDATE player_data
SET info_char = JSON_SET(
      JSON_SET(info_char, '$.bag_slots', 20),
      '$.bag_equipped_items', JSON_ARRAY()
    )
WHERE JSON_EXTRACT(info_char, '$.bag_slots') > 20
  AND (info_char NOT LIKE '%bag_equipped_items%'
       OR JSON_LENGTH(JSON_EXTRACT(info_char, '$.bag_equipped_items')) = 0);
```

Sau đó cấp lại item túi cho player.

### Cách B – Điền thủ công (biết chính xác player đã dùng gì)

```sql
UPDATE player_data
SET info_char = JSON_SET(info_char, '$.bag_equipped_items', JSON_ARRAY(
  JSON_OBJECT(
    'quick_slot_index', 0,
    'item_template_id', 62,
    'item_code', '',
    'item_name', 'Túi Mở Rộng Cấp 2',
    'icon_id', 0,
    'upgrade_level', 2,
    'str_options', '',
    'slot_bonus', 5,
    'is_locked', false
  )
))
WHERE player_id = <ID_PLAYER>;
```

---

## 7. Checklist test nhanh

- [ ] Chạy `SQL/migrate_bag_expansion_items.sql`
- [ ] Inspector: `maxSlotCount = 35`
- [ ] Inspector: gán đủ `bagQuickSlotIcons[0..2]` và `bagQuickSlotCounts[0..2]`
- [ ] Inspector: gán `defaultBagIcon` = sprite bất kỳ trông giống túi từ atlas
- [ ] Đổi tên BagSlot3 → BagSlot2 nếu chưa đổi
- [ ] Dùng item túi ID 61–64 từ inventory
- [ ] Ô nhanh hiện icon (defaultBagIcon) + "+cấp"
- [ ] `bagSlotCountText` tăng đúng (vd. 20 → 25)
- [ ] Số ô trong lưới tăng đúng
- [ ] Click ô nhanh → panel "Cất vào / Xem"
- [ ] "Xem" → ItemDetailPanel (không có nút Sử dụng)
- [ ] "Cất vào" → item về túi, slot giảm
- [ ] Cất khi túi đầy → server từ chối + thông báo lỗi
- [ ] Thoát / đăng nhập lại → quick slots và bag_slots đúng
- [ ] Gõ `item 61 1` trong kênh Lân cận → nhận Túi Mở Rộng Cấp 1
- [ ] Gõ `item 11 50` → nhận 50 Bình HP Nhỏ (gộp stack nếu đã có)

---

## 8. Lệnh chat debug: `item <itemId> <sốLượng>`

### Cách dùng

Trong game, gõ lệnh sau vào **kênh Lân cận (Proximity)**:

```
item 61 1
```

→ Server thêm 1x **Túi Mở Rộng Cấp 1** vào túi đồ của bạn và gửi thông báo hệ thống về client.

### Cú pháp

```
item <itemTemplateId> <sốLượng>
```

| Tham số | Ý nghĩa |
|---------|---------|
| `itemTemplateId` | ID của item trong bảng `item_template` |
| `sốLượng` | Số lượng (1–9999) |

### Ví dụ

| Lệnh | Kết quả |
|------|---------|
| `item 61 1` | Thêm 1 Túi Mở Rộng Cấp 1 |
| `item 11 50` | Thêm 50 Bình HP Nhỏ |
| `item 1 10` | Thêm 10 Đá Nâng Cấp Cấp 1 |

### Phản hồi

Server gửi về client qua `ReceiveSystemMessage`. ChatManager hiển thị trong tab **Lân cận** dạng:

```
[Hệ thống] Đã thêm 1x Túi Mở Rộng Cấp 1 vào túi đồ.
```

### Lưu ý

- Lệnh chỉ hoạt động khi gõ **kênh Lân cận** (Proximity) — không broadcast ra ngoài.
- Item stackable (isXepChong = True) sẽ gộp vào stack đã có.
- Nếu túi đầy, server trả về thông báo "Túi đồ đầy".
- **Mở lại inventory sau khi nhận lệnh** để thấy item mới (inventory tự refresh khi toggle).
- Đây là lệnh debug — **không có kiểm tra quyền** (bất kỳ player nào cũng dùng được). Nếu cần restrict, thêm kiểm tra `isAdmin` trong `TryHandleChatCommandAsync`.

---

## 9. Các file code đã thay đổi

| File | Thay đổi |
|------|---------|
| `GameServerApi/Controllers/PlayerController.cs` | `BagItemType = 32`, cap `ResolveBagSlotLimit` tối đa 35 |
| `GameServerApi/Hubs/ChatHub.cs` | Inject `IServiceScopeFactory`, lệnh `item <id> <qty>` trong proximity chat |
| `Client/Assets/Scripts/Inventory/Handlers/ItemUseHandler.cs` | `ItemTypeBag = 32`, thêm field `defaultBagIcon`, fallback icon chain |
| `Client/Assets/Scripts/Inventory/UI/InventoryUI.cs` | `maxSlotCount` default → 35, `SyncVisibleSlotCountFromPlayerData()`, `InitSlots()` fix |
| `Client/Assets/Scripts/Inventory/UI/BagQuickActionPanel.cs` | Nút "Tháo rời" → **"Cất vào"**, 2 nút xếp ngang |
| `Client/Assets/Scripts/Chat/ChatManager.cs` | Đăng ký `ReceiveSystemMessage`, `ReceiveSystemMessage()` handler |
| `SQL/migrate_bag_expansion_items.sql` | Migration thêm item túi ID 61–64 (type 32) |

> `Client_clone_0` đã được đồng bộ tự động với `Client`.

## 10. Ghi chú cho Client Clone (ParrelSync)

Nếu dùng `Client_clone_0` để test host/client:

- `Assembly-CSharp.csproj` của clone có thể chưa regenerate kịp khi thêm file `.cs` mới.
- Mở Unity hoặc regen project files nếu IDE chưa thấy file mới.
- Code logic đã được đồng bộ cho cả `Client` và `Client_clone_0`.
