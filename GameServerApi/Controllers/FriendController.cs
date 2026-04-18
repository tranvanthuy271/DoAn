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

        // ── GET /api/friends  ─────────────────────────────────────────────────
        /// <summary>Lấy danh sách bạn bè (accepted) và lời mời đang chờ.</summary>
        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            int myId = GetMyId();
            _logger.LogInformation("[FriendController] GetFriends requested by userId={UserId}", myId);

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

            _logger.LogInformation("[FriendController] GetFriends userId={UserId} returned {Count} relation(s)", myId, result.Count);

            return Ok(result);
        }

        // ── POST /api/friends/request  ────────────────────────────────────────
        /// <summary>Gửi lời mời kết bạn đến TargetUserId.</summary>
        [HttpPost("request")]
        public async Task<IActionResult> SendRequest([FromBody] SendFriendRequestDto dto)
        {
            int myId = GetMyId();
            _logger.LogInformation("[FriendController] SendRequest from userId={UserId} to targetUserId={TargetUserId}", myId, dto.TargetUserId);

            if (dto.TargetUserId == myId) return BadRequest("Không thể kết bạn với chính mình.");

            // Kiểm tra đã tồn tại chưa
            bool exists = await _db.FriendRelations.AnyAsync(r =>
                (r.UserId == myId  && r.FriendId == dto.TargetUserId) ||
                (r.UserId == dto.TargetUserId && r.FriendId == myId));

            if (exists)
            {
                _logger.LogWarning("[FriendController] SendRequest conflict: relation already exists between {UserId} and {TargetUserId}", myId, dto.TargetUserId);
                return Conflict("Quan hệ đã tồn tại.");
            }

            // Kiểm tra target tồn tại
            bool targetExists = await _db.Users.AnyAsync(u => u.UserId == dto.TargetUserId);
            if (!targetExists)
            {
                _logger.LogWarning("[FriendController] SendRequest target not found targetUserId={TargetUserId}", dto.TargetUserId);
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

            _logger.LogInformation("[FriendController] SendRequest success relationId={RelationId} from userId={UserId} to targetUserId={TargetUserId}", relation.Id, myId, dto.TargetUserId);

            return Ok(new { message = "Đã gửi lời mời kết bạn.", relationId = relation.Id });
        }

        // ── PUT /api/friends/{id}/accept  ─────────────────────────────────────
        /// <summary>Chấp nhận lời mời kết bạn.</summary>
        [HttpPut("{id}/accept")]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            int myId = GetMyId();
            _logger.LogInformation("[FriendController] AcceptRequest relationId={RelationId} by userId={UserId}", id, myId);

            var rel = await _db.FriendRelations.FirstOrDefaultAsync(
                r => r.Id == id && r.FriendId == myId && r.Status == "pending");

            if (rel == null)
            {
                _logger.LogWarning("[FriendController] AcceptRequest failed relationId={RelationId} userId={UserId}", id, myId);
                return NotFound("Lời mời không tồn tại hoặc bạn không phải người nhận.");
            }

            rel.Status = "accepted";
            await _db.SaveChangesAsync();

            _logger.LogInformation("[FriendController] AcceptRequest success relationId={RelationId} accepterUserId={UserId} requesterUserId={RequesterId}", id, myId, rel.UserId);

            return Ok(new { message = "Đã chấp nhận lời mời kết bạn." });
        }

        // ── DELETE /api/friends/{id}  ─────────────────────────────────────────
        /// <summary>Xóa bạn hoặc từ chối lời mời.</summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveFriend(int id)
        {
            int myId = GetMyId();
            _logger.LogInformation("[FriendController] RemoveFriend relationId={RelationId} by userId={UserId}", id, myId);

            var rel = await _db.FriendRelations.FirstOrDefaultAsync(
                r => r.Id == id && (r.UserId == myId || r.FriendId == myId));

            if (rel == null)
            {
                _logger.LogWarning("[FriendController] RemoveFriend failed relationId={RelationId} userId={UserId}", id, myId);
                return NotFound("Quan hệ không tồn tại.");
            }

            _db.FriendRelations.Remove(rel);
            await _db.SaveChangesAsync();

            _logger.LogInformation("[FriendController] RemoveFriend success relationId={RelationId} removedByUserId={UserId}", id, myId);

            return Ok(new { message = "Đã xóa." });
        }

        // ── GET /api/friends/search?q=name  ──────────────────────────────────
        /// <summary>Tìm người chơi theo tên nhân vật để gửi lời mời.</summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return BadRequest("Nhập ít nhất 2 ký tự.");

            int myId = GetMyId();
            _logger.LogInformation("[FriendController] SearchUsers userId={UserId} query='{Query}'", myId, q);

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

            _logger.LogInformation("[FriendController] SearchUsers userId={UserId} query='{Query}' returned {Count} user(s)", myId, q, users.Count);

            return Ok(users);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int GetMyId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");
            return int.TryParse(claim, out int id) ? id : 0;
        }
    }
}
