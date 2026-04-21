-- ============================================================
-- Migration: 042_fix_boss_drop_wave.sql
-- Sửa toàn bộ dữ liệu cần thiết để boss spawn và item drop hoạt động.
-- Idempotent - an toàn chạy nhiều lần.
-- ============================================================

SET NAMES utf8mb4;
SET foreign_key_checks = 0;

-- ============================================================
-- 0. Dọn dẹp duplicate trong map_spawn_config
-- ============================================================
DELETE t1 FROM `map_spawn_config` t1
INNER JOIN `map_spawn_config` t2
  ON t1.`map_id` = t2.`map_id` AND t1.`id` < t2.`id`;

-- ============================================================
-- 1. map_config – đảm bảo DungeonWaveScene (map_id=110) tồn tại
-- ============================================================
INSERT INTO `map_config`
  (`map_id`, `map_name`, `scene_name`, `spawn_points_json`, `min_level`, `max_level`)
SELECT 110, 'DungeonWave', 'DungeonWaveScene', '[{"x":0,"y":0}]', 1, 999
WHERE NOT EXISTS (SELECT 1 FROM `map_config` WHERE `map_id` = 110);

-- ============================================================
-- 2. dungeon_config – đảm bảo dungeon_id=6 dùng map_id=110
-- ============================================================
UPDATE `dungeon_config`
SET    `map_id` = 110, `scene_name` = 'DungeonWaveScene'
WHERE  `dungeon_id` = 6;

-- ============================================================
-- 3. map_spawn_config – đảm bảo row cho map_id=110 tồn tại
-- ============================================================
INSERT INTO `map_spawn_config` (`map_id`, `spawn_json`, `drop_json`)
SELECT 110, '[]', '[]'
WHERE NOT EXISTS (SELECT 1 FROM `map_spawn_config` WHERE `map_id` = 110);

-- Cập nhật spawn_json chuẩn (27 Mộc Linh + 1 Đế Băng)
UPDATE `map_spawn_config`
SET `spawn_json` = '[
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-4,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-1.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":1,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":3.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":6,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":8.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":11,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":13.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":16,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":18.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-4.56,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-2.06,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":0.44,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":2.94,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":5.44,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":7.94,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":10.44,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":12.94,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":15.44,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-4.29,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-1.79,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":0.71,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":3.21,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":5.71,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":8.21,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":10.71,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":13.21,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":11,"hp":110000,"exp":100000,"cx":18.55,"cy":5.88,"is_boss":true,"count":1,"respawn_time":0,"level":10}
]'
WHERE `map_id` = 110;

-- ============================================================
-- 4. enemy – set drop_items_json cho enemy_id=11 (Đế Băng) và 12 (Mộc Linh)
-- ============================================================
UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":37,"drop_chance":0.5,"qty_min":1,"qty_max":2},{"item_id":207,"drop_chance":0.08,"qty_min":1,"qty_max":1},{"item_id":31,"drop_chance":0.05,"qty_min":1,"qty_max":1}]'
WHERE `enemy_id` = 11 AND (`drop_items_json` IS NULL OR `drop_items_json` = '');

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":27,"drop_chance":0.45,"qty_min":1,"qty_max":3},{"item_id":25,"drop_chance":0.08,"qty_min":1,"qty_max":1}]'
WHERE `enemy_id` = 12 AND (`drop_items_json` IS NULL OR `drop_items_json` = '');

-- ============================================================
-- 5. item_template – nguyên liệu enemy drop + vé phó bản sóng
-- ============================================================
-- item_id=25: Mộc Tinh (rơi từ Mộc Linh enemy_id=12)
INSERT INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`, `isLock`, `sellPrice`)
SELECT 25, 'Moc Tinh', 'Tinh chat cua linh moc, roi tu Moc Linh', 'True', 2, 30, 0, 0, 1, 0, 12, 0, 0, 50
WHERE NOT EXISTS (SELECT 1 FROM `item_template` WHERE `id` = 25);

-- item_id=37: Tinh Thể Băng (rơi từ Đế Băng enemy_id=11)
INSERT INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`, `isLock`, `sellPrice`)
SELECT 37, 'Tinh The Bang', 'Tinh the bang gia tu De Bang, dung lam nguyen lieu nang cao', 'True', 2, 30, 0, 0, 1, 0, 11, 0, 0, 200
WHERE NOT EXISTS (SELECT 1 FROM `item_template` WHERE `id` = 37);

