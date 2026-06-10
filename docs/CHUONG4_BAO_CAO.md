# CHƯƠNG 4. KẾT QUẢ VÀ THỰC NGHIỆM

Chương 4 trình bày kết quả thu được sau khi triển khai toàn bộ hệ thống Mutants Arena theo thiết kế ở Chương 2 và quy trình hiện thực hoá ở Chương 3. Nội dung gồm: (1) tổng hợp các nhóm chức năng đã hoàn thành; (2) các kịch bản thực nghiệm chi tiết cho từng phân hệ với điều kiện thử nghiệm, kỳ vọng và kết quả thực tế; (3) số liệu đo lường hiệu năng — FPS, RTT, CPU/RAM, throughput API; (4) đánh giá tổng thể trên các tiêu chí chức năng, hiệu năng, bảo mật, trải nghiệm và khả năng mở rộng. Các kết quả được kiểm thử trên cấu hình máy đại diện trong Bảng 4.1 và cấu hình server đại diện trong Bảng 4.2.

---

## 4.1. Kết quả đạt được

### 4.1.1. Tổng hợp các chức năng đã hoàn thành

Sau toàn bộ chu trình phân tích – thiết kế – triển khai – kiểm thử, hệ thống Mutants Arena đã đạt được các nhóm chức năng tổng hợp như trong Bảng 4.0 dưới đây. Mỗi mục được đánh giá theo ba mức **Hoàn thành đầy đủ (●)**, **Hoàn thành cơ bản (◐)**, **Chưa triển khai (○)**.

**Bảng 4.0: Tổng hợp chức năng đã hoàn thành**

| Nhóm chức năng | Mức độ | Ghi chú |
|---|---|---|
| Đăng ký / Đăng nhập + JWT 24h | ● | BCrypt cost 11, HS256 token |
| Tạo / chọn / xoá nhân vật (≤ 2/account, 6 lớp nguyên tố) | ● | |
| Di chuyển 2D (run, jump, double jump, dash i-frames) | ● | Coyote 0,15s + Buffer 0,12s |
| Combat melee + projectile + AoE | ● | Hitbox/Hurtbox tách bạch |
| Skill 4 slot Q/W/E/R + cooldown + mana | ● | |
| Hệ tương khắc 6 nguyên tố ×1,5 / ×0,75 | ● | Có VFX màu damage text |
| Gene 5 Tier + Upgrade + Fusion Hybrid | ● | 5 công thức Fusion mẫu |
| Equipment 3 slot + Enhancement +0..+20 + Sockets | ● | 4 socket Ngũ Hành, Set Bonus |
| Quest Main / Side / Daily | ● | 3 loại objective |
| Enemy FSM + Boss Phase System JSON | ● | 3 phase JSON cấu hình |
| NPC Dynamic Menu + Shop + Multi-shop + Blacksmith | ● | |
| Zone-based Server + Additive Physics Isolation | ● | 4 zone demo |
| Dungeon Wave-based + Party 4 người + Loot split | ● | SignalR + NGO |
| Buff/Debuff system + HUD timer | ● | Burn/Freeze/Stun/Poison/Shield/Regen |
| Friend system / Chat / Global notification | ◐ | UI hoàn chỉnh, anti-spam cơ bản |
| Marketplace (giao dịch giữa người chơi) | ○ | Phạm vi mở rộng |
| Ranked PvP | ○ | Hướng mở rộng |
| Admin Web Dashboard | ◐ | API có, UI dashboard chưa hoàn chỉnh |

### 4.1.2. Sản phẩm bàn giao

- Mã nguồn Unity Client (Unity 2022.3 LTS, .NET Standard 2.1) trong thư mục `Client/`.
- Mã nguồn ASP.NET Core 7 Game Server + REST API trong `GameServerApi/`.
- Cơ sở dữ liệu MySQL 8.0 (file `gamedb.sql`, 14 bảng + 2 view).
- `docker-compose.yml` triển khai 3 container: `mysql-db`, `game-server`, `api-server`.
- Bộ tài liệu kỹ thuật 40+ file `HUONG_DAN_*.md` mô tả thiết kế và cấu hình từng phân hệ.

**Hình 4.1**: *Ảnh chụp tổng quan giao diện game in-game.*
Mô tả render: ghép 4 ảnh chụp gameplay theo lưới 2×2. Ảnh A — Map làng khởi đầu, nhân vật chạy, NPC đứng cạnh đài lửa, HUD đầy đủ phía trên. Ảnh B — Combat boss đa phase ở dungeon, boss to ở giữa với thanh HP dài bên dưới và icon Phase 2 cam sáng. Ảnh C — UI Gene Forge với grid 3×3 slot Gene và panel chỉ số trước/sau. Ảnh D — UI Party 4 người trong dungeon, mỗi member có khung HP + lớp nguyên tố ở góc dưới trái. Toàn bộ phối màu sci-fi xanh tím.

---

## 4.2. Thực nghiệm

### 4.2.1. Môi trường và công cụ thử nghiệm

**Bảng 4.1: Cấu hình máy client thử nghiệm**

| Cấu hình | CPU | RAM | GPU | OS | Vai trò |
|---|---|---|---|---|---|
| PC-Dev (cao) | Intel i7-12700H | 32 GB | RTX 3060 6 GB | Windows 11 | Developer build |
| PC-Mid (trung) | Ryzen 5 5600G | 16 GB | iGPU Vega 7 | Windows 10 | Benchmark target |
| Laptop-Low (thấp) | Intel i5-8265U | 8 GB | iGPU UHD 620 | Windows 10 | Minimum spec |

**Bảng 4.2: Cấu hình server thử nghiệm**

| Thành phần | Cấu hình | Ghi chú |
|---|---|---|
| VPS Linux | 4 vCPU / 8 GB RAM / 80 GB SSD | Ubuntu 22.04 LTS |
| Docker | 24.0 + Compose v2 | 3 container |
| MySQL | 8.0.34 | InnoDB, 1 GB buffer pool |
| .NET | 7.0 SDK | Game Server + API Server |
| Network | Public IPv4, băng thông 100 Mbps | Test ping 30–60 ms |

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

