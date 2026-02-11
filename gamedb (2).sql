-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Máy chủ: 127.0.0.1
-- Thời gian đã tạo: Th2 11, 2026 lúc 10:35 PM
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
  `drop_items_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON drop items',
  `element_type` varchar(10) DEFAULT NULL,
  `enemy_type` varchar(20) DEFAULT NULL COMMENT 'Normal, Elite, Boss',
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `enemy`
--

INSERT INTO `enemy` (`enemy_id`, `enemy_name`, `enemy_description`, `level`, `base_hp`, `base_mp`, `base_damage`, `base_defense`, `move_speed`, `attack_speed`, `exp_reward`, `gold_reward`, `drop_items_json`, `element_type`, `enemy_type`, `created_at`, `updated_at`) VALUES
(1, 'Slime', 'Quái yếu nhưng đông', 1, 50, 0, 5, 0, 1.5, 1, 10, 5, '[]', NULL, 'Normal', '2026-02-08 19:31:43', '2026-02-08 19:31:43'),
(2, 'Goblin', 'Nhanh nhẹn nhưng yếu', 2, 80, 0, 8, 2, 2.5, 1.2, 20, 10, '[]', NULL, 'Normal', '2026-02-08 19:31:43', '2026-02-08 19:31:43'),
(3, 'Orc Warrior', 'Orc có giáp', 3, 150, 0, 15, 5, 2, 1, 50, 25, '[]', NULL, 'Normal', '2026-02-08 19:31:43', '2026-02-08 19:31:43'),
(4, 'Fire Slime', 'Slime hệ Fire', 2, 70, 20, 8, 0, 1.5, 1, 15, 8, '[]', 'Fire', 'Normal', '2026-02-08 19:31:43', '2026-02-08 19:31:43'),
(5, 'Boss Dragon', 'Rồng Boss cực mạnh', 10, 1000, 200, 80, 20, 3, 2, 500, 200, '[]', 'Fire', 'Boss', '2026-02-08 19:31:43', '2026-02-08 19:31:43');

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
  `respawn_time` int(11) NOT NULL DEFAULT 30,
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `enemy_spawns`
--

INSERT INTO `enemy_spawns` (`spawn_id`, `map_id`, `enemy_type_id`, `spawn_x`, `spawn_y`, `max_spawn_count`, `respawn_time`, `created_at`, `updated_at`) VALUES
(1, 0, 1, 10, 0, 3, 30, '2026-02-08 19:31:43', '2026-02-08 19:31:43'),
(2, 0, 1, -10, 0, 3, 30, '2026-02-08 19:31:43', '2026-02-08 19:31:43'),
(3, 0, 2, 20, 0, 2, 45, '2026-02-08 19:31:43', '2026-02-08 19:31:43'),
(4, 0, 3, 25, 0, 1, 60, '2026-02-08 19:31:43', '2026-02-08 19:31:43'),
(5, 0, 5, 30, 5, 1, 120, '2026-02-08 19:31:43', '2026-02-08 19:31:43');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `exp_requirements`
--

CREATE TABLE `exp_requirements` (
  `level` int(11) NOT NULL,
  `exp_required` int(11) NOT NULL COMMENT 'Exp cần để lên level này',
  `base_stat_increase` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: {hp,mp,attack}',
  `skill_points` int(11) NOT NULL DEFAULT 0,
  `potential_points` int(11) NOT NULL DEFAULT 0,
  `created_at` datetime NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `exp_requirements`
--

INSERT INTO `exp_requirements` (`level`, `exp_required`, `base_stat_increase`, `skill_points`, `potential_points`, `created_at`) VALUES
(1, 0, '{\"hp\":0,\"mp\":0,\"attack\":0}', 0, 0, '2026-02-08 19:31:42'),
(2, 100, '{\"hp\":50,\"mp\":20,\"attack\":5}', 1, 2, '2026-02-08 19:31:42'),
(3, 300, '{\"hp\":80,\"mp\":30,\"attack\":10}', 1, 2, '2026-02-08 19:31:42'),
(4, 600, '{\"hp\":100,\"mp\":40,\"attack\":15}', 1, 2, '2026-02-08 19:31:42'),
(5, 1000, '{\"hp\":120,\"mp\":50,\"attack\":20}', 2, 3, '2026-02-08 19:31:42');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `item_template`
--

CREATE TABLE `item_template` (
  `id` int(11) NOT NULL,
  `code` varchar(50) NOT NULL,
  `name` varchar(100) NOT NULL,
  `description` text DEFAULT NULL,
  `category` tinyint(4) NOT NULL,
  `item_type` tinyint(4) NOT NULL,
  `stackable` tinyint(1) DEFAULT 1,
  `max_stack` int(11) DEFAULT 99,
  `gender_limit` tinyint(4) DEFAULT 0,
  `class_limit` int(11) DEFAULT 0,
  `level_required` int(11) DEFAULT 0,
  `rarity` tinyint(4) DEFAULT 1,
  `icon_path` varchar(255) DEFAULT NULL,
  `prefab_path` varchar(255) DEFAULT NULL,
  `base_stat_json` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL CHECK (json_valid(`base_stat_json`)),
  `created_at` timestamp NOT NULL DEFAULT current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `map_config`
--

CREATE TABLE `map_config` (
  `map_id` int(11) NOT NULL,
  `map_name` varchar(100) NOT NULL,
  `spawn_points_json` text NOT NULL COMMENT 'JSON: [{x,y}]',
  `created_at` datetime DEFAULT current_timestamp(),
  `updated_at` datetime DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `map_config`
--

INSERT INTO `map_config` (`map_id`, `map_name`, `spawn_points_json`, `created_at`, `updated_at`) VALUES
(0, 'Main Map', '[{\"x\":0,\"y\":0},{\"x\":5,\"y\":0},{\"x\":-5,\"y\":0},{\"x\":0,\"y\":5}]', '2026-02-08 19:31:43', '2026-02-08 19:31:43');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `player_data`
--

CREATE TABLE `player_data` (
  `player_id` int(11) NOT NULL,
  `level` int(11) NOT NULL DEFAULT 1,
  `experience` int(11) NOT NULL DEFAULT 0,
  `gold` int(11) NOT NULL DEFAULT 0,
  `map_id` int(11) NOT NULL DEFAULT 0 COMMENT '0 = Main map',
  `position_x` float NOT NULL DEFAULT 0 COMMENT 'Vị trí X khi logout',
  `position_y` float NOT NULL DEFAULT 0 COMMENT 'Vị trí Y khi logout',
  `hp` int(11) NOT NULL DEFAULT 100,
  `max_hp` int(11) NOT NULL DEFAULT 100,
  `mp` int(11) NOT NULL DEFAULT 50,
  `max_mp` int(11) NOT NULL DEFAULT 50,
  `attack` int(11) NOT NULL DEFAULT 10,
  `element_type` varchar(10) NOT NULL COMMENT 'Fire, Water, Earth, Wood, Metal',
  `gene_tier` tinyint(4) NOT NULL DEFAULT 1,
  `is_hybrid` tinyint(1) NOT NULL DEFAULT 0,
  `secondary_element` varchar(10) DEFAULT NULL,
  `gender` varchar(10) NOT NULL DEFAULT 'Male',
  `character_name` varchar(50) NOT NULL DEFAULT '',
  `equipment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: Trang bị đang mặc',
  `skills` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: Skills đã học',
  `inventory` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: Túi đồ',
  `potential_stats` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: Chỉ số tiềm năng',
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `player_data`
--

