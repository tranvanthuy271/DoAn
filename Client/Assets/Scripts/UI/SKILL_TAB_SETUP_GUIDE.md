# Hướng Dẫn Tạo UI Tab Kỹ Năng (Unity)

> Script liên quan:
> - `Assets/Scripts/Inventory/SkillTabUI.cs`
> - `Assets/Scripts/Inventory/SkillRowUI.cs`
> - `Assets/Scripts/UI/CharacterPanelController.cs`
> - Prefab có sẵn: `Assets/Prefabs/UI/Thông tin/SkillRowPrefab.prefab`

---

## BƯỚC 1 – Tạo cấu trúc GameObject trong ContentSkill

Mở scene **GameScene**. Trong Hierarchy, tìm (hoặc tạo) node `ContentSkill` nằm trong `CharacterPanel`.

Cấu trúc cần đạt được:

```
ContentSkill                       ← GameObject gốc của tab Kỹ Năng
├── TxtSkillPoints                 ← TMP_Text hiển thị điểm KN
├── TxtStatus                      ← TMP_Text hiển thị trạng thái / lỗi
└── ScrollView                     ← UI → ScrollView (có sẵn trong Unity)
    └── Viewport
        └── Content                ← Transform chứa các SkillRow
```

### 1.1 Tạo TxtSkillPoints
1. Chuột phải vào `ContentSkill` → **UI → Text - TextMeshPro** → đặt tên `TxtSkillPoints`.
2. **Rect Transform:** Anchor top-stretch, Pos Y ≈ -20, Height ≈ 30.
3. **Text:** `Điểm kỹ năng: 0`
4. **Alignment:** Left / Middle.
5. Font size: 18, style Bold.

### 1.2 Tạo TxtStatus
1. Chuột phải vào `ContentSkill` → **UI → Text - TextMeshPro** → đặt tên `TxtStatus`.
2. **Rect Transform:** Dưới TxtSkillPoints, Height ≈ 25.
3. **Text:** (để trống)
4. Color: vàng `#FFD700`, size 14, style Italic.
5. `Enabled = false` (sẽ được BẬT tự động khi có lỗi/loading).

### 1.3 Tạo ScrollView
1. Chuột phải vào `ContentSkill` → **UI → Scroll View**.
2. **Rect Transform:** Stretch both, Left/Right/Top ≈ 0, Bottom ≈ 0, đẩy top xuống vừa TxtSkillPoints + TxtStatus (≈ 60px).
3. **Inspector của ScrollView:**
   - `Horizontal = false` (tắt)
   - `Vertical = true` (bật)
   - `Scroll Sensitivity = 30`
4. Xóa object `Scrollbar Horizontal` nếu có (không cần).

### 1.4 Cấu hình Content bên trong ScrollView
1. Mở rộng `ScrollView → Viewport → Content`.
2. Chọn `Content` → Add Component:
   - **Vertical Layout Group**
     - Child Alignment: Upper Left
     - Control Child Size: Width ✅, Height ✅
     - Child Force Expand: Width ✅, Height ✗
     - Spacing: 6
     - Padding: Left 8, Right 8, Top 4, Bottom 4
   - **Content Size Fitter**
     - Vertical Fit: **Preferred Size**
     - Horizontal Fit: Unconstrained

---

## BƯỚC 2 – Gắn SkillTabUI lên ContentSkill

1. Chọn GameObject `ContentSkill`.
2. **Add Component → SkillTabUI** (tìm bằng tên trong Add Component).
3. Điền các slot trong **Inspector** như bảng dưới:

| Slot Inspector | Kéo object nào vào |
|---|---|
| **Txt Skill Points** | `TxtSkillPoints` (TMP_Text vừa tạo) |
| **Skill List Container** | `Content` (Transform con cùng tận của ScrollView) |
| **Skill Row Prefab** | `Assets/Prefabs/UI/Thông tin/SkillRowPrefab` |
| **Txt Status** | `TxtStatus` (TMP_Text vừa tạo) |

> ⚠️ **Quan trọng:** Kéo đúng `Content` (con của Viewport), KHÔNG kéo `ScrollView` hay `Viewport`.

---

## BƯỚC 3 – Kiểm tra / Tạo lại SkillRowPrefab

Prefab đã có tại `Assets/Prefabs/UI/Thông tin/SkillRowPrefab.prefab`.  
Mở ra kiểm tra các slot của **SkillRowUI** component:

### 3.1 Cấu trúc bên trong prefab
```
SkillRowPrefab                     ← Root, có SkillRowUI component
├── Background                     ← Image nền hàng (tuỳ chọn)
├── IconImage                      ← Image icon skill (tuỳ chọn)
├── VStack (VerticalLayoutGroup)
│   ├── HStack_Top
│   │   ├── TxtSkillName           ← TMP_Text tên skill + element tag
│   │   └── TxtLevel               ← TMP_Text "Lv.X / Y"
│   ├── TxtDesc                    ← TMP_Text mô tả effect
│   └── TxtRequire                 ← TMP_Text yêu cầu nâng cấp
└── BtnUpgrade                     ← Button nút "+"
    └── TxtBtnLabel                ← TMP_Text "+"
```

### 3.2 Kích thước gợi ý cho Root prefab
- **Preferred Height:** 100 (VerticalLayoutGroup trên Content sẽ auto-size)
- **Layout Element → Preferred Height: 100**

