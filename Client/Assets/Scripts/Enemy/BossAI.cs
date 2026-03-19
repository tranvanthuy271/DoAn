using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

/// <summary>
/// BossAI — AI Boss nâng cao với hệ thống Phase và Kỹ Năng.
///
/// CÁCH HOẠT ĐỘNG:
///   • Load config từ API GET /api/dungeon/boss/{bossId}/config khi Awake
///   • Phase: theo dõi %HP → kích hoạt action khi vượt ngưỡng
///   • Skill: mỗi skill có cooldown riêng, boss tự động cast
///   • Spawn adds: Boss có thể triệu hồi thêm quái
///
/// PHASES (từ LangLa BossTpl + InfoMap.isBossAi):
///   enrage   → tăng damage/speed
///   summon   → spawn thêm mob
///   heal     → hồi HP %
///   berserk  → damage * 2, cooldown / 2
///
/// SETUP:
///   1. Attach vào Boss prefab cùng với EnemyHealth
///   2. Set bossId khớp với enemy.enemy_id trong DB
///   3. Enemy prefab cho các skill (fireBreathPrefab, novaEffectPrefab...)
/// </summary>
[RequireComponent(typeof(EnemyHealth))]
public class BossAI : MonoBehaviour
{
    [Header("Boss ID (khớp DB enemy.enemy_id)")]
    public int bossId = 8;

    [Header("Combat References")]
    public Transform playerTarget;
    public float detectionRange   = 12f;
    public float meleeAttackRange = 2.0f;
    public float chaseSpeed       = 2.5f;

    [Header("Skill Prefabs (gán từ Editor)")]
    [Tooltip("Prefab hiệu ứng tấn công thở lửa/băng/gió")] 
    public GameObject skillBreathPrefab;
    [Tooltip("Prefab hiệu ứng bùng nổ vùng AoE")]
    public GameObject skillNovaPrefab;
    [Tooltip("Prefab quái spawn thêm (Adds)")]
    public GameObject addSpawnPrefab;

    [Header("Phase Text (optional)")]
    public TMPro.TextMeshProUGUI phaseAnnounceText;

    // ── Runtime state ──
    private EnemyHealth  _health;
    private Rigidbody2D  _rb;
    private Animator     _anim;

    private BossConfigData _config;
    private bool           _configLoaded = false;

    // Phase tracking
    private readonly HashSet<int> _triggeredPhases = new();  // hp_pct_threshold đã trigger
    private float _damageMultiplier   = 1f;
    private float _speedMultiplier    = 1f;
    private float _cooldownMultiplier = 1f;

    // Skill cooldown tracking  [skill_id → lastCastTime]
    private readonly Dictionary<string, float> _skillLastCast = new();

    private enum BossState { Idle, Chase, Skill, Dead }
    private BossState _state = BossState.Idle;

    // ──────────────────────────────────────────────

    private void Awake()
    {
        _health = GetComponent<EnemyHealth>();
        _rb     = GetComponent<Rigidbody2D>();
        _anim   = GetComponent<Animator>();

        _health.OnDeath.AddListener(OnDeath);
        _health.OnTakeDamage.AddListener(OnDamageTaken);
    }

    private void Start()
    {
        StartCoroutine(LoadConfigFromServer());
        FindNearestPlayer();
    }

    // ══════════════════════════════════════════════
    // Config Loading
    // ══════════════════════════════════════════════

    private IEnumerator LoadConfigFromServer()
    {
        string url = $"{ServerConfig.BaseUrl}/api/dungeon/boss/{bossId}/config";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[BossAI] Không load được config boss #{bossId}: {req.error}. Dùng default.");
            _configLoaded = true;
            yield break;
        }

