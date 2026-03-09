-- ============================================================
-- GAME DATABASE  v3.0  –  Thiết kế theo chuẩn LangLaServer
-- ============================================================
-- Hệ thống trang bị  (6 slot):
--   Weapon  │ Helmet │ Armor │ Pants │ Boots │ Ring
--   • Weapon  : phân theo hệ nguyên tố idClass (0=All 1=Fire 2=Water 3=Earth 4=Metal 5=Wood)
--   • 4 slot giáp (Helmet/Armor/Pants/Boots) : phân gioiTinh  0=Male 1=Female
--   • Ring   : gioiTinh=2 (All)
--
-- item_template.type  constants
--   0=Helmet  1=Weapon  2=Armor  3=Pants  4=Boots  5=Ring
--   21=UpgradeStone  22=HPPotion  23=MPPotion  24=Food
--   25=GeneStone     30=Material
--
-- option_template.type  constants
--   0=Weapon-base   2=Armor/Ring-base
--   3=(+4)unlock    4=(+8)unlock   5=(+12)unlock   6=(+16)unlock
--
-- option_template.level  = bậc nâng cấp tối thiểu để option ACTIVE
--   Unity: upgradeLevel < option.level  → hiển thị DIM (nhạt màu)
--          upgradeLevel >= option.level → hiển thị BRIGHT (đậm màu)
--
-- option_template.strOption  = 20 giá trị cách nhau ';'
--   strOption[N] = tổng giá trị stat khi item ở bậc +N
--
-- Hệ thống nâng cấp trang bị  (+1~+20)
--   Cần bạc + đá.  Số đá < stone_needed → % thành công thấp hơn.
--   Công thức: rate = base_rate * (actual_stones / stone_needed) [+ bonus Đá May Mắn]
--   Từ +7 trở lên có thể vỡ (xuống level) khi thất bại.
--
-- Hệ thống gene  (tier 1~5)
--   gene_upgrade_config : per (tier_from, element_type)
--   Cần Linh Thạch + bạc + đủ gene_exp ngưỡng.
--   Tinh Chất X : bổ sung gene_exp cho hệ tương ứng.
-- ============================================================

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";
/*!40101 SET NAMES utf8mb4 */;

-- ============================================================
-- DROP tables (an toàn khi chạy lại)
-- ============================================================
SET FOREIGN_KEY_CHECKS = 0;
DROP TABLE IF EXISTS `gene_upgrade_config`;
DROP TABLE IF EXISTS `equipment_upgrade_config`;
DROP TABLE IF EXISTS `stat_option_template`;
DROP TABLE IF EXISTS `option_template`;
DROP TABLE IF EXISTS `item_template`;
DROP TABLE IF EXISTS `enemy_spawns`;
DROP TABLE IF EXISTS `enemy`;
DROP TABLE IF EXISTS `skill_template`;
DROP TABLE IF EXISTS `exp_requirements`;
DROP TABLE IF EXISTS `map_config`;
DROP TABLE IF EXISTS `player_data`;
DROP TABLE IF EXISTS `users`;
SET FOREIGN_KEY_CHECKS = 1;

