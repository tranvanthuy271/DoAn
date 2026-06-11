using UnityEngine;
using Unity.Netcode;

// HYBRID_WATER_WOOD_VENOM — "Băng Độc Vĩnh Cửu"
// Bắn một viên đạn nước độc theo hướng player.
// Kẻ địch trúng đạn bị: Damage + Slow + chặn hồi HP.
// SETUP TRONG UNITY — thực hiện trên F_Thuy.prefab VÀ F_Moc.prefab
// 1. Chọn root GameObject → Add Component → HybridWaterWoodVenomSkill
// 2. Gán bulletPrefab  = Skill4_Water_Moc.prefab (NetworkObject +
// Rigidbody2D + BoxCollider2D trigger +
// VenomBulletDamage)
// 3. skillCode         = "HYBRID_WATER_WOOD_VENOM"
// 4. cooldown          = 16
// 5. mpCost            = 50
// 6. effectValue       = 250  (sát thương khi trúng)
// 7. bulletSpeed       = 12
// 8. bulletLifetime    = 3
// 9. spawnOffsetX      = 0.6
// 10. slowDuration      = 3    (thời gian slow khi trúng)
// 11. healBlockDuration = 3    (thời gian chặn hồi HP khi trúng)
// 12. Trong PlayerSkillManager trên cùng prefab:
// → Thêm vào danh sách skills:
// skillType = HybridVenom
// activationKey = U (117)
// animationTriggerName = "HybridSkill"
public class HybridWaterWoodVenomSkill : HybridSkillBase
{
    [Header("Venom – Bullet")]
    [Tooltip("Prefab viên đạn. CẦN: NetworkObject, Rigidbody2D (Gravity=0), "
           + "BoxCollider2D (Is Trigger=true), VenomBulletDamage")]
    [SerializeField] private GameObject bulletPrefab;

    [Header("Venom – Projectile")]
    [Tooltip("Tốc độ bay của đạn (units/giây)")]
    [SerializeField] private float bulletSpeed = 12f;

    [Tooltip("Thời gian sống tối đa của đạn (giây)")]
    [SerializeField] private float bulletLifetime = 3f;

    [Tooltip("Khoảng cách spawn đạn theo trục X tính từ vị trí player")]
    [SerializeField] private float spawnOffsetX = 0.6f;

    [Header("Venom – Debuff")]
    [Tooltip("Thời gian làm chậm kẻ địch khi trúng đạn (giây)")]
    [SerializeField] public float slowDuration = 3f;

    [Tooltip("Thời gian chặn hồi HP kẻ địch khi trúng đạn (giây)")]
    [SerializeField] public float healBlockDuration = 3f;

    //  ExecuteSkill — chạy trên Server (gọi từ HybridSkillBase.UseSkillServerRpc)

    protected override void ExecuteSkill(Vector2 direction)
    {
        if (bulletPrefab == null)
        {
            { /* Cảnh báo: Thực hiện ghi log */ }
            return;
        }

        float dirX = direction.x >= 0f ? 1f : -1f;
        Vector3 origin = transform.position + new Vector3(dirX * spawnOffsetX, 0f, 0f);

        GameObject bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);

        // Flip sprite TRƯỚC KHI Spawn() để client nhận đúng rotation ngay từ spawn packet
        bullet.transform.rotation = Quaternion.Euler(0f, dirX < 0f ? 180f : 0f, 0f);

        var netObj = bullet.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();

        // Gán vận tốc trên server (physics server-side)
        var rb = bullet.GetComponent<Rigidbody2D>();
        Vector2 bulletVelocity = new Vector2(dirX, 0f) * bulletSpeed;
        if (rb != null)
            rb.velocity = bulletVelocity;

        // Gán thông số cho damage component
        var dmg = bullet.GetComponent<VenomBulletDamage>();
        if (dmg != null)
        {
            dmg.damage = (int)effectValue;
            dmg.lifetime = bulletLifetime;
            dmg.slowDuration = slowDuration;
            dmg.healBlockDuration = healBlockDuration;
            dmg.ownerNetworkObjectId = NetworkObjectId;
            // Đồng bộ velocity sang tất cả client (server physics không tự sync nếu không có NetworkTransform)
            dmg.SetVelocityClientRpc(bulletVelocity);
        }
        else
        {
            Destroy(bullet, bulletLifetime);
        }
    }
}
