# Hướng Dẫn Setup Dữ Liệu Inventory

Tài liệu này hướng dẫn cách tạo dữ liệu inventory với các iconId từ DB:
- `client_icon_121` - Hồi Máu Nhỏ
- `client_icon_142` - Hồi Mana Nhỏ  
- `client_icon_152` - Đá Quý Thường
- `client_icon_167` - Kiếm Đồng

---

## 1. Setup Database (SQL)

### Bước 1: Chạy SQL Script

1. Mở MySQL/MariaDB client (phpMyAdmin, MySQL Workbench, hoặc command line).
2. Chọn database `gamedb`.
3. Chạy file `inventory_data_setup.sql`:
   - Script sẽ:
     - Tạo bảng `item_template` (nếu chưa có).
     - Insert 4 item template với `icon_id` tương ứng.
     - Update `player_data.inventory` cho `player_id = 1` với JSON chứa 4 items.

### Bước 2: Kiểm Tra Kết Quả

Chạy query sau để xem inventory đã được tạo:

```sql
SELECT 
    player_id,
    character_name,
    JSON_PRETTY(inventory) AS inventory_json
FROM player_data
WHERE player_id = 1;
```

Bạn sẽ thấy JSON dạng:

```json
[
  {
    "slotIndex": 0,
    "itemCode": "ITEM_ICON_121",
    "itemTemplateId": 1,
    "iconId": "client_icon_121",
    "quantity": 5,
    "isEquipped": false
  },
  {
    "slotIndex": 1,
    "itemCode": "ITEM_ICON_142",
    "itemTemplateId": 2,
    "iconId": "client_icon_142",
    "quantity": 3,
    "isEquipped": false
  },
  {
    "slotIndex": 2,
    "itemCode": "ITEM_ICON_152",
    "itemTemplateId": 3,
    "iconId": "client_icon_152",
    "quantity": 10,
    "isEquipped": false
  },
  {
    "slotIndex": 3,
    "itemCode": "ITEM_ICON_167",
    "itemTemplateId": 4,
    "iconId": "client_icon_167",
    "quantity": 1,
    "isEquipped": true
  }
]
```

---

## 2. Setup Unity - Icon Sprites

### Bước 1: Đảm Bảo Có Sprite Icons

1. Trong Unity, mở thư mục `Assets/Resources/ItemIcons`.
2. Đảm bảo có 4 sprite với tên chính xác:
   - `client_icon_121` (sprite cho Hồi Máu Nhỏ)
   - `client_icon_142` (sprite cho Hồi Mana Nhỏ)
   - `client_icon_152` (sprite cho Đá Quý Thường)
   - `client_icon_167` (sprite cho Kiếm Đồng)

> **Lưu ý**: Tên sprite phải **trùng 100%** với `iconId` trong DB (không có extension `.png`, `.jpg`, v.v.)

### Bước 2: Kiểm Tra IconDatabase

1. Trong scene, đảm bảo có GameObject `IconDatabase` với script `IconDatabase`.
2. Bấm Play và check Console:
   ```
   [IconDatabase] Loaded 4 item icons from Resources/ItemIcons
   ```
3. Nếu thiếu icon, bạn sẽ thấy warning:
   ```
   [IconDatabase] IconId 'client_icon_XXX' not found in cache.
   ```

---

## 3. Test Inventory Trong Unity (Dùng InventoryTestData)

### Bước 1: Setup InventoryTestData

1. Trong scene game, tạo GameObject `InventoryTestData`.
2. Gắn script `InventoryTestData` vào.
3. Trong Inspector:
   - **Test Inventory**: 
     - Kéo `NetworkInventory` của Player vào (nếu player đã có trong scene).
     - Hoặc bật `Auto Find Inventory` để script tự tìm.
   - **Test Items**: 
     - `Item 1 ID` = 1 (tương ứng với `item_template.id = 1` trong DB).
     - `Item 1 Quantity` = 5.
     - Tương tự cho Item 2, 3, 4.

