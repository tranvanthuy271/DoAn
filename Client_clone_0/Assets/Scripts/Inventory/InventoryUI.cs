using UnityEngine;
using UnityEngine.Events;

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
    [Tooltip("Panel hiển thị chi tiết item khi nhấn vào slot")]
    [SerializeField] private ItemDetailPanel itemDetailPanel;

    [Header("Settings")]
    [Tooltip("Số slot tối đa trong UI (nên >= số slot server gửi về)")]
    [SerializeField] private int maxSlotCount = 20;

    private InventorySlotUI[] slotUIs;
    private InventorySlotDto[] currentSlots;

    private void Awake()
    {
        if (inventoryRoot == null)
        {
            inventoryRoot = gameObject;
        }

        // Đảm bảo ban đầu Inventory đóng
        if (inventoryRoot != null)
        {
            inventoryRoot.SetActive(false);
        }
    }

    private void Start()
    {
        // Khởi tạo các ô slot UI khi bắt đầu
        InitSlots();
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
    }

    /// <summary>
    /// Gọi để bật/tắt panel inventory (dùng cho Button OnClick)
    /// </summary>
    public void ToggleInventory()
    {
        if (inventoryRoot == null)
        {
            Debug.LogWarning("[InventoryUI] ToggleInventory: inventoryRoot is null!");
            return;
        }

        bool isActive = !inventoryRoot.activeSelf;
        inventoryRoot.SetActive(isActive);
        
        Debug.Log($"[InventoryUI] ToggleInventory: Panel {(isActive ? "MỞ" : "ĐÓNG")}");

        if (isActive)
        {
            // Đảm bảo slots đã được khởi tạo
            if (slotUIs == null || slotUIs.Length == 0)
            {
                Debug.LogWarning("[InventoryUI] ToggleInventory: slotUIs chưa init, gọi InitSlots() ngay bây giờ...");
                InitSlots();
            }
            
            Debug.Log($"[InventoryUI] ToggleInventory: Đang refresh {slotUIs?.Length ?? 0} slots...");
            
            // ✅ REFRESH INVENTORY FROM DB KHI MỞ UI
            var bridge = FindObjectOfType<InventoryNetworkBridge>();
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
        if (slotUIs == null)
        {
            Debug.LogWarning("[InventoryUI] RefreshAllSlots: slotUIs is null! Có thể chưa InitSlots()?");
            return;
        }

        Debug.Log($"[InventoryUI] RefreshAllSlots: Bắt đầu refresh {slotUIs.Length} slots...");
        Debug.Log($"[InventoryUI] RefreshAllSlots: currentSlots = {(currentSlots == null ? "null" : $"{currentSlots.Length} items")}");

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
                slotsWithItems++;
                Debug.Log($"[InventoryUI] RefreshAllSlots: Slot {i} có item - code={slotData.itemCode}, iconId={slotData.iconId}, qty={slotData.quantity}");
            }

            slotUIs[i].SetSlot(slotData);
        }

        Debug.Log($"[InventoryUI] RefreshAllSlots: Hoàn thành! Có {slotsWithItems} slots có item được hiển thị.");
    }

    /// <summary>
    /// Callback khi người chơi nhấn vào 1 slot có item
    /// </summary>
    private void OnSlotItemClicked(InventorySlotDto slotData)
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.ShowItem(slotData);
        }
        else
        {
            Debug.LogWarning("[InventoryUI] itemDetailPanel chưa được gán trong Inspector!");
        }
    }

    /// <summary>
    /// Ẩn panel chi tiết item (gọi khi đóng inventory hoặc click vùng trống)
    /// </summary>
    public void HideItemDetail()
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.Hide();
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe events
        if (slotUIs != null)
        {
            foreach (var slot in slotUIs)
            {
                if (slot != null)
                {
                    slot.OnSlotClicked -= OnSlotItemClicked;
                }
            }
        }
    }
}

