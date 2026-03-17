-- ============================================================
-- Migration: Cập nhật WIND_BLADE từ Projectile → Melee diện rộng
-- Chạy file này nếu đã từng chạy migration_wind_skills.sql trước đây.
-- Nếu chưa chạy migration nào, chỉ cần chạy migration_wind_skills.sql đã cập nhật.
-- ============================================================

UPDATE `skill_template`
SET
  `description`  = 'Vung tay tạo lưỡi gió sắc bén quanh thân, gây sát thương cận chiến diện rộng hơn Chưởng Phong.',
  `levels_json`  = '[{"level_req":3,"sp_cost":1,"effect_value":35,"mp_cost":12,"cooldown_sec":4.0,"desc":"Gây 35 ST diện rộng"},
                     {"level_req":5,"sp_cost":1,"effect_value":55,"mp_cost":16,"cooldown_sec":3.5,"desc":"Gây 55 ST diện rộng"},
                     {"level_req":7,"sp_cost":2,"effect_value":80,"mp_cost":20,"cooldown_sec":3.0,"desc":"Gây 80 ST diện rộng"},
                     {"level_req":10,"sp_cost":2,"effect_value":115,"mp_cost":25,"cooldown_sec":2.8,"desc":"Gây 115 ST diện rộng"},
                     {"level_req":15,"sp_cost":3,"effect_value":160,"mp_cost":30,"cooldown_sec":2.5,"desc":"Gây 160 ST diện rộng"}]'
WHERE `skill_code` = 'WIND_BLADE';

-- Kiểm tra kết quả
SELECT skill_id, skill_code, skill_name, description, element_type
FROM skill_template
WHERE skill_code = 'WIND_BLADE';
