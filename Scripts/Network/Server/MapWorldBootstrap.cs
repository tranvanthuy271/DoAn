using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

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

    // ── Start server routine ──────────────────────────────────────────────────

    private IEnumerator StartServerRoutine()
    {
        yield return null; // 1 frame buffer

        // 1 — Initialize ZoneRoomRegistry (like LangLa Map.init())
        var registry = GetComponent<ZoneRoomRegistry>()
                    ?? gameObject.AddComponent<ZoneRoomRegistry>();
        registry.Initialize(_config);

        // 2 — Configure transport
        var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[MapWorldBootstrap] UnityTransport không tìm thấy!");
            yield break;
        }

        transport.SetConnectionData(_config.listenAddress, _port, _publicIp);

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

        // 6 — Register với API (optional — để API biết server đang online)
        yield return StartCoroutine(RegisterServerWithApi());

        // 7 — Khởi động heartbeat
        var heartbeat = GetComponent<ZoneServerHeartbeat>()
                     ?? gameObject.AddComponent<ZoneServerHeartbeat>();
        heartbeat.Initialize(_config, _apiBaseUrl, _port);
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
