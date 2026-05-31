# 3.1.6. Cơ chế khắc chế Gene và tính sát thương nguyên tố

Bên cạnh việc tăng chỉ số nhân vật, Gene còn lưu thông tin hệ để phục vụ giao diện, logic chọn Gene phụ, Hybrid Fusion và các nhánh tính sát thương nguyên tố. Dữ liệu hệ của người chơi nằm trong bảng `info_char`, gồm các cột `element_type`, `secondary_element`, `is_hybrid`, `hybrid_bonus_targets`, `hybrid_immune_elements` và `hybrid_atk_bonus_pct`. Dữ liệu hệ của quái và boss lưu trong bảng `enemy` qua các cột `element_type`, `khang_hoa`, `khang_thuy`, `khang_tho`, `khang_moc`, `khang_kim`, `khang_phong`, `tang_dame_*`, `counter_rate`. Cần phân biệt rõ dữ liệu CSDL/API với dữ liệu đã được runtime combat áp dụng: `EnemyController` trả về toàn bộ cột kháng/tăng sát thương/phản đòn qua API, nhưng DTO spawn quái qua network chưa truyền `tang_dame_*` và `counter_rate` vào component `MobPatrolAI`. Vì vậy, các cột này không được đưa vào công thức sát thương runtime.

---

## a) Ánh xạ hệ nguyên tố và cặp Hybrid cố định

`ElementHelper.GetCounteredElement()` định nghĩa vòng khắc chế Ngũ Hành:
Kim khắc Mộc, Mộc khắc Thủy, Thủy khắc Hỏa, Hỏa khắc Thổ, Thổ khắc Kim.
Hệ Phong không tham gia vòng này và trả về `null`.

Phần chọn Gene phụ và Hybrid Fusion không dùng toàn bộ vòng khắc mà dùng cặp cố định trong `ElementHelper.GetFixedSecondary()` và `GeneController.PartnerMap`: **Hỏa ↔ Thổ**, **Thủy ↔ Mộc**, **Kim ↔ Phong**. Đây là logic backend kiểm tra khi gọi API Hybrid Fusion.

---

## b) Sát thương người chơi gây ra — `DamageCalculator.CalcPlayerAttackDamage()`

Toàn bộ sát thương người chơi gây ra (đánh thường qua `PlayerCombat` và projectile qua `FireballDamage`) đều đi qua hàm tập trung `DamageCalculator.CalcPlayerAttackDamage()` với hai lớp điều chỉnh:

**Lớp 1 — AttackBuff:** `ActiveBuffManager.Instance.GetBonusPct("AttackBuff")` trả về tổng hệ số buff dạng thập phân (ví dụ: `value = 15` → `0.15`).

**Lớp 2 — Hybrid Gene bonus:** Nếu người chơi là Hybrid (`is_hybrid = true`), `hybrid_atk_bonus_pct > 0` và hệ của mục tiêu nằm trong danh sách `hybrid_bonus_targets`, sát thương được nhân thêm theo `hybrid_atk_bonus_pct`.

Công thức đầy đủ:

```
damage = Round(baseDamage × (1 + attackBonusPct))
nếu người chơi là Hybrid VÀ hệ mục tiêu trong hybrid_bonus_targets:
    damage = Round(damage × (1 + hybrid_atk_bonus_pct / 100))
```

Đoạn code tương ứng trong `DamageCalculator.cs`:

```csharp
public static int CalcPlayerAttackDamage(
    int baseDamage, float attackBonusPct,
    PlayerDataResponse attackerData, string targetElementType)
{
    int damage = baseDamage;
    if (attackBonusPct > 0f)
        damage = Mathf.RoundToInt(damage * (1f + attackBonusPct));

    if (attackerData != null
        && attackerData.is_hybrid
        && attackerData.hybrid_atk_bonus_pct > 0f
        && !string.IsNullOrEmpty(attackerData.hybrid_bonus_targets)
        && !string.IsNullOrEmpty(targetElementType)
        && targetElementType != "None")
    {
        if (ElementHelper.IsInCsvList(targetElementType, attackerData.hybrid_bonus_targets))
            damage = Mathf.RoundToInt(damage * (1f + attackerData.hybrid_atk_bonus_pct / 100f));
    }
    return damage;
}
```

`PlayerCombat` gọi hàm này với `targetElement` lấy từ `NetworkEnemyHealth.ElementType` của từng enemy trong tầm đánh. `FireballDamage` cũng gọi hàm này khi trúng `NetworkEnemyHealth`, với `attackBonusPercent` được nạp bởi `SetAttackBonus()` từ buff **EarthAura** trước khi projectile được bắn ra.

---

## c) Quái thường nhận sát thương — `MobPatrolAI.TakeDamageWithElement()`

`PlayerCombat` gọi `networkEnemyHealth.TakeDamage(finalDamage, attackerId)` đối với enemy network thông thường; riêng quái dùng `MobPatrolAI` (non-network hoặc standalone) gọi trực tiếp `TakeDamageWithElement(rawDamage, element)`. Encoding hệ nguyên tố trong hàm này: `0 = không rõ, 1 = Hỏa, 2 = Thủy, 3 = Thổ, 4 = Mộc, 5 = Kim, 6 = Phong`.

