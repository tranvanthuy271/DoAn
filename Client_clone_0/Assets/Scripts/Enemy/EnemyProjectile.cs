using UnityEngine;
using Unity.Netcode;

// Script xử lý projectile của enemy
// Tự động damage player khi va chạm
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Sát thương của projectile")]
    public int damage = 1;

    [Header("Collision Settings")]
    [Tooltip("Layer của player (để check collision)")]
    public LayerMask playerLayer = 1 << 6; // Layer 6 = Player (mặc định)

    [Tooltip("Có tự hủy sau khi va chạm với player không")]
    public bool destroyOnHit = true;

    [Tooltip("Có tự hủy khi chạm ground/wall không (mặc định false để projectile bay qua sàn)")]
    public bool destroyOnGround = false;

    [Header("Lifetime")]
    [Tooltip("Thời gian tối đa projectile tồn tại trước khi tự hủy. Đặt 0 để không tự hủy")]
    public float lifetime = 3f;

    // MapId của enemy sinh ra projectile này.
    // Được set bởi EnemyAI.PrepareProjectileInstance() để chặn cross-map damage.
    // -999 = không biết (bỏ qua kiểm tra map).
    [HideInInspector]
    public int EnemyMapId = -999;

    private bool hasHit = false;

    private void Start()
    {
        // Đảm bảo collider là trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }

        if (ShouldRunServerLogic() && lifetime > 0f)
        {
            Invoke(nameof(DestroyProjectile), lifetime);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!ShouldRunServerLogic()) return;

        // Chỉ xử lý một lần
        if (hasHit) return;

        // Check nếu va chạm với player
        if (collision.CompareTag("Player"))
        {
            NetworkPlayerHealth netPlayerHealth = collision.GetComponentInParent<NetworkPlayerHealth>();
            if (netPlayerHealth != null)
            {
                // Kiểm tra cùng map — không được damage player ở map khác
                if (EnemyMapId != -999)
                {
                    var registry = ZoneRoomRegistry.Instance;
                    if (registry != null)
                    {
                        var netObj = netPlayerHealth.GetComponent<Unity.Netcode.NetworkObject>();
                        if (netObj != null)
                        {
                            var room = registry.GetClientRoom(netObj.OwnerClientId);
                            if (room != null && room.MapId != EnemyMapId)
                            {
                                Debug.LogWarning($"[EnemyProjectile] Bỏ qua cross-map: enemy map={EnemyMapId}, player map={room.MapId}");
                                return;
                            }
                        }
                    }
                }
                netPlayerHealth.TakeDamage(damage);
                hasHit = true;
                if (destroyOnHit) DestroyProjectile();
                return;
            }
            PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                hasHit = true;
                if (destroyOnHit) DestroyProjectile();
            }
        }
        // Nếu va chạm với ground/wall, chỉ hủy nếu destroyOnGround = true
        else if (collision.CompareTag("Ground") || collision.CompareTag("Wall"))
        {
            if (destroyOnGround)
            {
                DestroyProjectile();
            }
        }
    }

    private bool ShouldRunServerLogic()
    {
        return NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
    }

    private void DestroyProjectile()
    {
        if (!gameObject) return;

        NetworkObject networkObject = GetComponent<NetworkObject>();
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(true);
            return;
        }

        Destroy(gameObject);
    }
}
