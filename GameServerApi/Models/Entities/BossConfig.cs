namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Bảng boss_config — Cấu hình spawn lịch trình cho Boss trên map.
    ///
    /// Pattern từ LangLa: BossTpl.java
    ///   cx/cy          → SpawnX / SpawnY
    ///   map            → MapId
    ///   min_spam       → MinSpawnHour  (giờ sớm nhất được spawn)
    ///   hou_spam       → MaxSpawnHour  (giờ muộn nhất)
    ///   timeDelay      → RespawnMinutes
    /// </summary>
    public class BossConfig
    {
        /// <summary>FK → enemy.enemy_id (phải là EnemyType=Boss)</summary>
        public int BossId { get; set; }

        /// <summary>Map boss xuất hiện</summary>
        public int MapId { get; set; }

        public float SpawnX { get; set; } = 0f;
        public float SpawnY { get; set; } = 0f;

        /// <summary>Giờ sớm nhất boss có thể spawn (0-23)</summary>
        public int MinSpawnHour { get; set; } = 0;

        /// <summary>Giờ muộn nhất boss có thể spawn (0-23)</summary>
        public int MaxSpawnHour { get; set; } = 23;

        /// <summary>Thời gian hồi sinh sau khi chết (phút)</summary>
        public int RespawnMinutes { get; set; } = 60;

        public bool IsActive { get; set; } = true;
    }
}
