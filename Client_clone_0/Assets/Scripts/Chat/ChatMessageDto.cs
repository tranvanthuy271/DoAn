using System;
using UnityEngine;

/// <summary>
/// DTO nhận từ SignalR Hub.
/// Phải match với ChatMessagePayload trên server.
/// </summary>
[Serializable]
public class ChatMessageDto
{
    public string senderId   = "";
    public string senderName = "";
    public string channel    = "";   // "world" | "proximity" | "clan" | "class" | "group" | "private"
    public string targetId   = "";   // chỉ dùng cho private
    public string message    = "";
    public string timestamp  = "";   // "HH:mm" từ server

    /// <summary>Parse từ JSON (JsonUtility).</summary>
    public static ChatMessageDto FromJson(string json)
    {
        try   { return JsonUtility.FromJson<ChatMessageDto>(json); }
        catch { return new ChatMessageDto { message = json }; }
    }

    /// <summary>Chuyển sang ChatChannel enum.</summary>
    public ChatChannel GetChannel() => ChatChannelHelper.FromString(channel);
}

/// <summary>
/// DTO bạn bè nhận từ REST API.
/// </summary>
[Serializable]
public class FriendEntryDto
{
    public int    relationId   = 0;
    public int    friendUserId = 0;
    public string username     = "";
    public string characterName = "";
    public string status       = "";  // accepted | pending_sent | pending_received
}

[Serializable]
public class FriendListResponse
{
    public FriendEntryDto[] items = Array.Empty<FriendEntryDto>();
}

[Serializable]
public class UserSearchResult
{
    public int    userId   = 0;
    public string username = "";
    public string characterName = "";
}

[Serializable]
public class UserSearchResponse
{
    public UserSearchResult[] results = Array.Empty<UserSearchResult>();
}
