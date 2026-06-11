using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Service xử lý lỗi kết nối game.
// Luồng mới không còn dùng error panel cũ mà hiển thị loader spinner rồi tự quay về scene fallback.
public class GameErrorNotifier : MonoBehaviour
{
    public static GameErrorNotifier Instance { get; private set; }
    private const string IntentionalLogoutStatus = "\u0110ang \u0111\u0103ng xu\u1ea5t...";
    private static float _suppressDisconnectNotificationsUntil = -1f;

    public enum ErrorType
    {
        ConnectionLost,
        CannotConnect,
        ServerMaintenance,
        SessionExpired,
        Unknown,
    }

    [Header("Tự động lắng nghe Netcode disconnect")]
    [Tooltip("Bật để tự phát hiện mất kết nối game server và hiển thị loader chờ quay về.")]
    public bool autoDetectNetworkDisconnect = true;

    [Header("Scene fallback")]
    [Tooltip("Scene sẽ quay về sau khi hiển thị loader lỗi.")]
    public string fallbackScene = "Login";

    [Header("Timeout / Delay")]
    [Min(1f)]
    public float connectionTimeoutSeconds = 8f;
    [Min(0.1f)]
    public float dismissDelaySeconds = 1.5f;

    private NetworkManager _subscribedNetworkManager;
    private Coroutine _connectionWatchRoutine;
    private Coroutine _autoDismissRoutine;
    private System.Action _pendingDismissAction;
    private bool _isWatchingConnection;
    private bool _localClientConnected;
    private bool _shown;

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
        {
            TrySubscribeNetcode();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (autoDetectNetworkDisconnect)
        {
            TrySubscribeNetcode();
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        CancelConnectionWatchInternal();
        StopAutoDismissRoutine();
        UnsubscribeNetcode();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _shown = false;
        _localClientConnected = NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;

        if (autoDetectNetworkDisconnect)
        {
            TrySubscribeNetcode();
        }
    }

    public static void EnsureReady()
    {
        EnsureInstance();
        Instance.TrySubscribeNetcode();
    }

    public static void Show(ErrorType type, System.Action onDismiss = null)
    {
        EnsureReady();
        if (IsDisconnectNotificationSuppressed)
        {
            LoginLoadingManager.ShowLoadingStatic(IntentionalLogoutStatus);
            return;
        }

        Instance.ShowInternal(MessageForType(type), onDismiss);
    }

    public static void Show(string rawMessage, System.Action onDismiss = null)
    {
        EnsureReady();
        if (IsDisconnectNotificationSuppressed)
        {
            LoginLoadingManager.ShowLoadingStatic(IntentionalLogoutStatus);
            return;
        }

        Instance.ShowInternal(rawMessage, onDismiss);
    }

    public static void WatchClientConnection(float timeoutSeconds = -1f, System.Action onDismiss = null)
    {
        EnsureReady();
        float effectiveTimeout = timeoutSeconds > 0f ? timeoutSeconds : Instance.connectionTimeoutSeconds;
        Instance.BeginConnectionWatchInternal(effectiveTimeout, onDismiss);
    }

    public static void MarkClientConnected()
    {
        if (Instance != null)
        {
            Instance.MarkConnectedInternal();
        }
    }

    public static void CancelPendingConnectionWatch()
    {
        if (Instance != null)
        {
            Instance.CancelConnectionWatchInternal();
        }
    }

    public static bool IsDisconnectNotificationSuppressed =>
        Time.realtimeSinceStartup < _suppressDisconnectNotificationsUntil;

    public static void SuppressDisconnectNotifications(float seconds = 10f)
    {
        _suppressDisconnectNotificationsUntil = Mathf.Max(
            _suppressDisconnectNotificationsUntil,
            Time.realtimeSinceStartup + Mathf.Max(0.1f, seconds));

        LoginLoadingManager.ShowLoadingStatic(IntentionalLogoutStatus);

        if (Instance != null)
        {
            Instance.CancelConnectionWatchInternal();
        }
    }

    public static void Reset()
    {
        if (Instance == null)
        {
            return;
        }

        Instance._shown = false;
        Instance._localClientConnected = false;
        Instance.StopAutoDismissRoutine();
    }

    private void ShowInternal(string message, System.Action onDismiss)
    {
        if (IsDisconnectNotificationSuppressed)
        {
            LoginLoadingManager.ShowLoadingStatic(IntentionalLogoutStatus);
            return;
        }

        if (_shown)
        {
            return;
        }

        _shown = true;

        System.Action pendingDismiss = _pendingDismissAction;
        CancelConnectionWatchInternal();
        StopAutoDismissRoutine();

        LoginLoadingManager.ShowLoadingStatic(message);

        System.Action dismiss = onDismiss ?? pendingDismiss ?? DefaultDismissAction;
        _autoDismissRoutine = StartCoroutine(AutoDismissRoutine(dismiss));
    }

    private void BeginConnectionWatchInternal(float timeoutSeconds, System.Action onDismiss)
    {
        TrySubscribeNetcode();

        _shown = false;
        _localClientConnected = false;
        _isWatchingConnection = true;
        _pendingDismissAction = onDismiss;

        StopAutoDismissRoutine();

        if (_connectionWatchRoutine != null)
        {
            StopCoroutine(_connectionWatchRoutine);
        }

        _connectionWatchRoutine = StartCoroutine(ConnectionWatchCoroutine(timeoutSeconds));
        Debug.Log($"[GameErrorNotifier] Watching client connection for {timeoutSeconds:0.0}s...");
    }

    private IEnumerator ConnectionWatchCoroutine(float timeoutSeconds)
    {
        float deadline = Time.unscaledTime + timeoutSeconds;

        while (Time.unscaledTime < deadline)
        {
            NetworkManager networkManager = NetworkManager.Singleton;
            if (networkManager != null && networkManager.IsConnectedClient)
            {
                MarkConnectedInternal();
                yield break;
            }

            yield return null;
        }

        _connectionWatchRoutine = null;

        if (_localClientConnected)
        {
            yield break;
        }

        Debug.LogWarning($"[GameErrorNotifier] Client connection timed out after {timeoutSeconds:0.0}s.");
        _isWatchingConnection = false;
        ShowInternal(MessageForType(ErrorType.CannotConnect), null);
    }

    private IEnumerator AutoDismissRoutine(System.Action dismiss)
    {
        float waitSeconds = Mathf.Max(0.1f, dismissDelaySeconds);
        yield return new WaitForSecondsRealtime(waitSeconds);

        _autoDismissRoutine = null;
        dismiss?.Invoke();
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

        StopAutoDismissRoutine();
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

    private void StopAutoDismissRoutine()
    {
        if (_autoDismissRoutine != null)
        {
            StopCoroutine(_autoDismissRoutine);
            _autoDismissRoutine = null;
        }
    }

    private void TrySubscribeNetcode()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == _subscribedNetworkManager)
        {
            return;
        }

        UnsubscribeNetcode();

        if (networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback += OnNetcodeConnected;
        networkManager.OnClientDisconnectCallback += OnNetcodeDisconnect;
        networkManager.OnTransportFailure += OnTransportFailure;
        _subscribedNetworkManager = networkManager;

        Debug.Log("[GameErrorNotifier] Subscribed to Netcode events.");
    }

    private void UnsubscribeNetcode()
    {
        if (_subscribedNetworkManager == null)
        {
            return;
        }

        _subscribedNetworkManager.OnClientConnectedCallback -= OnNetcodeConnected;
        _subscribedNetworkManager.OnClientDisconnectCallback -= OnNetcodeDisconnect;
        _subscribedNetworkManager.OnTransportFailure -= OnTransportFailure;
        _subscribedNetworkManager = null;
    }

    private void OnNetcodeConnected(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.IsServer || clientId != networkManager.LocalClientId)
        {
            return;
        }

        MarkConnectedInternal();
    }

