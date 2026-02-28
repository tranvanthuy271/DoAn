# 🔍 UNITY OBJECT CHECKLIST - Kiểm tra từng Object

## ❌ LỖI NGHIÊM TRỌNG (CRITICAL ERRORS)

### 🚨 1. NETWORK PREFAB MISSING (Hash 818046180)
**Lỗi:** `[Netcode] Failed to create object locally. [globalObjectIdHash=818046180]`

**Nguyên nhân:** Có một prefab được server spawn nhưng client không tìm thấy trong danh sách đã đăng ký.

**CÁCH FIX TRONG UNITY:**

#### Bước 1: Tìm prefab bị thiếu
1. Mở Unity → **Project** tab
2. Tìm tất cả prefabs có **NetworkObject** component:
   - `Assets/Prefabs/Player/` (MetalPrefab, FirePrefab, WaterPrefab, etc.)
   - `Assets/Prefabs/Enemies/` 
   - `Assets/Prefabs/Items/`
   - `Assets/Prefabs/Projectiles/`

#### Bước 2: Kiểm tra GlobalObjectIdHash
1. Chọn từng prefab
2. Inspect tab → Tìm **NetworkObject** component
3. Xem field `GlobalObjectIdHash` (hiển thị ở dưới cùng)
4. Tìm prefab nào có hash = **818046180**

#### Bước 3: Đăng ký prefab vào NetworkManager
1. **Hierarchy** → Tìm object **NetworkManager** (thường trong scene GameScene)
2. **Inspector** → Component **NetworkManager**
3. Xem list **Network Prefabs**
4. Kéo thả prefab bị thiếu vào list này

**HOẶC** (nếu dùng NetworkPrefabRegistrar):

1. **Hierarchy** → Tìm object **NetworkPrefabRegistrar**
2. **Inspector** → Script **NetworkPrefabRegistrar**
3. Kiểm tra các field:
   - `playerPrefabs[]` (các player prefabs)
   - `enemyPrefabs[]` (enemy prefabs)
   - `projectilePrefabs[]` (projectile prefabs)
   - `itemPickupPrefab` ⚠️ **ĐÂY LÀ VẤN ĐỀ - xem mục 2 bên dưới**
   - `skillEffectPrefab`
   - `inventorySlotPrefab`

4. Đảm bảo TẤT CẢ các field đều có prefab được assign (không có **None**)

---

### 🚨 2. ITEMPICKUP PREFAB MISSING
**Cảnh báo:** `[NetworkPrefabRegistrar] ItemPickup prefab not found!`

**CÁCH FIX:**

#### Option 1: Assign ItemPickup Prefab trong Inspector
1. **Project** → Tìm prefab `ItemPickup` (nên ở `Assets/Prefabs/Items/ItemPickup.prefab`)
2. **Hierarchy** → Chọn object **NetworkPrefabRegistrar**
3. **Inspector** → Tìm field `itemPickupPrefab`
4. Kéo prefab `ItemPickup` từ Project vào field này

#### Option 2: Nếu không có ItemPickup prefab
1. Tạo mới:
   - **Hierarchy** → Right-click → Create Empty → Đặt tên `ItemPickup`
   - Add component: **NetworkObject**
   - Add component: Script `ItemPickup.cs` (nếu đã có)
   - Add component: **SpriteRenderer** hoặc model 3D
   - Add component: **Collider** (BoxCollider2D hoặc SphereCollider)
2. Kéo object từ Hierarchy vào Project folder `Assets/Prefabs/Items/` để tạo Prefab
3. Xóa object trong Hierarchy (giữ lại prefab trong Project)
4. Assign prefab vào NetworkPrefabRegistrar như Option 1

---

### 🚨 3. AUTH MESSAGE KHÔNG ĐƯỢC NHẬN (Player Spawn Timeout)
**Lỗi:** `[NetworkPlayerSpawner] ✗ Player data NOT loaded after 120 attempts`

**Nguyên nhân:** Server chưa đăng ký Named Message handler để nhận auth từ client.

**Fix này cần CODE CHANGE - đã fix trong GameSceneNetworkInitializer.cs nhưng CHƯA CHẠY:**

#### Kiểm tra xem fix đã có chưa:
1. Mở **Visual Studio** hoặc code editor
2. Mở file: `Assets/Scripts/Network/GameSceneNetworkInitializer.cs`
3. Tìm method `StartHostMode()` (khoảng dòng 440-465)
4. **PHẢI CÓ CODE NÀY** sau dòng `networkManager.StartHost();`:

