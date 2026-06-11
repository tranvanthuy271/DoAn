using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

// Component gắn vào DoT projectile prefab (dùng cho EarthBlinkStrikeSkill).
// Khi chạm vào enemy hoặc player, áp dụng DoT (Damage Over Time):
// - Mỗi tickInterval giây gây dotDamagePerTick sát thương.
// - Tổng cộng dotTicks lần.
// - Sau đó tự hủy projectile.
// Là NetworkBehaviour để đồng bộ hit animation (ProjectileAnimController) sang client.
// Yêu cầu: NetworkObject trên cùng GameObject.
[RequireComponent(typeof(Collider2D))]
public class DotDamage : NetworkBehaviour
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
    private ulong ownerNetworkObjectId = 0;

    // Set owner để tránh tự gây damage cho chính mình.
    public void SetOwner(ulong networkObjectId) => ownerNetworkObjectId = networkObjectId;

    // Debuff Config
    private SkillEffectConfig _debuffConfig;

    // Gán SkillEffectConfig để áp dụng debuff khi projectile trúng target.
    // Gọi từ skill script ngay sau khi Instantiate projectile.
    public void SetDebuffConfig(SkillEffectConfig cfg) => _debuffConfig = cfg;

    private void Awake()
    {
        animCtrl = GetComponent<ProjectileAnimController>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Force-start animation on ALL instances (host + client)
        // so the fly-loop sprite is visible immediately after spawn.
        var animator = GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // On non-server clients, make Rigidbody2D kinematic so local physics
        // does not fight with NetworkTransform position sync.
        if (!IsServer)
        {
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        }
    }

    private void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chỉ server xử lý damage để tránh gọi RPC nhiều lần từ mỗi client
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer) return;

        if (hasHit) return;

        // Component-based detection (khong phu thuoc tag)
        EnemyHealth eh = collision.GetComponentInParent<EnemyHealth>();
        NetworkEnemyHealth neh = collision.GetComponentInParent<NetworkEnemyHealth>();

        if (eh != null || neh != null)
        {
            hasHit = true;
            MarkHitClientRpc();
            ApplyDebuffToTarget((neh != null ? (UnityEngine.Component)neh : eh).gameObject);
            StartCoroutine(ApplyDotEnemy(eh, neh));
            if (destroyOnHit)
                StartCoroutine(DespawnAfterDelay(dotTicks * tickInterval + 0.2f));
        }
        else if (collision.CompareTag("Player"))
        {
            // Bỏ qua nếu là chính người dùng skill
            NetworkObject targetNetObj = collision.GetComponent<NetworkObject>();
            if (targetNetObj != null && ownerNetworkObjectId != 0 && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
                return;

            NetworkPlayerHealth nph = collision.GetComponentInParent<NetworkPlayerHealth>();
            PlayerHealth ph = collision.GetComponentInParent<PlayerHealth>();

            if (nph != null || ph != null)
            {
                hasHit = true;
                // Đồng bộ hit animation sang tất cả client — kích hoạt animation event trên client
                MarkHitClientRpc();
                ApplyDebuffToTarget((nph != null ? (UnityEngine.Component)nph : ph).gameObject);
                StartCoroutine(ApplyDotPlayer(nph, ph));
                if (destroyOnHit)
                    StartCoroutine(DespawnAfterDelay(dotTicks * tickInterval + 0.2f));
            }
        }
    }

    // Đồng bộ trạng thái "đã trúng" sang tất cả client để ProjectileAnimController
    // chuyển từ fly-loop sang explosion animation đúng lúc.
    [ClientRpc]
    private void MarkHitClientRpc()
    {
        hasHit = true;
        animCtrl?.MarkHit();
    }

    private System.Collections.IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new UnityEngine.WaitForSeconds(delay);
        DespawnOrDestroy();
    }

    private void DespawnOrDestroy()
    {
        var netObj = GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
            netObj.Despawn(true);
        else
            Destroy(gameObject);
    }

    private ulong GetOwnerClientId()
    {
        if (ownerNetworkObjectId == 0) return ulong.MaxValue;
        if (NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                .TryGetValue(ownerNetworkObjectId, out var netOwner) == true)
            return netOwner.OwnerClientId;
        return ulong.MaxValue;
    }

    private System.Collections.IEnumerator ApplyDotEnemy(EnemyHealth eh, NetworkEnemyHealth neh)
    {
        ulong ownerClientId = GetOwnerClientId();
        for (int i = 0; i < dotTicks; i++)
        {
            if (eh != null) eh.TakeDamage(dotDamagePerTick);
            else if (neh != null) neh.TakeDamage(dotDamagePerTick, ownerClientId);
            yield return new WaitForSeconds(tickInterval);
        }
    }

    private System.Collections.IEnumerator ApplyDotPlayer(NetworkPlayerHealth nph, PlayerHealth ph)
    {
        for (int i = 0; i < dotTicks; i++)
        {
            if (nph != null) nph.TakeDamage(dotDamagePerTick);
            else if (ph != null) ph.TakeDamage(dotDamagePerTick);
            yield return new WaitForSeconds(tickInterval);
        }
    }

    // Áp dụng debuff từ _debuffConfig lên target vừa bị hit. Chỉ chạy trên server.
    private void ApplyDebuffToTarget(UnityEngine.GameObject target)
    {
        if (_debuffConfig == null) return;
        if (_debuffConfig.debuffType == SkillDebuffType.None) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

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
}
