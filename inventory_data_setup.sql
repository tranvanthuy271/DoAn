-- ============================================
-- Script tạo dữ liệu Inventory với iconId
-- iconId: client_icon_121, client_icon_142, client_icon_152, client_icon_167
-- Dựa trên cấu trúc DB thực tế từ gamedb (2).sql
-- ============================================

-- 1. Thêm cột icon_id vào bảng item_template nếu chưa có
-- (Bảng item_template đã có sẵn trong DB, chỉ cần thêm cột icon_id)
-- Lưu ý: Nếu cột icon_id đã tồn tại, bạn sẽ thấy lỗi "Duplicate column name"
-- Bạn có thể bỏ qua lỗi đó hoặc comment dòng ALTER TABLE này lại

-- Kiểm tra xem cột icon_id đã tồn tại chưa
SET @col_exists = (
    SELECT COUNT(*) 
    FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_SCHEMA = DATABASE() 
    AND TABLE_NAME = 'item_template' 
    AND COLUMN_NAME = 'icon_id'
);

-- Chỉ thêm cột nếu chưa tồn tại
SET @sql = IF(@col_exists = 0,
    'ALTER TABLE `item_template` ADD COLUMN `icon_id` VARCHAR(255) DEFAULT NULL COMMENT ''iconId để Unity load sprite từ Resources/ItemIcons'' AFTER `icon_path`',
    'SELECT ''Column icon_id already exists'' AS message'
);

PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

-- 2. Insert các item template với iconId tương ứng
-- Sử dụng INSERT IGNORE để tránh lỗi nếu item đã tồn tại (dựa trên code unique)
INSERT IGNORE INTO `item_template` 
    (`code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `rarity`, `icon_path`, `icon_id`, `base_stat_json`) 
VALUES
-- Item 1: client_icon_121 - Hồi Máu Nhỏ
('ITEM_ICON_121', 'Hồi Máu Nhỏ', 'Potion hồi máu cơ bản, hồi 50 HP', 2, 2, 1, 99, 1, 'ItemIcons/client_icon_121', 'client_icon_121', '{"heal_amount": 50}'),

-- Item 2: client_icon_142 - Hồi Mana Nhỏ
('ITEM_ICON_142', 'Hồi Mana Nhỏ', 'Potion hồi mana cơ bản, hồi 30 MP', 2, 2, 1, 99, 1, 'ItemIcons/client_icon_142', 'client_icon_142', '{"mana_restore": 30}'),

-- Item 3: client_icon_152 - Đá Quý Thường
('ITEM_ICON_152', 'Đá Quý Thường', 'Nguyên liệu quý hiếm, dùng để nâng cấp trang bị', 3, 3, 1, 50, 2, 'ItemIcons/client_icon_152', 'client_icon_152', '{"upgrade_value": 1}'),

-- Item 4: client_icon_167 - Kiếm Đồng
('ITEM_ICON_167', 'Kiếm Đồng', 'Vũ khí cơ bản, tăng 15 ATK', 1, 1, 0, 1, 1, 'ItemIcons/client_icon_167', 'client_icon_167', '{"attack": 15, "durability": 100}');

-- 3. Update lại icon_id cho các item đã tồn tại (nếu có)
UPDATE `item_template` 
SET `icon_id` = CASE 
    WHEN `code` = 'ITEM_ICON_121' THEN 'client_icon_121'
    WHEN `code` = 'ITEM_ICON_142' THEN 'client_icon_142'
    WHEN `code` = 'ITEM_ICON_152' THEN 'client_icon_152'
    WHEN `code` = 'ITEM_ICON_167' THEN 'client_icon_167'
    ELSE `icon_id`
END
WHERE `code` IN ('ITEM_ICON_121', 'ITEM_ICON_142', 'ITEM_ICON_152', 'ITEM_ICON_167');

-- 4. Lấy ID thực tế của các item vừa insert (để dùng trong inventory)
-- Lưu vào biến để dùng trong UPDATE
SET @item_id_121 = (SELECT `id` FROM `item_template` WHERE `code` = 'ITEM_ICON_121' LIMIT 1);
SET @item_id_142 = (SELECT `id` FROM `item_template` WHERE `code` = 'ITEM_ICON_142' LIMIT 1);
SET @item_id_152 = (SELECT `id` FROM `item_template` WHERE `code` = 'ITEM_ICON_152' LIMIT 1);
SET @item_id_167 = (SELECT `id` FROM `item_template` WHERE `code` = 'ITEM_ICON_167' LIMIT 1);

-- 5. Update inventory cho player_id = 1 (hoặc player bạn muốn test)
-- Format JSON: [{"slotIndex": 0, "itemCode": "ITEM_ICON_121", "itemTemplateId": X, "iconId": "client_icon_121", "quantity": 5, "isEquipped": false}, ...]
-- Sử dụng biến @item_id_XXX đã lấy ở trên

UPDATE `player_data` 
SET `inventory` = JSON_ARRAY(
    JSON_OBJECT(
        'slotIndex', 0,
        'itemCode', 'ITEM_ICON_121',
        'itemTemplateId', @item_id_121,
        'iconId', 'client_icon_121',
        'quantity', 5,
        'isEquipped', FALSE
    ),
    JSON_OBJECT(
        'slotIndex', 1,
        'itemCode', 'ITEM_ICON_142',
        'itemTemplateId', @item_id_142,
        'iconId', 'client_icon_142',
        'quantity', 3,
        'isEquipped', FALSE
    ),
    JSON_OBJECT(
        'slotIndex', 2,
        'itemCode', 'ITEM_ICON_152',
        'itemTemplateId', @item_id_152,
        'iconId', 'client_icon_152',
        'quantity', 10,
        'isEquipped', FALSE
    ),
    JSON_OBJECT(
        'slotIndex', 3,
        'itemCode', 'ITEM_ICON_167',
        'itemTemplateId', @item_id_167,
        'iconId', 'client_icon_167',
        'quantity', 1,
        'isEquipped', TRUE
    )
)
WHERE `player_id` = 1;

-- 6. Xem kết quả inventory đã update
SELECT 
    `player_id`,
    `character_name`,
    JSON_PRETTY(`inventory`) AS `inventory_json`
FROM `player_data`
WHERE `player_id` = 1;

-- 7. Xem item_template đã tạo
SELECT 
    `id`,
    `code`,
    `name`,
    `icon_path`,
    `icon_id`,
    `rarity`,
    `stackable`,
    `max_stack`,
    `category`,
    `item_type`
FROM `item_template`
WHERE `code` IN ('ITEM_ICON_121', 'ITEM_ICON_142', 'ITEM_ICON_152', 'ITEM_ICON_167')
ORDER BY `id`;

