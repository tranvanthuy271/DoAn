# 📦 Hướng Dẫn Hệ Thống Item Template Sync

## 🎯 Tổng Quan

Hệ thống đồng bộ item templates từ Database → API Server → Host → Clients

### Lợi Ích
- ✅ **Single Source of Truth**: Database là nguồn dữ liệu duy nhất
- ✅ **Không cần tạo ScriptableObject**: Items được quản lý hoàn toàn bởi DB
- ✅ **Hot-reload**: Sửa items trong DB, không cần build lại Unity
- ✅ **Auto-sync**: Host tự động broadcast cho tất cả Clients
- ✅ **Icon mapping**: Client load đúng icon theo iconId từ server

---

## 🔄 Luồng Hoạt Động

```
┌─────────────────────────────────────────────────────────────┐
│                    DATABASE                                  │
│  table: item_template                                        │
│  - id, code, name, description                              │
│  - category, item_type, stackable, max_stack               │
│  - rarity, icon_id, base_stat_json                         │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    API SERVER                                │
│  GET /api/item/templates                                    │
│  Response: {                                                │
│    "count": 4,                                              │
│    "item_templates": [...]                                  │
│  }                                                          │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    HOST (Unity)                             │
│  ItemTemplateManager.LoadItemTemplatesFromAPI()            │
│  - Gọi APIClient.GetItemTemplates()                        │
│  - Cache trong Dictionary (byId, byCode)                   │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    HOST (Unity)                             │
│  NetworkItemTemplateSync.SyncItemTemplates()               │
│  - Serialize item templates thành JSON                     │
│  - Chia nhỏ nếu JSON quá lớn (chunks)                     │
│  - Gọi ClientRpc gửi cho tất cả Clients                    │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    CLIENTS (Unity)                          │
│  NetworkItemTemplateSync.SyncItemTemplatesClientRpc()      │
│  - Nhận chunks từ Host                                      │
│  - Ghép chunks lại thành JSON đầy đủ                       │
│  - Deserialize và lưu vào ItemTemplateManager              │
└─────────────────────────────────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    ALL CLIENTS                              │
│  ItemTemplateManager.GetItemTemplate(id)                   │
│  - Lấy item data để hiển thị UI                            │
│  - Load icon theo iconId                                    │
│  - Hiển thị name, description, stats                       │
└─────────────────────────────────────────────────────────────┘
```

---

## 🛠️ Setup Chi Tiết

### A. Backend API Setup

#### 1. Model ItemTemplate

**File:** `GameServerApi/Models/ItemTemplate.cs`

```csharp
[Table("item_template")]
public class ItemTemplate
{
    [Key] public int Id { get; set; }
    [Required] public string Code { get; set; }
    [Required] public string Name { get; set; }
    public string? Description { get; set; }
    public int Category { get; set; }
    public int ItemType { get; set; }
    public bool Stackable { get; set; }
    public int MaxStack { get; set; }
    public int Rarity { get; set; }
    [Required] public string IconId { get; set; }
    public string? BaseStatJson { get; set; }
}
```

#### 2. DbContext

**File:** `GameServerApi/Data/GameDbContext.cs`

Đã thêm:
```csharp
public DbSet<ItemTemplate> ItemTemplates => Set<ItemTemplate>();
```

#### 3. ItemController

**File:** `GameServerApi/Controllers/ItemController.cs`

**Endpoint mới:**
```
GET /api/item/templates
GET /api/item/templates/{id}
GET /api/item/templates/code/{code}
```

**Test API:**
```bash
curl http://localhost:5000/api/item/templates
```

**Response:**
```json
{
  "count": 4,
  "item_templates": [
    {
      "id": 1,
      "code": "ITEM_ICON_121",
      "name": "Hồi Máu Nhỏ",
      "description": "Potion hồi máu cơ bản, hồi 50 HP",
      "category": 2,
      "item_type": 2,
      "stackable": true,
      "max_stack": 99,
      "rarity": 1,
      "icon_id": "client_icon_121",
      "base_stat_json": "{\"heal_amount\": 50}"
    },
    ...
  ]
}
```

---

### B. Unity Client Setup

#### 1. ItemTemplateManager

**File:** `Client/Assets/Scripts/Inventory/ItemTemplateManager.cs`

**Setup:**
1. Tạo GameObject mới tên `ItemTemplateManager` trong scene Main
2. Add Component: `ItemTemplateManager`
3. Cấu hình Inspector:
   ```
   ItemTemplateManager (Script)
   ├── Auto Load On Start: ✓
   └── Enable Debug Log: ✓
   ```

