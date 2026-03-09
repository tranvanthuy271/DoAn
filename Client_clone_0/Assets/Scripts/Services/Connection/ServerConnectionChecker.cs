using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Unity.Netcode;

/// <summary>
/// Check xem API Server và Netcode Server đã start chưa
/// </summary>
public class ServerConnectionChecker : MonoBehaviour
{
    [Header("Server Config")]
    public string apiBaseURL = "http://localhost:5000/api";
    public string netcodeServerIP = "127.0.0.1";
    public ushort netcodeServerPort = 2003;
    public float checkInterval = 2f; // Check mỗi 2 giây

    [Header("Connection Timeout")]
    public float connectionTimeout = 10f;

    private NetworkManager networkManager;
    private bool isChecking = false;

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
    }

    /// <summary>
    /// Check cả API Server và Netcode Server
    /// </summary>
    public void CheckServers(System.Action<bool> onComplete)
    {
        if (isChecking)
        {
            // UnityEngine.Debug.LogWarning("[ServerChecker] Đang check server...");
            return;
        }

        StartCoroutine(CheckServersCoroutine(onComplete));
    }

    private IEnumerator CheckServersCoroutine(System.Action<bool> onComplete)
    {
        isChecking = true;

        // 1. Check API Server
        bool apiServerReady = false;
        StartCoroutine(CheckAPIServer((ready) => { apiServerReady = ready; }));

        // 2. Check Netcode Server
        bool netcodeServerReady = false;
        StartCoroutine(CheckNetcodeServer((ready) => { netcodeServerReady = ready; }));

        // Đợi cả 2 check xong
        float timeout = Time.time + connectionTimeout;
        while ((!apiServerReady || !netcodeServerReady) && Time.time < timeout)
        {
            yield return new WaitForSeconds(0.5f);
        }

        bool allReady = apiServerReady && netcodeServerReady;

        if (allReady)
        {
            // UnityEngine.Debug.Log("[ServerChecker] ✓ Cả 2 server đã sẵn sàng!");
        }
        else
        {
            if (!apiServerReady)
            {
                // UnityEngine.Debug.LogError("[ServerChecker] ✗ API Server chưa sẵn sàng!");
            }
            if (!netcodeServerReady)
            {
                // UnityEngine.Debug.LogError("[ServerChecker] ✗ Netcode Server chưa sẵn sàng!");
            }
        }

        isChecking = false;
        onComplete?.Invoke(allReady);
    }

    /// <summary>
    /// Check API Server bằng cách gọi health check endpoint
    /// </summary>
    private IEnumerator CheckAPIServer(System.Action<bool> onComplete)
    {
        // Thử gọi một endpoint đơn giản (ví dụ: GET /api/player/1/data)
        using (UnityWebRequest www = UnityWebRequest.Get($"{apiBaseURL}/player/1/data"))
        {
            www.timeout = 3; // 3 giây timeout
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success || 
                www.responseCode == 401 || // Unauthorized (server đã chạy nhưng cần auth)
                www.responseCode == 404)   // Not Found (server đã chạy nhưng endpoint không tồn tại)
            {
                // UnityEngine.Debug.Log("[ServerChecker] ✓ API Server đã sẵn sàng");
                onComplete?.Invoke(true);
            }
            else
            {
                // UnityEngine.Debug.LogWarning($"[ServerChecker] API Server chưa sẵn sàng: {www.error}");
                onComplete?.Invoke(false);
            }
        }
    }

    /// <summary>
    /// Check Netcode Server bằng cách thử UDP ping (không cần NetworkManager)
    /// Hoặc đơn giản chỉ check xem có thể connect được không
    /// </summary>
    private IEnumerator CheckNetcodeServer(System.Action<bool> onComplete)
    {
        // Đơn giản: Thử connect thực sự, nếu được thì OK
        // Không cần tạo NetworkManager tạm thời vì sẽ có trong scene khi cần
        
        // Tìm NetworkManager trong scene (có thể chưa có)
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        // Nếu không có NetworkManager, đợi một chút rồi thử lại
        // Hoặc đơn giản: Bỏ qua check này, để client tự thử connect
        if (networkManager == null)
        {
            // UnityEngine.Debug.Log("[ServerChecker] NetworkManager not found. Will try to connect directly when needed.");
            // Giả sử server sẵn sàng nếu API Server đã sẵn sàng
            // Client sẽ tự thử connect và báo lỗi nếu không được
            onComplete?.Invoke(true); // Giả sử OK, để client tự thử connect
            yield break;
        }

        // Nếu đã connect rồi thì OK
        if (networkManager.IsConnectedClient || networkManager.IsHost)
        {
            // UnityEngine.Debug.Log("[ServerChecker] ✓ Đã connect đến Netcode Server/Host");
            onComplete?.Invoke(true);
            yield break;
        }

        // Thử connect thực sự
        var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
        if (transport == null)
        {
            // UnityEngine.Debug.LogError("[ServerChecker] UnityTransport not found!");
            onComplete?.Invoke(false);
            yield break;
        }

        transport.ConnectionData.Address = netcodeServerIP;
        transport.ConnectionData.Port = netcodeServerPort;

        bool connected = false;
        float timeout = Time.time + 3f;

        System.Action<ulong> onConnected = null;
        onConnected = (ulong clientId) =>
        {
            connected = true;
        };

        networkManager.OnClientConnectedCallback += onConnected;

        if (networkManager.StartClient())
        {
            // Đợi connect hoặc timeout
            while (!connected && Time.time < timeout)
            {
                yield return new WaitForSeconds(0.1f);
            }

            if (connected)
            {
                // UnityEngine.Debug.Log("[ServerChecker] ✓ Netcode Server đã sẵn sàng");
                // Disconnect ngay để test
                networkManager.Shutdown();
                onComplete?.Invoke(true);
            }
            else
            {
                // UnityEngine.Debug.LogWarning("[ServerChecker] Netcode Server chưa sẵn sàng (timeout)");
                networkManager.Shutdown();
                onComplete?.Invoke(false);
            }
        }
        else
        {
            // UnityEngine.Debug.LogError("[ServerChecker] Không thể start client để test!");
            onComplete?.Invoke(false);
        }

        // Cleanup
        if (onConnected != null && networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= onConnected;
        }
    }
}
