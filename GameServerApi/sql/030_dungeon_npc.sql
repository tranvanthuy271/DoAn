-- ============================================================
-- Migration: 030_dungeon_npc.sql
-- Thêm NPC dungeon và dữ liệu phó bản vào DB
-- ============================================================

SET NAMES utf8mb4;

-- Thêm NPC dungeon (npc_type = 'dungeon') nếu chưa có
INSERT IGNORE INTO npc_config (npc_id, npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id, is_active)
VALUES
  (100, N'Sứ Giả Phó Bản',   'dungeon', 0, 50.0, 10.0, 'npc_dungeon_greet', 0, 1),
  (101, N'Người Gác Hầm Ngục', 'dungeon', 1, 30.0, 15.0, 'npc_dungeon_greet', 0, 1);

-- Thêm cấu hình phó bản nếu chưa có
-- dungeon_type: 0 = wave/boss thường, 1 = party dungeon
INSERT IGNORE INTO dungeon_config
  (dungeon_id, dungeon_name, dungeon_type, map_id, max_players, min_level, max_level,
   wave_count, boss_id, reward_exp, reward_gold, is_active)
VALUES
  (1, N'Phó Bản Lẻ - Hang Quỷ',    0, 10, 1, 1,  30, 5, 101, 800,  200, 1),
  (2, N'Phó Bản Đội - Tháp Rồng',  1, 11, 4, 15, 40, 8, 102, 2000, 600, 1),
  (3, N'Phó Bản Đội - Ngục Băng',  1, 12, 4, 20, 50, 6, 103, 2500, 800, 1);

-- Ghi chú: nếu bảng dungeon_config chưa tồn tại, tạo trước:
-- CREATE TABLE IF NOT EXISTS dungeon_config (
--   dungeon_id    INT           NOT NULL AUTO_INCREMENT PRIMARY KEY,
--   dungeon_name  VARCHAR(100)  NOT NULL,
--   dungeon_type  TINYINT       NOT NULL DEFAULT 0 COMMENT '0=solo,1=party',
--   map_id        INT           NOT NULL DEFAULT 0,
--   max_players   INT           NOT NULL DEFAULT 1,
--   min_level     INT           NOT NULL DEFAULT 1,
--   max_level     INT           NOT NULL DEFAULT 999,
--   wave_count    INT           NOT NULL DEFAULT 1,
--   boss_id       INT           NOT NULL DEFAULT 0,
--   reward_exp    INT           NOT NULL DEFAULT 0,
--   reward_gold   INT           NOT NULL DEFAULT 0,
--   is_active     TINYINT(1)    NOT NULL DEFAULT 1
-- ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
