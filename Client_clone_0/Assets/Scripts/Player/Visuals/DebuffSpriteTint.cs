using UnityEngine;

/// <summary>
/// DebuffSpriteTint – Đổi màu (tint) SpriteRenderer của nhân vật khi bị debuff HOẶC khi nhận buff đồng đội.
///
/// Setup:
///   • Gắn component này vào cùng GameObject với SpriteRenderer (Player hoặc Enemy).
///   • Script tự tìm DebuffManager và PlayerBuffSync trên cùng GameObject hoặc parent.
///   • Không cần tạo thêm child object hay prefab gì thêm.
///
/// Hiệu ứng:
///   DEBUFF — bị tô màu theo loại debuff, fade out theo countdown.
///   BUFF   — bị tô màu buff (cyan hoặc vàng) khi đồng đội đang có buff active, fade out theo countdown.
///            ✦ Buff tint CHỈ xuất hiện khi PlayerBuffSync (trên cùng root) có buff active.
///            ✦ Tất cả clients đều thấy tint này vì PlayerBuffSync dùng NetworkVariable.
///   Ưu tiên: Freeze > Burn > Slow > Weaken > DefenseDown > ArmorBuff > AttackBuff.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DebuffSpriteTint : MonoBehaviour
{
    [Header("Debuff Tint Colors (A = cường độ tối đa)")]
    [SerializeField] private Color slowColor       = new Color(1f,   1f,   0f,   0.5f); // vàng
    [SerializeField] private Color weakenColor     = new Color(0.7f, 0f,   1f,   0.5f); // tím
    [SerializeField] private Color burnColor       = new Color(1f,   0.3f, 0f,   0.6f); // cam đỏ
    [SerializeField] private Color freezeColor     = new Color(0.3f, 0.8f, 1f,   0.7f); // xanh băng
    [SerializeField] private Color defenseDownColor= new Color(0.5f, 0f,   0f,   0.5f); // đỏ tối

    [Header("Buff Tint Colors (đồng đội nhận buff)")]
    [Tooltip("Màu tint khi nhận buff giáp (WaterArmor).")]
    [SerializeField] private Color armorBuffColor  = new Color(0.2f, 0.8f, 1f,   0.6f); // cyan
    [Tooltip("Màu tint khi nhận buff tấn công (EarthAura).")]
    [SerializeField] private Color attackBuffColor = new Color(1f,   0.85f, 0.1f, 0.5f); // vàng gold

    // ── Refs ──────────────────────────────────────────────────────────────────
    private SpriteRenderer _sr;
    private DebuffManager  _debuffManager;
    private PlayerBuffSync _buffSync;
    private Color          _originalColor;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        _sr            = GetComponent<SpriteRenderer>();
        _debuffManager = GetComponent<DebuffManager>() ?? GetComponentInParent<DebuffManager>();
        _buffSync      = GetComponent<PlayerBuffSync>() ?? GetComponentInParent<PlayerBuffSync>();
        _originalColor = _sr.color;
    }

    private void OnEnable()
    {
        if (_debuffManager != null)
            _debuffManager.OnDebuffsChanged += OnDebuffsChanged;
        if (_buffSync != null)
            _buffSync.OnBuffStateChanged += OnBuffChanged;
    }

    private void OnDisable()
    {
        if (_debuffManager != null)
            _debuffManager.OnDebuffsChanged -= OnDebuffsChanged;
        if (_buffSync != null)
            _buffSync.OnBuffStateChanged -= OnBuffChanged;
        // Đảm bảo sprite về màu gốc khi bị disable
        if (_sr != null)
            _sr.color = _originalColor;
    }

    private void OnDebuffsChanged() => ApplyTint();
    private void OnBuffChanged()    => ApplyTint();

    private void Update()
    {
        // Poll mỗi frame để alpha mờ dần theo countdown
        bool hasDebuff = _debuffManager != null && _debuffManager.HasAnyDebuff();
        bool hasBuff   = _buffSync != null && (_buffSync.IsArmorBuffActive() || _buffSync.IsAttackBuffActive());

        if (!hasDebuff && !hasBuff)
        {
            if (_sr.color != _originalColor)
                _sr.color = _originalColor;
            return;
        }
        ApplyTint();
    }

    // ── Core ──────────────────────────────────────────────────────────────────

    private void ApplyTint()
    {
        // 1. Ưu tiên debuff
        if (_debuffManager != null && _debuffManager.HasAnyDebuff())
        {
            (Color baseColor, float remaining, float total) = GetPriorityDebuffInfo();
            if (total > 0f)
            {
                float ratio = Mathf.Clamp01(remaining / total);
                Color tint  = baseColor;
                tint.a      = baseColor.a * ratio;
                _sr.color   = Color.Lerp(_originalColor, tint, tint.a);
                return;
            }
        }

        // 2. Buff tint (khi đồng đội có buff active)
        if (_buffSync != null)
        {
            // ArmorBuff ưu tiên hơn AttackBuff
            if (_buffSync.IsArmorBuffActive())
            {
                float remaining = _buffSync.GetArmorBuffRemaining();
                float total     = _buffSync.armorBuffTotalDuration.Value;
                if (total > 0f)
                {
                    float ratio = Mathf.Clamp01(remaining / total);
                    Color tint  = armorBuffColor;
                    tint.a      = armorBuffColor.a * ratio;
                    _sr.color   = Color.Lerp(_originalColor, tint, tint.a);
                    return;
                }
            }
            if (_buffSync.IsAttackBuffActive())
            {
                float remaining = _buffSync.GetAttackBuffRemaining();
                float total     = _buffSync.attackBuffTotalDuration.Value;
                if (total > 0f)
                {
                    float ratio = Mathf.Clamp01(remaining / total);
                    Color tint  = attackBuffColor;
                    tint.a      = attackBuffColor.a * ratio;
                    _sr.color   = Color.Lerp(_originalColor, tint, tint.a);
                    return;
                }
            }
        }

        _sr.color = _originalColor;
    }

    /// <summary>Trả về (màu, remaining, total) của debuff ưu tiên cao nhất.</summary>
    private (Color color, float remaining, float total) GetPriorityDebuffInfo()
    {
        float now = GetServerTime();
        var debuffs = _debuffManager.ActiveDebuffs;

        Color  best          = _originalColor;
        float  bestRemaining = 0f;
        float  bestTotal     = 0f;
        int    bestPriority  = -1;

        for (int i = 0; i < debuffs.Count; i++)
        {
            var e         = debuffs[i];
            float remain  = Mathf.Max(0f, e.ExpireServerTime - now);
            if (remain <= 0f) continue;

            int priority = GetPriority(e.Type);
            if (priority > bestPriority)
            {
                bestPriority  = priority;
                bestRemaining = remain;
                bestTotal     = e.TotalDuration;
                best          = GetDebuffColor(e.Type);
            }
        }

        return (best, bestRemaining, bestTotal);
    }

    private static int GetPriority(SkillDebuffType type)
    {
        return type switch
        {
            SkillDebuffType.Freeze      => 5,
            SkillDebuffType.Burn        => 4,
            SkillDebuffType.Slow        => 3,
            SkillDebuffType.Weaken      => 2,
            SkillDebuffType.DefenseDown => 1,
            _                           => 0,
        };
    }

    private Color GetDebuffColor(SkillDebuffType type)
    {
        return type switch
        {
            SkillDebuffType.Slow        => slowColor,
            SkillDebuffType.Weaken      => weakenColor,
            SkillDebuffType.Burn        => burnColor,
            SkillDebuffType.Freeze      => freezeColor,
            SkillDebuffType.DefenseDown => defenseDownColor,
            _                           => Color.white,
        };
    }

    private static float GetServerTime()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsListening)
            return (float)Unity.Netcode.NetworkManager.Singleton.ServerTime.TimeAsFloat;
        return Time.time;
    }
}
