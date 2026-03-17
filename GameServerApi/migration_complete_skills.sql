-- ============================================================
-- Migration: Hoàn thiện skill cho TẤT CẢ các hệ (3 skill/hệ)
-- + Thêm cooldown_sec vào levels_json của skill cũ
-- Mỗi level trong levels_json:
--   level_req    : cấp player cần đạt
--   sp_cost      : skill point tiêu khi nâng
--   effect_value : sát thương / hồi phục / khoảng cách (tùy skill)
--   mp_cost      : MP tiêu khi dùng skill
--   cooldown_sec : cooldown (giây) ở level đó — CLIENT ĐỌC để apply động
--   desc         : mô tả ngắn
-- ============================================================

-- ============================================================
-- [1] CẬP NHẬT levels_json CỦA CÁC SKILL CŨ — THÊM cooldown_sec
-- ============================================================

-- FIRE_BALL (skill_id 1)
UPDATE `skill_template` SET `levels_json` =
'[{"level_req":1,"sp_cost":1,"effect_value":20,"mp_cost":10,"cooldown_sec":3.0,"desc":"Gây 20 ST"},
  {"level_req":3,"sp_cost":1,"effect_value":35,"mp_cost":15,"cooldown_sec":2.8,"desc":"Gây 35 ST"},
  {"level_req":5,"sp_cost":1,"effect_value":55,"mp_cost":20,"cooldown_sec":2.5,"desc":"Gây 55 ST"},
  {"level_req":8,"sp_cost":2,"effect_value":80,"mp_cost":25,"cooldown_sec":2.2,"desc":"Gây 80 ST"},
  {"level_req":12,"sp_cost":2,"effect_value":110,"mp_cost":30,"cooldown_sec":2.0,"desc":"Gây 110 ST"}]'
WHERE `skill_code` = 'FIRE_BALL';

-- FIRE_WAVE (skill_id 2)
UPDATE `skill_template` SET `levels_json` =
'[{"level_req":5,"sp_cost":1,"effect_value":30,"mp_cost":15,"cooldown_sec":5.0,"desc":"Gây 30 ST diện rộng"},
  {"level_req":8,"sp_cost":1,"effect_value":50,"mp_cost":20,"cooldown_sec":4.5,"desc":"Gây 50 ST"},
  {"level_req":10,"sp_cost":2,"effect_value":75,"mp_cost":25,"cooldown_sec":4.0,"desc":"Gây 75 ST"},
  {"level_req":15,"sp_cost":2,"effect_value":100,"mp_cost":30,"cooldown_sec":3.5,"desc":"Gây 100 ST"},
  {"level_req":20,"sp_cost":3,"effect_value":140,"mp_cost":35,"cooldown_sec":3.0,"desc":"Gây 140 ST"}]'
WHERE `skill_code` = 'FIRE_WAVE';

-- WATER_SHIELD (skill_id 3)
UPDATE `skill_template` SET `levels_json` =
'[{"level_req":1,"sp_cost":1,"effect_value":30,"mp_cost":12,"cooldown_sec":8.0,"desc":"Hấp 30 ST"},
  {"level_req":3,"sp_cost":1,"effect_value":50,"mp_cost":18,"cooldown_sec":7.5,"desc":"Hấp 50 ST"},
  {"level_req":5,"sp_cost":1,"effect_value":75,"mp_cost":22,"cooldown_sec":7.0,"desc":"Hấp 75 ST"},
  {"level_req":8,"sp_cost":2,"effect_value":110,"mp_cost":28,"cooldown_sec":6.5,"desc":"Hấp 110 ST"},
  {"level_req":12,"sp_cost":2,"effect_value":150,"mp_cost":35,"cooldown_sec":6.0,"desc":"Hấp 150 ST"}]'
WHERE `skill_code` = 'WATER_SHIELD';

