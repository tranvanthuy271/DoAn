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
    private const int DefaultSharedZoneCount = 15;
    private const int DefaultSharedZoneMaxPlayers = 50;
    private const int DefaultInstanceZoneMaxPlayers = 8;

    [Header("Server Network — 1 port cho tất cả maps/zones")]
    [Tooltip("IP server lắng nghe. Luôn 0.0.0.0 để nhận từ mọi interface")]
    public string listenAddress = "0.0.0.0";

    [Tooltip("Port duy nhất. Tất cả client kết nối vào đây, bất kể đang ở map nào")]
    public ushort port = 7777;

    [Tooltip("Base URL của GameServerApi. KHÔNG có dấu / ở cuối")]
    public string apiBaseUrl = "http://localhost:5000/api";

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

    [Header("Zone Defaults")]
    [Tooltip("Số zone mặc định cho map thường nếu từng map không override. Giống Map.NUM_ZONE của LangLa.")]
    public int sharedMapDefaultZoneCount = DefaultSharedZoneCount;

    [Tooltip("Số player tối đa cho mỗi zone thường nếu từng map không override.")]
    public int sharedMapMaxPlayers = DefaultSharedZoneMaxPlayers;

    [Tooltip("Số player tối đa cho mỗi zone riêng/phó bản nếu từng map không override.")]
    public int instanceMapMaxPlayers = DefaultInstanceZoneMaxPlayers;

    [Tooltip("Map fallback nếu player đang lưu ở zone không còn tồn tại hoặc zone riêng đã hết hạn.")]
    public int fallbackMapId = 0;

    [Tooltip("Zone fallback trong map fallback.")]
    public int fallbackZoneId = 0;

    [Header("Maps & Zone Policy")]
    [Tooltip("Danh sách toàn bộ map. Map thường tự sinh zone mặc định khi server start; map phó bản chỉ tạo zone riêng khi cần.")]
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

    public int GetSharedZoneCountOrDefault() =>
        sharedMapDefaultZoneCount > 0 ? sharedMapDefaultZoneCount : DefaultSharedZoneCount;

    public int GetSharedZoneMaxPlayersOrDefault() =>
        sharedMapMaxPlayers > 0 ? sharedMapMaxPlayers : DefaultSharedZoneMaxPlayers;

    public int GetInstanceZoneMaxPlayersOrDefault() =>
        instanceMapMaxPlayers > 0 ? instanceMapMaxPlayers : DefaultInstanceZoneMaxPlayers;

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

        if (zoneId < 0)
            return map.SupportsCustomZones ? map.CreateCustomZone(this, zoneId) : null;

        if (!map.UsesPublicZones(this))
            return null;

        int publicZoneCount = map.GetPublicZoneCount(this);
        if (zoneId >= publicZoneCount)
            return null;

        return map.CreatePublicZone(this, zoneId);
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

    [Tooltip("SharedPublic = map thường tự sinh zone mặc định. InstanceOnly = chỉ cho zone riêng/phó bản.")]
    public MapZoneTopology zoneTopology = MapZoneTopology.SharedPublic;

    [Tooltip("Bật nếu map này vẫn cần zone riêng runtime ngoài các zone thường.")]
    public bool allowCustomZones = false;

    [Tooltip("Override số zone thường của map. 0 = dùng sharedMapDefaultZoneCount.")]
    public int publicZoneCountOverride = 0;

    [Tooltip("Override max player cho zone thường. 0 = dùng sharedMapMaxPlayers.")]
    public int publicZoneMaxPlayersOverride = 0;

    [Tooltip("Override max player cho zone riêng/phó bản. 0 = dùng instanceMapMaxPlayers.")]
    public int customZoneMaxPlayersOverride = 0;

    [Tooltip("Cho phép player tự đổi khu trong map này. Map phó bản thường để false.")]
    public bool allowPlayerZoneSwitch = true;

    [Tooltip("Danh sách entry point của map. entryPointId dùng khi teleport/chuyển khu.")]
    public Vector2[] entryPoints = { Vector2.zero };

    public int GetPublicZoneCount(MapWorldConfig config)
    {
        if (zoneTopology != MapZoneTopology.SharedPublic)
            return 0;

        return publicZoneCountOverride > 0
            ? publicZoneCountOverride
            : config.GetSharedZoneCountOrDefault();
    }

    public int GetPublicZoneMaxPlayers(MapWorldConfig config) =>
        publicZoneMaxPlayersOverride > 0
            ? publicZoneMaxPlayersOverride
            : config.GetSharedZoneMaxPlayersOrDefault();

    public int GetCustomZoneMaxPlayers(MapWorldConfig config) =>
        customZoneMaxPlayersOverride > 0
            ? customZoneMaxPlayersOverride
            : config.GetInstanceZoneMaxPlayersOrDefault();

    public bool UsesPublicZones(MapWorldConfig config) => GetPublicZoneCount(config) > 0;

    public bool SupportsCustomZones => zoneTopology == MapZoneTopology.InstanceOnly || allowCustomZones;

    public bool CanPlayerChangePublicZone(MapWorldConfig config) =>
        UsesPublicZones(config) && allowPlayerZoneSwitch;

    public ZoneDefinition CreatePublicZone(MapWorldConfig config, int zoneId)
    {
        return new ZoneDefinition
        {
            zoneId = zoneId,
            zoneName = $"{mapName}_Zone_{zoneId}",
            maxPlayers = GetPublicZoneMaxPlayers(config),
            entryPoints = GetEntryPointsOrDefault(),
            isCustom = false
        };
    }

    public ZoneDefinition CreateCustomZone(
        MapWorldConfig config,
        int zoneId,
        string customZoneName = null,
        int? maxPlayersOverride = null)
    {
        return new ZoneDefinition
        {
            zoneId = zoneId,
            zoneName = string.IsNullOrWhiteSpace(customZoneName)
                ? $"{mapName}_Instance_{Mathf.Abs(zoneId)}"
                : customZoneName,
            maxPlayers = maxPlayersOverride ?? GetCustomZoneMaxPlayers(config),
            entryPoints = GetEntryPointsOrDefault(),
            isCustom = true
        };
    }

    private Vector2[] GetEntryPointsOrDefault() =>
        entryPoints != null && entryPoints.Length > 0 ? entryPoints : new[] { Vector2.zero };
}

public enum MapZoneTopology
{
    SharedPublic = 0,
    InstanceOnly = 1
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

    [Tooltip("Danh sách entry points. Index = entryPointId dùng trong ZoneTransitionController")]
    public Vector2[] entryPoints = { Vector2.zero };

    [Tooltip("True nếu đây là zone riêng runtime (phó bản / party / solo room).")]
    public bool isCustom = false;

    /// <summary>Key dùng để nhận diện zone duy nhất, giống roomId trong LangLa.</summary>
    public string GetZoneKey(int mapId) => $"map{mapId}_zone{zoneId}";
}
