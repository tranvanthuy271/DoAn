-- Rebuild player skill master data for the character skill tab.
-- Keeps existing production skill_ids where possible so stored player_data.skills stays valid.

SET FOREIGN_KEY_CHECKS = 0;

UPDATE player_skill_record
SET skill_id = 1
WHERE skill_id = 41;

UPDATE player_data
SET skills = REPLACE(skills, '"skill_id":41', '"skill_id":1')
WHERE skills LIKE '%"skill_id":41%';

UPDATE player_data
SET skills = REPLACE(skills, '"skillCode":"WATER_SHIELD"', '"skillCode":"WATER_ARMOR"')
WHERE skills LIKE '%"skillCode":"WATER_SHIELD"%';

DELETE FROM skill_template;

INSERT INTO skill_template
(skill_id, skill_code, skill_name, description, element_type, max_level, level_to_unlock, levels_json, icon_id, created_at, gene_tier_required, hybrid_id)
VALUES
(1, 'NORMAL_ATTACK', 'Kiếm thuật cơ bản', 'Kiếm thuật giết gà, chặt củi, gây sát thương nhẹ lên quái.', NULL, 1, 1,
'[{"level_req":1,"sp_cost":0,"effect_value":10,"mp_cost":0,"cooldown_sec":0.8,"desc":"Gây 10 sát thương cơ bản"}]',
'icon_normal_attack', NOW(), 0, NULL),

(5, 'DASH', 'Lướt nhanh', 'Lướt nhanh về phía trước để né đòn hoặc rút ngắn khoảng cách.', NULL, 5, 1,
'[{"level_req":1,"sp_cost":1,"effect_value":1,"mp_cost":8,"cooldown_sec":4.0,"desc":"Lướt 1 ô"},{"level_req":3,"sp_cost":1,"effect_value":2,"mp_cost":10,"cooldown_sec":3.5,"desc":"Lướt 2 ô"},{"level_req":6,"sp_cost":1,"effect_value":3,"mp_cost":12,"cooldown_sec":3.0,"desc":"Lướt 3 ô"},{"level_req":10,"sp_cost":2,"effect_value":4,"mp_cost":14,"cooldown_sec":2.5,"desc":"Lướt 4 ô"},{"level_req":15,"sp_cost":2,"effect_value":5,"mp_cost":16,"cooldown_sec":2.0,"desc":"Lướt 5 ô"}]',
'icon_skill_5', NOW(), 0, NULL),

(8, 'WOOD_VINE', 'Dây leo cây', 'Triệu hồi dây leo trói mục tiêu, giữ chân kẻ địch trong thời gian ngắn.', 'Wood', 5, 1,
'[{"level_req":1,"sp_cost":1,"effect_value":1,"mp_cost":14,"cooldown_sec":6.0,"desc":"Trói 1 giây"},{"level_req":3,"sp_cost":1,"effect_value":2,"mp_cost":18,"cooldown_sec":5.5,"desc":"Trói 2 giây"},{"level_req":5,"sp_cost":2,"effect_value":3,"mp_cost":22,"cooldown_sec":5.0,"desc":"Trói 3 giây"},{"level_req":8,"sp_cost":2,"effect_value":4,"mp_cost":26,"cooldown_sec":4.5,"desc":"Trói 4 giây"},{"level_req":12,"sp_cost":3,"effect_value":5,"mp_cost":30,"cooldown_sec":4.0,"desc":"Trói 5 giây"}]',
'icon_skill_8', NOW(), 0, NULL),

(9, 'WIND_STRIKE', 'Chưởng phong', 'Tung luồng khí phong cận chiến, gây sát thương nhanh lên kẻ địch trước mặt.', 'Wind', 5, 1,
'[{"level_req":1,"sp_cost":1,"effect_value":18,"mp_cost":8,"cooldown_sec":3.0,"desc":"Gây 18 sát thương"},{"level_req":3,"sp_cost":1,"effect_value":32,"mp_cost":12,"cooldown_sec":2.8,"desc":"Gây 32 sát thương"},{"level_req":5,"sp_cost":1,"effect_value":50,"mp_cost":16,"cooldown_sec":2.5,"desc":"Gây 50 sát thương"},{"level_req":8,"sp_cost":2,"effect_value":75,"mp_cost":20,"cooldown_sec":2.2,"desc":"Gây 75 sát thương"},{"level_req":12,"sp_cost":2,"effect_value":105,"mp_cost":25,"cooldown_sec":2.0,"desc":"Gây 105 sát thương"}]',
'icon_wind_1', NOW(), 0, NULL),

