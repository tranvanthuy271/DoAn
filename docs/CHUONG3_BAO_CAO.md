# CHƯƠNG 3. XÂY DỰNG CÁC CƠ CHẾ GAME

Chương 3 trình bày chi tiết quá trình hiện thực hóa các hệ thống đã được phân tích và thiết kế ở Chương 2 vào sản phẩm game thực tế. Từng module được mô tả theo cấu trúc nhất quán: bối cảnh và mục tiêu của module, kiến trúc lớp tham gia, luồng xử lý chính (cả phía client và phía server), thuật toán cốt lõi (kèm pseudocode hoặc trích đoạn mã C#), giao diện người dùng và các điểm tối ưu đáng chú ý. Cách trình bày này giúp người đọc nắm được không chỉ "cái gì đã được làm" mà còn "vì sao làm như vậy" — yếu tố then chốt trong báo cáo kỹ thuật.

Toàn bộ mã nguồn được tổ chức theo nguyên tắc Separation of Concerns: client Unity chỉ chịu trách nhiệm hiển thị, dự đoán cục bộ (client-side prediction) và thu nhận input; server Authoritative giữ trạng thái thật và mọi tính toán quyết định kết quả game; backend REST API/SignalR phục vụ persistence và meta-service. Tất cả các phần mã trích trong chương này là bản rút gọn để minh họa luồng logic — bản đầy đủ được đính kèm ở Phụ lục.

> **Thứ tự trình bày — bám hai trục đề tài.** Hai trục chính của đề tài là *Gene Evolution* và *Multiplayer Server-Authoritative*. Vì vậy chương 3 mở đầu bằng §3.0 trình bày kiến trúc tổng thể client–server thật trên codebase (cơ sở chung cho mọi hệ thống), sau đó là §3.1–§3.2 nền gameplay (di chuyển, combat) để có ngữ cảnh, §3.3 đào sâu Gene, §3.4–§3.6 các hệ thống nền (Quest/NPC/Equipment), §3.7 ghép tất cả vào Zone và Dungeon đồng đội — đây cũng là nơi multiplayer thể hiện rõ nhất, cùng với phần đồng bộ chi tiết trong §3.0 và §3.3.

---

## 3.0. Kiến trúc tổng thể client–server và mô hình Server Authoritative

### 3.0.1. Cấu hình hai kênh giao tiếp song song

Triển khai thực tế tại `c:\Hub\DoAn\` chia giao tiếp client–server làm **ba kênh** theo tần suất và độ trễ chấp nhận:

| Kênh | Giao thức | Tải | Thư viện thực tế | Ví dụ payload |
|---|---|---|---|---|
| Gameplay realtime | UDP / `unity-transport` | 30–60 Hz | Unity NGO (`NetworkBehaviour`, `NetworkVariable`, `ServerRpc`) | `MoveServerRpc(input, position)`, `ApplyDamageClientRpc(target, dmg)` |
| Persistence / meta | HTTPS / REST + JWT | < 5 req/s/người | ASP.NET Core 7 Controllers (`GameServerApi/Controllers/`) | `POST /api/gene/upgrade`, `GET /api/player/{id}` |
| Social / Party | WebSocket / SignalR | Event-driven | `PartyHub`, `ChatHub` trong `GameServerApi/Hubs/` | `CreateParty`, `InviteMember`, `PartyStateUpdated` |

Cách tách kênh này phản ánh đúng "vì sao game multiplayer khác web app thông thường": dữ liệu gameplay phải đến *trong frame*, dữ liệu persistence phải đến *đúng và bền vững*, dữ liệu social chỉ cần *đến nơi* không cần độ trễ ms. Nếu nhồi tất cả vào một kênh sẽ phải đánh đổi một trong ba thuộc tính trên.

### 3.0.2. Lý do chọn Server Authoritative — và cách hiện thực trong codebase

Bảng 2.0b ở Chương 2 đã trình bày so sánh ba mô hình. Ở đây mô tả *cách triển khai thực tế* trên codebase Mutants Arena:

a) **Mọi giá trị tài sản (HP, Gold, Gene Tier, EXP)** chỉ được ghi từ phía server. Ví dụ điển hình: hàm xử lý nâng cấp Gene nằm trong `GeneController.cs` (`POST /api/gene/upgrade`) — client gửi `geneId` và `itemCount`, server tự load `gene_upgrade_config` từ DB, tự quyết định `successRate`, tự cộng `tier`:

```csharp
// GameServerApi/Controllers/GeneController.cs (rút gọn)
[HttpPost("upgrade")]
public async Task<IActionResult> Upgrade(GeneUpgradeRequest req)
{
    var cfg = await _db.GeneUpgradeConfigs.FirstOrDefaultAsync(
        c => c.TierFrom == currentTier && c.ElementType == elementType);

    // Server-side success rate: client gửi itemCount nhưng KHÔNG được phép gửi rate
    float successRate = cfg.BaseSuccessRate
                      * Math.Min((float)itemCount / cfg.ItemsNeeded, 1f);
    bool success = _rng.NextDouble() < successRate;

    if (success) {
        info.GeneTier += 1;
        info.MaxHp   += tierStat.HpBonus;     // bonus từ gene_tier_stat_config
        info.Attack  += tierStat.AttackBonus;
    }
    await _db.SaveChangesAsync();
    return Ok(response);
}
```

→ Ngay cả khi client bị decompile hoặc bị MitM, kẻ tấn công không thể tự "thắng" upgrade hoặc tự nhảy lên Tier 5 — vì server không hề tin trường `newTier` từ client.

b) **Combat damage** được chốt trên server qua `[ServerRpc]` từ skill activation; `BossAI` chỉ chạy trên server với guard `if (!IsServer) return;`; visual được phát qua `[ClientRpc] SpawnTransientVisualClientRpc()` (xem `Client/Assets/Scripts/Enemy/BossAI.cs`). Client hoàn toàn không có hàm "tự gây sát thương".

c) **Debuff** đồng bộ qua `NetworkList<DebuffEntry>` (xem `DebuffManager.cs`): server thêm/xoá trong list, client nhận tự động qua `OnListChanged` — không có RPC thừa, không có cơ hội cho client tự xoá debuff.

### 3.0.3. Client Prediction + Server Reconciliation cho di chuyển

Riêng với di chuyển, nếu chờ round-trip mới hiển thị thì input lag = RTT (thường 60–120 ms) — không chấp nhận được cho action game. Vì vậy `NetworkPlayerController.cs` áp dụng prediction:

```csharp
// Client/Assets/Scripts/Network/Player/NetworkPlayerController.cs (rút gọn)
void FixedUpdate()
{
    if (!IsOwner) return;
    float h = Input.GetAxis("Horizontal");
    // 1) Predict: client áp dụng input NGAY cho owner
    transform.position += new Vector3(h * speed * dt, 0, 0);
    // 2) Gửi input + vị trí dự đoán lên server để chốt
    MoveServerRpc(h, transform.position, rb.velocity.y, isGrounded);
}

[ServerRpc]
private void MoveServerRpc(float h, Vector2 clientPos, float vy, bool grounded)
{
    // Server nhận vị trí client, lưu vào NetworkVariable syncPosition
    // để các non-owner client khác nhìn thấy
    syncPosition.Value = clientPos;
}
```

Do đặc thù 2D side-scrolling có ground collider trên client (tile), server tin tưởng `clientPos` cho **vị trí**, nhưng vẫn xác thực **hành động** (skill, attack, item use) qua ServerRpc riêng. Nếu cần chống speed-hack quyết liệt hơn, có thể kẹp `|clientPos − lastPos|` trong khoảng `maxSpeed × dt × 1.2` — đây là hướng mở rộng đã được ghi nhận trong `repo/dedicated_server_netcode_notes.md`.

### 3.0.4. Zone-based scaling và visibility filter

`ZoneRoomRegistry` chia thế giới thành nhiều `ZoneRoom` chạy chung process. Hàm `FindLeastLoadedZone()` phân player vào instance ít tải nhất; `AreInSameZone()` được hệ thống NGO sử dụng để lọc visibility — client A đang ở Zone Village không cần biết và không nhận packet của player ở Zone Forest, giảm băng thông và CPU server tuyến tính theo số zone.

---

## 3.1. Xây dựng hệ thống điều khiển và di chuyển nhân vật

### 3.1.1. Mục tiêu thiết kế

Hệ thống điều khiển nhân vật là điểm tiếp xúc đầu tiên giữa người chơi và game, do đó cảm giác điều khiển (game feel) phải đạt yêu cầu "nhạy, mượt, dự đoán được". Mục tiêu cụ thể của module này bao gồm: nhân vật đáp ứng input dưới một frame (≤ 16 ms ở 60 FPS); chuyển trạng thái animation không gián đoạn giữa Idle ↔ Run ↔ Jump ↔ Fall ↔ Dash ↔ Attack ↔ Hit; loại bỏ hai cảm giác bực bội phổ biến trong game side-scrolling là "nhảy hụt mép" (edge slip) và "input trễ"; đảm bảo vật lý 2D không xuyên qua tile collider; và đồng bộ vị trí với server theo mô hình client prediction + server reconciliation để không phá vỡ trải nghiệm khi chơi multiplayer.

### 3.1.2. Kiến trúc lớp tham gia

Module di chuyển được phân tách thành bốn lớp chính nằm trong namespace `Game.Player`:

| Lớp | Vai trò | Chạy ở |
|---|---|---|
| `PlayerInputReader` | Đọc input bàn phím/chuột/gamepad qua Unity Input System, chuyển thành `MoveAxis`, `JumpPressed`, `DashPressed`, `AttackPressed` | Owner client |
| `PlayerController` (NetworkBehaviour) | Tính toán vận tốc, gọi `Rigidbody2D.MovePosition`, quản lý state machine di chuyển, ghi `NetworkTransform` | Server + Owner (prediction) |
| `PlayerAnimator` | Cập nhật `Animator` controller theo `NetworkVariable<MoveState>`; xử lý flip sprite | Mọi client |
| `GroundProbe` | Bắn `Physics2D.OverlapCircle` xuống lớp `Ground` để xác định `IsGrounded`, `IsOnSlope` | Server + Owner |

**Hình 3.1**: *Sơ đồ lớp module điều khiển nhân vật (Class Diagram).*
Mô tả render: sơ đồ UML class diagram bố cục ngang. Khối `PlayerInputReader` bên trái màu xanh dương nhạt với các phương thức `Read()`, `Reset()` và sự kiện `OnInput`. Khối `PlayerController` ở giữa kế thừa `NetworkBehaviour` (Unity Netcode), có ba `NetworkVariable<>` (Position, MoveState, FacingDir) tô màu vàng nhạt. Khối `GroundProbe` phía dưới có thuộc tính `groundLayer`, `checkRadius`. Khối `PlayerAnimator` bên phải có liên kết phụ thuộc (mũi tên đứt nét) tới `Animator`. Các quan hệ composition (mũi tên đặc đầu kim cương đen) chỉ từ `PlayerController` tới `PlayerInputReader`, `GroundProbe`, `PlayerAnimator`. Phông chữ Consolas, đường kẻ 1.5 px, nền trắng.

### 3.1.3. Vòng đời update và state machine di chuyển

Mọi nhân vật ở mỗi `FixedUpdate` (50 Hz) đi qua các pha tuần tự: (1) đọc và đệm input — `Input Buffering`; (2) cập nhật `GroundProbe` để xác định trạng thái tiếp đất; (3) tính vận tốc mục tiêu theo bảng `kMoveSpeed[state]`; (4) áp dụng các "feel modifier" gồm Coyote Time, Jump Buffer, Variable Gravity, Dash Cooldown; (5) ghi vận tốc vào `Rigidbody2D.velocity`; (6) phát sự kiện chuyển trạng thái cho `PlayerAnimator`; (7) (chỉ server) đồng bộ `NetworkTransform`.

Bảng 3.1 mô tả ngắn các trạng thái di chuyển, điều kiện chuyển và animation tương ứng:

**Bảng 3.1: State machine di chuyển nhân vật**

| Trạng thái | Điều kiện vào | Điều kiện ra | Animation clip |
|---|---|---|---|
| Idle | `|MoveAxis| < 0.1` và `IsGrounded` | Nhận input di chuyển hoặc nhảy | `Player_Idle` |
| Run | `|MoveAxis| ≥ 0.1` và `IsGrounded` | Dừng input, nhảy, bị đẩy | `Player_Run` |
| Jump | Người chơi bấm Space, `JumpsLeft > 0` | `velocity.y ≤ 0` → chuyển Fall | `Player_Jump` |
| DoubleJump | Bấm Space lần hai khi đang Jump/Fall, còn lượt | `velocity.y ≤ 0` | `Player_DoubleJump` |
| Fall | `velocity.y < 0` và `!IsGrounded` | `IsGrounded` → Idle/Run | `Player_Fall` |
| Dash | Bấm Shift, còn cooldown ≥ 0 | Hết `DashDuration` (0.18 s) | `Player_Dash` |
| Hit | `OnDamaged` event | Hết `HitStunDuration` | `Player_Hit` |

**Hình 3.2**: *Biểu đồ trạng thái (State Diagram) cho module di chuyển.*
Mô tả render: state diagram dạng vòng, 7 hình bo tròn (rounded rectangle) đặt theo vòng tròn. Idle ở 12 giờ, Run ở 2 giờ, Jump ở 4 giờ, DoubleJump ở 5 giờ, Fall ở 7 giờ, Dash ở 9 giờ, Hit ở 11 giờ. Mũi tên có nhãn điều kiện ngắn (`MoveAxis≠0`, `Space`, `Shift`, `velocity.y<0`...). Trạng thái khởi đầu là chấm đen đặc trỏ vào Idle. Màu sắc: Idle xám, Run xanh lá, Jump/DoubleJump cam, Fall đỏ nhạt, Dash tím, Hit đỏ đậm.

### 3.1.4. Triển khai các "feel modifier"

#### a) Coyote Time và Jump Buffer

Coyote Time cho phép người chơi nhảy trong khoảng 0,15 giây sau khi vừa rời mép platform, loại bỏ cảm giác nhảy hụt. Jump Buffer ghi nhớ thao tác nhấn nút Jump trong 0,12 giây trước khi tiếp đất, sau đó tự kích hoạt nhảy ngay khi `IsGrounded` trở thành true. Hai kỹ thuật này được hiện thực bằng hai bộ đếm hạ dần mỗi `FixedUpdate`:

```csharp
// PlayerController.cs (rút gọn)
private float _coyoteTimer;
private float _jumpBufferTimer;
private const float COYOTE = 0.15f;
private const float BUFFER = 0.12f;

private void HandleJumpFeel()
{
    if (groundProbe.IsGrounded) _coyoteTimer = COYOTE;
    else _coyoteTimer -= Time.fixedDeltaTime;

    if (input.JumpPressed) _jumpBufferTimer = BUFFER;
    else _jumpBufferTimer -= Time.fixedDeltaTime;

    bool canJump = _coyoteTimer > 0f && _jumpBufferTimer > 0f;
    if (canJump)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        _coyoteTimer = 0f;
        _jumpBufferTimer = 0f;
        SetState(MoveState.Jump);
    }
}
```

#### b) Variable Gravity

Để cảm giác nhảy "có trọng lượng", `gravityScale` được nhân hệ số `fallMultiplier = 2.5f` khi đang rơi và `lowJumpMultiplier = 2.0f` khi người chơi thả nút Jump sớm:

```csharp
if (rb.velocity.y < 0f)
    rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