INSERT INTO `player_data` (`player_id`, `level`, `experience`, `gold`, `map_id`, `position_x`, `position_y`, `hp`, `max_hp`, `mp`, `max_mp`, `attack`, `element_type`, `gene_tier`, `is_hybrid`, `secondary_element`, `gender`, `character_name`, `equipment`, `skills`, `inventory`, `potential_stats`, `updated_at`) VALUES
(1, 1, 0, 0, 0, -0.55167, -3.33882, 100, 100, 50, 50, 10, 'Metal', 1, 0, NULL, 'Male', '12312', '{}', '[]', '[]', '[]', '2026-02-11 21:29:43'),
(2, 1, 0, 0, 0, 0.395421, -3.33882, 100, 100, 50, 50, 10, 'Fire', 1, 0, NULL, 'Male', '1231', '{}', '[]', '[]', '[]', '2026-02-09 20:24:57');

-- --------------------------------------------------------

--
-- Cấu trúc bảng cho bảng `users`
--

CREATE TABLE `users` (
  `user_id` int(11) NOT NULL,
  `username` varchar(50) NOT NULL,
  `email` varchar(100) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `created_at` datetime NOT NULL DEFAULT current_timestamp(),
  `last_login` datetime DEFAULT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

--
-- Đang đổ dữ liệu cho bảng `users`
--

INSERT INTO `users` (`user_id`, `username`, `email`, `password_hash`, `created_at`, `last_login`) VALUES
(1, '1', '1@gmail.com', '123456', '2026-02-08 19:32:35', '2026-02-09 20:24:57'),
(2, '2', '2@gmail.com', '123456', '2026-02-08 21:27:21', '2026-02-09 20:18:37');

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
-- AUTO_INCREMENT cho bảng `users`
--
ALTER TABLE `users`
  MODIFY `user_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=3;

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
