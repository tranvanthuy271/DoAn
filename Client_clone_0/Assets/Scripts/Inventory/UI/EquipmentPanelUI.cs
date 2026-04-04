using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// EquipmentPanelUI - Panel UI hiển thị trang bị của player
/// Gồm 6 ô: 1 Vũ khí + 5 Phụ kiện (Mũ, Giáp, Quần, Giày, Phụ kiện)
/// 
/// Setup trong Unity:
/// 1. Tạo Panel "EquipmentPanel" trong Canvas
/// 2. Tạo 6 ô slot con (dùng prefab EquipmentSlotUI hoặc tạo thủ công)
/// 3. Kéo reference vào Inspector
/// 4. Gắn script này lên Panel
/// 
/// Có 2 cách setup slots:
/// A) Tự động: Gán slotPrefab + slotContainer → Script tự tạo 6 slots
/// B) Thủ công: Kéo 6 EquipmentSlotUI vào array manualSlots trong Inspector
/// </summary>
public class EquipmentPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Panel gốc để bật/tắt")]
    [SerializeField] private GameObject panelRoot;

    [Header("Auto-Create Slots (Cách A)")]
    [Tooltip("Prefab cho 1 ô trang bị")]
    [SerializeField] private EquipmentSlotUI slotPrefab;

    [Tooltip("Transform chứa các ô slot")]
    [SerializeField] private Transform slotContainer;

    [Header("Manual Slots (Cách B) - Ưu tiên hơn Auto-Create")]
    [Tooltip("Gán thủ công 6 slot: [0]=Weapon, [1]=Helmet, [2]=Armor, [3]=Pants, [4]=Boots, [5]=Accessory")]
    [SerializeField] private EquipmentSlotUI[] manualSlots;

    [Header("Unequip Confirmation")]
    [Tooltip("Panel xác nhận tháo trang bị (tuỳ chọn)")]
    [SerializeField] private GameObject unequipConfirmPanel;

    [Tooltip("Text hiển thị tên item sẽ tháo")]
    [SerializeField] private TMP_Text unequipItemNameText;

    [Tooltip("Nút xác nhận tháo")]
    [SerializeField] private Button confirmUnequipButton;

    [Tooltip("Nút hủy tháo")]
    [SerializeField] private Button cancelUnequipButton;

    [Header("Title")]
    [Tooltip("Text tiêu đề panel")]
    [SerializeField] private TMP_Text titleText;

    // Slot UI instances
    private Dictionary<EquipmentSlotType, EquipmentSlotUI> slotUIs = new Dictionary<EquipmentSlotType, EquipmentSlotUI>();

    // Current equipment data
    private PlayerEquipmentDto currentEquipment;

    // Slot đang chờ unequip
    private EquipmentSlotType? pendingUnequipSlot;

    /// <summary>
    /// Event khi người dùng muốn tháo trang bị
    /// </summary>
    public event Action<EquipmentSlotType> OnUnequipRequested;

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        UIRuntimeAssetHelper.ApplyNotoSans(titleText, unequipItemNameText);

        // Setup title
        if (titleText != null)
        {
            titleText.text = "Trang Bị";
        }

        // Setup unequip confirmation buttons
        if (confirmUnequipButton != null)
        {
            confirmUnequipButton.onClick.AddListener(OnConfirmUnequip);
        }
        if (cancelUnequipButton != null)
        {
            cancelUnequipButton.onClick.AddListener(OnCancelUnequip);
        }
        if (unequipConfirmPanel != null)
        {
            unequipConfirmPanel.SetActive(false);
        }
    }

    private void Start()
    {
        InitSlots();
        // Không tự ẩn panelRoot ở đây – việc hiển thị/ẩn do CharacterPanelController quản lý
        // thông qua contentEquipment.SetActive(). Nếu tự SetActive(false) thì dù parent
        // được bật lại, child này vẫn giữ activeSelf=false và không hiện lên.
    }

    /// <summary>
    /// Khởi tạo các slot UI
    /// </summary>
    private void InitSlots()
    {
        slotUIs.Clear();

        // Ưu tiên manual slots
        if (manualSlots != null && manualSlots.Length >= 6)
        {
            Debug.Log("[EquipmentPanelUI] Sử dụng manual slots");
            for (int i = 0; i < 6; i++)
            {
                EquipmentSlotType slotType = (EquipmentSlotType)i;
                if (manualSlots[i] != null)
                {
                    manualSlots[i].Init(slotType);
                    manualSlots[i].OnSlotClicked += OnEquipmentSlotClicked;
                    slotUIs[slotType] = manualSlots[i];
                }
            }
        }
        // Auto-create từ prefab
        else if (slotPrefab != null && slotContainer != null)
        {
            Debug.Log("[EquipmentPanelUI] Tự động tạo slots từ prefab");
            
            // Xóa con cũ
            for (int i = slotContainer.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(slotContainer.GetChild(i).gameObject);
            }

            // Tạo 6 slots
            for (int i = 0; i < 6; i++)
            {
                EquipmentSlotType slotType = (EquipmentSlotType)i;
                EquipmentSlotUI slot = Instantiate(slotPrefab, slotContainer);
                slot.Init(slotType);
                slot.OnSlotClicked += OnEquipmentSlotClicked;
                slotUIs[slotType] = slot;
                slot.gameObject.name = $"EquipSlot_{slotType}";
            }
        }
        else
        {
            Debug.LogWarning("[EquipmentPanelUI] Không có manualSlots hoặc slotPrefab/slotContainer! Kiểm tra Inspector.");
        }

        Debug.Log($"[EquipmentPanelUI] Đã khởi tạo {slotUIs.Count} equipment slots");
    }

    /// <summary>
    /// Bật/tắt panel trang bị
    /// </summary>
    public void TogglePanel()
    {
        if (panelRoot == null) return;

        bool isActive = !panelRoot.activeSelf;
        panelRoot.SetActive(isActive);

        Debug.Log($"[EquipmentPanelUI] Panel {(isActive ? "MỞ" : "ĐÓNG")}");

        if (isActive)
        {
            // Refresh equipment khi mở
            RefreshFromBridge();
        }
        else
        {
            HideUnequipConfirm();
        }
    }

    /// <summary>
    /// Mở panel
    /// </summary>
    public void Show()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            RefreshFromBridge();
        }
    }

    /// <summary>
    /// Đóng panel
    /// </summary>
    public void Hide()
    {
        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
        HideUnequipConfirm();
    }

    /// <summary>
    /// Kiểm tra panel có đang mở không
    /// </summary>
    public bool IsVisible()
    {
        return panelRoot != null && panelRoot.activeSelf;
    }

    /// <summary>
    /// Cập nhật toàn bộ UI từ PlayerEquipmentDto
    /// </summary>
    public void SetEquipmentData(PlayerEquipmentDto equipment)
    {
        currentEquipment = equipment;
        RefreshAllSlots();
    }

    /// <summary>
    /// Refresh UI từ InventoryNetworkBridge
    /// </summary>
    public void RefreshFromBridge()
    {
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge != null)
        {
            bridge.RefreshEquipmentFromDB();
        }
        else
        {
            Debug.LogWarning("[EquipmentPanelUI] Không tìm thấy InventoryNetworkBridge!");
        }
    }

    /// <summary>
    /// Refresh tất cả slot UI
    /// </summary>
    private void RefreshAllSlots()
    {
        if (slotUIs.Count == 0)
        {
            Debug.LogWarning("[EquipmentPanelUI] Chưa có slot UI! Gọi InitSlots() trước.");
            InitSlots();
        }

        foreach (var kvp in slotUIs)
        {
            EquipmentSlotType slotType = kvp.Key;
            EquipmentSlotUI slotUI = kvp.Value;

            if (currentEquipment != null)
            {
                EquipmentItemDto item = currentEquipment.GetSlot(slotType);
                slotUI.SetItem(item);
            }
            else
            {
                slotUI.Clear();
            }
        }

        Debug.Log($"[EquipmentPanelUI] Đã refresh {slotUIs.Count} slots");
    }

    /// <summary>
    /// Callback khi click vào 1 slot trang bị
    /// </summary>
    private void OnEquipmentSlotClicked(EquipmentSlotType slotType, EquipmentItemDto item)
    {
        Debug.Log($"[EquipmentPanelUI] Click slot {slotType}, item={item?.itemName ?? "trống"}");

        if (item == null || item.itemTemplateId <= 0)
        {
            // Slot trống - không làm gì
            return;
        }

        // Hiện confirm tháo trang bị
        ShowUnequipConfirm(slotType, item);
    }

    /// <summary>
    /// Hiện panel xác nhận tháo trang bị
    /// </summary>
    private void ShowUnequipConfirm(EquipmentSlotType slotType, EquipmentItemDto item)
    {
        pendingUnequipSlot = slotType;

        if (unequipConfirmPanel != null)
        {
            unequipConfirmPanel.SetActive(true);

            if (unequipItemNameText != null)
            {
                string slotName = PlayerEquipmentDto.GetSlotDisplayName(slotType);
                unequipItemNameText.text = $"Tháo {item.itemName ?? item.itemCode}\nkhỏi ô {slotName}?";
            }
        }
        else
        {
            // Không có panel xác nhận → tháo luôn
            OnConfirmUnequip();
        }
    }

    /// <summary>
    /// Ẩn panel xác nhận
    /// </summary>
    private void HideUnequipConfirm()
    {
        pendingUnequipSlot = null;
        if (unequipConfirmPanel != null)
        {
            unequipConfirmPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Xác nhận tháo trang bị
    /// </summary>
    private void OnConfirmUnequip()
    {
        if (pendingUnequipSlot.HasValue)
        {
            EquipmentSlotType slot = pendingUnequipSlot.Value;
            Debug.Log($"[EquipmentPanelUI] Xác nhận tháo trang bị slot {slot}");

            // Fire event
            OnUnequipRequested?.Invoke(slot);

            // Gọi bridge để xử lý
            var bridge = FindObjectOfType<InventoryNetworkBridge>();
            if (bridge != null)
            {
                bridge.RequestUnequipItem(slot);
            }
        }

        HideUnequipConfirm();
    }

    /// <summary>
    /// Hủy tháo trang bị
    /// </summary>
    private void OnCancelUnequip()
    {
        HideUnequipConfirm();
    }

    private void OnDestroy()
    {
        // Unsubscribe events
        foreach (var kvp in slotUIs)
        {
            if (kvp.Value != null)
            {
                kvp.Value.OnSlotClicked -= OnEquipmentSlotClicked;
            }
        }

        if (confirmUnequipButton != null)
        {
            confirmUnequipButton.onClick.RemoveListener(OnConfirmUnequip);
        }
        if (cancelUnequipButton != null)
        {
            cancelUnequipButton.onClick.RemoveListener(OnCancelUnequip);
        }
    }
}
