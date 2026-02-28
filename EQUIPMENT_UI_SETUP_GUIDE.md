# Hướng dẫn cấu hình Equipment UI trong Unity

## Tổng quan hệ thống trang bị

Hệ thống trang bị gồm 6 slots:
| Slot | EquipmentSlotType | item_type (DB) |
|------|-------------------|----------------|
| Vũ khí | Weapon (0) | 1, 2 |
| Mũ | Helmet (1) | 4 |
| Giáp | Armor (2) | 3 |
| Quần | Pants (3) | 5 |
| Giày | Boots (4) | 6 |
| Phụ kiện | Accessory (5) | 7 |

### Luồng hoạt động:
1. **Trang bị**: Click item trong Inventory → Button "Trang bị" → API `/player/{id}/equipment/equip` → Server xóa khỏi inventory, thêm vào equipment (nếu slot đã có → swap item cũ về inventory) → Refresh cả 2 UI
2. **Tháo trang bị**: Click slot trong Equipment Panel → Xác nhận tháo → API `/player/{id}/equipment/unequip` → Server chuyển item về inventory → Refresh cả 2 UI
3. **Load khi vào game**: Tự động gọi `RefreshEquipmentFromDB()` khi player spawn
4. **Load khi mở Inventory**: Gọi `RefreshEquipmentFromDB()` cùng lúc `RefreshInventoryFromDB()`

---

## Bước 1: Tạo Equipment Panel trong Canvas

### 1.1 Tạo Panel gốc

Trong **Hierarchy** → Chọn **Canvas** (cùng Canvas chứa InventoryPanel):

1. Click phải Canvas → **UI → Panel** → Đặt tên **`EquipmentPanel`**
2. Config RectTransform:
   - **Anchor**: Middle Center (hoặc vị trí bạn muốn)
   - **Width**: 350, **Height**: 500
   - **Pos X**: -250 (bên trái), **Pos Y**: 0
3. Thêm component **`EquipmentPanelUI`** (script đã có sẵn)
4. Có thể thêm **Image** component làm background (màu tối, alpha ~200)

### 1.2 Tạo Title

1. Click phải **EquipmentPanel** → **UI → Text - TextMeshPro** → Đặt tên **`TitleText`**
2. Config:
   - **Text**: "Trang Bị"
   - **Font Size**: 24
   - **Alignment**: Center
   - **Anchor**: Top Stretch
   - **Height**: 40
   - **Pos Y**: -20

---

## Bước 2: Tạo 6 Equipment Slot (Cách thủ công - Khuyến nghị)

### 2.1 Tạo container cho slots

1. Click phải **EquipmentPanel** → **UI → Empty** → Đặt tên **`SlotContainer`**
2. Thêm component **`Vertical Layout Group`**:
   - **Spacing**: 8
   - **Padding**: Left=10, Right=10, Top=10, Bottom=10
   - **Child Alignment**: Upper Center
   - **Control Child Size**: Width ✅, Height ❌
   - **Child Force Expand**: Width ✅, Height ❌
3. Config RectTransform:
   - **Anchor**: Stretch (top-bottom)
   - **Top**: 50 (dưới title)
   - **Bottom**: 60 (chừa chỗ cho nút đóng)

### 2.2 Tạo 1 Equipment Slot (lặp lại 6 lần)

Mỗi slot cần cấu trúc sau:

```
EquipSlot_Weapon  (có EquipmentSlotUI script + Button component)
├── PlaceholderImage  (Image - icon mờ khi trống)
├── IconImage         (Image - icon item đang trang bị)
├── SlotLabel         (TMP_Text - "Vũ khí", "Mũ", ...)
└── ItemNameText      (TMP_Text - tên item đang trang bị)
```

**Chi tiết tạo:**

1. Click phải **SlotContainer** → **UI → Button - TextMeshPro** → Đặt tên **`EquipSlot_Weapon`**
2. **Xóa** child Text (TMP) bên trong button
3. Config **EquipSlot_Weapon**:
   - **RectTransform Height**: 60
   - Thêm component **`EquipmentSlotUI`**
   - **Button.OnClick()**: Kéo chính nó vào → chọn `EquipmentSlotUI.OnClick()`

4. Tạo **PlaceholderImage** (icon mờ):
   - Click phải EquipSlot_Weapon → **UI → Image** → Đặt tên **`PlaceholderImage`**
   - **Anchor**: Left, **Width**: 50, **Height**: 50
   - **Pos X**: 35
   - **Color**: (255, 255, 255, 80) → mờ
   - Gán sprite placeholder (ví dụ: icon slot trống)

5. Tạo **IconImage** (icon item):
   - Click phải EquipSlot_Weapon → **UI → Image** → Đặt tên **`IconImage`**
   - **Anchor**: Left, **Width**: 50, **Height**: 50
   - **Pos X**: 35
   - **Raycast Target**: ❌ (không chặn click)

