-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Máy chủ: 127.0.0.1
-- Thời gian đã tạo: Th5 21, 2026 lúc 01:11 AM
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
  `reward_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL  CHECK (json_valid(`reward_json`)),
  `thumbnail_icon_id` varchar(50) NOT NULL DEFAULT '',
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Đang đổ dữ liệu cho bảng `dungeon_config`
--

INSERT INTO `dungeon_config` (`dungeon_id`, `dungeon_name`, `dungeon_type`, `map_id`, `scene_name`, `max_players`, `min_level_required`, `time_limit_seconds`, `description`, `boss_enemy_id`, `reward_json`, `thumbnail_icon_id`, `is_active`, `created_at`, `updated_at`) VALUES
(6, 'Phó Bản Sóng', 'solo', 110, 'DungeonWaveScene', 1, 1, 0, '', NULL, '{}', '', 1, '2026-04-19 19:18:57', '2026-04-20 18:57:46'),
(7, 'Phó Bản Tổ Đội', 'multi', 111, 'DungeonPartyScene', 4, 1, 0, '', NULL, '{}', '', 1, '2026-04-19 19:18:57', '2026-04-20 18:57:50');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `dungeon_wave_config`
--

CREATE TABLE `dungeon_wave_config` (
  `dungeon_id` int(11) NOT NULL COMMENT 'FK → dungeon_config.dungeon_id',
  `max_waves` int(11) NOT NULL DEFAULT 20 COMMENT 'Số vòng tối đa, mặc định 20',
  `wave_time_seconds` int(11) NOT NULL DEFAULT 300 COMMENT 'Giây mỗi vòng, mặc định 5 phút',
  `enemy_scale_percent` float NOT NULL DEFAULT 10 COMMENT '% tăng stat quái mỗi vòng (lũy thừa)',
  `boss_scale_percent` float NOT NULL DEFAULT 15 COMMENT '% tăng stat boss mỗi vòng (lũy thừa, config riêng)',
  `exp_gold_scale_percent` float NOT NULL DEFAULT 10 COMMENT '% tăng exp/gold drop mỗi vòng (lũy thừa)',
  `daily_entry_limit` int(11) NOT NULL DEFAULT 1 COMMENT 'Lượt vào tối đa 1 ngày',
  `entry_item_plus1_id` int(11) DEFAULT 409 COMMENT 'item_template_id cho vé +1 lần',
  `entry_item_plus2_id` int(11) DEFAULT 410 COMMENT 'item_template_id cho vé +2 lần',
  `milestone_reward_json` longtext NOT NULL  COMMENT 'JSON: [{wave,exp,gold,items:[{item_template_id,qty}]}]',
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Wave-specific config per dungeon; mirrors DungeonWaveConfig SO trong Unity';

--
-- Đang đổ dữ liệu cho bảng `dungeon_wave_config`
--

INSERT INTO `dungeon_wave_config` (`dungeon_id`, `max_waves`, `wave_time_seconds`, `enemy_scale_percent`, `boss_scale_percent`, `exp_gold_scale_percent`, `daily_entry_limit`, `entry_item_plus1_id`, `entry_item_plus2_id`, `milestone_reward_json`, `updated_at`) VALUES
(6, 20, 300, 10, 15, 10, 1, 409, 410, '[\n  {\"wave\":5,  \"exp\":5000,  \"gold\":500,  \"items\":[]},\n  {\"wave\":10, \"exp\":15000, \"gold\":1500, \"items\":[]},\n  {\"wave\":15, \"exp\":30000, \"gold\":3000, \"items\":[]},\n  {\"wave\":20, \"exp\":50000, \"gold\":5000, \"items\":[{\"item_template_id\":31,\"qty\":1}]}\n]', '2026-04-21 03:01:22');

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
(11, 'Đế Băng', 'Hoàng đế băng hà cổ đại bị phong ấn', 15, 2200, 500, 120, 35, 2, 1.2, 900, 380, 1500, '[{\"item_id\":37,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":2},{\"item_id\":207,\"drop_chance\":0.08,\"qty_min\":1,\"qty_max\":1},{\"item_id\":31,\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1}]', 'Water', 'Normal', '2026-04-01 00:00:00', '2026-04-21 17:03:53', '[{\"skill_id\":\"ICE_STORM\",\"damage_multiplier\":2.0,\"element\":\"Water\",\"cooldown_sec\":10,\"range\":7,\"aoe\":true,\"animation_trigger\":\"skill_storm\"},{\"skill_id\":\"FREEZE\",\"damage_multiplier\":1.0,\"element\":\"Water\",\"cooldown_sec\":6,\"range\":4,\"status_effect\":\"frozen\",\"duration_sec\":3,\"animation_trigger\":\"skill_freeze\"},{\"skill_id\":\"BLIZZARD\",\"damage_multiplier\":1.8,\"element\":\"Water\",\"cooldown_sec\":15,\"range\":10,\"aoe\":true,\"animation_trigger\":\"skill_blizzard\"}]', 0, 75, 0, 0, 0, 0, 0, 45, 0, 0, 0, 0, 8, 20, 12, '[{\"hp_pct_threshold\":70,\"action\":\"enrage\",\"damage_multiplier\":1.3,\"speed_multiplier\":1.1,\"message\":\"Đế Băng thức tỉnh!\"},{\"hp_pct_threshold\":40,\"action\":\"encase\",\"message\":\"Đế Băng phong ấn cả chiến trường!\",\"aoe_freeze\":true},{\"hp_pct_threshold\":20,\"action\":\"berserk\",\"damage_multiplier\":2.2,\"speed_multiplier\":1.4,\"message\":\"Đế Băng huy động toàn lực!\"}]'),
(12, 'Mộc Linh', 'Tinh linh rừng, ẩn trong bóng cây', 8, 150, 30, 16, 4, 1.8, 1, 35, 16, 65, '[{\"item_id\":27,\"drop_chance\":0.45,\"qty_min\":1,\"qty_max\":3},{\"item_id\":25,\"drop_chance\":0.08,\"qty_min\":1,\"qty_max\":1}]', 'Wood', 'Boss', '2026-04-01 00:00:00', '2026-04-21 17:03:53', NULL, 0, 0, 0, 50, 0, 0, 0, 0, 0, 20, 0, 0, 1, 10, 0, NULL),
(13, 'Cổ Thọ Mộc', 'Quái vật cây cổ thụ, rễ xuyên đất', 11, 450, 80, 45, 15, 1.5, 0.8, 130, 60, 240, '[{\"item_id\":27,\"drop_chance\":0.6,\"qty_min\":2,\"qty_max\":4},{\"item_id\":25,\"drop_chance\":0.12,\"qty_min\":1,\"qty_max\":1}]', 'Wood', 'Elite', '2026-04-01 00:00:00', '2026-04-01 00:00:00', NULL, 0, 0, 0, 60, 0, 0, 0, 0, 0, 30, 0, 0, 5, 8, 5, NULL),
(14, 'Rừng Chúa', 'Thực thể rừng rậm bất tử ngàn năm', 13, 1800, 400, 100, 30, 1.8, 0.9, 750, 300, 1200, '[{\"item_id\":38,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":2},{\"item_id\":222,\"drop_chance\":0.08,\"qty_min\":1,\"qty_max\":1},{\"item_id\":31,\"drop_chance\":0.05,\"qty_min\":1,\"qty_max\":1}]', 'Wood', 'Boss', '2026-04-01 00:00:00', '2026-04-01 00:00:00', '[{\"skill_id\":\"ROOT\",\"damage_multiplier\":1.2,\"element\":\"Wood\",\"cooldown_sec\":7,\"range\":5,\"status_effect\":\"rooted\",\"duration_sec\":2,\"animation_trigger\":\"skill_root\"},{\"skill_id\":\"THORN_WALL\",\"damage_multiplier\":1.8,\"element\":\"Wood\",\"cooldown_sec\":10,\"range\":8,\"aoe\":true,\"animation_trigger\":\"skill_thorn\"},{\"skill_id\":\"REGROW\",\"heal_pct\":10,\"cooldown_sec\":25,\"animation_trigger\":\"skill_regrow\"}]', 0, 0, 0, 70, 0, 0, 0, 0, 0, 40, 0, 0, 10, 12, 8, '[{\"hp_pct_threshold\":60,\"action\":\"enrage\",\"damage_multiplier\":1.3,\"message\":\"Rừng Chúa triệu gọi thiên nhiên!\"},{\"hp_pct_threshold\":30,\"action\":\"heal\",\"heal_pct\":15,\"message\":\"Rừng Chúa hồi phục từ đất!\"},{\"hp_pct_threshold\":15,\"action\":\"berserk\",\"damage_multiplier\":2.5,\"speed_multiplier\":1.5,\"message\":\"Rừng Chúa đốt cháy cơn thịnh nộ!\"}]'),
(15, 'Hắc Quân Binh', 'Binh lính bóng tối trang bị đầy đủ', 15, 300, 60, 35, 20, 2, 1, 70, 30, 120, '[{\"item_id\":26,\"drop_chance\":0.3,\"qty_min\":1,\"qty_max\":2},{\"item_id\":11,\"drop_chance\":0.2,\"qty_min\":1,\"qty_max\":1}]', 'Metal', 'Normal', '2026-04-01 00:00:00', '2026-04-01 00:00:00', NULL, 0, 0, 0, 0, 50, 0, 0, 0, 0, 0, 20, 0, 0, 5, 10, NULL),
(16, 'Hắc Quân Vệ', 'Vệ sĩ tinh nhuệ của Chúa Tể Bóng Tối', 18, 600, 120, 65, 30, 2.2, 1.2, 180, 80, 320, '[{\"item_id\":26,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":3},{\"item_id\":15,\"drop_chance\":0.15,\"qty_min\":1,\"qty_max\":1}]', 'Metal', 'Elite', '2026-04-01 00:00:00', '2026-04-01 00:00:00', NULL, 0, 0, 0, 0, 65, 0, 0, 0, 0, 0, 30, 0, 0, 10, 15, NULL),
(17, 'Chúa Tể Bóng Tối', 'Ác chủ bất tử cai trị thành trì cổ đại', 20, 3500, 800, 160, 50, 2.5, 1.5, 1500, 600, 2500, '[{\"item_id\":39,\"drop_chance\":0.5,\"qty_min\":1,\"qty_max\":2},{\"item_id\":40,\"drop_chance\":0.3,\"qty_min\":1,\"qty_max\":1},{\"item_id\":219,\"drop_chance\":0.06,\"qty_min\":1,\"qty_max\":1},{\"item_id\":31,\"drop_chance\":0.1,\"qty_min\":1,\"qty_max\":2}]', 'Metal', 'Boss', '2026-04-01 00:00:00', '2026-04-01 00:00:00', '[{\"skill_id\":\"DARK_SLASH\",\"damage_multiplier\":2.8,\"element\":\"Metal\",\"cooldown_sec\":6,\"range\":4,\"animation_trigger\":\"skill_slash\"},{\"skill_id\":\"SHADOW_NOVA\",\"damage_multiplier\":2.0,\"element\":\"Metal\",\"cooldown_sec\":10,\"range\":10,\"aoe\":true,\"animation_trigger\":\"skill_nova\"},{\"skill_id\":\"SUMMON_GUARDS\",\"spawn_enemy_id\":16,\"spawn_count\":2,\"cooldown_sec\":25,\"animation_trigger\":\"skill_summon\"},{\"skill_id\":\"VOID_SHIELD\",\"damage_reduction_pct\":50,\"duration_sec\":5,\"cooldown_sec\":30,\"animation_trigger\":\"skill_shield\"}]', 20, 0, 0, 0, 70, 0, 10, 0, 0, 0, 40, 0, 10, 20, 20, '[{\"hp_pct_threshold\":75,\"action\":\"summon\",\"mob_id\":15,\"mob_count\":3,\"message\":\"Chúa Tể triệu hồi quân binh!\"},{\"hp_pct_threshold\":50,\"action\":\"enrage\",\"damage_multiplier\":1.4,\"speed_multiplier\":1.2,\"message\":\"Chúa Tể kích hoạt giáp bóng tối!\"},{\"hp_pct_threshold\":25,\"action\":\"berserk\",\"damage_multiplier\":2.5,\"speed_multiplier\":1.5,\"skill_cooldown_multiplier\":0.4,\"message\":\"Chúa Tể dùng tuyệt kỹ cuối cùng!\"}]');

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
-- Cấu trúc bảng cho bảng `friend_relations`
--

CREATE TABLE `friend_relations` (
  `id` int(11) NOT NULL,
  `user_id` int(11) NOT NULL,
  `friend_id` int(11) NOT NULL,
  `status` varchar(20) NOT NULL DEFAULT 'pending',
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Đang đổ dữ liệu cho bảng `friend_relations`
--

INSERT INTO `friend_relations` (`id`, `user_id`, `friend_id`, `status`, `created_at`) VALUES
(7, 16, 17, 'accepted', '2026-04-17 13:16:57');

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
  `type` tinyint(4) NOT NULL COMMENT '0=Helmet 1=Weapon 2=Armor 3=Pants 4=Boots 5=Ring 21=UpgStone 22=HPPotion 23=MPPotion 24=Food 25=GeneStone 30=Material 31=WaveTicket 32=BagExpansion',
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
(25, 'Moc Tinh', 'Tinh chat cua linh moc, roi tu Moc Linh', 'True', 2, 30, 0, 0, 1, 0, 12, 0, 0, 50),
(26, 'Quặng Sắt', 'Nguyên liệu rèn đồ cơ bản', 'True', 2, 30, 0, 0, 1, 0, 1, 0, 0, 0),
(27, 'Thảo Dược', 'Chế bình máu', 'True', 2, 30, 0, 0, 1, 0, 1, 0, 0, 0),
(28, 'Vảy Rồng', 'Nguyên liệu quý hiếm', 'True', 2, 30, 0, 0, 30, 0, 5, 0, 0, 0),
(29, 'Nanh Độc', 'Drop từ Goblin Độc', 'True', 2, 30, 0, 0, 10, 0, 2, 0, 0, 0),
(30, 'Tinh Thể Lửa', 'Drop từ Fire Slime', 'True', 2, 30, 0, 0, 5, 0, 4, 0, 0, 0),
(31, 'Lõi Đột Biến', 'Vật liệu hiếm để Hybrid Fusion 2 gene Tier 5. Chỉ rơi từ Boss hoặc sự kiện đặc biệt.', 'True', 2, 25, 0, 0, 50, 0, 5, 0, 0, 0),
(37, 'Tinh The Bang', 'Tinh the bang gia tu De Bang, dung lam nguyen lieu nang cao', 'True', 2, 30, 0, 0, 1, 0, 11, 0, 0, 200),
(42, 'Đá Nâng Cấp Cấp 8', 'Dùng để nâng cấp trang bị +21~+22. Cần trang bị cấp 3x trở lên.', 'True', 2, 21, 0, 253, 30, 0, -1, 0, 0, 0),
(43, 'Đá Nâng Cấp Cấp 9', 'Dùng để nâng cấp trang bị +23~+24. Cần trang bị cấp 4x trở lên.', 'True', 2, 21, 0, 254, 40, 0, -1, 0, 0, 0),
(44, 'Đá Nâng Cấp Cấp 10', 'Đá quý hiếm, chỉ dùng cho trang bị tối thượng.', 'True', 2, 21, 0, 255, 45, 0, -1, 0, 0, 0),
(45, 'Đá Nâng Cấp Cấp 11', 'Đá cấp cao nhất phổ thông, rất hiếm.', 'True', 2, 21, 0, 256, 48, 0, -1, 0, 0, 0),
(46, 'Đá Nâng Cấp Cấp 12', 'Đá truyền thuyết, chỉ rơi từ boss tối thượng.', 'True', 2, 21, 0, 257, 50, 0, -1, 0, 0, 0),
(47, 'Lõi Đột Biến Hỏa', 'Lõi mang tinh hoa hệ Hỏa. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Hỏa.', 'True', 2, 25, 1, 319, 50, 0, -1, 0, 0, 0),
(48, 'Lõi Đột Biến Thủy', 'Lõi mang tinh hoa hệ Thủy. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Thủy.', 'True', 2, 25, 2, 320, 50, 0, -1, 0, 0, 0),
(49, 'Lõi Đột Biến Thổ', 'Lõi mang tinh hoa hệ Thổ. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Thổ.', 'True', 2, 25, 3, 321, 50, 0, -1, 0, 0, 0),
(50, 'Lõi Đột Biến Kim', 'Lõi mang tinh hoa hệ Kim. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Kim.', 'True', 2, 25, 4, 322, 50, 0, -1, 0, 0, 0),
(51, 'Lõi Đột Biến Mộc', 'Lõi mang tinh hoa hệ Mộc. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Mộc.', 'True', 2, 25, 5, 323, 50, 0, -1, 0, 0, 0),
(52, 'Lõi Đột Biến Phong', 'Lõi mang tinh hoa hệ Phong. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Phong.', 'True', 2, 25, 6, 324, 50, 0, -1, 0, 0, 0),
(61, 'Túi Mở Rộng Cấp 1', 'Mở rộng túi đồ thêm 5 ô. Có thể gắn tối đa 3 túi.', 'False', 2, 32, 0, 283, 1, 0, -1, 0, 0, 500),
(62, 'Túi Mở Rộng Cấp 2', 'Mở rộng túi đồ thêm 5 ô. Phiên bản nâng cao.', 'False', 2, 32, 0, 284, 10, 0, -1, 0, 0, 1200),
(63, 'Túi Mở Rộng Cấp 3', 'Mở rộng túi đồ thêm 5 ô. Phiên bản cao cấp.', 'False', 2, 32, 0, 285, 25, 0, -1, 0, 0, 2500),
(64, 'Túi Mở Rộng Cấp 4', 'Mở rộng túi đồ thêm 5 ô. Phiên bản thượng cấp hiếm.', 'False', 2, 32, 0, 774, 40, 0, -1, 0, 0, 5000),
(100, 'Mũ Da Nam', 'Mũ da cơ bản, thích hợp nam lính mới', 'False', 0, 0, 0, 118, 1, 0, -1, 0, 0, 0),
(101, 'Mũ Sắt Nam', 'Mũ sắt bền, bảo vệ hiệu quả', 'False', 0, 0, 0, 119, 10, 0, -1, 0, 0, 0),
(102, 'Mũ Thép Nam', 'Mũ thép vững chắc của chiến binh', 'False', 0, 0, 0, 120, 20, 0, -1, 0, 0, 0),
(103, 'Mũ Chiến Binh Nam', 'Mũ cao cấp của chiến binh tinh nhuệ', 'False', 0, 0, 0, 121, 35, 0, -1, 0, 0, 0),
(104, 'Mũ Tinh Luyện Nam', 'Mũ tinh luyện bằng thuật nguyên tố', 'False', 0, 0, 0, 122, 50, 0, -1, 0, 0, 0),
(105, 'Mũ Lụa Nữ', 'Mũ lụa nhẹ nhàng dành cho nữ chiến binh', 'False', 1, 0, 0, 123, 1, 0, -1, 0, 0, 0),
(106, 'Mũ Bạc Nữ', 'Mũ khảm bạc thanh lịch', 'False', 1, 0, 0, 124, 10, 0, -1, 0, 0, 0),
(107, 'Mũ Ngọc Nữ', 'Mũ nạm ngọc quý, tăng cường ma lực', 'False', 1, 0, 0, 125, 20, 0, -1, 0, 0, 0),
(108, 'Mũ Nữ Chiến Binh', 'Mũ chiến đấu cao cấp dành cho nữ', 'False', 1, 0, 0, 126, 35, 0, -1, 0, 0, 0),
(109, 'Mũ Tinh Luyện Nữ', 'Mũ nữ tinh luyện bằng năng lượng tinh khiết', 'False', 1, 0, 0, 127, 50, 0, -1, 0, 0, 0),
(110, 'Áo Da Nam', 'Áo da cơ bản', 'False', 0, 2, 0, 103, 1, 0, -1, 0, 0, 0),
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
(150, 'Giày Da Nam', 'Giày da cơ bản', 'False', 0, 4, 0, 148, 1, 0, -1, 0, 0, 0),
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
(229, 'Dao Phong Thượng Cấp', 'Thương tối cùng hệ Phong, điều khiển bão tố', 'False', 2, 1, 6, 177, 50, 0, -1, 0, 0, 0),
(409, 'Vé Phó Bản (+1 Lần)', 'Cho phép vào Phó Bản Sóng thêm 1 lần trong ngày', 'True', 2, 31, 0, 861, 1, 0, -1, 0, 0, 0),
(410, 'Vé Phó Bản (+2 Lần)', 'Cho phép vào Phó Bản Sóng thêm 2 lần trong ngày', 'True', 2, 31, 0, 866, 1, 0, -1, 0, 0, 0);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `leaderboard_cache`
--

CREATE TABLE `leaderboard_cache` (
  `id` int(11) NOT NULL,
  `name` varchar(100) NOT NULL DEFAULT '',
  `list` longtext NOT NULL ,
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci ROW_FORMAT=DYNAMIC;

--
-- Đang đổ dữ liệu cho bảng `leaderboard_cache`
--

INSERT INTO `leaderboard_cache` (`id`, `name`, `list`, `updated_at`) VALUES
(1, 'Cấp Độ', '[{\"Rank\":1,\"CharacterName\":\"Phong\",\"Value\":100,\"Extra\":\"C\\u1EA5p 100\"},{\"Rank\":2,\"CharacterName\":\"kim\",\"Value\":1,\"Extra\":\"C\\u1EA5p 1\"},{\"Rank\":3,\"CharacterName\":\"Hoa\",\"Value\":1,\"Extra\":\"C\\u1EA5p 1\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":1,\"Extra\":\"C\\u1EA5p 1\"}]', '2026-05-14 23:41:28'),
(2, 'Nhiệm Vụ', '[{\"Rank\":1,\"CharacterName\":\"Phong\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":2,\"CharacterName\":\"kim\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":3,\"CharacterName\":\"Hoa\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"}]', '2026-05-14 23:41:28'),
(3, 'Chuyên Cần', '[{\"Rank\":1,\"CharacterName\":\"Phong\",\"Value\":0,\"Extra\":\"0 ng\\u00E0y\"},{\"Rank\":2,\"CharacterName\":\"kim\",\"Value\":0,\"Extra\":\"0 ng\\u00E0y\"},{\"Rank\":3,\"CharacterName\":\"Hoa\",\"Value\":0,\"Extra\":\"0 ng\\u00E0y\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":0,\"Extra\":\"0 ng\\u00E0y\"}]', '2026-05-14 23:41:28'),
(4, 'Phó Bản', '[{\"Rank\":1,\"CharacterName\":\"Phong\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":2,\"CharacterName\":\"kim\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":3,\"CharacterName\":\"Hoa\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":0,\"Extra\":\"Wave 0\"}]', '2026-05-14 23:41:28'),
(5, 'Vàng', '[{\"Rank\":1,\"CharacterName\":\"kim\",\"Value\":2000000000,\"Extra\":\"2B v\\u00E0ng\"},{\"Rank\":2,\"CharacterName\":\"Phong\",\"Value\":0,\"Extra\":\"0 v\\u00E0ng\"},{\"Rank\":3,\"CharacterName\":\"Hoa\",\"Value\":0,\"Extra\":\"0 v\\u00E0ng\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":0,\"Extra\":\"0 v\\u00E0ng\"}]', '2026-05-14 23:41:28');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `leaderboard_caches`
--

CREATE TABLE `leaderboard_caches` (
  `Id` int(11) NOT NULL,
  `Name` varchar(100) NOT NULL DEFAULT '',
  `ListJson` longtext NOT NULL ,
  `UpdatedAt` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `leaderboard_caches`
--

INSERT INTO `leaderboard_caches` (`Id`, `Name`, `ListJson`, `UpdatedAt`) VALUES
(1, 'Cấp Độ', '[{\"Rank\":1,\"CharacterName\":\"Phong\",\"Value\":100,\"Extra\":\"C\\u1EA5p 100\"},{\"Rank\":2,\"CharacterName\":\"kim\",\"Value\":5,\"Extra\":\"C\\u1EA5p 5\"},{\"Rank\":3,\"CharacterName\":\"Hoa\",\"Value\":1,\"Extra\":\"C\\u1EA5p 1\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":1,\"Extra\":\"C\\u1EA5p 1\"},{\"Rank\":5,\"CharacterName\":\"thuy123\",\"Value\":1,\"Extra\":\"C\\u1EA5p 1\"},{\"Rank\":6,\"CharacterName\":\"phong1\",\"Value\":1,\"Extra\":\"C\\u1EA5p 1\"},{\"Rank\":7,\"CharacterName\":\"hoa123\",\"Value\":1,\"Extra\":\"C\\u1EA5p 1\"}]', '2026-05-20 22:35:29'),
(2, 'Nhiệm Vụ', '[{\"Rank\":1,\"CharacterName\":\"kim\",\"Value\":2,\"Extra\":\"2 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":2,\"CharacterName\":\"Phong\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":3,\"CharacterName\":\"Hoa\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":5,\"CharacterName\":\"thuy123\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":6,\"CharacterName\":\"phong1\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"},{\"Rank\":7,\"CharacterName\":\"hoa123\",\"Value\":0,\"Extra\":\"0 nhi\\u1EC7m v\\u1EE5\"}]', '2026-05-20 22:35:29'),
(3, 'Chuyên Cần', '[{\"Rank\":1,\"CharacterName\":\"Phong\",\"Value\":5,\"Extra\":\"5 ng\\u00E0y\"},{\"Rank\":2,\"CharacterName\":\"Hoa\",\"Value\":2,\"Extra\":\"2 ng\\u00E0y\"},{\"Rank\":3,\"CharacterName\":\"hoa123\",\"Value\":2,\"Extra\":\"2 ng\\u00E0y\"},{\"Rank\":4,\"CharacterName\":\"kim\",\"Value\":1,\"Extra\":\"1 ng\\u00E0y\"},{\"Rank\":5,\"CharacterName\":\"Thuy\",\"Value\":1,\"Extra\":\"1 ng\\u00E0y\"},{\"Rank\":6,\"CharacterName\":\"thuy123\",\"Value\":1,\"Extra\":\"1 ng\\u00E0y\"},{\"Rank\":7,\"CharacterName\":\"phong1\",\"Value\":0,\"Extra\":\"0 ng\\u00E0y\"}]', '2026-05-20 22:35:29'),
(4, 'Phó Bản', '[{\"Rank\":1,\"CharacterName\":\"Phong\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":2,\"CharacterName\":\"kim\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":3,\"CharacterName\":\"Hoa\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":5,\"CharacterName\":\"thuy123\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":6,\"CharacterName\":\"phong1\",\"Value\":0,\"Extra\":\"Wave 0\"},{\"Rank\":7,\"CharacterName\":\"hoa123\",\"Value\":0,\"Extra\":\"Wave 0\"}]', '2026-05-20 22:35:29'),
(5, 'Vàng', '[{\"Rank\":1,\"CharacterName\":\"kim\",\"Value\":1999903800,\"Extra\":\"2B v\\u00E0ng\"},{\"Rank\":2,\"CharacterName\":\"Hoa\",\"Value\":100000000,\"Extra\":\"100M v\\u00E0ng\"},{\"Rank\":3,\"CharacterName\":\"Phong\",\"Value\":14990,\"Extra\":\"14.99K v\\u00E0ng\"},{\"Rank\":4,\"CharacterName\":\"Thuy\",\"Value\":0,\"Extra\":\"0 v\\u00E0ng\"},{\"Rank\":5,\"CharacterName\":\"thuy123\",\"Value\":0,\"Extra\":\"0 v\\u00E0ng\"},{\"Rank\":6,\"CharacterName\":\"phong1\",\"Value\":0,\"Extra\":\"0 v\\u00E0ng\"},{\"Rank\":7,\"CharacterName\":\"hoa123\",\"Value\":0,\"Extra\":\"0 v\\u00E0ng\"}]', '2026-05-20 22:35:29');

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
  `required_quest_id` int(11) DEFAULT NULL COMMENT 'ID nhiệm vụ phải hoàn thành (quest_config.id) trước khi vào map. NULL = không yêu cầu.',
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `map_config`
--

INSERT INTO `map_config` (`map_id`, `map_name`, `scene_name`, `spawn_points_json`, `min_level`, `max_level`, `required_quest_id`, `created_at`, `updated_at`) VALUES
(0, 'Làng Khởi Đầu', 'GameScene', '[{\"x\":0,\"y\":0},{\"x\":5,\"y\":0}]', 1, 10, NULL, '2026-03-27 06:38:35', '2026-05-18 09:35:23'),
(6, 'Địa cung (sơ cấp)', 'Map6', '[{\"x\":0,\"y\":0}]', 10, 30, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(7, 'Địa cung (trung cấp)', 'Map7', '[{\"x\":0,\"y\":0}]', 30, 60, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(18, 'Địa cung (cao cấp)', 'Map18', '[{\"x\":0,\"y\":0}]', 80, 110, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(19, 'Địa cung (thượng cấp)', 'Map19', '[{\"x\":0,\"y\":0}]', 110, 140, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(56, 'Đồi trung tâm', 'Map11', '[{\"x\":0,\"y\":0}]', 136, 155, NULL, '2026-05-08 19:41:31', '2026-05-18 09:47:21'),
(57, 'Thánh Địa Thất Kiếm', 'Map10', '[{\"x\":0,\"y\":0}]', 141, 160, NULL, '2026-05-08 19:41:31', '2026-05-18 09:47:21'),
(58, 'Hang Vô Thú', 'Map58', '[{\"x\":0,\"y\":0}]', 135, 160, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(59, 'Làng Cát', 'Map59', '[{\"x\":0,\"y\":0}]', 36, 55, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(60, 'Làng Sương Mù', 'Map60', '[{\"x\":0,\"y\":0}]', 71, 90, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(61, 'Vách Chigiri', 'Map61', '[{\"x\":0,\"y\":0}]', 41, 60, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(62, 'Núi Kirigakure', 'Map62', '[{\"x\":0,\"y\":0}]', 46, 65, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(63, 'Cánh Đồng Kaminari', 'Map63', '[{\"x\":0,\"y\":0}]', 51, 70, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(64, 'Thung Lũng Chết', 'Map64', '[{\"x\":0,\"y\":0}]', 56, 75, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(65, 'Đồi Hoang', 'Map65', '[{\"x\":0,\"y\":0}]', 61, 80, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(66, 'Hầm Núi Mizu', 'Map66', '[{\"x\":0,\"y\":0}]', 66, 85, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(67, 'Hầm bí mật', 'Map67', '[{\"x\":0,\"y\":0}]', 50, 70, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(68, 'Làng Cổ', 'Map68', '[{\"x\":0,\"y\":0}]', 76, 95, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(69, 'Làng Mây', 'Map69', '[{\"x\":0,\"y\":0}]', 106, 125, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(70, 'Vách Đá Ngang', 'Map70', '[{\"x\":0,\"y\":0}]', 81, 100, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(71, 'Miếu Iwagakure', 'Map71', '[{\"x\":0,\"y\":0}]', 86, 105, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(72, 'Chân Núi Tsuchi', 'Map72', '[{\"x\":0,\"y\":0}]', 91, 110, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(73, 'Rừng Nấm', 'Map73', '[{\"x\":0,\"y\":0}]', 96, 115, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(74, 'Dòng Sông Kusagakure', 'Map74', '[{\"x\":0,\"y\":0}]', 101, 120, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(75, 'Làng Lá', 'Map75', '[{\"x\":0,\"y\":0}]', 1, 20, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(76, 'Đồng Cỏ Tenchi', 'Map76', '[{\"x\":0,\"y\":0}]', 6, 25, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(77, 'Rừng Kumogakure', 'Map77', '[{\"x\":0,\"y\":0}]', 11, 30, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(78, 'Nghĩa Địa Bỏ Hoang', 'Map78', '[{\"x\":0,\"y\":0}]', 16, 35, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(79, 'Chiến Trường Cổ', 'Map79', '[{\"x\":0,\"y\":0}]', 21, 40, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(80, 'Đồi Cát', 'Map80', '[{\"x\":0,\"y\":0}]', 26, 45, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(81, 'Sa mạc Sunagakure', 'Map81', '[{\"x\":0,\"y\":0}]', 31, 50, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(82, 'Núi Hokage', 'Map82', '[{\"x\":0,\"y\":0}]', 111, 130, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(83, 'Thung lũng Tấn Công', 'Map09', '[{\"x\":0,\"y\":0}]', 146, 165, NULL, '2026-05-08 19:41:31', '2026-05-18 09:47:21'),
(84, 'Khu luyện tập', 'Map84', '[{\"x\":0,\"y\":0}]', 1, 15, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(85, 'Làng Đá', 'Map85', '[{\"x\":0,\"y\":0}]', 116, 135, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(86, 'Trường Konoha', 'Map86', '[{\"x\":0,\"y\":0}]', 121, 140, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(87, 'Hang Khổ', 'Map13', '[{\"x\":0,\"y\":0}]', 126, 145, NULL, '2026-05-08 19:41:31', '2026-05-18 09:47:21'),
(88, 'Cầu Kannabi', 'Map12', '[{\"x\":0,\"y\":0}]', 131, 150, NULL, '2026-05-08 19:41:31', '2026-05-18 09:47:21'),
(89, 'Vòng Lặp Ảo Tưởng', 'Map89', '[{\"x\":0,\"y\":0}]', 130, 155, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(90, 'Hang Vô Thú (cấp 1)', 'Map90', '[{\"x\":0,\"y\":0}]', 135, 160, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(91, 'Hang Vô Thú (cấp 2)', 'Map91', '[{\"x\":0,\"y\":0}]', 145, 170, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(92, 'Hang Vô Thú (cấp 3)', 'Map92', '[{\"x\":0,\"y\":0}]', 155, 180, NULL, '2026-05-08 19:41:31', '2026-05-18 09:35:23'),
(93, 'Hang Gamaken', 'Map93', '[{\"x\":0,\"y\":0}]', 125, 150, NULL, '2026-05-08 19:41:31', '2026-05-08 19:41:31'),
(94, 'Hang Gamatatsu', 'Map94', '[{\"x\":0,\"y\":0}]', 135, 160, NULL, '2026-05-08 19:41:31', '2026-05-08 19:41:31'),
(95, 'Hang Gama Armored', 'Map95', '[{\"x\":0,\"y\":0}]', 145, 170, NULL, '2026-05-08 19:41:31', '2026-05-08 19:41:31'),
(96, 'Hang Gamabunta', 'Map96', '[{\"x\":0,\"y\":0}]', 155, 180, NULL, '2026-05-08 19:41:31', '2026-05-08 19:41:31'),
(97, 'Hang Gamahiro', 'Map97', '[{\"x\":0,\"y\":0}]', 165, 190, NULL, '2026-05-08 19:41:31', '2026-05-08 19:41:31'),
(98, 'Chiến trường', 'Map08', '[{\"x\":0,\"y\":0}]', 151, 170, NULL, '2026-05-08 19:41:31', '2026-05-18 09:47:21'),
(99, 'Cửa phía tây', 'Map00', '[{\"x\":0,\"y\":0}]', 1, 175, NULL, '2026-05-08 19:41:31', '2026-05-21 03:19:09'),
(100, 'Cửa phía đông', 'Map01', '[{\"x\":0,\"y\":0}]', 1, 180, NULL, '2026-05-08 19:41:31', '2026-05-21 03:19:11'),
(101, 'Chiến trường phó bản', 'Map02', '[{\"x\":0,\"y\":0}]', 5, 185, NULL, '2026-05-08 19:41:31', '2026-05-21 03:25:20'),
(102, 'Làng Mưa', 'Map03', '[{\"x\":0,\"y\":0}]', 10, 185, 1, '2026-05-08 19:41:31', '2026-05-21 03:25:23'),
(103, 'Pháo Đài Amega', 'Map04', '[{\"x\":0,\"y\":0}]', 15, 190, 2, '2026-05-08 19:41:31', '2026-05-21 03:25:26'),
(104, 'Vùng trũng Kusa', 'Map05', '[{\"x\":0,\"y\":0}]', 20, 195, 3, '2026-05-08 19:41:31', '2026-05-21 03:25:28'),
(105, 'Lãnh Địa thiên thần', 'Map06', '[{\"x\":0,\"y\":0}]', 25, 200, NULL, '2026-05-08 19:41:31', '2026-05-21 03:25:31'),
(106, 'Căn cứ Akatsuki', 'Map07', '[{\"x\":0,\"y\":0}]', 186, 205, NULL, '2026-05-08 19:41:31', '2026-05-18 09:47:21'),
(110, 'Vòng lặp vô tận', 'DungeonWaveScene', '[{\"x\":0,\"y\":0}]', 1, 999, NULL, '2026-04-06 10:58:57', '2026-05-18 09:35:23'),
(111, 'Địa Cung', 'DungeonPartyScene', '[{\"x\":0,\"y\":0}]', 1, 999, NULL, '2026-04-06 10:58:57', '2026-05-18 09:35:23');

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
  `required_level` smallint(5) UNSIGNED DEFAULT NULL COMMENT 'Level tối thiểu của nhân vật để đi qua cổng. NULL = không yêu cầu.',
  `required_quest_id` int(11) DEFAULT NULL COMMENT 'ID nhiệm vụ phải hoàn thành (player_quest_log.quest_id) trước khi đi qua. NULL = không yêu cầu.',
  `dungeon_id` int(11) DEFAULT NULL,
  `is_active` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `map_portal`
--

INSERT INTO `map_portal` (`portal_id`, `portal_name`, `source_map_id`, `src_x`, `src_y`, `src_radius`, `dest_map_id`, `dest_scene_name`, `dest_x`, `dest_y`, `portal_type`, `portal_direction`, `required_item_id`, `required_level`, `required_quest_id`, `dungeon_id`, `is_active`) VALUES
(1, 'Làng Khởi Đầu → Làng Lá', 0, 30, 0, 2.5, 99, 'Map00', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(2, 'Làng Lá → Làng Khởi Đầu', 87, -28, 0, 2.5, 88, 'Map12', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(3, 'Làng Lá → Đồng Cỏ Tenchi', 101, -7.46, 2.58, 2.5, 100, 'Map01', 23.48, 6.26, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(4, 'Đồng Cỏ Tenchi → Làng Lá', 101, 23.35, 12.41, 2.5, 102, 'Map03', -7.46, -1.88, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(5, 'Đồng Cỏ Tenchi → Rừng Kumogakure', 76, 30, 0, 2.5, 77, 'Map77', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(6, 'Rừng Kumogakure → Đồng Cỏ Tenchi', 77, -28, 0, 2.5, 76, 'Map76', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(7, 'Rừng Kumogakure → Nghĩa Địa Bỏ Hoang', 77, 30, 0, 2.5, 78, 'Map78', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(8, 'Nghĩa Địa Bỏ Hoang → Rừng Kumogakure', 78, -28, 0, 2.5, 77, 'Map77', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(9, 'Nghĩa Địa Bỏ Hoang → Chiến Trường Cổ', 78, 30, 0, 2.5, 79, 'Map79', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(10, 'Chiến Trường Cổ → Nghĩa Địa Bỏ Hoang', 79, -28, 0, 2.5, 78, 'Map78', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(11, 'Chiến Trường Cổ → Đồi Cát', 79, 30, 0, 2.5, 80, 'Map80', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(12, 'Đồi Cát → Chiến Trường Cổ', 80, -28, 0, 2.5, 79, 'Map79', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(13, 'Đồi Cát → Sa mạc Sunagakure', 80, 30, 0, 2.5, 81, 'Map81', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(14, 'Sa mạc Sunagakure → Đồi Cát', 81, -28, 0, 2.5, 80, 'Map80', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(15, 'Sa mạc Sunagakure → Làng Cát', 81, 30, 0, 2.5, 59, 'Map59', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(16, 'Làng Cát → Sa mạc Sunagakure', 59, -28, 0, 2.5, 81, 'Map81', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(17, 'Làng Cát → Vách Chigiri', 59, 30, 0, 2.5, 61, 'Map61', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(18, 'Vách Chigiri → Làng Cát', 61, -28, 0, 2.5, 59, 'Map59', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(19, 'Vách Chigiri → Núi Kirigakure', 61, 30, 0, 2.5, 62, 'Map62', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(20, 'Núi Kirigakure → Vách Chigiri', 62, -28, 0, 2.5, 61, 'Map61', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(21, 'Núi Kirigakure → Cánh Đồng Kaminari', 62, 30, 0, 2.5, 63, 'Map63', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(22, 'Cánh Đồng Kaminari → Núi Kirigakure', 63, -28, 0, 2.5, 62, 'Map62', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(23, 'Cánh Đồng Kaminari → Thung Lũng Chết', 63, 30, 0, 2.5, 64, 'Map64', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(24, 'Thung Lũng Chết → Cánh Đồng Kaminari', 64, -28, 0, 2.5, 63, 'Map63', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(25, 'Thung Lũng Chết → Đồi Hoang', 64, 30, 0, 2.5, 65, 'Map65', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(26, 'Đồi Hoang → Thung Lũng Chết', 65, -28, 0, 2.5, 64, 'Map64', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(27, 'Đồi Hoang → Hầm Núi Mizu', 65, 30, 0, 2.5, 66, 'Map66', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(28, 'Hầm Núi Mizu → Đồi Hoang', 66, -28, 0, 2.5, 65, 'Map65', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(29, 'Hầm Núi Mizu → Làng Sương Mù', 66, 30, 0, 2.5, 60, 'Map60', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(30, 'Làng Sương Mù → Hầm Núi Mizu', 60, -28, 0, 2.5, 66, 'Map66', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(31, 'Làng Sương Mù → Làng Cổ', 60, 30, 0, 2.5, 68, 'Map68', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(32, 'Làng Cổ → Làng Sương Mù', 68, -28, 0, 2.5, 60, 'Map60', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(33, 'Làng Cổ → Vách Đá Ngang', 68, 30, 0, 2.5, 70, 'Map70', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(34, 'Vách Đá Ngang → Làng Cổ', 70, -28, 0, 2.5, 68, 'Map68', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(35, 'Vách Đá Ngang → Miếu Iwagakure', 70, 30, 0, 2.5, 71, 'Map71', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(36, 'Miếu Iwagakure → Vách Đá Ngang', 71, -28, 0, 2.5, 70, 'Map70', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(37, 'Miếu Iwagakure → Chân Núi Tsuchi', 71, 30, 0, 2.5, 72, 'Map72', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(38, 'Chân Núi Tsuchi → Miếu Iwagakure', 72, -28, 0, 2.5, 71, 'Map71', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(39, 'Chân Núi Tsuchi → Rừng Nấm', 72, 30, 0, 2.5, 73, 'Map73', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(40, 'Rừng Nấm → Chân Núi Tsuchi', 73, -28, 0, 2.5, 72, 'Map72', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(41, 'Rừng Nấm → Dòng Sông Kusagakure', 73, 30, 0, 2.5, 74, 'Map74', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(42, 'Dòng Sông Kusagakure → Rừng Nấm', 74, -28, 0, 2.5, 73, 'Map73', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(43, 'Dòng Sông Kusagakure → Làng Mây', 74, 30, 0, 2.5, 69, 'Map69', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(44, 'Làng Mây → Dòng Sông Kusagakure', 69, -28, 0, 2.5, 74, 'Map74', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(45, 'Làng Mây → Núi Hokage', 69, 30, 0, 2.5, 82, 'Map82', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(46, 'Núi Hokage → Làng Mây', 82, -28, 0, 2.5, 69, 'Map69', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(47, 'Núi Hokage → Làng Đá', 82, 30, 0, 2.5, 85, 'Map85', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(48, 'Làng Đá → Núi Hokage', 85, -28, 0, 2.5, 82, 'Map82', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(49, 'Làng Đá → Trường Konoha', 85, 30, 0, 2.5, 86, 'Map86', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(50, 'Trường Konoha → Làng Đá', 86, -28, 0, 2.5, 85, 'Map85', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(51, 'Trường Konoha → Hang Khổ', 86, 30, 0, 2.5, 87, 'Map87', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(52, 'Hang Khổ → Trường Konoha', 106, -28, 0, 2.5, 98, 'Map08', 28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(53, 'Hang Khổ → Cầu Kannabi', 88, 30, 0, 2.5, 87, 'Map13', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(54, 'Cầu Kannabi → Hang Khổ', 88, -28, 0, 2.5, 56, 'Map11', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(55, 'Cầu Kannabi → Đồi trung tâm', 56, 30, 0, 2.5, 88, 'Map12', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(56, 'Đồi trung tâm → Cầu Kannabi', 56, -28, 0, 2.5, 57, 'Map10', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(57, 'Đồi trung tâm → Thánh Địa Thất Kiếm', 57, 30, 0, 2.5, 56, 'Map11', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(58, 'Thánh Địa Thất Kiếm → Đồi trung tâm', 57, -28, 0, 2.5, 83, 'Map09', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(59, 'Thánh Địa Thất Kiếm → Thung lũng Tấn Công', 83, 30, 0, 2.5, 57, 'Map10', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(60, 'Thung lũng Tấn Công → Thánh Địa Thất Kiếm', 83, -28, 0, 2.5, 98, 'Map08', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(61, 'Thung lũng Tấn Công → Chiến trường', 98, 30, 0, 2.5, 83, 'Map09', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(62, 'Chiến trường → Thung lũng Tấn Công', 98, -28, 0, 2.5, 106, 'Map07', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(63, 'Chiến trường → Cửa phía tây', 75, 30, 0, 2.5, 76, 'Map04', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(64, 'Cửa phía tây → Chiến trường', 99, -7.46, -1.58, 2.5, 0, 'GameScene', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(65, 'Cửa phía tây → Cửa phía đông', 99, 30.38, -1.34, 2.5, 100, 'Map01', -7.46, 4.88, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(66, 'Cửa phía đông → Cửa phía tây', 100, -7.46, 4.88, 2.5, 99, 'Map00', 30.38, -1.34, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(67, 'Cửa phía đông → Làng Mưa', 100, 23.48, 6.26, 2.5, 101, 'Map02', -7.46, 2.58, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(68, 'Làng Mưa → Cửa phía đông', 102, -7.46, -1.88, 2.5, 101, 'Map02', 23.35, 12.41, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(69, 'Làng Mưa → Pháo Đài Amega', 102, 32.92, -1.48, 2.5, 103, 'Map04', -7.46, -2.64, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(70, 'Pháo Đài Amega → Làng Mưa', 103, -7.46, -2.64, 2.5, 102, 'Map03', 32.92, -1.48, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(71, 'Pháo Đài Amega → Vùng trũng Kusa', 103, 49.4, -2.4, 2.5, 104, 'Map05', -7.46, 2.18, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(72, 'Vùng trũng Kusa → Pháo Đài Amega', 104, -7.46, 2.18, 2.5, 103, 'Map04', 49.4, -2.4, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(73, 'Vùng trũng Kusa → Lãnh Địa thiên thần', 104, 30, 0, 2.5, 105, 'Map06', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(74, 'Lãnh Địa thiên thần → Vùng trũng Kusa', 105, -28, 0, 2.5, 104, 'Map05', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(75, 'Lãnh Địa thiên thần → Căn cứ Akatsuki', 105, 30, 0, 2.5, 106, 'Map07', -28, 0, 'world_travel', 'right', NULL, NULL, NULL, NULL, 1),
(76, 'Căn cứ Akatsuki → Lãnh Địa thiên thần', 106, -28, 0, 2.5, 105, 'Map06', 28, 0, 'world_travel', 'left', NULL, NULL, NULL, NULL, 1),
(77, 'Làng Khởi Đầu → Địa cung (sơ cấp)', 0, 0, 0, 3, 6, 'Map6', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(78, 'Địa cung (sơ cấp) → Làng Khởi Đầu', 6, 0, 0, 3, 0, 'GameScene', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(79, 'Làng Khởi Đầu → Địa cung (trung cấp)', 0, 0, 0, 3, 7, 'Map7', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(80, 'Địa cung (trung cấp) → Làng Khởi Đầu', 7, 0, 0, 3, 0, 'GameScene', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(81, 'Làng Khởi Đầu → Địa cung (cao cấp)', 0, 0, 0, 3, 18, 'Map18', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(82, 'Địa cung (cao cấp) → Làng Khởi Đầu', 18, 0, 0, 3, 0, 'GameScene', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(83, 'Làng Khởi Đầu → Địa cung (thượng cấp)', 0, 0, 0, 3, 19, 'Map19', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(84, 'Địa cung (thượng cấp) → Làng Khởi Đầu', 19, 0, 0, 3, 0, 'GameScene', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(85, 'Đồi trung tâm → Hang Vô Thú', 56, 0, 0, 3, 58, 'Map58', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(86, 'Hang Vô Thú → Đồi trung tâm', 58, 0, 0, 3, 56, 'Map56', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(87, 'Cánh Đồng Kaminari → Hầm bí mật', 63, 0, 0, 3, 67, 'Map67', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(88, 'Hầm bí mật → Cánh Đồng Kaminari', 67, 0, 0, 3, 63, 'Map63', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(89, 'Làng Lá → Khu luyện tập', 75, 0, 0, 3, 84, 'Map84', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(90, 'Khu luyện tập → Làng Lá', 84, 0, 0, 3, 75, 'Map75', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(91, 'Cầu Kannabi → Vòng Lặp Ảo Tưởng', 88, 0, 0, 3, 89, 'Map89', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(92, 'Vòng Lặp Ảo Tưởng → Cầu Kannabi', 89, 0, 0, 3, 88, 'Map88', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(93, 'Hang Vô Thú → Hang Vô Thú (cấp 1)', 58, 0, 0, 3, 90, 'Map90', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(94, 'Hang Vô Thú (cấp 1) → Hang Vô Thú', 90, 0, 0, 3, 58, 'Map58', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(95, 'Hang Vô Thú → Hang Vô Thú (cấp 2)', 58, 0, 0, 3, 91, 'Map91', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(96, 'Hang Vô Thú (cấp 2) → Hang Vô Thú', 91, 0, 0, 3, 58, 'Map58', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(97, 'Hang Vô Thú → Hang Vô Thú (cấp 3)', 58, 0, 0, 3, 92, 'Map92', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(98, 'Hang Vô Thú (cấp 3) → Hang Vô Thú', 92, 0, 0, 3, 58, 'Map58', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(99, 'Hang Khổ → Hang Gamaken', 87, 0, 0, 3, 93, 'Map93', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(100, 'Hang Gamaken → Hang Khổ', 93, 0, 0, 3, 87, 'Map87', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(101, 'Hang Khổ → Hang Gamatatsu', 87, 0, 0, 3, 94, 'Map94', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(102, 'Hang Gamatatsu → Hang Khổ', 94, 0, 0, 3, 87, 'Map87', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(103, 'Hang Khổ → Hang Gama Armored', 87, 0, 0, 3, 95, 'Map95', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(104, 'Hang Gama Armored → Hang Khổ', 95, 0, 0, 3, 87, 'Map87', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(105, 'Hang Khổ → Hang Gamabunta', 87, 0, 0, 3, 96, 'Map96', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(106, 'Hang Gamabunta → Hang Khổ', 96, 0, 0, 3, 87, 'Map87', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(107, 'Hang Khổ → Hang Gamahiro', 87, 0, 0, 3, 97, 'Map97', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(108, 'Hang Gamahiro → Hang Khổ', 97, 0, 0, 3, 87, 'Map87', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(109, 'Cửa phía đông → Chiến trường phó bản', 100, 0, 0, 3, 101, 'Map02', 0, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(110, 'Chiến trường phó bản → Cửa phía đông', 101, 0, 0, 3, 100, 'Map01', 5, 0, 'world_travel', 'none', NULL, NULL, NULL, NULL, 1),
(111, 'Vào Vòng lặp vô tận', 0, 5, 0, 3, 110, 'DungeonWaveScene', 0, 0, 'enter_dungeon', 'none', NULL, NULL, NULL, 110, 1),
(112, 'Vào Địa Cung', 0, -5, 0, 3, 111, 'DungeonPartyScene', 0, 0, 'enter_dungeon', 'none', NULL, NULL, NULL, 111, 1);

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `map_spawn_config`
--

CREATE TABLE `map_spawn_config` (
  `id` int(11) NOT NULL,
  `map_id` int(11) NOT NULL COMMENT 'FK → map_config.map_id',
  `spawn_json` longtext NOT NULL  COMMENT 'JSON array — mỗi entry = 1 điểm spawn: {enemy_id,hp,exp,cx,cy,is_boss,count,respawn_time}',
  `drop_json` longtext NOT NULL  COMMENT 'JSON array — mỗi entry = 1 loại quái: {enemy_id, items:[{item_id,rate,qty_min,qty_max}]}',
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Cấu hình spawn enemy và tỉ lệ drop theo mapId — Unity host đọc khi khởi động scene';

--
-- Đang đổ dữ liệu cho bảng `map_spawn_config`
--

INSERT INTO `map_spawn_config` (`id`, `map_id`, `spawn_json`, `drop_json`, `updated_at`) VALUES
(1, 0, '[]', '[\n   {\"enemy_id\":1,\"items\":[\n     {\"item_id\":1,\"rate\":1,\"qty_min\":1,\"qty_max\":2},\n     {\"item_id\":1,\"rate\":0.05,\"qty_min\":1,\"qty_max\":1}\n   ]},\n   {\"enemy_id\":2,\"items\":[\n     {\"item_id\":22,\"rate\":0.20,\"qty_min\":1,\"qty_max\":1},\n     {\"item_id\":10,\"rate\":0.03,\"qty_min\":1,\"qty_max\":1}\n   ]},\n   {\"enemy_id\":4,\"items\":[\n     {\"item_id\":50,\"rate\":1.00,\"qty_min\":1,\"qty_max\":1},\n     {\"item_id\":10,\"rate\":0.50,\"qty_min\":1,\"qty_max\":2},\n     {\"item_id\":21,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\n   ]}\n ]', '2026-05-21 03:43:56'),
(2, 1, '[{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-8.14,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-7.2,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-6.27,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-5.34,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-4.41,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-3.48,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-2.55,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-1.62,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-0.69,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-8.12,\"cy\":15.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-7.19,\"cy\":15.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-6.27,\"cy\":15.46,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-6.25,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-3.64,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-1.03,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":1.58,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":4.2,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":6.81,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":9.42,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":12.03,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":14.64,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":17.25,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":19.87,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":22.48,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-6.49,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-5.51,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-4.52,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-3.54,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-2.55,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-1.57,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-0.58,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":0.4,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":1.39,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":2.37,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-5.1,\"cy\":14.51,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-4.33,\"cy\":14.51,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-3.56,\"cy\":14.51,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-3.02,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-2.03,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-1.03,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-0.04,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":0.95,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":1.94,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":2.93,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-0.64,\"cy\":9.9,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":0.24,\"cy\":9.9,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":1.13,\"cy\":9.9,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":2.02,\"cy\":9.9,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":2.91,\"cy\":9.9,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":1.15,\"cy\":3.25,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":2.11,\"cy\":3.25,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":3.07,\"cy\":3.25,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":3.82,\"cy\":3.35,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":4.71,\"cy\":3.35,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":5.6,\"cy\":3.35,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":6.49,\"cy\":3.35,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":7.38,\"cy\":3.35,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":8.27,\"cy\":3.35,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":9.16,\"cy\":3.35,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":4.15,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":5.12,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":6.09,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":7.07,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":8.04,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":9.01,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":9.99,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":10.96,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":11.93,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":6.15,\"cy\":12.34,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":7.14,\"cy\":12.34,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":8.14,\"cy\":12.34,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":9.13,\"cy\":12.34,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":10.13,\"cy\":12.34,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":6.49,\"cy\":3.18,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":7.32,\"cy\":3.18,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":9.51,\"cy\":2.36,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":10.29,\"cy\":2.36,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":12.15,\"cy\":1.57,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":12.96,\"cy\":1.57,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":12.86,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":13.82,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":14.78,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":15.74,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":16.71,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":17.67,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":18.63,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":19.59,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":20.56,\"cy\":13.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":15.36,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":16.33,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":17.3,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":18.28,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":19.25,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":20.22,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":21.19,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":22.17,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":23.14,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":24.11,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":16.58,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":17.52,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":18.46,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":19.4,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":20.34,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":21.28,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":22.21,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":23.15,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":24.09,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":21.36,\"cy\":5.56,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":22.3,\"cy\":5.56,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":23.24,\"cy\":5.56,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":24.18,\"cy\":5.56,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3},{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":22.15,\"cy\":14.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":1},{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":23.12,\"cy\":14.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":2},{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":24.09,\"cy\":14.0,\"is_boss\":false,\"count\":1,\"respawn_time\":15,\"level\":3}]', '[\n     {\"enemy_id\":2,\"items\":[\n       {\"item_id\":30,\"rate\":0.35,\"qty_min\":1,\"qty_max\":2},\n       {\"item_id\":21,\"rate\":0.05,\"qty_min\":1,\"qty_max\":1}\n     ]},\n     {\"enemy_id\":8,\"items\":[\n       {\"item_id\":28,\"rate\":0.40,\"qty_min\":1,\"qty_max\":2},\n       {\"item_id\":47,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\n     ]}\n  ]', '2026-05-19 03:56:43'),
(6, 100, '[\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-8.14,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-4.41,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-0.69,\"cy\":4.46,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-8.12,\"cy\":15.46,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-6.25,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":1.58,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":9.42,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":17.25,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":22.48,\"cy\":-1.16,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-6.49,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-2.55,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":1.39,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-5.1,\"cy\":14.51,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-3.02,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-0.64,\"cy\":9.9,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":2.91,\"cy\":9.9,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":1.15,\"cy\":3.25,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":3.82,\"cy\":3.35,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":5.12,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":9.01,\"cy\":8.94,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":6.15,\"cy\":12.34,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":10.13,\"cy\":12.34,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":9.51,\"cy\":2.36,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":12.15,\"cy\":1.57,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":12.86,\"cy\":13,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":16.71,\"cy\":13,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":20.56,\"cy\":13,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":15.36,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":19.25,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":23.14,\"cy\":2.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":16.58,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":20.34,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":24.09,\"cy\":8.31,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":21.36,\"cy\":5.56,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":22.15,\"cy\":14,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":24.09,\"cy\":14,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3}\n]', '[\n     {\"enemy_id\":3,\"items\":[\n       {\"item_id\":26,\"rate\":0.40,\"qty_min\":1,\"qty_max\":3},\n       {\"item_id\":2,\"rate\":0.25,\"qty_min\":1,\"qty_max\":2}\n     ]},\n     {\"enemy_id\":7,\"items\":[\n       {\"item_id\":17,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\n     ]},\n     {\"enemy_id\":9,\"items\":[\n       {\"item_id\":48,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\n     ]}\n  ]', '2026-05-21 02:27:49'),
(7, 110, '[\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":-4,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":-1.5,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":1,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":3.5,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":6,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":8.5,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":11,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":13.5,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":16,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":18.5,\"cy\":-1.7,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":-4.56,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":-2.06,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":0.44,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":2.94,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":5.44,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":7.94,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":10.44,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":12.94,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":15.44,\"cy\":2.21,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":-4.29,\"cy\":5.88,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":-1.79,\"cy\":5.88,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":0.71,\"cy\":5.88,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":3.21,\"cy\":5.88,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":5.71,\"cy\":5.88,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":8.21,\"cy\":5.88,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":10.71,\"cy\":5.88,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n{\"enemy_id\":11,\"hp\":110,\"exp\":1000,\"cx\":13.21,\"cy\":5.88,\"is_boss\":false,\"count\":1,\"respawn_time\":0,\"level\":5},\n\n\n{\"enemy_id\":12,\"hp\":1100,\"exp\":100000,\"cx\":18.55,\"cy\":5.88,\"is_boss\":true,\"count\":1,\"respawn_time\":0,\"level\":10}\n\n]', '[]', '2026-04-22 00:14:16'),
(8, 99, '[\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-5.58,\"cy\":-2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":0.66,\"cy\":-2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":6.91,\"cy\":-2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":13.15,\"cy\":-2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":19.39,\"cy\":-2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":25.64,\"cy\":-2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-7.27,\"cy\":3.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-5.52,\"cy\":3.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-3.78,\"cy\":3.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-4.76,\"cy\":0.79,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-3.01,\"cy\":0.79,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":-0.81,\"cy\":3.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":0.94,\"cy\":3.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":2.68,\"cy\":3.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":1.14,\"cy\":0.66,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":2.89,\"cy\":0.66,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":3.98,\"cy\":-1.33,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":5.76,\"cy\":-1.33,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":7.54,\"cy\":-1.33,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":9.32,\"cy\":-1.33,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":6.03,\"cy\":3.69,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":7.78,\"cy\":3.69,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":10.14,\"cy\":0.58,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":11.89,\"cy\":0.58,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":13.63,\"cy\":0.58,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":11.88,\"cy\":3.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":13.62,\"cy\":3.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":15.7,\"cy\":3.29,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":17.03,\"cy\":0.75,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":18.78,\"cy\":0.75,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":17.45,\"cy\":3.69,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":19.35,\"cy\":3.69,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":21.26,\"cy\":3.69,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":21.74,\"cy\":0.63,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":24.15,\"cy\":5.69,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":26.06,\"cy\":5.69,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":27.96,\"cy\":5.69,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":26.01,\"cy\":2.14,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":27.98,\"cy\":2.14,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":29.95,\"cy\":2.14,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1},\n\n  {\"enemy_id\":1,\"hp\":50,\"exp\":50,\"cx\":30.38,\"cy\":4.28,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":1}\n]', '[]', '2026-05-21 01:17:45'),
(10, 101, '[\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-7.94,\"cy\":2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-5.97,\"cy\":2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-4.01,\"cy\":2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-2.05,\"cy\":2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-7.99,\"cy\":11.82,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-5.31,\"cy\":11.82,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-5.71,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-2.58,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":0.56,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":3.69,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":6.82,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":9.95,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":13.09,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":16.22,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":19.35,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":22.48,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":25.62,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":28.75,\"cy\":-3.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-6.96,\"cy\":5.19,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-5.02,\"cy\":5.19,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-3.07,\"cy\":5.19,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-1.13,\"cy\":5.19,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":0.82,\"cy\":5.19,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-3.86,\"cy\":10.83,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":-1.93,\"cy\":10.83,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":0,\"cy\":10.83,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":1.94,\"cy\":10.83,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":3.87,\"cy\":10.83,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":0.89,\"cy\":0.95,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":2.85,\"cy\":0.95,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":4.41,\"cy\":9.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":6.36,\"cy\":9.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":8.31,\"cy\":9.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":10.25,\"cy\":9.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":12.2,\"cy\":9.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":4.24,\"cy\":-1.99,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":5.46,\"cy\":5.29,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":7.21,\"cy\":-0.47,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":9.21,\"cy\":13.59,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":9.48,\"cy\":-1.73,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":11.44,\"cy\":-1.73,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":13.4,\"cy\":-1.73,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":15.35,\"cy\":-1.73,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":17.31,\"cy\":-1.73,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":10.49,\"cy\":-0.47,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":11.71,\"cy\":1.88,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":12.27,\"cy\":11.38,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":12.68,\"cy\":6.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":14.61,\"cy\":6.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":16.55,\"cy\":6.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":18.49,\"cy\":6.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":20.42,\"cy\":6.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":15.44,\"cy\":1.85,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":17.4,\"cy\":1.85,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":19.35,\"cy\":1.85,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":21.31,\"cy\":1.85,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":23.27,\"cy\":1.85,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":16.12,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":18.78,\"cy\":12.41,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":21.36,\"cy\":11.49,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":23.32,\"cy\":11.49,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":21.94,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":23.93,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":25.92,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":2,\"hp\":80,\"exp\":20,\"cx\":27.91,\"cy\":7.48,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":21.74,\"cy\":4.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3}\n]', '[]', '2026-05-21 02:29:21'),
(11, 102, '[\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-5.45,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-2.17,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":1.11,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":4.39,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":7.67,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":10.95,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":14.23,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":17.51,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":20.79,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":24.07,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":27.35,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":30.63,\"cy\":-2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-7.34,\"cy\":3.93,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-5.43,\"cy\":3.93,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-5.54,\"cy\":0.9,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-3.85,\"cy\":0.9,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":-2.17,\"cy\":0.9,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-1.05,\"cy\":3.37,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":0.94,\"cy\":3.37,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":0.4,\"cy\":0.87,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":2.14,\"cy\":0.87,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":3.89,\"cy\":0.87,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":4.39,\"cy\":-1.47,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":6.19,\"cy\":-1.47,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":8,\"cy\":-1.47,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":9.81,\"cy\":-1.47,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":5.72,\"cy\":3.72,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":7.69,\"cy\":3.72,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":10.88,\"cy\":0.61,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":12.82,\"cy\":0.61,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":12.05,\"cy\":3.34,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":13.98,\"cy\":3.34,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":16.42,\"cy\":1.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":18.85,\"cy\":1.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":21.28,\"cy\":1.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":23.71,\"cy\":1.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":26.15,\"cy\":1.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":28.58,\"cy\":1.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":18.74,\"cy\":3.52,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":20.73,\"cy\":3.52,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":27.83,\"cy\":3.83,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":29.78,\"cy\":3.51,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3},\n{\"enemy_id\":3,\"hp\":150,\"exp\":50,\"cx\":31.64,\"cy\":3.51,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":3}\n]', '[]', '2026-05-21 02:31:24');
INSERT INTO `map_spawn_config` (`id`, `map_id`, `spawn_json`, `drop_json`, `updated_at`) VALUES
(12, 103, '[\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-4.28,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":0.31,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":4.9,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":9.5,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":14.09,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":18.68,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":23.28,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":27.87,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":32.47,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":37.06,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":41.65,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":46.25,\"cy\":-3.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-7.32,\"cy\":1.88,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-5.53,\"cy\":1.88,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-3.75,\"cy\":1.88,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-1.97,\"cy\":1.88,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-6.62,\"cy\":5.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-4.62,\"cy\":5.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-2.63,\"cy\":5.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-0.63,\"cy\":5.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-5.35,\"cy\":-0.97,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-3.57,\"cy\":-0.97,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-1.79,\"cy\":-0.97,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-0.01,\"cy\":-0.97,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-3.43,\"cy\":5.11,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":-1.6,\"cy\":5.11,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":0.23,\"cy\":5.11,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":2.05,\"cy\":5.11,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":0.22,\"cy\":2.35,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":1.58,\"cy\":2.78,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":3.4,\"cy\":2.78,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":5.22,\"cy\":2.78,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":2.29,\"cy\":-0.73,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":3.46,\"cy\":-0.55,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":4.53,\"cy\":5.05,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":6.49,\"cy\":5.05,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":8.46,\"cy\":5.05,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":10.42,\"cy\":5.05,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":4.8,\"cy\":-0.2,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":6.6,\"cy\":-0.2,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":8.4,\"cy\":-0.2,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":10.2,\"cy\":-0.2,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":8.4,\"cy\":2.12,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":10.07,\"cy\":1.75,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":11.82,\"cy\":1.75,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":13.58,\"cy\":1.75,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":11.73,\"cy\":-0.76,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":13.68,\"cy\":-0.76,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":15.63,\"cy\":-0.76,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":17.57,\"cy\":-0.76,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":12.62,\"cy\":4.93,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":14.4,\"cy\":4.93,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":16.18,\"cy\":4.93,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":16.39,\"cy\":2.27,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":20.42,\"cy\":-0.64,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":21.39,\"cy\":4.99,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":23.27,\"cy\":4.99,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":25.15,\"cy\":4.99,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":27.03,\"cy\":4.99,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":28.91,\"cy\":4.99,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":21.71,\"cy\":-0.28,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":23.52,\"cy\":-0.28,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":25.32,\"cy\":-0.28,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":27.12,\"cy\":-0.28,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":25.32,\"cy\":2.14,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":27.09,\"cy\":1.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":28.89,\"cy\":1.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":30.69,\"cy\":1.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":32.5,\"cy\":1.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":28.25,\"cy\":-0.94,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":30.14,\"cy\":-0.57,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":32.03,\"cy\":-0.57,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":33.92,\"cy\":-0.57,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":35.81,\"cy\":-0.57,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":31.31,\"cy\":4.9,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":33.23,\"cy\":4.9,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":35.14,\"cy\":4.9,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":34.44,\"cy\":2.26,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":37.97,\"cy\":-1.01,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":38.31,\"cy\":2.81,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":40.27,\"cy\":2.81,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":38.53,\"cy\":-0.75,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":39.38,\"cy\":5.03,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":41.2,\"cy\":5.03,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":43.01,\"cy\":5.03,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":44.83,\"cy\":5.03,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":46.65,\"cy\":5.03,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":40.04,\"cy\":-0.22,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":42.01,\"cy\":-0.22,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":43.99,\"cy\":-0.22,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":43.37,\"cy\":2.07,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":45.14,\"cy\":1.8,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":47.02,\"cy\":1.8,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":48.89,\"cy\":1.8,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":46.34,\"cy\":-0.87,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":49.11,\"cy\":-0.75,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":49.2,\"cy\":5.08,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":4,\"hp\":70,\"exp\":15,\"cx\":49.99,\"cy\":5.08,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":2}\n]', '[]', '2026-05-21 02:32:55'),
(13, 104, '[\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-7.91,\"cy\":1.62,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-6.04,\"cy\":1.62,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-4.16,\"cy\":1.62,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-2.29,\"cy\":1.62,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":-0.42,\"cy\":1.62,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":1.45,\"cy\":1.62,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-7.89,\"cy\":5.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-6.09,\"cy\":5.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-4.28,\"cy\":5.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-2.47,\"cy\":5.06,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-5.15,\"cy\":-2.56,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":2.11,\"cy\":-2.56,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":9.38,\"cy\":-2.56,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":16.64,\"cy\":-2.56,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":23.9,\"cy\":-2.56,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":31.17,\"cy\":-2.56,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-6.55,\"cy\":8.61,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-4.56,\"cy\":8.61,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-2.56,\"cy\":8.61,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":-0.57,\"cy\":8.61,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":0.05,\"cy\":-1.51,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":1.33,\"cy\":-0.81,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":1.45,\"cy\":6.62,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":2.32,\"cy\":-0.03,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":2.51,\"cy\":5.91,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":4.34,\"cy\":5.91,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":6.16,\"cy\":5.91,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":7.98,\"cy\":5.91,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":9.81,\"cy\":5.91,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":3.53,\"cy\":0.6,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":3.7,\"cy\":1.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":5.49,\"cy\":1.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":7.29,\"cy\":1.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":9.08,\"cy\":1.77,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":7.22,\"cy\":8.17,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":9.02,\"cy\":8.17,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":10.81,\"cy\":8.17,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":12.61,\"cy\":8.17,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":11.21,\"cy\":0.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":13.08,\"cy\":0.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":14.94,\"cy\":0.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":16.8,\"cy\":0.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":18.67,\"cy\":0.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":20.53,\"cy\":0.68,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":11.31,\"cy\":3.22,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":11.74,\"cy\":5.44,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":13.13,\"cy\":8.98,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":15.12,\"cy\":8.98,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":17.11,\"cy\":8.98,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":19.1,\"cy\":8.98,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":13.81,\"cy\":5.05,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":15.62,\"cy\":5.05,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":17.42,\"cy\":5.05,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":19.23,\"cy\":5.05,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":23.28,\"cy\":6.21,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":25.18,\"cy\":6.21,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":27.08,\"cy\":6.21,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":28.98,\"cy\":6.21,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":30.89,\"cy\":6.21,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":23.59,\"cy\":2.31,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":25.37,\"cy\":2.31,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":27.15,\"cy\":2.31,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":24.24,\"cy\":-1.94,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":25.64,\"cy\":-1.35,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":27.38,\"cy\":-0.59,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":29.16,\"cy\":-0.59,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":30.94,\"cy\":-0.59,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":30.34,\"cy\":2.79,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":32.29,\"cy\":5.4,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":32.71,\"cy\":6.85,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":7,\"hp\":220,\"exp\":65,\"cx\":33.09,\"cy\":4.4,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":10},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":33.34,\"cy\":3.46,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":35.14,\"cy\":3.46,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":34.65,\"cy\":6.95,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8},\n{\"enemy_id\":6,\"hp\":200,\"exp\":60,\"cx\":36.52,\"cy\":6.95,\"is_boss\":false,\"count\":1,\"respawn_time\":10,\"level\":8}\n]', '[]', '2026-05-21 02:34:44'),
(14, 105, '[]', '[]', '2026-05-21 04:43:57'),
(15, 106, '[]', '[]', '2026-05-21 04:41:59'),
(16, 98, '[]', '[]', '2026-05-21 04:42:04'),
(17, 83, '[]', '[]', '2026-05-21 04:42:11'),
(18, 57, '[]', '[]', '2026-05-21 04:42:25'),
(19, 56, '[]', '[]', '2026-05-21 04:44:40'),
(20, 88, '[]', '[]', '2026-05-21 04:42:17'),
(21, 87, '[]', '[]', '2026-05-21 04:42:21'),
(24, 111, '[{\"hp\": 50000, \"exp\": 50000, \"cx\": -8.14, \"cy\": 4.46, \"is_boss\": false, \"count\": 1, \"respawn_time\": 15, \"level\": 10}]', '[\n     {\"enemy_id\":25,\"items\":[\n       {\"item_id\":26,\"rate\":0.90,\"qty_min\":1,\"qty_max\":3},\n       {\"item_id\":2,\"rate\":0.25,\"qty_min\":1,\"qty_max\":2}\n     ]},\n     {\"enemy_id\":7,\"items\":[\n       {\"item_id\":17,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\n     ]},\n     {\"enemy_id\":9,\"items\":[\n       {\"item_id\":48,\"rate\":0.10,\"qty_min\":1,\"qty_max\":1}\n     ]}\n  ]', '2026-05-21 04:40:30');

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
  `is_active` tinyint(1) NOT NULL DEFAULT 1,
  `shop_items_json` text DEFAULT NULL COMMENT 'JSON: {"shop_name":"...","items":[{"item_template_id":1,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1}]}'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `npc_config`
--

INSERT INTO `npc_config` (`npc_id`, `npc_name`, `npc_type`, `map_id`, `pos_x`, `pos_y`, `dialogue_key`, `icon_id`, `is_active`, `shop_items_json`) VALUES
(1, 'Dược Phẩm', 'shop', 0, -4, 1.2, 'greet', 'npc_merchant_1', 1, '{\"shop_name\":\"Dược Phẩm\",\"items\":[\n  {\"item_template_id\":11,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":12,\"price_silver\":1500,\"price_gold\":0,\"stock\":-1,\"level_need\":5},\n  {\"item_template_id\":13,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":14,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":15,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":5},\n  {\"item_template_id\":16,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":121,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":122,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":161,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1}\n]}'),
(2, 'Đại Tướng Lan', 'quest', 0, -5, 4.9, 'quest_intro', 'npc_quest_1', 1, NULL),
(3, 'Thợ Rèn Hắc Long', 'blacksmith', 0, 2, 3.8, 'greet', 'npc_smith_1', 1, NULL),
(5, 'Binh Khí', 'shop', 0, 15.0086, -1.90751, 'greet', 'npc_merchant_2', 1, '{\"shop_name\":\"Binh Khí\",\"items\":[\n  {\"item_template_id\":200,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":201,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":202,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":203,\"price_silver\":50000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\n  {\"item_template_id\":204,\"price_silver\":150000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\n  {\"item_template_id\":205,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":206,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":207,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":208,\"price_silver\":50000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\n  {\"item_template_id\":209,\"price_silver\":150000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\n  {\"item_template_id\":210,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":211,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":212,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":213,\"price_silver\":50000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\n  {\"item_template_id\":214,\"price_silver\":150000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\n  {\"item_template_id\":215,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":216,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":217,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":218,\"price_silver\":50000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\n  {\"item_template_id\":219,\"price_silver\":150000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\n  {\"item_template_id\":220,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":221,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":222,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":223,\"price_silver\":50000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\n  {\"item_template_id\":224,\"price_silver\":150000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\n  {\"item_template_id\":225,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":226,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":227,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":228,\"price_silver\":50000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\n  {\"item_template_id\":229,\"price_silver\":150000,\"price_gold\":0,\"stock\":-1,\"level_need\":50}\n]}'),
(7, 'Trang bị ', 'shop', 0, 25.2086, -1.90751, 'greet', 'npc_merchant_3', 1, '{\"shop_name\":\"Trang Bị\",\"items\":[\r\n  {\"item_template_id\":100,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":101,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":102,\"price_silver\":10000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":103,\"price_silver\":35000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":104,\"price_silver\":100000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\r\n  {\"item_template_id\":105,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":106,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":107,\"price_silver\":10000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":108,\"price_silver\":35000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":109,\"price_silver\":100000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\r\n  {\"item_template_id\":110,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":111,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":112,\"price_silver\":10000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":113,\"price_silver\":35000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":114,\"price_silver\":100000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\r\n  {\"item_template_id\":115,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":116,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":117,\"price_silver\":10000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":118,\"price_silver\":35000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":119,\"price_silver\":100000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\r\n  {\"item_template_id\":130,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":131,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":132,\"price_silver\":10000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":133,\"price_silver\":35000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":134,\"price_silver\":100000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\r\n  {\"item_template_id\":135,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":136,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":137,\"price_silver\":10000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":138,\"price_silver\":35000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":139,\"price_silver\":100000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\r\n  {\"item_template_id\":140,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":141,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":142,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":143,\"price_silver\":50000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":144,\"price_silver\":150000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\r\n  {\"item_template_id\":150,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":151,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":152,\"price_silver\":10000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":153,\"price_silver\":35000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":154,\"price_silver\":100000,\"price_gold\":0,\"stock\":-1,\"level_need\":50},\r\n  {\"item_template_id\":155,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\r\n  {\"item_template_id\":156,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\r\n  {\"item_template_id\":157,\"price_silver\":10000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\r\n  {\"item_template_id\":158,\"price_silver\":35000,\"price_gold\":0,\"stock\":-1,\"level_need\":35},\r\n  {\"item_template_id\":159,\"price_silver\":100000,\"price_gold\":0,\"stock\":-1,\"level_need\":50}\r\n]}'),
(8, 'Tiên Dược', 'shop', 0, 35.0086, -1.90751, 'greet', 'npc_merchant_4', 1, '{\"shop_name\":\"Tiên Dược\",\"items\":[\n  {\"item_template_id\":121,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":122,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":123,\"price_silver\":50000,\"price_gold\":0,\"stock\":-1,\"level_need\":40},\n  {\"item_template_id\":161,\"price_silver\":8000,\"price_gold\":0,\"stock\":-1,\"level_need\":5},\n  {\"item_template_id\":162,\"price_silver\":25000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":163,\"price_silver\":80000,\"price_gold\":0,\"stock\":-1,\"level_need\":40},\n  {\"item_template_id\":171,\"price_silver\":8000,\"price_gold\":0,\"stock\":-1,\"level_need\":5},\n  {\"item_template_id\":172,\"price_silver\":25000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":173,\"price_silver\":80000,\"price_gold\":0,\"stock\":-1,\"level_need\":40}\n]}'),
(12, 'Thuong Nhan Canh Dong', 'shop', 1, 3, -1, 'greet', 'npc_merchant_1', 1, '{\"shop_name\":\"Tạp Hóa\",\"items\":[\n  {\"item_template_id\":11,\"price_silver\":500,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":12,\"price_silver\":1500,\"price_gold\":0,\"stock\":-1,\"level_need\":5},\n  {\"item_template_id\":13,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":14,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":15,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":5},\n  {\"item_template_id\":16,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":121,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":122,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":161,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1}\n]}'),
(13, 'Tho Ren Canh Dong', 'blacksmith', 1, 9, 0.5, 'greet', 'npc_smith_1', 1, NULL),
(14, 'Huong Dan Vien', 'quest', 99, 6.71102, -1.16919, 'quest_intro', 'npc_quest_1', 1, NULL),
(15, 'Thủ môn Phó Bản', 'dungeon', 0, 40, -1.90751, NULL, NULL, 1, NULL),
(999, 'Shop', 'shop', -1, 0, 0, NULL, 'shop_utility', 1, '{\"shop_name\":\"Cửa Hàng Tiện Ích\",\"items\":[\n  {\"item_template_id\":17,\"price_silver\":10,\"price_gold\":10,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":18,\"price_silver\":10,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":19,\"price_silver\":10,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":20,\"price_silver\":10,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":21,\"price_silver\":10,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":16,\"price_silver\":5000,\"price_gold\":0,\"stock\":-1,\"level_need\":10},\n  {\"item_template_id\":1,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":2,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":5},\n  {\"item_template_id\":3,\"price_silver\":8000,\"price_gold\":0,\"stock\":-1,\"level_need\":15},\n  {\"item_template_id\":8,\"price_silver\":2000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":121,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":122,\"price_silver\":40000,\"price_gold\":0,\"stock\":-1,\"level_need\":20},\n  {\"item_template_id\":47,\"price_silver\":1000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":48,\"price_silver\":3000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":49,\"price_silver\":8000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":50,\"price_silver\":2000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":51,\"price_silver\":15000,\"price_gold\":0,\"stock\":-1,\"level_need\":1},\n  {\"item_template_id\":52,\"price_silver\":40000,\"price_gold\":0,\"stock\":-1,\"level_need\":1}\n]}');

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
(13, 5, 200, 1000, 0, -1, 1),
(14, 5, 201, 5000, 0, -1, 10),
(15, 5, 202, 15000, 0, -1, 20),
(16, 5, 203, 50000, 0, -1, 35),
(17, 5, 204, 150000, 0, -1, 50),
(18, 5, 205, 1000, 0, -1, 1),
(19, 5, 206, 5000, 0, -1, 10),
(20, 5, 207, 15000, 0, -1, 20),
(21, 5, 208, 50000, 0, -1, 35),
(22, 5, 209, 150000, 0, -1, 50),
(23, 5, 210, 1000, 0, -1, 1),
(24, 5, 211, 5000, 0, -1, 10),
(25, 5, 212, 15000, 0, -1, 20),
(26, 5, 213, 50000, 0, -1, 35),
(27, 5, 214, 150000, 0, -1, 50),
(28, 5, 215, 1000, 0, -1, 1),
(29, 5, 216, 5000, 0, -1, 10),
(30, 5, 217, 15000, 0, -1, 20),
(31, 5, 218, 50000, 0, -1, 35),
(32, 5, 219, 150000, 0, -1, 50),
(33, 5, 220, 1000, 0, -1, 1),
(34, 5, 221, 5000, 0, -1, 10),
(35, 5, 222, 15000, 0, -1, 20),
(36, 5, 223, 50000, 0, -1, 35),
(37, 5, 224, 150000, 0, -1, 50),
(38, 5, 225, 1000, 0, -1, 1),
(39, 5, 226, 5000, 0, -1, 10),
(40, 5, 227, 15000, 0, -1, 20),
(41, 5, 228, 50000, 0, -1, 35),
(42, 5, 229, 150000, 0, -1, 50),
(43, 7, 100, 500, 0, -1, 1),
(44, 7, 101, 3000, 0, -1, 10),
(45, 7, 102, 10000, 0, -1, 20),
(46, 7, 103, 35000, 0, -1, 35),
(47, 7, 104, 100000, 0, -1, 50),
(48, 7, 105, 500, 0, -1, 1),
(49, 7, 106, 3000, 0, -1, 10),
(50, 7, 107, 10000, 0, -1, 20),
(51, 7, 108, 35000, 0, -1, 35),
(52, 7, 109, 100000, 0, -1, 50),
(53, 7, 110, 500, 0, -1, 1),
(54, 7, 111, 3000, 0, -1, 10),
(55, 7, 112, 10000, 0, -1, 20),
(56, 7, 113, 35000, 0, -1, 35),
(57, 7, 114, 100000, 0, -1, 50),
(58, 7, 115, 500, 0, -1, 1),
(59, 7, 116, 3000, 0, -1, 10),
(60, 7, 117, 10000, 0, -1, 20),
(61, 7, 118, 35000, 0, -1, 35),
(62, 7, 119, 100000, 0, -1, 50),
(63, 7, 130, 500, 0, -1, 1),
(64, 7, 131, 3000, 0, -1, 10),
(65, 7, 132, 10000, 0, -1, 20),
(66, 7, 133, 35000, 0, -1, 35),
(67, 7, 134, 100000, 0, -1, 50),
(68, 7, 135, 500, 0, -1, 1),
(69, 7, 136, 3000, 0, -1, 10),
(70, 7, 137, 10000, 0, -1, 20),
(71, 7, 138, 35000, 0, -1, 35),
(72, 7, 139, 100000, 0, -1, 50),
(73, 7, 150, 500, 0, -1, 1),
(74, 7, 151, 3000, 0, -1, 10),
(75, 7, 152, 10000, 0, -1, 20),
(76, 7, 153, 35000, 0, -1, 35),
(77, 7, 154, 100000, 0, -1, 50),
(78, 7, 155, 500, 0, -1, 1),
(79, 7, 156, 3000, 0, -1, 10),
(80, 7, 157, 10000, 0, -1, 20),
(81, 7, 158, 35000, 0, -1, 35),
(82, 7, 159, 100000, 0, -1, 50),
(83, 7, 140, 1000, 0, -1, 1),
(84, 7, 141, 5000, 0, -1, 10),
(85, 7, 142, 15000, 0, -1, 20),
(86, 7, 143, 50000, 0, -1, 35),
(87, 7, 144, 150000, 0, -1, 50),
(88, 8, 121, 5000, 0, -1, 1),
(89, 8, 122, 15000, 0, -1, 20),
(90, 8, 123, 50000, 0, -1, 40),
(91, 8, 161, 8000, 0, -1, 5),
(92, 8, 162, 25000, 0, -1, 20),
(93, 8, 163, 80000, 0, -1, 40),
(94, 8, 171, 8000, 0, -1, 5),
(95, 8, 172, 25000, 0, -1, 20),
(96, 8, 173, 80000, 0, -1, 40),
(97, 12, 11, 500, 0, -1, 1),
(98, 12, 12, 1500, 0, -1, 5),
(99, 12, 13, 5000, 0, -1, 10),
(100, 12, 14, 15000, 0, -1, 1),
(101, 12, 15, 15000, 0, -1, 5),
(102, 12, 16, 15000, 0, -1, 10),
(103, 12, 121, 15000, 0, -1, 1),
(104, 12, 122, 15000, 0, -1, 1),
(105, 12, 161, 15000, 0, -1, 1);

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
-- Cấu trúc bảng cho bảng `player2_data`
--

CREATE TABLE `player2_data` (
  `player_id` int(11) NOT NULL COMMENT 'FK → player_data.player_id (cùng user)',
  `character_name` varchar(50) NOT NULL DEFAULT '',
  `gender` varchar(10) NOT NULL DEFAULT 'Male',
  `info_char` longtext NOT NULL  COMMENT 'JSON InfoChar: level, exp, element_type, gene_tier, gene_exp, skills_points, potential_points, hp, mp, map_id, position...',
  `equipment` longtext NOT NULL  COMMENT 'JSON trang bị đang mặc',
  `inventory` longtext NOT NULL  COMMENT 'JSON danh sách vật phẩm túi đồ',
  `skills` longtext NOT NULL  COMMENT 'JSON danh sách skill đã học',
  `potential_stats` longtext NOT NULL  COMMENT 'JSON tiềm năng đã phân bổ',
  `active_buffs` longtext NOT NULL  COMMENT 'JSON buff đang active',
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Dữ liệu nhân vật hệ gene thứ 2 — dùng chung player_id với player_data';

--
-- Đang đổ dữ liệu cho bảng `player2_data`
--

INSERT INTO `player2_data` (`player_id`, `character_name`, `gender`, `info_char`, `equipment`, `inventory`, `skills`, `potential_stats`, `active_buffs`, `updated_at`) VALUES
(16, 'thuy123', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Metal\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":\"Wind\",\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"bag_equipped_items\":[],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":false,\"attendance_count\":0,\"last_attendance_date\":\"\",\"quest_completed_count\":0,\"dungeon_best_waves\":{},\"active_quest_id\":-1,\"quest_step\":0,\"quest_progress\":{},\"completed_quests\":[]}', '{}', '[]', '[{\"skill_id\":20,\"current_level\":1}]', '{\"attack\":0,\"hp\":0,\"mp\":0,\"defense\":0,\"gene\":0}', '[]', '2026-05-17 06:54:25'),
(17, 'kim123', 'Female', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Wind\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":\"Metal\",\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"bag_equipped_items\":[],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":false,\"attendance_count\":0,\"last_attendance_date\":\"\",\"quest_completed_count\":0,\"dungeon_best_waves\":{},\"active_quest_id\":-1,\"quest_step\":0,\"quest_progress\":{},\"completed_quests\":[]}', '{}', '[]', '[{\"skill_id\":9,\"current_level\":1}]', '{\"attack\":0,\"hp\":0,\"mp\":0,\"defense\":0,\"gene\":0}', '[]', '2026-05-20 21:00:04');

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
  `active_buffs` longtext NOT NULL  COMMENT 'JSON array các buff đang active'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `player_data`
--

INSERT INTO `player_data` (`player_id`, `character_name`, `gender`, `info_char`, `equipment`, `inventory`, `skills`, `potential_stats`, `updated_at`, `active_buffs`) VALUES
(16, 'Phong', 'Female', '{\"level\":100,\"experience\":3700,\"gold\":14990,\"silver\":398400000,\"skill_points\":300,\"potential_points\":0,\"element_type\":\"Wind\",\"gene_tier\":5,\"gene_exp\":1000000,\"is_hybrid\":true,\"secondary_element\":\"Metal\",\"secondary_gene_tier\":5,\"secondary_gene_exp\":0,\"hybrid_element_a\":\"Wind\",\"hybrid_element_b\":\"Metal\",\"hybrid_bonus_targets\":\"Wood,Fire\",\"hybrid_immune_elements\":\"Fire,Earth\",\"hybrid_atk_bonus_pct\":0.5,\"hybrid_id\":13,\"hybrid_prefab_path\":\"Prefabs/Player/Hybrid/Hybrid_Metal_Wind\",\"hp\":2335,\"max_hp\":2335,\"mp\":566,\"max_mp\":566,\"attack\":760,\"defense\":200,\"bag_slots\":35,\"bag_equipped_items\":[{\"quick_slot_index\":2,\"item_template_id\":63,\"item_code\":\"\",\"item_name\":\"T\\u00FAi M\\u1EDF R\\u1ED9ng C\\u1EA5p 3\",\"icon_id\":285,\"upgrade_level\":0,\"str_options\":\"\",\"slot_bonus\":5,\"is_locked\":false},{\"quick_slot_index\":1,\"item_template_id\":61,\"item_code\":\"\",\"item_name\":\"T\\u00FAi M\\u1EDF R\\u1ED9ng C\\u1EA5p 1\",\"icon_id\":283,\"upgrade_level\":0,\"str_options\":\"\",\"slot_bonus\":5,\"is_locked\":false},{\"quick_slot_index\":0,\"item_template_id\":64,\"item_code\":\"\",\"item_name\":\"T\\u00FAi M\\u1EDF R\\u1ED9ng C\\u1EA5p 4\",\"icon_id\":774,\"upgrade_level\":0,\"str_options\":\"\",\"slot_bonus\":5,\"is_locked\":false}],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":true,\"attendance_count\":5,\"last_attendance_date\":\"2026-05-20\",\"quest_completed_count\":0,\"dungeon_best_waves\":{},\"active_quest_id\":0,\"quest_step\":0,\"quest_progress\":{},\"completed_quests\":[]}', '{\"weapon\":{\"itemTemplateId\":203,\"itemCode\":\"\",\"iconId\":\"171\",\"itemName\":\"Ki\\u1EBFm H\\u1ECFa Th\\u1EA7n\",\"itemType\":1,\"upgradeLevel\":1,\"strOptions\":\"\"},\"helmet\":{\"itemTemplateId\":100,\"itemCode\":\"\",\"iconId\":\"118\",\"itemName\":\"M\\u0169 Da Nam\",\"itemType\":0,\"upgradeLevel\":0,\"strOptions\":\"3,30\"},\"armor\":null,\"pants\":null,\"boots\":null,\"accessory\":{\"itemTemplateId\":141,\"itemCode\":\"\",\"iconId\":\"114\",\"itemName\":\"Nh\\u1EABn B\\u1EA1c\",\"itemType\":5,\"upgradeLevel\":8,\"strOptions\":\"\"}}', '[{\"slotIndex\":0,\"itemTemplateId\":200,\"quantity\":1,\"upgradeLevel\":16,\"strOptions\":\"1,12\"},{\"slotIndex\":1,\"itemTemplateId\":11,\"strOptions\":\"\",\"quantity\":17,\"upgradeLevel\":0},{\"slotIndex\":2,\"itemTemplateId\":29,\"quantity\":73,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":3,\"itemTemplateId\":2,\"quantity\":46,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":4,\"itemTemplateId\":26,\"quantity\":130,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":5,\"itemTemplateId\":1,\"quantity\":22,\"upgradeLevel\":0,\"strOptions\":\"\",\"amount\":18},{\"slotIndex\":6,\"itemTemplateId\":27,\"quantity\":98,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":7,\"itemTemplateId\":62,\"quantity\":1,\"upgradeLevel\":0,\"strOptions\":\"\",\"isLocked\":false},{\"slotIndex\":8,\"itemTemplateId\":410,\"quantity\":8,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":9,\"itemTemplateId\":61,\"quantity\":99,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":10,\"itemTemplateId\":37,\"quantity\":44,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":11,\"itemTemplateId\":12,\"strOptions\":\"\",\"quantity\":11,\"upgradeLevel\":0},{\"slotIndex\":12,\"itemTemplateId\":25,\"quantity\":2,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":13,\"itemTemplateId\":52,\"quantity\":8,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":14,\"itemTemplateId\":31,\"quantity\":19,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":15,\"itemTemplateId\":61,\"quantity\":1,\"upgradeLevel\":0,\"strOptions\":\"\",\"isLocked\":false},{\"slotIndex\":16,\"itemTemplateId\":229,\"quantity\":1,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":17,\"itemTemplateId\":107,\"quantity\":1,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":18,\"itemTemplateId\":140,\"quantity\":1,\"upgradeLevel\":0,\"strOptions\":\"3,30\"},{\"slotIndex\":19,\"itemTemplateId\":21,\"quantity\":2,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":20,\"itemTemplateId\":17,\"strOptions\":\"\",\"quantity\":1,\"upgradeLevel\":0},{\"slotIndex\":21,\"itemTemplateId\":200,\"quantity\":1,\"upgradeLevel\":4,\"strOptions\":\"1,10\"}]', '[]', '{\"attack\":505,\"hp\":0,\"mp\":0,\"defense\":0,\"gene\":0}', '2026-05-20 22:49:28', '[{\"effectType\":\"HpRestoreOverTime\",\"value\":500,\"iconId\":532,\"name\":\"H\\u1ED3i m\\u00E1u\",\"detail\":\"\\u002B500 HP/s trong 30 gi\\u00E2y\",\"expireAt\":\"2026-05-19T01:55:02.6678494Z\"}]'),
(17, 'kim', 'Male', '{\"level\":5,\"experience\":1400,\"gold\":1999903800,\"silver\":699410470,\"skill_points\":5,\"potential_points\":25,\"element_type\":\"Metal\",\"gene_tier\":5,\"gene_exp\":10000,\"is_hybrid\":false,\"secondary_element\":\"Wind\",\"secondary_gene_tier\":3,\"secondary_gene_exp\":28000,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":585,\"max_hp\":585,\"mp\":250,\"max_mp\":250,\"attack\":63,\"defense\":22,\"bag_slots\":20,\"bag_equipped_items\":[],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":false,\"attendance_count\":1,\"last_attendance_date\":\"2026-05-20\",\"quest_completed_count\":2,\"dungeon_best_waves\":{},\"active_quest_id\":3,\"quest_step\":1,\"quest_progress\":{\"0\":1},\"completed_quests\":[1,2]}', '{\"weapon\":{\"itemTemplateId\":200,\"itemCode\":\"Ki\\u1EBFm H\\u1ECFa S\\u01A1 C\\u1EA5p\",\"iconId\":\"168\",\"itemName\":\"Ki\\u1EBFm H\\u1ECFa S\\u01A1 C\\u1EA5p\",\"itemType\":1,\"upgradeLevel\":0,\"strOptions\":\"1,10\"},\"helmet\":null,\"armor\":null,\"pants\":null,\"boots\":null,\"accessory\":null}', '[{\"slotIndex\":0,\"itemTemplateId\":161,\"strOptions\":\"\",\"quantity\":2,\"upgradeLevel\":0},{\"slotIndex\":1,\"itemTemplateId\":11,\"strOptions\":\"\",\"quantity\":4,\"upgradeLevel\":0},{\"slotIndex\":2,\"itemTemplateId\":122,\"strOptions\":\"\",\"quantity\":2,\"upgradeLevel\":0},{\"slotIndex\":3,\"itemTemplateId\":121,\"strOptions\":\"\",\"quantity\":6,\"upgradeLevel\":0},{\"slotIndex\":4,\"itemTemplateId\":17,\"strOptions\":\"\",\"quantity\":19,\"upgradeLevel\":0},{\"slotIndex\":5,\"itemTemplateId\":20,\"strOptions\":\"\",\"quantity\":19,\"upgradeLevel\":0},{\"slotIndex\":6,\"itemTemplateId\":47,\"strOptions\":\"\",\"quantity\":1,\"upgradeLevel\":0},{\"slotIndex\":7,\"itemTemplateId\":52,\"strOptions\":\"\",\"quantity\":9,\"upgradeLevel\":0},{\"slotIndex\":8,\"itemTemplateId\":50,\"strOptions\":\"\",\"quantity\":14,\"upgradeLevel\":0},{\"slotIndex\":9,\"itemTemplateId\":18,\"strOptions\":\"\",\"quantity\":15,\"upgradeLevel\":0},{\"slotIndex\":10,\"itemTemplateId\":19,\"strOptions\":\"\",\"quantity\":9,\"upgradeLevel\":0},{\"slotIndex\":11,\"itemTemplateId\":27,\"quantity\":4,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":12,\"itemTemplateId\":1,\"quantity\":6,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":13,\"itemTemplateId\":21,\"itemCode\":\"Tinh Ch\\u1EA5t \",\"iconId\":\"289\",\"quantity\":2,\"isEquipped\":false,\"upgradeLevel\":0}]', '[]', '{}', '2026-05-20 21:07:16', '[]'),
(18, 'Hoa', 'Male', '{\"level\":1,\"experience\":0,\"gold\":100000000,\"silver\":1000000000,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Fire\",\"gene_tier\":5,\"gene_exp\":1000000000,\"is_hybrid\":true,\"secondary_element\":\"Earth\",\"secondary_gene_tier\":5,\"secondary_gene_exp\":0,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"bag_equipped_items\":[],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":false,\"attendance_count\":2,\"last_attendance_date\":\"2026-05-19\",\"quest_completed_count\":0,\"dungeon_best_waves\":{},\"active_quest_id\":-1,\"quest_step\":0,\"quest_progress\":{},\"completed_quests\":[]}', '{}', '[{\"slotIndex\":0,\"itemTemplateId\":27,\"quantity\":1,\"upgradeLevel\":0,\"strOptions\":\"\"}]', '[]', '{}', '2026-05-19 01:49:46', '[]'),
(19, 'Thuy', 'Female', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Water\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":true,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"bag_equipped_items\":[],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":false,\"attendance_count\":1,\"last_attendance_date\":\"2026-05-19\",\"quest_completed_count\":0,\"dungeon_best_waves\":{},\"active_quest_id\":-1,\"quest_step\":0,\"quest_progress\":{},\"completed_quests\":[]}', '{}', '[]', '[]', '{}', '2026-05-19 00:19:45', '[]'),
(20, 'thuy123', 'Female', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Water\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"bag_equipped_items\":[],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":false,\"attendance_count\":1,\"last_attendance_date\":\"2026-05-19\",\"quest_completed_count\":0,\"dungeon_best_waves\":{},\"active_quest_id\":1,\"quest_step\":0,\"quest_progress\":{},\"completed_quests\":[]}', '{}', '[{\"slotIndex\":0,\"itemTemplateId\":27,\"quantity\":2,\"upgradeLevel\":0,\"strOptions\":\"\"},{\"slotIndex\":1,\"itemTemplateId\":1,\"quantity\":1,\"upgradeLevel\":0,\"strOptions\":\"\"}]', '[]', '{}', '2026-05-19 00:21:27', '[]'),
(21, 'phong1', 'Female', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Wind\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"bag_equipped_items\":[],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":false,\"attendance_count\":0,\"last_attendance_date\":\"\",\"quest_completed_count\":0,\"dungeon_best_waves\":{},\"active_quest_id\":-1,\"quest_step\":0,\"quest_progress\":{},\"completed_quests\":[]}', '{}', '[]', '[]', '{}', '2026-05-19 00:48:12', '[]'),
(22, 'hoa123', 'Male', '{\"level\":1,\"experience\":0,\"gold\":0,\"silver\":0,\"skill_points\":0,\"potential_points\":5,\"element_type\":\"Fire\",\"gene_tier\":1,\"gene_exp\":0,\"is_hybrid\":false,\"secondary_element\":null,\"secondary_gene_tier\":null,\"secondary_gene_exp\":null,\"hybrid_element_a\":null,\"hybrid_element_b\":null,\"hybrid_bonus_targets\":null,\"hybrid_immune_elements\":null,\"hybrid_atk_bonus_pct\":0,\"hybrid_id\":null,\"hybrid_prefab_path\":null,\"hp\":100,\"max_hp\":100,\"mp\":50,\"max_mp\":50,\"attack\":10,\"defense\":0,\"bag_slots\":20,\"bag_equipped_items\":[],\"map_id\":0,\"zone_id\":0,\"position_x\":0,\"position_y\":0,\"daily_wave_entries\":0,\"daily_wave_date\":\"\",\"is_level_locked\":false,\"attendance_count\":2,\"last_attendance_date\":\"2026-05-20\",\"quest_completed_count\":0,\"dungeon_best_waves\":{},\"active_quest_id\":-1,\"quest_step\":0,\"quest_progress\":{},\"completed_quests\":[]}', '{}', '[]', '[]', '{}', '2026-05-20 22:03:47', '[]');

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
-- Cấu trúc bảng cho bảng `quest_config`
--

CREATE TABLE `quest_config` (
  `id` int(11) NOT NULL,
  `name` varchar(200) NOT NULL DEFAULT '',
  `level_need` int(11) NOT NULL DEFAULT 1,
  `npc_id` int(11) NOT NULL DEFAULT 0 COMMENT 'NPC nhận và giao nhiệm vụ (cùng 1 NPC)',
  `str1` text NOT NULL COMMENT 'Hội thoại khi nhận nhiệm vụ',
  `str2` text NOT NULL COMMENT 'Hội thoại khi nộp/hoàn thành nhiệm vụ',
  `str3` text NOT NULL COMMENT 'Ghi chú / hướng dẫn cho người chơi',
  `exp_reward` int(11) NOT NULL DEFAULT 0,
  `gold_reward` int(11) NOT NULL DEFAULT 0,
  `silver_reward` int(11) NOT NULL DEFAULT 0,
  `item_reward` varchar(500) NOT NULL DEFAULT '' COMMENT 'Format: itemId@quantity,itemId@quantity',
  `step` longtext NOT NULL COMMENT 'JSON steps: [{id,name,idMob,idNpc,idItem,idMap,x,y,require,STR}]',
  `sort_order` int(11) NOT NULL DEFAULT 0,
  `is_active` tinyint(1) NOT NULL DEFAULT 1
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

--
-- Đang đổ dữ liệu cho bảng `quest_config`
--

INSERT INTO `quest_config` (`id`, `name`, `level_need`, `npc_id`, `str1`, `str2`, `str3`, `exp_reward`, `gold_reward`, `silver_reward`, `item_reward`, `step`, `sort_order`, `is_active`) VALUES
(1, 'Dọn sạch bãi tập', 1, 2, 'Hoan nghênh, dũng sĩ! Ta là Đại Tướng Lan. Trước khi nhận trọng trách lớn hơn, hãy quét sạch 5 con Slime quanh bãi tập của làng.', 'Tốt lắm. Ngươi đã chứng minh mình không ngại bắt đầu từ việc nhỏ.', 'Ghi chú: Slime xuất hiện quanh Làng Khởi Đầu, rất gần chỗ của ta.', 500, 0, 100, '11@2', '[{\"id\":0,\"name\":\"Tiêu diệt Slime\",\"idMob\":1,\"idNpc\":-1,\"idItem\":-1,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":5,\"STR\":\"\"}]', 1, 1),
(2, 'Thu gom thảo dược', 3, 2, 'Đám Slime gần làng làm rơi nhiều thảo dược hữu ích. Hãy thu thập cho ta 3 bó Thảo Dược để quân y pha chế thuốc.', 'Chính xác. Đây là loại dược liệu ta đang cần.', 'Ghi chú: Thảo Dược rơi khi ngươi chiến đấu quanh khu vực Slime.', 900, 0, 200, '17@2', '[{\"id\":1,\"name\":\"Thu thập Thảo Dược\",\"idMob\":-1,\"idNpc\":-1,\"idItem\":27,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":3,\"STR\":\"\"}]', 2, 1),
(3, 'Liên lạc với Hướng Dẫn Viên', 5, 2, 'Goblin đã bắt đầu lảng vảng ở cánh đồng phía trước. Hãy gặp Hướng Dẫn Viên ở bản đồ 1 để nhận chỉ dẫn, rồi tiêu diệt 8 con Goblin tại đó.', 'Tốt. Báo cáo của ngươi giúp ta nắm rõ tình hình bên ngoài làng.', 'Ghi chú: Hướng Dẫn Viên có npc_id=14 ở map 1.', 1500, 50, 350, '14@2', '[{\"id\":5,\"name\":\"Nói chuyện với Hướng Dẫn Viên\",\"idMob\":-1,\"idNpc\":14,\"idItem\":-1,\"idMap\":1,\"x\":0,\"y\":0,\"require\":1,\"STR\":\"14@Ta đã nhận được tin từ Đại Tướng Lan. Hãy giúp ta dọn bớt Goblin quanh cánh đồng.\"},{\"id\":0,\"name\":\"Tiêu diệt Goblin do thám\",\"idMob\":2,\"idNpc\":-1,\"idItem\":-1,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":8,\"STR\":\"\"}]', 3, 1),
(4, 'Quét sạch cánh đồng', 6, 14, 'Cánh đồng vẫn chưa an toàn. Hãy tiêu diệt thêm 12 con Goblin để dân làng có thể đi qua khu vực này.', 'Rất tốt. Cánh đồng đã bớt nguy hiểm hơn nhiều.', 'Ghi chú: Goblin tập trung đông quanh map 1.', 2200, 70, 450, '17@3', '[{\"id\":0,\"name\":\"Tiêu diệt Goblin ở cánh đồng\",\"idMob\":2,\"idNpc\":-1,\"idItem\":-1,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":12,\"STR\":\"\"}]', 1, 1),
(5, 'Thu thập tinh thể lửa', 8, 14, 'Trong lúc tuần tra, quân trinh sát phát hiện nhiều Tinh Thể Lửa rải rác ở cánh đồng. Hãy thu thập cho ta 4 khối để nghiên cứu.', 'Làm tốt lắm. Chúng sẽ rất hữu ích cho việc chế tạo.', 'Ghi chú: Tinh Thể Lửa có item_id=30.', 2800, 100, 600, '12@2', '[{\"id\":1,\"name\":\"Thu thập Tinh Thể Lửa\",\"idMob\":-1,\"idNpc\":-1,\"idItem\":30,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":4,\"STR\":\"\"}]', 2, 1),
(6, 'Mở đường tới Cửa Phía Đông', 10, 14, 'Tuyến đường ra Cửa Phía Đông đang bị chặn. Hãy tiến đến map 100 rồi hạ 6 Orc Warrior canh giữ nơi đó.', 'Tuyệt vời. Tuyến đường tiếp tế đã được khai thông.', 'Ghi chú: Bước đầu là đến map 100, sau đó tiêu diệt Orc Warrior.', 3600, 120, 800, '18@2', '[{\"id\":9,\"name\":\"Tiến đến Cửa Phía Đông\",\"idMob\":-1,\"idNpc\":-1,\"idItem\":-1,\"idMap\":100,\"x\":0,\"y\":0,\"require\":1,\"STR\":\"\"},{\"id\":0,\"name\":\"Tiêu diệt Orc Warrior\",\"idMob\":3,\"idNpc\":-1,\"idItem\":-1,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":6,\"STR\":\"\"}]', 3, 1),
(7, 'Kiếm quặng cho thợ rèn', 12, 14, 'Đám Orc mang theo rất nhiều quặng. Hãy thu thập 5 khối Quặng Sắt để thợ rèn gia cố trang bị cho quân lính.', 'Tốt. Số quặng này đủ để chuẩn bị cho đợt rèn tiếp theo.', 'Ghi chú: Quặng Sắt có item_id=26 và rơi từ Orc Warrior.', 4600, 150, 1000, '18@3', '[{\"id\":1,\"name\":\"Thu thập Quặng Sắt\",\"idMob\":-1,\"idNpc\":-1,\"idItem\":26,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":5,\"STR\":\"\"}]', 4, 1),
(8, 'Thu gom đá rèn cấp cao', 14, 14, 'Một số Orc mang theo Đá Nâng Cấp Cấp 2. Hãy đem về cho ta 4 viên để dùng cho tuyến sau.', 'Rất tốt. Vật tư nâng cấp đã về tới doanh trại.', 'Ghi chú: Đá Nâng Cấp Cấp 2 có item_id=2.', 5800, 180, 1300, '19@1', '[{\"id\":1,\"name\":\"Thu thập Đá Nâng Cấp Cấp 2\",\"idMob\":-1,\"idNpc\":-1,\"idItem\":2,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":4,\"STR\":\"\"}]', 5, 1),
(9, 'Trấn áp chiến binh Orc', 16, 14, 'Số lượng Orc Warrior ngoài tiền tuyến đang tăng mạnh. Ta cần ngươi hạ 15 tên để giữ thế chủ động.', 'Tình hình đã ổn định hơn. Ngươi làm rất tốt.', 'Ghi chú: Orc Warrior có enemy_id=3 tại khu vực map 100.', 7200, 220, 1600, '20@1', '[{\"id\":0,\"name\":\"Tiêu diệt Orc Warrior tiền tuyến\",\"idMob\":3,\"idNpc\":-1,\"idItem\":-1,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":15,\"STR\":\"\"}]', 6, 1),
(10, 'Tích trữ nguyên liệu gene', 18, 14, 'Ta đang chuẩn bị một đợt nghiên cứu gene quy mô lớn. Hãy gom 3 khối Quặng Sắt rồi quay lại cánh đồng thu thêm 3 Tinh Thể Lửa.', 'Hoàn hảo. Đống nguyên liệu này đủ để bắt đầu mẻ nghiên cứu mới.', 'Ghi chú: Hoàn thành theo thứ tự: Quặng Sắt trước, Tinh Thể Lửa sau.', 9000, 300, 2000, '17@5,18@3', '[{\"id\":1,\"name\":\"Thu thập Quặng Sắt\",\"idMob\":-1,\"idNpc\":-1,\"idItem\":26,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":3,\"STR\":\"\"},{\"id\":1,\"name\":\"Thu thập Tinh Thể Lửa\",\"idMob\":-1,\"idNpc\":-1,\"idItem\":30,\"idMap\":-1,\"x\":0,\"y\":0,\"require\":3,\"STR\":\"\"}]', 7, 1);

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
(16, 'phong', 'phong@gmail.com', '$2a$12$IVR2P43G/o.2px.QU691Qe0gsZzuYZoq0QVaKJtRgHCQOk.JrcYbO', '2026-04-01 19:08:48', '2026-05-20 22:47:17'),
(17, 'kim', 'kim@gmail.com', '$2a$12$G1hEIuasIWxnsJsYm4g.YexoQdX2lV5rucvhH04mRlGJ3Vd4KDkTy', '2026-04-01 19:29:09', '2026-05-20 21:06:31'),
(18, 'hoa', '123456@gmail.com', '$2a$12$4yyKm9g2cka5cE.4SFlTceYwBq.Lb5EIMOjYB0Lc88sy.qdk3rLcy', '2026-04-17 13:38:03', '2026-05-19 01:47:12'),
(19, 'thuy', 'fl2k3xb@gmail.com', '$2a$12$Y8UiEeKhlpXpBkhaOlhiLeiCBKtiBX163kv7xOJSHmEY27Lh443dC', '2026-05-07 22:41:26', '2026-05-19 00:19:00'),
(20, 'thuy1', 'thuy@gmail.com', '$2a$12$h0OsA2V8CZ6KTH8MgOtFIOzNtjRqTrO.g6pJjk5dcXrwjViZhhOFS', '2026-05-18 23:11:04', '2026-05-19 00:19:56'),
(21, 'phong1', 'phong1@gmail.com', '$2a$12$PG1mWknG1wv3ZF2gADLS8O/Efc9Jdv74.5MkL3.tC6a4h1cxLEOFe', '2026-05-19 00:47:44', '2026-05-19 00:47:54'),
(22, 'hoa1', 'hoa2@gmail.com', '$2a$12$iUwWxus1/tYT/fo/7CCDEeM6PpusFeW/8kCt4iwhASkmlozY11B7a', '2026-05-19 01:46:42', '2026-05-20 22:02:00');

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
-- Chỉ mục cho bảng `dungeon_wave_config`
--
ALTER TABLE `dungeon_wave_config`
  ADD PRIMARY KEY (`dungeon_id`);

--
-- Chỉ mục cho bảng `enemy`
--
ALTER TABLE `enemy`
  ADD PRIMARY KEY (`enemy_id`),
  ADD KEY `idx_enemy_level` (`level`),
  ADD KEY `idx_enemy_element` (`element_type`);

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
-- Chỉ mục cho bảng `friend_relations`
--
ALTER TABLE `friend_relations`
  ADD PRIMARY KEY (`id`),
  ADD UNIQUE KEY `uq_friend_pair` (`user_id`,`friend_id`),
  ADD KEY `idx_friend_id` (`friend_id`);

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
-- Chỉ mục cho bảng `leaderboard_cache`
--
ALTER TABLE `leaderboard_cache`
  ADD PRIMARY KEY (`id`);

--
-- Chỉ mục cho bảng `leaderboard_caches`
--
ALTER TABLE `leaderboard_caches`
  ADD PRIMARY KEY (`Id`),
  ADD KEY `idx_id` (`Id`);

--
-- Chỉ mục cho bảng `map_config`
--
ALTER TABLE `map_config`
  ADD PRIMARY KEY (`map_id`);

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
-- Chỉ mục cho bảng `player2_data`
--
ALTER TABLE `player2_data`
  ADD PRIMARY KEY (`player_id`);

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
-- Chỉ mục cho bảng `quest_config`
--
ALTER TABLE `quest_config`
  ADD PRIMARY KEY (`id`);

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
  MODIFY `dungeon_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=8;

