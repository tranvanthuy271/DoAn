# Kế hoạch chi tiết hoàn thành tới 01/05 (Online-first + Farm 12 người + PvP 1v1/2v2)

## 1) Scope chính (đúng theo yêu cầu bạn chốt)
- **Farm map**: tối đa **12 người**, có thể **solo farm**.
- **Progression**: **nhặt đồ → nâng cấp đồ lên cấp → tăng chỉ số + mở/tăng sức mạnh skill**.
- **PvP**: người chơi **tạo lobby sau** rồi đánh **1 ván** theo mode **1v1** hoặc **2v2**.
- **Online**: cần **Internet** (không chỉ LAN).

## 2) Kiến trúc bắt buộc (để khỏi “offline → online”)
- **Networking**: Unity Netcode for GameObjects (NGO).
- **Internet/NAT**: Unity Gaming Services (UGS) **Authentication + Lobby + Relay**.
- **Lưu tiến trình**: UGS **Cloud Save** (inventory/equipment level/skill unlock).
- **Authority**: **host-authoritative** (host là server). Client chỉ gửi **intent** (move/attack/pickup/upgrade).

## 3) Mốc bàn giao (bắt buộc phải đạt để kịp 01/05)
- **M1 – 09/02**: Internet join được (Auth + Lobby + Relay), **tối thiểu 2 người** thấy nhau, di chuyển/anim sync ổn.
- **M2 – 23/02**: PvP “1 ván” hoàn chỉnh (round start/end, win/lose), **combat/HP/damage server-authoritative**.
- **M3 – 09/03**: Farm map chạy được **tối thiểu 6 người**, có enemy server + drop/pickup server.
- **M4 – 30/03**: Farm map đạt **12 người**, có inventory + nâng cấp đồ lên cấp + skill progression, lưu Cloud Save.
- **M5 – 13/04**: UI/UX end-to-end (menu → lobby → vào game → farm/upgrade → pvp → end), test Internet ổn định.
- **M6 – 27/04**: Bug sprint + tối ưu + đóng gói build + tài liệu.
- **01/05**: Release candidate (demo + video + hướng dẫn).

## 4) Luồng chơi (để bạn luôn bám đúng khi dev)
1) **MainMenu**: đăng nhập UGS (anonymous), hiện trạng thái online.
2) **Farm Lobby**: tạo/join lobby farm (max 12) → Relay join → vào FarmScene.
3) **FarmScene**: đánh quái → drop item/material → pickup → vào **Upgrade UI** nâng cấp đồ/skill.
4) **PvP Lobby**: tạo lobby PvP (chọn 1v1 hoặc 2v2) → vào PvPScene → đánh 1 ván → end → quay về menu/lobby.
5) **Cloud Save**: load khi login, save khi pickup/upgrade/end match.

---

## 5) Kế hoạch theo tuần (20/01 → 01/05)

### Tuần 1 (20/01–26/01): Chốt nền dữ liệu (item/upgrade/skill) + chuẩn hoá network “online-first”
- **Đầu ra**:
  - Spec rõ: item slot nào, lên cấp tăng gì, skill mở theo mốc nào.
  - Quy ước authority + RPC + NetworkVariables dùng cho gameplay.
- **Việc làm**:
  - Tạo data model tối thiểu (để code không đập đi làm lại):
    - `ItemDefinition` (ScriptableObject): id, slot, rarity, baseStats, upgradeCurve, skillUnlockAtLevel.
    - `PlayerProfile` (Cloud Save): inventory, equipped, itemLevels, currencies/materials.
  - Chuẩn hoá prefab network: Player/Enemy/Drop đều có `NetworkObject` và đăng ký NetworkPrefabs.
  - Tách “Gameplay logic” khỏi “Network glue” (để sau dễ mở rộng 12 người).

#### Tuần 1 - Hướng dẫn chi tiết theo ngày (bắt buộc làm xong để tuần 2/3 code không bị đập)

##### Ngày 1: Chốt “core loop” + phạm vi tối thiểu (MVP)
- **Bạn phải chốt bằng văn bản (1 trang)**:
  - Farm: 12 người, solo được, quái rơi **item + material**.
  - Progression: **item lên cấp** → tăng stat + mở/tăng sức mạnh skill.
  - PvP: tạo lobby, mode 1v1/2v2, “1 ván” có win/lose.
- **Definition of Done**:
  - Bạn viết ra được 5 câu trả lời rõ ràng:
    - Farm rơi những gì? (item/material/coin?)
    - Nâng cấp tiêu gì? (material/coin)
    - Item có mấy slot? (VD: weapon/armor/boots/amulet)
    - Skill mở theo mốc nào? (VD: weapon lv3 mở skill 1, lv6 mở skill 2)
    - PvP thắng theo gì? (VD: score/time/last-man-standing)

