-- ============================================================
-- Migration 047 — Utility Shop (Virtual NPC id=999)
-- Tạo NPC ảo id=999 "Cửa Hàng Tiện Ích" không gắn map.
-- Player mở được từ bất kỳ đâu qua HUD (nút Shop trong UtilityDrawer).
-- API tái sử dụng: /api/npc/shop?npcId=999 và /api/npc/shop/buy
-- ============================================================

INSERT INTO `npc_config`
    (`npc_id`, `npc_name`, `npc_type`, `map_id`, `pos_x`, `pos_y`,
     `dialogue_key`, `icon_id`, `is_active`, `shop_items_json`)
VALUES
    (999, 'Cửa Hàng Tiện Ích', 'shop', -1, 0, 0,
     NULL, 'shop_utility', 1,
     '{"shop_name":"Cửa Hàng Tiện Ích","items":[
  {"item_template_id":11,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":12,"price_silver":1500,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":13,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":14,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":15,"price_silver":1500,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":16,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":1,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":2,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":3,"price_silver":8000,"price_gold":0,"stock":-1,"level_need":15},
  {"item_template_id":8,"price_silver":2000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":121,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":122,"price_silver":40000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":17,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":18,"price_silver":20000,"price_gold":0,"stock":-1,"level_need":15},
  {"item_template_id":19,"price_silver":60000,"price_gold":0,"stock":-1,"level_need":30}
]}');

-- ============================================================
-- Config Guide:
-- Để thêm/xóa/sửa item trong shop tiện ích, chạy:
--
-- UPDATE npc_config SET shop_items_json = '{"shop_name":"Cửa Hàng Tiện Ích","items":[
--   {"item_template_id":<id>,"price_silver":<gia>,"price_gold":0,"stock":-1,"level_need":<level>},
--   ...
-- ]}' WHERE npc_id = 999;
--
-- Các trường:
--   item_template_id : ID trong bảng item_template
--   price_silver     : Giá bạc (dùng khi price_gold = 0)
--   price_gold       : Giá vàng (ưu tiên nếu > 0)
--   stock            : -1 = vô hạn, > 0 = giới hạn số lượng
--   level_need       : Level tối thiểu để mua
-- ============================================================