(10, 'WIND_BLADE', 'Phong nhận', 'Tạo lưỡi gió sắc quét diện rộng quanh thân, phù hợp dọn nhiều mục tiêu.', 'Wind', 5, 3,
'[{"level_req":3,"sp_cost":1,"effect_value":35,"mp_cost":12,"cooldown_sec":4.0,"desc":"Gây 35 sát thương diện rộng"},{"level_req":5,"sp_cost":1,"effect_value":55,"mp_cost":16,"cooldown_sec":3.5,"desc":"Gây 55 sát thương diện rộng"},{"level_req":7,"sp_cost":2,"effect_value":80,"mp_cost":20,"cooldown_sec":3.0,"desc":"Gây 80 sát thương diện rộng"},{"level_req":10,"sp_cost":2,"effect_value":115,"mp_cost":25,"cooldown_sec":2.8,"desc":"Gây 115 sát thương diện rộng"},{"level_req":15,"sp_cost":3,"effect_value":160,"mp_cost":30,"cooldown_sec":2.5,"desc":"Gây 160 sát thương diện rộng"}]',
'icon_wind_2', NOW(), 0, NULL),

(11, 'WIND_STEP', 'Phong thoái bộ', 'Ẩn thân vào gió rồi lao tới trước bằng tốc độ phong hệ.', 'Wind', 5, 5,
'[{"level_req":5,"sp_cost":1,"effect_value":3,"mp_cost":15,"cooldown_sec":8.0,"desc":"Dịch chuyển 3 ô"},{"level_req":7,"sp_cost":1,"effect_value":4,"mp_cost":18,"cooldown_sec":7.0,"desc":"Dịch chuyển 4 ô"},{"level_req":9,"sp_cost":2,"effect_value":5,"mp_cost":22,"cooldown_sec":6.5,"desc":"Dịch chuyển 5 ô"},{"level_req":12,"sp_cost":2,"effect_value":6,"mp_cost":26,"cooldown_sec":6.0,"desc":"Dịch chuyển 6 ô"},{"level_req":16,"sp_cost":3,"effect_value":8,"mp_cost":30,"cooldown_sec":5.0,"desc":"Dịch chuyển 8 ô"}]',
'icon_wind_3', NOW(), 0, NULL),

(12, 'FIRE_BURST', 'Hỏa cầu', 'Bắn một cầu lửa lớn, bay chậm hơn nhưng gây sát thương cao.', 'Fire', 5, 2,
'[{"level_req":2,"sp_cost":1,"effect_value":35,"mp_cost":15,"cooldown_sec":5.0,"desc":"Gây 35 sát thương"},{"level_req":4,"sp_cost":1,"effect_value":60,"mp_cost":18,"cooldown_sec":5.0,"desc":"Gây 60 sát thương"},{"level_req":7,"sp_cost":2,"effect_value":90,"mp_cost":22,"cooldown_sec":4.5,"desc":"Gây 90 sát thương"},{"level_req":11,"sp_cost":2,"effect_value":130,"mp_cost":26,"cooldown_sec":4.0,"desc":"Gây 130 sát thương"},{"level_req":16,"sp_cost":3,"effect_value":180,"mp_cost":30,"cooldown_sec":4.0,"desc":"Gây 180 sát thương"}]',
'icon_fire_burst', NOW(), 0, NULL),

