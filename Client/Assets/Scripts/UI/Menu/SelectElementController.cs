using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class CharacterButtonData
{
    public Button button;
    /// <summary>0=Kim, 1=Mộc, 2=Thủy, 3=Hỏa, 4=Thổ, 5=Phong — giới tính tự động lấy từ ElementHelper</summary>
    public int elementId;
    public Sprite previewSprite; // legacy fallback nếu chưa cấu hình ElementIconConfig
}

public class SelectElementController : MonoBehaviour
{
    [Header("Character Buttons (6 hệ, mỗi hệ 1 button – gender config riêng)")]
    public CharacterButtonData[] characterButtons = new CharacterButtonData[6];
    // Gán đúng elementId cho từng preview trong Inspector; thứ tự mảng có thể theo layout scene.
    
    [Header("UI References")]
    public TMP_InputField characterNameInput;
    public TMP_Text errorText;
    public TMP_Text instructionText;
    public Button confirmButton;
    public Button backButton;
    public Button goButton;
    
    [Header("Character Preview")]
    public Image previewImage; // Image component để hiển thị preview (thay vì spawn prefab)

    [Header("Shared Element Visuals")]
    [SerializeField] private ElementIconConfig elementIconConfig;
    
    private APIClient apiClient;
    private int userId;
    private int selectedElementId = -1;  // -1 = chưa chọn
    private int selectedButtonIndex = -1;
    private bool _isCreateFormVisible;

    void Start()
    {
        apiClient = APIClient.Instance;
        userId = PlayerPrefs.GetInt("USER_ID", 0);

        if (userId == 0)
        {
            // Debug.LogWarning("User ID not found in PlayerPrefs! Trying to get from JWT token...");
            
            // Thử lấy user_id từ JWT token nếu có
            string token = PlayerPrefs.GetString("JWT_TOKEN", "");
            if (!string.IsNullOrEmpty(token))
            {
                // Parse JWT để lấy user_id (tạm thời, có thể cải thiện sau)
                // Hoặc gọi API để lấy user info
                // Debug.LogWarning("Có JWT token nhưng không có USER_ID. Quay lại Login để đăng nhập lại.");
            }
            
            SceneManager.LoadScene("Login");
            return;
        }
        
        // Debug.Log($"SelectElementController: User ID = {userId}");

        // Khởi tạo character buttons
        InitializeCharacterButtons();
        
        // Navigation buttons
        if (backButton != null) backButton.onClick.AddListener(OnBackButtonClicked);
        if (goButton != null) goButton.gameObject.SetActive(false); // Ẩn nút GO
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        SetCreateFormVisible(false);
        ClearStatusMessage();
        
        UpdateGoButtonState();
        UpdateInstructionText();
    }
    
