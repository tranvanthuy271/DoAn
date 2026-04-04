-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Máy chủ: 127.0.0.1
-- Thời gian đã tạo: Th4 04, 2026 lúc 08:58 AM
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
-- Cấu trúc bảng cho bảng `boss_config`
--

CREATE TABLE `boss_config` (
  `boss_id` int(11) NOT NULL COMMENT 'FK → enemy.enemy_id (phải là Boss type)',
  `map_id` int(11) NOT NULL COMMENT 'FK → map_config.map_id',
  `spawn_x` float NOT NULL DEFAULT 0 COMMENT 'Tọa độ X spawn boss trên map',
  `spawn_y` float NOT NULL DEFAULT 0 COMMENT 'Tọa độ Y spawn boss trên map',
  `min_spawn_hour` int(11) NOT NULL DEFAULT 0 COMMENT 'Giờ bắt đầu cho phép spawn (0–23)',
  `max_spawn_hour` int(11) NOT NULL DEFAULT 23 COMMENT 'Giờ kết thúc cho phép spawn (0–23)',
  `respawn_minutes` int(11) NOT NULL DEFAULT 60 COMMENT 'Thời gian hồi sinh boss (phút)',
  `is_active` tinyint(1) NOT NULL DEFAULT 1 COMMENT '1 = boss đang hoạt động'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `boss_config`
--

INSERT INTO `boss_config` (`boss_id`, `map_id`, `spawn_x`, `spawn_y`, `min_spawn_hour`, `max_spawn_hour`, `respawn_minutes`, `is_active`) VALUES
(8, 1, 25, 5, 0, 23, 60, 1),
(9, 2, -15, 8, 0, 23, 90, 1),
(10, 3, 40, -25, 0, 23, 120, 1);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `dungeon_config`
--

CREATE TABLE `dungeon_config` (
  `dungeon_id` int(11) NOT NULL,
  `dungeon_name` varchar(100) NOT NULL,
  `dungeon_type` enum('solo','multi') NOT NULL DEFAULT 'multi',
  `map_id` int(11) NOT NULL,
  `scene_name` varchar(100) NOT NULL DEFAULT '',
  `max_players` int(11) NOT NULL DEFAULT 4,
  `min_level_required` int(11) NOT NULL DEFAULT 1,
  `time_limit_seconds` int(11) NOT NULL DEFAULT 0,
  `description` text DEFAULT NULL,
  `boss_enemy_id` int(11) DEFAULT NULL,
  `reward_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL DEFAULT '{}' CHECK (json_valid(`reward_json`)),
  `thumbnail_icon_id` varchar(50) NOT NULL DEFAULT '',
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `dungeon_session`
--

CREATE TABLE `dungeon_session` (
  `session_id` int(11) NOT NULL,
  `dungeon_config_id` int(11) NOT NULL,
  `host_ip` varchar(45) NOT NULL,
  `host_port` int(11) NOT NULL DEFAULT 7777,
  `current_players` int(11) NOT NULL DEFAULT 0,
  `max_players` int(11) NOT NULL DEFAULT 4,
  `status` enum('waiting','active','ended') NOT NULL DEFAULT 'waiting',
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

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
  `base_damage` int(11) NOT NULL DEFAULT 5 COMMENT 'Sát thương cơ bản melee — làm cơ sở tính damage khi skill dùng damage_multiplier',
  `base_defense` int(11) NOT NULL DEFAULT 0,
  `move_speed` float NOT NULL DEFAULT 2,
  `attack_speed` float NOT NULL DEFAULT 1,
  `exp_reward` int(11) NOT NULL DEFAULT 10,
  `gold_reward` int(11) NOT NULL DEFAULT 5,
  `silver_reward` int(11) NOT NULL DEFAULT 20,
  `drop_items_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: [{"item_id":1,"drop_chance":0.2,"qty_min":1,"qty_max":3}]',
  `element_type` varchar(20) DEFAULT NULL COMMENT 'Nguyên tố chính: Fire/Water/Earth/Metal/Wood/Wind/None',
  `enemy_type` enum('Normal','Elite','Boss') DEFAULT 'Normal',
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `skills_json` longtext DEFAULT NULL COMMENT 'JSON array skill của quái (áp dụng cả quái thường và boss).\r\nMỗi phần tử:\r\n{\r\n  "skill_id"          : "FIRE_BREATH",   -- ID không dấu cách, dùng cho cooldown & animation\r\n  "flat_damage"       : 0,               -- damage tuyệt đối (>0 = dùng trực tiếp)\r\n  "damage_multiplier" : 2.5,             -- hệ số × base_damage (chỉ dùng khi flat_damage=0)\r\n  "element"           : "Fire",          -- nguyên tố skill (có thể khác element_type quái)\r\n  "cooldown_sec"      : 8.0,             -- giây hồi chiêu\r\n  "range"             : 5.0,             -- tầm đánh (Unity units)\r\n  "aoe"               : false,           -- true = tấn công diện\r\n  "aoe_radius"        : 3.0,             -- bán kính AoE (chỉ dùng khi aoe=true)\r\n  "animation_trigger" : "skill_fb",      -- tên Animator trigger, rỗng = không animation riêng\r\n  "status_effect"     : "Burn",          -- hiệu ứng trạng thái (rỗng = không có)\r\n  "duration_sec"      : 3.0,             -- thời gian duy trì status_effect\r\n  "spawn_enemy_id"    : 0,               -- ID qu',
  `khang_hoa` int(11) NOT NULL DEFAULT 0 COMMENT 'Kháng nguyên tố Hỏa (%)',
  `khang_thuy` int(11) NOT NULL DEFAULT 0 COMMENT 'Kháng nguyên tố Thủy (%)',
  `khang_tho` int(11) NOT NULL DEFAULT 0 COMMENT 'Kháng nguyên tố Thổ (%)',
  `khang_moc` int(11) NOT NULL DEFAULT 0 COMMENT 'Kháng nguyên tố Mộc (%)',
  `khang_kim` int(11) NOT NULL DEFAULT 0 COMMENT 'Kháng nguyên tố Kim (%)',
  `khang_phong` int(11) NOT NULL DEFAULT 0 COMMENT 'Kháng nguyên tố Phong (%)',
  `tang_dame_hoa` int(11) NOT NULL DEFAULT 0 COMMENT 'Tăng sát thương Hỏa (%)',
  `tang_dame_thuy` int(11) NOT NULL DEFAULT 0 COMMENT 'Tăng sát thương Thủy (%)',
  `tang_dame_tho` int(11) NOT NULL DEFAULT 0 COMMENT 'Tăng sát thương Thổ (%)',
  `tang_dame_moc` int(11) NOT NULL DEFAULT 0 COMMENT 'Tăng sát thương Mộc (%)',
  `tang_dame_kim` int(11) NOT NULL DEFAULT 0 COMMENT 'Tăng sát thương Kim (%)',
  `tang_dame_phong` int(11) NOT NULL DEFAULT 0 COMMENT 'Tăng sát thương Phong (%)',
  `hp_regen_per_sec` int(11) NOT NULL DEFAULT 0 COMMENT 'Hồi HP mỗi giây',
  `evasion_rate` int(11) NOT NULL DEFAULT 0 COMMENT 'Tỉ lệ né tránh (%)',
  `counter_rate` int(11) NOT NULL DEFAULT 0 COMMENT 'Tỉ lệ phản đòn (%)',
  `phases_json` longtext DEFAULT NULL COMMENT 'JSON giai đoạn boss: [{"hp_pct_threshold":50,"action":"enrage",...}]'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `enemy`
--

INSERT INTO `enemy` (`enemy_id`, `enemy_name`, `enemy_description`, `level`, `base_hp`, `base_mp`, `base_damage`, `base_defense`, `move_speed`, `attack_speed`, `exp_reward`, `gold_reward`, `silver_reward`, `drop_items_json`, `element_type`, `enemy_type`, `created_at`, `updated_at`, `skills_json`, `khang_hoa`, `khang_thuy`, `khang_tho`, `khang_moc`, `khang_kim`, `khang_phong`, `tang_dame_hoa`, `tang_dame_thuy`, `tang_dame_tho`, `tang_dame_moc`, `tang_dame_kim`, `tang_dame_phong`, `hp_regen_per_sec`, `evasion_rate`, `counter_rate`, `phases_json`) VALUES
(1, 'Slime', 'Quái yếu nhưng đông', 1, 50, 0, 8, 0, 1.5, 1, 50, 5, 20, '[{\"item_id\":27,\"drop_chance\":0.3,\"qty_min\":1,\"qty_max\":2},{\"item_id\":1,\"drop_chance\":0.2,\"qty_min\":1,\"qty_max\":1}]', 'Water', 'Normal', '2026-03-08 13:29:15', '2026-03-30 05:01:41', '[\r\n  {\r\n    \"skill_id\"          : \"MELEE_BITE\",\r\n    \"flat_damage\"       : 12,\r\n    \"damage_multiplier\" : 0,\r\n    \"element\"           : \"Water\",\r\n    \"cooldown_sec\"      : 2.5,\r\n    \"range\"             : 1.8,\r\n    \"aoe\"               : false,\r\n    \"aoe_radius\"        : 0,\r\n    \"animation_trigger\" : \"Attack\",\r\n    \"status_effect\"     : \"\",\r\n    \"duration_sec\"      : 0,\r\n    \"spawn_enemy_id\"    : 0,\r\n    \"spawn_count\"       : 0\r\n  },\r\n  {\r\n    \"skill_id\"          : \"WATER_BURST\",\r\n    \"flat_damage\"       : 0,\r\n    \"damage_multiplier\" : 1.5,\r\n    \"element\"           : \"Water\",\r\n    \"cooldown_sec\"      : 8.0,\r\n    \"range\"             : 5.0,\r\n    \"aoe\"               : true,\r\n    \"aoe_radius\"        : 2.0,\r\n    \"animation_trigger\" : \"skill_waterBurst\",\r\n    \"status_effect\"     : \"Slow\",\r\n    \"duration_sec\"      : 2.0,\r\n    \"spawn_enemy_id\"    : 0,\r\n    \"spawn_count\"       : 0\r\n  }\r\n]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(2, 'Goblin', 'Nhanh nhẹn nhưng yếu', 2, 80, 0, 12, 2, 2.5, 1.2, 20, 10, 40, '[{\"item_id\":11,\"drop_chance\":0.15,\"qty_min\":1,\"qty_max\":1},{\"item_id\":29,\"drop_chance\":0.4,\"qty_min\":1,\"qty_max\":2}]', 'Earth', 'Normal', '2026-03-08 13:29:15', '2026-03-29 01:00:53', '[\r\n   {\r\n     \"skill_id\"          : \"DIRT_THROW\",\r\n     \"flat_damage\"       : 15,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"Earth\",\r\n     \"cooldown_sec\"      : 5.0,\r\n     \"range\"             : 4.0,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_dirtThrow\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(3, 'Orc Warrior', 'Orc có giáp, chậm nhưng mạnh', 3, 150, 0, 20, 5, 2, 1, 50, 25, 100, '[{\"item_id\":26,\"drop_chance\":0.4,\"qty_min\":1,\"qty_max\":3},{\"item_id\":2,\"drop_chance\":0.25,\"qty_min\":1,\"qty_max\":2}]', 'Water', 'Normal', '2026-03-08 13:29:15', '2026-03-29 01:00:53', '[\r\n   {\r\n     \"skill_id\"          : \"ICE_BITE\",\r\n     \"flat_damage\"       : 25,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"Water\",\r\n     \"cooldown_sec\"      : 4.0,\r\n     \"range\"             : 1.5,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_iceBite\",\r\n     \"status_effect\"     : \"Freeze\",\r\n     \"duration_sec\"      : 2.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"ICE_HOWL\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 2.0,\r\n     \"element\"           : \"Water\",\r\n     \"cooldown_sec\"      : 12.0,\r\n     \"range\"             : 3.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 3.0,\r\n     \"animation_trigger\" : \"skill_iceHowl\",\r\n     \"status_effect\"     : \"Slow\",\r\n     \"duration_sec\"      : 3.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(4, 'Fire Slime', 'Slime hệ Hỏa', 2, 70, 20, 35, 0, 1.5, 1, 15, 8, 30, '[{\"item_id\":30,\"drop_chance\":0.35,\"qty_min\":1,\"qty_max\":2},{\"item_id\":21,\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1}]', 'Earth', 'Normal', '2026-03-08 13:29:15', '2026-03-29 01:00:53', '[\r\n   {\r\n     \"skill_id\"          : \"EARTH_SLAM\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 3.0,\r\n     \"element\"           : \"Earth\",\r\n     \"cooldown_sec\"      : 8.0,\r\n     \"range\"             : 2.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 2.5,\r\n     \"animation_trigger\" : \"skill_earthSlam\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"CHARGE\",\r\n     \"flat_damage\"       : 80,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"None\",\r\n     \"cooldown_sec\"      : 15.0,\r\n     \"range\"             : 6.0,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_charge\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"SUMMON_ADD\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"None\",\r\n     \"cooldown_sec\"      : 25.0,\r\n     \"range\"             : 5.0,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_summon\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 2,\r\n     \"spawn_count\"       : 3\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(5, 'Boss Dragon', 'Rồng Boss cực mạnh', 10, 1000, 200, 22, 20, 3, 2, 500, 200, 800, '[{\"item_id\":203,\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1},{\"item_id\":5,\"drop_chance\":0.6,\"qty_min\":2,\"qty_max\":5},{\"item_id\":28,\"drop_chance\":0.4,\"qty_min\":1,\"qty_max\":2}]', 'Fire', 'Boss', '2026-03-08 13:29:15', '2026-03-29 01:00:53', '[\r\n   {\r\n     \"skill_id\"          : \"FIRE_BURST\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 2.0,\r\n     \"element\"           : \"Fire\",\r\n     \"cooldown_sec\"      : 7.0,\r\n     \"range\"             : 3.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 2.0,\r\n     \"animation_trigger\" : \"skill_fireBurst\",\r\n     \"status_effect\"     : \"Burn\",\r\n     \"duration_sec\"      : 3.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(6, 'Goblin Archer', NULL, 8, 200, 0, 18, 3, 2.5, 1, 60, 8, 25, NULL, 'Earth', 'Normal', '2026-03-29 01:00:53', '2026-03-29 01:00:53', '[\r\n   {\r\n     \"skill_id\"          : \"QUICK_SHOT\",\r\n     \"flat_damage\"       : 25,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"Earth\",\r\n     \"cooldown_sec\"      : 3.0,\r\n     \"range\"             : 7.0,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_quickShot\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"ARROW_RAIN\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 2.5,\r\n     \"element\"           : \"Earth\",\r\n     \"cooldown_sec\"      : 14.0,\r\n     \"range\"             : 8.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 3.5,\r\n     \"animation_trigger\" : \"skill_arrowRain\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(7, 'Snow Goblin', NULL, 10, 220, 0, 18, 5, 2.5, 1, 65, 8, 25, NULL, 'Water', 'Normal', '2026-03-29 01:00:53', '2026-03-29 01:00:53', '[\r\n   {\r\n     \"skill_id\"          : \"ICE_SHARD\",\r\n     \"flat_damage\"       : 30,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"Water\",\r\n     \"cooldown_sec\"      : 5.0,\r\n     \"range\"             : 5.0,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_iceShard\",\r\n     \"status_effect\"     : \"Slow\",\r\n     \"duration_sec\"      : 2.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(8, 'Fire Dragon', NULL, 15, 3000, 0, 60, 20, 2, 0.8, 800, 200, 500, NULL, 'Fire', 'Boss', '2026-03-29 01:00:54', '2026-03-29 01:00:54', '[\r\n   {\r\n     \"skill_id\"          : \"FIRE_BREATH\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 3.5,\r\n     \"element\"           : \"Fire\",\r\n     \"cooldown_sec\"      : 8.0,\r\n     \"range\"             : 5.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 4.0,\r\n     \"animation_trigger\" : \"skill_fireBreath\",\r\n     \"status_effect\"     : \"Burn\",\r\n     \"duration_sec\"      : 4.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"WING_SLAM\",\r\n     \"flat_damage\"       : 150,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"None\",\r\n     \"cooldown_sec\"      : 12.0,\r\n     \"range\"             : 3.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 3.0,\r\n     \"animation_trigger\" : \"skill_wingSlam\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"SUMMON_ADD\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"None\",\r\n     \"cooldown_sec\"      : 30.0,\r\n     \"range\"             : 5.0,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_dragonCall\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 5,\r\n     \"spawn_count\"       : 2\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(9, 'Ice Witch', NULL, 15, 2500, 0, 50, 15, 1.8, 0.9, 600, 150, 400, NULL, 'Water', 'Boss', '2026-03-29 01:00:54', '2026-03-29 01:00:54', '[\r\n   {\r\n     \"skill_id\"          : \"BLIZZARD\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 2.5,\r\n     \"element\"           : \"Water\",\r\n     \"cooldown_sec\"      : 15.0,\r\n     \"range\"             : 6.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 5.0,\r\n     \"animation_trigger\" : \"skill_blizzard\",\r\n     \"status_effect\"     : \"Freeze\",\r\n     \"duration_sec\"      : 3.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"ICE_LANCE\",\r\n     \"flat_damage\"       : 120,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"Water\",\r\n     \"cooldown_sec\"      : 5.0,\r\n     \"range\"             : 8.0,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_iceLance\",\r\n     \"status_effect\"     : \"Slow\",\r\n     \"duration_sec\"      : 2.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(10, 'Final Dragon', NULL, 25, 8000, 0, 100, 30, 2, 0.7, 2000, 500, 1000, NULL, 'Fire', 'Boss', '2026-03-29 01:00:54', '2026-03-29 01:00:54', '[\r\n   {\r\n     \"skill_id\"          : \"MULTI_BREATH\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 4.0,\r\n     \"element\"           : \"Fire\",\r\n     \"cooldown_sec\"      : 10.0,\r\n     \"range\"             : 6.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 5.0,\r\n     \"animation_trigger\" : \"skill_multiBreath\",\r\n     \"status_effect\"     : \"Burn\",\r\n     \"duration_sec\"      : 4.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"WING_STORM\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 2.5,\r\n     \"element\"           : \"Wind\",\r\n     \"cooldown_sec\"      : 15.0,\r\n     \"range\"             : 4.0,\r\n     \"aoe\"               : true,\r\n     \"aoe_radius\"        : 6.0,\r\n     \"animation_trigger\" : \"skill_wingStorm\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 0,\r\n     \"spawn_count\"       : 0\r\n   },\r\n   {\r\n     \"skill_id\"          : \"SUMMON_ADD\",\r\n     \"flat_damage\"       : 0,\r\n     \"damage_multiplier\" : 0.0,\r\n     \"element\"           : \"None\",\r\n     \"cooldown_sec\"      : 40.0,\r\n     \"range\"             : 5.0,\r\n     \"aoe\"               : false,\r\n     \"aoe_radius\"        : 0.0,\r\n     \"animation_trigger\" : \"skill_dragonSummon\",\r\n     \"status_effect\"     : \"\",\r\n     \"duration_sec\"      : 0.0,\r\n     \"spawn_enemy_id\"    : 8,\r\n     \"spawn_count\"       : 1\r\n   }\r\n ]', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, NULL),
(11, 'Đế Băng', 'Hoàng đế băng hà cổ đại bị phong ấn', 15, 2200, 500, 120, 35, 2, 1.2, 900, 380, 1500, '[{\"item_id\":37,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":2},{\"item_id\":207,\"drop_chance\":0.08,\"qty_min\":1,\"qty_max\":1},{\"item_id\":31,\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1}]', 'Water', 'Boss', '2026-04-01 00:00:00', '2026-04-01 00:00:00', '[{\"skill_id\":\"ICE_STORM\",\"damage_multiplier\":2.0,\"element\":\"Water\",\"cooldown_sec\":10,\"range\":7,\"aoe\":true,\"animation_trigger\":\"skill_storm\"},{\"skill_id\":\"FREEZE\",\"damage_multiplier\":1.0,\"element\":\"Water\",\"cooldown_sec\":6,\"range\":4,\"status_effect\":\"frozen\",\"duration_sec\":3,\"animation_trigger\":\"skill_freeze\"},{\"skill_id\":\"BLIZZARD\",\"damage_multiplier\":1.8,\"element\":\"Water\",\"cooldown_sec\":15,\"range\":10,\"aoe\":true,\"animation_trigger\":\"skill_blizzard\"}]', 0, 75, 0, 0, 0, 0, 0, 45, 0, 0, 0, 0, 8, 20, 12, '[{\"hp_pct_threshold\":70,\"action\":\"enrage\",\"damage_multiplier\":1.3,\"speed_multiplier\":1.1,\"message\":\"Đế Băng thức tỉnh!\"},{\"hp_pct_threshold\":40,\"action\":\"encase\",\"message\":\"Đế Băng phong ấn cả chiến trường!\",\"aoe_freeze\":true},{\"hp_pct_threshold\":20,\"action\":\"berserk\",\"damage_multiplier\":2.2,\"speed_multiplier\":1.4,\"message\":\"Đế Băng huy động toàn lực!\"}]'),
(12, 'Mộc Linh', 'Tinh linh rừng, ẩn trong bóng cây', 8, 150, 30, 16, 4, 1.8, 1, 35, 16, 65, '[{\"item_id\":27,\"drop_chance\":0.45,\"qty_min\":1,\"qty_max\":3},{\"item_id\":25,\"drop_chance\":0.08,\"qty_min\":1,\"qty_max\":1}]', 'Wood', 'Normal', '2026-04-01 00:00:00', '2026-04-01 00:00:00', NULL, 0, 0, 0, 50, 0, 0, 0, 0, 0, 20, 0, 0, 1, 10, 0, NULL),
(13, 'Cổ Thọ Mộc', 'Quái vật cây cổ thụ, rễ xuyên đất', 11, 450, 80, 45, 15, 1.5, 0.8, 130, 60, 240, '[{\"item_id\":27,\"drop_chance\":0.6,\"qty_min\":2,\"qty_max\":4},{\"item_id\":25,\"drop_chance\":0.12,\"qty_min\":1,\"qty_max\":1}]', 'Wood', 'Elite', '2026-04-01 00:00:00', '2026-04-01 00:00:00', NULL, 0, 0, 0, 60, 0, 0, 0, 0, 0, 30, 0, 0, 5, 8, 5, NULL),
(14, 'Rừng Chúa', 'Thực thể rừng rậm bất tử ngàn năm', 13, 1800, 400, 100, 30, 1.8, 0.9, 750, 300, 1200, '[{\"item_id\":38,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":2},{\"item_id\":222,\"drop_chance\":0.08,\"qty_min\":1,\"qty_max\":1},{\"item_id\":31,\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1}]', 'Wood', 'Boss', '2026-04-01 00:00:00', '2026-04-01 00:00:00', '[{\"skill_id\":\"ROOT\",\"damage_multiplier\":1.2,\"element\":\"Wood\",\"cooldown_sec\":7,\"range\":5,\"status_effect\":\"rooted\",\"duration_sec\":2,\"animation_trigger\":\"skill_root\"},{\"skill_id\":\"THORN_WALL\",\"damage_multiplier\":1.8,\"element\":\"Wood\",\"cooldown_sec\":10,\"range\":8,\"aoe\":true,\"animation_trigger\":\"skill_thorn\"},{\"skill_id\":\"REGROW\",\"heal_pct\":10,\"cooldown_sec\":25,\"animation_trigger\":\"skill_regrow\"}]', 0, 0, 0, 70, 0, 0, 0, 0, 0, 40, 0, 0, 10, 12, 8, '[{\"hp_pct_threshold\":60,\"action\":\"enrage\",\"damage_multiplier\":1.3,\"message\":\"Rừng Chúa triệu gọi thiên nhiên!\"},{\"hp_pct_threshold\":30,\"action\":\"heal\",\"heal_pct\":15,\"message\":\"Rừng Chúa hồi phục từ đất!\"},{\"hp_pct_threshold\":15,\"action\":\"berserk\",\"damage_multiplier\":2.5,\"speed_multiplier\":1.5,\"message\":\"Rừng Chúa đốt cháy cơn thịnh nộ!\"}]'),
(15, 'Hắc Quân Binh', 'Binh lính bóng tối trang bị đầy đủ', 15, 300, 60, 35, 20, 2, 1, 70, 30, 120, '[{\"item_id\":26,\"drop_chance\":0.3,\"qty_min\":1,\"qty_max\":2},{\"item_id\":11,\"drop_chance\":0.2,\"qty_min\":1,\"qty_max\":1}]', 'Metal', 'Normal', '2026-04-01 00:00:00', '2026-04-01 00:00:00', NULL, 0, 0, 0, 0, 50, 0, 0, 0, 0, 0, 20, 0, 0, 5, 10, NULL),
(16, 'Hắc Quân Vệ', 'Vệ sĩ tinh nhuệ của Chúa Tể Bóng Tối', 18, 600, 120, 65, 30, 2.2, 1.2, 180, 80, 320, '[{\"item_id\":26,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":3},{\"item_id\":15,\"drop_chance\":0.15,\"qty_min\":1,\"qty_max\":1}]', 'Metal', 'Elite', '2026-04-01 00:00:00', '2026-04-01 00:00:00', NULL, 0, 0, 0, 0, 65, 0, 0, 0, 0, 0, 30, 0, 0, 10, 15, NULL),
(17, 'Chúa Tể Bóng Tối', 'Ác chủ bất tử cai trị thành trì cổ đại', 20, 3500, 800, 160, 50, 2.5, 1.5, 1500, 600, 2500, '[{\"item_id\":39,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":2},{\"item_id\":40,\"drop_chance\":0.3,\"qty_min\":1,\"qty_max\":1},{\"item_id\":219,\"drop_chance\":0.06,\"qty_min\":1,\"qty_max\":1},{\"item_id\":31,\"drop_chance\":0.1,\"qty_min\":1,\"qty_max\":2}]', 'Metal', 'Boss', '2026-04-01 00:00:00', '2026-04-01 00:00:00', '[{\"skill_id\":\"DARK_SLASH\",\"damage_multiplier\":2.8,\"element\":\"Metal\",\"cooldown_sec\":6,\"range\":4,\"animation_trigger\":\"skill_slash\"},{\"skill_id\":\"SHADOW_NOVA\",\"damage_multiplier\":2.0,\"element\":\"Metal\",\"cooldown_sec\":10,\"range\":10,\"aoe\":true,\"animation_trigger\":\"skill_nova\"},{\"skill_id\":\"SUMMON_GUARDS\",\"spawn_enemy_id\":16,\"spawn_count\":2,\"cooldown_sec\":25,\"animation_trigger\":\"skill_summon\"},{\"skill_id\":\"VOID_SHIELD\",\"damage_reduction_pct\":50,\"duration_sec\":5,\"cooldown_sec\":30,\"animation_trigger\":\"skill_shield\"}]', 20, 0, 0, 0, 70, 0, 10, 0, 0, 0, 40, 0, 10, 20, 20, '[{\"hp_pct_threshold\":75,\"action\":\"summon\",\"mob_id\":15,\"mob_count\":3,\"message\":\"Chúa Tể triệu hồi quân binh!\"},{\"hp_pct_threshold\":50,\"action\":\"enrage\",\"damage_multiplier\":1.4,\"speed_multiplier\":1.2,\"message\":\"Chúa Tể kích hoạt giáp bóng tối!\"},{\"hp_pct_threshold\":25,\"action\":\"berserk\",\"damage_multiplier\":2.5,\"speed_multiplier\":1.5,\"skill_cooldown_multiplier\":0.4,\"message\":\"Chúa Tể dùng tuyệt kỹ cuối cùng!\"}]');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `enemy_spawns`
--

CREATE TABLE `enemy_spawns` (
  `spawn_id` int(11) NOT NULL,
  `map_id` int(11) NOT NULL COMMENT 'FK → map_config.map_id',
  `enemy_type_id` int(11) NOT NULL COMMENT 'FK → enemy.enemy_id',
  `spawn_x` float NOT NULL DEFAULT 0,
  `spawn_y` float NOT NULL DEFAULT 0,
  `max_spawn_count` int(11) NOT NULL DEFAULT 1,
  `respawn_time` int(11) NOT NULL DEFAULT 30 COMMENT 'Giây',
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `equipment_upgrade_config`
--

CREATE TABLE `equipment_upgrade_config` (
  `upgrade_level` tinyint(4) NOT NULL COMMENT '+1 ~ +20',
  `silver_cost` int(11) NOT NULL,
  `stone_id` int(11) NOT NULL COMMENT 'FK → item_template.id',
  `stone_needed` tinyint(4) NOT NULL COMMENT 'đá cần dùng cho tỉ lệ base',
  `stone_min` tinyint(4) NOT NULL COMMENT 'đá tối thiểu',
  `base_success_rate` float NOT NULL,
  `fail_policy` tinyint(1) NOT NULL DEFAULT 0 COMMENT '0=an toàn 1=-1bậc 2=về+0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Đang đổ dữ liệu cho bảng `equipment_upgrade_config`
--

INSERT INTO `equipment_upgrade_config` (`upgrade_level`, `silver_cost`, `stone_id`, `stone_needed`, `stone_min`, `base_success_rate`, `fail_policy`) VALUES
(1, 1000, 1, 3, 1, 1, 0),
(2, 2000, 1, 5, 2, 1, 0),
(3, 4000, 1, 8, 3, 0.95, 0),
(4, 8000, 2, 5, 2, 0.9, 0),
(5, 15000, 2, 7, 3, 0.85, 0),
(6, 25000, 2, 10, 4, 0.8, 0),
(7, 40000, 3, 5, 2, 0.75, 1),
(8, 60000, 3, 7, 3, 0.7, 1),
(9, 90000, 3, 10, 4, 0.65, 1),
(10, 130000, 4, 5, 3, 0.6, 1),
(11, 180000, 4, 7, 3, 0.55, 1),
(12, 250000, 4, 10, 4, 0.5, 1),
(13, 350000, 5, 5, 3, 0.45, 1),
(14, 480000, 5, 7, 3, 0.4, 1),
(15, 650000, 5, 10, 4, 0.35, 1),
(16, 900000, 6, 5, 3, 0.3, 1),
(17, 1200000, 6, 7, 3, 0.28, 1),
(18, 1600000, 6, 10, 5, 0.25, 1),
(19, 2200000, 7, 10, 5, 0.2, 1),
(20, 3000000, 7, 15, 7, 0.15, 1),
(21, 4200000, 42, 20, 8, 0.12, 1),
(22, 5500000, 42, 25, 10, 0.1, 1),
(23, 7000000, 43, 30, 12, 0.08, 2),
(24, 9000000, 43, 40, 15, 0.06, 2);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `exp_requirements`
--

CREATE TABLE `exp_requirements` (
  `level` int(11) NOT NULL,
  `exp_required` int(11) NOT NULL COMMENT 'Tổng EXP cần để ĐẠT level này',
  `base_stat_increase` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `potential_points_reward` int(11) NOT NULL DEFAULT 5,
  `skill_points_reward` int(11) NOT NULL DEFAULT 1,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `exp_requirements`
--

INSERT INTO `exp_requirements` (`level`, `exp_required`, `base_stat_increase`, `potential_points_reward`, `skill_points_reward`, `created_at`) VALUES
(1, 0, '{\"hp\":0,  \"mp\":0,  \"attack\":0, \"defense\":0}', 0, 0, '2026-03-08 13:29:15'),
(2, 100, '{\"hp\":50, \"mp\":20, \"attack\":5, \"defense\":2}', 5, 1, '2026-03-08 13:29:15'),
(3, 300, '{\"hp\":60, \"mp\":25, \"attack\":6, \"defense\":2}', 5, 1, '2026-03-08 13:29:15'),
(4, 600, '{\"hp\":70, \"mp\":30, \"attack\":7, \"defense\":3}', 5, 1, '2026-03-08 13:29:15'),
(5, 1000, '{\"hp\":80, \"mp\":35, \"attack\":8, \"defense\":3}', 5, 2, '2026-03-08 13:29:15'),
(6, 1500, '{\"hp\":90, \"mp\":40, \"attack\":9, \"defense\":3}', 5, 1, '2026-03-08 13:29:15'),
(7, 2100, '{\"hp\":100,\"mp\":45,\"attack\":10,\"defense\":4}', 5, 1, '2026-03-08 13:29:15'),
(8, 2800, '{\"hp\":110,\"mp\":50,\"attack\":11,\"defense\":4}', 5, 1, '2026-03-08 13:29:15'),
(9, 3600, '{\"hp\":120,\"mp\":55,\"attack\":12,\"defense\":4}', 5, 1, '2026-03-08 13:29:15'),
(10, 4500, '{\"hp\":150,\"mp\":70,\"attack\":15,\"defense\":5}', 7, 2, '2026-03-08 13:29:15'),
(11, 5500, '{\"hp\":130,\"mp\":60,\"attack\":13,\"defense\":5}', 5, 1, '2026-03-08 13:29:15'),
(12, 6600, '{\"hp\":140,\"mp\":65,\"attack\":14,\"defense\":5}', 5, 1, '2026-03-08 13:29:15'),
(13, 7800, '{\"hp\":150,\"mp\":70,\"attack\":15,\"defense\":6}', 5, 1, '2026-03-08 13:29:15'),
(14, 9100, '{\"hp\":160,\"mp\":75,\"attack\":16,\"defense\":6}', 5, 1, '2026-03-08 13:29:15'),
(15, 10500, '{\"hp\":200,\"mp\":90,\"attack\":20,\"defense\":8}', 7, 2, '2026-03-08 13:29:15'),
(16, 12000, '{\"hp\":170,\"mp\":80,\"attack\":17,\"defense\":7}', 5, 1, '2026-03-08 13:29:15'),
(17, 13600, '{\"hp\":180,\"mp\":85,\"attack\":18,\"defense\":7}', 5, 1, '2026-03-08 13:29:15'),
(18, 15300, '{\"hp\":190,\"mp\":90,\"attack\":19,\"defense\":7}', 5, 1, '2026-03-08 13:29:15'),
(19, 17100, '{\"hp\":200,\"mp\":95,\"attack\":20,\"defense\":8}', 5, 1, '2026-03-08 13:29:15'),
(20, 19000, '{\"hp\":250,\"mp\":120,\"attack\":25,\"defense\":10}', 10, 3, '2026-03-08 13:29:15'),
(21, 21000, '{\"hp\":260,\"mp\":126,\"attack\":26,\"defense\":10}', 5, 1, '2026-03-18 17:58:09'),
(22, 23000, '{\"hp\":270,\"mp\":132,\"attack\":27,\"defense\":11}', 5, 1, '2026-03-18 17:58:09'),
(23, 25000, '{\"hp\":280,\"mp\":138,\"attack\":28,\"defense\":11}', 5, 1, '2026-03-18 17:58:09'),
(24, 27000, '{\"hp\":290,\"mp\":144,\"attack\":29,\"defense\":12}', 5, 1, '2026-03-18 17:58:09'),
(25, 30000, '{\"hp\":300,\"mp\":150,\"attack\":30,\"defense\":12}', 7, 2, '2026-03-08 13:29:15'),
(26, 34000, '{\"hp\":320,\"mp\":160,\"attack\":32,\"defense\":13}', 5, 1, '2026-03-18 17:58:09'),
(27, 38000, '{\"hp\":340,\"mp\":170,\"attack\":34,\"defense\":13}', 5, 1, '2026-03-18 17:58:09'),
(28, 42000, '{\"hp\":360,\"mp\":180,\"attack\":36,\"defense\":14}', 5, 1, '2026-03-18 17:58:09'),
(29, 46000, '{\"hp\":380,\"mp\":190,\"attack\":38,\"defense\":15}', 5, 1, '2026-03-18 17:58:09'),
(30, 50000, '{\"hp\":400,\"mp\":200,\"attack\":40,\"defense\":16}', 7, 2, '2026-03-08 13:29:15'),
(31, 56000, '{\"hp\":420,\"mp\":210,\"attack\":42,\"defense\":17}', 5, 1, '2026-03-18 17:58:09'),
(32, 62000, '{\"hp\":440,\"mp\":220,\"attack\":44,\"defense\":17}', 5, 1, '2026-03-18 17:58:09'),
(33, 68000, '{\"hp\":460,\"mp\":230,\"attack\":46,\"defense\":18}', 5, 1, '2026-03-18 17:58:09'),
(34, 74000, '{\"hp\":480,\"mp\":240,\"attack\":48,\"defense\":19}', 5, 1, '2026-03-18 17:58:09'),
(35, 80000, '{\"hp\":500,\"mp\":250,\"attack\":50,\"defense\":20}', 7, 2, '2026-03-08 13:29:15'),
(36, 88000, '{\"hp\":520,\"mp\":260,\"attack\":52,\"defense\":21}', 5, 1, '2026-03-18 17:58:09'),
(37, 96000, '{\"hp\":540,\"mp\":270,\"attack\":54,\"defense\":21}', 5, 1, '2026-03-18 17:58:09'),
(38, 104000, '{\"hp\":560,\"mp\":280,\"attack\":56,\"defense\":22}', 5, 1, '2026-03-18 17:58:09'),
(39, 112000, '{\"hp\":580,\"mp\":290,\"attack\":58,\"defense\":23}', 5, 1, '2026-03-18 17:58:09'),
(40, 120000, '{\"hp\":600,\"mp\":300,\"attack\":60,\"defense\":24}', 7, 2, '2026-03-08 13:29:15'),
(41, 132000, '{\"hp\":630,\"mp\":314,\"attack\":63,\"defense\":25}', 5, 1, '2026-03-18 17:58:09'),
(42, 144000, '{\"hp\":660,\"mp\":328,\"attack\":66,\"defense\":26}', 5, 1, '2026-03-18 17:58:09'),
(43, 156000, '{\"hp\":690,\"mp\":342,\"attack\":69,\"defense\":27}', 5, 1, '2026-03-18 17:58:09'),
(44, 168000, '{\"hp\":720,\"mp\":356,\"attack\":72,\"defense\":28}', 5, 1, '2026-03-18 17:58:09'),
(45, 180000, '{\"hp\":750,\"mp\":370,\"attack\":75,\"defense\":30}', 7, 2, '2026-03-08 13:29:15'),
(46, 194000, '{\"hp\":800,\"mp\":396,\"attack\":80,\"defense\":32}', 5, 1, '2026-03-18 17:58:09'),
(47, 208000, '{\"hp\":850,\"mp\":422,\"attack\":85,\"defense\":34}', 5, 1, '2026-03-18 17:58:09'),
(48, 222000, '{\"hp\":900,\"mp\":448,\"attack\":90,\"defense\":36}', 5, 1, '2026-03-18 17:58:09'),
(49, 236000, '{\"hp\":950,\"mp\":474,\"attack\":95,\"defense\":38}', 5, 1, '2026-03-18 17:58:09'),
(50, 250000, '{\"hp\":1000,\"mp\":500,\"attack\":100,\"defense\":40}', 10, 3, '2026-03-08 13:29:15');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `gene_hybrid_config`
--

CREATE TABLE `gene_hybrid_config` (
  `hybrid_id` int(11) NOT NULL,
  `element_a` varchar(10) NOT NULL,
  `element_b` varchar(10) NOT NULL,
  `hybrid_name` varchar(100) NOT NULL,
  `hybrid_description` varchar(500) DEFAULT NULL,
  `bonus_target_elements` varchar(100) NOT NULL,
  `immune_elements` varchar(100) NOT NULL,
  `fusion_silver_cost` int(11) NOT NULL DEFAULT 2000000,
  `fusion_item_id` int(11) NOT NULL,
  `fusion_item_count` int(11) NOT NULL DEFAULT 5,
  `atk_bonus_percent` float NOT NULL DEFAULT 0.5,
  `stat_bonus_hp` int(11) NOT NULL DEFAULT 2000,
  `stat_bonus_mp` int(11) NOT NULL DEFAULT 500,
  `stat_bonus_atk` int(11) NOT NULL DEFAULT 500,
  `stat_bonus_def` int(11) NOT NULL DEFAULT 200,
  `prefab_path` varchar(200) NOT NULL DEFAULT '' COMMENT 'Resources path dùng cho CharacterLoader (không có Assets/ prefix và không có .prefab)',
  `primary_skill_keep_count` int(11) NOT NULL DEFAULT 3 COMMENT 'Số slot skill từ hệ chính được giữ lại sau fusion'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Config 10+5 tổ hợp Hybrid Gene';

--
-- Đang đổ dữ liệu cho bảng `gene_hybrid_config`
--

INSERT INTO `gene_hybrid_config` (`hybrid_id`, `element_a`, `element_b`, `hybrid_name`, `hybrid_description`, `bonus_target_elements`, `immune_elements`, `fusion_silver_cost`, `fusion_item_id`, `fusion_item_count`, `atk_bonus_percent`, `stat_bonus_hp`, `stat_bonus_mp`, `stat_bonus_atk`, `stat_bonus_def`, `prefab_path`, `primary_skill_keep_count`) VALUES
(1, 'Earth', 'Fire', 'Dung Nham Địa Hỏa', 'Tanker lửa đất, phản đòn thiêu đốt và cứng như đá', 'Earth,Metal', 'Water,Wood', 2000000, 49, 5, 0.5, 2500, 400, 400, 300, 'Prefabs/Player/Hybrid/Hybrid_Earth_Fire', 3),
(2, 'Earth', 'Metal', 'Thổ Kim Bất Hoại', 'Phòng thủ tối thượng, counterattack chí mạng', 'Metal,Wood', 'Wood,Fire', 2000000, 49, 5, 0.5, 3000, 300, 300, 500, 'Prefabs/Player/Hybrid/Hybrid_Earth_Metal', 3),
(3, 'Earth', 'Water', 'Băng Địa Phong', 'Siêu tanker băng đất, miễn nhiễm vật lý và lửa', 'Fire,Metal', 'Metal,Wood', 2000000, 49, 5, 0.5, 3500, 500, 200, 400, 'Prefabs/Player/Hybrid/Hybrid_Earth_Water', 3),
(4, 'Earth', 'Wood', 'Địa Mộc Vĩnh Cửu', 'Kiểm soát bản đồ, hồi máu và trói địch', 'Water,Metal', 'Metal,Wood', 2000000, 49, 5, 0.5, 2000, 600, 300, 300, 'Prefabs/Player/Hybrid/Hybrid_Earth_Wood', 3),
(5, 'Fire', 'Metal', 'Kim Hỏa Phong Thần', 'Xuyên giáp thiêu đốt, chí mạng bốc lửa', 'Earth,Wood', 'Water,Fire', 2000000, 47, 5, 0.5, 1500, 400, 700, 200, 'Prefabs/Player/Hybrid/Hybrid_Fire_Metal', 3),
(6, 'Fire', 'Water', 'Hỏa Thủy Long', 'Sức mạnh hỗn độn giữa lửa và nước vũ trụ', 'Earth,Fire', 'Water,Metal', 2000000, 47, 5, 0.5, 2000, 500, 500, 200, 'Prefabs/Player/Hybrid/Hybrid_Fire_Water', 3),
(7, 'Fire', 'Wood', 'Hỏa Mộc Liên Sinh', 'Đốt cháy và tái sinh, DoT liên tục + AoE', 'Earth,Water', 'Water,Metal', 2000000, 47, 5, 0.5, 1500, 500, 600, 150, 'Prefabs/Player/Hybrid/Hybrid_Fire_Wood', 3),
(8, 'Metal', 'Water', 'Băng Kim Xuyên Phá', 'Xuyên giáp đóng băng, sát thương lạnh ngắt', 'Wood,Fire', 'Fire,Metal', 2000000, 50, 5, 0.5, 1500, 400, 600, 250, 'Prefabs/Player/Hybrid/Hybrid_Metal_Water', 3),
(9, 'Metal', 'Wood', 'Kim Mộc Gai Độc', 'Chí mạng vật lý + độc tố liên tục, phản đòn', 'Wood,Water', 'Fire,Metal', 2000000, 50, 5, 0.5, 1000, 400, 700, 200, 'Prefabs/Player/Hybrid/Hybrid_Metal_Wood', 3),
(10, 'Water', 'Wood', 'Băng Độc Vĩnh Hằng', 'Đóng băng + độc tố, kiểm soát hoàn toàn', 'Fire,Water', 'Metal,Fire', 2000000, 48, 5, 0.5, 1500, 600, 500, 200, 'Prefabs/Player/Hybrid/Hybrid_Water_Wood', 3),
(11, 'Earth', 'Wind', 'Lốc Đất Cổ Thần', 'Đất vững chắc kết hợp với gió điên cuồng — kiểm soát địa hình và vô hiệu hóa kẻ địch.', 'Wood,Water', 'Metal,Fire', 2000000, 49, 5, 0.5, 2800, 500, 450, 350, 'Prefabs/Player/Hybrid/Hybrid_Earth_Wind', 3),
(12, 'Fire', 'Wind', 'Bão Lửa Thiên Ma', 'Ngọn lửa cuồng nộ được gió mang đi khắp chiến trường — phạm vi sát thương cực rộng.', 'Metal,Wood', 'Water,Earth', 2000000, 47, 5, 0.5, 2200, 550, 700, 180, 'Prefabs/Player/Hybrid/Hybrid_Fire_Wind', 3),
(13, 'Metal', 'Wind', 'Kim Phong Thoán Thế', 'Kiếm kim loại sắc bén lướt theo cơn gió — tốc độ và sát thương phong trào vô song.', 'Wood,Fire', 'Fire,Earth', 2000000, 50, 5, 0.5, 2000, 500, 750, 200, 'Prefabs/Player/Hybrid/Hybrid_Metal_Wind', 3),
(14, 'Water', 'Wind', 'Băng Lốc Huyết Hải', 'Băng giá và bão tố kết hợp — làm chậm và đóng băng kẻ thù trong vùng bão tuyết.', 'Fire,Earth', 'Wood,Metal', 2000000, 48, 5, 0.5, 3200, 600, 400, 250, 'Prefabs/Player/Hybrid/Hybrid_Water_Wind', 3),
(15, 'Wind', 'Wood', 'Lâm Phong Thiên Địa', 'Rừng sinh sôi theo cơn gió — hệ thống hồi phục và dây trói kiểm soát chiến trường.', 'Earth,Water', 'Metal,Fire', 2000000, 52, 5, 0.5, 2600, 650, 350, 300, 'Prefabs/Player/Hybrid/Hybrid_Wind_Wood', 3);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `gene_hybrid_skill`
--

CREATE TABLE `gene_hybrid_skill` (
  `id` int(11) NOT NULL,
  `hybrid_id` int(11) NOT NULL COMMENT 'FK → gene_hybrid_config.hybrid_id',
  `skill_code` varchar(50) NOT NULL COMMENT 'Khớp với skill_template.skill_code',
  `slot_priority` int(11) NOT NULL DEFAULT 4 COMMENT 'Slot index trong hotbar (0-based). Hybrid skill luôn là slot 3)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Ánh xạ mỗi hybrid combination với skill combo đặc biệt';

--
-- Đang đổ dữ liệu cho bảng `gene_hybrid_skill`
--

INSERT INTO `gene_hybrid_skill` (`id`, `hybrid_id`, `skill_code`, `slot_priority`) VALUES
(1, 1, 'HYBRID_EARTH_FIRE_ERUPTION', 3),
(10, 10, 'HYBRID_WATER_WOOD_VENOM', 3),
(13, 13, 'HYBRID_METAL_WIND_GALE', 3);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `gene_multi_config`
--

CREATE TABLE `gene_multi_config` (
  `tier_from` tinyint(4) NOT NULL COMMENT '1~4: tier hiện tại của hệ phụ',
  `element_type` varchar(10) NOT NULL,
  `gene_exp_required` int(11) NOT NULL,
  `silver_cost` int(11) NOT NULL,
  `stone_id` int(11) NOT NULL,
  `stone_needed` tinyint(4) NOT NULL,
  `stone_min` tinyint(4) NOT NULL,
  `base_success_rate` float NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Config nâng cấp hệ gene thứ hai (secondary element)';

--
-- Đang đổ dữ liệu cho bảng `gene_multi_config`
--

INSERT INTO `gene_multi_config` (`tier_from`, `element_type`, `gene_exp_required`, `silver_cost`, `stone_id`, `stone_needed`, `stone_min`, `base_success_rate`) VALUES
(1, 'Earth', 600, 12000, 17, 5, 2, 0.8),
(1, 'Fire', 600, 12000, 17, 5, 2, 0.8),
(1, 'Metal', 600, 12000, 17, 5, 2, 0.8),
(1, 'Water', 600, 12000, 17, 5, 2, 0.8),
(1, 'Wind', 500, 12000, 17, 3, 1, 0.8),
(1, 'Wood', 600, 12000, 17, 5, 2, 0.8),
(2, 'Earth', 2400, 60000, 18, 8, 3, 0.65),
(2, 'Fire', 2400, 60000, 18, 8, 3, 0.65),
(2, 'Metal', 2400, 60000, 18, 8, 3, 0.65),
(2, 'Water', 2400, 60000, 18, 8, 3, 0.65),
(2, 'Wind', 2000, 60000, 18, 5, 2, 0.65),
(2, 'Wood', 2400, 60000, 18, 8, 3, 0.65),
(3, 'Earth', 9600, 240000, 19, 10, 5, 0.5),
(3, 'Fire', 9600, 240000, 19, 10, 5, 0.5),
(3, 'Metal', 9600, 240000, 19, 10, 5, 0.5),
(3, 'Water', 9600, 240000, 19, 10, 5, 0.5),
(3, 'Wind', 8000, 240000, 19, 7, 3, 0.5),
(3, 'Wood', 9600, 240000, 19, 10, 5, 0.5),
(4, 'Earth', 24000, 600000, 20, 12, 6, 0.35),
(4, 'Fire', 24000, 600000, 20, 12, 6, 0.35),
(4, 'Metal', 24000, 600000, 20, 12, 6, 0.35),
(4, 'Water', 24000, 600000, 20, 12, 6, 0.35),
(4, 'Wind', 20000, 600000, 20, 10, 4, 0.35),
(4, 'Wood', 24000, 600000, 20, 12, 6, 0.35);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `gene_tier_stat_config`
--

CREATE TABLE `gene_tier_stat_config` (
  `element_type` varchar(10) NOT NULL COMMENT 'Tên hệ gene: Fire, Water, Earth, Metal, Wood',
  `tier_to` tinyint(4) NOT NULL COMMENT 'Tier đạt được sau khi nâng cấp (2, 3, 4, 5)',
  `hp_bonus` int(11) NOT NULL DEFAULT 0 COMMENT 'Bonus MaxHp cộng thêm khi đạt tier này',
  `mp_bonus` int(11) NOT NULL DEFAULT 0 COMMENT 'Bonus MaxMp cộng thêm khi đạt tier này',
  `attack_bonus` int(11) NOT NULL DEFAULT 0 COMMENT 'Bonus Attack cộng thêm khi đạt tier này',
  `defense_bonus` int(11) NOT NULL DEFAULT 0 COMMENT 'Bonus Defense cộng thêm khi đạt tier này'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci COMMENT='Config stat bonus cho gene upgrade — mỗi hệ có chỉ số riêng theo tier';

--
-- Đang đổ dữ liệu cho bảng `gene_tier_stat_config`
--

INSERT INTO `gene_tier_stat_config` (`element_type`, `tier_to`, `hp_bonus`, `mp_bonus`, `attack_bonus`, `defense_bonus`) VALUES
('Earth', 2, 250, 40, 12, 20),
('Earth', 3, 500, 80, 25, 40),
('Earth', 4, 900, 160, 50, 80),
('Earth', 5, 1600, 300, 90, 150),
('Fire', 2, 200, 50, 25, 8),
('Fire', 3, 400, 100, 50, 15),
('Fire', 4, 800, 200, 100, 30),
('Fire', 5, 1500, 400, 180, 60),
('Metal', 2, 220, 50, 20, 15),
('Metal', 3, 440, 100, 40, 30),
('Metal', 4, 850, 200, 80, 60),
('Metal', 5, 1550, 380, 145, 110),
('Water', 2, 280, 80, 15, 10),
('Water', 3, 560, 160, 30, 20),
('Water', 4, 1100, 320, 60, 40),
('Water', 5, 2000, 600, 110, 80),
('Wind', 2, 150, 60, 18, 8),
('Wind', 3, 300, 120, 36, 16),
('Wind', 4, 600, 240, 70, 32),
('Wind', 5, 1200, 480, 130, 65),
('Wood', 2, 240, 70, 10, 12),
('Wood', 3, 480, 140, 20, 25),
('Wood', 4, 900, 280, 40, 50),
('Wood', 5, 1600, 520, 75, 95);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `gene_upgrade_config`
--

CREATE TABLE `gene_upgrade_config` (
  `tier_from` tinyint(4) NOT NULL,
  `element_type` varchar(10) NOT NULL,
  `gene_exp_required` int(11) NOT NULL,
  `silver_cost` int(11) NOT NULL,
  `stone_id` int(11) NOT NULL,
  `stone_needed` tinyint(4) NOT NULL,
  `stone_min` tinyint(4) NOT NULL,
  `base_success_rate` float NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Đang đổ dữ liệu cho bảng `gene_upgrade_config`
--

INSERT INTO `gene_upgrade_config` (`tier_from`, `element_type`, `gene_exp_required`, `silver_cost`, `stone_id`, `stone_needed`, `stone_min`, `base_success_rate`) VALUES
(1, 'Earth', 500, 10000, 17, 5, 2, 0.8),
(1, 'Fire', 500, 10000, 17, 5, 2, 0.8),
(1, 'Metal', 500, 10000, 17, 5, 2, 0.8),
(1, 'Water', 500, 10000, 17, 5, 2, 0.8),
(1, 'Wind', 500, 10000, 17, 3, 1, 0.8),
(1, 'Wood', 500, 10000, 17, 5, 2, 0.8),
(2, 'Earth', 2000, 50000, 18, 8, 3, 0.65),
(2, 'Fire', 2000, 50000, 18, 8, 3, 0.65),
(2, 'Metal', 2000, 50000, 18, 8, 3, 0.65),
(2, 'Water', 2000, 50000, 18, 8, 3, 0.65),
(2, 'Wind', 2000, 50000, 18, 5, 2, 0.65),
(2, 'Wood', 2000, 50000, 18, 8, 3, 0.65),
(3, 'Earth', 8000, 200000, 19, 10, 5, 0.5),
(3, 'Fire', 8000, 200000, 19, 10, 5, 0.5),
(3, 'Metal', 8000, 200000, 19, 10, 5, 0.5),
(3, 'Water', 8000, 200000, 19, 10, 5, 0.5),
(3, 'Wind', 8000, 200000, 19, 7, 3, 0.5),
(3, 'Wood', 8000, 200000, 19, 10, 5, 0.5),
(4, 'Earth', 20000, 500000, 20, 12, 6, 0.35),
(4, 'Fire', 20000, 500000, 20, 12, 6, 0.35),
(4, 'Metal', 20000, 500000, 20, 12, 6, 0.35),
(4, 'Water', 20000, 500000, 20, 12, 6, 0.35),
(4, 'Wind', 20000, 500000, 20, 10, 4, 0.35),
(4, 'Wood', 20000, 500000, 20, 12, 6, 0.35);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `item_effect_template`
--

CREATE TABLE `item_effect_template` (
  `id` int(10) UNSIGNED NOT NULL,
  `item_template_id` int(10) UNSIGNED NOT NULL COMMENT 'FK → item_template.id',
  `effect_type` varchar(50) NOT NULL COMMENT 'HpRestore|MpRestore|HpBuff|MpBuff|AttackBuff|DefenseBuff|GeneExpBuff|ExpBuff|PhucBuff',
  `value` int(11) NOT NULL DEFAULT 0 COMMENT 'Giá trị: số HP hồi / % tăng',
  `duration_sec` int(11) NOT NULL DEFAULT 0 COMMENT '0 = instant; >0 = timed buff (giây)',
  `icon_id` int(11) NOT NULL DEFAULT 0 COMMENT 'ID icon hiện trong HUD (0 = dùng icon item)',
  `display_name` varchar(200) NOT NULL DEFAULT '' COMMENT 'Tên hiển thị trong buff tooltip',
  `detail` varchar(500) NOT NULL DEFAULT '' COMMENT 'Mô tả chi tiết chỉ số được áp dụng',
  `sort_order` tinyint(4) NOT NULL DEFAULT 0 COMMENT 'Thứ tự hiển thị khi item có nhiều effect'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Effect/buff của từng item tiêu thụ';

--
-- Đang đổ dữ liệu cho bảng `item_effect_template`
--

INSERT INTO `item_effect_template` (`id`, `item_template_id`, `effect_type`, `value`, `duration_sec`, `icon_id`, `display_name`, `detail`, `sort_order`) VALUES
(1, 11, 'HpRestore', 200, 30, 531, 'Hồi máu', '+200 HP/s trong 30 giây', 1),
(2, 12, 'HpRestore', 500, 30, 532, 'Hồi máu', '+500 HP/s trong 30 giây', 1),
(3, 13, 'HpRestore', 1200, 30, 533, 'Hồi máu', '+1200 HP/s trong 30 giây', 1),
(5, 14, 'MpRestore', 150, 30, 538, 'Hồi linh', '+150 MP/s trong 30 giây', 2),
(6, 15, 'MpRestore', 400, 3, 539, 'Hồi linh', '+400 MP/s trong 3 giây', 2),
(7, 16, 'MpRestore', 1000, 3, 540, 'Hồi linh', '+1000 MP/s trong 3 giây', 2),
(8, 121, 'GeneExpBuff', 20, 1800, 562, 'EXP Gene +20%', '+20% EXP Gene (30 phút)', 3),
(9, 122, 'GeneExpBuff', 50, 1800, 563, 'EXP Gene +50%', '+50% EXP Gene (30 phút)', 3),
(10, 123, 'GeneExpBuff', 100, 3600, 564, 'EXP Gene +100%', '+100% EXP Gene (1 giờ)', 3),
(11, 131, 'ExpBuff', 25, 1800, 562, 'EXP +25%', '+25% EXP (30 phút)', 4),
(12, 132, 'ExpBuff', 50, 3600, 563, 'EXP +50%', '+50% EXP (1 giờ)', 4),
(13, 141, 'PhucBuff', 10, 3600, 564, 'Phúc +10%', '+10% vàng và EXP (1 giờ)', 5),
(14, 142, 'PhucBuff', 25, 7200, 564, 'Phúc +25%', '+25% vàng và EXP (2 giờ)', 5),
(15, 151, 'AttackBuff', 15, 1800, 393, 'Công +15%', '+15% sát thương (30 phút)', 6),
(16, 152, 'DefenseBuff', 15, 1800, 392, 'Thủ +15%', '+15% phòng thủ (30 phút)', 6),
(17, 161, 'HpBuff', 10, 1800, 391, 'Max HP +10%', '+10% Max HP (30 phút)', 0),
(18, 162, 'HpBuff', 20, 3600, 391, 'Max HP +20%', '+20% Max HP (1 giờ)', 0),
(19, 163, 'HpBuff', 40, 7200, 391, 'Max HP +40%', '+40% Max HP (2 giờ)', 0),
(20, 171, 'MpBuff', 10, 1800, 393, 'Max MP +10%', '+10% Max MP (30 phút)', 0),
(21, 172, 'MpBuff', 20, 3600, 393, 'Max MP +20%', '+20% Max MP (1 giờ)', 0),
(22, 173, 'MpBuff', 40, 7200, 393, 'Max MP +40%', '+40% Max MP (2 giờ)', 0);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `item_template`
--

CREATE TABLE `item_template` (
  `id` int(11) UNSIGNED NOT NULL,
  `name` varchar(200) NOT NULL,
  `detail` varchar(500) DEFAULT NULL,
  `isXepChong` varchar(5) NOT NULL DEFAULT 'False',
  `gioiTinh` tinyint(4) NOT NULL DEFAULT 2 COMMENT '0=Male 1=Female 2=All',
  `type` tinyint(4) NOT NULL COMMENT '0=Helmet 1=Weapon 2=Armor 3=Pants 4=Boots 5=Ring 21=UpgStone 22=HPPotion 23=MPPotion 24=Food 25=GeneStone 30=Material',
  `idClass` tinyint(4) NOT NULL DEFAULT 0 COMMENT '0=All 1=Fire 2=Water 3=Earth 4=Metal 5=Wood (vũ khí)',
  `idIcon` int(11) NOT NULL DEFAULT 0 COMMENT 'Admin tự config idIcon trong Unity',
  `levelNeed` smallint(6) NOT NULL DEFAULT 1,
  `taiPhuNeed` smallint(6) NOT NULL DEFAULT 0,
  `idMob` int(11) NOT NULL DEFAULT -1,
  `idChar` int(11) NOT NULL DEFAULT 0,
  `isLock` tinyint(1) NOT NULL DEFAULT 0 COMMENT 'Item bị khóa theo loại (VD: bạc khóa). 0=không khóa, 1=khóa',
  `sellPrice` int(11) NOT NULL DEFAULT 0 COMMENT 'Giá bán lại cho NPC (đơn vị bạc)'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Đang đổ dữ liệu cho bảng `item_template`
--

INSERT INTO `item_template` (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`, `isLock`, `sellPrice`) VALUES
(1, 'Đá Nâng Cấp Cấp 1', 'Dùng để nâng cấp trang bị +1~+3', 'True', 2, 21, 0, 246, 1, 0, -1, 0, 0, 0),
(2, 'Đá Nâng Cấp Cấp 2', 'Dùng để nâng cấp trang bị +4~+6', 'True', 2, 21, 0, 247, 1, 0, -1, 0, 0, 0),
(3, 'Đá Nâng Cấp Cấp 3', 'Dùng để nâng cấp trang bị +7~+9', 'True', 2, 21, 0, 248, 1, 0, -1, 0, 0, 0),
(4, 'Đá Nâng Cấp Cấp 4', 'Dùng để nâng cấp trang bị +10~+12', 'True', 2, 21, 0, 249, 20, 0, -1, 0, 0, 0),
(5, 'Đá Nâng Cấp Cấp 5', 'Dùng để nâng cấp trang bị +13~+15', 'True', 2, 21, 0, 250, 30, 0, -1, 0, 0, 0),
(6, 'Đá Nâng Cấp Cấp 6', 'Dùng để nâng cấp trang bị +16~+18', 'True', 2, 21, 0, 251, 40, 0, -1, 0, 0, 0),
(7, 'Đá Nâng Cấp Cấp 7', 'Dùng để nâng cấp trang bị +19~+20', 'True', 2, 21, 0, 252, 50, 0, -1, 0, 0, 0),
(8, 'Đá May Mắn', 'Tăng thêm 15% tỉ lệ thành công mỗi viên', 'True', 2, 21, 0, 234, 1, 0, -1, 0, 0, 0),
(9, 'Đá Bảo Vệ', 'Ngăn trang bị bị vỡ khi thất bại (dùng từ +7)', 'True', 2, 21, 0, 1122, 10, 0, -1, 0, 0, 0),
(10, 'Đá Hồi Phục', 'Khôi phục level trang bị về trước khi vỡ', 'True', 2, 21, 0, 1039, 15, 0, -1, 0, 0, 0),
(11, 'Bình HP Nhỏ', 'Hồi 200 HP mỗi giây trong 30 giây', 'True', 2, 22, 0, 409, 1, 0, -1, 0, 0, 0),
(12, 'Bình HP Vừa', 'Hồi 500 HP mỗi giây trong 30 giây', 'True', 2, 22, 0, 410, 10, 0, -1, 0, 0, 0),
(13, 'Bình HP Lớn', 'Hồi 1200 HP mỗi giây trong 30 giây', 'True', 2, 22, 0, 411, 20, 0, -1, 0, 0, 0),
(14, 'Bình MP Nhỏ', 'Hồi 150 MP mỗi giây trong 30 giây', 'True', 2, 23, 0, 236, 1, 0, -1, 0, 0, 0),
(15, 'Bình MP Vừa', 'Hồi 400 MP mỗi giây trong 3 giây', 'True', 2, 23, 0, 237, 10, 0, -1, 0, 0, 0),
(16, 'Bình MP Lớn', 'Hồi 1000 MP mỗi giây trong 3 giây', 'True', 2, 23, 0, 238, 20, 0, -1, 0, 0, 0),
(17, 'Linh Thạch Sơ Cấp', 'Nguyên liệu nâng gene tier 1→2', 'True', 2, 25, 0, 651, 1, 0, -1, 0, 0, 0),
(18, 'Linh Thạch Trung Cấp', 'Nguyên liệu nâng gene tier 2→3', 'True', 2, 25, 0, 652, 15, 0, -1, 0, 0, 0),
(19, 'Linh Thạch Cao Cấp', 'Nguyên liệu nâng gene tier 3→4', 'True', 2, 25, 0, 653, 30, 0, -1, 0, 0, 0),
(20, 'Linh Thạch Thượng Cấp', 'Nguyên liệu nâng gene tier 4→5', 'True', 2, 25, 0, 654, 45, 0, -1, 0, 0, 0),
(21, 'Tinh Chất ', 'Bổ sung 500 gene_exp hệ ', 'True', 2, 25, 1, 289, 5, 0, -1, 0, 0, 0),
(26, 'Quặng Sắt', 'Nguyên liệu rèn đồ cơ bản', 'True', 2, 30, 0, 0, 1, 0, 1, 0, 0, 0),
(27, 'Thảo Dược', 'Chế bình máu', 'True', 2, 30, 0, 0, 1, 0, 1, 0, 0, 0),
(28, 'Vảy Rồng', 'Nguyên liệu quý hiếm', 'True', 2, 30, 0, 0, 30, 0, 5, 0, 0, 0),
(29, 'Nanh Độc', 'Drop từ Goblin Độc', 'True', 2, 30, 0, 0, 10, 0, 2, 0, 0, 0),
(30, 'Tinh Thể Lửa', 'Drop từ Fire Slime', 'True', 2, 30, 0, 0, 5, 0, 4, 0, 0, 0),
(31, 'Lõi Đột Biến', 'Vật liệu hiếm để Hybrid Fusion 2 gene Tier 5. Chỉ rơi từ Boss hoặc sự kiện đặc biệt.', 'True', 2, 25, 0, 0, 50, 0, 5, 0, 0, 0),
(42, 'Đá Nâng Cấp Cấp 8', 'Dùng để nâng cấp trang bị +21~+22. Cần trang bị cấp 3x trở lên.', 'True', 2, 21, 0, 253, 30, 0, -1, 0, 0, 0),
(43, 'Đá Nâng Cấp Cấp 9', 'Dùng để nâng cấp trang bị +23~+24. Cần trang bị cấp 4x trở lên.', 'True', 2, 21, 0, 254, 40, 0, -1, 0, 0, 0),
(44, 'Đá Nâng Cấp Cấp 10', 'Đá quý hiếm, chỉ dùng cho trang bị tối thượng.', 'True', 2, 21, 0, 255, 45, 0, -1, 0, 0, 0),
(45, 'Đá Nâng Cấp Cấp 11', 'Đá cấp cao nhất phổ thông, rất hiếm.', 'True', 2, 21, 0, 256, 48, 0, -1, 0, 0, 0),
(46, 'Đá Nâng Cấp Cấp 12', 'Đá truyền thuyết, chỉ rơi từ boss tối thượng.', 'True', 2, 21, 0, 257, 50, 0, -1, 0, 0, 0),
(47, 'Lõi Đột Biến Hỏa', 'Lõi mang tinh hoa hệ Hỏa. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Hỏa.', 'True', 2, 25, 1, 0, 50, 0, -1, 0, 0, 0),
(48, 'Lõi Đột Biến Thủy', 'Lõi mang tinh hoa hệ Thủy. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Thủy.', 'True', 2, 25, 2, 0, 50, 0, -1, 0, 0, 0),
(49, 'Lõi Đột Biến Thổ', 'Lõi mang tinh hoa hệ Thổ. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Thổ.', 'True', 2, 25, 3, 0, 50, 0, -1, 0, 0, 0),
(50, 'Lõi Đột Biến Kim', 'Lõi mang tinh hoa hệ Kim. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Kim.', 'True', 2, 25, 4, 0, 50, 0, -1, 0, 0, 0),
(51, 'Lõi Đột Biến Mộc', 'Lõi mang tinh hoa hệ Mộc. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Mộc.', 'True', 2, 25, 5, 0, 50, 0, -1, 0, 0, 0),
(52, 'Lõi Đột Biến Phong', 'Lõi mang tinh hoa hệ Phong. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Phong.', 'True', 2, 25, 6, 0, 50, 0, -1, 0, 0, 0),
(100, 'Mũ Da Nam', 'Mũ da cơ bản, thích hợp nam lính mới', 'False', 0, 0, 0, 10, 1, 0, -1, 0, 0, 0),
(101, 'Mũ Sắt Nam', 'Mũ sắt bền, bảo vệ hiệu quả', 'False', 0, 0, 0, 119, 10, 0, -1, 0, 0, 0),
(102, 'Mũ Thép Nam', 'Mũ thép vững chắc của chiến binh', 'False', 0, 0, 0, 120, 20, 0, -1, 0, 0, 0),
(103, 'Mũ Chiến Binh Nam', 'Mũ cao cấp của chiến binh tinh nhuệ', 'False', 0, 0, 0, 121, 35, 0, -1, 0, 0, 0),
(104, 'Mũ Tinh Luyện Nam', 'Mũ tinh luyện bằng thuật nguyên tố', 'False', 0, 0, 0, 122, 50, 0, -1, 0, 0, 0),
(105, 'Mũ Lụa Nữ', 'Mũ lụa nhẹ nhàng dành cho nữ chiến binh', 'False', 1, 0, 0, 123, 1, 0, -1, 0, 0, 0),
(106, 'Mũ Bạc Nữ', 'Mũ khảm bạc thanh lịch', 'False', 1, 0, 0, 124, 10, 0, -1, 0, 0, 0),
(107, 'Mũ Ngọc Nữ', 'Mũ nạm ngọc quý, tăng cường ma lực', 'False', 1, 0, 0, 125, 20, 0, -1, 0, 0, 0),
(108, 'Mũ Nữ Chiến Binh', 'Mũ chiến đấu cao cấp dành cho nữ', 'False', 1, 0, 0, 126, 35, 0, -1, 0, 0, 0),
(109, 'Mũ Tinh Luyện Nữ', 'Mũ nữ tinh luyện bằng năng lượng tinh khiết', 'False', 1, 0, 0, 127, 50, 0, -1, 0, 0, 0),
(110, 'Áo Da Nam', 'Áo da cơ bản', 'False', 0, 2, 0, 11, 1, 0, -1, 0, 0, 0),
(111, 'Áo Sắt Nam', 'Áo giáp sắt rèn thủ công', 'False', 0, 2, 0, 104, 10, 0, -1, 0, 0, 0),
(112, 'Áo Thép Nam', 'Áo giáp thép của lính tinh nhuệ', 'False', 0, 2, 0, 105, 20, 0, -1, 0, 0, 0),
(113, 'Áo Chiến Binh Nam', 'Áo giáp cao cấp', 'False', 0, 2, 0, 106, 35, 0, -1, 0, 0, 0),
(114, 'Áo Tinh Luyện Nam', 'Áo tinh luyện, hấp thụ nguyên tố', 'False', 0, 2, 0, 107, 50, 0, -1, 0, 0, 0),
(115, 'Áo Lụa Nữ', 'Áo lụa nhẹ, linh hoạt trong chiến đấu', 'False', 1, 2, 0, 108, 1, 0, -1, 0, 0, 0),
(116, 'Áo Bạc Nữ', 'Áo khảm bạc, cân bằng phòng thủ và tốc độ', 'False', 1, 2, 0, 109, 10, 0, -1, 0, 0, 0),
(117, 'Áo Ngọc Nữ', 'Áo nạm ngọc, tăng MP tối đa', 'False', 1, 2, 0, 110, 20, 0, -1, 0, 0, 0),
(118, 'Áo Nữ Chiến Binh', 'Áo chiến đấu cao cấp dành cho nữ', 'False', 1, 2, 0, 111, 35, 0, -1, 0, 0, 0),
(119, 'Áo Tinh Luyện Nữ', 'Áo nữ tinh luyện bằng ánh sao', 'False', 1, 2, 0, 112, 50, 0, -1, 0, 0, 0),
(121, 'Nhân Sâm Tâm Linh', 'Tăng 20% EXP Gene trong 30 phút.', 'True', 2, 24, 0, 434, 1, 0, -1, 0, 0, 200),
(122, 'Nhân Sâm Thần Thánh', 'Tăng 50% EXP Gene trong 30 phút.', 'True', 2, 24, 0, 435, 20, 0, -1, 0, 0, 600),
(123, 'Nhân Sâm Thiên Hạ', 'Tăng 100% EXP Gene trong 1 giờ.', 'True', 2, 24, 0, 436, 40, 0, -1, 0, 0, 1500),
(130, 'Quần Da Nam', 'Quần da cơ bản', 'False', 0, 3, 0, 138, 1, 0, -1, 0, 0, 0),
(131, 'Quần Sắt Nam', 'Quần giáp sắt bảo vệ hông và đùi', 'False', 0, 3, 0, 139, 10, 0, -1, 0, 0, 0),
(132, 'Quần Thép Nam', 'Quần giáp thép vững chắc', 'False', 0, 3, 0, 140, 20, 0, -1, 0, 0, 0),
(133, 'Quần Chiến Binh Nam', 'Quần giáp cao cấp', 'False', 0, 3, 0, 141, 35, 0, -1, 0, 0, 0),
(134, 'Quần Tinh Luyện Nam', 'Quần tinh luyện, nhẹ mà bền', 'False', 0, 3, 0, 142, 50, 0, -1, 0, 0, 0),
(135, 'Quần Lụa Nữ', 'Quần lụa duyên dáng', 'False', 1, 3, 0, 143, 1, 0, -1, 0, 0, 0),
(136, 'Quần Bạc Nữ', 'Quần khảm bạc', 'False', 1, 3, 0, 144, 10, 0, -1, 0, 0, 0),
(137, 'Quần Ngọc Nữ', 'Quần nạm ngọc quý', 'False', 1, 3, 0, 145, 20, 0, -1, 0, 0, 0),
(138, 'Quần Nữ Chiến Binh', 'Quần chiến đấu cao cấp cho nữ', 'False', 1, 3, 0, 146, 35, 0, -1, 0, 0, 0),
(139, 'Quần Tinh Luyện Nữ', 'Quần nữ tinh luyện', 'False', 1, 3, 0, 147, 50, 0, -1, 0, 0, 0),
(140, 'Nhẫn Đá', 'Nhẫn đá thô, cơ bản nhất', 'False', 2, 5, 0, 113, 1, 0, -1, 0, 0, 10),
(141, 'Nhẫn Bạc', 'Nhẫn bạc, tăng chỉ số tổng thể', 'False', 2, 5, 0, 114, 10, 0, -1, 0, 0, 0),
(142, 'Nhẫn Vàng', 'Nhẫn vàng, tăng đáng kể HP và ATK', 'False', 2, 5, 0, 115, 20, 0, -1, 0, 0, 0),
(143, 'Nhẫn Ma', 'Nhẫn ám ma, chứa sức mạnh tối thượng', 'False', 2, 5, 0, 115, 35, 0, -1, 0, 0, 0),
(144, 'Nhẫn Huyền Thoại', 'Nhẫn huyền thoại, vượt qua mọi giới hạn', 'False', 2, 5, 0, 117, 50, 0, -1, 0, 0, 0),
(150, 'Giày Da Nam', 'Giày da cơ bản', 'False', 0, 4, 0, 13, 1, 0, -1, 0, 0, 0),
(151, 'Giày Sắt Nam', 'Giày sắt bảo vệ chân', 'False', 0, 4, 0, 148, 10, 0, -1, 0, 0, 0),
(152, 'Giày Thép Nam', 'Giày thép vững chắc', 'False', 0, 4, 0, 149, 20, 0, -1, 0, 0, 0),
(153, 'Giày Chiến Binh Nam', 'Giày cao cấp, tăng tốc độ', 'False', 0, 4, 0, 150, 35, 0, -1, 0, 0, 0),
(154, 'Giày Tinh Luyện Nam', 'Giày tinh luyện từ nguyên tố phong', 'False', 0, 4, 0, 151, 50, 0, -1, 0, 0, 0),
(155, 'Giày Lụa Nữ', 'Giày lụa nhẹ nhàng', 'False', 1, 4, 0, 152, 1, 0, -1, 0, 0, 0),
(156, 'Giày Bạc Nữ', 'Giày khảm bạc xinh xắn', 'False', 1, 4, 0, 153, 10, 0, -1, 0, 0, 0),
(157, 'Giày Ngọc Nữ', 'Giày nạm ngọc, tăng tốc độ di chuyển', 'False', 1, 4, 0, 154, 20, 0, -1, 0, 0, 0),
(158, 'Giày Nữ Chiến Binh', 'Giày chiến đấu cao cấp cho nữ', 'False', 1, 4, 0, 155, 35, 0, -1, 0, 0, 0),
(159, 'Giày Tinh Luyện Nữ', 'Giày nữ tinh luyện, đi như bay', 'False', 1, 4, 0, 156, 50, 0, -1, 0, 0, 0),
(161, 'Đan Cường Sinh Nhỏ', 'Tăng 10% Max HP trong 30 phút.', 'True', 2, 24, 0, 388, 5, 0, -1, 0, 0, 300),
(162, 'Đan Cường Sinh Lớn', 'Tăng 20% Max HP trong 1 giờ.', 'True', 2, 24, 0, 388, 20, 0, -1, 0, 0, 800),
(163, 'Đan Trường Thọ', 'Tăng 40% Max HP trong 2 giờ.', 'True', 2, 24, 0, 388, 40, 0, -1, 0, 0, 2000),
(171, 'Linh Dược Hồi Khí Nhỏ', 'Tăng 10% Max MP trong 30 phút.', 'True', 2, 24, 0, 390, 5, 0, -1, 0, 0, 300),
(172, 'Linh Dược Hồi Khí Lớn', 'Tăng 20% Max MP trong 1 giờ.', 'True', 2, 24, 0, 390, 20, 0, -1, 0, 0, 800),
(173, 'Linh Dược Thần Khí', 'Tăng 40% Max MP trong 2 giờ.', 'True', 2, 24, 0, 390, 40, 0, -1, 0, 0, 2000),
(200, 'Kiếm Hỏa Sơ Cấp', 'Kiếm hỏa rèn từ quặng hồng, lửa nhỏ', 'False', 2, 1, 1, 168, 1, 0, -1, 0, 0, 0),
(201, 'Kiếm Hỏa Trung Cấp', 'Lưỡi kiếm nung đỏ, toả nhiệt khi chém', 'False', 2, 1, 1, 169, 10, 0, -1, 0, 0, 0),
(202, 'Kiếm Hỏa Cao Cấp', 'Kiếm tôi trong dung nham, đỏ rực không tắt', 'False', 2, 1, 1, 170, 20, 0, -1, 0, 0, 0),
(203, 'Kiếm Hỏa Thần', 'Kiếm chứa ngọn lửa bất diệt của Thần Hỏa', 'False', 2, 1, 1, 171, 35, 0, -1, 0, 0, 0),
(204, 'Kiếm Hỏa Thượng Cấp', 'Kiếm tối cùng hệ Hỏa, đốt cháy linh hồn', 'False', 2, 1, 1, 172, 50, 0, -1, 0, 0, 0),
(205, 'Gậy Thủy Sơ Cấp', 'Cung gỗ ngấm nước, mũi tên ướt đẫm', 'False', 2, 1, 2, 183, 1, 0, -1, 0, 0, 0),
(206, 'Gậy Thủy Trung Cấp', 'Cung thủy tinh, mũi tên xuyên bão nước', 'False', 2, 1, 2, 184, 10, 0, -1, 0, 0, 0),
(207, 'Gậy Thủy Cao Cấp', 'Cung băng, đóng băng kẻ địch khi trúng', 'False', 2, 1, 2, 185, 20, 0, -1, 0, 0, 0),
(208, 'Gậy Thủy Thần', 'Cung chứa sức mạnh đại dương', 'False', 2, 1, 2, 186, 35, 0, -1, 0, 0, 0),
(209, 'Gậy Thủy Thượng Cấp', 'Cung tối cùng hệ Thủy, điều khiển thủy triều', 'False', 2, 1, 2, 187, 50, 0, -1, 0, 0, 0),
(210, 'Ám Thổ Sơ Cấp', 'Chùy đất nung, nặng nề', 'False', 2, 1, 3, 178, 1, 0, -1, 0, 0, 0),
(211, 'Ám Thổ Trung Cấp', 'Chùy granit, mỗi cú đánh rung chuyển đất', 'False', 2, 1, 3, 179, 10, 0, -1, 0, 0, 0),
(212, 'Ám Thổ Cao Cấp', 'Chùy thiên thạch, sức mạnh nặng như núi', 'False', 2, 1, 3, 180, 20, 0, -1, 0, 0, 0),
(213, 'Ám Thổ Thần', 'Chùy linh hồn đất đai cổ đại', 'False', 2, 1, 3, 181, 35, 0, -1, 0, 0, 0),
(214, 'Ám Thổ Thượng Cấp', 'Chùy tối cùng hệ Thổ, gây địa chấn', 'False', 2, 1, 3, 182, 50, 0, -1, 0, 0, 0),
(215, 'Đao Kim Sơ Cấp', 'Đao sắt mài bén, phản chiếu ánh sáng', 'False', 2, 1, 4, 128, 1, 0, -1, 0, 0, 0),
(216, 'Đao Kim Trung Cấp', 'Đao thép cao cấp, sắc bén tuyệt vời', 'False', 2, 1, 4, 129, 10, 0, -1, 0, 0, 0),
(217, 'Đao Kim Cao Cấp', 'Đao titanium – bén và không gỉ sét', 'False', 2, 1, 4, 130, 20, 0, -1, 0, 0, 0),
(218, 'Đao Kim Thần', 'Đao mang khí kim tinh nguyên tố', 'False', 2, 1, 4, 131, 35, 0, -1, 0, 0, 0),
(219, 'Đao Kim Thượng Cấp', 'Đao tối cùng hệ Kim, chém xuyên mọi giáp', 'False', 2, 1, 4, 132, 50, 0, -1, 0, 0, 0),
(220, 'Dao Mộc Sơ Cấp', 'Gậy gỗ rừng già, đơn giản hiệu quả', 'False', 2, 1, 5, 0, 1, 0, -1, 0, 0, 0),
(221, 'Dao Mộc Trung Cấp', 'Gậy trúc ma thuật, dẫn năng lượng cây cỏ', 'False', 2, 1, 5, 0, 10, 0, -1, 0, 0, 0),
(222, 'Dao Mộc Cao Cấp', 'Gậy gỗ thiêng, rễ cây bện vào từng thớ', 'False', 2, 1, 5, 0, 20, 0, -1, 0, 0, 0),
(223, 'Dao Mộc Thần', 'Gậy linh hồn đại thụ ngàn năm', 'False', 2, 1, 5, 0, 35, 0, -1, 0, 0, 0),
(224, 'Dao Mộc Thượng Cấp', 'Gậy tối cùng hệ Mộc, kết nối vũ trụ xanh', 'False', 2, 1, 5, 0, 50, 0, -1, 0, 0, 0),
(225, 'Dao Phong Sơ Cấp', 'Thương gỗ nhẹ, mỗi cú đánh tạo làn gió nhỏ', 'False', 2, 1, 6, 173, 1, 0, -1, 0, 0, 0),
(226, 'Dao Phong Trung Cấp', 'Thương bạc, kêu vù vù khi vung theo gió', 'False', 2, 1, 6, 174, 10, 0, -1, 0, 0, 0),
(227, 'Dao Phong Cao Cấp', 'Thương thép nhẹ như bấc, thuần khiết khí phong', 'False', 2, 1, 6, 175, 20, 0, -1, 0, 0, 0),
(228, 'Dao Phong Thần', 'Thương chứa tinh nguyên của Thần Phong, xuyên gió', 'False', 2, 1, 6, 176, 35, 0, -1, 0, 0, 0),
(229, 'Dao Phong Thượng Cấp', 'Thương tối cùng hệ Phong, điều khiển bão tố', 'False', 2, 1, 6, 177, 50, 0, -1, 0, 0, 0);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `map_config`
--

CREATE TABLE `map_config` (
  `map_id` int(11) NOT NULL,
  `map_name` varchar(100) NOT NULL,
  `scene_name` varchar(100) NOT NULL DEFAULT '',
  `spawn_points_json` text NOT NULL,
  `min_level` int(11) NOT NULL DEFAULT 1,
  `max_level` int(11) NOT NULL DEFAULT 999,
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `map_config`
--

INSERT INTO `map_config` (`map_id`, `map_name`, `scene_name`, `spawn_points_json`, `min_level`, `max_level`, `created_at`, `updated_at`) VALUES
(0, 'Làng Khởi Đầu', 'GameScene', '[{\"x\":0,\"y\":0},{\"x\":5,\"y\":0}]', 1, 10, '2026-03-27 06:38:35', '2026-03-27 06:38:35'),
(1, 'Cánh Đồng Lửa', 'Map1', '[{\"x\":2,\"y\":1}]', 5, 20, '2026-03-27 06:38:35', '2026-03-27 06:38:35'),
(2, 'Rừng Băng', 'Map2', '[{\"x\":0,\"y\":2}]', 15, 30, '2026-03-27 06:38:35', '2026-03-27 06:38:35'),
(3, 'Sa Mạc Phong', 'Map3', '[{\"x\":3,\"y\":0}]', 25, 40, '2026-03-27 06:38:35', '2026-03-27 06:38:35');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `map_enemy_drop`
--

CREATE TABLE `map_enemy_drop` (
  `id` int(11) NOT NULL,
  `map_id` int(11) NOT NULL COMMENT 'FK → map_config.map_id',
  `enemy_id` int(11) NOT NULL COMMENT 'FK → enemy.enemy_id',
  `item_id` int(11) NOT NULL COMMENT 'FK → item_template.id',
  `drop_chance` float NOT NULL DEFAULT 0.1 COMMENT '0.0–1.0 (0.25 = 25%)',
  `qty_min` int(11) NOT NULL DEFAULT 1,
  `qty_max` int(11) NOT NULL DEFAULT 1,
  `is_active` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `map_portal`
--

CREATE TABLE `map_portal` (
  `portal_id` int(11) NOT NULL,
  `portal_name` varchar(100) NOT NULL DEFAULT '',
  `source_map_id` int(11) NOT NULL,
  `src_x` float NOT NULL DEFAULT 0,
  `src_y` float NOT NULL DEFAULT 0,
  `src_radius` float NOT NULL DEFAULT 2,
  `dest_map_id` int(11) NOT NULL,
  `dest_scene_name` varchar(100) NOT NULL DEFAULT '',
  `dest_x` float NOT NULL DEFAULT 0,
  `dest_y` float NOT NULL DEFAULT 0,
  `portal_type` varchar(30) NOT NULL DEFAULT 'world_travel',
  `portal_direction` enum('left','right','none') NOT NULL DEFAULT 'none',
  `required_item_id` int(11) DEFAULT NULL,
  `dungeon_id` int(11) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `map_portal`
--

INSERT INTO `map_portal` (`portal_id`, `portal_name`, `source_map_id`, `src_x`, `src_y`, `src_radius`, `dest_map_id`, `dest_scene_name`, `dest_x`, `dest_y`, `portal_type`, `portal_direction`, `required_item_id`, `dungeon_id`, `is_active`) VALUES
(1, 'Cổng → Map1', 0, 18, 0, 2.5, 1, 'Map1', -3, -2, 'world_travel', 'right', NULL, NULL, 1),
(2, 'Cổng ← GameScene', 1, -18, 0, 2.5, 0, 'GameScene', 50, -2, 'world_travel', 'left', NULL, NULL, 1);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `map_spawn_config`
--

CREATE TABLE `map_spawn_config` (
  `id` int(11) NOT NULL,
  `map_id` int(11) NOT NULL COMMENT 'FK → map_config.map_id',
  `spawn_json` longtext NOT NULL DEFAULT '[]' COMMENT 'JSON array — mỗi entry = 1 điểm spawn: {enemy_id,hp,exp,cx,cy,is_boss,count,respawn_time}',
  `drop_json` longtext NOT NULL DEFAULT '[]' COMMENT 'JSON array — mỗi entry = 1 loại quái: {enemy_id, items:[{item_id,rate,qty_min,qty_max}]}',
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Cấu hình spawn enemy và tỉ lệ drop theo mapId — Unity host đọc khi khởi động scene';

--
-- Đang đổ dữ liệu cho bảng `map_spawn_config`
--

INSERT INTO `map_spawn_config` (`id`, `map_id`, `spawn_json`, `drop_json`, `updated_at`) VALUES
(1, 0, '[\n  {\"enemy_id\":1,\"hp\":120,\"exp\":28045,\"cx\":41,\"cy\":1.2,\"is_boss\":false,\"count\":1,\"respawn_time\":5,\"level\":1},\n  {\"enemy_id\":1,\"hp\":120,\"exp\":28045,\"cx\":46,\"cy\":3.83,\"is_boss\":false,\"count\":1,\"respawn_time\":5,\"level\":2},\n  {\"enemy_id\":1,\"hp\":120,\"exp\":28045,\"cx\":40.4,\"cy\":5,\"is_boss\":false,\"count\":1,\"respawn_time\":5,\"level\":1},\n  {\"enemy_id\":1,\"hp\":120,\"exp\":28045,\"cx\":38.6,\"cy\":7.5,\"is_boss\":false,\"count\":1,\"respawn_time\":5,\"level\":1},\n  {\"enemy_id\":1,\"hp\":120,\"exp\":28045,\"cx\":50,\"cy\":8,\"is_boss\":false,\"count\":1,\"respawn_time\":5,\"level\":1}\n]', '[\n   {\"enemy_id\":1,\"items\":[\n     {\"item_id\":1,\"rate\":1,\"qty_min\":1,\"qty_max\":2},\n     {\"item_id\":1,\"rate\":0.05,\"qty_min\":1,\"qty_max\":1}\n   ]},\n   {\"enemy_id\":2,\"items\":[\n     {\"item_id\":22,\"rate\":0.20,\"qty_min\":1,\"qty_max\":1},\n     {\"item_id\":10,\"rate\":0.03,\"qty_min\":1,\"qty_max\":1}\n   ]},\n   {\"enemy_id\":4,\"items\":[\n     {\"item_id\":50,\"rate\":1.00,\"qty_min\":1,\"qty_max\":1},\n     {\"item_id\":10,\"rate\":0.50,\"qty_min\":1,\"qty_max\":2},\n     {\"item_id\":21,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\n   ]}\n ]', '2026-03-30 08:05:10'),
(5, 1, '[\r\n     {\"enemy_id\":4,\"hp\":0,\"exp\":15,\"cx\":5.5,\"cy\":-2.0,\"is_boss\":false,\"count\":3,\"respawn_time\":15,\"level\":5},\r\n     {\"enemy_id\":4,\"hp\":0,\"exp\":15,\"cx\":12.0,\"cy\":1.5,\"is_boss\":false,\"count\":4,\"respawn_time\":15,\"level\":6},\r\n     {\"enemy_id\":8,\"hp\":0,\"exp\":800,\"cx\":25.0,\"cy\":5.0,\"is_boss\":true,\"count\":1,\"respawn_time\":300,\"level\":15}\r\n  ]', '[\r\n     {\"enemy_id\":4,\"items\":[\r\n       {\"item_id\":30,\"rate\":0.35,\"qty_min\":1,\"qty_max\":2},\r\n       {\"item_id\":21,\"rate\":0.05,\"qty_min\":1,\"qty_max\":1}\r\n     ]},\r\n     {\"enemy_id\":8,\"items\":[\r\n       {\"item_id\":28,\"rate\":0.40,\"qty_min\":1,\"qty_max\":2},\r\n       {\"item_id\":47,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\r\n     ]}\r\n  ]', '2026-03-29 03:30:57'),
(6, 2, '[\r\n     {\"enemy_id\":3,\"hp\":0,\"exp\":50,\"cx\":-5.0,\"cy\":-5.0,\"is_boss\":false,\"count\":2,\"respawn_time\":20,\"level\":12},\r\n     {\"enemy_id\":7,\"hp\":0,\"exp\":65,\"cx\":10.0,\"cy\":12.0,\"is_boss\":false,\"count\":3,\"respawn_time\":20,\"level\":15},\r\n     {\"enemy_id\":9,\"hp\":0,\"exp\":600,\"cx\":-15.0,\"cy\":8.0,\"is_boss\":true,\"count\":1,\"respawn_time\":300,\"level\":20}\r\n  ]', '[\r\n     {\"enemy_id\":3,\"items\":[\r\n       {\"item_id\":26,\"rate\":0.40,\"qty_min\":1,\"qty_max\":3},\r\n       {\"item_id\":2,\"rate\":0.25,\"qty_min\":1,\"qty_max\":2}\r\n     ]},\r\n     {\"enemy_id\":7,\"items\":[\r\n       {\"item_id\":17,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\r\n     ]},\r\n     {\"enemy_id\":9,\"items\":[\r\n       {\"item_id\":48,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\r\n     ]}\r\n  ]', '2026-03-29 03:30:57'),
(7, 3, '[\r\n     {\"enemy_id\":6,\"hp\":0,\"exp\":60,\"cx\":18.0,\"cy\":-10.0,\"is_boss\":false,\"count\":4,\"respawn_time\":20,\"level\":22},\r\n     {\"enemy_id\":10,\"hp\":0,\"exp\":2000,\"cx\":40.0,\"cy\":-25.0,\"is_boss\":true,\"count\":1,\"respawn_time\":600,\"level\":35}\r\n  ]', '[\r\n     {\"enemy_id\":6,\"items\":[\r\n       {\"item_id\":19,\"rate\":0.15,\"qty_min\":1,\"qty_max\":1}\r\n     ]},\r\n     {\"enemy_id\":10,\"items\":[\r\n       {\"item_id\":28,\"rate\":0.80,\"qty_min\":2,\"qty_max\":5},\r\n       {\"item_id\":31,\"rate\":0.20,\"qty_min\":1,\"qty_max\":1}\r\n     ]}\r\n  ]', '2026-03-29 03:30:57');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `npc_config`
--

CREATE TABLE `npc_config` (
  `npc_id` int(11) NOT NULL,
  `npc_name` varchar(100) NOT NULL,
  `npc_type` varchar(20) NOT NULL DEFAULT 'shop' COMMENT 'shop|quest|blacksmith|exchange|event',
  `map_id` int(11) NOT NULL DEFAULT 0,
  `pos_x` float NOT NULL DEFAULT 0,
  `pos_y` float NOT NULL DEFAULT 0,
  `dialogue_key` varchar(50) DEFAULT NULL,
  `icon_id` varchar(50) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `npc_config`
--

INSERT INTO `npc_config` (`npc_id`, `npc_name`, `npc_type`, `map_id`, `pos_x`, `pos_y`, `dialogue_key`, `icon_id`, `is_active`) VALUES
(1, 'Dược Phẩm', 'shop', 0, -4, 1.2, 'greet', 'npc_merchant_1', 1),
(2, 'Đại Tướng Lan', 'quest', 0, -5, 4.9, 'quest_intro', 'npc_quest_1', 1),
(3, 'Thợ Rèn Hắc Long', 'blacksmith', 0, 2, 3.8, 'greet', 'npc_smith_1', 1),
(5, 'Binh Khí', 'shop', 0, 15.0086, -1.90751, 'greet', 'npc_merchant_2', 1),
(7, 'Trang bị ', 'shop', 0, 25.2086, -1.90751, 'greet', 'npc_merchant_3', 1),
(8, 'Tiên Dược', 'shop', 0, 35.0086, -1.90751, 'greet', 'npc_merchant_4', 1);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `npc_dialogue`
--

CREATE TABLE `npc_dialogue` (
  `id` int(11) NOT NULL,
  `npc_id` int(11) NOT NULL,
  `dialogue_key` varchar(50) NOT NULL,
  `text_vi` varchar(1000) NOT NULL,
  `next_key` varchar(50) DEFAULT NULL,
  `action_type` varchar(20) NOT NULL DEFAULT 'none' COMMENT 'none|open_shop|give_quest|teleport'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `npc_dialogue`
--

INSERT INTO `npc_dialogue` (`id`, `npc_id`, `dialogue_key`, `text_vi`, `next_key`, `action_type`) VALUES
(1, 1, 'greet', 'Chào anh hùng! Ta có nhiều đồ tốt muốn bán. Anh hãy xem thử nhé!', 'shop_offer', 'none'),
(2, 1, 'shop_offer', 'Đây là những thứ ta đang bán. Chúc anh mua vui!', NULL, 'open_shop'),
(3, 2, 'quest_intro', 'Vùng đất phía đông đang bị quái thú hoành hành. Ta cần người hùng!', 'quest_accept', 'none'),
(4, 2, 'quest_accept', 'Hãy tiêu diệt 10 con Goblin Đen và quay lại gặp ta.', NULL, 'give_quest'),
(5, 3, 'greet', 'Mang trang bị đến đây đi, ta sẽ rèn cho mạnh hơn!', NULL, 'open_shop');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `npc_shop_item`
--

CREATE TABLE `npc_shop_item` (
  `id` int(11) NOT NULL,
  `npc_id` int(11) NOT NULL,
  `item_template_id` int(11) NOT NULL,
  `price_silver` int(11) NOT NULL DEFAULT 0,
  `price_gold` int(11) NOT NULL DEFAULT 0,
  `stock` int(11) NOT NULL DEFAULT -1 COMMENT '-1 = vô hạn',
  `required_level` int(11) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `npc_shop_item`
--

INSERT INTO `npc_shop_item` (`id`, `npc_id`, `item_template_id`, `price_silver`, `price_gold`, `stock`, `required_level`) VALUES
(1, 1, 11, 500, 0, -1, 1),
(2, 1, 12, 1500, 0, -1, 5),
(3, 1, 13, 5000, 0, -1, 10),
(4, 1, 14, 15000, 0, -1, 1),
(7, 1, 15, 15000, 0, -1, 5),
(8, 1, 16, 15000, 0, -1, 10),
(10, 1, 121, 15000, 0, -1, 1),
(11, 1, 122, 15000, 0, -1, 1),
(12, 1, 161, 15000, 0, -1, 1),
-- NPC 5: Binh Khí – bán vũ khí theo hệ (type=1)
-- Kiếm Hỏa (idClass=1)
(13, 5, 200, 1000, 0, -1, 1),
(14, 5, 201, 5000, 0, -1, 10),
(15, 5, 202, 15000, 0, -1, 20),
(16, 5, 203, 50000, 0, -1, 35),
(17, 5, 204, 150000, 0, -1, 50),
-- Gậy Thủy (idClass=2)
(18, 5, 205, 1000, 0, -1, 1),
(19, 5, 206, 5000, 0, -1, 10),
(20, 5, 207, 15000, 0, -1, 20),
(21, 5, 208, 50000, 0, -1, 35),
(22, 5, 209, 150000, 0, -1, 50),
-- Ám Thổ (idClass=3)
(23, 5, 210, 1000, 0, -1, 1),
(24, 5, 211, 5000, 0, -1, 10),
(25, 5, 212, 15000, 0, -1, 20),
(26, 5, 213, 50000, 0, -1, 35),
(27, 5, 214, 150000, 0, -1, 50),
-- Đao Kim (idClass=4)
(28, 5, 215, 1000, 0, -1, 1),
(29, 5, 216, 5000, 0, -1, 10),
(30, 5, 217, 15000, 0, -1, 20),
(31, 5, 218, 50000, 0, -1, 35),
(32, 5, 219, 150000, 0, -1, 50),
-- Dao Mộc (idClass=5)
(33, 5, 220, 1000, 0, -1, 1),
(34, 5, 221, 5000, 0, -1, 10),
(35, 5, 222, 15000, 0, -1, 20),
(36, 5, 223, 50000, 0, -1, 35),
(37, 5, 224, 150000, 0, -1, 50),
-- Dao Phong (idClass=6)
(38, 5, 225, 1000, 0, -1, 1),
(39, 5, 226, 5000, 0, -1, 10),
(40, 5, 227, 15000, 0, -1, 20),
(41, 5, 228, 50000, 0, -1, 35),
(42, 5, 229, 150000, 0, -1, 50),
-- NPC 7: Trang bị – bán mũ/áo/quần/giày/nhẫn (type=0,2,3,4,5)
-- Mũ Nam (type=0, gioiTinh=0)
(43, 7, 100, 500, 0, -1, 1),
(44, 7, 101, 3000, 0, -1, 10),
(45, 7, 102, 10000, 0, -1, 20),
(46, 7, 103, 35000, 0, -1, 35),
(47, 7, 104, 100000, 0, -1, 50),
-- Mũ Nữ (type=0, gioiTinh=1)
(48, 7, 105, 500, 0, -1, 1),
(49, 7, 106, 3000, 0, -1, 10),
(50, 7, 107, 10000, 0, -1, 20),
(51, 7, 108, 35000, 0, -1, 35),
(52, 7, 109, 100000, 0, -1, 50),
-- Áo Nam (type=2, gioiTinh=0)
(53, 7, 110, 500, 0, -1, 1),
(54, 7, 111, 3000, 0, -1, 10),
(55, 7, 112, 10000, 0, -1, 20),
(56, 7, 113, 35000, 0, -1, 35),
(57, 7, 114, 100000, 0, -1, 50),
-- Áo Nữ (type=2, gioiTinh=1)
(58, 7, 115, 500, 0, -1, 1),
(59, 7, 116, 3000, 0, -1, 10),
(60, 7, 117, 10000, 0, -1, 20),
(61, 7, 118, 35000, 0, -1, 35),
(62, 7, 119, 100000, 0, -1, 50),
-- Quần Nam (type=3, gioiTinh=0)
(63, 7, 130, 500, 0, -1, 1),
(64, 7, 131, 3000, 0, -1, 10),
(65, 7, 132, 10000, 0, -1, 20),
(66, 7, 133, 35000, 0, -1, 35),
(67, 7, 134, 100000, 0, -1, 50),
-- Quần Nữ (type=3, gioiTinh=1)
(68, 7, 135, 500, 0, -1, 1),
(69, 7, 136, 3000, 0, -1, 10),
(70, 7, 137, 10000, 0, -1, 20),
(71, 7, 138, 35000, 0, -1, 35),
(72, 7, 139, 100000, 0, -1, 50),
-- Giày Nam (type=4, gioiTinh=0)
(73, 7, 150, 500, 0, -1, 1),
(74, 7, 151, 3000, 0, -1, 10),
(75, 7, 152, 10000, 0, -1, 20),
(76, 7, 153, 35000, 0, -1, 35),
(77, 7, 154, 100000, 0, -1, 50),
-- Giày Nữ (type=4, gioiTinh=1)
(78, 7, 155, 500, 0, -1, 1),
(79, 7, 156, 3000, 0, -1, 10),
(80, 7, 157, 10000, 0, -1, 20),
(81, 7, 158, 35000, 0, -1, 35),
(82, 7, 159, 100000, 0, -1, 50),
-- Nhẫn (type=5, gioiTinh=2)
(83, 7, 140, 1000, 0, -1, 1),
(84, 7, 141, 5000, 0, -1, 10),
(85, 7, 142, 15000, 0, -1, 20),
(86, 7, 143, 50000, 0, -1, 35),
(87, 7, 144, 150000, 0, -1, 50),
-- NPC 8: Tiên Dược – bán item buff (type=24)
-- Nhân Sâm tăng Gene EXP
(88, 8, 121, 5000, 0, -1, 1),
(89, 8, 122, 15000, 0, -1, 20),
(90, 8, 123, 50000, 0, -1, 40),
-- Đan Cường Sinh tăng Max HP
(91, 8, 161, 8000, 0, -1, 5),
(92, 8, 162, 25000, 0, -1, 20),
(93, 8, 163, 80000, 0, -1, 40),
-- Linh Dược Hồi Khí tăng Max MP
(94, 8, 171, 8000, 0, -1, 5),
(95, 8, 172, 25000, 0, -1, 20),
(96, 8, 173, 80000, 0, -1, 40);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `option_template`
--

CREATE TABLE `option_template` (
  `id` int(11) NOT NULL,
  `name` varchar(200) NOT NULL COMMENT '# = placeholder giá trị',
  `type` tinyint(4) NOT NULL DEFAULT 0,
  `level` tinyint(4) NOT NULL DEFAULT 0 COMMENT 'min upgradeLevel để kích hoạt',
  `strOption` longtext NOT NULL COMMENT '20 giá trị cách nhau ;'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Đang đổ dữ liệu cho bảng `option_template`
--

INSERT INTO `option_template` (`id`, `name`, `type`, `level`, `strOption`) VALUES
(1, 'Tấn công: +#', 0, 0, '10;13;17;22;28;35;43;53;65;80;97;117;140;168;201;241;289;347;416;500'),
(2, 'Xuyên giáp: +#', 0, 0, '5;7;9;11;14;18;22;28;34;42;52;64;79;97;119;147;181;223;275;338'),
(3, 'Chí mạng: +#', 0, 0, '3;4;5;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163;200'),
(4, 'Tấn công khi chí mạng: +#%', 0, 0, '2;2;3;3;4;5;6;7;9;11;13;16;19;23;28;34;41;50;61;75'),
(5, 'Sát thương quái: +#', 0, 0, '8;10;13;16;20;25;31;39;48;60;74;92;114;141;175;217;269;333;413;512'),
(6, 'Hút máu: +#%', 0, 0, '1;1;1;1;2;2;2;2;2;3;3;3;3;3;4;4;4;4;4;5'),
(7, 'Chính xác: +#', 0, 0, '5;6;8;10;13;16;20;25;31;38;47;58;72;89;110;136;168;208;257;317'),
(8, 'Tăng tấn công hệ Hỏa: +#', 0, 0, '5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
(9, 'Tăng tấn công hệ Thủy: +#', 0, 0, '5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
(10, 'Tăng tấn công hệ Thổ: +#', 0, 0, '5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
(11, 'Tăng tấn công hệ Kim: +#', 0, 0, '5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
(12, 'Tăng tấn công hệ Mộc: +#', 0, 0, '5;6;8;10;12;15;18;22;27;33;40;49;60;74;90;111;136;167;205;252'),
(13, '(+4) Tốc độ tấn công: +#%', 3, 4, '0;0;0;0;3;3;4;4;5;5;6;6;7;7;8;8;9;9;10;10'),
(14, '(+4) Bỏ qua né tránh: +#', 3, 4, '0;0;0;0;10;12;14;17;20;24;28;33;39;46;54;63;74;87;102;120'),
(15, '(+8) Chí mạng: +#', 4, 8, '0;0;0;0;0;0;0;0;15;18;22;27;32;38;45;53;63;74;87;103'),
(16, '(+8) Xuyên giáp: +#%', 4, 8, '0;0;0;0;0;0;0;0;5;5;6;6;7;7;8;8;9;9;10;10'),
(17, '(+12) Phát huy tấn công cơ bản: +#%', 5, 12, '0;0;0;0;0;0;0;0;0;0;0;0;5;6;7;8;9;10;12;14'),
(18, '(+16) Gây bỏng khi chí mạng: +#%', 6, 16, '0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;5;5;6;6'),
(20, 'Phòng thủ: +#', 2, 0, '5;7;9;11;14;18;22;28;34;42;52;64;79;97;119;147;181;223;275;338'),
(21, 'HP tối đa: +#', 2, 0, '20;25;32;40;50;63;79;99;124;155;194;242;303;379;473;592;740;925;1156;1445'),
(22, 'MP tối đa: +#', 2, 0, '10;13;16;20;25;32;40;50;63;79;99;124;155;194;242;303;379;473;591;739'),
(23, 'Né tránh: +#', 2, 0, '3;4;5;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163;200'),
(24, 'Kháng Hỏa: +#', 2, 0, '3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(25, 'Kháng Thủy: +#', 2, 0, '3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(26, 'Kháng Thổ: +#', 2, 0, '3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(27, 'Kháng Kim: +#', 2, 0, '3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(28, 'Kháng Mộc: +#', 2, 0, '3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133;163'),
(29, 'Giảm trừ sát thương: +#', 2, 0, '2;3;4;5;6;7;9;11;14;17;21;26;32;39;48;59;72;88;108;133'),
(30, 'Phản đòn: +#%', 2, 0, '1;1;1;1;1;1;2;2;2;2;2;2;3;3;3;3;3;3;4;4'),
(31, '(+4) Hồi phục HP mỗi 0.5s: +#', 3, 4, '0;0;0;0;2;2;3;3;4;4;5;5;6;6;7;7;8;8;9;9'),
(32, '(+4) Hồi phục MP mỗi 0.5s: +#', 3, 4, '0;0;0;0;1;1;2;2;3;3;4;4;5;5;6;6;7;7;8;8'),
(33, '(+4) Tốc độ di chuyển: +#', 3, 4, '0;0;0;0;5;5;6;6;7;7;8;8;9;9;10;10;11;11;12;12'),
(34, '(+8) HP tối đa: +#%', 4, 8, '0;0;0;0;0;0;0;0;3;3;4;4;5;5;6;6;7;7;8;8'),
(35, '(+8) Chí mạng: +#', 4, 8, '0;0;0;0;0;0;0;0;10;12;15;18;21;25;30;36;43;51;61;73'),
(36, '(+12) Phòng thủ: +#%', 5, 12, '0;0;0;0;0;0;0;0;0;0;0;0;4;5;6;7;8;9;10;11'),
(37, '(+12) MP tối đa: +#%', 5, 12, '0;0;0;0;0;0;0;0;0;0;0;0;3;4;5;6;7;8;9;10'),
(38, '(+16) Kháng tất cả: +#', 6, 16, '0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;20;24;29;35'),
(40, 'HP tối đa: +#', 2, 0, '15;19;24;30;37;47;58;73;91;114;143;179;224;280;350;438;548;685;856;1070'),
(41, 'MP tối đa: +#', 2, 0, '10;12;15;19;24;30;37;46;58;72;90;113;141;176;220;275;344;430;537;671'),
(42, 'Tấn công: +#', 2, 0, '7;9;11;14;17;21;27;33;42;52;65;81;101;127;158;198;248;310;387;484'),
(43, 'Phòng thủ: +#', 2, 0, '4;5;6;8;10;12;15;19;24;30;37;46;58;72;90;113;141;176;220;275'),
(44, 'Chí mạng: +#', 2, 0, '2;3;4;5;6;7;9;11;14;17;21;26;32;40;50;62;78;97;121;151'),
(45, 'Né tránh: +#', 2, 0, '2;3;4;5;6;7;9;11;14;17;21;26;32;40;50;62;78;97;121;151'),
(46, '(+4) Kháng tất cả: +#', 3, 4, '0;0;0;0;5;6;7;8;10;12;14;17;20;24;29;35;42;50;60;72'),
(47, '(+8) HP tối đa: +#%', 4, 8, '0;0;0;0;0;0;0;0;2;3;3;4;4;5;5;6;6;7;7;8'),
(48, '(+8) Tấn công: +#%', 4, 8, '0;0;0;0;0;0;0;0;2;2;3;3;4;4;5;5;6;6;7;7');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `player_action_log`
--

CREATE TABLE `player_action_log` (
  `id` bigint(20) NOT NULL,
  `player_id` int(11) NOT NULL,
  `action_type` varchar(50) NOT NULL COMMENT 'login|level_up|equip_upgrade|gene_upgrade|fusion|item_consume|skill_upgrade',
  `detail_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Audit trail for fraud detection and game economy monitoring';

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `player_data`
--

CREATE TABLE `player_data` (
  `player_id` int(11) NOT NULL COMMENT 'FK → users.user_id',
  `character_name` varchar(50) NOT NULL DEFAULT '',
  `gender` enum('Male','Female') NOT NULL DEFAULT 'Male',
  `info_char` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `equipment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `inventory` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `skills` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `potential_stats` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp(),
  `active_buffs` longtext NOT NULL DEFAULT '[]' COMMENT 'JSON array các buff đang active'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `player_data`
--

INSERT INTO `player_data` (`player_id`, `character_name`, `gender`, `info_char`, `equipment`, `inventory`, `skills`, `potential_stats`, `updated_at`, `active_buffs`) VALUES
(1, 'Hero1', 'Male', '{\"level\":50,\"experience\":1000000,\"gold\":100000,\"silver\":992000,\"skill_points\":134,\"potential_points\":149,\"element_type\":\"Fire\",\"gene_tier\":4,\"gene_exp\":727000,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hp\":9050,\"max_hp\":9050,\"mp\":3720,\"max_mp\":3720,\"attack\":935,\"defense\":366,\"map_id\":0,\"position_x\":-0.5658877,\"position_y\":-3.350597}', '[]', '[]', '[{\"skill_id\":1,\"current_level\":1},{\"skill_id\":5,\"current_level\":2},{\"skill_id\":2,\"current_level\":0}]', '{\"attack\":5,\"hp\":0,\"mp\":0,\"defense\":0,\"gene\":0}', '2026-03-30 07:48:55', '[]'),
(2, 'Hero2', 'Female', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":3000,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Water\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"map_id\":0,\"position_x\":-2.300001,\"position_y\":-3.350597}', '[]', '[]', '[{\"skillCode\":\"WATER_SHIELD\",\"currentLevel\":1,\"isEquipped\":true,\"slotIndex\":0}]', '{\"attack\":0,\"hp\":0,\"mp\":0,\"defense\":0,\"gene\":0}', '2026-03-30 07:48:54', '[]'),
(4, '', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Fire\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"map_id\":0,\"position_x\":0,\"position_y\":0}', '{}', '[]', '[]', '{}', '2026-03-10 17:03:44', '[]'),
(5, '', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Fire\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"map_id\":0,\"position_x\":0,\"position_y\":0}', '{}', '[]', '[]', '{}', '2026-03-10 17:05:46', '[]'),
(11, 'thuy', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Water\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"map_id\":0,\"position_x\":29.57438,\"position_y\":-1.942338}', '{}', '[]', '[]', '{}', '2026-03-28 21:31:28', '[]'),
(12, 'hoa', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Fire\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"map_id\":0,\"position_x\":39.30009,\"position_y\":7.455923}', '{}', '[]', '[]', '{}', '2026-03-29 22:04:57', '[]'),
(13, 'Tho', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Earth\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"map_id\":0,\"position_x\":7.746673,\"position_y\":-1.157384}', '{}', '[]', '[]', '{}', '2026-03-27 01:16:44', '[]'),
(16, 'Phong', 'Female', '{\"level\":1,\"experience\":0,\"gold\":1000000,\"silver\":222500,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Wind\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":70,\"max_hp\":100,\"mp\":30,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"map_id\":0,\"position_x\":1.508539,\"position_y\":3.85973}', '{}', '[{\"slotIndex\":0,\"itemTemplateId\":1,\"quantity\":17,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":1,\"itemTemplateId\":11,\"strOptions\":\"\",\"quantity\":7,\"upgradeLevel\":0},{\"slotIndex\":2,\"itemTemplateId\":14,\"strOptions\":\"\",\"quantity\":2,\"upgradeLevel\":0}]', '[]', '{}', '2026-04-04 06:52:20', '[{\"effectType\":\"MpRestoreOverTime\",\"value\":150,\"iconId\":538,\"name\":\"H\\u1ED3i linh\",\"detail\":\"\\u002B150 MP/s trong 30 gi\\u00E2y\",\"expireAt\":\"2026-04-04T06:51:40.7961329Z\"}]'),
(17, 'kim', 'Male', '{\"level\":1,\"experience\":0,\"gold\":10000,\"silver\":699849000,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Metal\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"map_id\":0,\"position_x\":-2.353325,\"position_y\":1.327723}', '{}', '[{\"slotIndex\":0,\"itemTemplateId\":161,\"itemCode\":\"\\u0110an C\\u01B0\\u1EDDng Sinh Nh\\u1ECF\",\"iconId\":\"388\",\"quantity\":3,\"isEquipped\":false,\"upgradeLevel\":0},{\"slotIndex\":1,\"itemTemplateId\":11,\"itemCode\":\"B\\u00ECnh HP Nh\\u1ECF\",\"iconId\":\"409\",\"quantity\":2,\"isEquipped\":false,\"upgradeLevel\":0},{\"slotIndex\":2,\"itemTemplateId\":122,\"itemCode\":\"Nh\\u00E2n S\\u00E2m Th\\u1EA7n Th\\u00E1nh\",\"iconId\":\"435\",\"quantity\":2,\"isEquipped\":false,\"upgradeLevel\":0},{\"slotIndex\":3,\"itemTemplateId\":121,\"itemCode\":\"Nh\\u00E2n S\\u00E2m T\\u00E2m Linh\",\"iconId\":\"434\",\"quantity\":3,\"isEquipped\":false,\"upgradeLevel\":0},{\"slotIndex\":4,\"itemTemplateId\":14,\"itemCode\":\"B\\u00ECnh MP Nh\\u1ECF\",\"iconId\":\"236\",\"quantity\":1,\"isEquipped\":false,\"upgradeLevel\":0}]', '[]', '{}', '2026-04-04 03:08:00', '[{\"effectType\":\"GeneExpBuff\",\"value\":20,\"iconId\":562,\"name\":\"EXP Gene \\u002B20%\",\"detail\":\"\\u002B20% EXP Gene (30 ph\\u00FAt)\",\"expireAt\":\"2026-04-04T03:33:52.8352166Z\"}]');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `player_equipment`
--

CREATE TABLE `player_equipment` (
  `id` int(11) NOT NULL,
  `player_id` int(11) NOT NULL,
  `slot` varchar(20) NOT NULL,
  `item_template_id` int(10) UNSIGNED NOT NULL,
  `upgrade_level` int(11) NOT NULL DEFAULT 0,
  `str_options` varchar(500) NOT NULL DEFAULT '',
  `equipped_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `player_inventory`
--

CREATE TABLE `player_inventory` (
  `id` int(11) NOT NULL,
  `player_id` int(11) NOT NULL,
  `item_template_id` int(10) UNSIGNED NOT NULL,
  `quantity` int(11) NOT NULL DEFAULT 1,
  `slot_index` int(11) NOT NULL DEFAULT 0,
  `upgrade_level` int(11) NOT NULL DEFAULT 0,
  `str_options` varchar(500) NOT NULL DEFAULT '',
  `is_locked` tinyint(1) NOT NULL DEFAULT 0,
  `acquired_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `player_skill_record`
--

CREATE TABLE `player_skill_record` (
  `id` int(11) NOT NULL,
  `player_id` int(11) NOT NULL,
  `skill_id` int(11) NOT NULL,
  `skill_level` int(11) NOT NULL DEFAULT 1,
  `is_equipped` tinyint(1) NOT NULL DEFAULT 0,
  `hotbar_slot` int(11) NOT NULL DEFAULT -1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `skill_template`
--

CREATE TABLE `skill_template` (
  `skill_id` int(11) NOT NULL,
  `skill_code` varchar(50) NOT NULL,
  `skill_name` varchar(100) NOT NULL,
  `description` text DEFAULT NULL,
  `element_type` varchar(20) DEFAULT NULL,
  `max_level` int(11) NOT NULL DEFAULT 5,
  `level_to_unlock` int(11) NOT NULL DEFAULT 1,
  `levels_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL,
  `icon_id` varchar(100) DEFAULT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `gene_tier_required` int(11) NOT NULL DEFAULT 0,
  `hybrid_id` int(11) DEFAULT NULL COMMENT 'FK → gene_hybrid_config.hybrid_id, chỉ set với hybrid skill'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `skill_template`
--

INSERT INTO `skill_template` (`skill_id`, `skill_code`, `skill_name`, `description`, `element_type`, `max_level`, `level_to_unlock`, `levels_json`, `icon_id`, `created_at`, `gene_tier_required`, `hybrid_id`) VALUES
(5, 'DASH', 'Lướt Nhanh', 'Lướt về phía trước tránh đòn', NULL, 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":1,\"mp_cost\":8,\"cooldown_sec\":4.0,\"desc\":\"Lướt 1 đơn vị\"},\r\n  {\"level_req\":3,\"sp_cost\":1,\"effect_value\":2,\"mp_cost\":10,\"cooldown_sec\":3.5,\"desc\":\"Lướt 2 đơn vị\"},\r\n  {\"level_req\":6,\"sp_cost\":1,\"effect_value\":3,\"mp_cost\":12,\"cooldown_sec\":3.0,\"desc\":\"Lướt 3 đơn vị\"},\r\n  {\"level_req\":10,\"sp_cost\":2,\"effect_value\":4,\"mp_cost\":14,\"cooldown_sec\":2.5,\"desc\":\"Lướt 4 đơn vị\"},\r\n  {\"level_req\":15,\"sp_cost\":2,\"effect_value\":5,\"mp_cost\":16,\"cooldown_sec\":2.0,\"desc\":\"Lướt 5 đơn vị\"}]', 'icon_skill_5', '2026-03-08 13:29:15', 0, NULL),
(8, 'WOOD_VINE', 'Dây Leo Cây', 'Triệu hồi dây leo trói chặt kẻ địch', 'Wood', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":1,\"mp_cost\":14,\"cooldown_sec\":6.0,\"desc\":\"Trói 1s\"},\r\n  {\"level_req\":3,\"sp_cost\":1,\"effect_value\":2,\"mp_cost\":18,\"cooldown_sec\":5.5,\"desc\":\"Trói 2s\"},\r\n  {\"level_req\":5,\"sp_cost\":2,\"effect_value\":3,\"mp_cost\":22,\"cooldown_sec\":5.0,\"desc\":\"Trói 3s\"},\r\n  {\"level_req\":8,\"sp_cost\":2,\"effect_value\":4,\"mp_cost\":26,\"cooldown_sec\":4.5,\"desc\":\"Trói 4s\"},\r\n  {\"level_req\":12,\"sp_cost\":3,\"effect_value\":5,\"mp_cost\":30,\"cooldown_sec\":4.0,\"desc\":\"Trói 5s\"}]', 'icon_skill_8', '2026-03-08 13:29:15', 0, NULL),
(9, 'WIND_STRIKE', 'Chưởng Phong', 'Tung đòn cận chiến mang khí phong, gây sát thương cho kẻ địch xung quanh.', 'Wind', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":18,\"mp_cost\":8,\"cooldown_sec\":3.0,\"desc\":\"Gây 18 ST\"},\n   {\"level_req\":3,\"sp_cost\":1,\"effect_value\":32,\"mp_cost\":12,\"cooldown_sec\":2.8,\"desc\":\"Gây 32 ST\"},\n   {\"level_req\":5,\"sp_cost\":1,\"effect_value\":50,\"mp_cost\":16,\"cooldown_sec\":2.5,\"desc\":\"Gây 50 ST\"},\n   {\"level_req\":8,\"sp_cost\":2,\"effect_value\":75,\"mp_cost\":20,\"cooldown_sec\":2.2,\"desc\":\"Gây 75 ST\"},\n   {\"level_req\":12,\"sp_cost\":2,\"effect_value\":105,\"mp_cost\":25,\"cooldown_sec\":2.0,\"desc\":\"Gây 105 ST\"}]', 'icon_wind_1', '2026-03-15 22:58:39', 0, NULL),
(10, 'WIND_BLADE', 'Phong Nhận', 'Vung tay tạo lưỡi gió sắc bén quanh thân, gây sát thương cận chiến diện rộng hơn Chưởng Phong.', 'Wind', 5, 3, '[{\"level_req\":3,\"sp_cost\":1,\"effect_value\":35,\"mp_cost\":12,\"cooldown_sec\":4.0,\"desc\":\"Gây 35 ST diện rộng\"},\n                     {\"level_req\":5,\"sp_cost\":1,\"effect_value\":55,\"mp_cost\":16,\"cooldown_sec\":3.5,\"desc\":\"Gây 55 ST diện rộng\"},\n                     {\"level_req\":7,\"sp_cost\":2,\"effect_value\":80,\"mp_cost\":20,\"cooldown_sec\":3.0,\"desc\":\"Gây 80 ST diện rộng\"},\n                     {\"level_req\":10,\"sp_cost\":2,\"effect_value\":115,\"mp_cost\":25,\"cooldown_sec\":2.8,\"desc\":\"Gây 115 ST diện rộng\"},\n                     {\"level_req\":15,\"sp_cost\":3,\"effect_value\":160,\"mp_cost\":30,\"cooldown_sec\":2.5,\"desc\":\"Gây 160 ST diện rộng\"}]', 'icon_wind_2', '2026-03-15 22:58:39', 0, NULL),
(11, 'WIND_STEP', 'Phong Thoái Bộ', 'Ẩn thân vào gió, phát vầng sáng phong khí tại chỗ rồi lướt tới trước bằng tốc độ phong.', 'Wind', 5, 5, '[{\"level_req\":5,\"sp_cost\":1,\"effect_value\":3,\"mp_cost\":15,\"cooldown_sec\":8.0,\"desc\":\"Dịch chuyển 3 đơn vị\"},\r\n   {\"level_req\":7,\"sp_cost\":1,\"effect_value\":4,\"mp_cost\":18,\"cooldown_sec\":7.0,\"desc\":\"Dịch chuyển 4 đơn vị\"},\r\n   {\"level_req\":9,\"sp_cost\":2,\"effect_value\":5,\"mp_cost\":22,\"cooldown_sec\":6.5,\"desc\":\"Dịch chuyển 5 đơn vị\"},\r\n   {\"level_req\":12,\"sp_cost\":2,\"effect_value\":6,\"mp_cost\":26,\"cooldown_sec\":6.0,\"desc\":\"Dịch chuyển 6 đơn vị\"},\r\n   {\"level_req\":16,\"sp_cost\":3,\"effect_value\":8,\"mp_cost\":30,\"cooldown_sec\":5.0,\"desc\":\"Dịch chuyển 8 đơn vị\"}]', 'icon_wind_3', '2026-03-15 22:58:39', 0, NULL),
(12, 'FIRE_BURST', 'Hỏa Cầu', 'Bắn một cầu lửa lớn chậm hơn nhưng gây sát thương cao hơn (Skill 2 hệ Hỏa)', 'Fire', 5, 2, '[{\"level_req\":2,\"sp_cost\":1,\"effect_value\":35,\"mp_cost\":15,\"cooldown_sec\":5,\"desc\":\"Gây 35 ST\"},{\"level_req\":4,\"sp_cost\":1,\"effect_value\":60,\"mp_cost\":18,\"cooldown_sec\":5,\"desc\":\"Gây 60 ST\"},{\"level_req\":7,\"sp_cost\":2,\"effect_value\":90,\"mp_cost\":22,\"cooldown_sec\":4.5,\"desc\":\"Gây 90 ST\"},{\"level_req\":11,\"sp_cost\":2,\"effect_value\":130,\"mp_cost\":26,\"cooldown_sec\":4,\"desc\":\"Gây 130 ST\"},{\"level_req\":16,\"sp_cost\":3,\"effect_value\":180,\"mp_cost\":30,\"cooldown_sec\":4,\"desc\":\"Gây 180 ST\"}]', 'icon_fire_burst', '2026-03-15 22:59:12', 0, NULL),
(13, 'WATER_PILLAR', 'Thánh Mộc Hạ', 'Triệu hồi cây thánh từ trên trời rơi xuống, gây sát thương diện rộng khu vực đáp (Skill 2 hệ Thủy)', 'Water', 5, 3, '[{\"level_req\":3,\"sp_cost\":1,\"effect_value\":40,\"mp_cost\":16,\"cooldown_sec\":6,\"desc\":\"Gây 40 ST\"},{\"level_req\":5,\"sp_cost\":1,\"effect_value\":70,\"mp_cost\":20,\"cooldown_sec\":6,\"desc\":\"Gây 70 ST\"},{\"level_req\":8,\"sp_cost\":2,\"effect_value\":105,\"mp_cost\":24,\"cooldown_sec\":5.5,\"desc\":\"Gây 105 ST\"},{\"level_req\":12,\"sp_cost\":2,\"effect_value\":150,\"mp_cost\":28,\"cooldown_sec\":5,\"desc\":\"Gây 150 ST\"},{\"level_req\":18,\"sp_cost\":3,\"effect_value\":200,\"mp_cost\":32,\"cooldown_sec\":4.5,\"desc\":\"Gây 200 ST\"}]', 'icon_water_pillar', '2026-03-16 17:34:17', 0, NULL),
(14, 'EARTH_SHIELD', 'Thủy Giáp Hộ Thể', 'Bao phủ bản thân và đồng đội xung quanh lớp giáp nước, hấp thụ sát thương trong thời gian ngắn (Skill 3 hệ Thủy)', 'Water', 5, 5, '[{\"level_req\":5,\"sp_cost\":1,\"effect_value\":15,\"mp_cost\":20,\"cooldown_sec\":12,\"desc\":\"Buff 15 giáp 5 giây\"},{\"level_req\":8,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":25,\"cooldown_sec\":11,\"desc\":\"Buff 20 giáp 5 giây\"},{\"level_req\":11,\"sp_cost\":2,\"effect_value\":28,\"mp_cost\":28,\"cooldown_sec\":10,\"desc\":\"Buff 28 giáp 6 giây\"},{\"level_req\":15,\"sp_cost\":2,\"effect_value\":38,\"mp_cost\":30,\"cooldown_sec\":9,\"desc\":\"Buff 38 giáp 6 giây\"},{\"level_req\":20,\"sp_cost\":3,\"effect_value\":50,\"mp_cost\":35,\"cooldown_sec\":8,\"desc\":\"Buff 50 giáp 7 giây\"}]', 'icon_water_armor', '2026-03-15 22:59:12', 0, NULL),
(15, 'FIRE_BOLT', 'Hỏa Đạn', 'Bắn một viên đạn lửa theo hướng player, gây sát thương khi chạm enemy (Skill 1 hệ Hỏa)', 'Fire', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":10,\"cooldown_sec\":3,\"desc\":\"Gây 20 ST\"},{\"level_req\":3,\"sp_cost\":1,\"effect_value\":35,\"mp_cost\":13,\"cooldown_sec\":3,\"desc\":\"Gây 35 ST\"},{\"level_req\":6,\"sp_cost\":1,\"effect_value\":55,\"mp_cost\":16,\"cooldown_sec\":2.5,\"desc\":\"Gây 55 ST\"},{\"level_req\":9,\"sp_cost\":2,\"effect_value\":80,\"mp_cost\":20,\"cooldown_sec\":2,\"desc\":\"Gây 80 ST\"},{\"level_req\":14,\"sp_cost\":2,\"effect_value\":110,\"mp_cost\":24,\"cooldown_sec\":2,\"desc\":\"Gây 110 ST\"}]', 'icon_fire_bolt', '2026-03-16 21:02:18', 0, NULL),
(16, 'WATER_BOLT', 'Thủy Đạn', 'Bắn một viên đạn nước theo hướng player, gây sát thương khi chạm enemy (Skill 1 hệ Thủy)', 'Water', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":10,\"cooldown_sec\":3,\"desc\":\"Gây 20 ST\"},{\"level_req\":3,\"sp_cost\":1,\"effect_value\":35,\"mp_cost\":13,\"cooldown_sec\":3,\"desc\":\"Gây 35 ST\"},{\"level_req\":6,\"sp_cost\":1,\"effect_value\":55,\"mp_cost\":16,\"cooldown_sec\":2.5,\"desc\":\"Gây 55 ST\"},{\"level_req\":9,\"sp_cost\":2,\"effect_value\":80,\"mp_cost\":20,\"cooldown_sec\":2,\"desc\":\"Gây 80 ST\"},{\"level_req\":14,\"sp_cost\":2,\"effect_value\":110,\"mp_cost\":24,\"cooldown_sec\":2,\"desc\":\"Gây 110 ST\"}]', 'icon_water_bolt', '2026-03-25 10:00:00', 0, NULL),
(17, 'FIRE_RAIN', 'Thiên Hỏa', 'Triệu hồi mưa lửa từ trên trời rơi xuống vùng trước mặt, gây sát thương diện rộng (Skill 3 hệ Hỏa)', 'Fire', 5, 4, '[{\"level_req\":4,\"sp_cost\":1,\"effect_value\":25,\"mp_cost\":20,\"cooldown_sec\":8,\"desc\":\"5 cầu lửa 25 ST mỗi cầu\"},{\"level_req\":6,\"sp_cost\":1,\"effect_value\":40,\"mp_cost\":24,\"cooldown_sec\":8,\"desc\":\"5 cầu 40 ST\"},{\"level_req\":9,\"sp_cost\":2,\"effect_value\":60,\"mp_cost\":28,\"cooldown_sec\":7,\"desc\":\"6 cầu 60 ST\"},{\"level_req\":13,\"sp_cost\":2,\"effect_value\":85,\"mp_cost\":32,\"cooldown_sec\":6.5,\"desc\":\"7 cầu 85 ST\"},{\"level_req\":18,\"sp_cost\":3,\"effect_value\":115,\"mp_cost\":36,\"cooldown_sec\":6,\"desc\":\"8 cầu 115 ST\"}]', 'icon_fire_rain', '2026-03-16 21:00:58', 0, NULL),
(18, 'WOOD_ARROW', 'Tên Gỗ', 'Bắn mũi tên làm từ gỗ cứng theo hướng player nhìn.', 'Wood', 5, 3, '[{\"level_req\":3,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":10,\"cooldown_sec\":3.5,\"desc\":\"Gây 20 ST\"},\r\n   {\"level_req\":5,\"sp_cost\":1,\"effect_value\":35,\"mp_cost\":14,\"cooldown_sec\":3.2,\"desc\":\"Gây 35 ST\"},\r\n   {\"level_req\":7,\"sp_cost\":1,\"effect_value\":52,\"mp_cost\":18,\"cooldown_sec\":3.0,\"desc\":\"Gây 52 ST\"},\r\n   {\"level_req\":10,\"sp_cost\":2,\"effect_value\":75,\"mp_cost\":23,\"cooldown_sec\":2.8,\"desc\":\"Gây 75 ST\"},\r\n   {\"level_req\":15,\"sp_cost\":2,\"effect_value\":105,\"mp_cost\":28,\"cooldown_sec\":2.5,\"desc\":\"Gây 105 ST\"}]', 'icon_wood_2', '2026-03-15 22:59:12', 0, NULL),
(19, 'WOOD_HEAL', 'Thảo Dược Hồi', 'Hấp thụ năng lượng từ thiên nhiên để hồi máu bản thân.', 'Wood', 5, 5, '[{\"level_req\":5,\"sp_cost\":1,\"effect_value\":50,\"mp_cost\":22,\"cooldown_sec\":12.0,\"desc\":\"Hồi 50 HP\"},\r\n   {\"level_req\":7,\"sp_cost\":1,\"effect_value\":85,\"mp_cost\":28,\"cooldown_sec\":11.0,\"desc\":\"Hồi 85 HP\"},\r\n   {\"level_req\":10,\"sp_cost\":2,\"effect_value\":130,\"mp_cost\":34,\"cooldown_sec\":10.0,\"desc\":\"Hồi 130 HP\"},\r\n   {\"level_req\":14,\"sp_cost\":2,\"effect_value\":185,\"mp_cost\":40,\"cooldown_sec\":9.0,\"desc\":\"Hồi 185 HP\"},\r\n   {\"level_req\":18,\"sp_cost\":3,\"effect_value\":250,\"mp_cost\":48,\"cooldown_sec\":8.0,\"desc\":\"Hồi 250 HP\"}]', 'icon_wood_3', '2026-03-15 22:59:12', 0, NULL),
(20, 'METAL_STRIKE', 'Kim Phong', 'Đòn chém cận chiến bằng lưỡi kim loại sắc bén, gây sát thương cho kẻ địch trước mặt.', 'Metal', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":8,\"cooldown_sec\":3.0,\"desc\":\"Gây 20 ST cận chiến\"},\r\n   {\"level_req\":3,\"sp_cost\":1,\"effect_value\":38,\"mp_cost\":12,\"cooldown_sec\":3.0,\"desc\":\"Gây 38 ST\"},\r\n   {\"level_req\":5,\"sp_cost\":2,\"effect_value\":60,\"mp_cost\":16,\"cooldown_sec\":2.5,\"desc\":\"Gây 60 ST\"},\r\n   {\"level_req\":8,\"sp_cost\":2,\"effect_value\":88,\"mp_cost\":20,\"cooldown_sec\":2.5,\"desc\":\"Gây 88 ST\"},\r\n   {\"level_req\":12,\"sp_cost\":3,\"effect_value\":120,\"mp_cost\":24,\"cooldown_sec\":2.0,\"desc\":\"Gây 120 ST\"}]', 'icon_metal_strike', '2026-03-16 03:26:20', 0, NULL),
(21, 'METAL_BLADE', 'Kim Nhẫn', 'Tung lưỡi hình tròn quét vùng rộng xung quanh, gây sát thương cho toàn bộ kẻ địch gần đó.', 'Metal', 5, 3, '[{\"level_req\":3,\"sp_cost\":1,\"effect_value\":30,\"mp_cost\":14,\"cooldown_sec\":4.0,\"desc\":\"Gây 30 ST diện rộng\"},\r\n   {\"level_req\":5,\"sp_cost\":1,\"effect_value\":55,\"mp_cost\":18,\"cooldown_sec\":4.0,\"desc\":\"Gây 55 ST\"},\r\n   {\"level_req\":8,\"sp_cost\":2,\"effect_value\":85,\"mp_cost\":22,\"cooldown_sec\":3.5,\"desc\":\"Gây 85 ST\"},\r\n   {\"level_req\":12,\"sp_cost\":2,\"effect_value\":120,\"mp_cost\":26,\"cooldown_sec\":3.5,\"desc\":\"Gây 120 ST\"},\r\n   {\"level_req\":18,\"sp_cost\":3,\"effect_value\":165,\"mp_cost\":30,\"cooldown_sec\":3.0,\"desc\":\"Gây 165 ST\"}]', 'icon_metal_blade', '2026-03-16 03:26:20', 0, NULL),
(22, 'METAL_SHIELD', 'Kim Cương Khiên', 'Tạo khiên kim cương bất tử, miễn nhiễm hoàn toàn mọi sát thương và đòn tấn công trong thời gian duy trì. Mọi projectile chạm vào sẽ bị phá hủy ngay lập tức.', 'Metal', 5, 5, '[{\"level_req\":5,\"sp_cost\":1,\"effect_value\":3,\"mp_cost\":20,\"cooldown_sec\":12.0,\"desc\":\"Bất tử 3 giây\"},\r\n   {\"level_req\":8,\"sp_cost\":1,\"effect_value\":4,\"mp_cost\":25,\"cooldown_sec\":11.0,\"desc\":\"Bất tử 4 giây\"},\r\n   {\"level_req\":11,\"sp_cost\":2,\"effect_value\":5,\"mp_cost\":28,\"cooldown_sec\":10.0,\"desc\":\"Bất tử 5 giây\"},\r\n   {\"level_req\":15,\"sp_cost\":2,\"effect_value\":6,\"mp_cost\":30,\"cooldown_sec\":9.0,\"desc\":\"Bất tử 6 giây\"},\r\n   {\"level_req\":20,\"sp_cost\":3,\"effect_value\":8,\"mp_cost\":35,\"cooldown_sec\":8.0,\"desc\":\"Bất tử 8 giây\"}]', 'icon_metal_shield', '2026-03-16 03:26:20', 0, NULL),
(23, 'EARTH_AURA', 'Địa Uy Khí', 'Phát hào quang đất, tăng sát thương tấn công cho bản thân và đồng đội trong bán kính (Skill 1 hệ Thổ)', 'Earth', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":15,\"mp_cost\":15,\"cooldown_sec\":10,\"desc\":\"Buff +15% tấn công 6 giây\"},\r\n{\"level_req\":3,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":18,\"cooldown_sec\":10,\"desc\":\"Buff +20% tấn công 7 giây\"},\r\n{\"level_req\":7,\"sp_cost\":2,\"effect_value\":28,\"mp_cost\":22,\"cooldown_sec\":9,\"desc\":\"Buff +28% tấn công 8 giây\"},\r\n{\"level_req\":12,\"sp_cost\":2,\"effect_value\":38,\"mp_cost\":26,\"cooldown_sec\":8,\"desc\":\"Buff +38% tấn công 9 giây\"},\r\n{\"level_req\":17,\"sp_cost\":3,\"effect_value\":50,\"mp_cost\":30,\"cooldown_sec\":7,\"desc\":\"Buff +50% tấn công 10 giây\"}]', 'icon_earth_aura', '2026-03-16 23:03:50', 0, NULL),
(24, 'EARTH_BOOMERANG', 'Địa Phong Đao', 'Phóng dao đất theo hướng trước, sau khi bay xong tự quay về tay player (Skill 2 hệ Thổ)', 'Earth', 5, 2, '[{\"level_req\":2,\"sp_cost\":1,\"effect_value\":30,\"mp_cost\":12,\"cooldown_sec\":5,\"desc\":\"Gây 30 ST đi về\"},\r\n{\"level_req\":4,\"sp_cost\":1,\"effect_value\":50,\"mp_cost\":16,\"cooldown_sec\":5,\"desc\":\"Gây 50 ST đi về\"},\r\n{\"level_req\":8,\"sp_cost\":2,\"effect_value\":75,\"mp_cost\":20,\"cooldown_sec\":4.5,\"desc\":\"Gây 75 ST đi về\"},\r\n{\"level_req\":12,\"sp_cost\":2,\"effect_value\":105,\"mp_cost\":24,\"cooldown_sec\":4,\"desc\":\"Gây 105 ST đi về\"},\r\n{\"level_req\":17,\"sp_cost\":3,\"effect_value\":140,\"mp_cost\":28,\"cooldown_sec\":4,\"desc\":\"Gây 140 ST đi về\"}]', 'icon_earth_boomerang', '2026-03-16 23:03:50', 0, NULL),
(25, 'EARTH_BLINK', 'Địa Độn Thuật', 'Dịch chuyển ngắn về phía trước rồi bắn ra đạn DoT, gây sát thương liên tục khi chạm (Skill 3 hệ Thổ)', 'Earth', 5, 4, '[{\"level_req\":4,\"sp_cost\":1,\"effect_value\":5,\"mp_cost\":20,\"cooldown_sec\":7,\"desc\":\"DoT 5 ST/tick × 5 tick\"},\r\n{\"level_req\":6,\"sp_cost\":1,\"effect_value\":8,\"mp_cost\":24,\"cooldown_sec\":7,\"desc\":\"DoT 8 ST/tick × 5 tick\"},\r\n{\"level_req\":10,\"sp_cost\":2,\"effect_value\":12,\"mp_cost\":28,\"cooldown_sec\":6,\"desc\":\"DoT 12 ST/tick × 6 tick\"},\r\n{\"level_req\":14,\"sp_cost\":2,\"effect_value\":17,\"mp_cost\":32,\"cooldown_sec\":6,\"desc\":\"DoT 17 ST/tick × 6 tick\"},\r\n{\"level_req\":19,\"sp_cost\":3,\"effect_value\":24,\"mp_cost\":36,\"cooldown_sec\":5,\"desc\":\"DoT 24 ST/tick × 7 tick\"}]', 'icon_earth_blink', '2026-03-16 23:03:50', 0, NULL),
(26, 'HYBRID_EARTH_FIRE_ERUPTION', 'Đại Địa Phún Thạch', 'Triệu hồi cột nham thạch từ dưới đất tại vị trí kẻ địch. Gây sát thương AoE kết hợp DoT.', NULL, 1, 1, '[{\"level_req\":1,\"sp_cost\":0,\"effect_value\":280,\"mp_cost\":50,\"cooldown_sec\":14.0}]', 'icon_hybrid_101', '2026-03-18 02:37:55', 0, 1),
(35, 'HYBRID_WATER_WOOD_VENOM', 'Băng Độc Vĩnh Cửu', 'Tạo hồ nước độc đóng băng dưới chân kẻ địch. Kẻ địch đứng trong hồ bị Slow + DoT + giảm ATK.', NULL, 1, 1, '[{\"level_req\":1,\"sp_cost\":0,\"effect_value\":250,\"mp_cost\":50,\"cooldown_sec\":16.0}]', 'icon_hybrid_110', '2026-03-18 02:37:55', 0, 10),
(38, 'HYBRID_METAL_WIND_GALE', 'Kim Phong Thiên Vũ', 'Phóng 12 mũi tên gió kim loại theo hình nan quạt, mỗi mũi tên xuyên qua tối đa 3 kẻ địch.', NULL, 1, 1, '[{\"level_req\":1,\"sp_cost\":0,\"effect_value\":295,\"mp_cost\":55,\"cooldown_sec\":13.0}]', 'icon_hybrid_113', '2026-03-18 02:37:55', 0, 13),
(41, 'NORMAL_ATTACK', 'Đánh Thường', 'Đòn tấn công cơ bản, không tiêu hao MP. Sát thương tăng khi nâng cấp.', NULL, 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":10,\"mp_cost\":0,\"cooldown_sec\":0.8,\"desc\":\"Gây 10 ST cơ bản\"},\r\n      {\"level_req\":5,\"sp_cost\":1,\"effect_value\":18,\"mp_cost\":0,\"cooldown_sec\":0.75,\"desc\":\"Gây 18 ST\"},\r\n      {\"level_req\":10,\"sp_cost\":1,\"effect_value\":30,\"mp_cost\":0,\"cooldown_sec\":0.7,\"desc\":\"Gây 30 ST\"},\r\n      {\"level_req\":20,\"sp_cost\":2,\"effect_value\":48,\"mp_cost\":0,\"cooldown_sec\":0.65,\"desc\":\"Gây 48 ST\"},\r\n      {\"level_req\":35,\"sp_cost\":2,\"effect_value\":72,\"mp_cost\":0,\"cooldown_sec\":0.6,\"desc\":\"Gây 72 ST\"}]', 'icon_normal_attack', '2026-03-26 03:16:09', 0, NULL);

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
(1, '1', 'admin@game.com', '1', '2026-01-01 00:00:00', '2026-03-22 23:48:45'),
(2, '2', 'p1@game.com', '1', '2026-01-01 00:00:00', '2026-03-09 09:11:48'),
(3, 'player22', 'p2@game.com', '1', '2026-01-01 00:00:00', NULL),
(4, 'thuytran', 'fl2k3xb@gmail.com', '123456', '2026-03-10 17:03:44', '2026-03-10 17:04:04'),
(5, '4', 'fl2k3xb1@gmail.com', '123456', '2026-03-10 17:05:46', NULL),
(6, '5', 'accsv4x1@gmail.com', '123456', '2026-03-10 17:09:13', '2026-03-11 01:50:14'),
(7, 'tranvanthuy', 'tranvan@gmail.com', '123456', '2026-03-11 06:18:27', '2026-03-11 06:18:35'),
(8, 'a1', 'fl2k3xba1@gmail.com', '123456', '2026-03-13 21:44:20', '2026-03-14 05:19:22'),
(11, 'thuy', 'thuy@gmail.com', '123456', '2026-03-16 11:24:42', '2026-03-28 21:29:21'),
(12, 'hoa', 'hoa@gmail.com', '123456', '2026-03-16 11:24:42', '2026-03-29 22:03:00'),
(13, 'tho', 'tho@gmail.com', '123456', '2026-03-16 11:24:42', '2026-03-27 01:16:33'),
(14, 'phong1', 'phong1@gmail.com', '123456', '2026-03-19 18:14:50', '2026-03-19 18:14:58'),
(15, 'tho1', 'tho1@gmail.com', '123456', '2026-03-22 22:04:49', '2026-03-22 22:06:52'),
(16, 'phong', 'phong@gmail.com', '$2a$12$IVR2P43G/o.2px.QU691Qe0gsZzuYZoq0QVaKJtRgHCQOk.JrcYbO', '2026-04-01 19:08:48', '2026-04-04 06:55:47'),
(17, 'kim', 'kim@gmail.com', '$2a$12$G1hEIuasIWxnsJsYm4g.YexoQdX2lV5rucvhH04mRlGJ3Vd4KDkTy', '2026-04-01 19:29:09', '2026-04-04 03:02:45');

--
-- Chỉ mục cho các bảng đã đổ
--

--
-- Chỉ mục cho bảng `boss_config`
--
ALTER TABLE `boss_config`
  ADD PRIMARY KEY (`boss_id`),
  ADD KEY `idx_boss_map` (`map_id`);

--
-- Chỉ mục cho bảng `dungeon_config`
--
ALTER TABLE `dungeon_config`
  ADD PRIMARY KEY (`dungeon_id`),
  ADD KEY `fk_dungeon_map` (`map_id`),
  ADD KEY `fk_dungeon_boss` (`boss_enemy_id`);

--
-- Chỉ mục cho bảng `dungeon_session`
--
ALTER TABLE `dungeon_session`
  ADD PRIMARY KEY (`session_id`),
  ADD KEY `idx_session_dungeon_status` (`dungeon_config_id`,`status`),
  ADD KEY `idx_ds_status` (`status`),
  ADD KEY `idx_ds_created` (`created_at`);

--
-- Chỉ mục cho bảng `enemy`
--
ALTER TABLE `enemy`
  ADD PRIMARY KEY (`enemy_id`),
  ADD KEY `idx_enemy_level` (`level`),
  ADD KEY `idx_enemy_element` (`element_type`);

--
-- Chỉ mục cho bảng `enemy_spawns`
--
ALTER TABLE `enemy_spawns`
  ADD PRIMARY KEY (`spawn_id`),
  ADD KEY `idx_es_map_id` (`map_id`),
  ADD KEY `idx_es_enemy_type` (`enemy_type_id`);

--
-- Chỉ mục cho bảng `equipment_upgrade_config`
--
ALTER TABLE `equipment_upgrade_config`
  ADD PRIMARY KEY (`upgrade_level`);

--
-- Chỉ mục cho bảng `exp_requirements`
--
ALTER TABLE `exp_requirements`
  ADD PRIMARY KEY (`level`);

--
-- Chỉ mục cho bảng `gene_hybrid_config`
--
ALTER TABLE `gene_hybrid_config`
  ADD PRIMARY KEY (`hybrid_id`),
  ADD UNIQUE KEY `uk_combo` (`element_a`,`element_b`);

--
-- Chỉ mục cho bảng `gene_hybrid_skill`
--
ALTER TABLE `gene_hybrid_skill`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_hybrid_skill` (`hybrid_id`,`skill_code`);

--
-- Chỉ mục cho bảng `gene_multi_config`
--
ALTER TABLE `gene_multi_config`
  ADD PRIMARY KEY (`tier_from`,`element_type`);

--
-- Chỉ mục cho bảng `gene_tier_stat_config`
--
ALTER TABLE `gene_tier_stat_config`
  ADD PRIMARY KEY (`element_type`,`tier_to`);

--
-- Chỉ mục cho bảng `gene_upgrade_config`
--
ALTER TABLE `gene_upgrade_config`
  ADD PRIMARY KEY (`tier_from`,`element_type`);

--
-- Chỉ mục cho bảng `item_effect_template`
--
ALTER TABLE `item_effect_template`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_item_template_id` (`item_template_id`);

--
-- Chỉ mục cho bảng `item_template`
--
ALTER TABLE `item_template`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_item_type` (`type`),
  ADD KEY `idx_item_level` (`levelNeed`);

--
-- Chỉ mục cho bảng `map_config`
--
ALTER TABLE `map_config`
  ADD PRIMARY KEY (`map_id`);

--
-- Chỉ mục cho bảng `map_enemy_drop`
--
ALTER TABLE `map_enemy_drop`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_map_enemy_item` (`map_id`,`enemy_id`,`item_id`),
  ADD KEY `idx_med_map_enemy` (`map_id`,`enemy_id`);

--
-- Chỉ mục cho bảng `map_portal`
--
ALTER TABLE `map_portal`
  ADD PRIMARY KEY (`portal_id`),
  ADD KEY `idx_source_map` (`source_map_id`),
  ADD KEY `idx_dest_map` (`dest_map_id`);

--
-- Chỉ mục cho bảng `map_spawn_config`
--
ALTER TABLE `map_spawn_config`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `map_id` (`map_id`);

--
-- Chỉ mục cho bảng `npc_config`
--
ALTER TABLE `npc_config`
  ADD PRIMARY KEY (`npc_id`);

--
-- Chỉ mục cho bảng `npc_dialogue`
--
ALTER TABLE `npc_dialogue`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_npc_dialogue_key` (`npc_id`,`dialogue_key`);

--
-- Chỉ mục cho bảng `npc_shop_item`
--
ALTER TABLE `npc_shop_item`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_npc_shop_npc` (`npc_id`);

--
-- Chỉ mục cho bảng `option_template`
--
ALTER TABLE `option_template`
  ADD PRIMARY KEY (`id`);

--
-- Chỉ mục cho bảng `player_action_log`
--
ALTER TABLE `player_action_log`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_pal_player_id` (`player_id`),
  ADD KEY `idx_pal_action_type` (`action_type`),
  ADD KEY `idx_pal_created_at` (`created_at`),
  ADD KEY `idx_pal_player_type_time` (`player_id`,`action_type`,`created_at`);

--
-- Chỉ mục cho bảng `player_data`
--
ALTER TABLE `player_data`
  ADD PRIMARY KEY (`player_id`),
  ADD KEY `idx_pd_character_name` (`character_name`),
  ADD KEY `idx_pd_updated_at` (`updated_at`);

--
-- Chỉ mục cho bảng `player_equipment`
--
ALTER TABLE `player_equipment`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_player_slot` (`player_id`,`slot`),
  ADD KEY `idx_pe_player_id` (`player_id`),
  ADD KEY `idx_pe_template_id` (`item_template_id`);

--
-- Chỉ mục cho bảng `player_inventory`
--
ALTER TABLE `player_inventory`
  ADD PRIMARY KEY (`id`),
  ADD KEY `idx_pi_player_id` (`player_id`),
  ADD KEY `idx_pi_template_id` (`item_template_id`);

--
-- Chỉ mục cho bảng `player_skill_record`
--
ALTER TABLE `player_skill_record`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_player_skill` (`player_id`,`skill_id`),
  ADD KEY `idx_psr_player_id` (`player_id`),
  ADD KEY `idx_psr_skill_id` (`skill_id`);

--
-- Chỉ mục cho bảng `skill_template`
--
ALTER TABLE `skill_template`
  ADD PRIMARY KEY (`skill_id`),
  ADD UNIQUE KEY `uq_skill_code` (`skill_code`);

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
-- AUTO_INCREMENT cho bảng `dungeon_config`
--
ALTER TABLE `dungeon_config`
  MODIFY `dungeon_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=5;

--
-- AUTO_INCREMENT cho bảng `dungeon_session`
--
ALTER TABLE `dungeon_session`
  MODIFY `session_id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT cho bảng `enemy`
--
ALTER TABLE `enemy`
  MODIFY `enemy_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=18;

--
-- AUTO_INCREMENT cho bảng `enemy_spawns`
--
ALTER TABLE `enemy_spawns`
  MODIFY `spawn_id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT cho bảng `gene_hybrid_config`
--
ALTER TABLE `gene_hybrid_config`
  MODIFY `hybrid_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=31;

--
-- AUTO_INCREMENT cho bảng `gene_hybrid_skill`
--
ALTER TABLE `gene_hybrid_skill`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=16;

--
-- AUTO_INCREMENT cho bảng `item_effect_template`
--
ALTER TABLE `item_effect_template`
  MODIFY `id` int(10) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=23;

--
-- AUTO_INCREMENT cho bảng `item_template`
--
ALTER TABLE `item_template`
  MODIFY `id` int(11) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=409;

--
-- AUTO_INCREMENT cho bảng `map_enemy_drop`
--
ALTER TABLE `map_enemy_drop`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT cho bảng `map_portal`
--
ALTER TABLE `map_portal`
  MODIFY `portal_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

--
-- AUTO_INCREMENT cho bảng `map_spawn_config`
--
ALTER TABLE `map_spawn_config`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT cho bảng `npc_config`
--
ALTER TABLE `npc_config`
  MODIFY `npc_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT cho bảng `npc_dialogue`
--
ALTER TABLE `npc_dialogue`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT cho bảng `npc_shop_item`
--
ALTER TABLE `npc_shop_item`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=13;

--
-- AUTO_INCREMENT cho bảng `player_action_log`
--
ALTER TABLE `player_action_log`
  MODIFY `id` bigint(20) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT cho bảng `player_equipment`
--
ALTER TABLE `player_equipment`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT cho bảng `player_inventory`
--
ALTER TABLE `player_inventory`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT cho bảng `player_skill_record`
--
ALTER TABLE `player_skill_record`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT;

--
-- AUTO_INCREMENT cho bảng `skill_template`
--
ALTER TABLE `skill_template`
  MODIFY `skill_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=42;

--
-- AUTO_INCREMENT cho bảng `users`
--
ALTER TABLE `users`
  MODIFY `user_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=18;

--
-- Các ràng buộc cho các bảng đã đổ
--

--
-- Các ràng buộc cho bảng `boss_config`
--
ALTER TABLE `boss_config`
  ADD CONSTRAINT `fk_bc_enemy` FOREIGN KEY (`boss_id`) REFERENCES `enemy` (`enemy_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_bc_map` FOREIGN KEY (`map_id`) REFERENCES `map_config` (`map_id`);

--
-- Các ràng buộc cho bảng `dungeon_config`
--
ALTER TABLE `dungeon_config`
  ADD CONSTRAINT `fk_dungeon_boss` FOREIGN KEY (`boss_enemy_id`) REFERENCES `enemy` (`enemy_id`) ON DELETE SET NULL,
  ADD CONSTRAINT `fk_dungeon_map` FOREIGN KEY (`map_id`) REFERENCES `map_config` (`map_id`);

--
-- Các ràng buộc cho bảng `dungeon_session`
--
ALTER TABLE `dungeon_session`
  ADD CONSTRAINT `fk_session_dungeon` FOREIGN KEY (`dungeon_config_id`) REFERENCES `dungeon_config` (`dungeon_id`) ON DELETE CASCADE;

--
-- Các ràng buộc cho bảng `enemy_spawns`
--
ALTER TABLE `enemy_spawns`
  ADD CONSTRAINT `fk_es_enemy` FOREIGN KEY (`enemy_type_id`) REFERENCES `enemy` (`enemy_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_es_map` FOREIGN KEY (`map_id`) REFERENCES `map_config` (`map_id`) ON DELETE CASCADE;

--
-- Các ràng buộc cho bảng `gene_hybrid_skill`
--
ALTER TABLE `gene_hybrid_skill`
  ADD CONSTRAINT `fk_ghs_hybrid` FOREIGN KEY (`hybrid_id`) REFERENCES `gene_hybrid_config` (`hybrid_id`);

--
-- Các ràng buộc cho bảng `map_spawn_config`
--
ALTER TABLE `map_spawn_config`
  ADD CONSTRAINT `fk_msc_map` FOREIGN KEY (`map_id`) REFERENCES `map_config` (`map_id`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Các ràng buộc cho bảng `npc_dialogue`
--
ALTER TABLE `npc_dialogue`
  ADD CONSTRAINT `fk_npc_dialogue_npc` FOREIGN KEY (`npc_id`) REFERENCES `npc_config` (`npc_id`) ON DELETE CASCADE;

--
-- Các ràng buộc cho bảng `npc_shop_item`
--
ALTER TABLE `npc_shop_item`
  ADD CONSTRAINT `fk_npc_shop_npc` FOREIGN KEY (`npc_id`) REFERENCES `npc_config` (`npc_id`) ON DELETE CASCADE;

--
-- Các ràng buộc cho bảng `player_action_log`
--
ALTER TABLE `player_action_log`
  ADD CONSTRAINT `fk_pal_player` FOREIGN KEY (`player_id`) REFERENCES `player_data` (`player_id`) ON DELETE CASCADE ON UPDATE CASCADE;

--
-- Các ràng buộc cho bảng `player_data`
--
ALTER TABLE `player_data`
  ADD CONSTRAINT `fk_pd_user` FOREIGN KEY (`player_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE;

--
-- Các ràng buộc cho bảng `player_equipment`
--
ALTER TABLE `player_equipment`
  ADD CONSTRAINT `fk_pe_item_template` FOREIGN KEY (`item_template_id`) REFERENCES `item_template` (`id`),
  ADD CONSTRAINT `fk_pe_player` FOREIGN KEY (`player_id`) REFERENCES `player_data` (`player_id`) ON DELETE CASCADE;

--
-- Các ràng buộc cho bảng `player_inventory`
--
ALTER TABLE `player_inventory`
  ADD CONSTRAINT `fk_pi_item_template` FOREIGN KEY (`item_template_id`) REFERENCES `item_template` (`id`),
  ADD CONSTRAINT `fk_pi_player` FOREIGN KEY (`player_id`) REFERENCES `player_data` (`player_id`) ON DELETE CASCADE;

--
-- Các ràng buộc cho bảng `player_skill_record`
--
ALTER TABLE `player_skill_record`
  ADD CONSTRAINT `fk_psr_player` FOREIGN KEY (`player_id`) REFERENCES `player_data` (`player_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_psr_skill` FOREIGN KEY (`skill_id`) REFERENCES `skill_template` (`skill_id`) ON DELETE CASCADE;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
