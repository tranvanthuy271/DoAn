using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(NetworkAnimator))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public Transform leftPoint;   // điểm biên trái
    public Transform rightPoint;  // điểm biên phải

    [Header("Combat")]
    public float detectionRange = 5f;
    public float meleeAttackRange = 1.2f;  // Khoảng cách đánh thường (gần)
    public float attackCooldown = 1.0f;
    public int damage = 2;
    public Collider2D hitbox; // isTrigger, disable mặc định

    private Transform player;
    private Rigidbody2D rb;
    private NetworkAnimator networkAnimator; // Dùng NetworkAnimator thay vì Animator
    private Animator animator; // Lấy từ NetworkAnimator.Animator
    private EnemyHealth health;
    private NetworkEnemyController networkController;

    private bool facingRight = true;
    private float lastAttackTime;
    private bool autoPatrolPointsCreated = false;
    private float attackStartTime;
    private const float MAX_ATTACK_DURATION = 2f;

    private enum State { Run, MeleeAttack, Dead }
    private State state = State.Run;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        health = GetComponent<EnemyHealth>();
        networkController = GetComponent<NetworkEnemyController>();

        ApplyFacing();

        if (leftPoint == null || rightPoint == null)
        {
            CreateAutoPatrolPoints(3f);
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

    private void Start()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        FindPlayerInNetwork();
    }

    private void FindPlayerInNetwork()
    {
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        if (playerObjects.Length > 0)
        {
            player = playerObjects[0].transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
    }

    private void Update()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        if (state == State.Dead) return;
        
        if (rb != null)
        {
            if (rb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionX))
            {
                rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
            }
        }
        
        if (player == null)
        {
            FindPlayerInNetwork();
        }
        
        if (player == null)
        {
            PatrolLoop();
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);

        if (state == State.MeleeAttack)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
            
            if (Time.time - attackStartTime >= MAX_ATTACK_DURATION)
            {
                ForceResetAttackState();
            }
            return;
        }

        if (dist <= meleeAttackRange && Time.time - lastAttackTime >= attackCooldown)
        {
            StartMeleeAttack();
            return;
        }

        PatrolLoop();
    }

    private void StartMeleeAttack()
    {
        state = State.MeleeAttack;
        lastAttackTime = Time.time;
        attackStartTime = Time.time;
        rb.velocity = Vector2.zero;

        if (networkController != null)
        {
            networkController.TriggerAttackServerRpc();
        }
        else if (animator != null)
        {
            animator.SetBool("isAttacking", true);
        }
    }


    private void PatrolLoop()
    {
        if (leftPoint == null || rightPoint == null)
        {
            if (!autoPatrolPointsCreated)
            {
                CreateAutoPatrolPoints(3f);
            }
            if (leftPoint == null || rightPoint == null)
            {
                return;
            }
        }

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
        if (rb == null) return;
        
        float dir = Mathf.Sign(targetX - transform.position.x);
        Vector2 newVelocity = new Vector2(dir * moveSpeed, rb.velocity.y);
        rb.velocity = newVelocity;

        if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
        {
            Flip();
        }
    }

    public void OnAttackHit()
    {
        if (hitbox != null)
        {
            hitbox.enabled = true;
        }

        if (player != null && Vector2.Distance(transform.position, player.position) <= meleeAttackRange + 0.2f)
        {
            var networkPlayerHealth = player.GetComponent<NetworkPlayerHealth>();
            if (networkPlayerHealth != null)
            {
                networkPlayerHealth.TakeDamage(damage);
            }
            else
            {
                var playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(damage);
                }
            }
        }
    }

    public void OnAttackFinished()
    {
        ForceResetAttackState();
    }

    private void ForceResetAttackState()
    {
        if (hitbox != null)
        {
            hitbox.enabled = false;
        }

        state = State.Run;
        
        if (networkController != null)
        {
            networkController.ResetAttackAnimationClientRpc();
        }
        else if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }
    }

    private void OnDeath()
    {
        state = State.Dead;
        rb.velocity = Vector2.zero;
        Destroy(gameObject);
    }

    private void Flip()
    {
        facingRight = !facingRight;
        ApplyFacing();
    }

    private void ApplyFacing()
    {
        Vector3 scale = transform.localScale;
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


