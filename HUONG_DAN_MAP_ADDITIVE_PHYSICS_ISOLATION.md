# Hướng Dẫn Cấu Hình Unity — Per-Map Physics Isolation (Additive Scenes)

## Vấn Đề Đã Giải Quyết

Trước đây tất cả enemy/NPC/projectile của mọi map đều tồn tại trong cùng một Physics2D world (ServerScene). Điều này gây ra:
- Projectile của enemy ở Map 1 trigger `OnTriggerEnter2D` trên player ở Map 0
- Player nhìn thấy visual của projectile/enemy map khác
- Enemy ở map khác target và hitbox chạm vào player

**Giải pháp đã triển khai**: Mỗi map có một Physics2D scene riêng trên server (`LocalPhysicsMode.Physics2D`). Objects ở các map khác nhau **vật lý không thể trigger lẫn nhau**.

---

## Regression Đã Fix Sau Khi Bật Additive Physics

Sau khi thêm physics scenes riêng cho từng map, login có thể hỏng với các lỗi kiểu:
- `Object Scene Migration`
- `Failed to create object locally`
- `NetworkPrefab could not be found`

### Nguyên Nhân

1. `NetworkObject.SceneMigrationSynchronization` mặc định là `true`
  - Khi server gọi `SceneManager.MoveGameObjectToScene(...)` để đưa player/enemy/projectile sang physics scene nội bộ của map, NGO sẽ cố đồng bộ việc đổi scene đó sang client.
  - Nhưng các physics scenes này **chỉ tồn tại trên server**, client không load chúng.
  - Kết quả: client nhận scene migration không hợp lệ và sinh spam lỗi `Object Scene Migration`.

2. `NetworkEnemySpawner_Dedicated` trong `ServerScene` là một `NetworkObject` scene-placed chỉ dùng cho server
  - Client chạy `GameScene`, không có object counterpart của scene object này.
  - Nếu object đó vẫn được spawn cho client trong lúc approve/connect, client sẽ báo lỗi hash/prefab không resolve được.

3. Một số skill spawn projectile rồi mới thêm `NetworkObject`
  - Nếu move scene xảy ra trước khi object có `NetworkObject`, object đó sẽ không được tắt scene sync đúng lúc.

### Giải Pháp

1. `MapSceneManager` giờ tắt NGO scene sync cho mọi `NetworkObject` trước khi move vào server-only physics scene:
  - `ActiveSceneSynchronization = false`
  - `SceneMigrationSynchronization = false`

2. `NetworkEnemySpawner` giờ tự cấu hình scene object dedicated là server-only:
  - `SpawnWithObservers = false`
  - không replicate xuống client nữa

3. `FireRainSkill` giờ đảm bảo `NetworkObject` tồn tại trước khi `MoveToMapScene()`
  - tránh sót cấu hình ở projectile runtime

### Kết Luận

Physics isolation bằng additive scenes vẫn đúng hướng, nhưng phải đi kèm quy tắc này:

> **Server-only physics scene được dùng để simulate vật lý, không được để NGO coi đó là scene gameplay cần đồng bộ xuống client.**

---

## Các File Đã Thay Đổi

| File | Thay Đổi |
|------|----------|
| `Scripts/Network/Server/MapSceneManager.cs` | **MỚI** — Singleton quản lý per-map physics scenes |
| `Scripts/Network/Server/MapWorldBootstrap.cs` | Thêm khởi tạo `MapSceneManager` trong boot sequence |
| `Scripts/Network/Enemy/NetworkEnemySpawner.cs` | `MoveToMapScene()` trước `Spawn()` cho enemy |
| `Scripts/NPC/NpcServerManager.cs` | `MoveToMapScene()` trước `Spawn()` cho NPC |
| `Scripts/Enemy/EnemyAI.cs` | `MoveToMapScene()` trước `Spawn()` cho cả 2 loại projectile |
| `Scripts/Enemy/BossAI.cs` | `MoveToMapScene()` cho add spawns, skillBreath, skillNova |
| `Scripts/Network/Server/ZoneTransitionController.cs` | Player được di chuyển sang scene mới khi zone transfer |
| `Scripts/Network/Server/ZonePlayerSessionManager.cs` | Player được đặt vào đúng scene ngay khi connect |
| `Scripts/Player/Skills/FireRainSkill.cs` | `MoveToMapScene()` cho fireball trước `Spawn()` |
| `Scripts/Player/Skills/EarthBoomerangSkill.cs` | `MoveToMapScene()` cho boomerang trước `Spawn()` |
| `Scripts/Player/Skills/EarthBlinkStrikeSkill.cs` | `MoveToMapScene()` cho DoT projectile trước `Spawn()` |
| `Scripts/Player/Skills/WaterPillarSkill.cs` | `MoveToMapScene()` cho pillar trước `Spawn()` |

