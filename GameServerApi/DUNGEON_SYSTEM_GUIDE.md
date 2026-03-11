# Hệ Thống Phó Bản (Dungeon/Instance) — Hướng Dẫn

## 1. Tổng Quan Kiến Trúc

Hệ thống phó bản gồm **2 loại**:

| Loại | `dungeon_type` | Host tạo sau khi login | Session DB |
|---|---|---|---|
| Thử thách 1 mình | `"solo"` | Client **được host chính chỉ định** StartHost() trên máy của họ | Không cần |
| Nhiều người | `"multi"` | **Host chính** tự spawn dungeon host riêng (process/instance độc lập), đăng ký IP:port → gửi `JoinHost` cho client | `dungeon_session` |

> **Nguyên tắc bất biến:** Client KHÔNG BAO GIỜ tự quyết định `StartHost()` hay `StartClient()`. Mọi hành động đều được điều phối bởi **HOST CHÍNH** qua `RequestDungeonEntryServerRpc` → `DungeonCommandClientRpc`.

```
┌──────────────────────────────────────────────────────────┐
│  REST API (GameServerApi)                                │
│    GET  /api/dungeon/list                                │
│    GET  /api/dungeon/{id}                                │
│    GET  /api/dungeon/session/active/{dungeonConfigId}    │
│    POST /api/dungeon/session/create                      │
│    POST /api/dungeon/session/{id}/join                   │
│    POST /api/dungeon/session/{id}/leave                  │
│    POST /api/dungeon/session/{id}/end                    │
│    GET  /api/dungeon/map/{mapId}/setup   (spawn config)  │
└──────────────────────────────────────────────────────────┘
         ↕ HTTP
┌──────────────────────────────────────────────────────────┐
│  Unity Client                                            │
│    DungeonListUI     — Hiện danh sách, nút bấm          │
│    DungeonButtonItem — Mỗi nút 1 phó bản                │
│    DungeonManager    — Logic vào/ra phó bản             │
│    DungeonNetworkBridge — ServerRpc / ClientRpc          │
│    APIClient         — HTTP wrapper                      │
└──────────────────────────────────────────────────────────┘
```

---

## 2. Cơ Sở Dữ Liệu

### 2.1 Bảng `dungeon_config`

| Cột | Kiểu | Mô tả |
|---|---|---|
| `dungeon_id` | INT PK | ID duy nhất |
| `dungeon_name` | VARCHAR(100) | Tên hiển thị |
| `dungeon_type` | ENUM `'solo','multi'` | Loại phó bản |
| `map_id` | INT FK→`map_config` | Map Unity sẽ load (cùng bảng với trận bình thường) |
| `scene_name` | VARCHAR(100) | **Tên build scene Unity** — phải khớp Build Settings |
| `max_players` | INT | 1 với solo, N với multi |
| `min_level_required` | INT | Level tối thiểu để vào |
| `time_limit_seconds` | INT | 0 = không giới hạn |
| `boss_enemy_id` | INT FK→`enemy` (nullable) | Boss của phó bản |
| `reward_json` | JSON | Phần thưởng khi hoàn thành |
| `thumbnail_icon_id` | VARCHAR(50) | ID icon hiển thị trong UI |
| `is_active` | TINYINT | 1 = mở, 0 = ẩn |

> **Liên hệ với `map_config`:** `dungeon_config.map_id` → `map_config.map_id` → `spawn_points_json` là vị trí spawn NGƯỜI CHƠI.

> **Liên hệ với `enemy_spawns`:** Quái trong phó bản được cấu hình bằng cách thêm row vào `enemy_spawns` với `map_id` trùng với `dungeon_config.map_id`.

### 2.2 Bảng `dungeon_session` (chỉ cho multi)