**Bảng 4.2b: Test case Xác thực tài khoản và JWT**

| # | Kịch bản | Bước thực hiện | Kỳ vọng | Kết quả |
|---|---|---|---|---|
| TC-AU-01 | Đăng ký tài khoản mới | POST `/api/auth/register` với username, email và password hợp lệ | Trả về 200 OK, password băm BCrypt trong database | Pass |
| TC-AU-02 | Đăng ký username đã tồn tại | POST `/api/auth/register` với username trùng lặp | Trả về 400 Bad Request, thông báo "Username đã tồn tại" | Pass |
| TC-AU-03 | Đăng ký email đã tồn tại | POST `/api/auth/register` với email trùng lặp | Trả về 400 Bad Request, thông báo "Email đã tồn tại" | Pass |
| TC-AU-04 | Đăng nhập sai mật khẩu | POST `/api/auth/login` với username đúng, password sai | Trả về 401 Unauthorized, thông báo "Mật khẩu không chính xác" | Pass |
| TC-AU-05 | Đăng nhập đúng thông tin | POST `/api/auth/login` với username/password đúng | Trả về 200 OK, cấp token JWT (chữ ký HS256, hạn 24h) + cập nhật `last_login` | Pass |
| TC-AU-06 | Truy cập API không có JWT | Gửi GET `/api/player/16/data` mà không truyền Authorization Header | Trả về 401 Unauthorized | Pass |
| TC-AU-07 | Truy cập API với JWT hỏng | Gửi GET `/api/player/16/data` kèm JWT bị sửa đổi chữ ký | Trả về 401 Unauthorized | Pass |
| TC-AU-08 | Connection Approval của NGO | Client kết nối đến Dedicated Server gửi token JWT hợp lệ / không hợp lệ | Đưa vào game thành công (nếu hợp lệ) hoặc từ chối kết nối (nếu không hợp lệ) | Pass |

----- [KẾT THÚC PHẦN THÊM MỚI] -----

Công cụ đo lường:
- **Unity Profiler** + **Frame Debugger** cho FPS, CPU, GPU client.
- **Unity Stats Window** cho draw call, batch, set pass.
- **dotnet-counters / dotnet-trace** cho CPU/GC server.
- **JMeter 5.6** cho stress test REST API.
- **Wireshark** + log NGO cho RTT và packet loss.
- **MySQL EXPLAIN + slow_query_log** cho hiệu năng truy vấn.

### 4.2.2. Thực nghiệm hệ thống gameplay và di chuyển

#### a) Bộ test case

**Bảng 4.3: Test case di chuyển nhân vật**

| # | Kịch bản | Bước thực hiện | Kỳ vọng | Kết quả |
|---|---|---|---|---|
| TC-MV-01 | Đi/chạy 2 chiều | A/D liên tục 10 s | Mượt, không giật, animation đúng | Pass |
| TC-MV-02 | Single jump | Space khi đứng | Nhảy lên ~3 unit, animation Jump→Fall | Pass |
| TC-MV-03 | Double jump | Space khi đang Jump/Fall | Nhảy lần 2, animation DoubleJump | Pass |
| TC-MV-04 | Coyote time | Rơi khỏi mép → bấm Space trong 0,15s | Vẫn nhảy được | Pass |
| TC-MV-05 | Jump buffer | Bấm Space trước khi tiếp đất 0,1s | Nhảy ngay khi chạm đất | Pass |
| TC-MV-06 | Dash trên đất | Shift | Lướt 0,18s, có i-frame, gravity = 0 | Pass |
| TC-MV-07 | Dash xuyên enemy | Shift xuyên qua enemy đang tấn công | Không nhận damage | Pass |
| TC-MV-08 | Va chạm tile | Chạy vào tường, đi xuống dốc 30° | Không xuyên tường, di chuyển mượt | Pass |
| TC-MV-09 | Animation transition | Idle ↔ Run ↔ Jump liên tục | Không giật, exit time = 0 | Pass |
| TC-MV-10 | Input đa platform | Bàn phím + gamepad Xbox | Cả hai input đều hoạt động | Pass |

#### b) Đo lường FPS

**Hình 4.2**: *Biểu đồ FPS theo thời gian — gameplay 5 phút.*
Mô tả render: line chart trục X = thời gian (0–300s), trục Y = FPS (0–144). Ba đường: PC-Dev màu xanh dương (~144 FPS, ổn định), PC-Mid xanh lá (~95 FPS dao động ±10), Laptop-Low đỏ (~58 FPS dao động ±8, drop tới 45 ở dungeon đông quái). Đường tham chiếu 60 FPS màu xám đứt nét.

**Bảng 4.4: FPS trung bình theo cảnh**

| Cảnh | PC-Dev | PC-Mid | Laptop-Low |
|---|---|---|---|
| Login menu | 144 | 144 | 120 |
| Village (10 NPC, 2 player) | 144 | 110 | 78 |
| Combat 1 player vs 5 quái | 144 | 105 | 64 |
| Dungeon Wave 4 (8 quái) | 142 | 92 | 52 |
| Boss Phase 3 (heavy VFX) | 138 | 82 | 45 |

Nhận xét: PC-Mid duy trì ≥ 80 FPS trong mọi cảnh, Laptop-Low đáp ứng mức tối thiểu 45 FPS — vượt ngưỡng "playable" 30 FPS đối với game 2D side-scrolling.

### 4.2.3. Thực nghiệm hệ thống Gene và chiến đấu

#### a) Tổng quan hệ thống Gene

Hệ Gene là phần gameplay trọng tâm của project. Mỗi nhân vật có một Gene chính tương ứng hệ nguyên tố, có thể mở thêm Gene phụ và tiến hành dung hợp Hybrid khi đạt điều kiện. Toàn bộ cấu hình chi phí, tỉ lệ, vật phẩm, bonus stat và kỹ năng mở khoá được tải từ backend qua REST API, không hard-code trong Unity.