    private void InitializeCharacterButtons()
    {
        if (characterButtons == null || characterButtons.Length == 0)
        {
            Debug.LogError("SelectElementController: Character Buttons array trống!");
            return;
        }

        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i;
            if (characterButtons[i].button != null)
            {
                characterButtons[i].button.onClick.AddListener(() => OnCharacterButtonClicked(index));
            }
        }
    }

    private void OnCharacterButtonClicked(int buttonIndex)
    {
        if (buttonIndex < 0 || buttonIndex >= characterButtons.Length)
        {
            // Debug.LogError($"Invalid button index: {buttonIndex}");
            return;
        }
        
        // Cập nhật lựa chọn
        selectedButtonIndex = buttonIndex;
        selectedElementId = characterButtons[buttonIndex].elementId;
        
        // Highlight button được chọn
        UpdateButtonVisuals();
        
        // Hiển thị prefab preview
        ShowCharacterPreview(buttonIndex);
        
        // Enable Go button nếu đã có đủ thông tin
        UpdateGoButtonState();
        UpdateInstructionText();
    }
    
    private void UpdateButtonVisuals()
    {
        // Reset tất cả buttons về màu bình thường
        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i].button != null)
            {
                ColorBlock colors = characterButtons[i].button.colors;
                colors.normalColor = Color.white;
                characterButtons[i].button.colors = colors;
            }
        }
        
        // Highlight button được chọn
        if (selectedButtonIndex >= 0 && selectedButtonIndex < characterButtons.Length)
        {
            if (characterButtons[selectedButtonIndex].button != null)
            {
                ColorBlock colors = characterButtons[selectedButtonIndex].button.colors;
                colors.normalColor = Color.green;
                characterButtons[selectedButtonIndex].button.colors = colors;
            }
        }
    }
    
    private void ShowCharacterPreview(int buttonIndex)
    {
        if (previewImage == null)
        {
            Debug.LogWarning("[SelectElementController] PreviewImage chưa được gán trong Inspector.", this);
            return;
        }
        
        // Lấy sprite trực tiếp từ CharacterButtonData
        if (buttonIndex >= 0 && buttonIndex < characterButtons.Length)
        {
            Sprite sprite = null;
            var config = ResolveElementIconConfig();
            if (config != null)
            {
                sprite = config.GetSpriteOrLog(
                    characterButtons[buttonIndex].elementId,
                    ElementIconConfig.SpriteKind.Avatar,
                    this,
                    nameof(SelectElementController));
            }

            if (sprite == null)
            {
                sprite = characterButtons[buttonIndex].previewSprite;
                if (sprite != null)
                {
                    Debug.LogWarning(
                        $"[SelectElementController] Đang fallback sang previewSprite cũ cho hệ {ElementHelper.ToVietnamese(characterButtons[buttonIndex].elementId)}.",
                        this);
                }
            }
            
            // Set sprite vào preview Image
            if (sprite != null)
            {
                previewImage.sprite = sprite;
                previewImage.enabled = true;
                previewImage.color = Color.white;
                previewImage.preserveAspect = true; // Giữ nguyên tỷ lệ
                
                // Debug.Log($"Preview image set: {sprite.name} for {characterButtons[buttonIndex].elementType} - {characterButtons[buttonIndex].gender}");
            }
            else
            {
                Debug.LogWarning(
                    $"[SelectElementController] Thiếu avatar preview cho button index {buttonIndex} / hệ {ElementHelper.ToVietnamese(characterButtons[buttonIndex].elementId)}.",
                    this);
                previewImage.sprite = null;
                previewImage.enabled = false;
            }
        }
    }

    private ElementIconConfig ResolveElementIconConfig()
    {
        if (elementIconConfig == null)
            elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(SelectElementController));

        return elementIconConfig;
    }
    
    private void UpdateGoButtonState()
    {
        bool canConfirm = selectedButtonIndex >= 0 && ElementHelper.IsValid(selectedElementId);

        if (_isCreateFormVisible && characterNameInput != null)
        {
            canConfirm &= (!string.IsNullOrWhiteSpace(characterNameInput.text) && characterNameInput.text.Length >= 3);
        }

        if (confirmButton != null)
        {
            confirmButton.interactable = canConfirm;
        }
    }

    private void UpdateInstructionText()
    {
        if (instructionText == null) return;

        if (selectedButtonIndex < 0 || !ElementHelper.IsValid(selectedElementId))
        {
            instructionText.text = "Chọn nhân vật của bạn";
        }
        else if (!_isCreateFormVisible)
        {
            string elementVN     = ElementHelper.ToVietnamese(selectedElementId);
            string gender        = ElementHelper.GetGender(selectedElementId);
            string genderDisplay = gender == "Male" ? "Nam" : "Nữ";
            instructionText.text = $"Hệ: {elementVN} | Giới tính: {genderDisplay} | Bấm Tạo mới để nhập tên";
        }
        else
        {
            string elementVN     = ElementHelper.ToVietnamese(selectedElementId);
            string gender        = ElementHelper.GetGender(selectedElementId);
            string genderDisplay = gender == "Male" ? "Nam" : "Nữ";
            instructionText.text = $"Hệ: {elementVN} | Giới tính: {genderDisplay} | Nhập tên rồi bấm Xác nhận";
        }
    }
    
    // Gọi khi input tên thay đổi để update Go button
    public void OnCharacterNameChanged()
    {
        UpdateGoButtonState();
    }

    public void OnConfirmButtonClicked()
    {
        if (!_isCreateFormVisible)
        {
            if (selectedButtonIndex < 0 || !ElementHelper.IsValid(selectedElementId))
            {
                ShowError("Vui lòng chọn hệ trước!");
                return;
            }

            SetCreateFormVisible(true, true);
            ClearStatusMessage();
            UpdateGoButtonState();
            UpdateInstructionText();
            return;
        }

        // Kiểm tra apiClient
        if (apiClient == null)
        {
            // Debug.LogError("APIClient is null! Trying to get instance...");
            apiClient = APIClient.Instance;
            if (apiClient == null)
            {
                ShowError("Lỗi: Không thể kết nối đến API. Vui lòng thử lại.");
                return;
            }
        }
        
        // Validate input
        if (selectedButtonIndex < 0 || !ElementHelper.IsValid(selectedElementId))
        {
            ShowError("Vui lòng chọn hệ trước!");
            return;
        }
        
        string characterName = PlayerPrefs.GetString("USERNAME", "Player" + UnityEngine.Random.Range(1000, 9999));
        if (characterNameInput != null && characterNameInput.gameObject.activeInHierarchy)
        {
            characterName = characterNameInput.text.Trim();
            if (string.IsNullOrWhiteSpace(characterName) || characterName.Length < 3 || characterName.Length > 20)
            {
                ShowError("Tên nhân vật phải có từ 3 đến 20 ký tự!");
                return;
            }
        }

        // Disable all buttons
        SetCharacterButtonsInteractable(false);
        if (confirmButton != null) confirmButton.interactable = false;
        if (goButton != null) goButton.interactable = false;
        if (characterNameInput != null) characterNameInput.interactable = false;
        
        string elementKey = ElementHelper.ToEnglishKey(selectedElementId);
        string elementVN  = ElementHelper.ToVietnamese(selectedElementId);
        string gender     = ElementHelper.GetGender(selectedElementId);

        if (errorText != null)
        {
            errorText.text = $"Đang tạo nhân vật hệ {elementVN}...";
            errorText.enabled = true;
        }

        apiClient.CreatePlayer(
            elementKey,
            gender,
            characterName,
            onSuccess: (playerData) =>
            {
                if (playerData == null)
                {
                    // Debug.LogError("PlayerDataResponse is null!");
                    ShowError("Lỗi: Không nhận được dữ liệu từ server.");
                    SetCharacterButtonsInteractable(true);
                    if (confirmButton != null) confirmButton.interactable = true;
                    if (characterNameInput != null) characterNameInput.interactable = true;
                    return;
                }
                
                // Debug.Log($"Player created! Name: '{playerData.character_name}', Level: {playerData.level}, Element: '{playerData.element_type}', Gender: '{playerData.gender}'");
                
                // Kiểm tra GameManager
                if (GameManager.Instance == null)
                {
                    // Debug.LogWarning("GameManager.Instance is null! Creating GameManager...");
                    GameObject gameManagerObj = new GameObject("GameManager");
                    gameManagerObj.AddComponent<GameManager>();
                }
                
                // Lưu player data
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SetPlayerData(playerData);
                }
                else
                {
                    // Debug.LogError("GameManager.Instance is still null after creation!");
                }
                
                if (errorText != null)
                {
                    errorText.text = $"Tạo nhân vật thành công! Đang vào game...";
                    errorText.color = Color.green;
                    errorText.enabled = true;
                }

                // Chuyển sang GameScene
                SceneManager.LoadScene("GameScene");
            },
            onError: (error) =>
            {
                // Debug.LogError($"Create player failed: {error}");
                ShowError($"Tạo nhân vật thất bại: {error}");
                SetCharacterButtonsInteractable(true);
                if (confirmButton != null) confirmButton.interactable = true;
                if (characterNameInput != null) characterNameInput.interactable = true;
                UpdateGoButtonState();
            }
        );
    }
    
    public void OnGoButtonClicked()
    {
        // Kiểm tra xem đã tạo nhân vật chưa (player data đã được lưu trong GameManager sau khi CreatePlayer)
        if (GameManager.Instance == null || !GameManager.Instance.HasPlayerData())
        {
            ShowError("Vui lòng tạo nhân vật trước!");
            return;
        }
        
        goButton.interactable = false;
        errorText.text = "Đang vào game...";
        
        // Debug.Log("[SelectElementController] Player data already created. Loading scene 'GameScene'...");
        
        // Chuyển sang GameScene (logic connect sẽ được xử lý trong GameScene)
        SceneManager.LoadScene("GameScene");
    }

    public void OnBackButtonClicked()
    {
        // Quay lại scene Login
        SceneManager.LoadScene("Login");
    }

    private void SetCharacterButtonsInteractable(bool interactable)
    {
        for (int i = 0; i < characterButtons.Length; i++)
        {
            if (characterButtons[i].button != null)
            {
                characterButtons[i].button.interactable = interactable;
            }
        }
    }

    private void ShowError(string message)
    {
        if (errorText == null)
            return;

        errorText.text = message;
        errorText.color = Color.red;
        errorText.enabled = !string.IsNullOrWhiteSpace(message);
    }

    private void ClearStatusMessage()
    {
        if (errorText == null)
            return;

        errorText.text = string.Empty;
        errorText.enabled = false;
    }

    private void SetCreateFormVisible(bool visible, bool focusInput = false)
    {
        _isCreateFormVisible = visible;

        if (characterNameInput != null)
        {
            if (!visible)
                characterNameInput.text = string.Empty;

            characterNameInput.gameObject.SetActive(visible);
            characterNameInput.interactable = visible;

            if (visible && focusInput)
            {
                characterNameInput.Select();
                characterNameInput.ActivateInputField();
            }
        }

        UpdateConfirmButtonLabel(visible ? "Xác nhận" : "Tạo mới");
    }

    private void UpdateConfirmButtonLabel(string label)
    {
        if (confirmButton == null)
            return;

        var tmp = confirmButton.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            tmp.text = label;
    }
}
