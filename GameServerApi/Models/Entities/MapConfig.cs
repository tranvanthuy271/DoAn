using System;

namespace GameServerApi.Models
{
    /// <summary>
    /// Bảng map_config - Lưu thông tin map và spawn points
    /// </summary>
    public class MapConfig
    {
        public int MapId { get; set; }
        public string MapName { get; set; } = "";
        public string SpawnPointsJson { get; set; } = "[]";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
