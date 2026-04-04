-- ============================================================
-- migration_item_buff_hpmp.sql
-- Thêm item HpBuff và MpBuff (tăng Max HP / Max MP có thời hạn)
-- Áp dụng sau: migration_item_buff_system.sql
-- ============================================================

-- HpBuff items (type 24 – timed buff)
INSERT IGNORE INTO `item_template` (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed, taiPhuNeed, idMob, idChar, isLock, sellPrice) VALUES
(161, 'Đan Cường Sinh Nhỏ',   'Tăng 10% Max HP trong 30 phút.',  'True', 2, 24, 0, 161, 5,  0, -1, 0, 0, 300),
(162, 'Đan Cường Sinh Lớn',   'Tăng 20% Max HP trong 1 giờ.',    'True', 2, 24, 0, 162, 20, 0, -1, 0, 0, 800),
(163, 'Đan Trường Thọ',       'Tăng 40% Max HP trong 2 giờ.',    'True', 2, 24, 0, 163, 40, 0, -1, 0, 0, 2000);

-- MpBuff items (type 24 – timed buff)
INSERT IGNORE INTO `item_template` (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed, taiPhuNeed, idMob, idChar, isLock, sellPrice) VALUES
(171, 'Linh Dược Hồi Khí Nhỏ','Tăng 10% Max MP trong 30 phút.',  'True', 2, 24, 0, 171, 5,  0, -1, 0, 0, 300),
(172, 'Linh Dược Hồi Khí Lớn','Tăng 20% Max MP trong 1 giờ.',    'True', 2, 24, 0, 172, 20, 0, -1, 0, 0, 800),
(173, 'Linh Dược Thần Khí',   'Tăng 40% Max MP trong 2 giờ.',    'True', 2, 24, 0, 173, 40, 0, -1, 0, 0, 2000);

-- HpBuff effects
INSERT IGNORE INTO `item_effect_template` (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail) VALUES
(161, 'HpBuff', 10, 1800, 161, 'Max HP +10%', '+10% Max HP (30 phút)'),
(162, 'HpBuff', 20, 3600, 162, 'Max HP +20%', '+20% Max HP (1 giờ)'),
(163, 'HpBuff', 40, 7200, 163, 'Max HP +40%', '+40% Max HP (2 giờ)');

-- MpBuff effects
INSERT IGNORE INTO `item_effect_template` (item_template_id, effect_type, value, duration_sec, icon_id, display_name, detail) VALUES
(171, 'MpBuff', 10, 1800, 171, 'Max MP +10%', '+10% Max MP (30 phút)'),
(172, 'MpBuff', 20, 3600, 172, 'Max MP +20%', '+20% Max MP (1 giờ)'),
(173, 'MpBuff', 40, 7200, 173, 'Max MP +40%', '+40% Max MP (2 giờ)');
