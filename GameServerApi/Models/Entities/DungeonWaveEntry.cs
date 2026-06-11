using System;

namespace GameServerApi.Models.Entities
{
    // Bảng dungeon_wave_entry — giới hạn lượt vào Phó Bản Sóng mỗi ngày per player.
    // entry_date là ngày theo UTC; record mới được tạo mỗi ngày → tự reset.
    public class DungeonWaveEntry
    {
        public int      Id           { get; set; }
        public int      PlayerId     { get; set; }
        public int      DungeonId    { get; set; }
        // Ngày UTC (DATE, không có giờ). Cặp (PlayerId, DungeonId, EntryDate) là UNIQUE.
        public DateTime EntryDate    { get; set; }
        // Số lượt đã dùng hôm nay.
        public int      EntriesUsed  { get; set; } = 0;
        // Giới hạn hôm nay (base=1 + bonus từ vé). Tăng khi player dùng vé.
        public int      EntriesLimit { get; set; } = 1;
        public DateTime UpdatedAt    { get; set; } = DateTime.UtcNow;
    }
}
