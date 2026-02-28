-- =====================================================
-- FIX: Sửa code trong item_template cho đồng nhất
-- Nguyên nhân: code của item 12-15 dùng "Title Case With Space"
-- trong khi inventory JSON lưu dạng "UPPERCASE_UNDERSCORE"
-- Kết quả: GetItemTemplateByCode("ARMOR_IRON") không tìm được
-- template có code="Armor Iron" → không nhận diện được equipment
-- =====================================================

-- Kiểm tra trước khi sửa
SELECT id, code, name, category, item_type FROM item_template WHERE id IN (11, 12, 13, 14, 15);

-- Sửa code cho đồng nhất với format UPPERCASE_UNDERSCORE
UPDATE item_template SET code = 'ARMOR_IRON' WHERE id = 12;
UPDATE item_template SET code = 'PANTS_IRON' WHERE id = 13;
UPDATE item_template SET code = 'BOOTS_IRON' WHERE id = 14;
UPDATE item_template SET code = 'ACCESSORY_IRON' WHERE id = 15;

-- Kiểm tra sau khi sửa
SELECT id, code, name, category, item_type FROM item_template WHERE id IN (11, 12, 13, 14, 15);