---

## Bước 1: Cấu Hình Unity Project Settings

### 1.1 Tắt Auto Physics2D Simulation

Khi dùng `LocalPhysicsMode.Physics2D`, Unity sẽ tự tắt auto-simulation cho scenes đó. Tuy nhiên để đảm bảo, kiểm tra:

> **Edit → Project Settings → Physics 2D**
> - `Simulation Mode`: Chuyển sang **Script** (để `MapSceneManager.FixedUpdate()` điều khiển hoàn toàn)

Nếu để `FixedUpdate` thì global scene vẫn auto-simulate — chỉ local scenes cần manual simulate. Tuy nhiên setting `Script` ở global cũng được vì `MapSceneManager.FixedUpdate()` gọi `Simulate()` cho mọi map scene.

> ⚠️ Nếu chọn `Script` mode cho global Physics2D, **đảm bảo** `MapSceneManager.FixedUpdate()` luôn được gọi (GameObject không bị disable).

### 1.2 Kiểm Tra Physics2D Layer Collision Matrix

> **Edit → Project Settings → Physics 2D → Layer Collision Matrix**

Cấu hình layer isolation cần được giữ nguyên — physics scene isolation xử lý cross-map, layer matrix xử lý cross-type trong cùng map:
- `Player` ↔ `Enemy`: **Enable** (để trigger combat trong cùng map)
- `Player` ↔ `Player`: Tùy theo cấu hình PvP hiện tại

---

## Bước 2: Kiểm Tra GameObject "ServerBootstrap"

Mở **ServerScene** (`Assets/Scenes/ServerScene.unity`):

1. Chọn GameObject **"ServerBootstrap"** (hoặc tên tương đương có `MapWorldBootstrap`)
2. Kiểm tra các component đã có:

```
✅ MapWorldBootstrap
✅ ZoneRoomRegistry      ← tự add trong code
✅ ZoneConnectionApprovalV2 ← tự add trong code
✅ MapSceneManager       ← tự add trong code (KHÔNG cần gán tay)
✅ ZoneServerHeartbeat   ← tự add trong code
```

> `MapSceneManager` sẽ **tự động được AddComponent** bởi `MapWorldBootstrap.StartServerRoutine()`. Không cần gắn tay vào GameObject.

---

## Bước 3: Kiểm Tra MapWorldConfig

`MapWorldConfig` phải có đầy đủ các map definitions. `MapSceneManager.Initialize()` tạo 1 physics scene cho mỗi entry trong `config.maps`.

> **Assets → ScriptableObjects → MapWorldConfig** (hoặc tên tương tự)

Kiểm tra mảng `maps`:
```
maps[0]: mapId = 0, mapName = "Thị Trấn", sceneName = "Map1"
maps[1]: mapId = 1, mapName = "Rừng Tối", sceneName = "Map01"
maps[2]: mapId = 2, mapName = "...", sceneName = "..."
...
```

> ⚠️ Nếu một map không có trong danh sách này, `MapSceneManager` sẽ không tạo physics scene cho nó. Enemy/NPC của map đó vẫn sẽ ở main scene và có thể cross-map trigger!

---

## Bước 4: Kiểm Tra Prefabs Có ZoneOwnerTag