```csharp
networkManager.StartHost();

// ===== FIX: Đăng ký auth handler ngay sau StartHost() =====
if (networkManagerSingleton != null && networkManagerSingleton.IsServer)
{
    Debug.Log("[GameSceneNetworkInitializer] Server started. Registering auth handler...");
    networkManager.RegisterAuthMessageHandler();
}
```

#### Nếu CHƯA CÓ code trên:
- **YÊU CẦU:** Restart Unity Editor để load code mới
- **Sau khi restart:** Test lại Host + Client connection

#### Verify fix đang hoạt động:
- Console log PHẢI THẤY:
  ```
  [GameSceneNetworkInitializer] Server started. Registering auth handler...
  [NetworkManagerCustom] ✓ Registered Named Message handler for ClientAuth
  ```
- Sau khi Client connect, PHẢI THẤY:
  ```
  [NetworkManagerCustom] ===== AUTH MESSAGE RECEIVED =====
  [NetworkManagerCustom] SenderClientId: 1, UserId: 1, Token length: 216
  [ServerPlayerDataManager] Loading player data for client 1...
  [ServerPlayerDataManager] ✓ Player data loaded: [userId]
  ```

---

## ⚠️ WARNING (Không ảnh hưởng gameplay nhưng nên fix)

### ⚠️ 4. ANIMATOR CONTROLLER MISSING
**Cảnh báo:** `Animator is not playing an AnimatorController`

**Vị trí lỗi:** Tất cả Player Prefabs (MetalPrefab, FirePrefab, WaterPrefab, NetworkPlayer, etc.)

**CÁCH FIX:**

#### Bước 1: Kiểm tra từng Player Prefab
1. **Project** → `Assets/Prefabs/Player/`
2. Chọn từng prefab: **MetalPrefab**, **FirePrefab**, **WaterPrefab**, **NetworkPlayer**, etc.

#### Bước 2: Inspector - Animator Component
1. Tìm component **Animator** (thường ở gần đầu danh sách)
2. Kiểm tra field **Controller**:
   - ❌ Nếu = **None (Runtime Animator Controller)** → LỖI!
   - ✅ Nếu có AnimatorController được assign → OK

#### Bước 3: Assign AnimatorController
1. **Project** → Tìm AnimatorController:
   - Thường ở `Assets/Animations/` hoặc `Assets/Animators/`
   - File có icon ![controller icon] và đuôi `.controller`
   - Ví dụ: `PlayerAnimatorController.controller`

2. Kéo AnimatorController vào field **Controller** của Animator component

#### Option: Xóa NetworkAnimator nếu không dùng
- Nếu game không cần đồng bộ animation qua network:
1. Chọn prefab
2. **Inspector** → Component **NetworkAnimator**
3. Right-click → **Remove Component**

---

### ⚠️ 5. ICON DATABASE THIẾU ICONS
**Cảnh báo:** `[IconDatabase] IconId 'client_icon_121' not found in cache`

**Các icon bị thiếu:**
- `client_icon_121`
- `client_icon_142`
- `client_icon_152`
- `client_icon_167`

**CÁCH FIX:**

#### Bước 1: Tìm thư mục Icons
1. **Project** → `Assets/Resources/ItemIcons/`
2. Kiểm tra số lượng icons hiện tại (log cho biết chỉ có 7 icons)

#### Bước 2: Đảm bảo naming convention đúng
Icons trong folder PHẢI có tên chính xác như trong database:
```
ItemIcons/
  ├── client_icon_1.png
  ├── client_icon_2.png
  ├── client_icon_3.png
  ├── ...
  ├── client_icon_121.png  ← Thiếu
  ├── client_icon_142.png  ← Thiếu
  ├── client_icon_152.png  ← Thiếu
  └── client_icon_167.png  ← Thiếu
```

#### Bước 3: Thêm icons bị thiếu
**Option A: Tạo placeholder icons**
1. Copy icon hiện có (ví dụ `client_icon_1.png`)
2. Paste và đổi tên thành `client_icon_121.png`, `client_icon_142.png`, etc.
3. Đảm bảo file ở trong folder `Assets/Resources/ItemIcons/`