(13, 'WATER_PILLAR', 'Thánh mộc hạ', 'Triệu hồi cột nước rơi từ trên cao, gây sát thương diện rộng tại vùng đáp.', 'Water', 5, 3,
'[{"level_req":3,"sp_cost":1,"effect_value":40,"mp_cost":16,"cooldown_sec":6.0,"desc":"Gây 40 sát thương"},{"level_req":5,"sp_cost":1,"effect_value":70,"mp_cost":20,"cooldown_sec":6.0,"desc":"Gây 70 sát thương"},{"level_req":8,"sp_cost":2,"effect_value":105,"mp_cost":24,"cooldown_sec":5.5,"desc":"Gây 105 sát thương"},{"level_req":12,"sp_cost":2,"effect_value":150,"mp_cost":28,"cooldown_sec":5.0,"desc":"Gây 150 sát thương"},{"level_req":18,"sp_cost":3,"effect_value":200,"mp_cost":32,"cooldown_sec":4.5,"desc":"Gây 200 sát thương"}]',
'icon_water_pillar', NOW(), 0, NULL),

(14, 'WATER_ARMOR', 'Thủy giáp hộ thể', 'Bao phủ bản thân và đồng đội gần đó bằng giáp nước, tăng phòng thủ tạm thời.', 'Water', 5, 5,
'[{"level_req":5,"sp_cost":1,"effect_value":15,"mp_cost":20,"cooldown_sec":12.0,"desc":"Tăng 15 giáp trong 5 giây"},{"level_req":8,"sp_cost":1,"effect_value":20,"mp_cost":25,"cooldown_sec":11.0,"desc":"Tăng 20 giáp trong 5 giây"},{"level_req":11,"sp_cost":2,"effect_value":28,"mp_cost":28,"cooldown_sec":10.0,"desc":"Tăng 28 giáp trong 6 giây"},{"level_req":15,"sp_cost":2,"effect_value":38,"mp_cost":30,"cooldown_sec":9.0,"desc":"Tăng 38 giáp trong 6 giây"},{"level_req":20,"sp_cost":3,"effect_value":50,"mp_cost":35,"cooldown_sec":8.0,"desc":"Tăng 50 giáp trong 7 giây"}]',
'icon_water_armor', NOW(), 0, NULL),

(15, 'FIRE_BOLT', 'Hỏa đạn', 'Bắn đạn lửa theo hướng nhìn, gây sát thương đơn mục tiêu ổn định.', 'Fire', 5, 1,
'[{"level_req":1,"sp_cost":1,"effect_value":20,"mp_cost":10,"cooldown_sec":3.0,"desc":"Gây 20 sát thương"},{"level_req":3,"sp_cost":1,"effect_value":35,"mp_cost":13,"cooldown_sec":3.0,"desc":"Gây 35 sát thương"},{"level_req":6,"sp_cost":1,"effect_value":55,"mp_cost":16,"cooldown_sec":2.5,"desc":"Gây 55 sát thương"},{"level_req":9,"sp_cost":2,"effect_value":80,"mp_cost":20,"cooldown_sec":2.0,"desc":"Gây 80 sát thương"},{"level_req":14,"sp_cost":2,"effect_value":110,"mp_cost":24,"cooldown_sec":2.0,"desc":"Gây 110 sát thương"}]',
'icon_fire_bolt', NOW(), 0, NULL),

(16, 'WATER_BOLT', 'Thủy đạn', 'Bắn đạn nước theo hướng nhìn, gây sát thương đơn mục tiêu.', 'Water', 5, 1,
'[{"level_req":1,"sp_cost":1,"effect_value":20,"mp_cost":10,"cooldown_sec":3.0,"desc":"Gây 20 sát thương"},{"level_req":3,"sp_cost":1,"effect_value":35,"mp_cost":13,"cooldown_sec":3.0,"desc":"Gây 35 sát thương"},{"level_req":6,"sp_cost":1,"effect_value":55,"mp_cost":16,"cooldown_sec":2.5,"desc":"Gây 55 sát thương"},{"level_req":9,"sp_cost":2,"effect_value":80,"mp_cost":20,"cooldown_sec":2.0,"desc":"Gây 80 sát thương"},{"level_req":14,"sp_cost":2,"effect_value":110,"mp_cost":24,"cooldown_sec":2.0,"desc":"Gây 110 sát thương"}]',
'icon_water_bolt', NOW(), 0, NULL),

