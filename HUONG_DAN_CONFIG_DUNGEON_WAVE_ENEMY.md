# Hướng Dẫn Config Enemy & Boss Cho Dungeon Wave

## Tổng quan luồng hoạt động

```
Wave bắt đầu
  └─ Server spawn tất cả enemy bình thường (map_spawn_config, is_boss=false)
       └─ Player giết hết enemy
            └─ Server spawn Boss (map_spawn_config, is_boss=true)
                 └─ Player giết Boss
                      └─ Sang Wave mới
                           ├─ Enemy stats tăng theo enemy_scale_percent
                           ├─ Boss stats tăng theo boss_scale_percent
                           └─ Lặp lại đến wave max_waves
```

---

## Có 2 nơi cần config

| Thứ cần cấu hình | Bảng DB | Ghi chú |
|---|---|---|
| Loại enemy/boss, vị trí spawn | `map_spawn_config` (spawn_json) | Chọn enemy_id nào spawn ở đâu |
| Số vòng, thời gian, % tăng chỉ số | `dungeon_wave_config` | Điều chỉnh độ khó tổng thể |
| Chỉ số gốc của enemy/boss | bảng `enemy` | Thay đổi HP/ATK/DEF/EXP/Gold |

---

## 1. Config Enemy & Boss Spawn (vị trí + loại)

Enemy và boss spawn được lưu trong bảng `map_spawn_config`, cột `spawn_json`, với trường `is_boss` phân biệt hai loại.

### Cấu trúc spawn_json

```json
{
  "spawns": [
    {
      "enemy_id": 12,
      "x": 10.5,
      "y": 3.2,
      "is_boss": false
    },
    {
      "enemy_id": 12,
      "x": -8.0,
      "y": 3.2,
      "is_boss": false
    },
    {
      "enemy_id": 11,
      "x": 0.0,
      "y": 3.2,
      "is_boss": true
    }
  ]
}
```

- `enemy_id` = ID trong bảng `enemy` (ví dụ: 12 = Mộc Linh thường, 11 = Đế Băng boss)
- `x`, `y` = vị trí spawn trong scene Unity (tọa độ world)
- `is_boss = false` → enemy thường, spawn đầu mỗi wave
- `is_boss = true` → boss, spawn sau khi giết hết enemy thường

### SQL thêm/sửa enemy spawn cho dungeon map 110

```sql
-- Xem spawn hiện tại
SELECT spawn_json FROM map_spawn_config WHERE map_id = 110;

-- Thêm 1 enemy thường vào danh sách (ví dụ thêm enemy_id=12 tại x=15, y=3.2)
UPDATE map_spawn_config
SET spawn_json = JSON_SET(
    spawn_json,
    '$.spawns[#]',
    JSON_OBJECT('enemy_id', 12, 'x', 15.0, 'y', 3.2, 'is_boss', false)
)
WHERE map_id = 110;

-- Hoặc thay toàn bộ spawn_json bằng một cấu hình mới hoàn toàn:
UPDATE map_spawn_config
SET spawn_json = '{
  "spawns": [
    {"enemy_id": 12, "x": 10.5,  "y": 3.2, "is_boss": false},
    {"enemy_id": 12, "x": -10.5, "y": 3.2, "is_boss": false},
    {"enemy_id": 12, "x": 0.0,   "y": 5.0, "is_boss": false},
    {"enemy_id": 11, "x": 0.0,   "y": 3.2, "is_boss": true}
  ]
}'
WHERE map_id = 110;
```

> **Lưu ý**: `map_id = 110` là map của dungeon wave (dungeon_id = 6 "Phó Bản Sóng"). Xem mapping trong bảng `dungeon_config`.

---

## 2. Config Thông Số Flow Dungeon Wave

Bảng `dungeon_wave_config` kiểm soát số vòng, thời gian, và % tăng chỉ số theo vòng.

### Schema bảng

| Cột | Ý nghĩa | Mặc định |
|---|---|---|
| `dungeon_id` | ID dungeon (khóa chính) | — |
| `max_waves` | Tổng số vòng tối đa | 20 |
| `wave_time_seconds` | Thời gian mỗi vòng (giây) | 300 |
| `enemy_scale_percent` | % tăng chỉ số enemy mỗi vòng (cộng dồn) | 10 |
| `boss_scale_percent` | % tăng chỉ số boss mỗi vòng | 15 |
| `exp_gold_scale_percent` | % tăng EXP/Gold mỗi vòng | 10 |
| `daily_entry_limit` | Số lần vào trong ngày | 1 |
| `entry_item_plus1_id` | item_template_id của vé +1 lần | 409 |
| `entry_item_plus2_id` | item_template_id của vé +2 lần | 410 |
| `milestone_reward_json` | Phần thưởng mốc vòng (JSON) | {} |

### SQL sửa thông số

