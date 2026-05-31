## 3.1.5. Hệ thống Gene chính, Gene phụ, Hybrid Fusion và khắc chế nguyên tố

Hệ Gene là phần gameplay trọng tâm của project DoAn. Mỗi nhân vật có Gene chính tương ứng với hệ nguyên tố được chọn khi tạo nhân vật, có thể mở Gene phụ, nâng tier và dung hợp Hybrid khi đạt điều kiện. Toàn bộ cấu hình chi phí, tỉ lệ thành công, vật phẩm yêu cầu, bonus stat và kỹ năng mở khóa được tải từ backend qua REST API, không hard-code trong Unity.

### a) Nâng cấp Gene chính

UI Gene (`GeneUpgradePanel`) hiển thị tier hiện tại, EXP Gene, item yêu cầu, tỉ lệ thành công, bonus stat dự kiến và kỹ năng sẽ mở khóa. Khi người chơi xác nhận nâng cấp, UI không gọi API trực tiếp mà gửi lệnh qua `ServerRpc` để zone server kiểm tra phiên và gọi backend — đảm bảo không có client nào tự sửa dữ liệu Gene mà không qua kiểm tra server.

### b) Chọn và nâng Gene phụ

Luồng Gene phụ sử dụng cấu hình `gene_multi_config` từ backend để xác định hệ phụ hợp lệ, chi phí, vật phẩm yêu cầu và tỉ lệ nâng cấp. Hệ phụ được cố định theo cặp thiết kế sẵn trong `ElementHelper.GetFixedSecondary()` và `GeneController.PartnerMap`: Hỏa ↔ Thổ, Thủy ↔ Mộc, Kim ↔ Phong. Khi nâng Gene phụ thành công, một phần bonus stat được cộng thêm vào nhân vật.

### c) Dung hợp Hybrid

UI Hybrid hiển thị điều kiện fuse (cả Gene chính lẫn Gene phụ phải đạt Tier 5), item yêu cầu, vàng, tên Hybrid, prefab path, hệ bị khắc và hệ được miễn/giảm khắc. Khi fuse thành công, backend cập nhật các trường sau trong bảng `info_char`:

- `IsHybrid`, `HybridId`, `HybridPrefabPath` — định danh Hybrid và prefab hiển thị
- `HybridElementA`, `HybridElementB` — hai hệ nguyên tố tổng hợp
- `HybridBonusTargets` — danh sách hệ bị khắc được hưởng bonus tấn công
- `HybridImmuneElements` — danh sách hệ mà Hybrid miễn/giảm bị khắc
- `HybridAtkBonusPct` — phần trăm tăng sát thương khi tấn công hệ `HybridBonusTargets`

### d) Luồng nâng Gene qua server (Server-Authoritative)

Client không tự sửa dữ liệu Gene. Luồng xử lý từ UI đến DB diễn ra như sau:

1. UI `GeneUpgradePanel` gọi `GameplayCommandService.Instance.UpgradeGeneServerRpc(requestJson)`
2. Zone server nhận lệnh trong `UpgradeGeneServerRpc`, lấy JWT của client từ session runtime qua `ResolveClientJwt(cid)`
3. Server gọi `POST /api/gene/upgrade` với JWT đính kèm
4. Backend xác thực, kiểm tra điều kiện, trừ vật phẩm/vàng, nâng tier trong DB rồi trả JSON kết quả
5. Server gửi kết quả về **đúng client** bằng targeted `ClientRpc`: `GeneUpgradeResultClientRpc(json, Target(cid))`
6. Client nhận sự kiện `OnGeneUpgraded`, cập nhật dữ liệu nhân vật cục bộ
7. Client gửi `ServerRpc` đồng bộ chỉ số mới để các `NetworkVariable` trong zone được cập nhật

Đoạn mã dưới đây là phần cốt lõi của luồng này, rút từ `GameplayCommandService.cs` chạy trên Unity zone server:

```csharp
/// <summary>Nâng cấp gene. requestJson: {"player_id":N,"element_type":"Fire",...}</summary>
[ServerRpc(RequireOwnership = false)]
public void UpgradeGeneServerRpc(string requestJson, ServerRpcParams rpcParams = default)
{
    if (!IsServer) return;
    ulong cid = rpcParams.Receive.SenderClientId;
    string jwt = ResolveClientJwt(cid);
    StartCoroutine(DoPost(
        $"{ApiBase}/gene/upgrade", requestJson, jwt,
        json => GeneUpgradeResultClientRpc(json, Target(cid)),
        err  => GeneUpgradeResultClientRpc(ErrorJson(err), Target(cid))
    ));
}

[ClientRpc]
private void GeneUpgradeResultClientRpc(string json, ClientRpcParams p = default)
    => OnGeneUpgraded?.Invoke(json);
```