else if (rb.velocity.y > 0f && !input.JumpHeld)
    rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
```

#### c) Dash với invincibility frames

Dash cho phép nhân vật dịch chuyển nhanh 6,5 đơn vị/giây trong 0,18 giây theo hướng đang quay mặt, được cấp 0,2 giây i-frames và bỏ qua trọng lực trong suốt thời gian dash:

```csharp
IEnumerator DashCoroutine()
{
    isInvincible = true; rb.gravityScale = 0f;
    rb.velocity = new Vector2(facingDir * dashSpeed, 0f);
    yield return new WaitForSeconds(dashDuration);
    rb.gravityScale = defaultGravity;
    yield return new WaitForSeconds(iFrameTail);
    isInvincible = false;
}
```

### 3.1.5. Đồng bộ multiplayer: client prediction + server reconciliation

Vì độ trễ mạng 50–150 ms không thể chấp nhận đối với gameplay nhanh, hệ thống áp dụng mô hình tiêu chuẩn của game multiplayer:

1. **Owner client** dự đoán di chuyển ngay khi nhận input và áp dụng cục bộ.
2. Đồng thời gửi `MoveInputRpc(axis, jump, dash, dt)` lên server.
3. **Server** chạy chính xác cùng công thức di chuyển và viết kết quả vào `NetworkTransform`.
4. **NetworkVariable<Vector3> ServerPosition** được phát đến mọi observer. Nếu lệch quá `ReconcileThreshold = 0.4 unit`, client owner snap-back và replay các input chưa được server xác nhận.

**Hình 3.3**: *Sequence Diagram — đồng bộ di chuyển multiplayer.*
Mô tả render: bốn lifeline dọc từ trái sang phải — `Input`, `OwnerClient`, `Server`, `OtherClients`. Mũi tên từ Input → OwnerClient ghi "axis,jump"; OwnerClient áp dụng cục bộ (self-loop `Predict()`); OwnerClient → Server "MoveInputRpc"; Server self-loop `Simulate+Validate`; Server → OtherClients "NetworkTransform sync"; Server → OwnerClient "ServerPosition"; OwnerClient self-loop `if |delta|>0.4 → Reconcile`. Phông Consolas, nền trắng, mũi tên RPC màu đỏ, NetworkVariable màu xanh dương.

---

## 3.2. Xây dựng hệ thống chiến đấu

### 3.2.1. Tổng quan hệ thống chiến đấu

Hệ thống chiến đấu của Mutants Arena là realtime side-scrolling combat với hỗn hợp đòn cận chiến (melee) và đòn xa (projectile). Mọi quyết định mang tính authoritative (chi tiết damage, knockback, kill, drop) đều thực hiện trên server; client chỉ chịu trách nhiệm dự đoán VFX, SFX và animation để giảm độ trễ cảm nhận. Hệ thống bám sát ba nguyên tắc: (1) tách bạch *hitbox* (vùng gây sát thương) và *hurtbox* (vùng nhận sát thương); (2) toàn bộ buff/debuff đi qua `IStatModifier`; (3) công thức damage là một pure function nhận vào `AttackContext` và trả về `DamageResult`, phục vụ thuận tiện cho unit test.

### 3.2.2. Hitbox / Hurtbox

`Hitbox` là một `Collider2D` đặt isTrigger ở khung xương vũ khí, kích hoạt theo Animation Event do animator gọi vào đúng frame "active" (ví dụ frame 4–6 trong clip `Slash`). Mỗi `Hurtbox` được gắn vào root entity (Player/Enemy/Boss). Khi `OnTriggerEnter2D` xảy ra trên server, một `DamageRequest` được khởi tạo:

```csharp
public struct DamageRequest {
    public ulong   AttackerNetId;
    public ulong   TargetNetId;
    public float   BaseDamage;
    public Element AttackerElement;
    public Element TargetElement;
    public DamageKind Kind;       // Physical, Skill, DoT
    public float   CritChance;
    public float   CritMultiplier;
    public bool    CanBlock;
}
```

`CombatResolver` (singleton server-side) nhận request, tra cứu `IStatModifier` của attacker/target, áp dụng công thức ở 3.2.3, và bắn `DamageDealtClientRpc` để mọi client cập nhật floating-text/VFX. Một bảng `IgnoreList<ulong, float>` chống multi-hit cùng một animation (mặc định 0,25 giây/target).

### 3.2.3. Công thức tính sát thương

Công thức được hợp nhất tất cả lớp modifier trong một biểu thức duy nhất:

$$\text{Damage} = \big(\text{Base} \times (1 + \text{StatBonus}) - \text{Def}_\text{eff}\big) \times M_\text{elem} \times M_\text{crit} \times M_\text{combo} \times R_\text{var}$$

Trong đó:

- $\text{Base}$ — sát thương cơ bản của đòn đánh (từ stat ATK hoặc damage của skill).
- $\text{StatBonus}$ — tổng % bonus từ Gene Tier, trang bị, buff (cộng dồn).
- $\text{Def}_\text{eff} = \text{Def} \times (1 - \text{ArmorPenetration})$.
- $M_\text{elem}$ — hệ số tương khắc 6 nguyên tố (1,5 / 1,0 / 0,75 — xem 3.3).
- $M_\text{crit} = 2,0$ nếu Crit thành công, ngược lại 1,0.
- $M_\text{combo}$ — 1,0 / 1,1 / 1,15 / 1,2 cho combo hit thứ 1/2/3/4 (reset sau 1,2 s không đánh).
- $R_\text{var}$ — biến ngẫu nhiên uniform [0,9; 1,1] để tránh dồn damage cố định.

**Bảng 3.2: Ma trận tương khắc 6 nguyên tố (Attacker → Target)**

| Att\Tgt | Kim | Mộc | Thủy | Hỏa | Thổ | Phong |
|---|---|---|---|---|---|---|
| Kim | 1,0 | 1,5 | 0,75 | 1,0 | 0,75 | 1,5 |
| Mộc | 0,75 | 1,0 | 1,5 | 0,75 | 1,5 | 1,0 |
| Thủy | 1,5 | 0,75 | 1,0 | 1,5 | 1,0 | 0,75 |
| Hỏa | 1,0 | 1,5 | 0,75 | 1,0 | 1,5 | 0,75 |
| Thổ | 1,5 | 0,75 | 1,0 | 0,75 | 1,0 | 1,5 |
| Phong | 0,75 | 1,0 | 1,5 | 1,5 | 0,75 | 1,0 |

### 3.2.4. Hệ thống skill và projectile

Skill được mô tả bằng `ScriptableObject` `SkillDefinition` với các trường: `SkillId`, `Cooldown`, `ManaCost`, `CastTime`, `Element`, `Kind` (Melee/Projectile/AoE/Buff), `DamageFormula`, `VfxPrefab`, `SfxClip`. Projectile là `NetworkObject` `Projectile2D` có script `ProjectileMover` (server-side authoritative) thực thi quỹ đạo (thẳng / parabol / homing).

**Hình 3.4**: *Sequence Diagram — luồng phóng skill projectile.*
Mô tả render: 5 lifeline: `Input`, `ClientUI`, `OwnerPlayer`, `Server`, `OtherClients`. Bước 1: người chơi bấm Q (ClientUI shows cooldown spinner). 2: OwnerPlayer → Server `CastSkillRpc(skillId)`. 3: Server validate (cooldown, mana, GCD). 4: Server `Spawn` Projectile NetworkObject. 5: Projectile → Server `OnTriggerEnter2D` → CombatResolver. 6: Server → AllClients `DamageDealtClientRpc(target, dmg, isCrit)`. 7: AllClients hiển thị floating text + VFX. Màu RPC đỏ, Spawn xanh lá.

### 3.2.5. Buff / Debuff system

Mỗi buff/debuff là một `BuffInstance(BuffDef def, float remaining, int stack)` đặt trong `BuffContainer` của entity. `BuffContainer.Tick(dt)` chạy mỗi `FixedUpdate` trên server: hạ `remaining`, kích hoạt `OnTick` (cho DoT), `OnExpire` (cho cleanup), và phát `NetworkList<BuffSnapshot>` cho HUD client (đã ghi nhận chi tiết trong `repo/buff_hud_skill_system_architecture.md`). Các trạng thái Burn/Poison/Freeze/Stun/Shield/Regen được hiện thực dưới dạng `BuffDef` riêng — mở rộng không cần sửa code combat core.

### 3.2.6. Hit-stop và camera shake

Hit-stop là kỹ thuật dừng game 1–2 frame ngay khi đòn đánh trúng, tạo cảm giác "thịt" cho combat. Hiện thực bằng coroutine ngắn đặt `Time.timeScale = 0.01f` trong 2 frame rồi khôi phục. Camera shake dùng Cinemachine Impulse Source với biên độ 0,12 / 0,25 / 0,4 tương ứng damage nhỏ / trung / lớn.

---

## 3.3. Xây dựng hệ thống Gene (Ngũ Hành) và quản lý chỉ số nhân vật

### 3.3.1. Mô hình dữ liệu Gene

Một nhân vật có thể đeo tối đa **3 Gene**: 1 Gene chính (Main) cùng nguyên tố lớp nhân vật và 2 Gene phụ (Sub). Mỗi Gene có 5 Tier; tier càng cao, % bonus càng lớn và mở khóa thêm passive. Cấu trúc bảng `gene_inventory` đã trình bày ở 2.2.4 (ERD).

**Bảng 3.3: Bonus chỉ số theo Tier của Gene**

| Tier | ATK% | DEF% | HP% | Crit% | Unlock passive |
|---|---|---|---|---|---|
| 1 | +5 | +5 | +5 | +0 | — |
| 2 | +12 | +12 | +12 | +3 | Passive cấp 1 |
| 3 | +22 | +22 | +22 | +6 | Passive cấp 1 + 1 skill phụ |
| 4 | +35 | +35 | +35 | +10 | Passive cấp 2 |
| 5 | +50 | +50 | +50 | +15 | Passive cấp 2 + Skill Ultimate |

### 3.3.2. Nâng cấp Gene (Tier-Up)

Quy trình nâng Tier do người chơi khởi tạo ở UI Gene Forge, server kiểm tra điều kiện và thực hiện. Toàn bộ tham số (gold cost, item cần, success rate) được nạp từ bảng `gene_upgrade_config` trong CSDL — *không* hard-code trong source code Unity — cho phép vận hành thay đổi cân bằng game mà không cần build lại client.

**Bảng 3.3a: Trích `gene_upgrade_config` (nguyên tố Fire, 4 cấp nâng cấp)**

| tier_from → tier_to | gene_exp | gold | stone_id | stone_needed | stone_min | base_success_rate |
|---|---|---|---|---|---|---|
| 1 → 2 | 500 | 10 000 | 17 | 5 | 2 | 0.80 |
| 2 → 3 | 1 500 | 30 000 | 17 | 8 | 3 | 0.65 |
| 3 → 4 | 4 000 | 80 000 | 17 | 12 | 4 | 0.50 |
| 4 → 5 | 10 000 | 200 000 | 17 | 20 | 6 | 0.35 |

Success rate cuối được tính theo công thức $r_{\text{final}} = r_{\text{base}} \times \min\!\big(\tfrac{n_{\text{item}}}{n_{\text{needed}}}, 1.0\big)$ — người chơi có thể "đốt" nhiều item hơn `stone_needed` để bảo đảm 100%, hoặc liều với `stone_min` để tiết kiệm. Đoạn pseudocode sau phản ánh đúng hàm `Upgrade()` thực tế trong `GeneController.cs` (đã trích snippet đầy đủ ở §3.0.2):

```text
function UpgradeGene(player, geneId, itemCount):
    g = LoadGene(geneId)
    require g.OwnerId == player.Id
    require g.Tier < 5
    cfg = SELECT * FROM gene_upgrade_config
          WHERE tier_from = g.Tier AND element_type = g.Element
    require itemCount >= cfg.stone_min
    require player.Gold >= cfg.gold
    rate = cfg.base_success_rate * min(itemCount/cfg.stone_needed, 1.0)
    BeginTx:
        player.Stones[cfg.stone_id] -= itemCount
        player.Gold                 -= cfg.gold
        success = Random() < rate
        if success:
            g.Tier += 1
            ApplyStatBonus(player, gene_tier_stat_config[g.Tier])
        SaveGene(g); SavePlayer(player)
        WriteLog("gene_upgrade", player.Id, g.Id, success, g.Tier)
    CommitTx
    return { success, newTier: g.Tier }