-- HEAL_WAVE (skill_id 4)
UPDATE `skill_template` SET `levels_json` =
'[{"level_req":3,"sp_cost":1,"effect_value":40,"mp_cost":20,"cooldown_sec":10.0,"desc":"Hồi 40 HP"},
  {"level_req":6,"sp_cost":1,"effect_value":70,"mp_cost":28,"cooldown_sec":9.0,"desc":"Hồi 70 HP"},
  {"level_req":9,"sp_cost":2,"effect_value":110,"mp_cost":35,"cooldown_sec":8.0,"desc":"Hồi 110 HP"},
  {"level_req":13,"sp_cost":2,"effect_value":160,"mp_cost":42,"cooldown_sec":7.0,"desc":"Hồi 160 HP"},
  {"level_req":18,"sp_cost":3,"effect_value":220,"mp_cost":50,"cooldown_sec":6.0,"desc":"Hồi 220 HP"}]'
WHERE `skill_code` = 'HEAL_WAVE';

-- DASH / Universal (skill_id 5)
UPDATE `skill_template` SET `levels_json` =
'[{"level_req":1,"sp_cost":1,"effect_value":1,"mp_cost":8,"cooldown_sec":4.0,"desc":"Lướt 1 đơn vị"},
  {"level_req":3,"sp_cost":1,"effect_value":2,"mp_cost":10,"cooldown_sec":3.5,"desc":"Lướt 2 đơn vị"},
  {"level_req":6,"sp_cost":1,"effect_value":3,"mp_cost":12,"cooldown_sec":3.0,"desc":"Lướt 3 đơn vị"},
  {"level_req":10,"sp_cost":2,"effect_value":4,"mp_cost":14,"cooldown_sec":2.5,"desc":"Lướt 4 đơn vị"},
  {"level_req":15,"sp_cost":2,"effect_value":5,"mp_cost":16,"cooldown_sec":2.0,"desc":"Lướt 5 đơn vị"}]'
WHERE `skill_code` = 'DASH';

-- EARTH_SMASH (skill_id 6)
UPDATE `skill_template` SET `levels_json` =
'[{"level_req":1,"sp_cost":1,"effect_value":25,"mp_cost":12,"cooldown_sec":3.5,"desc":"Gây 25 ST"},
  {"level_req":3,"sp_cost":1,"effect_value":45,"mp_cost":18,"cooldown_sec":3.2,"desc":"Gây 45 ST"},
  {"level_req":6,"sp_cost":2,"effect_value":70,"mp_cost":24,"cooldown_sec":3.0,"desc":"Gây 70 ST"},
  {"level_req":10,"sp_cost":2,"effect_value":100,"mp_cost":30,"cooldown_sec":2.7,"desc":"Gây 100 ST"},
  {"level_req":15,"sp_cost":3,"effect_value":140,"mp_cost":36,"cooldown_sec":2.5,"desc":"Gây 140 ST"}]'
WHERE `skill_code` = 'EARTH_SMASH';

-- METAL_SLASH (skill_id 7)
UPDATE `skill_template` SET `levels_json` =
'[{"level_req":1,"sp_cost":1,"effect_value":22,"mp_cost":10,"cooldown_sec":3.0,"desc":"Gây 22 ST"},
  {"level_req":3,"sp_cost":1,"effect_value":40,"mp_cost":15,"cooldown_sec":2.8,"desc":"Gây 40 ST"},
  {"level_req":5,"sp_cost":2,"effect_value":62,"mp_cost":20,"cooldown_sec":2.5,"desc":"Gây 62 ST"},
  {"level_req":8,"sp_cost":2,"effect_value":90,"mp_cost":25,"cooldown_sec":2.3,"desc":"Gây 90 ST"},
  {"level_req":12,"sp_cost":3,"effect_value":125,"mp_cost":30,"cooldown_sec":2.0,"desc":"Gây 125 ST"}]'
WHERE `skill_code` = 'METAL_SLASH';

