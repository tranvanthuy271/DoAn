using UnityEngine;

/// <summary>
/// Script xử lý projectile của enemy
/// Tự động damage player khi va chạm
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Sát thương của projectile")]
    public int damage = 1;

    [Header("Collision Settings")]
    [Tooltip("Layer của player (để check collision)")]
    public LayerMask playerLayer = 1 << 6; // Layer 6 = Player (mặc định)

    [Tooltip("Có tự hủy sau khi va chạm không")]
    public bool destroyOnHit = true;

    private bool hasHit = false;

    private void Start()
    {
        // Đảm bảo collider là trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ xử lý một lần
        if (hasHit) return;

        // Check nếu va chạm với player
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                hasHit = true;

                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
            }
        }
        // Nếu va chạm với ground/wall, hủy projectile
        else if (collision.CompareTag("Ground") || collision.CompareTag("Wall"))
        {
            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}