```

### 3.3.3. Gene Fusion (Hybrid)

Fusion kết hợp 2 Gene Tier ≥ 2 khác nguyên tố để tạo Hybrid Gene chiếm slot phụ. Cấu hình **15 tổ hợp Hybrid** được lưu trong bảng `gene_hybrid_config` (xem `gamedb.sql` dòng 320) gồm: `element_a`, `element_b`, `fusion_cost`, `hp_bonus`, `atk_bonus`, `def_bonus`, `immune_elements` (mảng nguyên tố miễn nhiễm), `hybrid_prefab_path` (Resources path cho visual), `primary_skill_keep_count` (số skill giữ lại từ hệ chính). Code xử lý nằm trong `GeneFusionService.cs`, tài liệu vận hành tại `HUONG_DAN_FUSION_KIM_PHONG.md`.

**Bảng 3.4: Một số tổ hợp Fusion tiêu biểu (trích từ `gene_hybrid_config`)**

| element_a | element_b | Hybrid name | HP+ | ATK+ | DEF+ | Immune | Đặc trưng |
|---|---|---|---|---|---|---|---|
| Fire | Metal | Molten Metal | +500 | **+700** | +200 | — | Bleed + Burn, xuyên giáp |
| Water | Wood | Venom Frost | **+1 500** | +250 | **+600** | — | Poison + Slow tank |
| Water | Earth | Frost Earth | +800 | +200 | +800 | Fire | Auto shield khi HP < 30% |
| Fire | Earth | Volcanic Earth | +1 200 | +400 | +500 | — | Reflect 20% damage |
| Metal | Wood | Sharp Thorns | +600 | +350 | +700 | — | Reflect 35% melee, +15% Regen |

**Hình 3.5**: *Mockup UI Gene Forge.*
Mô tả render: panel dọc 1080×720, nền sci-fi tối với hexagon pattern mờ. Bên trái: lưới 3×3 slot Gene phát sáng theo màu nguyên tố. Giữa: vùng "Forge" hình tròn lớn xoay chậm. Bên phải: bảng chỉ số trước/sau nâng cấp với hai cột "Hiện tại / Sau khi nâng" và mũi tên xanh lá khi tăng. Dưới: nút "Nâng cấp" cam neon, "Fusion" tím neon, "Reset" xám. Font Rajdhani, accent màu cyan #00E5FF.

### 3.3.4. Quản lý chỉ số nhân vật

`StatBlock` là lớp dữ liệu chứa các stat cơ bản (Base) + bonus (FromGene, FromEquip, FromBuff). `StatCalculator` tổng hợp theo công thức `Final = (Base + Flat) × (1 + SumPercent)` và phát `OnStatsChanged` cho HUD. Mỗi lần thay đổi trang bị / nâng Gene / buff hết hạn, `StatCalculator.Recompute()` được gọi đúng một lần để tránh tính toán dư thừa.

---

## 3.4. Xây dựng hệ thống nhiệm vụ và quái vật

### 3.4.1. Hệ thống Quest

Quest được mô tả bằng `QuestDefinition` (ScriptableObject) gồm: `QuestId`, `Title`, `Description`, `Type` (Main/Side/Daily), `Objectives` (mảng `QuestObjective` — Kill/Collect/Talk/Reach), `Rewards` (Exp/Gold/Item/Fragment). Tiến trình quest của player lưu trong bảng `player_quests` (status, progress JSON).

Luồng xử lý: khi server phát sự kiện nội bộ (ví dụ `OnEnemyKilled(enemy, killer)`), `QuestTracker` duyệt các quest đang `In_Progress` của killer, tăng counter `Objectives[i].Current`; nếu đủ, mark `Completed`; khi đủ tất cả objective → mark quest `Ready_To_Claim`; người chơi tới NPC giao quest để nhận thưởng.

**Hình 3.6**: *Activity Diagram quy trình nhận và hoàn thành nhiệm vụ.*
Mô tả render: activity diagram dọc với swim-lane "Player | Server | DB". Bắt đầu: Player nói chuyện NPC → Server gọi `OfferQuest` → DB ghi status `In_Progress`. Vòng chính: Player thực hiện gameplay → Server lắng nghe event → `QuestTracker.OnEvent()` → cập nhật progress → nếu đủ → status `Ready_To_Claim` → notify client → Player tới NPC nhận thưởng → DB ghi status `Completed`. Quyết định (hình thoi) màu vàng, action (hình bo tròn) màu xanh.

### 3.4.2. Hệ thống quái vật & boss

Quái vật được phân ba lớp: Normal (FSM 3 trạng thái: Patrol/Chase/Attack), Elite (thêm trạng thái Special), và Boss (Phase System với JSON config). Chi tiết được lưu trong các tài liệu `HUONG_DAN_ENEMY_BOSS.md`, `HUONG_DAN_BOSS_ADVANCED.md` và bản memo `repo/enemy_skill_projectile_notes.md`, `repo/enemy_attack_animation.md`.

**Bảng 3.5: Cấu hình AI cho 3 lớp quái**

| Loại | Kiến trúc AI | Số state | Có Phase | Drop |
|---|---|---|---|---|
| Normal | FSM | 3 (Patrol, Chase, Attack) | Không | Gold + Fragment thấp |
| Elite | FSM + 1 Special skill | 4 | Không | Gold + Fragment trung |
| Boss | FSM + Phase System (JSON) | 4–6 mỗi phase | Có (3 phase) | Mutant Core + Set gear |

#### Boss Phase System

`Boss.phases_json` mô tả các phase theo dạng:

```json
{
  "phases": [
    { "hpThreshold": 1.0, "skills": ["slash","ground_slam"], "moveSpeed": 2.0 },
    { "hpThreshold": 0.6, "skills": ["slash","ground_slam","fire_rain"], "moveSpeed": 2.6, "enrage": true },
    { "hpThreshold": 0.3, "skills": ["fire_rain","meteor","teleport"], "moveSpeed": 3.2, "ultimateInterval": 12 }
  ]
}
```

`BossController` quan sát `HP%` và chuyển phase khi vượt ngưỡng; mỗi phase load lại bộ skill cooldown và pattern di chuyển. Việc giữ cấu hình ở dạng JSON cho phép balancing không cần recompile.

**Hình 3.7**: *Flowchart Boss Phase System.*
Mô tả render: flowchart dọc — Start → BossSpawned → đo HP% → quyết định (hình thoi) HP>60%? → Phase 1 (xanh lá); else HP>30%? → Phase 2 (cam, có badge Enrage); else → Phase 3 (đỏ, có badge Ultimate). Trong mỗi phase: vòng lặp Choose Skill → Cast → Cooldown. Cuối: HP≤0 → DropLoot → End. Nền trắng, đường viền 2 px.

### 3.4.3. Spawn config và respawn

`MapSpawnConfig` (ScriptableObject) chứa danh sách `SpawnPoint(position, enemyId, respawnTime, maxConcurrent)` cho mỗi zone. `SpawnManager` server-side đếm số quái sống ở mỗi point; khi giảm dưới `maxConcurrent`, sau `respawnTime` giây thì spawn mới. Cơ chế chi tiết được tham chiếu trong `HUONG_DAN_MAP_SPAWN_CONFIG.md` và memo `repo/spawn_config_compat.md`.

---

## 3.5. Hệ thống NPC và cửa hàng — đối thoại, mua/bán, giao dịch

### 3.5.1. Kiến trúc NPC

NPC là `NetworkObject` đơn giản (không di chuyển) gắn script `NpcInteractable` mở UI menu khi player nhấn phím tương tác trong bán kính 2 đơn vị. Mỗi NPC có `NpcDefinition` (ScriptableObject) liệt kê các action: `Dialogue`, `OpenShop`, `OpenBlacksmith`, `OfferQuest`, `Teleport`, `OpenGeneForge`. Menu được render động (dynamic menu) chỉ với action mà NPC này hỗ trợ — mẫu thiết kế được ghi trong memo `repo/npc_dynamic_menu.md`.

**Hình 3.8**: *Wireframe UI hội thoại NPC + Menu hành động.*
Mô tả render: panel hình chữ nhật bo góc đặt giữa-dưới màn hình. Bên trái avatar NPC tròn 96×96 px, bên phải hộp text 600×120 px nền đen mờ 80%, viền cyan. Bên dưới text là 3–5 nút hành động xếp ngang: "Trò chuyện", "Cửa hàng", "Nâng cấp", "Nhận nhiệm vụ", "Rời đi" — mỗi nút có icon nhỏ bên trái và highlight cam khi hover. Phím tắt số 1–5 hiện góc dưới phải mỗi nút.

### 3.5.2. Shop và Blacksmith

Shop cung cấp danh sách hàng hoá bán/mua theo bảng `npc_shop_items(shop_id, item_id, buy_price, sell_price, stock)`. Quy trình mua: client gọi `BuyItemRpc(shopId, itemId, quantity)` → server kiểm tra gold, stock, inventory slot → transaction → trả `BuyResultRpc(success, newGold, newItem)`. Blacksmith là NPC đặc biệt mở UI cường hoá / ghép đá / forge trang bị (xem 3.6).

NPC hỗ trợ **multi-shop**: cùng một NPC có thể mở nhiều shop khác nhau (ví dụ "Vũ khí" và "Vật phẩm tiêu hao") qua menu phụ — thiết kế đã chuẩn hoá ở `HUONG_DAN_NPC_SHOP_BLACKSMITH_MULTI_SHOP.md`.

**Bảng 3.6: Danh sách action điển hình của các NPC**

| NPC | Zone | Actions |
|---|---|---|
| Lý Trưởng | Làng khởi đầu | Dialogue, OfferQuest |
| Thợ Rèn Hắc Phong | Làng khởi đầu | OpenBlacksmith (cường hoá, ghép đá) |
| Thương Nhân Lưu Động | Mọi zone | OpenShop (đa tab: vũ khí / tiêu hao) |
| Sứ Giả Phó Bản | Cổng dungeon | Teleport (vào dungeon theo party) |
| Đạo Sĩ Ngũ Hành | Thành chính | OpenGeneForge |

### 3.5.3. Luồng giao dịch mua hàng

**Hình 3.9**: *Sequence Diagram giao dịch mua hàng.*
Mô tả render: lifeline Player, ShopUI, Server, DB. ShopUI bấm Buy → Server: `BuyItemRpc` → Server validate (gold, slot, stock) → BeginTx → DB UPDATE gold, INSERT inventory, UPDATE stock → CommitTx → Server → ShopUI `BuyResultRpc(ok)` → ShopUI refresh.

---

## 3.6. Hệ thống trang bị và nâng cấp

### 3.6.1. Mô hình trang bị

Trang bị thuộc một trong ba slot **Weapon / Armor / Accessory**, có 5 mức hiếm Common → Uncommon → Rare → Epic → Legendary và 6 Tier cường hoá +0 → +20 (theo bậc +5 → +10 → +15 → +20). Cấu trúc bảng `player_equipment` đã trình bày ở Chương 2. Mỗi item lưu thêm danh sách `Sockets[]` (cho ghép đá Ngũ Hành) và `RandomStats[]` (rerollable).

### 3.6.2. Cường hoá (Enhancement)

Cường hoá tăng chỉ số nền của trang bị; xác suất thành công giảm dần theo cấp, có thể thất bại làm tụt cấp ở mức cao (theo `HUONG_DAN_CUONG_HOA_UNITY.md`):

**Bảng 3.7: Bảng cường hoá trang bị (rút gọn)**

| Tier hiện tại | Xác suất thành công | Khi thất bại | Vật liệu |
|---|---|---|---|
| +0 → +5 | 100% → 80% | Giữ nguyên | 1 Stone/Tier |
| +5 → +10 | 70% → 50% | Giữ nguyên | 2 Stone + 500 Gold |
| +10 → +15 | 45% → 30% | Mất 1 cấp | 3 Stone + 2000 Gold + 1 Protect |
| +15 → +20 | 25% → 10% | Mất 2 cấp hoặc vỡ | 5 Stone + 10000 Gold + 1 Mutant Core |

Server thực hiện cường hoá atomic — `Random.NextDouble()` chỉ chạy trên server, kết quả gửi về client để play animation phù hợp (success: vòng sáng xanh; fail: hiệu ứng đỏ + rung).

### 3.6.3. Ghép đá Ngũ Hành (Socket)

Mỗi trang bị có 0–4 socket (mở thêm bằng Đục Đá NPC). Đá Ngũ Hành mang bonus theo nguyên tố; ghép thành công cộng dồn vào `StatBlock`. 3 đá cùng hệ kích hoạt **Set Bonus** (ví dụ 3 Hỏa = +15% Burn duration). Tham chiếu chi tiết `HUONG_DAN_GHEP_DA.md`.

**Hình 3.10**: *Mockup UI Cường hoá + Ghép đá.*
Mô tả render: hai cột song song. Cột trái "Cường hoá": item slot 128×128 phát sáng, bên dưới thanh tiến trình tier +0…+20 chia mốc, nút "Cường hoá" cam neon, ô vật liệu ngang dưới. Cột phải "Ghép đá": item slot 128×128 với 4 socket lục giác nhỏ xếp dọc bên cạnh; mỗi socket có thể drag đá Ngũ Hành (5 màu nguyên tố). Phía dưới hiển thị Set Bonus active. Toàn bộ nền sci-fi xám lam.

### 3.6.4. Trang bị tier animation

Trang bị từ +10 trở lên hiển thị aura phát sáng (Tier 1), từ +15 có hạt particle quanh nhân vật (Tier 2), từ +18 thêm trail kiếm (Tier 3) — theo cấu hình `EquipmentTierAnimationConfig` (xem `HUONG_DAN_CONFIG_EQUIPMENT_TIER_ANIMATION.md`).

---

## 3.7. Hệ thống bản đồ, khu vực và phó bản

### 3.7.1. Kiến trúc Zone-based server

`ZoneRoomRegistry` quản lý các Zone đồng thời trong cùng một server process. Mỗi zone là một scene `Additive` riêng, có physics isolation (Layer Mask riêng) để va chạm zone A không ảnh hưởng zone B — chi tiết kỹ thuật được mô tả trong `HUONG_DAN_MAP_ADDITIVE_PHYSICS_ISOLATION.md`. Khi player teleport, server `Despawn` khỏi NetworkObjectList của zone cũ, `Spawn` ở zone đích, gửi `SceneEventBatch` cho client load additive scene tương ứng.

**Hình 3.11**: *Sơ đồ Zone-based Server Architecture.*
Mô tả render: hình chữ nhật lớn ngoài "GameServer Process" màu xám nhạt. Bên trong 4 hình chữ nhật nhỏ "Zone_Village", "Zone_Forest", "Zone_Mine", "Dungeon_Wave_01" mỗi cái có icon scene + danh sách player + danh sách enemy. Mũi tên 2 chiều giữa các zone qua "ZoneRoomRegistry" ở giữa. Bên ngoài có icon "Unity Client A/B/C" kết nối qua Netcode tới các zone cụ thể.

### 3.7.2. Multi-Zone và NPC theo zone

Mỗi zone có danh sách NPC riêng nạp từ `ZoneConfig`. Khi load zone, server spawn các NPC theo cấu hình. Hệ thống được mô tả chi tiết trong `HUONG_DAN_MAP_MULTI_ZONE_NPC.md` (43,8 KB).

### 3.7.3. Phó bản (Dungeon) Wave-based

Dungeon là zone đặc biệt sinh khi party trigger cổng vào, server tạo `DungeonInstance` mới (instance riêng cho party), nạp `DungeonConfig` định nghĩa số wave, danh sách enemy mỗi wave, boss cuối. `WaveController` lần lượt:

```text
for wave in waves:
    Spawn enemies of wave
    Wait until all enemies dead
    Play "Wave Cleared" HUD
    Wait 3s
