using UnityEngine;

// Kenh chat trong game.
public enum ChatChannel
{
    World     = 0,
    Proximity = 1,
    Clan      = 2,
    Class     = 3,
    Group     = 4,
    Private   = 5,
}

public static class ChatChannelHelper
{
    // Ten hien thi tren Tab / Dropdown.
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

    // Ma viet tat cho icon badge.
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

    // Mau ten nguoi gui theo tung kenh, toi uu tren nen giay sang.
    public static Color MessageColor(this ChatChannel ch) => ch switch
    {
        ChatChannel.World     => new Color32(0xA3, 0x61, 0x10, 0xFF),
        ChatChannel.Proximity => new Color32(0x4F, 0x66, 0x73, 0xFF),
        ChatChannel.Clan      => new Color32(0x3E, 0x7A, 0x2E, 0xFF),
        ChatChannel.Class     => new Color32(0x2D, 0x5C, 0x91, 0xFF),
        ChatChannel.Group     => new Color32(0x9B, 0x52, 0x19, 0xFF),
        ChatChannel.Private   => new Color32(0x9A, 0x3F, 0x61, 0xFF),
        _                     => new Color32(0x3E, 0x29, 0x18, 0xFF)
    };

    // Mapping tu string channel name (server) sang enum.
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