`BossAI` sử dụng `GetComponent<ZoneOwnerTag>()` để lấy mapId trước khi `MoveToMapScene`. Đảm bảo enemy prefabs (đặc biệt Boss) có `ZoneOwnerTag` được gắn bởi spawner:

- `NetworkEnemySpawner.ApplyMapVisibility()` → gọi `ZoneOwnerTag.SetZone(mapId, 0)` ✅
- `NpcServerManager.ApplyMapVisibility()` → tương tự ✅
- Không cần gắn tay vào prefab — spawner tự add component

---

## Bước 5: Build Settings — Không Cần Thay Đổi

Physics isolation xảy ra hoàn toàn **server-side** qua `SceneManager.CreateScene()`. Client không tạo các scenes này. Không cần thêm scene nào vào Build Settings.

---

## Bước 6: Kiểm Tra Sau Deploy

### 6.1 Kiểm Tra Console Khi Server Start

Khi server khởi động thành công, bạn sẽ thấy:
```
[MapSceneManager] ✓ Created physics scene for map 0 (Thị Trấn)
[MapSceneManager] ✓ Created physics scene for map 1 (Rừng Tối)
[MapSceneManager] ✓ Created physics scene for map 2 (...)
[MapSceneManager] ✓ 3 map physics scene(s) ready.
```

Nếu không thấy log này → `MapSceneManager.Initialize()` chưa được gọi. Kiểm tra `MapWorldBootstrap` đã chạy đúng.

### 6.2 Kiểm Tra Enemy Spawn

```
[NetworkEnemySpawner] Spawned 'Goblin' at (10, 5) [copy 1/1] map=1
```
Không thấy warning `[MapSceneManager] Scene cho map X chưa được tạo` → ✅

### 6.3 Test Cross-Map Isolation

1. Kết nối 2 client: **Client A** vào Map 0, **Client B** vào Map 1
2. Để enemy ở Map 1 attack
3. **Kỳ vọng**: Client A KHÔNG bị damage, KHÔNG thấy projectile của enemy Map 1
4. **Kỳ vọng**: Console KHÔNG có warning `[EnemyProjectile] Cross-map blocked` (physics isolation ngăn trigger từ trước)

### 6.4 Test Zone Transition

1. Client A di chuyển từ Map 0 → Map 1 (qua portal/trigger)
2. **Kỳ vọng**: Sau khi transition, Client A bị hit bởi enemy Map 1 (interact đúng)
3. **Kỳ vọng**: Client A KHÔNG còn bị ảnh hưởng bởi enemy Map 0

---

## Cách Hoạt Động (Kỹ Thuật)

```
Server Boot
  └─ MapWorldBootstrap.StartServerRoutine()
       ├─ ZoneRoomRegistry.Initialize(_config)   ← logical zone registry
       ├─ MapSceneManager.Initialize(_config)     ← tạo physics scenes
       │     ├─ CreateScene("ServerMap_0", LocalPhysicsMode.Physics2D)
       │     ├─ CreateScene("ServerMap_1", LocalPhysicsMode.Physics2D)
       │     └─ ...
       └─ StartServer()

Enemy/NPC Spawn
  └─ Instantiate(prefab)
  └─ MapSceneManager.MoveToMapScene(obj, mapId)  ← đặt vào đúng physics world
  └─ NetworkObject.Spawn()                        ← replicate sang clients

Player Connect
  └─ ZonePlayerSessionManager.LoadAndSpawnPlayer()
       └─ MapSceneManager.MoveToMapScene(playerGo, initialMapId)
       └─ netObj.SpawnWithOwnership(clientId)

Player Zone Transfer
  └─ ZoneTransitionController.ExecuteTransferToRoom()
       └─ MapSceneManager.MoveToMapScene(playerGo, newMapId)
       └─ TeleportToZoneClientRpc(...)

Every FixedUpdate
  └─ MapSceneManager.FixedUpdate()
       ├─ ServerMap_0.GetPhysicsScene2D().Simulate(dt)
       ├─ ServerMap_1.GetPhysicsScene2D().Simulate(dt)
       └─ ...

Physics Trigger (e.g. Projectile hits Player)
  └─ OnTriggerEnter2D → CHỈ khi cùng Physics2D scene
       └─ Nếu projectile map=1 và player map=0 → KHÔNG có trigger ← ✅ BUG FIXED
```