| Cột | Kiểu | Mô tả |
|---|---|---|
| `session_id` | INT PK | ID session |
| `dungeon_config_id` | INT FK | Loại phó bản |
| `host_ip` | VARCHAR(45) | IP của Unity host |
| `host_port` | INT | Port Unity NetworkManager (thường 7777) |
| `current_players` | INT | Số người hiện tại |
| `max_players` | INT | Số người tối đa |
| `status` | ENUM `'waiting','active','ended'` | Trạng thái |

---

## 3. Config Quái Theo Map (enemy_spawns)

**Quái trong phó bản hoàn toàn được cấu hình qua DB**, không hard-code trong scene.

```sql
-- Phó bản "Hang Động Lửa" dùng map_id = 10
-- → Thêm quái vào map_id = 10 trong enemy_spawns

INSERT INTO enemy_spawns (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
VALUES
  (10, 3,   2.5,  -1.0, 3, 30),   -- Quái lửa nhỏ, spawn (2.5, -1.0), tối đa 3 con, respawn 30s
  (10, 3,  -3.0,   0.5, 2, 30),
  (10, 7,   0.0,  -5.0, 1, 60);   -- Boss lửa, spawn (0, -5), không respawn nhanh
```

### API lấy setup map cho host (gọi ngay sau StartHost)

```
GET /api/dungeon/map/{mapId}/setup
```

**Response:**
```json
{
  "map_id": 10,
  "map_name": "Hang Động Lửa",
  "player_spawn_points_json": "[{\"x\":0,\"y\":0},{\"x\":3,\"y\":0}]",
  "enemy_spawns": [
    {
      "spawn_id": 1,
      "enemy_type_id": 3,
      "spawn_x": 2.5,
      "spawn_y": -1.0,
      "max_spawn_count": 3,
      "respawn_time": 30,
      "enemy_name": "Lửa Nhỏ",
      "base_hp": 300,
      "base_damage": 25,
      "exp_reward": 50
    }
  ]
}
```

Unity host gọi endpoint này rồi truyền data xuống `EnemySpawner` để init quái động theo DB.

---

## 4. Config Map — Vị Trí Spawn Người Chơi

Vị trí spawn người chơi trong phó bản được lưu trong `map_config.spawn_points_json`:

```sql
-- Map cho phó bản (map_id = 10)
INSERT INTO map_config (map_id, map_name, scene_name, spawn_points_json)
VALUES (10, 'Hang Động Lửa', 'DungeonScene_FireCave',
        '[{"x":0,"y":0},{"x":3,"y":0},{"x":-3,"y":0},{"x":0,"y":3}]');
```

**Quy tắc:** Mảng `spawn_points_json` có ít nhất `max_players` phần tử để mỗi player có điểm spawn riêng.

---

## 5. Luồng Xử Lý Phó Bản

### 5.1 Solo Dungeon

```
Player click "Hang Động Lửa" (solo)
       ↓
DungeonManager.EnterDungeon(config)
  → bridge.RequestDungeonEntryServerRpc(dungeonId, mapId, "solo", clientId)

─── TRÊN HOST CHÍNH (server-side) ─────────────────────────────────────────
Nhận ServerRpc → dungeon_type = "solo"
  → Gửi DungeonCommandClientRpc("StartSoloHost", dungeonId, mapId) về đúng client
  → Kick client ra khỏi session chính (DisconnectClient)

─── TRÊN CLIENT (sau khi nhận lệnh từ host chính) ──────────────────────────
DungeonManager.ExecuteDungeonCommand("StartSoloHost", ...)
  → NetworkManager.Shutdown()          [rời session chính]
  → SceneManager.LoadScene("DungeonScene_FireCave")
  → NetworkManager.StartHost()         [host chính RA LỆNH mới được gọi]
  → [Lấy spawn config từ /api/dungeon/map/{mapId}/setup để init quái]
```

> ⚠️ **Client KHÔNG tự gọi StartHost() khi chưa nhận lệnh.** Khi host chính kick client, client sẽ tự động nhận `DungeonCommandClientRpc` trước khi bị ngắt kết nối (RPC được đảm bảo giao trước khi disconnect).