Luồng xử lý:
1. Kiểm tra né (`evasionRate`) — nếu né thành công, hiển thị "Miss!" và thoát.
2. Lấy chỉ số kháng theo hệ qua `GetResistance(element)` (đọc từ field trên component, không đọc trực tiếp từ DB trong hàm này).
3. Gọi `DamageCalculator.CalcEnemyReceivedDamage(rawDamage, resist, isWeakened)` để tính sát thương thực tế.
4. Trừ máu qua `_health.TakeDamage(actual)`.
5. Kiểm tra phản đòn (`counterRate`) — nếu kích hoạt, khởi chạy `CounterAttack()`.

Công thức sát thương quái nhận:

```
actual = Max(1, Round(rawDamage × (1 − resistPct / 100)))
nếu isWeakened: actual = Round(actual × 1.3)
```

Đoạn code trong `DamageCalculator.cs`:

```csharp
public static int CalcEnemyReceivedDamage(int rawDamage, float resistPct, bool isWeakened)
{
    int actual = Mathf.Max(1, Mathf.RoundToInt(rawDamage * (1f - resistPct / 100f)));
    if (isWeakened)
        actual = Mathf.RoundToInt(actual * 1.3f);
    return actual;
}
```

---

## d) Boss nhận sát thương — `BossController.HandleBeforeTakeDamage()`

Boss xử lý sát thương qua sự kiện `OnBeforeTakeDamage` của `NetworkBossHealth`. `BossController` đăng ký handler này và thực hiện hai bước:

1. **Dodge check:** `TryDodge()` kiểm tra `dodgeChance` và `dodgeCooldown` từ `BossData`. Nếu né thành công, trả về `0`.
2. **Kháng nguyên tố:** `GetResistance(elementType)` tra cứu cột kháng tương ứng trên `BossData` theo English key không dấu (`"Hoa"`, `"Thuy"`, `"Tho"`, `"Moc"`, `"Kim"`, `"Phong"`).

Công thức sát thương boss nhận:

```
nếu TryDodge() = true: finalDamage = 0
ngược lại: finalDamage = Max(1, Round(rawDamage × (1 − resistPct / 100)))
```

---

## e) Dungeon enemy nhận sát thương — `DamageCalculator.CalcDungeonEnemyReceivedDamage()`

Với enemy thuộc dungeon có `DungeonEnemyRuntimeStats`, sát thương được giảm theo phòng thủ tuyến tính (không dùng công thức kháng % như quái thường):

```
damage = Max(1, rawDamage − Defense)
```

---

## f) Người chơi nhận sát thương nguyên tố — `DamageCalculator.CalcPlayerReceivedElementDamage()`

Khi nguồn tấn công có kèm hệ nguyên tố (ví dụ phản đòn quái, skill boss), `NetworkPlayerHealth.TakeDamageWithElementInternal()` gọi `DamageCalculator.CalcPlayerReceivedElementDamage()` để tính sát thương thực tế:

- Kiểm tra xem `attackerElement` có **khắc hệ** của người chơi hay không (dùng `ElementHelper.GetElementThatCounters()`).
- Nếu không khắc → `finalDamage = rawDamage`.
- Nếu khắc và người chơi là Hybrid **miễn** hệ đó (`ElementHelper.IsImmuneToCounter()` trả `true`) → `finalDamage = rawDamage`.
- Nếu khắc và không miễn → `finalDamage = Round(rawDamage × 1.3)`.

---

## g) Phản đòn của quái — `MobPatrolAI.CounterAttack()`

Sau khi nhận sát thương qua `TakeDamageWithElement()`, nếu `counterRate > 0` và số ngẫu nhiên `[0, 100)` nhỏ hơn `counterRate`, `CounterAttack()` được kích hoạt:

```
counterDmg = Max(1, Round(baseDamage × 0.6))
```

Sát thương phản đòn được giao qua `NetworkPlayerHealth.TakeDamageWithElement(counterDmg, elementType)`, trong đó `elementType` là hệ của con quái (dạng English key). Điều này cho phép người chơi Hybrid có `HybridImmuneElements` chứa hệ quái đó được miễn bị nhân 1.3×.

**Lưu ý quan trọng:** Cột `counter_rate` trong bảng `enemy` hiện được backend trả về qua API, nhưng DTO spawn network của quái chưa gán cột này vào `MobPatrolAI.counterRate`. Vì vậy trong runtime, cơ chế phản đòn hoạt động đúng chỉ khi `counterRate` được gán thủ công trên prefab từ Inspector, không tự động lấy từ database.

---

## h) Tổng hợp các công thức runtime đã xác nhận

Bảng dưới đây tổng hợp các công thức có bằng chứng trực tiếp trong code runtime:

| Nhánh | Công thức |
|---|---|
| Người chơi tấn công (đánh thường & projectile) | `damage = Round(baseDamage × (1 + attackBonusPct))`; nếu Hybrid và mục tiêu trong `hybrid_bonus_targets`: `damage = Round(damage × (1 + hybrid_atk_bonus_pct/100))` |
| Quái thường nhận nguyên tố | `actual = Max(1, Round(rawDamage × (1 − resist/100)))`; nếu `isWeakened`: `actual = Round(actual × 1.3)` |
| Boss nhận nguyên tố | `if TryDodge(): 0`; ngược lại: `Max(1, Round(rawDamage × (1 − resist/100)))` |
| Dungeon enemy nhận sát thương | `Max(1, rawDamage − Defense)` |
| Người chơi nhận nguyên tố | `rawDamage` bình thường; nếu bị khắc và không miễn: `Round(rawDamage × 1.3)` |
| Phản đòn quái | `Max(1, Round(baseDamage × 0.6))` |
