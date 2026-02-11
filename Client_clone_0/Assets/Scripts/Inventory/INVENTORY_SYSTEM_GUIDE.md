## Hệ Thống Túi Đồ (Inventory System) – DB + Server + Unity

Tài liệu này mô tả cách build **hệ thống túi đồ** có:

- **Item definition**: lưu ở **DB** (bảng `item_template`) + **config trong Unity** (ScriptableObject / asset).
- **Player inventory**: lưu trong **cột JSON `player_data.inventory`** (không tạo thêm bảng riêng).
- **Server**: load inventory của player khi **login** vào RAM.
- **Client (Unity)**: khi mở hành trang → **xin dữ liệu từ RAM server**, Unity chỉ **hiển thị** + gửi yêu cầu dùng item.

Hệ thống vẫn **server-authoritative** (server quyết định hết), Unity/Netcode chỉ là lớp hiển thị và tương tác.

---

## 1. Kiến Trúc Tổng Quan

### 1.1. Thành phần chính

- **Database (MySQL / MariaDB / v.v.)**
  - Bảng `item_template`: định nghĩa tất cả item trong game.
  - Bảng `player_data`: trong đó cột `inventory` (kiểu JSON/longtext) lưu **túi đồ** của từng người chơi.

- **Game Server**
  - Khi start: load toàn bộ `item_template` vào RAM.
  - Khi player login: đọc `player_data.inventory` từ DB, parse JSON vào RAM.
  - Cung cấp API / message:
    - Gửi **snapshot inventory** cho client.
    - Xử lý request: nhặt item, dùng item, di chuyển item, v.v.

- **Unity Client**
  - Nhận data inventory từ server.
  - Dùng **ItemTemplate ScriptableObject** / database để hiển thị icon, tên, màu rare, tooltip.
  - Gửi request hành động (UseItem, MoveItem, DropItem…) lên server.

---

## 2. Thiết Kế Database

### 2.1. Bảng `item_template` (Item Definition)

Đây là bảng **master data** cho tất cả item:

