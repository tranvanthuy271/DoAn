-- phpMyAdmin SQL Dump
-- version 5.2.1
-- https://www.phpmyadmin.net/
--
-- Máy chủ: 127.0.0.1
-- Thời gian đã tạo: Th3 24, 2026 lúc 10:54 PM
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
(16, 'WATER_BOLT', 'Thủy Đạn', 'Bắn một viên đạn nước theo hướng player, gây sát thương khi chạm enemy (Skill 1 hệ Thủy)', 'Water', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":10,\"cooldown_sec\":3,\"desc\":\"Gây 20 ST\"},{\"level_req\":3,\"sp_cost\":1,\"effect_value\":35,\"mp_cost\":13,\"cooldown_sec\":3,\"desc\":\"Gây 35 ST\"},{\"level_req\":6,\"sp_cost\":1,\"effect_value\":55,\"mp_cost\":16,\"cooldown_sec\":2.5,\"desc\":\"Gây 55 ST\"},{\"level_req\":9,\"sp_cost\":2,\"effect_value\":80,\"mp_cost\":20,\"cooldown_sec\":2,\"desc\":\"Gây 80 ST\"},{\"level_req\":14,\"sp_cost\":2,\"effect_value\":110,\"mp_cost\":24,\"cooldown_sec\":2,\"desc\":\"Gây 110 ST\"}]', 'icon_water_bolt', '2026-03-25 10:00:00', 0, NULL),
(13, 'WATER_PILLAR', 'Thánh Mộc Hạ', 'Triệu hồi cây thánh từ trên trời rơi xuống, gây sát thương diện rộng khu vực đáp (Skill 2 hệ Thủy)', 'Water', 5, 3, '[{\"level_req\":3,\"sp_cost\":1,\"effect_value\":40,\"mp_cost\":16,\"cooldown_sec\":6,\"desc\":\"Gây 40 ST\"},{\"level_req\":5,\"sp_cost\":1,\"effect_value\":70,\"mp_cost\":20,\"cooldown_sec\":6,\"desc\":\"Gây 70 ST\"},{\"level_req\":8,\"sp_cost\":2,\"effect_value\":105,\"mp_cost\":24,\"cooldown_sec\":5.5,\"desc\":\"Gây 105 ST\"},{\"level_req\":12,\"sp_cost\":2,\"effect_value\":150,\"mp_cost\":28,\"cooldown_sec\":5,\"desc\":\"Gây 150 ST\"},{\"level_req\":18,\"sp_cost\":3,\"effect_value\":200,\"mp_cost\":32,\"cooldown_sec\":4.5,\"desc\":\"Gây 200 ST\"}]', 'icon_water_pillar', '2026-03-16 17:34:17', 0, NULL),
(14, 'EARTH_SHIELD', 'Thủy Giáp Hộ Thể', 'Bao phủ bản thân và đồng đội xung quanh lớp giáp nước, hấp thụ sát thương trong thời gian ngắn (Skill 3 hệ Thủy)', 'Water', 5, 5, '[{\"level_req\":5,\"sp_cost\":1,\"effect_value\":15,\"mp_cost\":20,\"cooldown_sec\":12,\"desc\":\"Buff 15 giáp 5 giây\"},{\"level_req\":8,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":25,\"cooldown_sec\":11,\"desc\":\"Buff 20 giáp 5 giây\"},{\"level_req\":11,\"sp_cost\":2,\"effect_value\":28,\"mp_cost\":28,\"cooldown_sec\":10,\"desc\":\"Buff 28 giáp 6 giây\"},{\"level_req\":15,\"sp_cost\":2,\"effect_value\":38,\"mp_cost\":30,\"cooldown_sec\":9,\"desc\":\"Buff 38 giáp 6 giây\"},{\"level_req\":20,\"sp_cost\":3,\"effect_value\":50,\"mp_cost\":35,\"cooldown_sec\":8,\"desc\":\"Buff 50 giáp 7 giây\"}]', 'icon_water_armor', '2026-03-15 22:59:12', 0, NULL),
(15, 'FIRE_BOLT', 'Hỏa Đạn', 'Bắn một viên đạn lửa theo hướng player, gây sát thương khi chạm enemy (Skill 1 hệ Hỏa)', 'Fire', 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":20,\"mp_cost\":10,\"cooldown_sec\":3,\"desc\":\"Gây 20 ST\"},{\"level_req\":3,\"sp_cost\":1,\"effect_value\":35,\"mp_cost\":13,\"cooldown_sec\":3,\"desc\":\"Gây 35 ST\"},{\"level_req\":6,\"sp_cost\":1,\"effect_value\":55,\"mp_cost\":16,\"cooldown_sec\":2.5,\"desc\":\"Gây 55 ST\"},{\"level_req\":9,\"sp_cost\":2,\"effect_value\":80,\"mp_cost\":20,\"cooldown_sec\":2,\"desc\":\"Gây 80 ST\"},{\"level_req\":14,\"sp_cost\":2,\"effect_value\":110,\"mp_cost\":24,\"cooldown_sec\":2,\"desc\":\"Gây 110 ST\"}]', 'icon_fire_bolt', '2026-03-16 21:02:18', 0, NULL),
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
(40, 'NORMAL_ATTACK', 'Đánh Thường', 'Đòn tấn công cơ bản, không tiêu hao MP. Sát thương tăng khi nâng cấp.', NULL, 5, 1, '[{\"level_req\":1,\"sp_cost\":1,\"effect_value\":10,\"mp_cost\":0,\"cooldown_sec\":0.8,\"desc\":\"Gây 10 ST cơ bản\"},\r\n  {\"level_req\":5,\"sp_cost\":1,\"effect_value\":18,\"mp_cost\":0,\"cooldown_sec\":0.75,\"desc\":\"Gây 18 ST\"},\r\n  {\"level_req\":10,\"sp_cost\":1,\"effect_value\":30,\"mp_cost\":0,\"cooldown_sec\":0.7,\"desc\":\"Gây 30 ST\"},\r\n  {\"level_req\":20,\"sp_cost\":2,\"effect_value\":48,\"mp_cost\":0,\"cooldown_sec\":0.65,\"desc\":\"Gây 48 ST\"},\r\n  {\"level_req\":35,\"sp_cost\":2,\"effect_value\":72,\"mp_cost\":0,\"cooldown_sec\":0.6,\"desc\":\"Gây 72 ST\"}]', 'icon_normal_attack', '2026-03-25 00:00:00', 0, NULL);

--
-- Chỉ mục cho các bảng đã đổ
--

--
-- Chỉ mục cho bảng `skill_template`
--
ALTER TABLE `skill_template`
  ADD PRIMARY KEY (`skill_id`),
  ADD UNIQUE KEY `uq_skill_code` (`skill_code`);

--
-- AUTO_INCREMENT cho các bảng đã đổ
--

--
-- AUTO_INCREMENT cho bảng `skill_template`
--
ALTER TABLE `skill_template`
  MODIFY `skill_id` int(11) NOT NULL AUTO_INCREMENT, AUTO_INCREMENT=41;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
