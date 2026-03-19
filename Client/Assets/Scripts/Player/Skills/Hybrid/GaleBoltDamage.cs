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

    private int _pierced;

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_pierced >= pierceCount)
        {
            Destroy(gameObject);
            return;
        }

        var health = other.GetComponent<EnemyHealth>();
        if (health == null) return;

        health.TakeDamage(damage);
        _pierced++;

        if (_pierced >= pierceCount)
            Destroy(gameObject);
    }
}
