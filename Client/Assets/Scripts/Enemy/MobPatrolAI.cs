using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// MobPatrolAI — AI quái nâng cao với tuần tra, phát hiện, phản công và kháng nguyên tố.
///
/// TÍNH NĂNG (dựa trên LangLa Mob.java):
///   • Tuần tra giữa leftPoint/rightPoint (giống EnemyAI gốc)
///   • Aggro range: khi thấy player → đuổi
///   • Flip sprite khi đổi hướng
///   • Resistances: khangHoa/Thuy/Tho/Moc/Kim/Phong (% giảm sát thương theo nguyên tố)
///   • Evasion: xác suất né tránh đòn (neTranh)
///   • Counter: khi bị đánh, xác suất phản đòn (phanDon)
///   • HP Regen: hồi HP mỗi giây nếu hoiHp > 0
///   • Stun, Freeze, Weaken: trạng thái đặc biệt
///   • Support: load config từ element-based asset hoặc hardcode từ Inspector
///
/// SETUP:
///   1. Attach vào quái prefab cùng EnemyHealth, Rigidbody2D (Kinematic), Collider2D, Animator
///   2. Assign leftPoint, rightPoint cho phạm vi tuần tra
///   3. Tuỳ chỉnh stats trong Inspector
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(EnemyHealth))]
public class MobPatrolAI : MonoBehaviour
{
    // ── Patrol ──
    [Header("Patrol Points")]
    public Transform leftPoint;
    public Transform rightPoint;

    [Header("Movement")]
    public float moveSpeed      = 2f;
    public float chaseSpeed     = 3f;
    public float detectionRange = 5f;
    public float attackRange    = 1.3f;
    public float attackCooldown = 1.0f;

    [Header("Combat")]
    public int  baseDamage = 5;
    public int  hitboxDamage = 3;       // Damage từ hitbox collider (OnTriggerEnter)
    public Collider2D hitbox;            // isTrigger, tắt mặc định

    // ── Mob stats (từ LangLa Mob.java) ──
    [Header("Element Resistances (% giảm sát thương, 0–100)")]
    [Range(0, 100)] public int khangHoa   = 0;  // Kháng Hỏa
    [Range(0, 100)] public int khangThuy  = 0;  // Kháng Thủy
    [Range(0, 100)] public int khangTho   = 0;  // Kháng Thổ
    [Range(0, 100)] public int khangMoc   = 0;  // Kháng Mộc
    [Range(0, 100)] public int khangKim   = 0;  // Kháng Kim
    [Range(0, 100)] public int khangPhong = 0;  // Kháng Phong

    [Header("Special Stats")]
    [Range(0f, 50f)] public float hpRegenPerSec = 0f;   // Hồi HP/giây (HoiHp)
    [Range(0f, 100f)] public float evasionRate  = 0f;   // % né tránh (NeTranh)
    [Range(0f, 50f)]  public float counterRate  = 0f;   // % phản đòn (PhanDon)

    // ── Status effects (bị áp đặt bởi player skill) ──
    [HideInInspector] public bool isStunned;   // IsChoang
    [HideInInspector] public bool isFrozen;    // IsBong
    [HideInInspector] public bool isWeakened;  // IsSuyYeu — tăng damage nhận 30%

    // ── Private ──
    private EnemyHealth   _health;
    private Rigidbody2D   _rb;
    private Animator      _anim;
    private NetworkAnimator _netAnim;

    private Transform _player;
    private bool      _facingRight = true;
    private float     _lastAttack   = 0f;
    private float     _hpRegenAccum = 0f;

#pragma warning disable CS0414
    private bool _patrolPointsAuto = false;
#pragma warning restore CS0414

    private enum State { Patrol, Chase, Attack, Stunned, Dead }
    private State _state = State.Patrol;

    // ══════════════════════════════════════════════
    // Init
    // ══════════════════════════════════════════════

    private void Awake()
    {
        _health  = GetComponent<EnemyHealth>();
        _rb      = GetComponent<Rigidbody2D>();
        _anim    = GetComponent<Animator>();
        _netAnim = GetComponent<NetworkAnimator>();

        if (hitbox != null) hitbox.enabled = false;

        if (leftPoint == null || rightPoint == null)
        {
            CreateAutoPatrolPoints(3f);
            _patrolPointsAuto = true;
        }

        _health.OnDeath.AddListener(OnDeath);
        _health.OnTakeDamage.AddListener(OnTakeDamage);
    }

    private void Start()
    {
        FindNearestPlayer();
    }

