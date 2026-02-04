# HƯỚNG DẪN SETUP NETCODE VÀ ĐỒNG BỘ NGƯỜI CHƠI

## 📋 TỔNG QUAN

Sau khi tạo nhân vật thành công và lưu vào database, bạn cần:
1. **Setup Netcode Server** để đồng bộ người chơi
2. **Spawn đúng prefab** dựa trên `element_type` + `gender` từ database
3. **Đồng bộ player data** (stats, level, HP, MP, etc.) qua NetworkVariable
4. **Đồng bộ di chuyển** và các hành động khác

---

## 1️⃣ CẤU TRÚC NETCODE HIỆN TẠI

### 1.1. Scripts đã có:
- **`NetworkManagerCustom.cs`**: Quản lý kết nối (StartHost, StartServer, ConnectToServer)
- **`NetworkPlayerSpawner.cs`**: Spawn player khi client connect
- **`NetworkPlayerController.cs`**: Đồng bộ di chuyển qua ServerRpc
- **`NetworkPlayerDataSync.cs`**: Đồng bộ player data (element_type, gender, stats) qua NetworkVariable
- **`GameSceneNetworkInitializer.cs`**: Load player data từ API khi vào GameScene

### 1.2. Prefabs:
- **`NetworkPlayer.prefab`**: Prefab mặc định (fallback)
- **Element Prefabs**: `FirePrefab`, `WaterPrefab`, `EarthPrefab`, `WoodPrefab`, `MetalPrefab` (trong `Assets/Prefabs/Player/`)

---

## 2️⃣ SETUP NETCODE SERVER

### 2.1. Cấu hình NetworkManager trong GameScene

1. **Mở scene `GameScene`** trong Unity Editor

2. **Tạo GameObject `NetworkManager`**:
   - Add Component: **`NetworkManager`** (từ Netcode for GameObjects)
   - Add Component: **`UnityTransport`** (từ Netcode.Transports.UTP)
   - Add Component: **`NetworkManagerCustom`** (script của bạn)

3. **Cấu hình NetworkManager**:
   - **Network Prefabs List**: Add các prefab cần spawn:
     - `NetworkPlayer.prefab` (fallback)
     - Các element prefabs (nếu muốn spawn trực tiếp)

4. **Cấu hình NetworkManagerCustom**:
   - **Server IP**: `127.0.0.1` (localhost)
   - **Server Port**: `2003`

### 2.2. Setup NetworkPlayerSpawner

1. **Tạo GameObject `NetworkPlayerSpawner`** trong GameScene:
   - Add Component: **`NetworkPlayerSpawner`**

2. **Assign Prefabs trong Inspector**:
   - **Network Player Prefab**: `NetworkPlayer.prefab` (fallback)
   - **Fire Male Prefab**: `FirePrefab.prefab` (hoặc prefab tương ứng)
   - **Fire Female Prefab**: Prefab Fire nữ (nếu có)
   - **Water Male Prefab**: `WaterPrefab.prefab`
   - **Water Female Prefab**: Prefab Water nữ (nếu có)
   - **Earth Male Prefab**: `EarthPrefab.prefab`
   - **Earth Female Prefab**: `null` (Earth chỉ có Male)
   - **Wood Male Prefab**: `WoodPrefab.prefab`
   - **Wood Female Prefab**: Prefab Wood nữ (nếu có)
   - **Metal Male Prefab**: `MetalPrefab.prefab`
   - **Metal Female Prefab**: Prefab Metal nữ (nếu có)

3. **Tạo Spawn Points**:
   - Tạo các GameObject `SpawnPoint1`, `SpawnPoint2`, `SpawnPoint3`, ...
   - Đặt vị trí spawn trong scene
   - Assign vào **Spawn Points** array trong `NetworkPlayerSpawner`

### 2.3. Setup GameSceneNetworkInitializer

1. **Tạo GameObject `GameSceneNetworkInitializer`** trong GameScene:
   - Add Component: **`GameSceneNetworkInitializer`**

2. **Script này sẽ tự động**:
   - Load player data từ API khi vào GameScene
   - Lưu vào `GameManager.Instance`
   - Đảm bảo player data được load trước khi spawn

---

## 3️⃣ CẤU HÌNH NETWORKPLAYER PREFAB

### 3.1. Yêu cầu cho mỗi Element Prefab:

Mỗi prefab (FirePrefab, WaterPrefab, etc.) cần có:

1. **NetworkObject** component:
   - **Owner Authority**: `Server` hoặc `Client`
   - **Dont Destroy With Owner**: `false`

