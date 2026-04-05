START TRANSACTION;

INSERT INTO npc_config
    (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id, is_active)
SELECT
    'Thuong Nhan Canh Dong', 'shop', 1, 3.0, -1.0, 'greet', 'npc_merchant_1', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM npc_config
    WHERE map_id = 1
      AND npc_type = 'shop'
      AND ABS(pos_x - 3.0) < 0.01
      AND ABS(pos_y - -1.0) < 0.01
);

INSERT INTO npc_config
    (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id, is_active)
SELECT
    'Tho Ren Canh Dong', 'blacksmith', 1, 9.0, 0.5, 'greet', 'npc_smith_1', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM npc_config
    WHERE map_id = 1
      AND npc_type = 'blacksmith'
      AND ABS(pos_x - 9.0) < 0.01
      AND ABS(pos_y - 0.5) < 0.01
);

INSERT INTO npc_config
    (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id, is_active)
SELECT
    'Huong Dan Vien', 'quest', 1, 15.0, 0.5, 'quest_intro', 'npc_quest_1', 1
WHERE NOT EXISTS (
    SELECT 1
    FROM npc_config
    WHERE map_id = 1c:\Users\fl2k3\AppData\Local\Packages\MicrosoftWindows.Client.Core_cw5n1h2txyewy\TempState\ScreenClip\{7CA9BA25-F895-4ED1-BE42-94A96934CE26}.png
      AND npc_type = 'quest'
      AND ABS(pos_x - 15.0) < 0.01
      AND ABS(pos_y - 0.5) < 0.01
);

INSERT INTO npc_shop_item
    (npc_id, item_template_id, price_silver, price_gold, stock, required_level)
SELECT
    target.npc_id,
    source.item_template_id,
    source.price_silver,
    source.price_gold,
    source.stock,
    source.required_level
FROM npc_shop_item source
JOIN npc_config target
    ON target.map_id = 1
   AND target.npc_name = 'Thuong Nhan Canh Dong'
WHERE source.npc_id = 1
  AND NOT EXISTS (
      SELECT 1
      FROM npc_shop_item existing
      WHERE existing.npc_id = target.npc_id
        AND existing.item_template_id = source.item_template_id
  );

INSERT INTO npc_shop_item
    (npc_id, item_template_id, price_silver, price_gold, stock, required_level)
SELECT
    target.npc_id,
    source.item_template_id,
    source.price_silver,
    source.price_gold,
    source.stock,
    source.required_level
FROM npc_shop_item source
JOIN npc_config target
    ON target.map_id = 1
   AND target.npc_name = 'Tho Ren Canh Dong'
WHERE source.npc_id = 3
  AND NOT EXISTS (
      SELECT 1
      FROM npc_shop_item existing
      WHERE existing.npc_id = target.npc_id
        AND existing.item_template_id = source.item_template_id
  );

INSERT INTO enemy_spawns
    (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
SELECT 0, 1, 41.0, 1.2, 1, 5
WHERE NOT EXISTS (
    SELECT 1
    FROM enemy_spawns
    WHERE map_id = 0
      AND enemy_type_id = 1
      AND ABS(spawn_x - 41.0) < 0.01
      AND ABS(spawn_y - 1.2) < 0.01
);

INSERT INTO enemy_spawns
    (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
SELECT 0, 1, 46.0, 3.83, 1, 5
WHERE NOT EXISTS (
    SELECT 1
    FROM enemy_spawns
    WHERE map_id = 0
      AND enemy_type_id = 1
      AND ABS(spawn_x - 46.0) < 0.01
      AND ABS(spawn_y - 3.83) < 0.01
);

INSERT INTO enemy_spawns
    (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
SELECT 0, 1, 40.4, 5.0, 1, 5
WHERE NOT EXISTS (
    SELECT 1
    FROM enemy_spawns
    WHERE map_id = 0
      AND enemy_type_id = 1
      AND ABS(spawn_x - 40.4) < 0.01
      AND ABS(spawn_y - 5.0) < 0.01
);

INSERT INTO enemy_spawns
    (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
SELECT 0, 1, 38.6, 7.5, 1, 5
WHERE NOT EXISTS (
    SELECT 1
    FROM enemy_spawns
    WHERE map_id = 0
      AND enemy_type_id = 1
      AND ABS(spawn_x - 38.6) < 0.01
      AND ABS(spawn_y - 7.5) < 0.01
);

INSERT INTO enemy_spawns
    (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
SELECT 0, 1, 50.0, 8.0, 1, 5
WHERE NOT EXISTS (
    SELECT 1
    FROM enemy_spawns
    WHERE map_id = 0
      AND enemy_type_id = 1
      AND ABS(spawn_x - 50.0) < 0.01
      AND ABS(spawn_y - 8.0) < 0.01
);

INSERT INTO enemy_spawns
    (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
SELECT 1, 4, 5.5, -2.0, 3, 15
WHERE NOT EXISTS (
    SELECT 1
    FROM enemy_spawns
    WHERE map_id = 1
      AND enemy_type_id = 4
      AND ABS(spawn_x - 5.5) < 0.01
      AND ABS(spawn_y - -2.0) < 0.01
);

INSERT INTO enemy_spawns
    (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
SELECT 1, 4, 12.0, 1.5, 4, 15
WHERE NOT EXISTS (
    SELECT 1
    FROM enemy_spawns
    WHERE map_id = 1
      AND enemy_type_id = 4
      AND ABS(spawn_x - 12.0) < 0.01
      AND ABS(spawn_y - 1.5) < 0.01
);

INSERT INTO enemy_spawns
    (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
SELECT 1, 8, 25.0, 5.0, 1, 300
WHERE NOT EXISTS (
    SELECT 1
    FROM enemy_spawns
    WHERE map_id = 1
      AND enemy_type_id = 8
      AND ABS(spawn_x - 25.0) < 0.01
      AND ABS(spawn_y - 5.0) < 0.01
);

COMMIT;