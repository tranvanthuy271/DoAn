-- =============================================================
-- MIGRATION: Audit log table for player actions
-- Creates: player_action_log
-- =============================================================

CREATE TABLE IF NOT EXISTS `player_action_log` (
    `id`          BIGINT       NOT NULL AUTO_INCREMENT,
    `player_id`   INT          NOT NULL,
    `action_type` VARCHAR(50)  NOT NULL COMMENT 'login|level_up|equip_upgrade|gene_upgrade|fusion|item_consume|skill_upgrade',
    `detail_json` JSON         NOT NULL COMMENT 'Before/after snapshot of relevant data',
    `created_at`  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    INDEX `idx_pal_player_id`   (`player_id`),
    INDEX `idx_pal_action_type` (`action_type`),
    INDEX `idx_pal_created_at`  (`created_at`),
    -- Compound index for the most common audit query: "show all equip upgrades for player X in last 7 days"
    INDEX `idx_pal_player_type_time` (`player_id`, `action_type`, `created_at`),
    CONSTRAINT `fk_pal_player`
        FOREIGN KEY (`player_id`) REFERENCES `player_data`(`id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
  COMMENT='Audit trail for fraud detection and game economy monitoring';

-- ─────────────────────────────────────────────────────────────
-- Optional: partition by month for long-term retention
-- (enable only when row count exceeds ~5M)
-- ALTER TABLE player_action_log
--   PARTITION BY RANGE (YEAR(created_at) * 100 + MONTH(created_at)) (
--     PARTITION p_initial VALUES LESS THAN (202501),
--     PARTITION p_future   VALUES LESS THAN MAXVALUE
-- );
-- ─────────────────────────────────────────────────────────────
