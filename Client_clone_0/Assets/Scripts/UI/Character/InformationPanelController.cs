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

    // ─────────────────────────────────────────────
    #region Unity lifecycle

    private void Awake()
    {
        btnThongTin?.onClick.AddListener(OnClickThongTin);
        btnTuiDo   ?.onClick.AddListener(OnClickTuiDo);
        btnCloseAll?.onClick.AddListener(HideAll);
    }

    private void OnDestroy()
    {
        btnThongTin?.onClick.RemoveListener(OnClickThongTin);
        btnTuiDo   ?.onClick.RemoveListener(OnClickTuiDo);
        btnCloseAll?.onClick.RemoveListener(HideAll);
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

    /// <summary>
    /// Mở toàn bộ panel và về tab Thông Tin.
    /// Dùng cho CharacterPanelToggleButton khi cần mở panel.
    /// </summary>
    public void ShowPanel()
    {
        Debug.Log("[InformationPanelController] ShowPanel() được gọi");
        
        // Đảm bảo CharacterPanel luôn hiện trước (để BtnThongTin/BtnTuiDo hiện ra)
        if (characterPanel != null)
        {
            Debug.Log("[InformationPanelController] Gọi characterPanel.Show()...");
            characterPanel.Show();
        }
        else
        {
            Debug.LogError("[InformationPanelController] characterPanel là NULL! Kiểm tra Inspector.");
        }
        
        SwitchTo(TopTab.ThongTin);
    }

    /// <summary>
    /// Ẩn toàn bộ: CharacterPanel + InventoryUI.
    /// Dùng cho nút "đóng" hoặc CharacterPanelToggleButton khi cần đóng panel.
    /// </summary>
    public void HideAll()
    {
        inventoryUI?.HideInventory();
        characterPanel?.Hide();
        // Reset state để lần mở sau mặc định vào tab Thông Tin
        _activeTab = TopTab.ThongTin;
        SetBtnColor(btnThongTin, false);
        SetBtnColor(btnTuiDo,    false);
    }

    /// <summary>Trả về true nếu bất kỳ panel nào đang hiện.</summary>
    public bool IsAnyPanelVisible =>
        (characterPanel != null && characterPanel.IsVisible()) ||
        (inventoryUI    != null && inventoryUI.gameObject.activeSelf);

    /// <summary>Tab nào đang hiển thị?</summary>
    public bool IsShowingInventory => _activeTab == TopTab.TuiDo;

    #endregion

    // ─────────────────────────────────────────────
    #region Private

    private void SwitchTo(TopTab tab)
    {
        _activeTab = tab;

        bool thongTin = tab == TopTab.ThongTin;
        bool tuiDo    = tab == TopTab.TuiDo;

        // Đảm bảo CharacterPanel.panelRoot hiện (giữ BtnThongTin/BtnTuiDo hiện)
        if (characterPanel != null)
        {
            if (!characterPanel.IsVisible()) characterPanel.Show();

            if (thongTin) characterPanel.ShowContent();
            else          characterPanel.HideContent();
        }

        // InventoryUI: luôn ẩn khi sang ThongTin, luôn mở khi sang TuiDo
        if (inventoryUI != null)
        {
            if (tuiDo) inventoryUI.ShowInventory();
            else       inventoryUI.HideInventory();
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

    #endregion
}
