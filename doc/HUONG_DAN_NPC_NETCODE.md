# HƯỚNG DẪN HỆ THỐNG NPC, SPAWN QUÁI & TỐI ƯU NETCODE

> **Dự án:** Game Unity Netcode for GameObjects + ASP.NET Core API  
> **Phạm vi:** NPC system, mob spawn, map zone, bảo mật & hiệu năng

---

## 1. KIẾN TRÚC TỔNG QUAN

```
┌──────────────────────────────────────────────────────────────┐
│                         CLIENT (Unity)                       │
│  NpcInteractTrigger → NpcApiClient → UI (NpcDialogueUI /     │
│  ShopUI) → Server Validation → Apply Result                  │
└────────────────────────┬─────────────────────────────────────┘
                         │  HTTP/API
┌────────────────────────▼─────────────────────────────────────┐
│                     GAMESERVERAPI (.NET)                     │
│  NpcController  ─── GameDbContext ─── MariaDB                │
│  GET  /api/npc/list          (danh sách NPC trên map)        │
│  POST /api/npc/interact      (bắt đầu hội thoại)            │
│  POST /api/npc/dialogue/next (node kế tiếp)                  │
│  GET  /api/npc/shop          (danh sách item bán)            │
│  POST /api/npc/shop/buy      (mua item — server auth)        │
└──────────────────────────────────────────────────────────────┘
```

### Nguyên tắc bảo mật
- **Server-authoritative**: Mọi giao dịch (mua bán, quest, trigger event) đều được xác thực server-side.  
- Client chỉ gửi Intent (muốn mua cái gì, muốn nói chuyện NPC nào); server kiểm tra level, tiền, tồn kho.  
- Không tin tưởng giá tiền hay quantity từ client.

---

## 2. CƠ SỞ DỮ LIỆU — NPC TABLES

### 2.1 `npc_config` — Master data NPC
| Cột | Kiểu | Mô tả |
|---|---|---|
| `npc_id` | INT PK AUTO | ID duy nhất |
| `npc_name` | VARCHAR(100) | Tên hiện trên màn hình |
| `npc_type` | VARCHAR(20) | `shop` / `quest` / `blacksmith` / `exchange` / `event` |
| `map_id` | INT | Map chứa NPC |
| `pos_x`, `pos_y` | FLOAT | Vị trí spawn trong scene |
| `dialogue_key` | VARCHAR(50) | Key node đầu tiên trong `npc_dialogue` |
| `icon_id` | VARCHAR(50) | Icon thumbnail |
| `is_active` | BOOL | Bật/tắt NPC |

### 2.2 `npc_dialogue` — Cây hội thoại
| Cột | Kiểu | Mô tả |
|---|---|---|
| `id` | INT PK | |
| `npc_id` | INT FK | |
| `dialogue_key` | VARCHAR(50) | Định danh node (UNIQUE per npc) |
| `text_vi` | VARCHAR(1000) | Nội dung thoại tiếng Việt |
| `next_key` | VARCHAR(50) NULL | Tiến đến node nào tiếp theo |
| `action_type` | VARCHAR(20) | `none` / `open_shop` / `give_quest` / `teleport` |

### 2.3 `npc_shop_item` — Inventory shop
| Cột | Kiểu | Mô tả |
|---|---|---|
| `id` | INT PK | |
| `npc_id` | INT FK | |
| `item_template_id` | INT FK | Link sang `item_template` |
| `price_silver` | INT | Giá bạc |
| `price_gold` | INT | Giá vàng (nếu 0 → dùng bạc) |
| `stock` | INT | Tồn kho (-1 = vô hạn) |
| `required_level` | INT | Level tối thiểu để mua |

---

## 3. API ENDPOINTS

### 3.1 Lấy danh sách NPC theo map
```http
GET /api/npc/list?mapId=0
```
**Response:**
```json
[
  { "npcId": 1, "npcName": "Lão Trương", "npcType": "shop",
    "posX": 2.0, "posY": -1.0, "iconId": "npc_merchant_1", "dialogueKey": "greet" }
]
```
→ Unity dùng để spawn NPC GameObject tại đúng vị trí và gắn đúng loại.

