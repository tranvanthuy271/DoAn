# Hệ Thống Stat Nhân Vật — Hướng Dẫn

## 1. Tổng Quan Công Thức

```
final_stats = base_stats (info_char) + equipment_bonus (strOptions) + potential_bonus
```

| Thành phần | Nguồn dữ liệu | Ghi chú |
|---|---|---|
| `base_stats` | `player_data.info_char` (JSON) | Đã bao gồm gene bonus tích lũy |
| `equipment_bonus` | `player_data.equipment` → `strOptions` | Bonus từ trang bị đang mặc |
| `potential_bonus` | `player_data.potential_stats` (JSON) | Điểm tiềm năng người chơi phân bổ |

---

## 2. Base Stats (info_char)

Lưu dưới dạng JSON trong cột `player_data.info_char`. Các trường liên quan đến stat:

```json
{
  "MaxHp": 1000,
  "MaxMp": 300,
  "Attack": 80,
  "Defense": 40,
  "MoveSpeed": 5.0,
  "GeneTier": 3,
  "GeneExp": 0,
  "ElementType": "Fire",
  "Silver": 50000
}
```

> **Quan trọng:** Gene bonus được **cộng trực tiếp** vào `MaxHp`, `MaxMp`, `Attack`, `Defense` khi nâng tier gene thành công. Không có cột riêng cho gene bonus — chỉ số trong `info_char` đã phản ánh tất cả các lần nâng tier trước đó.

---

## 3. Equipment Bonus (strOptions)

### Format strOptions
```
"optId,value;optId,value;..."
```

Ví dụ: `"1,40;3,60"` → Attack +40, MaxHp +60

### Bảng optId

| optId | Stat | Ghi chú |
|---|---|---|
| 1 | Attack | Nguồn chính |
| 2 | Defense | Nguồn chính |
| 3 | MaxHp | |
| 4 | MoveSpeed | Giá trị float |
| 5 | Attack | Nguồn phụ (unlock ≥ +5) |
| 6 | Defense | Nguồn phụ (unlock ≥ +9) |

> Giá trị `strOptions` được server tự động tính lại sau mỗi lần nâng cấp qua `RecalcStrOptions()`. Client không cần tính — chỉ cần đọc giá trị mới trong response.

---

## 4. Potential Bonus (potential_stats)

Lưu dưới dạng JSON trong cột `player_data.potential_stats`:

```json
{
  "max_hp": 200,
  "max_mp": 100,
  "attack": 30,
  "defense": 20,
  "move_speed": 0.5
}
```

---

## 5. Gene Upgrade System

### Flow nâng tier gene

```
Client gọi POST /api/gene/upgrade
    ↓
Server kiểm tra GeneExp đủ chưa (bảng gene_upgrade_config)
    ↓ đủ điều kiện
Server query stat bonus từ gene_tier_stat_config
    ↓
Server += bonus vào info.MaxHp, MaxMp, Attack, Defense
Server lưu info_char mới (bonus bị BAKE IN)
    ↓
Server trả về final_stats = StatCalculator.Compute(...)
    ↓
Client cập nhật UI
```

### Bảng gene_upgrade_config (chi phí nâng cấp)

Lưu điều kiện để nâng tier:

| Cột | Ý nghĩa |
|---|---|
| `element_type` | Hệ gene (Fire, Water, …) |
| `tier_from` | Tier hiện tại |
| `exp_required` | GeneExp cần thiết |
| `gold_cost` | Gold tiêu thụ |
| `item1_id`, `item1_count` | Nguyên liệu cần |

### Bảng gene_tier_stat_config (bonus stat theo tier)

Lưu stat được cộng thêm mỗi lần lên tier:

| Cột | Ý nghĩa |
|---|---|
| `element_type` | Hệ gene |
| `tier_to` | Tier đạt được (2, 3, 4, 5) |
| `hp_bonus` | MaxHp cộng thêm |
| `mp_bonus` | MaxMp cộng thêm |
| `attack_bonus` | Attack cộng thêm |
| `defense_bonus` | Defense cộng thêm |

