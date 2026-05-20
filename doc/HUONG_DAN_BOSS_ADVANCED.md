# HƯỚNG DẪN HỆ THỐNG BOSS NÂNG CAO

> **Phiên bản:** 1.0 | **Engine:** Unity 2022+ / Netcode for GameObjects

---

## 1. Cấu trúc file

```
Assets/Scripts/Boss/
├── BossData.cs               ← ScriptableObject config (Inspector)
├── BossController.cs         ← AI chính (MonoBehaviour, server-only logic)
├── NetworkBossController.cs  ← Sync network (scale, anim, stealth alpha)
├── NetworkBossHealth.cs      ← HP server-authoritative + dodge hook
└── Projectiles/
    ├── BossFireball.cs       ← Hỏa cầu rơi từ trời
    └── BossLightningBolt.cs  ← Tia sét gây stun
```

---

## 2. Tạo Boss Prefab (step-by-step)

### 2.1 Component tối thiểu

| Component | Ghi chú |
|-----------|---------|
| `NetworkObject` | Bắt buộc cho multiplayer |
| `NetworkTransform` | Tự thêm bởi `NetworkBossController` nếu chưa có |
| `Rigidbody2D` | BodyType = **Dynamic**, Freeze Rotation Z |
| `Collider2D` | CapsuleCollider2D hoặc BoxCollider2D |
| `Animator` | Xem phần 4 |
| `BossController` | AI chính |
| `NetworkBossController` | Sync clients |
| `NetworkBossHealth` | HP server-authoritative |
| `SpriteRenderer` | Trên child hoặc root |

### 2.2 Tạo GroundCheck child

```
Boss (prefab root)
└── GroundCheck (Transform, Y ≈ -0.55)
```

Gán vào trường `Ground Check` của `BossController`.

---

## 3. Cài đặt BossData (ScriptableObject)

**Tạo asset:** `Assets → Create → Game/Boss/Boss Data`

### 3.1 Thông tin cơ bản

| Trường | Ý nghĩa |
|--------|---------|
| `Boss Name` | Tên hiển thị |
| `Max Health` | HP tối đa |
| `Level` | Level boss |
| `Exp Reward` | EXP thưởng khi giết |

### 3.2 Di chuyển

| Trường | Ý nghĩa |
|--------|---------|
| `Move Speed` | Tốc độ đi bình thường |
| `Chase Speed` | Tốc độ đuổi người chơi |
| `Detection Range` | Bán kính phát hiện (Gizmo đỏ) |
| `Melee Attack Range` | Bán kính đánh thường (Gizmo vàng) |
| `Can Jump` | Bật để boss nhảy như người chơi |
| `Jump Force` | Lực nhảy (≈ 8–12) |
| `Max Jumps` | 1 = nhảy đơn, 2 = double jump |
| `Can Fly` | Bật để boss bay lượn (overrides movement) |
| `Fly Height` | Chiều cao Y so với người chơi khi bay |
| `Fly Speed` | Tốc độ bay |

### 3.3 Né tránh skill người chơi

| Trường | Ý nghĩa |
|--------|---------|
| `Dodge Chance` | % xác suất né (0–100) |
| `Dodge Cooldown` | Thời gian hồi giữa các lần né (giây) |
| `Dodge Distance` | Khoảng cách dịch chuyển khi né |

> Mỗi lần boss nhận damage, server roll ngẫu nhiên — nếu ≤ `dodgeChance` thì damage = 0 và boss dịch về phía sau.

### 3.4 Sát thương cố định khi bị đánh

| Trường | Ý nghĩa |
|--------|---------|
| `Return Damage Enabled` | Bật để boss trả damage |
| `Return Damage Amount` | Lượng damage trả lại mỗi lần bị đánh |

### 3.5 Tự hồi HP

| Trường | Ý nghĩa |
|--------|---------|
| `Hp Regen Enabled` | Bật tính năng hồi HP |
| `Regen Threshold Pct` | Bắt đầu hồi khi HP ≤ x% (VD: 50) |
| `Regen Per Sec` | HP hồi mỗi giây |

