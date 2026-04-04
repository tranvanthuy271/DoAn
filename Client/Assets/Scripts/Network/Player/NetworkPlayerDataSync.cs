using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

/// <summary>
/// Đồng bộ player data (element_type, gender, character_name, stats) từ API qua NetworkVariable
/// Shared script - dùng cho cả client và server
/// </summary>
public class NetworkPlayerDataSync : NetworkBehaviour
{
    [Header("Player Data (Synced)")]
    public NetworkVariable<int> networkPlayerId = new NetworkVariable<int>(0);
    public NetworkVariable<FixedString32Bytes> networkElementType = new NetworkVariable<FixedString32Bytes>("Fire");
    public NetworkVariable<FixedString32Bytes> networkGender = new NetworkVariable<FixedString32Bytes>("Male");
    public NetworkVariable<FixedString64Bytes> networkCharacterName = new NetworkVariable<FixedString64Bytes>("");
    public NetworkVariable<int> networkLevel = new NetworkVariable<int>(1);
    public NetworkVariable<int> networkHp = new NetworkVariable<int>(100);
    public NetworkVariable<int> networkMaxHp = new NetworkVariable<int>(100);
    public NetworkVariable<int> networkMp = new NetworkVariable<int>(50);
    public NetworkVariable<int> networkMaxMp = new NetworkVariable<int>(50);
    public NetworkVariable<int> networkAttack = new NetworkVariable<int>(10);
    public NetworkVariable<int> networkDefense = new NetworkVariable<int>(0);
    public NetworkVariable<float> networkMoveSpeed = new NetworkVariable<float>(5f);
    public NetworkVariable<int> networkGeneTier = new NetworkVariable<int>(1);

