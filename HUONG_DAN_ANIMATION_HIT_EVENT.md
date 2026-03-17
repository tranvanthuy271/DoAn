# Hướng Dẫn: Animation "Chờ Trúng Mới Phát Nổ" cho Projectile Skills

## Ý tưởng tổng quan

Bạn muốn animation projectile hoạt động như sau:

```
[Bay đi - lặp] ──── chạm mục tiêu ────▶ [Phát nổ - chạy 1 lần]
  frame 0→N                                frame N+1→cuối
  (loop nếu không trúng)                   (chỉ chạy khi hit)
```

Có **2 cách** thực hiện — cả hai đều dùng trong project này tùy trường hợp.

---

## Cách 1 (KHUYẾN NGHỊ): Tách thành 2 clip riêng biệt

Đây là cách dự án này đã dùng cho `EarthBlinkStrike` (Skill3.controller). Nó sạch hơn và không cần code phức tạp.

### Cấu trúc Animator Controller

```
[Idle/Fly clip] ──OnHit trigger──▶ [Explode clip]
  (default state, loop)               (play once)
        ▲
   AnyState transition
```

### Bước 1: Tạo 2 clip animation

Trong Unity Editor:  
1. Chọn projectile GameObject trong Hierarchy  
2. Mở **Window → Animation → Animation**  
3. Tạo clip **`fly.anim`** — chứa sprite bay, set **Loop Time = ✅**  
4. Tạo clip **`explode.anim`** — chứa sprite phát nổ, set **Loop Time = ❌**

### Bước 2: Thiết lập Animator Controller

1. Mở **Window → Animation → Animator**  
2. Trong Animator window:
   - Click **Parameters** → dấu `+` → chọn **Trigger** → đặt tên `OnHit`
   - Click phải vào vùng trống → **Create State → Empty** → đổi tên thành `Fly`
   - Click phải → **Create State → Empty** → đổi tên thành `Explode`
   - Gán Motion: `Fly` → `fly.anim`, `Explode` → `explode.anim`
   - Click phải vào `Fly` → **Set as Layer Default State** (màu cam)
3. Tạo transition từ `Any State` → `Explode`:
   - Click phải `Any State` → Make Transition → kéo đến `Explode`
   - Trong Inspector của transition đó:
     - **Has Exit Time**: ❌ bỏ tick
     - **Transition Duration**: `0`
     - **Conditions**: thêm `OnHit`

### Bước 3: Code gọi trigger khi va chạm

Trong script xử lý va chạm của projectile, thêm 1 dòng:

```csharp
// Trong OnTriggerEnter2D khi phát hiện trúng mục tiêu:
hasHit = true;
GetComponent<Animator>()?.SetTrigger("OnHit");  // ← dòng này
// ... xử lý damage
```

> **Project này đã làm điều này rồi** trong `DotDamage.cs`. Chỉ cần fill sprite vào `Skill3_3.anim` trong Unity Editor.

---

## Cách 2: Dùng Animation Event trong 1 clip duy nhất

Dùng khi bạn chỉ có **1 clip** chứa toàn bộ sprite (bay + nổ) và muốn "tạm dừng" tại điểm chia.

### Nguyên lý

```
clip: [frame0] [frame1] ... [frameN] [EVENT] [frameN+1] ... [frameEnd]
                ↑ bay, loop nếu chưa hit ↑         ↑ explosion ↑
```

- Khi animation chạy đến frame có Event, nó gọi một method trong C#.
- Method đó kiểm tra `hasHit`:
  - Nếu **chưa trúng** → tua lại về `frame 0` (giả lập loop)
  - Nếu **đã trúng** → để animation tiếp tục chạy phần explosion

### Bước 1: Tạo script `ProjectileAnimController.cs`

Tạo file mới: `Assets/Scripts/Player/Skills/ProjectileAnimController.cs`

```csharp
using UnityEngine;

/// <summary>
/// Gắn vào projectile cùng với Animator.
/// Được gọi bởi Animation Event tại điểm "checkpoint".
/// Nếu chưa trúng mục tiêu → tua animation về đầu (giả lập loop).
/// Nếu đã trúng → để animation tiếp tục chạy phần explosion.
/// </summary>
[RequireComponent(typeof(Animator))]
public class ProjectileAnimController : MonoBehaviour
{
    // Script damage (DotDamage hoặc FireballDamage) trên cùng GameObject
    // sẽ set cờ này = true khi va chạm xảy ra
    [HideInInspector] public bool hasHit = false;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Được gọi bởi Animation Event tại frame checkpoint trong clip.
    /// </summary>
    public void OnAnimationCheckpoint()
    {
        if (!hasHit)
        {
            // Chưa trúng → tua về frame 0
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
        }
        // Đã trúng → không làm gì, animation tự chạy tiếp
    }
}
```

### Bước 2: Thêm Animation Event vào clip trong Unity Editor

