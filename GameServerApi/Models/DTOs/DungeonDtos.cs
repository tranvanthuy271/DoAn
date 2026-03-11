namespace GameServerApi.Models.DTOs
{
    /// <summary>Body gửi khi tạo session phó bản mới (gọi bởi host Unity)</summary>
    public class CreateDungeonSessionDto
    {
        public int DungeonConfigId { get; set; }

        /// <summary>IP public hoặc LAN của host Unity</summary>
        public string HostIp { get; set; } = "";

        /// <summary>Port Unity NetworkManager đang listen (thường 7777)</summary>
        public int HostPort { get; set; } = 7777;
    }
}
