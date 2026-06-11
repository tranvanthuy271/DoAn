using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

// BlacksmithTabPanel – Cửa sổ Thợ Rèn với 3 tab:
// Tab 0 — Cường Hóa   (UpgradePanel)
// Tab 1 — Trang Bị    (EquipmentPanelUI)
// Tab 2 — Túi         (InventoryUI)
// Gọi mở từ NpcMenuUI:
// BlacksmithTabPanel.Instance.Open();          // tab Cường Hóa
// BlacksmithTabPanel.Instance.Open(1);         // tab Trang Bị
// Chuyển tab kèm filter túi (từ UpgradePanel):
// SwitchTabToInventoryWithFilter(filterItemType: 21)  // chọn đá
// SwitchTabToInventoryWithFilter(filterItemId:   8)   // chọn bùa
// HIERARCHY GỢI Ý:
// Canvas
// └─ BlacksmithPanel         [BlacksmithTabPanel.cs + Image bg]
// ├─ TabBar               [HorizontalLayoutGroup]
// ├─ BtnCuongHoa       [Button]  "Cường Hóa"
// ├─ BtnTrangBi        [Button]  "Trang Bị"
// └─ BtnTui            [Button]  "Túi"
// ├─ BtnClose             [Button]  "X"
// ├─ PanelCuongHoa        [UpgradePanel.cs]
// ├─ PanelTrangBi         [EquipmentPanelUI.cs + EquipmentSelectionForUpgrade.cs]
// └─ PanelTui             [InventoryUI.cs]
public class BlacksmithTabPanel : MonoBehaviour
{
    public static BlacksmithTabPanel Instance { get; private set; }

    // Tab Buttons
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

    // Content Panels
    [Header("Content Panels")]
    [SerializeField] private GameObject panelCuongHoa;   // UpgradePanel root
    [SerializeField] private GameObject panelTrangBi;    // EquipmentSelectionForUpgrade root
    [SerializeField] private GameObject panelTui;        // InventoryUI root

    // Navigation (Trang Bị tab mở CharacterPanel)
    [Header("Navigation — Trang Bi tab")]
    [Tooltip("Gan InformationPanelController cua scene. De trong se tu tim bang FindObjectOfType.")]
    [SerializeField] private InformationPanelController informationPanel;

    // ── External Inventory Panel (đứng song song BlacksmithPanel) ─
    [Header("External Inventory Panel")]
    [Tooltip("InventoryUI nằm ngoài BlacksmithPanel (ngang hàng). Sẽ được bật khi tab Túi active và tắt khi chuyển tab hoặc đóng.")]
    [SerializeField] private InventoryUI externalInventoryPanel;

    // Runtime
    private int _activeTab = -1;

    // filter mode khi chuyển sang tab Túi
    private int _filterItemId   = 0;   // 0 = không filter
    private int _filterItemType = 0;   // 0 = không filter

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UIPanelManager.Register(gameObject, Close);

        ResolveReferences();

        // Wire buttons and hide panels HERE, before disabling self.
        // Start() is deferred until the next frame after SetActive(true) — if we left
        // panel-hiding in Start(), it would fire AFTER SwitchTab() in Open() and undo the tab switch.
        WireTabButton(btnCuongHoa, OnCuongHoaTabClicked, "BtnCuongHoa");
        WireTabButton(btnTrangBi,  OnTrangBiTabClicked,  "BtnTrangBi");
        WireTabButton(btnTui,      OnTuiTabClicked,      "BtnTui");
        if (btnClose) btnClose.onClick.AddListener(Close);
        WireCloseButtons();

        SetContentPanelActive(panelCuongHoa, false, "PanelCuongHoa");
        SetContentPanelActive(panelTrangBi,  false, "PanelTrangBi");
        SetContentPanelActive(panelTui,      false, "PanelTui");
        BringNavigationToFront();

