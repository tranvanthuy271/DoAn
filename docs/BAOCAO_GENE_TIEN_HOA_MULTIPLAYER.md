# Hệ Thống Tiến Hóa Gene trong Game Multiplayer

## 1. Tổng quan

Hệ thống **Tiến Hóa Gene** (Gene Evolution) là một cơ chế phát triển nhân vật dài hạn được tích hợp vào game nhập vai multiplayer sử dụng Unity + Netcode for GameObjects (NGO) ở phía client và ASP.NET Core Web API + MySQL ở phía server. Hệ thống gồm ba giai đoạn phát triển:

- **Giai đoạn 1 — Nâng cấp Gene Chính** (Primary Gene Upgrade): nâng Tier 1 → 5 cho hệ nguyên tố chính của nhân vật.
- **Giai đoạn 2 — Nâng cấp Gene Phụ** (Secondary Gene Upgrade): sau khi gene chính đạt Tier đủ điều kiện, người chơi chọn một hệ thứ hai và nâng độc lập.
- **Giai đoạn 3 — Hybrid Fusion**: khi cả hai gene đều đạt Tier 5, nhân vật có thể hợp nhất tạo ra danh hiệu Hybrid với bộ kỹ năng đặc biệt.

---

## 2. Thiết kế dữ liệu (Database)

Hệ thống dùng bốn bảng cấu hình riêng biệt, tách hoàn toàn khỏi logic code:

| Bảng | Mô tả |
|---|---|
| `gene_upgrade_config` | Chi phí, tỉ lệ thành công, item cần thiết cho từng Tier của gene chính |
| `gene_multi_config` | Tương tự nhưng dành cho gene phụ (chi phí cao hơn ~20%) |
| `gene_tier_stat_config` | Chỉ số HP / MP / ATK / DEF tăng thêm khi đạt từng Tier |
| `gene_hybrid_config` | 10 tổ hợp Hybrid (chọn 2 trong 5 hệ), kỹ năng đặc biệt, miễn nhiễm |
| `gene_hybrid_skill` | Kỹ năng riêng của từng Hybrid ID |

Mỗi nhân vật lưu trạng thái gene trong bảng `player_data` (trường `info_char` JSON):
```
gene_tier, gene_exp,
secondary_element, secondary_gene_tier, secondary_gene_exp,
is_hybrid, hybrid_element_a, hybrid_element_b,
hybrid_bonus_targets, hybrid_immune_elements, hybrid_atk_bonus_pct
```

---

## 3. Luồng xử lý Multiplayer

### 3.1 Kiến trúc tổng thể

Hệ thống sử dụng kiến trúc **Hybrid Boundary** được chia thành hai vùng rõ ràng:

- **Pre-game** (Login, đăng ký, chọn nhân vật): Client gọi REST API trực tiếp, không qua NGO.
- **In-game** (mọi thao tác trong trận, bao gồm nâng gene chính): Client bắt buộc đi qua `GameplayCommandService` — một `NetworkBehaviour` singleton chạy trên máy host.

