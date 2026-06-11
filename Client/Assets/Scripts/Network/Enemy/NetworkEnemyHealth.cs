using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using Unity.Collections;

// NetworkEnemyHealth - Server-Authoritative Health System cho Enemy
// HP được quản lý bởi server, sync cho tất cả clients qua NetworkVariable
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

        // Enemy info synced via NetworkVariable (replicated to late joiners too)
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

        // DB enemy_id (từ bảng enemy) — dùng cho quest kill tracking.
        private NetworkVariable<int> _networkEnemyDbId =
            new NetworkVariable<int>(0,
                NetworkVariableReadPermission.Everyone,
                NetworkVariableWritePermission.Server);

        public string EnemyName    => _networkEnemyName.Value.IsEmpty ? "" : _networkEnemyName.Value.ToString();
        public string ElementType  => _networkElementType.Value.IsEmpty ? "None" : _networkElementType.Value.ToString();
        public int    EnemyLevel   => _networkEnemyLevel.Value > 0 ? _networkEnemyLevel.Value : 1;
        public int    EnemyDbId    => _networkEnemyDbId.Value;

        // Máu tối đa của quái (dùng để tính EXP Gene Tối Thượng khi giết).
        public int    MaxHealthValue => networkMaxHealth.Value > 0 ? networkMaxHealth.Value : maxHealth;

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

    // Callback khi NetworkVariable health thay đổi
    // Tự động sync cho tất cả clients
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

    // ServerRpc: Client yêu cầu server gây damage
    // Chỉ server mới có thể thực sự trừ HP
    // Internal: Xử lý damage trên server (không qua RPC).
    private void TakeDamageInternal(int damage, ulong attackerClientId)
    {
        if (networkCurrentHealth.Value <= 0 || isDead) return;

        var runtimeStats = GetComponent<DungeonEnemyRuntimeStats>();
        if (runtimeStats != null && runtimeStats.HasRuntimeOverride)
            damage = runtimeStats.ResolveIncomingDamage(damage);

        // DefenseDown debuff từ skill player: TĂNG damage nhận vào theo % giảm giáp
        var debuffMgr = GetComponent<DebuffManager>();
        if (debuffMgr != null)
        {
            int defDownPct = debuffMgr.GetDefenseDebuffPct();
            if (defDownPct > 0)
                damage = Mathf.Max(1, Mathf.RoundToInt(damage * (1f + defDownPct / 100f)));
        }

        if (attackerClientId != ulong.MaxValue)
            _lastAttackerClientId = attackerClientId;

        int newHealth = networkCurrentHealth.Value - damage;
        newHealth = Mathf.Max(newHealth, 0);
        networkCurrentHealth.Value = newHealth;

        OnServerTakeDamage?.Invoke(damage, attackerClientId);
        OnTakeDamageClientRpc(damage);

        { /* Enemy {NetworkObjectId} took {damage} damage. Health: {newHealth}/{maxHealth} */ }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int damage, ServerRpcParams rpcParams = default)
    {
        TakeDamageInternal(damage, rpcParams.Receive.SenderClientId);
    }

    // ClientRpc: Notify clients về damage (để play sound/effect)
        [ClientRpc]
    private void OnTakeDamageClientRpc(int damage)
    {
        OnTakeDamage?.Invoke();
    }

    // Enemy Info Sync (tên, hệ, level)

    // Server gọi để set NetworkVariables — được replicate tự động đến mọi client kể cả late-joiner.
    public void SetEnemyInfo(string enemyName, string elementType, int level, int enemyDbId = 0)
    {
        if (!IsServer) return;
        _networkEnemyName.Value   = enemyName ?? "";
        _networkElementType.Value = string.IsNullOrEmpty(elementType) ? "None" : elementType;
        _networkEnemyLevel.Value  = level > 0 ? level : 1;
        if (enemyDbId > 0) _networkEnemyDbId.Value = enemyDbId;
    }

    // Xử lý death trên server
    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;

        var runtimeOverride = GetComponent<EnemyStatOverride>();
        var runtimeStats = GetComponent<DungeonEnemyRuntimeStats>();
        { /* death netId={NetworkObjectId} name={gameObject.name} scene={(gameObject.scene.IsValid() ? gameObject.scene.name */ }

        if (IsServer)
        {
            try
            {
                var itemDrop = GetComponent<EnemyItemDrop>();
                itemDrop?.HandleDeathDrop();
            }
            catch (System.Exception ex)
            {
                { /* Lỗi: HandleDeathDrop failed: {ex.Message} */ }
            }

            var waveRuntime = FindAnyObjectByType<WaveDungeonRuntime>();
            if (waveRuntime != null)
            {
                { /* runtime=yes netId={NetworkObjectId} scene={gameObject.scene.name} */ }
            }
            else
            {
                { /* runtime=no netId={NetworkObjectId} scene={gameObject.scene.name} */ }
            }

            // OnDeathClientRpc chỉ chạy trên client, không chạy trên dedicated server.
            // Fire OnDeath trên server để WaveDungeonRuntime.HandleEnemyDeath() được trigger.
            // EnemyItemDrop.HandleDeathDrop() đã được gọi trực tiếp ở trên → hasDropped guard bảo vệ khỏi double-drop.
            OnDeath?.Invoke();
        }

        // Notify clients — play Die animation trước khi xóa
        OnDeathClientRpc();

        // Xử lý sự kiện attacker (EXP + Quest kill) — chạy trên server
        if (_lastAttackerClientId != ulong.MaxValue)
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
            {
                // Thưởng EXP
                if (ExpReward > 0)
                    killerSync.AwardExpOnServer(ExpReward, MaxHealthValue);

                // Quest kill hook: báo cáo tiến trình nhiệm vụ loại "kill"
                int dbPlayerId = killerSync.networkPlayerId.Value;
                int enemyDbId  = EnemyDbId;
                { /* dbPlayerId={dbPlayerId} enemyDbId={enemyDbId} enemy={gameObject.name} expReward={ExpReward} */ }
                if (dbPlayerId > 0 && enemyDbId > 0)
                {
                    { /* → gọi QuestProgressReporter.Report Kill playerId={dbPlayerId} targetId={enemyDbId} */ }
                    // Dùng killerSync làm host để coroutine không bị kill khi enemy despawn
                    var capturedSync = killerSync;
                    QuestProgressReporter.Report(killerSync, dbPlayerId, QuestProgressReporter.ProgressType.Kill, enemyDbId, 1,
                        () => capturedSync.NotifyQuestKillOnServer());
                }
                else
                    { /* Cảnh báo: BỎ QUA: dbPlayerId={dbPlayerId} enemyDbId={enemyDbId}  một trong hai bằng 0 */ }
            }
            else
                { /* Cảnh báo: Không tìm được NetworkPlayerDataSync cho client {_lastAttackerClientId} */ }
        }

        if (IsServer)
        {
            // Despawn ngay lập tức (không chờ animation) — OnDeathClientRpc đã được queue trước
            DestroyEnemyServer();
        }
    }

    // ClientRpc: Notify clients về death
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

    // Server xóa enemy
    private void DestroyEnemyServer()
    {
        if (!IsServer) return;
        bool hasNet = NetworkObject != null;
        bool isSpawned = hasNet && NetworkObject.IsSpawned;
        { /* DestroyEnemyServer name={gameObject.name} hasNet={hasNet} isSpawned={isSpawned} */ }
        // Despawn network object (chỉ nếu đã được spawn) — true để Destroy luôn GameObject,
        // đảm bảo go == null trong HostSpawnConfigLoader.CheckRespawnLoop hoạt động đúng.
        if (hasNet && isSpawned)
        {
            NetworkObject.Despawn(true);
            { /* Despawn(true) called on {gameObject.name} */ }
        }
        else
        {
            // Fallback: Destroy trực tiếp nếu không phải network object hoặc chưa spawn
            { /* Cảnh báo: Fallback Destroy (no NetworkObject or not spawned) name={gameObject.name} */ }
            Destroy(gameObject);
        }
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    public int GetCurrentHealth() => networkCurrentHealth.Value;
    // Trả về max HP — dùng networkMaxHealth.Value để đúng trên cả client.
    public int GetMaxHealth() => networkMaxHealth.Value > 0 ? networkMaxHealth.Value : maxHealth;
    public float GetHealthPercent() => maxHealth > 0
        ? (float)networkCurrentHealth.Value / maxHealth
        : 0f;

    // Đặt maxHealth TRƯỚC khi gọi NetworkObject.Spawn() để OnNetworkSpawn dùng giá trị đúng.
    // Không có IsServer guard — được gọi trên server trước khi NetworkObject được register với NGO.
    public void PreInitMaxHp(int hp)
    {
        if (hp <= 0) return;
        maxHealth = hp;
        { /* PreInitMaxHp({hp}) trên {gameObject.name} */ }
    }

    // Khởi tạo HP từ database (gọi bởi NetworkEnemySpawner sau networkObj.Spawn()).
    // Ghi đè giá trị maxHealth=10 cứng trong Inspector.
    public void InitHealth(int maxHp)
    {
        if (!IsServer) return;
        if (maxHp <= 0) return;
        maxHealth = maxHp;
        networkMaxHealth.Value = maxHp;  // sync maxHealth đến tất cả clients
        networkCurrentHealth.Value = maxHp;
        { /* InitHealth: {maxHp} HP (object {NetworkObjectId}) */ }
    }

    // EXP reward khi enemy chết. Được set bởi EnemyStatOverride từ DB config.
    public int ExpReward { get; private set; } = 0;

    // Lưu EXP override để death handler dùng (gọi bởi EnemyStatOverride).
    public void SetExpReward(int exp) => ExpReward = exp;

    // Public method để các script khác gọi (tự động chuyển thành ServerRpc).
    // Server-side callers (projectiles, skills) nên truyền attackerClientId để quest kill được ghi nhận.
    public void TakeDamage(int damage, ulong attackerClientId = ulong.MaxValue)
    {
        if (IsServer)
            TakeDamageInternal(damage, attackerClientId);
        else
            TakeDamageServerRpc(damage);
    }

    // Chặn hồi HP trong khoảng thời gian nhất định.
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