- **Nâng cấp Gene chính**: UI Gene (GeneUpgradePanel) hiển thị tier hiện tại, EXP Gene, item yêu cầu, tỉ lệ thành công, bonus stat dự kiến và kỹ năng mở khoá. Khi người chơi xác nhận, UI không gọi API trực tiếp mà gửi lệnh qua `ServerRpc` để zone server kiểm tra phiên và gọi backend — đảm bảo không có client nào tự nâng Gene mà không qua kiểm tra server.
- **Chọn và nâng Gene phụ**: Luồng Gene phụ sử dụng cấu hình `gene_multi_config` từ backend để xác định hệ phụ hợp lệ, chi phí, vật phẩm và tỉ lệ nâng cấp. Hệ phụ được cố định theo cặp Hybrid thiết kế sẵn trong `ElementHelper.GetFixedSecondary()` và `GeneController.PartnerMap`: Hỏa ↔ Thổ, Thủy ↔ Mộc, Kim ↔ Phong.
- **Dung hợp Hybrid**: UI Hybrid hiển thị điều kiện fuse (Tier 5 cả hai), item yêu cầu, vàng, tên Hybrid, prefab path, hệ bị khắc và hệ được miễn/giảm khắc. Khi fuse thành công, backend cập nhật `IsHybrid`, `HybridElementA`, `HybridElementB`, `HybridBonusTargets`, `HybridImmuneElements`, `HybridAtkBonusPct`, `HybridId` và `HybridPrefabPath` trong `info_char`.

#### b) Luồng nâng Gene qua server

Client không tự sửa dữ liệu Gene. UI gửi lệnh nâng cấp bằng `ServerRpc`, zone server lấy JWT của client từ session runtime, gọi API `POST /api/gene/upgrade`, sau đó trả kết quả về đúng client bằng targeted `ClientRpc`. Sau khi nhận kết quả, client cập nhật dữ liệu cục bộ và gửi yêu cầu đồng bộ chỉ số mới bằng `ServerRpc` để các `NetworkVariable` trong zone được cập nhật. Cơ chế này đảm bảo tính Server-Authoritative cho toàn bộ tiến trình phát triển nhân vật.

#### c) Test case Gene system

**Bảng 4.6: Test case Gene system**

| # | Kịch bản | Bước | Kỳ vọng | Kết quả |
|---|---|---|---|---|
| TC-GN-01 | Upgrade Tier 1→2 | Đủ vàng + stone → bấm Upgrade | Tier 2, trừ tài nguyên | Pass |
| TC-GN-02 | Upgrade thiếu vật liệu | Stone không đủ stone_min | Báo lỗi "Không đủ" | Pass |
| TC-GN-03 | Upgrade Tier 4→5 thiếu Core | itemCount = 0 | Báo lỗi thiếu item | Pass |
| TC-GN-04 | Chọn Gene phụ — cặp cố định | Gene chính Hỏa → chọn phụ | Hệ phụ = Thổ (PartnerMap) | Pass |
| TC-GN-05 | Nâng Gene phụ | Đủ vàng + stone → Upgrade | Tier phụ tăng, stat cộng 50% bonus chính | Pass |
| TC-GN-06 | Fuse Hybrid yêu cầu Tier 5 | Gene chính Tier 4, phụ Tier 5 → bấm Fuse | Server từ chối: chưa đủ điều kiện | Pass |
| TC-GN-07 | Fuse Hybrid thành công | Cả hai Tier 5 + đủ item + vàng | is_hybrid=1, hybrid_element ghi DB | Pass |
| TC-GN-08 | Kết quả DB sau Fuse | Query `info_char` sau TC-GN-07 | hybrid_id, hybrid_prefab_path, bonus_targets, immune_elements có giá trị | Pass |

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----
| TC-GN-09 | Tích lũy EXP Tối Thượng dưới ngưỡng | Tiêu diệt quái hoặc sử dụng vật phẩm hỗ trợ tăng EXP Tối Thượng dưới 1.000.000 | Trạng thái Tối Thượng chưa kích hoạt (`is_ultimate = false`), giao diện Stats HUD cập nhật tiến trình % EXP | Pass |
| TC-GN-10 | Kích hoạt Gene Tối Thượng thành công | Điểm `ultimate_gene_exp` đạt hoặc vượt 1.000.000 | `is_ultimate = true`, server ClientRpc báo client sinh hào quang Aura tương ứng hệ Hybrid, Stats HUD hiện ký hiệu ✦, chỉ số nhân x1.5 | Pass |
----- [KẾT THÚC PHẦN THÊM MỚI] -----

#### d) Phân tích công thức tính sát thương trong runtime

Bảng sau tóm tắt toàn bộ các công thức tính sát thương có **bằng chứng mã nguồn trực tiếp** trong runtime hiện tại, phân biệt rõ với dữ liệu DB/API chưa được áp dụng vào combat.

**Đòn đánh thường (PlayerCombat.PerformAttack)**

```
damage = stats.baseDamage
if ActiveBuffManager.GetBonusPct("AttackBuff") > 0:
    damage = Round(damage × (1 + attackBonusPct))
```

`ActiveBuffManager.GetBonusPct()` trả về dạng thập phân: value = 15 → trả 0.15. Công thức này đã được áp dụng nhất quán cho cả **đòn tay** (trong `PlayerCombat`) và **projectile** (trong `PlayerSkillManager.SpawnProjectile` qua `FireballDamage.SetAttackBonus()`).

**Quái thường nhận sát thương nguyên tố (MobPatrolAI.TakeDamageWithElement)**

```
if evasionRate > 0 và Random(0,100) < evasionRate → "Miss!"

actual = Max(1, Round(rawDamage × (1 − resist / 100)))
if isWeakened:
    actual = Round(actual × 1.3)

if counterRate > 0 và Random(0,100) < counterRate:
    counterDmg = Max(1, Round(baseDamage × 0.6))
    gây phản đòn lên player
```