        try
        {
            _config = JsonUtility.FromJson<BossConfigData>(req.downloadHandler.text);

            // Deserialize nested JSON strings (skills_json và phases_json là JSON trong JSON)
            if (!string.IsNullOrEmpty(_config.skills_json))
                _config.skills = ParseJsonArray<SkillData>(_config.skills_json);
            if (!string.IsNullOrEmpty(_config.phases_json))
                _config.phases = ParseJsonArray<PhaseData>(_config.phases_json);

            _configLoaded = true;
            Debug.Log($"[BossAI] Loaded: {_config.boss_name} | Skills: {_config.skills?.Count ?? 0} | Phases: {_config.phases?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BossAI] Parse config lỗi: {ex.Message}");
            _configLoaded = true;
        }
    }

    // ══════════════════════════════════════════════
    // Update Loop
    // ══════════════════════════════════════════════

    private void Update()
    {
        if (!_configLoaded || _state == BossState.Dead) return;

        RefreshPlayerTarget();
        CheckPhases();

        if (_state != BossState.Skill)
            RunStateMachine();
    }

    private void RunStateMachine()
    {
        if (playerTarget == null) { _state = BossState.Idle; return; }

        float dist = Vector2.Distance(transform.position, playerTarget.position);

        if (dist > detectionRange) { _state = BossState.Idle; return; }

        // Thử cast skill (ưu tiên skill trước melee)
        if (TryUseSkill())
        {
            _state = BossState.Skill;
            return;
        }

        if (dist <= meleeAttackRange)
        {
            MeleeAttack();
        }
        else
        {
            ChasePlayer(dist);
        }
    }

    // ══════════════════════════════════════════════
    // Phase System (từ LangLa BossTpl + phases_json)
    // ══════════════════════════════════════════════

    private void CheckPhases()
    {
        if (_config?.phases == null) return;
        if (_health == null) return;

        int maxHp = _health.GetMaxHealth();
        float hpPct = maxHp > 0
            ? (_health.GetCurrentHealth() / (float)maxHp) * 100f
            : 100f;

        foreach (var phase in _config.phases)
        {
            if (_triggeredPhases.Contains(phase.hp_pct_threshold)) continue;
            if (hpPct <= phase.hp_pct_threshold)
            {
                _triggeredPhases.Add(phase.hp_pct_threshold);
                StartCoroutine(ExecutePhase(phase));
            }
        }
    }

    private IEnumerator ExecutePhase(PhaseData phase)
    {
        Debug.Log($"[BossAI] Phase trigger: {phase.action} @ {phase.hp_pct_threshold}%HP");
        AnnouncePhase(phase.message);

        switch (phase.action)
        {
            case "enrage":
                _damageMultiplier *= phase.damage_multiplier > 0 ? phase.damage_multiplier : 1.2f;
                _speedMultiplier  *= phase.speed_multiplier  > 0 ? phase.speed_multiplier  : 1.1f;
                if (_anim) _anim.SetTrigger("enrage");
                break;

            case "summon":
                yield return SummonAdds(phase.mob_id, phase.mob_count > 0 ? phase.mob_count : 2);
                break;

            case "heal":
                float healPct = phase.heal_pct > 0 ? phase.heal_pct : 15f;
                int healAmt   = Mathf.RoundToInt(_health.GetMaxHealth() * healPct / 100f);
                _health.Heal(healAmt);
                if (_anim) _anim.SetTrigger("heal");
                break;

            case "berserk":
                _damageMultiplier   *= phase.damage_multiplier > 0 ? phase.damage_multiplier : 2f;
                _speedMultiplier    *= phase.speed_multiplier  > 0 ? phase.speed_multiplier  : 1.3f;
                _cooldownMultiplier *= phase.skill_cooldown_multiplier > 0 ? phase.skill_cooldown_multiplier : 0.5f;
                if (_anim) _anim.SetTrigger("berserk");
                break;
        }
    }

    private IEnumerator SummonAdds(int mobId, int count)
    {
        if (addSpawnPrefab == null) yield break;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = UnityEngine.Random.insideUnitCircle * 3f;
            var add = Instantiate(addSpawnPrefab, (Vector2)transform.position + offset, Quaternion.identity);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                var netObj = add.GetComponent<NetworkObject>();
                netObj?.Spawn();
            }
            yield return new WaitForSeconds(0.3f);
        }
    }

    // ══════════════════════════════════════════════
    // Skill System
    // ══════════════════════════════════════════════

    private bool TryUseSkill()
    {
        if (_config?.skills == null || playerTarget == null) return false;

        foreach (var skill in _config.skills)
        {
            float cooldown = skill.cooldown_sec * _cooldownMultiplier;

            if (_skillLastCast.TryGetValue(skill.skill_id, out float lastCast))
                if (Time.time - lastCast < cooldown) continue;

            float dist = Vector2.Distance(transform.position, playerTarget.position);
            if (dist > skill.range * 1.2f) continue;  // out of skill range

            // Không cast SUMMON_ADD nếu đã ở Phase trigger (summon chỉ từ phase)
            if (skill.skill_id == "SUMMON_ADD") continue;

            _skillLastCast[skill.skill_id] = Time.time;
            StartCoroutine(CastSkill(skill));
            return true;
        }

        return false;
    }

    private IEnumerator CastSkill(SkillData skill)
    {
        if (!string.IsNullOrEmpty(skill.animation_trigger) && _anim)
            _anim.SetTrigger(skill.animation_trigger);

        yield return new WaitForSeconds(0.3f);  // cast animation

        if (skill.aoe)
            CastAoeSkill(skill);
        else
            CastDirectSkill(skill);

        yield return new WaitForSeconds(0.5f);
        _state = BossState.Chase;
    }

    private void CastDirectSkill(SkillData skill)
    {
        if (skillBreathPrefab == null || playerTarget == null) return;

        Vector2 dir = (playerTarget.position - transform.position).normalized;
        var obj = Instantiate(skillBreathPrefab, transform.position, Quaternion.identity);

        // Tính damage
        int baseDmg = _config != null ? _config.base_damage : 30;
        int dmg     = Mathf.RoundToInt(baseDmg * skill.damage_multiplier * _damageMultiplier);

        var proj = obj.GetComponent<EnemyProjectile>();
        if (proj != null)
        {
            proj.damage = dmg;
        }

        // Di chuyển projectile theo hướng target
        var rb = obj.GetComponent<Rigidbody2D>();
        if (rb) rb.velocity = dir * 8f;
    }

    private void CastAoeSkill(SkillData skill)
    {
        if (skillNovaPrefab == null) return;

        var obj = Instantiate(skillNovaPrefab, transform.position, Quaternion.identity);
        int baseDmg = _config != null ? _config.base_damage : 30;
        int dmg     = Mathf.RoundToInt(baseDmg * skill.damage_multiplier * _damageMultiplier);

        // Tìm tất cả player trong range và gây damage
        var colliders = Physics2D.OverlapCircleAll(transform.position, skill.range);
        foreach (var col in colliders)
        {
            if (!col.CompareTag("Player")) continue;
            var ph = col.GetComponent<PlayerHealth>();
            ph?.TakeDamage(dmg);
        }

        Destroy(obj, 2f);
    }

    // ══════════════════════════════════════════════
    // Melee & Movement
    // ══════════════════════════════════════════════

    private void MeleeAttack()
    {
        if (_anim) _anim.SetTrigger("attack");
        // Damage áp dụng qua hitbox + Animation Event (giữ pattern cũ của EnemyAI)
    }

    private void ChasePlayer(float dist)
    {
        if (_rb == null || playerTarget == null) return;

        float speed = chaseSpeed * _speedMultiplier;
        Vector2 dir = (playerTarget.position - transform.position).normalized;
        _rb.velocity = new Vector2(dir.x * speed, _rb.velocity.y);

        if (_anim) _anim.SetBool("isMoving", true);

        // Flip sprite
        if (dir.x > 0.05f) transform.localScale = new Vector3(1, 1, 1);
        else if (dir.x < -0.05f) transform.localScale = new Vector3(-1, 1, 1);
    }

    // ══════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════

    /// <summary>
    /// JsonUtility không hỗ trợ top-level array, dùng wrapper trick.
    /// Wrap: {"items":[...]} rồi deserialize.
    /// </summary>
    private static List<T> ParseJsonArray<T>(string json)
    {
        try
        {
            string wrapped = $"{{\"items\":{json}}}";
            var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrapped);
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

    private void FindNearestPlayer()
    {
        float nearest = float.MaxValue;
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            float d = Vector2.Distance(transform.position, go.transform.position);
            if (d < nearest) { nearest = d; playerTarget = go.transform; }
        }
    }

    private void RefreshPlayerTarget()
    {
        if (playerTarget != null) return;
        FindNearestPlayer();
    }

    private void AnnouncePhase(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        if (phaseAnnounceText != null)
        {
            phaseAnnounceText.text = msg;
            phaseAnnounceText.gameObject.SetActive(true);
            StartCoroutine(HideAfter(phaseAnnounceText.gameObject, 3f));
        }
        Debug.Log($"[Boss] {msg}");
    }

    private IEnumerator HideAfter(GameObject go, float t)
    {
        yield return new WaitForSeconds(t);
        if (go) go.SetActive(false);
    }

    private void OnDamageTaken() { /* hook cho hiệu ứng flicker/hit */ }

    private void OnDeath()
    {
        _state = BossState.Dead;
        if (_rb) _rb.velocity = Vector2.zero;
        if (_anim) _anim.SetTrigger("die");
    }

    // ══════════════════════════════════════════════
    // Data Models (deserialize từ API JSON)
    // ══════════════════════════════════════════════

    [Serializable]
    private class BossConfigData
    {
        public int    boss_id;
        public string boss_name;
        public int    level;
        public int    base_hp;
        public int    base_damage;
        public float  move_speed;
        public float  attack_speed;
        public string element_type;
        public string skills_json;
        public string phases_json;

        // Deserialized từ skills_json / phases_json
        [NonSerialized] public List<SkillData>  skills;
        [NonSerialized] public List<PhaseData>  phases;
    }

    [Serializable]
    private class SkillData
    {
        public string skill_id;
        public float  damage_multiplier;
        public string element;
        public float  cooldown_sec;
        public float  range;
        public bool   aoe;
        public string animation_trigger;
        public string status_effect;
        public float  duration_sec;
        public int    spawn_enemy_id;
        public int    spawn_count;
    }

    [Serializable]
    private class PhaseData
    {
        public int    hp_pct_threshold;
        public string action;
        public float  damage_multiplier;
        public float  speed_multiplier;
        public float  skill_cooldown_multiplier;
        public float  heal_pct;
        public int    mob_id;
        public int    mob_count;
        public string message;
    }
}


