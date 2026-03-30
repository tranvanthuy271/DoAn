# Hướng Dẫn: Hệ Thống Config Spawn Enemy Theo Map (DB-Driven, Host Validates)

> Phiên bản: 1.0 — 28/03/2026

---

## 1. Đánh Giá Thiết Kế Đề Xuất & Cải Tiến

### Đề xuất ban đầu của bạn
- Lưu JSON `{idenemy, hp, exp, cx, cy, isboss}` lặp lại cho nhiều vị trí vào DB theo từng mapId.
- Thêm chuỗi `"itemid,tỉ lệ;itemid,tỉ lệ;..."` cho drop.
- Unity host fetch và overwrite stats + drop khi spawn.

### Vấn đề cần cải thiện

| Vấn đề | Cải tiến |
|---|---|
| Chuỗi `"itemid,rate;"` dễ sai format, khó validate | Dùng JSON array `[{"item_id":1,"rate":0.25,"qty_min":1,"qty_max":1}]` |
| Drop bị lặp lại y hệt cho mỗi vị trí spawn cùng loại quái | Tách drop ra object riêng, key là `enemy_id` — tránh dư thừa |
| Thiếu `count` (số lượng quái tại một vị trí) | Thêm field `count` vào mỗi spawn entry |
| Thiếu `respawn_time` per-spawn-point | Thêm `respawn_time` để từng điểm có thời gian khác nhau |
| Không có fallback khi `hp=0` hoặc `exp=0` | Host tự động fallback về `base_hp`/`exp_reward` từ bảng `enemy` |
| Không validate giới hạn drop rate | Host tổng cộng rate > 1.0 phải log warning |

### Thiết kế sau cải tiến — 2 JSON trong bảng `map_spawn_config`

```
map_spawn_config
├── map_id          (unique FK → map_config)
├── spawn_json      (array of spawn entries — mỗi entry = 1 vị trí)
└── drop_json       (array of drop rules — mỗi entry = 1 loại quái)
```

---

## 2. Cấu Trúc DB

### Bảng mới: `map_spawn_config`

```sql
CREATE TABLE map_spawn_config (
  id         INT AUTO_INCREMENT PRIMARY KEY,
  map_id     INT NOT NULL UNIQUE,
  spawn_json LONGTEXT NOT NULL DEFAULT '[]',
  drop_json  LONGTEXT NOT NULL DEFAULT '[]',
  updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  FOREIGN KEY (map_id) REFERENCES map_config(map_id)
);
```

> **Ghi chú:** Bảng `enemy_spawns` (individual rows) vẫn giữ lại để tương thích ngược. `map_spawn_config` là lớp config JSON bổ sung dành cho Unity host.

---

## 3. Format JSON Chi Tiết

### 3.1 `spawn_json` — Danh Sách Vị Trí Spawn

Mỗi phần tử = 1 điểm spawn trên map. Cùng `enemy_id` có thể xuất hiện tại nhiều vị trí khác nhau — chỉ cần thêm phần tử mới.

```json
[
  {
    "enemy_id":    1,
    "hp":          200,
    "exp":         50,
    "cx":          100.5,
    "cy":          60.0,
    "is_boss":     false,
    "count":       2,
    "respawn_time": 30
  },
  {
    "enemy_id":    1,
    "hp":          200,
    "exp":         50,
    "cx":          340.0,
    "cy":          120.0,
    "is_boss":     false,
    "count":       1,
    "respawn_time": 30
  },
  {
    "enemy_id":    3,
    "hp":          8000,
    "exp":         2000,
    "cx":          512.0,
    "cy":          256.0,
    "is_boss":     true,
    "count":       1,
    "respawn_time": 300
  }
]
```

| Field | Kiểu | Mô tả | Fallback khi bằng 0 |
|---|---|---|---|
| `enemy_id` | int | FK → enemy.enemy_id | Bắt buộc > 0 |
| `hp` | int | HP ghi đè (overwrite) | Dùng `enemy.base_hp` |
| `exp` | int | EXP thưởng khi kill | Dùng `enemy.exp_reward` |
| `cx` | float | Tọa độ X spawn (world space Unity) | Bắt buộc ≠ 0 |
| `cy` | float | Tọa độ Y spawn (world space Unity) | Bắt buộc ≠ 0 |
| `is_boss` | bool | Bật BossAI, boss health bar, boss BGM | false |
| `count` | int | Số lượng quái tại điểm này | 1 |
| `respawn_time` | int | Giây đến khi hồi sinh | 30 |

### 3.2 `drop_json` — Tỉ Lệ Rơi Item Per Enemy Type

Mỗi phần tử = 1 loại quái, kèm danh sách item có thể rơi.