    // ══════════════════════════════════════════════
    // Update
    // ══════════════════════════════════════════════

    private void Update()
    {
        if (_state == State.Dead) return;

        // HP Regen
        if (hpRegenPerSec > 0f)
        {
            _hpRegenAccum += hpRegenPerSec * Time.deltaTime;
            if (_hpRegenAccum >= 1f)
            {
                int regen = Mathf.FloorToInt(_hpRegenAccum);
                _health.Heal(regen);
                _hpRegenAccum -= regen;
            }
        }

        // Stun override
        if (isStunned || isFrozen)
        {
            _rb.velocity = Vector2.zero;
            if (_anim) _anim.SetBool("isMoving", false);
            return;
        }

        RefreshPlayerTarget();
        RunStateMachine();
    }

    private void RunStateMachine()
    {
        float dist = _player != null
            ? Vector2.Distance(transform.position, _player.position)
            : float.MaxValue;

        if (dist <= detectionRange)
        {
            if (dist <= attackRange)
                _state = State.Attack;
            else
                _state = State.Chase;
        }
        else
        {
            _state = State.Patrol;
        }

        switch (_state)
        {
            case State.Patrol: DoPatrol(); break;
            case State.Chase:  DoChase();  break;
            case State.Attack: DoAttack(); break;
        }
    }

    // ══════════════════════════════════════════════
    // Patrol
    // ══════════════════════════════════════════════

    private void DoPatrol()
    {
        if (leftPoint == null || rightPoint == null) return;

        float target = _facingRight ? rightPoint.position.x : leftPoint.position.x;
        float dir    = _facingRight ? 1f : -1f;

        _rb.velocity = new Vector2(dir * moveSpeed, _rb.velocity.y);
        SetFacing(dir > 0);

        if (_anim) _anim.SetBool("isMoving", true);

        // Đổi hướng khi đến biên
        float dist = Mathf.Abs(transform.position.x - target);
        if (dist < 0.2f)
            _facingRight = !_facingRight;
    }

    // ══════════════════════════════════════════════
    // Chase
    // ══════════════════════════════════════════════

    private void DoChase()
    {
        if (_player == null) return;

        Vector2 dir = (_player.position - transform.position).normalized;
        _rb.velocity = new Vector2(dir.x * chaseSpeed, _rb.velocity.y);
        SetFacing(dir.x > 0);

        if (_anim) _anim.SetBool("isMoving", true);
    }

    // ══════════════════════════════════════════════
    // Attack
    // ══════════════════════════════════════════════

    private void DoAttack()
    {
        _rb.velocity = Vector2.zero;
        if (_anim) _anim.SetBool("isMoving", false);

        if (Time.time - _lastAttack < attackCooldown) return;
        _lastAttack = Time.time;

        if (_anim) _anim.SetTrigger("attack");

        if (hitbox != null)
            StartCoroutine(EnableHitboxBriefly(0.15f, 0.3f));
    }

    private IEnumerator EnableHitboxBriefly(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        if (hitbox) hitbox.enabled = true;
        yield return new WaitForSeconds(duration);
        if (hitbox) hitbox.enabled = false;
    }

    // ══════════════════════════════════════════════
    // Damage calculation with resistances
    // ══════════════════════════════════════════════

    /// <summary>
    /// Gọi từ bên ngoài (e.g. PlayerCombat) để gây damage có tính kháng nguyên tố.
    /// element: 0=none, 1=Hoa, 2=Thuy, 3=Tho, 4=Moc, 5=Kim, 6=Phong
    /// </summary>
    public void TakeDamageWithElement(int rawDamage, int element = 0)
    {
        // Evasion check
        if (evasionRate > 0 && UnityEngine.Random.Range(0f, 100f) < evasionRate)
        {
            ShowFloatingText("Miss!");
            return;
        }

        float resist = GetResistance(element);
        int   actual = Mathf.Max(1, Mathf.RoundToInt(rawDamage * (1f - resist / 100f)));

        // Weaken: nhận thêm 30% sát thương
        if (isWeakened) actual = Mathf.RoundToInt(actual * 1.3f);

        _health.TakeDamage(actual);

        // Counter check
        if (counterRate > 0 && UnityEngine.Random.Range(0f, 100f) < counterRate)
            StartCoroutine(CounterAttack());
    }

    private float GetResistance(int element)
    {
        return element switch
        {
            1 => khangHoa,
            2 => khangThuy,
            3 => khangTho,
            4 => khangMoc,
            5 => khangKim,
            6 => khangPhong,
            _ => 0f
        };
    }