**API:**
```csharp
// Lấy item template theo ID
ItemTemplateDto template = ItemTemplateManager.Instance.GetItemTemplate(1);

// Lấy item template theo code
ItemTemplateDto template = ItemTemplateManager.Instance.GetItemTemplateByCode("ITEM_ICON_121");

// Lấy tất cả item templates
ItemTemplateDto[] allTemplates = ItemTemplateManager.Instance.GetAllItemTemplates();

// Kiểm tra đã load xong chưa
bool isLoaded = ItemTemplateManager.Instance.IsLoaded();
```

#### 2. NetworkItemTemplateSync

**File:** `Client/Assets/Scripts/Inventory/NetworkItemTemplateSync.cs`

**Setup:**
1. Tạo GameObject mới tên `NetworkItemTemplateSync`
2. Add Component: `NetworkObject`
3. Add Component: `NetworkItemTemplateSync`
4. Thêm vào Network Prefabs List trong NetworkManager
5. Cấu hình Inspector:
   ```
   NetworkItemTemplateSync (Script)
   ├── Auto Sync On Host Start: ✓
   └── Enable Debug Log: ✓
   ```

**Hoặc:**
- Gắn trực tiếp vào GameObject có NetworkObject (ví dụ: NetworkManager hoặc GameManager)

#### 3. APIClient Update

**File:** `Client/Assets/Scripts/API/APIClient.cs`

Đã thêm method:
```csharp
public void GetItemTemplates(
    System.Action<ItemTemplateDto[]> onSuccess = null, 
    System.Action<string> onError = null
)
```

---

### C. Database Setup

#### 1. Chạy SQL Script

**File:** `inventory_data_setup.sql`

```bash
mysql -u root -p gamedb < inventory_data_setup.sql
```

Hoặc import trong phpMyAdmin/MySQL Workbench.

#### 2. Verify Data

```sql
SELECT 
    id, code, name, icon_id, category, item_type, stackable, max_stack, rarity
FROM item_template
ORDER BY id;
```

**Expected Output:**
```
+----+---------------+------------------+------------------+----------+-----------+-----------+-----------+--------+
| id | code          | name             | icon_id          | category | item_type | stackable | max_stack | rarity |
+----+---------------+------------------+------------------+----------+-----------+-----------+-----------+--------+
|  1 | ITEM_ICON_121 | Hồi Máu Nhỏ      | client_icon_121 |        2 |         2 |         1 |        99 |      1 |
|  2 | ITEM_ICON_142 | Hồi Mana Nhỏ     | client_icon_142 |        2 |         2 |         1 |        99 |      1 |
|  3 | ITEM_ICON_152 | Đá Quý Thường    | client_icon_152 |        3 |         3 |         1 |        50 |      2 |
|  4 | ITEM_ICON_167 | Kiếm Đồng        | client_icon_167 |        1 |         1 |         0 |         1 |      1 |
+----+---------------+------------------+------------------+----------+-----------+-----------+-----------+--------+
```

---

## 🎮 Cách Sử Dụng

### Test Flow - Host Only

1. **Chạy API Server**
   ```bash
   cd GameServerApi
   dotnet run
   ```

2. **Mở Unity Main Scene**
   - Đảm bảo có `ItemTemplateManager` và `NetworkItemTemplateSync` trong scene
   - Click Play

3. **Kiểm tra Console Logs**
   ```
   [ItemTemplateManager] Bắt đầu load item templates từ API...
   [APIClient] Item templates loaded successfully
   [APIClient] Parsed 4 item templates
   [ItemTemplateManager] ✅ Đã load 4 item templates thành công!
     - Item 1: Hồi Máu Nhỏ (code=ITEM_ICON_121, iconId=client_icon_121)
     - Item 2: Hồi Mana Nhỏ (code=ITEM_ICON_142, iconId=client_icon_142)
     - Item 3: Đá Quý Thường (code=ITEM_ICON_152, iconId=client_icon_152)
     - Item 4: Kiếm Đồng (code=ITEM_ICON_167, iconId=client_icon_167)
   ```

4. **Click "Host" button**
   ```
   [NetworkItemTemplateSync] Host đang đợi ItemTemplateManager load...
   [NetworkItemTemplateSync] ✅ ItemTemplateManager đã load xong, bắt đầu sync...
   [NetworkItemTemplateSync] Host đang sync 4 item templates cho clients...
   ```

