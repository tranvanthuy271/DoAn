-- ============================================================
-- 055_gene_material_drops_and_dungeon_rewards.sql
-- Add all materials required by Gene Evolution and Hybrid Fusion
-- to live drop/reward sources.
--
-- Required by gene_upgrade_config / gene_multi_config:
--   17 Linh Thach So Cap, 18 Linh Thach Trung Cap,
--   19 Linh Thach Cao Cap, 20 Linh Thach Thuong Cap.
--
-- Required by gene_hybrid_config:
--   47-52 element Fusion Cores.
--
-- Idempotent: re-running only resets the intended drop/reward JSON.
-- ============================================================

SET NAMES utf8mb4;

-- ------------------------------------------------------------
-- World monster drops: evolution stones by enemy progression.
-- These drops are consumed through enemy.drop_items_json by
-- /api/map/{mapId}/spawn-config and the Unity enemy drop flow.
-- ------------------------------------------------------------
UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":27,"drop_chance":0.30,"qty_min":1,"qty_max":2},{"item_id":1,"drop_chance":0.20,"qty_min":1,"qty_max":1},{"item_id":17,"drop_chance":0.12,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 1;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":11,"drop_chance":0.15,"qty_min":1,"qty_max":1},{"item_id":29,"drop_chance":0.40,"qty_min":1,"qty_max":2},{"item_id":17,"drop_chance":0.10,"qty_min":1,"qty_max":1}]'
WHERE `enemy_id` = 2;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":26,"drop_chance":0.40,"qty_min":1,"qty_max":3},{"item_id":2,"drop_chance":0.25,"qty_min":1,"qty_max":2},{"item_id":18,"drop_chance":0.12,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 3;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":30,"drop_chance":0.35,"qty_min":1,"qty_max":2},{"item_id":21,"drop_chance":0.05,"qty_min":1,"qty_max":1},{"item_id":17,"drop_chance":0.10,"qty_min":1,"qty_max":1}]'
WHERE `enemy_id` = 4;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":203,"drop_chance":0.05,"qty_min":1,"qty_max":1},{"item_id":5,"drop_chance":0.60,"qty_min":2,"qty_max":5},{"item_id":28,"drop_chance":0.40,"qty_min":1,"qty_max":2},{"item_id":20,"drop_chance":0.12,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 5;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":18,"drop_chance":0.14,"qty_min":1,"qty_max":2},{"item_id":2,"drop_chance":0.18,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 6;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":18,"drop_chance":0.14,"qty_min":1,"qty_max":2},{"item_id":37,"drop_chance":0.10,"qty_min":1,"qty_max":1}]'
WHERE `enemy_id` = 7;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":20,"drop_chance":0.14,"qty_min":1,"qty_max":2},{"item_id":5,"drop_chance":0.30,"qty_min":1,"qty_max":3}]'
WHERE `enemy_id` = 8;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":20,"drop_chance":0.14,"qty_min":1,"qty_max":2},{"item_id":37,"drop_chance":0.35,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 9;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":20,"drop_chance":0.18,"qty_min":1,"qty_max":3},{"item_id":31,"drop_chance":0.08,"qty_min":1,"qty_max":1}]'
WHERE `enemy_id` = 10;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":37,"drop_chance":0.50,"qty_min":1,"qty_max":2},{"item_id":207,"drop_chance":0.08,"qty_min":1,"qty_max":1},{"item_id":20,"drop_chance":0.12,"qty_min":1,"qty_max":2},{"item_id":31,"drop_chance":0.05,"qty_min":1,"qty_max":1}]'
WHERE `enemy_id` = 11;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":27,"drop_chance":0.45,"qty_min":1,"qty_max":3},{"item_id":25,"drop_chance":0.08,"qty_min":1,"qty_max":1},{"item_id":18,"drop_chance":0.12,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 12;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":27,"drop_chance":0.60,"qty_min":2,"qty_max":4},{"item_id":25,"drop_chance":0.12,"qty_min":1,"qty_max":1},{"item_id":19,"drop_chance":0.12,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 13;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":38,"drop_chance":0.50,"qty_min":1,"qty_max":2},{"item_id":222,"drop_chance":0.08,"qty_min":1,"qty_max":1},{"item_id":20,"drop_chance":0.12,"qty_min":1,"qty_max":2},{"item_id":31,"drop_chance":0.05,"qty_min":1,"qty_max":1}]'
WHERE `enemy_id` = 14;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":26,"drop_chance":0.30,"qty_min":1,"qty_max":2},{"item_id":11,"drop_chance":0.20,"qty_min":1,"qty_max":1},{"item_id":19,"drop_chance":0.10,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 15;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":26,"drop_chance":0.50,"qty_min":1,"qty_max":3},{"item_id":15,"drop_chance":0.15,"qty_min":1,"qty_max":1},{"item_id":19,"drop_chance":0.12,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 16;

UPDATE `enemy`
SET `drop_items_json` = '[{"item_id":39,"drop_chance":0.50,"qty_min":1,"qty_max":2},{"item_id":40,"drop_chance":0.30,"qty_min":1,"qty_max":1},{"item_id":219,"drop_chance":0.06,"qty_min":1,"qty_max":1},{"item_id":20,"drop_chance":0.16,"qty_min":1,"qty_max":3},{"item_id":31,"drop_chance":0.10,"qty_min":1,"qty_max":2}]'
WHERE `enemy_id` = 17;

-- ------------------------------------------------------------
-- Wave dungeon rewards: put the rare/important fusion materials
-- into dungeon progression.
-- ------------------------------------------------------------
INSERT INTO `dungeon_wave_config`
  (`dungeon_id`, `max_waves`, `wave_time_seconds`, `enemy_scale_percent`, `boss_scale_percent`, `exp_gold_scale_percent`, `daily_entry_limit`, `entry_item_plus1_id`, `entry_item_plus2_id`, `milestone_reward_json`)
SELECT 6, 20, 300, 10.0, 15.0, 10.0, 1, 409, 410, '[]'
WHERE NOT EXISTS (SELECT 1 FROM `dungeon_wave_config` WHERE `dungeon_id` = 6);

UPDATE `dungeon_wave_config`
SET `milestone_reward_json` = '[
  {"wave":5, "exp":5000, "gold":500, "items":[
    {"item_template_id":17,"quantity":5},
    {"item_template_id":18,"quantity":2}
  ]},
  {"wave":10, "exp":15000, "gold":1500, "items":[
    {"item_template_id":18,"quantity":5},
    {"item_template_id":19,"quantity":2}
  ]},
  {"wave":15, "exp":30000, "gold":3000, "items":[
    {"item_template_id":19,"quantity":5},
    {"item_template_id":20,"quantity":2},
    {"item_template_id":47,"quantity":1},
    {"item_template_id":48,"quantity":1},
    {"item_template_id":49,"quantity":1}
  ]},
  {"wave":20, "exp":50000, "gold":5000, "items":[
    {"item_template_id":20,"quantity":5},
    {"item_template_id":31,"quantity":1},
    {"item_template_id":50,"quantity":1},
    {"item_template_id":51,"quantity":1},
    {"item_template_id":52,"quantity":1}
  ]}
]'
WHERE `dungeon_id` = 6;

-- Make the dungeon list/detail display the important rewards too.
UPDATE `dungeon_config`
SET `reward_json` = '{"items":[{"item_template_id":17,"quantity":5},{"item_template_id":18,"quantity":5},{"item_template_id":19,"quantity":5},{"item_template_id":20,"quantity":5},{"item_template_id":31,"quantity":1},{"item_template_id":47,"quantity":1},{"item_template_id":48,"quantity":1},{"item_template_id":49,"quantity":1},{"item_template_id":50,"quantity":1},{"item_template_id":51,"quantity":1},{"item_template_id":52,"quantity":1}]}'
WHERE `dungeon_id` IN (6, 7);