---

## Lưu Ý Quan Trọng

### ⚠️ Không Xóa Các mapId Filter Hiện Tại

Các check `EnemyMapId != room.MapId` trong `EnemyProjectile.cs` và `FireballDamage.cs` vẫn được giữ nguyên như **tầng bảo vệ thứ hai** (defense-in-depth). Không xóa chúng.

### ⚠️ Physics2D.OverlapCircle Trong BossAI

`CastAoeSkill()` trong `BossAI` dùng `Physics2D.OverlapCircleAll()` từ **default physics world**. Sau khi isolation, call này sẽ chỉ query objects trong default scene. BossAI nên dùng:

```csharp
// Thay Physics2D.OverlapCircleAll bằng:
var bossScene = MapSceneManager.Instance?.HasScene(bossZoneTag.MapId) == true
    ? scene.GetPhysicsScene2D()
    : null;
var colliders = bossPhysicsScene?.OverlapCircleAll(transform.position, skill.range)
                ?? Physics2D.OverlapCircleAll(transform.position, skill.range);
```

> ℹ️ Với physics isolation, `Physics2D.OverlapCircleAll` trong boss scene sẽ không tìm thấy players ở map khác. Tuy nhiên boss vẫn cần query đúng scene để hit players trong cùng map. Xem mục "Xử Lý Nâng Cao" bên dưới nếu cần.

### ⚠️ MapSceneManager Chỉ Chạy Trên Server

`MapSceneManager` chỉ tạo scenes khi được gọi từ server boot path (`#if UNITY_SERVER || ZONE_SERVER || UNITY_EDITOR`). Client không tạo scenes này.

---

## Xử Lý Nâng Cao (Nếu Cần)

### Truy Vấn Physics2D Trong Đúng Map Scene

Nhiều chỗ trong code dùng `Physics2D.OverlapX()` — các call này mặc định query **default scene**. Sau isolation, player/enemy đã được move sang map scene nên default query có thể miss. Để fix:

```csharp
// Lấy physics scene của map
bool TryGetMapPhysicsScene(int mapId, out PhysicsScene2D physicsScene)
{
    // MapSceneManager không expose PhysicsScene2D trực tiếp — cần extend nếu muốn
    physicsScene = default;
    return false;
}

// Hoặc expose từ MapSceneManager:
public PhysicsScene2D GetPhysicsScene(int mapId)
{
    return _mapScenes.TryGetValue(mapId, out Scene s) && s.IsValid()
        ? s.GetPhysicsScene2D()
        : Physics2D.defaultPhysicsScene;
}
```

Thêm method `GetPhysicsScene(int mapId)` vào `MapSceneManager` nếu cần query physics2D theo map.

### Respawn Enemy

Khi enemy chết và respawn qua `NetworkEnemySpawner.RespawnEnemy()` → gọi `SpawnEnemyAtPoint()` → `MoveToMapScene()` đã được gọi trong đó. ✅ Không cần thêm gì.

### Item Drop Từ Enemy

`EnemyItemDrop.cs` dùng `Instantiate(itemPickupPrefab)` — nếu item drop là NetworkObject và cần physics interaction, cũng cần `MoveToMapScene`. Xem xét nếu item drop bị nhặt cross-map.

---

## Tóm Tắt Checklist

- [ ] Server Console hiển thị `[MapSceneManager] ✓ X map physics scene(s) ready.` khi boot
- [ ] `MapWorldConfig` có đủ tất cả map IDs
- [ ] Enemy spawn log không có `[MapSceneManager] Scene cho map X chưa được tạo`
- [ ] Test: Client A (map 0) không bị hit bởi enemy/projectile của Map 1
- [ ] Test: Zone transition hoạt động đúng (combat interact với đúng map sau khi chuyển)
- [ ] Test: Player skill projectiles không xuyên map