6. Tạo **SlotLabel** (tên loại slot):
   - Click phải EquipSlot_Weapon → **UI → Text - TextMeshPro** → Đặt tên **`SlotLabel`**
   - **Anchor**: Left
   - **Pos X**: 90, **Width**: 80, **Height**: 30
   - **Font Size**: 14
   - **Color**: Xám nhạt
   - **Text**: "Vũ khí" (sẽ được set tự động bởi script)

7. Tạo **ItemNameText** (tên item):
   - Click phải EquipSlot_Weapon → **UI → Text - TextMeshPro** → Đặt tên **`ItemNameText`**
   - **Anchor**: Stretch horizontal
   - **Left**: 90, **Right**: 10, **Height**: 25
   - **Font Size**: 16
   - **Color**: Trắng
   - **Text**: "" (trống, sẽ được set bởi script)

8. **Gán references trong Inspector** (EquipmentSlotUI component):
   - **Icon Image**: Kéo `IconImage` vào
   - **Placeholder Image**: Kéo `PlaceholderImage` vào
   - **Slot Label Text**: Kéo `SlotLabel` vào
   - **Item Name Text**: Kéo `ItemNameText` vào
   - **Slot Type**: Chọn `Weapon`

### 2.3 Nhân bản cho 5 slot còn lại

Duplicate `EquipSlot_Weapon` 5 lần, đổi tên và **Slot Type** trong Inspector:

| GameObject Name | Slot Type | Label |
|----------------|-----------|-------|
| EquipSlot_Weapon | Weapon | Vũ khí |
| EquipSlot_Helmet | Helmet | Mũ |
| EquipSlot_Armor | Armor | Giáp |
| EquipSlot_Pants | Pants | Quần |
| EquipSlot_Boots | Boots | Giày |
| EquipSlot_Accessory | Accessory | Phụ kiện |

**LƯU Ý**: Sau khi duplicate, phải vào mỗi slot → EquipmentSlotUI component → thay đổi **Slot Type** cho đúng!

---

## Bước 3: Gán references cho EquipmentPanelUI

Chọn **EquipmentPanel** trong Hierarchy → Trong Inspector, gán:

### Panel
- **Panel Root**: Kéo chính `EquipmentPanel` vào (hoặc để trống, sẽ tự dùng gameObject)

### Manual Slots (Cách B - Ưu tiên)
- **Manual Slots** (array size = 6):
  - Element 0: Kéo `EquipSlot_Weapon` vào
  - Element 1: Kéo `EquipSlot_Helmet` vào
  - Element 2: Kéo `EquipSlot_Armor` vào
  - Element 3: Kéo `EquipSlot_Pants` vào
  - Element 4: Kéo `EquipSlot_Boots` vào
  - Element 5: Kéo `EquipSlot_Accessory` vào

> **THỨ TỰ RẤT QUAN TRỌNG**: [0]=Weapon, [1]=Helmet, [2]=Armor, [3]=Pants, [4]=Boots, [5]=Accessory

### Title
- **Title Text**: Kéo `TitleText` vào

### Unequip Confirmation (Tuỳ chọn)
Nếu muốn có popup xác nhận khi tháo trang bị:
1. Tạo thêm panel con **`UnequipConfirmPanel`** bên trong EquipmentPanel
2. Bên trong tạo: TMP_Text (tên item), Button "Xác nhận", Button "Hủy"
3. Gán vào Inspector:
   - **Unequip Confirm Panel**: `UnequipConfirmPanel`
   - **Unequip Item Name Text**: TMP_Text hiển thị tên item
   - **Confirm Unequip Button**: Nút "Xác nhận"
   - **Cancel Unequip Button**: Nút "Hủy"

Nếu **không** gán UnequipConfirmPanel → script sẽ tháo trang bị ngay lập tức khi click (không hỏi).

---

## Bước 4: Nút mở/đóng Equipment Panel

### Cách 1: Nút riêng

1. Tạo Button trong Canvas → Đặt tên **`EquipmentToggleButton`**
2. Text: "Trang bị" hoặc icon áo giáp
3. Button.OnClick(): Kéo `EquipmentPanel` → chọn `EquipmentPanelUI.TogglePanel()`

### Cách 2: Phím tắt (khuyến nghị)

Thêm vào script InventoryUI.cs hoặc tạo script mới:

```csharp
// Đã có sẵn trong InventoryUI - phím I mở Inventory
// Thêm phím E để mở Equipment
void Update()
{
    if (Input.GetKeyDown(KeyCode.E))
    {
        var equipPanel = FindObjectOfType<EquipmentPanelUI>();
        if (equipPanel != null)
        {
            equipPanel.TogglePanel();
        }
    }
}
```

### Cách 3: Mở cùng Inventory

Nếu muốn Equipment Panel luôn hiện khi mở Inventory, sửa `InventoryUI.ToggleInventory()`:
```csharp
// Đã được config: khi mở inventory sẽ RefreshEquipmentFromDB() 
// Equipment Panel sẽ tự refresh data, nhưng bạn cần bật hiển thị panel nếu muốn
```

