using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Damage component gắn vào đầu đạn của HYBRID_METAL_WIND_BARRAGE.
/// Va chạm 1 lần rồi tự hủy.
///
/// Yêu cầu:
///   - Collider2D với Is Trigger = true
///   - NetworkObject trên cùng GameObject
/// </summary>
public class BarrageBulletDamage : MonoBehaviour
{
    /// <summary>Sát thương gây ra — được set bởi HybridMetalWindBarrageSkill khi spawn.</summary>
    [HideInInspector] public int damage = 120;

    /// <summary>Thời gian sống tối đa (giây) — được set bởi HybridMetalWindBarrageSkill khi spawn.</summary>
    [HideInInspector] public float lifetime = 2.5f;

    private bool _hasHit;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasHit) return;

        // Ưu tiên NetworkEnemyHealth (multiplayer)
        var netHealth = other.GetComponent<NetworkEnemyHealth>();
        if (netHealth != null)
        {
            _hasHit = true;
            netHealth.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // Fallback: EnemyHealth cũ (single-player / local test)
        var localHealth = other.GetComponent<EnemyHealth>();
        if (localHealth != null)
        {
            _hasHit = true;
            localHealth.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
