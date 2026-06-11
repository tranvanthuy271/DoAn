using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using GameServerApi.Models;
using GameServerApi.Models.Entities;
using GameServerApi.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace GameServerApi.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;

        public AuthService(IConfiguration config)
        {
            _config = config;
        }

        // <inheritdoc/>
        public string HashPassword(string plainText) =>
            BCrypt.Net.BCrypt.HashPassword(plainText, workFactor: 12);

        // <inheritdoc/>
        public bool VerifyPassword(string plainText, string hash) =>
            BCrypt.Net.BCrypt.Verify(plainText, hash);

        // <inheritdoc/>
        public string GenerateJwtToken(User user)
        {
            var jwtSection = _config.GetSection("Jwt");
            var key        = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key is not configured.");
            var issuer     = jwtSection["Issuer"]   ?? "GameServerApi";
            var audience   = jwtSection["Audience"] ?? "GameClient";

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub,        user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
                new Claim("user_id",                          user.UserId.ToString()),
                new Claim(System.Security.Claims.ClaimTypes.Role, user.Role ?? "Player")
            };

            var signingKey     = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var signingCreds   = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
            var expiryDays     = int.TryParse(jwtSection["ExpiryDays"], out var d) ? d : 7;

            var token = new JwtSecurityToken(
                issuer:             issuer,
                audience:           audience,
                claims:             claims,
                expires:            DateTime.UtcNow.AddDays(expiryDays),
                signingCredentials: signingCreds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
