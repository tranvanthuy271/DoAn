-- migration_npc_system.sql
-- Tạo bảng NPC system cho DB hiện tại (không cần drop lại toàn bộ)

CREATE TABLE IF NOT EXISTS `npc_config` (
  `npc_id`       int(11)      NOT NULL AUTO_INCREMENT,
  `npc_name`     varchar(100) NOT NULL,
  `npc_type`     varchar(20)  NOT NULL DEFAULT 'shop' COMMENT 'shop|quest|blacksmith|exchange|event',
  `map_id`       int(11)      NOT NULL DEFAULT 0,
  `pos_x`        float        NOT NULL DEFAULT 0,
  `pos_y`        float        NOT NULL DEFAULT 0,
  `dialogue_key` varchar(50)  DEFAULT NULL,
  `icon_id`      varchar(50)  DEFAULT NULL,
  `is_active`    tinyint(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (`npc_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `npc_shop_item` (
  `id`               int(11) NOT NULL AUTO_INCREMENT,
  `npc_id`           int(11) NOT NULL,
  `item_template_id` int(11) NOT NULL,
  `price_silver`     int(11) NOT NULL DEFAULT 0,
  `price_gold`       int(11) NOT NULL DEFAULT 0,
  `stock`            int(11) NOT NULL DEFAULT -1 COMMENT '-1 = vô hạn',
  `required_level`   int(11) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  KEY `idx_npc_shop_npc` (`npc_id`),
  CONSTRAINT `fk_npc_shop_npc` FOREIGN KEY (`npc_id`) REFERENCES `npc_config` (`npc_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

CREATE TABLE IF NOT EXISTS `npc_dialogue` (
  `id`           int(11)       NOT NULL AUTO_INCREMENT,
  `npc_id`       int(11)       NOT NULL,
  `dialogue_key` varchar(50)   NOT NULL,
  `text_vi`      varchar(1000) NOT NULL,
  `next_key`     varchar(50)   DEFAULT NULL,
  `action_type`  varchar(20)   NOT NULL DEFAULT 'none' COMMENT 'none|open_shop|give_quest|teleport',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_npc_dialogue_key` (`npc_id`, `dialogue_key`),
  CONSTRAINT `fk_npc_dialogue_npc` FOREIGN KEY (`npc_id`) REFERENCES `npc_config` (`npc_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

-- Dữ liệu mẫu
INSERT IGNORE INTO `npc_config` (`npc_id`, `npc_name`, `npc_type`, `map_id`, `pos_x`, `pos_y`, `dialogue_key`, `icon_id`, `is_active`) VALUES
(1, 'Lão Trương — Thương Nhân', 'shop',       0,  2.0, -1.0, 'greet',       'npc_merchant_1', 1),
(2, 'Đại Tướng Lan',            'quest',      0, -1.0, -1.0, 'quest_intro', 'npc_quest_1',    1),
(3, 'Thợ Rèn Hắc Long',         'blacksmith', 0,  0.0,  1.0, 'greet',       'npc_smith_1',    1);

INSERT IGNORE INTO `npc_dialogue` (`id`, `npc_id`, `dialogue_key`, `text_vi`, `next_key`, `action_type`) VALUES
(1, 1, 'greet',        'Chào anh hùng! Ta có nhiều đồ tốt muốn bán. Anh hãy xem thử nhé!', 'shop_offer',  'none'),
(2, 1, 'shop_offer',   'Đây là những thứ ta đang bán. Chúc anh mua vui!',                   NULL,          'open_shop'),
(3, 2, 'quest_intro',  'Vùng đất phía đông đang bị quái thú hoành hành. Ta cần người hùng!','quest_accept','none'),
(4, 2, 'quest_accept', 'Hãy tiêu diệt 10 con Goblin Đen và quay lại gặp ta.',               NULL,          'give_quest'),
(5, 3, 'greet',        'Mang trang bị đến đây đi, ta sẽ rèn cho mạnh hơn!',                 NULL,          'open_shop');

INSERT IGNORE INTO `npc_shop_item` (`id`, `npc_id`, `item_template_id`, `price_silver`, `price_gold`, `stock`, `required_level`) VALUES
(1, 1, 17, 500,   0, -1, 1),
(2, 1, 18, 1500,  0, -1, 5),
(3, 1, 19, 5000,  0, -1, 10),
(4, 1, 20, 15000, 0, -1, 15),
(5, 3, 17, 800,   0, -1, 1),
(6, 3, 18, 2000,  0, -1, 5);
