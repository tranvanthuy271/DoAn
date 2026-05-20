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

-- 4. Dữ liệu mẫu — 10 quest cho 2 NPC (npc_id 2 và 14)
INSERT INTO `quest_config`
  (`id`, `name`, `level_need`, `npc_id`, `str1`, `str2`, `str3`,
   `exp_reward`, `gold_reward`, `silver_reward`, `item_reward`, `step`, `sort_order`, `is_active`)
VALUES
-- Quest 1: Kill Slime (map 0, NPC Đại Tướng Lan npc_id=2)
(1,
 'Dọn sạch bãi tập',
 1,
 2,
 'Hoan nghênh, dũng sĩ! Ta là Đại Tướng Lan. Trước khi nhận trọng trách lớn hơn, hãy quét sạch 5 con Slime quanh bãi tập của làng.',
 'Tốt lắm. Ngươi đã chứng minh mình không ngại bắt đầu từ việc nhỏ.',
 'Ghi chú: Slime xuất hiện quanh Làng Khởi Đầu, rất gần chỗ của ta.',
 500, 0, 100,
 '11@2',
 '[{"id":0,"name":"Tiêu diệt Slime","idMob":1,"idNpc":-1,"idItem":-1,"idMap":-1,"x":0,"y":0,"require":5,"STR":""}]',
 1, 1),

-- Quest 2: Collect herbs from Slime area (map 0, NPC Đại Tướng Lan npc_id=2)
(2,
 'Thu gom thảo dược',
 3,
 2,
 'Đám Slime gần làng làm rơi nhiều thảo dược hữu ích. Hãy thu thập cho ta 3 bó Thảo Dược để quân y pha chế thuốc.',
 'Chính xác. Đây là loại dược liệu ta đang cần.',
 'Ghi chú: Thảo Dược rơi khi ngươi chiến đấu quanh khu vực Slime.',
 900, 0, 200,
 '17@2',
 '[{"id":1,"name":"Thu thập Thảo Dược","idMob":-1,"idNpc":-1,"idItem":27,"idMap":-1,"x":0,"y":0,"require":3,"STR":""}]',
 2, 1),

-- Quest 3: Talk to guide NPC and clear Goblin scouts (NPC Đại Tướng Lan npc_id=2)
(3,
 'Liên lạc với Hướng Dẫn Viên',
 5,
 2,
 'Goblin đã bắt đầu lảng vảng ở cánh đồng phía trước. Hãy gặp Hướng Dẫn Viên ở bản đồ 1 để nhận chỉ dẫn, rồi tiêu diệt 8 con Goblin tại đó.',
 'Tốt. Báo cáo của ngươi giúp ta nắm rõ tình hình bên ngoài làng.',
 'Ghi chú: Hướng Dẫn Viên có npc_id=14 ở map 1.',
 1500, 50, 350,
 '14@2',
 '[{"id":5,"name":"Nói chuyện với Hướng Dẫn Viên","idMob":-1,"idNpc":14,"idItem":-1,"idMap":1,"x":0,"y":0,"require":1,"STR":"14@Ta đã nhận được tin từ Đại Tướng Lan. Hãy giúp ta dọn bớt Goblin quanh cánh đồng."},{"id":0,"name":"Tiêu diệt Goblin do thám","idMob":2,"idNpc":-1,"idItem":-1,"idMap":-1,"x":0,"y":0,"require":8,"STR":""}]',
 3, 1),

-- Quest 4: Kill Goblin wave (map 1, NPC Hướng Dẫn Viên npc_id=14)
(4,
 'Quét sạch cánh đồng',
 6,
 14,
 'Cánh đồng vẫn chưa an toàn. Hãy tiêu diệt thêm 12 con Goblin để dân làng có thể đi qua khu vực này.',
 'Rất tốt. Cánh đồng đã bớt nguy hiểm hơn nhiều.',
 'Ghi chú: Goblin tập trung đông quanh map 1.',
 2200, 70, 450,
 '17@3',
 '[{"id":0,"name":"Tiêu diệt Goblin ở cánh đồng","idMob":2,"idNpc":-1,"idItem":-1,"idMap":-1,"x":0,"y":0,"require":12,"STR":""}]',
 1, 1),

-- Quest 5: Collect fire crystals (map 1, NPC Hướng Dẫn Viên npc_id=14)
(5,
 'Thu thập tinh thể lửa',
 8,
 14,
 'Trong lúc tuần tra, quân trinh sát phát hiện nhiều Tinh Thể Lửa rải rác ở cánh đồng. Hãy thu thập cho ta 4 khối để nghiên cứu.',
 'Làm tốt lắm. Chúng sẽ rất hữu ích cho việc chế tạo.',
 'Ghi chú: Tinh Thể Lửa có item_id=30.',
 2800, 100, 600,
 '12@2',
 '[{"id":1,"name":"Thu thập Tinh Thể Lửa","idMob":-1,"idNpc":-1,"idItem":30,"idMap":-1,"x":0,"y":0,"require":4,"STR":""}]',
 2, 1),

