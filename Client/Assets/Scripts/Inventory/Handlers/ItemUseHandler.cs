using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// ItemUseHandler — Xử lý toàn bộ logic sử dụng item trong túi đồ.
///
/// Trách nhiệm:
///   1. Nhận sự kiện "Sử dụng" từ ItemDetailPanel.
///   2. Phân loại item (equipment / consumable / bag expansion) và gọi API tương ứng.
///   3. Quản lý 3 Quick-Slot hiển thị icon item mở rộng túi đồ.
///   4. Xử lý nút Sắp xếp (compact inventory qua API).
///   5. Hiển thị số túi đang có, vàng, bạc.
///
/// Gắn script này vào GameObject duy nhất trong scene (ví dụ: "InventoryManager").
/// </summary>
public class ItemUseHandler : MonoBehaviour
{
    // ── Singleton ──────────────────────────────────────────────────────────
    public static ItemUseHandler Instance { get; private set; }

    // ── Loại item ──────────────────────────────────────────────────────────
    /// <summary>type = 30 trong item_template → item mở rộng túi đồ (+5 ô).</summary>
    public const int ItemTypeBag        = 30;
    /// <summary>Số ô mở rộng mỗi lần dùng item túi.</summary>
    public const int BagExpandAmount    = 5;
    /// <summary>type 21-29 → item tiêu thụ (phục hồi HP/MP, v.v.).</summary>
    public const int ItemTypeConsumableMin = 21;
    public const int ItemTypeConsumableMax = 29;
    /// <summary>type 0-5 → trang bị.</summary>
    public const int ItemTypeEquipMax   = 5;

    // ── Inspector References ────────────────────────────────────────────────
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

    [Tooltip("Sprite hiển thị khi slot nhanh trống")]
    [SerializeField] private Sprite emptySlotSprite;

    [Header("UI Elements")]
    [Tooltip("Image/GameObject hiển thị khi item đang khóa")]
    [SerializeField] private Sprite lockIcon;

    [Tooltip("Nút sắp xếp túi đồ")]
    [SerializeField] private Button sortButton;

    // ── Private state ──────────────────────────────────────────────────────
    private int currentBagSlots = 20;
    private int currentGold;
    private int currentSilver;

    /// <summary>Slot data của các item túi tìm thấy trong inventory (tối đa 3).</summary>
    private readonly List<InventorySlotDto> _bagItemSlots = new List<InventorySlotDto>(3);

    // ── Unity Lifecycle ────────────────────────────────────────────────────
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

