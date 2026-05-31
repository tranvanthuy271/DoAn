using GameServerApi.Data;
using GameServerApi.Models;
using GameServerApi.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly IAuthService  _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(GameDbContext db, IAuthService authService, ILogger<AuthController> logger)
        {
            _db          = db;
            _authService = authService;
            _logger      = logger;
        }

        [HttpPost("register")]
        public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username, email và password là bắt buộc.");
            }

            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Email);

            if (existingUser != null)
            {
                return BadRequest("Username hoặc email đã tồn tại.");
            }

            var user = new User
            {
                Username     = request.Username,
                Email        = request.Email,
                PasswordHash = _authService.HashPassword(request.Password),
                CreatedAt    = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var token = _authService.GenerateJwtToken(user);

            _logger.LogInformation("Register thành công: {Username} (userId={UserId})", user.Username, user.UserId);

            return Ok(new
            {
                token   = token,
                user_id = user.UserId,
                message = "Register thành công."
            });
        }

        [HttpPost("login")]
        [EnableRateLimiting("login")]
        public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Username và password là bắt buộc.");
            }

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null)
            {
                return Unauthorized("Sai username hoặc password.");
            }

            if (!_authService.VerifyPassword(request.Password, user.PasswordHash))
            {
                return Unauthorized("Sai username hoặc password.");
            }

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            // ── Điểm danh chuyên cần (1 lần / ngày, INSERT IGNORE logic) ────
            await RecordDailyAttendanceAsync(user.UserId);

            var token = _authService.GenerateJwtToken(user);

            _logger.LogInformation("Login thành công: {Username} (userId={UserId})", user.Username, user.UserId);

            return Ok(new
            {
                token    = token,
                user_id  = user.UserId,
                username = user.Username
            });
        }
        // ── Điểm danh chuyên cần ─────────────────────────────────────────────
        private async Task RecordDailyAttendanceAsync(int userId)
        {
            try
            {
                var player = await _db.PlayerData.FindAsync(userId);
                if (player == null) return;

                var info  = player.GetInfoChar();
                var today = DateTime.UtcNow.Date.ToString("yyyy-MM-dd");

                // Chỉ đếm 1 lần/ngày
                if (info.LastAttendanceDate == today) return;

                info.AttendanceCount++;
                info.LastAttendanceDate = today;
                player.SetInfoChar(info);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Auth] Không thể ghi điểm danh cho userId={UserId}: {Msg}", userId, ex.Message);
            }
        }
    }
}