### 3.6 Kháng nguyên tố

`Khang Hoa / Thuy / Tho / Moc / Kim / Phong` — Giá trị 0–100 (%).  
Damage thực = `rawDamage × (1 − resist/100)`.

---

## 4. Animator — yêu cầu parameter

| Parameter | Type | Dùng bởi |
|-----------|------|---------|
| `isAttacking` | **Bool** | Mọi skill + đánh thường |
| `isMoving` | Bool | Di chuyển/đuổi |
| `isGrounded` | Bool | Nhảy/rơi |
| `Jump` | Trigger | Nhảy |
| `Dodge` | Trigger | Né tránh |
| `Die` | Trigger | Chết |

> **Quan trọng:** `isAttacking` phải là **Bool**, không phải Trigger, vì cần giữ trạng thái trong suốt animation attack.

---

## 5. Kỹ năng chi tiết

### 5.1 Đánh thường (`normalAttack`)

- Range kiểm tra qua `Physics2D.OverlapCircleAll` về phía boss đang mặt
- Gây knockback qua `Rigidbody2D.AddForce`
- Bật `isAttacking = true` → reset sau 0.65 giây

### 5.2 Hỏa Cầu Mưa (`fireballRain`)

**Config:**

| Trường | Ý nghĩa |
|--------|---------|
| `Fireball Prefab` | Prefab hỏa cầu (BossFireball.cs) |
| `Damage` | Sát thương mỗi viên |
| `Count` | Số viên mỗi lần cast |
| `Spawn Height` | Chiều cao spawn trên đầu người chơi |
| `Spread Radius` | Phạm vi X ngẫu nhiên |
| `Fall Speed` | Tốc độ rơi xuống |
| `Cooldown` | Thời gian hồi (giây) |

**Logic hủy hỏa cầu:**

```
Hỏa cầu rơi xuống
    → Chạm tag "Player"        → damage + hủy
    → Chạm tag "GroundFinal"   → hủy (tầng đất cuối)
    → Chạm ground khác         → XUYÊN QUA (không hủy)
```

**Setup tag cho tầng đất:**
- Tầng cuối (floor -1): thêm tag `GroundFinal` vào Tilemap Collider / Collider2D
- Tầng trung gian (floor 0, 1, 2...): giữ tag `Ground` hoặc bất kỳ — hỏa cầu xuyên qua

### 5.3 Sét Liên Tiếp (`lightning`)

**Config:**

| Trường | Ý nghĩa |
|--------|---------|
| `Lightning Prefab` | Prefab tia sét (BossLightningBolt.cs) |
| `Damage` | Sát thương mỗi tia |
| `Bolt Count` | Số tia (4–5) |
| `Bolt Spacing` | Khoảng cách giữa các tia (X) |
| `Bolt Duration` | Thời gian mỗi tia tồn tại |
| `Bolt Delay` | Delay giữa các tia liên tiếp |
| `Stun Duration` | Thời gian đứng im khi trúng |
| `Cooldown` | Thời gian hồi |

**Hiệu ứng khi trúng:**
- Trừ HP ngay lập tức
- Gọi `PlayerMovement.SetStunned(stunDuration)` trên owner client → người chơi không di chuyển được

### 5.4 Ẩn Thân (`stealth`)

| Trường | Ý nghĩa |
|--------|---------|
| `Duration` | Thời gian ẩn thân (giây) |
| `Cooldown` | Thời gian hồi |
| `Stealth Alpha` | Alpha khi ẩn (0 = vô hình hoàn toàn, 0.1 = gần vô hình) |

- Boss vẫn di chuyển và đuổi player trong stealth
- Tất cả `SpriteRenderer` trong prefab đều bị giảm alpha
- Sync alpha qua `NetworkBossController._netAlpha`

---

## 6. Tạo Prefab Hỏa Cầu

