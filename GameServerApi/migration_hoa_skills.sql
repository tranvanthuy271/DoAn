-- =============================================================================
-- Migration: Thêm skills hệ Hỏa (Fire Element) vào bảng skills
-- Skill IDs: 15 (FIRE_BOLT), 16 (FIRE_BURST), 17 (FIRE_RAIN)
-- =============================================================================

INSERT INTO `skill_template` (skill_id, skill_code, skill_name, description, element_type, max_level, level_to_unlock, levels_json, icon_id, created_at) VALUES
(15,'FIRE_BOLT','Hỏa Đạn','Bắn một viên đạn lửa theo hướng player, gây sát thương khi chạm enemy (Skill 1 hệ Hỏa)','Fire',5,1,
 '[{"level_req":1,"sp_cost":1,"effect_value":20,"mp_cost":10,"cooldown_sec":3,"desc":"Gây 20 ST"},{"level_req":3,"sp_cost":1,"effect_value":35,"mp_cost":13,"cooldown_sec":3,"desc":"Gây 35 ST"},{"level_req":6,"sp_cost":1,"effect_value":55,"mp_cost":16,"cooldown_sec":2.5,"desc":"Gây 55 ST"},{"level_req":9,"sp_cost":2,"effect_value":80,"mp_cost":20,"cooldown_sec":2,"desc":"Gây 80 ST"},{"level_req":14,"sp_cost":2,"effect_value":110,"mp_cost":24,"cooldown_sec":2,"desc":"Gây 110 ST"}]',
 'icon_fire_bolt',NOW()),
(16,'FIRE_BURST','Hỏa Cầu','Bắn một cầu lửa lớn chậm hơn nhưng gây sát thương cao hơn (Skill 2 hệ Hỏa)','Fire',5,2,
 '[{"level_req":2,"sp_cost":1,"effect_value":35,"mp_cost":15,"cooldown_sec":5,"desc":"Gây 35 ST"},{"level_req":4,"sp_cost":1,"effect_value":60,"mp_cost":18,"cooldown_sec":5,"desc":"Gây 60 ST"},{"level_req":7,"sp_cost":2,"effect_value":90,"mp_cost":22,"cooldown_sec":4.5,"desc":"Gây 90 ST"},{"level_req":11,"sp_cost":2,"effect_value":130,"mp_cost":26,"cooldown_sec":4,"desc":"Gây 130 ST"},{"level_req":16,"sp_cost":3,"effect_value":180,"mp_cost":30,"cooldown_sec":4,"desc":"Gây 180 ST"}]',
 'icon_fire_burst',NOW()),
(17,'FIRE_RAIN','Thiên Hỏa','Triệu hồi mưa lửa từ trên trời rơi xuống vùng trước mặt, gây sát thương diện rộng (Skill 3 hệ Hỏa)','Fire',5,4,
 '[{"level_req":4,"sp_cost":1,"effect_value":25,"mp_cost":20,"cooldown_sec":8,"desc":"5 cầu lửa 25 ST mỗi cầu"},{"level_req":6,"sp_cost":1,"effect_value":40,"mp_cost":24,"cooldown_sec":8,"desc":"5 cầu 40 ST"},{"level_req":9,"sp_cost":2,"effect_value":60,"mp_cost":28,"cooldown_sec":7,"desc":"6 cầu 60 ST"},{"level_req":13,"sp_cost":2,"effect_value":85,"mp_cost":32,"cooldown_sec":6.5,"desc":"7 cầu 85 ST"},{"level_req":18,"sp_cost":3,"effect_value":115,"mp_cost":36,"cooldown_sec":6,"desc":"8 cầu 115 ST"}]',
 'icon_fire_rain',NOW())
ON DUPLICATE KEY UPDATE
  `skill_name`      = VALUES(`skill_name`),
  `description`     = VALUES(`description`),
  `element_type`    = VALUES(`element_type`),
  `max_level`       = VALUES(`max_level`),
  `level_to_unlock` = VALUES(`level_to_unlock`),
  `levels_json`     = VALUES(`levels_json`),
  `icon_id`         = VALUES(`icon_id`);
