using System.Collections.Concurrent;
using System.Security.Claims;
using GameServerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameServerApi.Hubs
{
    /// <summary>
    /// SignalR Hub cho hệ thống chat game.
    /// Hỗ trợ: Thế giới, Lân cận, Gia tộc, Lớp, Nhóm, Tin riêng.
    /// JWT Bearer được truyền qua query param ?access_token= cho WebSocket.
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        // connectionId → user info (cho private msg routing)
        private static readonly ConcurrentDictionary<string, ChatUserSession> _sessions = new();

        // ── Lifecycle ───────────────────────────────────────────────────────

        public override async Task OnConnectedAsync()
        {
            var userId   = GetUserId();
            var username = GetUsername();

            _logger.LogInformation("[ChatHub] Connected: connectionId={ConnectionId} userId={UserId} username={Username}",
                Context.ConnectionId, userId, username);

            _sessions[Context.ConnectionId] = new ChatUserSession
            {
                UserId   = userId,
                Username = username
            };

            // Thông báo người dùng online cho client của chính họ
            await Clients.Caller.SendAsync("Connected", new { userId, username });
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation("[ChatHub] Disconnected: connectionId={ConnectionId} reason={Reason}",
                Context.ConnectionId, exception?.Message ?? "client closed");
            _sessions.TryRemove(Context.ConnectionId, out _);
            await base.OnDisconnectedAsync(exception);
        }

        // ── World (Thế giới) ────────────────────────────────────────────────

        /// <summary>Gửi tin nhắn đến toàn bộ người chơi trên server.</summary>
        public async Task SendWorldMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300) return;
            var session = GetSession();
            var msg = BuildMessage(session, "world", message);
            _logger.LogInformation("[ChatHub] World: fromUserId={UserId} fromName={Username} message={Message}",
                session.UserId, session.Username, message);
            await Clients.All.SendAsync("ReceiveWorldMessage", msg);
        }

        // ── Proximity (Lân cận) ──────────────────────────────────────────────

        /// <summary>Gửi tin nhắn lân cận đến cùng map/zone.</summary>
        public async Task SendProximityMessage(string mapId, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300) return;
            var session = GetSession();
            var msg = BuildMessage(session, "proximity", message);
            await Clients.Group($"map_{mapId}").SendAsync("ReceiveProximityMessage", msg);
        }

        /// <summary>Tham gia group map khi vào map.</summary>
        public async Task JoinMap(string mapId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"map_{mapId}");
        }

        /// <summary>Rời group map khi chuyển map.</summary>
        public async Task LeaveMap(string mapId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"map_{mapId}");
        }

        // ── Clan (Gia tộc) ──────────────────────────────────────────────────

        public async Task SendClanMessage(string clanId, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300) return;
            var session = GetSession();
            var msg = BuildMessage(session, "clan", message);
            await Clients.Group($"clan_{clanId}").SendAsync("ReceiveClanMessage", msg);
        }

        public async Task JoinClan(string clanId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"clan_{clanId}");
        }

        public async Task LeaveClan(string clanId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"clan_{clanId}");
        }

        // ── Class (Lớp) ─────────────────────────────────────────────────────

        public async Task SendClassMessage(string classType, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300) return;
            var session = GetSession();
            var msg = BuildMessage(session, "class", message);
            await Clients.Group($"class_{classType}").SendAsync("ReceiveClassMessage", msg);
        }

        public async Task JoinClass(string classType)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"class_{classType}");
        }

        // ── Group / Party (Nhóm) ────────────────────────────────────────────

        public async Task SendGroupMessage(string groupId, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300) return;
            var session = GetSession();
            var msg = BuildMessage(session, "group", message);
            await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", msg);
        }

        public async Task JoinGroup(string groupId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
        }

        public async Task LeaveGroup(string groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
        }

        // ── Private (Tin riêng) ─────────────────────────────────────────────

        /// <summary>
        /// Gửi tin nhắn riêng đến người chơi khác theo userId.
        /// Cả người gửi và người nhận đều nhận được message.
        /// </summary>
        public async Task SendPrivateMessage(string targetUserId, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300) return;
            var session = GetSession();
            var msg = BuildMessage(session, "private", message, targetId: targetUserId);
            _logger.LogInformation("[ChatHub] Private: fromUserId={UserId} toUserId={TargetUserId} message={Message}",
                session.UserId, targetUserId, message);

            // Gửi đến người nhận (theo userId)
            await Clients.User(targetUserId).SendAsync("ReceivePrivateMessage", msg);
            // Echo lại cho người gửi (để hiển thị trong tab riêng)
            await Clients.Caller.SendAsync("ReceivePrivateMessage", msg);
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private ChatUserSession GetSession()
        {
            if (_sessions.TryGetValue(Context.ConnectionId, out var s)) return s;
            return new ChatUserSession { UserId = GetUserId(), Username = GetUsername() };
        }

        private string GetUserId()   => Context.UserIdentifier ?? "0";
        private string GetUsername() => Context.User?.FindFirstValue("unique_name")
                                     ?? Context.User?.FindFirstValue(ClaimTypes.Name)
                                     ?? "Unknown";

        private static ChatMessagePayload BuildMessage(
            ChatUserSession session, string channel, string message, string? targetId = null)
        {
            return new ChatMessagePayload
            {
                senderId   = session.UserId,
                senderName = session.Username,
                channel    = channel,
                targetId   = targetId ?? "",
                message    = message,
                timestamp  = DateTime.UtcNow.ToString("HH:mm")
            };
        }
    }

    public class ChatUserSession
    {
        public string UserId   { get; set; } = "";
        public string Username { get; set; } = "";
    }
}
