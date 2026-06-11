using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// Điều khiển toàn bộ hành vi boss: tìm player, di chuyển, đánh thường, dùng kỹ năng,
// né đòn, hồi máu, phản sát thương và đồng bộ logic khi chạy bằng Unity Netcode.

[RequireComponent(typeof(Rigidbody2D), typeof(Animator))]
public class BossController : MonoBehaviour
{
    // Chứa toàn bộ chỉ số và bật/tắt kỹ năng của boss, được cấu hình bằng ScriptableObject.
    [Header("Config (ScriptableObject)")]
    public BossData data;

    // Prefab kỹ năng có thể gán trực tiếp tại Inspector nếu BossData chưa gán.
    [Header("Skill Prefabs (override nếu BossData chưa có)")]
    [Tooltip("Prefab hỏa cầu")]
    public GameObject fireballPrefab;
    [Tooltip("Prefab tia sét")]
    public GameObject lightningPrefab;

    // Điểm và bán kính kiểm tra mặt đất để boss biết khi nào có thể nhảy lại.
    [Header("Ground Check")]
    public Transform groundCheck;
    [Tooltip("Radius overlap circle để kiểm tra ground")]
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // Component được cache để tránh gọi GetComponent liên tục trong lúc AI cập nhật.
    private Rigidbody2D  _rb;
    private Animator     _anim;
    private NetworkBossHealth _netHealth;   // Xử lý máu khi boss chạy trong phòng network.
    private EnemyHealth       _localHealth; // Xử lý máu khi boss chạy offline hoặc scene không dùng network.

    private Transform _target;  // Player hiện tại mà boss đang đuổi theo hoặc tấn công.

    // Trạng thái hiện tại quyết định boss đang đứng yên, đuổi, đánh, né, ẩn thân hay đã chết.
    private enum BossState { Idle, Chase, Attacking, Dodging, Stealthed, Dead }
    private BossState _state = BossState.Idle;

    // Bộ đếm hồi chiêu riêng cho từng hành động để boss không spam cùng một kỹ năng.
    private float _normalAttackCooldown = 0f;
    private float _fireballCooldown     = 0f;
    private float _lightningCooldown    = 0f;
    private float _stealthCooldown      = 0f;
    private float _dodgeCooldown        = 0f;

    // Theo dõi boss có đang chạm đất không và còn bao nhiêu lần nhảy.
    private bool _isGrounded = false;
    private int  _jumpsLeft  = 0;

    // Dữ liệu dự phòng cho logic bay nếu sau này cần xử lý theo mốc spawn hoặc bật/tắt fly mode.
    private float _groundBaseY;     // Tọa độ Y lúc spawn, hiện chỉ được khởi tạo làm mốc tham chiếu.
    private bool  _flyModeActive;   // Cờ fly mode dự phòng, hiện chưa tham gia luồng di chuyển.

    // Tích lũy lượng hồi máu theo thời gian, đủ 1 HP thì mới gọi hàm heal.
    private float _regenAccum = 0f;

    // Theo dõi trạng thái ẩn thân và toàn bộ SpriteRenderer cần đổi alpha.
    private bool _isStealthed = false;
    private SpriteRenderer[] _renderers;

    // Hash tham số Animator để set animation nhanh hơn và tránh sai tên string lặp lại.
    private static readonly int AnimIsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int AnimIsMoving    = Animator.StringToHash("isMoving");
    private static readonly int AnimIsGrounded  = Animator.StringToHash("isGrounded");
    private static readonly int AnimJump        = Animator.StringToHash("Jump");

    // Hướng mặt hiện tại của boss, dùng để flip sprite và xác định hướng đánh/né.
    private bool _facingRight = true;

    // Lấy component cần dùng và khóa xoay Rigidbody2D để boss không bị lật khi va chạm.

