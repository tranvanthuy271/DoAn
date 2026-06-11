using System;

namespace GameServerApi.Models
{
    // Bảng enemy_spawns - Lưu thông tin spawn enemy trong map (tọa độ, enemy_type_id, số lượng, respawn time)
    public class EnemySpawn
    {
        public int SpawnId { get; set; }
        public int MapId { get; set; }
        public int EnemyTypeId { get; set; } // FK -> enemy.enemy_id
        public float SpawnX { get; set; } = 0f; // Vị trí spawn X (Game 2D)
        public float SpawnY { get; set; } = 0f; // Vị trí spawn Y (Game 2D)
        public int MaxSpawnCount { get; set; } = 1; // Số lượng enemy tối đa spawn tại vị trí này
        public int RespawnTime { get; set; } = 30; // Thời gian respawn (giây)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property (không map vào DB, chỉ để join)
        public Enemy? Enemy { get; set; }
    }
}
