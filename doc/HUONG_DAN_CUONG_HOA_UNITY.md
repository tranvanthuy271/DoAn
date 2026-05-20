# Hướng Dẫn Config Hệ Thống Cường Hóa (Blacksmith) Trong Unity

> **Phiên bản**: v2 – Layout mới với 16 ô đá, ô trang bị, ô bùa, 3 tab.
> **Scripts liên quan**: `BlacksmithTabPanel`, `UpgradePanel`, `UpgradeStoneSlot`, `InventorySlotUI`, `EquipmentSelectionForUpgrade`, `UpgradeStoneConfig`

---

## 1. Tổng Quan Luồng UI

```
[NPC Blacksmith]
      │  click
      ▼
[BlacksmithTabPanel]  ←── 3 tab trên thanh top bar
   ├── Tab 0: Cường Hóa    → UpgradePanel
   ├── Tab 1: Trang Bị     → EquipmentSelectionForUpgrade
   └── Tab 2: Túi          → InventoryUI (có thể bật select mode)

LUỒNG CHỌN ĐÁ:
  Click ô đá trống
    → UpgradePanel.OnStoneSlotClicked()
    → BlacksmithTabPanel.SwitchTabToInventoryWithFilter(filterItemType=21)
    → InventoryUI.EnterItemSelectMode(filterByType=21)
    → slot có type=21 hiện btn "Chọn"
    → Nhấn "Chọn" → UpgradePanel.OnStoneSelectedFromInventory()
    → Đá vào slot, slotIndex ghi lại
    → Chuyển lại tab 0

LUỒNG CHỌN BÙA (itemId=8):
  Click ô Bùa trống
    → UpgradePanel.OnCharmSlotClicked()
    → BlacksmithTabPanel.SwitchTabToInventoryWithFilter(filterItemId=8)
    → InventoryUI.EnterItemSelectMode(filterById=8)
    → slot id=8 hiện btn "Chọn"
    → Nhấn "Chọn" → UpgradePanel.SetCharmFromInventory()
    → Bùa vào ô, +3% rate

LUỒNG CHỌN TRANG BỊ:
  Click ô Trang Bị trống  OR  click tab "Trang Bị"
    → EquipmentSelectionForUpgrade.Show()
    → Hiển thị danh sách trang bị đang mặc + trang bị trong túi
    → Nhấn [Nâng Cấp] → UpgradePanel.SetChosenEquipItem()
    → Chuyển lại tab 0, load config tỉ lệ

LUỒNG XEM TRƯỚC:
  Nhấn [XEM TRƯỚC]
    → Nếu chưa chọn trang bị → hiện thông báo
    → Có trang bị → tính stats +1 level → hiện PreviewPanel

LUỒNG CƯỜNG HÓA:
  Nhấn [CƯỜNG HÓA]
    → Gửi UpgradeRequestDto lên server gồm:
        stoneSlotIndices  = các slotIndex trong túi của đá
        charmSlotIndices  = slotIndex của bùa (nếu có)
        clientRatePercent = % server so sánh để dò cheat
    → Server xác minh, trả kết quả
    → UI cập nhật
```

---

## 2. Tạo Hierarchy Trong Scene

### 2.1 Cấu Trúc GameObject Gợi Ý