(17, 'FIRE_RAIN', 'Thiên hỏa', 'Triệu hồi mưa lửa rơi xuống vùng trước mặt, gây sát thương nhiều lần.', 'Fire', 5, 4,
'[{"level_req":4,"sp_cost":1,"effect_value":25,"mp_cost":20,"cooldown_sec":8.0,"desc":"5 cầu lửa, 25 sát thương mỗi cầu"},{"level_req":6,"sp_cost":1,"effect_value":40,"mp_cost":24,"cooldown_sec":8.0,"desc":"5 cầu lửa, 40 sát thương mỗi cầu"},{"level_req":9,"sp_cost":2,"effect_value":60,"mp_cost":28,"cooldown_sec":7.0,"desc":"6 cầu lửa, 60 sát thương mỗi cầu"},{"level_req":13,"sp_cost":2,"effect_value":85,"mp_cost":32,"cooldown_sec":6.5,"desc":"7 cầu lửa, 85 sát thương mỗi cầu"},{"level_req":18,"sp_cost":3,"effect_value":115,"mp_cost":36,"cooldown_sec":6.0,"desc":"8 cầu lửa, 115 sát thương mỗi cầu"}]',
'icon_fire_rain', NOW(), 0, NULL),

(18, 'WOOD_ARROW', 'Tên gỗ', 'Bắn mũi tên gỗ cứng theo hướng nhìn, gây sát thương chính xác.', 'Wood', 5, 3,
'[{"level_req":3,"sp_cost":1,"effect_value":20,"mp_cost":10,"cooldown_sec":3.5,"desc":"Gây 20 sát thương"},{"level_req":5,"sp_cost":1,"effect_value":35,"mp_cost":14,"cooldown_sec":3.2,"desc":"Gây 35 sát thương"},{"level_req":7,"sp_cost":1,"effect_value":52,"mp_cost":18,"cooldown_sec":3.0,"desc":"Gây 52 sát thương"},{"level_req":10,"sp_cost":2,"effect_value":75,"mp_cost":23,"cooldown_sec":2.8,"desc":"Gây 75 sát thương"},{"level_req":15,"sp_cost":2,"effect_value":105,"mp_cost":28,"cooldown_sec":2.5,"desc":"Gây 105 sát thương"}]',
'icon_wood_2', NOW(), 0, NULL),

(19, 'WOOD_HEAL', 'Thảo dược hồi', 'Hấp thụ sinh khí thiên nhiên để hồi máu cho bản thân.', 'Wood', 5, 5,
'[{"level_req":5,"sp_cost":1,"effect_value":50,"mp_cost":22,"cooldown_sec":12.0,"desc":"Hồi 50 HP"},{"level_req":7,"sp_cost":1,"effect_value":85,"mp_cost":28,"cooldown_sec":11.0,"desc":"Hồi 85 HP"},{"level_req":10,"sp_cost":2,"effect_value":130,"mp_cost":34,"cooldown_sec":10.0,"desc":"Hồi 130 HP"},{"level_req":14,"sp_cost":2,"effect_value":185,"mp_cost":40,"cooldown_sec":9.0,"desc":"Hồi 185 HP"},{"level_req":18,"sp_cost":3,"effect_value":250,"mp_cost":48,"cooldown_sec":8.0,"desc":"Hồi 250 HP"}]',
'icon_wood_3', NOW(), 0, NULL),

(20, 'METAL_STRIKE', 'Kim phong', 'Chém cận chiến bằng lưỡi kim loại sắc, gây sát thương trước mặt.', 'Metal', 5, 1,
'[{"level_req":1,"sp_cost":1,"effect_value":20,"mp_cost":8,"cooldown_sec":3.0,"desc":"Gây 20 sát thương cận chiến"},{"level_req":3,"sp_cost":1,"effect_value":38,"mp_cost":12,"cooldown_sec":3.0,"desc":"Gây 38 sát thương"},{"level_req":5,"sp_cost":2,"effect_value":60,"mp_cost":16,"cooldown_sec":2.5,"desc":"Gây 60 sát thương"},{"level_req":8,"sp_cost":2,"effect_value":88,"mp_cost":20,"cooldown_sec":2.5,"desc":"Gây 88 sát thương"},{"level_req":12,"sp_cost":3,"effect_value":120,"mp_cost":24,"cooldown_sec":2.0,"desc":"Gây 120 sát thương"}]',
'icon_metal_strike', NOW(), 0, NULL),

