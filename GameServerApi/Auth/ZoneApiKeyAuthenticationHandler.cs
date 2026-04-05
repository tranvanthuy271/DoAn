using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace GameServerApi.Auth;

public sealed class ZoneApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "ZoneApiKey";
    public const string HeaderName = "X-Zone-Api-Key";

    private readonly IConfiguration _configuration;

    public ZoneApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
            return Task.FromResult(AuthenticateResult.NoResult());

        string providedKey = headerValues.ToString();
        string expectedKey = _configuration["ZoneApiKey"] ?? string.Empty;

        if (string.IsNullOrWhiteSpace(providedKey))
            return Task.FromResult(AuthenticateResult.Fail("X-Zone-Api-Key trống."));

        if (string.IsNullOrWhiteSpace(expectedKey))
            return Task.FromResult(AuthenticateResult.Fail("ZoneApiKey chưa được cấu hình trên API."));

        if (!SecureEquals(providedKey, expectedKey))
            return Task.FromResult(AuthenticateResult.Fail("X-Zone-Api-Key không hợp lệ."));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "gameserver"),
            new Claim(ClaimTypes.Name, "GameServer"),
            new Claim(ClaimTypes.Role, "GameServer")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool SecureEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);

        if (leftBytes.Length != rightBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}