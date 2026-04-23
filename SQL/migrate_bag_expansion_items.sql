-- ============================================================
-- MIGRATION: Thêm item túi mở rộng (type = 32)
-- 
-- Lý do đổi từ type 30 → 32:
--   type 30 đã được dùng cho Vật Liệu (Quặng Sắt, Thảo Dược...).
--   Nếu giữ type 30 cho túi mở rộng thì vật liệu cũng sẽ
--   vô tình kích hoạt logic mở rộng túi khi player dùng chúng.
--   type 32 = BagExpansion, riêng biệt và không xung đột.
--
-- Server code đã đổi: PlayerController.cs  BagItemType = 32
-- Client code đã đổi: ItemUseHandler.cs    ItemTypeBag  = 32
--
-- Chạy migration này MỘT LẦN trên DB production.
-- ============================================================

-- Cập nhật comment column type để ghi nhận type 32
ALTER TABLE `item_template`
  MODIFY COLUMN `type` tinyint(4) NOT NULL
  COMMENT '0=Helmet 1=Weapon 2=Armor 3=Pants 4=Boots 5=Ring 21=UpgStone 22=HPPotion 23=MPPotion 24=Food 25=GeneStone 30=Material 31=WaveTicket 32=BagExpansion';

-- ============================================================
-- Item túi mở rộng – 4 cấp, mỗi cấp +5 ô túi
-- idIcon: dùng tạm icon 0 (chưa có sprite riêng).
--         Thay số thực khi có icon trong atlas (xem HUONG_DAN_CONFIG_BAG_QUICK_SLOTS.md).
-- isXepChong = False vì mỗi item có upgradeLevel riêng (không stack lẫn cấp)
-- ============================================================
INSERT INTO `item_template`
  (`id`, `name`, `detail`, `isXepChong`, `gioiTinh`, `type`, `idClass`, `idIcon`, `levelNeed`, `taiPhuNeed`, `idMob`, `idChar`, `isLock`, `sellPrice`)
VALUES
  -- Cấp 1: +5 ô, cần level 1
  (61, 'Túi Mở Rộng Cấp 1', 'Mở rộng túi đồ thêm 5 ô. Có thể gắn tối đa 3 túi.', 'False', 2, 32, 0, 0, 1,  0, -1, 0, 0, 500),
  -- Cấp 2: +5 ô, cần level 10
  (62, 'Túi Mở Rộng Cấp 2', 'Mở rộng túi đồ thêm 5 ô. Phiên bản nâng cao.', 'False', 2, 32, 0, 0, 10, 0, -1, 0, 0, 1200),
  -- Cấp 3: +5 ô, cần level 25
  (63, 'Túi Mở Rộng Cấp 3', 'Mở rộng túi đồ thêm 5 ô. Phiên bản cao cấp.', 'False', 2, 32, 0, 0, 25, 0, -1, 0, 0, 2500),
  -- Cấp 4: +5 ô, cần level 40
  (64, 'Túi Mở Rộng Cấp 4', 'Mở rộng túi đồ thêm 5 ô. Phiên bản thượng cấp hiếm.', 'False', 2, 32, 0, 0, 40, 0, -1, 0, 0, 5000)
ON DUPLICATE KEY UPDATE
  `name`       = VALUES(`name`),
  `detail`     = VALUES(`detail`),
  `type`       = VALUES(`type`),
  `levelNeed`  = VALUES(`levelNeed`),
  `sellPrice`  = VALUES(`sellPrice`);

-- ============================================================
-- Tuỳ chọn: thêm vào shop NPC (npc_id = 1 là NPC đầu tiên)
-- Bỏ comment nếu muốn bán qua NPC Shop
-- ============================================================
-- INSERT INTO `npc_shop_item` (`npc_id`, `item_template_id`, `price_silver`, `price_gold`, `stock`, `required_level`)
-- VALUES
--   (1, 61, 500,  0, -1, 1),
--   (1, 62, 1200, 0, -1, 10),
--   (1, 63, 2500, 0, -1, 25),
--   (1, 64, 0,  500, -1, 40);  -- Cấp 4 dùng vàng

-- ============================================================
-- Ghi chú icon
-- ============================================================
-- idIcon = 0 có nghĩa là không có sprite trong atlas.
-- Để set icon:
--   1. Mở game, tìm ID icon trong IconDatabase (Assets/Scripts/IconDatabase.cs)
--   2. UPDATE item_template SET idIcon = <ID> WHERE id IN (61,62,63,64);
-- Ví dụ nếu icon túi là ID 580:
--   UPDATE item_template SET idIcon = 580 WHERE id = 61;
--   UPDATE item_template SET idIcon = 581 WHERE id = 62;
--   UPDATE item_template SET idIcon = 582 WHERE id = 63;
--   UPDATE item_template SET idIcon = 583 WHERE id = 64;
