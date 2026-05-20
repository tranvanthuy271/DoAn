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

> **Quy tắc damage duy nhất**: Mọi skill (melee, AoE, projectile) đều dùng chung `EnemyAI.damage`.  
> Giá trị này được set tự động từ cột `base_damage` trong bảng `enemy` lúc spawn.  
> **Không cần** set damage trong từng skill entry.

---

## Phần 1: Skill Đánh Gần (Melee) cho Enemy Thường

### 1.1 Cơ Chế Hoạt Động

`EnemyAI` gọi `EnemySkillSet.TryGetReadySkill()` mỗi frame trong combat.  
Khi có skill ready, `UseSkillCoroutine` chạy theo quy trình:

1. Bật bool `isAttacking = true` để chạy animation **"Attack"** — cùng animation với đòn đánh thường
2. Chờ **0.3s** (hit frame)
3. Dùng `EnemyAI.damage` (lấy từ `base_damage` trong DB) làm damage
4. Gây damage bằng `ApplyDamageToTarget()` (gọi `NetworkPlayerHealth.TakeDamage`)
5. Chờ **0.5s** (tail animation) → `ForceResetAttackState()` → quay về State.Run

### 1.2 Animator Setup trong Unity

Enemy cần có **2 state** trong Animator:

| State | Điều kiện chuyển |
|---|---|
| `Run` (default) | `isAttacking = false`, di chuyển |
| `Attack` | `isAttacking = true` → play animation đánh, xong thì trả về `Run` |

**Cách setup Animator:**
1. Mở prefab enemy → tab **Animator**
2. Tạo 2 Animator State: `Run` và `Attack`
3. `Run` → `Attack`: Condition = `isAttacking == true`
4. Gán animation clip tương ứng cho mỗi state
5. `Attack` → `Run`: Condition = `isAttacking == false` hoặc dùng **Exit Time** nếu animator của bạn đang setup như vậy

> **Lưu ý**: Chuẩn hiện tại là bool **`isAttacking`**. Nếu data cũ còn để `"Attack"` trong config thì code vẫn tự map về `isAttacking`.

### 1.3 Config trong Database

Cột `skills_json` trong bảng `enemy` — JSON array, mỗi phần tử là một `SkillEntry`:

