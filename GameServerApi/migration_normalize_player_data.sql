-- =============================================================
-- MIGRATION: Normalize player_data JSON columns
-- Creates: player_equipment, player_inventory, player_skill_record
-- Run AFTER: migration_npc_system.sql, migration_map_dungeon_system.sql
-- =============================================================

-- ─────────────────────────────────────────────────────────────
-- 1. player_equipment  (replaces player_data.equipment JSON)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `player_equipment` (
    `id`               INT          NOT NULL AUTO_INCREMENT,
    `player_id`        INT          NOT NULL,
    `slot`             VARCHAR(20)  NOT NULL COMMENT 'helmet|weapon|armor|pants|boots|ring',
    `item_template_id` INT          NOT NULL,
    `upgrade_level`    INT          NOT NULL DEFAULT 0,
    `str_options`      VARCHAR(500) NOT NULL DEFAULT '' COMMENT 'optId,tierVal;optId,tierVal',
    `equipped_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_player_slot` (`player_id`, `slot`),
    INDEX `idx_pe_player_id`   (`player_id`),
    INDEX `idx_pe_template_id` (`item_template_id`),
    CONSTRAINT `fk_pe_player`
        FOREIGN KEY (`player_id`)        REFERENCES `player_data`(`id`)     ON DELETE CASCADE,
    CONSTRAINT `fk_pe_item_template`
        FOREIGN KEY (`item_template_id`) REFERENCES `item_template`(`id`)   ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Normalized equipped items — replaces equipment JSON in player_data';


-- ─────────────────────────────────────────────────────────────
-- 2. player_inventory  (replaces player_data.inventory JSON)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `player_inventory` (
    `id`               INT          NOT NULL AUTO_INCREMENT,
    `player_id`        INT          NOT NULL,
    `item_template_id` INT          NOT NULL,
    `quantity`         INT          NOT NULL DEFAULT 1,
    `slot_index`       INT          NOT NULL DEFAULT 0 COMMENT 'UI bag slot position (0-based)',
    `upgrade_level`    INT          NOT NULL DEFAULT 0,
    `str_options`      VARCHAR(500) NOT NULL DEFAULT '',
    `is_locked`        TINYINT(1)   NOT NULL DEFAULT 0,
    `acquired_at`      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_pi_player_id`   (`player_id`),
    INDEX `idx_pi_template_id` (`item_template_id`),
    CONSTRAINT `fk_pi_player`
        FOREIGN KEY (`player_id`)        REFERENCES `player_data`(`id`)     ON DELETE CASCADE,
    CONSTRAINT `fk_pi_item_template`
        FOREIGN KEY (`item_template_id`) REFERENCES `item_template`(`id`)   ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Normalized inventory — replaces inventory JSON in player_data';


-- ─────────────────────────────────────────────────────────────
-- 3. player_skill_record  (replaces player_data.skills JSON)
-- ─────────────────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS `player_skill_record` (
    `id`          INT        NOT NULL AUTO_INCREMENT,
    `player_id`   INT        NOT NULL,
    `skill_id`    INT        NOT NULL,
    `skill_level` INT        NOT NULL DEFAULT 1,
    `is_equipped` TINYINT(1) NOT NULL DEFAULT 0,
    `hotbar_slot` INT        NOT NULL DEFAULT -1 COMMENT '-1 means not on hotbar',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_player_skill` (`player_id`, `skill_id`),
    INDEX `idx_psr_player_id` (`player_id`),
    INDEX `idx_psr_skill_id`  (`skill_id`),
    CONSTRAINT `fk_psr_player`
        FOREIGN KEY (`player_id`) REFERENCES `player_data`(`id`)     ON DELETE CASCADE,
    CONSTRAINT `fk_psr_skill`
        FOREIGN KEY (`skill_id`)  REFERENCES `skill_template`(`id`)  ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Normalized skill records — replaces skills JSON in player_data';
