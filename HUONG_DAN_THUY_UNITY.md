# HƯỚNG DẪN SETUP HỆ THỦY TRONG UNITY

## Tổng quan 3 Skill Hệ Thủy

| Skill | Tên | Phím | Mô tả |
|-------|-----|------|-------|
| Skill 1 | Thủy Đạn | J | Bắn đạn nước bay ngang, gây sát thương |
| Skill 2 | Thánh Mộc Hạ | K | Cây thánh từ trên trời rơi xuống |
| Skill 3 | Thủy Giáp Hộ Thể | L | Buff giáp cho bản thân và đồng đội xung quanh |

---

## BƯỚC 1 — Mở Thuy.prefab

1. Trong **Project** panel: `Assets/Prefabs/Player/He/Thuy.prefab`
2. Double-click để mở trong **Prefab Mode**

---

## BƯỚC 2 — Gán SkillEffect cho các component

Chọn object `Thuy` (root), nhìn vào **Inspector**:

### PlayerSkillManager
- `Default Skill Effect Object` đã tự động trỏ vào `SkillEffect` child — kiểm tra xem đúng chưa (nên thấy GameObject tên "SkillEffect")

### WaterPillarSkill (Skill 2)
- `Pillar Prefab` → **Để trống trước** (xem Bước 4 để tạo prefab), hoặc kéo prefab fallen pillar vào đây

### WaterArmorBuffSkill (Skill 3)
- `Buff Radius`: 4 (bán kính phát hiện đồng đội, đơn vị units)
- `Buff Duration`: 5 (giây buff giáp)
- `Armor Value`: 20 (điểm giáp hấp thụ sát thương)
- Không cần gán gì thêm — tự detect PlayerHealth trong bán kính

---

## BƯỚC 3 — Cấu hình Skill_Thuy.overrideController

File `Skill_Thuy.overrideController` đã được cấu hình tự động trong code, tuy nhiên cần **kiểm tra trong Unity Editor**:

1. Project panel: `Assets/Animations/Skills/Skill_Thuy.overrideController`
2. Click chọn, xem **Inspector**:
   - `Controller`: phải trỏ đến `Skill_Phong` (base controller)
   - Clip overrides:
     - `skill 1` → `skill 4_1` (từ folder Thuy)
     - `skill 2` → `skill 4_2`
     - `skill 3` → `skill 4_3`
3. Nếu chưa đúng: kéo thủ công từ `Assets/Animations/Skills/Thuy/`

### Kiểm tra SkillEffect trên Thuy.prefab
- Chọn child object `SkillEffect` trong Thuy.prefab
- Animator component → field `Controller` phải là `Skill_Thuy` (overrideController)
- Nếu đang là controller khác → đổi thành `Skill_Thuy.overrideController`

---

## BƯỚC 4 — Tạo Pillar Prefab (Skill 2)

Prefab cho cây thánh rơi xuống:

1. **Tạo prefab mới**: `Assets/Prefabs/Player/WaterPillarProjectile.prefab`
2. Cấu trúc GameObject:
   ```
   WaterPillarProjectile
   ├── NetworkObject         ← bắt buộc
   ├── Rigidbody2D           ← tự add bởi WaterPillarSkill, nhưng có sẵn tốt hơn
   │     gravityScale = 0
   │     bodyType = Dynamic
   ├── CapsuleCollider2D     ← trigger = ✓, kích thước 0.4 × 1.5
   ├── SpriteRenderer        ← gán sprite pillar/cây  
   ├── FireballDamage        ← damage = 40, destroyOnHit = ✓, destroyOnGround = ✓
   └── NetworkTransform      ← để sync vị trí rơi cho tất cả client
   ```
3. Save prefab
4. Quay lại Thuy.prefab → chọn `Thuy` root → `WaterPillarSkill` → kéo prefab vừa tạo vào `Pillar Prefab`

> **Lưu ý quan trọng**: Không dùng `WaterPrefab.prefab` (đây là prefab nhân vật). Hãy dùng prefab projectile riêng như `WaterPillarProjectile.prefab`.

---

## BƯỚC 5 — Cấu hình Skill 1 (Thủy Đạn — projectile ngang)

1. Tạo prefab projectile riêng cho Skill 1, ví dụ: `Assets/Prefabs/Projectile/WaterBoltProjectile.prefab`
2. Prefab `WaterBoltProjectile` cần có:
   - `NetworkObject`
   - `NetworkTransform`
   - `Rigidbody2D` (`gravityScale = 0`, `bodyType = Dynamic`)
   - `CapsuleCollider2D` (`isTrigger = true`)
   - `SpriteRenderer`
   - `FireballDamage` (`destroyOnHit = true`, `destroyOnGround = true`)
