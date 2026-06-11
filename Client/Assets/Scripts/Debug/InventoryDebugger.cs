using UnityEngine;

// InventoryDebugger - Nhấn phím I để in toàn bộ túi đồ của Player ra Console.
// Gắn script này lên cùng GameObject có NetworkInventory (thường là Player prefab).
public class InventoryDebugger : MonoBehaviour
{
    private NetworkInventory inventory;

    private void Start()
    {
        inventory = GetComponent<NetworkInventory>();

        if (inventory == null)
        {
            Debug.LogWarning("[InventoryDebugger] Không tìm thấy NetworkInventory trên GameObject này.");
        }
    }

    private void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked) return;
        if (Input.GetKeyDown(KeyCode.I) && inventory != null)
        {
            Debug.Log("===== INVENTORY =====");

            int maxSlots = inventory.GetMaxSlots();
            for (int i = 0; i < maxSlots; i++)
            {
                InventorySlot slot = inventory.GetSlot(i);
                if (slot != null && slot.itemID > 0 && slot.quantity > 0)
                {
                    var template = ItemTemplateManager.Instance?.GetItemTemplate(slot.itemID);
                    string itemName = template?.name ?? $"Item {slot.itemID}";
                    Debug.Log($"Slot {i}: {itemName} x{slot.quantity}");
                }
            }
        }
    }
}

