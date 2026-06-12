using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;

// Server-side NetworkBehaviour: xử lý yêu cầu chuyển zone từ client.
// Flow:
// 1. Client bước vào ZoneTransitionTrigger (BoxCollider2D)
// 2. Client gọi RequestZoneTransferServerRpc(targetZoneId, targetEntryPointId)
// 3. Server validate → save position qua API → fetch địa chỉ zone mới
// 4. Server gọi BeginZoneTransferClientRpc(ip, port, entryPointId) đến đúng client đó
// 5. Client ngắt kết nối → kết nối lại zone mới
// Gắn vào: persistent NetworkObject trong server scene.
[DisallowMultipleComponent]
public class ZoneTransitionManager : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private ZoneServerConfig _config;

    [Tooltip("Cooldown giữa 2 lần chuyển zone liên tiếp (giây) — ngăn spam")]
    [SerializeField] private float _transferCooldown = 2f;

    // Theo dõi cooldown mỗi client
    private readonly System.Collections.Generic.Dictionary<ulong, float> _lastTransferTime = new();

    // Server RPCs

    // Client gọi khi muốn chuyển zone.
    // Tham số targetMapId: Map ID đích
    // Tham số targetZoneId: Zone ID đích trong map đó
    // Tham số entryPointId: Entry point index trong zone đích
    [ServerRpc(RequireOwnership = false)]
    public void RequestZoneTransferServerRpc(int targetMapId, int targetZoneId, int entryPointId,
                                              ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        // 1 — Rate limit
        float now = Time.time;
        if (_lastTransferTime.TryGetValue(clientId, out float lastTime) &&
            now - lastTime < _transferCooldown)
        {
            { /* Cảnh báo: Client {clientId} spam zone transfer → bỏ qua */ }
            return;
        }
        _lastTransferTime[clientId] = now;

        // 2 — Validate không tự chuyển vào chính zone này
        int currentMapId  = _config != null ? _config.mapId  : -1;
        int currentZoneId = _config != null ? _config.zoneId : -1;
        if (targetMapId == currentMapId && targetZoneId == currentZoneId)
        {
            { /* Cảnh báo: Client {clientId} yêu cầu chuyển vào chính zone hiện tại → bỏ qua */ }
            return;
        }

        // 3 — Lấy vị trí player để save
        Vector3 playerPos = GetPlayerPosition(clientId);

        StartCoroutine(ProcessZoneTransfer(clientId, targetMapId, targetZoneId, entryPointId, playerPos));
    }

    // Client RPCs

    // Server gửi cho đúng client: thông tin zone server mới để kết nối.
    [ClientRpc]
    private void BeginZoneTransferClientRpc(string zoneServerIp, ushort zoneServerPort,
                                             int entryPointId, string targetSceneName,
                                             ClientRpcParams rpcParams = default)
    {
        // Client sẽ xử lý trong ZoneConnectionHandler
        ZoneConnectionHandler.Instance?.HandleZoneTransfer(zoneServerIp, zoneServerPort,
                                                            entryPointId, targetSceneName);
    }

    // Server thông báo transfer thất bại — client hiển thị thông báo lỗi.
    [ClientRpc]
    private void ZoneTransferFailedClientRpc(string reason, ClientRpcParams rpcParams = default)
    {
        { /* Cảnh báo: Zone transfer thất bại: {reason} */ }
        // TODO: client hiển thị UI thông báo lỗi
    }

    // Internal: Process Transfer

    private System.Collections.IEnumerator ProcessZoneTransfer(
        ulong clientId, int targetMapId, int targetZoneId, int entryPointId, Vector3 playerPos)
    {
        string apiBase = _config != null ? _config.apiBaseUrl : "http://localhost:5247/api";

        // 1 — Lấy userId của client
        var session = ZonePlayerSessionManager.Instance?.GetSession(clientId);
        if (session == null)
        {
            SendTransferFailed(clientId, "Session không tồn tại.");
            yield break;
        }

        // 2 — Save vị trí hiện tại của player lên API
        string saveUrl = $"{apiBase}/player/{session.UserId}/position";
        string saveBody = $"{{\"x\":{playerPos.x:F2},\"y\":{playerPos.y:F2}," +
                          $"\"mapId\":{targetMapId},\"zoneId\":{targetZoneId}}}";

        using (var saveReq = new UnityEngine.Networking.UnityWebRequest(saveUrl, "PUT")
        {
            uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(Encoding.UTF8.GetBytes(saveBody)),
            downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer()
        })
        {
            saveReq.SetRequestHeader("Content-Type", "application/json");
            string apiKey = Environment.GetEnvironmentVariable("ZONE_API_KEY") ?? "dev-zone-key";
            saveReq.SetRequestHeader("X-Zone-Api-Key", apiKey);
            yield return saveReq.SendWebRequest();

            if (saveReq.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                { /* Cảnh báo: Save vị trí thất bại: {saveReq.error}  vẫn tiếp tục transfer */ }
        }

        // 3 — Fetch địa chỉ zone server đích từ API
        string addrUrl = $"{apiBase}/zone/address?mapId={targetMapId}&zoneId={targetZoneId}";
        string apiKey2 = Environment.GetEnvironmentVariable("ZONE_API_KEY") ?? "dev-zone-key";

        ZoneAddressResponse zoneAddr = null;
        using (var addrReq = UnityEngine.Networking.UnityWebRequest.Get(addrUrl))
        {
            addrReq.SetRequestHeader("X-Zone-Api-Key", apiKey2);
            yield return addrReq.SendWebRequest();

            if (addrReq.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                { /* Lỗi: Không lấy được địa chỉ zone */ }
                SendTransferFailed(clientId, "Zone đích chưa sẵn sàng.");
                yield break;
            }

            try
            {
                zoneAddr = JsonUtility.FromJson<ZoneAddressResponse>(addrReq.downloadHandler.text);
            }
            catch (Exception ex)
            {
                { /* Lỗi: Parse ZoneAddress thất bại: {ex.Message} */ }
                SendTransferFailed(clientId, "Lỗi server nội bộ.");
                yield break;
            }
        }

        if (zoneAddr == null || string.IsNullOrEmpty(zoneAddr.ip))
        {
            SendTransferFailed(clientId, "Zone đích không tồn tại trong registry.");
            yield break;
        }

        // 4 — Gửi ClientRpc đến đúng client với thông tin zone mới
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };

        { /* Redirect client {clientId} → */ }

        BeginZoneTransferClientRpc(zoneAddr.ip, (ushort)zoneAddr.port, entryPointId,
                                   zoneAddr.sceneName, rpcParams);
    }

    private void SendTransferFailed(ulong clientId, string reason)
    {
        var rpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { clientId } }
        };
        ZoneTransferFailedClientRpc(reason, rpcParams);
    }

    private Vector3 GetPlayerPosition(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
            return client.PlayerObject.transform.position;
        return Vector3.zero;
    }

    // DTO

    [Serializable]
    private class ZoneAddressResponse
    {
        public string ip;
        public int    port;
        public string sceneName;
        public bool   isOnline;
    }
}
