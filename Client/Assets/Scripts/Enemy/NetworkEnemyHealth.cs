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

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; // current, max
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;

    private bool isDead = false; // Flag để tránh xử lý death nhiều lần

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to networkCurrentHealth changes
        networkCurrentHealth.OnValueChanged += OnHealthValueChanged;

        // Chỉ server mới set giá trị ban đầu
        if (IsServer)
        {
            networkCurrentHealth.Value = maxHealth;
        }

        // Initialize UI cho tất cả clients
        OnHealthValueChanged(0, networkCurrentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        networkCurrentHealth.OnValueChanged -= OnHealthValueChanged;
        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Callback khi NetworkVariable health thay đổi
    /// Tự động sync cho tất cả clients
    /// </summary>
    private void OnHealthValueChanged(int oldValue, int newValue)
    {
        // Invoke event để update UI
        OnHealthChanged?.Invoke(newValue, maxHealth);

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
        // Tránh xử lý death nhiều lần
        if (isDead) return;
        isDead = true;

        Debug.Log($"[NetworkEnemyHealth] Enemy {NetworkObjectId} died!");

        // Notify clients về death
        OnDeathClientRpc();

        // Server xóa enemy sau delay (nếu cần animation)
        if (IsServer)
        {
            // Có thể thêm delay để chơi death animation
            Invoke(nameof(DestroyEnemyServer), 0.5f);
        }
    }

    /// <summary>
    /// ClientRpc: Notify clients về death
    /// </summary>
    [ClientRpc]
    private void OnDeathClientRpc()
    {
        OnDeath?.Invoke();
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

    // Public API để đọc giá trị
    public int GetCurrentHealth() => networkCurrentHealth.Value;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => (float)networkCurrentHealth.Value / maxHealth;

    /// <summary>
    /// Public method để các script khác gọi (tự động chuyển thành ServerRpc)
    /// </summary>
    public void TakeDamage(int damage)
    {
        TakeDamageServerRpc(damage);
    }
}
