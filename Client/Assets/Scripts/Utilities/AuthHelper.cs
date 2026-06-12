using UnityEngine;
using UnityEngine.Networking;

// Helper tĩnh — gắn JWT header nhất quán cho mọi UnityWebRequest.
// Cách dùng:
// using var req = UnityWebRequest.Get(url);
// AuthHelper.AddAuthHeader(req);
// yield return req.SendWebRequest();
public static class AuthHelper
{
    private const string PrefKey = "JWT_TOKEN";

    // Đọc JWT từ SecureStorage và gắn Authorization header vào request.
    // Không làm gì nếu token rỗng.
    public static void AddAuthHeader(UnityWebRequest req)
    {
        string token = GetToken();
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");
    }

    // Lấy JWT token thô từ kho bảo mật.
    public static string GetToken()
    {
        return SecureStorage.GetString(PrefKey, "");
    }

    // Kiểm tra token hiện tại có tồn tại không.
    public static bool HasToken() => !string.IsNullOrEmpty(GetToken());

    // Lưu token mới vào SecureStorage.
    public static void SaveToken(string jwt) => SecureStorage.SaveString(PrefKey, jwt);

    // Xóa token (đăng xuất).
    public static void ClearToken() => SecureStorage.DeleteKey(PrefKey);
}
