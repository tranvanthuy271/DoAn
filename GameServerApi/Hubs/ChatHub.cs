using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;

namespace GameServerApi.Hubs
{
    // SignalR Hub cho hệ thống chat game.
    // Hỗ trợ: Thế giới, Lân cận, Gia tộc, Lớp, Nhóm, Tin riêng.
    // JWT Bearer được truyền qua query param ?access_token= cho WebSocket.
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private const int MaxDisplayNameLength = 24;

        public ChatHub(ILogger<ChatHub> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        // connectionId → user info (cho private msg routing)
        private static readonly ConcurrentDictionary<string, ChatUserSession> _sessions = new();

        // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

        public override async Task OnConnectedAsync()
        {
            var userId   = GetUserId();
            var username = GetUsername();

            _logger.LogInformation("[ChatHub] Connected: connectionId={ConnectionId} userId={UserId} username={Username}",
                Context.ConnectionId, userId, username);

            _sessions[Context.ConnectionId] = new ChatUserSession
            {
                UserId      = userId,
                Username    = username,
                DisplayName = username
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

        // World (Thế giới)

        // Gửi tin nhắn đến toàn bộ người chơi trên server.
        public async Task SendWorldMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300) return;

            // Kiểm tra lệnh chat trước khi broadcast (item, v.v.)
            if (await TryHandleChatCommandAsync(message)) return;

            var session = GetSession();
            var msg = BuildMessage(session, "world", message);
            _logger.LogInformation("[ChatHub] World: fromUserId={UserId} fromName={Username} message={Message}",
                session.UserId, session.Username, message);
            await Clients.All.SendAsync("ReceiveWorldMessage", msg);
        }

        // Proximity (Lân cận)

        // Gửi tin nhắn lân cận đến cùng map/zone. Hỗ trợ lệnh đặc biệt: "item &lt;id&gt; &lt;sốLượng&gt;".
        public async Task SendProximityMessage(string mapId, string message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Length > 300) return;

            // Kiểm tra lệnh chat trước khi broadcast
            if (await TryHandleChatCommandAsync(message)) return;

            var session = GetSession();
            var msg = BuildMessage(session, "proximity", message);
            await Clients.Group($"map_{mapId}").SendAsync("ReceiveProximityMessage", msg);
        }

