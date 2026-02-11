using System;

namespace GameServerApi.Models
{
    /// <summary>
    /// Bảng player_data - lưu toàn bộ thông tin nhân vật (theo mô tả trong luuthongtin.md, rút gọn cho bước đầu).
    /// Nhiều field có thể bổ sung dần, tạm thời tập trung vào những gì client đang dùng.
    /// </summary>
    public class PlayerData
    {
        public int PlayerId { get; set; } // PK, FK -> users.user_id

        // Thông tin cơ bản
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int Gold { get; set; } = 0;
        public int MapId { get; set; } = 0;
        
        // Vị trí cuối cùng khi out game (Game 2D chỉ cần x và y)
        public float PositionX { get; set; } = 0f;
        public float PositionY { get; set; } = 0f;

        // Base stats cơ bản
        public int Hp { get; set; } = 100;
        public int MaxHp { get; set; } = 100;
        public int Mp { get; set; } = 50;
        public int MaxMp { get; set; } = 50;
        public int Attack { get; set; } = 10;

        // Hệ / Gene
        public string ElementType { get; set; } = "Fire";
        public int GeneTier { get; set; } = 1;
        public bool IsHybrid { get; set; } = false;
        public string? SecondaryElement { get; set; }
        
        // Giới tính: "Male" hoặc "Female"
        public string Gender { get; set; } = "Male";
        
        // Tên nhân vật
        public string CharacterName { get; set; } = "";

        // JSON columns
        public string EquipmentJson { get; set; } = "{}";
        public string SkillsJson { get; set; } = "[]";
        public string InventoryJson { get; set; } = "[]";
        public string PotentialStatsJson { get; set; } = "[]";

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

