# Hướng dẫn Config Buff HUD System trong Unity Editor

> **Không cần viết code** — tất cả script đã có sẵn. Chỉ cần setup Hierarchy + Inspector theo hướng dẫn dưới đây.

---

## Mục lục
1. [Quy ước icon](#1-quy-ước-icon)
2. [Tạo Prefab BuffIconEntry](#2-tạo-prefab-buffIconEntry)
3. [Tạo Prefab BuffDetailTooltip](#3-tạo-prefab-buffDetailTooltip)
4. [Setup BuffHudPanel trong HUD Canvas](#4-setup-buffhudpanel-trong-hud-canvas)
5. [Setup ActiveBuffManager GameObject](#5-setup-activebuffmanager-gameobject)
6. [Gắn các Prefab vào Inspector](#6-gắn-các-prefab-vào-inspector)
7. [Kiểm tra icon đúng ID](#7-kiểm-tra-icon-đúng-id)
8. [Test nhanh trong Play Mode](#8-test-nhanh-trong-play-mode)

---

## 1. Quy ước icon

**Tất cả icon buff dùng chung thư mục `Resources/ItemIcons/` với icon item bình thường.**  
Tên file = số `icon_id` trong bảng `item_effect_template`.

```
Assets/
└── Resources/
    └── ItemIcons/
        ├── 101.png   ← icon HpRestore nhỏ  (item_effect_template.icon_id = 101)
        ├── 102.png   ← icon HpRestore vừa
        ├── 111.png   ← icon MpRestore nhỏ
        ├── 121.png   ← icon GeneExpBuff 20%
        ├── 122.png   ← icon GeneExpBuff 50%
        ├── 123.png   ← icon GeneExpBuff 100%
        ├── 131.png   ← icon ExpBuff 25%
        ├── 132.png   ← icon ExpBuff 50%
        ├── 141.png   ← icon PhucBuff 10%
        ├── 142.png   ← icon PhucBuff 25%
        ├── 151.png   ← icon AttackBuff 15%
        ├── 152.png   ← icon DefenseBuff 15%
        ├── 161.png   ← icon HpBuff 10%
        ├── 162.png   ← icon HpBuff 20%
        ├── 163.png   ← icon HpBuff 40%
        ├── 171.png   ← icon MpBuff 10%
        ├── 172.png   ← icon MpBuff 20%
        └── 173.png   ← icon MpBuff 40%
```

> **Lưu ý:** Icon cho buff timed (121–173) nên có viền màu để phân biệt với icon item.  
> Instant effect (101–113) không xuất hiện trong HUD nên không cần icon đặc biệt.

Cách import sprite:
1. Kéo file `.png` vào folder `Assets/Resources/ItemIcons/`
2. Chọn file → Inspector → **Texture Type = Sprite (2D and UI)** → **Apply**
3. Đặt tên file = số ID (ví dụ: `151.png`, không phải `buff_151.png`)

---

## 2. Tạo Prefab BuffIconEntry

Prefab này hiển thị **1 icon buff** với countdown ring xoay ngược chiều kim đồng hồ.

### 2.1 Tạo Hierarchy

Trong **Project** → chuột phải → **Create > UI > Image** để tạo root, hoặc làm thủ công:

```
BuffIconEntry          ← GameObject rễ (gắn script BuffIconEntry)
├── Background         ← Image (nền tối mờ)
├── Icon               ← Image (sprite icon buff - đây là ảnh chính)
├── CountdownRing      ← Image (vòng tròn đếm ngược)
└── TimeLabel          ← TextMeshPro - Text UI (hiện "30s", "5m"...)
```

### 2.2 Config từng thành phần

**Root `BuffIconEntry`**
| Thuộc tính | Giá trị |
|---|---|
| RectTransform Width/Height | **48 × 48** |
| Add Component | `BuffIconEntry` (script) |
| Add Component | `Canvas Group` (tuỳ chọn, để fade) |

**`Background` (Image)**
| Thuộc tính | Giá trị |
|---|---|
| Source Image | Sprite tròn hoặc vuông tối |
| Color | `(0, 0, 0, 150)` — đen, alpha ≈ 59% |
| RectTransform | Stretch toàn bộ parent (Anchor = Stretch All) |

**`Icon` (Image)**
| Thuộc tính | Giá trị |
|---|---|
| Source Image | Để trống (script sẽ load tự động) |
| Preserve Aspect | ✅ bật |
| RaycastTarget | ✅ bật (để nhận click) |
| RectTransform | Padding 4px mỗi cạnh → thực ra là Anchor Stretch, Left=4, Right=4, Top=4, Bottom=4 |

**`CountdownRing` (Image)**
| Thuộc tính | Giá trị |
|---|---|
| Source Image | Sprite **vòng tròn rỗng** (circle outline) |
| Color | `(1, 1, 0, 200)` — vàng, hơi trong |
| **Image Type** | **Filled** |
| **Fill Method** | **Radial 360** |
| **Fill Origin** | **Top** |
| **Clockwise** | ✅ bật |
| Fill Amount | 1 (runtime script tự cập nhật) |
| RaycastTarget | ❌ tắt |
| RectTransform | Stretch toàn bộ parent |

**`TimeLabel` (TextMeshPro - Text UI)**
| Thuộc tính | Giá trị |
|---|---|
| Font Size | **10** |
| Alignment | Center / Bottom |
| Color | Trắng với outline đen nhẹ |
| Overflow | Overflow hoặc Truncate |
| RaycastTarget | ❌ tắt |
| RectTransform | Anchor = Bottom Center, Height = 14, Width = 48 |

### 2.3 Gắn vào Inspector của script `BuffIconEntry`

Kéo các GameObject con vào đúng slot:

| Slot Inspector | Kéo vào |
|---|---|
| **Icon Image** | `Icon` (Image component) |
| **Countdown Ring** | `CountdownRing` (Image component) |
| **Time Label** | `TimeLabel` (TMP_Text component) |
| **Buff Icons Folder** | `ItemIcons` ← **giữ nguyên mặc định** |

### 2.4 Lưu thành Prefab

Kéo từ Hierarchy ra `Assets/Prefabs/UI/BuffIconEntry.prefab`

---

## 3. Tạo Prefab BuffDetailTooltip

Popup xuất hiện khi người chơi **click** vào buff icon.

### 3.1 Tạo Hierarchy

```
BuffDetailTooltip      ← Panel (gắn script BuffDetailTooltip + Canvas + GraphicRaycaster)
├── Background         ← Image (nền tối, góc bo)
├── NameText           ← TMP_Text (tên buff)
├── DetailText         ← TMP_Text (mô tả)
├── TimeText           ← TMP_Text (thời gian còn lại - màu vàng)
└── CloseBtn           ← Button (nút × nhỏ - tuỳ chọn)
```

### 3.2 Config từng thành phần

**Root `BuffDetailTooltip`**
| Thuộc tính | Giá trị |
|---|---|
| RectTransform Width/Height | **220 × 110** |
| Pivot | `(0, 0)` — góc dưới trái |
| Add Component | `BuffDetailTooltip` (script) |
| Add Component | `Canvas` → `Override Sorting = ON`, `Sorting Order = 250` |
| Add Component | `GraphicRaycaster` |

> Script sẽ tự add Canvas nếu thiếu — nhưng add trước sẽ tránh warning.

**`Background` (Image)**
| Thuộc tính | Giá trị |
|---|---|
| Color | `(0.1, 0.1, 0.1, 0.92)` → tối đậm |
| Source Image | Sprite có góc bo (optional) |
| RectTransform | Stretch toàn bộ parent |

**`NameText` (TMP_Text)**
| Thuộc tính | Giá trị |
|---|---|
| Font Size | **14** |
| Font Style | **Bold** |
| Color | Trắng |
| Overflow | Truncate |
| RectTransform | Anchor TopLeft, Pos (8, -8), Width=204, Height=20 |

**`DetailText` (TMP_Text)**
| Thuộc tính | Giá trị |
|---|---|
| Font Size | **11** |
| Word Wrapping | ✅ bật |
| Color | `(0.85, 0.85, 0.85, 1)` — xám nhạt |
| RectTransform | Anchor TopLeft, Pos (8, -30), Width=204, Height=44 |

**`TimeText` (TMP_Text)**
| Thuộc tính | Giá trị |
|---|---|
| Font Size | **11** |
| Color | **`(1, 0.9, 0.2, 1)`** — vàng |
| RectTransform | Anchor BottomLeft, Pos (8, 8), Width=204, Height=18 |

**`CloseBtn` (Button)** *(tuỳ chọn)*
| Thuộc tính | Giá trị |
|---|---|
| RectTransform | Anchor TopRight, Pos (-4, -4), Width=20, Height=20 |
| Text con | `×` FontSize=14 |

### 3.3 Gắn vào Inspector của script `BuffDetailTooltip`

| Slot Inspector | Kéo vào |
|---|---|
| **Name Text** | `NameText` |
| **Detail Text** | `DetailText` |
| **Time Text** | `TimeText` |
| **Close Button** | `CloseBtn` (nếu có) |
| **Auto Close Seconds** | `5` |
| **Y Offset** | `70` |

### 3.4 Lưu thành Prefab

Kéo ra `Assets/Prefabs/UI/BuffDetailTooltip.prefab`  
Sau đó **xoá khỏi Hierarchy** (script khởi tạo tại runtime).

---

## 4. Setup BuffHudPanel trong HUD Canvas

### 4.1 Tạo GameObject

Trong HUD Canvas của scene (Canvas chứa HealthBar, MiniMap...):

```
HUD Canvas
└── BuffHudPanel       ← GameObject mới, gắn script BuffHudPanel
```

> Tạo bằng : chuột phải vào HUD Canvas → **Create Empty** → đặt tên `BuffHudPanel`

### 4.2 RectTransform

| Thuộc tính | Giá trị |
|---|---|
| Anchor Preset | **Bottom Left** |
| Pos X | `10` |
| Pos Y | `60` (phía trên HP bar) |
| Pos Z | `0` |
| Width | `400` |
| Height | `52` |

### 4.3 Add Component — Layout

Add **Horizontal Layout Group** vào `BuffHudPanel`:

| Thuộc tính | Giá trị |
|---|---|
| Spacing | `4` |
| Child Alignment | `Middle Left` |
| Control Child Size Width | ❌ tắt |
| Control Child Size Height | ❌ tắt |
| Child Force Expand Width | ❌ tắt |
| Child Force Expand Height | ❌ tắt |

### 4.4 Gắn script BuffHudPanel vào Inspector

| Slot Inspector | Kéo vào |
|---|---|
| **Buff Icon Entry Prefab** | `Assets/Prefabs/UI/BuffIconEntry.prefab` |
| **Tooltip Prefab** | `Assets/Prefabs/UI/BuffDetailTooltip.prefab` |
| **Root Canvas** | Canvas gốc của scene (không phải HUD Canvas con) |

---

## 5. Setup ActiveBuffManager GameObject

Script này quản lý danh sách buff — cần có **DontDestroyOnLoad**.

### 5.1 Nơi đặt

Đặt vào scene **cố định** (ví dụ `Gameplay` hoặc scene tồn tại suốt phiên chơi).

```
Scene Hierarchy
└── [Managers]            ← GameObject cha của tất cả manager
    ├── GameManager
    ├── APIClient
    └── ActiveBuffManager  ← GameObject mới, gắn script ActiveBuffManager
```

### 5.2 Cách tạo

1. Trong Hierarchy → chuột phải `[Managers]` → **Create Empty** → đặt tên `ActiveBuffManager`
2. Add Component: `ActiveBuffManager`
3. Script tự gọi `DontDestroyOnLoad` — **không cần làm gì thêm**

### 5.3 Kiểm tra

Chạy game, mở **Window → Analysis → Profiler** hoặc gõ vào Console:

```
ActiveBuffManager.Instance != null → phải là True
```

---

## 6. Gắn các Prefab vào Inspector (tóm tắt)

| GameObject | Script | Slot cần gán |
|---|---|---|
| `BuffHudPanel` | `BuffHudPanel` | `buffIconEntryPrefab`, `tooltipPrefab`, `rootCanvas` |
| `BuffIconEntry.prefab` | `BuffIconEntry` | `iconImage`, `countdownRing`, `timeLabel` |
| `BuffDetailTooltip.prefab` | `BuffDetailTooltip` | `nameText`, `detailText`, `timeText`, `closeButton` |

---

## 7. Kiểm tra icon đúng ID

### 7.1 Nguyên tắc

`icon_id` trong bảng `item_effect_template` = **tên file** trong `Resources/ItemIcons/`.

```
item_effect_template.icon_id = 151
→ load Resources.Load<Sprite>("ItemIcons/151")
→ file: Assets/Resources/ItemIcons/151.png
```

Script `BuffIconEntry.LoadIcon()` gọi đúng path này. **Không cần folder `BuffIcons/` riêng.**

### 7.2 Kiểm tra icon nào đang thiếu

Chạy game, mở Console. Khi buff active mà icon không hiển thị, Unity sẽ không báo lỗi nhưng icon sẽ dùng sprite mặc định đã gán trong Prefab `BuffIconEntry`.

Để debug thủ công, tìm trong Project panel:

```
Assets/Resources/ItemIcons/
```

Đối chiếu với danh sách `icon_id` trong DB và đảm bảo đủ file:

| icon_id | Buff loại | File cần có |
|---|---|---|
| 121 | GeneExpBuff +20% | `121.png` |
| 122 | GeneExpBuff +50% | `122.png` |
| 123 | GeneExpBuff +100% | `123.png` |
| 131 | ExpBuff +25% | `131.png` |
| 132 | ExpBuff +50% | `132.png` |
| 141 | PhucBuff +10% | `141.png` |
| 142 | PhucBuff +25% | `142.png` |
| 151 | AttackBuff +15% | `151.png` |
| 152 | DefenseBuff +15% | `152.png` |
| 161 | HpBuff +10% | `161.png` |
| 162 | HpBuff +20% | `162.png` |
| 163 | HpBuff +40% | `163.png` |
| 171 | MpBuff +10% | `171.png` |
| 172 | MpBuff +20% | `172.png` |
| 173 | MpBuff +40% | `173.png` |

### 7.3 Sprite cần có Texture Type = Sprite

Chọn file trong Project → Inspector xem **Texture Type**:
- ✅ **Sprite (2D and UI)** → đúng
- ❌ Default / Texture → sai, không load được dạng Sprite

---

## 8. Test nhanh trong Play Mode

### 8.1 Test thủ công bằng DB

1. Mở game, vào Inventory
2. Sử dụng item ID 151 (Bùa Tăng Công Nhỏ) hoặc 121 (Nhân Sâm Tâm Linh)
3. Quan sát góc dưới-trái màn hình → icon phải xuất hiện trong `BuffHudPanel`
4. Click vào icon → `BuffDetailTooltip` phải popup với tên, mô tả, countdown

### 8.2 Kiểm tra countdown ring

- Ring phải đầy (fill=1) khi vừa mới dùng item
- Ring cạn dần (giảm fill) theo thời gian thực
- Số giây bên dưới icon phải đếm ngược

### 8.3 Kiểm tra buff áp dụng trong game

| Buff | Kiểm tra bằng cách |
|---|---|
| AttackBuff | Dùng item 151 → tấn công quái, damage phải cao hơn bình thường 15% |
| DefenseBuff | Dùng item 152 → để quái đánh, nhận damage thấp hơn ~15% |
| ExpBuff | Dùng item 131 → giết quái, EXP nhận được +25% |
| HpBuff | Dùng item 161 → mở Stats Tab, MaxHP tăng 10% |
| MpBuff | Dùng item 171 → mở Stats Tab, MaxMP tăng 10% |

### 8.4 Nếu icon không hiện

1. Kiểm tra `ActiveBuffManager` có trong scene không (xem Console có log `[ActiveBuffManager]`)
2. Kiểm tra `BuffHudPanel.buffIconEntryPrefab` đã gán chưa (xem Inspector)
3. Kiểm tra file `151.png` tồn tại trong `Assets/Resources/ItemIcons/`
4. Kiểm tra API `/api/player/{id}/inventory/use-item` trả về `active_buffs` có item không (xem Console log `[ItemUseHandler] ✅ UseItem OK`)

---

## Sơ đồ luồng dữ liệu (tóm tắt)

```
Người chơi dùng item (InventoryUI)
        ↓
ItemUseHandler.DoUseConsumableItem()
        ↓
REST API: POST /api/player/{id}/inventory/use-item
        ↓ response: active_buffs[]
ActiveBuffManager.OnBuffsReceived(active_buffs)
        ↓
OnBuffListChanged event
        ↓
BuffHudPanel.OnBuffListChanged(buffs)
        ↓ foreach buff
BuffIconEntry.Bind(buff)
  → Resources.Load("ItemIcons/{iconId}")   ← icon_id từ item_effect_template
  → CountdownRing fillAmount = remaining/total
  → TimeLabel "30m", "5m", "30s"
        ↓ click
BuffDetailTooltip.Show(buff, screenPos)
  → NameText   = buff.name
  → DetailText = buff.detail
  → TimeText   = countdown live (1s interval)
```
