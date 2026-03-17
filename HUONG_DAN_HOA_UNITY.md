# Hướng Dẫn Cấu Hình Hệ Hỏa (Fire) Trong Unity

## Tổng Quan

Hệ Hỏa có 3 skill:
| Phím | Tên | Loại | SkillType |
|------|-----|------|-----------|
| J (106) | Hỏa Đạn | Bắn đạn thẳng | `Projectile (0)` |
| K (107) | Hỏa Cầu | Bắn cầu lửa lớn | `Projectile (0)` |
| L (108) | Thiên Hỏa | Mưa lửa từ trên trời | `FireRain (7)` |

---

## 1. Cấu Trúc Prefab `Hoa.prefab`

```
Hoa (root)
├── [PlayerSkillManager]   — skills: 3 skills đã set
├── [FireRainSkill]        — cooldown=8, fireballCount=5, spreadRadius=3, fallSpeed=16
├── [TeleportSkill]        — T key
├── ... (các component khác)
└── SkillEffect            — Animator → Skill_Hoa.overrideController
```

**defaultSkillEffectObject** đã được set sang `SkillEffect` child.

---

## 2. Gán Projectile Prefabs

Script **FireRainSkill** cần:
- `firePrefab`: prefab có `NetworkObject`, `Rigidbody2D`, `Collider2D` (trigger), `FireballDamage`

Với Skill 1 và Skill 2 (Projectile type), dùng `projectilePrefab` trong SkillData:
- `Hỏa Đạn`: prefab đạn lửa nhỏ, speed=12, lifetime=3s
- `Hỏa Cầu`: prefab cầu lửa lớn, speed=10, lifetime=2.5s

Cách gán trong Unity:
1. Mở `Hoa.prefab` trong Inspector
2. Tìm component **PlayerSkillManager** → mỗi skill có field `projectilePrefab`
3. Kéo thả prefab đạn lửa vào tương ứng
4. Tìm component **FireRainSkill** → gán `firePrefab`

---

## 3. Animation Setup

**Skill_Hoa.overrideController** đã được cấu hình sẵn:
- Slot Skill1 → `skill 1_1.anim` (Hoa folder)
- Slot Skill2 → `skill 1_2.anim` (Hoa folder)
- Slot Skill3 → `skill 1_3.anim` (Hoa folder)

SkillEffect Animator đã trỏ đến `Skill_Hoa.overrideController`.

Để trigger animation khi dùng skill, `PlayerSkillManager` tìm trigger `Skill1`, `Skill2`, `Skill3` trong Animator.

---

## 4. Database

**Chạy migration:**
```sql
-- File: GameServerApi/migration_hoa_skills.sql
```

| skill_id | skill_code | Tên | Cooldown base |
|----------|-----------|-----|---------------|
| 15 | FIRE_BOLT | Hỏa Đạn | 3s |
| 16 | FIRE_BURST | Hỏa Cầu | 5s |
| 17 | FIRE_RAIN | Thiên Hỏa | 8s |

---

## 5. Kiểm Tra Trong Unity

1. **Play mode** → chọn nhân vật Hoa
2. Nhấn **J** → đạn lửa nhỏ bắn ra
3. Nhấn **K** → cầu lửa lớn bắn ra
4. Nhấn **L** → mưa lửa từ trên trời rơi xuống vùng trước mặt
5. Kiểm tra animation SkillEffect khi mỗi skill được dùng

---

## 6. Lưu Ý

- **FireRainSkill.firePrefab**: bắt buộc phải gán trong Inspector mới hoạt động
- Mưa lửa rơi ngẫu nhiên trong `spreadRadius=3` đơn vị trước mặt player
- `fireballCount=5` cầu lửa được spawn với interval `0.12s` giữa mỗi cầu
- Attack bonus từ **Địa Uy Khí** (hệ Thổ) sẽ tăng damage của Hỏa Đạn và Hỏa Cầu qua `FireballDamage.SetAttackBonus()`