#### Luồng "Lấy Config Gene" (GetGeneConfig)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  UNITY CLIENT                                                               │
│                                                                             │
│  GeneUpgradePanel.cs — phương thức LoadGeneConfig() (Coroutine)            │
│                                                                             │
│  ① Đăng ký callback trước khi gọi:                                         │
│     GameplayCommandService.OnGeneConfigReceived -= HandleGeneConfig;        │
│     GameplayCommandService.OnGeneConfigReceived += HandleGeneConfig;        │
│                                                                             │
│  ② Gọi ServerRpc:                                                           │
│     GameplayCommandService.Instance                                         │
│         .GetGeneConfigServerRpc(element_type, gene_tier);                   │
│                                                                             │
│  [NGO Transport — UDP packet gửi lên host]                                  │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │  [ServerRpc packet]
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  HOST / DEDICATED SERVER — GameplayCommandService.cs                        │
│                                                                             │
│  ③ Nhận ServerRpc, kiểm tra quyền:                                         │
│     [ServerRpc(RequireOwnership = false)]                                   │
│     public void GetGeneConfigServerRpc(string elementType, int tier,        │
│                                         ServerRpcParams rpcParams)          │
│     {                                                                       │
│         if (!IsServer) return;   // guard: chỉ chạy trên server             │
│         ulong cid  = rpcParams.Receive.SenderClientId;                      │
│                                                                             │
│  ④ Lấy JWT từ session (không dùng JWT của client gửi lên):                 │
│         string jwt = ResolveClientJwt(cid);                                 │
│         // → ZonePlayerSessionManager.GetClientJwt(clientId)               │
│         // JWT được lưu khi player kết nối, KHÔNG đi qua NGO payload       │
│                                                                             │
│  ⑤ Gọi REST API nội bộ bằng coroutine DoGet():                             │
│         StartCoroutine(DoGet(                                               │
│             $"{ApiBase}/gene/config?elementType={escaped}&tier={tier}",     │
│             jwt,                                                            │
│             json => SendGeneConfigClientRpc(json, Target(cid)),             │
│             err  => SendGeneConfigClientRpc(ErrorJson(err), Target(cid))    │
│         ));                                                                 │
│     }                                                                       │
│                                                                             │
│  [HTTP/HTTPS + Authorization: Bearer {jwt}]                                 │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │  [REST GET request]
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  ASP.NET Core — GeneController.cs                                           │
│                                                                             │
│  ⑥ Validate JWT + xử lý request:                                           │
│     [HttpGet("config")]                                                     │
│     [Authorize]    ← ASP.NET tự validate JWT, reject nếu invalid           │
│     public async Task<IActionResult> GetConfig(                             │
│         [FromQuery] string elementType, [FromQuery] int tier)               │
│     {                                                                       │
│         var cfg = await _db.GeneUpgradeConfigs                              │
│             .FirstOrDefaultAsync(c => c.TierFrom == tier                   │
│                                    && c.ElementType == elementType);        │
│         var tierStat = await _db.GeneTierStatConfigs ...;                   │
│         var unlockSkills = await _db.SkillTemplates ...;                    │
│         return Ok(new { tierFrom, tierTo, geneExpRequired, ... });          │
│     }                                                                       │
│                                                                             │
│  [MySQL qua EF Core: gene_upgrade_config, gene_tier_stat_config]            │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │  [HTTP 200 JSON]
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  HOST — GameplayCommandService.cs — callback DoGet() onOk                  │
│                                                                             │
│  ⑦ Gửi về đúng 1 client (targeted ClientRpc):                              │
│     private ClientRpcParams Target(ulong clientId) => new ClientRpcParams  │
│     {                                                                       │
│         Send = new ClientRpcSendParams                                      │
│             { TargetClientIds = new[] { clientId } }   // chỉ 1 client     │
│     };                                                                      │
│                                                                             │
│     [ClientRpc]                                                             │
│     private void SendGeneConfigClientRpc(string json, ClientRpcParams p)   │
│         => OnGeneConfigReceived?.Invoke(json);                              │
│                                                                             │
│  [NGO Transport — packet chỉ gửi về client đã yêu cầu]                     │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │  [ClientRpc packet]
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  UNITY CLIENT — GeneUpgradePanel.cs                                         │
│                                                                             │
│  ⑧ Nhận event, parse JSON, cập nhật UI:                                    │
│     void HandleGeneConfig(string json)                                      │
│     {                                                                       │
│         GameplayCommandService.OnGeneConfigReceived -= HandleGeneConfig;   │
│         _config = JsonUtility.FromJson<GeneConfigDto>(json);                │
│         // → RefreshUI(): điền tierText, expBar, successRateText, ...       │
│     }                                                                       │
└─────────────────────────────────────────────────────────────────────────────┘
```

#### Luồng "Nâng Cấp Gene" (UpgradeGene)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  UNITY CLIENT — GeneUpgradePanel.cs — phương thức DoUpgrade() (Coroutine)  │
│                                                                             │
│  ① Tạo request object + subscribe callback:                                │
│     var request = new GeneUpgradeRequest                                   │
│         { playerId = _playerData.player_id, itemCount = itemCount };        │
│     GameplayCommandService.OnGeneUpgraded -= HandleGeneUpgraded;            │
│     GameplayCommandService.OnGeneUpgraded += HandleGeneUpgraded;            │
│                                                                             │
│  ② Serialize sang JSON và gọi ServerRpc:                                   │
│     GameplayCommandService.Instance                                         │
│         .UpgradeGeneServerRpc(JsonUtility.ToJson(request));                 │
│                                                                             │
│     yield return new WaitUntil(() => done);  // block UI chờ kết quả       │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  HOST — GameplayCommandService.cs                                           │
│                                                                             │
│  ③ [ServerRpc(RequireOwnership = false)]                                   │
│     public void UpgradeGeneServerRpc(string requestJson,                    │
│                                       ServerRpcParams rpcParams)            │
│     {                                                                       │
│         if (!IsServer) return;                                              │
│         ulong cid  = rpcParams.Receive.SenderClientId;                      │
│         string jwt = ResolveClientJwt(cid);                                 │
│                                                                             │
│  ④ Gọi REST POST /api/gene/upgrade:                                        │
│         StartCoroutine(DoPost(                                              │
│             $"{ApiBase}/gene/upgrade", requestJson, jwt,                    │
│             json => GeneUpgradeResultClientRpc(json, Target(cid)),          │
│             err  => GeneUpgradeResultClientRpc(ErrorJson(err), Target(cid)) │
│         ));                                                                 │
│     }                                                                       │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  ASP.NET Core — GeneController.cs — phương thức UpgradeGene()              │
│                                                                             │
│  ⑤ Kiểm tra điều kiện (server-side, không thể bypass):                     │
│     var player = await _db.PlayerData.FindAsync(playerId);                  │
│     if (info.GeneExp < cfg.GeneExpRequired) return BadRequest(...);         │
│     if (info.Gold < cfg.GoldCost) return BadRequest(...);                   │
│     if (availableItems < cfg.ItemsMin) return BadRequest(...);              │
│                                                                             │
│  ⑥ Tính tỉ lệ + Random TRÊN SERVER:                                       │
│     float successRate = cfg.BaseSuccessRate                                 │
│         * Math.Min((float)itemCount / cfg.ItemsNeeded, 1f);                 │
│     bool success = new Random().NextDouble() < successRate;                 │
│     // Client không biết kết quả trước bước này                            │
│                                                                             │
│  ⑦ Áp dụng kết quả, lưu DB:                                               │
│     if (success) { info.GeneTier++; info.MaxHp += tierStat.HpBonus; ... }  │
│     info.GeneExp -= cfg.GeneExpRequired;  info.Gold -= cfg.GoldCost;        │
│     await _db.SaveChangesAsync();                                           │
│     var finalStats = StatCalculator.Compute(info, equipJson, potJson);      │
│     return Ok(new { success, newGeneTier, newGeneExp, gold,                 │
│                     statBonus, final_stats, newlyUnlockedSkills });          │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  HOST — GameplayCommandService.cs                                           │
│                                                                             │
│  ⑧ [ClientRpc] GeneUpgradeResultClientRpc → OnGeneUpgraded?.Invoke(json)  │
└───────────────────────────────┬─────────────────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│  UNITY CLIENT — GeneUpgradePanel.cs — HandleGeneUpgraded()                  │
│                                                                             │
│  ⑨ Parse response + cập nhật local data (không reload từ server):          │
│     response = JsonUtility.FromJson<GeneUpgradeResponse>(resultJson);       │
│     _playerData.gene_tier = response.newGeneTier;                           │
│     _playerData.gene_exp  = response.newGeneExp;                            │
│     _playerData.gold      = response.gold;                                  │
│     // Hiển thị skill mới mở khoá, stat bonus, thông báo thành công/thất bại│
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Bảng tổng hợp — Từng bước ánh xạ sang file .cs cụ thể

#### GetGeneConfig (lấy config trước khi nâng)

| Bước | Hành động | File | Phương thức / Dòng quan trọng |
|---|---|---|---|
| ① | Subscribe event + gọi ServerRpc | `GeneUpgradePanel.cs` | `LoadGeneConfig()` — dòng 232–234 |
| ② | Serialize tham số, gửi qua NGO | `GeneUpgradePanel.cs` | `GetGeneConfigServerRpc(element_type, gene_tier)` |
| ③ | Nhận ServerRpc, guard `IsServer` | `GameplayCommandService.cs` | `GetGeneConfigServerRpc()` — `if (!IsServer) return` |
| ③ | Lấy JWT của client từ session | `GameplayCommandService.cs` | `ResolveClientJwt(cid)` → `ZonePlayerSessionManager.GetClientJwt()` |
| ④ | Gọi REST GET nội bộ | `GameplayCommandService.cs` | `StartCoroutine(DoGet($"{ApiBase}/gene/config?...", jwt, ...))` |
| ⑤ | DoGet: thêm Bearer header, gửi HTTP | `GameplayCommandService.cs` | `DoGet()` — `req.SetRequestHeader("Authorization", "Bearer " + jwt)` |
| ⑥ | Validate JWT + query DB | `GeneController.cs` | `GetConfig()` — `[Authorize]`, `_db.GeneUpgradeConfigs.FirstOrDefaultAsync(...)` |
| ⑦ | Targeted ClientRpc về đúng 1 client | `GameplayCommandService.cs` | `SendGeneConfigClientRpc(json, Target(cid))` — `Target()` dùng `TargetClientIds = new[] { clientId }` |
| ⑦ | Fire static event | `GameplayCommandService.cs` | `SendGeneConfigClientRpc` → `OnGeneConfigReceived?.Invoke(json)` |
| ⑧ | Nhận event, parse, cập nhật UI | `GeneUpgradePanel.cs` | `HandleGeneConfig()` → `JsonUtility.FromJson<GeneConfigDto>(json)` |

#### UpgradeGene (thực hiện nâng cấp)

| Bước | Hành động | File | Phương thức / Dòng quan trọng |
|---|---|---|---|
| ① | Tạo `GeneUpgradeRequest`, subscribe event | `GeneUpgradePanel.cs` | `DoUpgrade()` — dòng 450–469 |
| ② | Serialize JSON + gọi ServerRpc | `GeneUpgradePanel.cs` | `UpgradeGeneServerRpc(JsonUtility.ToJson(request))` |
| ③ | Nhận, resolve JWT | `GameplayCommandService.cs` | `UpgradeGeneServerRpc()` — `ResolveClientJwt(cid)` |
| ④ | Gọi REST POST | `GameplayCommandService.cs` | `DoPost($"{ApiBase}/gene/upgrade", requestJson, jwt, ...)` |
| ⑤–⑦ | Validate điều kiện, Random, lưu DB | `GeneController.cs` | `UpgradeGene()` — check exp/gold/item, `new Random().NextDouble() < successRate`, `_db.SaveChangesAsync()` |
| ⑧ | Targeted ClientRpc fire event | `GameplayCommandService.cs` | `GeneUpgradeResultClientRpc` → `OnGeneUpgraded?.Invoke(json)` |
| ⑨ | Nhận kết quả, update local playerData | `GeneUpgradePanel.cs` | `HandleGeneUpgraded()` — cập nhật `gene_tier`, `gene_exp`, `gold` tại chỗ |

### 3.3 Phân biệt Gene Chính vs Gene Phụ trong Multiplayer

| Đặc điểm | Gene Chính | Gene Phụ |
|---|---|---|
| Kênh truyền tin | ServerRpc → NGO → REST | REST trực tiếp (HTTP + JWT) |
| Lý do | Yêu cầu server-authoritative chặt chẽ | Ít xung đột, đơn giản hóa flow |
| File xử lý (client) | `GeneUpgradePanel.cs` | `SecondaryGeneUpgradePanel.cs` |
| JWT | Server tự lấy từ session | Client đính kèm trong header |

Gene phụ dùng REST trực tiếp vì đây là thao tác đơn lẻ, ít nguy cơ xung đột hơn gene chính (chỉ 1 lần nâng mỗi khi đủ điều kiện, không liên quan đến combat real-time).

### 3.4 Đặc điểm Multiplayer quan trọng

a) **Server-Authoritative hoàn toàn cho gene chính**: kết quả nâng cấp (thành công/thất bại) được `Random()` phía server, không phải client. Client không biết kết quả trước khi server trả về — không thể cheat tỉ lệ thành công.

b) **JWT không đi qua mạng NGO**: client không gửi JWT trong payload ServerRpc. Server giữ JWT của từng client trong `ZonePlayerSessionManager` (đọc từ quá trình xác thực kết nối). Điều này ngăn JWT bị sniff qua Unity transport.

c) **GeneExpBuff trong multiplayer**: khi nhân vật tiêu diệt kẻ thù, `PlayerController` trên server kiểm tra bảng `active_buffs` và cộng thêm % vào `gene_exp`. Buff `GeneExpBuff` được client đồng bộ lên host qua `SyncBuffBonusesServerRpc` (trong `InventoryNetworkBridge`) mỗi khi trạng thái buff thay đổi.

d) **Session cache**: `ZonePlayerSessionManager` giữ `gene_tier` và `gene_exp` trong bộ nhớ (`PlayerSession` struct) để server zone tính ngay sát thương nguyên tố trong combat mà không phải truy vấn DB mỗi frame.

e) **Tràn exp sang gene phụ**: khi gene chính đã đạt Tier 5 (max), mọi `gene_exp` kiếm được từ combat tự động cộng vào `secondary_gene_exp` — logic nằm hoàn toàn ở `PlayerController` server-side. Client không cần xử lý gì thêm, trải nghiệm liền mạch.

f) **Spawn với gene data**: khi player join vào zone, `ZonePlayerSessionManager.LoadAndSpawnPlayer()` fetch đầy đủ player data (bao gồm `gene_tier`, `gene_exp`, `secondary_gene_tier`…) từ REST API trước khi spawn `NetworkObject`. Khi spawn xong, `GameplayCommandService.PushSkillsToClient()` đẩy luôn danh sách skill (được lọc theo `gene_slot`) về client mà không cần client phải request riêng.

---

## 4. API Endpoints

| Method | Endpoint | Chức năng |
|---|---|---|
| GET | `/api/gene/config` | Lấy config nâng gene chính (tier, elementType) |
| POST | `/api/gene/upgrade` | Nâng cấp gene chính |
| GET | `/api/gene/list` | Xem toàn bộ trạng thái gene của player |
| POST | `/api/gene/secondary/select` | Chọn hệ gene phụ (1 lần duy nhất) |
| GET | `/api/gene/multi/config` | Lấy config nâng gene phụ |
| POST | `/api/gene/secondary/upgrade` | Nâng cấp gene phụ |
| GET | `/api/gene/hybrid/config` | Xem config Hybrid Fusion |
| POST | `/api/gene/hybrid/fuse` | Thực hiện Hybrid Fusion |

Tất cả endpoint đều yêu cầu JWT Bearer Token (`[Authorize]`).

---

## 5. Logic nâng cấp (Upgrade Flow)

### 5.1 Điều kiện nâng cấp gene chính

a) `gene_exp` hiện có ≥ `gene_exp_required` (cấu hình theo tier và hệ).

b) Vàng (`gold`) ≥ `gold_cost`.

c) Có đủ số item nguyên liệu (`stone_id`) trong túi đồ, tối thiểu `items_min` viên.

### 5.2 Tính tỉ lệ thành công

$$\text{successRate} = \text{baseSuccessRate} \times \min\!\left(\frac{\text{itemCount}}{\text{itemsNeeded}},\ 1\right)$$

Người chơi tự chọn `itemCount` trong khoảng `[itemsMin, itemsNeeded]` bằng thanh kéo (Slider) trong UI. Dùng nhiều item → tỉ lệ cao hơn.

### 5.3 Kết quả khi thành công

- `gene_tier` tăng 1.
- Chỉ số HP, MP, ATK, DEF tăng theo bảng `gene_tier_stat_config`.
- Nhân vật hồi đầy HP và MP ngay lập tức.
- Skill mới tương ứng với tier và hệ nguyên tố được mở khoá tự động (ghi vào `skills_json`).
- Server trả về `final_stats` (base + equipment + potential) để client cập nhật HUD ngay.

### 5.4 Kết quả khi thất bại

- `gene_exp` bị trừ đúng lượng `gene_exp_required` (không reset về 0 — thiết kế nhân từ).
- Vàng vẫn bị trừ.
- Item đã dùng vẫn bị tiêu.
- `gene_tier` không thay đổi.

### 5.5 Gene phụ (Secondary Gene)

- Stat bonus chỉ bằng **50%** so với gene chính (cùng bảng `gene_tier_stat_config`).
- Chi phí item cao hơn ~20% (bảng `gene_multi_config` riêng).
- Hệ phụ bắt buộc phải là đối tác cố định theo cặp ngũ hành:

| Hệ chính | Hệ phụ bắt buộc |
|---|---|
| Fire (Hỏa) | Earth (Thổ) |
| Water (Thủy) | Wood (Mộc) |
| Metal (Kim) | Wind (Phong) |

### 5.6 Hybrid Fusion

Khi cả hai gene đạt Tier 5:

- Tốn Vàng + "Lõi Đột Biến" (item fusion).
- Nhân vật nhận danh hiệu Hybrid (ví dụ: "Viêm Thổ Dị Nhân").
- Hưởng **dmg bonus** đối với các hệ bị khắc (union của 2 hệ gốc).
- **Miễn nhiễm** với các hệ từng khắc 2 hệ gốc.
- Mở khoá 1–2 kỹ năng Hybrid độc quyền từ bảng `gene_hybrid_skill`.

---

## 6. Các file .cs liên quan

### 6.1 Client — Unity

| File | Vai trò |
|---|---|
| `Client/Assets/Scripts/Inventory/UI/GeneUpgradePanel.cs` | Panel chính nâng gene chính; gọi `GameplayCommandService.UpgradeGeneServerRpc` |
| `Client/Assets/Scripts/Inventory/UI/SecondaryGeneUpgradePanel.cs` | Panel nâng gene phụ; gọi REST trực tiếp `/api/gene/secondary/upgrade` |
| `Client/Assets/Scripts/Inventory/UI/SecondaryGeneSelectPanel.cs` | Giao diện chọn hệ phụ lần đầu |
| `Client/Assets/Scripts/Inventory/Data/GeneDtos.cs` | DTO serialize/deserialize: `GeneConfigDto`, `GeneUpgradeRequest`, `GeneUpgradeResponse`, `GeneStatBonus`, `GeneSkillUnlock` |
| `Client/Assets/Scripts/UI/SelectGene/SelectGeneController.cs` | Controller màn hình chọn hệ gene khi tạo nhân vật |
| `Client/Assets/Scripts/UI/SelectGene/GeneSlotUI.cs` | Slot hiển thị 1 hệ gene trong màn hình chọn |
| `Client/Assets/Scripts/UI/Character/GeneUpgradePanelToggleButton.cs` | Button mở/đóng `GeneUpgradePanel` từ menu nhân vật |
| `Client/Assets/Scripts/UI/Character/SelectSecondaryGeneButton.cs` | Button chọn hệ phụ từ menu nhân vật |
| `Client/Assets/Scripts/Debug/GeneItemDebugAdder.cs` | Tool debug: thêm item gene vào túi đồ trong Editor |
| `Client/Assets/Scripts/Inventory/Network/NetworkInventory.cs` | Chứa `SyncBuffBonusesServerRpc` — đồng bộ GeneExpBuff bonus lên server |
| `Client/Assets/Scripts/Inventory/Network/InventoryNetworkBridge.cs` | Cầu nối gọi `SyncBuffBonusesServerRpc` khi buff thay đổi |

### 6.2 Server — ASP.NET Core

| File | Vai trò |
|---|---|
| `GameServerApi/Controllers/GeneController.cs` | Controller chứa toàn bộ logic gene: config, upgrade, secondary, hybrid fusion |
| `GameServerApi/Models/Entities/GeneUpgradeConfig.cs` | Entity bảng `gene_upgrade_config` |
| `GameServerApi/Models/Entities/GeneTierStatConfig.cs` | Entity bảng `gene_tier_stat_config` |
| `GameServerApi/Models/Entities/GeneMultiConfig.cs` | Entity bảng `gene_multi_config` (gene phụ) |
| `GameServerApi/Models/Entities/GeneHybridConfig.cs` | Entity bảng `gene_hybrid_config` + `NormalizeKey()` helper |
| `GameServerApi/Models/Entities/GeneHybridSkill.cs` | Entity bảng `gene_hybrid_skill` |
| `GameServerApi/Services/GameConfigCache.cs` | Cache in-memory: `GetGeneUpgrade`, `GetGeneMulti`, `GetGeneTierStat`, `GetHybrid`, `GetHybridSkills` |
| `GameServerApi/Data/GameDbContext.cs` | DbSet cho 5 bảng gene, cấu hình EF Fluent API |

### 6.3 Networking / Session

| File | Vai trò |
|---|---|
| `Scripts/Network/Server/GameplayCommandService.cs` | NetworkBehaviour chứa `GetGeneConfigServerRpc`, `UpgradeGeneServerRpc`, event `OnGeneConfigReceived`, `OnGeneUpgraded` |
| `Scripts/Network/Server/ZonePlayerSessionManager.cs` | Struct session giữ `gene_tier`, `gene_exp` trong bộ nhớ zone server |
| `GameServerApi/Controllers/PlayerController.cs` | Logic cộng gene_exp sau combat, áp dụng GeneExpBuff, tràn exp sang gene phụ |

---

## 7. Đặc điểm nổi bật trong bối cảnh Multiplayer

a) **Không có cheat**: mọi kết quả nâng cấp đều do server tính toán (Random seed phía server), client chỉ gửi request và nhận kết quả — không thể giả mạo tỉ lệ thành công.

b) **Config động từ DB**: toàn bộ chi phí, tỉ lệ, chỉ số bonus đều đọc từ cơ sở dữ liệu, không hardcode. Quản trị viên có thể điều chỉnh balance mà không cần build lại game.

c) **Buff GeneExp trong party**: khi chơi nhóm, nếu một thành viên mang buff `GeneExpBuff` (từ item hoặc skill), lượng gene_exp nhận được sau mỗi trận đấu được nhân hệ số tương ứng — tăng động lực chơi hợp tác.

d) **Tự động tràn exp**: người chơi không cần quản lý thủ công việc phân bổ gene_exp — server tự động chuyển sang gene phụ khi gene chính đã max, trải nghiệm liền mạch.

e) **Hybrid là mục tiêu cuối game**: yêu cầu max cả hai gene (10 lần nâng cấp + 1 fusion) tạo ra nội dung end-game dài hạn, phù hợp môi trường online nhiều người chơi.

---

## 8. Sơ đồ trạng thái tiến hóa

```
[Tạo nhân vật] → Chọn hệ chính (Fire/Water/Earth/Metal/Wood)
      │
      ▼
 Gene Chính Tier 1 ──nâng cấp──► Tier 2 ──► Tier 3 ──► Tier 4 ──► Tier 5 (MAX)
                                                                         │
                                                              Mở khoá chọn Hệ Phụ
                                                                         │
                                                                         ▼
                                               Gene Phụ Tier 1 ──► ... ──► Tier 5 (MAX)
                                                                                   │
                                                                      Đủ điều kiện Fusion
                                                                                   │
                                                                                   ▼
                                                                         Hybrid Gene
                                                                    (Danh hiệu + Kỹ năng đặc biệt)
```
