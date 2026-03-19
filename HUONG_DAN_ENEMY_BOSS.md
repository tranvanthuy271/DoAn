# HƯỚNG DẪN ENEMY & BOSS SYSTEM

> **Phiên bản**: 1.0 — Dựa trên phân tích LangLa Mob.java + BossTpl.java + Map.DropConfig

---

## MỤC LỤC

1. [Chỉ số Enemy — Bảng tham chiếu](#1-chỉ-số-enemy--bảng-tham-chiếu)
2. [Nguyên tố & Kháng nguyên tố](#2-nguyên-tố--kháng-nguyên-tố)
3. [Cấu hình Boss — skills_json](#3-cấu-hình-boss--skills_json)
4. [Cấu hình Boss — phases_json](#4-cấu-hình-boss--phases_json)
5. [Boss Spawn Config](#5-boss-spawn-config)
6. [Drop Rate hệ thống](#6-drop-rate-hệ-thống)
7. [BossAI.cs — Hướng dẫn Setup Unity](#7-bossaics--hướng-dẫn-setup-unity)
8. [MobPatrolAI.cs — Hướng dẫn Setup Unity](#8-mobpatrolaics--hướng-dẫn-setup-unity)
9. [Danh sách Enemy hiện tại](#9-danh-sách-enemy-hiện-tại)
10. [Thêm Enemy / Boss mới](#10-thêm-enemy--boss-mới)

---

## 1. Chỉ số Enemy — Bảng tham chiếu

### Bảng `enemy` — Cột đầy đủ

| Cột | Kiểu | Mô tả |
|-----|------|-------|
| enemy_id | INT PK | |
| enemy_name | VARCHAR | Tên hiển thị |
| level | INT | Level của enemy |
| base_hp | INT | HP tối đa |
| base_damage | INT | Sát thương cơ bản |
| base_defense | INT | Phòng thủ vật lý |
| move_speed | FLOAT | Tốc độ di chuyển (Unity units/s) |
| attack_speed | FLOAT | Tốc độ đòn đánh (s/đòn) |
| exp_reward | INT | EXP thưởng khi giết |
| gold_reward | INT | Vàng thưởng |
| silver_reward | INT | Bạc thưởng |
| element_type | VARCHAR | Nguyên tố: Fire/Water/Wood/Metal/Earth/Wind/None |
| is_boss | TINYINT | 1 = Boss |
| **khang_hoa** | INT | Kháng Hỏa % (0-100) |
| **khang_thuy** | INT | Kháng Thủy % |
| **khang_tho** | INT | Kháng Thổ % |
| **khang_moc** | INT | Kháng Mộc % |
| **khang_kim** | INT | Kháng Kim % |
| **khang_phong** | INT | Kháng Phong % |
| **tang_dame_hoa** | INT | Tăng sát thương khi đấm Hỏa % |
| **tang_dame_thuy** | INT | Tăng sát thương khi đấm Thủy % |
| *(tương tự cho tho/moc/kim/phong)* | | |
| **hp_regen_per_sec** | FLOAT | Hồi HP mỗi giây (HoiHp) |
| **evasion_rate** | FLOAT | % né tránh đòn (NeTranh) |
| **counter_rate** | FLOAT | % phản đòn (PhanDon) |
| **skills_json** | TEXT | JSON skills của boss |
| **phases_json** | TEXT | JSON phases của boss |

---

### Công thức tính Sát thương nhận

```
damage_nhận = base_damage_player
            × (1 - khang_element / 100)     ← kháng nguyên tố
            × (1 + weaken_bonus / 100)       ← nếu bị yếu hóa (+30%)
            - base_defense                   ← phòng thủ vật lý
            (tối thiểu = 1)
```

---

## 2. Nguyên tố & Kháng nguyên tố

### Hệ nguyên tố (từ LangLa Mob.java field `he`)

| ID | Nguyên tố | Tên tiếng Anh | Màu gợi ý |
|----|-----------|---------------|-----------|
| 0 | Không có | None | Trắng |
| 1 | Hỏa | Fire | Đỏ cam |
| 2 | Thủy | Water | Xanh biển |
| 3 | Thổ | Earth | Nâu vàng |
| 4 | Mộc | Wood | Xanh lá |
| 5 | Kim | Metal | Bạch kim |
| 6 | Phong | Wind | Xanh nhạt |

### Tương khắc nguyên tố (đề xuất)

```
Hỏa → khắc Mộc → khắc Thổ → khắc Thủy → khắc Hỏa
Kim → khắc Phong (extra 20% damage)
```

Triển khai: Khi skill nguyên tố A tấn công quái nguyên tố B bị khắc → `damage * 1.2`

---

### Cấu hình kháng nguyên tố mẫu

```sql
-- Quái lửa: kháng hỏa 80%, yếu thuỷ -30%
UPDATE enemy SET
  khang_hoa = 80,
  khang_thuy = 0,    -- chú ý: kháng thấp = dễ bị đánh
  tang_dame_thuy = 30  -- quái lấy thêm 30% từ đòn thủy
WHERE enemy_id = 7;   -- Lửa Thú
```

---

## 3. Cấu hình Boss — skills_json

### Cú pháp

```json
[
  {
    "skill_id":           "FIRE_BREATH",       // ID nội bộ (không dấu cách)
    "damage_multiplier":  2.5,                  // Nhân với base_damage
    "element":            "Fire",               // Nguyên tố của skill
    "cooldown_sec":       8,                    // Giây hồi chiêu
    "range":              6,                    // Range đơn vị Unity
    "aoe":                false,               // true = vùng diện
    "animation_trigger":  "skill_breath",       // Tên trigger Animator
    "status_effect":      "burn",               // dot effect (tùy chọn)
    "duration_sec":       3                     // thời gian hiệu ứng
  },
  {
    "skill_id":           "FLAME_NOVA",
    "damage_multiplier":  1.5,
    "element":            "Fire",
    "cooldown_sec":       12,
    "range":              8,
    "aoe":                true,
    "animation_trigger":  "skill_nova"
  },
  {
    "skill_id":           "SUMMON_ADD",        // Triệu hồi từ phase, không dùng trong skill loop
    "spawn_enemy_id":     6,
    "spawn_count":        2,
    "cooldown_sec":       20,
    "animation_trigger":  "skill_summon"
  }
]
```

### Các trường `skill_id` chuẩn đề xuất

| skill_id | Hành động |
|----------|-----------|
| FIRE_BREATH | Thổi lửa thẳng |
| FLAME_NOVA | Bùng nổ lửa AoE |
| ICE_LANCE | Phóng giáo băng |
| BLIZZARD | Bão tuyết AoE |
| VINE_SNARE | Bẫy dây leo (làm chậm) |
| LEAF_STORM | Lốc lá AoE |
| SHADOW_SLASH | Chém bóng tối thẳng |
| DARK_PULSE | Xung bóng tối AoE |
| SUMMON_ADD | Triệu hồi quái thêm |
| HEAL_SELF | Boss tự hồi máu |

---

### Ví dụ Boss Phong hoàn chỉnh (skills_json)

```json
[
  {
    "skill_id": "WIND_SLASH",
    "damage_multiplier": 2.0,
    "element": "Wind",
    "cooldown_sec": 6,
    "range": 7,
    "aoe": false,
    "animation_trigger": "skill_wind_slash"
  },
  {
    "skill_id": "TORNADO",
    "damage_multiplier": 1.8,
    "element": "Wind",
    "cooldown_sec": 15,
    "range": 9,
    "aoe": true,
    "animation_trigger": "skill_tornado"
  }
]
```

---

## 4. Cấu hình Boss — phases_json

### Cú pháp

```json
[
  {
    "hp_pct_threshold": 75,               // Trigger khi HP ≤ 75%
    "action": "enrage",                   // enrage | summon | heal | berserk
    "damage_multiplier": 1.2,             // Tăng damage tổng lên 1.2x
    "speed_multiplier": 1.1,              // Tăng tốc độ 1.1x
    "message": "Hỏa Long nổi giận!"       // Thông báo hiển thị
  },
  {
    "hp_pct_threshold": 50,
    "action": "summon",
    "mob_id": 6,                          // FK → enemy.enemy_id của quái triệu hồi
    "mob_count": 2,                       // Số quái spawn
    "message": "Hỏa Long triệu hồi Lửa Thú con!"
  },
  {
    "hp_pct_threshold": 25,
    "action": "berserk",
    "damage_multiplier": 2.0,
    "speed_multiplier": 1.3,
    "skill_cooldown_multiplier": 0.5,    // Cooldown giảm còn 50%
    "message": "Hỏa Long vào trạng thái Berserk!"
  }
]
```

### Các `action` được hỗ trợ

| action | Hiệu ứng | Trường bổ sung |
|--------|----------|----------------|
| `enrage` | Tăng damage + tốc độ | `damage_multiplier`, `speed_multiplier` |
| `summon` | Spawn quái thêm | `mob_id`, `mob_count` |
| `heal` | Hồi HP % | `heal_pct` (0-100) |
| `berserk` | Tăng mạnh damage + tốc độ + giảm cooldown | `damage_multiplier`, `speed_multiplier`, `skill_cooldown_multiplier` |

> **Lưu ý**: Mỗi phase chỉ trigger **1 lần** (theo ngưỡng hp_pct_threshold giảm dần). Sắp xếp từ cao xuống thấp.

---

### Template 3 Phase chuẩn

```json
[
  {"hp_pct_threshold":75,"action":"enrage","damage_multiplier":1.2,"speed_multiplier":1.1,"message":"[Boss] nổi giận!"},
  {"hp_pct_threshold":50,"action":"summon","mob_id":0,"mob_count":2,"message":"[Boss] triệu hồi quái!"},
  {"hp_pct_threshold":25,"action":"berserk","damage_multiplier":2.0,"speed_multiplier":1.3,"skill_cooldown_multiplier":0.5,"message":"[Boss] Berserk!"}
]
```

---

## 5. Boss Spawn Config

### Bảng `boss_config`

```sql
-- Cú pháp
INSERT INTO boss_config (boss_id, map_id, spawn_x, spawn_y, min_spawn_hour, max_spawn_hour, respawn_minutes, is_active)
VALUES (
  [enemy_id],      -- FK → enemy.enemy_id (is_boss=1)
  [map_id],        -- Map boss xuất hiện
  [x], [y],        -- Tọa độ spawn (Unity world space)
  [min_hour],      -- Giờ sớm nhất boss xuất hiện (0-23)
  [max_hour],      -- Giờ muộn nhất (min_spam/hou_spam trong LangLa)
  [minutes],       -- Phút hồi sinh sau khi chết (timeDelay)
  1                -- is_active
);
```

**Ví dụ:**
```sql
-- Đế Băng: xuất hiện 0h-24h (cả ngày), hồi sinh sau 45 phút
INSERT INTO boss_config VALUES (11, 19, 10.0, 2.0, 0, 23, 45, 1);

-- Hỏa Long: chỉ xuất hiện ban ngày 9h-20h, hồi sinh sau 60 phút
INSERT INTO boss_config VALUES (8,  16, 10.0, 2.0, 9, 20, 60, 1);
```

---

## 6. Drop Rate hệ thống

### Tỉ lệ drop LangLa (Map.DropConfig tham chiếu)

| Loại drop | Tỉ lệ | Ghi chú |
|-----------|-------|---------|
| Boss → Trang bị | 5% | LangLa: `addEquipsDrop(boss, 5)` |
| Boss → Đá nâng cấp | level/7 % | VD: boss lv14 → 2% |
| Boss → Lá bài (Gene) | level/9 % | VD: boss lv14 → 1.5% |
| Mob → Đá nâng cấp | 10% | |
| Mob → Tiền | 40% | `addMoneyDrop(mob, 40)` |
| Mob → Item sự kiện | 10% | |
| Bảo vệ drop | 1 phút | Chỉ người giết được nhặt |
| Biến mất | 2 phút | Sau bảo vệ hết |

### Bảng `map_enemy_drop` — Override drop theo Map

```sql
-- Cú pháp
INSERT INTO map_enemy_drop (map_id, enemy_id, item_id, drop_chance, qty_min, qty_max, is_active)
VALUES ([map], [enemy], [item], [0.0-1.0], [min], [max], 1);

-- Ví dụ: Phòng boss lửa (map 16) - Hỏa Long (enemy 8) drop mảnh boss Fire_Essence (item 36)
INSERT INTO map_enemy_drop VALUES (16, 8, 36, 0.15, 1, 1, 1);

-- Mob lửa (enemy 7) ở tầng 1 (map 14) drop viên ngọc lửa
INSERT INTO map_enemy_drop VALUES (14, 7, 32, 0.08, 1, 2, 1);
```

### API lấy drop config

```
GET /api/dungeon/map/{mapId}/drops?enemyId={enemyId}
```

Unity có thể dùng endpoint này để hiển thị tỉ lệ drop trong UI Item Preview.

---

## 7. BossAI.cs — Hướng dẫn Setup Unity

### Yêu cầu Component

```
Boss Prefab:
├─ Rigidbody2D (Gravity Scale = 0 nếu top-down, hoặc theo game)
├─ Collider2D (body)
├─ EnemyHealth (maxHealth = base_hp từ DB)
├─ Animator (cần các Trigger: attack, enrage, berserk, heal, die)
├─ BossAI ← script chính
│   ├─ bossId: [enemy_id trong DB]
│   ├─ detectionRange: 12
│   ├─ meleeAttackRange: 2
│   ├─ chaseSpeed: 2.5
│   ├─ skillBreathPrefab: [Prefab projectile thẳng]
│   ├─ skillNovaPrefab: [Prefab AoE explosion]
│   ├─ addSpawnPrefab: [Prefab quái spawn thêm]
│   └─ phaseAnnounceText: [TextMeshPro UI]
└─ NetworkObject (nếu dùng Netcode)
```

### Animator Triggers cần tạo

| Trigger | Khi nào |
|---------|---------|
| `attack` | Melee đánh |
| `skill_breath` hoặc tên trong skill.animation_trigger | Skill thẳng |
| `skill_nova` | Skill AoE |
| `enrage` | Phase enrage |
| `berserk` | Phase berserk |
| `heal` | Phase heal |
| `die` | Chết |

### Luồng hoạt động BossAI

```
Start() → LoadConfigFromServer()
       ↓ (coroutine async)
       ParseJsonArray<SkillData>(skills_json)
       ParseJsonArray<PhaseData>(phases_json)
       _configLoaded = true

Update() (mỗi frame):
  1. CheckPhases() → scan phases[], trigger nếu HP% ≤ ngưỡng
  2. RunStateMachine():
     a. dist > detectionRange → Idle
     b. TryUseSkill() → cast nếu cooldown đã hết, trong range
     c. dist ≤ meleeAttackRange → MeleeAttack()
     d. else → ChasePlayer()
```

### Gọi từ ngoài

```csharp
// Khi player skill hit boss (ví dụ trong PlayerSkillCast.cs):
var bossHealth = hitObject.GetComponent<EnemyHealth>();
bossHealth?.TakeDamage(calculatedDamage);

// Hoặc nếu muốn tính kháng nguyên tố trên mob thường:
var mobAI = hitObject.GetComponent<MobPatrolAI>();
mobAI?.TakeDamageWithElement(rawDamage, elementId);
```

---

## 8. MobPatrolAI.cs — Hướng dẫn Setup Unity

### Yêu cầu Component

```
Mob Prefab:
├─ Rigidbody2D
├─ Collider2D (body)
├─ Collider2D (hitbox, Is Trigger = true, tắt mặc định) ← gán vào hitbox field
├─ EnemyHealth
├─ Animator (cần: isMoving bool, attack trigger, hit trigger, die trigger)
└─ MobPatrolAI
    ├─ leftPoint / rightPoint: Transform patrol biên (hoặc để tự tạo)
    ├─ moveSpeed: 2, chaseSpeed: 3
    ├─ detectionRange: 5, attackRange: 1.3
    ├─ baseDamage: [từ DB]
    ├─ Resistances: khangHoa/Thuy/Tho/Moc/Kim/Phong
    └─ Special: hpRegenPerSec, evasionRate, counterRate
```

### Cấu hình kháng nguyên tố trong Prefab

Khuyến nghị: tạo **Mob Preset ScriptableObject** cho từng loại quái:

```csharp
// MobPreset.cs (ScriptableObject)
[CreateAssetMenu(fileName = "MobPreset", menuName = "Game/MobPreset")]
public class MobPreset : ScriptableObject
{
    public int enemyId;
    public int khangHoa, khangThuy, khangTho, khangMoc, khangKim, khangPhong;
    public float hpRegenPerSec, evasionRate, counterRate;
}
```

Sau đó `MobPatrolAI.Start()` có thể load từ preset:

```csharp
public MobPreset preset;

private void Start()
{
    if (preset != null)
    {
        khangHoa = preset.khangHoa;
        // ...
    }
}
```

### API Status Effects (dùng từ PlayerSkillCast)

```csharp
// Áp stun 2 giây khi skill Thổ hit quái
var mob = hitObj.GetComponent<MobPatrolAI>();
mob?.ApplyStun(2f);

// Áp freeze 3 giây (Thủy)
mob?.ApplyFreeze(3f);

// Áp weaken (Mộc) — quái nhận thêm 30% sát thương
mob?.ApplyWeaken(5f);
```

---

## 9. Danh sách Enemy hiện tại

### Quái thường (is_boss = 0)

| enemy_id | Tên | Level | HP | Damage | Nguyên tố | Map |
|----------|-----|-------|----|----|-----------|-----|
| 1 | Slime | 1 | 30 | 5 | None | 1,2 |
| 2 | Goblin | 3 | 60 | 10 | None | 2,3 |
| 3 | Forest Wolf | 5 | 90 | 15 | Wood | 4,5 |
| 4 | Stone Golem | 8 | 150 | 20 | Earth | 5,6 |
| 5 | Shadow Bat | 6 | 75 | 12 | None | 7 |
| 6 | Lửa Thú Nhỏ | 8 | 200 | 28 | Fire | 14 |
| 7 | Lửa Thú | 9 | 300 | 35 | Fire | 14,15 |
| 9 | Băng Tinh | 12 | 320 | 38 | Water | 17 |
| 10 | Băng Chiến | 13 | 450 | 45 | Water | 17,18 |
| 12 | Mộc Thú | 10 | 280 | 30 | Wood | 20 |
| 13 | Rừng Tinh | 12 | 400 | 40 | Wood | 20,21 |
| 15 | Kim Giáp Binh | 17 | 700 | 55 | Metal | 23 |
| 16 | Hắc Giáp Binh | 18 | 900 | 65 | Metal | 23,24 |

### Boss (is_boss = 1)

| enemy_id | Tên | Level | HP | Damage | Nguyên tố | Map | Phases |
|----------|-----|-------|----|--------|-----------|-----|--------|
| 8 | Hỏa Long | 10 | 1500 | 45 | Fire | 16 | 3 phases |
| 11 | Đế Băng | 15 | 2200 | 65 | Water | 19 | 3 phases |
| 14 | Rừng Chúa | 13 | 1800 | 50 | Wood | 22 | 3 phases (+ HEAL) |
| 17 | Chúa Tể Bóng Tối | 20 | 3500 | 90 | Metal | 25 | 3 phases + 4 skills |

---

## 10. Thêm Enemy / Boss mới

### Bước 1: Insert vào DB

```sql
-- Quái thường
INSERT INTO enemy (enemy_name, level, base_hp, base_damage, base_defense,
  move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
  element_type, is_boss,
  khang_hoa, khang_thuy, khang_tho, khang_moc, khang_kim, khang_phong,
  tang_dame_hoa, tang_dame_thuy, tang_dame_tho, tang_dame_moc, tang_dame_kim, tang_dame_phong,
  hp_regen_per_sec, evasion_rate, counter_rate,
  skills_json, phases_json)
VALUES (
  'Tên Quái', [lv], [hp], [dmg], [def],
  2.5, 1.0, [exp], [gold], [silver],
  'Fire', 0,
  [khang_hoa...], [tang_dame...],
  0, 0, 0,
  NULL, NULL  -- quái thường không có skills/phases
);

-- Boss (thêm skills_json và phases_json)
INSERT INTO enemy (..., is_boss, skills_json, phases_json)
VALUES (
  'Tên Boss', ..., 1,
  '[{"skill_id":"SKILL_A","damage_multiplier":2,"cooldown_sec":8,"range":6,"aoe":false,"animation_trigger":"skill_a"}]',
  '[{"hp_pct_threshold":50,"action":"enrage","damage_multiplier":1.5,"message":"Boss nổi giận!"}]'
);
```

### Bước 2: Thêm enemy_spawn

```sql
INSERT INTO enemy_spawn (map_id, enemy_id, spawn_x, spawn_y, patrol_range, spawn_count, respawn_seconds)
VALUES ([map], [enemy_id], [x], [y], [range], [count], [seconds]);
```

### Bước 3: Tạo Prefab Unity

1. Duplicate prefab quái gần nhất
2. Sửa `EnemyHealth.maxHealth` khớp `base_hp`
3. Sửa `EnemyAI.damage` hoặc `MobPatrolAI.baseDamage` khớp `base_damage`
4. Sửa Sprite / Animator Controller
5. Nếu là Boss: thêm `BossAI`, điền `bossId`
6. Sửa resistances (khangHoa...) theo DB

### Bước 4: (Tùy chọn) Thêm drop override

```sql
INSERT INTO map_enemy_drop (map_id, enemy_id, item_id, drop_chance, qty_min, qty_max, is_active)
VALUES ([map], [enemy_id], [item_id], [0.0–1.0], 1, 1, 1);
```

---

## Ví dụ JSON đầy đủ — Boss Chúa Tể Bóng Tối

### skills_json

```json
[
  {
    "skill_id": "SHADOW_SLASH",
    "damage_multiplier": 2.0,
    "element": "Metal",
    "cooldown_sec": 6,
    "range": 5,
    "aoe": false,
    "animation_trigger": "skill_slash"
  },
  {
    "skill_id": "DARK_PULSE",
    "damage_multiplier": 1.5,
    "element": "Metal",
    "cooldown_sec": 12,
    "range": 8,
    "aoe": true,
    "animation_trigger": "skill_pulse"
  },
  {
    "skill_id": "VOID_RIFT",
    "damage_multiplier": 3.0,
    "element": "Metal",
    "cooldown_sec": 25,
    "range": 6,
    "aoe": false,
    "animation_trigger": "skill_rift",
    "status_effect": "paralyze",
    "duration_sec": 2
  },
  {
    "skill_id": "SOUL_DRAIN",
    "heal_pct": 5,
    "cooldown_sec": 30,
    "range": 7,
    "aoe": false,
    "animation_trigger": "skill_drain"
  }
]
```

### phases_json

```json
[
  {
    "hp_pct_threshold": 75,
    "action": "enrage",
    "damage_multiplier": 1.3,
    "speed_multiplier": 1.2,
    "message": "Chúa Tể Bóng Tối giải phóng sức mạnh bóng tối!"
  },
  {
    "hp_pct_threshold": 50,
    "action": "summon",
    "mob_id": 15,
    "mob_count": 3,
    "message": "Chúa Tể triệu hồi Kim Giáp Binh!"
  },
  {
    "hp_pct_threshold": 25,
    "action": "berserk",
    "damage_multiplier": 2.0,
    "speed_multiplier": 1.5,
    "skill_cooldown_multiplier": 0.4,
    "message": "Chúa Tể Bóng Tối kích hoạt Hắc Ám Không Thua!"
  }
]
```