```sql
CREATE TABLE item_template (
    id INT PRIMARY KEY AUTO_INCREMENT,

    code VARCHAR(50) UNIQUE NOT NULL,         -- Ví dụ: KIM_FRAGMENT, HP_POTION_SMALL
    name VARCHAR(100) NOT NULL,
    description TEXT,

    category TINYINT NOT NULL,                -- 1=Equipment, 2=Consumable, 3=Material, 4=Gene, 5=Core
    item_type TINYINT NOT NULL,               -- Sub-type chi tiết hơn

    stackable BOOLEAN DEFAULT TRUE,
    max_stack INT DEFAULT 99,

    gender_limit TINYINT DEFAULT 0,           -- 0=All, 1=Male, 2=Female
    class_limit INT DEFAULT 0,                -- 0=All class

    level_required INT DEFAULT 0,

    rarity TINYINT DEFAULT 1,                 -- 1=Common → 5=Legend

    icon_path VARCHAR(255),                   -- Unity load theo string (Sprite / Addressable key)
    prefab_path VARCHAR(255),                 -- Nếu là equipment, dùng cho model/prefab

    base_stat_json JSON,                      -- Lưu stat linh hoạt (ATK, DEF, bonus, v.v.)

    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

**Lưu ý quan trọng:**

- **`code`** là khoá logic, dùng chung **DB ↔ Server ↔ Unity** (vd: `SWORD_BRONZE`, `HP_POTION_SMALL`).
- **`icon_path` / `prefab_path`** là string map sang asset trong Unity (Resources path, Addressables key, v.v.).
- **`base_stat_json`** cho phép item linh hoạt stats mà không phải sửa schema.

---

### 2.2. Bảng `player_data` và cột `inventory`

Trong DB hiện tại (file `gamedb (1).sql`), player được lưu trong bảng `player_data` với cột `inventory` dạng JSON/longtext:

```sql
CREATE TABLE `player_data` (
  `player_id` int(11) NOT NULL,
  `level` int(11) NOT NULL DEFAULT 1,
  `experience` int(11) NOT NULL DEFAULT 0,
  `gold` int(11) NOT NULL DEFAULT 0,
  `map_id` int(11) NOT NULL DEFAULT 0 COMMENT '0 = Main map',
  `position_x` float NOT NULL DEFAULT 0 COMMENT 'Vị trí X khi logout',
  `position_y` float NOT NULL DEFAULT 0 COMMENT 'Vị trí Y khi logout',
  `hp` int(11) NOT NULL DEFAULT 100,
  `max_hp` int(11) NOT NULL DEFAULT 100,
  `mp` int(11) NOT NULL DEFAULT 50,
  `max_mp` int(11) NOT NULL DEFAULT 50,
  `attack` int(11) NOT NULL DEFAULT 10,
  `element_type` varchar(10) NOT NULL COMMENT 'Fire, Water, Earth, Wood, Metal',
  `gene_tier` tinyint(4) NOT NULL DEFAULT 1,
  `is_hybrid` tinyint(1) NOT NULL DEFAULT 0,
  `secondary_element` varchar(10) DEFAULT NULL,
  `gender` varchar(10) NOT NULL DEFAULT 'Male',
  `character_name` varchar(50) NOT NULL DEFAULT '',
  `equipment` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: Trang bị đang mặc',
  `skills` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: Skills đã học',
  `inventory` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: Túi đồ',
  `potential_stats` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_bin DEFAULT NULL COMMENT 'JSON: Chỉ số tiềm năng',
  `updated_at` datetime NOT NULL DEFAULT current_timestamp() ON UPDATE current_timestamp()
);
```

- Trường `inventory` sẽ chứa JSON dạng danh sách slot, ví dụ gợi ý:
  ```json
  [
    { "slotIndex": 0, "itemCode": "HP_POTION_SMALL", "quantity": 5, "isEquipped": false },
    { "slotIndex": 1, "itemCode": "SWORD_BRONZE", "quantity": 1, "isEquipped": true }
  ]
  ```
- Khi cần thay đổi thiết kế inventory, chỉ cần thay đổi **format JSON**, không phải sửa schema.

---

## 3. Luồng Xử Lý Trên Server

### 3.1. Khi server khởi động

1. Query tất cả `item_template`:
   - `SELECT * FROM item_template`
2. Map vào RAM:
   - Ví dụ: `Dictionary<int, ItemTemplateData>` (theo `id`)  
   - Hoặc: `Dictionary<string, ItemTemplateData>` (theo `code`)

Mục tiêu:

- Tra cứu nhanh theo `item_template_id` hoặc `code`.
- Không query DB cho mỗi lần xem thông tin item.

### 3.2. Khi player login

1. **Xác thực tài khoản** → xác định `player_id`.
2. **Load inventory từ DB** (đọc từ bảng `player_data`):
   ```sql
   SELECT inventory FROM player_data WHERE player_id = ?;
   ```
3. Parse JSON `inventory` thành object `PlayerInventory` trong RAM, ví dụ:
   - `Dictionary<int, InventorySlot>` với key = `slot_index`.
   - Mỗi `InventorySlot` chứa:
     - `itemTemplateId` hoặc `itemCode`
     - `quantity`
     - `isEquipped`
4. Gửi **snapshot inventory** cho client (gói message hoặc RPC).

### 3.3. Khi player mở hành trang (Inventory UI)

- **Client**:
  - Nếu chưa có data hoặc muốn refresh → gửi request `GetInventory`.
- **Server**:
  - Lấy data từ `PlayerInventory` đang giữ trong RAM.
  - Build response đơn giản:
    - `slotIndex`
    - `itemTemplateCode` (hoặc `itemTemplateId`)
    - `quantity`
    - `isEquipped`
  - **Không cần query DB lại** (trừ trường hợp sync / reload).

---

## 4. Cấu Trúc Dữ Liệu Trong Unity

### 4.1. ScriptableObject: `ItemTemplate`

Unity dùng ScriptableObject để config hiển thị + logic client cho item.

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ItemTemplate", menuName = "Game/Item Template")]
public class ItemTemplate : ScriptableObject
{
    [Header("Identity")]
    public int id;                 // Mirror với DB (nếu cần)
    public string code;            // Phải trùng với item_template.code

    [Header("Display")]
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;

    [Header("Type & Rules")]
    public int category;           // 1=Equipment, 2=Consumable, ...
    public int itemType;
    public bool stackable = true;
    public int maxStack = 99;

    public int genderLimit;
    public int classLimit;
    public int levelRequired;
    public int rarity;             // 1-5

    [Header("Prefab / Visual")]
    public GameObject prefab;      // Dùng khi equip / hiển thị ngoài map
}
```

### 4.2. `ItemDatabase` trong Unity

Dùng để tra cứu `ItemTemplate` theo `code`:

```csharp
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Game/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemTemplate> items;

    private Dictionary<string, ItemTemplate> _byCode;

    public void Init()
    {
        if (_byCode != null) return;

        _byCode = new Dictionary<string, ItemTemplate>();
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.code))
            {
                _byCode[item.code] = item;
            }
        }
    }

    public ItemTemplate GetByCode(string code)
    {
        if (_byCode == null) Init();
        _byCode.TryGetValue(code, out var item);
        return item;
    }
}
```

- Tạo một asset `ItemDatabase` trong Unity, kéo tất cả `ItemTemplate` vào list `items`.
- Ở `GameManager`/`Bootstrap` scene, gọi `itemDatabase.Init()` khi game start.

---

## 5. Cấu Trúc UI Inventory Trong Unity

### 5.1. Bố cục UI

- **InventoryPanel**: Panel tổng chứa grid các slot.
- **SlotPrefab**:
  - `Image` icon item.
  - `Text` hoặc `TMP_Text` hiển thị số lượng.
  - `Button` hoặc xử lý click/hover (dùng UnityEvent hoặc event system).
- **TooltipPanel**:
  - Tên item.
  - Mô tả.
  - Màu/viền theo `rarity`.
  - Stats cơ bản (nếu muốn).

### 5.2. Data nhận từ server

Client nên có struct đơn giản tương ứng với RAM server:

```csharp
public struct InventorySlotData
{
    public int slotIndex;
    public string itemCode;
    public int quantity;
    public bool isEquipped;
}
```

Server gửi về `List<InventorySlotData>` hoặc mảng.

### 5.3. Script quản lý UI: `InventoryUI`

Ví dụ cơ bản:

```csharp
using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public GameObject inventoryPanel;
    public Transform slotContainer;
    public GameObject slotPrefab;
    public ItemDatabase itemDatabase;

    private InventorySlotUI[] _slotUIs;
    private int _maxSlots;

    public void Init(int maxSlots)
    {
        _maxSlots = maxSlots;
        _slotUIs = new InventorySlotUI[_maxSlots];

        for (int i = 0; i < _maxSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, slotContainer);
            var slotUI = slotObj.GetComponent<InventorySlotUI>();
            slotUI.Setup(i);
            _slotUIs[i] = slotUI;
        }
    }

    public void Refresh(InventorySlotData[] slots)
    {
        // Clear toàn bộ
        for (int i = 0; i < _slotUIs.Length; i++)
        {
            _slotUIs[i].Clear();
        }

        // Fill theo data server
        foreach (var slot in slots)
        {
            if (slot.slotIndex < 0 || slot.slotIndex >= _slotUIs.Length)
                continue;

            ItemTemplate template = itemDatabase.GetByCode(slot.itemCode);
            _slotUIs[slot.slotIndex].SetItem(template, slot.quantity, slot.isEquipped);
        }
    }

    public void Toggle()
    {
        inventoryPanel.SetActive(!inventoryPanel.activeSelf);
    }
}
```

