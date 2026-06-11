using System.Security.Claims;
using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/friends")]
    [Authorize]
    public class FriendController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly ILogger<FriendController> _logger;

        public FriendController(GameDbContext db, ILogger<FriendController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET /api/friends
        // Lấy danh sách bạn bè (accepted) và lời mời đang chờ.
        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            int myId = GetMyId();
            { /* GetFriends requested by userId={UserId} */ }

            var relations = await _db.FriendRelations
                .Include(r => r.User)
                .Include(r => r.Friend)
                .Where(r => (r.UserId == myId || r.FriendId == myId) && r.Status != "blocked")
                .ToListAsync();

            var otherUserIds = relations
                .Select(r => r.UserId == myId ? r.FriendId : r.UserId)
                .Distinct()
                .ToArray();

            var characterNames = await _db.PlayerData
                .Where(p => otherUserIds.Contains(p.PlayerId))
                .ToDictionaryAsync(p => p.PlayerId, p => p.CharacterName);

            var result = new List<FriendEntryDto>();
            foreach (var r in relations)
            {
                bool isSender    = r.UserId == myId;
                var  otherUser   = isSender ? r.Friend : r.User;
                int  otherUserId = isSender ? r.FriendId : r.UserId;
                characterNames.TryGetValue(otherUserId, out var characterName);

                string statusLabel = r.Status switch
                {
                    "accepted"  => "accepted",
                    "pending"   => isSender ? "pending_sent" : "pending_received",
                    _           => r.Status
                };

                result.Add(new FriendEntryDto
                {
                    RelationId   = r.Id,
                    FriendUserId = otherUserId,
                    Username     = otherUser?.Username ?? "?",
                    CharacterName = string.IsNullOrWhiteSpace(characterName) ? (otherUser?.Username ?? "?") : characterName,
                    Status       = statusLabel
                });
            }

            { /* GetFriends userId={UserId} returned {Count} relation(s) */ }

            return Ok(result);
        }

        // POST /api/friends/request
        // Gửi lời mời kết bạn đến TargetUserId.
        [HttpPost("request")]
        public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto)
        {
            int myId = GetMyId();
            { /* SendRequest from userId={UserId} to targetUserId={TargetUserId} */ }

            if (dto.TargetUserId == myId) return BadRequest("Không thể kết bạn với chính mình.");

            // Kiểm tra đã tồn tại chưa
            bool exists = await _db.FriendRelations.AnyAsync(r =>
                (r.UserId == myId  && r.FriendId == dto.TargetUserId) ||
                (r.UserId == dto.TargetUserId && r.FriendId == myId));

            if (exists)
            {
                { /* Cảnh báo: SendRequest conflict: relation already exists between {UserId} and {TargetUserId} */ }
                return Conflict("Quan hệ đã tồn tại.");
            }

            // Kiểm tra target tồn tại
            bool targetExists = await _db.Users.AnyAsync(u => u.UserId == dto.TargetUserId);
            if (!targetExists)
            {
                { /* Cảnh báo: SendRequest target not found targetUserId={TargetUserId} */ }
                return NotFound("Người chơi không tồn tại.");
            }

            var relation = new FriendRelation
            {
                UserId    = myId,
                FriendId  = dto.TargetUserId,
                Status    = "pending",
                CreatedAt = DateTime.UtcNow
            };
            _db.FriendRelations.Add(relation);
            await _db.SaveChangesAsync();

            { /* SendRequest success relationId={RelationId} from userId={UserId} to targetUserId={TargetUserId} */ }

            return Ok(new { message = "Đã gửi lời mời kết bạn.", relationId = relation.Id });
        }

        // PUT /api/friends/{id}/accept
        // Chấp nhận lời mời kết bạn.
        [HttpPut("{id}/accept")]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            int myId = GetMyId();
            { /* AcceptRequest relationId={RelationId} by userId={UserId} */ }

            var rel = await _db.FriendRelations.FirstOrDefaultAsync(
                r => r.Id == id && r.FriendId == myId && r.Status == "pending");

            if (rel == null)
            {
                { /* Cảnh báo: AcceptRequest failed relationId={RelationId} userId={UserId} */ }
                return NotFound("Lời mời không tồn tại hoặc bạn không phải người nhận.");
            }

            rel.Status = "accepted";
            await _db.SaveChangesAsync();

            { /* AcceptRequest success relationId={RelationId} accepterUserId={UserId} requesterUserId={RequesterId} */ }

            return Ok(new { message = "Đã chấp nhận lời mời kết bạn." });
        }

        // DELETE /api/friends/{id}
        // Xóa bạn hoặc từ chối lời mời.
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFriend(int id)
        {
            int myId = GetMyId();
            { /* RemoveFriend relationId={RelationId} by userId={UserId} */ }

            var rel = await _db.FriendRelations.FirstOrDefaultAsync(
                r => r.Id == id && (r.UserId == myId || r.FriendId == myId));

            if (rel == null)
            {
                { /* Cảnh báo: RemoveFriend failed relationId={RelationId} userId={UserId} */ }
                return NotFound("Quan hệ không tồn tại.");
            }

            _db.FriendRelations.Remove(rel);
            await _db.SaveChangesAsync();

            { /* RemoveFriend success relationId={RelationId} removedByUserId={UserId} */ }

            return Ok(new { message = "Đã xóa." });
        }

        // GET /api/friends/search?q=name
        // Tìm người chơi theo tên nhân vật để gửi lời mời.
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest("Nhập ít nhất 2 ký tự.");

            int myId = GetMyId();
            { /* SearchUsers userId={UserId} query='{Query}' */ }

            var users = await _db.PlayerData
                .Join(
                    _db.Users,
                    player => player.PlayerId,
                    user => user.UserId,
                    (player, user) => new { player.PlayerId, player.CharacterName, user.Username })
                .Where(x => x.PlayerId != myId && x.CharacterName.Contains(q))
                .OrderBy(x => x.CharacterName)
                .Select(x => new
                {
                    userId = x.PlayerId,
                    username = x.Username,
                    characterName = string.IsNullOrWhiteSpace(x.CharacterName) ? x.Username : x.CharacterName
                })
                .Take(10)
                .ToListAsync();

            { /* SearchUsers userId={UserId} query='{Query}' returned {Count} user(s) */ }

            return Ok(users);
        }

        // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

        private int GetMyId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
            return int.TryParse(claim, out int id) ? id : 0;
        }
    }
}