```sql
-- Xem config hiện tại
SELECT * FROM dungeon_wave_config WHERE dungeon_id = 6;

-- Thay đổi số vòng, thời gian và hệ số tăng
UPDATE dungeon_wave_config
SET
    max_waves            = 30,       -- tăng lên 30 vòng
    wave_time_seconds    = 240,      -- 4 phút mỗi vòng
    enemy_scale_percent  = 8.0,      -- enemy tăng 8% mỗi vòng
    boss_scale_percent   = 12.0,     -- boss tăng 12% mỗi vòng
    exp_gold_scale_percent = 8.0     -- EXP/Gold tăng 8% mỗi vòng
WHERE dungeon_id = 6;

-- Thêm mới nếu chưa có
INSERT INTO dungeon_wave_config
    (dungeon_id, max_waves, wave_time_seconds, enemy_scale_percent, boss_scale_percent, exp_gold_scale_percent, daily_entry_limit)
VALUES
    (6, 20, 300, 10.0, 15.0, 10.0, 1)
ON DUPLICATE KEY UPDATE
    max_waves = VALUES(max_waves),
    wave_time_seconds = VALUES(wave_time_seconds);
```

---

## 3. Config Chỉ Số Gốc Enemy/Boss

Chỉ số HP/ATK/DEF/EXP/Gold của enemy thay đổi theo vòng được tính từ chỉ số gốc trong bảng `enemy`.

```sql
-- Xem chỉ số enemy thường (id=12) và boss (id=11)
SELECT enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
       exp_reward, gold_reward
FROM enemy
WHERE enemy_id IN (11, 12);

-- Sửa chỉ số gốc boss
UPDATE enemy
SET base_hp = 50000, base_damage = 800, base_defense = 300, exp_reward = 5000, gold_reward = 2000
WHERE enemy_id = 11;

-- Sửa chỉ số gốc enemy thường
UPDATE enemy
SET base_hp = 8000, base_damage = 200, base_defense = 80, exp_reward = 500, gold_reward = 150
WHERE enemy_id = 12;
```

---

## 4. Công Thức Tính Chỉ Số Theo Vòng

Server tính chỉ số thực tế của enemy/boss ở vòng N theo công thức cộng dồn:

```
enemy_scale_factor = 1 + (enemy_scale_percent / 100) * (wave - 1)
boss_scale_factor  = 1 + (boss_scale_percent / 100)  * (wave - 1)

HP_enemy_at_wave_N  = base_hp  * enemy_scale_factor
ATK_enemy_at_wave_N = base_damage * enemy_scale_factor
...
HP_boss_at_wave_N   = base_hp  * boss_scale_factor  (boss lấy từ bảng enemy)
...
```

**Ví dụ** với `enemy_scale_percent = 10`, `base_hp = 8000`:
- Vòng 1: 8000 HP
- Vòng 2: 8800 HP (+10%)
- Vòng 5: 12000 HP (+50%)
- Vòng 10: 16000 HP (+100%)

---

## 5. Mapping Dungeon → Map → Config

```sql
-- Xem dungeon_id nào dùng map nào
SELECT dungeon_id, dungeon_name, map_id, scene_name
FROM dungeon_config
WHERE dungeon_type = 'wave';

-- Kết quả ví dụ:
-- dungeon_id=6, dungeon_name="Phó Bản Sóng", map_id=110, scene_name="DungeonWaveScene"
```

Vậy để config enemy cho dungeon_id=6:
- Sửa **spawn** → `map_spawn_config` WHERE `map_id = 110`
- Sửa **flow** → `dungeon_wave_config` WHERE `dungeon_id = 6`

---

## 6. API Endpoint (dùng để debug/verify)

```
GET /api/dungeon/wave/{dungeonId}/config
```

Trả về JSON đầy đủ mà Unity client đọc khi vào dungeon:

```json
{
  "dungeon_id": 6,
  "map_id": 110,
  "max_waves": 20,
  "wave_time_seconds": 300,
  "enemy_scale_percent": 10.0,
  "boss_scale_percent": 15.0,
  "exp_gold_scale_percent": 10.0,
  "enemy_spawns": [
    { "enemy_id": 12, "display_name": "Mộc Linh", "spawn_x": 10.5, "spawn_y": 3.2, "base_hp": 8000 }
  ],
  "boss_spawn": {
    "enemy_id": 11, "display_name": "Đế Băng", "spawn_x": 0.0, "spawn_y": 3.2, "base_hp": 30000
  }
}
```

---

## 7. Checklist Khi Thêm Enemy/Boss Mới

- [ ] Thêm bản ghi trong bảng `enemy` với `enemy_type = 'Normal'` hoặc `'Boss'`
- [ ] Thêm entry trong `skills_json` của enemy đó nếu cần kỹ năng
- [ ] Nếu là boss: thêm bản ghi trong `boss_config` (tọa độ spawn thế giới mở, nếu có)
- [ ] Thêm vào `spawn_json` của `map_spawn_config` với `map_id` tương ứng
- [ ] Verify qua API: `GET /api/dungeon/wave/6/config` và kiểm tra `enemy_spawns` + `boss_spawn`
- [ ] Đảm bảo Unity có Prefab enemy với đúng `enemyId` trong `NetworkEnemySpawner` prefab mapping

---

## 8. File SQL Tham Khảo

| File | Nội dung |
|---|---|
| `GameServerApi/sql/040_dungeon_wave.sql` | Schema + seed dữ liệu ban đầu |
| `GameServerApi/sql/041_fix_dungeon_wave_map110_spawn_ids.sql` | Fix enemy_id đúng cho map 110 (27 enemy + 1 boss) |