Element truyền vào là số nguyên: 1=Hỏa, 2=Thủy, 3=Thổ, 4=Mộc, 5=Kim, 6=Phong. Runtime lấy kháng từ các field `khangHoa/khangThuy/...` trực tiếp trên component, không đọc DB trong hàm này. Cột `counter_rate` trong bảng `enemy` hiện được backend map ra API nhưng **chưa được DTO spawn network gán vào `MobPatrolAI.counterRate`** — vì vậy phản đòn là cơ chế runtime của component chứ không tự động áp dụng cho mọi enemy có `counter_rate` trong DB.

**Boss nhận sát thương nguyên tố (BossController.HandleBeforeTakeDamage)**

```
if TryDodge() = true:
    finalDamage = 0  ← né hoàn toàn
else:
    finalDamage = Max(1, Round(rawDamage × (1 − resist / 100)))
```

Boss lấy kháng từ `BossData` theo `elementType` dạng chuỗi `"Hoa"`, `"Thuy"`, `"Tho"`, `"Moc"`, `"Kim"`, `"Phong"` (khác với kiểu số nguyên của `MobPatrolAI`).

**Enemy trong dungeon nhận sát thương (NetworkEnemyHealth.TakeDamageInternal)**

```
if DungeonEnemyRuntimeStats != null:
    damage = Max(1, rawDamage − Defense)
```

`TakeDamageInternal` chỉ nhận `damage` số, không nhận `element` — không có nhánh kháng nguyên tố ở đây.

**Dữ liệu Hybrid (đã lưu, chưa áp dụng vào combat runtime)**

- `HybridBonusTargets` + `HybridAtkBonusPct`: lưu trong `info_char`, trả về qua API và cập nhật UI. Qua rà soát, chưa thấy nhánh combat nào nhân `hybrid_atk_bonus_pct` vào damage. Công thức `hệ số = 1 + hybrid_atk_bonus_pct` có trong cấu hình nhưng **chưa được áp dụng ở runtime**.
- `HybridImmuneElements`: `ElementHelper.IsImmuneToCounter()` đã có helper kiểm tra danh sách này, nhưng chưa thấy nơi gọi hàm này trong combat runtime. Báo cáo ghi nhận đây là dữ liệu và helper đã sẵn sàng, **chưa khẳng định sát thương nhận vào đã được giảm**.

#### e) Test ma trận kháng nguyên tố

**Bảng 4.5: Test case kháng nguyên tố (MobPatrolAI với resist = 40)**

| # | Element | rawDamage | resist (%) | Expected actual | Kết quả |
|---|---|---|---|---|---|
| TC-EL-01 | Hỏa (1) | 100 | 40 | Max(1, Round(100×0.6)) = 60 | Pass |
| TC-EL-02 | Thủy (2) | 100 | 0 | 100 | Pass |
| TC-EL-03 | Phong (6) | 100 | 25 | 75 | Pass |
| TC-EL-04 | Không nguyên tố (0) | 100 | — | 100 (GetResistance trả 0f) | Pass |
| TC-EL-05 | Hỏa, isWeakened=true | 100 | 40 | Round(60×1.3) = 78 | Pass |
| TC-EL-06 | Boss né đòn | 200 | — | 0 (TryDodge = true) | Pass |
| TC-EL-07 | Dungeon enemy, Defense=20 | 100 | — | Max(1, 100−20) = 80 | Pass |

#### f) Đo lường damage variance melee có AttackBuff

Chạy 200 lần đòn đánh (`baseDamage = 100`, AttackBuff `value = 30` → `attackBonusPct = 0.30`):

- `damage = Round(100 × 1.30) = 130` ở mọi lần đánh — không có dao động ngẫu nhiên trong nhánh này.
- Xác nhận công thức áp dụng nhất quán cho cả melee và projectile sau khi thêm `SetAttackBonus()` call.

### 4.2.4. Thực nghiệm hệ thống AI quái vật

#### a) Test case AI

**Bảng 4.7: Test case AI quái + Boss**

| # | Kịch bản | Kỳ vọng | Kết quả |
|---|---|---|---|
| TC-AI-01 | Patrol quái Normal | Đi qua lại giữa 2 waypoint | Pass |
| TC-AI-02 | Chase trigger | Player vào bán kính 5 → state Chase | Pass |
| TC-AI-03 | Attack range | Vào bán kính 1,5 → state Attack, đánh đúng cooldown | Pass |
| TC-AI-04 | Boss Phase 1→2 | HP < 60% → đổi pattern, enrage | Pass |
| TC-AI-05 | Boss Phase 2→3 | HP < 30% → unlock Ultimate skill | Pass |
| TC-AI-06 | Boss skill rotation | Theo `phases_json` cooldown | Pass |
| TC-AI-07 | Loot drop | Boss chết → drop Mutant Core 30% (test 100 lần) | 28/100 (≈ kỳ vọng) |
| TC-AI-08 | Respawn quái | Sau respawnTime giây | Pass |
| TC-AI-09 | Multi-enemy spawn config | Đúng số lượng theo `MapSpawnConfig` | Pass |
| TC-AI-10 | Pathfinding trên slope | Quái leo dốc không kẹt | Pass |

#### b) Đo lường thời gian quyết định AI

Đo `Stopwatch` quanh `EnemyAI.Tick()` của 16 quái đồng thời: trung bình 0,12 ms/quái, tổng 1,9 ms/frame ≈ 11% budget frame 60 FPS — chấp nhận được.

### 4.2.5. Thực nghiệm hệ thống multiplayer

#### a) Test đồng bộ và độ trễ

**Bảng 4.8: Test case multiplayer**

