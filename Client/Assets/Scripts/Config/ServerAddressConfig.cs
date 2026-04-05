using UnityEngine;

/// <summary>
/// ScriptableObject chứa tất cả địa chỉ server: API URL, Game Server IP, Port.
/// Dùng làm "1 nơi duy nhất" để config khi chuyển từ localhost sang VPS.
///
/// Tạo asset: Assets → Create → DoAn → ServerAddressConfig
/// Gán vào Inspector của các script, hoặc dùng ServerAddressConfig.Instance (auto-load từ Resources).
///
/// Runtime override: đặt file server_config.json trong StreamingAssets (hoặc cùng thư mục build)
/// để ghi đè mà không cần rebuild.
/// </summary>
[CreateAssetMenu(fileName = "ServerAddressConfig", menuName = "DoAn/ServerAddressConfig")]
public class ServerAddressConfig : ScriptableObject
{
    // ── Singleton (auto-load từ Resources/ServerAddressConfig) ─────────────────
    private static ServerAddressConfig _instance;
    public static ServerAddressConfig Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<ServerAddressConfig>("ServerAddressConfig");
                if (_instance == null)
                {
                    Debug.LogWarning("[ServerAddressConfig] Không tìm thấy asset trong Resources/ServerAddressConfig. Tạo default runtime.");
                    _instance = CreateInstance<ServerAddressConfig>();
                }
                _instance.ApplyRuntimeOverrides();
            }
            return _instance;
        }
    }

    // ── API Server ────────────────────────────────────────────────────────────
    [Header("API Server (GameServerApi / REST)")]
    [Tooltip("URL gốc của GameServerApi, KHÔNG có /api ở cuối.\nVí dụ: http://localhost:5000 hoặc http://123.45.67.89:5000")]
    public string apiBaseUrl = "http://localhost:5000";

    // ── Game Server (Unity Netcode / UDP) ─────────────────────────────────────
    [Header("Game Server (Unity Netcode for GameObjects)")]
    [Tooltip("IP mà CLIENT dùng để kết nối tới Game Server.\nLocalhost: 127.0.0.1 | VPS: IP public của VPS")]
    public string gameServerIp = "127.0.0.1";

    [Tooltip("Port UDP của Game Server. Mặc định 7777")]
    public ushort gameServerPort = 7777;

    // ── Derived helpers (read-only) ───────────────────────────────────────────

    /// <summary>API base kèm /api. Ví dụ: http://localhost:5000/api</summary>
    public string ApiUrl => $"{apiBaseUrl.TrimEnd('/')}/api";

    /// <summary>API base KHÔNG có /api. Ví dụ: http://localhost:5000</summary>
    public string ApiRoot => apiBaseUrl.TrimEnd('/');

    // ── Runtime JSON override ─────────────────────────────────────────────────
    private bool _overridesApplied;

    /// <summary>
    /// Đọc server_config.json từ StreamingAssets (hoặc cùng thư mục exe) và ghi đè giá trị.
    /// Gọi tự động lần đầu khi truy cập Instance.
    /// </summary>
    public void ApplyRuntimeOverrides()
    {
        if (_overridesApplied) return;
        _overridesApplied = true;

        string json = ServerConfigFileReader.ReadConfigJson();
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var overrides = JsonUtility.FromJson<ServerAddressOverrides>(json);
            if (overrides == null) return;

            if (!string.IsNullOrEmpty(overrides.apiBaseUrl))
                apiBaseUrl = overrides.apiBaseUrl;
            if (!string.IsNullOrEmpty(overrides.gameServerIp))
                gameServerIp = overrides.gameServerIp;
            if (overrides.gameServerPort > 0)
                gameServerPort = overrides.gameServerPort;

            Debug.Log($"[ServerAddressConfig] Runtime override applied → API={apiBaseUrl} GameServer={gameServerIp}:{gameServerPort}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ServerAddressConfig] Parse server_config.json thất bại: {ex.Message}");
        }
    }

    [System.Serializable]
    private class ServerAddressOverrides
    {
        public string apiBaseUrl;
        public string gameServerIp;
        public ushort gameServerPort;
    }
}
