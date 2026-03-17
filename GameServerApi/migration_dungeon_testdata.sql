-- ============================================================
-- Test Data: Map config + Enemy spawns cho 4 phó bản
-- Chạy SAU migration_dungeon.sql
-- Cập nhật dungeon_config để dùng đúng map_id riêng biệt
-- ============================================================

-- ────────────────────────────────────────────────────────────
-- 1. Thêm map_config cho từng phó bản
--    (mỗi dungeon có map riêng, không dùng chung map_id=1)
-- ────────────────────────────────────────────────────────────
INSERT IGNORE INTO map_config (map_id, map_name, scene_name, spawn_points_json)
VALUES
    -- Hang Động Lửa (solo) — map_id = 10
    (10, 'Hang Động Lửa', 'DungeonScene_FireCave',
     '[{"x":0,"y":0},{"x":3,"y":0}]'),

    -- Tháp Băng Giá (solo) — map_id = 11
    (11, 'Tháp Băng Giá', 'DungeonScene_IceTower',
     '[{"x":0,"y":0},{"x":2,"y":0}]'),

    -- Mê Cung Rừng Rậm (multi 4 người) — map_id = 12
    (12, 'Mê Cung Rừng Rậm', 'DungeonScene_Forest',
     '[{"x":0,"y":0},{"x":3,"y":0},{"x":-3,"y":0},{"x":0,"y":3}]'),

    -- Thành Trì Bóng Tối (multi 6 người) — map_id = 13
    (13, 'Thành Trì Bóng Tối', 'DungeonScene_DarkCastle',
     '[{"x":0,"y":0},{"x":3,"y":0},{"x":-3,"y":0},{"x":0,"y":3},{"x":3,"y":3},{"x":-3,"y":3}]');

-- ────────────────────────────────────────────────────────────
-- 2. Cập nhật dungeon_config trỏ đúng map_id
-- ────────────────────────────────────────────────────────────
UPDATE dungeon_config SET map_id = 10 WHERE dungeon_id = 1; -- Hang Động Lửa
UPDATE dungeon_config SET map_id = 11 WHERE dungeon_id = 2; -- Tháp Băng Giá
UPDATE dungeon_config SET map_id = 12 WHERE dungeon_id = 3; -- Mê Cung Rừng Rậm
UPDATE dungeon_config SET map_id = 13 WHERE dungeon_id = 4; -- Thành Trì Bóng Tối

-- ────────────────────────────────────────────────────────────
-- 3. Enemy spawns cho từng dungeon map
-- ────────────────────────────────────────────────────────────

-- map_id = 10 (Hang Động Lửa — solo, quái lửa nhỏ + boss lửa)
INSERT IGNORE INTO enemy_spawns (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
VALUES
    (10, 1,  3.0, -1.0, 2, 30),   -- Quái lửa nhỏ
    (10, 1, -2.0,  0.5, 2, 30),
    (10, 1,  5.0,  1.0, 1, 45),
    (10, 1,  0.0, -4.0, 1, 60);   -- Boss lửa (tạm dùng enemy_type_id=1, đổi sau)

-- map_id = 11 (Tháp Băng Giá — solo, quái băng + boss băng)
INSERT IGNORE INTO enemy_spawns (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
VALUES
    (11, 1,  2.0,  0.0, 2, 35),
    (11, 1, -2.0,  1.0, 2, 35),
    (11, 1,  0.0,  3.0, 1, 50),
    (11, 1,  0.0, -6.0, 1, 999);  -- Boss băng (respawn chậm)

-- map_id = 12 (Mê Cung Rừng Rậm — multi 4 người, quái rừng + elite)
INSERT IGNORE INTO enemy_spawns (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
VALUES
    (12,  1,  4.0, -1.0, 3, 25),
    (12,  1, -4.0,  0.0, 3, 25),
    (12,  1,  0.0,  4.0, 2, 30),
    (12,  1,  6.0,  2.0, 2, 30),
    (12,  1, -6.0, -2.0, 2, 30),
    (12,  1,  0.0, -7.0, 1, 120); -- Boss

-- map_id = 13 (Thành Trì Bóng Tối — multi 6 người, quái tối + boss mạnh)
INSERT IGNORE INTO enemy_spawns (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
VALUES
    (13,  1,  5.0,  0.0, 4, 20),
    (13,  1, -5.0,  0.0, 4, 20),
    (13,  1,  0.0,  5.0, 3, 25),
    (13,  1,  8.0, -2.0, 3, 25),
    (13,  1, -8.0, -2.0, 3, 25),
    (13,  1,  3.0, -4.0, 2, 40),
    (13,  1, -3.0, -4.0, 2, 40),
    (13,  1,  0.0,-10.0, 1, 999); -- Boss cuối

-- ────────────────────────────────────────────────────────────
-- 4. Kiểm tra kết quả
-- ────────────────────────────────────────────────────────────
SELECT
    dc.dungeon_id,
    dc.dungeon_name,
    dc.dungeon_type,
    dc.map_id,
    mc.map_name,
    dc.scene_name,
    dc.max_players,
    dc.min_level_required,
    dc.time_limit_seconds,
    (SELECT COUNT(*) FROM enemy_spawns es WHERE es.map_id = dc.map_id) AS enemy_spawn_count
FROM dungeon_config dc
LEFT JOIN map_config mc ON mc.map_id = dc.map_id
WHERE dc.is_active = 1
ORDER BY dc.dungeon_id;