**Option B: Update database để dùng icons hiện có**
1. Kết nối database
2. Update bảng `item_templates` hoặc `player_inventory`:
```sql
-- Thay icon_id của items đang dùng icon thiếu
UPDATE item_templates 
SET icon_id = 'client_icon_1'  -- Dùng icon có sẵn
WHERE icon_id IN ('client_icon_121', 'client_icon_142', 'client_icon_152', 'client_icon_167');
```

#### Bước 4: Kiểm tra Texture Import Settings
Mỗi icon file:
1. Chọn file trong Project
2. **Inspector** → Texture Import Settings:
   - **Texture Type:** Sprite (2D and UI)
   - **Sprite Mode:** Single
   - **Read/Write Enabled:** ✅ (checked)
3. Click **Apply**

---

### ⚠️ 6. SPAWN POINTS MISSING
**Cảnh báo:** `[NetworkPlayerHealth] No spawn points found, using current position`

**CÁCH FIX:**

#### Bước 1: Tạo spawn points trong scene
1. **Hierarchy** → Right-click → Create Empty
2. Đặt tên: **PlayerSpawnPoint**
3. Thêm **Tag**:
   - Inspector → Tag dropdown → Add Tag...
   - Tạo tag mới: `PlayerSpawnPoint`
   - Quay lại PlayerSpawnPoint object → Tag = `PlayerSpawnPoint`

4. Position spawn point ở vị trí hợp lý (ví dụ: `(0, 0, 0)`)

#### Bước 2: Tạo nhiều spawn points
1. Duplicate object (Ctrl+D) nhiều lần
2. Đặt ở các vị trí khác nhau trong map
3. Đảm bảo TẤT CẢ đều có Tag = `PlayerSpawnPoint`

---

## ✅ CHECKLIST TỔNG HỢP

### 🔧 NETWORK PREFAB REGISTRY
**Location:** Hierarchy → NetworkPrefabRegistrar (hoặc NetworkManager)

- [ ] **MetalPrefab** - Đã đăng ký
- [ ] **FirePrefab** - Đã đăng ký
- [ ] **WaterPrefab** - Đã đăng ký
- [ ] **EarthPrefab** - Đã đăng ký
- [ ] **WoodPrefab** - Đã đăng ký
- [ ] **NetworkPlayer** - Đã đăng ký
- [ ] **Enemy1** - Đã đăng ký
- [ ] **FireballProjectile** - Đã đăng ký
- [ ] **SkillEffect** - Đã đăng ký
- [ ] **InventorySlot** - Đã đăng ký
- [ ] **ItemPickup** - ❌ **THIẾU - CẦN ASSIGN**
- [ ] **TEST:** Tất cả prefabs có GlobalObjectIdHash match với client/server

### 🎮 PLAYER PREFABS
**Location:** Project → Assets/Prefabs/Player/

Kiểm tra từng prefab (MetalPrefab, FirePrefab, etc.):

- [ ] **NetworkObject** component có
- [ ] **Animator** component có **Controller** được assign (không phải None)
- [ ] **NetworkAnimator** component - Nếu có thì Animator.Controller phải không null
- [ ] **NetworkPlayerHealth** component có
- [ ] **NetworkPlayerDataSync** component có
- [ ] **NetworkInventory** component có
- [ ] **PlayerSkillManager** component có
- [ ] Prefab ở trong **NetworkPrefabs list** của NetworkManager

### 🎨 ICONS & RESOURCES
**Location:** Project → Assets/Resources/ItemIcons/

- [ ] Folder path chính xác: `Assets/Resources/ItemIcons/`
- [ ] Các icons có tên đúng format: `client_icon_[số].png`
- [ ] **client_icon_121.png** - ❌ **THIẾU**
- [ ] **client_icon_142.png** - ❌ **THIẾU**
- [ ] **client_icon_152.png** - ❌ **THIẾU**
- [ ] **client_icon_167.png** - ❌ **THIẾU**
- [ ] Tất cả icons có Texture Type = Sprite (2D and UI)

### 📍 SCENE SETUP
**Location:** Hierarchy (trong GameScene)

- [ ] **NetworkManager** object có trong scene
- [ ] **NetworkPrefabRegistrar** object có trong scene
- [ ] **ServerPlayerDataManager** object tạo dynamic (check Console log)
- [ ] **PlayerSpawnPoint** tags - Tối thiểu 1 spawn point có tag `PlayerSpawnPoint`
- [ ] **ItemTemplateManager** object có trong scene
- [ ] **IconDatabase** object có trong scene
- [ ] **InventoryNetworkBridge** object có trong scene

