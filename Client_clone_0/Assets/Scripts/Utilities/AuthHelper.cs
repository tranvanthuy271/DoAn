using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Helper tĩnh — gắn JWT header nhất quán cho mọi UnityWebRequest.
///
/// Cách dùng:
///   using var req = UnityWebRequest.Get(url);
///   AuthHelper.AddAuthHeader(req);
///   yield return req.SendWebRequest();
/// </summary>
public static class AuthHelper
{
    private const string PrefKey = "JWT_TOKEN";

    /// <summary>
    /// Đọc JWT từ PlayerPrefs và gắn Authorization header vào request.
    /// Không làm gì nếu token rỗng.
    /// </summary>
    public static void AddAuthHeader(UnityWebRequest req)
    {
        string token = PlayerPrefs.GetString(PrefKey, "");
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");
    }

    /// <summary>Kiểm tra token hiện tại có tồn tại không.</summary>
    public static bool HasToken() => !string.IsNullOrEmpty(PlayerPrefs.GetString(PrefKey, ""));

    /// <summary>Lưu token mới vào PlayerPrefs.</summary>
    public static void SaveToken(string jwt) => PlayerPrefs.SetString(PrefKey, jwt);

    /// <summary>Xóa token (đăng xuất).</summary>
    public static void ClearToken() => PlayerPrefs.DeleteKey(PrefKey);
}