### 5.2 Multi Dungeon — Chưa Có Session

```
Player A click "Mê Cung Rừng Rậm" (multi)
       ↓
bridge.RequestDungeonEntryServerRpc(3, mapId, "multi", clientIdA)

─── TRÊN HOST CHÍNH ────────────────────────────────────────────────────────
Nhận ServerRpc → gọi API: GET /api/dungeon/session/active/3
  Kết quả: has_session = false
  → Host chính TỰ spawn dungeon host riêng (process headless / dedicated instance)
  → POST /api/dungeon/session/create
    { dungeon_config_id: 3, host_ip: "192.168.1.10", host_port: 7778 }
  → Nhận session_id = 5
  → Gửi DungeonCommandClientRpc("JoinHost", 3, mapId, "192.168.1.10", 7778, 5) → clientA
  → Kick clientA ra khỏi session chính

─── TRÊN CLIENT A (sau khi nhận lệnh từ host chính) ────────────────────────
ExecuteDungeonCommand("JoinHost", ..., "192.168.1.10", 7778, 5)
  → NetworkManager.Shutdown()
  → LoadScene("DungeonScene_Forest")
  → UnityTransport.SetConnectionData("192.168.1.10", 7778)
  → NetworkManager.StartClient()       [host chính RA LỆNH mới được gọi]
```

> **Lưu ý:** Cả hai case (chưa có session và đã có session) đều kết thúc bằng lệnh `JoinHost` cho client. Sự khác biệt là host chính cần spawn dungeon host trước khi gửi lệnh nếu chưa có session.

### 5.3 Multi Dungeon — Đã Có Session

```
Player B click "Mê Cung Rừng Rậm"
       ↓
brige.RequestDungeonEntryServerRpc(3, mapId, "multi", clientIdB)

─── TRÊN HOST CHÍNH ────────────────────────────────────────────────────────
Nhận ServerRpc → gọi API: GET /api/dungeon/session/active/3
  Kết quả: has_session = true, session_id=5, host="192.168.1.20":7777, slots=1/4
  → API: POST /api/dungeon/session/5/join
  → Gửi DungeonCommandClientRpc("JoinHost", 3, mapId, "192.168.1.20", 7777, 5) → clientB
  → Kick clientB ra khỏi session chính

─── TRÊN CLIENT B (sau khi nhận lệnh từ host chính) ────────────────────────
ExecuteDungeonCommand("JoinHost", ..., "192.168.1.20", 7777, 5)
  → NetworkManager.Shutdown()
  → LoadScene("DungeonScene_Forest")
  → UnityTransport.SetConnectionData("192.168.1.20", 7777)
  → NetworkManager.StartClient()       [host chính RA LỆNH mới được gọi]
```

---

## 6. Setup Unity — Hướng Dẫn Từng Bước

### Bước 1: Tạo Scene Phó Bản

1. Tạo scene mới, đặt tên khớp với cột `scene_name`.  
   Ví dụ: `DungeonScene_FireCave`
2. Thêm scene vào **File → Build Settings** (đúng thứ tự index).
3. Trong scene: đặt `NetworkManager`, `DungeonNetworkBridge`, `DungeonManager`, `EnemySpawner`.

### Bước 2: Tạo DungeonNetworkBridge  Prefab

1. Tạo GameObject tên `DungeonBridgeObject`.
2. Gắn component: `NetworkObject` + `DungeonNetworkBridge`.
3. Kéo vào **NetworkManager → Network Prefabs**.
4. Host spawn object này ngay khi `OnServerStarted`. (Hoặc đặt thẳng trong scene với `NetworkObject`.)

### Bước 3: Tạo DungeonButtonItemPrefab

