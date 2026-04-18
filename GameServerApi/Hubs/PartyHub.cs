using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using GameServerApi.Models.Realtime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameServerApi.Hubs
{
    [Authorize]
    public class PartyHub : Hub
    {
        private const int MaxPartyMembers = 4;
        private const int MaxNameLength = 32;
        private const int MaxClassNameLength = 24;

        private static readonly object SyncRoot = new();
        private static readonly ConcurrentDictionary<string, PartySessionState> Parties = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, PartyPresenceState> PresenceByUser = new(StringComparer.Ordinal);
        private static readonly ConcurrentDictionary<string, HashSet<string>> ConnectionsByUser = new(StringComparer.Ordinal);

        private readonly ILogger<PartyHub> _logger;

        public PartyHub(ILogger<PartyHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            string userId = GetUserId();
            lock (SyncRoot)
            {
                if (!ConnectionsByUser.TryGetValue(userId, out var set))
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    ConnectionsByUser[userId] = set;
                }

                set.Add(Context.ConnectionId);
            }

            _logger.LogInformation("[PartyHub] Connected userId={UserId} connectionId={ConnectionId}", userId, Context.ConnectionId);

            string partyId = string.Empty;
            lock (SyncRoot)
            {
                partyId = FindPartyByMemberUnsafe(userId)?.PartyId ?? string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(partyId))
                await Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(partyId));

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            string userId = GetUserId();
            string disbandedPartyId = string.Empty;
            string updatedPartyId = string.Empty;
            PartyStatePayload payload = null;

            lock (SyncRoot)
            {
                if (ConnectionsByUser.TryGetValue(userId, out var set))
                {
                    set.Remove(Context.ConnectionId);
                    if (set.Count == 0)
                    {
                        ConnectionsByUser.TryRemove(userId, out _);
                        PresenceByUser.TryRemove(userId, out _);

                        var party = FindPartyByMemberUnsafe(userId);
                        if (party != null)
                        {
                            party.MemberUserIds.Remove(userId);
                            if (party.MemberUserIds.Count == 0)
                            {
                                Parties.TryRemove(party.PartyId, out _);
                                disbandedPartyId = party.PartyId;
                            }
                            else
                            {
                                if (string.Equals(party.LeaderUserId, userId, StringComparison.Ordinal))
                                    party.LeaderUserId = party.MemberUserIds.First();

                                updatedPartyId = party.PartyId;
                                payload = BuildPartyStateUnsafe(party);
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(disbandedPartyId))
                await Clients.Group(BuildGroupName(disbandedPartyId)).SendAsync("PartyDisbanded");

            if (!string.IsNullOrWhiteSpace(updatedPartyId) && payload != null)
                await Clients.Group(BuildGroupName(updatedPartyId)).SendAsync("PartyStateUpdated", payload);

            _logger.LogInformation("[PartyHub] Disconnected userId={UserId} connectionId={ConnectionId}", userId, Context.ConnectionId);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task UpdatePresence(string mapId, string zoneId, string level, string characterName, string className, string elementType)
        {
            string userId = GetUserId();

            lock (SyncRoot)
            {
                PresenceByUser[userId] = new PartyPresenceState
                {
                    UserId = userId,
                    MapId = ParseInt(mapId),
                    ZoneId = ParseInt(zoneId),
                    Level = Math.Max(1, ParseInt(level)),
                    CharacterName = SanitizeLabel(characterName, MaxNameLength, GetUsername()),
                    ClassName = SanitizeLabel(className, MaxClassNameLength, "Khác"),
                    ElementType = SanitizeLabel(string.IsNullOrWhiteSpace(elementType) ? className : elementType, MaxClassNameLength, string.Empty),
                    UpdatedAtUtc = DateTime.UtcNow
                };
            }

            await Task.CompletedTask;
        }

        public async Task CreateParty()
        {
            string userId = GetUserId();
            PartyStatePayload payload;
            string partyId;

            lock (SyncRoot)
            {
                EnsureUserNotInExistingPartyUnsafe(userId);
                var party = CreatePartyUnsafe(userId);
                partyId = party.PartyId;
                payload = BuildPartyStateUnsafe(party);
            }

            await AddUserConnectionsToGroup(userId, partyId);
            await Clients.Group(BuildGroupName(partyId)).SendAsync("PartyStateUpdated", payload);
        }

        public async Task InviteMember(string targetUserId)
        {
            string callerUserId = GetUserId();
            targetUserId = targetUserId?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(targetUserId) || string.Equals(targetUserId, callerUserId, StringComparison.Ordinal))
            {
                await SendErrorAsync("Không thể mời người chơi này.");
                return;
            }

            PartyInvitePayload payload;
            string partyId;

            lock (SyncRoot)
            {
                var party = FindPartyByLeaderUnsafe(callerUserId) ?? CreatePartyUnsafe(callerUserId);
                if (party.MemberUserIds.Count >= party.MaxMembers)
                {
                    payload = null;
                    partyId = string.Empty;
                }
                else
                {
                    partyId = party.PartyId;
                    payload = new PartyInvitePayload
                    {
                        partyId = party.PartyId,
                        leaderUserId = callerUserId,
                        leaderName = ResolveDisplayNameUnsafe(callerUserId)
                    };
                }
            }

            if (payload == null || string.IsNullOrWhiteSpace(partyId))
            {
                await SendErrorAsync("Tổ đội đã đầy.");
                return;
            }

            await AddUserConnectionsToGroup(callerUserId, partyId);
            await Clients.User(targetUserId).SendAsync("PartyInviteReceived", payload);
        }

        public async Task RequestJoinParty(string partyId)
        {
            string callerUserId = GetUserId();
            partyId = partyId?.Trim() ?? string.Empty;
            PartyStatePayload payload = null;
            string leaderUserId = string.Empty;
            PartyJoinRequestPayload joinRequestPayload = null;
            string errorMessage = string.Empty;

            lock (SyncRoot)
            {
                if (!Parties.TryGetValue(partyId, out var party))
                    return;

                var existingParty = FindPartyByMemberUnsafe(callerUserId);
                if (existingParty != null && !string.Equals(existingParty.PartyId, partyId, StringComparison.Ordinal))
                {
                    errorMessage = "Bạn đang ở trong một tổ đội khác.";
                }
                else if (existingParty != null)
                {
                    payload = BuildPartyStateUnsafe(party);
                }
                else if (party.MemberUserIds.Count >= party.MaxMembers)
                {
                    errorMessage = "Tổ đội đã đầy.";
                }
                else if (party.IsLocked)
                {
                    errorMessage = "Tổ đội đang khóa, không thể xin vào.";
                }
                else if (party.AutoAccept)
                {
                    party.MemberUserIds.Add(callerUserId);
                    payload = BuildPartyStateUnsafe(party);
                }
                else
                {
                    leaderUserId = party.LeaderUserId;
                    joinRequestPayload = new PartyJoinRequestPayload
                    {
                        partyId = partyId,
                        requesterUserId = callerUserId,
                        requesterName = ResolveDisplayNameUnsafe(callerUserId),
                        requesterLevel = ResolveLevelUnsafe(callerUserId),
                        requesterElementType = ResolveElementTypeUnsafe(callerUserId)
                    };
                }
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                await SendErrorAsync(errorMessage);
                return;
            }

            if (payload != null)
            {
                await AddUserConnectionsToGroup(callerUserId, partyId);
                await Clients.Group(BuildGroupName(partyId)).SendAsync("PartyStateUpdated", payload);
                return;
            }

            if (!string.IsNullOrWhiteSpace(leaderUserId) && joinRequestPayload != null)
                await Clients.User(leaderUserId).SendAsync("PartyJoinRequestReceived", joinRequestPayload);
        }

        public async Task AcceptJoinRequest(string partyId, string requesterUserId)
        {
            string callerUserId = GetUserId();
            partyId = partyId?.Trim() ?? string.Empty;
            requesterUserId = requesterUserId?.Trim() ?? string.Empty;
            PartyStatePayload payload = null;
            string errorMessage = string.Empty;

            lock (SyncRoot)
            {
                if (!Parties.TryGetValue(partyId, out var party))
                    return;

                if (!string.Equals(party.LeaderUserId, callerUserId, StringComparison.Ordinal))
                    return;

                if (party.MemberUserIds.Count >= party.MaxMembers)
                {
                    errorMessage = "Tổ đội đã đầy.";
                }
                else
                {
                    var existingParty = FindPartyByMemberUnsafe(requesterUserId);
                    if (existingParty != null && !string.Equals(existingParty.PartyId, partyId, StringComparison.Ordinal))
                    {
                        errorMessage = "Người chơi này đang ở trong một tổ đội khác.";
                    }
                    else if (existingParty != null)
                    {
                        payload = BuildPartyStateUnsafe(party);
                    }
                    else
                    {
                        party.MemberUserIds.Add(requesterUserId);
                        payload = BuildPartyStateUnsafe(party);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                await Clients.User(callerUserId).SendAsync("PartyError", new PartyErrorPayload { message = errorMessage });
                await Clients.User(requesterUserId).SendAsync("PartyError", new PartyErrorPayload { message = errorMessage });
                return;
            }

            if (payload == null)
                return;

            await AddUserConnectionsToGroup(requesterUserId, partyId);
            await Clients.Group(BuildGroupName(partyId)).SendAsync("PartyStateUpdated", payload);
        }

        public async Task RejectJoinRequest(string partyId, string requesterUserId)
        {
            string callerUserId = GetUserId();
            bool validLeader;

            lock (SyncRoot)
            {
                validLeader = Parties.TryGetValue(partyId ?? string.Empty, out var party)
                              && string.Equals(party.LeaderUserId, callerUserId, StringComparison.Ordinal);
            }

            if (!validLeader)
                return;

            await Clients.User(requesterUserId ?? string.Empty).SendAsync("PartyError", new PartyErrorPayload
            {
                message = "Yêu cầu vào nhóm đã bị từ chối."
            });
        }

        public async Task LeaveParty()
        {
            string callerUserId = GetUserId();
            string disbandedPartyId = string.Empty;
            string updatedPartyId = string.Empty;
            PartyStatePayload payload = null;

            lock (SyncRoot)
            {
                var party = FindPartyByMemberUnsafe(callerUserId);
                if (party == null)
                    return;

                party.MemberUserIds.Remove(callerUserId);
                if (party.MemberUserIds.Count == 0)
                {
                    Parties.TryRemove(party.PartyId, out _);
                    disbandedPartyId = party.PartyId;
                }
                else
                {
                    if (string.Equals(party.LeaderUserId, callerUserId, StringComparison.Ordinal))
                        party.LeaderUserId = party.MemberUserIds.First();

                    updatedPartyId = party.PartyId;
                    payload = BuildPartyStateUnsafe(party);
                }
            }

            await RemoveUserConnectionsFromGroup(callerUserId, string.IsNullOrWhiteSpace(disbandedPartyId) ? updatedPartyId : disbandedPartyId);
            await Clients.User(callerUserId).SendAsync("PartyDisbanded");

            if (!string.IsNullOrWhiteSpace(disbandedPartyId))
            {
                await Clients.Group(BuildGroupName(disbandedPartyId)).SendAsync("PartyDisbanded");
                return;
            }

            if (!string.IsNullOrWhiteSpace(updatedPartyId) && payload != null)
                await Clients.Group(BuildGroupName(updatedPartyId)).SendAsync("PartyStateUpdated", payload);
        }

        public async Task DisbandParty()
        {
            string callerUserId = GetUserId();
            string partyId = string.Empty;
            List<string> members = new();

            lock (SyncRoot)
            {
                var party = FindPartyByLeaderUnsafe(callerUserId);
                if (party == null)
                    return;

                partyId = party.PartyId;
                members.AddRange(party.MemberUserIds);
                Parties.TryRemove(party.PartyId, out _);
            }

            foreach (string member in members)
                await RemoveUserConnectionsFromGroup(member, partyId);

            if (members.Count > 0)
                await Clients.Users(members).SendAsync("PartyDisbanded");
        }

        public async Task SetLock(string locked)
        {
            await UpdatePartyFlagAsync(locked, setAutoAccept: false);
        }

        public async Task SetAutoAccept(string autoAccept)
        {
            await UpdatePartyFlagAsync(autoAccept, setAutoAccept: true);
        }

        public async Task GetPartiesInZone(string mapId, string zoneId)
        {
            string callerUserId = GetUserId();
            int map = ParseInt(mapId);
            int zone = ParseInt(zoneId);
            PartySearchResultPayload payload;

            lock (SyncRoot)
            {
                payload = new PartySearchResultPayload
                {
                    parties = Parties.Values
                        .Where(p => p.MemberUserIds.Count > 0 && p.MemberUserIds.Count < p.MaxMembers)
                        .Select(BuildPartySearchEntryUnsafe)
                        .Where(p => p != null && p.mapId == map && p.zoneId == zone)
                        .OrderBy(p => p.isLocked)
                        .ThenBy(p => p.leaderName)
                        .ToArray()
                };
            }

            await Clients.Caller.SendAsync("PartySearchResults", payload);
        }

        public async Task GetNearbyPlayers(string mapId, string zoneId)
        {
            int map = ParseInt(mapId);
            int zone = ParseInt(zoneId);
            NearbyPlayersPayload payload;

            lock (SyncRoot)
            {
                payload = new NearbyPlayersPayload
                {
                    players = PresenceByUser.Values
                        .Where(p => p.MapId == map && p.ZoneId == zone)
                        .OrderByDescending(p => p.Level)
                        .ThenBy(p => p.CharacterName)
                        .Select(p => new NearbyPlayerDto
                        {
                            userId = p.UserId,
                            characterName = p.CharacterName,
                            level = p.Level,
                            className = p.ClassName,
                            elementType = p.ElementType,
                            mapId = p.MapId,
                            zoneId = p.ZoneId,
                            inParty = FindPartyByMemberUnsafe(p.UserId) != null,
                            isPartyLeader = string.Equals(FindPartyByMemberUnsafe(p.UserId)?.LeaderUserId, p.UserId, StringComparison.Ordinal)
                        })
                        .ToArray()
                };
            }

            await Clients.Caller.SendAsync("NearbyPlayersUpdated", payload);
        }

        public async Task StartPartyDungeon(string dungeonId, string mapId, string dungeonType)
        {
            string callerUserId = GetUserId();
            string partyId;

            lock (SyncRoot)
            {
                var party = FindPartyByLeaderUnsafe(callerUserId) ?? CreatePartyUnsafe(callerUserId);
                if (!string.Equals(party.LeaderUserId, callerUserId, StringComparison.Ordinal))
                    return;

                partyId = party.PartyId;
            }

            await AddUserConnectionsToGroup(callerUserId, partyId);
            await Clients.Group(BuildGroupName(partyId)).SendAsync("PartyDungeonRequested", new PartyDungeonRequestPayload
            {
                dungeonId = ParseInt(dungeonId),
                mapId = ParseInt(mapId),
                dungeonType = string.IsNullOrWhiteSpace(dungeonType) ? "multi" : dungeonType.Trim().ToLowerInvariant()
            });
        }

        private async Task UpdatePartyFlagAsync(string rawValue, bool setAutoAccept)
        {
            string callerUserId = GetUserId();
            string partyId = string.Empty;
            PartyStatePayload payload = null;

            lock (SyncRoot)
            {
                var party = FindPartyByLeaderUnsafe(callerUserId);
                if (party == null)
                    return;

                if (setAutoAccept)
                    party.AutoAccept = ParseBool(rawValue);
                else
                    party.IsLocked = ParseBool(rawValue);

                partyId = party.PartyId;
                payload = BuildPartyStateUnsafe(party);
            }

            if (!string.IsNullOrWhiteSpace(partyId) && payload != null)
                await Clients.Group(BuildGroupName(partyId)).SendAsync("PartyStateUpdated", payload);
        }

        private async Task SendErrorAsync(string message)
        {
            await Clients.Caller.SendAsync("PartyError", new PartyErrorPayload { message = message });
        }

        private PartySessionState CreatePartyUnsafe(string leaderUserId)
        {
            var party = new PartySessionState
            {
                PartyId = Guid.NewGuid().ToString("N")[..8],
                LeaderUserId = leaderUserId,
                MaxMembers = MaxPartyMembers
            };

            party.MemberUserIds.Add(leaderUserId);
            Parties[party.PartyId] = party;
            return party;
        }

        private void EnsureUserNotInExistingPartyUnsafe(string userId)
        {
            var party = FindPartyByMemberUnsafe(userId);
            if (party == null)
                return;

            party.MemberUserIds.Remove(userId);
            if (party.MemberUserIds.Count == 0)
            {
                Parties.TryRemove(party.PartyId, out _);
                return;
            }

            if (string.Equals(party.LeaderUserId, userId, StringComparison.Ordinal))
                party.LeaderUserId = party.MemberUserIds.First();
        }

        private PartySessionState FindPartyByMemberUnsafe(string userId)
        {
            return Parties.Values.FirstOrDefault(p => p.MemberUserIds.Contains(userId));
        }

        private PartySessionState FindPartyByLeaderUnsafe(string userId)
        {
            return Parties.Values.FirstOrDefault(p => string.Equals(p.LeaderUserId, userId, StringComparison.Ordinal));
        }

        private PartyStatePayload BuildPartyStateUnsafe(PartySessionState party)
        {
            return new PartyStatePayload
            {
                partyId = party.PartyId,
                leaderUserId = party.LeaderUserId,
                isLocked = party.IsLocked,
                autoAccept = party.AutoAccept,
                memberCount = party.MemberUserIds.Count,
                maxMembers = party.MaxMembers,
                members = party.MemberUserIds
                    .Select(BuildPartyMemberUnsafe)
                    .OrderByDescending(m => string.Equals(m.userId, party.LeaderUserId, StringComparison.Ordinal))
                    .ThenBy(m => m.characterName)
                    .ToArray()
            };
        }

        private PartySearchEntryDto BuildPartySearchEntryUnsafe(PartySessionState party)
        {
            if (!PresenceByUser.TryGetValue(party.LeaderUserId, out var leader))
                return null;

            return new PartySearchEntryDto
            {
                partyId = party.PartyId,
                leaderUserId = party.LeaderUserId,
                leaderName = leader.CharacterName,
                leaderLevel = leader.Level,
                leaderClassName = leader.ClassName,
                leaderElementType = leader.ElementType,
                isLocked = party.IsLocked,
                autoAccept = party.AutoAccept,
                memberCount = party.MemberUserIds.Count,
                maxMembers = party.MaxMembers,
                mapId = leader.MapId,
                zoneId = leader.ZoneId
            };
        }

        private PartyMemberDto BuildPartyMemberUnsafe(string userId)
        {
            if (!PresenceByUser.TryGetValue(userId, out var presence))
            {
                return new PartyMemberDto
                {
                    userId = userId,
                    characterName = userId,
                    level = 1,
                    className = "Khác",
                    elementType = string.Empty,
                    online = ConnectionsByUser.TryGetValue(userId, out var set) && set.Count > 0
                };
            }

            return new PartyMemberDto
            {
                userId = userId,
                characterName = presence.CharacterName,
                level = presence.Level,
                className = presence.ClassName,
                elementType = presence.ElementType,
                online = ConnectionsByUser.TryGetValue(userId, out var onlineConnections) && onlineConnections.Count > 0
            };
        }

        private int ResolveLevelUnsafe(string userId)
        {
            return PresenceByUser.TryGetValue(userId, out var presence)
                ? Math.Max(1, presence.Level)
                : 1;
        }

        private string ResolveElementTypeUnsafe(string userId)
        {
            if (!PresenceByUser.TryGetValue(userId, out var presence))
                return string.Empty;

            return !string.IsNullOrWhiteSpace(presence.ElementType)
                ? presence.ElementType
                : presence.ClassName;
        }

        private string ResolveDisplayName(string userId)
        {
            lock (SyncRoot)
            {
                return ResolveDisplayNameUnsafe(userId);
            }
        }

        private string ResolveDisplayNameUnsafe(string userId)
        {
            return PresenceByUser.TryGetValue(userId, out var presence)
                ? presence.CharacterName
                : userId;
        }

        private async Task AddUserConnectionsToGroup(string userId, string partyId)
        {
            List<string> connections;
            lock (SyncRoot)
            {
                connections = ConnectionsByUser.TryGetValue(userId, out var set) ? set.ToList() : new List<string>();
            }

            foreach (string connectionId in connections)
                await Groups.AddToGroupAsync(connectionId, BuildGroupName(partyId));
        }

        private async Task RemoveUserConnectionsFromGroup(string userId, string partyId)
        {
            if (string.IsNullOrWhiteSpace(partyId))
                return;

            List<string> connections;
            lock (SyncRoot)
            {
                connections = ConnectionsByUser.TryGetValue(userId, out var set) ? set.ToList() : new List<string>();
            }

            foreach (string connectionId in connections)
                await Groups.RemoveFromGroupAsync(connectionId, BuildGroupName(partyId));
        }

        private string GetUserId()
        {
            return Context.UserIdentifier
                ?? Context.User?.FindFirstValue("user_id")
                ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "0";
        }

        private string GetUsername()
        {
            return Context.User?.FindFirstValue("unique_name")
                ?? Context.User?.FindFirstValue(ClaimTypes.Name)
                ?? GetUserId();
        }

        private static string BuildGroupName(string partyId) => $"party_{partyId}";
        private static int ParseInt(string raw) => int.TryParse(raw, out var value) ? value : 0;
        private static bool ParseBool(string raw) => bool.TryParse(raw, out var value) && value;

        private static string SanitizeLabel(string value, int maxLength, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            value = value.Trim();
            return value.Length > maxLength ? value[..maxLength] : value;
        }
    }
}