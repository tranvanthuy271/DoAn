using UnityEngine;

// ScriptableObject chứa tất cả địa chỉ server: API URL, Game Server IP, Port.
// Dùng làm "1 nơi duy nhất" để config khi chuyển từ localhost sang VPS.
// Tạo asset: Assets → Create → DoAn → ServerAddressConfig
// Gán vào Inspector của các script, hoặc dùng ServerAddressConfig.Instance (auto-load từ Resources).
// Runtime override: đặt file server_config.json trong StreamingAssets (hoặc cùng thư mục build)
// để ghi đè mà không cần rebuild.
[CreateAssetMenu(fileName = "ServerAddressConfig", menuName = "DoAn/ServerAddressConfig")]
public class ServerAddressConfig : ScriptableObject
{
    private const string DefaultApiBasePlaceholder = "http://localhost:5000";
    private const string DefaultApiScheme = "http";
    private const int DefaultApiPort = 5000;

    // Singleton (auto-load từ Resources/ServerAddressConfig)
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
            }
            _instance.ApplyRuntimeOverrides();
            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCachedInstance()
    {
        _instance = null;
    }

    // API Server
    [Header("API Server (GameServerApi / REST)")]
    [Tooltip("URL gốc của GameServerApi, KHÔNG có /api ở cuối.\nVí dụ: http://localhost:5000 hoặc http://123.45.67.89:5000")]
    public string apiBaseUrl = "http://localhost:5000";

    // Game Server (Unity Netcode / UDP)
    [Header("Game Server (Unity Netcode for GameObjects)")]
    [Tooltip("IP mà CLIENT dùng để kết nối tới Game Server.\nLocalhost: 127.0.0.1 | VPS: IP public của VPS")]
    public string gameServerIp = "127.0.0.1";

    [Tooltip("Port UDP của Game Server. Mặc định 7777")]
    public ushort gameServerPort = 7777;

    // Derived helpers (read-only)

    // API base kèm /api. Ví dụ: http://localhost:5000/api
    public string ApiUrl => NormalizeApiUrl(apiBaseUrl);

    // API base KHÔNG có /api. Ví dụ: http://localhost:5000
    public string ApiRoot => NormalizeApiRoot(apiBaseUrl);

    public string ResolveApiRoot(string configuredValue)
    {
        return ShouldUseRuntimeApiOverride(configuredValue)
            ? ApiRoot
            : NormalizeApiRoot(configuredValue);
    }

    public string ResolveApiUrl(string configuredValue)
    {
        return ShouldUseRuntimeApiOverride(configuredValue)
            ? ApiUrl
            : NormalizeUrl(configuredValue);
    }

    // Runtime JSON override
    private bool _overridesApplied;
    private string _lastRuntimeConfigPath;
    private long _lastRuntimeConfigTicks = -1;

    // Đọc server_config.json từ StreamingAssets (hoặc cùng thư mục exe) và ghi đè giá trị.
    // Tự reload khi file đổi để Editor không giữ IP cũ khi tắt domain reload.
    public void ApplyRuntimeOverrides()
    {
        if (!ServerConfigFileReader.TryReadConfig(out string json, out string path, out long ticks))
            return;

        if (_overridesApplied &&
            ticks == _lastRuntimeConfigTicks &&
            string.Equals(path, _lastRuntimeConfigPath, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

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

            string host = NormalizeHost(overrides.serverHost);
            if (!string.IsNullOrEmpty(host))
            {
                string scheme = string.IsNullOrWhiteSpace(overrides.apiScheme)
                    ? DefaultApiScheme
                    : overrides.apiScheme.Trim().TrimEnd(':', '/');
                int apiPort = overrides.apiPort > 0 ? overrides.apiPort : DefaultApiPort;

                apiBaseUrl = BuildApiBaseUrl(scheme, host, apiPort);
                gameServerIp = host;
            }

            _overridesApplied = true;
            _lastRuntimeConfigPath = path;
            _lastRuntimeConfigTicks = ticks;

            Debug.Log($"[ServerAddressConfig] Runtime override applied ({path}) → API={apiBaseUrl} GameServer={gameServerIp}:{gameServerPort}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ServerAddressConfig] Parse server_config.json thất bại: {ex.Message}");
        }
    }

    private static string NormalizeApiRoot(string value)
    {
        string normalized = NormalizeUrl(value);
        if (normalized.EndsWith("/api", System.StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring(0, normalized.Length - 4);
        return normalized;
    }

    private static string NormalizeApiUrl(string value)
    {
        string root = NormalizeApiRoot(value);
        return string.IsNullOrEmpty(root) ? string.Empty : $"{root}/api";
    }

    private static string NormalizeUrl(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/');
    }

    private static string NormalizeHost(string value)
    {
        string host = NormalizeUrl(value);
        if (string.IsNullOrEmpty(host)) return string.Empty;

        int schemeIndex = host.IndexOf("://", System.StringComparison.Ordinal);
        if (schemeIndex >= 0)
            host = host.Substring(schemeIndex + 3);

        int slashIndex = host.IndexOf('/');
        if (slashIndex >= 0)
            host = host.Substring(0, slashIndex);

        int colonIndex = host.LastIndexOf(':');
        if (colonIndex > 0 && host.IndexOf(':') == colonIndex)
            host = host.Substring(0, colonIndex);

        return host.Trim();
    }

    private static string BuildApiBaseUrl(string scheme, string host, int apiPort)
    {
        if (string.IsNullOrWhiteSpace(host)) return string.Empty;

        string safeScheme = string.IsNullOrWhiteSpace(scheme)
            ? DefaultApiScheme
            : scheme.Trim().TrimEnd(':', '/');
        return $"{safeScheme}://{host}:{apiPort}";
    }

    private static bool ShouldUseRuntimeApiOverride(string configuredValue)
    {
        if (string.IsNullOrWhiteSpace(configuredValue)) return true;
        return NormalizeApiRoot(configuredValue)
            .Equals(DefaultApiBasePlaceholder, System.StringComparison.OrdinalIgnoreCase);
    }

    [System.Serializable]
    private class ServerAddressOverrides
    {
        public string serverHost;
        public string apiScheme;
        public int apiPort;
        public string apiBaseUrl;
        public string gameServerIp;
        public ushort gameServerPort;
    }
}
