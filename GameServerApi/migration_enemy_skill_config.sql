-- ============================================================
--  Migration: enemy_skill_config
--  Mở rộng cột skills_json trong bảng enemy để dùng cho
--  TẤT CẢ quái (không chỉ boss), đồng thời seed dữ liệu 10 enemy
--  mẫu kèm skill JSON đầy đủ.
--
--  Yêu cầu:
--    - Bảng enemy đã tồn tại (đã chạy migration_map_spawn_config.sql
--      hoặc DB được tạo từ GameDbContext)
--    - Chạy file này MỘT LẦN duy nhất.
--
--  Các cột liên quan đến skill (đã có sẵn từ entity, kiểm tra lại):
--    skills_json    LONGTEXT NULL   — JSON array của tất cả skill
--    element_type   VARCHAR(20) NULL — nguyên tố của quái
--    base_damage    INT NOT NULL    — sát thương cơ bản (dùng khi skill chỉ có multiplier)
-- ============================================================

-- ────────────────────────────────────────────────────────────
--  Bước 1: Đảm bảo cột skills_json tồn tại và cập nhật COMMENT
--  (ALTER TABLE ... MODIFY không fail nếu cột đã có đúng kiểu)
-- ────────────────────────────────────────────────────────────

ALTER TABLE enemy
    MODIFY COLUMN skills_json LONGTEXT NULL
    COMMENT 'JSON array skill của quái (áp dụng cả quái thường và boss).
Mỗi phần tử:
{
  "skill_id"          : "FIRE_BREATH",   -- ID không dấu cách, dùng cho cooldown & animation
  "flat_damage"       : 0,               -- damage tuyệt đối (>0 = dùng trực tiếp)
  "damage_multiplier" : 2.5,             -- hệ số × base_damage (chỉ dùng khi flat_damage=0)
  "element"           : "Fire",          -- nguyên tố skill (có thể khác element_type quái)
  "cooldown_sec"      : 8.0,             -- giây hồi chiêu
  "range"             : 5.0,             -- tầm đánh (Unity units)
  "aoe"               : false,           -- true = tấn công diện
  "aoe_radius"        : 3.0,             -- bán kính AoE (chỉ dùng khi aoe=true)
  "animation_trigger" : "skill_fb",      -- tên Animator trigger, rỗng = không animation riêng
  "status_effect"     : "Burn",          -- hiệu ứng trạng thái (rỗng = không có)
  "duration_sec"      : 3.0,             -- thời gian duy trì status_effect
  "spawn_enemy_id"    : 0,               -- ID quái triệu hồi (skill SUMMON_ADD)
  "spawn_count"       : 0                -- số lượng quái triệu hồi
}';

-- ────────────────────────────────────────────────────────────
--  Bước 2: Đảm bảo cột element_type và base_damage tồn tại
-- ────────────────────────────────────────────────────────────

ALTER TABLE enemy
    MODIFY COLUMN element_type VARCHAR(20) NULL
    COMMENT 'Nguyên tố chính: Fire/Water/Earth/Metal/Wood/Wind/None';

ALTER TABLE enemy
    MODIFY COLUMN base_damage INT NOT NULL DEFAULT 5
    COMMENT 'Sát thương cơ bản melee — làm cơ sở tính damage khi skill dùng damage_multiplier';

-- ────────────────────────────────────────────────────────────
--  Bước 3: Seed / cập nhật 10 enemy mẫu
--
--  Chiến lược:
--    ON DUPLICATE KEY UPDATE → nếu enemy đã tồn tại, chỉ cập nhật
--    skills_json, element_type, base_damage (không đụng stats khác).
--    Nếu enemy chưa tồn tại → insert row đầy đủ.
--
--  Map seed (từ migration_map_spawn_config.sql):
--    Map 0 (Làng Khởi Đầu)  : enemy 1, 2, 4
--    Map 1 (Cánh Đồng Lửa)  : enemy 5, 6, 8
--    Map 2 (Rừng Băng)       : enemy 3, 7, 9
--    Map 3 (Sa Mạc Phong)    : enemy 1, 3, 5, 10
-- ────────────────────────────────────────────────────────────

