using UnityEngine;

/// <summary>
/// Script xá»­ lÃ½ damage enemy khi fireball va cháº¡m
/// Tá»± Ä‘á»™ng damage enemy vÃ  xÃ³a fireball khi cháº¡m
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class FireballDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("SÃ¡t thÆ°Æ¡ng cá»§a fireball")]
    [SerializeField] private int damage = 5;

    // Attack bonus tá»« owner (EarthAura buff)
    private int attackBonusPercent = 0;

    [Header("Collision Settings")]
    [Tooltip("CÃ³ tá»± há»§y sau khi va cháº¡m vá»›i enemy khÃ´ng")]
    [SerializeField] private bool destroyOnHit = true;

    [Tooltip("CÃ³ tá»± há»§y khi va cháº¡m vá»›i ground/wall khÃ´ng")]
    [SerializeField] private bool destroyOnGround = true;

    private bool hasHit = false;

    private void Start()
    {
        // Äáº£m báº£o collider lÃ  trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
            Debug.LogWarning("[FireballDamage] Collider Ä‘Ã£ Ä‘Æ°á»£c tá»± Ä‘á»™ng set thÃ nh trigger!");
        }

        // Kiá»ƒm tra náº¿u khÃ´ng cÃ³ Collider2D
        if (col == null)
        {
            Debug.LogError("[FireballDamage] Fireball khÃ´ng cÃ³ Collider2D! Vui lÃ²ng thÃªm Collider2D vÃ o Prefab.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chá»‰ xá»­ lÃ½ má»™t láº§n (trÃ¡nh damage nhiá»u láº§n)
        if (hasHit) return;

        int finalDamage = damage + damage * attackBonusPercent / 100;

        // Check náº¿u va cháº¡m vá»›i enemy
        if (collision.CompareTag("Enemy"))
        {
            // TÃ¬m component EnemyHealth hoáº·c NetworkEnemyHealth
            EnemyHealth enemyHealth = collision.GetComponent<EnemyHealth>();
            NetworkEnemyHealth networkEnemyHealth = collision.GetComponent<NetworkEnemyHealth>();

            if (enemyHealth != null)
            {
                // Standalone mode: dÃ¹ng EnemyHealth
                enemyHealth.TakeDamage(finalDamage);
                hasHit = true;
                Debug.Log($"[FireballDamage] Fireball Ä‘Ã£ damage enemy {collision.name} vá»›i {damage} damage!");

                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
            }
            else if (networkEnemyHealth != null)
            {
                // Network mode: dÃ¹ng NetworkEnemyHealth
                networkEnemyHealth.TakeDamage(finalDamage);
                hasHit = true;
                Debug.Log($"[FireballDamage] Fireball Ä‘Ã£ damage enemy {collision.name} vá»›i {damage} damage! (Network)");

                if (destroyOnHit)
                {
                    Destroy(gameObject);
                }
            }
            else
            {
                Debug.LogWarning($"[FireballDamage] Enemy {collision.name} khÃ´ng cÃ³ EnemyHealth hoáº·c NetworkEnemyHealth component!");
            }
        }
        // Náº¿u va cháº¡m vá»›i ground/wall, há»§y fireball
        else if (destroyOnGround && (collision.CompareTag("Ground") || collision.CompareTag("Wall")))
        {
            Debug.Log("[FireballDamage] Fireball Ä‘Ã£ cháº¡m ground/wall, tá»± há»§y.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Set sÃ¡t thÆ°Æ¡ng cá»§a fireball (cÃ³ thá»ƒ gá»i tá»« script khÃ¡c)
    /// </summary>
    /// <summary>Set attack bonus % from owner's EarthAura buff.</summary>
    public void SetAttackBonus(int bonusPercent) => attackBonusPercent = bonusPercent;

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    /// <summary>
    /// Get sÃ¡t thÆ°Æ¡ng hiá»‡n táº¡i
    /// </summary>
    public int GetDamage() => damage;
}
