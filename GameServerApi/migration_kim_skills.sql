-- ============================================================
-- Migration: Thêm kỹ năng hệ Kim (Metal)
-- Chạy file này sau khi đã chạy gamedb.sql + các migration trước.
--
-- Skill 1 - METAL_STRIKE  : Kim Phong        (cận chiến, trigger "Skill1")
-- Skill 2 - METAL_BLADE   : Kim Nhẫn         (cận chiến diện rộng, trigger "Skill2")
-- Skill 3 - METAL_SHIELD  : Kim Cương Khiên  (bất tử + phá projectile, trigger "Skill3")
--
-- levels_json fields:
--   level_req    : cấp độ player cần để mở level này
--   sp_cost      : skill point tiêu tốn
--   effect_value : sức mạnh (ST sát thương / giây bất tử)
--   mp_cost      : MP tiêu tốn khi dùng
--   cooldown_sec : thời gian hồi kỹ năng khuyến nghị (giây)
--   desc         : mô tả hiệu ứng
-- ============================================================

-- An toàn khi chạy lại nhiều lần
INSERT INTO `skill_template`
  (`skill_code`, `skill_name`, `description`, `element_type`,
   `max_level`, `level_to_unlock`, `levels_json`, `icon_id`, `created_at`)
VALUES

-- ── Skill 1: Kim Phong (Melee – đánh gần, Animator trigger "Skill1") ─────
('METAL_STRIKE', 'Kim Phong',
 'Đòn chém cận chiến bằng lưỡi kim loại sắc bén, gây sát thương cho kẻ địch trước mặt.',
 'Metal', 5, 1,
 '[{"level_req":1,"sp_cost":1,"effect_value":20,"mp_cost":8,"cooldown_sec":3.0,"desc":"Gây 20 ST cận chiến"},
   {"level_req":3,"sp_cost":1,"effect_value":38,"mp_cost":12,"cooldown_sec":3.0,"desc":"Gây 38 ST"},
   {"level_req":5,"sp_cost":2,"effect_value":60,"mp_cost":16,"cooldown_sec":2.5,"desc":"Gây 60 ST"},
   {"level_req":8,"sp_cost":2,"effect_value":88,"mp_cost":20,"cooldown_sec":2.5,"desc":"Gây 88 ST"},
   {"level_req":12,"sp_cost":3,"effect_value":120,"mp_cost":24,"cooldown_sec":2.0,"desc":"Gây 120 ST"}]',
 'icon_metal_strike', NOW()),

-- ── Skill 2: Kim Nhẫn (Melee AoE – Animator trigger "Skill2") ────────────
('METAL_BLADE', 'Kim Nhẫn',
 'Tung lưỡi hình tròn quét vùng rộng xung quanh, gây sát thương cho toàn bộ kẻ địch gần đó.',
 'Metal', 5, 3,
 '[{"level_req":3,"sp_cost":1,"effect_value":30,"mp_cost":14,"cooldown_sec":4.0,"desc":"Gây 30 ST diện rộng"},
   {"level_req":5,"sp_cost":1,"effect_value":55,"mp_cost":18,"cooldown_sec":4.0,"desc":"Gây 55 ST"},
   {"level_req":8,"sp_cost":2,"effect_value":85,"mp_cost":22,"cooldown_sec":3.5,"desc":"Gây 85 ST"},
   {"level_req":12,"sp_cost":2,"effect_value":120,"mp_cost":26,"cooldown_sec":3.5,"desc":"Gây 120 ST"},
   {"level_req":18,"sp_cost":3,"effect_value":165,"mp_cost":30,"cooldown_sec":3.0,"desc":"Gây 165 ST"}]',
 'icon_metal_blade', NOW()),

-- ── Skill 3: Kim Cương Khiên (MetalShield – Animator trigger "Skill3") ───
('METAL_SHIELD', 'Kim Cương Khiên',
 'Tạo khiên kim cương bất tử, miễn nhiễm hoàn toàn mọi sát thương và đòn tấn công trong thời gian duy trì. Mọi projectile chạm vào sẽ bị phá hủy ngay lập tức.',
 'Metal', 5, 5,
 '[{"level_req":5,"sp_cost":1,"effect_value":3,"mp_cost":20,"cooldown_sec":12.0,"desc":"Bất tử 3 giây"},
   {"level_req":8,"sp_cost":1,"effect_value":4,"mp_cost":25,"cooldown_sec":11.0,"desc":"Bất tử 4 giây"},
   {"level_req":11,"sp_cost":2,"effect_value":5,"mp_cost":28,"cooldown_sec":10.0,"desc":"Bất tử 5 giây"},
   {"level_req":15,"sp_cost":2,"effect_value":6,"mp_cost":30,"cooldown_sec":9.0,"desc":"Bất tử 6 giây"},
   {"level_req":20,"sp_cost":3,"effect_value":8,"mp_cost":35,"cooldown_sec":8.0,"desc":"Bất tử 8 giây"}]',
 'icon_metal_shield', NOW())

ON DUPLICATE KEY UPDATE
  `skill_name`      = VALUES(`skill_name`),
  `description`     = VALUES(`description`),
  `element_type`    = VALUES(`element_type`),
  `max_level`       = VALUES(`max_level`),
  `level_to_unlock` = VALUES(`level_to_unlock`),
  `levels_json`     = VALUES(`levels_json`),
  `icon_id`         = VALUES(`icon_id`);