```
Canvas (UI)
└─ BlacksmithPanel                  ← [BlacksmithTabPanel.cs] + [Image] nền
   ├─ TabBar                        ← [HorizontalLayoutGroup]
   │  ├─ BtnCuongHoa                ← [Button] text "Cường Hóa"
   │  ├─ BtnTrangBi                 ← [Button] text "Trang Bị"
   │  └─ BtnTui                     ← [Button] text "Túi"
   ├─ BtnClose                      ← [Button] text "X"
   ├─ PanelCuongHoa                 ← [UpgradePanel.cs]
   │  ├─ EquipSlot                  ← [Button] + [Image icon] + [TMP_Text name]
   │  │  └─ UpgradeLevelText        ← [TMP_Text] "+3"
   │  ├─ EquipInfoBox               ← [GameObject] ẩn mặc định – mini popup khi click EquipSlot
   │  │  ├─ TitleText               ← [TMP_Text] "Áo Nhẫn Giả Base (+19)"
   │  │  ├─ BtnClose                ← [Button] "X"
   │  │  ├─ BtnEquipRemove          ← [Button] "Lấy Ra"
   │  │  └─ BtnEquipViewStats       ← [Button] "Xem"
   │  ├─ CharmSlot                  ← [Button] + [Image icon] + [TMP_Text name]
   │  ├─ CharmInfoBox               ← [GameObject] ẩn mặc định – mini popup khi click CharmSlot
   │  │  ├─ TitleText               ← [TMP_Text] tên bùa
   │  │  ├─ BtnClose                ← [Button] "X"
   │  │  ├─ BtnCharmRemove          ← [Button] "Lấy Ra"
   │  │  └─ BtnCharmView            ← [Button] "Xem"
   │  ├─ StoneGrid                  ← [GridLayoutGroup] 4 cột
   │  │  ├─ StoneSlot_00            ← [UpgradeStoneSlot.cs]
   │  │  ├─ StoneSlot_01
   │  │  ├─ ...
   │  │  └─ StoneSlot_15            ← (16 slot tổng cộng)
   │  ├─ PreviewPanel               ← [GameObject] ẩn mặc định
   │  │  ├─ BtnClose                ← [Button] "X" – đóng PreviewPanel
   │  │  ├─ PreviewNameText         ← [TMP_Text]
   │  │  └─ StatsText               ← [TMP_Text] hiển thị chỉ số (tạm thời)
   │  ├─ BtnPreview                 ← [Button] "XEM TRƯỚC"
   │  ├─ BtnUpgrade                 ← [Button] "CƯỜNG HÓA"
   │  ├─ BtnCancel                  ← [Button] "HỦY"
   │  ├─ RateBar                    ← [Slider] (Interactable=false)
   │  ├─ RateText                   ← [TMP_Text] "72%"
   │  ├─ SilverCostText             ← [TMP_Text]
   │  ├─ SilverOwnText              ← [TMP_Text]
   │  ├─ FailWarningObj             ← [GameObject] cảnh báo giảm cấp
   │  └─ StatusText                 ← [TMP_Text]
   ├─ PanelTrangBi                  ← [EquipmentSelectionForUpgrade.cs]
   │  ├─ HeaderEquipped             ← [TMP_Text] "Trang bị đang mặc"
   │  ├─ ScrollEquipped             ← [ScrollRect]
   │  │  └─ Content                 ← [VerticalLayoutGroup] (chứa EquipUpgradeRow)
   │  ├─ HeaderInventory            ← [TMP_Text] "Trang bị trong túi"
   │  └─ ScrollInventory            ← [ScrollRect]
   │     └─ Content                 ← [VerticalLayoutGroup]
   └─ PanelTui                      ← [InventoryUI.cs]
      └─ SlotContainer              ← [GridLayoutGroup] (chứa InventorySlotUI prefab)
```

---

## 3. Gán Inspector Cho BlacksmithTabPanel

| Field | Gán vào |
|-------|---------|
| `Btn Cuong Hoa` | BtnCuongHoa (Button) |
| `Btn Trang Bi` | BtnTrangBi (Button) |
| `Btn Tui` | BtnTui (Button) |
| `Btn Close` | BtnClose (Button) |
| `Panel Cuong Hoa` | PanelCuongHoa (GameObject) |
| `Panel Trang Bi` | PanelTrangBi (GameObject) |
| `Panel Tui` | PanelTui (GameObject) |
| `Color Tab Active` | Màu vàng `#FFD900` |
| `Color Tab Inactive` | Màu xám `#999999` |
| `Bg Tab Active` | Màu nền tab active |
| `Bg Tab Inactive` | Màu nền tab inactive |

---

## 4. Gán Inspector Cho UpgradePanel

### 4.1 Ô Trang Bị (Equip Slot)

| Field | Gán vào |
|-------|---------|
| `Equip Slot Button` | EquipSlot (Button – root) |
| `Equip Slot Icon` | EquipSlot/IconImage (Image) |
| `Equip Slot Name Text` | EquipSlot/NameText (TMP_Text) |
| `Upgrade Level Text` | EquipSlot/UpgradeLevelText (TMP_Text) |
| `Equip Remove Button` | BtnEquipRemove (Button, ẩn mặc định) |
| `Equip View Stats Button` | BtnEquipViewStats (Button, ẩn mặc định) |

### 4.2 Ô Bùa Cường Hóa (Charm Slot – itemId=8)

