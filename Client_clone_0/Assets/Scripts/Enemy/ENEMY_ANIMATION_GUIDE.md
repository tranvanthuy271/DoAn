# 🧠 Hướng Dẫn Animation & AI Cho Enemy/Mod (Run + Attack)

Yêu cầu mới: Enemy chỉ có **2 animation** `Run` và `Attack`. Enemy luôn chạy qua lại (loop run). Khi thấy player, nó chạy lại gần; khi vào phạm vi đánh thì phát `Attack`. Khi chết thì **destroy object** ngay (không cần anim death).

---

## 1. Mục tiêu tổng

- Enemy luôn chạy (run loop) khi không có player hoặc đang đuổi.
- Khi player ở trong detection range → chạy về phía player.
- Khi player trong attack range → chuyển anim `Attack`, gây damage, sau đó quay lại run/đuổi.
- Khi chết → destroy object ngay (skip anim death).

---

## 2. Chuẩn bị asset

1. Sprite sheet: `Assets/Art/Sprites/Enemy/...`.
2. Animation clip (chỉ 2):
   - `Enemy_Run.anim` (loop).
   - `Enemy_Attack.anim` (non-loop, có event đánh).
3. Animator Controller: `Assets/Animations/Enemy/Enemy.controller`.
4. Prefab: `Assets/Prefabs/Enemy/EnemyBasic.prefab`.

> ⚠️ Sprite import: Sprite Mode = Multiple, PPU thống nhất, Filter = Point (pixel-art).

---

## 3. Animator Controller (2 state)

1. Parameters:
   - `bool isAttacking`
2. States:
   - `Run` (default) → clip `Enemy_Run` (loop).
   - `Attack` → clip `Enemy_Attack` (non-loop).
3. Transitions:
   - `Run → Attack`: condition `isAttacking == true`, `Has Exit Time = false`.
   - `Attack → Run`: `Has Exit Time = true`, `Fixed Duration` on, no condition (anim tự về Run sau khi hết clip).
4. Animation Event (QUAN TRỌNG):
   
   **Cách thêm Animation Event trong Unity:**
   
   **Bước 1: Mở Animation Window**
   - Menu: `Window` → `Animation` → `Animation` (hoặc `Ctrl+6`)
   - Hoặc chọn GameObject có Animator → tab `Animation` ở Inspector
   
   **Bước 2: Chọn Animation Clip**
   - Ở Animation window, dropdown phía trên chọn `Enemy_Attack`
   - Timeline sẽ hiển thị các frame của animation Attack
   
   **Bước 3: Chọn frame muốn thêm event**
   - Kéo timeline đến frame muốn gây damage (ví dụ: giữa animation khi tay đánh ra)
   - Hoặc click vào timeline tại vị trí đó
   - Ví dụ: nếu animation có 10 frame, chọn frame 5-6 (khoảng giữa)
   
   **Bước 4: Thêm Event**
   - Click nút **"Add Event"** (dấu `+` nhỏ ở timeline) tại frame đã chọn
   - Hoặc click chuột phải trên timeline → `Add Animation Event`
   - Một marker màu trắng sẽ xuất hiện trên timeline
   
   **Bước 5: Chọn Function Name**
   - Click vào marker event vừa tạo
   - Ở Inspector bên phải, có dropdown `Function`
   - Chọn function `OnAttackHit` (phải có trong script `EnemyAI.cs`)
   - Có thể thêm parameter nếu cần (ví dụ: int, float, string)
   
   **Bước 6: Thêm event thứ 2 (kết thúc attack)**
   - Chọn frame cuối của animation `Enemy_Attack`
   - Thêm event tương tự, chọn function `OnAttackFinished`
   
   **Kết quả:**
   - Timeline sẽ có 2 marker trắng:
     - Marker 1: `OnAttackHit()` ở frame giữa (gây damage)
     - Marker 2: `OnAttackFinished()` ở frame cuối (kết thúc attack)
   
   > ⚠️ **Lưu ý:** 
   > - Function name phải KHỚP chính xác với tên function trong script (phân biệt hoa thường)
   > - Function phải là `public` trong script `EnemyAI.cs`
   > - Nếu không thấy function trong dropdown → check lại script đã attach chưa
   
   **Minh họa Timeline:**
   ```
   Enemy_Attack Timeline:
   |----|----|----|----|----|----|----|----|----|----|
   0    1    2    3    4    5    6    7    8    9    10
                    ↑                    ↑
              OnAttackHit()      OnAttackFinished()
              (frame 5)           (frame 10)
   ```
   
   **Cách test nhanh:**
   - Play game → enemy attack → check Console xem có log từ `OnAttackHit()` không
   - Nếu không chạy → check lại function name và public modifier

---

## 4. Component cần có trên prefab

