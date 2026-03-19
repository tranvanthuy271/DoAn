# Hướng Dẫn Config Hybrid Skill + Prefab Player Hybrid

## Tổng quan

Sau khi Fusion thành công, player nhận được:
1. **Prefab mới** (thay thế model player — đã config trong DB)
2. **Hybrid Skill** (skill đặc biệt chỉ hybrid mới dùng được)

Hệ thống chỉ có **3 cặp Hybrid hợp lệ**:

| Cặp | hybrid_id DB | Hybrid Skill | Prefab Path |
|-----|-------------|--------------|-------------|
| Hỏa ↔ Thổ | 1 | `HYBRID_EARTH_FIRE_ERUPTION` (skill_id=26) | `Prefabs/Player/Hybrid/Hybrid_Earth_Fire` |
| Thủy ↔ Mộc | 10 | `HYBRID_WATER_WOOD_VENOM` (skill_id=35) | `Prefabs/Player/Hybrid/Hybrid_Water_Wood` |
| Kim ↔ Phong | 13 | `HYBRID_METAL_WIND_GALE` (skill_id=38) | `Prefabs/Player/Hybrid/Hybrid_Metal_Wind` |

---

## PHẦN 0 — Nguyên tắc Skill sau Fusion (ĐỌC TRƯỚC)

### Skill được giữ lại theo Hệ CHÍNH (primary element)

Sau Fusion, server tự động:
- **Giữ lại 3 skill hệ chính** (3 skill có `skill_id` nhỏ nhất thuộc `element_type` chính)
- **Thêm 1 hybrid skill** chung cho cả 2 chiều

Ví dụ cặp **Kim + Phong** (`hybrid_id=13`):

| Trường hợp | Hệ chính | Hệ phụ | Skills sau Fusion |
|-----------|---------|--------|-------------------|
| A | **Phong** (Wind) | Kim (Metal) | `WIND_STRIKE` + `WIND_BLADE` + `WIND_STEP` + `HYBRID_METAL_WIND_GALE` |
| B | **Kim** (Metal) | Phong (Wind) | `METAL_STRIKE` + `METAL_BLADE` + `METAL_SHIELD` + `HYBRID_METAL_WIND_GALE` |

> **Hybrid Skill (`HYBRID_METAL_WIND_GALE`) là CHUNG cho cả 2 chiều** — chỉ có `hybrid_id` phân biệt, không phụ thuộc ai là primary/secondary.

### Config số skill giữ lại

Mặc định: `PrimarySkillKeepCount = 3` (lưu trong `gene_hybrid_config`).

```sql
-- Đổi thành 2 skill nếu muốn:
UPDATE gene_hybrid_config SET primary_skill_keep_count = 2 WHERE hybrid_id = 13;
```

### Hybrid Skill KHÔNG nâng cấp

`max_level = 1`, không tiêu SP, `sp_cost = 0`. Skill luôn ở level tối đa ngay khi unlock.

---

---

## PHẦN 1 — Config DB (Server)

### 1.1 Cấu trúc bảng `skill_template` cho Hybrid Skill

```sql
-- Các cột quan trọng:
-- element_type  = NULL   (không thuộc hệ đơn nào)
-- hybrid_id     = FK → gene_hybrid_config.hybrid_id
-- max_level     = 1      (hybrid skill không nâng cấp)
```

### 1.2 Thêm Hybrid Skill mới (ví dụ Kim+Phong thêm skill thứ 2)

```sql
INSERT INTO skill_template
    (skill_code, skill_name, description, element_type, max_level, level_to_unlock,
     levels_json, icon_id, gene_tier_required, hybrid_id)
VALUES
    ('HYBRID_METAL_WIND_STORM',
     'Kim Phong Bão Táp',
     'Tạo cơn bão kim loại xoáy, gây sát thương liên tục trong vùng rộng.',
     NULL,         -- không thuộc hệ đơn
     1,            -- hybrid skill không nâng cấp
     1,
     '[{"level_req":1,"sp_cost":0,"effect_value":350,"mp_cost":60,"cooldown_sec":18.0}]',
     'icon_hybrid_metal_wind_storm',
     0,
     13            -- hybrid_id=13 → Kim+Phong
    );
```

