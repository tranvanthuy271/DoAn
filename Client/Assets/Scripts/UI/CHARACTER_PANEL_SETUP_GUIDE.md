# Character Panel – Hướng Dẫn Setup Unity

Panel nhân vật 3 tab: **Trang Bị** | **Kỹ Năng** | **Tiềm Năng**

---

## 1. Chuẩn bị trước khi bắt đầu

- Đã có `EquipmentPanelUI` hoạt động trong scene.
- `APIClient` đã được đặt trong scene và `baseURL` trỏ đúng server.
- TextMesh Pro (TMP) đã được import.

---

## 2. Tạo cấu trúc Hierarchy

```
Canvas
└── CharacterPanel                    ← Panel gốc (Image + CanvasGroup tuỳ ý)
    ├── Background                    ← Image nền tối mờ (tuỳ ý)
    ├── Window                        ← Khung cửa sổ chính
    │   ├── Header
    │   │   ├── TxtTitle              ← TMP_Text "Nhân Vật"
    │   │   └── BtnClose              ← Button "X"  (gọi characterPanel.Hide())
    │   ├── TabBar                    ← HorizontalLayoutGroup
    │   │   ├── BtnEquipment          ← Button "Trang Bị"
    │   │   ├── BtnSkill              ← Button "Kỹ Năng"
    │   │   └── BtnPotential          ← Button "Tiềm Năng"
    │   └── TabContents
    │       ├── ContentEquipment      ← Chứa EquipmentPanelUI (đã có)
    │       ├── ContentSkill          ← Tab kỹ năng (tạo mới bên dưới)
    │       └── ContentPotential      ← Tab tiềm năng (tạo mới bên dưới)
```

---

## 3. Gắn CharacterPanelController

1. Chọn GameObject **CharacterPanel**.
2. **Add Component → CharacterPanelController**.
3. Điền các slot trong Inspector:

| Inspector Slot       | Gán vào                          |
|----------------------|----------------------------------|
| Panel Root           | `CharacterPanel` (chính nó)      |
| Btn Equipment        | `BtnEquipment`                   |
| Btn Skill            | `BtnSkill`                       |
| Btn Potential        | `BtnPotential`                   |
| Content Equipment    | `ContentEquipment` (GameObject)  |
| Content Skill        | `ContentSkill` (component SkillTabUI) |
| Content Potential    | `ContentPotential` (component PotentialTabUI) |
| Color Active Tab     | Màu nút đang chọn (vd: xanh dương) |
| Color Inactive Tab   | Màu nút không chọn (vd: xám)    |

4. Mặc định panel **ẩn khi Start** – Script tự xử lý.

---

## 4. Nút mở/đóng Panel (Toggle Button)

1. Tạo hoặc chọn Button trong HUD (vd: nút hình nhân vật).
2. **Add Component → CharacterPanelToggleButton**.
3. Kéo **CharacterPanel** vào slot `Character Panel`.

> Sau khi login thành công, gọi:
> ```csharp
> characterPanel.SetPlayerId(loginResponse.user_id);
> ```
> Thường đặt trong `LoginController.cs` hoặc `SelectElementController.cs`.

---

## 5. Setup ContentEquipment (Tab Trang Bị)

Dùng lại `EquipmentPanelUI` đã có:

1. Di chuyển GameObject `EquipmentPanel` vào trong `ContentEquipment` (hoặc dùng chính `ContentEquipment` làm root cho EquipmentPanelUI).
2. Không cần chỉnh thêm – CharacterPanelController sẽ bật/tắt `ContentEquipment` khi chuyển tab.

---

## 6. Setup ContentSkill (Tab Kỹ Năng)

### 6.1 Cấu trúc ContentSkill

```
ContentSkill                          ← GameObject, gắn SkillTabUI.cs
├── TxtSkillPoints                    ← TMP_Text "Điểm kỹ năng: X"
├── TxtStatus                         ← TMP_Text trạng thái (loading / lỗi)
└── ScrollView                        ← UI → ScrollView (kéo thả từ menu)
    └── Viewport
        └── Content                   ← gán vào "Skill List Container"
            │   ← VerticalLayoutGroup
            │   ← ContentSizeFitter (Vertical Fit = Preferred Size)
```

**VerticalLayoutGroup** trên `Content`:
- Child Alignment: Upper Center
- Control Child Size: Width ✓, Height ✓
- Child Force Expand: Width ✓, Height ✗
- Spacing: 8

### 6.2 Tạo Prefab SkillRow

1. Tạo GameObject `SkillRowPrefab`:

```
SkillRowPrefab                        ← Image nền dòng, Height ~90
├── IconImage                         ← Image (tuỳ chọn, icon skill)
├── TxtSkillName                      ← TMP_Text  (bold, vd: "[Fire] Cầu Lửa")
├── TxtLevel                          ← TMP_Text  (vd: "Lv.2 / 5")
├── TxtRequire                        ← TMP_Text  (vd: "Nâng: 1 SP • cần lv.4")
├── TxtDesc                           ← TMP_Text  (nhỏ, mô tả hiệu ứng)
└── BtnUpgrade                        ← Button "+"  (Width ~40, Height ~40)
```

