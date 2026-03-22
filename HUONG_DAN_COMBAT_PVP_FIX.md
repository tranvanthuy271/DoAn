# Hướng Dẫn: Combat PvP Fix + Hệ Thống Bình + HP Bar + Stun + Gray Overlay

## Tổng Quan Các Thay Đổi

### Vấn Đề Được Sửa
| Vấn đề | Nguyên nhân | Giải pháp |
|--------|------------|-----------|
| Dùng skill không mất MP | `UseSkill()` không trừ MP | Thêm `TryConsumeMP()` trước khi dùng skill |
| Skill không damage player khác | `FireballDamage` chỉ check tag "Enemy" | Thêm check tag "Player" + gọi `NetworkPlayerHealth` |
| HP Bar không có cho player khác | Không có world-space HP bar | Tạo `PlayerWorldHpBar.cs` |
| Không bị stun khi trúng skill | Không có hệ thống stun | Thêm `SetStunned()` trong `PlayerMovement` |
| Không có hiệu ứng xám khi trúng | Không có overlay | Tạo `PlayerHitEffect.cs` |
| Không có bình HP/MP | Không có hệ thống potion | Tạo `PotionUsage.cs` |

---

## 1. Fix MP Không Mất Khi Dùng Skill

### File Đã Sửa
- `Scripts/Player/Combat/PlayerSkillManager.cs` — Thêm `TryConsumeMP()`
- `Scripts/Network/Player/NetworkPlayerDataSync.cs` — Thêm `ConsumeMpServerRpc()`

### Cách Hoạt Động
```
Player nhấn phím skill
  → TryConsumeMP(skill.currentMpCost)
    → Kiểm tra networkMp.Value >= cost
    → Nếu đủ MP: trừ MP (server trực tiếp hoặc ServerRpc)
    → Nếu thiếu MP: log warning + chặn skill
  → UseSkill() tiếp tục bình thường
```

### Config Trong Unity
- `SkillData.currentMpCost` được load tự động từ DB qua `SkillRuntimeLoader`
- Không cần config thủ công
- Nếu test offline: Đặt `currentMpCost = 0` để không tốn MP

---

## 2. Fix Skill Không Damage Player Khác (PvP)

### File Đã Sửa
- `Scripts/Player/Skills/FireballDamage.cs` — Thêm "Player" tag handler
- `Scripts/Player/Skills/DotDamage.cs` — Cập nhật dùng `NetworkPlayerHealth`

### Cài Đặt Trong Unity (BẮT BUỘC)
**Tag cho Player Prefab phải là `"Player"`:**
1. Mở Player Prefab trong Unity Editor
2. Chọn root GameObject của prefab
3. Trong Inspector → Tag dropdown → Chọn hoặc tạo tag `"Player"`

### Tránh Tự Bắn Trúng Mình
- `FireballDamage.SetOwner(ulong id)` được gọi khi spawn projectile
- Cần thêm vào `PlayerSkillManager.SpawnProjectileWithDirection()`:
```csharp
// Sau khi Instantiate projectile, trước khi Spawn:
var fb = projectile.GetComponent<FireballDamage>();
if (fb != null) fb.SetOwner(GetComponent<NetworkObject>().NetworkObjectId);

var dot = projectile.GetComponent<DotDamage>();
if (dot != null) dot.SetOwner(GetComponent<NetworkObject>().NetworkObjectId);
```

### Kiểm Tra HP Sync
- Khi player bị damage, `NetworkPlayerHealth.TakeDamageServerRpc()` tự động sync HP về `NetworkPlayerDataSync.networkHp`
- HealthBar sẽ cập nhật đúng sau khi fix này

---

## 3. HP Bar Cho Player Khác (PlayerWorldHpBar)

### File Mới
`Scripts/UI/HUD/PlayerWorldHpBar.cs`

### Cài Đặt Trong Unity

#### Bước 1: Tạo Canvas World Space
1. Chọn Player Prefab → chuột phải → UI → Canvas
2. Canvas component settings:
   - **Render Mode**: World Space
   - **Sort Order**: 10 (hiện trên mọi thứ)
3. Đặt tên Canvas là `"PlayerHpBarCanvas"`

#### Bước 2: Thiết Lập Transform
- Canvas `LocalPosition`: `(0, 1.2, 0)` (trên đầu player)
- Canvas `LocalScale`: `(0.01, 0.01, 0.01)`
- Canvas `Width x Height`: `100 x 15`

#### Bước 3: Tạo UI Elements Trong Canvas
```
PlayerHpBarCanvas (Canvas - World Space)
├── Background (Image - màu đen, width=100, height=10)
├── HPSlider (Slider)
│   ├── Fill Area
│   │   └── Fill (Image - sẽ đổi màu xanh↔đỏ)
│   └── (xóa Handle Slide Area nếu có)
├── HPText (TextMeshPro - hiển thị "80/100")
└── PlayerNameText (TextMeshPro - hiển thị tên nhân vật)
```

#### Bước 4: Gắn Script
1. Chọn `PlayerHpBarCanvas`
2. Add Component → `PlayerWorldHpBar`
3. Kéo các UI elements vào Inspector:
   - `Hp Slider` → HPSlider
   - `Fill Image` → Fill image trong Slider
   - `Hp Text` → HPText
   - `Player Name Text` → PlayerNameText (tùy chọn)

#### Bước 5: Config
| Thuộc tính | Giá trị đề xuất | Mô tả |
|-----------|----------------|-------|
| `Hide For Local Player` | ✅ True | Ẩn HP bar của bản thân |
| `Face Camera` | ✅ True | Canvas luôn quay về camera |
| `Full Health Color` | Xanh lá | Màu khi HP đầy |
| `Low Health Color` | Đỏ | Màu khi HP thấp |
| `Low Health Threshold` | 0.3 | Ngưỡng chuyển màu (30% HP) |