(21, 'METAL_BLADE', 'Kim nhẫn', 'Tạo lưỡi kim loại xoay quanh thân, quét nhiều kẻ địch gần đó.', 'Metal', 5, 3,
'[{"level_req":3,"sp_cost":1,"effect_value":30,"mp_cost":14,"cooldown_sec":4.0,"desc":"Gây 30 sát thương diện rộng"},{"level_req":5,"sp_cost":1,"effect_value":55,"mp_cost":18,"cooldown_sec":4.0,"desc":"Gây 55 sát thương"},{"level_req":8,"sp_cost":2,"effect_value":85,"mp_cost":22,"cooldown_sec":3.5,"desc":"Gây 85 sát thương"},{"level_req":12,"sp_cost":2,"effect_value":120,"mp_cost":26,"cooldown_sec":3.5,"desc":"Gây 120 sát thương"},{"level_req":18,"sp_cost":3,"effect_value":165,"mp_cost":30,"cooldown_sec":3.0,"desc":"Gây 165 sát thương"}]',
'icon_metal_blade', NOW(), 0, NULL),

(22, 'METAL_SHIELD', 'Kim cương khiên', 'Tạo khiên kim cương, miễn nhiễm sát thương trong thời gian ngắn.', 'Metal', 5, 5,
'[{"level_req":5,"sp_cost":1,"effect_value":3,"mp_cost":20,"cooldown_sec":12.0,"desc":"Bất tử 3 giây"},{"level_req":8,"sp_cost":1,"effect_value":4,"mp_cost":25,"cooldown_sec":11.0,"desc":"Bất tử 4 giây"},{"level_req":11,"sp_cost":2,"effect_value":5,"mp_cost":28,"cooldown_sec":10.0,"desc":"Bất tử 5 giây"},{"level_req":15,"sp_cost":2,"effect_value":6,"mp_cost":30,"cooldown_sec":9.0,"desc":"Bất tử 6 giây"},{"level_req":20,"sp_cost":3,"effect_value":8,"mp_cost":35,"cooldown_sec":8.0,"desc":"Bất tử 8 giây"}]',
'icon_metal_shield', NOW(), 0, NULL),

(23, 'EARTH_AURA', 'Địa uy khí', 'Phát hào quang đất, tăng sức tấn công cho bản thân và đồng đội xung quanh.', 'Earth', 5, 1,
'[{"level_req":1,"sp_cost":1,"effect_value":15,"mp_cost":15,"cooldown_sec":10.0,"desc":"Tăng 15% tấn công trong 6 giây"},{"level_req":3,"sp_cost":1,"effect_value":20,"mp_cost":18,"cooldown_sec":10.0,"desc":"Tăng 20% tấn công trong 7 giây"},{"level_req":7,"sp_cost":2,"effect_value":28,"mp_cost":22,"cooldown_sec":9.0,"desc":"Tăng 28% tấn công trong 8 giây"},{"level_req":12,"sp_cost":2,"effect_value":38,"mp_cost":26,"cooldown_sec":8.0,"desc":"Tăng 38% tấn công trong 9 giây"},{"level_req":17,"sp_cost":3,"effect_value":50,"mp_cost":30,"cooldown_sec":7.0,"desc":"Tăng 50% tấn công trong 10 giây"}]',
'icon_earth_aura', NOW(), 0, NULL),