### 3.2 Tương tác NPC
```http
POST /api/npc/interact
{ "playerId": 1, "npcId": 1 }
```
**Response:**
```json
{
  "npcId": 1, "npcName": "Lão Trương", "npcType": "shop",
  "dialogue": {
    "key": "greet",
    "text": "Chào anh hùng! ...",
    "nextKey": "shop_offer",
    "actionType": "none"
  }
}
```

### 3.3 Node hội thoại kế tiếp
```http
POST /api/npc/dialogue/next
{ "npcId": 1, "dialogueKey": "shop_offer" }
```
→ `actionType: "open_shop"` → client mở ShopUI.

### 3.4 Xem shop
```http
GET /api/npc/shop?npcId=1&playerId=1
```

### 3.5 Mua item
```http
POST /api/npc/shop/buy
{ "playerId": 1, "npcId": 1, "shopItemId": 2, "quantity": 1 }
```
Server kiểm tra: level, tiền, tồn kho → trừ tiền → thêm item vào inventory.

---

## 4. UNITY — TÍCH HỢP NPC CLIENT

### 4.1 NpcManager.cs (Singleton)
```csharp
public class NpcManager : MonoBehaviour
{
    public static NpcManager Instance { get; private set; }

    // Gọi khi load map, spawn NPC từ data
    public async Task LoadNpcsForMap(int mapId)
    {
        var npcs = await ApiClient.Instance.GetNpcList(mapId);
        foreach (var data in npcs)
        {
            var prefab = Resources.Load<GameObject>($"Prefabs/NPC/{data.npc_type}");
            var go = Instantiate(prefab, new Vector3(data.pos_x, data.pos_y, 0), Quaternion.identity);
            go.GetComponent<NpcBehaviour>().Init(data);
        }
    }
}
```

### 4.2 NpcBehaviour.cs — Gắn vào prefab NPC
```csharp
public class NpcBehaviour : MonoBehaviour
{
    [SerializeField] NpcData data;   // Được set bởi NpcManager.Init()
    
    // Collider 2D trigger để phát hiện player đến gần
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && other.GetComponent<NetworkObject>().IsOwner)
        {
            NpcUIManager.Instance.ShowInteractPrompt(data.npcId);
        }
    }

    public void OnInteract()
    {
        _ = NpcUIManager.Instance.StartDialogue(data.npcId);
    }
}
```

### 4.3 NpcUIManager.cs — Hiển thị hội thoại
```csharp
public async Task StartDialogue(int npcId)
{
    var resp = await ApiClient.Instance.InteractNpc(LocalPlayer.PlayerId, npcId);
    ShowDialogueNode(resp.dialogue);
}

private void ShowDialogueNode(DialogueNodeData node)
{
    dialoguePanel.SetActive(true);
    dialogueText.text = node.text;
    // Nếu có nút "Tiếp theo"
    nextButton.onClick.RemoveAllListeners();
    nextButton.onClick.AddListener(async () =>
    {
        if (node.nextKey != null)
        {
            var next = await ApiClient.Instance.NextDialogue(currentNpcId, node.nextKey);
            if (next.actionType == "open_shop")
                OpenShopUI(currentNpcId);
            else
                ShowDialogueNode(next);
        }
        else
            CloseDialogue();
    });
}
```

### 4.4 APIClient.cs — thêm NPC methods
```csharp
// Thêm vào APIClient.cs
public async Task<List<NpcData>> GetNpcList(int mapId)
{
    string json = await Get($"/api/npc/list?mapId={mapId}");
    return JsonUtility.FromJson<NpcListWrapper>("{\"list\":" + json + "}").list;
}

public async Task<NpcInteractResponse> InteractNpc(int playerId, int npcId)
{
    string body = $"{{\"playerId\":{playerId},\"npcId\":{npcId}}}";
    string json = await Post("/api/npc/interact", body);
    return JsonUtility.FromJson<NpcInteractResponse>(json);
}

public async Task<DialogueNodeData> NextDialogue(int npcId, string dialogueKey)
{
    string body = $"{{\"npcId\":{npcId},\"dialogueKey\":\"{dialogueKey}\"}}";
    string json = await Post("/api/npc/dialogue/next", body);
    return JsonUtility.FromJson<DialogueNodeData>(json);
}

public async Task<ShopResponse> GetShop(int npcId, int playerId)
{
    string json = await Get($"/api/npc/shop?npcId={npcId}&playerId={playerId}");
    return JsonUtility.FromJson<ShopResponse>(json);
}

public async Task<BuyResponse> BuyItem(int playerId, int npcId, int shopItemId, int quantity = 1)
{
    string body = $"{{\"playerId\":{playerId},\"npcId\":{npcId},\"shopItemId\":{shopItemId},\"quantity\":{quantity}}}";
    string json = await Post("/api/npc/shop/buy", body);
    return JsonUtility.FromJson<BuyResponse>(json);
}
```

