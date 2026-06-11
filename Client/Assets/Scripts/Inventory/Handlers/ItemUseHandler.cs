using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine.Networking;

// ItemUseHandler — Xử lý toàn bộ logic sử dụng item trong túi đồ.
// Trách nhiệm:
// 1. Nhận sự kiện "Sử dụng" từ ItemDetailPanel.
// 2. Phân loại item (equipment / consumable / bag expansion) và gọi API tương ứng.
// 3. Quản lý 3 Quick-Slot hiển thị icon item mở rộng túi đồ.
// 4. Xử lý nút Sắp xếp (compact inventory qua API).
// 5. Hiển thị số túi đang có, vàng, bạc.
// Gắn script này vào GameObject duy nhất trong scene (ví dụ: "InventoryManager").
public class ItemUseHandler : MonoBehaviour
{
    // Singleton
    public static ItemUseHandler Instance { get; private set; }

    // Loại item
    // type = 32 trong item_template → item mở rộng túi đồ (+5 ô). KHÔNG phải type 30 (vật liệu).
    public const int ItemTypeBag        = 32;
    // Số ô mở rộng mỗi lần dùng item túi.
    public const int BagExpandAmount    = 5;
    // type 21-29 → item tiêu thụ (phục hồi HP/MP, v.v.).
    public const int ItemTypeConsumableMin = 21;
    public const int ItemTypeConsumableMax = 29;
    public const int ItemTypeWaveTicket = 31;
    // type 0-5 → trang bị.
    public const int ItemTypeEquipMax   = 5;

    // Inspector References
    [Header("References")]
    [Tooltip("InventoryNetworkBridge để gọi APIs (equip/unequip/refresh)")]
    [SerializeField] private InventoryNetworkBridge inventoryBridge;

    [Tooltip("InventoryUI để refresh sau khi dùng item")]
    [SerializeField] private InventoryUI inventoryUI;

    [Header("Stat Bar")]
    [Tooltip("Text hiển thị lượng vàng")]
    [SerializeField] private TMP_Text goldText;

    [Tooltip("Text hiển thị lượng bạc")]
    [SerializeField] private TMP_Text silverText;

    [Tooltip("Text hiển thị số ô túi đồ (ví dụ: 20/20)")]
    [SerializeField] private TMP_Text bagSlotCountText;

    [Header("Quick Slots (Bag Expansion Items)")]
    [Tooltip("3 Image dùng để hiển thị icon item túi ở các slot nhanh")]
    [SerializeField] private Image[] bagQuickSlotIcons;  // length = 3

    [Tooltip("3 TMP_Text hiển thị số lượng item túi tương ứng")]
    [SerializeField] private TMP_Text[] bagQuickSlotCounts;  // length = 3

    [Tooltip("3 Image overlay tối (90% kích thước) – bật khi slot có item, tắt khi trống. Gán ItemBg Image của BagSlot0/1/3.")]
    [SerializeField] private Image[] bagSlotItemBgs;  // length = 3

    [Tooltip("Sprite hiển thị khi slot nhanh trống")]
    [SerializeField] private Sprite emptySlotSprite;

    [Tooltip("Sprite mặc định hiển thị trên quick slot khi túi mở rộng không có icon riêng (idIcon = 0). Gán 1 sprite 'bag' bất kỳ từ atlas vào đây.")]
    [SerializeField] private Sprite defaultBagIcon;

    [Tooltip("Prefab BagQuickActionPanel dùng khi click vào BagQuickSlot trên HUD.")]
    [SerializeField] private BagQuickActionPanel bagQuickActionPanelPrefab;

    [Header("UI Elements")]
    [Tooltip("Image/GameObject hiển thị khi item đang khóa")]
    [SerializeField] private Sprite lockIcon;

    [Tooltip("Nút sắp xếp túi đồ")]
    [SerializeField] private Button sortButton;

    // Private state
    private int currentBagSlots = 20;
    private int currentGold;
    private int currentSilver;

    // Slot data của các item túi tìm thấy trong inventory (tối đa 3).
    private readonly Dictionary<int, BagEquippedItemData> _equippedBagItemsByQuickSlot = new Dictionary<int, BagEquippedItemData>(3);
    private readonly List<Button> _bagQuickSlotButtons = new List<Button>(3);
    private BagQuickActionPanel _bagQuickActionPanel;

    // Unity Lifecycle
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (inventoryBridge == null)
            inventoryBridge = FindObjectOfType<InventoryNetworkBridge>();

        if (inventoryUI == null)
            inventoryUI = FindObjectOfType<InventoryUI>();

        if (sortButton != null)
            sortButton.onClick.AddListener(RequestSortInventory);

