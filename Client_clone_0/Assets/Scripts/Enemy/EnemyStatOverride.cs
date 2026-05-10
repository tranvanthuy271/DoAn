using UnityEngine;

/// <summary>
/// EnemyStatOverride — Lưu thông số ghi đè (HP, EXP, is_boss, respawn_time)
/// cho một enemy instance được spawn từ HostSpawnConfigLoader.
///
/// HostSpawnConfigLoader gọi Apply() ngay sau NetworkObject.Spawn().
/// Component này KHÔNG sync qua mạng — chỉ server cần các giá trị này.
///
/// Không cần thêm vào prefab thủ công; HostSpawnConfigLoader tự AddComponent nếu thiếu.
/// </summary>
public class EnemyStatOverride : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    //  Overridden stats (read-only sau khi Apply() được gọi)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>HP tối đa ghi đè. 0 = dùng giá trị mặc định trong prefab.</summary>
    public int OverrideHp      { get; private set; }

    /// <summary>EXP thưởng khi giết. 0 = dùng giá trị mặc định.</summary>
    public int OverrideExp     { get; private set; }

    /// <summary>True = enemy này hoạt động ở chế độ boss.</summary>
    public bool IsBoss         { get; private set; }

    /// <summary>Giây chờ hồi sinh. Dùng bởi respawn logic trên host.</summary>
    public int RespawnTime     { get; private set; }

    /// <summary>Level của enemy (dùng để hiển thị trong UI).</summary>
    public int Level           { get; private set; } = 1;

    /// <summary>Tên quái lấy từ DB (dùng để hiển thị trong EnemyInfoPanel).</summary>
    public string EnemyName    { get; private set; } = "";

    /// <summary>True sau khi Apply() đã được gọi ít nhất một lần.</summary>
    public bool IsApplied      { get; private set; }

    // ─────────────────────────────────────────────────────────────────────
    //  Apply — gọi ngay sau NetworkObject.Spawn()
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Áp dụng thông số ghi đè cho enemy này.
    /// Gọi ngay sau NetworkObject.Spawn() trên host.
    /// </summary>
    /// <param name="hp">HP tối đa. 0 → fallback về prefab mặc định.</param>
    /// <param name="exp">EXP thưởng khi kill. 0 → fallback về mặc định.</param>
    /// <param name="isBoss">Kích hoạt chế độ boss nếu true.</param>
    /// <param name="respawnTime">Giây hồi sinh. ≤0 → dùng 30 giây mặc định.</param>
    /// <param name="level">Level của enemy. ≤0 → mặc định 1.</param>
    /// <param name="enemyName">Tên quái từ DB. Rỗng → giữ tên prefab.</param>
    public void Apply(int hp, int exp, bool isBoss, int respawnTime, int level = 1, string enemyName = "")
    {
        OverrideHp   = hp;
        OverrideExp  = exp;
        IsBoss       = isBoss;
        RespawnTime  = respawnTime > 0 ? respawnTime : 30;
        Level        = level > 0 ? level : 1;
        EnemyName    = enemyName ?? "";
        IsApplied    = true;

        BossAI existingBossAI = GetComponent<BossAI>();
        EnemyAI existingEnemyAI = GetComponent<EnemyAI>();
        if (ShouldLogBoss25(isBoss, existingBossAI))
        {
            Debug.LogWarning(
                $"[BOSS25][EnemyStatOverride:{name}] Apply hp={hp} exp={exp} isBoss={isBoss} respawn={RespawnTime} level={Level} enemyName='{EnemyName}' hasBossAI={(existingBossAI != null)} bossAIEnabledBefore={(existingBossAI != null && existingBossAI.enabled)} hasEnemyAI={(existingEnemyAI != null)} enemyAIEnabledBefore={(existingEnemyAI != null && existingEnemyAI.enabled)} scene={gameObject.scene.name}",
                this);
        }

        ApplyHealth(hp);
        ApplyBossMode(isBoss);
        ApplyExpOverride(exp);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Private apply helpers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Đặt HP tối đa và HP hiện tại qua NetworkEnemyHealth.InitHealth().</summary>
    private void ApplyHealth(int hp)
    {
        if (hp <= 0) return; // hp=0 → giữ nguyên default trong prefab

        NetworkEnemyHealth health = GetComponent<NetworkEnemyHealth>();
        if (health != null)
        {
            health.InitHealth(hp);
            return;
        }

        // Fallback: EnemyHealth (standalone mode)
        EnemyHealth standaloneHealth = GetComponent<EnemyHealth>();
        if (standaloneHealth != null)
        {
            standaloneHealth.InitHealth(hp);
        }
    }

    /// <summary>
    /// Kích hoạt / tắt Boss mode trên EnemyAI hoặc BossAI.
    /// Nếu isBoss = true và không có BossAI → log warning (thiếu component).
    /// </summary>
    private void ApplyBossMode(bool isBoss)
    {
        // Tắt/bật EnemyAI bình thường
        EnemyAI normalAI = GetComponent<EnemyAI>();

        // Bật/tắt BossAI
        BossAI bossAI = GetComponent<BossAI>();
        bool shouldLog = ShouldLogBoss25(isBoss, bossAI);

        if (shouldLog)
        {
            Debug.LogWarning(
                $"[BOSS25][EnemyStatOverride:{name}] ApplyBossMode BEFORE isBoss={isBoss} hasBossAI={(bossAI != null)} bossAIEnabled={(bossAI != null && bossAI.enabled)} hasEnemyAI={(normalAI != null)} enemyAIEnabled={(normalAI != null && normalAI.enabled)}",
                this);
        }

        if (isBoss)
        {
            if (bossAI == null)
            {
                Debug.LogWarning($"[EnemyStatOverride] Enemy '{gameObject.name}' được đánh dấu is_boss=true nhưng không có BossAI component. Thêm BossAI vào prefab.");
                // Không crash — vẫn dùng EnemyAI bình thường
            }
            else
            {
                bossAI.enabled = true;
                if (normalAI != null) normalAI.enabled = false;
            }
        }
        else
        {
            if (bossAI != null) bossAI.enabled = false;
            if (normalAI != null) normalAI.enabled = true;
        }

        if (shouldLog)
        {
            Debug.LogWarning(
                $"[BOSS25][EnemyStatOverride:{name}] ApplyBossMode AFTER isBoss={isBoss} bossAIEnabled={(bossAI != null && bossAI.enabled)} enemyAIEnabled={(normalAI != null && normalAI.enabled)}",
                this);
        }
    }

    private bool ShouldLogBoss25(bool isBoss, BossAI bossAI)
    {
        return isBoss
            || bossAI != null
            || gameObject.name.Contains("Enemy 25");
    }

    /// <summary>
    /// Lưu EXP override vào NetworkEnemyHealth để HandleDeath() trả đúng EXP.
    /// Nếu exp = 0 thì không ghi đè (giữ giá trị default).
    /// </summary>
    private void ApplyExpOverride(int exp)
    {
        if (exp <= 0) return;

        NetworkEnemyHealth health = GetComponent<NetworkEnemyHealth>();
        if (health != null)
        {
            health.SetExpReward(exp);
            return;
        }

        EnemyHealth standaloneHealth = GetComponent<EnemyHealth>();
        if (standaloneHealth != null)
        {
            standaloneHealth.SetExpReward(exp);
        }
    }
}
