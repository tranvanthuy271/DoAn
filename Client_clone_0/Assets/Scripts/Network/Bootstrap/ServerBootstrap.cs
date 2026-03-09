using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Script để khởi động dedicated server
/// Đặt trong scene DedicatedServerScene
/// Tự động start server khi scene load
/// </summary>
public class ServerBootstrap : MonoBehaviour
{
    [Header("Server Config")]
    [Tooltip("Port để server listen (mặc định: 2003)")]
    public ushort serverPort = 2003;
    
    [Tooltip("IP để server listen (0.0.0.0 = listen all interfaces)")]
    public string serverIP = "0.0.0.0";

    [Header("Auto Start")]
    [Tooltip("Tự động start server khi scene load. CHỈ bật trong ServerScene, TẮT trong Client scenes (Main, Login, etc.)")]
    public bool autoStart = true;

    void Start()
    {
        // Chỉ auto start nếu đang trong scene ServerScene
        // Nếu không phải ServerScene, không tự động start (để client chỉ connect)
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        // UnityEngine.Debug.Log($"[ServerBootstrap] Start() called in scene: '{currentSceneName}'");
        
        if (autoStart && currentSceneName == "ServerScene")
        {
            // UnityEngine.Debug.Log("[ServerBootstrap] Scene is ServerScene, starting dedicated server...");
            StartDedicatedServer();
        }
        else if (autoStart && currentSceneName != "ServerScene")
        {
            // UnityEngine.Debug.LogWarning($"[ServerBootstrap] ⚠️ Auto start is enabled but current scene is '{currentSceneName}', not 'ServerScene'. Disabling auto start to prevent client from starting server.");
            // UnityEngine.Debug.LogWarning($"[ServerBootstrap] ⚠️ ServerBootstrap should only be in ServerScene. Please remove it from scene '{currentSceneName}'.");
        }
        else if (!autoStart)
        {
            // UnityEngine.Debug.Log($"[ServerBootstrap] Auto start is disabled. Current scene: '{currentSceneName}'");
        }
    }

    /// <summary>
    /// Khởi động dedicated server
    /// </summary>
    public void StartDedicatedServer()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            // Debug.LogError("[ServerBootstrap] NetworkManager not found! Make sure NetworkManager is in the scene.");
            return;
        }

        // Kiểm tra nếu server/host đã chạy
        if (networkManager.IsServer || networkManager.IsHost)
        {
            // Debug.LogWarning("[ServerBootstrap] Server/Host is already running!");
            return;
        }

        // Setup Unity Transport
        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            // Debug.LogError("[ServerBootstrap] UnityTransport not found on NetworkManager!");
            return;
        }

        // Cấu hình transport
        transport.ConnectionData.Address = serverIP;
        transport.ConnectionData.Port = serverPort;

        // Debug.Log($"[ServerBootstrap] Starting dedicated server on {serverIP}:{serverPort}...");

        // Start Host (vừa server vừa client) để có thể join vào map
        if (networkManager.StartHost())
        {
            // Debug.Log($"[ServerBootstrap] ✓✓✓ Dedicated Host started successfully on {serverIP}:{serverPort} ✓✓✓");
            // Debug.Log($"[ServerBootstrap] Host is ready. Waiting for clients to connect...");
        }
        else
        {
            // Debug.LogError("[ServerBootstrap] ✗ Failed to start dedicated host!");
            // Debug.LogError("[ServerBootstrap] Check if port is already in use or NetworkManager is configured correctly.");
        }
    }

    /// <summary>
    /// Dừng server
    /// </summary>
    public void StopServer()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager != null && (networkManager.IsServer || networkManager.IsHost))
        {
            networkManager.Shutdown();
            // Debug.Log("[ServerBootstrap] Server/Host stopped.");
        }
    }

    void OnApplicationQuit()
    {
        StopServer();
    }
}
