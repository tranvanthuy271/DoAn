using GameServerApi.Models;
using GameServerApi.Models.Entities;

namespace GameServerApi.Services.Interfaces
{
    public interface IAuthService
    {
        // Hashes a plain-text password using BCrypt.
        string HashPassword(string plainText);

        // Verifies a plain-text password against a stored BCrypt hash.
        bool VerifyPassword(string plainText, string hash);

        // Generates a signed JWT bearer token for the given user.
        string GenerateJwtToken(User user);
    }
}
