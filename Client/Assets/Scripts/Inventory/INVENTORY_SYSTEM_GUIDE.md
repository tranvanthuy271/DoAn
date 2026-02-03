# Hệ Thống Túi Đồ (Inventory System) với Unity Netcode

## Tổng Quan

Hệ thống túi đồ này được thiết kế để hoạt động trong môi trường multiplayer với Unity Netcode for GameObjects. Tất cả dữ liệu inventory được đồng bộ hóa giữa server và clients thông qua NetworkVariable, đảm bảo tính nhất quán và chống cheat.

## Kiến Trúc Hệ Thống

### 1. **ItemData** (ScriptableObject)
- Định nghĩa các loại item trong game
- Chứa thông tin: tên, mô tả, icon, ID, loại item, stackable, drop prefab, etc.
- Tạo asset trong Unity: `Right-click > Create > Item > ItemData`

### 2. **NetworkInventory** (NetworkBehaviour)
- Component chính quản lý inventory của player
- Sử dụng NetworkVariable để sync dữ liệu
- Server-authoritative: chỉ server mới có thể thay đổi inventory
- Clients chỉ đọc và hiển thị

### 3. **ItemPickup** (NetworkBehaviour)
- Component gắn vào item drop trên ground
- Xử lý việc nhặt item khi player đến gần
- Tự động despawn sau khi được nhặt

### 4. **EnemyItemDrop**
- Component gắn vào enemy
- Drop item khi enemy chết
- Hỗ trợ drop rate và random quantity

## Cài Đặt và Sử Dụng

### Bước 1: Tạo ItemData Assets

1. Tạo thư mục `Assets/Resources/Items/` (hoặc bất kỳ thư mục nào trong Resources)
2. Tạo ItemData asset:
   - `Right-click > Create > Item > ItemData`
   - Đặt tên: `Item_Stone`, `Item_Potion`, etc.
   - Điền thông tin:
     - **Item Name**: Tên hiển thị
     - **Item ID**: ID duy nhất (ví dụ: 1, 2, 3...)
     - **Item Type**: Loại item (Weapon, Consumable, Material, etc.)
     - **Stackable**: Có thể stack không
     - **Max Stack**: Số lượng tối đa mỗi stack
     - **Icon**: Sprite icon của item
     - **Drop Prefab**: Prefab của ItemPickup (sẽ tạo ở bước sau)

### Bước 2: Tạo ItemPickup Prefab

1. Tạo GameObject mới: `ItemPickup`
2. Thêm các components:
   - **NetworkObject** (từ Netcode)
   - **ItemPickup** (script)
   - **SpriteRenderer** (để hiển thị icon)
   - **Collider2D** (CircleCollider2D hoặc BoxCollider2D)
     - Đặt `Is Trigger = true`
   - **Rigidbody2D** (để vật lý khi drop)
     - Đặt `Body Type = Kinematic` hoặc `Dynamic`
     - Đặt `Gravity Scale = 1` nếu muốn item rơi xuống

3. Cấu hình ItemPickup:
   - **Item Data**: Để trống (sẽ set khi spawn)
   - **Quantity**: Mặc định 1
   - **Pickup Range**: 1.5 (khoảng cách tự động nhặt)
   - **Auto Pickup**: true
   - **Player Layer**: Layer của Player (mặc định Layer 6)

4. Lưu thành Prefab: `Assets/Prefabs/ItemPickup.prefab`

### Bước 3: Gắn NetworkInventory vào Player

1. Mở Player Prefab
2. Thêm component **NetworkInventory**
3. Cấu hình:
   - **Max Slots**: 20 (số lượng slot tối đa)
   - **Events**: Có thể subscribe để update UI

4. Đảm bảo Player có **NetworkObject** component

### Bước 4: Gắn EnemyItemDrop vào Enemy

1. Mở Enemy Prefab
2. Thêm component **EnemyItemDrop**
3. Cấu hình:
   - **Item Pickup Prefab**: Kéo prefab ItemPickup vào đây
   - **Drop Force**: 3 (lực khi drop)
   - **Drop Spread**: 1 (độ phân tán)

4. Thêm Drop Items:
   - Click `+` trong Drop Items list
   - Gán **Item Data** (kéo ItemData asset vào)
   - **Drop Rate**: 50% (tỷ lệ drop, 0-100)
   - **Min Quantity**: 1
   - **Max Quantity**: 3

