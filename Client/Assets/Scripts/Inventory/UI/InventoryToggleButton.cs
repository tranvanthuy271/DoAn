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
        ResolveControllers();
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
        ResolveControllers();
        Debug.Log("[InventoryToggleButton] Button clicked!");
        
        // Ưu tiên dùng InformationPanelController để hiển thị đồng bộ cả frame + inventory
        if (informationPanel != null)
        {
            Debug.Log("[InventoryToggleButton] Sử dụng InformationPanelController");

            // Toggle: nếu túi đồ đang mở thì đóng toàn bộ, ngược lại mở tab Túi Đồ
            if (informationPanel.IsAnyPanelVisible && informationPanel.IsShowingInventory)
            {
                Debug.Log("[InventoryToggleButton] Túi Đồ đang mở → đóng toàn bộ panel");
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

        bool inventoryVisible = inventoryUI != null && inventoryUI.gameObject.activeSelf;
        if (inventoryVisible)
        {
            Debug.Log("[InventoryToggleButton] Túi đồ đang mở → đóng");
            inventoryUI.HideInventory();
            characterPanel?.Hide();
        }
        else if (inventoryUI != null)
        {
            Debug.Log("[InventoryToggleButton] Show CharacterPanel shell + InventoryUI");
            characterPanel?.Show();
            characterPanel?.HideContent();
            inventoryUI.ShowInventory();
        }
        else
        {
            Debug.LogError("[InventoryToggleButton] Chưa gán InformationPanelController, CharacterPanelController hoặc InventoryUI trong Inspector!");
        }
    }

    private void ResolveControllers()
    {
        if (characterPanel == null)
        {
            characterPanel = FindObjectOfType<CharacterPanelController>(includeInactive: true);
            if (characterPanel != null)
                Debug.Log($"[InventoryToggleButton] Auto-found CharacterPanelController: {characterPanel.gameObject.name}");
        }

        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>(includeInactive: true);
            if (inventoryUI != null)
                Debug.Log($"[InventoryToggleButton] Auto-found InventoryUI: {inventoryUI.gameObject.name}");
        }

        if (informationPanel == null)
        {
            informationPanel = InformationPanelController.GetOrCreate(characterPanel, inventoryUI);
            if (informationPanel != null)
                Debug.Log($"[InventoryToggleButton] Auto-found/created InformationPanelController: {informationPanel.gameObject.name}");
        }
    }
}

