    -- ============================================================
-- Migration: Thêm kỹ năng hệ Phong (Wind)
-- Chạy file này sau khi đã chạy gamedb.sql + các migration trước.
--
-- Skill 1 - WIND_STRIKE  : Chưởng Phong (cận chiến, trigger "Skill1")
-- Skill 2 - WIND_BLADE   : Phong Nhận   (tầm ngắn, trigger "Skill2")
-- Skill 3 - WIND_STEP    : Phong Thoái Bộ (ẩn thân + animation + dash, trigger "Skill3")
--
-- levels_json fields:
--   level_req    : cấp độ player cần để mở level này
--   sp_cost      : skill point tiêu tốn
--   effect_value : sức mạnh (ST sát thương hoặc khoảng dịch chuyển đơn vị)
--   mp_cost      : MP tiêu tốn khi dùng
--   cooldown_sec : (client reference) thời gian hồi kỹ năng khuyến nghị
--   desc         : mô tả hiệu ứng
-- ============================================================

INSERT INTO `skill_template`
  (`skill_code`, `skill_name`, `description`, `element_type`,
   `max_level`, `level_to_unlock`, `levels_json`, `icon_id`, `created_at`)
VALUES

-- ── Skill 1: Chưởng Phong (Melee – đánh gần, Animator trigger "Skill1") ───
('WIND_STRIKE', 'Chưởng Phong',
 'Tung đòn cận chiến mang khí phong, gây sát thương cho kẻ địch xung quanh.',
 'Wind', 5, 1,
 '[{"level_req":1,"sp_cost":1,"effect_value":18,"mp_cost":8,"cooldown_sec":3.0,"desc":"Gây 18 ST"},
   {"level_req":3,"sp_cost":1,"effect_value":32,"mp_cost":12,"cooldown_sec":2.8,"desc":"Gây 32 ST"},
   {"level_req":5,"sp_cost":1,"effect_value":50,"mp_cost":16,"cooldown_sec":2.5,"desc":"Gây 50 ST"},
   {"level_req":8,"sp_cost":2,"effect_value":75,"mp_cost":20,"cooldown_sec":2.2,"desc":"Gây 75 ST"},
   {"level_req":12,"sp_cost":2,"effect_value":105,"mp_cost":25,"cooldown_sec":2.0,"desc":"Gây 105 ST"}]',
 'icon_wind_1', NOW()),

-- ── Skill 2: Phong Nhận (Melee phạm vi rộng – đánh cận chiến diện rộng, trigger "Skill2") ─
('WIND_BLADE', 'Phong Nhận',
 'Vung tay tạo lưỡi gió sắc bén quanh thân, gây sát thương cận chiến diện rộng hơn Chưởng Phong.',
 'Wind', 5, 3,
 '[{"level_req":3,"sp_cost":1,"effect_value":35,"mp_cost":12,"cooldown_sec":4.0,"desc":"Gây 35 ST diện rộng"},
   {"level_req":5,"sp_cost":1,"effect_value":55,"mp_cost":16,"cooldown_sec":3.5,"desc":"Gây 55 ST diện rộng"},
   {"level_req":7,"sp_cost":2,"effect_value":80,"mp_cost":20,"cooldown_sec":3.0,"desc":"Gây 80 ST diện rộng"},
   {"level_req":10,"sp_cost":2,"effect_value":115,"mp_cost":25,"cooldown_sec":2.8,"desc":"Gây 115 ST diện rộng"},
   {"level_req":15,"sp_cost":3,"effect_value":160,"mp_cost":30,"cooldown_sec":2.5,"desc":"Gây 160 ST diện rộng"}]',
 'icon_wind_2', NOW()),

-- ── Skill 3: Phong Thoái Bộ (WindStep – ẩn + animation + dash, trigger "Skill3") ─
('WIND_STEP', 'Phong Thoái Bộ',
 'Ẩn thân vào gió, phát vầng sáng phong khí tại chỗ rồi lướt tới trước bằng tốc độ phong.',
 'Wind', 5, 5,
 '[{"level_req":5,"sp_cost":1,"effect_value":3,"mp_cost":15,"cooldown_sec":8.0,"desc":"Dịch chuyển 3 đơn vị"},
   {"level_req":7,"sp_cost":1,"effect_value":4,"mp_cost":18,"cooldown_sec":7.0,"desc":"Dịch chuyển 4 đơn vị"},
   {"level_req":9,"sp_cost":2,"effect_value":5,"mp_cost":22,"cooldown_sec":6.5,"desc":"Dịch chuyển 5 đơn vị"},
   {"level_req":12,"sp_cost":2,"effect_value":6,"mp_cost":26,"cooldown_sec":6.0,"desc":"Dịch chuyển 6 đơn vị"},
   {"level_req":16,"sp_cost":3,"effect_value":8,"mp_cost":30,"cooldown_sec":5.0,"desc":"Dịch chuyển 8 đơn vị"}]',
 'icon_wind_3', NOW());

-- ============================================================
-- Kiểm tra kết quả
-- ============================================================
SELECT skill_id, skill_code, skill_name, element_type, max_level, level_to_unlock
FROM skill_template
WHERE element_type = 'Wind'
ORDER BY skill_id;
