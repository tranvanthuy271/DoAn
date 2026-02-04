using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;

public class NetworkManagerCustom : MonoBehaviour
{
    [Header("Server Config")]
    public string serverIP = "127.0.0.1"; // localhost
    public ushort serverPort = 2003;

    private NetworkManager networkManager;

    void Start()
    {
        networkManager = NetworkManager.Singleton;
        
        // Setup callbacks
        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;
    }

    public void ConnectToServer()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = serverIP;
        transport.ConnectionData.Port = serverPort;
        
        if (networkManager.StartClient())
        {
            Debug.Log($"Connecting to {serverIP}:{serverPort}");
        }
        else
        {
            Debug.LogError("Failed to start client!");
        }
    }

    public void StartHost()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = serverPort;
        
        if (networkManager.StartHost())
        {
            Debug.Log($"Host started on port {serverPort}");
        }
        else
        {
            Debug.LogError("Failed to start host!");
        }
    }

    public void StartServer()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.LogError("UnityTransport not found!");
            return;
        }

        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = serverPort;
        
        if (networkManager.StartServer())
        {
            Debug.Log($"Server started on port {serverPort}");
        }
        else
        {
            Debug.LogError("Failed to start server!");
        }
    }

    public void Disconnect()
    {
        if (networkManager != null && networkManager.IsClient)
        {
            networkManager.Shutdown();
            Debug.Log("Disconnected from server");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected to server");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected from server");
    }

    void OnDestroy()
    {
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback -= OnClientConnected;
            networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
