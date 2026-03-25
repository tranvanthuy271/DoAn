-- ============================================================
-- Migration: Thêm cột isLock và sellPrice vào item_template
-- Chạy một lần trên DB để thêm các trường mới
-- ============================================================

-- Thêm cột isLock (mặc định FALSE — item thường không bị khóa)
ALTER TABLE `item_template`
    ADD COLUMN IF NOT EXISTS `isLock` TINYINT(1) NOT NULL DEFAULT 0
        COMMENT 'Item bị khóa theo loại (VD: bạc khóa). 0=không khóa, 1=khóa';

-- Thêm cột sellPrice (mặc định 0 bạc)
ALTER TABLE `item_template`
    ADD COLUMN IF NOT EXISTS `sellPrice` INT NOT NULL DEFAULT 0
        COMMENT 'Giá bán lại cho NPC (đơn vị bạc)';

-- ============================================================
-- Ví dụ: Đánh dấu các item "bạc khóa" là isLock = 1
-- Điều chỉnh id hoặc tên cho phù hợp với dữ liệu thực tế
-- ============================================================
-- UPDATE `item_template` SET isLock = 1 WHERE name LIKE '%bạc khóa%';
-- UPDATE `item_template` SET isLock = 1 WHERE name LIKE '%khóa%' AND type >= 21;

-- ============================================================
-- Ví dụ: Update giá bán cho các loại item thông dụng
-- ============================================================
-- UPDATE `item_template` SET sellPrice = 100  WHERE type = 22;  -- HP Potion
-- UPDATE `item_template` SET sellPrice = 100  WHERE type = 23;  -- MP Potion
-- UPDATE `item_template` SET sellPrice = 500  WHERE type = 21;  -- UpgradeStone
-- UPDATE `item_template` SET sellPrice = 1000 WHERE type >= 0 AND type <= 5; -- Trang bị

SELECT 'Migration hoàn tất: isLock và sellPrice đã được thêm vào item_template' AS result;