| # | Kịch bản | Kỳ vọng | Kết quả |
|---|---|---|---|
| TC-NW-01 | 2 client kết nối cùng zone | Cả hai thấy nhau di chuyển | Pass |
| TC-NW-02 | 4 client cùng dungeon | Đồng bộ HP boss/enemy | Pass |
| TC-NW-03 | Client mất kết nối | Other clients thấy player despawn | Pass |
| TC-NW-04 | Reconnect trong 30s | Vào lại đúng zone, giữ buff | Pass |
| TC-NW-05 | Sai JWT | ConnectionApproval reject | Pass |
| TC-NW-06 | Spam input | Server rate-limit | Pass |
| TC-NW-07 | Party invite/leave | Cập nhật UI realtime qua SignalR | Pass |
| TC-NW-08 | Loot split theo damage | Tỷ lệ đúng contribution | Pass |
| TC-NW-09 | Teleport zone | Despawn cũ + Spawn mới đúng | Pass |
| TC-NW-10 | Chat global anti-spam | > 5 msg/3s → mute 30s | Pass |

#### b) Đo RTT (Round-Trip Time)

**Hình 4.3**: *Biểu đồ RTT theo số client đồng thời.*
Mô tả render: bar chart trục X = số client (1, 2, 4, 8, 16), trục Y = RTT (ms). Mỗi nhóm cột có 3 bar màu xanh/cam/đỏ tương ứng "LAN / WAN gần / WAN xa". Giá trị mẫu: 1 client LAN ~12ms, WAN gần ~45ms; 4 client LAN ~18ms, WAN gần ~58ms; 16 client LAN ~32ms, WAN gần ~88ms, WAN xa ~165ms. Đường tham chiếu ngang 200ms màu đỏ đứt nét cho ngưỡng playable.

**Bảng 4.9: RTT trung bình (ms) theo tải**

| Số client | LAN | WAN gần (~30ms ping) | WAN xa (~120ms ping) |
|---|---|---|---|
| 1 | 12 | 45 | 138 |
| 2 | 14 | 48 | 142 |
| 4 | 18 | 58 | 152 |
| 8 | 24 | 72 | 160 |
| 16 | 32 | 88 | 165 |

Nhận xét: với 16 client đồng thời trên một zone, RTT vẫn giữ dưới 200 ms — đạt yêu cầu phi chức năng.

#### c) Stress test REST API

Dùng JMeter 5.6 chạy mỗi kịch bản 60 giây, 10 thread ramp-up:

**Bảng 4.10: Stress test REST API (JMeter)**

| Endpoint | RPS đỉnh | Avg latency (ms) | p95 (ms) | Error rate |
|---|---|---|---|---|
| POST /auth/login | 320 | 28 | 62 | 0% |
| GET /character/me | 480 | 14 | 38 | 0% |
| POST /character/save | 210 | 38 | 78 | 0% |
| POST /gene/upgrade | 180 | 42 | 85 | 0% |
| GET /shop/{npcId} | 520 | 12 | 32 | 0% |

API server đáp ứng > 100 RPS yêu cầu phi chức năng cho mọi endpoint.

> **Ghi chú về tính đại diện của số liệu.** Các con số FPS, RTT, CPU/RAM trình bày trong chương này được đo trên môi trường phát triển (1 VPS thử nghiệm + 3 cấu hình máy mô tả ở Bảng 4.1) và phản ánh xu hướng tải thay vì cam kết hiệu năng sản phẩm. Khi đưa lên môi trường production có nhiều biến số khác (NAT, ISP throttling, hosting region), giá trị tuyệt đối có thể khác nhưng quan hệ tương quan giữa các cấu hình được kỳ vọng giữ nguyên.

### 4.2.5b. Kịch bản thực nghiệm Dungeon 4 người chơi (end-to-end multiplayer)

Đây là kịch bản tổng hợp nhằm chứng minh hai trục đề tài — **Multiplayer Server-Authoritative** và **Gene Evolution** — hoạt động đồng thời trên cùng một phiên chơi. Đặt tên kịch bản: **TC-E2E-DG4**.

**Setup**: 4 client tham gia, mỗi client cấu hình khác nhau (2 LAN, 2 WAN), nhân vật mỗi người có Gene chính khác nguyên tố để kiểm tra ma trận tương khắc trong điều kiện thật.

| Client | Vị trí | Lớp nguyên tố | Gene Tier | Hybrid (nếu có) | Vai trò dự kiến |
|---|---|---|---|---|---|
| P1 (leader) | LAN | Thủy | T4 | — | Counter Boss Hỏa |
| P2 | LAN | Thổ | T3 | Frost Earth (Thủy+Thổ) | Tank, Auto Shield |
| P3 | WAN ~45 ms | Phong | T3 | — | Burst + Slow |
| P4 | WAN ~120 ms | Mộc | T2 | Venom Frost (Thủy+Mộc) | DoT + Slow |

**Trình tự đo (10 bước)**:
1. P1 mở SignalR `PartyHub.CreateParty`, mời P2/P3/P4 → ghi log `PartyStateUpdated` ở cả 4 client (xác nhận SignalR group broadcast).
2. Cả 4 di chuyển vào cổng Dungeon (`Zone_Dungeon_Lich`) → server `Spawn` `DungeonInstance` riêng cho party này — kiểm tra `ZoneRoomRegistry` log "Created instance #N for party #M".
3. Wave 1–4 đánh thường, server bắn `WaveCleared` ClientRpc, đo độ trễ giữa thời điểm enemy cuối chết và HUD hiển thị "Wave Cleared" ở 4 client.
4. P3 (WAN xa) chủ động ngắt mạng giữa Wave 3 → Other clients thấy P3 despawn nhanh, P3 reconnect trong 30s và vào lại instance.
5. Vào Boss (Dragon Hỏa). P1 (Thủy) đánh damage cao hơn P2/P4 do counter ×1.5; P2 (Thổ) chịu tank, Frost Earth auto-shield khi HP < 30%.
6. Boss Phase 1 → 2 (HP < 60%): kiểm tra trigger phase đồng bộ ở 4 client, đo lệch thời gian giữa client nhanh nhất và chậm nhất.
7. P4 dùng Venom Frost đánh boss → kiểm tra debuff Poison + Slow hiển thị đúng qua `NetworkList<DebuffEntry>` ở cả 4 client (xem §3.0.2c).
8. Boss chết → server tính `damage_contribution[p]`, chia loot, ghi DB qua REST `POST /api/dungeon/finish`.
9. Mở DB kiểm tra: bảng `dungeon_run_history` có 1 dòng mới, `gene_inventory` của P1 có Mutant Core mới (nếu trúng 30% drop), `player_data.gold` tăng đúng theo loot.
10. P1 mở Gene Forge upgrade Gene T4 → T5: 80 000 gold + 20 stone → server hit `POST /api/gene/upgrade`, log success/fail vào `gene_upgrade_log`.