---

## 4. Stun 0.5 Giây Khi Trúng Skill

### File Đã Sửa
- `Scripts/Player/Controllers/PlayerMovement.cs` — Thêm `stunTimer` + `SetStunned()`

### File Mới
- `Scripts/Player/Combat/PlayerHitEffect.cs` — Tích hợp stun + gray overlay

### Cài Đặt Trong Unity
1. Chọn Player Prefab root
2. Add Component → `PlayerHitEffect`
3. Config trong Inspector:

| Thuộc tính | Giá trị mặc định | Mô tả |
|-----------|-----------------|-------|
| `Hit Tint Color` | `(0.35, 0.35, 0.35, 1)` | Màu xám overlay |
| `Gray Overlay Duration` | `0.5` | Thời gian màu xám (giây) |
| `Stun Duration` | `0.5` | Thời gian bất động (giây) |

> ⚠️ Script **tự động** subscribe vào `NetworkPlayerHealth.OnTakeDamage` - không cần wire thủ công

### Cách Hoạt Động
```
Player bị trúng skill
  → NetworkPlayerHealth.TakeDamageServerRpc() (server)
  → OnTakeDamageClientRpc() broadcast → tất cả client
  → PlayerHitEffect.OnHit() được gọi
    → ApplyGrayOverlay(): đổi all SpriteRenderer sang màu xám 0.5s
    → IsOwner? → ApplyStun() → PlayerMovement.SetStunned(0.5f)
      → HandleInput() bị block 0.5s → không di chuyển được
```

---

## 5. Bình HP và MP (PotionUsage)

### File Mới
`Scripts/Player/Combat/PotionUsage.cs`

### Cài Đặt Trong Unity
1. Chọn Player Prefab root
2. Add Component → `PotionUsage`
3. Config trong Inspector:

| Thuộc tính | Mặc định | Mô tả |
|-----------|---------|-------|
| `Hp Potion Key` | `H` | Phím uống bình máu |
| `Hp Restore Amount` | `30` | Lượng HP hồi |
| `Hp Potion Cooldown` | `5` | Cooldown (giây) |
| `Mp Potion Key` | `M` | Phím uống bình mana |
| `Mp Restore Amount` | `30` | Lượng MP hồi |
| `Mp Potion Cooldown` | `5` | Cooldown (giây) |

### Cách Hoạt Động
```
Player nhấn H (HP) hoặc M (MP)
  → Kiểm tra cooldown
  → Gọi NetworkPlayerHealth.HealServerRpc() hoặc NetworkPlayerDataSync.RestoreHpServerRpc()
  → HP/MP được hồi và sync về tất cả client
  → HealthBar / MPBar tự động cập nhật
```

---

## 6. Tổng Hợp Các Script Mới/Sửa

### Scripts Mới
| File | Mô tả | Gắn vào |
|------|-------|---------|
| `PlayerHitEffect.cs` | Gray overlay + stun khi bị skill | Player Prefab root |
| `PlayerWorldHpBar.cs` | HP bar world-space cho player khác | Canvas con của Player Prefab |
| `PotionUsage.cs` | Bình HP/MP | Player Prefab root |

### Scripts Đã Sửa
| File | Thay đổi |
|------|---------|
| `PlayerSkillManager.cs` | Thêm MP check + TryConsumeMP() |
| `NetworkPlayerDataSync.cs` | Thêm ConsumeMpServerRpc, RestoreMpServerRpc, RestoreHpServerRpc |
| `NetworkPlayerHealth.cs` | Sync HP về NetworkPlayerDataSync khi damage |
| `FireballDamage.cs` | Thêm PvP damage (tag "Player") + owner check |
| `DotDamage.cs` | Dùng NetworkPlayerHealth cho PvP damage |
| `PlayerMovement.cs` | Thêm stun system (SetStunned, isStunned timer) |

---

## 7. Kiểm Tra Sau Khi Cài Đặt

### Checklist
- [ ] Player Prefab có tag `"Player"` (bắt buộc cho PvP damage)
- [ ] `PlayerHitEffect` được gắn vào Player Prefab root
- [ ] `PotionUsage` được gắn vào Player Prefab root
- [ ] World Space Canvas được tạo với `PlayerWorldHpBar` script
- [ ] HP + MP Bar UI đã có Slider và kéo đúng references

### Test PvP Damage
1. Bật ParrelSync hoặc Build + chạy 2 instance
2. Player A bắn skill vào Player B
3. Player B phải mất HP (HP bar giảm)
4. Player B bị màu xám 0.5 giây
5. Player B không di chuyển được 0.5 giây

### Test MP Consumption
1. Mở Stats tab → ghi nhớ MP hiện tại
2. Dùng một skill
3. MP phải giảm đúng với `currentMpCost` của skill đó

### Test Potion
1. Giảm HP bằng cách để bị tấn công
2. Nhấn `H` → HP tăng 30 (hoặc số đã config)
3. Không thể dùng lại trong 5 giây (cooldown)

---

## 8. Lưu Ý Quan Trọng

### Layer Collision Matrix
Đảm bảo trong **Edit → Project Settings → Physics 2D → Layer Collision Matrix**:
- Layer `"Player"` có thể va chạm với projectile layer

### Tag Player
Tag `"Player"` phải được tạo trong **Edit → Project Settings → Tags and Layers** trước khi gán cho prefab.

### NetworkPlayerHealth vs PlayerHealth
- **Multiplayer (Netcode)**: dùng `NetworkPlayerHealth` → server-authoritative
- **Standalone test**: dùng `PlayerHealth` → local only
- `FireballDamage` và `DotDamage` tự động detect và dùng đúng loại
