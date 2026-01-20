using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform leftPoint;   // điểm biên trái
    public Transform rightPoint;  // điểm biên phải

    [Header("Combat")]
    public float detectionRange = 1f;
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
    private bool autoPatrolPointsCreated = false;

    private enum State { Run, Attack, Dead }
    private State state = State.Run;

    private void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();

        // Đồng bộ hướng nhìn ban đầu với sprite
        ApplyFacing();

        // Tự tạo điểm patrol nếu chưa gán trong Inspector
        if (leftPoint == null || rightPoint == null)
        {
            CreateAutoPatrolPoints(3f); // mặc định ±3f
        }

        if (hitbox != null)
        {
            hitbox.enabled = false;
        }

        if (health != null)
        {
            health.OnDeath.AddListener(OnDeath);
        }
    }

    private void Update()
    {
        if (state == State.Dead) return;
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // Đang attack thì đứng yên, chờ animation/event xử lý
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
            if (animator != null)
            {
                animator.SetBool("isAttacking", true);
            }
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
        {
            Flip();
        }
    }

    // Animation Event: gọi ở giữa animation Enemy_Attack
    public void OnAttackHit()
    {
        // Cách 1: bật hitbox collider, để script khác xử lý OnTriggerEnter2D
        if (hitbox != null)
        {
            hitbox.enabled = true;
        }

        // Cách 2 (đơn giản): check khoảng cách và damage player trực tiếp
        if (player != null && Vector2.Distance(transform.position, player.position) <= attackRange + 0.2f)
        {
            var playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }
    }

    // Animation Event: gọi ở cuối animation Enemy_Attack
    public void OnAttackFinished()
    {
        if (hitbox != null)
        {
            hitbox.enabled = false;
        }

        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }

        state = State.Run;
    }

    private void OnDeath()
    {
        state = State.Dead;
        rb.velocity = Vector2.zero;
        Destroy(gameObject); // xóa hẳn object
    }

    private void Flip()
    {
        facingRight = !facingRight;
        ApplyFacing();
    }

    /// <summary>
    /// Áp dụng hướng nhìn ra sprite dựa vào facingRight và spriteFacesRight
    /// </summary>
    private void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
        // Sprite gốc nhìn TRÁI (scale.x = 1).
        // facingRight = false  -> scale.x =  1 (trái)
        // facingRight = true   -> scale.x = -1 (phải)
        float dirSign = facingRight ? -1f : 1f;
        scale.x = Mathf.Abs(scale.x) * dirSign;
        transform.localScale = scale;
    }

    private void CreateAutoPatrolPoints(float offset)
    {
        if (autoPatrolPointsCreated) return;
        autoPatrolPointsCreated = true;

        GameObject left = new GameObject("AutoPatrolLeft");
        left.transform.position = transform.position + Vector3.left * offset;
        left.transform.SetParent(transform.parent);
        leftPoint = left.transform;

        GameObject right = new GameObject("AutoPatrolRight");
        right.transform.position = transform.position + Vector3.right * offset;
        right.transform.SetParent(transform.parent);
        rightPoint = right.transform;
    }
}


