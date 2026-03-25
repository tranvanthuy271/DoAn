# Hướng Dẫn Config Inventory UI trong Unity

> **Không cần viết code thêm** — chỉ cần config prefab và scene theo hướng dẫn dưới đây.

---

## 1. Cấu trúc Hierarchy Panel Inventory

```
Canvas (Main)
└── InventoryPanel                ← bật/tắt khi mở/đóng túi
    ├── Header
    │   └── TitleText              (TMP_Text) "Túi đồ"
    ├── StatBar                   ← thanh hiển thị vàng/bạc/số ô
    │   ├── GoldIcon               (Image)
    │   ├── GoldText               (TMP_Text) → gán vào ItemUseHandler ▸ Gold Text
    │   ├── SilverIcon             (Image)
    │   ├── SilverText             (TMP_Text) → gán vào ItemUseHandler ▸ Silver Text
    │   ├── BagIcon                (Image)
    │   └── BagSlotCountText       (TMP_Text) → gán vào ItemUseHandler ▸ Bag Slot Count Text
    ├── BagQuickSlots             ← 3 ô hiển thị item túi đang có
    │   ├── BagSlot0
    │   │   ├── Icon (Image)       → gán vào ItemUseHandler ▸ Bag Quick Slot Icons [0]
    │   │   └── Count (TMP_Text)   → gán vào ItemUseHandler ▸ Bag Quick Slot Counts [0]
    │   ├── BagSlot1
    │   │   ├── Icon (Image)       → [1]
    │   │   └── Count (TMP_Text)   → [1]
    │   └── BagSlot2
    │       ├── Icon (Image)       → [2]
    │       └── Count (TMP_Text)   → [2]
    ├── SlotGrid                  ← Grid chứa các ô item
    │   └── (Instantiate lúc runtime từ prefab SlotItem)
    └── Footer
        ├── BtnBaoMat              (Button) "Bảo mật"
        └── BtnSapXep              (Button) "Sắp xếp" → gán vào ItemUseHandler ▸ Sort Button
```

---

## 2. Config Grid Layout Group cho SlotGrid

1. Chọn GameObject **SlotGrid** → Add Component → **Grid Layout Group**
2. Cấu hình:

| Thuộc tính | Giá trị đề xuất |
|---|---|
| Cell Size | 60 x 60 |
| Spacing | 4 x 4 |
| Start Corner | Upper Left |
| Start Axis | Horizontal ← **bắt buộc** để xếp trái → phải rồi xuống |
| Child Alignment | Upper Left |
| Constraint | Flexible |

3. Add thêm **Content Size Fitter** lên SlotGrid:
   - Vertical Fit → **Preferred Size** (tự giãn theo số item)

4. Nếu danh sách dài: đặt **SlotGrid** bên trong **Scroll View** (Vertical Scroll).

---

## 3. Prefab SlotItem (1 ô túi đồ)

Tạo prefab `SlotItem.prefab` với hierarchy:

```
SlotItem                        ← gắn InventorySlotUI + Button (OnClick → InventorySlotUI.OnClick)
├── BG (Image)                  ← nền ô
├── ItemIcon (Image)            → gán vào InventorySlotUI ▸ Icon Image
├── QuantityText (TMP_Text)     → gán vào InventorySlotUI ▸ Quantity Text
├── EquippedMark (Image/GO)     → gán vào InventorySlotUI ▸ Equipped Mark
│   (hiện khi item đang trang bị — tắt mặc định)
└── LockMark (Image/GO)         → gán vào InventorySlotUI ▸ Lock Mark
    (hiện khi isLocked = true — dùng sprite ổ khoá)
```

**Lưu ý lock/unlock:**
- Tạo thêm một ảnh ổ khoá nhỏ (góc trên-trái hoặc dưới-phải của ô).
- Kéo GameObject đó vào field **Lock Mark** trong `InventorySlotUI`.
- Runtime script sẽ tự `SetActive(true/false)` theo `isLocked` của từng item.

---

## 4. Config InventoryUI trên Panel

Chọn **InventoryPanel** → thêm component `InventoryUI`, kéo:

| Field | Gán vào |
|---|---|
| Inventory Root | InventoryPanel (chính nó) |
| Slot Container | SlotGrid |
| Slot Prefab | SlotItem.prefab |
| Item Detail Panel Prefab | ItemDetailPanel.prefab |
| Item Detail Panel Parent | (để trống → tự tìm Canvas root) |
| Max Slot Count | **để trống / 0** — script sẽ dùng `bag_slots` từ server |

