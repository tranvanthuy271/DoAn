# Hướng Dẫn Config Viền + BG Animation Theo Level Trang Bị

## Tổng quan hệ thống

Mỗi ô slot trang bị trong tab Trang Bị (`EquipmentPanel`) hiển thị **viền** (`Vien`) và **background** (`BG`) với animation khác nhau tùy theo `upgradeLevel` của item đang trang bị.

```
EquipmentSlotUI (150×150)
 ├─ BG        (Image) ← background, ngầm dưới cùng
 ├─ Vien      (Image) ← viền/frame, hiện trên BG
 ├─ iconImage (Image) ← icon item
 └─ upgradeButton (Button)
```

Animation dùng **Animator + RuntimeAnimatorController** (sprite-swap animation), không phải UIImageTierAnimation.

---

## 1. Cấu trúc file animation

```
Assets/Animations/UI/
  Vien.controller          ← base controller (tier 4)
  Vien.anim                ← clip tier 4
  Vien 1.overrideController ← tier 8
  Vien 1.anim
  Vien 2.overrideController ← tier 12
  Vien 2.anim
  Vien 3.overrideController ← tier 14
  Vien 3.anim
  Vien 4.overrideController ← tier 16
  Vien 4.anim
  Vien 5.anim              ← (reserved)

  BG.controller            ← base controller (tier 4)
  BG.anim                  ← clip tier 4
  BG 1.overrideController  ← tier 8
  BG 1.anim
  BG1 2.overrideController ← tier 12
  BG 2.anim
  BG1 3.overrideController ← tier 14
  BG 3.anim
  BG1 4.overrideController ← tier 16
  BG 4.anim
```

> **Lưu ý quan trọng:** Các animation clip (`.anim`) dùng **sprite-swap**: chúng animate thuộc tính `m_Sprite` trên component `Image` (classID 212) ở path rỗng `""` — tức là target chính là GameObject đang gắn Animator, không phải child nào. Animator **phải được gắn trên cùng GameObject với Image** (BG hoặc Vien).

---

## 2. EquipmentTierConfig ScriptableObject

File: `Assets/Resources/ScriptableObjects/EquipmentTierConfig.asset`

Tạo hoặc chỉnh sửa: **Right-click > Create > Equipment > Tier Config**

| Tier | minLevel | borderAnimator | bgAnimator |
|------|----------|----------------|------------|
| 4    | 4        | Vien.controller | BG.controller |
| 8    | 8        | Vien 1.overrideController | BG 1.overrideController |
| 12   | 12       | Vien 2.overrideController | BG1 2.overrideController |
| 14   | 14       | Vien 3.overrideController | BG1 3.overrideController |
| 16   | 16       | Vien 4.overrideController | BG1 4.overrideController |

**Các field trong mỗi TierEntry:**
- `minLevel` — upgradeLevel tối thiểu để kích hoạt tier này
- `borderSprite` — sprite đầu tiên của animation viền (để pre-fill tránh 1-frame trống)
- `bgSprite` — sprite đầu tiên của animation BG
- `borderAnimator` — **RuntimeAnimatorController** cho viền (Vien series)
- `bgAnimator` — **RuntimeAnimatorController** cho BG (BG series)
- `borderColor` / `bgColor` — màu tint, để `{1,1,1,1}` (white) cho hiện đúng màu sprite
- `defaultTier` — tier dùng khi `upgradeLevel < 4` (để trống = không hiện viền/BG)

> **KHÔNG** để color là `{0,0,0,0}` — code chuyển về white nhưng về chuẩn nên set `{1,1,1,1}`.

---

## 3. Hierarchy bắt buộc trong từng EquipmentSlotUI prefab/scene object

```
SlotRoot (có EquipmentSlotUI script)
 ├─ BG    ← Image — kéo vào Inspector field "bgImage"   (background)
 ├─ Vien  ← Image — kéo vào Inspector field "borderImage" (viền/frame)
 ├─ Icon  ← Image — kéo vào "iconImage"
 └─ UpgradeBtn ← Button — kéo vào "upgradeButton"
```

> **Quy tắc thứ tự quan trọng:**
> - `BG` phải là **child đầu tiên** (index 0) để render ở dưới cùng
> - `Vien` phải là **child thứ 2** (index 1) để render trên BG
> - `Icon` phải ở **sau Vien** để icon nổi lên trên cả hai

**Mapping Inspector:**
| Inspector Field | Gán vào | Lý do |
|----------------|---------|-------|
| `borderImage`  | Image trên `Vien` object | Viền = frame ngoài, render trên BG |
| `bgImage`      | Image trên `BG` object   | Background = nền dưới |

> ⚠️ **Lỗi phổ biến:** Gán ngược `borderImage` ↔ `bgImage` khiến viền bị BG che khuất hoàn toàn → không thấy animation viền.

---

## 4. Kiểm tra từng slot trong GameScene

Hiện tại **6 slots** trong `ContentEquipment` (EquipmentPanelUI.manualSlots):