- `SpriteRenderer`
- `Animator` trỏ `Enemy.controller`
- `Rigidbody2D` (Dynamic, Freeze Z, Gravity Scale = 0 nếu không cần rơi)
- `CapsuleCollider2D`/`BoxCollider2D` body
- `CircleCollider2D`/`BoxCollider2D` làm **attack hitbox** (isTrigger, disable mặc định)
- `EnemyHealth`
- Script `EnemyAI.cs`

---

## 5. Script EnemyAI (run + attack)

Tạo/ghi đè `Assets/Scripts/Enemy/EnemyAI.cs` theo mẫu dưới:

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;
    public Transform leftPoint;   // điểm biên trái
    public Transform rightPoint;  // điểm biên phải

    [Header("Combat")]
    public float detectionRange = 6f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public int damage = 2;
    public Collider2D hitbox; // isTrigger, disable mặc định

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private EnemyHealth health;
    private bool facingRight = true;
    private float lastAttackTime;

    private enum State { Run, Attack, Dead }
    private State state = State.Run;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        if (hitbox != null) hitbox.enabled = false;

        health.OnDeath.AddListener(OnDeath);
    }

    private void Update()
    {
        if (state == State.Dead) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (state == State.Attack)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            return;
        }

        // Luôn chạy (loop run). Nếu có player gần thì chạy về phía player.
        if (dist <= detectionRange)
        {
            RunTowards(player.position.x);
        }
        else
        {
            PatrolLoop();
        }

        // Nếu đã đủ gần để đánh → chuyển Attack
        if (dist <= attackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            state = State.Attack;
            lastAttackTime = Time.time;
            animator.SetBool("isAttacking", true);
            rb.velocity = Vector2.zero;
        }
    }

    private void PatrolLoop()
    {
        if (leftPoint == null || rightPoint == null) return;
        float targetX = facingRight ? rightPoint.position.x : leftPoint.position.x;
        RunTowards(targetX);

        if (Mathf.Abs(transform.position.x - targetX) < 0.1f)
        {
            facingRight = !facingRight;
            Flip();
        }
    }

    private void RunTowards(float targetX)
    {
        float dir = Mathf.Sign(targetX - transform.position.x);
        rb.velocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
            Flip();
    }

    // Animation Event
    public void OnAttackHit()
    {
        if (hitbox != null) hitbox.enabled = true;
        // Hoặc tại đây: check khoảng cách và gọi PlayerHealth.TakeDamage(damage)
    }

    // Animation Event
    public void OnAttackFinished()
    {
        if (hitbox != null) hitbox.enabled = false;
        animator.SetBool("isAttacking", false);
        state = State.Run;
    }

    private void OnDeath()
    {
        state = State.Dead;
        Destroy(gameObject); // xóa hẳn object
    }

    private void Flip()
    {
        facingRight = !facingRight;
        var scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}
```

> 📝 Chỉnh các tham số (range, speed, damage) theo gameplay thực tế.

---

## 6. Logic attack & hitbox

1. Tạo `Hitbox` con, `BoxCollider2D`/`CircleCollider2D`, `isTrigger = true`, disable mặc định.
2. Animation `Attack` thêm 2 event:
   - Bật hitbox: gọi `OnAttackHit()` (enable collider hoặc trực tiếp damage player nếu muốn).
   - Kết thúc: gọi `OnAttackFinished()` (tắt hitbox, về run).
3. Trong script, khi hitbox chạm player → gọi `PlayerHealth.TakeDamage(damage)`.

---

## 7. Kết nối với hệ thống hiện có

- `EnemyHealth`: OnDeath → destroy ngay (đã xử lý trong script).
- `PlayerCombat`: đánh trúng enemy → gọi `EnemyHealth.TakeDamage`.
- UI HP quái (nếu cần): gán `HealthBar` world-space lên prefab.

---

## 8. Checklist test nhanh

| Hạng mục | OK? |
|----------|-----|
| Run anim loop khi không có player | ☐ |
| Patrol qua lại, tự flip sprite | ☐ |
| Thấy player trong `detectionRange` → chạy tới | ☐ |
| Khi vào `attackRange` → phát anim Attack | ☐ |
| Hitbox bật đúng frame, player nhận damage | ☐ |
| Attack xong về Run, tiếp tục đuổi | ☐ |
| Enemy chết → destroy object ngay | ☐ |

---

## 9. Next steps đề xuất

1. Tuning tham số move/attack theo level.
2. Thêm knockback/flash khi bị đánh (nếu muốn feedback).
3. Bổ sung VFX/SFX cho Attack để rõ hiệu ứng trúng đòn.

Làm theo file này bạn sẽ có enemy chỉ-run-và-attack, đuổi player và destroy khi chết. Cần chi tiết thêm đoạn hitbox hoặc gọi damage, cứ ping! 💪


