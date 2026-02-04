using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Script để load player data từ API khi vào GameScene và đảm bảo player data được load trước khi spawn
/// </summary>
public class GameSceneNetworkInitializer : MonoBehaviour
{
    [Header("References")]
    private APIClient apiClient;
    private NetworkManagerCustom networkManager;

    private bool playerDataLoaded = false;
    private bool isInitializing = false;

    private void Start()
    {
        apiClient = APIClient.Instance;
        networkManager = FindObjectOfType<NetworkManagerCustom>();

        // Kiểm tra xem đã có player data chưa
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            Debug.Log("[GameSceneNetworkInitializer] Player data already loaded from previous scene.");
            playerDataLoaded = true;
        }
        else
        {
            // Load player data từ API
            LoadPlayerDataFromAPI();
        }
    }

    /// <summary>
    /// Load player data từ API
    /// </summary>
    private void LoadPlayerDataFromAPI()
    {
        if (isInitializing)
        {
            Debug.LogWarning("[GameSceneNetworkInitializer] Already loading player data...");
            return;
        }

        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        if (userId == 0)
        {
            Debug.LogError("[GameSceneNetworkInitializer] USER_ID not found in PlayerPrefs! Returning to Login scene.");
            SceneManager.LoadScene("Login");
            return;
        }

        isInitializing = true;
        Debug.Log($"[GameSceneNetworkInitializer] Loading player data for user ID: {userId}");

        if (apiClient == null)
        {
            Debug.LogError("[GameSceneNetworkInitializer] APIClient.Instance is null!");
            isInitializing = false;
            return;
        }

        apiClient.LoadPlayerData(
            userId,
            onSuccess: (playerData) =>
            {
                Debug.Log($"[GameSceneNetworkInitializer] Player data loaded successfully: {playerData.character_name} ({playerData.element_type} - {playerData.gender}), Level {playerData.level}");
                
                // Lưu vào GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPlayerData(playerData);
                    playerDataLoaded = true;
                    Debug.Log("[GameSceneNetworkInitializer] Player data saved to GameManager.");
                }
                else
                {
                    Debug.LogError("[GameSceneNetworkInitializer] GameManager.Instance is null! Cannot save player data.");
                }

                isInitializing = false;
            },
            onError: (error) =>
            {
                Debug.LogError($"[GameSceneNetworkInitializer] Failed to load player data: {error}");
                
                // Nếu lỗi 404 (chưa có player), chuyển về SelectElement
                if (error.Contains("404") || error.Contains("not found") || error.Contains("Player không tồn tại"))
                {
                    Debug.Log("[GameSceneNetworkInitializer] Player data not found. Returning to SelectElement scene.");
                    SceneManager.LoadScene("SelectElement");
                }
                else
                {
                    // Lỗi khác: quay về MainMenu
                    Debug.Log("[GameSceneNetworkInitializer] Error loading player data. Returning to MainMenu.");
                    SceneManager.LoadScene("MainMenu");
                }

                isInitializing = false;
            }
        );
    }

    /// <summary>
    /// Kiểm tra xem player data đã được load chưa (để các script khác có thể check)
    /// </summary>
    public bool IsPlayerDataLoaded()
    {
        return playerDataLoaded && GameManager.Instance != null && GameManager.Instance.HasPlayerData();
    }

    /// <summary>
    /// Get player data (nếu đã load)
    /// </summary>
    public PlayerDataResponse GetPlayerData()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            return GameManager.Instance.GetPlayerData();
        }
        return null;
    }
}
