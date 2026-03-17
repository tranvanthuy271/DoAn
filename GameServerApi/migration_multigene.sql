-- ============================================================
-- MIGRATION: Multi-Gene + Hybrid Gene Fusion System
-- Version: 1.0  |  2026-03-14
-- Chạy file này một lần trên database hiện có
-- ============================================================

-- ============================================================
-- BẢNG 1: gene_multi_config
-- Config nâng cấp hệ GEN THỨ 2 (secondary gene).
-- Chi phí cao hơn ~20% so với gene_upgrade_config.
-- ============================================================
CREATE TABLE IF NOT EXISTS `gene_multi_config` (
  `tier_from`         tinyint(4)  NOT NULL COMMENT '1~4: tier hiện tại của hệ phụ',
  `element_type`      varchar(10) NOT NULL COMMENT 'Fire|Water|Earth|Metal|Wood',
  `gene_exp_required` int(11)     NOT NULL COMMENT 'gene_exp cần tích luỹ trước khi nâng',
  `silver_cost`       int(11)     NOT NULL COMMENT 'vàng (gold) tiêu hao',
  `stone_id`          int(11)     NOT NULL COMMENT 'FK → item_template.id (Linh Thạch)',
  `stone_needed`      tinyint(4)  NOT NULL COMMENT 'số đá để đạt tỉ lệ thành công tối đa',
  `stone_min`         tinyint(4)  NOT NULL COMMENT 'số đá tối thiểu',
  `base_success_rate` float       NOT NULL COMMENT '0.0~1.0 khi dùng đủ stone_needed',
  PRIMARY KEY (`tier_from`, `element_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Config nâng cấp hệ gene thứ hai (secondary element)';

-- Điền dữ liệu: lấy từ gene_upgrade_config, tăng 20% chi phí
INSERT INTO `gene_multi_config`
  (`tier_from`, `element_type`, `gene_exp_required`, `silver_cost`,
   `stone_id`, `stone_needed`, `stone_min`, `base_success_rate`)
SELECT
  `tier_from`,
  `element_type`,
  FLOOR(`gene_exp_required` * 1.2) AS `gene_exp_required`,
  FLOOR(`silver_cost`       * 1.2) AS `silver_cost`,
  `stone_id`,
  `stone_needed`,
  `stone_min`,
  `base_success_rate`
FROM `gene_upgrade_config`;

-- ============================================================
-- BẢNG 2: gene_hybrid_config
-- Config cho 10 tổ hợp Hybrid (5 hệ chọn 2, không phân biệt thứ tự).
-- Lưu ý: element_a < element_b theo thứ tự alphabet để đảm bảo unique.
-- ============================================================
CREATE TABLE IF NOT EXISTS `gene_hybrid_config` (
  `hybrid_id`              int(11)      NOT NULL AUTO_INCREMENT,
  `element_a`              varchar(10)  NOT NULL COMMENT 'Hệ A (alphabet nhỏ hơn)',
  `element_b`              varchar(10)  NOT NULL COMMENT 'Hệ B (alphabet lớn hơn)',
  `hybrid_name`            varchar(100) NOT NULL COMMENT 'Tên gene lai',
  `hybrid_description`     varchar(500) DEFAULT NULL,
  `bonus_target_elements`  varchar(100) NOT NULL COMMENT 'CSV hệ bị sát thương tăng (union hai hệ khắc)',
  `immune_elements`        varchar(100) NOT NULL COMMENT 'CSV hệ không còn khắc được player',
  `fusion_silver_cost`     int(11)      NOT NULL DEFAULT 2000000 COMMENT 'vàng tiêu hao khi fusion',
  `fusion_item_id`         int(11)      NOT NULL COMMENT 'FK → item_template.id',
  `fusion_item_count`      int(11)      NOT NULL DEFAULT 5 COMMENT 'số item cần để fusion',
  `atk_bonus_percent`      float        NOT NULL DEFAULT 0.5 COMMENT '+50% ATK lên bonus_target_elements',
  `stat_bonus_hp`          int(11)      NOT NULL DEFAULT 2000 COMMENT 'HP bonus khi fusion',
  `stat_bonus_mp`          int(11)      NOT NULL DEFAULT 500  COMMENT 'MP bonus khi fusion',
  `stat_bonus_atk`         int(11)      NOT NULL DEFAULT 500  COMMENT 'ATK bonus khi fusion',
  `stat_bonus_def`         int(11)      NOT NULL DEFAULT 200  COMMENT 'DEF bonus khi fusion',
  PRIMARY KEY (`hybrid_id`),
  UNIQUE KEY `uk_combo` (`element_a`, `element_b`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
COMMENT='Config 10 tổ hợp Hybrid Gene';

-- -----------------------------------------------------------------
-- Ngũ Hành tương khắc:
--   Kim(Metal) → khắc → Mộc(Wood)
--   Mộc(Wood)  → khắc → Thủy(Water)
--   Thủy(Water)→ khắc → Hỏa(Fire)
--   Hỏa(Fire)  → khắc → Thổ(Earth)
--   Thổ(Earth) → khắc → Kim(Metal)
--
-- Hybrid bonus_target  = hệ mà elementA khắc + hệ mà elementB khắc
-- Hybrid immune        = hệ khắc elementA + hệ khắc elementB
--
-- Ví dụ Fire+Water:
--   Fire khắc Earth, Water khắc Fire
--   bonus_target = Earth,Fire
--   immune       = Water (khắc Fire) + Metal (khắc Wood nhưng Water bị Metal khắc? 
--                  Wood khắc Water nên khắc Water là Metal) → Water,Metal
--   Nên:  immune = Water,Metal
-- -----------------------------------------------------------------
INSERT INTO `gene_hybrid_config`
  (`element_a`, `element_b`, `hybrid_name`, `hybrid_description`,
   `bonus_target_elements`, `immune_elements`,
   `fusion_silver_cost`, `fusion_item_id`, `fusion_item_count`,
   `atk_bonus_percent`, `stat_bonus_hp`, `stat_bonus_mp`, `stat_bonus_atk`, `stat_bonus_def`)
VALUES
-- 1. Earth + Fire = Dung Nham (Volcanic Earth)
--    Fire khắc Earth  → bonus: Earth
--    Earth khắc Metal → bonus: Metal
--    bonus_target: Earth,Metal
--    Khắc Fire: Water; Khắc Earth: Wood → immune: Water,Wood
('Earth','Fire','Dung Nham Địa Hỏa','Tanker lửa đất, phản đòn thiêu đốt và cứng như đá',
 'Earth,Metal','Water,Wood', 2000000, 20, 5, 0.50, 2500, 400, 400, 300),

-- 2. Earth + Metal = Thổ Kim Hợp Nhất
--    Earth khắc Metal → bonus: Metal
--    Metal khắc Wood  → bonus: Wood
--    bonus_target: Metal,Wood
--    Khắc Earth: Wood; Khắc Metal: Fire → immune: Wood,Fire
('Earth','Metal','Thổ Kim Bất Hoại','Phòng thủ tối thượng, counterattack chí mạng',
 'Metal,Wood','Wood,Fire', 2000000, 20, 5, 0.50, 3000, 300, 300, 500),

-- 3. Earth + Water = Băng Địa
--    Water khắc Fire → bonus: Fire
--    Earth khắc Metal → bonus: Metal
--    bonus_target: Fire,Metal
--    Khắc Water: Metal; Khắc Earth: Wood → immune: Metal,Wood
('Earth','Water','Băng Địa Phong','Siêu tanker băng đất, miễn nhiễm vật lý và lửa',
 'Fire,Metal','Metal,Wood', 2000000, 20, 5, 0.50, 3500, 500, 200, 400),

-- 4. Earth + Wood = Địa Mộc Sinh
--    Wood khắc Water  → bonus: Water
--    Earth khắc Metal → bonus: Metal
--    bonus_target: Water,Metal
--    Khắc Wood: Metal; Khắc Earth: Wood → immune: Metal,Wood
('Earth','Wood','Địa Mộc Vĩnh Cửu','Kiểm soát bản đồ, hồi máu và trói địch',
 'Water,Metal','Metal,Wood', 2000000, 20, 5, 0.50, 2000, 600, 300, 300),

-- 5. Fire + Metal = Kim Hỏa Luyện
--    Fire khắc Earth  → bonus: Earth
--    Metal khắc Wood  → bonus: Wood
--    bonus_target: Earth,Wood
--    Khắc Fire: Water; Khắc Metal: Fire → immune: Water,Fire
('Fire','Metal','Kim Hỏa Phong Thần','Xuyên giáp thiêu đốt, chí mạng bốc lửa',
 'Earth,Wood','Water,Fire', 2000000, 20, 5, 0.50, 1500, 400, 700, 200),

-- 6. Fire + Water = Hỏa Thủy Long
--    Fire khắc Earth → bonus: Earth
--    Water khắc Fire → bonus: Fire
--    bonus_target: Earth,Fire
--    Khắc Fire: Water; Khắc Water: Metal → immune: Water,Metal
('Fire','Water','Hỏa Thủy Long','Sức mạnh hỗn độn giữa lửa và nước vũ trụ',
 'Earth,Fire','Water,Metal', 2000000, 20, 5, 0.50, 2000, 500, 500, 200),

-- 7. Fire + Wood = Hỏa Mộc Thiêu
--    Fire khắc Earth → bonus: Earth
--    Wood khắc Water → bonus: Water
--    bonus_target: Earth,Water
--    Khắc Fire: Water; Khắc Wood: Metal → immune: Water,Metal
('Fire','Wood','Hỏa Mộc Liên Sinh','Đốt cháy và tái sinh, DoT liên tục + AoE',
 'Earth,Water','Water,Metal', 2000000, 20, 5, 0.50, 1500, 500, 600, 150),

-- 8. Metal + Water = Băng Kim
--    Metal khắc Wood → bonus: Wood
--    Water khắc Fire → bonus: Fire
--    bonus_target: Wood,Fire
--    Khắc Metal: Fire; Khắc Water: Metal → immune: Fire,Metal
('Metal','Water','Băng Kim Xuyên Phá','Xuyên giáp đóng băng, sát thương lạnh ngắt',
 'Wood,Fire','Fire,Metal', 2000000, 20, 5, 0.50, 1500, 400, 600, 250),

-- 9. Metal + Wood = Gai Kim Độc
--    Metal khắc Wood → bonus: Wood
--    Wood khắc Water → bonus: Water
--    bonus_target: Wood,Water
--    Khắc Metal: Fire; Khắc Wood: Metal → immune: Fire,Metal
('Metal','Wood','Kim Mộc Gai Độc','Chí mạng vật lý + độc tố liên tục, phản đòn',
 'Wood,Water','Fire,Metal', 2000000, 20, 5, 0.50, 1000, 400, 700, 200),

-- 10. Water + Wood = Băng Độc
--    Water khắc Fire → bonus: Fire
--    Wood khắc Water → bonus: Water
--    bonus_target: Fire,Water
--    Khắc Water: Metal; Khắc Wood: Metal → immune: Metal (gộp lại)
('Water','Wood','Băng Độc Vĩnh Hằng','Đóng băng + độc tố, kiểm soát hoàn toàn',
 'Fire,Water','Metal,Fire', 2000000, 20, 5, 0.50, 1500, 600, 500, 200);

-- ============================================================
-- ITEM: Lõi Đột Biến (Mutant Core) — dùng để Fusion
-- id=20 đã được định nghĩa trong gene_upgrade_config tier 4→5
-- Nếu chưa có, thêm item loại gene stone (type=25)
-- ============================================================
INSERT IGNORE INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`,
   `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`)
VALUES
(31, 'Lõi Đột Biến', 'Vật liệu hiếm để Hybrid Fusion 2 gene Tier 5.
Chỉ rơi từ Boss hoặc sự kiện đặc biệt.',
 'True', 2, 25, 0, 0, 50, 0, 5, 0);

-- Cập nhật gene_hybrid_config dùng item id=31 (Lõi Đột Biến)
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 31;

-- ============================================================
-- KIỂM TRA
-- ============================================================
-- SELECT * FROM gene_multi_config ORDER BY element_type, tier_from;
-- SELECT hybrid_name, element_a, element_b, bonus_target_elements, immune_elements FROM gene_hybrid_config;
-- SELECT * FROM item_template WHERE id = 31;
