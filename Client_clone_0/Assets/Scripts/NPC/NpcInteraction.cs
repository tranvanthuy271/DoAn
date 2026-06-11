using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using Unity.Netcode;

// NPC click handler — NGO server-authoritative.
// Luồng: Client click → InteractServerRpc → Server validate + fetch dialogue → OpenMenuClientRpc về đúng client.
// Tương tự cho shop (LoadShopServerRpc) và mua hàng (BuyItemServerRpc).
// Yêu cầu:
// - Gắn trên NPC Prefab cùng với NetworkObject component.
// - Camera cần có Physics2DRaycaster để IPointerClickHandler hoạt động.
// - NpcServerManager phải có trong scene (server-side, để validate và lấy cache).
public class NpcInteraction : NetworkBehaviour, IPointerClickHandler
{
    private const string LogPrefix = "[NpcInteraction]";
    private const int ApiRequestTimeoutSeconds = 8;

    private NpcData _npcData;   // chỉ server có — set bởi NpcServerManager.InitOnServer()

    // NetworkVariable sync tên NPC từ server → tất cả client
    private readonly NetworkVariable<FixedString128Bytes> _networkNpcName =
        new NetworkVariable<FixedString128Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private NpcNameLabel _nameLabel;

    // Selection (chọn NPC lần 1 → hiện info, lần 2 → mở menu)
    [Tooltip("Child GameObject mũi tên chỉ thị, mặc định ẩn. Nếu để trống sẽ tự tìm theo tên 'SelectionIndicator'.")]
    [SerializeField] private GameObject selectionIndicator;

    private static NpcInteraction _currentSelected;

    private static readonly string[] _randomElements =
        { "Fire", "Water", "Earth", "Metal", "Wood", "Wind" };

    private const float MAX_DIST = 3.5f;    // khoảng cách tối đa tương tác (units)
    private const float LENIENCY = 1.5f;    // hệ số khoan nhượng bù lag mạng

    // Gọi bởi NpcServerManager ngay sau NetworkObject.Spawn(). Chỉ chạy trên server.
    public void InitOnServer(NpcData data)
    {
        _npcData = data;
        // Sync tên sang tất cả client qua NetworkVariable
        _networkNpcName.Value = new FixedString128Bytes(data.npc_name ?? "");
    }

    public override void OnNetworkSpawn()
    {
        // Tự thêm NpcNameLabel nếu prefab chưa có
        _nameLabel = GetComponent<NpcNameLabel>() ?? gameObject.AddComponent<NpcNameLabel>();

        // Lắng nghe thay đổi tên (server set → client nhận)
        _networkNpcName.OnValueChanged += (_, newVal) => _nameLabel.SetName(newVal.ToString());

        // Nếu giá trị đã có sẵn (client join muộn) thì set ngay
        if (!_networkNpcName.Value.IsEmpty)
            _nameLabel.SetName(_networkNpcName.Value.ToString());

        // Tự động tìm SelectionIndicator nếu chưa gán trong Inspector
        if (selectionIndicator == null)
            selectionIndicator = transform.Find("SelectionIndicator")?.gameObject;
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    public override void OnNetworkDespawn()
    {
        // Dọn selection state khi NPC despawn (chuyển map, v.v.)
        if (_currentSelected == this)
        {
            DeselectThis();
            _currentSelected = null;
        }
        base.OnNetworkDespawn();
    }

    // CLIENT — Click / Tap

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsClient) return;

