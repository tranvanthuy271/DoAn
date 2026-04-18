-- ── Chat & Friend System ────────────────────────────────────────────────────
-- Chạy sau các migration hiện có

CREATE TABLE IF NOT EXISTS `friend_relations` (
    `id`         INT          NOT NULL AUTO_INCREMENT,
    `user_id`    INT          NOT NULL,
    `friend_id`  INT          NOT NULL,
    `status`     VARCHAR(20)  NOT NULL DEFAULT 'pending',  -- pending | accepted | blocked
    `created_at` DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_friend_pair` (`user_id`, `friend_id`),
    KEY `idx_friend_id` (`friend_id`),
    CONSTRAINT `fk_fr_user`   FOREIGN KEY (`user_id`)   REFERENCES `users`(`user_id`) ON DELETE CASCADE,
    CONSTRAINT `fk_fr_friend` FOREIGN KEY (`friend_id`) REFERENCES `users`(`user_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
