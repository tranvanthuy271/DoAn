-- ============================================================
--  UPSERT: Item effect cho Tinh Chat (templateId = 21)
--  Bo sung config GeneExpAdd cho DB dang chay, khong phu thuoc id co san.
--
--  Ap dung:
--      mysql -u root -p gamedb < SQL/upsert_item_effect_template_21_gene_exp.sql
-- ============================================================

INSERT INTO `item_effect_template`
  (`item_template_id`, `effect_type`, `value`, `duration_sec`, `icon_id`, `display_name`, `detail`, `sort_order`)
SELECT
  21,
  'GeneExpAdd',
  500,
  0,
  289,
  'Gene EXP +500',
  '+500 Gene EXP',
  3
FROM DUAL
WHERE NOT EXISTS (
  SELECT 1
  FROM `item_effect_template`
  WHERE `item_template_id` = 21
    AND `effect_type` = 'GeneExpAdd'
);

UPDATE `item_effect_template`
SET
  `value` = 500,
  `duration_sec` = 0,
  `icon_id` = 289,
  `display_name` = 'Gene EXP +500',
  `detail` = '+500 Gene EXP',
  `sort_order` = 3
WHERE `item_template_id` = 21
  AND `effect_type` = 'GeneExpAdd';