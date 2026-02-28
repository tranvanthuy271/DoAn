-- Insert sample item templates for testing
-- Run this in your gamedb database

-- Check if table is empty first
-- SELECT COUNT(*) FROM item_template;

-- Sample items
INSERT INTO `item_template` 
(`id`, `code`, `name`, `description`, `category`, `item_type`, `stackable`, `max_stack`, `gender_limit`, `class_limit`, `level_required`, `rarity`, `icon_path`, `prefab_path`, `base_stat_json`) 
VALUES
-- Weapons (Category 1)
(1, 'SWORD_001', 'Iron Sword', 'A basic iron sword', 1, 1, 0, 1, 0, 0, 1, 1, 'sword_iron', NULL, '{"attack": 10, "durability": 100}'),
(2, 'SWORD_002', 'Steel Sword', 'A sturdy steel sword', 1, 1, 0, 1, 0, 0, 5, 2, 'sword_steel', NULL, '{"attack": 20, "durability": 150}'),
(3, 'BOW_001', 'Wooden Bow', 'A simple wooden bow', 1, 2, 0, 1, 0, 0, 1, 1, 'bow_wood', NULL, '{"attack": 8, "range": 10, "durability": 80}'),

-- Potions (Category 2)
(4, 'POTION_HP_SMALL', 'Small Health Potion', 'Restores 50 HP', 2, 1, 1, 99, 0, 0, 1, 1, 'potion_hp_small', NULL, '{"heal_amount": 50}'),
(5, 'POTION_HP_MEDIUM', 'Medium Health Potion', 'Restores 150 HP', 2, 1, 1, 99, 0, 0, 5, 2, 'potion_hp_medium', NULL, '{"heal_amount": 150}'),
(6, 'POTION_MP_SMALL', 'Small Mana Potion', 'Restores 30 MP', 2, 2, 1, 99, 0, 0, 1, 1, 'potion_mp_small', NULL, '{"mana_amount": 30}'),

-- Materials (Category 3)
(7, 'MATERIAL_WOOD', 'Wood', 'Basic crafting material', 3, 1, 1, 999, 0, 0, 0, 1, 'material_wood', NULL, '{}'),
(8, 'MATERIAL_IRON_ORE', 'Iron Ore', 'Can be smelted into iron', 3, 1, 1, 999, 0, 0, 0, 1, 'material_iron_ore', NULL, '{}'),
(9, 'MATERIAL_HERB', 'Herb', 'Used for alchemy', 3, 2, 1, 999, 0, 0, 0, 1, 'material_herb', NULL, '{}'),

-- Equipment (Category 1)
(10, 'ARMOR_LEATHER', 'Leather Armor', 'Basic leather armor', 1, 3, 0, 1, 0, 0, 3, 1, 'armor_leather', NULL, '{"defense": 15}'),
(11, 'HELMET_IRON', 'Iron Helmet', 'An iron helmet', 1, 4, 0, 1, 0, 0, 5, 2, 'helmet_iron', NULL, '{"defense": 10}');

-- Verify insertion
-- SELECT * FROM item_template ORDER BY id;
