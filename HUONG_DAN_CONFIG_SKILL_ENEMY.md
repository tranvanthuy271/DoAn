# Hướng dẫn Config Skill Enemy trong DB

> Tài liệu này hướng dẫn cách thêm, sửa, xóa skill cho enemy thông qua cột `skills_json` trong bảng `enemy`.
> Áp dụng cho **tất cả loại quái** — từ Slime đơn giản đến Boss cuối.

---

## 1. Tổng quan luồng hoạt động

```
DB (enemy.skills_json)
        │
        ▼  GET /api/map/{mapId}/spawn-config
 MapController.BuildEnemySkillsResponseAsync()
        │  trả về EnemySkillsEntry[] trong response
        ▼
 HostSpawnConfigLoader.BuildSkillLookup()
        │  build dict {enemy_id → EnemySkillsEntry}
        ▼
 HostSpawnConfigLoader.SpawnSingleEnemy()
        │  AddComponent<EnemySkillSet>() + SetSkillsFromConfig()
        ▼
 EnemyAI.Update() (mỗi frame, server-side)
        │  TryGetReadySkill(dist) → nếu có → UseSkillCoroutine()
        ▼
 EnemySkillSet.CalculateDamage() → apply damage → MarkSkillUsed()
```

**Quy tắc quan trọng:**
- `SUMMON_ADD` chỉ chạy qua **BossAI** (phase system) — EnemyAI thường tự động bỏ qua
- Skill được đọc **một lần khi spawn** — thay đổi DB không ảnh hưởng enemy đang sống
- Tất cả logic skill là **server-side** — client chỉ nhận animation trigger và kết quả damage

---

## 2. Schema JSON đầy đủ

Mỗi enemy có một mảng JSON, mỗi phần tử là một skill:

```json
[
  {
    "skill_id"          : "FIRE_BREATH",
    "flat_damage"       : 0,
    "damage_multiplier" : 2.5,
    "element"           : "Fire",
    "cooldown_sec"      : 8.0,
    "range"             : 5.0,
    "aoe"               : false,
    "aoe_radius"        : 3.0,
    "animation_trigger" : "skill_fireBreath",
    "status_effect"     : "Burn",
    "duration_sec"      : 3.0,
    "spawn_enemy_id"    : 0,
    "spawn_count"       : 0
  }
]
```

### Giải thích từng field

| Field | Kiểu | Mô tả |
|---|---|---|
| `skill_id` | string | ID duy nhất, **không dấu cách**. Dùng để tra cooldown và animation. |
| `flat_damage` | int | Damage tuyệt đối (điểm HP). **Nếu > 0 → dùng trực tiếp**, bỏ qua multiplier. |
| `damage_multiplier` | float | Hệ số nhân lên `base_damage` của quái. **Chỉ dùng khi `flat_damage = 0`**. |
| `element` | string | Nguyên tố skill: `Fire`, `Water`, `Earth`, `Metal`, `Wood`, `Wind`, `None` |
| `cooldown_sec` | float | Giây hồi chiêu. Mặc định 5.0 nếu ≤ 0. |
| `range` | float | Tầm đánh (Unity units). Mặc định 4.0 nếu ≤ 0. |
| `aoe` | bool | `true` = tấn công tất cả player trong bán kính |
| `aoe_radius` | float | Bán kính AoE. Chỉ dùng khi `aoe = true`. Mặc định 3.0 nếu = 0. |
| `animation_trigger` | string | Tên trigger trong Animator. Rỗng = không có animation riêng. |
| `status_effect` | string | Hiệu ứng trạng thái: `Burn`, `Freeze`, `Slow`, `Poison`, rỗng = không có. |
| `duration_sec` | float | Thời gian duy trì `status_effect` (giây). |
| `spawn_enemy_id` | int | Chỉ dùng cho `SUMMON_ADD` — ID loại quái sẽ triệu hồi. |
| `spawn_count` | int | Số lượng quái triệu hồi. Chỉ khi `skill_id = "SUMMON_ADD"`. |

