using UnityEngine;
using UnityEngine.UI;

// InventoryToggleButton
// - Gắn lên UI Button (nút túi đồ).
// - Hiển thị CharacterPanel + InventoryUI khi nhấn nút (giống như nút BtnTuiDo).
// - Ưu tiên dùng InformationPanelController để đồng bộ state.
[RequireComponent(typeof(Button))]
public class InventoryToggleButton : MonoBehaviour
{
    [Header("Controller References")]
    [Tooltip("Tham chiếu tới InformationPanelController (ưu tiên - quản lý cả CharacterPanel + InventoryUI)")]
    [SerializeField] private InformationPanelController informationPanel;

    [Tooltip("Fallback: tham chiếu tới CharacterPanelController nếu không dùng InformationPanelController")]
    [SerializeField] private CharacterPanelController characterPanel;
    
    [Tooltip("Fallback: tham chiếu tới InventoryUI nếu không dùng InformationPanelController")]
    [SerializeField] private InventoryUI inventoryUI;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
            button.onClick.AddListener(OnButtonClicked);
    }

    private void Start()
    {
        ResolveControllers();
        if (informationPanel == null && characterPanel == null && inventoryUI == null)
            { /* Lỗi: Không tìm thấy InformationPanelController, CharacterPanelController hay InventoryUI trong scene! Hãy gán thủ công trong Inspector */ }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        ResolveControllers();
        { /* Button clicked */ }
        
        // Ưu tiên dùng InformationPanelController để hiển thị đồng bộ cả frame + inventory
        if (informationPanel != null)
        {
            { /* Sử dụng InformationPanelController */ }
            
            // LUÔN hiển thị tab Túi Đồ (giống như nhấn vào BtnTuiDo)
            // Nếu đã đang hiển thị Túi Đồ thì đóng, nếu không thì mở Túi Đồ
            if (informationPanel.IsAnyPanelVisible && informationPanel.IsShowingInventory)
            {
                { /* Đang hiển thị Túi Đồ → đóng panel */ }
                informationPanel.HideAll();
            }
            else
            {
                { /* Mở tab Túi Đồ */ }
                informationPanel.ShowTuiDo();
            }
            return;
        }

        { /* Cảnh báo: Không tìm thấy InformationPanelController, dùng fallback */ }

        bool inventoryVisible = inventoryUI != null && inventoryUI.gameObject.activeSelf;
        if (inventoryVisible)
        {
            { /* Đang hiển thị Túi Đồ → đóng fallback panels */ }
            inventoryUI.HideInventory();
            characterPanel?.Hide();
        }
        else if (inventoryUI != null)
        {
            { /* Show CharacterPanel shell + InventoryUI */ }
            characterPanel?.HideContent();
            inventoryUI.ShowInventory();
        }
        else
        {
            { /* Lỗi: Chưa gán InformationPanelController, CharacterPanelController hoặc InventoryUI trong Inspector */ }
        }
    }

    private void ResolveControllers()
    {
        if (characterPanel == null)
        {
            characterPanel = FindObjectOfType<CharacterPanelController>(includeInactive: true);
            if (characterPanel != null)
                { /* Auto-found CharacterPanelController: {characterPanel.gameObject.name} */ }
        }

        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>(includeInactive: true);
            if (inventoryUI != null)
                { /* Auto-found InventoryUI: {inventoryUI.gameObject.name} */ }
        }

        if (informationPanel == null)
        {
            informationPanel = InformationPanelController.GetOrCreate(characterPanel, inventoryUI);
            if (informationPanel != null)
                { /* Auto-found/created InformationPanelController: {informationPanel.gameObject.name} */ }
        }
    }
}

