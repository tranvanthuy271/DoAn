using System;

namespace GameServerApi.Models.Entities
{
    /// <summary>
    /// Bảng dungeon_wave_entry — giới hạn lượt vào Phó Bản Sóng mỗi ngày per player.
    /// entry_date là ngày theo UTC; record mới được tạo mỗi ngày → tự reset.
    /// </summary>
    public class DungeonWaveEntry
    {
        public int      Id           { get; set; }
        public int      PlayerId     { get; set; }
        public int      DungeonId    { get; set; }
        /// <summary>Ngày UTC (DATE, không có giờ). Cặp (PlayerId, DungeonId, EntryDate) là UNIQUE.</summary>
        public DateTime EntryDate    { get; set; }
        /// <summary>Số lượt đã dùng hôm nay.</summary>
        public int      EntriesUsed  { get; set; } = 0;
        /// <summary>Giới hạn hôm nay (base=1 + bonus từ vé). Tăng khi player dùng vé.</summary>
        public int      EntriesLimit { get; set; } = 1;
        public DateTime UpdatedAt    { get; set; } = DateTime.UtcNow;
    }
}
