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

    [Header("Attack State")]
    private float attackCooldown;
    private bool canAttack = true;

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
            attackPointObj.transform.localPosition = new Vector3(0.5f, 0, 0);
            attackPoint = attackPointObj.transform;
        }
    }

    private void Update()
    {
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

    private void Attack()
    {
        if (!canAttack) return;

        PlayerStats stats = controller.stats;
        if (stats == null) return;

        Debug.Log("Player attacks!");

        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger("Attack");
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

