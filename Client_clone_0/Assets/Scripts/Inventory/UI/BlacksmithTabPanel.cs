using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BlacksmithTabPanel – Cửa sổ Thợ Rèn với 3 tab:
///   Tab 0 — Cường Hóa   (UpgradePanel)
///   Tab 1 — Trang Bị    (EquipmentPanelUI)
///   Tab 2 — Túi         (InventoryUI)
///
/// ══════════════════════════════════════════════════════════
/// Gọi mở từ NpcMenuUI:
///   BlacksmithTabPanel.Instance.Open();          // tab Cường Hóa
///   BlacksmithTabPanel.Instance.Open(1);         // tab Trang Bị
///
/// Chuyển tab kèm filter túi (từ UpgradePanel):
///   SwitchTabToInventoryWithFilter(filterItemType: 21)  // chọn đá
///   SwitchTabToInventoryWithFilter(filterItemId:   8)   // chọn bùa
/// ══════════════════════════════════════════════════════════
/// HIERARCHY GỢI Ý:
///   Canvas
///   └─ BlacksmithPanel         [BlacksmithTabPanel.cs + Image bg]
///      ├─ TabBar               [HorizontalLayoutGroup]
///      │  ├─ BtnCuongHoa       [Button]  "Cường Hóa"
///      │  ├─ BtnTrangBi        [Button]  "Trang Bị"
///      │  └─ BtnTui            [Button]  "Túi"
///      ├─ BtnClose             [Button]  "X"
///      ├─ PanelCuongHoa        [UpgradePanel.cs]
///      ├─ PanelTrangBi         [EquipmentPanelUI.cs + EquipmentSelectionForUpgrade.cs]
///      └─ PanelTui             [InventoryUI.cs]
/// ══════════════════════════════════════════════════════════
/// </summary>
public class BlacksmithTabPanel : MonoBehaviour
{
    public static BlacksmithTabPanel Instance { get; private set; }

    // ── Tab Buttons ───────────────────────────────────────────────
    [Header("Tab Buttons")]
    [SerializeField] private Button    btnCuongHoa;
    [SerializeField] private Button    btnTrangBi;
    [SerializeField] private Button    btnTui;
    [SerializeField] private Button    btnClose;

    [Header("Tab Style")]
    [SerializeField] private Color colorTabActive   = new Color(1f,   0.85f, 0.1f, 1f);
    [SerializeField] private Color colorTabInactive = new Color(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color bgTabActive      = new Color(0.25f,0.22f,0.08f,1f);
    [SerializeField] private Color bgTabInactive    = new Color(0.12f,0.12f,0.18f,1f);

    // ── Content Panels ────────────────────────────────────────────
    [Header("Content Panels")]
    [SerializeField] private GameObject panelCuongHoa;   // UpgradePanel root
    [SerializeField] private GameObject panelTrangBi;    // EquipmentSelectionForUpgrade root
    [SerializeField] private GameObject panelTui;        // InventoryUI root

    // ── Navigation (Trang Bị tab mở CharacterPanel) ──────────────
    [Header("Navigation — Trang Bi tab")]
    [Tooltip("Gan InformationPanelController cua scene. De trong se tu tim bang FindObjectOfType.")]
    [SerializeField] private InformationPanelController informationPanel;

    // ── Runtime ───────────────────────────────────────────────────
    private int _activeTab = -1;

    // filter mode khi chuyển sang tab Túi
    private int _filterItemId   = 0;   // 0 = không filter
    private int _filterItemType = 0;   // 0 = không filter

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ResolveReferences();

        // Wire buttons and hide panels HERE, before disabling self.
        // Start() is deferred until the next frame after SetActive(true) — if we left
        // panel-hiding in Start(), it would fire AFTER SwitchTab() in Open() and undo the tab switch.
        btnCuongHoa.onClick.AddListener(() => SwitchTab(0));
        btnTrangBi .onClick.AddListener(() => SwitchTab(1));
        btnTui     .onClick.AddListener(() => SwitchTab(2));
        if (btnClose) btnClose.onClick.AddListener(Close);

        SetContentPanelActive(panelCuongHoa, false, "PanelCuongHoa");
        SetContentPanelActive(panelTrangBi,  false, "PanelTrangBi");
        SetContentPanelActive(panelTui,      false, "PanelTui");

        gameObject.SetActive(false);
    }

    private void Start() { /* Initialization handled in Awake() */ }

    // ── Public API ────────────────────────────────────────────────

    /// <summary>Mở panel, mặc định tab Cường Hóa (0).</summary>
    public void Open(int defaultTab = 0)
    {
        ResolveReferences();
        gameObject.SetActive(true);
        _activeTab = -1; // force refresh
        SwitchTab(defaultTab);
    }

    public void Close()
    {
        ResolveReferences();
        UpgradePanel.Instance?.CloseFromTabPanel();
        informationPanel?.HideAll();

        var invUI = GetComponentInPanel<InventoryUI>(panelTui);
        invUI?.HideInventory();

        SetContentPanelActive(panelCuongHoa, false, "PanelCuongHoa");
        SetContentPanelActive(panelTrangBi,  false, "PanelTrangBi");
        SetContentPanelActive(panelTui,      false, "PanelTui");

        _activeTab = -1;
        gameObject.SetActive(false);
    }

