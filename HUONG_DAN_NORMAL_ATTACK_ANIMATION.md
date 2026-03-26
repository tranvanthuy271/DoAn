# Hướng Dẫn Thêm Animation cho Skill Đánh Thường (NormalAttack)

## Tổng Quan

Hệ thống `NormalAttack` đã được tích hợp vào toàn bộ 12 player prefabs (6 He/ + 6 Fusion/).  
Animator controller đã có state `normal attack` và trigger `NormalAttack`.  
**Việc còn lại của bạn**: tạo file `.anim` chứa các sprite frames và gắn vào state.

---

## 1. Sơ Đồ Controller / Prefab

| Animator Controller | Loại | Dùng bởi Prefab |
|---|---|---|
| `Skill_Phong.controller` | Base | He/Phong, F/Phong |
| `Skill_Hoa.overrideController` | Override (base = Skill_Phong) | He/Hoa |
| `Skill_Kim.overrideController` | Override (base = Skill_Phong) | He/Kim, F/Kim |
| `Skill_Tho.overrideController` | Override (base = Skill_Phong) | He/Tho, F/Tho |
| `Skill_F_Hoa.controller` | Base | F/Hoa |
| `Skill_Thuy.controller` | Base | He/Thuy, F/Thuy |
| *(không có SkillEffect)* | — | He/Moc, F/Moc |

> **Override controller**: Hoa/Kim/Tho chỉ cần override clip `normal attack` trong Unity Editor.  
> Không cần sửa trigger/parameter gì thêm — kế thừa từ `Skill_Phong.controller`.

---

## 2. Tạo File Animation Clip (.anim)

### Bước 2.1 — Tạo clip mới trong Unity

1. Mở Unity Editor, vào thư mục `Assets/Animations/Skills/`
2. Right-click → **Create → Animation**
3. Đặt tên theo quy ước: `NormalAttack_Phong.anim`, `NormalAttack_Hoa.anim`, v.v.

### Bước 2.2 — Thêm sprite frames vào clip

1. Chọn file `.anim` vừa tạo → mở **Animation Window** (Ctrl+6)
2. Bật **Record mode** (nút tròn đỏ)
3. Kéo các sprite frames từ Project vào timeline
4. Đặt `Loop` = **false** (1 lần rồi dừng — phù hợp với đánh thường)
5. Chỉnh Sample Rate = **12** hoặc **24** FPS tùy sprite sheet của bạn

### Bước 2.3 — Đặt thời gian clip khớp với cooldown

- `NormalAttack` có `cooldown: 0.8s` (level 1) → tổng clip nên ~**0.5–0.6s**  
- Transition `normal attack → idle` được set `ExitTime: 0.95` → clip phải **có độ dài thực** (>=1 frame)

---

## 3. Gắn Clip vào Animator Controller

### Base Controllers (Skill_Phong, Skill_F_Hoa, Skill_Thuy)

1. Mở controller trong **Animator Window**
2. Double-click vào state `normal attack`
3. Trong Inspector → kéo file `.anim` vào trường **Motion**

### Override Controllers (Hoa, Kim, Tho)

1. Chọn file `Skill_Hoa.overrideController` (hoặc Kim/Tho)
2. Trong Inspector → bảng **Original** / **Override**
3. Tìm dòng **"normal attack"** (clip từ Skill_Phong)
4. Kéo clip `.anim` của hệ/nhân vật tương ứng vào cột **Override**

---

## 4. Nhân Vật Moc và F_Moc (Không Có SkillEffect)

`He/Moc` và `F/Moc` hiện tại **không có** child GameObject `SkillEffect`.  
Trường `disablePlayerSkillEffectAnimation` được set = `1` → animation SkillEffect bị tắt.

### Nếu muốn thêm SkillEffect cho Moc:

1. Mở prefab `He/Moc.prefab` hoặc `Fusion/F_Moc.prefab` trong Unity
2. Thêm child GameObject tên **SkillEffect**
3. Gắn component `Animator` → assign controller mới cho Moc
4. Gắn component `SpriteRenderer`
5. Trong component `PlayerSkillManager` → kéo GO vào trường `defaultSkillEffectObject`
6. Trong `SkillData` của NORMAL_ATTACK → đổi `disablePlayerSkillEffectAnimation` = `0`, điền `animationTriggerName` = `NormalAttack`

---

## 5. Kiểm Tra Hoạt Động

### Trong Unity Play Mode:

1. Bật prefab player (He/Phong chẳng hạn)
2. Nhấn **Z** hoặc **Click chuột trái**
3. Expected flow:
   - Cooldown bar trên HUD cập nhật
   - Animation player character phát (nếu đã có clip cho PlayerAnimator)
   - `SkillEffect` child GO phát animation state `normal attack`
   - PlayerCombat hitbox được trigger → damage áp dụng lên enemy trong range

### Debug Logs:

```
[PlayerSkillManager] Detected PlayerCombat component (NormalAttack).
[PlayerCombat] Attack triggered! Damage: 10
```

---

## 6. Tham Số Skill trong Prefab

```yaml
skillName: "Đánh Thường"
skillCode: NORMAL_ATTACK
skillType: 15               # SkillType.NormalAttack
activationKey: 122          # Z key (cộng LMB xử lý riêng trong HandleSkillInput)
cooldown: 0.8
animationTriggerName: NormalAttack
disablePlayerSkillEffectAnimation: 0   # (1 với Moc/F_Moc)
iconId: icon_normal_attack
```

---

## 7. Cấu Trúc Code Liên Quan

| File | Vai Trò |
|---|---|
| `Scripts/Player/Skills/SkillData.cs` | Enum `SkillType.NormalAttack = 15` |
| `Scripts/Player/Combat/PlayerCombat.cs` | `TriggerAttack(int dmg)`, `CanAttackNow` |
| `Scripts/Player/Combat/PlayerSkillManager.cs` | `UseNormalAttackLocal()`, `UseNormalAttackServerRpc()`, xử lý LMB/Z |
| `Animations/Skills/Skill_Phong.controller` | State machine chính — có state `normal attack` + trigger `NormalAttack` |
| `Animations/Skills/Skill_F_Hoa.controller` | State machine cho F/Hoa |
| `Animations/Skills/Skill_Thuy.controller` | State machine cho He/Thuy + F/Thuy |
| `GameServerApi/migration_normal_attack.sql` | Migrate DB: thêm `NORMAL_ATTACK` vào `skill_template` |

---

## 8. DB Migration

Chạy file SQL sau khi deploy:

```
GameServerApi/migration_normal_attack.sql
```

Hoặc trong `skill_template.sql` — đã có skill_id=40 (`NORMAL_ATTACK`).

5 level damage: **10 / 18 / 30 / 48 / 72** — cooldown giảm từ **0.8s → 0.6s** — MP cost = **0**.
