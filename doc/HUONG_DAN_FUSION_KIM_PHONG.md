# HƯỚNG DẪN SETUP SKILL FUSION KIM + PHONG (hybrid_id = 13)

> File này hướng dẫn từng bước cụ thể để config prefab **F_Phong** và **F_Kim**,  
> gồm: sửa các lỗi hiện tại, thêm skill 5 đạn ngang "Kim Phong Liên Tiễn",  
> và thêm skill melee mạnh "Kim Phong Trảm" cho hệ Kim chính.

---

## Mục lục

1. [Tổng quan kiến trúc](#1-tổng-quan-kiến-trúc)
2. [Fix lỗi F_Phong hiện tại](#2-fix-lỗi-f_phong-hiện-tại)
3. [Tạo Bullet Prefab MetalWindBullet](#3-tạo-bullet-prefab-metalwindbullet)
4. [Thêm skill Barrage vào F_Phong và F_Kim](#4-thêm-skill-barrage-vào-f_phong-và-f_kim)
5. [Config Skill 4 — Kiếm Phong Trảm (F_Kim)](#5-config-skill-4--kiếm-phong-trảm-f_kim)
6. [Animator Controller — thêm triggers](#6-animator-controller--thêm-triggers)
7. [Đăng ký NetworkPrefabs](#7-đăng-ký-networkprefabs)
8. [Checklist cuối](#8-checklist-cuối)

---

## 1. Tổng quan kiến trúc

```
F_Phong / F_Kim  (root GO)
├── PlayerController
├── PlayerMovement
├── PlayerAnimator          ← Animator Controller riêng của từng hệ
├── NetworkAnimator
├── PlayerCombat            ← Đánh thường (phím N)
├── PlayerSkillManager
│     skills[0]  Skill 1 (J)
│     skills[1]  Skill 2 (K)
│     skills[2]  Skill 3 (L)
│     skills[3]  Skill 4 (U)  ← THÊM MỚI
│     defaultSkillEffectObject → SkillEffect (child)
├── WindStepSkill           ← delegate skill L (Wind Step)
├── PlayerDash              ← Shift
├── HybridMetalWindGaleSkill    ← skill quạt 12 mũi tên (đã có)
└── HybridMetalWindBarrageSkill ← skill 5 đạn ngang (MỚI — code đã tạo)

Children:
├── GroundCheck
└── SkillEffect             ← Animator Controller riêng cho VFX skill
```

Hai scripts mới đã được tạo:

| File | Vị trí |
|---|---|
| `HybridMetalWindBarrageSkill.cs` | `Assets/Scripts/Player/Skills/Hybrid/` |
| `BarrageBulletDamage.cs` | `Assets/Scripts/Player/Skills/Hybrid/` |

---

## 2. Fix lỗi F_Phong hiện tại

Mở prefab `Assets/Prefabs/Player/Fusion/F_Phong.prefab`.

### 2.1 — WindStepSkill thiếu SkillEffect

| Component | Field | Hiện tại | Sửa thành |
|---|---|---|---|
| `WindStepSkill` | **Skill Effect Object** | `null` ❌ | Kéo child `SkillEffect` vào |

**Cách làm:**
1. Mở prefab F_Phong → Hierarchy thấy child **SkillEffect**
2. Chọn root `F_Phong` → nhìn Inspector → tìm component `WindStepSkill`
3. Kéo GameObject **SkillEffect** từ Hierarchy thả vào field **Skill Effect Object**

### 2.2 — PlayerCombat thiếu EnemyLayer

| Component | Field | Hiện tại | Sửa thành |
|---|---|---|---|
| `PlayerCombat` | **Enemy Layers** | `0` (không gì cả) ❌ | Chọn layer **Enemy** (layer 7) |
| `PlayerCombat` | **Attack Range** | `0.5` | `0.8` (gợi ý) |

**Cách làm:**
1. Chọn root `F_Phong` → `PlayerCombat`
2. Click field **Enemy Layers** → tích vào layer **Enemy**

---

## 3. Tạo Bullet Prefab MetalWindBullet

Đây là viên đạn mà skill Barrage sẽ spawn.

### Bước 3.1 — Tạo GameObject

1. Trong **Hierarchy** (không cần mở prefab nào), click chuột phải → **Create Empty**
2. Đặt tên: `MetalWindBullet`

### Bước 3.2 — Thêm Components

Thêm theo thứ tự sau:

| Component | Config quan trọng |
|---|---|
| `SpriteRenderer` | Gán sprite đạn (hình kim loại nhỏ / mũi tên / ngôi sao) |
| `Rigidbody2D` | Gravity Scale = **0** · Collision Detection = **Continuous** · Constraints: Freeze Rotation Z |
| `BoxCollider2D` | Is Trigger = **true** · Size ≈ `(0.2, 0.15)` |
| `NetworkObject` | _(không cần config gì thêm)_ |
| `BarrageBulletDamage` | _(script mới, không config trong prefab — sẽ được set lúc runtime)_ |

### Bước 3.3 — Lưu thành Prefab

1. Kéo GameObject `MetalWindBullet` từ Hierarchy vào:  
   `Assets/Prefabs/Projectile/MetalWindBullet.prefab`
2. Xóa instance khỏi Hierarchy sau khi đã tạo prefab

---

## 4. Thêm skill Barrage vào F_Phong và F_Kim

### 4.1 — Add Component

1. Mở prefab `F_Phong.prefab` (hoặc `F_Kim.prefab`)
2. Chọn root GameObject
3. **Add Component** → tìm `HybridMetalWindBarrageSkill`

### 4.2 — Config Inspector

| Field | Giá trị | Ghi chú |
|---|---|---|
| **Skill Code** | `HYBRID_METAL_WIND_BARRAGE` | Khớp với DB |
| **Cooldown** | `10` | giây |
| **Mp Cost** | `40` | |
| **Effect Value** | `120` | damage mỗi viên đạn |
| **Bullet Prefab** | `MetalWindBullet` | prefab vừa tạo ở Bước 3 |
| **Bullet Count** | `5` | |
| **Y Spacing** | `0.25` | khoảng cách Y giữa các viên |
| **Fire Delay** | `0.08` | giây giữa mỗi viên |
| **Bullet Speed** | `18` | units/giây |
| **Bullet Lifetime** | `2.5` | giây |
| **Spawn Offset X** | `0.6` | khoảng cách từ player |

> Visual kết quả — 5 viên đạn theo Y:  
> `Y = -0.50 → -0.25 → 0.00 → +0.25 → +0.50` (cách nhau 0.08s)

### 4.3 — Thêm SkillData vào PlayerSkillManager

Chọn root → `PlayerSkillManager` → mở **Skills** list → nhấn **+** để thêm element mới (index 3):

| Field | Giá trị |
|---|---|
| **Skill Name** | `Kim Phong Liên Tiễn` |
| **Skill Code** | `HYBRID_METAL_WIND_BARRAGE` |
| **Skill Type** | `Projectile` (0) |
| **Activation Key** | `U` (KeyCode = 117) |
| **Cooldown** | `10` |
| **projectilePrefab** | _(để trống — HybridSkillBase tự xử lý)_ |
| **animationTriggerName** | `HybridSkill` |
| Còn lại | mặc định |

> **Tại sao Skill Type = Projectile?**  
> PlayerSkillManager sẽ gọi `SpawnProjectile` nhưng `projectilePrefab = null` → không spawn gì cả.  
> `HybridMetalWindBarrageSkill` tự lắng nghe qua `TryUse()` của `HybridSkillBase`.  
> Trigger animation `HybridSkill` vẫn chạy qua `PlayerAnimator.TriggerHybridSkill()`.

---

## 5. Config Skill 4 — Kiếm Phong Trảm (F_Kim)

Skill này dành cho **F_Kim** (hệ chính Kim): melee mạnh diện rộng, cooldown ngắn hơn Barrage.

### 5.1 — Thêm vào PlayerSkillManager.skills (index 3)

Mở `F_Kim.prefab` → `PlayerSkillManager` → thêm element index 3:

| Field | Giá trị |
|---|---|
| **Skill Name** | `Kiếm Phong Trảm` |
| **Skill Code** | `METAL_WIND_SLASH` |
| **Skill Type** | `Melee` (2) |
| **Activation Key** | `U` (KeyCode = 117) |
| **Cooldown** | `5` |
| **animationTriggerName** | `Skill4` |
| **currentEffectValue** | `0` _(load từ DB)_ |

> F_Kim chưa có prefab → cần duplicate từ `F_Phong.prefab`, đổi tên, thay Animator Controller,  
> và thay sprite sang hệ Kim.

### 5.2 — Thêm PlayerCombat melee cho Skill4

Skill4 dùng `SkillType.Melee` — `PlayerSkillManager` sẽ chỉ trigger animation và thực hiện
`OverlapCircle` để detect enemy. Đảm bảo:
- `PlayerCombat.enemyLayers` = layer **Enemy**
- `attackRange` ≥ `1.5` cho diện rộng

---

## 6. Animator Controller — thêm Triggers

### 6.1 — Animator Controller của Player (phong.controller / kim.controller)

Mở controller của từng hệ trong Unity Animator Window.

**Thêm Parameter:**

| Tên | Loại |
|---|---|
| `Attack` | Trigger |
| `HybridSkill` | Trigger |

**Thêm Transition:**

```
Any State ──[HybridSkill]──► HybridSkill_State
    └── Animation clip: hiệu ứng phát sáng / vung tay / pose Hybrid
    └── Has Exit Time: false
    └── Transition Duration: 0.05
    └── Can Transition To Self: false
```

### 6.2 — Animator Controller của SkillEffect (child)

Mở controller gắn vào child `SkillEffect`.

**Thêm Parameters:**

| Tên | Loại |
|---|---|
| `Skill1` | Trigger |
| `Skill2` | Trigger |
| `Skill3` | Trigger |

**Thêm Transitions từ Any State:**

```
Any State ──[Skill1]──► Skill1_Anim   (animation VFX Chướng Phong)
Any State ──[Skill2]──► Skill2_Anim   (animation VFX Phong Nhận)
Any State ──[Skill3]──► Skill3_Anim   (animation VFX Phong Thoái Bộ)
```

> Nếu chưa có animation clip VFX → có thể dùng clip giả (1 sprite trắng) để test trước.

---

## 7. Đăng ký NetworkPrefabs

**BẮT BUỘC** cho multiplayer: mọi prefab có `NetworkObject` đều phải được đăng ký.

1. Mở `Assets/ScriptableObjects/NetworkPrefabsList.asset`
2. Nhấn **+** → kéo `MetalWindBullet.prefab` vào
3. Hoặc mở `NetworkManager` prefab → mục **Network Prefabs** → thêm vào

---

## 8. Checklist cuối

### F_Phong

- [ ] `WindStepSkill.Skill Effect Object` = child `SkillEffect`
- [ ] `PlayerCombat.enemyLayers` = layer **Enemy**
- [ ] `PlayerSkillManager.skills[3]` thêm `Kim Phong Liên Tiễn`
- [ ] `HybridMetalWindBarrageSkill` được Add Component và gán `bulletPrefab`
- [ ] Animator Controller của player có Trigger `HybridSkill`
- [ ] Animator Controller của SkillEffect có Trigger `Skill1`, `Skill2`, `Skill3`

### F_Kim (khi tạo mới)

- [ ] Duplicate từ F_Phong, đổi tên thành `F_Kim.prefab`
- [ ] Thay Animator Controller → `kim.controller`
- [ ] `PlayerSkillManager.skills[3]` = `Kiếm Phong Trảm` (Melee, key U, CD 5s)
- [ ] Animator Controller kim.controller có Trigger `Skill4`

### Projectile

- [ ] `MetalWindBullet.prefab` tồn tại tại `Assets/Prefabs/Projectile/`
- [ ] `MetalWindBullet` có: SpriteRenderer + Rigidbody2D + BoxCollider2D(trigger) + NetworkObject + BarrageBulletDamage
- [ ] `MetalWindBullet` đã đăng ký trong `NetworkPrefabsList`

### NetworkPlayerSpawner

- [ ] `hybridMetalWindPrefab_WindPrimary` = `F_Phong.prefab`
- [ ] `hybridMetalWindPrefab_MetalPrimary` = `F_Kim.prefab`

---

## Sơ đồ luồng khi nhấn phím U

```
Player nhấn U
     │
     ▼
PlayerSkillManager.HandleSkillInput()
     │  skills[3].CanUse() == true?
     ▼
UseSkill(skills[3])  ← skillType = Projectile (projectilePrefab = null → bỏ qua spawn)
     │  animationTriggerName = "HybridSkill"
     │
     ├──► TriggerPlayerSkillEffectAnimation()  ← nếu có SkillEffect
     │
     └──► HybridMetalWindBarrageSkill.TryUse(direction)  [IsOwner check]
               │
               ▼  [ServerRpc]
          ExecuteSkill(direction)  [chạy trên Server]
               │
               └──► FireSequence(direction) Coroutine
                         │  bắn 5 viên, delay 0.08s giữa mỗi viên
                         ├── Spawn bullet Y = -0.50
                         ├── Spawn bullet Y = -0.25
                         ├── Spawn bullet Y =  0.00
                         ├── Spawn bullet Y = +0.25
                         └── Spawn bullet Y = +0.50

          PlayAnimationClientRpc()  ← trigger "HybridSkill" trên tất cả client
```

---

*Generated: 2026-03-19 | Dự án: Ngũ Hành Game*
