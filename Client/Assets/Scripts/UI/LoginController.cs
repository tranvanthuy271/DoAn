using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class LoginController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public TMP_Text errorText;

    private APIClient apiClient;

    void Start()
    {
        apiClient = APIClient.Instance;
        if (apiClient == null)
        {
            GameObject apiClientObj = new GameObject("APIClient");
            apiClient = apiClientObj.AddComponent<APIClient>();
        }

        loginButton.onClick.AddListener(OnLoginClicked);
        registerButton.onClick.AddListener(OnRegisterClicked);
    }

    private void OnLoginClicked()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        loginButton.interactable = false;
        errorText.text = "Đang đăng nhập...";

        apiClient.Login(
            username,
            password,
            onSuccess: (response) =>
            {
                Debug.Log($"Login successful! User ID from response: {response.user_id}, Token: {response.token}");
                
                // Đảm bảo user_id > 0
                int userId = response.user_id;
                if (userId == 0)
                {
                    Debug.LogError("WARNING: user_id = 0 from response! This should not happen.");
                    // Có thể response không parse đúng, nhưng JWT parsing đã xử lý trong APIClient
                    userId = response.user_id; // Sẽ được fix trong APIClient.ParseUserIdFromJWT
                }
                
                // Lưu user_id và username ngay lập tức
                PlayerPrefs.SetInt("USER_ID", userId);
                PlayerPrefs.SetString("USERNAME", response.username);
                PlayerPrefs.Save(); // Đảm bảo lưu ngay
                
                int savedUserId = PlayerPrefs.GetInt("USER_ID", 0);
                Debug.Log($"Saved USER_ID to PlayerPrefs: {savedUserId}");
                
                if (savedUserId == 0)
                {
                    Debug.LogError("CRITICAL: USER_ID vẫn = 0 sau khi lưu! Kiểm tra lại.");
                }

                // Load player data
                LoadPlayerData(userId);
            },
            onError: (error) =>
            {
                Debug.LogError($"Login failed: {error}");
                ShowError($"Đăng nhập thất bại: {error}");
                loginButton.interactable = true;
            }
        );
    }

    private void LoadPlayerData(int userId)
    {
        apiClient.LoadPlayerData(
            userId,
            onSuccess: (playerData) =>
            {
                Debug.Log($"[LoginController] Player data loaded! Level: {playerData.level}, Map: {playerData.map_id}");
                
                // Đảm bảo GameManager tồn tại trước khi set data
                if (GameManager.Instance == null)
                {
                    GameObject gameManagerObj = new GameObject("GameManager");
                    gameManagerObj.AddComponent<GameManager>();
                }
                
                // Lưu player data vào GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPlayerData(playerData);
                    Debug.Log("[LoginController] Player data saved to GameManager.");
                }
                else
                {
                    Debug.LogError("[LoginController] GameManager.Instance is still null after creation!");
                }
                
                // Chuyển sang GameScene (đã có player data)
                Debug.Log("[LoginController] Player data found. Loading scene 'GameScene'...");
                SceneManager.LoadScene("GameScene");
            },
            onError: (error) =>
            {
                Debug.LogError($"[LoginController] Load player data failed: {error}");
                
                // Nếu chưa có player_data (404), chuyển sang scene chọn hệ để tạo nhân vật
                if (error.Contains("404") || error.Contains("Not Found") || error.Contains("not found") || error.Contains("Player không tồn tại"))
                {
                    Debug.Log("[LoginController] Chưa có player_data, chuyển sang scene SelectElement để tạo nhân vật");
                    // Đảm bảo USER_ID đã được lưu trước khi chuyển scene
                    int savedUserId = PlayerPrefs.GetInt("USER_ID", 0);
                    if (savedUserId == 0)
                    {
                        Debug.LogWarning("[LoginController] USER_ID chưa được lưu, lưu lại từ userId parameter");
                        PlayerPrefs.SetInt("USER_ID", userId);
                        PlayerPrefs.Save();
                    }
                    Debug.Log($"[LoginController] Chuyển sang SelectElement với USER_ID: {PlayerPrefs.GetInt("USER_ID", 0)}");
                    SceneManager.LoadScene("SelectElement");
                }
                else
                {
                    ShowError($"Không thể tải dữ liệu: {error}");
                    loginButton.interactable = true;
                }
            }
        );
    }

    private void OnRegisterClicked()
    {
        SceneManager.LoadScene("Register");
    }

    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
    }
}
