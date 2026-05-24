# Hướng Dẫn Config Hệ Thống Phó Bản (Zone-Based Dungeon)

> Kiến trúc mới: phó bản chạy trên **cùng server**, dùng zone transfer — không còn Shutdown/StartHost.

---

## Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Config Database (GameServerApi)](#2-config-database)
3. [Config MapWorldConfig (Unity ScriptableObject)](#3-config-mapworldconfig)
4. [Config Scene Unity](#4-config-scene-unity)
5. [Config NPC phó bản](#5-config-npc-phó-bản)
6. [Config Dungeon Instance (Runtime)](#6-config-dungeon-instance)
7. [Config Boss & Enemy Spawn](#7-config-boss--enemy-spawn)
8. [Config Reward](#8-config-reward)
9. [Thêm phó bản mới — Checklist](#9-thêm-phó-bản-mới--checklist)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Tổng quan kiến trúc

```
Player tương tác NPC (npc_type='dungeon')
  → DungeonNpcMenuUI hiện danh sách (GET /api/dungeon/list)
  → Player chọn + Confirm
  → DungeonManager gọi ZoneTransitionController ServerRpc
  → Server tạo custom room (zoneId âm) trên dungeon map
  → Client load scene additive, không disconnect
  → Khi xong/hết giờ → ExitDungeon → về overworld
  → Room tự xóa khi trống
```

**Các thành phần cần config:**

| Thành phần | Ở đâu | Mục đích |
|------------|--------|----------|
| `dungeon_config` | DB | Định nghĩa phó bản (tên, loại, map, thời gian, boss) |
| `map_config` | DB | Map tương ứng với phó bản (scene_name) |
| MapWorldConfig | Unity SO | Server nhận diện map topology (InstanceOnly) |
| Scene Unity | Unity Editor | Scene thực tế của phó bản |
| NPC | DB `npc_config` | NPC mở menu phó bản |
| BaseDungeonInstance | Unity Prefab | Logic runtime phó bản (spawn enemy, timer, reward) |

---

## 2. Config Database

### 2.1. Bảng `map_config` — Đăng ký map cho phó bản

Mỗi phó bản cần một `map_id` riêng trong `map_config`.

```sql
INSERT INTO map_config (map_id, map_name, scene_name, spawn_points_json, min_level, max_level)
VALUES (110, 'DungeonWave', 'DungeonWaveScene', '[]', 1, 999);
```

| Cột | Giá trị | Ghi chú |
|-----|---------|---------|
| `map_id` | Số nguyên duy nhất | Dùng **110+** cho dungeon để không trùng overworld (0-8) |
| `map_name` | Tên hiển thị | |
| `scene_name` | **Phải khớp** tên scene Unity (không có .unity) | VD: `DungeonWaveScene` |
| `spawn_points_json` | `'[]'` | Dungeon dùng entry points từ dungeon config, không cần |

### 2.2. Bảng `dungeon_config` — Định nghĩa phó bản

```sql
INSERT INTO dungeon_config 
  (dungeon_id, dungeon_name, dungeon_type, map_id, scene_name,
   max_players, min_level_required, time_limit_seconds,
   description, boss_enemy_id, reward_json, thumbnail_icon_id, is_active)
VALUES 
  (6, 'Phó Bản Sóng', 'solo', 110, 'DungeonWaveScene',
   1, 1, 300,
   'Phó bản thử thách solo, vượt qua các đợt quái', 
   NULL, '{}', 'icon_dungeon_wave', 1);
```

| Cột | Ý nghĩa | Giá trị |
|-----|---------|---------|
| `dungeon_type` | Solo hay party | `'solo'` hoặc `'multi'` |
| `map_id` | **Phải trùng** `map_config.map_id` | VD: `110` |
| `scene_name` | **Phải trùng** `map_config.scene_name` | VD: `DungeonWaveScene` |
| `max_players` | Solo = `1`, Multi = `2-8` | |
| `time_limit_seconds` | Giới hạn thời gian | `0` = vô hạn, `300` = 5 phút |
| `boss_enemy_id` | FK → `enemy.enemy_id` | `NULL` nếu không có boss |
| `reward_json` | JSON phần thưởng | Xem [mục 8](#8-config-reward) |
| `is_active` | Hiện trong danh sách? | `1` = hiện, `0` = ẩn |

### 2.3. Dữ liệu mẫu hiện có

| dungeon_id | Tên | Type | map_id | Scene |
|------------|-----|------|--------|-------|
| 6 | Phó Bản Sóng | solo | 110 | DungeonWaveScene |
| 7 | Phó Bản Tổ Đội | multi | 111 | DungeonPartyScene |

---

## 3. Config MapWorldConfig

### 3.1. API Runtime Bootstrap (tự động)

Khi `loadMapsFromApiOnBoot = true`, server khởi động sẽ gọi `GET /api/map/runtime-bootstrap` và nhận danh sách map gồm:

```json
{
  "map_id": 110,
  "map_name": "DungeonWave",
  "scene_name": "DungeonWaveScene",
  "zone_topology": 1,
  "allow_custom_zones": true,
  "public_zone_count_override": 0,
  "custom_zone_max_players_override": 0,
  "allow_player_zone_switch": false
}
```

**Quy tắc tự động:**
- Map có `dungeon_config` nào tham chiếu → `zone_topology = 1` (InstanceOnly), `allow_custom_zones = true`
- Map thường → `zone_topology = 0` (SharedPublic)

→ **Không cần config thủ công** trong MapWorldConfig ScriptableObject nếu `loadMapsFromApiOnBoot = true`.

### 3.2. Config thủ công (fallback)

Nếu muốn config offline hoặc override, mở MapWorldConfig asset trong Unity:

```
Assets/ → tìm MapWorldConfig (ScriptableObject)
  → maps[] → Add Element
```

| Field | Giá trị cho dungeon |
|-------|---------------------|
| `mapId` | `110` (trùng DB) |
| `mapName` | `"DungeonWave"` |
| `sceneName` | `"DungeonWaveScene"` |
| `zoneTopology` | **InstanceOnly** |
| `allowCustomZones` | **✓ true** |
| `publicZoneCountOverride` | `0` (không tạo public zone) |
| `customZoneMaxPlayersOverride` | `0` (dùng mặc định = `instanceMapMaxPlayers`) |
| `allowPlayerZoneSwitch` | `false` |

### 3.3. Các setting quan trọng trên MapWorldConfig

| Setting | Default | Ảnh hưởng dungeon |
|---------|---------|-------------------|
| `instanceMapMaxPlayers` | `8` | Max players mỗi custom room (dungeon) |
| `fallbackMapId` | `0` | Map về khi exit dungeon không chỉ định returnMapId |

---

## 4. Config Scene Unity

### 4.1. Tạo scene mới

1. **File → New Scene** → đặt tên **đúng** với `scene_name` trong DB (VD: `DungeonWaveScene`)
2. Lưu vào `Assets/Scenes/DungeonWaveScene.unity`
3. **File → Build Settings → Add Open Scenes** — đảm bảo scene nằm trong Build Settings

### 4.2. Nội dung scene phó bản

Scene phó bản cần có:

| GameObject | Component | Bắt buộc | Ghi chú |
|------------|-----------|----------|---------|
| Tilemap / Environment | — | ✓ | Bản đồ phó bản |
| DungeonInstance | Script kế thừa `BaseDungeonInstance` | ✓ | Logic spawn enemy, timer, reward |
| SpawnPoint | Transform | ✓ | Vị trí spawn player (entry point) |
| Main Camera | Camera | ✗ | Chỉ cần nếu camera khác overworld |
| Canvas (HUD) | — | ✗ | countdown/status text |

**Lưu ý:** Scene phó bản được load **additive** — không cần NetworkManager, ServerBootstrap, hay bất kỳ singleton nào. Tất cả đã có sẵn từ ServerScene.

### 4.3. Build Settings

Đảm bảo thứ tự scene trong Build Settings:

```
0: Login
1: Register  
2: SelectElement
3: GameScene
4: ServerScene
...
N: DungeonWaveScene    ← thêm dungeon scene vào đây
N+1: DungeonPartyScene
```

---

## 5. Config NPC Phó Bản

### 5.1. Thêm NPC trong DB

```sql
INSERT INTO npc_config 
  (npc_id, npc_name, npc_type, map_id, pos_x, pos_y,
   dialogue_key, description)
VALUES 
  (next_id, 'Thủ Môn Phó Bản', 'dungeon', 0, 50.0, 10.0,
   'npc_dungeon_greet', 'NPC mở menu phó bản');
```

| Cột | Giá trị | Quan trọng |
|-----|---------|------------|
| `npc_type` | **`'dungeon'`** | Bắt buộc — hệ thống dùng giá trị này để mở `DungeonNpcMenuUI` |
| `map_id` | Map overworld chứa NPC | VD: `0` = map chính |
| `pos_x`, `pos_y` | Tọa độ spawn NPC | |
| `dialogue_key` | Key dialogue | `'npc_dungeon_greet'` |

### 5.2. NPC Prefab trong Unity

NPC dungeon dùng chung prefab NPC thường. Hệ thống tự nhận diện qua `npc_type`:

```
Player interact NPC
  → NpcMenuUI kiểm tra npc_type
  → npc_type == "dungeon" → DungeonNpcMenuUI.GetOrCreate().Open(npc)
  → Hiện danh sách tất cả dungeon (GET /api/dungeon/list)
```

Không cần config gì thêm trên prefab — chỉ cần đảm bảo NPC spawn đúng vị trí trên map.

---

## 6. Config Dungeon Instance (Runtime)

### 6.1. Tạo script kế thừa BaseDungeonInstance

```csharp
public class MyDungeonWaveInstance : BaseDungeonInstance
{
    [Header("Wave Config")]
    public DungeonEnemyUnitConfig[] waveEnemies;
    public float timeBetweenWaves = 5f;
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            StartCoroutine(RunWaves());
        }
    }
    
    private IEnumerator RunWaves()
    {
        foreach (var enemy in waveEnemies)
        {
            BroadcastStatus($"Wave tiếp theo trong {timeBetweenWaves}s...");
            yield return new WaitForSeconds(timeBetweenWaves);
            SpawnConfiguredEnemy(enemy, 1f, enemy == waveEnemies[^1]);
        }
        
        // Hoàn thành
        yield return StartCoroutine(GrantRewardsToAll(rewardItems));
        BeginReturnFlow(completed: true, countdownSeconds: 10f, 
                        returnMapId: 0, returnSceneName: "GameScene");
    }
}
```

### 6.2. Gắn vào Scene

1. Tạo GameObject `DungeonInstance` trong scene phó bản
2. Gắn script `MyDungeonWaveInstance`
3. Assign UI references: `countdownText`, `statusText` (TMP_Text)
4. Config enemy spawn trong Inspector

### 6.3. BaseDungeonInstance API

| Method | Mô tả | Chạy trên |
|--------|-------|-----------|
| `SpawnConfiguredEnemy(config, scale, isBoss)` | Spawn enemy theo config | Server |
| `BroadcastStatus(msg)` | Hiện thông báo trên HUD | Server → Client |
| `GrantRewardsToAll(rewards)` | Phát thưởng cho tất cả player | Server |
| `BeginReturnFlow(completed, countdown, returnMapId, returnSceneName)` | Countdown rồi exit dungeon | Server/Client |

---

## 7. Config Boss & Enemy Spawn

### 7.1. DB `dungeon_enemy_spawn`

```sql
INSERT INTO dungeon_enemy_spawn 
  (dungeon_config_id, enemy_id, spawn_x, spawn_y, spawn_count, is_boss)
VALUES 
  (6, 1, 0.0, 0.0, 5, 0),   -- 5 quái thường
  (6, 10, 0.0, 5.0, 1, 1);   -- 1 boss
```

### 7.2. API lấy config

```
GET /api/dungeon/{dungeonId}
→ trả về dungeon config + enemy_spawns[] + player_spawn_points[]

GET /api/dungeon/boss/{bossId}/config  
→ trả về boss stats, skills, spawn config
```

### 7.3. DungeonEnemyUnitConfig (Unity)

Config enemy spawn trên Inspector:

| Field | Ý nghĩa |
|-------|---------|
| `enemyId` | ID enemy trong DB |
| `spawnPosition` | Vị trí spawn (local) |
| `spawnCount` | Số lượng |
| `isBoss` | Có phải boss? |

---

## 8. Config Reward

### 8.1. `reward_json` trong dungeon_config

```json
{
  "items": [
    { "itemTemplateId": 101, "quantity": 3 },
    { "itemTemplateId": 205, "quantity": 1, "upgradeLevel": 2 }
  ],
  "exp": 500,
  "gold": 1000
}
```

### 8.2. API grant reward

Server gọi nội bộ:

```
POST /api/dungeonreward/grant
Header: X-Zone-Api-Key: {zoneApiKey}
Body: {
  "targetPlayerId": 42,
  "items": [
    { "itemTemplateId": 101, "quantity": 3 }
  ]
}
```

---

## 9. Thêm Phó Bản Mới — Checklist

### Bước 1: Database

- [ ] Chọn `map_id` mới (VD: `112`)
- [ ] INSERT `map_config` với `scene_name` đúng
- [ ] INSERT `dungeon_config` với `map_id` trùng, `dungeon_type` đúng (solo/multi)
- [ ] INSERT `dungeon_enemy_spawn` nếu có enemy
- [ ] Kiểm tra `is_active = 1`

```sql
-- 1. Map
INSERT INTO map_config (map_id, map_name, scene_name, spawn_points_json)
VALUES (112, 'DungeonBoss', 'DungeonBossScene', '[]');

-- 2. Dungeon
INSERT INTO dungeon_config 
  (dungeon_name, dungeon_type, map_id, scene_name, max_players, 
   min_level_required, time_limit_seconds, boss_enemy_id, is_active)
VALUES 
  ('Phó Bản Boss', 'multi', 112, 'DungeonBossScene', 4, 
   10, 600, 10, 1);

-- 3. Enemy spawn
INSERT INTO dungeon_enemy_spawn (dungeon_config_id, enemy_id, spawn_x, spawn_y, spawn_count, is_boss)
VALUES (LAST_INSERT_ID(), 10, 0, 5, 1, 1);
```

### Bước 2: Unity Scene

- [ ] Tạo scene `DungeonBossScene.unity` trong `Assets/Scenes/`
- [ ] Thêm vào **Build Settings**
- [ ] Tạo Tilemap / environment
- [ ] Tạo GameObject `DungeonInstance` với script kế thừa `BaseDungeonInstance`
- [ ] Config enemy spawn, UI references

### Bước 3: MapWorldConfig

- [ ] Nếu `loadMapsFromApiOnBoot = true` → **không cần làm gì** (auto từ API)
- [ ] Nếu offline → thêm MapDefinition: mapId=112, InstanceOnly, allowCustomZones=true

### Bước 4: NPC (tùy chọn)

- [ ] Nếu muốn NPC mới → INSERT `npc_config` với `npc_type='dungeon'`
- [ ] NPC dungeon hiện **tất cả** phó bản active, không cần gắn NPC riêng cho từng phó bản

### Bước 5: Test

- [ ] Restart API server (`dotnet run`)
- [ ] Kiểm tra `GET /api/dungeon/list` có phó bản mới
- [ ] Kiểm tra `GET /api/map/runtime-bootstrap` có map mới với `zone_topology=1`
- [ ] Vào game → tương tác NPC dungeon → thấy phó bản mới trong danh sách
- [ ] Solo: enter → transfer mượt, không disconnect → hoàn thành → về overworld
- [ ] Multi: tạo party → leader enter → tất cả members được transfer vào cùng room

---

## 10. Troubleshooting

| Lỗi | Nguyên nhân | Cách fix |
|-----|------------|----------|
| `DUNGEON_MAP_INVALID` | map_id không có trong MapWorldConfig hoặc topology ≠ InstanceOnly | Kiểm tra DB map_config + runtime-bootstrap API |
| `DUNGEON_ROOM_CREATE_FAILED` | ZoneRoomRegistry không tạo được room | Kiểm tra `allowCustomZones = true` cho map đó |
| `NO_RETURN_ROOM` | Không tìm được zone overworld khi exit | Kiểm tra `fallbackMapId` trong MapWorldConfig |
| `MAP_NOT_FOUND` | returnMapId không tồn tại | Kiểm tra map_config có map đó |
| Phó bản không hiện trong danh sách | `is_active = 0` hoặc chưa INSERT | `SELECT * FROM dungeon_config WHERE is_active=1` |
| Scene load lỗi | scene_name trong DB ≠ tên scene Unity, hoặc chưa thêm Build Settings | So sánh tên chính xác, kiểm tra Build Settings |
| Party transfer thiếu member | userId không match trên server | Kiểm tra ZonePlayerSessionManager có player đó |
| `map_config.scene_name` sai | DB bị swap scene_name | API tự repair khi khởi động (xem log `[DBRepair]`) |
