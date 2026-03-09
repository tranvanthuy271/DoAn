using UnityEngine;

/// <summary>
/// Script xử lý damage enemy khi fireball va chạm
/// Tự động damage enemy và xóa fireball khi chạm
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FireballDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Sát thương của fireball")]
    [SerializeField] private int damage = 5;

    [Header("Collision Settings")]
    [Tooltip("Có tự hủy sau khi va chạm với enemy không")]
    [SerializeField] private bool destroyOnHit = true;

    [Tooltip("Có tự hủy khi va chạm với ground/wall không")]
    [SerializeField] private bool destroyOnGround = true;

    private bool hasHit = false;

    private void Start()
    {
        // Đảm bảo collider là trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("[FireballDamage] Collider đã được tự động set thành trigger!");
        }

        // Kiểm tra nếu không có Collider2D
        if (col == null)
        {
            Debug.LogError("[FireballDamage] Fireball không có Collider2D! Vui lòng thêm Collider2D vào Prefab.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ xử lý một lần (tránh damage nhiều lần)
        if (hasHit) return;

        // Check nếu va chạm với enemy
        if (collision.CompareTag("Enemy"))
        {
            // Tìm component EnemyHealth hoặc NetworkEnemyHealth
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            NetworkEnemyHealth networkEnemyHealth = collision.GetComponent<NetworkEnemyHealth>();

            if (enemyHealth != null)
            {
                // Standalone mode: dùng EnemyHealth
                enemyHealth.TakeDamage(damage);
                hasHit = true;
                Debug.Log($"[FireballDamage] Fireball đã damage enemy {collision.name} với {damage} damage!");

                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
            }
            else if (networkEnemyHealth != null)
            {
                // Network mode: dùng NetworkEnemyHealth
                networkEnemyHealth.TakeDamage(damage);
                hasHit = true;
                Debug.Log($"[FireballDamage] Fireball đã damage enemy {collision.name} với {damage} damage! (Network)");

                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"[FireballDamage] Enemy {collision.name} không có EnemyHealth hoặc NetworkEnemyHealth component!");
            }
        }
        // Nếu va chạm với ground/wall, hủy fireball
        else if (destroyOnGround && (collision.CompareTag("Ground") || collision.CompareTag("Wall")))
        {
            Debug.Log("[FireballDamage] Fireball đã chạm ground/wall, tự hủy.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set sát thương của fireball (có thể gọi từ script khác)
    /// </summary>
    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    /// <summary>
    /// Get sát thương hiện tại
    /// </summary>
    public int GetDamage() => damage;
}