| Field | Gán vào |
|-------|---------|
| `Charm Slot Button` | CharmSlot (Button – root) |
| `Charm Slot Icon` | CharmSlot/IconImage (Image) |
| `Charm Slot Name Text` | CharmSlot/NameText (TMP_Text) |
| `Charm Remove Button` | BtnCharmRemove (Button, ẩn mặc định) |
| `Charm View Button` | BtnCharmView (Button, ẩn mặc định) |

> **Lưu ý**: Bùa cường hóa (itemId=8) sẽ tự động cộng +3% tỉ lệ thành công.

### 4.3 Preview Panel

| Field | Gán vào |
|-------|---------|
| `Preview Panel` | PreviewPanel (GameObject, ẩn mặc định) |
| `Preview Name Text` | PreviewPanel/PreviewNameText (TMP_Text) |
| `Preview Stats Text` | PreviewPanel/StatsText (TMP_Text) |
| `Preview Close Button` | PreviewPanel/BtnClose (Button) |

### 4.4 Stone Grid (16 ô đá)

- Tạo đúng **16** GameObject StoneSlot_00 → StoneSlot_15
- Mỗi ô gắn script **`UpgradeStoneSlot.cs`**
- Gán tất cả 16 vào mảng `Stone Slots[0..15]` trong Inspector UpgradePanel
- Cả 16 ô phải là **con của PanelCuongHoa** (để `GetComponentInParent<UpgradePanel>()` hoạt động)

### 4.5 Nút Chính

| Field | Gán vào |
|-------|---------|
| `Preview Button` | BtnPreview (Button) |
| `Upgrade Button` | BtnUpgrade (Button) |
| `Cancel Button` | BtnCancel (Button) |

### 4.6 Rate & Cost

| Field | Gán vào |
|-------|---------|
| `Rate Bar` | RateBar (Slider, Interactable=**false**) |
| `Rate Text` | RateText (TMP_Text) |
| `Silver Cost Text` | SilverCostText (TMP_Text) |
| `Silver Own Text` | SilverOwnText (TMP_Text) |
| `Fail Warning Obj` | FailWarningObj (GameObject) |

### 4.7 Stone Config

| Field | Gán vào |
|-------|---------|
| `Upgrade Stone Config` | UpgradeStoneConfig asset (tạo ở bước 6) |
| `Status Text` | StatusText (TMP_Text) |

---

### 4.8 Equip Info Box & Charm Info Box (mini popup)

> **Luồng hoạt động:**
> 1. Click vào `EquipSlot` (khi đã có trang bị) → `EquipInfoBox` hiện ra với tên trang bị + cấp
> 2. Click **"Xem"** trong popup → mở full `ItemDetailPanel` (tên, cấp yêu cầu, giới tính, hệ, mô tả, chỉ số)
> 3. Click **"Lấy Ra"** trong popup → xóa trang bị khỏi ô, đóng popup
> 4. Click **"X"** trong popup → chỉ đóng popup, giữ nguyên trang bị
>
> Tương tự cho `CharmInfoBox` khi click `CharmSlot` có bùa.

**EquipInfoBox** (con trực tiếp của PanelCuongHoa, ẩn mặc định `SetActive(false)`):

| Field | Gán vào |
|-------|----------|
| `Equip Info Box` | EquipInfoBox (GameObject) |
| `Equip Info Title Text` | EquipInfoBox/TitleText (TMP_Text) |
| `Equip Info Close Button` | EquipInfoBox/BtnClose (Button "X") |

**CharmInfoBox** (con trực tiếp của PanelCuongHoa, ẩn mặc định):

| Field | Gán vào |
|-------|----------|
| `Charm Info Box` | CharmInfoBox (GameObject) |
| `Charm Info Title Text` | CharmInfoBox/TitleText (TMP_Text) |
| `Charm Info Close Button` | CharmInfoBox/BtnClose (Button "X") |

**Gợi ý thiết kế EquipInfoBox:**
```
EquipInfoBox                [Image (nền tối)] [SetActive=false]
├─ TitleText                [TMP_Text] "Áo Nhẫn Giả Base (+19)" + font bold
├─ BtnClose                 [Button] "X" – góc trên phải
└─ ButtonRow                [HorizontalLayoutGroup]
   ├─ BtnEquipRemove        [Button] text "Lấy Ra"
   └─ BtnEquipViewStats     [Button] text "Xem"
```
---

