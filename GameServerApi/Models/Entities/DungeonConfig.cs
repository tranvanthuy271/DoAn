using System;

namespace GameServerApi.Models
{
    /// <summary>
    /// Bảng dungeon_config — cấu hình phó bản (dungeon / instance)
    /// </summary>
    public class DungeonConfig
    {
        public int DungeonId { get; set; }

        /// <summary>Tên phó bản hiển thị cho người chơi</summary>
        public string DungeonName { get; set; } = "";

        /// <summary>"solo" = thử thách 1 mình | "multi" = cho phép nhiều người</summary>
        public string DungeonType { get; set; } = "multi";

        /// <summary>FK -> map_config.map_id — map/scene Unity sẽ load khi vào phó bản</summary>
        public int MapId { get; set; }

        /// <summary>Tên scene Unity (phải khớp với Build Settings). VD: "Dungeon_FireCave"</summary>
        public string SceneName { get; set; } = "";

        /// <summary>Số người chơi tối đa (1 với solo, N với multi)</summary>
        public int MaxPlayers { get; set; } = 4;

        /// <summary>Level tối thiểu để vào phó bản</summary>
        public int MinLevelRequired { get; set; } = 1;

        /// <summary>Giới hạn thời gian (giây). 0 = không giới hạn</summary>
        public int TimeLimitSeconds { get; set; } = 0;

        /// <summary>Mô tả phó bản</summary>
        public string Description { get; set; } = "";

        /// <summary>FK -> enemy.enemy_id — boss của phó bản (null = không có boss riêng)</summary>
        public int? BossEnemyId { get; set; }

        /// <summary>JSON phần thưởng hoàn thành. VD: {"gold":1000,"items":[{"id":5,"qty":2}]}</summary>
        public string RewardJson { get; set; } = "{}";

        /// <summary>ID icon thumbnail hiển thị trong UI danh sách phó bản</summary>
        public string ThumbnailIconId { get; set; } = "";

        /// <summary>Có mở phó bản này cho người chơi không</summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public MapConfig? Map { get; set; }
        public Enemy? BossEnemy { get; set; }
    }
}