### 3.3 Gắn slot SkillRowUI trong Prefab
Slot | Component cần kéo vào
---|---
`Txt Skill Name` | `TxtSkillName` – TMP_Text
`Txt Level` | `TxtLevel` – TMP_Text (text mẫu: `Lv.0 / 5`)
`Txt Require` | `TxtRequire` – TMP_Text (text mẫu: _(để trống)_)
`Txt Desc` | `TxtDesc` – TMP_Text (text mẫu: _(để trống)_)
`Btn Upgrade` | `BtnUpgrade` – Button
`Icon Image` | `IconImage` – Image _(optional)_

> Sau khi gán xong → nhớ nhấn **Apply** (Overrides → Apply All) để lưu lại Prefab.

---

## BƯỚC 4 – Gắn ContentSkill vào CharacterPanelController

1. Chọn GameObject `CharacterPanel`.
2. Tìm component **CharacterPanelController** trong Inspector.
3. Slot **Content Skill** → kéo **component SkillTabUI** từ `ContentSkill` vào.

> Lưu ý: slot `Content Skill` nhận kiểu `SkillTabUI`, không phải `GameObject`.  
> Cách kéo đúng: kéo thẳng GameObject `ContentSkill` vào slot — Unity sẽ tự resolve component.

---

## BƯỚC 5 – Ẩn ContentSkill ban đầu

`CharacterPanelController.Start()` sẽ tự quản lý ẩn/hiện các tab.  
Tuy nhiên để Editor gọn:

1. Chọn `ContentSkill` → **bỏ tick** ở trên cùng Inspector (SetActive false).
2. Khi chạy game, `SwitchTab(0)` sẽ tự bật `ContentEquipment` và ẩn `ContentSkill`.

---

## BƯỚC 6 – Kiểm tra APIClient trong scene

1. Tìm GameObject có component **APIClient** trong Hierarchy (thường là `GameManager` hoặc `APIClient`).
2. Đảm bảo `baseURL` = `http://localhost:5062/api` (hoặc địa chỉ server thực).
3. Đảm bảo `APIClient.Instance` không null khi scene load (singleton).

---

## BƯỚC 7 – Chạy thử trong Editor

### 7.1 Chuẩn bị DB (chạy 1 lần)
```sql
-- Cho player_id=1 có skill points để test nút nâng cấp
UPDATE player_data
SET info_char = JSON_SET(info_char, '$.skill_points', 5)
WHERE player_id = 1;
```

### 7.2 Play Mode checklist
```
[ ] Đăng nhập / Load scene với player hợp lệ
[ ] Nhấn mở CharacterPanel
[ ] Click tab "Kỹ Năng"
[ ] Console: KHÔNG có NullReferenceException
[ ] Console: Thấy log "[APIClient] Skills response: ..."
[ ] Tab hiển thị "Điểm kỹ năng: X"
[ ] Danh sách có ít nhất 1 skill row
[ ] Mỗi row hiển thị: tên skill, "Lv.0 / 5", mô tả, nút +
[ ] Skill Universal có tag [Universal] trong tên
[ ] Nút + disabled khi skill_points = 0
[ ] Nút + enabled sau khi update DB (bước 7.1)
[ ] Click nút + → row refresh, level tăng, SP giảm
```

---

## SƠ ĐỒ HIERARCHY HOÀN CHỈNH

```
Canvas
└── CharacterPanel                        [CharacterPanelController]
    ├── Window
    │   ├── Header
    │   │   ├── TxtTitle                  TMP_Text "Nhân Vật"
    │   │   └── BtnClose                  Button
    │   ├── TabBar                        HorizontalLayoutGroup
    │   │   ├── BtnEquipment              Button
    │   │   ├── BtnSkill                  Button
    │   │   └── BtnPotential              Button
    │   └── TabContents
    │       ├── ContentEquipment          [EquipmentPanelUI]
    │       ├── ContentSkill              [SkillTabUI]        ← TẠO MỚI
    │       │   ├── TxtSkillPoints        TMP_Text
    │       │   ├── TxtStatus             TMP_Text
    │       │   └── ScrollView
    │       │       └── Viewport
    │       │           └── Content       [VerticalLayoutGroup] [ContentSizeFitter]
    │       │               └── (SkillRow được Instantiate vào đây lúc runtime)
    │       └── ContentPotential          [PotentialTabUI]
```

---

## LỖI THƯỜNG GẶP

| Triệu chứng | Nguyên nhân | Fix |
|---|---|---|
| Không có row nào, không có log lỗi nào | `contentSkill` NULL trong CharacterPanelController | Gán slot Content Skill trong Inspector |
| Log: `Thiếu skillRowPrefab hoặc skillListContainer` | Slot SkillTabUI chưa gán đủ | Gán đủ 4 slot trong SkillTabUI Inspector |
| Rows xuất hiện nhưng chồng lên nhau | Content thiếu VerticalLayoutGroup | Add Component VerticalLayoutGroup + ContentSizeFitter lên Content |
| Scroll không kéo được | ContentSizeFitter thiếu hoặc sai | Vertical Fit = Preferred Size |
| Tên skill trống | `txtSkillName` NULL trong Prefab | Gán slot trong SkillRowPrefab |
| Nút + không click được | `btnUpgrade` NULL hoặc `can_upgrade=false` | Kiểm tra slot prefab; update skill_points trong DB |
| Text "Đang tải kỹ năng..." không mất | API call fail (server tắt / sai URL) | Kiểm tra server đang chạy, baseURL đúng |
| `playerId` = -1, không load | Chưa login hoặc PlayerPrefs không có USER_ID | Đảm bảo login flow save `PlayerPrefs.SetInt("USER_ID", id)` |
