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

    [Header("Settings")]
    [Tooltip("Tự động tìm NetworkInventory của local player khi Start")]
    [SerializeField] private bool autoFindPlayerInventory = true;

    private bool hasSubscribedToNetworkEvents = false;

    private void Start()
    {
        // Tìm InventoryUI nếu chưa gán
        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI == null)
            {
                Debug.LogWarning("[InventoryNetworkBridge] Không tìm thấy InventoryUI trong scene!");
            }
        }

        // Subscribe vào NetworkManager events để tự động tìm NetworkInventory khi client connect
        SubscribeToNetworkEvents();

        // Tìm NetworkInventory nếu chưa gán (có thể chưa có nếu player chưa spawn)
        if (networkInventory == null && autoFindPlayerInventory)
        {
            FindPlayerInventory();
        }

        // Subscribe event từ NetworkInventory nếu đã tìm thấy
        if (networkInventory != null)
        {
            SubscribeToInventoryEvents();
        }
    }

    private void SubscribeToNetworkEvents()
    {
        if (hasSubscribedToNetworkEvents) return;

        var networkManager = NetworkManager.Singleton;
        if (networkManager != null)
        {
            networkManager.OnClientConnectedCallback += OnClientConnected;
            hasSubscribedToNetworkEvents = true;
            Debug.Log("[InventoryNetworkBridge] Subscribed to OnClientConnectedCallback");
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
        // Đợi 0.5 giây để đảm bảo player object đã spawn
        yield return new WaitForSeconds(0.5f);

        if (networkInventory == null && autoFindPlayerInventory)
        {
            FindPlayerInventory();
            
            // Nếu tìm thấy, subscribe events
            if (networkInventory != null)
            {
                SubscribeToInventoryEvents();
            }
        }
    }

    private void SubscribeToInventoryEvents()
    {
        if (networkInventory != null)
        {
            networkInventory.OnInventoryChanged.AddListener(OnInventoryChanged);
            
            // Refresh ngay lần đầu
            RefreshInventoryUI();
            Debug.Log("[InventoryNetworkBridge] Subscribed to NetworkInventory.OnInventoryChanged");
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

        // Tìm trong các NetworkObject đã spawn
        foreach (var networkObject in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (networkObject == null) continue; // Bỏ qua null entries

            if (networkObject.IsOwner || networkObject.IsOwnedByServer)
            {
                NetworkInventory inv = networkObject.GetComponent<NetworkInventory>();
                if (inv != null)
                {
                    networkInventory = inv;
                    Debug.Log($"[InventoryNetworkBridge] Tìm thấy NetworkInventory của player: {networkObject.name}");
                    return;
                }
            }
        }

        Debug.LogWarning("[InventoryNetworkBridge] Không tìm thấy NetworkInventory của local player!");
    }

    /// <summary>
    /// Callback khi NetworkInventory thay đổi
    /// </summary>
    private void OnInventoryChanged()
    {
        Debug.Log("[InventoryNetworkBridge] OnInventoryChanged: NetworkInventory đã thay đổi, đang refresh UI...");
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
            InventorySlot slot = networkInventory.GetSlot(i);
            
            if (slot != null && slot.itemData != null && slot.quantity > 0)
            {
                itemsFound++;
                string iconId = GetIconIdFromItemData(slot.itemData);
                
                // Convert InventorySlot → InventorySlotDto
                InventorySlotDto dto = new InventorySlotDto
                {
                    slotIndex = i,
                    itemTemplateId = slot.itemData.itemID,
                    itemCode = slot.itemData.itemName, // Tạm dùng itemName làm code, bạn có thể thêm field code vào ItemData sau
                    iconId = iconId,
                    quantity = slot.quantity,
                    isEquipped = false // Tạm để false, bạn có thể thêm flag này vào NetworkInventory sau
                };

                Debug.Log($"[InventoryNetworkBridge] RefreshInventoryUI: Slot {i} - itemID={slot.itemData.itemID}, name={slot.itemData.itemName}, iconId={iconId}, qty={slot.quantity}");
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
}
