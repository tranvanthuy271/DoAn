using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class ConnectionUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_Text statusText;

    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        // Setup buttons
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);

        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);

        if (disconnectButton != null)
            disconnectButton.onClick.AddListener(Disconnect);

        // Setup IP input (mặc định localhost)
        if (ipInputField != null)
        {
            ipInputField.text = "127.0.0.1";
        }

        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        bool isConnected = networkManager != null && 
                          (networkManager.IsClient || networkManager.IsServer);

        if (hostButton != null)
            hostButton.interactable = !isConnected;

        if (clientButton != null)
            clientButton.interactable = !isConnected;

        if (disconnectButton != null)
            disconnectButton.interactable = isConnected;

        // Update status
        if (statusText != null)
        {
            if (isConnected)
            {
                if (networkManager.IsHost)
                    statusText.text = "Status: HOST (Server + Client)";
                else if (networkManager.IsClient)
                    statusText.text = "Status: CLIENT";
                else if (networkManager.IsServer)
                    statusText.text = "Status: SERVER";
            }
            else
            {
                statusText.text = "Status: Disconnected";
            }
        }
    }

    public void StartHost()
    {
        if (networkManager != null)
        {
            networkManager.StartHost();
            Debug.Log("Started as HOST");
        }
    }

    public void StartClient()
    {
        if (networkManager != null)
        {
            // Set IP address nếu có
            var transport = networkManager.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport != null && ipInputField != null)
            {
                transport.ConnectionData.Address = ipInputField.text;
            }

            networkManager.StartClient();
            Debug.Log($"Started as CLIENT, connecting to {ipInputField?.text ?? "localhost"}");
        }
    }

    public void Disconnect()
    {
        if (networkManager != null)
        {
            networkManager.Shutdown();
            Debug.Log("Disconnected");
        }
    }
}







