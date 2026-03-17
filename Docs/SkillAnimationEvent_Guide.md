# Hướng Dẫn Setup Animation Event Cho Skill Effects

## Mục tiêu

Mỗi animation skill gồm **2 pha trong 1 clip duy nhất**:
- **Pha Bay** (frames đầu): projectile đang di chuyển → loop lại nếu chưa trúng
- **Pha Nổ** (frames cuối): hiệu ứng nổ khi trúng mục tiêu

Tại frame ranh giới giữa 2 pha, thêm **Animation Event** gọi `OnAnimationCheckpoint()`:
- Nếu **chưa trúng** → animation tua về frame 0, tiếp tục loop pha bay
- Nếu **đã trúng** (MarkHit đã gọi) → animation tiếp tục chạy sang pha nổ

---

## Script Có Sẵn

### ProjectileAnimController.cs

File này đã có, đặt tại: `Assets/Scripts/Player/Skills/ProjectileAnimController.cs`

```csharp
[RequireComponent(typeof(Animator))]
public class ProjectileAnimController : MonoBehaviour
{
    private bool hasHit = false;
    private Animator animator;

    private void Awake() => animator = GetComponent<Animator>();

    // Gọi từ DotDamage / FireballDamage khi trúng mục tiêu
    public void MarkHit() => hasHit = true;

    // Được gọi bởi Animation Event tại frame checkpoint
    public void OnAnimationCheckpoint()
    {
        if (!hasHit)
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
        // Nếu hasHit = true → không làm gì, animation chạy tiếp sang pha nổ
    }
}
```

---

## Bước 1: Cấu Trúc Animation Clip

### Cách bố trí frames trong 1 clip

```
Frame 0                    Frame 15        Frame 30 (kết thúc)
|---- Pha Bay - Loop ------|-- Pha Nổ ----|
                           ^
                   Animation Event ở đây
                   gọi OnAnimationCheckpoint()
```

**Ví dụ thực tế với skill 24fps:**
| Frame | Nội dung |
|-------|----------|
| 0–11  | Sprite projectile đang bay |
| 12    | ⚡ **Animation Event: `OnAnimationCheckpoint`** |
| 12–23 | Sprite hiệu ứng nổ / explosion |

> **Lưu ý:** Frame 12 vừa là frame cuối pha bay, vừa là frame đầu pha nổ.

---

## Bước 2: Thêm Animation Event Trong Unity Editor

### 2.1. Mở Animation Clip

1. Chọn file `.anim` trong **Project window** (ví dụ: `Assets/Animations/Skills/Hoa/skill 1_1.anim`)
2. Mở **Animation window**: `Window → Animation → Animation`
3. **Không** chọn GameObject trên Scene, chỉ chọn file `.anim` trong Project

### 2.2. Thêm Event

1. Trong Animation window, kéo thanh timeline đến **frame bắt đầu pha nổ** (ví dụ frame 12)
2. Click icon **Add Event** (biểu tượng dấu `+` trên timeline) hoặc chuột phải → **Add Animation Event**
3. Trong **Inspector** của event vừa tạo:
   - **Function:** `OnAnimationCheckpoint`
   - Các trường Float/Int/String/Object: để trống

