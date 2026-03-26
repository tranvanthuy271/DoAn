# Hướng Dẫn: Multi-Map, Zone Riêng, Config Enemy & NPC

> **Dự án:** DoAn — Unity (Netcode for GameObjects) + ASP.NET Core API + MySQL  
> **Phạm vi:** Config nhiều map (mỗi map 1 scene riêng), phân vùng zone 1 port, config enemy đầy đủ, nút chuyển map, NPC shop.

---

## Mục Lục

1. [Config Nhiều Map — Mỗi Map Một Scene](#1-config-nhiều-map--mỗi-map-một-scene)
2. [Phân Vùng Zone — Mỗi Khu Có Host Riêng](#2-phân-vùng-zone--mỗi-khu-có-host-riêng)
3. [Config Enemy (HP, EXP, Hệ, Rơi Item)](#3-config-enemy-hp-exp-hệ-rơi-item)
4. [Nút Chuyển Map Trái / Phải](#4-nút-chuyển-map-trái--phải)
5. [Config NPC — Menu & Mua/Bán Item](#5-config-npc--menu--muabán-item)
6. [Config Hình Ảnh Cho Mỗi Hệ Nguyên Tố](#6-config-hình-ảnh-cho-mỗi-hệ-nguyên-tố)
7. [Kiến Trúc Zone: 1 Port + Room ID (ứng dụng)](#7-kiến-trúc-zone-1-port--room-id)
8. [Danh Sách File .cs & Trạng Thái](#8-danh-sách-file-cs--trạng-thái)
9. [Changelog — Lỗi Đã Sửa](#9-changelog--lỗi-đã-sửa)

---

## 1. Config Nhiều Map — Mỗi Map Một Scene

> **Kiến trúc:** Mỗi map là **1 Unity scene riêng biệt**, ánh xạ qua bảng `map_config` trong DB.  
> Player chuyển map → `SceneManager.LoadScene()` → `MapManager` tự fetch `map_id` mới từ API.

### 1.1 — Kiến trúc tổng quan

```
GameScene.unity  ← map_id = 0  (Làng Khởi Đầu)
Map1.unity       ← map_id = 1  (Cánh Đồng Lửa)
Map2.unity       ← map_id = 2  (Rừng Băng)
Map3.unity       ← map_id = 3  (Sa Mạc Phong)
...
```

Mỗi map = **1 scene Unity riêng**. `MapManager` tự gọi `GET /api/map/by-scene?scene=<tên scene>`
khi scene load để lấy `map_id` — không cần set cứng trong code.

### 1.2 — Đăng ký scene vào Build Settings

1. Mở **File → Build Settings**
2. Kéo tất cả scene vào danh sách (thứ tự index không quan trọng vì dùng tên)
3. Scene phải có **tên khớp trường `scene_name` trong DB**

### 1.3 — Bảng `map_config` trong MySQL

```sql
-- Thêm map mới vào DB
INSERT INTO map_config (map_id, map_name, scene_name, spawn_points_json, min_level, max_level)
VALUES
  (0, 'Làng Khởi Đầu', 'GameScene',  '[{"x":0,"y":0},{"x":5,"y":0}]', 1,  10),
  (1, 'Cánh Đồng Lửa', 'Map1',       '[{"x":2,"y":1}]',                5,  20),
  (2, 'Rừng Băng',      'Map2',       '[{"x":0,"y":2}]',                15, 30),
  (3, 'Sa Mạc Phong',   'Map3',       '[{"x":3,"y":0}]',                25, 40);
```

| Trường | Ý nghĩa |
|---|---|
| `map_id` | ID duy nhất, dùng xuyên suốt backend + client |
| `scene_name` | Tên scene Unity (phải khớp chính xác) |
| `spawn_points_json` | JSON array vị trí spawn khi player vào map |
| `min_level / max_level` | Giới hạn level cho phép vào |

### 1.4 — MapManager.cs — Tự động set mapId theo scene

Cập nhật `MapManager.cs` để tự lấy mapId từ scene đang chạy thay vì set cứng:

```csharp
// Client/Assets/Scripts/Map/MapManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [SerializeField] private string apiBase = "http://localhost:5000";

    private int mapId = 0;
    private string mapName = "";

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FetchMapConfigBySceneName(scene.name));
    }

    private IEnumerator FetchMapConfigBySceneName(string sceneName)
    {
        // Gọi API map/by-scene?scene=GameScene
        var url = $"{apiBase}/api/map/by-scene?scene={UnityWebRequest.EscapeURL(sceneName)}";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<MapConfigResponse>(req.downloadHandler.text);
            mapId   = resp.map_id;
            mapName = resp.map_name;
            Debug.Log($"[MapManager] Loaded map: {mapName} (id={mapId})");
        }
        else
        {
            // Fallback: parse mapId từ scene name nếu scene là số (ví dụ: "1" → mapId=1)
            if (int.TryParse(sceneName, out int parsedId))
                mapId = parsedId;
            Debug.LogWarning($"[MapManager] API failed for scene '{sceneName}', using mapId={mapId}");
        }
    }

    public int  GetMapId()   => mapId;
    public string GetMapName() => mapName;

    [System.Serializable]
    private class MapConfigResponse
    {
        public int    map_id;
        public string map_name;
        public string scene_name;
        public int    min_level;
        public int    max_level;
    }
}
```

### 1.5 — Thêm endpoint `GET /api/map/by-scene` vào MapController.cs

```csharp
// GameServerApi/Controllers/MapController.cs — thêm action sau:

// GET /api/map/by-scene?scene=GameScene
[HttpGet("by-scene")]
public async Task<IActionResult> GetMapByScene([FromQuery] string scene)
{
    var map = await _db.MapConfigs
        .FirstOrDefaultAsync(m => m.SceneName == scene);
    if (map == null) return NotFound($"Scene '{scene}' not found");
    return Ok(map);
}
```

---

## 2. Phân Vùng Zone — 1 Port + Room ID

> **Kiến trúc áp dụng:** 1 NGO server process + 1 port duy nhất (`:7777`).  
> Zone phân biệt bằng `room_id` logic — **không cần disconnect/reconnect**.  
> Xem phân tích đầy đủ tại [Mục 7](#7-kiến-trúc-zone-1-port--room-id).

### 2.1 — Mô hình Zone (1 port)

```
Map1.unity
├── Zone0   room_id="map1_zone0"  ┐
├── Zone1   room_id="map1_zone1"  ├─ tất cả dùng chung NGO server :7777
└── Zone2   room_id="map1_zone2"  ┘
```

Player bước qua **ZoneTrigger** → gửi `ServerRpc` với `room_id` mới → server teleport player + cập nhật phân nhóm.

### 2.2 — Bảng `map_zone_config` (schema mới)

```sql
CREATE TABLE IF NOT EXISTS map_zone_config (
    zone_id      INT PRIMARY KEY AUTO_INCREMENT,
    map_id       INT NOT NULL,
    zone_index   INT NOT NULL,          -- 0, 1, 2, ...
    zone_name    VARCHAR(50),
    room_id      VARCHAR(50) NOT NULL DEFAULT '',  -- "map1_zone0", "map1_zone1"...
    host_ip      VARCHAR(50) DEFAULT 'localhost',  -- IP chung của NGO server
    trigger_x_min FLOAT,                -- vùng trigger khi player đi vào
    trigger_x_max FLOAT,
    trigger_y_min FLOAT,
    trigger_y_max FLOAT,
    UNIQUE KEY uq_map_zone (map_id, zone_index)
);

-- Ví dụ Map1 có 3 zone
INSERT INTO map_zone_config (map_id, zone_index, zone_name, host_port,
                              trigger_x_min, trigger_x_max, trigger_y_min, trigger_y_max)
VALUES
  (1, 0, 'Khu A - Cổng Vào', 'map1_zone0',  -100, 20,  -50, 50),
  (1, 1, 'Khu B - Trung Tâm', 'map1_zone1',   20,  80,  -50, 50),
  (1, 2, 'Khu C - Sào Huyệt', 'map1_zone2',   80, 200,  -50, 50);
```

### 2.3 — ZoneTrigger.cs (Client Unity)

> File: `Client/Assets/Scripts/Map/ZoneTrigger.cs` — **đã implement**.

Kiến trúc 1 port: không disconnect/reconnect. Phiên bản rút gọn:

```csharp
// ZoneTrigger.cs — 1-port architecture
// Player bước qua trigger → fetch room_id → gửi ServerRpc → server teleport
[RequireComponent(typeof(BoxCollider2D))]
public class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private int    targetZoneIndex;
    [SerializeField] private int    mapId;
    [SerializeField] private float  spawnX, spawnY;
    [SerializeField] private string apiBase = "http://localhost:5000";

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent<NetworkObject>(out var net)) return;
        if (!net.IsOwner) return;
        StartCoroutine(FetchAndSwitchZone(other.gameObject));
    }

    private IEnumerator FetchAndSwitchZone(GameObject playerObj)
    {
        // Fetch room_id từ API
        using var req = UnityWebRequest.Get($"{apiBase}/api/map/zone?mapId={mapId}&zoneIndex={targetZoneIndex}");
        AuthHelper.AddAuthHeader(req);
        yield return req.SendWebRequest();

        var data = JsonUtility.FromJson<ZoneData>(req.downloadHandler.text);

        // Không cần reconnect — chỉ gửi ServerRpc
        if (playerObj.TryGetComponent<PlayerZoneHandler>(out var h))
            h.RequestZoneChangeServerRpc(new FixedString64Bytes(data.room_id), spawnX, spawnY);
    }
}
```

### 2.4 — Endpoint `GET /api/map/zone` (Backend — đã cập nhật)

```csharp
// MapController.cs — trả về room_id + host cố định
[HttpGet("zone")]
public async Task<IActionResult> GetZoneConfig([FromQuery] int mapId, [FromQuery] int zoneIndex)
{
    var zone = await _db.MapZoneConfigs
        .FirstOrDefaultAsync(z => z.MapId == mapId && z.ZoneIndex == zoneIndex && z.IsActive);
    if (zone == null) return NotFound();

    return Ok(new {
        zone_id   = zone.ZoneId,
        zone_name = zone.ZoneName,
        room_id   = zone.RoomId,       // "map1_zone0" — dùng để route trong server
        host_ip   = zone.HostIp,       // luôn cố định
        host_port = 7777               // 1 port duy nhất
    });
}
```

### 2.5 — Setup GameObject trong Unity (1-port)

```
[SERVER scene — DontDestroyOnLoad]
├── ZoneRoomManager (GameObject)          ← THÊM MỚI
│     ZoneRoomManager.cs

[Player Prefab]                           ← THÊM COMPONENT
└── PlayerZoneHandler.cs
      NetworkVariable<FixedString64Bytes> CurrentRoomId

[Map1.unity — Hierarchy]
├── ZoneManager
│   ├── ZoneTrigger_A_to_B      ← BoxCollider2D trigger tại x=20
│   │     ZoneTrigger.cs: targetZoneIndex=1, mapId=1, spawnX=22, spawnY=0
│   └── ZoneTrigger_B_to_C      ← BoxCollider2D trigger tại x=80
│         ZoneTrigger.cs: targetZoneIndex=2, mapId=1, spawnX=82, spawnY=0
```

---

## 3. Config Enemy (HP, EXP, Hệ, Rơi Item)

### 3.1 — Bảng `enemy` — Thêm/sửa quái


```sql
-- Xem danh sách enemy hiện có
SELECT enemy_id, name, level, base_hp, base_damage, exp_reward, element_type
FROM enemy ORDER BY level;

-- Thêm enemy mới (đầy đủ thuộc tính)
INSERT INTO enemy (
    name, level,
    base_hp, base_mp, base_damage, base_defense,
    move_speed, attack_speed,
    exp_reward, gold_reward, silver_reward,
    element_type,
    -- Kháng nguyên tố (0 = không kháng, 50 = giảm 50% sát thương từ hệ đó)
    khang_hoa, khang_thuy, khang_moc, khang_tho, khang_kim, khang_phong,
    -- Tăng dame vào hệ đối lập
    tang_dame_hoa, tang_dame_thuy, tang_dame_moc,
    tang_dame_tho,  tang_dame_kim,  tang_dame_phong,
    -- Đặc tính chiến đấu
    hp_regen_per_sec, evasion_rate, counter_rate,
    skills_json, phases_json
)
VALUES (
    'Hỏa Linh Nhỏ', 5,
    350, 100, 45, 20,
    2.5, 1.2,
    80, 2, 15,
    'Hoa',       -- hệ: Hoa | Thuy | Moc | Tho | Kim | Phong
    30, 0, 0, 0, 0, 10,   -- kháng Hỏa 30%, kháng Phong 10%
    0, 20, 10, 0, 0, 0,   -- +20% dmg vào Thủy, +10% vào Mộc
    1.5, 5.0, 0,           -- hồi 1.5 HP/s, 5% né tránh, 0% phản đòn
    '[]',         -- skills_json: array skill_id mà quái dùng
    'null'        -- phases_json: chỉ dùng cho boss
);
```

**Bảng tham chiếu hệ nguyên tố:**

| Hệ quái | Khắc chế | Bị khắc chế |
|---|---|---|
| Hỏa (Hoa) | Mộc | Thủy |
| Thủy (Thuy) | Hỏa | Mộc |
| Mộc (Moc) | Thổ | Hỏa |
| Thổ (Tho) | Thủy | Mộc |
| Kim (Kim) | Mộc | Hỏa |
| Phong (Phong) | Thổ | Kim |

### 3.2 — Bảng `enemy_spawns` — Vị trí spawn quái trên map

```sql
-- Xem spawn của map
SELECT * FROM enemy_spawns WHERE map_id = 1;

-- Thêm spawn point
INSERT INTO enemy_spawns (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
VALUES
  (1, 1, -5.5, 0.5, 3, 30),   -- map1: spawn 3 con Hỏa Linh, respawn 30s
  (1, 1,  3.0, 0.5, 2, 30),   -- thêm spawn point khác
  (1, 2, 10.0, 0.0, 1, 60);   -- 1 con boss nhỏ, respawn 60s
```

| Trường | Ý nghĩa |
|---|---|
| `enemy_type_id` | FK đến `enemy.enemy_id` |
| `spawn_x, spawn_y` | Vị trí spawn trong Unity scene |
| `max_spawn_count` | Số con tối đa ở điểm này cùng lúc |
| `respawn_time` | Giây chờ sau khi chết trước khi spawn lại |

### 3.3 — Bảng `map_enemy_drop` — Config rơi item

```sql
-- Cú pháp: enemy X ở map Y có Z% rơi item W
INSERT INTO map_enemy_drop (map_id, enemy_id, item_id, drop_chance, min_qty, max_qty)
VALUES
  (1, 1, 101, 50.0, 1, 3),  -- map1, quái id=1: 50% rơi item id=101, số lượng 1-3
  (1, 1, 202, 10.0, 1, 1),  -- 10% rơi item id=202
  (1, 2, 305, 5.0,  1, 1),  -- 5% rơi item hiếm
  (0, 0, 101, 80.0, 1, 5);  -- map=0, enemy=0 → áp dụng cho TẤT CẢ (global drop)
```

> **Lưu ý:** Khi `map_id=0` và `enemy_id=0` → server xử lý đó là drop global cho mọi quái.

### 3.4 — Config boss riêng biệt

```sql
INSERT INTO boss_config (enemy_id, map_id, spawn_x, spawn_y,
                          min_spawn_hour, max_spawn_hour, respawn_minutes)
VALUES
  (10, 1, 15.0, 0.0,  20, 22, 120);  -- boss id=10 ở map1, spawn 20h-22h, respawn 2 tiếng
```

### 3.5 — Wiring HP từ DB vào NetworkEnemyHealth.cs

Hiện tại `EnemyHealth.cs` dùng `maxHealth = 10` cứng. Sửa để dùng giá trị từ DB:

```csharp
// Client/Assets/Scripts/Network/Enemy/NetworkEnemyHealth.cs
// (hoặc EnemyHealth.cs nếu dùng non-networked)

public class NetworkEnemyHealth : NetworkBehaviour
{
    public NetworkVariable<int> CurrentHealth = new NetworkVariable<int>(10,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> MaxHealth = new NetworkVariable<int>(10,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Gọi từ NetworkEnemySpawner sau khi spawn, truyền base_hp từ API
    public void InitHealth(int maxHp)
    {
        if (!IsServer) return;
        MaxHealth.Value    = maxHp;
        CurrentHealth.Value = maxHp;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage)
    {
        int actual = Mathf.Max(0, damage);
        CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - actual);
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

Trong `NetworkEnemySpawner.cs`, sau khi spawn thêm:

```csharp
// Sau dòng networkObj.Spawn()
var health = enemyObj.GetComponent<NetworkEnemyHealth>();
if (health != null)
    health.InitHealth(spawnData.enemy.base_hp);  // base_hp đến từ API
```

---

## 4. Nút Chuyển Map Trái / Phải

### 4.1 — Kiến trúc

Mỗi map có **2 portal** ở cuối map: một bên trái (về map trước) và một bên phải (về map sau).

```
Map1.unity
├── Portal_Left   ← chuyển về map_id = 0 (Làng Khởi Đầu)
└── Portal_Right  ← chuyển sang map_id = 2 (Rừng Băng)
```

### 4.2 — Config portal trong DB

```sql
-- Portal trái (right edge map0 → left edge map1 và ngược lại)
INSERT INTO map_portal (
    source_map_id, dest_map_id,
    src_x, src_y, src_radius,
    dest_x, dest_y, dest_scene_name,
    portal_type, required_item_id, dungeon_id
) VALUES
-- Map0 → Map1 (bên phải map0)
(0, 1,  25.0, 0.0, 3.0,  -24.0, 0.0,  'Map1',  'world_travel', NULL, NULL),
-- Map1 → Map0 (bên trái map1) — quay lại
(1, 0, -25.0, 0.0, 3.0,   24.0, 0.0,  'GameScene', 'world_travel', NULL, NULL),
-- Map1 → Map2 (bên phải map1)
(1, 2,  25.0, 0.0, 3.0,  -24.0, 0.0,  'Map2',  'world_travel', NULL, NULL),
-- Map2 → Map1 (bên trái map2) — quay lại
(2, 1, -25.0, 0.0, 3.0,   24.0, 0.0,  'Map1',  'world_travel', NULL, NULL);
```

> **Quy ước:** Portal bên **phải** (`src_x = +25`) dẫn đến map tiếp theo. Portal bên **trái** (`src_x = -25`) quay về map trước.

### 4.3 — MapTransitionButton.cs — Nút bấm UI

Thêm nút buttonLeft / buttonRight trên Canvas thay vì (hoặc cộng thêm) portal vật lý:

```csharp
// Client/Assets/Scripts/Map/MapTransitionButton.cs
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class MapTransitionButton : MonoBehaviour
{
    [Header("Loại nút")]
    [SerializeField] private bool isRightButton = true;   // true = phải, false = trái

    [SerializeField] private int    currentMapId;
    [SerializeField] private string apiBase = "http://localhost:5000";

    [Header("UI")]
    [SerializeField] private Button     button;
    [SerializeField] private GameObject loadingPanel;

    void Start()
    {
        button.onClick.AddListener(OnClick);
        // Nút chuyển map chỉ khả dụng khi player ở edge của map
        // (tuỳ chọn: ẩn/hiện theo vị trí player)
    }

    private void OnClick()
    {
        StartCoroutine(DoTravel());
    }

    private IEnumerator DoTravel()
    {
        button.interactable = false;
        if (loadingPanel) loadingPanel.SetActive(true);

        // Lấy portal phù hợp: trái hoặc phải của map hiện tại
        var direction = isRightButton ? "right" : "left";
        var url = $"{apiBase}/api/map/portal/direction?mapId={currentMapId}&direction={direction}";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[MapTransitionButton] " + req.error);
            button.interactable = true;
            if (loadingPanel) loadingPanel.SetActive(false);
            yield break;
        }

        var portal = JsonUtility.FromJson<PortalData>(req.downloadHandler.text);

        // Xác nhận với server qua endpoint travel
        var travelJson = JsonUtility.ToJson(new TravelPayload
        {
            portal_id      = portal.portal_id,
            player_id      = PlayerPrefs.GetInt("USER_ID"),
            current_map_id = currentMapId,
            player_x       = 0f,  // vị trí thực tế hoặc lấy từ player transform
            player_y       = 0f
        });

        using var travelReq = new UnityWebRequest($"{apiBase}/api/map/travel", "POST");
        travelReq.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(travelJson));
        travelReq.downloadHandler = new DownloadHandlerBuffer();
        travelReq.SetRequestHeader("Content-Type", "application/json");
        yield return travelReq.SendWebRequest();

        if (travelReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[MapTransitionButton] Travel failed: " + travelReq.downloadHandler.text);
            button.interactable = true;
            if (loadingPanel) loadingPanel.SetActive(false);
            yield break;
        }

        var travelResp = JsonUtility.FromJson<TravelResponse>(travelReq.downloadHandler.text);
        PortalArrivalHandler.PendingDestX  = travelResp.dest_x;
        PortalArrivalHandler.PendingDestY  = travelResp.dest_y;
        PortalArrivalHandler.PendingMapId  = travelResp.dest_map_id;

        // Shutdown NGO trước khi chuyển scene
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene(travelResp.dest_scene_name);
    }

    [System.Serializable] private class PortalData  { public int portal_id; }
    [System.Serializable] private class TravelPayload
    {
        public int   portal_id;
        public int   player_id;
        public int   current_map_id;
        public float player_x;
        public float player_y;
    }
    [System.Serializable] private class TravelResponse
    {
        public int    dest_map_id;
        public string dest_scene_name;
        public float  dest_x;
        public float  dest_y;
    }
}
```

### 4.4 — Thêm endpoint `GET /api/map/portal/direction` vào Backend

```csharp
// MapController.cs — thêm action:

// GET /api/map/portal/direction?mapId=1&direction=right
[HttpGet("portal/direction")]
public async Task<IActionResult> GetPortalByDirection(
    [FromQuery] int mapId,
    [FromQuery] string direction)
{
    // Quy ước: right = portal có src_x > 0, left = portal có src_x < 0
    var portals = await _db.MapPortals
        .Where(p => p.SourceMapId == mapId && p.PortalType == "world_travel")
        .ToListAsync();

    MapPortal? portal = direction == "right"
        ? portals.OrderByDescending(p => p.SrcX).FirstOrDefault()
        : portals.OrderBy(p => p.SrcX).FirstOrDefault();

    if (portal == null) return NotFound($"No {direction} portal for map {mapId}");
    return Ok(portal);
}
```

### 4.5 — Setup GameObject trong Unity

```
Hierarchy (Map1.unity)
├── Canvas
│   ├── Btn_MapLeft
│   │     MapTransitionButton.cs:
│   │       isRightButton = false
│   │       currentMapId  = 1
│   │       button (drag Button component)
│   └── Btn_MapRight
│         MapTransitionButton.cs:
│           isRightButton = true
│           currentMapId  = 1
│           button (drag Button component)
```

---

## 5. Config NPC — Menu & Mua/Bán Item

### 5.1 — Bảng `npc_config` — Thêm NPC lên map

```sql
-- Xem NPC hiện có
SELECT npc_id, npc_name, npc_type, map_id, pos_x, pos_y FROM npc_config;

-- Thêm NPC mua/bán vào Map1
INSERT INTO npc_config (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key)
VALUES
  ('Lão Thương Nhân', 'shop',    1,  5.0, 0.5, 'npc_shop_001'),
  ('Thầy Rèn Kỳ Lão', 'blacksmith', 1, -3.0, 0.5, 'npc_blacksmith_001'),
  ('Sứ Giả Nhiệm Vụ', 'quest',   1,  0.0, 0.5, 'npc_quest_001');
```

**Các loại NPC (`npc_type`):**

| npc_type | Chức năng |
|---|---|
| `shop` | Mua/bán item thường |
| `blacksmith` | Nâng cấp trang bị |
| `quest` | Phát/nhận nhiệm vụ |
| `exchange` | Trao đổi item đặc biệt |
| `event` | NPC sự kiện theo mùa |

### 5.2 — Bảng `npc_dialogue` — Tree hội thoại

```sql
-- NPC shop: cây dialogue
INSERT INTO npc_dialogue (dialogue_key, text_vi, next_key, action_type)
VALUES
  ('npc_shop_001',      'Xin chào, ta có nhiều hàng tốt. Ngươi muốn gì?',
                          NULL, 'show_menu'),

  ('npc_shop_001_buy',  'Hãy chọn vật phẩm ta đây ngươi.',
                          NULL, 'open_shop'),

  ('npc_shop_001_bye',  'Hẹn gặp lại, hảo hán!',
                          NULL, 'close'),

  -- Quest NPC
  ('npc_quest_001',     'Ngươi có muốn nhận nhiệm vụ không?',
                          NULL, 'show_menu'),
  ('npc_quest_001_accept', 'Tốt! Hãy tiêu diệt 10 Hỏa Linh.',
                          NULL, 'give_quest');
```

### 5.3 — Bảng `npc_shop_item` — Hàng bán

```sql
-- NPC id = 1 (Lão Thương Nhân) bán các item sau
INSERT INTO npc_shop_item (npc_id, item_template_id, price_silver, price_gold,
                            stock, required_level)
VALUES
  (1, 101, 500,   0, -1, 1),   -- Thuốc Hồi HP nhỏ (500 bạc, không giới hạn stock)
  (1, 102, 1500,  0, -1, 5),   -- Thuốc Hồi HP lớn
  (1, 103, 0,     5, -1, 10),  -- Trang bị level 10 (5 vàng)
  (1, 201, 3000,  0, 10, 15);  -- Item hiếm, chỉ 10 cái, cần level 15
```

> `stock = -1` = không giới hạn số lượng.

### 5.4 — NpcSpawner.cs — Tự spawn NPC trong scene từ DB

Tạo file mới: `Client/Assets/Scripts/NPC/NpcSpawner.cs`

```csharp
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// Khi scene load xong, tự động gọi API lấy danh sách NPC của map
/// và instantiate prefab NPC tại vị trí tương ứng.
/// </summary>
public class NpcSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] npcPrefabs;  // index tương ứng npc_type
    // 0=shop, 1=blacksmith, 2=quest, 3=exchange, 4=event
    [SerializeField] private string apiBase = "http://localhost:5000";

    private void Start()
    {
        StartCoroutine(SpawnNpcs());
    }

    private IEnumerator SpawnNpcs()
    {
        int mapId = MapManager.Instance?.GetMapId() ?? 0;
        var url = $"{apiBase}/api/npc/list?mapId={mapId}";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[NpcSpawner] " + req.error);
            yield break;
        }

        var resp = JsonUtility.FromJson<NpcListResponse>("{\"npcs\":" + req.downloadHandler.text + "}");
        foreach (var npc in resp.npcs)
        {
            var prefab = GetPrefabForType(npc.npc_type);
            if (prefab == null) continue;

            var go = Instantiate(prefab, new Vector3(npc.pos_x, npc.pos_y, 0), Quaternion.identity);
            go.name = npc.npc_name;

            // Truyền data để NPC biết mình là ai
            if (go.TryGetComponent<NpcInteraction>(out var inter))
                inter.Init(npc);
        }
    }

    private GameObject GetPrefabForType(string type) => type switch
    {
        "shop"        => npcPrefabs.Length > 0 ? npcPrefabs[0] : null,
        "blacksmith"  => npcPrefabs.Length > 1 ? npcPrefabs[1] : null,
        "quest"       => npcPrefabs.Length > 2 ? npcPrefabs[2] : null,
        "exchange"    => npcPrefabs.Length > 3 ? npcPrefabs[3] : null,
        "event"       => npcPrefabs.Length > 4 ? npcPrefabs[4] : null,
        _             => npcPrefabs.Length > 0 ? npcPrefabs[0] : null
    };

    [System.Serializable] private class NpcListResponse  { public NpcData[] npcs; }
    [System.Serializable] public  class NpcData
    {
        public int    npc_id;
        public string npc_name;
        public string npc_type;
        public float  pos_x;
        public float  pos_y;
        public string dialogue_key;
    }
}
```

### 5.5 — NpcInteraction.cs — Click vào NPC → Mở menu

Tạo file mới: `Client/Assets/Scripts/NPC/NpcInteraction.cs`

```csharp
using UnityEngine;

/// <summary>
/// Gắn vào prefab NPC. Khi player click/chạm vào → mở NpcMenuUI.
/// </summary>
public class NpcInteraction : MonoBehaviour
{
    private NpcSpawner.NpcData data;

    public void Init(NpcSpawner.NpcData npcData)
    {
        data = npcData;
    }

    // Click chuột (PC) hoặc tap (mobile)
    private void OnMouseDown()
    {
        if (data == null) return;

        // Kiểm tra khoảng cách player - NPC
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.transform.position);
            if (dist > 3f)
            {
                Debug.Log("[NpcInteraction] Quá xa, hãy lại gần NPC!");
                return;
            }
        }

        NpcMenuUI.Instance?.Open(data);
    }
}
```

### 5.6 — NpcMenuUI.cs — UI Menu NPC (mua/bán)

Tạo file mới: `Client/Assets/Scripts/NPC/NpcMenuUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

/// <summary>
/// Panel UI xuất hiện khi tương tác với NPC.
/// Hiển thị menu tuỳ theo npc_type.
/// </summary>
public class NpcMenuUI : MonoBehaviour
{
    public static NpcMenuUI Instance { get; private set; }

    [Header("Panel chính")]
    [SerializeField] private GameObject   mainPanel;
    [SerializeField] private TMP_Text     npcNameText;
    [SerializeField] private TMP_Text     dialogueText;

    [Header("Các nút menu")]
    [SerializeField] private Button       btnBuy;      // mua hàng
    [SerializeField] private Button       btnSell;     // bán đồ
    [SerializeField] private Button       btnClose;

    [Header("Shop Panel")]
    [SerializeField] private GameObject   shopPanel;
    [SerializeField] private Transform    shopItemContainer;
    [SerializeField] private GameObject   shopItemRowPrefab;  // prefab 1 dòng item

    [SerializeField] private string apiBase = "http://localhost:5000";

    private NpcSpawner.NpcData currentNpc;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        btnClose.onClick.AddListener(Close);
        btnBuy.onClick.AddListener(OpenShop);
        btnSell.onClick.AddListener(() => Debug.Log("TODO: Sell panel"));
        mainPanel.SetActive(false);
        shopPanel.SetActive(false);
    }

    public void Open(NpcSpawner.NpcData npc)
    {
        currentNpc = npc;
        npcNameText.text  = npc.npc_name;
        dialogueText.text = "Đang tải...";
        mainPanel.SetActive(true);
        shopPanel.SetActive(false);

        // Hiện/ẩn nút theo loại NPC
        bool isShop = npc.npc_type is "shop" or "exchange";
        btnBuy.gameObject.SetActive(isShop);
        btnSell.gameObject.SetActive(isShop);

        // Gọi API lấy dialogue đầu tiên
        StartCoroutine(FetchDialogue(npc.npc_id));
    }

    public void Close()
    {
        mainPanel.SetActive(false);
        shopPanel.SetActive(false);
        currentNpc = null;
    }

    // ── Dialogue ─────────────────────────────────────────────
    private IEnumerator FetchDialogue(int npcId)
    {
        int playerId = PlayerPrefs.GetInt("USER_ID");
        var body     = JsonUtility.ToJson(new InteractPayload { npc_id = npcId, player_id = playerId });

        using var req = new UnityWebRequest($"{apiBase}/api/npc/interact", "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<InteractResponse>(req.downloadHandler.text);
            dialogueText.text = resp.dialogue_text;
        }
        else
        {
            dialogueText.text = "Xin chào, ta có thể giúp gì cho ngươi?";
        }
    }

    // ── Shop ─────────────────────────────────────────────────
    private void OpenShop()
    {
        shopPanel.SetActive(true);
        StartCoroutine(FetchShopItems());
    }

    private IEnumerator FetchShopItems()
    {
        // Xóa danh sách cũ
        foreach (Transform child in shopItemContainer)
            Destroy(child.gameObject);

        int playerId = PlayerPrefs.GetInt("USER_ID");
        var url = $"{apiBase}/api/npc/shop?npcId={currentNpc.npc_id}&playerId={playerId}";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[NpcMenuUI] Shop load failed: " + req.error);
            yield break;
        }

        var shopResp = JsonUtility.FromJson<ShopResponse>("{\"items\":" + req.downloadHandler.text + "}");
        foreach (var item in shopResp.items)
        {
            var row = Instantiate(shopItemRowPrefab, shopItemContainer);

            // Set tên item
            row.transform.Find("ItemName").GetComponent<TMP_Text>().text = item.item_name;

            // Set giá
            string priceStr = item.price_gold > 0
                ? $"{item.price_gold} vàng"
                : $"{item.price_silver} bạc";
            row.transform.Find("Price").GetComponent<TMP_Text>().text = priceStr;

            // Nút mua
            var btnBuyItem = row.transform.Find("BtnBuy").GetComponent<Button>();
            var capturedItem = item;
            btnBuyItem.onClick.AddListener(() => StartCoroutine(BuyItem(capturedItem)));

            // Xám hoá nếu cần level cao hơn
            bool canBuy = item.can_afford && item.meets_level;
            btnBuyItem.interactable = canBuy;
        }
    }

    private IEnumerator BuyItem(ShopItem item)
    {
        int playerId = PlayerPrefs.GetInt("USER_ID");
        var body = JsonUtility.ToJson(new BuyPayload
        {
            player_id = playerId,
            npc_id    = currentNpc.npc_id,
            item_id   = item.item_template_id,
            quantity  = 1
        });

        using var req = new UnityWebRequest($"{apiBase}/api/npc/shop/buy", "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"[NpcMenuUI] Mua thành công: {item.item_name}");
            // Refresh shop + inventory UI
            StartCoroutine(FetchShopItems());
        }
        else
        {
            var errMsg = req.downloadHandler.text;
            Debug.LogError($"[NpcMenuUI] Mua thất bại: {errMsg}");
            // TODO: hiện thông báo lỗi cho player
        }
    }

    // ── Serializable types ────────────────────────────────────
    [System.Serializable] private class InteractPayload  { public int npc_id; public int player_id; }
    [System.Serializable] private class InteractResponse { public string dialogue_text; }
    [System.Serializable] private class ShopResponse     { public ShopItem[] items; }
    [System.Serializable] private class BuyPayload
    {
        public int player_id;
        public int npc_id;
        public int item_id;
        public int quantity;
    }

    [System.Serializable]
    public class ShopItem
    {
        public int    item_template_id;
        public string item_name;
        public int    price_silver;
        public int    price_gold;
        public int    stock;
        public int    required_level;
        public bool   can_afford;
        public bool   meets_level;
    }
}
```

### 5.7 — Setup prefab & Canvas trong Unity

```
Hierarchy (Map1.unity — hoặc persistent Canvas)
├── NpcSpawner (GameObject)
│     NpcSpawner.cs:
│       npcPrefabs[0] = NPC_Shop_Prefab
│       npcPrefabs[1] = NPC_Blacksmith_Prefab
│       npcPrefabs[2] = NPC_Quest_Prefab
│
└── Canvas (Screen Space Overlay)
      └── NpcMenuUI (GameObject — DontDestroyOnLoad nếu cần)
            NpcMenuUI.cs: (assign tất cả fields trên Inspector)
            ├── mainPanel
            │   ├── NpcNameText  (TMP_Text)
            │   ├── DialogueText (TMP_Text)
            │   ├── BtnBuy       (Button)
            │   ├── BtnSell      (Button)
            │   └── BtnClose     (Button)
            └── shopPanel
                ├── ShopItemContainer (ScrollRect/Content)
                └── [ShopItemRow Prefab]:
                      ├── ItemName  (TMP_Text)
                      ├── Price     (TMP_Text)
                      └── BtnBuy    (Button)
```

---

## 6. Config Hình Ảnh Cho Mỗi Hệ Nguyên Tố

### 6.1 — Mục đích

Bảng `element_type_config` cho phép thay đổi **icon, màu sắc** hiển thị của mỗi hệ  
(Hỏa, Thủy, Mộc, Thổ, Kim, Phong) mà **không cần sửa code** — chỉ cần cập nhật DB  
và đặt file ảnh đúng đường dẫn Resources trong Unity.

### 6.2 — Chạy migration

```sql
-- Chạy file: migration_element_type_config.sql
-- Bảng sẽ được tạo kèm 6 bản ghi mặc định cho 6 hệ
```

Kết quả mặc định sau migration:

| element_key | display_name | icon_path | color_hex |
|---|---|---|---|
| Hoa | Hỏa | `Elements/icon_hoa` | `#FF4500` |
| Thuy | Thủy | `Elements/icon_thuy` | `#00BFFF` |
| Moc | Mộc | `Elements/icon_moc` | `#228B22` |
| Tho | Thổ | `Elements/icon_tho` | `#A0522D` |
| Kim | Kim | `Elements/icon_kim` | `#C0C0C0` |
| Phong | Phong | `Elements/icon_phong` | `#9370DB` |

### 6.3 — Đặt file ảnh trong Unity

```
Assets/
└── Resources/
    └── Elements/
        ├── icon_hoa.png      ← Sprite hệ Hỏa
        ├── icon_thuy.png     ← Sprite hệ Thủy
        ├── icon_moc.png      ← Sprite hệ Mộc
        ├── icon_tho.png      ← Sprite hệ Thổ
        ├── icon_kim.png      ← Sprite hệ Kim
        └── icon_phong.png    ← Sprite hệ Phong
```

> Mỗi ảnh phải ở trong **Assets/Resources/** (hoặc thư mục con).  
> `icon_path` trong DB là đường dẫn **tương đối từ Resources/** và **không có đuôi `.png`**.

### 6.4 — API Endpoints

| Method | Endpoint | Mục đích |
|---|---|---|
| `GET` | `/api/element-type` | Lấy toàn bộ danh sách hệ + icon |
| `GET` | `/api/element-type/{elementKey}` | Lấy config của một hệ (vd: `Hoa`) |
| `PUT` | `/api/element-type/{elementKey}/icon` | **Thay đổi icon** (và tuỳ chọn màu) |
| `PUT` | `/api/element-type/{elementKey}` | Cập nhật toàn bộ config |

**Ví dụ đổi icon hệ Hỏa:**
```http
PUT /api/element-type/Hoa/icon
Content-Type: application/json

{
  "icon_path": "Elements/icon_hoa_v2",
  "color_hex": "#FF6A00"
}
```

### 6.5 — Load icon trong Unity (ElementIconLoader.cs)

Tạo file: `Client/Assets/Scripts/UI/ElementIconLoader.cs`

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Tải icon + màu cho tất cả hệ nguyên tố từ API,
/// sau đó cache lại để các UI khác dùng.
/// </summary>
public class ElementIconLoader : MonoBehaviour
{
    public static ElementIconLoader Instance { get; private set; }

    // Gán trong Inspector — base URL API
    [SerializeField] private string apiBaseUrl = "http://localhost:5000";

    // Cache: element_key -> Sprite
    public Dictionary<string, Sprite>      Icons  { get; private set; } = new();
    // Cache: element_key -> Color
    public Dictionary<string, Color>       Colors { get; private set; } = new();

    private bool _loaded = false;
    public bool IsLoaded => _loaded;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Start() => StartCoroutine(LoadAllElements());

    private IEnumerator LoadAllElements()
    {
        string url = $"{apiBaseUrl}/api/element-type";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ElementIconLoader] Lỗi tải hệ: {req.error}");
            yield break;
        }

        var response = JsonConvert.DeserializeObject<ElementListResponse>(req.downloadHandler.text);
        if (response?.elements == null) yield break;

        foreach (var elem in response.elements)
        {
            // Load sprite từ Resources
            var sprite = Resources.Load<Sprite>(elem.icon_path);
            if (sprite != null)
                Icons[elem.element_key] = sprite;
            else
                Debug.LogWarning($"[ElementIconLoader] Không tìm thấy sprite: Resources/{elem.icon_path}");

            // Parse màu hex
            if (!string.IsNullOrEmpty(elem.color_hex) &&
                ColorUtility.TryParseHtmlString(elem.color_hex, out Color c))
                Colors[elem.element_key] = c;
        }

        _loaded = true;
        Debug.Log($"[ElementIconLoader] Đã tải {Icons.Count} icon hệ nguyên tố.");
    }

    /// <summary>Lấy Sprite của hệ, trả null nếu chưa load hoặc không có.</summary>
    public Sprite GetIcon(string elementKey)
        => Icons.TryGetValue(elementKey, out var s) ? s : null;

    /// <summary>Lấy Color của hệ, trả về màu trắng nếu không tìm thấy.</summary>
    public Color GetColor(string elementKey)
        => Colors.TryGetValue(elementKey, out var c) ? c : Color.white;

    // --- DTO nội bộ ---
    [System.Serializable]
    private class ElementListResponse
    {
        public List<ElementDto> elements;
    }
    [System.Serializable]
    private class ElementDto
    {
        public string element_key;
        public string display_name;
        public string icon_path;
        public string color_hex;
    }
}
```

### 6.6 — Dùng icon trong UI (Enemy HUD, tên hệ, v.v.)

```csharp
// Ví dụ: gắn icon hệ lên EnemyHealthBar khi spawn
void RefreshElementIcon(string elementType)
{
    if (ElementIconLoader.Instance == null || !ElementIconLoader.Instance.IsLoaded)
        return;

    var sprite = ElementIconLoader.Instance.GetIcon(elementType);
    if (sprite != null)
        elementIconImage.sprite = sprite;  // Image component trên HUD

    var color = ElementIconLoader.Instance.GetColor(elementType);
    healthBarFill.color = color;           // Đổi màu HP bar theo hệ
}
```

Gắn `ElementIconLoader.cs` lên **một GameObject persistent** (ví dụ: GameManager) để nó load  
một lần và cache cho toàn bộ session.

---

## Tóm Tắt Quy Trình Cài Đặt

### Bước 1 — Database

```sql
-- 1. Thêm map mới
INSERT INTO map_config ...

-- 2. Thêm zone cho map (nếu muốn multi-zone)
INSERT INTO map_zone_config ...

-- 3. Thêm portal trái/phải
INSERT INTO map_portal ...

-- 4. Thêm enemy + spawn points
INSERT INTO enemy ...
INSERT INTO enemy_spawns ...

-- 5. Config drop item
INSERT INTO map_enemy_drop ...

-- 6. Thêm NPC
INSERT INTO npc_config ...
INSERT INTO npc_dialogue ...
INSERT INTO npc_shop_item ...

-- 7. Config hình ảnh hệ nguyên tố (chạy migration_element_type_config.sql)
-- Sau đó UPDATE icon_path nếu muốn đổi ảnh:
UPDATE element_type_config SET icon_path = 'Elements/icon_hoa_v2' WHERE element_key = 'Hoa';
```

### Bước 2 — Unity Scene

| Việc | File cần tạo |
|---|---|
| Tạo scene mới `Map1.unity` | File → New Scene |
| Thêm `MapManager.cs` (singleton) | Gắn vào GameObject persistent |
| Thêm `NpcSpawner.cs` | Gắn vào GameObject trong scene |
| Thêm `MapTransitionButton.cs` | Gắn vào Button UI trái/phải |
| Thêm `ZoneTrigger.cs` | Gắn vào object có BoxCollider2D |
| Thêm `NpcMenuUI.cs` + Canvas | UI Panel với ScrollView shop |
| Thêm `ElementIconLoader.cs` | Gắn vào GameObject persistent (GameManager) |
| Đặt file ảnh hệ vào `Assets/Resources/Elements/` | 6 file PNG (icon_hoa, icon_thuy, ...) |
| Đăng ký scene trong Build Settings | File → Build Settings |

### Bước 3 — Backend API (C#)

Thêm vào `MapController.cs`:
- `GET /api/map/by-scene?scene=`
- `GET /api/map/zone?mapId=&zoneIndex=`
- `GET /api/map/portal/direction?mapId=&direction=`

Thêm Entity + DbSet:
- `MapZoneConfig.cs`
- `GameDbContext.MapZoneConfigs`

---

## Checklist Nhanh

- [ ] Scene mới tạo xong và đăng ký Build Settings
- [ ] `map_config` có bản ghi cho scene mới (`scene_name` khớp tên scene Unity)
- [ ] `MapManager.cs` tự fetch mapId khi scene load
- [ ] `enemy` + `enemy_spawns` có data cho map mới
- [ ] `NetworkEnemySpawner` đọc `base_hp` từ API và gọi `InitHealth()`
- [ ] `map_enemy_drop` config drop item cho quái
- [ ] `map_portal` có 2 bản ghi trái/phải cho mỗi map (world_travel)
- [ ] `MapTransitionButton.cs` gắn vào Button, `currentMapId` set đúng
- [ ] `npc_config` + `npc_shop_item` + `npc_dialogue` thêm xong
- [ ] `NpcSpawner.cs` gắn vào scene, npcPrefabs assign đủ
- [ ] `NpcMenuUI.cs` gắn vào Canvas, tất cả fields assign trong Inspector
- [ ] **[1-port]** Chạy SQL migration đổi `map_zone_config` từ `host_port` sang `room_id`
- [ ] **[1-port]** `map_zone_config` có `room_id` cho mỗi zone (vd: "map1_zone0")
- [ ] **[1-port]** `ZoneRoomManager.cs` gắn vào GameObject persistent trên server scene
- [ ] **[1-port]** `PlayerZoneHandler.cs` gắn vào Player Prefab
- [ ] `ZoneTrigger.cs` gắn vào BoxCollider2D tại ranh giới zone
- [ ] Chạy `migration_element_type_config.sql` → bảng `element_type_config` có 6 hệ
- [ ] Đặt 6 file PNG vào `Assets/Resources/Elements/` (đặt tên khớp `icon_path` trong DB)
- [ ] `ElementIconLoader.cs` gắn vào GameManager, `apiBaseUrl` trỏ đúng server
- [ ] Dùng `ElementIconLoader.Instance.GetIcon(elementType)` để gán sprite trong HUD enemy

---

## 7. Kiến Trúc Zone: 1 Port + Room ID

### 7.1 — Vấn đề với kiến trúc nhiều port

Kiến trúc cũ: **mỗi zone = 1 port NGO riêng** → nhiều vấn đề khi deploy thật:

| Vấn đề | Chi tiết |
|---|---|
| Mở nhiều port | 10 zone = 10 port mở trên firewall |
| Khó quản lý | Mỗi zone là 1 process riêng, crash riêng |
| Client reconnect | Phải shutdown NGO + reconnect mỗi khi đổi zone |
| Timing nguy hiểm | NGO shutdown giữa chừng có thể kill coroutine của player |

### 7.2 — Kiến trúc mới: 1 NGO Server + Room ID Logic

```
[Client] ──── connect ────► [NGO Server :7777] ◄──── [ASP.NET API :5000]
                                   │
                          ┌────────┴────────┐
                          │  ZoneRoomManager │
                          │  room_id logic   │
                          ├─────────────────┤
                          │ "map1_zone0" → {clientA, clientB}
                          │ "map1_zone1" → {clientC}
                          │ "map1_zone2" → {clientD, clientE}
                          └─────────────────┘
```

**Nguyên tắc:**
- **1 port duy nhất** (`7777`) cho toàn bộ NGO server
- Zone phân biệt bằng `room_id` (chuỗi logic trong DB, vd: `"map1_zone0"`)
- Client đổi zone → gửi `ServerRpc` → server cập nhật room + teleport → không cần reconnect
- Broadcast damage/spawn → lọc theo `room_id` bằng `RoomBroadcast.ToRoom()`
- **2 port tổng cộng:** `7777` (NGO) + `5000` (API)

### 7.3 — SQL Migration

Chạy migration để đổi schema `map_zone_config`:

```sql
-- Bước 1: Thêm cột room_id
ALTER TABLE map_zone_config
  ADD COLUMN room_id VARCHAR(50) NOT NULL DEFAULT '';

-- Bước 2: Auto-fill room_id theo quy ước "map{mapId}_zone{zoneIndex}"
UPDATE map_zone_config
SET room_id = CONCAT('map', map_id, '_zone', zone_index);

-- Bước 3: Xóa cột host_port (không còn dùng)
ALTER TABLE map_zone_config DROP COLUMN host_port;

-- Kiểm tra kết quả
SELECT map_id, zone_index, room_id, host_ip FROM map_zone_config;
```

Kết quả mẫu:

| map_id | zone_index | room_id | host_ip |
|---|---|---|---|
| 1 | 0 | `map1_zone0` | localhost |
| 1 | 1 | `map1_zone1` | localhost |
| 1 | 2 | `map1_zone2` | localhost |

### 7.4 — Luồng hoạt động khi player đổi zone

```
1. Player bước vào BoxCollider2D của ZoneTrigger
2. ZoneTrigger → GET /api/map/zone?mapId=1&zoneIndex=1  → nhận {"room_id":"map1_zone1"}
3. ZoneTrigger → PlayerZoneHandler.RequestZoneChangeServerRpc("map1_zone1", spawnX, spawnY)
4. [SERVER] ZoneRoomManager.AssignClientToRoom(clientId, "map1_zone1")
5. [SERVER] CurrentRoomId.Value = "map1_zone1"  (sync xuống tất cả client)
6. [SERVER] transform.position = (spawnX, spawnY)  (teleport server-authoritative)
7. [CLIENT] OnZoneChangedClientRpc callback → hiện thông báo / ẩn loading
```

### 7.5 — File .cs mới trong kiến trúc 1 port

| File | Đặt ở | Mục đích |
|---|---|---|
| `ZoneRoomManager.cs` | `Scripts/Map/` | Server-side: theo dõi client trong từng zone |
| `PlayerZoneHandler.cs` | `Scripts/Player/` | Gắn vào Player Prefab: xử lý ServerRpc đổi zone |
| `RoomBroadcast.cs` | `Scripts/Map/` | Utility: tạo `ClientRpcParams` lọc theo zone |

### 7.6 — Setup GameObject trong Unity

```
[SERVER scene / persistent]
├── ZoneRoomManager (GameObject)
│     ZoneRoomManager.cs  ← quản lý room assignment
│
[Player Prefab]  ← thêm component này
└── PlayerZoneHandler.cs  ← xử lý RequestZoneChangeServerRpc
    NetworkVariable<FixedString64Bytes> CurrentRoomId
```

### 7.7 — Dùng RoomBroadcast trong Enemy/Game systems

Khi broadcast damage, spawn effect... chỉ gửi đến client trong cùng zone:

```csharp
// Trong NetworkEnemyHealth.cs — chỉ gửi damage update cho zone có enemy
[ServerRpc(RequireOwnership = false)]
public void TakeDamageServerRpc(int damage)
{
    networkCurrentHealth.Value = Mathf.Max(0, networkCurrentHealth.Value - damage);

    // Lọc theo room — chỉ client trong cùng zone nhận ClientRpc
    string enemyRoom = "map1_zone0";  // lấy từ EnemyZoneTag component
    var target = RoomBroadcast.ToRoom(enemyRoom, ZoneRoomManager.Instance);
    ShowDamageClientRpc(damage, target);

    if (networkCurrentHealth.Value <= 0) Die();
}

[ClientRpc]
private void ShowDamageClientRpc(int damage, ClientRpcParams rpcParams = default)
{
    // Chỉ client trong zone nhận được — hiện damage popup
    Debug.Log($"Damage: {damage}");
}
```

### 7.8 — So sánh 2 kiến trúc

| Tiêu chí | Nhiều port (cũ) | 1 port + room_id (mới) |
|---|---|---|
| Port mở ra ngoài | N port (3100, 3101...) | **1 port (7777)** |
| Firewall config | Phức tạp | **Đơn giản** |
| Client đổi zone | Phải disconnect/reconnect | **Gửi 1 ServerRpc** |
| Isolation zone | Tự nhiên (process riêng) | Phải lọc bằng RoomBroadcast |
| Deploy lên VPS | Phức tạp | **Đơn giản** |
| Phù hợp | Server rất mạnh, zone cần isolation cứng | **Game vừa, dễ deploy** |

> **Kết luận:** Dự án DoAn dùng kiến trúc **1 port + room_id**. Chỉ cần mở đúng 2 port:
> - `:7777` cho NGO game server
> - `:5000` (hoặc `:443` sau nginx) cho ASP.NET API

---

## 8. Danh Sách File .cs & Trạng Thái

### Unity Client (`Client/Assets/Scripts/`)

| File | Trạng thái | Mô tả |
|---|---|---|
| `Map/MapManager.cs` | ✅ Cập nhật | Auto-fetch mapId từ API khi scene load |
| `Map/ZoneTrigger.cs` | ✅ Cập nhật | 1-port: gửi ServerRpc thay vì reconnect |
| `Map/ZoneRoomManager.cs` | ✅ Mới | Server-side room manager |
| `Map/RoomBroadcast.cs` | ✅ Mới | Utility lọc ClientRpc theo zone |
| `Map/MapTransitionButton.cs` | ✅ Mới | Nút ← / → chuyển map, có UI error feedback |
| `Map/ZoneConnectionManager.cs` | ⚠️ Superseded | Kiến trúc nhiều port cũ — giữ để tham khảo |
| `Player/PlayerZoneHandler.cs` | ✅ Mới | NetworkBehaviour xử lý zone switch ServerRpc |
| `NPC/NpcSpawner.cs` | ✅ Mới | Fetch và spawn NPC từ API |
| `NPC/NpcInteraction.cs` | ✅ Mới | IPointerClickHandler — hoạt động cả PC + mobile |
| `NPC/NpcMenuUI.cs` | ✅ Mới | UI dialogue + shop + feedback text |
| `UI/ElementIconLoader.cs` | ✅ Mới | Load icon hệ nguyên tố từ API (JsonUtility) |
| `Utilities/AuthHelper.cs` | ✅ Mới | Gắn JWT header nhất quán cho mọi request |

### Backend (`GameServerApi/`)

| File | Trạng thái | Thay đổi |
|---|---|---|
| `Models/Entities/MapZoneConfig.cs` | ✅ Cập nhật | `HostPort` → `RoomId` |
| `Data/GameDbContext.cs` | ✅ Cập nhật | Mapping `host_port` → `room_id` |
| `Controllers/MapController.cs` | ✅ Cập nhật | Zone endpoint trả `room_id` + fixed port `7777` |
| `Controllers/NpcController.cs` | ✅ Cập nhật | `[Authorize]` trên buy, playerId từ JWT claim |

---

## 9. Changelog — Lỗi Đã Sửa

| # | Vấn đề | File | Trạng thái |
|---|---|---|---|
| 1 | ZoneTrigger timing: `WaitForSeconds` cứng, NGO chưa shutdown xong | `ZoneTrigger.cs` | ✅ Sửa — không cần shutdown nữa (1-port) |
| 2 | MapTransitionButton thiếu UI error feedback cho player | `MapTransitionButton.cs` | ✅ Sửa — thêm `errorText + ShowError()` |
| 3 | NpcInteraction dùng `OnMouseDown` không chạy mobile | `NpcInteraction.cs` | ✅ Sửa — implement `IPointerClickHandler` |
| 4 | NpcMenuUI wrap JSON thủ công crash nếu API lỗi | `NpcMenuUI.cs` | ✅ Sửa — try-catch + null guard |
| 5 | ElementIconLoader dùng Newtonsoft thay vì JsonUtility | `ElementIconLoader.cs` | ✅ Sửa — viết lại với JsonUtility |
| 6 | Zone host down không báo player | `ZoneTrigger.cs` | ✅ Sửa — lỗi zone hiện Debug log rõ ràng |
| 7 | `PlayerPrefs("USER_ID")` không an toàn — có thể giả mạo | `NpcController.cs` | ✅ Sửa — `[Authorize]` + playerId từ JWT claim |
| 8 | Kiến trúc nhiều port: khó deploy, phải reconnect | `ZoneTrigger + MapZoneConfig` | ✅ Redesign — 1 port + room_id |

