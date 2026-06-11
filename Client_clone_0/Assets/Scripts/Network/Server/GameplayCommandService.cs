using System;
using System.Collections;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Networking;

// GameplayCommandService — Server-authoritative gateway cho mọi gameplay action in-game.
// Hybrid Architecture Boundary:
// PRE-GAME  → Client gọi REST trực tiếp (Login/Register/CharSelect)
// IN-GAME   → Client gọi ServerRpc → Server gọi REST → ClientRpc
// Flow cho mỗi action:
// 1. Client UI gọi GameplayCommandService.Instance.XxxServerRpc()
// 2. Server nhận, resolve playerId + JWT từ ZonePlayerSessionManager
// 3. Server gọi GameServerApi (HTTPS) với JWT bearer
// 4. Server gửi kết quả về đúng client qua targeted ClientRpc
// 5. Static C# event fired → UI callback
// Gắn vào: Singleton NetworkObject trong ServerScene (cùng GO với MapWorldBootstrap).
[DisallowMultipleComponent]
public class GameplayCommandService : NetworkBehaviour
{
    public static GameplayCommandService Instance { get; private set; }

    // Static C# events (client-side callbacks)
    // Pattern sử dụng: subscribe → gọi ServerRpc → nhận event → unsubscribe

    public static event Action<string> OnPlayerDataReceived;      // RequestPlayerDataServerRpc

    public static event Action<string> OnSkillsReceived;          // GetPlayerSkillsServerRpc
    public static event Action<string> OnSkillUpgraded;           // UpgradeSkillServerRpc
    // Server đẩy skill data về ngay khi player spawn (pre-cache, không cần client request).
    public static event Action<string> OnInitialSkillsReceived;   // PushSkillsToClient (server-initiated)

    public static event Action<string> OnPotentialReceived;       // GetPlayerPotentialServerRpc
    public static event Action<string> OnPotentialAllocated;      // AllocatePotentialStatsServerRpc

    public static event Action<string> OnEquipmentReceived;       // GetPlayerEquipmentServerRpc
    public static event Action<string> OnEquipResult;             // EquipItemServerRpc
    public static event Action<string> OnUnequipResult;           // UnequipItemServerRpc
    public static event Action<string> OnBagUnequipResult;        // UnequipBagItemServerRpc

    public static event Action<string> OnGeneConfigReceived;      // GetGeneConfigServerRpc
    public static event Action<string> OnGeneUpgraded;            // UpgradeGeneServerRpc

    public static event Action<string> OnUpgradeConfigReceived;   // GetUpgradeConfigServerRpc
    public static event Action<string> OnOptionTemplatesReceived; // GetOptionTemplatesServerRpc
    public static event Action<string> OnEquipmentUpgraded;       // UpgradeEquipmentServerRpc

    public static event Action<string> OnUseItemResult;           // UseInventoryItemServerRpc
    public static event Action<string> OnRemoveItemResult;        // RemoveInventoryItemServerRpc
    public static event Action<string> OnActiveBuffsReceived;     // GetActiveBuffsServerRpc
    public static event Action<string> OnDungeonListReceived;     // GetDungeonListServerRpc
    public static event Action<string> OnInventoryReceived;       // GetPlayerInventoryServerRpc

    public static event Action<string> OnUtilityShopReceived;     // LoadUtilityShopServerRpc
    public static event Action<string> OnUtilityShopBuyResult;    // BuyUtilityShopItemServerRpc

