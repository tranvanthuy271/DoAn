using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InventoryToggleButton
/// - Gắn lên UI Button (nút túi đồ).
/// - Hiển thị CharacterPanel + InventoryUI khi nhấn nút (giống như nút BtnTuiDo).
/// - Ưu tiên dùng InformationPanelController để đồng bộ state.
/// </summary>
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
        // Tự động tìm trong scene nếu chưa được gán trong Inspector
        if (informationPanel == null)
        {
            informationPanel = FindObjectOfType<InformationPanelController>();
            if (informationPanel != null)
                Debug.Log($"[InventoryToggleButton] Auto-found InformationPanelController: {informationPanel.gameObject.name}");
        }

        if (informationPanel == null && characterPanel == null)
        {
            characterPanel = FindObjectOfType<CharacterPanelController>();
            if (characterPanel != null)
                Debug.Log($"[InventoryToggleButton] Auto-found CharacterPanelController: {characterPanel.gameObject.name}");
        }

        if (informationPanel == null && inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI != null)
                Debug.Log($"[InventoryToggleButton] Auto-found InventoryUI: {inventoryUI.gameObject.name}");
        }

        if (informationPanel == null && characterPanel == null && inventoryUI == null)
            Debug.LogError("[InventoryToggleButton] Không tìm thấy InformationPanelController, CharacterPanelController hay InventoryUI trong scene! Hãy gán thủ công trong Inspector.");
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        Debug.Log("[InventoryToggleButton] Button clicked!");
        
        // Ưu tiên dùng InformationPanelController để hiển thị đồng bộ cả frame + inventory
        if (informationPanel != null)
        {
            Debug.Log("[InventoryToggleButton] Sử dụng InformationPanelController");
            
            // LUÔN hiển thị tab Túi Đồ (giống như nhấn vào BtnTuiDo)
            // Nếu đã đang hiển thị Túi Đồ thì đóng, nếu không thì mở Túi Đồ
            if (informationPanel.IsAnyPanelVisible && informationPanel.IsShowingInventory)
            {
                Debug.Log("[InventoryToggleButton] Đang hiển thị Túi Đồ → đóng panel");
                informationPanel.HideAll();
            }
            else
            {
                Debug.Log("[InventoryToggleButton] Mở tab Túi Đồ");
                informationPanel.ShowTuiDo();
            }
            return;
        }

        Debug.LogWarning("[InventoryToggleButton] Không tìm thấy InformationPanelController, dùng fallback");
        
        // Fallback: nếu không có InformationPanelController
        // Hiển thị CharacterPanel frame + mở InventoryUI
        if (characterPanel != null)
        {
            Debug.Log("[InventoryToggleButton] Show CharacterPanel");
            if (!characterPanel.IsVisible())
            {
                characterPanel.Show();
            }
            // Chuyển sang tab Equipment (index 1) để giống tab Túi Đồ
            characterPanel.GetType().GetMethod("SwitchTab", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(characterPanel, new object[] { 1 });
        }

        if (inventoryUI != null)
        {
            Debug.Log("[InventoryToggleButton] Show InventoryUI");
            inventoryUI.ShowInventory();
        }
        else
        {
            Debug.LogError("[InventoryToggleButton] Chưa gán InformationPanelController, CharacterPanelController hoặc InventoryUI trong Inspector!");
        }
    }
}

