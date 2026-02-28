# FIX: NetworkPrefab Hash 818046180 Not Found Error

## Vấn đề (Problem)
Client không thể spawn object vì prefab không được đăng ký trong NetworkManager:
```
[Netcode] Failed to create object locally. [globalObjectIdHash=818046180]. 
NetworkPrefab could not be found. Is the prefab registered with NetworkManager?
```

## Nguyên nhân (Root Cause)
**CLIENT không có cùng danh sách prefabs như SERVER!**

- **Server (Host)**: NetworkPlayerSpawner có prefabs được assign → prefabs được đăng ký ✓
- **Client**: NetworkPlayerSpawner KHÔNG có prefabs assigned hoặc bị thiếu → prefabs KHÔNG được đăng ký ✗

Khi server spawn player cho client, client không nhận diện được prefab.

## Giải pháp (Solutions)

### 🔧 Solution 1: Assign Prefabs vào NetworkPlayerSpawner (KHUYẾN NGHỊ)

1. **Mở Unity Editor** trong project `Client_clone_0`

2. **Mở scene GameScene**:
   - File → Open Scene
   - Chọn: `Assets/Scenes/GameScene.unity`

3. **Tìm NetworkPlayerSpawner trong Hierarchy**:
   - Tìm GameObject có component `NetworkPlayerSpawner`
   - Nếu không có, tạo mới: GameObject → Create Empty → Add Component → NetworkPlayerSpawner

4. **Assign tất cả Player Prefabs trong Inspector**:
   ```
   NetworkPlayerSpawner (Script)
   ├─ Default Player Prefab (Fallback)
   │  └─ Kéo prefab: Assets/Prefabs/NetworkPlayer.prefab
   ├─ Element Prefabs
   │  ├─ Fire Male Prefab    → Assets/Prefabs/FirePrefab.prefab (hoặc FireMalePrefab)
   │  ├─ Fire Female Prefab  → Assets/Prefabs/FireFemalePrefab.prefab
   │  ├─ Water Male Prefab   → Assets/Prefabs/WaterPrefab.prefab
   │  ├─ Water Female Prefab → Assets/Prefabs/WaterFemalePrefab.prefab
   │  ├─ Earth Male Prefab   → Assets/Prefabs/EarthPrefab.prefab
   │  ├─ Earth Female Prefab → Assets/Prefabs/EarthFemalePrefab.prefab
   │  ├─ Wood Male Prefab    → Assets/Prefabs/WoodPrefab.prefab
   │  ├─ Wood Female Prefab  → Assets/Prefabs/WoodFemalePrefab.prefab
   │  ├─ Metal Male Prefab   → Assets/Prefabs/MetalPrefab.prefab
   │  └─ Metal Female Prefab → Assets/Prefabs/MetalFemalePrefab.prefab
   ```

5. **Lưu Scene**: `Ctrl+S`

6. **Kiểm tra các prefabs có NetworkObject component**:
   - Chọn từng prefab trong Project window
   - Đảm bảo có component `NetworkObject`

### 🔧 Solution 2: Sử dụng Manual Prefabs List

Nếu không muốn dùng NetworkPlayerSpawner, bạn có thể assign trực tiếp:

1. **Tìm NetworkPrefabRegistrar** trong GameScene Hierarchy

2. **Trong Inspector**:
   - Bỏ tick `Auto-register from NetworkPlayerSpawner` (nếu không dùng)
   - Mở rộng `Manual Prefab List`
   - Set `Size` = 11 (hoặc số lượng prefabs bạn có)
   - Kéo tất cả player prefabs vào list

3. **Lưu Scene**

### 🔧 Solution 3: Copy từ Server sang Client

1. **Mở project HOST (Client) trong Unity**
2. **Mở GameScene**
3. **Tìm NetworkPlayerSpawner**
4. **Copy component**: Right-click → Copy Component
5. **Chuyển sang project CLIENT_CLONE_0**
6. **Paste component**: Right-click NetworkPlayerSpawner → Paste Component Values

## Kiểm tra (Verification)

### Bước 1: Chạy Debug Tool

Tôi đã tạo script `NetworkPrefabDebugger.cs` để kiểm tra:

1. **Attach script vào NetworkManager**:
   - Chọn NetworkManager GameObject trong scene
   - Add Component → NetworkPrefabDebugger

2. **Chạy game** và xem Console log:
   ```
   [NetworkPrefabRegistrar] ✓ Found NetworkPlayerSpawner, registering prefabs...
   [NetworkPrefabRegistrar] ✓ Registered prefab: 'FirePrefab' | Hash: 123456789
   [NetworkPrefabRegistrar] ✓ Registered 11 prefab(s) to NetworkManager
   ```

3. **Kiểm tra hash**: Tìm prefab nào có hash `818046180`

### Bước 2: Verify Prefabs Được Đăng Ký

Sau khi start client, log sẽ hiện:
```
[NetworkPrefabRegistrar] ===== REGISTERED PREFABS LIST =====
[NetworkPrefabRegistrar] Total registered prefabs: 11
[NetworkPrefabRegistrar]   - Prefab: 'FirePrefab' | Hash: 818046180  ← TÌM THẤY!
[NetworkPrefabRegistrar]   - Prefab: 'WaterPrefab' | Hash: 234567890
...
[NetworkPrefabRegistrar] ===== END PREFABS LIST =====
```

Nếu **không thấy list này** hoặc **list rỗng** → Prefabs CHƯA được assign!

## Lưu ý quan trọng (Important Notes)

### ⚠️ Prefabs phải TRÙNG KHỚP giữa Server và Client

- **Cùng tên prefab**: `FirePrefab`, `WaterPrefab`, etc.
- **Cùng GlobalObjectIdHash**: Unity tự động generate
- **Cùng NetworkObject configuration**

### ⚠️ Timing của Prefab Registration

Prefabs phải được đăng ký **TRƯỚC KHI** StartClient():
```
1. RegisterNetworkPrefabs()  ← Phải gọi trước!
2. StartClient()             ← Gọi sau
```

Code hiện tại đã đúng thứ tự trong `GameSceneNetworkInitializer.Start()`.

### ⚠️ DefaultNetworkPrefabs.asset

Nếu có file `Assets/DefaultNetworkPrefabs.asset` hoặc `Assets/ScriptableObjects/DefaultNetworkPrefabs.asset`:

1. **Mở file** trong Inspector
2. **Kiểm tra danh sách**: Phải có tất cả player prefabs
3. **Nếu thiếu**: Add prefabs vào list

## Các lỗi liên quan (Related Errors)

Nếu bạn thấy các lỗi này, cùng là vấn đề prefab registration:

```
[Netcode] Failed to spawn NetworkObject for Hash 818046180
Animator is not playing an AnimatorController  
NetworkInventory ItemID X not found!
```

## Tóm tắt (Summary)

**Bước làm:**
1. ✅ Mở `Client_clone_0` project
2. ✅ Mở `GameScene.unity`
3. ✅ Tìm `NetworkPlayerSpawner` component
4. ✅ Assign TẤT CẢ player prefabs vào các slots trong Inspector
5. ✅ Save scene
6. ✅ Test lại: Start Host trong Client, Start Client trong Client_clone_0

**Kết quả mong đợi:**
```
[NetworkPrefabRegistrar] ✓ Registered 11 prefab(s) to NetworkManager
[Netcode] Client connected successfully!
[PlayerSkillManager] Đã khởi tạo X skill(s)
[NetworkInventory] OnNetworkSpawn CALLED!
```

**KHÔNG còn error:** `NetworkPrefab could not be found`

---

**Need more help?** Check console logs để xem prefab nào missing!
