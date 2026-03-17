# Hướng Dẫn Cấu Hình Hệ Thổ (Earth) Trong Unity

## Tổng Quan

Hệ Thổ có 3 skill:
| Phím | Tên | Loại | SkillType |
|------|-----|------|-----------|
| J (106) | Địa Uy Khí | Buff tấn công vùng | `EarthAura (8)` |
| K (107) | Địa Phong Đao | Boomerang | `EarthBoomerang (9)` |
| L (108) | Địa Độn Thuật | Dịch chuyển + DoT | `EarthBlinkStrike (10)` |

---

## 1. Cấu Trúc Prefab `Tho.prefab`

```
Tho (root)
├── [PlayerSkillManager]       — skills: 3 earth skills đã set
├── [EarthAttackBuffSkill]     — cooldown=10, buffRadius=5, buffDuration=6, attackBonus=30%
├── [EarthBoomerangSkill]      — cooldown=5, launchSpeed=14
├── [EarthBlinkStrikeSkill]    — cooldown=7, blinkDistance=4, projectileSpeed=10
├── [TeleportSkill]            — T key
├── ... (các component khác)
└── SkillEffect                — Animator → Skill_Tho.overrideController ✅
```

**defaultSkillEffectObject** đã được set sang `SkillEffect` child.

---

## 2. Gán Prefabs

### Skill 2 — Địa Phong Đao (Boomerang)
Tìm component **EarthBoomerangSkill** → gán `boomerangPrefab`:
- Prefab cần: `Rigidbody2D`, `Collider2D` (trigger), `FireballDamage`, `EarthBoomerangProjectile`
- `EarthBoomerangProjectile` sẽ tự xử lý chuyển động quay về

### Skill 3 — Địa Độn Thuật (Blink + DoT)
Tìm component **EarthBlinkStrikeSkill** → gán `dotProjectilePrefab`:
- Prefab cần: `Rigidbody2D`, `Collider2D` (trigger), `DotDamage`
- `DotDamage` gây 5 tick × interval, cả enemy lẫn player đều bị ảnh hưởng

---

## 3. Animation Setup

**Skill_Tho.overrideController** đã được cấu hình:
- Identity mapping — dùng trực tiếp Tho clips (`skill 3_1/2/3.anim`)
- SkillEffect Animator đã trỏ đến `Skill_Tho.overrideController` ✅

**Lưu ý**: Các file `.anim` trong `Assets/Animations/Skills/Tho/` đã được sửa sprites (thay thế missing sprites bằng Art/player sprites 3386-3403).

---

## 4. Script: `EarthAttackBuffSkill` (Skill 1 — Địa Uy Khí)

- Quét bán kính `buffRadius` (default: 5 units)
- Gọi `PlayerHealth.ApplyAttackBuff(30, 6f)` trên mỗi player trong bán kính
- Buff stacks theo giá trị cao nhất (không cộng dồn)
- **FireballDamage** tự động nhận bonus khi owner bắn đạn: `SetAttackBonus(percent)`

Cần gán thêm trong code nếu muốn attack bonus áp dụng tự động cho projectile:
```csharp
// Trong FireballDamage.Start() hoặc khi spawn:
var ownerHealth = owner.GetComponent<PlayerHealth>();
if (ownerHealth != null)
    SetAttackBonus(ownerHealth.GetAttackBonusPercent());
```

---

## 5. Script: `DotDamage` (Component trên DoT projectile)

```
DotDamage settings (default):
  dotDamagePerTick: 3    // ST mỗi tick
  dotTicks: 5            // Số tick
  tickInterval: 0.8      // Giây giữa tick
  destroyOnHit: true     // Hủy sau khi chạm
```

Cả `Enemy` lẫn `Player` (với tag tương ứng) đều nhận DoT.

---

## 6. Database

**Chạy migration:**
```sql
-- File: GameServerApi/migration_tho_skills.sql
```

| skill_id | skill_code | Tên | Cooldown base |
|----------|-----------|-----|---------------|
| 18 | EARTH_AURA | Địa Uy Khí | 10s |
| 19 | EARTH_BOOMERANG | Địa Phong Đao | 5s |
| 20 | EARTH_BLINK | Địa Độn Thuật | 7s |

---

## 7. Kiểm Tra Trong Unity

1. **Play mode** → chọn nhân vật Thổ
2. Nhấn **J** → animation Skill1, tất cả player trong 5 units nhận +30% ATK (6 giây)
3. Nhấn **K** → dao đất bay ra, sau 0.6s quay về player
4. Nhấn **L** → player dịch chuyển 4 units, đạn DoT bay ra tại vị trí cũ

---

## 8. Lưu Ý Kỹ Thuật

- **EarthBoomerangProjectile.Initialize(ownerTransform, velocity)** phải được gọi sau khi Instantiate
- **EarthBlinkStrikeSkill** dùng **ClientRpc** để đồng bộ vị trí player trên tất cả client
- DoT projectile cần có **NetworkObject** để sync qua network; nếu không, chỉ hoạt động server-side
- Các prefab boomerang và DoT cần được tạo trong Unity và gán vào Inspector
