namespace GameServerApi.Models.DTOs
{
    public class LeaderboardEntryDto
    {
        public int    Rank          { get; set; }
        public string CharacterName { get; set; } = "";
        public long   Value         { get; set; }
        /// <summary>Thông tin phụ, ví dụ: tên phó bản, element type.</summary>
        public string Extra         { get; set; } = "";
    }
}