##### Ngày 2: Thiết kế dữ liệu Item (ScriptableObject) + bảng upgrade curve
- **Làm trên giấy/markdown trước, chưa cần code**:
  - Danh sách **slot** (ít thôi để kịp): ví dụ 3 slot (Weapon/Armor/Accessory).
  - Danh sách **rarity**: Common/Rare/Epic (tối thiểu 2).
  - **Stat set tối thiểu** để không nổ scope: HP, Damage, MoveSpeed, CooldownReduction.
  - **Upgrade curve**: level 1..10 (hoặc 1..15), mỗi level tăng bao nhiêu stat.
- **Definition of Done**:
  - Có bảng rõ ràng, ví dụ:
    - Weapon level 1..10: Damage +x/level, CDR +y/level
    - Armor level 1..10: HP +x/level

##### Ngày 3: Thiết kế PlayerProfile (lưu Cloud Save) + format dữ liệu
- **Chốt format lưu** (ưu tiên đơn giản, dễ debug):
  - `inventory`: list item instance (mỗi cái có `definitionId`, `rarity`, `seed`/`roll`, `level`)
  - `equipped`: map slot → itemInstanceId
  - `materials/coins`: số lượng
  - `unlockedSkills`: list skill ids (hoặc derive từ item level để khỏi lưu trùng)
- **Rule chống hack tối thiểu**:
  - Client **không được** tự set inventory/level/material.
  - Chỉ server/host **cập nhật profile**, client chỉ gửi request.
- **Definition of Done**:
  - Bạn viết được 1 ví dụ “profile JSON” mẫu (ngắn thôi) và liệt kê khi nào save/load.

##### Ngày 4: Chốt “authority + network contract” (RPC/NetworkVariables)
- **Bạn phải viết ra contract** (để code đúng ngay từ đầu):
  - `AttemptPickup(dropId)` (client → server) → server verify distance + ownership → add inventory.
  - `RequestUpgrade(itemInstanceId)` (client → server) → server check cost + level cap → apply.
  - `AttemptAttack(targetId/dir)` (client → server) → server validate cooldown/range → apply damage.
- **State sync**:
  - In-match: HP, position, animation… sync realtime.
  - Out-match: inventory/equipment/levels lưu Cloud Save.
- **Definition of Done**:
  - Bạn liệt kê được “những thứ tuyệt đối không để client quyết”: HP, damage, drop, upgrade, currency.

##### Ngày 5: Chuẩn hoá Prefab/Scene để “online-first”
- **Trong Unity**:
  - Player prefab: có `NetworkObject`, (nếu sync transform) `NetworkTransform`.
  - Enemy prefab: có `NetworkObject` (sau này AI chạy server), `NetworkTransform`.
  - Drop prefab: có `NetworkObject` (để server spawn/despawn).
  - NetworkPrefabs list: đăng ký đủ Player/Enemy/Drop.
- **Definition of Done**:
  - Bạn mở được scene và nhìn inspector thấy: prefab nào network được, prefab nào chưa.

##### Ngày 6: Viết checklist test (để tuần 2/3 test có hệ thống)
- **Test matrix tối thiểu** (ghi ra và tick):
  - 2 instance local: host+client → spawn OK → move OK.
  - 2 máy internet (sau tuần 3): lobby join OK.
  - Farm: 2 người nhặt cùng 1 drop → chỉ 1 người nhận.
  - Upgrade: spam request → server vẫn đúng cooldown/cost.
- **Definition of Done**:
  - Bạn có checklist “pass/fail” (10 dòng) và biết log nào cần in ra.

##### Ngày 7: Khoá scope + lập backlog tuần 2 (đầu việc cụ thể)
- **Khoá scope**: tuần 2 chỉ làm NGO baseline (spawn/ownership/movement sync), không đụng farm/upgrade sâu.
- **Backlog tuần 2**:
  - Owner-only input
  - Spawn system ổn định
  - Debug overlay
- **Definition of Done**:
  - Bạn có danh sách task tuần 2 (tối thiểu 8 task) và thứ tự làm.

### Tuần 2 (27/01–02/02): NGO baseline (spawn/ownership/movement sync) + test 2 instance
- **Đầu ra**: 2 người join (local), spawn đúng, **owner-only input**, movement sync ổn.
- **Việc làm**:
  - Fix/chuẩn hoá: spawner, ownership, camera theo local player.
  - Thêm debug HUD: clientId, role(host/client), ping, tickrate.

### Tuần 3 (03/02–09/02): Internet thật bằng UGS (Auth + Lobby + Relay) ✅ (M1)
- **Đầu ra**: 2 máy khác mạng join được (không cần mở port).
- **Việc làm**:
  - UGS Auth (anonymous) + profile name.
  - Lobby create/join/leave + ready.
  - Relay allocate/join code tự động đi kèm lobby data.

### Tuần 4 (10/02–16/02): PvP mode “1 ván” (1v1 + khung 2v2)
- **Đầu ra**: vào PvPScene, start countdown, end match, quay về lobby.
- **Việc làm**:
  - Match state server-authoritative (Waiting → Playing → RoundEnd).
  - Mode select: 1v1 / 2v2 (2v2 có team assignment tối thiểu).
  - Spawn positions theo team.

