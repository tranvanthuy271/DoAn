using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

// NetworkPlayerHealth - Server-Authoritative Health System
// HP được quản lý bởi server, sync cho tất cả clients qua NetworkVariable
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

    // Heal Block (Lava Aura)
    private bool isHealBlocked = false;
    private float healBlockTimer = 0f;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 5f;
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
            // Tìm spawn points trong scene - Tìm bằng tên thay vì tag để tránh lỗi tag không tồn tại
            GameObject[] spawnPointObjects = null;
            
            // Thử tìm bằng tag nếu tag tồn tại
            try
            {
                spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            }
            catch (UnityException)
            {
                // Tag không tồn tại, tìm bằng tên thay thế
                spawnPointObjects = GameObject.FindGameObjectsWithTag("Untagged"); // Tạm thời dùng Untagged
                // Hoặc tìm bằng tên
                var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
                var spawnPointsList = new System.Collections.Generic.List<GameObject>();
                foreach (var obj in allObjects)
                {
                    if (obj.name.Contains("SpawnPoint") || obj.name.Contains("Spawn"))
                    {
                        spawnPointsList.Add(obj);
                    }
                }
                spawnPointObjects = spawnPointsList.ToArray();
            }
            
            if (spawnPointObjects != null && spawnPointObjects.Length > 0)
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
                Debug.LogWarning($"[NetworkPlayerHealth] No spawn points found, using current position: {transform.position}");
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

        // Update heal block timer (server-side)
        if (IsServer && healBlockTimer > 0f)
        {
            healBlockTimer -= Time.deltaTime;
            if (healBlockTimer <= 0f)
            {
                isHealBlocked = false;
                healBlockTimer = 0f;
            }
        }
    }

    // Callback khi NetworkVariable health thay đổi
    // Tự động sync cho tất cả clients
    private void OnHealthValueChanged(int oldValue, int newValue)
    {
        // Use networkMaxHp from NetworkPlayerDataSync when available — it is a proper NetworkVariable
        // synced from server and is always correct. The local maxHealth field can be stale on clients.
        var dataSync = GetComponent<NetworkPlayerDataSync>();
        int effectiveMaxHp = (dataSync != null && dataSync.networkMaxHp.Value > 0)
                             ? dataSync.networkMaxHp.Value
                             : maxHealth;

        // Invoke event để update UI
        OnHealthChanged?.Invoke(newValue, effectiveMaxHp);

        // Check death
        if (newValue <= 0 && oldValue > 0)
        {
            HandleDeath();
        }
    }

    // Internal: Xử lý damage trên server (không qua RPC).
    // Gọi bởi TakeDamage (khi IsServer) và TakeDamageServerRpc (khi client gửi RPC).
    private void TakeDamageInternal(int damage)
    {
        if (isDead) return;

        if (controller != null && controller.godMode)
        {
            Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} - God Mode: Damage blocked!");
            return;
        }

        if (isInvincible) return;

        var dataSync = GetComponent<NetworkPlayerDataSync>();
        if (dataSync != null && dataSync.networkDefenseBonusPct.Value > 0)
        {
            float defBonus = dataSync.networkDefenseBonusPct.Value / 100f;
            damage = Mathf.Max(1, Mathf.RoundToInt(damage / (1f + defBonus)));
        }

        // DefenseDown debuff từ skill enemy/PvP: TĂNG damage nhận vào theo % giảm giáp
        var debuffMgr = GetComponent<DebuffManager>();
        if (debuffMgr != null)
        {
            int defDownPct = debuffMgr.GetDefenseDebuffPct();
            if (defDownPct > 0)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f + defDownPct / 100f)));
        }

        int newHealth = networkCurrentHealth.Value - damage;
        newHealth = Mathf.Max(newHealth, 0);
        networkCurrentHealth.Value = newHealth;

        if (dataSync != null)
            dataSync.networkHp.Value = newHealth;

        if (newHealth > 0)
        {
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }

        OnTakeDamageClientRpc(damage);

        Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} took {damage} damage. Health: {newHealth}/{maxHealth}");
    }

    // ServerRpc: Client yêu cầu server gây damage
    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        TakeDamageInternal(damage);
    }

    // ClientRpc: Notify clients về damage (để play sound/effect)
    [ClientRpc]
    private void OnTakeDamageClientRpc(int damage)
    {
        OnTakeDamage?.Invoke();
    }

    // ServerRpc: Client yêu cầu server heal
    // ServerRpc: Chặn hồi máu trong thời gian nhất định (dùng bởi Lava Aura)
    [ServerRpc(RequireOwnership = false)]
    public void BlockHealServerRpc(float duration)
    {
        isHealBlocked = true;
        healBlockTimer = Mathf.Max(healBlockTimer, duration);
        Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} heal blocked for {duration}s");
    }

    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(int amount, ServerRpcParams rpcParams = default)
    {
        HealInternal(amount);
    }

    private void HealInternal(int amount)
    {
        if (isDead) return;
        if (isHealBlocked)
        {
            Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} - Heal bị chặn bởi Lava Aura!");
            return;
        }

        int newHealth = networkCurrentHealth.Value + amount;
        newHealth = Mathf.Min(newHealth, maxHealth);
        networkCurrentHealth.Value = newHealth;

        var dataSync = GetComponent<NetworkPlayerDataSync>();
        if (dataSync != null)
            dataSync.networkHp.Value = newHealth;

        OnHealClientRpc(amount);

        Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} healed {amount}. Health: {newHealth}/{maxHealth}");
    }

    // ClientRpc: Notify clients về heal
    [ClientRpc]
    private void OnHealClientRpc(int amount)
    {
        OnHeal?.Invoke();
    }

    // ServerRpc: Heal full HP
    [ServerRpc(RequireOwnership = false)]
    public void HealFullServerRpc()
    {
        HealFullInternal();
    }

    private void HealFullInternal()
    {
        if (isDead) return;

        networkCurrentHealth.Value = maxHealth;
        var dataSync = GetComponent<NetworkPlayerDataSync>();
        if (dataSync != null)
            dataSync.networkHp.Value = maxHealth;
        OnHealClientRpc(maxHealth);
    }

    // Xử lý death trên server
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

    // ClientRpc: Notify clients về death
    [ClientRpc]
    private void OnDeathClientRpc()
    {
        var playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInChildren<PlayerAnimator>();
        playerAnimator?.TriggerDie();

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = Vector2.zero;

        OnDeath?.Invoke();
    }

    // Server xử lý respawn
    private void RespawnServer()
    {
        if (!IsServer) return;

        Vector3 spawnPosition = Vector3.zero;
        bool zoneRespawnHandled = false;
        var zoneTransition = FindAnyObjectByType<ZoneTransitionController>();
        if (zoneTransition != null)
            zoneRespawnHandled = zoneTransition.TryRespawnClientAfterDeath(OwnerClientId, NetworkObject, out spawnPosition);

        if (!zoneRespawnHandled)
        {
            // Chọn spawn point ngẫu nhiên
            if (spawnPoints != null && spawnPoints.Length > 0)
                spawnPosition = spawnPoints[Random.Range(0, spawnPoints.Length)];
            else
                Debug.LogWarning("[NetworkPlayerHealth] spawnPoints trống — respawn tại (0,0,0)");
        }

        // Lấy HP/MP max thực tế từ NetworkPlayerDataSync (authoritative stats từ API)
        var dataSync = GetComponent<NetworkPlayerDataSync>();
        int fullHp = (dataSync != null && dataSync.networkMaxHp.Value > 0)
                     ? dataSync.networkMaxHp.Value
                     : maxHealth;

        // Đồng bộ maxHealth trong NetworkPlayerHealth cho nhất quán
        maxHealth = fullHp;

        // Reset HP — cập nhật CẢ HAI system để HealthBar nhận được callback
        networkCurrentHealth.Value = fullHp;
        if (dataSync != null)
        {
            dataSync.networkHp.Value = fullHp;                           // HealthBar subscribe cái này
            dataSync.networkMp.Value = dataSync.networkMaxMp.Value;      // MpBar subscribe cái này
        }

        isDead = false;
        isInvincible = false;

        // Teleport player đến spawn point
        transform.position = spawnPosition;

        // Notify clients về respawn
        OnRespawnClientRpc(spawnPosition);

        Debug.Log($"[NetworkPlayerHealth] Player {NetworkObjectId} respawned at {spawnPosition} HP={fullHp}/{fullHp}");
    }

    // ClientRpc: Notify clients về respawn
    [ClientRpc]
    private void OnRespawnClientRpc(Vector3 spawnPosition)
    {
        isDead = false;
        isInvincible = false;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        var playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInChildren<PlayerAnimator>();
        playerAnimator?.ResetToIdleAfterRespawn();

        transform.position = spawnPosition;

        OnRespawn?.Invoke();
    }

    // Public API để đọc giá trị (không cần network)
    public int GetCurrentHealth() => networkCurrentHealth.Value;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => (float)networkCurrentHealth.Value / maxHealth;
    public bool IsInvincible() => isInvincible;
    public bool IsDead() => isDead;

    // Public method để các script khác gọi (tự động chuyển thành ServerRpc)
    public void TakeDamage(int damage)
    {
        if (IsServer)
        {
            TakeDamageInternal(damage);
        }
        else
        {
            TakeDamageServerRpc(damage);
        }
    }

    // Nhận sát thương có xét hệ nguyên tố của kẻ tấn công.
    // - Nếu attackerElement khắc hệ của người chơi bị tấn công → +30% sát thương.
    // - Nếu người chơi là Hybrid và attackerElement nằm trong HybridImmuneElements → bỏ qua bổ sung.
    // Gọi từ quái/enemy có elementType xác định (MobPatrolAI, EnemyAI…).
    public void TakeDamageWithElement(int rawDamage, string attackerElement)
    {
        if (IsServer)
            TakeDamageWithElementInternal(rawDamage, attackerElement);
        else
            TakeDamageWithElementServerRpc(rawDamage, attackerElement);
    }

    private void TakeDamageWithElementInternal(int rawDamage, string attackerElement)
    {
        var pd = ServerPlayerDataManager.Instance?.GetPlayerDataByClientId(OwnerClientId);
        int finalDamage = DamageCalculator.CalcPlayerReceivedElementDamage(rawDamage, attackerElement, pd);

        TakeDamageInternal(finalDamage);
    }

    [ServerRpc(RequireOwnership = false)]
    private void TakeDamageWithElementServerRpc(int rawDamage, string attackerElement,
                                                 ServerRpcParams rpc = default)
    {
        TakeDamageWithElementInternal(rawDamage, attackerElement);
    }

    public void Heal(int amount)
    {
        if (IsServer)
            HealInternal(amount);
        else
            HealServerRpc(amount);
    }

    public void HealFull()
    {
        if (IsServer)
            HealFullInternal();
        else
            HealFullServerRpc();
    }

    // ServerRpc: Set max health (chỉ server mới có quyền)
    [ServerRpc(RequireOwnership = false)]
    public void SetMaxHealthServerRpc(int newMaxHealth)
    {
        if (!IsServer) return;
        
        maxHealth = newMaxHealth;
        
        // Nếu current health > max health, giảm xuống
        if (networkCurrentHealth.Value > maxHealth)
        {
            networkCurrentHealth.Value = maxHealth;
        }
        
        Debug.Log($"[NetworkPlayerHealth] Max health set to {maxHealth} for player {NetworkObjectId}");
    }

    // ServerRpc: Set current health (chỉ server mới có quyền)
    [ServerRpc(RequireOwnership = false)]
    public void SetHealthServerRpc(int newHealth)
    {
        if (!IsServer) return;
        
        newHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        networkCurrentHealth.Value = newHealth;
        
        Debug.Log($"[NetworkPlayerHealth] Health set to {newHealth}/{maxHealth} for player {NetworkObjectId}");
    }

    // Public method để set max health (tự động chuyển thành ServerRpc nếu cần)
    public void SetMaxHealth(int newMaxHealth)
    {
        if (IsServer)
        {
            maxHealth = newMaxHealth;
            if (networkCurrentHealth.Value > maxHealth)
            {
                networkCurrentHealth.Value = maxHealth;
            }
        }
        else
        {
            SetMaxHealthServerRpc(newMaxHealth);
        }
    }

    // Public method để set current health (tự động chuyển thành ServerRpc nếu cần)
    public void SetHealth(int newHealth)
    {
        if (IsServer)
        {
            newHealth = Mathf.Clamp(newHealth, 0, maxHealth);
            networkCurrentHealth.Value = newHealth;
        }
        else
        {
            SetHealthServerRpc(newHealth);
        }
    }
}
