using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

// NetworkInventory - Hệ thống túi đồ với network synchronization
// Sử dụng NetworkVariable để sync inventory giữa các clients
[RequireComponent(typeof(NetworkObject))]
public class NetworkInventory : NetworkBehaviour
{
    private const int PickupTraceItemId = 27;

    [Header("Inventory Settings")]
    [Tooltip("Số lượng slot tối đa trong inventory")]
    [SerializeField] private int maxSlots = 20;

    [Header("Debug")]
    [SerializeField] private bool verboseInventoryLogs = false;
    
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

    private static bool ShouldTracePickupItem(int itemID) => itemID == PickupTraceItemId;

    private static void TracePickup(int itemID, string message)
    {
        if (ShouldTracePickupItem(itemID))
            { /* [NetworkInventory] {message} */ }
    }

    private void VerboseLog(string message)
    {
        if (verboseInventoryLogs)
            { /* Ghi nhận: message */ }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        VerboseLog($"[NetworkInventory] OnNetworkSpawn IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}, OwnerClientId={OwnerClientId}");
        
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
            VerboseLog($"[NetworkInventory] Server loading inventory from DB. OwnerClientId={OwnerClientId}");
            StartCoroutine(LoadInventoryFromDBDelayed());
        }
        
        // Initialize local cache
        if (networkInventoryData.Value.slotData != null)
        {
            DeserializeInventory(networkInventoryData.Value);
            VerboseLog($"[NetworkInventory] Deserialized inventory on spawn. UsedSlots={GetUsedSlots()}");
        }
        
        // 🔥 CLIENT: Trigger OnInventoryChanged sau một delay để đảm bảo Bridge đã subscribe
        if (IsClient && !IsServer)
        {
            VerboseLog("[NetworkInventory] Client scheduling delayed OnInventoryChanged trigger.");
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
        
        VerboseLog("[NetworkInventory] Manual OnInventoryChanged trigger on client.");
        OnInventoryChanged?.Invoke();
    }

    // Wrapper cho JSON serialization mảng InventoryItem qua RPC.
    [System.Serializable]
    public class InventoryJsonWrapper
    {
        public InventoryItem[] items;
        public int bag_slots;
        public int gold;
        public int silver;
        public BagEquippedItemData[] bag_equipped_items;
    }

    // Client gọi lên host để yêu cầu dữ liệu inventory.
    // Host fetch DB rồi gửi JSON về đúng client đó qua SendInventoryDataClientRpc.
    [ServerRpc(RequireOwnership = false)]
    public void RequestInventoryDataServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        VerboseLog($"[NetworkInventory] RequestInventoryDataServerRpc clientId={senderClientId}");

        int playerId = ResolveInventoryApiPlayerId(senderClientId);

        if (playerId == 0)
        {
            { /* Cảnh báo: RequestInventoryDataServerRpc: Không thể resolve playerId cho clientId={senderClientId} */ }
            return;
        }

