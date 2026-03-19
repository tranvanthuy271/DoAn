-- ============================================================
-- MIGRATION: migration_equipment_full.sql
-- Thêm đầy đủ trang bị 1x-5x (2 giới tính), đá nâng cấp cấp 8-12,
-- vũ khí hệ Phong, tinh chất Phong, mở rộng upgrade +21→+24,
-- và điền đầy đủ exp_requirements cho tất cả 50 level.
--
-- Áp dụng công thức LangLa:
--   - Số đá/lần: ngocUpgrade = levelNeed/10 * 100
--     (1x: 100, 2x: 200, 3x: 300, 4x: 400, 5x: 500)
--   - Bạc/lần (quy đổi về DoAn scale):
--     Loại áo (type 0,2,3,4): base 15M LangLa → ~15k-200k DoAn tùy bậc
--     Loại khác (type 1,5): base 25M LangLa → ~25k-300k DoAn tùy bậc
--   - Tỉ lệ thành công: 100%→6% (tăng dần khó theo bậc +)
-- ============================================================

-- ============================================================
-- SECTION 1: TINH CHẤT PHONG NGUYÊN (gene item cho hệ Phong)
-- ============================================================
INSERT IGNORE INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`) VALUES
(41, 'Tinh Chất Phong Nguyên', 'Bổ sung 500 gene_exp hệ Phong', 'True', 2, 25, 6, 0, 5, 0, -1, 0);

-- ============================================================
-- SECTION 2: ĐÁ NÂNG CẤP CẤP 8-12 (cho upgrade +21→+24)
-- LangLa: đá cấp 1-12 (id 0-11) dùng cho các bậc nâng cấp cao
-- DoAn mapping: đá 8=id42, đá 9=id43, đá 10=id44, đá 11=id45, đá 12=id46
-- ============================================================
INSERT IGNORE INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`) VALUES
(42, 'Đá Nâng Cấp Cấp 8',  'Dùng để nâng cấp trang bị +21~+22. Cần trang bị cấp 3x trở lên.', 'True', 2, 21, 0, 0, 30, 0, -1, 0),
(43, 'Đá Nâng Cấp Cấp 9',  'Dùng để nâng cấp trang bị +23~+24. Cần trang bị cấp 4x trở lên.', 'True', 2, 21, 0, 0, 40, 0, -1, 0),
(44, 'Đá Nâng Cấp Cấp 10', 'Đá quý hiếm, chỉ dùng cho trang bị tối thượng.', 'True', 2, 21, 0, 0, 45, 0, -1, 0),
(45, 'Đá Nâng Cấp Cấp 11', 'Đá cấp cao nhất phổ thông, rất hiếm.', 'True', 2, 21, 0, 0, 48, 0, -1, 0),
(46, 'Đá Nâng Cấp Cấp 12', 'Đá truyền thuyết, chỉ rơi từ boss tối thượng.', 'True', 2, 21, 0, 0, 50, 0, -1, 0);

