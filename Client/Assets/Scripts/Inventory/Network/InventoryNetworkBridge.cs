using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// InventoryNetworkBridge - Bridge giữa NetworkInventory (Netcode) và InventoryUI (DTO)
/// - Subscribe NetworkInventory.OnInventoryChanged
/// - Convert từ NetworkInventory data → InventorySlotDto[]
/// - Gọi InventoryUI.SetInventoryData() để hiển thị UI
/// 
/// Gắn script này vào scene (có thể gắn vào cùng GameObject với InventoryUI hoặc tách riêng)
/// </summary>
public class InventoryNetworkBridge : MonoBehaviour
{
    [Header("References")]
    [Tooltip("NetworkInventory của player (tự động tìm local player nếu để trống)")]
    [SerializeField] private NetworkInventory networkInventory;

    [Tooltip("InventoryUI để hiển thị (tự động tìm trong scene nếu để trống)")]
    [SerializeField] private InventoryUI inventoryUI;

    [Tooltip("EquipmentPanelUI để hiển thị trang bị (tự động tìm trong scene nếu để trống)")]
    [SerializeField] private EquipmentPanelUI equipmentPanelUI;

    [Header("Settings")]
    [Tooltip("Tự động tìm NetworkInventory của local player khi Start")]
    [SerializeField] private bool autoFindPlayerInventory = true;

    [Header("Debug")]
    [Tooltip("Hiển thị debug logs chi tiết")]
    [SerializeField] private bool verboseDebug = true;

    private bool hasSubscribedToNetworkEvents = false;

    // ── Inventory cache ──────────────────────────────────────────────
    /// <summary>Raw items nhận lần cuối từ DB (null = chưa từng fetch).</summary>
    private InventoryItem[] _cachedInventoryItems;
    /// <summary>true = cache cũ hoặc chưa có, cần fetch lại khi mở túi.</summary>
    private bool _isCacheDirty = true;

    /// <summary>
    /// Refresh inventory từ DB và update UI (gọi khi mở inventory panel).
    /// - Có cache mới → hiển thị ngay từ cache.
    /// - Client thuần → yêu cầu host qua RPC → host fetch DB → gửi về → cache.
    /// - Host / offline → fetch API trực tiếp.
    /// </summary>
    public void RefreshInventoryFromDB()
    {
        // Cache hit: dữ liệu còn mới, không cần gọi mạng
        if (_cachedInventoryItems != null && !_isCacheDirty)
        {
            Debug.Log("[InventoryNetworkBridge] ✅ Cache còn mới, hiển thị từ cache...");
            UpdateUIFromDBInventory(_cachedInventoryItems);
            return;
        }

        Debug.Log("[InventoryNetworkBridge] ========== RefreshInventoryFromDB() GỌI! ==========");

        // Đường host-RPC: client thuần gửi yêu cầu lên host, host lấy DB rồi trả về
        if (networkInventory != null && networkInventory.IsSpawned &&
            NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            Debug.Log("[InventoryNetworkBridge] 📡 Client: yêu cầu inventory từ host qua RPC...");
            networkInventory.RequestInventoryDataServerRpc();
            return;
        }

        // Đường API trực tiếp: host-self hoặc chưa kết nối mạng
        FetchInventoryDirectFromAPI();
    }

    /// <summary>
    /// Đánh dấu cache cũ – gọi ngay sau khi mua item / trang bị / bỏ trang bị.
    /// Lần mở túi tiếp theo sẽ fetch lại từ host/DB.
    /// </summary>
    public void InvalidateInventoryCache()
    {
        _isCacheDirty = true;
        Debug.Log("[InventoryNetworkBridge] 🗑️ Inventory cache invalidated");
    }

    /// <summary>
    /// Callback từ NetworkInventory.SendInventoryDataClientRpc – host gửi JSON về client.
    /// Parse → cache → update UI.
    /// </summary>
    public void OnReceivedInventoryDataFromHost(string inventoryJson)
    {
        Debug.Log($"[InventoryNetworkBridge] 📦 Nhận inventory từ host ({inventoryJson?.Length ?? 0} chars)");

        if (string.IsNullOrEmpty(inventoryJson))
        {
            Debug.LogWarning("[InventoryNetworkBridge] Nhận JSON rỗng từ host!");
            return;
        }

        try
        {
            var wrapper = JsonUtility.FromJson<NetworkInventory.InventoryJsonWrapper>(inventoryJson);
            var items = wrapper?.items ?? new InventoryItem[0];
            Debug.Log($"[InventoryNetworkBridge] Parse thành công {items.Length} items từ host");
            UpdateUIFromDBInventory(items);  // also saves to cache
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[InventoryNetworkBridge] ❌ Lỗi parse inventory JSON từ host: {ex.Message}");
        }
    }

