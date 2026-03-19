-- ============================================================
-- Migration: Map/Dungeon Portal System + Boss/Enemy Enhancement
-- Áp dụng sau migration_npc_system.sql
-- ============================================================

-- ============================================================
-- 1. THÊM CỘT MỚI VÀO BẢNG enemy
--    (Kháng nguyên tố, tăng sát thương, kỹ năng boss, giai đoạn boss)
-- ============================================================
ALTER TABLE `enemy`
  ADD COLUMN IF NOT EXISTS `silver_reward`     int(11)    NOT NULL DEFAULT 20    COMMENT 'Thưởng bạc khi tiêu diệt' AFTER `gold_reward`,
  ADD COLUMN IF NOT EXISTS `khang_hoa`         int(11)    NOT NULL DEFAULT 0     COMMENT 'Kháng Hỏa % (0-100)',
  ADD COLUMN IF NOT EXISTS `khang_thuy`        int(11)    NOT NULL DEFAULT 0     COMMENT 'Kháng Thủy %',
  ADD COLUMN IF NOT EXISTS `khang_tho`         int(11)    NOT NULL DEFAULT 0     COMMENT 'Kháng Thổ %',
  ADD COLUMN IF NOT EXISTS `khang_moc`         int(11)    NOT NULL DEFAULT 0     COMMENT 'Kháng Mộc %',
  ADD COLUMN IF NOT EXISTS `khang_kim`         int(11)    NOT NULL DEFAULT 0     COMMENT 'Kháng Kim %',
  ADD COLUMN IF NOT EXISTS `khang_phong`       int(11)    NOT NULL DEFAULT 0     COMMENT 'Kháng Phong %',
  ADD COLUMN IF NOT EXISTS `tang_dame_hoa`     int(11)    NOT NULL DEFAULT 0     COMMENT 'Tăng sát thương vs nhân vật Hỏa %',
  ADD COLUMN IF NOT EXISTS `tang_dame_thuy`    int(11)    NOT NULL DEFAULT 0     COMMENT 'Tăng sát thương vs nhân vật Thủy %',
  ADD COLUMN IF NOT EXISTS `tang_dame_tho`     int(11)    NOT NULL DEFAULT 0     COMMENT 'Tăng sát thương vs nhân vật Thổ %',
  ADD COLUMN IF NOT EXISTS `tang_dame_moc`     int(11)    NOT NULL DEFAULT 0     COMMENT 'Tăng sát thương vs nhân vật Mộc %',
  ADD COLUMN IF NOT EXISTS `tang_dame_kim`     int(11)    NOT NULL DEFAULT 0     COMMENT 'Tăng sát thương vs nhân vật Kim %',
  ADD COLUMN IF NOT EXISTS `tang_dame_phong`   int(11)    NOT NULL DEFAULT 0     COMMENT 'Tăng sát thương vs nhân vật Phong %',
  ADD COLUMN IF NOT EXISTS `hp_regen_per_sec`  int(11)    NOT NULL DEFAULT 0     COMMENT 'HP hồi phục mỗi giây',
  ADD COLUMN IF NOT EXISTS `evasion_rate`      int(11)    NOT NULL DEFAULT 0     COMMENT 'Tỉ lệ né đòn % (0-100)',
  ADD COLUMN IF NOT EXISTS `counter_rate`      int(11)    NOT NULL DEFAULT 0     COMMENT 'Tỉ lệ phản đòn % (0-100)',
  ADD COLUMN IF NOT EXISTS `skills_json`       longtext   DEFAULT NULL           COMMENT 'JSON mảng kỹ năng boss (chỉ dùng với EnemyType=Boss)',
  ADD COLUMN IF NOT EXISTS `phases_json`       longtext   DEFAULT NULL           COMMENT 'JSON các giai đoạn boss theo %HP';

