using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InformationPanelController – Điều khiển 2 tab cấp cao nhất:
///   • BtnThongTin → ẩn InventoryUI, hiện CharacterPanel (4 tab con)
///   • BtnTuiDo    → ẩn CharacterPanel, hiện InventoryUI và refresh dữ liệu
///   • BtnCloseAll → ẩn toàn bộ (CharacterPanel + InventoryUI)
///
/// Setup trong Inspector:
///   1. Gắn script này lên GameObject gốc chứa toàn bộ UI nhân vật.
///   2. Kéo BtnThongTin, BtnTuiDo, BtnCloseAll vào đây.
///   3. Kéo CharacterPanelController và InventoryUI vào đây.
/// </summary>
public class InformationPanelController : MonoBehaviour
{
    [Header("Top-level Tab Buttons")]
    [SerializeField] private Button btnThongTin;
    [SerializeField] private Button btnTuiDo;
    [Tooltip("Nút đóng toàn bộ panel (ẩn cả Thông Tin lẫn Túi Đồ)")]
    [SerializeField] private Button btnCloseAll;

    [Header("Panels")]
    [SerializeField] private CharacterPanelController characterPanel;
    [SerializeField] private InventoryUI inventoryUI;

    [Header("Tab Colors")]
    [SerializeField] private Color colorActive   = new Color(0.2f, 0.7f, 1f, 1f);
    [SerializeField] private Color colorInactive = new Color(0.8f, 0.8f, 0.8f, 1f);

    private enum TopTab { ThongTin, TuiDo }
    private TopTab _activeTab = TopTab.ThongTin;
    private readonly List<Button> autoCloseButtons = new List<Button>();

    public static InformationPanelController GetOrCreate(
        CharacterPanelController runtimeCharacterPanel = null,
        InventoryUI runtimeInventoryUI = null)
    {
        InformationPanelController controller = FindObjectOfType<InformationPanelController>(includeInactive: true);
        if (controller == null)
        {
            GameObject host = runtimeCharacterPanel != null
                ? runtimeCharacterPanel.gameObject
                : runtimeInventoryUI != null
                    ? runtimeInventoryUI.gameObject
                    : null;

            if (host == null)
                return null;

            controller = host.GetComponent<InformationPanelController>();
            if (controller == null)
                controller = host.AddComponent<InformationPanelController>();
        }

        controller.SetRuntimeReferences(runtimeCharacterPanel, runtimeInventoryUI);
        return controller;
    }

    // ─────────────────────────────────────────────
    #region Unity lifecycle

    private void Awake()
    {
        UIPanelManager.Register(gameObject, HideAll);
        ResolveReferences();
        RegisterButtonListeners();
    }

    private void OnEnable()
    {
        ResolveReferences();
        RegisterButtonListeners();
    }

    private void OnDestroy()
    {
        UIPanelManager.Unregister(gameObject);
        UnregisterButtonListeners();
    }

    #endregion

    // ─────────────────────────────────────────────
    #region Button handlers

    private void OnClickThongTin() => SwitchTo(TopTab.ThongTin);
    private void OnClickTuiDo()    => SwitchTo(TopTab.TuiDo);

    #endregion

    // ─────────────────────────────────────────────
    #region Public API

    /// <summary>Mở panel nhân vật (tab Thông Tin). Cũng ẩn túi đồ nếu đang mở.</summary>
    public void ShowThongTin() => SwitchTo(TopTab.ThongTin);

    /// <summary>Mở túi đồ (tab Túi Đồ). Cũng ẩn thông tin nếu đang mở.</summary>
    public void ShowTuiDo() => SwitchTo(TopTab.TuiDo);

    public void SetRuntimeReferences(
        CharacterPanelController runtimeCharacterPanel,
        InventoryUI runtimeInventoryUI)
    {
        if (characterPanel == null && runtimeCharacterPanel != null)
            characterPanel = runtimeCharacterPanel;

        if (inventoryUI == null && runtimeInventoryUI != null)
            inventoryUI = runtimeInventoryUI;

        ResolveReferences();
        RegisterButtonListeners();
    }

    /// <summary>
    /// Mở toàn bộ panel và về tab Thông Tin.
    /// Dùng cho CharacterPanelToggleButton khi cần mở panel.
    /// </summary>
    public void ShowPanel()
    {
        UIPanelManager.CloseOthers(gameObject);
        ResolveReferences();
        Debug.Log("[InformationPanelController] ShowPanel() được gọi");

        if (characterPanel == null)
            Debug.LogError("[InformationPanelController] characterPanel là NULL! Kiểm tra Inspector.");

        SwitchTo(TopTab.ThongTin);
        UIPanelManager.NotifyOpened(gameObject);
    }

    /// <summary>
    /// Ẩn toàn bộ: CharacterPanel + InventoryUI.
    /// Dùng cho nút "đóng" hoặc CharacterPanelToggleButton khi cần đóng panel.
    /// </summary>
    public void HideAll()
    {
        ResolveReferences();
        inventoryUI?.HideInventory();
        characterPanel?.Hide();
        // Reset state để lần mở sau mặc định vào tab Thông Tin
        _activeTab = TopTab.ThongTin;
        SetBtnColor(btnThongTin, false);
        SetBtnColor(btnTuiDo,    false);
        UIPanelManager.NotifyClosed(gameObject);
    }

