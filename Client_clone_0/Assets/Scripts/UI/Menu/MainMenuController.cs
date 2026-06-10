using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Unity.Netcode;

public class MainMenuController : MonoBehaviour
{
    [Header("UI References")]
    public Button joinGameButton;
    public Button logoutButton;
    public TMP_Text playerInfoText;

    private NetworkManagerCustom networkManager;
    private APIClient apiClient;
    private bool isLoggingOut;

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
            // Debug.LogError("NetworkManagerCustom not found!");
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
        // Tìm scene game trong Build Settings (có thể là "Main" hoặc "GameScene")
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        string targetSceneName = null;
        int targetSceneIndex = -1;
        
        // Debug.Log($"[MainMenuController] Checking Build Settings: {sceneCount} scenes in build");
        
        // Ưu tiên tìm "Main" trước, nếu không có thì tìm "GameScene"
        for (int i = 0; i < sceneCount; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            // Debug.Log($"[MainMenuController] Build index {i}: '{sceneName}' (path: {scenePath})");
            
            if (sceneName == "Main" || sceneName == "GameScene")
            {
                targetSceneName = sceneName;
                targetSceneIndex = i;
                // Debug.Log($"[MainMenuController] ✓ Scene '{targetSceneName}' found at build index {targetSceneIndex}");
                break;
            }
        }
        
        if (targetSceneIndex >= 0)
        {
            // Debug.Log($"[MainMenuController] Loading scene '{targetSceneName}' by index {targetSceneIndex}...");
            SceneManager.LoadScene(targetSceneIndex);
        }
        else
        {
            // Debug.LogError("[MainMenuController] ✗ Scene 'Main' or 'GameScene' not found in Build Settings!");
            // Debug.LogError("[MainMenuController] Please add scene 'Main' or 'GameScene' to Build Settings (File → Build Settings → Add Open Scenes)");
            // Fallback: thử load bằng tên
            SceneManager.LoadScene("Main");
        }
    }

    private void OnLogoutClicked()
    {
        if (isLoggingOut)
            return;

        isLoggingOut = true;
        logoutButton.interactable = false;

        int playerId = GameManager.Instance?.GetPlayerData()?.player_id ?? PlayerPrefs.GetInt("USER_ID", 0);
        string logoutMessage = "\u0110ang \u0111\u0103ng xu\u1ea5t...";
        GameErrorNotifier.SuppressDisconnectNotifications();
        if (playerInfoText != null)
            playerInfoText.text = logoutMessage;
        LoginLoadingManager.ShowLoadingStatic(logoutMessage);

        if (apiClient != null && playerId > 0 && !string.IsNullOrEmpty(apiClient.GetToken()))
        {
            apiClient.ResetPlayerToStartMap(
                playerId,
                onSuccess: CompleteLogout,
                onError: _ => CompleteLogout());
            return;
        }

        CompleteLogout();
    }

    private void CompleteLogout()
    {
        StartCoroutine(CompleteLogoutRoutine());
    }

    private System.Collections.IEnumerator CompleteLogoutRoutine()
    {
        GameErrorNotifier.SuppressDisconnectNotifications();
        DisconnectNetwork();
        ResetLocalSessionState();

        if (apiClient != null)
            apiClient.ClearToken();

        PlayerPrefs.DeleteKey("JWT_TOKEN");
        PlayerPrefs.DeleteKey("USER_ID");
        PlayerPrefs.DeleteKey("USERNAME");
        PlayerPrefs.DeleteKey("PLAYER_ZONE_ID");
        PlayerPrefs.DeleteKey("SelectedMapId");
        PlayerPrefs.DeleteKey("CONNECT_TO_SERVER");
        PlayerPrefs.Save();

        Time.timeScale = 1f;
        LoginLoadingManager.ShowLoadingStatic("\u0110ang \u0111\u0103ng xu\u1ea5t...");
        yield return null;
        SceneManager.LoadScene("Login");
    }

    private void DisconnectNetwork()
    {
        if (networkManager != null)
            networkManager.Disconnect();

        if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
            NetworkManager.Singleton.Shutdown();
    }

    private static void ResetLocalSessionState()
    {
        GameManager.Instance?.ClearPlayerData();
        ClientSceneController.Instance?.ResetZoneState();
        MapManager.Instance?.ResetRuntimeState();
    }
}