-- ============================================================
-- TABLE: users
-- ============================================================
CREATE TABLE `users` (
  `user_id`       int(11)      NOT NULL AUTO_INCREMENT,
  `username`      varchar(50)  NOT NULL,
  `email`         varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL COMMENT 'BCrypt hash',
  `created_at`    datetime     NOT NULL DEFAULT current_timestamp(),
  `last_login`    datetime     DEFAULT NULL,
  PRIMARY KEY (`user_id`),
  UNIQUE KEY `uk_username` (`username`),
  UNIQUE KEY `uk_email`    (`email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `users` VALUES
(1,'admin','admin@game.com','$2b$10$replaceme','2026-01-01 00:00:00',NULL),
(2,'player1','p1@game.com', '$2b$10$replaceme','2026-01-01 00:00:00',NULL),
(3,'player2','p2@game.com', '$2b$10$replaceme','2026-01-01 00:00:00',NULL);

-- ============================================================
-- TABLE: player_data
-- ============================================================
-- info_char JSON:
--   level, experience, gold, silver,
--   skill_points, potential_points,
--   element_type, gene_tier, gene_exp,
--   is_hybrid, secondary_element, secondary_gene_tier, secondary_gene_exp,
--   hp, max_hp, mp, max_mp, attack, defense,
--   map_id, position_x, position_y
--
-- equipment JSON (6 slots):
--   {
--     "weapon":  {"id":200,"upgradeLevel":0,"strOptions":"1,10;3,3"},
--     "helmet":  {"id":100,"upgradeLevel":0,"strOptions":"20,5;21,20"},
--     "armor":   {"id":110,"upgradeLevel":0,"strOptions":"20,5;21,20"},
--     "pants":   {"id":130,"upgradeLevel":0,"strOptions":"20,5;22,10"},
--     "boots":   {"id":150,"upgradeLevel":0,"strOptions":"20,5;23,3"},
--     "ring":    {"id":140,"upgradeLevel":0,"strOptions":"40,15;42,7"}
--   }
--   strOptions = "optionId,value;..." dựa vào strOption[upgradeLevel] từ option_template
--
-- inventory JSON: [{id, upgradeLevel, strOptions, amount, slotIndex, isEquipped}]
-- skills   JSON: [{skillCode, currentLevel, isEquipped, slotIndex}]
-- potential_stats JSON: {"attack":0,"hp":0,"mp":0,"defense":0,"gene":0}
-- ============================================================
CREATE TABLE `player_data` (
  `player_id`       int(11)   NOT NULL COMMENT 'FK → users.user_id',
  `character_name`  varchar(50) NOT NULL DEFAULT '',
  `gender`          enum('Male','Female') NOT NULL DEFAULT 'Male',
  `info_char`       longtext  CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `equipment`       longtext  CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `inventory`       longtext  CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `skills`          longtext  CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `potential_stats` longtext  CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `updated_at`      datetime  NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`player_id`),
  CONSTRAINT `fk_player_user` FOREIGN KEY (`player_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `player_data` (`player_id`,`character_name`,`gender`,`info_char`,`equipment`,`inventory`,`skills`,`potential_stats`,`updated_at`) VALUES
(1,'Hero1','Male',
 '{"level":1,"experience":0,"gold":0,"silver":5000,"skill_points":0,"potential_points":5,"element_type":"Fire","gene_tier":1,"gene_exp":0,"is_hybrid":false,"secondary_element":null,"secondary_gene_tier":null,"secondary_gene_exp":null,"hp":100,"max_hp":100,"mp":50,"max_mp":50,"attack":10,"defense":0,"map_id":0,"position_x":0.0,"position_y":0.0}',
 '{"weapon":{"id":200,"upgradeLevel":0,"strOptions":"1,10;3,3"},"helmet":{"id":100,"upgradeLevel":0,"strOptions":"20,5;21,20"},"armor":{"id":110,"upgradeLevel":0,"strOptions":"20,5;21,20"},"pants":{"id":130,"upgradeLevel":0,"strOptions":"20,5;22,10"},"boots":{"id":150,"upgradeLevel":0,"strOptions":"20,5;23,3"},"ring":{"id":140,"upgradeLevel":0,"strOptions":"40,15;42,7"}}',
 '[{"id":1,"upgradeLevel":0,"strOptions":"","amount":20,"slotIndex":0,"isEquipped":false},{"id":11,"upgradeLevel":0,"strOptions":"","amount":5,"slotIndex":1,"isEquipped":false},{"id":17,"upgradeLevel":0,"strOptions":"","amount":3,"slotIndex":2,"isEquipped":false}]',
 '[{"skillCode":"FIRE_BALL","currentLevel":1,"isEquipped":true,"slotIndex":0},{"skillCode":"DASH","currentLevel":1,"isEquipped":true,"slotIndex":1}]',
 '{"attack":0,"hp":0,"mp":0,"defense":0,"gene":0}',
 '2026-03-08 00:00:00'),
(2,'Hero2','Female',
 '{"level":1,"experience":0,"gold":0,"silver":3000,"skill_points":0,"potential_points":5,"element_type":"Water","gene_tier":1,"gene_exp":0,"is_hybrid":false,"secondary_element":null,"secondary_gene_tier":null,"secondary_gene_exp":null,"hp":100,"max_hp":100,"mp":50,"max_mp":50,"attack":10,"defense":0,"map_id":0,"position_x":2.0,"position_y":0.0}',
 '{"weapon":{"id":205,"upgradeLevel":0,"strOptions":"1,10;3,3"},"helmet":{"id":105,"upgradeLevel":0,"strOptions":"20,5;21,20"},"armor":{"id":115,"upgradeLevel":0,"strOptions":"20,5;21,20"},"pants":{"id":135,"upgradeLevel":0,"strOptions":"20,5;22,10"},"boots":{"id":155,"upgradeLevel":0,"strOptions":"20,5;23,3"},"ring":{"id":140,"upgradeLevel":0,"strOptions":"40,15;42,7"}}',
 '[{"id":14,"upgradeLevel":0,"strOptions":"","amount":10,"slotIndex":0,"isEquipped":false}]',
 '[{"skillCode":"WATER_SHIELD","currentLevel":1,"isEquipped":true,"slotIndex":0}]',
 '{"attack":0,"hp":0,"mp":0,"defense":0,"gene":0}',
 '2026-03-08 00:00:00');

-- ============================================================
-- TABLE: item_template  (LangLa-inspired)
-- ============================================================
-- id        : unique item ID (unsigned)
-- name      : tên item
-- detail    : mô tả chi tiết
-- isXepChong: 'True'=stackable  'False'=non-stackable
-- gioiTinh  : 0=Male  1=Female  2=All
-- type      : loại slot / loại item (xem constants ở đầu file)
-- idClass   : 0=tất cả  1=Fire  2=Water  3=Earth  4=Metal  5=Wood
--             Chỉ có Weapon mới dùng idClass để giới hạn hệ nguyên tố
-- idIcon    : ID icon trong Unity Resources/ItemIcons/{idIcon}
--             → Admin tự config idIcon sau
-- levelNeed : level nhân vật tối thiểu để trang bị
-- taiPhuNeed: uy tín / prestige tối thiểu (0=không yêu cầu)
-- idMob     : mob nào drop item (-1=không drop từ mob, 0=bất kỳ)
-- idChar    : appearance/animation index
-- ============================================================
CREATE TABLE `item_template` (
  `id`         int(11) UNSIGNED NOT NULL AUTO_INCREMENT,
  `name`       varchar(200)     NOT NULL,
  `detail`     varchar(500)     DEFAULT NULL,
  `isXepChong` varchar(5)       NOT NULL DEFAULT 'False',
  `gioiTinh`   tinyint(4)       NOT NULL DEFAULT 2  COMMENT '0=Male 1=Female 2=All',
  `type`       tinyint(4)       NOT NULL             COMMENT '0=Helmet 1=Weapon 2=Armor 3=Pants 4=Boots 5=Ring 21=UpgStone 22=HPPotion 23=MPPotion 24=Food 25=GeneStone 30=Material',
  `idClass`    tinyint(4)       NOT NULL DEFAULT 0   COMMENT '0=All 1=Fire 2=Water 3=Earth 4=Metal 5=Wood (vũ khí)',
  `idIcon`     int(11)          NOT NULL DEFAULT 0   COMMENT 'Admin tự config idIcon trong Unity',
  `levelNeed`  smallint(6)      NOT NULL DEFAULT 1,
  `taiPhuNeed` smallint(6)      NOT NULL DEFAULT 0,
  `idMob`      int(11)          NOT NULL DEFAULT -1,
  `idChar`     int(11)          NOT NULL DEFAULT 0,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- -------------------------------------------------------
-- ĐÁ NÂNG CẤP  (type=21, stackable, all gender)
-- -------------------------------------------------------
INSERT INTO `item_template` (`id`,`name`,`detail`,`isXepChong`,`gioiTinh`,`type`,`idClass`,`idIcon`,`levelNeed`,`taiPhuNeed`,`idMob`,`idChar`) VALUES
( 1,'Đá Nâng Cấp Cấp 1','Dùng để nâng cấp trang bị +1~+3','True',2,21,0,0, 1,0,-1,0),
( 2,'Đá Nâng Cấp Cấp 2','Dùng để nâng cấp trang bị +4~+6','True',2,21,0,0, 1,0,-1,0),
( 3,'Đá Nâng Cấp Cấp 3','Dùng để nâng cấp trang bị +7~+9','True',2,21,0,0, 1,0,-1,0),
( 4,'Đá Nâng Cấp Cấp 4','Dùng để nâng cấp trang bị +10~+12','True',2,21,0,0,20,0,-1,0),
( 5,'Đá Nâng Cấp Cấp 5','Dùng để nâng cấp trang bị +13~+15','True',2,21,0,0,30,0,-1,0),
( 6,'Đá Nâng Cấp Cấp 6','Dùng để nâng cấp trang bị +16~+18','True',2,21,0,0,40,0,-1,0),
( 7,'Đá Nâng Cấp Cấp 7','Dùng để nâng cấp trang bị +19~+20','True',2,21,0,0,50,0,-1,0),
( 8,'Đá May Mắn','Tăng thêm 15% tỉ lệ thành công mỗi viên','True',2,21,0,0, 1,0,-1,0),
( 9,'Đá Bảo Vệ','Ngăn trang bị bị vỡ khi thất bại (dùng từ +7)','True',2,21,0,0,10,0,-1,0),
(10,'Đá Hồi Phục','Khôi phục level trang bị về trước khi vỡ','True',2,21,0,0,15,0,-1,0);

-- -------------------------------------------------------
-- BÌNH HP / MP  (type=22/23, stackable)
-- -------------------------------------------------------
INSERT INTO `item_template` VALUES
(11,'Bình HP Nhỏ', 'Hồi phục 100 HP tức thì', 'True',2,22,0,0, 1,0,-1,0),
(12,'Bình HP Vừa', 'Hồi phục 350 HP tức thì', 'True',2,22,0,0,15,0,-1,0),
(13,'Bình HP Lớn', 'Hồi phục 900 HP tức thì', 'True',2,22,0,0,30,0,-1,0),
(14,'Bình MP Nhỏ', 'Hồi phục 60 MP tức thì',  'True',2,23,0,0, 1,0,-1,0),
(15,'Bình MP Vừa', 'Hồi phục 200 MP tức thì', 'True',2,23,0,0,15,0,-1,0),
(16,'Bình MP Lớn', 'Hồi phục 500 MP tức thì', 'True',2,23,0,0,30,0,-1,0);

-- -------------------------------------------------------
-- LINH THẠCH  (type=25, stackable)
-- Dùng để nâng cấp gene tier.  idClass = hệ nguyên tố (Tinh Chất)
-- Tinh Chất: khi sử dụng bổ sung 500 gene_exp cho đúng hệ
-- -------------------------------------------------------
INSERT INTO `item_template` VALUES
(17,'Linh Thạch Sơ Cấp',     'Nguyên liệu nâng gene tier 1→2',    'True',2,25,0,0, 1,0,-1,0),
(18,'Linh Thạch Trung Cấp',  'Nguyên liệu nâng gene tier 2→3',    'True',2,25,0,0,15,0,-1,0),
(19,'Linh Thạch Cao Cấp',    'Nguyên liệu nâng gene tier 3→4',    'True',2,25,0,0,30,0,-1,0),
(20,'Linh Thạch Thượng Cấp', 'Nguyên liệu nâng gene tier 4→5',    'True',2,25,0,0,45,0,-1,0),
(21,'Tinh Chất Hỏa Nguyên',  'Bổ sung 500 gene_exp hệ Hỏa',       'True',2,25,1,0, 5,0,-1,0),
(22,'Tinh Chất Thủy Nguyên', 'Bổ sung 500 gene_exp hệ Thủy',      'True',2,25,2,0, 5,0,-1,0),
(23,'Tinh Chất Thổ Nguyên',  'Bổ sung 500 gene_exp hệ Thổ',       'True',2,25,3,0, 5,0,-1,0),
(24,'Tinh Chất Kim Nguyên',  'Bổ sung 500 gene_exp hệ Kim',       'True',2,25,4,0, 5,0,-1,0),
(25,'Tinh Chất Mộc Nguyên',  'Bổ sung 500 gene_exp hệ Mộc',      'True',2,25,5,0, 5,0,-1,0);

-- -------------------------------------------------------
-- NGUYÊN LIỆU  (type=30, stackable)
-- -------------------------------------------------------
INSERT INTO `item_template` VALUES
(26,'Quặng Sắt',    'Nguyên liệu rèn đồ cơ bản',   'True',2,30,0,0, 1,0, 1,0),
(27,'Thảo Dược',    'Chế bình máu',                 'True',2,30,0,0, 1,0, 1,0),
(28,'Vảy Rồng',     'Nguyên liệu quý hiếm',         'True',2,30,0,0,30,0, 5,0),
(29,'Nanh Độc',     'Drop từ Goblin Độc',            'True',2,30,0,0,10,0, 2,0),
(30,'Tinh Thể Lửa', 'Drop từ Fire Slime',            'True',2,30,0,0, 5,0, 4,0);

-- ============================================================
-- THIẾT BỊ  (type = 0,2,3,4,5)
-- ============================================================

-- -------------------------------------------------------
-- MŨ  type=0  |  Nam (gioiTinh=0)  |  Nữ (gioiTinh=1)
-- Tier 1 levelNeed=1  T2=10  T3=20  T4=35  T5=50
-- -------------------------------------------------------
INSERT INTO `item_template` VALUES
(100,'Mũ Da Nam',          'Mũ da cơ bản, thích hợp nam lính mới',      'False',0,0,0,0, 1,0,-1,0),
(101,'Mũ Sắt Nam',         'Mũ sắt bền, bảo vệ hiệu quả',               'False',0,0,0,0,10,0,-1,0),
(102,'Mũ Thép Nam',        'Mũ thép vững chắc của chiến binh',           'False',0,0,0,0,20,0,-1,0),
(103,'Mũ Chiến Binh Nam',  'Mũ cao cấp của chiến binh tinh nhuệ',        'False',0,0,0,0,35,0,-1,0),
(104,'Mũ Tinh Luyện Nam',  'Mũ tinh luyện bằng thuật nguyên tố',        'False',0,0,0,0,50,0,-1,0),
(105,'Mũ Lụa Nữ',          'Mũ lụa nhẹ nhàng dành cho nữ chiến binh',  'False',1,0,0,0, 1,0,-1,0),
(106,'Mũ Bạc Nữ',          'Mũ khảm bạc thanh lịch',                    'False',1,0,0,0,10,0,-1,0),
(107,'Mũ Ngọc Nữ',         'Mũ nạm ngọc quý, tăng cường ma lực',       'False',1,0,0,0,20,0,-1,0),
(108,'Mũ Nữ Chiến Binh',   'Mũ chiến đấu cao cấp dành cho nữ',         'False',1,0,0,0,35,0,-1,0),
(109,'Mũ Tinh Luyện Nữ',   'Mũ nữ tinh luyện bằng năng lượng tinh khiết','False',1,0,0,0,50,0,-1,0);

-- -------------------------------------------------------
-- ÁO GIÁP  type=2  |  Nam  |  Nữ
-- -------------------------------------------------------
INSERT INTO `item_template` VALUES
(110,'Áo Da Nam',          'Áo da cơ bản',                               'False',0,2,0,0, 1,0,-1,0),
(111,'Áo Sắt Nam',         'Áo giáp sắt rèn thủ công',                  'False',0,2,0,0,10,0,-1,0),
(112,'Áo Thép Nam',        'Áo giáp thép của lính tinh nhuệ',            'False',0,2,0,0,20,0,-1,0),
(113,'Áo Chiến Binh Nam',  'Áo giáp cao cấp',                            'False',0,2,0,0,35,0,-1,0),
(114,'Áo Tinh Luyện Nam',  'Áo tinh luyện, hấp thụ nguyên tố',          'False',0,2,0,0,50,0,-1,0),
(115,'Áo Lụa Nữ',          'Áo lụa nhẹ, linh hoạt trong chiến đấu',    'False',1,2,0,0, 1,0,-1,0),
(116,'Áo Bạc Nữ',          'Áo khảm bạc, cân bằng phòng thủ và tốc độ','False',1,2,0,0,10,0,-1,0),
(117,'Áo Ngọc Nữ',         'Áo nạm ngọc, tăng MP tối đa',               'False',1,2,0,0,20,0,-1,0),
(118,'Áo Nữ Chiến Binh',   'Áo chiến đấu cao cấp dành cho nữ',         'False',1,2,0,0,35,0,-1,0),
(119,'Áo Tinh Luyện Nữ',   'Áo nữ tinh luyện bằng ánh sao',             'False',1,2,0,0,50,0,-1,0);

-- -------------------------------------------------------
-- QUẦN  type=3  |  Nam  |  Nữ
-- -------------------------------------------------------
INSERT INTO `item_template` VALUES
(130,'Quần Da Nam',         'Quần da cơ bản',                            'False',0,3,0,0, 1,0,-1,0),
(131,'Quần Sắt Nam',        'Quần giáp sắt bảo vệ hông và đùi',         'False',0,3,0,0,10,0,-1,0),
(132,'Quần Thép Nam',       'Quần giáp thép vững chắc',                  'False',0,3,0,0,20,0,-1,0),
(133,'Quần Chiến Binh Nam', 'Quần giáp cao cấp',                         'False',0,3,0,0,35,0,-1,0),
(134,'Quần Tinh Luyện Nam', 'Quần tinh luyện, nhẹ mà bền',               'False',0,3,0,0,50,0,-1,0),
(135,'Quần Lụa Nữ',         'Quần lụa duyên dáng',                      'False',1,3,0,0, 1,0,-1,0),
(136,'Quần Bạc Nữ',         'Quần khảm bạc',                            'False',1,3,0,0,10,0,-1,0),
(137,'Quần Ngọc Nữ',        'Quần nạm ngọc quý',                        'False',1,3,0,0,20,0,-1,0),
(138,'Quần Nữ Chiến Binh',  'Quần chiến đấu cao cấp cho nữ',            'False',1,3,0,0,35,0,-1,0),
(139,'Quần Tinh Luyện Nữ',  'Quần nữ tinh luyện',                       'False',1,3,0,0,50,0,-1,0);

-- -------------------------------------------------------
-- GIÀY  type=4  |  Nam  |  Nữ
-- -------------------------------------------------------
INSERT INTO `item_template` VALUES
(150,'Giày Da Nam',         'Giày da cơ bản',                            'False',0,4,0,0, 1,0,-1,0),
(151,'Giày Sắt Nam',        'Giày sắt bảo vệ chân',                     'False',0,4,0,0,10,0,-1,0),
(152,'Giày Thép Nam',       'Giày thép vững chắc',                       'False',0,4,0,0,20,0,-1,0),
(153,'Giày Chiến Binh Nam', 'Giày cao cấp, tăng tốc độ',                 'False',0,4,0,0,35,0,-1,0),
(154,'Giày Tinh Luyện Nam', 'Giày tinh luyện từ nguyên tố phong',       'False',0,4,0,0,50,0,-1,0),
(155,'Giày Lụa Nữ',         'Giày lụa nhẹ nhàng',                       'False',1,4,0,0, 1,0,-1,0),
(156,'Giày Bạc Nữ',         'Giày khảm bạc xinh xắn',                   'False',1,4,0,0,10,0,-1,0),
(157,'Giày Ngọc Nữ',        'Giày nạm ngọc, tăng tốc độ di chuyển',    'False',1,4,0,0,20,0,-1,0),
(158,'Giày Nữ Chiến Binh',  'Giày chiến đấu cao cấp cho nữ',            'False',1,4,0,0,35,0,-1,0),
(159,'Giày Tinh Luyện Nữ',  'Giày nữ tinh luyện, đi như bay',           'False',1,4,0,0,50,0,-1,0);

-- -------------------------------------------------------
-- NHẪN  type=5  |  gioiTinh=2 (All)
-- -------------------------------------------------------
INSERT INTO `item_template` VALUES
(140,'Nhẫn Đá',            'Nhẫn đá thô, cơ bản nhất',                  'False',2,5,0,0, 1,0,-1,0),
(141,'Nhẫn Bạc',           'Nhẫn bạc, tăng chỉ số tổng thể',            'False',2,5,0,0,10,0,-1,0),
(142,'Nhẫn Vàng',          'Nhẫn vàng, tăng đáng kể HP và ATK',         'False',2,5,0,0,20,0,-1,0),
(143,'Nhẫn Ma',            'Nhẫn ám ma, chứa sức mạnh tối thượng',      'False',2,5,0,0,35,0,-1,0),
(144,'Nhẫn Huyền Thoại',   'Nhẫn huyền thoại, vượt qua mọi giới hạn',  'False',2,5,0,0,50,0,-1,0);

-- ============================================================
-- VŨ KHÍ  type=1  |  phân theo hệ (idClass 1~5)
-- Nhân vật chỉ có thể trang bị vũ khí đúng hệ nguyên tố mình
-- (server kiểm tra: player.element_type == weaponClass)
-- ============================================================

-- HỆ HỎA  idClass=1
INSERT INTO `item_template` VALUES
(200,'Kiếm Hỏa Sơ Cấp',     'Kiếm hỏa rèn từ quặng hồng, lửa nhỏ',       'False',2,1,1,0, 1,0,-1,0),
(201,'Kiếm Hỏa Trung Cấp',  'Lưỡi kiếm nung đỏ, toả nhiệt khi chém',      'False',2,1,1,0,10,0,-1,0),
(202,'Kiếm Hỏa Cao Cấp',    'Kiếm tôi trong dung nham, đỏ rực không tắt',  'False',2,1,1,0,20,0,-1,0),
(203,'Kiếm Hỏa Thần',       'Kiếm chứa ngọn lửa bất diệt của Thần Hỏa',   'False',2,1,1,0,35,0,-1,0),
(204,'Kiếm Hỏa Thượng Cấp', 'Kiếm tối cùng hệ Hỏa, đốt cháy linh hồn',   'False',2,1,1,0,50,0,-1,0);

-- HỆ THỦY  idClass=2
INSERT INTO `item_template` VALUES
(205,'Cung Thủy Sơ Cấp',    'Cung gỗ ngấm nước, mũi tên ướt đẫm',        'False',2,1,2,0, 1,0,-1,0),
(206,'Cung Thủy Trung Cấp', 'Cung thủy tinh, mũi tên xuyên bão nước',     'False',2,1,2,0,10,0,-1,0),
(207,'Cung Thủy Cao Cấp',   'Cung băng, đóng băng kẻ địch khi trúng',     'False',2,1,2,0,20,0,-1,0),
(208,'Cung Thủy Thần',      'Cung chứa sức mạnh đại dương',                'False',2,1,2,0,35,0,-1,0),
(209,'Cung Thủy Thượng Cấp','Cung tối cùng hệ Thủy, điều khiển thủy triều','False',2,1,2,0,50,0,-1,0);

-- HỆ THỔ  idClass=3
INSERT INTO `item_template` VALUES
(210,'Chùy Thổ Sơ Cấp',    'Chùy đất nung, nặng nề',                       'False',2,1,3,0, 1,0,-1,0),
(211,'Chùy Thổ Trung Cấp', 'Chùy granit, mỗi cú đánh rung chuyển đất',     'False',2,1,3,0,10,0,-1,0),
(212,'Chùy Thổ Cao Cấp',   'Chùy thiên thạch, sức mạnh nặng như núi',      'False',2,1,3,0,20,0,-1,0),
(213,'Chùy Thổ Thần',      'Chùy linh hồn đất đai cổ đại',                  'False',2,1,3,0,35,0,-1,0),
(214,'Chùy Thổ Thượng Cấp','Chùy tối cùng hệ Thổ, gây địa chấn',           'False',2,1,3,0,50,0,-1,0);

-- HỆ KIM  idClass=4
INSERT INTO `item_template` VALUES
(215,'Đao Kim Sơ Cấp',     'Đao sắt mài bén, phản chiếu ánh sáng',          'False',2,1,4,0, 1,0,-1,0),
(216,'Đao Kim Trung Cấp',  'Đao thép cao cấp, sắc bén tuyệt vời',            'False',2,1,4,0,10,0,-1,0),
(217,'Đao Kim Cao Cấp',    'Đao titanium – bén và không gỉ sét',              'False',2,1,4,0,20,0,-1,0),
(218,'Đao Kim Thần',       'Đao mang khí kim tinh nguyên tố',                 'False',2,1,4,0,35,0,-1,0),
(219,'Đao Kim Thượng Cấp', 'Đao tối cùng hệ Kim, chém xuyên mọi giáp',      'False',2,1,4,0,50,0,-1,0);

-- HỆ MỘC  idClass=5
INSERT INTO `item_template` VALUES
(220,'Gậy Mộc Sơ Cấp',    'Gậy gỗ rừng già, đơn giản hiệu quả',             'False',2,1,5,0, 1,0,-1,0),
(221,'Gậy Mộc Trung Cấp', 'Gậy trúc ma thuật, dẫn năng lượng cây cỏ',       'False',2,1,5,0,10,0,-1,0),
(222,'Gậy Mộc Cao Cấp',   'Gậy gỗ thiêng, rễ cây bện vào từng thớ',         'False',2,1,5,0,20,0,-1,0),
(223,'Gậy Mộc Thần',      'Gậy linh hồn đại thụ ngàn năm',                   'False',2,1,5,0,35,0,-1,0),
(224,'Gậy Mộc Thượng Cấp','Gậy tối cùng hệ Mộc, kết nối vũ trụ xanh',      'False',2,1,5,0,50,0,-1,0);

-- ============================================================
-- TABLE: option_template
-- ============================================================
-- type = 0 : vũ khí – base option
-- type = 2 : giáp / nhẫn – base option
-- type = 3 : mở khoá tại +4   (level=4)
-- type = 4 : mở khoá tại +8   (level=8)
-- type = 5 : mở khoá tại +12  (level=12)
-- type = 6 : mở khoá tại +16  (level=16)
--
-- level = min item.upgradeLevel để option ACTIVE
--
-- strOption : 20 giá trị, ';' phân cách
--   index 0 = +0,  index 1 = +1, …, index 19 = +19
--   Giá trị là TỔNG stat tại cấp đó (không phải delta)
--
-- *** HƯỚNG DẪN CONFIG UNITY ***
-- Trong ItemDetailPanel / EquipmentStatDisplay:
--   1. Parse item.strOptions → List<(optId, value)>
--   2. Với mỗi optId: tra option_template bằng optId
--   3. displayValue = strOption[item.upgradeLevel]
--   4. Nếu item.upgradeLevel < option.level → tô màu xám (dim)
--      Nếu item.upgradeLevel >= option.level → tô màu trắng/vàng (bright)
--   5. Hiển thị: "(+{option.level}) {option.name}" nếu option.level > 0
--                "{option.name}" nếu option.level == 0
--   Ví dụ: option.level=4, upgradeLevel=3 → "(+4) HP tối đa: +79" [dim]
--           option.level=4, upgradeLevel=5 → "(+4) HP tối đa: +99" [bright]
-- ============================================================
CREATE TABLE `option_template` (
  `id`        int(11)      NOT NULL,
  `name`      varchar(200) NOT NULL COMMENT '# = placeholder giá trị',
  `type`      tinyint(4)   NOT NULL DEFAULT 0,
  `level`     tinyint(4)   NOT NULL DEFAULT 0 COMMENT 'min upgradeLevel để kích hoạt',
  `strOption` longtext     NOT NULL            COMMENT '20 giá trị cách nhau ;',
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ------- VŨ KHÍ BASE  (type=0, level=0) -------
INSERT INTO `option_template` VALUES
( 1,'Tấn công: +#',                       0, 0,'10;13;17;22;28;35;43;53;65;80;97;117;140;168;201;241;289;347;416;500'),
( 2,'Xuyên giáp: +#',                     0, 0,'5;7;9;11;14;18;22;28;34;42;52;64;79;97;119;147;181;223;275;338'),
( 3,'Chí mạng: +#',                       0, 0,'3;4;5;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163;200'),
( 4,'Tấn công khi chí mạng: +#%',         0, 0,'2;2;3;3;4;5;6;7;9;11;13;16;19;23;28;34;41;50;61;75'),
( 5,'Sát thương quái: +#',                0, 0,'8;10;13;16;20;25;31;39;48;60;74;92;114;141;175;217;269;333;413;512'),
( 6,'Hút máu: +#%',                       0, 0,'1;1;1;1;2;2;2;2;2;3;3;3;3;3;4;4;4;4;4;5'),
( 7,'Chính xác: +#',                      0, 0,'5;6;8;10;13;16;20;25;31;38;47;58;72;89;110;136;168;208;257;317'),
( 8,'Tăng tấn công hệ Hỏa: +#',          0, 0,'5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
( 9,'Tăng tấn công hệ Thủy: +#',         0, 0,'5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
(10,'Tăng tấn công hệ Thổ: +#',          0, 0,'5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
(11,'Tăng tấn công hệ Kim: +#',          0, 0,'5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
(12,'Tăng tấn công hệ Mộc: +#',         0, 0,'5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252');

-- ------- VŨ KHÍ UNLOCK (+4) type=3 level=4 -------
INSERT INTO `option_template` VALUES
(13,'(+4) Tốc độ tấn công: +#%',         3, 4,'0;0;0;0;3;3;4;4;5;5;6;6;7;7;8;8;9;9;10;10'),
(14,'(+4) Bỏ qua né tránh: +#',          3, 4,'0;0;0;0;10;12;14;17;20;24;28;33;39;46;54;63;74;87;102;120');

-- ------- VŨ KHÍ UNLOCK (+8) type=4 level=8 -------
INSERT INTO `option_template` VALUES
(15,'(+8) Chí mạng: +#',                 4, 8,'0;0;0;0;0;0;0;0;15;18;22;27;32;38;45;53;63;74;87;103'),
(16,'(+8) Xuyên giáp: +#%',              4, 8,'0;0;0;0;0;0;0;0;5;5;6;6;7;7;8;8;9;9;10;10');

-- ------- VŨ KHÍ UNLOCK (+12) type=5 level=12 -------
INSERT INTO `option_template` VALUES
(17,'(+12) Phát huy tấn công cơ bản: +#%',5,12,'0;0;0;0;0;0;0;0;0;0;0;0;5;6;7;8;9;10;12;14');

-- ------- VŨ KHÍ UNLOCK (+16) type=6 level=16 -------
INSERT INTO `option_template` VALUES
(18,'(+16) Gây bỏng khi chí mạng: +#%', 6,16,'0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;5;5;6;6');

-- ------- GIÁP BASE  (type=2, level=0) -------
INSERT INTO `option_template` VALUES
(20,'Phòng thủ: +#',                     2, 0,'5;7;9;11;14;18;22;28;34;42;52;64;79;97;119;147;181;223;275;338'),
(21,'HP tối đa: +#',                     2, 0,'20;25;32;40;50;63;79;99;124;155;194;242;303;379;473;592;740;925;1156;1445'),
(22,'MP tối đa: +#',                     2, 0,'10;13;16;20;25;32;40;50;63;79;99;124;155;194;242;303;379;473;591;739'),
(23,'Né tránh: +#',                      2, 0,'3;4;5;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163;200'),
(24,'Kháng Hỏa: +#',                     2, 0,'3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(25,'Kháng Thủy: +#',                    2, 0,'3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(26,'Kháng Thổ: +#',                     2, 0,'3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(27,'Kháng Kim: +#',                     2, 0,'3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(28,'Kháng Mộc: +#',                    2, 0,'3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(29,'Giảm trừ sát thương: +#',           2, 0,'2;3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133'),
(30,'Phản đòn: +#%',                     2, 0,'1;1;1;1;1;1;2;2;2;2;2;2;3;3;3;3;3;3;4;4');

-- ------- GIÁP UNLOCK (+4) type=3 level=4 -------
INSERT INTO `option_template` VALUES
(31,'(+4) Hồi phục HP mỗi 0.5s: +#',    3, 4,'0;0;0;0;2;2;3;3;4;4;5;5;6;6;7;7;8;8;9;9'),
(32,'(+4) Hồi phục MP mỗi 0.5s: +#',    3, 4,'0;0;0;0;1;1;2;2;3;3;4;4;5;5;6;6;7;7;8;8'),
(33,'(+4) Tốc độ di chuyển: +#',         3, 4,'0;0;0;0;5;5;6;6;7;7;8;8;9;9;10;10;11;11;12;12');

-- ------- GIÁP UNLOCK (+8) type=4 level=8 -------
INSERT INTO `option_template` VALUES
(34,'(+8) HP tối đa: +#%',               4, 8,'0;0;0;0;0;0;0;0;3;3;4;4;5;5;6;6;7;7;8;8'),
(35,'(+8) Chí mạng: +#',                 4, 8,'0;0;0;0;0;0;0;0;10;12;15;18;21;25;30;36;43;51;61;73');

-- ------- GIÁP UNLOCK (+12) type=5 level=12 -------
INSERT INTO `option_template` VALUES
(36,'(+12) Phòng thủ: +#%',              5,12,'0;0;0;0;0;0;0;0;0;0;0;0;4;5;6;7;8;9;10;11'),
(37,'(+12) MP tối đa: +#%',              5,12,'0;0;0;0;0;0;0;0;0;0;0;0;3;4;5;6;7;8;9;10');

-- ------- GIÁP UNLOCK (+16) type=6 level=16 -------
INSERT INTO `option_template` VALUES
(38,'(+16) Kháng tất cả: +#',            6,16,'0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;20;24;29;35');

-- ------- NHẪN BASE  (type=2, level=0) -------
INSERT INTO `option_template` VALUES
(40,'HP tối đa: +#',                     2, 0,'15;19;24;30;37;47;58;73;91;114;143;179;224;280;350;438;548;685;856;1070'),
(41,'MP tối đa: +#',                     2, 0,'10;12;15;19;24;30;37;46;58;72;90;113;141;176;220;275;344;430;537;671'),
(42,'Tấn công: +#',                      2, 0,'7;9;11;14;17;21;27;33;42;52;65;81;101;127;158;198;248;310;387;484'),
(43,'Phòng thủ: +#',                     2, 0,'4;5;6;8;10;12;15;19;24;30;37;46;58;72;90;113;141;176;220;275'),
(44,'Chí mạng: +#',                      2, 0,'2;3;4;5;6;7;9;11;14;17;21;26;32;40;50;62;78;97;121;151'),
(45,'Né tránh: +#',                      2, 0,'2;3;4;5;6;7;9;11;14;17;21;26;32;40;50;62;78;97;121;151');

-- ------- NHẪN UNLOCK (+4) type=3 level=4 -------
INSERT INTO `option_template` VALUES
(46,'(+4) Kháng tất cả: +#',             3, 4,'0;0;0;0;5;6;7;8;10;12;14;17;20;24;29;35;42;50;60;72');

-- ------- NHẪN UNLOCK (+8) type=4 level=8 -------
INSERT INTO `option_template` VALUES
(47,'(+8) HP tối đa: +#%',               4, 8,'0;0;0;0;0;0;0;0;2;3;3;4;4;5;5;6;6;7;7;8'),
(48,'(+8) Tấn công: +#%',                4, 8,'0;0;0;0;0;0;0;0;2;2;3;3;4;4;5;5;6;6;7;7');

-- ============================================================
-- TABLE: equipment_upgrade_config
-- ============================================================
-- upgrade_level     : target level (+1~+20)
-- silver_cost       : bạc tiêu hao khi nâng cấp
-- stone_id          : đá nâng cấp cần dùng (FK → item_template.id)
-- stone_needed      : số đá để đạt base_success_rate (100% input)
-- stone_min         : số đá tối thiểu được phép dùng
-- base_success_rate : tỉ lệ thành công khi dùng đúng stone_needed viên
-- fail_policy       : 0=an toàn (không mất)  1=-1 bậc  2=về +0
--
-- *** CÔNG THỨC TỈ LỆ THÀNH CÔNG (server-side) ***
--   actual_rate = base_success_rate * min(actual_stones / stone_needed, 1.0)
--   + Mỗi Đá May Mắn (id=8) thêm +0.15, tối đa 1.0
--   + Nếu có Đá Bảo Vệ (id=9) và fail: bỏ qua fail_policy
--
-- *** HƯỚNG DẪN CONFIG UNITY (UI Nâng Cấp) ***
-- UpgradePanel.cs :
--   1. Hiển thị slot nhập số đá nâng cấp + Đá May Mắn + Đá Bảo Vệ
--   2. Tính và hiển thị actual_rate realtime khi người chơi nhập số đá
--   3. Nếu actual_stones < stone_min: disable nút nâng cấp + hiện cảnh báo
--   4. Khi thất bại và fail_policy=1 và không có Đá Bảo Vệ:
--        item.upgradeLevel -= 1  (min 0)
-- ============================================================
CREATE TABLE `equipment_upgrade_config` (
  `upgrade_level`     tinyint(4) NOT NULL  COMMENT '+1 ~ +20',
  `silver_cost`       int(11)    NOT NULL,
  `stone_id`          int(11)    NOT NULL  COMMENT 'FK → item_template.id',
  `stone_needed`      tinyint(4) NOT NULL  COMMENT 'đá cần dùng cho tỉ lệ base',
  `stone_min`         tinyint(4) NOT NULL  COMMENT 'đá tối thiểu',
  `base_success_rate` float      NOT NULL,
  `fail_policy`       tinyint(1) NOT NULL DEFAULT 0 COMMENT '0=an toàn 1=-1bậc 2=về+0',
  PRIMARY KEY (`upgrade_level`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `equipment_upgrade_config` VALUES
-- +1 ~ +3  |  Đá Cấp 1 (id=1)  |  an toàn
( 1,   1000,1, 3,1,1.00,0),
( 2,   2000,1, 5,2,1.00,0),
( 3,   4000,1, 8,3,0.95,0),
-- +4 ~ +6  |  Đá Cấp 2 (id=2)  |  an toàn
( 4,   8000,2, 5,2,0.90,0),
( 5,  15000,2, 7,3,0.85,0),
( 6,  25000,2,10,4,0.80,0),
-- +7 ~ +9  |  Đá Cấp 3 (id=3)  |  có thể vỡ
( 7,  40000,3, 5,2,0.75,1),
( 8,  60000,3, 7,3,0.70,1),
( 9,  90000,3,10,4,0.65,1),
-- +10~+12  |  Đá Cấp 4 (id=4)  |  vỡ -1 bậc
(10, 130000,4, 5,3,0.60,1),
(11, 180000,4, 7,3,0.55,1),
(12, 250000,4,10,4,0.50,1),
-- +13~+15  |  Đá Cấp 5 (id=5)  |  vỡ -1 bậc
(13, 350000,5, 5,3,0.45,1),
(14, 480000,5, 7,3,0.40,1),
(15, 650000,5,10,4,0.35,1),
-- +16~+18  |  Đá Cấp 6 (id=6)  |  vỡ -1 bậc
(16, 900000,6, 5,3,0.30,1),
(17,1200000,6, 7,3,0.28,1),
(18,1600000,6,10,5,0.25,1),
-- +19~+20  |  Đá Cấp 7 (id=7)  |  vỡ -1 bậc
(19,2200000,7,10,5,0.20,1),
(20,3000000,7,15,7,0.15,1);

-- ============================================================
-- TABLE: gene_upgrade_config
-- ============================================================
-- tier_from           : gene tier hiện tại (1~4)
-- element_type        : 'Fire'|'Water'|'Earth'|'Metal'|'Wood'
-- gene_exp_required   : gene_exp cần có TRƯỚC KHI thực hiện nâng cấp
-- silver_cost         : bạc tiêu hao
-- stone_id            : Linh Thạch cần dùng (FK → item_template.id)
-- stone_needed        : số đá cho tỉ lệ base
-- stone_min           : số đá tối thiểu
-- base_success_rate   : tỉ lệ khi đủ đá (0.0~1.0)
--
-- *** CÔNG THỨC ***
--   actual_rate = base_success_rate * min(actual_stones / stone_needed, 1.0)
--   + Tinh Chất X tương ứng không thêm tỉ lệ, chỉ thêm gene_exp (dùng riêng)
--   Nếu thất bại: gene_exp đặt lại về 0, tier không thay đổi
--
-- *** HƯỚNG DẪN CONFIG UNITY (UI Nâng Cấp Gene) ***
-- GeneUpgradePanel.cs :
--   1. Lấy config từ gene_upgrade_config WHERE tier_from = player.gene_tier
--      AND element_type = player.element_type
--   2. Kiểm tra: player.gene_exp >= gene_exp_required  +  player.silver >= silver_cost
--   3. Hiển thị progressbar gene_exp (0 → gene_exp_required)
--   4. Hiển thị tỉ lệ thành công realtime theo số Linh Thạch nhập
--   5. Nút dùng Tinh Chất: thêm 500 gene_exp tức thì (server xử lý)
-- ============================================================
CREATE TABLE `gene_upgrade_config` (
  `tier_from`         tinyint(4)  NOT NULL,
  `element_type`      varchar(10) NOT NULL,
  `gene_exp_required` int(11)     NOT NULL,
  `silver_cost`       int(11)     NOT NULL,
  `stone_id`          int(11)     NOT NULL,
  `stone_needed`      tinyint(4)  NOT NULL,
  `stone_min`         tinyint(4)  NOT NULL,
  `base_success_rate` float       NOT NULL,
  PRIMARY KEY (`tier_from`,`element_type`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO `gene_upgrade_config` VALUES
-- tier 1→2  |  Linh Thạch Sơ Cấp (id=17)
(1,'Fire',   500,  10000,17,5,2,0.80),
(1,'Water',  500,  10000,17,5,2,0.80),
(1,'Earth',  500,  10000,17,5,2,0.80),
(1,'Metal',  500,  10000,17,5,2,0.80),
(1,'Wood',   500,  10000,17,5,2,0.80),
-- tier 2→3  |  Linh Thạch Trung Cấp (id=18)
(2,'Fire',  2000,  50000,18,8,3,0.65),
(2,'Water', 2000,  50000,18,8,3,0.65),
(2,'Earth', 2000,  50000,18,8,3,0.65),
(2,'Metal', 2000,  50000,18,8,3,0.65),
(2,'Wood',  2000,  50000,18,8,3,0.65),
-- tier 3→4  |  Linh Thạch Cao Cấp (id=19)
(3,'Fire',  8000, 200000,19,10,5,0.50),
(3,'Water', 8000, 200000,19,10,5,0.50),
(3,'Earth', 8000, 200000,19,10,5,0.50),
(3,'Metal', 8000, 200000,19,10,5,0.50),
(3,'Wood',  8000, 200000,19,10,5,0.50),
-- tier 4→5  |  Linh Thạch Thượng Cấp (id=20)
(4,'Fire', 20000, 500000,20,12,6,0.35),
(4,'Water',20000, 500000,20,12,6,0.35),
(4,'Earth',20000, 500000,20,12,6,0.35),
(4,'Metal',20000, 500000,20,12,6,0.35),
(4,'Wood', 20000, 500000,20,12,6,0.35);

-- ============================================================
-- TABLE: exp_requirements
-- ============================================================
CREATE TABLE `exp_requirements` (
  `level`                   int(11) NOT NULL,
  `exp_required`            int(11) NOT NULL COMMENT 'Tổng EXP cần để ĐẠT level này',
  `base_stat_increase`      longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `potential_points_reward` int(11) NOT NULL DEFAULT 5,
  `skill_points_reward`     int(11) NOT NULL DEFAULT 1,
  `created_at`              datetime NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`level`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `exp_requirements` VALUES
( 1,      0,'{"hp":0,  "mp":0,  "attack":0, "defense":0}', 0,0,NOW()),
( 2,    100,'{"hp":50, "mp":20, "attack":5, "defense":2}',  5,1,NOW()),
( 3,    300,'{"hp":60, "mp":25, "attack":6, "defense":2}',  5,1,NOW()),
( 4,    600,'{"hp":70, "mp":30, "attack":7, "defense":3}',  5,1,NOW()),
( 5,   1000,'{"hp":80, "mp":35, "attack":8, "defense":3}',  5,2,NOW()),
( 6,   1500,'{"hp":90, "mp":40, "attack":9, "defense":3}',  5,1,NOW()),
( 7,   2100,'{"hp":100,"mp":45,"attack":10,"defense":4}',   5,1,NOW()),
( 8,   2800,'{"hp":110,"mp":50,"attack":11,"defense":4}',   5,1,NOW()),
( 9,   3600,'{"hp":120,"mp":55,"attack":12,"defense":4}',   5,1,NOW()),
(10,   4500,'{"hp":150,"mp":70,"attack":15,"defense":5}',   7,2,NOW()),
(11,   5500,'{"hp":130,"mp":60,"attack":13,"defense":5}',   5,1,NOW()),
(12,   6600,'{"hp":140,"mp":65,"attack":14,"defense":5}',   5,1,NOW()),
(13,   7800,'{"hp":150,"mp":70,"attack":15,"defense":6}',   5,1,NOW()),
(14,   9100,'{"hp":160,"mp":75,"attack":16,"defense":6}',   5,1,NOW()),
(15,  10500,'{"hp":200,"mp":90,"attack":20,"defense":8}',   7,2,NOW()),
(16,  12000,'{"hp":170,"mp":80,"attack":17,"defense":7}',   5,1,NOW()),
(17,  13600,'{"hp":180,"mp":85,"attack":18,"defense":7}',   5,1,NOW()),
(18,  15300,'{"hp":190,"mp":90,"attack":19,"defense":7}',   5,1,NOW()),
(19,  17100,'{"hp":200,"mp":95,"attack":20,"defense":8}',   5,1,NOW()),
(20,  19000,'{"hp":250,"mp":120,"attack":25,"defense":10}',10,3,NOW()),
(25,  30000,'{"hp":300,"mp":150,"attack":30,"defense":12}', 7,2,NOW()),
(30,  50000,'{"hp":400,"mp":200,"attack":40,"defense":16}', 7,2,NOW()),
(35,  80000,'{"hp":500,"mp":250,"attack":50,"defense":20}', 7,2,NOW()),
(40, 120000,'{"hp":600,"mp":300,"attack":60,"defense":24}', 7,2,NOW()),
(45, 180000,'{"hp":750,"mp":370,"attack":75,"defense":30}', 7,2,NOW()),
(50, 250000,'{"hp":1000,"mp":500,"attack":100,"defense":40}',10,3,NOW());

-- ============================================================
-- TABLE: skill_template
-- ============================================================
CREATE TABLE `skill_template` (
  `skill_id`        int(11)      NOT NULL AUTO_INCREMENT,
  `skill_code`      varchar(50)  NOT NULL,
  `skill_name`      varchar(100) NOT NULL,
  `description`     text         DEFAULT NULL,
  `element_type`    varchar(20)  DEFAULT NULL,
  `max_level`       int(11)      NOT NULL DEFAULT 5,
  `level_to_unlock` int(11)      NOT NULL DEFAULT 1,
  `levels_json`     longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `icon_id`         varchar(100) DEFAULT NULL,
  `created_at`      datetime     NOT NULL DEFAULT current_timestamp(),
  PRIMARY KEY (`skill_id`),
  UNIQUE KEY `uq_skill_code` (`skill_code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `skill_template` VALUES
(1,'FIRE_BALL','Cầu Lửa','Phóng cầu lửa gây sát thương','Fire',5,1,
 '[{"level_req":1,"sp_cost":1,"effect_value":20,"mp_cost":10,"desc":"Gây 20 ST"},{"level_req":3,"sp_cost":1,"effect_value":35,"mp_cost":15,"desc":"Gây 35 ST"},{"level_req":5,"sp_cost":1,"effect_value":55,"mp_cost":20,"desc":"Gây 55 ST"},{"level_req":8,"sp_cost":2,"effect_value":80,"mp_cost":25,"desc":"Gây 80 ST"},{"level_req":12,"sp_cost":2,"effect_value":110,"mp_cost":30,"desc":"Gây 110 ST"}]',
 'icon_skill_1',NOW()),
(2,'FIRE_WAVE','Sóng Lửa','Tạo sóng lửa diện rộng','Fire',5,5,
 '[{"level_req":5,"sp_cost":1,"effect_value":30,"mp_cost":15,"desc":"Gây 30 ST diện rộng"},{"level_req":8,"sp_cost":1,"effect_value":50,"mp_cost":20,"desc":"Gây 50 ST"},{"level_req":10,"sp_cost":2,"effect_value":75,"mp_cost":25,"desc":"Gây 75 ST"},{"level_req":15,"sp_cost":2,"effect_value":100,"mp_cost":30,"desc":"Gây 100 ST"},{"level_req":20,"sp_cost":3,"effect_value":140,"mp_cost":35,"desc":"Gây 140 ST"}]',
 'icon_skill_2',NOW()),
(3,'WATER_SHIELD','Khiên Nước','Tạo lớp khiên hấp thụ sát thương','Water',5,1,
 '[{"level_req":1,"sp_cost":1,"effect_value":30,"mp_cost":12,"desc":"Hấp 30 ST"},{"level_req":3,"sp_cost":1,"effect_value":50,"mp_cost":18,"desc":"Hấp 50 ST"},{"level_req":5,"sp_cost":1,"effect_value":75,"mp_cost":22,"desc":"Hấp 75 ST"},{"level_req":8,"sp_cost":2,"effect_value":110,"mp_cost":28,"desc":"Hấp 110 ST"},{"level_req":12,"sp_cost":2,"effect_value":150,"mp_cost":35,"desc":"Hấp 150 ST"}]',
 'icon_skill_3',NOW()),
(4,'HEAL_WAVE','Sóng Hồi Phục','Hồi máu cho bản thân','Water',5,3,
 '[{"level_req":3,"sp_cost":1,"effect_value":40,"mp_cost":20,"desc":"Hồi 40 HP"},{"level_req":6,"sp_cost":1,"effect_value":70,"mp_cost":28,"desc":"Hồi 70 HP"},{"level_req":9,"sp_cost":2,"effect_value":110,"mp_cost":35,"desc":"Hồi 110 HP"},{"level_req":13,"sp_cost":2,"effect_value":160,"mp_cost":42,"desc":"Hồi 160 HP"},{"level_req":18,"sp_cost":3,"effect_value":220,"mp_cost":50,"desc":"Hồi 220 HP"}]',
 'icon_skill_4',NOW()),
(5,'DASH','Lướt Nhanh','Lướt về phía trước tránh đòn',NULL,5,1,
 '[{"level_req":1,"sp_cost":1,"effect_value":1,"mp_cost":8,"desc":"Lướt 1 đơn vị"},{"level_req":3,"sp_cost":1,"effect_value":2,"mp_cost":10,"desc":"Lướt 2 đơn vị"},{"level_req":6,"sp_cost":1,"effect_value":3,"mp_cost":12,"desc":"Lướt 3 đơn vị"},{"level_req":10,"sp_cost":2,"effect_value":4,"mp_cost":14,"desc":"Lướt 4 đơn vị"},{"level_req":15,"sp_cost":2,"effect_value":5,"mp_cost":16,"desc":"Lướt 5 đơn vị"}]',
 'icon_skill_5',NOW()),
(6,'EARTH_SMASH','Đập Đất','Nện xuống đất gây sát thương AoE','Earth',5,1,
 '[{"level_req":1,"sp_cost":1,"effect_value":25,"mp_cost":12,"desc":"Gây 25 ST"},{"level_req":3,"sp_cost":1,"effect_value":45,"mp_cost":18,"desc":"Gây 45 ST"},{"level_req":6,"sp_cost":2,"effect_value":70,"mp_cost":24,"desc":"Gây 70 ST"},{"level_req":10,"sp_cost":2,"effect_value":100,"mp_cost":30,"desc":"Gây 100 ST"},{"level_req":15,"sp_cost":3,"effect_value":140,"mp_cost":36,"desc":"Gây 140 ST"}]',
 'icon_skill_6',NOW()),
(7,'METAL_SLASH','Chém Thép','Tung lưỡi kim loại sắc bén','Metal',5,1,
 '[{"level_req":1,"sp_cost":1,"effect_value":22,"mp_cost":10,"desc":"Gây 22 ST"},{"level_req":3,"sp_cost":1,"effect_value":40,"mp_cost":15,"desc":"Gây 40 ST"},{"level_req":5,"sp_cost":2,"effect_value":62,"mp_cost":20,"desc":"Gây 62 ST"},{"level_req":8,"sp_cost":2,"effect_value":90,"mp_cost":25,"desc":"Gây 90 ST"},{"level_req":12,"sp_cost":3,"effect_value":125,"mp_cost":30,"desc":"Gây 125 ST"}]',
 'icon_skill_7',NOW()),
(8,'WOOD_VINE','Dây Leo Cây','Triệu hồi dây leo trói chặt kẻ địch','Wood',5,1,
 '[{"level_req":1,"sp_cost":1,"effect_value":1,"mp_cost":14,"desc":"Trói 1s"},{"level_req":3,"sp_cost":1,"effect_value":2,"mp_cost":18,"desc":"Trói 2s"},{"level_req":5,"sp_cost":2,"effect_value":3,"mp_cost":22,"desc":"Trói 3s"},{"level_req":8,"sp_cost":2,"effect_value":4,"mp_cost":26,"desc":"Trói 4s"},{"level_req":12,"sp_cost":3,"effect_value":5,"mp_cost":30,"desc":"Trói 5s"}]',
 'icon_skill_8',NOW());

-- ============================================================
-- TABLE: map_config
-- ============================================================
CREATE TABLE `map_config` (
  `map_id`            int(11)      NOT NULL,
  `map_name`          varchar(100) NOT NULL,
  `spawn_points_json` text         NOT NULL,
  `min_level`         int(11)      NOT NULL DEFAULT 1,
  `max_level`         int(11)      NOT NULL DEFAULT 999,
  `created_at`        datetime DEFAULT current_timestamp(),
  `updated_at`        datetime DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`map_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `map_config` VALUES
(0,'Main Map',      '[{"x":0,"y":0},{"x":5,"y":0},{"x":-5,"y":0},{"x":0,"y":5}]', 1, 999,NOW(),NOW()),
(1,'Forest Zone',   '[{"x":10,"y":10},{"x":15,"y":10}]', 5, 30, NOW(),NOW()),
(2,'Volcano Crater','[{"x":20,"y":20}]',                 20, 50, NOW(),NOW()),
(3,'Ocean Depths',  '[{"x":-10,"y":-10}]',               30, 999,NOW(),NOW());

-- ============================================================
-- TABLE: enemy
-- ============================================================
CREATE TABLE `enemy` (
  `enemy_id`          int(11)     NOT NULL AUTO_INCREMENT,
  `enemy_name`        varchar(50) NOT NULL,
  `enemy_description` text        DEFAULT NULL,
  `level`             int(11)     NOT NULL DEFAULT 1,
  `base_hp`           int(11)     NOT NULL DEFAULT 50,
  `base_mp`           int(11)     NOT NULL DEFAULT 0,
  `base_damage`       int(11)     NOT NULL DEFAULT 5,
  `base_defense`      int(11)     NOT NULL DEFAULT 0,
  `move_speed`        float       NOT NULL DEFAULT 2,
  `attack_speed`      float       NOT NULL DEFAULT 1,
  `exp_reward`        int(11)     NOT NULL DEFAULT 10,
  `gold_reward`       int(11)     NOT NULL DEFAULT 5,
  `silver_reward`     int(11)     NOT NULL DEFAULT 20,
  `drop_items_json`   longtext    CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL
    COMMENT 'JSON: [{"item_id":1,"drop_chance":0.2,"qty_min":1,"qty_max":3}]',
  `element_type`      varchar(10) DEFAULT NULL,
  `enemy_type`        enum('Normal','Elite','Boss') DEFAULT 'Normal',
  `created_at`        datetime    DEFAULT current_timestamp(),
  `updated_at`        datetime    DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`enemy_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `enemy` VALUES
(1,'Slime',       'Quái yếu nhưng đông',        1,  50,  0,  5, 0,1.5,1.0, 10,  5, 20,'[{"item_id":27,"drop_chance":0.3,"qty_min":1,"qty_max":2},{"item_id":1,"drop_chance":0.2,"qty_min":1,"qty_max":1}]',NULL,'Normal',NOW(),NOW()),
(2,'Goblin',      'Nhanh nhẹn nhưng yếu',        2,  80,  0,  8, 2,2.5,1.2, 20, 10, 40,'[{"item_id":11,"drop_chance":0.15,"qty_min":1,"qty_max":1},{"item_id":29,"drop_chance":0.4,"qty_min":1,"qty_max":2}]',NULL,'Normal',NOW(),NOW()),
(3,'Orc Warrior', 'Orc có giáp, chậm nhưng mạnh',3, 150,  0, 15, 5,2.0,1.0, 50, 25,100,'[{"item_id":26,"drop_chance":0.4,"qty_min":1,"qty_max":3},{"item_id":2,"drop_chance":0.25,"qty_min":1,"qty_max":2}]',NULL,'Normal',NOW(),NOW()),
(4,'Fire Slime',  'Slime hệ Hỏa',                2,  70, 20,  8, 0,1.5,1.0, 15,  8, 30,'[{"item_id":30,"drop_chance":0.35,"qty_min":1,"qty_max":2},{"item_id":21,"drop_chance":0.05,"qty_min":1,"qty_max":1}]','Fire','Normal',NOW(),NOW()),
(5,'Boss Dragon', 'Rồng Boss cực mạnh',          10,1000,200,80,20,3.0,2.0,500,200,800,'[{"item_id":203,"drop_chance":0.05,"qty_min":1,"qty_max":1},{"item_id":5,"drop_chance":0.6,"qty_min":2,"qty_max":5},{"item_id":28,"drop_chance":0.4,"qty_min":1,"qty_max":2}]','Fire','Boss',NOW(),NOW());

-- ============================================================
-- TABLE: enemy_spawns
-- ============================================================
CREATE TABLE `enemy_spawns` (
  `spawn_id`        int(11) NOT NULL AUTO_INCREMENT,
  `map_id`          int(11) NOT NULL,
  `enemy_type_id`   int(11) NOT NULL,
  `spawn_x`         float   NOT NULL DEFAULT 0,
  `spawn_y`         float   NOT NULL DEFAULT 0,
  `max_spawn_count` int(11) NOT NULL DEFAULT 1,
  `respawn_time`    int(11) NOT NULL DEFAULT 30 COMMENT 'Giây',
  `created_at`      datetime DEFAULT current_timestamp(),
  `updated_at`      datetime DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  PRIMARY KEY (`spawn_id`),
  KEY `idx_map`   (`map_id`),
  KEY `idx_enemy` (`enemy_type_id`),
  CONSTRAINT `fk_spawn_enemy` FOREIGN KEY (`enemy_type_id`) REFERENCES `enemy`      (`enemy_id`) ON DELETE CASCADE,
  CONSTRAINT `fk_spawn_map`   FOREIGN KEY (`map_id`)        REFERENCES `map_config` (`map_id`)   ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

INSERT INTO `enemy_spawns` VALUES
(1,0,1, 10,  0,3, 30,NOW(),NOW()),
(2,0,1,-10,  0,3, 30,NOW(),NOW()),
(3,0,2, 20,  0,2, 45,NOW(),NOW()),
(4,0,3, 25,  0,1, 60,NOW(),NOW()),
(5,0,5, 30,  5,1,120,NOW(),NOW()),
(6,1,2, 10, 10,5, 30,NOW(),NOW()),
(7,1,3, 15, 10,3, 45,NOW(),NOW()),
(8,2,4,  5,  5,4, 30,NOW(),NOW()),
(9,2,5, 20, 15,1,120,NOW(),NOW());

COMMIT;