```json
[
  {
    "enemy_id": 1,
    "items": [
      { "item_id": 10, "rate": 0.25, "qty_min": 1, "qty_max": 1 },
      { "item_id": 15, "rate": 0.05, "qty_min": 1, "qty_max": 2 }
    ]
  },
  {
    "enemy_id": 3,
    "items": [
      { "item_id": 50, "rate": 0.10, "qty_min": 1, "qty_max": 2 },
      { "item_id": 60, "rate": 1.00, "qty_min": 5, "qty_max": 10 }
    ]
  }
]
```

| Field | Kiểu | Mô tả |
|---|---|---|
| `enemy_id` | int | Áp dụng cho quái này trên map |
| `items[].item_id` | int | ID item trong `item_template` |
| `items[].rate` | float | Tỉ lệ rơi, **0.0 – 1.0** (0.25 = 25%) |
| `items[].qty_min` | int | Số lượng tối thiểu mỗi lần rơi |
| `items[].qty_max` | int | Số lượng tối đa |

> **Quan trọng:** `rate` luôn dùng hệ thập phân 0.0–1.0, KHÔNG phải phần trăm 0–100.

---

## 4. API Endpoint

### `GET /api/map/{mapId}/spawn-config`

**Response mẫu:**

```json
{
  "map_id": 1,
  "spawns": [
    {
      "enemy_id": 1,
      "hp": 200,
      "exp": 50,
      "cx": 100.5,
      "cy": 60.0,
      "is_boss": false,
      "count": 2,
      "respawn_time": 30
    }
  ],
  "drops": [
    {
      "enemy_id": 1,
      "items": [
        { "item_id": 10, "rate": 0.25, "qty_min": 1, "qty_max": 1 }
      ]
    }
  ]
}
```

Nếu chưa có config → trả về `{ "map_id": X, "spawns": [], "drops": [] }` (200 OK, không lỗi).

---

## 5. Kế Hoạch Thực Hiện

### Phase 1 — DB + API (Backend)

1. Chạy file migration SQL để tạo bảng `map_spawn_config`.
2. Seed dữ liệu test cho map 0 (Làng Khởi Đầu).
3. Thêm entity `MapSpawnConfig` vào `GameServerApi`.
4. Đăng ký `DbSet<MapSpawnConfig>` trong `GameDbContext`.
5. Thêm endpoint `GET /api/map/{mapId}/spawn-config` vào `MapController`.
6. Test endpoint qua Swagger hoặc curl.

### Phase 2 — Unity Data Layer

7. Tạo `MapSpawnConfigDto.cs` — định nghĩa các class C# để `JsonUtility` deseralize JSON response.
8. Tạo `EnemyStatOverride.cs` — component gắn vào Enemy prefab, lưu HP/EXP/IsBoss ghi đè.

### Phase 3 — Unity Host Logic

9. Tạo `HostSpawnConfigLoader.cs` — MonoBehaviour chỉ chạy trên host:
   - Gọi API `spawn-config` khi scene load xong.
   - Validate toàn bộ config.
   - Thay thế logic spawn của `NetworkEnemySpawner` bằng dữ liệu đã load.
10. Tích hợp `HostSpawnConfigLoader` vào `HostSceneInitializer` — kích hoạt sau khi host start.

### Phase 4 — Drop System

11. Thêm method `SetDropsFromConfig(...)` vào `EnemyItemDrop.cs`.
12. `HostSpawnConfigLoader` gọi method này ngay sau khi spawn mỗi enemy.
13. Test: kill enemy → item rơi đúng rate.

### Phase 5 — Kiểm Thử & Fine-Tune

14. Test trường hợp `hp=0` trong spawn_json → phải dùng fallback `base_hp`.
15. Test trường hợp `enemy_id` không có prefab → skip + log warning.
16. Test `rate` > 1.0 → log warning nhưng không crash.
17. Test map không có config trong DB → spawn bình thường từ `enemy_spawns` cũ.

---

## 6. Thiết Kế Class Unity (Vai Trò Từng Class)

### 6.1 `MapSpawnConfigDto` — Data Transfer Objects
- **Vị trí:** `Assets/Scripts/Network/Enemy/`
- **Không gắn vào GameObject**, chỉ là class dữ liệu thuần.
- Định nghĩa 4 class: `MapSpawnConfigResponse`, `SpawnEntry`, `DropEntry`, `DropItemEntry`.
- Dùng `[System.Serializable]` để `JsonUtility.FromJson<>()` hoạt động.
- Xử lý field `count` mặc định 1, `respawn_time` mặc định 30.

