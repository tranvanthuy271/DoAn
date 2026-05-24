# HƯỚNG DẪN CONFIG HỆ THỐNG SKILL TOÀN BỘ

## Mục lục
1. [Tổng quan kiến trúc](#tổng-quan-kiến-trúc)
2. [Chạy migration DB](#chạy-migration-db)
3. [Skill per element — bảng tổng hợp](#skill-per-element)
4. [Config Unity — Player Prefab](#config-unity--player-prefab)
5. [Config Animator Controller (SkillEffect)](#config-animator-controller-skilleffect)
6. [Config hotbar UI + cooldown hiển thị](#config-hotbar-ui--cooldown-hiển-thị)
7. [Load stats từ DB tự động (SkillRuntimeLoader)](#load-stats-từ-db-tự-động-skillruntimeloader)
8. [Hướng dẫn chi tiết hệ Phong](#hướng-dẫn-chi-tiết-hệ-phong)
9. [Thêm hệ mới / thêm skill thứ 4](#thêm-hệ-mới--thêm-skill-thứ-4)
10. [Cấu trúc levels_json](#cấu-trúc-levels_json)
11. [Luồng dữ liệu đầy đủ](#luồng-dữ-liệu-đầy-đủ)

---

## Tổng quan kiến trúc

```
DB (skill_template)
  │  levels_json: [{ level_req, sp_cost, effect_value, mp_cost, cooldown_sec, desc }]
  │
  ▼  GET /api/player/{id}/skills
API (PlayerController)
  │  Trả: current_cooldown_sec, current_effect_value, current_mp_cost
  │       (của level hiện tại player đang có)
  │
  ▼  SkillRuntimeLoader.cs (chạy sau OnNetworkSpawn — Owner)
Unity Client
  ├── SkillData.cooldown         ← current_cooldown_sec
  ├── SkillData.currentEffectValue ← current_effect_value
  ├── SkillData.currentMpCost    ← current_mp_cost
  ├── WindStepSkill.cooldown     ← (nếu là WIND_STEP)
  └── TeleportSkill.cooldown     ← (nếu là DASH)
        │
        ▼
PlayerSkillManager → UseSkill() (Melee / Projectile / WindStep / Teleport)
        │
        ▼
SkillHotbarUI / SkillSlotUI → cooldown overlay + countdown text
```

---

## Chạy migration DB

**Thứ tự bắt buộc:**

```bash
# 1. Schema gốc (chỉ lần đầu)
source gamedb.sql

# 2. Thêm skill hệ Phong
source GameServerApi/migration_wind_skills.sql

# 3. Hoàn thiện 3 skill cho tất cả hệ + thêm cooldown_sec vào skill cũ
source GameServerApi/migration_complete_skills.sql
```

> ⚠️ `migration_complete_skills.sql` dùng `UPDATE` — nếu chạy lại sẽ ghi đè, **không tạo trùng**.

---

## Skill per element

| Element | Skill 1 | Skill 2 | Skill 3 |
|---------|---------|---------|---------|
| **Fire** | FIRE_BALL (Cầu Lửa) — Projectile, trigger `Skill1` | FIRE_WAVE (Sóng Lửa) — Projectile AoE, trigger `Skill2` | FIRE_BURST (Bùng Lửa Cận) — **Melee**, trigger `Skill3` |
| **Water** | WATER_SHIELD (Khiên Nước) — **Melee/buff**, trigger `Skill1` | HEAL_WAVE (Sóng Hồi) — **Melee/heal**, trigger `Skill2` | WATER_SURGE (Lướt Sóng) — **WindStep**, trigger `Skill3` |
| **Earth** | EARTH_SMASH (Đập Đất) — Projectile AoE, trigger `Skill1` | EARTH_SHIELD (Khiên Đất) — **Melee/buff**, trigger `Skill2` | EARTH_SPIKE (Gai Đất) — Projectile xuyên, trigger `Skill3` |
| **Metal** | METAL_SLASH (Chém Thép) — Projectile, trigger `Skill1` | METAL_STORM (Bão Kim Loại) — Projectile multi, trigger `Skill2` | METAL_ARMOR (Giáp Thép) — **Melee/buff**, trigger `Skill3` |
| **Wood** | WOOD_VINE (Dây Leo Cây) — Projectile, trigger `Skill1` | WOOD_ARROW (Tên Gỗ) — Projectile, trigger `Skill2` | WOOD_HEAL (Thảo Dược Hồi) — **Melee/heal**, trigger `Skill3` |
| **Wind** | WIND_STRIKE (Chưởng Phong) — **Melee**, trigger `Skill1` | WIND_BLADE (Phong Nhận) — Projectile, trigger `Skill2` | WIND_STEP (Phong Thoái Bộ) — **WindStep**, trigger `Skill3` |
| Universal | DASH (Lướt Nhanh) — **Teleport** | — | — |

> **Quy tắc trigger Animator:** Tất cả skill dùng tên trigger chung `Skill1`, `Skill2`, `Skill3` trong Animator Controller của từng hệ. Mỗi hệ có controller riêng → clip riêng nhưng tên trigger giống nhau.

---

## Config Unity — Player Prefab

### Cấu trúc Hierarchy của player prefab (mỗi hệ)

```
[Root "Phong"]                   ← NetworkObject, PlayerController, NetworkTransform
├── Animator                     ← Skill_Phong.controller (animation nhân vật)
├── SpriteRenderer               ← Sprite nhân vật
├── PlayerSkillManager           ← Quản lý 3 SkillData (skills[0..2])
│   defaultSkillEffectObject ──────────────────────────────────────┐
├── SkillRuntimeLoader           ← Tự động load thống kê từ DB     │
├── WindStepSkill                ← (chỉ hệ có Skill3 dạng WindStep) │ trỏ vào
│   skillEffectObject ──────────────────────────────────────────────┘
│                                                                   │
├── SkillEffect              ◄──────────────────────────────────────┘
│   └── Animator             ← Skill_Phong.controller (animation hiệu ứng skill)
│
└── GroundCheck              ← kiểm tra chạm đất
```

> **Lưu ý:** Cả root "Phong" và child "SkillEffect" dùng chung `Skill_Phong.controller`. Mỗi GameObject có Animator instance riêng — root dùng cho animation nhân vật (idle/walk), child dùng cho hiệu ứng skill khi nhấn phím. Hai instance **không ảnh hưởng lẫn nhau**.

### Thêm SkillRuntimeLoader

1. Chọn player prefab → **Add Component → SkillRuntimeLoader**
2. Không cần gán gì thêm, nó tự tìm qua `GetComponent<PlayerSkillManager>()`.

### Config `PlayerSkillManager` — 3 SkillData cho **mỗi hệ**

> **Xem hướng dẫn step-by-step chi tiết cho hệ Phong ở mục [Hướng dẫn chi tiết hệ Phong](#hướng-dẫn-chi-tiết-hệ-phong) bên dưới.**

**Bảng tóm tắt hệ Phong (Wind):**

#### Skill 0 — Chưởng Phong (Melee)
| Field | Giá trị |
|-------|---------|
| `skillName` | `Chưởng Phong` |
| **`skillCode`** | **`WIND_STRIKE`** |
| `skillType` | **Melee** |
| `activationKey` | `J` (106) |
| `cooldown` | `3` *(sẽ bị ghi đè bởi DB)* |
| `animationTriggerName` | `Skill1` |
| `playerSkillEffectObject` | *(None — dùng defaultSkillEffectObject)* |

#### Skill 1 — Phong Nhận (Projectile) — KHÔNG phải Melee
| Field | Giá trị |
|-------|---------|
| `skillName` | `Phong Nhận` |
| **`skillCode`** | **`WIND_BLADE`** |
| `skillType` | **Projectile** ← đây là loại bắn đạn |
| `activationKey` | `K` (107) |
| `cooldown` | `4` |
| `projectilePrefab` | WindBlade prefab (tạm dùng Fireball để test) |
| `projectileSpeed` | `12` |
| `spawnOffset` | `0.6` |
| `projectileLifetime` | `2` |
| `animationTriggerName` | `Skill2` |
| `playerSkillEffectObject` | *(None — dùng default)* |

#### Skill 2 — Phong Thoái Bộ (WindStep)
| Field | Giá trị |
|-------|---------|
| `skillName` | `Phong Thoái Bộ` |
| **`skillCode`** | **`WIND_STEP`** |
| `skillType` | **WindStep** |
| `activationKey` | `L` (108) |
| `cooldown` | `8` *(phải khớp WindStepSkill.cooldown)* |
| `animationTriggerName` | `Skill3` *(sau khi sửa typo trong controller)* |

**Template config cho hệ Lửa (Fire):**

| Skill | `skillCode` | `skillType` | Key | `animationTriggerName` |
|-------|-------------|-------------|-----|----------------------|
| Cầu Lửa | `FIRE_BALL` | Projectile | Z | `Skill1` |
| Sóng Lửa | `FIRE_WAVE` | Projectile | X | `Skill2` |
| Bùng Lửa Cận | `FIRE_BURST` | Melee | C | `Skill3` |

**Template config cho hệ Nước (Water):**

| Skill | `skillCode` | `skillType` | Key | `animationTriggerName` |
|-------|-------------|-------------|-----|----------------------|
| Khiên Nước | `WATER_SHIELD` | Melee | Z | `Skill1` |
| Sóng Hồi | `HEAL_WAVE` | Melee | X | `Skill2` |
| Lướt Sóng | `WATER_SURGE` | WindStep | C | `Skill3` |

**Template config cho hệ Đất (Earth):**

| Skill | `skillCode` | `skillType` | Key | `animationTriggerName` |
|-------|-------------|-------------|-----|----------------------|
| Đập Đất | `EARTH_SMASH` | Projectile | Z | `Skill1` |
| Khiên Đất | `EARTH_SHIELD` | Melee | X | `Skill2` |
| Gai Đất | `EARTH_SPIKE` | Projectile | C | `Skill3` |

**Template config cho hệ Kim (Metal):**

| Skill | `skillCode` | `skillType` | Key | `animationTriggerName` |
|-------|-------------|-------------|-----|----------------------|
| Chém Thép | `METAL_SLASH` | Projectile | Z | `Skill1` |
| Bão Kim Loại | `METAL_STORM` | Projectile | X | `Skill2` |
| Giáp Thép | `METAL_ARMOR` | Melee | C | `Skill3` |

**Template config cho hệ Mộc (Wood):**

| Skill | `skillCode` | `skillType` | Key | `animationTriggerName` |
|-------|-------------|-------------|-----|----------------------|
| Dây Leo Cây | `WOOD_VINE` | Projectile | Z | `Skill1` |
| Tên Gỗ | `WOOD_ARROW` | Projectile | X | `Skill2` |
| Thảo Dược Hồi | `WOOD_HEAL` | Melee | C | `Skill3` |

---

## Config Animator Controller (SkillEffect)

Mỗi hệ cần 1 Animator Controller riêng (VD: `Skill_Phong.controller`, `Skill_Fire.controller`...). Tuy nhiên **cấu trúc state machine giống nhau**, chỉ khác animation clip.

### Cấu trúc chuẩn Animator

```
Entry ──→ Idle (empty / loop sprite mặc định)
            │
Any State ──→ skill1_state   [Trigger: Skill1]  ──→ [exit time] ──→ Idle
Any State ──→ skill2_state   [Trigger: Skill2]  ──→ [exit time] ──→ Idle
Any State ──→ skill3_state   [Trigger: Skill3]  ──→ [exit time] ──→ Idle
```

### Chi tiết transition

| Transition | Condition | Has Exit Time | Exit Time | Transition Duration |
|-----------|-----------|--------------|-----------|-------------------|
| Idle → skill1_state | Trigger `Skill1` | No | — | 0 |
| skill1_state → Idle | — | **Yes** | 1.0 (hết clip) | 0 |
| Idle → skill2_state | Trigger `Skill2` | No | — | 0 |
| skill2_state → Idle | — | **Yes** | 1.0 | 0 |
| Idle → skill3_state | Trigger `Skill3` | No | — | 0 |
| skill3_state → Idle | — | **Yes** | 1.0 | 0 |

> **Lưu ý cho Skill3 dạng WindStep:** Độ dài clip `skill3` trong Animator phải **bằng đúng** giá trị `Animation Duration` trong `WindStepSkill` component. Nếu clip = 0.8s thì set `animationDuration = 0.8`.

### Từng hệ — Animation Clips cần có

| Hệ | Skill 1 clip | Skill 2 clip | Skill 3 clip |
|----|-------------|-------------|-------------|
| Wind | `wind_strike` (đánh cận) | `wind_blade` (tung lưỡi) | `wind_step` (fade out/in) |
| Fire | `fire_ball` (ném cầu lửa) | `fire_wave` (sóng lửa) | `fire_burst` (nổ AoE) |
| Water | `water_shield` (giơ khiên) | `heal_wave` (sóng hồi) | `water_surge` (lướt sóng) |
| Earth | `earth_smash` (đập) | `earth_shield` (khiên đất) | `earth_spike` (gai trồi) |
| Metal | `metal_slash` (chém) | `metal_storm` (bão kim) | `metal_armor` (mặc giáp) |
| Wood | `wood_vine` (dây leo) | `wood_arrow` (bắn tên) | `wood_heal` (hồi máu) |

---

## Config Hotbar UI + Cooldown hiển thị

### Cấu trúc Canvas chuẩn

```
Canvas
└── SkillHotbar                  ← SkillHotbarUI.cs
    ├── Slot0 (SkillSlotUI)      ← Skill 1 (phím Z)
    │   ├── IconImage            ← Image — icon skill
    │   ├── CooldownOverlay      ← Image (Filled / Radial360) — overlay khi CD
    │   └── CooldownText         ← TMP_Text — "2.4s" — tự ẩn khi sẵn sàng
    ├── Slot1 (SkillSlotUI)      ← Skill 2 (phím X)
    │   ├── IconImage
    │   ├── CooldownOverlay
    │   └── CooldownText
    └── Slot2 (SkillSlotUI)      ← Skill 3 (phím C)
        ├── IconImage
        ├── CooldownOverlay
        └── CooldownText
```

### Setup từng Slot prefab

1. Tạo **Image** (background slot), thêm **Button** → đặt tên `Slot0`.
2. Tạo **Image** con → tên `IconImage`, gán ảnh icon skill.
3. Tạo **Image** con → tên `CooldownOverlay`:
   - Image Type: **Filled**
   - Fill Method: **Radial360**
   - Fill Origin: **Top**
   - Fill Amount: 0
   - Color: `(0,0,0,0.6)` (đen bán trong suốt)
4. Tạo **TMP_Text** con → tên `CooldownText`:
   - Alignment: Center Middle
   - Font Size: 14
   - Color: trắng

### Gán vào `SkillHotbarUI`

- **Slots** list → kéo thả 3 `SkillSlotUI` component (Slot0, Slot1, Slot2)
- **Skill Icons** list → kéo sprite icon theo đúng thứ tự skill trong PlayerSkillManager:
  - Index 0 → icon Skill 1
  - Index 1 → icon Skill 2
  - Index 2 → icon Skill 3

### Cooldown tự động hoạt động

`SkillSlotUI` trong Update() gọi:
- `SkillData.GetCooldownPercent()` → set `CooldownOverlay.fillAmount`
- `SkillData.GetCooldownRemaining()` → set `CooldownText` (VD: `"2.4s"`)

Không cần config thêm gì.

---

## Load stats từ DB tự động (SkillRuntimeLoader)

### Cách hoạt động

**Khi StartHost hoặc Client join:**
1. `SkillRuntimeLoader.OnNetworkSpawn()` chạy (chỉ trên owner).
2. Đọc `player_id` từ `GameManager.Instance.currentPlayerData.player_id`.
3. Gọi `APIClient.GetPlayerSkills(playerId, ...)`.
4. API trả về `current_cooldown_sec`, `current_effect_value`, `current_mp_cost` cho mỗi skill.
5. Loader tìm `SkillData` có `skillCode` khớp (VD: `WIND_STRIKE`), ghi đè giá trị.
6. SkillHotbarUI tự động reflect cooldown mới vì nó đọc từ `SkillData`.

### Điều kiện cần đảm bảo

- `SkillData.skillCode` phải được điền (**bắt buộc**) — xem bảng template trên.
- `GameManager.Instance.currentPlayerData` phải có `player_id` trước khi player spawn.
- Nếu player chưa học skill đó (`current_level = 0`), loader sẽ **bỏ qua** (dùng giá trị Inspector).

### Debug

```
[SkillRuntimeLoader] Applied 'WIND_STRIKE' lv1: CD=3s EV=18 MP=8
[SkillRuntimeLoader] Applied 'WIND_BLADE' lv1: CD=4s EV=25 MP=12
[SkillRuntimeLoader] Load xong: 3/3 skill đã apply từ DB.
```

---

## Hướng dẫn chi tiết hệ Phong

> **Prefab đã có sẵn** — không cần tạo mới. Mở `Assets/Prefabs/Player/He/Phong.prefab`.

---

### Hiểu cấu trúc thực tế của Phong prefab

```
Phong.prefab  (Assets/Prefabs/Player/He/Phong.prefab)
│
├── [Root GameObject "Phong"]
│   ├── Animator        → Skill_Phong.controller  ✅ đã gán (animation nhân vật)
│   ├── SpriteRenderer  → sprite nhân vật
│   ├── PlayerSkillManager  (đang chưa đầy đủ — xem bên dưới)
│   ├── WindStepSkill       (cần thêm / config)
│   ├── SkillRuntimeLoader  (cần thêm)
│   └── ... (các component khác)
│
├── SkillEffect  ← CHILD OBJECT — dùng cho animation hiệu ứng skill
│   └── Animator  ← ⚠️  m_Controller = NONE — CHƯA GÁN CONTROLLER!
│                         Cần gán Skill_Phong.controller vào đây
│
└── GroundCheck
```

> ⚠️ **PHÂN BIỆT quan trọng — 2 thứ tên gần giống nhau:**
>
> | | Là cái gì | Dùng cho |
> |---|---|---|
> | `SkillEffect` | **Child object** bên trong `Phong.prefab` | `playerSkillEffectObject` / `WindStepSkill.skillEffectObject` |
> | `SkillEffect_Phong.prefab` | **Prefab standalone** ở `Assets/Prefabs/Projectile/` | Projectile/Dash effect (DashComponent) — **KHÔNG DÙNG ở đây** |
>
> Khi .md nói "drag `SkillEffect`" nghĩa là kéo **child object** tên `SkillEffect` bên trong Hierarchy của Phong prefab — **không phải** file prefab trong Projectile folder.

---

### ⚠️ Lỗi typo trong Skill_Phong.controller — đọc trước khi config

Mở `Assets/Animations/Skills/Skill_Phong.controller` → Animator window → tab **Parameters**:

| Trigger hiện tại | Đúng phải là | Trạng thái |
|-----------------|--------------|-----------|
| `Skill1` | `Skill1` | ✅ đúng |
| `Skill2` | `Skill2` | ✅ đúng |
| `Skil3` | `Skill3` | ❌ **thiếu chữ 'l'** — typo! |

**Cần sửa ngay trong Animator window:**
1. Window → Animation → Animator → chọn controller `Skill_Phong`
2. Tab **Parameters** → double-click `Skil3` → đổi tên thành `Skill3`
3. Ctrl+S để save

> Nếu không sửa, `animationTriggerName = "Skill3"` sẽ không kích hoạt animation Skill3. Bạn có thể dùng `"Skil3"` làm workaround, nhưng không khuyến nghị.

---

### Bước 1 — Gán Animator Controller cho child `SkillEffect`

1. Trong Project panel, double-click `Assets/Prefabs/Player/He/Phong.prefab` để mở Prefab mode.
2. Trong Hierarchy của prefab mode, chọn child **`SkillEffect`** (không phải root "Phong").
3. Inspector → component **Animator** → field **Controller** đang để trống (`None`).
4. Kéo `Assets/Animations/Skills/Skill_Phong.controller` từ Project vào field **Controller**.
5. Lưu prefab (Ctrl+S hoặc nút **Save** ở đầu Hierarchy).

**Sau bước này:** `SkillEffect` child đã có Animator hoạt động với 4 states: `New State` (idle), `skill 1`, `skill 2`, `skill 3`.

---

### Bước 2 — Gán `Default Skill Effect Object` trên PlayerSkillManager

Thay vì gán riêng từng skill, chỉ gán một lần vào field chung:

1. Trong Prefab mode, chọn root **`Phong`**.
2. Inspector → component **PlayerSkillManager** → field **Default Skill Effect Object** đang `None`.
3. Từ Hierarchy, kéo child **`SkillEffect`** vào field **Default Skill Effect Object**.

> Tất cả 3 skill sẽ dùng chung `SkillEffect` này. Field `playerSkillEffectObject` trong từng SkillData **có thể để None** (code tự fallback về `defaultSkillEffectObject`).

---

### Bước 3 — Config Skill 0: Chưởng Phong (Melee)

Trong PlayerSkillManager Inspector → **Skills** list → **Element 0**:

| Field | Giá trị | Ghi chú |
|-------|---------|---------|
| `Skill Name` | `Chưởng Phong` | Tên hiển thị UI |
| `Skill Code` | `WIND_STRIKE` | **Bắt buộc** — dùng để load stats từ DB |
| `Skill Type` | **Melee** (index 2) | Tấn công gần, không bắn projectile |
| `Activation Key` | `J` (KeyCode 106) | Phím kích hoạt |
| `Cooldown` | `3` | Giá trị tạm — sẽ bị SkillRuntimeLoader ghi đè từ DB |
| `Animation Trigger Name` | `Skill1` | Trigger trong Skill_Phong.controller |
| `Player Skill Effect Object` | *(để None)* | Dùng defaultSkillEffectObject đã gán ở Bước 2 |

---

### Bước 4 — Config Skill 1: Phong Nhận (Projectile)

> **WIND_BLADE là Projectile — KHÔNG phải Melee.** Skill này tung một lưỡi gió bay ngang theo hướng nhân vật.

Trong PlayerSkillManager Inspector → **Skills** list → **Element 1** (hiện đang trống):

| Field | Giá trị | Ghi chú |
|-------|---------|---------|
| `Skill Name` | `Phong Nhận` | |
| `Skill Code` | `WIND_BLADE` | **Bắt buộc** |
| `Skill Type` | **Projectile** (index 0) | Bắn đạn theo hướng |
| `Activation Key` | `K` (KeyCode 107) | |
| `Cooldown` | `4` | Tạm thời, DB ghi đè |
| `Projectile Prefab` | *(prefab lưỡi gió)* | Drag prefab projectile vào đây |
| `Projectile Speed` | `12` | Tốc độ bay |
| `Spawn Offset` | `0.6` | Khoảng cách spawn so với nhân vật |
| `Projectile Lifetime` | `2` | Thời gian tồn tại (giây) |
| `Animation Trigger Name` | `Skill2` | Trigger trong controller |
| `Player Skill Effect Object` | *(để None)* | Dùng default |

> **Về Projectile Prefab:** Nếu chưa có prefab lưỡi gió riêng, tạm thời dùng `FireballProjectile` để test hiệu ứng. Về sau tạo `WindBlade.prefab` với sprite lưỡi gió + Rigidbody2D + Collider2D + script `ProjectileController`.

---

### Bước 5 — Config Skill 2: Phong Thoái Bộ (WindStep)

Trong PlayerSkillManager Inspector → **Skills** list → **+ (thêm Element 2)**:

| Field | Giá trị | Ghi chú |
|-------|---------|---------|
| `Skill Name` | `Phong Thoái Bộ` | |
| `Skill Code` | `WIND_STEP` | **Bắt buộc** |
| `Skill Type` | **WindStep** (index 3) | Dash + ẩn thân |
| `Activation Key` | `L` (KeyCode 108) | |
| `Cooldown` | `8` | Phải khớp với `WindStepSkill.cooldown` |
| `Animation Trigger Name` | `Skill3` | Sau khi đã sửa typo ở trên. Nếu chưa sửa: dùng `Skil3` |
| `Player Skill Effect Object` | *(để None)* | |

---

### Bước 6 — Config component WindStepSkill

Nếu chưa có: root "Phong" → **Add Component → WindStepSkill**.

| Field | Giá trị | Ghi chú |
|-------|---------|---------|
| `Cooldown` | `8` | Phải bằng SkillData[2].cooldown |
| `Dash Distance` | `3` | Khoảng cách dash (unit). SkillRuntimeLoader sẽ ghi đè từ DB |
| `Dash Duration` | `0.2` | Thời gian di chuyển mượt (giây) |
| `Animation Duration` | `0.8` | **Phải bằng đúng độ dài clip `skill 3.anim`** |
| `Player Sprite Renderer` | Kéo **SpriteRenderer** của root "Phong" | Để ẩn sprite khi dash |
| `Skill Effect Object` | Kéo child **`SkillEffect`** | Object được kích hoạt/animate khi dash |
| `Check Collision` | `true` | Tránh xuyên tường |
| `Obstacle Layer Mask` | Layer `Wall` (hoặc tương đương) | |

> **Kiểm tra `Animation Duration`:**  
> Project panel → `Assets/Animations/Skills/Phong/skill 3.anim` → xem **Length** trong Inspector. Điền đúng giá trị đó vào `animationDuration`.

---

### Bước 7 — Thêm SkillRuntimeLoader

1. Chọn root **"Phong"** → **Add Component → SkillRuntimeLoader**.
2. Không cần config thêm gì — nó tự tìm `PlayerSkillManager` và `APIClient`.

---

### Bước 8 — Kiểm tra Animator Controller (Skill_Phong.controller)

Mở Animator window, chọn `SkillEffect` child → xác nhận các state sau đây TỒN TẠI:

| State | Animation Clip | Motion file |
|-------|---------------|-------------|
| `New State` (default/idle) | *(empty)* | Không có clip |
| `skill 1` | `skill 1.anim` | `Assets/Animations/Skills/Phong/skill 1.anim` ✅ |
| `skill 2` | `skill 2.anim` | `Assets/Animations/Skills/Phong/skill 2.anim` ✅ |
| `skill 3` | `skill 3.anim` | `Assets/Animations/Skills/Phong/skill 3.anim` ✅ |

Tất cả 4 clip đã tồn tại sẵn. Chỉ cần verify xem chúng đã được assign đúng trong controller.

**Transitions (gốc của controller):**

| Từ | Đến | Condition | Has Exit Time | Exit Time |
|----|-----|-----------|--------------|-----------|
| `New State` | `skill 1` | Trigger `Skill1` | Yes | 0.75 |
| `New State` | `skill 2` | Trigger `Skill2` | Yes | 0.75 |
| `New State` | `skill 3` | Trigger `Skill3` | Yes | 0.75 |

> Nếu muốn animation phản hồi ngay lập tức (không chờ 75% idle): đổi `Has Exit Time = false` trên từng transition.

---

### Bước 9 — Kiểm tra kết quả cuối

Sau khi hoàn thành tất cả bước, Inspector của **PlayerSkillManager** trên Phong prefab phải trông như sau:

```
PlayerSkillManager
├── Skills (Size = 3)
│   ├── Element 0
│   │   ├── Skill Name: "Chưởng Phong"
│   │   ├── Skill Code: "WIND_STRIKE"
│   │   ├── Skill Type: Melee
│   │   ├── Activation Key: J (106)
│   │   ├── Cooldown: 3
│   │   ├── Animation Trigger Name: "Skill1"
│   │   └── Player Skill Effect Object: None (dùng default)
│   │
│   ├── Element 1
│   │   ├── Skill Name: "Phong Nhận"
│   │   ├── Skill Code: "WIND_BLADE"
│   │   ├── Skill Type: Projectile
│   │   ├── Activation Key: K (107)
│   │   ├── Cooldown: 4
│   │   ├── Projectile Prefab: [WindBlade hoặc Fireball tạm]
│   │   ├── Projectile Speed: 12
│   │   ├── Animation Trigger Name: "Skill2"
│   │   └── Player Skill Effect Object: None
│   │
│   └── Element 2
│       ├── Skill Name: "Phong Thoái Bộ"
│       ├── Skill Code: "WIND_STEP"
│       ├── Skill Type: WindStep
│       ├── Activation Key: L (108)
│       ├── Cooldown: 8
│       ├── Animation Trigger Name: "Skill3"
│       └── Player Skill Effect Object: None
│
└── Default Skill Effect Object: [SkillEffect] ← đã kéo child vào đây ✅
```

```
WindStepSkill
├── Cooldown: 8
├── Dash Distance: 3
├── Dash Duration: 0.2
├── Animation Duration: 0.8
├── Player Sprite Renderer: [Phong/SpriteRenderer] ✅
├── Skill Effect Object: [Phong/SkillEffect] ✅
└── Check Collision: true
```

---

### Bước 10 — Đăng ký prefab với NetworkManager

1. Project → `Assets/` → mở `DefaultNetworkPrefabs.asset`
2. Thêm `Phong.prefab` vào danh sách **Network Prefabs** (nếu chưa có).
3. Trong `NetworkPlayerSpawner`, map element_type `"Wind"` → prefab `Phong`.

---

## Thêm hệ mới / thêm skill thứ 4

### Thêm skill thứ 4 (DB)

```sql
INSERT INTO skill_template
  (skill_code, skill_name, description, element_type, max_level, level_to_unlock, levels_json, icon_id, created_at)
VALUES
('WIND_CYCLONE','Lốc Xoáy','Tạo lốc xoáy quét diện rộng.','Wind',5,10,
 '[{"level_req":10,"sp_cost":2,"effect_value":60,"mp_cost":25,"cooldown_sec":12.0,"desc":"Gây 60 ST AoE"},...]',
 'icon_wind_4',NOW());
```

### Thêm skill thứ 4 (Unity)

1. Trong `PlayerSkillManager` Inspector → Skills List → `+` thêm element mới.
2. Điền `skillCode = WIND_CYCLONE`, `skillType = Melee`, `activationKey = V`.
3. Trong `SkillHotbarUI` → thêm `Slot3` prefab vào `slots` list.
4. Thêm icon vào `skillIcons` list tương ứng.

### Thêm hệ mới (VD: hệ Băng — Ice)

1. **DB:** INSERT 3 skill với `element_type = 'Ice'` vào `skill_template`.
2. **Animator:** Tạo `Skill_Ice.controller` với 3 trigger Skill1/Skill2/Skill3.
3. **Prefab:** Duplicate từ player hiện có, đổi sprite, Animator, SkillEffect.
4. **PlayerSkillManager:** Config 3 SkillData với đúng `skillCode` Ice.
5. **NetworkPlayerSpawner:** Map `"Ice"` → prefab mới.

---

## Cấu trúc levels_json

```json
[
  {
    "level_req":    1,      // Cấp player cần để mở level này
    "sp_cost":      1,      // Skill point tiêu khi bấm nâng cấp
    "effect_value": 18.0,   // Sát thương / heal / khoảng cách di chuyển (đơn vị tùy skill)
    "mp_cost":      8,      // MP tiêu khi kích hoạt skill
    "cooldown_sec": 3.0,    // Cooldown (giây) ở level này — client apply vào SkillData.cooldown
    "desc":         "Gây 18 ST" // Mô tả hiển thị trong UI skill panel
  },
  { ... }  // Max 5 phần tử (max_level = 5)
]
```

> **Index logic:** `levels_json[0]` = level 1, `levels_json[1]` = level 2, ..., `levels_json[n-1]` = level n.  
> API đọc `levels_json[current_level - 1]` để lấy stats hiện tại.

---

## Luồng dữ liệu đầy đủ

```
[Login] → JWT Token + player_id stored
            │
[StartHost / Connect] → NetworkManager.StartHost()
            │
[Player Spawn] → NetworkPlayer_[Element] spawn
            │
[NetworkPlayerDataSync.OnNetworkSpawn()]
   └── Server: LoadPlayerDataFromGameManager()
       → Set networkElementType, networkLevel, etc.
            │
[SkillRuntimeLoader.OnNetworkSpawn()]   ← chạy trên Owner
   └── Gọi APIClient.GetPlayerSkills(player_id)
       → Server trả về: current_cooldown_sec, current_effect_value, current_mp_cost
       → Apply vào SkillData[i] theo skillCode
       → Apply vào WindStepSkill.cooldown / TeleportSkill.cooldown nếu cần
            │
[Game loop]
   ├── Player nhấn Z/X/C hoặc touch Slot0/1/2
   ├── PlayerSkillManager.HandleSkillInput()
   │   └── UseSkill(skill) → Melee | Projectile | WindStep | Teleport
   │
   └── SkillSlotUI.Update()
       ├── CooldownOverlay.fillAmount = skill.GetCooldownPercent() → visual countdown pie
       └── CooldownText.text = skill.GetCooldownRemaining().ToString("F1") + "s"
```

---

## Config nút bấm mobile (Touch Buttons)

Nếu game có cả bàn phím lẫn touch buttons:

### Option A — Button gọi `TryUseSkillByIndex()`

Gán vào `OnClick` của Button:
```
PlayerSkillManager.TryUseSkillByIndex(0)   // Slot 0 → Skill 1
PlayerSkillManager.TryUseSkillByIndex(1)   // Slot 1 → Skill 2
PlayerSkillManager.TryUseSkillByIndex(2)   // Slot 2 → Skill 3
```

> `SkillSlotUI` đã có `skillButton` và tự gọi `TryUseSkillByIndex(slotIndex)` trong `Bind()`.  
> Chỉ cần gán Button reference vào `SkillSlotUI.skillButton`.

### Option B — Keyboard keys

| Phím | Skill |
|------|-------|
| `Z` | Skill 1 (damage/melee) |
| `X` | Skill 2 (projectile) |
| `C` | Skill 3 (special) |

Key được set trong `SkillData.activationKey` — thay đổi trong Inspector.
