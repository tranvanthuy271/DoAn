using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("Components")]
    private PlayerController controller;
    private Animator animator;

    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Attack Facing / Offsets")]
    [Tooltip("Local position của AttackPoint khi nhân vật nhìn sang PHẢI (scale.x > 0).")]
    [SerializeField] private Vector3 attackPointLocalPosRight = new Vector3(0.5f, 0f, 0f);

    [Tooltip("Local position của AttackPoint khi nhân vật nhìn sang TRÁI (scale.x < 0).")]
    [SerializeField] private Vector3 attackPointLocalPosLeft = new Vector3(-0.5f, 0f, 0f);

    [Tooltip("Nếu bạn có object Animator/VFX riêng cho đòn chém (ví dụ Slash), kéo vào đây để nó tự dịch theo hướng. Có thể để trống.")]
    [SerializeField] private Transform attackVisual;

    [Tooltip("Local position của Attack Visual khi nhìn PHẢI.")]
    [SerializeField] private Vector3 attackVisualLocalPosRight = new Vector3(0.5f, 0f, 0f);

    [Tooltip("Local position của Attack Visual khi nhìn TRÁI.")]
    [SerializeField] private Vector3 attackVisualLocalPosLeft = new Vector3(-0.5f, 0f, 0f);

    [Tooltip("Tên Trigger trong Animator để phát animation chém.")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("Attack State")]
    private float attackCooldown;
    private bool canAttack = true;
    private bool lastFacingRight;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Create attack point if not assigned
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.SetParent(transform);
            attackPointObj.transform.localPosition = attackPointLocalPosRight;
            attackPoint = attackPointObj.transform;
        }

        lastFacingRight = IsFacingRight();
        ApplyFacingOffsets(lastFacingRight);
    }

    private void Update()
    {
        // Keep offsets correct when player flips (scale.x changes)
        bool facingRight = IsFacingRight();
        if (facingRight != lastFacingRight)
        {
            lastFacingRight = facingRight;
            ApplyFacingOffsets(facingRight);
        }

        // Update attack cooldown
        if (!canAttack)
        {
            attackCooldown -= Time.deltaTime;
            if (attackCooldown <= 0f)
            {
                canAttack = true;
            }
        }

        // Attack input - Nhấn J để tấn công
        if (Input.GetKeyDown(KeyCode.J))
        {
            Attack();
        }
    }

    private bool IsFacingRight()
    {
        // Theo code flip hiện tại: phải = scale.x > 0, trái = scale.x < 0
        return transform.localScale.x >= 0f;
    }

    private void ApplyFacingOffsets(bool facingRight)
    {
        if (attackPoint != null)
        {
            attackPoint.localPosition = facingRight ? attackPointLocalPosRight : attackPointLocalPosLeft;
        }

        if (attackVisual != null)
        {
            attackVisual.localPosition = facingRight ? attackVisualLocalPosRight : attackVisualLocalPosLeft;
        }
    }

    private void Attack()
    {
        if (!canAttack) return;

        PlayerStats stats = controller.stats;
        if (stats == null) return;

        Debug.Log("Player attacks!");

        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger(attackTriggerName);
        }

        // Detect enemies in range
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        // Damage enemies
        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log($"Hit {enemy.name}");
            
            // Try to damage enemy - Trừ 1 HP
            var enemyHealth = enemy.GetComponent<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Trừ 1 HP mỗi lần đánh
                enemyHealth.TakeDamage(1);
            }
        }

        // Set cooldown
        canAttack = false;
        attackCooldown = 1f / stats.attackSpeed;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(attackPoint.position, attackRange);
        }
    }
}