```json
{
  "skill_id"          : "WIND_SLASH",
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

> Damage không cần khai báo trong skill — tự động dùng `base_damage` của enemy.

### 1.3 Giải Thích Từng Trường SkillEntry

| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `skill_id` | string | ✅ | ID duy nhất, không dấu cách (dùng cho cooldown key) |
| `element` | string | | Nguyên tố skill (`"Fire"`, `"Water"`, `"None"`…) |
| `cooldown_sec` | float | | Giây hồi chiêu (mặc định 5s nếu ≤ 0) |
| `range` | float | | Tầm đánh tối đa (Unity units, mặc định 4) |
| `aoe` | bool | | **false** = đánh gần single-target |
| `aoe_radius` | float | | Bán kính AoE (chỉ dùng khi `aoe = true`) |
| `animation_trigger` | string | | Tên parameter custom trong Animator. Để trống, `"Attack"` hoặc `"isAttacking"` đều sẽ dùng bool chuẩn `isAttacking`; tên khác vẫn có thể là Trigger riêng |
| `status_effect` | string | | Hiệu ứng: `"burn"`, `"freeze"`, `"slow"`, `"poison"` hoặc `""` |
| `duration_sec` | float | | Thời gian hiệu ứng trạng thái (giây) |
| `projectile_prefab_key` | string | | Key prefab đạn — **để trống** = melee/direct hit |
| `projectile_speed` | float | | Tốc độ đạn (mặc định 8) |
| `projectile_lifetime` | float | | Giây tự hủy nếu miss (mặc định 3) |
| `projectile_spawn_offset_x` | float | | Offset X về phía trước mặt quái (mặc định 0.6) |
| `projectile_spawn_offset_y` | float | | Offset Y điểm bắn (mặc định 0.25) |

### 1.4 Skill AoE (Đánh Vùng)

Để skill đánh tất cả player trong bán kính:

```json
{
  "skill_id"   : "FIRE_NOVA",
  "element"    : "Fire",
  "cooldown_sec": 10.0,
  "aoe"        : true,
  "aoe_radius" : 4.0,
  "range"      : 4.0,
  "animation_trigger" : "skill_nova"
}
```

`EnemyAI.UseSkillCoroutine` dùng `Physics2D.OverlapCircleAll` với **LayerMask "Player"** để detect.

### 1.5 Set Damage cho Enemy

Damage của mọi skill = cột `base_damage` trong bảng `enemy`. Set cho từng loại quái:

```sql
UPDATE enemy SET base_damage = 15 WHERE enemy_id = 2;
```

`HostSpawnConfigLoader` tự đọc giá trị này và gán vào `EnemyAI.damage` lúc spawn — không cần làm thêm gì trên Unity.

---

## Phần 2: Skill Projectile (Bắn Đạn) cho Enemy Thường

### 2.1 Cơ Chế Hoạt Động

`EnemyAI` hỗ trợ 3 kiểu cast — quyết định theo thứ tự:

| Ưu tiên | Điều kiện | Hành động |
|---|---|---|
| 1 | `projectile_prefab_key != ""` | Bật `isAttacking` → spawn projectile networked tại hit frame |
| 2 | `aoe = true` | Bật `isAttacking` → nổ quanh enemy, hit tất cả player trong `aoe_radius` |
| 3 | Còn lại | Bật `isAttacking` → gây damage trực tiếp đến player trong tầm |

**Mọi kiểu đều dùng chung animation "Attack"** — enemy sẽ chạy đến tầm `skill.range`, đứng lại, play animation Attack, rồi:
- Nếu là melee/direct: gây damage trực tiếp
- Nếu là projectile: spawn đạn bay về phía player

### 2.2 Luồng Di Chuyển + Tấn Công

```
Player ở xa (dist > skill.range)
  → Enemy chạy animation "Run", RunTowards player

Player vào tầm (dist ≤ skill.range) + skill off cooldown
  → Enemy dừng lại (velocity = 0)
  → SetBool("isAttacking", true) → play animation Attack
  → Sau 0.3s (hit frame):
       Melee/AoE  → gây damage trực tiếp
       Projectile → spawn đạn bay về phía player
  → Sau 0.5s (tail) → ForceResetAttackState() → về "Run"

Skill đang cooldown, player trong tầm meleeAttackRange (1.2u)
  → Basic melee attack (không cần skill config)
```

### 2.3 Config DB cho Skill Projectile

```json
{
  "skill_id"                  : "FIRE_SHOT",
  "element"                   : "Fire",
  "cooldown_sec"              : 4.0,
  "range"                     : 6.0,
  "aoe"                       : false,
  "projectile_prefab_key"     : "FireballProjectile",
  "projectile_speed"          : 8.5,
  "projectile_lifetime"       : 2.5,
  "projectile_spawn_offset_x" : 0.9,
  "projectile_spawn_offset_y" : 0.35
}
```

> **`animation_trigger` không cần điền** — để trống là enemy tự dùng bool `isAttacking` để vào animation "Attack".  
> Chỉ điền nếu muốn dùng trigger Animator khác (ví dụ boss skill đặc biệt).

### 2.4 Cấu Hình Prefab Enemy trong Unity Inspector

Mở prefab enemy (`Enemy1.prefab` hoặc prefab tương ứng), chọn component **EnemyAI**, tìm list **Projectile Prefabs**:

```
EnemyAI
├─ [Header] Projectile Skills
├─ Allow Projectile Flight ← bật nếu quái này được phép bay theo X/Y
├─ Projectile Spawn Point    ← (tuỳ chọn) Transform điểm bắn, để trống = dùng offset
└─ Projectile Prefabs        ← List key → prefab
   ├─ [0] Key: "FireballProjectile"   Prefab: FireballProjectile
   └─ [1] Key: "WaterBoltProjectile"  Prefab: WaterBoltProjectile
