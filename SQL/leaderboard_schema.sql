-- =============================================================
-- Leaderboard Schema Migration (v2)
-- Chạy một lần trên database gamedb
-- =============================================================

-- Một bảng duy nhất lưu toàn bộ bảng xế hạng (giống pattern bangxephang)
-- id: 1=Cấp Độ / 2=Nhiệm Vụ / 3=Chuyên Cần / 4=Phó Bản / 5=Vàng
-- list: JSON array của ranked entries […], được server tính và lưu của các lần refresh
CREATE TABLE IF NOT EXISTS `leaderboard_cache` (
  `id`         int(11)      NOT NULL,
  `name`       varchar(100) NOT NULL DEFAULT '',
  `list`       longtext     NOT NULL DEFAULT '[]',
  `updated_at` datetime     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci ROW_FORMAT=DYNAMIC;

-- Seed 5 danh mục
INSERT IGNORE INTO `leaderboard_cache` (`id`, `name`, `list`) VALUES
(1, 'Cấp Độ',    '[]'),
(2, 'Nhiệm Vụ',   '[]'),
(3, 'Chuyên Cần', '[]'),
(4, 'Phó Bản',  '[]'),
(5, 'Vàng',       '[]');
