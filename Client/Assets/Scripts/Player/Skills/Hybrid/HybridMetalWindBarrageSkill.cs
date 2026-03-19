using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// HYBRID_METAL_WIND_BARRAGE — "Kim Phong Liên Tiễn"
/// Bắn 5 viên đạn nhỏ theo chiều ngang, lần lượt cách nhau một khoảng thời gian ngắn.
/// Mỗi viên đạn ở một vị trí Y khác nhau (cao hơn / thấp hơn một chút so với viên kề bên).
///
/// ═══════════════════════════════════════════════════════════════════════════╗
/// SETUP TRONG UNITY — thực hiện trên F_Phong.prefab VÀ F_Kim.prefab         ║
/// ───────────────────────────────────────────────────────────────────────── ║
///  1. Chọn root GameObject → Add Component → HybridMetalWindBarrageSkill    ║
///  2. Gán bulletPrefab  = MetalWindBullet.prefab (NetworkObject + Rigidbody2D ║
///                          + BoxCollider2D trigger + BarrageBulletDamage)    ║
///  3. skillCode         = "HYBRID_METAL_WIND_BARRAGE"                       ║
///  4. cooldown          = 10                                                 ║
///  5. mpCost            = 40                                                 ║
///  6. effectValue       = 120   (damage mỗi viên đạn)                       ║
///  7. bulletCount       = 5                                                  ║
///  8. bulletSpeed       = 18                                                 ║
///  9. bulletLifetime    = 2.5                                                ║
/// 10. ySpacing          = 0.25  (khoảng cách Y giữa các viên)               ║
/// 11. fireDelay         = 0.08  (giây giữa mỗi viên bắn)                   ║
/// 12. spawnOffsetX      = 0.6   (khoảng cách từ player khi spawn)           ║
/// ═══════════════════════════════════════════════════════════════════════════╝
/// </summary>
public class HybridMetalWindBarrageSkill : HybridSkillBase
{
    [Header("Barrage – Bullet")]
    [Tooltip("Prefab viên đạn. CẦN: NetworkObject, Rigidbody2D (Gravity=0), "
           + "BoxCollider2D (Is Trigger=true), BarrageBulletDamage")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Barrage – Pattern")]
    [Tooltip("Số viên đạn bắn ra mỗi lần kích hoạt skill")]
    [SerializeField] private int bulletCount = 5;

    [Tooltip("Khoảng cách Y giữa hai viên đạn liền kề (units). "
           + "Ví dụ: 0.25 → 5 viên ở Y = -0.50, -0.25, 0, +0.25, +0.50")]
    [SerializeField] private float ySpacing = 0.25f;

    [Tooltip("Thời gian chờ (giây) giữa mỗi lần bắn một viên đạn")]
    [SerializeField] private float fireDelay = 0.08f;

    [Header("Barrage – Projectile")]
    [Tooltip("Tốc độ bay của đạn (units/giây)")]
    [SerializeField] private float bulletSpeed = 18f;

    [Tooltip("Thời gian sống tối đa của đạn (giây)")]
    [SerializeField] private float bulletLifetime = 2.5f;

    [Tooltip("Khoảng cách spawn đạn theo trục X tính từ vị trí player (theo chiều nhìn)")]
    [SerializeField] private float spawnOffsetX = 0.6f;

    // ─────────────────────────────────────────────────────────────────────────
    //  ExecuteSkill — chạy trên Server (gọi từ HybridSkillBase.UseSkillServerRpc)
    // ─────────────────────────────────────────────────────────────────────────

    protected override void ExecuteSkill(Vector2 direction)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning($"[{nameof(HybridMetalWindBarrageSkill)}] "
                           + "bulletPrefab chưa được gán trong Inspector!");
            return;
        }

        StartCoroutine(FireSequence(direction));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Coroutine: bắn lần lượt từng viên đạn
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator FireSequence(Vector2 direction)
    {
        // Xác định hướng ngang (±1). Nếu direction.x = 0 thì mặc định bắn sang phải
        float dirX = direction.x >= 0f ? 1f : -1f;

        // Tính vị trí Y bắt đầu để các viên đạn được căn giữa theo Y player
        float totalSpan = ySpacing * (bulletCount - 1);
        float startY    = -totalSpan * 0.5f;

        for (int i = 0; i < bulletCount; i++)
        {
            float yOffset  = startY + ySpacing * i;
            Vector3 origin = transform.position
                           + new Vector3(dirX * spawnOffsetX, yOffset, 0f);

            // Spawn đạn và đăng ký vào network
            GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);

            var netObj = bullet.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Spawn();

            // Gán vận tốc — đạn bay ngang hoàn toàn, không có thành phần Y
            var rb = bullet.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.velocity = new Vector2(dirX, 0f) * bulletSpeed;

            // Xoay sprite theo hướng bay
            //   dirX > 0 → xoay 0°  (sprite mặc định nhìn sang phải)
            //   dirX < 0 → xoay 180° theo trục Y để lật ngang
            bullet.transform.rotation = Quaternion.Euler(0f, dirX < 0f ? 180f : 0f, 0f);

            // Gán thông số damage
            var dmg = bullet.GetComponent<BarrageBulletDamage>();
            if (dmg != null)
            {
                dmg.damage   = (int)effectValue;
                dmg.lifetime = bulletLifetime;
            }
            else
            {
                // Fallback: tự destroy nếu không có damage component
                Destroy(bullet, bulletLifetime);
            }

            // Đợi trước khi bắn viên tiếp theo (bỏ qua delay sau viên cuối)
            if (i < bulletCount - 1)
                yield return new WaitForSeconds(fireDelay);
        }
    }
}