Cấu trúc Prefab UI:
```
DungeonButtonItemPrefab
├─ Button (Component)
├─ DungeonButtonItem (Component)
├─ Background (Image)
├─ Icon (Image)           ← thumbnail
├─ NameText (Text)
├─ TypeBadge
│   ├─ BadgeBg (Image)   ← tô màu cam/xanh
│   └─ TypeBadgeText (Text)
├─ LevelText (Text)      ← "Yêu cầu Lv.X"
├─ DescText (Text)
├─ SlotText (Text)        ← "0/4" — hiện với multi
└─ LockOverlay (GameObject) ← che khi locked
```

Assign từng field trong `DungeonButtonItem` Inspector.

### Bước 4: Setup DungeonListUI

1. Tạo Canvas > Panel `DungeonPanel`, gắn `DungeonListUI`.
2. Bên trong Panel thêm:
   - `ScrollView → Content` → assign vào `dungeonListContent`
   - `StatusText` (Text)
   - `CloseButton` (Button)
   - `ConfirmDialog` (Panel con — tuỳ chọn)
3. Nút mở panel `OpenDungeonBtn` đặt trên HUD → assign vào `openDungeonBtn`.
4. Assign `dungeonItemPrefab` = prefab đã tạo ở Bước 3.

### Bước 5: Setup DungeonManager

1. Tạo GameObject `DungeonManager` (persistent, DontDestroyOnLoad).
2. Gắn component `DungeonManager`.
3. `DungeonManager` **không tự xử lý gì** — chỉ gửi `RequestDungeonEntryServerRpc` và đợi `ExecuteDungeonCommand` được gọi lại từ `DungeonNetworkBridge`.
4. Không cần assign gì thêm.

---

## 7. Database — Thêm/Sửa Phó Bản

### Thêm phó bản mới

```sql
-- 1. Tạo map config
INSERT INTO map_config (map_id, map_name, scene_name, spawn_points_json)
VALUES (20, 'Núi Lửa Cổ Đại', 'DungeonScene_Volcano',
        '[{"x":0,"y":1},{"x":4,"y":1},{"x":-4,"y":1}]');

-- 2. Thêm quái cho map này
INSERT INTO enemy_spawns (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
VALUES
  (20, 5, 3.0,  0.0, 4, 25),   -- Quái lửa trung bình
  (20, 5, -3.0, 0.0, 4, 25),
  (20, 8, 0.0, -8.0, 1, 120);  -- Boss núi lửa

-- 3. Tạo dungeon config
INSERT INTO dungeon_config
  (dungeon_name, dungeon_type, map_id, scene_name, max_players,
   min_level_required, time_limit_seconds, description, boss_enemy_id, thumbnail_icon_id)
VALUES
  ('Núi Lửa Cổ Đại', 'multi', 20, 'DungeonScene_Volcano', 4,
   20, 600, 'Phó bản 4 người — leo núi lửa và tiêu diệt Thần Lửa.', 8, 'icon_dungeon_volcano');
```

### Tắt phó bản (không xoá)

```sql
UPDATE dungeon_config SET is_active = 0 WHERE dungeon_id = 3;
```

### Điều chỉnh spawn quái

```sql
-- Tăng số quái tối đa
UPDATE enemy_spawns SET max_spawn_count = 6 WHERE spawn_id = 12;

-- Thêm quái boss
INSERT INTO enemy_spawns (map_id, enemy_type_id, spawn_x, spawn_y, max_spawn_count, respawn_time)
VALUES (20, 9, 0.0, -10.0, 1, 999);
```

---

## 8. API Reference

### GET /api/dungeon/list
Lấy danh sách phó bản active (đã sắp xếp theo `min_level_required`).

**Response:**
```json
{
  "dungeons": [
    {
      "dungeon_id": 1,
      "dungeon_name": "Hang Động Lửa",
      "dungeon_type": "solo",
      "map_id": 10,
      "scene_name": "DungeonScene_FireCave",
      "max_players": 1,
      "min_level_required": 5,
      "time_limit_seconds": 300,
      "description": "..."
    }
  ]
}
```

