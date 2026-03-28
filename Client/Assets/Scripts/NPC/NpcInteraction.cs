using System.Collections;
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
    private NpcData _npcData;   // chỉ server có — set bởi NpcServerManager.InitOnServer()

    private const float MAX_DIST = 3.5f;    // khoảng cách tối đa tương tác (units)
    private const float LENIENCY = 1.5f;    // hệ số khoan nhượng bù lag mạng

    /// <summary>Gọi bởi NpcServerManager ngay sau NetworkObject.Spawn(). Chỉ chạy trên server.</summary>
    public void InitOnServer(NpcData data) => _npcData = data;

    // ── CLIENT — Click / Tap ──────────────────────────────────

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsClient) return;

        // Không cho tương tác khi panel NPC đang mở (tránh click xuyên qua UI)
        var ui = NpcMenuUI.GetOrFind();
        if (ui != null && ui.IsOpen) return;

        // Pre-check khoảng cách ở client để UX mượt (không authoritative)
        // Thử nhiều cách tìm player object — nếu không tìm thấy vẫn cho phép gửi RPC (server validate)
        NetworkObject localObj = NetworkManager.Singleton?.SpawnManager?.GetLocalPlayerObject();
        if (localObj == null)
            localObj = NetworkManager.Singleton?.LocalClient?.PlayerObject;

        if (localObj != null)
        {
            float dist = Vector2.Distance(transform.position, localObj.transform.position);
            if (dist > MAX_DIST)
            {
                Debug.Log($"[NpcInteraction] Quá xa ({dist:F1}u). Lại gần NPC hơn!");
                return;
            }
        }
        // else: không tìm được PlayerObject client-side → bỏ qua check khoảng cách client, để server validate

        InteractServerRpc(NetworkObjectId);
    }

    private void OnMouseDown()   // fallback khi chưa có Physics2DRaycaster
    {
        OnPointerClick(null);
    }

    // ── INTERACT — Server validate + fetch dialogue ───────────

    [ServerRpc(RequireOwnership = false)]
    private void InteractServerRpc(ulong npcNetworkId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

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
                Debug.LogWarning($"[NpcInteraction] Client {clientId} quá xa ({dist:F1}u). Từ chối.");
                return;
            }
        }
        // playerObj == null → PlayerObject chưa spawn/register → bỏ qua distance check, tiếp tục

        // Ưu tiên cache từ NpcServerManager, fallback về _npcData cục bộ
        NpcData data = _npcData;
        if (NpcServerManager.Instance != null && NpcServerManager.Instance.TryGetNpcData(npcNetworkId, out var cached))
            data = cached;

        if (data == null) return;

        StartCoroutine(FetchDialogueAndSend(data, clientId));
    }

    private IEnumerator FetchDialogueAndSend(NpcData data, ulong clientId)
    {
        int userId = ServerPlayerDataManager.Instance != null
            ? ServerPlayerDataManager.Instance.GetUserIdFromClientId(clientId)
            : PlayerPrefs.GetInt("USER_ID");

        string apiBase = NpcServerManager.Instance?.ApiBase ?? "http://localhost:5000";
        string body    = JsonUtility.ToJson(new InteractPayload { npc_id = data.npc_id, player_id = userId });

        using var req = PostJson($"{apiBase}/api/npc/interact", body);
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        data.dialogue_text = "Xin chào, ta có thể giúp gì cho ngươi?";
        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<InteractResponse>(req.downloadHandler.text);
            if (!string.IsNullOrEmpty(resp?.dialogue_text))
                data.dialogue_text = resp.dialogue_text;
        }

        OpenMenuClientRpc(JsonUtility.ToJson(data), TargetClient(clientId));
    }

    [ClientRpc]
    private void OpenMenuClientRpc(string npcDataJson, ClientRpcParams clientRpcParams = default)
    {
        var data = JsonUtility.FromJson<NpcData>(npcDataJson);
        NpcMenuUI.GetOrFind()?.Open(data, this);
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

        int userId = ServerPlayerDataManager.Instance != null
            ? ServerPlayerDataManager.Instance.GetUserIdFromClientId(clientId)
            : PlayerPrefs.GetInt("USER_ID");

        string apiBase = NpcServerManager.Instance?.ApiBase ?? "http://localhost:5000";
        string url     = $"{apiBase}/api/npc/shop?npcId={data.npc_id}&playerId={userId}";

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
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
        int userId = ServerPlayerDataManager.Instance != null
            ? ServerPlayerDataManager.Instance.GetUserIdFromClientId(clientId)
            : PlayerPrefs.GetInt("USER_ID");

        if (data == null)
        {
            Debug.LogWarning("[Buy] NpcData null — không thể mua.");
            SendBuyResult(clientId, false, "Lỗi: NPC data không tồn tại.", 0);
            yield break;
        }

        string jwtToken = PlayerPrefs.GetString("JWT_TOKEN", "");
        if (string.IsNullOrEmpty(jwtToken))
        {
            Debug.LogWarning("[Buy] JWT_TOKEN trống — chưa đăng nhập.");
            SendBuyResult(clientId, false, "Chưa đăng nhập. Vui lòng đăng nhập lại.", 0);
            yield break;
        }

        string apiBase = NpcServerManager.Instance?.ApiBase ?? "http://localhost:5000";
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

    private static UnityWebRequest PostJson(string url, string json)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    [System.Serializable] private class InteractPayload  { public int npc_id, player_id; }
    [System.Serializable] private class InteractResponse { public string dialogue_text; }
    [System.Serializable] private class BuyPayload       { public int npcId, shopItemId, quantity; }
    [System.Serializable] private class BuyResponse      { public bool success; public string message; public int playerGold; }
}
