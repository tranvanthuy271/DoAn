# Hướng dẫn cấu hình hệ thống Nâng cấp Gene

## Tổng quan

| Thành phần | Mô tả |
|---|---|
| **Chi phí** | Vàng (`gold`) + item vật liệu |
| **Điều kiện** | `gene_exp` đủ ngưỡng config |
| **Khi thành công** | `gene_tier` tăng; chỉ số nhân vật tăng; mở khoá skill mới |
| **Khi thất bại** | `gene_exp` reset về 0; trừ vàng + item như thường |
| **API** | `GET /api/gene/config` · `POST /api/gene/upgrade` |

---

## 1. Migration SQL bắt buộc

Chạy lần đầu trên database trước khi khởi động server:

```sql
-- Thêm cột gene_tier_required vào skill_template
ALTER TABLE skill_template
  ADD COLUMN gene_tier_required INT NOT NULL DEFAULT 0;
```

Sau đó chạy server lại để EF Core nhận cột mới.

---

## 2. Cấu hình item ID cho từng tier

Bảng `gene_upgrade_config` trong DB:

| Cột DB | Ý nghĩa | Ghi chú |
|---|---|---|
| `tier_from` | Gene tier hiện tại (1-4) | PK |
| `element_type` | 'Fire' / 'Water' / 'Earth' / 'Metal' / 'Wood' | PK |
| `gene_exp_required` | Gene exp cần tích luỹ trước khi nâng | |
| `silver_cost` | **Vàng** tiêu hao (server đọc là gold) | Cột DB tên là silver_cost |
| `stone_id` | **ID item vật liệu** cần dùng | Đây là `item_template.id` |
| `stone_min` | Số item tối thiểu để thực hiện nâng | |
| `stone_needed` | Số item để đạt tỉ lệ thành công tối đa | |
| `base_success_rate` | Tỉ lệ thành công khi dùng đủ `stone_needed` item | 0.0 - 1.0 |

### Cách đổi item ID:

```sql
-- Đặt item ID = 42 cho tất cả gene Fire
UPDATE gene_upgrade_config
SET stone_id = 42
WHERE element_type = 'Fire';

-- Đặt item ID 55 riêng cho tier 3 → 4 của Water
UPDATE gene_upgrade_config
SET stone_id = 55
WHERE element_type = 'Water' AND tier_from = 3;
```

### Xem config hiện tại:

```sql
SELECT tier_from, element_type, silver_cost AS gold_cost,
       stone_id AS item_id, stone_min, stone_needed, base_success_rate
FROM gene_upgrade_config
ORDER BY element_type, tier_from;
```

---

## 3. Cấu hình tỉ lệ thành công

Công thức server dùng:

```
successRate = base_success_rate × min(itemCount / stone_needed, 1.0)
```

- `itemCount` do client gửi lên (bị clamp trong khoảng `stone_min` → `stone_needed`)
- Ví dụ: `base_success_rate=0.8`, `stone_needed=5`, dùng 3 item → `rate = 0.8 × 0.6 = 48%`

---

## 4. Cấu hình chỉ số tăng khi lên tier

Chỉnh trong file **`GameServerApi/Controllers/GeneController.cs`**, mục `TierStatBoost`:

```csharp
private static readonly Dictionary<int, (int Hp, int Mp, int Atk, int Def)> TierStatBoost = new()
{
    [2] = (200,  50,  20,  10),   // tier 1 → 2
    [3] = (400, 100,  40,  20),   // tier 2 → 3
    [4] = (800, 200,  80,  40),   // tier 3 → 4
    [5] = (1500, 400, 150,  80),  // tier 4 → 5
};
```

- Key = **tier mới** sau khi nâng cấp thành công
- Các chỉ số được cộng vào `max_hp`, `max_mp`, `attack`, `defense` của nhân vật
- Khi thành công, `hp` và `mp` cũng được hồi đầy theo `max_hp`/`max_mp` mới

---

## 5. Cấu hình skill mở khoá theo gene tier

### Bước 1 — Chạy SQL cập nhật `gene_tier_required` trên skill

```sql
-- Xem danh sách skill hiện có
SELECT id, skill_code, skill_name, element_type, gene_tier_required
FROM skill_template;

-- Ví dụ: mở khoá FIRE_WAVE ở gene tier 2
UPDATE skill_template SET gene_tier_required = 2 WHERE skill_code = 'FIRE_WAVE';

-- Mở khoá WATER_SHIELD ở gene tier 3
UPDATE skill_template SET gene_tier_required = 3 WHERE skill_code = 'WATER_SHIELD';

-- Skill với element_type = NULL sẽ mở cho tất cả loại gene (dùng khi muốn skill chung)
UPDATE skill_template SET gene_tier_required = 2, element_type = NULL WHERE skill_code = 'COMMON_BUFF';
```

