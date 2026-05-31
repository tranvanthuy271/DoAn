using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// ─────────────────────────────────────────────────────────────────────────────
//  BossController  —  AI chính của Boss
//
//  TÍNH NĂNG:
//    • State machine: Idle → Chase → Attack → Dodge → Dead
//    • Né tránh theo xác suất (dodgeChance)
//    • Đánh thường + 3 kỹ năng đặc biệt (hỏa cầu / sét / ẩn thân)
//    • Tự hồi HP khi HP < ngưỡng
//    • Sát thương cố định phản lại người đánh
//    • Nhảy (canJump) hoặc bay lượn (canFly) tùy config
//    • Mọi attack đều bật bool "isAttacking" trên Animator
//
//  SETUP (xem HUONG_DAN_BOSS_ADVANCED.md):
//    1. Attach vào Boss prefab cùng NetworkObject, Rigidbody2D, Animator
//    2. Gán BossData ScriptableObject vào trường data
//    3. Gán các prefab projectile trong Inspector (hoặc trong BossData)
//    4. Gán groundCheck Transform
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class BossController : MonoBehaviour
{
    // ── Config ───────────────────────────────────────────────────────────────
    [Header("Config (ScriptableObject)")]
    public BossData data;

    // ── Prefab overrides (nếu không muốn set trong BossData) ─────────────────
    [Header("Skill Prefabs (override nếu BossData chưa có)")]
    [Tooltip("Prefab hỏa cầu")]
    public GameObject fireballPrefab;
    [Tooltip("Prefab tia sét")]
    public GameObject lightningPrefab;

    // ── Physics / Ground ─────────────────────────────────────────────────────
    [Header("Ground Check")]
    public Transform groundCheck;
    [Tooltip("Radius overlap circle để kiểm tra ground")]
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // ── Internal ─────────────────────────────────────────────────────────────
    private Rigidbody2D  _rb;
    private Animator     _anim;
    private NetworkBossHealth _netHealth;   // Có thể null nếu chạy offline
    private EnemyHealth       _localHealth; // Fallback offline

    private Transform _target;  // target player

    // State machine
    private enum BossState { Idle, Chase, Attacking, Dodging, Stealthed, Dead }
    private BossState _state = BossState.Idle;

    // Skill cooldown tracking
    private float _normalAttackCooldown = 0f;
    private float _fireballCooldown     = 0f;
    private float _lightningCooldown    = 0f;
    private float _stealthCooldown      = 0f;
    private float _dodgeCooldown        = 0f;

    // Jump
    private bool _isGrounded = false;
    private int  _jumpsLeft  = 0;

    // Fly
    private float _groundBaseY;     // Y lúc spawn (để tính fly height)
    private bool  _flyModeActive;

    // Regen
    private float _regenAccum = 0f;

    // Stealth
    private bool _isStealthed = false;
    private SpriteRenderer[] _renderers;

    // Animation hash (cached)
    private static readonly int AnimIsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int AnimIsMoving    = Animator.StringToHash("isMoving");
    private static readonly int AnimIsGrounded  = Animator.StringToHash("isGrounded");
    private static readonly int AnimJump        = Animator.StringToHash("Jump");

    // Facing
    private bool _facingRight = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  Init
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb         = GetComponent<Rigidbody2D>();
        _anim       = GetComponent<Animator>();
        _netHealth  = GetComponent<NetworkBossHealth>();
        _localHealth = GetComponent<EnemyHealth>();
        _renderers  = GetComponentsInChildren<SpriteRenderer>(true);

        // Freeze rotation (chuẩn 2D)
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void Start()
    {
        if (data == null)
        {
            Debug.LogError("[BossController] BossData chưa được gán!", this);
            enabled = false;
            return;
        }

        _groundBaseY = transform.position.y;
        _jumpsLeft   = data.maxJumps;

        // Nếu là server/standalone → bắt đầu tìm target
        if (ShouldRunAI())
            InvokeRepeating(nameof(RefreshTarget), 0f, 1f);

        // Lắng nghe event damage để xử lý dodge & return damage
        if (_netHealth != null)
        {
            _netHealth.OnBeforeTakeDamage += HandleBeforeTakeDamage;
            _netHealth.OnAfterTakeDamage  += HandleAfterTakeDamage;
        }
        else if (_localHealth != null)
        {
            _localHealth.OnTakeDamage.AddListener(OnLocalTakeDamage);
            _localHealth.OnDeath.AddListener(OnDead);
        }
    }

    private void OnDestroy()
    {
        if (_netHealth != null)
        {
            _netHealth.OnBeforeTakeDamage -= HandleBeforeTakeDamage;
            _netHealth.OnAfterTakeDamage  -= HandleAfterTakeDamage;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Server/Standalone gate
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>AI chỉ chạy trên server (hoặc standalone không có NetworkManager).</summary>
    private bool ShouldRunAI()
    {
        return NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Update loop
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!ShouldRunAI()) return;
        if (data == null)  return;

        TickCooldowns();
        HandleHpRegen();

        if (_state == BossState.Dead) return;

        DoGroundCheck();
        RunStateMachine();
    }

    private void TickCooldowns()
    {
        float dt = Time.deltaTime;
        _normalAttackCooldown = Mathf.Max(0f, _normalAttackCooldown - dt);
        _fireballCooldown     = Mathf.Max(0f, _fireballCooldown     - dt);
        _lightningCooldown    = Mathf.Max(0f, _lightningCooldown    - dt);
        _stealthCooldown      = Mathf.Max(0f, _stealthCooldown      - dt);
        _dodgeCooldown        = Mathf.Max(0f, _dodgeCooldown        - dt);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  State Machine
    // ─────────────────────────────────────────────────────────────────────────

    private void RunStateMachine()
    {
        if (_state == BossState.Dodging || _state == BossState.Stealthed) return;
        if (_target == null) { _state = BossState.Idle; return; }

        float dist = Vector2.Distance(transform.position, _target.position);

        if (dist > data.detectionRange)
        {
            _state = BossState.Idle;
            SetMovingAnim(false);
            return;
        }

        // Luôn flip về phía target
        FaceTarget();

        // Bay lượn
        if (data.canFly)
        {
            HandleFlyMovement();
        }

        // Thử kỹ năng đặc biệt trước (ưu tiên cao hơn đánh thường)
        if (TryUseSpecialSkill()) return;

        // Đánh thường khi đủ gần
        if (dist <= data.meleeAttackRange && _normalAttackCooldown <= 0f)
        {
            DoNormalAttack();
            return;
        }

        // Chase
        if (!data.canFly)
        {
            ChaseTarget(dist);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Chase & Movement
    // ─────────────────────────────────────────────────────────────────────────

    private void ChaseTarget(float dist)
    {
        _state = BossState.Chase;
        float dir = _target.position.x > transform.position.x ? 1f : -1f;
        _rb.velocity = new Vector2(dir * data.chaseSpeed, _rb.velocity.y);
        SetMovingAnim(true);

        // Nhảy khi gặp tường hoặc vực (đơn giản: kiểm tra grounded + target cao hơn)
        if (data.canJump && _isGrounded && _jumpsLeft > 0)
        {
            float heightDiff = _target.position.y - transform.position.y;
            if (heightDiff > 0.8f)
            {
                PerformJump();
            }
        }
    }

    private void HandleFlyMovement()
    {
        if (_target == null) return;

        // Target position = player X + offset height
        float targetX = _target.position.x;
        float targetY = _target.position.y + data.flyHeight;
        Vector2 flyTarget = new Vector2(targetX, targetY);
        Vector2 dir = (flyTarget - (Vector2)transform.position).normalized;
        _rb.velocity = dir * data.flySpeed;
        _rb.gravityScale = 0f;
        SetMovingAnim(true);
    }

    private void PerformJump()
    {
        _jumpsLeft--;
        _rb.velocity = new Vector2(_rb.velocity.x, data.jumpForce);
        _anim.SetTrigger(AnimJump);
    }

    private void DoGroundCheck()
    {
        if (groundCheck == null) return;
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (_isGrounded && !wasGrounded)
            _jumpsLeft = data.maxJumps; // Reset jumps on land

        if (_anim != null)
            _anim.SetBool(AnimIsGrounded, _isGrounded);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Skills
    // ─────────────────────────────────────────────────────────────────────────

    /// <returns>true nếu đã kích hoạt skill nào đó</returns>
    private bool TryUseSpecialSkill()
    {
        // Ẩn thân — ưu tiên cao nhất
        if (data.stealth.enabled && _stealthCooldown <= 0f && !_isStealthed)
        {
            StartCoroutine(DoStealth());
            return true;
        }

        // Sét liên tiếp
        if (data.lightning.enabled && _lightningCooldown <= 0f && _target != null)
        {
            StartCoroutine(DoLightningStrike());
            return true;
        }

        // Hỏa cầu mưa
        if (data.fireballRain.enabled && _fireballCooldown <= 0f && _target != null)
        {
            StartCoroutine(DoFireballRain());
            return true;
        }

        return false;
    }

    // ── Đánh thường ──────────────────────────────────────────────────────────

    private void DoNormalAttack()
    {
        _state = BossState.Attacking;
        _normalAttackCooldown = data.normalAttack.cooldown;
        StartCoroutine(NormalAttackCoroutine());
    }

    private IEnumerator NormalAttackCoroutine()
    {
        SetAttackAnim(true);
        _rb.velocity = Vector2.zero;

        // Delay nhỏ để animation chạy
        yield return new WaitForSeconds(0.25f);

        // Hitbox check vùng tấn công
        LayerMask mask = data.normalAttack.playerLayer != 0
            ? data.normalAttack.playerLayer
            : LayerMask.GetMask("Player");

        Vector2 attackCenter = transform.position + Vector3.right * (_facingRight ? data.normalAttack.range : -data.normalAttack.range);
        Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, attackCenter, data.normalAttack.range, mask.value);

        Debug.Log($"[BossController] NormalAttack hits={hits.Length} center={attackCenter} range={data.normalAttack.range}");

        foreach (var hit in hits)
        {
            DealDamageToPlayer(hit, data.normalAttack.damage);

            // Knockback
            var playerRb = hit.GetComponentInParent<Rigidbody2D>();
            if (playerRb != null)
            {
                float kbDir = hit.transform.position.x > transform.position.x ? 1f : -1f;
                playerRb.AddForce(new Vector2(kbDir * data.normalAttack.knockback, 2f), ForceMode2D.Impulse);
            }
        }

        yield return new WaitForSeconds(0.4f);
        SetAttackAnim(false);
        _state = BossState.Chase;
    }

    // ── Hỏa Cầu Mưa ──────────────────────────────────────────────────────────

    private IEnumerator DoFireballRain()
    {
        _state = BossState.Attacking;
        _fireballCooldown = data.fireballRain.cooldown;
        SetAttackAnim(true);

        GameObject prefab = data.fireballRain.fireballPrefab != null
            ? data.fireballRain.fireballPrefab
            : fireballPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("[BossController] Fireball prefab chưa được gán!");
            SetAttackAnim(false);
            _state = BossState.Chase;
            yield break;
        }

        for (int i = 0; i < data.fireballRain.count; i++)
        {
            float offsetX = Random.Range(-data.fireballRain.spreadRadius, data.fireballRain.spreadRadius);
            Vector3 spawnPos = new Vector3(
                _target.position.x + offsetX,
                _target.position.y + data.fireballRain.spawnHeight,
                transform.position.z);

            GameObject fb = Instantiate(prefab, spawnPos, Quaternion.identity);
            var comp = fb.GetComponent<BossFireball>();
            if (comp != null)
                comp.Init(data.fireballRain.damage, data.fireballRain.fallSpeed);

            // Spawn qua network nếu có
            var netObj = fb.GetComponent<NetworkObject>();
            if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                netObj.Spawn(true);

            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.5f);
        SetAttackAnim(false);
        _state = BossState.Chase;
    }

    // ── Sét Liên Tiếp ─────────────────────────────────────────────────────────

    private IEnumerator DoLightningStrike()
    {
        _state = BossState.Attacking;
        _lightningCooldown = data.lightning.cooldown;
        SetAttackAnim(true);
        _rb.velocity = Vector2.zero;

        GameObject prefab = data.lightning.lightningPrefab != null
            ? data.lightning.lightningPrefab
            : lightningPrefab;

        if (prefab == null)
        {
            Debug.LogWarning("[BossController] Lightning prefab chưa được gán!");
            SetAttackAnim(false);
            _state = BossState.Chase;
            yield break;
        }

        float startX = _target != null ? _target.position.x - (data.lightning.boltCount / 2f) * data.lightning.boltSpacing : transform.position.x;

        for (int i = 0; i < data.lightning.boltCount; i++)
        {
            float bx = startX + i * data.lightning.boltSpacing;
            Vector3 spawnPos = new Vector3(bx, transform.position.y + 4f, transform.position.z);

            GameObject bolt = Instantiate(prefab, spawnPos, Quaternion.identity);
            var comp = bolt.GetComponent<BossLightningBolt>();
            if (comp != null)
                comp.Init(data.lightning.damage, data.lightning.boltDuration, data.lightning.stunDuration);

            var netObj = bolt.GetComponent<NetworkObject>();
            if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                netObj.Spawn(true);

            yield return new WaitForSeconds(data.lightning.boltDelay);
        }

        yield return new WaitForSeconds(0.6f);
        SetAttackAnim(false);
        _state = BossState.Chase;
    }

    // ── Ẩn Thân ──────────────────────────────────────────────────────────────

    private IEnumerator DoStealth()
    {
        _state = BossState.Stealthed;
        _stealthCooldown = data.stealth.cooldown;
        _isStealthed = true;

        // Không tắt isAttacking ở đây vì ẩn thân không phải attack animation
        // Thay vào đó giảm alpha sprite
        SetRendererAlpha(data.stealth.stealthAlpha);

        // Vẫn có thể di chuyển trong stealth
        float elapsed = 0f;
        while (elapsed < data.stealth.duration)
        {
            elapsed += Time.deltaTime;
            if (_target != null && !data.canFly)
            {
                float dir = _target.position.x > transform.position.x ? 1f : -1f;
                _rb.velocity = new Vector2(dir * data.chaseSpeed, _rb.velocity.y);
                FaceTarget();
            }
            yield return null;
        }

        // Hiện lại
        _isStealthed = false;
        SetRendererAlpha(1f);
        _state = BossState.Chase;
    }

    // ── Dodge ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ NetworkBossHealth/EnemyHealth khi nhận damage.
    /// Trả về true nếu boss né thành công (damage không được tính).
    /// </summary>
    public bool TryDodge()
    {
        if (_state == BossState.Dead) return false;
        if (_dodgeCooldown > 0f) return false;
        if (data == null || data.dodgeChance <= 0f) return false;

        float roll = Random.Range(0f, 100f);
        if (roll > data.dodgeChance) return false;

        // Né thành công
        _dodgeCooldown = data.dodgeCooldown;
        StartCoroutine(DodgeCoroutine());
        return true;
    }

    private IEnumerator DodgeCoroutine()
    {
        _state = BossState.Dodging;
        _anim.SetTrigger("Dodge");

        float dodgeDir = _facingRight ? -1f : 1f; // Né về phía sau
        _rb.velocity = new Vector2(dodgeDir * data.dodgeDistance * 4f, _rb.velocity.y);

        yield return new WaitForSeconds(0.35f);

        _rb.velocity = new Vector2(0f, _rb.velocity.y);
        _state = BossState.Chase;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HP Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Được gọi bởi NetworkBossHealth TRƯỚC khi trừ máu.
    /// Trả về damage thực tế sau kháng và dodge.
    /// </summary>
    public int HandleBeforeTakeDamage(int rawDamage, string elementType, ulong attackerClientId)
    {
        if (_state == BossState.Dead) return 0;

        // Dodge check
        if (TryDodge()) return 0;

        // Kháng nguyên tố
        int resist = GetResistance(elementType);
        return DamageCalculator.CalcBossReceivedDamage(rawDamage, resist);
    }

    /// <summary>Gọi sau khi trừ máu — trả lại damage nếu có config.</summary>
    public void HandleAfterTakeDamage(int finalDamage, ulong attackerClientId)
    {
        if (!data.returnDamageEnabled || data.returnDamageAmount <= 0) return;
        ReturnDamageToPlayer(attackerClientId, data.returnDamageAmount);
    }

    private void OnLocalTakeDamage()
    {
        // Local (non-network) — không có element info, chỉ check dodge
        TryDodge();
    }

    public void OnDead()
    {
        _state = BossState.Dead;
        _rb.velocity = Vector2.zero;
        _rb.bodyType = RigidbodyType2D.Kinematic;
        StopAllCoroutines();
        SetAttackAnim(false);
        SetMovingAnim(false);
        SetRendererAlpha(1f);
        enabled = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  HP Regen
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleHpRegen()
    {
        if (!data.hpRegenEnabled || data.regenPerSec <= 0f) return;

        int maxHp  = GetMaxHp();
        int curHp  = GetCurrentHp();
        if (maxHp <= 0) return;

        float pct = curHp / (float)maxHp * 100f;
        if (pct > data.regenThresholdPct) return;

        _regenAccum += data.regenPerSec * Time.deltaTime;
        if (_regenAccum >= 1f)
        {
            int healAmt = Mathf.FloorToInt(_regenAccum);
            _regenAccum -= healAmt;
            HealBoss(healAmt);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers — Health
    // ─────────────────────────────────────────────────────────────────────────

    private int GetMaxHp()
    {
        if (_netHealth != null) return _netHealth.GetMaxHealth();
        if (_localHealth != null) return _localHealth.GetMaxHealth();
        return data.maxHealth;
    }

    private int GetCurrentHp()
    {
        if (_netHealth != null) return _netHealth.GetCurrentHealth();
        if (_localHealth != null) return _localHealth.GetCurrentHealth();
        return 0;
    }

    private void HealBoss(int amount)
    {
        if (_netHealth != null) _netHealth.HealServer(amount);
        else if (_localHealth != null) _localHealth.Heal(amount);
    }

    private void DealDamageToPlayer(Collider2D col, int dmg)
    {
        var netPH = col.GetComponentInParent<NetworkPlayerHealth>();
        if (netPH != null) { netPH.TakeDamage(dmg); return; }
        var ph = col.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(dmg);
    }

    private void ReturnDamageToPlayer(ulong clientId, int dmg)
    {
        if (NetworkManager.Singleton == null) return;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.ClientId != clientId) continue;
            var netPH = client.PlayerObject?.GetComponent<NetworkPlayerHealth>();
            if (netPH != null) netPH.TakeDamage(dmg);
            break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers — Misc
    // ─────────────────────────────────────────────────────────────────────────

    private int GetResistance(string elementType)
    {
        if (data == null) return 0;
        return elementType switch
        {
            "Hoa"   => data.khangHoa,
            "Thuy"  => data.khangThuy,
            "Tho"   => data.khangTho,
            "Moc"   => data.khangMoc,
            "Kim"   => data.khangKim,
            "Phong" => data.khangPhong,
            _       => 0
        };
    }

    private void RefreshTarget()
    {
        if (!ShouldRunAI()) return;

        float bestDist = float.MaxValue;
        Transform best = null;

        // Tìm player gần nhất trong range
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            float d = Vector2.Distance(transform.position, p.transform.position);
            if (d < bestDist && d <= data.detectionRange * 1.5f)
            {
                bestDist = d;
                best     = p.transform;
            }
        }
        _target = best;
    }

    private void FaceTarget()
    {
        if (_target == null) return;
        bool shouldFaceRight = _target.position.x > transform.position.x;
        if (shouldFaceRight == _facingRight) return;
        _facingRight = shouldFaceRight;
        Vector3 s = transform.localScale;
        s.x = _facingRight ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    private void SetAttackAnim(bool state)
    {
        if (_anim != null) _anim.SetBool(AnimIsAttacking, state);
    }

    private void SetMovingAnim(bool state)
    {
        if (_anim != null) _anim.SetBool(AnimIsMoving, state);
    }

    private void SetRendererAlpha(float alpha)
    {
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            Color c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Gizmos (Editor only)
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.detectionRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.meleeAttackRange);
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