> **Quy tắc quan trọng:**
> - `element_type` = **NULL** (bắt buộc)
> - `hybrid_id` = **đúng ID** của cặp hệ (xem bảng trên)
> - Skill sẽ CHỈ hiện cho player có `is_hybrid=true` và `hybrid_id` khớp

### 1.3 Cập nhật prefab_path trong `gene_hybrid_config`

Prefab đường dẫn dùng `Resources.Load<GameObject>(path)` trong Unity.

```sql
-- 3 cặp hợp lệ đã được cấu hình sẵn:
UPDATE gene_hybrid_config SET prefab_path = 'Prefabs/Player/Hybrid/Hybrid_Earth_Fire' WHERE hybrid_id = 1;
UPDATE gene_hybrid_config SET prefab_path = 'Prefabs/Player/Hybrid/Hybrid_Water_Wood' WHERE hybrid_id = 10;
UPDATE gene_hybrid_config SET prefab_path = 'Prefabs/Player/Hybrid/Hybrid_Metal_Wind' WHERE hybrid_id = 13;
```

### 1.4 Kiểm tra DB

```sql
-- Xem tất cả hybrid skills đã config:
SELECT s.skill_id, s.skill_code, s.skill_name, s.hybrid_id,
       h.element_a, h.element_b, h.hybrid_name
FROM skill_template s
JOIN gene_hybrid_config h ON s.hybrid_id = h.hybrid_id
ORDER BY s.hybrid_id;

-- Kết quả mong đợi:
-- 26 | HYBRID_EARTH_FIRE_ERUPTION | Đại Địa Phún Thạch  | 1  | Earth | Fire  | Dung Nham Địa Hỏa
-- 35 | HYBRID_WATER_WOOD_VENOM    | Băng Độc Vĩnh Cửu   | 10 | Water | Wood  | Băng Độc Vĩnh Hằng
-- 38 | HYBRID_METAL_WIND_GALE     | Kim Phong Thiên Vũ  | 13 | Metal | Wind  | Kim Phong Thoán Thế
```

---

## PHẦN 2 — Config Unity Prefab Player Hybrid

### 2.1 Tạo Prefab Hybrid

Mỗi cặp hybrid cần **1 prefab riêng** đặt tại `Assets/Resources/Prefabs/Player/Hybrid/`:

```
Assets/Resources/Prefabs/Player/Hybrid/
├── Hybrid_Earth_Fire.prefab     ← Hỏa + Thổ
├── Hybrid_Water_Wood.prefab     ← Thủy + Mộc  
└── Hybrid_Metal_Wind.prefab     ← Kim + Phong
```

> **Lưu ý:** Thư mục phải nằm trong `Resources/` để `Resources.Load()` hoạt động.
> Đường dẫn relative từ `Resources/`:  
> `gene_hybrid_config.prefab_path` = `"Prefabs/Player/Hybrid/Hybrid_Metal_Wind"` (không có `Assets/Resources/`)

### 2.2 Cấu trúc Prefab

Copy từ prefab player thường, đổi:
1. **Model/Sprite** — nhân vật hybrid (màu sắc/hình dạng kết hợp 2 hệ)
2. **Animator Controller** — dùng controller hybrid riêng (xem Phần 3)
3. **Component giữ nguyên:** `PlayerController`, `PlayerNetworkSync`, `SkillManager`, `HealthComponent`

### 2.3 Khi Fusion Thành Công

Server trả về `prefabPath` trong response `/api/gene/hybrid/fuse`. Client `HybridFusionPanel` tự động lưu vào `PlayerData.hybrid_prefab_path`. Code spawn/swap prefab cần đọc field này:

```csharp
// Ví dụ spawn hybrid prefab sau fusion:
var pd = GameManager.Instance.GetPlayerData();
if (pd.is_hybrid && !string.IsNullOrEmpty(pd.hybrid_prefab_path))
{
    var hybridPrefab = Resources.Load<GameObject>(pd.hybrid_prefab_path);
    if (hybridPrefab != null)
    {
        // Spawn và replace player object hiện tại
    }
}
```

---

## PHẦN 3 — Config Animation Hybrid Skill

### 3.1 Vấn đề: Prefab khác nhau theo chiều Fusion

Vì **cùng hybrid_id=13 nhưng có 2 model khác nhau** (Phong-primary vs Kim-primary), có 2 cách xử lý animator:

**Cách A: Dùng chung 1 Animator Controller** (đơn giản hơn)
- Cả 2 chiều dùng cùng prefab `Hybrid_Metal_Wind.prefab`
- Animator có trigger chung: `HybridSkill` → play cùng 1 clip animation
- Phù hợp nếu 2 chiều trông giống nhau

**Cách B: 2 Prefab riêng biệt** (linh hoạt hơn)
- `Hybrid_Metal_Wind_MetalPrimary.prefab` — khi hệ Kim là primary
- `Hybrid_Metal_Wind_WindPrimary.prefab` — khi hệ Phong là primary
- Mỗi prefab có Animator Controller + clip animation riêng
- Cần config thêm `gene_hybrid_config.prefab_path` theo chiều

> **Hiện tại server dùng Cách A** — 1 `prefab_path` duy nhất trong DB không phân biệt chiều.

---

### 3.2 Cấu trúc thư mục Animation

```
Assets/Animations/Skills/Hybrid/
├── MetalWind/
│   ├── HybridSkill_Cast.anim       ← animation chuẩn bị / channel skill
│   └── HybridSkill_Release.anim    ← animation phóng 12 mũi tên hình nan quạt
├── EarthFire/
│   ├── HybridSkill_Cast.anim
│   └── HybridSkill_Release.anim
└── WaterWood/
    ├── HybridSkill_Cast.anim
    └── HybridSkill_Release.anim
```

### 3.3 Animator Controller Hybrid

Tạo file `Hybrid_Metal_Wind_AC.controller` tại `Assets/Animations/Skills/Hybrid/MetalWind/`:

**States tối thiểu:**
```
[Any State] ──trigger HybridSkill──► HybridSkill_Cast
                                           │ (Exit Time hoặc Animation Event)
                                           ▼
                                     HybridSkill_Release
                                           │
                                           ▼ (về Idle)
```

**Parameters bắt buộc:**

| Tên | Kiểu | Trigger khi nào |
|-----|------|----------------|
| `HybridSkill` | Trigger | Player bấm slot skill hybrid |
| `isMoving` | Bool | Di chuyển |
| `Hit` | Trigger | Nhận đòn |
| `Die` | Trigger | Chết |

### 3.4 Animation Event — Thời điểm gây sát thương

Hybrid skill thường có **2 phase**:
1. **Cast phase** (0.3–0.5s): channel năng lượng, không gây damage
2. **Release phase**: phóng projectile/AoE → **đây là lúc thêm Animation Event**

**Cách thêm Animation Event:**

1. Mở `Assets/Animations/Skills/Hybrid/MetalWind/HybridSkill_Release.anim`
2. Mở **Animation window** (Window → Animation → Animation)
3. Chọn frame muốn gây sát thương (thường frame 5–8 của release)
4. Click nút **Add Event** (biểu tượng bút ở đầu timeline)
5. Inspector của event:
   - **Function:** `OnHybridSkillFire` (hoặc tên method trong script skill của bạn)

```csharp
// Trong script Hybrid Skill hoặc PlayerAnimEventHandler.cs
public void OnHybridSkillFire()
{
    // Gọi logic tạo projectile / AoE
    hybridSkillComponent?.ExecuteRelease();
}
```

### 3.5 Mẫu script Hybrid Skill (Kim + Phong)