    private void OnNetcodeDisconnect(ulong clientId)
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null || networkManager.IsHost || networkManager.IsServer)
        {
            return;
        }

        bool isOurs = clientId == 0
                      || clientId == ulong.MaxValue
                      || (networkManager.LocalClientId != ulong.MaxValue && clientId == networkManager.LocalClientId);
        if (!isOurs)
        {
            return;
        }

        if (IsDisconnectNotificationSuppressed)
        {
            CancelConnectionWatchInternal();
            _localClientConnected = false;
            Debug.Log("[GameErrorNotifier] Local client disconnect ignored because logout is intentional.");
            return;
        }

        bool wasConnected = _localClientConnected && !_isWatchingConnection;
        CancelConnectionWatchInternal();

        string disconnectReason = string.IsNullOrWhiteSpace(networkManager.DisconnectReason)
            ? "<empty>"
            : networkManager.DisconnectReason;
        Debug.LogWarning($"[GameErrorNotifier] Local client disconnect detected. clientId={clientId}, reason={disconnectReason}");

        ShowInternal(MessageForType(wasConnected ? ErrorType.ConnectionLost : ErrorType.CannotConnect), null);
        _localClientConnected = false;
    }

    private void OnTransportFailure()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && (networkManager.IsHost || networkManager.IsServer) && !_isWatchingConnection)
        {
            return;
        }

        bool wasConnected = _localClientConnected && !_isWatchingConnection;

        if (IsDisconnectNotificationSuppressed)
        {
            CancelConnectionWatchInternal();
            _localClientConnected = false;
            Debug.Log("[GameErrorNotifier] Transport failure ignored because logout is intentional.");
            return;
        }

        CancelConnectionWatchInternal();
        Debug.LogWarning("[GameErrorNotifier] NetworkManager.OnTransportFailure fired.");

        ShowInternal(MessageForType(wasConnected ? ErrorType.ConnectionLost : ErrorType.CannotConnect), null);
        _localClientConnected = false;
    }

    private void DefaultDismissAction()
    {
        if (!string.IsNullOrEmpty(fallbackScene))
        {
            SceneManager.LoadScene(fallbackScene);
        }
        else
        {
            LoginLoadingManager.HideLoadingStatic();
        }
    }

    private static void EnsureInstance()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject go = new GameObject("[GameErrorNotifier]");
        go.AddComponent<GameErrorNotifier>();
    }

    private static string MessageForType(ErrorType type)
    {
        return type switch
        {
            ErrorType.ConnectionLost => "Mất kết nối với máy chủ. Đang quay về đăng nhập...",
            ErrorType.CannotConnect => "Không thể kết nối đến máy chủ. Đang quay về đăng nhập...",
            ErrorType.ServerMaintenance => "Máy chủ đang bảo trì. Vui lòng thử lại sau.",
            ErrorType.SessionExpired => "Phiên đăng nhập đã hết hạn. Đang quay về đăng nhập...",
            _ => "Đã xảy ra lỗi không xác định. Đang quay về đăng nhập..."
        };
    }
}
