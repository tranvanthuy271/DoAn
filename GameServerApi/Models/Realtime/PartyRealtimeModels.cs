using System;
using System.Collections.Generic;

namespace GameServerApi.Models.Realtime
{
    public sealed class PartyMemberDto
    {
        public string userId { get; set; } = string.Empty;
        public string characterName { get; set; } = string.Empty;
        public int level { get; set; }
        public string className { get; set; } = string.Empty;
        public string elementType { get; set; } = string.Empty;
        public bool online { get; set; }
    }

    public sealed class PartyStatePayload
    {
        public string partyId { get; set; } = string.Empty;
        public string leaderUserId { get; set; } = string.Empty;
        public bool isLocked { get; set; }
        public bool autoAccept { get; set; }
        public int memberCount { get; set; }
        public int maxMembers { get; set; }
        public PartyMemberDto[] members { get; set; } = Array.Empty<PartyMemberDto>();
    }

    public sealed class PartyInvitePayload
    {
        public string partyId { get; set; } = string.Empty;
        public string leaderUserId { get; set; } = string.Empty;
        public string leaderName { get; set; } = string.Empty;
    }

    public sealed class PartyJoinRequestPayload
    {
        public string partyId { get; set; } = string.Empty;
        public string requesterUserId { get; set; } = string.Empty;
        public string requesterName { get; set; } = string.Empty;
        public int requesterLevel { get; set; }
        public string requesterElementType { get; set; } = string.Empty;
    }

    public sealed class PartySearchEntryDto
    {
        public string partyId { get; set; } = string.Empty;
        public string leaderUserId { get; set; } = string.Empty;
        public string leaderName { get; set; } = string.Empty;
        public int leaderLevel { get; set; }
        public string leaderClassName { get; set; } = string.Empty;
        public string leaderElementType { get; set; } = string.Empty;
        public bool isLocked { get; set; }
        public bool autoAccept { get; set; }
        public int memberCount { get; set; }
        public int maxMembers { get; set; }
        public int mapId { get; set; }
        public int zoneId { get; set; }
    }

    public sealed class PartySearchResultPayload
    {
        public PartySearchEntryDto[] parties { get; set; } = Array.Empty<PartySearchEntryDto>();
    }

    public sealed class NearbyPlayerDto
    {
        public string userId { get; set; } = string.Empty;
        public string characterName { get; set; } = string.Empty;
        public int level { get; set; }
        public string className { get; set; } = string.Empty;
        public string elementType { get; set; } = string.Empty;
        public int mapId { get; set; }
        public int zoneId { get; set; }
        public bool inParty { get; set; }
        public bool isPartyLeader { get; set; }
    }

    public sealed class NearbyPlayersPayload
    {
        public NearbyPlayerDto[] players { get; set; } = Array.Empty<NearbyPlayerDto>();
    }

    public sealed class PartyDungeonRequestPayload
    {
        public int dungeonId { get; set; }
        public int mapId { get; set; }
        public string dungeonType { get; set; } = "multi";
    }

    public sealed class PartyErrorPayload
    {
        public string message { get; set; } = string.Empty;
    }

    internal sealed class PartyPresenceState
    {
        public string UserId { get; set; } = string.Empty;
        public string CharacterName { get; set; } = string.Empty;
        public int Level { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public string ElementType { get; set; } = string.Empty;
        public int MapId { get; set; }
        public int ZoneId { get; set; }
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    internal sealed class PartySessionState
    {
        public string PartyId { get; set; } = string.Empty;
        public string LeaderUserId { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public bool AutoAccept { get; set; }
        public int MaxMembers { get; set; } = 4;
        public HashSet<string> MemberUserIds { get; } = new(StringComparer.Ordinal);
    }
}