## 5. Gán Inspector Cho EquipmentSelectionForUpgrade

| Field | Gán vào |
|-------|---------|
| `Header Equipped` | HeaderEquipped (GameObject/TMP_Text) |
| `Container Equipped` | ScrollEquipped/Viewport/Content (Transform) |
| `Header Inventory` | HeaderInventory (GameObject/TMP_Text) |
| `Container Inventory` | ScrollInventory/Viewport/Content (Transform) |
| `Equip Upgrade Row Prefab` | Prefab `EquipUpgradeRow` |

### Prefab EquipUpgradeRow

```
EquipUpgradeRow              [HorizontalLayoutGroup, ContentSizeFitter]
├─ IconImage                 [Image] 48×48
├─ NameText                  [TMP_Text] tên trang bị
├─ LevelText                 [TMP_Text] "+3" (index 1 trong GetComponentsInChildren)
└─ BtnNangCap                [Button] text "Nâng Cấp"
```

> ⚠️ Script đọc TMP_Text theo thứ tự index: `texts[0]` = tên, `texts[1]` = level.
> Kiểm tra thứ tự con trong prefab nếu hiển thị sai.

---

## 6. Tạo Asset UpgradeStoneConfig

1. Trong **Project** panel: `Create → Upgrade → Stone Config`
2. Đặt tên: `UpgradeStoneConfig` (trong `Assets/Data/Upgrade/`)
3. Cấu hình:

### 6.1 Mảng `Stones` – Danh sách đá (type=21)

| Trường | Mô tả | Ví dụ |
|--------|-------|-------|
| `Item Id` | item_template.id của đá | 10 |
| `Stone Name` | Tên hiển thị | "Đá Sắt" |
| `Rate Point Per Stone` | Điểm tỉ lệ mỗi viên | 5 |
| `Max Rate Point From This Stone` | Tối đa đá này đóng góp (0=không giới hạn) | 30 |

### 6.2 Bùa Cường Hóa

| Trường | Giá trị |
|--------|---------|
| `Charm Item Id` | 8 |
| `Charm Bonus Percent` | 3 (= +3%) |

### 6.3 Mảng `Item Configs` – Tỉ lệ theo từng trang bị

| Trường | Mô tả | Ví dụ |
|--------|-------|-------|
| `Item Template Id` | item_template.id của trang bị | 200 |
| `Item Name` | (debug) tên | "Kiếm Thép" |
| `Base Success Percent` | Tỉ lệ cơ bản ở +0→+1 | 80 |
| `Success Decrease Per Level` | Giảm % mỗi bậc | 5 |
| `Max Upgrade Level` | Bậc tối đa | 15 |
| `Stone Min Override` | Số đá tối thiểu (0=dùng server) | 0 |
| `Stone Needed Override` | Số đá đủ tỉ lệ (0=dùng server) | 0 |

### 6.4 Cài Đặt Chung

| Trường | Mô tả | Ví dụ |
|--------|-------|-------|
| `Full Rate Points` | Tổng điểm cần cho 100% | 100 |
| `Max Success Percent` | Giới hạn tỉ lệ tối đa | 95 |

---

## 7. Setup Prefab InventorySlotUI (thêm "Chọn" button)

Để các ô đá trong túi hiện nút "Chọn" khi BlacksmithTabPanel bật select mode:

1. Mở prefab **InventorySlot** (prefab dùng cho InventoryUI)
2. Thêm con: `ChooseButton` [Button]
   - Text: "Chọn"
   - Đặt Pivot ở giữa ô, stretch full hoặc bottom-center tùy design
   - **Tắt** SetActive(false) mặc định
3. Thêm component **CanvasGroup** vào root của prefab
4. Gán trong Inspector của **InventorySlotUI**:
   - `Choose Button` → ChooseButton
   - `Canvas Group` → CanvasGroup ở root slot

> **Lưu ý**: Khi enter select mode, các ô **không khớp** filter bị mờ (`alpha=0.35`).
> Ô khớp filter sẽ hiện btn "Chọn" và interactable bình thường.

---

## 8. Setup UpgradeStoneSlot Prefab

Mỗi UpgradeStoneSlot cần:
```
StoneSlot_XX                    [UpgradeStoneSlot.cs]
├─ IconImage                    [Image] (icon đá, ẩn mặc định)
├─ QuantityText                 [TMP_Text] số lượng
├─ EmptyIndicator               [GameObject] dấu "+" hoặc placeholder
└─ HighlightBorder              [Image] (optional, border khi hover)
```

