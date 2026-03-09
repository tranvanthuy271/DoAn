# HƯỚNG DẪN CONFIG UNITY – TOÀN BỘ HỆ THỐNG

> Áp dụng sau khi refactor cấu trúc thư mục.  
> Thực hiện theo đúng thứ tự từ trên xuống dưới.

---

## 0. SAU KHI MỞ UNITY LẦN ĐẦU

Unity sẽ re-import toàn bộ Scripts do folder thay đổi.  
**Chờ thanh progress bar hoàn tất** trước khi làm bất cứ điều gì.

Nếu có lỗi đỏ trong Console sau import → kiểm tra mục **Troubleshoot** ở cuối file.

---

## 1. CẤU TRÚC SCENE

Dự án có **3 scene chính**:

| Scene | Mục đích |
|---|---|
| `LoginScene` | Màn hình đăng nhập / đăng ký |
| `MainScene` (hoặc `GameScene`) | Scene game chính – có network |
| `ServerScene` | Dedicated server (headless build) |

---

## 2. SCENE: LoginScene

### 2.1 Hierarchy cần có

```
LoginScene
├── [Persistent] ──────── DontDestroyOnLoad singletons
│   ├── GameManager          ← GameManager.cs
│   ├── APIClient            ← APIClient.cs (singleton)
│   └── ItemTemplateManager  ← ItemTemplateManager.cs (singleton)
│
└── Canvas (Screen Space – Overlay)
    └── LoginPanel
        ├── Title (TextMeshPro)
        ├── UsernameInput    ← TMP_InputField
        ├── PasswordInput    ← TMP_InputField
        ├── LoginButton      ← Button
        ├── RegisterButton   ← Button
        └── ErrorText        ← TextMeshPro
```

### 2.2 Config từng GameObject

#### GameObject: `GameManager`
| Component | Field | Giá trị |
|---|---|---|
| GameManager | *(không cần gán gì, player tự tìm)* | — |

#### GameObject: `APIClient`
| Component | Field | Giá trị |
|---|---|---|
| APIClient | Base URL | `http://localhost:5247/api` |

> **Quan trọng:** Sửa `baseURL` trong script `APIClient.cs` hoặc expose field trong Inspector nếu chưa có.

#### GameObject: `ItemTemplateManager`
| Component | Field | Giá trị |
|---|---|---|
| ItemTemplateManager | Auto Load On Start | ✅ `true` |
| ItemTemplateManager | Enable Debug Log | `true` (tắt khi release) |

#### GameObject: `LoginPanel` (hoặc một UI controller)
| Component | Field | Giá trị |
|---|---|---|
| LoginController | Username Input | → `UsernameInput` |
| LoginController | Password Input | → `PasswordInput` |
| LoginController | Login Button | → `LoginButton` |
| LoginController | Register Button | → `RegisterButton` |
| LoginController | Error Text | → `ErrorText` |

---

## 3. SCENE: MainScene / GameScene

### 3.1 Hierarchy cần có

```
MainScene
├── [Network] ──────────── Unity Netcode objects
│   ├── NetworkManager      ← Unity NetworkManager component
│   │   └── NetworkManagerController.cs
│   └── NetworkManagerCustom  ← NetworkManagerCustom.cs
│
├── [Initializers]
│   ├── NetworkInitializer   ← GameSceneNetworkInitializer.cs
│   │                          (hoặc MainSceneNetworkInitializer.cs)
│   └── ServerConnectionApproval  ← ServerConnectionApproval.cs
│
├── [Persistent Managers] (nếu chưa DontDestroyOnLoad từ LoginScene)
│   ├── GameManager
│   ├── APIClient
│   ├── ItemTemplateManager
│   └── IconDatabase
│
├── Camera
│   └── Main Camera          ← CameraFollow.cs
│
└── Canvas (Screen Space – Overlay)
    ├── HUD
    │   ├── HealthBar         ← HealthBar.cs
    │   ├── FlightMeter       ← FlightMeter.cs
    │   └── PlayerInfoUI      ← PlayerInfoUI.cs
    │
    ├── InventoryPanel        ← InventoryUI.cs
    │   ├── SlotContainer (GridLayoutGroup)
    │   └── ItemDetailPanel   ← ItemDetailPanel.cs
    │
    ├── EquipmentPanel        ← EquipmentPanelUI.cs
    │   └── Slot_0..5         ← EquipmentSlotUI.cs (x6)
    │
    ├── CharacterPanel        ← CharacterPanelController.cs
    │   ├── Tab_Equipment     ← EquipmentPanelUI
    │   ├── Tab_Potential     ← PotentialTabUI.cs
    │   └── Tab_Skills        ← SkillTabUI.cs
    │
    ├── EnemyHealthBars       ← EnemyHealthBarSpawner.cs
    │
    └── [Buttons làm HUD nhanh]
        ├── BtnInventory      ← InventoryToggleButton.cs
        └── BtnCharacter      ← CharacterPanelToggleButton.cs
```

