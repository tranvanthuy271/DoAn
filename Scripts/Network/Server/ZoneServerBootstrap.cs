using System;
using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

// Entry point của một Zone Server headless process.
// Cách chạy (command line):
// GameServer.exe -batchmode -nographics --mapId=1 --zoneId=0 --port=7770 --publicIp=192.168.1.100
// Trong Unity Editor (testing):
// Đặt UNITY_SERVER define symbol HOẶC tick "Server Build" trong Build Settings.
// Gắn script này vào GameObject "ZoneServerBootstrap" trong ServerScene.
// Assign ZoneServerConfig ScriptableObject.
// Dependencies: ZoneServerConfig, ZoneServerRegistrar, ZoneConnectionApproval, ZonePlayerSessionManager
[DisallowMultipleComponent]
public class ZoneServerBootstrap : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ZoneServerConfig _config;

    [Header("Retry")]
    [SerializeField] private float _registrationRetryDelay = 3f;
    [SerializeField] private int _maxRegistrationRetries = 10;

    // Runtime override từ command-line args
    private int _mapId;
    private int _zoneId;
    private ushort _port;
    private string _publicIp;
    private string _apiBaseUrl;

    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError("[ZoneServerBootstrap] Chưa gán ZoneServerConfig! Hủy khởi động.");
            enabled = false;
            return;
        }

        // Áp dụng defaults từ config, sau đó override bằng CLI args
        _mapId      = _config.mapId;
        _zoneId     = _config.zoneId;
        _port       = _config.port;
        _publicIp   = _config.publicIp;
        _apiBaseUrl = _config.apiBaseUrl;

        ParseCommandLineArgs();
    }

    private void Start()
    {
        // Chỉ chạy trong server build hoặc khi có define symbol ZONE_SERVER
#if UNITY_SERVER || ZONE_SERVER || UNITY_EDITOR
        StartCoroutine(StartServerRoutine());
#else
        Debug.LogWarning("[ZoneServerBootstrap] Không phải server build — script bị vô hiệu hóa.");
        enabled = false;
#endif
    }

    private void ParseCommandLineArgs()
    {
        string[] args = Environment.GetCommandLineArgs();
        foreach (string arg in args)
        {
            ReadArg(arg, "--mapId=",    v => { if (int.TryParse(v, out int i)) _mapId = i; });
            ReadArg(arg, "--zoneId=",   v => { if (int.TryParse(v, out int i)) _zoneId = i; });
            ReadArg(arg, "--port=",     v => { if (ushort.TryParse(v, out ushort p)) _port = p; });
            ReadArg(arg, "--publicIp=", v => _publicIp = v);
            ReadArg(arg, "--apiUrl=",   v => _apiBaseUrl = v);
        }

        Debug.Log($"[ZoneServerBootstrap] Config => map={_mapId} zone={_zoneId} " +
                  $"port={_port} publicIp={_publicIp} api={_apiBaseUrl}");
    }

    private static void ReadArg(string arg, string prefix, Action<string> setter)
    {
        if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            setter(arg.Substring(prefix.Length));
    }

    private IEnumerator StartServerRoutine()
    {
        // Đợi 1 frame để các Awake khác hoàn tất
        yield return null;

        // 1 — Configure transport
        var transport = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.GetComponent<UnityTransport>()
            : null;

        if (transport == null)
        {
            Debug.LogError("[ZoneServerBootstrap] UnityTransport không tìm thấy trên NetworkManager!");
            yield break;
        }

        transport.SetConnectionData(_config.listenAddress, _port);

        // 2 — Khởi tạo connection approval trước khi start server
        var approval = GetComponent<ZoneConnectionApproval>()
                    ?? gameObject.AddComponent<ZoneConnectionApproval>();
        approval.Initialize(_config);

        // 3 — Start server
        bool started = NetworkManager.Singleton.StartServer();
        if (!started)
        {
            Debug.LogError($"[ZoneServerBootstrap] Không thể StartServer() trên port {_port}. " +
                           "Kiểm tra port có đang bị dùng không.");
            yield break;
        }

        Debug.Log($"[ZoneServerBootstrap] ✓ Server started — " +
                  $"map={_mapId} zone={_zoneId} port={_port}");

        // 4 — Register zone server với API
        yield return StartCoroutine(RegisterWithApiRoutine());

        // 5 — Load scene tương ứng (chỉ khi scene hiện tại chưa phải scene của zone này)
        if (!string.IsNullOrEmpty(_config.sceneName) &&
            SceneManager.GetActiveScene().name != _config.sceneName)
        {
            // NetworkManager.SceneManager quản lý scene cho server-authoritative scene loading
            NetworkManager.Singleton.SceneManager.LoadScene(
                _config.sceneName,
                UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
    }

    private IEnumerator RegisterWithApiRoutine()
    {
        var registrar = GetComponent<ZoneServerRegistrar>()
                     ?? gameObject.AddComponent<ZoneServerRegistrar>();
        registrar.Initialize(_apiBaseUrl);

        int retries = 0;
        while (retries < _maxRegistrationRetries)
        {
            bool success = false;
            yield return StartCoroutine(
                registrar.Register(_mapId, _zoneId, _publicIp, _port,
                                   result => success = result));

            if (success)
            {
                Debug.Log($"[ZoneServerBootstrap] ✓ Đã đăng ký zone server với API.");
                yield break;
            }

            retries++;
            Debug.LogWarning($"[ZoneServerBootstrap] Đăng ký API thất bại " +
                             $"(lần {retries}/{_maxRegistrationRetries}). " +
                             $"Thử lại sau {_registrationRetryDelay}s...");
            yield return new WaitForSeconds(_registrationRetryDelay);
        }

        Debug.LogError("[ZoneServerBootstrap] Không thể đăng ký với API sau tất cả retry. " +
                       "Kiểm tra API server đang chạy và apiBaseUrl đúng.");
    }

    private void OnApplicationQuit()
    {
        // Hủy đăng ký khi server tắt để clients không kết nối vào server đã chết
        var registrar = GetComponent<ZoneServerRegistrar>();
        if (registrar != null)
            StartCoroutine(registrar.Deregister(_mapId, _zoneId));
    }
}