### Tuần 5 (17/02–23/02): Combat online chuẩn server ✅ (M2)
- **Đầu ra**: HP/damage/death không lệch; client không thể tự “hack damage”.
- **Việc làm**:
  - Client gửi **AttemptAttack** RPC; server validate range/cooldown → apply damage.
  - HP là NetworkVariable; death/respawn do server điều khiển.
  - Sync animation đánh/hit bằng NetworkAnimator hoặc state sync.

### Tuần 6 (24/02–02/03): Farm mode bản đầu (mục tiêu 6 người) + drop/pickup
- **Đầu ra**: 6 người farm chung ổn, có drop vật phẩm/material.
- **Việc làm**:
  - Spawn enemy server + respawn.
  - Drop item/material server-spawn.
  - Pickup: client AttemptPickup → server verify khoảng cách → add inventory.

### Tuần 7 (03/03–09/03): Enemy AI chạy server + tối ưu sync ✅ (M3)
- **Đầu ra**: AI không phụ thuộc “Player tag local”; 6 người vẫn mượt.
- **Việc làm**:
  - AI server chọn target (gần nhất / threat) từ danh sách player.
  - Enemy movement sync (NetworkTransform), attack/damage server.
  - Giảm bandwidth: send rate hợp lý, chỉ sync cái cần.

### Tuần 8 (10/03–16/03): Inventory/Equipment UI + stats pipeline
- **Đầu ra**: người chơi mở inventory, equip đồ, stats cập nhật đúng ở client.
- **Việc làm**:
  - Equip request: client → server validate → apply.
  - UI hiển thị slot/equipped + so sánh stats.

### Tuần 9 (17/03–23/03): Nâng cấp đồ lên cấp + mở/tăng sức mạnh skill
- **Đầu ra**: nâng cấp tiêu material/coin, itemLevel tăng, skill unlock/upgrade đúng.
- **Việc làm**:
  - Upgrade request: client → server trừ cost → tăng level → broadcast state.
  - Skill effects/cooldown do server quyết (tối thiểu).

### Tuần 10 (24/03–30/03): Cloud Save (lưu farm/đồ/nâng cấp) + nâng farm lên 12 ✅ (M4)
- **Đầu ra**: login là load đúng đồ; farm/upgrade xong thoát vào lại vẫn còn; farm 12 người chạy.
- **Việc làm**:
  - Save khi: pickup lớn, upgrade, end match, quit.
  - Giới hạn dữ liệu: chỉ lưu profile; state trong trận không cần lưu.
  - Stress test 12 người: spawn, pickup, combat, disconnect.

### Tuần 11 (31/03–06/04): “Farm → PvP” flow hoàn chỉnh + queue/lobby UX
- **Đầu ra**: người chơi farm xong quay về menu, tạo PvP lobby, đánh xong quay lại.
- **Việc làm**:
  - UI tách: Farm Lobby / PvP Lobby.
  - Prevent mismatch: chỉ vào PvP khi đủ người (1v1 cần 2; 2v2 cần 4).

### Tuần 12 (07/04–13/04): UI/UX polish + stability Internet ✅ (M5)
- **Đầu ra**: build chạy end-to-end, demo được “không cần mở Unity”.
- **Việc làm**:
  - HUD: HP, cooldown, buffs/gear power.
  - Endgame screen: K/D hoặc điểm + nút rematch/back.
  - Xử lý disconnect: host out → đóng trận đúng; client out → cleanup đúng.

### Tuần 13 (14/04–20/04): Cân bằng + tối ưu network/perf
- **Đầu ra**: mượt hơn, ít giật, ít desync.
- **Việc làm**:
  - Tuning NetworkTransform/NetworkAnimator.
  - Pooling projectile/FX nếu có.
  - Balance upgrade curve để farm có ý nghĩa nhưng PvP không “vỡ game”.

### Tuần 14 (21/04–27/04): Bug sprint + đóng gói + checklist nộp ✅ (M6)
- **Đầu ra**: build ổn định, checklist pass.
- **Việc làm**:
  - Soak test Internet 30–60 phút (12 farm + 1 trận PvP).
  - Fix crash/softlock/desync.
  - Chuẩn bị hướng dẫn chạy + release notes.

### 28/04–01/05: Release candidate + tài liệu + video demo
- **Đầu ra 01/05**:
  - Video demo: **Internet** → farm 12 (demo 3–4 người cũng được) → upgrade → PvP 1v1 hoặc 2v2 → end.
  - Tài liệu: kiến trúc NGO + UGS (Auth/Lobby/Relay/Cloud Save), sơ đồ data profile, mô tả hệ tiến hoá theo trang bị.

---

## 6) Checklist kỹ thuật (ngắn gọn nhưng bắt buộc)
- **UGS**: Auth + Lobby + Relay + Cloud Save đã hoạt động trong build.
- **Authority**:
  - Damage/HP/death/respawn: server/host quyết.
  - Drop/pickup/upgrade: server/host quyết.
- **Scalability**: farm 12 người không phụ thuộc `FindGameObjectWithTag("Player")` trong AI.
- **Testing**: luôn test tối thiểu 2 instance; mỗi tuần có 1 buổi test Internet.