### Bước 2 — Logic server khi mở khoá

Server tự động tìm skill thoả:
1. `gene_tier_required == newTier` (tier vừa đạt được)
2. `element_type == player.element_type` **hoặc** `element_type IS NULL`
3. Chưa có trong `skills_json` của player

Và thêm vào skills_json dạng: `{ "skill_id": X, "current_level": 0 }`

---

## 6. API Reference

### GET `/api/gene/config`

**Query params:**
- `elementType` — ví dụ `Fire`
- `tier` — tier hiện tại của player (1-4)

**Response:**
```json
{
  "tierFrom": 1,
  "tierTo": 2,
  "elementType": "Fire",
  "geneExpRequired": 1000,
  "goldCost": 5000,
  "itemId": 7,
  "itemName": "Tinh thạch Hỏa",
  "itemIcon": 301,
  "itemsMin": 2,
  "itemsNeeded": 5,
  "baseSuccessRate": 0.8,
  "statBonus": { "hp": 200, "mp": 50, "attack": 20, "defense": 10 },
  "skillsToUnlock": [
    { "skillId": 3, "skillName": "Hỏa Cầu Cường Hóa", "elementType": "Fire", "iconId": "skill_003" }
  ]
}
```

### POST `/api/gene/upgrade`

**Body:**
```json
{
  "playerId": 1,
  "itemCount": 3
}
```

- `itemCount`: số item muốn dùng (bị clamp tự động về `stone_min`..`stone_needed`)

**Response khi thành công:**
```json
{
  "success": true,
  "newGeneTier": 2,
  "newGeneExp": 0,
  "gold": 45000,
  "message": "Nâng cấp gene thành công! Tier 1 → 2",
  "statBonus": { "hp": 200, "mp": 50, "attack": 20, "defense": 10 },
  "newStats": { "maxHp": 1200, "maxMp": 250, "attack": 120, "defense": 60 },
  "newlyUnlockedSkills": [
    { "skillId": 3, "skillName": "Hỏa Cầu Cường Hóa", "iconId": "skill_003" }
  ],
  "updatedInventory": [...]
}
```

**Response khi thất bại:**
```json
{
  "success": false,
  "newGeneTier": 1,
  "newGeneExp": 0,
  "gold": 45000,
  "message": "Nâng cấp thất bại. Gene exp đã reset.",
  "statBonus": null,
  "newStats": null,
  "newlyUnlockedSkills": [],
  "updatedInventory": [...]
}
```

---

## 7. Tích hợp Unity — GeneUpgradePanel

Script đã viết sẵn: `Assets/Scripts/Inventory/UI/GeneUpgradePanel.cs`  
Gọi mở panel từ bất kỳ chỗ nào:
```csharp
GeneUpgradePanel.Instance.Open();
```

---

### Bước 1 — Tạo Hierarchy trong Unity

Trong **Hierarchy**, tạo cấu trúc GameObject như sau (có thể dùng chuột phải → UI):

```
Canvas (hoặc thêm vào Canvas có sẵn)
└── GeneUpgradePanel  [Panel / Image background]
    ├── LoadingOverlay          [Image màu đen mờ, che toàn bộ khi loading]
    │
    ├── TierDisplayText         [TextMeshPro — "Gene Tier 1 → 2"]
    ├── ElementIcon             [Image — icon nguyên tố]
    │
    ├── GeneExpBar              [Slider — interactable = OFF]
    │   └── GeneExpText         [TextMeshPro — "1000 / 5000 exp"]
    │
    ├── CostSection
    │   ├── GoldCostText        [TextMeshPro — "5,000 vàng"]
    │   ├── ItemIcon            [Image — icon vật liệu]
    │   └── ItemCostText        [TextMeshPro — "x2 Tinh thạch Hỏa"]
    │
    ├── SuccessRateSection
    │   ├── SuccessRateText     [TextMeshPro — "Tỉ lệ: 48%"]
    │   ├── ItemCountSlider     [Slider — kéo để chọn số item]
    │   └── ItemCountText       [TextMeshPro — "3 item"]
    │
    ├── StatBonusSection
    │   ├── StatHpText          [TextMeshPro — "+200 HP"]
    │   ├── StatMpText          [TextMeshPro — "+50 MP"]
    │   ├── StatAtkText         [TextMeshPro — "+20 ATK"]
    │   └── StatDefText         [TextMeshPro — "+10 DEF"]
    │
    ├── SkillsSection
    │   └── SkillsContainer     [Empty GameObject — parent chứa skill rows]
    │       └── (SkillRow được sinh tự động lúc runtime)
    │
    ├── StatusText              [TextMeshPro — hiện thông báo lỗi / thành công]
    │
    ├── UpgradeButton           [Button]
    └── CloseButton             [Button]
```

---

### Bước 2 — Thêm component GeneUpgradePanel

