# 📦 Hướng Dẫn Hệ Thống Inventory Đã Refactor

## 🎯 Tổng Quan

Hệ thống inventory đã được refactor với các tính năng mới:

1. **Test thêm item bằng phím Q**: Tự động thêm các item có sẵn từ data vào túi
2. **Sync với Database**: Host tự động cập nhật inventory lên DB khi có thay đổi
3. **Unity Netcode Integration**: Đồng bộ inventory giữa Host và Client
4. **API-driven**: Sử dụng REST API để quản lý dữ liệu inventory

---

## 🔄 Luồng Hoạt Động (Flow)

### 1. Luồng Thêm Item Khi Nhấn Phím Q

```
┌─────────────────────────────────────────────────────────────┐
│                    CLIENT / HOST                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Player nhấn phím Q              │
        │  (InventoryTestManager)           │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Lấy danh sách test items        │
        │  (cấu hình trong Inspector)       │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Gửi ServerRpc                   │
        │  AddItemWithDBSyncServerRpc()    │
        │  - itemTemplateId                 │
        │  - itemCode                       │
        │  - iconId                         │
        │  - quantity                       │
        └───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    HOST (SERVER)                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  NetworkInventory nhận ServerRpc  │
        │  AddItemWithDBSyncServerRpc()    │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  1. Thêm item vào NetworkVariable│
        │     (tìm slot trống)              │
        │     (update networkInventoryData) │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  2. Gọi API HTTP                 │
        │     POST /api/player/{id}/        │
        │          inventory/add            │
        │     (APIClient.AddItemsToInventory)│
        └───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    API SERVER                                │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  PlayerController.                │
        │  AddItemsToInventory()           │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  1. Lấy inventory hiện tại từ DB │
        │     (parse JSON)                  │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  2. Tìm slot trống               │
        │     (slotIndex = 0..19)          │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  3. Thêm item vào slot           │
        │     { slotIndex, itemTemplateId, │
        │       itemCode, iconId, quantity }│
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  4. Serialize và lưu vào DB      │
        │     UPDATE player_data           │
        │     SET inventory = JSON         │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  5. Trả về response OK           │
        │     { message, inventory }       │
        └───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    HOST (SERVER)                            │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  3. Gọi OnItemAddedClientRpc()   │
        │     (notify tất cả clients)       │
        └───────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│            ALL CLIENTS (bao gồm Host)                       │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  NetworkVariable changed event    │
        │  OnInventoryDataChanged()        │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  Deserialize inventory data       │
        │  UpdateInventoryUI()             │
        └───────────────────────────────────┘
                            │
                            ▼
        ┌───────────────────────────────────┐
        │  UI hiển thị item mới            │
        │  (icon + quantity)               │
        └───────────────────────────────────┘
```

---

## 📁 Cấu Trúc File

### Backend (GameServerApi)

```
GameServerApi/
├── Controllers/
│   └── PlayerController.cs           (✨ MỚI: AddItemsToInventory endpoint)
└── ...
```

### Unity Client

```
Client/Assets/Scripts/
├── API/
│   └── APIClient.cs                  (✨ MỚI: AddItemsToInventory method)
├── Inventory/
│   ├── NetworkInventory.cs           (✨ REFACTORED: AddItemWithDBSyncServerRpc)
│   ├── InventoryTestManager.cs       (✨ MỚI: Quản lý test phím Q)
│   ├── InventoryUI.cs               
│   ├── InventorySlotUI.cs           
│   ├── InventoryNetworkBridge.cs    
│   └── InventoryDtos.cs             
└── ...
```

---

## 🛠️ Setup Chi Tiết

### A. Setup Backend API

#### 1. Kiểm tra API Endpoint

File: `GameServerApi/Controllers/PlayerController.cs`

Endpoint mới đã được thêm:

```csharp
[HttpPost("{playerId}/inventory/add")]
public async Task<IActionResult> AddItemsToInventory(int playerId, [FromBody] JsonElement body)
```

**Request Format:**

```json
POST /api/player/1/inventory/add
Authorization: Bearer <JWT_TOKEN>
Content-Type: application/json

{
  "items": [
    {
      "itemTemplateId": 1,
      "itemCode": "ITEM_ICON_121",
      "iconId": "client_icon_121",
      "quantity": 5
    },
    {
      "itemTemplateId": 2,
      "itemCode": "ITEM_ICON_142",
      "iconId": "client_icon_142",
      "quantity": 3
    }
  ]
}
```

**Response:**

```json
{
  "message": "Đã thêm 2 item(s) vào inventory",
  "player_id": 1,
  "inventory": [ ... ],
  "updated_at": "2026-02-27T10:30:00Z"
}
```

#### 2. Test API Endpoint

Sử dụng Postman hoặc curl:

```bash
curl -X POST http://localhost:5000/api/player/1/inventory/add \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "items": [
      {
        "itemTemplateId": 1,
        "itemCode": "ITEM_ICON_121",
        "iconId": "client_icon_121",
        "quantity": 5
      }
    ]
  }'
```

---

### B. Setup Unity Client

#### 1. Thêm InventoryTestManager vào Scene

**Bước 1:** Tạo GameObject mới trong scene Main

- Tên: `InventoryTestManager`
- Vị trí: Cùng cấp với NetworkManager hoặc Canvas

**Bước 2:** Gắn script `InventoryTestManager`

- Add Component → Scripts → Inventory → InventoryTestManager

**Bước 3:** Cấu hình trong Inspector

```
InventoryTestManager (Script)
├── Test Items Configuration
│   ├── Test Items (List)
│   │   ├── Element 0
│   │   │   ├── Item Template Id: 1
│   │   │   ├── Item Code: "ITEM_ICON_121"
│   │   │   ├── Icon Id: "client_icon_121"
│   │   │   └── Quantity: 5
│   │   ├── Element 1
│   │   │   ├── Item Template Id: 2
│   │   │   ├── Item Code: "ITEM_ICON_142"
│   │   │   ├── Icon Id: "client_icon_142"
│   │   │   └── Quantity: 3
│   │   ├── Element 2
│   │   │   ├── Item Template Id: 3
│   │   │   ├── Item Code: "ITEM_ICON_152"
│   │   │   ├── Icon Id: "client_icon_152"
│   │   │   └── Quantity: 10
│   │   └── Element 3
│   │       ├── Item Template Id: 4
│   │       ├── Item Code: "ITEM_ICON_167"
│   │       ├── Icon Id: "client_icon_167"
│   │       └── Quantity: 1
│   └── ...
├── Settings
│   ├── Test Key: Q
│   └── Enable Debug Log: ✓
└── ...
```

#### 2. Kiểm tra NetworkInventory Component

Đảm bảo player prefab có:

- `NetworkObject` component
- `NetworkInventory` component (Max Slots = 20)

#### 3. Kiểm tra APIClient

Đảm bảo có GameObject `APIClient` trong scene hoặc DontDestroyOnLoad:

- Script: `APIClient`
- Base URL: `http://localhost:5000/api`

#### 4. Kiểm tra IconDatabase

Đảm bảo có sprites trong `Assets/Resources/ItemIcons/`:

- `client_icon_121.png` (Hồi Máu Nhỏ)
- `client_icon_142.png` (Hồi Mana Nhỏ)
- `client_icon_152.png` (Đá Quý Thường)
- `client_icon_167.png` (Kiếm Đồng)

---

## 🎮 Cách Sử Dụng

### Test Thêm Item Bằng Phím Q

1. **Chạy Game Server API**
   ```bash
   cd GameServerApi
   dotnet run
   ```

2. **Mở Unity và chạy Main scene**
   - Login với tài khoản đã có
   - Kết nối vào server (Host hoặc Client)

3. **Nhấn phím Q**
   - Tất cả test items sẽ được thêm vào inventory
   - Check Console để xem logs:
     ```
     [InventoryTestManager] Phím Q được nhấn - Bắt đầu thêm test items...
     [InventoryTestManager] Đang thêm 4 test items vào inventory...
     [NetworkInventory] AddItemWithDBSyncServerRpc: itemCode=ITEM_ICON_121, quantity=5
     [NetworkInventory] Đã thêm 5x ITEM_ICON_121 vào slot 0
     [APIClient] Items added to inventory successfully: {...}
     [NetworkInventory] ✅ Đã sync inventory với DB thành công!
     ```

4. **Mở Inventory UI (phím I hoặc nút)**
   - Các items sẽ hiển thị với icon và quantity

5. **Kiểm tra Database**
   ```sql
   SELECT 
       player_id,
       JSON_PRETTY(inventory) AS inventory_json
   FROM player_data
   WHERE player_id = 1;
   ```

---

## 🔍 Debugging

### Log Levels

**InventoryTestManager:**
- Khi nhấn Q: `[InventoryTestManager] Phím Q được nhấn`
- Khi gửi item: `[InventoryTestManager] Đã gửi request thêm: ITEM_ICON_121 x5`

**NetworkInventory:**
- Khi nhận ServerRpc: `[NetworkInventory] AddItemWithDBSyncServerRpc: itemCode=...`
- Khi thêm vào slot: `[NetworkInventory] Đã thêm 5x ITEM_ICON_121 vào slot 0`
- Khi sync DB thành công: `[NetworkInventory] ✅ Đã sync inventory với DB thành công!`
- Khi sync DB thất bại: `[NetworkInventory] ❌ Lỗi khi sync inventory với DB: ...`

