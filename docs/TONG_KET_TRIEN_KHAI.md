# TỔNG KẾT TRIỂN KHAI DỰ ÁN GAME NGŨ HÀNH

> Cập nhật lần cuối: 16/03/2026

---

## MỤC LỤC

1. [Tổng quan dự án](#1-tổng-quan-dự-án)
2. [Kiến trúc hệ thống](#2-kiến-trúc-hệ-thống)
3. [Backend — GameServerApi](#3-backend--gameserverapi)
4. [Client — Unity](#4-client--unity)
5. [Hệ thống nhân vật (6 Nguyên Tố)](#5-hệ-thống-nhân-vật-6-nguyên-tố)
6. [Hệ thống Skill](#6-hệ-thống-skill)
7. [Hệ thống Gene & Nâng cấp](#7-hệ-thống-gene--nâng-cấp)
8. [Hệ thống Dungeon (Phó bản)](#8-hệ-thống-dungeon-phó-bản)
9. [Hệ thống Mạng (Multiplayer)](#9-hệ-thống-mạng-multiplayer)
10. [UI & HUD](#10-ui--hud)
11. [Cơ sở dữ liệu](#11-cơ-sở-dữ-liệu)
12. [Danh sách Scenes](#12-danh-sách-scenes)
13. [Tình trạng triển khai tổng thể](#13-tình-trạng-triển-khai-tổng-thể)

---

## 1. Tổng quan dự án

Game hành động 2D multiplayer, xây dựng theo chủ đề **Ngũ Hành** (Kim - Mộc - Thủy - Hỏa - Thổ + Phong). Người chơi chọn một trong 6 nguyên tố để tạo nhân vật, mỗi nguyên tố có bộ 3 skill riêng và cơ chế tương khắc lẫn nhau.

| Thành phần | Công nghệ |
|---|---|
| Game Engine | Unity (2D, Netcode for GameObjects) |
| Backend API | ASP.NET Core (C#) |
| Database | MySQL |
| Multiplayer | Unity Netcode (Host/Client) |
| Auth | JWT Bearer Token |

---

## 2. Kiến trúc hệ thống

```
┌─────────────────────────────────────────────┐
│              Unity Client                   │
│  Scenes: Login → Register → SelectElement  │
│          → HostScene → GameScene           │
└──────────────────┬──────────────────────────┘
                   │ HTTP (REST API)
                   │ JWT Token
┌──────────────────▼──────────────────────────┐
│         GameServerApi (ASP.NET Core)        │
│  Controllers: Auth, Player, Enemy, Item,   │
│               Gene, Upgrade, Dungeon, Map  │
└──────────────────┬──────────────────────────┘
                   │ EF Core
┌──────────────────▼──────────────────────────┐
│              MySQL Database                 │
│  Tables: users, player_data, enemies,      │
│          skills, items, gene_*, dungeon_*  │
└─────────────────────────────────────────────┘
```

---

## 3. Backend — GameServerApi

### 3.1 Controllers đã triển khai

| Controller | Route | Chức năng |
|---|---|---|
| `AuthController` | `POST /api/auth/register` `POST /api/auth/login` | Đăng ký / đăng nhập, trả về JWT |
| `PlayerController` | `POST /api/player/create` `GET/PUT /api/player/{id}` | Tạo nhân vật, load/save dữ liệu |
| `EnemyController` | `GET /api/enemy` `GET /api/enemy/{id}` | Lấy dữ liệu enemy và spawn |
| `EnemySpawnController` | `GET /api/enemyspawn/{mapId}` | Lấy spawn config theo map |
| `ItemController` | `GET /api/item/templates` | Load danh sách item templates |
| `GeneController` | `GET /api/gene/config` `POST /api/gene/upgrade` `POST /api/gene/multi` `POST /api/gene/hybrid` | Nâng cấp gene, multi-gene, hybrid |
| `UpgradeController` | `GET /api/upgrade/config` `POST /api/upgrade/enhance` | Config và thực hiện nâng cấp trang bị |
| `DungeonController` | `GET /api/dungeon/list` `POST /api/dungeon/start` `POST /api/dungeon/complete` | Quản lý phó bản |
| `MapController` | `GET /api/map/{mapId}/config` | Lấy spawn points của map |

### 3.2 DbContext — Các bảng đã đăng ký

| Bảng (Entity) | Mô tả |
|---|---|
| `users` | Tài khoản người dùng (username, email, password_hash, JWT) |
| `player_data` | Dữ liệu nhân vật (stats, equipment, inventory, skills — JSON) |
| `exp_requirements` | Bảng EXP yêu cầu theo level + stat tăng khi lên cấp |
| `map_configs` | Cấu hình map + spawn points (JSON) |
| `enemies` | Thông tin enemy (HP, damage, drop items, element_type) |
| `enemy_spawns` | Spawn config của enemy theo map |
| `item_templates` | Template tất cả items trong game |
| `skill_templates` | Template tất cả skills, stats theo level |
| `equipment_upgrade_configs` | Config nâng cấp trang bị theo bậc |
| `gene_upgrade_configs` | Config nâng cấp gene theo tier và element |
| `gene_tier_stat_configs` | Chỉ số stat tăng theo tier gene (đọc từ DB) |
| `dungeon_config` | Cấu hình phó bản (solo/multi, level yêu cầu) |
| `dungeon_sessions` | Phiên phó bản đang diễn ra |
| `gene_multi_configs` | Config multi-gene (2 gene kết hợp) |
| `gene_hybrid_configs` | Config hybrid gene (gene lai cao cấp) |

### 3.3 Authentication

- Đăng ký: hash password (hiện tại plain — **chưa dùng bcrypt**), tạo player_data mặc định
- Đăng nhập: trả về JWT (HS256), chứa `user_id` và `player_id` trong claims
- Client gửi token qua `Authorization: Bearer <token>` header

---

## 4. Client — Unity

### 4.1 Cấu trúc Scripts

```
Assets/Scripts/
├── Player/
│   ├── Controllers/       PlayerController, PlayerMovement, PlayerDash
│   ├── Combat/            PlayerCombat, PlayerHealth, PlayerSkillManager
│   ├── Skills/            SkillData, SkillRuntimeLoader + tất cả SkillComponent
│   ├── Animation/         (animator helpers)
│   ├── Visuals/           BuffOutlineVisual (hiệu ứng buff màu)
│   └── PlayerStats.cs
├── Enemy/
│   ├── EnemyAI, EnemyHealth, EnemyItemDrop
│   ├── EnemyProjectile, EnemyPrefabManager
├── Network/
│   ├── Auth/              ClientAuthHandler, ClientAuthSender, ServerConnectionApproval
│   ├── Player/            NetworkPlayerController, NetworkPlayerHealth, NetworkPlayerSpawner, PlayerPositionUpdater...
│   ├── Enemy/             NetworkEnemyController, NetworkEnemyHealth, NetworkEnemySpawner
│   ├── Dungeon/           DungeonManager, DungeonNetworkBridge
│   ├── Managers/          NetworkManagerController, NetworkManagerCustom, NetworkPrefabRegistrar
│   └── Bootstrap/
├── UI/
│   ├── Auth/              LoginController, RegisterController, ConnectionUI
│   ├── Menu/              MainMenuController, SelectElementController
│   ├── HUD/               GameUI, HealthBar, MpBar, PlayerInfoUI, SkillHotbarUI, EnemyHealthBar...
│   └── Character/         CharacterPanelController, StatsTabUI, SkillTabUI, PotentialTabUI...
├── Inventory/
│   └── Managers/          ItemManager, ItemTemplateManager, IconDatabase
├── Services/
│   └── Api/, Connection/, Player/
└── Map/                   MapManager
```

### 4.2 Player Prefabs

| Prefab | Element | Vị trí |
|---|---|---|
| `Hoa.prefab` | Hỏa (Fire) | `Prefabs/Player/He/` |
| `Kim.prefab` | Kim (Metal) | `Prefabs/Player/He/` |
| `Moc.prefab` | Mộc (Wood) | `Prefabs/Player/He/` |
| `Phong.prefab` | Phong (Wind) | `Prefabs/Player/He/` |
| `Tho.prefab` | Thổ (Earth) | `Prefabs/Player/He/` |
| `Thuy.prefab` | Thủy (Water) | `Prefabs/Player/He/` |
| `NetworkPlayer.prefab` | Chung (shared network) | `Prefabs/Player/` |
| `EarthPrefab`, `FirePrefab`, `WaterPrefab`, `MetalPrefab`, `WoodPrefab` | Element root | `Prefabs/Player/` |

---

## 5. Hệ thống nhân vật (6 Nguyên Tố)

### Thông tin nhân vật

Mỗi nhân vật lưu trong cột JSON `info_char` của bảng `player_data`:

```json
{
  "level": 1,
  "exp": 0,
  "hp": 100,
  "mp": 50,
  "attack_damage": 10,
  "defense": 5,
  "move_speed": 5,
  "element_type": "Fire",
  "gene_tier": 1,
  "gold": 0,
  "skill_points": 0,
  "potential_points": 0
}
```

### Hệ tương khắc Ngũ Hành

| Hệ tấn công | →Lợi thế x1.5 | →Bất lợi x0.75 |
|---|---|---|
| Kim | Khắc Mộc | Bị Thổ khắc |
| Mộc | Khắc Thủy | Bị Kim khắc |
| Thủy | Khắc Hỏa | Bị Mộc khắc |
| Hỏa | Khắc Thổ | Bị Thủy khắc |
| Thổ | Khắc Kim | Bị Hỏa khắc |

### Stats per Level

Dữ liệu tăng chỉ số khi lên cấp đọc từ bảng `exp_requirements` (JSON `base_stat_increase`). Mỗi level có thể thưởng thêm `skill_points` và `potential_points`.

---

## 6. Hệ thống Skill

### 6.1 SkillType enum (đã triển khai)

| SkillType | Script xử lý | Mô tả |
|---|---|---|
| `Projectile` | `PlayerSkillManager` | Bắn đạn theo hướng nhân vật |
| `Teleport` | `TeleportSkill` | Dịch chuyển tức thời |
| `Melee` | `PlayerSkillManager` | Cận chiến (trigger animation tại chỗ) |
| `WindStep` | `WindStepSkill` | Ẩn thân + dash |
| `MetalShield` | `MetalShieldSkill` | Khiên bất tử, miễn damage + xóa projectile |
| `WaterPillar` | `WaterPillarSkill` | Cây thánh rơi từ trên trời xuống |
| `WaterArmorBuff` | `WaterArmorBuffSkill` | Buff giáp cho bản thân và đồng đội |
| `FireRain` | `FireRainSkill` | Mưa lửa AoE từ trên trời |
| `EarthAura` | `EarthAttackBuffSkill` | Buff tấn công cho bản thân và đồng đội |
| `EarthBoomerang` | `EarthBoomerangSkill` | Bắn đạn boomerang quay về |
| `EarthBlinkStrike` | `EarthBlinkStrikeSkill` | Dịch chuyển + DoT projectile |

### 6.2 Skills theo từng Nguyên Tố

#### Hỏa (Fire) — phím J/K/L
| Skill | Tên | Loại | Projectile prefab |
|---|---|---|---|
| Skill 1 (J) | Hỏa Đạn | Projectile | `FIREBOLT.prefab` |
| Skill 2 (K) | Hỏa Bùng Nổ | Projectile | `FIREBURST.prefab` |
| Skill 3 (L) | Thiên Hỏa | FireRain | `FIRE_RAIN.prefab` |

#### Phong (Wind) — J/K/L
| Skill | Tên | Loại |
|---|---|---|
| Skill 1 (J) | ? | Projectile |
| Skill 2 (K) | ? | WindStep |
| Skill 3 (L) | ? | Teleport |

#### Thủy (Water) — J/K/L
| Skill | Tên | Loại | Projectile prefab |
|---|---|---|---|
| Skill 1 (J) | Thủy Đạn | Projectile | `WaterBoltProjectile.prefab` |
| Skill 2 (K) | Thánh Mộc Hạ | WaterPillar | `WaterPillarProjectile.prefab` |
| Skill 3 (L) | Thủy Giáp Hộ Thể | WaterArmorBuff | — (buff AoE) |

#### Thổ (Earth) — J/K/L
| Skill | Tên | Loại |
|---|---|---|
| Skill 1 (J) | ? | EarthAura |
| Skill 2 (K) | Địa Phong Đao | EarthBoomerang |
| Skill 3 (L) | Địa Độn Thuật | EarthBlinkStrike |

#### Kim (Metal) — J/K/L
| Skill | Tên | Loại |
|---|---|---|
| Skill 1 (J) | ? | Projectile |
| Skill 2 (K) | ? | MetalShield |
| Skill 3 (L) | ? | ? |

#### Mộc (Wood) — J/K/L
| Skill | Tên | Loại |
|---|---|---|
| Skill 1–3 | (DoT, Hồi phục, Buff) | DotDamage / Melee |

### 6.3 Stat Skill từ DB

- Script `SkillRuntimeLoader` chạy sau `StartHost`, gọi API `GET /api/player/{id}/skills` để load `damage`, `mp_cost`, `cooldown` cho từng skill tại level hiện tại.
- Dữ liệu skill lưu trong bảng `skill_templates` với các cột per-level.

### 6.4 Projectile Prefabs đã tạo

| Prefab | Dùng cho |
|---|---|
| `FireballProjectile.prefab` | Hệ Hỏa cơ bản |
| `FIREBOLT.prefab` | Hỏa — Skill 1 |
| `FIREBURST.prefab` | Hỏa — Skill 2 |
| `FIRE_RAIN.prefab` | Hỏa — Skill 3 (mưa lửa) |
| `WaterBoltProjectile.prefab` | Thủy — Skill 1 |
| `WaterPillarProjectile.prefab` | Thủy — Skill 2 (cây rơi) |
| `SkillEffect_Phong.prefab` | Phong — hiệu ứng skill |

### 6.5 AnimatorControllers Skill

| File | Dùng cho |
|---|---|
| `Skill_Phong.controller` | Base controller (các hệ khác override) |
| `Skill_Hoa.overrideController` | 3 clip hệ Hỏa (skill 1_1/2/3.anim) |
| `Skill_Kim.overrideController` | 3 clip hệ Kim |
| `Skill_Tho.overrideController` | 3 clip hệ Thổ (skill 3_1/2/3.anim) |
| `Skill_Thuy.overrideController` / `Skill_Thuy.controller` | 3 clip hệ Thủy (skill 4_1/2/3.anim) |

---

## 7. Hệ thống Gene & Nâng cấp

### 7.1 Gene System

- **5 loại Gene**: Kim, Mộc, Thủy, Hỏa, Thổ
- **4 Tier**: Tier 1 → 2 → 3 → 4 (mỗi tier mạnh hơn, tốn nhiều nguyên liệu hơn)
- **Stat boost** theo tier đọc từ bảng `gene_tier_stat_config` (không hardcode)
- **Multi-Gene**: Kết hợp 2 gene cùng loại để tăng hiệu quả (bảng `gene_multi_configs`)
- **Hybrid Gene**: Kết hợp 2 gene khác loại (bảng `gene_hybrid_configs`)

### API Gene
```
GET  /api/gene/config?elementType=Fire&tier=1   → config nâng cấp
POST /api/gene/upgrade                          → tiêu nguyên liệu, lên tier
POST /api/gene/multi                            → kết hợp multi-gene
POST /api/gene/hybrid                           → tạo hybrid gene
```

### 7.2 Equipment Upgrade (Nâng cấp Trang bị)

- **20 bậc** nâng cấp cho mỗi trang bị (+0 → +20)
- Option stats: Tấn công, Phòng thủ, HP, Tốc độ (unlock thêm ở bậc cao)
- Config đọc từ bảng `equipment_upgrade_configs` + hardcode option templates trong `UpgradeController`
- Dữ liệu trang bị lưu JSON trong `player_data.equipment`

---

## 8. Hệ thống Dungeon (Phó bản)

### 8.1 Config

Bảng `dungeon_config` quản lý:
- `dungeon_type`: `"solo"` hoặc `"multi"`
- `min_level_required`: level tối thiểu để vào
- `max_players`: số người tối đa
- `scene_name`: tên Scene Unity tương ứng

### 8.2 Flow phó bản

```
Client → GET /api/dungeon/list    →  Hiển thị danh sách phó bản
Client → POST /api/dungeon/start  →  Tạo DungeonSession, trả về session_id
[Game diễn ra trên Unity Network...]
Client → POST /api/dungeon/complete → Lưu kết quả, phát thưởng
```

### 8.3 Scripts Dungeon (Unity)

- `DungeonManager.cs` — quản lý flow phó bản phía client
- `DungeonNetworkBridge.cs` — cầu nối network cho multiplayer dungeon
- `DungeonListUI.cs`, `DungeonButtonItem.cs` — UI chọn phó bản

---

## 9. Hệ thống Mạng (Multiplayer)

Sử dụng **Unity Netcode for GameObjects** (NGO), cấu hình Host/Client:

### 9.1 Authentication Flow

```
Client → Connect → ServerConnectionApproval (kiểm tra token JWT)
Server → Approve/Deny
Client (nếu OK) → ClientAuthSender gửi player_id cho server
Server → ServerPlayerDataManager load player data
```

### 9.2 Network Scripts

| Script | Chức năng |
|---|---|
| `NetworkPlayerSpawner` | Spawn đúng prefab nhân vật theo element |
| `NetworkPlayerController` | Đồng bộ input/movement qua mạng |
| `NetworkPlayerHealth` | Đồng bộ HP, xử lý damage qua ServerRpc |
| `NetworkPlayerDataSync` | Sync dữ liệu player data |
| `NetworkEnemySpawner` | Spawn enemy có NetworkObject |
| `NetworkEnemyController` | Đồng bộ AI enemy |
| `NetworkEnemyHealth` | Đồng bộ HP enemy, xử lý chết |
| `NetworkManagerCustom` | Custom NetworkManager |
| `NetworkPrefabRegistrar` | Đăng ký prefab vào NetworkManager |
| `PlayerPositionUpdater` | Gửi vị trí lên server định kỳ |

### 9.3 Prefabs Network

- `NetworkManager.prefab` — Singleton quản lý NGO
- `AuthSenderNetworkObjectPrefab.prefab` — Object gửi auth token
- Tất cả Player prefabs và Projectile prefabs đều có `NetworkObject` Component

---

## 10. UI & HUD

### 10.1 In-game HUD

| Script | Chức năng |
|---|---|
| `HealthBar.cs` | Thanh HP player (đồng bộ mạng) |
| `MpBar.cs` | Thanh MP player |
| `SkillHotbarUI.cs`, `SkillSlotUI.cs` | Hiển thị 3 skill + cooldown timer |
| `PlayerInfoUI.cs` | Tên + level player |
| `EnemyHealthBar.cs`, `EnemyHealthBarSpawner.cs` | Thanh HP nổi trên enemy |
| `FlightMeter.cs` | (?) Đồng hồ bay/dash |
| `GameUI.cs` | Container tổng thể HUD |

### 10.2 Character Panel (Menu)

| Tab | Script | Chức năng |
|---|---|---|
| Stats | `StatsTabUI.cs` | Xem chỉ số nhân vật |
| Skills | `SkillTabUI.cs`, `SkillRowUI.cs` | Xem skill đang trang bị |
| Potential | `PotentialTabUI.cs`, `PotentialStatRowUI.cs` | Phân bổ điểm tiềm năng |

Toggle bằng `CharacterPanelController` + nút `CharacterPanelToggleButton`, `GeneUpgradePanelToggleButton`.

### 10.3 Flow Màn hình

```
Login.unity  →  Register.unity (nếu chưa có tài khoản)
Login.unity  →  SelectElement.unity (chọn nguyên tố lần đầu)
             →  HostScene.unity (chọn Host/Join)
             →  GameScene.unity (gameplay)
```

---

## 11. Cơ sở dữ liệu

### 11.1 File schema/migration

| File | Nội dung |
|---|---|
| `gamedb.sql` | Schema gốc toàn bộ database |
| `migration_thuy_skills.sql` | Skill hệ Thủy (skill_id 12, 13, 14) |
| `migration_tho_skills.sql` | Skill hệ Thổ |
| `migration_hoa_skills.sql` | Skill hệ Hỏa |
| `migration_kim_skills.sql` | Skill hệ Kim |
| `migration_wind_skills.sql` | Skill hệ Phong |
| `migration_multigene.sql` | Config multi-gene |
| `migration_gene_tier_stat_config.sql` | Stat boost theo gene tier |
| `migration_dungeon.sql` | Config phó bản |
| `migration_dungeon_testdata.sql` | Test data phó bản |
| `migration_fix_wind_blade.sql` | Fix skill Phong |
| `migration_complete_skills.sql` | Hoàn chỉnh bảng skills |

### 11.2 Các Skill ID trong DB

| Hệ | Skill 1 | Skill 2 | Skill 3 |
|---|---|---|---|
| Hỏa (Fire) | FIRE_BOLT | FIRE_BURST | FIRE_RAIN |
| Phong (Wind) | WIND_STEP / WIND_BLADE | WIND_DASH | WIND_STRIKE |
| Thủy (Water) | WATER_BOLT (12) | WATER_PILLAR (13) | WATER_ARMOR (14) |
| Thổ (Earth) | EARTH_AURA | EARTH_BOOMERANG | EARTH_BLINK |
| Kim (Metal) | KIM_* | KIM_SHIELD | KIM_* |
| Mộc (Wood) | MOC_* | MOC_* | MOC_* |

---

## 12. Danh sách Scenes

| Scene | Mô tả |
|---|---|
| `Login.unity` | Màn hình đăng nhập |
| `Register.unity` | Màn hình đăng ký |
| `SelectElement.unity` | Chọn nguyên tố (Kim/Mộc/Thủy/Hỏa/Thổ/Phong) lần đầu |
| `HostScene.unity` | Tạo phòng (Host) hoặc vào phòng (Join) |
| `GameScene.unity` | Màn hình gameplay chính |
| `1.unity` | Scene test / dev |

---

## 13. Tình trạng triển khai tổng thể

### ✅ Đã hoàn thành

| Hạng mục | Trạng thái |
|---|---|
| Backend API (Auth, Player, Enemy, Item, Map) | ✅ Hoàn thành |
| JWT Authentication | ✅ Hoàn thành |
| Hệ thống Skill — Hỏa (3 skills) | ✅ Hoàn thành |
| Hệ thống Skill — Phong (3 skills) | ✅ Hoàn thành |
| Hệ thống Skill — Thủy (3 skills: Thủy Đạn, Thánh Mộc Hạ, Thủy Giáp) | ✅ Hoàn thành |
| Hệ thống Skill — Thổ (3 skills: Địa Uy Khí, Địa Phong Đao, Địa Độn Thuật) | ✅ Hoàn thành |
| Hệ thống Skill — Kim (MetalShield + skills) | ✅ Hoàn thành |
| Hệ thống Skill — Mộc (DoT + heal) | ✅ Hoàn thành |
| Skill load stats từ DB (SkillRuntimeLoader) | ✅ Hoàn thành |
| Skill HUD (hotbar 3 slot + cooldown) | ✅ Hoàn thành |
| Hệ thống Gene (4 tier, 5 loại) | ✅ Hoàn thành |
| Multi-Gene & Hybrid Gene | ✅ Hoàn thành |
| Nâng cấp trang bị (20 bậc) | ✅ Hoàn thành |
| Character Panel (Stats/Skills/Potential) | ✅ Hoàn thành |
| Enemy AI + Health + Item Drop | ✅ Hoàn thành |
| Multiplayer Network (Host/Client, NGO) | ✅ Hoàn thành |
| Network Auth (JWT qua connection approval) | ✅ Hoàn thành |
| Enemy Network Sync (spawn/health/AI) | ✅ Hoàn thành |
| Player Network Sync (health, movement) | ✅ Hoàn thành |
| Dungeon System (config, session, API) | ✅ Hoàn thành |
| Dungeon UI (danh sách, nút vào phó bản) | ✅ Hoàn thành |
| Database Schema + Migrations | ✅ Hoàn thành |
| 6 Player Prefabs (mỗi nguyên tố 1 prefab) | ✅ Hoàn thành |
| Animator Override Controllers (Hỏa, Kim, Thổ, Thủy) | ✅ Hoàn thành |
| Buff visual (BuffOutlineVisual — đổi màu khi có buff) | ✅ Hoàn thành |

### ⚠️ Còn hạn chế / cần lưu ý

| Hạng mục | Ghi chú |
|---|---|
| Password hashing | Hiện tại plain text — **phải dùng bcrypt trước production** |
| Cơ chế tương khắc Ngũ Hành | Đã thiết kế (trong `detai.md`) nhưng cần kiểm tra implementation trong `PlayerCombat` |
| Animator Override cho Phong, Mộc | Chưa có file `.overrideController` riêng — dùng base controller |
| Scene `1.unity` | Scene test, không dùng trong production |

---

## Tài liệu hướng dẫn Unity

| File | Nội dung |
|---|---|
| [HUONG_DAN_HOA_UNITY.md](HUONG_DAN_HOA_UNITY.md) | Setup skill hệ Hỏa |
| [HUONG_DAN_THUY_UNITY.md](HUONG_DAN_THUY_UNITY.md) | Setup skill hệ Thủy |
| [HUONG_DAN_THO_UNITY.md](HUONG_DAN_THO_UNITY.md) | Setup skill hệ Thổ |
| [HUONG_DAN_CONFIG_SKILL_SYSTEM.md](HUONG_DAN_CONFIG_SKILL_SYSTEM.md) | Cấu hình chung hệ thống skill |
| [HUONG_DAN_NANG_CAP_GENE.md](HUONG_DAN_NANG_CAP_GENE.md) | Nâng cấp gene |
| [HUONG_DAN_MULTI_GENE_UNITY.md](HUONG_DAN_MULTI_GENE_UNITY.md) | Multi-gene trong Unity |
| [HUONG_DAN_NANG_CAP_TRANG_BI.md](HUONG_DAN_NANG_CAP_TRANG_BI.md) | Nâng cấp trang bị |
| [HUONG_DAN_UI_NANG_CAP_UNITY.md](HUONG_DAN_UI_NANG_CAP_UNITY.md) | UI nâng cấp |
| [HUONG_DAN_UI_SKILL_HUD.md](HUONG_DAN_UI_SKILL_HUD.md) | UI skill HUD |
| [HUONG_DAN_CONFIG_UNITY.md](HUONG_DAN_CONFIG_UNITY.md) | Config chung Unity |
| [GameServerApi/DUNGEON_SYSTEM_GUIDE.md](GameServerApi/DUNGEON_SYSTEM_GUIDE.md) | Hệ thống Dungeon |
| [GameServerApi/STAT_SYSTEM_GUIDE.md](GameServerApi/STAT_SYSTEM_GUIDE.md) | Hệ thống Stat |