    private void FetchInventoryDirectFromAPI()
    {
        // ✅ FIX: Lấy playerId từ GameManager (in-memory) thay vì PlayerPrefs
        // PlayerPrefs bị shared giữa ParrelSync host/clone trên Windows,
        // dẫn đến host đọc được USER_ID của clone sau khi clone login
        int playerId = 0;
        
        // Ưu tiên 1: GameManager (in-memory, mỗi instance có riêng)
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerId = GameManager.Instance.GetPlayerData().user_id;
            Debug.Log($"[InventoryNetworkBridge] Lấy playerId từ GameManager (in-memory): {playerId}");
        }
        
        // Ưu tiên 2: ServerPlayerDataManager (host-side, dùng LocalClientId)
        if (playerId == 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var serverDataMgr = ServerPlayerDataManager.Instance;
            if (serverDataMgr != null)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                var playerData = serverDataMgr.GetPlayerDataForClient(localClientId);
                if (playerData != null)
                {
                    playerId = playerData.user_id;
                    Debug.Log($"[InventoryNetworkBridge] Lấy playerId từ ServerPlayerDataManager (clientId={localClientId}): {playerId}");
                }
            }
        }
        
        // Fallback cuối cùng: PlayerPrefs (có thể bị shared giữa ParrelSync host/clone)
        if (playerId == 0)
        {
            playerId = PlayerPrefs.GetInt("USER_ID", 0);
            Debug.LogWarning($"[InventoryNetworkBridge] Fallback PlayerPrefs USER_ID: {playerId} (có thể không chính xác khi dùng ParrelSync!)");
        }
        
        if (playerId == 0)
        {
            Debug.LogWarning("[InventoryNetworkBridge] playerId = 0, không thể fetch inventory từ DB!");
            ManualSyncInventoryUI();
            return;
        }

        Debug.Log($"[InventoryNetworkBridge] Đang fetch inventory từ DB cho player {playerId}...");

        if (APIClient.Instance != null)
        {
            APIClient.Instance.GetPlayerInventory(
                playerId,
                (inventoryItems) =>
                {
                    Debug.Log($"[InventoryNetworkBridge] ✅ Fetch thành công {inventoryItems.Length} items từ DB!");
                    UpdateUIFromDBInventory(inventoryItems);
                    
                    if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && networkInventory != null)
                    {
                        Debug.Log("[InventoryNetworkBridge] Server/Host: Đang sync DB data vào NetworkInventory...");
                        SyncDBInventoryToNetwork(inventoryItems);
                    }
                },
                (error) =>
                {
                    Debug.LogError($"[InventoryNetworkBridge] ❌ Lỗi khi fetch inventory từ DB: {error}");
                    ManualSyncInventoryUI();
                }
            );
        }
        else
        {
            Debug.LogWarning("[InventoryNetworkBridge] APIClient.Instance is null!");
            ManualSyncInventoryUI();
        }
    }

    /// <summary>
    /// Update UI trực tiếp từ DB inventory data (không qua NetworkInventory)
    /// </summary>
    private void UpdateUIFromDBInventory(InventoryItem[] dbItems)
    {
        if (inventoryUI == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] inventoryUI is null!");
            return;
        }

        Debug.Log($"[InventoryNetworkBridge] UpdateUIFromDBInventory: Converting {dbItems.Length} DB items to DTO...");

        List<InventorySlotDto> slotDtos = new List<InventorySlotDto>();

        // Tạo dictionary để map slotIndex → item
        // Items không có slotIndex (= 0 mặc định) được gán slot tự động để tránh ghi đè
        Dictionary<int, InventoryItem> itemsBySlot = new Dictionary<int, InventoryItem>();
        int autoSlot = 0;
        foreach (var item in dbItems)
        {
            if (item.quantity <= 0) continue;
            int slot = item.slotIndex;
            // Nếu slot đã bị chiếm (nhiều item cùng slotIndex=0), tìm slot tiếp theo
            if (itemsBySlot.ContainsKey(slot))
            {
                while (itemsBySlot.ContainsKey(autoSlot)) autoSlot++;
                slot = autoSlot;
            }
            itemsBySlot[slot] = item;
            if (slot >= autoSlot) autoSlot = slot + 1;
        }

        // Tạo DTO cho tất cả slots (giả sử max 20 slots)
        int maxSlots = 20; // TODO: Get from NetworkInventory or config
        for (int i = 0; i < maxSlots; i++)
        {
            if (itemsBySlot.TryGetValue(i, out InventoryItem item) && item.quantity > 0)
            {
                // Có item ở slot này
                // Resolve iconId: server may not store it for old items → fall back to ItemTemplateManager
                string resolvedIconId = item.iconId;
                if (string.IsNullOrEmpty(resolvedIconId) && item.itemTemplateId > 0)
                {
                    var tpl = ItemTemplateManager.Instance?.GetItemTemplate(item.itemTemplateId);
                    if (tpl != null && tpl.idIcon > 0)
                        resolvedIconId = tpl.idIcon.ToString();
                }

                InventorySlotDto dto = new InventorySlotDto
                {
                    slotIndex = i,
                    itemTemplateId = item.itemTemplateId,
                    itemCode = item.itemCode,
                    iconId = resolvedIconId,
                    quantity = item.quantity,
                    isEquipped = item.isEquipped
                };
                slotDtos.Add(dto);
                
                Debug.Log($"[InventoryNetworkBridge] Slot {i}: {item.itemCode} x{item.quantity}");
            }
            else
            {
                // Slot trống
                InventorySlotDto emptyDto = new InventorySlotDto
                {
                    slotIndex = i,
                    itemTemplateId = 0,
                    itemCode = null,
                    iconId = null,
                    quantity = 0,
                    isEquipped = false
                };
                slotDtos.Add(emptyDto);
            }
        }

        Debug.Log($"[InventoryNetworkBridge] Đang gửi {slotDtos.Count} slots cho InventoryUI...");
        inventoryUI.SetInventoryData(slotDtos.ToArray());
        Debug.Log($"[InventoryNetworkBridge] ✅ UI đã được update từ DB data!");

        // Lưu vào cache để lần mở tiếp theo khỏi fetch lại
        _cachedInventoryItems = dbItems;
        _isCacheDirty = false;

        // Thông báo cho ItemUseHandler để cập nhật stat bar, quick-slots túi, v.v.
        int bagSlots = 20;
        int gold     = 0;
        int silver   = 0;
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var pd = GameManager.Instance.GetPlayerData();
            bagSlots = pd.bag_slots > 0 ? pd.bag_slots : 20;
            gold     = pd.gold;
            silver   = pd.silver;
        }
        ItemUseHandler.Instance?.OnInventoryRefreshed(slotDtos.ToArray(), bagSlots, gold, silver);
    }

    /// <summary>
    /// Sync DB inventory vào NetworkInventory (chỉ server/host gọi)
    /// </summary>
    private void SyncDBInventoryToNetwork(InventoryItem[] dbItems)
    {
        // TODO: Implement if needed - Server sẽ populate NetworkInventory từ DB
        // Hiện tại NetworkInventory đã tự load từ DB khi spawn
        Debug.Log("[InventoryNetworkBridge] SyncDBInventoryToNetwork: TODO - Server should handle this in NetworkInventory.LoadInventoryFromDB()");
    }

    /// <summary>
    /// Public method để manual refresh UI từ NetworkInventory
    /// Gọi từ Button UI hoặc debug command
    /// </summary>
    public void ManualSyncInventoryUI()
    {
        Debug.Log("===================== [InventoryNetworkBridge] MANUAL SYNC ĐƯỢC GỌI! =====================");
        
        if (networkInventory == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] NetworkInventory is NULL! Đang tìm kiếm...");
            FindPlayerInventory();
        }

        if (networkInventory != null)
        {
            Debug.Log("[InventoryNetworkBridge] ✓ Có NetworkInventory, đang refresh UI...");
            RefreshInventoryUI();
        }
        else
        {
            Debug.LogError("[InventoryNetworkBridge] ❌ Vẫn không tìm thấy NetworkInventory sau khi tìm!");
            
            // Debug: List tất cả NetworkInventory trong scene
            var allInventories = FindObjectsOfType<NetworkInventory>();
            Debug.Log($"[InventoryNetworkBridge] Tổng số NetworkInventory trong scene: {allInventories.Length}");
            foreach (var inv in allInventories)
            {
                Debug.Log($"[InventoryNetworkBridge]   - {inv.gameObject.name}: IsOwner={inv.IsOwner}, IsSpawned={inv.IsSpawned}");
            }
        }
    }

    private void Start()
    {
        Debug.Log("==================== [InventoryNetworkBridge] START() ĐƯỢC GỌI! ====================");
        
        // Tìm InventoryUI nếu chưa gán
        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogWarning("[InventoryNetworkBridge] Không tìm thấy InventoryUI trong scene!");
            }
            else
            {
                Debug.Log($"[InventoryNetworkBridge] ✓ Tìm thấy InventoryUI: {inventoryUI.name}");
            }
        }

        // Kiểm tra NetworkManager
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[InventoryNetworkBridge] ❌ NetworkManager.Singleton IS NULL! Không thể subscribe network events!");
        }
        else
        {
            Debug.Log($"[InventoryNetworkBridge] ✓ NetworkManager.Singleton exists");
        }

        // Subscribe vào NetworkManager events để tự động tìm NetworkInventory khi client connect
        SubscribeToNetworkEvents();

        // Tìm NetworkInventory nếu chưa gán (có thể chưa có nếu player chưa spawn)
        if (networkInventory == null && autoFindPlayerInventory)
        {
            Debug.Log("[InventoryNetworkBridge] Đang tìm NetworkInventory lần đầu tiên...");
            FindPlayerInventory();
        }

        // Subscribe event từ NetworkInventory nếu đã tìm thấy
        if (networkInventory != null)
        {
            Debug.Log("[InventoryNetworkBridge] ✓ NetworkInventory đã được tìm thấy trong Start(), đang subscribe events...");
            SubscribeToInventoryEvents();
        }
        else
        {
            Debug.LogWarning("[InventoryNetworkBridge] ⚠️ Chưa tìm thấy NetworkInventory trong Start(), sẽ tìm lại sau khi client connect.");
        }
    }

    private void SubscribeToNetworkEvents()
    {
        Debug.Log("[InventoryNetworkBridge] SubscribeToNetworkEvents() được gọi...");
        
        if (hasSubscribedToNetworkEvents)
        {
            Debug.Log("[InventoryNetworkBridge] Đã subscribe rồi, skip.");
            return;
        }

        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            hasSubscribedToNetworkEvents = true;
            Debug.Log("[InventoryNetworkBridge] ✓ Đã subscribe OnClientConnectedCallback");
        }
        else
        {
            Debug.LogError("[InventoryNetworkBridge] ❌ NetworkManager.Singleton is NULL, không thể subscribe events!");
        }
    }



    private void OnClientConnected(ulong clientId)
    {
        // Chỉ tìm lại nếu là local client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            Debug.Log($"[InventoryNetworkBridge] Client connected (ID: {clientId}), trying to find NetworkInventory...");
            
            // Đợi một chút để player object được spawn
            StartCoroutine(FindPlayerInventoryDelayed());
        }
    }

    private System.Collections.IEnumerator FindPlayerInventoryDelayed()
    {
        // Đợi 1 giây để player character có thời gian spawn
        Debug.Log("[InventoryNetworkBridge] Đang đợi player character spawn (1s)...");
        yield return new WaitForSeconds(1f);

        if (networkInventory == null && autoFindPlayerInventory)
        {
            // Thử tìm tối đa 30 lần, mỗi lần cách nhau 0.2 giây (tổng 6 giây)
            int maxAttempts = 30;
            int currentAttempt = 0;
            
            while (currentAttempt < maxAttempts && networkInventory == null)
            {
                currentAttempt++;
                Debug.Log($"[InventoryNetworkBridge] Lần thử {currentAttempt}/{maxAttempts}...");
                
                FindPlayerInventory();
                
                if (networkInventory != null)
                {
                    // Tìm thấy rồi!
                    Debug.Log($"[InventoryNetworkBridge] ✓✓✓ Tìm thấy NetworkInventory ở lần thử {currentAttempt}!");
                    Debug.Log($"[InventoryNetworkBridge] → Đang subscribe to inventory events...");
                    SubscribeToInventoryEvents();
                    Debug.Log($"[InventoryNetworkBridge] ✓ Subscribe thành công!");
                    
                    // ✅ Tự động load equipment từ DB khi vào game
                    Debug.Log("[InventoryNetworkBridge] 🔄 Auto-load equipment từ DB khi vào game...");
                    RefreshEquipmentFromDB();
                    
                    yield break;
                }
                
                // Chưa tìm thấy, đợi thêm 0.2 giây
                if (currentAttempt < maxAttempts)
                {
                    yield return new WaitForSeconds(0.2f);
                }
            }
            
            // Sau tất cả các lần thử vẫn không tìm thấy
            if (networkInventory == null)
            {
                Debug.LogError($"[InventoryNetworkBridge] ❌ KHÔNG TÌM THẤY NetworkInventory sau {maxAttempts} lần thử (7 giây)!\n" +
                    "NGUYÊN NHÂN CÓ THỂ:\n" +
                    "1. Player prefab CHƯA CÓ NetworkInventory component → Thêm trong Unity Editor\n" +
                    "2. Player không spawn (xem log NetworkPlayerSpawner)\n" +
                    "3. Player thiếu NetworkPlayerHealth hoặc PlayerMovement component");
            }
        }
    }

    private void SubscribeToInventoryEvents()
    {
        if (networkInventory != null)
        {
            Debug.Log($"[InventoryNetworkBridge] ===== SUBSCRIBING TO INVENTORY EVENTS =====");
            Debug.Log($"[InventoryNetworkBridge] NetworkInventory: {networkInventory.gameObject.name}");
            Debug.Log($"[InventoryNetworkBridge] IsServer={networkInventory.IsServer}, IsClient={networkInventory.IsClient}, IsOwner={networkInventory.IsOwner}");
            
            networkInventory.OnInventoryChanged.AddListener(OnInventoryChanged);
            
            // Refresh ngay lần đầu
            Debug.Log("[InventoryNetworkBridge] Calling initial RefreshInventoryUI()...");
            RefreshInventoryUI();
            
            Debug.Log("[InventoryNetworkBridge] ✅ Subscribed to NetworkInventory.OnInventoryChanged");
        }
        else
        {
            Debug.LogError("[InventoryNetworkBridge] ❌ Cannot subscribe - networkInventory is NULL!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe từ NetworkInventory
        if (networkInventory != null)
        {
            networkInventory.OnInventoryChanged.RemoveListener(OnInventoryChanged);
        }

        // Unsubscribe từ NetworkManager
        if (hasSubscribedToNetworkEvents && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            hasSubscribedToNetworkEvents = false;
        }
    }

    /// <summary>
    /// Tìm NetworkInventory của local player
    /// </summary>
    private void FindPlayerInventory()
    {
        Debug.Log("[InventoryNetworkBridge] ========== FindPlayerInventory() BẮT ĐẦU ==========" );
        
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] NetworkManager.Singleton is null!");
            return;
        }

        // Kiểm tra SpawnManager có sẵn sàng không
        if (NetworkManager.Singleton.SpawnManager == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] NetworkManager.SpawnManager is null! Network may not be initialized yet.");
            return;
        }

        // Kiểm tra SpawnedObjectsList có sẵn sàng không
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjectsList == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] SpawnedObjectsList is null! No objects spawned yet.");
            return;
        }
        
        Debug.Log($"[InventoryNetworkBridge] SpawnedObjectsList count: {NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count}");

        int objectsChecked = 0;
        int ownedObjectsFound = 0;
        int playerCharactersFound = 0;

        // Debug.Log($"[InventoryNetworkBridge] ========== BẮT ĐẦU TÌM KIẾM ==========");
        // Debug.Log($"[InventoryNetworkBridge] Tổng số NetworkObjects đã spawn: {NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count}");

        // Tìm trong các NetworkObject đã spawn
        foreach (var networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (networkObject == null) continue;
            objectsChecked++;

            // Log tất cả objects để debug
            // Debug.Log($"[InventoryNetworkBridge] Object #{objectsChecked}: Name='{networkObject.name}', IsOwner={networkObject.IsOwner}, IsLocalPlayer={networkObject.IsLocalPlayer}, IsOwnedByServer={networkObject.IsOwnedByServer}");

            // Kiểm tra tất cả objects owned by local client
            if (networkObject.IsOwner)
            {
                ownedObjectsFound++;

                // Log các components của object này
                // var allComponents = networkObject.GetComponents<Component>();
                // string componentList = string.Join(", ", System.Array.ConvertAll(allComponents, c => c.GetType().Name));
                // Debug.Log($"[InventoryNetworkBridge]   → Object '{networkObject.name}' có {allComponents.Length} components: {componentList}");

                // Kiểm tra có phải player character không
                var playerHealth = networkObject.GetComponent<NetworkPlayerHealth>();
                var playerMovement = networkObject.GetComponent<PlayerMovement>();
                
                bool isPlayerCharacter = playerHealth != null || playerMovement != null;

                // Debug.Log($"[InventoryNetworkBridge]   → Has NetworkPlayerHealth: {playerHealth != null}, Has PlayerMovement: {playerMovement != null}");

                if (!isPlayerCharacter)
                {
                    // Bỏ qua các utility objects
                    // Debug.Log($"[InventoryNetworkBridge]   → Bỏ qua utility object: '{networkObject.name}'");
                    continue;
                }

                playerCharactersFound++;
                Debug.Log($"[InventoryNetworkBridge] ✓ Tìm thấy player character: '{networkObject.name}'");

                // Kiểm tra có NetworkInventory không
                NetworkInventory inv = networkObject.GetComponent<NetworkInventory>();
                // Debug.Log($"[InventoryNetworkBridge]   → Has NetworkInventory: {inv != null}");

                if (inv != null)
                {
                    networkInventory = inv;
                    Debug.Log($"[InventoryNetworkBridge] ✓✓✓ TÌM THẤY NetworkInventory của player: {networkObject.name}");
                    Debug.Log($"[InventoryNetworkBridge] → NetworkInventory GameObject: {networkObject.gameObject.name}");
                    Debug.Log($"[InventoryNetworkBridge] → OwnerClientId: {networkObject.OwnerClientId}");
                    Debug.Log($"[InventoryNetworkBridge] → IsSpawned: {networkObject.IsSpawned}");
                    Debug.Log($"[InventoryNetworkBridge] → Component found at: {inv.GetType().FullName}");
                    return;
                }
                else
                {
                    Debug.LogWarning($"[InventoryNetworkBridge] ⚠️ Player character '{networkObject.name}' KHÔNG có NetworkInventory component!");
                }
            }
        }

        // Debug.Log($"[InventoryNetworkBridge] ========== KẾT QUẢ TÌM KIẾM ==========");
        Debug.LogWarning($"[InventoryNetworkBridge] Không tìm thấy NetworkInventory. Owned objects: {ownedObjectsFound}, Player characters: {playerCharactersFound}");
        
        if (playerCharactersFound == 0)
        {
            // Debug chỉ khi có owned objects nhưng không phải player character
            if (ownedObjectsFound > 0)
            {
                Debug.Log($"[InventoryNetworkBridge] Có {ownedObjectsFound} owned object(s) nhưng không phải player character (utility objects). Đợi player spawn...");
            }
        }
    }

    /// <summary>
    /// Callback khi NetworkInventory thay đổi
    /// </summary>
    private void OnInventoryChanged()
    {
        Debug.Log("========== [InventoryNetworkBridge] OnInventoryChanged EVENT RECEIVED! ==========");
        Debug.Log($"[InventoryNetworkBridge] Client/Server: IsClient={NetworkManager.Singleton?.IsClient}, IsServer={NetworkManager.Singleton?.IsServer}");
        RefreshInventoryUI();
    }

    /// <summary>
    /// Convert từ NetworkInventory → InventorySlotDto[] và gửi cho InventoryUI
    /// </summary>
    private void RefreshInventoryUI()
    {
        if (networkInventory == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] RefreshInventoryUI: networkInventory is null!");
            return;
        }

        if (inventoryUI == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] RefreshInventoryUI: inventoryUI is null!");
            return;
        }

        Debug.Log("[InventoryNetworkBridge] RefreshInventoryUI: Bắt đầu convert từ NetworkInventory...");

        int maxSlots = networkInventory.GetMaxSlots();
        List<InventorySlotDto> slotDtos = new List<InventorySlotDto>();
        int itemsFound = 0;

        // Đọc từng slot từ NetworkInventory
        for (int i = 0; i < maxSlots; i++)
        {
            // Lấy slot từ localInventory (đã được deserialize từ NetworkVariable)
            InventorySlot slot = networkInventory.GetSlot(i);
            
            if (slot != null && slot.itemID > 0 && slot.quantity > 0)
            {
                itemsFound++;
                
                // Query ItemTemplateManager để lấy thông tin chi tiết
                var template = ItemTemplateManager.Instance?.GetItemTemplate(slot.itemID);
                
                string iconId = template?.icon_id ?? $"unknown_{slot.itemID}";
                string itemCode = template?.code ?? $"ITEM_{slot.itemID}";
                string itemName = template?.name ?? $"Unknown Item {slot.itemID}";
                
                if (template != null)
                {
                    Debug.Log($"[InventoryNetworkBridge] Slot {i} - ✓ Có template: itemID={slot.itemID}, name={itemName}, iconId={iconId}, qty={slot.quantity}");
                }
                else
                {
                    Debug.LogWarning($"[InventoryNetworkBridge] Slot {i} - ⚠️ KHÔNG tìm thấy template cho itemID={slot.itemID}, dùng fallback!");
                }
                
                // Convert → InventorySlotDto
                InventorySlotDto dto = new InventorySlotDto
                {
                    slotIndex = i,
                    itemTemplateId = slot.itemID,
                    itemCode = itemCode,
                    iconId = iconId,
                    quantity = slot.quantity,
                    isEquipped = false
                };

                slotDtos.Add(dto);
            }
            else
            {
                // Slot trống - vẫn tạo DTO với quantity = 0 để UI biết slot này trống
                InventorySlotDto emptyDto = new InventorySlotDto
                {
                    slotIndex = i,
                    itemTemplateId = 0,
                    itemCode = null,
                    iconId = null,
                    quantity = 0,
                    isEquipped = false
                };

                slotDtos.Add(emptyDto);
            }
        }

        Debug.Log($"[InventoryNetworkBridge] RefreshInventoryUI: Tìm thấy {itemsFound} items trong {maxSlots} slots. Đang gửi cho InventoryUI...");

        // Gửi data cho InventoryUI
        inventoryUI.SetInventoryData(slotDtos.ToArray());
        
        Debug.Log($"[InventoryNetworkBridge] RefreshInventoryUI: Đã gửi {slotDtos.Count} slots cho InventoryUI.");
    }

    /// <summary>
    /// Lấy iconId từ ItemData
    /// Ưu tiên: dùng sprite.name làm iconId (nếu sprite.name trùng với iconId trong DB)
    /// </summary>
    private string GetIconIdFromItemData(ItemData itemData)
    {
        if (itemData == null)
            return null;

        // Nếu có sprite, dùng tên sprite làm iconId
        if (itemData.icon != null && !string.IsNullOrEmpty(itemData.icon.name))
        {
            return itemData.icon.name;
        }

        // Fallback: dùng itemID làm iconId (nếu bạn đặt tên sprite = itemID)
        return itemData.itemID.ToString();
    }

    /// <summary>
    /// Public API để gán NetworkInventory từ bên ngoài (dùng khi player spawn runtime)
    /// </summary>
    public void SetNetworkInventory(NetworkInventory inv)
    {
        if (networkInventory != null)
        {
            networkInventory.OnInventoryChanged.RemoveListener(OnInventoryChanged);
        }

        networkInventory = inv;

        if (networkInventory != null)
        {
            networkInventory.OnInventoryChanged.AddListener(OnInventoryChanged);
            RefreshInventoryUI();
        }
    }

    /// <summary>
    /// Public API để gán InventoryUI từ bên ngoài
    /// </summary>
    public void SetInventoryUI(InventoryUI ui)
    {
        inventoryUI = ui;
        if (inventoryUI != null && networkInventory != null)
        {
            RefreshInventoryUI();
        }
    }

    /// <summary>Trả về túi đồ hiện tại (dùng cho UpgradePanel)</summary>
    public InventorySlotDto[] CurrentInventory => inventoryUI?.CurrentSlots;

    /// <summary>
    /// Lấy playerId hiện tại từ GameManager hoặc PlayerPrefs
    /// </summary>
    private int GetCurrentPlayerId()
    {
        int playerId = 0;
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerId = GameManager.Instance.GetPlayerData().user_id;
        }
        if (playerId == 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var serverDataMgr = ServerPlayerDataManager.Instance;
            if (serverDataMgr != null)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                var playerData = serverDataMgr.GetPlayerDataForClient(localClientId);
                if (playerData != null)
                    playerId = playerData.user_id;
            }
        }
        if (playerId == 0)
            playerId = PlayerPrefs.GetInt("USER_ID", 0);
        return playerId;
    }

    /// <summary>
    /// Gửi request sử dụng item lên server (gọi từ ItemDetailPanel khi nhấn nút Sử dụng).
    /// Ưu tiên dùng ItemUseHandler; phương thức này giữ lại như fallback.
    /// </summary>
    public void RequestUseItem(int slotIndex, string itemCode, int itemTemplateId = 0)
    {
        Debug.Log($"[InventoryNetworkBridge] RequestUseItem (fallback): slotIndex={slotIndex}, itemCode={itemCode}");

        // Nếu ItemUseHandler tồn tại, để nó xử lý
        if (ItemUseHandler.Instance != null)
        {
            // Tạo DTO từ dữ liệu có sẵn
            var slot = new InventorySlotDto
            {
                slotIndex      = slotIndex,
                itemCode       = itemCode,
                itemTemplateId = itemTemplateId,
                quantity       = 1
            };
            ItemUseHandler.Instance.RequestUseItem(slot);
            return;
        }

        // Legacy fallback: kiểm tra nếu là equipment thì equip, còn lại refresh
        int playerId = GetCurrentPlayerId();
        if (playerId == 0) return;

        ItemTemplateDto template = null;
        if (ItemTemplateManager.Instance != null)
        {
            if (itemTemplateId > 0)
                template = ItemTemplateManager.Instance.GetItemTemplate(itemTemplateId);
            if (template == null && !string.IsNullOrEmpty(itemCode))
                template = ItemTemplateManager.Instance.GetItemTemplateByCode(itemCode);
        }

        if (template != null && template.category == 1)
        {
            RequestEquipItem(slotIndex, itemCode);
            return;
        }

        Debug.Log("[InventoryNetworkBridge] Fallback: refresh inventory (ItemUseHandler không tìm thấy).");
        RefreshInventoryFromDB();
    }

    /// <summary>
    /// Lấy itemTemplateId từ inventory slot (dùng cache hiện tại)
    /// </summary>
    private int GetItemTemplateIdFromSlot(int slotIndex)
    {
        if (networkInventory != null)
        {
            var slot = networkInventory.GetSlot(slotIndex);
            if (slot != null) return slot.itemID;
        }
        return 0;
    }

    // ==================== EQUIPMENT ====================

    /// <summary>
    /// Gửi request trang bị item lên server
    /// Gọi từ ItemDetailPanel khi nhấn nút "Trang bị"
    /// Server sẽ: remove item khỏi inventory, thêm vào equipment slot,
    /// nếu slot đã có item cũ thì swap (item cũ quay về inventory)
    /// </summary>
    public void RequestEquipItem(int inventorySlotIndex, string itemCode)
    {
        Debug.Log($"[InventoryNetworkBridge] ⚔️ RequestEquipItem: slotIndex={inventorySlotIndex}, itemCode={itemCode}");

        int playerId = GetCurrentPlayerId();
        if (playerId == 0)
        {
            Debug.LogWarning("[InventoryNetworkBridge] RequestEquipItem: playerId = 0!");
            return;
        }

        if (APIClient.Instance == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] RequestEquipItem: APIClient.Instance is null!");
            return;
        }

        Debug.Log($"[InventoryNetworkBridge] ⚔️ Đang gửi equip request lên server: playerId={playerId}, slot={inventorySlotIndex}, item={itemCode}");

        APIClient.Instance.EquipItem(
            playerId,
            inventorySlotIndex,
            (response) =>
            {
                Debug.Log($"[InventoryNetworkBridge] ✅ Equip thành công! Response: {response}");
                // Server đã update cả inventory + equipment trong DB
                // Refresh cả inventory và equipment UI từ DB
                InvalidateInventoryCache();
                RefreshInventoryFromDB();
                RefreshEquipmentFromDB();
                // Refresh final_stats trong GameManager để StatsTabUI hiển thị đúng
                APIClient.Instance.LoadPlayerData(playerId,
                    data => GameManager.Instance?.SetPlayerData(data),
                    _ => { });
            },
            (error) =>
            {
                Debug.LogError($"[InventoryNetworkBridge] ❌ Equip thất bại: {error}");
            }
        );
    }

    /// <summary>
    /// Gửi request tháo trang bị lên server
    /// Gọi từ EquipmentPanelUI khi click tháo
    /// </summary>
    public void RequestUnequipItem(EquipmentSlotType slotType)
    {
        string slotName = slotType switch
        {
            EquipmentSlotType.Weapon => "weapon",
            EquipmentSlotType.Helmet => "helmet",
            EquipmentSlotType.Armor => "armor",
            EquipmentSlotType.Pants => "pants",
            EquipmentSlotType.Boots => "boots",
            EquipmentSlotType.Accessory => "accessory",
            _ => ""
        };

        Debug.Log($"[InventoryNetworkBridge] 🔧 RequestUnequipItem: slot={slotName}");

        int playerId = GetCurrentPlayerId();
        if (playerId == 0)
        {
            Debug.LogWarning("[InventoryNetworkBridge] RequestUnequipItem: playerId = 0!");
            return;
        }

        if (APIClient.Instance == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] RequestUnequipItem: APIClient.Instance is null!");
            return;
        }

        APIClient.Instance.UnequipItem(
            playerId,
            slotName,
            (response) =>
            {
                Debug.Log($"[InventoryNetworkBridge] ✅ Unequip thành công!");
                InvalidateInventoryCache();
                RefreshInventoryFromDB();
                RefreshEquipmentFromDB();
                // Refresh final_stats trong GameManager để StatsTabUI hiển thị đúng
                APIClient.Instance.LoadPlayerData(playerId,
                    data => GameManager.Instance?.SetPlayerData(data),
                    _ => { });
            },
            (error) =>
            {
                Debug.LogError($"[InventoryNetworkBridge] ❌ Unequip thất bại: {error}");
            }
        );
    }

    /// <summary>
    /// Refresh equipment UI từ DB
    /// Gọi khi mở EquipmentPanel hoặc sau khi equip/unequip
    /// </summary>
    public void RefreshEquipmentFromDB()
    {
        Debug.Log("[InventoryNetworkBridge] 🔄 RefreshEquipmentFromDB()");

        int playerId = GetCurrentPlayerId();
        if (playerId == 0)
        {
            Debug.LogWarning("[InventoryNetworkBridge] RefreshEquipmentFromDB: playerId = 0!");
            return;
        }

        if (APIClient.Instance == null)
        {
            Debug.LogWarning("[InventoryNetworkBridge] RefreshEquipmentFromDB: APIClient.Instance is null!");
            return;
        }

        // Tìm EquipmentPanelUI nếu chưa có
        if (equipmentPanelUI == null)
        {
            equipmentPanelUI = FindObjectOfType<EquipmentPanelUI>();
        }

        APIClient.Instance.GetPlayerEquipment(
            playerId,
            (equipment) =>
            {
                Debug.Log($"[InventoryNetworkBridge] ✅ Equipment loaded from DB");
                if (equipmentPanelUI != null)
                {
                    equipmentPanelUI.SetEquipmentData(equipment);
                }
            },
            (error) =>
            {
                Debug.LogError($"[InventoryNetworkBridge] ❌ Failed to load equipment: {error}");
            }
        );
    }
}
