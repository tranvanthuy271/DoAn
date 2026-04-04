using GameServerApi.Models;
using GameServerApi.Models.Entities;

namespace GameServerApi.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>Hashes a plain-text password using BCrypt.</summary>
        string HashPassword(string plainText);

        /// <summary>Verifies a plain-text password against a stored BCrypt hash.</summary>
        bool VerifyPassword(string plainText, string hash);

        /// <summary>Generates a signed JWT bearer token for the given user.</summary>
        string GenerateJwtToken(User user);
    }
}
