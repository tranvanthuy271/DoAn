using UnityEngine;
using Unity.Netcode;

// InventoryDebugHelper - Debug tool để kiểm tra inventory system
// Nhấn phím I để debug
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
            { /* ==================== [InventoryDebug] FORCE REFRESH UI ==================== */ }
            ForceSyncInventoryUI();
        }
    }

    private void DebugInventorySystem()
    {
        { /* ==================== [InventoryDebug] INVENTORY SYSTEM STATUS ==================== */ }

        // 1. Check NetworkManager
        if (NetworkManager.Singleton == null)
        {
            { /* Lỗi: NetworkManager.Singleton is NULL */ }
            return;
        }
        { /* ✓ NetworkManager: IsClient={NetworkManager.Singleton.IsClient}, IsServer={NetworkManager.Singleton.IsServer}, LocalClientId={NetworkManager.Singleton.LocalClientId} */ }

        // 2. Check InventoryNetworkBridge
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge == null)
        {
            { /* Lỗi: InventoryNetworkBridge KHÔNG TÌM THẤY trong scene */ }
            { /* Lỗi: → NGUYÊN NHÂN: Script chưa được add vào GameObject nào trong scene */ }
            { /* Lỗi: → GIẢI PHÁP: Thêm InventoryNetworkBridge script vào GameObject trong GameScene */ }
        }
        else
        {
            { /* ✓ InventoryNetworkBridge found on: {bridge.gameObject.name} */ }
            { /* → enabled: {bridge.enabled}, gameObject.activeInHierarchy: {bridge.gameObject.activeInHierarchy} */ }
        }

        // 3. Check InventoryUI
        var ui = FindObjectOfType<InventoryUI>();
        if (ui == null)
        {
            { /* Lỗi: InventoryUI KHÔNG TÌM THẤY */ }
        }
        else
        {
            { /* ✓ InventoryUI found on: {ui.gameObject.name} */ }
        }

        // 4. Check NetworkInventory
        var allInventories = FindObjectsOfType<NetworkInventory>();
        { /* NetworkInventory count: {allInventories.Length} */ }
        
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
            
            { /* {inv.gameObject.name}: {ownerStatus} */ }
            { /* MaxSlots={inv.GetMaxSlots()}, UsedSlots={inv.GetUsedSlots()} */ }
            
            // Debug raw data
            for (int i = 0; i < inv.GetMaxSlots(); i++)
            {
                var rawSlot = inv.GetRawSlotData(i);
                if (rawSlot.itemID > 0)
                {
                    { /* Slot {i}: itemID={rawSlot.itemID}, qty={rawSlot.quantity} */ }
                }
            }
        }

        // 5. Kiểm tra local player inventory
        if (NetworkManager.Singleton.SpawnManager != null)
        {
            { /* Spawned objects count: {NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count} */ }
            
            NetworkInventory localPlayerInv = null;
            foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
            {
                if (obj.IsOwner)
                {
                    var inv = obj.GetComponent<NetworkInventory>();
                    if (inv != null)
                    {
                        { /* ✓✓✓ FOUND LOCAL PLAYER INVENTORY: {obj.name} */ }
                        localPlayerInv = inv;
                        
                        // Trigger manual refresh
                        { /* Triggering manual OnInventoryChanged event */ }
                        if (inv.OnInventoryChanged != null)
                        {
                            inv.OnInventoryChanged.Invoke();
                        }
                        else
                        {
                            { /* Cảnh báo: OnInventoryChanged event is NULL */ }
                        }
                    }
                }
            }
            
            if (localPlayerInv == null)
            {
                { /* Cảnh báo: ⚠️ Local player inventory NOT FOUND */ }
            }
        }

        { /* ==================== [InventoryDebug] END STATUS ==================== */ }
    }

    // Force sync inventory UI - gọi từ button
    public void ForceSyncInventoryUI()
    {
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge != null)
        {
            bridge.ManualSyncInventoryUI();
        }
        else
        {
            { /* Lỗi: InventoryNetworkBridge not found! Không thể sync */ }
        }
    }
}