---

## 3. Tính toán sát thương

```
Nếu flat_damage > 0:
    damage = flat_damage
Ngược lại:
    damage = round(base_damage × damage_multiplier)
    damage = max(damage, 1)
```

**Ví dụ:**
- Enemy có `base_damage = 20`, skill `flat_damage = 0`, `damage_multiplier = 2.5` → damage = **50**
- Enemy có `base_damage = 20`, skill `flat_damage = 35`, `damage_multiplier = 1.0` → damage = **35** (flat ưu tiên)

---

## 4. Các kiểu skill phổ biến

### 4.1 Đánh melee mạnh (single target)
```json
{
  "skill_id"          : "POWER_SLASH",
  "flat_damage"       : 0,
  "damage_multiplier" : 2.0,
  "element"           : "None",
  "cooldown_sec"      : 6.0,
  "range"             : 1.5,
  "aoe"               : false,
  "aoe_radius"        : 0.0,
  "animation_trigger" : "skill_powerSlash",
  "status_effect"     : "",
  "duration_sec"      : 0.0,
  "spawn_enemy_id"    : 0,
  "spawn_count"       : 0
}
```

### 4.2 Tấn công tầm xa (projectile / ranged)
```json
{
  "skill_id"          : "FIRE_BOLT",
  "flat_damage"       : 40,
  "damage_multiplier" : 0.0,
  "element"           : "Fire",
  "cooldown_sec"      : 4.0,
  "range"             : 7.0,
  "aoe"               : false,
  "aoe_radius"        : 0.0,
  "animation_trigger" : "skill_fireBolt",
  "status_effect"     : "Burn",
  "duration_sec"      : 2.0,
  "spawn_enemy_id"    : 0,
  "spawn_count"       : 0
}
```

### 4.3 Tấn công diện (AoE)
```json
{
  "skill_id"          : "EARTH_QUAKE",
  "flat_damage"       : 0,
  "damage_multiplier" : 3.0,
  "element"           : "Earth",
  "cooldown_sec"      : 12.0,
  "range"             : 3.0,
  "aoe"               : true,
  "aoe_radius"        : 4.0,
  "animation_trigger" : "skill_earthQuake",
  "status_effect"     : "",
  "duration_sec"      : 0.0,
  "spawn_enemy_id"    : 0,
  "spawn_count"       : 0
}
```

### 4.4 Skill gây hiệu ứng (status only, damage thấp)
```json
{
  "skill_id"          : "POISON_MIST",
  "flat_damage"       : 5,
  "damage_multiplier" : 0.0,
  "element"           : "None",
  "cooldown_sec"      : 10.0,
  "range"             : 3.0,
  "aoe"               : true,
  "aoe_radius"        : 3.0,
  "animation_trigger" : "skill_poisonMist",
  "status_effect"     : "Poison",
  "duration_sec"      : 5.0,
  "spawn_enemy_id"    : 0,
  "spawn_count"       : 0
}
```

### 4.5 Triệu hồi thêm quái (chỉ Boss, dùng SUMMON_ADD)
```json
{
  "skill_id"          : "SUMMON_ADD",
  "flat_damage"       : 0,
  "damage_multiplier" : 0.0,
  "element"           : "None",
  "cooldown_sec"      : 30.0,
  "range"             : 5.0,
  "aoe"               : false,
  "aoe_radius"        : 0.0,
  "animation_trigger" : "skill_summon",
  "status_effect"     : "",
  "duration_sec"      : 0.0,
  "spawn_enemy_id"    : 2,
  "spawn_count"       : 3
}
```
> ⚠️ `SUMMON_ADD` bị **EnemyAI thường bỏ qua** — chỉ hoạt động qua `BossAI` phase system.

---

## 5. Danh sách enemy mẫu đã seed

