using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Boss AI with local Inspector-configured skills, retreat behavior, and optional ground physics.
/// Legacy server boss config is still supported when useInspectorSkillsOnly = false.
/// </summary>
[RequireComponent(typeof(EnemyHealth), typeof(Rigidbody2D), typeof(NetworkObject))]
public class BossAI : NetworkBehaviour
{
    public enum LocalBossSkillType
    {
        Melee,
        Projectile,
        Aoe
    }

    [Serializable]
    public class LocalBossSkillConfig
    {
        public string skillId = "";
        public LocalBossSkillType skillType = LocalBossSkillType.Melee;
        public GameObject visualPrefab;
        public Transform spawnPoint;
        public float range = 2f;
        public float minDistance = 0f;
        public float cooldown = 1.2f;
        public float castDelay = 0.25f;
        public float recoveryTime = 0.45f;
        public float damageMultiplier = 1f;
        public string animationParameter = "isAttacking";
        public float spawnOffsetX = 0.65f;
        public float spawnOffsetY = 0.25f;
        public float projectileSpeed = 8f;
        public float projectileLifetime = 3f;
        public float effectLifetime = 1.1f;
        public float hitRadius = 0.85f;
        public bool retreatAfterUse = true;
    }

    private static readonly int AnimIsAttacking = Animator.StringToHash("isAttacking");
    private static readonly int AnimIsMoving = Animator.StringToHash("isMoving");
    private static readonly int AnimIsGrounded = Animator.StringToHash("isGrounded");
    private const string DefaultAttackBoolParameter = "isAttacking";
    private const string DefaultMoveBoolParameter = "isMoving";
    private const string DefaultGroundedBoolParameter = "isGrounded";
    private const string DefaultJumpTriggerParameter = "Jump";
    private const string BasicMeleeSkillId = "__boss_basic_melee__";

    [Header("Boss ID (khop DB enemy.enemy_id)")]
    public int bossId = 8;

    [Header("Combat References")]
    public Transform playerTarget;
    public float detectionRange = 12f;
    public float meleeAttackRange = 2f;
    public float chaseSpeed = 2.5f;
    public float basicAttackCooldown = 1.2f;
    public Collider2D meleeHitbox;

    [Header("Inspector Skills")]
    [Tooltip("Bat de boss dung skill config trong Unity, bo qua skill tu boss config API.")]
    public bool useInspectorSkillsOnly = false;
    public List<LocalBossSkillConfig> localSkills = new();

    [Header("Movement / Retreat")]
    [Tooltip("Bat neu boss can dung gravity, dung tren ground va nhay giua platform.")]
    public bool useGroundPhysics = false;
    public bool canJump = false;
    public float jumpForce = 8f;
    public float minCalculatedJumpForce = 6f;
    public float jumpHeightPadding = 0.45f;
    public float obstacleJumpHeightPadding = 0.75f;
    public bool requireGroundedToJump = true;
    public bool allowUntargetedJumps = true;
    public int maxJumps = 1;
    public float verticalTargetThreshold = 0.85f;
    public float preferredRetreatDistance = 4.5f;
    public float retreatDuration = 1.1f;
    public float retreatSpeedMultiplier = 1.15f;
    [Range(0f, 1f)] public float retreatJumpChance = 0.6f;
    public float retreatJumpCooldown = 1.2f;
    public bool snapToGroundOnStart = true;
    public float spawnGroundSnapDistance = 12f;
    public float spawnGroundOffset = 0.02f;
    public float groundCheckRadius = 0.18f;
    public LayerMask groundLayerMask;
    public float groundSearchDistance = 14f;
    public float upperPlatformSearchHorizontalRange = 5f;
    public float upperPlatformSearchVerticalRange = 8f;
    public float upperPlatformJumpEdgeDistance = 1.25f;
    public float fallThroughDuration = 0.35f;
    public float fallThroughDrop = 0.12f;
    public float projectileAimOvershoot = 0.45f;
    public bool alwaysRetreatAfterMelee = true;
    public float retreatEvasionInterval = 0.35f;
    [Range(0f, 1f)] public float retreatFallThroughChance = 0.35f;
    [Range(0f, 1f)] public float approachJumpChance = 0.15f;
    public float reengageDelayAfterRetreat = 0.2f;
    public float postAttackRetreatSpeedMultiplier = 2f;
    public float postAttackRetreatMinDuration = 0.9f;

    [Header("Ground Traversal Control")]
    [Tooltip("Bat de chi cho boss doi tang ground khi dang chase player hoac dang retreat sau khi vua tan cong.")]
    public bool restrictGroundTraversalToChaseOrPostAttack = false;
    [Tooltip("Cooldown toi thieu giua hai lan nhay/roi doi ground. 0 = dung logic cu.")]
    public float groundTraversalCooldownMin = 0f;
    [Tooltip("Cooldown toi da giua hai lan nhay/roi doi ground. Nen >= min.")]
    public float groundTraversalCooldownMax = 0f;
    [Tooltip("Layer chan duong nhay/roi doi ground, vi du MaxMap.")]
    public LayerMask groundTraversalBlockerMask;

    [Header("Threat Evasion")]
    public bool evadeIncomingPlayerThreats = true;
    public float threatScanRadius = 4f;
    public float threatEvadeDuration = 0.9f;
    public float threatDirectionRefreshInterval = 0.12f;
    public LayerMask threatLayerMask;

    [Header("Advanced Dodge")]
    public bool useAdvancedDodge = true;
    [Range(0f, 1f)] public float dodgeJumpChance = 0.65f;
    [Range(0f, 1f)] public float dodgeDropChance = 0.45f;
    [Range(0f, 1f)] public float dodgeDirectionChangeChance = 0.35f;
    public float dodgeBurstSpeedMultiplier = 2.8f;
    public float dodgeBurstDuration = 0.35f;
    public float dodgeDecisionCooldown = 0.28f;
    public float dodgeObstacleProbeDistance = 0.85f;
    public float dodgeEdgeProbeDistance = 1.35f;
    public LayerMask dodgeObstacleMask;

    [Header("Obstacle Climb / Unstuck")]
    public bool climbObstaclesWhileChasing = true;
    public float obstacleProbeDistance = 0.55f;
    public float obstacleJumpHorizontalBoost = 1.35f;
    public float obstacleJumpCooldown = 0.35f;
    public float stuckTimeBeforeRetreat = 1.1f;

    [Header("Debug")]
    public bool debugLogs = false;
    public float debugLogInterval = 0.6f;

    [Header("Legacy Skill Prefabs")]
    [Tooltip("Prefab hieu ung tan cong huong thang trong legacy boss config.")]
    public GameObject skillBreathPrefab;
    [Tooltip("Prefab hieu ung AoE trong legacy boss config.")]
    public GameObject skillNovaPrefab;
    [Tooltip("Prefab quai spawn them theo phase.")]
    public GameObject addSpawnPrefab;

    [Header("Phase Text (optional)")]
    public TMPro.TextMeshProUGUI phaseAnnounceText;

    private EnemyHealth _health;
    private NetworkEnemyHealth _networkHealth;
    private Rigidbody2D _rb;
    private Animator _anim;
    private Collider2D _bodyCollider;

    private BossConfigData _config;
    private bool _configLoaded;

    private readonly HashSet<int> _triggeredPhases = new();
    private readonly Dictionary<string, float> _skillLastCast = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, float> _debugLogTimes = new(StringComparer.OrdinalIgnoreCase);

    private float _damageMultiplier = 1f;
    private float _speedMultiplier = 1f;
    private float _cooldownMultiplier = 1f;
    private float _defaultGravityScale = 1f;
    private float _retreatUntilTime = -1f;
    private float _retreatMinUntilTime = -1f;
    private float _retreatDistanceGoal = 0f;
    private float _retreatHorizontalDirection = 0f;
    private float _retreatSpeedMultiplierOverride = -1f;
    private float _nextRetreatEvasionTime = -1f;
    private float _nextThreatScanTime = -1f;
    private float _reengageLockedUntil = -1f;
    private float _lastRetreatJumpTime = -10f;
    private int _runtimeBaseDamageOverride = -1;
    private int _jumpsLeft = 0;
    private bool _isGrounded;
    private string _activeAttackBoolParameter;
    private Coroutine _fallThroughCoroutine;
    private Collider2D _ignoredGroundCollider;
    private Coroutine _jumpThroughCoroutine;
    private Collider2D _jumpThroughGroundCollider;
    private Collider2D _lastHigherPlatformCollider;
    private float _lastHigherPlatformTopY;
    private bool _lastHigherPlatformCloseEnough;
    private float _platformSearchDirection = 0f;
    private float _nextPlatformSearchFlipTime = -1f;
    private float _nextLifecycleLogTime = -1f;
    private float _nextGroundTraversalTime = -1f;
    private float _nextAdvancedDodgeTime = -1f;
    private float _lastObstacleJumpTime = -10f;
    private float _chaseBlockedSince = -1f;
    private int _boss25MeleeComboCount = 0;
    private bool _currentRetreatAllowsGroundTraversal = true;
    private const string Boss25LogTag = "[BOSS25]";
    private const string Boss25JumpLogTag = "[BOSS25_JUMP]";

    private enum BossState
    {
        Idle,
        Chase,
        Skill,
        Retreat,
        Dead
    }

    private BossState _state = BossState.Idle;

    public bool UsesGroundPhysics => useGroundPhysics;

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        _networkHealth = GetComponent<NetworkEnemyHealth>();
        _rb = GetComponent<Rigidbody2D>();
        _anim = GetComponent<Animator>();
        _bodyCollider = GetComponent<Collider2D>();

        ApplyBoss25JumpDefaults();

        if (_rb != null)
        {
            _defaultGravityScale = _rb.gravityScale > 0f ? _rb.gravityScale : 1f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (groundLayerMask == 0)
            groundLayerMask = LayerMask.GetMask("Ground");

        if (threatLayerMask == 0)
            threatLayerMask = Physics2D.DefaultRaycastLayers;

        if (dodgeObstacleMask == 0)
            dodgeObstacleMask = BuildDefaultDodgeObstacleMask();

        _jumpsLeft = Mathf.Max(0, maxJumps);

        ApplyMovementPhysics();
        ConfigureMeleeHitbox();

        if (_health != null)
        {
            _health.OnDeath.AddListener(OnDeath);
            _health.OnTakeDamage.AddListener(OnDamageTaken);
        }

        if (_networkHealth != null)
            _networkHealth.OnServerTakeDamage += OnNetworkDamageTaken;

        LogBossLifecycle("Awake");
    }

    private void ApplyBoss25JumpDefaults()
    {
        if (!UsesBoss25JumpRules())
            return;

        allowUntargetedJumps = false;
        requireGroundedToJump = true;
        maxJumps = 1;
        approachJumpChance = 0f;
        upperPlatformJumpEdgeDistance = Mathf.Max(upperPlatformJumpEdgeDistance, 2f);
        obstacleJumpHorizontalBoost = Mathf.Max(obstacleJumpHorizontalBoost, 2.1f);
    }

    private bool UsesBoss25JumpRules()
    {
        return bossId == 13 || gameObject.name.Contains("Enemy 25");
    }

    private void OnEnable()
    {
        ApplyMovementPhysics();
        ConfigureMeleeHitbox();
        LogBossLifecycle("OnEnable");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        LogBossLifecycle("OnNetworkSpawn");
    }

    private void OnDisable()
    {
        RestoreIgnoredGroundCollision();
        RestoreJumpThroughGroundCollision();
        ResetAttackAnimation();
    }

    public override void OnDestroy()
    {
        if (_networkHealth != null)
            _networkHealth.OnServerTakeDamage -= OnNetworkDamageTaken;

        base.OnDestroy();
    }

    private void Start()
    {
        if (useInspectorSkillsOnly)
        {
            _configLoaded = true;
        }
        else
        {
            StartCoroutine(LoadConfigFromServer());
        }

        FindNearestPlayer();

        if (useGroundPhysics && snapToGroundOnStart)
            StartCoroutine(SnapToGroundAfterSpawn());

        LogBossLifecycle("Start");
    }

    public bool SnapToGroundForServerSpawn()
    {
        if (!useGroundPhysics)
            return false;

        if (groundLayerMask == 0)
            groundLayerMask = LayerMask.GetMask("Ground");

        bool snapped = TrySnapToGround();
        if (snapped)
            UpdateGroundState();

        return snapped;
    }

    public void ApplyRuntimeOverride(int baseDamage, float runtimeChaseSpeed)
    {
        if (baseDamage > 0)
            _runtimeBaseDamageOverride = baseDamage;

        if (runtimeChaseSpeed > 0f)
            chaseSpeed = runtimeChaseSpeed;
    }

    private void ApplyMovementPhysics()
    {
        if (_rb == null)
            return;

        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (useGroundPhysics)
        {
            _rb.gravityScale = _defaultGravityScale > 0f ? _defaultGravityScale : 1f;
            if (_bodyCollider != null)
                _bodyCollider.isTrigger = false;
        }
        else
        {
            _rb.gravityScale = 0f;
        }
    }

    private void ConfigureMeleeHitbox()
    {
        if (meleeHitbox == null || meleeHitbox == _bodyCollider)
            return;

        meleeHitbox.isTrigger = true;
        meleeHitbox.usedByEffector = false;
    }

    private IEnumerator LoadConfigFromServer()
    {
        string url = $"{ServerConfig.BaseUrl}/api/dungeon/boss/{bossId}/config";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[BossAI] Khong load duoc config boss #{bossId}: {req.error}. Dung fallback inspector/default.");
            _configLoaded = true;
            yield break;
        }