5. Đảm bảo Enemy có **NetworkEnemyHealth** hoặc **EnemyHealth** component

### Bước 5: Tạo ItemManager (Optional - để load ItemData)

Tạo script `ItemManager.cs` để quản lý ItemData:

```csharp
using UnityEngine;
using System.Collections.Generic;

public class ItemManager : MonoBehaviour
{
    private static ItemManager instance;
    private Dictionary<int, ItemData> itemDatabase = new Dictionary<int, ItemData>();

    public static ItemManager Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadItemDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadItemDatabase()
    {
        ItemData[] items = Resources.LoadAll<ItemData>("Items");
        foreach (var item in items)
        {
            itemDatabase[item.itemID] = item;
        }
        Debug.Log($"[ItemManager] Loaded {itemDatabase.Count} items");
    }

    public ItemData GetItemData(int itemID)
    {
        return itemDatabase.TryGetValue(itemID, out ItemData item) ? item : null;
    }
}
```

Sau đó cập nhật `NetworkInventory.cs` để dùng ItemManager:

```csharp
private ItemData GetItemDataByID(int itemID)
{
    if (ItemManager.Instance != null)
    {
        return ItemManager.Instance.GetItemData(itemID);
    }
    
    // Fallback: Load từ Resources
    ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
    foreach (var item in allItems)
    {
        if (item.itemID == itemID)
            return item;
    }
    return null;
}
```

## Luồng Hoạt Động

### 1. Enemy Chết → Drop Item

```
Enemy Death Event
    ↓
EnemyItemDrop.OnEnemyDeath()
    ↓
DropItems() (chỉ trên Server)
    ↓
Spawn ItemPickup với NetworkObject.Spawn()
    ↓
Item xuất hiện trên ground
```

### 2. Player Nhặt Item

```
Player vào range của ItemPickup
    ↓
ItemPickup.CheckPlayerInRange()
    ↓
TryPickupItemServerRpc() (gửi lên Server)
    ↓
Server: NetworkInventory.AddItemServerRpc()
    ↓
Server: Cập nhật NetworkVariable
    ↓
Tất cả Clients: Nhận update qua OnInventoryDataChanged
    ↓
ItemPickup.DespawnItemClientRpc()
    ↓
Item biến mất
```

### 3. Sử Dụng Item

```
Player sử dụng item (UI hoặc input)
    ↓
NetworkInventory.UseItemServerRpc()
    ↓
Server: Validate và apply effect
    ↓
Server: Giảm quantity hoặc xóa item
    ↓
Clients: Nhận update
```

## Network Synchronization

### NetworkVariable

`NetworkInventory` sử dụng `NetworkVariable<NetworkInventoryData>` để sync:

- **Read Permission**: Everyone (tất cả clients đều đọc được)
- **Write Permission**: Server (chỉ server mới viết được)

### ServerRpc và ClientRpc

- **AddItemServerRpc**: Client yêu cầu server thêm item
- **RemoveItemServerRpc**: Client yêu cầu server xóa item
- **UseItemServerRpc**: Client yêu cầu server sử dụng item
- **OnItemAddedClientRpc**: Server notify clients về item mới
- **OnItemRemovedClientRpc**: Server notify clients về item bị xóa

## API Sử Dụng

### NetworkInventory

```csharp
// Thêm item
inventory.AddItem(itemID, quantity);

// Xóa item
inventory.RemoveItem(slotIndex, quantity);

// Sử dụng item
inventory.UseItem(slotIndex);

// Kiểm tra có item không
bool hasItem = inventory.HasItem(itemID, quantity);

// Lấy số lượng item
int quantity = inventory.GetItemQuantity(itemID);

// Lấy slot
InventorySlot slot = inventory.GetSlot(slotIndex);
```

### Events

```csharp
// Subscribe events
inventory.OnItemAdded.AddListener((slotIndex, itemData, quantity) => {
    Debug.Log($"Added {quantity}x {itemData.itemName} to slot {slotIndex}");
});

inventory.OnItemRemoved.AddListener((slotIndex, quantity) => {
    Debug.Log($"Removed {quantity} from slot {slotIndex}");
});

inventory.OnInventoryChanged.AddListener(() => {
    // Update UI
    UpdateInventoryUI();
});
```

## Tạo UI Inventory

