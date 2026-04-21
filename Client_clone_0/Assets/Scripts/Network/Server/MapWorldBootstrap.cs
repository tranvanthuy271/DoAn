using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Entry point duy nhất của toàn bộ game server.
/// 1 port — quản lý tất cả maps và zones như LangLa.
///
/// Command line args:
///   --port=7777        Port lắng nghe
///   --publicIp=x.x.x  IP public
///   --apiUrl=http://...
///
/// Gắn vào: "ServerBootstrap" GameObject trong ServerScene.
/// Dependencies: MapWorldConfig (assign inspector), ZoneRoomRegistry, ZoneConnectionApprovalV2
/// </summary>
[DisallowMultipleComponent]
public class MapWorldBootstrap : MonoBehaviour
{
    [Header("Config — assign MapWorldConfig asset")]
    [SerializeField] private MapWorldConfig _config;

    [Header("Network Managers Prefab")]
    [Tooltip("Prefab có NetworkObject + ZoneTransitionController + ZonePlayerSessionManager. Spawn ngay sau StartServer.")]
    [SerializeField] private GameObject _networkManagersPrefab;

    [Header("Retry")]
    [SerializeField] private float _apiRetryDelay = 3f;
    [SerializeField] private int   _maxApiRetries  = 5;

    // Runtime overrides from CLI
    private ushort _port;
    private string _publicIp;
    private string _apiBaseUrl;

    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError("[MapWorldBootstrap] MapWorldConfig chưa gán! Dừng khởi động.");
            enabled = false;
            return;
        }

        _config.ResolveFromGlobalConfig();
        _port      = _config.port;
        _publicIp  = _config.publicIp;
        _apiBaseUrl = _config.apiBaseUrl;
        ParseCliArgs();
    }

    private void Start()
    {
#if UNITY_SERVER || ZONE_SERVER || UNITY_EDITOR
        StartCoroutine(StartServerRoutine());
#else
        Debug.LogWarning("[MapWorldBootstrap] Không phải server build — disabled.");
        enabled = false;
#endif
    }

    private void ParseCliArgs()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            ReadArg(arg, "--port=",    v => { if (ushort.TryParse(v, out ushort p)) _port = p; });
            ReadArg(arg, "--publicIp=", v => _publicIp = v);
            ReadArg(arg, "--apiUrl=",  v => _apiBaseUrl = v);
        }
        Debug.Log($"[MapWorldBootstrap] Config → port={_port} publicIp={_publicIp} api={_apiBaseUrl}");
    }

    private static void ReadArg(string arg, string prefix, Action<string> setter)
    {
        if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            setter(arg[prefix.Length..]);
    }

    private string GetRuntimeMapBootstrapUrl()
    {
        string path = string.IsNullOrWhiteSpace(_config.runtimeMapBootstrapPath)
            ? "/map/runtime-bootstrap"
            : _config.runtimeMapBootstrapPath.Trim();

        if (!path.StartsWith("/", StringComparison.Ordinal))
            path = "/" + path;

        return $"{_apiBaseUrl.TrimEnd('/')}{path}";
    }

    private IEnumerator LoadRuntimeMapsFromApiIfEnabled()
    {
        if (_config == null || !_config.loadMapsFromApiOnBoot)
            yield break;

        if (string.IsNullOrWhiteSpace(_apiBaseUrl))
        {
            Debug.LogWarning("[MapWorldBootstrap] apiBaseUrl rỗng. Bỏ qua runtime map bootstrap.");
            yield break;
        }

        string url = GetRuntimeMapBootstrapUrl();
        for (int attempt = 1; attempt <= _maxApiRetries; attempt++)
        {
            using var req = UnityWebRequest.Get(url);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("X-Zone-Api-Key", _config.GetZoneApiKey());
            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                RuntimeMapBootstrapResponse response = null;
                try
                {
                    response = JsonUtility.FromJson<RuntimeMapBootstrapResponse>(req.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[MapWorldBootstrap] Không parse được runtime map bootstrap JSON: {ex.Message}");
                    break;
                }

                if (_config.ApplyRuntimeMapBootstrap(response))
                {
                    Debug.Log($"[MapWorldBootstrap] ✓ Loaded {_config.maps.Length} maps from API runtime bootstrap.");
                    yield break;
                }

                Debug.LogWarning("[MapWorldBootstrap] Runtime map bootstrap trả về 0 map hợp lệ. Giữ nguyên asset config.");
                yield break;
            }

            string error = string.IsNullOrWhiteSpace(req.downloadHandler?.text)
                ? req.error
                : req.downloadHandler.text;
            Debug.LogWarning($"[MapWorldBootstrap] Runtime map bootstrap thất bại ({attempt}/{_maxApiRetries}): {error}");

            if (attempt < _maxApiRetries)
                yield return new WaitForSeconds(_apiRetryDelay);
        }

        Debug.LogWarning($"[MapWorldBootstrap] Dùng fallback MapWorldConfig asset với {_config.maps.Length} maps.");
    }

    private void LogSceneAvailabilityWarnings()
    {
        foreach (var mapDef in _config.maps)
        {
            if (string.IsNullOrWhiteSpace(mapDef.sceneName))
            {
                Debug.LogWarning($"[MapWorldBootstrap] Map {mapDef.mapId} ({mapDef.mapName}) chưa có sceneName.");
                continue;
            }

            if (!Application.CanStreamedLevelBeLoaded(mapDef.sceneName))
            {
                Debug.LogWarning($"[MapWorldBootstrap] Scene '{mapDef.sceneName}' của map {mapDef.mapId} chưa có trong Build Settings hoặc chưa tồn tại. Client teleport vào map này sẽ fail.");
            }
        }
    }

    // ── Start server routine ──────────────────────────────────────────────────

    private IEnumerator StartServerRoutine()
    {
        yield return null; // 1 frame buffer

        // 0 — Nạp map runtime từ API/DB trước khi tạo ZoneRoomRegistry
        yield return StartCoroutine(LoadRuntimeMapsFromApiIfEnabled());
        LogSceneAvailabilityWarnings();

        // 1 — Initialize ZoneRoomRegistry (like LangLa Map.init())
        var registry = GetComponent<ZoneRoomRegistry>()
                    ?? gameObject.AddComponent<ZoneRoomRegistry>();
        registry.Initialize(_config);

        // 1b — Tạo per-map Physics2D scenes (isolation cross-map collision)
        //      PHẢI chạy trước bất kỳ enemy/NPC/player nào được spawn.
        var mapSceneMgr = GetComponent<MapSceneManager>()
                       ?? gameObject.AddComponent<MapSceneManager>();
        mapSceneMgr.Initialize(_config);

        // 2 — Configure transport
        var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[MapWorldBootstrap] UnityTransport không tìm thấy!");
            yield break;
        }

        transport.SetConnectionData(_publicIp, _port, _config.listenAddress);

        // 3 — DTLS encryption (bắt buộc khi production)
        if (_config.enableDtlsEncryption)
        {
            // UnityTransport hỗ trợ DTLS từ NGO 1.4+:
            //   transport.SetServerSecrets(serverCert, serverPrivateKey);
            //   transport.ConnectionData.IsSecure = true;
            // TODO: Thay bằng cert thực tế khi production.
            Debug.LogWarning("[MapWorldBootstrap] DTLS bật nhưng chưa có certificate. " +
                             "Đặt cert trong transport.SetServerSecrets() trước khi build production.");
        }

        // 4 — Setup Connection Approval
        var approval = GetComponent<ZoneConnectionApprovalV2>()
                    ?? gameObject.AddComponent<ZoneConnectionApprovalV2>();
        approval.Initialize(_config);

        // 5 — Start Server
        bool started = NetworkManager.Singleton.StartServer();
        if (!started)
        {
            Debug.LogError($"[MapWorldBootstrap] StartServer() thất bại (port={_port}). " +
                           "Kiểm tra port không bị chiếm, firewall, NetworkManager config.");
            yield break;
        }

        Debug.Log($"[MapWorldBootstrap] ✓ Server started — 1 port {_port} cho {_config.maps.Length} maps");

        // 6 — Spawn NetworkManagers (ZoneTransitionController + ZonePlayerSessionManager)
        if (_networkManagersPrefab != null)
        {
            if (_networkManagersPrefab.GetComponent<GameplayCommandService>() == null)
            {
                Debug.LogError("[MapWorldBootstrap] NetworkManagers prefab thiếu GameplayCommandService. " +
                               "Luồng item/skill/inventory ServerRpc sẽ không hoạt động đúng.");
            }

            var go = Instantiate(_networkManagersPrefab);
            go.GetComponent<Unity.Netcode.NetworkObject>()?.Spawn();
            Debug.Log("[MapWorldBootstrap] ✓ NetworkManagers spawned.");
        }
        else
        {
            Debug.LogWarning("[MapWorldBootstrap] _networkManagersPrefab chưa gán — ZoneTransitionController sẽ không nhận được RPC từ client!");
        }

        // 7 — Register với API (optional — để API biết server đang online)
        yield return StartCoroutine(RegisterServerWithApi());

        // 8 — Khởi động heartbeat
        var heartbeat = GetComponent<ZoneServerHeartbeat>()
                     ?? gameObject.AddComponent<ZoneServerHeartbeat>();
        heartbeat.Initialize(_config, _apiBaseUrl, _port);

        // 9 — Khởi tạo ItemTemplateManager trên dedicated server
        ItemTemplateManager.EnsureInstance();
    }

    private IEnumerator RegisterServerWithApi()
    {
        string apiKey = _config.GetZoneApiKey();
        string url = $"{_apiBaseUrl.TrimEnd('/')}/zone/server/register";
        string body = $"{{\"ip\":\"{EscapeJson(_publicIp)}\",\"port\":{_port}," +
                      $"\"mapCount\":{_config.maps.Length}}}";

        for (int attempt = 1; attempt <= _maxApiRetries; attempt++)
        {
            using var req = new UnityEngine.Networking.UnityWebRequest(url, "POST")
            {
                uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
                downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer()
            };
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-Zone-Api-Key", apiKey);
            yield return req.SendWebRequest();

            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("[MapWorldBootstrap] ✓ Đã đăng ký server với API.");
                yield break;
            }

            Debug.LogWarning($"[MapWorldBootstrap] API register thất bại ({attempt}/{_maxApiRetries}): " +
                             $"{req.error}. Retry sau {_apiRetryDelay}s...");
            yield return new WaitForSeconds(_apiRetryDelay);
        }

        Debug.LogWarning("[MapWorldBootstrap] Không đăng ký được với API — server vẫn hoạt động bình thường.");
    }

    private void OnApplicationQuit()
    {
        StartCoroutine(DeregisterServer());
    }

    private IEnumerator DeregisterServer()
    {
        string url = $"{_apiBaseUrl.TrimEnd('/')}/zone/server/deregister?port={_port}";
        using var req = UnityEngine.Networking.UnityWebRequest.Delete(url);
        req.SetRequestHeader("X-Zone-Api-Key", _config.GetZoneApiKey());
        yield return req.SendWebRequest();
    }

    private static string EscapeJson(string s) =>
        s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";
}
