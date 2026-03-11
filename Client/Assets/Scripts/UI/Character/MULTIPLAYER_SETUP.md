# Hướng Dẫn Config Multiplayer: Skill PvP, Enemy Sync, Chọn Map

> Framework: **Unity Netcode for GameObjects**  
> Kiến trúc: **Server-Authoritative** — Server mới được phép thay đổi HP, Client chỉ đọc

---

## Mục Lục

1. [Skill Gây Damage Cho Player Khác (PvP) — Cả 2 Thấy Máu Giảm](#1-skill-gây-damage-cho-player-khác-pvp)
2. [Config Enemy Cho Toàn Bộ Client Nhìn Thấy](#2-config-enemy-cho-toàn-bộ-client-nhìn-thấy)
3. [Config Button Chọn Map](#3-config-button-chọn-map)
4. [Checklist Tổng](#4-checklist-tổng)

---

## 1. Skill Gây Damage Cho Player Khác (PvP)

### Luồng hoạt động

```
Client A bắn skill
  → Projectile va chạm Player B
  → Gọi TakeDamageServerRpc(damage)
  → Server trừ networkCurrentHealth.Value
  → NetworkVariable tự broadcast sang TẤT CẢ clients
  → OnHealthChanged event kích hoạt trên MỌI client
  → UI HP của Player B cập nhật cho cả A lẫn B
```

---

### Bước 1.1 — Sửa FireballDamage để damage Player khác

Mở `Assets/Scripts/Player/Skills/FireballDamage.cs`.

Hiện tại script chỉ xử lý tag `"Enemy"`. Thêm xử lý tag `"Player"` vào `OnTriggerEnter2D`:

```csharp
private void OnTriggerEnter2D(Collider2D collision)
{
    if (hasHit) return;

    // ✅ ĐÃ CÓ: Damage Enemy
    if (collision.CompareTag("Enemy"))
    {
        // ... code hiện tại giữ nguyên
    }

    // 🆕 THÊM: Damage Player khác (PvP)
    else if (collision.CompareTag("Player"))
    {
        NetworkPlayerHealth targetHealth = collision.GetComponent<NetworkPlayerHealth>();
        if (targetHealth == null)
            targetHealth = collision.GetComponentInParent<NetworkPlayerHealth>();

        if (targetHealth != null)
        {
            // Không damage chính mình (owner của projectile)
            // Lấy NetworkObject của người bắn qua ProjectileMovement
            NetworkObject shooterNetObj = GetComponent<ProjectileMovement>()?.GetShooterNetworkObject();
            if (shooterNetObj != null && targetHealth.NetworkObjectId == shooterNetObj.NetworkObjectId)
                return; // Bỏ qua nếu đây là chính người bắn

            // Gọi ServerRpc để server trừ HP — tự động sync cho tất cả clients
            targetHealth.TakeDamageServerRpc(damage);
            hasHit = true;

            if (destroyOnHit) Destroy(gameObject);
        }
    }
}
```

> **Lưu ý:** Nếu muốn bỏ qua damage bản thân cần truyền `OwnerClientId` qua `ProjectileMovement`. Xem Bước 1.2.

---

### Bước 1.2 — Truyền thông tin người bắn vào Projectile

Mở `Assets/Scripts/Player/Skills/ProjectileMovement.cs`. Thêm field để lưu NetworkObject của người bắn:

```csharp
[HideInInspector] public ulong shooterNetworkObjectId; // NetworkObjectId của người bắn

public NetworkObject GetShooterNetworkObject()
{
    // Tìm NetworkObject theo ID (nếu cần tự định danh)
    // Có thể dùng NetworkManager.Singleton.SpawnManager.SpawnedObjects
    if (NetworkManager.Singleton != null &&
        NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(shooterNetworkObjectId, out NetworkObject netObj))
    {
        return netObj;
    }
    return null;
}
```

Trong `PlayerSkillManager.cs`, khi spawn projectile, gán ID:

```csharp
// Sau khi Instantiate projectile
var pm = projectileObj.GetComponent<ProjectileMovement>();
if (pm != null)
    pm.shooterNetworkObjectId = NetworkObjectId; // ID của player đang bắn
```

---

### Bước 1.3 — Đảm bảo Tag "Player" tồn tại

1. Unity → **Edit → Project Settings → Tags and Layers**
2. Thêm tag `Player` (nếu chưa có)
3. Chọn Player Prefab trong **Prefabs/** → Inspector → **Tag** → chọn `Player`

---

### Bước 1.4 — Kiểm tra NetworkPlayerHealth có `TakeDamageServerRpc` public

Script `NetworkPlayerHealth.cs` đã có:
```csharp
[ServerRpc(RequireOwnership = false)]
public void TakeDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
```

`RequireOwnership = false` cho phép **bất kỳ client nào** gọi RPC này, không cần phải là owner của object đó. Đây là điều bắt buộc cho PvP.

---

### Bước 1.5 — HealthBar của Player B tự động cập nhật

`HealthBar.cs` đã subscribe `OnHealthChanged`:
```csharp
networkPlayerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
```

`NetworkVariable` khi thay đổi trên server → trigger `OnHealthValueChanged` → gọi `OnHealthChanged.Invoke()` **trên tất cả clients** → `HealthBar` của tất cả clients cập nhật ngay lập tức.

**Không cần thêm code gì thêm cho HP bar.**

---

### Bước 1.6 — Test PvP Damage

1. Dùng **ParrelSync** (có sẵn trong project) để mở 2 instance Unity
2. Instance 1: Host/Server
3. Instance 2: Client
4. Bắn skill vào Player của instance kia
5. Verify: HP bar trên **cả 2 màn hình** phải giảm cùng lúc

---

## 2. Config Enemy Cho Toàn Bộ Client Nhìn Thấy

### Nguyên lý

Enemy được **server spawn** qua `NetworkObject.Spawn()` → Unity Netcode tự động replicate sang tất cả clients. Vị trí, HP, animation đều sync qua `NetworkTransform` + `NetworkVariable`.

---

### Bước 2.1 — Cấu Trúc Enemy Prefab (Bắt Buộc)

Mỗi Enemy Prefab dùng cho multiplayer phải có đủ các component sau:

| Component | Bắt buộc | Mục đích |
|-----------|----------|----------|
| `NetworkObject` | ✅ | Định danh mạng, cho phép replicate |
| `NetworkTransform` | ✅ | Sync vị trí cho tất cả clients |
| `NetworkEnemyHealth` | ✅ | Sync HP qua NetworkVariable |
| `NetworkEnemyController` | ✅ | Sync hướng đi, velocity |
| `EnemyAI` | ✅ | Logic AI (chỉ chạy trên server) |
| `EnemyItemDrop` | ✅ | Drop item khi chết (server) |
| `EnemyHealthBarSpawner` | ✅ | Tự spawn health bar trên mỗi client |
| `Rigidbody2D` | ✅ | Vật lý di chuyển |
| `Collider2D` (với tag Enemy) | ✅ | Va chạm với skill |
| `Animator` | Tùy chọn | Animation chạy/chết |

**Cách nhanh nhất:** Duplicate Enemy Prefab hiện có trong `Assets/Prefabs/` và kiểm tra đủ component.

---

### Bước 2.2 — Setup NetworkObject trên Enemy Prefab

1. Chọn Enemy Prefab trong Project
2. Inspector → Add Component → tìm `NetworkObject`
3. Tick: `Always Replicate` = **ON**
4. `Active Scene Object` = **OFF** (vì sẽ spawn runtime)

---

### Bước 2.3 — Setup NetworkTransform

NetworkTransform đã được `NetworkEnemyController` tự thêm nếu chưa có:
```csharp
// Trong Awake() của NetworkEnemyController.cs
if (GetComponent<NetworkTransform>() == null)
{
    var networkTransform = gameObject.AddComponent<NetworkTransform>();
    networkTransform.SyncPositionX = true;
    networkTransform.SyncPositionY = true;
    networkTransform.SyncPositionZ = false;
}
```

Nhưng nên thêm thủ công để tránh xung đột:
1. Inspector → Add Component → `NetworkTransform`
2. Cấu hình:
   - `Sync Position X` = ✅
   - `Sync Position Y` = ✅
   - `Sync Position Z` = ❌ (2D game)
   - `Sync Rotation` = ❌ (2D game dùng flip thay vì rotate)
   - `Interpolate` = ✅ (chuyển động mượt mà)

---

### Bước 2.4 — Config NetworkEnemyHealth (HP sync)

1. Inspector → Add Component → `NetworkEnemyHealth`
2. Set `Max Health` theo loại enemy:

| Loại enemy | Max Health gợi ý |
|------------|-----------------|
| Slime / Tiny | 10 |
| Goblin / Normal | 30 |
| Orc / Elite | 80 |
| Boss | 300+ |

**NetworkVariable đã được set sẵn:**
```csharp
// Trong NetworkEnemyHealth.cs — Đã có sẵn, không cần thêm
private NetworkVariable<int> networkCurrentHealth = new NetworkVariable<int>(
    10,
    NetworkVariableReadPermission.Everyone,   // ← Tất cả clients đọc được
    NetworkVariableWritePermission.Server     // ← Chỉ server ghi
);
```

---

### Bước 2.5 — Config EnemyHealthBarSpawner (HP bar hiển thị mỗi client)

1. Inspector → Add Component → `EnemyHealthBarSpawner`
2. Slot `Health Bar Prefab` → kéo prefab từ `Assets/Prefabs/` có chứa `EnemyHealthBar` component

**Tạo Health Bar Prefab (nếu chưa có):**
```
EnemyHealthBarCanvas          [Canvas — World Space, Render Mode = World Space]
└─ HealthBarPanel             [RectTransform, width=1, height=0.15]
   ├─ BG                      [Image, màu xám #777]
   └─ Fill                    [Image, màu đỏ #E53935]
      ← Slider (full width)   [Slider, interactable = OFF]
```

Gắn `EnemyHealthBar.cs` lên `EnemyHealthBarCanvas`. Script sẽ tự tìm `NetworkEnemyHealth` trên parent.

---

### Bước 2.6 — Đăng Ký Prefab vào NetworkManager

Enemy Prefab phải được đăng ký để NetworkManager biết cách replicate:

**Cách 1 — Qua DefaultNetworkPrefabs (khuyến nghị):**
1. Project → tìm `DefaultNetworkPrefabs.asset` (ở root Assets/)
2. Inspector → thêm Enemy Prefab vào list

**Cách 2 — Qua NetworkManager Inspector:**
1. Hierarchy → `NetworkManager` object
2. Inspector → `NetworkPrefabs` list → dấu `+` → kéo Enemy Prefab vào

---

### Bước 2.7 — Config NetworkEnemySpawner (spawn từ API)

Trên GameObject chứa `NetworkEnemySpawner` trong scene:

| Slot Inspector | Giá trị |
|----------------|---------|
| `Enemy Prefab Manager` | Kéo GameObject có `EnemyPrefabManager` |
| `Api Base URL` | `http://localhost:5000/api` (hoặc URL server thực) |
| `Map Id` | 0 = tự lấy từ `MapManager` |
| `Spawn On Server Only` | ✅ ON |

---

### Bước 2.8 — Config EnemyPrefabManager

1. Tạo hoặc tìm GameObject `EnemyPrefabManager` trong scene
2. Add Component → `EnemyPrefabManager`
3. Thêm từng loại enemy vào list:

| Enemy Id | Enemy Prefab | Enemy Name |
|----------|-------------|------------|
| 1 | SlimePrefab | Slime |
| 2 | GoblinPrefab | Goblin |
| 3 | OrcPrefab | Orc |

`Enemy Id` phải khớp với `enemy_id` trong database/API.

---

### Bước 2.9 — EnemyAI chỉ chạy trên Server

`EnemyAI.cs` đã có guard:
```csharp
// Chỉ server mới di chuyển enemy
if (!IsServer) return;
```

Client chỉ **nhận** vị trí qua `NetworkTransform`, không tự tính toán AI. Điều này đảm bảo mọi client nhìn thấy enemy ở đúng vị trí.

---

### Bước 2.10 — Damage Enemy từ Skill (đã đúng)

`FireballDamage.cs` đã gọi `TakeDamageServerRpc`:
```csharp
networkEnemyHealth.TakeDamageServerRpc(damage);
```

`RequireOwnership = false` → Bất kỳ client nào bắn đều được server nhận. Server trừ HP → sync cho tất cả. Tất cả clients nhìn thấy HP enemy giảm.

---

## 3. Config Button Chọn Map

### Tổng Quan

```
MainMenu Scene
└─ MapSelectionPanel
   ├─ BtnMap1   → Load GameScene với mapId = 1
   ├─ BtnMap2   → Load GameScene với mapId = 2
   └─ BtnMap3   → Load GameScene với mapId = 3
```

---

### Bước 3.1 — Tạo MapSelectionPanel trong Scene

Trong Hierarchy của **Main Menu Scene**:

```
MainMenuCanvas
├─ (Các panel hiện có)
└─ MapSelectionPanel        ← Tạo mới: UI → Panel
   ├─ TxtTitle              [TMP_Text] "Chọn Map"
   ├─ BtnMap1               [Button] "Rừng Xanh"
   ├─ BtnMap2               [Button] "Sa Mạc"
   ├─ BtnMap3               [Button] "Đầm Lầy"
   └─ BtnClose              [Button] "×"
```

---

### Bước 3.2 — Tạo Script MapSelectionController

Tạo file `Assets/Scripts/UI/Menu/MapSelectionController.cs`:

```csharp
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller cho panel chọn map trước khi vào game
/// </summary>
public class MapSelectionController : MonoBehaviour
{
    [Header("Map Buttons")]
    [SerializeField] private Button btnMap1;
    [SerializeField] private Button btnMap2;
    [SerializeField] private Button btnMap3;
    [SerializeField] private Button btnClose;

    [Header("Panel")]
    [SerializeField] private GameObject mapSelectionPanel;

    [Header("Scene Names")]
    [SerializeField] private string gameSceneName = "GameScene";

    private void Awake()
    {
        if (btnMap1 != null) btnMap1.onClick.AddListener(() => SelectMap(1));
        if (btnMap2 != null) btnMap2.onClick.AddListener(() => SelectMap(2));
        if (btnMap3 != null) btnMap3.onClick.AddListener(() => SelectMap(3));
        if (btnClose != null) btnClose.onClick.AddListener(Hide);
    }

    /// <summary>
    /// Mở panel chọn map — gọi từ button "Vào Game" ở MainMenu
    /// </summary>
    public void Show()
    {
        if (mapSelectionPanel != null)
            mapSelectionPanel.SetActive(true);
    }

    public void Hide()
    {
        if (mapSelectionPanel != null)
            mapSelectionPanel.SetActive(false);
    }

    /// <summary>
    /// Lưu mapId và load scene game
    /// </summary>
    private void SelectMap(int mapId)
    {
        // Lưu mapId để MapManager đọc khi scene load
        PlayerPrefs.SetInt("SelectedMapId", mapId);
        PlayerPrefs.Save();

        Debug.Log($"[MapSelectionController] Selected map ID: {mapId}");

        // Nếu đang là Host/Server: dùng NetworkManager để load scene
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.SceneManager.LoadScene(
                    gameSceneName, LoadSceneMode.Single);
            }
            else
            {
                // Client không tự load scene — Server sẽ push scene sang
                Debug.LogWarning("[MapSelectionController] Client không thể tự load scene. Host sẽ load.");
            }
        }
        else
        {
            // Standalone / Offline
            SceneManager.LoadScene(gameSceneName);
        }
    }
}
```

---

### Bước 3.3 — Gắn Script và Kéo Slot

1. Chọn `MapSelectionPanel` → Add Component → `MapSelectionController`
2. Kéo các slot:

| Slot Inspector | Kéo vào |
|----------------|---------|
| `Btn Map 1` | Button "Rừng Xanh" |
| `Btn Map 2` | Button "Sa Mạc" |
| `Btn Map 3` | Button "Đầm Lầy" |
| `Btn Close` | Button "×" |
| `Map Selection Panel` | GameObject `MapSelectionPanel` |
| `Game Scene Name` | Tên scene game (vd: `GameScene`) |

3. Set `MapSelectionPanel.SetActive(false)` ban đầu (ẩn)

---

### Bước 3.4 — Button "Vào Game" Mở Panel

Tìm Button "Vào Game" / "Play" trong MainMenu → Inspector → `On Click ()`:
1. Dấu `+`
2. Kéo `MapSelectionPanel` object vào
3. Chọn function: `MapSelectionController → Show()`

---

### Bước 3.5 — MapManager Đọc SelectedMapId Khi Scene Load

Mở `Assets/Scripts/Map/MapManager.cs`, thêm vào `Awake()`:

```csharp
private void Awake()
{
    if (instance == null)
    {
        instance = this;
        DontDestroyOnLoad(gameObject);

        // 🆕 Đọc mapId từ PlayerPrefs (được set bởi MapSelectionController)
        int savedMapId = PlayerPrefs.GetInt("SelectedMapId", mapId);
        if (savedMapId != 0)
        {
            mapId = savedMapId;
            Debug.Log($"[MapManager] Loaded map ID from PlayerPrefs: {mapId}");
        }
    }
    else if (instance != this)
    {
        Destroy(gameObject);
    }
}
```

---

### Bước 3.6 — NetworkEnemySpawner Tự Lấy Map ID

`NetworkEnemySpawner` đã có logic này trong `Start()`:
```csharp
if (mapId == 0 && MapManager.Instance != null)
{
    mapId = MapManager.Instance.GetMapId();
}
```

Khi scene load, `NetworkEnemySpawner` sẽ lấy đúng `mapId` từ `MapManager` → call API đúng endpoint → spawn đúng enemy cho map đó.

---

### Bước 3.7 — Thêm Map Mới (Optional)

Để thêm map thứ 4:
1. Thêm `Button btnMap4` vào Inspector của `MapSelectionController`
2. Trong `Awake()`: `btnMap4.onClick.AddListener(() => SelectMap(4));`
3. Tạo scene mới tên `GameScene_Map4` hoặc dùng cùng scene với `mapId = 4`
4. Thêm spawn data vào API/database cho `map_id = 4`

---

## 4. Checklist Tổng

### PvP — Skill Damage Player Khác

- [ ] Tag `Player` đã tạo trong Tags & Layers
- [ ] Player Prefab có tag `Player`
- [ ] `FireballDamage.cs` xử lý collision với tag `Player`
- [ ] Gọi `targetHealth.TakeDamageServerRpc(damage)` — không phải `TakeDamage()`
- [ ] `TakeDamageServerRpc` có `RequireOwnership = false`
- [ ] Đã test bằng ParrelSync: HP giảm hiển thị trên cả 2 màn hình

### Enemy Sync Cho Tất Cả Client

- [ ] Enemy Prefab có đủ: `NetworkObject`, `NetworkTransform`, `NetworkEnemyHealth`, `NetworkEnemyController`, `EnemyAI`, `EnemyHealthBarSpawner`
- [ ] Collider2D trên enemy có tag `Enemy`
- [ ] Enemy Prefab đăng ký trong `DefaultNetworkPrefabs.asset` hoặc `NetworkManager`
- [ ] `EnemyPrefabManager` có đủ mapping `enemyId → prefab`
- [ ] `NetworkEnemySpawner` có `enemyPrefabManager` và `apiBaseURL` đúng
- [ ] `EnemyHealthBarSpawner` có `healthBarPrefab` được gán
- [ ] Test: Enemy spawn trên server → Client thấy enemy xuất hiện + HP bar

### Map Selection

- [ ] Script `MapSelectionController.cs` đã tạo
- [ ] `MapSelectionPanel` có đủ buttons gắn đúng slot
- [ ] Button "Vào Game" gọi `MapSelectionController.Show()`
- [ ] `MapManager.Awake()` đọc `PlayerPrefs.GetInt("SelectedMapId")`
- [ ] Test: Chọn Map 2 → vào game → enemy đúng của Map 2 spawn ra

---

## Lưu Ý Quan Trọng

| Điều | Lý do |
|------|-------|
| Luôn dùng `TakeDamageServerRpc()` thay vì `TakeDamage()` khi multiplayer | Chỉ server mới được ghi `NetworkVariable` |
| Enemy Prefab phải đăng ký trong NetworkManager | Nếu không đăng ký, `NetworkObject.Spawn()` sẽ throw error |
| Chỉ Host/Server mới gọi `SceneManager.LoadScene()` qua Netcode | Client sẽ nhận scene push tự động |
| `EnemyAI` chỉ chạy trên Server | Client không chạy AI, chỉ nhận sync vị trí qua `NetworkTransform` |
| `PlayerPrefs` để lưu mapId | Reset khi thoát game, nếu cần persistent dùng API save |