2. **NetworkTransform** component:
   - **Sync Position**: `true`
   - **Sync Rotation**: `true` (nếu cần)
   - **Sync Scale**: `false` (thường không cần)

3. **NetworkPlayerDataSync** component:
   - Script này sẽ tự động sync player data từ GameManager

4. **NetworkPlayerController** component:
   - Đồng bộ di chuyển qua ServerRpc

5. **PlayerController** component:
   - Quản lý input và movement

6. **PlayerMovement** component:
   - Xử lý di chuyển

7. **NetworkPlayerHealth** component (nếu có):
   - Đồng bộ HP/MP

### 3.2. Ví dụ cấu trúc NetworkPlayer.prefab:

```
NetworkPlayer (GameObject)
├── NetworkObject
├── NetworkTransform
├── NetworkPlayerDataSync
├── NetworkPlayerController
├── PlayerController
│   └── PlayerStats (ScriptableObject reference)
├── PlayerMovement
├── PlayerAnimator
├── Rigidbody2D
├── Collider2D
└── SpriteRenderer
```

---

## 4️⃣ FLOW HOẠT ĐỘNG

### 4.1. Flow khi vào GameScene:

```
1. Login → LoadPlayerData → MainMenu
2. MainMenu → Click "Join Game"
3. NetworkManagerCustom.ConnectToServer()
4. Load GameScene
5. GameSceneNetworkInitializer.Start()
   ├── Check GameManager.Instance.HasPlayerData()
   ├── Nếu chưa có → LoadPlayerDataFromAPI()
   └── Lưu vào GameManager.Instance
6. NetworkManager.OnClientConnectedCallback
7. NetworkPlayerSpawner.SpawnPlayer(clientId)
   ├── GetPlayerPrefabForClient(clientId)
   │   ├── Lấy playerData từ GameManager.Instance
   │   ├── element_type + gender → Chọn prefab
   │   └── Return prefab tương ứng
   ├── Instantiate(prefab, spawnPos)
   └── NetworkObject.SpawnWithOwnership(clientId)
8. NetworkPlayerDataSync.OnNetworkSpawn()
   ├── IsServer → LoadPlayerDataFromGameManager()
   │   └── Set NetworkVariable từ playerData
   └── ApplyPlayerData() → Apply vào PlayerController, NetworkPlayerHealth
```

### 4.2. Đồng bộ Player Data:

- **Server**: Load player data từ `GameManager.Instance` → Set `NetworkVariable`
- **Clients**: Nhận `NetworkVariable` → Apply vào `PlayerController`, `NetworkPlayerHealth`
- **Khi stats thay đổi**: Gọi `UpdatePlayerDataServerRpc()` để update

### 4.3. Đồng bộ Di chuyển:

- **Owner Client**: Đọc input → Gọi `MoveServerRpc()`
- **Server**: Xử lý movement → `NetworkTransform` tự động sync position
- **Remote Clients**: Nhận position từ `NetworkTransform` → Update transform

---

## 5️⃣ TEST NETCODE

### 5.1. Test Localhost (Host Mode):

1. **Mở scene `GameScene`** trong Unity Editor
2. **Play** (sẽ chạy Host mode nếu có button StartHost)
3. **Hoặc tạo button** gọi `NetworkManagerCustom.StartHost()`

### 5.2. Test với ParrelSync (2 Clients):

1. **Editor 1 (Host)**:
   - Mở scene `GameScene`
   - Click button **Start Host** (hoặc tự động start)
   - Login với account 1

2. **Editor 2 (Client)**:
   - Mở ParrelSync clone
   - Mở scene `GameScene`
   - Click button **Connect to Server**
   - Login với account 2

3. **Kiểm tra**:
   - Cả 2 players đều spawn đúng prefab (dựa trên element_type + gender)
   - Di chuyển được đồng bộ
   - Stats được đồng bộ (HP, Level, etc.)

### 5.3. Test Dedicated Server:

1. **Build Server**:
   - File → Build Settings → Switch Platform → Windows
   - Build (chọn thư mục build)
   - Chạy file `.exe` → Server sẽ chạy trên port 2003

2. **Editor/Client**:
   - Mở scene `GameScene`
   - Click **Connect to Server**
   - Login và join game

---

## 6️⃣ TROUBLESHOOTING

### 6.1. Player không spawn:

**Kiểm tra**:
- `NetworkPlayerSpawner` có được assign prefabs chưa?
- `GameManager.Instance` có player data chưa?
- Console log có báo lỗi gì không?

