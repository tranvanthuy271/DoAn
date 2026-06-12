using System;
using System.Security.Cryptography;
using System.Text;

// Lightweight JWT validator chạy trong Unity (không cần external library).
// Chỉ hỗ trợ HMAC-SHA256 (HS256) — thuật toán mặc định của ASP.NET Core JWT.
// Dùng cùng secret với appsettings.json > JwtSettings > Key.
public static class JwtValidator
{
    // Kết quả sau khi validate JWT.
    public readonly struct Result
    {
        public readonly bool IsValid;
        public readonly string UserId;
        public readonly string Username;
        public readonly string ErrorMessage;

        public Result(bool valid, string userId, string username, string error)
        {
            IsValid = valid;
            UserId = userId;
            Username = username;
            ErrorMessage = error;
        }

        public static Result Fail(string reason) => new Result(false, null, null, reason);
        public static Result Ok(string userId, string username) => new Result(true, userId, username, null);
    }

    // Validate JWT token.
    // Tham số token: JWT string (3 phần cách nhau bởi dấu chấm)
    // Tham số secret: HMAC-SHA256 secret key (phải khớp với API)
    // Trả về: Result chứa IsValid, UserId, Username
    public static Result Validate(string token, string secret)
    {
        if (string.IsNullOrEmpty(token))
            return Result.Fail("Token rỗng.");

        string[] parts = token.Split('.');
        if (parts.Length != 3)
            return Result.Fail("JWT format không hợp lệ (cần 3 phần).");

        // 1 — Verify signature
        string signingInput = parts[0] + "." + parts[1];
        string expectedSig = ComputeHmacSha256Base64Url(signingInput, secret);
        if (!SecureEquals(expectedSig, parts[2]))
            return Result.Fail("Chữ ký JWT không hợp lệ.");

        // 2 — Decode payload
        string payloadJson;
        try
        {
            payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
        }
        catch (Exception ex)
        {
            return Result.Fail($"Không thể decode payload: {ex.Message}");
        }

        // 3 — Parse claims (minimal JSON parse — không dùng thư viện ngoài)
        string userId = ExtractClaim(payloadJson, "sub")
                     ?? ExtractClaim(payloadJson, "nameid")
                     ?? ExtractClaim(payloadJson, "userId");

        string username = ExtractClaim(payloadJson, "unique_name")
                       ?? ExtractClaim(payloadJson, "name")
                       ?? ExtractClaim(payloadJson, "username");

        // 4 — Check expiry
        string expStr = ExtractClaim(payloadJson, "exp");
        if (!string.IsNullOrEmpty(expStr) && long.TryParse(expStr, out long expUnix))
        {
            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (nowUnix > expUnix)
                return Result.Fail($"Token đã hết hạn (exp={expUnix}, now={nowUnix}).");
        }

        if (string.IsNullOrEmpty(userId))
            return Result.Fail("Token thiếu claim 'sub' hoặc 'userId'.");

        return Result.Ok(userId, username ?? "unknown");
    }

    // Private helpers

    private static string ComputeHmacSha256Base64Url(string input, string secret)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secret);
        byte[] dataBytes = Encoding.UTF8.GetBytes(input);
        using var hmac = new HMACSHA256(keyBytes);
        byte[] hash = hmac.ComputeHash(dataBytes);
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        return Convert.ToBase64String(input)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string input)
    {
        string padded = input.Replace('-', '+').Replace('_', '/');
        int mod = padded.Length % 4;
        if (mod == 2) padded += "==";
        else if (mod == 3) padded += "=";
        return Convert.FromBase64String(padded);
    }

    // Trích claim value từ JSON payload string.
    // Hỗ trợ string ("key":"value") và number ("key":123).
    private static string ExtractClaim(string json, string claimName)
    {
        // Tìm "claimName": sau đó lấy value
        string searchStr = "\"" + claimName + "\"";
        int idx = json.IndexOf(searchStr, StringComparison.Ordinal);
        if (idx < 0) return null;

        int colonIdx = json.IndexOf(':', idx + searchStr.Length);
        if (colonIdx < 0) return null;

        int valueStart = colonIdx + 1;
        while (valueStart < json.Length && json[valueStart] == ' ') valueStart++;

        if (valueStart >= json.Length) return null;

        if (json[valueStart] == '"')
        {
            // String value
            int endQuote = json.IndexOf('"', valueStart + 1);
            if (endQuote < 0) return null;
            return json.Substring(valueStart + 1, endQuote - valueStart - 1);
        }
        else
        {
            // Number/bool value
            int endIdx = valueStart;
            while (endIdx < json.Length && json[endIdx] != ',' && json[endIdx] != '}')
                endIdx++;
            return json.Substring(valueStart, endIdx - valueStart).Trim();
        }
    }

    // Public overload — dùng để trích claim từ JSON bất kỳ (không phải JWT payload).
    // Ví dụ: lấy "token" từ ConnectionData JSON.
    public static string ExtractClaimPublic(string json, string claimName)
        => ExtractClaim(json, claimName);

    // So sánh constant-time để tránh timing attacks.
    private static bool SecureEquals(string a, string b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;

        int result = 0;
        for (int i = 0; i < a.Length; i++)
            result |= a[i] ^ b[i];

        return result == 0;
    }
}