-- WOOD_VINE (skill_id 8)
UPDATE `skill_template` SET `levels_json` =
'[{"level_req":1,"sp_cost":1,"effect_value":1,"mp_cost":14,"cooldown_sec":6.0,"desc":"Trói 1s"},
  {"level_req":3,"sp_cost":1,"effect_value":2,"mp_cost":18,"cooldown_sec":5.5,"desc":"Trói 2s"},
  {"level_req":5,"sp_cost":2,"effect_value":3,"mp_cost":22,"cooldown_sec":5.0,"desc":"Trói 3s"},
  {"level_req":8,"sp_cost":2,"effect_value":4,"mp_cost":26,"cooldown_sec":4.5,"desc":"Trói 4s"},
  {"level_req":12,"sp_cost":3,"effect_value":5,"mp_cost":30,"cooldown_sec":4.0,"desc":"Trói 5s"}]'
WHERE `skill_code` = 'WOOD_VINE';

-- ============================================================
-- [2] THÊM SKILL THỨ 3 CHO HỆ LỬA (Fire)
--     Skill 3 gợi ý: FIRE_BURST — Bùng nổ lửa cận chiến (Melee), trigger "Skill3"
-- ============================================================
INSERT INTO `skill_template`
  (`skill_code`,`skill_name`,`description`,`element_type`,`max_level`,`level_to_unlock`,`levels_json`,`icon_id`,`created_at`)
VALUES
('FIRE_BURST','Bùng Lửa Cận','Bùng nổ lửa ngay tại vị trí player, thiêu đốt kẻ địch xung quanh.','Fire',5,5,
 '[{"level_req":5,"sp_cost":1,"effect_value":35,"mp_cost":18,"cooldown_sec":6.0,"desc":"Gây 35 ST AoE"},
   {"level_req":7,"sp_cost":1,"effect_value":60,"mp_cost":22,"cooldown_sec":5.5,"desc":"Gây 60 ST AoE"},
   {"level_req":10,"sp_cost":2,"effect_value":90,"mp_cost":28,"cooldown_sec":5.0,"desc":"Gây 90 ST AoE"},
   {"level_req":14,"sp_cost":2,"effect_value":130,"mp_cost":34,"cooldown_sec":4.5,"desc":"Gây 130 ST AoE"},
   {"level_req":18,"sp_cost":3,"effect_value":180,"mp_cost":40,"cooldown_sec":4.0,"desc":"Gây 180 ST AoE"}]',
 'icon_fire_3',NOW());

-- ============================================================
-- [3] THÊM SKILL THỨ 3 CHO HỆ NƯỚC (Water)
--     Skill 3: WATER_SURGE — Lướt sóng (WindStep-style), trigger "Skill3"
-- ============================================================
INSERT INTO `skill_template`
  (`skill_code`,`skill_name`,`description`,`element_type`,`max_level`,`level_to_unlock`,`levels_json`,`icon_id`,`created_at`)
VALUES
('WATER_SURGE','Lướt Sóng','Cưỡi trên làn sóng lao tới trước, né tránh đòn tấn công.','Water',5,5,
 '[{"level_req":5,"sp_cost":1,"effect_value":3,"mp_cost":16,"cooldown_sec":7.0,"desc":"Lướt 3 đơn vị"},
   {"level_req":7,"sp_cost":1,"effect_value":4,"mp_cost":19,"cooldown_sec":6.5,"desc":"Lướt 4 đơn vị"},
   {"level_req":9,"sp_cost":2,"effect_value":5,"mp_cost":23,"cooldown_sec":6.0,"desc":"Lướt 5 đơn vị"},
   {"level_req":12,"sp_cost":2,"effect_value":6,"mp_cost":27,"cooldown_sec":5.5,"desc":"Lướt 6 đơn vị"},
   {"level_req":16,"sp_cost":3,"effect_value":8,"mp_cost":32,"cooldown_sec":5.0,"desc":"Lướt 8 đơn vị"}]',
 'icon_water_3',NOW());

-- ============================================================
-- [4] THÊM SKILL 2 VÀ 3 CHO HỆ ĐẤT (Earth)
--     Skill 2: EARTH_SHIELD — Khiên Đất (Melee-style buff), trigger "Skill2"
--     Skill 3: EARTH_SPIKE  — Gai Đất trồi lên (Projectile), trigger "Skill3"
-- ============================================================
INSERT INTO `skill_template`
  (`skill_code`,`skill_name`,`description`,`element_type`,`max_level`,`level_to_unlock`,`levels_json`,`icon_id`,`created_at`)
