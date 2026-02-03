# HƯỚNG DẪN: NETWORK PLAYER HEALTH SYSTEM

## 📋 TỔNG QUAN

`NetworkPlayerHealth` là hệ thống HP server-authoritative cho player trong multiplayer game. HP được quản lý bởi server và tự động sync cho tất cả clients qua `NetworkVariable`.

---

## 🎯 TẠI SAO CẦN NETWORKPLAYERHEALTH?

### Vấn đề với PlayerHealth (local):
- ❌ Mỗi client tự tính HP → không đồng bộ
- ❌ Dễ bị cheat: client có thể tự tăng HP
- ❌ Không phù hợp cho multiplayer

### Giải pháp với NetworkPlayerHealth:
- ✅ Server quyết định HP (server-authoritative)
- ✅ Chống cheat: client không thể tự sửa HP
- ✅ Tự động sync cho tất cả clients
- ✅ Hỗ trợ death/respawn trên server

---

## 🔧 CÁCH SỬ DỤNG

### Bước 1: Thêm Component vào Player Prefab

1. Chọn Player Prefab
2. Add Component → **NetworkPlayerHealth**
3. Component sẽ tự động yêu cầu `NetworkObject` (nếu chưa có)

### Bước 2: Cấu hình trong Inspector

#### **Health Settings**
- **Max Health**: HP tối đa (mặc định: 100)
- Tự động lấy từ `PlayerStats.maxHealth` nếu có

#### **Invincibility**
- **Invincibility Duration**: Thời gian bất tử sau khi bị đánh (mặc định: 1 giây)

#### **Respawn**
- **Respawn Delay**: Thời gian chờ trước khi respawn (mặc định: 3 giây)
- **Spawn Points**: Mảng các vị trí spawn (có thể để trống, sẽ tự tìm GameObject có tag "SpawnPoint")

### Bước 3: Setup Spawn Points (Optional)

**Cách 1: Tự động tìm**
- Tạo các GameObject với tag "SpawnPoint"
- Script sẽ tự động tìm và sử dụng

**Cách 2: Gán thủ công**
- Kéo các Transform vào mảng `Spawn Points` trong Inspector

### Bước 4: Tích hợp với HealthBar

`HealthBar` đã được cập nhật để tự động detect và sử dụng `NetworkPlayerHealth` nếu có, fallback về `PlayerHealth` nếu không có network.

Không cần làm gì thêm, HealthBar sẽ tự động hoạt động!

---

## 💻 API SỬ DỤNG

### Gây Damage (Từ Enemy hoặc Script khác)

```csharp
// Cách 1: Gọi trực tiếp (tự động chuyển thành ServerRpc)
NetworkPlayerHealth playerHealth = player.GetComponent<NetworkPlayerHealth>();
playerHealth.TakeDamage(10); // Gây 10 damage

// Cách 2: Gọi ServerRpc trực tiếp (nếu cần)
playerHealth.TakeDamageServerRpc(10);
```

### Heal

```csharp
// Heal một lượng
playerHealth.Heal(20); // Hoặc playerHealth.HealServerRpc(20);

// Heal full
playerHealth.HealFull(); // Hoặc playerHealth.HealFullServerRpc();
```

### Đọc giá trị HP

```csharp
int currentHP = playerHealth.GetCurrentHealth();
int maxHP = playerHealth.GetMaxHealth();
float percent = playerHealth.GetHealthPercent();
bool isDead = playerHealth.IsDead();
bool isInvincible = playerHealth.IsInvincible();
```

### Subscribe Events

```csharp
// HP thay đổi
playerHealth.OnHealthChanged.AddListener((current, max) => {
    Debug.Log($"HP: {current}/{max}");
});

// Chết
playerHealth.OnDeath.AddListener(() => {
    Debug.Log("Player died!");
});

// Bị đánh
playerHealth.OnTakeDamage.AddListener(() => {
    // Play sound, effect, v.v.
});

// Hồi máu
playerHealth.OnHeal.AddListener(() => {
    Debug.Log("Player healed!");
});

// Respawn
playerHealth.OnRespawn.AddListener(() => {
    Debug.Log("Player respawned!");
});
```

---

## 🏗️ KIẾN TRÚC

### Flow hoạt động:

```
[Enemy/Script] → TakeDamageServerRpc() → [Server validate] → [Server trừ HP] 
    → NetworkVariable sync → [Tất cả clients nhận update] → [HealthBar tự động update]
```

