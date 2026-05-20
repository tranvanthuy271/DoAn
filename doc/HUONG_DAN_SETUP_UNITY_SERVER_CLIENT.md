# HƯỚNG DẪN SETUP UNITY: SERVER-CLIENT + CHUYỂN MAP/ZONE

> Kiến trúc: 1 Unity server process — 1 port — nhiều maps/zones (LangLa-style)  
> Yêu cầu: Unity NGO (Netcode for GameObjects) ≥ 1.5, UnityTransport

---

## TỔNG QUAN LUỒNG

```
[Client bước vào ZoneTransitionTrigger]
         ↓
[RequestZoneTransferServerRpc → Server]
         ↓
[ZoneTransitionController.ExecuteTransferToRoom()]
  ├─ Cập nhật ZoneRoomRegistry
  ├─ Cập nhật ZonePlayerSessionManager.UpdateZone()
  └─ TeleportToZoneClientRpc → đúng client đó
         ↓
[ClientSceneController.HandleZoneTeleport()]
  ├─ Load scene mới (nếu đổi map)
  └─ Reposition player tại entry point
```

---

## PHẦN 1 — SETUP SCRIPTABLEOBJECT `MapWorldConfig.asset`

File asset đã có tại: `Assets/ScriptableObjects/MapWorldConfig.asset`

Mở file trong Inspector và điền theo hướng dẫn dưới.

### 1.1. Server Network

| Field | Giá trị mặc định | Ý nghĩa |
|---|---|---|
| `Listen Address` | `0.0.0.0` | Giữ nguyên |
| `Port` | `7777` | Port server, client dùng port này để connect |
| `Api Base Url` | `http://localhost:5247/api` | URL GameServerApi |
| `Public Ip` | `127.0.0.1` | IP server (khi deploy: IP thật của server) |

### 1.2. Security (Dev)

| Field | Giá trị | Ý nghĩa |
|---|---|---|
| `Jwt Secret Dev Only` | (khớp với appsettings) | JWT signing key — chỉ điền khi dev |
| `Zone Api Key Dev Only` | `dev-key` | Server → API key |

> **Production**: để trống 2 field này, đặt env var `JWT_SECRET` và `ZONE_API_KEY`.

### 1.3. Zone Defaults

| Field | Giá trị gợi ý | Ý nghĩa |
|---|---|---|
| `Shared Map Default Zone Count` | `3` (dev) / `15` (prod) | Số khu mặc định mỗi map thường |
| `Shared Map Max Players` | `50` | Max player mỗi khu thường |
| `Instance Map Max Players` | `4` | Max player mỗi phòng phó bản |
| `Fallback Map Id` | `0` | Map trả về khi zone lỗi |
| `Fallback Zone Id` | `0` | Zone trả về trong map fallback |

### 1.4. Thêm Maps

Nhấn **+** ở mảng `Maps` để thêm từng map. Mỗi phần tử là một `MapDefinition`:

**Ví dụ Map thường (làng khởi đầu):**
```
Map Id              = 0
Map Name            = LangKhoiDau
Scene Name          = GameScene        ← tên scene Unity CHÍNH XÁC
Zone Topology       = SharedPublic     ← tự sinh khu mặc định
Allow Custom Zones  = false
Public Zone Count Override    = 0      ← 0 = dùng sharedMapDefaultZoneCount
Public Zone Max Players Override = 0   ← 0 = dùng sharedMapMaxPlayers
Allow Player Zone Switch = true        ← cho switch khu công khai
Entry Points        = [ (12, 5) ]      ← vị trí spawn khi vào map này
```

**Ví dụ Map khác (đồng cỏ):**
```
Map Id              = 1
Map Name            = DongCo
Scene Name          = Map1
Zone Topology       = SharedPublic
Allow Custom Zones  = false
Public Zone Count Override    = 2      ← chỉ 2 khu, không cần nhiều
Entry Points        = [ (5, 2), (30, 2) ]  ← 2 entry point: bên trái và bên phải
```

**Ví dụ Map phó bản (chỉ zone riêng):**
```
Map Id              = 20
Map Name            = PhoBanLuaTang1
Scene Name          = Dungeon_Fire_1
Zone Topology       = InstanceOnly     ← KHÔNG sinh khu công khai
Allow Custom Zones  = true
Custom Zone Max Players Override = 4
Allow Player Zone Switch = false
Entry Points        = [ (0, 0) ]
```

---

## PHẦN 2 — SETUP SERVER SCENE

Dùng scene `HostScene.unity` hoặc tạo scene mới tên `ServerScene`.

### 2.1. NetworkManager GameObject