-- ─── Enemy 1: Slime (Water) ───────────────────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(1, 'Slime', 1, 120, 0, 8, 0, 2.0, 1.0, 30, 5, 20,
 'Water', 'Normal',
 '[
   {
     "skill_id"          : "WATER_BURST",
     "flat_damage"       : 0,
     "damage_multiplier" : 1.5,
     "element"           : "Water",
     "cooldown_sec"      : 8.0,
     "range"             : 3.0,
     "aoe"               : true,
     "aoe_radius"        : 2.0,
     "animation_trigger" : "skill_waterBurst",
     "status_effect"     : "Slow",
     "duration_sec"      : 2.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 2: Goblin (Earth) ──────────────────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(2, 'Goblin', 1, 80, 0, 12, 2, 2.5, 1.0, 20, 3, 10,
 'Earth', 'Normal',
 '[
   {
     "skill_id"          : "DIRT_THROW",
     "flat_damage"       : 15,
     "damage_multiplier" : 0.0,
     "element"           : "Earth",
     "cooldown_sec"      : 5.0,
     "range"             : 4.0,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_dirtThrow",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 3: Ice Wolf (Water) ────────────────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(3, 'Ice Wolf', 5, 200, 0, 20, 5, 3.0, 1.2, 60, 10, 30,
 'Water', 'Normal',
 '[
   {
     "skill_id"          : "ICE_BITE",
     "flat_damage"       : 25,
     "damage_multiplier" : 0.0,
     "element"           : "Water",
     "cooldown_sec"      : 4.0,
     "range"             : 1.5,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_iceBite",
     "status_effect"     : "Freeze",
     "duration_sec"      : 2.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "ICE_HOWL",
     "flat_damage"       : 0,
     "damage_multiplier" : 2.0,
     "element"           : "Water",
     "cooldown_sec"      : 12.0,
     "range"             : 3.0,
     "aoe"               : true,
     "aoe_radius"        : 3.0,
     "animation_trigger" : "skill_iceHowl",
     "status_effect"     : "Slow",
     "duration_sec"      : 3.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 4: Goblin Chief (Earth, Boss) ──────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(4, 'Goblin Chief', 5, 800, 0, 35, 10, 2.0, 0.8, 200, 50, 100,
 'Earth', 'Boss',
 '[
   {
     "skill_id"          : "EARTH_SLAM",
     "flat_damage"       : 0,
     "damage_multiplier" : 3.0,
     "element"           : "Earth",
     "cooldown_sec"      : 8.0,
     "range"             : 2.0,
     "aoe"               : true,
     "aoe_radius"        : 2.5,
     "animation_trigger" : "skill_earthSlam",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "CHARGE",
     "flat_damage"       : 80,
     "damage_multiplier" : 0.0,
     "element"           : "None",
     "cooldown_sec"      : 15.0,
     "range"             : 6.0,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_charge",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "SUMMON_ADD",
     "flat_damage"       : 0,
     "damage_multiplier" : 0.0,
     "element"           : "None",
     "cooldown_sec"      : 25.0,
     "range"             : 5.0,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_summon",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 2,
     "spawn_count"       : 3
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 5: Fire Slime (Fire) ───────────────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(5, 'Fire Slime', 8, 300, 0, 22, 3, 2.0, 1.0, 80, 12, 40,
 'Fire', 'Normal',
 '[
   {
     "skill_id"          : "FIRE_BURST",
     "flat_damage"       : 0,
     "damage_multiplier" : 2.0,
     "element"           : "Fire",
     "cooldown_sec"      : 7.0,
     "range"             : 3.0,
     "aoe"               : true,
     "aoe_radius"        : 2.0,
     "animation_trigger" : "skill_fireBurst",
     "status_effect"     : "Burn",
     "duration_sec"      : 3.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 6: Goblin Archer (Earth) ───────────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(6, 'Goblin Archer', 8, 200, 0, 18, 3, 2.5, 1.0, 60, 8, 25,
 'Earth', 'Normal',
 '[
   {
     "skill_id"          : "QUICK_SHOT",
     "flat_damage"       : 25,
     "damage_multiplier" : 0.0,
     "element"           : "Earth",
     "cooldown_sec"      : 3.0,
     "range"             : 7.0,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_quickShot",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "ARROW_RAIN",
     "flat_damage"       : 0,
     "damage_multiplier" : 2.5,
     "element"           : "Earth",
     "cooldown_sec"      : 14.0,
     "range"             : 8.0,
     "aoe"               : true,
     "aoe_radius"        : 3.5,
     "animation_trigger" : "skill_arrowRain",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 7: Snow Goblin (Water) ─────────────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(7, 'Snow Goblin', 10, 220, 0, 18, 5, 2.5, 1.0, 65, 8, 25,
 'Water', 'Normal',
 '[
   {
     "skill_id"          : "ICE_SHARD",
     "flat_damage"       : 30,
     "damage_multiplier" : 0.0,
     "element"           : "Water",
     "cooldown_sec"      : 5.0,
     "range"             : 5.0,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_iceShard",
     "status_effect"     : "Slow",
     "duration_sec"      : 2.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 8: Fire Dragon (Fire, Boss) ────────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(8, 'Fire Dragon', 15, 3000, 0, 60, 20, 2.0, 0.8, 800, 200, 500,
 'Fire', 'Boss',
 '[
   {
     "skill_id"          : "FIRE_BREATH",
     "flat_damage"       : 0,
     "damage_multiplier" : 3.5,
     "element"           : "Fire",
     "cooldown_sec"      : 8.0,
     "range"             : 5.0,
     "aoe"               : true,
     "aoe_radius"        : 4.0,
     "animation_trigger" : "skill_fireBreath",
     "status_effect"     : "Burn",
     "duration_sec"      : 4.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "WING_SLAM",
     "flat_damage"       : 150,
     "damage_multiplier" : 0.0,
     "element"           : "None",
     "cooldown_sec"      : 12.0,
     "range"             : 3.0,
     "aoe"               : true,
     "aoe_radius"        : 3.0,
     "animation_trigger" : "skill_wingSlam",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "SUMMON_ADD",
     "flat_damage"       : 0,
     "damage_multiplier" : 0.0,
     "element"           : "None",
     "cooldown_sec"      : 30.0,
     "range"             : 5.0,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_dragonCall",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 5,
     "spawn_count"       : 2
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 9: Ice Witch (Water, Boss) ─────────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(9, 'Ice Witch', 15, 2500, 0, 50, 15, 1.8, 0.9, 600, 150, 400,
 'Water', 'Boss',
 '[
   {
     "skill_id"          : "BLIZZARD",
     "flat_damage"       : 0,
     "damage_multiplier" : 2.5,
     "element"           : "Water",
     "cooldown_sec"      : 15.0,
     "range"             : 6.0,
     "aoe"               : true,
     "aoe_radius"        : 5.0,
     "animation_trigger" : "skill_blizzard",
     "status_effect"     : "Freeze",
     "duration_sec"      : 3.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "ICE_LANCE",
     "flat_damage"       : 120,
     "damage_multiplier" : 0.0,
     "element"           : "Water",
     "cooldown_sec"      : 5.0,
     "range"             : 8.0,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_iceLance",
     "status_effect"     : "Slow",
     "duration_sec"      : 2.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ─── Enemy 10: Final Dragon (Fire, Boss) ──────────────────
INSERT INTO enemy
    (enemy_id, enemy_name, level, base_hp, base_mp, base_damage, base_defense,
     move_speed, attack_speed, exp_reward, gold_reward, silver_reward,
     element_type, enemy_type, skills_json, created_at, updated_at)
VALUES
(10, 'Final Dragon', 25, 8000, 0, 100, 30, 2.0, 0.7, 2000, 500, 1000,
 'Fire', 'Boss',
 '[
   {
     "skill_id"          : "MULTI_BREATH",
     "flat_damage"       : 0,
     "damage_multiplier" : 4.0,
     "element"           : "Fire",
     "cooldown_sec"      : 10.0,
     "range"             : 6.0,
     "aoe"               : true,
     "aoe_radius"        : 5.0,
     "animation_trigger" : "skill_multiBreath",
     "status_effect"     : "Burn",
     "duration_sec"      : 4.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "WING_STORM",
     "flat_damage"       : 0,
     "damage_multiplier" : 2.5,
     "element"           : "Wind",
     "cooldown_sec"      : 15.0,
     "range"             : 4.0,
     "aoe"               : true,
     "aoe_radius"        : 6.0,
     "animation_trigger" : "skill_wingStorm",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 0,
     "spawn_count"       : 0
   },
   {
     "skill_id"          : "SUMMON_ADD",
     "flat_damage"       : 0,
     "damage_multiplier" : 0.0,
     "element"           : "None",
     "cooldown_sec"      : 40.0,
     "range"             : 5.0,
     "aoe"               : false,
     "aoe_radius"        : 0.0,
     "animation_trigger" : "skill_dragonSummon",
     "status_effect"     : "",
     "duration_sec"      : 0.0,
     "spawn_enemy_id"    : 8,
     "spawn_count"       : 1
   }
 ]',
 NOW(), NOW())
ON DUPLICATE KEY UPDATE
    skills_json  = VALUES(skills_json),
    element_type = VALUES(element_type),
    base_damage  = VALUES(base_damage),
    updated_at   = NOW();

-- ────────────────────────────────────────────────────────────
--  Kiểm tra kết quả
-- ────────────────────────────────────────────────────────────

SELECT
    enemy_id,
    enemy_name,
    element_type,
    base_damage,
    CASE WHEN skills_json IS NOT NULL AND skills_json != '' THEN 'OK' ELSE 'MISSING' END AS skill_status,
    JSON_LENGTH(skills_json) AS skill_count
FROM enemy
WHERE enemy_id BETWEEN 1 AND 10
ORDER BY enemy_id;
