-- ============================================================
-- Migration 045 — NPC Shop JSON per NPC (LangLa-style)
-- Thêm cột shop_items_json vào npc_config.
-- Mỗi NPC shop có 1 JSON object chứa shop_name và danh sách item.
-- idClass mapping: 0=Tất Cả 1=Hỏa 2=Thủy 3=Thổ 4=Lôi(Kim) 5=Mộc 6=Phong
-- ============================================================

ALTER TABLE `npc_config`
    ADD COLUMN `shop_items_json` TEXT DEFAULT NULL
    COMMENT 'JSON: {"shop_name":"...","items":[{"item_template_id":1,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1}]}';

-- ── NPC 1: Dược Phẩm ─────────────────────────────────────────────────────────
UPDATE `npc_config` SET `shop_items_json` = '{"shop_name":"Dược Phẩm","items":[
  {"item_template_id":11,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":12,"price_silver":1500,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":13,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":14,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":15,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":16,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":121,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":122,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":161,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1}
]}' WHERE `npc_id` = 1;

-- ── NPC 5: Binh Khí (có tab element Hỏa/Thủy/Thổ/Lôi/Mộc/Phong) ─────────────
UPDATE `npc_config` SET `shop_items_json` = '{"shop_name":"Binh Khí","items":[
  {"item_template_id":200,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":201,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":202,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":203,"price_silver":50000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":204,"price_silver":150000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":205,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":206,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":207,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":208,"price_silver":50000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":209,"price_silver":150000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":210,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":211,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":212,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":213,"price_silver":50000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":214,"price_silver":150000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":215,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":216,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":217,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":218,"price_silver":50000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":219,"price_silver":150000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":220,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":221,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":222,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":223,"price_silver":50000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":224,"price_silver":150000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":225,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":226,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":227,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":228,"price_silver":50000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":229,"price_silver":150000,"price_gold":0,"stock":-1,"level_need":50}
]}' WHERE `npc_id` = 5;

-- ── NPC 7: Trang Bị (idClass=0, không có tab element) ────────────────────────
UPDATE `npc_config` SET `shop_items_json` = '{"shop_name":"Trang Bị","items":[
  {"item_template_id":100,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":101,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":102,"price_silver":10000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":103,"price_silver":35000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":104,"price_silver":100000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":105,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":106,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":107,"price_silver":10000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":108,"price_silver":35000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":109,"price_silver":100000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":110,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":111,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":112,"price_silver":10000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":113,"price_silver":35000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":114,"price_silver":100000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":115,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":116,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":117,"price_silver":10000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":118,"price_silver":35000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":119,"price_silver":100000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":130,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":131,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":132,"price_silver":10000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":133,"price_silver":35000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":134,"price_silver":100000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":135,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":136,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":137,"price_silver":10000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":138,"price_silver":35000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":139,"price_silver":100000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":140,"price_silver":1000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":141,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":142,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":143,"price_silver":50000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":144,"price_silver":150000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":150,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":151,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":152,"price_silver":10000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":153,"price_silver":35000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":154,"price_silver":100000,"price_gold":0,"stock":-1,"level_need":50},
  {"item_template_id":155,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":156,"price_silver":3000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":157,"price_silver":10000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":158,"price_silver":35000,"price_gold":0,"stock":-1,"level_need":35},
  {"item_template_id":159,"price_silver":100000,"price_gold":0,"stock":-1,"level_need":50}
]}' WHERE `npc_id` = 7;

-- ── NPC 8: Tiên Dược ─────────────────────────────────────────────────────────
UPDATE `npc_config` SET `shop_items_json` = '{"shop_name":"Tiên Dược","items":[
  {"item_template_id":121,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":122,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":123,"price_silver":50000,"price_gold":0,"stock":-1,"level_need":40},
  {"item_template_id":161,"price_silver":8000,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":162,"price_silver":25000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":163,"price_silver":80000,"price_gold":0,"stock":-1,"level_need":40},
  {"item_template_id":171,"price_silver":8000,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":172,"price_silver":25000,"price_gold":0,"stock":-1,"level_need":20},
  {"item_template_id":173,"price_silver":80000,"price_gold":0,"stock":-1,"level_need":40}
]}' WHERE `npc_id` = 8;

-- ── NPC 12: Tạp Hóa (Cánh Đồng map) ─────────────────────────────────────────
UPDATE `npc_config` SET `shop_items_json` = '{"shop_name":"Tạp Hóa","items":[
  {"item_template_id":11,"price_silver":500,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":12,"price_silver":1500,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":13,"price_silver":5000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":14,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":15,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":5},
  {"item_template_id":16,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":10},
  {"item_template_id":121,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":122,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1},
  {"item_template_id":161,"price_silver":15000,"price_gold":0,"stock":-1,"level_need":1}
]}' WHERE `npc_id` = 12;

-- ── Cập nhật idClass comment để phản ánh đúng mapping ────────────────────────
-- (chỉ comment trong code, không đổi DB enum)
-- 0=Tất Cả  1=Hỏa  2=Thủy  3=Thổ  4=Lôi(Kim)  5=Mộc  6=Phong
