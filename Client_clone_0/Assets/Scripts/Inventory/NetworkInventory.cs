using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

/// <summary>
/// NetworkInventory - Hệ thống túi đồ với network synchronization
/// Sử dụng NetworkVariable để sync inventory giữa các clients
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkInventory : NetworkBehaviour
{
    [Header("Inventory Settings")]
    [Tooltip("Số lượng slot tối đa trong inventory")]
    [SerializeField] private int maxSlots = 20;
    
    [Header("Events")]
    public UnityEvent<int, ItemData, int> OnItemAdded; // slotIndex, itemData, quantity
    public UnityEvent<int, int> OnItemRemoved; // slotIndex, quantity
    public UnityEvent<int, int, int> OnItemQuantityChanged; // slotIndex, oldQuantity, newQuantity
    public UnityEvent OnInventoryChanged; // Khi inventory có thay đổi bất kỳ

    // NetworkVariable để sync inventory data
    // Sử dụng struct để lưu trữ item info
    private NetworkVariable<NetworkInventoryData> networkInventoryData = new NetworkVariable<NetworkInventoryData>(
        new NetworkInventoryData(),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Local cache để truy cập nhanh (không cần parse NetworkVariable mỗi lần)
    private Dictionary<int, InventorySlot> localInventory = new Dictionary<int, InventorySlot>();

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to network data changes
        networkInventoryData.OnValueChanged += OnInventoryDataChanged;
        
        // Initialize inventory data trên server
        if (IsServer)
        {
            var initialData = new NetworkInventoryData
            {
                slotData = new InventorySlotData[maxSlots]
            };
            // Khởi tạo tất cả slot là trống
            for (int i = 0; i < maxSlots; i++)
            {
                initialData.slotData[i] = new InventorySlotData { itemID = 0, quantity = 0 };
            }
            networkInventoryData.Value = initialData;
        }
        
        // Initialize local cache
        if (networkInventoryData.Value.slotData != null)
        {
            DeserializeInventory(networkInventoryData.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        networkInventoryData.OnValueChanged -= OnInventoryDataChanged;
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Callback khi NetworkVariable thay đổi
    /// </summary>
    private void OnInventoryDataChanged(NetworkInventoryData oldData, NetworkInventoryData newData)
    {
        DeserializeInventory(newData);
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Deserialize NetworkInventoryData thành local dictionary
    /// </summary>
    private void DeserializeInventory(NetworkInventoryData data)
    {
        localInventory.Clear();
        
        if (data.slotData == null || data.slotData.Length == 0)
            return;

        for (int i = 0; i < data.slotData.Length; i++)
        {
            var slotInfo = data.slotData[i];
            if (slotInfo.itemID != 0) // itemID = 0 nghĩa là slot trống
            {
                ItemData itemData = GetItemDataByID(slotInfo.itemID);
                if (itemData != null)
                {
                    localInventory[i] = new InventorySlot
                    {
                        itemData = itemData,
                        quantity = slotInfo.quantity
                    };
                }
            }
        }
    }

    /// <summary>
    /// ServerRpc: Thêm item vào inventory
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddItemServerRpc(int itemID, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ItemData itemData = GetItemDataByID(itemID);
        if (itemData == null)
        {
            Debug.LogWarning($"[NetworkInventory] ItemID {itemID} không tồn tại!");
            return;
        }

        int remainingQuantity = quantity;

        // Đảm bảo slotData được khởi tạo
        var currentData = networkInventoryData.Value;
        if (currentData.slotData == null || currentData.slotData.Length == 0)
        {
            currentData.slotData = new InventorySlotData[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                currentData.slotData[i] = new InventorySlotData { itemID = 0, quantity = 0 };
            }
        }

        // Nếu item có thể stack, tìm slot đã có item đó
        if (itemData.stackable)
        {
            for (int i = 0; i < currentData.slotData.Length && remainingQuantity > 0; i++)
            {
                var slot = currentData.slotData[i];
                if (slot.itemID == itemID)
                {
                    int spaceAvailable = itemData.maxStack - slot.quantity;
                    if (spaceAvailable > 0)
                    {
                        int addAmount = Mathf.Min(remainingQuantity, spaceAvailable);
                        slot.quantity += addAmount;
                        remainingQuantity -= addAmount;
                        currentData.slotData[i] = slot;
                    }
                }
            }
        }

        // Thêm vào slot trống nếu còn dư
        if (remainingQuantity > 0)
        {
            for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
            {
                var slot = currentData.slotData[i];
                if (slot.itemID == 0) // Slot trống
                {
                    int addAmount = itemData.stackable 
                        ? Mathf.Min(remainingQuantity, itemData.maxStack) 
                        : 1;
                    
                    slot.itemID = itemID;
                    slot.quantity = addAmount;
                    remainingQuantity -= addAmount;
                    currentData.slotData[i] = slot;
                }
            }
        }

        networkInventoryData.Value = currentData;

        // Notify clients
        if (remainingQuantity < quantity)
        {
            int addedQuantity = quantity - remainingQuantity;
            OnItemAddedClientRpc(itemID, addedQuantity);
            Debug.Log($"[NetworkInventory] Added {addedQuantity}x {itemData.itemName} to inventory");
        }

        // Nếu còn dư và không thể thêm được nữa
        if (remainingQuantity > 0)
        {
            Debug.LogWarning($"[NetworkInventory] Inventory đầy! Không thể thêm {remainingQuantity}x {itemData.itemName}");
        }
    }

    /// <summary>
    /// ServerRpc: Xóa item khỏi inventory
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RemoveItemServerRpc(int slotIndex, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (slotIndex < 0 || slotIndex >= maxSlots) return;

        var currentData = networkInventoryData.Value;
        
        // Đảm bảo slotData được khởi tạo
        if (currentData.slotData == null || currentData.slotData.Length == 0)
        {
            currentData.slotData = new InventorySlotData[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                currentData.slotData[i] = new InventorySlotData { itemID = 0, quantity = 0 };
            }
        }

        var slot = currentData.slotData[slotIndex];
        
        if (slot.itemID == 0) return; // Slot trống

        int oldQuantity = slot.quantity;
        slot.quantity -= quantity;
        
        if (slot.quantity <= 0)
        {
            // Xóa item khỏi slot
            slot.itemID = 0;
            slot.quantity = 0;
        }

        currentData.slotData[slotIndex] = slot;
        networkInventoryData.Value = currentData;

        // Notify clients
        OnItemRemovedClientRpc(slotIndex, oldQuantity, slot.quantity);
        Debug.Log($"[NetworkInventory] Removed {quantity}x from slot {slotIndex}");
    }

    /// <summary>
    /// ServerRpc: Sử dụng item (consumable)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void UseItemServerRpc(int slotIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (slotIndex < 0 || slotIndex >= maxSlots) return;

        var currentData = networkInventoryData.Value;
        
        // Đảm bảo slotData được khởi tạo
        if (currentData.slotData == null || currentData.slotData.Length == 0)
        {
            currentData.slotData = new InventorySlotData[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                currentData.slotData[i] = new InventorySlotData { itemID = 0, quantity = 0 };
            }
        }

        var slot = currentData.slotData[slotIndex];
        
        if (slot.itemID == 0) return;

        ItemData itemData = GetItemDataByID(slot.itemID);
        if (itemData == null || !itemData.usable) return;

        // Xử lý effect của item (ví dụ: heal, buff)
        ApplyItemEffectServerRpc(slot.itemID, rpcParams);

        // Giảm quantity hoặc xóa item
        int oldQuantity = slot.quantity;
        slot.quantity--;
        if (slot.quantity <= 0)
        {
            slot.itemID = 0;
            slot.quantity = 0;
        }

        currentData.slotData[slotIndex] = slot;
        networkInventoryData.Value = currentData;

        OnItemRemovedClientRpc(slotIndex, oldQuantity, slot.quantity);
    }

    /// <summary>
    /// ServerRpc: Áp dụng effect của item
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void ApplyItemEffectServerRpc(int itemID, ServerRpcParams rpcParams = default)
    {
        ItemData itemData = GetItemDataByID(itemID);
        if (itemData == null) return;

        // Tìm player owner
        var playerHealth = GetComponent<NetworkPlayerHealth>();
        if (playerHealth != null && itemData.itemType == ItemType.Consumable)
        {
            // Ví dụ: heal player
            playerHealth.Heal(itemData.value);
        }

        // Notify clients về effect
        OnItemUsedClientRpc(itemID);
    }

    /// <summary>
    /// ClientRpc: Notify về item được thêm
    /// </summary>
    [ClientRpc]
    private void OnItemAddedClientRpc(int itemID, int quantity)
    {
        ItemData itemData = GetItemDataByID(itemID);
        if (itemData != null)
        {
            // Tìm slot index
            int slotIndex = FindSlotIndex(itemID);
            OnItemAdded?.Invoke(slotIndex, itemData, quantity);
        }
    }

    /// <summary>
    /// ClientRpc: Notify về item bị xóa
    /// </summary>
    [ClientRpc]
    private void OnItemRemovedClientRpc(int slotIndex, int oldQuantity, int newQuantity)
    {
        OnItemQuantityChanged?.Invoke(slotIndex, oldQuantity, newQuantity);
        if (newQuantity == 0)
        {
            OnItemRemoved?.Invoke(slotIndex, oldQuantity);
        }
    }

    /// <summary>
    /// ClientRpc: Notify về item được sử dụng
    /// </summary>
    [ClientRpc]
    private void OnItemUsedClientRpc(int itemID)
    {
        // Có thể play sound/effect ở đây
        Debug.Log($"[NetworkInventory] Item {itemID} được sử dụng");
    }

    /// <summary>
    /// Tìm slot index của item
    /// </summary>
    private int FindSlotIndex(int itemID)
    {
        var data = networkInventoryData.Value;
        for (int i = 0; i < data.slotData.Length; i++)
        {
            if (data.slotData[i].itemID == itemID)
                return i;
        }
        return -1;
    }

    /// <summary>
    /// Lấy ItemData từ ItemID
    /// </summary>
    private ItemData GetItemDataByID(int itemID)
    {
        // Ưu tiên dùng ItemManager nếu có
        if (ItemManager.Instance != null)
        {
            return ItemManager.Instance.GetItemData(itemID);
        }
        
        // Fallback: Load từ Resources
        ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
        foreach (var item in allItems)
        {
            if (item.itemID == itemID)
                return item;
        }
        
        Debug.LogWarning($"[NetworkInventory] ItemID {itemID} not found!");
        return null;
    }

    // Public API
    public void AddItem(int itemID, int quantity)
    {
        AddItemServerRpc(itemID, quantity);
    }

    public void RemoveItem(int slotIndex, int quantity)
    {
        RemoveItemServerRpc(slotIndex, quantity);
    }

    public void UseItem(int slotIndex)
    {
        UseItemServerRpc(slotIndex);
    }

    public InventorySlot GetSlot(int slotIndex)
    {
        if (localInventory.ContainsKey(slotIndex))
            return localInventory[slotIndex];
        return null;
    }

    public int GetItemQuantity(int itemID)
    {
        int total = 0;
        foreach (var slot in localInventory.Values)
        {
            if (slot.itemData != null && slot.itemData.itemID == itemID)
            {
                total += slot.quantity;
            }
        }
        return total;
    }

    public bool HasItem(int itemID, int quantity = 1)
    {
        return GetItemQuantity(itemID) >= quantity;
    }

    public int GetMaxSlots() => maxSlots;
    public int GetUsedSlots() => localInventory.Count;
}

/// <summary>
/// Struct để lưu trữ dữ liệu inventory trên network
/// </summary>
[System.Serializable]
public struct NetworkInventoryData : INetworkSerializable
{
    public InventorySlotData[] slotData;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref slotData);
    }
}

/// <summary>
/// Struct để lưu trữ thông tin slot trên network
/// </summary>
[System.Serializable]
public struct InventorySlotData : INetworkSerializable
{
    public int itemID;
    public int quantity;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemID);
        serializer.SerializeValue(ref quantity);
    }
}

/// <summary>
/// Class để lưu trữ thông tin slot local
/// </summary>
[System.Serializable]
public class InventorySlot
{
    public ItemData itemData;
    public int quantity;
}