---

## 5. HỆ THỐNG SPAWN QUÁI TỐI ƯU (NETCODE)

### 5.1 Nguyên tắc Server-Authoritative Spawn

```
Server (Host) sở hữu:
├── SpawnTimerSystem    – đếm giờ respawn, không phụ thuộc client
├── MobRegistry         – dict<networkObjectId, MobState>
└── ZoneOccupancyCheck  – chỉ spawn nếu có player trong zone

Client nhận:
├── SpawnRpc (OnSpawnMob)  – tạo mob prefab tại đúng vị trí
└── DespawnRpc             – xóa mob khi server despawn
```

**Thiết kế quan trọng**: Client KHÔNG được phép quyết định spawn hay không.

### 5.2 ServerMobSpawnManager.cs
```csharp
/// Chỉ chạy trên Server (IsServer = true)
public class ServerMobSpawnManager : NetworkBehaviour
{
    [SerializeField] private SpawnZone[] zones;

    private Dictionary<int, float> _respawnTimers = new();

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        InvokeRepeating(nameof(TickSpawns), 1f, 1f);
    }

    private void TickSpawns()
    {
        foreach (var zone in zones)
        {
            // Chỉ spawn khi có player trong zone
            if (!IsAnyPlayerInZone(zone)) continue;

            if (!_respawnTimers.TryGetValue(zone.ZoneId, out float timer))
                timer = 0f;

            if (timer > 0f)
            {
                _respawnTimers[zone.ZoneId] = timer - 1f;
                continue;
            }

            if (zone.CurrentCount < zone.MaxCount)
                SpawnMobInZone(zone);
        }
    }

    private void SpawnMobInZone(SpawnZone zone)
    {
        var spawnPos = zone.GetRandomSpawnPoint();
        var prefab   = NetworkManager.Singleton.NetworkConfig.Prefabs.Prefabs
                           .FirstOrDefault(p => p.Prefab.name == zone.MobPrefabName)?.Prefab;
        if (prefab == null) return;

        var mob = Instantiate(prefab, spawnPos, Quaternion.identity);
        mob.GetComponent<NetworkObject>().Spawn();
        zone.CurrentCount++;
    }

    private bool IsAnyPlayerInZone(SpawnZone zone)
    {
        return NetworkManager.Singleton.ConnectedClientsList.Any(client =>
        {
            var playerObj = client.PlayerObject;
            if (playerObj == null) return false;
            return zone.Bounds.Contains(playerObj.transform.position);
        });
    }

    // Gọi khi mob chết
    public void OnMobDied(int zoneId, float respawnDelay)
    {
        if (!IsServer) return;
        _respawnTimers[zoneId] = respawnDelay;
        // Tìm zone để giảm current count
        var zone = System.Array.Find(zones, z => z.ZoneId == zoneId);
        if (zone != null) zone.CurrentCount--;
    }
}
```

### 5.3 SpawnZone ScriptableObject
```csharp
[CreateAssetMenu(menuName = "Game/SpawnZone")]
public class SpawnZone : ScriptableObject
{
    public int     ZoneId;
    public string  MobPrefabName;
    public Bounds  Bounds;              // Vùng quan tâm
    public int     MaxCount   = 5;
    public float   RespawnDelay = 30f;
    [HideInInspector] public int CurrentCount = 0;

    public Vector3 GetRandomSpawnPoint()
    {
        return new Vector3(
            Random.Range(Bounds.min.x, Bounds.max.x),
            Random.Range(Bounds.min.y, Bounds.max.y),
            0f);
    }
}
```

