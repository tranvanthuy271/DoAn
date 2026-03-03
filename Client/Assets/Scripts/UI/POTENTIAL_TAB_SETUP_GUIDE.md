# Hướng Dẫn Tạo UI Tab Tiềm Năng (Unity)

> Script liên quan:
> - `Assets/Scripts/Inventory/PotentialTabUI.cs`
> - `Assets/Scripts/Inventory/PotentialStatRowUI.cs`
> - `Assets/Scripts/UI/CharacterPanelController.cs`
> - Prefab có sẵn: `Assets/Prefabs/UI/Thông tin/PotentialStatRowPrefab.prefab`

---

## BƯỚC 1 – Tạo cấu trúc GameObject trong ContentPotential

Mở scene **GameScene**. Trong Hierarchy, tìm node `ContentPotential` nằm trong `CharacterPanel → Window → TabContents`.

Cấu trúc cần đạt được:

```
ContentPotential                      ← GameObject gốc của tab Tiềm Năng
├── TxtPotentialPoints                ← TMP_Text hiển thị điểm tiềm năng
├── TxtStatus                         ← TMP_Text hiển thị trạng thái / lỗi
└── ScrollView
    └── Viewport
        └── Content                   ← Transform chứa các StatRow
```

### 1.1 Tạo TxtPotentialPoints
1. Chuột phải vào `ContentPotential` → **UI → Text - TextMeshPro** → đặt tên `TxtPotentialPoints`.
2. **Rect Transform:** Anchor top-stretch, Pos Y ≈ -20, Height ≈ 30.
3. **Text:** `Điểm tiềm năng: 0`
4. **Alignment:** Left / Middle. Font size: 18, Bold.

### 1.2 Tạo TxtStatus
1. Chuột phải vào `ContentPotential` → **UI → Text - TextMeshPro** → đặt tên `TxtStatus`.
2. **Rect Transform:** Dưới TxtPotentialPoints, Height ≈ 25.
3. **Text:** (để trống). Color: `#FFD700`, size 14, Italic.
4. `Enabled = false`.

### 1.3 Tạo ScrollView
1. Chuột phải vào `ContentPotential` → **UI → Scroll View**.
2. **Rect Transform:** Stretch both, Top ≈ 60 (dưới 2 text), Left/Right/Bottom = 0.
3. Inspector ScrollView:
   - `Horizontal = false`
   - `Vertical = true`
   - `Scroll Sensitivity = 30`
4. Xóa `Scrollbar Horizontal` nếu có.

### 1.4 Cấu hình Content bên trong ScrollView
1. Mở rộng `ScrollView → Viewport → Content`.
2. Chọn `Content` → **Add Component:**
   - **Vertical Layout Group**
     - Child Alignment: Upper Left
     - Control Child Size: Width ✅  Height ✅
     - Child Force Expand: Width ✅  Height ✗
     - Spacing: 6
     - Padding: Left 8, Right 8, Top 4, Bottom 4
   - **Content Size Fitter**
     - Vertical Fit: **Preferred Size**
     - Horizontal Fit: Unconstrained

---

## BƯỚC 2 – Gắn PotentialTabUI lên ContentPotential

1. Chọn GameObject `ContentPotential`.
2. **Add Component → PotentialTabUI**.
3. Điền các slot trong Inspector:

| Slot Inspector | Kéo object nào vào |
|---|---|
| **Txt Potential Points** | `TxtPotentialPoints` (TMP_Text vừa tạo) |
| **Stat List Container** | `Content` (con của Viewport, tận cùng trong ScrollView) |
| **Potential Row Prefab** | `Assets/Prefabs/UI/Thông tin/PotentialStatRowPrefab` |
| **Txt Status** | `TxtStatus` (TMP_Text vừa tạo) |

> ⚠️ Kéo đúng `Content` (con của Viewport), KHÔNG kéo `ScrollView` hay `Viewport`.

---

## BƯỚC 3 – Kiểm tra / Tạo lại PotentialStatRowPrefab

Prefab đã có tại `Assets/Prefabs/UI/Thông tin/PotentialStatRowPrefab.prefab`.

### 3.1 Cấu trúc bên trong prefab

```
PotentialStatRowPrefab                ← Root, có PotentialStatRowUI component
├── Background                        ← Image nền (tuỳ chọn)
├── TxtStatName                       ← TMP_Text tên chỉ số (vd: "Tấn Công")
├── TxtPoints                         ← TMP_Text số điểm đã đầu tư (vd: "3 điểm")
├── TxtValue                          ← TMP_Text tổng giá trị (vd: "Tổng: +15")
└── BtnUpgrade                        ← Button nút "+"
    └── Text (TMP)                    ← TMP_Text "+"
```