**APIClient:**
- Khi gửi request: `[APIClient] POST /api/player/1/inventory/add`
- Khi thành công: `[APIClient] Items added to inventory successfully: ...`
- Khi thất bại: `[APIClient] Failed to add items to inventory: ...`

### Common Issues

#### 1. Phím Q không hoạt động

**Nguyên nhân:**
- Chưa kết nối network
- Không tìm thấy local player
- NetworkInventory component thiếu

**Giải pháp:**
- Kiểm tra `NetworkManager.Singleton.IsClient`
- Kiểm tra player prefab có `NetworkInventory` component
- Check Console logs

#### 2. Inventory không sync với DB

**Nguyên nhân:**
- API Server chưa chạy
- JWT Token hết hạn
- URL API sai

**Giải pháp:**
- Chạy API Server: `dotnet run` trong GameServerApi
- Kiểm tra `APIClient.baseURL` trong Inspector
- Re-login để lấy JWT token mới

#### 3. Icon không hiển thị

**Nguyên nhân:**
- Sprite không có trong `Resources/ItemIcons`
- Tên sprite không khớp với `iconId`
- IconDatabase không load được

**Giải pháp:**
- Kiểm tra sprites trong `Assets/Resources/ItemIcons/`
- Đảm bảo tên sprite = iconId (không có extension .png)
- Check IconDatabase logs khi Start

---

## 📊 Performance Considerations

### Network Bandwidth

- Mỗi lần thêm item: ~200 bytes (ServerRpc + NetworkVariable sync)
- Mỗi lần sync DB: ~500 bytes (HTTP request)

### Optimization Tips

1. **Batch thêm items**: Thay vì gọi ServerRpc nhiều lần, gom items lại gọi 1 lần
2. **Debounce DB sync**: Chỉ sync DB mỗi X giây hoặc khi có thay đổi lớn
3. **Cache inventory data**: Lưu inventory trong RAM, chỉ query DB khi cần

---

## 🚀 Mở Rộng

### 1. Thêm Item Từ Gameplay

Thay vì dùng phím Q, có thể trigger từ:

- **Loot từ quái vật:**
  ```csharp
  networkInventory.AddItemWithDBSyncServerRpc(
      itemTemplateId, itemCode, iconId, quantity
  );
  ```

- **Nhặt item từ ground:**
  ```csharp
  ItemPickup pickup = hit.GetComponent<ItemPickup>();
  networkInventory.AddItemWithDBSyncServerRpc(
      pickup.itemTemplateId, 
      pickup.itemCode, 
      pickup.iconId, 
      1
  );
  ```

- **Quest reward:**
  ```csharp
  foreach (var reward in quest.rewards) {
      networkInventory.AddItemWithDBSyncServerRpc(
          reward.itemTemplateId,
          reward.itemCode,
          reward.iconId,
          reward.quantity
      );
  }
  ```

### 2. Xóa Item

Thêm method tương tự:

```csharp
[ServerRpc(RequireOwnership = false)]
public void RemoveItemWithDBSyncServerRpc(int slotIndex, int quantity)
{
    // 1. Remove from NetworkVariable
    RemoveItemServerRpc(slotIndex, quantity);
    
    // 2. Sync with DB
    SyncInventoryRemoveWithDB(slotIndex, quantity);
}
```

### 3. Stackable Items

Cải tiến logic để items có thể stack:

```csharp
// Tìm slot đã có item đó
for (int i = 0; i < maxSlots; i++)
{
    if (currentData.slotData[i].itemID == itemID)
    {
        // Stack vào slot đã có
        currentData.slotData[i].quantity += quantity;
        break;
    }
}
```

---

## 📝 Summary

### ✅ Đã Implement

- ✅ API endpoint thêm item vào inventory
- ✅ SDK method trong APIClient
- ✅ ServerRpc với DB sync trong NetworkInventory
- ✅ InventoryTestManager với phím Q
- ✅ Sync inventory giữa Host - Client - Database

### 🎯 Kết Quả

- Nhấn phím Q → Items được thêm vào inventory
- Items được sync giữa tất cả clients
- Database được update real-time
- UI hiển thị items với icon và quantity

### 📚 Tài Liệu Liên Quan

- [INVENTORY_UI_GUIDE.md](./INVENTORY_UI_GUIDE.md) - Hướng dẫn setup UI inventory
- [INVENTORY_DATA_SETUP_GUIDE.md](./INVENTORY_DATA_SETUP_GUIDE.md) - Hướng dẫn setup dữ liệu
- [UNITY_NETCODE_CHECKLIST.md](./UNITY_NETCODE_CHECKLIST.md) - Checklist setup Unity Netcode

---

**Version:** 1.0
**Last Updated:** 2026-02-27
**Author:** GitHub Copilot