### Thêm/chỉnh sửa config gene stat

```sql
-- Thay đổi bonus cho hệ Fire tier 3
UPDATE gene_tier_stat_config
SET hp_bonus = 500, attack_bonus = 60
WHERE element_type = 'Fire' AND tier_to = 3;

-- Thêm hệ gene mới (ví dụ: Lightning)
INSERT INTO gene_tier_stat_config VALUES
  ('Lightning', 2,  210,  60,  28,  9),
  ('Lightning', 3,  420, 120,  56, 18),
  ('Lightning', 4,  840, 240, 112, 35),
  ('Lightning', 5, 1560, 450, 200, 70);
```

> Thay đổi DB có hiệu lực ngay lần upgrade tiếp theo — không cần restart server.

---

## 6. StatCalculator (Models/Services/StatCalculator.cs)

Service tính `final_stats` từ 3 nguồn:

```csharp
var fs = StatCalculator.Compute(infoChar, equipmentJson, potentialStatsJson);
// fs.Hp, fs.MaxHp, fs.Mp, fs.MaxMp, fs.Attack, fs.Defense, fs.MoveSpeed
```

**Được gọi ở:**
- `PlayerController.POST /create` — trả về final_stats khi tạo/login nhân vật
- `PlayerController.GET /{id}/data` — trả về final_stats khi load dữ liệu
- `GeneController.POST /api/gene/upgrade` — trả về final_stats sau khi nâng gene
- `UpgradeController.POST /api/upgrade/equipment` — trả về final_stats sau khi nâng trang bị

---

## 7. Response Format final_stats

Tất cả các endpoint trên đều trả về cùng format:

```json
{
  "final_stats": {
    "hp": 950,
    "max_hp": 1200,
    "mp": 280,
    "max_mp": 400,
    "attack": 130,
    "defense": 75,
    "move_speed": 5.5
  }
}
```

- `hp` = `max_hp` khi server trả về (client hiển thị HP hiện tại từ NetworkPlayerHealth, không từ đây)
- `move_speed` = base `5.0` + equipment SPD bonus + potential MoveSpeed

---

## 8. Client Unity — Cập Nhật UI Khi Nhận Stats

### Script nhận response và update UI

```csharp
// Sau khi nhận response từ server (PlayerController GET /{id}/data):
void ApplyFinalStats(JsonNode finalStats)
{
    int maxHp    = finalStats["max_hp"].GetValue<int>();
    int maxMp    = finalStats["max_mp"].GetValue<int>();
    int attack   = finalStats["attack"].GetValue<int>();
    int defense  = finalStats["defense"].GetValue<int>();
    float spd    = finalStats["move_speed"].GetValue<float>();

    // Cập nhật UI panels
    statsPanel.SetStats(maxHp, maxMp, attack, defense, spd);
    
    // Cập nhật NetworkPlayerHealth nếu là local player
    if (NetworkManager.Singleton.IsServer)
        playerHealth.MaxHp.Value = maxHp;
}
```

### Khi nào client cần reload stats

| Sự kiện | Endpoint gọi | Cần update UI |
|---|---|---|
| Login / load game | `GET /api/player/{id}/data` | ✅ |
| Nâng cấp gene | `POST /api/gene/upgrade` | ✅ response có `final_stats` |
| Nâng cấp trang bị | `POST /api/upgrade/equipment` | ✅ response có `final_stats` |
| Phân bổ điểm tiềm năng | (API riêng) | ✅ cần gọi lại GET data |

---

## 9. Thêm Stat Mới

Ví dụ: thêm stat `critical_rate`:

1. **`InfoChar`**: Thêm field `CriticalRate` vào `Models/Entities/InfoChar.cs`
2. **`GeneTierStatConfig`**: Thêm cột `critical_bonus` vào DB và entity
3. **`StatCalculator`**: Thêm `CriticalRate` vào `FinalStats` class và tính toán trong `Compute()`
4. **Tất cả controllers**: Thêm `critical_rate = fs.CriticalRate` vào response `final_stats`
5. **Client**: Đọc `critical_rate` từ response và cập nhật UI