VALUES
('EARTH_SHIELD','Khiên Đất','Tạo lớp giáp đất tạm thời, giảm sát thương nhận vào.','Earth',5,3,
 '[{"level_req":3,"sp_cost":1,"effect_value":10,"mp_cost":15,"cooldown_sec":8.0,"desc":"Giảm 10 ST nhận vào trong 3s"},
   {"level_req":5,"sp_cost":1,"effect_value":18,"mp_cost":20,"cooldown_sec":7.5,"desc":"Giảm 18 ST trong 3s"},
   {"level_req":8,"sp_cost":2,"effect_value":28,"mp_cost":26,"cooldown_sec":7.0,"desc":"Giảm 28 ST trong 4s"},
   {"level_req":12,"sp_cost":2,"effect_value":40,"mp_cost":32,"cooldown_sec":6.5,"desc":"Giảm 40 ST trong 4s"},
   {"level_req":17,"sp_cost":3,"effect_value":55,"mp_cost":38,"cooldown_sec":6.0,"desc":"Giảm 55 ST trong 5s"}]',
 'icon_earth_2',NOW()),
('EARTH_SPIKE','Gai Đất','Triệu hồi gai đất phóng thẳng về phía trước xuyên kẻ địch.','Earth',5,5,
 '[{"level_req":5,"sp_cost":1,"effect_value":40,"mp_cost":18,"cooldown_sec":5.0,"desc":"Gây 40 ST xuyên"},
   {"level_req":7,"sp_cost":1,"effect_value":65,"mp_cost":22,"cooldown_sec":4.5,"desc":"Gây 65 ST xuyên"},
   {"level_req":10,"sp_cost":2,"effect_value":95,"mp_cost":28,"cooldown_sec":4.2,"desc":"Gây 95 ST xuyên"},
   {"level_req":14,"sp_cost":2,"effect_value":130,"mp_cost":34,"cooldown_sec":3.8,"desc":"Gây 130 ST xuyên"},
   {"level_req":19,"sp_cost":3,"effect_value":175,"mp_cost":40,"cooldown_sec":3.5,"desc":"Gây 175 ST xuyên"}]',
 'icon_earth_3',NOW());

-- ============================================================
-- [5] THÊM SKILL 2 VÀ 3 CHO HỆ KIM (Metal)
--     Skill 2: METAL_STORM — Bão Kim Loại (Projectile nhiều đạn), trigger "Skill2"
--     Skill 3: METAL_ARMOR — Giáp Thép (Melee/buff), trigger "Skill3"
-- ============================================================
INSERT INTO `skill_template`
  (`skill_code`,`skill_name`,`description`,`element_type`,`max_level`,`level_to_unlock`,`levels_json`,`icon_id`,`created_at`)
VALUES
('METAL_STORM','Bão Kim Loại','Tung ra loạt lưỡi kim loại theo hướng ngang liên tiếp.','Metal',5,3,
 '[{"level_req":3,"sp_cost":1,"effect_value":15,"mp_cost":14,"cooldown_sec":5.0,"desc":"3 mảnh x15 ST"},
   {"level_req":5,"sp_cost":1,"effect_value":25,"mp_cost":18,"cooldown_sec":4.5,"desc":"3 mảnh x25 ST"},
   {"level_req":8,"sp_cost":2,"effect_value":38,"mp_cost":24,"cooldown_sec":4.2,"desc":"4 mảnh x38 ST"},
   {"level_req":11,"sp_cost":2,"effect_value":55,"mp_cost":30,"cooldown_sec":3.8,"desc":"4 mảnh x55 ST"},
   {"level_req":16,"sp_cost":3,"effect_value":75,"mp_cost":36,"cooldown_sec":3.5,"desc":"5 mảnh x75 ST"}]',
 'icon_metal_2',NOW()),
