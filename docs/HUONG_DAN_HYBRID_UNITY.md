# HUONG_DAN_HYBRID_UNITY.md
# Hướng Dẫn Config Hybrid Gene System trong Unity

## Mục lục
1. [Chạy SQL Migration](#1-chạy-sql-migration)
2. [Tạo Hybrid Prefabs](#2-tạo-hybrid-prefabs)
3. [Tạo HybridPrefabMap Asset](#3-tạo-hybridprefabmap-asset)
4. [Tạo Animator Controllers](#4-tạo-animator-controllers)
5. [Gắn Hybrid Skill Scripts](#5-gắn-hybrid-skill-scripts)
6. [Config 3 UI Panels mới](#6-config-3-ui-panels-mới)
7. [Mở rộng GeneUpgradePanel](#7-mở-rộng-geneupgradepanel)
8. [Register DefaultNetworkPrefabs](#8-register-defaultnetworkprefabs)
9. [Kiểm tra Immunity Combat](#9-kiểm-tra-immunity-combat)
10. [Checklist Verification](#10-checklist-verification)

---

## 1. Chạy SQL Migration

**Thứ tự bắt buộc:**

```
1. migration_multigene.sql     (đã chạy trước)
2. migration_hybrid_wind.sql   (file mới — chạy bây giờ)
```

**Cách chạy:**
```sql
-- Trong MySQL Workbench hoặc terminal
SOURCE c:/Hub/DoAn/GameServerApi/migration_hybrid_wind.sql;
```

**Kiểm tra sau khi chạy:**
```sql
SELECT COUNT(*) FROM gene_hybrid_config;   -- phải = 3
SELECT COUNT(*) FROM gene_hybrid_skill;    -- phải = 3
SELECT COUNT(*) FROM gene_upgrade_config WHERE element_type = 'Wind';  -- phải = 4
SELECT COUNT(*) FROM skill_template WHERE skill_code LIKE 'HYBRID_%';  -- phải = 3
SELECT element_a, element_b, hybrid_name FROM gene_hybrid_config;  -- 3 dòng
-- Kết quả mong muốn:
-- Earth | Fire  | Dung Nham Địa Hỏa  (sát thương nặng, slow)
-- Water | Wood  | Thủy Mộc Sinh Nguyên  (hồi phục, sustain)
-- Metal | Wind  | Kim Phong Thiên Tốc  (nhanh, nhiều hit)
```

---

## 2. Tạo Hybrid Prefabs

### Folder structure cần tạo:
```
Assets/Prefabs/Player/Hybrid/
├── Hybrid_Earth_Fire.prefab    ← Hỏa + Thổ (Dung Nham Địa Hỏa)
├── Hybrid_Water_Wood.prefab    ← Thủy + Mộc (Thủy Mộc Sinh Nguyên)
└── Hybrid_Metal_Wind.prefab    ← Phong + Kim (Kim Phong Thiên Tốc)
```

> **Chú ý:** Chỉ có **3 combo hợp lệ** trong game. Player chọn hệ chính sẽ chỉ được chọn đúng 1 hệ phụ tương thích:
> - Hỏa chính ↔ Thổ phụ (và ngược lại)
> - Thủy chính ↔ Mộc phụ (và ngược lại)
> - Phong chính ↔ Kim phụ (và ngược lại)

### Cách tạo từng Hybrid Prefab:

**Bước 1:** Chọn prefab hệ chính của combo (ví dụ: Hybrid_Metal_Wind → dùng `He/Kim.prefab` làm base). Kéo vào scene.

**Bước 2:** Đổi tên trong Hierarchy thành `Hybrid_Metal_Wind`.

**Bước 3:** Trong Inspector kiểm tra các component bắt buộc:
- ✅ **NetworkObject** — bắt buộc cho multiplayer
- ✅ **PlayerController** — giữ nguyên
- ✅ **PlayerCombat**
- ✅ **PlayerSkillManager**
- ✅ **PlayerAnimator**
- ✅ **PlayerHealth** / health system

**Bước 4:** Đổi Animator Controller → trỏ vào `Hybrid_Metal_Wind_AC` (xem mục 4).

**Bước 5:** Thêm component Hybrid Skill tương ứng (xem mục 5).

**Bước 6:** Kéo vào folder `Assets/Prefabs/Player/Hybrid/`. Lưu prefab.

> **Tip:** Prefab phụ và chính trong cùng combo chỉ khác nhau ở Animator Controller và Skill script. Visual (mesh/sprite) có thể blend tay hoặc để nguyên từ hệ chính.

---

## 3. Tạo HybridPrefabMap Asset

**Bước 1:** Trong Project window → chuột phải vào `Assets/ScriptableObjects/`  
→ **Create → Game → HybridPrefabMap**

**Bước 2:** Đặt tên asset là `HybridPrefabMap`.

**Bước 3:** Trong Inspector, thêm **3 entries** (chỉ 3 combo hợp lệ):

| Key (string) | Prefab | Mô tả |
|---|---|---|
| `Earth_Fire` | Hybrid_Earth_Fire.prefab | Hỏa+Thổ: damage nặng, slow |
| `Metal_Wind` | Hybrid_Metal_Wind.prefab | Phong+Kim: nhanh, nhiều hit |
| `Water_Wood` | Hybrid_Water_Wood.prefab | Thủy+Mộc: hồi phục, sustain |

> **Quan trọng:** Key phải alphabetically sorted. `Earth_Fire` đúng (E < F), `Metal_Wind` đúng (M < W), `Water_Wood` đúng (Water < Wood).

**Bước 4:** Kéo `HybridPrefabMap` asset vào field tương ứng của **PlayerSpawner** (hoặc CharacterLoader) trên GameManager/NetworkManager prefab của scene.

---

## 4. Tạo Animator Controllers

**Folder:** `Assets/Animations/Hybrid/`

Tạo **3 Animator Controller** files:
```
Hybrid_Earth_Fire_AC.controller    ← Dung Nham Địa Hỏa
Hybrid_Water_Wood_AC.controller    ← Thủy Mộc Sinh Nguyên
Hybrid_Metal_Wind_AC.controller    ← Kim Phong Thiên Tốc
```

### Cách tạo nhanh:

1. Duplicate Animator Controller của hệ chính trong combo  
   (Ví dụ: Hybrid_Metal_Wind → duplicate Kim Animator)

2. Đổi tên thành `Hybrid_Metal_Wind_AC`

3. Mở Animator window → thêm **AnimatorState mới** tên `HybridSkill`:
   - Motion: animation clip skill combo (hoặc reuse clip từ hệ chính nếu chưa có clip mới)
   - Transition: Any State → HybridSkill khi trigger `HybridSkillTrigger`
   - Transition: HybridSkill → Idle khi animation kết thúc

4. Bind **Animation Event** tên `"Hit"` vào frame tác động của animation  
   (xem hướng dẫn HUONG_DAN_ANIMATION_HIT_EVENT.md)

### PlayerAnimator.cs — thêm trigger method:

Trong `PlayerAnimator.cs` (hoặc script Animator wrapper của bạn), thêm:

```csharp
public void TriggerHybridSkill()
{
    // Animator component trỏ đến Hybrid Animator Controller
    _animator.SetTrigger("HybridSkillTrigger");
}
```

---

## 5. Gắn Hybrid Skill Scripts

### Danh sách Skill Script → Prefab (chỉ 3 combo):

| Script Class | Prefab | Skill Code DB |
|---|---|---|
| `HybridEarthFireEruptionSkill` | Hybrid_Earth_Fire.prefab | `HYBRID_EARTH_FIRE_ERUPTION` |
| `HybridWaterWoodRejuvenateSkill` | Hybrid_Water_Wood.prefab | `HYBRID_WATER_WOOD_REJUVENATE` |
| `HybridMetalWindGaleSkill` | Hybrid_Metal_Wind.prefab | `HYBRID_METAL_WIND_GALE` |

### Cách tạo script cho combo khác:

1. Tạo file `.cs` mới trong `Assets/Scripts/Player/Skills/Hybrid/`
2. Kế thừa `HybridSkillBase`
3. Override `ExecuteSkill(Vector2 direction)`
4. Gắn component vào prefab tương ứng

**Template nhanh:**
```csharp
public class HybridFireWindFirestormSkill : HybridSkillBase
{
    [SerializeField] private GameObject stormPrefab;
    [SerializeField] private float duration = 3f;

    protected override void ExecuteSkill(Vector2 direction)
    {
        // Spawn vùng AoE tại vị trí player
        var go = Instantiate(stormPrefab, transform.position, Quaternion.identity);
        go.GetComponent<NetworkObject>()?.Spawn();
        Destroy(go, duration);
    }
}
```

### Config trong Inspector (Hybrid Prefab):

Sau khi gắn script, điền trong Inspector:

| Field | Giá trị |
|---|---|
| Skill Code | `HYBRID_METAL_WIND_GALE` (khớp DB) |
| Cooldown | 13 |
| Mp Cost | 55 |
| Effect Value | 295 |
| Arrow Count | 12 |
| Spread Angle Deg | 180 |
| Pierce Count | 3 |

> **Skill Code phải khớp chính xác với cột `skill_code` trong bảng `skill_template`.**

---

## 6. Config 3 UI Panels mới

### Tạo 3 Panel GameObjects trong Canvas:

```
Canvas
├── GeneUpgradePanel (existing)
├── SecondaryGeneSelectPanel  ← MỚI
├── SecondaryGeneUpgradePanel ← MỚI
└── HybridFusionPanel         ← MỚI
```

### 6.1 SecondaryGeneSelectPanel

> **⚠ Hệ phụ CỐ ĐỊNH — Backend chỉ chấp nhận đúng 1 lựa chọn theo hệ chính:**
> | Hệ chính | Hệ phụ duy nhất |
> |----------|-----------------|
> | Hỏa (Fire)  | Thổ (Earth) |
> | Thổ (Earth) | Hỏa (Fire)  |
> | Thủy (Water)| Mộc (Wood)  |
> | Mộc (Wood)  | Thủy (Water)|
> | Kim (Metal) | Phong (Wind)|
> | Phong (Wind)| Kim (Metal) |
>
> UI **chỉ nên hiển thị 1 nút** — đúng hệ đối tác. Gửi sai hệ sẽ nhận lỗi 400 từ API.

1. Tạo UI Panel → Add Component `SecondaryGeneSelectPanel.cs`
2. Kéo các TMP_Text, Button vào đúng slot theo Inspector docstring trong file `.cs`
3. **ElementButtonPrefab:** tạo prefab đơn giản gồm Button + Image + TMP_Text
4. **ElementSprites[6]:** kéo 6 sprite icon hệ vào, đúng thứ tự index (dùng để tra sprite theo tên hệ):
   - [0] = Kim (Metal)
   - [1] = Mộc (Wood)
   - [2] = Thủy (Water)
   - [3] = Hỏa (Fire)
   - [4] = Thổ (Earth)
   - [5] = Phong (Wind)
5. Trong `SecondaryGeneSelectPanel.cs`, khi `Open()` được gọi: tính đối tác cố định theo `_playerData.element_type` rồi chỉ Instantiate **1 button** cho hệ đó (không spawn cả 6).
6. Đặt `gameObject.SetActive(false)` mặc định

### 6.2 SecondaryGeneUpgradePanel

1. Duplicate layout của `GeneUpgradePanel` → thêm vào component `SecondaryGeneUpgradePanel.cs`
2. Thêm 2 GameObject mới:
   - `CanFuseBanner` — text "✨ Đủ điều kiện Hybrid Fusion!" (ẩn mặc định)
   - `FuseButton` — nút "Tiến hành Fusion →"
3. Kéo đúng field vào Inspector

### 6.3 HybridFusionPanel

1. Tạo Panel mới → Add Component `HybridFusionPanel.cs`
2. Layout gợi ý:
   ```
   ┌──────────────────────────────┐
   │  [Icon A] + [Icon B]         │  elementA/BIcon
   │  "Kim Phong Thoán Thế"       │  hybridNameText
   │  "Mô tả hybrid..."           │  hybridDescText
   │  ─────────────────────────── │
   │  Stat Bonus: +2000HP...      │  statBonusText
   │  Miễn nhiễm: Hỏa, Thổ       │  immuneElementsText
   │  +50% lên: Mộc, Hỏa          │  bonusTargetsText
   │  Skill Combo: Kim Phong...   │  comboSkillText
   │  ─────────────────────────── │
   │  Vàng: 2,000,000             │  goldCostText
   │  Bạn có: 3,500,000 ✅        │  goldPlayerText
   │  5× Lõi Đột Biến             │  itemCostText
   │  Bạn có: 7 ✅                │  itemPlayerText
   │  ─────────────────────────── │
   │        [FUSION!]             │  fuseButton
   │         [Đóng]               │  closeButton
   │  "Thông báo kết quả..."      │  statusText
   └──────────────────────────────┘
   ```
3. `SuccessEffect` — kéo Particle System prefab vào (có thể reuse từ gene upgrade effect)

---

## 7. Mở rộng GeneUpgradePanel

Cần thêm logic mở Panel phụ từ GeneUpgradePanel hiện có:

### 7.1 Thêm nút "Hệ Phụ" và "Kết Hợp" vào GeneUpgradePanel:

Trong `GeneUpgradePanel.cs`, thêm 2 nút mới vào Inspector:
```csharp
[Header("Navigation to Other Panels")]
[SerializeField] private Button secondaryGeneButton;   // tab/nút "Hệ Phụ"
[SerializeField] private Button hybridFusionButton;    // tab/nút "Kết Hợp"
[SerializeField] private TMP_Text secondaryLockedText; // "Cần Tier 5 hệ chính"
```

Trong `Start()`, thêm:
```csharp
if (secondaryGeneButton != null)
    secondaryGeneButton.onClick.AddListener(OnSecondaryClicked);
if (hybridFusionButton != null)
    hybridFusionButton.onClick.AddListener(OnHybridClicked);
```

Thêm 2 method:
```csharp
private void OnSecondaryClicked()
{
    if (_playerData == null) return;
    if (_playerData.gene_tier < 5)
    {
        SetStatus("Cần nâng Gene hệ chính lên Tier 5 trước!", Color.yellow);
        return;
    }
    gameObject.SetActive(false);
    if (string.IsNullOrEmpty(_playerData.secondary_element))
        SecondaryGeneSelectPanel.Instance?.Open();
    else
        SecondaryGeneUpgradePanel.Instance?.Open();
}

private void OnHybridClicked()
{
    if (_playerData == null) return;
    if (_playerData.gene_tier < 5 || _playerData.secondary_gene_tier < 5)
    {
        SetStatus("Cần cả 2 hệ đạt Tier 5!", Color.yellow);
        return;
    }
    gameObject.SetActive(false);
    HybridFusionPanel.Instance?.Open();
}
```

---

## 8. Register DefaultNetworkPrefabs

**Bắt buộc trước khi test multiplayer.**

1. Mở `Assets/DefaultNetworkPrefabs.asset` trong Inspector
2. Bấm "+" 3 lần, kéo 3 hybrid prefab vào:
   - `Hybrid_Earth_Fire`
   - `Hybrid_Water_Wood`
   - `Hybrid_Metal_Wind`

> Nếu quên bước này, server sẽ báo: `"[Netcode] The prefab hash set could not locate..."` khi spawn hybrid player.

---

## 9. Kiểm tra Immunity Combat

File liên quan: `ElementHelper.cs` (đã thêm `IsImmuneToCounter` method).

### Cách áp dụng trong DamageCalculator / CombatManager:

Tìm đoạn code tính counter penalty (thường ở `PlayerCombat.cs` hoặc `EnemyCombat.cs`):

```csharp
// TRƯỚC (cũ):
if (counterSystem.IsCounteredBy(attackerElement, defenderElement))
    damage = (int)(damage * 0.75f);  // -25% ATK penalty

// SAU (mới):
if (counterSystem.IsCounteredBy(attackerElement, defenderElement))
{
    // Kiểm tra hybrid immunity TRƯỚC khi áp penalty
    var defenderPlayerData = GetPlayerData(defender);  // lấy PlayerDataResponse
    if (!ElementHelper.IsImmuneToCounter(attackerElement, defenderPlayerData))
        damage = (int)(damage * 0.75f);
}
```

**Method signature:**
```csharp
// attackerElement: English key ("Fire", "Water", "Metal"...)
// target: PlayerDataResponse của người bị tấn công
ElementHelper.IsImmuneToCounter(string attackerElement, PlayerDataResponse target)
// → true nếu target là Hybrid và attackerElement thuộc immune_elements của target
```

---

## 10. Checklist Verification

### SQL
- [ ] `SELECT COUNT(*) FROM gene_hybrid_config` = **3**
- [ ] `SELECT COUNT(*) FROM gene_hybrid_skill` = **3**
- [ ] `SELECT COUNT(*) FROM gene_upgrade_config WHERE element_type = 'Wind'` = **4**
- [ ] `SELECT COUNT(*) FROM gene_multi_config WHERE element_type = 'Wind'` = **4**
- [ ] `SELECT COUNT(*) FROM skill_template WHERE skill_code LIKE 'HYBRID_%'` = **3**
- [ ] Item id=26 "Tinh Chất Phong" tồn tại
- [ ] 3 combo đúng: `SELECT element_a, element_b FROM gene_hybrid_config` = (Earth,Fire), (Water,Wood), (Metal,Wind)

### Backend API
- [ ] `GET /api/gene/config?elementType=Wind&tier=1` → trả về config (không 404)
- [ ] `POST /api/gene/secondary/select` với player Kim (Metal) + `secondaryElement="Wind"` → thành công
- [ ] `POST /api/gene/secondary/select` với player Kim (Metal) + `secondaryElement="Fire"` → nhận **BadRequest 400** (sai hệ đối tác)
- [ ] `POST /api/gene/hybrid/fuse` → response có `prefabPath`, `comboSkillCode`
- [ ] Sau fuse: `player.SkillsJson` chứa entry với `skillCode` bắt đầu bằng `"HYBRID_"`

### Unity
- [ ] Tạo đủ **3 prefab** trong `Assets/Prefabs/Player/Hybrid/` (Earth_Fire, Water_Wood, Metal_Wind)
- [ ] Mỗi prefab có **NetworkObject** component
- [ ] `HybridPrefabMap.asset` có đủ **3 entries**, keys alphabetically sorted
- [ ] HybridPrefabMap được kéo vào PlayerSpawner/CharacterLoader
- [ ] 3 prefab đã được add vào `DefaultNetworkPrefabs.asset`
- [ ] Tạo 3 Animator Controllers trong `Assets/Animations/Hybrid/`
- [ ] Tạo 3 Hybrid Skill scripts: `HybridEarthFireEruptionSkill`, `HybridWaterWoodRejuvenateSkill`, `HybridMetalWindGaleSkill`
- [ ] Test spawn hybrid player → correct prefab xuất hiện
- [ ] Test SecondaryGeneSelectPanel: chỉ hiện 1 lựa chọn phụ phù hợp với hệ chính
- [ ] Test combat: hệ thuộc immune_elements attack hybrid player → không nhận penalty

### UI
- [ ] SecondaryGeneSelectPanel mở sau khi hệ chính Tier 5
- [ ] SecondaryGeneSelectPanel chỉ cho chọn 1 lần (nút disabled sau confirm)
- [ ] SecondaryGeneUpgradePanel hiện đúng tier, exp, cost hệ phụ
- [ ] HybridFusionPanel hiện đúng stat preview, cost, canFuse logic
- [ ] Sau fusion: panels đóng, player character đổi prefab, skill hotbar cập nhật

---

## Lưu ý quan trọng

1. **Alphabet sorting:** Key trong HybridPrefabMap phải alphabetically sorted giống DB.  
   Thứ tự: `Earth < Fire < Metal < Water < Wind < Wood`

2. **skill_code prefix:** Tất cả hybrid skill phải có prefix `HYBRID_`.  
   SkillRuntimeLoader cần check prefix này để load đúng script.

3. **secondary_element chỉ set 1 lần:** Backend đã enforce (`BadRequest` nếu cố ghi đè).  
   Không cần guard thêm ở client.

4. **Hệ Phong (Wind) không tham gia Ngũ Hành vòng khắc:**  
   `ElementHelper.GetCounteredElement("Wind")` trả về `null`.  
   Wind hybrid combos dùng bonus/immune riêng của config.

5. **Stat bonus hệ phụ = 50% stat bonus hệ chính:**  
   Logic này nằm trong `GeneController.UpgradeSecondaryGene()` — không cần config riêng.
