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
        public string SceneName { get; set; } = "";
        public string SpawnPointsJson { get; set; } = "[]";
        public int MinLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 999;
        /// <summary>ID nhiệm vụ phải hoàn thành trước khi vào map (NULL = không yêu cầu)</summary>
        public int? RequiredQuestId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
