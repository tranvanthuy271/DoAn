-- ============================================================
-- Migration: 040_dungeon_wave.sql
-- DungeonWaveScene – toàn bộ schema và dữ liệu cần thiết:
--   1. map_config  : thêm map_id=110 (DungeonWaveScene)
--   2. dungeon_config : sửa map_id của dungeon_id=6 thành 110
--   3. map_spawn_config : seed 27 quái thường + 1 boss cho map 110
--   4. item_template : 2 item vé phó bản (+1 lần / +2 lần)
--   5. dungeon_wave_config  (NEW) : tham số wave per dungeon
--   6. dungeon_wave_entry   (NEW) : giới hạn lượt vào hàng ngày per player
--   7. dungeon_wave_session (NEW) : lưu trạng thái session để reconnect
-- ============================================================

SET NAMES utf8mb4;
SET foreign_key_checks = 0;

-- ------------------------------------------------------------
-- 1. map_config – thêm DungeonWaveScene (map_id=110)
-- ------------------------------------------------------------
INSERT INTO `map_config`
  (`map_id`, `map_name`, `scene_name`, `spawn_points_json`, `min_level`, `max_level`)
SELECT 110, 'DungeonWave', 'DungeonWaveScene', '[{"x":0,"y":0}]', 1, 999
WHERE NOT EXISTS (SELECT 1 FROM `map_config` WHERE `map_id` = 110);

-- ------------------------------------------------------------
-- 2. dungeon_config – sửa map_id=100 → 110 cho dungeon_id=6
--    (030_dungeon_npc.sql đã seed row này với map_id=100 nhầm)
-- ------------------------------------------------------------
UPDATE `dungeon_config`
SET    `map_id` = 110, `scene_name` = 'DungeonWaveScene'
WHERE  `dungeon_id` = 6;

-- ------------------------------------------------------------
-- 3. map_spawn_config – enemy data cho DungeonWaveScene
--    27 quái thường (enemy_id=12) + 1 boss (enemy_id=11)
--    respawn_time=0 vì WaveDungeonRuntime tự spawn lại mỗi vòng
-- ------------------------------------------------------------
INSERT INTO `map_spawn_config` (`map_id`, `spawn_json`, `drop_json`)
SELECT 110,
'[
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
]',
'[]'
WHERE NOT EXISTS (SELECT 1 FROM `map_spawn_config` WHERE `map_id` = 110);

-- ------------------------------------------------------------
-- 4. item_template – vé phó bản sóng
--    type=31 = DungeonTicket (loại mới, consumable entry item)
--    idIcon config sau trong Unity; đặt 0 tạm thời
-- ------------------------------------------------------------
INSERT INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`,
   `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`, `isLock`, `sellPrice`)
SELECT 409, 'Vé Phó Bản (+1 Lần)',
       'Cho phép vào Phó Bản Sóng thêm 1 lần trong ngày',
       'True', 2, 31, 0, 0, 1, 0, -1, 0, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM `item_template` WHERE `id` = 409);

INSERT INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`,
   `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`, `isLock`, `sellPrice`)
SELECT 410, 'Vé Phó Bản (+2 Lần)',
       'Cho phép vào Phó Bản Sóng thêm 2 lần trong ngày',
       'True', 2, 31, 0, 0, 1, 0, -1, 0, 0, 0
WHERE NOT EXISTS (SELECT 1 FROM `item_template` WHERE `id` = 410);

