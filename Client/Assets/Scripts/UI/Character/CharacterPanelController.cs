using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CharacterPanelController – Panel nhân vật 4 tab.
///
/// Cấu trúc GameObject khuyến nghị:
/// ┌─ CharacterPanel (Canvas/Panel root)
/// │   ├─ TabBar
/// │   │   ├─ BtnStats      [Button] "Thông Số"   ← tab 0
/// │   │   ├─ BtnEquipment  [Button] "Trang Bị"  ← tab 1
/// │   │   ├─ BtnSkill      [Button] "Kỹ Năng"   ← tab 2
/// │   │   └─ BtnPotential  [Button] "Tiềm Năng" ← tab 3
/// │   ├─ ContentStats      ── chứa StatsTabUI
/// │   ├─ ContentEquipment  ── chứa EquipmentPanelUI (đã có)
/// │   ├─ ContentSkill      ── chứa SkillTabUI (mới)
/// │   └─ ContentPotential  ── chứa PotentialTabUI (mới)
///
/// Setup:
/// 1. Tạo Panel root (CharacterPanel) trong Canvas.
/// 2. Thêm 4 Button tab vào TabBar, kéo vào các slot btnStats/btnEquipment/btnSkill/btnPotential.
/// 3. Kéo StatsTabUI, EquipmentPanelUI, SkillTabUI, PotentialTabUI vào các slot tương ứng.
/// 4. Gắn script này lên CharacterPanel.
/// 5. Đặt playerId = user_id sau khi login (gọi SetPlayerId(id) từ LoginController).
///
/// Với nút mở panel: tạo Button ngoài và gọi characterPanelController.Toggle().
/// </summary>
public class CharacterPanelController : MonoBehaviour
{
    [Header("Panel Root")]
    [Tooltip("Root GameObject của toàn bộ panel nhân vật (thường chính là gameObject này)")]
    [SerializeField] private GameObject panelRoot;

    [Header("Tab Buttons")]
    [SerializeField] private Button btnStats;
    [SerializeField] private Button btnEquipment;
    [SerializeField] private Button btnSkill;
    [SerializeField] private Button btnPotential;

    [Header("Tab Content Panels")]
    [SerializeField] private StatsTabUI     contentStats;
    [SerializeField] private GameObject     contentEquipment;
    [SerializeField] private SkillTabUI     contentSkill;
    [SerializeField] private PotentialTabUI contentPotential;

    [Header("Optional – Highlight active tab")]
    [Tooltip("Màu nút tab đang chọn")]
    [SerializeField] private Color colorActiveTab   = new Color(0.2f, 0.7f, 1f, 1f);
    [Tooltip("Màu nút tab không active")]
    [SerializeField] private Color colorInactiveTab = new Color(0.8f, 0.8f, 0.8f, 1f);

    // --------------- Runtime ---------------
    private int    playerId = -1;
    private int    activeTab = 0; // 0=Stats, 1=Equipment, 2=Skill, 3=Potential

    // ───────────────────────────────────────────────
    #region Unity lifecycle

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;

        btnStats    ?.onClick.AddListener(() => SwitchTab(0));
        btnEquipment?.onClick.AddListener(() => SwitchTab(1));
        btnSkill    ?.onClick.AddListener(() => SwitchTab(2));
        btnPotential?.onClick.AddListener(() => SwitchTab(3));
    }

    private void Start()
    {
        panelRoot.SetActive(false);

        // Tự động đọc playerId từ PlayerPrefs nếu chưa được set qua SetPlayerId()
        if (playerId <= 0)
        {
            int savedId = PlayerPrefs.GetInt("USER_ID", 0);
            if (savedId > 0)
            {
                Debug.Log($"[CharacterPanel] Auto-set playerId={savedId} từ PlayerPrefs.");
                SetPlayerId(savedId);
            }
            else
            {
                Debug.LogWarning("[CharacterPanel] Không tìm thấy USER_ID trong PlayerPrefs!");
            }
        }
    }

    private void OnDestroy()
    {
        btnStats    ?.onClick.RemoveAllListeners();
        btnEquipment?.onClick.RemoveAllListeners();
        btnSkill    ?.onClick.RemoveAllListeners();
        btnPotential?.onClick.RemoveAllListeners();
    }

    #endregion

    // ───────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Đặt player ID (gọi sau khi login thành công).
    /// </summary>
    public void SetPlayerId(int id)
    {
        playerId = id;
        contentSkill    ?.SetPlayerId(id);
        contentPotential?.SetPlayerId(id);
    }

    /// <summary>Mở / đóng panel (dùng cho toggle button bên ngoài).</summary>
    public void Toggle()
    {
        if (panelRoot.activeSelf)
            Hide();
        else
            Show();
    }

    public void Show()
    {
        panelRoot.SetActive(true);
        SwitchTab(activeTab); // refresh tab hiện tại
    }

    public void Hide()
    {
        panelRoot.SetActive(false);
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

        // Show / hide content panels
        if (contentStats     != null) ((MonoBehaviour)contentStats).gameObject.SetActive(st);
        if (contentEquipment != null) contentEquipment.SetActive(eq);
        if (contentSkill     != null) ((MonoBehaviour)contentSkill).gameObject.SetActive(sk);
        if (contentPotential != null) ((MonoBehaviour)contentPotential).gameObject.SetActive(pt);

        // Highlight active tab button
        SetTabColor(btnStats,     st);
        SetTabColor(btnEquipment, eq);
        SetTabColor(btnSkill,     sk);
        SetTabColor(btnPotential, pt);

        // Refresh tab data when switched to it
        if (st)                       contentStats    ?.Load();
        if (sk  && playerId > 0)      contentSkill    ?.Load();
        if (pt)
        {
            Debug.Log($"[CharacterPanel] Tab Tiềm Năng được chọn – playerId={playerId}, contentPotential={(contentPotential == null ? "NULL" : "OK")}");
            if (playerId > 0) contentPotential?.Load();
            else Debug.LogWarning("[CharacterPanel] playerId chưa được set, không gọi Load().");
        }
    }

    private void SetTabColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? colorActiveTab : colorInactiveTab;
    }

    #endregion
}
