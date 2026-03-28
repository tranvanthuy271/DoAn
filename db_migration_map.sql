-- ============================================================
-- Migration: Dọn dẹp & hoàn chỉnh schema Map cho LangLa
-- Chạy file này một lần trên database `gamedb`
-- ============================================================

-- 1. XÓA bảng map_zone_config (không cần nữa — zone xử lý trong Unity Inspector)
DROP TABLE IF EXISTS `map_zone_config`;

-- 2. Thêm cột portal_direction vào map_portal (thay cho heuristic x-position)
ALTER TABLE `map_portal`
    ADD COLUMN IF NOT EXISTS `portal_direction`
        ENUM('left','right','none') NOT NULL DEFAULT 'none'
        AFTER `portal_type`;

-- Cập nhật dữ liệu cũ: portal bên phải -> src_x dương, bên trái -> src_x âm
UPDATE `map_portal`
SET `portal_direction` = CASE
    WHEN `portal_type` = 'world_travel' AND `src_x` >= 0 THEN 'right'
    WHEN `portal_type` = 'world_travel' AND `src_x` < 0  THEN 'left'
    ELSE 'none'
END;

-- ============================================================
-- 3. TẠO bảng map_portal nếu chưa tồn tại
-- ============================================================
CREATE TABLE IF NOT EXISTS `map_portal` (
  `portal_id`       int(11) NOT NULL AUTO_INCREMENT,
  `portal_name`     varchar(100) NOT NULL DEFAULT '',
  `source_map_id`   int(11) NOT NULL,
  `src_x`           float NOT NULL DEFAULT 0,
  `src_y`           float NOT NULL DEFAULT 0,
  `src_radius`      float NOT NULL DEFAULT 2.0,
  `dest_map_id`     int(11) NOT NULL,
  `dest_scene_name` varchar(100) NOT NULL DEFAULT '',
  `dest_x`          float NOT NULL DEFAULT 0,
  `dest_y`          float NOT NULL DEFAULT 0,
  `portal_type`     varchar(30) NOT NULL DEFAULT 'world_travel'
                    COMMENT 'world_travel | enter_dungeon | exit_dungeon',
  `portal_direction` ENUM('left','right','none') NOT NULL DEFAULT 'none',
  `required_item_id` int(11) DEFAULT NULL,
  `dungeon_id`       int(11) DEFAULT NULL,
  `is_active`        tinyint(1) NOT NULL DEFAULT 1,
  PRIMARY KEY (`portal_id`),
  KEY `idx_source_map` (`source_map_id`),
  KEY `idx_dest_map`   (`dest_map_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 4. DỮ LIỆU MẪU map_config
-- (Cập nhật nếu đã có, INSERT nếu chưa có)
-- ============================================================
INSERT INTO `map_config` (`map_id`, `map_name`, `scene_name`, `spawn_points_json`, `min_level`, `max_level`)
VALUES
  (0, 'Làng Khởi Đầu', 'GameScene', '[{"x":0,"y":0},{"x":5,"y":0},{"x":-5,"y":0}]', 1,  10),
  (1, 'Cánh Đồng Lửa',  'Map1',      '[{"x":2,"y":1},{"x":-2,"y":1}]',               5,  20),
  (2, 'Rừng Băng',       'Map2',      '[{"x":0,"y":2},{"x":4,"y":2}]',                15, 30),
  (3, 'Sa Mạc Phong',    'Map3',      '[{"x":3,"y":0},{"x":-3,"y":0}]',               25, 40)
ON DUPLICATE KEY UPDATE
  `map_name`         = VALUES(`map_name`),
  `scene_name`       = VALUES(`scene_name`),
  `spawn_points_json`= VALUES(`spawn_points_json`),
  `min_level`        = VALUES(`min_level`),
  `max_level`        = VALUES(`max_level`);

-- ============================================================
-- 5. DỮ LIỆU MẪU map_portal
-- ============================================================
INSERT INTO `map_portal`
  (`portal_name`, `source_map_id`, `src_x`, `src_y`, `src_radius`,
   `dest_map_id`, `dest_scene_name`, `dest_x`, `dest_y`,
   `portal_type`, `portal_direction`, `is_active`)
VALUES
  -- GameScene → Map1 (cổng phải)
  ('Cổng → Cánh Đồng Lửa',  0, 18, 0, 2.5, 1, 'Map1',      -16, 0, 'world_travel', 'right', 1),
  -- Map1 ← GameScene (cổng trái)
  ('Cổng ← Làng Khởi Đầu',  1, -18, 0, 2.5, 0, 'GameScene', 16, 0, 'world_travel', 'left',  1),
  -- Map1 → Map2 (cổng phải)
  ('Cổng → Rừng Băng',       1, 18, 0, 2.5, 2, 'Map2',      -16, 0, 'world_travel', 'right', 1),
  -- Map2 ← Map1 (cổng trái)
  ('Cổng ← Cánh Đồng Lửa',  2, -18, 0, 2.5, 1, 'Map1',      16, 0, 'world_travel', 'left',  1),
  -- Map2 → Map3 (cổng phải)
  ('Cổng → Sa Mạc Phong',    2, 18, 0, 2.5, 3, 'Map3',      -16, 0, 'world_travel', 'right', 1),
  -- Map3 ← Map2 (cổng trái)
  ('Cổng ← Rừng Băng',       3, -18, 0, 2.5, 2, 'Map2',      16, 0, 'world_travel', 'left',  1);

-- ============================================================
-- 6. DỮ LIỆU MẪU enemy_spawns
-- ============================================================
INSERT INTO `enemy_spawns` (`map_id`, `enemy_type_id`, `spawn_x`, `spawn_y`, `max_spawn_count`, `respawn_time`)
VALUES
  -- GameScene (Làng Khởi Đầu): Slime + Goblin
  (0, 1, -8,  0, 3, 30),
  (0, 1,  8,  0, 3, 30),
  (0, 2,  12, 0, 2, 45),
  -- Map1 (Cánh Đồng Lửa): Fire Slime + Goblin
  (1, 4, -10, 0, 4, 30),
  (1, 4,  10, 0, 4, 30),
  (1, 2,   5, 1, 3, 45),
  -- Map2 (Rừng Băng): Orc Warrior
  (2, 3, -8,  1, 3, 60),
  (2, 3,  8,  1, 3, 60),
  -- Map3 (Sa Mạc Phong): Boss Dragon
  (3, 5,  0,  2, 1, 300);

-- ============================================================
-- 7. DỮ LIỆU MẪU npc_config
-- ============================================================
INSERT INTO `npc_config` (`npc_name`, `npc_type`, `map_id`, `pos_x`, `pos_y`, `dialogue_key`, `icon_id`, `is_active`)
VALUES
  ('Lão Trưởng — Thương Nhân', 'shop',       0,  3, -1, 'greet',         'npc_merchant_1', 1),
  ('Đại Tướng Lan',             'quest',      0, -3, -1, 'quest_intro',   'npc_quest_1',    1),
  ('Thợ Rèn Hắc Long',          'blacksmith', 0,  0,  0, 'greet',         'npc_smith_1',    1),
  ('Lữ Hành Giả',               'quest',      1,  0, -1, 'map1_quest',    'npc_quest_2',    1),
  ('Thương Nhân Sa Mạc',         'shop',       3,  2, -1, 'desert_shop',   'npc_merchant_2', 1)
ON DUPLICATE KEY UPDATE
  `npc_name`     = VALUES(`npc_name`),
  `npc_type`     = VALUES(`npc_type`),
  `pos_x`        = VALUES(`pos_x`),
  `pos_y`        = VALUES(`pos_y`),
  `dialogue_key` = VALUES(`dialogue_key`);