-- ============================================================
-- 2. BẢNG map_portal — Cổng dịch chuyển giữa các map/phòng phó bản
--
--  Tương tự WayPoint trong LangLa (dataWayPoint[]):
--   mapHere  → source_map_id
--   mapNext  → dest_map_id
--   l,m,n,o  → src_x,src_y + src_radius  (vùng trigger)
--   p,q      → dest_x, dest_y  (điểm đến)
-- ============================================================
CREATE TABLE IF NOT EXISTS `map_portal` (
  `portal_id`        int(11)     NOT NULL AUTO_INCREMENT,
  `portal_name`      varchar(100) NOT NULL               COMMENT 'Tên hiển thị (e.g. "Cửa vào Tầng 2")',
  `source_map_id`    int(11)     NOT NULL                COMMENT 'Map chứa cổng này',
  `src_x`            float       NOT NULL DEFAULT 0      COMMENT 'Tọa độ X trung tâm cổng (Unity scene)',
  `src_y`            float       NOT NULL DEFAULT 0      COMMENT 'Tọa độ Y trung tâm cổng',
  `src_radius`       float       NOT NULL DEFAULT 2.0    COMMENT 'Bán kính vùng trigger (server validation)',
  `dest_map_id`      int(11)     NOT NULL                COMMENT 'Map đích',
  `dest_scene_name`  varchar(100) NOT NULL DEFAULT ''    COMMENT 'Tên Unity Scene cần load',
  `dest_x`           float       NOT NULL DEFAULT 0      COMMENT 'Tọa độ X điểm đến trong map đích',
  `dest_y`           float       NOT NULL DEFAULT 0      COMMENT 'Tọa độ Y điểm đến',
  `portal_type`      enum('enter_dungeon','room_transition','exit_dungeon','world_travel')
                                 NOT NULL DEFAULT 'room_transition',
  `required_item_id` int(11)     DEFAULT NULL            COMMENT 'Cần item này trong túi đồ (NULL = tự do)',
  `dungeon_id`       int(11)     DEFAULT NULL            COMMENT 'Phó bản sở hữu cổng này (NULL = open world)',
  `is_active`        tinyint(1)  NOT NULL DEFAULT 1,
  PRIMARY KEY (`portal_id`),
  KEY `idx_portal_src`  (`source_map_id`),
  KEY `idx_portal_dest` (`dest_map_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ============================================================
-- 3. BẢNG boss_config — Cấu hình spawn & lịch trình Boss
--
--  Tương tự BossTpl.java trong LangLa:
--   cx/cy     → spawn_x / spawn_y
--   map       → map_id
--   min_spam  → min_spawn_hour (giờ spawn sớm nhất)
--   hou_spam  → max_spawn_hour
--   timeDelay → respawn_minutes
-- ============================================================
CREATE TABLE IF NOT EXISTS `boss_config` (
  `boss_id`           int(11)    NOT NULL  COMMENT 'FK → enemy.enemy_id (Boss)',
  `map_id`            int(11)    NOT NULL  COMMENT 'Map boss spawn',
  `spawn_x`           float      NOT NULL DEFAULT 0,
  `spawn_y`           float      NOT NULL DEFAULT 0,
  `min_spawn_hour`    int(11)    NOT NULL DEFAULT 0   COMMENT 'Giờ sớm nhất spawn (0-23)',
  `max_spawn_hour`    int(11)    NOT NULL DEFAULT 23  COMMENT 'Giờ muộn nhất spawn',
  `respawn_minutes`   int(11)    NOT NULL DEFAULT 60  COMMENT 'Thời gian hồi sinh (phút)',
  `is_active`         tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`boss_id`),
  KEY `idx_boss_map` (`map_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ============================================================
-- 4. BẢNG map_enemy_drop — Tỉ lệ drop riêng theo map
--
--  Tương tự BossDropConfig trong LangLa
--  Ghi đè drop_items_json mặc định của enemy khi ở map cụ thể
-- ============================================================
CREATE TABLE IF NOT EXISTS `map_enemy_drop` (
  `id`          int(11)    NOT NULL AUTO_INCREMENT,
  `map_id`      int(11)    NOT NULL  COMMENT 'Map áp dụng tỉ lệ này',
  `enemy_id`    int(11)    NOT NULL  COMMENT 'FK → enemy.enemy_id',
  `item_id`     int(11)    NOT NULL  COMMENT 'FK → item_template.id',
  `drop_chance` float      NOT NULL DEFAULT 0.1  COMMENT 'Tỉ lệ rơi (0.0-1.0)',
  `qty_min`     int(11)    NOT NULL DEFAULT 1,
  `qty_max`     int(11)    NOT NULL DEFAULT 1,
  `is_active`   tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_map_enemy_item` (`map_id`, `enemy_id`, `item_id`),
  KEY `idx_med_map_enemy` (`map_id`, `enemy_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- ============================================================
-- 5. THÊM MÁP PHÒNG PHÓ BẢN (sub-rooms)
--    Mỗi dungeon có 3 phòng: Tầng 1 → Tầng 2 → Phòng Boss
-- ============================================================
INSERT IGNORE INTO `map_config` (`map_id`, `map_name`, `scene_name`, `spawn_points_json`, `min_level`, `max_level`) VALUES
-- Hang Động Lửa (dungeon_id=1, entry=map 10)
(14, 'Hang Động Lửa - Tầng 2',       'DungeonScene_FireCave_Floor2',  '[{"x":-8,"y":0}]',  5, 999),
(15, 'Hang Động Lửa - Phòng Boss',    'DungeonScene_FireCave_Boss',    '[{"x":-10,"y":0}]', 5, 999),
-- Tháp Băng Giá (dungeon_id=2, entry=map 11)
(16, 'Tháp Băng Giá - Tầng 2',       'DungeonScene_IceTower_Floor2',  '[{"x":-8,"y":0}]',  10, 999),
(17, 'Tháp Băng Giá - Phòng Boss',    'DungeonScene_IceTower_Boss',    '[{"x":-10,"y":0}]', 10, 999),
-- Mê Cung Rừng Rậm (dungeon_id=3, entry=map 12)
(18, 'Mê Cung Rừng Rậm - Tầng 2',    'DungeonScene_Forest_Floor2',    '[{"x":-8,"y":0}]',  8, 999),
(19, 'Mê Cung Rừng Rậm - Phòng Boss','DungeonScene_Forest_Boss',      '[{"x":-10,"y":0}]', 8, 999),
-- Thành Trì Bóng Tối (dungeon_id=4, entry=map 13)
(20, 'Thành Trì Bóng Tối - Tầng 2',  'DungeonScene_DarkCastle_Floor2','[{"x":-8,"y":0}]',  15, 999),
(21, 'Thành Trì Bóng Tối - Tầng 3',  'DungeonScene_DarkCastle_Floor3','[{"x":-8,"y":0}]',  15, 999),
(22, 'Thành Trì Bóng Tối - Phòng Boss','DungeonScene_DarkCastle_Boss', '[{"x":-10,"y":0}]', 15, 999);

-- ============================================================
-- 6. THÊM ENEMY MỚI CHO PHÓ BẢN
-- ============================================================
INSERT IGNORE INTO `enemy`
  (`enemy_id`,`enemy_name`,`enemy_description`,`level`,`base_hp`,`base_mp`,`base_damage`,`base_defense`,
   `move_speed`,`attack_speed`,`exp_reward`,`gold_reward`,`silver_reward`,`drop_items_json`,
   `element_type`,`enemy_type`,
   `khang_hoa`,`khang_thuy`,`khang_tho`,`khang_moc`,`khang_kim`,`khang_phong`,
   `tang_dame_hoa`,`tang_dame_thuy`,`tang_dame_tho`,`tang_dame_moc`,`tang_dame_kim`,`tang_dame_phong`,
   `hp_regen_per_sec`,`evasion_rate`,`counter_rate`,
   `skills_json`,`phases_json`) VALUES

-- ── Hang Động Lửa mobs ──
(6, 'Hỏa Linh', 'Tinh linh lửa nhỏ, nhanh nhẹn', 5, 120, 20, 12, 3, 2.5, 1.2, 25, 12, 50,
 '[{"item_id":30,"drop_chance":0.4,"qty_min":1,"qty_max":2},{"item_id":11,"drop_chance":0.2,"qty_min":1,"qty_max":1}]',
 'Fire','Normal', 50,0,0,0,0,0, 20,0,0,0,0,0, 0,5,0, NULL, NULL),

(7, 'Hỏa Quỷ', 'Quỷ lửa tinh nhuệ, kiểm soát ngọn lửa', 8, 280, 60, 25, 8, 2, 1, 80, 35, 140,
 '[{"item_id":30,"drop_chance":0.5,"qty_min":1,"qty_max":3},{"item_id":21,"drop_chance":0.08,"qty_min":1,"qty_max":1}]',
 'Fire','Elite', 60,0,0,0,0,0, 30,0,0,0,0,0, 2,10,5, NULL, NULL),

(8, 'Hỏa Long', 'Rồng lửa thống lĩnh Hang Động', 10, 1500, 300, 90, 25, 2.5, 1.5, 600, 250, 1000,
 '[{"item_id":36,"drop_chance":0.5,"qty_min":1,"qty_max":2},{"item_id":203,"drop_chance":0.1,"qty_min":1,"qty_max":1},{"item_id":31,"drop_chance":0.05,"qty_min":1,"qty_max":1}]',
 'Fire','Boss', 70,0,0,0,0,0, 40,0,0,0,0,0, 5,15,10,
 '[{"skill_id":"FIRE_BREATH","damage_multiplier":2.5,"element":"Fire","cooldown_sec":8,"range":6,"aoe":false,"animation_trigger":"skill_breath"},{"skill_id":"FLAME_NOVA","damage_multiplier":1.5,"element":"Fire","cooldown_sec":12,"range":8,"aoe":true,"animation_trigger":"skill_nova"},{"skill_id":"SUMMON_ADD","spawn_enemy_id":6,"spawn_count":2,"cooldown_sec":20,"animation_trigger":"skill_summon"}]',
 '[{"hp_pct_threshold":75,"action":"enrage","damage_multiplier":1.2,"speed_multiplier":1.1,"message":"Hỏa Long điên cuồng!"},{"hp_pct_threshold":50,"action":"summon","mob_id":6,"mob_count":2,"message":"Hỏa Long triệu tập hậu vệ!"},{"hp_pct_threshold":25,"action":"berserk","damage_multiplier":2.0,"speed_multiplier":1.3,"skill_cooldown_multiplier":0.5,"message":"Hỏa Long bước vào cuồng bạo!"}]'),

-- ── Tháp Băng Giá mobs ──
(9, 'Băng Linh', 'Tinh linh băng lạnh giá', 10, 180, 40, 20, 5, 2, 1.1, 45, 20, 80,
 '[{"item_id":22,"drop_chance":0.35,"qty_min":1,"qty_max":2},{"item_id":26,"drop_chance":0.25,"qty_min":1,"qty_max":2}]',
 'Water','Normal', 0,50,0,0,0,0, 0,20,0,0,0,0, 0,8,0, NULL, NULL),

(10, 'Băng Ma', 'Ma băng tinh nhuệ, triệu hồi từ sông băng cổ', 13, 380, 100, 38, 12, 2.2, 1, 110, 50, 200,
 '[{"item_id":22,"drop_chance":0.5,"qty_min":1,"qty_max":2},{"item_id":14,"drop_chance":0.15,"qty_min":1,"qty_max":1}]',
 'Water','Elite', 0,60,0,0,0,0, 0,30,0,0,0,0, 3,12,8, NULL, NULL),

(11, 'Đế Băng', 'Hoàng đế băng hà cổ đại bị phong ấn', 15, 2200, 500, 120, 35, 2, 1.2, 900, 380, 1500,
 '[{"item_id":37,"drop_chance":0.5,"qty_min":1,"qty_max":2},{"item_id":207,"drop_chance":0.08,"qty_min":1,"qty_max":1},{"item_id":31,"drop_chance":0.05,"qty_min":1,"qty_max":1}]',
 'Water','Boss', 0,75,0,0,0,0, 0,45,0,0,0,0, 8,20,12,
 '[{"skill_id":"ICE_STORM","damage_multiplier":2.0,"element":"Water","cooldown_sec":10,"range":7,"aoe":true,"animation_trigger":"skill_storm"},{"skill_id":"FREEZE","damage_multiplier":1.0,"element":"Water","cooldown_sec":6,"range":4,"status_effect":"frozen","duration_sec":3,"animation_trigger":"skill_freeze"},{"skill_id":"BLIZZARD","damage_multiplier":1.8,"element":"Water","cooldown_sec":15,"range":10,"aoe":true,"animation_trigger":"skill_blizzard"}]',
 '[{"hp_pct_threshold":70,"action":"enrage","damage_multiplier":1.3,"speed_multiplier":1.1,"message":"Đế Băng thức tỉnh!"},{"hp_pct_threshold":40,"action":"encase","message":"Đế Băng phong ấn cả chiến trường!","aoe_freeze":true},{"hp_pct_threshold":20,"action":"berserk","damage_multiplier":2.2,"speed_multiplier":1.4,"message":"Đế Băng huy động toàn lực!"}]'),

-- ── Mê Cung Rừng Rậm mobs ──
(12, 'Mộc Linh', 'Tinh linh rừng, ẩn trong bóng cây', 8, 150, 30, 16, 4, 1.8, 1, 35, 16, 65,
 '[{"item_id":27,"drop_chance":0.45,"qty_min":1,"qty_max":3},{"item_id":25,"drop_chance":0.08,"qty_min":1,"qty_max":1}]',
 'Wood','Normal', 0,0,0,50,0,0, 0,0,0,20,0,0, 1,10,0, NULL, NULL),

(13, 'Cổ Thọ Mộc', 'Quái vật cây cổ thụ, rễ xuyên đất', 11, 450, 80, 45, 15, 1.5, 0.8, 130, 60, 240,
 '[{"item_id":27,"drop_chance":0.6,"qty_min":2,"qty_max":4},{"item_id":25,"drop_chance":0.12,"qty_min":1,"qty_max":1}]',
 'Wood','Elite', 0,0,0,60,0,0, 0,0,0,30,0,0, 5,8,5, NULL, NULL),

(14, 'Rừng Chúa', 'Thực thể rừng rậm bất tử ngàn năm', 13, 1800, 400, 100, 30, 1.8, 0.9, 750, 300, 1200,
 '[{"item_id":38,"drop_chance":0.5,"qty_min":1,"qty_max":2},{"item_id":222,"drop_chance":0.08,"qty_min":1,"qty_max":1},{"item_id":31,"drop_chance":0.05,"qty_min":1,"qty_max":1}]',
 'Wood','Boss', 0,0,0,70,0,0, 0,0,0,40,0,0, 10,12,8,
 '[{"skill_id":"ROOT","damage_multiplier":1.2,"element":"Wood","cooldown_sec":7,"range":5,"status_effect":"rooted","duration_sec":2,"animation_trigger":"skill_root"},{"skill_id":"THORN_WALL","damage_multiplier":1.8,"element":"Wood","cooldown_sec":10,"range":8,"aoe":true,"animation_trigger":"skill_thorn"},{"skill_id":"REGROW","heal_pct":10,"cooldown_sec":25,"animation_trigger":"skill_regrow"}]',
 '[{"hp_pct_threshold":60,"action":"enrage","damage_multiplier":1.3,"message":"Rừng Chúa triệu gọi thiên nhiên!"},{"hp_pct_threshold":30,"action":"heal","heal_pct":15,"message":"Rừng Chúa hồi phục từ đất!"},{"hp_pct_threshold":15,"action":"berserk","damage_multiplier":2.5,"speed_multiplier":1.5,"message":"Rừng Chúa đốt cháy cơn thịnh nộ!"}]'),

-- ── Thành Trì Bóng Tối mobs ──
(15, 'Hắc Quân Binh', 'Binh lính bóng tối trang bị đầy đủ', 15, 300, 60, 35, 20, 2, 1, 70, 30, 120,
 '[{"item_id":26,"drop_chance":0.3,"qty_min":1,"qty_max":2},{"item_id":11,"drop_chance":0.2,"qty_min":1,"qty_max":1}]',
 'Metal','Normal', 0,0,0,0,50,0, 0,0,0,0,20,0, 0,5,10, NULL, NULL),

(16, 'Hắc Quân Vệ', 'Vệ sĩ tinh nhuệ của Chúa Tể Bóng Tối', 18, 600, 120, 65, 30, 2.2, 1.2, 180, 80, 320,
 '[{"item_id":26,"drop_chance":0.5,"qty_min":1,"qty_max":3},{"item_id":15,"drop_chance":0.15,"qty_min":1,"qty_max":1}]',
 'Metal','Elite', 0,0,0,0,65,0, 0,0,0,0,30,0, 0,10,15, NULL, NULL),

(17, 'Chúa Tể Bóng Tối', 'Ác chủ bất tử cai trị thành trì cổ đại', 20, 3500, 800, 160, 50, 2.5, 1.5, 1500, 600, 2500,
 '[{"item_id":39,"drop_chance":0.5,"qty_min":1,"qty_max":2},{"item_id":40,"drop_chance":0.3,"qty_min":1,"qty_max":1},{"item_id":219,"drop_chance":0.06,"qty_min":1,"qty_max":1},{"item_id":31,"drop_chance":0.1,"qty_min":1,"qty_max":2}]',
 'Metal','Boss', 20,0,0,0,70,0, 10,0,0,0,40,0, 10,20,20,
 '[{"skill_id":"DARK_SLASH","damage_multiplier":2.8,"element":"Metal","cooldown_sec":6,"range":4,"animation_trigger":"skill_slash"},{"skill_id":"SHADOW_NOVA","damage_multiplier":2.0,"element":"Metal","cooldown_sec":10,"range":10,"aoe":true,"animation_trigger":"skill_nova"},{"skill_id":"SUMMON_GUARDS","spawn_enemy_id":16,"spawn_count":2,"cooldown_sec":25,"animation_trigger":"skill_summon"},{"skill_id":"VOID_SHIELD","damage_reduction_pct":50,"duration_sec":5,"cooldown_sec":30,"animation_trigger":"skill_shield"}]',
 '[{"hp_pct_threshold":75,"action":"summon","mob_id":15,"mob_count":3,"message":"Chúa Tể triệu hồi quân binh!"},{"hp_pct_threshold":50,"action":"enrage","damage_multiplier":1.4,"speed_multiplier":1.2,"message":"Chúa Tể kích hoạt giáp bóng tối!"},{"hp_pct_threshold":25,"action":"berserk","damage_multiplier":2.5,"speed_multiplier":1.5,"skill_cooldown_multiplier":0.4,"message":"Chúa Tể dùng tuyệt kỹ cuối cùng!"}]');

-- ============================================================
-- 7. CẬP NHẬT boss_enemy_id trong dungeon_config
-- ============================================================
UPDATE `dungeon_config` SET `boss_enemy_id` = 8  WHERE `dungeon_id` = 1;  -- Hỏa Long
UPDATE `dungeon_config` SET `boss_enemy_id` = 11 WHERE `dungeon_id` = 2;  -- Đế Băng
UPDATE `dungeon_config` SET `boss_enemy_id` = 14 WHERE `dungeon_id` = 3;  -- Rừng Chúa
UPDATE `dungeon_config` SET `boss_enemy_id` = 17 WHERE `dungeon_id` = 4;  -- Chúa Tể Bóng Tối

-- ============================================================
-- 8. CẤU HÌNH BOSS CONFIG
-- ============================================================
INSERT IGNORE INTO `boss_config` (`boss_id`,`map_id`,`spawn_x`,`spawn_y`,`min_spawn_hour`,`max_spawn_hour`,`respawn_minutes`) VALUES
(8,  15, 0, 0,  0, 23, 30),   -- Hỏa Long, phòng boss dungeon 1
(11, 17, 0, 0,  0, 23, 45),   -- Đế Băng, phòng boss dungeon 2
(14, 19, 0, 0,  0, 23, 40),   -- Rừng Chúa, phòng boss dungeon 3
(17, 22, 0, 0,  0, 23, 60);   -- Chúa Tể, phòng boss dungeon 4

-- ============================================================
-- 9. CẤU HÌNH CỔNG DỊCH CHUYỂN (map_portal)
--
--  Cấu trúc cổng cho 1 dungeon (ví dụ Hang Động Lửa):
--  Main Lobby (map 0)
--       │ portal_id=1 (enter_dungeon, cần Chìa Khóa item_id=34)
--       ▼
--  Tầng 1 (map 10) ──exit──► Main Lobby (portal_id=2)
--       │ portal_id=3 (room_transition, sau khi giết hết mob)
--       ▼
--  Tầng 2 (map 14) ──back──► Tầng 1 (portal_id=4)
--       │ portal_id=5 (room_transition)
--       ▼
--  Phòng Boss (map 15) ──exit──► Main Lobby sau khi boss chết (portal_id=6)
-- ============================================================
INSERT IGNORE INTO `map_portal`
  (`portal_id`,`portal_name`,`source_map_id`,`src_x`,`src_y`,`src_radius`,
   `dest_map_id`,`dest_scene_name`,`dest_x`,`dest_y`,`portal_type`,`required_item_id`,`dungeon_id`) VALUES

-- ═══ DUNGEON 1: HANG ĐỘNG LỬA ═══
(1, 'Vào Hang Động Lửa',      0,  18, 0, 2,  10, 'DungeonScene_FireCave',       -8, 0, 'enter_dungeon',  34, 1),
(2, 'Thoát Hang Động Lửa',   10, -10, 0, 2,   0, '',                             18, 0, 'exit_dungeon',   NULL, 1),
(3, 'Cửa Tầng 2 - Lửa',      10,  10, 0, 2,  14, 'DungeonScene_FireCave_Floor2', -8, 0, 'room_transition',NULL, 1),
(4, 'Quay Lại Tầng 1 - Lửa', 14, -10, 0, 2,  10, 'DungeonScene_FireCave',        10, 0, 'room_transition',NULL, 1),
(5, 'Cửa Phòng Boss - Lửa',  14,  10, 0, 2,  15, 'DungeonScene_FireCave_Boss',  -10, 0, 'room_transition',NULL, 1),
(6, 'Thoát sau Boss Lửa',    15,  12, 0, 2,   0, '',                             18, 0, 'exit_dungeon',   NULL, 1),

-- ═══ DUNGEON 2: THÁP BĂNG GIÁ ═══
(7,  'Vào Tháp Băng Giá',      0,  26, 0, 2,  11, 'DungeonScene_IceTower',       -8, 0, 'enter_dungeon',  35, 2),
(8,  'Thoát Tháp Băng Giá',   11, -10, 0, 2,   0, '',                             26, 0, 'exit_dungeon',   NULL, 2),
(9,  'Cầu Thang Lên - Băng',  11,  10, 0, 2,  16, 'DungeonScene_IceTower_Floor2',-8, 0, 'room_transition',NULL, 2),
(10, 'Cầu Thang Xuống - Băng',16, -10, 0, 2,  11, 'DungeonScene_IceTower',        10, 0, 'room_transition',NULL, 2),
(11, 'Cửa Phòng Boss - Băng', 16,  10, 0, 2,  17, 'DungeonScene_IceTower_Boss', -10, 0, 'room_transition',NULL, 2),
(12, 'Thoát sau Boss Băng',   17,  12, 0, 2,   0, '',                             26, 0, 'exit_dungeon',   NULL, 2),

-- ═══ DUNGEON 3: MÊ CUNG RỪNG RẬM ═══
(13, 'Vào Mê Cung Rừng',       0,  34, 0, 2,  12, 'DungeonScene_Forest',         -8, 0, 'enter_dungeon',  34, 3),
(14, 'Thoát Mê Cung Rừng',    12, -10, 0, 2,   0, '',                             34, 0, 'exit_dungeon',   NULL, 3),
(15, 'Đường Mòn Sâu Hơn',     12,  10, 0, 2,  18, 'DungeonScene_Forest_Floor2',  -8, 0, 'room_transition',NULL, 3),
(16, 'Đường Mòn Ra Ngoài',    18, -10, 0, 2,  12, 'DungeonScene_Forest',          10, 0, 'room_transition',NULL, 3),
(17, 'Lõi Rừng Cổ Đại',       18,  10, 0, 2,  19, 'DungeonScene_Forest_Boss',   -10, 0, 'room_transition',NULL, 3),
(18, 'Thoát sau Boss Rừng',    19,  12, 0, 2,   0, '',                             34, 0, 'exit_dungeon',   NULL, 3),

-- ═══ DUNGEON 4: THÀNH TRÌ BÓNG TỐI ═══
(19, 'Cổng Thành Bóng Tối',    0,  42, 0, 2,  13, 'DungeonScene_DarkCastle',     -8, 0, 'enter_dungeon',  35, 4),
(20, 'Thoát Thành Trì',       13, -10, 0, 2,   0, '',                             42, 0, 'exit_dungeon',   NULL, 4),
(21, 'Lên Tầng 2 - Bóng Tối', 13,  10, 0, 2,  20, 'DungeonScene_DarkCastle_Floor2',-8,0, 'room_transition',NULL, 4),
(22, 'Xuống Tầng 1',          20, -10, 0, 2,  13, 'DungeonScene_DarkCastle',      10, 0, 'room_transition',NULL, 4),
(23, 'Lên Tầng 3 - Bóng Tối', 20,  10, 0, 2,  21, 'DungeonScene_DarkCastle_Floor3',-8,0, 'room_transition',NULL, 4),
(24, 'Xuống Tầng 2',          21, -10, 0, 2,  20, 'DungeonScene_DarkCastle_Floor2',10,0, 'room_transition',NULL, 4),
(25, 'Vào Ngai Vàng Bóng Tối',21,  10, 0, 2,  22, 'DungeonScene_DarkCastle_Boss',-10,0, 'room_transition',NULL, 4),
(26, 'Thoát sau Boss Bóng Tối',22, 12, 0, 2,   0, '',                             42, 0, 'exit_dungeon',   NULL, 4);

-- ============================================================
-- 10. THÊM ITEM MỚI VÀO item_template
--     TYPE: 26=MovementBuff, 27=DungeonKey, 28=BossFragment
-- ============================================================
INSERT IGNORE INTO `item_template` (`id`,`name`,`detail`,`isXepChong`,`gioiTinh`,`type`,`idClass`,`idIcon`,`levelNeed`,`taiPhuNeed`,`idMob`,`idChar`) VALUES
-- Cuộn tốc độ (type=26)
(32, 'Cuộn Tốc Độ Nhỏ',     'Tăng 20% tốc độ di chuyển trong 3 phút',   'True', 2, 26, 0, 50, 1,  0, -1, 0),
(33, 'Cuộn Tốc Độ Lớn',     'Tăng 40% tốc độ di chuyển trong 5 phút',   'True', 2, 26, 0, 51, 15, 0, -1, 0),
-- Chìa khóa phó bản (type=27)
(34, 'Chìa Khóa Phó Bản Thường',  'Mở cổng Hang Động Lửa hoặc Mê Cung Rừng Rậm', 'False', 2, 27, 0, 60, 5,  0, -1, 0),
(35, 'Chìa Khóa Phó Bản Tinh Anh','Mở cổng Tháp Băng Giá hoặc Thành Trì Bóng Tối','False', 2, 27, 0, 61, 10, 0, -1, 0),
-- Mảnh hồn Boss (type=28) — thu thập đổi trang bị
(36, 'Mảnh Hồn Hỏa Long',    'Mảnh hồn từ Hỏa Long. Thu thập 5 mảnh đổi phần thưởng đặc biệt.', 'True', 2, 28, 1, 70, 1,  0,  8, 0),
(37, 'Mảnh Hồn Đế Băng',     'Mảnh hồn từ Đế Băng.',    'True', 2, 28, 2, 71, 1,  0, 11, 0),
(38, 'Mảnh Hồn Rừng Chúa',   'Mảnh hồn từ Rừng Chúa.',  'True', 2, 28, 5, 72, 1,  0, 14, 0),
(39, 'Mảnh Hồn Chúa Tể',     'Mảnh hồn từ Chúa Tể Bóng Tối.', 'True', 2, 28, 4, 73, 1, 0, 17, 0),
(40, 'Mảnh Hồn Cổ Thọ',      'Rơi từ Cổ Thọ Mộc Elite (boss mini).',    'True', 2, 28, 5, 74, 1,  0, 13, 0);

-- ============================================================
-- 11. THÊM DROP OVERRIDE THEO MAP (map_enemy_drop)
--     Khi quái chết ở map này, tỉ lệ drop THAY ĐỔI so với mặc định
-- ============================================================
INSERT IGNORE INTO `map_enemy_drop` (`map_id`,`enemy_id`,`item_id`,`drop_chance`,`qty_min`,`qty_max`) VALUES
-- Tầng 1 Hang Động Lửa
(10,  6, 30, 0.5, 1, 2),  -- Hỏa Linh rơi Tinh Thể Lửa nhiều hơn
(10,  6, 34, 0.02, 1, 1), -- Hiếm rơi Chìa Khóa
(10,  7, 30, 0.6, 1, 3),
-- Tầng 2 Hang Động Lửa
(14,  6, 30, 0.7, 2, 4),  -- Tầng 2: drop cao hơn
(14,  7, 21, 0.15, 1, 1), -- Tinh Chất Hỏa Nguyên
-- Phòng Boss Hang Động Lửa  
(15,  8, 36, 0.8, 1, 2),  -- Mảnh Hồn Hỏa Long tỉ lệ cao hơn ở phòng boss
(15,  8, 31, 0.1, 1, 1),  -- Lõi đột biến hiếm
-- Tháp Băng Giá
(11,  9, 22, 0.4, 1, 2),
(16, 10, 22, 0.6, 1, 3),
(17, 11, 37, 0.8, 1, 2),
-- Mê Cung Rừng Rậm
(12, 12, 27, 0.5, 1, 3),
(18, 13, 25, 0.15, 1, 1),
(19, 14, 38, 0.8, 1, 2),
-- Thành Trì Bóng Tối
(13, 15, 26, 0.4, 1, 2),
(20, 16, 26, 0.6, 2, 4),
(22, 17, 39, 0.8, 1, 2),
(22, 17, 40, 0.5, 1, 1),
(22, 17, 31, 0.15, 1, 1);

-- ============================================================
-- 12. THÊM SPAWN POINTS MỚI CHO PHÒNG PHÓ BẢN
-- ============================================================
INSERT IGNORE INTO `enemy_spawns` (`spawn_id`,`map_id`,`enemy_type_id`,`spawn_x`,`spawn_y`,`max_spawn_count`,`respawn_time`) VALUES
-- Hang Động Lửa - Tầng 1 (map 10)
(10, 10, 6, 2,  0, 4, 30),
(11, 10, 6, 5,  0, 4, 30),
(12, 10, 7, 8,  0, 2, 45),
-- Hang Động Lửa - Tầng 2 (map 14)
(13, 14, 6, 2,  0, 3, 25),
(14, 14, 7, 5,  0, 3, 40),
(15, 14, 7, 8,  0, 2, 40),
-- Hang Động Lửa - Boss Room (map 15)
(16, 15, 8, 0,  0, 1, 1800),  -- Boss: respawn 30 phút
-- Tháp Băng Giá - Tầng 1 (map 11)
(17, 11, 9,   2, 0, 4, 30),
(18, 11, 10,  6, 0, 2, 45),
-- Tháp Băng Giá - Tầng 2 (map 16)
(19, 16, 9,   2, 0, 3, 25),
(20, 16, 10,  6, 0, 3, 40),
-- Tháp Băng Giá - Boss Room (map 17)
(21, 17, 11,  0, 0, 1, 2700),
-- Mê Cung Rừng Rậm - Tầng 1 (map 12)
(22, 12, 12,  2, 0, 5, 25),
(23, 12, 13,  7, 0, 2, 40),
-- Mê Cung Rừng Rậm - Tầng 2 (map 18)
(24, 18, 12,  2, 0, 4, 22),
(25, 18, 13,  6, 0, 3, 38),
-- Mê Cung Rừng Rậm - Boss Room (map 19)
(26, 19, 14,  0, 0, 1, 2400),
-- Thành Trì - Tầng 1 (map 13)
(27, 13, 15,  2, 0, 4, 25),
(28, 13, 16,  7, 0, 2, 40),
-- Thành Trì - Tầng 2 (map 20)
(29, 20, 15,  2, 0, 4, 22),
(30, 20, 16,  7, 0, 3, 38),
-- Thành Trì - Tầng 3 (map 21)
(31, 21, 15,  2, 0, 3, 20),
(32, 21, 16,  6, 0, 4, 35),
-- Thành Trì - Boss Room (map 22)
(33, 22, 17,  0, 0, 1, 3600);  -- Boss: respawn 60 phút
