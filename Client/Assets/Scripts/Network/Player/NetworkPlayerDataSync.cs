using Unity.Netcode;
using UnityEngine;
using Unity.Collections;

// Đồng bộ player data (element_type, gender, character_name, stats) từ API qua NetworkVariable
// Shared script - dùng cho cả client và server
public class NetworkPlayerDataSync : NetworkBehaviour, IPlayerDataReceiver
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

    // Gene Tối Thượng (Ultimate Gene)
    // True khi player đã kích hoạt Gene Tối Thượng (hiển thị aura sau lưng).
    public NetworkVariable<bool> networkIsUltimate = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // Resources path của aura prefab gắn sau lưng khi Ultimate kích hoạt.
    public NetworkVariable<FixedString128Bytes> networkUltimateAuraPath = new NetworkVariable<FixedString128Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Buff stat modifiers (set by server from ActiveBuff)
    // % bonus EXP gene nạp vào (e.g. 20 = +20%). Set bởi server khi dùng GeneExpBuff item.
    public NetworkVariable<int> networkGeneExpBonusPct  = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // % bonus EXP khi kill enemy (e.g. 25 = +25%).
    public NetworkVariable<int> networkExpBonusPct      = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // % bonus vàng và EXP drop (Phúc buff).
    public NetworkVariable<int> networkPhucBonusPct     = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // % tăng sát thương (AttackBuff).
    public NetworkVariable<int> networkAttackBonusPct   = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    // % giảm sát thương nhận (DefenseBuff).
    public NetworkVariable<int> networkDefenseBonusPct  = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // ID nhóm (party) mà player đang ở. Rỗng nếu không có nhóm.
    // Dùng server-side để kiểm tra 2 player có cùng nhóm không trước khi áp buff đồng đội.
    public NetworkVariable<FixedString64Bytes> networkPartyId = new NetworkVariable<FixedString64Bytes>(
        default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("References")]
    private PlayerController playerController;
    private NetworkPlayerHealth playerHealth;
    private UltimateAuraVisual ultimateAura;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        { /* OnNetworkSpawn {BuildNetworkIdentity()} state={BuildStateSnapshot()} */ }

        if (!IsServer && IsOwner)
        {
            { /* *** CLIENT RECEIVED OWN PLAYER OBJECT *** */ }
        }

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

        // Gene Tối Thượng: hiển thị / ẩn aura khi cờ thay đổi
        networkIsUltimate.OnValueChanged += OnUltimateChanged;
        networkUltimateAuraPath.OnValueChanged += OnUltimateAuraPathChanged;
        // Apply trạng thái ban đầu (trường hợp player đã là Ultimate khi vừa spawn)
        RefreshUltimateAura();
        if (IsOwner)
        {
            var pm = PartyManager.Instance;
            if (pm != null)
            {
                pm.OnPartyStateChanged += OnPartyStateChanged_Sync;
                // Sync ngay giá trị hiện tại nếu đang có party
                if (pm.HasParty && !string.IsNullOrEmpty(pm.CurrentParty?.partyId))
                    SyncPartyIdServerRpc(pm.CurrentParty.partyId);
            }
        }

        // Apply data ngay lập tức
        ApplyPlayerData();
        { /* OnNetworkSpawn applied initial data {BuildNetworkIdentity()} state={BuildStateSnapshot()} */ }
    }

    // Client: Gửi auth (userId + token) lên server ngay khi player spawn
    private void SendAuthToServer()
    {
        string token = AuthHelper.GetToken();
        int userId = PlayerPrefs.GetInt("USER_ID", 0);
        int geneSlot = PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1);

        if (string.IsNullOrEmpty(token) || userId == 0)
        {
            { /* Lỗi: ✗ Cannot send auth - JWT_TOKEN or USER_ID not found */ }
            { /* Lỗi: Token empty: {string.IsNullOrEmpty(token)}, UserId: {userId} */ }
            return;
        }

        { /* ===== SENDING AUTH TO SERVER ===== */ }
        { /* UserId: {userId} */ }
        { /* Token length: {token.Length} */ }
        { /* OwnerClientId: {OwnerClientId} */ }
        { /* Calling SendAuthServerRpc */ }

        SendAuthServerRpc(token, userId, geneSlot);
    }

    // ServerRpc: Nhận auth từ client và load player data
    [ServerRpc(RequireOwnership = true)]
    private void SendAuthServerRpc(string token, int userId, int geneSlot, ServerRpcParams rpcParams = default)
    {
        var senderClientId = rpcParams.Receive.SenderClientId;

        { /* \n\n\n */ }
        { /* █████████████████████████████████████████████████████ */ }
        { /* █████████████████████████████████████████████████████ */ }
        { /* ███ 🎯 AUTH SERVERRPC RECEIVED ON HOST!!! 🎯 ███ */ }
        { /* █████████████████████████████████████████████████████ */ }
        { /* █████████████████████████████████████████████████████ */ }
        { /* Time: {Time.time} */ }
        { /* Frame: {Time.frameCount} */ }
        { /* SenderClientId: {senderClientId} */ }
        { /* UserId: {userId} */ }
        { /* Token length: {token?.Length ?? 0} */ }

        // Load player data từ API
        if (ServerPlayerDataManager.Instance != null)
        {
            { /* ===== CALLING SERVERPLAYERDATAMANAGER ===== */ }
            { /* Parameters - ClientId: {senderClientId}, UserId: {userId} */ }

            ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                senderClientId,
                userId,
                onSuccess: (playerData) =>
                {
                    { /* ===== PLAYER DATA LOADED SUCCESSFULLY ===== */ }
                    { /* ✓ ClientId: {senderClientId} */ }
                    { /* ✓ Character: {playerData.character_name} */ }
                    { /* ✓ Element: {playerData.element_type} */ }
                    { /* ✓ Gender: {playerData.gender} */ }
                    { /* ✓ Level: {playerData.level} */ }

                    // Update NetworkVariables với player data mới load
                    UpdateNetworkVariablesFromPlayerData(playerData);
                },
                onError: (error) =>
                {
                    { /* Lỗi: ===== FAILED TO LOAD PLAYER DATA ===== */ }
                    { /* Lỗi: ✗ ClientId: {senderClientId} */ }
                    { /* Lỗi: ✗ UserId: {userId} */ }
                    { /* Lỗi: ✗ Error: {error} */ }
                },
                geneSlot: geneSlot
            );
        }
        else
        {
            { /* Lỗi: ===== SERVERPLAYERDATAMANAGER IS NULL ===== */ }
            { /* Lỗi: ✗ Cannot load player data for clientId: {senderClientId}, userId: {userId} */ }
        }
    }

    // Server: Update NetworkVariables từ PlayerDataResponse
    private void UpdateNetworkVariablesFromPlayerData(PlayerDataResponse playerData)
    {
        if (playerData == null)
        {
            { /* Cảnh báo: UpdateNetworkVariablesFromPlayerData ignored null data. {BuildNetworkIdentity()} */ }
            return;
        }

        { /* UpdateNetworkVariablesFromPlayerData {DescribeIncomingData(playerData)} | {BuildNetworkIdentity()} */ }

        networkPlayerId.Value = playerData.player_id;
        networkElementType.Value = (FixedString32Bytes)(playerData.element_type ?? "Fire");
        networkGender.Value = (FixedString32Bytes)(playerData.gender ?? "Male");
        networkCharacterName.Value = (FixedString64Bytes)(playerData.character_name ?? "");
        networkLevel.Value = playerData.level;

        // Stats từ final_stats hoặc base_stats
        if (playerData.final_stats != null)
        {
            // Set max BEFORE current so Clamp in SetHealth uses the correct ceiling
            networkMaxHp.Value     = playerData.final_stats.max_hp;
            networkHp.Value        = playerData.final_stats.hp;
            networkMaxMp.Value     = playerData.final_stats.max_mp;
            networkMp.Value        = playerData.final_stats.mp;
            networkAttack.Value    = playerData.final_stats.attack;
            networkDefense.Value   = playerData.final_stats.defense;
            networkMoveSpeed.Value = playerData.final_stats.move_speed;
        }
        else if (playerData.base_stats != null)
        {
            networkMaxHp.Value     = playerData.base_stats.max_hp;
            networkHp.Value        = playerData.base_stats.hp;
            networkMaxMp.Value     = playerData.base_stats.max_mp;
            networkMp.Value        = playerData.base_stats.mp;
            networkAttack.Value    = playerData.base_stats.attack;
            networkMoveSpeed.Value = 5f;
        }
        networkGeneTier.Value = playerData.gene_tier;
        networkIsUltimate.Value = playerData.is_ultimate;
        networkUltimateAuraPath.Value = (FixedString128Bytes)(playerData.ultimate_aura_path ?? "");

        { /* ✓ Loaded {networkCharacterName.Value} | HP={networkHp.Value}/{networkMaxHp.Value} | MP={networkMp.Value}/{networkMaxMp.Value} */ }
    }

    public override void OnNetworkDespawn()
    {
        { /* OnNetworkDespawn {BuildNetworkIdentity()} state={BuildStateSnapshot()} */ }

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

        // Gene Tối Thượng
        networkIsUltimate.OnValueChanged -= OnUltimateChanged;
        networkUltimateAuraPath.OnValueChanged -= OnUltimateAuraPathChanged;
        if (IsOwner)
        {
            var pm = PartyManager.Instance;
            if (pm != null)
                pm.OnPartyStateChanged -= OnPartyStateChanged_Sync;
        }

        base.OnNetworkDespawn();
    }

    // Server: Load player data từ ServerPlayerDataManager (hoặc GameManager fallback) và set vào NetworkVariable
    private void LoadPlayerDataFromGameManager()
    {
        PlayerDataResponse playerData = null;
        string dataSource = null;

        // Ưu tiên: Lấy từ ServerPlayerDataManager (server-side, cho tất cả clients)
        if (ServerPlayerDataManager.Instance != null && IsServer)
        {
            ulong clientId = OwnerClientId;
            playerData = ServerPlayerDataManager.Instance.GetPlayerDataForClient(clientId);
            if (playerData != null)
                dataSource = $"ServerPlayerDataManager(clientId={clientId})";
        }

        // Fallback: Lấy từ GameManager (cho local player hoặc host)
        if (playerData == null && GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerData = GameManager.Instance.GetPlayerData();
            if (playerData != null)
                dataSource = "GameManager fallback";
        }

        if (playerData == null)
        {
            { /* Cảnh báo: LoadPlayerDataFromGameManager: không tìm thấy player data. {BuildNetworkIdentity()} */ }
            return;
        }

        { /* LoadPlayerDataFromGameManager source={dataSource}: {DescribeIncomingData(playerData)} | {BuildNetworkIdentity()} */ }

        // Set NetworkVariable (chỉ server mới có quyền write)
        networkPlayerId.Value = playerData.player_id;
        networkElementType.Value = (FixedString32Bytes)(playerData.element_type ?? "Fire");
        networkGender.Value = (FixedString32Bytes)(playerData.gender ?? "Male");
        networkCharacterName.Value = (FixedString64Bytes)(playerData.character_name ?? "");
        networkLevel.Value = playerData.level;
        
        // Stats từ final_stats hoặc base_stats
        // IMPORTANT: set max before current so OnHpChanged → SetHealth uses correct ceiling
        if (playerData.final_stats != null)
        {
            networkMaxHp.Value     = playerData.final_stats.max_hp;
            networkHp.Value        = playerData.final_stats.hp;
            networkMaxMp.Value     = playerData.final_stats.max_mp;
            networkMp.Value        = playerData.final_stats.mp;
            networkAttack.Value    = playerData.final_stats.attack;
            networkDefense.Value   = playerData.final_stats.defense;
            networkMoveSpeed.Value = playerData.final_stats.move_speed;
        }
        else if (playerData.base_stats != null)
        {
            networkMaxHp.Value     = playerData.base_stats.max_hp;
            networkHp.Value        = playerData.base_stats.hp;
            networkMaxMp.Value     = playerData.base_stats.max_mp;
            networkMp.Value        = playerData.base_stats.mp;
            networkAttack.Value    = playerData.base_stats.attack;
            networkMoveSpeed.Value = 5f;
        }
        networkGeneTier.Value = playerData.gene_tier;
        networkIsUltimate.Value = playerData.is_ultimate;
        networkUltimateAuraPath.Value = (FixedString128Bytes)(playerData.ultimate_aura_path ?? "");

        { /* Server loaded {networkCharacterName.Value} | HP={networkHp.Value}/{networkMaxHp.Value} | MP={networkMp.Value}/{networkMaxMp.Value} */ }
    }

    // IPlayerDataReceiver — gọi bởi ZonePlayerSessionManager ngay sau khi spawn.
    // Đẩy data từ API trực tiếp vào NetworkVariable mà không cần ServerPlayerDataManager.
    public void OnPlayerDataLoaded(PlayerDataResponse data, ulong clientId)
    {
        if (!IsServer)
        {
            { /* Cảnh báo: OnPlayerDataLoaded bị gọi ngoài server. {BuildNetworkIdentity()} */ }
            return;
        }

        if (data == null)
        {
            { /* Cảnh báo: OnPlayerDataLoaded nhận data=null. {BuildNetworkIdentity()} */ }
            return;
        }

        { /* OnPlayerDataLoaded received for clientId={clientId}: {DescribeIncomingData(data)} | {BuildNetworkIdentity()} */ }

        networkPlayerId.Value      = data.player_id;
        networkElementType.Value   = (Unity.Collections.FixedString32Bytes)(data.element_type ?? "Fire");
        networkGender.Value        = (Unity.Collections.FixedString32Bytes)(data.gender ?? "Male");
        networkCharacterName.Value = (Unity.Collections.FixedString64Bytes)(data.character_name ?? "");
        networkLevel.Value         = data.level;
        networkGeneTier.Value      = data.gene_tier;
        networkIsUltimate.Value    = data.is_ultimate;
        networkUltimateAuraPath.Value = (Unity.Collections.FixedString128Bytes)(data.ultimate_aura_path ?? "");

        // Stats — ưu tiên final_stats (đã có buff), fallback flat fields
        networkMaxHp.Value     = data.GetMaxHp();
        networkHp.Value        = data.GetHp();
        networkMaxMp.Value     = data.GetMaxMp();
        networkMp.Value        = data.GetMp();
        networkAttack.Value    = data.GetAttack();
        networkDefense.Value   = data.GetDefense();
        networkMoveSpeed.Value = data.GetMoveSpeed();

        // Sync vào NetworkPlayerHealth
        var nph = GetComponent<NetworkPlayerHealth>();
        if (nph != null)
        {
            nph.SetMaxHealth(networkMaxHp.Value);
            nph.SetHealth(networkHp.Value);
        }

        { /* IPlayerDataReceiver → {data.character_name} HP={networkHp.Value}/{networkMaxHp.Value} MP={networkMp.Value}/{networkMaxMp.Value} ATK={networkAttack.Value} */ }
    }

    // Apply player data vào PlayerController và các components khác
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

        // Apply HP vào NetworkPlayerHealth — only on server to avoid stale-default RPCs from client
        if (IsServer && playerHealth != null)
        {
            playerHealth.SetMaxHealth(networkMaxHp.Value);
            playerHealth.SetHealth(networkHp.Value);
        }

        // TODO: Apply element_type và gender để thay đổi sprite/visual
        ApplyVisuals();

        { /* ApplyPlayerData complete. hasPlayerController={playerController != null}, hasStats={playerController?.stats != null}, hasPlayerHealth={playerHealth != null} | {BuildNetworkIdentity()} | state={BuildStateSnapshot()} */ }
    }

    // Thay đổi visual (sprite, animator) dựa trên element_type + gender
    private void ApplyVisuals()
    {
        { /* ApplyVisuals element={networkElementType.Value}, gender={networkGender.Value}, character={networkCharacterName.Value} | {BuildNetworkIdentity()} */ }
    }

    #region NetworkVariable Change Callbacks

    private void OnElementTypeChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        { /* Element type changed: {oldValue} → {newValue} | {BuildNetworkIdentity()} */ }
        ApplyVisuals();
        // Đổi hệ → cập nhật aura Tối Thượng theo hệ mới
        RefreshUltimateAura();
    }

    private void OnGenderChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
    {
        { /* Gender changed: {oldValue} → {newValue} | {BuildNetworkIdentity()} */ }
        ApplyVisuals();
    }

    private void OnCharacterNameChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
    {
        { /* Character name changed: {oldValue} → {newValue} | {BuildNetworkIdentity()} */ }
    }

    private void OnLevelChanged(int oldValue, int newValue)
    {
        { /* Level changed: {oldValue} → {newValue} | {BuildNetworkIdentity()} */ }
    }

    private string BuildNetworkIdentity()
    {
        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
        bool isLocalPlayer = NetworkObject != null && NetworkObject.IsLocalPlayer;
        bool isPlayerObject = NetworkObject != null && NetworkObject.IsPlayerObject;
        return $"obj={gameObject.name}, scene={gameObject.scene.name}, netId={NetworkObjectId}, owner={OwnerClientId}, localClient={localClientId}, isServer={IsServer}, isClient={IsClient}, isOwner={IsOwner}, isLocalPlayer={isLocalPlayer}, isPlayerObject={isPlayerObject}";
    }

    private string BuildStateSnapshot()
    {
        return $"playerId={networkPlayerId.Value}, name={networkCharacterName.Value}, element={networkElementType.Value}, gender={networkGender.Value}, level={networkLevel.Value}, hp={networkHp.Value}/{networkMaxHp.Value}, mp={networkMp.Value}/{networkMaxMp.Value}, atk={networkAttack.Value}, def={networkDefense.Value}, speed={networkMoveSpeed.Value}, geneTier={networkGeneTier.Value}";
    }

    private static string DescribeIncomingData(PlayerDataResponse data)
    {
        if (data == null)
            return "(null PlayerDataResponse)";

        return $"playerId={data.player_id}, name={data.character_name}, element={data.element_type}, gender={data.gender}, level={data.level}, hybrid={data.is_hybrid}, hybridPath={data.hybrid_prefab_path}, hp={data.GetHp()}/{data.GetMaxHp()}, mp={data.GetMp()}/{data.GetMaxMp()}, atk={data.GetAttack()}, def={data.GetDefense()}, move={data.GetMoveSpeed()}, map={data.map_id}, zone={data.zone_id}";
    }

    private void OnHpChanged(int oldValue, int newValue)
    {
        { /* HP: {oldValue} → {newValue}/{networkMaxHp.Value} */ }
        // Only server should write to NetworkPlayerHealth — clients must not send SetHealthServerRpc
        // with stale default values (which would overwrite correct server state)
        if (IsServer && playerHealth != null)
        {
            playerHealth.SetHealth(newValue);
        }
        if (IsOwner) SyncStatToGameManagerAndUI();
    }

    private void OnMpChanged(int oldValue, int newValue)
    {
        { /* MP: {oldValue} → {newValue}/{networkMaxMp.Value} */ }
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
        // Only server should write to NetworkPlayerHealth — clients must not send SetMaxHealthServerRpc
        // with stale default values (which would overwrite correct server state)
        if (IsServer && playerHealth != null)
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

    // Gene Tối Thượng handlers
    private void OnUltimateChanged(bool oldValue, bool newValue)
    {
        RefreshUltimateAura();

        // Owner: cập nhật GameManager để UI hiển thị trạng thái Tối Thượng
        if (IsOwner)
        {
            var pd = GameManager.Instance?.currentPlayerData;
            if (pd != null) pd.is_ultimate = newValue;
        }
    }

    private void OnUltimateAuraPathChanged(FixedString128Bytes oldValue, FixedString128Bytes newValue)
    {
        RefreshUltimateAura();
    }

    // Bật/tắt aura Gene Tối Thượng dựa trên NetworkVariable hiện tại.
    // Chạy trên mọi client (host + remote) nên ai cũng thấy aura.
    private void RefreshUltimateAura()
    {
        if (ultimateAura == null)
            ultimateAura = GetComponent<UltimateAuraVisual>();

        if (ultimateAura == null)
            ultimateAura = gameObject.AddComponent<UltimateAuraVisual>();

        ultimateAura.Apply(
            networkIsUltimate.Value,
            networkElementType.Value.ToString(),
            networkUltimateAuraPath.Value.ToString());
    }

    // Owner-only: đồng bộ NetworkVariable hiện tại vào GameManager.currentPlayerData rồi refresh StatsTabUI.
    // Được gọi khi bất kỳ stat NetworkVariable nào thay đổi.
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

    // ServerRpc để client request update player data (khi level up, stats change, etc.)
    // Client gọi RPC này sau khi upgrade gene/trang bị thành công.
    // Host nhận được, ghi vào NetworkVariable → tự động sync sang tất cả client.
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
        { /* ServerRpc: stats updated → atk={attack} def={defense} maxHp={maxHp} spd={moveSpeed} tier={geneTier} */ }
    }

    // ServerRpc nhẹ: chỉ cập nhật Max HP / Max MP (dùng sau khi HpBuff / MpBuff áp dụng).
    [ServerRpc(RequireOwnership = false)]
    public void UpdateMaxHpMpServerRpc(int newMaxHp, int newMaxMp, ServerRpcParams rpcParams = default)
    {
        if (newMaxHp > 0) networkMaxHp.Value = newMaxHp;
        if (newMaxMp > 0) networkMaxMp.Value = newMaxMp;

        // Đồng bộ maxHealth sang NetworkPlayerHealth để HP bar hiển thị đúng
        if (newMaxHp > 0)
        {
            var nph = GetComponent<NetworkPlayerHealth>();
            if (nph != null) nph.SetMaxHealth(newMaxHp);
        }

        { /* UpdateMaxHpMp → maxHp={newMaxHp} maxMp={newMaxMp} */ }
    }

    // Get player data (để hiển thị trong UI, name tag, etc.)
    public string GetCharacterName() => networkCharacterName.Value.ToString();
    public string GetElementType() => networkElementType.Value.ToString();
    public string GetGender() => networkGender.Value.ToString();
    public int GetLevel() => networkLevel.Value;

    //  MP / HP Consume & Restore (dùng bởi PlayerSkillManager & PotionUsage)

    // Trừ MP khi dùng skill. Chỉ owner gọi.
    [ServerRpc(RequireOwnership = true)]
    public void ConsumeMpServerRpc(int amount)
    {
        networkMp.Value = Mathf.Max(0, networkMp.Value - amount);
        { /* ConsumeMp {amount} → MP={networkMp.Value}/{networkMaxMp.Value} */ }
    }

    // Hồi MP (bình mana). Chỉ owner gọi.
    [ServerRpc(RequireOwnership = true)]
    public void RestoreMpServerRpc(int amount)
    {
        networkMp.Value = Mathf.Min(networkMaxMp.Value, networkMp.Value + amount);
        { /* RestoreMp {amount} → MP={networkMp.Value}/{networkMaxMp.Value} */ }
    }

    // Hồi HP (bình máu). Chỉ owner gọi.
    [ServerRpc(RequireOwnership = true)]
    public void RestoreHpServerRpc(int amount)
    {
        networkHp.Value = Mathf.Min(networkMaxHp.Value, networkHp.Value + amount);
        { /* RestoreHp {amount} → HP={networkHp.Value}/{networkMaxHp.Value} */ }
    }

    //  EXP AWARD (gọi từ NetworkEnemyHealth khi quái chết)

    // Server-only: Cộng EXP cho player này và lưu vào DB.
    // Được gọi bởi NetworkEnemyHealth.HandleDeath() khi player kill quái.
    public void AwardExpOnServer(int expAmount, int enemyMaxHp = 0)
    {
        if (!IsServer) return;
        int playerId = networkPlayerId.Value;
        if (playerId <= 0 || expAmount <= 0) return;

        int ultimateExp = enemyMaxHp > 0 ? enemyMaxHp : expAmount;
        { /* AwardExp +{expAmount} EXP, ultimateExp={ultimateExp} cho playerId={playerId} (clientId={OwnerClientId}) */ }
        StartCoroutine(GainExpCoroutine(playerId, expAmount, ultimateExp));
    }

    [System.Serializable]
    private class GainExpResponse
    {
        public bool success;
        public int experience;
        public int level;
        public bool leveled_up;
        // Gene Tối Thượng
        public int  ultimate_gene_exp;
        public bool is_ultimate;
        public bool ultimate_activated;
    }

    private System.Collections.IEnumerator GainExpCoroutine(int playerId, int expAmount, int ultimateExp)
    {
        // Áp dụng ExpBuff + PhucBuff trước khi gửi lên REST API
        float expPct  = networkExpBonusPct.Value  / 100f;
        float phucPct = networkPhucBonusPct.Value / 100f;
        if (expPct + phucPct > 0f)
            expAmount = Mathf.RoundToInt(expAmount * (1f + expPct + phucPct));

        string baseUrl = ServerAddressConfig.Instance != null ? ServerAddressConfig.Instance.ApiUrl : "http://localhost:3000/api";
        string url = $"{baseUrl}/player/{playerId}/gain-exp";
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes($"{{\"amount\":{expAmount},\"ultimate_exp\":{Mathf.Max(0, ultimateExp)}}}");

        using var req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
        req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        string zoneApiKey = ZoneRoomRegistry.Instance?.Config?.GetZoneApiKey()
                            ?? System.Environment.GetEnvironmentVariable("ZONE_API_KEY")
                            ?? "dev-zone-key";
        if (!string.IsNullOrWhiteSpace(zoneApiKey))
            req.SetRequestHeader("X-Zone-Api-Key", zoneApiKey);

        yield return req.SendWebRequest();

        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            { /* GainExp OK: {req.downloadHandler.text} */ }

            // Parse JSON response để lấy level mới
            var resp = JsonUtility.FromJson<GainExpResponse>(req.downloadHandler.text);

            // Cập nhật networkLevel.Value trên server → tự động sync về tất cả clients
            if (resp != null && resp.level > 0 && resp.level != networkLevel.Value)
            {
                networkLevel.Value = resp.level;
                { /* networkLevel cập nhật → {resp.level} */ }
            }

            // Gene Tối Thượng vừa kích hoạt từ việc giết quái → bật aura ngay cho mọi client
            if (resp != null && resp.ultimate_activated && !networkIsUltimate.Value)
            {
                networkIsUltimate.Value = true;
                if (networkUltimateAuraPath.Value.Length == 0)
                    networkUltimateAuraPath.Value = (FixedString128Bytes)"Prefabs/Player/Aura/UltimateAura";
                { /* ✨ Gene Tối Thượng KÍCH HOẠT cho playerId={playerId} */ }
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
            { /* Lỗi: GainExp FAIL playerId={playerId}: {req.downloadHandler?.text ?? req.error} */ }
        }
    }

    // ClientRpc: thông báo owner client nhận EXP để refresh UI.
    [ClientRpc]
    private void NotifyExpGainClientRpc(int expAmount, int newLevel, bool leveledUp, ClientRpcParams rpcParams = default)
    {
        if (leveledUp)
            { /* LEVEL UP! Level {newLevel}! (+{expAmount} EXP) */ }
        else
            { /* +{expAmount} EXP từ kill quái! Level={newLevel} */ }
        // Refresh stats UI nếu đang mở
        if (IsOwner)
            FindObjectOfType<StatsTabUI>()?.Load();
    }

    // Quest Refresh Notify

    // Server gọi sau khi QuestProgressReporter.Report() thành công.
    // Gửi ClientRpc đến owner client để refresh QuestHudWidget.
    public void NotifyQuestKillOnServer()
    {
        NotifyQuestProgressOnServer("kill");
    }

    public void NotifyQuestProgressOnServer(string source = "unknown")
    {
        if (!IsServer) return;
        { /* NotifyQuestProgressOnServer source={source} → gửi ClientRpc đến clientId={OwnerClientId} */ }
        NotifyQuestRefreshClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void NotifyQuestRefreshClientRpc(ClientRpcParams _ = default)
    {
        { /* NotifyQuestRefresh nhận được → RefreshPlayerOverview() */ }
        QuestManager.Instance?.RefreshPlayerOverview();
    }

    // Proximity Chat Bubble

    // Gọi từ owner khi gửi tin lân cận.
    // Broadcast bubble lên tất cả client (mọi người thấy bubble trên đầu nhân vật này).
    [ServerRpc(RequireOwnership = true)]
    public void ShowProximityBubbleServerRpc(FixedString128Bytes senderName, FixedString512Bytes message)
    {
        ShowProximityBubbleClientRpc(senderName, message);
    }

    [ClientRpc]
    private void ShowProximityBubbleClientRpc(FixedString128Bytes senderName, FixedString512Bytes message)
    {
        GetComponentInChildren<ProximityChatBubble>()?.ShowMessage(senderName.ToString(), message.ToString());
    }

    //  Party ID Sync — dùng để server kiểm tra 2 player có cùng nhóm không

    // Owner client gọi khi PartyManager.OnPartyStateChanged fire.
    // Sync partyId của bản thân lên server để server có thể kiểm tra party membership.
    private void OnPartyStateChanged_Sync(PartyStatePayload state)
    {
        string pid = state?.partyId ?? string.Empty;
        SyncPartyIdServerRpc(pid);
    }

    // Server nhận partyId từ owner và set vào networkPartyId.
    [ServerRpc(RequireOwnership = false)]
    public void SyncPartyIdServerRpc(string partyId, ServerRpcParams rpc = default)
    {
        networkPartyId.Value = new FixedString64Bytes(partyId ?? string.Empty);
        { /* SyncPartyId clientId={rpc.Receive.SenderClientId} partyId={partyId} */ }
    }

    // Kiểm tra xem NetworkObject khác có cùng party với object này không.
    // Dùng server-side trước khi áp buff đồng đội.
    // Trả về true nếu cả hai đều có partyId giống nhau (không rỗng).
    public bool IsInSameParty(NetworkObject other)
    {
        if (other == null) return false;
        var otherSync = other.GetComponent<NetworkPlayerDataSync>();
        if (otherSync == null) return false;
        string myId    = networkPartyId.Value.ToString();
        string otherId = otherSync.networkPartyId.Value.ToString();
        return !string.IsNullOrEmpty(myId) && myId == otherId;
    }
}
