# Hướng Dẫn Config Tab Thông Số Nhân Vật (StatsTabUI)

> Script: `Assets/Scripts/UI/Character/StatsTabUI.cs`  
> Controller: `Assets/Scripts/UI/Character/CharacterPanelController.cs`

---

## Tổng Quan

Tab **"Thông Số"** hiển thị:

| Thông tin | Nguồn dữ liệu |
|-----------|---------------|
| Tên nhân vật | `GameManager → PlayerData.character_name` |
| Level | `GameManager → PlayerData.level` |
| Hệ nguyên tố + Gene Tier | `PlayerData.element_type` + `gene_tier` |
| Thanh HP (live, realtime) | `NetworkPlayerHealth.OnHealthChanged` (multiplayer) hoặc `PlayerHealth.OnHealthChanged` (offline) |
| Thanh MP (static) | `PlayerData.final_stats.max_mp` |
| ATK, Tốc độ | `PlayerData.final_stats` |
| Vàng | `PlayerData.gold` |

---

## Bước 1 – Tạo GameObject ContentStats

Trong Hierarchy, bên trong `CharacterPanel`:

```
CharacterPanel
├─ TabBar
│   ├─ BtnStats       ← Button "Thông Số"  (MỚI)
│   ├─ BtnEquipment   ← Button "Trang Bị"
│   ├─ BtnSkill       ← Button "Kỹ Năng"
│   └─ BtnPotential   ← Button "Tiềm Năng"
├─ ContentStats        ← GameObject MỚI (gắn StatsTabUI)
├─ ContentEquipment
├─ ContentSkill
└─ ContentPotential
```

**Tạo nhanh:**
1. Click phải `CharacterPanel` → **UI → Panel** → đặt tên `ContentStats`
2. Gắn script `StatsTabUI` lên `ContentStats`

---

## Bước 2 – Tạo UI bên trong ContentStats

Tạo cấu trúc con theo gợi ý (dùng VerticalLayoutGroup cho gọn):

```
ContentStats  [StatsTabUI]
├─ TxtCharacterName   [TMP_Text]   vd: "Nguyễn Văn A"
├─ TxtLevel           [TMP_Text]   vd: "Lv. 25"
├─ TxtElement         [TMP_Text]   vd: "Hệ Fire  ★★☆☆☆  (Gene Tier 2)"
├─ ── HP ──
│   ├─ HpBar          [Slider]     *interactable = OFF*
│   └─ TxtHp          [TMP_Text]   vd: "2500 / 3000"
├─ ── MP ──
│   ├─ MpBar          [Slider]     *interactable = OFF*
│   └─ TxtMp          [TMP_Text]   vd: "800 / 1000"
├─ TxtAttack          [TMP_Text]   vd: "ATK: 350"
├─ TxtMoveSpeed       [TMP_Text]   vd: "Tốc: 5.5"
├─ TxtGold            [TMP_Text]   vd: "Vàng: 12,500"
└─ TxtStatus          [TMP_Text]   (ẩn khi không có lỗi)
```

### Cách tạo từng thành phần nhanh:

| Thành phần | Menu Unity |
|------------|-----------|
| TMP_Text | Right-click → **UI → Text - TextMeshPro** |
| Slider | Right-click → **UI → Slider** |

---

## Bước 3 – Kéo Vào Inspector của StatsTabUI

Chọn GameObject `ContentStats`, mở Inspector, **kéo từng thứ vào đúng slot**:

| Slot Inspector | Kéo vào |
|----------------|---------|
| **Txt Character Name** | TMP_Text tên nhân vật |
| **Txt Level** | TMP_Text level |
| **Txt Element** | TMP_Text hệ + tier |
| **Hp Bar** | Slider HP |
| **Txt Hp** | TMP_Text "2500 / 3000" |
| **Mp Bar** | Slider MP |
| **Txt Mp** | TMP_Text "800 / 1000" |
| **Txt Attack** | TMP_Text ATK |
| **Txt Move Speed** | TMP_Text Tốc |
| **Txt Gold** | TMP_Text Vàng |
| **Txt Status** | TMP_Text trạng thái (loader/lỗi) |