        gameObject.SetActive(false);
    }

    private void Start() { /* Initialization handled in Awake() */ }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Mở panel, mặc định tab Cường Hóa (0).
    public void Open(int defaultTab = 0)
    {
        UIPanelManager.CloseOthers(gameObject);
        ResolveReferences();
        gameObject.SetActive(true);
        // Awake() có thể chạy lần đầu tiên (khi panel khởi đầu ở trạng thái inactive)
        // và gọi SetActive(false) ở cuối. Gọi lại SetActive(true) để đảm bảo panel hiển thị.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        UIPanelManager.NotifyOpened(gameObject);
        BringNavigationToFront();
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

        externalInventoryPanel?.HideInventory();
        externalInventoryPanel?.SetBlacksmithUpgradeMode(false);

        SetContentPanelActive(panelCuongHoa, false, "PanelCuongHoa");
        SetContentPanelActive(panelTrangBi,  false, "PanelTrangBi");
        SetContentPanelActive(panelTui,      false, "PanelTui");

        _activeTab = -1;
        gameObject.SetActive(false);
        UIPanelManager.NotifyClosed(gameObject);
    }

    // Chuyển sang tab Túi với filter đặc biệt (gọi từ UpgradePanel).
    public void SwitchTabToInventoryWithFilter(int filterItemId = 0, int filterItemType = 0)
    {
        _filterItemId   = filterItemId;
        _filterItemType = filterItemType;
        SwitchTab(2);
    }

    // Tab Switching

    public void SwitchTab(int tabIndex)
    {
        ResolveReferences();

        if (_activeTab == tabIndex)
        {
            OnTabActivated(tabIndex);
            return;
        }

        // Thoát upgrade select mode khi rời tab 1
        if (_activeTab == 1 && tabIndex != 1)
        {
            var prevEqPanel = GetComponentInPanel<EquipmentPanelUI>(panelTrangBi);
            prevEqPanel?.ExitUpgradeSelectMode();
        }

        // Ẩn external inventory panel khi rời tab Túi
        if (_activeTab == 2 && tabIndex != 2)
        {
            GetComponentInPanel<InventoryUI>(panelTui)?.HideInventory();
            externalInventoryPanel?.HideInventory();
            externalInventoryPanel?.SetBlacksmithUpgradeMode(false);
        }

        _activeTab = tabIndex;
        bool keepTrangBiActiveForInventory = tabIndex == 2 && IsSameOrChildOf(panelTui, panelTrangBi);

        SetContentPanelActive(panelCuongHoa, tabIndex == 0, "PanelCuongHoa");
        SetContentPanelActive(panelTrangBi,  tabIndex == 1 || keepTrangBiActiveForInventory, "PanelTrangBi");
        SetContentPanelActive(panelTui,      tabIndex == 2, "PanelTui");

        // Child component Awake() (e.g. UpgradePanel.Awake) may fire on first activation
        // and call gameObject.SetActive(false) as part of its own init pattern.
        // Re-ensure the intended active state after all Awake() calls have settled.
        if (tabIndex == 0 && panelCuongHoa != null && !panelCuongHoa.activeSelf)
            panelCuongHoa.SetActive(true);
        if ((tabIndex == 1 || keepTrangBiActiveForInventory) && panelTrangBi != null && !panelTrangBi.activeSelf)
            panelTrangBi.SetActive(true);
        if (tabIndex == 2 && panelTui != null && !panelTui.activeSelf)
            panelTui.SetActive(true);

        SetTabStyle(btnCuongHoa, tabIndex == 0);
        SetTabStyle(btnTrangBi,  tabIndex == 1);
        SetTabStyle(btnTui,      tabIndex == 2);
        BringNavigationToFront();

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
                HideInformationPanelIfUsed();
                if (UpgradePanel.Instance != null && panelCuongHoa != null && !panelCuongHoa.activeSelf)
                {
                    // panel đã bật ở trên, chỉ refresh rate
                }
                break;

            case 1:  // Trang Bị — chọn trang bị đang mặc để nâng cấp
            {
                _filterItemId   = 0;
                _filterItemType = 0;

                // Ưu tiên: EquipmentPanelUI gắn trực tiếp trong panelTrangBi
                ShowEquipmentPanelIfUsed();

                var eqPanel = GetComponentInPanel<EquipmentPanelUI>(panelTrangBi);
                if (eqPanel != null)
                {
                    eqPanel.EnterUpgradeSelectMode((item, slotType) =>
                    {
                        var invUI  = GetComponentInPanel<InventoryUI>(panelTui);
                        string key = slotType.ToString().ToLower();
                        UpgradePanel.Instance?.SetChosenEquipItem(
                            item, key, fromInventory: false, inventory: invUI?.CurrentSlots);
                        SwitchTab(0);
                    });
                }
                else
                {
                    // Fallback: EquipmentSelectionForUpgrade (dạng danh sách cũ)
                    var selPanel = GetComponentInPanel<EquipmentSelectionForUpgrade>(panelTrangBi);
                    if (selPanel != null)
                        selPanel.Show();
                    else
                    {
                        // Fallback cuối: mở InventoryPanel (không cần CharacterPanel)
                        if (informationPanel == null)
                            informationPanel = FindObjectOfType<InformationPanelController>(includeInactive: true);
                        informationPanel?.ShowTuiDo();
                    }
                }
                break;
            }

            case 2:  // Túi
            {
                var invUI = GetComponentInPanel<InventoryUI>(panelTui) ?? externalInventoryPanel;
                ShowInventoryPanelIfUsed(invUI);

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

                // Bật external inventory panel (song song với BlacksmithPanel)
                if (externalInventoryPanel != null)
                {
                    externalInventoryPanel.ShowInventory();
                    // Bật upgrade mode khi không đang ở chế độ chọn đá/bùa
                    if (_filterItemId == 0 && _filterItemType == 0)
                        externalInventoryPanel.SetBlacksmithUpgradeMode(true, OnBagEquipSelectedForUpgrade);
                    else
                        externalInventoryPanel.SetBlacksmithUpgradeMode(false);
                }
                break;
            }
        }
    }

    // Callback từ InventoryUI

    // Gọi khi người chơi nhấn "Nâng cấp" trên item trang bị trong tab Túi (external panel).
    // Chuyển item sang UpgradePanel và về tab Cường Hóa.
    private void OnBagEquipSelectedForUpgrade(InventorySlotDto slot)
    {
        if (slot == null) return;

        // Ẩn detail panel trước khi chuyển tab
        externalInventoryPanel?.HideItemDetail();
        GetComponentInPanel<InventoryUI>(panelTui)?.HideItemDetail();

        var invUI = externalInventoryPanel ?? GetComponentInPanel<InventoryUI>(panelTui);
        UpgradePanel.Instance?.OpenForInventory(slot, invUI?.CurrentSlots);
        SwitchTab(0);
    }

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

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private void ResolveReferences()
    {
        btnCuongHoa ??= FindChildButton("BtnCuongHoa");
        btnTrangBi  ??= FindChildButton("BtnTrangBi");
        btnTui      ??= FindChildButton("BtnTui");
        btnClose    ??= FindChildButton("BtnClose");

        panelCuongHoa = ResolvePanelReference(panelCuongHoa, "PanelCuongHoa");
        panelTrangBi  = ResolvePanelReference(panelTrangBi,  "PanelTrangBi");
        panelTui      = ResolvePanelReference(panelTui,      "PanelTui");

        if (informationPanel == null)
            informationPanel = FindObjectOfType<InformationPanelController>(includeInactive: true);

        if (externalInventoryPanel == null)
            externalInventoryPanel = GetComponentInPanel<InventoryUI>(panelTui);

        // Khi người dùng nhấn nút đóng trữ tiếp trong external inventory, đóng luôn BlacksmithTabPanel
        if (externalInventoryPanel != null)
            externalInventoryPanel.OnCloseButtonClicked = Close;

        WireCloseButtons();
    }

    private void OnCuongHoaTabClicked() => SwitchTab(0);
    private void OnTrangBiTabClicked() => SwitchTab(1);
    private void OnTuiTabClicked() => SwitchTab(2);

    private void WireTabButton(Button button, UnityAction action, string expectedName)
    {
        if (button == null)
        {
            { /* Cảnh báo: Missing tab button '{expectedName}'. Check BlacksmithPanel hierarchy or assign it in Inspector */ }
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private GameObject ResolvePanelReference(GameObject current, string childName)
    {
        if (current != null && current != gameObject)
            return current;

        Transform child = FindChildRecursive(transform, childName);
        if (child != null)
            return child.gameObject;

        return current;
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
                { /* Cảnh báo: {expectedChildName} dang bi gan nham vao chinh BlacksmithPanel root. Bo qua SetActive de tranh tat root */ }
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

    private void BringNavigationToFront()
    {
        Transform navRoot = btnClose != null ? btnClose.transform.parent : null;
        navRoot ??= btnTui != null ? btnTui.transform.parent : null;
        navRoot ??= btnCuongHoa != null ? btnCuongHoa.transform.parent : null;
        navRoot ??= btnTrangBi != null ? btnTrangBi.transform.parent : null;

        if (navRoot != null && navRoot.parent == transform)
        {
            navRoot.SetAsLastSibling();
            return;
        }

        btnCuongHoa?.transform.SetAsLastSibling();
        btnTrangBi?.transform.SetAsLastSibling();
        btnTui?.transform.SetAsLastSibling();
        btnClose?.transform.SetAsLastSibling();
    }

    private void WireCloseButtons()
    {
        if (btnClose == null)
            btnClose = FindNavigationCloseButton();

        if (btnClose == null)
            return;

        btnClose.onClick.RemoveListener(Close);
        btnClose.onClick.AddListener(Close);
    }

    private Button FindNavigationCloseButton()
    {
        Transform navRoot = btnTui != null ? btnTui.transform.parent : null;
        navRoot ??= btnCuongHoa != null ? btnCuongHoa.transform.parent : null;
        navRoot ??= btnTrangBi != null ? btnTrangBi.transform.parent : null;

        if (navRoot == null)
            return null;

        return FindChildButton("BtnClose", navRoot);
    }

    private Button FindChildButton(string childName, Transform root = null)
    {
        Transform child = FindChildRecursive(root ?? transform, childName);
        if (child == null)
            return null;

        return child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        foreach (Transform child in root)
        {
            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }

        return null;
    }

    private void ShowEquipmentPanelIfUsed()
    {
        var characterPanel = GetComponentInPanel<CharacterPanelController>(panelTrangBi);
        if (characterPanel == null)
            return;

        GetComponentInPanel<InventoryUI>(panelTui)?.HideInventory();

        if (informationPanel != null)
            informationPanel.ShowThongTin();

        characterPanel.ShowEquipmentTab();
    }

    private void ShowInventoryPanelIfUsed(InventoryUI invUI)
    {
        if (invUI == null)
            return;

        if (informationPanel != null
            && informationPanel.GetComponentInChildren<InventoryUI>(true) == invUI)
        {
            informationPanel.ShowTuiDo();
            return;
        }

        var characterPanel = invUI.GetComponentInParent<CharacterPanelController>(true);
        if (characterPanel != null)
            characterPanel.HideContent();
    }

    private void HideInformationPanelIfUsed()
    {
        if (informationPanel == null)
            return;

        if (IsSameOrChildOf(panelTrangBi, informationPanel.gameObject)
            || IsSameOrChildOf(panelTui, informationPanel.gameObject)
            || informationPanel.GetComponentInChildren<InventoryUI>(true) == externalInventoryPanel)
        {
            informationPanel.HideAll();
        }
    }

    private static bool IsSameOrChildOf(GameObject child, GameObject possibleParent)
    {
        if (child == null || possibleParent == null)
            return false;

        Transform current = child.transform;
        Transform parent = possibleParent.transform;
        while (current != null)
        {
            if (current == parent)
                return true;

            current = current.parent;
        }

        return false;
    }
}
