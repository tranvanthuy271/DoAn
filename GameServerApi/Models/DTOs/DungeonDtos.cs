namespace GameServerApi.Models.DTOs
{
    // Body gửi khi tạo session phó bản mới (gọi bởi host Unity)
    public class CreateDungeonSessionDto
    {
        public int DungeonConfigId { get; set; }

        // IP public hoặc LAN của host Unity
        public string HostIp { get; set; } = "";

        // Port Unity NetworkManager đang listen (thường 7777)
        public int HostPort { get; set; } = 7777;
    }
}
