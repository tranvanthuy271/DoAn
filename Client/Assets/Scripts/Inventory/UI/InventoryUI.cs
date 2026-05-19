using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// InventoryUI - Quản lý panel túi đồ và các ô item UI
/// - Gắn lên GameObject Panel Inventory trong Canvas
/// - Dùng dữ liệu InventorySlotDto (nhận từ server, qua network code riêng)
/// - Không phụ thuộc trực tiếp vào NetworkInventory / ItemData
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Panel gốc của Inventory (bật/tắt khi mở túi)")]
    [SerializeField] private GameObject inventoryRoot;

    [Tooltip("Transform chứa các ô slot (Grid Layout Group)")]
    [SerializeField] private Transform slotContainer;

    [Tooltip("Prefab của 1 ô slot (có InventorySlotUI)")]
    [SerializeField] private InventorySlotUI slotPrefab;

    [Header("Item Detail")]
    [Tooltip("Prefab của ItemDetailPanel (sẽ được định vị dưới parent khi cần)")]
    [SerializeField] private ItemDetailPanel itemDetailPanelPrefab;

    [Tooltip("Parent để instantiate ItemDetailPanel vào (nên là root Canvas). Để trống sẽ dùng transform.root.")]
    [SerializeField] private Transform itemDetailPanelParent;

    // instance được tạo runtime, tái sử dụng sau đó
    private ItemDetailPanel _itemDetailPanelInstance;

    [Header("Settings")]
    [Tooltip("Kích thước POOL slot UI. Đặt bằng tổng slot tối đa có thể đạt được: 20 (base) + số túi mở rộng × 5.\nVí dụ 3 túi × 5 = 15 → đặt 35. Số slot HIỂN THỊ thực sự do bag_slots của player quyết định (gọi SetVisibleSlotCount).")]
    [SerializeField] private int maxSlotCount = 35;

    private InventorySlotUI[] slotUIs;
    private InventorySlotDto[] currentSlots;
    private readonly Dictionary<int, int> _reservedQuantities = new Dictionary<int, int>();
    private int currentVisibleSlotCount = 20;
    private bool _openingInventoryRoot;

    /// <summary>Snapshot túi đồ hiện tại (dùng cho UpgradePanel)</summary>
    public InventorySlotDto[] CurrentSlots => currentSlots;
    public int GetConfiguredMaxSlotCount() => maxSlotCount > 0 ? maxSlotCount : 20;

    public void SetVisibleSlotCount(int slotCount)
    {
        currentVisibleSlotCount = Mathf.Clamp(slotCount > 0 ? slotCount : 20, 0, GetConfiguredMaxSlotCount());

        if (slotUIs == null)
            return;

        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] != null)
                slotUIs[i].gameObject.SetActive(i < currentVisibleSlotCount);
        }
    }

    /// <summary>
    /// Đọc bag_slots từ GameManager và gọi SetVisibleSlotCount.
    /// Gọi mỗi khi mở túi đồ để đồng bộ với dữ liệu player hiện tại.
    /// </summary>
    public void SyncVisibleSlotCountFromPlayerData()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            int bagSlots = GameManager.Instance.GetPlayerData().bag_slots;
            if (bagSlots > 0)
            {
                SetVisibleSlotCount(bagSlots);
                return;
            }
        }
        SetVisibleSlotCount(currentVisibleSlotCount > 0 ? currentVisibleSlotCount : 20);
    }

    private void Awake()
    {
        ResolveInventoryRoot();
        UIPanelManager.Register(gameObject, HideInventory);

        // Đảm bảo ban đầu Inventory đóng
        if (inventoryRoot != null && !_openingInventoryRoot)
        {
            inventoryRoot.SetActive(false);
        }
    }

    private void Start()
    {
        // Chỉ khởi tạo nếu chưa được khởi tạo bởi bridge (NetworkBridge có thể đã gọi InitSlots trước khi gameobject active)
        if (slotUIs == null || slotUIs.Length == 0)
        {
            InitSlots();
            SetVisibleSlotCount(ResolveInitialVisibleSlotCount());
        }
    }

    /// <summary>
    /// Khởi tạo các ô slot UI
    /// </summary>
    private void InitSlots()
    {
        if (slotContainer == null || slotPrefab == null)
        {
            Debug.LogError("[InventoryUI] InitSlots: Chưa gán SlotContainer hoặc SlotPrefab trong Inspector.");
            return;
        }

        Debug.Log($"[InventoryUI] InitSlots: Bắt đầu khởi tạo slots... (maxSlotCount = {maxSlotCount})");

        // Xoá con cũ (nếu có) - dùng DestroyImmediate để xóa ngay lập tức
        int oldChildCount = slotContainer.childCount;
        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(slotContainer.GetChild(i).gameObject);
        }
        if (oldChildCount > 0)
        {
            Debug.Log($"[InventoryUI] InitSlots: Đã xóa {oldChildCount} slot cũ.");
        }

        if (maxSlotCount <= 0)
        {
            maxSlotCount = 20;
            Debug.LogWarning($"[InventoryUI] InitSlots: maxSlotCount <= 0, đặt lại = {maxSlotCount}");
        }

        slotUIs = new InventorySlotUI[maxSlotCount];

        for (int i = 0; i < maxSlotCount; i++)
        {
            InventorySlotUI slot = Instantiate(slotPrefab, slotContainer);
            slot.Init(i);
            slotUIs[i] = slot;

            // Subscribe sự kiện click slot để hiển thị panel chi tiết
            slot.OnSlotClicked += OnSlotItemClicked;
        }

        Debug.Log($"[InventoryUI] InitSlots: Đã tạo thành công {maxSlotCount} slots!");

        // Ẩn ngay các slot vượt quá currentVisibleSlotCount để không lộ pool khi inventory mở
        SetVisibleSlotCount(currentVisibleSlotCount);
    }

    /// <summary>Mở inventory và refresh data từ server.</summary>
    public void ShowInventory()
    {
        ResolveInventoryRoot();
        if (inventoryRoot == null) return;
        SetInventoryRootActive(true);

        if (slotUIs == null || slotUIs.Length == 0) InitSlots();

        var bridge = InventoryNetworkBridge.GetExisting(true);
        if (bridge != null)
        {
            Debug.Log("[InventoryUI] ShowInventory: Tìm thấy bridge, gọi RefreshInventoryFromDB()...");
            bridge.RefreshInventoryFromDB();
            bridge.RefreshEquipmentFromDB();
        }
        else
        {
            Debug.LogWarning("[InventoryUI] ShowInventory: KHÔNG tìm thấy InventoryNetworkBridge trong scene!");
        }
        RefreshAllSlots();
    }

    /// <summary>Đóng inventory và ẩn panel chi tiết.</summary>
    public void HideInventory()
    {
        ResolveInventoryRoot();
        if (inventoryRoot == null) return;
        SetInventoryRootActive(false);
        HideItemDetail();
    }

    /// <summary>
    /// Gọi để bật/tắt panel inventory (dùng cho Button OnClick)
    /// </summary>
    public void ToggleInventory()
    {
        ResolveInventoryRoot();
        if (inventoryRoot == null)
        {
            Debug.LogWarning("[InventoryUI] ToggleInventory: inventoryRoot is null!");
            return;
        }

        bool isActive = !inventoryRoot.activeSelf;
        SetInventoryRootActive(isActive);
        
        Debug.Log($"[InventoryUI] ToggleInventory: Panel {(isActive ? "MỞ" : "ĐÓNG")}");

        if (isActive)
        {
            // Đảm bảo slots đã được khởi tạo
            if (slotUIs == null || slotUIs.Length == 0)
            {
                Debug.LogWarning("[InventoryUI] ToggleInventory: slotUIs chưa init, gọi InitSlots() ngay bây giờ...");
                InitSlots();
            }

            // Đồng bộ số slot hiển thị từ player data ngay khi mở túi
            SyncVisibleSlotCountFromPlayerData();

            Debug.Log($"[InventoryUI] ToggleInventory: Đang refresh {slotUIs?.Length ?? 0} slots...");
            
            // ✅ REFRESH INVENTORY FROM DB KHI MỞ UI
            var bridge = InventoryNetworkBridge.GetExisting(true);
            if (bridge != null)
            {
                Debug.Log("[InventoryUI] ✓ Tìm thấy InventoryNetworkBridge, gọi RefreshInventoryFromDB() + RefreshEquipmentFromDB()...");
                bridge.RefreshInventoryFromDB();
                bridge.RefreshEquipmentFromDB();
            }
            else
            {
                Debug.LogWarning("[InventoryUI] ⚠️ KHÔNG tìm thấy InventoryNetworkBridge trong scene!");
            }
            
            // Vẫn gọi RefreshAllSlots để update UI ngay lập tức với data hiện tại
            RefreshAllSlots();
        }
        else
        {
            // Ẩn panel chi tiết item khi đóng inventory
            HideItemDetail();
        }
    }

    /// <summary>
    /// Gán dữ liệu inventory mới (parse từ JSON server gửi về)
    /// Network layer nên gọi hàm này mỗi khi nhận snapshot/update.
    /// </summary>
    public void SetInventoryData(InventorySlotDto[] slots)
    {
        currentSlots = slots;
        
        if (slots == null)
        {
            Debug.LogWarning("[InventoryUI] SetInventoryData: slots is null!");
        }
        else
        {
            int itemCount = 0;
            foreach (var slot in slots)
            {
                if (slot != null && slot.quantity > 0)
                {
                    itemCount++;
                }
            }
            Debug.Log($"[InventoryUI] SetInventoryData: Nhận {slots.Length} slots, trong đó có {itemCount} slots có item (quantity > 0)");
        }
        
        RefreshAllSlots();
    }

    /// <summary>
    /// Refresh toàn bộ ô slot dựa trên dữ liệu trong NetworkInventory
    /// </summary>
    public void RefreshAllSlots()
    {
        if (slotUIs == null || slotUIs.Length == 0)
        {
            InitSlots();
            if (slotUIs == null || slotUIs.Length == 0)
            {
                Debug.LogWarning("[InventoryUI] RefreshAllSlots: slotUIs chưa sẵn sàng sau InitSlots().");
                return;
            }
        }

        Debug.Log($"[InventoryUI] RefreshAllSlots: Bắt đầu refresh {slotUIs.Length} slots...");
        Debug.Log($"[InventoryUI] RefreshAllSlots: currentSlots = {(currentSlots == null ? "null" : $"{currentSlots.Length} items")}");

        SetVisibleSlotCount(currentVisibleSlotCount);

        int slotsWithItems = 0;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            InventorySlotDto slotData = null;

            if (currentSlots != null)
            {
                // Tìm slot theo index i
                for (int j = 0; j < currentSlots.Length; j++)
                {
                    if (currentSlots[j].slotIndex == i)
                    {
                        slotData = currentSlots[j];
                        break;
                    }
                }
            }

            if (slotData != null && slotData.quantity > 0)
            {
                if (_reservedQuantities.TryGetValue(i, out int reservedQuantity) && reservedQuantity > 0)
                {
                    InventorySlotDto displaySlot = CloneSlot(slotData);
                    displaySlot.quantity = Mathf.Max(0, slotData.quantity - reservedQuantity);
                    slotData = displaySlot;
                }

                slotsWithItems++;
                Debug.Log($"[InventoryUI] RefreshAllSlots: Slot {i} có item - code={slotData.itemCode}, iconId={slotData.iconId}, qty={slotData.quantity}");
            }

            slotUIs[i].SetSlot(slotData);
        }

        ApplySelectModeToSlots();

        Debug.Log($"[InventoryUI] RefreshAllSlots: Hoàn thành! Có {slotsWithItems} slots có item được hiển thị.");
    }

    public void SetReservedQuantities(Dictionary<int, int> reservedQuantities)
    {
        _reservedQuantities.Clear();
        if (reservedQuantities != null)
        {
            foreach (var entry in reservedQuantities)
            {
                if (entry.Value > 0)
                    _reservedQuantities[entry.Key] = entry.Value;
            }
        }
        RefreshAllSlots();
    }

    private int ResolveInitialVisibleSlotCount()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            int bagSlots = GameManager.Instance.GetPlayerData().bag_slots;
            if (bagSlots > 0)
                return bagSlots;
        }

        return 20;
    }

    private void ResolveInventoryRoot()
    {
        if (inventoryRoot == null)
            inventoryRoot = gameObject;
    }

    private void SetInventoryRootActive(bool active)
    {
        ResolveInventoryRoot();
        if (inventoryRoot == null) return;

        if (!active)
        {
            inventoryRoot.SetActive(false);
            UIPanelManager.NotifyClosed(gameObject);
            return;
        }

        UIPanelManager.CloseOthers(gameObject);
        bool previousOpeningState = _openingInventoryRoot;
        _openingInventoryRoot = true;
        inventoryRoot.SetActive(true);
        _openingInventoryRoot = previousOpeningState;
        UIPanelManager.NotifyOpened(gameObject);
    }

    private static InventorySlotDto CloneSlot(InventorySlotDto slot)
    {
        if (slot == null) return null;
        return new InventorySlotDto
        {
            slotIndex = slot.slotIndex,
            id = slot.id,
            amount = slot.amount,
            isEquipped = slot.isEquipped,
            isLocked = slot.isLocked,
            upgradeLevel = slot.upgradeLevel,
            strOptions = slot.strOptions,
            itemCode = slot.itemCode,
            iconId = slot.iconId
        };
    }

    /// <summary>
    /// Lấy instance hiện tại hoặc instantiate mới từ prefab.
    /// </summary>
    private ItemDetailPanel GetOrCreateDetailPanel()
    {
        if (_itemDetailPanelInstance != null) return _itemDetailPanelInstance;

        if (itemDetailPanelPrefab == null)
        {
            Debug.LogError("[InventoryUI] itemDetailPanelPrefab chưa được gán trong Inspector!");
            return null;
        }

        // Ưu tiên parent được chỉ định; nếu không, tìm root Canvas Screen Space (không phải World Space)
        Transform parent = itemDetailPanelParent;
        if (parent == null)
        {
            Canvas best = null;
            int bestOrder = int.MinValue;
            // includeInactive=true: tránh bỏ qua Canvas đang bị tắt (ví dụ inventory panel đóng)
            foreach (var c in FindObjectsOfType<Canvas>(true))
            {
                if (!c.isRootCanvas) continue;
                // Bỏ qua World Space canvas (ví dụ PlayerHpBarCanvas trên đầu player)
                if (c.renderMode == RenderMode.WorldSpace) continue;
                if (c.sortingOrder > bestOrder)
                {
                    bestOrder = c.sortingOrder;
                    best = c;
                }
            }

            if (best != null)
            {
                parent = best.transform;
            }
            else
            {
                // Fallback theo tên cố định trước khi dùng transform.root
                var namedCanvas = GameObject.Find("ScreenSpaceCanvas")
                               ?? GameObject.Find("InformationCanvas")
                               ?? GameObject.Find("Canvas");
                if (namedCanvas != null)
                {
                    parent = namedCanvas.transform;
                    Debug.LogWarning($"[InventoryUI] Không tìm được Screen Space Canvas qua loop, dùng fallback theo tên '{namedCanvas.name}'");
                }
                else
                {
                    parent = transform.root;
                    Debug.LogWarning($"[InventoryUI] Không tìm được bất kỳ Screen Space Canvas nào — ItemDetailPanel sẽ được tạo dưới '{parent.name}'. Hãy gán 'itemDetailPanelParent' trong Inspector!");
                }
            }
        }

        _itemDetailPanelInstance = Instantiate(itemDetailPanelPrefab, parent);
        Debug.Log($"[InventoryUI] Đã instantiate ItemDetailPanel prefab dưới '{parent.name}'");
        return _itemDetailPanelInstance;
    }

    public ItemDetailPanel GetSharedItemDetailPanel()
    {
        return GetOrCreateDetailPanel();
    }

    public void ShowItemDetail(InventorySlotDto slotData, bool showUseButton = true,
                               string buttonTextOverride = null, System.Action primaryButtonAction = null)
    {
        var panel = GetOrCreateDetailPanel();
        if (panel != null)
            panel.ShowItem(slotData, showUseButton, buttonTextOverride, primaryButtonAction);
        else
            Debug.LogWarning("[InventoryUI] itemDetailPanel chưa được gán trong Inspector!");
    }

    /// <summary>
    /// Callback khi người chơi nhấn vào 1 slot có item — mở ItemDetailPanel.
    /// </summary>
    private void OnSlotItemClicked(InventorySlotDto slotData)
    {
        // Chế độ nâng cấp Thợ Rèn: item trang bị (category 1 / type 0–5) → hiện nút "Nâng cấp"
        if (_blacksmithUpgradeMode && slotData != null && slotData.quantity > 0)
        {
            var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(slotData.id);
            if (tmpl != null && tmpl.category == 1)
            {
                var capturedSlot     = slotData;
                var capturedCallback = _blacksmithUpgradeCallback;
                ShowItemDetail(slotData, showUseButton: true,
                    buttonTextOverride: "Nâng cấp",
                    primaryButtonAction: () => capturedCallback?.Invoke(capturedSlot));
                return;
            }
        }
        ShowItemDetail(slotData);
    }

    /// <summary>
    /// Ẩn panel chi tiết item (gọi khi đóng inventory hoặc click vùng trống)
    /// </summary>
    public void HideItemDetail()
    {
        _itemDetailPanelInstance?.Hide();
    }

    // ═══════════════════════════════════════════════════════
    // STONE / ITEM SELECT MODE  (dùng cho cửa sổ Thợ Rèn)
    // ═══════════════════════════════════════════════════════

    private bool _inSelectMode    = false;
    private int  _selectFilterId  = 0;    // lọc theo item_template.id (0 = không filter)
    private int  _selectFilterType= 0;    // lọc theo item type (0 = không filter)

    // ═══════════════════════════════════════════════════════
    // BLACKSMITH UPGRADE MODE  (nâng cấp trang bị từ túi)
    // ═══════════════════════════════════════════════════════

    private bool _blacksmithUpgradeMode = false;
    private System.Action<InventorySlotDto> _blacksmithUpgradeCallback;

    /// <summary>
    /// Bật / tắt chế độ Thợ Rèn: khi bật, nhấn vào item trang bị (type 0–5)
    /// sẽ hiện nút "Nâng cấp" thay vì "Sử dụng" / "Trang bị".
    /// </summary>
    public void SetBlacksmithUpgradeMode(bool active, System.Action<InventorySlotDto> upgradeCallback = null)
    {
        _blacksmithUpgradeMode    = active;
        _blacksmithUpgradeCallback = active ? upgradeCallback : null;
    }
    private System.Action<InventorySlotDto> _selectCallback;

    /// <summary>
    /// Vào chế độ chọn item: các ô khớp filter sẽ hiện nút "Chọn",
    /// các ô khác bị mờ.
    /// </summary>
    public void EnterItemSelectMode(int filterById = 0, int filterByType = 0,
                                    System.Action<InventorySlotDto> callback = null)
    {
        _inSelectMode     = true;
        _selectFilterId   = filterById;
        _selectFilterType = filterByType;
        _selectCallback   = callback;
        ApplySelectModeToSlots();
    }

    /// <summary>Thoát khỏi chế độ chọn, khôi phục UI bình thường.</summary>
    public void ExitItemSelectMode()
    {
        _inSelectMode     = false;
        _selectFilterId   = 0;
        _selectFilterType = 0;
        _selectCallback   = null;
        ApplySelectModeToSlots();
    }

    private void ApplySelectModeToSlots()
    {
        if (slotUIs == null) return;
        foreach (var slotUI in slotUIs)
        {
            if (slotUI == null) continue;
            var data = slotUI.GetCurrentData();

            if (!_inSelectMode)
            {
                slotUI.SetSelectMode(false, false, null);
                continue;
            }

            bool match = false;
            if (data != null && data.quantity > 0)
            {
                if (_selectFilterId > 0)
                    match = data.id == _selectFilterId;
                else if (_selectFilterType > 0)
                {
                    var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(data.id);
                    match = tmpl != null && tmpl.type == _selectFilterType;
                }
                else
                    match = true;
            }

            slotUI.SetSelectMode(
                inSelectMode: true,
                canSelect:    match,
                onSelect:     match ? () => OnSelectModeSlotClicked(data) : (System.Action)null
            );
        }
    }

    private void OnSelectModeSlotClicked(InventorySlotDto data)
    {
        _selectCallback?.Invoke(data);
    }

    private void OnDestroy()
    {
        UIPanelManager.Unregister(gameObject);

        // Unsubscribe events
        if (slotUIs != null)
        {
            foreach (var slot in slotUIs)
            {
                if (slot != null)
                    slot.OnSlotClicked -= OnSlotItemClicked;
            }
        }

        // Destroy panel instance nếu có
        if (_itemDetailPanelInstance != null)
            Destroy(_itemDetailPanelInstance.gameObject);
    }

}

