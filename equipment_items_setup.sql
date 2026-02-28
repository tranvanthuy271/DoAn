-- ============================================
-- Script thêm item trang bị mẫu cho tất cả 6 slot
-- Equipment slots: Weapon, Helmet, Armor, Pants, Boots, Accessory
-- Category 1 = Equipment
-- item_type: 1=Weapon, 2=Ranged, 3=Armor, 4=Helmet, 5=Pants, 6=Boots, 7=Accessory
-- ============================================

-- Thêm items mẫu cho tất cả loại trang bị
-- Sử dụng INSERT IGNORE để tránh lỗi duplicate

-- === WEAPONS (item_type = 1) ===
INSERT IGNORE INTO `item_template` 
(`code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `gender_limit`, `class_limit`, `level_required`, `rarity`, `icon_path`, `icon_id`, `base_stat_json`) 
VALUES
('SWORD_FIRE', 'Kiếm Lửa', 'Kiếm được rèn từ lửa nguyên tố, tăng 25 ATK', 1, 1, 0, 1, 0, 0, 1, 2, 'ItemIcons/sword_fire', 'sword_fire', '{"attack": 25, "durability": 120}'),
('SWORD_ICE', 'Kiếm Băng', 'Kiếm băng giá vĩnh cửu, tăng 20 ATK', 1, 1, 0, 1, 0, 0, 3, 2, 'ItemIcons/sword_ice', 'sword_ice', '{"attack": 20, "durability": 100}');

-- === RANGED WEAPONS (item_type = 2) - cũng vào slot Weapon ===
INSERT IGNORE INTO `item_template` 
(`code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `gender_limit`, `class_limit`, `level_required`, `rarity`, `icon_path`, `icon_id`, `base_stat_json`) 
VALUES
('BOW_WIND', 'Cung Gió', 'Cung phong hệ, tầm xa, tăng 18 ATK', 1, 2, 0, 1, 0, 0, 2, 2, 'ItemIcons/bow_wind', 'bow_wind', '{"attack": 18, "range": 12}');

-- === ARMOR (item_type = 3) ===
INSERT IGNORE INTO `item_template` 
(`code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `gender_limit`, `class_limit`, `level_required`, `rarity`, `icon_path`, `icon_id`, `base_stat_json`) 
VALUES
('ARMOR_IRON', 'Giáp Sắt', 'Giáp sắt chắc chắn, tăng 20 DEF', 1, 3, 0, 1, 0, 0, 3, 2, 'ItemIcons/armor_iron', 'armor_iron', '{"defense": 20}'),
('ARMOR_GOLD', 'Giáp Vàng', 'Giáp quý hiếm, tăng 35 DEF', 1, 3, 0, 1, 0, 0, 8, 3, 'ItemIcons/armor_gold', 'armor_gold', '{"defense": 35, "hp": 50}');

-- === HELMET (item_type = 4) ===
INSERT IGNORE INTO `item_template` 
(`code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `gender_limit`, `class_limit`, `level_required`, `rarity`, `icon_path`, `icon_id`, `base_stat_json`) 
VALUES
('HELMET_LEATHER', 'Mũ Da', 'Mũ da cơ bản, tăng 8 DEF', 1, 4, 0, 1, 0, 0, 1, 1, 'ItemIcons/helmet_leather', 'helmet_leather', '{"defense": 8}'),
('HELMET_STEEL', 'Mũ Thép', 'Mũ thép bền bỉ, tăng 15 DEF', 1, 4, 0, 1, 0, 0, 5, 2, 'ItemIcons/helmet_steel', 'helmet_steel', '{"defense": 15}');

-- === PANTS (item_type = 5) ===
INSERT IGNORE INTO `item_template` 
(`code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `gender_limit`, `class_limit`, `level_required`, `rarity`, `icon_path`, `icon_id`, `base_stat_json`) 
VALUES
('PANTS_LEATHER', 'Quần Da', 'Quần da nhẹ, tăng 6 DEF', 1, 5, 0, 1, 0, 0, 1, 1, 'ItemIcons/pants_leather', 'pants_leather', '{"defense": 6}'),
('PANTS_IRON', 'Quần Sắt', 'Quần giáp sắt, tăng 12 DEF', 1, 5, 0, 1, 0, 0, 4, 2, 'ItemIcons/pants_iron', 'pants_iron', '{"defense": 12}');

-- === BOOTS (item_type = 6) ===
INSERT IGNORE INTO `item_template` 
(`code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `gender_limit`, `class_limit`, `level_required`, `rarity`, `icon_path`, `icon_id`, `base_stat_json`) 
VALUES
('BOOTS_LEATHER', 'Giày Da', 'Giày da nhẹ, tăng 4 DEF và 0.5 tốc độ', 1, 6, 0, 1, 0, 0, 1, 1, 'ItemIcons/boots_leather', 'boots_leather', '{"defense": 4, "move_speed": 0.5}'),
('BOOTS_WIND', 'Giày Gió', 'Giày phong hệ, tăng 1.0 tốc độ', 1, 6, 0, 1, 0, 0, 5, 3, 'ItemIcons/boots_wind', 'boots_wind', '{"defense": 6, "move_speed": 1.0}');

-- === ACCESSORY (item_type = 7) ===
INSERT IGNORE INTO `item_template` 
(`code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `gender_limit`, `class_limit`, `level_required`, `rarity`, `icon_path`, `icon_id`, `base_stat_json`) 
VALUES
('RING_HP', 'Nhẫn Sinh Lực', 'Nhẫn hồi sinh, tăng 30 HP', 1, 7, 0, 1, 0, 0, 1, 1, 'ItemIcons/ring_hp', 'ring_hp', '{"hp": 30}'),
('NECKLACE_ATK', 'Vòng Cổ Sức Mạnh', 'Vòng cổ kỳ bí, tăng 10 ATK', 1, 7, 0, 1, 0, 0, 3, 2, 'ItemIcons/necklace_atk', 'necklace_atk', '{"attack": 10}'),
('RING_LEGENDARY', 'Nhẫn Huyền Thoại', 'Nhẫn cổ đại, tăng 50 HP và 15 ATK', 1, 7, 0, 1, 0, 0, 10, 4, 'ItemIcons/ring_legendary', 'ring_legendary', '{"hp": 50, "attack": 15}');

-- Verify kết quả
SELECT id, code, name, category, item_type, rarity, icon_id, base_stat_json 
FROM item_template 
WHERE category = 1 
ORDER BY item_type, id;
