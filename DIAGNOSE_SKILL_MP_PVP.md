# Chẩn Đoán & Sửa Lỗi: Skill / MP / PvP Damage

## Tóm tắt vấn đề

| # | Triệu chứng | Nguyên nhân gốc | Trạng thái |
|---|-------------|-----------------|------------|
| 1 | Client không thể dùng skill (host OK) | networkMp khởi tạo từ DB = 0 (sau khi thêm SkillRuntimeLoader) | ✅ Đã fix |
| 2 | MP không bị trừ khi dùng skill | `SkillRuntimeLoader` chưa có trên prefab → `currentMpCost = 0` mãi | ⚠️ Cần fix trong Unity Editor |
| 3 | Skill đánh player khác không mất HP | `OnTriggerEnter2D` chạy trên tất cả clients → gọi TakeDamageServerRpc nhiều lần | ✅ Đã fix |
| 4 | Có thể tự bắn vào bản thân | `SetOwner()` không được gọi sau Spawn → ownerNetworkObjectId = 0 | ✅ Đã fix |
| 5 | PlayerHitEffect / PotionUsage không hoạt động | 3 component thiếu trên tất cả prefab Player | ⚠️ Cần fix trong Unity Editor |

---

## Bug 1 – Client không thể dùng skill

### Nguyên nhân

```
Flow: UseSkill() → TryConsumeMP(cost) → nếu cost > networkMp.Value → return false → skill bị block
```

Khi `SkillRuntimeLoader` được thêm vào prefab, nó load `currentMpCost` từ API (ví dụ: 30 MP). Nhưng `NetworkPlayerDataSync` khởi tạo `networkMp.Value` bằng **giá trị current mp lưu trong DB**, có thể đã bằng 0 từ session trước.

Kết quả: `networkMp.Value = 0`, `currentMpCost = 30` → `TryConsumeMP(30)` trả về false → không dùng được skill.

### Code đã sửa

**File:** `Assets/Scripts/Network/Player/NetworkPlayerDataSync.cs`

```csharp
// BEFORE (BUG):
networkMp.Value = playerData.final_stats.mp;    // current mp từ DB, có thể = 0

// AFTER (FIX):
networkMp.Value = playerData.final_stats.max_mp; // Initialize full MP on spawn
```

Áp dụng ở cả 2 hàm: `UpdateNetworkVariablesFromPlayerData()` và `LoadPlayerDataFromGameManager()`.

---

## Bug 2 – MP không bị trừ khi dùng skill

### Nguyên nhân

```
TryConsumeMP(cost):
    if (cost <= 0) return true;  ← Khi cost = 0, bỏ qua toàn bộ, không gọi ConsumeMpServerRpc
```

`SkillData.currentMpCost` có giá trị mặc định là `0` (serialized field trong ScriptableObject/prefab). Không có `SkillRuntimeLoader` để load giá trị thực từ API → `currentMpCost = 0` mãi mãi → MP không bao giờ bị trừ.

### Cách fix trong Unity Editor

**Bắt buộc thêm component `SkillRuntimeLoader` vào TẤT CẢ prefab Player:**

1. Mở Unity Editor
2. Mở từng prefab trong `Assets/Prefabs/Player/Fusion/`:
   - F_Hoa, F_Kim, F_Moc, F_Phong, F_Tho, F_Thuy
3. Mở từng prefab trong `Assets/Prefabs/Player/He/`:
   - Hoa, Kim, Moc, Phong, Tho, Thuy
4. Chọn root GameObject của prefab
5. **Add Component → SkillRuntimeLoader**
6. Save prefab (Ctrl+S)

> **Lý do phải dùng Unity Editor:** `SkillRuntimeLoader` là `NetworkBehaviour`. NetworkBehaviour **bắt buộc** phải được thêm vào prefab trước khi spawn lên network — không thể `AddComponent` ở runtime sau khi network object đã spawned.

---

## Bug 3 – Skill đánh vào player khác không mất HP (hoặc mất quá nhiều)

### Nguyên nhân

Trong Unity Netcode for GameObjects, physics simulation chạy **trên tất cả clients** (server + remote clients). Khi projectile di chuyển qua NetworkTransform, Rigidbody2D vẫn được simulate ở mỗi client → `OnTriggerEnter2D` bắn trên **mỗi** client.

Trước khi fix:
```
Server:        FireballDamage.OnTriggerEnter2D ─→ TakeDamageServerRpc(damage)  [gọi 1 lần]
Client A:      FireballDamage.OnTriggerEnter2D ─→ TakeDamageServerRpc(damage)  [gọi thêm 1 lần]
Client B:      FireballDamage.OnTriggerEnter2D ─→ TakeDamageServerRpc(damage)  [gọi thêm 1 lần]
→ Target nhận damage × 3 (hoặc nhiều hơn tùy số client)
```

### Code đã sửa

**File:** `Assets/Scripts/Player/Skills/FireballDamage.cs`  
**File:** `Assets/Scripts/Player/Skills/DotDamage.cs`

```csharp
private void OnTriggerEnter2D(Collider2D collision)
{
    // Chỉ server xử lý damage để tránh gọi RPC nhiều lần từ mỗi client
    if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer)
        return;

    // ... xử lý damage bình thường
}
```

