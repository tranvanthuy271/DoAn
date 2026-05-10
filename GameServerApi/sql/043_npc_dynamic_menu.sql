-- ============================================================
-- Migration 043: [SUPERSEDED - NOP]
--
-- Ban đầu migration này thêm cột `menu_items` vào bảng npc_config.
-- Nhưng quyết định cuối cùng là KHÔNG lưu menu config trong DB —
-- toàn bộ config được định nghĩa trong C# class NpcMenuConfig.cs.
--
-- File này được giữ lại để duy trì thứ tự migration, nhưng không
-- thực hiện bất kỳ thay đổi schema nào.
--
-- Nếu bạn đã chạy migration 043 cũ (có ADD COLUMN menu_items),
-- hãy chạy script cleanup bên dưới để xoá cột đó:
-- ============================================================

-- Cleanup nếu đã lỡ chạy 043 cũ:
-- ALTER TABLE npc_config DROP COLUMN IF EXISTS menu_items;

-- (NOP — không thay đổi gì)
SELECT 1;

-- Hỗ trợ hệ thống NPC Dynamic Menu (server-driven menu list)
--
-- Định dạng menu_items: "label:action_type;label2:action_type2"
-- action_type hỗ trợ: open_shop | open_blacksmith | open_dungeon | close
--
-- Chạy 1 lần khi deploy phiên bản có NpcDynamicMenuUI.
-- ============================================================

-- 1. Thêm cột menu_items (nullable, để NPCs cũ tương thích)
ALTER TABLE npc_config
    ADD COLUMN IF NOT EXISTS menu_items VARCHAR(1000) DEFAULT NULL;

-- 2. Cập nhật menu mặc định theo npc_type cho các NPC hiện có
--    (chỉ set khi cột đang NULL để không ghi đè config tuỳ chỉnh)

UPDATE npc_config
SET menu_items = 'Mua đồ:open_shop;Cáo từ:close'
WHERE npc_type = 'shop'
  AND (menu_items IS NULL OR menu_items = '');

UPDATE npc_config
SET menu_items = 'Đến lò rèn:open_blacksmith;Cáo từ:close'
WHERE npc_type = 'blacksmith'
  AND (menu_items IS NULL OR menu_items = '');

UPDATE npc_config
SET menu_items = 'Vào phó bản:open_dungeon;Cáo từ:close'
WHERE npc_type = 'dungeon'
  AND (menu_items IS NULL OR menu_items = '');

UPDATE npc_config
SET menu_items = 'Nhiệm vụ:open_quest;Cáo từ:close'
WHERE npc_type = 'quest'
  AND (menu_items IS NULL OR menu_items = '');

UPDATE npc_config
SET menu_items = 'Đổi đồ:open_exchange;Cáo từ:close'
WHERE npc_type = 'exchange'
  AND (menu_items IS NULL OR menu_items = '');

-- 3. Ví dụ config nâng cao cho NPC có nhiều chức năng (tuỳ chỉnh theo npc_id):
-- UPDATE npc_config
-- SET menu_items = 'Tẩy tiềm năng:reset_potential;Tẩy kỹ năng:reset_skill;Luyện bí kíp:learn_skill;Đổi bí kíp:exchange_skill;Khóa cấp nhân vật:lock_level;Cáo từ:close'
-- WHERE npc_id = 5;

-- Verify
SELECT npc_id, npc_name, npc_type, menu_items
FROM npc_config
ORDER BY npc_id;