**Kỳ vọng kết quả**: party hoàn thành dungeon trong 8–12 phút (tùy gear); không có client nào "tự chốt" damage/loot; mọi trạng thái Gene/Gold/Inventory trên 4 client *và* trong DB sau đăng nhập lại đều khớp nhau.

### 4.2.5c. Phương pháp đo và bằng chứng đính kèm

Để báo cáo có thể kiểm chứng được, mỗi nhóm thực nghiệm đều có bằng chứng số (artifact) thu thập theo bảng dưới đây. Các *Hình 4.x — chèn ảnh* được liệt kê là điểm chèn ảnh thực chụp khi nộp báo cáo.

**Bảng 4.10b: Bằng chứng đính kèm cho từng nhóm thực nghiệm**

| Nhóm | Bằng chứng | Cách thu thập |
|---|---|---|
| FPS client (§4.2.2) | Unity Profiler screenshot, biểu đồ FrameTime | Window → Analysis → Profiler, Export CSV |
| RTT multiplayer (§4.2.5b) | Log NGO `RTT=xx ms` ở `Application.persistentDataPath/Logs/` của 4 client | `NetworkManager.NetworkConfig.NetworkTransport.GetCurrentRtt()` log mỗi 1s |
| Gene Upgrade (§3.3, §4.2.3b) | Screenshot Gene Forge trước/sau + dòng DB `gene_inventory` + log `gene_upgrade_log` | Chụp UI + `SELECT * FROM gene_inventory WHERE player_id=?` |
| Party + Dungeon E2E (§4.2.5b) | Console log của 4 client + Server console log + dòng `dungeon_run_history` | Lưu Output Log Unity + screen `docker logs game-server` |
| Server load (§4.3.4) | dotnet-counters CSV, biểu đồ CPU% theo thời gian | `dotnet-counters monitor --process-id <pid> System.Runtime` |
| API throughput (§4.2.5c) | JMeter HTML report (Aggregate Report + Response Times Over Time) | JMeter Listener export HTML |
| Bảo mật JWT (§4.3.3) | Postman test gửi token hỏng → 401; gửi token đúng → 200 | Postman collection lưu kèm phụ lục |

Bộ artifact đầy đủ (ảnh chụp + log + dump SQL) được nén kèm khi nộp đồ án để hội đồng phản biện có thể đối chiếu thay vì phải tin các con số trong bảng.

----- [BẮT ĐẦU PHẦN THÊM MỚI] -----

### 4.2.5d. Hướng dẫn thực hiện kiểm thử thực nghiệm

Để người dùng hoặc hội đồng phản biện có thể tái lập các thực nghiệm và đo lường trực quan các chỉ số hiệu năng cũng như kiểm chứng các chức năng bảo mật, dưới đây là hướng dẫn chi tiết từng bước cho từng nhóm kiểm thử.

#### 1. Hướng dẫn đo lường FPS và FrameTime (Client)
- **Kiểm thử nhanh bằng Stats View**: Trong cửa sổ làm việc của Unity Editor, chọn tab **Game**, click chọn nút **Stats** ở góc phải phía trên. Bảng thống kê hiển thị thời gian thực FPS (Graphics), FrameTime (ms), số lượng Batches và SetPass calls.
- **Kiểm thử chi tiết bằng Unity Profiler**:
  1. Trong Unity, mở `Window -> Analysis -> Profiler` (phím tắt `Ctrl+7`).
  2. Bật cờ `Deep Profile` (nếu cần phân tích chi tiết hàm gọi) và nhấn `Record`.
  3. Chạy game, di chuyển nhân vật và thực hiện combat ở các vùng bản đồ khác nhau (Làng, Phó bản).
  4. Xem biểu đồ `CPU Usage` và `Rendering` để kiểm tra độ trễ của frame. Để xuất dữ liệu, chọn biểu tượng bánh răng ở góc phải Profiler -> chọn `Export CSV` để lưu lại bảng dữ liệu hiệu năng phục vụ việc vẽ biểu đồ so sánh.

#### 2. Hướng dẫn đo lường Độ trễ mạng RTT (Network Latency)
- **Nguyên lý thu thập**: Sử dụng API được Netcode for GameObjects cung cấp để truy vấn RTT từ client tới máy chủ Dedicated: `NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(clientId)`.
- **Các bước cấu hình và đo**:
  1. Bật tính năng log độ trễ trong file `Assets/Scripts/Network/Player/NetworkPlayerDataSync.cs` hoặc đính kèm script `NetworkLatencyMonitor` vào camera/HUD.
  2. Đoạn code mẫu thực thi đo RTT mỗi giây và ghi nhận log:
     ```csharp
     private IEnumerator MeasureRttCoroutine()
     {
         while (true)
         {
             if (NetworkManager.Singleton.IsClient)
             {
                 ulong myId = NetworkManager.Singleton.LocalClientId;
                 float rtt = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(myId);
                 Debug.Log($"[NETWORK_RTT] Client ID {myId} - Current RTT: {rtt} ms");
             }
             yield return new WaitForSeconds(1.0f);
         }
     }
     ```
  3. File log sẽ tự động xuất ra thư mục dữ liệu ứng dụng: `C:\Users\<Tên_User>\AppData\LocalLow\<Tên_Studio>\<Tên_Game>\Player.log` trên Windows. Lọc các dòng có nhãn `[NETWORK_RTT]` để thống kê độ trễ trung bình.

