namespace GameServerApi.Models
{
    // Chat DTOs

    // Payload được gửi từ Hub đến client cho mỗi tin nhắn.
    public class ChatMessagePayload
    {
        public string senderId   { get; set; } = "";
        public string senderName { get; set; } = "";
        public string channel    { get; set; } = "";   // world|proximity|clan|class|group|private
        public string targetId   { get; set; } = "";   // chỉ dùng cho private
        public string message    { get; set; } = "";
        public string timestamp  { get; set; } = "";   // "HH:mm"
    }

    // Friend DTOs

    public class SendFriendRequestDto
    {
        public int TargetUserId { get; set; }
    }

    public class FriendEntryDto
    {
        public int    RelationId   { get; set; }
        public int    FriendUserId { get; set; }
        public string Username     { get; set; } = "";
        public string CharacterName { get; set; } = "";
        public string Status       { get; set; } = "";  // accepted | pending_sent | pending_received
    }
}