    private void Awake()
    {
        _rb         = GetComponent<Rigidbody2D>();
        _anim       = GetComponent<Animator>();
        _netHealth  = GetComponent<NetworkBossHealth>();
        _localHealth = GetComponent<EnemyHealth>();
        _renderers  = GetComponentsInChildren<SpriteRenderer>(true);

        // Giữ boss luôn đứng thẳng trong gameplay 2D.
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    // Kiểm tra dữ liệu cấu hình, khởi tạo chỉ số ban đầu và đăng ký event máu.
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

        // Chỉ máy chạy AI mới quét player định kỳ để chọn mục tiêu.
        if (ShouldRunAI())
            InvokeRepeating(nameof(RefreshTarget), 0f, 1f);

        // Khi dùng network, boss can thiệp trước/sau lúc nhận damage để né và phản damage.
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

    // Chỉ cho phép AI chạy trên server; nếu không có NetworkManager thì xem như chạy offline.
    private bool ShouldRunAI()
    {
        return NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
    }

    // Mỗi frame cập nhật hồi chiêu, hồi máu, kiểm tra mặt đất và chạy state machine.
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

    // Chọn hành động chính của boss dựa trên trạng thái, mục tiêu, khoảng cách và hồi chiêu.
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

        // Quay mặt về phía player trước khi quyết định di chuyển hoặc tấn công.
        FaceTarget();

        // Boss bay sẽ bám theo vị trí player với offset độ cao thay vì chạy trên mặt đất.
        if (data.canFly)
        {
            HandleFlyMovement();
        }

        // Kỹ năng đặc biệt có độ ưu tiên cao hơn đánh thường nếu đã hết hồi chiêu.
        if (TryUseSpecialSkill()) return;

        // Khi player vào tầm đánh gần và đòn thường đã hồi, boss thực hiện combo đánh thường.
        if (dist <= data.meleeAttackRange && _normalAttackCooldown <= 0f)
        {
            DoNormalAttack();
            return;
        }

        // Boss không bay thì chạy bộ về phía player.
        if (!data.canFly)
        {
            ChaseTarget(dist);
        }
    }

    // Di chuyển boss trên trục X về phía player và nhảy nếu player đang ở cao hơn.
    private void ChaseTarget(float dist)
    {
        _state = BossState.Chase;
        float dir = _target.position.x > transform.position.x ? 1f : -1f;
        _rb.velocity = new Vector2(dir * data.chaseSpeed, _rb.velocity.y);
        SetMovingAnim(true);

        // Nếu boss được phép nhảy và player cao hơn, boss nhảy để tiếp cận mục tiêu.
        if (data.canJump && _isGrounded && _jumpsLeft > 0)
        {
            float heightDiff = _target.position.y - transform.position.y;
            if (heightDiff > 0.8f)
            {
                PerformJump();
            }
        }
    }

    // Di chuyển boss bay đến vị trí ngay phía trên player và tắt trọng lực khi đang bay.
    private void HandleFlyMovement()
    {
        if (_target == null) return;

        // Vị trí bay mong muốn là cùng X với player và cao hơn player theo flyHeight.
        float targetX = _target.position.x;
        float targetY = _target.position.y + data.flyHeight;
        Vector2 flyTarget = new Vector2(targetX, targetY);
        Vector2 dir = (flyTarget - (Vector2)transform.position).normalized;
        _rb.velocity = dir * data.flySpeed;
        _rb.gravityScale = 0f;
        SetMovingAnim(true);
    }

    // Tiêu hao một lượt nhảy, đẩy Rigidbody2D lên trên và kích hoạt animation nhảy.
    private void PerformJump()
    {
        _jumpsLeft--;
        _rb.velocity = new Vector2(_rb.velocity.x, data.jumpForce);
        _anim.SetTrigger(AnimJump);
    }

    // Kiểm tra boss có chạm groundLayer không, reset lượt nhảy khi vừa tiếp đất.
    private void DoGroundCheck()
    {
        if (groundCheck == null) return;
        bool wasGrounded = _isGrounded;
        _isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (_isGrounded && !wasGrounded)
            _jumpsLeft = data.maxJumps; // Khi vừa chạm đất, boss được nạp lại số lần nhảy tối đa.

        if (_anim != null)
            _anim.SetBool(AnimIsGrounded, _isGrounded);
    }