### Test Flow - Host + Client

**Machine 1 (Host):**
1. Chạy API Server
2. Mở Unity Main scene
3. Click Play → Click "Host"
4. Kiểm tra logs (như trên)

**Machine 2 (Client):**
1. Mở Unity Main scene
2. Click Play → Nhập Server IP → Click "Join"
3. Kiểm tra Console logs:
   ```
   [NetworkItemTemplateSync] Client nhận chunk 1/1 (size=850)
   [NetworkItemTemplateSync] ✅ Client nhận 4 item templates từ Host
   [ItemTemplateManager] ✅ Đã load 4 item templates thành công!
   ```

4. **Verify trong code:**
   ```csharp
   // Trong bất kỳ script nào của Client
   void Start()
   {
       StartCoroutine(WaitAndCheckItemTemplates());
   }

   IEnumerator WaitAndCheckItemTemplates()
   {
       // Đợi ItemTemplateManager load xong
       while (!ItemTemplateManager.Instance.IsLoaded())
       {
           yield return new WaitForSeconds(0.1f);
       }

       // Lấy item template
       var template = ItemTemplateManager.Instance.GetItemTemplate(1);
       Debug.Log($"✅ Item template: {template.name} - {template.icon_id}");
   }
   ```

---

## 🔧 Sử Dụng Trong Code

### Hiển thị Item Trong UI

```csharp
// File: InventorySlotUI.cs
public void SetSlot(InventorySlotDto slot)
{
    if (slot == null || slot.quantity <= 0)
    {
        Clear();
        return;
    }

    // Lấy item template từ ItemTemplateManager
    var template = ItemTemplateManager.Instance.GetItemTemplate(slot.itemTemplateId);
    
    if (template != null)
    {
        // Hiển thị tên
        if (nameText != null)
        {
            nameText.text = template.name;
        }

        // Hiển thị icon
        if (iconImage != null)
        {
            Sprite icon = IconDatabase.Instance.GetIcon(template.icon_id);
            if (icon != null)
            {
                iconImage.sprite = icon;
            }
        }

        // Hiển thị quantity
        if (quantityText != null)
        {
            quantityText.text = slot.quantity.ToString();
        }
    }
}
```

### Loot Item Từ Enemy

```csharp
// File: EnemyDropHandler.cs
void OnEnemyKilled()
{
    // Lấy item template theo code
    var template = ItemTemplateManager.Instance.GetItemTemplateByCode("ITEM_ICON_121");
    
    if (template != null)
    {
        // Thêm vào inventory
        networkInventory.AddItemWithDBSyncServerRpc(
            template.id,
            template.code,
            template.icon_id,
            1
        );
        
        Debug.Log($"Dropped: {template.name}");
    }
}
```

### Kiểm Tra Stats Của Item

```csharp
// File: ItemTooltipHandler.cs
void ShowTooltip(int itemTemplateId)
{
    var template = ItemTemplateManager.Instance.GetItemTemplate(itemTemplateId);
    
    if (template != null)
    {
        string tooltipText = $"<b>{template.name}</b>\n";
        tooltipText += $"{template.description}\n\n";
        
        // Parse base stats từ JSON
        if (!string.IsNullOrEmpty(template.base_stat_json))
        {
            var stats = JsonUtility.FromJson<Dictionary<string, object>>(template.base_stat_json);
            foreach (var stat in stats)
            {
                tooltipText += $"{stat.Key}: {stat.Value}\n";
            }
        }
        
        tooltipPanel.SetText(tooltipText);
    }
}
```

---

## 🐛 Troubleshooting

### Issue 1: "ItemTemplateManager.Instance is null"

**Nguyên nhân:**
- Chưa tạo GameObject ItemTemplateManager trong scene
- Script chưa được gắn

**Giải pháp:**
1. Tạo GameObject mới tên `ItemTemplateManager`
2. Add Component: `ItemTemplateManager`
3. Kiểm tra script có trong scene (Ctrl+F "ItemTemplateManager")

---

### Issue 2: "Item templates chưa được load"

**Nguyên nhân:**
- API Server chưa chạy
- Network connection failed
- Database chưa có data

**Giải pháp:**
1. Chạy API Server: `cd GameServerApi && dotnet run`
2. Kiểm tra API endpoint: `curl http://localhost:5000/api/item/templates`
3. Kiểm tra database có data: `SELECT * FROM item_template`
4. Check Console logs để xem lỗi cụ thể

---

### Issue 3: "Client không nhận được item templates"

