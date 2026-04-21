using System;
using UnityEngine;

[Serializable]
public class PartyMemberDto
{
    public string userId = string.Empty;
    public string characterName = string.Empty;
    public int level;
    public string className = string.Empty;
    public string elementType = string.Empty;
    public bool online;
}

[Serializable]
public class PartyStatePayload
{
    public string partyId = string.Empty;
    public string leaderUserId = string.Empty;
    public bool isLocked;
    public bool autoAccept;
    public int memberCount;
    public int maxMembers;
    public PartyMemberDto[] members = Array.Empty<PartyMemberDto>();

    public static PartyStatePayload FromJson(string json)
    {
        try { return JsonUtility.FromJson<PartyStatePayload>(json); }
        catch { return new PartyStatePayload(); }
    }
}

[Serializable]
public class PartyInvitePayload
{
    public string partyId = string.Empty;
    public string leaderUserId = string.Empty;
    public string leaderName = string.Empty;

    public static PartyInvitePayload FromJson(string json)
    {
        try { return JsonUtility.FromJson<PartyInvitePayload>(json); }
        catch { return new PartyInvitePayload(); }
    }
}

[Serializable]
public class PartyJoinRequestPayload
{
    public string partyId = string.Empty;
    public string requesterUserId = string.Empty;
    public string requesterName = string.Empty;
    public int    requesterLevel;
    public string requesterElementType = string.Empty;

    public static PartyJoinRequestPayload FromJson(string json)
    {
        try { return JsonUtility.FromJson<PartyJoinRequestPayload>(json); }
        catch { return new PartyJoinRequestPayload(); }
    }
}

[Serializable]
public class PartySearchEntryDto
{
    public string partyId = string.Empty;
    public string leaderUserId = string.Empty;
    public string leaderName = string.Empty;
    public int leaderLevel;
    public string leaderClassName = string.Empty;
    public string leaderElementType = string.Empty;
    public bool isLocked;
    public bool autoAccept;
    public int memberCount;
    public int maxMembers;
    public int mapId;
    public int zoneId;
}

[Serializable]
public class PartySearchResultPayload
{
    public PartySearchEntryDto[] parties = Array.Empty<PartySearchEntryDto>();

    public static PartySearchResultPayload FromJson(string json)
    {
        try { return JsonUtility.FromJson<PartySearchResultPayload>(json); }
        catch { return new PartySearchResultPayload(); }
    }
}

[Serializable]
public class NearbyPlayerDto
{
    public string userId = string.Empty;
    public string characterName = string.Empty;
    public int level;
    public string className = string.Empty;
    public string elementType = string.Empty;
    public int mapId;
    public int zoneId;
    public bool inParty;
    public bool isPartyLeader;
}

[Serializable]
public class NearbyPlayersPayload
{
    public NearbyPlayerDto[] players = Array.Empty<NearbyPlayerDto>();

    public static NearbyPlayersPayload FromJson(string json)
    {
        try { return JsonUtility.FromJson<NearbyPlayersPayload>(json); }
        catch { return new NearbyPlayersPayload(); }
    }
}

[Serializable]
public class PartyDungeonRequestPayload
{
    public int dungeonId;
    public int mapId;
    public string dungeonType = "multi";

    public static PartyDungeonRequestPayload FromJson(string json)
    {
        try { return JsonUtility.FromJson<PartyDungeonRequestPayload>(json); }
        catch { return new PartyDungeonRequestPayload(); }
    }
}

[Serializable]
public class PartyErrorPayload
{
    public string message = string.Empty;

    public static PartyErrorPayload FromJson(string json)
    {
        try { return JsonUtility.FromJson<PartyErrorPayload>(json); }
        catch { return new PartyErrorPayload { message = json ?? string.Empty }; }
    }
}