Spawn Boss
Wait Boss dead
Drop loot proportional to damage contribution
Show "Dungeon Cleared" screen, return party to lobby zone
```

Khi party leader rời, instance giữ trạng thái 30 giây để chờ reconnect, sau đó destroy. Cấu hình wave & enemy theo `HUONG_DAN_CONFIG_DUNGEON_WAVE_ENEMY.md`, UI theo `HUONG_DAN_UI_PHO_BAN.md` và `HUONG_DAN_WAVE_HUD_UNITY.md`.

**Bảng 3.8: Mẫu cấu hình một dungeon 5 wave + Boss**

| Wave | Enemy types | Số lượng | Note |
|---|---|---|---|
| 1 | Slime, Bat | 8 | Khởi động |
| 2 | Slime, Bat, Wolf | 10 | Thêm enemy nhanh |
| 3 | Wolf, Skeleton | 10 | DoT nhiều |
| 4 | Skeleton, Elite Knight | 6 + 1 | Có Elite |
| 5 | Mini-Boss Lich | 1 | Phase 2 |
| Boss | Boss Dragon | 1 | 3 phase, drop Mutant Core 30% |

**Hình 3.12**: *UI HUD Wave và Boss Phase.*
Mô tả render: HUD top-center "Wave 3 / 5" với progress bar enemy còn sống (đỏ). Khi đến boss: bottom HUD đầy đủ chiều ngang, thanh HP boss dài, dưới có 3 marker tròn cho 3 phase (sáng dần khi đi qua). Bên dưới HP có icon nguyên tố boss hiện tại + tên skill đang cast.

### 3.7.4. Party System và đồng bộ multiplayer 4 người

Party là điểm hội tụ hai trục đề tài: nhiều người chơi (multiplayer) cùng vận dụng Gene khác nguyên tố để vượt boss. Triển khai thực tế tách thành hai tầng theo đúng phân chia kênh ở §3.0.1:

a) **Tầng meta (SignalR — `PartyHub.cs` trong `GameServerApi/Hubs/`)** quản lý mời/tham gia/rời nhóm và đồng bộ presence (map, zone, level, element của từng thành viên). Mỗi party là một SignalR group; mọi event đẩy realtime cho tất cả thành viên kể cả khi họ đang ở map khác. Snippet xử lý leader leave:

```csharp
// GameServerApi/Hubs/PartyHub.cs (rút gọn)
public override async Task OnDisconnectedAsync(Exception? e)
{
    var party = FindPartyByUser(userId);
    if (party == null) return;

    // Nếu leader disconnect, chuyển leader cho member còn lại
    if (string.Equals(party.LeaderUserId, userId))
        party.LeaderUserId = party.MemberUserIds.First();

    party.MemberUserIds.Remove(userId);
    await Clients.Group(BuildGroupName(party.Id))
        .SendAsync("PartyStateUpdated", BuildPartyStateUnsafe(party));
}
```

b) **Tầng gameplay (NGO — `DungeonInstance` + `WaveController`)** chỉ chạy khi party vào dungeon. Server tạo `DungeonInstance` riêng cho từng party (instance isolation), spawn wave, theo dõi damage contribution, chia loot tỉ lệ. Logic cốt lõi:

```text
DungeonInstance(party):
    isolate physics scene cho party                 // additive scene, layer mask
    for wave in dungeon_config.waves:
        spawn enemies of wave
        wait until all enemies dead
        broadcast "WaveCleared" via ClientRpc
        wait 3s
    spawn Boss with phases_json
    wait Boss dead
    contrib = sum damage dealt per player
    for p in party.members:
        loot[p] = boss.loot × (contrib[p] / total_contrib)
    SaveDungeonRun(party, result, loot)             // ghi DB qua REST
    schedule destroy instance after 30s
