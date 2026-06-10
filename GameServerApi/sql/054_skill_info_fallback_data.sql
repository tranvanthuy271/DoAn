-- Fill missing skill info for the character SkillInfoScrollView.
-- Safe to run repeatedly: only updates empty description / empty levels_json.

UPDATE skill_template
SET description = CASE
    WHEN description IS NOT NULL AND TRIM(description) <> '' THEN description
    WHEN skill_code = 'NORMAL_ATTACK' THEN 'Don danh co ban, luon mo san va khong ton MP.'
    WHEN skill_code = 'DASH' THEN 'Luot nhanh de ne don hoac rut ngan khoang cach.'
    WHEN skill_code LIKE '%STEP%' THEN 'Di chuyen nhanh theo huong dang nhin.'
    WHEN skill_code LIKE '%VINE%' THEN 'Khong che muc tieu trong thoi gian ngan.'
    WHEN skill_code LIKE '%HEAL%' THEN 'Hoi HP cho ban than.'
    WHEN skill_code LIKE '%ARMOR%' THEN 'Tang phong thu tam thoi.'
    WHEN skill_code LIKE '%SHIELD%' THEN 'Tao trang thai bao ve trong thoi gian ngan.'
    WHEN skill_code LIKE '%AURA%' THEN 'Tang suc tan cong tam thoi.'
    WHEN skill_code LIKE 'HYBRID_%' THEN 'Ky nang dung hop gene, mo khi nhan vat da fusion dung he.'
    ELSE 'Ky nang chien dau gay sat thuong len muc tieu.'
