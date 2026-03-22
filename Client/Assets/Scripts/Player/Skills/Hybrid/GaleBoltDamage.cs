using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Damage component gắn vào đầu đạn mũi tên của HYBRID_METAL_WIND_GALE.
/// Xuyên qua tối đa <pierceCount> kẻ địch, sau đó tự hủy.
/// </summary>
public class GaleBoltDamage : MonoBehaviour
{
    [HideInInspector] public int   damage      = 295;
    [HideInInspector] public int   pierceCount = 3;
    [HideInInspector] public float lifetime    = 1.8f;
    [HideInInspector] public ulong ownerNetworkObjectId = 0;

    private int _pierced;

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
        // Chỉ server xử lý damage
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
            return;

        if (_pierced >= pierceCount)
        {
            DespawnOrDestroy();
            return;
        }

        // Bỏ qua nếu va chạm với chính người dùng skill
        var targetNetObj = other.GetComponent<NetworkObject>();
        if (targetNetObj != null && ownerNetworkObjectId != 0
            && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
            return;

        // Multiplayer enemy
        var netHealth = other.GetComponent<NetworkEnemyHealth>();
        if (netHealth != null)
        {
            netHealth.TakeDamage(damage);
            _pierced++;
            if (_pierced >= pierceCount) DespawnOrDestroy();
            return;
        }

        // Fallback: EnemyHealth cũ
        var localHealth = other.GetComponent<EnemyHealth>();
        if (localHealth != null)
        {
            localHealth.TakeDamage(damage);
            _pierced++;
            if (_pierced >= pierceCount) DespawnOrDestroy();
            return;
        }

        // PvP: gây damage cho player khác
        if (other.CompareTag("Player"))
        {
            var netPlayer = other.GetComponent<NetworkPlayerHealth>();
            if (netPlayer != null)
            {
                netPlayer.TakeDamage(damage);
                _pierced++;
                if (_pierced >= pierceCount) DespawnOrDestroy();
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
}
