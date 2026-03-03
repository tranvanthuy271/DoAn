-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Máy chủ: 127.0.0.1
-- Thời gian đã tạo: Th3 03, 2026 lúc 06:31 PM
-- Phiên bản máy phục vụ: 10.4.32-MariaDB
-- Phiên bản PHP: 8.0.30

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Cơ sở dữ liệu: `gamedb`
--

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `enemy`
--

CREATE TABLE `enemy` (
  `enemy_id` int(11) NOT NULL,
  `enemy_name` varchar(50) NOT NULL,
  `enemy_description` text DEFAULT NULL,
  `level` int(11) NOT NULL DEFAULT 1,
  `base_hp` int(11) NOT NULL DEFAULT 50,
  `base_mp` int(11) NOT NULL DEFAULT 0,
  `base_damage` int(11) NOT NULL DEFAULT 5,
  `base_defense` int(11) NOT NULL DEFAULT 0,
  `move_speed` float NOT NULL DEFAULT 2,
  `attack_speed` float NOT NULL DEFAULT 1,
  `exp_reward` int(11) NOT NULL DEFAULT 10,
  `gold_reward` int(11) NOT NULL DEFAULT 5,
  `drop_items_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: [{"item_code":"POTION_HP_SMALL","drop_chance":0.2,"qty_min":1,"qty_max":3}]',
  `element_type` varchar(10) DEFAULT NULL,
  `enemy_type` enum('Normal','Elite','Boss') DEFAULT 'Normal',
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `enemy`
--

INSERT INTO `enemy` (`enemy_id`, `enemy_name`, `enemy_description`, `level`, `base_hp`, `base_mp`, `base_damage`, `base_defense`, `move_speed`, `attack_speed`, `exp_reward`, `gold_reward`, `drop_items_json`, `element_type`, `enemy_type`, `created_at`, `updated_at`) VALUES
(1, 'Slime', 'Quái yếu nhưng đông', 1, 50, 0, 5, 0, 1.5, 1, 10, 5, '[{\"item_code\":\"MAT_HERB\",\"drop_chance\":0.3,\"qty_min\":1,\"qty_max\":2}]', NULL, 'Normal', '2026-03-01 10:02:38', '2026-03-01 10:02:38'),
(2, 'Goblin', 'Nhanh nhẹn nhưng yếu', 2, 80, 0, 8, 2, 2.5, 1.2, 20, 10, '[{\"item_code\":\"POTION_HP_S\",\"drop_chance\":0.15,\"qty_min\":1,\"qty_max\":1}]', NULL, 'Normal', '2026-03-01 10:02:38', '2026-03-01 10:02:38'),
(3, 'Orc Warrior', 'Orc có giáp, chậm nhưng mạnh', 3, 150, 0, 15, 5, 2, 1, 50, 25, '[{\"item_code\":\"MAT_IRON_ORE\",\"drop_chance\":0.4,\"qty_min\":1,\"qty_max\":3}]', NULL, 'Normal', '2026-03-01 10:02:38', '2026-03-01 10:02:38'),
(4, 'Fire Slime', 'Slime hệ Fire', 2, 70, 20, 8, 0, 1.5, 1, 15, 8, '[{\"item_code\":\"MAT_HERB\",\"drop_chance\":0.2,\"qty_min\":1,\"qty_max\":2}]', 'Fire', 'Normal', '2026-03-01 10:02:38', '2026-03-01 10:02:38'),
(5, 'Boss Dragon', 'Rồng Boss cực mạnh', 10, 1000, 200, 80, 20, 3, 2, 500, 200, '[{\"item_code\":\"SWORD_002\",\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1},{\"item_code\":\"POTION_HP_M\",\"drop_chance\":0.8,\"qty_min\":2,\"qty_max\":5}]', 'Fire', 'Boss', '2026-03-01 10:02:38', '2026-03-01 10:02:38');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `enemy_spawns`
--

CREATE TABLE `enemy_spawns` (
  `spawn_id` int(11) NOT NULL,
  `map_id` int(11) NOT NULL,
  `enemy_type_id` int(11) NOT NULL,
  `spawn_x` float NOT NULL DEFAULT 0,
  `spawn_y` float NOT NULL DEFAULT 0,
  `max_spawn_count` int(11) NOT NULL DEFAULT 1,
  `respawn_time` int(11) NOT NULL DEFAULT 30 COMMENT 'Giây',
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `enemy_spawns`
--

INSERT INTO `enemy_spawns` (`spawn_id`, `map_id`, `enemy_type_id`, `spawn_x`, `spawn_y`, `max_spawn_count`, `respawn_time`, `created_at`, `updated_at`) VALUES
(1, 0, 1, 10, 0, 3, 30, '2026-03-01 10:02:38', '2026-03-01 10:02:38'),
(2, 0, 1, -10, 0, 3, 30, '2026-03-01 10:02:38', '2026-03-01 10:02:38'),
(3, 0, 2, 20, 0, 2, 45, '2026-03-01 10:02:38', '2026-03-01 10:02:38'),
(4, 0, 3, 25, 0, 1, 60, '2026-03-01 10:02:38', '2026-03-01 10:02:38'),
(5, 0, 5, 30, 5, 1, 120, '2026-03-01 10:02:38', '2026-03-01 10:02:38');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `exp_requirements`
--

CREATE TABLE `exp_requirements` (
  `level` int(11) NOT NULL,
  `exp_required` int(11) NOT NULL COMMENT 'Tổng EXP cần để ĐẠT level này',
  `base_stat_increase` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: {"hp":50,"mp":20,"attack":5,"defense":2} – tự động cộng khi lên level',
  `potential_points_reward` int(11) NOT NULL DEFAULT 5 COMMENT 'Potential points nhận khi lên level',
  `skill_points_reward` int(11) NOT NULL DEFAULT 1 COMMENT 'Skill points nhận khi lên level',
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `exp_requirements`
--

INSERT INTO `exp_requirements` (`level`, `exp_required`, `base_stat_increase`, `potential_points_reward`, `skill_points_reward`, `created_at`) VALUES
(1, 0, '{\"hp\":0,  \"mp\":0,  \"attack\":0, \"defense\":0}', 0, 0, '2026-03-01 10:02:38'),
(2, 100, '{\"hp\":50, \"mp\":20, \"attack\":5, \"defense\":2}', 5, 1, '2026-03-01 10:02:38'),
(3, 300, '{\"hp\":60, \"mp\":25, \"attack\":6, \"defense\":2}', 5, 1, '2026-03-01 10:02:38'),
(4, 600, '{\"hp\":70, \"mp\":30, \"attack\":7, \"defense\":3}', 5, 1, '2026-03-01 10:02:38'),
(5, 1000, '{\"hp\":80, \"mp\":35, \"attack\":8, \"defense\":3}', 5, 2, '2026-03-01 10:02:38'),
(6, 1500, '{\"hp\":90, \"mp\":40, \"attack\":9, \"defense\":3}', 5, 1, '2026-03-01 10:02:38'),
(7, 2100, '{\"hp\":100,\"mp\":45, \"attack\":10,\"defense\":4}', 5, 1, '2026-03-01 10:02:38'),
(8, 2800, '{\"hp\":110,\"mp\":50, \"attack\":11,\"defense\":4}', 5, 1, '2026-03-01 10:02:38'),
(9, 3600, '{\"hp\":120,\"mp\":55, \"attack\":12,\"defense\":4}', 5, 1, '2026-03-01 10:02:38'),
(10, 4500, '{\"hp\":150,\"mp\":70, \"attack\":15,\"defense\":5}', 7, 2, '2026-03-01 10:02:38'),
(11, 5500, '{\"hp\":130,\"mp\":60, \"attack\":13,\"defense\":5}', 5, 1, '2026-03-01 10:02:38'),
(12, 6600, '{\"hp\":140,\"mp\":65, \"attack\":14,\"defense\":5}', 5, 1, '2026-03-01 10:02:38'),
(13, 7800, '{\"hp\":150,\"mp\":70, \"attack\":15,\"defense\":6}', 5, 1, '2026-03-01 10:02:38'),
(14, 9100, '{\"hp\":160,\"mp\":75, \"attack\":16,\"defense\":6}', 5, 1, '2026-03-01 10:02:38'),
(15, 10500, '{\"hp\":200,\"mp\":90, \"attack\":20,\"defense\":8}', 7, 2, '2026-03-01 10:02:38'),
(16, 12000, '{\"hp\":170,\"mp\":80, \"attack\":17,\"defense\":7}', 5, 1, '2026-03-01 10:02:38'),
(17, 13600, '{\"hp\":180,\"mp\":85, \"attack\":18,\"defense\":7}', 5, 1, '2026-03-01 10:02:38'),
(18, 15300, '{\"hp\":190,\"mp\":90, \"attack\":19,\"defense\":7}', 5, 1, '2026-03-01 10:02:38'),
(19, 17100, '{\"hp\":200,\"mp\":95, \"attack\":20,\"defense\":8}', 5, 1, '2026-03-01 10:02:38'),
(20, 19000, '{\"hp\":250,\"mp\":120,\"attack\":25,\"defense\":10}', 10, 3, '2026-03-01 10:02:38');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `item_template`
--

CREATE TABLE `item_template` (
  `id` int(11) NOT NULL,
  `code` varchar(50) NOT NULL,
  `name` varchar(100) NOT NULL,
  `description` text DEFAULT NULL,
  `category` tinyint(4) NOT NULL COMMENT '1=Equipment,2=Consumable,3=Material,4=Quest',
  `item_type` tinyint(4) NOT NULL,
  `stackable` tinyint(1) NOT NULL DEFAULT 0,
  `max_stack` int(11) NOT NULL DEFAULT 1,
  `gender_limit` tinyint(4) NOT NULL DEFAULT 0 COMMENT '0=All,1=Male,2=Female',
  `class_limit` int(11) NOT NULL DEFAULT 0 COMMENT 'Bitmask; 0=All',
  `level_required` int(11) NOT NULL DEFAULT 1,
  `rarity` tinyint(4) NOT NULL DEFAULT 1 COMMENT '1-5',
  `icon_id` varchar(100) DEFAULT NULL COMMENT 'Resources/ItemIcons/<icon_id>',
  `base_stat_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: {"attack":10,"defense":5,"heal_amount":50,...}',
  `max_option_slots` tinyint(4) NOT NULL DEFAULT 0 COMMENT '0-4 random stat option slots',
  `created_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `map_config`
--

CREATE TABLE `map_config` (
  `map_id` int(11) NOT NULL,
  `map_name` varchar(100) NOT NULL,
  `spawn_points_json` text NOT NULL COMMENT 'JSON: [{"x":0,"y":0},...]',
  `min_level` int(11) NOT NULL DEFAULT 1,
  `max_level` int(11) NOT NULL DEFAULT 999,
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `map_config`
--

INSERT INTO `map_config` (`map_id`, `map_name`, `spawn_points_json`, `min_level`, `max_level`, `created_at`, `updated_at`) VALUES
(0, 'Main Map', '[{\"x\":0,\"y\":0},{\"x\":5,\"y\":0},{\"x\":-5,\"y\":0},{\"x\":0,\"y\":5}]', 1, 999, '2026-03-01 10:02:38', '2026-03-01 10:02:38');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `player_data`
--

CREATE TABLE `player_data` (
  `player_id` int(11) NOT NULL COMMENT 'FK → users.user_id',
  `character_name` varchar(50) NOT NULL DEFAULT '',
  `gender` enum('Male','Female') NOT NULL DEFAULT 'Male',
  `info_char` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: {level,experience,gold,skill_points,potential_points,element_type,gene_tier,gene_exp,is_hybrid,secondary_element,secondary_gene_tier,secondary_gene_exp,hp,max_hp,mp,max_mp,attack,defense,map_id,position_x,position_y}',
  `equipment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: {weapon,helmet,armor,pants,boots,accessory} → each: {itemTemplateId,itemCode,iconId,itemName,itemType,baseStatJson,optionStats:[]}',
  `inventory` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON array: [{slotIndex,itemTemplateId,itemCode,iconId,qty,isEquipped,optionStats:[]}]',
  `skills` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON array: [{skillCode,currentLevel,isEquipped,slotIndex}]',
  `potential_stats` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: {"attack":0,"hp":0,"mp":0,"defense":0,"gene":0} – điểm đã phân bổ',
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `player_data`
--

INSERT INTO `player_data` (`player_id`, `character_name`, `gender`, `info_char`, `equipment`, `inventory`, `skills`, `potential_stats`, `updated_at`) VALUES
(1, 'Player1', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Metal\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"map_id\":0,\"position_x\":0.3954209,\"position_y\":-3.350597}', '{\"weapon\":{\"itemTemplateId\":1,\"itemCode\":\"SWORD_001\",\"iconId\":\"client_icon_8\",\"itemName\":\"Iron Sword\",\"itemType\":1,\"baseStatJson\":\"{\"attack\":10,\"durability\":100}\",\"optionStats\":[]},\"helmet\":{\"itemTemplateId\":11,\"itemCode\":\"HELMET_IRON\",\"iconId\":\"client_icon_10\",\"itemName\":\"Iron Helmet\",\"itemType\":4,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]},\"armor\":{\"itemTemplateId\":12,\"itemCode\":\"ARMOR_IRON\",\"iconId\":\"client_icon_11\",\"itemName\":\"Iron Armor\",\"itemType\":3,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]},\"pants\":{\"itemTemplateId\":13,\"itemCode\":\"PANTS_IRON\",\"iconId\":\"client_icon_12\",\"itemName\":\"Iron Pants\",\"itemType\":5,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]},\"boots\":{\"itemTemplateId\":14,\"itemCode\":\"BOOTS_IRON\",\"iconId\":\"client_icon_13\",\"itemName\":\"Iron Boots\",\"itemType\":6,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]},\"accessory\":{\"itemTemplateId\":15,\"itemCode\":\"ACCESSORY_IRON\",\"iconId\":\"client_icon_14\",\"itemName\":\"Iron Accessory\",\"itemType\":7,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]}}', '[{\"slotIndex\":0,\"itemTemplateId\":1,\"itemCode\":\"SWORD_001\",\"iconId\":\"client_icon_8\",\"quantity\":1,\"isEquipped\":false},{\"slotIndex\":1,\"itemTemplateId\":11,\"itemCode\":\"HELMET_IRON\",\"iconId\":\"client_icon_10\",\"quantity\":1,\"isEquipped\":false},{\"slotIndex\":2,\"itemTemplateId\":12,\"itemCode\":\"ARMOR_IRON\",\"iconId\":\"client_icon_11\",\"quantity\":1,\"isEquipped\":false},{\"slotIndex\":3,\"itemTemplateId\":13,\"itemCode\":\"PANTS_IRON\",\"iconId\":\"client_icon_12\",\"quantity\":1,\"isEquipped\":false},{\"slotIndex\":4,\"itemTemplateId\":14,\"itemCode\":\"BOOTS_IRON\",\"iconId\":\"client_icon_13\",\"quantity\":1,\"isEquipped\":false},{\"slotIndex\":5,\"itemTemplateId\":15,\"itemCode\":\"ACCESSORY_IRON\",\"iconId\":\"client_icon_14\",\"quantity\":1,\"isEquipped\":false}]', '[]', '{\"attack\":0,\"hp\":0,\"mp\":0,\"defense\":0,\"gene\":0}', '2026-03-01 18:12:03'),
(2, 'Player2', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Fire\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"map_id\":0,\"position_x\":0.65,\"position_y\":-3.35}', '{\"weapon\":{\"itemTemplateId\":1,\"itemCode\":\"SWORD_001\",\"iconId\":\"client_icon_8\",\"itemName\":\"Iron Sword\",\"itemType\":1,\"baseStatJson\":\"{\"attack\":10,\"durability\":100}\",\"optionStats\":[]},\"helmet\":{\"itemTemplateId\":11,\"itemCode\":\"HELMET_IRON\",\"iconId\":\"client_icon_10\",\"itemName\":\"Iron Helmet\",\"itemType\":4,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]},\"armor\":{\"itemTemplateId\":12,\"itemCode\":\"ARMOR_IRON\",\"iconId\":\"client_icon_11\",\"itemName\":\"Iron Armor\",\"itemType\":3,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]},\"pants\":{\"itemTemplateId\":13,\"itemCode\":\"PANTS_IRON\",\"iconId\":\"client_icon_12\",\"itemName\":\"Iron Pants\",\"itemType\":5,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]},\"boots\":{\"itemTemplateId\":14,\"itemCode\":\"BOOTS_IRON\",\"iconId\":\"client_icon_13\",\"itemName\":\"Iron Boots\",\"itemType\":6,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]},\"accessory\":{\"itemTemplateId\":15,\"itemCode\":\"ACCESSORY_IRON\",\"iconId\":\"client_icon_14\",\"itemName\":\"Iron Accessory\",\"itemType\":7,\"baseStatJson\":\"{\"defense\":10}\",\"optionStats\":[]}}', '[]', '[]', '{\"attack\":0,\"hp\":0,\"mp\":0,\"defense\":0,\"gene\":0}', '2026-02-28 19:04:43'),
(3, 'Player3', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"skill_points\":0,\"potential_points\":0,\"element_type\":\"Fire\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"map_id\":0,\"position_x\":0.3954209,\"position_y\":-3.350597}', '{\"weapon\":{\"itemTemplateId\":1,\"itemCode\":\"SWORD_001\",\"iconId\":\"client_icon_8\",\"itemName\":\"Iron Sword\",\"itemType\":1,\"baseStatJson\":\"{\\u0022attack\\u0022:10,\\u0022durability\\u0022:100}\"},\"helmet\":{\"itemTemplateId\":11,\"itemCode\":\"HELMET_IRON\",\"iconId\":\"client_icon_10\",\"itemName\":\"Iron Helmet\",\"itemType\":4,\"baseStatJson\":\"{\\u0022defense\\u0022:10}\"},\"armor\":{\"itemTemplateId\":12,\"itemCode\":\"ARMOR_IRON\",\"iconId\":\"client_icon_11\",\"itemName\":\"Iron Armor\",\"itemType\":3,\"baseStatJson\":\"{\\u0022defense\\u0022:10}\"},\"pants\":{\"itemTemplateId\":13,\"itemCode\":\"PANTS_IRON\",\"iconId\":\"client_icon_12\",\"itemName\":\"Iron Pants\",\"itemType\":5,\"baseStatJson\":\"{\\u0022defense\\u0022:10}\"},\"boots\":{\"itemTemplateId\":14,\"itemCode\":\"BOOTS_IRON\",\"iconId\":\"client_icon_13\",\"itemName\":\"Iron Boots\",\"itemType\":6,\"baseStatJson\":\"{\\u0022defense\\u0022:10}\"},\"accessory\":{\"itemTemplateId\":15,\"itemCode\":\"ACCESSORY_IRON\",\"iconId\":\"client_icon_14\",\"itemName\":\"Iron Accessory\",\"itemType\":7,\"baseStatJson\":\"{\\u0022defense\\u0022:10}\"}}', '[]', '[\n  {\"skillCode\":\"FIRE_BALL\",    \"currentLevel\":2, \"isEquipped\":true,  \"slotIndex\":0},\n  {\"skillCode\":\"WATER_SHIELD\", \"currentLevel\":1, \"isEquipped\":true,  \"slotIndex\":1},\n  {\"skillCode\":\"DASH\",         \"currentLevel\":1, \"isEquipped\":false, \"slotIndex\":-1}\n]', '{\"attack\":5,\"hp\":0,\"mp\":0,\"defense\":0,\"gene\":0}', '2026-03-03 17:18:30');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `skill_template`
--

CREATE TABLE `skill_template` (
  `skill_id` int(11) NOT NULL,
  `skill_code` varchar(50) NOT NULL,
  `skill_name` varchar(100) NOT NULL,
  `description` text DEFAULT NULL,
  `element_type` varchar(20) DEFAULT NULL COMMENT 'NULL=Universal | Fire | Water | Earth | Wood | Metal',
  `max_level` int(11) NOT NULL DEFAULT 5,
  `level_to_unlock` int(11) NOT NULL DEFAULT 1 COMMENT 'Player level required to learn (reach level 1)',
  `levels_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON array: [{level_req,sp_cost,effect_value,mp_cost,desc},...]',
  `icon_id` varchar(100) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `skill_template`
--

INSERT INTO `skill_template` (`skill_id`, `skill_code`, `skill_name`, `description`, `element_type`, `max_level`, `level_to_unlock`, `levels_json`, `icon_id`, `created_at`) VALUES
(1, 'FIRE_BALL', 'Cầu Lửa', 'Phóng cầu lửa gây sát thương', 'Fire', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":10,\"desc\":\"Gây 20 sát thương\"},{\"level_req\":3,\"sp_cost\":1,\"effect_value\":35,\"mp_cost\":15,\"desc\":\"Gây 35 sát thương\"},{\"level_req\":5,\"sp_cost\":1,\"effect_value\":55,\"mp_cost\":20,\"desc\":\"Gây 55 sát thương\"},{\"level_req\":8,\"sp_cost\":2,\"effect_value\":80,\"mp_cost\":25,\"desc\":\"Gây 80 sát thương\"},{\"level_req\":12,\"sp_cost\":2,\"effect_value\":110,\"mp_cost\":30,\"desc\":\"Gây 110 sát thương\"}]', 'icon_skill_1', '2026-03-02 02:12:21'),
(2, 'FIRE_WAVE', 'Sóng Lửa', 'Tạo sóng lửa diện rộng', 'Fire', 5, 5, '[{\"level_req\":5,\"sp_cost\":1,\"effect_value\":30,\"mp_cost\":15,\"desc\":\"Gây 30 sát thương diện rộng\"},{\"level_req\":8,\"sp_cost\":1,\"effect_value\":50,\"mp_cost\":20,\"desc\":\"Gây 50 sát thương diện rộng\"},{\"level_req\":10,\"sp_cost\":2,\"effect_value\":75,\"mp_cost\":25,\"desc\":\"Gây 75 sát thương\"},{\"level_req\":15,\"sp_cost\":2,\"effect_value\":100,\"mp_cost\":30,\"desc\":\"Gây 100 sát thương\"},{\"level_req\":20,\"sp_cost\":3,\"effect_value\":140,\"mp_cost\":35,\"desc\":\"Gây 140 sát thương\"}]', 'icon_skill_2', '2026-03-02 02:12:21'),
(3, 'WATER_SHIELD', 'Khiên Nước', 'Tạo lớp khiên hấp thụ sát thương', 'Water', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":30,\"mp_cost\":12,\"desc\":\"Hấp thụ 30 sát thương\"},{\"level_req\":3,\"sp_cost\":1,\"effect_value\":50,\"mp_cost\":18,\"desc\":\"Hấp thụ 50 sát thương\"},{\"level_req\":5,\"sp_cost\":1,\"effect_value\":75,\"mp_cost\":22,\"desc\":\"Hấp thụ 75 sát thương\"},{\"level_req\":8,\"sp_cost\":2,\"effect_value\":110,\"mp_cost\":28,\"desc\":\"Hấp thụ 110 sát thương\"},{\"level_req\":12,\"sp_cost\":2,\"effect_value\":150,\"mp_cost\":35,\"desc\":\"Hấp thụ 150 sát thương\"}]', 'icon_skill_3', '2026-03-02 02:12:21'),
(4, 'HEAL_WAVE', 'Sóng Hồi Phục', 'Hồi máu cho bản thân', 'Water', 5, 3, '[{\"level_req\":3,\"sp_cost\":1,\"effect_value\":40,\"mp_cost\":20,\"desc\":\"Hồi 40 HP\"},{\"level_req\":6,\"sp_cost\":1,\"effect_value\":70,\"mp_cost\":28,\"desc\":\"Hồi 70 HP\"},{\"level_req\":9,\"sp_cost\":2,\"effect_value\":110,\"mp_cost\":35,\"desc\":\"Hồi 110 HP\"},{\"level_req\":13,\"sp_cost\":2,\"effect_value\":160,\"mp_cost\":42,\"desc\":\"Hồi 160 HP\"},{\"level_req\":18,\"sp_cost\":3,\"effect_value\":220,\"mp_cost\":50,\"desc\":\"Hồi 220 HP\"}]', 'icon_skill_4', '2026-03-02 02:12:21'),
(5, 'DASH', 'Lướt Nhanh', 'Lướt về phía trước tránh đòn', NULL, 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":1,\"mp_cost\":8,\"desc\":\"Lướt 1 đơn vị\"},{\"level_req\":3,\"sp_cost\":1,\"effect_value\":2,\"mp_cost\":10,\"desc\":\"Lướt 2 đơn vị\"},{\"level_req\":6,\"sp_cost\":1,\"effect_value\":3,\"mp_cost\":12,\"desc\":\"Lướt 3 đơn vị\"},{\"level_req\":10,\"sp_cost\":2,\"effect_value\":4,\"mp_cost\":14,\"desc\":\"Lướt 4 đơn vị\"},{\"level_req\":15,\"sp_cost\":2,\"effect_value\":5,\"mp_cost\":16,\"desc\":\"Lướt 5 đơn vị\"}]', 'icon_skill_5', '2026-03-02 02:12:21');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `stat_option_template`
--

CREATE TABLE `stat_option_template` (
  `option_id` int(11) NOT NULL,
  `option_code` varchar(50) NOT NULL,
  `option_name` varchar(100) NOT NULL,
  `stat_type` varchar(30) NOT NULL COMMENT 'attack|defense|hp|mp|crit_rate|crit_damage|move_speed|attack_speed|element_dmg',
  `value_type` enum('flat','percentage') NOT NULL DEFAULT 'flat',
  `min_value` float NOT NULL DEFAULT 0,
  `max_value` float NOT NULL DEFAULT 0,
  `applicable_item_types_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON int array. NULL = áp dụng cho tất cả equipment',
  `rarity_weight` int(11) NOT NULL DEFAULT 100 COMMENT 'Trọng số pool theo rarity: 100=common,50=uncommon,20=rare,5=epic,1=legendary',
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `stat_option_template`
--

INSERT INTO `stat_option_template` (`option_id`, `option_code`, `option_name`, `stat_type`, `value_type`, `min_value`, `max_value`, `applicable_item_types_json`, `rarity_weight`, `created_at`) VALUES
(1, 'OPT_ATK_FLAT', '+Attack (Flat)', 'attack', 'flat', 3, 20, '[1,2]', 100, '2026-03-01 10:02:38'),
(2, 'OPT_ATK_PCT', '+Attack (%)', 'attack', 'percentage', 0.02, 0.12, '[1,2]', 50, '2026-03-01 10:02:38'),
(3, 'OPT_CRIT_RATE', '+Crit Rate', 'crit_rate', 'percentage', 0.02, 0.08, '[1,2]', 40, '2026-03-01 10:02:38'),
(4, 'OPT_CRIT_DMG', '+Crit Damage', 'crit_damage', 'percentage', 0.05, 0.25, '[1,2]', 40, '2026-03-01 10:02:38'),
(5, 'OPT_ATK_SPEED', '+Attack Speed', 'attack_speed', 'percentage', 0.03, 0.15, '[1,2]', 35, '2026-03-01 10:02:38'),
(6, 'OPT_ELEM_DMG_FIRE', '+Fire DMG Bonus', 'element_dmg', 'percentage', 0.03, 0.12, '[1,2]', 20, '2026-03-01 10:02:38'),
(7, 'OPT_ELEM_DMG_WATER', '+Water DMG Bonus', 'element_dmg', 'percentage', 0.03, 0.12, '[1,2]', 20, '2026-03-01 10:02:38'),
(8, 'OPT_ELEM_DMG_EARTH', '+Earth DMG Bonus', 'element_dmg', 'percentage', 0.03, 0.12, '[1,2]', 20, '2026-03-01 10:02:38'),
(9, 'OPT_ELEM_DMG_WOOD', '+Wood DMG Bonus', 'element_dmg', 'percentage', 0.03, 0.12, '[1,2]', 20, '2026-03-01 10:02:38'),
(10, 'OPT_ELEM_DMG_METAL', '+Metal DMG Bonus', 'element_dmg', 'percentage', 0.03, 0.12, '[1,2]', 20, '2026-03-01 10:02:38'),
(11, 'OPT_DEF_FLAT', '+Defense (Flat)', 'defense', 'flat', 2, 15, '[3,4,5,6,7]', 100, '2026-03-01 10:02:38'),
(12, 'OPT_DEF_PCT', '+Defense (%)', 'defense', 'percentage', 0.02, 0.1, '[3,4,5,6,7]', 50, '2026-03-01 10:02:38'),
(13, 'OPT_HP_FLAT', '+HP (Flat)', 'hp', 'flat', 20, 150, '[3,4,5,6,7]', 100, '2026-03-01 10:02:38'),
(14, 'OPT_HP_PCT', '+HP (%)', 'hp', 'percentage', 0.02, 0.08, '[3,4,5,6,7]', 40, '2026-03-01 10:02:38'),
(15, 'OPT_MOV_SPEED', '+Move Speed', 'move_speed', 'percentage', 0.02, 0.1, '[6,7]', 30, '2026-03-01 10:02:38'),
(16, 'OPT_MP_FLAT', '+MP (Flat)', 'mp', 'flat', 10, 80, NULL, 80, '2026-03-01 10:02:38'),
(17, 'OPT_MP_PCT', '+MP (%)', 'mp', 'percentage', 0.02, 0.08, NULL, 35, '2026-03-01 10:02:38'),
(18, 'OPT_ELEM_RESIST', '+Element Resistance', 'element_resist', 'percentage', 0.02, 0.08, NULL, 25, '2026-03-01 10:02:38'),
(19, 'OPT_SKILL_COOLDOWN', '-Skill Cooldown', 'skill_cooldown', 'percentage', 0.02, 0.07, NULL, 15, '2026-03-01 10:02:38'),
(20, 'OPT_LIFESTEAL', '+Life Steal', 'lifesteal', 'percentage', 0.02, 0.06, '[1,2]', 10, '2026-03-01 10:02:38');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `users`
--

CREATE TABLE `users` (
  `user_id` int(11) NOT NULL,
  `username` varchar(50) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL COMMENT 'BCrypt hash',
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `last_login` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `users`
--

INSERT INTO `users` (`user_id`, `username`, `email`, `password_hash`, `created_at`, `last_login`) VALUES
(1, '1', '1@gmail.com', '1', '2026-02-08 19:32:35', '2026-03-01 18:13:01'),
(2, '2', '2@gmail.com', '1', '2026-02-08 21:27:21', '2026-03-01 00:38:00'),
(3, '3', 'fl2k3xb@gmail.com', '123456', '2026-03-01 18:13:26', '2026-03-03 17:19:35');

--
-- Chỉ mục cho các bảng đã đổ
--

--
-- Chỉ mục cho bảng `enemy`
--
ALTER TABLE `enemy`
  ADD PRIMARY KEY (`enemy_id`),
  ADD KEY `idx_enemy_level` (`level`),
  ADD KEY `idx_enemy_type` (`enemy_type`);

--
-- Chỉ mục cho bảng `enemy_spawns`
--
ALTER TABLE `enemy_spawns`
  ADD PRIMARY KEY (`spawn_id`),
  ADD KEY `idx_map` (`map_id`),
  ADD KEY `idx_enemy` (`enemy_type_id`);

--
-- Chỉ mục cho bảng `exp_requirements`
--
ALTER TABLE `exp_requirements`
  ADD PRIMARY KEY (`level`);

--
-- Chỉ mục cho bảng `item_template`
--
ALTER TABLE `item_template`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `code` (`code`);

--
-- Chỉ mục cho bảng `map_config`
--
ALTER TABLE `map_config`
  ADD PRIMARY KEY (`map_id`);

--
-- Chỉ mục cho bảng `player_data`
--
ALTER TABLE `player_data`
  ADD PRIMARY KEY (`player_id`);

--
-- Chỉ mục cho bảng `skill_template`
--
ALTER TABLE `skill_template`
  ADD PRIMARY KEY (`skill_id`),
  ADD UNIQUE KEY `uq_skill_code` (`skill_code`);

--
-- Chỉ mục cho bảng `stat_option_template`
--
ALTER TABLE `stat_option_template`
  ADD PRIMARY KEY (`option_id`),
  ADD UNIQUE KEY `option_code` (`option_code`),
  ADD KEY `idx_stat_type` (`stat_type`);

--
-- Chỉ mục cho bảng `users`
--
ALTER TABLE `users`
  ADD PRIMARY KEY (`user_id`),
  ADD UNIQUE KEY `uk_username` (`username`),
  ADD UNIQUE KEY `uk_email` (`email`);

--
-- AUTO_INCREMENT cho các bảng đã đổ
--

--
-- AUTO_INCREMENT cho bảng `enemy`
--
ALTER TABLE `enemy`
  MODIFY `enemy_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT cho bảng `enemy_spawns`
--
ALTER TABLE `enemy_spawns`
  MODIFY `spawn_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT cho bảng `item_template`
--
ALTER TABLE `item_template`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT cho bảng `skill_template`
--
ALTER TABLE `skill_template`
  MODIFY `skill_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT cho bảng `stat_option_template`
--
ALTER TABLE `stat_option_template`
  MODIFY `option_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=21;

--
-- AUTO_INCREMENT cho bảng `users`
--
ALTER TABLE `users`
  MODIFY `user_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=4;

--
-- Các ràng buộc cho các bảng đã đổ
--

--
-- Các ràng buộc cho bảng `enemy_spawns`
--
ALTER TABLE `enemy_spawns`
  ADD CONSTRAINT `fk_spawn_enemy` FOREIGN KEY (`enemy_type_id`) REFERENCES `enemy` (`enemy_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_spawn_map` FOREIGN KEY (`map_id`) REFERENCES `map_config` (`map_id`) ON DELETE CASCADE;

--
-- Các ràng buộc cho bảng `player_data`
--
ALTER TABLE `player_data`
  ADD CONSTRAINT `fk_player_user` FOREIGN KEY (`player_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
