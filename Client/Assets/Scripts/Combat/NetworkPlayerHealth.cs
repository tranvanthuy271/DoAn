using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

/// <summary>
/// NetworkPlayerHealth - Server-Authoritative Health System
/// HP được quản lý bởi server, sync cho tất cả clients qua NetworkVariable
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerHealth : NetworkBehaviour
{
    [Header("Components")]
    private PlayerController controller;

    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    
    // NetworkVariable để sync HP cho tất cả clients
    private NetworkVariable<int> networkCurrentHealth = new NetworkVariable<int>(
        100,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1f;
    private float invincibilityTimer;
    private bool isInvincible;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private Vector3[] spawnPoints; // Spawn points khi respawn
    private bool isDead = false;

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; // current, max
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;
    public UnityEvent OnHeal;
    public UnityEvent OnRespawn;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to networkCurrentHealth changes
        networkCurrentHealth.OnValueChanged += OnHealthValueChanged;

        // Initialize health từ PlayerStats nếu có
        if (controller != null && controller.stats != null)
        {
            maxHealth = controller.stats.maxHealth;
        }

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

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        // Tìm spawn points nếu chưa được gán
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            // Tìm spawn points trong scene
            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            if (spawnPointObjects.Length > 0)
            {
                spawnPoints = new Vector3[spawnPointObjects.Length];
                for (int i = 0; i < spawnPointObjects.Length; i++)
                {
                    spawnPoints[i] = spawnPointObjects[i].transform.position;
                }
            }
            else
            {
                // Fallback: Dùng vị trí hiện tại
                spawnPoints = new Vector3[] { transform.position };
            }
        }
    }

    private void Update()
    {
        // Chỉ owner hoặc server mới cần update invincibility
        if (!IsOwner && !IsServer) return;

        // Update invincibility timer
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
            }
        }
    }

    /// <summary>
    /// Callback khi NetworkVariable health thay đổi
    /// Tự động sync cho tất cả clients
    /// </summary>
    private void OnHealthValueChanged(int oldValue, int newValue)
    {
        // Invoke event để update UI
        OnHealthChanged?.Invoke(newValue, maxHealth);

        // Check death
        if (newValue <= 0 && oldValue > 0)
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
        // Server validate và xử lý damage
        if (isDead) return;

        // God mode prevents damage (nếu có)
        if (controller != null && controller.godMode)
        {
            Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} - God Mode: Damage blocked!");
            return;
        }

        // Invincibility check (server cần tự quản lý)
        if (isInvincible)
        {
            return;
        }

        // Server trừ HP
        int newHealth = networkCurrentHealth.Value - damage;
        newHealth = Mathf.Max(newHealth, 0);
        networkCurrentHealth.Value = newHealth;

        // Start invincibility frames
        if (newHealth > 0)
        {
            StartInvincibilityServerRpc();
        }

        // Notify clients về damage (để play sound/effect)
        OnTakeDamageClientRpc(damage);

        Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} took {damage} damage. Health: {newHealth}/{maxHealth}");
    }

    /// <summary>
    /// ServerRpc: Start invincibility frames
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void StartInvincibilityServerRpc()
    {
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
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
    /// ServerRpc: Client yêu cầu server heal
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int amount, ServerRpcParams rpcParams = default)
    {
        if (isDead) return;

        int newHealth = networkCurrentHealth.Value + amount;
        newHealth = Mathf.Min(newHealth, maxHealth);
        networkCurrentHealth.Value = newHealth;

        // Notify clients
        OnHealClientRpc(amount);

        Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} healed {amount}. Health: {newHealth}/{maxHealth}");
    }

    /// <summary>
    /// ClientRpc: Notify clients về heal
    /// </summary>
    [ClientRpc]
    private void OnHealClientRpc(int amount)
    {
        OnHeal?.Invoke();
    }

    /// <summary>
    /// ServerRpc: Heal full HP
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void HealFullServerRpc()
    {
        if (isDead) return;

        networkCurrentHealth.Value = maxHealth;
        OnHealClientRpc(maxHealth);
    }

    /// <summary>
    /// Xử lý death trên server
    /// </summary>
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} died!");

        // Notify clients về death
        OnDeathClientRpc();

        // Server xử lý respawn sau delay
        if (IsServer)
        {
            Invoke(nameof(RespawnServer), respawnDelay);
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
    /// Server xử lý respawn
    /// </summary>
    private void RespawnServer()
    {
        if (!IsServer) return;

        // Chọn spawn point ngẫu nhiên
        Vector3 spawnPosition = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Reset HP
        networkCurrentHealth.Value = maxHealth;
        isDead = false;
        isInvincible = false;

        // Teleport player đến spawn point
        transform.position = spawnPosition;

        // Notify clients về respawn
        OnRespawnClientRpc(spawnPosition);

        Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} respawned at {spawnPosition}");
    }

    /// <summary>
    /// ClientRpc: Notify clients về respawn
    /// </summary>
    [ClientRpc]
    private void OnRespawnClientRpc(Vector3 spawnPosition)
    {
        // Teleport player (nếu là owner, camera sẽ tự follow)
        if (IsOwner)
        {
            transform.position = spawnPosition;
        }

        OnRespawn?.Invoke();
    }

    // Public API để đọc giá trị (không cần network)
    public int GetCurrentHealth() => networkCurrentHealth.Value;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => (float)networkCurrentHealth.Value / maxHealth;
    public bool IsInvincible() => isInvincible;
    public bool IsDead() => isDead;

    /// <summary>
    /// Public method để các script khác gọi (tự động chuyển thành ServerRpc)
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            // Nếu đã là server, gọi trực tiếp
            TakeDamageServerRpc(damage);
        }
        else
        {
            // Nếu là client, gửi request lên server
            TakeDamageServerRpc(damage);
        }
    }

    public void Heal(int amount)
    {
        HealServerRpc(amount);
    }

    public void HealFull()
    {
        HealFullServerRpc();
    }
}