```
BossFireball (prefab)
├── SpriteRenderer   (sprite lửa)
├── CircleCollider2D (isTrigger = true)
├── Rigidbody2D      (gravity scale = 0 — BossFireball.Init override)
├── BossFireball.cs
├── NetworkObject    (nếu dùng multiplayer)
└── Animator         (tùy chọn — vòng lặp fire)
```

---

## 7. Tạo Prefab Tia Sét

```
BossLightningBolt (prefab)
├── SpriteRenderer    (sprite sét)
├── BoxCollider2D     (isTrigger = true, bao phủ chiều cao tia)
├── BossLightningBolt.cs
├── NetworkObject     (nếu dùng multiplayer)
└── Animator          (tùy chọn — vòng lặp sét)
```

---

## 8. Setup Multiplayer

### 8.1 Đăng ký Network Prefabs

Vào `NetworkManager → Network Prefabs List`, thêm:
- Boss prefab
- BossFireball prefab
- BossLightningBolt prefab

### 8.2 Server spawn boss

```csharp
// Ví dụ spawn boss từ server
GameObject bossObj = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
bossObj.GetComponent<NetworkObject>().Spawn(true);
```

### 8.3 Damage từ player → boss

Thay thế `NetworkEnemyHealth.TakeDamageServerRpc(damage)` bằng:

```csharp
// Trong script skill của player
bossHealth.TakeDamageServerRpc(damage, "Hoa"); // Truyền element type
```

---

## 9. Tối ưu & Bảo mật

| Điểm | Giải pháp trong code |
|------|---------------------|
| AI chỉ chạy trên server | `ShouldRunAI()` gate trong `BossController.Update` |
| Damage chỉ server xử lý | `NetworkBossHealth.TakeDamageInternal` server-only |
| Dodge không thể bị client fake | Xác suất né tính trên server |
| Return damage không thể spam | Chỉ khi `returnDamageEnabled = true` và damage > 0 |
| Stun chỉ gửi đến đúng owner | `ClientRpcParams.TargetClientIds = [ownerClientId]` |
| Projectile không memory leak | `maxLifetime` fallback + `CancelInvoke` khi destroy |
| Fireball hit chỉ tính 1 lần | `_hasHit` bool flag |
| Lightning hit tracking per player | `HashSet<uint>` theo NetworkObjectId |

---

## 10. Checklist nhanh

- [ ] Tạo `BossData` ScriptableObject, điền stats + skill config
- [ ] Tạo Boss prefab với đầy đủ component (mục 2.1)
- [ ] Tạo `GroundCheck` child, gán vào `BossController`
- [ ] Tạo prefab `BossFireball` và `BossLightningBolt` (mục 6, 7)
- [ ] Gán prefab vào `BossData.fireballRain.fireballPrefab` và `BossData.lightning.lightningPrefab`
- [ ] Tag tầng đất cuối cùng là `GroundFinal`
- [ ] Animator boss có đủ parameters (mục 4)
- [ ] Đăng ký 3 prefabs vào `NetworkManager`
- [ ] (Nếu cần EXP) Implement `IExpReceiver` trên `PlayerController`

---

## 11. Ví dụ config nhanh cho 2 loại boss

### Dragon Boss (bay lượn + hỏa cầu)
```
canFly = true | flyHeight = 4 | flySpeed = 5
dodgeChance = 10%
fireballRain.enabled = true | count = 5 | damage = 40
lightning.enabled = false
stealth.enabled = false
hpRegenEnabled = true | regenThresholdPct = 30% | regenPerSec = 8
```

### Shadow Boss (ẩn thân + sét)
```
canJump = true | maxJumps = 2 | jumpForce = 10
dodgeChance = 40%
fireballRain.enabled = false
lightning.enabled = true | boltCount = 5 | stunDuration = 3
stealth.enabled = true | duration = 5 | stealthAlpha = 0
returnDamageEnabled = true | returnDamageAmount = 15
```