```

`Allow Projectile Flight = false`
- Quái vẫn dùng range của skill projectile để đứng bắn từ xa
- Quái chỉ di chuyển ngang để giữ khoảng cách hoặc áp sát

`Allow Projectile Flight = true`
- Quái được phép di chuyển cả X/Y để bay đuổi hoặc lùi
- Khi đi xuống qua nhiều platform `Ground`, quái chỉ bị chặn ở platform cuối cùng; nếu còn ground bên dưới thì vẫn được rơi xuyên xuống

**Các bước thêm key mới:**
1. Chọn prefab enemy trong Project → mở Inspector
2. Component `EnemyAI` → cuộn xuống phần **Projectile Skills**
3. Tick **Allow Projectile Flight** nếu đây là quái bay
4. Nhấn **+** trong list **Projectile Prefabs**
5. Điền **Key** (phải khớp chính xác với `projectile_prefab_key` trong DB)
6. Kéo prefab đạn vào **Prefab** slot
7. Save prefab

> **Lưu ý**: Key so sánh không phân biệt hoa thường (`FireballProjectile` = `fireballprojectile`).
> Nếu quên thêm key trong `Projectile Prefabs`, `EnemyAI` sẽ thử fallback tìm prefab đã đăng ký trong `NetworkPrefabs` theo đúng tên prefab. Dù vậy vẫn nên map explicit trong Inspector để dễ kiểm soát.

### 2.4 Yêu Cầu Prefab Đạn

Prefab được dùng làm đạn enemy cần có:

| Component | Ghi chú |
|---|---|
| `NetworkObject` | Bắt buộc — để spawn/despawn đồng bộ qua mạng |
| `NetworkTransform` | Để nội suy vị trí trên client |
| `EnemyProjectile` | Script xử lý damage khi chạm player |
| `Rigidbody2D` | Để nhận velocity; `gravityScale` tự set = 0 khi spawn |
| `Collider2D` | `isTrigger = true` |

`EnemyAI` sẽ tự động:
- Disable `FireballDamage` nếu có (để tránh đạn hit enemy thay vì player)
- Add `EnemyProjectile` nếu chưa có
- Set `damage` và `lifetime` từ config
- Set velocity theo hướng player

### 2.5 Projectile Spawn Point (Tuỳ Chọn)

Mặc định điểm bắn = `enemy.position + offset` (từ `projectile_spawn_offset_x/y`).

Nếu muốn bắn từ một điểm cố định trên thân enemy (ví dụ: mõm/tay):
1. Thêm **child GameObject** vào prefab enemy, đặt tên `ProjectileSpawnPoint`
2. Kéo Transform đó vào field **Projectile Spawn Point** của `EnemyAI`

---

## Phần 3: Boss (BossAI)

`BossAI` hỗ trợ projectile thông qua `CastDirectSkill()`. Prefab gắn trực tiếp trên Inspector:

| Field trong BossAI Inspector | Mô tả |
|---|---|
| `skillBreathPrefab` | Prefab đạn bắn thẳng về phía player |
| `skillNovaPrefab` | Prefab hiệu ứng AoE |
| `addSpawnPrefab` | Prefab enemy triệu hồi thêm |

Prefab đạn boss cần có: `EnemyProjectile`, `Rigidbody2D`, `Collider2D (isTrigger)`.

#### Component `EnemyProjectile` — Các trường trong Inspector

| Field | Mô tả |
|---|---|
| `damage` | Damage mặc định (BossAI/EnemyAI override khi spawn) |
| `destroyOnHit` | **true** = hủy projectile khi trúng player |
| `destroyOnGround` | **false** (mặc định) = không hủy khi chạm ground/wall |

---

## Phần 4: Ví Dụ SQL Đầy Đủ

### Enemy melee + AoE

```sql
UPDATE enemy
SET
  base_damage  = 12,
  element_type = 'Fire',
  skills_json  = '[
    {
      "skill_id"          : "FIRE_BREATH",
      "element"           : "Fire",
      "cooldown_sec"      : 8.0,
      "range"             : 5.0,
      "aoe"               : false,
      "animation_trigger" : "skill_breath",
      "status_effect"     : "burn",
      "duration_sec"      : 3.0
    },
    {
      "skill_id"          : "FIRE_NOVA",
      "element"           : "Fire",
      "cooldown_sec"      : 15.0,
      "range"             : 4.0,
      "aoe"               : true,
      "aoe_radius"        : 4.0,
      "animation_trigger" : "skill_nova"
    }
  ]'
