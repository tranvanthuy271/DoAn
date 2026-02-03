using UnityEngine;

/// <summary>
/// InventoryDebugger - Nhấn phím I để in toàn bộ túi đồ của Player ra Console.
/// Gắn script này lên cùng GameObject có NetworkInventory (thường là Player prefab).
/// </summary>
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
        if (Input.GetKeyDown(KeyCode.I) && inventory != null)
        {
            Debug.Log("===== INVENTORY =====");

            int maxSlots = inventory.GetMaxSlots();
            for (int i = 0; i < maxSlots; i++)
            {
                InventorySlot slot = inventory.GetSlot(i);
                if (slot != null && slot.itemData != null && slot.quantity > 0)
                {
                    Debug.Log($"Slot {i}: {slot.itemData.itemName} x{slot.quantity}");
                }
            }
        }
    }
}

