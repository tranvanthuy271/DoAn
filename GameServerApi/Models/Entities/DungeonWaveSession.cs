using System;

namespace GameServerApi.Models.Entities
{
    // Bảng dungeon_wave_session — trạng thái wave session của player.
    // Server dùng để reconnect đúng vòng và xử lý timeout khi player offline.
    public class DungeonWaveSession
    {
        public int      SessionId          { get; set; }
        public int      PlayerId           { get; set; }
        public int      DungeonId          { get; set; }
        public int      CurrentWave        { get; set; } = 1;
        // "enemy" hoặc "boss"
        public string   CurrentPhase       { get; set; } = "enemy";
        public DateTime SessionStartedAt   { get; set; } = DateTime.UtcNow;
        public DateTime WaveStartedAt      { get; set; } = DateTime.UtcNow;
        // 1 = đang chơi, 0 = đã kết thúc.
        public bool     IsActive           { get; set; } = true;
        // "completed" | "timeout" | "left" | ""
        public string   ExitReason         { get; set; } = "";
        public DateTime UpdatedAt          { get; set; } = DateTime.UtcNow;
    }
}