2. **Add Component → SkillRowUI** vào `SkillRowPrefab`.
3. Kéo các UI vào Inspector slots của `SkillRowUI`.
4. Kéo prefab vào **Project** để tạo `.prefab`.

### 6.3 Gắn tham chiếu vào SkillTabUI

Chọn `ContentSkill`, Inspector của `SkillTabUI`:

| Slot                  | Gán vào                     |
|-----------------------|-----------------------------|
| Txt Skill Points      | `TxtSkillPoints`            |
| Skill List Container  | `Content` (trong ScrollView)|
| Skill Row Prefab      | Prefab `SkillRowPrefab`     |
| Txt Status            | `TxtStatus`                 |

---

## 7. Setup ContentPotential (Tab Tiềm Năng)

### 7.1 Cấu trúc ContentPotential

```
ContentPotential                      ← GameObject, gắn PotentialTabUI.cs
├── TxtPotentialPoints                ← TMP_Text "Điểm tiềm năng: X"
├── TxtStatus                         ← TMP_Text trạng thái
└── StatList                          ← VerticalLayoutGroup, 5 dòng cố định
    │   ← ContentSizeFitter (nếu muốn auto height)
```

> 5 chỉ số tiềm năng: **Tấn Công** · **Máu (HP)** · **Mana (MP)** · **Phòng Thủ** · **Gene**

### 7.2 Tạo Prefab PotentialStatRow

```
PotentialStatRowPrefab                ← Image nền, Height ~70
├── TxtStatName                       ← TMP_Text  (vd: "Tấn Công")
├── TxtPoints                         ← TMP_Text  (vd: "3 điểm")
├── TxtValue                          ← TMP_Text  (vd: "Tổng: +15  (+5/điểm)")
└── BtnUpgrade                        ← Button "+"
```

1. **Add Component → PotentialStatRowUI** vào `PotentialStatRowPrefab`.
2. Kéo các UI vào Inspector slots của `PotentialStatRowUI`.
3. Kéo prefab vào Project.

### 7.3 Gắn tham chiếu vào PotentialTabUI

| Slot                    | Gán vào                     |
|-------------------------|-----------------------------|
| Txt Potential Points    | `TxtPotentialPoints`        |
| Stat List Container     | `StatList`                  |
| Potential Row Prefab    | Prefab `PotentialStatRowPrefab` |
| Txt Status              | `TxtStatus`                 |

---

## 8. Gọi SetPlayerId sau khi Login

Trong `LoginController.cs` (hoặc `SelectElementController.cs`), sau khi login / tạo nhân vật thành công:

```csharp
// Tìm CharacterPanelController trong scene
var charPanel = FindObjectOfType<CharacterPanelController>();
if (charPanel != null)
    charPanel.SetPlayerId(loginResponse.user_id);
```

---

## 9. Kết nối BtnClose (nút X)

Trong Inspector của `BtnClose` → **On Click()** → kéo `CharacterPanel` → chọn `CharacterPanelController.Hide`.

---

## 10. Kiểm tra hoạt động

### Tab Trang Bị
- Nhấn nút toggle → panel mở, tab Trang Bị hiển thị EquipmentPanelUI.

### Tab Kỹ Năng
- Nhấn **Kỹ Năng** → load từ `GET /api/player/{id}/skills`.
- Mỗi dòng hiển thị: tên skill [hệ], **Lv.X / MaxLv**, yêu cầu nâng, mô tả efect.
- Nút `+` **chỉ active** khi player đủ level & đủ SP.
- Nhấn `+` → `POST /api/player/{id}/skills/upgrade` → UI tự load lại.

### Tab Tiềm Năng
- Nhấn **Tiềm Năng** → load từ `GET /api/player/{id}/potential`.
- Hiển thị số điểm tiềm năng còn lại ở trên cùng.
- 5 dòng chỉ số: tên, số điểm đã đầu tư, tổng giá trị nhận được.
- Nhấn `+` → `POST /api/player/{id}/potential/upgrade` → UI tự load lại.

---

## 11. API Endpoints tham khảo

| Method | URL | Mô tả |
|--------|-----|-------|
| GET    | `/api/player/{id}/skills`           | Lấy toàn bộ skill + level player |
| POST   | `/api/player/{id}/skills/upgrade`   | Body: `{"skill_id": 1}` |
| GET    | `/api/player/{id}/potential`        | Lấy 5 chỉ số tiềm năng |
| POST   | `/api/player/{id}/potential/upgrade`| Body: `{"stat_name": "attack"}` |

**stat_name hợp lệ:** `attack` · `hp` · `mp` · `defense` · `gene`

---

## 12. Các lỗi thường gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| `Chưa có playerId` | Chưa gọi `SetPlayerId()` | Gọi sau khi login |
| `APIClient không tồn tại` | Thiếu APIClient trong scene | Thêm GameObject gắn `APIClient` |
| Nút `+` luôn bị tắt | `can_upgrade = false` từ server | Kiểm tra level nhân vật & SP trong DB |
| Danh sách skill trống | DB chưa import `skill_template` | Import lại `gamedb_v2.sql` |
| `Skill List Container` null | Chưa kéo `Content` vào slot | Kéo đúng object `Content` trong ScrollView |