        ulong capturedClientId = senderClientId;
        VerboseLog("[NetworkInventory] RequestInventoryDataServerRpc fetch inventory from DB.");
        StartCoroutine(PushInventoryDataToClientDirect(playerId, capturedClientId));
    }

    // Client gọi lên host để yêu cầu sắp xếp inventory (gom item về phía trước).
    // Host sort DB → fetch lại dữ liệu mới → gửi về đúng client đó qua SendInventoryDataClientRpc.
    [ServerRpc(RequireOwnership = false)]
    public void RequestSortInventoryServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        VerboseLog($"[NetworkInventory] RequestSortInventoryServerRpc clientId={senderClientId}");

        int playerId = ResolveInventoryApiPlayerId(senderClientId);

        if (playerId == 0)
        {
            { /* Cảnh báo: RequestSortInventoryServerRpc: Không thể resolve playerId cho clientId={senderClientId} */ }
            return;
        }

        ulong capturedClientId = senderClientId;
        StartCoroutine(SortInventoryDirect(playerId, capturedClientId));
    }

    // Host gửi JSON inventory về đúng client đã yêu cầu.
    // InventoryNetworkBridge phía client nhận và cập nhật cache + UI.
    [ClientRpc]
    public void SendInventoryDataClientRpc(string inventoryJson, ClientRpcParams rpcParams = default)
    {
        VerboseLog($"[NetworkInventory] Client received inventory data from host ({inventoryJson?.Length ?? 0} chars)");
        var bridge = FindObjectOfType<InventoryNetworkBridge>(true);
        if (bridge != null)
            bridge.OnReceivedInventoryDataFromHost(inventoryJson);
        else
            { /* Cảnh báo: SendInventoryDataClientRpc: InventoryNetworkBridge không tìm thấy */ }
    }

    public override void OnNetworkDespawn()
    {
        networkInventoryData.OnValueChanged -= OnInventoryDataChanged;
        base.OnNetworkDespawn();
    }

    // Callback khi NetworkVariable thay đổi
    private void OnInventoryDataChanged(NetworkInventoryData oldData, NetworkInventoryData newData)
    {
        VerboseLog($"[NetworkInventory] OnInventoryDataChanged IsServer={IsServer}, IsClient={IsClient}, IsOwner={IsOwner}");
        
        int itemCount = 0;
        if (newData.slotData != null)
        {
            foreach (var slot in newData.slotData)
            {
                if (slot.itemID > 0) itemCount++;
            }
        }
        VerboseLog($"[NetworkInventory] Network data item count={itemCount}");
        
        DeserializeInventory(newData);
        
        OnInventoryChanged?.Invoke();
        VerboseLog("[NetworkInventory] OnInventoryChanged invoked.");
    }

    // Deserialize NetworkInventoryData thành local dictionary
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

    // ServerRpc: Thêm item vào inventory
    [ServerRpc(RequireOwnership = false)]
    public void AddItemServerRpc(int itemID, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        TryAddItemInternal(itemID, quantity, out _);
    }

    public bool TryAddItemOnServer(int itemID, int quantity)
    {
        if (!IsServer)
        {
            { /* Cảnh báo: TryAddItemOnServer chỉ được gọi trên server */ }
            return false;
        }

        bool added = TryAddItemInternal(itemID, quantity, out int addedQty);
        if (added && addedQty > 0)
        {
            // Quest collect hook
            var playerSync = GetComponent<NetworkPlayerDataSync>();
            int dbPlayerId = playerSync != null ? playerSync.networkPlayerId.Value : 0;
            if (dbPlayerId > 0)
                QuestProgressReporter.Report(this, dbPlayerId, QuestProgressReporter.ProgressType.Collect, itemID, addedQty,
                    () => playerSync?.NotifyQuestProgressOnServer("collect"));
        }
        return added;
    }

    private bool TryAddItemInternal(int itemID, int quantity, out int addedQuantity)
    {
        addedQuantity = 0;

        if (quantity <= 0)
        {
            { /* Cảnh báo: Bỏ qua AddItem vì quantity={quantity} không hợp lệ */ }
            return false;
        }

        var template = GetItemTemplate(itemID);
        if (template == null)
        {
            { /* Cảnh báo: ItemID {itemID} không tồn tại */ }
            return false;
        }

        int remainingQuantity = quantity;

        // Keep Netcode slot capacity aligned with the player's current bag size.
        var currentData = networkInventoryData.Value;
        currentData.slotData = EnsureSlotCapacity(currentData.slotData, maxSlots);

        TracePickup(
            itemID,
            $"NetAddStart item={itemID} qty={quantity} stackable={template.stackable} maxStack={template.max_stack} maxSlots={maxSlots} slotDataLen={currentData.slotData.Length} usedSlots={CountUsedSlots(currentData.slotData)} stacks={BuildItemSlotSummary(currentData.slotData, itemID)}");

        // Fill existing stacks first, capped by max_stack.
        if (template.stackable)
        {
            for (int i = 0; i < currentData.slotData.Length && remainingQuantity > 0; i++)
            {
                var slot = currentData.slotData[i];
                if (slot.itemID == itemID)
                {
                    int spaceAvailable = template.max_stack - slot.quantity;
                    if (spaceAvailable <= 0)
                    {
                        TracePickup(itemID, $"StackFull slot={i} qty={slot.quantity} maxStack={template.max_stack}");
                        continue;
                    }

                    int addAmount = Mathf.Min(remainingQuantity, spaceAvailable);
                    slot.quantity += addAmount;
                    remainingQuantity -= addAmount;
                    currentData.slotData[i] = slot;
                    TracePickup(itemID, $"FillStack slot={i} add={addAmount} newQty={slot.quantity} remaining={remainingQuantity}");
                }
            }
        }

        // Put any remainder into empty slots.
        if (remainingQuantity > 0)
        {
            for (int i = 0; i < currentData.slotData.Length && remainingQuantity > 0; i++)
            {
                var slot = currentData.slotData[i];
                if (slot.itemID == 0)
                {
                    int addAmount = template.stackable
                        ? Mathf.Min(remainingQuantity, template.max_stack)
                        : 1;

                    slot.itemID = itemID;
                    slot.quantity = addAmount;
                    remainingQuantity -= addAmount;
                    currentData.slotData[i] = slot;
                    TracePickup(itemID, $"UseEmptySlot slot={i} add={addAmount} remaining={remainingQuantity}");
                }
            }
        }

        if (remainingQuantity < quantity)
        {
            addedQuantity = quantity - remainingQuantity;
            networkInventoryData.Value = currentData;
            OnItemAddedClientRpc(itemID, addedQuantity);
            TracePickup(itemID, $"NetAddSuccess added={addedQuantity} remaining={remainingQuantity} usedSlots={CountUsedSlots(currentData.slotData)} stacks={BuildItemSlotSummary(currentData.slotData, itemID)}");

            // Persist the accepted quantity to DB after the NetworkVariable update.
            SyncInventoryToDB(itemID, template.code, template.icon_id, addedQuantity);
            return true;
        }

        // Nothing could be added; report the capacity state once.
        if (remainingQuantity > 0)
        {
            { /* Cảnh báo: [NetworkInventory] NetAddFail item={itemID} remaining={remainingQuantity} maxSlots={maxSlots} slotDataLen={currentData.slotData.Length} usedSlots={CountUsedSlots(currentData.slotData)} stacks={BuildItemSlotSummary(currentData.slotData, itemID)} */ }
        }

        return false;
    }

    // ServerRpc: Xóa item khỏi inventory
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
        { /* Removed {quantity}x from slot {slotIndex} */ }
    }

    // ServerRpc: Sử dụng item (consumable) — NGO-only path, giảm NGO cache inventory.
    // Dùng khi KHÔNG có REST API (testing/offline). Production dùng ApplyConsumableStatServerRpc.
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
                currentData.slotData[i] = new InventorySlotData { itemID = 0, quantity = 0 };
        }

        var slot = currentData.slotData[slotIndex];
        if (slot.itemID == 0) return;

        var template = GetItemTemplate(slot.itemID);
        if (template == null) return;

        // Chỉ xử lý consumable (type 21-29) và bag item (type 30)
        int itemType = template.type;
        bool isConsumable = itemType >= 21 && itemType <= 29;
        bool isBagItem    = itemType == 30;
        if (!isConsumable && !isBagItem) return;

        // Áp dụng effect
        ApplyItemEffect(slot.itemID, itemType, rpcParams.Receive.SenderClientId);

        // Giảm quantity hoặc xóa item khỏi NGO cache
        int oldQuantity = slot.quantity;
        slot.quantity--;
        if (slot.quantity <= 0)
        {
            slot.itemID  = 0;
            slot.quantity = 0;
        }
        currentData.slotData[slotIndex] = slot;
        networkInventoryData.Value = currentData;

        OnItemRemovedClientRpc(slotIndex, oldQuantity, slot.quantity);
    }

    // ServerRpc: Áp dụng heal tick mỗi giây từ HpRestoreOverTime / MpRestoreOverTime buff.
    // Gọi từ InventoryNetworkBridge khi ActiveBuffManager.OnHealTick fire.
    [ServerRpc(RequireOwnership = false)]
    public void ApplyHealTickServerRpc(int hpHeal, int mpHeal, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        var dataSync = GetComponent<NetworkPlayerDataSync>();
        if (dataSync == null) return;

        if (hpHeal > 0)
            dataSync.networkHp.Value = Mathf.Min(
                dataSync.networkMaxHp.Value,
                dataSync.networkHp.Value + hpHeal);

        if (mpHeal > 0)
            dataSync.networkMp.Value = Mathf.Min(
                dataSync.networkMaxMp.Value,
                dataSync.networkMp.Value + mpHeal);

        { /* 💉 Heal tick: +{hpHeal} HP / +{mpHeal} MP */ }
    }

    // ServerRpc: Đặt HP/MP về giá trị chính xác từ REST API (server-authoritative).
    // Gọi từ ItemUseHandler sau khi sử dụng item hồi phục HP/MP tức thì.
    [ServerRpc(RequireOwnership = false)]
    public void ApplySyncHpMpServerRpc(int syncHp, int syncMp, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        var dataSync = GetComponent<NetworkPlayerDataSync>();
        if (dataSync == null) return;

        if (syncHp > 0)
            dataSync.networkHp.Value = Mathf.Min(dataSync.networkMaxHp.Value, syncHp);

        if (syncMp > 0)
            dataSync.networkMp.Value = Mathf.Min(dataSync.networkMaxMp.Value, syncMp);

        { /* Sync HP={syncHp} MP={syncMp} từ REST API */ }
    }

    // ServerRpc: CHỈ áp dụng stat effect (HP/MP) của consumable — KHÔNG giảm inventory.
    // Gọi sau khi REST API đã persist việc tiêu thụ item lên DB.
    [ServerRpc(RequireOwnership = false)]
    public void ApplyConsumableStatServerRpc(int templateId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        var template = ItemTemplateManager.Instance != null
            ? ItemTemplateManager.Instance.GetItemTemplate(templateId)
            : null;
        if (template == null)
        {
            { /* Cảnh báo: ApplyConsumableStatServerRpc: template {templateId} not found */ }
            return;
        }

        int itemType = template.type;
        if (itemType < 21 || itemType > 29)
        {
            { /* Cảnh báo: ApplyConsumableStatServerRpc: item type {itemType} is not consumable (21-29) */ }
            return;
        }

        ApplyItemEffect(templateId, itemType, rpcParams.Receive.SenderClientId);
    }

    // Áp dụng HP/MP heal lên player. Chạy trên server.
    // Với consumable type 22 (HP) / 23 (MP) – dùng giá trị từ ItemData ScriptableObject.
    // Với type 24 (timed buff) – chỉ notify client; timed buff quản lý bởi ActiveBuffManager.
    private void ApplyItemEffect(int itemID, int itemType, ulong senderClientId)
    {
        var itemData     = ItemManager.Instance != null ? ItemManager.Instance.GetItemData(itemID) : null;
        int healValue    = itemData != null && itemData.value > 0 ? itemData.value : 50;
        var playerHealth = GetComponent<NetworkPlayerHealth>();
        var dataSync     = GetComponent<NetworkPlayerDataSync>();

        // type 22 = HP Potion
        if (itemType == 22 && dataSync != null)
        {
            dataSync.networkHp.Value = Mathf.Min(dataSync.networkMaxHp.Value,
                dataSync.networkHp.Value + healValue);
            { /* 💊 +{healValue} HP (type=22) */ }
        }
        // type 23 = MP Potion
        else if (itemType == 23 && dataSync != null)
        {
            dataSync.networkMp.Value = Mathf.Min(dataSync.networkMaxMp.Value, dataSync.networkMp.Value + healValue);
            { /* 🔵 +{healValue} MP (type=23) */ }
        }
        // type 24 = Timed buff — buff đã được server persist trong active_buffs;
        //   chỉ cần notify client reload buff HUD.
        else if (itemType == 24)
        {
            { /* ✨ Timed buff item (type=24, id={itemID})  client sẽ refresh buff HUD */ }
            // Client tự handle qua UseItemResponse.active_buffs từ REST API
        }
        // Fallback (generic consumable): ưu tiên sync vào dataSync để HP bar/UI luôn cập nhật.
        else if (dataSync != null)
        {
            dataSync.networkHp.Value = Mathf.Min(dataSync.networkMaxHp.Value,
                dataSync.networkHp.Value + healValue);
            { /* 💊 +{healValue} HP fallback qua dataSync (type={itemType}) */ }
        }
        else if (playerHealth != null)
        {
            playerHealth.HealServerRpc(healValue);
            { /* 💊 +{healValue} HP fallback (type={itemType}) */ }
        }

        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } }
        };
        OnItemUsedClientRpc(itemID, clientParams);
    }

    // ServerRpc: Cập nhật % bonus buff vào NetworkPlayerDataSync.
    // Gọi sau khi client nhận active_buffs từ REST API và muốn sync lên NGO.
    // geneExpBonusPct, expBonusPct, phucBonusPct, attackBonusPct, defenseBonusPct = tổng % (sum của tất cả buff đang active cùng loại).
    [ServerRpc(RequireOwnership = false)]
    public void SyncBuffBonusesServerRpc(int geneExpBonusPct, int expBonusPct, int phucBonusPct,
                                          int attackBonusPct, int defenseBonusPct,
                                          ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        var dataSync = GetComponent<NetworkPlayerDataSync>();
        if (dataSync == null) return;
        dataSync.networkGeneExpBonusPct.Value = geneExpBonusPct;
        dataSync.networkExpBonusPct.Value     = expBonusPct;
        dataSync.networkPhucBonusPct.Value    = phucBonusPct;
        dataSync.networkAttackBonusPct.Value  = attackBonusPct;
        dataSync.networkDefenseBonusPct.Value = defenseBonusPct;
        { /* 🎯 Sync buff bonuses: GeneEXP+{geneExpBonusPct}% EXP+{expBonusPct}% Phuc+{phucBonusPct}% ATK+{attackBonusPct}% DEF+{defenseBonusPct}% */ }
    }

    // ClientRpc: Notify về item được thêm
    [ClientRpc]
    private void OnItemAddedClientRpc(int itemID, int quantity)
    {
        var template = GetItemTemplate(itemID);
        string itemName = template?.name ?? $"item_id={itemID}";
        TracePickup(itemID, $"ClientItemAdded item={itemID} qty={quantity} name={itemName}");

        // Force refresh UI in case OnValueChanged has not fired yet.
        DeserializeInventory(networkInventoryData.Value);
        OnInventoryChanged?.Invoke();
    }

    // ClientRpc: Notify về item bị xóa
    [ClientRpc]
    private void OnItemRemovedClientRpc(int slotIndex, int oldQuantity, int newQuantity)
    {
        OnItemQuantityChanged?.Invoke(slotIndex, oldQuantity, newQuantity);
        if (newQuantity == 0)
        {
            OnItemRemoved?.Invoke(slotIndex, oldQuantity);
        }
    }

    // ClientRpc: Notify về item được sử dụng
    [ClientRpc]
    private void OnItemUsedClientRpc(int itemID, ClientRpcParams clientRpcParams = default)
    {
        // Có thể play sound/effect ở đây
        { /* Item {itemID} được sử dụng */ }
    }

    // Lấy ItemTemplateDto từ ItemID (dùng ItemTemplateManager mới)
    private ItemTemplateDto GetItemTemplate(int itemID)
    {
        if (ItemTemplateManager.Instance == null)
        {
            // Dedicated server: tự tạo singleton nếu chưa có
            if (IsServer)
            {
                ItemTemplateManager.EnsureInstance();
                { /* Cảnh báo: ItemTemplateManager vừa được tạo tự động, đang load */ }
            }
            else
            {
                { /* Cảnh báo: ItemTemplateManager chưa sẵn sàng */ }
            }
            return null;
        }

        if (!ItemTemplateManager.Instance.IsLoaded())
        {
            { /* Cảnh báo: ItemTemplateManager chưa load xong templates */ }
            return null;
        }

        var template = ItemTemplateManager.Instance.GetItemTemplate(itemID);
        if (template == null)
        {
            { /* Cảnh báo: ItemID {itemID} not found in ItemTemplateManager */ }
        }
        return template;
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.
    public void AddItem(int itemID, int quantity)
    {
        if (IsServer)
        {
            TryAddItemInternal(itemID, quantity, out _);
            return;
        }

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

    private int CountUsedSlots(InventorySlotData[] slots)
    {
        if (slots == null) return 0;
        int count = 0;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].itemID > 0 && slots[i].quantity > 0)
                count++;
        }
        return count;
    }

    private string BuildItemSlotSummary(InventorySlotData[] slots, int itemID)
    {
        if (slots == null || slots.Length == 0)
            return "none";

        var parts = new List<string>();
        for (int i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot.itemID == itemID && slot.quantity > 0)
                parts.Add($"slot={i},qty={slot.quantity}");
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : "none";
    }

    private void ApplyBagSlotLimit(int bagSlots)
    {
        if (bagSlots > maxSlots)
        {
            int oldMaxSlots = maxSlots;
            maxSlots = bagSlots;
            { /* [NetworkInventory] Apply bag_slots from DB: {oldMaxSlots} -> {maxSlots} */ }
        }
    }

    private InventorySlotData[] CreateEmptySlotArray(int slotCount)
    {
        var slots = new InventorySlotData[slotCount];
        for (int i = 0; i < slotCount; i++)
            slots[i] = new InventorySlotData { itemID = 0, quantity = 0 };
        return slots;
    }

    private InventorySlotData[] EnsureSlotCapacity(InventorySlotData[] slots, int slotCount)
    {
        if (slots == null || slots.Length == 0)
            return CreateEmptySlotArray(slotCount);
        if (slots.Length >= slotCount)
            return slots;

        var expanded = CreateEmptySlotArray(slotCount);
        for (int i = 0; i < slots.Length; i++)
            expanded[i] = slots[i];
        return expanded;
    }

    // Lấy raw slot data từ NetworkVariable (không cần ItemData ScriptableObject)
    // Dùng khi ItemData chưa được load hoặc không tồn tại
    public InventorySlotData GetRawSlotData(int slotIndex)
    {
        var data = networkInventoryData.Value;
        if (data.slotData == null || slotIndex < 0 || slotIndex >= data.slotData.Length)
        {
            return new InventorySlotData { itemID = 0, quantity = 0 };
        }
        return data.slotData[slotIndex];
    }

    // ServerRpc: Thêm item vào inventory VÀ sync với DB (1 item riêng lẻ)
    // ⚠️ KHÔNG dùng khi thêm nhiều items cùng lúc → sẽ bị race condition!
    // Dùng AddItemWithoutDBSyncServerRpc + SyncBatchToDB thay thế.
    [ServerRpc(RequireOwnership = false)]
    public void AddItemWithDBSyncServerRpc(int itemTemplateId, string itemCode, string iconId, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        { /* AddItemWithDBSyncServerRpc: itemCode={itemCode}, quantity={quantity} */ }

        AddItemToNetworkVariable(itemTemplateId, itemCode, iconId, quantity);

        // ✅ Sync với DB ngay lập tức sau khi thêm item
        { /* Đang sync item vào DB */ }
        SyncInventoryToDB(itemTemplateId, itemCode, iconId, quantity);
        
        { /* AddItemWithDBSyncServerRpc hoàn thành */ }
    }

    // ServerRpc: Thêm item vào NetworkVariable KHÔNG sync DB.
    // Dùng khi cần thêm nhiều items rồi batch sync 1 lần sau.
    [ServerRpc(RequireOwnership = false)]
    public void AddItemWithoutDBSyncServerRpc(int itemTemplateId, string itemCode, string iconId, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        { /* AddItemWithoutDBSyncServerRpc: itemCode={itemCode}, quantity={quantity} */ }
        AddItemToNetworkVariable(itemTemplateId, itemCode, iconId, quantity);
        { /* AddItemWithoutDBSyncServerRpc hoàn thành */ }
    }

    // Helper: Thêm item vào NetworkVariable (không sync DB)
    private void AddItemToNetworkVariable(int itemTemplateId, string itemCode, string iconId, int quantity)
    {
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
            { /* Cảnh báo: Inventory đầy! Không thể thêm item */ }
            return;
        }

        int itemID = itemTemplateId;

        currentData.slotData[emptySlotIndex] = new InventorySlotData 
        { 
            itemID = itemID, 
            quantity = quantity 
        };
        
        networkInventoryData.Value = currentData;

        { /* Đã thêm {quantity}x {itemCode} vào slot {emptySlotIndex} */ }

        OnItemAddedClientRpc(itemID, quantity);
    }

    // Sync inventory với DB qua HTTP API
    private void SyncInventoryToDB(int itemTemplateId, string itemCode, string iconId, int quantity)
    {
        int playerId = ResolveInventoryApiPlayerId(OwnerClientId);

        if (playerId == 0)
        {
            { /* Cảnh báo: SyncInventoryToDB: playerId=0, OwnerClientId={OwnerClientId}  không thể sync DB */ }
            return;
        }

        TracePickup(itemTemplateId, $"DbSyncStart owner={OwnerClientId} playerId={playerId} qty={quantity}");

        // Chỉ gửi itemTemplateId + quantity — server tự tra item_template
        var item = new APIClient.AddInventoryItemRequest
        {
            itemTemplateId = itemTemplateId,
            quantity = quantity
        };

        var items = new APIClient.AddInventoryItemRequest[] { item };

        // Lấy JWT của đúng client (không dùng JWT của HOST khi sync cho CLIENT)
        string clientJwt = ResolveClientJwt(OwnerClientId);

        if (string.IsNullOrEmpty(clientJwt))
        {
            { /* Cảnh báo: APIClient.Instance is null và không có JWT client! Không thể sync với DB */ }
            return;
        }

        StartCoroutine(SyncInventoryToApiDirect(playerId, items, clientJwt, OwnerClientId));
    }

    // Load inventory từ DB khi player spawn
    private void LoadInventoryFromDB()
    {
        if (!IsServer)
        {
            { /* Cảnh báo: LoadInventoryFromDB: Chỉ server mới load được */ }
            return;
        }

        int playerId = ResolveInventoryApiPlayerId(OwnerClientId);
        
        if (playerId == 0)
        {
            { /* Cảnh báo: LoadInventoryFromDB: playerId = 0, không thể load */ }
            return;
        }

        TracePickup(PickupTraceItemId, $"DBLoadStart playerId={playerId} owner={OwnerClientId}");
        StartCoroutine(LoadInventoryFromApiDirect(playerId));
    }

    private int ResolveInventoryApiPlayerId(ulong clientId)
    {
        if (ZonePlayerSessionManager.Instance != null)
        {
            string sessionUserId = ZonePlayerSessionManager.Instance.GetPlayerId(clientId);
            if (int.TryParse(sessionUserId, out int zoneUserId) && zoneUserId > 0)
                return zoneUserId;
        }

        if (ServerPlayerDataManager.Instance != null)
        {
            var playerData = ServerPlayerDataManager.Instance.GetPlayerDataByClientId(clientId);
            if (playerData != null && playerData.player_id > 0)
                return playerData.player_id;
        }

        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            int gameManagerPlayerId = GameManager.Instance.GetPlayerData().player_id;
            if (gameManagerPlayerId > 0)
                return gameManagerPlayerId;
        }

        int prefsPlayerId = PlayerPrefs.GetInt("PLAYER_ID", 0);
        if (prefsPlayerId > 0)
            return prefsPlayerId;

        int prefsUserId = PlayerPrefs.GetInt("USER_ID", 0);
        if (prefsUserId > 0)
        {
            { /* Cảnh báo: ResolveInventoryApiPlayerId: fallback USER_ID={prefsUserId} cho clientId={clientId}. Kiểm tra luồng set PLAYER_ID/player_id */ }
            return prefsUserId;
        }

        return 0;
    }

    private int ResolveInventoryGeneSlot(ulong clientId)
    {
        if (ZonePlayerSessionManager.Instance != null)
        {
            int slot = ZonePlayerSessionManager.Instance.GetClientGeneSlot(clientId);
            if (slot == 2) return 2;
        }

        return PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1) == 2 ? 2 : 1;
    }

    private string ResolveClientJwt(ulong clientId)
    {
        string clientJwt = "";

        if (IsServer && ServerPlayerDataManager.Instance != null)
            clientJwt = ServerPlayerDataManager.Instance.GetClientJwt(clientId);

        if (string.IsNullOrEmpty(clientJwt) && ZonePlayerSessionManager.Instance != null)
            clientJwt = ZonePlayerSessionManager.Instance.GetClientJwt(clientId) ?? "";

        if (string.IsNullOrEmpty(clientJwt) && APIClient.Instance != null)
            clientJwt = APIClient.Instance.GetToken();

        if (string.IsNullOrEmpty(clientJwt))
            clientJwt = AuthHelper.GetToken();

        return clientJwt;
    }

    private IEnumerator SyncInventoryToApiDirect(int playerId, APIClient.AddInventoryItemRequest[] items, string clientJwt, ulong targetClientId)
    {
        string apiBase = ZoneRoomRegistry.Instance?.Config?.apiBaseUrl ?? ServerAddressConfig.Instance.ApiUrl;
        string url = $"{apiBase.TrimEnd('/')}/player/{playerId}/inventory/add";
        int traceItemId = items != null && items.Length > 0 ? items[0].itemTemplateId : 0;

        var requestBody = new APIClient.AddInventoryItemsRequest
        {
            items = items
        };

        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(JsonUtility.ToJson(requestBody));

        using var req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
        req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {clientJwt}");

        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string errorMessage = req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text)
                ? req.downloadHandler.text
                : req.error;
            { /* Lỗi: [NetworkInventory] DbSyncFail item={traceItemId} playerId={playerId} targetClient={targetClientId} error={errorMessage} */ }
            yield break;
        }

        TracePickup(traceItemId, $"DbSyncSuccess playerId={playerId} response={req.downloadHandler.text}");
        yield return PushInventoryDataToClientDirect(playerId, targetClientId);
    }

    private IEnumerator SortInventoryDirect(int playerId, ulong targetClientId)
    {
        string apiBase = ZoneRoomRegistry.Instance?.Config?.apiBaseUrl ?? ServerAddressConfig.Instance.ApiUrl;
        string apiKey = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey() ?? "dev-zone-key";
        string url = $"{apiBase.TrimEnd('/')}/player/{playerId}/inventory/sort";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes("{}");

        using var req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
        req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-Zone-Api-Key", apiKey);

        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string errorMessage = req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text)
                ? req.downloadHandler.text
                : req.error;
            { /* Lỗi: Direct sort inventory failed: {errorMessage} */ }
            yield break;
        }

        { /* Direct sort inventory thành công: {req.downloadHandler.text} */ }
        yield return PushInventoryDataToClientDirect(playerId, targetClientId);
    }

    private IEnumerator PushInventoryDataToClientDirect(int playerId, ulong targetClientId)
    {
        yield return FetchPlayerDataFromApiDirect(
            playerId,
            ResolveInventoryGeneSlot(targetClientId),
            freshItems =>
            {
                string json = JsonUtility.ToJson(new InventoryJsonWrapper
                {
                    items = freshItems?.inventory ?? System.Array.Empty<InventoryItem>(),
                    bag_slots = freshItems?.bag_slots ?? 20,
                    gold = freshItems?.gold ?? 0,
                    silver = freshItems?.silver ?? 0,
                    bag_equipped_items = freshItems?.bag_equipped_items ?? System.Array.Empty<BagEquippedItemData>()
                });
                var clientParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
                };
                SendInventoryDataClientRpc(json, clientParams);
                { /* 📡 Direct push {freshItems.Length} items về client {targetClientId} */ }
            },
            error => { /* Lỗi: Direct push inventory thất bại cho clientId={targetClientId}: {error} */ });
    }

    private IEnumerator FetchPlayerDataFromApiDirect(int playerId, int geneSlot, System.Action<PlayerDataResponse> onSuccess, System.Action<string> onError = null)
    {
        string apiBase = ZoneRoomRegistry.Instance?.Config?.apiBaseUrl ?? ServerAddressConfig.Instance.ApiUrl;
        string apiKey  = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey() ?? "dev-zone-key";
        string endpoint = geneSlot == 2 ? "data2" : "data";
        string url = $"{apiBase.TrimEnd('/')}/player/{playerId}/{endpoint}";

        using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
        req.SetRequestHeader("X-Zone-Api-Key", apiKey);
        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            string errorMessage = req.downloadHandler != null && !string.IsNullOrEmpty(req.downloadHandler.text)
                ? req.downloadHandler.text
                : req.error;
            onError?.Invoke(errorMessage);
            yield break;
        }

        try
        {
            var response = JsonUtility.FromJson<PlayerDataResponse>(req.downloadHandler.text);
            onSuccess?.Invoke(response);
        }
        catch (System.Exception ex)
        {
            onError?.Invoke($"Parse player data failed: {ex.Message}");
        }
    }

    private IEnumerator FetchInventoryFromApiDirect(int playerId, System.Action<InventoryItem[]> onSuccess, System.Action<string> onError = null)
    {
        yield return FetchPlayerDataFromApiDirect(
            playerId,
            ResolveInventoryGeneSlot(OwnerClientId),
            response => onSuccess?.Invoke(response?.inventory ?? System.Array.Empty<InventoryItem>()),
            onError);
    }

    // Fallback cho dedicated server: gọi API trực tiếp khi APIClient.Instance null.
    private IEnumerator LoadInventoryFromApiDirect(int playerId)
    {
        yield return FetchPlayerDataFromApiDirect(
            playerId,
            ResolveInventoryGeneSlot(OwnerClientId),
            response =>
            {
                InventoryItem[] items = response?.inventory ?? System.Array.Empty<InventoryItem>();
                int bagSlots = response != null && response.bag_slots > 0 ? response.bag_slots : maxSlots;
                PopulateInventoryFromDB(items, bagSlots);
            },
            error => { /* Cảnh báo: Direct API load failed: {error} */ });
    }

    // Rebuild Netcode inventory from DB data using the player's current bag slot limit.
    private void PopulateInventoryFromDB(InventoryItem[] dbItems, int bagSlots)
    {
        if (!IsServer) return;

        ApplyBagSlotLimit(bagSlots);

        var currentData = networkInventoryData.Value;
        currentData.slotData = CreateEmptySlotArray(maxSlots);

        foreach (var dbItem in dbItems)
        {
            int slotIndex = dbItem.slotIndex;
            
            if (slotIndex < 0 || slotIndex >= maxSlots)
            {
                { /* Cảnh báo: [NetworkInventory] DBLoadSkipSlot item={dbItem.itemTemplateId} slot={slotIndex} qty={dbItem.quantity} maxSlots={maxSlots} */ }
                continue;
            }

            int itemID = dbItem.itemTemplateId;
            
            if (itemID > 0 && dbItem.quantity > 0)
            {
                currentData.slotData[slotIndex] = new InventorySlotData
                {
                    itemID = itemID,
                    quantity = dbItem.quantity
                };
            }
        }

        networkInventoryData.Value = currentData;
        
        // Keep local cache in sync immediately; the NetworkVariable callback may arrive later.
        DeserializeInventory(currentData);
        TracePickup(PickupTraceItemId, $"DBLoadDone bagSlots={bagSlots} maxSlots={maxSlots} usedSlots={CountUsedSlots(currentData.slotData)} item27={BuildItemSlotSummary(currentData.slotData, PickupTraceItemId)}");
        OnInventoryChanged?.Invoke();
    }
}

// Struct để lưu trữ dữ liệu inventory trên network
[System.Serializable]
public struct NetworkInventoryData : INetworkSerializable
{
    public InventorySlotData[] slotData;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        // Đảm bảo slotData không null khi serialize (tránh NullRef trên ItemPickup prefab)
        if (serializer.IsWriter && slotData == null)
            slotData = System.Array.Empty<InventorySlotData>();
        serializer.SerializeValue(ref slotData);
    }
}

// Struct để lưu trữ thông tin slot trên network
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

// Class để lưu trữ thông tin slot local
[System.Serializable]
public class InventorySlot
{
    public int itemID; // Đổi từ ItemData sang itemID
    public int quantity;
}