![Cách thêm Animation Event](https://docs.unity3d.com/uploads/Main/AnimationEditorEventInspector.png)

> **Quan trọng:** Animation Event gọi function trên **tất cả component** của **cùng GameObject**. Vì vậy `ProjectileAnimController` phải ở **cùng GameObject** với `Animator`.

---

## Bước 3: Cài Đặt Animator Controller

### 3.1. Cấu hình State trong Animator

```
[Entry] → [Fly_Explode_State]
             ↓
         Loop: KHÔNG (Unchecked)
         Motion: skill_1_1.anim (clip chứa cả 2 pha)
```

> **Tại sao không loop?** Vì `ProjectileAnimController` tự xử lý việc loop pha bay bằng cách gọi `animator.Play(..., 0, 0f)` để tua về đầu clip.

### 3.2. Không cần Transition

Không cần tạo transition hay parameter vì logic hoàn toàn chạy qua Animation Event.

---

## Bước 4: Setup Prefab Projectile

### Cấu trúc Component trên Projectile GameObject:

```
ProjectilePrefab
├── Animator (gán Animator Controller)
├── ProjectileAnimController    ← Script xử lý event
├── DotDamage / FireballDamage  ← Script gọi MarkHit()
├── ProjectileMovement          ← Script di chuyển
├── Rigidbody2D
└── Collider2D (Is Trigger = true)
```

---

## Bước 5: Kết Nối DotDamage / FireballDamage

### DotDamage.cs (đã có sẵn logic MarkHit)

File này **đã đúng**, khi trúng enemy/player:
```csharp
animCtrl?.MarkHit(); // Gọi trong OnTriggerEnter2D
```

**Không cần sửa gì thêm.**

---

### FireballDamage.cs (cần cập nhật)

File gốc dùng `Destroy(gameObject)` ngay khi trúng → animation nổ chưa chạy kịp.

**Cần sửa để hỗ trợ explosion animation:**

```csharp
[RequireComponent(typeof(Collider2D))]
public class FireballDamage : MonoBehaviour
{
    [SerializeField] private int damage = 5;
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private bool destroyOnGround = true;
    [SerializeField] private float destroyDelay = 0.5f; // Thời gian chờ animation nổ

    private bool hasHit = false;
    private int attackBonusPercent = 0;
    private ProjectileAnimController animCtrl; // Thêm dòng này

    private void Awake()
    {
        animCtrl = GetComponent<ProjectileAnimController>(); // Thêm dòng này
    }

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        int finalDamage = damage + damage * attackBonusPercent / 100;

        if (collision.CompareTag("Enemy"))
        {
            EnemyHealth eh = collision.GetComponent<EnemyHealth>();
            NetworkEnemyHealth neh = collision.GetComponent<NetworkEnemyHealth>();

            if (eh != null)       { eh.TakeDamage(finalDamage); hasHit = true; }
            else if (neh != null) { neh.TakeDamage(finalDamage); hasHit = true; }

            if (hasHit)
            {
                animCtrl?.MarkHit(); // Thêm dòng này
                if (destroyOnHit)
                    Destroy(gameObject, destroyDelay); // Đổi từ Destroy(gameObject) thành có delay
            }
        }
        else if (destroyOnGround && (collision.CompareTag("Ground") || collision.CompareTag("Wall")))
        {
            Destroy(gameObject);
        }
    }

    public void SetAttackBonus(int bonusPercent) => attackBonusPercent = bonusPercent;
    public void SetDamage(int newDamage) => damage = newDamage;
}
```

**Thay đổi chính:**
1. Thêm field `destroyDelay` (mặc định 0.5s để animation nổ kịp chạy)
2. Thêm `animCtrl` để gọi `MarkHit()`
3. `Destroy(gameObject, destroyDelay)` thay vì `Destroy(gameObject)`

---

## Bước 6: Checklist Kiểm Tra Từng Skill

Dùng bảng này để kiểm tra từng skill prefab:

| Skill | Prefab | `ProjectileAnimController` | `MarkHit()` gọi đúng | Animation Event | Delay Destroy |
|-------|--------|---------------------------|---------------------|-----------------|---------------|
| Hoa Skill 1 | `skill 1_1.anim` | ☐ | ☐ | ☐ | ☐ |
| Hoa Skill 2 | `skill 1_2.anim` | ☐ | ☐ | ☐ | ☐ |
| Hoa Skill 3 | `skill 1_3.anim` | ☐ | ☐ | ☐ | ☐ |
| Phong Skill 1 | `skill 1.anim` | ☐ | ☐ | ☐ | ☐ |
| Phong Skill 2 | `skill 2.anim` | ☐ | ☐ | ☐ | ☐ |
| Phong Skill 3 | `skill 3.anim` | ☐ | ☐ | ☐ | ☐ |
| Phong Skill 4 | `skill 4.anim` | ☐ | ☐ | ☐ | ☐ |
| Loi Skill 1 | `skill 2_1.anim` | ☐ | ☐ | ☐ | ☐ |
| Loi Skill 2 | `skill 2_2.anim` | ☐ | ☐ | ☐ | ☐ |
| Loi Skill 3 | `skill 2_3.anim` | ☐ | ☐ | ☐ | ☐ |
| Tho Skill 3 | `Skill3_1.anim` | ☐ | ☐ | ☐ | ☐ |
| Thuy Skill 4_1 | `skill 4_1.anim` | ☐ | ☐ | ☐ | ☐ |

---

## Bước 7: Xử Lý Trường Hợp Đặc Biệt

### Skill Effect trên Player (không phải Projectile)

Nếu `playerSkillEffectObject` là hiệu ứng attach vào player (không có hit detection), bạn cần kích hoạt "pha nổ" theo cách khác, ví dụ:

```csharp
// Trong PlayerSkillManager khi skill trúng
skillEffectAnimators[skill.skillName].SetTrigger("OnHit");
```

Hoặc dùng 2 clip riêng biệt với Animator Controller có transition.

### Skill Area-of-Effect (nổ ngay tại chỗ)

Nếu skill luôn nổ tại vị trí nhất định (không cần hit detection), thì:
- Chỉ cần 1 clip, không cần `OnAnimationCheckpoint`
- Gọi `Destroy(gameObject, clipLength)` sau khi spawn

### Boomerang (EarthBoomerangSkill)

Skill này có logic riêng trong `EarthBoomerangProjectile.cs` và `EarthBoomerangSkill.cs`. Nếu muốn thêm explosion:
1. Thêm `ProjectileAnimController` vào boomerang prefab
2. Gọi `MarkHit()` trong `EarthBoomerangProjectile.OnTriggerEnter2D()`
3. Thêm Animation Event vào clip boomerang

---

## Tóm Tắt Luồng Hoạt Động

```
Projectile Spawn
    │
    ▼
Animation: Frame 0 → [Pha Bay loop]
    │
    ▼ (mỗi lần qua frame checkpoint)
OnAnimationCheckpoint() được gọi
    │
    ├── hasHit = false → Play(state, 0, 0f) → Tua về frame 0
    │                                          Tiếp tục bay
    │
    └── hasHit = true  → Không làm gì
                         Animation tiếp tục: [Pha Nổ]
                             │
                             ▼
                     Destroy(gameObject, delay)
```

---

## Lỗi Thường Gặp

| Lỗi | Nguyên nhân | Cách sửa |
|-----|-------------|----------|
| Animation Event không được gọi | Function name sai hoặc script không ở cùng GameObject | Kiểm tra lại tên function và vị trí script |
| Animation bay mãi không nổ | `MarkHit()` chưa được gọi | Thêm `animCtrl?.MarkHit()` vào OnTriggerEnter2D |
| Nổ ngay lập tức khi spawn | Event ở frame 0 hoặc clip không có pha bay | Đẩy event về frame ranh giới đúng |
| Projectile biến mất trước khi nổ | `Destroy(gameObject)` không có delay | Đổi thành `Destroy(gameObject, destroyDelay)` |
| Nổ trên client này nhưng không thấy trên client khác | Animator không sync qua network | Dùng `NetworkAnimator` nếu cần sync, hoặc spawn explosion prefab riêng qua ServerRpc |
