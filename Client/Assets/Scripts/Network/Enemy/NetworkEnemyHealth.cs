using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

/// <summary>
/// NetworkEnemyHealth - Server-Authoritative Health System cho Enemy
/// HP được quản lý bởi server, sync cho tất cả clients qua NetworkVariable
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkEnemyHealth : NetworkBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 10;
    
    // NetworkVariable để sync HP cho tất cả clients
    private NetworkVariable<int> networkCurrentHealth = new NetworkVariable<int>(
        10,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // NetworkVariable sync maxHealth — cần để HP bar client hiển thị đúng tỉ lệ
    private NetworkVariable<int> networkMaxHealth = new NetworkVariable<int>(
        10,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; // current, max
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;

    private bool isDead = false; // Flag để tránh xử lý death nhiều lần
    private bool _healBlocked = false;
    private ulong _lastAttackerClientId = ulong.MaxValue; // Client ID của người gây damage cuối

    /// <summary>Trả về true nếu enemy đang bị chặn hồi HP.</summary>
    public bool IsHealBlocked => _healBlocked;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to networkCurrentHealth changes
        networkCurrentHealth.OnValueChanged += OnHealthValueChanged;
        networkMaxHealth.OnValueChanged += OnMaxHealthValueChanged;

        // Chỉ server mới set giá trị ban đầu
        if (IsServer)
        {
            networkMaxHealth.Value = maxHealth;
            networkCurrentHealth.Value = maxHealth;
        }

        // Initialize UI cho tất cả clients (dùng networkMaxHealth.Value để đúng trên mọi client)
        OnHealthValueChanged(0, networkCurrentHealth.Value);

        // Client: đảm bảo EnemyClickHandler tồn tại để click chọn enemy hoạt động
        if (IsClient && GetComponent<EnemyClickHandler>() == null)
            gameObject.AddComponent<EnemyClickHandler>();
    }

    public override void OnNetworkDespawn()
    {
        networkCurrentHealth.OnValueChanged -= OnHealthValueChanged;
        networkMaxHealth.OnValueChanged -= OnMaxHealthValueChanged;
        base.OnNetworkDespawn();
    }

    private void OnMaxHealthValueChanged(int oldValue, int newValue)
    {
        // Cập nhật maxHealth local khi sync từ server
        maxHealth = newValue;
        // Refresh HP bar
        OnHealthChanged?.Invoke(networkCurrentHealth.Value, newValue);
    }

    /// <summary>
    /// Callback khi NetworkVariable health thay đổi
    /// Tự động sync cho tất cả clients
    /// </summary>
    private void OnHealthValueChanged(int oldValue, int newValue)
    {
        // Invoke event để update UI — dùng networkMaxHealth.Value để đảm bảo đúng trên mọi client
        OnHealthChanged?.Invoke(newValue, networkMaxHealth.Value > 0 ? networkMaxHealth.Value : maxHealth);

        // Check death (chỉ xử lý trên server, tránh gọi nhiều lần)
        if (newValue <= 0 && oldValue > 0 && IsServer && !isDead)
        {
            HandleDeath();
        }
    }

    /// <summary>
    /// ServerRpc: Client yêu cầu server gây damage
    /// Chỉ server mới có thể thực sự trừ HP
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        // Không nhận damage nếu đã chết
        if (networkCurrentHealth.Value <= 0 || isDead) return;

        // Ghi nhớ người đánh cuối để cứu xét EXP
        _lastAttackerClientId = rpcParams.Receive.SenderClientId;

        // Server trừ HP
        int newHealth = networkCurrentHealth.Value - damage;
        newHealth = Mathf.Max(newHealth, 0);
        networkCurrentHealth.Value = newHealth;

        // Notify clients về damage
        OnTakeDamageClientRpc(damage);

        Debug.Log($"[NetworkEnemyHealth] Enemy {NetworkObjectId} took {damage} damage. Health: {newHealth}/{maxHealth}");

        // Không gọi HandleDeath() ở đây nữa - để OnHealthValueChanged xử lý
        // Tránh gọi death nhiều lần
    }

    /// <summary>
    /// ClientRpc: Notify clients về damage (để play sound/effect)
    /// </summary>
    [ClientRpc]
    private void OnTakeDamageClientRpc(int damage)
    {
        OnTakeDamage?.Invoke();
    }

    /// <summary>
    /// Xử lý death trên server
    /// </summary>
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[NetworkEnemyHealth] Enemy {NetworkObjectId} died! ExpReward={ExpReward} Attacker={_lastAttackerClientId}");

        // Notify clients — play Die animation trước khi xóa
        OnDeathClientRpc();

        // Thưởng EXP cho người đánh cuối (chạy trên server)
        if (ExpReward > 0 && _lastAttackerClientId != ulong.MaxValue)
        {
            // Tìm NetworkPlayerDataSync của killer theo OwnerClientId
            // (dùng SpawnedObjects vì game dùng SpawnWithOwnership, không phải SpawnAsPlayerObject)
            NetworkPlayerDataSync killerSync = null;
            foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
            {
                if (kvp.Value.OwnerClientId == _lastAttackerClientId)
                {
                    var sync = kvp.Value.GetComponent<NetworkPlayerDataSync>();
                    if (sync != null) { killerSync = sync; break; }
                }
            }

            if (killerSync != null)
                killerSync.AwardExpOnServer(ExpReward);
            else
                Debug.LogWarning($"[NetworkEnemyHealth] Không tìm được NetworkPlayerDataSync cho client {_lastAttackerClientId}");
        }

        if (IsServer)
        {
            // Chờ animation die (0.8 giây) rồi mới Despawn
            Invoke(nameof(DestroyEnemyServer), 0.9f);
        }
    }

    /// <summary>
    /// ClientRpc: Notify clients về death
    /// </summary>
    [ClientRpc]
    private void OnDeathClientRpc()
    {
        OnDeath?.Invoke();

        // Kích hoạt Die animation trên client
        Animator anim = GetComponent<Animator>();
        if (anim != null)
        {
            foreach (var p in anim.parameters)
            {
                if (p.name == "Die")
                {
                    anim.SetTrigger("Die");
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Server xóa enemy
    /// </summary>
    private void DestroyEnemyServer()
    {
        if (!IsServer) return;
        
        // Despawn network object (chỉ nếu đã được spawn)
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        else if (NetworkObject == null || !NetworkObject.IsSpawned)
        {
            // Fallback: Destroy trực tiếp nếu không phải network object hoặc chưa spawn
            Destroy(gameObject);
        }
    }

    // ── Public API ─────────────────────────────────────────────

    public int GetCurrentHealth() => networkCurrentHealth.Value;
    /// <summary>Trả về max HP — dùng networkMaxHealth.Value để đúng trên cả client.</summary>
    public int GetMaxHealth() => networkMaxHealth.Value > 0 ? networkMaxHealth.Value : maxHealth;
    public float GetHealthPercent() => maxHealth > 0
        ? (float)networkCurrentHealth.Value / maxHealth
        : 0f;

    /// <summary>
    /// Khởi tạo HP từ database (gọi bởi NetworkEnemySpawner sau networkObj.Spawn()).
    /// Ghi đè giá trị maxHealth=10 cứng trong Inspector.
    /// </summary>
    public void InitHealth(int maxHp)
    {
        if (!IsServer) return;
        if (maxHp <= 0) return;
        maxHealth = maxHp;
        networkMaxHealth.Value = maxHp;  // sync maxHealth đến tất cả clients
        networkCurrentHealth.Value = maxHp;
        Debug.Log($"[NetworkEnemyHealth] InitHealth: {maxHp} HP (object {NetworkObjectId})");
    }

    /// <summary>EXP reward khi enemy chết. Được set bởi EnemyStatOverride từ DB config.</summary>
    public int ExpReward { get; private set; } = 0;

    /// <summary>Lưu EXP override để death handler dùng (gọi bởi EnemyStatOverride).</summary>
    public void SetExpReward(int exp) => ExpReward = exp;

    /// <summary>
    /// Public method để các script khác gọi (tự động chuyển thành ServerRpc)
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamageServerRpc(damage);
    }

    /// <summary>
    /// Chặn hồi HP trong khoảng thời gian nhất định.
    /// </summary>
    public void BlockHeal(float duration)
    {
        StartCoroutine(BlockHealCoroutine(duration));
    }

    private System.Collections.IEnumerator BlockHealCoroutine(float duration)
    {
        _healBlocked = true;
        yield return new WaitForSeconds(duration);
        _healBlocked = false;
    }
}
