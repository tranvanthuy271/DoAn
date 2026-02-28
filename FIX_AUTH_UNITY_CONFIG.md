# Hướng dẫn Config Auth trong Unity Editor

## Vấn đề hiện tại
Tất cả **player prefabs** vẫn có **ClientAuthHandler** component cũ, nhưng thiếu **NetworkPlayerDataSync** mới.

## Các bước thực hiện trong Unity

### Bước 1: Xóa ClientAuthHandler khỏi tất cả Player Prefabs

1. **Mở Unity Editor**
2. **Tìm các prefabs** trong Project window:
   ```
   Assets/Prefabs/Player/
   ```
3. **Các prefab cần sửa:**
   - `MetalPrefab.prefab`
   - `MetalPrefab_1.prefab`
   - `FirePrefab.prefab`
   - `WaterPrefab.prefab`
   - `EarthPrefab.prefab`
   - `WoodPrefab.prefab`
   - `NetworkPlayer.prefab` (nếu có)

4. **Với MỖI prefab:**
   - Click vào prefab trong Project window
   - Trong Inspector window, tìm **ClientAuthHandler** component
   - Click vào ⚙️ (gear icon) bên phải tên component
   - Chọn **"Remove Component"**
   - Click **"Apply"** ở góc trên bên

phải của Inspector

### Bước 2: Thêm NetworkPlayerDataSync vào tất cả Player Prefabs

1. **Với MỖI prefab** (cùng danh sách ở trên):
   - Click vào prefab trong Project window
   - Trong Inspector window, click nút **"Add Component"** ở cuối
   - Gõ: `NetworkPlayerDataSync`
   - Click vào component khi nó hiện ra
   - Click **"Apply"** ở góc trên bên phải

### Bước 3: Xóa AuthSenderNetworkObject (nếu có)

1. **Tìm trong scene hoặc prefab:**
   ```
   Assets/Prefabs/AuthSenderNetworkObjectPrefab.prefab
   ```
2. **Xóa hoàn toàn** (không cần nữa vì auth được gửi qua Player NetworkObject)

### Bước 4: Kiểm tra NetworkPlayerSpawner

1. **Tìm NetworkPlayerSpawner** trong scene:
   - Hierarchy window → tìm object có NetworkPlayerSpawner script
2. **Trong Inspector:**
   - Xóa **authSenderPrefab** field (set về None)
   - Kiểm tra các player prefabs đã được assign đúng

### Bước 5: Kiểm tra GameSceneNetworkInitializer

1. **Tìm trong scene:**
   - Hierarchy → GameSceneNetworkInitializer
2. **Trong Inspector:**
   - Xóa **Auth Sender Prefab** field (set về None)

## Kiểm tra kết quả

### Sau khi sửa, mỗi Player Prefab phải có:
✅ NetworkObject
✅ NetworkPlayerDataSync (MỚI)
✅ NetworkInventory
✅ PlayerSkillManager
✅ NetworkPlayerHealth
✅ ... (các component khác)

❌ KHÔNG còn ClientAuthHandler
❌ KHÔNG còn ClientAuthSender

### Log console khi chạy đúng:
```
[NetworkPlayerDataSync] OnNetworkSpawn - IsOwner: True
[NetworkPlayerDataSync] Sending auth to server...
[NetworkPlayerDataSync] ✓ JWT token found, length: 216
[NetworkPlayerDataSync] ✓ User ID: 1
🎯 AUTH SERVERRPC RECEIVED ON HOST!!!
[ServerPlayerDataManager] ✓ Player data loaded for client 0
```

## Nếu gặp lỗi compile

Nếu Unity báo lỗi về NetworkPlayerDataSync:

1. **Đảm bảo file đã save:**
   ```
   Client/Assets/Scripts/Network/Shared/NetworkPlayerDataSync.cs
   ```
2. **Restart Unity** để reload scripts
3. **Xóa Library/ScriptAssemblies** folder và mở lại Unity

## Các file đã được update

✅ `NetworkPlayerDataSync.cs` - Đã thêm SendAuthServerRpc()
✅ `NetworkManagerCustom.cs` - Đã xóa ClientAuthSender calls

## Lưu ý quan trọng

⚠️ **Phải xóa ClientAuthHandler khỏi TẤT CẢ player prefabs**, nếu không sẽ bị duplicate auth gửi 2 lần và gây conflict!

⚠️ **Phải Apply changes** sau mỗi lần sửa prefab

⚠️ **Nên backup project** trước khi sửa prefabs

## Nếu muốn dùng script để auto-fix

Tôi có thể tạo Python script để tự động sửa prefab files, nhưng khuyến nghị làm thủ công trong Unity Editor để an toàn hơn.

---

**Sau khi hoàn thành tất cả các bước trên, test lại và kiểm tra log.**
