-- ============================================================
-- Migration: Cleanup cho mo hinh zone kieu LangLa
-- Ngay: 2026-04-05
-- Mo ta:
--   1. Zone thuong KHONG con duoc luu bang bang map_zone_config.
--   2. Unity server tu sinh zone cong khai theo MapWorldConfig khi boot.
--   3. Zone rieng/pho ban duoc tao runtime trong memory voi zone_id am.
--   4. Vi tri player van luu trong player_data.info_char JSON.
-- ============================================================

-- Neu DB cu van con bang map_zone_config thi xoa di de tranh drift tai lieu/architecture.
DROP TABLE IF EXISTS `map_zone_config`;

-- KHONG can tao bang game_server.
-- KHONG can ALTER TABLE player_data.
-- Du lieu can luu van la:
--   info_char.map_id
--   info_char.zone_id
--   info_char.position_x
--   info_char.position_y

-- Ghi chu van hanh:
--   - zone_id >= 0  : zone cong khai cua map thuong
--   - zone_id < 0   : zone rieng runtime (party/solo/dungeon room)
--   - Neu player login lai ma custom room da bien mat, server se fallback
--     ve 1 public zone hop le theo MapWorldConfig.

-- Vi du JSON info_char hop le:
-- {
--   "map_id": 1,
--   "zone_id": 3,
--   "position_x": 12.5,
--   "position_y": 4.0
-- }
