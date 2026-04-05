using System.Collections;
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

    /// <summary>Wrapper cho JSON serialization mảng InventoryItem qua RPC.</summary>
    [System.Serializable]
    public class InventoryJsonWrapper
    {
        public InventoryItem[] items;
    }

    /// <summary>
    /// Client gọi lên host để yêu cầu dữ liệu inventory.
    /// Host fetch DB rồi gửi JSON về đúng client đó qua SendInventoryDataClientRpc.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestInventoryDataServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[NetworkInventory] RequestInventoryDataServerRpc từ clientId={senderClientId}");

        int playerId = ResolveInventoryApiPlayerId(senderClientId);

        if (playerId == 0)
        {
            Debug.LogWarning($"[NetworkInventory] RequestInventoryDataServerRpc: Không thể resolve playerId cho clientId={senderClientId}");
            return;
        }

        ulong capturedClientId = senderClientId;
        if (APIClient.Instance != null)
        {
            APIClient.Instance.GetPlayerInventory(
                playerId,
                (items) =>
                {
                    string json = JsonUtility.ToJson(new InventoryJsonWrapper { items = items });
                    Debug.Log($"[NetworkInventory] Host trả dữ liệu inventory ({items.Length} items) về clientId={capturedClientId}");
                    var clientParams = new ClientRpcParams
                    {
                        Send = new ClientRpcSendParams { TargetClientIds = new[] { capturedClientId } }
                    };
                    SendInventoryDataClientRpc(json, clientParams);
                },
                (error) => Debug.LogError($"[NetworkInventory] Lỗi fetch inventory cho clientId={capturedClientId}: {error}")
            );
            return;
        }

        Debug.Log("[NetworkInventory] RequestInventoryDataServerRpc: APIClient null, dùng direct API fetch.");
        StartCoroutine(PushInventoryDataToClientDirect(playerId, capturedClientId));
    }

    /// <summary>
    /// Client gọi lên host để yêu cầu sắp xếp inventory (gom item về phía trước).
    /// Host sort DB → fetch lại dữ liệu mới → gửi về đúng client đó qua SendInventoryDataClientRpc.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestSortInventoryServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"[NetworkInventory] RequestSortInventoryServerRpc từ clientId={senderClientId}");

        int playerId = ResolveInventoryApiPlayerId(senderClientId);

        if (playerId == 0)
        {
            Debug.LogWarning($"[NetworkInventory] RequestSortInventoryServerRpc: Không thể resolve playerId cho clientId={senderClientId}");
            return;
        }

        ulong capturedClientId = senderClientId;
        if (APIClient.Instance == null)
        {
            Debug.Log("[NetworkInventory] RequestSortInventoryServerRpc: APIClient null, dùng direct API sort.");
            StartCoroutine(SortInventoryDirect(playerId, capturedClientId));
            return;
        }

        // Bước 1: sort trên DB
        APIClient.Instance.SortInventory(
            playerId,
            _ =>
            {
                Debug.Log($"[NetworkInventory] Sort thành công cho playerId={playerId}, đang fetch dữ liệu mới...");
                // Bước 2: fetch lại inventory mới nhất sau khi sort
                APIClient.Instance.GetPlayerInventory(
                    playerId,
                    items =>
                    {
                        string json = JsonUtility.ToJson(new InventoryJsonWrapper { items = items });
                        var clientParams = new ClientRpcParams
                        {
                            Send = new ClientRpcSendParams { TargetClientIds = new[] { capturedClientId } }
                        };
                        Debug.Log($"[NetworkInventory] Gửi inventory đã sort ({items.Length} items) về clientId={capturedClientId}");
                        SendInventoryDataClientRpc(json, clientParams);
                    },
                    err => Debug.LogError($"[NetworkInventory] Fetch sau sort thất bại cho clientId={capturedClientId}: {err}")
                );
            },
            err => Debug.LogError($"[NetworkInventory] Sort thất bại cho clientId={capturedClientId}: {err}")
        );
    }

    /// <summary>
    /// Host gửi JSON inventory về đúng client đã yêu cầu.
    /// InventoryNetworkBridge phía client nhận và cập nhật cache + UI.
    /// </summary>
    [ClientRpc]
    public void SendInventoryDataClientRpc(string inventoryJson, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[NetworkInventory] 📦 Client nhận inventory data từ host ({inventoryJson?.Length ?? 0} chars)");
        var bridge = FindObjectOfType<InventoryNetworkBridge>(true);
        if (bridge != null)
            bridge.OnReceivedInventoryDataFromHost(inventoryJson);
        else
            Debug.LogWarning("[NetworkInventory] SendInventoryDataClientRpc: InventoryNetworkBridge không tìm thấy!");
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

        TryAddItemInternal(itemID, quantity, out _);
    }

    public bool TryAddItemOnServer(int itemID, int quantity)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[NetworkInventory] TryAddItemOnServer chỉ được gọi trên server.");
            return false;
        }

        return TryAddItemInternal(itemID, quantity, out _);
    }

    private bool TryAddItemInternal(int itemID, int quantity, out int addedQuantity)
    {
        addedQuantity = 0;

        if (quantity <= 0)
        {
            Debug.LogWarning($"[NetworkInventory] Bỏ qua AddItem vì quantity={quantity} không hợp lệ.");
            return false;
        }

        var template = GetItemTemplate(itemID);
        if (template == null)
        {
            Debug.LogWarning($"[NetworkInventory] ItemID {itemID} không tồn tại!");
            return false;
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

        if (remainingQuantity < quantity)
        {
            addedQuantity = quantity - remainingQuantity;
            networkInventoryData.Value = currentData;
            OnItemAddedClientRpc(itemID, addedQuantity);
            Debug.Log($"[NetworkInventory] Added {addedQuantity}x {template.name} to inventory");

            // ✅ Persist to DB sau khi update NetworkVariable
            SyncInventoryToDB(itemID, template.code, template.icon_id, addedQuantity);
            return true;
        }

        // Nếu còn dư và không thể thêm được nữa
        if (remainingQuantity > 0)
        {
            Debug.LogWarning($"[NetworkInventory] Inventory đầy! Không thể thêm {remainingQuantity}x {template.name}");
        }

        return false;
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
    /// ServerRpc: Sử dụng item (consumable) — NGO-only path, giảm NGO cache inventory.
    /// Dùng khi KHÔNG có REST API (testing/offline). Production dùng ApplyConsumableStatServerRpc.
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

    /// <summary>
    /// ServerRpc: Áp dụng heal tick mỗi giây từ HpRestoreOverTime / MpRestoreOverTime buff.
    /// Gọi từ InventoryNetworkBridge khi ActiveBuffManager.OnHealTick fire.
    /// </summary>
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

        Debug.Log($"[NetworkInventory] 💉 Heal tick: +{hpHeal} HP / +{mpHeal} MP");
    }

    /// <summary>
    /// ServerRpc: Đặt HP/MP về giá trị chính xác từ REST API (server-authoritative).
    /// Gọi từ ItemUseHandler sau khi sử dụng item hồi phục HP/MP tức thì.
    /// </summary>
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

        Debug.Log($"[NetworkInventory] ✅ Sync HP={syncHp} MP={syncMp} từ REST API");
    }

    /// <summary>
    /// ServerRpc: CHỈ áp dụng stat effect (HP/MP) của consumable — KHÔNG giảm inventory.
    /// Gọi sau khi REST API đã persist việc tiêu thụ item lên DB.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyConsumableStatServerRpc(int templateId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        var template = ItemTemplateManager.Instance != null
            ? ItemTemplateManager.Instance.GetItemTemplate(templateId)
            : null;
        if (template == null)
        {
            Debug.LogWarning($"[NetworkInventory] ApplyConsumableStatServerRpc: template {templateId} not found");
            return;
        }

        int itemType = template.type;
        if (itemType < 21 || itemType > 29)
        {
            Debug.LogWarning($"[NetworkInventory] ApplyConsumableStatServerRpc: item type {itemType} is not consumable (21-29)");
            return;
        }

        ApplyItemEffect(templateId, itemType, rpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// Áp dụng HP/MP heal lên player. Chạy trên server.
    /// Với consumable type 22 (HP) / 23 (MP) – dùng giá trị từ ItemData ScriptableObject.
    /// Với type 24 (timed buff) – chỉ notify client; timed buff quản lý bởi ActiveBuffManager.
    /// </summary>
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
            Debug.Log($"[NetworkInventory] 💊 +{healValue} HP (type=22)");
        }
        // type 23 = MP Potion
        else if (itemType == 23 && dataSync != null)
        {
            dataSync.networkMp.Value = Mathf.Min(dataSync.networkMaxMp.Value, dataSync.networkMp.Value + healValue);
            Debug.Log($"[NetworkInventory] 🔵 +{healValue} MP (type=23)");
        }
        // type 24 = Timed buff — buff đã được server persist trong active_buffs;
        //   chỉ cần notify client reload buff HUD.
        else if (itemType == 24)
        {
            Debug.Log($"[NetworkInventory] ✨ Timed buff item (type=24, id={itemID}) — client sẽ refresh buff HUD.");
            // Client tự handle qua UseItemResponse.active_buffs từ REST API
        }
        // Fallback (generic consumable): ưu tiên sync vào dataSync để HP bar/UI luôn cập nhật.
        else if (dataSync != null)
        {
            dataSync.networkHp.Value = Mathf.Min(dataSync.networkMaxHp.Value,
                dataSync.networkHp.Value + healValue);
            Debug.Log($"[NetworkInventory] 💊 +{healValue} HP fallback qua dataSync (type={itemType})");
        }
        else if (playerHealth != null)
        {
            playerHealth.HealServerRpc(healValue);
            Debug.Log($"[NetworkInventory] 💊 +{healValue} HP fallback (type={itemType})");
        }

        ClientRpcParams clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } }
        };
        OnItemUsedClientRpc(itemID, clientParams);
    }

    /// <summary>
    /// ServerRpc: Cập nhật % bonus buff vào NetworkPlayerDataSync.
    /// Gọi sau khi client nhận active_buffs từ REST API và muốn sync lên NGO.
    /// geneExpBonusPct, expBonusPct, phucBonusPct, attackBonusPct, defenseBonusPct = tổng % (sum của tất cả buff đang active cùng loại).
    /// </summary>
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
        Debug.Log($"[NetworkInventory] 🎯 Sync buff bonuses: GeneEXP+{geneExpBonusPct}% EXP+{expBonusPct}% Phuc+{phucBonusPct}% ATK+{attackBonusPct}% DEF+{defenseBonusPct}%");
    }

    /// <summary>
    /// ClientRpc: Notify về item được thêm
    /// </summary>
    [ClientRpc]
    private void OnItemAddedClientRpc(int itemID, int quantity)
    {
        var template = GetItemTemplate(itemID);
        string itemName = template?.name ?? $"item_id={itemID}";
        Debug.Log($"[NetworkInventory] Client: Nhận được {quantity}x {itemName}");

        // Force refresh UI — đảm bảo inventory hiển thị mới nhất dù OnValueChanged chưa kịp fire
        DeserializeInventory(networkInventoryData.Value);
        OnInventoryChanged?.Invoke();
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
    private void OnItemUsedClientRpc(int itemID, ClientRpcParams clientRpcParams = default)
    {
        // Có thể play sound/effect ở đây
        Debug.Log($"[NetworkInventory] Item {itemID} được sử dụng");
    }

    /// <summary>
    /// Lấy ItemTemplateDto từ ItemID (dùng ItemTemplateManager mới)
    /// </summary>
    private ItemTemplateDto GetItemTemplate(int itemID)
    {
        if (ItemTemplateManager.Instance == null)
        {
            // Dedicated server: tự tạo singleton nếu chưa có
            if (IsServer)
            {
                ItemTemplateManager.EnsureInstance();
                Debug.LogWarning($"[NetworkInventory] ItemTemplateManager vừa được tạo tự động, đang load...");
            }
            else
            {
                Debug.LogWarning($"[NetworkInventory] ItemTemplateManager chưa sẵn sàng!");
            }
            return null;
        }

        if (!ItemTemplateManager.Instance.IsLoaded())
        {
            Debug.LogWarning($"[NetworkInventory] ItemTemplateManager chưa load xong templates!");
            return null;
        }

        var template = ItemTemplateManager.Instance.GetItemTemplate(itemID);
        if (template == null)
        {
            Debug.LogWarning($"[NetworkInventory] ItemID {itemID} not found in ItemTemplateManager!");
        }
        return template;
    }

    // Public API
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
    /// ServerRpc: Thêm item vào inventory VÀ sync với DB (1 item riêng lẻ)
    /// ⚠️ KHÔNG dùng khi thêm nhiều items cùng lúc → sẽ bị race condition!
    /// Dùng AddItemWithoutDBSyncServerRpc + SyncBatchToDB thay thế.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddItemWithDBSyncServerRpc(int itemTemplateId, string itemCode, string iconId, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        Debug.Log($"[NetworkInventory] AddItemWithDBSyncServerRpc: itemCode={itemCode}, quantity={quantity}");

        AddItemToNetworkVariable(itemTemplateId, itemCode, iconId, quantity);

        // ✅ Sync với DB ngay lập tức sau khi thêm item
        Debug.Log($"[NetworkInventory] Đang sync item vào DB...");
        SyncInventoryToDB(itemTemplateId, itemCode, iconId, quantity);
        
        Debug.Log($"[NetworkInventory] ✅ AddItemWithDBSyncServerRpc hoàn thành!");
    }

    /// <summary>
    /// ServerRpc: Thêm item vào NetworkVariable KHÔNG sync DB.
    /// Dùng khi cần thêm nhiều items rồi batch sync 1 lần sau.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddItemWithoutDBSyncServerRpc(int itemTemplateId, string itemCode, string iconId, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        Debug.Log($"[NetworkInventory] AddItemWithoutDBSyncServerRpc: itemCode={itemCode}, quantity={quantity}");
        AddItemToNetworkVariable(itemTemplateId, itemCode, iconId, quantity);
        Debug.Log($"[NetworkInventory] ✅ AddItemWithoutDBSyncServerRpc hoàn thành!");
    }

    /// <summary>
    /// Helper: Thêm item vào NetworkVariable (không sync DB)
    /// </summary>
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
            Debug.LogWarning("[NetworkInventory] Inventory đầy! Không thể thêm item.");
            return;
        }

        int itemID = itemTemplateId;

        currentData.slotData[emptySlotIndex] = new InventorySlotData 
        { 
            itemID = itemID, 
            quantity = quantity 
        };
        
        networkInventoryData.Value = currentData;

        Debug.Log($"[NetworkInventory] Đã thêm {quantity}x {itemCode} vào slot {emptySlotIndex}");

        OnItemAddedClientRpc(itemID, quantity);
    }

    /// <summary>
    /// Sync inventory với DB qua HTTP API
    /// </summary>
    private void SyncInventoryToDB(int itemTemplateId, string itemCode, string iconId, int quantity)
    {
        int playerId = ResolveInventoryApiPlayerId(OwnerClientId);

        if (playerId == 0)
        {
            Debug.LogWarning($"[NetworkInventory] SyncInventoryToDB: playerId=0, OwnerClientId={OwnerClientId} — không thể sync DB!");
            return;
        }

        Debug.Log($"[NetworkInventory] SyncInventoryToDB: owner={OwnerClientId}, playerId={playerId}, itemTemplateId={itemTemplateId}, quantity={quantity}");

        // Chỉ gửi itemTemplateId + quantity — server tự tra item_template
        var item = new APIClient.AddInventoryItemRequest
        {
            itemTemplateId = itemTemplateId,
            quantity = quantity
        };

        var items = new APIClient.AddInventoryItemRequest[] { item };

        // Lấy JWT của đúng client (không dùng JWT của HOST khi sync cho CLIENT)
        string clientJwt = ResolveClientJwt(OwnerClientId);

        // Gọi API
        if (APIClient.Instance != null)
        {
            int capturedPlayerId = playerId;
            ulong capturedOwnerId = OwnerClientId;

            APIClient.Instance.AddItemsToInventory(
                playerId,
                items,
                (response) =>
                {
                    Debug.Log($"[NetworkInventory] ✅ Đã sync inventory với DB thành công! Response: {response}");

                    // ✅ Push inventory mới nhất từ DB về đúng client để update UI
                    APIClient.Instance.GetPlayerInventory(
                        capturedPlayerId,
                        (freshItems) =>
                        {
                            string json = JsonUtility.ToJson(new InventoryJsonWrapper { items = freshItems });
                            var clientParams = new ClientRpcParams
                            {
                                Send = new ClientRpcSendParams { TargetClientIds = new[] { capturedOwnerId } }
                            };
                            SendInventoryDataClientRpc(json, clientParams);
                            Debug.Log($"[NetworkInventory] 📡 Đã push {freshItems.Length} items về client {capturedOwnerId} sau khi lưu DB");
                        },
                        (fetchError) =>
                        {
                            Debug.LogError($"[NetworkInventory] ❌ Lỗi fetch inventory sau sync DB: {fetchError}");
                        }
                    );
                },
                (error) =>
                {
                    Debug.LogError($"[NetworkInventory] ❌ Lỗi khi sync inventory với DB: {error}");
                },
                jwtOverride: clientJwt
            );
        }
        else
        {
            if (string.IsNullOrEmpty(clientJwt))
            {
                Debug.LogWarning("[NetworkInventory] APIClient.Instance is null và không có JWT client! Không thể sync với DB.");
                return;
            }

            Debug.Log("[NetworkInventory] APIClient null (dedicated server) → sync inventory qua UnityWebRequest trực tiếp.");
            StartCoroutine(SyncInventoryToApiDirect(playerId, items, clientJwt, OwnerClientId));
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

        int playerId = ResolveInventoryApiPlayerId(OwnerClientId);
        
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
            // Dedicated server không có APIClient → gọi API trực tiếp
            Debug.Log("[NetworkInventory] APIClient null (dedicated server) → dùng UnityWebRequest trực tiếp.");
            StartCoroutine(LoadInventoryFromApiDirect(playerId));
        }
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
            Debug.LogWarning($"[NetworkInventory] ResolveInventoryApiPlayerId: fallback USER_ID={prefsUserId} cho clientId={clientId}. Kiểm tra luồng set PLAYER_ID/player_id.");
            return prefsUserId;
        }

        return 0;
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
            clientJwt = PlayerPrefs.GetString("JWT_TOKEN", "");

        return clientJwt;
    }

    private IEnumerator SyncInventoryToApiDirect(int playerId, APIClient.AddInventoryItemRequest[] items, string clientJwt, ulong targetClientId)
    {
        string apiBase = ZoneRoomRegistry.Instance?.Config?.apiBaseUrl ?? ServerAddressConfig.Instance.ApiUrl;
        string url = $"{apiBase.TrimEnd('/')}/player/{playerId}/inventory/add";

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
            Debug.LogError($"[NetworkInventory] ❌ Direct sync inventory failed: {errorMessage}");
            yield break;
        }

        Debug.Log($"[NetworkInventory] ✅ Direct sync inventory thành công: {req.downloadHandler.text}");
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
            Debug.LogError($"[NetworkInventory] ❌ Direct sort inventory failed: {errorMessage}");
            yield break;
        }

        Debug.Log($"[NetworkInventory] ✅ Direct sort inventory thành công: {req.downloadHandler.text}");
        yield return PushInventoryDataToClientDirect(playerId, targetClientId);
    }

    private IEnumerator PushInventoryDataToClientDirect(int playerId, ulong targetClientId)
    {
        yield return FetchInventoryFromApiDirect(
            playerId,
            freshItems =>
            {
                string json = JsonUtility.ToJson(new InventoryJsonWrapper { items = freshItems });
                var clientParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams { TargetClientIds = new[] { targetClientId } }
                };
                SendInventoryDataClientRpc(json, clientParams);
                Debug.Log($"[NetworkInventory] 📡 Direct push {freshItems.Length} items về client {targetClientId}");
            },
            error => Debug.LogError($"[NetworkInventory] ❌ Direct push inventory thất bại cho clientId={targetClientId}: {error}")
        );
    }

    private IEnumerator FetchInventoryFromApiDirect(int playerId, System.Action<InventoryItem[]> onSuccess, System.Action<string> onError = null)
    {
        string apiBase = ZoneRoomRegistry.Instance?.Config?.apiBaseUrl ?? ServerAddressConfig.Instance.ApiUrl;
        string apiKey  = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey() ?? "dev-zone-key";
        string url = $"{apiBase.TrimEnd('/')}/player/{playerId}/data";

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
            onSuccess?.Invoke(response?.inventory ?? System.Array.Empty<InventoryItem>());
        }
        catch (System.Exception ex)
        {
            onError?.Invoke($"Parse player data failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Fallback cho dedicated server: gọi API trực tiếp khi APIClient.Instance null.
    /// </summary>
    private IEnumerator LoadInventoryFromApiDirect(int playerId)
    {
        yield return FetchInventoryFromApiDirect(
            playerId,
            items =>
            {
                if (items != null && items.Length > 0)
                {
                    Debug.Log($"[NetworkInventory] ✅ Load thành công {items.Length} items từ API (direct)!");
                    PopulateInventoryFromDB(items);
                }
                else
                {
                    Debug.Log("[NetworkInventory] Inventory trong DB trống (player mới) — direct API.");
                }
            },
            error => Debug.LogWarning($"[NetworkInventory] Direct API load failed: {error}")
        );
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
        // Đảm bảo slotData không null khi serialize (tránh NullRef trên ItemPickup prefab)
        if (serializer.IsWriter && slotData == null)
            slotData = System.Array.Empty<InventorySlotData>();
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
