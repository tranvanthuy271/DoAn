using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class CharacterButtonData
{
    public Button button;
    // 0=Kim, 1=Mộc, 2=Thủy, 3=Hỏa, 4=Thổ, 5=Phong — giới tính tự động lấy từ ElementHelper
    public int elementId;
    public Sprite previewSprite; // legacy fallback nếu chưa cấu hình ElementIconConfig
}

public class SelectElementController : MonoBehaviour
{
    private const string PreviewLayerName = "UICharacter";
    private const string PreviewCameraName = "SelectElementPreviewCamera";
    private const string PreviewRawImageName = "PrefabPreviewRawImage";

    private static readonly string[] PreviewIdleStateNames =
    {
        "Idle", "idle", "ide", "Ide",
        "Idle_01", "Idle_Loop", "IdleNormal",
        "Base Layer.Idle", "locomotion"
    };

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
    public Image previewImage;
    [SerializeField] private PlayerPreviewPrefabConfig previewPrefabConfig;
    [SerializeField] private Vector3 previewWorldPosition = new Vector3(1000f, 0f, 1000f);
    [SerializeField] private Vector3 previewScale = Vector3.one;
    [SerializeField] private float previewRotationY = 180f;
    [SerializeField] private float previewCameraVerticalOffset = -3f;
    [SerializeField] private string[] previewHiddenChildren = { "SkillEffect" };

    [Header("Shared Element Visuals")]
    [SerializeField] private ElementIconConfig elementIconConfig;
    
    private APIClient apiClient;
    private int userId;
    private int selectedElementId = -1;  // -1 = chưa chọn
    private int selectedButtonIndex = -1;
    private bool _isCreateFormVisible;
    private RawImage _previewRawImage;
    private Camera _previewCamera;
    private RenderTexture _previewRenderTexture;
    private GameObject _previewInstance;
    private Animator _previewAnimator;
    private bool _ownsPreviewCamera;

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
        if (characterNameInput != null) characterNameInput.onValueChanged.AddListener(OnCharacterNameInputValueChanged);

        EnsurePreviewRuntime();

        SetCreateFormVisible(false);
        ClearStatusMessage();
        
