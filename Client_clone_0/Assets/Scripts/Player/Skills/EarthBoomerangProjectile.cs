using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Component gắn vào boomerang projectile prefab.
///
/// Hành vi:
///   1. Bay thẳng theo hướng được set khi Spawn.
///   2. Sau returnDelay giây, đổi hướng quay về owner.
///   3. Khi chạm owner hoặc hết lifetime thì Despawn.
///
/// Damage được xử lý bởi FireballDamage component trên cùng prefab.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EarthBoomerangProjectile : NetworkBehaviour
{
    [Tooltip("Thời gian bay thẳng trước khi quay về (giây)")]
    [SerializeField] private float returnDelay = 0.6f;

    [Tooltip("Tốc độ khi quay về owner (units/giây)")]
    [SerializeField] private float returnSpeed = 14f;

    [Tooltip("Tổng thời gian sống tối đa (giây)")]
    [SerializeField] private float maxLifetime = 4f;

    /// <summary>Sát thương gây ra khi chạm — set từ EarthBoomerangSkill (ghi đè FireballDamage).</summary>
    [HideInInspector] public int damage = 100;
    /// <summary>NetworkObjectId của caster để tránh tự gây damage cho mình.</summary>
    [HideInInspector] public ulong ownerNetworkObjectId = 0;

    // Cooldown per-target: tránh spam damage lên cùng 1 mục tiêu trong vòng 0.8s
    private const float PER_TARGET_HIT_COOLDOWN = 0.8f;
    private readonly Dictionary<ulong, float> _recentHits = new Dictionary<ulong, float>();

    // ── Runtime state ─────────────────────────────────────────────────────────
    private Rigidbody2D rb;
    private bool returning = false;
    private float timer = 0f;

    // Server: cached owner Transform; Client: resolved from NetworkObjectId each frame
    private Transform _ownerTransform;
    private ulong _ownerNetworkObjectId;

    // ── Server setup (gọi từ EarthBoomerangSkill TRƯỚC khi Spawn()) ───────────

    /// <summary>Server-side init: set velocity + cache owner Transform.</summary>
    public void InitializeOnServer(Transform ownerTransform, Vector2 velocity)
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.velocity = velocity;
        _ownerTransform = ownerTransform;
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Tắt FireballDamage NGAY trong Awake — trước khi bất kỳ physics tick nào chạy.
        // Phải làm ở đây (không phải Start/OnNetworkSpawn) vì:
        //   - Start() chạy trước Spawn() → IsServer luôn false → không bao giờ disable
        //   - OnNetworkSpawn() chạy sau Spawn() → có thể đã có physics overlap rồi
        var fbd = GetComponent<FireballDamage>();
        if (fbd != null) fbd.enabled = false;
    }

    // ── Network lifecycle ─────────────────────────────────────────────────────

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        // Force-start projectile animation on ALL instances (host + all clients)
        var anim = GetComponent<Animator>();
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            anim.Rebind();
            anim.Update(0f);
        }

        // Client: Rigidbody2D kinematic — NetworkTransform đồng bộ vị trí từ server
        if (!IsServer)
        {
            rb = rb != null ? rb : GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Server-side: schedule lifetime despawn
        if (IsServer)
            StartCoroutine(LifetimeDespawn());
    }

    private void Start()
    {
        rb = rb != null ? rb : GetComponent<Rigidbody2D>();
    }

    // ── Damage (Server-only, KHÔNG despawn khi trúng) ─────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        // Bỏ qua ground/wall
        if (other.CompareTag("Ground") || other.CompareTag("Wall")) return;

        // Per-target cooldown — tránh spam damage lên cùng một mục tiêu
        var targetNetObj = other.GetComponent<NetworkObject>()
                        ?? other.GetComponentInParent<NetworkObject>();
        if (targetNetObj != null)
        {
            // Bỏ qua chính owner
            if (ownerNetworkObjectId != 0 && targetNetObj.NetworkObjectId == ownerNetworkObjectId)
                return;

            ulong targetId = targetNetObj.NetworkObjectId;
            float now = Time.time;
            if (_recentHits.TryGetValue(targetId, out float lastHit) &&
                (now - lastHit) < PER_TARGET_HIT_COOLDOWN)
                return; // cooldown chưa hết, bỏ qua

            _recentHits[targetId] = now;
        }

        int finalDamage = damage > 0 ? damage : 50;

        // Damage enemy
        var netEnemy = other.GetComponent<NetworkEnemyHealth>()
                    ?? other.GetComponentInParent<NetworkEnemyHealth>();
        if (netEnemy != null) { netEnemy.TakeDamage(finalDamage); return; }

        var localEnemy = other.GetComponent<EnemyHealth>()
                      ?? other.GetComponentInParent<EnemyHealth>();
        if (localEnemy != null) { localEnemy.TakeDamage(finalDamage); return; }

        // PvP: damage player khác
        if (other.CompareTag("Player"))
        {
            var netPlayer = other.GetComponent<NetworkPlayerHealth>()
                         ?? other.GetComponentInParent<NetworkPlayerHealth>();
            if (netPlayer != null) { netPlayer.TakeDamage(finalDamage); return; }

            var localPlayer = other.GetComponent<PlayerHealth>()
                           ?? other.GetComponentInParent<PlayerHealth>();
            localPlayer?.TakeDamage(finalDamage);
        }
    }

    // ── Movement (Server-only — NetworkTransform đồng bộ vị trí sang client) ──

    private void Update()
    {
        if (!IsServer) return;

        timer += Time.deltaTime;

        if (!returning && timer >= returnDelay)
            returning = true;

        if (!returning) return;

        Transform ownerT = ResolveOwner();
        if (ownerT == null) return;

        if (rb != null)
            rb.velocity = ((Vector2)ownerT.position - (Vector2)transform.position).normalized * returnSpeed;

        // Khi về đủ gần owner thì Despawn
        if (Vector2.Distance(transform.position, ownerT.position) < 0.5f)
            DespawnOrDestroy();
    }

    private Transform ResolveOwner()
    {
        if (_ownerTransform != null) return _ownerTransform; // server path

        // Client path: tra cứu từ SpawnedObjects
        if (_ownerNetworkObjectId == 0) return null;
        if (NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                .TryGetValue(_ownerNetworkObjectId, out var netOwner) == true)
            return netOwner.transform;
        return null;
    }

    private IEnumerator LifetimeDespawn()
    {
        yield return new WaitForSeconds(maxLifetime);
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
}