WHERE enemy_id = 6;
```

### Enemy bắn đạn

```sql
UPDATE enemy
SET
  base_damage  = 18,
  element_type = 'Fire',
  skills_json  = '[
    {
      "skill_id"                  : "FIRE_SHOT",
      "element"                   : "Fire",
      "cooldown_sec"              : 4.0,
      "range"                     : 6.0,
      "projectile_prefab_key"     : "FireballProjectile",
      "projectile_speed"          : 8.5,
      "projectile_lifetime"       : 2.5,
      "projectile_spawn_offset_x" : 0.9,
      "projectile_spawn_offset_y" : 0.35,
      "animation_trigger"         : "skill_fireShot"
    }
  ]'
WHERE enemy_id = 5;
```

---

## Phần 5: Checklist Config Skill Enemy

### Damage
- [ ] Set `base_damage` trong DB cho loại quái → đây là damage của **mọi skill** (melee + AoE + projectile)

### Melee / Direct Hit
- [ ] Thêm entry `skills_json` với `skill_id` duy nhất
- [ ] Set `range` ≤ `EnemyAI.detectionRange`
- [ ] Nếu có animation riêng: `animation_trigger` khớp parameter trong Animator prefab

### AoE
- [ ] `aoe = true` + `aoe_radius > 0`
- [ ] Player phải ở **Layer "Player"**

### Projectile
- [ ] `projectile_prefab_key` trong DB khớp chính xác key trong `EnemyAI.projectilePrefabs` (Inspector prefab enemy)
- [ ] Nếu quái cần bay theo X/Y: bật `Allow Projectile Flight` trên `EnemyAI`
- [ ] Prefab đạn có `NetworkObject` + `NetworkTransform` + `EnemyProjectile` + `Rigidbody2D` + `Collider2D (isTrigger)`
- [ ] Prefab đạn đã đăng ký trong **NetworkPrefabsList**
- [ ] Set `projectile_lifetime` > 0 (tránh đạn miss tồn tại mãi)

---

## Phần 6: Tóm Tắt Luồng Code

```
DB: enemy.base_damage
  └─► HostSpawnConfigLoader.SpawnSingleEnemy()
        └─► EnemyAI.damage = base_damage         ← gán damage lúc spawn

DB: enemy.skills_json
  └─► HostSpawnConfigLoader.SetSkillsFromConfig()
        └─► EnemySkillSet._skills (List<SkillEntry>)

EnemyAI.Update() — chạy trên server
  │
  ├─ dist > skill.range  → RunTowards(player)     ← animation Run
  │
  └─ dist ≤ skill.range + skill off cooldown
       └─► UseSkillCoroutine(skill)
             ├─► skill.animation_trigger != ""?
             │     YES → dùng parameter custom nếu có
             │     NO  → TriggerAttackAnimation()  ← SetBool("isAttacking", true) — cùng với melee
             ├─► WaitForSeconds(0.3f)              ← hit frame
             ├─► dmg = EnemyAI.damage
             ├─► projectile_prefab_key != ""?
             │     YES → TrySpawnProjectileSkill() → Instantiate + NetworkObject.Spawn()
             │     NO + aoe → OverlapCircleAll + ApplyDamageToTarget()
             │     NO       → ApplyDamageToTarget(player) trong tầm
             ├─► EnemySkillSet.MarkSkillUsed()    ← bắt đầu cooldown
             ├─► WaitForSeconds(0.5f)             ← tail animation
             └─► ForceResetAttackState()           ← state = Run, reset animation
```