Tạo `Assets/Scripts/Player/Skills/Hybrid/HybridMetalWindSkill.cs`:

```csharp
/// <summary>
/// Hybrid Skill: Kim Phong Thiên Vũ — HYBRID_METAL_WIND_GALE
/// Phóng 12 mũi tên kim loại theo hình nan quạt, mỗi mũi xuyên 3 kẻ địch.
/// skill_code = "HYBRID_METAL_WIND_GALE", hybrid_id = 13
/// Dùng cho CẢ 2 CHIỀU: Kim primary và Phong primary.
/// </summary>
public class HybridMetalWindSkill : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private int   arrowCount   = 12;
    [SerializeField] private float spreadAngle  = 120f;
    [SerializeField] private float arrowSpeed   = 14f;
    [SerializeField] private int   pierceCount  = 3;

    private float _effectValue;   // từ server: effect_value tại level 1
    private Animator _animator;

    private void Awake() => _animator = GetComponentInParent<Animator>();

    /// <summary>Gọi từ SkillManager khi player kích hoạt hybrid skill.</summary>
    public void Cast(float effectValue, Vector2 direction)
    {
        _effectValue = effectValue;
        _cachedDirection = direction;
        _animator.SetTrigger("HybridSkill");  // trigger animation Cast → Release
    }

    private Vector2 _cachedDirection;

    /// <summary>Gọi bởi Animation Event trên clip HybridSkill_Release.anim</summary>
    public void OnHybridSkillFire()
    {
        float angleStep  = spreadAngle / (arrowCount - 1);
        float startAngle = -spreadAngle / 2f;

        for (int i = 0; i < arrowCount; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector2 dir = Rotate(_cachedDirection, angle);

            var go   = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();   // component projectile có sẵn
            if (proj != null)
            {
                proj.Init(damage: (int)_effectValue, direction: dir,
                          speed: arrowSpeed, pierceRemaining: pierceCount);
            }
        }
    }

    private static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        return new Vector2(v.x * Mathf.Cos(r) - v.y * Mathf.Sin(r),
                           v.x * Mathf.Sin(r) + v.y * Mathf.Cos(r));
    }
}
```

---

## PHẦN 4 — Checklist Config Đầy Đủ

### Khi thêm Hybrid Skill mới:

- [ ] **DB:** `INSERT INTO skill_template` với `element_type=NULL`, `hybrid_id` đúng, `max_level=1`, `sp_cost=0`
- [ ] **DB:** Verify bằng query Section 1.4
- [ ] **Unity:** Tạo script skill trong `Assets/Scripts/Player/Skills/Hybrid/`
- [ ] **Unity:** Tạo 2 animation clip trong `Assets/Animations/Skills/Hybrid/{PairName}/`:
  - `HybridSkill_Cast.anim` — phase channel năng lượng
  - `HybridSkill_Release.anim` — phase phóng projectile (thêm Animation Event ở đây)
- [ ] **Unity:** Thêm state `HybridSkill_Cast` → `HybridSkill_Release` vào Animator Controller hybrid
- [ ] **Unity:** Thêm Animation Event `OnHybridSkillFire` vào clip Release
- [ ] **Unity:** Assign script skill vào prefab hybrid

### Khi thêm Prefab Player Hybrid mới:

- [ ] **Unity:** Tạo prefab tại `Assets/Resources/Prefabs/Player/Hybrid/`
- [ ] **Unity:** Tên file khớp với `gene_hybrid_config.prefab_path` trong DB
- [ ] **Unity:** Gán Animator Controller hybrid vào prefab
- [ ] **DB:** Verify `prefab_path` trong `gene_hybrid_config`
- [ ] **Unity:** Kéo prefab vào slot trong `NetworkPlayerSpawner` Inspector
- [ ] **Test:** Fusion → verify prefab đổi đúng, hybrid skill xuất hiện trên HUD

---

## PHẦN 5 — Bảng Mapping đầy đủ 3 cặp

### Kim + Phong (`hybrid_id=13`)

