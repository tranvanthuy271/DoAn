using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using Unity.Collections;

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
    public event System.Action<int, ulong> OnServerTakeDamage;

        private bool isDead = false;
        private bool _healBlocked = false;
        private ulong _lastAttackerClientId = ulong.MaxValue;

        // ── Enemy info synced via NetworkVariable (replicated to late joiners too) ──
        private NetworkVariable<FixedString128Bytes> _networkEnemyName =
            new NetworkVariable<FixedString128Bytes>(default,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private NetworkVariable<FixedString32Bytes> _networkElementType =
            new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("None"),
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        private NetworkVariable<int> _networkEnemyLevel =
            new NetworkVariable<int>(1,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public string EnemyName    => _networkEnemyName.Value.IsEmpty ? "" : _networkEnemyName.Value.ToString();
        public string ElementType  => _networkElementType.Value.IsEmpty ? "None" : _networkElementType.Value.ToString();
        public int    EnemyLevel   => _networkEnemyLevel.Value > 0 ? _networkEnemyLevel.Value : 1;

        public bool IsHealBlocked => _healBlocked;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Đảm bảo enemy luôn ở layer "Enemy" trên MỌI client.
        // Layer không tự sync qua NGO — cần set lại khi object được spawn về phía client.
        // Physics2D.OverlapCircleAll dùng layer của chính collider, không phải parent.
        int enemyLayerIdx = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIdx >= 0)
            SetLayerRecursively(gameObject, enemyLayerIdx);

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

    private static void SetLayerRecursively(GameObject go, int layer)
    {
        go.layer = layer;
        for (int i = 0; i < go.transform.childCount; i++)
            SetLayerRecursively(go.transform.GetChild(i).gameObject, layer);
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
    /// <summary>
    /// Internal: Xử lý damage trên server (không qua RPC).
    /// </summary>
    private void TakeDamageInternal(int damage, ulong attackerClientId)
    {
        if (networkCurrentHealth.Value <= 0 || isDead) return;

        var runtimeStats = GetComponent<DungeonEnemyRuntimeStats>();
        if (runtimeStats != null && runtimeStats.HasRuntimeOverride)
            damage = runtimeStats.ResolveIncomingDamage(damage);

        if (attackerClientId != ulong.MaxValue)
            _lastAttackerClientId = attackerClientId;

        int newHealth = networkCurrentHealth.Value - damage;
        newHealth = Mathf.Max(newHealth, 0);
        networkCurrentHealth.Value = newHealth;

        OnServerTakeDamage?.Invoke(damage, attackerClientId);
        OnTakeDamageClientRpc(damage);

        Debug.Log($"[NetworkEnemyHealth] Enemy {NetworkObjectId} took {damage} damage. Health: {newHealth}/{maxHealth}");
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        TakeDamageInternal(damage, rpcParams.Receive.SenderClientId);
    }

    /// <summary>
    /// ClientRpc: Notify clients về damage (để play sound/effect)
    /// </summary>
        [ClientRpc]
    private void OnTakeDamageClientRpc(int damage)
    {
        OnTakeDamage?.Invoke();
    }

    // ── Enemy Info Sync (tên, hệ, level) ───────────────────────────────────

    /// <summary>
    /// Server gọi để set NetworkVariables — được replicate tự động đến mọi client kể cả late-joiner.
    /// </summary>
    public void SetEnemyInfo(string enemyName, string elementType, int level)
    {
        if (!IsServer) return;
        _networkEnemyName.Value  = enemyName ?? "";
        _networkElementType.Value = string.IsNullOrEmpty(elementType) ? "None" : elementType;
        _networkEnemyLevel.Value  = level > 0 ? level : 1;
    }

    /// <summary>
    /// Xử lý death trên server
    /// </summary>
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        var runtimeOverride = GetComponent<EnemyStatOverride>();
        var runtimeStats = GetComponent<DungeonEnemyRuntimeStats>();
        Debug.Log($"[NEH] death netId={NetworkObjectId} name={gameObject.name} scene={(gameObject.scene.IsValid() ? gameObject.scene.name : "invalid")} boss={(runtimeOverride != null && runtimeOverride.IsBoss)} runtime={(runtimeStats != null && runtimeStats.HasRuntimeOverride)} atk={_lastAttackerClientId} exp={ExpReward}");

        if (IsServer)
        {
            try
            {
                var itemDrop = GetComponent<EnemyItemDrop>();
                itemDrop?.HandleDeathDrop();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NetworkEnemyHealth] HandleDeathDrop failed: {ex.Message}");
            }

            var waveRuntime = FindAnyObjectByType<WaveDungeonRuntime>();
            if (waveRuntime != null)
            {
                Debug.Log($"[NEH] runtime=yes netId={NetworkObjectId} scene={gameObject.scene.name}");
            }
            else
            {
                Debug.Log($"[NEH] runtime=no netId={NetworkObjectId} scene={gameObject.scene.name}");
            }

            // OnDeathClientRpc chỉ chạy trên client, không chạy trên dedicated server.
            // Fire OnDeath trên server để WaveDungeonRuntime.HandleEnemyDeath() được trigger.
            // EnemyItemDrop.HandleDeathDrop() đã được gọi trực tiếp ở trên → hasDropped guard bảo vệ khỏi double-drop.
            OnDeath?.Invoke();
        }

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
            // Despawn ngay lập tức (không chờ animation) — OnDeathClientRpc đã được queue trước
            DestroyEnemyServer();
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
    /// Đặt maxHealth TRƯỚC khi gọi NetworkObject.Spawn() để OnNetworkSpawn dùng giá trị đúng.
    /// Không có IsServer guard — được gọi trên server trước khi NetworkObject được register với NGO.
    /// </summary>
    public void PreInitMaxHp(int hp)
    {
        if (hp <= 0) return;
        maxHealth = hp;
        Debug.Log($"[NetworkEnemyHealth] PreInitMaxHp({hp}) trên {gameObject.name}");
    }

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
        if (IsServer)
            TakeDamageInternal(damage, ulong.MaxValue);
        else
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