**Nguyên nhân:**
- NetworkItemTemplateSync chưa được setup đúng
- Client join sau khi Host đã sync xong
- Network connection issue

**Giải pháp:**
1. Đảm bảo NetworkItemTemplateSync có NetworkObject component
2. Thêm vào Network Prefabs List trong NetworkManager
3. Client có thể gọi thủ công:
   ```csharp
   // Fallback: Client tự load từ API nếu chưa nhận được từ Host
   if (!ItemTemplateManager.Instance.IsLoaded())
   {
       ItemTemplateManager.Instance.LoadItemTemplatesFromAPI();
   }
   ```

---

### Issue 4: "Icon không hiển thị"

**Nguyên nhân:**
- iconId không khớp với tên sprite trong Resources/ItemIcons
- IconDatabase chưa load

**Giải pháp:**
1. Kiểm tra sprites trong `Assets/Resources/ItemIcons/`
2. Tên sprite phải = iconId (client_icon_121, không có .png)
3. Kiểm tra IconDatabase logs khi Start
4. Verify template có đúng iconId:
   ```csharp
   var template = ItemTemplateManager.Instance.GetItemTemplate(1);
   Debug.Log($"Icon ID: {template.icon_id}");
   ```

---

## 📊 Performance

### Memory Usage
- **Each item template**: ~200 bytes
- **100 item templates**: ~20 KB
- **1000 item templates**: ~200 KB

### Network Bandwidth
- **Initial sync (100 items)**: ~20 KB
- **Chunked if > 900 bytes**: Multiple ClientRpc calls
- **One-time cost**: Chỉ sync 1 lần khi connect

### Optimization Tips

1. **Lazy Loading**: Chỉ load khi cần
   ```csharp
   if (!ItemTemplateManager.Instance.IsLoaded())
   {
       ItemTemplateManager.Instance.LoadItemTemplatesFromAPI();
   }
   ```

2. **Cache Sprites**: Load icon 1 lần, reuse
   ```csharp
   // IconDatabase đã cache rồi
   Sprite icon = IconDatabase.Instance.GetIcon(iconId);
   ```

3. **Compress JSON**: Nếu có rất nhiều items (>1000), xem xét compress

---

## 🚀 Mở Rộng

### 1. Hot-reload Items Trong Runtime

```csharp
// Host force reload từ API
ItemTemplateManager.Instance.Reload();

// Host re-sync cho clients
NetworkItemTemplateSync sync = FindObjectOfType<NetworkItemTemplateSync>();
sync.SyncItemTemplates();
```

### 2. Thêm Item Categories/Filters

```csharp
// Lấy tất cả items theo category
var consumables = ItemTemplateManager.Instance.GetAllItemTemplates()
    .Where(t => t.category == 2) // 2 = Consumable
    .ToArray();
```

### 3. Localization

Thêm column `name_en`, `name_vi`, `description_en`, `description_vi` trong DB:

```sql
ALTER TABLE item_template 
ADD COLUMN name_en VARCHAR(100) AFTER name,
ADD COLUMN name_vi VARCHAR(100) AFTER name_en;
```

Unity code:
```csharp
string GetLocalizedName(ItemTemplateDto template)
{
    string lang = PlayerPrefs.GetString("Language", "vi");
    return lang == "en" ? template.name_en : template.name_vi;
}
```

---

## 📝 Summary

### ✅ Đã Implement

- ✅ Backend API: ItemTemplate model + ItemController
- ✅ Unity: ItemTemplateManager để cache item templates
- ✅ Unity: NetworkItemTemplateSync để sync Host → Clients
- ✅ APIClient: GetItemTemplates() method
- ✅ Auto-load khi Start và auto-sync khi Host start

### 🎯 Kết Quả

- Database là Single Source of Truth
- Host load từ API và cache
- Clients nhận từ Host qua Netcode
- Tất cả clients đều có đầy đủ item templates
- UI hiển thị đúng name, description, icon theo DB

### 📚 Files Mới

- ✅ `GameServerApi/Models/ItemTemplate.cs`
- ✅ `GameServerApi/Controllers/ItemController.cs`
- ✅ `Client/Assets/Scripts/Inventory/ItemTemplateManager.cs`
- ✅ `Client/Assets/Scripts/Inventory/NetworkItemTemplateSync.cs`
- ✅ `ITEM_TEMPLATE_SYNC_GUIDE.md` (file này)

---

**Version:** 1.0
**Last Updated:** 2026-02-27
**Author:** GitHub Copilot