        UpdateGoButtonState();
        UpdateInstructionText();
    }

    private void LateUpdate()
    {
        if (_previewAnimator == null || !_previewAnimator.enabled)
            return;

        LockPreviewAnimatorToIdle(_previewAnimator);
    }

    private void OnDisable()
    {
        if (_previewCamera != null)
            _previewCamera.enabled = false;
    }

    private void OnEnable()
    {
        if (_previewCamera != null && _previewInstance != null)
            _previewCamera.enabled = true;
    }

    private void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(OnBackButtonClicked);
        if (confirmButton != null) confirmButton.onClick.RemoveListener(OnConfirmButtonClicked);
        if (characterNameInput != null) characterNameInput.onValueChanged.RemoveListener(OnCharacterNameInputValueChanged);

        DestroyPreviewRuntime();
    }
    
    private void InitializeCharacterButtons()
    {
        if (characterButtons == null || characterButtons.Length == 0)
        {
            { /* Lỗi: SelectElementController: Character Buttons array trống */ }
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
        if (buttonIndex < 0 || buttonIndex >= characterButtons.Length)
            return;

        int elementId = characterButtons[buttonIndex].elementId;
        if (TryShowCharacterPrefabPreview(elementId))
            return;

        ShowCharacterSpritePreview(buttonIndex);
    }

    private bool TryShowCharacterPrefabPreview(int elementId)
    {
        if (!ElementHelper.IsValid(elementId) || !EnsurePreviewRuntime())
            return false;

        var prefab = ResolvePreviewPrefab(elementId);
        if (prefab == null)
        {
            HidePrefabPreview();
            return false;
        }

        ShowPrefabPreview(prefab);

        if (previewImage != null)
        {
            previewImage.sprite = null;
            previewImage.enabled = false;
            previewImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (_previewRawImage != null)
            _previewRawImage.color = Color.white;

        return true;
    }

    private void ShowCharacterSpritePreview(int buttonIndex)
    {
        if (previewImage == null)
        {
            { /* Cảnh báo: PreviewImage chưa được gán trong Inspector */ }
            return;
        }

        HidePrefabPreview();

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
                { /* Cảnh báo: Đang fallback sang previewSprite cũ cho hệ {ElementHelper.ToVietnamese(characterButtons[buttonIndex].elementId)} */ }
            }
        }

        if (sprite != null)
        {
            previewImage.sprite = sprite;
            previewImage.enabled = true;
            previewImage.color = Color.white;
            previewImage.preserveAspect = true;
        }
        else
        {
            { /* Cảnh báo: Thiếu avatar preview cho button index {buttonIndex} / hệ {ElementHelper.ToVietnamese(characterButtons[buttonIndex].elementId)} */ }
            previewImage.sprite = null;
            previewImage.enabled = false;
        }
    }

    private PlayerPreviewPrefabConfig ResolvePreviewPrefabConfig()
    {
        if (previewPrefabConfig == null)
            previewPrefabConfig = PlayerPreviewPrefabConfig.Load();

        return previewPrefabConfig;
    }

    private GameObject ResolvePreviewPrefab(int elementId)
    {
        var config = ResolvePreviewPrefabConfig();
        if (config == null)
            return null;

        return config.Resolve(ElementHelper.ToEnglishKey(elementId), ElementHelper.GetGender(elementId));
    }

    private bool EnsurePreviewRuntime()
    {
        if (previewImage == null)
            return false;

        if (_previewRawImage == null)
        {
            var rawTransform = previewImage.transform.Find(PreviewRawImageName);
            if (rawTransform == null)
            {
                var rawObject = new GameObject(PreviewRawImageName, typeof(RectTransform), typeof(RawImage));
                rawObject.transform.SetParent(previewImage.transform, false);
                var rawRect = rawObject.GetComponent<RectTransform>();
                rawRect.anchorMin = Vector2.zero;
                rawRect.anchorMax = Vector2.one;
                rawRect.offsetMin = Vector2.zero;
                rawRect.offsetMax = Vector2.zero;
                _previewRawImage = rawObject.GetComponent<RawImage>();
            }
            else
            {
                _previewRawImage = rawTransform.GetComponent<RawImage>();
                if (_previewRawImage == null)
                    _previewRawImage = rawTransform.gameObject.AddComponent<RawImage>();
            }

            _previewRawImage.raycastTarget = false;
            _previewRawImage.color = new Color(1f, 1f, 1f, 0f);
        }

        if (_previewCamera == null)
        {
            var existingCameraObject = GameObject.Find(PreviewCameraName);
            if (existingCameraObject != null)
                _previewCamera = existingCameraObject.GetComponent<Camera>();

            if (_previewCamera == null)
            {
                var cameraObject = new GameObject(PreviewCameraName, typeof(Camera));
                _previewCamera = cameraObject.GetComponent<Camera>();
                _ownsPreviewCamera = true;
            }

            ConfigurePreviewCamera(_previewCamera);
        }

        return _previewRawImage != null && _previewCamera != null;
    }

    private void ConfigurePreviewCamera(Camera camera)
    {
        if (camera == null)
            return;

        camera.transform.position = new Vector3(previewWorldPosition.x, 1f, previewWorldPosition.z - 2f);
        camera.transform.rotation = Quaternion.identity;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.cullingMask = 0;
        camera.orthographic = true;
        camera.orthographicSize = 1.5f;
        camera.depth = 5f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 20f;
        camera.allowHDR = false;
        camera.allowMSAA = false;
        camera.enabled = false;
    }

    private void EnsurePreviewRenderTexture()
    {
        if (_previewCamera == null || _previewRawImage == null)
            return;

        int width = Mathf.RoundToInt(_previewRawImage.rectTransform.rect.width);
        int height = Mathf.RoundToInt(_previewRawImage.rectTransform.rect.height);

        width = Mathf.Max(width, 220);
        height = Mathf.Max(height, 320);

        if (_previewRenderTexture != null && _previewRenderTexture.width == width && _previewRenderTexture.height == height)
        {
            _previewCamera.targetTexture = _previewRenderTexture;
            _previewRawImage.texture = _previewRenderTexture;
            return;
        }

        if (_previewRenderTexture != null)
        {
            _previewCamera.targetTexture = null;
            _previewRawImage.texture = null;
            _previewRenderTexture.Release();
            Destroy(_previewRenderTexture);
        }

        _previewRenderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 2,
            name = "SelectElementPreviewRT"
        };

        _previewCamera.targetTexture = _previewRenderTexture;
        _previewRawImage.texture = _previewRenderTexture;
    }

    private void ShowPrefabPreview(GameObject prefab)
    {
        if (prefab == null || !EnsurePreviewRuntime())
            return;

        DestroyPreviewInstance();
        EnsurePreviewRenderTexture();

        _previewInstance = Instantiate(prefab);
        _previewInstance.transform.position = previewWorldPosition;
        _previewInstance.transform.localScale = previewScale;
        _previewInstance.transform.rotation = Quaternion.Euler(0f, previewRotationY, 0f);

        DisablePreviewControlScripts(_previewInstance);
        HidePreviewChildren(_previewInstance);

        ForcePreviewIdleAnimation(_previewInstance);
        _previewAnimator = _previewInstance.GetComponentInChildren<Animator>(false);
        if (_previewAnimator == null)
            _previewAnimator = _previewInstance.GetComponentInChildren<Animator>(true);

        int previewLayer = LayerMask.NameToLayer(PreviewLayerName);
        if (previewLayer >= 0)
            SetLayerRecursive(_previewInstance, previewLayer);

        if (_previewCamera != null)
        {
            int targetLayer = previewLayer >= 0 ? previewLayer : _previewInstance.layer;
            _previewCamera.cullingMask = 1 << targetLayer;
            AutoCenterPreviewCamera();
            _previewCamera.enabled = true;
        }
    }

    private void HidePrefabPreview()
    {
        DestroyPreviewInstance();

        if (_previewCamera != null)
            _previewCamera.enabled = false;

        if (_previewRawImage != null)
            _previewRawImage.color = new Color(1f, 1f, 1f, 0f);
    }

    private void DestroyPreviewRuntime()
    {
        HidePrefabPreview();

        if (_previewRenderTexture != null)
        {
            if (_previewCamera != null)
                _previewCamera.targetTexture = null;
            if (_previewRawImage != null)
                _previewRawImage.texture = null;

            _previewRenderTexture.Release();
            Destroy(_previewRenderTexture);
            _previewRenderTexture = null;
        }

        if (_ownsPreviewCamera && _previewCamera != null)
            Destroy(_previewCamera.gameObject);

        _previewCamera = null;
        _previewRawImage = null;
        _ownsPreviewCamera = false;
    }

    private void DestroyPreviewInstance()
    {
        _previewAnimator = null;

        if (_previewInstance == null)
            return;

        Destroy(_previewInstance);
        _previewInstance = null;
    }

    private void AutoCenterPreviewCamera()
    {
        if (_previewCamera == null || _previewInstance == null)
            return;

        var renderers = _previewInstance.GetComponentsInChildren<Renderer>(false);
        if (renderers.Length == 0)
            renderers = _previewInstance.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        var allBounds = renderers[0].bounds;
        foreach (var renderer in renderers)
            allBounds.Encapsulate(renderer.bounds);

        var cameraTransform = _previewCamera.transform;
        cameraTransform.position = new Vector3(cameraTransform.position.x, allBounds.center.y + previewCameraVerticalOffset, cameraTransform.position.z);
        _previewCamera.orthographicSize = Mathf.Max(1.5f, allBounds.extents.y * 2.4f);
    }

    private void HidePreviewChildren(GameObject root)
    {
        if (root == null || previewHiddenChildren == null)
            return;

        foreach (var childName in previewHiddenChildren)
        {
            if (string.IsNullOrEmpty(childName))
                continue;

            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!string.Equals(child.name, childName, System.StringComparison.Ordinal))
                    continue;

                child.gameObject.SetActive(false);
            }
        }
    }

    private void DisablePreviewControlScripts(GameObject root)
    {
        if (root == null)
            return;

        foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!component.enabled)
                continue;

            component.enabled = false;
        }

        foreach (var rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
        {
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }

        foreach (var rigidbody2D in root.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rigidbody2D.bodyType = RigidbodyType2D.Kinematic;
            rigidbody2D.gravityScale = 0f;
            rigidbody2D.velocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }

        foreach (var collider3D in root.GetComponentsInChildren<Collider>(true))
            collider3D.enabled = false;
        foreach (var collider2D in root.GetComponentsInChildren<Collider2D>(true))
            collider2D.enabled = false;
    }

    private static void ForcePreviewIdleAnimation(GameObject root)
    {
        if (root == null)
            return;

        var animator = root.GetComponentInChildren<Animator>(false);
        if (animator == null)
            animator = root.GetComponentInChildren<Animator>(true);
        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.enabled = true;
        animator.speed = 1f;
        LockPreviewAnimatorToIdle(animator);

        foreach (var stateName in PreviewIdleStateNames)
        {
            int hash = Animator.StringToHash(stateName);
            if (!animator.HasState(0, hash))
                continue;

            animator.Play(hash, 0, 0f);
            return;
        }

        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (string.IsNullOrEmpty(clip.name))
                continue;

            int hash = Animator.StringToHash(clip.name);
            if (!animator.HasState(0, hash))
                continue;

            animator.Play(hash, 0, 0f);
            return;
        }

        animator.Rebind();
        animator.Update(0f);
    }

    private static void LockPreviewAnimatorToIdle(Animator animator)
    {
        foreach (var param in animator.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    var name = param.name.ToLowerInvariant();
                    animator.SetBool(param.nameHash, name.Contains("ground") || name.Contains("land") || name.Contains("floor"));
                    break;
                case AnimatorControllerParameterType.Float:
                    animator.SetFloat(param.nameHash, 0f);
                    break;
                case AnimatorControllerParameterType.Int:
                    animator.SetInteger(param.nameHash, 0);
                    break;
                case AnimatorControllerParameterType.Trigger:
                    animator.ResetTrigger(param.nameHash);
                    break;
            }
        }
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
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

    private void OnCharacterNameInputValueChanged(string _)
    {
        OnCharacterNameChanged();
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
                LoginLoadingManager.ShowLoadingStatic("Đang vào game...");
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
        LoginLoadingManager.ShowLoadingStatic("Đang vào game...");
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