('METAL_ARMOR','Giáp Thép','Khoác lên người lớp giáp kim loại, tăng phòng thủ tạm thời.','Metal',5,5,
 '[{"level_req":5,"sp_cost":1,"effect_value":15,"mp_cost":16,"cooldown_sec":10.0,"desc":"Tăng 15 DEF trong 5s"},
   {"level_req":7,"sp_cost":1,"effect_value":25,"mp_cost":20,"cooldown_sec":9.5,"desc":"Tăng 25 DEF trong 5s"},
   {"level_req":10,"sp_cost":2,"effect_value":38,"mp_cost":25,"cooldown_sec":9.0,"desc":"Tăng 38 DEF trong 6s"},
   {"level_req":14,"sp_cost":2,"effect_value":55,"mp_cost":30,"cooldown_sec":8.5,"desc":"Tăng 55 DEF trong 6s"},
   {"level_req":18,"sp_cost":3,"effect_value":75,"mp_cost":36,"cooldown_sec":8.0,"desc":"Tăng 75 DEF trong 8s"}]',
 'icon_metal_3',NOW());

-- ============================================================
-- [6] THÊM SKILL 2 VÀ 3 CHO HỆ MỘC (Wood)
--     Skill 2: WOOD_ARROW  — Tên Gỗ (Projectile), trigger "Skill2"
--     Skill 3: WOOD_HEAL   — Hồi Sinh (Melee/heal), trigger "Skill3"
-- ============================================================
INSERT INTO `skill_template`
  (`skill_code`,`skill_name`,`description`,`element_type`,`max_level`,`level_to_unlock`,`levels_json`,`icon_id`,`created_at`)
VALUES
('WOOD_ARROW','Tên Gỗ','Bắn mũi tên làm từ gỗ cứng theo hướng player nhìn.','Wood',5,3,
 '[{"level_req":3,"sp_cost":1,"effect_value":20,"mp_cost":10,"cooldown_sec":3.5,"desc":"Gây 20 ST"},
   {"level_req":5,"sp_cost":1,"effect_value":35,"mp_cost":14,"cooldown_sec":3.2,"desc":"Gây 35 ST"},
   {"level_req":7,"sp_cost":1,"effect_value":52,"mp_cost":18,"cooldown_sec":3.0,"desc":"Gây 52 ST"},
   {"level_req":10,"sp_cost":2,"effect_value":75,"mp_cost":23,"cooldown_sec":2.8,"desc":"Gây 75 ST"},
   {"level_req":15,"sp_cost":2,"effect_value":105,"mp_cost":28,"cooldown_sec":2.5,"desc":"Gây 105 ST"}]',
 'icon_wood_2',NOW()),
('WOOD_HEAL','Thảo Dược Hồi','Hấp thụ năng lượng từ thiên nhiên để hồi máu bản thân.','Wood',5,5,
 '[{"level_req":5,"sp_cost":1,"effect_value":50,"mp_cost":22,"cooldown_sec":12.0,"desc":"Hồi 50 HP"},
   {"level_req":7,"sp_cost":1,"effect_value":85,"mp_cost":28,"cooldown_sec":11.0,"desc":"Hồi 85 HP"},
   {"level_req":10,"sp_cost":2,"effect_value":130,"mp_cost":34,"cooldown_sec":10.0,"desc":"Hồi 130 HP"},
   {"level_req":14,"sp_cost":2,"effect_value":185,"mp_cost":40,"cooldown_sec":9.0,"desc":"Hồi 185 HP"},
   {"level_req":18,"sp_cost":3,"effect_value":250,"mp_cost":48,"cooldown_sec":8.0,"desc":"Hồi 250 HP"}]',
 'icon_wood_3',NOW());

-- ============================================================
-- Kiểm tra kết quả toàn bộ
-- ============================================================
SELECT skill_id, skill_code, skill_name, element_type,
       max_level, level_to_unlock
FROM skill_template
ORDER BY COALESCE(element_type,'zzz'),skill_id;
