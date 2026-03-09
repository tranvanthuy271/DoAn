using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class CharacterButtonData
{
    public Button button;
    public string elementType;
    public string gender;
    public Sprite previewSprite; // Sprite để hiển thị preview (thay vì prefab)
}

public class SelectElementController : MonoBehaviour
{
    [Header("Character Buttons (9 buttons)")]
    public CharacterButtonData[] characterButtons = new CharacterButtonData[9];
    // Thứ tự: [0]Metal-Male, [1]Metal-Female, [2]Wood-Male, [3]Wood-Female, 
    //          [4]Water-Male, [5]Water-Female, [6]Fire-Male, [7]Fire-Female, [8]Earth-Male
    
    [Header("UI References")]
    public TMP_InputField characterNameInput;
    public TMP_Text errorText;
    public TMP_Text instructionText;
    public Button confirmButton;
    public Button backButton;
    public Button goButton;
    
    [Header("Character Preview")]
    public Image previewImage; // Image component để hiển thị preview (thay vì spawn prefab)
    
    private APIClient apiClient;
    private int userId;
    private string selectedElement = "";
    private string selectedGender = "Male";
    private int selectedButtonIndex = -1;

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

        // Khởi tạo 9 character buttons
        InitializeCharacterButtons();
        
        // Navigation buttons
        backButton.onClick.AddListener(OnBackButtonClicked);
        goButton.onClick.AddListener(OnGoButtonClicked);
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        
        // Ban đầu disable go button
        goButton.interactable = false;
        UpdateInstructionText();
    }
    
    private void InitializeCharacterButtons()
    {
        // Đảm bảo có đủ 9 button
        if (characterButtons == null || characterButtons.Length != 9)
        {
            // Debug.LogError("SelectElementController: Cần đúng 9 character buttons!");
            return;
        }
        
        // Gán sự kiện cho từng button
        for (int i = 0; i < characterButtons.Length; i++)
        {
            int index = i; // Capture index để dùng trong lambda
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
        selectedElement = characterButtons[buttonIndex].elementType;
        selectedGender = characterButtons[buttonIndex].gender;
        
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
            // Debug.LogWarning("PreviewImage is null! Please assign an Image component in Inspector.");
            return;
        }
        
        // Lấy sprite trực tiếp từ CharacterButtonData
        if (buttonIndex >= 0 && buttonIndex < characterButtons.Length)
        {
            Sprite sprite = characterButtons[buttonIndex].previewSprite;
            
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
                // Debug.LogWarning($"Preview sprite is null for button index {buttonIndex}. Please assign a sprite in the Character Buttons array.");
                previewImage.sprite = null;
                previewImage.enabled = false;
            }
        }
    }
    
    private void UpdateGoButtonState()
    {
        // Enable Go button khi đã có đủ: nhân vật được chọn và tên nhân vật
        bool canGo = selectedButtonIndex >= 0 && 
                     !string.IsNullOrEmpty(selectedElement) && 
                     !string.IsNullOrWhiteSpace(characterNameInput.text) &&
                     characterNameInput.text.Length >= 3;
        goButton.interactable = canGo;
    }

    private void UpdateInstructionText()
    {
        if (selectedButtonIndex < 0 || string.IsNullOrEmpty(selectedElement))
        {
            instructionText.text = "Chọn nhân vật của bạn";
        }
        else
        {
            string genderText = selectedGender == "Male" ? "Nam" : "Nữ";
            instructionText.text = $"Đã chọn: {selectedElement} - {genderText}";
        }
    }
    
    // Gọi khi input tên thay đổi để update Go button
    public void OnCharacterNameChanged()
    {
        UpdateGoButtonState();
    }

    public void OnConfirmButtonClicked()
    {
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
        if (selectedButtonIndex < 0 || string.IsNullOrEmpty(selectedElement))
        {
            ShowError("Vui lòng chọn nhân vật trước!");
            return;
        }
        
        if (string.IsNullOrEmpty(selectedGender))
        {
            ShowError("Lỗi: Giới tính chưa được chọn!");
            return;
        }
        
        if (characterNameInput == null)
        {
            ShowError("Lỗi: Input field tên nhân vật không tồn tại!");
            return;
        }
        
        string characterName = characterNameInput.text.Trim();
        if (string.IsNullOrWhiteSpace(characterName))
        {
            ShowError("Vui lòng nhập tên nhân vật!");
            return;
        }
        
        if (characterName.Length < 3 || characterName.Length > 20)
        {
            ShowError("Tên nhân vật phải có từ 3 đến 20 ký tự!");
            return;
        }

        // Disable all buttons
        SetCharacterButtonsInteractable(false);
        if (confirmButton != null) confirmButton.interactable = false;
        if (goButton != null) goButton.interactable = false;
        if (characterNameInput != null) characterNameInput.interactable = false;
        
        string genderText = selectedGender == "Male" ? "Nam" : "Nữ";
        if (errorText != null)
        {
            errorText.text = $"Đang tạo nhân vật {characterName} - {selectedElement} - {genderText}...";
        }

        apiClient.CreatePlayer(
            selectedElement,
            selectedGender,
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
                
                // Enable Go button để người chơi có thể vào game
                if (goButton != null)
                {
                    goButton.interactable = true;
                    // Debug.Log("Go button enabled!");
                }
                else
                {
                    // Debug.LogError("Go button is null!");
                }
                
                if (errorText != null)
                {
                    errorText.text = $"Tạo nhân vật thành công! Nhấn 'Go' để vào game.";
                    errorText.color = Color.green;
                }
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
        errorText.text = message;
        errorText.color = Color.red;
    }
}
