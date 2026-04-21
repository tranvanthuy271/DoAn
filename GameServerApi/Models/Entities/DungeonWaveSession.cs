using System;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Bảng dungeon_wave_session — trạng thái wave session của player.
    /// Server dùng để reconnect đúng vòng và xử lý timeout khi player offline.
    /// </summary>
    public class DungeonWaveSession
    {
        public int      SessionId          { get; set; }
        public int      PlayerId           { get; set; }
        public int      DungeonId          { get; set; }
        public int      CurrentWave        { get; set; } = 1;
        /// <summary>"enemy" hoặc "boss"</summary>
        public string   CurrentPhase       { get; set; } = "enemy";
        public DateTime SessionStartedAt   { get; set; } = DateTime.UtcNow;
        public DateTime WaveStartedAt      { get; set; } = DateTime.UtcNow;
        /// <summary>1 = đang chơi, 0 = đã kết thúc.</summary>
        public bool     IsActive           { get; set; } = true;
        /// <summary>"completed" | "timeout" | "left" | ""</summary>
        public string   ExitReason         { get; set; } = "";
        public DateTime UpdatedAt          { get; set; } = DateTime.UtcNow;
    }
}
