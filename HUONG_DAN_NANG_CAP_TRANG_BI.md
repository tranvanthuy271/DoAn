# Hướng dẫn Config Hệ Thống Nâng Cấp Trang Bị

## Mục lục
1. [Tổng quan luồng nâng cấp](#1-tổng-quan-luồng-nâng-cấp)
2. [Config DB: equipment_upgrade_config](#2-config-db-equipment_upgrade_config)
3. [Config DB: option_template (strOption)](#3-config-db-option_template-strOption)
4. [Config item_template (các loại đá)](#4-config-item_template-các-loại-đá)
5. [Server-side: Công thức tỉ lệ & xử lý thành/thất bại](#5-server-side-công-thức-tỉ-lệ--xử-lý-thànhthất-bại)
6. [Unity: UI Nâng Cấp (UpgradePanel)](#6-unity-ui-nâng-cấp-upgradepanel)
7. [Unity: Hiển thị stat (ItemDetailPanel)](#7-unity-hiển-thị-stat-itemdetailpanel)
8. [Ví dụ nâng cấp step-by-step](#8-ví-dụ-nâng-cấp-step-by-step)
9. [Bảng tham chiếu nhanh](#9-bảng-tham-chiếu-nhanh)

---

## 1. Tổng quan luồng nâng cấp

```
Player mở UI Nâng Cấp
        │
        ▼
[Server] Lấy config từ equipment_upgrade_config
         WHERE upgrade_level = item.upgradeLevel + 1
        │
        ▼
[Client] Hiển thị:
  - Silver cần          (silver_cost)
  - Đá cần loại gì      (stone_id → tên item)
  - Số đá tối thiểu     (stone_min)
  - Số đá đề nghị       (stone_needed → đạt tỉ lệ base)
  - Tỉ lệ thành công    (tính realtime theo số đá nhập)
  - Có thể vỡ không?    (fail_policy: 0=an toàn, 1=mất 1 bậc)
        │
        ▼
Player xác nhận → Server tính random → Thành công / Thất bại
        │                                     │
        ▼                                     ▼
item.upgradeLevel += 1               fail_policy=0 → giữ nguyên
                                     fail_policy=1 → upgradeLevel -= 1
```

> **Lưu ý quan trọng:**
> Sau mỗi lần nâng cấp thành công, server phải **tái tính lại `strOptions`** của item dựa trên `option_template.strOption[newUpgradeLevel]` và lưu vào DB/player_data.

---

## 2. Config DB: equipment_upgrade_config

### Cấu trúc bảng

| Cột | Kiểu | Ý nghĩa |
|-----|------|---------|
| `upgrade_level` | tinyint PK | Bậc muốn đạt (1~20) |
| `silver_cost` | int | Bạc tiêu hao khi thực hiện |
| `stone_id` | int | FK → item_template.id (loại đá cần dùng) |
| `stone_needed` | tinyint | Số đá để đạt đúng `base_success_rate` |
| `stone_min` | tinyint | Số đá ít nhất được phép dùng |
| `base_success_rate` | float | Tỉ lệ gốc (0.0 ~ 1.0) khi dùng đủ `stone_needed` |
| `fail_policy` | tinyint | `0`=an toàn `1`=-1 bậc `2`=về +0 |

### Cách thêm/sửa config

```sql
-- Sửa tỉ lệ nâng lên +5 (hiện tại 85% → muốn 90%)
UPDATE equipment_upgrade_config
SET base_success_rate = 0.90
WHERE upgrade_level = 5;

-- Thêm bậc +21 (nếu muốn mở rộng)
INSERT INTO equipment_upgrade_config VALUES
(21, 5000000, 7, 20, 10, 0.10, 1);
```

### Phân vùng bậc & fail_policy mặc định

| Bậc | Loại đá | fail_policy | Ghi chú |
|-----|---------|-------------|---------|
| +1 ~ +3 | Đá Cấp 1 (id=1) | 0 (an toàn) | Không bao giờ mất bậc |
| +4 ~ +6 | Đá Cấp 2 (id=2) | 0 (an toàn) | Không bao giờ mất bậc |
| +7 ~ +9 | Đá Cấp 3 (id=3) | 1 (-1 bậc) | Thất bại → về -1 |
| +10 ~ +12 | Đá Cấp 4 (id=4) | 1 (-1 bậc) | |
| +13 ~ +15 | Đá Cấp 5 (id=5) | 1 (-1 bậc) | |
| +16 ~ +18 | Đá Cấp 6 (id=6) | 1 (-1 bậc) | |
| +19 ~ +20 | Đá Cấp 7 (id=7) | 1 (-1 bậc) | Cực khó |

> **Đá Bảo Vệ (id=9):** Nếu player sử dụng thêm Đá Bảo Vệ,
> server bỏ qua `fail_policy` → không mất bậc khi thất bại.

---

## 3. Config DB: option_template (strOption)

### Cấu trúc strOption

```
strOption = "v0;v1;v2;v3;v4;v5;v6;v7;v8;v9;v10;v11;v12;v13;v14;v15;v16;v17;v18;v19"
                │  │  │                   │                              │
               +0 +1 +2 (bậc nâng cấp)  +9                            +19
```

- **Đúng 20 giá trị**, cách nhau dấu `;`
- **vN = tổng stat** khi item đang ở bậc `+N` (không phải delta)
- Giá trị ở bậc cao phải **≥ giá trị ở bậc thấp** (không bao giờ giảm)

### Ví dụ option id=21 (HP tối đa)

```sql
-- HP tối đa: +20 tại +0, tăng dần, +1445 tại +19
UPDATE option_template
SET strOption = '20;25;32;40;50;63;79;99;124;155;194;242;303;379;473;592;740;925;1156;1445'
WHERE id = 21;
```

> **Lúc nào server dùng giá trị này?**
> Khi nâng cấp thành công lên bậc `N`:
> ```
> value = strOption.Split(';')[N]   // lấy đúng giá trị tại bậc N
> ```
> Ghi vào `strOptions` của item instance: `"21,{value}"`.

### Config option unlock (+4 / +8 / +12 / +16)

```
level = 4  → option CỐ ĐỊNH DIM cho đến khi item đạt +4
           → strOption[0] = strOption[1] = strOption[2] = strOption[3] = 0
           → từ index 4 trở đi mới có giá trị thật
```

```sql
-- Ví dụ: option id=31 "(+4) Hồi HP mỗi 0.5s"
-- index 0-3 = 0, index 4 trở đi có giá trị
UPDATE option_template
SET strOption = '0;0;0;0;2;2;3;3;4;4;5;5;6;6;7;7;8;8;9;9'
WHERE id = 31;
```

### Thêm option mới cho một trang bị

**Bước 1:** Thêm dòng vào `option_template`:
```sql
INSERT INTO option_template (id, name, type, level, strOption) VALUES
(50, 'Tốc độ di chuyển: +#', 2, 0,
 '5;6;7;9;11;13;16;19;23;28;33;40;48;57;68;81;97;116;139;167');
```

**Bước 2:** Khi phát hành item mới (thêm vào item_template), gán option này bằng cách config `strOptions` mặc định trong code/tool tạo item:
```
strOptions default = "20,5;21,20;50,5"
             │           │      │
             optId=20    optId=21  optId=50
             value=strOption[0] của mỗi option
```

---

## 4. Config item_template (các loại đá)

### Thêm loại đá nâng cấp mới

```sql
-- Giả sử muốn thêm Đá Cấp 8 cho bậc +21~+25
INSERT INTO item_template (id, name, detail, isXepChong, gioiTinh, type, idClass, idIcon, levelNeed)
VALUES (31, 'Đá Nâng Cấp Cấp 8', 'Dùng để nâng cấp trang bị +21~+25',
        'True', 2, 21, 0, 0, 60);
```

### Tham chiếu type=21 (UpgradeStone)

| id | Tên | Dùng cho bậc |
|----|-----|-------------|
| 1 | Đá Nâng Cấp Cấp 1 | +1 ~ +3 |
| 2 | Đá Nâng Cấp Cấp 2 | +4 ~ +6 |
| 3 | Đá Nâng Cấp Cấp 3 | +7 ~ +9 |
| 4 | Đá Nâng Cấp Cấp 4 | +10 ~ +12 |
| 5 | Đá Nâng Cấp Cấp 5 | +13 ~ +15 |
| 6 | Đá Nâng Cấp Cấp 6 | +16 ~ +18 |
| 7 | Đá Nâng Cấp Cấp 7 | +19 ~ +20 |
| 8 | Đá May Mắn | +15% mỗi viên, max 1.0 |
| 9 | Đá Bảo Vệ | Bỏ qua fail_policy khi thất bại |
| 10 | Đá Hồi Phục | Khôi phục level về trước khi vỡ |

---

## 5. Server-side: Công thức tỉ lệ & xử lý thành/thất bại

### Tính tỉ lệ thành công

```csharp
// UpgradeService.cs (GameServerApi)

float CalculateRate(EquipmentUpgradeConfig cfg, int actualStones, int luckyStones)
{
    // Clamp số đá, không vượt stone_needed
    float stoneRatio = Math.Min((float)actualStones / cfg.StoneNeeded, 1.0f);
    float rate = cfg.BaseSuccessRate * stoneRatio;

    // Mỗi Đá May Mắn (id=8) cộng thêm 15%
    rate += luckyStones * 0.15f;

    return Math.Min(rate, 1.0f); // tối đa 100%
}
```

### Xử lý kết quả

```csharp
bool isSuccess = Random.NextDouble() < rate;

if (isSuccess)
{
    item.UpgradeLevel += 1;
    // Tái tính strOptions theo option_template.strOption[newLevel]
    item.StrOptions = RecalculateStrOptions(item, newLevel);
}
else
{
    bool hasProtection = usedProtectionStone; // id=9
    if (!hasProtection)
    {
        switch (cfg.FailPolicy)
        {
            case 1: item.UpgradeLevel = Math.Max(0, item.UpgradeLevel - 1); break;
            case 2: item.UpgradeLevel = 0; break;
            // case 0: không làm gì
        }
        if (item.UpgradeLevel < originalLevel) // bị vỡ
            item.StrOptions = RecalculateStrOptions(item, item.UpgradeLevel);
    }
    // Có Đá Bảo Vệ → giữ nguyên upgradeLevel
}

// Trừ bạc + đá (bao gồm Đá Bảo Vệ, Đá May Mắn nếu dùng)
player.Silver -= cfg.SilverCost;
```

### RecalculateStrOptions

```csharp
// Tái tính value cho từng option tại bậc mới
string RecalculateStrOptions(ItemInstance item, int upgradeLevel)
{
    var parts = new List<string>();
    foreach (var (optId, _) in ParseStrOptions(item.StrOptions))
    {
        var tmpl = db.OptionTemplates.Find(optId);
        int newValue = tmpl.GetValueAt(upgradeLevel); // strOption.Split(';')[upgradeLevel]
        parts.Add($"{optId},{newValue}");
    }
    return string.Join(";", parts);
}
```

---

## 6. Unity: UI Nâng Cấp (UpgradePanel)

### Giao diện cần có

```
┌─────────────────────────────────────┐
│  NÂNG CẤP TRANG BỊ                  │
│                                     │
│  [Icon]  Kiếm Hỏa Sơ Cấp           │
│          Bậc hiện tại: +3           │
│          Bậc mục tiêu: +4           │
│                                     │
│  Loại đá cần: Đá Nâng Cấp Cấp 2    │
│  Số đá tối thiểu: 2                 │
│  Số đá đề nghị : 5 (đạt 90%)       │
│                                     │
│  Nhập số đá: [____]  (Bạn có: 8)   │
│  Đá May Mắn: [____]  (Bạn có: 3)   │
│  ☐ Dùng Đá Bảo Vệ  (Bạn có: 1)    │
│                                     │
│  Tỉ lệ thành công: 72%  ████▒▒▒    │
│  Chi phí bạc: 8,000                 │
│                                     │
│  ⚠️ Từ +7 trở lên có thể vỡ!       │  ← ẩn nếu fail_policy=0
│                                     │
│       [HỦY]      [NÂNG CẤP]        │
└─────────────────────────────────────┘
```

### Logic UpgradePanel.cs

```csharp
void OnStoneCountChanged(int actualStones)
{
    var cfg = upgradeConfig; // lấy từ server cho bậc target
    
    // Kiểm tra đủ số tối thiểu
    if (actualStones < cfg.stone_min)
    {
        upgradeButton.interactable = false;
        warningText.text = $"Cần ít nhất {cfg.stone_min} viên đá!";
        return;
    }
    
    // Tính tỉ lệ
    float stoneRatio = Mathf.Min((float)actualStones / cfg.stone_needed, 1f);
    float rate = cfg.base_success_rate * stoneRatio;
    rate += luckyStoneCount * 0.15f;
    rate = Mathf.Min(rate, 1f);
    
    // Hiển thị
    rateText.text = $"Tỉ lệ thành công: {rate * 100:F0}%";
    rateBar.fillAmount = rate;
    
    // Cảnh báo vỡ
    failWarning.SetActive(cfg.fail_policy > 0 && !useProtectionStone);
    
    upgradeButton.interactable = true;
}

void OnUpgradeClicked()
{
    // Gửi request lên server
    var req = new UpgradeRequest {
        itemSlot         = currentSlot,       // "weapon"/"helmet"/...
        actualStones     = int.Parse(stoneInput.text),
        luckyStones      = int.Parse(luckyInput.text),
        useProtection    = protectionToggle.isOn
    };
    StartCoroutine(api.PostUpgrade(req, OnUpgradeResponse));
}

void OnUpgradeResponse(UpgradeResponse resp)
{
    if (resp.success)
        ShowEffect("Nâng cấp thành công! → +" + resp.newUpgradeLevel);
    else if (resp.downgraded)
        ShowEffect("Thất bại! Trang bị xuống +" + resp.newUpgradeLevel, Color.red);
    else
        ShowEffect("Thất bại! Trang bị giữ nguyên.", Color.yellow);
    
    // Refresh UI trang bị
    equipmentManager.Refresh(resp.equipment);
}
```

---

## 7. Unity: Hiển thị stat (ItemDetailPanel)

### Logic hiển thị dim/bright

```csharp
// ItemDetailPanel.cs

void DisplayStats(EquipmentItemDto item, List<OptionTemplateDto> allOptions)
{
    var equipped = EquippedOptionDisplay.ParseAll(item.strOptions);
    
    foreach (var opt in equipped)
    {
        var tmpl = allOptions.Find(o => o.id == opt.optionId);
        if (tmpl == null) continue;
        
        // Xây label: "(+4) HP tối đa: +79"
        string label = tmpl.BuildLabel(opt.value);
        
        // Màu: sáng nếu đang active, xám nếu chưa đạt cấp
        Color color = tmpl.IsActive(item.upgradeLevel) ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        
        // Tạo dòng UI
        var row = Instantiate(statRowPrefab, statContainer);
        row.GetComponent<TMP_Text>().text  = label;
        row.GetComponent<TMP_Text>().color = color;
    }
}
```

### Ví dụ trực quan

Trang bị đang ở **+3**:

```
Tấn công: +22              ← option.level=0  → BRIGHT (trắng)
HP tối đa: +40             ← option.level=0  → BRIGHT (trắng)
(+4) Tốc độ tấn công: +0  ← option.level=4  → DIM   (xám, chưa đạt)
(+8) Chí mạng: +0         ← option.level=8  → DIM   (xám, chưa đạt)
```

Trang bị đang ở **+5**:

```
Tấn công: +35              ← BRIGHT
HP tối đa: +63             ← BRIGHT
(+4) Tốc độ tấn công: +3% ← BRIGHT (đã đạt +4)
(+8) Chí mạng: +0         ← DIM    (chưa đạt +8)
```

---

## 8. Ví dụ nâng cấp step-by-step

**Tình huống:** Nâng Kiếm Hỏa Sơ Cấp từ **+3 → +4**

### Bước 1: Lấy config
```sql
SELECT * FROM equipment_upgrade_config WHERE upgrade_level = 4;
-- silver_cost=8000, stone_id=2, stone_needed=5, stone_min=2,
-- base_success_rate=0.90, fail_policy=0
```

### Bước 2: Kiểm tra điều kiện
```
Player có: silver=15000 ✓
Item đang ở: +3 ✓ (target là +4)
Player nhập: 4 viên Đá Cấp 2, 1 Đá May Mắn
Tính rate:
  stoneRatio = min(4/5, 1.0) = 0.80
  rate = 0.90 * 0.80 = 0.72
  rate += 1 * 0.15 = 0.87  → 87%
```

### Bước 3: Nâng cấp thành công (Random < 0.87)
```
item.upgradeLevel: 3 → 4
Tái tính strOptions:
  option id=1  (Tấn công):  strOption[4] = 28  → "1,28"
  option id=13 (Tốc độ ATK): strOption[4] = 3  → "13,3"
strOptions mới = "1,28;13,3"

Trừ bạc: 15000 - 8000 = 7000
Trừ đá: -4 viên Đá Cấp 2, -1 Đá May Mắn
```

### Bước 4: Server response → Unity refresh
```json
{
  "success": true,
  "newUpgradeLevel": 4,
  "equipment": {
    "weapon": { "id": 200, "upgradeLevel": 4, "strOptions": "1,28;13,3" }
  }
}
```

---

## 9. Bảng tham chiếu nhanh

### Tỉ lệ thành công khi dùng đủ đá (base_success_rate)

| Bậc | Tỉ lệ gốc | Đá cần | Đá tối thiểu | Vỡ? |
|-----|-----------|--------|-------------|-----|
| +1 | 100% | 3 | 1 | Không |
| +2 | 100% | 5 | 2 | Không |
| +3 | 95% | 8 | 3 | Không |
| +4 | 90% | 5 | 2 | Không |
| +5 | 85% | 7 | 3 | Không |
| +6 | 80% | 10 | 4 | Không |
| +7 | 75% | 5 | 2 | **Có (-1)** |
| +8 | 70% | 7 | 3 | **Có (-1)** |
| +9 | 65% | 10 | 4 | **Có (-1)** |
| +10 | 60% | 5 | 3 | **Có (-1)** |
| +12 | 50% | 10 | 4 | **Có (-1)** |
| +15 | 35% | 10 | 4 | **Có (-1)** |
| +18 | 25% | 10 | 5 | **Có (-1)** |
| +20 | 15% | 15 | 7 | **Có (-1)** |

### Công thức tóm tắt

```
actual_rate = base_rate × min(your_stones / stone_needed, 1.0)
            + lucky_stones × 0.15
            capped at 1.0 (100%)

Nếu thất bại và fail_policy=1 và không có Đá Bảo Vệ:
    item.upgradeLevel -= 1  (min 0)
```

### Option type → unlock bậc

| type | level | Ý nghĩa |
|------|-------|---------|
| 0 | 0 | Vũ khí – base, luôn active |
| 2 | 0 | Giáp/Nhẫn – base, luôn active |
| 3 | 4 | Mở khoá tại +4 |
| 4 | 8 | Mở khoá tại +8 |
| 5 | 12 | Mở khoá tại +12 |
| 6 | 16 | Mở khoá tại +16 |