### Bước 2: Test Thêm Items

**Cách 1: Dùng Context Menu (nhanh nhất)**

1. Bấm Play (ở **Host mode** - vì chỉ server mới thêm được item).
2. Trong Hierarchy, chọn GameObject `InventoryTestData`.
3. Trong Inspector, click vào **3 chấm (⋮)** ở góc trên phải của component `InventoryTestData`.
4. Chọn:
   - `Add All Test Items` → thêm tất cả 4 items.
   - Hoặc `Add Item 1`, `Add Item 2`, ... để thêm từng item.

**Cách 2: Gọi Từ Code**

```csharp
InventoryTestData testData = FindObjectOfType<InventoryTestData>();
testData.AddAllTestItems();
```

### Bước 3: Kiểm Tra UI

1. Bấm nút túi đồ (`InventoryButton`).
2. Panel inventory mở → bạn sẽ thấy:
   - Slot 0: `client_icon_121` x5 (Hồi Máu Nhỏ)
   - Slot 1: `client_icon_142` x3 (Hồi Mana Nhỏ)
   - Slot 2: `client_icon_152` x10 (Đá Quý Thường)
   - Slot 3: `client_icon_167` x1 (Kiếm Đồng) - có mark "Equipped"

---

## 4. Lưu Ý Quan Trọng

### Mapping ItemID

- **ItemID trong Unity** (`ItemData.itemID` hoặc `item_template.id` trong DB) phải **trùng** với ID bạn dùng trong `InventoryTestData`.
- Ví dụ:
  - DB: `item_template.id = 1` → Unity: `ItemData.itemID = 1` → `InventoryTestData.item1ID = 1`
  - Nếu không trùng, `NetworkInventory` sẽ không tìm thấy `ItemData` và bỏ qua.

### IconId Mapping

- **iconId trong DB** (`item_template.icon_id`) phải **trùng** với tên sprite trong Unity (`Resources/ItemIcons/client_icon_XXX`).
- `InventoryNetworkBridge` sẽ dùng `itemData.icon.name` làm `iconId` → map với `IconDatabase`.

### Nếu Không Thấy Icon

1. Check Console:
   - `[IconDatabase] IconId 'XXX' not found` → thiếu sprite hoặc tên sai.
2. Kiểm tra:
   - Sprite có trong `Resources/ItemIcons` không?
   - Tên sprite có trùng với `iconId` trong DB không?
   - `IconDatabase` đã load chưa? (check log khi Start).

---

## 5. Format JSON Inventory (Tham Khảo)

File `inventory_sample_data.json` chứa format JSON mẫu mà server nên gửi cho client:

```json
[
  {
    "slotIndex": 0,
    "itemCode": "ITEM_ICON_121",
    "itemTemplateId": 1,
    "iconId": "client_icon_121",
    "quantity": 5,
    "isEquipped": false
  }
]
```

Server của bạn nên:
1. Đọc `player_data.inventory` từ DB (JSON).
2. Parse JSON → `InventorySlotDto[]`.
3. Gửi cho Unity client qua network.
4. Unity parse → gọi `InventoryUI.SetInventoryData(slots)`.

---

## 6. Checklist Nhanh

- [ ] Chạy `inventory_data_setup.sql` trong DB.
- [ ] Kiểm tra `item_template` có 4 records với `icon_id` đúng.
- [ ] Kiểm tra `player_data.inventory` có JSON với 4 items.
- [ ] Unity: có 4 sprite trong `Resources/ItemIcons` với tên trùng `iconId`.
- [ ] Unity: `IconDatabase` load được 4 icons (check Console).
- [ ] Unity: `ItemData` có `itemID` trùng với `item_template.id` trong DB.
- [ ] Unity: Test thêm items bằng `InventoryTestData` → mở túi đồ → thấy 4 items hiển thị đúng icon.

Sau khi hoàn thành, bạn sẽ có inventory hoàn chỉnh với 4 items hiển thị đúng icon từ DB!
