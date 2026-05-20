using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(NetworkAnimator))]
public class EnemyAI : MonoBehaviour
{
    [Serializable]
    public class EnemyProjectilePrefabData
    {
        public string key;
        public GameObject prefab;
    }

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

    [Header("Projectile Skills")]
    [Tooltip("Điểm bắn projectile. Để trống sẽ dùng vị trí enemy + offset trong skill config.")]
    public Transform projectileSpawnPoint;
    [Tooltip("Map key trong skill.projectile_prefab_key -> projectile prefab thực tế.")]
    public List<EnemyProjectilePrefabData> projectilePrefabs = new List<EnemyProjectilePrefabData>();

    [Header("Flight")]
    [Tooltip("Bật nếu quái có thể bay/di chuyển theo cả trục X và Y (dơi, rồng...). Tắt = chỉ di chuyển ngang. Độc lập với projectile.")]
    [FormerlySerializedAs("allowProjectileFlight")]
    public bool canFly = false;

    [Header("Projectile Movement")]
    [Tooltip("Khoảng cách tối thiểu để quái bắn xa không đứng sát player.")]
    public float projectileKeepDistance = 1.6f;
    [Tooltip("Tốc độ đuổi theo trục Y của quái có projectile.")]
    public float projectileVerticalMoveSpeed = 2f;
    [Tooltip("Khoảng dò ground bên dưới để quyết định có được đi xuống xuyên platform hay không.")]
    public float projectileGroundSearchDistance = 14f;
    [Tooltip("Thời gian bỏ qua collision với one-way ground khi quái ranged đi xuống.")]
    public float projectileFallThroughDuration = 0.35f;
    [Tooltip("Đẩy nhẹ enemy xuống dưới trước khi rơi qua platform.")]
    public float projectileFallThroughDrop = 0.12f;
    [Tooltip("Bắn vượt qua vị trí player một chút để projectile vẫn thấy đường bay khi đứng gần.")]
    public float projectileAimOvershoot = 0.45f;

    [Header("Patrol")]
    [Tooltip("Random hướng tuần tra ban đầu để enemy không chạy đồng loạt cùng một hướng khi vừa spawn.")]
    public bool randomizeInitialPatrolDirection = true;

    [Header("Server Virtual Ground (non-fly)")]
    [Tooltip("Trên dedicated server không có ground collider của mọi map. Non-fly enemy sẽ dùng spawn_y làm 'virtual ground' — khoá Y tại vị trí spawn để không rơi mãi.")]
    public bool useServerVirtualGround = true;

    [Header("Ground Physics Fail-Safe")]
    [Tooltip("Snap enemy xuống ground hiện có ngay sau khi spawn để bắt đầu đúng mặt đất.")]
    public bool snapToGroundOnStart = true;
    [Tooltip("Khoảng raycast tìm ground bên dưới khi vừa spawn hoặc khi recover khỏi fall.")]
    public float spawnGroundSnapDistance = 12f;
    [Tooltip("Offset nhỏ đẩy enemy lên trên mặt ground sau khi snap.")]
    public float spawnGroundOffset = 0.02f;
    [Tooltip("Nếu enemy rơi xa hơn mức này khỏi ground cuối cùng thì sẽ recover thay vì rơi vô hạn.")]
    public float maxUnsupportedFallDistance = 8f;
    [Tooltip("Nếu enemy rơi quá lâu mà không chạm ground thì sẽ recover.")]
    public float maxUnsupportedFallTime = 1.25f;

    [Header("Ground Debug")]
    [Tooltip("Bật log debug cho movement/ground của enemy không bay trên server.")]
    public bool debugGroundMovement = true;
    [Tooltip("Throttle log ground để tránh spam quá dày.")]
    public float debugGroundLogInterval = 0.5f;

    [Header("Fly Patrol (canFly)")]
    [Tooltip("Bán kính patrol trục X cho enemy bay khi chưa thấy player.")]
    public float flyPatrolRadiusX = 3f;
    [Tooltip("Bán kính patrol trục Y cho enemy bay khi chưa thấy player (lên/xuống quanh vị trí spawn).")]
    public float flyPatrolRadiusY = 2f;
    [Tooltip("Mỗi bao nhiêu giây sẽ random một điểm patrol 2D mới khi enemy bay đi tuần.")]
    public float flyPatrolRetargetSeconds = 2.5f;

    private Transform player;
    private Rigidbody2D rb;
    private NetworkAnimator networkAnimator; // Dùng NetworkAnimator thay vì Animator
    private Animator animator; // Lấy từ NetworkAnimator.Animator
    private EnemyHealth health;
    private NetworkEnemyController networkController;
    private Collider2D bodyCollider;

    private bool facingRight = true;
    private float lastAttackTime;
    private bool autoPatrolPointsCreated = false;
    private float attackStartTime;
    private float _findPlayerTimer = 0f; // timer tìm lại player
    private float _retargetTimer   = 0f; // timer retarget player gần nhất
    private const float MAX_ATTACK_DURATION = 2f;
    private const string DefaultAttackBoolParameter = "isAttacking";
    private const string LegacyAttackTriggerParameter = "Attack";
    private string _activeAttackBoolParameter;
    private bool _initialPatrolDirectionInitialized;

    // Server virtual ground / fly patrol state
    private float _serverAnchorY;
    private float _serverAnchorX;
    private bool _serverAnchorSet;
    private Vector2 _flyPatrolTarget;
    private float _flyPatrolRetargetTimer;
    private float _initialGravityScale = 1f;
    private Vector2 _lastSupportedGroundPosition;
    private bool _hasSupportedGroundPosition;
    private float _unsupportedFallStartTime = -1f;
    private float _nextGroundDebugLogTime;
    private bool _lastGroundedEnough = true;
    private bool _lastVirtualGroundMode;
    // Bật khi raycast spawn không tìm thấy collider Ground nào trong scene
    // (map server không có ground thật) — chuyển enemy sang chế độ virtual ground:
    // tắt gravity và khóa Y theo anchor để vẫn di chuyển trục X bình thường.
    private bool _forceVirtualGround;

    // Skill system — set bởi HostSpawnConfigLoader sau khi spawn
    private EnemySkillSet _skillSet;
    private readonly Dictionary<string, GameObject> _projectilePrefabLookup
        = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
    private Coroutine _projectileFallThroughCoroutine;
    private Collider2D _ignoredGroundCollider;
    private int _groundLayerMask;
    private bool _hasMapBounds;
    private float _mapMinX, _mapMaxX, _mapMinY, _mapMaxY;

    private enum State { Run, MeleeAttack, Dead }
    private State state = State.Run;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Cho phép enemy đi xuyên nhau (không va chạm giữa các enemy cùng layer)
        Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
        animator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        health = GetComponent<EnemyHealth>();
        networkController = GetComponent<NetworkEnemyController>();
        bodyCollider = GetComponent<Collider2D>();
        _initialGravityScale = rb != null && rb.gravityScale > 0f ? rb.gravityScale : 1f;
        _originalMoveSpeed = moveSpeed;
        _skillSet = GetComponent<EnemySkillSet>(); // có thể null nếu chưa được gán
        _groundLayerMask = LayerMask.GetMask("Ground");
        BuildProjectileLookup();