### Ví dụ Script UI:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject inventoryPanel;
    public Transform slotContainer;
    public GameObject slotPrefab;
    
    private NetworkInventory playerInventory;
    private InventorySlotUI[] slotUIs;

    private void Start()
    {
        // Tìm player inventory
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerInventory = player.GetComponent<NetworkInventory>();
            if (playerInventory != null)
            {
                InitializeUI();
                SubscribeToEvents();
            }
        }
    }

    private void InitializeUI()
    {
        int maxSlots = playerInventory.GetMaxSlots();
        slotUIs = new InventorySlotUI[maxSlots];

        for (int i = 0; i < maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            slotUIs[i] = slotObj.GetComponent<InventorySlotUI>();
            slotUIs[i].Initialize(i, playerInventory);
        }
    }

    private void SubscribeToEvents()
    {
        playerInventory.OnItemAdded.AddListener(OnItemAdded);
        playerInventory.OnItemRemoved.AddListener(OnItemRemoved);
        playerInventory.OnInventoryChanged.AddListener(UpdateAllSlots);
    }

    private void OnItemAdded(int slotIndex, ItemData itemData, int quantity)
    {
        UpdateSlot(slotIndex);
    }

    private void OnItemRemoved(int slotIndex, int quantity)
    {
        UpdateSlot(slotIndex);
    }

    private void UpdateAllSlots()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            UpdateSlot(i);
        }
    }

    private void UpdateSlot(int slotIndex)
    {
        InventorySlot slot = playerInventory.GetSlot(slotIndex);
        slotUIs[slotIndex].UpdateSlot(slot);
    }

    public void ToggleInventory()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }
}
```

## Lưu Ý Quan Trọng

### 1. Server Authority
- **Luôn validate trên Server**: Tất cả thao tác với inventory phải qua ServerRpc
- **Không trust client**: Client chỉ gửi request, server quyết định

### 2. NetworkObject Spawn
- ItemPickup phải có NetworkObject và được spawn bằng `NetworkObject.Spawn()`
- Chỉ Server mới spawn được NetworkObject

### 3. ItemID Phải Unique
- Mỗi ItemData phải có itemID duy nhất
- Không được trùng itemID giữa các item

### 4. Resources Folder
- ItemData assets phải nằm trong thư mục `Resources/` để có thể load bằng `Resources.LoadAll<T>()`
- Hoặc dùng ItemManager để quản lý tốt hơn

### 5. Performance
- NetworkVariable chỉ sync khi giá trị thay đổi
- Sử dụng local cache (`localInventory`) để truy cập nhanh
- Tránh parse NetworkVariable mỗi frame

## Troubleshooting

### Item không drop khi enemy chết
- Kiểm tra EnemyItemDrop có được gắn vào enemy không
- Kiểm tra EnemyHealth có gọi OnDeath event không
- Kiểm tra NetworkManager.IsServer = true khi enemy chết

### Item không nhặt được
- Kiểm tra ItemPickup có NetworkObject không
- Kiểm tra Collider2D có Is Trigger = true không
- Kiểm tra Player Layer có đúng không
- Kiểm tra NetworkInventory có được gắn vào Player không

### Inventory không sync giữa clients
- Kiểm tra NetworkInventory có NetworkObject không
- Kiểm tra NetworkVariable có được khởi tạo đúng không
- Kiểm tra Server có đang chạy không

### Item không hiển thị trong inventory
- Kiểm tra ItemData có được load đúng không (itemID match)
- Kiểm tra UI có subscribe events không
- Kiểm tra localInventory có được update không

## Mở Rộng

### Thêm Item Types
- Mở rộng enum `ItemType` trong `ItemData.cs`
- Thêm logic xử lý trong `ApplyItemEffectServerRpc()`

### Thêm Item Effects
- Tạo script `ItemEffect.cs` để xử lý các effect khác nhau
- Sử dụng Strategy Pattern để dễ mở rộng

### Thêm Equipment System
- Tạo `NetworkEquipment` component tương tự NetworkInventory
- Quản lý weapon, armor, accessory riêng

### Thêm Item Trading
- Tạo `TradeSystem` với ServerRpc để trade giữa players
- Validate trên server để chống cheat

## Kết Luận

Hệ thống inventory này cung cấp nền tảng vững chắc cho việc quản lý item trong multiplayer game. Tất cả logic đều được xử lý trên server để đảm bảo tính công bằng và chống cheat. Bạn có thể mở rộng hệ thống này theo nhu cầu của game.
