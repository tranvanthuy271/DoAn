using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class DebugNetworkHUD : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private Text debugTextLegacy; // Fallback cho UGUI Text

    [Header("Settings")]
    [SerializeField] private bool showFPS = true;
    [SerializeField] private bool showPing = true;
    [SerializeField] private bool showClientInfo = true;

    private NetworkManager networkManager;
    private float fps;
    private float fpsUpdateInterval = 0.5f;
    private float fpsAccumulator = 0f;
    private int fpsFrames = 0;
    private float fpsTimer = 0f;

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        // Tự động tìm Text component nếu chưa gán
        if (debugText == null && debugTextLegacy == null)
        {
            debugText = GetComponent<TextMeshProUGUI>();
            if (debugText == null)
            {
                debugTextLegacy = GetComponent<Text>();
            }
        }
    }

    private void Update()
    {
        UpdateFPS();
        UpdateDebugText();
    }

    private void UpdateFPS()
    {
        if (!showFPS) return;

        fpsTimer += Time.unscaledDeltaTime;
        fpsAccumulator += Time.unscaledDeltaTime;
        fpsFrames++;

        if (fpsTimer >= fpsUpdateInterval)
        {
            fps = fpsFrames / fpsAccumulator;
            fpsTimer = 0f;
            fpsAccumulator = 0f;
            fpsFrames = 0;
        }
    }

    private void UpdateDebugText()
    {
        if (networkManager == null)
        {
            networkManager = NetworkManager.Singleton;
        }

        string text = "";

        if (networkManager != null && (networkManager.IsClient || networkManager.IsServer))
        {
            // Client Info
            if (showClientInfo)
            {
                text += $"ClientId: {networkManager.LocalClientId} | ";
                
                if (networkManager.IsHost)
                    text += "Role: Host (Server + Client)";
                else if (networkManager.IsServer)
                    text += "Role: Server";
                else if (networkManager.IsClient)
                    text += "Role: Client";
                
                text += "\n";
            }

            // Connection Info
            if (networkManager.IsConnectedClient)
            {
                text += $"Connected: Yes";
                if (networkManager.IsHost)
                {
                    text += $" | Clients: {networkManager.ConnectedClients.Count}";
                }
                text += "\n";
            }

            // Connection Time (thời gian đã kết nối)
            if (showPing && networkManager.IsClient)
            {
                // Hiển thị thời gian đã kết nối (từ khi start client)
                // Unity Netcode không có NetworkTime trực tiếp, dùng Time.time thay thế
                text += $"Uptime: {Time.time:F1}s\n";
            }
        }
        else
        {
            text = "Status: Disconnected\n";
        }

        // FPS
        if (showFPS)
        {
            text += $"FPS: {fps:F1}";
        }

        // Update UI
        if (debugText != null)
        {
            debugText.text = text;
        }
        else if (debugTextLegacy != null)
        {
            debugTextLegacy.text = text;
        }
    }
}

