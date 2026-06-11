using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.Networking;

// InventoryNetworkBridge - Bridge giữa NetworkInventory (Netcode) và InventoryUI (DTO)
// - Subscribe NetworkInventory.OnInventoryChanged
// - Convert từ NetworkInventory data → InventorySlotDto[]
// - Gọi InventoryUI.SetInventoryData() để hiển thị UI
// Gắn script này vào scene (có thể gắn vào cùng GameObject với InventoryUI hoặc tách riêng)
public class InventoryNetworkBridge : MonoBehaviour
{
    public static InventoryNetworkBridge Instance { get; private set; }

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
#pragma warning disable CS0414
    [SerializeField] private bool verboseDebug = true;
#pragma warning restore CS0414

    private bool hasSubscribedToNetworkEvents = false;

    // Inventory cache
    // Raw items nhận lần cuối từ DB (null = chưa từng fetch).
    private InventoryItem[] _cachedInventoryItems;
    // true = cache cũ hoặc chưa có, cần fetch lại khi mở túi.
    private bool _isCacheDirty = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            { /* Cảnh báo: Duplicate bridge detected, destroying scene-local copy */ }
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        RefreshUiReferences();
    }

    public static InventoryNetworkBridge GetExisting(bool includeInactive = true)
    {
        if (Instance != null)
            return Instance;

        return FindObjectOfType<InventoryNetworkBridge>(includeInactive);
    }

    private void RefreshUiReferences()
    {
        if (inventoryUI == null)
            inventoryUI = FindObjectOfType<InventoryUI>(true);

        if (equipmentPanelUI == null)
            equipmentPanelUI = FindObjectOfType<EquipmentPanelUI>(true);
    }

    // Sắp xếp inventory (gom item về phía trước) theo đường đúng:
    // - Client → gửi ServerRpc lên host → host sort DB → host fetch fresh → gửi ClientRpc về client.
    // - Host/offline → gọi API trực tiếp → fetch lại.
    public void RequestSortAndRefresh()
    {
        _isCacheDirty = true;

        // Client: đường host-mediated (host sort rồi push kết quả về)
        if (networkInventory != null && networkInventory.IsSpawned &&
            NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            { /* 📡 Client: yêu cầu host sort inventory */ }
            networkInventory.RequestSortInventoryServerRpc();
            return;
        }

        // Host / offline: delegate to NetworkInventory direct sort (server-side API)
        if (networkInventory != null && networkInventory.IsSpawned)
        {
            networkInventory.RequestSortInventoryServerRpc();
            return;
        }

        { /* Cảnh báo: RequestSortAndRefresh: no networkInventory available */ }
    }

    // Refresh inventory từ DB và update UI (gọi khi mở inventory panel).
    // - Có cache mới → hiển thị ngay từ cache.
    // - Client thuần → yêu cầu host qua RPC → host fetch DB → gửi về → cache.
    // - Host / offline → fetch API trực tiếp.
    public void RefreshInventoryFromDB()
    {
        RefreshUiReferences();

        // Cache hit: dữ liệu còn mới, không cần gọi mạng
        if (_cachedInventoryItems != null && !_isCacheDirty)
        {
            { /* Cache còn mới, hiển thị từ cache */ }
            UpdateUIFromDBInventory(_cachedInventoryItems);
            return;
        }

        { /* ========== RefreshInventoryFromDB() GỌI! ========== */ }

        // ✅ FIX: Sau khi chuyển scene (additive), networkInventory có thể bị mất reference.
        // Thử tìm lại trước khi quyết định đường đi.
        if (networkInventory == null || !networkInventory.IsSpawned)
        {
            { /* networkInventory null hoặc chưa spawn, thử tìm lại */ }
            FindPlayerInventory();
        }

        // Đường host-RPC: client thuần gửi yêu cầu lên host, host lấy DB rồi trả về
        if (networkInventory != null && networkInventory.IsSpawned &&
            NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            { /* 📡 Client: yêu cầu inventory từ host qua RPC */ }
            networkInventory.RequestInventoryDataServerRpc();
            return;
        }

        // Hybrid: host-self hoặc offline → dùng GameplayCommandService
        if (GameplayCommandService.Instance != null)
        {
            void HandleInvFallback(string json)
            {
                GameplayCommandService.OnInventoryReceived -= HandleInvFallback;
                var data = JsonUtility.FromJson<PlayerDataResponse>(json);
                if (data?.inventory != null)
                    UpdateUIFromPlayerData(data);
                else
                    ManualSyncInventoryUI();
            }
            GameplayCommandService.OnInventoryReceived -= HandleInvFallback;
            GameplayCommandService.OnInventoryReceived += HandleInvFallback;
            GameplayCommandService.Instance.GetPlayerInventoryServerRpc();
        }
        else
        {
            FetchInventoryDirectFromAPI();
        }
    }

    // Đánh dấu cache cũ – gọi ngay sau khi mua item / trang bị / bỏ trang bị.
    // Lần mở túi tiếp theo sẽ fetch lại từ host/DB.
    public void InvalidateInventoryCache()
    {
        _isCacheDirty = true;
        { /* 🗑️ Inventory cache invalidated */ }
    }

    // Callback từ NetworkInventory.SendInventoryDataClientRpc – host gửi JSON về client.
    // Parse → cache → update UI.
    public void OnReceivedInventoryDataFromHost(string inventoryJson)
    {
        { /* 📦 Nhận inventory từ host ({inventoryJson?.Length ?? 0} chars) */ }

        if (string.IsNullOrEmpty(inventoryJson))
        {
            { /* Cảnh báo: Nhận JSON rỗng từ host */ }
            return;
        }

        try
        {
            var wrapper = JsonUtility.FromJson<NetworkInventory.InventoryJsonWrapper>(inventoryJson);
            var items = wrapper?.items ?? new InventoryItem[0];
            { /* Parse thành công {items.Length} items từ host */ }
            MergeInventoryContextIntoGameManager(
                wrapper?.bag_slots,
                wrapper?.gold,
                wrapper?.silver,
                wrapper?.bag_equipped_items);
            UpdateUIFromDBInventory(
                items,
                wrapper?.bag_slots,
                wrapper?.gold,
                wrapper?.silver,
                wrapper?.bag_equipped_items);
        }
        catch (System.Exception ex)
        {
            { /* Lỗi: Lỗi parse inventory JSON từ host: {ex.Message} */ }
        }
    }

    public void RefreshInventoryDirectFromAPI()
    {
        int playerId = GetCurrentPlayerId();
        if (playerId <= 0)
        {
            { /* Cảnh báo: RefreshInventoryDirectFromAPI: playerId = 0 */ }
            return;
        }

        InvalidateInventoryCache();
        StartCoroutine(FetchInventoryJwtDirect(playerId));
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
            { /* Lấy playerId từ GameManager (in-memory): {playerId} */ }
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
                    { /* Lấy playerId từ ServerPlayerDataManager (clientId={localClientId}): {playerId} */ }
                }
            }
        }
        
        // Fallback cuối cùng: PlayerPrefs (có thể bị shared giữa ParrelSync host/clone)
        if (playerId == 0)
        {
            playerId = PlayerPrefs.GetInt("USER_ID", 0);
            { /* Cảnh báo: Fallback PlayerPrefs USER_ID: {playerId} (có thể không chính xác khi dùng ParrelSync!) */ }
        }
        
        if (playerId == 0)
        {
            { /* Cảnh báo: playerId = 0, không thể fetch inventory từ DB */ }
            ManualSyncInventoryUI();
            return;
        }

        { /* Đang fetch inventory từ DB cho player {playerId} */ }

        if (GameplayCommandService.Instance != null)
        {
            void HandleInvFetch(string json)
            {
                GameplayCommandService.OnInventoryReceived -= HandleInvFetch;
                var data = JsonUtility.FromJson<PlayerDataResponse>(json);
                if (data?.inventory != null)
                {
                    { /* Fetch thành công {data.inventory.Length} items từ DB */ }
                    UpdateUIFromPlayerData(data);
                }
                else
                {
                    { /* Cảnh báo: Inventory data null từ server */ }
                    ManualSyncInventoryUI();
                }
            }
            GameplayCommandService.OnInventoryReceived -= HandleInvFetch;
            GameplayCommandService.OnInventoryReceived += HandleInvFetch;
            GameplayCommandService.Instance.GetPlayerInventoryServerRpc();
        }
        else
        {
            { /* Cảnh báo: GameplayCommandService.Instance is null! Dùng REST trực tiếp với JWT */ }
            StartCoroutine(FetchInventoryJwtDirect(playerId));
        }
    }

    private IEnumerator FetchInventoryJwtDirect(int playerId)
    {
        int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1) == 2 ? 2 : 1;
        string dataEndpoint = geneSlot == 2 ? "data2" : "data";
        string url = $"{APIClient.BASE_URL}/api/player/{playerId}/{dataEndpoint}";
        { /* FetchInventoryJwtDirect: GET {url} */ }
        using var req = UnityWebRequest.Get(url);
        AuthHelper.AddAuthHeader(req);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var data = JsonUtility.FromJson<PlayerDataResponse>(req.downloadHandler.text);
            if (data?.inventory != null)
            {
                { /* FetchInventoryJwtDirect: Nhận {data.inventory.Length} items */ }
                UpdateUIFromPlayerData(data);
            }
            else
            {
                { /* Cảnh báo: FetchInventoryJwtDirect: inventory null trong response */ }
                ManualSyncInventoryUI();
            }
        }
        else
        {
            { /* Cảnh báo: FetchInventoryJwtDirect thất bại: {req.error} */ }
            ManualSyncInventoryUI();
        }
    }

    // Update UI trực tiếp từ DB inventory data (không qua NetworkInventory)
    private void UpdateUIFromDBInventory(
        InventoryItem[] dbItems,
        int? bagSlots = null,
        int? gold = null,
        int? silver = null,
        BagEquippedItemData[] bagEquippedItems = null)
    {
        dbItems ??= System.Array.Empty<InventoryItem>();

        // Lazy-find: inventoryUI có thể chưa được gán nếu Start() chạy trước khi InventoryUI tồn tại
        if (inventoryUI == null)
            inventoryUI = FindObjectOfType<InventoryUI>(true);

        if (inventoryUI == null)
        {
            { /* Cảnh báo: inventoryUI is null! Không thể hiển thị túi đồ */ }
            return;
        }

        { /* UpdateUIFromDBInventory: Converting {dbItems.Length} DB items to DTO */ }

        List<InventorySlotDto> slotDtos = new List<InventorySlotDto>();
        int highestSlotIndex = -1;

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
            if (slot > highestSlotIndex)
                highestSlotIndex = slot;
            if (slot >= autoSlot) autoSlot = slot + 1;
        }

        int resolvedBagSlots = bagSlots ?? 0;
        int resolvedGold = gold ?? 0;
        int resolvedSilver = silver ?? 0;
        BagEquippedItemData[] resolvedBagEquippedItems = bagEquippedItems;

        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var pd = GameManager.Instance.GetPlayerData();
            if (pd != null)
            {
                if (resolvedBagSlots <= 0)
                    resolvedBagSlots = pd.bag_slots > 0 ? pd.bag_slots : 20;

                if (!gold.HasValue)
                    resolvedGold = pd.gold;

                if (!silver.HasValue)
                    resolvedSilver = pd.silver;

                if (resolvedBagEquippedItems == null)
                    resolvedBagEquippedItems = pd.bag_equipped_items;
            }
        }

        if (resolvedBagSlots <= 0)
            resolvedBagSlots = 20;

        if (resolvedBagEquippedItems == null)
            resolvedBagEquippedItems = System.Array.Empty<BagEquippedItemData>();

        inventoryUI.SetVisibleSlotCount(resolvedBagSlots);

        // Tạo DTO cho toàn bộ pool slot, nhưng luôn đủ lớn để chứa slot bag hiện tại và item đang có.
        int maxSlots = Mathf.Max(
            inventoryUI.GetConfiguredMaxSlotCount(),
            resolvedBagSlots,
            highestSlotIndex + 1);

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
                    isEquipped = item.isEquipped,
                    isLocked = item.isLocked,
                    upgradeLevel = item.upgradeLevel,
                    strOptions = item.strOptions ?? string.Empty
                };
                slotDtos.Add(dto);
                
                { /* Slot {i}: {item.itemCode} x{item.quantity} */ }
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

        { /* Đang gửi {slotDtos.Count} slots cho InventoryUI */ }
        inventoryUI.SetInventoryData(slotDtos.ToArray());
        { /* UI đã được update từ DB data */ }

        // Lưu vào cache để lần mở tiếp theo khỏi fetch lại
        _cachedInventoryItems = dbItems;
        _isCacheDirty = false;

        // Thông báo cho ItemUseHandler để cập nhật stat bar, quick-slots túi, v.v.
        ItemUseHandler.Instance?.OnInventoryRefreshed(
            slotDtos.ToArray(),
            resolvedBagSlots,
            resolvedGold,
            resolvedSilver,
            resolvedBagEquippedItems);
    }

    private void UpdateUIFromPlayerData(PlayerDataResponse data)
    {
        if (data == null)
        {
            ManualSyncInventoryUI();
            return;
        }

        GameManager.Instance?.SetPlayerData(data);
        UpdateUIFromDBInventory(
            data.inventory ?? System.Array.Empty<InventoryItem>(),
            data.bag_slots,
            data.gold,
            data.silver,
            data.bag_equipped_items);
    }

    private void MergeInventoryContextIntoGameManager(
        int? bagSlots,
        int? gold,
        int? silver,
        BagEquippedItemData[] bagEquippedItems)
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasPlayerData())
            return;

        var playerData = GameManager.Instance.GetPlayerData();
        if (playerData == null)
            return;

        if (bagSlots.HasValue && bagSlots.Value > 0)
            playerData.bag_slots = bagSlots.Value;

        if (gold.HasValue)
            playerData.gold = gold.Value;

        if (silver.HasValue)
            playerData.silver = silver.Value;

        if (bagEquippedItems != null)
            playerData.bag_equipped_items = bagEquippedItems;

        GameManager.Instance.SetPlayerData(playerData);
    }

    // Public method để manual refresh UI từ NetworkInventory
    // Gọi từ Button UI hoặc debug command
    public void ManualSyncInventoryUI()
    {
        { /* ===================== [InventoryNetworkBridge] MANUAL SYNC ĐƯỢC GỌI! ===================== */ }
        
        if (networkInventory == null)
        {
            { /* Cảnh báo: NetworkInventory is NULL! Đang tìm kiếm */ }
            FindPlayerInventory();
        }

        if (networkInventory != null)
        {
            { /* ✓ Có NetworkInventory, đang refresh UI */ }
            RefreshInventoryUI();
        }
        else
        {
            { /* Lỗi: Vẫn không tìm thấy NetworkInventory sau khi tìm */ }
            
            // Debug: List tất cả NetworkInventory trong scene
            var allInventories = FindObjectsOfType<NetworkInventory>();
            { /* Tổng số NetworkInventory trong scene: {allInventories.Length} */ }
            foreach (var inv in allInventories)
            {
                { /* {inv.gameObject.name}: IsOwner={inv.IsOwner}, IsSpawned={inv.IsSpawned} */ }
            }
        }
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Khi scene mới được load (additive hoặc single), reset reference để tìm lại.
    // Đảm bảo inventory hoạt động trên mọi map, không chỉ GameScene.
    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        { /* Scene loaded: {scene.name} (mode={mode}), invalidate cache + re-find references */ }
        _isCacheDirty = true;

        if (networkInventory != null)
            networkInventory.OnInventoryChanged.RemoveListener(OnInventoryChanged);

        inventoryUI = null;
        equipmentPanelUI = null;
        RefreshUiReferences();

        // Re-find NetworkInventory (player NetworkObject đã được move sang scene mới)
        networkInventory = null;

        StartCoroutine(RebindAfterSceneLoad());
    }

    private void Start()
    {
        { /* ==================== [InventoryNetworkBridge] START() ĐƯỢC GỌI! ==================== */ }

        RefreshUiReferences();
        if (inventoryUI == null)
            { /* Cảnh báo: Không tìm thấy InventoryUI trong scene */ }
        else
            { /* ✓ Tìm thấy InventoryUI: {inventoryUI.name} */ }

        // Kiểm tra NetworkManager
        if (NetworkManager.Singleton == null)
        {
            { /* Lỗi: NetworkManager.Singleton IS NULL! Không thể subscribe network events */ }
        }
        else
        {
            { /* ✓ NetworkManager.Singleton exists */ }
        }

        // Subscribe vào NetworkManager events để tự động tìm NetworkInventory khi client connect
        SubscribeToNetworkEvents();

        // Tìm NetworkInventory nếu chưa gán (có thể chưa có nếu player chưa spawn)
        if (networkInventory == null && autoFindPlayerInventory)
        {
            { /* Đang tìm NetworkInventory lần đầu tiên */ }
            FindPlayerInventory();
        }

        // Subscribe event từ NetworkInventory nếu đã tìm thấy
        if (networkInventory != null)
        {
            { /* ✓ NetworkInventory đã được tìm thấy trong Start(), đang subscribe events */ }
            SubscribeToInventoryEvents();
        }
        else
        {
            { /* Cảnh báo: ⚠️ Chưa tìm thấy NetworkInventory trong Start(), sẽ tìm lại sau khi client connect */ }
        }

        // Subscribe heal-over-time tick từ ActiveBuffManager
        ActiveBuffManager.OnHealTick += ApplyHealTick;
    }

    private void SubscribeToNetworkEvents()
    {
        { /* SubscribeToNetworkEvents() được gọi */ }
        
        if (hasSubscribedToNetworkEvents)
        {
            { /* Đã subscribe rồi, skip */ }
            return;
        }

        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            hasSubscribedToNetworkEvents = true;
            { /* ✓ Đã subscribe OnClientConnectedCallback */ }
        }
        else
        {
            { /* Lỗi: NetworkManager.Singleton is NULL, không thể subscribe events */ }
        }
    }



    private void OnClientConnected(ulong clientId)
    {
        // Chỉ tìm lại nếu là local client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId)
        {
            { /* Client connected (ID: {clientId}), trying to find NetworkInventory */ }
            
            // Đợi một chút để player object được spawn
            StartCoroutine(FindPlayerInventoryDelayed());
        }
    }

    private System.Collections.IEnumerator FindPlayerInventoryDelayed()
    {
        // Đợi 1 giây để player character có thời gian spawn
        { /* Đang đợi player character spawn (1s) */ }
        yield return new WaitForSeconds(1f);

        if (networkInventory == null && autoFindPlayerInventory)
        {
            // Thử tìm tối đa 30 lần, mỗi lần cách nhau 0.2 giây (tổng 6 giây)
            int maxAttempts = 30;
            int currentAttempt = 0;
            
            while (currentAttempt < maxAttempts && networkInventory == null)
            {
                currentAttempt++;
                { /* Lần thử {currentAttempt}/{maxAttempts} */ }
                
                FindPlayerInventory();
                
                if (networkInventory != null)
                {
                    // Tìm thấy rồi!
                    { /* ✓✓✓ Tìm thấy NetworkInventory ở lần thử {currentAttempt} */ }
                    { /* → Đang subscribe to inventory events */ }
                    SubscribeToInventoryEvents();
                    { /* ✓ Subscribe thành công */ }

                    { /* 🔄 Auto-load inventory + equipment từ DB khi vào game/chuyển map */ }
                    RefreshInventoryFromDB();
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
                { /* Lỗi: KHÔNG TÌM THẤY NetworkInventory sau {maxAttempts} lần thử (7 giây)!\n */ }
            }
        }
    }

    private void SubscribeToInventoryEvents()
    {
        if (networkInventory != null)
        {
            { /* ===== SUBSCRIBING TO INVENTORY EVENTS ===== */ }
            { /* NetworkInventory: {networkInventory.gameObject.name} */ }
            { /* IsServer={networkInventory.IsServer}, IsClient={networkInventory.IsClient}, IsOwner={networkInventory.IsOwner} */ }

            networkInventory.OnInventoryChanged.RemoveListener(OnInventoryChanged);
            networkInventory.OnInventoryChanged.AddListener(OnInventoryChanged);
            
            // Refresh ngay lần đầu
            { /* Calling initial RefreshInventoryUI() */ }
            RefreshInventoryUI();
            
            { /* Subscribed to NetworkInventory.OnInventoryChanged */ }
        }
        else
        {
            { /* Lỗi: Cannot subscribe - networkInventory is NULL */ }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        // Unsubscribe heal tick
        ActiveBuffManager.OnHealTick -= ApplyHealTick;

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

    // Gửi heal tick lên NGO server mỗi giây khi có buff HpRestoreOverTime / MpRestoreOverTime.
    private void ApplyHealTick(int hpPerSec, int mpPerSec)
    {
        if (networkInventory == null)
        {
            { /* Cảnh báo: ApplyHealTick: networkInventory null, thử tìm lại */ }
            FindPlayerInventory();
            if (networkInventory == null) return;
        }
        networkInventory.ApplyHealTickServerRpc(hpPerSec, mpPerSec);
    }

    // Tìm NetworkInventory của local player
    private void FindPlayerInventory()
    {
        { /* ========== FindPlayerInventory() BẮT ĐẦU ========== */ }
        
        if (NetworkManager.Singleton == null)
        {
            { /* Cảnh báo: NetworkManager.Singleton is null */ }
            return;
        }

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        // Kiểm tra SpawnManager có sẵn sàng không
        if (NetworkManager.Singleton.SpawnManager == null)
        {
            { /* Cảnh báo: NetworkManager.SpawnManager is null! Network may not be initialized yet */ }
            return;
        }

        // Kiểm tra SpawnedObjectsList có sẵn sàng không
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjectsList == null)
        {
            { /* Cảnh báo: SpawnedObjectsList is null! No objects spawned yet */ }
            return;
        }
        
        { /* SpawnedObjectsList count: {NetworkManager.Singleton.SpawnManager.SpawnedObjectsList.Count}, LocalClientId: {localClientId} */ }

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

            bool isLocalOwnedObject = networkObject.IsOwner
                                      || networkObject.IsLocalPlayer
                                      || (networkObject.IsPlayerObject && networkObject.OwnerClientId == localClientId);

            // Kiểm tra tất cả objects thuộc local client
            if (isLocalOwnedObject)
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
                { /* ✓ Tìm thấy player character: '{networkObject.name}' */ }

                // Kiểm tra có NetworkInventory không
                NetworkInventory inv = networkObject.GetComponent<NetworkInventory>();
                // Debug.Log($"[InventoryNetworkBridge]   → Has NetworkInventory: {inv != null}");

                if (inv != null)
                {
                    networkInventory = inv;
                    { /* ✓✓✓ TÌM THẤY NetworkInventory của player: {networkObject.name} */ }
                    { /* → NetworkInventory GameObject: {networkObject.gameObject.name} */ }
                    { /* → OwnerClientId: {networkObject.OwnerClientId} (LocalClientId={localClientId}) */ }
                    { /* → IsSpawned: {networkObject.IsSpawned} */ }
                    { /* → Component found at: {inv.GetType().FullName} */ }
                    return;
                }
                else
                {
                    { /* Cảnh báo: ⚠️ Player character '{networkObject.name}' KHÔNG có NetworkInventory component */ }
                }
            }
        }

        // Debug.Log($"[InventoryNetworkBridge] ========== KẾT QUẢ TÌM KIẾM ==========");
        { /* Cảnh báo: Không tìm thấy NetworkInventory. Owned objects: {ownedObjectsFound}, Player characters: {playerCharactersFound} */ }
        
        if (playerCharactersFound == 0)
        {
            // Debug chỉ khi có owned objects nhưng không phải player character
            if (ownedObjectsFound > 0)
            {
                { /* Có {ownedObjectsFound} owned object(s) nhưng không phải player character (utility objects). Đợi player spawn */ }
            }
        }
    }

    // Callback khi NetworkInventory thay đổi
    private void OnInventoryChanged()
    {
        { /* ========== [InventoryNetworkBridge] OnInventoryChanged EVENT RECEIVED! ========== */ }
        { /* Client/Server: IsClient={NetworkManager.Singleton?.IsClient}, IsServer={NetworkManager.Singleton?.IsServer} */ }
        RefreshInventoryUI();
    }

    // Convert từ NetworkInventory → InventorySlotDto[] và gửi cho InventoryUI
    private void RefreshInventoryUI()
    {
        if (networkInventory == null)
        {
            { /* Cảnh báo: RefreshInventoryUI: networkInventory is null */ }
            return;
        }
        if (inventoryUI == null)
        {
            { /* Cảnh báo: RefreshInventoryUI: inventoryUI is null */ }
            return;
        }

        { /* RefreshInventoryUI: NetworkInventory thay đổi, tiến hành fetch full data từ DB */ }
        InvalidateInventoryCache();
        RefreshInventoryFromDB();
    }

    private IEnumerator UseItemDirectCoroutine(int playerId, int slotIndex)
    {
        string url = $"{APIClient.BASE_URL}/api/player/{playerId}/inventory/use-item";
        int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1) == 2 ? 2 : 1;
        string body = $"{{\"slotIndex\":{slotIndex},\"geneSlot\":{geneSlot}}}";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;
        req.SetRequestHeader("Content-Type", "application/json");
        AuthHelper.AddAuthHeader(req);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string error = !string.IsNullOrWhiteSpace(req.downloadHandler?.text)
                ? req.downloadHandler.text
                : $"HTTP {(long)req.responseCode}: {req.error}";
            { /* Lỗi: Direct use-item fallback failed: {error} */ }
            GlobalNotificationUI.Show(error, "Vat Pham", 3.5f, "OK");
            yield break;
        }

        { /* Direct use-item fallback OK: {req.downloadHandler.text} */ }
        HandleDirectUseItemResponse(req.downloadHandler.text);
        InvalidateInventoryCache();
        StartCoroutine(FetchInventoryJwtDirect(playerId));
    }

    private void HandleDirectUseItemResponse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        var response = JsonUtility.FromJson<UseItemResult>(json);
        if (response == null)
        {
            { /* Cảnh báo: UseItem response parse failed. Raw={json} */ }
            ActiveBuffManager.Instance?.LoadFromServer();
            return;
        }

        if (response.hp_restore > 0 || response.mp_restore > 0)
            RequestSyncHpMp(response.current_hp, response.current_mp);

        bool changedBuffs = false;
        if (response.active_buffs != null && response.active_buffs.Length > 0)
        {
            ActiveBuffManager.Instance?.OnBuffsReceived(response.active_buffs);
            changedBuffs = true;
        }
        else if (response.new_buffs != null && response.new_buffs.Length > 0)
        {
            ActiveBuffManager.Instance?.OnBuffsAdded(response.new_buffs);
            changedBuffs = true;
        }

        if (changedBuffs)
            RequestSyncBuffBonuses();

        ActiveBuffManager.Instance?.LoadFromServer();
    }

    // Lấy iconId từ ItemData
    // Ưu tiên: dùng sprite.name làm iconId (nếu sprite.name trùng với iconId trong DB)
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

    // Public API để gán NetworkInventory từ bên ngoài (dùng khi player spawn runtime)
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

    // Public API để gán InventoryUI từ bên ngoài
    public void SetInventoryUI(InventoryUI ui)
    {
        inventoryUI = ui;
        if (inventoryUI != null && networkInventory != null)
        {
            RefreshInventoryUI();
        }
    }

    // Trả về túi đồ hiện tại (dùng cho UpgradePanel)
    public InventorySlotDto[] CurrentInventory => inventoryUI?.CurrentSlots;

    // Lấy playerId hiện tại từ GameManager hoặc PlayerPrefs
    public int GetCurrentPlayerId()
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

    // Gửi request sử dụng item lên server (gọi từ ItemDetailPanel khi nhấn nút Sử dụng).
    // Ưu tiên dùng ItemUseHandler; phương thức này giữ lại như fallback.
    public void RequestUseItem(int slotIndex, string itemCode, int itemTemplateId = 0)
    {
        { /* RequestUseItem (fallback): slotIndex={slotIndex}, itemCode={itemCode} */ }

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

        { /* ItemUseHandler unavailable, using direct REST use-item fallback */ }
        StartCoroutine(UseItemDirectCoroutine(playerId, slotIndex));
    }

    public void RequestRemoveItem(int slotIndex, int quantity)
    {
        int playerId = GetCurrentPlayerId();
        if (playerId == 0)
        {
            { /* Cảnh báo: RequestRemoveItem: playerId = 0 */ }
            return;
        }

        if (GameplayCommandService.Instance == null || !GameplayCommandService.Instance.IsSpawned)
        {
            { /* Cảnh báo: RequestRemoveItem: GameplayCommandService unavailable, using direct REST fallback */ }
            StartCoroutine(RemoveItemDirectCoroutine(playerId, slotIndex, quantity));
            return;
        }

        void HandleRemoveResult(string json)
        {
            GameplayCommandService.OnRemoveItemResult -= HandleRemoveResult;
            if (string.IsNullOrWhiteSpace(json) || json.Contains("\"error\""))
            {
                { /* Lỗi: Remove item failed: {json} */ }
                GlobalNotificationUI.Show(ExtractErrorMessage(json, "Khong the vut bo item."), "Vat Pham", 3.5f, "Dong");
                return;
            }

            { /* Remove item OK: {json} */ }
            InvalidateInventoryCache();
            RefreshInventoryFromDB();
        }

        GameplayCommandService.OnRemoveItemResult -= HandleRemoveResult;
        GameplayCommandService.OnRemoveItemResult += HandleRemoveResult;
        GameplayCommandService.Instance.RemoveInventoryItemServerRpc(slotIndex, quantity);
    }

    private IEnumerator RemoveItemDirectCoroutine(int playerId, int slotIndex, int quantity)
    {
        string url = $"{APIClient.BASE_URL}/api/player/{playerId}/inventory/remove";
        string body = $"{{\"slotIndex\":{slotIndex},\"quantity\":{quantity}}}";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;
        req.SetRequestHeader("Content-Type", "application/json");
        AuthHelper.AddAuthHeader(req);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string error = !string.IsNullOrWhiteSpace(req.downloadHandler?.text)
                ? req.downloadHandler.text
                : $"HTTP {(long)req.responseCode}: {req.error}";
            { /* Lỗi: Direct remove-item fallback failed: {error} */ }
            GlobalNotificationUI.Show(ExtractErrorMessage(error, "Khong the vut bo item."), "Vat Pham", 3.5f, "Dong");
            yield break;
        }

        { /* Direct remove-item fallback OK: {req.downloadHandler.text} */ }
        InvalidateInventoryCache();
        StartCoroutine(FetchInventoryJwtDirect(playerId));
    }

    private static string ExtractErrorMessage(string jsonOrText, string fallback)
    {
        if (string.IsNullOrWhiteSpace(jsonOrText))
            return fallback;

        const string marker = "\"error\":\"";
        int start = jsonOrText.IndexOf(marker, System.StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return jsonOrText;

        start += marker.Length;
        int end = jsonOrText.IndexOf('"', start);
        return end > start ? jsonOrText.Substring(start, end - start) : fallback;
    }

    // Lấy itemTemplateId từ inventory slot (dùng cache hiện tại)
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

    // Gửi request áp dụng stat effect (HP/MP) của consumable lên server qua NGO.
    // Gọi SAU KHI REST API đã persist việc tiêu thụ item.
    public void RequestApplyStatEffect(int templateId)
    {
        if (networkInventory == null)
        {
            { /* Cảnh báo: RequestApplyStatEffect: networkInventory is null */ }
            return;
        }
        networkInventory.ApplyConsumableStatServerRpc(templateId);
    }

    // Sync HP/MP trực tiếp từ giá trị authoritative của REST API lên NGO.
    // Dùng cho instant HP/MP restore để thanh HP/MP cập nhật ngay lập tức.
    public void RequestSyncHpMp(int currentHp, int currentMp)
    {
        if (networkInventory == null) return;
        networkInventory.ApplySyncHpMpServerRpc(currentHp, currentMp);
    }

    // Sync % bonus buff (GeneExp, Exp, Phúc, ATK, DEF) lên server qua NGO.
    // Gọi sau khi client nhận active_buffs từ REST API.
    // Dùng ActiveBuffManager.GetBonusPct() để lấy tổng %.
    public void RequestSyncBuffBonuses()
    {
        if (networkInventory == null || ActiveBuffManager.Instance == null) return;
        int geneExp  = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("GeneExpBuff")  * 100);
        int exp      = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("ExpBuff")       * 100);
        int phuc     = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("PhucBuff")      * 100);
        int atk      = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("AttackBuff")    * 100);
        int def      = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("DefenseBuff")   * 100);
        networkInventory.SyncBuffBonusesServerRpc(geneExp, exp, phuc, atk, def);
    }

    // Cập nhật Max HP / Max MP lên NGO server sau khi HpBuff / MpBuff được áp dụng.
    // Gọi sau khi reload player data từ REST API.
    public void RequestUpdatePlayerStats(int newMaxHp, int newMaxMp)
    {
        if (networkInventory == null) return;
        var dataSync = networkInventory.GetComponent<NetworkPlayerDataSync>();
        if (dataSync == null) return;
        dataSync.UpdateMaxHpMpServerRpc(newMaxHp, newMaxMp);
    }

    // Gửi request trang bị item lên server
    // Gọi từ ItemDetailPanel khi nhấn nút "Trang bị"
    // Server sẽ: remove item khỏi inventory, thêm vào equipment slot,
    // nếu slot đã có item cũ thì swap (item cũ quay về inventory)
    public void RequestEquipItem(int inventorySlotIndex, string itemCode)
    {
        { /* ⚔️ RequestEquipItem: slotIndex={inventorySlotIndex}, itemCode={itemCode} */ }

        int playerId = GetCurrentPlayerId();
        if (playerId == 0)
        {
            { /* Cảnh báo: RequestEquipItem: playerId = 0 */ }
            return;
        }

        if (GameplayCommandService.Instance == null || !GameplayCommandService.Instance.IsSpawned)
        {
            { /* Cảnh báo: RequestEquipItem: GameplayCommandService unavailable, using direct REST fallback */ }
            StartCoroutine(EquipItemDirectCoroutine(playerId, inventorySlotIndex));
            return;
        }

        { /* ⚔️ Đang gửi equip request lên server: slot={inventorySlotIndex}, item={itemCode} */ }

        void HandleEquipResult(string json)
        {
            GameplayCommandService.OnEquipResult -= HandleEquipResult;
            if (json.Contains("\"error\""))
            {
                { /* Lỗi: Equip thất bại: {json} */ }
                if (ShouldUseDirectEquipFallback(json))
                    StartCoroutine(EquipItemDirectCoroutine(playerId, inventorySlotIndex));
                return;
            }
            { /* Equip thành công */ }
            InvalidateInventoryCache();
            RefreshInventoryFromDB();
            RefreshEquipmentFromDB();
            // Refresh final_stats
            void HandlePlayerData(string pdJson)
            {
                GameplayCommandService.OnPlayerDataReceived -= HandlePlayerData;
                var data = JsonUtility.FromJson<PlayerDataResponse>(pdJson);
                if (data != null) GameManager.Instance?.SetPlayerData(data);
            }
            GameplayCommandService.OnPlayerDataReceived -= HandlePlayerData;
            GameplayCommandService.OnPlayerDataReceived += HandlePlayerData;
            GameplayCommandService.Instance.RequestPlayerDataServerRpc();
        }
        GameplayCommandService.OnEquipResult -= HandleEquipResult;
        GameplayCommandService.OnEquipResult += HandleEquipResult;
        GameplayCommandService.Instance.EquipItemServerRpc(inventorySlotIndex);
    }

    private IEnumerator EquipItemDirectCoroutine(int playerId, int inventorySlotIndex)
    {
        string url = $"{APIClient.BASE_URL}/api/player/{playerId}/equipment/equip";
        string body = $"{{\"inventorySlotIndex\":{inventorySlotIndex}}}";
        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;
        req.SetRequestHeader("Content-Type", "application/json");
        AuthHelper.AddAuthHeader(req);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string error = !string.IsNullOrWhiteSpace(req.downloadHandler?.text)
                ? req.downloadHandler.text
                : $"HTTP {(long)req.responseCode}: {req.error}";
            { /* Lỗi: Direct equip fallback failed: {error} */ }
            yield break;
        }

        { /* Direct equip fallback succeeded */ }
        InvalidateInventoryCache();
        StartCoroutine(FetchInventoryJwtDirect(playerId));
        StartCoroutine(FetchEquipmentDirectCoroutine(playerId));
    }

    private static bool ShouldUseDirectEquipFallback(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return true;

        string e = json.ToLowerInvariant();
        return e.Contains("http 0")
            || e.Contains("http 401")
            || e.Contains("unauthorized")
            || e.Contains("connection")
            || e.Contains("connect")
            || e.Contains("timeout")
            || e.Contains("network")
            || e.Contains("name resolution")
            || e.Contains("dns")
            || e.Contains("refused")
            || e.Contains("localhost");
    }

    // Gửi request tháo trang bị lên server
    // Gọi từ EquipmentPanelUI khi click tháo
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

        { /* 🔧 RequestUnequipItem: slot={slotName} */ }

        int playerId = GetCurrentPlayerId();
        if (playerId == 0)
        {
            { /* Cảnh báo: RequestUnequipItem: playerId = 0 */ }
            return;
        }

        if (GameplayCommandService.Instance == null)
        {
            { /* Cảnh báo: RequestUnequipItem: GameplayCommandService.Instance is null */ }
            return;
        }

        void HandleUnequipResult(string json)
        {
            GameplayCommandService.OnUnequipResult -= HandleUnequipResult;
            if (json.Contains("\"error\""))
            {
                { /* Lỗi: Unequip thất bại: {json} */ }
                return;
            }
            { /* Unequip thành công */ }
            InvalidateInventoryCache();
            RefreshInventoryFromDB();
            RefreshEquipmentFromDB();
            // Refresh final_stats
            void HandlePlayerData(string pdJson)
            {
                GameplayCommandService.OnPlayerDataReceived -= HandlePlayerData;
                var data = JsonUtility.FromJson<PlayerDataResponse>(pdJson);
                if (data != null) GameManager.Instance?.SetPlayerData(data);
            }
            GameplayCommandService.OnPlayerDataReceived -= HandlePlayerData;
            GameplayCommandService.OnPlayerDataReceived += HandlePlayerData;
            GameplayCommandService.Instance.RequestPlayerDataServerRpc();
        }
        GameplayCommandService.OnUnequipResult -= HandleUnequipResult;
        GameplayCommandService.OnUnequipResult += HandleUnequipResult;
        GameplayCommandService.Instance.UnequipItemServerRpc(slotName);
    }

    // Gửi request tháo túi mở rộng khỏi quick slot.
    // Gọi từ ItemUseHandler khi người chơi click BagQuickSlot trên HUD.
    public void RequestUnequipBagItem(int quickSlotIndex, System.Action<string> onResult = null)
    {
        { /* RequestUnequipBagItem: quickSlotIndex={quickSlotIndex} */ }

        int playerId = GetCurrentPlayerId();
        if (playerId == 0)
        {
            { /* Cảnh báo: RequestUnequipBagItem: playerId = 0 */ }
            onResult?.Invoke("{\"error\":\"playerId = 0\"}");
            return;
        }

        if (GameplayCommandService.Instance == null)
        {
            { /* Cảnh báo: RequestUnequipBagItem: GameplayCommandService.Instance is null */ }
            onResult?.Invoke("{\"error\":\"GameplayCommandService unavailable\"}");
            return;
        }

        void HandleBagUnequipResult(string json)
        {
            GameplayCommandService.OnBagUnequipResult -= HandleBagUnequipResult;
            onResult?.Invoke(json);

            if (string.IsNullOrEmpty(json) || json.Contains("\"error\""))
            {
                { /* Lỗi: Unequip bag thất bại: {json} */ }
                return;
            }

            { /* Unequip bag thành công */ }
            InvalidateInventoryCache();
            RefreshInventoryFromDB();

            void HandlePlayerData(string pdJson)
            {
                GameplayCommandService.OnPlayerDataReceived -= HandlePlayerData;
                var data = JsonUtility.FromJson<PlayerDataResponse>(pdJson);
                if (data == null)
                    return;

                GameManager.Instance?.SetPlayerData(data);
                ItemUseHandler.Instance?.RefreshStatBar();
            }

            GameplayCommandService.OnPlayerDataReceived -= HandlePlayerData;
            GameplayCommandService.OnPlayerDataReceived += HandlePlayerData;
            GameplayCommandService.Instance.RequestPlayerDataServerRpc();
        }

        GameplayCommandService.OnBagUnequipResult -= HandleBagUnequipResult;
        GameplayCommandService.OnBagUnequipResult += HandleBagUnequipResult;
        GameplayCommandService.Instance.UnequipBagItemServerRpc(quickSlotIndex);
    }

    // Refresh equipment UI từ DB
    // Gọi khi mở EquipmentPanel hoặc sau khi equip/unequip
    public void RefreshEquipmentFromDB()
    {
        { /* 🔄 RefreshEquipmentFromDB() */ }

        RefreshUiReferences();

        int playerId = GetCurrentPlayerId();
        if (playerId == 0)
        {
            { /* Cảnh báo: RefreshEquipmentFromDB: playerId = 0 */ }
            return;
        }

        if (GameplayCommandService.Instance == null || !GameplayCommandService.Instance.IsSpawned)
        {
            { /* Cảnh báo: RefreshEquipmentFromDB: GameplayCommandService unavailable, using direct REST fallback */ }
            StartCoroutine(FetchEquipmentDirectCoroutine(playerId));
            return;
        }

        // Tìm EquipmentPanelUI nếu chưa có
        if (equipmentPanelUI == null)
        {
            equipmentPanelUI = FindObjectOfType<EquipmentPanelUI>(true);
        }

        void HandleEquipment(string json)
        {
            GameplayCommandService.OnEquipmentReceived -= HandleEquipment;
            var equipment = EquipmentPayloadParser.Parse(json);
            if (equipment != null)
            {
                { /* Equipment loaded from DB */ }
                if (equipmentPanelUI != null)
                    equipmentPanelUI.SetEquipmentData(equipment);
            }
            else
            {
                { /* Lỗi: Failed to parse equipment JSON */ }
            }
        }
        GameplayCommandService.OnEquipmentReceived -= HandleEquipment;
        GameplayCommandService.OnEquipmentReceived += HandleEquipment;
        GameplayCommandService.Instance.GetPlayerEquipmentServerRpc();
    }

    private IEnumerator FetchEquipmentDirectCoroutine(int playerId)
    {
        if (equipmentPanelUI == null)
            equipmentPanelUI = FindObjectOfType<EquipmentPanelUI>(true);

        string url = $"{APIClient.BASE_URL}/api/player/{playerId}/equipment";
        using var req = UnityWebRequest.Get(url);
        req.timeout = 10;
        AuthHelper.AddAuthHeader(req);

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            string error = !string.IsNullOrWhiteSpace(req.downloadHandler?.text)
                ? req.downloadHandler.text
                : $"HTTP {(long)req.responseCode}: {req.error}";
            { /* Lỗi: Direct equipment fetch failed: {error} */ }
            yield break;
        }

        var equipment = EquipmentPayloadParser.Parse(req.downloadHandler.text);
        if (equipment != null)
        {
            { /* Equipment loaded via direct REST fallback */ }
            equipmentPanelUI?.SetEquipmentData(equipment);
        }
        else
        {
            { /* Lỗi: Failed to parse direct equipment JSON */ }
        }
    }

    private IEnumerator RebindAfterSceneLoad()
    {
        yield return null;
        RefreshUiReferences();

        FindPlayerInventory();
        if (networkInventory != null)
        {
            SubscribeToInventoryEvents();
            RefreshInventoryFromDB();
            RefreshEquipmentFromDB();
            yield break;
        }

        if (autoFindPlayerInventory)
            StartCoroutine(FindPlayerInventoryDelayed());
    }
}
