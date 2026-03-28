# Hướng Dẫn Config NPC Bán Hàng Trong Unity

> **Dự án:** DoAn — Unity (Netcode for GameObjects) + ASP.NET Core API + MySQL  
> **Phạm vi:** Setup toàn bộ NPC shop trong Unity — từ tạo prefab đến test thử mua hàng.

---

## Mục Lục

1. [Tổng quan luồng](#1-tổng-quan-luồng)
2. [Bước 1 — Tạo NPC Prefab](#2-bước-1--tạo-npc-prefab)
3. [Bước 2 — Đăng ký Prefab vào NetworkManager](#3-bước-2--đăng-ký-prefab-vào-networkmanager)
4. [Bước 3 — Tạo ShopItemRow Prefab](#4-bước-3--tạo-shopitemrow-prefab)
5. [Bước 4 — Tạo NpcMenuUI trên Canvas](#5-bước-4--tạo-npcmenuui-trên-canvas)
6. [Bước 5 — Tạo NpcServerManager trong Scene](#6-bước-5--tạo-npcservermanager-trong-scene)
7. [Bước 6 — Thêm Physics2DRaycaster vào Camera](#7-bước-6--thêm-physics2draycaster-vào-camera)
8. [Bước 7 — Cấu hình DB (npc_config + npc_shop_item)](#8-bước-7--cấu-hình-db-npc_config--npc_shop_item)
9. [Kiểm tra hoạt động từng bước](#9-kiểm-tra-hoạt-động-từng-bước)
10. [Lỗi phổ biến & cách sửa](#10-lỗi-phổ-biến--cách-sửa)

---

## 1. Tổng quan luồng

```
[Host/Server start]
  NpcServerManager → GET /api/npc/list?mapId=X
    → Instantiate NPC prefab
    → NetworkObject.Spawn()        ← NPC hiện lên tất cả client

[Client click NPC]
  NpcInteraction.OnPointerClick()
    → InteractServerRpc(npcNetworkId)
      [Server]
      → validate khoảng cách ≤ 3.5u × 1.5 (leniency)
      → GET /api/npc/interact → lấy dialogue_text
      → OpenMenuClientRpc(npcDataJson)          ← chỉ gửi về client đó
        [Client]
        → NpcMenuUI.Open(data, interaction)

[Client bấm "Mua hàng"]
  → NpcInteraction.LoadShopServerRpc()
    [Server]
    → GET /api/npc/shop?npcId=X&playerId=Y
    → ShowShopClientRpc(shopItemsJson)
      [Client]
      → NpcMenuUI.ShowShop(json)   ← render danh sách item

[Client bấm BtnBuy trên 1 dòng]
  → NpcInteraction.BuyItemServerRpc(itemId, 1)
    [Server]
    → POST /api/npc/shop/buy
    → BuyResultClientRpc(success, message, newGold)
      [Client]
      → NpcMenuUI.OnBuyResult(...)
```

> **Quy tắc vàng:** Client chỉ gửi RPC, không bao giờ tự gọi API. Server làm tất cả validation và gọi API.

---

## 2. Bước 1 — Tạo NPC Prefab

### 2.1 — Tạo GameObject cơ bản

1. Trong **Project window** → `Assets/Prefabs/NPC/` → chuột phải → **Create Empty**
2. Đặt tên ví dụ: `NPC_Shop_Prefab`
3. Hierarchy của prefab:

```
NPC_Shop_Prefab                  ← root
  SpriteRenderer                 ← hình NPC (sprite sheet / single sprite)
  BoxCollider2D
    isTrigger = false            ← collision vật lý bình thường
    Size: (1, 2)                 ← tuỳ kích thước sprite
  CircleCollider2D               ← vùng detect click — PHẢI là trigger
    isTrigger = true             ← BẮT BUỘC
    radius = 0.8                 ← vừa đủ để click vào thân NPC
  NetworkObject                  ← BẮT BUỘC — thiếu là Spawn() lỗi
  NpcInteraction                 ← script xử lý click + RPC
```

### 2.2 — Add component theo thứ tự

1. Select root GameObject của prefab
2. **Add Component → NetworkObject** (tìm "NetworkObject" trong search)
3. **Add Component → NpcInteraction** (file `Client/Assets/Scripts/NPC/NpcInteraction.cs`)
4. Thêm `CircleCollider2D` → tick **Is Trigger = true** → Radius = 0.8
5. Thêm `SpriteRenderer` → kéo sprite NPC vào `Sprite` field

> `NpcInteraction` không có field nào cần set trong Inspector — toàn bộ data được server inject lúc `InitOnServer()` sau khi Spawn.

### 2.3 — Tại sao cần 2 Collider?

| Collider | isTrigger | Mục đích |
|---|---|---|
| `BoxCollider2D` | false | NPC đứng chắn vật lý — player không đi xuyên qua |
| `CircleCollider2D` | **true** | Nhận sự kiện click/raycast từ `Physics2DRaycaster` |

Physics2DRaycaster chỉ phát hiện Collider có **isTrigger = true** khi raycasting. Nếu thiếu trigger collider → click vào NPC không có event nào.

### 2.4 — Tạo đủ 5 loại prefab

Mỗi `npc_type` cần 1 prefab riêng (có thể dùng chung script, khác nhau sprite / màu):

| Prefab | npc_type | npc_type_id |
|---|---|---|
| `NPC_Shop_Prefab` | `shop` | 0 |
| `NPC_Blacksmith_Prefab` | `blacksmith` | 1 |
| `NPC_Quest_Prefab` | `quest` | 2 |
| `NPC_Exchange_Prefab` | `exchange` | 3 |
| `NPC_Event_Prefab` | `event` | 4 |

> `npc_type_id` phải **khớp chính xác** với index trong mảng `npcPrefabs[]` của `NpcServerManager`. Bước này hay sai nhất.

---

## 3. Bước 2 — Đăng ký Prefab vào NetworkManager

**BẮT BUỘC** — thiếu bước này thì `NetworkObject.Spawn()` sẽ báo lỗi:  
`"NetworkObject is not registered in the NetworkPrefabs list"`

1. Trong scene, chọn GameObject có **NetworkManager** component
2. Trong Inspector → cuộn xuống phần **Network Prefabs**
3. Nhấn **+** → kéo **mỗi NPC prefab** vào (5 prefab = 5 lần nhấn +)

```
NetworkManager (Inspector)
  [...]
  Network Prefabs
    ├── NPC_Shop_Prefab
    ├── NPC_Blacksmith_Prefab
    ├── NPC_Quest_Prefab
    ├── NPC_Exchange_Prefab
    └── NPC_Event_Prefab
```

> **Ghi chú Unity 2022+:** Có thể dùng `NetworkPrefabsList` asset thay vì gắn trực tiếp vào NetworkManager. Cả hai đều được.

---

## 4. Bước 3 — Tạo ShopItemRow Prefab

Đây là template cho **1 dòng item** trong danh sách cửa hàng.

### 4.1 — Tạo prefab

1. Trong Canvas → tạo Empty GameObject → đặt tên `ShopItemRow`
2. Add Component → **Horizontal Layout Group** (để các element tự căn hàng ngang)
3. Kéo ra thành prefab: kéo `ShopItemRow` từ Hierarchy vào `Assets/Prefabs/UI/`

### 4.2 — Hierarchy của ShopItemRow (ô vuông grid)

```
ShopItemRow (root, 110×110)       ← Button + Image: click toàn ô = mua
  VerticalLayoutGroup
  ├── IconRow (HLG, h=60)
  │     └── ItemIcon              ← Image: icon item — 52×52, Preserve Aspect
  ├── PriceRow (HLG, h=18)
  │     ├── CoinIcon              ← Image: icon đồng tiền — 14×14
  │     └── Price                 ← TMP_Text: số giá — màu vàng #FFD700
  └── ItemName                    ← TMP_Text: tên item — h=22, Ellipsis
```

> **Component `ShopItemRowUI`** gắn trên root — kéo các child vào 5 fields trong Inspector.  
> **`btnBuy` = root Button** — click bất cứ đâu trên ô = trigger mua.  
> **`GridLayoutGroup`** phải đặt trên `Content` của `ShopScrollView` (KHÔNG phải trên prefab root).

### 4.3 — Config từng element

**ItemIcon (Image):**
```
Inspector:
  Width             = 52
  Height            = 52
  Image Type        = Simple
  Preserve Aspect   = true
  Raycast Target    = false
  LayoutElement → Preferred Width/Height = 52
```
> Sprite load runtime từ `Resources/ItemIcons/{icon_id}`. Tạo folder `Assets/Resources/ItemIcons/`.

**CoinIcon (Image):**
```
  Width             = 14
  Height            = 14
  Color             = #FFD700 (vàng)          ← placeholder cho đến khi có sprite coin
  Raycast Target    = false
  LayoutElement → Preferred Width/Height = 14
```
> Kéo sprite đồng tiền bạc/vàng vào đây nếu có. Script không đổi sprite này — bạn set thủ công.

**Price (TMP_Text):**
```
  Font Size   = 12
  Color       = vàng (#FFD700)
  Alignment   = MidlineLeft
  Raycast Target = false
```
> Script chỉ set số (ví dụ "500"), không kèm chữ "Vàng"/"Bạc" — CoinIcon làm role đó.

**ItemName (TMP_Text):**
```
  Font Size         = 11
  Color             = trắng
  Alignment         = Midline (center)
  Overflow          = Ellipsis
  Word Wrap         = false
  LayoutElement → Preferred Height = 22
```

**Root Button (toàn ô):**
```
  Normal Color    = trắng (opacity trên Image bg)
  Highlighted     = vàng nhạt (#FFEE88)
  Pressed         = nâu tối
  Disabled Color  = xám (#8C8C8C)  ← khi không đủ level/vàng
```

---

## 5. Bước 4 — Tạo NpcMenuUI trên Canvas

### 5.1 — Tạo cấu trúc Hierarchy

```
Canvas (Screen Space Overlay)          ← đã có EventSystem con
└── NpcMenuPanel                       ← Panel chính (NpcMenuUI gắn ở đây)
      Image (background mờ tối)
      VerticalLayoutGroup
      ├── HeaderRow (HLG)
      │     ├── NpcNameText            ← TMP_Text: tên NPC (flex width)
      │     └── BtnClose              ← Button "✕" (góc phải, w=30)
      ├── DialogueText                 ← TMP_Text: lời thoại
      ├── FeedbackText                 ← TMP_Text: thông báo kết quả (mặc định Inactive)
      └── ShopPanel                   ← Panel shop (mặc định Inactive)
            VerticalLayoutGroup
            ├── TabRow (HLG, h=36)
            │     ├── BtnTabShop      ← Button "Cửa hàng" / "Dược phẩm"
            │     └── BtnTabBag       ← Button "Túi"
            ├── ShopScrollView        ← ScrollRect (hiện khi tab Shop)
            │     Viewport
            │       Content          ← GridLayoutGroup → shopItemContainer
            └── BagPanel             ← Panel túi đồ (hiện khi tab Túi, mặc định Inactive)
                  [TODO: kết nối inventory system]
```

> **GridLayoutGroup trên `Content`:**  
> Cell Size = (110, 110) — Spacing = (8, 8) — Start Axis = Horizontal — Child Alignment = **Upper Center**  
> Constraint = Flexible → tự xuống hàng khi đủ 1 hàng, căn giữa.

### 5.2 — Add NpcMenuUI.cs và assign fields

1. Select `NpcMenuPanel` → Add Component → `NpcMenuUI`
2. Assign các fields:

```
Inspector — NpcMenuUI.cs:
  ── Panel chính ──
  mainPanel          → kéo NpcMenuPanel
  npcNameText        → kéo NpcNameText (TMP_Text)
  dialogueText       → kéo DialogueText (TMP_Text)
  btnClose           → kéo BtnClose (Button)

  ── Tabs (Cua hang | Tui) ──
  btnTabShop         → kéo BtnTabShop (Button)
  btnTabBag          → kéo BtnTabBag (Button)

  ── Shop Panel ──
  shopPanel          → kéo ShopPanel (GameObject)
  shopItemContainer  → kéo Content (Transform trong ShopScrollView)
  shopItemRowPrefab  → kéo ShopItemRow prefab từ Project

  ── Tui Panel ──
  bagPanel           → kéo BagPanel (GameObject)

  ── Icons ──
  defaultItemIcon    → kéo sprite fallback từ Project   (để trống nếu không cần)

  ── Thong bao ──
  feedbackText       → kéo FeedbackText (TMP_Text)   (để trống nếu không cần)
  feedbackDuration   = 2
```

### 5.3 — Trạng thái mặc định

```
NpcMenuPanel        → SetActive(false)   ← tắt — script tự mở khi cần
ShopPanel           → SetActive(false)   ← tắt — chỉ hiện khi bấm "Mua hàng"
FeedbackText        → SetActive(false)   ← tắt — hiện 2 giây rồi tự ẩn
```

> Nếu để `NpcMenuPanel` active lúc khởi động → panel sẽ hiện ra ngay khi vào game. Phải tắt trong Inspector.

---

## 6. Bước 5 — Tạo NpcServerManager trong Scene

### 6.1 — Setup

1. Tạo Empty GameObject → đặt tên `NpcServerManager`
2. **KHÔNG** để trong scene bình thường nếu scene có thể unload — đặt trong persistent scene hoặc cùng GameObject với `NetworkManager` (DontDestroyOnLoad)
3. Add Component → `NpcServerManager`

### 6.2 — Assign fields

```
Inspector — NpcServerManager.cs:
  ── API ──
  apiBase     = "http://localhost:5000"    ← đổi thành IP VPS khi deploy
  mapId       = 1                          ← SỐ CỤ THỂ — tránh race condition với MapManager

  ── NPC Prefabs ──
  Size        = 5
  Element 0   → NPC_Shop_Prefab
  Element 1   → NPC_Blacksmith_Prefab
  Element 2   → NPC_Quest_Prefab
  Element 3   → NPC_Exchange_Prefab
  Element 4   → NPC_Event_Prefab
```

> **QUAN TRỌNG:** `mapId` phải set số cụ thể (1, 2, 3...). Để 0 sẽ fallback lấy từ `MapManager` — nhưng MapManager có thể chưa fetch xong khi script chạy (race condition).

### 6.3 — Logic tự động

`NpcServerManager.Start()` kiểm tra:
- Nếu `NetworkManager.IsServer` → spawn ngay (Host đã start trước khi scene load)
- Nếu không → đăng ký `OnServerStarted` → spawn khi StartHost() xong

Bạn **không cần làm gì thêm** — script tự xử lý cả hai trường hợp.

---

## 7. Bước 6 — Thêm Physics2DRaycaster vào Camera

`IPointerClickHandler` trên NPC cần `Physics2DRaycaster` để nhận event click trên world object.

1. Select **Main Camera** trong Hierarchy
2. Add Component → tìm `Physics 2D Raycaster` → Add

```
Main Camera (Inspector)
  Camera
  AudioListener
  Physics 2D Raycaster    ← THÊM MỚI
    Event Mask: Everything (mặc định)
```

> Nếu thiếu bước này → `OnPointerClick()` trong `NpcInteraction` sẽ **không bao giờ được gọi**, không có lỗi console — rất khó debug.

---

## 8. Bước 7 — Cấu hình DB (npc_config + npc_shop_item)

### 8.1 — Thêm NPC vào bảng npc_config

```sql
-- Ví dụ: NPC shop ở Map1 (map_id = 1)
INSERT INTO npc_config
  (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id, is_active)
VALUES
  ('Lão Trưởng — Thương Nhân', 'shop', 1, 3.0, -1.0, 'greet', 'npc_merchant_1', 1);
```

**Giải thích cột `npc_type`:**

| npc_type | npc_type_id (index prefab) | Hiện nút trong NpcMenuUI |
|---|---|---|
| `shop` | 0 | BtnBuy + BtnSell |
| `blacksmith` | 1 | BtnBuy + BtnSell |
| `exchange` | 3 | BtnBuy + BtnSell |
| `quest` | 2 | Không hiện nút shop |
| `event` | 4 | Không hiện nút shop |

> Chỉ `npc_type = 'shop'`, `'blacksmith'`, `'exchange'` mới hiện nút "Mua hàng" trong UI (được kiểm ở `NpcMenuUI.Open()`).

### 8.2 — Thêm item vào bảng npc_shop_item

**Trước tiên cần có `item_template_id` hợp lệ** trong bảng `item_templates`.

```sql
-- Lấy danh sách item_template để biết id
SELECT id, name, type FROM item_templates LIMIT 20;

-- Thêm item vào shop của NPC (npc_id = 1 = Lão Trưởng)
INSERT INTO npc_shop_item
  (npc_id, item_template_id, price_silver, price_gold, stock, required_level, sort_order)
VALUES
  (1, 101, 500,   0,  -1, 1,  1),   -- Thuốc Hồi HP nhỏ: 500 bạc, vô hạn kho, level 1+
  (1, 102, 0,     5,  -1, 5,  2),   -- Thuốc Hồi HP vừa: 5 vàng,  vô hạn kho, level 5+
  (1, 103, 0,    20,  10, 10, 3),   -- Thuốc Đặc Biệt:   20 vàng, 10 cái tồn kho, level 10+
  (1, 201, 1000,  0,  -1, 1,  4);  -- Kiếm Sắt:         1000 bạc, vô hạn, level 1+
```

**Ý nghĩa từng cột:**

| Cột | Kiểu | Ý nghĩa |
|---|---|---|
| `npc_id` | int | FK đến `npc_config.npc_id` |
| `item_template_id` | int | FK đến `item_templates.id` |
| `price_silver` | int | Giá bạc — 0 nếu dùng vàng |
| `price_gold` | int | Giá vàng — 0 nếu dùng bạc |
| `stock` | int | Tồn kho. `-1` = vô hạn |
| `required_level` | int | Level tối thiểu để mua |
| `sort_order` | int | Thứ tự hiển thị trong danh sách |

> Không nên để cả `price_silver` và `price_gold` đều > 0 cho cùng 1 item — API ưu tiên vàng.

### 8.3 — Thêm dialogue cho NPC

```sql
INSERT INTO npc_dialogue
  (npc_id, dialogue_key, text_vi, next_key, action_type)
VALUES
  (1, 'greet', 'Chào ngươi! Ta có nhiều hàng hiếm đây. Muốn xem không?', NULL, 'open_shop');
```

Nếu không cần dialogue phức tạp thì để `dialogue_key = 'greet'` và `text_vi` là câu chào — NpcInteraction sẽ lấy text này gửi về client trước khi mở menu.

---

## 9. Kiểm tra hoạt động từng bước

### 9.1 — Bước test 1: NPC có spawn không?

1. Play game (Enter Play Mode) → host tự động start (StartHost)
2. Nhìn Console:
```
[NpcServerManager] Spawned 'Lão Trưởng — Thương Nhân' (shop) tại (3, -1)
[NpcServerManager] Đã spawn 1 NPC trên mapId=1.
```
3. NPC xuất hiện trong scene ✅

**Nếu không thấy log:** API trả lỗi. Kiểm tra Console xem có `[NpcServerManager] GET ... lỗi:` không.

### 9.2 — Bước test 2: Click NPC có mở menu không?

1. Di chuyển player đến gần NPC (trong vòng 3.5 units)
2. Click vào NPC
3. Console server: không có warning "quá xa"
4. Menu NPC xuất hiện với tên và dialogue ✅

**Nếu menu không mở:**
- Kiểm tra Camera có `Physics2DRaycaster` chưa
- Kiểm tra NPC có `CircleCollider2D` với `isTrigger = true` chưa
- Kiểm tra `NpcMenuUI.Instance` không null (có `NpcMenuUI` trong scene chưa)

### 9.3 — Bước test 3: Shop load được item không?

1. Bấm nút "Mua hàng"
2. Console server: gọi `GET /api/npc/shop?npcId=1&playerId=X`
3. Danh sách item hiện ra trong `ShopPanel` ✅

**Nếu shop trống:** Kiểm tra DB có `npc_shop_item` record cho `npc_id` đó chưa.

### 9.4 — Bước test 4: Mua item có thành công không?

1. Bấm `BtnBuy` trên 1 dòng item (nút phải active — player đủ level và vàng)
2. Console server: gọi `POST /api/npc/shop/buy`
3. FeedbackText hiện: "Mua thành công 1x Thuốc Hồi HP nhỏ." ✅
4. Shop refresh lại (stock giảm nếu không phải -1)

---

## 10. Lỗi phổ biến & cách sửa

### ❌ Lỗi: "NetworkObject is not registered in the NetworkPrefabs list"
**Nguyên nhân:** Quên đăng ký NPC prefab vào NetworkManager.  
**Sửa:** Xem [Bước 2 — Đăng ký Prefab vào NetworkManager](#3-bước-2--đăng-ký-prefab-vào-networkmanager).

---

### ❌ Lỗi: Click vào NPC không có gì xảy ra (không có log)
**Nguyên nhân:** Thiếu `Physics2DRaycaster` trên Camera HOẶC thiếu trigger collider trên NPC.  
**Sửa:**
1. Main Camera → Add Component → Physics 2D Raycaster
2. NPC prefab → thêm `CircleCollider2D` → tick `isTrigger = true`

---

### ❌ Lỗi: Menu mở nhưng Shop trống hoàn toàn
**Nguyên nhân 1:** `npc_shop_item` trong DB không có record cho `npc_id` này.  
**Nguyên nhân 2:** `NpcType` trong DB là `'quest'` — `NpcMenuUI.Open()` sẽ ẩn nút "Mua hàng" với quest NPC.  
**Nguyên nhân 3:** API `/api/npc/shop` trả lỗi — xem Console server log.

---

### ❌ Lỗi: NPC xuất hiện nhưng ở vị trí (0,0) thay vì đúng vị trí
**Nguyên nhân:** `pos_x`/`pos_y` trong bảng `npc_config` bằng 0.  
**Sửa:** Cập nhật DB:
```sql
UPDATE npc_config SET pos_x = 3.0, pos_y = -1.0 WHERE npc_id = 1;
```

---

### ❌ Lỗi: npcPrefabs index out of range
**Nguyên nhân:** `npc_type_id` trong DB cao hơn số phần tử trong mảng `npcPrefabs`.  
**Sửa:** Đảm bảo `npc_type_id` trong DB từ 0–4 và mảng `npcPrefabs` có đủ 5 phần tử (có thể duplicate prefab tạm nếu chưa có đủ).

---

### ❌ Lỗi: "Mua thất bại: Không đủ vàng" dù player giàu
**Nguyên nhân:** `price_gold` và `price_silver` trong `npc_shop_item` setup sai (đặt `price_gold = 500` thay vì `price_silver = 500`).  
**Kiểm tra:**
```sql
SELECT item_template_id, price_silver, price_gold FROM npc_shop_item WHERE npc_id = 1;
```

---

### ❌ Lỗi: NpcServerManager không spawn NPC (không log gì)
**Nguyên nhân:** `IsServer` = false lúc `Start()` chạy và `OnServerStarted` không bao giờ fire.  
**Kiểm tra:** NpcServerManager.cs dùng `MonoBehaviour.Start()` + subscribe `OnServerStarted`. Nếu NetworkManager Singleton bị null lúc Start() → cả hai nhánh đều bỏ qua.  
**Sửa:** Đảm bảo `NpcServerManager` GameObject trong scene **load sau** NetworkManager (đặt execution order hoặc đặt cùng scene có NetworkManager).

---

## Checklist Nhanh

### Unity
- [ ] NPC prefab có **NetworkObject** component
- [ ] NPC prefab có **CircleCollider2D** với `isTrigger = true`
- [ ] Mỗi NPC prefab đã **đăng ký trong NetworkManager → NetworkPrefabs**
- [ ] **5 prefab** gắn đúng thứ tự vào `NpcServerManager.npcPrefabs[]` (Element 0 = shop, ...)
- [ ] `NpcServerManager.mapId` set số cụ thể (không để 0 nếu không ở GameScene)
- [ ] `NpcMenuUI` trong Canvas với đầy đủ fields được assign
- [ ] `ShopItemRow` prefab có `ShopItemRowUI` component với fields: `itemIcon`, `coinIcon`, `itemName`, `price`, `btnBuy`
- [ ] `Content` của `ShopScrollView` có **GridLayoutGroup** (Cell Size 110×110, Child Alignment Upper Center)
- [ ] `BtnTabShop` và `BtnTabBag` assign đúng trong Inspector
- [ ] `BagPanel` assign vào field `bagPanel` (có thể để inactive, chỉ cần GameObject)
- [ ] `NpcMenuPanel` và `FeedbackText` mặc định **Inactive** trong scene
- [ ] **Physics2DRaycaster** add vào Main Camera

### Database
- [ ] `npc_config` có record với `npc_type = 'shop'` và `map_id` đúng
- [ ] `npc_shop_item` có ít nhất 1 item cho `npc_id` đó
- [ ] `item_template_id` trong `npc_shop_item` tồn tại trong bảng `item_templates`
- [ ] `stock = -1` nếu muốn vô hạn, số cụ thể nếu muốn giới hạn
- [ ] Không để cả `price_silver` và `price_gold` đều > 0 cùng lúc
