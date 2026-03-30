-- Cập nhật dữ liệu Spawn (Sinh Quái) và Drop (Rớt đồ) cho các map còn thiếu (Map 1, 2, 3)
-- Thông tin được lấy từ bảng map_config và enemy để định vị boss và bầy quái phù hợp.

INSERT INTO `map_spawn_config` (`map_id`, `spawn_json`, `drop_json`) VALUES
(
  1, 
  '[
     {"enemy_id":4,"hp":0,"exp":15,"cx":5.5,"cy":-2.0,"is_boss":false,"count":3,"respawn_time":15,"level":5},
     {"enemy_id":4,"hp":0,"exp":15,"cx":12.0,"cy":1.5,"is_boss":false,"count":4,"respawn_time":15,"level":6},
     {"enemy_id":8,"hp":0,"exp":800,"cx":25.0,"cy":5.0,"is_boss":true,"count":1,"respawn_time":300,"level":15}
  ]',
  '[
     {"enemy_id":4,"items":[
       {"item_id":30,"rate":0.35,"qty_min":1,"qty_max":2},
       {"item_id":21,"rate":0.05,"qty_min":1,"qty_max":1}
     ]},
     {"enemy_id":8,"items":[
       {"item_id":28,"rate":0.40,"qty_min":1,"qty_max":2},
       {"item_id":47,"rate":0.10,"qty_min":1,"qty_max":1}
     ]}
  ]'
),
(
  2, 
  '[
     {"enemy_id":3,"hp":0,"exp":50,"cx":-5.0,"cy":-5.0,"is_boss":false,"count":2,"respawn_time":20,"level":12},
     {"enemy_id":7,"hp":0,"exp":65,"cx":10.0,"cy":12.0,"is_boss":false,"count":3,"respawn_time":20,"level":15},
     {"enemy_id":9,"hp":0,"exp":600,"cx":-15.0,"cy":8.0,"is_boss":true,"count":1,"respawn_time":300,"level":20}
  ]',
  '[
     {"enemy_id":3,"items":[
       {"item_id":26,"rate":0.40,"qty_min":1,"qty_max":3},
       {"item_id":2,"rate":0.25,"qty_min":1,"qty_max":2}
     ]},
     {"enemy_id":7,"items":[
       {"item_id":17,"rate":0.10,"qty_min":1,"qty_max":1}
     ]},
     {"enemy_id":9,"items":[
       {"item_id":48,"rate":0.10,"qty_min":1,"qty_max":1}
     ]}
  ]'
),
(
  3, 
  '[
     {"enemy_id":6,"hp":0,"exp":60,"cx":18.0,"cy":-10.0,"is_boss":false,"count":4,"respawn_time":20,"level":22},
     {"enemy_id":10,"hp":0,"exp":2000,"cx":40.0,"cy":-25.0,"is_boss":true,"count":1,"respawn_time":600,"level":35}
  ]',
  '[
     {"enemy_id":6,"items":[
       {"item_id":19,"rate":0.15,"qty_min":1,"qty_max":1}
     ]},
     {"enemy_id":10,"items":[
       {"item_id":28,"rate":0.80,"qty_min":2,"qty_max":5},
       {"item_id":31,"rate":0.20,"qty_min":1,"qty_max":1}
     ]}
  ]'
)
ON DUPLICATE KEY UPDATE 
  spawn_json = VALUES(spawn_json),
  drop_json = VALUES(drop_json);
