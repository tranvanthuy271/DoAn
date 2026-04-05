using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(NetworkAnimator))]
public class EnemyAI : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    private float _originalMoveSpeed;
    private Coroutine _slowCoroutine;
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
    private float _findPlayerTimer = 0f; // timer tìm lại player
    private float _retargetTimer   = 0f; // timer retarget player gần nhất
    private const float MAX_ATTACK_DURATION = 2f;

    // Skill system — set bởi HostSpawnConfigLoader sau khi spawn
    private EnemySkillSet _skillSet;

    private enum State { Run, MeleeAttack, Dead }
    private State state = State.Run;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        health = GetComponent<EnemyHealth>();
        networkController = GetComponent<NetworkEnemyController>();
        _originalMoveSpeed = moveSpeed;
        _skillSet = GetComponent<EnemySkillSet>(); // có thể null nếu chưa được gán

        // Đảm bảo NetworkAnimator luôn có Animator – tránh NullRef trong CheckParametersChanged
        if (networkAnimator != null && networkAnimator.Animator == null)
            networkAnimator.Animator = animator;

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
        // Tìm player theo tag "Player"
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        if (playerObjects.Length > 0)
        {
            // Ưu tiên player có NetworkPlayerHealth (tránh chọn enemy prefab bị gán nhau tag)
            NetworkPlayerHealth best = null;
            Transform bestTr = null;
            float bestDist = float.MaxValue;
            foreach (var obj in playerObjects)
            {
                var nph = obj.GetComponent<NetworkPlayerHealth>();
                var ph  = obj.GetComponent<PlayerHealth>();
                if (nph == null && ph == null) continue;
                float d = Vector2.Distance(transform.position, obj.transform.position);
                if (d < bestDist) { bestDist = d; best = nph; bestTr = obj.transform; }
            }
            if (bestTr != null) { player = bestTr; return; }
        }

        // Fallback: tìm PlayerController bất kỳ trong scene
        var ctrl = UnityEngine.Object.FindObjectOfType<PlayerController>();
        if (ctrl != null) player = ctrl.transform;
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

        // Retarget lại player gần nhất mỗi 1.5s — để chứa chấp nhận client mới join
        _retargetTimer -= Time.deltaTime;
        if (_retargetTimer <= 0f)
        {
            FindPlayerInNetwork();
            _retargetTimer = 1.5f;
        }
        
        if (player == null)
        {
            // Tự tìm lại player mỗi 0.5 giây (dùng timer thô)
            _findPlayerTimer -= Time.deltaTime;
            if (_findPlayerTimer <= 0f)
            {
                FindPlayerInNetwork();
                _findPlayerTimer = 0.5f;
            }
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

        // Aggro: nếu player trong detectionRange thì đuổi theo
        if (dist <= detectionRange)
        {
            // Hướng mặt vào player
            bool shouldFaceRight = player.position.x > transform.position.x;
            if (shouldFaceRight != facingRight)
            {
                facingRight = shouldFaceRight;
                ApplyFacing();
            }

            // Thử dùng skill trước
            if (_skillSet != null && _skillSet.HasSkills)
            {
                SkillEntry readySkill = _skillSet.TryGetReadySkill(dist);
                if (readySkill != null)
                {
                    state = State.MeleeAttack;
                    lastAttackTime = Time.time;
                    attackStartTime = Time.time;
                    rb.velocity = Vector2.zero;
                    StartCoroutine(UseSkillCoroutine(readySkill));
                    return;
                }
            }

            // Tấn công melee (fallback khi không có skill hoặc skill đang cooldown)
            if (dist <= meleeAttackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                StartMeleeAttack();
                return;
            }

            // Chạy về phía player khi chưa đủ tầm đánh
            if (dist > meleeAttackRange)
            {
                RunTowards(player.position.x);
                return;
            }
        }

        // Ngoài tầm phát hiện → tuần tra
        PatrolLoop();
    }

    private void StartMeleeAttack()
    {
        state = State.MeleeAttack;
        lastAttackTime = Time.time;
        attackStartTime = Time.time;
        rb.velocity = Vector2.zero;

        // Trigger animation trên tất cả client
        if (networkController != null)
            networkController.TriggerAttackServerRpc();
        else if (networkAnimator != null)
            networkAnimator.SetTrigger("Attack");
        else if (animator != null)
            animator.SetBool("isAttacking", true);

        // Gây sát thương sau 0.35s (hit frame) — không phụ thuộc Animation Event
        StartCoroutine(MeleeHitCoroutine());
    }

    private IEnumerator MeleeHitCoroutine()
    {
        yield return new WaitForSeconds(0.35f);

        // Chỉ gây damage nếu vẫn còn trong tầm (tránh lag compensation)
        if (player != null && Vector2.Distance(transform.position, player.position) <= meleeAttackRange + 0.5f)
        {
            ApplyDamageToTarget(player.gameObject, damage);
            Debug.Log($"[EnemyAI] {gameObject.name} melee hit player for {damage} dmg");
        }

        yield return new WaitForSeconds(0.45f);
        ForceResetAttackState();
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
            ApplyDamageToTarget(player.gameObject, damage);
        }
    }

    /// <summary>Thực thể skill từ EnemySkillSet: trigger animation, tnhả sát thương, reset state.</summary>
    private IEnumerator UseSkillCoroutine(SkillEntry skill)
    {
        // Phát animation trigger
        if (!string.IsNullOrEmpty(skill.animation_trigger))
        {
            if (networkAnimator != null)
                networkAnimator.SetTrigger(skill.animation_trigger);
            else if (animator != null)
                animator.SetTrigger(skill.animation_trigger);
        }

        // Chờ hit frame
        yield return new WaitForSeconds(0.3f);

        int dmg = _skillSet != null ? _skillSet.CalculateDamage(skill) : damage;

        if (skill.aoe)
        {
            float radius = skill.aoe_radius > 0f ? skill.aoe_radius : Mathf.Max(skill.range, 1f);
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, radius,
                LayerMask.GetMask("Player"));
            foreach (var col in hits)
                ApplyDamageToTarget(col.gameObject, dmg);
        }
        else if (player != null)
        {
            float effectiveRange = skill.range > 0f ? skill.range : meleeAttackRange + 0.5f;
            if (Vector2.Distance(transform.position, player.position) <= effectiveRange)
                ApplyDamageToTarget(player.gameObject, dmg);
        }

        if (_skillSet != null)
            _skillSet.MarkSkillUsed(skill.skill_id);

        // Chờ animation kết thúc rồi reset state
        yield return new WaitForSeconds(0.5f);
        state = State.Run;
    }

    /// <summary>Shared helper: apply damage to player by checking NetworkPlayerHealth first.</summary>
    private void ApplyDamageToTarget(GameObject target, int dmg)
    {
        var netHealth = target.GetComponentInParent<NetworkPlayerHealth>();
        if (netHealth != null) { netHealth.TakeDamage(dmg); return; }
        var ph = target.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(dmg);
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
        if (state == State.Dead) return; // tránh gọi 2 lần
        state = State.Dead;

        if (rb != null) rb.velocity = Vector2.zero;

        // Tắt hitbox và EnemyAI Update ngay
        if (hitbox != null) hitbox.enabled = false;

        // Trigger animation Die trước khi destroy
        bool hasDieAnim = false;
        if (networkAnimator != null)
        {
            // Kiểm tra Animator có parameter "Die" không trước khi gọi setter
            if (animator != null)
            {
                foreach (var p in animator.parameters)
                {
                    if (p.name == "Die")
                    {
                        hasDieAnim = true;
                        break;
                    }
                }
            }
            if (hasDieAnim)
                networkAnimator.SetTrigger("Die");
        }
        else if (animator != null)
        {
            foreach (var p in animator.parameters)
            {
                if (p.name == "Die") { hasDieAnim = true; break; }
            }
            if (hasDieAnim)
                animator.SetTrigger("Die");
        }

        // Chờ animation rồi mới Destroy (nếu là standalone EnemyHealth)
        // NetworkEnemyHealth tự xử lý Despawn ở HandleDeath
        NetworkObject netObj = GetComponent<NetworkObject>();
        if (netObj == null)
        {
            // Standalone mode: chờ animation die rồi destroy
            float delay = hasDieAnim ? 0.8f : 0.1f;
            Destroy(gameObject, delay);
        }
        // Nếu là network mode: NetworkEnemyHealth.HandleDeath() đã lên lịch Despawn — không Destroy ở đây
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

    /// <summary>
    /// Giảm tốc độ di chuyển trong khoảng thời gian nhất định (50% speed).
    /// </summary>
    public void ApplySlow(float duration)
    {
        if (_slowCoroutine != null)
            StopCoroutine(_slowCoroutine);
        _slowCoroutine = StartCoroutine(SlowCoroutine(duration));
    }

    private IEnumerator SlowCoroutine(float duration)
    {
        moveSpeed = _originalMoveSpeed * 0.5f;
        yield return new WaitForSeconds(duration);
        moveSpeed = _originalMoveSpeed;
        _slowCoroutine = null;
    }
}


