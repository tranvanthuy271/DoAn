using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Component gắn vào boomerang projectile prefab.
///
/// Hành vi:
///   1. Bay thẳng theo hướng được set khi Spawn.
///   2. Sau returnDelay giây, đổi hướng quay về owner.
///   3. Khi chạm owner hoặc hết lifetime thì tự hủy.
///
/// Damage được xử lý bởi FireballDamage component trên cùng prefab.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EarthBoomerangProjectile : MonoBehaviour
{
    [Tooltip("Thời gian bay thẳng trước khi quay về (giây)")]
    [SerializeField] private float returnDelay = 0.6f;

    [Tooltip("Tốc độ khi quay về owner (units/giây)")]
    [SerializeField] private float returnSpeed = 14f;

    [Tooltip("Tổng thời gian sống tối đa (giây)")]
    [SerializeField] private float maxLifetime = 4f;

    // ── Set bởi EarthBoomerangSkill khi spawn ────────────────────────────────
    private Transform owner;
    private Vector2 initialVelocity;
    private Rigidbody2D rb;
    private bool returning = false;
    private float timer = 0f;

    public void Initialize(Transform ownerTransform, Vector2 velocity)
    {
        owner = ownerTransform;
        initialVelocity = velocity;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.velocity = initialVelocity;
        Destroy(gameObject, maxLifetime);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (!returning && timer >= returnDelay)
        {
            returning = true;
        }

        if (returning && owner != null)
        {
            Vector2 dir = ((Vector2)owner.position - (Vector2)transform.position).normalized;
            rb.velocity = dir * returnSpeed;

            // Hủy khi đã quay về gần owner
            if (Vector2.Distance(transform.position, owner.position) < 0.5f)
            {
                Destroy(gameObject);
            }
        }
    }
}
