# Kế hoạch online-first (tới 01/05) — tích hợp toàn bộ nội dung @detai.md

## 1) Scope & yêu cầu
- **Online-first**: Netcode for GameObjects + UGS (Auth, Lobby, Relay, Cloud Save). Không làm offline trước rồi port.
- **Farm 12 người**: solo được, farm chung tối đa 12; enemy/boss server-authoritative; drop/pickup server.
- **PvP 1v1/2v2**: tạo lobby → 1 ván → end → quay về menu/lobby.
- **Progression**: nhặt đồ/material → nâng cấp đồ/skill → lưu Cloud Save. Gene/Ngũ Hành ảnh hưởng stat/skill.
- **Gene System (từ detai.md)**: 5 hệ Kim/Mộc/Thủy/Hỏa/Thổ, tương khắc (1.5x / 0.75x), áp vào combat, skill, chỉ số.
- **Authority**: host/server quyết định HP/damage/drop/upgrade/currency; client chỉ gửi intent.

## 2) Luồng chơi tổng (end-to-end)
1) MainMenu: UGS Auth (anonymous) → hiển thị online status/profile.
2) Farm Lobby: tạo/join (max 12) → Relay join → vào FarmScene.
3) FarmScene: enemy/boss server spawn → drop item/material → pickup (server validate) → Upgrade UI (equip/upgrade/item skill unlock).
4) PvP Lobby: tạo lobby chọn 1v1/2v2 → PvPScene → countdown → combat → end → back to menu/lobby.
5) Cloud Save: load profile khi login; save sau pickup lớn/upgrade/end match/quit.

## 3) Mốc bàn giao (cứng để kịp 01/05)
- **M1 – 09/02**: Online join 2 người Internet (Auth + Lobby + Relay), di chuyển/anim sync ổn. (Hoàn thành tuần 2 baseline trước đó)
- **M2 – 23/02**: PvP “1 ván” hoàn chỉnh 1v1/2v2; combat/HP/damage server-authoritative.
- **M3 – 09/03**: Farm chạy 6 người; enemy server + drop/pickup server; Gene khung tương khắc áp vào damage.
- **M4 – 30/03**: Farm 12 người; inventory + nâng cấp đồ/skill; Cloud Save profile.
- **M5 – 13/04**: UI/UX end-to-end (menu → lobby → farm/upgrade → PvP → end); test Internet ổn định.
- **M6 – 27/04**: Bug sprint + tối ưu + đóng gói build + tài liệu.
- **01/05**: Release candidate (demo + video + hướng dẫn).

## 4) Kế hoạch theo tuần (online-first, bám detai.md)
### Tuần 2 (đã xong): NGO baseline local (spawn/ownership/movement sync)
- Đầu ra: 2 instance local spawn đúng, owner-only input, camera local, debug HUD.

### Tuần 3 (03/02–09/02) — M1: UGS Internet
- Auth anonymous + profile name.
- Lobby create/join/leave + ready + Relay allocate/join.
- Test 2 máy khác mạng: join lobby, vào scene, di chuyển/anim sync.
- Tài liệu làm anim sync: `animator_sync_guide.md` (tổng quan) và `Client/Assets/Scripts/ANIMATOR_SYNC_GUIDE.md` (shortcut trong thư mục Scripts).

### Tuần 4 (10/02–16/02): PvP “1 ván” (1v1 + khung 2v2)
- Match state server: Waiting → Playing → RoundEnd.
- Team assign (2v2), spawn points theo team.
- Countdown start, end screen (win/lose), back lobby/menu.

### Tuần 5 (17/02–23/02) — M2: Combat server-authoritative
- RPC: `AttemptAttack(targetId/dir)` client→server (validate range/cooldown).
- HP/damage/death/respawn do server quyết; animation sync (NetworkAnimator hoặc state sync).
- Gene tương khắc áp dụng vào damage multiplier (1.5x/0.75x/1x), hiển thị màu damage text (Vàng/Trắng/Xám).
- Nếu trigger bắt đầu gây lệch nhịp: chuyển attack/hit sang “state sync” theo hướng dẫn trong `animator_sync_guide.md`.

### Tuần 6 (24/02–02/03): Farm bản đầu (6 người) + drop/pickup
- Enemy/boss server spawn + respawn.
- Drop server-spawn; `AttemptPickup(dropId)` client→server validate khoảng cách/owner → add inventory.
- Spawn points farm, basic wave; log rõ để debug.

