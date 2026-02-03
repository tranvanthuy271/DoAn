# Quick Start - Hệ Thống Inventory

## Tóm Tắt Nhanh

### 1. Tạo ItemData
```
Right-click > Create > Item > ItemData
- Đặt ItemID duy nhất
- Gán Icon, Drop Prefab
```

### 2. Tạo ItemPickup Prefab
```
GameObject với:
- NetworkObject
- ItemPickup script
- SpriteRenderer
- Collider2D (Is Trigger = true)
- Rigidbody2D
```

### 3. Gắn vào Player
```
Player GameObject:
- NetworkInventory component
- Max Slots: 20
```

### 4. Gắn vào Enemy
```
Enemy GameObject:
- EnemyItemDrop component
- Gán ItemPickup Prefab
- Thêm Drop Items (ItemData, Drop Rate, Quantity)
```

### 5. Tạo ItemManager (Optional)
```
GameObject trong scene:
- ItemManager component
- Items Resource Path: "Items"
```

## Sử Dụng Code

```csharp
// Lấy inventory
NetworkInventory inventory = player.GetComponent<NetworkInventory>();

// Thêm item
inventory.AddItem(itemID, quantity);

// Xóa item
inventory.RemoveItem(slotIndex, quantity);

// Sử dụng item
inventory.UseItem(slotIndex);

// Kiểm tra có item không
bool has = inventory.HasItem(itemID, quantity);
```

## Events

```csharp
inventory.OnItemAdded.AddListener((slotIndex, itemData, quantity) => {
    // Update UI
});

inventory.OnItemRemoved.AddListener((slotIndex, quantity) => {
    // Update UI
});
```

Xem file `INVENTORY_SYSTEM_GUIDE.md` để biết chi tiết đầy đủ!