    // Thử kích hoạt một kỹ năng đặc biệt theo thứ tự ưu tiên: ẩn thân, sét, hỏa cầu.
    private bool TryUseSpecialSkill()
    {
        // Ẩn thân được ưu tiên cao nhất vì nó đổi trạng thái phòng thủ/tiếp cận của boss.
        if (data.stealth.enabled && _stealthCooldown <= 0f && !_isStealthed)
        {
            StartCoroutine(DoStealth());
            return true;
        }

        // Sét tạo nhiều tia đánh theo hàng ngang quanh vị trí player.
        if (data.lightning.enabled && _lightningCooldown <= 0f && _target != null)
        {
            StartCoroutine(DoLightningStrike());
            return true;
        }

        // Hỏa cầu mưa spawn projectile rơi xuống quanh player.
        if (data.fireballRain.enabled && _fireballCooldown <= 0f && _target != null)
        {
            StartCoroutine(DoFireballRain());
            return true;
        }

        return false;
    }

    // Bắt đầu đòn đánh thường và đặt hồi chiêu cho lần đánh tiếp theo.
    private void DoNormalAttack()
    {
        _state = BossState.Attacking;
        _normalAttackCooldown = data.normalAttack.cooldown;
        StartCoroutine(NormalAttackCoroutine());
    }

    // Chạy timing của đòn đánh thường: bật animation, chờ khung đánh, quét hitbox rồi gây damage.
    private IEnumerator NormalAttackCoroutine()
    {
        SetAttackAnim(true);
        _rb.velocity = Vector2.zero;

        // Chờ tới thời điểm va chạm của animation trước khi kiểm tra trúng đòn.
        yield return new WaitForSeconds(0.25f);

        // Dùng layer player được cấu hình; nếu chưa gán thì fallback sang layer tên "Player".
        LayerMask mask = data.normalAttack.playerLayer != 0
            ? data.normalAttack.playerLayer
            : LayerMask.GetMask("Player");

        // Tạo vùng đánh phía trước boss dựa trên hướng đang nhìn.
        Vector2 attackCenter = transform.position + Vector3.right * (_facingRight ? data.normalAttack.range : -data.normalAttack.range);
        Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, attackCenter, data.normalAttack.range, mask.value);

        Debug.Log($"[BossController] NormalAttack hits={hits.Length} center={attackCenter} range={data.normalAttack.range}");

        foreach (var hit in hits)
        {
            DealDamageToPlayer(hit, data.normalAttack.damage);

            // Nếu player có Rigidbody2D thì đẩy bật ra khỏi boss sau khi nhận damage.
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

    // Spawn nhiều hỏa cầu ở phía trên player với vị trí X ngẫu nhiên trong spreadRadius.
    private IEnumerator DoFireballRain()
    {
        _state = BossState.Attacking;
        _fireballCooldown = data.fireballRain.cooldown;
        SetAttackAnim(true);

        // Ưu tiên prefab trong BossData, nếu không có thì dùng prefab override trên controller.
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
            // Mỗi hỏa cầu rơi từ trên xuống gần player để tạo vùng nguy hiểm ngẫu nhiên.
            float offsetX = Random.Range(-data.fireballRain.spreadRadius, data.fireballRain.spreadRadius);
            Vector3 spawnPos = new Vector3(
                _target.position.x + offsetX,
                _target.position.y + data.fireballRain.spawnHeight,
                transform.position.z);

            // Truyền damage và tốc độ rơi cho script projectile nếu prefab có component này.
            GameObject fb = Instantiate(prefab, spawnPos, Quaternion.identity);
            var comp = fb.GetComponent<BossFireball>();
            if (comp != null)
                comp.Init(data.fireballRain.damage, data.fireballRain.fallSpeed);

            // Nếu prefab có NetworkObject và đang ở server, spawn để client khác nhìn thấy.
            var netObj = fb.GetComponent<NetworkObject>();
            if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                netObj.Spawn(true);

            yield return new WaitForSeconds(0.3f);
        }

        yield return new WaitForSeconds(0.5f);
        SetAttackAnim(false);
        _state = BossState.Chase;
    }