        try
        {
            _config = JsonUtility.FromJson<BossConfigData>(req.downloadHandler.text);

            if (!string.IsNullOrEmpty(_config.skills_json))
                _config.skills = ParseJsonArray<SkillData>(_config.skills_json);
            if (!string.IsNullOrEmpty(_config.phases_json))
                _config.phases = ParseJsonArray<PhaseData>(_config.phases_json);

            _configLoaded = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BossAI] Parse config loi: {ex.Message}");
            _configLoaded = true;
        }
    }

    private void Update()
    {
        bool isServerContext = NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
        if (isServerContext && ShouldEmitBoss25Logs() && Time.time >= _nextLifecycleLogTime)
        {
            _nextLifecycleLogTime = Time.time + 2f;
            LogBossLifecycle("UpdateHeartbeat");
        }

        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return;

        if (!_configLoaded || _state == BossState.Dead)
            return;

        RefreshPlayerTarget();
        UpdateGroundState();
        CheckPhases();

        if (_state == BossState.Skill)
            return;

        if (TryStartThreatEvade())
        {
            HandleRetreat();
            return;
        }

        if (IsRetreating())
        {
            HandleRetreat();
            return;
        }

        if (_state == BossState.Retreat)
        {
            FinishRetreat();
            return;
        }

        if (Time.time < _reengageLockedUntil)
        {
            StopMovement();
            return;
        }

        RunStateMachine();
    }

    private void RunStateMachine()
    {
        if (playerTarget == null)
        {
            _state = BossState.Idle;
            StopMovement();
            return;
        }

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        if (dist > detectionRange)
        {
            _state = BossState.Idle;
            StopMovement();
            return;
        }

        if (HasLocalSkillsConfigured() && TryHandleLocalSkillState(dist))
            return;

        if (!useInspectorSkillsOnly && TryUseLegacySkill())
        {
            _state = BossState.Skill;
            return;
        }

        if (CanUseBasicMelee(dist))
        {
            _state = BossState.Skill;
            _skillLastCast[BasicMeleeSkillId] = Time.time;
            StartCoroutine(CastBasicMelee());
            return;
        }

        if (dist <= meleeAttackRange && IsTargetVerticallyReachableForMelee(meleeAttackRange, 0.75f))
        {
            StopMovement();
            return;
        }

        ChasePlayer(meleeAttackRange);
    }

    private bool TryHandleLocalSkillState(float dist)
    {
        LocalBossSkillConfig retreatSkill = null;
        LocalBossSkillConfig approachSkill = null;
        LocalBossSkillConfig readyApproachSkill = null;

        foreach (var skill in localSkills)
        {
            if (!IsLocalSkillConfigured(skill))
                continue;

            approachSkill ??= skill;

            if (!IsLocalSkillReady(skill))
                continue;

            readyApproachSkill ??= skill;

            float minDistance = Mathf.Max(0f, skill.minDistance);
            if (dist < minDistance)
            {
                retreatSkill ??= skill;
                continue;
            }

            if (skill.skillType == LocalBossSkillType.Melee && !IsTargetVerticallyReachableForMelee(skill.range, skill.hitRadius))
            {
                BossDebug(
                    "melee-vertical-block",
                    $"Skip melee '{ResolveLocalSkillId(skill)}': target khong cung tang. dist={dist:F2} heightDelta={(playerTarget.position.y - transform.position.y):F2}");
                approachSkill ??= skill;
                continue;
            }

            if (dist <= Mathf.Max(0.1f, skill.range))
            {
                _state = BossState.Skill;
                _skillLastCast[ResolveLocalSkillId(skill)] = Time.time;
                BossDebug(
                    "cast-local",
                    $"Cast local skill '{ResolveLocalSkillId(skill)}' type={skill.skillType} dist={dist:F2} range={skill.range:F2} visual={(skill.visualPrefab != null ? skill.visualPrefab.name : "null")}",
                    0f);
                StartCoroutine(CastLocalSkill(skill, localSkills.IndexOf(skill)));
                return true;
            }
        }

        if (retreatSkill != null)
        {
            BossDebug("retreat-too-close", $"Retreat vi dang gan hon minDistance cua skill '{ResolveLocalSkillId(retreatSkill)}'. dist={dist:F2}", 0f);
            StartRetreat(
                Mathf.Max(preferredRetreatDistance, retreatSkill.minDistance),
                postAttackRetreatSpeedMultiplier,
                postAttackRetreatMinDuration,
                0f,
                false);
            return true;
        }

        if (readyApproachSkill != null)
        {
            ChasePlayer(GetApproachStopDistance(readyApproachSkill));
            return true;
        }

        if (approachSkill != null)
        {
            if (UsesBoss25JumpRules())
            {
                BossDebug(
                    "local-skill-cooldown-chase",
                    $"Local skill dang cooldown, fallback chase/basic melee. dist={dist:F2} skill='{ResolveLocalSkillId(approachSkill)}'",
                    0.25f);
                return false;
            }

            ChasePlayer(GetApproachStopDistance(approachSkill));
            return true;
        }

        return false;
    }

    private IEnumerator CastBasicMelee()
    {
        bool useBoss25Rush = UsesBoss25JumpRules();
        if (useBoss25Rush)
            RushTowardPlayerForAttack(meleeAttackRange);
        else
            StopMovement();
        PlayAttackAnimation(DefaultAttackBoolParameter);

        yield return new WaitForSeconds(0.25f);
        if (useBoss25Rush)
            StopMovement();

        if (_state == BossState.Dead)
            yield break;

        int damage = Mathf.RoundToInt(ResolveBaseDamage() * _damageMultiplier);
        if (IsTargetVerticallyReachableForMelee(meleeAttackRange, 0.75f))
            PerformMeleeHit(damage, meleeAttackRange, 0.75f, 0.2f);
        else
            BossDebug("basic-melee-cancel", "Cancel basic melee: player da lech tang trong luc cast.", 0f);

        yield return new WaitForSeconds(0.35f);

        ResetAttackAnimation();
        if (_state == BossState.Dead)
            yield break;

        if (ShouldRetreatAfterBoss25MeleeCombo())
            StartRetreat(preferredRetreatDistance, postAttackRetreatSpeedMultiplier, postAttackRetreatMinDuration, 0f, true);
        else
            _state = BossState.Chase;
    }

    private IEnumerator CastLocalSkill(LocalBossSkillConfig skill, int skillIndex)
    {
        bool useBoss25Rush = UsesBoss25JumpRules() && skill.skillType == LocalBossSkillType.Melee;
        if (useBoss25Rush)
            RushTowardPlayerForAttack(skill.range);
        else
            StopMovement();
        PlayAttackAnimation(skill.animationParameter);

        yield return new WaitForSeconds(Mathf.Max(0f, skill.castDelay));
        if (useBoss25Rush)
            StopMovement();

        if (_state == BossState.Dead)
            yield break;

        int damage = Mathf.RoundToInt(ResolveBaseDamage() * Mathf.Max(0.1f, skill.damageMultiplier) * _damageMultiplier);
        Vector2 aimDirection = GetAimDirection(GetBaseSpawnPosition(skill));

        switch (skill.skillType)
        {
            case LocalBossSkillType.Projectile:
                SpawnProjectileSkill(skill, damage, aimDirection);
                break;

            case LocalBossSkillType.Aoe:
                CastLocalAoe(skill, skillIndex, damage, aimDirection);
                break;

            default:
                if (!IsTargetVerticallyReachableForMelee(skill.range, skill.hitRadius))
                {
                    BossDebug(
                        "local-melee-cancel",
                        $"Cancel melee '{ResolveLocalSkillId(skill)}': player da lech tang trong luc cast. heightDelta={(playerTarget != null ? playerTarget.position.y - transform.position.y : 0f):F2}",
                        0f);
                    break;
                }

                SpawnDirectionalEffect(skill, skillIndex, aimDirection);
                PerformMeleeHit(damage, skill.range, skill.hitRadius, skill.spawnOffsetY);
                break;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, skill.recoveryTime));

        ResetAttackAnimation();
        if (_state == BossState.Dead)
            yield break;

        if (UsesBoss25JumpRules() && skill.skillType == LocalBossSkillType.Melee)
        {
            if (ShouldRetreatAfterBoss25MeleeCombo())
                StartRetreat(
                    Mathf.Max(preferredRetreatDistance, skill.minDistance),
                    postAttackRetreatSpeedMultiplier,
                    postAttackRetreatMinDuration,
                    0f,
                    true);
            else
                _state = BossState.Chase;
        }
        else if (skill.retreatAfterUse || (alwaysRetreatAfterMelee && skill.skillType == LocalBossSkillType.Melee))
            StartRetreat(
                Mathf.Max(preferredRetreatDistance, skill.minDistance),
                postAttackRetreatSpeedMultiplier,
                postAttackRetreatMinDuration,
                0f,
                true);
        else
            _state = BossState.Chase;
    }

    private void RushTowardPlayerForAttack(float desiredRange)
    {
        if (_rb == null || playerTarget == null)
            return;

        float deltaX = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= Mathf.Max(0.2f, desiredRange * 0.45f))
        {
            StopMovement();
            return;
        }

        float direction = Mathf.Sign(deltaX);
        float speed = chaseSpeed * _speedMultiplier * Mathf.Max(1.5f, postAttackRetreatSpeedMultiplier);
        _rb.velocity = new Vector2(direction * speed, useGroundPhysics ? _rb.velocity.y : 0f);
        UpdateFacing(direction);
        SetMovingState(true);
        BossDebug(
            "attack-rush",
            $"Attack rush dir={direction:F0} speed={speed:F2} distX={Mathf.Abs(deltaX):F2} desiredRange={desiredRange:F2}",
            0.1f);
    }

    private bool ShouldRetreatAfterBoss25MeleeCombo()
    {
        if (!UsesBoss25JumpRules())
            return true;

        _boss25MeleeComboCount++;
        int comboLimit = 3;
        bool shouldRetreat = _boss25MeleeComboCount >= comboLimit;
        BossDebug(
            "boss25-melee-combo",
            $"Boss25 melee combo {_boss25MeleeComboCount}/{comboLimit} retreat={shouldRetreat}",
            0f);

        if (shouldRetreat)
        {
            _boss25MeleeComboCount = 0;
            return true;
        }

        _reengageLockedUntil = Mathf.Min(_reengageLockedUntil, Time.time + 0.05f);
        return false;
    }

    private void SpawnProjectileSkill(LocalBossSkillConfig skill, int damage, Vector2 aimDirection)
    {
        GameObject projectilePrefab = skill.visualPrefab != null ? skill.visualPrefab : skillBreathPrefab;
        if (projectilePrefab == null)
        {
            Debug.LogWarning($"[BossAI] {gameObject.name} skill '{ResolveLocalSkillId(skill)}' thieu projectile prefab.");
            return;
        }

        Vector3 spawnPosition = GetSpawnPosition(skill, aimDirection);
        aimDirection = GetAimDirection(spawnPosition);
        GameObject projectileObject = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        MoveSpawnedObjectToCurrentMap(projectileObject);
        ApplyMapVisibility(projectileObject, GetMyMapId());

        FireballDamage playerProjectileDamage = projectileObject.GetComponent<FireballDamage>();
        if (playerProjectileDamage != null)
            playerProjectileDamage.enabled = false;

        EnemyProjectile enemyProjectile = projectileObject.GetComponent<EnemyProjectile>();
        if (enemyProjectile == null)
            enemyProjectile = projectileObject.AddComponent<EnemyProjectile>();

        enemyProjectile.damage = damage;
        enemyProjectile.EnemyMapId = GetMyMapId();
        enemyProjectile.lifetime = skill.projectileLifetime > 0f ? skill.projectileLifetime : 3f;

        Rigidbody2D projectileRb = projectileObject.GetComponent<Rigidbody2D>();
        if (projectileRb == null)
            projectileRb = projectileObject.AddComponent<Rigidbody2D>();

        projectileRb.gravityScale = 0f;
        projectileRb.velocity = aimDirection * Mathf.Max(0.1f, skill.projectileSpeed);

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        projectileObject.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (Mathf.Abs(aimDirection.x) > 0.01f)
        {
            Vector3 localScale = projectileObject.transform.localScale;
            localScale.x = Mathf.Abs(localScale.x);
            localScale.y = aimDirection.x < 0f ? -Mathf.Abs(localScale.y) : Mathf.Abs(localScale.y);
            projectileObject.transform.localScale = localScale;
        }

        SpawnNetworkObjectIfNeeded(projectileObject);
    }

    private void SpawnDirectionalEffect(LocalBossSkillConfig skill, int skillIndex, Vector2 aimDirection)
    {
        if (skill.visualPrefab == null)
            return;

        Vector3 spawnPosition = GetSpawnPosition(skill, aimDirection);
        Vector3 effectScale = skill.visualPrefab.transform.localScale;

        if (Mathf.Abs(aimDirection.x) > 0.01f)
        {
            effectScale.x = Mathf.Abs(effectScale.x) * Mathf.Sign(aimDirection.x);
        }

        BossDebug(
            "directional-visual",
            $"Spawn directional visual '{skill.visualPrefab.name}' skillIndex={skillIndex} pos={spawnPosition} scale={effectScale} aim={aimDirection}",
            0f);
        SpawnTransientVisual(skill.visualPrefab, skillIndex, spawnPosition, Quaternion.identity, effectScale, skill.effectLifetime);
    }

    private void CastLocalAoe(LocalBossSkillConfig skill, int skillIndex, int damage, Vector2 aimDirection)
    {
        if (skill.visualPrefab != null)
        {
            Vector3 spawnPosition = GetSpawnPosition(skill, aimDirection);
            SpawnTransientVisual(skill.visualPrefab, skillIndex, spawnPosition, Quaternion.identity, skill.visualPrefab.transform.localScale, skill.effectLifetime);
        }

        float radius = Mathf.Max(0.25f, skill.hitRadius > 0f ? skill.hitRadius : skill.range);
        Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, transform.position, radius, LayerMask.GetMask("Player"));
        ApplyDamageToHitSet(hits, damage);
    }

    private bool TryUseLegacySkill()
    {
        if (_config?.skills == null || playerTarget == null)
            return false;

        foreach (var skill in _config.skills)
        {
            if (skill == null || skill.skill_id == "SUMMON_ADD")
                continue;

            float cooldown = Mathf.Max(0.05f, skill.cooldown_sec * _cooldownMultiplier);
            if (_skillLastCast.TryGetValue(skill.skill_id, out float lastCast)
                && Time.time - lastCast < cooldown)
            {
                continue;
            }

            float dist = Vector2.Distance(transform.position, playerTarget.position);
            if (dist > skill.range * 1.2f)
                continue;

            _skillLastCast[skill.skill_id] = Time.time;
            StartCoroutine(CastLegacySkill(skill));
            return true;
        }

        return false;
    }

    private IEnumerator CastLegacySkill(SkillData skill)
    {
        StopMovement();
        PlayAttackAnimation(skill.animation_trigger);

        yield return new WaitForSeconds(0.3f);

        if (_state == BossState.Dead)
            yield break;

        if (skill.aoe)
            CastLegacyAoeSkill(skill);
        else
            CastLegacyDirectSkill(skill);

        yield return new WaitForSeconds(0.5f);

        ResetAttackAnimation();
        if (_state == BossState.Dead)
            yield break;

        StartRetreat(preferredRetreatDistance, postAttackRetreatSpeedMultiplier, postAttackRetreatMinDuration, 0f, true);
    }

    private void CastLegacyDirectSkill(SkillData skill)
    {
        if (playerTarget == null)
            return;

        GameObject projectilePrefab = skillBreathPrefab;
        if (projectilePrefab == null)
            return;

        Vector2 aimDirection = GetAimDirection(transform.position);
        GameObject projectileObject = Instantiate(projectilePrefab, transform.position, Quaternion.identity);

        MoveSpawnedObjectToCurrentMap(projectileObject);
        ApplyMapVisibility(projectileObject, GetMyMapId());

        int damage = Mathf.RoundToInt(ResolveBaseDamage() * Mathf.Max(0.1f, skill.damage_multiplier) * _damageMultiplier);

        FireballDamage playerProjectileDamage = projectileObject.GetComponent<FireballDamage>();
        if (playerProjectileDamage != null)
            playerProjectileDamage.enabled = false;

        EnemyProjectile enemyProjectile = projectileObject.GetComponent<EnemyProjectile>();
        if (enemyProjectile == null)
            enemyProjectile = projectileObject.AddComponent<EnemyProjectile>();

        enemyProjectile.damage = damage;
        enemyProjectile.EnemyMapId = GetMyMapId();

        Rigidbody2D projectileRb = projectileObject.GetComponent<Rigidbody2D>();
        if (projectileRb == null)
            projectileRb = projectileObject.AddComponent<Rigidbody2D>();

        projectileRb.gravityScale = 0f;
        projectileRb.velocity = aimDirection * 8f;

        SpawnNetworkObjectIfNeeded(projectileObject);
    }

    private void CastLegacyAoeSkill(SkillData skill)
    {
        if (skillNovaPrefab != null)
        {
            GameObject effect = Instantiate(skillNovaPrefab, transform.position, Quaternion.identity);
            PrepareTransientEffect(effect, 2f);
        }

        int damage = Mathf.RoundToInt(ResolveBaseDamage() * Mathf.Max(0.1f, skill.damage_multiplier) * _damageMultiplier);
        Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, transform.position, Mathf.Max(0.25f, skill.range), LayerMask.GetMask("Player"));
        ApplyDamageToHitSet(hits, damage);
    }

    private void ChasePlayer(float stopDistance)
    {
        if (_rb == null || playerTarget == null)
            return;

        _state = BossState.Chase;

        float deltaX = playerTarget.position.x - transform.position.x;
        bool needsVerticalTraversal = NeedsVerticalTraversal();
        if (Mathf.Abs(deltaX) <= Mathf.Max(0f, stopDistance) && !needsVerticalTraversal)
        {
            StopMovement();
            HandleVerticalMovement(true);
            return;
        }

        if (Mathf.Abs(deltaX) <= Mathf.Max(0.15f, stopDistance * 0.35f) && needsVerticalTraversal)
        {
            if (playerTarget.position.y > transform.position.y + verticalTargetThreshold)
            {
                SearchForHigherPlatformEntry();
                return;
            }

            StopMovement();
            HandleVerticalMovement(true);
            return;
        }

        float horizontalDirection = Mathf.Sign(deltaX);
        float speed = chaseSpeed * _speedMultiplier;
        float yVelocity = useGroundPhysics ? _rb.velocity.y : 0f;

        if (TryHandleChaseObstacle(horizontalDirection, speed, needsVerticalTraversal))
            return;

        _rb.velocity = new Vector2(horizontalDirection * speed, yVelocity);
        UpdateFacing(horizontalDirection);
        SetMovingState(true);
        HandleVerticalMovement(true);
        if (!needsVerticalTraversal)
            TryApproachHop();
    }

    private void StopMovement()
    {
        if (_rb != null)
            _rb.velocity = new Vector2(0f, useGroundPhysics ? _rb.velocity.y : 0f);

        SetMovingState(false);
    }

    private void StartRetreat(
        float distanceGoal,
        float speedMultiplierOverride = -1f,
        float minDuration = 0f,
        float horizontalDirectionOverride = 0f,
        bool allowGroundTraversal = true)
    {
        _state = BossState.Retreat;
        if (UsesBoss25JumpRules())
            _boss25MeleeComboCount = 0;
        _currentRetreatAllowsGroundTraversal = allowGroundTraversal || !restrictGroundTraversalToChaseOrPostAttack;
        _retreatDistanceGoal = Mathf.Max(distanceGoal, preferredRetreatDistance, meleeAttackRange + 0.5f);
        float minRetreatDuration = Mathf.Max(0f, minDuration);
        _retreatUntilTime = Time.time + Mathf.Max(0.1f, retreatDuration, minRetreatDuration);
        _retreatMinUntilTime = Time.time + minRetreatDuration;
        _retreatSpeedMultiplierOverride = speedMultiplierOverride > 0f ? speedMultiplierOverride : -1f;
        _retreatHorizontalDirection = Mathf.Abs(horizontalDirectionOverride) > 0.01f
            ? Mathf.Sign(horizontalDirectionOverride)
            : ResolveRetreatDirection();
        _nextRetreatEvasionTime = Time.time;
        if (_currentRetreatAllowsGroundTraversal
            && restrictGroundTraversalToChaseOrPostAttack
            && UsesGroundTraversalCooldown())
        {
            DelayNextGroundTraversal();
        }

        BossDebug(
            "start-retreat",
            $"StartRetreat goal={_retreatDistanceGoal:F2} dir={_retreatHorizontalDirection:F0} allowGroundTraversal={_currentRetreatAllowsGroundTraversal} until={_retreatUntilTime - Time.time:F2}s min={_retreatMinUntilTime - Time.time:F2}s distToPlayer={(playerTarget != null ? Vector2.Distance(transform.position, playerTarget.position) : -1f):F2}",
            0f);
        TryRetreatEvasion(true);
    }

    private bool TryHandleChaseObstacle(float horizontalDirection, float speed, bool needsVerticalTraversal)
    {
        if (!climbObstaclesWhileChasing
            || !useGroundPhysics
            || !canJump
            || _rb == null
            || _bodyCollider == null
            || Mathf.Abs(horizontalDirection) <= 0.01f)
        {
            _chaseBlockedSince = -1f;
            return false;
        }

        bool blockedAhead = IsChaseObstacleAhead(horizontalDirection, out RaycastHit2D obstacleHit);
        bool missingGroundAhead = _isGrounded && IsGroundMissingAhead(horizontalDirection);
        bool ignoreSameLevelGround = ShouldIgnoreSameLevelGroundObstacle(obstacleHit, needsVerticalTraversal);
        if (ignoreSameLevelGround)
            blockedAhead = false;

        bool shouldClimb = UsesBoss25JumpRules()
            ? (blockedAhead || (missingGroundAhead && needsVerticalTraversal))
            : (blockedAhead || (missingGroundAhead && (needsVerticalTraversal || IsTargetAboveOrSameLevel())));

        if (!shouldClimb)
        {
            _chaseBlockedSince = -1f;
            return false;
        }

        if (_chaseBlockedSince < 0f)
            _chaseBlockedSince = Time.time;

        if (_isGrounded && Time.time - _lastObstacleJumpTime >= Mathf.Max(0.05f, obstacleJumpCooldown))
        {
            float targetY = ResolveObstacleJumpTargetY(blockedAhead, obstacleHit);
            Collider2D jumpThroughPlatform = _lastHigherPlatformCloseEnough ? _lastHigherPlatformCollider : null;
            float jumpThroughTopY = _lastHigherPlatformTopY;
            BeginJumpThroughHigherPlatform(jumpThroughPlatform, jumpThroughTopY);
            bool jumped = TryJump(true, targetY);
            if (!jumped && jumpThroughPlatform != null)
                RestoreJumpThroughGroundCollision(jumpThroughPlatform);
            if (jumped)
            {
                _lastObstacleJumpTime = Time.time;
                float boostedSpeed = speed * Mathf.Max(1f, obstacleJumpHorizontalBoost);
                _rb.velocity = new Vector2(horizontalDirection * boostedSpeed, _rb.velocity.y);
                UpdateFacing(horizontalDirection);
                SetMovingState(true);
                BossDebug(
                    "chase-obstacle-jump",
                    $"dir={horizontalDirection:F0} speed={boostedSpeed:F2} blocked={blockedAhead} missingGround={missingGroundAhead} hit={(obstacleHit.collider != null ? obstacleHit.collider.name : "none")}",
                    0.05f);
                return true;
            }
        }

        if (Time.time - _chaseBlockedSince >= Mathf.Max(0.2f, stuckTimeBeforeRetreat))
        {
            BossDebug(
                "chase-obstacle-retreat",
                $"Obstacle stuck for {Time.time - _chaseBlockedSince:F2}s. Start short reposition. hit={(obstacleHit.collider != null ? obstacleHit.collider.name : "none")}",
                0.05f);
            StartRetreat(
                Mathf.Max(preferredRetreatDistance * 0.5f, meleeAttackRange + 0.75f),
                postAttackRetreatSpeedMultiplier,
                Mathf.Min(0.6f, postAttackRetreatMinDuration),
                -horizontalDirection,
                true);
            _chaseBlockedSince = -1f;
            return true;
        }

        return false;
    }

    private bool ShouldIgnoreSameLevelGroundObstacle(RaycastHit2D obstacleHit, bool needsVerticalTraversal)
    {
        if (!UsesBoss25JumpRules()
            || needsVerticalTraversal
            || !_isGrounded
            || _bodyCollider == null
            || obstacleHit.collider == null
            || !IsUsableGroundCollider(obstacleHit.collider))
        {
            return false;
        }

        Bounds bodyBounds = _bodyCollider.bounds;
        Bounds obstacleBounds = obstacleHit.collider.bounds;
        float floorTolerance = Mathf.Max(0.25f, groundCheckRadius * 2.5f);
        bool sameFloorTop = obstacleBounds.max.y <= bodyBounds.min.y + floorTolerance;
        bool playerIsSameLevel = playerTarget == null || Mathf.Abs(playerTarget.position.y - transform.position.y) <= verticalTargetThreshold;
        if (!sameFloorTop || !playerIsSameLevel)
            return false;

        BossJumpDebug(
            "same-level-ground-ignore",
            $"Ignore same-level ground obstacle collider={obstacleHit.collider.name} hit={obstacleHit.point} bossFeet={bodyBounds.min.y:F2} groundTop={obstacleBounds.max.y:F2} playerPos={(playerTarget != null ? playerTarget.position : Vector3.zero)}",
            0.25f);
        return true;
    }

    private bool IsTargetAboveOrSameLevel()
    {
        return playerTarget == null || playerTarget.position.y >= transform.position.y - verticalTargetThreshold;
    }

    private bool IsChaseObstacleAhead(float horizontalDirection, out RaycastHit2D hit)
    {
        hit = default;
        if (_bodyCollider == null)
            return false;

        int obstacleMask = BuildChaseObstacleMask();
        if (obstacleMask == 0)
            return false;

        Bounds bounds = _bodyCollider.bounds;
        Vector2 direction = horizontalDirection > 0f ? Vector2.right : Vector2.left;
        float originX = horizontalDirection > 0f ? bounds.max.x + 0.03f : bounds.min.x - 0.03f;
        float distance = Mathf.Max(0.1f, obstacleProbeDistance);
        Vector2[] origins =
        {
            new Vector2(originX, bounds.center.y + bounds.extents.y * 0.2f),
            new Vector2(originX, bounds.center.y - bounds.extents.y * 0.25f),
            new Vector2(originX, bounds.min.y + 0.12f)
        };

        foreach (Vector2 origin in origins)
        {
            RaycastHit2D candidate = RaycastInCurrentScene(origin, direction, distance, obstacleMask);
            if (candidate.collider == null || IsTemporarilyIgnoredGround(candidate.collider))
                continue;

            if (!IsUsableTraversalBlocker(candidate.collider))
                continue;

            hit = candidate;
            return true;
        }

        return false;
    }

    private bool IsGroundMissingAhead(float horizontalDirection)
    {
        if (_bodyCollider == null || groundLayerMask == 0)
            return false;

        Bounds bounds = _bodyCollider.bounds;
        float edgeX = horizontalDirection > 0f
            ? bounds.max.x + Mathf.Max(0.2f, obstacleProbeDistance * 0.8f)
            : bounds.min.x - Mathf.Max(0.2f, obstacleProbeDistance * 0.8f);
        float originY = bounds.center.y + bounds.extents.y * 0.15f;
        float distance = bounds.extents.y + Mathf.Max(0.35f, dodgeEdgeProbeDistance);
        RaycastHit2D groundHit = RaycastInCurrentScene(new Vector2(edgeX, originY), Vector2.down, distance, groundLayerMask);
        return !IsUsableGroundCollider(groundHit.collider);
    }

    private float ResolveObstacleJumpTargetY(bool blockedAhead, RaycastHit2D obstacleHit)
    {
        float targetY = float.NaN;
        bool foundPlatform = TryResolveHigherPlatformTarget(out float platformTargetY, out _, out bool closeEnoughToPlatform);
        if (foundPlatform && (!UsesBoss25JumpRules() || closeEnoughToPlatform))
            targetY = platformTargetY;

        if (blockedAhead && obstacleHit.collider != null)
        {
            float obstacleTopY = obstacleHit.point.y + Mathf.Max(0f, obstacleJumpHeightPadding);
            targetY = float.IsNaN(targetY) ? obstacleTopY : Mathf.Max(targetY, obstacleTopY);
        }

        if (playerTarget != null && playerTarget.position.y > transform.position.y + verticalTargetThreshold)
        {
            float playerTargetY = playerTarget.position.y + Mathf.Max(0f, jumpHeightPadding);
            if (!UsesBoss25JumpRules() || closeEnoughToPlatform || blockedAhead)
                targetY = float.IsNaN(targetY) ? playerTargetY : Mathf.Max(targetY, playerTargetY);
        }

        return targetY;
    }

    private int BuildChaseObstacleMask()
    {
        int mask = 0;
        if (groundLayerMask.value != 0)
            mask |= groundLayerMask.value;
        if (dodgeObstacleMask.value != 0)
            mask |= dodgeObstacleMask.value;

        int wallLayer = LayerMask.NameToLayer("Wall");
        int maxMapLayer = LayerMask.NameToLayer("MaxMap");
        if (wallLayer >= 0)
            mask |= 1 << wallLayer;
        if (maxMapLayer >= 0)
            mask |= 1 << maxMapLayer;

        return mask;
    }

    private bool IsRetreating()
    {
        return _retreatUntilTime > Time.time;
    }

    private void HandleRetreat()
    {
        if (_rb == null || playerTarget == null)
        {
            FinishRetreat(BossState.Idle);
            return;
        }

        RefreshRetreatAgainstThreat();

        float dist = Vector2.Distance(transform.position, playerTarget.position);
        if (dist >= _retreatDistanceGoal && Time.time >= _retreatMinUntilTime)
        {
            FinishRetreat();
            return;
        }

        _state = BossState.Retreat;

        if (Mathf.Abs(_retreatHorizontalDirection) <= 0.01f)
            _retreatHorizontalDirection = ResolveRetreatDirection();

        float horizontalDirection = ResolveSafeHorizontalDodgeDirection(_retreatHorizontalDirection);
        _retreatHorizontalDirection = horizontalDirection;
        float activeRetreatMultiplier = _retreatSpeedMultiplierOverride > 0f
            ? _retreatSpeedMultiplierOverride
            : retreatSpeedMultiplier;
        float retreatSpeed = chaseSpeed * Mathf.Max(0.1f, activeRetreatMultiplier) * _speedMultiplier;
        float yVelocity = useGroundPhysics ? _rb.velocity.y : 0f;

        _rb.velocity = new Vector2(horizontalDirection * retreatSpeed, yVelocity);
        UpdateFacing(horizontalDirection);
        SetMovingState(true);
        BossDebug("handle-retreat", $"Retreat moving dir={horizontalDirection:F0} speed={retreatSpeed:F2} dist={dist:F2}/{_retreatDistanceGoal:F2}");
        TryRetreatEvasion(false);
    }

    private void FinishRetreat(BossState nextState = BossState.Chase)
    {
        _retreatUntilTime = -1f;
        _retreatMinUntilTime = -1f;
        _retreatSpeedMultiplierOverride = -1f;
        _currentRetreatAllowsGroundTraversal = true;
        _reengageLockedUntil = Time.time + Mathf.Max(0f, reengageDelayAfterRetreat);
        _state = nextState;
        StopMovement();
    }

    private void HandleVerticalMovement(bool movingTowardTarget)
    {
        if (!useGroundPhysics || playerTarget == null)
            return;

        float heightDelta = playerTarget.position.y - transform.position.y;
        if (heightDelta > verticalTargetThreshold)
        {
            SearchForHigherPlatformEntry();
            return;
        }

        if (heightDelta < -verticalTargetThreshold)
        {
            if (!TryFallThroughPlatform(true))
                SearchForLowerPlatformExit();
            return;
        }

        if (!movingTowardTarget && _isGrounded && playerTarget.position.y > transform.position.y + 0.15f)
            TryRetreatJump();
    }

    private bool NeedsVerticalTraversal()
    {
        return useGroundPhysics
            && playerTarget != null
            && Mathf.Abs(playerTarget.position.y - transform.position.y) > verticalTargetThreshold;
    }

    private bool CanStartGroundTraversal(bool ignoreCooldown = false)
    {
        if (restrictGroundTraversalToChaseOrPostAttack)
        {
            bool allowedByState = _state == BossState.Chase
                || (_state == BossState.Retreat && _currentRetreatAllowsGroundTraversal);
            if (!allowedByState)
            {
                BossDebug("ground-traversal-state-block", $"Ground traversal blocked by state={_state} allowRetreat={_currentRetreatAllowsGroundTraversal}");
                return false;
            }
        }

        if (!ignoreCooldown && UsesGroundTraversalCooldown() && Time.time < _nextGroundTraversalTime)
        {
            BossDebug("ground-traversal-cooldown", $"Ground traversal cooldown remain={_nextGroundTraversalTime - Time.time:F2}s");
            return false;
        }

        return true;
    }

    private bool UsesGroundTraversalCooldown()
    {
        return Mathf.Max(groundTraversalCooldownMin, groundTraversalCooldownMax) > 0f;
    }

    private void MarkGroundTraversalUsed()
    {
        if (!UsesGroundTraversalCooldown())
            return;

        _nextGroundTraversalTime = Time.time + CreateGroundTraversalDelay();
    }

    private void DelayNextGroundTraversal()
    {
        float nextAllowedTime = Time.time + CreateGroundTraversalDelay();
        _nextGroundTraversalTime = Mathf.Max(_nextGroundTraversalTime, nextAllowedTime);
    }

    private float CreateGroundTraversalDelay()
    {
        float minDelay = Mathf.Max(0f, Mathf.Min(groundTraversalCooldownMin, groundTraversalCooldownMax));
        float maxDelay = Mathf.Max(minDelay, Mathf.Max(groundTraversalCooldownMin, groundTraversalCooldownMax));
        return Mathf.Approximately(minDelay, maxDelay)
            ? maxDelay
            : UnityEngine.Random.Range(minDelay, maxDelay);
    }

    private int ResolveGroundTraversalBlockerMask()
    {
        if (groundTraversalBlockerMask.value != 0)
            return groundTraversalBlockerMask.value;

        if (!restrictGroundTraversalToChaseOrPostAttack)
            return 0;

        int maxMapLayer = LayerMask.NameToLayer("MaxMap");
        return maxMapLayer >= 0 ? 1 << maxMapLayer : 0;
    }

    private bool IsJumpBlockedByGroundTraversalMask()
    {
        int blockerMask = ResolveGroundTraversalBlockerMask();
        if (blockerMask == 0 || _bodyCollider == null)
            return false;

        Bounds bounds = _bodyCollider.bounds;
        float inset = Mathf.Min(bounds.extents.x * 0.55f, 0.25f);
        float probeDistance = Mathf.Max(0.75f, Mathf.Min(Mathf.Max(jumpForce, verticalTargetThreshold + 1f), groundSearchDistance));
        float originY = bounds.max.y + 0.03f;
        float leftX = bounds.min.x + inset;
        float centerX = bounds.center.x;
        float rightX = bounds.max.x - inset;

        if (TryRaycastTraversalBlocker(new Vector2(centerX, originY), Vector2.up, probeDistance, blockerMask, out RaycastHit2D upHit)
            || TryRaycastTraversalBlocker(new Vector2(leftX, originY), Vector2.up, probeDistance, blockerMask, out upHit)
            || TryRaycastTraversalBlocker(new Vector2(rightX, originY), Vector2.up, probeDistance, blockerMask, out upHit))
        {
            BossDebug("jump-blocker", $"Jump blocked by {upHit.collider.name} distance={upHit.distance:F2}", 0.2f);
            return true;
        }

        if (playerTarget != null)
        {
            Vector2 origin = bounds.center;
            Vector2 targetPoint = GetTargetMeleeBounds(playerTarget).center;
            Vector2 toTarget = targetPoint - origin;
            float targetDistance = toTarget.magnitude;
            if (targetDistance > 0.05f
                && TryRaycastTraversalBlocker(origin, toTarget.normalized, targetDistance, blockerMask, out RaycastHit2D targetHit))
            {
                BossDebug("jump-target-blocker", $"Jump target path blocked by {targetHit.collider.name} distance={targetHit.distance:F2}", 0.2f);
                return true;
            }
        }

        return false;
    }

    private bool IsFallThroughBlockedByGroundTraversalMask(Collider2D currentPlatform)
    {
        int blockerMask = ResolveGroundTraversalBlockerMask();
        if (blockerMask == 0 || _bodyCollider == null || currentPlatform == null)
            return false;

        Bounds bounds = _bodyCollider.bounds;
        float inset = Mathf.Min(bounds.extents.x * 0.55f, 0.25f);
        float leftX = bounds.min.x + inset;
        float centerX = bounds.center.x;
        float rightX = bounds.max.x - inset;
        float startY = currentPlatform.bounds.min.y - 0.05f;
        float distance = Mathf.Max(0.5f, groundSearchDistance);

        Vector2[] origins =
        {
            new Vector2(centerX, startY),
            new Vector2(leftX, startY),
            new Vector2(rightX, startY)
        };

        bool foundBlocker = false;
        foreach (Vector2 origin in origins)
        {
            if (!TryRaycastTraversalBlocker(origin, Vector2.down, distance, blockerMask, out RaycastHit2D blockerHit))
                continue;

            foundBlocker = true;
            if (HasGroundBelowAtPointBeforeDistance(currentPlatform, origin, blockerHit.distance))
                return false;
        }

        if (foundBlocker)
            BossDebug("fall-through-blocker", "FallThrough blocked by groundTraversalBlocker before any lower ground.", 0.2f);

        return foundBlocker;
    }

    private bool HasGroundBelowAtPointBeforeDistance(Collider2D currentPlatform, Vector2 origin, float maxDistance)
    {
        RaycastHit2D[] hits = new RaycastHit2D[16];
        int hitCount = RaycastAllInCurrentScene(origin, Vector2.down, Mathf.Max(0f, maxDistance), groundLayerMask, hits);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hits[i];
            if (!IsUsableGroundCollider(hit.collider) || hit.collider == currentPlatform)
                continue;

            return true;
        }

        return false;
    }

    private bool TryRaycastTraversalBlocker(Vector2 origin, Vector2 direction, float distance, int layerMask, out RaycastHit2D hit)
    {
        hit = RaycastInCurrentScene(origin, direction, distance, layerMask);
        return IsUsableTraversalBlocker(hit.collider);
    }

    private bool IsUsableTraversalBlocker(Collider2D collider)
    {
        if (collider == null || collider == _bodyCollider)
            return false;

        return !collider.transform.IsChildOf(transform);
    }

    private void SearchForHigherPlatformEntry()
    {
        if (!useGroundPhysics || _rb == null || playerTarget == null)
        {
            BossJumpDebug(
                "search-up-disabled",
                $"SearchUp disabled useGroundPhysics={useGroundPhysics} rb={(_rb != null)} target={(playerTarget != null ? playerTarget.name : "null")}",
                0.2f);
            return;
        }

        float deltaX = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) > 0.3f)
        {
            _platformSearchDirection = Mathf.Sign(deltaX);
            _nextPlatformSearchFlipTime = Time.time + 2.5f;
        }
        else
        {
            if (Mathf.Abs(_platformSearchDirection) <= 0.01f)
            {
                _platformSearchDirection = GetFacingSign();
                _nextPlatformSearchFlipTime = Time.time + 2.5f;
            }
            else if (Time.time >= _nextPlatformSearchFlipTime)
            {
                _platformSearchDirection *= -1f;
                _nextPlatformSearchFlipTime = Time.time + 2.5f;
            }
        }

        float targetY = float.NaN;
        bool foundPlatformTarget = false;
        bool closeEnoughToJump = false;
        if (TryResolveHigherPlatformTarget(out float platformTargetY, out float platformDirection, out bool resolvedCloseEnoughToJump))
        {
            foundPlatformTarget = true;
            closeEnoughToJump = resolvedCloseEnoughToJump;
            targetY = platformTargetY;
            _platformSearchDirection = platformDirection;
            if (!closeEnoughToJump)
                _nextPlatformSearchFlipTime = Time.time + 1.2f;
        }

        bool jumped = false;
        string jumpDecision = "not-grounded";
        if (_isGrounded && foundPlatformTarget && Mathf.Abs(playerTarget.position.y - transform.position.y) > verticalTargetThreshold)
        {
            bool canJumpNow = closeEnoughToJump;
            if (canJumpNow)
            {
                Collider2D jumpThroughPlatform = _lastHigherPlatformCollider;
                float jumpThroughTopY = _lastHigherPlatformTopY;
                BeginJumpThroughHigherPlatform(jumpThroughPlatform, jumpThroughTopY);
                jumped = TryJump(true, targetY);
                if (!jumped && jumpThroughPlatform != null)
                    RestoreJumpThroughGroundCollision(jumpThroughPlatform);
                jumpDecision = jumped ? "jumped" : "try-jump-failed";
            }
            else
            {
                jumpDecision = "move-to-platform-edge";
            }
        }
        else if (_isGrounded && !foundPlatformTarget)
        {
            jumpDecision = "searching-no-platform-target";
        }
        else if (_isGrounded)
        {
            jumpDecision = "height-delta-too-small";
        }

        float speed = chaseSpeed * _speedMultiplier;
        float activeSpeed = jumped ? speed * Mathf.Max(1f, obstacleJumpHorizontalBoost) : speed;
        float yVelocity = useGroundPhysics ? _rb.velocity.y : 0f;
        _rb.velocity = new Vector2(_platformSearchDirection * activeSpeed, yVelocity);
        UpdateFacing(_platformSearchDirection);
        SetMovingState(true);
        BossDebug("search-up", $"Search higher platform dir={_platformSearchDirection:F0} jumped={jumped} targetY={(float.IsNaN(targetY) ? -999f : targetY):F2} heightDelta={(playerTarget.position.y - transform.position.y):F2}");
        BossJumpDebug(
            "search-up-decision",
            $"SearchUp decision={jumpDecision} foundPlatform={foundPlatformTarget} close={closeEnoughToJump} grounded={_isGrounded} jumpsLeft={_jumpsLeft} dir={_platformSearchDirection:F0} speed={activeSpeed:F2} bossPos={transform.position} playerPos={playerTarget.position} targetY={(float.IsNaN(targetY) ? -999f : targetY):F2} vel={(_rb != null ? _rb.velocity : Vector2.zero)} nextTraversal={Mathf.Max(0f, _nextGroundTraversalTime - Time.time):F2}s",
            0.15f);
    }

    private bool TryResolveHigherPlatformTarget(out float targetY, out float horizontalDirection, out bool closeEnoughToJump)
    {
        targetY = float.NaN;
        horizontalDirection = 0f;
        closeEnoughToJump = false;
        _lastHigherPlatformCollider = null;
        _lastHigherPlatformTopY = 0f;
        _lastHigherPlatformCloseEnough = false;

        if (_bodyCollider == null || playerTarget == null || groundLayerMask == 0)
        {
            BossJumpDebug(
                "scan-up-disabled",
                $"ScanUp disabled body={(_bodyCollider != null)} target={(playerTarget != null ? playerTarget.name : "null")} groundMask={groundLayerMask.value}",
                0.25f);
            return false;
        }

        Bounds bodyBounds = _bodyCollider.bounds;
        float currentFeetY = bodyBounds.min.y;
        float scanHalfWidth = Mathf.Max(0.5f, upperPlatformSearchHorizontalRange) * 0.5f;
        float minX = Mathf.Min(transform.position.x, playerTarget.position.x) - scanHalfWidth;
        float maxX = Mathf.Max(transform.position.x, playerTarget.position.x) + scanHalfWidth;
        float scanTopY = Mathf.Max(playerTarget.position.y + Mathf.Max(1f, upperPlatformSearchVerticalRange * 0.5f), currentFeetY + Mathf.Max(2f, upperPlatformSearchVerticalRange));
        float scanDistance = Mathf.Max(2f, scanTopY - currentFeetY + 1f);
        int sampleCount = 9;

        bool found = false;
        float bestScore = float.MaxValue;
        RaycastHit2D bestHit = default;
        RaycastHit2D[] hits = new RaycastHit2D[16];
        int usableCandidateCount = 0;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = sampleCount <= 1 ? 0.5f : i / (float)(sampleCount - 1);
            float sampleX = Mathf.Lerp(minX, maxX, t);
            int hitCount = RaycastAllInCurrentScene(new Vector2(sampleX, scanTopY), Vector2.down, scanDistance, groundLayerMask, hits);
            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                RaycastHit2D hit = hits[hitIndex];
                if (!IsUsableGroundCollider(hit.collider) || IsTemporarilyIgnoredGround(hit.collider))
                    continue;

                if (hit.point.y <= currentFeetY + Mathf.Max(0.1f, verticalTargetThreshold * 0.5f))
                    continue;

                usableCandidateCount++;
                float playerYScore = Mathf.Abs(hit.point.y - playerTarget.position.y) * 2.5f;
                float playerXScore = Mathf.Abs(hit.point.x - playerTarget.position.x) * 0.6f;
                float bossXScore = Mathf.Abs(hit.point.x - transform.position.x) * 0.15f;
                float score = playerYScore + playerXScore + bossXScore;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestHit = hit;
                found = true;
            }
        }

        if (!found)
        {
            BossJumpDebug(
                "scan-up-none",
                $"ScanUp no platform candidates={usableCandidateCount} bossFeetY={currentFeetY:F2} playerPos={playerTarget.position} scanX={minX:F2}->{maxX:F2} scanTopY={scanTopY:F2} distance={scanDistance:F2} groundMask={groundLayerMask.value}",
                0.25f);
            return false;
        }

        targetY = bestHit.point.y + Mathf.Max(0f, jumpHeightPadding);
        _lastHigherPlatformCollider = bestHit.collider;
        _lastHigherPlatformTopY = bestHit.point.y;
        Bounds platformBounds = bestHit.collider.bounds;
        float bodyHalfWidth = Mathf.Max(0.1f, bodyBounds.extents.x);
        float entryPadding = bodyHalfWidth + 0.18f;
        float leftEdgeEntryX = platformBounds.min.x - bodyHalfWidth - 0.08f;
        float rightEdgeEntryX = platformBounds.max.x + bodyHalfWidth + 0.08f;
        bool bossIsLeftOfPlatform = transform.position.x < platformBounds.min.x;
        bool bossIsRightOfPlatform = transform.position.x > platformBounds.max.x;
        bool useLeftEdge = bossIsLeftOfPlatform || (!bossIsRightOfPlatform && Mathf.Abs(leftEdgeEntryX - bestHit.point.x) <= Mathf.Abs(rightEdgeEntryX - bestHit.point.x));
        float localEntrySide = transform.position.x <= bestHit.point.x ? -1f : 1f;
        float localEntryX = bestHit.point.x + localEntrySide * entryPadding;
        float nearestEdgeEntryX = useLeftEdge ? leftEdgeEntryX : rightEdgeEntryX;
        float nearestEdgeGapFromHit = Mathf.Abs(nearestEdgeEntryX - bestHit.point.x);
        float nearestEdgeGapFromBoss = Mathf.Abs(nearestEdgeEntryX - transform.position.x);
        float maxReasonableEdgeGap = Mathf.Max(2f, upperPlatformSearchHorizontalRange * 0.65f);
        bool bossOutsidePlatform = bossIsLeftOfPlatform || bossIsRightOfPlatform;
        bool usePlatformEdgeEntry = UsesBoss25JumpRules()
            ? bossOutsidePlatform && nearestEdgeGapFromBoss <= Mathf.Max(2f, upperPlatformSearchHorizontalRange)
            : nearestEdgeGapFromHit <= maxReasonableEdgeGap;
        float entryX = usePlatformEdgeEntry ? nearestEdgeEntryX : localEntryX;
        string entryMode = usePlatformEdgeEntry ? "edge" : "local";

        float desiredX = entryX;
        float deltaX = desiredX - transform.position.x;
        float jumpEntryDistance = Mathf.Max(0.25f, upperPlatformJumpEdgeDistance);
        if (UsesBoss25JumpRules())
            jumpEntryDistance = Mathf.Max(jumpEntryDistance, bodyHalfWidth + 1.6f);
        bool isNearEntry = Mathf.Abs(deltaX) <= jumpEntryDistance;
        if (Mathf.Abs(deltaX) <= 0.05f)
            deltaX = bestHit.point.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= 0.05f)
            deltaX = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) <= 0.05f)
            deltaX = GetFacingSign();

        horizontalDirection = Mathf.Sign(deltaX);
        closeEnoughToJump = isNearEntry;
        _lastHigherPlatformCloseEnough = closeEnoughToJump;
        BossJumpDebug(
            "scan-up-found",
            $"ScanUp found collider={bestHit.collider.name} hitPoint={bestHit.point} targetY={targetY:F2} candidates={usableCandidateCount} score={bestScore:F2} dir={horizontalDirection:F0} close={closeEnoughToJump} bossX={transform.position.x:F2} entryX={entryX:F2} entryDist={Mathf.Abs(entryX - transform.position.x):F2}/{jumpEntryDistance:F2} bounds=({platformBounds.min.x:F2},{platformBounds.max.x:F2}) entryMode={entryMode} edgeGap={nearestEdgeGapFromHit:F2}/{maxReasonableEdgeGap:F2} edgeBossGap={nearestEdgeGapFromBoss:F2}",
            0.15f);
        return true;
    }

    private float ResolveRetreatDirection()
    {
        if (playerTarget == null)
            return -GetFacingSign();

        float deltaX = transform.position.x - playerTarget.position.x;
        if (Mathf.Abs(deltaX) > 0.05f)
            return Mathf.Sign(deltaX);

        return -GetFacingSign();
    }

    private bool TryStartThreatEvade()
    {
        if (!evadeIncomingPlayerThreats || _rb == null || Time.time < _nextThreatScanTime)
            return false;

        _nextThreatScanTime = Time.time + Mathf.Max(0.02f, threatDirectionRefreshInterval);

        if (!TryResolveIncomingThreat(out Vector2 awayDirection))
            return false;

        StartThreatRetreat(awayDirection);
        return true;
    }

    private void RefreshRetreatAgainstThreat()
    {
        if (!evadeIncomingPlayerThreats || Time.time < _nextThreatScanTime)
            return;

        _nextThreatScanTime = Time.time + Mathf.Max(0.02f, threatDirectionRefreshInterval);

        if (!TryResolveIncomingThreat(out Vector2 awayDirection))
            return;

        _retreatHorizontalDirection = ResolveHorizontalEvadeDirection(awayDirection);
        TryAdvancedThreatDodge(awayDirection, false);
        _retreatUntilTime = Mathf.Max(_retreatUntilTime, Time.time + Mathf.Max(0.1f, threatEvadeDuration));
        _retreatMinUntilTime = Mathf.Max(_retreatMinUntilTime, Time.time + Mathf.Max(0f, postAttackRetreatMinDuration));
        _retreatSpeedMultiplierOverride = Mathf.Max(_retreatSpeedMultiplierOverride, postAttackRetreatSpeedMultiplier);
        TryThreatVerticalEvasion(awayDirection, true);
    }

    private void StartThreatRetreat(Vector2 awayDirection)
    {
        float horizontalDirection = ResolveHorizontalEvadeDirection(awayDirection);
        float distanceGoal = Mathf.Max(preferredRetreatDistance, meleeAttackRange + 2f);
        StartRetreat(distanceGoal, postAttackRetreatSpeedMultiplier, postAttackRetreatMinDuration, horizontalDirection, false);
        TryAdvancedThreatDodge(awayDirection, true);
        TryThreatVerticalEvasion(awayDirection, true);
    }

    private float ResolveHorizontalEvadeDirection(Vector2 awayDirection)
    {
        if (Mathf.Abs(awayDirection.x) > 0.05f)
            return Mathf.Sign(awayDirection.x);

        return ResolveRetreatDirection();
    }

    private bool TryAdvancedThreatDodge(Vector2 awayDirection, bool forceDecision)
    {
        if (!useAdvancedDodge || _rb == null)
            return false;

        if (!forceDecision && Time.time < _nextAdvancedDodgeTime)
            return false;

        _nextAdvancedDodgeTime = Time.time + Mathf.Max(0.03f, dodgeDecisionCooldown);

        float horizontalDirection = ResolveSafeHorizontalDodgeDirection(ResolveHorizontalEvadeDirection(awayDirection));
        if (Mathf.Abs(horizontalDirection) > 0.01f
            && UnityEngine.Random.value < dodgeDirectionChangeChance)
        {
            float alternateDirection = -horizontalDirection;
            if (!IsHorizontalDodgeBlocked(alternateDirection) && DoesDirectionKeepDistanceFromPlayer(alternateDirection))
                horizontalDirection = alternateDirection;
        }

        if (Mathf.Abs(horizontalDirection) > 0.01f)
            _retreatHorizontalDirection = horizontalDirection;

        _retreatSpeedMultiplierOverride = Mathf.Max(_retreatSpeedMultiplierOverride, dodgeBurstSpeedMultiplier);
        _retreatUntilTime = Mathf.Max(_retreatUntilTime, Time.time + Mathf.Max(0.05f, dodgeBurstDuration));
        _retreatMinUntilTime = Mathf.Max(_retreatMinUntilTime, Time.time + Mathf.Max(0.05f, dodgeBurstDuration * 0.5f));

        bool verticalDodge = false;
        if (useGroundPhysics && _isGrounded)
        {
            bool preferDrop = awayDirection.y < -0.2f || (Mathf.Abs(awayDirection.y) <= 0.2f && UnityEngine.Random.value < dodgeDropChance);
            if (preferDrop && UnityEngine.Random.value < dodgeDropChance)
                verticalDodge = TryFallThroughPlatform();

            if (!verticalDodge && UnityEngine.Random.value < dodgeJumpChance)
                verticalDodge = TryRetreatJump(true);

            if (!verticalDodge && Mathf.Abs(horizontalDirection) <= 0.01f)
                verticalDodge = TryRetreatJump(true) || TryFallThroughPlatform();
        }

        BossDebug(
            "advanced-dodge",
            $"Advanced dodge dir={_retreatHorizontalDirection:F0} away={awayDirection} vertical={verticalDodge} burst={_retreatSpeedMultiplierOverride:F1} blocked={IsHorizontalDodgeBlocked(_retreatHorizontalDirection)}",
            0f);

        return true;
    }

    private float ResolveSafeHorizontalDodgeDirection(float desiredDirection)
    {
        float direction = Mathf.Abs(desiredDirection) > 0.01f
            ? Mathf.Sign(desiredDirection)
            : ResolveRetreatDirection();

        if (!IsHorizontalDodgeBlocked(direction))
            return direction;

        float oppositeDirection = -direction;
        if (!IsHorizontalDodgeBlocked(oppositeDirection))
        {
            BossDebug("dodge-direction-switch", $"Switch dodge dir {direction:F0} -> {oppositeDirection:F0}: desired path blocked.", 0.1f);
            return oppositeDirection;
        }

        BossDebug("dodge-direction-blocked", $"Both horizontal dodge paths blocked. dir={direction:F0}", 0.1f);
        return 0f;
    }

    private bool DoesDirectionKeepDistanceFromPlayer(float direction)
    {
        if (playerTarget == null)
            return true;

        float currentDistance = Mathf.Abs(transform.position.x - playerTarget.position.x);
        float projectedX = transform.position.x + Mathf.Sign(direction) * Mathf.Max(0.25f, dodgeObstacleProbeDistance);
        float projectedDistance = Mathf.Abs(projectedX - playerTarget.position.x);
        return projectedDistance >= currentDistance - 0.35f;
    }

    private bool IsHorizontalDodgeBlocked(float direction)
    {
        if (_bodyCollider == null || Mathf.Abs(direction) <= 0.01f)
            return false;

        Bounds bounds = _bodyCollider.bounds;
        Vector2 rayDirection = direction > 0f ? Vector2.right : Vector2.left;
        float originX = direction > 0f ? bounds.max.x + 0.03f : bounds.min.x - 0.03f;
        float rayDistance = Mathf.Max(0.1f, dodgeObstacleProbeDistance);
        int obstacleMask = dodgeObstacleMask.value != 0 ? dodgeObstacleMask.value : BuildDefaultDodgeObstacleMask().value;

        if (obstacleMask != 0)
        {
            float highY = bounds.center.y + bounds.extents.y * 0.35f;
            float lowY = bounds.center.y - bounds.extents.y * 0.25f;
            if (RaycastInCurrentScene(new Vector2(originX, highY), rayDirection, rayDistance, obstacleMask).collider != null
                || RaycastInCurrentScene(new Vector2(originX, lowY), rayDirection, rayDistance, obstacleMask).collider != null)
            {
                return true;
            }
        }

        if (!useGroundPhysics || !_isGrounded || groundLayerMask == 0)
            return false;

        float edgeX = direction > 0f ? bounds.max.x + 0.25f : bounds.min.x - 0.25f;
        float downDistance = bounds.extents.y + Mathf.Max(0.2f, dodgeEdgeProbeDistance);
        RaycastHit2D groundHit = RaycastInCurrentScene(new Vector2(edgeX, bounds.center.y), Vector2.down, downDistance, groundLayerMask);
        return !IsUsableGroundCollider(groundHit.collider);
    }

    private LayerMask BuildDefaultDodgeObstacleMask()
    {
        int mask = 0;
        int maxMapLayer = LayerMask.NameToLayer("MaxMap");
        int wallLayer = LayerMask.NameToLayer("Wall");
        if (maxMapLayer >= 0)
            mask |= 1 << maxMapLayer;
        if (wallLayer >= 0)
            mask |= 1 << wallLayer;
        return mask;
    }

    private bool TryResolveIncomingThreat(out Vector2 awayDirection)
    {
        awayDirection = Vector2.zero;

        float scanRadius = Mathf.Max(0.5f, threatScanRadius);
        int layerMask = threatLayerMask == 0 ? Physics2D.DefaultRaycastLayers : threatLayerMask;
        Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, transform.position, scanRadius, layerMask);
        if (hits.Length == 0)
            return false;

        Vector2 bossPosition = transform.position;
        float bestScore = float.MaxValue;
        Vector2 bestAway = Vector2.zero;

        foreach (Collider2D hit in hits)
        {
            if (!IsPlayerThreatCollider(hit) || !IsSameMapAsThreat(hit))
                continue;

            Vector2 threatPosition = hit.bounds.center;
            Vector2 toBoss = bossPosition - threatPosition;
            float distance = Mathf.Max(0.01f, toBoss.magnitude);
            if (!IsThreatApproaching(hit, toBoss, distance, scanRadius))
                continue;

            float score = distance;
            Rigidbody2D threatBody = hit.attachedRigidbody != null ? hit.attachedRigidbody : hit.GetComponentInParent<Rigidbody2D>();
            if (threatBody != null)
                score -= Mathf.Min(1.5f, threatBody.velocity.magnitude * 0.08f);

            if (score < bestScore)
            {
                bestScore = score;
                bestAway = toBoss.normalized;
            }
        }

        if (bestAway.sqrMagnitude <= 0.0001f)
            return false;

        awayDirection = bestAway;
        return true;
    }

    private bool IsPlayerThreatCollider(Collider2D collider)
    {
        if (collider == null || collider.transform.IsChildOf(transform))
            return false;

        if (collider.GetComponentInParent<EnemyProjectile>() != null)
            return false;

        return collider.GetComponentInParent<FireballDamage>() != null
            || collider.GetComponentInParent<DotDamage>() != null
            || collider.GetComponentInParent<EarthBoomerangProjectile>() != null
            || collider.GetComponentInParent<BarrageBulletDamage>() != null
            || collider.GetComponentInParent<GaleBoltDamage>() != null
            || collider.GetComponentInParent<VenomBulletDamage>() != null
            || collider.GetComponentInParent<ProjectileMovement>() != null;
    }

    private bool IsSameMapAsThreat(Collider2D collider)
    {
        int myMapId = GetMyMapId();
        if (myMapId < 0)
            return true;

        ZoneOwnerTag threatZone = collider.GetComponentInParent<ZoneOwnerTag>();
        if (threatZone != null)
            return threatZone.MapId == myMapId;

        return collider.gameObject.scene == gameObject.scene;
    }

    private bool IsThreatApproaching(Collider2D threat, Vector2 toBoss, float distance, float scanRadius)
    {
        float immediateRadius = Mathf.Max(1.1f, scanRadius * 0.3f);
        if (distance <= immediateRadius)
            return true;

        Rigidbody2D threatBody = threat.attachedRigidbody != null ? threat.attachedRigidbody : threat.GetComponentInParent<Rigidbody2D>();
        if (threatBody == null || threatBody.velocity.sqrMagnitude < 0.01f)
            return true;

        return Vector2.Dot(threatBody.velocity.normalized, toBoss.normalized) > 0.15f;
    }

    private void TryThreatVerticalEvasion(Vector2 awayDirection, bool forceDecision)
    {
        if (!useGroundPhysics || !canJump || _rb == null)
            return;

        if (!forceDecision && Time.time < _nextRetreatEvasionTime)
            return;

        _nextRetreatEvasionTime = Time.time + Mathf.Max(0.05f, retreatEvasionInterval);

        if (!_isGrounded)
            return;

        if (awayDirection.y < -0.25f)
        {
            if (!TryFallThroughPlatform())
                TryRetreatJump(true);
            return;
        }

        if (awayDirection.y > 0.25f)
        {
            if (!TryRetreatJump(true))
                TryFallThroughPlatform();
            return;
        }

        if (UnityEngine.Random.value < 0.5f && TryFallThroughPlatform())
            return;

        TryRetreatJump(true);
    }

    private void TryRetreatEvasion(bool forceDecision)
    {
        if (!useGroundPhysics || !canJump || _rb == null || playerTarget == null)
            return;

        if (!forceDecision && Time.time < _nextRetreatEvasionTime)
            return;

        _nextRetreatEvasionTime = Time.time + Mathf.Max(0.05f, retreatEvasionInterval);

        float heightDelta = playerTarget.position.y - transform.position.y;
        if (heightDelta > verticalTargetThreshold)
        {
            if (!TryFallThroughPlatform())
                TryRetreatJump(true);
            return;
        }

        if (heightDelta < -verticalTargetThreshold)
        {
            if (!TryRetreatJump(true))
                TryFallThroughPlatform();
            return;
        }

        if (!_isGrounded)
            return;

        if (UnityEngine.Random.value < retreatFallThroughChance)
        {
            if (TryFallThroughPlatform())
                return;
        }

        TryRetreatJump();
    }

    private void TryApproachHop()
    {
        if (!useGroundPhysics || !canJump || !_isGrounded || playerTarget == null)
            return;

        if (Time.time - _lastRetreatJumpTime < retreatJumpCooldown)
            return;

        float heightDelta = playerTarget.position.y - transform.position.y;
        if (heightDelta > verticalTargetThreshold)
        {
            TryJump();
            return;
        }

        if (UnityEngine.Random.value <= approachJumpChance && TryJump())
            _lastRetreatJumpTime = Time.time;
    }

    private void UpdateGroundState()
    {
        if (!useGroundPhysics)
            return;

        Vector2 checkOrigin = GetGroundCheckOrigin();
        bool wasGrounded = _isGrounded;
        Collider2D[] groundHits = MapPhysicsQuery2D.OverlapCircleAll(
            gameObject,
            checkOrigin,
            Mathf.Max(0.01f, groundCheckRadius),
            groundLayerMask);

        bool canUseGroundContact = _rb == null || _rb.velocity.y <= 0.05f;
        _isGrounded = false;
        foreach (Collider2D groundHit in groundHits)
        {
            if (canUseGroundContact && IsUsableGroundCollider(groundHit) && !IsTemporarilyIgnoredGround(groundHit))
            {
                _isGrounded = true;
                break;
            }
        }

        if (_isGrounded && !wasGrounded)
            _jumpsLeft = Mathf.Max(0, maxJumps);

        if (_isGrounded != wasGrounded)
        {
            BossJumpDebug(
                "ground-state-change",
                $"GroundState {wasGrounded}->{_isGrounded} canUseContact={canUseGroundContact} origin={checkOrigin} radius={groundCheckRadius:F2} hits={groundHits.Length} vel={(_rb != null ? _rb.velocity : Vector2.zero)} jumpsLeft={_jumpsLeft}/{maxJumps}",
                0f);
        }

        if (_anim != null && HasAnimatorParameter(DefaultGroundedBoolParameter, AnimatorControllerParameterType.Bool))
            _anim.SetBool(AnimIsGrounded, _isGrounded);
    }

    private IEnumerator SnapToGroundAfterSpawn()
    {
        yield return null;
        TrySnapToGround();
        UpdateGroundState();
    }

    private bool TrySnapToGround()
    {
        if (!useGroundPhysics || _rb == null || _bodyCollider == null || groundLayerMask == 0)
            return false;

        Bounds bounds = _bodyCollider.bounds;
        float rayDistance = Mathf.Max(0.5f, spawnGroundSnapDistance);
        float topPadding = Mathf.Max(0.1f, bounds.extents.y + 0.1f);
        Vector2 origin = new Vector2(bounds.center.x, transform.position.y + topPadding);
        RaycastHit2D hit = RaycastInCurrentScene(origin, Vector2.down, rayDistance + topPadding, groundLayerMask);

        if (hit.collider == null)
            return false;

        float currentBottomOffset = bounds.min.y - transform.position.y;
        float snappedY = hit.point.y - currentBottomOffset + Mathf.Max(0f, spawnGroundOffset);
        _rb.position = new Vector2(_rb.position.x, snappedY);
        _rb.velocity = Vector2.zero;
        return true;
    }

    private Vector2 GetGroundCheckOrigin()
    {
        if (_bodyCollider != null)
            return new Vector2(_bodyCollider.bounds.center.x, _bodyCollider.bounds.min.y - 0.02f);

        return (Vector2)transform.position + Vector2.down * 0.6f;
    }

    private bool TryJump(bool ignoreTraversalCooldown = false, float targetWorldY = float.NaN)
    {
        if (!useGroundPhysics || !canJump || _rb == null || _jumpsLeft <= 0)
        {
            BossJumpDebug(
                "try-jump-disabled",
                $"TryJump blocked basic useGroundPhysics={useGroundPhysics} canJump={canJump} rb={(_rb != null)} jumpsLeft={_jumpsLeft} grounded={_isGrounded} targetY={(float.IsNaN(targetWorldY) ? -999f : targetWorldY):F2}",
                0.2f);
            return false;
        }

        if (requireGroundedToJump && !_isGrounded)
        {
            BossJumpDebug(
                "try-jump-air",
                $"TryJump blocked air-jump requireGrounded={requireGroundedToJump} grounded={_isGrounded} vel={_rb.velocity} jumpsLeft={_jumpsLeft} targetY={(float.IsNaN(targetWorldY) ? -999f : targetWorldY):F2}",
                0.15f);
            return false;
        }

        if (!allowUntargetedJumps && float.IsNaN(targetWorldY))
        {
            BossJumpDebug(
                "try-jump-untargeted",
                $"TryJump blocked untargeted allowUntargeted={allowUntargetedJumps} state={_state} grounded={_isGrounded} jumpsLeft={_jumpsLeft} playerPos={(playerTarget != null ? playerTarget.position : Vector3.zero)}",
                0.15f);
            return false;
        }

        if (!CanStartGroundTraversal(ignoreTraversalCooldown))
        {
            BossJumpDebug(
                "try-jump-traversal-block",
                $"TryJump blocked traversal ignoreCooldown={ignoreTraversalCooldown} state={_state} nextTraversal={Mathf.Max(0f, _nextGroundTraversalTime - Time.time):F2}s targetY={(float.IsNaN(targetWorldY) ? -999f : targetWorldY):F2}",
                0.15f);
            return false;
        }

        if (IsJumpBlockedByGroundTraversalMask())
        {
            BossJumpDebug(
                "try-jump-mask-block",
                $"TryJump blocked by traversal mask mask={groundTraversalBlockerMask.value} pos={transform.position} targetY={(float.IsNaN(targetWorldY) ? -999f : targetWorldY):F2}",
                0.15f);
            return false;
        }

        float resolvedJumpForce = ResolveJumpVelocity(targetWorldY);
        _jumpsLeft--;
        _rb.velocity = new Vector2(_rb.velocity.x, resolvedJumpForce);
        _isGrounded = false;
        MarkGroundTraversalUsed();
        BossDebug(
            "jump",
            $"Jump force={resolvedJumpForce:F1}/{jumpForce:F1} targetY={(float.IsNaN(targetWorldY) ? -999f : targetWorldY):F2} jumpsLeft={_jumpsLeft} heightDelta={(playerTarget != null ? playerTarget.position.y - transform.position.y : 0f):F2}",
            0.2f);
        BossJumpDebug(
            "try-jump-success",
            $"TryJump success force={resolvedJumpForce:F2}/{jumpForce:F2} targetY={(float.IsNaN(targetWorldY) ? -999f : targetWorldY):F2} bossPos={transform.position} playerPos={(playerTarget != null ? playerTarget.position : Vector3.zero)} vel={_rb.velocity} jumpsLeft={_jumpsLeft}",
            0.05f);

        if (_anim != null && HasAnimatorParameter(DefaultJumpTriggerParameter, AnimatorControllerParameterType.Trigger))
            _anim.SetTrigger(DefaultJumpTriggerParameter);

        return true;
    }

    private float ResolveJumpVelocity(float targetWorldY)
    {
        float maxForce = Mathf.Max(0.1f, jumpForce);
        float minForce = Mathf.Clamp(minCalculatedJumpForce, 0.1f, maxForce);
        float resolvedTargetY = targetWorldY;

        if (float.IsNaN(resolvedTargetY)
            && playerTarget != null
            && playerTarget.position.y > transform.position.y + verticalTargetThreshold)
        {
            resolvedTargetY = playerTarget.position.y + Mathf.Max(0f, jumpHeightPadding);
        }

        if (float.IsNaN(resolvedTargetY) || _bodyCollider == null)
            return Mathf.Clamp(maxForce * 0.7f, minForce, maxForce);

        float currentFeetY = _bodyCollider.bounds.min.y;
        float requiredHeight = Mathf.Max(0.1f, resolvedTargetY - currentFeetY);
        float gravity = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.01f, _rb.gravityScale);
        if (UsesBoss25JumpRules() && requiredHeight < 1.5f)
            minForce = Mathf.Min(minForce, 5f);
        float calculated = Mathf.Sqrt(2f * gravity * requiredHeight);
        return Mathf.Clamp(calculated, minForce, maxForce);
    }

    private bool TryRetreatJump(bool force = false)
    {
        if (!useGroundPhysics || !canJump || !_isGrounded)
            return false;

        if (!force && Time.time - _lastRetreatJumpTime < retreatJumpCooldown)
            return false;

        if (!force && UnityEngine.Random.value > retreatJumpChance)
            return false;

        if (TryJump())
        {
            _lastRetreatJumpTime = Time.time;
            return true;
        }

        return false;
    }

    private void SearchForLowerPlatformExit()
    {
        if (!useGroundPhysics || _rb == null || playerTarget == null)
            return;

        float deltaX = playerTarget.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) > 0.3f)
        {
            _platformSearchDirection = Mathf.Sign(deltaX);
            _nextPlatformSearchFlipTime = Time.time + 3f;
        }
        else
        {
            if (Mathf.Abs(_platformSearchDirection) <= 0.01f)
            {
                _platformSearchDirection = GetFacingSign();
                _nextPlatformSearchFlipTime = Time.time + 3f;
            }
            else if (Time.time >= _nextPlatformSearchFlipTime)
            {
                _platformSearchDirection *= -1f;
                _nextPlatformSearchFlipTime = Time.time + 3f;
            }
        }

        float speed = chaseSpeed * _speedMultiplier;
        float yVelocity = useGroundPhysics ? _rb.velocity.y : 0f;
        _rb.velocity = new Vector2(_platformSearchDirection * speed, yVelocity);
        UpdateFacing(_platformSearchDirection);
        SetMovingState(true);
        BossDebug("search-down", $"Search lower platform dir={_platformSearchDirection:F0} heightDelta={(playerTarget.position.y - transform.position.y):F2}");
    }

    private bool TryFallThroughPlatform(bool forceForLowerTarget = false)
    {
        if (!useGroundPhysics || _fallThroughCoroutine != null || _bodyCollider == null || _rb == null)
            return false;

        if (!CanStartGroundTraversal())
            return false;

        Collider2D platform = GetCurrentGroundPlatform();
        if (platform == null)
        {
            BossDebug("fall-through-fail", "FallThrough fail: khong tim thay platform hien tai.");
            return false;
        }

        bool targetIsBelow = forceForLowerTarget
            && playerTarget != null
            && playerTarget.position.y < transform.position.y - verticalTargetThreshold;
        bool canDropThroughCurrentPlatform = HasGroundBelow(platform) || (targetIsBelow && IsOneWayGround(platform));
        if (!canDropThroughCurrentPlatform)
        {
            BossDebug("fall-through-fail", $"FallThrough fail: platform={platform.name} hasGroundBelow={HasGroundBelow(platform)} oneWay={IsOneWayGround(platform)} targetIsBelow={targetIsBelow}");
            return false;
        }

        if (IsFallThroughBlockedByGroundTraversalMask(platform))
            return false;

        _fallThroughCoroutine = StartCoroutine(FallThroughPlatformCoroutine(platform));
        MarkGroundTraversalUsed();
        BossDebug("fall-through", $"FallThrough platform={platform.name} force={forceForLowerTarget} targetIsBelow={targetIsBelow}", 0f);
        return true;
    }

    private Collider2D GetCurrentGroundPlatform()
    {
        if (_bodyCollider == null || groundLayerMask == 0)
            return null;

        Bounds bounds = _bodyCollider.bounds;
        float inset = Mathf.Min(bounds.extents.x * 0.6f, 0.25f);
        float rayDistance = 0.2f;
        float leftX = bounds.min.x + inset;
        float centerX = bounds.center.x;
        float rightX = bounds.max.x - inset;
        float rayOriginY = bounds.min.y + 0.05f;

        Collider2D platform = RaycastGroundForFallThrough(new Vector2(centerX, rayOriginY), rayDistance);
        if (platform != null)
            return platform;

        platform = RaycastGroundForFallThrough(new Vector2(leftX, rayOriginY), rayDistance);
        if (platform != null)
            return platform;

        platform = RaycastGroundForFallThrough(new Vector2(rightX, rayOriginY), rayDistance);
        if (platform != null)
            return platform;

        return OverlapCurrentGroundForFallThrough();
    }

    private bool IsTemporarilyIgnoredGround(Collider2D collider)
    {
        return collider != null && (collider == _ignoredGroundCollider || collider == _jumpThroughGroundCollider);
    }

    private void BeginJumpThroughHigherPlatform(Collider2D platform, float platformTopY)
    {
        if (!UsesBoss25JumpRules() || platform == null || _bodyCollider == null || _rb == null)
            return;

        if (!IsUsableGroundCollider(platform))
            return;

        if (_jumpThroughGroundCollider != null && _jumpThroughGroundCollider != platform)
            RestoreJumpThroughGroundCollision();

        _jumpThroughGroundCollider = platform;
        Physics2D.IgnoreCollision(_bodyCollider, platform, true);
        if (_jumpThroughCoroutine != null)
            StopCoroutine(_jumpThroughCoroutine);

        _jumpThroughCoroutine = StartCoroutine(RestoreJumpThroughHigherPlatformCoroutine(platform, platformTopY));
        BossJumpDebug(
            "jump-through-start",
            $"JumpThrough start platform={platform.name} topY={platformTopY:F2} bossFeet={_bodyCollider.bounds.min.y:F2} vel={_rb.velocity}",
            0.05f);
    }

    private IEnumerator RestoreJumpThroughHigherPlatformCoroutine(Collider2D platform, float platformTopY)
    {
        float timeoutAt = Time.time + 2.2f;
        while (_bodyCollider != null && _rb != null && platform != null && Time.time < timeoutAt)
        {
            float feetY = _bodyCollider.bounds.min.y;
            bool feetReachedTop = feetY >= platformTopY - 0.04f;
            bool isFallingOrPeaking = _rb.velocity.y <= 0.05f;
            if (feetReachedTop && isFallingOrPeaking)
                break;

            yield return null;
        }

        RestoreJumpThroughGroundCollision(platform, false);
    }

    private Collider2D RaycastGroundForFallThrough(Vector2 origin, float distance)
    {
        RaycastHit2D[] hits = new RaycastHit2D[8];
        int hitCount = RaycastAllInCurrentScene(origin, Vector2.down, distance, groundLayerMask, hits);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hits[i];
            if (!IsUsableGroundCollider(hit.collider))
                continue;

            if (IsOneWayGround(hit.collider))
                return hit.collider;

            if (HasGroundBelow(hit.collider))
                return hit.collider;
        }

        return null;
    }

    private Collider2D OverlapCurrentGroundForFallThrough()
    {
        Collider2D[] groundHits = MapPhysicsQuery2D.OverlapCircleAll(
            gameObject,
            GetGroundCheckOrigin(),
            Mathf.Max(0.05f, groundCheckRadius * 1.5f),
            groundLayerMask);

        Collider2D fallback = null;
        foreach (Collider2D hit in groundHits)
        {
            if (!IsUsableGroundCollider(hit) || IsTemporarilyIgnoredGround(hit))
                continue;

            if (IsOneWayGround(hit))
                return hit;

            fallback ??= hit;
        }

        return fallback;
    }

    private bool IsUsableGroundCollider(Collider2D collider)
    {
        if (collider == null || collider == _bodyCollider || collider.isTrigger)
            return false;

        return !collider.transform.IsChildOf(transform);
    }

    private bool IsOneWayGround(Collider2D collider)
    {
        if (collider == null)
            return false;

        PlatformEffector2D effector = collider.GetComponent<PlatformEffector2D>();
        if (effector == null)
            effector = collider.GetComponentInParent<PlatformEffector2D>();

        return effector != null && effector.useOneWay;
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

    private bool HasGroundBelow(Collider2D currentPlatform)
    {
        if (_bodyCollider == null || currentPlatform == null || groundLayerMask == 0)
            return false;

        Bounds bounds = _bodyCollider.bounds;
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
        RaycastHit2D[] hits = new RaycastHit2D[16];
        int hitCount = RaycastAllInCurrentScene(origin, Vector2.down, groundSearchDistance, groundLayerMask, hits);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = hits[i];
            if (!IsUsableGroundCollider(hit.collider) || hit.collider == currentPlatform)
                continue;

            return true;
        }

        return false;
    }

    private int RaycastAllInCurrentScene(Vector2 origin, Vector2 direction, float distance, int layerMask, RaycastHit2D[] results)
    {
        if (results == null || results.Length == 0)
            return 0;

        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(layerMask);
        filter.useTriggers = false;

        var scene = gameObject.scene;
        if (scene.IsValid())
        {
            var physicsScene = scene.GetPhysicsScene2D();
            if (physicsScene.IsValid())
                return physicsScene.Raycast(origin, direction, distance, filter, results);
        }

        return Physics2D.Raycast(origin, direction, filter, results, distance);
    }

    private IEnumerator FallThroughPlatformCoroutine(Collider2D platform)
    {
        if (_bodyCollider == null || _rb == null || platform == null)
        {
            _fallThroughCoroutine = null;
            yield break;
        }

        _ignoredGroundCollider = platform;
        Bounds platformBounds = platform.bounds;

        Physics2D.IgnoreCollision(_bodyCollider, platform, true);
        _rb.position = new Vector2(_rb.position.x, _rb.position.y - Mathf.Max(0.05f, fallThroughDrop));
        _rb.velocity = new Vector2(_rb.velocity.x, Mathf.Min(_rb.velocity.y, -Mathf.Max(0.1f, jumpForce * 0.5f)));

        float elapsed = 0f;
        float maxWait = fallThroughDuration + 0.75f;
        while (elapsed < maxWait)
        {
            elapsed += Time.deltaTime;

            bool minimumDurationElapsed = elapsed >= fallThroughDuration;
            bool fullyBelowPlatform = _bodyCollider == null || _bodyCollider.bounds.max.y < platformBounds.min.y - 0.05f;
            if (minimumDurationElapsed && fullyBelowPlatform)
                break;

            yield return null;
        }

        if (_bodyCollider != null && platform != null)
            Physics2D.IgnoreCollision(_bodyCollider, platform, false);

        _ignoredGroundCollider = null;
        _fallThroughCoroutine = null;
    }

    private void RestoreIgnoredGroundCollision()
    {
        if (_fallThroughCoroutine != null)
        {
            StopCoroutine(_fallThroughCoroutine);
            _fallThroughCoroutine = null;
        }

        if (_bodyCollider != null && _ignoredGroundCollider != null)
            Physics2D.IgnoreCollision(_bodyCollider, _ignoredGroundCollider, false);

        _ignoredGroundCollider = null;
    }

    private void RestoreJumpThroughGroundCollision(Collider2D expectedPlatform = null, bool stopCoroutine = true)
    {
        if (expectedPlatform != null && _jumpThroughGroundCollider != expectedPlatform)
            return;

        if (stopCoroutine && _jumpThroughCoroutine != null)
        {
            StopCoroutine(_jumpThroughCoroutine);
            _jumpThroughCoroutine = null;
        }
        else if (!stopCoroutine)
        {
            _jumpThroughCoroutine = null;
        }

        if (_bodyCollider != null && _jumpThroughGroundCollider != null)
        {
            Physics2D.IgnoreCollision(_bodyCollider, _jumpThroughGroundCollider, false);
            BossJumpDebug(
                "jump-through-restore",
                $"JumpThrough restore platform={_jumpThroughGroundCollider.name} bossFeet={_bodyCollider.bounds.min.y:F2} vel={(_rb != null ? _rb.velocity : Vector2.zero)}",
                0.05f);
        }

        _jumpThroughGroundCollider = null;
    }

    private void CheckPhases()
    {
        if (_config?.phases == null || _health == null)
            return;

        int maxHp = _health.GetMaxHealth();
        float hpPct = maxHp > 0
            ? (_health.GetCurrentHealth() / (float)maxHp) * 100f
            : 100f;

        foreach (var phase in _config.phases)
        {
            if (_triggeredPhases.Contains(phase.hp_pct_threshold))
                continue;

            if (hpPct <= phase.hp_pct_threshold)
            {
                _triggeredPhases.Add(phase.hp_pct_threshold);
                StartCoroutine(ExecutePhase(phase));
            }
        }
    }

    private IEnumerator ExecutePhase(PhaseData phase)
    {
        AnnouncePhase(phase.message);

        switch (phase.action)
        {
            case "enrage":
                _damageMultiplier *= phase.damage_multiplier > 0f ? phase.damage_multiplier : 1.2f;
                _speedMultiplier *= phase.speed_multiplier > 0f ? phase.speed_multiplier : 1.1f;
                if (_anim != null && HasAnimatorParameter("enrage", AnimatorControllerParameterType.Trigger))
                    _anim.SetTrigger("enrage");
                break;

            case "summon":
                yield return SummonAdds(phase.mob_count > 0 ? phase.mob_count : 2);
                break;

            case "heal":
                float healPct = phase.heal_pct > 0f ? phase.heal_pct : 15f;
                int healAmount = Mathf.RoundToInt(_health.GetMaxHealth() * healPct / 100f);
                _health.Heal(healAmount);
                if (_anim != null && HasAnimatorParameter("heal", AnimatorControllerParameterType.Trigger))
                    _anim.SetTrigger("heal");
                break;

            case "berserk":
                _damageMultiplier *= phase.damage_multiplier > 0f ? phase.damage_multiplier : 2f;
                _speedMultiplier *= phase.speed_multiplier > 0f ? phase.speed_multiplier : 1.3f;
                _cooldownMultiplier *= phase.skill_cooldown_multiplier > 0f ? phase.skill_cooldown_multiplier : 0.5f;
                if (_anim != null && HasAnimatorParameter("berserk", AnimatorControllerParameterType.Trigger))
                    _anim.SetTrigger("berserk");
                break;
        }
    }

    private IEnumerator SummonAdds(int count)
    {
        if (addSpawnPrefab == null)
            yield break;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 3f;
            GameObject add = Instantiate(addSpawnPrefab, (Vector2)transform.position + offset, Quaternion.identity);
            MoveSpawnedObjectToCurrentMap(add);
            ApplyMapVisibility(add, GetMyMapId());
            SpawnNetworkObjectIfNeeded(add);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void FindNearestPlayer()
    {
        float nearest = float.MaxValue;
        float fallbackNearest = float.MaxValue;
        int myMapId = GetMyMapId();
        var registry = ZoneRoomRegistry.Instance;
        playerTarget = null;
        Transform fallbackTarget = null;
        int taggedCount = 0;
        int healthCandidateCount = 0;
        int roomMismatchCount = 0;

        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            taggedCount++;
            if (go.GetComponent<NetworkPlayerHealth>() == null && go.GetComponent<PlayerHealth>() == null)
                continue;

            healthCandidateCount++;
            float dist = Vector2.Distance(transform.position, go.transform.position);
            if (dist < fallbackNearest)
            {
                fallbackNearest = dist;
                fallbackTarget = go.transform;
            }

            if (registry != null && myMapId != -999)
            {
                NetworkObject netObj = go.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    var room = registry.GetClientRoom(netObj.OwnerClientId);
                    if (room == null || room.MapId != myMapId)
                    {
                        roomMismatchCount++;
                        continue;
                    }
                }
            }

            if (dist < nearest)
            {
                nearest = dist;
                playerTarget = go.transform;
            }
        }

        if (playerTarget == null && fallbackTarget != null)
        {
            playerTarget = fallbackTarget;
            nearest = fallbackNearest;
            BossDebug(
                "find-target-fallback",
                $"FindNearestPlayer fallback chose {fallbackTarget.name}; room filter failed. taggedPlayers={taggedCount} healthCandidates={healthCandidateCount} roomMismatch={roomMismatchCount} myMapId={myMapId}",
                0.5f);
        }

        if (ShouldEmitBoss25Logs())
        {
            BossDebug(
                "find-target",
                $"FindNearestPlayer taggedPlayers={taggedCount} healthCandidates={healthCandidateCount} roomMismatch={roomMismatchCount} selected={(playerTarget != null ? playerTarget.name : "null")} nearest={(nearest < float.MaxValue ? nearest : -1f):F2} myMapId={myMapId} registry={(registry != null)} nmServer={(NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)}",
                1f);
        }
    }

    private void RefreshPlayerTarget()
    {
        if (playerTarget == null || !IsSameMapAsTarget(playerTarget))
        {
            FindNearestPlayer();
            return;
        }

        if (!playerTarget.gameObject.activeInHierarchy)
            FindNearestPlayer();
    }

    private int GetMyMapId()
    {
        ZoneOwnerTag tag = GetComponent<ZoneOwnerTag>();
        return tag != null ? tag.MapId : -999;
    }

    private bool IsSameMapAsTarget(Transform targetTransform)
    {
        if (targetTransform == null)
            return false;

        int myMapId = GetMyMapId();
        if (myMapId == -999)
            return true;

        var registry = ZoneRoomRegistry.Instance;
        if (registry == null)
            return true;

        NetworkObject netObj = targetTransform.GetComponent<NetworkObject>();
        if (netObj == null)
            return false;

        var room = registry.GetClientRoom(netObj.OwnerClientId);
        return room != null && room.MapId == myMapId;
    }

    private void ApplyDamageToHitSet(Collider2D[] hits, int damage)
    {
        if (hits == null || hits.Length == 0)
            return;

        var damagedRoots = new HashSet<GameObject>();
        foreach (var hit in hits)
        {
            if (hit == null)
                continue;

            GameObject root = hit.transform.root.gameObject;
            if (!damagedRoots.Add(root))
                continue;

            ApplyDamageToTarget(root, damage);
        }
    }

    private void ApplyDamageToTarget(GameObject target, int damage)
    {
        if (target == null || !IsSameMapAsTarget(target.transform))
            return;

        NetworkPlayerHealth netHealth = target.GetComponentInParent<NetworkPlayerHealth>();
        if (netHealth != null)
        {
            netHealth.TakeDamage(damage);
            return;
        }

        PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(damage);
    }

    private void PerformMeleeHit(int damage, float range, float fallbackRadius, float yOffset)
    {
        Vector2 hitCenter;
        float hitRadius;

        if (meleeHitbox != null)
        {
            hitCenter = meleeHitbox.bounds.center;
            hitRadius = Mathf.Max(meleeHitbox.bounds.extents.x, meleeHitbox.bounds.extents.y);
        }
        else
        {
            float facingSign = GetFacingSign();
            float offset = Mathf.Max(0.35f, range * 0.45f);
            hitCenter = (Vector2)transform.position + new Vector2(facingSign * offset, yOffset);
            hitRadius = Mathf.Max(0.2f, fallbackRadius);
        }

        Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, hitCenter, hitRadius, LayerMask.GetMask("Player"));
        var validHits = new List<Collider2D>(hits.Length);
        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            Transform targetRoot = hit.transform.root;
            bool horizontalOk = IsTargetHorizontallyReachableForMelee(range, targetRoot);
            bool verticalOk = IsTargetVerticallyReachableForMelee(range, fallbackRadius, targetRoot);
            if (!horizontalOk || !verticalOk)
            {
                BossDebug(
                    "melee-hit-block",
                    $"Block melee hit target={targetRoot.name} horizontalOk={horizontalOk} verticalOk={verticalOk} center={hitCenter} radius={hitRadius:F2} range={range:F2}");
                continue;
            }

            validHits.Add(hit);
        }

        BossDebug("melee-hit-query", $"Melee query hits={hits.Length} valid={validHits.Count} center={hitCenter} radius={hitRadius:F2} damage={damage}", 0.2f);

        if (validHits.Count == 0
            && playerTarget != null
            && Vector2.Distance(transform.position, playerTarget.position) <= range + 0.35f
            && IsTargetHorizontallyReachableForMelee(range, playerTarget)
            && IsTargetVerticallyReachableForMelee(range, fallbackRadius, playerTarget))
        {
            ApplyDamageToTarget(playerTarget.gameObject, damage);
            BossDebug("melee-hit-fallback", $"Fallback melee damage target={playerTarget.name} damage={damage}", 0.2f);
            return;
        }

        ApplyDamageToHitSet(validHits.ToArray(), damage);
    }

    private void MoveSpawnedObjectToCurrentMap(GameObject spawnedObject)
    {
        if (spawnedObject == null)
            return;

        int myMapId = GetMyMapId();
        if (myMapId < 0)
            return;

        MapSceneManager.Instance?.MoveToMapScene(spawnedObject, myMapId);
    }

    private void SpawnNetworkObjectIfNeeded(GameObject spawnedObject)
    {
        if (spawnedObject == null)
            return;

        NetworkObject netObj = spawnedObject.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsSpawned)
        {
            netObj.Spawn();
            BossDebug("network-spawn", $"Network spawn object={spawnedObject.name} netId={netObj.NetworkObjectId} scene={spawnedObject.scene.name}", 0f);
        }
    }

    private void PrepareTransientEffect(GameObject effect, float lifetime)
    {
        if (effect == null)
            return;

        MoveSpawnedObjectToCurrentMap(effect);
        ApplyMapVisibility(effect, GetMyMapId());
        SpawnNetworkObjectIfNeeded(effect);

        if (effect.GetComponent<EnemyProjectile>() != null)
            return;

        NetworkAutoDespawn autoDespawn = effect.GetComponent<NetworkAutoDespawn>();
        if (autoDespawn == null)
            autoDespawn = effect.AddComponent<NetworkAutoDespawn>();

        autoDespawn.Arm(lifetime > 0f ? lifetime : 1f);
    }

    private void SpawnTransientVisual(GameObject visualPrefab, int skillIndex, Vector3 position, Quaternion rotation, Vector3 localScale, float lifetime)
    {
        if (visualPrefab == null)
            return;

        NetworkObject prefabNetObj = visualPrefab.GetComponent<NetworkObject>();
        bool hasNetworkObject = prefabNetObj != null;
        bool canUseClientRpc = CanSpawnVisualThroughRpc(skillIndex);
        BossDebug(
            "visual-route",
            $"Visual route prefab={visualPrefab.name} hasNetworkObject={hasNetworkObject} canUseClientRpc={canUseClientRpc} pos={position} lifetime={lifetime:F2}",
            0f);

        if (hasNetworkObject || !canUseClientRpc)
        {
            GameObject effect = Instantiate(visualPrefab, position, rotation);
            effect.transform.localScale = localScale;
            PrepareTransientEffect(effect, lifetime);
            NetworkObject effectNetObj = effect.GetComponent<NetworkObject>();
            BossDebug(
                "visual-instantiated",
                $"Visual instantiated object={effect.name} net={(effectNetObj != null)} spawned={(effectNetObj != null && effectNetObj.IsSpawned)} scene={effect.scene.name}",
                0f);
            return;
        }

        SpawnTransientVisualClientRpc(skillIndex, position, rotation, localScale, lifetime);
        BossDebug("visual-clientrpc", $"Visual ClientRpc sent skillIndex={skillIndex} pos={position}", 0f);
    }

    private bool CanSpawnVisualThroughRpc(int skillIndex)
    {
        return skillIndex >= 0
            && skillIndex < localSkills.Count
            && NetworkManager.Singleton != null
            && NetworkManager.Singleton.IsServer
            && NetworkObject != null
            && NetworkObject.IsSpawned;
    }

    [ClientRpc]
    private void SpawnTransientVisualClientRpc(int skillIndex, Vector3 position, Quaternion rotation, Vector3 localScale, float lifetime)
    {
        if (skillIndex < 0 || skillIndex >= localSkills.Count)
            return;

        GameObject visualPrefab = localSkills[skillIndex]?.visualPrefab;
        if (visualPrefab == null)
            return;

        GameObject effect = Instantiate(visualPrefab, position, rotation);
        effect.transform.localScale = localScale;
        Destroy(effect, lifetime > 0f ? lifetime : 1f);
        BossDebug("visual-clientrpc-received", $"Visual ClientRpc received prefab={visualPrefab.name} pos={position} lifetime={lifetime:F2}", 0f);
    }

    private Vector3 GetBaseSpawnPosition(LocalBossSkillConfig skill)
    {
        return skill != null && skill.spawnPoint != null ? skill.spawnPoint.position : transform.position;
    }

    private Vector3 GetSpawnPosition(LocalBossSkillConfig skill, Vector2 aimDirection)
    {
        Vector3 basePosition = GetBaseSpawnPosition(skill);
        float horizontalSign = Mathf.Abs(aimDirection.x) > 0.01f ? Mathf.Sign(aimDirection.x) : GetFacingSign();
        return basePosition + new Vector3(Mathf.Abs(skill.spawnOffsetX) * horizontalSign, skill.spawnOffsetY, 0f);
    }

    private Vector2 GetAimDirection(Vector2 origin)
    {
        if (playerTarget == null)
            return GetFacingSign() > 0f ? Vector2.right : Vector2.left;

        Vector2 toPlayer = (Vector2)playerTarget.position - origin;
        if (toPlayer.sqrMagnitude < 0.0001f)
            toPlayer = GetFacingSign() > 0f ? Vector2.right : Vector2.left;

        float overshoot = Mathf.Max(0f, projectileAimOvershoot);
        Collider2D playerCollider = playerTarget.GetComponentInChildren<Collider2D>();
        if (playerCollider != null)
            overshoot += Mathf.Max(playerCollider.bounds.extents.x, playerCollider.bounds.extents.y);

        Vector2 targetPoint = (Vector2)playerTarget.position + toPlayer.normalized * overshoot;
        Vector2 aimDirection = targetPoint - origin;
        return aimDirection.sqrMagnitude < 0.0001f ? toPlayer.normalized : aimDirection.normalized;
    }

    private void SetMovingState(bool isMoving)
    {
        if (_anim != null && HasAnimatorParameter(DefaultMoveBoolParameter, AnimatorControllerParameterType.Bool))
            _anim.SetBool(AnimIsMoving, isMoving);
    }

    private void UpdateFacing(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) <= 0.01f)
            return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(horizontalDirection);
        transform.localScale = scale;
    }

    private float GetFacingSign()
    {
        return transform.localScale.x >= 0f ? 1f : -1f;
    }

    private void PlayAttackAnimation(string parameterName)
    {
        if (_anim == null)
            return;

        if (string.IsNullOrWhiteSpace(parameterName)
            || string.Equals(parameterName, DefaultAttackBoolParameter, StringComparison.OrdinalIgnoreCase))
        {
            _activeAttackBoolParameter = DefaultAttackBoolParameter;
            if (HasAnimatorParameter(DefaultAttackBoolParameter, AnimatorControllerParameterType.Bool))
                _anim.SetBool(AnimIsAttacking, true);
            return;
        }

        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            _activeAttackBoolParameter = parameterName;
            _anim.SetBool(parameterName, true);
            return;
        }

        _activeAttackBoolParameter = null;
        if (HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
            _anim.SetTrigger(parameterName);
        else if (HasAnimatorParameter(DefaultAttackBoolParameter, AnimatorControllerParameterType.Bool))
        {
            _activeAttackBoolParameter = DefaultAttackBoolParameter;
            _anim.SetBool(AnimIsAttacking, true);
        }
    }

    private void ResetAttackAnimation()
    {
        if (_anim == null)
            return;

        string boolParameter = string.IsNullOrWhiteSpace(_activeAttackBoolParameter)
            ? DefaultAttackBoolParameter
            : _activeAttackBoolParameter;

        if (HasAnimatorParameter(boolParameter, AnimatorControllerParameterType.Bool))
            _anim.SetBool(boolParameter, false);

        if (!string.Equals(boolParameter, DefaultAttackBoolParameter, StringComparison.OrdinalIgnoreCase)
            && HasAnimatorParameter(DefaultAttackBoolParameter, AnimatorControllerParameterType.Bool))
        {
            _anim.SetBool(AnimIsAttacking, false);
        }

        _activeAttackBoolParameter = null;
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (_anim == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (var parameter in _anim.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
                return true;
        }

        return false;
    }

    private bool HasLocalSkillsConfigured()
    {
        foreach (var skill in localSkills)
        {
            if (IsLocalSkillConfigured(skill))
                return true;
        }

        return false;
    }

    private bool IsLocalSkillConfigured(LocalBossSkillConfig skill)
    {
        if (skill == null)
            return false;

        if (skill.range <= 0f)
            return false;

        if (skill.skillType == LocalBossSkillType.Projectile)
            return skill.visualPrefab != null || skillBreathPrefab != null;

        return true;
    }

    private bool IsLocalSkillReady(LocalBossSkillConfig skill)
    {
        string skillId = ResolveLocalSkillId(skill);
        float cooldown = Mathf.Max(0.05f, skill.cooldown * _cooldownMultiplier);
        return !_skillLastCast.TryGetValue(skillId, out float lastUsed)
            || Time.time - lastUsed >= cooldown;
    }

    private string ResolveLocalSkillId(LocalBossSkillConfig skill)
    {
        if (skill == null)
            return "__boss_invalid_skill__";

        return string.IsNullOrWhiteSpace(skill.skillId)
            ? skill.skillType.ToString()
            : skill.skillId.Trim();
    }

    private float GetApproachStopDistance(LocalBossSkillConfig skill)
    {
        if (skill == null)
            return meleeAttackRange;

        if (skill.skillType == LocalBossSkillType.Projectile)
        {
            float minDistance = Mathf.Max(0f, skill.minDistance);
            return Mathf.Max(minDistance, skill.range - 0.25f);
        }

        return Mathf.Max(0.1f, skill.range - 0.1f);
    }

    private bool CanUseBasicMelee(float dist)
    {
        if (dist > meleeAttackRange)
            return false;

        if (!IsTargetVerticallyReachableForMelee(meleeAttackRange, 0.75f))
            return false;

        if (_skillLastCast.TryGetValue(BasicMeleeSkillId, out float lastAttack))
            return Time.time - lastAttack >= Mathf.Max(0.05f, basicAttackCooldown * _cooldownMultiplier);

        return true;
    }

    private bool IsTargetHorizontallyReachableForMelee(float range, Transform targetTransform)
    {
        if (targetTransform == null)
            return false;

        Bounds bossBounds = GetBossMeleeBounds();
        Bounds targetBounds = GetTargetMeleeBounds(targetTransform);
        float horizontalGap = Mathf.Max(0f, Mathf.Max(targetBounds.min.x - bossBounds.max.x, bossBounds.min.x - targetBounds.max.x));
        return horizontalGap <= range + 0.15f;
    }

    private bool IsTargetVerticallyReachableForMelee(float range, float hitRadius, Transform targetOverride = null)
    {
        Transform targetTransform = targetOverride != null ? targetOverride : playerTarget;
        if (!useGroundPhysics || targetTransform == null)
            return true;

        Bounds bossBounds = GetBossMeleeBounds();
        Bounds targetBounds = GetTargetMeleeBounds(targetTransform);

        float allowedVerticalGap = Mathf.Max(0.08f, Mathf.Min(0.22f, hitRadius * 0.25f));
        float targetAboveGap = targetBounds.min.y - bossBounds.max.y;
        float targetBelowGap = bossBounds.min.y - targetBounds.max.y;
        if (targetAboveGap > allowedVerticalGap || targetBelowGap > allowedVerticalGap)
        {
            BossDebug(
                "melee-vertical-gap",
                $"Melee blocked by vertical gap target={targetTransform.name} aboveGap={targetAboveGap:F2} belowGap={targetBelowGap:F2} allowed={allowedVerticalGap:F2}");
            return false;
        }

        if (HasGroundBlockingMeleeTarget(targetTransform, bossBounds, targetBounds))
        {
            BossDebug("melee-ground-block", $"Melee blocked by ground between boss and target={targetTransform.name}");
            return false;
        }

        float centerTolerance = Mathf.Max(0.45f, Mathf.Min(0.95f, Mathf.Max(verticalTargetThreshold * 0.75f, range * 0.35f)));
        float centerDelta = Mathf.Abs(targetBounds.center.y - bossBounds.center.y);
        if (centerDelta > centerTolerance)
        {
            BossDebug(
                "melee-center-block",
                $"Melee blocked by center delta target={targetTransform.name} centerDelta={centerDelta:F2} tolerance={centerTolerance:F2}");
            return false;
        }

        return true;
    }

    private Bounds GetBossMeleeBounds()
    {
        if (_bodyCollider != null)
            return _bodyCollider.bounds;

        return new Bounds(transform.position, Vector3.one);
    }

    private Bounds GetTargetMeleeBounds(Transform targetTransform)
    {
        Collider2D targetCollider = targetTransform != null ? targetTransform.GetComponentInChildren<Collider2D>() : null;
        if (targetCollider != null)
            return targetCollider.bounds;

        return new Bounds(targetTransform != null ? targetTransform.position : transform.position, Vector3.one);
    }

    private bool HasGroundBlockingMeleeTarget(Transform targetTransform, Bounds bossBounds, Bounds targetBounds)
    {
        if (targetTransform == null || groundLayerMask == 0)
            return false;

        Vector2 origin = bossBounds.center;
        Vector2 targetPoint = targetBounds.center;
        Vector2 direction = targetPoint - origin;
        float distance = direction.magnitude;
        if (distance <= 0.05f)
            return false;

        RaycastHit2D[] hits = new RaycastHit2D[8];
        int hitCount = RaycastAllInCurrentScene(origin, direction.normalized, distance, groundLayerMask, hits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hitCollider = hits[i].collider;
            if (!IsUsableGroundCollider(hitCollider))
                continue;

            if (hitCollider.transform.IsChildOf(targetTransform))
                continue;

            return true;
        }

        return false;
    }

    private void BossDebug(string key, string message, float interval = -1f)
    {
        if (!ShouldEmitBoss25Logs())
            return;

        float activeInterval = interval >= 0f ? interval : Mathf.Max(0.05f, debugLogInterval);
        string logKey = string.IsNullOrWhiteSpace(key) ? message : key;
        if (activeInterval > 0f
            && _debugLogTimes.TryGetValue(logKey, out float nextAllowedTime)
            && Time.time < nextAllowedTime)
        {
            return;
        }

        _debugLogTimes[logKey] = Time.time + activeInterval;
        Debug.Log($"{Boss25LogTag}[BossAI:{name}] {message}", this);
        MirrorBossServerLog(message);
    }

    private void BossJumpDebug(string key, string message, float interval = -1f)
    {
        BossDebug(
            $"jump-{key}",
            $"{Boss25JumpLogTag} {message}",
            interval);
    }

    private bool ShouldEmitBoss25Logs()
    {
        return debugLogs
            || bossId == 13
            || gameObject.name.Contains("Enemy 25");
    }

    private void LogBossLifecycle(string step)
    {
        if (!ShouldEmitBoss25Logs())
            return;

        NetworkObject netObj = GetComponent<NetworkObject>();
        EnemyAI normalAI = GetComponent<EnemyAI>();
        string sceneName = gameObject.scene.IsValid() ? gameObject.scene.name : "<invalid>";
        bool nmServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        bool nmClient = NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
        bool netSpawned = netObj != null && netObj.IsSpawned;
        string targetName = playerTarget != null ? playerTarget.name : "null";

        string message = $"{step} enabled={enabled} active={gameObject.activeInHierarchy} scene={sceneName} bossId={bossId} debugLogs={debugLogs} useInspectorSkillsOnly={useInspectorSkillsOnly} localSkills={(localSkills != null ? localSkills.Count : 0)} nmServer={nmServer} nmClient={nmClient} netSpawned={netSpawned} normalAIEnabled={(normalAI != null && normalAI.enabled)} target={targetName}";
        Debug.LogWarning($"{Boss25LogTag}[BossAI:{name}] {message}", this);
        MirrorBossServerLog(message);
    }

    private void MirrorBossServerLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (NetworkObject == null || !NetworkObject.IsSpawned)
            return;

        MirrorBossServerLogClientRpc(message);
    }

    [ClientRpc]
    private void MirrorBossServerLogClientRpc(string message)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            return;

      //  Debug.LogWarning($"{Boss25LogTag}[SERVER->CLIENT][BossAI:{name}] {message}", this);
    }

    private void AnnouncePhase(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            return;

        if (phaseAnnounceText != null)
        {
            phaseAnnounceText.text = msg;
            phaseAnnounceText.gameObject.SetActive(true);
            StartCoroutine(HideAfter(phaseAnnounceText.gameObject, 3f));
        }
    }

    private IEnumerator HideAfter(GameObject go, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (go != null)
            go.SetActive(false);
    }

    private void OnDamageTaken()
    {
        if (_state == BossState.Dead || _state == BossState.Skill)
            return;

        Vector2 awayDirection = playerTarget != null
            ? ((Vector2)transform.position - (Vector2)playerTarget.position).normalized
            : new Vector2(-GetFacingSign(), 0f);

        StartThreatRetreat(awayDirection);
    }

    private void OnNetworkDamageTaken(int damage, ulong attackerClientId)
    {
        OnDamageTaken();
    }

    private void OnDeath()
    {
        if (_state == BossState.Dead)
            return;

        StopAllCoroutines();
        RestoreIgnoredGroundCollision();
        RestoreJumpThroughGroundCollision();
        ResetAttackAnimation();

        _state = BossState.Dead;
        _retreatUntilTime = -1f;
        _retreatMinUntilTime = -1f;
        _retreatSpeedMultiplierOverride = -1f;
        _currentRetreatAllowsGroundTraversal = true;

        if (_rb != null)
            _rb.velocity = Vector2.zero;

        if (_anim != null && HasAnimatorParameter("die", AnimatorControllerParameterType.Trigger))
            _anim.SetTrigger("die");
    }

    private int ResolveBaseDamage()
    {
        if (_runtimeBaseDamageOverride > 0)
            return _runtimeBaseDamageOverride;

        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null && enemyAI.damage > 0)
            return enemyAI.damage;

        if (_config != null && _config.base_damage > 0)
            return _config.base_damage;

        return 30;
    }

    private static void ApplyMapVisibility(GameObject spawnedObject, int mapId)
    {
        if (spawnedObject == null || mapId < 0)
            return;

        var zoneTag = spawnedObject.GetComponent<ZoneOwnerTag>() ?? spawnedObject.AddComponent<ZoneOwnerTag>();
        zoneTag.SetZone(mapId, 0);

        var filter = spawnedObject.GetComponent<NetworkVisibilityZoneFilter>() ?? spawnedObject.AddComponent<NetworkVisibilityZoneFilter>();
        filter.InitializeForServer();
    }

    private static List<T> ParseJsonArray<T>(string json)
    {
        try
        {
            string wrapped = $"{{\"items\":{json}}}";
            JsonArrayWrapper<T> wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrapped);
            return wrapper?.items ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    [Serializable]
    private class JsonArrayWrapper<T>
    {
        public List<T> items;
    }

    [Serializable]
    private class BossConfigData
    {
        public int boss_id;
        public string boss_name;
        public int level;
        public int base_hp;
        public int base_damage;
        public float move_speed;
        public float attack_speed;
        public string element_type;
        public string skills_json;
        public string phases_json;

        [NonSerialized] public List<SkillData> skills;
        [NonSerialized] public List<PhaseData> phases;
    }

    [Serializable]
    private class SkillData
    {
        public string skill_id;
        public float damage_multiplier;
        public string element;
        public float cooldown_sec;
        public float range;
        public bool aoe;
        public string animation_trigger;
        public string status_effect;
        public float duration_sec;
        public int spawn_enemy_id;
        public int spawn_count;
    }

    [Serializable]
    private class PhaseData
    {
        public int hp_pct_threshold;
        public string action;
        public float damage_multiplier;
        public float speed_multiplier;
        public float skill_cooldown_multiplier;
        public float heal_pct;
        public int mob_id;
        public int mob_count;
        public string message;
    }
}
