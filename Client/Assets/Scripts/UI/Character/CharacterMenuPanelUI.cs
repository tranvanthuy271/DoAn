using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// Panel nhân vật mini – hiển thị avatar, tên tài khoản, tên nhân vật,
// cấp độ + % EXP tới cấp tiếp theo và các nút chức năng.
// Cách dùng: đặt trong Canvas, gán tất cả SerializedField, gán
// partyPanel tới PartyPanelUI để nút "Quan hệ" mở đúng panel.
public class CharacterMenuPanelUI : MonoBehaviour
{
    private const string GameplayBlockSource = "CharacterMenuPanelUI";
    private const string LogPrefix = "[CharacterMenuPanelUI]";

    // Character info
    [Header("Character Info")]
    [SerializeField] private Image        avatarImage;
    [SerializeField] private TMP_Text     accountNameText;
    [SerializeField] private TMP_Text     characterNameText;
    [SerializeField] private TMP_Text     levelText;        // "Cấp: 54  (62%)"
    [SerializeField] private Slider       expSlider;        // 0 → 1
    [SerializeField] private TMP_Text     expDetailText;    // "12345 / 20000 EXP"

    [Header("Visual Config")]
    [Tooltip("Asset chung chứa icon hệ và avatar theo hệ. Nếu bỏ trống sẽ thử load từ Resources/ScriptableObjects/ElementIconConfig.")]
    [SerializeField] private ElementIconConfig elementIconConfig;

    // Navigation buttons
    [Header("Navigation Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button questButton;       // Nhiệm vụ
    [SerializeField] private Button relationButton;    // Quan hệ → mở PartyPanel
    [SerializeField] private Button clanButton;        // Gia tộc (bỏ qua)
    [SerializeField] private Button settingButton;     // Setting (placeholder)
    [SerializeField] private Button changeCharButton;  // Đổi nhân vật → về login
    [SerializeField] private Button quitButton;        // Thoát game

    // Linked panels
    [Header("Linked Panels")]
    [Tooltip("Panel tổ đội (PartyPanelUI)")]
    [FormerlySerializedAs("socialPanel")]
    [SerializeField] private GameObject partyPanel;

    [Tooltip("Dùng khi partyPanel chưa được gán hoặc đang trỏ tới prefab asset.")]
    [FormerlySerializedAs("socialPanelResourcesPath")]
    [SerializeField] private string partyPanelResourcesPath = "Prefabs/UI/PartyPanel";

    [Tooltip("Panel nhiệm vụ (tuỳ chọn)")]
    [SerializeField] private GameObject questPanel;

    // Login scene name (có thể override trong Inspector)
    [Header("Scene Names")]
    [SerializeField] private string loginSceneName = "Login";

    // Xử lý nội bộ phục vụ các hàm public.
    private PlayerDataResponse _cachedData;

    #region Unity lifecycle

    private void Awake()
    {
        UIPanelManager.Register(gameObject, Close);
        closeButton       ?.onClick.AddListener(Close);
        questButton       ?.onClick.AddListener(OnQuestClicked);
        relationButton    ?.onClick.AddListener(OnRelationClicked);
        clanButton        ?.onClick.AddListener(OnClanClicked);
        settingButton     ?.onClick.AddListener(OnSettingClicked);
        changeCharButton  ?.onClick.AddListener(OnChangeCharClicked);
        quitButton        ?.onClick.AddListener(OnQuitClicked);
    }

    private void OnEnable()
    {
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, true);
        InputManager.Instance?.CancelAutoMove();
        RefreshData();
    }

    private void OnDisable()
    {
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, false);
    }

    #endregion

    #region Public API

    // Mở panel và làm mới dữ liệu hiển thị.
    public void Open()
    {
        UIPanelManager.CloseOthers(gameObject);
        gameObject.SetActive(true);
        UIPanelManager.NotifyOpened(gameObject);
    }

    // Đóng panel.
    public void Close()
    {
        gameObject.SetActive(false);
        UIPanelManager.NotifyClosed(gameObject);
    }

    #endregion

    #region Data refresh