| ID | Tên | Nguyên tố | base_damage | Loại | Số skill |
|---|---|---|---|---|---|
| 1 | Slime | Water | 8 | Normal | 1 (WATER_BURST) |
| 2 | Goblin | Earth | 12 | Normal | 1 (DIRT_THROW) |
| 3 | Ice Wolf | Water | 20 | Normal | 2 (ICE_BITE, ICE_HOWL) |
| 4 | Goblin Chief | Earth | 35 | Boss | 3 (EARTH_SLAM, CHARGE, SUMMON_ADD) |
| 5 | Fire Slime | Fire | 22 | Normal | 1 (FIRE_BURST) |
| 6 | Goblin Archer | Earth | 18 | Normal | 2 (QUICK_SHOT, ARROW_RAIN) |
| 7 | Snow Goblin | Water | 18 | Normal | 1 (ICE_SHARD) |
| 8 | Fire Dragon | Fire | 60 | Boss | 3 (FIRE_BREATH, WING_SLAM, SUMMON_ADD) |
| 9 | Ice Witch | Water | 50 | Boss | 2 (BLIZZARD, ICE_LANCE) |
| 10 | Final Dragon | Fire | 100 | Boss | 3 (MULTI_BREATH, WING_STORM, SUMMON_ADD) |

---

## 6. Cách chỉnh sửa nhanh qua SQL

### Thêm skill vào enemy đã có

```sql
-- Thêm skill THUNDER_STRIKE vào Goblin (id=2)
UPDATE enemy
SET skills_json = JSON_ARRAY_APPEND(
    skills_json,
    '$',
    JSON_OBJECT(
        'skill_id',          'THUNDER_STRIKE',
        'flat_damage',       35,
        'damage_multiplier', 0.0,
        'element',           'Metal',
        'cooldown_sec',      7.0,
        'range',             3.0,
        'aoe',               FALSE,
        'aoe_radius',        0.0,
        'animation_trigger', 'skill_thunderStrike',
        'status_effect',     '',
        'duration_sec',      0.0,
        'spawn_enemy_id',    0,
        'spawn_count',       0
    )
),
updated_at = NOW()
WHERE enemy_id = 2;
```

### Thay toàn bộ skills_json

```sql
UPDATE enemy
SET skills_json = '[
  {
    "skill_id": "ICE_BITE",
    "flat_damage": 25,
    "damage_multiplier": 0.0,
    "element": "Water",
    "cooldown_sec": 4.0,
    "range": 1.5,
    "aoe": false,
    "aoe_radius": 0.0,
    "animation_trigger": "skill_iceBite",
    "status_effect": "Freeze",
    "duration_sec": 2.0,
    "spawn_enemy_id": 0,
    "spawn_count": 0
  }
]',
updated_at = NOW()
WHERE enemy_id = 3;
```

### Xóa toàn bộ skill (quái không dùng skill)

```sql
UPDATE enemy
SET skills_json = '[]', updated_at = NOW()
WHERE enemy_id = 1;
```

### Kiểm tra kết quả

```sql
SELECT
    enemy_id,
    enemy_name,
    element_type,
    base_damage,
    JSON_LENGTH(skills_json) AS so_skill,
    JSON_EXTRACT(skills_json, '$[0].skill_id') AS skill_dau_tien
FROM enemy
ORDER BY enemy_id;
```

---

## 7. Quy tắc đặt tên `skill_id`

- **UPPER_SNAKE_CASE**: `FIRE_BREATH`, `ICE_HOWL`, `EARTH_SLAM`
- **Không dấu cách, không ký tự đặc biệt**
- Duy nhất trong phạm vi một enemy (không cần duy nhất toàn DB)
- `SUMMON_ADD` là **từ khóa dành riêng** — tên này trigger cơ chế triệu hồi

---

## 8. Thêm enemy mới với skill

