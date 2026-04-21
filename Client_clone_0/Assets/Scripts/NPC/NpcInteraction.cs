using System;
using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using Unity.Netcode;

/// <summary>
/// NPC click handler — NGO server-authoritative.
///
/// Luồng: Client click → InteractServerRpc → Server validate + fetch dialogue → OpenMenuClientRpc về đúng client.
/// Tương tự cho shop (LoadShopServerRpc) và mua hàng (BuyItemServerRpc).
///
/// Yêu cầu:
///   - Gắn trên NPC Prefab cùng với NetworkObject component.
///   - Camera cần có Physics2DRaycaster để IPointerClickHandler hoạt động.
///   - NpcServerManager phải có trong scene (server-side, để validate và lấy cache).
/// </summary>
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

    // ── Selection (chọn NPC lần 1 → hiện info, lần 2 → mở menu) ─────────────
    [Tooltip("Child GameObject mũi tên chỉ thị, mặc định ẩn. Nếu để trống sẽ tự tìm theo tên 'SelectionIndicator'.")]
    [SerializeField] private GameObject selectionIndicator;

    private static NpcInteraction _currentSelected;

    private static readonly string[] _randomElements =
        { "Fire", "Water", "Earth", "Metal", "Wood", "Wind" };

    private const float MAX_DIST = 3.5f;    // khoảng cách tối đa tương tác (units)
    private const float LENIENCY = 1.5f;    // hệ số khoan nhượng bù lag mạng

    /// <summary>Gọi bởi NpcServerManager ngay sau NetworkObject.Spawn(). Chỉ chạy trên server.</summary>
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

    // ── CLIENT — Click / Tap ──────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsClient) return;

        Debug.Log($"{LogPrefix} Click | {DescribeNpcForLog()} state={DescribeClientState()}", this);

        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked)
        {
            Debug.Log($"{LogPrefix} Click ignored because gameplay input is blocked by UI.", this);
            return;
        }

        // Nếu menu NPC đang mở → không xử lý click (tránh click xuyên UI)
        var ui = NpcMenuUI.GetOrFind();
        if (ui != null && ui.IsOpen)
        {
            Debug.Log($"{LogPrefix} Click ignored because NpcMenuUI is already open.", this);
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
                Debug.Log($"{LogPrefix} Quá xa ({dist:F1}u). Lại gần NPC hơn!", this);
                return;
            }
        }

        // Click lần 1 (NPC chưa được chọn): hiển thị thông tin + mũi tên
        if (_currentSelected != this)
        {
            Debug.Log($"{LogPrefix} First click -> select NPC | {DescribeNpcForLog()}", this);
            SelectThis();
            return;
        }

        // Click lần 2 (NPC đã được chọn): mở menu / tương tác như cũ
        Debug.Log($"{LogPrefix} Second click -> InteractServerRpc | {DescribeNpcForLog()}", this);
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

    // ── SELECTION — Chọn NPC (click 1) / bỏ chọn ────────────────────────────

    /// <summary>Chọn NPC này: hiển thị mũi tên, info panel và đặt làm target auto-move.</summary>
    private void SelectThis()
    {
        // Bỏ chọn enemy đang được chọn
        EnemyClickHandler.DeselectCurrent();

        // Bỏ chọn NPC cũ
        if (_currentSelected != null && _currentSelected != this)
            _currentSelected.DeselectThis();

        _currentSelected = this;

        if (selectionIndicator != null)
            selectionIndicator.SetActive(true);

        TargetSelector.SetTarget(transform);

        EnemyInfoPanel.Instance?.Show(BuildNpcStats());
        Debug.Log($"{LogPrefix} Selected | {DescribeNpcForLog()}", this);
    }

    /// <summary>Bỏ chọn NPC này: ẩn mũi tên, ẩn info panel.</summary>
    private void DeselectThis()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);

        TargetSelector.ClearTarget(transform);

        EnemyInfoPanel.Instance?.Hide();
        Debug.Log($"{LogPrefix} Deselected | {DescribeNpcForLog()}", this);
    }

    /// <summary>Bỏ chọn NPC hiện tại (gọi từ EnemyClickHandler khi enemy được chọn).</summary>
    public static void DeselectCurrent()
    {
        if (_currentSelected != null)
        {
            _currentSelected.DeselectThis();
            _currentSelected = null;
        }
    }

    /// <summary>Xây thông số NPC để hiển thị trên EnemyInfoPanel.</summary>
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

    // ── INTERACT — Server validate + fetch dialogue ───────────

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(ulong npcNetworkId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        Debug.Log($"{LogPrefix} InteractServerRpc | sender={clientId} npcNetId={npcNetworkId} worldPos={transform.position}", this);

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
                Debug.LogWarning($"{LogPrefix} Client {clientId} quá xa ({dist:F1}u). Từ chối.", this);
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
            Debug.LogWarning($"{LogPrefix} Không resolve được NpcData cho npcNetId={npcNetworkId}.", this);
            return;
        }

        Debug.Log($"{LogPrefix} Interact validated | sender={clientId} npcId={data.npc_id} type='{data.npc_type}' name='{data.npc_name}'", this);

        StartCoroutine(FetchDialogueAndSend(data, clientId));
    }

    private IEnumerator FetchDialogueAndSend(NpcData data, ulong clientId)
    {
        int userId = ResolveClientUserId(clientId);
        string jwtToken = ResolveClientJwt(clientId);
        data.dialogue_text = ResolveFallbackDialogueText(data);

        if (userId <= 0)
        {
            Debug.LogWarning($"{LogPrefix} Skip dialogue fetch | client={clientId} npcId={data.npc_id} because resolved playerId is invalid.", this);
            Debug.Log($"{LogPrefix} OpenMenuClientRpc send | client={clientId} npcId={data.npc_id} type='{data.npc_type}'", this);
            OpenMenuClientRpc(JsonUtility.ToJson(data), TargetClient(clientId));
            yield break;
        }

        string apiBase = NpcServerManager.Instance?.ApiBase ?? ServerAddressConfig.Instance.ApiRoot;
        string body    = JsonUtility.ToJson(new InteractPayload { npcId = data.npc_id, playerId = userId });

        Debug.Log($"{LogPrefix} FetchDialogue start | client={clientId} npcId={data.npc_id} playerId={userId} url={apiBase}/api/npc/interact", this);

        using var req = PostJson($"{apiBase}/api/npc/interact", body);
        if (!string.IsNullOrEmpty(jwtToken))
            req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            string dialogueText = ExtractDialogueText(req.downloadHandler.text);
            if (!string.IsNullOrWhiteSpace(dialogueText))
                data.dialogue_text = dialogueText;

            Debug.Log($"{LogPrefix} FetchDialogue success | client={clientId} npcId={data.npc_id} dialogue='{data.dialogue_text}'", this);
        }
        else
        {
            Debug.LogWarning($"{LogPrefix} FetchDialogue failed | client={clientId} npcId={data.npc_id} error={req.error} response={req.downloadHandler?.text}", this);
        }

        Debug.Log($"{LogPrefix} OpenMenuClientRpc send | client={clientId} npcId={data.npc_id} type='{data.npc_type}'", this);
        OpenMenuClientRpc(JsonUtility.ToJson(data), TargetClient(clientId));
    }

    [ClientRpc]
    private void OpenMenuClientRpc(string npcDataJson, ClientRpcParams clientRpcParams = default)
    {
        var data = JsonUtility.FromJson<NpcData>(npcDataJson);
        var menu = NpcMenuUI.GetOrFind();
        Debug.Log($"{LogPrefix} OpenMenuClientRpc received | {DescribeNpcForLog(data)} menuFound={menu != null} state={DescribeClientState()}", this);
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

    // ── LOAD SHOP — Server fetch shop items + gửi về client ──

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
        NpcMenuUI.GetOrFind()?.ShowShop(shopItemsJson);
    }

    // ── BUY — Server gọi API mua, trả kết quả về client ─────

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
            Debug.LogWarning("[Buy] NpcData null — không thể mua.");
            SendBuyResult(clientId, false, "Lỗi: NPC data không tồn tại.", 0);
            yield break;
        }

        string jwtToken = ResolveClientJwt(clientId);
        if (string.IsNullOrEmpty(jwtToken))
        {
            Debug.LogWarning("[Buy] JWT_TOKEN trống — chưa đăng nhập.");
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

        Debug.Log($"[Buy] POST {apiBase}/api/npc/shop/buy  body={body}  userId={userId}");

        using var req = PostJson($"{apiBase}/api/npc/shop/buy", body);
        req.SetRequestHeader("Authorization", $"Bearer {jwtToken}");
        yield return req.SendWebRequest();

        Debug.Log($"[Buy] Response: {req.responseCode}  {req.downloadHandler?.text}");

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

    // ── Utility ──────────────────────────────────────────────

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
}