        RefreshStatBar();
    }

    private void OnDestroy()
    {
        if (sortButton != null)
            sortButton.onClick.RemoveListener(RequestSortInventory);

        if (Instance == this) Instance = null;
    }

    // ── Public API: gọi từ ItemDetailPanel ────────────────────────────────

    /// <summary>
    /// Entry point khi người chơi nhấn nút "Sử dụng" trên ItemDetailPanel.
    /// Tự động phân loại item và gọi handler tương ứng.
    /// </summary>
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

    // ── Item Use Handlers ─────────────────────────────────────────────────

    /// <summary>Trang bị item (equipment type 0-5) qua bridge.</summary>
    private void DoEquipItem(InventorySlotDto slot, ItemTemplateDto template)
    {
        Debug.Log($"[ItemUseHandler] ⚔️ Trang bị item: slot={slot.slotIndex}, code={slot.itemCode}");
        inventoryBridge?.RequestEquipItem(slot.slotIndex, slot.itemCode);
    }

    /// <summary>Sử dụng item tiêu thụ (type 21-29): gọi API → áp dụng HP/MP qua NGO → cập nhật buff HUD.</summary>
    private void DoUseConsumableItem(InventorySlotDto slot)
    {
        Debug.Log($"[ItemUseHandler] 🍶 Sử dụng consumable: slot={slot.slotIndex}");
        int playerId = GetCurrentPlayerId();
        if (playerId == 0 || APIClient.Instance == null) return;

        if (TryGetCurrentVitals(out int currentHp, out int maxHp, out int currentMp, out int maxMp))
        {
            string vitalsJson =
                $"{{\"hp\":{currentHp},\"max_hp\":{maxHp},\"mp\":{currentMp},\"max_mp\":{maxMp}}}";

            APIClient.Instance.UpdatePlayerData(
                playerId,
                vitalsJson,
                onSuccess: () => SendUseConsumableRequest(slot, playerId),
                onError: error =>
                {
                    Debug.LogWarning($"[ItemUseHandler] Sync vitals trước khi dùng item thất bại: {error}. Tiếp tục use-item.");
                    SendUseConsumableRequest(slot, playerId);
                }
            );
            return;
        }

        SendUseConsumableRequest(slot, playerId);
    }

    private void SendUseConsumableRequest(InventorySlotDto slot, int playerId)
    {
        int templateId = slot.itemTemplateId;

        APIClient.Instance.UseInventoryItem(
            playerId, slot.slotIndex,
            response =>
            {
                Debug.Log($"[ItemUseHandler] ✅ UseItem OK: {response.message}");

                // Hồi HP/MP: dùng giá trị authoritative từ REST API để sync ngược lên NGO
                // (thay thế ApplyStatEffect cũ để tránh lấy value từ ScriptableObject sai)
                if (response.hp_restore > 0 || response.mp_restore > 0)
                    inventoryBridge?.RequestSyncHpMp(response.current_hp, response.current_mp);

                // Cập nhật gene_exp của player nếu có GeneExpAdd
                if (response.gene_exp > 0)
                {
                    var pd = GameManager.Instance?.GetPlayerData();
                    if (pd != null)
                    {
                        pd.gene_exp = response.gene_exp;
                        GameManager.Instance.SetPlayerData(pd);
                    }
                }

                // Cập nhật buff HUD với danh sách buff mới từ server
                if (response.active_buffs != null && response.active_buffs.Length > 0)
                {
                    ActiveBuffManager.Instance?.OnBuffsReceived(response.active_buffs);
                    inventoryBridge?.RequestSyncBuffBonuses(); // sync % bonus lên NGO
                }
                else if (response.new_buffs != null && response.new_buffs.Length > 0)
                {
                    ActiveBuffManager.Instance?.OnBuffsAdded(response.new_buffs);
                    inventoryBridge?.RequestSyncBuffBonuses();
                }

                // Chỉ reload stats khi LẦN NÀY có thêm buff mới ảnh hưởng max HP/MP
                // (kiểm tra new_buffs thay vì active_buffs để tránh reload sai khi buff cũ còn active)
                if (response.new_buffs != null && response.new_buffs.Length > 0)
                {
                    bool hasNewStatBuff = System.Array.Exists(response.new_buffs,
                        b => b.effectType == "HpBuff" || b.effectType == "MpBuff");
                    if (hasNewStatBuff)
                        ReloadPlayerStats();
                }

                // Chốt lại HUD theo dữ liệu authoritative từ server.
                ActiveBuffManager.Instance?.LoadFromServer();

                RefreshInventory();
            },
            error => Debug.LogError($"[ItemUseHandler] ❌ UseItem thất bại: {error}")
        );
    }

    /// <summary>Sử dụng item mở rộng túi (type 30): gọi API use-item + cập nhật bag count.</summary>
    private void DoUseBagItem(InventorySlotDto slot)
    {
        Debug.Log($"[ItemUseHandler] 🎒 Mở rộng túi đồ: slot={slot.slotIndex}");
        int playerId = GetCurrentPlayerId();
        if (playerId == 0 || APIClient.Instance == null) return;

        APIClient.Instance.UseInventoryItem(
            playerId, slot.slotIndex,
            response =>
            {
                Debug.Log($"[ItemUseHandler] ✅ Mở túi OK: {response.message} | bag_slots={response.bag_slots}");
                currentBagSlots = response.bag_slots;
                UpdateBagSlotCountText();
                RefreshInventory();
            },
            error => Debug.LogError($"[ItemUseHandler] ❌ Mở túi thất bại: {error}")
        );
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

    // ── Sort ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi API sắp xếp inventory (gom item về phía trước, không để ô trống ở giữa).
    /// Gắn vào OnClick của nút Sắp xếp.
    /// </summary>
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

    // ── UI Update ─────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi khi inventory được refresh để cập nhật Quick Slots và stat bar.
    /// InventoryNetworkBridge gọi hàm này sau mỗi lần fetch từ DB thành công.
    /// </summary>
    public void OnInventoryRefreshed(InventorySlotDto[] slots, int bagSlots, int gold, int silver)
    {
        currentBagSlots = bagSlots;
        currentGold     = gold;
        currentSilver   = silver;

        UpdateStatBar();
        UpdateBagQuickSlots(slots);
    }

    /// <summary>Cập nhật thanh vàng/bạc/ô túi từ GameManager (có thể gọi riêng lẻ).</summary>
    public void RefreshStatBar()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var data = GameManager.Instance.GetPlayerData();
            currentGold    = data.gold;
            currentSilver  = data.silver;
            currentBagSlots = data.bag_slots > 0 ? data.bag_slots : 20;
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

    /// <summary>
    /// Duyệt qua inventory, tìm tối đa 3 item túi đồ (type=30)
    /// và hiển thị icon + số lượng vào 3 quick-slot.
    /// </summary>
    private void UpdateBagQuickSlots(InventorySlotDto[] slots)
    {
        _bagItemSlots.Clear();

        if (slots != null)
        {
            foreach (var slot in slots)
            {
                if (slot == null || slot.quantity <= 0) continue;

                ItemTemplateDto tpl = null;
                if (ItemTemplateManager.Instance != null)
                {
                    if (slot.itemTemplateId > 0)
                        tpl = ItemTemplateManager.Instance.GetItemTemplate(slot.itemTemplateId);
                    if (tpl == null && !string.IsNullOrEmpty(slot.itemCode))
                        tpl = ItemTemplateManager.Instance.GetItemTemplateByCode(slot.itemCode);
                }

                if (tpl != null && tpl.type == ItemTypeBag)
                {
                    _bagItemSlots.Add(slot);
                    if (_bagItemSlots.Count >= 3) break;
                }
            }
        }

        // Áp vào 3 quick-slot UI
        int len = bagQuickSlotIcons?.Length ?? 0;
        for (int i = 0; i < len; i++)
        {
            if (i < _bagItemSlots.Count)
            {
                var s = _bagItemSlots[i];
                if (bagQuickSlotIcons[i] != null)
                {
                    Sprite icon = IconDatabase.Instance != null ? IconDatabase.Instance.GetIcon(s.iconId) : null;
                    bagQuickSlotIcons[i].sprite  = icon ?? emptySlotSprite;
                    bagQuickSlotIcons[i].enabled = true;
                }
                if (bagQuickSlotCounts != null && i < bagQuickSlotCounts.Length && bagQuickSlotCounts[i] != null)
                    bagQuickSlotCounts[i].text = s.quantity > 1 ? s.quantity.ToString() : "";
            }
            else
            {
                if (bagQuickSlotIcons[i] != null)
                {
                    bagQuickSlotIcons[i].sprite  = emptySlotSprite;
                    bagQuickSlotIcons[i].enabled = emptySlotSprite != null;
                }
                if (bagQuickSlotCounts != null && i < bagQuickSlotCounts.Length && bagQuickSlotCounts[i] != null)
                    bagQuickSlotCounts[i].text = "";
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void RefreshInventory()
    {
        // Invalidate cache trước mọi lần refresh sau khi dùng item/sắp xếp
        // để đảm bảo dữ liệu luôn được lấy mới từ DB
        inventoryBridge?.InvalidateInventoryCache();
        inventoryBridge?.RefreshInventoryFromDB();
    }

    /// <summary>Reload toàn bộ player data từ REST API bao gồm final_stats (có HpBuff/MpBuff).</summary>
    private void ReloadPlayerStats()
    {
        int playerId = GetCurrentPlayerId();
        if (playerId <= 0 || APIClient.Instance == null) return;
        APIClient.Instance.LoadPlayerData(playerId,
            data =>
            {
                GameManager.Instance?.SetPlayerData(data);
                // Sync maxHp/maxMp lên NGO nếu có InventoryNetworkBridge
                if (data?.final_stats != null && inventoryBridge != null)
                    inventoryBridge.RequestUpdatePlayerStats(data.final_stats.max_hp, data.final_stats.max_mp);
            },
            _ => { });
    }

    /// <summary>Gọi từ bên ngoài (ví dụ NpcMenuUI sau khi mua item) để refresh túi đồ.</summary>
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
