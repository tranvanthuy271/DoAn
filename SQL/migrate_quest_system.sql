-- ============================================================
--  MIGRATION: Quest System Simplification
--  - Xoá bảng player_quest (không còn dùng)
--  - Xoá và tạo lại bảng quest_config theo kiểu LangLa
--    (1 bảng config; tiến trình player lưu trong player_data.info_char)
--
--  Áp dụng lên DB đang chạy:
--      mysql -u root -p gamedb < SQL/migrate_quest_system.sql
-- ============================================================

-- 1. Xoá bảng player_quest
DROP TABLE IF EXISTS `player_quest`;

-- 2. Xoá bảng quest_config cũ (nếu có)
DROP TABLE IF EXISTS `quest_config`;

-- 3. Tạo bảng quest_config mới (inspired by LangLa task table)
CREATE TABLE `quest_config` (
  `id`            int(11)      NOT NULL AUTO_INCREMENT,
  `name`          varchar(200) NOT NULL DEFAULT '',
  `level_need`    int(11)      NOT NULL DEFAULT 1,
  `npc_id`        int(11)      NOT NULL DEFAULT 0    COMMENT 'NPC nhận và giao nhiệm vụ (cùng 1 NPC)',
  `str1`          text         NOT NULL              COMMENT 'Hội thoại khi nhận nhiệm vụ',
  `str2`          text         NOT NULL              COMMENT 'Hội thoại khi nộp/hoàn thành nhiệm vụ',
  `str3`          text         NOT NULL              COMMENT 'Ghi chú / hướng dẫn cho người chơi',
  `exp_reward`    int(11)      NOT NULL DEFAULT 0,
  `gold_reward`   int(11)      NOT NULL DEFAULT 0,
  `silver_reward` int(11)      NOT NULL DEFAULT 0,
  `item_reward`   varchar(500) NOT NULL DEFAULT ''   COMMENT 'Format: itemId@quantity,itemId@quantity',
  `step`          longtext     NOT NULL              COMMENT 'JSON steps: [{id,name,idMob,idNpc,idItem,idMap,x,y,require,STR}]',
  `sort_order`    int(11)      NOT NULL DEFAULT 0,
  `is_active`     tinyint(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- ============================================================
--  Step JSON format:
--  [
--    {
--      "id": <type>,        -- 0=kill mob, 1=collect item, 5=talk to NPC, 9=reach map
--      "name": "Mô tả",
--      "idMob":  -1,        -- id quái (-1 = không cần)
--      "idNpc":  -1,        -- id NPC  (-1 = không cần)
--      "idItem": -1,        -- id item (-1 = không cần)
--      "idMap":  -1,        -- id map  (-1 = bất kỳ map)
--      "x": 0, "y": 0,      -- toạ độ (0,0 = bất kỳ)
--      "require": 5,        -- số lần / số lượng cần
--      "STR": ""            -- hội thoại phụ (dành cho step loại 5)
--    }
--  ]
--
--  Player quest progress (lưu trong player_data.info_char JSON):
--  {
--    "active_quest_id": 1,         -- null = không có quest đang làm
--    "quest_step": 0,              -- bước hiện tại (index)
--    "quest_progress": {"0": 3},   -- key=stepIndex, value=số đã thực hiện
--    "completed_quests": [1, 2]    -- danh sách id quest đã hoàn thành
--  }
-- ============================================================

-- 4. Dữ liệu mẫu — 3 quest cho 2 NPC (npc_id 2 và 14)
INSERT INTO `quest_config`
  (`id`, `name`, `level_need`, `npc_id`, `str1`, `str2`, `str3`,
   `exp_reward`, `gold_reward`, `silver_reward`, `item_reward`, `step`, `sort_order`, `is_active`)
VALUES
-- Quest 1: Kill quest (map 0, NPC Đại Tướng Lan npc_id=2)
(1,
 'Diệt quái vật đầu tiên',
 1,
 2,
 'Hoan nghênh, dũng sĩ! Ta là Đại Tướng Lan. Khu vực này đang bị Goblin quấy phá.\nNhiệm vụ đầu tiên của ngươi là tiêu diệt 5 con Goblin gần đây.\nNgươi có sẵn sàng nhận nhiệm vụ không?',
 'Xuất sắc! Ngươi đã hoàn thành nhiệm vụ. Đây là phần thưởng xứng đáng.',
 'Ghi chú: Goblin xuất hiện ở khu vực phía đông bản đồ.',
 500, 0, 100,
 '',
 '[{"id":0,"name":"Tiêu diệt Goblin","idMob":2,"idNpc":-1,"idItem":-1,"idMap":-1,"x":0,"y":0,"require":5,"STR":""}]',
 1, 1),

-- Quest 2: Talk + kill (map 0, NPC Đại Tướng Lan npc_id=2)
(2,
 'Điều tra nguồn gốc',
 5,
 2,
 'Dũng sĩ, ta cần ngươi làm rõ tại sao Goblin ngày càng xuất hiện nhiều hơn.\nHãy nói chuyện với Hướng Dẫn Viên ở bản đồ tiếp theo,\nsau đó tiêu diệt 10 con Goblin để thu thập manh mối.',
 'Tốt lắm! Đúng như ta lo ngại — cần có hành động ngay. Nhận thưởng đi.',
 'Ghi chú: Hướng Dẫn Viên ở npc_id=14, bản đồ 1.',
 1200, 50, 300,
 '',
 '[{"id":5,"name":"Nói chuyện với Hướng Dẫn Viên","idMob":-1,"idNpc":14,"idItem":-1,"idMap":1,"x":0,"y":0,"require":1,"STR":"14@Chào dũng sĩ! Ta đã biết về tình trạng Goblin tăng đột biến. Hãy đi tiêu diệt chúng và thu thập manh mối."},{"id":0,"name":"Tiêu diệt Goblin để điều tra","idMob":2,"idNpc":-1,"idItem":-1,"idMap":-1,"x":0,"y":0,"require":10,"STR":""}]',
 2, 1),

-- Quest 3: Collect quest (map 1, NPC Hướng Dẫn Viên npc_id=14)
(3,
 'Thu thập nguyên liệu',
 10,
 14,
 'Chào mừng đến với bản đồ mới, dũng sĩ! Ta là Hướng Dẫn Viên.\nTa cần ngươi thu thập 3 mảnh Goblin từ bọn chúng làm nguyên liệu chế tạo.\nNgươi có nhận nhiệm vụ này không?',
 'Cảm ơn! Đây chính là nguyên liệu ta cần. Nhận phần thưởng của ngươi.',
 '',
 2000, 100, 500,
 '',
 '[{"id":1,"name":"Thu thập mảnh Goblin","idMob":2,"idNpc":-1,"idItem":1,"idMap":-1,"x":0,"y":0,"require":3,"STR":""}]',
 1, 1);