---

## 5. Config ItemUseHandler

Tạo một **GameObject rỗng** trong scene, đặt tên `InventoryManager`.  
Add component `ItemUseHandler`, kéo:

| Field | Gán vào |
|---|---|
| Inventory Bridge | InventoryNetworkBridge trong scene |
| Inventory UI | InventoryPanel (có InventoryUI) |
| Gold Text | StatBar → GoldText |
| Silver Text | StatBar → SilverText |
| Bag Slot Count Text | StatBar → BagSlotCountText |
| Bag Quick Slot Icons | [0] BagSlot0/Icon, [1] BagSlot1/Icon, [2] BagSlot2/Icon |
| Bag Quick Slot Counts | [0] BagSlot0/Count, [1] BagSlot1/Count, [2] BagSlot2/Count |
| Empty Slot Sprite | Sprite nền ô trống (tùy chọn) |
| Sort Button | Footer → BtnSapXep |

---

## 6. Nút Sắp xếp (BtnSapXep)

1. Thêm Button vào Footer với text **"Sắp xếp"**.
2. **Không cần gán OnClick qua inspector** — `ItemUseHandler.Start()` tự subscribe `sortButton.onClick`.
3. Gán Button object vào field **Sort Button** của `ItemUseHandler`.

Khi nhấn:
- Client gọi `POST /api/player/{id}/inventory/sort`
- Server gom item về đầu dãy, re-index slotIndex
- Client refresh lại UI

---

## 7. Ba ô Quick Slot mở rộng túi

Mỗi ô gồm:
- `Image` (icon túi đồ) — `ItemUseHandler` tự update khi thấy item type=30 trong inventory
- `TMP_Text` (số lượng)

Khi người chơi **dùng item mở rộng túi** (bất kỳ slot nào trong túi đồ chứa item type=30):
1. Chọn item → nhấn **Sử dụng** trong ItemDetailPanel
2. `ItemUseHandler` nhận biết type=30 → gọi API `use-item`
3. Server tăng `bag_slots` += 5, xóa 1 item
4. Client nhận `bag_slots` mới → cập nhật `BagSlotCountText` + Quick Slots

---

## 8. Hiển thị Vàng / Bạc / Số ô túi

- **Vàng / Bạc**: lấy từ `PlayerDataResponse.gold` và `.silver` sau khi đăng nhập / refresh.
- **Số ô túi**: lấy từ `PlayerDataResponse.bag_slots` (mặc định = 20).
- Format lớn hơn 1000 sẽ hiển thị dạng `1.5K`, `2.3M`.
- `ItemUseHandler.RefreshStatBar()` gọi ngay trong `Start()` và sau mỗi lần dùng item.

---

## 9. Item có trạng thái Khóa (isLocked)

- Slot có `isLocked = true` sẽ hiện **LockMark** (ảnh ổ khoá).
- Template item có `isLock = true` (ở ItemTemplate DB) → item drop ra sẽ mang cờ này.
- Để hiển thị đúng: gán `LockMark` GameObject vào `InventorySlotUI` trong prefab SlotItem.
- Khi bán/drop, server sẽ từ chối nếu `isLocked = true`.

---

## 10. ItemDetailPanel Prefab

```
ItemDetailPanel                 ← gắn ItemDetailPanel script + Canvas (sortingOrder=200)
├── Background (Image)
├── ItemIcon (Image)            → gán vào ItemDetailPanel ▸ Item Icon
├── ItemName (TMP_Text)         → Item Name Text
├── ItemDescription (TMP_Text)  → Item Description Text
├── BtnUse (Button)             → Use Button
│   └── BtnUseText (TMP_Text)   → Use Button Text
└── BtnClose (Button)           → Btn Close
```

Button **Sử dụng** tự động:
- Category 1 (trang bị) → text = "Trang bị" → gọi equip API
- Type 30 (túi) → text = "Sử dụng" → gọi use-item API → mở rộng túi
- Type 21-29 (tiêu thụ) → text = "Sử dụng" → gọi use-item API

---

## 11. Lưu ý khi Debug

- Mở Console Unity, lọc `[ItemUseHandler]` để xem log sử dụng item.
- Lọc `[InventoryNetworkBridge]` để xem luồng fetch/sync DB.
- Kiểm tra `bag_slots` trong DB bằng: `GET /api/player/{id}/data` → trường `bag_slots`.
- Sắp xếp inventori thủ công qua Postman: `POST /api/player/{id}/inventory/sort` (body: `{}`).