    // Spawn một dãy tia sét theo hàng ngang, bắt đầu quanh vị trí player hiện tại.
    private IEnumerator DoLightningStrike()
    {
        _state = BossState.Attacking;
        _lightningCooldown = data.lightning.cooldown;
        SetAttackAnim(true);
        _rb.velocity = Vector2.zero;

        // Ưu tiên prefab trong BossData, nếu không có thì dùng prefab override trên controller.
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

        // Canh dãy sét sao cho player nằm gần giữa cụm bolt.
        float startX = _target != null ? _target.position.x - (data.lightning.boltCount / 2f) * data.lightning.boltSpacing : transform.position.x;

        for (int i = 0; i < data.lightning.boltCount; i++)
        {
            // Mỗi bolt được đặt cách nhau theo boltSpacing và spawn phía trên boss.
            float bx = startX + i * data.lightning.boltSpacing;
            Vector3 spawnPos = new Vector3(bx, transform.position.y + 4f, transform.position.z);

            // Truyền damage, thời gian tồn tại và thời gian stun cho script tia sét.
            GameObject bolt = Instantiate(prefab, spawnPos, Quaternion.identity);
            var comp = bolt.GetComponent<BossLightningBolt>();
            if (comp != null)
                comp.Init(data.lightning.damage, data.lightning.boltDuration, data.lightning.stunDuration);

            // Nếu prefab có NetworkObject và đang ở server, spawn để đồng bộ qua network.
            var netObj = bolt.GetComponent<NetworkObject>();
            if (netObj != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                netObj.Spawn(true);

            yield return new WaitForSeconds(data.lightning.boltDelay);
        }

        yield return new WaitForSeconds(0.6f);
        SetAttackAnim(false);
        _state = BossState.Chase;
    }

    // Làm boss mờ đi trong một khoảng thời gian và vẫn cho phép tiếp tục áp sát player.
    private IEnumerator DoStealth()
    {
        _state = BossState.Stealthed;
        _stealthCooldown = data.stealth.cooldown;
        _isStealthed = true;

        // Ẩn thân không phải animation tấn công, nên chỉ đổi alpha sprite thay vì bật isAttacking.
        SetRendererAlpha(data.stealth.stealthAlpha);

        // Trong thời gian ẩn thân, boss dưới đất vẫn chạy theo player để áp sát.
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

        // Hết thời gian ẩn thân thì đưa alpha về bình thường và quay lại trạng thái chase.
        _isStealthed = false;
        SetRendererAlpha(1f);
        _state = BossState.Chase;
    }

    // Kiểm tra xác suất né khi boss sắp nhận damage; né thành công thì damage bị hủy.
    public bool TryDodge()
    {
        if (_state == BossState.Dead) return false;
        if (_dodgeCooldown > 0f) return false;
        if (data == null || data.dodgeChance <= 0f) return false;

        float roll = Random.Range(0f, 100f);
        if (roll > data.dodgeChance) return false;

        // Né thành công thì đặt hồi chiêu né và chạy animation/di chuyển né.
        _dodgeCooldown = data.dodgeCooldown;
        StartCoroutine(DodgeCoroutine());
        return true;
    }

    // Đẩy boss lùi về phía sau trong thời gian ngắn rồi trả về trạng thái chase.
    private IEnumerator DodgeCoroutine()
    {
        _state = BossState.Dodging;
        _anim.SetTrigger("Dodge");

        float dodgeDir = _facingRight ? -1f : 1f; // Boss luôn né lùi về hướng ngược với hướng đang nhìn.
        _rb.velocity = new Vector2(dodgeDir * data.dodgeDistance * 4f, _rb.velocity.y);

        yield return new WaitForSeconds(0.35f);

        _rb.velocity = new Vector2(0f, _rb.velocity.y);
        _state = BossState.Chase;
    }

    // Xử lý damage trước khi trừ máu: boss có thể né, nếu không thì giảm damage theo kháng hệ.
    public int HandleBeforeTakeDamage(int rawDamage, string elementType, ulong attackerClientId)
    {
        if (_state == BossState.Dead) return 0;

        // Né thành công thì trả về 0 để hệ thống máu không trừ HP.
        if (TryDodge()) return 0;

        // Không né được thì lấy kháng theo hệ đòn đánh và tính damage cuối.
        int resist = GetResistance(elementType);
        return DamageCalculator.CalcBossReceivedDamage(rawDamage, resist);
    }

    // Sau khi nhận damage, boss phản lại một lượng damage cố định cho người đã đánh nếu được bật.
    public void HandleAfterTakeDamage(int finalDamage, ulong attackerClientId)
    {
        if (!data.returnDamageEnabled || data.returnDamageAmount <= 0) return;
        ReturnDamageToPlayer(attackerClientId, data.returnDamageAmount);
    }

    private void OnLocalTakeDamage()
    {
        // Chế độ offline không có thông tin hệ đòn đánh, nên chỉ chạy logic né.
        TryDodge();
    }

    // Chuyển boss sang trạng thái chết, dừng di chuyển/coroutine và tắt controller.
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

    // Hồi máu theo giây khi HP hiện tại thấp hơn hoặc bằng ngưỡng phần trăm được cấu hình.
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

    // Lấy máu tối đa từ hệ thống network, offline hoặc BossData tùy component đang có.
    private int GetMaxHp()
    {
        if (_netHealth != null) return _netHealth.GetMaxHealth();
        if (_localHealth != null) return _localHealth.GetMaxHealth();
        return data.maxHealth;
    }

    // Lấy máu hiện tại từ hệ thống network hoặc offline; trả 0 nếu không có component máu.
    private int GetCurrentHp()
    {
        if (_netHealth != null) return _netHealth.GetCurrentHealth();
        if (_localHealth != null) return _localHealth.GetCurrentHealth();
        return 0;
    }

    // Gọi đúng hàm hồi máu theo loại health component đang được dùng.
    private void HealBoss(int amount)
    {
        if (_netHealth != null) _netHealth.HealServer(amount);
        else if (_localHealth != null) _localHealth.Heal(amount);
    }

    // Gây damage cho player trúng hitbox, ưu tiên health network rồi fallback sang health offline.
    private void DealDamageToPlayer(Collider2D col, int dmg)
    {
        var netPH = col.GetComponentInParent<NetworkPlayerHealth>();
        if (netPH != null) { netPH.TakeDamage(dmg); return; }
        var ph = col.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(dmg);
    }

    // Tìm player theo clientId và gây damage phản lại thông qua NetworkPlayerHealth.
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

    // Đổi tên hệ đòn đánh sang chỉ số kháng tương ứng trong BossData.
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

    // Quét tất cả PlayerController trong scene và chọn player gần nhất trong vùng phát hiện mở rộng.
    private void RefreshTarget()
    {
        if (!ShouldRunAI()) return;

        float bestDist = float.MaxValue;
        Transform best = null;

        // Chỉ nhận mục tiêu nằm trong 1.5 lần detectionRange để boss không bám quá xa.
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

    // Flip localScale.x để sprite boss luôn quay mặt về phía player hiện tại.
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

    // Bật/tắt bool isAttacking trên Animator nếu boss có Animator hợp lệ.
    private void SetAttackAnim(bool state)
    {
        if (_anim != null) _anim.SetBool(AnimIsAttacking, state);
    }

    // Bật/tắt bool isMoving trên Animator nếu boss có Animator hợp lệ.
    private void SetMovingAnim(bool state)
    {
        if (_anim != null) _anim.SetBool(AnimIsMoving, state);
    }

    // Đổi alpha của toàn bộ SpriteRenderer con, dùng cho hiệu ứng ẩn thân.
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

    // Vẽ vùng phát hiện, vùng đánh gần và vòng kiểm tra ground khi chọn boss trong Editor.
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
