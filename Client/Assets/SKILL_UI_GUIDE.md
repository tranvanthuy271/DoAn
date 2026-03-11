# SKILL UI & HP/MP BAR — Hướng dẫn cấu hình

> Tài liệu này hướng dẫn bạn cài đặt toàn bộ HUD gồm:
> - **Skill Hotbar** (icon, nút bấm, countdown cooldown)
> - **HP Bar** (thanh máu slider)
> - **MP Bar** (thanh mana slider) — hiển thị **ngoài tab thông tin**

---

## Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Xây dựng Canvas HUD từ đầu (step-by-step)](#2-xây-dựng-canvas-hud-từ-đầu)
   - 2.1 Tạo Canvas chính
   - 2.2 Tạo HUD panel
   - 2.3 Tạo HealthBarObject
   - 2.4 Tạo MpBarObject
   - 2.5 Tạo SkillHotbar + các Slot
3. [Skill Hotbar — Nút kích hoạt + Cooldown + Icon](#3-skill-hotbar)
   - 3.1 Tạo Prefab SkillSlot
   - 3.2 Gán icon skill
   - 3.3 Kiểm tra
4. [Thêm skill mới vào PlayerSkillManager](#4-thêm-skill-mới)
5. [Tuỳ chỉnh nâng cao](#5-tuỳ-chỉnh-nâng-cao)
6. [Câu hỏi thường gặp](#6-câu-hỏi-thường-gặp)

---

## 1. Tổng quan kiến trúc

```
Canvas (Screen Space - Overlay)
├── HUD                                      ← Panel trong suốt, full-screen
│   ├── StatsGroup                           ← Vertical Layout Group (góc trên trái)
│   │   ├── HealthBarObject                  ← HealthBar.cs — thanh HP
│   │   └── MpBarObject                      ← MpBar.cs     — thanh MP
│   └── SkillHotbar                          ← SkillHotbarUI.cs (góc dưới giữa)
│       ├── Slot0                            ← SkillSlotUI.cs
│       ├── Slot1                            ← SkillSlotUI.cs
│       └── Slot2                            ← SkillSlotUI.cs
└── CharacterPanel                           ← (tab thông tin cũ, giữ nguyên)
```

**Kết quả trên màn hình:**
```
┌──────────────────────────────────────────┐
│ [████████░░] 850/1000 HP                 │  ← HealthBarObject
│ [██████░░░░]  60/100 MP                  │  ← MpBarObject
│                                          │
│                                          │
│                                          │
│            [🔥][❄️][⚡]                  │  ← SkillHotbar
└──────────────────────────────────────────┘
```

**Script mới được thêm vào dự án:**

| Script | Vị trí | Chức năng |
|--------|--------|-----------|
| `SkillSlotUI.cs` | `Scripts/UI/HUD/` | Icon + nút bấm + cooldown overlay + đếm ngược |
| `SkillHotbarUI.cs` | `Scripts/UI/HUD/` | Quản lý tất cả slot, tự bind vào `PlayerSkillManager` |
| `MpBar.cs` | `Scripts/UI/HUD/` | Thanh MP slider, đọc từ `NetworkPlayerDataSync` |

**Script được cập nhật:**

| Script | Thay đổi |
|--------|---------|
| `SkillData.cs` | Thêm `GetCooldownRemaining()` — trả về số giây cooldown còn lại |
| `PlayerSkillManager.cs` | Thêm `GetSkill(int)`, `GetSkillCount()`, `TryUseSkillByIndex(int)` |

---

## 2. Xây dựng Canvas HUD từ đầu

### 2.1 Tạo Canvas chính

> Bỏ qua bước này nếu scene đã có Canvas.

1. Trong **Hierarchy** → chuột phải → **UI → Canvas**.
2. Đặt tên `Canvas`.
3. Inspector của Canvas:
   - **Render Mode** = `Screen Space - Overlay`
   - **UI Scale Mode** = `Scale With Screen Size`
   - **Reference Resolution** = `1920 × 1080`
   - **Screen Match Mode** = `Match Width Or Height`, Match = `0.5`
4. **CanvasScaler** đã tự thêm — không cần chỉnh thêm.
5. Thêm component **`GraphicRaycaster`** nếu chưa có (Unity thêm tự động).

---

### 2.2 Tạo HUD panel

HUD là một panel trong suốt phủ toàn màn hình, chứa tất cả UI game (HP, MP, Skill).

1. Chuột phải vào `Canvas` → **Create Empty**, đặt tên `HUD`.
2. Inspector của `HUD` → **RectTransform**:
   - Chọn **Anchor Presets** (góc trên trái của Inspector) → giữ `Alt+Shift` và click ô **Stretch / Stretch** (ô cuối cùng góc dưới phải — full stretch).
   - Left = 0, Right = 0, Top = 0, Bottom = 0.
   - **Không** thêm Image component (để trong suốt hoàn toàn).

> Tất cả UI HUD (HP, MP, Skill) sẽ là **con của HUD**.

---

### 2.3 Tạo HealthBarObject (thanh HP)

**Cấu trúc:**
```
HUD
└── StatsGroup               ← Vertical Layout Group
    ├── HealthBarObject      ← HealthBar.cs
    │   ├── HPLabel          [TMP_Text]
    │   ├── HPSlider         [Slider]
    │   │   ├── Background   [Image]  (tự sinh bởi Unity)
    │   │   ├── Fill Area
    │   │   │   └── Fill     [Image]  ← fillImage
    │   │   └── Handle Slide Area (xoá hoặc ẩn)
    │   └── HPText           [TMP_Text]
    └── MpBarObject          ← (xem mục 2.4)
```

**Từng bước:**

**Bước A — Tạo StatsGroup**

1. Chuột phải vào `HUD` → **Create Empty**, đặt tên `StatsGroup`.
2. **RectTransform** của `StatsGroup`:
   - Anchor: **Top Left** (`Alt+Shift` → click ô góc trên trái).
   - Pivot = (0, 1).
   - Pos X = 20, Pos Y = -20.
   - Width = 280, Height = 80 *(sẽ tự giãn khi thêm Layout)*.
3. Thêm component **`Vertical Layout Group`**:
   - Spacing = 6
   - Child Alignment = **Upper Left**
   - Control Child Size: Width ✅, Height ✅
   - Child Force Expand: Width ✅, Height ❌
4. Thêm component **`Content Size Fitter`**:
   - Vertical Fit = **Preferred Size**

**Bước B — Tạo HealthBarObject**

1. Chuột phải vào `StatsGroup` → **Create Empty**, đặt tên `HealthBarObject`.
2. Thêm component **`Horizontal Layout Group`** (để HPLabel + HPSlider + HPText xếp ngang):
   - Spacing = 6
   - Child Alignment = **Middle Left**
   - Control Child Size: Width ❌, Height ✅
   - Child Force Expand: Width ❌, Height ❌
3. Thêm component **`Layout Element`** → Preferred Height = 28.
4. Tạo các **child** trong `HealthBarObject`:

   **Child 1 — HPLabel** (`TMP_Text`):
   - Chuột phải → **UI → Text - TextMeshPro**, đặt tên `HPLabel`.
   - Text = `HP`, Font Size = 16, Bold, Color = `#FF4444`.
   - **Layout Element**: Preferred Width = 30, Min Width = 30.

   **Child 2 — HPSlider** (`Slider`):
   - Chuột phải → **UI → Slider**, đặt tên `HPSlider`.
   - **Slider** component:
     - Min Value = 0, Max Value = 1, Value = 1.
     - Interactable = ❌ *(tắt — chỉ hiển thị, không kéo được)*.
     - **Xoá** `Handle Slide Area` hoặc ẩn (không cần núm kéo).
   - **Layout Element**: Flexible Width = 1 *(slider sẽ giãn đầy còn lại)*, Min Width = 100.
   - Chọn `Fill` (con của `Fill Area`) → **Image** component → Color = `#00C851` *(xanh lá)*.

   **Child 3 — HPText** (`TMP_Text`):
   - Chuột phải → **UI → Text - TextMeshPro**, đặt tên `HPText`.
   - Text = `1000/1000`, Font Size = 13, Color = trắng.
   - Alignment = Middle Right.
   - **Layout Element**: Preferred Width = 80, Min Width = 80.

5. Quay lại `HealthBarObject`, thêm component **`HealthBar`**:
   - `Health Slider` ← kéo `HPSlider`
   - `Fill Image` ← kéo `Fill` (Image bên trong HPSlider → Fill Area → Fill)
   - `Health Text TMP` ← kéo `HPText`
   - Full Health Color = `#00C851`, Low Health Color = `#FF4444`, Threshold = 0.3

---

### 2.4 Tạo MpBarObject (thanh MP)

Cấu trúc y hệt `HealthBarObject` nhưng dùng màu xanh dương.

1. Chuột phải vào `StatsGroup` → **Create Empty**, đặt tên `MpBarObject`.
2. Thêm component **`Horizontal Layout Group`** (cùng setting như HealthBarObject).
3. Thêm component **`Layout Element`** → Preferred Height = 28.
4. Tạo các **child** trong `MpBarObject`:

   **Child 1 — MPLabel** (`TMP_Text`):
   - Text = `MP`, Font Size = 16, Bold, Color = `#3366FF`.
   - **Layout Element**: Preferred Width = 30, Min Width = 30.

   **Child 2 — MPSlider** (`Slider`):
   - Min = 0, Max = 1, Value = 1, Interactable = ❌.
   - Xoá `Handle Slide Area`.
   - **Layout Element**: Flexible Width = 1, Min Width = 100.
   - Chọn `Fill` → **Image** component → Color = `#3366FF` *(xanh dương)*.

   **Child 3 — MPText** (`TMP_Text`):
   - Text = `100/100`, Font Size = 13, Color = trắng.
   - Alignment = Middle Right.
   - **Layout Element**: Preferred Width = 80, Min Width = 80.

5. Thêm component **`MpBar`** vào `MpBarObject`:
   - `Mp Slider` ← kéo `MPSlider`
   - `Fill Image` ← kéo `Fill` (bên trong MPSlider)
   - `Mp Text` ← kéo `MPText`
   - Full MP Color = `#3366FF`, Low MP Color = `#9933FF`, Threshold = 0.25

**Sau bước 2.3 + 2.4, kết quả Hierarchy:**
```
HUD
└── StatsGroup  [Vertical Layout Group]
    ├── HealthBarObject  [HealthBar]
    │   ├── HPLabel
    │   ├── HPSlider
    │   └── HPText
    └── MpBarObject  [MpBar]
        ├── MPLabel
        ├── MPSlider
        └── MPText
```

---

### 2.5 Tạo SkillHotbar + các Slot

**Bước A — Tạo SkillHotbar container**

1. Chuột phải vào `HUD` → **Create Empty**, đặt tên `SkillHotbar`.
2. **RectTransform** của `SkillHotbar`:
   - Anchor: **Bottom Center** (`Alt+Shift` → click ô dưới giữa).
   - Pivot = (0.5, 0).
   - Pos X = 0, Pos Y = 20.
   - Width = tự động (để Layout tính), Height = 80.
3. Thêm component **`Horizontal Layout Group`**:
   - Spacing = 8
   - Child Alignment = **Middle Center**
   - Control Child Size: Width ❌, Height ❌
   - Child Force Expand: Width ❌, Height ❌
4. Thêm component **`Content Size Fitter`**:
   - Horizontal Fit = **Preferred Size**
5. Thêm component **`SkillHotbarUI`**:
   - Auto Find = ✅

**Bước B — Tạo Slot0, Slot1, Slot2**

Làm **3 lần** (hoặc bao nhiêu skill bạn có). Ví dụ tạo `Slot0`:

1. Chuột phải vào `SkillHotbar` → **UI → Image**, đặt tên `Slot0`.  
   *(Dùng Image thay vì Empty để nhìn thấy khung slot)*
2. **RectTransform**: Width = 72, Height = 72.
3. **Image** component:
   - Color = `#1A1A2E` với Alpha = 200 *(background tối)*.
   - Image Type = Simple.
4. Thêm component **`Button`** vào `Slot0`.
5. Tạo **child** `IconImage`:
   - Chuột phải `Slot0` → **UI → Image**, đặt tên `IconImage`.
   - **RectTransform**: Anchor = Stretch/Stretch, Left=4, Right=4, Top=4, Bottom=4.
   - Sprite: để trống (sẽ gán qua script).
   - Color = trắng.
6. Tạo **child** `CooldownOverlay`:
   - Chuột phải `Slot0` → **UI → Image**, đặt tên `CooldownOverlay`.
   - **RectTransform**: Anchor = Stretch/Stretch, tất cả = 0.
   - **Image** component:
     - **Image Type** = **Filled**
     - **Fill Method** = **Radial 360**
     - **Fill Origin** = **Top**
     - **Clockwise** = ✅
     - Color = `#000000`, Alpha = 180.
     - Fill Amount = 0 *(ban đầu ẩn)*.
7. Tạo **child** `CooldownText`:
   - Chuột phải `Slot0` → **UI → Text - TextMeshPro**, đặt tên `CooldownText`.
   - **RectTransform**: Anchor = Stretch/Stretch, tất cả = 0.
   - Text = rỗng, Font Size = 20, Bold, Alignment = Center/Middle.
   - Color = trắng.
   - **Outline** hoặc **Shadow** để dễ đọc: thêm component `Shadow` hoặc dùng Material.
8. Thêm component **`SkillSlotUI`** vào `Slot0`:
   - `Icon Image` ← kéo `IconImage`
   - `Cooldown Overlay` ← kéo `CooldownOverlay`
   - `Cooldown Text` ← kéo `CooldownText`
   - `Skill Button` ← kéo `Button` (chính là Slot0)
   - Ready Color = `#FFFFFF`, Cooldown Color = `#666666`.

**Lặp lại** các bước trên để tạo `Slot1`, `Slot2` (đổi tên, giữ nguyên setting).

**Bước C — Gán Slots vào SkillHotbarUI**

1. Chọn `SkillHotbar` → Inspector → **`SkillHotbarUI`**.
2. **Slots** → Size = 3 → kéo `Slot0`, `Slot1`, `Slot2` vào.
3. **Skill Icons** → xem mục 3.2.

**Hierarchy hoàn chỉnh của SkillHotbar:**
```
SkillHotbar  [Horizontal Layout Group + Content Size Fitter + SkillHotbarUI]
├── Slot0    [Image + Button + SkillSlotUI]
│   ├── IconImage        [Image]
│   ├── CooldownOverlay  [Image — Filled Radial360]
│   └── CooldownText     [TMP_Text]
├── Slot1    [Image + Button + SkillSlotUI]
│   ├── IconImage
│   ├── CooldownOverlay
│   └── CooldownText
└── Slot2    [Image + Button + SkillSlotUI]
    ├── IconImage
    ├── CooldownOverlay
    └── CooldownText
```

---

## 3. Skill Hotbar — Chi tiết thêm

### 3.1 Tạo Prefab SkillSlot (để tái sử dụng)

Sau khi tạo xong `Slot0` theo mục 2.5:

1. Trong **Project** panel, tạo thư mục `Assets/Prefabs/UI/`.
2. Kéo `Slot0` từ Hierarchy **vào thư mục** trên → Unity hỏi "Create Original Prefab" → chọn **Original Prefab**.
3. Đổi tên file thành `SkillSlot.prefab`.
4. Để tạo `Slot1`, `Slot2`: kéo `SkillSlot.prefab` từ Project vào `SkillHotbar` trong Hierarchy, đổi tên.

---

### 3.2 Gán icon skill

Icons là file PNG được import vào Unity dưới dạng Sprite.

1. Chuẩn bị file PNG icon (128×128 hoặc 256×256 pixels).
2. Kéo file PNG vào `Assets/Icons/Skills/` trong Project panel.
3. Chọn file PNG → Inspector:
   - **Texture Type** = `Sprite (2D and UI)`
   - **Sprite Mode** = `Single`
   - Nhấn **Apply**.
4. Chọn `SkillHotbar` trong Hierarchy → Inspector → **`SkillHotbarUI`**:
   - **Skill Icons** → Size = 3 (hoặc số slot bạn có).
   - Element 0 = icon của skill thứ 0 *(kéo Sprite từ Project vào)*.
   - Element 1 = icon skill thứ 1, v.v.

> **Thứ tự phải khớp** với thứ tự `skills` trong `PlayerSkillManager` trên Player Prefab.

---

### 3.3 Kiểm tra

Nhấn **Play** → kiểm tra theo bảng:

| Trạng thái | Kết quả mong đợi |
|-----------|-----------------|
| Khởi động | Hotbar xuất hiện dưới màn hình, HP/MP bar góc trên trái |
| Skill sẵn sàng | Icon sáng, overlay ẩn, button có thể nhấn |
| Dùng skill (phím tắt hoặc nhấn nút) | Overlay đen quay tròn đếm ngược |
| Đang cooldown | Text hiện `"2s"`, `"1.5s"`, rồi ẩn khi xong |
| HP/MP thay đổi | Thanh slider cập nhật ngay lập tức |

---

## 4. Thêm skill mới

### Phía Client (Gameplay)

1. Trong **Prefab Player** → component `PlayerSkillManager` → **Skills** → `+`.
2. Điền thông tin:
   - **Skill Name**: tên duy nhất (ví dụ `"IceBolt"`)
   - **Activation Key**: phím tắt bàn phím (ví dụ `KeyCode.E`)
   - **Cooldown**: số giây cooldown (ví dụ `4`)
   - **Projectile Prefab**: kéo prefab đạn vào
   - **Projectile Speed / Lifetime / Spawn Offset**: tuỳ chỉnh
   - **Animation Trigger Name**: tên Trigger trong Animator (để trống nếu không có)
3. Trong `SkillHotbarUI`:
   - **Slots** → thêm một `SkillSlotUI` mới (kéo thêm Prefab SkillSlot vào scene).
   - **Skill Icons** → thêm icon tương ứng.

> Thứ tự trong list `Skills` của `PlayerSkillManager` **phải khớp** với thứ tự `Slots` trong `SkillHotbarUI`.

### Phía Server (Dữ liệu)

Skill gameplay (projectile) là **client-side only**. Nếu bạn muốn skill này được kiểm soát bởi server (tốn MP, có level, v.v.), tham khảo `PlayerController.cs` — endpoint `/api/player/{id}/skills`.

---

## 5. Tuỳ chỉnh nâng cao

### Thay đổi hiệu ứng overlay cooldown

Mở `SkillSlotUI.cs`, phương thức `Update()`:

```csharp
// Thay Radial bằng fill trái→phải:
// Đổi Image Type = Filled, Fill Method = Horizontal
cooldownOverlay.fillAmount = 1f - boundSkill.GetCooldownPercent();
```

### Thêm hiệu ứng pulse khi skill sẵn sàng

Thêm vào `Update()` trong `SkillSlotUI.cs`:

```csharp
// Nháy sáng khi vừa hết cooldown (tùy chọn)
if (!onCooldown && iconImage != null)
{
    float pulse = 0.9f + 0.1f * Mathf.Sin(Time.time * 4f);
    iconImage.transform.localScale = Vector3.one * pulse;
}
else if (iconImage != null)
{
    iconImage.transform.localScale = Vector3.one;
}
```

### Hiển thị tên skill dưới slot

Thêm một TMP_Text con `SkillNameText` vào Prefab SkillSlot, rồi trong `SkillSlotUI.Bind()`:

```csharp
if (skillNameText != null)
    skillNameText.text = skill.skillName;
```

### MP Bar không dùng Network (single-player)

Nếu không có `NetworkPlayerDataSync`, sửa `MpBar.Start()` để đọc từ `PlayerController` hoặc `PlayerHealth` trực tiếp.

---

## 6. Câu hỏi thường gặp

**Q: Hotbar không hiện sau khi chạy game?**  
A: Kiểm tra `SkillHotbarUI.autoFind = true` và đảm bảo `PlayerSkillManager` đã được spawn. Script retry mỗi 0.3s — chờ vài giây rồi thử lại.

**Q: Nhấn nút không dùng skill?**  
A: Đảm bảo player là **IsOwner**. Trong single-player / host mode luôn là owner. Kiểm tra Console xem có lỗi không.

**Q: Countdown text không hiện?**  
A: Kiểm tra `CooldownText` đã được gán đúng reference trong `SkillSlotUI`. Đảm bảo TextMeshPro đã cài (Package Manager).

**Q: MP Bar luôn rỗng?**  
A: Đảm bảo server đã trả `mp` và `max_mp` trong player data, và `NetworkPlayerDataSync` đã set `networkMp` / `networkMaxMp`. Kiểm tra `PlayerInfoUI` — nếu text MP hiển thị đúng thì `MpBar` sẽ hoạt động.

**Q: Muốn 4 slot nhưng chỉ có 3 skill?**  
A: Slot dư sẽ tự bị `Unbind()` (ẩn/mờ). Bạn có thể custom `Unbind()` trong `SkillSlotUI` để hiển thị icon khoá thay thế.

**Q: Icon skill lấy từ server (`icon_id`) như thế nào?**  
A: `icon_id` trong `PlayerSkillInfo` là string ID. Bạn tạo một `ScriptableObject` hoặc `Dictionary<string, Sprite>` mapping `icon_id` → Sprite, rồi truyền vào `SkillHotbarUI.skillIcons`. Ví dụ:

```csharp
// SkillIconRegistry.cs (ScriptableObject)
[CreateAssetMenu]
public class SkillIconRegistry : ScriptableObject
{
    [System.Serializable]
    public struct Entry { public string iconId; public Sprite sprite; }
    public List<Entry> entries;
    public Sprite Get(string id) => entries.Find(e => e.iconId == id).sprite;
}
```

Sau đó trong `SkillHotbarUI`, load skill từ API, map `icon_id` → `Sprite` và gán vào `skillIcons`.