-- ------------------------------------------------------------
-- 5. dungeon_wave_config (NEW)
--    Tham số wave cho từng dungeon. SO trong Unity mirror bảng
--    này nhưng server là nguồn sự thật.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `dungeon_wave_config` (
  `dungeon_id`            int(11)     NOT NULL COMMENT 'FK → dungeon_config.dungeon_id',
  `max_waves`             int(11)     NOT NULL DEFAULT 20     COMMENT 'Số vòng tối đa, mặc định 20',
  `wave_time_seconds`     int(11)     NOT NULL DEFAULT 300    COMMENT 'Giây mỗi vòng, mặc định 5 phút',
  `enemy_scale_percent`   float       NOT NULL DEFAULT 10.0   COMMENT '% tăng stat quái mỗi vòng (lũy thừa)',
  `boss_scale_percent`    float       NOT NULL DEFAULT 15.0   COMMENT '% tăng stat boss mỗi vòng (lũy thừa, config riêng)',
  `exp_gold_scale_percent` float      NOT NULL DEFAULT 10.0   COMMENT '% tăng exp/gold drop mỗi vòng (lũy thừa)',
  `daily_entry_limit`     int(11)     NOT NULL DEFAULT 1      COMMENT 'Lượt vào tối đa 1 ngày',
  `entry_item_plus1_id`   int(11)     DEFAULT 409             COMMENT 'item_template_id cho vé +1 lần',
  `entry_item_plus2_id`   int(11)     DEFAULT 410             COMMENT 'item_template_id cho vé +2 lần',
  `milestone_reward_json` longtext    NOT NULL DEFAULT '[]'   COMMENT 'JSON: [{wave,exp,gold,items:[{item_template_id,qty}]}]',
  `updated_at`            datetime    NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`dungeon_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Wave-specific config per dungeon; mirrors DungeonWaveConfig SO trong Unity';

INSERT INTO `dungeon_wave_config`
  (`dungeon_id`, `max_waves`, `wave_time_seconds`,
   `enemy_scale_percent`, `boss_scale_percent`, `exp_gold_scale_percent`,
   `daily_entry_limit`, `entry_item_plus1_id`, `entry_item_plus2_id`,
   `milestone_reward_json`)
SELECT 6, 20, 300, 10.0, 15.0, 10.0, 1, 409, 410,
'[
  {"wave":5,  "exp":5000,  "gold":500,  "items":[]},
  {"wave":10, "exp":15000, "gold":1500, "items":[]},
  {"wave":15, "exp":30000, "gold":3000, "items":[]},
  {"wave":20, "exp":50000, "gold":5000, "items":[{"item_template_id":31,"qty":1}]}
]'
WHERE NOT EXISTS (SELECT 1 FROM `dungeon_wave_config` WHERE `dungeon_id` = 6);

-- ------------------------------------------------------------
-- 6. dungeon_wave_entry (NEW)
--    Theo dõi số lượt vào của từng player trong ngày.
--    Reset tự nhiên vì dùng entry_date = DATE(NOW()) làm key.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `dungeon_wave_entry` (
  `id`            int(11)     NOT NULL AUTO_INCREMENT,
  `character_id`  int(11)     NOT NULL COMMENT 'FK → characters.id (chơi bằng nhân vật)',
  `dungeon_id`    int(11)     NOT NULL COMMENT 'FK → dungeon_config.dungeon_id',
  `entry_date`    date        NOT NULL COMMENT 'Ngày theo giờ server (UTC). Reset tự nhiên sang ngày mới.',
  `entries_used`  int(11)     NOT NULL DEFAULT 0 COMMENT 'Số lượt đã dùng hôm nay',
  `entries_limit` int(11)     NOT NULL DEFAULT 1 COMMENT 'Giới hạn hôm nay (base + bonus từ vé)',
  `updated_at`    datetime    NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_player_dungeon_date` (`character_id`, `dungeon_id`, `entry_date`),
  KEY `idx_wentry_char` (`character_id`),
  KEY `idx_wentry_date` (`entry_date`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Giới hạn lượt vào hàng ngày. entries_limit tăng khi dùng vé; reset sang ngày mới vì entry_date là key.';

-- ------------------------------------------------------------
-- 7. dungeon_wave_session (NEW)
--    Lưu trạng thái active session để server xử lý reconnect
--    và timeout đúng giờ khi player offline.
-- ------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `dungeon_wave_session` (
  `session_id`        int(11)     NOT NULL AUTO_INCREMENT,
  `character_id`      int(11)     NOT NULL COMMENT 'FK → characters.id',
  `dungeon_id`        int(11)     NOT NULL COMMENT 'FK → dungeon_config.dungeon_id',
  `current_wave`      int(11)     NOT NULL DEFAULT 1,
  `current_phase`     enum('enemy','boss') NOT NULL DEFAULT 'enemy'
                      COMMENT 'enemy = đang xử lý quái thường; boss = boss đã spawn',
  `session_started_at` datetime   NOT NULL DEFAULT current_timestamp()
                      COMMENT 'Thời điểm player bắt đầu phó bản (để tính tổng thời gian)',
  `wave_started_at`   datetime    NOT NULL DEFAULT current_timestamp()
                      COMMENT 'Thời điểm bắt đầu vòng hiện tại (để tính còn bao nhiêu giây)',
  `is_active`         tinyint(1)  NOT NULL DEFAULT 1
                      COMMENT '1=đang chơi, 0=đã kết thúc (hoàn thành/timeout/rời)',
  `exit_reason`       enum('completed','timeout','left','') NOT NULL DEFAULT ''
                      COMMENT 'Lý do kết thúc; rỗng khi is_active=1',
  `updated_at`        datetime    NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`session_id`),
  -- Mỗi nhân vật chỉ có 1 session active trên 1 dungeon tại 1 thời điểm
  UNIQUE KEY `uq_active_session` (`character_id`, `dungeon_id`, `is_active`),
  KEY `idx_wsession_char` (`character_id`),
  KEY `idx_wsession_active` (`is_active`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Trạng thái wave session. Server dùng để reconnect và xử lý timeout khi player offline.';

SET foreign_key_checks = 1;