| Index | SlotType | borderImage gán vào | bgImage gán vào | Status |
|-------|----------|--------------------|-----------------|----|
| 0 | Weapon (0) | Vien ✓ | BG ✓ | OK |
| 1 | Helmet (1) | Vien ✓ (đã sửa) | BG ✓ (đã sửa) | Fixed |
| 2 | Armor (2)  | Vien ✓ (đã sửa) | BG ✓ (đã sửa) | Fixed |
| 3 | Pants (3)  | Vien ✓ | BG ✓ | OK |
| 4 | Boots (4)  | Vien ✓ | BG ✓ | OK |
| 5 | Accessory (5) | Vien ✓ | BG ✓ | OK |

> Slot Helmet và Armor trước đó bị gán ngược (`borderImage` trỏ vào "BG" object, `bgImage` trỏ vào "Vien" object) → đã sửa trong scene.

---

## 5. Luồng code ApplyTierEffect (sau khi sửa)

```csharp
// Khi player equip 1 item:
// item.upgradeLevel = 8 → tier = tier[1] (minLevel=8)

void ApplyTierEffect(int level)
{
    // 1. Lấy tier phù hợp từ TierConfig
    var tier = tierConfig.GetTier(level);
    // level=8 → tier.borderAnimator = "Vien 1.overrideController"
    // level=8 → tier.bgAnimator     = "BG 1.overrideController"

    // 2. Viền (Vien Image):
    borderImage.enabled = true;
    borderImage.sprite  = tier.borderSprite;   // pre-fill frame 0
    borderImage.color   = Color.white;

    // 3. Gắn Animator + play controller:
    // → AddComponent<Animator> vào Vien GameObject (nếu chưa có)
    // → animator.runtimeAnimatorController = tier.borderAnimator
    // → animator.Play(0, -1, 0f)            // bắt đầu từ đầu
    // → Animation clip "Vien 1" chạy, swap sprite theo 60fps

    // 4. Tương tự cho BG Image với tier.bgAnimator
}
```

---

## 6. Lý do animation trước đây không chạy

| Vấn đề | Trước khi sửa | Sau khi sửa |
|--------|--------------|-------------|
| EquipmentSlotUI dùng UIImageTierAnimation | Tạo PulseGlow code (không phải sprite-swap) | Dùng Animator + RuntimeAnimatorController từ TierConfig |
| Helmet slot — borderImage/bgImage ngược | borderImage → "BG" (dưới cùng, bị BG che) | borderImage → "Vien" (render trên BG) |
| Armor slot — borderImage/bgImage ngược | borderImage → "BG" (bị che) | borderImage → "Vien" |
| Màu tiers 8–16 là `{0,0,0,0}` | Trong suốt (dù code có fallback) | Sửa thành `{1,1,1,1}` |

---

## 7. Điều kiện để viền + BG hiện

```
upgradeLevel >= 4  → Tier 4  → Vien.controller    + BG.controller
upgradeLevel >= 8  → Tier 8  → Vien 1.override... + BG 1.override...
upgradeLevel >= 12 → Tier 12 → ...
upgradeLevel >= 14 → Tier 14 → ...
upgradeLevel >= 16 → Tier 16 → ...
upgradeLevel < 4   → defaultTier → KHÔNG hiện (sprites null)
```

> Nếu muốn hiện viền từ level 1, thay đổi `minLevel` của tier đầu tiên trong `EquipmentTierConfig` từ `4` xuống `1`.

---

## 8. Checklist test sau khi sửa

1. ☐ Mở Unity Editor, mở GameScene
2. ☐ Equip 1 item với `upgradeLevel = 4` → viền Vien.anim chạy trên slot đó
3. ☐ Equip item với `upgradeLevel = 8` → Vien 1 + BG 1 animation chạy
4. ☐ Slot Helmet và Armor hiện đúng viền Vien (không bị BG che)
5. ☐ Slot Weapon, Pants, Boots, Accessory vẫn đúng
6. ☐ Unequip item → viền + BG ẩn
7. ☐ Re-equip cùng item → animation reset về frame 0 và chạy lại

---

## 9. Nếu muốn thêm tier mới (ví dụ tier 18)

1. Tạo `Vien 5.overrideController` (nếu chưa có) và override clip `Vien`  
2. Tạo `BG1 5.overrideController` và override clip `BG`  
3. Mở `EquipmentTierConfig` → thêm TierEntry mới:  
   - `minLevel = 18`  
   - `borderSprite` = frame đầu của clip Vien 5  
   - `bgSprite` = frame đầu của clip BG 5  
   - `borderAnimator` = Vien 5.overrideController  
   - `bgAnimator` = BG1 5.overrideController  
   - `borderColor` = (1,1,1,1)  
   - `bgColor` = (1,1,1,1)  
4. Save asset → Unity tự pick up thay đổi

---

## 10. Debug nhanh trong Play Mode

Right-click component `EquipmentSlotUI` trong Inspector → **"Debug Tier State"**

Output sẽ in ra Console:
```
---- [TierDebug] WeaponSlot (Weapon) ----
  tierConfig    : EquipmentTierConfig
  borderImage   : Vien
  bgImage       : BG
  currentItem   : Kiếm Hỏa lv=8
  _currentTierLevel: 8
  Tier sẽ dùng  : minLevel=8, border=3504, bg=3560, 
                  borderAnim=Vien 1, bgAnim=BG 1
------------------------------------------
```