**Nguyên tắc:** Trong Netcode for GameObjects, logic nhạy cảm (damage, spawn, destroy) **chỉ nên chạy trên server**. Clients chỉ hiển thị visual effects.

---

## Bug 4 – Tự bắn vào bản thân

### Nguyên nhân

`FireballDamage.SetOwner(ownerNetworkObjectId)` tồn tại nhưng **không bao giờ được gọi** sau khi projectile spawn. Kết quả:

```csharp
private ulong ownerNetworkObjectId = 0;  // Luôn = 0

// Trong OnTriggerEnter2D:
if (ownerNetworkObjectId != 0 && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
    return;  // Điều kiện ownerNetworkObjectId != 0 luôn FALSE → không skip
```

### Code đã sửa

**File:** `Assets/Scripts/Player/Combat/PlayerSkillManager.cs`  
Trong hàm `SpawnProjectileWithDirection()`, sau `projectileNetworkObject.Spawn()`:

```csharp
if (IsServer)
{
    projectileNetworkObject.Spawn();

    // Gán owner để projectile không tự gây damage cho người bắn
    ulong ownerId = NetworkObjectId;
    var fireballDmg = projectile.GetComponent<FireballDamage>();
    if (fireballDmg != null) fireballDmg.SetOwner(ownerId);
    var dotDmg = projectile.GetComponent<DotDamage>();
    if (dotDmg != null) dotDmg.SetOwner(ownerId);
}
```

---

## Bug 5 – PlayerHitEffect và PotionUsage không hoạt động

### Nguyên nhân

Ba component sau đã được tạo trong code nhưng **chưa được thêm vào prefab Player**:

| Component | GUID | Loại | Tác dụng |
|-----------|------|------|----------|
| `SkillRuntimeLoader` | `40034d4d3475c2b45853a877f447d355` | NetworkBehaviour | Load MP cost, cooldown từ API |
| `PlayerHitEffect` | `92ab84efaf7f4994e834a37e72709003` | MonoBehaviour | Gray overlay + stun khi bị đánh |
| `PotionUsage` | `1c27a1fb127ac8e4f884c1d19648cee4` | NetworkBehaviour | Phím H/M dùng potion |

### Cách fix trong Unity Editor

Mở từng prefab Player (xem danh sách ở Bug 2), sau đó:

1. **Add Component → SkillRuntimeLoader** (NetworkBehaviour — phải thêm trước spawn)
2. **Add Component → PlayerHitEffect** (MonoBehaviour — có thể thêm sau nhưng nên thêm trước)
3. **Add Component → PotionUsage** (NetworkBehaviour — phải thêm trước spawn)

> ⚠️ **Lưu ý quan trọng về NetworkBehaviour:** Các component là NetworkBehaviour (`SkillRuntimeLoader`, `PotionUsage`) **phải** được thêm vào prefab và đăng ký trong `DefaultNetworkPrefabs.asset` trước khi chạy. Nếu thêm sau khi đã build/run, cần **rebuild** và kiểm tra lại `DefaultNetworkPrefabs.asset`.

---

## Log debug đã thêm

Để chẩn đoán runtime, đã thêm log sau vào `PlayerSkillManager.UseSkill()`:

```
[PlayerSkillManager] UseSkill: <TênSkill> | IsOwner=True | IsServer=False | MP=100/100 | Cost=30
```

**Cách đọc log:**
- `IsOwner=True` → client là owner của player → UseSkill được phép chạy ✅
- `IsOwner=False` → KHÔNG phải owner → skill không được kích hoạt ❌
- `MP=0/100 | Cost=30` → không đủ MP để dùng skill ❌
- `Cost=0` → SkillRuntimeLoader chưa load được giá trị → cần thêm component vào prefab ❌

---

## Thứ tự fix hoàn chỉnh

1. **[Code - DONE]** `FireballDamage.cs` – IsServer guard
2. **[Code - DONE]** `DotDamage.cs` – IsServer guard  
3. **[Code - DONE]** `PlayerSkillManager.cs` – SetOwner sau Spawn + debug log
4. **[Code - DONE]** `NetworkPlayerDataSync.cs` – Khởi tạo MP = max_mp
5. **[Unity Editor - CẦN LÀM]** Thêm `SkillRuntimeLoader` vào tất cả prefab Player
6. **[Unity Editor - CẦN LÀM]** Thêm `PlayerHitEffect` vào tất cả prefab Player
7. **[Unity Editor - CẦN LÀM]** Thêm `PotionUsage` vào tất cả prefab Player

---

## Kiểm tra sau khi fix

Sau khi thêm đủ components trong Unity Editor:

1. **Host** khởi động game, tạo session
2. **Client** join vào
3. Kiểm tra Console:
   - `[NetworkPlayerDataSync] ✓ NetworkVariables updated for player: ...`
   - `[SkillRuntimeLoader] Loaded skill costs for ...` (nếu có log)
4. Client nhấn phím skill (J/K/L/U tùy element)
5. Kiểm tra Console client:
   - `[PlayerSkillManager] UseSkill: ... | IsOwner=True | MP=100/100 | Cost=30`
6. Sau khi dùng skill, kiểm tra MP bar giảm đúng lượng
7. Bắn skill vào host, kiểm tra HP bar của host giảm (chỉ giảm 1 lần)