### 🔐 AUTH FLOW (Code)
**Location:** Assets/Scripts/Network/

- [ ] `GameSceneNetworkInitializer.cs` có code đăng ký auth handler sau StartHost()
- [ ] `NetworkManagerCustom.cs` có method `RegisterAuthMessageHandler()`
- [ ] **Console log** khi test cho thấy auth message được nhận bởi server
- [ ] Không còn timeout lỗi "Player data NOT loaded after 120 attempts"

---

## 🛠️ HƯỚNG DẪN TEST SAU KHI FIX

### Bước 1: Lưu tất cả changes trong Unity
- File → Save (Ctrl+S)
- File → Save Project

### Bước 2: Restart Unity Editor
- Đóng Unity hoàn toàn
- Mở lại project

### Bước 3: Clear cache (Optional)
```powershell
# Chạy trong PowerShell tại C:\Hub\DoAn
Remove-Item -Recurse -Force "Client\Library\ScriptAssemblies"
Remove-Item -Recurse -Force "Client\Library\ShaderCache"
```

### Bước 4: Test Host + Client
1. Unity Editor → Play mode
2. Click **Start Host**
3. Click **Start Client**
4. Kiểm tra Console logs:

**✅ Expected Logs (Thành công):**
```
[GameSceneNetworkInitializer] Server started. Registering auth handler...
[NetworkManagerCustom] ✓ Registered Named Message handler for ClientAuth
[NetworkManagerCustom] ===== AUTH MESSAGE RECEIVED =====
[ServerPlayerDataManager] ✓ Player data loaded: [userId]
[NetworkPlayerSpawner] ✓ Player data found for clientId 1
[NetworkPlayerSpawner] Spawning player with prefab: MetalPrefab
```

**❌ Failed Logs (Vẫn lỗi):**
```
[NetworkPlayerSpawner] ✗ Player data NOT loaded after 120 attempts
[Netcode] Failed to create object locally. [globalObjectIdHash=...]
```

---

## 🚀 PRIORITY ORDER (Làm theo thứ tự)

1. **🔴 TOP PRIORITY - FIX AUTH MESSAGE:**
   - Check code đã có fix trong `GameSceneNetworkInitializer.cs`
   - Restart Unity để load code mới
   - Test và verify auth message được nhận

2. **🔴 CRITICAL - FIX MISSING NETWORK PREFAB (Hash 818046180):**
   - Tìm prefab với hash này
   - Assign vào NetworkPrefabRegistrar hoặc NetworkManager

3. **🟡 HIGH - FIX ITEMPICKUP PREFAB:**
   - Assign ItemPickup prefab vào NetworkPrefabRegistrar field
   - Hoặc tạo prefab mới nếu chưa có

4. **🟢 MEDIUM - FIX ANIMATOR CONTROLLERS:**
   - Assign AnimatorController cho tất cả player prefabs
   - Hoặc remove NetworkAnimator nếu không dùng

5. **🟢 LOW - FIX ICONS:**
   - Thêm các icons bị thiếu vào Resources/ItemIcons/
   - Hoặc update database để dùng icons hiện có

6. **🟢 LOW - FIX SPAWN POINTS:**
   - Tạo PlayerSpawnPoint objects với tag đúng trong scene

---

## 📋 QUICK REFERENCE

### Tìm Objects trong Unity:
- **Ctrl+F** trong Project/Hierarchy tab
- Search by type: `t:Prefab`, `t:AnimatorController`, `t:Sprite`
- Search by name: Tên prefab, component, script

### Check Network Prefabs đã đăng ký:
1. Console log → Tìm `[NetworkPrefabRegistrar] ===== REGISTERED PREFABS LIST =====`
2. Đếm số prefabs: Hiện tại = **13 prefabs**
3. So sánh với prefabs trong project

### Common File Locations:
```
Assets/
├── Prefabs/
│   ├── Player/          ← Player prefabs (MetalPrefab, etc.)
│   ├── Enemies/         ← Enemy prefabs
│   ├── Items/           ← ItemPickup prefab
│   └── Projectiles/     ← Projectile prefabs
├── Animations/          ← AnimatorController files
├── Resources/
│   └── ItemIcons/       ← Icon sprites
└── Scripts/
    └── Network/         ← Network scripts
```

---

**LƯU Ý:** Nếu sau khi fix mà vẫn lỗi, copy toàn bộ Console log mới và báo lại!
