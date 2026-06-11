using System;

namespace GameServerApi.Models.Entities
{
    // Bảng lưu ranking đã tính sẵn (1 row per category).
    // id: 1=Cấp Độ / 2=Nhiệm Vụ / 3=Chuyên Cần / 4=Phó Bản / 5=Vàng
    // list: JSON array của LeaderboardEntryDto[]
    public class LeaderboardCache
    {
        public int      Id        { get; set; }
        public string   Name      { get; set; } = "";
        public string   ListJson  { get; set; } = "[]";
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
