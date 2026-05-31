using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public Button loginButton;
    public Button registerButton;
    public Button togglePasswordButton;
    public TMP_Text togglePasswordLabel;
    public Button accountListButton;
    public GameObject accountListPanel;
    public Button closeAccountListButton;
    public Transform savedAccountContent;
    public GameObject savedAccountRowPrefab;
    public TMP_Text emptySavedAccountText;
    public TMP_Text errorText;

    [Header("Saved Accounts")]
    public bool autoLoginSavedAccount;

    private APIClient apiClient;
    private bool passwordVisible;
    private bool quickFillFromSavedAccount;
    private bool suppressInputChangeEvents;

    private void Start()
    {
        apiClient = APIClient.Instance;
        if (apiClient == null)
        {
            GameObject apiClientObj = new GameObject("APIClient");
            apiClient = apiClientObj.AddComponent<APIClient>();
        }

        if (loginButton != null)
        {
            loginButton.onClick.AddListener(OnLoginClicked);
        }

        if (registerButton != null)
        {
            registerButton.onClick.AddListener(OnRegisterClicked);
        }

        if (togglePasswordButton != null)
        {
            togglePasswordButton.onClick.AddListener(TogglePasswordVisibility);
        }

        if (accountListButton != null)
        {
            accountListButton.onClick.AddListener(ToggleAccountListPanel);
        }

        if (closeAccountListButton != null)
        {
            closeAccountListButton.onClick.AddListener(HideAccountListPanel);
        }

        if (usernameInput != null)
        {
            usernameInput.onValueChanged.AddListener(_ => HandleManualInputChanged());
        }

        if (passwordInput != null)
        {
            passwordInput.onValueChanged.AddListener(_ => HandleManualInputChanged());
        }

        SetPasswordVisible(false);
        SetQuickFillMode(false);
        HideAccountListPanel();
        RefreshSavedAccounts();
    }

    private void OnLoginClicked()
    {
        string username = usernameInput != null ? usernameInput.text.Trim() : string.Empty;
        string password = passwordInput != null ? passwordInput.text : string.Empty;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        if (username.Length < 3 || username.Length > 30)
        {
            ShowError("Tên đăng nhập phải từ 3 đến 30 ký tự!");
            return;
        }

        if (password.Length < 6)
        {
            ShowError("Mật khẩu phải có ít nhất 6 ký tự!");
            return;
        }

        if (loginButton != null)
        {
            loginButton.interactable = false;
        }

        if (errorText != null)
        {
            errorText.text = string.Empty;
        }

        apiClient.Login(
            username,
            password,
            onSuccess: response =>
            {
                int userId = response.user_id;

                PlayerPrefs.SetInt("USER_ID", userId);
                PlayerPrefs.SetString("USERNAME", response.username);
                PlayerPrefs.Save();

                LoginSavedAccountStore.Upsert(username, password);
                RefreshSavedAccounts();
                ShowLoadingPanel(userId);
            },
            onError: error =>
            {
                if (loginButton != null)
                {
                    loginButton.interactable = true;
                }

                ShowError(LoginLoadingManager.BuildUserFacingMessage(error));
            });
    }

    private void ShowLoadingPanel(int userId)
    {
        LoginLoadingManager.EnsureInstance();
        LoginLoadingManager.Instance.BeginLoading(userId, onFailed: message =>
        {
            if (loginButton != null)
            {
                loginButton.interactable = true;
            }

            ShowError(message);
        });
    }

    private void OnRegisterClicked()
    {
        SceneManager.LoadScene("Register");
    }

    private void ShowError(string message)
    {
        if (errorText == null)
        {
            return;
        }

        errorText.text = message;
        errorText.color = Color.red;
    }

    private void TogglePasswordVisibility()
    {
        if (quickFillFromSavedAccount)
        {
            return;
        }

        SetPasswordVisible(!passwordVisible);
    }

    private void SetPasswordVisible(bool visible)
    {
        passwordVisible = visible;

        if (passwordInput != null)
        {
            passwordInput.contentType = visible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            passwordInput.ForceLabelUpdate();
        }

        if (togglePasswordLabel == null && togglePasswordButton != null)
        {
            togglePasswordLabel = togglePasswordButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (togglePasswordLabel != null)
        {
            togglePasswordLabel.text = visible ? "\u1ea8n" : "Hi\u1ec7n";
        }
    }

    private void HandleManualInputChanged()
    {
        if (suppressInputChangeEvents)
        {
            return;
        }

        if (quickFillFromSavedAccount)
        {
            SetQuickFillMode(false);
        }
    }

    private void SetQuickFillMode(bool enabled)
    {
        quickFillFromSavedAccount = enabled;

        if (togglePasswordButton != null)
        {
            togglePasswordButton.gameObject.SetActive(!enabled);
        }

        if (enabled)
        {
            SetPasswordVisible(false);
        }
    }

    private void ToggleAccountListPanel()
    {
        if (accountListPanel != null && accountListPanel.activeSelf)
        {
            HideAccountListPanel();
        }
        else
        {
            ShowAccountListPanel();
        }
    }

    private void ShowAccountListPanel()
    {
        RefreshSavedAccounts();

        if (accountListPanel != null)
        {
            accountListPanel.SetActive(true);
        }
    }

    private void HideAccountListPanel()
    {
        if (accountListPanel != null)
        {
            accountListPanel.SetActive(false);
        }
    }

    private void RefreshSavedAccounts()
    {
        if (savedAccountContent == null || savedAccountRowPrefab == null)
        {
            return;
        }

        for (int i = savedAccountContent.childCount - 1; i >= 0; i--)
        {
            Transform child = savedAccountContent.GetChild(i);
            if (child.gameObject == savedAccountRowPrefab)
            {
                continue;
            }

            Destroy(child.gameObject);
        }

        var accounts = LoginSavedAccountStore.GetAccounts();
        bool hasAccounts = accounts.Count > 0;

        if (emptySavedAccountText != null)
        {
            emptySavedAccountText.gameObject.SetActive(!hasAccounts);
        }

        savedAccountRowPrefab.SetActive(false);

        foreach (LoginSavedAccountStore.AccountRecord account in accounts)
        {
            GameObject row = Instantiate(savedAccountRowPrefab, savedAccountContent);
            row.SetActive(true);

            LoginSavedAccountRow rowUi = row.GetComponent<LoginSavedAccountRow>();
            if (rowUi != null)
            {
                rowUi.Bind(account, SelectSavedAccount, DeleteSavedAccount);
            }
        }
    }

    private void SelectSavedAccount(LoginSavedAccountStore.AccountRecord account)
    {
        if (account == null)
        {
            return;
        }

        string password = account.GetPassword();
        if (string.IsNullOrEmpty(account.username) || string.IsNullOrEmpty(password))
        {
            ShowError("T\u00e0i kho\u1ea3n \u0111\u00e3 l\u01b0u b\u1ecb thi\u1ebfu m\u1eadt kh\u1ea9u.");
            return;
        }

        suppressInputChangeEvents = true;
        if (usernameInput != null)
        {
            usernameInput.text = account.username;
        }

        if (passwordInput != null)
        {
            passwordInput.text = password;
        }

        suppressInputChangeEvents = false;

        SetQuickFillMode(true);
        HideAccountListPanel();

        if (autoLoginSavedAccount)
        {
            OnLoginClicked();
        }
    }

    private void DeleteSavedAccount(LoginSavedAccountStore.AccountRecord account)
    {
        if (account == null)
        {
            return;
        }

        LoginSavedAccountStore.Remove(account.username);

        if (usernameInput != null &&
            string.Equals(usernameInput.text, account.username, System.StringComparison.OrdinalIgnoreCase))
        {
            suppressInputChangeEvents = true;
            usernameInput.text = string.Empty;
            if (passwordInput != null)
            {
                passwordInput.text = string.Empty;
            }

            suppressInputChangeEvents = false;
            SetQuickFillMode(false);
        }

        RefreshSavedAccounts();
    }
}