(24, 'EARTH_BOOMERANG', 'Địa phong đao', 'Phóng dao đất bay tới rồi quay về, có thể gây sát thương hai lượt.', 'Earth', 5, 2,
'[{"level_req":2,"sp_cost":1,"effect_value":30,"mp_cost":12,"cooldown_sec":5.0,"desc":"Gây 30 sát thương lượt đi/về"},{"level_req":4,"sp_cost":1,"effect_value":50,"mp_cost":16,"cooldown_sec":5.0,"desc":"Gây 50 sát thương lượt đi/về"},{"level_req":8,"sp_cost":2,"effect_value":75,"mp_cost":20,"cooldown_sec":4.5,"desc":"Gây 75 sát thương lượt đi/về"},{"level_req":12,"sp_cost":2,"effect_value":105,"mp_cost":24,"cooldown_sec":4.0,"desc":"Gây 105 sát thương lượt đi/về"},{"level_req":17,"sp_cost":3,"effect_value":140,"mp_cost":28,"cooldown_sec":4.0,"desc":"Gây 140 sát thương lượt đi/về"}]',
'icon_earth_boomerang', NOW(), 0, NULL),

(25, 'EARTH_BLINK', 'Địa độn thuật', 'Độn đất ngắn về phía trước rồi bắn đạn gây sát thương theo thời gian.', 'Earth', 5, 4,
'[{"level_req":4,"sp_cost":1,"effect_value":5,"mp_cost":20,"cooldown_sec":7.0,"desc":"5 sát thương/tick x 5 tick"},{"level_req":6,"sp_cost":1,"effect_value":8,"mp_cost":24,"cooldown_sec":7.0,"desc":"8 sát thương/tick x 5 tick"},{"level_req":10,"sp_cost":2,"effect_value":12,"mp_cost":28,"cooldown_sec":6.0,"desc":"12 sát thương/tick x 6 tick"},{"level_req":14,"sp_cost":2,"effect_value":17,"mp_cost":32,"cooldown_sec":6.0,"desc":"17 sát thương/tick x 6 tick"},{"level_req":19,"sp_cost":3,"effect_value":24,"mp_cost":36,"cooldown_sec":5.0,"desc":"24 sát thương/tick x 7 tick"}]',
'icon_earth_blink', NOW(), 0, NULL),

(26, 'HYBRID_FIRE_EARTH_LAVA_AURA', 'Hỏa Thổ Dung Nham', 'Dung nham bao quanh người chơi, gây sát thương theo thời gian và chặn hồi phục của mục tiêu.', NULL, 1, 1,
'[{"level_req":1,"sp_cost":0,"effect_value":280,"mp_cost":50,"cooldown_sec":14.0,"desc":"Gây sát thương dung nham diện rộng"}]',
'icon_hybrid_101', NOW(), 0, 1),

(35, 'HYBRID_WATER_WOOD_VENOM', 'Băng Độc Vĩnh Cửu', 'Tạo hồ nước độc đóng băng dưới chân kẻ địch, gây Slow, DoT và giảm tấn công.', NULL, 1, 1,
'[{"level_req":1,"sp_cost":0,"effect_value":250,"mp_cost":50,"cooldown_sec":16.0,"desc":"Slow, độc và giảm tấn công"}]',
'icon_hybrid_110', NOW(), 0, 10),

(38, 'HYBRID_METAL_WIND_BARRAGE', 'Kim Phong Liên Tiễn', 'Phóng loạt mũi tên gió kim loại theo hình nan quạt, xuyên qua nhiều kẻ địch.', NULL, 1, 1,
'[{"level_req":1,"sp_cost":0,"effect_value":295,"mp_cost":55,"cooldown_sec":13.0,"desc":"Bắn loạt tên kim phong xuyên mục tiêu"}]',
'icon_hybrid_113', NOW(), 0, 13);

DELETE FROM gene_hybrid_skill
WHERE hybrid_id IN (1, 10, 13);

INSERT INTO gene_hybrid_skill (hybrid_id, skill_code, slot_priority)
VALUES
(1, 'HYBRID_FIRE_EARTH_LAVA_AURA', 3),
(10, 'HYBRID_WATER_WOOD_VENOM', 3),
(13, 'HYBRID_METAL_WIND_BARRAGE', 3);

ALTER TABLE skill_template AUTO_INCREMENT = 42;

SET FOREIGN_KEY_CHECKS = 1;
