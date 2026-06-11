using System;

namespace GameServerApi.Models
{
    // Bảng dungeon_config — cấu hình phó bản (dungeon / instance)
    public class DungeonConfig
    {
        public int DungeonId { get; set; }

        // Tên phó bản hiển thị cho người chơi
        public string DungeonName { get; set; } = "";

        // "solo" = thử thách 1 mình | "multi" = cho phép nhiều người
        public string DungeonType { get; set; } = "multi";

        // FK -> map_config.map_id — map/scene Unity sẽ load khi vào phó bản
        public int MapId { get; set; }

        // Tên scene Unity (phải khớp với Build Settings). VD: "Dungeon_FireCave"
        public string SceneName { get; set; } = "";

        // Số người chơi tối đa (1 với solo, N với multi)
        public int MaxPlayers { get; set; } = 4;

        // Level tối thiểu để vào phó bản
        public int MinLevelRequired { get; set; } = 1;

        // Giới hạn thời gian (giây). 0 = không giới hạn
        public int TimeLimitSeconds { get; set; } = 0;

        // Mô tả phó bản
        public string Description { get; set; } = "";

        // FK -> enemy.enemy_id — boss của phó bản (null = không có boss riêng)
        public int? BossEnemyId { get; set; }

        // JSON phần thưởng hoàn thành. VD: {"gold":1000,"items":[{"id":5,"qty":2}]}
        public string RewardJson { get; set; } = "{}";

        // ID icon thumbnail hiển thị trong UI danh sách phó bản
        public string ThumbnailIconId { get; set; } = "";

        // Có mở phó bản này cho người chơi không
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public MapConfig? Map { get; set; }
        public Enemy? BossEnemy { get; set; }
    }
}
