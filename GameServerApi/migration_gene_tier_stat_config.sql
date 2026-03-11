-- ============================================================
-- Migration: tạo bảng gene_tier_stat_config
-- Lưu config stat bonus cho từng hệ gene (element_type) ở từng tier
-- Chạy lệnh này trên MySQL trước khi khởi động server
-- ============================================================

CREATE TABLE IF NOT EXISTS `gene_tier_stat_config` (
  `element_type`  VARCHAR(10)  NOT NULL  COMMENT 'Tên hệ gene: Fire, Water, Earth, Metal, Wood',
  `tier_to`       TINYINT      NOT NULL  COMMENT 'Tier đạt được sau khi nâng cấp (2, 3, 4, 5)',
  `hp_bonus`      INT          NOT NULL  DEFAULT 0  COMMENT 'Bonus MaxHp cộng thêm khi đạt tier này',
  `mp_bonus`      INT          NOT NULL  DEFAULT 0  COMMENT 'Bonus MaxMp cộng thêm khi đạt tier này',
  `attack_bonus`  INT          NOT NULL  DEFAULT 0  COMMENT 'Bonus Attack cộng thêm khi đạt tier này',
  `defense_bonus` INT          NOT NULL  DEFAULT 0  COMMENT 'Bonus Defense cộng thêm khi đạt tier này',
  PRIMARY KEY (`element_type`, `tier_to`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4
  COMMENT='Config stat bonus cho gene upgrade — mỗi hệ có chỉ số riêng theo tier';

-- ============================================================
-- Seed data: 5 hệ × 4 tier (tier_to = 2, 3, 4, 5)
-- Cột: element_type | tier_to | hp_bonus | mp_bonus | attack_bonus | defense_bonus
-- ============================================================

-- Hệ Hỏa (Fire) — tấn công cao
INSERT INTO `gene_tier_stat_config` VALUES
  ('Fire', 2,  200,  50,  25, 8),
  ('Fire', 3,  400, 100,  50, 15),
  ('Fire', 4,  800, 200, 100, 30),
  ('Fire', 5, 1500, 400, 180, 60);

-- Hệ Thủy (Water) — máu và mana cao
INSERT INTO `gene_tier_stat_config` VALUES
  ('Water', 2,  280,  80,  15, 10),
  ('Water', 3,  560, 160,  30, 20),
  ('Water', 4, 1100, 320,  60, 40),
  ('Water', 5, 2000, 600, 110, 80);

-- Hệ Thổ (Earth) — phòng thủ cao
INSERT INTO `gene_tier_stat_config` VALUES
  ('Earth', 2,  250,  40,  12, 20),
  ('Earth', 3,  500,  80,  25, 40),
  ('Earth', 4,  900, 160,  50, 80),
  ('Earth', 5, 1600, 300,  90, 150);

-- Hệ Kim (Metal) — cân bằng tấn công và phòng thủ
INSERT INTO `gene_tier_stat_config` VALUES
  ('Metal', 2,  220,  50,  20, 15),
  ('Metal', 3,  440, 100,  40, 30),
  ('Metal', 4,  850, 200,  80, 60),
  ('Metal', 5, 1550, 380, 145, 110);

-- Hệ Mộc (Wood) — mana và hỗ trợ
INSERT INTO `gene_tier_stat_config` VALUES
  ('Wood', 2,  240,  70,  10, 12),
  ('Wood', 3,  480, 140,  20, 25),
  ('Wood', 4,  900, 280,  40, 50),
  ('Wood', 5, 1600, 520,  75, 95);