### Server-Authoritative:

- ✅ **Server quyết định**: Chỉ server mới có thể thay đổi HP
- ✅ **Client chỉ gửi request**: Client gọi `TakeDamageServerRpc()`, server validate và xử lý
- ✅ **Tự động sync**: `NetworkVariable` tự động sync HP cho tất cả clients
- ✅ **Chống cheat**: Client không thể tự sửa HP

---

## 🔄 DEATH VÀ RESPAWN

### Death Flow:

1. HP về 0 → `OnHealthValueChanged()` được gọi
2. Server xử lý `HandleDeath()`
3. `OnDeathClientRpc()` notify tất cả clients
4. Server đợi `respawnDelay` giây
5. Server gọi `RespawnServer()`
6. Reset HP, teleport đến spawn point
7. `OnRespawnClientRpc()` notify tất cả clients

### Respawn Points:

- Tự động tìm GameObject có tag "SpawnPoint"
- Hoặc gán thủ công vào mảng `Spawn Points`
- Chọn ngẫu nhiên một spawn point khi respawn

---

## ⚠️ LƯU Ý QUAN TRỌNG

### 1. NetworkObject Required

`NetworkPlayerHealth` yêu cầu `NetworkObject` component. Nếu chưa có, Unity sẽ tự động thêm.

### 2. Server-Only Operations

Một số operations chỉ chạy trên server:
- `TakeDamageServerRpc()` - Chỉ server mới thực sự trừ HP
- `HealServerRpc()` - Chỉ server mới thực sự heal
- `RespawnServer()` - Chỉ server mới xử lý respawn

### 3. NetworkVariable Sync

`networkCurrentHealth` là `NetworkVariable`, tự động sync cho tất cả clients. Không cần gọi RPC để sync HP.

### 4. God Mode

Nếu `PlayerController.godMode = true`, damage sẽ bị chặn (server-side check).

### 5. Invincibility Frames

Sau khi bị đánh, player có `invincibilityDuration` giây bất tử (mặc định: 1 giây).

---

## 🐛 TROUBLESHOOTING

### Vấn đề 1: HP không sync giữa clients

**Nguyên nhân:**
- NetworkObject chưa được spawn
- NetworkManager chưa start

**Giải pháp:**
1. Kiểm tra Player Prefab có NetworkObject component
2. Kiểm tra NetworkObject đã được spawn chưa (`IsSpawned`)
3. Kiểm tra NetworkManager đã start chưa

### Vấn đề 2: TakeDamage không hoạt động

**Nguyên nhân:**
- Không phải server
- Player đã chết
- God mode đang bật

**Giải pháp:**
1. Kiểm tra `IsServer` hoặc gọi `TakeDamageServerRpc()` từ client
2. Kiểm tra `IsDead()` trước khi gây damage
3. Kiểm tra `godMode` trong PlayerController

### Vấn đề 3: Respawn không hoạt động

**Nguyên nhân:**
- Không có spawn points
- Respawn delay chưa hết

**Giải pháp:**
1. Tạo GameObject với tag "SpawnPoint" hoặc gán vào mảng `Spawn Points`
2. Kiểm tra `respawnDelay` trong Inspector

### Vấn đề 4: HealthBar không update

**Nguyên nhân:**
- HealthBar chưa tìm thấy NetworkPlayerHealth
- Event chưa được subscribe

**Giải pháp:**
1. Kiểm tra HealthBar có tìm thấy NetworkPlayerHealth không (xem Console log)
2. Kiểm tra `OnHealthChanged` event có được invoke không

---

## 📝 TÓM TẮT

### NetworkPlayerHealth là gì?
- Hệ thống HP server-authoritative cho multiplayer
- HP được quản lý bởi server, sync tự động cho tất cả clients
- Hỗ trợ death/respawn trên server

### Cách sử dụng:
1. Add component vào Player Prefab
2. Cấu hình maxHealth, invincibility, respawn
3. Gọi `TakeDamage()` hoặc `Heal()` từ script khác
4. HealthBar tự động hoạt động

### Lợi ích:
- ✅ Chống cheat
- ✅ Đồng bộ chính xác
- ✅ Dễ sử dụng
- ✅ Tự động sync

---

**Tác giả**: Auto (AI Assistant)  
**Ngày tạo**: 2026  
**Phiên bản**: 1.0
