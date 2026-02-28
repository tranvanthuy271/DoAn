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
        
        Debug.Log($"[NetworkInventory] ===== OnNetworkSpawn CALLED! =====");
        Debug.Log($"[NetworkInventory] IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}");
        
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
            
            // ✅ LOAD INVENTORY FROM DB
            // Server load inventory cho owner của object này
            Debug.Log($"[NetworkInventory] Server: Bắt đầu load inventory từ DB... (OwnerClientId={OwnerClientId})");
            StartCoroutine(LoadInventoryFromDBDelayed());
        }
        
        // Initialize local cache
        if (networkInventoryData.Value.slotData != null)
        {
            DeserializeInventory(networkInventoryData.Value);
            Debug.Log($"[NetworkInventory] Deserialized inventory on spawn. UsedSlots={GetUsedSlots()}");
        }
        
        // 🔥 CLIENT: Trigger OnInventoryChanged sau một delay để đảm bảo Bridge đã subscribe
        if (IsClient && !IsServer)
        {
            Debug.Log("[NetworkInventory] Client: Scheduling delayed OnInventoryChanged trigger...");
            StartCoroutine(TriggerInventoryChangedDelayed());
        }
    }
    
    private System.Collections.IEnumerator LoadInventoryFromDBDelayed()
    {
        // Đợi một frame để đảm bảo NetworkObject đã spawn hoàn toàn
        yield return new WaitForSeconds(0.5f);
        LoadInventoryFromDB();
    }
    
    private System.Collections.IEnumerator TriggerInventoryChangedDelayed()
    {
        // Đợi 2 giây để Bridge có thời gian subscribe
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[NetworkInventory] ===== MANUAL TRIGGER OnInventoryChanged (Client) =====");
        OnInventoryChanged?.Invoke();
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
        Debug.Log($"[NetworkInventory] ===== OnInventoryDataChanged TRIGGERED! =====");
        Debug.Log($"[NetworkInventory] IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}");
        
        int itemCount = 0;
        if (newData.slotData != null)
        {
            foreach (var slot in newData.slotData)
            {
                if (slot.itemID > 0) itemCount++;
            }
        }
        Debug.Log($"[NetworkInventory] New data has {itemCount} items");
        
        DeserializeInventory(newData);
        
        Debug.Log($"[NetworkInventory] Calling OnInventoryChanged?.Invoke()...");
        OnInventoryChanged?.Invoke();
        
        Debug.Log($"[NetworkInventory] ✓ OnInventoryChanged event invoked!");
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
                // Không cần check GetItemTemplate() ở đây, chỉ lưu itemID
                // Template sẽ được query khi cần display UI
                localInventory[i] = new InventorySlot
                {
                    itemID = slotInfo.itemID,
                    quantity = slotInfo.quantity
                };
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

        var template = GetItemTemplate(itemID);
        if (template == null)
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
        if (template.stackable)
        {
            for (int i = 0; i < currentData.slotData.Length && remainingQuantity > 0; i++)
            {
                var slot = currentData.slotData[i];
                if (slot.itemID == itemID)
                {
                    int spaceAvailable = template.max_stack - slot.quantity;
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
                    int addAmount = template.stackable 
                        ? Mathf.Min(remainingQuantity, template.max_stack) 
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
            Debug.Log($"[NetworkInventory] Added {addedQuantity}x {template.name} to inventory");
        }

        // Nếu còn dư và không thể thêm được nữa
        if (remainingQuantity > 0)
        {
            Debug.LogWarning($"[NetworkInventory] Inventory đầy! Không thể thêm {remainingQuantity}x {template.name}");
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

        var template = GetItemTemplate(slot.itemID);
        if (template == null) return;
        
        // Check if item is usable (consumable)
        if (template.item_type != 1) return; // 1 = Consumable

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
        var template = GetItemTemplate(itemID);
        if (template == null) return;

        // Tìm player owner
        var playerHealth = GetComponent<NetworkPlayerHealth>();
        if (playerHealth != null && template.item_type == 1) // 1 = Consumable
        {
            // TODO: Implement healing logic based on item stats
            // playerHealth.Heal(template.value);
            Debug.Log($"[NetworkInventory] Used item {template.name} - Effect not implemented yet");
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
    /// Lấy ItemTemplateDto từ ItemID (dùng ItemTemplateManager mới)
    /// </summary>
    private ItemTemplateDto GetItemTemplate(int itemID)
    {
        if (ItemTemplateManager.Instance == null)
        {
            Debug.LogWarning($"[NetworkInventory] ItemTemplateManager chưa sẵn sàng!");
            return null;
        }

        var template = ItemTemplateManager.Instance.GetItemTemplate(itemID);
        if (template == null)
        {
            Debug.LogWarning($"[NetworkInventory] ItemID {itemID} not found in ItemTemplateManager!");
        }
        return template;
    }

    /// <summary>
    /// DEPRECATED: Giữ lại để tương thích code cũ
    /// </summary>
    private ItemData GetItemDataByID(int itemID)
    {
        Debug.LogWarning($"[NetworkInventory] GetItemDataByID() is deprecated! Use GetItemTemplate() instead.");
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
            if (slot.itemID == itemID)
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

    /// <summary>
    /// Lấy raw slot data từ NetworkVariable (không cần ItemData ScriptableObject)
    /// Dùng khi ItemData chưa được load hoặc không tồn tại
    /// </summary>
    public InventorySlotData GetRawSlotData(int slotIndex)
    {
        var data = networkInventoryData.Value;
        if (data.slotData == null || slotIndex < 0 || slotIndex >= data.slotData.Length)
        {
            return new InventorySlotData { itemID = 0, quantity = 0 };
        }
        return data.slotData[slotIndex];
    }

    /// <summary>
    /// ServerRpc: Thêm item vào inventory và sync với DB (dùng cho test với phím Q)
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddItemWithDBSyncServerRpc(int itemTemplateId, string itemCode, string iconId, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        Debug.Log($"[NetworkInventory] AddItemWithDBSyncServerRpc: itemCode={itemCode}, quantity={quantity}");

        // 1. Thêm item vào NetworkVariable (để sync với clients)
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

        // Tìm slot trống
        int emptySlotIndex = -1;
        for (int i = 0; i < maxSlots; i++)
        {
            if (currentData.slotData[i].itemID == 0)
            {
                emptySlotIndex = i;
                break;
            }
        }

        if (emptySlotIndex == -1)
        {
            Debug.LogWarning("[NetworkInventory] Inventory đầy! Không thể thêm item.");
            return;
        }

        // Tạo một itemID tạm thời dựa trên itemTemplateId (hoặc có thể map từ ItemDatabase)
        int itemID = itemTemplateId; // Tạm thời dùng itemTemplateId làm itemID

        // Thêm vào slot
        currentData.slotData[emptySlotIndex] = new InventorySlotData 
        { 
            itemID = itemID, 
            quantity = quantity 
        };
        
        // ✅ Update NetworkVariable - Điều này sẽ trigger OnInventoryDataChanged callback
        networkInventoryData.Value = currentData;

        Debug.Log($"[NetworkInventory] Đã thêm {quantity}x {itemCode} vào slot {emptySlotIndex}");
        Debug.Log($"[NetworkInventory] NetworkVariable updated - OnInventoryDataChanged sẽ được trigger tự động!");

        // ✅ Sync với DB ngay lập tức sau khi thêm item
        Debug.Log($"[NetworkInventory] Đang sync item vào DB...");
        SyncInventoryToDB(itemTemplateId, itemCode, iconId, quantity);

        // 3. Notify clients
        OnItemAddedClientRpc(itemID, quantity);
        
        Debug.Log($"[NetworkInventory] ✅ AddItemWithDBSyncServerRpc hoàn thành!");
    }

    /// <summary>
    /// Sync inventory với DB qua HTTP API
    /// </summary>
    private void SyncInventoryToDB(int itemTemplateId, string itemCode, string iconId, int quantity)
    {
        // ✅ FIX: Lấy playerId từ GameManager (in-memory) thay vì PlayerPrefs
        // PlayerPrefs bị shared giữa ParrelSync host/clone
        int playerId = 0;
        
        // Ưu tiên 1: GameManager (in-memory)
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerId = GameManager.Instance.GetPlayerData().user_id;
        }
        
        // Ưu tiên 2: ServerPlayerDataManager (host-side)
        if (playerId == 0 && IsServer && ServerPlayerDataManager.Instance != null)
        {
            var playerData = ServerPlayerDataManager.Instance.GetPlayerDataByClientId(OwnerClientId);
            if (playerData != null)
                playerId = playerData.user_id;
        }
        
        // Fallback: PlayerPrefs
        if (playerId == 0)
            playerId = PlayerPrefs.GetInt("USER_ID", 0);
        
        if (playerId == 0)
        {
            Debug.LogWarning("[NetworkInventory] SyncInventoryToDB: playerId = 0, không thể sync với DB!");
            return;
        }

        // Tạo request
        var item = new APIClient.AddInventoryItemRequest
        {
            itemTemplateId = itemTemplateId,
            itemCode = itemCode,
            iconId = iconId,
            quantity = quantity
        };

        var items = new APIClient.AddInventoryItemRequest[] { item };

        // Gọi API
        if (APIClient.Instance != null)
        {
            APIClient.Instance.AddItemsToInventory(
                playerId,
                items,
                (response) =>
                {
                    Debug.Log($"[NetworkInventory] ✅ Đã sync inventory với DB thành công! Response: {response}");
                },
                (error) =>
                {
                    Debug.LogError($"[NetworkInventory] ❌ Lỗi khi sync inventory với DB: {error}");
                }
            );
        }
        else
        {
            Debug.LogWarning("[NetworkInventory] APIClient.Instance is null! Không thể sync với DB.");
        }
    }

    /// <summary>
    /// Load inventory từ DB khi player spawn
    /// </summary>
    private void LoadInventoryFromDB()
    {
        if (!IsServer)
        {
            Debug.LogWarning("[NetworkInventory] LoadInventoryFromDB: Chỉ server mới load được!");
            return;
        }

        // Get playerId từ ServerPlayerDataManager dựa vào OwnerClientId
        int playerId = 0;
        
        if (ServerPlayerDataManager.Instance != null)
        {
            var playerData = ServerPlayerDataManager.Instance.GetPlayerDataByClientId(OwnerClientId);
            if (playerData != null)
            {
                playerId = playerData.player_id;
            }
        }
        
        // Fallback 1: GameManager (in-memory, không bị shared giữa host/clone)
        if (playerId == 0 && GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerId = GameManager.Instance.GetPlayerData().user_id;
            Debug.Log($"[NetworkInventory] Fallback to GameManager: playerId = {playerId}");
        }
        
        // Fallback 2: PlayerPrefs (có thể bị shared giữa ParrelSync host/clone)
        if (playerId == 0)
        {
            playerId = PlayerPrefs.GetInt("USER_ID", 0);
            Debug.LogWarning($"[NetworkInventory] Fallback to PlayerPrefs: playerId = {playerId} (có thể không chính xác khi dùng ParrelSync!)");
        }
        
        if (playerId == 0)
        {
            Debug.LogWarning("[NetworkInventory] LoadInventoryFromDB: playerId = 0, không thể load!");
            return;
        }

        Debug.Log($"[NetworkInventory] Đang load inventory từ DB cho player {playerId} (OwnerClientId={OwnerClientId})...");

        // Gọi API để load player data
        if (APIClient.Instance != null)
        {
            APIClient.Instance.LoadPlayerData(
                playerId,
                (response) =>
                {
                    if (response.inventory != null && response.inventory.Length > 0)
                    {
                        Debug.Log($"[NetworkInventory] ✅ Load thành công {response.inventory.Length} items từ DB!");
                        PopulateInventoryFromDB(response.inventory);
                    }
                    else
                    {
                        Debug.Log("[NetworkInventory] Inventory trong DB trống (player mới).");
                    }
                },
                (error) =>
                {
                    Debug.LogError($"[NetworkInventory] ❌ Lỗi khi load inventory từ DB: {error}");
                }
            );
        }
        else
        {
            Debug.LogWarning("[NetworkInventory] APIClient.Instance is null!");
        }
    }

    /// <summary>
    /// Populate NetworkInventoryData từ DB data
    /// </summary>
    private void PopulateInventoryFromDB(InventoryItem[] dbItems)
    {
        if (!IsServer) return;

        Debug.Log($"[NetworkInventory] PopulateInventoryFromDB: Đang populate {dbItems.Length} items...");

        var currentData = networkInventoryData.Value;
        
        // Đảm bảo slotData đã được khởi tạo
        if (currentData.slotData == null || currentData.slotData.Length == 0)
        {
            currentData.slotData = new InventorySlotData[maxSlots];
            for (int i = 0; i < maxSlots; i++)
            {
                currentData.slotData[i] = new InventorySlotData { itemID = 0, quantity = 0 };
            }
        }

        // Populate từ DB data
        foreach (var dbItem in dbItems)
        {
            int slotIndex = dbItem.slotIndex;
            
            if (slotIndex < 0 || slotIndex >= maxSlots)
            {
                Debug.LogWarning($"[NetworkInventory] Invalid slotIndex {slotIndex}, skipping...");
                continue;
            }

            // Dùng itemTemplateId làm itemID
            int itemID = dbItem.itemTemplateId;
            
            if (itemID > 0 && dbItem.quantity > 0)
            {
                currentData.slotData[slotIndex] = new InventorySlotData
                {
                    itemID = itemID,
                    quantity = dbItem.quantity
                };
                
                Debug.Log($"[NetworkInventory] Loaded slot {slotIndex}: itemID={itemID}, qty={dbItem.quantity}");
            }
        }

        // Update NetworkVariable
        networkInventoryData.Value = currentData;
        
        // IMPORTANT: Force deserialize vào localInventory ngay lập tức
        // Vì OnInventoryDataChanged callback có thể không được trigger kịp thời
        DeserializeInventory(currentData);
        
        Debug.Log($"[NetworkInventory] ✅ Đã populate inventory từ DB, triggering OnInventoryChanged event...");
        
        // Trigger OnInventoryChanged manually để refresh UI
        OnInventoryChanged?.Invoke();
    }
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
    public int itemID; // Đổi từ ItemData sang itemID
    public int quantity;
}
