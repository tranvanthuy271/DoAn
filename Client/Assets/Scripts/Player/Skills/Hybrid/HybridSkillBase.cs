using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Base class cho tất cả Hybrid Skill (skill combo đặc biệt chỉ xuất hiện khi Hybrid Fusion).
///
/// ═══════════════════════════════════════════════════════════
/// CÁCH TẠO HYBRID SKILL MỚI:
///   1. Tạo class mới kế thừa HybridSkillBase
///   2. Override ExecuteSkill(Vector2 direction)
///   3. Gắn component vào Hybrid Prefab tương ứng
///   4. SkillRuntimeLoader tự động nhận diện qua prefix HYBRID_ trong skill_code
///
/// Ví dụ:
///   public class HybridMetalWindGaleSkill : HybridSkillBase
///   {
///       protected override void ExecuteSkill(Vector2 direction) { ... }
///   }
/// ═══════════════════════════════════════════════════════════
/// </summary>
public abstract class HybridSkillBase : NetworkBehaviour
{
    [Header("Hybrid Skill Base")]
    [Tooltip("Phải khớp đúng với skill_code trong DB — ví dụ: HYBRID_METAL_WIND_GALE")]
    [SerializeField] public string skillCode;

    [Tooltip("Cooldown giữa các lần dùng skill (giây)")]
    [SerializeField] public float cooldown = 15f;

    [Tooltip("MP tiêu hao mỗi lần dùng")]
    [SerializeField] public int mpCost = 55;

    [Tooltip("Giá trị sát thương / hiệu ứng cơ bản")]
    [SerializeField] public float effectValue = 300f;

    // ── Runtime state ─────────────────────────────────────────────
    private float _cooldownTimer;
    private bool  _canUse = true;

    public bool CanUseNow => _canUse;
    public float GetCooldownPercent()    => _canUse ? 1f : Mathf.Clamp01(1f - _cooldownTimer / cooldown);
    public float GetCooldownRemaining()  => _canUse ? 0f : Mathf.Max(0f, _cooldownTimer);

    protected PlayerAnimator PlayerAnimator { get; private set; }

    // ── Lifecycle ─────────────────────────────────────────────────
    protected virtual void Awake()
    {
        PlayerAnimator = GetComponentInParent<PlayerAnimator>();
    }

    protected virtual void Update()
    {
        if (!_canUse)
        {
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer <= 0f)
            {
                _cooldownTimer = 0f;
                _canUse        = true;
            }
        }
    }

    // ── Public API (gọi từ PlayerSkillManager) ────────────────────

    /// <summary>
    /// Cố gắng dùng skill. Kiểm tra cooldown. Chỉ gọi trên Owner.
    /// Trả về true nếu thực sự kích hoạt.
    /// </summary>
    public bool TryUse(Vector2 direction)
    {
        if (!IsOwner || !_canUse) return false;

        _canUse        = false;
        _cooldownTimer = cooldown;

        // Trigger ngay trên owner (không chờ server round-trip)
        PlayerAnimator?.TriggerAttack();

        UseSkillServerRpc(direction);
        return true;
    }

    [ServerRpc]
    private void UseSkillServerRpc(Vector2 direction)
    {
        ExecuteSkill(direction);
        PlayAnimationClientRpc();
    }

    [ClientRpc]
    private void PlayAnimationClientRpc()
    {
        // Owner đã trigger locally trong TryUse — chỉ trigger cho các client khác
        if (IsOwner) return;
        PlayerAnimator?.TriggerAttack();
    }

    /// <summary>
    /// Logic chính của skill — chạy trên Server.
    /// Spawn projectile, áp buff, v.v.
    /// </summary>
    protected abstract void ExecuteSkill(Vector2 direction);
}
