-- ============================================================================
-- 050_inspect_orochimaru_spawns.sql
-- Mục đích: tìm tất cả map_spawn_config có enemy_id=13 (prefab "Enemy 25" /
-- Orochimaru). enemyId=13 là ID đang được EnemyPrefabManager map vào prefab
-- Enemy 25 trong ServerScene.unity, nên mọi spawn enemy_id=13 ở map không có
-- ground proxy đều rơi xuống vô hạn.
-- ============================================================================

-- 1) Liệt kê các row có chứa enemy_id=13 trong spawn_json (in toàn bộ JSON)
SELECT
    msc.id,
    msc.map_id,
    m.name AS map_name,
    msc.spawn_json
FROM map_spawn_config msc
LEFT JOIN map m ON m.id = msc.map_id
WHERE JSON_SEARCH(msc.spawn_json, 'one', 13, NULL, '$[*].enemy_id') IS NOT NULL
ORDER BY msc.map_id;

-- 2) Đếm số row theo map_id và đánh dấu xem map đó có phải dungeon không
SELECT
    msc.map_id,
    m.name AS map_name,
    CASE WHEN msc.map_id IN (110, 111) THEN 'DUNGEON_KEEP' ELSE 'REGULAR_MAP' END AS map_kind,
    COUNT(*) AS row_count
FROM map_spawn_config msc
LEFT JOIN map m ON m.id = msc.map_id
WHERE JSON_SEARCH(msc.spawn_json, 'one', 13, NULL, '$[*].enemy_id') IS NOT NULL
GROUP BY msc.map_id, m.name
ORDER BY msc.map_id;