        // Đảm bảo NetworkAnimator luôn có Animator – tránh NullRef trong CheckParametersChanged
        if (networkAnimator != null && networkAnimator.Animator == null)
            networkAnimator.Animator = animator;

        // Preserve the rigidbody's configured gravity; canFly only changes movement behavior.
        if (rb != null && !canFly)
        {
            rb.gravityScale = _initialGravityScale;
        }

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
        InitializeInitialPatrolDirection();
        DetectMapBounds();

        // Ghi nhớ vị trí spawn làm anchor cho server-side virtual ground (non-fly)
        // và tâm patrol 2D (fly).
        if (!_serverAnchorSet)
        {
            _serverAnchorX = transform.position.x;
            _serverAnchorY = transform.position.y;
            _serverAnchorSet = true;
        }

        if (!canFly)
            LogGroundMovement("Start", $"anchorSet={_serverAnchorSet} anchorY={_serverAnchorY:F2}", true);

        // Mỗi enemy bay có một patrol target ban đầu khác nhau → mỗi con bay 1 hướng khác.
        if (canFly)
            RetargetFlyPatrol();
        else if (UsesRealGroundPhysics() && snapToGroundOnStart)
            StartCoroutine(SnapToGroundAfterSpawn());
    }

    public void ApplyRuntimeOverride(int attackDamage, float movementSpeed, bool enableFlight)
    {
        if (attackDamage > 0)
            damage = attackDamage;

        if (movementSpeed > 0f)
        {
            moveSpeed = movementSpeed;
            _originalMoveSpeed = movementSpeed;
        }

        canFly = enableFlight;
        if (rb != null && !enableFlight && !_forceVirtualGround)
            rb.gravityScale = _initialGravityScale;

        if (enableFlight)
            _unsupportedFallStartTime = -1f;
    }

    // ── Freeze (DebuffManager gọi qua ClientRpc) ─────────────────────────────
    private bool _isFrozen;

    public void ApplyFreeze(float duration)
    {
        _isFrozen = true;
        // Auto-unfreeze sau duration (fallback nếu DebuffManager không gọi RemoveFreeze đúng lúc)
        CancelInvoke(nameof(RemoveFreeze));
        Invoke(nameof(RemoveFreeze), duration);
    }

    public void RemoveFreeze()
    {
        _isFrozen = false;
    }

    /// <summary>DebuffManager.GetSlowFactor() hoặc IsFrozen() không accessible trực tiếp từ đây
    /// vì EnemyAI có thể chạy trên client. Thay vào đó, DebuffManager gọi ApplyFreeze để tắt movement.</summary>
    public bool IsFrozen => _isFrozen;

    // ── Map isolation helpers ────────────────────────────────────────────────

    /// <summary>Trả về MapId của enemy này (qua ZoneOwnerTag). -999 nếu chưa gắn tag.</summary>
    private int GetMyMapId()
    {
        var tag = GetComponent<ZoneOwnerTag>();
        return tag != null ? tag.MapId : -999;
    }

    /// <summary>
    /// Kiểm tra một player Transform có cùng map với enemy này không.
    /// Dùng ZoneRoomRegistry để tra map của client sở hữu NetworkObject của player.
    /// </summary>
    private bool IsSameMapAsTarget(Transform targetTransform)
    {
        if (targetTransform == null) return false;
        var registry = ZoneRoomRegistry.Instance;
        if (registry == null) return true; // registry chưa init, cho qua
        var netObj = targetTransform.GetComponent<NetworkObject>();
        if (netObj == null) return false;
        var room = registry.GetClientRoom(netObj.OwnerClientId);
        if (room == null) return false;
        return GetMyMapId() == room.MapId;
    }

    // ── End map isolation helpers ─────────────────────────────────────────────

    private void FindPlayerInNetwork()
    {
        int myMapId = GetMyMapId();
        var registry = ZoneRoomRegistry.Instance;

        // Tìm player theo tag "Player", lọc cùng map
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

                // Kiểm tra cùng map — bỏ qua player ở map khác
                if (registry != null && myMapId != -999)
                {
                    var netObj = obj.GetComponent<NetworkObject>();
                    if (netObj != null)
                    {
                        var room = registry.GetClientRoom(netObj.OwnerClientId);
                        if (room == null || room.MapId != myMapId) continue;
                    }
                }

                float d = Vector2.Distance(transform.position, obj.transform.position);
                if (d < bestDist) { bestDist = d; best = nph; bestTr = obj.transform; }
            }
            if (bestTr != null) { player = bestTr; return; }
        }

        // Fallback: tìm PlayerController bất kỳ trong scene (chỉ standalone / không có registry)
        if (registry == null)
        {
            var ctrl = UnityEngine.Object.FindObjectOfType<PlayerController>();
            if (ctrl != null) player = ctrl.transform;
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
            // Unfreeze X luôn (enemy cần di chuyển)
            if (rb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionX))
            {
                rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
            }
            // ✅ FIX: Unfreeze Y nếu enemy bay (canFly) — tránh bị stuck dọc
            if (canFly && rb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionY))
            {
                rb.constraints &= ~RigidbodyConstraints2D.FreezePositionY;
            }
        }

        if (_skillSet == null)
            _skillSet = GetComponent<EnemySkillSet>();
        
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
        bool hasProjectileSkill = HasProjectileSkillConfigured();
        bool hasAnyProjectileCapability = hasProjectileSkill || HasInspectorProjectilePrefabs();
        float projectileAttackRange = hasProjectileSkill ? GetMaxProjectileSkillRange() : 0f;
        // Nếu có prefab nhưng không có DB skill, dùng range mặc định 4.5 units
        if (!hasProjectileSkill && hasAnyProjectileCapability)
            projectileAttackRange = 4.5f;
        float projectileMinDistance = hasAnyProjectileCapability ? GetProjectileMinDistance() : 0f;
        if (hasAnyProjectileCapability)
            projectileMinDistance = Mathf.Min(projectileMinDistance, Mathf.Max(0.75f, projectileAttackRange - 0.25f));
        float combatRange = hasAnyProjectileCapability ? Mathf.Max(detectionRange, projectileAttackRange) : detectionRange;

        if (state == State.MeleeAttack)
        {
            rb.velocity = Vector2.zero;

            if (Time.time - attackStartTime >= MAX_ATTACK_DURATION)
            {
                ForceResetAttackState();
            }
            return;
        }

        // Aggro: nếu player trong detectionRange thì đuổi theo
        if (dist <= combatRange)
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

            // ✅ FIX: Nếu enemy có projectile prefab trong Inspector nhưng không có DB skill,
            // tự động bắn projectile khi player trong tầm 4.5 units
            if (!hasProjectileSkill && HasInspectorProjectilePrefabs()
                && dist <= projectileAttackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                if (TryFireInspectorProjectile())
                {
                    state = State.MeleeAttack;
                    lastAttackTime = Time.time;
                    attackStartTime = Time.time;
                    rb.velocity = Vector2.zero;
                    TriggerAttackAnimation();
                    StartCoroutine(ResetAttackAfterDelay(0.8f));
                    return;
                }
            }

            if (hasAnyProjectileCapability && dist < projectileMinDistance)
            {
                if (canFly)
                    MoveProjectileEnemyAwayFrom(player.position, projectileMinDistance);
                else
                    MoveGroundProjectileEnemyAwayFrom(player.position.x, projectileMinDistance);

                return;
            }

            if (hasAnyProjectileCapability)
            {
                if (dist <= projectileAttackRange)
                {
                    HoldProjectileEnemyPosition();
                    return;
                }

                if (canFly)
                    MoveProjectileEnemyTowards(player.position, projectileAttackRange);
                else
                    MoveGroundProjectileEnemyTowards(player.position.x, projectileAttackRange);

                return;
            }

            // Tấn công melee (fallback khi không có skill hoặc skill đang cooldown)
            if (dist <= meleeAttackRange && Time.time - lastAttackTime >= attackCooldown)
            {
                StartMeleeAttack();
                return;
            }

            if (dist <= meleeAttackRange)
            {
                HoldMeleePosition();
                return;
            }

            // Chạy về phía player khi chưa đủ tầm đánh
            if (dist > meleeAttackRange)
            {
                if (canFly)
                    MoveProjectileEnemyTowards(player.position, meleeAttackRange);
                else
                    RunTowards(player.position.x);
                return;
            }
        }

        // Ngoài tầm phát hiện → tuần tra
        PatrolLoop();
    }

    /// <summary>Bật animation attack theo bool isAttacking; vẫn tương thích config cũ dùng Attack trigger.</summary>
    private void TriggerAttackAnimation()
    {
        _activeAttackBoolParameter = DefaultAttackBoolParameter;

        if (TrySetAttackBool(DefaultAttackBoolParameter, true))
            return;

        _activeAttackBoolParameter = null;
        TrySetAttackTrigger(LegacyAttackTriggerParameter);
    }

    private void PlaySkillAnimation(string animationParameter)
    {
        if (string.IsNullOrWhiteSpace(animationParameter)
            || string.Equals(animationParameter, DefaultAttackBoolParameter, StringComparison.OrdinalIgnoreCase)
            || string.Equals(animationParameter, LegacyAttackTriggerParameter, StringComparison.OrdinalIgnoreCase))
        {
            TriggerAttackAnimation();
            return;
        }

        if (TrySetAttackBool(animationParameter, true))
        {
            _activeAttackBoolParameter = animationParameter;
            return;
        }

        _activeAttackBoolParameter = null;
        if (TrySetAttackTrigger(animationParameter))
            return;

        TriggerAttackAnimation();
    }

    private bool TrySetAttackBool(string parameterName, bool value)
    {
        if (string.IsNullOrWhiteSpace(parameterName))
            return false;

        if (string.Equals(parameterName, DefaultAttackBoolParameter, StringComparison.OrdinalIgnoreCase)
            && networkController != null)
        {
            networkController.SetAttackAnimationState(value);
            return true;
        }

        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
            return false;

        animator.SetBool(parameterName, value);
        return true;
    }

    private bool TrySetAttackTrigger(string parameterName)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
            return false;

        animator.SetTrigger(parameterName);
        return true;
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (var parameter in animator.parameters)
        {
            if (string.Equals(parameter.name, parameterName, StringComparison.Ordinal)
                && parameter.type == parameterType)
                return true;
        }

        return false;
    }

    private void ResetAttackAnimation()
    {
        string parameterToReset = string.IsNullOrWhiteSpace(_activeAttackBoolParameter)
            ? DefaultAttackBoolParameter
            : _activeAttackBoolParameter;

        if (!TrySetAttackBool(parameterToReset, false)
            && !string.Equals(parameterToReset, DefaultAttackBoolParameter, StringComparison.OrdinalIgnoreCase))
        {
            TrySetAttackBool(DefaultAttackBoolParameter, false);
        }

        _activeAttackBoolParameter = null;
    }

    private void StartMeleeAttack()
    {
        state = State.MeleeAttack;
        lastAttackTime = Time.time;
        attackStartTime = Time.time;
        rb.velocity = Vector2.zero;

        TriggerAttackAnimation();

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

    private void HoldMeleePosition()
    {
        if (rb == null)
            return;

        rb.velocity = Vector2.zero;
    }


    private void PatrolLoop()
    {
        if (canFly)
        {
            // Tuần tra 2D ngẫu nhiên quanh anchor (mỗi con 1 hướng).
            _flyPatrolRetargetTimer -= Time.deltaTime;
            float dist = Vector2.Distance(transform.position, _flyPatrolTarget);
            if (dist < 0.25f || _flyPatrolRetargetTimer <= 0f)
                RetargetFlyPatrol();

            MoveProjectileEnemyTowards(_flyPatrolTarget, 0.15f);

            // Cập nhật facing theo hướng X tới target
            float dx = _flyPatrolTarget.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f)
            {
                bool wantRight = dx > 0f;
                if (wantRight != facingRight)
                {
                    facingRight = wantRight;
                    Flip();
                }
            }
            return;
        }

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

        Vector2 targetPos = facingRight ? (Vector2)rightPoint.position : (Vector2)leftPoint.position;
        RunTowards(targetPos.x);
        if (Mathf.Abs(transform.position.x - targetPos.x) < 0.1f)
        {
            facingRight = !facingRight;
            Flip();
        }
    }

    private void RetargetFlyPatrol()
    {
        if (!_serverAnchorSet)
        {
            _serverAnchorX = transform.position.x;
            _serverAnchorY = transform.position.y;
            _serverAnchorSet = true;
        }

        float rx = Mathf.Max(0.5f, flyPatrolRadiusX);
        float ry = Mathf.Max(0.5f, flyPatrolRadiusY);
        float ox = UnityEngine.Random.Range(-rx, rx);
        // Bias lên trên một chút để enemy bay trải đều trên không (anchor +- ry, không bị âm quá nhiều).
        float oy = UnityEngine.Random.Range(-ry * 0.5f, ry);
        _flyPatrolTarget = new Vector2(_serverAnchorX + ox, _serverAnchorY + oy);
        _flyPatrolRetargetTimer = Mathf.Max(0.5f, flyPatrolRetargetSeconds);
    }

    private void RunTowards(float targetX)
    {
        if (rb == null) return;
        if (_isFrozen) { rb.velocity = Vector2.zero; return; }
        if (!canFly && !IsGroundedEnough())
        {
            LogGroundMovement("RunBlockedUngrounded", $"targetX={targetX:F2}");
            rb.velocity = new Vector2(0f, rb.velocity.y);
            return;
        }

        float dir = Mathf.Sign(targetX - transform.position.x);
        float yVel = canFly ? 0f : rb.velocity.y;
        // Áp dụng SlowFactor từ DebuffManager nếu có
        float slow = GetDebuffSlowFactor();
        Vector2 newVelocity = new Vector2(dir * moveSpeed * slow, yVel);
        rb.velocity = newVelocity;

        if ((dir > 0 && !facingRight) || (dir < 0 && facingRight))
        {
            Flip();
        }
    }

    private float GetDebuffSlowFactor()
    {
        var debuffMgr = GetComponent<DebuffManager>();
        return debuffMgr != null ? debuffMgr.GetSlowFactor() : 1f;
    }

    private bool ShouldUseServerVirtualGround()
    {
        return useServerVirtualGround
            && !canFly
            && rb != null
            && _serverAnchorSet
            && !UsesRealGroundPhysics();
    }

    private void LogGroundMovement(string eventName, string details = "", bool force = false)
    {
        if (!debugGroundMovement || canFly || rb == null)
            return;

        if (!force && Time.time < _nextGroundDebugLogTime)
            return;

        _nextGroundDebugLogTime = Time.time + Mathf.Max(0.05f, debugGroundLogInterval);

        NetworkObject netObj = GetComponent<NetworkObject>();
        string ownerInfo = netObj != null ? netObj.OwnerClientId.ToString() : "none";
        Debug.Log(
            $"[EnemyGroundDebug] evt={eventName} name={gameObject.name} scene={gameObject.scene.name} map={GetMyMapId()} owner={ownerInfo} pos={rb.position} vel={rb.velocity} gravity={rb.gravityScale:F2} useVirtual={ShouldUseServerVirtualGround()} anchorY={_serverAnchorY:F2} {details}",
            this);
    }

    private bool HasProjectileSkillConfigured()
    {
        if (_skillSet == null || !_skillSet.HasSkills)
            return false;

        foreach (var skill in _skillSet.Skills)
        {
            if (!string.IsNullOrWhiteSpace(skill.projectile_prefab_key))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra xem enemy có projectile prefab được gán trong Inspector không (không cần DB skill).
    /// Dùng để cho phép bay/di chuyển ranged khi không có DB skill config.
    /// </summary>
    private bool HasInspectorProjectilePrefabs()
    {
        if (projectilePrefabs == null || projectilePrefabs.Count == 0)
            return false;

        foreach (var entry in projectilePrefabs)
        {
            if (entry != null && entry.prefab != null && !string.IsNullOrWhiteSpace(entry.key))
                return true;
        }

        return false;
    }

    private float GetMaxProjectileSkillRange()
    {
        if (_skillSet == null || !_skillSet.HasSkills)
            return 0f;

        float maxRange = 0f;
        foreach (var skill in _skillSet.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.projectile_prefab_key))
                continue;

            maxRange = Mathf.Max(maxRange, skill.range);
        }

        return maxRange;
    }

    private float GetProjectileMinDistance()
    {
        float desiredDistance = Mathf.Max(projectileKeepDistance, meleeAttackRange + 0.35f);
        if (_skillSet == null || !_skillSet.HasSkills)
            return desiredDistance;

        float maxSpawnOffset = 0f;
        foreach (var skill in _skillSet.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.projectile_prefab_key))
                continue;

            maxSpawnOffset = Mathf.Max(maxSpawnOffset, Mathf.Abs(skill.projectile_spawn_offset_x));
        }

        return Mathf.Max(desiredDistance, maxSpawnOffset + 0.65f);
    }

    private void HoldProjectileEnemyPosition()
    {
        if (rb == null) return;
        rb.velocity = Vector2.zero;
    }

    private void MoveProjectileEnemyTowards(Vector2 targetPosition, float stopDistance)
    {
        if (rb == null) return;

        Vector2 toTarget = targetPosition - rb.position;
        if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
        {
            HoldProjectileEnemyPosition();
            return;
        }

        MoveProjectileEnemy(toTarget.normalized);
    }

    private void MoveProjectileEnemyAwayFrom(Vector2 targetPosition, float keepDistance)
    {
        if (rb == null) return;

        Vector2 awayFromTarget = rb.position - targetPosition;
        if (awayFromTarget.sqrMagnitude >= keepDistance * keepDistance)
        {
            HoldProjectileEnemyPosition();
            return;
        }

        if (awayFromTarget.sqrMagnitude < 0.0001f)
            awayFromTarget = facingRight ? Vector2.left : Vector2.right;

        MoveProjectileEnemy(awayFromTarget.normalized);
    }

    private void MoveGroundProjectileEnemyTowards(float targetX, float stopDistance)
    {
        if (rb == null)
            return;

        float horizontalDelta = targetX - rb.position.x;
        if (Mathf.Abs(horizontalDelta) <= stopDistance)
        {
            HoldProjectileEnemyPosition();
            return;
        }

        if (!IsGroundedEnough()) { rb.velocity = new Vector2(0f, rb.velocity.y); return; }
        float horizontalDirection = Mathf.Sign(horizontalDelta);
        float slow = GetDebuffSlowFactor();
        if (_isFrozen) { HoldProjectileEnemyPosition(); return; }
        rb.velocity = new Vector2(horizontalDirection * moveSpeed * slow, rb.velocity.y);
        UpdateFacingFromHorizontal(rb.velocity.x);
    }

    private void MoveGroundProjectileEnemyAwayFrom(float targetX, float keepDistance)
    {
        if (rb == null)
            return;

        float horizontalDelta = rb.position.x - targetX;
        if (Mathf.Abs(horizontalDelta) >= keepDistance)
        {
            HoldProjectileEnemyPosition();
            return;
        }

        float horizontalDirection = Mathf.Abs(horizontalDelta) > 0.01f
            ? Mathf.Sign(horizontalDelta)
            : (facingRight ? -1f : 1f);

        if (!IsGroundedEnough()) { rb.velocity = new Vector2(0f, rb.velocity.y); return; }
        float slow = GetDebuffSlowFactor();
        if (_isFrozen) { HoldProjectileEnemyPosition(); return; }
        rb.velocity = new Vector2(horizontalDirection * moveSpeed * slow, rb.velocity.y);
        UpdateFacingFromHorizontal(rb.velocity.x);
    }

    private void MoveProjectileEnemy(Vector2 desiredDirection)
    {
        if (rb == null)
            return;

        if (desiredDirection.sqrMagnitude < 0.0001f)
        {
            HoldProjectileEnemyPosition();
            return;
        }

        Vector2 moveDirection = desiredDirection.normalized;
        float verticalSpeed = Mathf.Max(0.1f, projectileVerticalMoveSpeed);
        float slow = GetDebuffSlowFactor();
        if (_isFrozen) { HoldProjectileEnemyPosition(); return; }
        Vector2 velocity = new Vector2(moveDirection.x * moveSpeed * slow, moveDirection.y * verticalSpeed * slow);

        if (!canFly && velocity.y < -0.01f && !CanProjectileEnemyMoveDown())
        {
            velocity.y = 0f;
            if (Mathf.Abs(velocity.x) < 0.01f)
            {
                HoldProjectileEnemyPosition();
                return;
            }
        }

        rb.velocity = velocity;
        UpdateFacingFromHorizontal(rb.velocity.x);
    }

    private void UpdateFacingFromHorizontal(float horizontalVelocity)
    {
        if (Mathf.Abs(horizontalVelocity) <= 0.01f)
            return;

        bool shouldFaceRight = horizontalVelocity > 0f;
        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            ApplyFacing();
        }
    }

    private bool CanProjectileEnemyMoveDown()
    {
        if (bodyCollider == null || _groundLayerMask == 0)
            return true;

        if (_ignoredGroundCollider != null)
            return true;

        Collider2D currentPlatform = GetCurrentGroundPlatform();
        if (currentPlatform == null)
            return true;

        if (!HasGroundBelow(currentPlatform))
            return false;

        if (_projectileFallThroughCoroutine == null)
            _projectileFallThroughCoroutine = StartCoroutine(ProjectileFallThroughCoroutine(currentPlatform));

        return true;
    }

    private Collider2D GetCurrentGroundPlatform()
    {
        if (bodyCollider == null || _groundLayerMask == 0)
            return null;

        Bounds bounds = bodyCollider.bounds;
        float rayDistance = 0.2f;
        float inset = Mathf.Min(bounds.extents.x * 0.6f, 0.25f);
        float leftX = bounds.min.x + inset;
        float centerX = bounds.center.x;
        float rightX = bounds.max.x - inset;
        float rayOriginY = bounds.min.y + 0.05f;

        Collider2D platform = RaycastOneWayGround(new Vector2(centerX, rayOriginY), rayDistance);
        if (platform != null) return platform;

        platform = RaycastOneWayGround(new Vector2(leftX, rayOriginY), rayDistance);
        if (platform != null) return platform;

        return RaycastOneWayGround(new Vector2(rightX, rayOriginY), rayDistance);
    }

    private Collider2D RaycastOneWayGround(Vector2 origin, float distance)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, distance, _groundLayerMask);
        foreach (var hit in hits)
        {
            if (hit.collider == null)
                continue;

            PlatformEffector2D effector = hit.collider.GetComponent<PlatformEffector2D>();
            if (effector != null && effector.useOneWay)
                return hit.collider;
        }

        return null;
    }

    private bool HasGroundBelow(Collider2D currentPlatform)
    {
        if (currentPlatform == null || _groundLayerMask == 0)
            return false;

        Bounds bounds = bodyCollider != null ? bodyCollider.bounds : currentPlatform.bounds;
        float inset = Mathf.Min(bounds.extents.x * 0.6f, 0.25f);
        float leftX = bounds.min.x + inset;
        float centerX = bounds.center.x;
        float rightX = bounds.max.x - inset;
        float startY = currentPlatform.bounds.min.y - 0.05f;

        return HasGroundBelowAtPoint(currentPlatform, new Vector2(centerX, startY))
            || HasGroundBelowAtPoint(currentPlatform, new Vector2(leftX, startY))
            || HasGroundBelowAtPoint(currentPlatform, new Vector2(rightX, startY));
    }

    private bool HasGroundBelowAtPoint(Collider2D currentPlatform, Vector2 origin)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, projectileGroundSearchDistance, _groundLayerMask);
        foreach (var hit in hits)
        {
            if (hit.collider == null || hit.collider == currentPlatform)
                continue;

            return true;
        }

        return false;
    }

    private IEnumerator ProjectileFallThroughCoroutine(Collider2D platform)
    {
        if (bodyCollider == null || rb == null || platform == null)
        {
            _projectileFallThroughCoroutine = null;
            yield break;
        }

        Bounds platformBounds = platform.bounds;
        _ignoredGroundCollider = platform;
        Physics2D.IgnoreCollision(bodyCollider, platform, true);
        rb.position = new Vector2(rb.position.x, rb.position.y - Mathf.Max(0.05f, projectileFallThroughDrop));
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(rb.velocity.y, -Mathf.Max(0.1f, projectileVerticalMoveSpeed)));

        float elapsed = 0f;
        float maxWait = projectileFallThroughDuration + 0.75f;

        while (elapsed < maxWait)
        {
            elapsed += Time.deltaTime;

            bool minimumDurationElapsed = elapsed >= projectileFallThroughDuration;
            bool fullyBelowPlatform = bodyCollider == null || bodyCollider.bounds.max.y < platformBounds.min.y - 0.05f;
            if (minimumDurationElapsed && fullyBelowPlatform)
                break;

            yield return null;
        }

        if (bodyCollider != null && platform != null)
            Physics2D.IgnoreCollision(bodyCollider, platform, false);

        _ignoredGroundCollider = null;
        _projectileFallThroughCoroutine = null;
    }

    private void RestoreIgnoredGroundCollision()
    {
        if (_projectileFallThroughCoroutine != null)
        {
            StopCoroutine(_projectileFallThroughCoroutine);
            _projectileFallThroughCoroutine = null;
        }

        if (bodyCollider != null && _ignoredGroundCollider != null)
            Physics2D.IgnoreCollision(bodyCollider, _ignoredGroundCollider, false);

        _ignoredGroundCollider = null;
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

    /// <summary>Thực thi skill từ EnemySkillSet: trigger animation Attack, thả đạn hoặc gây damage, reset state.</summary>
    private IEnumerator UseSkillCoroutine(SkillEntry skill)
    {
        // Chuẩn mới: melee và ranged đều bật bool isAttacking.
        // Nếu config animation_trigger là trigger riêng thì vẫn hỗ trợ, còn "Attack"/"isAttacking" sẽ map về bool chuẩn.
        PlaySkillAnimation(skill.animation_trigger);

        // Chờ hit frame
        yield return new WaitForSeconds(0.3f);

        // Damage luôn lấy từ stat của chính con quái (EnemyAI.damage)
        int dmg = damage;

        bool projectileSpawned = TrySpawnProjectileSkill(skill, dmg);

        // ✅ FIX: Nếu DB skill có projectile_prefab_key nhưng TrySpawnProjectileSkill thất bại
        // (không resolve được prefab), fallback sang Inspector prefab
        if (!projectileSpawned && !string.IsNullOrWhiteSpace(skill.projectile_prefab_key)
            && HasInspectorProjectilePrefabs())
        {
            Debug.Log($"[EnemyAI] {gameObject.name} skill '{skill.skill_id}' DB prefab fail → fallback Inspector projectile.");
            projectileSpawned = TryFireInspectorProjectile();
        }

        if (projectileSpawned)
        {
            if (_skillSet != null)
                _skillSet.MarkSkillUsed(skill.skill_id);

            yield return new WaitForSeconds(0.5f);
            ForceResetAttackState();
            yield break;
        }

        if (skill.aoe)
        {
            float radius = skill.aoe_radius > 0f ? skill.aoe_radius : Mathf.Max(skill.range, 1f);
            Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(
                gameObject,
                transform.position,
                radius,
                LayerMask.GetMask("Player"));
            foreach (var col in hits)
            {
                // ApplyDamageToTarget đã tự kiểm tra cùng map bên trong
                ApplyDamageToTarget(col.gameObject, dmg);
            }
        }
        else if (player != null)
        {
            float effectiveRange = skill.range > 0f ? skill.range : meleeAttackRange + 0.5f;
            if (Vector2.Distance(transform.position, player.position) <= effectiveRange)
                ApplyDamageToTarget(player.gameObject, dmg);
        }

        if (_skillSet != null)
            _skillSet.MarkSkillUsed(skill.skill_id);

        yield return new WaitForSeconds(0.5f);
        ForceResetAttackState();
    }

    /// <summary>
    /// Bắn projectile từ Inspector prefab list khi không có DB skill config.
    /// Dùng prefab đầu tiên có sẵn, tầm bắn mặc định, tốc độ mặc định.
    /// </summary>
    private bool TryFireInspectorProjectile()
    {
        if (player == null || projectilePrefabs == null || projectilePrefabs.Count == 0)
            return false;

        // Lấy prefab đầu tiên hợp lệ
        GameObject projectilePrefab = null;
        foreach (var entry in projectilePrefabs)
        {
            if (entry != null && entry.prefab != null)
            {
                projectilePrefab = entry.prefab;
                break;
            }
        }

        if (projectilePrefab == null)
            return false;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        Vector2 aimDir = GetProjectileAimDirection(spawnPos);
        spawnPos += new Vector3(Mathf.Sign(aimDir.x) * 0.6f, 0.25f, 0f);
        aimDir = GetProjectileAimDirection(spawnPos);

        GameObject projectileObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        // Xoay projectile theo hướng bắn (Z rotation)
        float angle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
        projectileObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Disable player's FireballDamage, enable EnemyProjectile
        FireballDamage fb = projectileObj.GetComponent<FireballDamage>();
        if (fb != null) fb.enabled = false;

        EnemyProjectile ep = projectileObj.GetComponent<EnemyProjectile>();
        if (ep == null) ep = projectileObj.AddComponent<EnemyProjectile>();
        ep.damage = damage;
        ep.EnemyMapId = GetMyMapId();
        ep.lifetime = 3f;

        // Flip projectile sprite theo hướng bắn + giữ rotation
        if (Mathf.Abs(aimDir.x) > 0.01f)
        {
            Vector3 ls = projectileObj.transform.localScale;
            ls.x = Mathf.Abs(ls.x);
            // Nếu bắn sang trái, flip Y thay vì X để giữ rotation đúng
            if (aimDir.x < 0f)
                ls.y = -Mathf.Abs(ls.y);
            else
                ls.y = Mathf.Abs(ls.y);
            projectileObj.transform.localScale = ls;
        }

        // Di chuyển vào physics scene riêng của map — TRƯỚC Spawn()
        int myMap = GetMyMapId();
        MapSceneManager.Instance?.MoveToMapScene(projectileObj, myMap);

        // Gán visibility theo map để client ở map khác không thấy projectile
        ApplyProjectileVisibility(projectileObj, myMap);

        // Set velocity SAU khi chuyển physics scene để tránh bị reset
        Rigidbody2D projRb = projectileObj.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.gravityScale = 0f;
            projRb.velocity = aimDir * 8f;
        }

        NetworkObject netObj = projectileObj.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
            netObj.Spawn();

        Debug.Log($"[EnemyAI] {gameObject.name} bắn projectile (Inspector prefab) → damage={damage}");
        return true;
    }

    private IEnumerator ResetAttackAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ForceResetAttackState();
    }

    /// <summary>Shared helper: apply damage to player by checking NetworkPlayerHealth first.
    /// Kiểm tra cùng map trước khi gây damage — ngăn cross-map damage.</summary>
    private void ApplyDamageToTarget(GameObject target, int dmg)
    {
        // Kiểm tra cùng map: không được tấn công player ở map khác
        if (!IsSameMapAsTarget(target.transform))
        {
            Debug.LogWarning($"[EnemyAI] {gameObject.name} bỏ qua damage cross-map (enemy map={GetMyMapId()}, target={target.name})");
            return;
        }
        var netHealth = target.GetComponentInParent<NetworkPlayerHealth>();
        if (netHealth != null) { netHealth.TakeDamage(dmg); return; }
        var ph = target.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(dmg);
    }

    private void BuildProjectileLookup()
    {
        _projectilePrefabLookup.Clear();

        if (projectilePrefabs == null) return;

        foreach (var entry in projectilePrefabs)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.prefab == null)
                continue;

            _projectilePrefabLookup[entry.key.Trim()] = entry.prefab;
        }
    }

    private bool TrySpawnProjectileSkill(SkillEntry skill, int dmg)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.projectile_prefab_key) || player == null)
            return false;

        GameObject projectilePrefab = ResolveProjectilePrefab(skill.projectile_prefab_key);
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[EnemyAI] {gameObject.name} thiếu projectile prefab cho key '{skill.projectile_prefab_key}' (skill '{skill.skill_id}').");
            return false;
        }

        Vector2 aimDirection = GetProjectileAimDirection(projectileSpawnPoint != null
            ? (Vector2)projectileSpawnPoint.position
            : rb != null ? rb.position : (Vector2)transform.position);
        Vector3 spawnPosition = GetProjectileSpawnPosition(skill, aimDirection);
        aimDirection = GetProjectileAimDirection(spawnPosition);
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
        // Di chuyển vào physics scene riêng của map — TRƯỚC Spawn()
        int myMap = GetMyMapId();
        MapSceneManager.Instance?.MoveToMapScene(projectileObject, myMap);

        // Gán visibility theo map để client ở map khác không thấy projectile
        ApplyProjectileVisibility(projectileObject, myMap);

        // Set velocity SAU khi chuyển physics scene (qua PrepareProjectileInstance)
        PrepareProjectileInstance(projectileObject, skill, dmg, aimDirection);

        NetworkObject projectileNetObj = projectileObject.GetComponent<NetworkObject>();
        if (projectileNetObj != null && !projectileNetObj.IsSpawned)
            projectileNetObj.Spawn();

        return true;
    }

    private GameObject ResolveProjectilePrefab(string projectileKey)
    {
        if (string.IsNullOrWhiteSpace(projectileKey)) return null;
        if (_projectilePrefabLookup.Count == 0) BuildProjectileLookup();

        string normalizedKey = projectileKey.Trim();
        if (_projectilePrefabLookup.TryGetValue(normalizedKey, out GameObject projectilePrefab)
            && projectilePrefab != null)
        {
            return projectilePrefab;
        }

        // Thử tìm trong NetworkManager registered prefabs
        projectilePrefab = ResolveRegisteredProjectilePrefab(normalizedKey);
        if (projectilePrefab != null)
        {
            _projectilePrefabLookup[normalizedKey] = projectilePrefab;
            Debug.Log($"[EnemyAI] {gameObject.name} dùng network prefab '{projectilePrefab.name}' cho projectile key '{normalizedKey}'.");
            return projectilePrefab;
        }

        // ✅ FIX: Fallback — nếu không tìm thấy bằng key, dùng prefab đầu tiên trong Inspector list
        // Điều này đảm bảo enemy có Inspector prefab vẫn bắn được dù DB key không khớp tên
        if (projectilePrefabs != null)
        {
            foreach (var entry in projectilePrefabs)
            {
                if (entry != null && entry.prefab != null)
                {
                    Debug.Log($"[EnemyAI] {gameObject.name} key '{normalizedKey}' không tìm thấy → fallback Inspector prefab '{entry.prefab.name}'.");
                    _projectilePrefabLookup[normalizedKey] = entry.prefab;
                    return entry.prefab;
                }
            }
        }

        return null;
    }

    private GameObject ResolveRegisteredProjectilePrefab(string projectileKey)
    {
        var networkManager = NetworkManager.Singleton;
        var prefabsList = networkManager?.NetworkConfig?.Prefabs;
        if (prefabsList?.Prefabs == null)
            return null;

        foreach (var registeredPrefab in prefabsList.Prefabs)
        {
            GameObject prefab = registeredPrefab?.Prefab;
            if (prefab == null)
                continue;

            if (string.Equals(prefab.name, projectileKey, StringComparison.OrdinalIgnoreCase))
                return prefab;
        }

        return null;
    }

    private Vector2 GetProjectileAimDirection(Vector2 origin)
    {
        if (player == null)
            return facingRight ? Vector2.right : Vector2.left;

        Vector2 toPlayer = (Vector2)player.position - origin;
        if (toPlayer.sqrMagnitude < 0.0001f)
            toPlayer = facingRight ? Vector2.right : Vector2.left;

        float overshootDistance = Mathf.Max(0f, projectileAimOvershoot);
        Collider2D playerCollider = player.GetComponentInChildren<Collider2D>();
        if (playerCollider != null)
            overshootDistance += Mathf.Max(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y);

        Vector2 targetPoint = (Vector2)player.position + toPlayer.normalized * overshootDistance;
        Vector2 aimDirection = targetPoint - origin;
        if (aimDirection.sqrMagnitude < 0.0001f)
            return toPlayer.normalized;

        return aimDirection.normalized;
    }

    private Vector3 GetProjectileSpawnPosition(SkillEntry skill, Vector2 aimDirection)
    {
        Vector3 basePosition = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        float horizontalSign = Mathf.Abs(aimDirection.x) > 0.01f ? Mathf.Sign(aimDirection.x) : (facingRight ? 1f : -1f);
        float offsetX = Mathf.Abs(skill.projectile_spawn_offset_x) * horizontalSign;
        return basePosition + new Vector3(offsetX, skill.projectile_spawn_offset_y, 0f);
    }

    private void PrepareProjectileInstance(GameObject projectileObject, SkillEntry skill, int dmg, Vector2 aimDirection)
    {
        if (projectileObject == null) return;

        FireballDamage playerProjectileDamage = projectileObject.GetComponent<FireballDamage>();
        if (playerProjectileDamage != null)
            playerProjectileDamage.enabled = false;

        EnemyProjectile enemyProjectile = projectileObject.GetComponent<EnemyProjectile>();
        if (enemyProjectile == null)
            enemyProjectile = projectileObject.AddComponent<EnemyProjectile>();

        enemyProjectile.damage = dmg;
        enemyProjectile.EnemyMapId = GetMyMapId(); // Truyền map của enemy vào projectile để tránh cross-map damage
        if (skill.projectile_lifetime > 0f)
            enemyProjectile.lifetime = skill.projectile_lifetime;

        Rigidbody2D projectileRb = projectileObject.GetComponent<Rigidbody2D>();
        if (projectileRb != null)
        {
            projectileRb.gravityScale = 0f;
            projectileRb.velocity = aimDirection * Mathf.Max(0.1f, skill.projectile_speed);
        }

        // Xoay projectile theo hướng bắn (Z rotation)
        float aimAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, aimAngle);

        // Flip Y thay vì X khi bắn trái — giữ Z rotation đúng hướng
        if (Mathf.Abs(aimDirection.x) > 0.01f)
        {
            Vector3 localScale = projectileObject.transform.localScale;
            localScale.x = Mathf.Abs(localScale.x);
            localScale.y = aimDirection.x < 0f ? -Mathf.Abs(localScale.y) : Mathf.Abs(localScale.y);
            projectileObject.transform.localScale = localScale;
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
        
        ResetAttackAnimation();
    }

    private void OnDeath()
    {
        if (state == State.Dead) return; // tránh gọi 2 lần
        state = State.Dead;

        RestoreIgnoredGroundCollision();

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

    /// <summary>
    /// Gán ZoneOwnerTag + NetworkVisibilityZoneFilter cho projectile
    /// để client ở map khác không thấy (giống enemy).
    /// </summary>
    private static void ApplyProjectileVisibility(GameObject projObj, int mapId)
    {
        if (projObj == null || mapId < 0) return;

        var zoneTag = projObj.GetComponent<ZoneOwnerTag>();
        if (zoneTag == null) zoneTag = projObj.AddComponent<ZoneOwnerTag>();
        zoneTag.SetZone(mapId, 0);

        var filter = projObj.GetComponent<NetworkVisibilityZoneFilter>();
        if (filter == null) filter = projObj.AddComponent<NetworkVisibilityZoneFilter>();
        filter.InitializeForServer();
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

    private void InitializeInitialPatrolDirection()
    {
        if (_initialPatrolDirectionInitialized || !randomizeInitialPatrolDirection)
            return;

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return;

        bool shouldFaceRight = UnityEngine.Random.value >= 0.5f;
        if (shouldFaceRight != facingRight)
        {
            facingRight = shouldFaceRight;
            ApplyFacing();
        }

        _initialPatrolDirectionInitialized = true;
        Debug.Log($"[EnemyAI] {gameObject.name}: initial patrol direction={(facingRight ? "right" : "left")}");
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

    private bool IsGroundedEnough()
    {
        if (canFly || bodyCollider == null || _groundLayerMask == 0) return true;

        bool isServerCtx = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        bool useVirtualGround = isServerCtx && ShouldUseServerVirtualGround();
        bool grounded;
        if (useVirtualGround)
        {
            grounded = Mathf.Abs(rb.position.y - _serverAnchorY) <= 0.5f;
        }
        else
        {
            var physScene = gameObject.scene.GetPhysicsScene2D();
            Bounds b = bodyCollider.bounds;

            float inset = Mathf.Min(b.extents.x * 0.5f, 0.2f);
            float originY = Mathf.Max(transform.position.y, b.center.y);
            float feetY = b.min.y;
            float checkDist = Mathf.Max(0.75f, originY - feetY + 0.4f);

            grounded = IsGroundHitCloseToFeet(physScene.Raycast(new Vector2(b.center.x, originY), Vector2.down, checkDist, _groundLayerMask), feetY)
                || IsGroundHitCloseToFeet(physScene.Raycast(new Vector2(b.min.x + inset, originY), Vector2.down, checkDist, _groundLayerMask), feetY)
                || IsGroundHitCloseToFeet(physScene.Raycast(new Vector2(b.max.x - inset, originY), Vector2.down, checkDist, _groundLayerMask), feetY);
        }

        if (grounded != _lastGroundedEnough)
        {
            _lastGroundedEnough = grounded;
            LogGroundMovement("GroundedChanged", $"grounded={grounded} branch={(useVirtualGround ? "virtual" : "physics")}", true);
        }

        return grounded;
    }

    private bool IsGroundHitCloseToFeet(RaycastHit2D hit, float feetY)
    {
        if (hit.collider == null)
            return false;

        float surfaceY = hit.point.y;
        float maxGapBelowFeet = 0.35f;
        float maxSurfaceAboveFeet = 0.12f;
        return surfaceY <= feetY + maxSurfaceAboveFeet
            && feetY - surfaceY <= maxGapBelowFeet;
    }

    private void LateUpdate()
    {
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return;
        if (state == State.Dead) return;

        bool useVirtualGround = ShouldUseServerVirtualGround();
        if (useVirtualGround != _lastVirtualGroundMode)
        {
            _lastVirtualGroundMode = useVirtualGround;
            LogGroundMovement("VirtualGroundModeChanged", $"enabled={useVirtualGround}", true);
        }

        if (useVirtualGround)
        {
            Vector2 p = rb.position;
            if (!Mathf.Approximately(p.y, _serverAnchorY))
            {
                LogGroundMovement("VirtualGroundReset", $"fromY={p.y:F2} toY={_serverAnchorY:F2}", true);
                rb.position = new Vector2(p.x, _serverAnchorY);
            }
            if (Mathf.Abs(rb.velocity.y) > 0.01f)
            {
                LogGroundMovement("VirtualGroundZeroVelocity", $"oldVy={rb.velocity.y:F2}", true);
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }
        }
        else
        {
            MaintainGroundFailSafe();
        }

        ClampToMapBounds();
    }

    private bool UsesRealGroundPhysics()
    {
        return !canFly
            && !_forceVirtualGround
            && rb != null
            && bodyCollider != null
            && _groundLayerMask != 0
            && rb.gravityScale > 0.01f;
    }

    private IEnumerator SnapToGroundAfterSpawn()
    {
        yield return null;
        bool snapped = TrySnapToGround();

        if (!snapped && useServerVirtualGround && rb != null && _serverAnchorSet)
        {
            // Map không có collider Ground → bật virtual ground: tắt gravity,
            // khóa Y về anchor để enemy không rơi và vẫn chạy trục X.
            _forceVirtualGround = true;
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
            rb.position = new Vector2(rb.position.x, _serverAnchorY);
            CacheSupportedGroundPosition(rb.position);
            LogGroundMovement("VirtualGroundFallback", $"anchorY={_serverAnchorY:F2}", true);
            yield break;
        }

        if (IsGroundedEnough() && rb != null)
            CacheSupportedGroundPosition(rb.position);
    }

    private bool TrySnapToGround()
    {
        if (!UsesRealGroundPhysics())
            return false;

        Bounds bounds = bodyCollider.bounds;
        float rayDistance = Mathf.Max(0.5f, spawnGroundSnapDistance);
        float topPadding = Mathf.Max(0.1f, bounds.extents.y + 0.1f);
        Vector2 origin = new Vector2(bounds.center.x, transform.position.y + topPadding);
        RaycastHit2D hit = RaycastInCurrentScene(origin, Vector2.down, rayDistance + topPadding, _groundLayerMask);
        if (hit.collider == null)
            return false;

        float currentBottomOffset = bounds.min.y - transform.position.y;
        float snappedY = hit.point.y - currentBottomOffset + Mathf.Max(0f, spawnGroundOffset);
        rb.position = new Vector2(rb.position.x, snappedY);
        rb.velocity = Vector2.zero;
        CacheSupportedGroundPosition(rb.position);
        return true;
    }

    private void MaintainGroundFailSafe()
    {
        if (!UsesRealGroundPhysics() || rb == null)
        {
            _unsupportedFallStartTime = -1f;
            return;
        }

        if (IsGroundedEnough())
        {
            CacheSupportedGroundPosition(rb.position);
            return;
        }

        if (rb.velocity.y >= -0.01f)
        {
            _unsupportedFallStartTime = -1f;
            return;
        }

        if (_unsupportedFallStartTime < 0f)
            _unsupportedFallStartTime = Time.time;

        float referenceY = _hasSupportedGroundPosition
            ? _lastSupportedGroundPosition.y
            : (_serverAnchorSet ? _serverAnchorY : rb.position.y);
        bool fellTooFar = referenceY - rb.position.y >= Mathf.Max(0.5f, maxUnsupportedFallDistance);
        bool fellTooLong = Time.time - _unsupportedFallStartTime >= Mathf.Max(0.1f, maxUnsupportedFallTime);
        if (!fellTooFar && !fellTooLong)
            return;

        bool snappedToGround = TrySnapToGround();
        if (!snappedToGround)
        {
            Vector2 fallbackPosition = _hasSupportedGroundPosition
                ? _lastSupportedGroundPosition
                : new Vector2(rb.position.x, _serverAnchorSet ? _serverAnchorY : rb.position.y);
            rb.position = fallbackPosition;
            rb.velocity = Vector2.zero;
            CacheSupportedGroundPosition(fallbackPosition);
        }

        _unsupportedFallStartTime = -1f;
    }

    private void CacheSupportedGroundPosition(Vector2 position)
    {
        _lastSupportedGroundPosition = position;
        _hasSupportedGroundPosition = true;
        _unsupportedFallStartTime = -1f;
    }

    private RaycastHit2D RaycastInCurrentScene(Vector2 origin, Vector2 direction, float distance, int layerMask)
    {
        var scene = gameObject.scene;
        if (scene.IsValid())
        {
            var physicsScene = scene.GetPhysicsScene2D();
            if (physicsScene.IsValid())
                return physicsScene.Raycast(origin, direction, distance, layerMask);
        }

        return Physics2D.Raycast(origin, direction, distance, layerMask);
    }

    private void DetectMapBounds()
    {
        int maxMapLayerId = LayerMask.NameToLayer("MaxMap");
        if (maxMapLayerId < 0) return;

        BoxCollider2D[] allCols = FindObjectsOfType<BoxCollider2D>();
        Bounds combined = new Bounds();
        bool anyFound = false;

        foreach (var col in allCols)
        {
            if (col.gameObject.layer != maxMapLayerId) continue;
            if (!anyFound) { combined = col.bounds; anyFound = true; }
            else combined.Encapsulate(col.bounds);
        }

        if (!anyFound) return;

        Vector2 center = combined.center;
        float innerMinX = float.MinValue, innerMaxX = float.MaxValue;
        float innerMinY = float.MaxValue, innerMaxY = float.MaxValue;

        foreach (var col in allCols)
        {
            if (col.gameObject.layer != maxMapLayerId) continue;
            Bounds b = col.bounds;
            bool isVertical   = b.size.y > b.size.x * 1.5f;
            bool isHorizontal = b.size.x > b.size.y * 1.5f;

            if (isVertical)
            {
                if (b.center.x < center.x)
                    innerMinX = Mathf.Max(innerMinX, b.max.x);
                else
                    innerMaxX = Mathf.Min(innerMaxX, b.min.x);
            }
            if (isHorizontal)
            {
                if (b.center.y > center.y)
                    innerMaxY = Mathf.Min(innerMaxY, b.min.y);
                else
                    innerMinY = Mathf.Min(innerMinY, b.max.y);
            }
        }

        _mapMinX = (innerMinX > float.MinValue) ? innerMinX : combined.min.x;
        _mapMaxX = (innerMaxX < float.MaxValue) ? innerMaxX : combined.max.x;
        _mapMinY = (innerMinY < float.MaxValue) ? innerMinY : combined.min.y;
        _mapMaxY = (innerMaxY < float.MaxValue) ? innerMaxY : combined.max.y;
        _hasMapBounds = true;
    }

    private void ClampToMapBounds()
    {
        if (!_hasMapBounds || rb == null) return;

        Vector2 pos = rb.position;
        float newX = pos.x, newY = pos.y;
        float newVx = rb.velocity.x, newVy = rb.velocity.y;

        if (pos.x < _mapMinX)
        {
            newX = _mapMinX;
            if (newVx < 0f) newVx = 0f;
            if (!facingRight) { facingRight = true; ApplyFacing(); }
        }
        else if (pos.x > _mapMaxX)
        {
            newX = _mapMaxX;
            if (newVx > 0f) newVx = 0f;
            if (facingRight) { facingRight = false; ApplyFacing(); }
        }

        if (canFly)
        {
            if (pos.y < _mapMinY)
            {
                LogGroundMovement("ClampMapMinY", $"fromY={pos.y:F2} toY={_mapMinY:F2}", true);
                newY = _mapMinY;
                if (newVy < 0f) newVy = 0f;
            }
            else if (pos.y > _mapMaxY)
            {
                LogGroundMovement("ClampMapMaxY", $"fromY={pos.y:F2} toY={_mapMaxY:F2}", true);
                newY = _mapMaxY;
                if (newVy > 0f) newVy = 0f;
            }
        }

        if (newX != pos.x || newY != pos.y)
            rb.position = new Vector2(newX, newY);
        if (newVx != rb.velocity.x || newVy != rb.velocity.y)
            rb.velocity = new Vector2(newVx, newVy);
    }
}


