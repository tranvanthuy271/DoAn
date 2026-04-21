using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton service — hiện ErrorNotifyPanel từ bất kỳ đâu trong game.
///
/// Tính năng:
///  - Tự tạo instance khi gọi lần đầu (DontDestroyOnLoad)
///  - Lắng nghe Netcode disconnect / transport failure → tự động hiện panel
///  - Dùng LoginLoadingManager để render đúng ErrorNotifyPanel prefab đã config
///
/// Cách gọi từ bất kỳ script nào:
///   GameErrorNotifier.Show("Tin nhắn lỗi tuỳ ý");
///   GameErrorNotifier.Show(GameErrorNotifier.ErrorType.ConnectionLost);
///   GameErrorNotifier.Show(GameErrorNotifier.ErrorType.ServerMaintenance);
/// </summary>
public class GameErrorNotifier : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────

    public static GameErrorNotifier Instance { get; private set; }

    // ── Error types ───────────────────────────────────────────────────────

    public enum ErrorType
    {
        /// <summary>Mất kết nối / socket fail</summary>
        ConnectionLost,
        /// <summary>Không thể reach server (connection refused / timeout)</summary>
        CannotConnect,
        /// <summary>Server đang bảo trì</summary>
        ServerMaintenance,
        /// <summary>Phiên đăng nhập hết hạn</summary>
        SessionExpired,
        /// <summary>Lỗi không xác định</summary>
        Unknown,
    }

    // ── Config ────────────────────────────────────────────────────────────

    [Header("Tự động lắng nghe Netcode disconnect")]
    [Tooltip("Bật để GameErrorNotifier tự detect mất kết nối game server và hiện panel")]
    public bool autoDetectNetworkDisconnect = true;

    [Header("Scene cần quay về khi bấm Xác nhận (để trống = quay Login)")]
    public string fallbackScene = "Login";

    [Header("Timeout khi đang kết nối game server")]
    [Min(1f)]
    public float connectionTimeoutSeconds = 8f;

    // ── State ─────────────────────────────────────────────────────────────

    private NetworkManager _subscribedNetworkManager;
    private Coroutine _connectionWatchRoutine;
    private System.Action _pendingDismissAction;
    private bool _isWatchingConnection;
    private bool _localClientConnected;
    private bool _shown; // tránh hiện nhiều lần liên tiếp

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (autoDetectNetworkDisconnect)
            TrySubscribeNetcode();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (autoDetectNetworkDisconnect)
            TrySubscribeNetcode();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CancelConnectionWatchInternal();
        UnsubscribeNetcode();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _shown = false;
        _localClientConnected = NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;

        if (autoDetectNetworkDisconnect)
            TrySubscribeNetcode();
    }

    // ── Public static API ─────────────────────────────────────────────────

    public static void EnsureReady()
    {
        EnsureInstance();
        Instance.TrySubscribeNetcode();
    }

    /// <summary>Hiện panel với lỗi loại định sẵn.</summary>
    public static void Show(ErrorType type, System.Action onDismiss = null)
    {
        EnsureReady();
        Instance.ShowInternal(MessageForType(type), onDismiss);
    }

    /// <summary>Hiện panel với chuỗi lỗi tuỳ ý (raw từ server hoặc exception).</summary>
    public static void Show(string rawMessage, System.Action onDismiss = null)
    {
        EnsureReady();
        Instance.ShowInternal(rawMessage, onDismiss);
    }

    /// <summary>Bắt đầu theo dõi một lần connect client để hiện panel nếu timeout / fail.</summary>
    public static void WatchClientConnection(float timeoutSeconds = -1f, System.Action onDismiss = null)
    {
        EnsureReady();
        float effectiveTimeout = timeoutSeconds > 0f ? timeoutSeconds : Instance.connectionTimeoutSeconds;
        Instance.BeginConnectionWatchInternal(effectiveTimeout, onDismiss);
    }

    /// <summary>Đánh dấu client đã connect thành công, huỷ timeout đang chờ.</summary>
    public static void MarkClientConnected()
    {
        if (Instance != null)
            Instance.MarkConnectedInternal();
    }

    /// <summary>Huỷ việc theo dõi connect đang chờ.</summary>
    public static void CancelPendingConnectionWatch()
    {
        if (Instance != null)
            Instance.CancelConnectionWatchInternal();
    }

    /// <summary>Reset trạng thái để panel có thể hiện lại (dùng sau khi thử kết nối lại).</summary>
    public static void Reset()
    {
        if (Instance == null) return;

        Instance._shown = false;
        Instance._localClientConnected = false;
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private void ShowInternal(string message, System.Action onDismiss)
    {
        if (_shown) return;
        _shown = true;

        System.Action pendingDismiss = _pendingDismissAction;
        CancelConnectionWatchInternal();

        System.Action dismiss = onDismiss ?? pendingDismiss ?? (() =>
        {
            if (!string.IsNullOrEmpty(fallbackScene))
                SceneManager.LoadScene(fallbackScene);
        });

        LoginLoadingManager.ShowErrorStatic(message, dismiss);
    }

    private void BeginConnectionWatchInternal(float timeoutSeconds, System.Action onDismiss)
    {
        TrySubscribeNetcode();

        _shown = false;
        _localClientConnected = false;
        _isWatchingConnection = true;
        _pendingDismissAction = onDismiss;

        if (_connectionWatchRoutine != null)
            StopCoroutine(_connectionWatchRoutine);

        _connectionWatchRoutine = StartCoroutine(ConnectionWatchCoroutine(timeoutSeconds));
        Debug.Log($"[GameErrorNotifier] Watching client connection for {timeoutSeconds:0.0}s...");
    }

    private IEnumerator ConnectionWatchCoroutine(float timeoutSeconds)
    {
        float deadline = Time.unscaledTime + timeoutSeconds;

        while (Time.unscaledTime < deadline)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && nm.IsConnectedClient)
            {
                MarkConnectedInternal();
                yield break;
            }

            yield return null;
        }

        _connectionWatchRoutine = null;

        if (_localClientConnected)
            yield break;

        Debug.LogWarning($"[GameErrorNotifier] Client connection timed out after {timeoutSeconds:0.0}s.");
        _isWatchingConnection = false;
        ShowInternal(MessageForType(ErrorType.CannotConnect), null);
    }

    private void MarkConnectedInternal()
    {
        _localClientConnected = true;
        _isWatchingConnection = false;
        _pendingDismissAction = null;

        if (_connectionWatchRoutine != null)
        {
            StopCoroutine(_connectionWatchRoutine);
            _connectionWatchRoutine = null;
        }

        LoginLoadingManager.HideLoadingStatic();
        Debug.Log("[GameErrorNotifier] Local client connected successfully.");
    }

    private void CancelConnectionWatchInternal()
    {
        _isWatchingConnection = false;
        _pendingDismissAction = null;

        if (_connectionWatchRoutine != null)
        {
            StopCoroutine(_connectionWatchRoutine);
            _connectionWatchRoutine = null;
        }
    }

    // ── Netcode auto-detect ───────────────────────────────────────────────

    private void TrySubscribeNetcode()
    {
        var nm = NetworkManager.Singleton;
        if (nm == _subscribedNetworkManager) return;

        UnsubscribeNetcode();

        if (nm == null)
            return;

        nm.OnClientConnectedCallback += OnNetcodeConnected;
        nm.OnClientDisconnectCallback += OnNetcodeDisconnect;
        nm.OnTransportFailure += OnTransportFailure;
        _subscribedNetworkManager = nm;

        Debug.Log("[GameErrorNotifier] Subscribed to Netcode events.");
    }

    private void UnsubscribeNetcode()
    {
        if (_subscribedNetworkManager == null) return;

        _subscribedNetworkManager.OnClientConnectedCallback -= OnNetcodeConnected;
        _subscribedNetworkManager.OnClientDisconnectCallback -= OnNetcodeDisconnect;
        _subscribedNetworkManager.OnTransportFailure -= OnTransportFailure;
        _subscribedNetworkManager = null;
    }

    private void OnNetcodeConnected(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;
        if (nm.IsServer) return;
        if (clientId != nm.LocalClientId) return;

        MarkConnectedInternal();
    }

    /// <summary>
    /// Fires khi:
    ///  clientId == 0            — socket fail trước khi connect
    ///  clientId == MaxValue     — connection refused / timeout
    ///  clientId == LocalClientId — mất kết nối sau khi đã vào game
    /// </summary>
    private void OnNetcodeDisconnect(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;
        if (nm.IsHost || nm.IsServer) return;

        bool isOurs = clientId == 0
                   || clientId == ulong.MaxValue
                   || (nm.LocalClientId != ulong.MaxValue && clientId == nm.LocalClientId);
        if (!isOurs) return;

        bool wasConnected = _localClientConnected && !_isWatchingConnection;
        CancelConnectionWatchInternal();

        string disconnectReason = string.IsNullOrWhiteSpace(nm.DisconnectReason)
            ? "<empty>"
            : nm.DisconnectReason;
        Debug.LogWarning($"[GameErrorNotifier] Local client disconnect detected. clientId={clientId}, reason={disconnectReason}");

        ShowInternal(MessageForType(wasConnected ? ErrorType.ConnectionLost : ErrorType.CannotConnect), null);
        _localClientConnected = false;
    }

    private void OnTransportFailure()
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsHost || nm.IsServer) && !_isWatchingConnection)
            return;

        bool wasConnected = _localClientConnected && !_isWatchingConnection;
        CancelConnectionWatchInternal();
        Debug.LogWarning("[GameErrorNotifier] NetworkManager.OnTransportFailure fired.");

        ShowInternal(MessageForType(wasConnected ? ErrorType.ConnectionLost : ErrorType.CannotConnect), null);
        _localClientConnected = false;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("[GameErrorNotifier]");
        go.AddComponent<GameErrorNotifier>();
        // Awake đặt Instance và DontDestroyOnLoad
    }

    private static string MessageForType(ErrorType type)
    {
        return type switch
        {
            ErrorType.ConnectionLost      => "Mất kết nối với máy chủ.\nVui lòng kiểm tra mạng và thử lại.",
            ErrorType.CannotConnect       => "Không thể kết nối đến máy chủ.\nĐường truyền Internet có vấn đề hoặc\nmáy chủ đang bảo trì.",
            ErrorType.ServerMaintenance   => "Máy chủ đang bảo trì.\nVui lòng thử lại sau.",
            ErrorType.SessionExpired      => "Phiên đăng nhập đã hết hạn.\nVui lòng đăng nhập lại.",
            _                             => "Đã xảy ra lỗi không xác định.",
        };
    }
}