1. Chọn GameObject **GeneUpgradePanel** trong Hierarchy
2. **Add Component** → tìm `GeneUpgradePanel`
3. Đặt `GeneUpgradePanel` là **SetActive = false** lúc mặc định (panel ẩn khi mở game)

---

### Bước 3 — Kéo references vào Inspector

Sau khi thêm component, Inspector sẽ hiện các slot. Kéo đúng theo thứ tự:

| Slot Inspector | GameObject cần kéo vào |
|---|---|
| **Tier Display Text** | `TierDisplayText` |
| **Element Icon** | `ElementIcon` (Image) |
| **Gene Exp Bar** | `GeneExpBar` (Slider) |
| **Gene Exp Text** | `GeneExpText` (TMP_Text) |
| **Gold Cost Text** | `GoldCostText` |
| **Item Cost Text** | `ItemCostText` |
| **Item Icon** | `ItemIcon` (Image) |
| **Success Rate Text** | `SuccessRateText` |
| **Item Count Slider** | `ItemCountSlider` (Slider) |
| **Item Count Text** | `ItemCountText` |
| **Stat Hp Text** | `StatHpText` |
| **Stat Mp Text** | `StatMpText` |
| **Stat Atk Text** | `StatAtkText` |
| **Stat Def Text** | `StatDefText` |
| **Skills Container** | `SkillsContainer` (Transform) |
| **Skill Row Prefab** | prefab 1 dòng skill (xem Bước 4) |
| **Upgrade Button** | `UpgradeButton` |
| **Close Button** | `CloseButton` |
| **Status Text** | `StatusText` |
| **Loading Overlay** | `LoadingOverlay` |

**Element Icon Sprites** (kéo Sprite asset vào từng slot):

| Slot | Sprite |
|---|---|
| Fire Sprite | sprite icon lửa |
| Water Sprite | sprite icon nước |
| Earth Sprite | sprite icon đất |
| Metal Sprite | sprite icon kim |
| Wood Sprite | sprite icon mộc |

---

### Bước 4 — Tạo SkillRow Prefab