        { /* {LogPrefix} Click | {DescribeNpcForLog()} state={DescribeClientState()} */ }

        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked)
        {
            { /* {LogPrefix} Click ignored because gameplay input is blocked by UI */ }
            return;
        }

        // Nếu menu NPC đang mở → không xử lý click (tránh click xuyên UI)
        var ui = NpcMenuUI.GetOrFind();
        if (ui != null && ui.IsOpen)
        {
            { /* {LogPrefix} Click ignored because NpcMenuUI is already open */ }
            return;
        }
        var dynUi = NpcDynamicMenuUI.GetOrFind();
        if (dynUi != null && dynUi.IsOpen)
        {
            { /* {LogPrefix} Click ignored because NpcDynamicMenuUI is already open */ }
            return;
        }

        // Pre-check khoảng cách ở client
        NetworkObject localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (localObj == null)
            localObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;

        if (localObj != null)
        {
            float dist = Vector2.Distance(transform.position, localObj.transform.position);
            if (dist > MAX_DIST)
            {
                { /* {LogPrefix} Quá xa ({dist:F1}u). Lại gần NPC hơn */ }
                return;
            }
        }

        // Click lần 1 (NPC chưa được chọn): hiển thị thông tin + mũi tên
        if (_currentSelected != this)
        {
            { /* {LogPrefix} First click -> select NPC | {DescribeNpcForLog()} */ }
            SelectThis();
            return;
        }

        // Click lần 2 (NPC đã được chọn): mở menu / tương tác như cũ
        { /* {LogPrefix} Second click -> InteractServerRpc | {DescribeNpcForLog()} */ }
        InteractServerRpc(NetworkObjectId);
    }

    private void OnMouseDown()   // fallback khi chưa có Physics2DRaycaster
    {
        // Không xử lý nếu con trỏ đang ở trên UI (ví dụ: ChatPanel đang mở phủ lên)
        if (UnityEngine.EventSystems.EventSystem.current != null &&
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            return;
        OnPointerClick(null);
    }

    // SELECTION — Chọn NPC (click 1) / bỏ chọn

    // Chọn NPC này: hiển thị mũi tên, info panel và đặt làm target auto-move.
    private void SelectThis()
    {
        // Bỏ chọn enemy đang được chọn
        EnemyClickHandler.DeselectCurrent();
        PlayerClickHandler.DeselectCurrent();

        // Bỏ chọn NPC cũ
        if (_currentSelected != null && _currentSelected != this)
            _currentSelected.DeselectThis();

        _currentSelected = this;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        TargetSelector.SetTarget(transform);

        EnemyInfoPanel.Instance?.Show(BuildNpcStats());
        { /* {LogPrefix} Selected | {DescribeNpcForLog()} */ }
    }

    // Bỏ chọn NPC này: ẩn mũi tên, ẩn info panel.
    private void DeselectThis()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        TargetSelector.ClearTarget(transform);

        EnemyInfoPanel.Instance?.Hide();
        { /* {LogPrefix} Deselected | {DescribeNpcForLog()} */ }
    }

    // Bỏ chọn NPC hiện tại (gọi từ EnemyClickHandler khi enemy được chọn).
    public static void DeselectCurrent()
    {
        if (_currentSelected != null)
        {
            _currentSelected.DeselectThis();
            _currentSelected = null;
        }
    }

    // Xây thông số NPC để hiển thị trên EnemyInfoPanel.
    private EnemyStats BuildNpcStats()
    {
        string npcName = (!_networkNpcName.Value.IsEmpty)
            ? _networkNpcName.Value.ToString()
            : gameObject.name.Replace("(Clone)", "").Trim();

        string element = _randomElements[UnityEngine.Random.Range(0, _randomElements.Length)];

        return new EnemyStats
        {
            enemyName   = npcName,
            currentHp   = 100,
            maxHp       = 100,
            elementType = element,
            level       = 1,
            expReward   = 0
        };
    }

    // INTERACT — Server validate + fetch dialogue

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(ulong npcNetworkId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        { /* {LogPrefix} InteractServerRpc | sender={clientId} npcNetId={npcNetworkId} worldPos={transform.position} */ }

        if (!NetworkManager.ConnectedClients.TryGetValue(clientId, out var client)) return;

        // Validate khoảng cách thật phía server (chống gian lận)
        // Lấy PlayerObject — với Host thử cả LocalClient
        NetworkObject playerObj = client.PlayerObject;
        if (playerObj == null && clientId == NetworkManager.ServerClientId)
            playerObj = NetworkManager.LocalClient?.PlayerObject;

        if (playerObj != null)
        {
            float dist = Vector2.Distance(transform.position, playerObj.transform.position);
            if (dist > MAX_DIST * LENIENCY)
            {
                { /* Cảnh báo: {LogPrefix} Client {clientId} quá xa ({dist:F1}u). Từ chối */ }
                return;
            }
        }
        // playerObj == null → PlayerObject chưa spawn/register → bỏ qua distance check, tiếp tục

        // Ưu tiên cache từ NpcServerManager, fallback về _npcData cục bộ
        NpcData data = _npcData;
        if (NpcServerManager.Instance != null && NpcServerManager.Instance.TryGetNpcData(npcNetworkId, out var cached))
            data = cached;

        if (data == null)
        {
            { /* Cảnh báo: {LogPrefix} Không resolve được NpcData cho npcNetId={npcNetworkId} */ }
            return;
        }

        { /* {LogPrefix} Interact validated | sender={clientId} npcId={data.npc_id} type='{data.npc_type}' name='{data.npc_name}' */ }

        TryReportQuestTalkProgress(clientId, data);

        StartCoroutine(FetchDialogueAndSend(data, clientId));
    }

    private IEnumerator FetchDialogueAndSend(NpcData serverData, ulong clientId)
    {
        int userId = ResolveClientUserId(clientId);
        string jwtToken = ResolveClientJwt(clientId);

        // Tạo bản copy để gửi về client — KHÔNG mutate serverData._npcData
        var clientData = new NpcData
        {
            npc_id       = serverData.npc_id,
            npc_name     = serverData.npc_name,
            npc_type     = serverData.npc_type,
            pos_x        = serverData.pos_x,
            pos_y        = serverData.pos_y,
            dialogue_key = serverData.dialogue_key,
            icon_id      = serverData.icon_id,
            dialogue_text = ResolveFallbackDialogueText(serverData),
            // Menu items từ config C# — chỉ gửi labels về client, action_type giữ phía server
            menu_items   = ExtractMenuItemLabels(
                               NpcMenuConfig.GetMenuItems(serverData.npc_id, serverData.npc_type)),
        };

        if (userId <= 0)
        {
            { /* Cảnh báo: {LogPrefix} Skip dialogue fetch | client={clientId} npcId={clientData.npc_id} because resolved playerId is invalid */ }
            { /* {LogPrefix} OpenMenuClientRpc send | client={clientId} npcId={clientData.npc_id} type='{clientData.npc_type}' */ }
            OpenMenuClientRpc(JsonUtility.ToJson(clientData), TargetClient(clientId));
            yield break;
        }

        string apiBase = NpcServerManager.Instance?.ApiBase ?? ServerAddressConfig.Instance.ApiRoot;
        string body    = JsonUtility.ToJson(new InteractPayload { npcId = clientData.npc_id, playerId = userId });

        { /* {LogPrefix} FetchDialogue start | client={clientId} npcId={clientData.npc_id} playerId={userId} url={apiBase}/api/npc/interact */ }

        using var req = PostJson($"{apiBase}/api/npc/interact", body);
        if (!string.IsNullOrEmpty(jwtToken))
            req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<InteractResponse>(req.downloadHandler.text);

            string dialogueText = !string.IsNullOrWhiteSpace(resp?.dialogue_text) ? resp.dialogue_text
                                : !string.IsNullOrWhiteSpace(resp?.dialogue?.text) ? resp.dialogue.text
                                : string.Empty;
            if (!string.IsNullOrWhiteSpace(dialogueText))
                clientData.dialogue_text = dialogueText;

            { /* {LogPrefix} FetchDialogue success | client={clientId} npcId={clientData.npc_id} dialogue='{clientData.dialogue_text}' menuItems='{clientData.menu_items}' */ }
        }
        else
        {
            { /* Cảnh báo: {LogPrefix} FetchDialogue failed | client={clientId} npcId={clientData.npc_id} error={req.error} response={req.downloadHandler?.text} */ }
        }

        { /* {LogPrefix} OpenMenuClientRpc send | client={clientId} npcId={clientData.npc_id} type='{clientData.npc_type}' */ }
        OpenMenuClientRpc(JsonUtility.ToJson(clientData), TargetClient(clientId));
    }

    [ClientRpc]
    private void OpenMenuClientRpc(string npcDataJson, ClientRpcParams clientRpcParams = default)
    {
        var data = JsonUtility.FromJson<NpcData>(npcDataJson);
        { /* {LogPrefix} OpenMenuClientRpc received | {DescribeNpcForLog(data)} menuFound=true state={DescribeClientState()} */ }

        // Nếu server trả về menu_items → hiện NpcDynamicMenuUI (server-driven)
        if (!string.IsNullOrWhiteSpace(data?.menu_items))
        {
            var dynMenu = NpcDynamicMenuUI.GetOrCreate();
            if (dynMenu != null)
            {
                dynMenu.Open(data, this);
                return;
            }
            { /* Cảnh báo: {LogPrefix} NpcDynamicMenuUI not found  fallback to NpcMenuUI */ }
        }

        // Fallback: menu cũ theo npc_type
        var menu = NpcMenuUI.GetOrFind();
        { /* {LogPrefix} OpenMenuClientRpc fallback to NpcMenuUI | menuFound={menu != null} */ }
        menu?.Open(data, this);
    }

    private string DescribeNpcForLog()
    {
        string npcName = !_networkNpcName.Value.IsEmpty
            ? _networkNpcName.Value.ToString()
            : _npcData?.npc_name ?? gameObject.name.Replace("(Clone)", string.Empty).Trim();

        string npcType = _npcData?.npc_type ?? "unknown";
        int npcId = _npcData != null ? _npcData.npc_id : -1;
        return $"npcId={npcId} name='{npcName}' type='{npcType}' netId={NetworkObjectId}";
    }

    private static string DescribeNpcForLog(NpcData npc)
    {
        if (npc == null)
        {
            return "npc=null";
        }

        return $"npcId={npc.npc_id} name='{npc.npc_name}' type='{npc.npc_type}'";
    }

    private static string DescribeClientState()
    {
        ClientSceneController controller = ClientSceneController.Instance;
        controller?.EnsureZoneStateFromRuntimeData();
        int mapId = controller?.CurrentMapId ?? -1;
        int zoneId = controller?.CurrentZoneId ?? -1;
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return $"scene={sceneName} map={mapId} zone={zoneId}";
    }

    // LOAD SHOP — Server fetch shop items + gửi về client

    [ServerRpc(RequireOwnership = false)]
    public void LoadShopServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        StartCoroutine(FetchShopAndSend(clientId));
    }

    private IEnumerator FetchShopAndSend(ulong clientId)
    {
        NpcData data = _npcData;
        if (data == null) yield break;

        int userId = ResolveClientUserId(clientId);
        string jwtToken = ResolveClientJwt(clientId);

        string apiBase = NpcServerManager.Instance?.ApiBase ?? ServerAddressConfig.Instance.ApiRoot;
        string url     = $"{apiBase}/api/npc/shop?npcId={data.npc_id}&playerId={userId}";

        using var req = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(jwtToken))
            req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
        yield return req.SendWebRequest();

        string json = req.result == UnityWebRequest.Result.Success
            ? req.downloadHandler.text
            : "[]";

        ShowShopClientRpc(json, TargetClient(clientId));
    }

    [ClientRpc]
    private void ShowShopClientRpc(string shopItemsJson, ClientRpcParams clientRpcParams = default)
    {
        // Đóng dynamic menu trước khi mở shop
        NpcDynamicMenuUI.GetOrFind()?.Close();

        var ui = NpcMenuUI.GetOrFind();
        if (ui != null)
        {
            ui.OpenShopDirect(this);
            ui.ShowShop(shopItemsJson);
        }
    }

    // SELECT MENU ITEM — Client gửi lựa chọn, server thực thi action

    [ServerRpc(RequireOwnership = false)]
    public void SelectMenuItemServerRpc(int menuIndex, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        NpcData data = _npcData;
        if (NpcServerManager.Instance != null && NpcServerManager.Instance.TryGetNpcData(NetworkObjectId, out var cached))
            data = cached;

        // Lấy menu items từ C# config — KHÔNG đọc từ DB
        string menuItemsRaw = NpcMenuConfig.GetMenuItems(data?.npc_id ?? 0, data?.npc_type ?? "");
        if (string.IsNullOrWhiteSpace(menuItemsRaw))
        {
            { /* Cảnh báo: {LogPrefix} SelectMenuItemServerRpc: no menu_items | npcId={data?.npc_id} */ }
            return;
        }

        string[] items = menuItemsRaw.Split(';');
        if (menuIndex < 0 || menuIndex >= items.Length)
        {
            { /* Cảnh báo: {LogPrefix} SelectMenuItemServerRpc: invalid index={menuIndex} count={items.Length} */ }
            return;
        }

        string item       = items[menuIndex];
        int    colonIdx   = item.IndexOf(':');
        string actionType = colonIdx >= 0 ? item.Substring(colonIdx + 1).Trim() : item.Trim();

        { /* {LogPrefix} SelectMenuItemServerRpc | client={clientId} idx={menuIndex} action='{actionType}' */ }

        switch (actionType.ToLowerInvariant())
        {
            case "open_shop":
                StartCoroutine(FetchShopAndSend(clientId));
                break;
            case "open_blacksmith":
            case "open_gene_upgrade":
            case "open_secondary_select":
            case "open_secondary_upgrade":
            case "open_hybrid_fusion":
                ExecuteMenuActionClientRpc(actionType.ToLowerInvariant(), TargetClient(clientId));
                break;
            case "open_dungeon":
                ExecuteMenuActionClientRpc("open_dungeon", TargetClient(clientId));
                break;

            // NPC actions xử lý server-side qua NpcAction
            case "reset_potential":
            case "reset_skill":
            case "learn_skill":
            case "exchange_skill":
            case "exchange_charm":
            case "lock_level":
                NpcAction.Execute(actionType, data, clientId, this);
                break;

            case "close":
            default:
                ExecuteMenuActionClientRpc("close", TargetClient(clientId));
                break;
        }
    }

    [ClientRpc]
    private void ExecuteMenuActionClientRpc(string actionType, ClientRpcParams clientRpcParams = default)
    {
        var dynMenu = NpcDynamicMenuUI.GetOrFind();
        var lastData = dynMenu?.LastOpenedNpcData;

        // Đóng dynamic menu trước
        dynMenu?.Close();

        switch (actionType.ToLowerInvariant())
        {
            case "open_blacksmith":
            {
                // Mở thẳng BlacksmithTabPanel tab 0 (Cường Hóa Trang Bị)
                var tabPanel = BlacksmithTabPanel.Instance
                    ?? UnityEngine.Object.FindObjectOfType<BlacksmithTabPanel>(true);
                if (tabPanel != null)
                {
                    tabPanel.Open(0);
                }
                else
                {
                    // Fallback cũ nếu chưa có BlacksmithTabPanel trong scene
                    var mockData = new NpcData { npc_type = "blacksmith", npc_name = lastData?.npc_name ?? "Thợ Rèn" };
                    NpcMenuUI.GetOrFind()?.Open(mockData, this);
                }
                break;
            }
            case "open_gene_upgrade":
            {
                var panel = GeneUpgradePanel.Instance
                    ?? UnityEngine.Object.FindObjectOfType<GeneUpgradePanel>(true);
                if (panel != null) panel.Open();
                else { /* Cảnh báo: {LogPrefix} ExecuteMenuAction: GeneUpgradePanel không tìm thấy trong scene */ }
                break;
            }
            case "open_secondary_select":
            {
                var panel = SecondaryGeneSelectPanel.Instance
                    ?? UnityEngine.Object.FindObjectOfType<SecondaryGeneSelectPanel>(true);
                if (panel != null) panel.Open();
                else { /* Cảnh báo: {LogPrefix} ExecuteMenuAction: SecondaryGeneSelectPanel không tìm thấy trong scene */ }
                break;
            }
            case "open_secondary_upgrade":
            {
                var panel = GeneUpgradePanel.Instance
                    ?? UnityEngine.Object.FindObjectOfType<GeneUpgradePanel>(true);
                if (panel != null) panel.OpenForSecondary();
                else { /* Cảnh báo: {LogPrefix} ExecuteMenuAction: GeneUpgradePanel không tìm thấy trong scene */ }
                break;
            }
            case "open_hybrid_fusion":
            {
                var panel = HybridFusionPanel.Instance
                    ?? UnityEngine.Object.FindObjectOfType<HybridFusionPanel>(true);
                if (panel == null)
                {
                    var prefabGO = Resources.Load<GameObject>("UI/HybridFusionCanvas");
                    if (prefabGO != null)
                    {
                        var go = Instantiate(prefabGO);
                        go.name = "HybridFusionCanvas";
                        panel = go.GetComponentInChildren<HybridFusionPanel>(true);
                    }
                }
                if (panel != null) panel.Open();
                else { /* Cảnh báo: {LogPrefix} ExecuteMenuAction: HybridFusionPanel không tìm thấy */ }
                break;
            }
            case "open_dungeon":
            {
                var dungeonMenu = DungeonNpcMenuUI.GetOrCreate();
                if (dungeonMenu != null)
                {
                    var mockData = lastData ?? new NpcData { npc_type = "dungeon", npc_name = "Dungeon NPC" };
                    dungeonMenu.Open(mockData);
                }
                break;
            }
            case "close":
            default:
                // Dynamic menu đã đóng ở trên
                break;
        }
    }

    // BUY — Server gọi API mua, trả kết quả về client

    [ServerRpc(RequireOwnership = false)]
    public void BuyItemServerRpc(int itemId, int quantity, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        StartCoroutine(ProcessBuy(clientId, itemId, quantity));
    }

    private IEnumerator ProcessBuy(ulong clientId, int shopItemId, int quantity)
    {
        NpcData data = _npcData;
        int userId = ResolveClientUserId(clientId);

        if (data == null)
        {
            { /* Cảnh báo: NpcData null  không thể mua */ }
            SendBuyResult(clientId, false, "Lỗi: NPC data không tồn tại.", 0);
            yield break;
        }

        string jwtToken = ResolveClientJwt(clientId);
        if (string.IsNullOrEmpty(jwtToken))
        {
            { /* Cảnh báo: JWT_TOKEN trống  chưa đăng nhập */ }
            SendBuyResult(clientId, false, "Chưa đăng nhập. Vui lòng đăng nhập lại.", 0);
            yield break;
        }

        string apiBase = NpcServerManager.Instance?.ApiBase ?? ServerAddressConfig.Instance.ApiRoot;
        string body = JsonUtility.ToJson(new BuyPayload
        {
            npcId      = data.npc_id,
            shopItemId = shopItemId,
            quantity   = quantity
        });

        { /* POST {apiBase}/api/npc/shop/buy  body={body}  userId={userId} */ }

        using var req = PostJson($"{apiBase}/api/npc/shop/buy", body);
        req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
        yield return req.SendWebRequest();

        { /* Response: {req.responseCode}  {req.downloadHandler?.text} */ }

        if (req.result == UnityWebRequest.Result.Success)
        {
            var result = JsonUtility.FromJson<BuyResponse>(req.downloadHandler.text);
            SendBuyResult(clientId, result.success, result.message, result.playerGold);
        }
        else
        {
            string errMsg = req.responseCode == 401 ? "Token hết hạn, vui lòng đăng nhập lại."
                          : req.responseCode == 400 ? req.downloadHandler?.text ?? "Không thể mua."
                          : "Lỗi kết nối server.";
            SendBuyResult(clientId, false, errMsg, 0);
        }
    }

    private void SendBuyResult(ulong clientId, bool success, string message, int newGold)
        => BuyResultClientRpc(success, message, newGold, TargetClient(clientId));

    [ClientRpc]
    private void BuyResultClientRpc(bool success, string message, int newGold,
        ClientRpcParams clientRpcParams = default)
    {
        NpcMenuUI.GetOrFind()?.OnBuyResult(success, message, newGold);
    }

    // Utility

    private static ClientRpcParams TargetClient(ulong clientId) => new()
    {
        Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
    };

    private static int ResolveClientUserId(ulong clientId)
    {
        // 1. ServerPlayerDataManager (host mode)
        if (ServerPlayerDataManager.Instance != null)
        {
            int uid = ServerPlayerDataManager.Instance.GetUserIdFromClientId(clientId);
            if (uid > 0) return uid;
        }

        // 2. ZonePlayerSessionManager (dedicated 1-port server)
        if (ZonePlayerSessionManager.Instance != null)
        {
            string s = ZonePlayerSessionManager.Instance.GetPlayerId(clientId);
            if (!string.IsNullOrEmpty(s) && int.TryParse(s, out int uid)) return uid;
        }

        // 3. Fallback: PlayerPrefs (chỉ đúng khi host call cho chính mình)
        return PlayerPrefs.GetInt("USER_ID", 0);
    }

    private static string ResolveClientJwt(ulong clientId)
    {
        // 1. ServerPlayerDataManager (host mode)
        if (ServerPlayerDataManager.Instance != null)
        {
            string jwt = ServerPlayerDataManager.Instance.GetClientJwt(clientId);
            if (!string.IsNullOrEmpty(jwt)) return jwt;
        }

        // 2. ZonePlayerSessionManager (dedicated 1-port server)
        if (ZonePlayerSessionManager.Instance != null)
        {
            string jwt = ZonePlayerSessionManager.Instance.GetClientJwt(clientId);
            if (!string.IsNullOrEmpty(jwt)) return jwt;
        }

        // 3. Fallback: JWT của chính process (chỉ đúng khi là chính host/self)
        return PlayerPrefs.GetString("JWT_TOKEN", "");
    }

    private static string ResolveFallbackDialogueText(NpcData data)
    {
        if (!string.IsNullOrWhiteSpace(data?.dialogue_text))
            return data.dialogue_text;

        if (string.Equals(data?.npc_type, "dungeon", StringComparison.OrdinalIgnoreCase))
            return "Xin chào, ta có thể đưa ngươi vào các vùng nguy hiểm.";

        return "Xin chào, ta có thể giúp gì cho ngươi?";
    }

    private void TryReportQuestTalkProgress(ulong clientId, NpcData data)
    {
        if (!IsServer || data == null)
            return;

        if (!string.Equals(data.npc_type, "quest", StringComparison.OrdinalIgnoreCase) || data.npc_id <= 0)
            return;

        var playerSync = FindPlayerSyncByClientId(clientId);
        int dbPlayerId = playerSync != null ? playerSync.networkPlayerId.Value : ResolveClientUserId(clientId);
        if (dbPlayerId <= 0)
        {
            { /* Cảnh báo: BỎ QUA: không resolve được playerId cho clientId={clientId} npcId={data.npc_id} */ }
            return;
        }

        { /* → gọi QuestProgressReporter.Report Talk playerId={dbPlayerId} npcId={data.npc_id} */ }
        QuestProgressReporter.Report(this, dbPlayerId, QuestProgressReporter.ProgressType.Talk, data.npc_id, 1,
            () => playerSync?.NotifyQuestProgressOnServer("talk"));
    }

    private static NetworkPlayerDataSync FindPlayerSyncByClientId(ulong clientId)
    {
        var spawnedObjects = NetworkManager.Singleton?.SpawnManager?.SpawnedObjects;
        if (spawnedObjects == null)
            return null;

        foreach (var kvp in spawnedObjects)
        {
            if (kvp.Value.OwnerClientId != clientId)
                continue;

            var sync = kvp.Value.GetComponent<NetworkPlayerDataSync>();
            if (sync != null)
                return sync;
        }

        return null;
    }

    private static string ExtractDialogueText(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        var resp = JsonUtility.FromJson<InteractResponse>(json);
        if (!string.IsNullOrWhiteSpace(resp?.dialogue_text))
            return resp.dialogue_text;

        return !string.IsNullOrWhiteSpace(resp?.dialogue?.text)
            ? resp.dialogue.text
            : string.Empty;
    }

    // Từ chuỗi "label:action_type;label2:action_type2" trả về "label;label2"
    // (chỉ labels để gửi về client — action_type giữ lại phía server).
    private static string ExtractMenuItemLabels(string menuItemsRaw)
    {
        if (string.IsNullOrWhiteSpace(menuItemsRaw))
            return string.Empty;

        var labels = new System.Collections.Generic.List<string>();
        foreach (var item in menuItemsRaw.Split(';'))
        {
            if (string.IsNullOrWhiteSpace(item)) continue;
            int colonIdx = item.IndexOf(':');
            labels.Add(colonIdx >= 0 ? item.Substring(0, colonIdx).Trim() : item.Trim());
        }
        return string.Join(";", labels);
    }

    private static UnityWebRequest PostJson(string url, string json)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.timeout = ApiRequestTimeoutSeconds;
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    [System.Serializable] private class InteractPayload  { public int npcId, playerId; }
    [System.Serializable] private class InteractResponse { public string dialogue_text; public InteractDialogue dialogue; }
    [System.Serializable] private class InteractDialogue { public string text; }
    [System.Serializable] private class BuyPayload       { public int npcId, shopItemId, quantity; }
    [System.Serializable] private class BuyResponse      { public bool success; public string message; public int playerGold; }

    // Public static wrappers — dùng bởi NpcAction.cs

    public static int ResolveClientUserIdStatic(ulong clientId) => ResolveClientUserId(clientId);
    public static string ResolveClientJwtStatic(ulong clientId) => ResolveClientJwt(clientId);

    // SendActionResultRpc — gửi kết quả action về client (gọi bởi NpcAction)

    // Gửi kết quả xử lý NPC action (reset_potential, lock_level, ...) về client.
    // playerDataJson: JSON của NpcAction.NpcActionPlayerData, có thể null/empty nếu không cần cập nhật.
    public void SendActionResultRpc(ulong clientId, bool success, string message, string playerDataJson)
    {
        if (IsServer)
            ShowActionResultClientRpc(success, message, playerDataJson ?? "", TargetClient(clientId));
    }

    [ClientRpc]
    private void ShowActionResultClientRpc(bool success, string message, string playerDataJson,
                                           ClientRpcParams clientRpcParams = default)
    {
        { /* {LogPrefix} ActionResult success={success} msg='{message}' */ }

        // Hiện thông báo trên UI
        GlobalNotificationUI.Show(message, success ? "Thành công" : "Thông báo", 3f);

        // Cập nhật thông số nhân vật nếu server trả về (dự phòng cho tương lai)
        if (success && !string.IsNullOrWhiteSpace(playerDataJson) && playerDataJson != "null" && playerDataJson != "{}")
        {
            try
            {
                var pd = JsonUtility.FromJson<NpcAction.NpcActionPlayerData>(playerDataJson);
                if (pd != null)
                {
                    { /* {LogPrefix} Player data updated gold={pd.gold} silver={pd.silver} level={pd.level} */ }
                    // TODO: khi có GoldHUD / StatsHUD, gọi cập nhật ở đây
                }
            }
            catch (Exception ex)
            {
                { /* Cảnh báo: {LogPrefix} ShowActionResultClientRpc: parse playerData failed: {ex.Message} */ }
            }
        }
    }
}

