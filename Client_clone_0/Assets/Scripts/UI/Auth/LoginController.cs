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
                // Debug.Log($"Login successful! User ID from response: {response.user_id}, Token: {response.token}");
                
                // Đảm bảo user_id > 0
                int userId = response.user_id;
                if (userId == 0)
                {
                    // Debug.LogError("WARNING: user_id = 0 from response! This should not happen.");
                    // Có thể response không parse đúng, nhưng JWT parsing đã xử lý trong APIClient
                    userId = response.user_id; // Sẽ được fix trong APIClient.ParseUserIdFromJWT
                }
                
                // Lưu user_id và username ngay lập tức
                PlayerPrefs.SetInt("USER_ID", userId);
                PlayerPrefs.SetString("USERNAME", response.username);
                PlayerPrefs.Save(); // Đảm bảo lưu ngay
                
                int savedUserId = PlayerPrefs.GetInt("USER_ID", 0);
                // Debug.Log($"Saved USER_ID to PlayerPrefs: {savedUserId}");
                
                if (savedUserId == 0)
                {
                    // Debug.LogError("CRITICAL: USER_ID vẫn = 0 sau khi lưu! Kiểm tra lại.");
                }

                // Show loading overlay → loads player data → auto-transitions to GameScene
                ShowLoadingPanel(userId);
            },
            onError: (error) =>
            {
                // Hiện ErrorNotifyPanel thay vì chỉ text đỏ
                loginButton.interactable = true;
                LoginLoadingManager.ShowErrorStatic(error, onDismiss: () =>
                {
                    loginButton.interactable = true;
                });
            }
        );
    }

    /// <summary>
    /// Find or create LoginLoadingManager and start the loading flow.
    /// </summary>
    private void ShowLoadingPanel(int userId)
    {
        if (LoginLoadingManager.Instance == null)
        {
            var go = new GameObject("[LoginLoadingManager]");
            go.AddComponent<LoginLoadingManager>();
        }
        LoginLoadingManager.Instance.BeginLoading(userId);
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

