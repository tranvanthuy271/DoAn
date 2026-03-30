# Hướng Dẫn Config NPC Shop & Thợ Rèn

---

## Phần 1: Mở Panel Thợ Rèn Bằng Cách Nhấn Vào NPC

### 1.1 Cơ Chế Sau Fix

Trước đây `UpgradePanel` chỉ mở qua nút trong tab Character/Inventory.  
**Sau fix**: khi player click vào NPC có `npc_type = "blacksmith"`, `NpcMenuUI.Open()` nhận diện loại NPC và gọi `UpgradePanel.Instance.gameObject.SetActive(true)` trực tiếp thay vì mở shop panel thông thường.

### 1.2 Luồng Code

```
Player click NPC
  → NpcInteraction.OnPointerClick()         ← cần Camera có Physics2DRaycaster
  → NpcInteraction.InteractServerRpc()
  → NpcInteraction.FetchDialogueAndSend()   ← GET /api/npc/interact
  → NpcInteraction.OpenMenuClientRpc()
  → NpcMenuUI.Open(npcData, interaction)
      ├── npc_type == "blacksmith"
      │     └── UpgradePanel.Instance.gameObject.SetActive(true)   ← MỞ PANEL NÂNG ĐỒ
      └── npc_type khác
            └── ShowShopTab()  (shop thông thường)
```

### 1.3 Setup Unity — Blacksmith NPC Prefab

| Bước | Việc cần làm |
|---|---|
| 1 | Tạo NPC prefab tên `NPC_Blacksmith_Prefab` |
| 2 | Gắn `NetworkObject` component |
| 3 | Gắn `NpcInteraction` component |
| 4 | Sprite/Animator cho hình ảnh NPC thợ rèn |
| 5 | Gắn prefab vào `NpcServerManager.npcPrefabs[1]` trong Inspector |

> **Index quan trọng:**
> - `npcPrefabs[0]` = shop
> - **`npcPrefabs[1]` = blacksmith** ← gán NPC_Blacksmith_Prefab vào đây
> - `npcPrefabs[2]` = quest
> - `npcPrefabs[3]` = exchange
> - `npcPrefabs[4]` = event

### 1.4 Yêu cầu Camera

`NpcInteraction` dùng `IPointerClickHandler` — cần Camera trong scene có component `Physics2DRaycaster` (Add Component → Physics 2D Raycaster).

Nếu chưa có, fallback `OnMouseDown()` vẫn hoạt động nhưng yếu hơn (không check EventSystem).

### 1.5 Yêu cầu UpgradePanel trong Scene

`NpcMenuUI.Open()` gọi `UpgradePanel.Instance` — cần đảm bảo `UpgradePanel` singleton có trong scene (không cần active, chỉ cần tồn tại).

Để chọn item nâng cấp sau khi mở: người chơi kéo trang bị từ túi/slot vào UpgradePanel như bình thường.

---

## Phần 2: Config NPC Shop trong DB

### 2.1 Bảng liên quan

| Bảng | Mô tả |
|---|---|
| `npc_config` | Định nghĩa NPC (tên, loại, bản đồ, tọa độ) |
| `npc_shop_item` | Danh sách hàng hóa của từng NPC (theo `npc_id`) |
| `item_template` | Thông tin item (tên, icon, loại) |

### 2.2 Thêm NPC Mới vào Map

```sql
INSERT INTO npc_config (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id, is_active)
VALUES ('Tên NPC', 'shop', <map_id>, <pos_x>, <pos_y>, 'greet', 'npc_icon_id', 1);
```

`npc_type` hợp lệ: `shop` | `blacksmith` | `quest` | `exchange` | `event`

### 2.3 Thêm Hàng Hóa cho NPC Shop

```sql
-- Gán item cho NPC cụ thể qua npc_id
INSERT INTO npc_shop_item (npc_id, item_template_id, price_silver, price_gold, stock, required_level)
VALUES
  (<npc_id>, <item_id>, <giá_bạc>, <giá_vàng>, -1, <level_yêu_cầu>);
```

| Field | Mô tả |
|---|---|
| `npc_id` | FK đến `npc_config.npc_id` |
| `item_template_id` | FK đến `item_template.id` |
| `price_silver` | Giá bạc (0 nếu bán bằng vàng) |
| `price_gold` | Giá vàng (0 nếu bán bằng bạc) |
| `stock` | `-1` = vô hạn; `> 0` = số lượng giới hạn |
| `required_level` | Level tối thiểu của player |

### 2.4 Ví Dụ: 3 NPC Shop Bán Hàng Khác Nhau

