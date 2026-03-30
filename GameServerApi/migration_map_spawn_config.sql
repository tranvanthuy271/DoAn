-- ============================================================
--  Migration: map_spawn_config
--  Tạo bảng lưu cấu hình spawn enemy + drop item theo map,
--  dùng JSON blob để Unity host fetch và validate khi khởi động.
--
--  Chạy file này MỘT LẦN duy nhất.
--  Bảng enemy_spawns cũ vẫn giữ nguyên (backward compat).
-- ============================================================

CREATE TABLE IF NOT EXISTS map_spawn_config (
    id         INT          NOT NULL AUTO_INCREMENT PRIMARY KEY,
    map_id     INT          NOT NULL UNIQUE COMMENT 'FK → map_config.map_id',

    -- JSON array mô tả tất cả vị trí spawn trên map.
    -- Mỗi phần tử: {enemy_id, hp, exp, cx, cy, is_boss, count, respawn_time}
    -- hp=0 → Unity host tự đọc base_hp từ prefab/enemy table.
    -- Ví dụ:
    -- [
    --   {"enemy_id":1,"hp":200,"exp":50,"cx":100.5,"cy":60.0,
    --    "is_boss":false,"count":2,"respawn_time":30},
    --   {"enemy_id":3,"hp":8000,"exp":2000,"cx":512.0,"cy":256.0,
    --    "is_boss":true,"count":1,"respawn_time":300}
    -- ]
    spawn_json LONGTEXT     NOT NULL DEFAULT '[]'
        COMMENT 'JSON array — mỗi entry = 1 điểm spawn: {enemy_id,hp,exp,cx,cy,is_boss,count,respawn_time}',

    -- JSON array mô tả tỉ lệ rơi item per enemy_id.
    -- Mỗi phần tử: {enemy_id, items:[{item_id, rate, qty_min, qty_max}]}
    -- rate dùng hệ 0.0–1.0 (0.25 = 25%).
    -- Ví dụ:
    -- [
    --   {"enemy_id":1,"items":[
    --       {"item_id":10,"rate":0.25,"qty_min":1,"qty_max":1},
    --       {"item_id":15,"rate":0.05,"qty_min":1,"qty_max":2}
    --   ]},
    --   {"enemy_id":3,"items":[
    --       {"item_id":50,"rate":0.10,"qty_min":1,"qty_max":2}
    --   ]}
    -- ]
    drop_json  LONGTEXT     NOT NULL DEFAULT '[]'
        COMMENT 'JSON array — mỗi entry = 1 loại quái: {enemy_id, items:[{item_id,rate,qty_min,qty_max}]}',

    updated_at DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP
                            ON UPDATE CURRENT_TIMESTAMP,

    CONSTRAINT fk_msc_map FOREIGN KEY (map_id) REFERENCES map_config(map_id)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Cấu hình spawn enemy và tỉ lệ drop theo mapId — Unity host đọc khi khởi động scene';


-- ============================================================
--  Seed dữ liệu mẫu
--  Map 0: Làng Khởi Đầu
--  Map 1: Cánh Đồng Lửa
--  Map 2: Rừng Băng
--  Map 3: Sa Mạc Phong (boss cuối)
-- ============================================================

-- Map 0 — Làng Khởi Đầu: Slime (id=1) + Goblin (id=2) + Boss Goblin Chief (id=4)
INSERT INTO map_spawn_config (map_id, spawn_json, drop_json) VALUES
(0,
 '[
   {"enemy_id":1,"hp":120,"exp":30,"cx":8.5,"cy":3.0,"is_boss":false,"count":2,"respawn_time":25},
   {"enemy_id":1,"hp":120,"exp":30,"cx":-6.0,"cy":2.5,"is_boss":false,"count":1,"respawn_time":25},
   {"enemy_id":2,"hp":80,"exp":20,"cx":15.0,"cy":0.0,"is_boss":false,"count":3,"respawn_time":20},
   {"enemy_id":2,"hp":80,"exp":20,"cx":-12.0,"cy":1.5,"is_boss":false,"count":2,"respawn_time":20},
   {"enemy_id":4,"hp":800,"exp":200,"cx":0.0,"cy":8.0,"is_boss":true,"count":1,"respawn_time":180}
 ]',
 '[
   {"enemy_id":1,"items":[
     {"item_id":22,"rate":0.30,"qty_min":1,"qty_max":2},
     {"item_id":10,"rate":0.05,"qty_min":1,"qty_max":1}
   ]},
   {"enemy_id":2,"items":[
     {"item_id":22,"rate":0.20,"qty_min":1,"qty_max":1},
     {"item_id":10,"rate":0.03,"qty_min":1,"qty_max":1}
   ]},
   {"enemy_id":4,"items":[
     {"item_id":50,"rate":1.00,"qty_min":1,"qty_max":1},
     {"item_id":10,"rate":0.50,"qty_min":1,"qty_max":2},
     {"item_id":21,"rate":0.10,"qty_min":1,"qty_max":1}
   ]}
 ]'
)
ON DUPLICATE KEY UPDATE
  spawn_json = VALUES(spawn_json),
  drop_json  = VALUES(drop_json);

