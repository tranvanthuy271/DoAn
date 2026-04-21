using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CharacterPanelController – Panel nhân vật 4 tab.
///
/// Thứ tự tab (trái → phải):
///   0 = Thông Số   (StatsTabUI)
///   1 = Trang Bị   (contentEquipment – EquipmentTabUI)
///   2 = Kỹ Năng    (SkillTabUI)
///   3 = Tiềm Năng  (PotentialTabUI)
/// </summary>
public class CharacterPanelController : MonoBehaviour
{
    [Header("Panel Root")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("Child chứa nội dung (Window). Nếu gán, chỉ ẩn/hiện phần này khi dùng InformationPanelController.")]
    [SerializeField] private GameObject contentRoot;

    [Header("Tab Buttons  (Thông Số | Trang Bị | Kỹ Năng | Tiềm Năng)")]
    [SerializeField] private Button btnStats;
    [SerializeField] private Button btnEquipment;
    [SerializeField] private Button btnSkill;
    [SerializeField] private Button btnPotential;

    [Header("Tab Content Panels")]
    [SerializeField] private StatsTabUI     contentStats;
    [SerializeField] private GameObject     contentEquipment;
    [SerializeField] private SkillTabUI     contentSkill;
    [SerializeField] private PotentialTabUI contentPotential;

    [Header("Tab Colors")]
    [SerializeField] private Color colorActiveTab   = new Color(0.2f, 0.7f, 1f, 1f);
    [SerializeField] private Color colorInactiveTab = new Color(0.8f, 0.8f, 0.8f, 1f);

    private int playerId  = -1;
    private int activeTab = 0; // 0=Stats, 1=Equipment, 2=Skill, 3=Potential
    private bool _isExternalProfileView;
    private PlayerProfileDto _externalProfile;
    private string _externalProfileFallbackName;

    // ───────────────────────────────────────────────
    #region Unity lifecycle

    private void Awake()
    {
        // QUAN TRỌNG: Nếu panelRoot không được gán trong Inspector, tự động lấy parent hoặc gameObject
        if (panelRoot == null)
        {
            // Tìm parent GameObject của script này (thường là CharacterPanel root)
            panelRoot = transform.parent != null && transform.parent.gameObject.name.Contains("CharacterPanel") 
                ? transform.parent.gameObject 
                : gameObject;
            Debug.LogWarning($"[CharacterPanelController] panelRoot chưa được gán, tự động lấy: {panelRoot.name}");
        }
        
        if (contentRoot == null) contentRoot = panelRoot; // fallback nếu chưa gán

        btnStats     ?.onClick.AddListener(() => SwitchTab(0));
        btnEquipment ?.onClick.AddListener(() => SwitchTab(1));
        btnSkill     ?.onClick.AddListener(() => SwitchTab(2));
        btnPotential ?.onClick.AddListener(() => SwitchTab(3));
    }

    private void Start()
    {
        // Ẩn panel khi start (chỉ khi không phải null)
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
            Debug.Log($"[CharacterPanelController] Start: Ẩn panelRoot ({panelRoot.name})");
        }
        
        // Đảm bảo contentRoot ẩn khi start
        if (contentRoot != null && contentRoot != panelRoot) 
            contentRoot.SetActive(false);

