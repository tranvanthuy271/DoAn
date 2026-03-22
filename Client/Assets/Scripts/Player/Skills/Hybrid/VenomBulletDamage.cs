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
public class VenomBulletDamage : NetworkBehaviour
{
    /// <summary>Sát thương gây ra — được set bởi HybridWaterWoodVenomSkill khi spawn.</summary>
    [HideInInspector] public int damage = 250;

    /// <summary>Thời gian sống tối đa (giây) — được set bởi HybridWaterWoodVenomSkill khi spawn.</summary>
    [HideInInspector] public float lifetime = 3f;

    /// <summary>Thời gian áp slow lên kẻ địch (giây).</summary>
    [HideInInspector] public float slowDuration = 3f;

    /// <summary>Thời gian chặn hồi HP (giây).</summary>
    [HideInInspector] public float healBlockDuration = 3f;

    /// <summary>NetworkObjectId của caster để tránh tự gây damage cho mình.</summary>
    [HideInInspector] public ulong ownerNetworkObjectId = 0;

    private bool _hasHit;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Force-start projectile animation on ALL instances (host + all clients)
        var anim = GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }
    }

    private void Start()
    {
        // Chỉ server mới schedule despawn — client không được gọi Destroy trên NetworkObject
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        StartCoroutine(ServerLifetimeDespawn());
    }

    private System.Collections.IEnumerator ServerLifetimeDespawn()
    {
        yield return new UnityEngine.WaitForSeconds(lifetime);
        DespawnOrDestroy();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ server xử lý damage để tránh gọi nhiều lần từ các client
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (_hasHit) return;

        // Bỏ qua nếu va chạm với chính người dùng skill
        var targetNetObj = other.GetComponent<NetworkObject>();
        if (targetNetObj != null && ownerNetworkObjectId != 0
            && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
            return;

        // Ưu tiên NetworkEnemyHealth (multiplayer)
        var netHealth = other.GetComponent<NetworkEnemyHealth>()
                     ?? other.GetComponentInParent<NetworkEnemyHealth>();
        if (netHealth != null)
        {
            _hasHit = true;
            netHealth.TakeDamage(damage);

            var enemyAI = other.GetComponent<EnemyAI>()
                       ?? other.GetComponentInParent<EnemyAI>();
            if (enemyAI != null)
                enemyAI.ApplySlow(slowDuration);

            netHealth.BlockHeal(healBlockDuration);

            DespawnOrDestroy();
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

            DespawnOrDestroy();
            return;
        }

        // PvP: gây damage cho player khác
        if (other.CompareTag("Player"))
        {
            var netPlayerHealth = other.GetComponent<NetworkPlayerHealth>()
                               ?? other.GetComponentInParent<NetworkPlayerHealth>();
            if (netPlayerHealth != null)
            {
                _hasHit = true;
                netPlayerHealth.TakeDamage(damage);
                netPlayerHealth.BlockHealServerRpc(healBlockDuration);
                DespawnOrDestroy();
            }
        }
    }

    private void DespawnOrDestroy()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Gọi sau khi Spawn() để set velocity trên tất cả client.
    /// Server tự set velocity trực tiếp; ClientRpc chỉ chạy trên các client còn lại.
    /// </summary>
    [ClientRpc]
    public void SetVelocityClientRpc(Vector2 velocity)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer) return;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = velocity;
    }
}
