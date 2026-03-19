using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Damage component gắn vào đầu đạn của HYBRID_WATER_WOOD_VENOM.
/// Va chạm 1 lần rồi tự hủy. Khi trúng kẻ địch:
///   • Gây damage
///   • Áp slow (giảm tốc độ di chuyển)
///   • Chặn hồi HP
///
/// Yêu cầu:
///   - Collider2D với Is Trigger = true
///   - NetworkObject trên cùng GameObject
/// </summary>
public class VenomBulletDamage : MonoBehaviour
{
    /// <summary>Sát thương gây ra — được set bởi HybridWaterWoodVenomSkill khi spawn.</summary>
    [HideInInspector] public int damage = 250;

    /// <summary>Thời gian sống tối đa (giây) — được set bởi HybridWaterWoodVenomSkill khi spawn.</summary>
    [HideInInspector] public float lifetime = 3f;

    /// <summary>Thời gian áp slow lên kẻ địch (giây).</summary>
    [HideInInspector] public float slowDuration = 3f;

    /// <summary>Thời gian chặn hồi HP (giây).</summary>
    [HideInInspector] public float healBlockDuration = 3f;

    private bool _hasHit;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;

        // Ưu tiên NetworkEnemyHealth (multiplayer)
        var netHealth = other.GetComponent<NetworkEnemyHealth>()
                     ?? other.GetComponentInParent<NetworkEnemyHealth>();
        if (netHealth != null)
        {
            _hasHit = true;
            netHealth.TakeDamage(damage);

            // Áp slow lên EnemyAI
            var enemyAI = other.GetComponent<EnemyAI>()
                       ?? other.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.ApplySlow(slowDuration);

            // Chặn hồi HP cho enemy (nếu có hỗ trợ)
            netHealth.BlockHeal(healBlockDuration);

            Destroy(gameObject);
            return;
        }

        // Fallback: EnemyHealth cũ (single-player / local test)
        var localHealth = other.GetComponent<EnemyHealth>()
                       ?? other.GetComponentInParent<EnemyHealth>();
        if (localHealth != null)
        {
            _hasHit = true;
            localHealth.TakeDamage(damage);

            var enemyAI = other.GetComponent<EnemyAI>()
                       ?? other.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.ApplySlow(slowDuration);

            Destroy(gameObject);
            return;
        }

        // Chặn hồi HP cho player khác (PvP)
        var playerHealth = other.GetComponent<PlayerHealth>()
                        ?? other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            _hasHit = true;
            playerHealth.BlockHeal(healBlockDuration);

            var netPlayerHealth = other.GetComponent<NetworkPlayerHealth>()
                               ?? other.GetComponentInParent<NetworkPlayerHealth>();
            netPlayerHealth?.BlockHealServerRpc(healBlockDuration);

            Destroy(gameObject);
        }
    }
}
