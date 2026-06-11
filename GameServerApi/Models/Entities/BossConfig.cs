namespace GameServerApi.Models.Entities
{
    // Bảng boss_config — Cấu hình spawn lịch trình cho Boss trên map.
    // Pattern từ LangLa: BossTpl.java
    // cx/cy          → SpawnX / SpawnY
    // map            → MapId
    // min_spam       → MinSpawnHour  (giờ sớm nhất được spawn)
    // hou_spam       → MaxSpawnHour  (giờ muộn nhất)
    // timeDelay      → RespawnMinutes
    public class BossConfig
    {
        // FK → enemy.enemy_id (phải là EnemyType=Boss)
        public int BossId { get; set; }

        // Map boss xuất hiện
        public int MapId { get; set; }

        public float SpawnX { get; set; } = 0f;
        public float SpawnY { get; set; } = 0f;

        // Giờ sớm nhất boss có thể spawn (0-23)
        public int MinSpawnHour { get; set; } = 0;

        // Giờ muộn nhất boss có thể spawn (0-23)
        public int MaxSpawnHour { get; set; } = 23;

        // Thời gian hồi sinh sau khi chết (phút)
        public int RespawnMinutes { get; set; } = 60;

        public bool IsActive { get; set; } = true;
    }
}
