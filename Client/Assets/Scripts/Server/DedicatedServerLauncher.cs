using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Diagnostics;
using System.IO;

/// <summary>
/// Dedicated Server Launcher: Tự động start API Server và Netcode Server
/// Chạy độc lập, không cần UI
/// </summary>
public class DedicatedServerLauncher : MonoBehaviour
{
    [Header("Server Config")]
    public string apiServerPath = ""; // Path đến GameServerApi.exe
    public ushort netcodePort = 2003;
    public string serverIP = "127.0.0.1";

    [Header("API Server Config")]
    public string apiBaseURL = "http://localhost:5000/api";

    private NetworkManager networkManager;
    private Process apiServerProcess;
    private bool isServerRunning = false;

    private void Awake()
    {
        // Singleton pattern
        if (FindObjectsOfType<DedicatedServerLauncher>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        // DontDestroyOnLoad(gameObject); // Removed: DedicatedServerLauncher chỉ nên chạy trong ServerScene, không persist qua các scene
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
        
        // Chỉ auto start nếu đang trong scene ServerScene
        // Nếu không phải ServerScene, không tự động start (để client chỉ connect)
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        UnityEngine.Debug.Log($"[DedicatedServerLauncher] Start() called in scene: '{currentSceneName}'");
        
        if (currentSceneName == "ServerScene")
        {
            // Tự động start server khi script start (chỉ trong ServerScene)
            UnityEngine.Debug.Log("[DedicatedServerLauncher] Scene is ServerScene, starting dedicated server...");
            StartDedicatedServer();
        }
        else
        {
            UnityEngine.Debug.LogWarning($"[DedicatedServerLauncher] ⚠️ Current scene is '{currentSceneName}', not 'ServerScene'. DedicatedServerLauncher should only be in ServerScene. Disabling auto start.");
            UnityEngine.Debug.LogWarning($"[DedicatedServerLauncher] ⚠️ Please remove DedicatedServerLauncher from scene '{currentSceneName}'.");
        }
    }

    /// <summary>
    /// Start cả API Server và Netcode Server
    /// </summary>
    public void StartDedicatedServer()
    {
        if (isServerRunning)
        {
            UnityEngine.Debug.LogWarning("[DedicatedServer] Server đã đang chạy!");
            return;
        }

        UnityEngine.Debug.Log("[DedicatedServer] Đang khởi động Dedicated Server...");

        // 1. Start API Server
        StartAPIServer();

        // 2. Đợi API Server start (3 giây) rồi thử start Netcode Server
        // Lưu ý: Nếu ServerBootstrap đã start server, thì sẽ bỏ qua
        Invoke(nameof(StartNetcodeServer), 3f);
    }

    /// <summary>
    /// Start API Server (GameServerApi.exe)
    /// </summary>
    private void StartAPIServer()
    {
        // Tìm path đến GameServerApi.exe
        if (string.IsNullOrEmpty(apiServerPath))
        {
            // Tự động tìm trong project
            string projectPath = Application.dataPath.Replace("/Assets", "");
            string[] possiblePaths = new string[]
            {
                Path.Combine(projectPath, "GameServerApi", "bin", "Debug", "net9.0", "GameServerApi.exe"),
                Path.Combine(projectPath, "..", "GameServerApi", "bin", "Debug", "net9.0", "GameServerApi.exe"),
                Path.Combine(Directory.GetCurrentDirectory(), "GameServerApi.exe")
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    apiServerPath = path;
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(apiServerPath) || !File.Exists(apiServerPath))
        {
            UnityEngine.Debug.LogError($"[DedicatedServer] Không tìm thấy GameServerApi.exe tại: {apiServerPath}");
            UnityEngine.Debug.LogError("Vui lòng build GameServerApi project trước!");
            return;
        }

        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = apiServerPath,
                UseShellExecute = true,
                CreateNoWindow = false, // Hiển thị console để debug
                WorkingDirectory = Path.GetDirectoryName(apiServerPath)
            };

            apiServerProcess = Process.Start(startInfo);
            UnityEngine.Debug.Log($"[DedicatedServer] ✓ API Server đã khởi động: {apiServerPath}");
        }
        catch (System.Exception ex)
        {
            UnityEngine.Debug.LogError($"[DedicatedServer] ✗ Lỗi khi start API Server: {ex.Message}");
        }
    }

    /// <summary>
    /// Start Netcode Server (chỉ start nếu chưa có server nào đang chạy)
    /// </summary>
    private void StartNetcodeServer()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        if (networkManager == null)
        {
            UnityEngine.Debug.LogError("[DedicatedServer] NetworkManager not found!");
            return;
        }

        // Kiểm tra xem server/host đã start chưa (có thể bởi ServerBootstrap hoặc script khác)
        if (networkManager.IsServer || networkManager.IsHost)
        {
            UnityEngine.Debug.Log("[DedicatedServer] Netcode Server/Host đã được start bởi script khác (có thể là ServerBootstrap). Bỏ qua.");
            isServerRunning = true;
            return;
        }

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            UnityEngine.Debug.LogError("[DedicatedServer] UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = netcodePort;

        if (networkManager.StartServer())
        {
            isServerRunning = true;
            UnityEngine.Debug.Log($"[DedicatedServer] ✓ Netcode Server đã khởi động trên port {netcodePort}");
        }
        else
        {
            UnityEngine.Debug.LogError("[DedicatedServer] ✗ Không thể start Netcode Server!");
        }
    }

    /// <summary>
    /// Stop server
    /// </summary>
    public void StopDedicatedServer()
    {
        // Stop Netcode Server/Host
        if (networkManager != null && (networkManager.IsServer || networkManager.IsHost))
        {
            networkManager.Shutdown();
            isServerRunning = false;
            UnityEngine.Debug.Log("[DedicatedServer] Netcode Server/Host đã dừng");
        }

        // Stop API Server
        if (apiServerProcess != null && !apiServerProcess.HasExited)
        {
            apiServerProcess.Kill();
            apiServerProcess.Dispose();
            apiServerProcess = null;
            UnityEngine.Debug.Log("[DedicatedServer] API Server đã dừng");
        }
    }

    private void OnApplicationQuit()
    {
        StopDedicatedServer();
    }
}