    /// <summary>Trả về true nếu bất kỳ panel nào đang hiện.</summary>
    public bool IsAnyPanelVisible
    {
        get
        {
            ResolveReferences();
            return (characterPanel != null && characterPanel.IsVisible())
                || (inventoryUI != null && inventoryUI.gameObject.activeSelf);
        }
    }

    /// <summary>Tab nào đang hiển thị?</summary>
    public bool IsShowingInventory => _activeTab == TopTab.TuiDo;

    #endregion

    // ─────────────────────────────────────────────
    #region Private

    private void SwitchTo(TopTab tab)
    {
        ResolveReferences();
        _activeTab = tab;

        bool thongTin = tab == TopTab.ThongTin;
        bool tuiDo    = tab == TopTab.TuiDo;

        if (thongTin)
        {
            characterPanel?.ShowContent();
            inventoryUI?.HideInventory();
        }
        else if (tuiDo)
        {
            characterPanel?.HideContent();
            inventoryUI?.ShowInventory();
        }

        // Màu nút
        SetBtnColor(btnThongTin, thongTin);
        SetBtnColor(btnTuiDo,    tuiDo);
    }

    private void SetBtnColor(Button btn, bool active)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = active ? colorActive : colorInactive;
    }

    private void ResolveReferences()
    {
        if (characterPanel == null)
        {
            characterPanel = FindObjectOfType<CharacterPanelController>(includeInactive: true);
            if (characterPanel != null)
                Debug.Log($"[InformationPanelController] Auto-resolved CharacterPanelController: {characterPanel.gameObject.name}");
        }

        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>(includeInactive: true);
            if (inventoryUI != null)
                Debug.Log($"[InformationPanelController] Auto-resolved InventoryUI: {inventoryUI.gameObject.name}");
        }

        if (btnThongTin == null)
            btnThongTin = FindButtonByName("BtnThongTin");

        if (btnTuiDo == null)
            btnTuiDo = FindButtonByName("BtnTuiDo");

        if (btnCloseAll == null)
        {
            btnCloseAll = FindButtonInHierarchy(characterPanel != null ? characterPanel.transform : null, "BtnClose")
                       ?? FindButtonInHierarchy(inventoryUI != null ? inventoryUI.transform : null, "BtnClose");
        }
    }

    private void RegisterButtonListeners()
    {
        ResolveReferences();

        btnThongTin?.onClick.RemoveListener(OnClickThongTin);
        btnThongTin?.onClick.AddListener(OnClickThongTin);

        btnTuiDo?.onClick.RemoveListener(OnClickTuiDo);
        btnTuiDo?.onClick.AddListener(OnClickTuiDo);

        RebindCloseButtons();
    }

    private void UnregisterButtonListeners()
    {
        btnThongTin?.onClick.RemoveListener(OnClickThongTin);
        btnTuiDo?.onClick.RemoveListener(OnClickTuiDo);

        for (int i = 0; i < autoCloseButtons.Count; i++)
        {
            Button closeButton = autoCloseButtons[i];
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HideAll);
        }

        autoCloseButtons.Clear();
    }

    private void RebindCloseButtons()
    {
        for (int i = 0; i < autoCloseButtons.Count; i++)
        {
            Button closeButton = autoCloseButtons[i];
            if (closeButton != null)
                closeButton.onClick.RemoveListener(HideAll);
        }

        autoCloseButtons.Clear();

        AddAutoCloseButton(btnCloseAll);
        AddCloseButtonsFromRoot(characterPanel != null ? characterPanel.transform : null);
        AddCloseButtonsFromRoot(inventoryUI != null ? inventoryUI.transform : null);
    }

    private void AddCloseButtonsFromRoot(Transform root)
    {
        if (root == null)
            return;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button != null && button.name == "BtnClose")
                AddAutoCloseButton(button);
        }
    }

    private void AddAutoCloseButton(Button button)
    {
        if (button == null || autoCloseButtons.Contains(button))
            return;

        button.onClick.RemoveListener(HideAll);
        button.onClick.AddListener(HideAll);
        autoCloseButtons.Add(button);
    }

    private Button FindButtonByName(string buttonName)
    {
        Button button = FindButtonInHierarchy(characterPanel != null ? characterPanel.transform : null, buttonName);
        if (button != null)
            return button;

        button = FindButtonInHierarchy(inventoryUI != null ? inventoryUI.transform : null, buttonName);
        if (button != null)
            return button;

        Button[] buttons = FindObjectsOfType<Button>(includeInactive: true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == buttonName)
                return buttons[i];
        }

        return null;
    }

    private static Button FindButtonInHierarchy(Transform root, string buttonName)
    {
        if (root == null)
            return null;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].name == buttonName)
                return buttons[i];
        }

        return null;
    }

    #endregion
}
