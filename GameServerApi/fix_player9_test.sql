-- Fix player_id=9 (acc Phong) để test Hybrid Fusion ngay
-- primary=Wind tier5, secondary=Metal tier5, có 10x Lõi Đột Biến Kim (id=50)

USE gamedb;

-- 1. Set gene_tier=5, secondary_element='Metal', secondary_gene_tier=5
UPDATE player_data
SET info_char = JSON_SET(
    info_char,
    '$.secondary_element',   'Metal',
    '$.secondary_gene_tier', 5,
    '$.secondary_gene_exp',  0,
    '$.gene_tier',           5,
    '$.gene_exp',            0
),
updated_at = NOW()
WHERE player_id = 9;

-- 2. Set inventory với Lõi Đột Biến Kim (itemTemplateId=50) x10
UPDATE player_data
SET inventory = '[{"slotIndex":0,"itemTemplateId":50,"quantity":10,"itemCode":"Loi Dot Bien Kim","iconId":"0"}]',
updated_at = NOW()
WHERE player_id = 9;

-- 3. Verify
SELECT
    player_id,
    character_name,
    JSON_UNQUOTE(JSON_EXTRACT(info_char, '$.element_type'))        AS element,
    JSON_EXTRACT(info_char, '$.gene_tier')                         AS gene_tier,
    JSON_UNQUOTE(JSON_EXTRACT(info_char, '$.secondary_element'))   AS secondary_element,
    JSON_EXTRACT(info_char, '$.secondary_gene_tier')               AS secondary_gene_tier,
    inventory
FROM player_data
WHERE player_id = 9;