-- ============================================================
-- SECTION 3: VŨ KHÍ HỆ PHONG (Wind weapons — idClass=6)
-- Theo mẫu vũ khí các hệ khác (lv1, 10, 20, 35, 50)
-- ============================================================
INSERT IGNORE INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`) VALUES
(225, 'Thương Phong Sơ Cấp',    'Thương gỗ nhẹ, mỗi cú đánh tạo làn gió nhỏ',          'False', 2, 1, 6, 0, 1,  0, -1, 0),
(226, 'Thương Phong Trung Cấp', 'Thương bạc, kêu vù vù khi vung theo gió',              'False', 2, 1, 6, 0, 10, 0, -1, 0),
(227, 'Thương Phong Cao Cấp',   'Thương thép nhẹ như bấc, thuần khiết khí phong',       'False', 2, 1, 6, 0, 20, 0, -1, 0),
(228, 'Thương Phong Thần',      'Thương chứa tinh nguyên của Thần Phong, xuyên gió',    'False', 2, 1, 6, 0, 35, 0, -1, 0),
(229, 'Thương Phong Thượng Cấp','Thương tối cùng hệ Phong, điều khiển bão tố',          'False', 2, 1, 6, 0, 50, 0, -1, 0);

-- ============================================================
-- SECTION 4: TRANG BỊ 3x — CẤP 30 (Tier Ngân Tinh)
-- Helmet(0) Armor(2) Pants(3) Ring(5) Boots(4)
-- Male gioiTinh=0, Female gioiTinh=1, Ring gioiTinh=2
-- Công thức bạc nâng cấp 3x: ~60k–600k per attempt (scaled từ LangLa)
-- Số đá/lần 3x: 300 viên (theo LangLa ngocUpgrade = 30/10*100)
-- ============================================================
INSERT IGNORE INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`) VALUES
-- Mũ cấp 30
(300, 'Mũ Ngân Tinh Nam',      'Mũ khắc tinh văn ngân, tăng cường khí lực',             'False', 0, 0, 0, 0, 30, 0, -1, 0),
(301, 'Mũ Ngân Tinh Nữ',       'Mũ tinh ngân nữ, toả sáng trong đêm tối',               'False', 1, 0, 0, 0, 30, 0, -1, 0),
-- Áo cấp 30
(302, 'Áo Ngân Tinh Nam',      'Giáp ngân tinh rèn bằng bạc linh thiêng',               'False', 0, 2, 0, 0, 30, 0, -1, 0),
(303, 'Áo Ngân Tinh Nữ',       'Áo nữ ngân tinh, bảo vệ cơ thể khỏi đòn nguyên tố',    'False', 1, 2, 0, 0, 30, 0, -1, 0),
-- Quần cấp 30
(304, 'Quần Ngân Tinh Nam',    'Quần giáp ngân tinh, linh hoạt và bền bỉ',              'False', 0, 3, 0, 0, 30, 0, -1, 0),
(305, 'Quần Ngân Tinh Nữ',     'Quần lụa ngân tinh, thanh thoát và bảo vệ tốt',         'False', 1, 3, 0, 0, 30, 0, -1, 0),
-- Nhẫn cấp 30
(306, 'Nhẫn Bạch Kim',         'Nhẫn bạch kim trắng, kết cấu ngũ hành tinh thần',       'False', 2, 5, 0, 0, 30, 0, -1, 0),
-- Giày cấp 30
(307, 'Giày Ngân Tinh Nam',    'Giày ngân tinh nhẹ, như đi trên mây',                   'False', 0, 4, 0, 0, 30, 0, -1, 0),
(308, 'Giày Ngân Tinh Nữ',     'Giày nữ ngân tinh, tốc độ vượt gió cuốn',               'False', 1, 4, 0, 0, 30, 0, -1, 0);

