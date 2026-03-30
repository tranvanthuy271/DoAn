# Hướng Dẫn Config Skill Enemy: Đánh Gần & Projectile

---

## Tổng Quan Kiến Trúc

| Class | Vai trò |
|---|---|
| `EnemySkillSet` | Lưu danh sách skill của một enemy instance; server-only |
| `SkillEntry` | DTO một skill (JSON field trong DB column `skills_json`) |
| `EnemySkillsEntry` | DTO toàn bộ skill config của một loại enemy |
| `EnemyAI` | Đọc `EnemySkillSet`, quyết định cast skill khi trong tầm |
| `BossAI` | Đọc config từ API, có phase system, cast projectile/AoE |
| `EnemyProjectile` | Component gắn lên prefab đạn của enemy/boss |

---

## Phần 1: Skill Đánh Gần (Melee) cho Enemy Thường

### 1.1 Cơ Chế Hoạt Động

`EnemyAI` gọi `EnemySkillSet.TryGetReadySkill()` mỗi frame trong combat.  
Khi có skill ready, `UseSkillCoroutine` chạy theo quy trình:

1. Trigger animation qua `NetworkAnimator` (nếu có `animation_trigger`)
2. Chờ **0.3s** (hit frame)
3. Tính damage qua `EnemySkillSet.CalculateDamage()`
4. Gây damage bằng `ApplyDamageToTarget()` (gọi `NetworkPlayerHealth.TakeDamage`)
5. `EnemySkillSet.MarkSkillUsed()` → bắt đầu tính cooldown

### 1.2 Config trong Database

Cột `skills_json` trong bảng `enemy` — JSON array, mỗi phần tử là một `SkillEntry`:

```json
{
  "skill_id"          : "WIND_SLASH",
  "flat_damage"       : 20,
  "damage_multiplier" : 0,
  "element"           : "Wind",
  "cooldown_sec"      : 6.0,
  "range"             : 2.5,
  "aoe"               : false,
  "aoe_radius"        : 0,
  "animation_trigger" : "skill_slash",
  "status_effect"     : "",
  "duration_sec"      : 0
}
```

### 1.3 Giải Thích Từng Trường

| Trường | Kiểu | Mô tả |
|---|---|---|
| `skill_id` | string | ID duy nhất, không dấu cách (dùng cho cooldown key) |
| `flat_damage` | int | Damage cố định (> 0 = dùng trực tiếp, bỏ qua multiplier) |
| `damage_multiplier` | float | Hệ số × `base_damage` (chỉ dùng khi `flat_damage = 0`) |
| `cooldown_sec` | float | Giây hồi chiêu (mặc định 5s nếu ≤ 0) |
| `range` | float | Tầm đánh tối đa (Unity units) — enemy phải trong range mới cast |
| `aoe` | bool | **false** = đánh gần single-target |
| `animation_trigger` | string | Tên trigger trong Animator của enemy prefab (để trống = không animation riêng) |
| `status_effect` | string | Hiệu ứng: `"burn"`, `"freeze"`, `"slow"`, `"poison"` hoặc `""` |
| `duration_sec` | float | Thời gian hiệu ứng trạng thái (giây) |

### 1.4 Skill AoE (Đánh Vùng)

Để skill đánh tất cả player trong bán kính, set:

```json
{
  "skill_id"   : "FIRE_NOVA",
  "flat_damage": 35,
  "aoe"        : true,
  "aoe_radius" : 4.0,
  "range"      : 4.0
}
```

`EnemyAI.UseSkillCoroutine` dùng `Physics2D.OverlapCircleAll` với **LayerMask "Player"** để detect.

### 1.5 Config base_damage trong Enemy

`EnemySkillSet.BaseDamage` lấy từ `EnemySkillsEntry.base_damage` (cột `base_damage` trong bảng `enemy`).  
Dùng khi `flat_damage = 0` và `damage_multiplier > 0`.

```sql
UPDATE enemy SET base_damage = 15 WHERE enemy_id = 5;
```

---

## Phần 2: Skill Projectile (Bắn Đạn) cho Enemy

### 2.1 Enemy Thường (EnemyAI)

> **Lưu ý**: `EnemyAI.UseSkillCoroutine` hiện chưa tự động spawn projectile prefab.  
> Skill không có `aoe = true` sẽ gây damage trực tiếp theo range check, không tạo đạn bay.
>
> Để thêm projectile cho enemy thường → cần mở rộng `UseSkillCoroutine` trong `EnemyAI` để spawn prefab có `EnemyProjectile`.

### 2.2 Boss (BossAI)

`BossAI` hỗ trợ projectile thông qua `CastDirectSkill()`:

#### Cách gắn Prefab trên Inspector

| Field trong BossAI Inspector | Mô tả |
|---|---|
| `skillBreathPrefab` | Prefab đạn directional (bắn thẳng về phía player) |
| `skillNovaPrefab` | Prefab hiệu ứng AoE |
| `addSpawnPrefab` | Prefab enemy triệu hồi thêm |

