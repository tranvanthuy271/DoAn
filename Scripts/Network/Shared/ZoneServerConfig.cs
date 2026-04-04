using UnityEngine;

/// <summary>
/// ScriptableObject chứa config của một Zone Server (1 zone = 1 server process).
/// Tạo: Assets → Create → DoAn → ZoneServerConfig
/// 
/// QUAN TRỌNG - Bảo mật:
///   jwtSecret KHÔNG được lưu trong file này khi build production.
///   Ưu tiên đọc từ environment variable JWT_SECRET (xem ZoneConnectionApproval.cs).
///   Field jwtSecret ở đây chỉ dùng cho Editor/dev-mode.
/// </summary>
[CreateAssetMenu(fileName = "ZoneServerConfig", menuName = "DoAn/ZoneServerConfig")]
public class ZoneServerConfig : ScriptableObject
{
    [Header("Zone Identity")]
    [Tooltip("ID của map mà zone này thuộc về (khớp với map_config.map_id trong DB)")]
    public int mapId;

    [Tooltip("Index của zone trong map (0-based). Phải khớp với ZoneTransitionTrigger.targetZoneId")]
    public int zoneId;

    [Tooltip("Tên hiển thị trong log và UI (ví dụ: Map1_Zone0_CanhDongLua)")]
    public string zoneName = "Zone_Unnamed";

    [Header("Network — Server")]
    [Tooltip("IP server lắng nghe. Luôn để 0.0.0.0 để nhận từ mọi interface")]
    public string listenAddress = "0.0.0.0";

    [Tooltip("Port server mở. Mỗi zone server phải có port riêng biệt")]
    public ushort port = 7777;

    [Tooltip("IP public mà client dùng để kết nối. Phải reachable từ client machine")]
    public string publicIp = "127.0.0.1";

    [Header("API")]
    [Tooltip("Base URL của GameServerApi (ASP.NET Core). KHÔNG có dấu / ở cuối")]
    public string apiBaseUrl = "http://localhost:5247/api";

    [Header("JWT — Dev Only")]
    [Tooltip("Secret dùng để validate JWT. KHÔNG hardcode khi production — đặt env var JWT_SECRET thay thế. Bỏ trống = đọc từ env var")]
    public string jwtSecretDevOnly = "";

    [Header("Scene")]
    [Tooltip("Tên scene Unity chứa zone này. Phải được đăng ký trong Build Settings")]
    public string sceneName = "GameScene";

    [Header("Entry Points")]
    [Tooltip("Mảng điểm spawn khi player đến từ zone khác. Index = entryPointId (dùng trong ZoneTransitionTrigger)")]
    public Vector2[] entryPoints = { Vector2.zero };

    [Header("Capacity")]
    [Tooltip("Số player tối đa trong zone này (0 = không giới hạn)")]
    public int maxPlayers = 50;

    /// <summary>
    /// Trả về JWT secret. Ưu tiên: env var > jwtSecretDevOnly field.
    /// </summary>
    public string GetJwtSecret()
    {
        string envSecret = System.Environment.GetEnvironmentVariable("JWT_SECRET");
        if (!string.IsNullOrEmpty(envSecret))
            return envSecret;

        if (!string.IsNullOrEmpty(jwtSecretDevOnly))
            return jwtSecretDevOnly;

        throw new System.InvalidOperationException(
            "[ZoneServerConfig] JWT_SECRET chưa được cấu hình. " +
            "Đặt environment variable JWT_SECRET hoặc điền jwtSecretDevOnly (chỉ dùng khi dev).");
    }
}
