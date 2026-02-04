using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Button joinGameButton;
    public Button logoutButton;
    public Text playerInfoText;

    private NetworkManagerCustom networkManager;
    private APIClient apiClient;

    void Start()
    {
        networkManager = FindObjectOfType<NetworkManagerCustom>();
        if (networkManager == null)
        {
            GameObject networkManagerObj = new GameObject("NetworkManagerCustom");
            networkManager = networkManagerObj.AddComponent<NetworkManagerCustom>();
        }

        apiClient = APIClient.Instance;

        joinGameButton.onClick.AddListener(OnJoinGameClicked);
        logoutButton.onClick.AddListener(OnLogoutClicked);

        UpdatePlayerInfo();
    }

    private void UpdatePlayerInfo()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var playerData = GameManager.Instance.GetPlayerData();
            playerInfoText.text = $"Level: {playerData.level} | Gold: {playerData.gold} | EXP: {playerData.experience}/{playerData.exp_required_for_next_level}";
        }
        else
        {
            playerInfoText.text = "Chưa có dữ liệu player";
        }
    }

    private void OnJoinGameClicked()
    {
        if (networkManager == null)
        {
            Debug.LogError("NetworkManagerCustom not found!");
            return;
        }

        joinGameButton.interactable = false;
        playerInfoText.text = "Đang kết nối đến server...";

        // Connect to server
        networkManager.ConnectToServer();

        // Chuyển sang Game Scene sau khi connect
        Invoke(nameof(LoadGameScene), 1f);
    }

    private void LoadGameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    private void OnLogoutClicked()
    {
        // Clear token
        if (apiClient != null)
        {
            apiClient.ClearToken();
        }

        PlayerPrefs.DeleteKey("JWT_TOKEN");
        PlayerPrefs.DeleteKey("USER_ID");
        PlayerPrefs.DeleteKey("USERNAME");

        // Chuyển về Login Scene
        SceneManager.LoadScene("Login");
    }
}
