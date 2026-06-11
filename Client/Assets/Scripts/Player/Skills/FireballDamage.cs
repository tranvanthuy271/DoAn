using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

// Script xử lý damage enemy khi fireball va chạm.
// Hỗ trợ cả PvP: gây damage cho player khác khi trúng skill.
[RequireComponent(typeof(Collider2D))]
public class FireballDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [Tooltip("Sát thương của fireball")]
    [SerializeField] private int damage = 5;

    // Attack bonus từ owner (EarthAura buff)
    private int attackBonusPercent = 0;

    [Header("Collision Settings")]
    [Tooltip("Có tự hủy sau khi va chạm với enemy không")]
    [SerializeField] private bool destroyOnHit = true;

    [Tooltip("Có tự hủy khi va chạm với ground/wall không")]
    [SerializeField] private bool destroyOnGround = false;

    private bool hasHit = false;

    // NetworkObjectId của player sử dụng skill (Ä'ể tránh tự bán)
    private ulong ownerNetworkObjectId = 0;

    // Set owner NetworkObjectId để projectile không tự gây damage cho chính người bắn.
    public void SetOwner(ulong networkObjectId) => ownerNetworkObjectId = networkObjectId;

    // Debuff Config
    private SkillEffectConfig _debuffConfig;

    // Gán SkillEffectConfig để áp dụng debuff khi projectile trúng target.
    // Gọi từ skill script ngay sau khi Instantiate projectile.
    public void SetDebuffConfig(SkillEffectConfig cfg) => _debuffConfig = cfg;

    // Lấy MapId của player sở hữu projectile này qua ZoneRoomRegistry.
    // Trả về -999 nếu không tra được (registry chưa sẵn sàng).
    private int GetOwnerMapId()
    {
        if (ownerNetworkObjectId == 0) return -999;
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null) return -999;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(ownerNetworkObjectId, out var ownerNetObj))
            return -999;
        var registry = ZoneRoomRegistry.Instance;
        if (registry == null) return -999;
        var room = registry.GetClientRoom(ownerNetObj.OwnerClientId);
        return room?.MapId ?? -999;
    }

    private ulong GetOwnerClientId()
    {
        if (ownerNetworkObjectId == 0) return ulong.MaxValue;
        var nm = Unity.Netcode.NetworkManager.Singleton;
        if (nm == null) return ulong.MaxValue;
        if (!nm.SpawnManager.SpawnedObjects.TryGetValue(ownerNetworkObjectId, out var ownerNetObj))
            return ulong.MaxValue;
        return ownerNetObj.OwnerClientId;
    }
    private void Start()
    {
        // Äảm bảo collider là trigger
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
        // Chỉ server mới xử lý damage — tránh double-damage khi physics chạy trên cả client
        if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer)
            return;

        // Chỉ xử lý một lần (tránh damage nhiều lần)
        if (hasHit) return;

        // Sát thương gốc có AttackBuff (cho PvP và EnemyHealth fallback)
        int baseFinalDamage = damage + damage * attackBonusPercent / 100;

        // Check enemy: component-based detection (khong phu thuoc tag)
        NetworkEnemyHealth networkEnemyHealth = collision.GetComponentInParent<NetworkEnemyHealth>();
        EnemyHealth enemyHealth = collision.GetComponentInParent<EnemyHealth>();

        if (networkEnemyHealth != null)
        {
            // Kiểm tra cùng map — không được damage enemy ở map khác
            var enemyZoneTag = networkEnemyHealth.GetComponent<ZoneOwnerTag>();
            int ownerMap = GetOwnerMapId();
            if (enemyZoneTag != null && ownerMap != -999 && ownerMap != enemyZoneTag.MapId)
            {
                Debug.LogWarning($"[FireballDamage] Bỏ qua cross-map: owner map={ownerMap}, enemy map={enemyZoneTag.MapId}");
                return;
            }

            // Tính sát thương qua DamageCalculator (AttackBuff + Hybrid Gene bonus)
            Unity.Netcode.NetworkObject ownerNetObj = null;
            Unity.Netcode.NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                .TryGetValue(ownerNetworkObjectId, out ownerNetObj);
            var attackerPd = ownerNetObj != null
                ? ServerPlayerDataManager.Instance?.GetPlayerDataByClientId(ownerNetObj.OwnerClientId)
                : null;
            float atkBonusPct = attackBonusPercent / 100f;
            int finalDamage = DamageCalculator.CalcPlayerAttackDamage(
                damage, atkBonusPct, attackerPd, networkEnemyHealth.ElementType);

            networkEnemyHealth.TakeDamage(finalDamage, GetOwnerClientId());
            ApplyDebuffToTarget(networkEnemyHealth.gameObject);
            hasHit = true;
            Debug.Log($"[FireballDamage] Fireball damage enemy {collision.name} voi {finalDamage} damage! (Network)");
            if (destroyOnHit) Destroy(gameObject);
        }
        else if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(baseFinalDamage);
            hasHit = true;
            Debug.Log($"[FireballDamage] Fireball damage enemy {collision.name} voi {baseFinalDamage} damage!");
            if (destroyOnHit) Destroy(gameObject);
        }        // Check va chạm với Player (PvP)
        else if (collision.CompareTag("Player"))
        {
            // Bá» qua néu va chạm với chính ngưá»i sử dụng skill
            NetworkObject targetNetObj = collision.GetComponent<NetworkObject>();
            if (targetNetObj != null && ownerNetworkObjectId != 0 && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
                return;

            // Network mode: dùng NetworkPlayerHealth
            NetworkPlayerHealth networkPlayerHealth = collision.GetComponentInParent<NetworkPlayerHealth>();
            if (networkPlayerHealth != null)
            {
                networkPlayerHealth.TakeDamage(baseFinalDamage);
                ApplyDebuffToTarget(networkPlayerHealth.gameObject);
                hasHit = true;
                Debug.Log($"[FireballDamage] Hit player {collision.name} với {baseFinalDamage} damage! (Network PvP)");
                if (destroyOnHit) Destroy(gameObject);
                return;
            }

            // Standalone mode: dùng PlayerHealth
            PlayerHealth playerHealth = collision.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(baseFinalDamage);
                hasHit = true;
                Debug.Log($"[FireballDamage] Hit player {collision.name} với {baseFinalDamage} damage! (Standalone PvP)");
                if (destroyOnHit) Destroy(gameObject);
            }
        }        // Nếu va chạm với ground/wall, hủy fireball
        else if (destroyOnGround && (collision.CompareTag("Ground") || collision.CompareTag("Wall")))
        {
            Debug.Log("[FireballDamage] Fireball đã chạm ground/wall, tự hủy.");
            Destroy(gameObject);
        }
    }

    // Áp dụng debuff từ _debuffConfig lên target vừa bị hit. Chỉ chạy trên server.
    private void ApplyDebuffToTarget(GameObject target)
    {
        if (_debuffConfig == null) return;
        if (_debuffConfig.debuffType == SkillDebuffType.None) return;
        if (!Unity.Netcode.NetworkManager.Singleton.IsServer) return;

        var debuffMgr = target.GetComponent<DebuffManager>()
                     ?? target.GetComponentInParent<DebuffManager>();
        if (debuffMgr == null) return;

        debuffMgr.ApplyDebuffServerRpc(
            _debuffConfig.debuffType,
            _debuffConfig.debuffValue,
            _debuffConfig.debuffDuration,
            _debuffConfig.iconId,
            new Unity.Collections.FixedString64Bytes(_debuffConfig.debuffName)
        );
    }

    // Set attack bonus % from owner's EarthAura buff.
    public void SetAttackBonus(int bonusPercent) => attackBonusPercent = bonusPercent;

    public void SetDamage(int newDamage)
    {
        damage = newDamage;
    }

    // Get sát thương hiện tại
    public int GetDamage() => damage;
}
