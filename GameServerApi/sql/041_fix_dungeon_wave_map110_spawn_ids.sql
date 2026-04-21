-- ============================================================
-- Migration: 041_fix_dungeon_wave_map110_spawn_ids.sql
-- Fix map 110 legacy spawn_json so normal mobs and boss match the
-- actual enemy table IDs used by the live DB.
--
-- enemy_id=12 -> Mộc Linh (Normal)
-- enemy_id=11 -> Đế Băng (Boss)
-- ============================================================

SET NAMES utf8mb4;

UPDATE `map_spawn_config`
SET `spawn_json` = '[
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-4,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-1.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":1,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":3.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":6,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":8.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":11,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":13.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":16,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":18.5,"cy":-1.7,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-4.56,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-2.06,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":0.44,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":2.94,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":5.44,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":7.94,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":10.44,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":12.94,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":15.44,"cy":2.21,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-4.29,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":-1.79,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":0.71,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":3.21,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":5.71,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":8.21,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":10.71,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":12,"hp":1100,"exp":1000,"cx":13.21,"cy":5.88,"is_boss":false,"count":1,"respawn_time":0,"level":5},
  {"enemy_id":11,"hp":110000,"exp":100000,"cx":18.55,"cy":5.88,"is_boss":true,"count":1,"respawn_time":0,"level":10}
]'
WHERE `map_id` = 110;