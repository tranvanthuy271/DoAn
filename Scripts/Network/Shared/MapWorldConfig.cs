using System;
using UnityEngine;

/// <summary>
/// ScriptableObject duy nhất chứa toàn bộ config maps + zones + server network.
/// Thay thế nhiều ZoneServerConfig riêng lẻ — học từ LangLa Map[] maps pattern.
///
/// Tạo: Assets → Create → DoAn → MapWorldConfig
/// Chỉ cần 1 asset cho toàn bộ game.
/// </summary>
[CreateAssetMenu(fileName = "MapWorldConfig", menuName = "DoAn/MapWorldConfig")]
public class MapWorldConfig : ScriptableObject
{
    [Header("Server Network — 1 port cho tất cả maps/zones")]
    [Tooltip("IP server lắng nghe. Luôn 0.0.0.0 để nhận từ mọi interface")]
    public string listenAddress = "0.0.0.0";

    [Tooltip("Port duy nhất. Tất cả client kết nối vào đây, bất kể đang ở map nào")]
    public ushort port = 7777;

    [Tooltip("Base URL của GameServerApi. KHÔNG có dấu / ở cuối")]
    public string apiBaseUrl = "http://localhost:5247/api";

    [Tooltip("IP public mà client dùng để kết nối")]
    public string publicIp = "127.0.0.1";

    [Header("Security")]
    [Tooltip("Bật DTLS encryption cho NGO UDP transport. BẮT BUỘC trong production.")]
    public bool enableDtlsEncryption = false;

    [Tooltip("JWT secret — DEV ONLY. Để trống khi production, dùng env var JWT_SECRET")]
    public string jwtSecretDevOnly = "";

    [Tooltip("Service API key — DEV ONLY. Để trống khi production, dùng env var ZONE_API_KEY")]
    public string zoneApiKeyDevOnly = "";

    [Header("Heartbeat")]
    [Tooltip("Interval gửi heartbeat lên API (giây). API đánh dấu offline sau 2x interval")]
    public float heartbeatInterval = 30f;

    [Header("Maps & Zones")]
    [Tooltip("Danh sách toàn bộ map và zones trong game, giống Map[] maps của LangLa")]
    public MapDefinition[] maps = Array.Empty<MapDefinition>();

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>JWT secret từ env var → fallback dev field.</summary>
    public string GetJwtSecret()
    {
        string v = System.Environment.GetEnvironmentVariable("JWT_SECRET");
        if (!string.IsNullOrEmpty(v)) return v;
        if (!string.IsNullOrEmpty(jwtSecretDevOnly)) return jwtSecretDevOnly;
        throw new InvalidOperationException(
            "[MapWorldConfig] JWT_SECRET chưa cấu hình. " +
            "Đặt env var JWT_SECRET hoặc điền jwtSecretDevOnly (chỉ dev).");
    }

    /// <summary>API key từ env var → fallback dev field.</summary>
    public string GetZoneApiKey()
    {
        string v = System.Environment.GetEnvironmentVariable("ZONE_API_KEY");
        if (!string.IsNullOrEmpty(v)) return v;
        return string.IsNullOrEmpty(zoneApiKeyDevOnly) ? "dev-key" : zoneApiKeyDevOnly;
    }

    /// <summary>Tìm MapDefinition theo mapId.</summary>
    public MapDefinition GetMap(int mapId)
    {
        foreach (var m in maps)
            if (m.mapId == mapId) return m;
        return null;
    }

    /// <summary>Tìm ZoneDefinition theo mapId + zoneId.</summary>
    public ZoneDefinition GetZone(int mapId, int zoneId)
    {
        var map = GetMap(mapId);
        if (map == null) return null;
        foreach (var z in map.zones)
            if (z.zoneId == zoneId) return z;
        return null;
    }
}

// ── Data classes ──────────────────────────────────────────────────────────────

/// <summary>
/// Định nghĩa 1 map (tương đương MapTemplate + Map trong LangLa).
/// </summary>
[Serializable]
public class MapDefinition
{
    [Tooltip("map_id trong DB, khớp với map_config.map_id")]
    public int mapId;

    [Tooltip("Tên map hiển thị trong log (ví dụ: Lang_KhoiDau)")]
    public string mapName = "Map_Unnamed";

    [Tooltip("Tên scene Unity. Phải có trong Build Settings")]
    public string sceneName = "GameScene";

    [Tooltip("Danh sách zones trong map này. Tối thiểu 1 zone")]
    public ZoneDefinition[] zones = { new ZoneDefinition() };
}

/// <summary>
/// Định nghĩa 1 zone trong map (tương đương Zone trong LangLa).
/// Zone là logical room — không phải separate process hay port.
/// </summary>
[Serializable]
public class ZoneDefinition
{
    [Tooltip("Zone index 0-based trong map")]
    public int zoneId;

    [Tooltip("Tên zone trong log")]
    public string zoneName = "Zone_0";

    [Tooltip("Số player tối đa. 0 = không giới hạn")]
    public int maxPlayers = 50;

    [Tooltip("Danh sách entry points. Index = entryPointId dùng trong ZoneTransitionTrigger")]
    public Vector2[] entryPoints = { Vector2.zero };

    /// <summary>Key dùng để nhận diện zone duy nhất, giống roomId trong LangLa.</summary>
    public string GetZoneKey(int mapId) => $"map{mapId}_zone{zoneId}";
}