1. Trong **Hierarchy**, tạo: `Empty GameObject` → đặt tên `SkillRowPrefab`
2. Thêm con: **Image** (icon skill 32×32) + **TextMeshPro** (tên skill)
3. Layout ngang bằng **HorizontalLayoutGroup**
4. Drag từ Hierarchy vào thư mục **Resources/Prefabs/** để tạo prefab
5. Kéo prefab đó vào slot **Skill Row Prefab** trong Inspector của GeneUpgradePanel

> **Lưu ý:** Script đọc icon từ `Resources/SkillIcons/<iconId>` và item icon từ `Resources/ItemIcons/<iconIcon>`. Tạo thư mục này trong Assets nếu chưa có.

---

### Bước 5 — GeneExpBar cấu hình

Chọn `GeneExpBar` (Slider):
- `Interactable` → **tắt** (chỉ hiển thị, không cho kéo)
- `Min Value` = 0
- `Max Value` = 1 (script sẽ tự set lúc runtime)
- `Whole Numbers` = OFF

---

### Bước 6 — ItemCountSlider cấu hình

Chọn `ItemCountSlider` (Slider):
- `Interactable` → **bật**
- `Whole Numbers` → **bật** (chỉ dùng số nguyên)
- `Min Value`/`Max Value` = script tự set từ `stone_min`/`stone_needed`

---

### Bước 7 — Tạo nút Gene độc lập trong Canvas

Nút Gene là **button riêng biệt**, đặt thẳng trong Canvas (hoặc HUD), **không** liên quan gì đến CharacterPanel.

---

#### 7a — Tạo GameObject Button trong Hierarchy

1. Trong **Hierarchy**, click chuột phải vào **Canvas** (cùng cấp với CharacterPanel, InventoryPanel, v.v.)
2. Chọn **UI → Button - TextMeshPro**
3. Đặt tên: **`BtnGeneUpgrade`**
4. Đổi text bên trong thành **"Gene"** (hoặc dùng icon)
5. Kéo vào vị trí mong muốn trên màn hình (góc phải, thanh HUD, v.v.)

Cấu trúc Hierarchy sau bước này:

```
Canvas
├── HUD / PlayerInfoUI ...
├── CharacterPanel          ← panel nhân vật (đã có)
├── GeneUpgradePanel        ← panel gene (đã tạo ở Bước 1-6)
├── BtnGeneUpgrade          ← MỚI — button độc lập
│   └── Text (TMP)  "Gene"
└── ...
```

> **BtnGeneUpgrade** và **GeneUpgradePanel** đều nằm thẳng dưới Canvas — không nằm trong panel nào cả.

---

#### 7b — Gắn script GeneUpgradePanelToggleButton lên button

Script đã viết sẵn: `Assets/Scripts/UI/Character/GeneUpgradePanelToggleButton.cs`

1. Chọn **BtnGeneUpgrade** trong Hierarchy
2. **Inspector → Add Component** → gõ `GeneUpgradePanelToggleButton` → Enter
3. Slot **Gene Panel** hiện ra → kéo **GeneUpgradePanel** (GameObject) từ Hierarchy vào:

```
BtnGeneUpgrade
  [Button]                         ← component tự có
  [GeneUpgradePanelToggleButton]
        Gene Panel ──────────────── GeneUpgradePanel   ← kéo vào đây
```

Bấm Play → click `BtnGeneUpgrade` → `GeneUpgradePanel` mở ra. Xong.

---

#### 7c — Đặt GeneUpgradePanel inactive mặc định

1. Chọn **GeneUpgradePanel** trong Hierarchy
2. Inspector → **bỏ tick checkbox** ở góc trên cùng bên cạnh tên

```
Inspector:  ☐ GeneUpgradePanel    ← bỏ tick
```

Panel sẽ tự `SetActive(true)` khi click nút, và `SetActive(false)` khi bấm **CloseButton** bên trong panel.

---

#### 7d — Đặt GeneUpgradePanel ở đúng vị trí trong Hierarchy để hiện đè lên trên

Unity render **từ trên xuống dưới** trong Hierarchy — GameObject ở **dưới cùng** sẽ hiện **trên cùng** màn hình.

Kéo **GeneUpgradePanel** xuống **dưới** CharacterPanel:

```
Canvas
├── CharacterPanel       ← index thấp → bị che
├── BtnGeneUpgrade
└── GeneUpgradePanel     ← index cao → hiện đè lên trên
```

Để kéo: chuột phải **GeneUpgradePanel** → **Move to Bottom**.

---

#### Tóm tắt 4 việc cần làm

| # | Việc làm | Chỗ thực hiện |
|---|---|---|
| 1 | Tạo `BtnGeneUpgrade` (Button-TMP) | Chuột phải Canvas → UI → Button |
| 2 | Add Component `GeneUpgradePanelToggleButton` | Inspector của BtnGeneUpgrade |
| 3 | Kéo `GeneUpgradePanel` vào slot **Gene Panel** | Inspector của script trên |
| 4 | Bỏ tick `GeneUpgradePanel` cho inactive mặc định | Inspector của GeneUpgradePanel |

---

### Luồng hoạt động khi player bấm Upgrade

```
Open()
  └─ LoadAndRefresh()
       ├─ RefreshPlayerData()     [gọi API /api/player/{id}]
       ├─ LoadGeneConfig()        [gọi API /api/gene/config?elementType=X&tier=Y]
       └─ RefreshUI()             [cập nhật toàn bộ UI]
            ├─ tierDisplayText    "Gene Tier 1 → 2"
            ├─ geneExpBar         progress bar
            ├─ goldCostText       "5,000 vàng"
            ├─ itemCountSlider    min=stone_min, max=stone_needed
            ├─ statBonusSection   "+200 HP, +50 MP..."
            ├─ skillsList         danh sách skill sẽ mở
            └─ upgradeButton      chỉ enable nếu đủ exp + vàng

Khi bấm UpgradeButton
  └─ DoUpgrade(itemCount)
       ├─ gọi API POST /api/gene/upgrade
       ├─ Nếu success=true  → cập nhật tier/gold/stats, load config mới
       └─ Nếu success=false → báo thất bại, exp reset về 0
```

---

## 8. Kiểm tra nhanh sau deploy

```bash
# Lấy config gene Fire tier 1
curl "http://localhost:5000/api/gene/config?elementType=Fire&tier=1"

# Thử nâng cấp (playerId=1, dùng 2 item)
curl -X POST "http://localhost:5000/api/gene/upgrade" \
  -H "Content-Type: application/json" \
  -d '{"playerId":1,"itemCount":2}'
```

---

## 9. Lỗi thường gặp

| Lỗi server trả về | Nguyên nhân | Cách xử lý |
|---|---|---|
| `Thiếu elementType` | Query param bị null | Đảm bảo `GameManager.PlayerElement` không rỗng |
| `Không có config gene cho Fire tier 0` | Players mới chưa có gene tier | Set `gene_tier = 1` mặc định trong DB |
| `Cần X gene exp` | Gene exp chưa đủ | Hiển thị progress bar + thông báo |
| `Không đủ vàng` | Gold thiếu | Hiện popup mua vàng |
| `Không đủ item (id=7)` | Inventory thiếu vật liệu | Hiển thị số lượng còn thiếu |
| `Gene đã đạt bậc tối đa (Tier 5)` | Đã max | Ẩn nút Upgrade |
| Column `gene_tier_required` not found | Chưa chạy migration SQL | Chạy lại bước 1 |