Cơ chế `Target(cid)` đảm bảo kết quả nâng Gene chỉ gửi về đúng client đã yêu cầu, không broadcast cho toàn zone.

---

## Cơ chế khắc chế Gene và tính sát thương nguyên tố

Bên cạnh việc tăng chỉ số, Gene lưu thông tin hệ của người chơi để phục vụ UI, chọn Gene phụ, Hybrid Fusion và các hàm hỗ trợ khắc chế. Dữ liệu hệ của người chơi nằm trong `info_char` gồm `element_type`, `secondary_element`, `is_hybrid`, `hybrid_bonus_targets`, `hybrid_immune_elements` và `hybrid_atk_bonus_pct`. Dữ liệu hệ của quái/boss có trong bảng `enemy` gồm `element_type`, `khang_hoa`, `khang_thuy`, `khang_tho`, `khang_moc`, `khang_kim`, `khang_phong` và `counter_rate`; tuy nhiên cần phân biệt rõ dữ liệu CSDL/API với dữ liệu đã được runtime combat sử dụng trực tiếp.

### a) Quan hệ hệ và cặp Hybrid

`ElementHelper.GetCounteredElement()` định nghĩa vòng khắc chế Ngũ Hành ở mức helper: Kim khắc Mộc, Mộc khắc Thủy, Thủy khắc Hỏa, Hỏa khắc Thổ, Thổ khắc Kim; hệ Phong không nằm trong vòng này và trả về `null`. Phần chọn Gene phụ và Hybrid không dùng toàn bộ vòng này mà dùng cặp cố định trong `ElementHelper.GetFixedSecondary()` và `GeneController.PartnerMap`:

| Gene chính | Gene phụ/Hybrid cố định |
|---|---|
| Hỏa | Thổ |
| Thổ | Hỏa |
| Thủy | Mộc |
| Mộc | Thủy |
| Kim | Phong |
| Phong | Kim |

Đây là logic đang được backend kiểm tra khi gọi API Hybrid Fusion.

### b) Tăng sát thương theo AttackBuff

**Đòn đánh thường (`PlayerCombat.PerformAttack`):** Hệ thống lấy `baseDamage` từ `PlayerStats`, sau đó kiểm tra buff đang hoạt động qua `ActiveBuffManager`. Nếu `AttackBuff` đang kích hoạt, damage được nhân lên theo hệ số buff. `GetBonusPct("AttackBuff")` trả về dạng thập phân: value = 15 → trả 0.15.

```csharp
int damage = stats.baseDamage;
if (ActiveBuffManager.Instance != null)
{
    float attackBonusPct = ActiveBuffManager.Instance.GetBonusPct("AttackBuff");
    if (attackBonusPct > 0f)
        damage = Mathf.RoundToInt(damage * (1f + attackBonusPct));
}
```

**Projectile (`FireballDamage`):** Component `FireballDamage` có biến `attackBonusPercent` và công thức áp dụng bonus khi tính `finalDamage`:

```csharp
int finalDamage = damage + damage * attackBonusPercent / 100;
```

`attackBonusPercent` được thiết lập từ bên ngoài qua `SetAttackBonus(int bonusPercent)`. Trong `PlayerSkillManager.SpawnProjectile()`, sau khi đặt damage cho projectile, AttackBuff của owner được đọc từ `ActiveBuffManager` và truyền vào component ngay trước khi projectile hoạt động:

```csharp
var fireballDmg = projectile.GetComponent<FireballDamage>();
if (fireballDmg != null)
{
    fireballDmg.SetOwner(ownerId);
    if (skill.currentEffectValue > 0f) fireballDmg.SetDamage((int)skill.currentEffectValue);
    // Apply AttackBuff của owner vào projectile (giống PlayerCombat.PerformAttack)
    if (ActiveBuffManager.Instance != null)
    {
        int atkBonusPct = Mathf.RoundToInt(ActiveBuffManager.Instance.GetBonusPct("AttackBuff") * 100f);
        if (atkBonusPct > 0) fireballDmg.SetAttackBonus(atkBonusPct);
    }
}
```

Nhờ đó, cơ chế buff tấn công hoạt động nhất quán cho cả **đòn đánh tay** (`PlayerCombat`) lẫn **kỹ năng phóng đạn** (`PlayerSkillManager` + `FireballDamage`).

