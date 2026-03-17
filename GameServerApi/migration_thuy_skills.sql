-- =============================================================
-- Migration: Thêm 3 skill hệ Thủy (Water)
-- Chạy file này sau khi đã có bảng `skill` trong DB
-- An toàn khi chạy nhiều lần (ON DUPLICATE KEY UPDATE)
-- =============================================================

INSERT INTO `skill_template` (skill_id, skill_code, skill_name, description, element_type, max_level, level_to_unlock, levels_json, icon_id, created_at)
VALUES
(12, 'WATER_BOLT', 'Thủy Đạn',
 'Phóng viên đạn nước di chuyển ngang, gây sát thương khi trúng địch (Skill 1 hệ Thủy)',
 'Water', 5, 1,
 '[{"level_req":1,"sp_cost":1,"effect_value":18,"mp_cost":8,"cooldown_sec":3,"desc":"Gây 18 ST"},{"level_req":3,"sp_cost":1,"effect_value":32,"mp_cost":12,"cooldown_sec":3,"desc":"Gây 32 ST"},{"level_req":6,"sp_cost":1,"effect_value":52,"mp_cost":16,"cooldown_sec":2.5,"desc":"Gây 52 ST"},{"level_req":10,"sp_cost":2,"effect_value":78,"mp_cost":20,"cooldown_sec":2.5,"desc":"Gây 78 ST"},{"level_req":16,"sp_cost":2,"effect_value":110,"mp_cost":24,"cooldown_sec":2,"desc":"Gây 110 ST"}]',
 'icon_water_bolt', NOW()),

(13, 'WATER_PILLAR', 'Thánh Mộc Hạ',
 'Triệu hồi cây thánh từ trên trời rơi xuống, gây sát thương diện rộng khu vực đáp (Skill 2 hệ Thủy)',
 'Water', 5, 3,
 '[{"level_req":3,"sp_cost":1,"effect_value":40,"mp_cost":16,"cooldown_sec":6,"desc":"Gây 40 ST"},{"level_req":5,"sp_cost":1,"effect_value":70,"mp_cost":20,"cooldown_sec":6,"desc":"Gây 70 ST"},{"level_req":8,"sp_cost":2,"effect_value":105,"mp_cost":24,"cooldown_sec":5.5,"desc":"Gây 105 ST"},{"level_req":12,"sp_cost":2,"effect_value":150,"mp_cost":28,"cooldown_sec":5,"desc":"Gây 150 ST"},{"level_req":18,"sp_cost":3,"effect_value":200,"mp_cost":32,"cooldown_sec":4.5,"desc":"Gây 200 ST"}]',
 'icon_water_pillar', NOW()),

(14, 'WATER_ARMOR', 'Thủy Giáp Hộ Thể',
 'Bao phủ bản thân và đồng đội xung quanh lớp giáp nước, hấp thụ sát thương trong thời gian ngắn (Skill 3 hệ Thủy)',
 'Water', 5, 5,
 '[{"level_req":5,"sp_cost":1,"effect_value":15,"mp_cost":20,"cooldown_sec":12,"desc":"Buff 15 giáp 5 giây"},{"level_req":8,"sp_cost":1,"effect_value":20,"mp_cost":25,"cooldown_sec":11,"desc":"Buff 20 giáp 5 giây"},{"level_req":11,"sp_cost":2,"effect_value":28,"mp_cost":28,"cooldown_sec":10,"desc":"Buff 28 giáp 6 giây"},{"level_req":15,"sp_cost":2,"effect_value":38,"mp_cost":30,"cooldown_sec":9,"desc":"Buff 38 giáp 6 giây"},{"level_req":20,"sp_cost":3,"effect_value":50,"mp_cost":35,"cooldown_sec":8,"desc":"Buff 50 giáp 7 giây"}]',
 'icon_water_armor', NOW())

ON DUPLICATE KEY UPDATE
  `skill_name`      = VALUES(`skill_name`),
  `description`     = VALUES(`description`),
  `element_type`    = VALUES(`element_type`),
  `max_level`       = VALUES(`max_level`),
  `level_to_unlock` = VALUES(`level_to_unlock`),
  `levels_json`     = VALUES(`levels_json`),
  `icon_id`         = VALUES(`icon_id`);