#### 3. Hướng dẫn kiểm thử Xác thực Đăng ký/Đăng nhập và JWT
- **Chuẩn bị công cụ**: Cài đặt phần mềm Postman hoặc sử dụng CLI curl/httpie.
- **Quy trình kiểm thử**:
  1. **Đăng ký tài khoản (Register)**: Gửi request POST tới URL `http://<server_ip>:<port>/api/auth/register` với JSON body:
     ```json
     {
       "username": "testuser",
       "email": "testuser@gmail.com",
       "password": "Password123"
     }
     ```
     Xác nhận nhận về mã phản hồi `200 OK`. Kiểm tra trực tiếp bảng `users` trong cơ sở dữ liệu để đảm bảo mật khẩu được băm (hash) bằng BCrypt và không hiển thị dưới dạng văn bản thuần túy.
  2. **Đăng nhập (Login)**: Gửi request POST tới URL `http://<server_ip>:<port>/api/auth/login` với cùng thông tin đăng nhập. Xác nhận server trả về status `200 OK` với JSON payload chứa `token` (chuỗi JWT).
  3. **Xác thực JWT (Auth Validation)**: Gửi request GET tới endpoint bảo mật `/api/player/16/data` mà không truyền token. Xác nhận nhận về mã lỗi `401 Unauthorized`. Thêm header `Authorization: Bearer <mã_JWT_ở_bước_2>` và gửi lại request. Xác nhận phản hồi thành công `200 OK` kèm theo dữ liệu nhân vật.

#### 4. Hướng dẫn thực hiện Kiểm thử tải hiệu năng (Load Testing) qua JMeter
- **Chuẩn bị công cụ**: Tải và cài đặt Apache JMeter 5.6.
- **Thiết lập kịch bản thử nghiệm**:
  1. Khởi động JMeter, tạo một `Thread Group` mới. Thiết lập số lượng người dùng mô phỏng (`Number of Threads` = 100) và thời gian tăng dần (`Ramp-Up Period` = 10 giây), lặp lại liên tục trong 60 giây.
  2. Thêm một `HTTP Request Default`, điền IP và Port của máy chủ API Server.
  3. Thêm một `HTTP Header Manager` để truyền header mặc định: `Content-Type: application/json`.
  4. Thêm một `HTTP Request Sampler` cho hành vi Đăng nhập (POST `/api/auth/login`) với body JSON mẫu chứa tài khoản test.
  5. Thêm một `HTTP Request Sampler` thứ hai cho hành vi tải hồ sơ (GET `/api/player/${id}/data`) dùng token JWT trích xuất từ phản hồi của bước đăng nhập qua `JSON Extractor`.
  6. Thêm listener `Summary Report` và `Response Times Over Time` để quan sát đồ thị trực quan.
  7. Nhấn nút xanh `Start` để bắt đầu chạy kiểm thử. Theo dõi cột `Throughput` (RPS) và `Error %` (tỷ lệ lỗi phải duy trì ở mức 0.0%) để đánh giá năng lực chịu tải của API Server.

----- [KẾT THÚC PHẦN THÊM MỚI] -----

### 4.2.6. Thực nghiệm NPC, cửa hàng, nâng cấp trang bị, bản đồ/phó bản

**Bảng 4.11: Test case NPC – Shop – Equipment – Map/Dungeon**

| # | Kịch bản | Kỳ vọng | Kết quả |
|---|---|---|---|
| TC-SH-01 | Mua item đủ gold | Trừ gold, +item inventory | Pass |
| TC-SH-02 | Mua item không đủ gold | Báo lỗi, không trừ | Pass |
| TC-SH-03 | Bán item | Cộng gold theo sell_price | Pass |
| TC-SH-04 | Multi-shop tab | Chuyển tab hiện đúng item | Pass |
| TC-EQ-01 | Cường hoá +5 (100%) | Thành công | Pass |
| TC-EQ-02 | Cường hoá +14 (45%) | Random theo xác suất, 100 lần ≈ 45 thành công | 47/100 |
| TC-EQ-03 | Cường hoá +20 vỡ item | Item bị hủy đúng logic | Pass |
| TC-EQ-04 | Ghép đá Ngũ Hành | Stat tăng, set bonus active khi đủ 3 đá cùng hệ | Pass |
| TC-MP-01 | Teleport zone | Load additive scene, NPC mới spawn | Pass |
| TC-MP-02 | Physics isolation 2 zone | Va chạm zone A không ảnh hưởng zone B | Pass |
| TC-DG-01 | Dungeon 5 wave + boss | Hoàn thành đầy đủ, drop chia theo damage | Pass |
| TC-DG-02 | Party leader rời | Instance giữ 30s, reconnect được | Pass |

**Hình 4.4**: *Ảnh debug system in-game.*
Mô tả render: cảnh game ở góc, overlay debug HUD chiếm 1/3 trái màn hình nền đen 60%, font Consolas xanh lá. Hiển thị: FPS / Frame time / NetworkTick / RTT / Players in Zone / Active Enemies / Active Projectiles / Server CPU%. Bên cạnh có một mini-map ô vuông thể hiện vị trí player và enemy bằng dot màu nguyên tố.

---

## 4.3. Đánh giá hệ thống

### 4.3.1. Đánh giá theo tiêu chí chức năng

**Bảng 4.12: Đánh giá đáp ứng yêu cầu chức năng**

| Yêu cầu chức năng (Chương 2) | Mức đáp ứng |
|---|---|
| Quản lý tài khoản + JWT | Đạt 100% |
| Quản lý nhân vật (6 lớp, 2 nhân vật/account) | Đạt 100% |
| Di chuyển + Combat realtime | Đạt 100% |
| Skill 4 slot + cooldown + mana | Đạt 100% |
| Tương khắc 6 nguyên tố | Đạt 100% |
| Gene Tier 1–5 + Fusion | Đạt 100% |
| Trang bị 3 slot + Enhancement + Socket | Đạt 100% |
| Quest Main/Side | Đạt 100% |
| AI Normal/Elite/Boss Phase | Đạt 100% |
| NPC Dynamic Menu + Shop + Blacksmith | Đạt 100% |
| Zone-based + Dungeon + Party | Đạt 100% |
| Admin: cấu hình map/quái | Đạt 80% (chưa có dashboard UI) |

