-- ============================================================
-- Migration: Tạo bảng player2_data (Hệ Gene Thứ 2)
-- Chạy file này trên DB để thêm bảng player2_data.
-- player2_data lưu toàn bộ dữ liệu nhân vật cho hệ gene thứ 2:
--   skill, tiềm năng, kinh nghiệm, trang bị, inventory, ...
-- Điều kiện tạo: player_data.info_char.secondary_element != null
-- ============================================================

CREATE TABLE IF NOT EXISTS `player2_data` (
  `player_id`       int(11) NOT NULL COMMENT 'FK → player_data.player_id (cùng user)',
  `character_name`  varchar(50)  NOT NULL DEFAULT '',
  `gender`          varchar(10)  NOT NULL DEFAULT 'Male',
  `info_char`       longtext     NOT NULL DEFAULT '{}' COMMENT 'JSON InfoChar: level, exp, element_type, gene_tier, gene_exp, skills_points, potential_points, hp, mp, map_id, position...',
  `equipment`       longtext     NOT NULL DEFAULT '{}' COMMENT 'JSON trang bị đang mặc',
  `inventory`       longtext     NOT NULL DEFAULT '[]' COMMENT 'JSON danh sách vật phẩm túi đồ',
  `skills`          longtext     NOT NULL DEFAULT '[]' COMMENT 'JSON danh sách skill đã học',
  `potential_stats` longtext     NOT NULL DEFAULT '[]' COMMENT 'JSON tiềm năng đã phân bổ',
  `active_buffs`    longtext     NOT NULL DEFAULT '[]' COMMENT 'JSON buff đang active',
  `updated_at`      datetime     NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Dữ liệu nhân vật hệ gene thứ 2 — dùng chung player_id với player_data';

ALTER TABLE `player2_data`
  ADD PRIMARY KEY (`player_id`);

ALTER TABLE `player2_data`
  ADD CONSTRAINT `fk_p2d_player` FOREIGN KEY (`player_id`)
    REFERENCES `player_data` (`player_id`)
    ON DELETE CASCADE ON UPDATE CASCADE;
