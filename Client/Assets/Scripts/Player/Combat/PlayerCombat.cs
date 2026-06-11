using UnityEngine;
using Unity.Netcode;

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
        // Fallback: if the mask wasn't set in the Inspector, default to the "Enemy" layer.
        if (enemyLayers.value == 0)
        {
            enemyLayers = LayerMask.GetMask("Enemy");
            Debug.Log("[PlayerCombat] enemyLayers was 0, automatically set to 'Enemy' layer mask.");
        }
        Debug.Log($"[PlayerCombat] enemyLayers mask value: {enemyLayers.value}");

        // Create attack point if not assigned in Inspector
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

        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked)
            return;

        // Attack input — phím N dùng cho debug/fallback.
        // Khi PlayerSkillManager có NormalAttack slot, Z / LMB sẽ gọi TriggerAttack() thay thế.
        if (Input.GetKeyDown(KeyCode.N))
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

    // Cho phép hệ thống skill (PlayerSkillManager) kích hoạt đòn đánh thường.
    public void TriggerAttack(int overrideDamage = -1)
    {
        if (!canAttack) return;
        if (overrideDamage > 0)
        {
            // Dùng damage từ DB thay vì baseDamage
            int savedBase = controller?.stats?.baseDamage ?? 0;
            if (controller?.stats != null) controller.stats.baseDamage = overrideDamage;
            Attack();
            if (controller?.stats != null) controller.stats.baseDamage = savedBase;
        }
        else
        {
            Attack();
        }
    }

    // Trả về: True nếu skill đánh thường hiện đang sẵn sàng.
    public bool CanAttackNow => canAttack;

    private void Attack()
    {
        if (!canAttack) return;

        PlayerStats stats = controller.stats;
        if (stats == null) return;

        int damage = stats.baseDamage;
        if (ActiveBuffManager.Instance != null)
        {
            float attackBonusPct = ActiveBuffManager.Instance.GetBonusPct("AttackBuff");
            if (attackBonusPct > 0f)
                damage = Mathf.RoundToInt(damage * (1f + attackBonusPct));
        }

        Debug.Log("Player attacks!");

        // Play attack animation
        if (animator != null)
        {
            animator.SetTrigger(attackTriggerName);
        }

        // Detect enemies in range
        Collider2D[] hitEnemies = MapPhysicsQuery2D.OverlapCircleAll(gameObject, attackPoint.position, attackRange, enemyLayers.value);
        Debug.Log($"[PlayerCombat] Detected {hitEnemies.Length} enemies in range.");
        foreach (var e in hitEnemies)
        {
            Debug.Log($"[PlayerCombat] Hit candidate: {e.name}, tag={e.tag}, layer={LayerMask.LayerToName(e.gameObject.layer)}");
        }
        foreach (Collider2D enemy in hitEnemies)
        {
            // Bỏ qua chính mình — enemyLayers mask có thể bao gồm Player layer
            if (enemy.transform.root == transform.root) continue;

            Debug.Log($"Hit {enemy.name} for {damage} damage");
            // Dùng GetComponentInParent để tìm được khi collider nằm ở child object
            var networkEnemyHealth = enemy.GetComponentInParent<NetworkEnemyHealth>();
            if (networkEnemyHealth != null)
            {
                // Lấy clientId của người tấn công (quan trọng cho quest kill tracking)
                ulong attackerId = GetComponent<NetworkObject>()?.OwnerClientId ?? ulong.MaxValue;
                // Gây damage từ baseDamage trong PlayerStats (tự động gọi ServerRpc)
                networkEnemyHealth.TakeDamage(damage, attackerId);
                Debug.Log($"[PlayerCombat] Dealt {damage} damage to {enemy.transform.root.name} (NetworkEnemyHealth)");
            }
            else
            {
                // Fallback: Dùng EnemyHealth cũ (không network)
                var enemyHealth = enemy.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.TakeDamage(damage);
                    Debug.Log($"[PlayerCombat] Dealt {damage} damage to {enemy.transform.root.name} (EnemyHealth - fallback)");
                }
            }
        }

        // Set cooldown
        canAttack = false;
        attackCooldown = 1f / stats.attackSpeed;

        // PvP: quét thêm player khác trong tầm (không dùng enemyLayers — Player layer thường không ở đó)
        NetworkObject selfNetObj = GetComponent<NetworkObject>();
        Collider2D[] pvpHits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, attackPoint.position, attackRange);
        foreach (Collider2D hit in pvpHits)
        {
            if (hit.gameObject == gameObject) continue;
            NetworkObject hitNetObj = hit.GetComponent<NetworkObject>();
            if (selfNetObj != null && hitNetObj != null && hitNetObj.NetworkObjectId == selfNetObj.NetworkObjectId) continue;
            if (!hit.CompareTag("Player")) continue;
            var netPlayer = hit.GetComponentInParent<NetworkPlayerHealth>();
            if (netPlayer != null)
            {
                netPlayer.TakeDamage(damage);
                Debug.Log($"[PlayerCombat] Dealt {damage} PvP damage to {hit.name}");
            }
        }
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