### 3.2 Config từng Component

#### `GameSceneNetworkInitializer`
| Field | Giá trị |
|---|---|
| Server IP | `127.0.0.1` (local) hoặc IP server thật |
| Server Port | `2003` |
| Start Host Button | → Button "StartHost" (chỉ trong Editor/Host build) |
| Start Client Button | → Button "StartClient" (tuỳ chọn) |
| Status Text | → TMP_Text hiện trạng kết nối |

#### `IconDatabase`
| Field | Giá trị |
|---|---|
| Resources Folder | `ItemIcons` |

> **Sprite icons phải đặt tại:** `Assets/Resources/ItemIcons/`  
> **Tên sprite phải là số** khớp với `item_template.idIcon` trong DB.  
> Ví dụ: `idIcon = 5` → sprite tên là `5` trong folder `ItemIcons`.

#### `CameraFollow`
| Field | Giá trị |
|---|---|
| Target | → Transform của Player GameObject |

#### `InventoryUI`
| Field | Giá trị |
|---|---|
| Inventory Root | → Panel `InventoryPanel` (tự bật/tắt) |
| Slot Container | → `SlotContainer` (GameObject có GridLayoutGroup) |
| Slot Prefab | → Prefab `InventorySlotUI` |
| Item Detail Panel | → `ItemDetailPanel` trong scene |
| Max Slot Count | `20` |

#### `EquipmentPanelUI`
| Field | Giá trị |
|---|---|
| Panel Root | → Panel `EquipmentPanel` |
| Manual Slots (Cách B) | Index 0=Weapon, 1=Helmet, 2=Armor, 3=Pants, 4=Boots, 5=Accessory |
| Title Text | → TMP_Text tiêu đề |

> **Khuyến nghị dùng Cách B (Manual Slots):** tạo sẵn 6 `EquipmentSlotUI` con trong Hierarchy rồi kéo vào array.

#### `InventoryNetworkBridge`
| Field | Giá trị |
|---|---|
| Network Inventory | *(để trống – tự tìm local player)* |
| Inventory UI | → `InventoryUI` trong scene |
| Equipment Panel UI | → `EquipmentPanelUI` trong scene |
| Auto Find Player Inventory | ✅ `true` |
| Verbose Debug | `false` (khi release) |

#### `InventoryToggleButton` (gắn lên Button mở túi)
| Field | Giá trị |
|---|---|
| Inventory UI | → `InventoryUI` trong scene |

#### `CharacterPanelToggleButton` (gắn lên Button mở nhân vật)
| Field | Giá trị |
|---|---|
| Character Panel | → `CharacterPanelController` trong scene |

#### `EnemyHealthBarSpawner`
| Field | Giá trị |
|---|---|
| Health Bar Prefab | → Prefab `EnemyHealthBar` |
| Canvas | → Canvas chứa health bars |

---

## 4. SCENE: ServerScene

```
ServerScene
└── ServerBootstrap    ← ServerBootstrap.cs
```

#### `ServerBootstrap`
| Field | Giá trị |
|---|---|
| Server Port | `2003` |
| Server IP | `0.0.0.0` |
| Auto Start | ✅ `true` |

> Scene này **không có Canvas hay Camera** – chạy headless.

---

## 5. PREFABS CẦN TẠO

### 5.1 Prefab: `InventorySlotUI`

```
InventorySlotUI (Button)
├── InventorySlotUI.cs
├── Image (Background)
├── Icon (Image)           ← field iconImage
├── AmountText (TMP_Text)  ← field amountText
└── EquipBadge (GameObject) ← field equippedBadge (huy hiệu "Đang Mặc")
```