3. Chọn `Thuy` root → `PlayerSkillManager` → xem danh sách Skills
4. Skill 1 (`Thủy Đạn`):
   - `Projectile Prefab` → kéo `WaterBoltProjectile.prefab` vào
   - `Projectile Speed`: 12
   - `Spawn Offset`: 0.5
   - `Projectile Lifetime`: 3

### Cấu hình animation cho projectile Skill 1 / Skill 2

- Animation gần player dùng `SkillEffect` + `Skill_Thuy.overrideController` (trigger `Skill1/Skill2/Skill3`).
- Nếu muốn chính projectile cũng chạy animation:
  1. Thêm `Animator` vào prefab projectile (`WaterBoltProjectile`, `WaterPillarProjectile`).
  2. Gán Animator Controller có trigger tương ứng (`Skill1` cho đạn skill 1, `Skill2` cho pillar skill 2) hoặc để clip chạy ở state mặc định.
  3. Nếu dùng trigger thì phải tạo đúng tên trigger trong Controller (đúng chữ hoa/thường).
- Nếu projectile không có `Animator`, nó chỉ hiển thị sprite tĩnh khi bay/rơi.

### Nếu Skill 3 (L) không chạy animation và không có cooldown

Kiểm tra trong `Thuy.prefab` trên object `Thuy` root bắt buộc phải có component `WaterArmorBuffSkill`:
- `cooldown`: 12
- `buffRadius`: 4
- `buffDuration`: 5
- `armorValue`: 20
- `animTriggerName`: `Skill3`

Thiếu component này thì bấm L sẽ không kích hoạt skill (nên cũng không bắt đầu cooldown).

---

## BƯỚC 6 — Tags cần đảm bảo tồn tại

- `Enemy` — tag trên enemy để FireballDamage xác định va chạm
- Player layer phải là **Layer 8** (đã config sẵn trong Thuy.prefab)

Kiểm tra: **Edit → Project Settings → Tags and Layers**
- Layer 8: `Player`
- Tag: `Enemy`

---

## BƯỚC 7 — Kiểm tra AnimatorController của SkillEffect hệ Thủy

Animations folder `Assets/Animations/Skills/Thuy/` có 3 clips:
- `skill 4_1.anim` — animation Skill 1 (xuất hiện khi nhấn J)
- `skill 4_2.anim` — animation Skill 2 (nhân vật dang tay khi gọi pillar)
- `skill 4_3.anim` — animation Skill 3 (nhân vật channeling buff)

Nếu clips bị **Missing Sprites**:
1. Project → `Assets/Art/player/` → chọn PNG texture
2. Inspector: Texture Type = `Sprite`, Sprite Mode = `Multiple` → **Sprite Editor** → Slice → Apply
3. Animation window → chọn clip → click từng keyframe → gán sprite từ PNG đã slice

---

## BƯỚC 8 — Test trong Play Mode

### Kiểm tra Skill 1 (J):
- Nhấn J → phải thấy animation Skill1 chạy + đạn nước bay ngang
- Damage enemy khi trúng

### Kiểm tra Skill 2 (K):
- Nhấn K → animation Skill2 chạy + pillar xuất hiện từ Y+5 units và rơi xuống
- Cooldown 6 giây

### Kiểm tra Skill 3 (L):
- Nhấn L → animation Skill3 chạy + player (và đồng đội trong bán kính 4) chuyển màu xanh nước (cyan)
- Khi bị đánh trong lúc có buff: hấp thụ 20 điểm sát thương trước
- Sau 5 giây: màu trở lại trắng bình thường
- Cooldown 12 giây

---

## Tóm tắt File Đã Thay Đổi / Tạo Mới

| File | Thay đổi |
|------|----------|
| `Scripts/Player/Skills/SkillData.cs` | Thêm `WaterPillar = 5`, `WaterArmorBuff = 6` vào SkillType enum |
| `Scripts/Player/Combat/PlayerHealth.cs` | Thêm `temporaryArmor` + `ApplyArmorBuff()` |
| `Scripts/Player/Skills/WaterPillarSkill.cs` | **MỚI** — Skill 2: cây thánh rơi xuống |
| `Scripts/Player/Skills/WaterArmorBuffSkill.cs` | **MỚI** — Skill 3: buff giáp cho đồng đội |
| `Scripts/Player/Combat/PlayerSkillManager.cs` | Auto-detect + dispatch cho 2 skill mới |
| `Prefabs/Player/He/Thuy.prefab` | Cấu hình 3 skill + thêm 2 component mới |
| `Animations/Skills/Skill_Thuy.overrideController` | Base controller + 3 clip overrides |
| `gamedb.sql` | Thêm skill_id 12, 13, 14 (WATER_BOLT, WATER_PILLAR, WATER_ARMOR) |
| `GameServerApi/migration_thuy_skills.sql` | **MỚI** — Migration DB an toàn |