        SetupBagQuickSlotButtons();
        RefreshStatBar();
    }

    private void OnDestroy()
    {
        if (sortButton != null)
            sortButton.onClick.RemoveListener(RequestSortInventory);

        foreach (var button in _bagQuickSlotButtons)
        {
            if (button != null)
                button.onClick.RemoveAllListeners();
        }

        if (Instance == this) Instance = null;
    }

    private void SetupBagQuickSlotButtons()
    {
        _bagQuickSlotButtons.Clear();

        int quickSlotCount = bagQuickSlotIcons?.Length ?? 0;
        for (int i = 0; i < quickSlotCount; i++)
        {
            Image icon = bagQuickSlotIcons[i];
            if (icon == null)
            {
                _bagQuickSlotButtons.Add(null);
                continue;
            }

            GameObject buttonRoot = icon.transform.parent != null ? icon.transform.parent.gameObject : icon.gameObject;
            Button button = buttonRoot.GetComponent<Button>();
            if (button == null)
                button = buttonRoot.AddComponent<Button>();

            button.targetGraphic = icon;
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.RemoveAllListeners();

            int quickSlotIndex = i;
            button.onClick.AddListener(() => OnBagQuickSlotClicked(quickSlotIndex));
            _bagQuickSlotButtons.Add(button);
        }
    }

    private void OnBagQuickSlotClicked(int quickSlotIndex)
    {
        Debug.Log($"[ItemUseHandler] BagSlot clicked: quickSlotIndex={quickSlotIndex}");
        Debug.Log($"[ItemUseHandler] _equippedBagItems count={_equippedBagItemsByQuickSlot.Count}");

        SyncBagQuickSlotsFromPlayerDataIfNeeded();

        if (!_equippedBagItemsByQuickSlot.TryGetValue(quickSlotIndex, out var bagItem) || bagItem == null)
        {
            Debug.LogWarning($"[ItemUseHandler] Không tiìm thấy bagItem tại slot {quickSlotIndex} — bỏ qua.");
            return;
        }

        var actionPanel = GetOrCreateBagQuickActionPanel();
        if (actionPanel == null)
        {
            Debug.LogError("[ItemUseHandler] actionPanel null!");
            return;
        }

        inventoryUI?.HideItemDetail();

        Debug.Log($"[ItemUseHandler] actionPanel='{actionPanel.gameObject.name}' active={actionPanel.gameObject.activeInHierarchy}");

        // Lấy RectTransform của slot vừa click để định vị panel
        RectTransform slotRect = null;
        if (quickSlotIndex < _bagQuickSlotButtons.Count && _bagQuickSlotButtons[quickSlotIndex] != null)
            slotRect = _bagQuickSlotButtons[quickSlotIndex].transform as RectTransform;

        string itemName = BuildBagItemDisplayName(bagItem);
        Debug.Log($"[ItemUseHandler] Gọi Show(): itemName='{itemName}' slotRect={slotRect?.name}");
        actionPanel.Show(
            itemName,
            () => RequestUnequipBagQuickSlot(quickSlotIndex),
            () => inventoryUI?.ShowItemDetail(ConvertBagItemToSlotDto(bagItem), showUseButton: false),
            (Vector2)Input.mousePosition);
    }

    private BagQuickActionPanel GetOrCreateBagQuickActionPanel()
    {
        // Dùng instance đã tạo trước
        if (_bagQuickActionPanel != null)
            return _bagQuickActionPanel;

        // Instantiate từ prefab dưới root Canvas (luôn active)
        if (bagQuickActionPanelPrefab != null)
        {
            Transform canvasRoot = ResolveCanvasParent();
            _bagQuickActionPanel = Instantiate(bagQuickActionPanelPrefab, canvasRoot);
            Debug.Log($"[ItemUseHandler] Instantiated BagQuickActionPanel prefab dưới '{canvasRoot.name}'");
            return _bagQuickActionPanel;
        }

        // Fallback runtime
        Debug.LogWarning("[ItemUseHandler] Chưa gán Prefab — dùng Create() runtime.");
        _bagQuickActionPanel = BagQuickActionPanel.Create(ResolveCanvasParent());
        return _bagQuickActionPanel;
    }

    private Transform ResolveCanvasParent()
    {
        // BagQuickActionPanel là overlay độc lập, ưu tiên root canvas có sortingOrder cao nhất.
        Canvas bestCanvas = null;
        int bestOrder = int.MinValue;
        foreach (var canvas in FindObjectsOfType<Canvas>(true))
        {
            if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
            if (!canvas.gameObject.activeInHierarchy) continue;
            if (canvas.sortingOrder > bestOrder) { bestOrder = canvas.sortingOrder; bestCanvas = canvas; }
        }

        if (bestCanvas != null)
            return bestCanvas.transform;

        Transform panelParent = inventoryUI != null && inventoryUI.GetSharedItemDetailPanel() != null
            ? inventoryUI.GetSharedItemDetailPanel().transform.parent
            : null;

        return panelParent != null ? panelParent : transform.root;
    }

    // Public API: gọi từ ItemDetailPanel

    // Entry point khi người chơi nhấn nút "Sử dụng" trên ItemDetailPanel.
    // Tự động phân loại item và gọi handler tương ứng.
    public void RequestUseItem(InventorySlotDto slot)
    {
        if (slot == null || slot.quantity <= 0)
        {
            Debug.LogWarning("[ItemUseHandler] RequestUseItem: slot null hoặc quantity = 0.");
            return;
        }

        // Lấy template để kiểm tra loại
        ItemTemplateDto template = null;
        if (ItemTemplateManager.Instance != null)
        {
            if (slot.itemTemplateId > 0)
                template = ItemTemplateManager.Instance.GetItemTemplate(slot.itemTemplateId);
            if (template == null && !string.IsNullOrEmpty(slot.itemCode))
                template = ItemTemplateManager.Instance.GetItemTemplateByCode(slot.itemCode);
        }

        int itemType = template?.type ?? -1;
        Debug.Log($"[ItemUseHandler] RequestUseItem: slot={slot.slotIndex}, templateId={slot.itemTemplateId}, type={itemType}");

        if (TryUseItemInBlacksmith(slot))
            return;

        if (itemType >= 0 && itemType <= ItemTypeEquipMax)
        {
            // Trang bị
            DoEquipItem(slot, template);
        }
        else if (itemType == ItemTypeBag)
        {
            // Mở rộng túi
            DoUseBagItem(slot);
        }
        else if (itemType == ItemTypeWaveTicket)
        {
            // Vé phó bản sóng 409/410
            DoUseConsumableItem(slot);
        }
        else if (itemType >= ItemTypeConsumableMin && itemType <= ItemTypeConsumableMax)
        {
            // Item tiêu thụ
            DoUseConsumableItem(slot);
        }
        else
        {
            Debug.LogWarning($"[ItemUseHandler] Không xác định được loại item (type={itemType}), thử dùng như consumable.");
            DoUseConsumableItem(slot);
        }
    }

    // Item Use Handlers

    // Trang bị item (equipment type 0-5) qua bridge.
    private void DoEquipItem(InventorySlotDto slot, ItemTemplateDto template)
    {
        Debug.Log($"[ItemUseHandler] ⚔️ Trang bị item: slot={slot.slotIndex}, code={slot.itemCode}");
        inventoryBridge?.RequestEquipItem(slot.slotIndex, slot.itemCode);
    }

    // Sử dụng item tiêu thụ (type 21-29): gọi GameplayCommandService → áp dụng HP/MP qua NGO → cập nhật buff HUD.
    private void DoUseConsumableItem(InventorySlotDto slot)
    {
        Debug.Log($"[ItemUseHandler] 🍶 Sử dụng consumable: slot={slot.slotIndex}");
        if (!CanUseGameplayCommandService())
        {
            Debug.LogError("[ItemUseHandler] GameplayCommandService chưa spawn. " +
                           "Kiểm tra NetworkManagers.prefab có GameplayCommandService và đã được ServerBootstrap spawn.");
            UseItemDirectFromApi(slot, isBagItem: false, "GameplayCommandService unavailable");
            return;
        }

        SendUseConsumableRequest(slot);
    }

    private void SendUseConsumableRequest(InventorySlotDto slot)
    {
        void HandleUseResult(string json)
        {
            GameplayCommandService.OnUseItemResult -= HandleUseResult;
            HandleUseItemResponse(json, isBagItem: false, sourceSlot: slot, allowDirectFallback: true);
        }
        GameplayCommandService.OnUseItemResult -= HandleUseResult;
        GameplayCommandService.OnUseItemResult += HandleUseResult;
        GameplayCommandService.Instance.UseInventoryItemServerRpc(slot.slotIndex);
    }

    // Sử dụng item mở rộng túi (type 30): gọi GameplayCommandService use-item + cập nhật bag count.
    private void DoUseBagItem(InventorySlotDto slot)
    {
        Debug.Log($"[ItemUseHandler] 🎒 Mở rộng túi đồ: slot={slot.slotIndex}");
        if (!CanUseGameplayCommandService())
        {
            Debug.LogError("[ItemUseHandler] GameplayCommandService chưa spawn. " +
                           "Kiểm tra NetworkManagers.prefab có GameplayCommandService và đã được ServerBootstrap spawn.");
            UseItemDirectFromApi(slot, isBagItem: true, "GameplayCommandService unavailable");
            return;
        }

        void HandleBagResult(string json)
        {
            GameplayCommandService.OnUseItemResult -= HandleBagResult;
            HandleUseItemResponse(json, isBagItem: true, sourceSlot: slot, allowDirectFallback: true);
        }
        GameplayCommandService.OnUseItemResult -= HandleBagResult;
        GameplayCommandService.OnUseItemResult += HandleBagResult;
        GameplayCommandService.Instance.UseInventoryItemServerRpc(slot.slotIndex);
    }

    private bool CanUseGameplayCommandService()
    {
        return GameplayCommandService.Instance != null && GameplayCommandService.Instance.IsSpawned;
    }

    private void HandleUseItemResponse(
        string json,
        bool isBagItem,
        InventorySlotDto sourceSlot = null,
        bool allowDirectFallback = false)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[ItemUseHandler] ❌ UseItem trả về JSON rỗng.");
            return;
        }

        if (json.Contains("\"error\""))
        {
            string errorMessage = ExtractErrorMessage(json);
            Debug.LogError(isBagItem
                ? $"[ItemUseHandler] ❌ Mở túi thất bại: {json}"
                : $"[ItemUseHandler] ❌ UseItem thất bại: {json}");

            if (allowDirectFallback && sourceSlot != null && ShouldUseDirectFallback(errorMessage))
            {
                UseItemDirectFromApi(sourceSlot, isBagItem, errorMessage);
                return;
            }

            GlobalNotificationUI.Show(
                errorMessage,
                isBagItem ? "Tui Do" : "Vat Pham",
                3.5f,
                "OK");
            return;
        }

        var response = JsonUtility.FromJson<UseItemResult>(json);
        if (response == null)
        {
            Debug.LogError($"[ItemUseHandler] Parse UseItemResult null. Raw={json}");
            return;
        }

        if (isBagItem)
        {
            Debug.Log($"[ItemUseHandler] ✅ Mở túi OK: {response.message} | bag_slots={response.bag_slots}");
            if (response.bag_slots > 0)
            {
                currentBagSlots = response.bag_slots;
                UpdateBagSlotCountText();
            }

            if (response.bag_equipped_items != null)
                UpdateBagQuickSlots(response.bag_equipped_items);

            var playerData = GameManager.Instance?.GetPlayerData();
            if (playerData != null)
            {
                playerData.bag_slots = response.bag_slots > 0 ? response.bag_slots : playerData.bag_slots;
                if (response.bag_equipped_items != null)
                    playerData.bag_equipped_items = response.bag_equipped_items;
                GameManager.Instance.SetPlayerData(playerData);
            }

            RefreshInventory();
            return;
        }

        Debug.Log($"[ItemUseHandler] ✅ UseItem OK: {response.message}");

        if (response.wave_entry_bonus_added > 0)
        {
            GlobalNotificationUI.Show(
                $"Bạn nhận thêm {response.wave_entry_bonus_added} lượt Phó Bản Sóng cho hôm nay.",
                "Vé Phó Bản",
                3.5f,
                "OK");
        }

        if (response.hp_restore > 0 || response.mp_restore > 0)
            inventoryBridge?.RequestSyncHpMp(response.current_hp, response.current_mp);

        if (response.gene_exp > 0)
        {
            var pd = GameManager.Instance?.GetPlayerData();
            if (pd != null)
            {
                pd.gene_exp = response.gene_exp;
                GameManager.Instance.SetPlayerData(pd);
                // Cập nhật GeneUpgradePanel ngay nếu đang mở (không cần RPC round-trip)
                GeneUpgradePanel.Instance?.RefreshFromLocalCache();
            }
        }

        if (response.active_buffs != null && response.active_buffs.Length > 0)
        {
            ActiveBuffManager.Instance?.OnBuffsReceived(response.active_buffs);
            inventoryBridge?.RequestSyncBuffBonuses();
        }
        else if (response.new_buffs != null && response.new_buffs.Length > 0)
        {
            ActiveBuffManager.Instance?.OnBuffsAdded(response.new_buffs);
            inventoryBridge?.RequestSyncBuffBonuses();
        }

        if (response.new_buffs != null && response.new_buffs.Length > 0)
        {
            bool hasNewStatBuff = System.Array.Exists(response.new_buffs,
                b => b.effectType == "HpBuff" || b.effectType == "MpBuff");
            if (hasNewStatBuff) ReloadPlayerStats();
        }

        ActiveBuffManager.Instance?.LoadFromServer();
        RefreshInventory();
    }

    private void UseItemDirectFromApi(InventorySlotDto slot, bool isBagItem, string reason)
    {
        if (slot == null)
            return;

        int playerId = GetCurrentPlayerId();
        if (playerId <= 0)
        {
            string errorJson = BuildErrorJson("Khong xac dinh duoc playerId de dung vat pham.");
            HandleUseItemResponse(errorJson, isBagItem);
            return;
        }

        Debug.LogWarning($"[ItemUseHandler] UseItem direct REST fallback. slot={slot.slotIndex}, playerId={playerId}, reason={reason}");
        StartCoroutine(UseItemDirectFromApiCoroutine(playerId, slot.slotIndex, isBagItem));
    }

    private IEnumerator UseItemDirectFromApiCoroutine(int playerId, int slotIndex, bool isBagItem)
    {
        string url = $"{APIClient.BASE_URL}/api/player/{playerId}/inventory/use-item";
        int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1) == 2 ? 2 : 1;
        string body = $"{{\"slotIndex\":{slotIndex},\"geneSlot\":{geneSlot}}}";
        byte[] bytes = Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;
        req.SetRequestHeader("Content-Type", "application/json");
        AuthHelper.AddAuthHeader(req);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            HandleUseItemResponse(req.downloadHandler.text, isBagItem);
            inventoryBridge?.RefreshInventoryDirectFromAPI();
            yield break;
        }

        string error = !string.IsNullOrWhiteSpace(req.downloadHandler?.text)
            ? req.downloadHandler.text
            : $"HTTP {(long)req.responseCode}: {req.error}";
        HandleUseItemResponse(BuildErrorJson(error), isBagItem);
    }

    private static bool ShouldUseDirectFallback(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return true;

        string e = errorMessage.ToLowerInvariant();
        return e.Contains("http 0")
            || e.Contains("http 401")
            || e.Contains("unauthorized")
            || e.Contains("unavailable")
            || e.Contains("connection")
            || e.Contains("connect")
            || e.Contains("timeout")
            || e.Contains("network")
            || e.Contains("name resolution")
            || e.Contains("dns")
            || e.Contains("refused")
            || e.Contains("localhost");
    }

    private static string BuildErrorJson(string message)
    {
        return $"{{\"error\":\"{EscapeJson(message)}\"}}";
    }

    private static string EscapeJson(string value)
    {
        return (value ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private bool TryUseItemInBlacksmith(InventorySlotDto slot)
    {
        if (BlacksmithTabPanel.Instance == null || !BlacksmithTabPanel.Instance.gameObject.activeInHierarchy)
            return false;

        if (UpgradePanel.Instance == null)
            return false;

        bool handled = UpgradePanel.Instance.TryUseInventoryItemForUpgrade(slot);
        if (handled)
        {
            inventoryUI?.HideItemDetail();
            Debug.Log($"[ItemUseHandler] Redirected item use into Blacksmith flow: slot={slot.slotIndex}, item={slot.itemCode}");
            return true;
        }

        ItemTemplateDto template = ItemTemplateManager.Instance?.GetItemTemplate(slot.id);
        int itemType = template?.type ?? -1;
        bool isBlacksmithItem = slot.id == UpgradePanel.CHARM_ITEM_ID
            || itemType == UpgradePanel.STONE_ITEM_TYPE
            || (itemType >= 0 && itemType <= ItemTypeEquipMax);

        if (isBlacksmithItem)
        {
            inventoryUI?.HideItemDetail();
            return true;
        }

        return false;
    }

    // Sort

    // Gọi API sắp xếp inventory (gom item về phía trước, không để ô trống ở giữa).
    // Gắn vào OnClick của nút Sắp xếp.
    public void RequestSortInventory()
    {
        if (inventoryBridge == null)
        {
            Debug.LogWarning("[ItemUseHandler] RequestSortInventory: inventoryBridge null.");
            return;
        }

        Debug.Log("[ItemUseHandler] 🔀 Gửi request sắp xếp inventory...");
        if (sortButton != null) sortButton.interactable = false;

        // Delegate toàn bộ logic về bridge:
        // - Client  → host-mediated qua ServerRpc (host sort DB → push ClientRpc về client)
        // - Host/offline → sort trực tiếp qua API rồi fetch lại
        inventoryBridge.RequestSortAndRefresh();

        // Re-enable sau 1 giây để tránh spam (không cần chờ callback)
        StartCoroutine(ReenableSortButtonAfterDelay(1f));
    }

    private System.Collections.IEnumerator ReenableSortButtonAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sortButton != null) sortButton.interactable = true;
    }

    // UI Update

    // Gọi khi inventory được refresh để cập nhật Quick Slots và stat bar.
    // InventoryNetworkBridge gọi hàm này sau mỗi lần fetch từ DB thành công.
    public void OnInventoryRefreshed(InventorySlotDto[] slots, int bagSlots, int gold, int silver, BagEquippedItemData[] bagEquippedItems = null)
    {
        currentBagSlots = bagSlots;
        currentGold     = gold;
        currentSilver   = silver;

        inventoryUI?.SetVisibleSlotCount(currentBagSlots);
        UpdateStatBar();
        UpdateBagQuickSlots(bagEquippedItems);
    }

    // Cập nhật thanh vàng/bạc/ô túi từ GameManager (có thể gọi riêng lẻ).
    public void RefreshStatBar()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var data = GameManager.Instance.GetPlayerData();
            currentGold    = data.gold;
            currentSilver  = data.silver;
            currentBagSlots = data.bag_slots > 0 ? data.bag_slots : 20;
            inventoryUI?.SetVisibleSlotCount(currentBagSlots);
            UpdateBagQuickSlots(data.bag_equipped_items);
        }
        UpdateStatBar();
    }

    private void UpdateStatBar()
    {
        if (goldText   != null) goldText.text   = FormatNumber(currentGold);
        if (silverText != null) silverText.text = FormatNumber(currentSilver);
        UpdateBagSlotCountText();
    }

    private void UpdateBagSlotCountText()
    {
        if (bagSlotCountText == null) return;
        int usedSlots = inventoryUI?.CurrentSlots != null
            ? System.Array.FindAll(inventoryUI.CurrentSlots, s => s != null && s.quantity > 0).Length
            : 0;
        bagSlotCountText.text = $"{usedSlots}/{currentBagSlots}";
    }

    // Duyệt qua inventory, tìm tối đa 3 item túi đồ (type=30)
    // và hiển thị icon + số lượng vào 3 quick-slot.
    private void UpdateBagQuickSlots(BagEquippedItemData[] bagItems)
    {
        if (bagItems == null && GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            bagItems = GameManager.Instance.GetPlayerData().bag_equipped_items;

        _equippedBagItemsByQuickSlot.Clear();
        if (bagItems != null)
        {
            foreach (var bagItem in bagItems)
            {
                if (bagItem == null) continue;
                if (bagItem.quick_slot_index < 0) continue;
                _equippedBagItemsByQuickSlot[bagItem.quick_slot_index] = bagItem;
            }
        }

        int len = bagQuickSlotIcons?.Length ?? 0;
        for (int i = 0; i < len; i++)
        {
            if (_equippedBagItemsByQuickSlot.TryGetValue(i, out var bagItem) && bagItem != null)
            {
                if (bagQuickSlotIcons[i] != null)
                {
                    string iconId = !string.IsNullOrEmpty(bagItem.icon_id) && bagItem.icon_id != "0"
                        ? bagItem.icon_id
                        : ResolveBagItemIconId(bagItem.item_template_id);
                    Sprite icon = IconDatabase.Instance != null ? IconDatabase.Instance.GetIcon(iconId) : null;
                    bagQuickSlotIcons[i].sprite = icon ?? defaultBagIcon ?? emptySlotSprite;
                    bagQuickSlotIcons[i].enabled = true;
                }

                if (bagSlotItemBgs != null && i < bagSlotItemBgs.Length && bagSlotItemBgs[i] != null)
                    bagSlotItemBgs[i].enabled = true;

                if (bagQuickSlotCounts != null && i < bagQuickSlotCounts.Length && bagQuickSlotCounts[i] != null)
                    bagQuickSlotCounts[i].text = bagItem.upgrade_level > 0 ? $"+{bagItem.upgrade_level}" : "";
            }
            else
            {
                if (bagQuickSlotIcons[i] != null)
                {
                    bagQuickSlotIcons[i].sprite = emptySlotSprite;
                    bagQuickSlotIcons[i].enabled = emptySlotSprite != null;
                }

                if (bagSlotItemBgs != null && i < bagSlotItemBgs.Length && bagSlotItemBgs[i] != null)
                    bagSlotItemBgs[i].enabled = false;

                if (bagQuickSlotCounts != null && i < bagQuickSlotCounts.Length && bagQuickSlotCounts[i] != null)
                    bagQuickSlotCounts[i].text = "";
            }

            if (i < _bagQuickSlotButtons.Count && _bagQuickSlotButtons[i] != null)
                _bagQuickSlotButtons[i].interactable = _equippedBagItemsByQuickSlot.ContainsKey(i);
        }
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private void RequestUnequipBagQuickSlot(int quickSlotIndex)
    {
        if (inventoryBridge == null)
        {
            Debug.LogWarning("[ItemUseHandler] RequestUnequipBagQuickSlot: inventoryBridge null, using direct REST fallback.");
            StartCoroutine(UnequipBagDirectFromApiCoroutine(quickSlotIndex));
            return;
        }

        inventoryBridge.RequestUnequipBagItem(quickSlotIndex, json =>
        {
            if (!string.IsNullOrEmpty(json) && json.Contains("\"error\""))
            {
                string errorMessage = ExtractErrorMessage(json);
                if (ShouldUseDirectFallback(errorMessage))
                {
                    Debug.LogWarning($"[ItemUseHandler] Unequip bag direct REST fallback. quickSlot={quickSlotIndex}, reason={errorMessage}");
                    StartCoroutine(UnequipBagDirectFromApiCoroutine(quickSlotIndex));
                    return;
                }

                ShowUnequipBagError(errorMessage);
                return;
            }

            HandleUnequipBagSuccess(json);
        });
    }

    private void HandleUnequipBagSuccess(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        var response = JsonUtility.FromJson<UseItemResult>(json);
        if (response == null)
        {
            Debug.LogWarning($"[ItemUseHandler] Khong parse duoc UnequipBag response. Raw={json}");
            RefreshInventory();
            return;
        }

        if (response.bag_slots > 0)
        {
            currentBagSlots = response.bag_slots;
            inventoryUI?.SetVisibleSlotCount(currentBagSlots);
        }

        if (response.bag_equipped_items != null)
            UpdateBagQuickSlots(response.bag_equipped_items);

        var playerData = GameManager.Instance?.GetPlayerData();
        if (playerData != null)
        {
            if (response.bag_slots > 0)
                playerData.bag_slots = response.bag_slots;
            if (response.bag_equipped_items != null)
                playerData.bag_equipped_items = response.bag_equipped_items;
            GameManager.Instance.SetPlayerData(playerData);
        }

        UpdateBagSlotCountText();
        RefreshInventory();
    }

    private IEnumerator UnequipBagDirectFromApiCoroutine(int quickSlotIndex)
    {
        int playerId = GetCurrentPlayerId();
        if (playerId <= 0)
        {
            ShowUnequipBagError("Khong xac dinh duoc playerId de thao tui.");
            yield break;
        }

        string url = $"{APIClient.BASE_URL}/api/player/{playerId}/bag/unequip";
        string body = $"{{\"quickSlotIndex\":{quickSlotIndex}}}";
        byte[] bytes = Encoding.UTF8.GetBytes(body);

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;
        req.SetRequestHeader("Content-Type", "application/json");
        AuthHelper.AddAuthHeader(req);

        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            HandleUnequipBagSuccess(req.downloadHandler.text);
            inventoryBridge?.InvalidateInventoryCache();
            inventoryBridge?.RefreshInventoryDirectFromAPI();
            yield break;
        }

        string error = !string.IsNullOrWhiteSpace(req.downloadHandler?.text)
            ? req.downloadHandler.text
            : $"HTTP {(long)req.responseCode}: {req.error}";
        ShowUnequipBagError(ExtractErrorMessage(BuildErrorJson(error)));
    }

    private static void ShowUnequipBagError(string errorMessage)
    {
        GlobalNotificationUI.Show(
            string.IsNullOrWhiteSpace(errorMessage)
                ? "Khong the thao tui mo rong. Hay kiem tra lai cho trong trong tui."
                : errorMessage,
            "Tui Do",
            3f,
            "OK");
    }

    private void SyncBagQuickSlotsFromPlayerDataIfNeeded()
    {
        if (_equippedBagItemsByQuickSlot.Count > 0)
            return;

        var playerData = GameManager.Instance != null && GameManager.Instance.HasPlayerData()
            ? GameManager.Instance.GetPlayerData()
            : null;

        if (playerData?.bag_equipped_items != null && playerData.bag_equipped_items.Length > 0)
            UpdateBagQuickSlots(playerData.bag_equipped_items);
    }

    private static string ResolveBagItemIconId(int itemTemplateId)
    {
        if (itemTemplateId <= 0)
            return null;

        ItemTemplateDto template = ItemTemplateManager.Instance?.GetItemTemplate(itemTemplateId);
        return template != null && template.idIcon > 0 ? template.idIcon.ToString() : null;
    }

    private static string BuildBagItemDisplayName(BagEquippedItemData bagItem)
    {
        if (bagItem == null)
            return "Tui mo rong";

        string baseName = !string.IsNullOrEmpty(bagItem.item_name)
            ? bagItem.item_name
            : ItemTemplateManager.Instance?.GetItemTemplate(bagItem.item_template_id)?.name ?? "Tui mo rong";

        return bagItem.upgrade_level > 0 ? $"{baseName} +{bagItem.upgrade_level}" : baseName;
    }

    private static string ExtractErrorMessage(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return "Co loi xay ra. Vui long thu lai.";

        try
        {
            var error = JsonUtility.FromJson<ErrorResponse>(json);
            if (!string.IsNullOrWhiteSpace(error?.error))
                return error.error;
        }
        catch
        {
            // Fall back to raw string below.
        }

        return json;
    }

    private static InventorySlotDto ConvertBagItemToSlotDto(BagEquippedItemData bagItem)
    {
        if (bagItem == null)
            return null;

        return new InventorySlotDto
        {
            slotIndex = -1,
            itemTemplateId = bagItem.item_template_id,
            itemCode = bagItem.item_code,
            iconId = !string.IsNullOrEmpty(bagItem.icon_id) && bagItem.icon_id != "0"
                ? bagItem.icon_id
                : ResolveBagItemIconId(bagItem.item_template_id),
            quantity = 1,
            isLocked = bagItem.is_locked,
            upgradeLevel = bagItem.upgrade_level,
            strOptions = bagItem.str_options
        };
    }

    private void RefreshInventory()
    {
        // Invalidate cache trước mọi lần refresh sau khi dùng item/sắp xếp
        // để đảm bảo dữ liệu luôn được lấy mới từ DB
        inventoryBridge?.InvalidateInventoryCache();
        inventoryBridge?.RefreshInventoryFromDB();
    }

    // Reload toàn bộ player data qua GameplayCommandService bao gồm final_stats (có HpBuff/MpBuff).
    private void ReloadPlayerStats()
    {
        if (!CanUseGameplayCommandService()) return;

        void HandleStats(string json)
        {
            GameplayCommandService.OnPlayerDataReceived -= HandleStats;
            var data = JsonUtility.FromJson<PlayerDataResponse>(json);
            if (data == null) return;
            GameManager.Instance?.SetPlayerData(data);
            if (data.final_stats != null && inventoryBridge != null)
                inventoryBridge.RequestUpdatePlayerStats(data.final_stats.max_hp, data.final_stats.max_mp);
        }
        GameplayCommandService.OnPlayerDataReceived -= HandleStats;
        GameplayCommandService.OnPlayerDataReceived += HandleStats;
        GameplayCommandService.Instance.RequestPlayerDataServerRpc();
    }

    // Gọi từ bên ngoài (ví dụ NpcMenuUI sau khi mua item) để refresh túi đồ.
    public void RequestRefreshInventory()
    {
        // Invalidate cache trước để đảm bảo fetch lại dữ liệu mới nhất
        inventoryBridge?.InvalidateInventoryCache();
        RefreshInventory();
    }

    private bool TryGetCurrentVitals(out int currentHp, out int maxHp, out int currentMp, out int maxMp)
    {
        currentHp = 0;
        maxHp = 0;
        currentMp = 0;
        maxMp = 0;

        var syncs = FindObjectsOfType<NetworkPlayerDataSync>();
        foreach (var sync in syncs)
        {
            if (sync == null || !sync.IsOwner) continue;

            currentHp = sync.networkHp.Value;
            maxHp = sync.networkMaxHp.Value;
            currentMp = sync.networkMp.Value;
            maxMp = sync.networkMaxMp.Value;
            return true;
        }

        var pd = GameManager.Instance?.GetPlayerData();
        if (pd?.final_stats != null)
        {
            currentHp = pd.final_stats.hp;
            maxHp = pd.final_stats.max_hp;
            currentMp = pd.final_stats.mp;
            maxMp = pd.final_stats.max_mp;
            return true;
        }

        return false;
    }

    private int GetCurrentPlayerId()
    {
        // Ưu tiên: dùng inventoryBridge vì nó có logic resolve playerId đầy đủ nhất
        if (inventoryBridge != null)
        {
            int bridgeId = inventoryBridge.GetCurrentPlayerId();
            if (bridgeId != 0) return bridgeId;
        }

        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            return GameManager.Instance.GetPlayerData().user_id;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var serverDataMgr = ServerPlayerDataManager.Instance;
            if (serverDataMgr != null)
            {
                var pd = serverDataMgr.GetPlayerDataForClient(NetworkManager.Singleton.LocalClientId);
                if (pd != null) return pd.user_id;
            }
        }

        return PlayerPrefs.GetInt("USER_ID", 0);
    }

    private static string FormatNumber(int value)
    {
        if (value >= 1_000_000) return (value / 1_000_000f).ToString("0.#") + "M";
        if (value >= 1_000)     return (value / 1_000f).ToString("0.#")     + "K";
        return value.ToString();
    }
}

[System.Serializable]
public class UseItemResult
{
    public string message;
    public int item_template_id;
    public int wave_entry_bonus_added;
    public int hp_restore;
    public int mp_restore;
    public int current_hp;
    public int current_mp;
    public int gene_exp;
    public int bag_slots;
    public BagEquippedItemData[] bag_equipped_items;
    public ActiveBuffDto[] active_buffs;
    public ActiveBuffDto[] new_buffs;
}

[System.Serializable]
public class ErrorResponse
{
    public string error;
}
