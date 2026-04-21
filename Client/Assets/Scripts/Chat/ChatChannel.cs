using UnityEngine;

/// <summary>Kênh chat trong game.</summary>
public enum ChatChannel
{
    World     = 0,   // Thế giới   – tất cả người chơi
    Proximity = 1,   // Lân cận    – hiển thị bubble trên đầu nhân vật
    Clan      = 2,   // Gia tộc
    Class     = 3,   // Lớp
    Group     = 4,   // Nhóm / Party
    Private   = 5,   // Tin riêng
}

public static class ChatChannelHelper
{
    /// <summary>Tên hiển thị trên Tab / Dropdown.</summary>
    public static string DisplayName(this ChatChannel ch) => ch switch
    {
        ChatChannel.World     => "Thế giới",
        ChatChannel.Proximity => "Lân cận",
        ChatChannel.Clan      => "Gia tộc",
        ChatChannel.Class     => "Lớp",
        ChatChannel.Group     => "Nhóm",
        ChatChannel.Private   => "Riêng",
        _                     => ch.ToString()
    };

    /// <summary>Mã viết tắt (2 ký tự) cho icon badge.</summary>
    public static string ShortCode(this ChatChannel ch) => ch switch
    {
        ChatChannel.World     => "TG",
        ChatChannel.Proximity => "LC",
        ChatChannel.Clan      => "GT",
        ChatChannel.Class     => "LO",
        ChatChannel.Group     => "N",
        ChatChannel.Private   => "R",
        _                     => "?"
    };

    /// <summary>Màu hiển thị tin nhắn của kênh.</summary>
    public static Color MessageColor(this ChatChannel ch) => ch switch
    {
        ChatChannel.World     => new Color(1f, 1f, 0.6f),       // vàng nhạt
        ChatChannel.Proximity => Color.white,
        ChatChannel.Clan      => new Color(0.5f, 1f, 0.5f),     // xanh lá
        ChatChannel.Class     => new Color(0.6f, 0.8f, 1f),     // xanh dương
        ChatChannel.Group     => new Color(1f, 0.7f, 0.3f),     // cam
        ChatChannel.Private   => new Color(1f, 0.6f, 0.8f),     // hồng
        _                     => Color.white
    };

    /// <summary>Mapping từ string channel name (server) sang enum.</summary>
    public static ChatChannel FromString(string s) => s?.ToLower() switch
    {
        "world"     => ChatChannel.World,
        "proximity" => ChatChannel.Proximity,
        "clan"      => ChatChannel.Clan,
        "class"     => ChatChannel.Class,
        "group"     => ChatChannel.Group,
        "private"   => ChatChannel.Private,
        _           => ChatChannel.World
    };
}