`InventorySlotUI` xử lý icon, số lượng, highlight:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image iconImage;
    public TMP_Text quantityText;
    public GameObject equippedMark;

    private int _slotIndex;
    private ItemTemplate _currentItem;

    public void Setup(int slotIndex)
    {
        _slotIndex = slotIndex;
        Clear();
    }

    public void Clear()
    {
        _currentItem = null;
        iconImage.enabled = false;
        quantityText.text = "";
        equippedMark.SetActive(false);
    }

    public void SetItem(ItemTemplate item, int quantity, bool isEquipped)
    {
        _currentItem = item;

        if (item == null)
        {
            Clear();
            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = item.icon;

        if (item.stackable && quantity > 1)
        {
            quantityText.text = quantity.ToString();
        }
        else
        {
            quantityText.text = "";
        }

        equippedMark.SetActive(isEquipped);
    }

    // Hàm này gọi từ OnClick của Button
    public void OnClick()
    {
        if (_currentItem == null) return;

        // TODO: Gửi request lên server: UseItem / EquipItem / MoveItem...
        Debug.Log($"Clicked slot {_slotIndex} with item {_currentItem.code}");
    }
}
```

---

## 6. Tích Hợp Với Netcode / Network Layer

Tuỳ kiến trúc, bạn có thể:

- Dùng **Unity Netcode for GameObjects** với `ServerRpc` / `ClientRpc`.
- Hoặc dùng custom socket (Mirror, FishNet, Photon, v.v.).

### 6.1. Luồng đề xuất

1. **Login thành công**:
   - Server:
     - Load `player_data.inventory` từ DB → parse JSON → RAM.
     - Gửi gói `InventorySnapshot` cho client.
   - Client:
     - Nhận gói → parse thành `InventorySlotData[]`.
     - Gọi `InventoryUI.Refresh(...)`.

2. **Player nhặt item**:
   - Client gửi request `PickupItem(itemObjectId)` → Server.
   - Server:
     - Validate (khoảng cách, ownership…).
     - Cập nhật `PlayerInventory` trong RAM.
     - Serialize lại JSON và cập nhật cột `player_data.inventory` trong DB.

3. **Player sử dụng item**:
   - Client:
     - Bấm UI → gửi request `UseItem(slotIndex)` lên server.
   - Server:
     - Validate (có đủ quantity, điều kiện level/class, cooldown…).
     - Áp dụng hiệu ứng (heal, buff, spawn, v.v.).
     - Giảm quantity hoặc xoá slot.
     - Serialize lại JSON inventory và cập nhật DB (cột `player_data.inventory`).
     - Gửi update inventory cho client.

4. **Player sắp xếp / di chuyển slot**:
   - Client gửi request `MoveItem(srcSlot, dstSlot)`.
   - Server xử lý swap/merge → update RAM + DB → gửi kết quả về client.

---

## 7. Quy Ước Mapping DB ↔ Unity

Để tránh lệch dữ liệu giữa DB và Unity:

- **`item_template.code` trong DB** phải **trùng 100%** với `ItemTemplate.code` trong Unity.
- `icon_path` / `prefab_path` nên có format rõ ràng:
  - Ví dụ: `Icons/Items/sword_bronze` nếu dùng `Resources.Load<Sprite>()`.
  - Hoặc `"sword_bronze_icon"` nếu dùng Addressables key.
- Có thể viết **tool import/export**:
  - Export `item_template` ra CSV/JSON.
  - Unity Editor Script đọc CSV/JSON, tự tạo/cập nhật `ItemTemplate` ScriptableObjects.

---

## 8. Checklist Triển Khai

- **Database**
  - [ ] Tạo bảng `item_template`.
  - [ ] Tạo bảng `player_inventory`.
  - [ ] Seed một số item mẫu.

- **Server**
  - [ ] Khi start: load toàn bộ `item_template` vào RAM.
  - [ ] Khi player login: load `player_inventory` vào RAM.
  - [ ] Cài đặt API / RPC:
    - [ ] `InventorySnapshot` (gửi full inventory).
    - [ ] `GetInventory` (client xin lại khi cần).
    - [ ] `PickupItem`, `UseItem`, `MoveItem`, `DropItem` (tuỳ game).
  - [ ] Đồng bộ DB (insert/update) mỗi khi inventory thay đổi.

- **Unity Client**
  - [ ] Tạo `ItemTemplate` ScriptableObjects.
  - [ ] Tạo `ItemDatabase` + init khi game start.
  - [ ] Tạo UI: Panel inventory + slot prefab + tooltip.
  - [ ] Implement `InventoryUI` + `InventorySlotUI`.
  - [ ] Tích hợp network: nhận gói từ server → `Refresh`.
  - [ ] Từ `InventorySlotUI.OnClick` gửi request thích hợp (Use/Equip/Move…).

---

## 9. Gợi Ý Mở Rộng

- **Equipment System**:
  - Tách thêm bảng `player_equipment` hoặc field riêng cho slot trang bị.
  - Unity tạo UI Equipment riêng (helmet, armor, weapon…).

- **Item Effect System**:
  - Với mỗi item type (consumable, buff, scroll…), định nghĩa `effect_code`.
  - Server có `ItemEffectHandler` map `effect_code` → logic xử lý.

- **Trading / Shop**:
  - Thêm hệ thống shop / trade, nhưng vẫn dùng chung `item_template` + `player_inventory`.

- **Log / Anti-cheat**:
  - Log mọi thay đổi item (nhặt, bán, trade, v.v.) ở server để debug và chống cheat.

---

Tài liệu này tập trung mô tả **luồng Inventory kiểu MMO**: item & inventory lưu **DB**, server load vào RAM, client chỉ hiển thị và gửi request. Dựa trên đây, bạn có thể nối với hệ thống Netcode hiện tại (NetworkObject, ServerRpc/ClientRpc) nhưng vẫn giữ nguyên nguyên tắc: **mọi thay đổi túi đồ đều do server quyết định, DB là nguồn dữ liệu gốc**.