### 6.2 `HostSpawnConfigLoader` — Điều Phối Spawning Từ DB
- **Vị trí:** `Assets/Scripts/Network/Enemy/`
- **Extends:** `NetworkBehaviour`
- **Chỉ chạy trên host/server** — kiểm tra `IsServer` trước mọi thao tác.
- **Lifecycle:**
  1. `OnNetworkSpawn()` → gọi `StartCoroutine(LoadAndApplyConfig())`
  2. Fetch JSON từ `GET /api/map/{mapId}/spawn-config`
  3. Validate từng `SpawnEntry` (HP, vị trí, count)
  4. Validate từng `DropEntry` (rate, item_id, qty)
  5. Với mỗi SpawnEntry: lấy prefab từ `EnemyPrefabManager`, spawn `count` lần, gọi `EnemyStatOverride.Apply()`
  6. Fire event `OnSpawnComplete` → thông báo hệ thống khác
- **Fallback:** Nếu spawn-config API trả về rỗng, gọi `NetworkEnemySpawner.LoadAndSpawnEnemies()` như cũ.
- **Inspector fields:** `apiBaseURL`, `mapId`, reference tới `EnemyPrefabManager`.

### 6.3 `EnemyStatOverride` — Ghi Đè Thông Số Per-Instance
- **Vị trí:** `Assets/Scripts/Enemy/`
- **Extends:** `MonoBehaviour`
- Gắn vào **Enemy Prefab** (thêm tự động khi không có).
- Lưu: `OverrideHp`, `OverrideExp`, `IsBoss`, `RespawnTime`.
- Method `Apply()` được gọi ngay sau `NetworkObject.Spawn()`:
  - Gọi `NetworkEnemyHealth.InitHealth(OverrideHp)` để đặt HP.
  - Nếu `IsBoss = true`: kích hoạt `BossAI` component (hoặc `EnemyAI` ở boss mode).
  - Lưu `OverrideExp` để `NetworkEnemyHealth.HandleDeath()` trả đúng EXP.
- Không sync qua mạng — chỉ server cần gọi `Apply()`.

### 6.4 `EnemyItemDrop` — Cập Nhật Hỗ Trợ Config Từ DB
> File đã có tại `Assets/Scripts/Enemy/EnemyItemDrop.cs` — **chỉ bổ sung**, không viết lại.
- Thêm method `SetDropsFromConfig(List<DropItemEntry> items)`.
- Method này clear `dropItems` list hiện tại và thay thế bằng entries từ config.
- Nếu không được gọi (không có DB config): dùng list cũ trong Inspector như bình thường.

### 6.5 `NetworkEnemySpawner` — Điều Chỉnh Để Hỗ Trợ Override
> File đã có tại `Assets/Scripts/Network/Enemy/NetworkEnemySpawner.cs` — **chỉ bổ sung**.
- Expose method `SpawnEnemyWithOverride(SpawnEntry entry, DropEntry drops)` public để `HostSpawnConfigLoader` gọi.
- Giữ nguyên logic cũ cho backward compatibility.

---

## 7. Quy Trình Host Validate Chi Tiết

Khi host vào map, trước khi spawn bất kỳ enemy nào:

```
  API: GET /api/map/{mapId}/spawn-config
        │
        ▼
  Nhận JSON response
        │
        ▼
  [Validate từng SpawnEntry]
  ├── enemy_id ≤ 0?          → SKIP + log error
  ├── cx == 0 AND cy == 0?   → SKIP + log warning (vị trí gốc thế giới thường không hợp lệ)
  ├── hp ≤ 0?                → FALLBACK: đọc base_hp từ EnemyPrefabManager
  ├── exp < 0?               → SET exp = 0
  ├── count ≤ 0?             → SET count = 1
  └── respawn_time ≤ 0?     → SET respawn_time = 30
        │
        ▼
  [Validate từng DropEntry]
  ├── enemy_id không có trong spawns? → log warning (drop rule thừa)
  ├── item_id ≤ 0?            → SKIP item này
  ├── rate < 0?               → SET rate = 0
  ├── rate > 1?               → CLAMP to 1.0 + log warning
  ├── qty_min < 1?            → SET qty_min = 1
  └── qty_min > qty_max?      → SWAP min/max
        │
        ▼
  Spawn enemies với thông số đã validate
        │
        ▼
  Gắn EnemyStatOverride + SetDropsFromConfig cho mỗi enemy
        │
        ▼
  Log kết quả: "{N} enemies spawned across {K} types, {M} drop rules applied"
```

---

## 8. Config Trong Unity Inspector

### `HostSpawnConfigLoader` component cần set:

