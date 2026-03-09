using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InventoryToggleButton
/// - Gắn lên UI Button (nút túi đồ).
/// - Gọi ToggleInventory() trên InventoryUI khi player bấm nút.
/// </summary>
[RequireComponent(typeof(Button))]
public class InventoryToggleButton : MonoBehaviour
{
    [Tooltip("Tham chiếu tới InventoryUI trong scene")]
    [SerializeField] private InventoryUI inventoryUI;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClicked);
        }
    }

    private void OnButtonClicked()
    {
        if (inventoryUI == null)
        {
            Debug.LogWarning("[InventoryToggleButton] Chưa gán InventoryUI, hãy gán trong Inspector.");
            return;
        }

        inventoryUI.ToggleInventory();
    }
}