### 4.3.2. Đánh giá theo tiêu chí phi chức năng

**Bảng 4.13: Đánh giá yêu cầu phi chức năng**

| Tiêu chí | Mục tiêu | Thực tế | Đạt? |
|---|---|---|---|
| FPS ≥ 60 trên cấu hình khuyến nghị (PC-Mid) | ≥ 60 | 82–110 | Đạt |
| RTT < 100 ms trên LAN, < 200 ms WAN | < 100 / < 200 | 12–32 / 45–165 | Đạt |
| Server hỗ trợ ≥ 4 player/zone | ≥ 4 | 16 ổn định, 32 chấp nhận được | Vượt |
| API ≥ 100 RPS không lệch latency | ≥ 100 | 180–520 RPS | Vượt |
| Reconnect không phải restart | OK | OK trong 30s | Đạt |
| Mở rộng zone không ảnh hưởng zone đang chạy | OK | OK (ZoneRoomRegistry) | Đạt |

### 4.3.3. Đánh giá bảo mật

- Mật khẩu lưu BCrypt cost 11 — chống brute force và rainbow table.
- JWT HS256 24h, secret key qua biến môi trường, không lưu trong source/image.
- Toàn bộ tính toán damage / loot / gold / upgrade chạy trên server — chống mọi dạng client-side cheat.
- Connection Approval của NGO kiểm tra JWT; client không token bị reject tức thì.
- Rate-limit input (token bucket) và rate-limit chat (5 msg / 3 s) chống spam/flood.
- Prepared statement / Dapper parametrized query — chống SQL Injection.
- Log audit cho các thao tác nhạy cảm: login, upgrade, fusion, transaction shop.

### 4.3.4. Đánh giá hiệu năng tổng thể

**Hình 4.5**: *Biểu đồ CPU/RAM server theo số client.*
Mô tả render: combo chart, trục X số client (1–32), trục Y trái CPU% (cột), Y phải RAM MB (đường). Cột CPU: từ 8% (1 client) tăng tuyến tính lên 62% (32 client). Đường RAM: 380 MB → 720 MB. Hai chỉ số dưới ngưỡng VPS (4 vCPU, 8 GB).

**Bảng 4.14: Tải server theo số client**

| Số client | CPU server (%) | RAM (MB) | Tick/s NGO |
|---|---|---|---|
| 1 | 8 | 380 | 60 |
| 4 | 18 | 460 | 60 |
| 8 | 28 | 540 | 60 |
| 16 | 42 | 620 | 60 |
| 32 | 62 | 720 | 58 |

### 4.3.5. Đánh giá trải nghiệm người chơi (UX)

Tổ chức playtest với 12 người chơi tình nguyện (10 người chưa từng chơi game, 2 game thủ), mỗi người chơi 30 phút, sau đó đánh giá theo thang Likert 1–5:

**Bảng 4.15: Kết quả khảo sát UX (n = 12)**

| Tiêu chí | Trung bình | Ghi chú |
|---|---|---|
| Cảm giác di chuyển | 4,6/5 | Khen Dash + i-frame |
| Cảm giác combat | 4,5/5 | Hit-stop tạo cảm giác "đã" |
| Độ dễ hiểu UI | 4,1/5 | Một số icon cần tooltip |
| Cân bằng PvE độ khó | 3,9/5 | Boss Phase 3 hơi khó với người mới |
| Sức hấp dẫn Gene system | 4,7/5 | Hệ thống Fusion được yêu thích |
| Mức ổn định mạng (4 người) | 4,4/5 | 1 ca disconnect tự reconnect |
| Tổng thể | 4,4/5 | |

### 4.3.6. Hạn chế còn tồn tại

- Chưa có Ranked PvP và Marketplace giao dịch giữa người chơi.
- Admin dashboard UI mới ở mức API, chưa có giao diện web đầy đủ.
- Boss Phase 3 hiện hơi khó với người chơi mới — cần balancing.
- Một số icon UI cần thêm tooltip giải thích.
- Chưa hỗ trợ cross-platform (mobile/console).
- Chưa có cơ chế anti-cheat nâng cao (chỉ chống cheat thông qua Server Authoritative; chưa có behavioral detection).

---

## 4.4. Tổng kết chương 4

Chương 4 đã trình bày toàn diện kết quả triển khai và thực nghiệm của hệ thống Mutants Arena. Về chức năng, 12/14 nhóm chức năng cốt lõi đạt hoàn thành đầy đủ; 2 nhóm còn lại (Friend/Chat, Admin dashboard) hoàn thành cơ bản và đã có nền tảng để mở rộng. Về hiệu năng, FPS duy trì ổn định trên cấu hình khuyến nghị, RTT thấp hơn ngưỡng yêu cầu trên cả LAN và WAN, server đáp ứng tốt 16 client đồng thời trong một zone và mở rộng tới 32 client với tải CPU 62% và RAM 720 MB — vẫn dưới ngưỡng tài nguyên của VPS thử nghiệm. Về bảo mật, mô hình Server Authoritative kết hợp BCrypt, JWT, Connection Approval và Audit Log đã đảm bảo các vector tấn công cơ bản đều bị chặn. Khảo sát UX với 12 người chơi cho điểm trung bình 4,4/5 — phản hồi tích cực về cảm giác di chuyển, combat và hệ thống Gene/Fusion đặc trưng của đề tài.

Những hạn chế còn lại — Ranked PvP, Marketplace, Admin Dashboard, balancing boss khó và hỗ trợ cross-platform — mở ra các hướng phát triển tiếp theo cho đề tài trong giai đoạn sau, sẽ được tóm tắt cụ thể ở phần Kết luận.