| Field | Giá trị mẫu | Ghi chú |
|---|---|---|
| `Api Base URL` | `http://localhost:5000/api` | Đổi sang IP server production |
| `Map Id` | `0` | Để `0` sẽ auto-lấy từ `MapManager.Instance` |
| `Enemy Prefab Manager` | (drag) | Reference tới `EnemyPrefabManager` trong scene |
| `Fallback To Old Spawner` | `true` | Nếu API trả về rỗng, dùng `NetworkEnemySpawner` cũ |

### Quy tắc đặt prefab:

- Enemy thường: **phải có** `NetworkEnemyHealth`, `EnemyAI`, `NetworkObject`, `NetworkTransform`, `EnemyItemDrop`.
- Enemy boss: thêm `BossAI` (hoặc cờ `isBoss` trong `EnemyAI`), boss health bar prefab riêng.
- `EnemyStatOverride` **không cần đặt trong prefab** — `HostSpawnConfigLoader` tự `AddComponent` nếu thiếu.

### Thứ tự component trong HostScene:

1. `NetworkManager` (sẵn có)
2. `HostSceneInitializer` (sẵn có) → kích hoạt host
3. `HostSpawnConfigLoader` → chạy sau khi host start, fetch + spawn enemies
4. `NetworkEnemySpawner` → giữ lại làm fallback

---

## 9. Ví Dụ Config Hoàn Chỉnh Cho Map 0 (Làng Khởi Đầu)

Chạy SQL sau để thêm config mẫu:

```sql
INSERT INTO map_spawn_config (map_id, spawn_json, drop_json)
VALUES (
  0,
  '[
    {"enemy_id":1,"hp":120,"exp":30,"cx":8.5,"cy":3.0,"is_boss":false,"count":2,"respawn_time":25},
    {"enemy_id":1,"hp":120,"exp":30,"cx":-6.0,"cy":2.5,"is_boss":false,"count":1,"respawn_time":25},
    {"enemy_id":2,"hp":80,"exp":20,"cx":15.0,"cy":0.0,"is_boss":false,"count":3,"respawn_time":20},
    {"enemy_id":4,"hp":800,"exp":200,"cx":0.0,"cy":8.0,"is_boss":true,"count":1,"respawn_time":180}
  ]',
  '[
    {"enemy_id":1,"items":[
      {"item_id":22,"rate":0.30,"qty_min":1,"qty_max":2},
      {"item_id":10,"rate":0.05,"qty_min":1,"qty_max":1}
    ]},
    {"enemy_id":2,"items":[
      {"item_id":22,"rate":0.20,"qty_min":1,"qty_max":1}
    ]},
    {"enemy_id":4,"items":[
      {"item_id":50,"rate":1.00,"qty_min":1,"qty_max":1},
      {"item_id":10,"rate":0.50,"qty_min":1,"qty_max":2},
      {"item_id":21,"rate":0.10,"qty_min":1,"qty_max":1}
    ]}
  ]'
);
```

---

## 10. Sơ Đồ Luồng Tổng Quát

```
[GameManager] DB MySQL
      |
      |  GET /api/map/{id}/spawn-config
      ▼
[GameServerApi] (ASP.NET Core)
  MapController.GetSpawnConfig()
      |
      ▼
[Unity Host - HostSpawnConfigLoader]
  1. Fetch JSON
  2. Deserialize → MapSpawnConfigResponse
  3. Validate SpawnEntries + DropEntries
  4. For each SpawnEntry (count times):
       a. EnemyPrefabManager.GetPrefab(enemy_id)
       b. Instantiate + NetworkObject.Spawn()
       c. EnemyStatOverride.Apply(hp, exp, is_boss)
       d. EnemyItemDrop.SetDropsFromConfig(drops[enemy_id])
  5. Log kết quả
      |
      ▼
[Unity Clients] (auto-sync qua Netcode NetworkVariable)
  - Nhận HP/position qua NetworkVariable/NetworkTransform
  - Không cần biết gì về config DB
```

---

## 11. Các File Liên Quan

| File | Loại | Hành động |
|---|---|---|
| `migration_map_spawn_config.sql` | SQL | Chạy để tạo bảng |
| `MapSpawnConfig.cs` | C# (API) | Entity mới |
| `GameDbContext.cs` | C# (API) | Thêm DbSet + mapping |
| `MapController.cs` | C# (API) | Thêm endpoint |
| `MapSpawnConfigDto.cs` | C# (Unity) | DTOs deserialization |
| `HostSpawnConfigLoader.cs` | C# (Unity) | Main loader + validator |
| `EnemyStatOverride.cs` | C# (Unity) | Override HP/EXP/IsBoss |
| `EnemyItemDrop.cs` | C# (Unity) | Thêm SetDropsFromConfig() |