GameObject đã có tên `NetworkManager`. Kiểm tra gắn đúng:
- ✅ `NetworkManager` component
- ✅ `UnityTransport` component
- ✅ `Network Prefabs List` → chọn `Assets/ScriptableObjects/NetworkPrefabsList.asset`
- ✅ `Connection Approval` = **Bật** (checkbox trong NetworkManager Inspector)

### 2.2. GameObject "ServerBootstrap"

Tạo Empty GameObject tên `ServerBootstrap`. Gắn các component:

```
ServerBootstrap
├─ MapWorldBootstrap       ← entry point server mới
│    Config = [MapWorldConfig.asset]
├─ ZoneConnectionApprovalV2  ← gắn thêm (MapWorldBootstrap sẽ Init tự động)
└─ ZoneRoomRegistry          ← gắn thêm (MapWorldBootstrap sẽ Init + gọi Initialize)
```

> **Không cần** điền gì thêm. `MapWorldBootstrap.StartServerRoutine()` sẽ tự gọi  
> `registry.Initialize(config)` và `approval.Initialize(config)` khi server boot.

### 2.3. Prefab "NetworkManagers" (cho RPC)

`ZoneTransitionController` và `ZonePlayerSessionManager` là `NetworkBehaviour` — cần là **NetworkObject** để nhận/gửi RPC. Tạo như sau:

**Tạo prefab:**
1. Hierarchy → tạo Empty GameObject tên `NetworkManagers`
2. Add Component: `NetworkObject`
3. Add Component: `ZoneTransitionController`
4. Add Component: `ZonePlayerSessionManager`
   - Field `Config` → kéo `MapWorldConfig.asset` vào
   - Field `Player Prefabs` → đây là **mảng**, mỗi entry = 1 prefab tương ứng 1 hệ/giới tính:

   | Element Type | Gender | Is Hybrid | Prefab |
   |---|---|---|---|
   | `Fire` | `He` | ☐ | `He/Hoa.prefab` |
   | `Water` | `He` | ☐ | `He/Thuy.prefab` |
   | `Earth` | `He` | ☐ | `He/Tho.prefab` |
   | `Wood` | `He` | ☐ | `He/Moc.prefab` |
   | `Metal` | `He` | ☐ | `He/Kim.prefab` |
   | `Wind` | `He` | ☐ | `He/Phong.prefab` |
   | `Fire` | `He` | ☑ | `Fusion/F_Hoa.prefab` |
   | `Water` | `He` | ☑ | `Fusion/F_Thuy.prefab` |
   | *(tương tự các hệ Fusion còn lại)* | | | |

   > **Lookup logic**: server chọn prefab theo `element_type` + `gender` + `is_hybrid` từ PlayerData. Hybrid có `hybrid_prefab_path` trong DB → server dùng `Resources.Load()` trước, fallback về array nếu fail.
5. **Tạo Prefab**: trong Project window mở thư mục `Assets/Prefabs/`, rồi **kéo GameObject `NetworkManagers` từ Hierarchy thả vào đó** → Unity tự tạo file `NetworkManagers.prefab`
   > ⚠️ Đây là Prefab (file .prefab), **khác hoàn toàn** ScriptableObject (.asset). ScriptableObject là data asset (như `MapWorldConfig.asset`). Prefab là template cho GameObject — có thể Instantiate lúc runtime.
6. Xóa GameObject `NetworkManagers` khỏi Hierarchy (giữ lại file .prefab trong Assets là đủ)

**Đăng ký prefab vào NetworkPrefabsList:**
1. Mở `Assets/ScriptableObjects/NetworkPrefabsList.asset`
2. Thêm prefab `NetworkManagers` vào danh sách

**Spawn khi server start — thêm vào `MapWorldBootstrap.cs`:**

```csharp
// Trong StartServerRoutine(), SAU dòng "bool started = NetworkManager.Singleton.StartServer();"
// thêm:
GameObject managersGo = Instantiate(networkManagersPrefab);
managersGo.GetComponent<NetworkObject>().Spawn();
```

Và thêm field vào `MapWorldBootstrap`:
```csharp
[Header("Network Managers Prefab")]
[SerializeField] private GameObject _networkManagersPrefab;
```

Sau khi thêm field, Inspector của `ServerBootstrap` GO sẽ có slot `Network Managers Prefab` — kéo prefab vừa tạo vào.

---

## PHẦN 3 — SETUP CLIENT (Persistent Object)

### 3.1. Tạo "ClientBootstrap" GameObject

Trong **LoginScene** (scene đầu tiên load) hoặc **GameScene**, tạo Empty GameObject tên `ClientBootstrap`:

```
ClientBootstrap
└─ ClientSceneController    ← DontDestroyOnLoad, nhận TeleportRpc từ server
     Loading Screen Prefab = (nếu có prefab màn loading, kéo vào; có thể để null)
```

Đảm bảo `ClientBootstrap` tồn tại **trước** khi client connect đến server, vì `ZoneTransitionController` sẽ gọi `ClientSceneController.Instance.HandleZoneTeleport(...)`.

### 3.2. Cập nhật connection payload

Khi client connect đến server, cần gửi payload JSON đúng format mà `ZoneConnectionApprovalV2` parse được:

```json
{ "token": "<JWT>", "mapId": 0, "zoneId": 0 }
```

Trong `GameSceneClientInitializer.cs` (hoặc bất kỳ script nào đang gọi `StartClient()`), tìm chỗ set `ConnectionData` và thay bằng:

```csharp
string jwt = PlayerPrefs.GetString("JWT_TOKEN", "");
int mapId  = PlayerPrefs.GetInt("PLAYER_MAP_ID", 0);
int zoneId = PlayerPrefs.GetInt("PLAYER_ZONE_ID", 0);

string payload = $"{{\"token\":\"{jwt}\",\"mapId\":{mapId},\"zoneId\":{zoneId}}}";
NetworkManager.Singleton.NetworkConfig.ConnectionData =
    System.Text.Encoding.UTF8.GetBytes(payload);

// Sau đó mới StartClient
NetworkManager.Singleton.StartClient();
```

Đồng thời đổi port kết nối sang `7777` (port của `MapWorldConfig`) thay vì port cũ `2003`:
```csharp
var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
transport.SetConnectionData("127.0.0.1", 7777);
```

---

## PHẦN 4 — PLAYER PREFAB

### 4.1. Kiểm tra Player prefab có đủ các component

Mở prefab player của bạn (trong `Assets/Prefabs/Player/`) và đảm bảo có:

```
PlayerPrefab
├─ NetworkObject         ← bắt buộc
├─ NetworkTransform      ← sync vị trí
├─ NetworkVisibilityZoneFilter   ← để lọc visibility theo zone
└─ [script player của bạn]
```

`NetworkVisibilityZoneFilter` đã có tại `Assets/Scripts/Network/Shared/NetworkVisibilityZoneFilter.cs` — gắn vào prefab player.

### 4.2. Đăng ký player prefab

Mở `Assets/ScriptableObjects/NetworkPrefabsList.asset`, thêm player prefab vào danh sách (nếu chưa có).

---

## PHẦN 5 — SETUP CHUYỂN MAP TRONG SCENE

### 5.1. ZoneTransitionTrigger — chuyển khu trong cùng map

Dùng khi player bước qua ranh giới giữa các khu trong **cùng map** (cùng scene).

**Ví dụ: map 0, khu 0 sang khu 1 (cùng scene GameScene):**

1. Trong scene `GameScene`, tạo Empty GameObject tên `Trigger_Zone0_to_Zone1`
2. Đặt ở ranh giới giữa khu 0 và khu 1
3. Add Component: `BoxCollider2D` (Is Trigger = ✅)
4. Add Component: `ZoneTransitionTrigger`
5. Điền Inspector:
   ```
   Target Map Id     = 0        ← cùng map
   Target Zone Id    = 1        ← khu đích
   Entry Point Id    = 0        ← index trong MapDefinition.Entry Points
   Transition Label  = "Khu0 → Khu1"
   Player Layer Mask = [chọn layer Player]
   ```

**Chiều ngược lại** — tạo trigger thứ 2 ngay cạnh:
```
Target Map Id  = 0
Target Zone Id = 0
Entry Point Id = 0
```

### 5.2. ZoneTransitionTrigger — chuyển sang map khác

Dùng khi player bước sang **scene/map khác** (ví dụ từ làng sang đồng cỏ).

**Ví dụ: map 0 (GameScene) → map 1 (Map1):**

1. Scene `GameScene`: tạo trigger ra đồng cỏ
   ```
   Target Map Id  = 1        ← Map1 ID
   Target Zone Id = 0        ← vào khu 0 của Map1
   Entry Point Id = 0        ← Entry Point đầu tiên của Map1
   ```
2. Scene `Map1`: tạo trigger về làng
   ```
   Target Map Id  = 0        ← GameScene/Làng ID
   Target Zone Id = 0
   Entry Point Id = 0        ← quay về làng, entry point 0
   ```

> **Lưu ý**: `Entry Points` trong `MapDefinition` phải khớp với vị trí spawn thực tế trong scene.  
> Ví dụ Map1 có `Entry Points = [(5, 2), (30, 2)]`:
> - `Entry Point Id = 0` → spawn tại (5, 2) — cổng bên trái
> - `Entry Point Id = 1` → spawn tại (30, 2) — cổng bên phải

