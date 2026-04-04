-- =============================================================
-- MIGRATION: Add performance indexes to hot query tables
-- Chạy SAU tất cả migration tạo table (đặc biệt sau
-- migration_map_dungeon_system.sql).
--
-- Các index đã tồn tại trong gamedb.sql KHÔNG được thêm lại
-- ở đây (PRIMARY KEY, uq_skill_code, idx_item_type,
-- idx_item_level, idx_enemy_level, idx_enemy_element,
-- idx_ds_status, idx_ds_created).
-- =============================================================

-- skill_template: tìm theo nguyên tố, tier gene, cấp mở khoá
ALTER TABLE `skill_template`
    ADD INDEX IF NOT EXISTS `idx_st_element_type`  (`element_type`),
    ADD INDEX IF NOT EXISTS `idx_st_level_unlock`  (`level_to_unlock`),
    ADD INDEX IF NOT EXISTS `idx_st_gene_tier`     (`gene_tier_required`);

-- item_template: tìm theo class nhân vật và trạng thái khoá
ALTER TABLE `item_template`
    ADD INDEX IF NOT EXISTS `idx_it_class`   (`idClass`),
    ADD INDEX IF NOT EXISTS `idx_it_is_lock` (`isLock`);

-- enemy: tìm theo loại (Normal/Elite/Boss)
ALTER TABLE `enemy`
    ADD INDEX IF NOT EXISTS `idx_en_enemy_type` (`enemy_type`);

-- enemy_spawns: tìm theo map (bảng này thay thế enemy_spawn cũ)
ALTER TABLE `enemy_spawns`
    ADD INDEX IF NOT EXISTS `idx_es_map_id`      (`map_id`),
    ADD INDEX IF NOT EXISTS `idx_es_enemy_type`  (`enemy_type_id`);

-- dungeon_config: tìm phó bản còn hoạt động và theo cấp
ALTER TABLE `dungeon_config`
    ADD INDEX IF NOT EXISTS `idx_dc_is_active`   (`is_active`),
    ADD INDEX IF NOT EXISTS `idx_dc_min_level`   (`min_level_required`);

-- npc_config: tìm NPC theo map và loại
ALTER TABLE `npc_config`
    ADD INDEX IF NOT EXISTS `idx_nc_map_id`   (`map_id`),
    ADD INDEX IF NOT EXISTS `idx_nc_npc_type` (`npc_type`);

-- option_template: tìm option theo loại và cấp
ALTER TABLE `option_template`
    ADD INDEX IF NOT EXISTS `idx_ot_type`  (`type`),
    ADD INDEX IF NOT EXISTS `idx_ot_level` (`level`);

-- map_config: tìm map theo khoảng cấp độ
ALTER TABLE `map_config`
    ADD INDEX IF NOT EXISTS `idx_mc_min_level` (`min_level`),
    ADD INDEX IF NOT EXISTS `idx_mc_max_level` (`max_level`);
