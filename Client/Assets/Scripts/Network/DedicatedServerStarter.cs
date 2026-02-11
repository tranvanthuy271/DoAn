using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// DedicatedServerStarter - Tự động start server khi build server
/// Chỉ chạy khi có define UNITY_SERVER hoặc khi được gọi từ code
/// </summary>
public class DedicatedServerStarter : MonoBehaviour
{
    [Header("Server Config")]
    [SerializeField] private ushort serverPort = 2003;
    [SerializeField] private bool autoStartOnAwake = true;
    [SerializeField] private bool isDedicatedServer = false;

    private NetworkManager networkManager;

    private void Awake()
    {
        networkManager = NetworkManager.Singleton;
        
        #if UNITY_SERVER
        // Tự động set isDedicatedServer = true khi build server
        isDedicatedServer = true;
        Debug.Log("[DedicatedServerStarter] UNITY_SERVER define detected. Running as dedicated server.");
        #endif

        if (autoStartOnAwake && isDedicatedServer)
        {
            StartDedicatedServer();
        }
    }

    /// <summary>
    /// Start dedicated server
    /// </summary>
    public void StartDedicatedServer()
    {
        if (networkManager == null)
        {
            Debug.LogError("[DedicatedServerStarter] NetworkManager not found!");
            return;
        }

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("[DedicatedServerStarter] UnityTransport not found!");
            return;
        }

        // Configure transport
        transport.ConnectionData.Address = "0.0.0.0"; // Listen on all interfaces
        transport.ConnectionData.Port = serverPort;

        // Start server
        if (networkManager.StartServer())
        {
            Debug.Log($"[DedicatedServerStarter] ✓ Dedicated Server started on port {serverPort}");
            Debug.Log($"[DedicatedServerStarter] Server is ready to accept connections!");
        }
        else
        {
            Debug.LogError("[DedicatedServerStarter] ✗ Failed to start dedicated server!");
        }
    }

    /// <summary>
    /// Stop server
    /// </summary>
    public void StopServer()
    {
        if (networkManager != null && networkManager.IsServer)
        {
            networkManager.Shutdown();
            Debug.Log("[DedicatedServerStarter] Server stopped.");
        }
    }

    /// <summary>
    /// Set server port (có thể gọi từ command line arguments)
    /// </summary>
    public void SetServerPort(ushort port)
    {
        serverPort = port;
        Debug.Log($"[DedicatedServerStarter] Server port set to {serverPort}");
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }
}