```

Phép chia loot theo damage contribution buộc party phải có *vai trò phân hoá* — và đây chính là điểm Gene Ngũ Hành phát huy: party đi đánh boss Hỏa lý tưởng có 1 Gene Thủy (counter ×1.5), 1 Gene Phong (burst), 1 Gene Thổ (tank), 1 Gene Mộc (debuff thiêu đốt). Không có team-composition kiểu này thì party Thiên Hỏa toàn bộ sẽ chịu damage Hỏa lên Hỏa = ×1.0 và mất nhiều tài nguyên hồi máu.

Chi tiết bổ sung: chia EXP/Loot trong `HUONG_DAN_PHO_BAN_VA_TO_DOI.md`; preset prefab party UI trong `HUONG_DAN_PREFAB_PHO_BAN_TO_DOI.md`; bản memo runtime tại `repo/character_menu_relation_party_only.md`.

---

## 3.8. Tổng kết chương 3

Chương 3 đã trình bày toàn bộ quá trình hiện thực hoá 7 nhóm hệ thống cốt lõi của Mutants Arena: điều khiển di chuyển, chiến đấu, Gene Ngũ Hành, nhiệm vụ & quái vật/boss, NPC & cửa hàng, trang bị & nâng cấp, bản đồ & phó bản. Với mỗi hệ thống, báo cáo đã chỉ ra mô hình lớp, luồng xử lý chính, công thức/thuật toán quan trọng và minh hoạ UI/sequence cần thiết. Đặc biệt, mọi quyết định ảnh hưởng trạng thái game đều được giữ trên server (Server Authoritative), client chỉ tham gia ở phần dự đoán hiển thị — đảm bảo tính nhất quán dữ liệu và chống gian lận. Trên nền tảng này, Chương 4 sẽ trình bày kết quả thực nghiệm và đánh giá hiệu năng tổng thể của hệ thống.
