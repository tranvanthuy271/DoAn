using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EnemySkillSet — Lưu danh sách skill của một enemy instance sau khi host fetch từ DB.
///
/// Component này được gắn bởi HostSpawnConfigLoader ngay sau NetworkObject.Spawn().
/// EnemyAI và BossAI đọc từ đây để quyết định có cast skill không.
///
/// Không cần gắn thủ công vào prefab — HostSpawnConfigLoader tự AddComponent nếu thiếu.
/// Không sync qua mạng — chỉ server/host cần; damage kết quả mới được sync.
///
/// ─── Luồng hoạt động ───
///  1. HostSpawnConfigLoader gọi SetSkillsFromConfig(EnemySkillsEntry)
///  2. EnemyAI.Update() gọi TryGetReadySkill() mỗi khi vào combat range
///  3. Nếu có skill ready → EnemyAI gọi MarkSkillUsed(skill_id) + thực thi skill
///  4. Sau cooldown_sec → skill lại ready
/// </summary>
public class EnemySkillSet : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  Public read-only data
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Sát thương cơ bản của enemy này (dùng khi skill chỉ có damage_multiplier).</summary>
    public int BaseDamage { get; private set; }

    /// <summary>Nguyên tố của enemy (Fire/Water/None/…).</summary>
    public string ElementType { get; private set; } = "None";

    /// <summary>Danh sách skill đã được load và validate. Readonly sau SetSkillsFromConfig().</summary>
    public IReadOnlyList<SkillEntry> Skills => _skills;

    /// <summary>True khi đã gọi SetSkillsFromConfig() ít nhất một lần.</summary>
    public bool HasSkills => _skills.Count > 0;

    // ─────────────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────────────

    private readonly List<SkillEntry> _skills = new List<SkillEntry>();

    // skill_id → thời gian (Time.time) lần cast cuối
    private readonly Dictionary<string, float> _lastCastTime = new Dictionary<string, float>();

    // ─────────────────────────────────────────────────────────────────────
    //  Setup
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Load + validate skills từ EnemySkillsEntry của DB response.
    /// Gọi bởi HostSpawnConfigLoader ngay sau NetworkObject.Spawn().
    /// </summary>
    public void SetSkillsFromConfig(EnemySkillsEntry entry)
    {
        _skills.Clear();
        _lastCastTime.Clear();

        if (entry == null) return;

        BaseDamage  = entry.base_damage > 0 ? entry.base_damage : 1;
        ElementType = string.IsNullOrEmpty(entry.element_type) ? "None" : entry.element_type;

        if (entry.skills == null || entry.skills.Length == 0) return;

        foreach (var skill in entry.skills)
        {
            if (!ValidateSkill(skill)) continue;

            // Đảm bảo có ít nhất một nguồn damage
            if (skill.flat_damage <= 0 && skill.damage_multiplier <= 0f)
                skill.damage_multiplier = 1.0f;

            if (skill.cooldown_sec <= 0f) skill.cooldown_sec = 5f;
            if (skill.range <= 0f)        skill.range        = 4f;
            if (skill.aoe && skill.aoe_radius <= 0f) skill.aoe_radius = 3f;

            _skills.Add(skill);
        }

        Debug.Log($"[EnemySkillSet] {gameObject.name}: {_skills.Count} skill(s) loaded. BaseDamage={BaseDamage}, Element={ElementType}");
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Runtime query
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về skill đầu tiên đã hết cooldown và trong tầm đánh.
    /// Trả về null nếu không có skill nào ready.
    /// </summary>
    /// <param name="distToTarget">Khoảng cách thực (Unity units) đến mục tiêu gần nhất.</param>
    public SkillEntry TryGetReadySkill(float distToTarget)
    {
        if (_skills.Count == 0) return null;

        foreach (var skill in _skills)
        {
            // SUMMON_ADD chỉ được BossAI dùng thông qua phase system — không cho EnemyAI thường
            if (skill.skill_id == "SUMMON_ADD") continue;

            if (distToTarget > skill.range) continue;

            // Kiểm tra cooldown
            if (_lastCastTime.TryGetValue(skill.skill_id, out float lastCast))
                if (Time.time - lastCast < skill.cooldown_sec) continue;

            return skill;
        }

        return null;
    }

    /// <summary>
    /// Đánh dấu skill đã được cast — bắt đầu tính cooldown.
    /// Gọi ngay khi bắt đầu cast (trước khi damage được áp dụng).
    /// </summary>
    public void MarkSkillUsed(string skillId)
    {
        _lastCastTime[skillId] = Time.time;
    }

    /// <summary>
    /// Tính sát thương thực tế của skill.
    /// Ưu tiên flat_damage; fallback về base_damage × damage_multiplier.
    /// </summary>
    public int CalculateDamage(SkillEntry skill)
    {
        if (skill.flat_damage > 0) return skill.flat_damage;
        return Mathf.Max(1, Mathf.RoundToInt(BaseDamage * skill.damage_multiplier));
    }

    /// <summary>
    /// Trả về tất cả skills của loại skill_id nhất định (thường chỉ 1).
    /// </summary>
    public SkillEntry GetSkillById(string skillId)
    {
        return _skills.Find(s => s.skill_id == skillId);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Validation
    // ─────────────────────────────────────────────────────────────────────

    private bool ValidateSkill(SkillEntry skill)
    {
        if (string.IsNullOrWhiteSpace(skill.skill_id))
        {
            Debug.LogWarning($"[EnemySkillSet] {gameObject.name}: Bỏ qua skill không có skill_id.");
            return false;
        }
        return true;
    }
}
