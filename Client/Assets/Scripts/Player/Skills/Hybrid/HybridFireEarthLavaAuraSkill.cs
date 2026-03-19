using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// HYBRID_FIRE_EARTH_LAVA_AURA — "Hỏa Thổ Dung Nham"
/// Tạo vùng dung nham bao quanh người chơi trong <auraDuration> giây.
/// Bất kỳ ai đi vào bán kính <auraRadius> sẽ:
///   • Mất HP liên tục mỗi <tickInterval> giây (<effectValue> sát thương/tick)
///   • Không thể hồi HP trong vòng <healBlockDuration> giây
///
/// ═══════════════════════════════════════════════════════════════════════════╗
/// SETUP TRONG UNITY — thực hiện trên F_Hoa.prefab VÀ F_Tho.prefab          ║
/// ───────────────────────────────────────────────────────────────────────── ║
///  1. Chọn root GameObject → Add Component → HybridFireEarthLavaAuraSkill   ║
///  2. skillCode         = "HYBRID_FIRE_EARTH_LAVA_AURA"                     ║
///  3. cooldown          = 14                                                 ║
///  4. mpCost            = 60                                                 ║
///  5. effectValue       = 25   (sát thương mỗi tick)                        ║
///  6. auraRadius        = 3    (bán kính dung nham, units)                  ║
///  7. auraDuration      = 8    (thời gian duy trì, giây)                    ║
///  8. tickInterval      = 0.5  (khoảng cách giữa các tick damage, giây)     ║
///  9. healBlockDuration = 2    (thời gian chặn hồi HP mỗi tick, giây)       ║
/// 10. Trong PlayerSkillManager trên cùng prefab:                            ║
///       → Thêm vào danh sách skills:                                        ║
///           skillType = HybridLavaAura                                      ║
///           activationKey = <phím 4 hoặc tùy cấu hình>                     ║
///           animationTriggerName = "HybridSkill"                            ║
/// ═══════════════════════════════════════════════════════════════════════════╝
/// </summary>
public class HybridFireEarthLavaAuraSkill : HybridSkillBase
{
    [Header("Lava Aura – Range")]
    [Tooltip("Bán kính vùng dung nham (units)")]
    [SerializeField] private float auraRadius = 3f;

    [Header("Lava Aura – Duration")]
    [Tooltip("Thời gian duy trì aura (giây)")]
    [SerializeField] private float auraDuration = 8f;

    [Tooltip("Khoảng cách giữa các lần gây sát thương (giây)")]
    [SerializeField] private float tickInterval = 0.5f;

    [Header("Lava Aura – Heal Block")]
    [Tooltip("Thời gian chặn hồi HP áp lên mục tiêu mỗi tick (giây). "
           + "Phải lớn hơn tickInterval để hiệu ứng duy trì liên tục.")]
    [SerializeField] private float healBlockDuration = 2f;

    // Layer masks — gán qua Inspector hoặc để script tự detect
    [Header("Lava Aura – Layers")]
    [Tooltip("Layer của Enemy (mặc định: layer 7)")]
    [SerializeField] private LayerMask enemyLayer;

    [Tooltip("Layer của Player (mặc định: layer 8)")]
    [SerializeField] private LayerMask playerLayer;

    protected override void Awake()
    {
        base.Awake();

        // Auto-fill layers nếu chưa được gán trong Inspector
        if (enemyLayer.value == 0)
            enemyLayer = 1 << LayerMask.NameToLayer("Enemy");
        if (playerLayer.value == 0)
            playerLayer = 1 << 8; // layer 8 = Player (khớp với WaterArmorBuffSkill)
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ExecuteSkill — chạy trên Server (gọi từ HybridSkillBase.UseSkillServerRpc)
    // ─────────────────────────────────────────────────────────────────────────

    protected override void ExecuteSkill(Vector2 direction)
    {
        StartCoroutine(LavaAuraSequence());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Coroutine chính: tick damage + heal block
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator LavaAuraSequence()
    {
        float elapsed = 0f;
        int tickDamage = Mathf.RoundToInt(effectValue);

        ShowAuraClientRpc(true);

        while (elapsed < auraDuration)
        {
            yield return new WaitForSeconds(tickInterval);
            elapsed += tickInterval;

            Vector2 center = transform.position;

            // 1. Gây sát thương cho quái vật
            Collider2D[] enemyHits = Physics2D.OverlapCircleAll(center, auraRadius, enemyLayer);
            foreach (var col in enemyHits)
            {
                // Ưu tiên NetworkEnemyHealth (multiplayer)
                var netEnemy = col.GetComponent<NetworkEnemyHealth>()
                            ?? col.GetComponentInParent<NetworkEnemyHealth>();
                if (netEnemy != null)
                {
                    netEnemy.TakeDamageServerRpc(tickDamage);
                    continue;
                }

                // Fallback: EnemyHealth (single-player / local test)
                var localEnemy = col.GetComponent<EnemyHealth>()
                              ?? col.GetComponentInParent<EnemyHealth>();
                localEnemy?.TakeDamage(tickDamage);
            }

            // 2. Áp hiệu ứng chặn hồi HP lên tất cả player trong vùng
            Collider2D[] playerHits = Physics2D.OverlapCircleAll(center, auraRadius, playerLayer);
            foreach (var col in playerHits)
            {
                // PlayerHealth (local heal block — visual/offline)
                var ph = col.GetComponent<PlayerHealth>()
                      ?? col.GetComponentInParent<PlayerHealth>();
                ph?.BlockHeal(healBlockDuration);

                // NetworkPlayerHealth (server-authoritative heal block)
                var nph = col.GetComponent<NetworkPlayerHealth>()
                       ?? col.GetComponentInParent<NetworkPlayerHealth>();
                nph?.BlockHealServerRpc(healBlockDuration);
            }
        }

        ShowAuraClientRpc(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ClientRpc: bật / tắt hiệu ứng aura (visual)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Bật / tắt hiệu ứng aura dung nham (VFX hoặc particle) trên tất cả clients.
    /// Cần thêm AudioSource / ParticleSystem trong Inspector để tăng hiệu ứng.
    /// </summary>
    [ClientRpc]
    private void ShowAuraClientRpc(bool show)
    {
        // TODO: Thay bằng particle / VFX lava aura khi có asset
        // Ví dụ: lavaAuraVFX.SetActive(show);
        Debug.Log($"[HybridFireEarthLavaAuraSkill] Lava Aura {(show ? "BẬT" : "TẮT")}");

        // Khi tắt aura → reset animator SkillEffect về trạng thái mặc định
        if (!show)
        {
            var skillEffect = transform.Find("SkillEffect")
                           ?? transform.parent?.Find("SkillEffect");
            if (skillEffect != null)
            {
                var animator = skillEffect.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.ResetTrigger("Skill4");
                    animator.ResetTrigger("HybridSkill");
                    animator.Play("New State", 0, 0f);
                }

                var sr = skillEffect.GetComponent<SpriteRenderer>();
                if (sr != null) sr.sprite = null;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Gizmos (hỗ trợ debug trong Scene view)
    // ─────────────────────────────────────────────────────────────────────────

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, auraRadius);
    }
}