---

## Bước 5: Gán InventoryNetworkBridge (nếu chưa)

Chọn GameObject có **InventoryNetworkBridge** trong scene:

- **Equipment Panel UI**: Kéo `EquipmentPanel` (có EquipmentPanelUI) vào

Nếu để trống, script sẽ tự tìm bằng `FindObjectOfType<EquipmentPanelUI>()`.

---

## Bước 6: Kiểm tra cấu trúc cuối cùng

```
Canvas
├── InventoryPanel (InventoryUI script)
│   ├── SlotContainer (Grid Layout Group)
│   │   ├── InventorySlot_0 (InventorySlotUI)
│   │   ├── InventorySlot_1
│   │   └── ...
│   └── ItemDetailPanel (ItemDetailPanel script)
│       ├── ItemIcon
│       ├── ItemName
│       ├── ItemDescription
│       └── UseButton ("Trang bị" / "Sử dụng")
│
├── EquipmentPanel (EquipmentPanelUI script)  ← MỚI
│   ├── TitleText ("Trang Bị")
│   ├── SlotContainer
│   │   ├── EquipSlot_Weapon (EquipmentSlotUI)
│   │   │   ├── PlaceholderImage
│   │   │   ├── IconImage
│   │   │   ├── SlotLabel
│   │   │   └── ItemNameText
│   │   ├── EquipSlot_Helmet (EquipmentSlotUI)
│   │   ├── EquipSlot_Armor (EquipmentSlotUI)
│   │   ├── EquipSlot_Pants (EquipmentSlotUI)
│   │   ├── EquipSlot_Boots (EquipmentSlotUI)
│   │   └── EquipSlot_Accessory (EquipmentSlotUI)
│   └── UnequipConfirmPanel (tuỳ chọn)
│       ├── UnequipItemNameText
│       ├── ConfirmButton
│       └── CancelButton
│
├── EquipmentToggleButton (tuỳ chọn)
│
└── InventoryNetworkBridge (gắn lên bất kỳ GO nào trong scene)
```

---

## Bước 7: Test

### 7.1 Chuẩn bị
1. Đảm bảo Server API đang chạy (`cd GameServerApi && dotnet run`)
2. Trong DB đã có items test (SWORD_001, HELMET_IRON, ARMOR_IRON, PANTS_IRON, BOOTS_IRON, ACCESSORY_IRON)
3. Play game, login

### 7.2 Test trang bị
1. Nhấn **Q** để thêm 6 items test vào inventory
2. Mở Inventory (nhấn **I**)
3. Click vào item → xem nút hiện "Trang bị" (equipment, category=1)
4. Click "Trang bị" → 
   - Item biến mất khỏi Inventory UI ✅
   - Equipment Panel hiển thị item ở đúng slot ✅
   - Console log: `✅ Equip thành công!` ✅

### 7.3 Test swap trang bị
1. Có 2 weapons trong inventory (ví dụ: cùng SWORD_001)
2. Trang bị weapon thứ 1 → hiện ở slot Vũ khí
3. Trang bị weapon thứ 2 → weapon cũ chuyển về inventory, weapon mới vào slot Vũ khí

### 7.4 Test tháo trang bị
1. Mở Equipment Panel
2. Click vào slot đang có item
3. Xác nhận tháo → Item quay về Inventory

### 7.5 Test load khi vào game
1. Trang bị 1 vài items
2. Thoát game, vào lại
3. Equipment Panel hiển thị đúng items đã trang bị trước đó ✅

---

## Troubleshooting

### Equipment Panel không hiển thị data
- Kiểm tra `EquipmentPanelUI.manualSlots` đã gán đủ 6 slots và đúng thứ tự
- Kiểm tra Console log: `[EquipmentPanelUI] Đã khởi tạo 6 equipment slots`
- Nếu log `0 equipment slots` → manualSlots chưa gán hoặc slotPrefab chưa gán

### Click "Trang bị" nhưng không có phản hồi
- Kiểm tra Console: `[InventoryNetworkBridge] ⚔️ RequestEquipItem` có xuất hiện không?
- Kiểm tra Console: `[InventoryNetworkBridge] RequestUseItem: template=..., category=1` → nếu category≠1 thì item không phải equipment
- Kiểm tra API Server log có nhận được request không

### Icon không hiển thị
- Kiểm tra `IconDatabase` đã setup với đúng icon IDs
- Kiểm tra item trong DB có `iconId` không rỗng
- Console: `[EquipmentSlotUI] Không tìm thấy icon: ...`

### Equipment không load khi vào game
- Kiểm tra Console: `[InventoryNetworkBridge] 🔄 Auto-load equipment từ DB khi vào game...`
- Nếu không thấy log → NetworkInventory chưa được tìm thấy (kiểm tra player prefab có NetworkInventory component)
- Kiểm tra: `[InventoryNetworkBridge] RefreshEquipmentFromDB: playerId = 0!` → GameManager chưa có player data