--
-- AUTO_INCREMENT cho bảng `enemy`
--
ALTER TABLE `enemy`
  MODIFY `enemy_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=18;

--
-- AUTO_INCREMENT cho bảng `friend_relations`
--
ALTER TABLE `friend_relations`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

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
  MODIFY `id` int(11) UNSIGNED NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=411;

--
-- AUTO_INCREMENT cho bảng `leaderboard_caches`
--
ALTER TABLE `leaderboard_caches`
  MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT cho bảng `map_portal`
--
ALTER TABLE `map_portal`
  MODIFY `portal_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=114;

--
-- AUTO_INCREMENT cho bảng `map_spawn_config`
--
ALTER TABLE `map_spawn_config`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=25;

--
-- AUTO_INCREMENT cho bảng `npc_config`
--
ALTER TABLE `npc_config`
  MODIFY `npc_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=1000;

--
-- AUTO_INCREMENT cho bảng `npc_dialogue`
--
ALTER TABLE `npc_dialogue`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=6;

--
-- AUTO_INCREMENT cho bảng `npc_shop_item`
--
ALTER TABLE `npc_shop_item`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=106;

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
-- AUTO_INCREMENT cho bảng `quest_config`
--
ALTER TABLE `quest_config`
  MODIFY `id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=11;

--
-- AUTO_INCREMENT cho bảng `skill_template`
--
ALTER TABLE `skill_template`
  MODIFY `skill_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=42;

--
-- AUTO_INCREMENT cho bảng `users`
--
ALTER TABLE `users`
  MODIFY `user_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=23;

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
-- Các ràng buộc cho bảng `friend_relations`
--
ALTER TABLE `friend_relations`
  ADD CONSTRAINT `fk_fr_friend` FOREIGN KEY (`friend_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE,
  ADD CONSTRAINT `fk_fr_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`user_id`) ON DELETE CASCADE;

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
-- Các ràng buộc cho bảng `player2_data`
--
ALTER TABLE `player2_data`
  ADD CONSTRAINT `fk_p2d_player` FOREIGN KEY (`player_id`) REFERENCES `player_data` (`player_id`) ON DELETE CASCADE ON UPDATE CASCADE;

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
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
