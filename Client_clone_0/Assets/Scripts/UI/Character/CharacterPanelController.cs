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

    // ───────────────────────────────────────────────
    #region Unity lifecycle

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (contentRoot == null) contentRoot = panelRoot; // fallback nếu chưa gán

        btnStats     ?.onClick.AddListener(() => SwitchTab(0));
        btnEquipment ?.onClick.AddListener(() => SwitchTab(1));
        btnSkill     ?.onClick.AddListener(() => SwitchTab(2));
        btnPotential ?.onClick.AddListener(() => SwitchTab(3));
    }

    private void Start()
    {
        panelRoot.SetActive(false);
        // Đảm bảo contentRoot ận khi start
        if (contentRoot != panelRoot) contentRoot.SetActive(false);

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

    public void Toggle()
    {
        if (panelRoot.activeSelf) Hide();
        else Show();
    }

    /// <summary>Hiện toàn bộ panel (CharacterPanelToggleButton sử dụng).</summary>
    public void Show()
    {
        panelRoot.SetActive(true);
        contentRoot.SetActive(true);  // đảm bảo Window cũng hiện
        SwitchTab(activeTab);
    }

    /// <summary>Tắt toàn bộ panel (CharacterPanelToggleButton sử dụng).</summary>
    public void Hide() => panelRoot.SetActive(false);

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
    public void HideContent() => contentRoot.SetActive(false);

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

        if (st)                  contentStats?.Load();
        if (eq)                  contentEquipment?.GetComponent<EquipmentPanelUI>()?.RefreshFromBridge();
        if (sk && playerId > 0)  contentSkill?.Load();
        if (pt && playerId > 0)  contentPotential?.Load();
    }

    private void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? colorActiveTab : colorInactiveTab;
    }

    #endregion
}
