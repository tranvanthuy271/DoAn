using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Damage component gắn vào đầu đạn của HYBRID_METAL_WIND_BARRAGE.
/// Va chạm 1 lần rồi tự hủy.
///
/// Yêu cầu:
///   - Collider2D với Is Trigger = true
///   - NetworkObject trên cùng GameObject
/// </summary>
public class BarrageBulletDamage : NetworkBehaviour
{
    /// <summary>Sát thương gây ra — được set bởi HybridMetalWindBarrageSkill khi spawn.</summary>
    [HideInInspector] public int damage = 120;

    /// <summary>Thời gian sống tối đa (giây) — được set bởi HybridMetalWindBarrageSkill khi spawn.</summary>
    [HideInInspector] public float lifetime = 2.5f;

    /// <summary>NetworkObjectId của caster để tránh tự gây damage cho mình.</summary>
    [HideInInspector] public ulong ownerNetworkObjectId = 0;

    private bool _hasHit;

    private void Start()
    {
        // Chỉ server mới schedule despawn — client không được gọi Destroy trên NetworkObject
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;
        StartCoroutine(ServerLifetimeDespawn());
    }

    private IEnumerator ServerLifetimeDespawn()
    {
        yield return new WaitForSeconds(lifetime);
        DespawnOrDestroy();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ server xử lý damage để tránh gọi nhiều lần từ các client
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return;

        if (_hasHit) return;

        // Bỏ qua nếu va chạm với chính người dùng skill
        var targetNetObj = other.GetComponent<NetworkObject>();
        if (targetNetObj != null && ownerNetworkObjectId != 0
            && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
            return;

        // Ưu tiên NetworkEnemyHealth (multiplayer, tìm cả parent)
        var netHealth = other.GetComponentInParent<NetworkEnemyHealth>();
        if (netHealth != null)
        {
            _hasHit = true;
            netHealth.TakeDamage(damage, GetOwnerClientId());
            DespawnOrDestroy();
            return;
        }

        // Fallback: EnemyHealth cũ (single-player / local test)
        var localHealth = other.GetComponentInParent<EnemyHealth>();
        if (localHealth != null)
        {
            _hasHit = true;
            localHealth.TakeDamage(damage);
            DespawnOrDestroy();
            return;
        }

        // PvP: gây damage cho player khác
        if (other.CompareTag("Player"))
        {
            var netPlayer = other.GetComponentInParent<NetworkPlayerHealth>();
            if (netPlayer != null)
            {
                _hasHit = true;
                netPlayer.TakeDamage(damage);
                DespawnOrDestroy();
            }
        }
    }

    private ulong GetOwnerClientId()
    {
        if (ownerNetworkObjectId == 0) return ulong.MaxValue;
        if (NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                .TryGetValue(ownerNetworkObjectId, out var netOwner) == true)
            return netOwner.OwnerClientId;
        return ulong.MaxValue;
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