    /// <summary>Chuyển sang tab Túi với filter đặc biệt (gọi từ UpgradePanel).</summary>
    public void SwitchTabToInventoryWithFilter(int filterItemId = 0, int filterItemType = 0)
    {
        _filterItemId   = filterItemId;
        _filterItemType = filterItemType;
        SwitchTab(2);
    }

    // ── Tab Switching ─────────────────────────────────────────────

    public void SwitchTab(int tabIndex)
    {
        if (_activeTab == tabIndex) return;
        ResolveReferences();
        _activeTab = tabIndex;

        SetContentPanelActive(panelCuongHoa, tabIndex == 0, "PanelCuongHoa");
        SetContentPanelActive(panelTrangBi,  tabIndex == 1, "PanelTrangBi");
        SetContentPanelActive(panelTui,      tabIndex == 2, "PanelTui");

        SetTabStyle(btnCuongHoa, tabIndex == 0);
        SetTabStyle(btnTrangBi,  tabIndex == 1);
        SetTabStyle(btnTui,      tabIndex == 2);

        OnTabActivated(tabIndex);
    }

    private void OnTabActivated(int tabIndex)
    {
        switch (tabIndex)
        {
            case 0:
                // Quay lại Cường Hóa – reset filter, không reload
                _filterItemId   = 0;
                _filterItemType = 0;
                if (UpgradePanel.Instance != null && !panelCuongHoa.activeSelf)
                {
                    // panel đã bật ở trên, chỉ refresh rate
                }
                break;

            case 1:  // Trang Bị — điều hướng sang CharacterPanel → tab Trang Bị
            {
                _filterItemId   = 0;
                _filterItemType = 0;

                // Tìm InformationPanelController nếu chưa gán trong Inspector
                if (informationPanel == null)
                    informationPanel = FindObjectOfType<InformationPanelController>();

                informationPanel?.ShowThongTin();  // mở tab "Thông Tin" trong InformationPanel

                // Chuyển CharacterPanel vào sub-tab Trang Bị (index 1)
                var cp = informationPanel != null
                    ? informationPanel.GetComponentInChildren<CharacterPanelController>(true)
                    : FindObjectOfType<CharacterPanelController>();
                cp?.ShowEquipmentTab();
                break;
            }

            case 2:  // Túi
            {
                var invUI = GetComponentInPanel<InventoryUI>(panelTui);
                if (invUI != null)
                {
                    // ShowInventory() kích hoạt inventoryRoot + load dữ liệu.
                    // Chỉ gọi RefreshAllSlots() không đủ vì InventoryUI.Awake() đã
                    // set inventoryRoot inactive — cần set lại active rõ ràng.
                    invUI.ShowInventory();

                    // Kích hoạt chế độ stone-selection nếu có filter
                    if (_filterItemId > 0)
                        invUI.EnterItemSelectMode(filterById: _filterItemId, callback: OnInventoryItemSelected);
                    else if (_filterItemType > 0)
                        invUI.EnterItemSelectMode(filterByType: _filterItemType, callback: OnInventoryItemSelected);
                    else
                        invUI.ExitItemSelectMode();
                }
                break;
            }
        }
    }

    // ── Callback từ InventoryUI ───────────────────────────────────

    private void OnInventoryItemSelected(InventorySlotDto slot)
    {
        if (slot == null) return;

        if (_filterItemId == UpgradePanel.CHARM_ITEM_ID)
        {
            // Người chơi chọn bùa
            UpgradePanel.Instance?.SetCharmFromInventory(slot);
        }
        else if (_filterItemType == UpgradePanel.STONE_ITEM_TYPE)
        {
            // Người chơi chọn đá cường hóa
            UpgradePanel.Instance?.OnStoneSelectedFromInventory(slot);
        }

        _filterItemId   = 0;
        _filterItemType = 0;

        // ExitSelectMode sau khi chọn xong
        var invUI = GetComponentInPanel<InventoryUI>(panelTui);
        invUI?.ExitItemSelectMode();
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void ResolveReferences()
    {
        if (panelCuongHoa == null || panelCuongHoa == gameObject)
        {
            var panelCuongHoaTransform = transform.Find("PanelCuongHoa");
            if (panelCuongHoaTransform != null)
                panelCuongHoa = panelCuongHoaTransform.gameObject;
        }

        if (informationPanel == null)
            informationPanel = FindObjectOfType<InformationPanelController>();
    }

    private void SetContentPanelActive(GameObject panel, bool active, string expectedChildName)
    {
        if (panel == gameObject)
        {
            var expectedChild = transform.Find(expectedChildName);
            if (expectedChild != null)
            {
                panel = expectedChild.gameObject;
            }
            else
            {
                Debug.LogWarning($"[BlacksmithTabPanel] {expectedChildName} dang bi gan nham vao chinh BlacksmithPanel root. Bo qua SetActive de tranh tat root.");
                return;
            }
        }

        if (panel != null)
            panel.SetActive(active);
    }

    private void SetTabStyle(Button btn, bool isActive)
    {
        if (btn == null) return;
        var tmp = btn.GetComponentInChildren<TMP_Text>();
        if (tmp) tmp.color = isActive ? colorTabActive : colorTabInactive;
        var img = btn.GetComponent<Image>();
        if (img) img.color = isActive ? bgTabActive : bgTabInactive;
    }

    private static T GetComponentInPanel<T>(GameObject panel) where T : Component
    {
        if (panel == null) return null;
        return panel.GetComponent<T>() ?? panel.GetComponentInChildren<T>(true);
    }
}
