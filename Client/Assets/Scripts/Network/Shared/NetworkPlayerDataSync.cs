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
    public NetworkVariable<float> networkMoveSpeed = new NetworkVariable<float>(5f);

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
        networkMoveSpeed.OnValueChanged += OnMoveSpeedChanged;

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
            networkHp.Value = playerData.final_stats.hp;
            networkMaxHp.Value = playerData.final_stats.max_hp;
            networkMp.Value = playerData.final_stats.mp;
            networkMaxMp.Value = playerData.final_stats.max_mp;
            networkAttack.Value = playerData.final_stats.attack;
            networkMoveSpeed.Value = playerData.final_stats.move_speed;
        }
        else if (playerData.base_stats != null)
        {
            networkHp.Value = playerData.base_stats.hp;
            networkMaxHp.Value = playerData.base_stats.max_hp;
            networkMp.Value = playerData.base_stats.mp;
            networkMaxMp.Value = playerData.base_stats.max_mp;
            networkAttack.Value = playerData.base_stats.attack;
            networkMoveSpeed.Value = 5f; // Default move speed
        }

        Debug.Log($"[NetworkPlayerDataSync] ✓ NetworkVariables updated for player: {networkCharacterName.Value}");
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
        networkMoveSpeed.OnValueChanged -= OnMoveSpeedChanged;

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
            networkHp.Value = playerData.final_stats.hp;
            networkMaxHp.Value = playerData.final_stats.max_hp;
            networkMp.Value = playerData.final_stats.mp;
            networkMaxMp.Value = playerData.final_stats.max_mp;
            networkAttack.Value = playerData.final_stats.attack;
            networkMoveSpeed.Value = playerData.final_stats.move_speed;
        }
        else if (playerData.base_stats != null)
        {
            networkHp.Value = playerData.base_stats.hp;
            networkMaxHp.Value = playerData.base_stats.max_hp;
            networkMp.Value = playerData.base_stats.mp;
            networkMaxMp.Value = playerData.base_stats.max_mp;
            networkAttack.Value = playerData.base_stats.attack;
            networkMoveSpeed.Value = 5f; // Default move speed
        }

        // Debug.Log($"[NetworkPlayerDataSync] Server loaded player data: {networkCharacterName.Value} ({networkElementType.Value} - {networkGender.Value}), Level {networkLevel.Value}");
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
        if (playerHealth != null)
        {
            playerHealth.SetHealth(newValue);
        }
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
    }

    private void OnAttackChanged(int oldValue, int newValue)
    {
        if (playerController != null && playerController.stats != null)
        {
            playerController.stats.baseDamage = newValue;
        }
    }

    private void OnMoveSpeedChanged(float oldValue, float newValue)
    {
        if (playerController != null && playerController.stats != null)
        {
            playerController.stats.moveSpeed = newValue;
        }
    }

    #endregion

    /// <summary>
    /// ServerRpc để client request update player data (khi level up, stats change, etc.)
    /// </summary>
    [ServerRpc(RequireOwnership = true)]
    public void UpdatePlayerDataServerRpc(int playerId, string elementType, string gender, string characterName, 
        int level, int hp, int maxHp, int mp, int maxMp, int attack, float moveSpeed)
    {
        networkPlayerId.Value = playerId;
        networkElementType.Value = (FixedString32Bytes)elementType;
        networkGender.Value = (FixedString32Bytes)gender;
        networkCharacterName.Value = (FixedString64Bytes)characterName;
        networkLevel.Value = level;
        networkHp.Value = hp;
        networkMaxHp.Value = maxHp;
        networkMp.Value = mp;
        networkMaxMp.Value = maxMp;
        networkAttack.Value = attack;
        networkMoveSpeed.Value = moveSpeed;
    }

    /// <summary>
    /// Get player data (để hiển thị trong UI, name tag, etc.)
    /// </summary>
    public string GetCharacterName() => networkCharacterName.Value.ToString();
    public string GetElementType() => networkElementType.Value.ToString();
    public string GetGender() => networkGender.Value.ToString();
    public int GetLevel() => networkLevel.Value;
}