END,
levels_json = CASE
    WHEN levels_json IS NOT NULL AND TRIM(levels_json) <> '' AND levels_json <> '[]' THEN levels_json
    WHEN skill_code = 'NORMAL_ATTACK' THEN
        '[{"level_req":1,"sp_cost":0,"effect_value":10,"mp_cost":0,"cooldown_sec":0.8,"desc":"Gay 10 sat thuong co ban"}]'
    WHEN skill_code = 'DASH' THEN
        '[{"level_req":1,"sp_cost":1,"effect_value":1,"mp_cost":8,"cooldown_sec":4.0,"desc":"Luot 1 o"},{"level_req":3,"sp_cost":1,"effect_value":2,"mp_cost":10,"cooldown_sec":3.5,"desc":"Luot 2 o"},{"level_req":6,"sp_cost":1,"effect_value":3,"mp_cost":12,"cooldown_sec":3.0,"desc":"Luot 3 o"},{"level_req":10,"sp_cost":2,"effect_value":4,"mp_cost":14,"cooldown_sec":2.5,"desc":"Luot 4 o"},{"level_req":15,"sp_cost":2,"effect_value":5,"mp_cost":16,"cooldown_sec":2.0,"desc":"Luot 5 o"}]'
    WHEN skill_code LIKE '%HEAL%' THEN
        '[{"level_req":1,"sp_cost":1,"effect_value":50,"mp_cost":22,"cooldown_sec":12.0,"desc":"Hoi 50 HP"},{"level_req":4,"sp_cost":1,"effect_value":85,"mp_cost":26,"cooldown_sec":11.0,"desc":"Hoi 85 HP"},{"level_req":7,"sp_cost":2,"effect_value":130,"mp_cost":30,"cooldown_sec":10.0,"desc":"Hoi 130 HP"},{"level_req":10,"sp_cost":2,"effect_value":185,"mp_cost":36,"cooldown_sec":9.0,"desc":"Hoi 185 HP"},{"level_req":13,"sp_cost":3,"effect_value":250,"mp_cost":42,"cooldown_sec":8.0,"desc":"Hoi 250 HP"}]'
    WHEN skill_code LIKE '%ARMOR%' THEN
        '[{"level_req":1,"sp_cost":1,"effect_value":15,"mp_cost":20,"cooldown_sec":12.0,"desc":"Tang 15 phong thu"},{"level_req":4,"sp_cost":1,"effect_value":23,"mp_cost":24,"cooldown_sec":11.0,"desc":"Tang 23 phong thu"},{"level_req":7,"sp_cost":2,"effect_value":31,"mp_cost":28,"cooldown_sec":10.0,"desc":"Tang 31 phong thu"},{"level_req":10,"sp_cost":2,"effect_value":39,"mp_cost":32,"cooldown_sec":9.0,"desc":"Tang 39 phong thu"},{"level_req":13,"sp_cost":3,"effect_value":47,"mp_cost":36,"cooldown_sec":8.0,"desc":"Tang 47 phong thu"}]'
    WHEN skill_code LIKE '%SHIELD%' THEN
        '[{"level_req":1,"sp_cost":1,"effect_value":3,"mp_cost":20,"cooldown_sec":12.0,"desc":"Bao ve 3 giay"},{"level_req":4,"sp_cost":1,"effect_value":4,"mp_cost":24,"cooldown_sec":11.0,"desc":"Bao ve 4 giay"},{"level_req":7,"sp_cost":2,"effect_value":5,"mp_cost":28,"cooldown_sec":10.0,"desc":"Bao ve 5 giay"},{"level_req":10,"sp_cost":2,"effect_value":6,"mp_cost":32,"cooldown_sec":9.0,"desc":"Bao ve 6 giay"},{"level_req":13,"sp_cost":3,"effect_value":7,"mp_cost":36,"cooldown_sec":8.0,"desc":"Bao ve 7 giay"}]'
    WHEN skill_code LIKE '%AURA%' THEN
        '[{"level_req":1,"sp_cost":1,"effect_value":15,"mp_cost":15,"cooldown_sec":10.0,"desc":"Tang 15% tan cong"},{"level_req":4,"sp_cost":1,"effect_value":23,"mp_cost":19,"cooldown_sec":9.5,"desc":"Tang 23% tan cong"},{"level_req":7,"sp_cost":2,"effect_value":31,"mp_cost":23,"cooldown_sec":9.0,"desc":"Tang 31% tan cong"},{"level_req":10,"sp_cost":2,"effect_value":39,"mp_cost":27,"cooldown_sec":8.5,"desc":"Tang 39% tan cong"},{"level_req":13,"sp_cost":3,"effect_value":47,"mp_cost":31,"cooldown_sec":8.0,"desc":"Tang 47% tan cong"}]'
    WHEN skill_code LIKE '%STEP%' THEN
        '[{"level_req":1,"sp_cost":1,"effect_value":3,"mp_cost":15,"cooldown_sec":8.0,"desc":"Di chuyen 3 o"},{"level_req":4,"sp_cost":1,"effect_value":4,"mp_cost":19,"cooldown_sec":7.5,"desc":"Di chuyen 4 o"},{"level_req":7,"sp_cost":2,"effect_value":5,"mp_cost":23,"cooldown_sec":7.0,"desc":"Di chuyen 5 o"},{"level_req":10,"sp_cost":2,"effect_value":6,"mp_cost":27,"cooldown_sec":6.5,"desc":"Di chuyen 6 o"},{"level_req":13,"sp_cost":3,"effect_value":7,"mp_cost":31,"cooldown_sec":6.0,"desc":"Di chuyen 7 o"}]'
    WHEN skill_code LIKE 'HYBRID_%' THEN
        '[{"level_req":1,"sp_cost":0,"effect_value":280,"mp_cost":50,"cooldown_sec":14.0,"desc":"Ky nang fusion gay hieu ung manh"}]'
    ELSE
        '[{"level_req":1,"sp_cost":1,"effect_value":20,"mp_cost":10,"cooldown_sec":3.0,"desc":"Gay 20 sat thuong"},{"level_req":4,"sp_cost":1,"effect_value":38,"mp_cost":14,"cooldown_sec":2.8,"desc":"Gay 38 sat thuong"},{"level_req":7,"sp_cost":2,"effect_value":56,"mp_cost":18,"cooldown_sec":2.6,"desc":"Gay 56 sat thuong"},{"level_req":10,"sp_cost":2,"effect_value":74,"mp_cost":22,"cooldown_sec":2.4,"desc":"Gay 74 sat thuong"},{"level_req":13,"sp_cost":3,"effect_value":92,"mp_cost":26,"cooldown_sec":2.2,"desc":"Gay 92 sat thuong"}]'
END;
