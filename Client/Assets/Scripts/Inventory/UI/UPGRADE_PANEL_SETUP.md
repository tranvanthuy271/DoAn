# Hướng Dẫn Setup UI Nâng Cấp Trang Bị (UpgradePanel)

> **Liên quan:** `UpgradePanel.cs`, `UpgradeItemCard.cs`, `UpgradeStoneSlot.cs`, `StatRowEntry.cs`

---

## Tổng Quan Hệ Thống

```
EquipmentSlotUI
  └─ Button "Nâng Cấp" (OnClick → OnUpgradeClick())
       └─ UpgradePanel.Instance.OpenForEquipped(item, slotKey, inventory)
            ├─ CurrentCard  (UpgradeItemCard) — stat hiện tại
            ├─ PreviewCard  (UpgradeItemCard) — stat sau khi nâng
            ├─ StoneSlot_00 ~ StoneSlot_15    — 16 ô đặt đá
            ├─ StonePicker Panel              — danh sách đá trong túi
            ├─ Rate Bar / Cost Text           — tỉ lệ & chi phí bạc
            └─ Nút NÂNG CẤP / HỦY
```

---

## Bước 1 — Tạo Hierarchy trong Canvas

Tạo cấu trúc GameObject trong **Canvas** như sau (tên phải chính xác để dễ kéo reference):

```
Canvas
└── UpgradePanel                        ← gắn UpgradePanel.cs tại đây
    ├── Background (Image, tối màu)
    ├── Title (TMP_Text "NÂNG CẤP TRANG BỊ")
    │
    ├── CardArea
    │   ├── CurrentCard                 ← gắn UpgradeItemCard.cs
    │   │   ├── ItemIcon (Image)
    │   │   ├── ItemNameText (TMP_Text)
    │   │   ├── UpgradeLevelText (TMP_Text "+0")
    │   │   └── StatsContainer          ← VerticalLayoutGroup
    │   │       └── (StatRowEntry sinh ra lúc runtime)
    │   │
    │   └── PreviewCard                 ← gắn UpgradeItemCard.cs
    │       ├── ItemIcon (Image)
    │       ├── ItemNameText (TMP_Text)
    │       ├── UpgradeLevelText (TMP_Text "+1")
    │       └── StatsContainer          ← VerticalLayoutGroup
    │
    ├── StoneMatrix                     ← GridLayout 4×4
    │   ├── StoneSlot_00                ← gắn UpgradeStoneSlot.cs
    │   ├── StoneSlot_01
    │   ├── ...
    │   └── StoneSlot_15               (tổng 16 slot)
    │
    ├── StonePicker                     ← Panel, tắt mặc định (SetActive false)
    │   └── ScrollRect
    │       ├── Viewport
    │       │   └── Content             ← kéo vào Stone Picker Content
    │       └── Scrollbar
    │
    ├── InfoArea
    │   ├── RateBar (Slider, Interactable = OFF)
    │   ├── RateText (TMP_Text "0%")
    │   ├── SilverCostText (TMP_Text "Bạc cần: ...")
    │   ├── SilverOwnText  (TMP_Text "Bạn có: ...")
    │   └── FailWarning (GameObject, Text "⚠ Thất bại có thể giảm bậc")
    │
    ├── ButtonArea
    │   ├── ButtonUpgrade (Button "NÂNG CẤP")
    │   └── ButtonCancel  (Button "HỦY")
    │
    ├── StatusText (TMP_Text — hiển thị kết quả, có thể ẩn ban đầu)
    │
    └── UnequipConfirmPanel (không cần cho UpgradePanel, bỏ qua)
```

---

## Bước 2 — Tạo Prefab StoneSlot

Mỗi **StoneSlot_XX** cần:

| Component | Ghi chú |
|-----------|---------|
| `Image` | Background của ô |
| `UpgradeStoneSlot.cs` | Script chính |
| Child `Icon` (`Image`) | Icon đá — kéo vào **Icon Image** |
| Child `QuantityText` (`TMP_Text`) | Số lượng, kéo vào **Quantity Text** |
| Child `EmptyIndicator` (`GameObject`) | Hiện dấu "+" khi trống |
| Child `HighlightBorder` (`Image`) | Viền xanh khi hover *(tuỳ chọn)* |

> ⚠ `UpgradeStoneSlot` tự tìm `UpgradePanel` qua `GetComponentInParent<>()`.  
> Bắt buộc phải là **con** của GameObject đã gắn `UpgradePanel.cs`.

---

## Bước 3 — Tạo Prefab StatRowEntry

Đây là prefab **1 dòng stat** hiển thị trong `CurrentCard` và `PreviewCard`.

```
StatRowEntry (GameObject)
└── Label (TMP_Text)        ← kéo vào field labelText
```

Gắn `StatRowEntry.cs`. Không cần Button, Layout group tự handle.

---

## Bước 4 — Tạo Prefab InventorySlotUI (dùng trong Stone Picker)

Stone Picker dùng lại prefab `InventorySlotUI` để hiển thị đá trong túi.  
Đảm bảo prefab này có **Button** component (script tự `AddComponent<Button>` nếu thiếu, nhưng tốt hơn là có sẵn).

---

## Bước 5 — Kéo Reference vào UpgradePanel Inspector

Chọn GameObject **UpgradePanel**, kéo đúng thứ tự:

| Inspector Field | Kéo từ đâu |
|----------------|------------|
| **Current Card** | `CurrentCard` (UpgradeItemCard) |
| **Preview Card** | `PreviewCard` (UpgradeItemCard) |
| **Stone Slots** (array 16 phần tử) | Kéo lần lượt `StoneSlot_00` → `StoneSlot_15` |
| **Stone Picker Panel** | `StonePicker` GameObject |
| **Stone Picker Content** | `StonePicker/ScrollRect/Viewport/Content` |
| **Stone Picker Item Prefab** | Prefab `InventorySlotUI` |
| **Rate Bar** | `RateBar` (Slider) |
| **Rate Text** | `RateText` (TMP_Text) |
| **Silver Cost Text** | `SilverCostText` (TMP_Text) |
| **Silver Own Text** | `SilverOwnText` (TMP_Text) |
| **Fail Warning Obj** | `FailWarning` GameObject |
| **Upgrade Button** | `ButtonUpgrade` (Button) |
| **Cancel Button** | `ButtonCancel` (Button) |
| **Status Text** | `StatusText` (TMP_Text) |

---

## Bước 6 — Kéo Reference vào UpgradeItemCard Inspector (×2)

Làm cho cả **CurrentCard** và **PreviewCard**:

| Inspector Field | Kéo từ đâu |
|----------------|------------|
| **Item Icon** | `ItemIcon` (Image) |
| **Item Name Text** | `ItemNameText` (TMP_Text) |
| **Upgrade Level Text** | `UpgradeLevelText` (TMP_Text) |
| **Stats Container** | `StatsContainer` (Transform, có VerticalLayoutGroup) |
| **Stat Row Prefab** | Prefab `StatRowEntry` |

---

## Bước 7 — Kéo Reference vào từng UpgradeStoneSlot (×16)

Làm cho mỗi `StoneSlot_00` đến `StoneSlot_15`:

| Inspector Field | Kéo từ đâu |
|----------------|------------|
| **Icon Image** | Child `Icon` (Image) |
| **Quantity Text** | Child `QuantityText` (TMP_Text) |
| **Empty Indicator** | Child `EmptyIndicator` (GameObject) |
| **Highlight Border** | Child `HighlightBorder` (Image) — có thể bỏ trống |

---

## Bước 8 — Kết Nối Nút "Nâng Cấp" trong EquipmentSlotUI

Mỗi **EquipmentSlotUI** đã có field `upgradeButton`. Kéo Button "Nâng Cấp" của từng slot vào đó, rồi trong OnClick của button đó chọn:

```
EquipmentSlotUI.OnUpgradeClick()
```

Script `OnUpgradeClick()` sẽ tự lấy `currentItem`, `slotKey`, inventory rồi gọi:
```csharp
UpgradePanel.Instance.OpenForEquipped(currentItem, slotKey, inventory);
```

---

## Bước 9 — Đặt UpgradePanel ở Đâu?

**Về vị trí trong Hierarchy:**
- Đặt **cùng cấp** với các panel khác trong Canvas, **không** đặt con của EquipmentPanel.
- `UpgradePanel` dùng `Instance` (singleton) nên chỉ cần 1 cái trong toàn scene.
- Mặc định `SetActive(false)` — script tự `SetActive(true)` khi `OpenForEquipped()` được gọi.

**Về thứ tự Layer (Sort Order / Z):**
- Đặt UpgradePanel **sau** (cao hơn) EquipmentPanel trong Hierarchy để nó hiện đè lên.
- Hoặc dùng Canvas riêng với Sort Order cao hơn.

```
Canvas (Main)
├── InventoryPanel
├── EquipmentPanel
└── UpgradePanel        ← đặt cuối = hiện lên trên cùng
```

---

## Bước 10 — APIClient Cần Có (Kiểm Tra)

`UpgradePanel` gọi 3 method sau trên `APIClient.Instance`:

| Method | Endpoint |
|--------|----------|
| `GetOptionTemplates(onSuccess, onError)` | `GET /api/upgrade/options` |
| `GetUpgradeConfig(itemId, targetLevel, onSuccess, onError)` | `GET /api/upgrade/config?itemId=...&targetLevel=...` |
| `UpgradeEquipment(request, onSuccess, onError)` | `POST /api/upgrade/equipment` |

Nếu server chưa có endpoint nào, panel sẽ log lỗi và hiện "Không tải được config nâng cấp."

---

## Lưu Ý Quan Trọng

| # | Vấn đề | Giải pháp |
|---|--------|-----------|
| 1 | `UpgradePanel.Instance` là null | Đảm bảo GameObject UpgradePanel **tồn tại trong scene ngay từ đầu**, dù đang `SetActive(false)` |
| 2 | StoneSlot không tìm được UpgradePanel | StoneSlot phải là **con** (bất kỳ cấp nào) của UpgradePanel, `GetComponentInParent` sẽ leo lên tìm |
| 3 | Stone Picker không hiện đá | Kiểm tra: item trong túi có `type == 21` (UpgradeStone) hoặc `id == 8` (Lucky) / `id == 9` (Protection) |
| 4 | Tỉ lệ luôn 0% | Cần đặt ít nhất `stoneMin` viên đá (lấy từ config server) vào ô trước khi tỉ lệ > 0 |
| 5 | Silver luôn hiện 0 | `GetPlayerSilver()` đang đọc từ `pd.gold` tạm thời — cập nhật khi server trả về field `silver` riêng |
| 6 | Preview card không cập nhật | `strOptions` phải được set đúng trong `EquipmentItemDto` (field, không phải property) |
