-- =============================================================================
-- Migration: Thêm skills hệ Thổ (Earth Element) vào bảng skills
-- Skill IDs: 18 (EARTH_AURA), 19 (EARTH_BOOMERANG), 20 (EARTH_BLINK)
-- =============================================================================

INSERT INTO skills (skill_id, skill_code, skill_name, skill_description, element, max_level, min_level_req, levels_json, icon_url, created_at) VALUES
(18,'EARTH_AURA','Địa Uy Khí','Phát hào quang đất, tăng sát thương tấn công cho bản thân và đồng đội trong bán kính (Skill 1 hệ Thổ)','Earth',5,1,
 '[{"level_req":1,"sp_cost":1,"effect_value":15,"mp_cost":15,"cooldown_sec":10,"desc":"Buff +15% tấn công 6 giây"},{"level_req":3,"sp_cost":1,"effect_value":20,"mp_cost":18,"cooldown_sec":10,"desc":"Buff +20% tấn công 7 giây"},{"level_req":7,"sp_cost":2,"effect_value":28,"mp_cost":22,"cooldown_sec":9,"desc":"Buff +28% tấn công 8 giây"},{"level_req":12,"sp_cost":2,"effect_value":38,"mp_cost":26,"cooldown_sec":8,"desc":"Buff +38% tấn công 9 giây"},{"level_req":17,"sp_cost":3,"effect_value":50,"mp_cost":30,"cooldown_sec":7,"desc":"Buff +50% tấn công 10 giây"}]',
 'icon_earth_aura',NOW()),
(19,'EARTH_BOOMERANG','Địa Phong Đao','Phóng dao đất theo hướng trước, sau khi bay xong tự quay về tay player (Skill 2 hệ Thổ)','Earth',5,2,
 '[{"level_req":2,"sp_cost":1,"effect_value":30,"mp_cost":12,"cooldown_sec":5,"desc":"Gây 30 ST đi về"},{"level_req":4,"sp_cost":1,"effect_value":50,"mp_cost":16,"cooldown_sec":5,"desc":"Gây 50 ST đi về"},{"level_req":8,"sp_cost":2,"effect_value":75,"mp_cost":20,"cooldown_sec":4.5,"desc":"Gây 75 ST đi về"},{"level_req":12,"sp_cost":2,"effect_value":105,"mp_cost":24,"cooldown_sec":4,"desc":"Gây 105 ST đi về"},{"level_req":17,"sp_cost":3,"effect_value":140,"mp_cost":28,"cooldown_sec":4,"desc":"Gây 140 ST đi về"}]',
 'icon_earth_boomerang',NOW()),
(20,'EARTH_BLINK','Địa Độn Thuật','Dịch chuyển ngắn về phía trước rồi bắn ra đạn DoT, gây sát thương liên tục khi chạm (Skill 3 hệ Thổ)','Earth',5,4,
 '[{"level_req":4,"sp_cost":1,"effect_value":5,"mp_cost":20,"cooldown_sec":7,"desc":"DoT 5 ST/tick × 5 tick"},{"level_req":6,"sp_cost":1,"effect_value":8,"mp_cost":24,"cooldown_sec":7,"desc":"DoT 8 ST/tick × 5 tick"},{"level_req":10,"sp_cost":2,"effect_value":12,"mp_cost":28,"cooldown_sec":6,"desc":"DoT 12 ST/tick × 6 tick"},{"level_req":14,"sp_cost":2,"effect_value":17,"mp_cost":32,"cooldown_sec":6,"desc":"DoT 17 ST/tick × 6 tick"},{"level_req":19,"sp_cost":3,"effect_value":24,"mp_cost":36,"cooldown_sec":5,"desc":"DoT 24 ST/tick × 7 tick"}]',
 'icon_earth_blink',NOW());
