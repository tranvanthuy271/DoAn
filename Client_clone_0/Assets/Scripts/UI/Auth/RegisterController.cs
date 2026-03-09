using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;
using TMPro;

public class RegisterController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;
    public TMP_InputField confirmPasswordInput;
    public Button registerButton;
    public Button backButton;
    public TMP_Text errorText;
    public TMP_Text successText;

    private APIClient apiClient;

    void Start()
    {
        apiClient = APIClient.Instance;
        if (apiClient == null)
        {
            GameObject apiClientObj = new GameObject("APIClient");
            apiClient = apiClientObj.AddComponent<APIClient>();
        }

        registerButton.onClick.AddListener(OnRegisterClicked);
        backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnRegisterClicked()
    {
        string username = usernameInput.text;
        string email = emailInput.text;
        string password = passwordInput.text;
        string confirmPassword = confirmPasswordInput.text;

        // Validate
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || 
            string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
        {
            ShowError("Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        if (password != confirmPassword)
        {
            ShowError("Mật khẩu xác nhận không khớp!");
            return;
        }

        if (!IsValidEmail(email))
        {
            ShowError("Email không hợp lệ!");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự!");
            return;
        }

        registerButton.interactable = false;
        errorText.text = "";
        successText.text = "Đang đăng ký...";

        apiClient.Register(
            username,
            email,
            password,
            onSuccess: (response) =>
            {
                // Debug.Log($"Register successful! User ID: {response.user_id}");
                ShowSuccess("Đăng ký thành công! Đang chuyển đến trang đăng nhập...");
                
                // Chờ 2 giây rồi chuyển về Login
                Invoke(nameof(GoToLogin), 2f);
            },
            onError: (error) =>
            {
                // Debug.LogError($"Register failed: {error}");
                ShowError($"Đăng ký thất bại: {error}");
                registerButton.interactable = true;
            }
        );
    }

    private bool IsValidEmail(string email)
    {
        string pattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
        return Regex.IsMatch(email, pattern);
    }

    private void OnBackClicked()
    {
        SceneManager.LoadScene("Login");
    }

    private void GoToLogin()
    {
        SceneManager.LoadScene("Login");
    }

    private void ShowError(string message)
    {
        errorText.text = message;
        errorText.color = Color.red;
        successText.text = "";
    }

    private void ShowSuccess(string message)
    {
        successText.text = message;
        successText.color = Color.green;
        errorText.text = "";
    }
}