Gán trong Inspector:
- `Icon Image` → IconImage
- `Quantity Text` → QuantityText
- `Empty Indicator` → EmptyIndicator
- `Highlight Border` → HighlightBorder (optional)

---

## 9. Gán NPC Blacksmith → Mở Cửa Sổ

Trong `NpcMenuUI.cs`, đoạn xử lý khi click NPC Blacksmith:

```csharp
// Mở cửa sổ Blacksmith (tab 0 = Cường Hóa mặc định)
BlacksmithTabPanel.Instance?.Open(0);
```

Đảm bảo trong scene có **đúng 1** `BlacksmithTabPanel` trong Canvas.

---

## 10. Server-Side Validation

Khi client gửi `UpgradeRequestDto`:
```json
{
  "playerId": 1,
  "slotKey": "weapon",
  "isFromInventory": false,
  "stoneSlotIndices": [2, 5, 8],
  "charmSlotIndices": [3],
  "clientRatePercent": 72
}
```

Server cần:
1. Kiểm tra `stoneSlotIndices` có đúng số đá trong inventory của player
2. Xác nhận mỗi đá có `item_template.type = 21`
3. Kiểm tra `charmSlotIndices` có itemId=8
4. Tính lại `serverRatePercent` từ DB config → so sánh với `clientRatePercent`
5. Nếu chênh lệch > ngưỡng cho phép → reject (chống cheat)
6. Random kết quả dựa trên `serverRatePercent`

---

## 11. Kiểm Tra Sau Khi Config

### Checklist
- [ ] BlacksmithTabPanel có Instance? (duy nhất 1 trong scene)
- [ ] UpgradePanel có Instance? (gắn trong PanelCuongHoa)
- [ ] 16 UpgradeStoneSlot gán đủ trong `Stone Slots[0..15]`
- [ ] UpgradeStoneConfig asset được gán vào `Upgrade Stone Config`
- [ ] InventorySlotUI prefab có `ChooseButton` và `CanvasGroup`
- [ ] EquipUpgradeRow prefab có đúng thứ tự TMP_Text (index 0=tên, 1=level)
- [ ] EquipmentSelectionForUpgrade gắn trên PanelTrangBi
- [ ] NPC Blacksmith gọi `BlacksmithTabPanel.Instance.Open(0)`
- [ ] Tab Túi có InventoryUI với prefab slot đúng loại

### Test Flow
1. Mở NPC Blacksmith → xác nhận tab Cường Hóa mặc định
2. Click ô đá → xác nhận chuyển sang tab Túi, ô type=21 hiện "Chọn"
3. Chọn đá → xác nhận đá vào ô, chuyển lại tab Cường Hóa
4. Click tab Trang Bị → xác nhận danh sách trang bị hiển thị
5. Nhấn [Nâng Cấp] trên trang bị → xác nhận chuyển về Cường Hóa, ô trang bị được điền
6. Click ô Bùa → chọn bùa id=8 → xác nhận +3% trên thanh tỉ lệ
7. Nhấn [XEM TRƯỚC] → xác nhận hiện stat +1 level
8. Nhấn [CƯỜNG HÓA] → xác nhận gửi request + nhận kết quả

---

## 12. Lỗi Thường Gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| Btn "Chọn" không hiện | `ChooseButton` chưa gán trong InventorySlotUI | Gán trong prefab slot |
| Slot không mờ đi khi select mode | `CanvasGroup` chưa gán | Thêm CanvasGroup vào root prefab slot |
| Tab Trang Bị không hiển thị list | `EquipmentSelectionForUpgrade` chưa gán đúng Container | Kiểm tra `Container Equipped` / `Container Inventory` |
| Rate không cập nhật | `UpgradeStoneConfig` chưa gán | Kéo asset vào Inspector UpgradePanel |
| NullRef khi cường hóa | `GameManager.currentPlayerData` null | Đảm bảo player đã load data trước khi mở NPC |
| Đá không trừ sau nâng | Server logic – không phải client | Kiểm tra server endpoint `/upgrade` |
| `UpgradePanel.Instance` null | Chưa active PanelCuongHoa khi Start() | UpgradePanel Awake() gán Instance kể cả khi inactive |