Gán trong Inspector của `InventorySlotUI.cs`:
| Field | Object |
|---|---|
| Icon Image | → `Icon` |
| Amount Text | → `AmountText` |
| Equipped Badge | → `EquipBadge` |

### 5.2 Prefab: `EquipmentSlotUI`

```
EquipmentSlotUI (Button)
├── EquipmentSlotUI.cs
├── SlotBackground (Image)
├── ItemIcon (Image)        ← field iconImage
├── UpgradeLevelText (TMP)  ← field upgradeLevelText ("+5")
├── EmptyLabel (TMP_Text)   ← field emptyText ("Trống")
└── SlotTypeIcon (Image)    ← ảnh nền loại slot (kiếm/mũ/...)
```

### 5.3 Prefab: `EnemyHealthBar`

```
EnemyHealthBar (Canvas, World Space)
├── EnemyHealthBar.cs
├── Background (Image, màu đỏ tối)
└── Fill (Image, màu đỏ sáng) ← field fillImage
```

---

## 6. RESOURCES FOLDER

Cấu trúc `Assets/Resources/` cần có:

```
Assets/Resources/
└── ItemIcons/
    ├── 1.png       ← idIcon = 1 (ví dụ HP Potion nhỏ)
    ├── 2.png       ← idIcon = 2
    ├── 5.png       ← idIcon = 5
    └── ...         ← tên file = idIcon trong item_template DB
```

> **Quy tắc đặt tên:** Tên sprite (không có đuôi) = số `idIcon` trong bảng `item_template`.  
> IconDatabase sẽ load tất cả sprites trong folder này và tra cứu theo `idIcon.ToString()`.

---

## 7. BUILD SETTINGS

### 7.1 Thứ tự Scene

```
File > Build Settings > Scenes In Build:
  0 – LoginScene
  1 – MainScene   (hoặc GameScene)
  2 – ServerScene
```

### 7.2 Scripting Define Symbols

Nếu dùng ParrelSync (clone editor):
```
Edit > Project Settings > Player > Scripting Define Symbols:
  (không cần thêm gì, ParrelSync tự inject PARREL_SYNC)
```

---

## 8. NETWORK PREFABS (NetworkManager)

Mở `NetworkManager` GameObject → Component `NetworkManager` → tab **Prefabs**:

| Prefab cần đăng ký |
|---|
| Player Prefab (có `NetworkObject` component) |
| Prefab có `NetworkInventory.cs` |
| Prefab có `NetworkPlayerDataSync.cs` |
| Enemy Prefab (có `NetworkObject`) |

> Hoặc dùng `DefaultNetworkPrefabs` asset đã có trong project → kéo vào field **Network Prefabs List**.

---

## 9. KIỂM TRA NHANH TRƯỚC KHI PLAY

Mở scene `MainScene`, nhấn Play (với server đang chạy):

- [ ] Console không có lỗi đỏ
- [ ] `[ItemTemplateManager] Loaded X templates` xuất hiện trong Console
- [ ] `[IconDatabase] Loaded X item icons` xuất hiện
- [ ] Login xong → chuyển scene thành công
- [ ] Mở túi đồ (nút inventory) → panel hiện ra
- [ ] Slot có icon, số lượng đúng
- [ ] Mở panel nhân vật → 3 tab hiện đúng

---

## 10. TROUBLESHOOT

| Lỗi | Nguyên nhân | Cách sửa |
|---|---|---|
| `NullReferenceException: InventoryUI` | Chưa gán InventoryUI trong Bridge | Kéo InventoryUI vào `InventoryNetworkBridge.cs` Inspector |
| `[IconDatabase] Loaded 0 icons` | Sprites chưa vào `Resources/ItemIcons` | Chuyển sprites vào đúng folder |
| Icon không hiện | Tên sprite không khớp `idIcon` | Đổi tên sprite = số idIcon |
| Không kết nối được server | Sai IP/Port | Kiểm tra `GameSceneNetworkInitializer` Inspector |
| Script bị mất link trong Prefab | Do di chuyển file | Mở Prefab → kéo lại script bị missing |
| Lỗi `CS0246: type not found` sau refactor | Unity chưa re-import | Nhấn `Assets > Refresh` hoặc `Ctrl+R` |
