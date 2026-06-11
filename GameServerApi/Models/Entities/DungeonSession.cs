using System;

namespace GameServerApi.Models
{
    // Bảng dungeon_session — theo dõi các session phó bản đang chạy.
    // Mỗi phó bản "multi" có thể có TỐI ĐA 1 session "waiting/active" cùng lúc.
    // Phó bản "solo" không cần session (client tự host, không đăng ký server).
    public class DungeonSession
    {
        public int SessionId { get; set; }

        // FK -> dungeon_config.dungeon_id
        public int DungeonConfigId { get; set; }

        // IP của Unity host đang chạy phó bản này
        public string HostIp { get; set; } = "";

        // Port của Unity host đang chạy phó bản này
        public int HostPort { get; set; } = 7777;

        // Số người chơi hiện tại trong session
        public int CurrentPlayers { get; set; } = 0;

        // Số người chơi tối đa (sao chép từ dungeon_config.max_players)
        public int MaxPlayers { get; set; } = 4;

        // Trạng thái: "waiting" | "active" | "ended"
        public string Status { get; set; } = "waiting";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public DungeonConfig? DungeonConfig { get; set; }
    }
}