1. Chọn projectile GameObject trong Hierarchy (phải đang open scene có nó)
2. Mở **Window → Animation → Animation**
3. Chọn clip cần chỉnh (ví dụ `fly_explode.anim`)
4. Kéo thanh scrub đến **frame đầu tiên của phần explosion** (ví dụ frame 8)
5. Click nút **biểu tượng dấu cộng ở thanh Event** (phía trên timeline, hình ngọn cờ nhỏ):

   ```
   ┌────────────────────────────────────────────────────┐
   │ 0   1   2   3   4   5   6   7  [8]  9   10  11    │
   │ ●   ●   ●   ●   ●   ●   ●   ●   ▼  ●   ●   ●    │ ← sprite frames
   │                                  ⚑                │ ← event được thêm tại frame 8
   └────────────────────────────────────────────────────┘
   ```

6. Trong **Inspector** của event vừa thêm:
   - **Function**: gõ `OnAnimationCheckpoint`
   - Để trống các trường Float, Int, String, Object

### Bước 3: Kết nối script với damage script

Trong `DotDamage.cs` (hoặc `FireballDamage.cs`), sửa phần hit handler:

```csharp
// Thêm field ở đầu class:
private ProjectileAnimController animCtrl;

private void Awake()
{
    animCtrl = GetComponent<ProjectileAnimController>();
}

// Trong OnTriggerEnter2D khi phát hiện hit:
hasHit = true;
if (animCtrl != null) animCtrl.hasHit = true;  // ← báo cho anim controller
```

### Bước 4: Gắn component lên prefab

Trong Unity Editor:
1. Chọn prefab projectile
2. **Add Component** → tìm `ProjectileAnimController`

---

## So sánh 2 cách

| | Cách 1 (2 clip) | Cách 2 (1 clip + Event) |
|---|---|---|
| **Độ phức tạp** | Thấp | Cao hơn |
| **Số clip cần tạo** | 2 clip | 1 clip |
| **Code cần viết** | Không (đã có) | Thêm 1 script mới |
| **Linh hoạt** | Cao (dễ chỉnh từng phần) | Trung bình |
| **Dùng khi** | Có clip riêng cho fly và explode | Chỉ có 1 clip ghép |
| **Dự án này** | ✅ Đang dùng (Skill3.controller) | Dùng nếu cần |

---

## Áp dụng vào các skill hiện tại của dự án

### EarthBlinkStrike (Skill 3 - Hệ Thổ)
- **Đang dùng Cách 1** ✅
- Controller: `Animations/Skills/Tho/Skill3.controller`
- State: `Idle` (default, không motion) → `OnHit` trigger → `EarthBlinkStrike` (Skill3_3.anim)
- **Việc còn lại**: Mở `Skill3_3.anim` trong Unity Editor, thêm sprite explosion vào clip

### EarthBoomerang (Skill 2 - Hệ Thổ)
- **Auto-play** (bay + xoay cả quá trình) — không cần "chờ hit"
- Controller: `Animations/Skills/Tho/Skill2.controller`
- Dùng `Skill3_2_prefabs.anim` tự động play khi spawn

### Hỏa skills (FIREBOLT, FIREBURST, FIRE_RAIN)
- **Auto-play** — animation chạy ngay khi spawn, tự destroy sau khi xong
- Nếu muốn thêm explosion riêng khi hit, dùng Cách 1:
  1. Split clip thành `firebolt_fly.anim` + `firebolt_explode.anim`
  2. Thêm Trigger `OnHit` vào `FireballDamage.cs`
  3. Sửa controller tương tự Skill3.controller

---

## Ví dụ: Làm cho FIREBOLT có explosion khi hit

### 1. Sửa `FireballDamage.cs` — thêm SetTrigger

```csharp
private void OnTriggerEnter2D(Collider2D collision)
{
    if (hasHit) return;

    if (collision.CompareTag("Enemy"))
    {
        // ... get components ...
        if (enemyHealth != null || networkEnemyHealth != null)
        {
            hasHit = true;
            GetComponent<Animator>()?.SetTrigger("OnHit");  // ← thêm dòng này
            // ... deal damage ...
            if (destroyOnHit)
                Destroy(gameObject, 0.5f);  // delay để animation kịp chạy
        }
    }
}
```

### 2. Sửa controller `skill1.controller` (FIREBOLT)

Thay cấu trúc hiện tại (chỉ có 1 state auto-play) thành:
- Default state: `Fly` (firebolt_fly.anim, loop)
- AnyState → `Explode` (firebolt_explode.anim) khi trigger `OnHit`

### 3. Split animation clip trong Unity Editor

Cách dễ nhất: Trong Animation window, tạo 2 clip mới từ sprite sheet:
- `firebolt_fly.anim`: kéo các sprite "đang bay" vào
- `firebolt_explode.anim`: kéo các sprite "phát nổ" vào

---

## Lưu ý quan trọng

### `destroyOnHit` phải delay đủ cho animation
```csharp
// SAI — destroy ngay, animation không kịp chạy
if (destroyOnHit) Destroy(gameObject);

// ĐÚNG — delay bằng thời lượng explode clip
if (destroyOnHit) Destroy(gameObject, 0.5f); // 0.5f = độ dài explode.anim
```

### Animation Event chỉ hoạt động khi có component trên CÙNG GameObject
Method được gọi bởi Animation Event **phải nằm trong MonoBehaviour trên cùng GameObject** với Animator. Không thể gọi method trên component con/cha.

### Sprite phải được set trước khi mở Animation window
Kéo sprites vào clip phải làm trong Animation window khi GameObject đang được chọn trong scene (không phải trong Project tab).
