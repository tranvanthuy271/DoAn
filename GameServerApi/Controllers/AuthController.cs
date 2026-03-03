using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GameServerApi.Data;
using GameServerApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GameServerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly GameDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(GameDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
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

            // TODO: dùng bcrypt/argon2. Tạm thời demo: lưu plain text (không dùng cho production).
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = request.Password,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Khởi tạo PlayerData với InfoChar mặc định
            var playerData = new PlayerData
            {
                PlayerId = user.UserId
            };
            playerData.SetInfoChar(PlayerData.DefaultInfoChar("Fire"));

            _db.PlayerData.Add(playerData);
            await _db.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            var response = new
            {
                token = token,
                user_id = user.UserId,
                message = "Register thành công."
            };

            return Ok(response);
        }

        [HttpPost("login")]
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

            // TODO: so sánh password đã hash. Tạm thời so sánh plain text cho demo.
            if (user.PasswordHash != request.Password)
            {
                return Unauthorized("Sai username hoặc password.");
            }

            user.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            var response = new
            {
                token = token,
                user_id = user.UserId,
                username = user.Username
            };

            return Ok(response);
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key = jwtSection["Key"] ?? "DEV_KEY_CHANGE_ME";
            var issuer = jwtSection["Issuer"] ?? "GameServerApi";
            var audience = jwtSection["Audience"] ?? "GameClient";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim("user_id", user.UserId.ToString())
            };

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var creds = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}