    [Serializable]
    private sealed class UseItemServerResponse
    {
        public int item_template_id;
        public int wave_entry_bonus_added;
        public string message;
    }

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[GameplayCommandService] Duplicate instance on '{gameObject.name}' (existing='{Instance.gameObject.name}') — destroying duplicate COMPONENT only.");
            Destroy(this);
            return;
        }
        Instance = this;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this) Instance = null;
    }

    // PLAYER DATA — in-game reload (control-plane LoadPlayerData đã cover pre-game)

    // Client yêu cầu reload full player data từ DB (ví dụ: sau khi level up).
    [ServerRpc(RequireOwnership = false)]
    public void RequestPlayerDataServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        int geneSlot = ResolveClientGeneSlot(cid);
        string endpoint = geneSlot == 2 ? $"{ApiBase}/player/{pid}/data2" : $"{ApiBase}/player/{pid}/data";
        StartCoroutine(DoGet(
            endpoint, jwt,
            json => SendPlayerDataClientRpc(json, Target(cid)),
            err  => Debug.LogError($"[GameplayCmd] RequestPlayerData cid={cid}: {err}")
        ));
    }

    [ClientRpc]
    private void SendPlayerDataClientRpc(string json, ClientRpcParams p = default)
        => OnPlayerDataReceived?.Invoke(json);

    // SKILLS

    // Lấy danh sách skill của player cùng level hiện tại.
    [ServerRpc(RequireOwnership = false)]
    public void GetPlayerSkillsServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        int geneSlot = ZonePlayerSessionManager.Instance != null ? ZonePlayerSessionManager.Instance.GetClientGeneSlot(cid) : 1;
        string endpoint = geneSlot == 2 ? $"{ApiBase}/player/{pid}/skills2" : $"{ApiBase}/player/{pid}/skills";
        StartCoroutine(DoGet(
            endpoint, jwt,
            json => SendSkillsClientRpc(json, Target(cid)),
            err  => SendSkillsClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    // Nâng cấp 1 skill lên level kế tiếp.
    [ServerRpc(RequireOwnership = false)]
    public void UpgradeSkillServerRpc(int skillId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        int geneSlot = ZonePlayerSessionManager.Instance != null ? ZonePlayerSessionManager.Instance.GetClientGeneSlot(cid) : 1;
        string endpoint = geneSlot == 2 ? $"{ApiBase}/player/{pid}/skills2/upgrade" : $"{ApiBase}/player/{pid}/skills/upgrade";
        StartCoroutine(DoPost(
            endpoint,
            $"{{\"skill_id\":{skillId}}}", jwt,
            json => UpgradeSkillResultClientRpc(json, Target(cid)),
            err  => UpgradeSkillResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendSkillsClientRpc(string json, ClientRpcParams p = default)
        => OnSkillsReceived?.Invoke(json);

    [ClientRpc] private void UpgradeSkillResultClientRpc(string json, ClientRpcParams p = default)
        => OnSkillUpgraded?.Invoke(json);

    // Server chủ động đẩy skill data về client ngay khi player spawn.
    // Gọi từ ZonePlayerSessionManager sau khi spawn xong — client không cần request riêng.
    public void PushSkillsToClient(ulong clientId, int playerId, string jwt, int geneSlot = 1)
    {
        if (!IsServer) return;
        string endpoint = geneSlot == 2 ? $"{ApiBase}/player/{playerId}/skills2" : $"{ApiBase}/player/{playerId}/skills";
        StartCoroutine(DoGet(
            endpoint, jwt,
            json => SendInitialSkillsClientRpc(json, Target(clientId)),
            err  => Debug.LogWarning($"[GameplayCmd] PushSkillsToClient pid={playerId} geneSlot={geneSlot}: {err}")
        ));
    }

    [ClientRpc]
    private void SendInitialSkillsClientRpc(string json, ClientRpcParams p = default)
        => OnInitialSkillsReceived?.Invoke(json);

    // POTENTIAL

    // Lấy thông tin tiềm năng của player.
    [ServerRpc(RequireOwnership = false)]
    public void GetPlayerPotentialServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoGet(
            $"{ApiBase}/player/{pid}/potential", jwt,
            json => SendPotentialClientRpc(json, Target(cid)),
            err  => SendPotentialClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    // Phân bổ tiềm năng. allocationsJson: {"allocations":[{"stat_name":"attack","points":3},...]}
    [ServerRpc(RequireOwnership = false)]
    public void AllocatePotentialStatsServerRpc(string allocationsJson, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoPost(
            $"{ApiBase}/player/{pid}/potential/allocate",
            allocationsJson, jwt,
            json => AllocatePotentialResultClientRpc(json, Target(cid)),
            err  => AllocatePotentialResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendPotentialClientRpc(string json, ClientRpcParams p = default)
        => OnPotentialReceived?.Invoke(json);

    [ClientRpc] private void AllocatePotentialResultClientRpc(string json, ClientRpcParams p = default)
        => OnPotentialAllocated?.Invoke(json);

    // EQUIPMENT

    // Lấy thông tin trang bị đang đeo của player.
    [ServerRpc(RequireOwnership = false)]
    public void GetPlayerEquipmentServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoGet(
            $"{ApiBase}/player/{pid}/equipment", jwt,
            json => SendEquipmentClientRpc(json, Target(cid)),
            err  => SendEquipmentClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    // Trang bị item từ inventory theo slot index.
    [ServerRpc(RequireOwnership = false)]
    public void EquipItemServerRpc(int inventorySlotIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoPost(
            $"{ApiBase}/player/{pid}/equipment/equip",
            $"{{\"inventorySlotIndex\":{inventorySlotIndex}}}", jwt,
            json => EquipResultClientRpc(json, Target(cid)),
            err  => EquipResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    // Tháo trang bị theo slot name (weapon, armor, pants, boots).
    [ServerRpc(RequireOwnership = false)]
    public void UnequipItemServerRpc(string equipmentSlot, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoPost(
            $"{ApiBase}/player/{pid}/equipment/unequip",
            $"{{\"equipmentSlot\":\"{equipmentSlot}\"}}", jwt,
            json => UnequipResultClientRpc(json, Target(cid)),
            err  => UnequipResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnequipBagItemServerRpc(int quickSlotIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoPost(
            $"{ApiBase}/player/{pid}/bag/unequip",
            $"{{\"quickSlotIndex\":{quickSlotIndex}}}", jwt,
            json => UnequipBagResultClientRpc(json, Target(cid)),
            err  => UnequipBagResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendEquipmentClientRpc(string json, ClientRpcParams p = default)
        => OnEquipmentReceived?.Invoke(json);

    [ClientRpc] private void EquipResultClientRpc(string json, ClientRpcParams p = default)
        => OnEquipResult?.Invoke(json);

    [ClientRpc] private void UnequipResultClientRpc(string json, ClientRpcParams p = default)
        => OnUnequipResult?.Invoke(json);

    [ClientRpc] private void UnequipBagResultClientRpc(string json, ClientRpcParams p = default)
        => OnBagUnequipResult?.Invoke(json);

    // GENE UPGRADE

    // Lấy config nâng gene cho element + tier.
    [ServerRpc(RequireOwnership = false)]
    public void GetGeneConfigServerRpc(string elementType, int tier, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        string jwt = ResolveClientJwt(cid);
        string escaped = UnityWebRequest.EscapeURL(elementType);
        StartCoroutine(DoGet(
            $"{ApiBase}/gene/config?elementType={escaped}&tier={tier}", jwt,
            json => SendGeneConfigClientRpc(json, Target(cid)),
            err  => SendGeneConfigClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    // Nâng cấp gene. requestJson: {"player_id":N,"element_type":"Fire",...}
    [ServerRpc(RequireOwnership = false)]
    public void UpgradeGeneServerRpc(string requestJson, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoPost(
            $"{ApiBase}/gene/upgrade", requestJson, jwt,
            json => GeneUpgradeResultClientRpc(json, Target(cid)),
            err  => GeneUpgradeResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendGeneConfigClientRpc(string json, ClientRpcParams p = default)
        => OnGeneConfigReceived?.Invoke(json);

    [ClientRpc] private void GeneUpgradeResultClientRpc(string json, ClientRpcParams p = default)
        => OnGeneUpgraded?.Invoke(json);

    // EQUIPMENT UPGRADE (Blacksmith)

    // Lấy config nâng cấp trang bị cho 1 bậc cụ thể.
    [ServerRpc(RequireOwnership = false)]
    public void GetUpgradeConfigServerRpc(int itemId, int targetLevel, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoGet(
            $"{ApiBase}/upgrade/config?itemId={itemId}&targetLevel={targetLevel}", jwt,
            json => SendUpgradeConfigClientRpc(json, Target(cid)),
            err  => SendUpgradeConfigClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    // Lấy danh sách tất cả option template cho trang bị.
    [ServerRpc(RequireOwnership = false)]
    public void GetOptionTemplatesServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoGet(
            $"{ApiBase}/upgrade/options", jwt,
            json => SendOptionTemplatesClientRpc(json, Target(cid)),
            err  => SendOptionTemplatesClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    // Nâng cấp trang bị. requestJson: {"inventorySlotIndex":N,"targetLevel":M,...}
    [ServerRpc(RequireOwnership = false)]
    public void UpgradeEquipmentServerRpc(string requestJson, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoPost(
            $"{ApiBase}/upgrade/equipment", requestJson, jwt,
            json => EquipmentUpgradeResultClientRpc(json, Target(cid)),
            err  => EquipmentUpgradeResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendUpgradeConfigClientRpc(string json, ClientRpcParams p = default)
        => OnUpgradeConfigReceived?.Invoke(json);

    [ClientRpc] private void SendOptionTemplatesClientRpc(string json, ClientRpcParams p = default)
        => OnOptionTemplatesReceived?.Invoke(json);

    [ClientRpc] private void EquipmentUpgradeResultClientRpc(string json, ClientRpcParams p = default)
        => OnEquipmentUpgraded?.Invoke(json);

    // INVENTORY: USE ITEM
    // (Sort và GetInventory vẫn đi qua NetworkInventory.RequestSortInventoryServerRpc)

    // Sử dụng item trong inventory theo slot.
    [ServerRpc(RequireOwnership = false)]
    public void UseInventoryItemServerRpc(int slotIndex, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        int geneSlot = ResolveClientGeneSlot(cid);
        StartCoroutine(DoPost(
            $"{ApiBase}/player/{pid}/inventory/use-item",
            $"{{\"slotIndex\":{slotIndex},\"geneSlot\":{geneSlot}}}", jwt,
            json =>
            {
                TryApplyWaveTicketBonus(cid, json);
                UseItemResultClientRpc(json, Target(cid));
            },
            err  => UseItemResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void UseItemResultClientRpc(string json, ClientRpcParams p = default)
        => OnUseItemResult?.Invoke(json);

    // Remove/drop item from one inventory slot.
    [ServerRpc(RequireOwnership = false)]
    public void RemoveInventoryItemServerRpc(int slotIndex, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoPost(
            $"{ApiBase}/player/{pid}/inventory/remove",
            $"{{\"slotIndex\":{slotIndex},\"quantity\":{quantity}}}", jwt,
            json => RemoveItemResultClientRpc(json, Target(cid)),
            err  => RemoveItemResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void RemoveItemResultClientRpc(string json, ClientRpcParams p = default)
        => OnRemoveItemResult?.Invoke(json);

    private void TryApplyWaveTicketBonus(ulong clientId, string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Contains("\"error\""))
            return;

        UseItemServerResponse response;
        try
        {
            response = JsonUtility.FromJson<UseItemServerResponse>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GameplayCmd] TryApplyWaveTicketBonus parse failed: {ex.Message}");
            return;
        }

        if (response == null || response.wave_entry_bonus_added <= 0)
            return;

        string userId = ZonePlayerSessionManager.Instance?.GetPlayerId(clientId);
        if (string.IsNullOrWhiteSpace(userId))
            userId = ResolveClientUserId(clientId).ToString();

        WaveSessionManager.Instance?.AddBonusEntries(userId, response.wave_entry_bonus_added);
        Debug.Log($"[GameplayCmd] Applied wave ticket bonus client={clientId} userId={userId} itemTemplateId={response.item_template_id} add={response.wave_entry_bonus_added} msg='{response.message}'");
    }

    // ACTIVE BUFFS

    // Lấy danh sách buff đang active của player.
    [ServerRpc(RequireOwnership = false)]
    public void GetActiveBuffsServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        int geneSlot = ResolveClientGeneSlot(cid);
        StartCoroutine(DoGet(
            $"{ApiBase}/player/{pid}/active-buffs?geneSlot={geneSlot}", jwt,
            json => SendActiveBuffsClientRpc(json, Target(cid)),
            err  => SendActiveBuffsClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendActiveBuffsClientRpc(string json, ClientRpcParams p = default)
        => OnActiveBuffsReceived?.Invoke(json);

    // DUNGEON LIST  (Phase 6: EnterDungeon đi qua ZoneTransitionController)

    // Lấy danh sách phó bản từ DB.
    [ServerRpc(RequireOwnership = false)]
    public void GetDungeonListServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        // Dungeon list endpoint không yêu cầu auth → không gửi JWT để tránh middleware exception
        Debug.Log($"[GameplayCmd] GetDungeonList | cid={cid} url={ApiBase}/dungeon/list", this);
        StartCoroutine(DoGet(
            $"{ApiBase}/dungeon/list", null,
            json => SendDungeonListClientRpc(json, Target(cid)),
            err  => SendDungeonListClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendDungeonListClientRpc(string json, ClientRpcParams p = default)
    {
        Debug.Log($"[GameplayCmd] SendDungeonListClientRpc | payloadLength={(json != null ? json.Length : 0)} hasError={(json != null && json.Contains("\"error\""))}", this);
        OnDungeonListReceived?.Invoke(json);
    }

    // INVENTORY

    // Lấy danh sách inventory của player (trả về full player data JSON, client parse inventory).
    [ServerRpc(RequireOwnership = false)]
    public void GetPlayerInventoryServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        int geneSlot = ResolveClientGeneSlot(cid);
        string endpoint = geneSlot == 2 ? $"{ApiBase}/player/{pid}/data2" : $"{ApiBase}/player/{pid}/data";
        StartCoroutine(DoGet(
            endpoint, jwt,
            json => SendInventoryClientRpc(json, Target(cid)),
            err  => SendInventoryClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendInventoryClientRpc(string json, ClientRpcParams p = default)
        => OnInventoryReceived?.Invoke(json);

    // UTILITY SHOP (Virtual NPC 999 — accessible from anywhere via HUD)

    private const int UtilityShopNpcId = 999;

    // Tải danh sách item của Cửa Hàng Tiện Ích (NPC ảo id=999).
    [ServerRpc(RequireOwnership = false)]
    public void LoadUtilityShopServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid = rpcParams.Receive.SenderClientId;
        int pid   = ResolveClientUserId(cid);
        string jwt = ResolveClientJwt(cid);
        StartCoroutine(DoGet(
            $"{ApiBase}/npc/shop?npcId={UtilityShopNpcId}&playerId={pid}", jwt,
            json => SendUtilityShopClientRpc(json, Target(cid)),
            err  => SendUtilityShopClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    // Mua item từ Cửa Hàng Tiện Ích.
    [ServerRpc(RequireOwnership = false)]
    public void BuyUtilityShopItemServerRpc(int shopItemId, int quantity, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        ulong cid  = rpcParams.Receive.SenderClientId;
        string jwt = ResolveClientJwt(cid);
        string body = $"{{\"npcId\":{UtilityShopNpcId},\"shopItemId\":{shopItemId},\"quantity\":{quantity}}}";
        StartCoroutine(DoPost(
            $"{ApiBase}/npc/shop/buy", body, jwt,
            json => UtilityShopBuyResultClientRpc(json, Target(cid)),
            err  => UtilityShopBuyResultClientRpc(ErrorJson(err), Target(cid))
        ));
    }

    [ClientRpc] private void SendUtilityShopClientRpc(string json, ClientRpcParams p = default)
        => OnUtilityShopReceived?.Invoke(json);

    [ClientRpc] private void UtilityShopBuyResultClientRpc(string json, ClientRpcParams p = default)
        => OnUtilityShopBuyResult?.Invoke(json);

    // UTILITY

    // API base URL (có /api ở cuối) từ ServerAddressConfig.
    private string ApiBase
    {
        get
        {
            string fromZoneConfig = ZoneRoomRegistry.Instance?.Config?.apiBaseUrl;
            if (!string.IsNullOrWhiteSpace(fromZoneConfig))
                return NormalizeApiBaseUrl(fromZoneConfig);

            return NormalizeApiBaseUrl(ServerAddressConfig.Instance.ApiUrl);
        }
    }

    private static ClientRpcParams Target(ulong clientId) => new()
    {
        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
    };

    // Resolve numeric player ID từ NGO clientId.
    private static int ResolveClientUserId(ulong clientId)
    {
        if (ServerPlayerDataManager.Instance != null)
        {
            int uid = ServerPlayerDataManager.Instance.GetUserIdFromClientId(clientId);
            if (uid > 0) return uid;
        }
        if (ZonePlayerSessionManager.Instance != null)
        {
            string s = ZonePlayerSessionManager.Instance.GetPlayerId(clientId);
            if (!string.IsNullOrEmpty(s) && int.TryParse(s, out int uid)) return uid;
        }
        return PlayerPrefs.GetInt("USER_ID", 0);
    }

    // Resolve JWT bearer token từ session manager của client.
    private static int ResolveClientGeneSlot(ulong clientId)
    {
        if (ZonePlayerSessionManager.Instance != null)
        {
            int slot = ZonePlayerSessionManager.Instance.GetClientGeneSlot(clientId);
            if (slot == 2) return 2;
        }

        return PlayerPrefs.GetInt("ACTIVE_GENE_SLOT", 1) == 2 ? 2 : 1;
    }

    private static string ResolveClientJwt(ulong clientId)
    {
        if (ServerPlayerDataManager.Instance != null)
        {
            string jwt = ServerPlayerDataManager.Instance.GetClientJwt(clientId);
            if (!string.IsNullOrEmpty(jwt)) return jwt;
        }
        if (ZonePlayerSessionManager.Instance != null)
        {
            string jwt = ZonePlayerSessionManager.Instance.GetClientJwt(clientId);
            if (!string.IsNullOrEmpty(jwt)) return jwt;
        }
        return PlayerPrefs.GetString("JWT_TOKEN", "");
    }

    private IEnumerator DoGet(string url, string jwt, Action<string> onOk, Action<string> onErr)
    {
        using var req = UnityWebRequest.Get(url);
        req.timeout = 10;
        if (!string.IsNullOrEmpty(jwt))
            req.SetRequestHeader("Authorization", $"Bearer {jwt}");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            onOk?.Invoke(req.downloadHandler.text);
        else
            onErr?.Invoke(DownloadError(req));
    }

    private IEnumerator DoPost(string url, string body, string jwt, Action<string> onOk, Action<string> onErr)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(body ?? "{}");
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(bytes);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = 10;
        req.SetRequestHeader("Content-Type", "application/json");
        if (!string.IsNullOrEmpty(jwt))
            req.SetRequestHeader("Authorization", $"Bearer {jwt}");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success)
            onOk?.Invoke(req.downloadHandler.text);
        else
            onErr?.Invoke(DownloadError(req));
    }

    private static string NormalizeApiBaseUrl(string value)
    {
        string normalized = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().TrimEnd('/');
        if (normalized.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            return normalized;
        return string.IsNullOrEmpty(normalized) ? normalized : $"{normalized}/api";
    }

    private static string DownloadError(UnityWebRequest req)
    {
        string body = req.downloadHandler?.text;
        if (!string.IsNullOrWhiteSpace(body))
            return body;

        string transportError = string.IsNullOrWhiteSpace(req.error) ? "HTTP request failed" : req.error;
        return $"HTTP {(long)req.responseCode}: {transportError}";
    }

    // Tạo JSON error payload từ error message.
    private static string ErrorJson(string err)
        => $"{{\"error\":\"{(err ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ")}\"}}";
}
