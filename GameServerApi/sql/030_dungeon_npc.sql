-- ============================================================
-- Migration: 030_dungeon_npc.sql
-- Thêm NPC dungeon và dữ liệu phó bản theo schema hiện tại
-- ============================================================

SET NAMES utf8mb4;

-- Thêm NPC dungeon (npc_type = 'dungeon') cho map chính nếu chưa có.
INSERT INTO npc_config (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id, is_active)
SELECT N'Thủ môn Phó Bản', 'dungeon', 0, 50.0, 10.0, 'npc_dungeon_greet', '', 1
WHERE NOT EXISTS (
  SELECT 1 FROM npc_config WHERE npc_type = 'dungeon' AND map_id = 0
);

INSERT INTO npc_config (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id, is_active)
SELECT N'Thủ môn Phó Bản Tổ Đội', 'dungeon', 1, 30.0, 15.0, 'npc_dungeon_greet', '', 1
WHERE NOT EXISTS (
  SELECT 1 FROM npc_config WHERE npc_type = 'dungeon' AND map_id = 1
);

-- Seed / cập nhật 2 phó bản mặc định dùng bởi UI danh sách phó bản.
-- map_id 100 -> DungeonWaveScene
-- map_id 101 -> DungeonPartyScene
INSERT INTO dungeon_config
  (dungeon_id, dungeon_name, dungeon_type, map_id, scene_name, max_players,
   min_level_required, time_limit_seconds, description, boss_enemy_id,
   reward_json, thumbnail_icon_id, is_active)
VALUES
  (6, N'Phó Bản Sóng', 'solo', 100, 'DungeonWaveScene', 1,
   1, 0, '', NULL, JSON_OBJECT(), '', 1),
  (7, N'Phó Bản Tổ Đội', 'multi', 101, 'DungeonPartyScene', 4,
   1, 0, '', NULL, JSON_OBJECT(), '', 1)
ON DUPLICATE KEY UPDATE
  dungeon_name = VALUES(dungeon_name),
  dungeon_type = VALUES(dungeon_type),
  map_id = VALUES(map_id),
  scene_name = VALUES(scene_name),
  max_players = VALUES(max_players),
  min_level_required = VALUES(min_level_required),
  time_limit_seconds = VALUES(time_limit_seconds),
  description = VALUES(description),
  boss_enemy_id = VALUES(boss_enemy_id),
  reward_json = VALUES(reward_json),
  thumbnail_icon_id = VALUES(thumbnail_icon_id),
  is_active = VALUES(is_active);