| Hệ chính | Skills giữ lại (3) | Hybrid Skill thêm vào |
|---------|-------------------|-----------------------|
| **Phong** (Wind) | WIND_STRIKE (9) + WIND_BLADE (10) + WIND_STEP (11) | HYBRID_METAL_WIND_GALE (38) |
| **Kim** (Metal) | METAL_STRIKE (20) + METAL_BLADE (21) + METAL_SHIELD (22) | HYBRID_METAL_WIND_GALE (38) |

### Hỏa + Thổ (`hybrid_id=1`)

| Hệ chính | Skills giữ lại (3) | Hybrid Skill thêm vào |
|---------|-------------------|-----------------------|
| **Hỏa** (Fire) | FIRE_BOLT (15) + FIRE_BURST (12) + FIRE_RAIN (17) | HYBRID_EARTH_FIRE_ERUPTION (26) |
| **Thổ** (Earth) | EARTH_AURA (23) + EARTH_BOOMERANG (24) + EARTH_BLINK (25) | HYBRID_EARTH_FIRE_ERUPTION (26) |

### Thủy + Mộc (`hybrid_id=10`)

| Hệ chính | Skills giữ lại (3) | Hybrid Skill thêm vào |
|---------|-------------------|-----------------------|
| **Thủy** (Water) | WATER_BOLT (*) + WATER_PILLAR (13) + EARTH_SHIELD (14) | HYBRID_WATER_WOOD_VENOM (35) |
| **Mộc** (Wood) | WOOD_VINE (8) + WOOD_ARROW (18) + WOOD_HEAL (19) | HYBRID_WATER_WOOD_VENOM (35) |

> `primary_skill_keep_count = 3` trong `gene_hybrid_config`. Server sắp xếp theo `skill_id ASC` và lấy 3 cái đầu tiên thuộc hệ chính.

---

## PHẦN 6 — Luồng kỹ thuật đầy đủ khi Fusion

```
Player bấm FUSION
       │
       ▼
POST /api/gene/hybrid/fuse
       │
       ├─ Validate: tier 5 cả 2 hệ, đủ item + gold, cặp hợp lệ
       │
       ├─ GeneController:
       │   ├─ info.IsHybrid = true
       │   ├─ info.HybridId = cfg.HybridId         ← 1 / 10 / 13
       │   ├─ info.HybridPrefabPath = cfg.PrefabPath
       │   ├─ Cộng stat bonus
       │   ├─ Lấy skills hệ chính, giữ top 3 (theo primary_skill_keep_count)
       │   └─ Thêm hybrid skill_id (skill.HybridId == cfg.HybridId) vào SkillsJson
       │
       ▼
Response: { hybridId, prefabPath, comboSkillCode, ... }
       │
       ▼
HybridFusionPanel (Client):
       ├─ Lưu hybrid_id, hybrid_prefab_path vào PlayerData
       └─ (TODO) Trigger swap prefab → NetworkPlayerSpawner spawn hybrid prefab lần sau
       
       ▼
GET /api/player/{id}/skills (gọi lại sau fusion)
       │
       ├─ Query: WHERE (hybrid_id IS NULL AND element_type = playerElement)
       │          OR   (hybrid_id = player.HybridId AND is_hybrid = true)
       │
       └─ Response: 3 skills hệ chính + 1 hybrid skill (đúng cặp)
```

---

## PHẦN 7 — Mapping Skill Code ↔ Hybrid (dùng trong Unity)

```csharp
public static readonly HashSet<string> HybridSkillCodes = new()
{
    "HYBRID_EARTH_FIRE_ERUPTION",   // Hỏa+Thổ  → hybrid_id=1
    "HYBRID_WATER_WOOD_VENOM",      // Thủy+Mộc → hybrid_id=10
    "HYBRID_METAL_WIND_GALE",       // Kim+Phong → hybrid_id=13
};

public static bool IsHybridSkill(string skillCode)
    => HybridSkillCodes.Contains(skillCode);
```
