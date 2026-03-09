using UnityEngine;
using Unity.Netcode;

/// <summary>
/// InventoryDebugHelper - Debug tool để kiểm tra inventory system
/// Nhấn phím I để debug
/// </summary>
public class InventoryDebugHelper : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private KeyCode debugKey = KeyCode.I;

    private void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            DebugInventorySystem();
        }
        
        // Phím R để force refresh UI
        if (Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("==================== [InventoryDebug] FORCE REFRESH UI ====================");
            ForceSyncInventoryUI();
        }
    }

    private void DebugInventorySystem()
    {
        Debug.Log("==================== [InventoryDebug] INVENTORY SYSTEM STATUS ====================");

        // 1. Check NetworkManager
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[InventoryDebug] ❌ NetworkManager.Singleton is NULL!");
            return;
        }
        Debug.Log($"[InventoryDebug] ✓ NetworkManager: IsClient={NetworkManager.Singleton.IsClient}, IsServer={NetworkManager.Singleton.IsServer}, LocalClientId={NetworkManager.Singleton.LocalClientId}");

        // 2. Check InventoryNetworkBridge
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge == null)
        {
            Debug.LogError("[InventoryDebug] ❌ InventoryNetworkBridge KHÔNG TÌM THẤY trong scene!");
            Debug.LogError("[InventoryDebug]    → NGUYÊN NHÂN: Script chưa được add vào GameObject nào trong scene!");
            Debug.LogError("[InventoryDebug]    → GIẢI PHÁP: Thêm InventoryNetworkBridge script vào GameObject trong GameScene");
        }
        else
        {
            Debug.Log($"[InventoryDebug] ✓ InventoryNetworkBridge found on: {bridge.gameObject.name}");
            Debug.Log($"[InventoryDebug]    → enabled: {bridge.enabled}, gameObject.activeInHierarchy: {bridge.gameObject.activeInHierarchy}");
        }

        // 3. Check InventoryUI
        var ui = FindObjectOfType<InventoryUI>();
        if (ui == null)
        {
            Debug.LogError("[InventoryDebug] ❌ InventoryUI KHÔNG TÌM THẤY!");
        }
        else
        {
            Debug.Log($"[InventoryDebug] ✓ InventoryUI found on: {ui.gameObject.name}");
        }

        // 4. Check NetworkInventory
        var allInventories = FindObjectsOfType<NetworkInventory>();
        Debug.Log($"[InventoryDebug] NetworkInventory count: {allInventories.Length}");
        
        foreach (var inv in allInventories)
        {
            string ownerStatus = "";
            if (inv.IsSpawned)
            {
                ownerStatus = $"IsOwner={inv.IsOwner}, OwnerClientId={inv.OwnerClientId}";
            }
            else
            {
                ownerStatus = "NOT SPAWNED YET";
            }
            
            Debug.Log($"[InventoryDebug]   - {inv.gameObject.name}: {ownerStatus}");
            Debug.Log($"[InventoryDebug]      MaxSlots={inv.GetMaxSlots()}, UsedSlots={inv.GetUsedSlots()}");
            
            // Debug raw data
            for (int i = 0; i < inv.GetMaxSlots(); i++)
            {
                var rawSlot = inv.GetRawSlotData(i);
                if (rawSlot.itemID > 0)
                {
                    Debug.Log($"[InventoryDebug]      Slot {i}: itemID={rawSlot.itemID}, qty={rawSlot.quantity}");
                }
            }
        }

        // 5. Kiểm tra local player inventory
        if (NetworkManager.Singleton.SpawnManager != null)
        {
            Debug.Log($"[InventoryDebug] Spawned objects count: {NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count}");
            
            NetworkInventory localPlayerInv = null;
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (obj.IsOwner)
                {
                    var inv = obj.GetComponent<NetworkInventory>();
                    if (inv != null)
                    {
                        Debug.Log($"[InventoryDebug] ✓✓✓ FOUND LOCAL PLAYER INVENTORY: {obj.name}");
                        localPlayerInv = inv;
                        
                        // Trigger manual refresh
                        Debug.Log("[InventoryDebug] Triggering manual OnInventoryChanged event...");
                        if (inv.OnInventoryChanged != null)
                        {
                            inv.OnInventoryChanged.Invoke();
                        }
                        else
                        {
                            Debug.LogWarning("[InventoryDebug] OnInventoryChanged event is NULL!");
                        }
                    }
                }
            }
            
            if (localPlayerInv == null)
            {
                Debug.LogWarning("[InventoryDebug] ⚠️ Local player inventory NOT FOUND!");
            }
        }

        Debug.Log("==================== [InventoryDebug] END STATUS ====================");
    }

    /// <summary>
    /// Force sync inventory UI - gọi từ button
    /// </summary>
    public void ForceSyncInventoryUI()
    {
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge != null)
        {
            bridge.ManualSyncInventoryUI();
        }
        else
        {
            Debug.LogError("[InventoryDebug] InventoryNetworkBridge not found! Không thể sync!");
        }
    }
}