    private IEnumerator CounterAttack()
    {
        yield return new WaitForSeconds(0.2f);
        if (_player == null || _state == State.Dead) yield break;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist > attackRange * 1.5f) yield break;

        // Kiểm tra cùng map — không phản đòn player ở map khác
        int myMapId = GetComponent<ZoneOwnerTag>()?.MapId ?? -999;
        if (myMapId != -999)
        {
            var registry = ZoneRoomRegistry.Instance;
            var netObj = _player.GetComponent<Unity.Netcode.NetworkObject>();
            if (registry != null && netObj != null)
            {
                var room = registry.GetClientRoom(netObj.OwnerClientId);
                if (room != null && room.MapId != myMapId) yield break;
            }
        }

        var nph = _player.GetComponent<NetworkPlayerHealth>();
        if (nph != null)
        {
            int counterDmg = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.6f));
            nph.TakeDamage(counterDmg);
            Debug.Log($"[MobAI] Counter! {counterDmg} dmg");
            yield break;
        }
        var ph = _player.GetComponent<PlayerHealth>();
        if (ph != null)
        {
            int counterDmg = Mathf.Max(1, Mathf.RoundToInt(baseDamage * 0.6f));
            ph.TakeDamage(counterDmg);
            Debug.Log($"[MobAI] Counter! {counterDmg} dmg");
        }
    }

    // ══════════════════════════════════════════════
    // Status effects API
    // ══════════════════════════════════════════════

    public void ApplyStun(float duration)    => StartCoroutine(StunTimer(duration));
    public void ApplyFreeze(float duration)  => StartCoroutine(FreezeTimer(duration));
    public void ApplyWeaken(float duration)  => StartCoroutine(WeakenTimer(duration));

    private IEnumerator StunTimer(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    private IEnumerator FreezeTimer(float duration)
    {
        isFrozen = true;
        yield return new WaitForSeconds(duration);
        isFrozen = false;
    }

    private IEnumerator WeakenTimer(float duration)
    {
        isWeakened = true;
        yield return new WaitForSeconds(duration);
        isWeakened = false;
    }

    // ══════════════════════════════════════════════
    // Events
    // ══════════════════════════════════════════════

    private void OnTakeDamage()
    {
        if (_anim) _anim.SetTrigger("hit");
    }

    private void OnDeath()
    {
        _state = State.Dead;
        _rb.velocity = Vector2.zero;
        if (_anim) _anim.SetTrigger("die");
        if (hitbox) hitbox.enabled = false;
    }

    // ══════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════

    private void FindNearestPlayer()
    {
        float nearest = float.MaxValue;
        int myMapId = GetComponent<ZoneOwnerTag>()?.MapId ?? -999;
        var registry = ZoneRoomRegistry.Instance;

        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            // Lọc cùng map — bỏ qua player ở map khác
            if (registry != null && myMapId != -999)
            {
                var netObj = go.GetComponent<Unity.Netcode.NetworkObject>();
                if (netObj != null)
                {
                    var room = registry.GetClientRoom(netObj.OwnerClientId);
                    if (room == null || room.MapId != myMapId) continue;
                }
            }

            float d = Vector2.Distance(transform.position, go.transform.position);
            if (d < nearest) { nearest = d; _player = go.transform; }
        }
    }

    private void RefreshPlayerTarget()
    {
        if (_player != null) return;
        FindNearestPlayer();
    }

    private void SetFacing(bool right)
    {
        if (_facingRight == right) return;
        _facingRight = right;
        Vector3 s = transform.localScale;
        s.x = right ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
        transform.localScale = s;
    }

    private void CreateAutoPatrolPoints(float halfRange)
    {
        var L = new GameObject($"{gameObject.name}_Left");
        var R = new GameObject($"{gameObject.name}_Right");
        L.transform.position = transform.position + Vector3.left  * halfRange;
        R.transform.position = transform.position + Vector3.right * halfRange;
        leftPoint  = L.transform;
        rightPoint = R.transform;
    }

    private void ShowFloatingText(string text)
    {
        // Hook cho floating text "Miss!" — implement với FloatingTextManager nếu có
        Debug.Log($"[MobAI] {gameObject.name}: {text}");
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (leftPoint)  { Gizmos.color = Color.cyan; Gizmos.DrawSphere(leftPoint.position,  0.2f); }
        if (rightPoint) { Gizmos.color = Color.cyan; Gizmos.DrawSphere(rightPoint.position, 0.2f); }
    }
#endif
}
