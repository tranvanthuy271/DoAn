-- =============================================================
-- Migration: Thêm skill NORMAL_ATTACK (Đánh Thường) vào skill_template
-- Áp dụng cho: gamedb
-- Ngày: 2026-03-25
-- Ghi chú: Skill đánh thường dùng chung cho TẤT CẢ player prefab
--          (Phong, Hỏa, Thủy, Thổ, Kim, Mộc). Kích hoạt bằng Z / LMB.
--          element_type = NULL => hiển thị cho mọi class.
-- =============================================================

-- Thêm row NORMAL_ATTACK nếu chưa tồn tại
INSERT INTO `skill_template`
    (`skill_code`, `skill_name`, `description`, `element_type`,
     `max_level`, `level_to_unlock`, `levels_json`, `icon_id`,
     `gene_tier_required`, `hybrid_id`)
SELECT
    'NORMAL_ATTACK',
    'Đánh Thường',
    'Đòn tấn công cơ bản, không tiêu hao MP. Sát thương tăng khi nâng cấp.',
    NULL,
    5,
    1,
    '[{"level_req":1,"sp_cost":1,"effect_value":10,"mp_cost":0,"cooldown_sec":0.8,"desc":"Gây 10 ST cơ bản"},
      {"level_req":5,"sp_cost":1,"effect_value":18,"mp_cost":0,"cooldown_sec":0.75,"desc":"Gây 18 ST"},
      {"level_req":10,"sp_cost":1,"effect_value":30,"mp_cost":0,"cooldown_sec":0.7,"desc":"Gây 30 ST"},
      {"level_req":20,"sp_cost":2,"effect_value":48,"mp_cost":0,"cooldown_sec":0.65,"desc":"Gây 48 ST"},
      {"level_req":35,"sp_cost":2,"effect_value":72,"mp_cost":0,"cooldown_sec":0.6,"desc":"Gây 72 ST"}]',
    'icon_normal_attack',
    0,
    NULL
WHERE NOT EXISTS (
    SELECT 1 FROM `skill_template` WHERE `skill_code` = 'NORMAL_ATTACK'
);

-- Xác nhận kết quả
SELECT skill_id, skill_code, skill_name, element_type, max_level
FROM `skill_template`
WHERE `skill_code` = 'NORMAL_ATTACK';