```sql
INSERT INTO enemy (
    enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
    move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
    element_type, enemy_type, skills_json, created_at, updated_at
) VALUES (
    11, 'Lava Golem', 12, 600, 0, 40, 15,
    1.5, 0.8, 150, 25, 80,
    'Fire', 'Normal',
    '[
      {
        "skill_id": "LAVA_SMASH",
        "flat_damage": 0,
        "damage_multiplier": 2.2,
        "element": "Fire",
        "cooldown_sec": 9.0,
        "range": 2.0,
        "aoe": true,
        "aoe_radius": 2.5,
        "animation_trigger": "skill_lavaSmash",
        "status_effect": "Burn",
        "duration_sec": 3.0,
        "spawn_enemy_id": 0,
        "spawn_count": 0
      },
      {
        "skill_id": "ROCK_THROW",
        "flat_damage": 55,
        "damage_multiplier": 0.0,
        "element": "Earth",
        "cooldown_sec": 6.0,
        "range": 6.0,
        "aoe": false,
        "aoe_radius": 0.0,
        "animation_trigger": "skill_rockThrow",
        "status_effect": "",
        "duration_sec": 0.0,
        "spawn_enemy_id": 0,
        "spawn_count": 0
      }
    ]',
    NOW(), NOW()
)
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();
```

---

## 9. Thêm spawn config cho enemy mới vào map

Sau khi thêm enemy vào bảng `enemy`, cập nhật `map_spawn_config`:

```sql
-- Thêm Lava Golem (id=11) vào Map 1 (Cánh Đồng Lửa)
UPDATE map_spawn_config
SET spawn_json = JSON_ARRAY_APPEND(
    spawn_json,
    '$',
    JSON_OBJECT(
        'enemy_id',     11,
        'hp',           600,
        'exp',          150,
        'cx',           5.0,
        'cy',           0.0,
        'is_boss',      FALSE,
        'count',        2,
        'respawn_time', 45
    )
),
updated_at = NOW()
WHERE map_id = 1;
```

---

## 10. Lỗi thường gặp

| Tình huống | Nguyên nhân | Cách sửa |
|---|---|---|
| Enemy không dùng skill | `skills_json` rỗng hoặc NULL trong DB | Chạy UPDATE với JSON hợp lệ |
| Skill dùng damage = 0 | Cả `flat_damage = 0` và `damage_multiplier = 0` | Đặt ít nhất một trong hai > 0 |
| Boss triệu hồi không ra quái | `SUMMON_ADD` nhưng `spawn_enemy_id = 0` | Đặt `spawn_enemy_id` = ID quái muốn triệu hồi |
| Skill cast liên tục không cooldown | `cooldown_sec ≤ 0` | EnemySkillSet tự set default 5.0, kiểm tra lại |
| Quái thường cast `SUMMON_ADD` | Không — EnemyAI bỏ qua `SUMMON_ADD` | Đây là behavior đúng, không cần sửa |
| JSON parse error server log | JSON không hợp lệ (dấu `'`, thiếu `"`) | Dùng `JSON_VALID(skills_json)` để kiểm tra |

```sql
-- Kiểm tra JSON hợp lệ
SELECT enemy_id, enemy_name,
    JSON_VALID(skills_json) AS json_ok
FROM enemy
WHERE skills_json IS NOT NULL;
```

---

## 11. Files liên quan

| File | Vai trò |
|---|---|
| `GameServerApi/GameServerApi/migration_enemy_skill_config.sql` | **Migration DB** — chạy file này để seed dữ liệu |
| `GameServerApi/Models/Entities/Enemy.cs` | Entity C# — mapping `skills_json` → `SkillsJson` |
| `GameServerApi/Controllers/MapController.cs` | API trả về `enemy_skills[]` trong spawn-config response |
| `Client/Assets/Scripts/Network/Enemy/MapSpawnConfigDto.cs` | DTO deserialize — `EnemySkillsEntry`, `SkillEntry` |
| `Client/Assets/Scripts/Network/Enemy/HostSpawnConfigLoader.cs` | Build skill lookup, gọi `EnemySkillSet.SetSkillsFromConfig()` |
| `Client/Assets/Scripts/Enemy/EnemySkillSet.cs` | Runtime component — cooldown tracking, tính damage |
| `Client/Assets/Scripts/Enemy/EnemyAI.cs` | Dùng `EnemySkillSet.TryGetReadySkill()` trong combat loop |
