# Hướng Dẫn: Multi-Map, Zone, Config Enemy & NPC

> **Dự án:** DoAn — Unity (Netcode for GameObjects) + ASP.NET Core API + MySQL  
> **Phạm vi:** Tài liệu này **tập trung vào setup trong Unity** — cấu hình DB xem file `db_migration_map.sql`.

---

## Mục Lục

1. [Config Nhiều Map](#1-config-nhiều-map)
2. [Phân Vùng Zone — Inspector-Driven](#2-phân-vùng-zone--inspector-driven)
3. [Config Enemy — Wiring HP từ DB](#3-config-enemy--wiring-hp-từ-db)
4. [Trigger Chuyển Map Biên Trái / Phải](#4-trigger-chuyển-map-biên-trái--phải)
5. [Config NPC — Spawn & Menu](#5-config-npc--spawn--menu)
6. [Config Hình Ảnh Hệ Nguyên Tố](#6-config-hình-ảnh-hệ-nguyên-tố)
7. [Danh Sách File .cs](#7-danh-sách-file-cs)
8. [Checklist Nhanh](#8-checklist-nhanh)

---

## 1. Config Nhiều Map

### 1.1 — Kiến trúc

```
GameScene.unity   map_id = 0  (Làng Khởi Đầu)
Map1.unity        map_id = 1  (Cánh Đồng Lửa)
Map2.unity        map_id = 2  (Rừng Băng)
Map3.unity        map_id = 3  (Sa Mạc Phong)
```

Mỗi map = **1 scene Unity riêng**. `MapManager` tự gọi `GET /api/map/by-scene?scene=<tên>` khi scene load.

> **DB:** Chạy `db_migration_map.sql` để tạo/điền bảng `map_config`.

### 1.2 — Đăng ký scene vào Build Settings

1. Mở **File → Build Settings**
2. Kéo tất cả scene vào danh sách
3. Tên scene phải **khớp chính xác** với `scene_name` trong bảng `map_config`

### 1.3 — MapManager.cs (Setup Inspector)

> File: `Client/Assets/Scripts/Map/MapManager.cs` — đã implement.

Gắn vào 1 **persistent GameObject** (ví dụ: GameManager). Script tự fetch `map_id` từ API mỗi khi scene load.

```
Inspector:
  MapManager.cs:
    apiBase = "http://localhost:5000"
```

Nếu API fail → fallback về `mapId` set trong Inspector (mặc định 0).

---

## 2. Phân Vùng Zone — Inspector-Driven

### 2.1 — Mô hình Zone (1 port)

```
Map1.unity
  Zone A   roomId = "map1_zone0"   |
  Zone B   roomId = "map1_zone1"   |-- 1 NGO server :7777
  Zone C   roomId = "map1_zone2"   |
```

Toàn bộ cấu hình zone set **trong Unity Inspector** — không cần DB, không cần API.  
Player bước qua `BoxCollider2D` của `ZoneTrigger` → gửi `ServerRpc` → server teleport + cập nhật room.

> **Không còn bảng `map_zone_config`** — đã xóa (chạy `db_migration_map.sql`).

---

### 2.2 — Các file .cs tham gia

| File | Đặt ở | Vai trò |
|---|---|---|
| `ZoneTrigger.cs` | scene, trên BoxCollider2D tại ranh giới | Client: phát hiện player đi qua, gửi ServerRpc |
| `PlayerZoneHandler.cs` | **Player Prefab** | Chứa ServerRpc đổi zone + NetworkVariable CurrentRoomId |
| `ZoneRoomManager.cs` | persistent GameObject (server) | Server: theo dõi client nào đang ở zone nào |
| `RoomBroadcast.cs` | không attach, dùng như static utility | Server: lọc ClientRpc chỉ gửi đến đúng zone |

---

### 2.3 — Luồng hoạt động khi player đổi zone

```
1. Player bước vào BoxCollider2D của ZoneTrigger_A_to_B
2. [Client] ZoneTrigger → PlayerZoneHandler.RequestZoneChangeServerRpc("map1_zone1", 22, 0)
3. [Server] PlayerZoneHandler nhận RPC:
       a. ZoneRoomManager.AssignClientToRoom(clientId, "map1_zone1")
       b. CurrentRoomId.Value = "map1_zone1"   ← sync xuống tất cả client
       c. transform.position = (22, 0, 0)       ← teleport, server-authoritative
       d. OnZoneChangedClientRpc(...)            ← callback riêng về client đó
4. [Client owner] nhận OnZoneChangedClientRpc → có thể hiện thông báo / ẩn loading
```

---

### 2.4 — Bước 1: Thêm ZoneRoomManager vào scene

> File: `Client/Assets/Scripts/Map/ZoneRoomManager.cs` — đã có.

1. Tạo Empty GameObject, đặt tên `ZoneRoomManager`
2. Add Component → `ZoneRoomManager`
3. Đặt object này ở **persistent scene** hoặc scene đầu tiên load (DontDestroyOnLoad tự xử lý)

```
Hierarchy (persistent / DontDestroyOnLoad)
└── ZoneRoomManager
      ZoneRoomManager.cs  ← không có field nào cần set
```

> Script chạy **chỉ trên server**. Client không cần làm gì với script này.

---

### 2.5 — Bước 2: Thêm PlayerZoneHandler vào Player Prefab

> File: `Client/Assets/Scripts/Player/PlayerZoneHandler.cs` — đã có.

1. Mở **Player Prefab** trong Project window
2. Add Component → `PlayerZoneHandler`
3. Không có field nào cần set trong Inspector

```
Player Prefab
  NetworkObject       ← đã có
  NetworkPlayerController
  PlayerZoneHandler   ← THÊM MỚI (không cần config)
    [auto] CurrentRoomId : NetworkVariable<FixedString64Bytes>
```

**Sau khi đổi zone thành công**, `PlayerZoneHandler.CurrentRoomId.Value` được sync xuống tất cả client — có thể dùng để:
- Hiện tên zone trên UI: `zoneLabel.text = zoneHandler.RoomId;`
- Kiểm tra 2 player có cùng zone: `handlerA.IsSameRoom(handlerB)`

---

### 2.6 — Bước 3: Tạo ZoneTrigger trên scene

> File: `Client/Assets/Scripts/Map/ZoneTrigger.cs` — đã có.

1. Tạo Empty GameObject tại **ranh giới giữa 2 zone**, đặt tên `ZoneTrigger_A_to_B`
2. Add Component → `BoxCollider2D` → tick **Is Trigger = true**
3. Chỉnh size BoxCollider2D thành dải dọc hẹp (ví dụ: Width = 1, Height = 10)
4. Add Component → `ZoneTrigger`
5. Điền vào Inspector:

```
Inspector — ZoneTrigger.cs:
  [Zone đích]
  Room Id   =  "map1_zone1"          ← định danh nội bộ (server dùng)
  Zone Name =  "Đồng Bằng Lửa"      ← TÊN HIỆN LÊN UI cho người chơi
                                        (để trống = không hiện banner)
  [Vị trí spawn]
  Spawn X   =  22
  Spawn Y   =  0
```

6. Tạo thêm trigger **chiều ngược lại** `ZoneTrigger_B_to_A` với:

```
  Room Id   =  "map1_zone0"
  Zone Name =  "Khu Vực Làng"
  Spawn X   =  18
  Spawn Y   =  0
```

> **Quy tắc đặt tên roomId:** `map{mapId}_zone{index}` — ví dụ `map1_zone0`, `map1_zone1`.  
> `Zone Name` thì đặt tự do theo ý nghĩa địa điểm — đây là chuỗi hiển thị lên UI, không lưu DB.

---

### 2.6b — Bước 3b: Setup ZoneNameBanner UI

> File mới: `Client/Assets/Scripts/Map/ZoneNameBanner.cs`

Khi player bước qua trigger, banner xuất hiện giữa trên màn hình → hiện **tên zone** → fade out sau 3 giây.

**Setup trong Unity:**

1. Trong Canvas (HUD), tạo Empty GameObject → đặt tên `ZoneNameBanner`
2. Add Component → `Image` (nền tối mờ) — màu `(0, 0, 0, 180)`
3. Trong `ZoneNameBanner`, tạo child `ZoneNameText` → Add Component → `TextMeshPro - Text (UI)`
   - Font Size: 28, Alignment: Center, Bold
4. Add Component → `CanvasGroup` (để fade)
5. Add Component → `ZoneNameBanner.cs` → kéo các field:

```
Inspector — ZoneNameBanner.cs:
  Zone Name Text    → kéo TextMeshPro "ZoneNameText"
  Display Duration  =  3          ← giây hiển thị trước khi fade
  Canvas Group      → kéo CanvasGroup của panel này
```

6. **Tắt active** của `ZoneNameBanner` GameObject (script tự bật khi cần)

```
Canvas (HUD)
└── ZoneNameBanner              ← Panel, mặc định Inactive
      Image (nền mờ)
      CanvasGroup
      ZoneNameBanner.cs:
        zoneNameText  → ZoneNameText
        displayDuration = 3
        canvasGroup   → CanvasGroup
      └── ZoneNameText          ← TMP_Text
            Font Size: 28
            Text: "Đồng Bằng Lửa"   ← preview
```

**Kết quả:** Mỗi khi player vào zone mới có `Zone Name` được điền → banner hiện tên khu vực → fade out sau 3 giây.

---

### 2.7 — Bước 4: Hierarchy hoàn chỉnh trong Map1.unity

```
Map1.unity  Hierarchy
│
├── [Persistent - DontDestroyOnLoad]
│     └── ZoneRoomManager
│           ZoneRoomManager.cs
│
├── ZoneBoundaries                    ← parent gom tất cả trigger
│   ├── ZoneTrigger_A_to_B            ← ranh giới Zone A / Zone B (x = 20)
│   │     BoxCollider2D: isTrigger=true, Size (1, 20)
│   │     ZoneTrigger.cs:
│   │       Room Id = "map1_zone1"
│   │       Spawn X = 22
│   │       Spawn Y = 0
│   │
│   ├── ZoneTrigger_B_to_A            ← chiều ngược (cùng vị trí, offset nhỏ)
│   │     BoxCollider2D: isTrigger=true, Size (1, 20)
│   │     ZoneTrigger.cs:
│   │       Room Id = "map1_zone0"
│   │       Spawn X = 18
│   │       Spawn Y = 0
│   │
│   ├── ZoneTrigger_B_to_C            ← ranh giới Zone B / Zone C (x = 60)
│   │     ZoneTrigger.cs:
│   │       Room Id = "map1_zone2"
│   │       Spawn X = 62
│   │       Spawn Y = 0
│   │
│   └── ZoneTrigger_C_to_B
│         ZoneTrigger.cs:
│           Room Id = "map1_zone1"
│           Spawn X = 58
│           Spawn Y = 0
│
└── Player Prefab (đã có PlayerZoneHandler.cs)
```

---

### 2.8 — Bước 5: Dùng RoomBroadcast để lọc damage theo zone

Khi broadcast damage/effect, chỉ gửi đến client **trong cùng zone** với enemy:

```csharp
// NetworkEnemyHealth.cs — chỉ gửi sync đến đúng zone
[ServerRpc(RequireOwnership = false)]
public void TakeDamageServerRpc(int damage)
{
    CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - Mathf.Max(0, damage));

    // Lấy room của enemy này (gắn EnemyZoneTag khi spawn)
    string myRoom = GetComponent<EnemyZoneTag>()?.RoomId ?? "";

    var target = RoomBroadcast.ToRoom(myRoom, ZoneRoomManager.Instance);
    ShowDamageClientRpc(damage, target);  // chỉ client trong zone nhận

    if (CurrentHealth.Value <= 0) Die();
}

[ClientRpc]
private void ShowDamageClientRpc(int damage, ClientRpcParams rpcParams = default)
{
    // Hiện damage popup
}
```

> `EnemyZoneTag` là component đơn giản: `public string RoomId;` — gắn vào enemy và set `RoomId` khi spawn trong `NetworkEnemySpawner`.

---

### 2.9 — Kiểm tra hoạt động

Sau khi setup xong, chạy game (host + 1 client):

1. **Player bước qua trigger** → Console server phải in:  
   `[ZoneRoomManager] Client 1 → room 'map1_zone1' (tổng trong room: 1)`
2. **Client nhận callback** → Console client phải in:  
   `[PlayerZoneHandler] Đã vào zone 'map1_zone1' tại (22.0, 0.0)`
3. **Player teleport** đến `(22, 0)` ngay lập tức (server-authoritative)

---

## 3. Config Enemy — Wiring HP từ DB

> **DB:** `enemy` + `enemy_spawns` trong `db_migration_map.sql`.

Server trả `base_hp` từ API. Sau khi spawn, gọi `InitHealth(base_hp)` để đồng bộ lên client.

### 3.1 — NetworkEnemyHealth.cs

```csharp
// Client/Assets/Scripts/Network/Enemy/NetworkEnemyHealth.cs
public class NetworkEnemyHealth : NetworkBehaviour
{
    public NetworkVariable<int> CurrentHealth = new(10,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> MaxHealth = new(10,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Gọi từ NetworkEnemySpawner sau Spawn() — truyền base_hp từ API
    public void InitHealth(int maxHp)
    {
        if (!IsServer) return;
        MaxHealth.Value     = maxHp;
        CurrentHealth.Value = maxHp;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - Mathf.Max(0, damage));
        if (CurrentHealth.Value <= 0) Die();
    }

    private void Die()
    {
        OnDeath?.Invoke();
        GetComponent<NetworkObject>()?.Despawn(true);
    }

    public event System.Action OnDeath;
}
```

### 3.2 — Trong NetworkEnemySpawner.cs

```csharp
// Sau dòng networkObj.Spawn()
var health = enemyObj.GetComponent<NetworkEnemyHealth>();
if (health != null)
    health.InitHealth(spawnData.enemy.base_hp);  // base_hp từ API /api/enemy/{id}
```

---
// Client/Assets/Scripts/Network/Enemy/NetworkEnemyHealth.cs
public class NetworkEnemyHealth : NetworkBehaviour
{
    public NetworkVariable<int> CurrentHealth = new(10,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> MaxHealth = new(10,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public void InitHealth(int maxHp)
    {
        if (!IsServer) return;
        MaxHealth.Value     = maxHp;
        CurrentHealth.Value = maxHp;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - Mathf.Max(0, damage));
        if (CurrentHealth.Value <= 0) Die();
    }

    private void Die() { OnDeath?.Invoke(); GetComponent<NetworkObject>()?.Despawn(true); }
    public event System.Action OnDeath;
}
```

### 3.2 — Trong NetworkEnemySpawner.cs

```csharp
// Sau dòng networkObj.Spawn()
var health = enemyObj.GetComponent<NetworkEnemyHealth>();
if (health != null)
    health.InitHealth(spawnData.enemy.base_hp);
```

---

## 4. Trigger Chuyển Map Biên Trái / Phải

> **Thay thế hoàn toàn** `MapTransitionButton.cs` (nút UI). Player chỉ cần bước tới rìa màn hình — giống cơ chế `ZoneTrigger` đã có.

### 4.1 — Kiến trúc & Phân công vai trò

| Script | Dùng ở đâu | Mục đích |
|---|---|---|
| `MapEdgeTrigger.cs` | **Biên trái/phải** của mỗi map | Trigger nhẹ, player bước vào → tự lookup portal → load scene |
| `MapPortalTrigger.cs` | Cổng phó bản / dungeon | Cần `portalId` cụ thể, hỗ trợ chìa khóa + visual effect — **KHÔNG dùng cho biên map thường** |
| `MapTransitionButton.cs` | ~~Canvas UI~~ | ❌ **Xóa khỏi scene** — không còn cần |

> **DB:** Không thay đổi — bảng `map_portal` + cột `portal_direction` vẫn dùng như cũ.

---

### 4.2 — Luồng hoạt động

```
1. Player bước vào BoxCollider2D của EdgeRight (rìa phải màn hình)
2. [Client] MapEdgeTrigger → GET /api/map/portal/direction?mapId=0&direction=right
   → nhận portal_id của portal đó
3. [Client] POST /api/map/travel { portal_id, player_id, current_map_id, player_x, player_y }
   → server validate vị trí hợp lệ
   → trả { success, dest_scene_name, dest_map_id, dest_x, dest_y }
4. [Client] lưu PortalArrivalHandler.Pending* → NetworkManager.Shutdown() → SceneManager.LoadScene()
5. Scene mới load → PortalArrivalHandler.ApplyPendingArrival() đặt player đúng vị trí
```

---

### 4.3 — MapEdgeTrigger.cs (file mới)

> **File:** `Client/Assets/Scripts/Map/MapEdgeTrigger.cs`

```csharp
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Trigger biên map trái/phải — đặt tại rìa scene, player bước vào tự chuyển map.
/// Không cần config portalId thủ công — tự lookup từ API theo direction.
///
/// Inspector:
///   direction       = "left" hoặc "right"
///   currentMapId    = 0 để tự lấy từ MapManager
///   transitionDelay = 0.5 (giây chờ trước khi load)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MapEdgeTrigger : MonoBehaviour
{
    [Header("Hướng di chuyển")]
    [Tooltip("'left' = đi map trước, 'right' = đi map tiếp theo")]
    [SerializeField] private string direction = "right";

    [Tooltip("MapId của scene này. Để 0 → tự lấy từ MapManager.")]
    [SerializeField] private int currentMapId = 0;

    [Header("UX")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private float transitionDelay = 0.5f;

    [Header("API")]
    [SerializeField] private string apiBase = "http://localhost:5000";

    private bool _isTransitioning = false;

    private void Start()
    {
        if (currentMapId == 0 && MapManager.Instance != null)
            currentMapId = MapManager.Instance.GetMapId();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTransitioning) return;
        if (!other.CompareTag("Player")) return;

        var netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner) return;

        StartCoroutine(DoTravel(other.gameObject));
    }

    private IEnumerator DoTravel(GameObject player)
    {
        _isTransitioning = true;
        if (loadingPanel) loadingPanel.SetActive(true);

        // ── Bước 1: tìm portal theo direction ──
        string url = $"{apiBase}/api/map/portal/direction?mapId={currentMapId}&direction={direction}";
        using var portalReq = UnityWebRequest.Get(url);
        portalReq.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(\"JWT_TOKEN\")}");
        yield return portalReq.SendWebRequest();

        if (portalReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[MapEdgeTrigger] Không có portal '{direction}' tại map {currentMapId}.");
            ResetTrigger();
            yield break;
        }

        var portal = JsonUtility.FromJson<PortalInfo>(portalReq.downloadHandler.text);

        // ── Bước 2: validate travel với server ──
        Vector3 pos = player.transform.position;
        var payload = new TravelPayload
        {
            portal_id      = portal.portal_id,
            player_id      = GetLocalPlayerId(player),
            current_map_id = currentMapId,
            player_x       = pos.x,
            player_y       = pos.y
        };

        string json = JsonUtility.ToJson(payload);
        using var travelReq = new UnityWebRequest($"{apiBase}/api/map/travel", "POST");
        travelReq.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        travelReq.downloadHandler = new DownloadHandlerBuffer();
        travelReq.SetRequestHeader("Content-Type", "application/json");
        travelReq.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString(\"JWT_TOKEN\")}");
        yield return travelReq.SendWebRequest();

        if (travelReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[MapEdgeTrigger] Travel lỗi: {travelReq.downloadHandler.text}");
            ResetTrigger();
            yield break;
        }

        var resp = JsonUtility.FromJson<TravelResponse>(travelReq.downloadHandler.text);
        if (!resp.success)
        {
            Debug.LogWarning($"[MapEdgeTrigger] Server từ chối: {resp.message}");
            ResetTrigger();
            yield break;
        }

        // ── Bước 3: lưu tọa độ đến → shutdown NGO → load scene ──
        PortalArrivalHandler.PendingDestX  = resp.dest_x;
        PortalArrivalHandler.PendingDestY  = resp.dest_y;
        PortalArrivalHandler.PendingMapId  = resp.dest_map_id;

        yield return new WaitForSeconds(transitionDelay);

        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsClient || nm.IsHost || nm.IsServer))
            nm.Shutdown();

        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene(resp.dest_scene_name);
    }

    private void ResetTrigger()
    {
        _isTransitioning = false;
        if (loadingPanel) loadingPanel.SetActive(false);
    }

    private int GetLocalPlayerId(GameObject player)
    {
        var pd = player.GetComponent<PlayerDataHolder>();
        return pd != null ? pd.PlayerId : PlayerPrefs.GetInt("USER_ID", -1);
    }

    [Serializable] private class PortalInfo    { public int portal_id; }
    [Serializable] private class TravelPayload
    {
        public int portal_id, player_id, current_map_id;
        public float player_x, player_y;
    }
    [Serializable] private class TravelResponse
    {
        public bool   success;
        public string message;
        public int    dest_map_id;
        public string dest_scene_name;
        public float  dest_x, dest_y;
    }
}
```

---

### 4.4 — Setup trong Unity Inspector

#### Cấu trúc GameObject

```
GameScene (map_id = 0)
└── MapEdges/
    └── EdgeRight                    ← rìa PHẢI màn hình (x = rìa phải map)
          BoxCollider2D:
            isTrigger = true             ← BẮT BUỘC check này!
            Size: (1, 30)                ← chiều cao đủ bao toàn bộ màn hình
          MapEdgeTrigger.cs:
            direction     = "right"
            currentMapId  = 0            ← SỐ CỤ THỂ (GameScene = 0)
            transitionDelay = 0.5
    ← KHÔNG có EdgeLeft (map_id=0 là map đầu tiên)

Map1 (map_id = 1)
└── MapEdges/
    ├── EdgeLeft
    │     BoxCollider2D: isTrigger = true, Size (1, 30)
    │     MapEdgeTrigger.cs:
    │       direction     = "left"
    │       currentMapId  = 1            ← SỐ CỤ THỂ (Map1 = 1)
    └── EdgeRight
          BoxCollider2D: isTrigger = true, Size (1, 30)
          MapEdgeTrigger.cs:
            direction     = "right"
            currentMapId  = 1            ← SỐ CỤ THỂ (Map1 = 1)

Map2 (map_id = 2)
└── MapEdges/
    ├── EdgeLeft
    │     MapEdgeTrigger.cs: direction = "left",  currentMapId = 2
    └── EdgeRight           ← chỉ tạo nếu tồn tại Map3
          MapEdgeTrigger.cs: direction = "right", currentMapId = 2
```

> **Quy tắc:** Map đầu tiên chỉ có `EdgeRight`. Map cuối chỉ có `EdgeLeft`. Map giữa có cả hai.  
> **QUAN TRỌNG:** `currentMapId` phải set **số cụ thể** (0/1/2/3). Để `-1` chỉ khi chắc chắn MapManager đã fetch xong.

#### Kích thước BoxCollider2D

```
Inspector — BoxCollider2D:
  Is Trigger = true  ← BẮT BUỘC
  Offset:  X = 0, Y = 0
  Size:    X = 1     ← dải mỏng đủ để player chắc chắn chạm vào
           Y = 30    ← cao hơn chiều cao màn hình (camera height ≈ 12–18 units)
```

> Đặt trigger **sát rìa ngoài cùng của tilemap** — player bước ra ngoài tilemap là trigger ngay, không cần thêm bước.

---

### 4.4b — Lý do vì sao trigger không hoạt động (3 lỗi phổ biến)

#### Lỗi #1 — Race Condition `currentMapId` (nguyên nhân hàng đầu)

```
MapManager.Start() → StartCoroutine(FetchMapConfigByScene)  ← async, chờ HTTP
MapEdgeTrigger.Start() → GetMapId()                         ← chạy cùng frame!
                                               ↓
                              MapManager chưa fetch xong → trả về 0
                              currentMapId = 0 dù đang ở Map1
                              → query mapId=0&direction=right (SAI)
```

**Đã sửa:** Không còn dùng `Start()` để resolve mapId. `DoTravel()` được gọi khi player bước đến rìa (vài giây sau khi game chạy), lúc đó MapManager đã fetch xong.  
**SẹR hơn:** Set `currentMapId` số cụ thể trong Inspector — không cần phụ thuộc MapManager.

#### Lỗi #2 — Detection logic sai so với ZoneTrigger

```csharp
// CODE CŨ (sai) — CompareTag có thể fail nếu Player tag chưa set
if (!other.CompareTag("Player")) return;
var netObj = other.GetComponent<NetworkObject>();
if (netObj != null && !netObj.IsOwner) return;  // BUG: nếu netObj=null → vẫn qua!

// CODE MỚI (khớp với ZoneTrigger)
if (!other.TryGetComponent<NetworkObject>(out var netObj)) return;  // Không có NetworkObject → bỏ qua
if (!netObj.IsOwner) return;                                         // Không phải owner → bỏ qua
```

#### Lỗi #3 — Coroutine bị kill khi scene unload

`MapEdgeTrigger` là MonoBehaviour bình thường trong scene. Khi `SceneManager.LoadScene()` được gọi:

```
Frame hiện tại:
  MapEdgeTrigger.DoTravel() coroutine chạy ...
  → nm.Shutdown()
  → SceneManager.LoadScene("Map1")  ← scene cũ scheduled để unload

Frame tiếp theo:
  Unity unload GameScene → destroy tất cả GameObject trong scene
  → MapEdgeTrigger bị destroy
  → Coroutine bị kill ở đây!
  → NetworkManager.StartHost() KHÔNG bao giờ được gọi trong scene mới
  → Scene mới load nhưng NGO chết → không spawn được player
```

**Đã sửa:** `MapEdgeTrigger` không còn tự làm Shutdown/LoadScene. Thay vào đó nó spawn `MapTravelHelper` — một GameObject tạm thời với `DontDestroyOnLoad` — để chạy toàn bộ flow `Shutdown → LoadSceneAsync → StartHost`. Helper tự destroy khi xong.

#### Lỗi #4 — `PortalArrivalHandler.ApplyPendingArrival()` không ai gọi

```
PortalArrivalHandler.PendingDestX/Y được set trước khi load scene
→ Scene mới load
→ StartHost() → player prefab spawn
→ NetworkPlayerController.OnNetworkSpawn() không gọi ApplyPendingArrival
→ Player spawn tại vị trí mặc định (0,0), không phải dest_x/dest_y từ server
```

**Đã sửa:** `NetworkPlayerController.OnNetworkSpawn()` khi `IsOwner` gọi `PortalArrivalHandler.ApplyPendingArrival(transform)` ngay đầu.

---

### 4.5 — DB: Dữ liệu map_portal cần có

> Bảng `map_portal` trong `db_migration_map.sql`.

```sql
-- GameScene (map_id=0) → Map1 (map_id=1), chỉ có portal phải
INSERT INTO map_portal
  (src_map_id, dest_map_id, dest_scene_name, dest_x, dest_y,
   portal_type, portal_direction, is_active)
VALUES
  (0, 1, 'Map1', 2.0, 0.0, 'map_transition', 'right', 1);

-- Map1 (map_id=1) → GameScene, portal trái
INSERT INTO map_portal
  (src_map_id, dest_map_id, dest_scene_name, dest_x, dest_y,
   portal_type, portal_direction, is_active)
VALUES
  (1, 0, 'GameScene', -2.0, 0.0, 'map_transition', 'left', 1);

-- Map1 (map_id=1) → Map2 (map_id=2), portal phải
INSERT INTO map_portal
  (src_map_id, dest_map_id, dest_scene_name, dest_x, dest_y,
   portal_type, portal_direction, is_active)
VALUES
  (1, 2, 'Map2', 2.0, 0.0, 'map_transition', 'right', 1);
```

> `dest_x`, `dest_y` = vị trí spawn player **trong scene đích** (gần rìa đối diện portal).

---

### 4.6 — Kiểm tra hoạt động

1. Player chạy tới rìa phải GameScene  
   → Console: `[MapEdgeTrigger] ...` không có lỗi  
   → Scene chuyển sang `Map1`
2. Player spawn tại `(2.0, 0.0)` trong Map1 (tọa độ từ `dest_x`/`dest_y` của portal)
3. Chạy ngược lại sang trái → về GameScene tại `(-2.0, 0.0)`

---
<!-- 
## 5. Config NPC — Spawn & Menu (Server-Authoritative NGO)

> **Kiến trúc NGO:** Chỉ Server/Host được phép `Spawn()` NetworkObject. Client **KHÔNG** được tự gọi API hay tự spawn NPC trực tiếp — làm vậy sẽ bị từ chối hoặc desync.  
> **DB:** `npc_config`, `npc_dialogue`, `npc_shop_item` trong `db_migration_map.sql`.

---

### 5.0 — Tổng quan luồng hoạt động

```
[Server khi OnNetworkSpawn]
  NpcServerManager → GET /api/npc/list?mapId=X
    → Instantiate NPC prefab
    → NetworkObject.Spawn()        ← CHỈ server làm được
    → NPC hiện trên màn hình TẤT CẢ client tự động

[Client click NPC]
  NpcInteraction.OnPointerClick()
    → pre-check khoảng cách (UX nhẹ, không authoritative)
    → InteractServerRpc(npcNetworkId)
      [Server nhận RPC]
      → validate khoảng cách thật (chống gian lận)
      → lấy NpcData từ cache server-side
      → OpenMenuClientRpc(npcDataJson)  ← gửi về ĐÚNG client đó (targeted)
        [Client nhận ClientRpc]
        → NpcMenuUI.Instance.Open(npcData)   ← render UI bình thường

[Client bấm Mua]
  → BuyItemServerRpc(itemId, quantity)
    [Server nhận]
    → validate quyền, số vàng, tồn kho
    → POST /api/npc/shop/buy
    → BuyResultClientRpc(success, message, newGold)
      [Client nhận]
      → NpcMenuUI.OnBuyResult(...)   ← hiện thông báo kết quả
```

---

### 5.1 — NpcData.cs — Shared DTO

> **File:** `Client/Assets/Scripts/NPC/NpcData.cs`  
> Dùng chung cho server lẫn client. Phải `[Serializable]` để `JsonUtility` serialize khi truyền qua RPC.

```csharp
using System;

[Serializable]
public class NpcData
{
    public int    npc_id;
    public string npc_name;
    public string npc_type;       // "shop" | "blacksmith" | "quest" | "exchange" | "event"
    public int    npc_type_id;    // index vào npcPrefabs array trong Inspector
    public float  pos_x;
    public float  pos_y;
    public string dialogue_text;
}

[Serializable]
public class NpcListResponse
{
    public NpcData[] data;
}
```

> **Ghi chú truyền qua RPC:** NGO yêu cầu struct hoặc `INetworkSerializable` cho tham số `[ClientRpc]`/`[ServerRpc]`. Cách đơn giản nhất là serialize thành JSON string trước khi truyền — xem ví dụ trong `5.3`.

---

### 5.2 — NpcServerManager.cs (thay thế NpcSpawner.cs)

> **File:** `Client/Assets/Scripts/NPC/NpcServerManager.cs`  
> Gắn vào một **persistent GameObject** (ví dụ: cùng GameObject với `NetworkManager` hoặc `GameManager`).  
> Chỉ thực thi khi `IsServer` — client bỏ qua toàn bộ script này.

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

/// <summary>
/// Server-authoritative NPC manager — thay thế NpcSpawner.cs.
/// Chỉ chạy trên server/host. Client không được phép spawn NPC trực tiếp.
/// </summary>
public class NpcServerManager : NetworkBehaviour
{
    [Header("API")]
    [SerializeField] private string apiBase = "http://localhost:5000";
    [SerializeField] private int    mapId   = 0;

    [Header("NPC Prefabs — index = npc_type_id trong DB")]
    [SerializeField] private GameObject[] npcPrefabs;
    // npcPrefabs[0] = NPC_Shop_Prefab
    // npcPrefabs[1] = NPC_Blacksmith_Prefab
    // npcPrefabs[2] = NPC_Quest_Prefab
    // npcPrefabs[3] = NPC_Exchange_Prefab
    // npcPrefabs[4] = NPC_Event_Prefab

    // Server-side cache: NetworkObjectId → NpcData
    private readonly Dictionary<ulong, NpcData> _npcCache = new();

    public string ApiBase => apiBase;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;   // CLIENT không làm gì cả
        StartCoroutine(LoadAndSpawnNpcs());
    }

    private IEnumerator LoadAndSpawnNpcs()
    {
        string url = $"{apiBase}/api/npc/list?mapId={mapId}";
        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[NpcServerManager] GET {url} thất bại: {req.error}");
            yield break;
        }

        var response = JsonUtility.FromJson<NpcListResponse>(req.downloadHandler.text);
        if (response?.data == null) yield break;

        foreach (var npc in response.data)
        {
            var prefab = GetPrefabByTypeId(npc.npc_type_id);
            if (prefab == null)
            {
                Debug.LogWarning($"[NpcServerManager] Không có prefab cho npc_type_id={npc.npc_type_id}. Bỏ qua '{npc.npc_name}'.");
                continue;
            }

            var obj    = Instantiate(prefab, new Vector3(npc.pos_x, npc.pos_y, 0f), Quaternion.identity);
            var netObj = obj.GetComponent<NetworkObject>();
            if (netObj == null)
            {
                Debug.LogError($"[NpcServerManager] Prefab '{prefab.name}' thiếu NetworkObject component!");
                Destroy(obj);
                continue;
            }

            netObj.Spawn();   // chỉ server/host gọi được — client nhận bản sao tự động

            // Truyền data xuống component trên prefab
            var interaction = obj.GetComponent<NpcInteraction>();
            if (interaction != null)
                interaction.InitOnServer(npc);

            // Cache phục vụ validate sau
            _npcCache[netObj.NetworkObjectId] = npc;

            Debug.Log($"[NpcServerManager] Spawned '{npc.npc_name}' ({npc.npc_type}) tại ({npc.pos_x}, {npc.pos_y})");
        }
    }

    private GameObject GetPrefabByTypeId(int typeId)
    {
        if (npcPrefabs == null || typeId < 0 || typeId >= npcPrefabs.Length) return null;
        return npcPrefabs[typeId];
    }

    /// <summary>NpcInteraction dùng để lấy data từ server cache khi validate.</summary>
    public bool TryGetNpcData(ulong networkObjectId, out NpcData data)
        => _npcCache.TryGetValue(networkObjectId, out data);
}
```

```
Inspector — NpcServerManager.cs:
  apiBase           = "http://localhost:5000"   (đổi thành VPS IP khi deploy)
  mapId             = 1                          (mapId của scene này — đặt số cụ thể)
  npcPrefabs:
    Element 0       → NPC_Shop_Prefab
    Element 1       → NPC_Blacksmith_Prefab
    Element 2       → NPC_Quest_Prefab
    Element 3       → NPC_Exchange_Prefab
    Element 4       → NPC_Event_Prefab
```

---

### 5.3 — NpcInteraction.cs (server-authoritative)

> **File:** `Client/Assets/Scripts/NPC/NpcInteraction.cs`  
> **Đặt trên NPC Prefab** cùng với `NetworkObject`. Kế thừa `NetworkBehaviour`.

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using Unity.Netcode;

/// <summary>
/// NPC click handler — NGO server-authoritative.
/// Client click → ServerRpc → server validate → ClientRpc trả data về đúng client đó.
/// </summary>
public class NpcInteraction : NetworkBehaviour, IPointerClickHandler
{
    private NpcData _npcData;   // server-side: set bởi NpcServerManager.InitOnServer()

    private const float MAX_INTERACT_DIST   = 3.5f;
    private const float LENIENCY_MULTIPLIER = 1.5f;  // hệ số leniency cho lag mạng

    /// <summary>Gọi bởi NpcServerManager ngay sau Spawn() — chỉ chạy trên server.</summary>
    public void InitOnServer(NpcData data) => _npcData = data;

    // ──────────────────────────────────────────────────────
    //  CLIENT — Click handler
    // ──────────────────────────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        // Pre-check khoảng cách ở client để UX nhanh hơn (không authoritative)
        var localPlayer = GetLocalPlayerTransform();
        if (localPlayer == null) return;

        if (Vector3.Distance(localPlayer.position, transform.position) > MAX_INTERACT_DIST)
        {
            Debug.Log("[NpcInteraction] Quá xa để tương tác.");
            return;
        }

        // Gửi NetworkObjectId lên server — server sẽ validate lại
        InteractServerRpc(NetworkObjectId);
    }

    // ──────────────────────────────────────────────────────
    //  SERVER — Validate + gửi data về đúng client
    // ──────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(ulong npcNetworkId, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!NetworkManager.ConnectedClients.TryGetValue(senderClientId, out var client)) return;
        var playerObj = client.PlayerObject;
        if (playerObj == null) return;

        // Server validate khoảng cách thật (chống gian lận)
        float dist = Vector3.Distance(playerObj.transform.position, transform.position);
        if (dist > MAX_INTERACT_DIST * LENIENCY_MULTIPLIER)
        {
            Debug.LogWarning($"[NpcInteraction] Client {senderClientId} quá xa ({dist:F1}u). Từ chối.");
            return;
        }

        // Ưu tiên lấy từ NpcServerManager cache; fallback về _npcData cục bộ
        NpcData dataToSend = _npcData;
        var serverManager = FindObjectOfType<NpcServerManager>();
        if (serverManager != null && serverManager.TryGetNpcData(npcNetworkId, out var cached))
            dataToSend = cached;

        if (dataToSend == null) return;

        // Serialize thành JSON string để truyền qua ClientRpc (tránh INetworkSerializable)
        string json = JsonUtility.ToJson(dataToSend);

        var targetParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderClientId } }
        };
        OpenMenuClientRpc(json, targetParams);
    }

    [ClientRpc]
    private void OpenMenuClientRpc(string npcDataJson, ClientRpcParams clientRpcParams = default)
    {
        var npcData = JsonUtility.FromJson<NpcData>(npcDataJson);
        NpcMenuUI.Instance?.Open(npcData, this);   // truyền 'this' để menu biết NpcInteraction nào đang mở
    }

    // ──────────────────────────────────────────────────────
    //  SERVER — Xử lý mua hàng
    // ──────────────────────────────────────────────────────

    [ServerRpc(RequireOwnership = false)]
    public void BuyItemServerRpc(int itemId, int quantity, ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        StartCoroutine(ProcessBuyOnServer(senderClientId, itemId, quantity));
    }

    private IEnumerator ProcessBuyOnServer(ulong clientId, int itemId, int quantity)
    {
        // Lấy userId server-side (không tin client tự khai)
        int userId = ServerPlayerDataManager.Instance != null
            ? ServerPlayerDataManager.Instance.GetUserIdForClient(clientId)
            : 0;

        if (userId == 0)
        {
            SendBuyResult(clientId, false, "Không tìm được thông tin người chơi.", 0);
            yield break;
        }

        string apiBase = FindObjectOfType<NpcServerManager>()?.ApiBase ?? "http://localhost:5000";
        string body    = JsonUtility.ToJson(new BuyRequest
        {
            player_id = userId,
            npc_id    = _npcData?.npc_id ?? 0,
            item_id   = itemId,
            quantity  = quantity
        });

        using var req = new UnityWebRequest($"{apiBase}/api/npc/shop/buy", "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<BuyResponse>(req.downloadHandler.text);
            SendBuyResult(clientId, result.success, result.message, result.new_gold);
        }
        else
        {
            SendBuyResult(clientId, false, "Lỗi kết nối đến server.", 0);
        }
    }

    private void SendBuyResult(ulong clientId, bool success, string message, int newGold)
    {
        var p = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
        BuyResultClientRpc(success, message, newGold, p);
    }

    [ClientRpc]
    private void BuyResultClientRpc(bool success, string message, int newGold,
        ClientRpcParams clientRpcParams = default)
    {
        NpcMenuUI.Instance?.OnBuyResult(success, message, newGold);
    }

    // ──────────────────────────────────────────────────────
    //  Utility
    // ──────────────────────────────────────────────────────

    private Transform GetLocalPlayerTransform()
    {
        var localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        return localObj != null ? localObj.transform : null;
    }

    [System.Serializable] private class BuyRequest  { public int player_id, npc_id, item_id, quantity; }
    [System.Serializable] private class BuyResponse { public bool success; public string message; public int new_gold; }
}
```

---

### 5.4 — NpcMenuUI.cs — Thay đổi tối thiểu

`NpcMenuUI.cs` là **pure UI layer** — toàn bộ Canvas setup trong Unity **giữ y nguyên 100%**. Chỉ cần 3 thay đổi nhỏ:

**1. Signature `Open()` nhận thêm tham chiếu `NpcInteraction`:**

```csharp
private NpcInteraction _currentInteraction;

public void Open(NpcData npcData, NpcInteraction interaction)
{
    _currentInteraction = interaction;
    npcNameText.text    = npcData.npc_name;
    dialogueText.text   = npcData.dialogue_text;
    // ... hiện panel như cũ
}
```

**2. Nút Buy gọi `ServerRpc` thay vì gọi API trực tiếp:**

```csharp
// Trong hàm xử lý click nút Mua — KHÔNG gọi API trực tiếp
public void OnClickBuyItem(int itemId, int quantity)
{
    _currentInteraction?.BuyItemServerRpc(itemId, quantity);
}
```

**3. Thêm callback nhận kết quả mua:**

```csharp
public void OnBuyResult(bool success, string message, int newGold)
{
    ShowNotification(success ? message : $"Lỗi: {message}");
    if (success) UpdateGoldDisplay(newGold);
}
```

**Canvas setup trong Unity — không đổi:**

```
Canvas (Screen Space Overlay)
└── NpcMenuUI (GameObject)
      NpcMenuUI.cs — assign tất cả:
        mainPanel         → Panel chính
        npcNameText       → TMP_Text tên NPC
        dialogueText      → TMP_Text hội thoại
        btnBuy            → Button "Mua hàng"
        btnSell           → Button "Bán đồ"
        btnClose          → Button "X"
        shopPanel         → Panel shop (ScrollView)
        shopItemContainer → Content của ScrollRect
        shopItemRowPrefab → Prefab 1 dòng (ItemName, Price, BtnBuy)
```

---

### 5.5 — Setup NPC Prefab trong Unity

```
NPC_Shop_Prefab
  NetworkObject        ← BẮT BUỘC — thiếu thì server không Spawn() được
  SpriteRenderer       ← hình NPC
  BoxCollider2D        ← vật lý collision (isTrigger = false)
  NpcInteraction.cs    ← click handler + RPC logic
  CircleCollider2D     ← (tuỳ chọn) detect player lại gần để hiện prompt "Nhấn E để nói chuyện"
    isTrigger = true
    radius    = 3.5
```

> **Quan trọng:** Mỗi NPC prefab phải được thêm vào **NetworkManager → NetworkPrefabs** list. Thiếu bước này thì `Spawn()` sẽ báo lỗi "Prefab not registered".

---

### 5.6 — Các loại NPC (`npc_type`)

| npc_type | npc_type_id | Chức năng |
|---|---|---|
| `shop` | 0 | Mua/bán item thường — hiện nút Buy/Sell |
| `blacksmith` | 1 | Nâng cấp trang bị |
| `quest` | 2 | Phát/nhận nhiệm vụ |
| `exchange` | 3 | Trao đổi item đặc biệt |
| `event` | 4 | NPC sự kiện theo mùa |

`npc_type_id` phải **khớp với index** trong mảng `npcPrefabs` của `NpcServerManager` trong Inspector.

---

### 5.7 — Tóm tắt phân quyền

| Hành động | Ai thực hiện | Cách thực hiện |
|---|---|---|
| Fetch danh sách NPC từ API | **Server** | `NpcServerManager.LoadAndSpawnNpcs()` |
| Spawn NPC vào scene | **Server** | `NetworkObject.Spawn()` — client nhận bản sao tự động |
| Click tương tác | **Client** | `OnPointerClick` → `InteractServerRpc` |
| Validate khoảng cách thật | **Server** | Trong `InteractServerRpc` |
| Gửi NpcData về client | **Server** | `OpenMenuClientRpc` (targeted — đúng client đó) |
| Render menu NPC | **Client** | `NpcMenuUI.Open(data, interaction)` |
| Gửi lệnh mua | **Client** | `BuyItemServerRpc(itemId, quantity)` |
| Validate & gọi API mua | **Server** | `ProcessBuyOnServer` coroutine |
| Cập nhật UI kết quả mua | **Client** | `BuyResultClientRpc` → `NpcMenuUI.OnBuyResult` |

---

### 5.8 — Kiểm tra hoạt động

1. Server start → Console: `[NpcServerManager] Spawned 'Lái Buôn' (shop) tại (10, 0)`
2. NPC xuất hiện trên màn hình tất cả client (NGO replicate tự động qua `NetworkObject.Spawn()`)
3. Client click NPC trong phạm vi 3.5u → `InteractServerRpc` gửi lên
4. Console server: không warning "quá xa" → `OpenMenuClientRpc` gửi về đúng client đó
5. Menu NPC mở trên client đó — **các client khác không thấy menu của nhau**
6. Bấm Mua → `BuyItemServerRpc` → server gọi API → `BuyResultClientRpc` → notification kết quả

--- -->

## 6. Config Hình Ảnh Hệ Nguyên Tố

### 6.1 — Đặt file ảnh trong Unity

```
Assets/Resources/Elements/
  icon_hoa.png    (#FF4500)
  icon_thuy.png   (#00BFFF)
  icon_moc.png    (#228B22)
  icon_tho.png    (#A0522D)
  icon_kim.png    (#C0C0C0)
  icon_phong.png  (#9370DB)
```

> `icon_path` trong DB không có đuôi `.png` — VD: `"Elements/icon_hoa"`.

### 6.2 — ElementIconLoader.cs

> File: `Client/Assets/Scripts/UI/ElementIconLoader.cs`  
> Gắn vào **GameManager** (persistent), tự load khi game khởi động.

```
Inspector:
  ElementIconLoader.cs:
    apiBase = "http://localhost:5000"
```

Tự gọi `GET /api/element-type` → load Sprite từ `Resources.Load<Sprite>(icon_path)` → cache.

### 6.3 — Dùng icon trong Enemy HUD

```csharp
void RefreshElementIcon(string elementType)
{
    if (ElementIconLoader.Instance == null || !ElementIconLoader.Instance.IsLoaded) return;
    var icon = ElementIconLoader.Instance.GetIcon(elementType);
    if (icon != null) elementIconImage.sprite = icon;
    healthBarFill.color = ElementIconLoader.Instance.GetColor(elementType);
}
```

---

## 7. Danh Sách File .cs

### Unity Client

| File | Trạng thái | Mô tả |
|---|---|---|
| `Map/MapManager.cs` | ✅ | Singleton, auto-fetch mapId khi scene load |
| `Map/ZoneTrigger.cs` | ✅ Rewrite | Inspector-driven: roomId + zoneName + spawnX/Y, không gọi API |
| `Map/ZoneNameBanner.cs` | ✅ Mới | UI banner hiện tên zone, singleton, fade out |
| `Map/ZoneRoomManager.cs` | ✅ | Server-side room assignment |
| `Map/RoomBroadcast.cs` | ✅ | Utility lọc ClientRpc theo zone |
| `Map/MapEdgeTrigger.cs` | ✅ Mới | Trigger biên map trái/phải, inspector-driven, tự lookup portal |
| `Map/MapTravelHelper.cs` | ✅ Mới | DontDestroyOnLoad helper: Shutdown → LoadSceneAsync → StartHost |
| `Map/MapPortalTrigger.cs` | ✅ | Portal phó bản/dungeon (cần portalId + chìa khóa) |
| ~~`Map/MapTransitionButton.cs`~~ | ❌ Xóa | Thay bằng `MapEdgeTrigger` |
| `Player/PlayerZoneHandler.cs` | ✅ | NetworkBehaviour xử lý zone switch ServerRpc |
| `NPC/NpcData.cs` | ✅ Mới | Shared DTO `[Serializable]`, dùng chung server + client, truyền qua RPC dưới dạng JSON string |
| `NPC/NpcServerManager.cs` | ✅ Rewrite | NetworkBehaviour, chỉ chạy khi `IsServer` — fetch API + `NetworkObject.Spawn()` NPC |
| `NPC/NpcInteraction.cs` | ✅ Rewrite | NetworkBehaviour, `ServerRpc`/`ClientRpc` — client click → server validate → client render |
| `NPC/NpcMenuUI.cs` | ✅ | Dialogue + shop UI — giữ nguyên Canvas, thêm `OnBuyResult()` callback |
| `UI/ElementIconLoader.cs` | ✅ | Load + cache icon hệ nguyên tố |

### Backend

| File | Ghi chú |
|---|---|
| `Controllers/MapController.cs` | Đã xóa `/api/map/zone`; thêm `by-scene`, `portal/direction` dùng cột `portal_direction` |
| `Models/Entities/MapPortal.cs` | Thêm `PortalDirection` (left/right/none) |
| `Data/GameDbContext.cs` | Xóa `MapZoneConfigs` DbSet; map `portal_direction` |

---

## 8. Checklist Nhanh

### Unity (mỗi scene mới)

- [ ] Scene tạo xong, đăng ký **Build Settings**, tên khớp `scene_name` trong DB
- [ ] `MapManager.cs` gắn vào persistent GameObject (1 lần cho toàn game)
- [ ] `NpcServerManager.cs` gắn vào persistent GameObject, set `mapId` + assign `npcPrefabs` (index khớp `npc_type_id`)
- [ ] Mỗi NPC prefab có `NetworkObject` component + đăng ký vào **NetworkManager → NetworkPrefabs** list
- [ ] `NpcMenuUI.cs` trên Canvas, assign tất cả UI fields
- [ ] `MapEdgeTrigger.cs` tại **rìa trái/phải** scene: `direction = "left"/"right"`, `currentMapId` = mapId scene (hoặc để 0)
- [ ] `ZoneTrigger.cs` tại ranh giới zone: điền `roomId`, `zoneName`, `spawnX`, `spawnY` trong Inspector
- [ ] `ZoneNameBanner` Panel trong Canvas HUD: assign `zoneNameText`, `canvasGroup`, để mặc định Inactive
- [ ] `PlayerZoneHandler.cs` gắn vào Player Prefab
- [ ] `ElementIconLoader.cs` gắn vào GameManager, set `apiBase`
- [ ] 6 sprite PNG vào `Assets/Resources/Elements/`

### Database (1 lần)

- [ ] Chạy `db_migration_map.sql` trên database `gamedb`
- [ ] `map_config.scene_name` khớp tên scene Unity
- [ ] `map_portal.portal_direction` = `'left'` / `'right'`
- [ ] `enemy_spawns` có spawn point cho map
- [ ] `npc_config` + `npc_shop_item` + `npc_dialogue` thêm xong

### Backend

- [ ] `GET /api/map/by-scene?scene=GameScene` trả đúng `map_id`
- [ ] `GET /api/map/portal/direction?mapId=1&direction=right` trả portal
- [ ] `POST /api/map/travel` validate vị trí + trả `dest_scene_name`
- [ ] `GET /api/npc/list?mapId=1` trả NPC list
- [ ] `POST /api/npc/shop/buy` có `[Authorize]`