### Tuần 7 (03/03–09/03) — M3: AI server + tối ưu sync
- AI server chọn target (gần nhất/threat), không dùng `FindWithTag` client.
- Enemy movement/attack sync; giảm bandwidth (send rate hợp lý, chỉ sync cần thiết).
- Stress test 6 người.

### Tuần 8 (10/03–16/03): Inventory/Equipment + stats pipeline + Gene stats
- UI inventory/equip; equip request validate server.
- Stats calc: base + item + Gene (Ngũ Hành) bonus; show in UI.
- Gene effect hook sẵn cho combat (buff/DoT/slow/shield tuỳ hệ).

### Tuần 8.5 (song song Tuần 8–9): Kế hoạch Skill (thiết kế & nền tảng)
- Thiết kế khung skill: định nghĩa SkillDefinition (id, element, cooldown, cost, base dmg/heal, effect type: burn/poison/freeze/stun/shield).
- Binding Gene: mỗi skill gắn 1 hệ Ngũ Hành; áp multiplier tương khắc vào damage/heal; màu damage text theo hệ.
- Phân tầng mở khoá: skill1/skill2 mở theo item level/Gene tier (ghi rõ mốc trong dữ liệu).
- Network contract: `RequestCastSkill(skillId, targetId/dir)` client→server; server validate cooldown/range/resource → áp damage/effect.
- Visual: placeholder VFX/SFX tối thiểu; dùng NetworkObject/FX pooling nếu có.
- Definition of Done: có bảng skill tối thiểu (mỗi hệ ≥2 skill cơ bản), JSON/Scriptable template, RPC đã khai báo.

### Tuần 9 (17/03–23/03): Nâng cấp đồ/skill + Fusion/Gene unlock
- Upgrade request: server trừ material/coin, tăng level, broadcast.
- Skill unlock/upgrade theo item level/Gene tier; cập nhật cooldown/damage.
- Thiết kế Hybrid/Fusion placeholder (kim+hỏa…): dữ liệu sẵn, hiệu ứng đơn giản.

### Tuần 10 (24/03–30/03) — M4: Cloud Save + farm 12 người
- Cloud Save: load profile on login; save khi pickup lớn/upgrade/end match/quit.
- Giới hạn payload (chỉ profile, không lưu state trận).
- Stress test 12 người: spawn, pickup, combat, disconnect.

### Tuần 11 (31/03–06/04): Farm→PvP flow + lobby UX
- UI tách Farm Lobby / PvP Lobby; chặn vào PvP khi thiếu người (1v1 cần 2, 2v2 cần 4).
- Back-to-menu flow sau trận; giữ profile/session.

### Tuần 12 (07/04–13/04) — M5: UI/UX polish + stability
- HUD: HP, cooldown, buffs/Gene, gear power; endgame screen.
- Disconnect handling: host out đóng trận; client out cleanup.
- Build chạy end-to-end không cần Editor.

### Tuần 13 (14/04–20/04): Balance + tối ưu perf/network
- Tune NetworkTransform/Animator; pooling projectile/FX nếu có.
- Balance Gene multiplier/upgrade curve để farm có ý nghĩa nhưng PvP không vỡ.

### Tuần 14 (21/04–27/04) — M6: Bug sprint + đóng gói
- Soak test Internet 30–60 phút (12 farm + 1 PvP).
- Fix crash/softlock/desync; checklist release.

### 28/04–01/05: Release candidate + tài liệu + video
- Video: Internet → farm (≥3–4 người) → upgrade → PvP 1v1/2v2 → end.
- Tài liệu: kiến trúc NGO+UGS, data profile, Gene/Ngũ Hành áp dụng combat/progression.

## 5) Checklist kỹ thuật
- UGS: Auth + Lobby + Relay + Cloud Save hoạt động trong build.
- Authority: HP/damage/death/respawn, drop/pickup/upgrade/currency do server/host quyết.
- Scalability: AI/logic không phụ thuộc `FindGameObjectWithTag("Player")`; farm 12 người không choke.
- Testing: luôn test ≥2 instance; mỗi tuần 1 buổi test Internet; log rõ lỗi pickup/upgrade/combat.