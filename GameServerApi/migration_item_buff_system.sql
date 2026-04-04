-- ============================================================
-- migration_item_buff_system.sql
-- Hệ thống buff/effect cho item tiêu thụ (DoAn project)
-- Áp dụng sau: migration_add_islock_sellprice_to_item_template.sql
-- ============================================================

-- ────────────────────────────────────────────────────────────
-- 1. Bảng item_effect_template
--    Mỗi item có thể có 1 hoặc nhiều effect (row per effect).
--    effectType:
--      'HpRestore'   – hồi máu ngay lập tức (instant)
--      'MpRestore'   – hồi MP ngay lập tức (instant)
--      'HpBuff'      – tăng max HP trong thời gian (timed)
--      'MpBuff'      – tăng max MP trong thời gian (timed)
--      'AttackBuff'  – tăng sát thương công (timed)
--      'DefenseBuff' – tăng phòng thủ (timed)
--      'GeneExpBuff' – tăng % EXP gene nạp vào (timed)
--      'ExpBuff'     – tăng % EXP nhận khi kill (timed)
--      'PhucBuff'    – phúc lợi tăng vàng/exp drop (timed)
-- ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `item_effect_template` (
    `id`              INT UNSIGNED NOT NULL AUTO_INCREMENT,
    `item_template_id` INT UNSIGNED NOT NULL  COMMENT 'FK → item_template.id',
    `effect_type`     VARCHAR(50) NOT NULL   COMMENT 'HpRestore|MpRestore|HpBuff|MpBuff|AttackBuff|DefenseBuff|GeneExpBuff|ExpBuff|PhucBuff',
    `value`           INT NOT NULL DEFAULT 0 COMMENT 'Giá trị: số HP hồi / % tăng',
    `duration_sec`    INT NOT NULL DEFAULT 0 COMMENT '0 = instant; >0 = timed buff (giây)',
    `icon_id`         INT NOT NULL DEFAULT 0 COMMENT 'ID icon hiện trong HUD (0 = dùng icon item)',
    `display_name`    VARCHAR(200) NOT NULL DEFAULT '' COMMENT 'Tên hiển thị trong buff tooltip',
    `detail`          VARCHAR(500) NOT NULL DEFAULT '' COMMENT 'Mô tả chi tiết chỉ số được áp dụng',
    `sort_order`      TINYINT NOT NULL DEFAULT 0 COMMENT 'Thứ tự hiển thị khi item có nhiều effect',
    PRIMARY KEY (`id`),
    KEY `idx_item_template_id` (`item_template_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Effect/buff của từng item tiêu thụ';

-- ────────────────────────────────────────────────────────────
-- 2. Thêm cột active_buffs vào player_data
--    Lưu danh sách buff đang active dạng JSON array:
--    [ { "effectType":"GeneExpBuff", "value":20, "iconId":42,
--        "name":"Nhân Sâm Tâm Linh", "detail":"+20% EXP Gene",
--        "expireAt":"2024-06-01T10:30:00Z" } ]
-- ────────────────────────────────────────────────────────────
ALTER TABLE `player_data`
    ADD COLUMN IF NOT EXISTS `active_buffs` LONGTEXT NOT NULL DEFAULT '[]'
    COMMENT 'JSON array các buff đang active';

-- ────────────────────────────────────────────────────────────
-- 3. Thêm item mẫu vào item_template
--    type 22 = HP Potion
--    type 23 = MP Potion
--    type 24 = Timed buff item (GeneExpBuff / PhucBuff / AttackBuff)
-- ────────────────────────────────────────────────────────────

-- HP Potions (type 22)
INSERT IGNORE INTO `item_template` (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed, taiPhuNeed, idMob, idChar, isLock, sellPrice) VALUES
(101, 'Thuốc Hồi Máu Nhỏ',   'Hồi phục 200 HP ngay lập tức.',           'True', 2, 22, 0, 101, 1, 0, -1, 0, 0, 50),
(102, 'Thuốc Hồi Máu Vừa',   'Hồi phục 500 HP ngay lập tức.',           'True', 2, 22, 0, 102, 10, 0, -1, 0, 0, 120),
(103, 'Thuốc Hồi Máu Lớn',   'Hồi phục 1200 HP ngay lập tức.',          'True', 2, 22, 0, 103, 25, 0, -1, 0, 0, 300),
(104, 'Đan Hồi Sinh',         'Hồi phục toàn bộ HP.',                    'True', 2, 22, 0, 104, 50, 0, -1, 0, 0, 800);

-- MP Potions (type 23)
INSERT IGNORE INTO `item_template` (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed, taiPhuNeed, idMob, idChar, isLock, sellPrice) VALUES
(111, 'Thuốc Hồi Linh Nhỏ',  'Hồi phục 150 MP ngay lập tức.',           'True', 2, 23, 0, 111, 1, 0, -1, 0, 0, 50),
(112, 'Thuốc Hồi Linh Vừa',  'Hồi phục 400 MP ngay lập tức.',           'True', 2, 23, 0, 112, 10, 0, -1, 0, 0, 120),
(113, 'Thuốc Hồi Linh Lớn',  'Hồi phục 1000 MP ngay lập tức.',          'True', 2, 23, 0, 113, 25, 0, -1, 0, 0, 300);

-- Timed buff items (type 24)
INSERT IGNORE INTO `item_template` (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed, taiPhuNeed, idMob, idChar, isLock, sellPrice) VALUES
-- GeneExpBuff (30 phút)
(121, 'Nhân Sâm Tâm Linh',    'Tăng 20% EXP Gene trong 30 phút.',        'True', 2, 24, 0, 121, 1, 0, -1, 0, 0, 200),
(122, 'Nhân Sâm Thần Thánh',  'Tăng 50% EXP Gene trong 30 phút.',        'True', 2, 24, 0, 122, 20, 0, -1, 0, 0, 600),
(123, 'Nhân Sâm Thiên Hạ',    'Tăng 100% EXP Gene trong 1 giờ.',         'True', 2, 24, 0, 123, 40, 0, -1, 0, 0, 1500),
-- ExpBuff (30 phút)
(131, 'Nén Hương Kinh Nghiệm','Tăng 25% EXP nhận được trong 30 phút.',   'True', 2, 24, 0, 131, 1, 0, -1, 0, 0, 300),
(132, 'Nén Hương Thần Thánh', 'Tăng 50% EXP nhận được trong 1 giờ.',     'True', 2, 24, 0, 132, 20, 0, -1, 0, 0, 800),
-- PhucBuff (1 giờ) – tăng vàng + EXP drop
(141, 'Bùa Phúc Nhỏ',         '+10% vàng và EXP nhận được trong 1 giờ.', 'True', 2, 24, 0, 141, 1, 0, -1, 0, 0, 400),
(142, 'Bùa Phúc Lớn',         '+25% vàng và EXP nhận được trong 2 giờ.', 'True', 2, 24, 0, 142, 30, 0, -1, 0, 0, 1200),
-- AttackBuff / DefenseBuff (30 phút)
(151, 'Bùa Tăng Công Nhỏ',    'Tăng 15% sát thương trong 30 phút.',      'True', 2, 24, 0, 151, 5, 0, -1, 0, 0, 250),
(152, 'Bùa Phòng Thủ Nhỏ',    'Tăng 15% phòng thủ trong 30 phút.',       'True', 2, 24, 0, 152, 5, 0, -1, 0, 0, 250);

-- ────────────────────────────────────────────────────────────
-- 4. Cấu hình effect cho từng item (item_effect_template)
-- ────────────────────────────────────────────────────────────

-- HP Potions → HpRestore (instant, duration_sec=0)
INSERT IGNORE INTO `item_effect_template` (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail) VALUES
(101, 'HpRestore', 200,  0, 101, 'Hồi máu', '+200 HP'),
(102, 'HpRestore', 500,  0, 102, 'Hồi máu', '+500 HP'),
(103, 'HpRestore', 1200, 0, 103, 'Hồi máu', '+1200 HP'),
(104, 'HpRestore', 9999, 0, 104, 'Hồi máu', 'Hồi toàn bộ HP');

-- MP Potions → MpRestore (instant)
INSERT IGNORE INTO `item_effect_template` (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail) VALUES
(111, 'MpRestore', 150,  0, 111, 'Hồi linh', '+150 MP'),
(112, 'MpRestore', 400,  0, 112, 'Hồi linh', '+400 MP'),
(113, 'MpRestore', 1000, 0, 113, 'Hồi linh', '+1000 MP');

-- GeneExpBuff → timed
INSERT IGNORE INTO `item_effect_template` (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail) VALUES
(121, 'GeneExpBuff', 20,  1800, 121, 'EXP Gene +20%',  '+20% EXP Gene (30 phút)'),
(122, 'GeneExpBuff', 50,  1800, 122, 'EXP Gene +50%',  '+50% EXP Gene (30 phút)'),
(123, 'GeneExpBuff', 100, 3600, 123, 'EXP Gene +100%', '+100% EXP Gene (1 giờ)');

-- ExpBuff → timed
INSERT IGNORE INTO `item_effect_template` (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail) VALUES
(131, 'ExpBuff', 25, 1800, 131, 'EXP +25%', '+25% EXP (30 phút)'),
(132, 'ExpBuff', 50, 3600, 132, 'EXP +50%', '+50% EXP (1 giờ)');

-- PhucBuff → timed (2 effects: ExpBuff + GoldBuff)
INSERT IGNORE INTO `item_effect_template` (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail, sort_order) VALUES
(141, 'PhucBuff', 10, 3600,  141, 'Phúc +10%', '+10% vàng và EXP (1 giờ)',  0),
(142, 'PhucBuff', 25, 7200,  142, 'Phúc +25%', '+25% vàng và EXP (2 giờ)',  0);

-- AttackBuff / DefenseBuff → timed
INSERT IGNORE INTO `item_effect_template` (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail) VALUES
(151, 'AttackBuff',  15, 1800, 151, 'Công +15%', '+15% sát thương (30 phút)'),
(152, 'DefenseBuff', 15, 1800, 152, 'Thủ +15%',  '+15% phòng thủ (30 phút)');