```sql
-- NPC 1: Thương nhân bán tiêu hao (HP/MP potions)
INSERT INTO npc_config (npc_id, npc_name, npc_type, map_id, pos_x, pos_y, icon_id, is_active)
VALUES (10, 'Thương Nhân Vật Phẩm', 'shop', 1, 5.0, -1.0, 'npc_merchant_2', 1);

INSERT INTO npc_shop_item (npc_id, item_template_id, price_silver, price_gold, stock, required_level) VALUES
(10, 1,  100, 0, -1, 1),  -- HP Potion nhỏ
(10, 2,  300, 0, -1, 1),  -- HP Potion lớn
(10, 3,  200, 0, -1, 1);  -- MP Potion

-- NPC 2: Vũ khí sư bán vũ khí cấp cao
INSERT INTO npc_config (npc_id, npc_name, npc_type, map_id, pos_x, pos_y, icon_id, is_active)
VALUES (11, 'Vũ Khí Sư Kim Long', 'shop', 1, -3.0, -1.0, 'npc_weapon_smith', 1);

INSERT INTO npc_shop_item (npc_id, item_template_id, price_silver, price_gold, stock, required_level) VALUES
(11, 17, 0, 50,  -1, 10),  -- Kiếm Gió (cần level 10, 50 vàng)
(11, 18, 0, 120, -1, 20),  -- Đại Kiếm Phong (cần level 20)
(11, 19, 0, 300, -1, 30);  -- Thần Kiếm

-- NPC 3: Thợ rèn nâng đồ (không có shop_item — chỉ mở UpgradePanel)
INSERT INTO npc_config (npc_id, npc_name, npc_type, map_id, pos_x, pos_y, icon_id, is_active)
VALUES (12, 'Thợ Rèn Hắc Long', 'blacksmith', 1, 0.0, -1.5, 'npc_smith_1', 1);
-- Không cần npc_shop_item rows cho blacksmith
```

### 2.5 Cập Nhật / Xóa Hàng

```sql
-- Thay đổi giá
UPDATE npc_shop_item SET price_silver = 500 WHERE npc_id = 10 AND item_template_id = 1;

-- Ẩn hàng tạm (tăng required_level cao)
UPDATE npc_shop_item SET required_level = 999 WHERE id = <id>;

-- Xóa hàng vĩnh viễn
DELETE FROM npc_shop_item WHERE npc_id = 10 AND item_template_id = 3;
```

---

## Phần 3: Vị Trí NPC trong Scene

### 3.1 Cách `NpcServerManager` Spawn NPC

1. Khi Host start → `NpcServerManager.SpawnNpcsForMap(mapId)` gọi `GET /api/npc/list?mapId={mapId}`
2. API trả về mảng NPC có `npc_type` và `pos_x, pos_y`
3. Server Instantiate prefab từ `npcPrefabs[idx]` dựa trên type
4. `networkObject.Spawn()` → replicate tới clients
5. `NpcInteraction.InitOnServer(npcData)` — set data chỉ trên server

### 3.2 Lấy `pos_x, pos_y` từ Unity

Để biết tọa độ đặt NPC trong Unity:
1. Drag một dummy GameObject vào scene ở vị trí mong muốn
2. Đọc giá trị `Transform.position.x` và `Transform.position.y`
3. Điền vào `pos_x, pos_y` trong DB
4. Xóa dummy GameObject

---

## Phần 4: API Server cho NPC Shop

### 4.1 Endpoint liên quan

| Endpoint | Mô tả |
|---|---|
| `GET /api/npc/list?mapId={id}` | Lấy danh sách NPC trong map |
| `POST /api/npc/interact` | Lấy dialogue khi click NPC |
| `GET /api/npc/shop?npcId={id}&playerId={id}` | Lấy danh sách hàng bán (kèm `can_afford`, `meets_level`) |
| `POST /api/npc/shop/buy` | Mua hàng |

### 4.2 Kiểm Tra Shop Hoạt Động

```sql
-- Xem toàn bộ shop items của một NPC
SELECT nsi.id, nc.npc_name, it.name as item_name,
       nsi.price_silver, nsi.price_gold, nsi.stock, nsi.required_level
FROM npc_shop_item nsi
JOIN npc_config nc ON nc.npc_id = nsi.npc_id
JOIN item_template it ON it.id = nsi.item_template_id
WHERE nsi.npc_id = <npc_id>
ORDER BY nsi.required_level;
```

---

## Phần 5: Checklist Setup

### NPC Shop thông thường
- [ ] Insert vào `npc_config` với `npc_type = 'shop'`
- [ ] Insert items vào `npc_shop_item` với đúng `npc_id`
- [ ] Gán `NPC_Shop_Prefab` vào `NpcServerManager.npcPrefabs[0]`
- [ ] Prefab có `NetworkObject` + `NpcInteraction` component
- [ ] Camera trong scene có `Physics2DRaycaster`

### NPC Blacksmith (nâng đồ)
- [ ] Insert vào `npc_config` với `npc_type = 'blacksmith'`
- [ ] KHÔNG cần `npc_shop_item` rows
- [ ] Gán `NPC_Blacksmith_Prefab` vào `NpcServerManager.npcPrefabs[1]`
- [ ] `UpgradePanel` singleton tồn tại trong scene (có thể inactive)
- [ ] Nhấn vào NPC → `UpgradePanel` tự mở