INSERT INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`, `isLock`, `sellPrice`)
SELECT 409, 'Vé Phó Bản (+1 Lần)', 'Cho phép vào Phó Bản Sóng thêm 1 lần trong ngày', 'True', 2, 31, 0, 0, 1, 0, -1, 0, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM `item_template` WHERE `id` = 409);

INSERT INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`, `isLock`, `sellPrice`)
SELECT 410, 'Vé Phó Bản (+2 Lần)', 'Cho phép vào Phó Bản Sóng thêm 2 lần trong ngày', 'True', 2, 31, 0, 0, 1, 0, -1, 0, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM `item_template` WHERE `id` = 410);

-- ============================================================
-- 6. dungeon_wave_config (CREATE IF NOT EXISTS + seed)
-- ============================================================
CREATE TABLE IF NOT EXISTS `dungeon_wave_config` (
  `dungeon_id`             int(11)  NOT NULL,
  `max_waves`              int(11)  NOT NULL DEFAULT 20,
  `wave_time_seconds`      int(11)  NOT NULL DEFAULT 300,
  `enemy_scale_percent`    float    NOT NULL DEFAULT 10.0,
  `boss_scale_percent`     float    NOT NULL DEFAULT 15.0,
  `exp_gold_scale_percent` float    NOT NULL DEFAULT 10.0,
  `daily_entry_limit`      int(11)  NOT NULL DEFAULT 1,
  `entry_item_plus1_id`    int(11)  DEFAULT 409,
  `entry_item_plus2_id`    int(11)  DEFAULT 410,
  `milestone_reward_json`  longtext NOT NULL DEFAULT '[]',
  `updated_at`             datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`dungeon_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `dungeon_wave_config`
  (`dungeon_id`, `max_waves`, `wave_time_seconds`, `enemy_scale_percent`, `boss_scale_percent`, `exp_gold_scale_percent`, `daily_entry_limit`, `entry_item_plus1_id`, `entry_item_plus2_id`, `milestone_reward_json`)
SELECT 6, 20, 300, 10.0, 15.0, 10.0, 1, 409, 410,
'[{"wave":5,"exp":5000,"gold":500,"items":[]},{"wave":10,"exp":15000,"gold":1500,"items":[]},{"wave":15,"exp":30000,"gold":3000,"items":[]},{"wave":20,"exp":50000,"gold":5000,"items":[{"item_template_id":31,"qty":1}]}]'
WHERE NOT EXISTS (SELECT 1 FROM `dungeon_wave_config` WHERE `dungeon_id` = 6);

-- ============================================================
-- 7. dungeon_wave_entry (CREATE IF NOT EXISTS)
-- ============================================================
CREATE TABLE IF NOT EXISTS `dungeon_wave_entry` (
  `id`            int(11)  NOT NULL AUTO_INCREMENT,
  `character_id`  int(11)  NOT NULL,
  `dungeon_id`    int(11)  NOT NULL,
  `entry_date`    date     NOT NULL,
  `entries_used`  int(11)  NOT NULL DEFAULT 0,
  `entries_limit` int(11)  NOT NULL DEFAULT 1,
  `updated_at`    datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_player_dungeon_date` (`character_id`, `dungeon_id`, `entry_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
-- 8. dungeon_wave_session (CREATE IF NOT EXISTS)
-- ============================================================
CREATE TABLE IF NOT EXISTS `dungeon_wave_session` (
  `session_id`         int(11)                      NOT NULL AUTO_INCREMENT,
  `character_id`       int(11)                      NOT NULL,
  `dungeon_id`         int(11)                      NOT NULL,
  `current_wave`       int(11)                      NOT NULL DEFAULT 1,
  `current_phase`      enum('enemy','boss')          NOT NULL DEFAULT 'enemy',
  `session_started_at` datetime                     NOT NULL DEFAULT current_timestamp(),
  `wave_started_at`    datetime                     NOT NULL DEFAULT current_timestamp(),
  `is_active`          tinyint(1)                   NOT NULL DEFAULT 1,
  `exit_reason`        enum('completed','timeout','left','') NOT NULL DEFAULT '',
  `updated_at`         datetime                     NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`session_id`),
  UNIQUE KEY `uq_active_session` (`character_id`, `dungeon_id`, `is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

SET foreign_key_checks = 1;
