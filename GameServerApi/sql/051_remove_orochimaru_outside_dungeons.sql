-- ============================================================================
-- 051_remove_orochimaru_outside_dungeons.sql
-- Mục đích: loại các phần tử có enemy_id=13 (Orochimaru / prefab Enemy 25)
-- khỏi spawn_json của TẤT CẢ map_spawn_config TRỪ map 110/111 (dungeon).
--
-- Yêu cầu: MySQL 8+ (dùng JSON_TABLE + JSON_ARRAYAGG).
-- HÃY BACKUP TRƯỚC: `mysqldump <db> map_spawn_config > backup_msc.sql`
-- ============================================================================

START TRANSACTION;

-- 1) Cập nhật từng row: rebuild spawn_json bỏ phần tử enemy_id=13
UPDATE map_spawn_config AS msc
JOIN (
    SELECT
        msc2.id AS msc_id,
        COALESCE(
            JSON_ARRAYAGG(
                JSON_OBJECT(
                    'enemy_id',     jt.enemy_id,
                    'hp',           jt.hp,
                    'exp',          jt.exp,
                    'cx',           jt.cx,
                    'cy',           jt.cy,
                    'is_boss',      jt.is_boss,
                    'count',        jt.count_,
                    'respawn_time', jt.respawn_time,
                    'level',        jt.level
                )
            ),
            JSON_ARRAY()
        ) AS new_json
    FROM map_spawn_config msc2
    JOIN JSON_TABLE(
        msc2.spawn_json,
        '$[*]' COLUMNS(
            enemy_id     INT  PATH '$.enemy_id',
            hp           INT  PATH '$.hp',
            `exp`        INT  PATH '$.exp',
            cx           DOUBLE PATH '$.cx',
            cy           DOUBLE PATH '$.cy',
            is_boss      BOOL PATH '$.is_boss',
            count_       INT  PATH '$.count',
            respawn_time INT  PATH '$.respawn_time',
            level        INT  PATH '$.level'
        )
    ) jt
    WHERE msc2.map_id NOT IN (110, 111)
      AND jt.enemy_id <> 13
    GROUP BY msc2.id
) cleaned ON cleaned.msc_id = msc.id
SET msc.spawn_json = cleaned.new_json
WHERE msc.map_id NOT IN (110, 111);

-- 2) Với những row mà SAU khi lọc còn lại 0 phần tử (toàn bộ row chỉ chứa enemy_id=13)
--    sub-query trên không trả về dòng nào → cập nhật riêng thành mảng rỗng.
UPDATE map_spawn_config
SET spawn_json = JSON_ARRAY()
WHERE map_id NOT IN (110, 111)
  AND JSON_SEARCH(spawn_json, 'one', 13, NULL, '$[*].enemy_id') IS NOT NULL;

-- 3) Kiểm tra kết quả
SELECT
    map_id,
    JSON_LENGTH(spawn_json) AS entries,
    JSON_SEARCH(spawn_json, 'one', 13, NULL, '$[*].enemy_id') AS still_has_13
FROM map_spawn_config
WHERE map_id NOT IN (110, 111)
ORDER BY map_id;

COMMIT;