---

## PHẦN 6 — THÊM TẤT CẢ SCENES VÀO BUILD SETTINGS

1. **File → Build Settings**
2. Thêm các scene theo thứ tự:
   ```
   0: Login
   1: GameScene   (map 0)
   2: Map1        (map 1)
   3: HostScene   (hoặc ServerScene)
   (các scene khác...)
   ```

Tên scene trong `Build Settings` phải **khớp chính xác** với field `Scene Name` trong từng `MapDefinition`.

---

## PHẦN 7 — TEST TRONG EDITOR

### 7.1. Chạy server trong Editor

Unity Editor tự động chạy như server nhờ `#if UNITY_EDITOR` trong `MapWorldBootstrap`:

1. Mở scene `HostScene`
2. Play — Console sẽ hiện:
   ```
   [MapWorldBootstrap] Config → port=7777 ...
   [ZoneRoomRegistry] Loaded ZoneRoom(map0_zone0, type=public, ...)
   [ZoneRoomRegistry] Loaded ZoneRoom(map0_zone1, type=public, ...)
   [MapWorldBootstrap] ✓ Server started — 1 port 7777 cho 2 maps
   ```

### 7.2. Chạy client

Build client hoặc dùng **Multiplayer Play Mode** package của Unity (MPPM):  
1. Cài `com.unity.multiplayer.playmode` từ Package Manager
2. Window → Multiplayer Play Mode → thêm virtual player
3. Play → cả 2 instance cùng chạy trong editor

Hoặc build client riêng rồi chạy client `.exe` kết nối vào editor đang chạy server.

### 7.3. Debug log quan sát transfer

Khi player bước vào `ZoneTransitionTrigger`:
```
[ZoneTransitionTrigger] 'Khu0 → Khu1' → map=0 zone=1 entry=0
[ZoneTransitionController] Client 1 → map0_zone1 (5, 2)
[ClientSceneController] ✓ Zone transfer hoàn thành → map0_zone1
```

---

## CHECKLIST HOÀN THÀNH

- [ ] `MapWorldConfig.asset` đã có ≥ 1 map với `Scene Name` chính xác
- [ ] `MapWorldConfig.asset` field `Jwt Secret Dev Only` khớp với API
- [ ] `ServerBootstrap` GO trong server scene có `MapWorldBootstrap`, `ZoneConnectionApprovalV2`, `ZoneRoomRegistry`
- [ ] Prefab `NetworkManagers` có `NetworkObject` + `ZoneTransitionController` + `ZonePlayerSessionManager`
- [ ] Prefab `NetworkManagers` đã thêm vào `NetworkPrefabsList.asset`
- [ ] `MapWorldBootstrap` có slot `Network Managers Prefab` → spawn khi server start
- [ ] Player prefab có `NetworkVisibilityZoneFilter`
- [ ] `ClientBootstrap` GO với `ClientSceneController` có trong scene đầu (DontDestroyOnLoad)
- [ ] Connection payload gửi JSON `{"token":..,"mapId":..,"zoneId":..}`
- [ ] Port connect đổi sang `7777`
- [ ] Mọi scene trong `MapDefinition.Scene Name` đã có trong Build Settings
- [ ] `ZoneTransitionTrigger` đã đặt tại ranh giới map/zone trong scene

---

## SƠ ĐỒ TỔNG QUÁT

```
HostScene/ServerScene
├─ NetworkManager  [NetworkManager] [UnityTransport]
└─ ServerBootstrap [MapWorldBootstrap] [ZoneConnectionApprovalV2] [ZoneRoomRegistry]
                   MapWorldBootstrap.Start()
                     → registry.Initialize(config)   ← tạo public zones
                     → approval.Initialize(config)   ← đăng ký callback
                     → StartServer()
                     → Spawn(NetworkManagers prefab)

NetworkManagers prefab  [NetworkObject] [ZoneTransitionController] [ZonePlayerSessionManager]
  ← spawned bởi server, tồn tại suốt session

GameScene (map 0)
├─ ZoneTransitionTrigger  targetMapId=0, targetZoneId=1     ← đổi khu
├─ ZoneTransitionTrigger  targetMapId=1, targetZoneId=0     ← sang Map1
└─ ClientBootstrap  [ClientSceneController]  ← DontDestroyOnLoad

Map1 (map 1)
├─ ZoneTransitionTrigger  targetMapId=0, targetZoneId=0     ← về GameScene
└─ ZoneTransitionTrigger  targetMapId=1, targetZoneId=1     ← đổi khu trong Map1
```
