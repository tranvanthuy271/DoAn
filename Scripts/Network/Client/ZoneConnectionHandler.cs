using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

// Client-side: quản lý toàn bộ vòng đời kết nối đến zone server.
// Trách nhiệm:
// - Kết nối lần đầu (sau Login)
// - Nhận lệnh zone transfer từ ZoneTransitionManager (qua ClientRpc)
// - Ngắt kết nối zone cũ → kết nối zone mới (Shutdown → StartClient)
// Gắn vào: persistent GameObject "NetworkClient" (DontDestroyOnLoad từ LoginScene).
[DisallowMultipleComponent]
public class ZoneConnectionHandler : MonoBehaviour
{
    public static ZoneConnectionHandler Instance { get; private set; }

    [Header("Connection")]
    [Tooltip("Số giây timeout khi kết nối tới zone server")]
    [SerializeField] private float _connectTimeout = 10f;

    // Trạng thái đang transfer (ngăn double-trigger)
    private bool _isTransferring;

    // Thông tin zone kế tiếp (dùng sau khi scene load xong)
    private PendingZoneTransfer _pendingTransfer;

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Public: Initial Connect

    // Kết nối ban đầu sau Login. Gọi từ MainSceneNetworkInitializer.
    // Tham số ip: IP của zone server đầu tiên (spawn zone)
    // Tham số port: Port của zone server đầu tiên
    // Tham số jwt: JWT token từ PlayerPrefs
    // Tham số entryPointId: Entry point index (0 = default)
    public void ConnectToZone(string ip, ushort port, string jwt, int entryPointId = 0)
    {
        if (_isTransferring)
        {
            Debug.LogWarning("[ZoneConnectionHandler] Đang trong quá trình transfer — bỏ qua lệnh connect.");
            return;
        }
        StartCoroutine(ConnectRoutine(ip, port, jwt, entryPointId, sceneName: null));
    }

    // Called by ZoneTransitionManager via ClientRpc

    // Gọi bởi ZoneTransitionManager.BeginZoneTransferClientRpc().
    public void HandleZoneTransfer(string newIp, ushort newPort, int entryPointId, string targetSceneName)
    {
        if (_isTransferring)
        {
            Debug.LogWarning("[ZoneConnectionHandler] Zone transfer đang xử lý, bỏ lệnh transfer mới.");
            return;
        }

        Debug.Log($"[ZoneConnectionHandler] Bắt đầu transfer → {newIp}:{newPort} scene={targetSceneName}");
        StartCoroutine(TransferRoutine(newIp, newPort, entryPointId, targetSceneName));
    }

    // Internal Routines

    private IEnumerator TransferRoutine(string newIp, ushort newPort, int entryPointId, string targetSceneName)
    {
        _isTransferring = true;

        // 1 — Hiển thị loading UI (nếu có)
        ZoneTransitionUI.Instance?.Show("Đang chuyển vùng...");

        // 2 — Shutdown kết nối hiện tại
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            // Đợi NGO shutdown hoàn tất (tránh race condition)
            yield return new WaitUntil(() => !NetworkManager.Singleton.IsClient &&
                                             !NetworkManager.Singleton.IsServer);
            yield return null; // thêm 1 frame buffer
        }

        // 3 — Load scene mới nếu khác scene hiện tại
        if (!string.IsNullOrEmpty(targetSceneName) &&
            SceneManager.GetActiveScene().name != targetSceneName)
        {
            var loadOp = SceneManager.LoadSceneAsync(targetSceneName);
            yield return new WaitUntil(() => loadOp.isDone);
            yield return null;
        }

        // 4 — Lấy JWT từ PlayerPrefs
        string jwt = UnityEngine.PlayerPrefs.GetString("JWT_TOKEN", "");
        if (string.IsNullOrEmpty(jwt))
        {
            Debug.LogError("[ZoneConnectionHandler] JWT_TOKEN rỗng — không thể kết nối zone mới!");
            _isTransferring = false;
            ZoneTransitionUI.Instance?.Hide();
            // TODO: chuyển về màn login
            yield break;
        }

        // 5 — Kết nối zone mới
        yield return StartCoroutine(ConnectRoutine(newIp, newPort, jwt, entryPointId, targetSceneName));
    }

    private IEnumerator ConnectRoutine(string ip, ushort port, string jwt, int entryPointId, string sceneName)
    {
        _isTransferring = true;

        // Cấu hình transport
        var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[ZoneConnectionHandler] UnityTransport không tìm thấy!");
            _isTransferring = false;
            yield break;
        }

        transport.SetConnectionData(ip, port);

        // Payload gửi lên server: JSON chứa JWT + entryPointId
        string payload = $"{{\"token\":\"{EscapeJson(jwt)}\",\"entryPointId\":{entryPointId}}}";
        NetworkManager.Singleton.NetworkConfig.ConnectionData =
            Encoding.UTF8.GetBytes(payload);

        // Subscribe event kết nối
        bool connected = false;
        bool failed    = false;

        void OnConnected(ulong _)   { connected = true; }
        void OnDisconnected(ulong _){ failed    = true; }

        NetworkManager.Singleton.OnClientConnectedCallback    += OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   += OnDisconnected;

        // StartClient
        bool started = NetworkManager.Singleton.StartClient();
        if (!started)
        {
            Debug.LogError($"[ZoneConnectionHandler] StartClient() thất bại ({ip}:{port}).");
            NetworkManager.Singleton.OnClientConnectedCallback    -= OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback   -= OnDisconnected;
            _isTransferring = false;
            ZoneTransitionUI.Instance?.Hide();
            yield break;
        }

        // Timeout
        float elapsed = 0f;
        while (!connected && !failed)
        {
            elapsed += Time.deltaTime;
            if (elapsed > _connectTimeout)
            {
                NetworkManager.Singleton.Shutdown();
                failed = true;
                break;
            }
            yield return null;
        }

        NetworkManager.Singleton.OnClientConnectedCallback    -= OnConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback   -= OnDisconnected;

        if (failed)
        {
            Debug.LogError($"[ZoneConnectionHandler] Kết nối tới {ip}:{port} thất bại/timeout.");
            // TODO: hiện thông báo lỗi, retry hoặc về main menu
        }
        else
        {
            Debug.Log($"[ZoneConnectionHandler] ✓ Kết nối thành công → {ip}:{port}");
        }

        _isTransferring = false;
        ZoneTransitionUI.Instance?.Hide();
    }

    private static string EscapeJson(string s) =>
        s?.Replace("\\", "\\\\").Replace("\"", "\\\"") ?? "";

    // Inner type

    private struct PendingZoneTransfer
    {
        public string Ip;
        public ushort Port;
        public int    EntryPointId;
        public string SceneName;
    }
}

// Stub cho Loading UI — tạo MonoBehaviour thật trong dự án và implement interface này.
public class ZoneTransitionUI : MonoBehaviour
{
    public static ZoneTransitionUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public virtual void Show(string message) { }
    public virtual void Hide() { }
}
