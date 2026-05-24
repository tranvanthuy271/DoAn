# HƯỚNG DẪN MAP & DUNGEON SYSTEM

> **Phiên bản**: 1.0 — Dựa trên phân tích LangLa WayPoint + InfoMap + Zone system

---

## MỤC LỤC

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Cơ sở dữ liệu](#2-cơ-sở-dữ-liệu)
3. [Cấu hình Portal (cửa dịch chuyển)](#3-cấu-hình-portal-cửa-dịch-chuyển)
4. [Cấu hình Dungeon](#4-cấu-hình-dungeon)
5. [Setup Unity](#5-setup-unity)
6. [Luồng dịch chuyển End-to-End](#6-luồng-dịch-chuyển-end-to-end)
7. [Tham chiếu API](#7-tham-chiếu-api)
8. [Troubleshooting](#8-troubleshooting)

---

## 1. Tổng quan kiến trúc

```
[Player chạm BoxCollider2D (Portal Trigger)]
        ↓
[MapPortalTrigger.cs] → POST /api/map/travel
        ↓
[Server kiểm tra: khoảng cách, portal tồn tại, item yêu cầu]
        ↓
[Client nhận DestSceneName + DestX/Y]
        ↓
[PortalArrivalHandler lưu tọa độ → SceneManager.LoadScene]
        ↓
[Scene mới load → Player spawn tại DestX/DestY]
```

**Pattern gốc LangLa (WayPoint):**  
LangLa dùng tọa độ vùng `(l,m) → (n,o)` trên bản đồ tile + `mapHere/mapNext` để định nghĩa cửa dịch chuyển.  
DoAn dùng Unity BoxCollider2D với `MapPortalTrigger.cs` — cùng tư duy nhưng phù hợp kiến trúc Unity 2D.

---

## 2. Cơ sở dữ liệu

### 2.1 Bảng `map_config`

| Cột | Kiểu | Mô tả |
|-----|------|-------|
| map_id | INT PK | ID bản đồ |
| map_name | VARCHAR | Tên hiển thị |
| scene_name | VARCHAR | Tên Scene Unity (SceneManager.LoadScene) |
| min_level | INT | Level tối thiểu để vào |
| max_level | INT | Level tối đa (0 = không giới hạn) |
| is_dungeon | TINYINT | 1 nếu là phó bản |

**Ví dụ:**

```sql
INSERT INTO map_config (map_id, map_name, scene_name, min_level, max_level, is_dungeon)
VALUES
  (1,  'Làng Khởi Đầu',    'VillageScene',    1,  0, 0),
  (14, 'Phó Bản Lửa T1',   'DungeonFire_1',   5, 15, 1),
  (15, 'Phó Bản Lửa T2',   'DungeonFire_2',   5, 15, 1),
  (16, 'Phòng Boss Lửa',   'DungeonFire_Boss', 5, 15, 1);
```

---

### 2.2 Bảng `map_portal`

| Cột | Kiểu | Mô tả |
|-----|------|-------|
| portal_id | INT PK AUTO | |
| portal_name | VARCHAR | Tên cửa (dùng cho debug) |
| source_map_id | INT FK | Bản đồ chứa cửa này |
| src_x | FLOAT | Trung tâm X của cửa (Unity world space) |
| src_y | FLOAT | Trung tâm Y của cửa |
| src_radius | FLOAT | Bán kính vùng trigger (server validate) |
| dest_map_id | INT FK | Bản đồ đích |
| dest_scene_name | VARCHAR | Scene Unity đích |
| dest_x | FLOAT | Tọa độ spawn X khi đến nơi |
| dest_y | FLOAT | Tọa độ spawn Y khi đến nơi |
| portal_type | TINYINT | 0=thường, 1=vào dungeon, 2=ra dungeon |
| required_item_id | INT NULL | ID item cần (NULL = không cần) |
| dungeon_id | INT NULL FK | Dungeon liên quan |
| is_active | TINYINT | 1 = đang hoạt động |

**Ví dụ (Làng → Phó Bản Lửa Tầng 1):**

```sql
INSERT INTO map_portal 
  (portal_name, source_map_id, src_x, src_y, src_radius,
   dest_map_id, dest_scene_name, dest_x, dest_y,
   portal_type, required_item_id, dungeon_id, is_active)
VALUES
  ('Cửa Vào Lửa T1',
   1,        -- source: Làng
   12.5, 3.0, 1.5,
   14,       -- dest: DungeonFire T1
   'DungeonFire_1', 2.0, 1.0,
   1,        -- portal_type: 1 = vào dungeon
   34,       -- required_item_id: Chìa Khóa Phó Bản Lửa
   1,        -- dungeon_id
   1);

-- Cửa chuyển tiếp nội bộ (T1 → T2)
INSERT INTO map_portal 
  (portal_name, source_map_id, src_x, src_y, src_radius,
   dest_map_id, dest_scene_name, dest_x, dest_y,
   portal_type, required_item_id, dungeon_id, is_active)
VALUES
  ('Cửa Lên Tầng 2',
   14, 18.0, 1.0, 1.5,
   15, 'DungeonFire_2', 2.0, 1.0,
   0, NULL, 1, 1);
```

---

### 2.3 Bảng `dungeon_config`

| Cột | Mô tả |
|-----|-------|
| dungeon_id | ID phó bản |
| dungeon_name | Tên |
| dungeon_type | 1=sơ cấp, 2=trung cấp, 3=cao cấp, 4=thượng cấp |
| entry_map_id | Map đầu tiên khi vào dungeon |
| boss_map_id | Map chứa boss |
| boss_enemy_id | ID enemy boss |
| min_level / max_level | Yêu cầu level |
| required_item_id | Item chìa khóa (NULL = tự do) |
| time_limit_minutes | Thời gian giới hạn (0 = vô hạn) |

---

### 2.4 Bảng `boss_config`

| Cột | Mô tả |
|-----|-------|
| boss_id | FK → enemy.enemy_id |
| map_id | Map boss xuất hiện |
| spawn_x / spawn_y | Tọa độ spawn |
| min_spawn_hour / max_spawn_hour | Khung giờ boss hồi sinh (0-23) |
| respawn_minutes | Thời gian hồi sinh sau khi bị giết |

```sql
-- Boss Hỏa Long hồi sinh sau 60 phút, chỉ xuất hiện 9h-22h
INSERT INTO boss_config (boss_id, map_id, spawn_x, spawn_y, min_spawn_hour, max_spawn_hour, respawn_minutes)
VALUES (8, 16, 10.0, 2.0, 9, 22, 60);
```

---

## 3. Cấu hình Portal (cửa dịch chuyển)

### 3.1 Quy trình thêm cửa mới

**Bước 1: Thêm vào DB**

```sql
INSERT INTO map_portal (portal_name, source_map_id, src_x, src_y, src_radius,
  dest_map_id, dest_scene_name, dest_x, dest_y, portal_type, required_item_id, dungeon_id, is_active)
VALUES ('Tên Cửa', [id_map_gốc], [x], [y], [bán_kính],
        [id_map_đích], '[TênScene]', [dest_x], [dest_y],
        [loại], [id_item_hoặc_NULL], [id_dungeon_hoặc_NULL], 1);
```

> **Lưu ý tọa độ**: `src_x/src_y` là trung tâm BoxCollider2D trong Unity **world space**. Dùng Scene View để đọc tọa độ.

**Bước 2: Tạo GameObject Portal trong Unity**

1. Tạo GameObject rỗng, đặt tên `Portal_[TênCửa]`
2. Add **BoxCollider2D** → tick **Is Trigger** → chỉnh Size phủ kín vùng cửa
3. Add **MapPortalTrigger** script
4. Điền `portalId` (khớp DB), `sourceMapId`
5. Đặt Layer `Portal` (tạo layer mới nếu chưa có) — để player có thể trigger

**Bước 3: Physics2D Settings (Project Settings)**

- Đảm bảo layer **Player** và layer **Portal** có thể interact trong **Physics 2D → Layer Collision Matrix**

---

### 3.2 Portal có Chìa Khóa (required_item_id)

Khi server trả về `"denied": true` và `"reason": "item_required"`, Unity hiển thị `keyRequiredPrompt` UI.

Cách thêm:
1. `required_item_id` trong DB = ID item trong `item_template`
2. Unity: trên `MapPortalTrigger`, gán `keyRequiredPrompt` = UI GameObject panel "Cần Chìa Khóa"

---

### 3.3 Portal Type

| Giá trị | Ý nghĩa |
|---------|---------|
| 0 | Cửa bình thường (giữa các khu/tầng) |
| 1 | Vào dungeon |
| 2 | Ra khỏi dungeon |

---

## 4. Cấu hình Dungeon

### 4.1 Cấu trúc 4 Phó Bản hiện tại

```
Dungeon_1: Hang Lửa (Hỏa)
  map 13 → Khu Vực Trước Cửa (world map)
  map 14 → Tầng 1: Hang Lửa Sơ Cấp
  map 15 → Tầng 2: Hang Lửa Thâm Sâu
  map 16 → Phòng Boss: Bào Thai Lửa (Boss: Hỏa Long, enemy_id=8)

Dungeon_2: Cung Băng (Thủy)
  map 17 → Tầng 1
  map 18 → Tầng 2
  map 19 → Phòng Boss (Boss: Đế Băng, enemy_id=11)

Dungeon_3: Rừng Cổ Thụ (Mộc)
  map 20 → Tầng 1
  map 21 → Tầng 2
  map 22 → Phòng Boss (Boss: Rừng Chúa, enemy_id=14)

Dungeon_4: Tháp Bóng Tối (Kim)
  map 23 → Tầng 1
  map 24 → Tầng 2 + 3
  map 25 → Phòng Boss (Boss: Chúa Tể Bóng Tối, enemy_id=17)
```

### 4.2 Thêm Dungeon Mới

1. **DB**: INSERT vào `map_config` (N map mới), INSERT vào `dungeon_config`, INSERT vào `map_portal` (entry + inter-floor + exit), INSERT vào `boss_config`
2. **Unity**: Tạo Scene mới cho mỗi map room, setup portals theo Mục 3.1
3. **Enemy**: INSERT enemy mới với `is_boss=1`, điền `skills_json` và `phases_json`

---

## 5. Setup Unity

### 5.1 MapPortalTrigger Component

```
GameObject: Portal_[Name]
├─ BoxCollider2D (Is Trigger = true)
└─ MapPortalTrigger
   ├─ portalId: [ID từ bảng map_portal]
   ├─ sourceMapId: [ID map hiện tại]
   └─ keyRequiredPrompt: [UI Panel "Cần chìa khóa" — có thể null]
```

### 5.2 Scene Setup cho mỗi Dungeon Room

Cấu trúc Scene đề xuất:

```
[Scene: DungeonFire_1]
├─ Environment/
│   ├─ Tilemap_Ground
│   ├─ Tilemap_Wall
│   └─ Lighting
├─ Portals/
│   ├─ Portal_EnterFromVillage   (BoxCollider2D + MapPortalTrigger)  ← spawn điểm đến từ làng
│   └─ Portal_GoToFloor2        (BoxCollider2D + MapPortalTrigger)  ← đi lên tầng 2
├─ Enemies/
│   ├─ Mob_FireSlime_1 (EnemyAI hoặc MobPatrolAI)
│   └─ Mob_FireSlime_2
└─ SpawnPoints/
    └─ DefaultSpawn (Transform, PlayerSpawnManager sẽ dùng)
```

### 5.3 PlayerSpawnManager Pattern

Trong `Start()` của một `PlayerSpawnManager.cs`:

```csharp
private void Start()
{
    // Kiểm tra nếu có pending arrival từ portal
    if (PortalArrivalHandler.HasPending)
    {
        float x = PortalArrivalHandler.PendingDestX;
        float y = PortalArrivalHandler.PendingDestY;
        PortalArrivalHandler.Clear();
        SpawnPlayerAt(x, y);
    }
    else
    {
        SpawnPlayerAt(defaultSpawn.position.x, defaultSpawn.position.y);
    }
}
```

### 5.4 Build Settings — Thêm Scene mới

1. File → Build Settings → Add Open Scenes
2. Thêm tất cả scene dungeon vào danh sách (phải có trong Build để `SceneManager.LoadScene` hoạt động)

---

## 6. Luồng dịch chuyển End-to-End

### Ví dụ: Vào Dungeon Lửa từ Làng

```
1. Player đứng gần cửa → BoxCollider2D Trigger kích hoạt
   [OnTriggerEnter2D] → kiểm tra IsOwner (NetworkObject)

2. Client gửi: POST /api/map/travel
   Body: { portalId: 1, playerId: 42, currentMapId: 1, playerX: 12.3, playerY: 3.1 }

3. Server (MapController.cs) xử lý:
   a. Load portal từ DB → kiểm tra is_active
   b. Tính dist = √((12.3-12.5)² + (3.1-3.0)²) ≈ 0.22
   c. Kiểm tra dist ≤ srcRadius*2 = 3.0 ✓
   d. portal.required_item_id = 34 → kiểm tra inventory player có item 34 chưa
   e. Nếu có → return { success:true, destSceneName:"DungeonFire_1", destX:2.0, destY:1.0 }
   
4. Client nhận response:
   PortalArrivalHandler.PendingDestX = 2.0
   PortalArrivalHandler.PendingDestY = 1.0
   PortalArrivalHandler.HasPending   = true
   SceneManager.LoadScene("DungeonFire_1")

5. Scene DungeonFire_1 load xong:
   PlayerSpawnManager.Start() → đọc PortalArrivalHandler → spawn player tại (2.0, 1.0)
   PortalArrivalHandler.Clear()
```

### Ví dụ: Thiếu Chìa Khóa

```
3d (server): inventory KHÔNG có item 34
   → return { success:false, denied:true, reason:"item_required", requiredItemId:34 }

4 (client): keyRequiredPrompt.SetActive(true) → hiện 2.5 giây → ẩn
   (player không bị dịch chuyển, đứng yên)
```

---

## 7. Tham chiếu API

### GET /api/map/{mapId}/portals

Trả về danh sách cửa trên bản đồ `mapId`.

**Response:**
```json
[
  {
    "portalId": 1,
    "portalName": "Cửa Vào Lửa T1",
    "sourceMapId": 1,
    "srcX": 12.5, "srcY": 3.0, "srcRadius": 1.5,
    "destMapId": 14,
    "destSceneName": "DungeonFire_1",
    "destX": 2.0, "destY": 1.0,
    "portalType": 1,
    "requiredItemId": 34
  }
]
```

---

### POST /api/map/travel

**Request Body:**
```json
{
  "portalId": 1,
  "playerId": 42,
  "currentMapId": 1,
  "playerX": 12.3,
  "playerY": 3.1
}
```

**Response (thành công):**
```json
{
  "success": true,
  "destSceneName": "DungeonFire_1",
  "destX": 2.0,
  "destY": 1.0,
  "message": "OK"
}
```

**Response (thất bại):**
```json
{
  "success": false,
  "denied": true,
  "reason": "item_required",
  "requiredItemId": 34,
  "message": "Cần Chìa Khóa Phó Bản Lửa"
}
```

---

### GET /api/dungeon/boss/{bossId}/config

Trả về config đầy đủ của boss (skills_json, phases_json, spawn config).

**Response:**
```json
{
  "bossId": 8,
  "bossName": "Hỏa Long",
  "level": 10,
  "baseHp": 1500,
  "baseDamage": 45,
  "skillsJson": "[{\"skill_id\":\"FIRE_BREATH\",...}]",
  "phasesJson": "[{\"hp_pct_threshold\":75,...}]",
  "spawnX": 10.0,
  "spawnY": 2.0
}
```

---

## 8. Troubleshooting

### Portal không trigger

| Nguyên nhân | Giải pháp |
|-------------|-----------|
| BoxCollider2D không tick Is Trigger | Bật Is Trigger |
| Layer không interact | Project Settings → Physics 2D → Layer Collision Matrix → bật Player-Portal |
| Player không có NetworkObject | Cần NetworkObject + IsOwner check |

### Server trả về "portal not found"

- Kiểm tra `portalId` trong DB có `is_active = 1`
- Kiểm tra `sourceMapId` trùng với DB

### Player spawn sai vị trí sau khi dịch chuyển

- Kiểm tra `PortalArrivalHandler.HasPending` trong `PlayerSpawnManager.Start()`
- Đảm bảo gọi `PortalArrivalHandler.Clear()` sau khi dùng tọa độ

### SceneManager.LoadScene lỗi "Scene not found"

- Scene phải được thêm vào **Build Settings → Scenes In Build**
- `dest_scene_name` trong DB phải khớp chính xác tên Scene (case-sensitive)

