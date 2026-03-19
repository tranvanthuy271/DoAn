using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// HYBRID_METAL_WIND_GALE — "Kim Phong Thoán Thế"
/// Phóng 12 mũi tên gió kim loại theo hình nan quạt.
/// Mỗi mũi tên xuyên qua tối đa 3 kẻ địch.
///
/// ═══════════════════════════════════════════════════════════
/// SETUP TRONG UNITY (Hybrid_Metal_Wind.prefab):
///   1. Add component HybridMetalWindGaleSkill
///   2. Gán arrowPrefab: prefab mũi tên (NetworkObject + Rigidbody2D + Collider2D trigger)
///   3. skillCode  = "HYBRID_METAL_WIND_GALE"
///   4. cooldown   = 13
///   5. mpCost     = 55
///   6. effectValue = 295
/// ═══════════════════════════════════════════════════════════
/// </summary>
public class HybridMetalWindGaleSkill : HybridSkillBase
{
    [Header("Gale Settings")]
    [Tooltip("Prefab mũi tên. Cần: NetworkObject, Rigidbody2D, Collider2D trigger, GaleBoltDamage")]
    [SerializeField] private GameObject arrowPrefab;

    [Tooltip("Số mũi tên bắn ra")]
    [SerializeField] private int arrowCount = 12;

    [Tooltip("Góc tổng của nan quạt (độ). 180 = nửa vòng tròn phía trước")]
    [SerializeField] private float spreadAngleDeg = 180f;

    [Tooltip("Tốc độ mũi tên (units/giây)")]
    [SerializeField] private float arrowSpeed = 22f;

    [Tooltip("Thời gian sống của mỗi mũi tên (giây)")]
    [SerializeField] private float arrowLifetime = 1.8f;

    [Tooltip("Tối đa bao nhiêu kẻ địch mỗi mũi tên có thể xuyên qua")]
    [SerializeField] private int pierceCount = 3;

    protected override void ExecuteSkill(Vector2 direction)
    {
        if (arrowPrefab == null)
        {
            Debug.LogWarning($"[{nameof(HybridMetalWindGaleSkill)}] arrowPrefab chưa được gán!");
            return;
        }

        float baseAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        float step      = arrowCount > 1 ? spreadAngleDeg / (arrowCount - 1) : 0f;
        float startAngle = baseAngle - spreadAngleDeg * 0.5f;

        Vector3 origin = transform.position;

        for (int i = 0; i < arrowCount; i++)
        {
            float   angleDeg = startAngle + step * i;
            float   rad      = angleDeg * Mathf.Deg2Rad;
            Vector2 dir      = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            var go = Instantiate(arrowPrefab, origin, Quaternion.identity);
            go.GetComponent<NetworkObject>()?.Spawn();

            var rb = go.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = dir * arrowSpeed;

            // Rotate sprite theo hướng bay
            float spriteAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, spriteAngle);

            // Cấu hình damage component
            var dmg = go.GetComponent<GaleBoltDamage>();
            if (dmg != null)
            {
                dmg.damage      = (int)effectValue;
                dmg.pierceCount = pierceCount;
                dmg.lifetime    = arrowLifetime;
            }
            else
            {
                Destroy(go, arrowLifetime);
            }
        }
    }
}