> **Lưu ý Slider:** Bỏ tick `Interactable` trên mỗi Slider (hoặc script tự tắt khi Awake).

---

## Bước 4 – Cập Nhật CharacterPanelController

Chọn `CharacterPanel` → Inspector → `CharacterPanelController`:

| Slot Inspector | Kéo vào |
|----------------|---------|
| **Btn Stats** | Button "Thông Số" ← **MỚI** |
| **Btn Equipment** | Button "Trang Bị" |
| **Btn Skill** | Button "Kỹ Năng" |
| **Btn Potential** | Button "Tiềm Năng" |
| **Content Stats** | GameObject `ContentStats` ← **MỚI** |
| **Content Equipment** | GameObject `ContentEquipment` |
| **Content Skill** | SkillTabUI |
| **Content Potential** | PotentialTabUI |

> **Tab mặc định** mở là tab 0 = Thông Số.  
> Để đổi, thay `activeTab = 0` → `activeTab = 1` (Equipment) trong `CharacterPanelController.cs`.

---

## Bước 5 – Config Slider HP/MP (màu sắc)

Để Slider có màu đẹp:

1. Chọn Slider `HpBar` → mở rộng hierarchy
2. Tìm child `Fill Area → Fill` → `Image` component → chỉnh `Color` thành **xanh lá** (`#00C853`)
3. Làm tương tự `MpBar` → `Fill` → **xanh dương** (`#2979FF`)

### Tùy chọn thêm: Đổi màu khi HP thấp

Thêm component `HealthBarColorSync.cs` (tự tạo) hoặc dùng `HealthBar.cs` có sẵn trong `Scripts/UI/HUD/`.

---

## Bước 6 – Đảm Bảo PlayerHealth Tồn Tại Trong Scene

`StatsTabUI` tự động tìm `NetworkPlayerHealth` (multiplayer) hoặc `PlayerHealth` (offline):

- **Multiplayer:** `NetworkPlayerHealth` phải được gắn trên Player Prefab và đăng ký event `OnHealthChanged`.
- **Offline/Test:** `PlayerHealth` phải tồn tại trong scene.

Nếu không tìm thấy → Thanh HP hiển thị **max/max** (lấy từ server data) — không crash.

---

## Bước 7 – Trigger Refresh Thủ Công (tùy chọn)

Sau khi player nhận buff / equip đồ → gọi để cập nhật UI:

```csharp
// Ở bất kỳ script nào
FindObjectOfType<StatsTabUI>()?.Load();

// Hoặc nếu CharacterPanel đang mở tab Stats, nó tự Load khi OnEnable
```

---

## Checklist Hoàn Thành

- [ ] Script `StatsTabUI` gắn lên `ContentStats`
- [ ] Tất cả TMP_Text / Slider đã kéo vào đúng slot
- [ ] `CharacterPanelController` có slot `BtnStats` + `ContentStats`
- [ ] `BtnStats` đã được tạo trong TabBar
- [ ] Slider `interactable = OFF` (hoặc để script tự xử lý)
- [ ] Test Play → mở CharacterPanel → click tab "Thông Số" → HP live cập nhật khi nhận damage

---

## Lưu Ý

- **MP chưa có live system** → luôn hiển thị `max/max`. Khi implement `NetworkPlayerMana`, thêm subscribe tương tự HP vào `StatsTabUI`.
- **Tên nhân vật** lấy từ `PlayerData.character_name` — đảm bảo server trả về field này trong `/api/player/{id}/data`.
- `FinalStats` (ATK, Move Speed) đã bao gồm bonus trang bị + tiềm năng → **ưu tiên dùng `final_stats`** thay vì `base_stats`.