-- ============================================================
-- SECTION 5: TRANG BỊ 4x — CẤP 40 (Tier Thiên Mệnh)
-- Công thức bạc nâng cấp 4x: ~120k–1.2M per attempt
-- Số đá/lần 4x: 400 viên (theo LangLa ngocUpgrade = 40/10*100)
-- ============================================================
INSERT IGNORE INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`) VALUES
-- Mũ cấp 40
(400, 'Mũ Thiên Mệnh Nam',     'Mũ chiến được thiên mệnh ban, tụ khí trời đất',         'False', 0, 0, 0, 0, 40, 0, -1, 0),
(401, 'Mũ Thiên Mệnh Nữ',      'Mũ thiên mệnh nữ, linh khí ngưng tụ quanh đầu',         'False', 1, 0, 0, 0, 40, 0, -1, 0),
-- Áo cấp 40
(402, 'Áo Thiên Mệnh Nam',     'Giáp thiên mệnh, thấm nhuần nguyên khí đất trời',       'False', 0, 2, 0, 0, 40, 0, -1, 0),
(403, 'Áo Thiên Mệnh Nữ',      'Áo nữ thiên mệnh, nhẹ tựa lụa mà cứng như thép',        'False', 1, 2, 0, 0, 40, 0, -1, 0),
-- Quần cấp 40
(404, 'Quần Thiên Mệnh Nam',   'Quần thiên mệnh, bảo hộ hoàn toàn',                     'False', 0, 3, 0, 0, 40, 0, -1, 0),
(405, 'Quần Thiên Mệnh Nữ',    'Quần nữ thiên mệnh, linh động vô song',                  'False', 1, 3, 0, 0, 40, 0, -1, 0),
-- Nhẫn cấp 40
(406, 'Nhẫn Huyết Long',       'Nhẫn khắc hình rồng, máu long thần ẩn bên trong',       'False', 2, 5, 0, 0, 40, 0, -1, 0),
-- Giày cấp 40
(407, 'Giày Thiên Mệnh Nam',   'Giày thiên mệnh, chạm đất như bay',                     'False', 0, 4, 0, 0, 40, 0, -1, 0),
(408, 'Giày Thiên Mệnh Nữ',    'Giày nữ thiên mệnh, thanh tao mà cực kỳ nhanh',          'False', 1, 4, 0, 0, 40, 0, -1, 0);

-- ============================================================
-- SECTION 6: MỞ RỘNG equipment_upgrade_config ĐẾN +24
-- Theo công thức LangLa:
--   +21/+22: Đá cấp 8 (id=42), tỉ lệ 12%/10%
--   +23/+24: Đá cấp 9 (id=43), tỉ lệ 8%/6%
--   stone_needed (số đá tối đa): 20/25/30/40 — ra từ LangLa 5x = 500 stones giảm
--   stone_min (số đá tối thiểu để kích hoạt): 8/10/12/15
--   fail_policy=2 ở +23/+24: thất bại sẽ về 0 (rủi ro cao nhất)
-- ============================================================
INSERT IGNORE INTO `equipment_upgrade_config`
  (`upgrade_level`, `silver_cost`, `stone_id`, `stone_needed`, `stone_min`, `base_success_rate`, `fail_policy`) VALUES
(21, 4200000,  42, 20,  8, 0.12, 1),
(22, 5500000,  42, 25, 10, 0.10, 1),
(23, 7000000,  43, 30, 12, 0.08, 2),
(24, 9000000,  43, 40, 15, 0.06, 2);

-- ============================================================
-- SECTION 7: ĐIỀN ĐẦY ĐỦ exp_requirements (các level còn thiếu)
-- Hiện chỉ có: 1-20, 25, 30, 35, 40, 45, 50
-- Thêm: 21-24, 26-29, 31-34, 36-39, 41-44, 46-49
--
-- Công thức EXP (nội suy tuyến tính giữa các mốc):
--   Đoạn 20→25 mỗi level +2200 exp
--   Đoạn 25→30 mỗi level +4000 exp
--   Đoạn 30→35 mỗi level +6000 exp
--   Đoạn 35→40 mỗi level +8000 exp
--   Đoạn 40→45 mỗi level +12000 exp
--   Đoạn 45→50 mỗi level +14000 exp
--
-- Công thức chỉ số (nội suy từ LangLa InfoChar + DoAn base stats):
--   hp theo bậc: tăng 10-50 mỗi level, bonus lớn tại mốc 10
--   mp: tăng ~6-26 mỗi level
--   attack: tăng ~1-5 mỗi level
--   defense: tăng ~0.4-2 mỗi level
-- ============================================================
INSERT IGNORE INTO `exp_requirements`
  (`level`, `exp_required`, `base_stat_increase`, `potential_points_reward`, `skill_points_reward`, `created_at`) VALUES
-- Levels 21-24 (giữa tier 2x và milestone 25)
(21, 21000, '{"hp":260,"mp":126,"attack":26,"defense":10}', 5, 1, NOW()),
(22, 23000, '{"hp":270,"mp":132,"attack":27,"defense":11}', 5, 1, NOW()),
(23, 25000, '{"hp":280,"mp":138,"attack":28,"defense":11}', 5, 1, NOW()),
(24, 27000, '{"hp":290,"mp":144,"attack":29,"defense":12}', 5, 1, NOW()),
-- Levels 26-29 (giữa milestone 25 và tier 3x lv30)
(26, 34000, '{"hp":320,"mp":160,"attack":32,"defense":13}', 5, 1, NOW()),
(27, 38000, '{"hp":340,"mp":170,"attack":34,"defense":13}', 5, 1, NOW()),
(28, 42000, '{"hp":360,"mp":180,"attack":36,"defense":14}', 5, 1, NOW()),
(29, 46000, '{"hp":380,"mp":190,"attack":38,"defense":15}', 5, 1, NOW()),
-- Levels 31-34 (giữa tier 3x và milestone 35)
(31, 56000, '{"hp":420,"mp":210,"attack":42,"defense":17}', 5, 1, NOW()),
(32, 62000, '{"hp":440,"mp":220,"attack":44,"defense":17}', 5, 1, NOW()),
(33, 68000, '{"hp":460,"mp":230,"attack":46,"defense":18}', 5, 1, NOW()),
(34, 74000, '{"hp":480,"mp":240,"attack":48,"defense":19}', 5, 1, NOW()),
-- Levels 36-39 (giữa milestone 35 và tier 4x lv40)
(36, 88000,  '{"hp":520,"mp":260,"attack":52,"defense":21}', 5, 1, NOW()),
(37, 96000,  '{"hp":540,"mp":270,"attack":54,"defense":21}', 5, 1, NOW()),
(38, 104000, '{"hp":560,"mp":280,"attack":56,"defense":22}', 5, 1, NOW()),
(39, 112000, '{"hp":580,"mp":290,"attack":58,"defense":23}', 5, 1, NOW()),
-- Levels 41-44 (giữa tier 4x và milestone 45)
(41, 132000, '{"hp":630,"mp":314,"attack":63,"defense":25}', 5, 1, NOW()),
(42, 144000, '{"hp":660,"mp":328,"attack":66,"defense":26}', 5, 1, NOW()),
(43, 156000, '{"hp":690,"mp":342,"attack":69,"defense":27}', 5, 1, NOW()),
(44, 168000, '{"hp":720,"mp":356,"attack":72,"defense":28}', 5, 1, NOW()),
-- Levels 46-49 (giữa milestone 45 và cap lv50)
(46, 194000, '{"hp":800,"mp":396,"attack":80,"defense":32}', 5, 1, NOW()),
(47, 208000, '{"hp":850,"mp":422,"attack":85,"defense":34}', 5, 1, NOW()),
(48, 222000, '{"hp":900,"mp":448,"attack":90,"defense":36}', 5, 1, NOW()),
(49, 236000, '{"hp":950,"mp":474,"attack":95,"defense":38}', 5, 1, NOW());

-- ============================================================
-- SECTION 8: THÊM VŨ KHÍ PHONG VÀO NPC SHOP (nếu có bảng npc_shop_item)
-- Vũ khí Phong bán ở NPC giống các hệ khác
-- ============================================================

-- Tạo bảng nếu migration_npc_system.sql chưa được chạy
CREATE TABLE IF NOT EXISTS `npc_config` (
  `npc_id`       int(11)      NOT NULL AUTO_INCREMENT,
  `npc_name`     varchar(100) NOT NULL,
  `npc_type`     varchar(20)  NOT NULL DEFAULT 'shop',
  `map_id`       int(11)      NOT NULL DEFAULT 0,
  `pos_x`        float        NOT NULL DEFAULT 0,
  `pos_y`        float        NOT NULL DEFAULT 0,
  `dialogue_key` varchar(50)  DEFAULT NULL,
  `icon_id`      varchar(50)  DEFAULT NULL,
  `is_active`    tinyint(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (`npc_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `npc_shop_item` (
  `id`               int(11) NOT NULL AUTO_INCREMENT,
  `npc_id`           int(11) NOT NULL,
  `item_template_id` int(11) NOT NULL,
  `price_silver`     int(11) NOT NULL DEFAULT 0,
  `price_gold`       int(11) NOT NULL DEFAULT 0,
  `stock`            int(11) NOT NULL DEFAULT -1,
  `required_level`   int(11) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  KEY `idx_npc_shop_npc` (`npc_id`),
  CONSTRAINT `fk_npc_shop_npc` FOREIGN KEY (`npc_id`) REFERENCES `npc_config` (`npc_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Đảm bảo các NPC cơ bản tồn tại
INSERT IGNORE INTO `npc_config` (`npc_id`, `npc_name`, `npc_type`, `map_id`, `pos_x`, `pos_y`, `dialogue_key`, `icon_id`, `is_active`) VALUES
(1, 'Lão Trương — Thương Nhân', 'shop',       0,  2.0, -1.0, 'greet', 'npc_merchant_1', 1),
(2, 'Đại Tướng Lan',            'quest',      0, -1.0, -1.0, 'quest_intro', 'npc_quest_1', 1),
(3, 'Thợ Rèn Hắc Long',         'blacksmith', 0,  0.0,  1.0, 'greet', 'npc_smith_1',    1);

INSERT IGNORE INTO `npc_shop_item` (`npc_id`, `item_template_id`, `price_silver`, `price_gold`, `stock`, `required_level`) VALUES
-- NPC vũ khí (npc_id=2 theo migration_npc_system.sql nếu tồn tại)
(2, 225, 500,    0, -1, 1),   -- Thương Phong Sơ Cấp
(2, 226, 2000,   0, -1, 10),  -- Thương Phong Trung Cấp
(2, 227, 8000,   0, -1, 20),  -- Thương Phong Cao Cấp
(2, 228, 50000,  0, -1, 35),  -- Thương Phong Thần
(2, 229, 200000, 0, -1, 50);  -- Thương Phong Thượng Cấp

-- ============================================================
-- SECTION 9: THÊM TRANG BỊ 3x VÀ 4x VÀO NPC SHOP
-- Bán ởp NPC trang bị (npc_id=1 hoặc 3 tùy setup)
-- ============================================================
INSERT IGNORE INTO `npc_shop_item` (`npc_id`, `item_template_id`, `price_silver`, `price_gold`, `stock`, `required_level`) VALUES
-- Trang bị 3x (lv30) tại NPC trang bị
(1, 300, 30000,  0, -1, 30),  -- Mũ Ngân Tinh Nam
(1, 301, 30000,  0, -1, 30),  -- Mũ Ngân Tinh Nữ
(1, 302, 35000,  0, -1, 30),  -- Áo Ngân Tinh Nam
(1, 303, 35000,  0, -1, 30),  -- Áo Ngân Tinh Nữ
(1, 304, 28000,  0, -1, 30),  -- Quần Ngân Tinh Nam
(1, 305, 28000,  0, -1, 30),  -- Quần Ngân Tinh Nữ
(1, 306, 40000,  0, -1, 30),  -- Nhẫn Bạch Kim
(1, 307, 25000,  0, -1, 30),  -- Giày Ngân Tinh Nam
(1, 308, 25000,  0, -1, 30),  -- Giày Ngân Tinh Nữ
-- Trang bị 4x (lv40) tại NPC trang bị
(1, 400, 80000,  0, -1, 40),  -- Mũ Thiên Mệnh Nam
(1, 401, 80000,  0, -1, 40),  -- Mũ Thiên Mệnh Nữ
(1, 402, 90000,  0, -1, 40),  -- Áo Thiên Mệnh Nam
(1, 403, 90000,  0, -1, 40),  -- Áo Thiên Mệnh Nữ
(1, 404, 75000,  0, -1, 40),  -- Quần Thiên Mệnh Nam
(1, 405, 75000,  0, -1, 40),  -- Quần Thiên Mệnh Nữ
(1, 406, 100000, 0, -1, 40),  -- Nhẫn Huyết Long
(1, 407, 70000,  0, -1, 40),  -- Giày Thiên Mệnh Nam
(1, 408, 70000,  0, -1, 40),  -- Giày Thiên Mệnh Nữ
-- Đá nâng cấp cấp 8-12 tại NPC đá
(3, 42, 5000,   0, -1, 30),   -- Đá Nâng Cấp Cấp 8
(3, 43, 15000,  0, -1, 40),   -- Đá Nâng Cấp Cấp 9
(3, 44, 0,  1, -1, 45),       -- Đá Nâng Cấp Cấp 10 (chỉ bán bằng vàng)
(3, 45, 0,  3, -1, 48),       -- Đá Nâng Cấp Cấp 11
(3, 46, 0,  5, -1, 50);       -- Đá Nâng Cấp Cấp 12