-- Quest 6: Reach eastern gate and kill Orc scouts (map 100, NPC Hướng Dẫn Viên npc_id=14)
(6,
 'Mở đường tới Cửa Phía Đông',
 10,
 14,
 'Tuyến đường ra Cửa Phía Đông đang bị chặn. Hãy tiến đến map 100 rồi hạ 6 Orc Warrior canh giữ nơi đó.',
 'Tuyệt vời. Tuyến đường tiếp tế đã được khai thông.',
 'Ghi chú: Bước đầu là đến map 100, sau đó tiêu diệt Orc Warrior.',
 3600, 120, 800,
 '18@2',
 '[{"id":9,"name":"Tiến đến Cửa Phía Đông","idMob":-1,"idNpc":-1,"idItem":-1,"idMap":100,"x":0,"y":0,"require":1,"STR":""},{"id":0,"name":"Tiêu diệt Orc Warrior","idMob":3,"idNpc":-1,"idItem":-1,"idMap":-1,"x":0,"y":0,"require":6,"STR":""}]',
 3, 1),

-- Quest 7: Collect iron ore (map 100, NPC Hướng Dẫn Viên npc_id=14)
(7,
 'Kiếm quặng cho thợ rèn',
 12,
 14,
 'Đám Orc mang theo rất nhiều quặng. Hãy thu thập 5 khối Quặng Sắt để thợ rèn gia cố trang bị cho quân lính.',
 'Tốt. Số quặng này đủ để chuẩn bị cho đợt rèn tiếp theo.',
 'Ghi chú: Quặng Sắt có item_id=26 và rơi từ Orc Warrior.',
 4600, 150, 1000,
 '18@3',
 '[{"id":1,"name":"Thu thập Quặng Sắt","idMob":-1,"idNpc":-1,"idItem":26,"idMap":-1,"x":0,"y":0,"require":5,"STR":""}]',
 4, 1),

-- Quest 8: Collect upgrade stones from Orc line (map 100, NPC Hướng Dẫn Viên npc_id=14)
(8,
 'Thu gom đá rèn cấp cao',
 14,
 14,
 'Một số Orc mang theo Đá Nâng Cấp Cấp 2. Hãy đem về cho ta 4 viên để dùng cho tuyến sau.',
 'Rất tốt. Vật tư nâng cấp đã về tới doanh trại.',
 'Ghi chú: Đá Nâng Cấp Cấp 2 có item_id=2.',
 5800, 180, 1300,
 '19@1',
 '[{"id":1,"name":"Thu thập Đá Nâng Cấp Cấp 2","idMob":-1,"idNpc":-1,"idItem":2,"idMap":-1,"x":0,"y":0,"require":4,"STR":""}]',
 5, 1),

-- Quest 9: Orc suppression (map 100, NPC Hướng Dẫn Viên npc_id=14)
(9,
 'Trấn áp chiến binh Orc',
 16,
 14,
 'Số lượng Orc Warrior ngoài tiền tuyến đang tăng mạnh. Ta cần ngươi hạ 15 tên để giữ thế chủ động.',
 'Tình hình đã ổn định hơn. Ngươi làm rất tốt.',
 'Ghi chú: Orc Warrior có enemy_id=3 tại khu vực map 100.',
 7200, 220, 1600,
 '20@1',
 '[{"id":0,"name":"Tiêu diệt Orc Warrior tiền tuyến","idMob":3,"idNpc":-1,"idItem":-1,"idMap":-1,"x":0,"y":0,"require":15,"STR":""}]',
 6, 1),

-- Quest 10: Gather mixed materials for gene research (maps 100 and 1, NPC Hướng Dẫn Viên npc_id=14)
(10,
 'Tích trữ nguyên liệu gene',
 18,
 14,
 'Ta đang chuẩn bị một đợt nghiên cứu gene quy mô lớn. Hãy gom 3 khối Quặng Sắt rồi quay lại cánh đồng thu thêm 3 Tinh Thể Lửa.',
 'Hoàn hảo. Đống nguyên liệu này đủ để bắt đầu mẻ nghiên cứu mới.',
 'Ghi chú: Hoàn thành theo thứ tự: Quặng Sắt trước, Tinh Thể Lửa sau.',
 9000, 300, 2000,
 '17@5,18@3',
 '[{"id":1,"name":"Thu thập Quặng Sắt","idMob":-1,"idNpc":-1,"idItem":26,"idMap":-1,"x":0,"y":0,"require":3,"STR":""},{"id":1,"name":"Thu thập Tinh Thể Lửa","idMob":-1,"idNpc":-1,"idItem":30,"idMap":-1,"x":0,"y":0,"require":3,"STR":""}]',
 7, 1);
