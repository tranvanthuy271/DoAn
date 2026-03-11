-- ============================================================
-- Migration: Hệ thống Phó Bản (Dungeon/Instance System)
-- Tệp: migration_dungeon.sql
-- Chạy AFTER khi đã có bảng map_config và enemy
-- ============================================================

-- ────────────────────────────────────────────────────────────
-- 1. Bảng dungeon_config — Cấu hình phó bản
-- ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS dungeon_config (
    dungeon_id          INT          AUTO_INCREMENT PRIMARY KEY,
    dungeon_name        VARCHAR(100) NOT NULL,
    -- "solo"  = thử thách 1 mình (client tự host, không đăng ký session)
    -- "multi" = nhiều người (dùng dungeon_session để track host)
    dungeon_type        ENUM('solo','multi') NOT NULL DEFAULT 'multi',
    -- FK sang map_config: xác định map/scene nào sẽ load khi vào phó bản
    map_id              INT          NOT NULL,
    -- Tên scene Unity (phải khớp Build Settings). VD: "Dungeon_FireCave"
    scene_name          VARCHAR(100) NOT NULL DEFAULT '',
    max_players         INT          NOT NULL DEFAULT 4,  -- 1 với solo
    min_level_required  INT          NOT NULL DEFAULT 1,
    time_limit_seconds  INT          NOT NULL DEFAULT 0,  -- 0 = vô hạn
    description         TEXT,
    boss_enemy_id       INT          NULL,                -- FK sang enemy
    reward_json         JSON         NOT NULL DEFAULT ('{}'),
    thumbnail_icon_id   VARCHAR(50)  NOT NULL DEFAULT '',
    is_active           TINYINT(1)   NOT NULL DEFAULT 1,
    created_at          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                                     ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_dungeon_map
        FOREIGN KEY (map_id) REFERENCES map_config(map_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_dungeon_boss
        FOREIGN KEY (boss_enemy_id) REFERENCES enemy(enemy_id)
        ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ────────────────────────────────────────────────────────────
-- 2. Bảng dungeon_session — Theo dõi session đang chạy
--    Chỉ dùng cho phó bản "multi".
--    Mỗi lần ai đó host phó bản multi → INSERT 1 row ở đây.
--    API server dùng bảng này để biết host IP/port cần connect.
-- ────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS dungeon_session (
    session_id          INT         AUTO_INCREMENT PRIMARY KEY,
    dungeon_config_id   INT         NOT NULL,
    host_ip             VARCHAR(45) NOT NULL,          -- IPv4 hoặc IPv6
    host_port           INT         NOT NULL DEFAULT 7777,
    current_players     INT         NOT NULL DEFAULT 0,
    max_players         INT         NOT NULL DEFAULT 4,
    -- "waiting" = đang chờ player | "active" = đầy | "ended" = kết thúc
    status              ENUM('waiting','active','ended') NOT NULL DEFAULT 'waiting',
    created_at          DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at          DATETIME    NOT NULL DEFAULT CURRENT_TIMESTAMP
                                    ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_session_dungeon
        FOREIGN KEY (dungeon_config_id) REFERENCES dungeon_config(dungeon_id)
        ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Index nhanh khi query session đang chờ của 1 phó bản
CREATE INDEX idx_session_dungeon_status
    ON dungeon_session (dungeon_config_id, status);

-- ────────────────────────────────────────────────────────────
-- 3. Thêm cột scene_name vào map_config (nếu chưa có)
--    scene_name: tên scene Unity tương ứng, dùng để SceneManager.LoadScene()
-- ────────────────────────────────────────────────────────────
ALTER TABLE map_config
    ADD COLUMN IF NOT EXISTS scene_name VARCHAR(100) NOT NULL DEFAULT ''
    AFTER map_name;

-- ────────────────────────────────────────────────────────────
-- 4. Dữ liệu mẫu — Xoá / tuỳ biến lại cho game thực tế
-- ────────────────────────────────────────────────────────────
-- Giả sử map_id 1 = map chính (GameScene), map_id 2 = map 2 (nếu có)
-- Nếu chưa có map 2, INSERT trước:
-- INSERT IGNORE INTO map_config (map_id, map_name, scene_name, spawn_points_json)
--     VALUES (2, 'Rừng Tối', 'ForestScene', '[{"x":0,"y":0},{"x":5,"y":0}]');

INSERT INTO dungeon_config
    (dungeon_name, dungeon_type, map_id, scene_name, max_players,
     min_level_required, time_limit_seconds, description, thumbnail_icon_id)
VALUES
    -- Solo dungeons — max_players = 1, player tự làm host
    ('Hang Động Lửa',      'solo',  1, 'DungeonScene_FireCave',  1,  5, 300,
     'Thử thách một mình: vượt qua hang động ngọn lửa để nhận phần thưởng hiếm.',
     'icon_dungeon_fire'),

    ('Tháp Băng Giá',      'solo',  1, 'DungeonScene_IceTower',  1, 10, 600,
     'Solo boss băng hà cổ đại bị phong ấn trong tháp băng.',
     'icon_dungeon_ice'),

    -- Multi dungeons — nhiều người cùng vào
    ('Mê Cung Rừng Rậm',   'multi', 1, 'DungeonScene_Forest',    4,  8,   0,
     'Khám phá mê cung rừng rậm cùng đồng đội — cẩn thận bẫy và quái ẩn.',
     'icon_dungeon_forest'),

    ('Thành Trì Bóng Tối', 'multi', 1, 'DungeonScene_DarkCastle',6, 15, 900,
     'Đội nhóm 6 người đối đầu Chúa Tể Bóng Tối trong thành trì cổ.',
     'icon_dungeon_dark');