        // Tham gia group map khi vào map.
        public async Task JoinMap(string mapId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"map_{mapId}");
        }

        // Rời group map khi chuyển map.
        public async Task LeaveMap(string mapId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"map_{mapId}");
        }

        // Clan (Gia tộc)

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

        // Class (Lớp)

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

        // Group / Party (Nhóm)

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

        // Đồng bộ tên hiển thị runtime của nhân vật hiện tại.
        // Chat dùng tên này thay cho username tài khoản trong JWT.
        public Task UpdateDisplayName(string displayName)
        {
            var userId = GetUserId();
            var fallbackName = GetUsername();
            var sanitizedDisplayName = SanitizeDisplayName(displayName, fallbackName);

            _sessions.AddOrUpdate(
                Context.ConnectionId,
                _ => new ChatUserSession
                {
                    UserId = userId,
                    Username = fallbackName,
                    DisplayName = sanitizedDisplayName
                },
                (_, existing) =>
                {
                    existing.UserId = string.IsNullOrWhiteSpace(existing.UserId) ? userId : existing.UserId;
                    existing.Username = string.IsNullOrWhiteSpace(existing.Username) ? fallbackName : existing.Username;
                    existing.DisplayName = sanitizedDisplayName;
                    return existing;
                });

            _logger.LogInformation(
                "[ChatHub] Updated display name: connectionId={ConnectionId} userId={UserId} displayName={DisplayName}",
                Context.ConnectionId,
                userId,
                sanitizedDisplayName);

            return Task.CompletedTask;
        }

        // Private (Tin riêng)

        // Gửi tin nhắn riêng đến người chơi khác theo userId.
        // Cả người gửi và người nhận đều nhận được message.
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

        // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

        private ChatUserSession GetSession()
        {
            if (_sessions.TryGetValue(Context.ConnectionId, out var s)) return s;

            var fallbackName = GetUsername();
            return new ChatUserSession
            {
                UserId = GetUserId(),
                Username = fallbackName,
                DisplayName = fallbackName
            };
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

        private static string SanitizeDisplayName(string? displayName, string fallbackName)
        {
            var sanitized = string.IsNullOrWhiteSpace(displayName)
                ? fallbackName
                : displayName.Trim();

            if (sanitized.Length > MaxDisplayNameLength)
                sanitized = sanitized.Substring(0, MaxDisplayNameLength);

            return string.IsNullOrWhiteSpace(sanitized) ? fallbackName : sanitized;
        }

        private static ChatMessagePayload BuildMessage(
            ChatUserSession session, string channel, string message, string? targetId = null)
        {
            return new ChatMessagePayload
            {
                senderId   = session.UserId,
                senderName = string.IsNullOrWhiteSpace(session.DisplayName) ? session.Username : session.DisplayName,
                channel    = channel,
                targetId   = targetId ?? "",
                message    = message,
                timestamp  = DateTime.UtcNow.ToString("HH:mm")
            };
        }

        private static ChatMessagePayload BuildSystemMessage(string text) => new ChatMessagePayload
        {
            senderId   = "0",
            senderName = "Hệ thống",
            channel    = "system",
            targetId   = "",
            message    = text,
            timestamp  = DateTime.UtcNow.ToString("HH:mm")
        };

        // Chat Commands

        // Xử lý lệnh chat đặc biệt. Hiện hỗ trợ:
        // item &lt;itemId&gt; &lt;sốLượng&gt;  — thêm item vào túi người gõ lệnh.
        // Trả về true nếu là lệnh (dù lỗi), false nếu là tin thường.
        private async Task<bool> TryHandleChatCommandAsync(string message)
        {
            var trimmed = message.Trim();

            // Chỉ xử lý lệnh bắt đầu bằng "item "
            if (!trimmed.StartsWith("item ", StringComparison.OrdinalIgnoreCase))
                return false;

            _logger.LogInformation("[Chat Command] Start TryHandleChatCommandAsync. Message: '{Message}'", trimmed);

            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // "item <itemId> <quantity>"
            if (parts.Length < 3)
            {
                _logger.LogWarning("[Chat Command] Invalid syntax, args count: {Count}", parts.Length);
                await Clients.Caller.SendAsync("ReceiveSystemMessage", BuildSystemMessage("Cú pháp: item <itemId> <sốLượng>  ví dụ: item 61 1"));
                return true;
            }

            if (!int.TryParse(parts[1], out int itemTemplateId) || itemTemplateId <= 0)
            {
                _logger.LogWarning("[Chat Command] Invalid item ID: {ItemId}", parts[1]);
                await Clients.Caller.SendAsync("ReceiveSystemMessage", BuildSystemMessage("itemId không hợp lệ."));
                return true;
            }

            if (!int.TryParse(parts[2], out int quantity) || quantity <= 0 || quantity > 9999)
            {
                _logger.LogWarning("[Chat Command] Invalid quantity: {Quantity}", parts[2]);
                await Clients.Caller.SendAsync("ReceiveSystemMessage", BuildSystemMessage("Số lượng không hợp lệ (1–9999)."));
                return true;
            }

            string rawUserId = GetUserId();
            _logger.LogInformation("[Chat Command] Parsed. userId: '{UserId}', itemId: {ItemId}, qty: {Qty}", rawUserId, itemTemplateId, quantity);

            if (!int.TryParse(rawUserId, out int playerId) || playerId <= 0)
            {
                _logger.LogError("[Chat Command] Cannot parse playerId <= 0 from '{RawUserId}'", rawUserId);
                return false;
            }

            _logger.LogInformation("[Chat Command] Database lookup for playerId: {PlayerId}", playerId);
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();

            var player = await db.PlayerData.FindAsync(playerId);
            if (player == null)
            {
                _logger.LogWarning("[Chat Command] Player not found: {PlayerId}", playerId);
                await Clients.Caller.SendAsync("ReceiveSystemMessage", BuildSystemMessage("Không tìm thấy tài khoản."));
                return true;
            }

            var itemTemplate = await db.ItemTemplates.FindAsync(itemTemplateId);
            if (itemTemplate == null)
            {
                _logger.LogWarning("[Chat Command] Item template not found: {TemplateId}", itemTemplateId);
                await Clients.Caller.SendAsync("ReceiveSystemMessage", BuildSystemMessage($"Item ID {itemTemplateId} không tồn tại."));
                return true;
            }

            // Đọc bag_slots
            var infoChar = player.GetInfoChar();
            int maxSlots = infoChar?.BagSlots > 0 ? infoChar.BagSlots : 20;
            _logger.LogInformation("[Chat Command] Player loaded. maxSlots: {MaxSlots}", maxSlots);

            // Parse inventory
            var inventory = new List<Dictionary<string, object>>();
            if (!string.IsNullOrEmpty(player.InventoryJson) && player.InventoryJson != "[]")
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(player.InventoryJson);
                    if (parsed != null)
                        foreach (var item in parsed)
                        {
                            var d = new Dictionary<string, object>();
                            foreach (var kv in item)
                                d[kv.Key] = kv.Value.ValueKind switch
                                {
                                    JsonValueKind.Number => kv.Value.TryGetInt32(out var v) ? (object)v : kv.Value.GetDouble(),
                                    JsonValueKind.String => kv.Value.GetString() ?? "",
                                    JsonValueKind.True   => true,
                                    JsonValueKind.False  => false,
                                    _                    => kv.Value.ToString()
                                };
                            inventory.Add(d);
                        }
                    _logger.LogInformation("[Chat Command] Loaded {Count} items from InventoryJson.", inventory.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError("[Chat Command] Error parsing inventory playerId={PlayerId}: {Err}", playerId, ex.Message);
                }
            }
            else
            {
                _logger.LogInformation("[Chat Command] Inventory is empty (or whitespace).");
            }

            bool isStackable = string.Equals(itemTemplate.IsXepChong, "True", StringComparison.OrdinalIgnoreCase);
            _logger.LogInformation("[Chat Command] Item isStackable: {IsStackable}", isStackable);

            // Gộp vào slot đã có nếu stackable
            if (isStackable)
            {
                var existing = inventory.FirstOrDefault(s =>
                    s.ContainsKey("itemTemplateId") && Convert.ToInt32(s["itemTemplateId"]) == itemTemplateId);
                if (existing != null)
                {
                    existing["quantity"] = Convert.ToInt32(existing["quantity"]) + quantity;
                    player.InventoryJson = JsonSerializer.Serialize(inventory);
                    player.UpdatedAt     = DateTime.UtcNow;
                    await db.SaveChangesAsync();
                    _logger.LogInformation("[Chat Command] AddItem(stack): added {Qty} to existing stack. New Total: {TotalQty}", quantity, existing["quantity"]);
                    await Clients.Caller.SendAsync("ReceiveSystemMessage",
                        BuildSystemMessage($"Đã thêm {quantity}x {itemTemplate.Name} vào túi đồ."));
                    return true;
                }
            }

            // Tìm slot trống
            int emptySlot = -1;
            for (int i = 0; i < maxSlots; i++)
            {
                if (!inventory.Any(s => s.ContainsKey("slotIndex") && Convert.ToInt32(s["slotIndex"]) == i))
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot == -1)
            {
                _logger.LogWarning("[Chat Command] Inventory is full. Iterated {MaxSlots} slots and couldn't find an empty one.", maxSlots);
                await Clients.Caller.SendAsync("ReceiveSystemMessage", BuildSystemMessage("Túi đồ đầy, không thêm được."));
                return true;
            }

            inventory.Add(new Dictionary<string, object>
            {
                ["slotIndex"]      = emptySlot,
                ["itemTemplateId"] = itemTemplateId,
                ["quantity"]       = quantity,
                ["upgradeLevel"]   = 0,
                ["strOptions"]     = ""
            });

            player.InventoryJson = JsonSerializer.Serialize(inventory);
            player.UpdatedAt     = DateTime.UtcNow;
            await db.SaveChangesAsync();

            _logger.LogInformation("[Chat Command] AddItem(new slot): playerId={P} itemId={I} qty={Q} slot={S}",
                playerId, itemTemplateId, quantity, emptySlot);
            await Clients.Caller.SendAsync("ReceiveSystemMessage",
                BuildSystemMessage($"Đã thêm {quantity}x {itemTemplate.Name} vào túi đồ."));
            return true;
        }
    }

    public class ChatUserSession
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