    private void RefreshData()
    {
        // Account name từ PlayerPrefs
        string accountName = PlayerPrefs.GetString("USERNAME", "---");
        if (accountNameText != null)
            accountNameText.text = accountName;

        // Character data từ GameManager
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            _cachedData = GameManager.Instance.GetPlayerData();
            ApplyPlayerData(_cachedData);
        }
        else
        {
            { /* Cảnh báo: {LogPrefix} Chưa có PlayerData trong GameManager, hiển thị dữ liệu mặc định */ }
            ShowNoData();
        }
    }

    private void ApplyPlayerData(PlayerDataResponse data)
    {
        if (characterNameText != null)
            characterNameText.text = data.character_name ?? "---";

        int level   = data.level;
        int curExp  = data.experience - data.exp_at_current_level;
        int maxExp  = data.exp_required_for_next_level - data.exp_at_current_level;
        if (maxExp <= 0) maxExp = 1; // tránh chia 0

        float pct = Mathf.Clamp01((float)curExp / maxExp);
        int   pctInt = Mathf.RoundToInt(pct * 100f);

        if (levelText != null)
            levelText.text = $"Cấp: {level}  ({pctInt}%)";

        if (expSlider != null)
        {
            expSlider.minValue = 0f;
            expSlider.maxValue = 1f;
            expSlider.value    = pct;
        }

        if (expDetailText != null)
            expDetailText.text = $"{curExp:N0} / {maxExp:N0} EXP";

        ApplyAvatar(data);
    }

    private void ShowNoData()
    {
        if (characterNameText != null) characterNameText.text = "---";
        if (levelText       != null) levelText.text        = "Cấp: ---";
        if (expDetailText   != null) expDetailText.text    = "--- EXP";
        if (expSlider       != null) expSlider.value       = 0f;
    }

    #endregion

    #region Button handlers

    private void OnQuestClicked()
    {
        if (questPanel != null)
        {
            Close();
            questPanel.SetActive(true);
        }
        // nếu chưa có questPanel thì chưa làm gì
    }

    private void OnRelationClicked()
    {
        var partyManager = PartyManager.EnsureInstance();
        if (partyManager == null)
        {
            { /* Lỗi: {LogPrefix} Không thể mở Quan hệ vì PartyManager không khởi tạo được */ }
            return;
        }

        var resolvedPartyPanel = ResolvePartyPanel();
        if (resolvedPartyPanel == null)
        {
            { /* Lỗi: {LogPrefix} Không thể mở Quan hệ vì không resolve được PartyPanel */ }
            return;
        }

        Close();
        resolvedPartyPanel.SetActive(true);

        var partyPanelUi = resolvedPartyPanel.GetComponent<PartyPanelUI>();
        if (partyPanelUi != null)
        {
            partyPanelUi.SelectTab(0);
            return;
        }

        { /* Cảnh báo: {LogPrefix} PartyPanel không có component PartyPanelUI, fallback SetActive(true) */ }
    }

    private void OnClanClicked()
    {
        // Gia tộc – bỏ qua, chưa triển khai
        { /* Gia tộc chưa triển khai */ }
    }

    private void OnSettingClicked()
    {
        // Setting – placeholder, triển khai sau
        { /* Setting chưa triển khai */ }
    }

    private void OnChangeCharClicked()
    {
        StartCoroutine(ChangeCharRoutine());
    }

    private System.Collections.IEnumerator ChangeCharRoutine()
    {
        // Đóng block input trước
        InputManager.Instance?.SetGameplayInputBlocked(GameplayBlockSource, false);

        // Chặn popup mất kết nối khi chủ động ngắt kết nối mạng
        GameErrorNotifier.SuppressDisconnectNotifications(10f);

        // Hiển thị panel xoay "Đang đăng xuất..."
        LoginLoadingManager.ShowLoadingStatic("\u0110ang \u0111\u0103ng xu\u1ea5t...");

        // Xoá session (giữ USERNAME để autofill login)
        PlayerPrefs.DeleteKey("AUTH_TOKEN");
        AuthHelper.ClearToken();
        PlayerPrefs.DeleteKey("USER_ID");
        PlayerPrefs.DeleteKey("PLAYER_ID");
        PlayerPrefs.DeleteKey("PLAYER_ZONE_ID");
        PlayerPrefs.DeleteKey("SelectedMapId");
        PlayerPrefs.DeleteKey("CONNECT_TO_SERVER");
        PlayerPrefs.Save();

        // Xoá dữ liệu player và zone
        GameManager.Instance?.ClearPlayerData();
        ClientSceneController.Instance?.ResetZoneState();
        if (MapManager.Instance != null)
        {
            MapManager.Instance.ResetRuntimeState();
        }

        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
            nm.Shutdown();

        // Chờ 3 giây để đảm bảo dữ liệu nhân vật được lưu trữ thành công lên máy chủ trước khi thoát hoàn toàn
        yield return new WaitForSecondsRealtime(3f);

        string targetScene = string.IsNullOrEmpty(loginSceneName) || loginSceneName == "LoginScene" ? "Login" : loginSceneName;
        SceneManager.LoadScene(targetScene);
    }

    private void OnQuitClicked()
    {
        { /* Thoát game */ }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    private void ApplyAvatar(PlayerDataResponse data)
    {
        if (avatarImage == null)
        {
            { /* Cảnh báo: {LogPrefix} Chưa gán AvatarImage trong Inspector */ }
            return;
        }

        var resolvedConfig = ResolveElementIconConfig();
        if (resolvedConfig == null)
            return;

        // Fallback: element_type → ChatManager.CurrentClassId
        string resolvedType = !string.IsNullOrWhiteSpace(data?.element_type)
            ? data.element_type
            : ChatManager.Instance?.CurrentClassId ?? string.Empty;
        int elementId = ElementHelper.ToId(resolvedType);
        if (!ElementHelper.IsValid(elementId))
        {
            { /* Cảnh báo: {LogPrefix} Không resolve được element_type='{data?.element_type ?? string.Empty}' fallback='{resolvedType}' để hiển thị avatar cho '{data?.character_name ?? */ }
            return;
        }

        var avatarSprite = resolvedConfig.GetSpriteOrLog(elementId, ElementIconConfig.SpriteKind.Avatar, this, nameof(CharacterMenuPanelUI));
        if (avatarSprite == null)
            return;

        avatarImage.sprite = avatarSprite;
        avatarImage.enabled = true;
        avatarImage.preserveAspect = true;
        avatarImage.color = Color.white;
    }

    private ElementIconConfig ResolveElementIconConfig()
    {
        if (elementIconConfig == null)
            elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(CharacterMenuPanelUI));

        return elementIconConfig;
    }

    private GameObject ResolvePartyPanel()
    {
        if (IsSceneObjectAlive(partyPanel))
            return partyPanel;

        if (partyPanel != null && TryInstantiateUiPrefab(partyPanel, out var instantiatedFromField))
        {
            partyPanel = instantiatedFromField;
            { /* {LogPrefix} Đã instantiate PartyPanel từ prefab reference gán trong CharacterMenuPanel */ }
            return partyPanel;
        }

        var existingPartyPanel = FindObjectOfType<PartyPanelUI>(includeInactive: true);
        if (existingPartyPanel != null)
        {
            partyPanel = existingPartyPanel.gameObject;
            { /* {LogPrefix} Đã resolve PartyPanelUI đang có sẵn trong scene */ }
            return partyPanel;
        }

        var partyPanelPrefab = Resources.Load<GameObject>(partyPanelResourcesPath);
        if (partyPanelPrefab == null)
        {
            { /* Lỗi: {LogPrefix} Không tìm thấy prefab PartyPanel tại Resources/{partyPanelResourcesPath} */ }
            return null;
        }

        if (!TryInstantiateUiPrefab(partyPanelPrefab, out var instantiatedFromResources))
            return null;

        partyPanel = instantiatedFromResources;
        { /* {LogPrefix} Đã instantiate PartyPanel từ Resources/{partyPanelResourcesPath} */ }
        return partyPanel;
    }

    private bool TryInstantiateUiPrefab(GameObject prefabAsset, out GameObject instance)
    {
        instance = null;
        if (prefabAsset == null)
            return false;

        var parent = ResolveUiParent();
        if (parent == null)
        {
            { /* Lỗi: {LogPrefix} Không tìm thấy Canvas/UI root để instantiate '{prefabAsset.name}' */ }
            return false;
        }

        instance = Instantiate(prefabAsset, parent, false);
        instance.name = prefabAsset.name;
        instance.SetActive(false);
        instance.transform.SetAsLastSibling();

        if (instance.transform is RectTransform rectTransform)
        {
            rectTransform.localScale = Vector3.one;
            rectTransform.anchoredPosition = rectTransform.anchoredPosition;
        }

        return true;
    }

    private Transform ResolveUiParent()
    {
        var currentCanvas = GetComponentInParent<Canvas>(includeInactive: true);
        if (currentCanvas != null)
            return currentCanvas.transform;

        var anyCanvas = FindObjectOfType<Canvas>(includeInactive: true);
        if (anyCanvas != null)
            return anyCanvas.transform;

        return null;
    }

    private static bool IsSceneObjectAlive(GameObject target)
    {
        return target != null && target.scene.IsValid() && target.scene.isLoaded;
    }
}