### c) Kháng nguyên tố của mục tiêu

Công thức kháng nguyên tố xuất hiện trong ba nhánh runtime, mỗi nhánh dùng nguồn dữ liệu khác nhau:

**Quái dùng `MobPatrolAI.TakeDamageWithElement()`:** Hệ nguyên tố truyền vào là số nguyên — 1=Hỏa, 2=Thủy, 3=Thổ, 4=Mộc, 5=Kim, 6=Phong. Runtime lấy kháng từ các field trực tiếp trên component (`khangHoa`, `khangThuy`, …), không truy vấn DB trong hàm này.

$$\text{actual} = \max\!\bigl(1,\; \text{Round}\!\bigl(\text{rawDamage} \times (1 - \tfrac{\text{resist}}{100})\bigr)\bigr)$$

Nếu mục tiêu đang bị trạng thái `isWeakened`, `MobPatrolAI` nhân thêm hệ số suy yếu:

$$\text{actual} = \text{Round}(\text{actual} \times 1.3)$$

**Boss dùng `BossController.HandleBeforeTakeDamage()`:** Boss có thể né đòn trước qua `TryDodge()`; nếu né thành công, `finalDamage = 0`. Nếu không né, `BossController` lấy kháng từ `BossData` theo `elementType` dạng chuỗi (`"Hoa"`, `"Thuy"`, `"Tho"`, `"Moc"`, `"Kim"`, `"Phong"`) rồi tính:

$$\text{finalDamage} = \max\!\bigl(1,\; \text{Round}\!\bigl(\text{rawDamage} \times (1 - \tfrac{\text{resist}}{100})\bigr)\bigr)$$

**Enemy trong dungeon qua `NetworkEnemyHealth.TakeDamageInternal()`:** Hàm này chỉ nhận `damage` số, không nhận `element` — không có nhánh kháng nguyên tố ở đây. Nếu enemy có `DungeonEnemyRuntimeStats`, damage được giảm theo phòng thủ:

$$\text{damage} = \max(1,\; \text{rawDamage} - \text{Defense})$$

Đoạn mã dưới đây là toàn bộ hàm nhận sát thương nguyên tố của quái (`MobPatrolAI`), sinh ra các công thức trên trực tiếp trong runtime:

```csharp
public void TakeDamageWithElement(int rawDamage, int element = 0)
{
    if (evasionRate > 0 && UnityEngine.Random.Range(0f, 100f) < evasionRate)
    {
        ShowFloatingText("Miss!");
        return;
    }

    float resist = GetResistance(element);
    int actual = Mathf.Max(1,
        Mathf.RoundToInt(rawDamage * (1f - resist / 100f)));

    if (isWeakened)
        actual = Mathf.RoundToInt(actual * 1.3f);

    _health.TakeDamage(actual);

    if (counterRate > 0 &&
        UnityEngine.Random.Range(0f, 100f) < counterRate)
    {
        StartCoroutine(CounterAttack());
    }
}
```

### d) Phản đòn của quái đặc biệt

Runtime phản đòn nằm trong `MobPatrolAI`. Sau khi nhận sát thương qua `TakeDamageWithElement()`, nếu `counterRate > 0` và số ngẫu nhiên trong khoảng [0, 100) nhỏ hơn `counterRate`, quái kích hoạt `CounterAttack()` với công thức:

$$\text{counterDmg} = \max\!\bigl(1,\; \text{Round}(\text{baseDamage} \times 0.6)\bigr)$$

Cột `counter_rate` trong bảng `enemy` hiện được backend map ra API, nhưng DTO spawn network hiện chưa gán cột này vào field `MobPatrolAI.counterRate`. Vì vậy, phản đòn là cơ chế runtime của component, không mặc định áp dụng cho mọi dòng enemy có `counter_rate` trong DB.

### e) Cơ chế chiến đấu Hybrid đã được triển khai runtime

Sau khi xác nhận thiếu sót qua rà soát code, hai tính năng Hybrid Gene đã được triển khai đầy đủ vào hệ thống chiến đấu:

**e.1 — Kháng nguyên tố (HybridImmuneElements)**

Khi một nguồn sát thương có `attackerElement` truyền vào `NetworkPlayerHealth.TakeDamageWithElement()`, phương thức này chạy server-side và thực hiện theo thứ tự:

1. Tra cứu `PlayerDataResponse` của người chơi phòng thủ qua `ServerPlayerDataManager.GetPlayerDataByClientId(OwnerClientId)`.
2. Tính `counterOf = ElementHelper.GetElementThatCounters(pd.element_type)` — hệ nào khắc hệ của người chơi trong vòng Ngũ Hành.
3. Nếu `attackerElement == counterOf`:
   - Kiểm tra `ElementHelper.IsImmuneToCounter(attackerElement, pd)` — tra `hybrid_immune_elements` (CSV).
   - **Nếu miễn kháng** (người chơi Hybrid): giữ nguyên sát thương, bỏ qua hiệu ứng khắc hệ.
   - **Nếu không miễn**: áp dụng hệ số `×1,3` (khắc hệ).

```csharp
// NetworkPlayerHealth.cs — TakeDamageWithElementInternal()
string counterOf = ElementHelper.GetElementThatCounters(pd.element_type);
bool isCountered = string.Equals(attackerElement, counterOf, OrdinalIgnoreCase);
if (isCountered)
{
    if (ElementHelper.IsImmuneToCounter(attackerElement, pd))
        Debug.Log("[Hybrid Immune] counter blocked");
    else
        finalDamage = Mathf.RoundToInt(rawDamage * 1.3f);
}
```

Nguồn sát thương hiện chuyển sang gọi `TakeDamageWithElement()`:
- `MobPatrolAI.CounterAttack()` — truyền `elementType` (field mới được thêm).
- `EnemyAI.ApplyDamageToTarget()` — nhận tham số `attackerElement` (optional, mặc định `null`).

**e.2 — Tăng sát thương khắc hệ (HybridBonusTargets + HybridAtkBonusPct)**

Khi người chơi Hybrid tấn công một enemy có hệ thuộc `hybrid_bonus_targets` (CSV), sát thương được nhân hệ số bonus:

$$\text{finalDamage} = \text{Round}\!\Bigl(\text{damage} \times \Bigl(1 + \frac{\text{hybrid\_atk\_bonus\_pct}}{100}\Bigr)\Bigr)$$

Tính năng này được áp dụng trước khi gọi `TakeDamage()` ở hai nhánh:

- **Đánh thường** (`PlayerCombat.PerformAttack`): Trước vòng lặp hit-enemy, cache `myPlayerData = GameManager.Instance.GetPlayerData()`. Trong vòng lặp, tra `NetworkEnemyHealth.ElementType` và kiểm tra `ElementHelper.IsInCsvList(enemyElement, hybrid_bonus_targets)`.

- **Projectile** (`FireballDamage.OnTriggerEnter2D`): Chạy trên server, tra cứu `PlayerDataResponse` của owner qua `ServerPlayerDataManager.GetPlayerDataByClientId(ownerNetObj.OwnerClientId)` trước khi gọi `networkEnemyHealth.TakeDamage()`.

Công thức phụ trợ mới bổ sung vào `ElementHelper`:
- `GetElementThatCounters(string)` — nghịch đảo của `GetCounteredElement()`: trả về hệ khắc hệ đầu vào.
- `IsInCsvList(string element, string csvList)` — kiểm tra element có trong chuỗi CSV.

---

### Tổng hợp công thức có bằng chứng runtime trực tiếp

| Nhánh | Công thức |
|---|---|
| Đánh thường (PlayerCombat) | `damage = baseDamage`; nếu AttackBuff: `damage = Round(damage × (1 + attackBonusPct))`; nếu Hybrid bonus: `damage = Round(damage × (1 + hybrid_atk_bonus_pct/100))` |
| Projectile (FireballDamage) | `finalDamage = damage + damage × attackBonusPercent / 100`; nếu Hybrid bonus: `×(1 + hybrid_atk_bonus_pct/100)` |
| Quái nhận damage nguyên tố (MobPatrolAI) | `actual = Max(1, Round(raw × (1 − resist/100)))`; nếu isWeakened: `× 1.3` |
| Boss nhận damage (BossController) | Nếu né: `0`; nếu không: `Max(1, Round(raw × (1 − resist/100)))` |
| Enemy dungeon (NetworkEnemyHealth) | `Max(1, rawDamage − Defense)` |
| Phản đòn quái (MobPatrolAI) | `Max(1, Round(baseDamage × 0.6))` truyền `elementType` vào TakeDamageWithElement |
| Nhận damage có nguyên tố (NetworkPlayerHealth) | Nếu bị khắc hệ: `×1.3`; nếu Hybrid miễn kháng: `×1.0` (giữ nguyên) |