### 3.2 Kích thước gợi ý
- Root: **Layout Element → Preferred Height: 80**

### 3.3 Gắn slot PotentialStatRowUI trong Prefab

| Slot | Component cần kéo vào |
|---|---|
| `Txt Stat Name` | `TxtStatName` – TMP_Text |
| `Txt Points` | `TxtPoints` – TMP_Text (text mẫu: `0 điểm`) |
| `Txt Value` | `TxtValue` – TMP_Text (text mẫu: `Tổng: +0`) |
| `Btn Upgrade` | `BtnUpgrade` – Button |

> Sau khi gán xong → nhấn **Apply (Overrides → Apply All)** để lưu Prefab.

### 3.4 Tắt Raycast Target (QUAN TRỌNG – giống SkillRowPrefab)
Giống lỗi đã gặp ở tab Kỹ Năng, `PotentialStatRowUI` cũng có `Awake()` tự tắt
raycast của Image và TMP_Text trang trí, nhưng để an toàn hãy tắt thủ công trong prefab:
- Mở `PotentialStatRowPrefab` trong Prefab Mode
- Chọn `Background` Image → bỏ tick **Raycast Target**
- Chọn `TxtStatName`, `TxtPoints`, `TxtValue` → bỏ tick **Raycast Target**
- **Apply All**

---

## BƯỚC 4 – Gắn ContentPotential vào CharacterPanelController

1. Chọn GameObject `CharacterPanel`.
2. Tìm component **CharacterPanelController** trong Inspector.
3. Slot **Content Potential** → kéo GameObject `ContentPotential` vào.

> Slot nhận kiểu `PotentialTabUI` — Unity tự resolve component khi kéo GameObject vào.

---

## BƯỚC 5 – Fix click button trong PotentialStatRowUI

Tương tự SkillRowUI, thêm `Awake()` để tắt raycast target tự động:

File `PotentialStatRowUI.cs` cần thêm `using UnityEngine.EventSystems;` và method `Awake()` + `IPointerClickHandler`. Code đã được cập nhật tự động (xem file nguồn).

---

## BƯỚC 6 – Ẩn ContentPotential ban đầu

1. Chọn `ContentPotential` → **bỏ tick** ở trên cùng Inspector.
2. `CharacterPanelController.SwitchTab(0)` sẽ tự ẩn khi start.

---

## BƯỚC 7 – Chuẩn bị DB (chạy 1 lần để test)

```sql
-- Cho player_id=1 có điểm tiềm năng để test
UPDATE player_data
SET info_char = JSON_SET(info_char, '$.potential_points', 5)
WHERE player_id = 1;
```

---

## BƯỚC 8 – Checklist Play Mode

```
[ ] Click tab "Tiềm Năng"
[ ] Console: "[PotentialTabUI] >>> Load() gọi – _playerId=X"
[ ] Console: "[PotentialTabUI] ✅ Server response nhận được"
[ ] Hiển thị "Điểm tiềm năng: X"
[ ] Có ít nhất 4-5 dòng stat (Tấn Công, Phòng Thủ, HP, MP, Gene)
[ ] Mỗi dòng: tên stat, số điểm, tổng giá trị, nút "+"
[ ] Nút "+" enabled khi có điểm (potential_points > 0)
[ ] Click "+" → dòng refresh, điểm giảm, chỉ số tăng
```

---

## SƠ ĐỒ HIERARCHY HOÀN CHỈNH

```
ContentPotential                      [PotentialTabUI]
├── TxtPotentialPoints                TMP_Text "Điểm tiềm năng: 0"
├── TxtStatus                         TMP_Text (ẩn ban đầu)
└── ScrollView
    └── Viewport
        └── Content                   [VerticalLayoutGroup] [ContentSizeFitter]
            └── (PotentialStatRow được Instantiate vào đây lúc runtime)
```

---

## LỖI THƯỜNG GẶP

| Triệu chứng | Nguyên nhân | Fix |
|---|---|---|
| Không có dòng stat nào | `statListContainer` hoặc `potentialRowPrefab` NULL | Gán đủ slot trong PotentialTabUI Inspector |
| Nút "+" không click được | TMP_Text hoặc Background Image chặn raycast | Bỏ tick Raycast Target trên Text và Image trang trí trong Prefab |
| "Đang tải tiềm năng..." không mất | Server tắt hoặc sai URL | Kiểm tra server đang chạy, `baseURL` đúng |
| `potential_points` = 0 nên nút bị xám | DB chưa có điểm | Chạy SQL ở Bước 7 |
| Layout bị vỡ, rows chồng nhau | Content thiếu VerticalLayoutGroup | Add Component VerticalLayoutGroup + ContentSizeFitter lên `Content` |
