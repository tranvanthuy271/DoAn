-- ============================================================
-- Migration 048: Thêm cột yêu cầu nhiệm vụ vào map_config
--   required_quest_id: Phải hoàn thành nhiệm vụ này trước khi vào map
--   (Level yêu cầu đã có sẵn: map_config.min_level / max_level)
-- ============================================================

ALTER TABLE map_config
    ADD COLUMN required_quest_id INT NULL DEFAULT NULL
        COMMENT 'ID nhiệm vụ phải hoàn thành (quest_config.id) trước khi vào map. NULL = không yêu cầu.'
    AFTER max_level;

-- Ví dụ: đặt quest yêu cầu cho các map đầu
-- UPDATE map_config SET required_quest_id = 1 WHERE map_id = 1;  -- Map1 cần quest 1
-- UPDATE map_config SET required_quest_id = 2 WHERE map_id = 2;  -- Map2 cần quest 2