        if (playerId <= 0)
        {
            int savedId = PlayerPrefs.GetInt("USER_ID", 0);
            if (savedId > 0) SetPlayerId(savedId);
        }
    }

    private void OnDestroy()
    {
        btnStats     ?.onClick.RemoveAllListeners();
        btnEquipment ?.onClick.RemoveAllListeners();
        btnSkill     ?.onClick.RemoveAllListeners();
        btnPotential ?.onClick.RemoveAllListeners();
    }

    #endregion

    // ───────────────────────────────────────────────
    #region Public API

    public void SetPlayerId(int id)
    {
        playerId = id;
        contentStats    ?.SetPlayerId(id);
        contentSkill    ?.SetPlayerId(id);
        contentPotential?.SetPlayerId(id);
    }

    public void ShowFriendProfile(PlayerProfileDto profile, string fallbackUsername = null)
    {
        if (profile == null)
        {
            Debug.LogWarning("[CharacterPanelController] ShowFriendProfile called with null profile.");
            return;
        }

        _isExternalProfileView = true;
        _externalProfile = profile;
        _externalProfileFallbackName = fallbackUsername;

        string displayName = ResolveExternalProfileName(profile, fallbackUsername);
        Debug.Log($"[CharacterPanelController] ShowFriendProfile displayName='{displayName}' playerId={profile.player_id} userId={profile.user_id}");

        contentStats?.ShowFriendProfile(profile, displayName);
        contentEquipment?.GetComponent<EquipmentPanelUI>()?.ShowFriendEquipment(profile.equipment, displayName);
        contentSkill?.ShowFriendSkills(profile.skills, displayName);
        contentPotential?.ShowFriendPotential(profile.potential_stats, displayName);

        if (panelRoot == null)
        {
            Debug.LogError("[CharacterPanelController] ShowFriendProfile failed because panelRoot is null.");
            return;
        }

        panelRoot.SetActive(true);
        if (contentRoot != null)
            contentRoot.SetActive(true);

        panelRoot.transform.SetAsLastSibling();
        activeTab = 0;
        SwitchTab(activeTab);
    }

    public void ExitExternalProfileView()
    {
        if (!_isExternalProfileView && _externalProfile == null)
            return;

        Debug.Log("[CharacterPanelController] ExitExternalProfileView()");

        _isExternalProfileView = false;
        _externalProfile = null;
        _externalProfileFallbackName = null;

        contentStats?.ClearFriendProfile();
        contentEquipment?.GetComponent<EquipmentPanelUI>()?.ClearFriendEquipmentView();
        contentSkill?.ClearFriendSkills();
        contentPotential?.ClearFriendPotential();
    }

    public void Toggle()
    {
        if (panelRoot.activeSelf) Hide();
        else Show();
    }

    /// <summary>Hiện toàn bộ panel (CharacterPanelToggleButton sử dụng).</summary>
    public void Show()
    {
        ExitExternalProfileView();

        if (panelRoot == null)
        {
            Debug.LogError("[CharacterPanelController] Show() bị gọi nhưng panelRoot là NULL! Kiểm tra Inspector.");
            return;
        }
        
        Debug.Log($"[CharacterPanelController] Show() - Active panelRoot: {panelRoot.name}");
        panelRoot.SetActive(true);
        panelRoot.transform.SetAsLastSibling();
        
        if (contentRoot != null)
        {
            contentRoot.SetActive(true);  // đảm bảo Window cũng hiện
            Debug.Log($"[CharacterPanelController] Show() - Active contentRoot: {contentRoot.name}");
        }
        
        SwitchTab(activeTab);
    }

    /// <summary>Tắt toàn bộ panel (CharacterPanelToggleButton sử dụng).</summary>
    public void Hide()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        ExitExternalProfileView();
    }

    /// <summary>
    /// Hiện panel nhân vật và chuyển thẳng vào tab Trang Bị (index 1).
    /// Gọi từ BlacksmithTabPanel khi bấm tab "Trang Bị".
    /// </summary>
    public void ShowEquipmentTab()
    {
        if (panelRoot != null) panelRoot.SetActive(true);
        if (contentRoot != null && contentRoot != panelRoot) contentRoot.SetActive(true);
        SwitchTab(1);
    }

    /// <summary>
    /// Chỉ hiện phần nội dung (Window), giữ nguyên panelRoot để BtnThongTin/BtnTuiDo vẫn hiện.
    /// Dùng bởi InformationPanelController khi bấm BtnThongTin.
    /// </summary>
    public void ShowContent()
    {
        if (!panelRoot.activeSelf) panelRoot.SetActive(true);
        contentRoot.SetActive(true);
        SwitchTab(activeTab);
    }

    /// <summary>
    /// Chỉ ẩn phần nội dung (Window), giữ nguyên panelRoot để BtnThongTin/BtnTuiDo vẫn hiện.
    /// Dùng bởi InformationPanelController khi bấm BtnTuiDo.
    /// </summary>
    public void HideContent()
    {
        if (panelRoot != null && !panelRoot.activeSelf)
            panelRoot.SetActive(true);

        if (contentRoot != null)
            contentRoot.SetActive(false);
    }

    public bool IsVisible() => panelRoot != null && panelRoot.activeSelf;

    #endregion

    // ───────────────────────────────────────────────
    #region Tab switching

    private void SwitchTab(int tabIndex)
    {
        activeTab = tabIndex;

        bool st = tabIndex == 0;
        bool eq = tabIndex == 1;
        bool sk = tabIndex == 2;
        bool pt = tabIndex == 3;

        if (contentStats     != null) contentStats.gameObject.SetActive(st);
        if (contentEquipment != null) contentEquipment.SetActive(eq);
        if (contentSkill     != null) contentSkill.gameObject.SetActive(sk);
        if (contentPotential != null) contentPotential.gameObject.SetActive(pt);

        SetTabColor(btnStats,     st);
        SetTabColor(btnEquipment, eq);
        SetTabColor(btnSkill,     sk);
        SetTabColor(btnPotential, pt);

        if (_isExternalProfileView && _externalProfile != null)
        {
            string displayName = ResolveExternalProfileName(_externalProfile, _externalProfileFallbackName);

            if (st) contentStats?.ShowFriendProfile(_externalProfile, displayName);
            if (eq) contentEquipment?.GetComponent<EquipmentPanelUI>()?.ShowFriendEquipment(_externalProfile.equipment, displayName);
            if (sk) contentSkill?.ShowFriendSkills(_externalProfile.skills, displayName);
            if (pt) contentPotential?.ShowFriendPotential(_externalProfile.potential_stats, displayName);
            return;
        }

        if (st)                  contentStats?.Load();
        if (eq)                  contentEquipment?.GetComponent<EquipmentPanelUI>()?.RefreshFromBridge();
        if (sk && playerId > 0)  contentSkill?.Load();
        if (pt && playerId > 0)  contentPotential?.Load();
    }

    private static string ResolveExternalProfileName(PlayerProfileDto profile, string fallbackUsername)
    {
        if (profile != null && !string.IsNullOrWhiteSpace(profile.character_name))
            return profile.character_name;

        return string.IsNullOrWhiteSpace(fallbackUsername) ? "Bạn bè" : fallbackUsername;
    }

    private void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? colorActiveTab : colorInactiveTab;
    }

    #endregion
}
