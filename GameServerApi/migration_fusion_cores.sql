-- ============================================================
-- MIGRATION: migration_fusion_cores.sql
-- Thêm 6 Lõi Đột Biến theo từng hệ nguyên tố
-- để dùng trong Hybrid Fusion thay cho item generic id=31.
--
-- Logic:
--   + Khi hệ chính (primary) là X → cần Lõi Đột Biến của hệ phụ (secondary Y)
--   Ví dụ: primary=Phong, secondary=Kim → cần Lõi Đột Biến Kim (id=50)
--          primary=Kim, secondary=Phong → cần Lõi Đột Biến Phong (id=52)
--
-- Mapping element → item_id:
--   Fire  → 47 | Water → 48 | Earth → 49
--   Metal → 50 | Wood  → 51 | Wind  → 52
--
-- IDs không trùng với:
--   1-31  (gamedb.sql)
--   32-40 (migration_map_dungeon_system.sql: Cuộn/Chìa Khóa/Mảnh Hồn)
--   41-46 (migration_equipment_full.sql: Tinh Chất Phong + Đá NC cấp 8-12)
-- ============================================================

-- ── 1. THÊM ITEM TEMPLATE ──────────────────────────────────
INSERT IGNORE INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`)
VALUES
(47, 'Lõi Đột Biến Hỏa',
 'Lõi mang tinh hoa hệ Hỏa. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Hỏa.',
 'True', 2, 25, 1, 0, 50, 0, -1, 0),

(48, 'Lõi Đột Biến Thủy',
 'Lõi mang tinh hoa hệ Thủy. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Thủy.',
 'True', 2, 25, 2, 0, 50, 0, -1, 0),

(49, 'Lõi Đột Biến Thổ',
 'Lõi mang tinh hoa hệ Thổ. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Thổ.',
 'True', 2, 25, 3, 0, 50, 0, -1, 0),

(50, 'Lõi Đột Biến Kim',
 'Lõi mang tinh hoa hệ Kim. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Kim.',
 'True', 2, 25, 4, 0, 50, 0, -1, 0),

(51, 'Lõi Đột Biến Mộc',
 'Lõi mang tinh hoa hệ Mộc. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Mộc.',
 'True', 2, 25, 5, 0, 50, 0, -1, 0),

(52, 'Lõi Đột Biến Phong',
 'Lõi mang tinh hoa hệ Phong. Dùng để thực hiện Hybrid Fusion khi hệ phụ là Phong.',
 'True', 2, 25, 6, 0, 50, 0, -1, 0);

-- ── 2. CẬP NHẬT gene_hybrid_config ───────────────────────
-- Bảng NormalizeKey sắp xếp (element_a < element_b) theo alphabet.
-- => element_a và element_b ko phản ánh primary/secondary của player.
-- Controller sẽ tự luận ra fusionItemId dựa vào secondary element,
-- nhưng ta vẫn lưu thêm 2 cột để tham khảo / báo cáo:
--   fusion_item_id   = core khi element_a là secondary (element_b là primary)
--   fusion_item_id_b = core khi element_b là secondary (element_a là primary)
-- NOTE: Controller KHÔNG dùng cột này nữa — dùng mapping tĩnh trong code.
-- Row cập nhật dưới đây chỉ để tài liệu hóa đúng item cho từng cặp.

-- Cặp Earth+Fire   (row element_a=Earth, element_b=Fire)
--   primary=Fire   → secondary=Earth → cần id=49 (Lõi Thổ)
--   primary=Earth  → secondary=Fire  → cần id=47 (Lõi Hỏa)
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 49 WHERE `element_a`='Earth' AND `element_b`='Fire';

-- Cặp Earth+Metal  (element_a=Earth, element_b=Metal)
--   primary=Metal  → secondary=Earth → id=49
--   primary=Earth  → secondary=Metal → id=50
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 49 WHERE `element_a`='Earth' AND `element_b`='Metal';

-- Cặp Earth+Water  (element_a=Earth, element_b=Water)
--   primary=Water  → secondary=Earth → id=49
--   primary=Earth  → secondary=Water → id=48
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 49 WHERE `element_a`='Earth' AND `element_b`='Water';

-- Cặp Earth+Wind   (element_a=Earth, element_b=Wind)
--   primary=Wind   → secondary=Earth → id=49
--   primary=Earth  → secondary=Wind  → id=52
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 49 WHERE `element_a`='Earth' AND `element_b`='Wind';

-- Cặp Earth+Wood   (element_a=Earth, element_b=Wood)
--   primary=Wood   → secondary=Earth → id=49
--   primary=Earth  → secondary=Wood  → id=51
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 49 WHERE `element_a`='Earth' AND `element_b`='Wood';

-- Cặp Fire+Metal   (element_a=Fire, element_b=Metal)
--   primary=Metal  → secondary=Fire  → id=47
--   primary=Fire   → secondary=Metal → id=50
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 47 WHERE `element_a`='Fire' AND `element_b`='Metal';

-- Cặp Fire+Water   (element_a=Fire, element_b=Water)
--   primary=Water  → secondary=Fire  → id=47
--   primary=Fire   → secondary=Water → id=48
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 47 WHERE `element_a`='Fire' AND `element_b`='Water';

-- Cặp Fire+Wind    (element_a=Fire, element_b=Wind)
--   primary=Wind   → secondary=Fire  → id=47
--   primary=Fire   → secondary=Wind  → id=52
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 47 WHERE `element_a`='Fire' AND `element_b`='Wind';

-- Cặp Fire+Wood    (element_a=Fire, element_b=Wood)
--   primary=Wood   → secondary=Fire  → id=47
--   primary=Fire   → secondary=Wood  → id=51
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 47 WHERE `element_a`='Fire' AND `element_b`='Wood';

-- Cặp Metal+Water  (element_a=Metal, element_b=Water)
--   primary=Water  → secondary=Metal → id=50
--   primary=Metal  → secondary=Water → id=48
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 50 WHERE `element_a`='Metal' AND `element_b`='Water';

-- Cặp Metal+Wind   (element_a=Metal, element_b=Wind)
--   primary=Wind   → secondary=Metal → id=50
--   primary=Metal  → secondary=Wind  → id=52
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 50 WHERE `element_a`='Metal' AND `element_b`='Wind';

-- Cặp Metal+Wood   (element_a=Metal, element_b=Wood)
--   primary=Wood   → secondary=Metal → id=50
--   primary=Metal  → secondary=Wood  → id=51
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 50 WHERE `element_a`='Metal' AND `element_b`='Wood';

-- Cặp Water+Wind   (element_a=Water, element_b=Wind)
--   primary=Wind   → secondary=Water → id=48
--   primary=Water  → secondary=Wind  → id=52
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 48 WHERE `element_a`='Water' AND `element_b`='Wind';

-- Cặp Water+Wood   (element_a=Water, element_b=Wood)
--   primary=Wood   → secondary=Water → id=48
--   primary=Water  → secondary=Wood  → id=51
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 48 WHERE `element_a`='Water' AND `element_b`='Wood';

-- Cặp Wind+Wood    (element_a=Wind, element_b=Wood)
--   primary=Wood   → secondary=Wind  → id=52
--   primary=Wind   → secondary=Wood  → id=51
UPDATE `gene_hybrid_config` SET `fusion_item_id` = 52 WHERE `element_a`='Wind' AND `element_b`='Wood';

-- ── 3. DROP từ Boss (thêm lõi theo hệ Boss) ────────────────
-- Boss Hỏa Long (id=8) drop thêm Lõi Hỏa
INSERT IGNORE INTO `map_enemy_drop` (`map_id`,`enemy_id`,`item_id`,`drop_chance`,`qty_min`,`qty_max`)
VALUES
(15,  8, 47, 0.05, 1, 1),   -- Hỏa Long → Lõi Đột Biến Hỏa
(17, 11, 48, 0.05, 1, 1),   -- Đế Băng  → Lõi Đột Biến Thủy
(19, 14, 51, 0.05, 1, 1),   -- Rừng Chúa→ Lõi Đột Biến Mộc
(22, 17, 50, 0.05, 1, 1);   -- Chúa Tể  → Lõi Đột Biến Kim

-- ── 4. TEST DATA: acc Phong hệ phụ Kim (chạy 1 lần để test) ───────────────
-- primary=Wind, secondary=Metal → cần Lõi Đột Biến Kim (id=50)
-- Yêu cầu Hybrid Fusion: gene_tier=5 VÀ secondary_gene_tier=5

-- 4a. Gán secondary_element='Metal', đưa cả 2 tier lên 5 cho tất cả acc Phong
UPDATE `player_data`
SET `info_char` = JSON_SET(
    `info_char`,
    '$.secondary_element',   'Metal',
    '$.secondary_gene_tier', 5,
    '$.secondary_gene_exp',  0,
    '$.gene_tier',           5
),
`updated_at` = NOW()
WHERE JSON_UNQUOTE(JSON_EXTRACT(`info_char`, '$.element_type')) = 'Wind';

-- 4b. Thêm Lõi Đột Biến Kim (id=50) x10 vào inventory acc Phong
UPDATE `player_data`
SET `inventory` = JSON_ARRAY_APPEND(
    COALESCE(NULLIF(`inventory`, ''), '[]'),
    '$',
    JSON_OBJECT(
        'slotIndex',       100,
        'itemTemplateId',  50,
        'quantity',        10,
        'itemCode',        'Lõi Đột Biến Kim',
        'iconId',          '0'
    )
),
`updated_at` = NOW()
WHERE JSON_UNQUOTE(JSON_EXTRACT(`info_char`, '$.element_type')) = 'Wind';