-- Map 1 — Cánh Đồng Lửa: FireSlime (id=5) + Goblin Archer (id=6) + Boss Fire Dragon (id=8)
INSERT INTO map_spawn_config (map_id, spawn_json, drop_json) VALUES
(1,
 '[
   {"enemy_id":5,"hp":300,"exp":80,"cx":10.0,"cy":2.0,"is_boss":false,"count":3,"respawn_time":30},
   {"enemy_id":5,"hp":300,"exp":80,"cx":-8.0,"cy":3.0,"is_boss":false,"count":2,"respawn_time":30},
   {"enemy_id":6,"hp":200,"exp":60,"cx":20.0,"cy":0.0,"is_boss":false,"count":2,"respawn_time":35},
   {"enemy_id":8,"hp":3000,"exp":800,"cx":0.0,"cy":10.0,"is_boss":true,"count":1,"respawn_time":300}
 ]',
 '[
   {"enemy_id":5,"items":[
     {"item_id":22,"rate":0.35,"qty_min":1,"qty_max":2},
     {"item_id":11,"rate":0.08,"qty_min":1,"qty_max":1}
   ]},
   {"enemy_id":6,"items":[
     {"item_id":22,"rate":0.25,"qty_min":1,"qty_max":2},
     {"item_id":11,"rate":0.05,"qty_min":1,"qty_max":1}
   ]},
   {"enemy_id":8,"items":[
     {"item_id":51,"rate":1.00,"qty_min":1,"qty_max":1},
     {"item_id":11,"rate":0.80,"qty_min":1,"qty_max":3},
     {"item_id":21,"rate":0.15,"qty_min":1,"qty_max":1}
   ]}
 ]'
)
ON DUPLICATE KEY UPDATE
  spawn_json = VALUES(spawn_json),
  drop_json  = VALUES(drop_json);

-- Map 2 — Rừng Băng: IceWolf (id=9) + Orc (id=3)
INSERT INTO map_spawn_config (map_id, spawn_json, drop_json) VALUES
(2,
 '[
   {"enemy_id":9,"hp":450,"exp":120,"cx":12.0,"cy":4.0,"is_boss":false,"count":2,"respawn_time":40},
   {"enemy_id":9,"hp":450,"exp":120,"cx":-10.0,"cy":3.5,"is_boss":false,"count":2,"respawn_time":40},
   {"enemy_id":3,"hp":600,"exp":150,"cx":0.0,"cy":0.0,"is_boss":false,"count":3,"respawn_time":45}
 ]',
 '[
   {"enemy_id":9,"items":[
     {"item_id":22,"rate":0.40,"qty_min":1,"qty_max":2},
     {"item_id":12,"rate":0.10,"qty_min":1,"qty_max":1}
   ]},
   {"enemy_id":3,"items":[
     {"item_id":22,"rate":0.30,"qty_min":2,"qty_max":3},
     {"item_id":12,"rate":0.07,"qty_min":1,"qty_max":1}
   ]}
 ]'
)
ON DUPLICATE KEY UPDATE
  spawn_json = VALUES(spawn_json),
  drop_json  = VALUES(drop_json);

-- Map 3 — Sa Mạc Phong: Boss Dragon (id=7) — boss-only map
INSERT INTO map_spawn_config (map_id, spawn_json, drop_json) VALUES
(3,
 '[
   {"enemy_id":7,"hp":15000,"exp":5000,"cx":0.0,"cy":5.0,"is_boss":true,"count":1,"respawn_time":600}
 ]',
 '[
   {"enemy_id":7,"items":[
     {"item_id":100,"rate":1.00,"qty_min":1,"qty_max":1},
     {"item_id":101,"rate":0.50,"qty_min":1,"qty_max":2},
     {"item_id":21,"rate":0.30,"qty_min":1,"qty_max":1},
     {"item_id":10,"rate":1.00,"qty_min":10,"qty_max":20}
   ]}
 ]'
)
ON DUPLICATE KEY UPDATE
  spawn_json = VALUES(spawn_json),
  drop_json  = VALUES(drop_json);
