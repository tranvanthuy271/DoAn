using UnityEngine;

/// <summary>
/// Component gắn vào DoT projectile prefab (dùng cho EarthBlinkStrikeSkill).
///
/// Khi chạm vào enemy hoặc player, áp dụng DoT (Damage Over Time):
///   - Mỗi tickInterval giây gây dotDamagePerTick sát thương.
///   - Tổng cộng dotTicks lần.
///   - Sau đó tự hủy projectile.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DotDamage : MonoBehaviour
{
    [Header("DoT Settings")]
    [Tooltip("Sát thương mỗi tick")]
    [SerializeField] private int dotDamagePerTick = 3;

    [Tooltip("Số tick sát thương")]
    [SerializeField] private int dotTicks = 5;

    [Tooltip("Khoảng cách giữa các tick (giây)")]
    [SerializeField] private float tickInterval = 0.8f;

    [Tooltip("Tự hủy sau khi áp dụng DoT không?")]
    [SerializeField] private bool destroyOnHit = true;

    private bool hasHit = false;
    private ProjectileAnimController animCtrl;

    private void Awake()
    {
        animCtrl = GetComponent<ProjectileAnimController>();
    }

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasHit) return;

        if (collision.CompareTag("Enemy"))
        {
            EnemyHealth eh = collision.GetComponent<EnemyHealth>();
            NetworkEnemyHealth neh = collision.GetComponent<NetworkEnemyHealth>();

            if (eh != null || neh != null)
            {
                hasHit = true;
                animCtrl?.MarkHit();
                StartCoroutine(ApplyDotEnemy(eh, neh));
                if (destroyOnHit)
                    Destroy(gameObject, dotTicks * tickInterval + 0.2f);
            }
        }
        else if (collision.CompareTag("Player"))
        {
            PlayerHealth ph = collision.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                hasHit = true;
                animCtrl?.MarkHit();
                StartCoroutine(ApplyDotPlayer(ph));
                if (destroyOnHit)
                    Destroy(gameObject, dotTicks * tickInterval + 0.2f);
            }
        }
    }

    private System.Collections.IEnumerator ApplyDotEnemy(EnemyHealth eh, NetworkEnemyHealth neh)
    {
        for (int i = 0; i < dotTicks; i++)
        {
            if (eh != null) eh.TakeDamage(dotDamagePerTick);
            else if (neh != null) neh.TakeDamage(dotDamagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }
    }

    private System.Collections.IEnumerator ApplyDotPlayer(PlayerHealth ph)
    {
        for (int i = 0; i < dotTicks; i++)
        {
            if (ph != null) ph.TakeDamage(dotDamagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }
    }
}