Prefab đạn (`skillBreathPrefab`) cần có component:
- `EnemyProjectile` — xử lý damage khi chạm player
- `Rigidbody2D` — để nhận velocity
- `Collider2D` (isTrigger = true)

#### Component `EnemyProjectile` — Các trường cần chú ý

| Field | Mô tả |
|---|---|
| `damage` | Damage mặc định (BossAI override bằng config) |
| `destroyOnHit` | **true** = hủy projectile khi trúng player |
| `destroyOnGround` | **false** (mặc định) = **không** hủy khi chạm ground/wall — giữ nguyên để đạn bay qua sàn |

> **Fix đã áp dụng**: `destroyOnGround` mặc định là `false`. Nếu muốn đạn tan khi chạm sàn, set thành `true` trong Inspector của prefab.

#### Config Boss Skill trong API (`/api/dungeon/boss/{bossId}/config`)

`BossAI` đọc từ `SkillData` (player skill format) thông qua API. Với skill direct:

| Field | Giá trị |
|---|---|
| `skillType` | `Projectile` |
| `projectileSpeed` | Tốc độ đạn (units/s) — BossAI set vào `rb.velocity` |

---

## Phần 3: Skill Projectile cho Player (Tham Khảo)

Component `FireballDamage` gắn trên prefab đạn player:

| Field | Mô tả |
|---|---|
| `damage` | Damage base (bị override bởi `PlayerSkillManager`) |
| `destroyOnHit` | Hủy khi trúng enemy |
| `destroyOnGround` | **false** (mặc định sau fix) — **không** hủy khi chạm sàn |

> **Fix đã áp dụng**: `destroyOnGround` giờ mặc định `false`. Nếu cần đạn tan khi chạm sàn (ví dụ đạn nước/đất), check lại và set `true` trong prefab Inspector.

---

## Phần 4: Ví Dụ JSON Đầy Đủ cho Một Enemy

```sql
UPDATE enemy
SET
  base_damage  = 12,
  element_type = 'Fire',
  skills_json  = '[
    {
      "skill_id"          : "FIRE_BREATH",
      "flat_damage"       : 0,
      "damage_multiplier" : 2.5,
      "element"           : "Fire",
      "cooldown_sec"      : 8.0,
      "range"             : 5.0,
      "aoe"               : false,
      "aoe_radius"        : 0,
      "animation_trigger" : "skill_breath",
      "status_effect"     : "burn",
      "duration_sec"      : 3.0,
      "spawn_enemy_id"    : 0,
      "spawn_count"       : 0
    },
    {
      "skill_id"          : "FIRE_NOVA",
      "flat_damage"       : 25,
      "damage_multiplier" : 0,
      "element"           : "Fire",
      "cooldown_sec"      : 15.0,
      "range"             : 4.0,
      "aoe"               : true,
      "aoe_radius"        : 4.0,
      "animation_trigger" : "skill_nova",
      "status_effect"     : "",
      "duration_sec"      : 0,
      "spawn_enemy_id"    : 0,
      "spawn_count"       : 0
    }
  ]'
WHERE enemy_id = 6;
```

---

## Phần 5: Checklist Config Skill Enemy

### Melee / AoE Skill
- [ ] Thêm entry vào `skills_json` với `skill_id` không trùng
- [ ] Set `flat_damage > 0` HOẶC `damage_multiplier > 0` + `base_damage` trên enemy
- [ ] Set `range` phù hợp (phải ≤ `EnemyAI.detectionRange` để enemy tiếp cận được)
- [ ] Nếu có animation: đặt `animation_trigger` khớp với tên Trigger trong Animator prefab
- [ ] Nếu AoE: set `aoe = true` + `aoe_radius` (cần có player ở **Layer "Player"**)

### Projectile Skill (Boss)
- [ ] Gán `skillBreathPrefab` trong Inspector của Boss prefab
- [ ] Prefab có `EnemyProjectile` component
- [ ] `EnemyProjectile.destroyOnHit = true` (hủy khi trúng player)
- [ ] `EnemyProjectile.destroyOnGround = false` (không hủy khi chạm sàn — mặc định)
- [ ] Prefab có `Rigidbody2D` (gravity scale = 0 nếu bay ngang) và `Collider2D` trigger

---

## Phần 6: Tóm Tắt Luồng Code

```
DB skills_json
  └─► HostSpawnConfigLoader.SetSkillsFromConfig()
        └─► EnemySkillSet._skills (List<SkillEntry>)

EnemyAI.Update()
  └─► EnemySkillSet.TryGetReadySkill(dist)     ← kiểm tra range + cooldown
        └─► (có skill ready) UseSkillCoroutine()
              ├─► NetworkAnimator.SetTrigger()
              ├─► WaitForSeconds(0.3f)           ← hit frame delay
              ├─► EnemySkillSet.CalculateDamage()
              ├─► ApplyDamageToTarget() → NetworkPlayerHealth.TakeDamage()
              └─► EnemySkillSet.MarkSkillUsed()  ← bắt đầu cooldown
```