### 5.4 Tải dữ liệu spawn từ API
```csharp
// Gọi khi host bắt đầu game, lấy danh sách spawn points từ server
public async Task LoadSpawnDataFromApi(int mapId)
{
    if (!IsServer) return;
    var spawns = await ApiClient.Instance.GetEnemySpawns(mapId);
    foreach (var s in spawns)
    {
        var zone = ScriptableObject.CreateInstance<SpawnZone>();
        zone.ZoneId        = s.spawn_id;
        zone.MobPrefabName = $"Enemy_{s.enemy_type_id}";
        zone.Bounds        = new Bounds(new Vector3(s.spawn_x, s.spawn_y, 0), Vector3.one * 5f);
        zone.MaxCount      = s.max_spawn_count;
        zone.RespawnDelay  = s.respawn_time;
        AddZone(zone);
    }
}
```

---

## 6. TỐI ƯU MAP & NETWORK

### 6.1 Zone-Based Interest Management
Chỉ sync dữ liệu của object trong zone mà client đang ở.

```csharp
// Gắn vào mỗi NetworkObject (mob, item drop, NPC động)
public class ZoneNetworkRelevancy : NetworkBehaviour
{
    [SerializeField] private float relevancyRadius = 15f;

    // Netcode 1.x: Override CheckObjectVisibility
    public override bool CheckObjectVisibility(ulong clientId)
    {
        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj == null) return false;
        float dist = Vector3.Distance(transform.position, playerObj.transform.position);
        return dist <= relevancyRadius;
    }
}
```

### 6.2 Network Tick Rate
```csharp
// NetworkManager → NetworkConfig
// TickRate: 20  (20 ticks/giây cho game action RPG)
// ClientConnectionBufferTimeout: 10s
```

Giá trị hợp lý:
| Thành phần | Tick Rate | Ghi chú |
|---|---|---|
| Player movement | 20 Hz | Interpolation client-side |
| Mob AI + health | 10 Hz | Chỉ sync khi có thay đổi |
| NPC state | On demand | HTTP API, không sync liên tục |
| Projectiles | 20 Hz | Server-spawned, client predict |

### 6.3 NetworkVariable cho thông tin quan trọng
```csharp
// Trong MobController.cs
public NetworkVariable<int>   CurrentHp  = new(100, NetworkVariableReadPermission.Everyone,
                                                   NetworkVariableWritePermission.Server);
public NetworkVariable<float> PosX       = new(0f,  NetworkVariableReadPermission.Everyone,
                                                   NetworkVariableWritePermission.Server);
```

### 6.4 Tối ưu Inventory & Skills (không dùng NetworkVariable)
Inventory và skills KHÔNG cần sync realtime → chỉ cần request API khi cần:
- Mua item → `POST /api/npc/shop/buy` → server cập nhật DB → client reload.
- Skill level up → `POST /api/player/skill/learn` → server cập nhật → client reload skills.

---

## 7. BẢO MẬT

### 7.1 Những gì phải validate server-side
| Hành động | Validate gì? |
|---|---|
| Mua item NPC | Đủ tiền, đủ level, tồn kho > 0 |
| Fuse hybrid gene | Tier 5 cả 2 hệ, có item, có vàng |
| Upgrade trang bị | Đúng level, đủ vật liệu |
| Skill damage | Tính sát thương on-server, không nhận từ client |
| Player position | Validate không teleport bất thường |

### 7.2 Rate Limiting (cần thêm Middleware)
```csharp
// Program.cs — thêm rate limiting
builder.Services.AddRateLimiter(opt =>
{
    opt.AddFixedWindowLimiter("api", policy =>
    {
        policy.PermitLimit         = 30;
        policy.Window              = TimeSpan.FromSeconds(10);
        policy.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        policy.QueueLimit          = 5;
    });
});
// ...
app.UseRateLimiter();
// Controller: [EnableRateLimiting("api")]
```

### 7.3 Input Validation
```csharp
// Luôn kiểm tra range cho quantity, itemId, npcId
if (quantity <= 0 || quantity > 999)
    return BadRequest("Số lượng không hợp lệ.");
if (npcId <= 0)
    return BadRequest("NpcId không hợp lệ.");
```

---

## 8. CẤU HÌNH UNITY — CHECKLIST

### 8.1 NPC Prefab Setup
- [ ] Prefab name: `NPC_shop`, `NPC_quest`, `NPC_blacksmith` — khớp với `npc_type` trong DB
- [ ] Gắn `NpcBehaviour.cs` với `CircleCollider2D` (Is Trigger = TRUE, Radius = 2)
- [ ] Gắn `SpriteRenderer` với sprite NPC phù hợp
- [ ] Gắn `NpcNameTag` (TextMeshPro) hiển thị tên NPC

