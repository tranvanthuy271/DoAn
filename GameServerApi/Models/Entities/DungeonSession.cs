using System;

namespace GameServerApi.Models
{
    /// <summary>
    /// Bảng dungeon_session — theo dõi các session phó bản đang chạy.
    /// Mỗi phó bản "multi" có thể có TỐI ĐA 1 session "waiting/active" cùng lúc.
    /// Phó bản "solo" không cần session (client tự host, không đăng ký server).
    /// </summary>
    public class DungeonSession
    {
        public int SessionId { get; set; }

        /// <summary>FK -> dungeon_config.dungeon_id</summary>
        public int DungeonConfigId { get; set; }

        /// <summary>IP của Unity host đang chạy phó bản này</summary>
        public string HostIp { get; set; } = "";

        /// <summary>Port của Unity host đang chạy phó bản này</summary>
        public int HostPort { get; set; } = 7777;

        /// <summary>Số người chơi hiện tại trong session</summary>
        public int CurrentPlayers { get; set; } = 0;

        /// <summary>Số người chơi tối đa (sao chép từ dungeon_config.max_players)</summary>
        public int MaxPlayers { get; set; } = 4;

        /// <summary>Trạng thái: "waiting" | "active" | "ended"</summary>
        public string Status { get; set; } = "waiting";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public DungeonConfig? DungeonConfig { get; set; }
    }
}
