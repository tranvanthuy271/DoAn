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

    // Đọc JWT từ PlayerPrefs và gắn Authorization header vào request.
    // Không làm gì nếu token rỗng.
    public static void AddAuthHeader(UnityWebRequest req)
    {
        string token = PlayerPrefs.GetString(PrefKey, "");
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");
    }

    // Kiểm tra token hiện tại có tồn tại không.
    public static bool HasToken() => !string.IsNullOrEmpty(PlayerPrefs.GetString(PrefKey, ""));

    // Lưu token mới vào PlayerPrefs.
    public static void SaveToken(string jwt) => PlayerPrefs.SetString(PrefKey, jwt);

    // Xóa token (đăng xuất).
    public static void ClearToken() => PlayerPrefs.DeleteKey(PrefKey);
}