    // ── Buff stat modifiers (set by server from ActiveBuff) ──────────────
    /// <summary>% bonus EXP gene nạp vào (e.g. 20 = +20%). Set bởi server khi dùng GeneExpBuff item.</summary>
    public NetworkVariable<int> networkGeneExpBonusPct  = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>% bonus EXP khi kill enemy (e.g. 25 = +25%).</summary>
    public NetworkVariable<int> networkExpBonusPct      = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>% bonus vàng và EXP drop (Phúc buff).</summary>
    public NetworkVariable<int> networkPhucBonusPct     = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>% tăng sát thương (AttackBuff).</summary>
    public NetworkVariable<int> networkAttackBonusPct   = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    /// <summary>% giảm sát thương nhận (DefenseBuff).</summary>
    public NetworkVariable<int> networkDefenseBonusPct  = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    [Header("References")]
    private PlayerController playerController;
    private NetworkPlayerHealth playerHealth;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Server: Load player data từ ServerPlayerDataManager và set vào NetworkVariable
            LoadPlayerDataFromGameManager();
        }
        // NOTE: Auth giờ được gửi qua CustomMessagingManager (Named Messages) trong NetworkManagerCustom
        // Không cần gửi auth từ đây nữa vì auth đã được gửi TRƯỚC khi player spawn

        // Subscribe để update khi data thay đổi
        networkElementType.OnValueChanged += OnElementTypeChanged;
        networkGender.OnValueChanged += OnGenderChanged;
        networkCharacterName.OnValueChanged += OnCharacterNameChanged;
        networkLevel.OnValueChanged += OnLevelChanged;
        networkHp.OnValueChanged += OnHpChanged;
        networkMaxHp.OnValueChanged += OnMaxHpChanged;
        networkAttack.OnValueChanged += OnAttackChanged;
        networkDefense.OnValueChanged += OnDefenseChanged;
        networkMoveSpeed.OnValueChanged += OnMoveSpeedChanged;
        networkGeneTier.OnValueChanged += OnGeneTierChanged;
        networkMp.OnValueChanged += OnMpChanged;
        networkMaxMp.OnValueChanged += OnMaxMpChanged;

        // Apply data ngay lập tức
        ApplyPlayerData();
    }

    /// <summary>
    /// Client: Gửi auth (userId + token) lên server ngay khi player spawn
    /// </summary>
    private void SendAuthToServer()
    {
        string token = PlayerPrefs.GetString("JWT_TOKEN", "");
        int userId = PlayerPrefs.GetInt("USER_ID", 0);

        if (string.IsNullOrEmpty(token) || userId == 0)
        {
            Debug.LogError($"[NetworkPlayerDataSync] ✗ Cannot send auth - JWT_TOKEN or USER_ID not found!");
            Debug.LogError($"[NetworkPlayerDataSync] Token empty: {string.IsNullOrEmpty(token)}, UserId: {userId}");
            return;
        }

        Debug.Log($"[NetworkPlayerDataSync] ===== SENDING AUTH TO SERVER =====");
        Debug.Log($"[NetworkPlayerDataSync] UserId: {userId}");
        Debug.Log($"[NetworkPlayerDataSync] Token length: {token.Length}");
        Debug.Log($"[NetworkPlayerDataSync] OwnerClientId: {OwnerClientId}");
        Debug.Log($"[NetworkPlayerDataSync] Calling SendAuthServerRpc...");

        SendAuthServerRpc(token, userId);
    }

    /// <summary>
    /// ServerRpc: Nhận auth từ client và load player data
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    private void SendAuthServerRpc(string token, int userId, ServerRpcParams rpcParams = default)
    {
        var senderClientId = rpcParams.Receive.SenderClientId;

        Debug.Log("\n\n\n");
        Debug.Log("█████████████████████████████████████████████████████");
        Debug.Log("█████████████████████████████████████████████████████");
        Debug.Log("███ 🎯 AUTH SERVERRPC RECEIVED ON HOST!!! 🎯 ███");
        Debug.Log("█████████████████████████████████████████████████████");
        Debug.Log("█████████████████████████████████████████████████████");
        Debug.Log($"[HOST/SERVER] Time: {Time.time}");
        Debug.Log($"[HOST/SERVER] Frame: {Time.frameCount}");
        Debug.Log($"[HOST/SERVER] SenderClientId: {senderClientId}");
        Debug.Log($"[HOST/SERVER] UserId: {userId}");
        Debug.Log($"[HOST/SERVER] Token length: {token?.Length ?? 0}");

        // Load player data từ API
        if (ServerPlayerDataManager.Instance != null)
        {
            Debug.Log("[NetworkPlayerDataSync] ===== CALLING SERVERPLAYERDATAMANAGER =====");
            Debug.Log($"[NetworkPlayerDataSync] Parameters - ClientId: {senderClientId}, UserId: {userId}");

            ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                senderClientId,
                userId,
                onSuccess: (playerData) =>
                {
                    Debug.Log("[NetworkPlayerDataSync] ===== PLAYER DATA LOADED SUCCESSFULLY =====");
                    Debug.Log($"[NetworkPlayerDataSync] ✓ ClientId: {senderClientId}");
                    Debug.Log($"[NetworkPlayerDataSync] ✓ Character: {playerData.character_name}");
                    Debug.Log($"[NetworkPlayerDataSync] ✓ Element: {playerData.element_type}");
                    Debug.Log($"[NetworkPlayerDataSync] ✓ Gender: {playerData.gender}");
                    Debug.Log($"[NetworkPlayerDataSync] ✓ Level: {playerData.level}");

                    // Update NetworkVariables với player data mới load
                    UpdateNetworkVariablesFromPlayerData(playerData);
                },
                onError: (error) =>
                {
                    Debug.LogError("[NetworkPlayerDataSync] ===== FAILED TO LOAD PLAYER DATA =====");
                    Debug.LogError($"[NetworkPlayerDataSync] ✗ ClientId: {senderClientId}");
                    Debug.LogError($"[NetworkPlayerDataSync] ✗ UserId: {userId}");
                    Debug.LogError($"[NetworkPlayerDataSync] ✗ Error: {error}");
                }
            );
        }
        else
        {
            Debug.LogError("[NetworkPlayerDataSync] ===== SERVERPLAYERDATAMANAGER IS NULL =====");
            Debug.LogError($"[NetworkPlayerDataSync] ✗ Cannot load player data for clientId: {senderClientId}, userId: {userId}");
        }
    }

    /// <summary>
    /// Server: Update NetworkVariables từ PlayerDataResponse
    /// </summary>
    private void UpdateNetworkVariablesFromPlayerData(PlayerDataResponse playerData)
    {
        if (playerData == null) return;

        networkPlayerId.Value = playerData.player_id;
        networkElementType.Value = (FixedString32Bytes)(playerData.element_type ?? "Fire");
        networkGender.Value = (FixedString32Bytes)(playerData.gender ?? "Male");
        networkCharacterName.Value = (FixedString64Bytes)(playerData.character_name ?? "");
        networkLevel.Value = playerData.level;

        // Stats từ final_stats hoặc base_stats
        if (playerData.final_stats != null)
        {
            networkHp.Value        = playerData.final_stats.hp;
            networkMaxHp.Value     = playerData.final_stats.max_hp;
            networkMp.Value        = playerData.final_stats.mp;
            networkMaxMp.Value     = playerData.final_stats.max_mp;
            networkAttack.Value    = playerData.final_stats.attack;
            networkDefense.Value   = playerData.final_stats.defense;
            networkMoveSpeed.Value = playerData.final_stats.move_speed;
        }
        else if (playerData.base_stats != null)
        {
            networkHp.Value        = playerData.base_stats.hp;
            networkMaxHp.Value     = playerData.base_stats.max_hp;
            networkMp.Value        = playerData.base_stats.mp;
            networkMaxMp.Value     = playerData.base_stats.max_mp;
            networkAttack.Value    = playerData.base_stats.attack;
            networkMoveSpeed.Value = 5f;
        }
        networkGeneTier.Value = playerData.gene_tier;

        Debug.Log($"[NetworkPlayerDataSync] ✓ Loaded {networkCharacterName.Value} | HP={networkHp.Value}/{networkMaxHp.Value} | MP={networkMp.Value}/{networkMaxMp.Value}");
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe
        networkElementType.OnValueChanged -= OnElementTypeChanged;
        networkGender.OnValueChanged -= OnGenderChanged;
        networkCharacterName.OnValueChanged -= OnCharacterNameChanged;
        networkLevel.OnValueChanged -= OnLevelChanged;
        networkHp.OnValueChanged -= OnHpChanged;
        networkMaxHp.OnValueChanged -= OnMaxHpChanged;
        networkAttack.OnValueChanged -= OnAttackChanged;
        networkDefense.OnValueChanged -= OnDefenseChanged;
        networkMoveSpeed.OnValueChanged -= OnMoveSpeedChanged;
        networkGeneTier.OnValueChanged -= OnGeneTierChanged;
        networkMp.OnValueChanged -= OnMpChanged;
        networkMaxMp.OnValueChanged -= OnMaxMpChanged;

        base.OnNetworkDespawn();
    }

    /// <summary>
    /// Server: Load player data từ ServerPlayerDataManager (hoặc GameManager fallback) và set vào NetworkVariable
    /// </summary>
    private void LoadPlayerDataFromGameManager()
    {
        PlayerDataResponse playerData = null;

        // Ưu tiên: Lấy từ ServerPlayerDataManager (server-side, cho tất cả clients)
        if (ServerPlayerDataManager.Instance != null && IsServer)
        {
            ulong clientId = OwnerClientId;
            playerData = ServerPlayerDataManager.Instance.GetPlayerDataForClient(clientId);
        }

        // Fallback: Lấy từ GameManager (cho local player hoặc host)
        if (playerData == null && GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerData = GameManager.Instance.GetPlayerData();
            // Debug.LogWarning("[NetworkPlayerDataSync] Using GameManager fallback for player data");
        }

        if (playerData == null)
        {
            // Debug.LogWarning("[NetworkPlayerDataSync] No player data found! Using default values.");
            return;
        }

        // Set NetworkVariable (chỉ server mới có quyền write)
        networkPlayerId.Value = playerData.player_id;
        networkElementType.Value = (FixedString32Bytes)(playerData.element_type ?? "Fire");
        networkGender.Value = (FixedString32Bytes)(playerData.gender ?? "Male");
        networkCharacterName.Value = (FixedString64Bytes)(playerData.character_name ?? "");
        networkLevel.Value = playerData.level;
        
        // Stats từ final_stats hoặc base_stats
        if (playerData.final_stats != null)
        {
            networkHp.Value        = playerData.final_stats.hp;
            networkMaxHp.Value     = playerData.final_stats.max_hp;
            networkMp.Value        = playerData.final_stats.mp;
            networkMaxMp.Value     = playerData.final_stats.max_mp;
            networkAttack.Value    = playerData.final_stats.attack;
            networkDefense.Value   = playerData.final_stats.defense;
            networkMoveSpeed.Value = playerData.final_stats.move_speed;
        }
        else if (playerData.base_stats != null)
        {
            networkHp.Value        = playerData.base_stats.hp;
            networkMaxHp.Value     = playerData.base_stats.max_hp;
            networkMp.Value        = playerData.base_stats.mp;
            networkMaxMp.Value     = playerData.base_stats.max_mp;
            networkAttack.Value    = playerData.base_stats.attack;
            networkMoveSpeed.Value = 5f;
        }
        networkGeneTier.Value = playerData.gene_tier;

        Debug.Log($"[NetworkPlayerDataSync] Server loaded {networkCharacterName.Value} | HP={networkHp.Value}/{networkMaxHp.Value} | MP={networkMp.Value}/{networkMaxMp.Value}");
    }

    /// <summary>
    /// Apply player data vào PlayerController và các components khác
    /// </summary>
    private void ApplyPlayerData()
    {
        if (playerController == null)
        {
            playerController = GetComponent<PlayerController>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<NetworkPlayerHealth>();
        }

        // Apply stats vào PlayerController
        if (playerController != null && playerController.stats != null)
        {
            playerController.stats.maxHealth = networkMaxHp.Value;
            playerController.stats.baseDamage = networkAttack.Value;
            playerController.stats.moveSpeed = networkMoveSpeed.Value;
        }

        // Apply HP vào NetworkPlayerHealth
        if (playerHealth != null)
        {
            playerHealth.SetMaxHealth(networkMaxHp.Value);
            playerHealth.SetHealth(networkHp.Value);
        }

        // TODO: Apply element_type và gender để thay đổi sprite/visual
        ApplyVisuals();
    }

    /// <summary>
    /// Thay đổi visual (sprite, animator) dựa trên element_type + gender
    /// </summary>
    private void ApplyVisuals()
    {
        // TODO: Implement logic để thay đổi sprite/animator dựa trên element_type + gender
        // Debug.Log($"[NetworkPlayerDataSync] Apply visuals: {networkElementType.Value} - {networkGender.Value}");
    }

    #region NetworkVariable Change Callbacks

    private void OnElementTypeChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        // Debug.Log($"[NetworkPlayerDataSync] Element type changed: {oldValue} → {newValue}");
        ApplyVisuals();
    }

    private void OnGenderChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        // Debug.Log($"[NetworkPlayerDataSync] Gender changed: {oldValue} → {newValue}");
        ApplyVisuals();
    }

    private void OnCharacterNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        // Debug.Log($"[NetworkPlayerDataSync] Character name changed: {oldValue} → {newValue}");
    }

    private void OnLevelChanged(int oldValue, int newValue)
    {
        // Debug.Log($"[NetworkPlayerDataSync] Level changed: {oldValue} → {newValue}");
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        Debug.Log($"[NetworkPlayerDataSync] HP: {oldValue} → {newValue}/{networkMaxHp.Value}");
        if (playerHealth != null)
        {
            playerHealth.SetHealth(newValue);
        }
        if (IsOwner) SyncStatToGameManagerAndUI();
    }

    private void OnMpChanged(int oldValue, int newValue)
    {
        Debug.Log($"[NetworkPlayerDataSync] MP: {oldValue} → {newValue}/{networkMaxMp.Value}");
        if (IsOwner) SyncStatToGameManagerAndUI();
    }

    private void OnMaxMpChanged(int oldValue, int newValue)
    {
        // Max MP changed (e.g. after equipment update)
        if (IsOwner) SyncStatToGameManagerAndUI();
    }

    private void OnMaxHpChanged(int oldValue, int newValue)
    {
        if (playerController != null && playerController.stats != null)
        {
            playerController.stats.maxHealth = newValue;
        }
        if (playerHealth != null)
        {
            playerHealth.SetMaxHealth(newValue);
        }
        if (IsOwner) SyncStatToGameManagerAndUI();
    }

    private void OnAttackChanged(int oldValue, int newValue)
    {
        if (playerController != null && playerController.stats != null)
            playerController.stats.baseDamage = newValue;
        if (IsOwner) SyncStatToGameManagerAndUI();
    }

    private void OnDefenseChanged(int oldValue, int newValue)
    {
        // No PlayerStats field for defense currently — only update UI
        if (IsOwner) SyncStatToGameManagerAndUI();
    }

    private void OnMoveSpeedChanged(float oldValue, float newValue)
    {
        if (playerController != null && playerController.stats != null)
            playerController.stats.moveSpeed = newValue;
        if (IsOwner) SyncStatToGameManagerAndUI();
    }

    private void OnGeneTierChanged(int oldValue, int newValue)
    {
        // Gene tier change: update GameManager so StatsTabUI shows correct tier
        if (IsOwner)
        {
            var pd = GameManager.Instance?.currentPlayerData;
            if (pd != null) pd.gene_tier = newValue;
            SyncStatToGameManagerAndUI();
        }
    }

    /// <summary>
    /// Owner-only: đồng bộ NetworkVariable hiện tại vào GameManager.currentPlayerData rồi refresh StatsTabUI.
    /// Được gọi khi bất kỳ stat NetworkVariable nào thay đổi.
    /// </summary>
    private void SyncStatToGameManagerAndUI()
    {
        var pd = GameManager.Instance?.currentPlayerData;
        if (pd == null) return;

        // Đảm bảo final_stats object tồn tại
        if (pd.final_stats == null) pd.final_stats = new FinalStats();

        pd.final_stats.hp         = networkHp.Value;
        pd.final_stats.max_hp     = networkMaxHp.Value;
        pd.final_stats.mp         = networkMp.Value;
        pd.final_stats.max_mp     = networkMaxMp.Value;
        pd.final_stats.attack     = networkAttack.Value;
        pd.final_stats.defense    = networkDefense.Value;
        pd.final_stats.move_speed = networkMoveSpeed.Value;

        // Refresh StatsTabUI nếu đang mở
        FindObjectOfType<StatsTabUI>()?.Load();
    }

    #endregion

    /// <summary>
    /// ServerRpc để client request update player data (khi level up, stats change, etc.)
    /// </summary>
    /// <summary>
    /// Client gọi RPC này sau khi upgrade gene/trang bị thành công.
    /// Host nhận được, ghi vào NetworkVariable → tự động sync sang tất cả client.
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void UpdatePlayerDataServerRpc(int playerId, string elementType, string gender, string characterName,
        int level, int hp, int maxHp, int mp, int maxMp, int attack, int defense, float moveSpeed, int geneTier)
    {
        networkPlayerId.Value      = playerId;
        networkElementType.Value   = (FixedString32Bytes)elementType;
        networkGender.Value        = (FixedString32Bytes)gender;
        networkCharacterName.Value = (FixedString64Bytes)characterName;
        networkLevel.Value         = level;
        networkHp.Value            = hp;
        networkMaxHp.Value         = maxHp;
        networkMp.Value            = mp;
        networkMaxMp.Value         = maxMp;
        networkAttack.Value        = attack;
        networkDefense.Value       = defense;
        networkMoveSpeed.Value     = moveSpeed;
        networkGeneTier.Value      = geneTier;
        Debug.Log($"[NetworkPlayerDataSync] ServerRpc: stats updated → atk={attack} def={defense} maxHp={maxHp} spd={moveSpeed} tier={geneTier}");
    }

    /// <summary>
    /// ServerRpc nhẹ: chỉ cập nhật Max HP / Max MP (dùng sau khi HpBuff / MpBuff áp dụng).
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void UpdateMaxHpMpServerRpc(int newMaxHp, int newMaxMp, ServerRpcParams rpcParams = default)
    {
        if (newMaxHp > 0) networkMaxHp.Value = newMaxHp;
        if (newMaxMp > 0) networkMaxMp.Value = newMaxMp;
        Debug.Log($"[NetworkPlayerDataSync] UpdateMaxHpMp → maxHp={newMaxHp} maxMp={newMaxMp}");
    }

    /// <summary>
    /// Get player data (để hiển thị trong UI, name tag, etc.)
    /// </summary>
    public string GetCharacterName() => networkCharacterName.Value.ToString();
    public string GetElementType() => networkElementType.Value.ToString();
    public string GetGender() => networkGender.Value.ToString();
    public int GetLevel() => networkLevel.Value;

    // ══════════════════════════════════════════════════════════════════════════
    //  MP / HP Consume & Restore (dùng bởi PlayerSkillManager & PotionUsage)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Trừ MP khi dùng skill. Chỉ owner gọi.</summary>
    [ServerRpc(RequireOwnership = true)]
    public void ConsumeMpServerRpc(int amount)
    {
        networkMp.Value = Mathf.Max(0, networkMp.Value - amount);
        Debug.Log($"[NetworkPlayerDataSync] ConsumeMp {amount} → MP={networkMp.Value}/{networkMaxMp.Value}");
    }

    /// <summary>Hồi MP (bình mana). Chỉ owner gọi.</summary>
    [ServerRpc(RequireOwnership = true)]
    public void RestoreMpServerRpc(int amount)
    {
        networkMp.Value = Mathf.Min(networkMaxMp.Value, networkMp.Value + amount);
        Debug.Log($"[NetworkPlayerDataSync] RestoreMp {amount} → MP={networkMp.Value}/{networkMaxMp.Value}");
    }

    /// <summary>Hồi HP (bình máu). Chỉ owner gọi.</summary>
    [ServerRpc(RequireOwnership = true)]
    public void RestoreHpServerRpc(int amount)
    {
        networkHp.Value = Mathf.Min(networkMaxHp.Value, networkHp.Value + amount);
        Debug.Log($"[NetworkPlayerDataSync] RestoreHp {amount} → HP={networkHp.Value}/{networkMaxHp.Value}");
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  EXP AWARD (gọi từ NetworkEnemyHealth khi quái chết)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Server-only: Cộng EXP cho player này và lưu vào DB.
    /// Được gọi bởi NetworkEnemyHealth.HandleDeath() khi player kill quái.
    /// </summary>
    public void AwardExpOnServer(int expAmount)
    {
        if (!IsServer) return;
        int playerId = networkPlayerId.Value;
        if (playerId <= 0 || expAmount <= 0) return;

        Debug.Log($"[NetworkPlayerDataSync] AwardExp +{expAmount} EXP cho playerId={playerId} (clientId={OwnerClientId})");
        StartCoroutine(GainExpCoroutine(playerId, expAmount));
    }

    [System.Serializable]
    private class GainExpResponse
    {
        public bool success;
        public int experience;
        public int level;
        public bool leveled_up;
    }

    private System.Collections.IEnumerator GainExpCoroutine(int playerId, int expAmount)
    {
        // Áp dụng ExpBuff + PhucBuff trước khi gửi lên REST API
        float expPct  = networkExpBonusPct.Value  / 100f;
        float phucPct = networkPhucBonusPct.Value / 100f;
        if (expPct + phucPct > 0f)
            expAmount = Mathf.RoundToInt(expAmount * (1f + expPct + phucPct));

        string baseUrl = APIClient.Instance != null ? APIClient.Instance.baseURL : "http://localhost:5000/api";
        string url = $"{baseUrl}/player/{playerId}/gain-exp";
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes($"{{\"amount\":{expAmount}}}");

        using var req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
        req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.Log($"[NetworkPlayerDataSync] GainExp OK: {req.downloadHandler.text}");

            // Parse JSON response để lấy level mới
            var resp = JsonUtility.FromJson<GainExpResponse>(req.downloadHandler.text);

            // Cập nhật networkLevel.Value trên server → tự động sync về tất cả clients
            if (resp != null && resp.level > 0 && resp.level != networkLevel.Value)
            {
                networkLevel.Value = resp.level;
                Debug.Log($"[NetworkPlayerDataSync] networkLevel cập nhật → {resp.level}");
            }

            int newLevel  = resp != null ? resp.level : networkLevel.Value;
            bool leveledUp = resp != null && resp.leveled_up;

            // Thông báo cho owner client để cập nhật UI
            NotifyExpGainClientRpc(expAmount, newLevel, leveledUp, new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
            });
        }
        else
        {
            Debug.LogError($"[NetworkPlayerDataSync] GainExp FAIL playerId={playerId}: {req.downloadHandler?.text ?? req.error}");
        }
    }

    /// <summary>ClientRpc: thông báo owner client nhận EXP để refresh UI.</summary>
    [ClientRpc]
    private void NotifyExpGainClientRpc(int expAmount, int newLevel, bool leveledUp, ClientRpcParams rpcParams = default)
    {
        if (leveledUp)
            Debug.Log($"[NetworkPlayerDataSync] LEVEL UP! Level {newLevel}! (+{expAmount} EXP)");
        else
            Debug.Log($"[NetworkPlayerDataSync] +{expAmount} EXP từ kill quái! Level={newLevel}");
        // Refresh stats UI nếu đang mở
        if (IsOwner)
            FindObjectOfType<StatsTabUI>()?.Load();
    }
}