**Fix**:
- Đảm bảo `GameSceneNetworkInitializer` đã load player data trước khi spawn
- Check `NetworkPlayerSpawner.GetPlayerPrefabForClient()` có return đúng prefab không

### 6.2. Spawn sai prefab:

**Kiểm tra**:
- `element_type` và `gender` trong database có đúng không?
- Prefabs trong Inspector có được assign đúng không?

**Fix**:
- Check `GameManager.Instance.GetPlayerData()` trả về đúng `element_type` và `gender`
- Đảm bảo prefabs trong `NetworkPlayerSpawner` được assign đúng

### 6.3. Stats không đồng bộ:

**Kiểm tra**:
- `NetworkPlayerDataSync` có được add vào prefab chưa?
- `NetworkVariable` có được set trên server chưa?

**Fix**:
- Đảm bảo `NetworkPlayerDataSync` component có trong prefab
- Check `LoadPlayerDataFromGameManager()` có được gọi trên server không

### 6.4. Di chuyển không đồng bộ:

**Kiểm tra**:
- `NetworkTransform` có được add vào prefab chưa?
- `NetworkPlayerController` có được add vào prefab chưa?

**Fix**:
- Đảm bảo `NetworkTransform` component có trong prefab
- Check `MoveServerRpc()` có được gọi từ owner client không

---

## 7️⃣ TÍCH HỢP VỚI API

### 7.1. Khi player level up:

```csharp
// Trong script xử lý level up
NetworkPlayerDataSync dataSync = GetComponent<NetworkPlayerDataSync>();
if (dataSync != null && IsOwner)
{
    dataSync.UpdatePlayerDataServerRpc(
        playerId, elementType, gender, characterName,
        newLevel, newHp, newMaxHp, newMp, newMaxMp, newAttack, newMoveSpeed
    );
}
```

### 7.2. Khi player nhận damage:

```csharp
// Trong NetworkPlayerHealth
[ServerRpc(RequireOwnership = false)]
public void TakeDamageServerRpc(int damage)
{
    networkHp.Value = Mathf.Max(0, networkHp.Value - damage);
    // NetworkVariable tự động sync cho tất cả clients
}
```

### 7.3. Sync với database:

- **Khi stats thay đổi**: Gọi API để update database
- **Khi disconnect**: Lưu stats cuối cùng vào database
- **Khi connect**: Load stats từ database → Set vào NetworkVariable

---

## 8️⃣ TÓM TẮT CHECKLIST

### Setup Scene:
- [ ] NetworkManager với UnityTransport
- [ ] NetworkManagerCustom component
- [ ] NetworkPlayerSpawner với prefabs assigned
- [ ] Spawn Points created và assigned
- [ ] GameSceneNetworkInitializer component

### Setup Prefabs:
- [ ] NetworkPlayer.prefab có đầy đủ components
- [ ] Element prefabs có NetworkObject, NetworkTransform
- [ ] Element prefabs có NetworkPlayerDataSync
- [ ] Element prefabs có NetworkPlayerController

### Test:
- [ ] Login → Load player data → MainMenu
- [ ] Join Game → Connect to server
- [ ] Player spawn đúng prefab (element_type + gender)
- [ ] Stats đồng bộ (HP, Level, etc.)
- [ ] Di chuyển đồng bộ giữa clients

---

## 9️⃣ LƯU Ý QUAN TRỌNG

1. **Player Data phải được load TRƯỚC khi spawn**:
   - `GameSceneNetworkInitializer` load data từ API
   - `NetworkPlayerSpawner` sử dụng data từ `GameManager.Instance`

2. **NetworkVariable chỉ server mới có quyền write**:
   - Client không thể trực tiếp set `NetworkVariable.Value`
   - Phải dùng `ServerRpc` để request server update

3. **Owner Authority**:
   - Chỉ owner mới xử lý input
   - Server xử lý movement và sync qua NetworkTransform

4. **Prefab Selection**:
   - Dựa trên `element_type` + `gender` từ database
   - Fallback về `NetworkPlayer.prefab` nếu không tìm thấy

---

## 🔟 NEXT STEPS

Sau khi setup xong, bạn có thể:
1. **Thêm visual sync**: Thay đổi sprite/animator dựa trên element_type + gender
2. **Thêm name tag**: Hiển thị character_name trên đầu player
3. **Thêm combat sync**: Đồng bộ tấn công, skill, etc.
4. **Thêm inventory sync**: Đồng bộ inventory qua NetworkVariable
5. **Thêm chat system**: Chat giữa players qua NetworkVariable hoặc Custom Messages

---

**Chúc bạn setup thành công! 🎮**