### GET /api/dungeon/{dungeonId}
Chi tiết một phó bản gồm cả `enemy_spawns` và `player_spawn_points`.

### GET /api/dungeon/session/active/{dungeonConfigId}
```json
{ "has_session": true, "session": { "session_id": 5, "host_ip": "192.168.1.10", "host_port": 7777, "current_players": 2, "max_players": 4, "status": "waiting" } }
```

### POST /api/dungeon/session/create
```json
{ "dungeon_config_id": 3, "host_ip": "192.168.1.10", "host_port": 7777 }
```

### GET /api/dungeon/map/{mapId}/setup
Dùng cho Unity host sau StartHost() để lấy config spawn quái và vị trí spawn player.

---

## 9. Lưu Ý Quan Trọng

### Nguyên tắc điều phối bởi host chính
Client **không bao giờ** tự quyết định `StartHost()` hay `StartClient()`. Toàn bộ quyết định thuộc về host chính:
- Host chính kiểm tra DB session
- Host chính gửi `DungeonCommandClientRpc` với lệnh cụ thể (`StartSoloHost` cho solo / `JoinHost` cho multi)
- Host chính kick client ra khỏi session chính
- Client **chỉ thực thi lệnh nhận được**

### Ai làm dungeon host?
- **Solo**: Client được host chính chỉ định → client `StartHost()` trên máy của họ
- **Multi**: **Host chính** tự spawn dungeon host riêng (process headless / dedicated instance) → tất cả client chỉ nhận lệnh `JoinHost` và `StartClient()` để connect vào
- Host chính đăng ký session (IP:port) vào DB rồi mới gửi lệnh cho client
- Trên **dedicated server**: host machine spawn process headless mới cho mỗi dungeon multi, giữ nguyên overworld session

### Multi dungeon — port động
Hiện tại mặc định dùng port **7777**. Nếu nhiều phó bản multi chạy trên cùng máy → cần cấp phát port động.

### LAN vs Internet
`GetLocalIP()` trả về IP LAN. Nếu game chạy qua Internet, cần dùng **relay** (Steam Relay, Unity Relay). Thay `host_ip` bằng `join_code` khi dùng Unity Relay.

### Orphan sessions
API tự động dọn session cũ hơn 1 giờ khi có request `POST /session/create`. Có thể thêm **background cleanup job** nếu cần dọn dẹp tức thì.

### Map ID vs Scene Name
- `map_id` — định danh logic, dùng để query `enemy_spawns` và `map_config`.
- `scene_name` — tên file scene Unity, dùng để `SceneManager.LoadScene(sceneName)`.
- Một `map_id` có thể dùng lại cho nhiều phó bản nếu muốn cùng layout.

---

## 10. Tóm Tắt File Liên Quan

| File | Vị trí | Mô tả |
|---|---|---|
| `DungeonConfig.cs` | `Models/Entities/` | Entity DB |
| `DungeonSession.cs` | `Models/Entities/` | Entity DB |
| `DungeonDtos.cs` | `Models/DTOs/` | DTOs cho request body |
| `DungeonController.cs` | `Controllers/` | REST API endpoints |
| `GameDbContext.cs` | `Data/` | EF Core mapping |
| `migration_dungeon.sql` | `GameServerApi/` | SQL để tạo bảng + dữ liệu mẫu |
| `DungeonManager.cs` | `Scripts/Network/Dungeon/` | Logic vào/ra phó bản |
| `DungeonNetworkBridge.cs` | `Scripts/Network/Dungeon/` | ServerRpc / ClientRpc |
| `DungeonListUI.cs` | `Scripts/UI/HUD/` | Panel danh sách phó bản |
| `DungeonButtonItem.cs` | `Scripts/UI/HUD/` | Mỗi nút 1 phó bản |
| `APIClient.cs` | `Scripts/Services/Api/` | HTTP calls đến REST API |