### 8.2 NpcUIManager
- [ ] Reference `NpcDialogueUI` Canvas (đặt ở World Space hoặc Screen Space - Camera)
- [ ] `dialogueText` — `TMP_Text`
- [ ] `nextButton` — `Button`
- [ ] `closeButton` — `Button`

### 8.3 ShopUI
- [ ] `itemListContainer` — Scroll View Content
- [ ] `itemSlotPrefab` — Prefab có icon, name, price, buy Button
- [ ] Gọi `ApiClient.BuyItem(playerId, npcId, shopItemId, qty)` khi nhấn Buy
- [ ] Refresh PlayerGold/Silver label sau khi mua thành công

### 8.4 Map Zones
- [ ] Tạo `SpawnZone` ScriptableObjects cho từng vùng có mob
- [ ] Gắn vào `ServerMobSpawnManager` component trên Network Manager
- [ ] Đảm bảo `ServerMobSpawnManager` chỉ tồn tại trên Host

---

## 9. THÊM NPC MỚI — STEP BY STEP

1. **DB**: Thêm row vào `npc_config`:
   ```sql
   INSERT INTO npc_config (npc_name, npc_type, map_id, pos_x, pos_y, dialogue_key, icon_id)
   VALUES ('Phù Thủy Hắc Ám', 'exchange', 0, 3.5, -2.0, 'greet', 'npc_wizard_1');
   ```

2. **DB**: Thêm dialogue:
   ```sql
   INSERT INTO npc_dialogue (npc_id, dialogue_key, text_vi, next_key, action_type)
   VALUES (4, 'greet', 'Ta có thể chuyển đổi gene cho ngươi...', NULL, 'open_shop');
   ```

3. **Unity**: Tạo prefab `NPC_exchange` dựa trên template NPC_shop.

4. **Unity**: Không cần code thêm — `NpcManager.LoadNpcsForMap()` tự spawn.

---

## 10. MIGRATION SQL

Chạy lệnh sau để tạo bảng NPC vào DB hiện có:

```sql
-- File: migration_npc_system.sql
CREATE TABLE IF NOT EXISTS `npc_config` (
  `npc_id`       int(11)      NOT NULL AUTO_INCREMENT,
  `npc_name`     varchar(100) NOT NULL,
  `npc_type`     varchar(20)  NOT NULL DEFAULT 'shop',
  `map_id`       int(11)      NOT NULL DEFAULT 0,
  `pos_x`        float        NOT NULL DEFAULT 0,
  `pos_y`        float        NOT NULL DEFAULT 0,
  `dialogue_key` varchar(50)  DEFAULT NULL,
  `icon_id`      varchar(50)  DEFAULT NULL,
  `is_active`    tinyint(1)   NOT NULL DEFAULT 1,
  PRIMARY KEY (`npc_id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `npc_shop_item` (
  `id`               int(11) NOT NULL AUTO_INCREMENT,
  `npc_id`           int(11) NOT NULL,
  `item_template_id` int(11) NOT NULL,
  `price_silver`     int(11) NOT NULL DEFAULT 0,
  `price_gold`       int(11) NOT NULL DEFAULT 0,
  `stock`            int(11) NOT NULL DEFAULT -1,
  `required_level`   int(11) NOT NULL DEFAULT 1,
  PRIMARY KEY (`id`),
  CONSTRAINT `fk_npc_shop_npc` FOREIGN KEY (`npc_id`) REFERENCES `npc_config` (`npc_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS `npc_dialogue` (
  `id`           int(11)       NOT NULL AUTO_INCREMENT,
  `npc_id`       int(11)       NOT NULL,
  `dialogue_key` varchar(50)   NOT NULL,
  `text_vi`      varchar(1000) NOT NULL,
  `next_key`     varchar(50)   DEFAULT NULL,
  `action_type`  varchar(20)   NOT NULL DEFAULT 'none',
  PRIMARY KEY (`id`),
  UNIQUE KEY `uq_npc_dialogue_key` (`npc_id`, `dialogue_key`),
  CONSTRAINT `fk_npc_dialogue_npc` FOREIGN KEY (`npc_id`) REFERENCES `npc_config` (`npc_id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```
