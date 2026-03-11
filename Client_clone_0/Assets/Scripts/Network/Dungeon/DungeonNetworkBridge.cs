using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// DungeonNetworkBridge — Cầu nối RPC giữa Client và Host Chính.
///
/// NGUYÊN TẮC:
///   Client KHÔNG BAO GIỜ tự gọi StartHost() hay StartClient() cho phó bản.
///   TẤT CẢ quyết định được thực hiện bởi HOST CHÍNH:
///     1. Host chính kiểm tra session DB
///     2. Host chính quyết định lệnh gửi về client (start_solo_host / start_multi_host / join_host)
///     3. Host chính remove client khỏi session chính
///     4. Client chỉ thực thi lệnh nhận được
///
/// ┌─────────────────────────────────────────────────────────────┐
/// │  Client    →  RequestDungeonEntryServerRpc(dungeonId, ...)  │
/// │  Host Chính → Check DB session                             │
/// │  Solo       → Lệnh StartSoloHost → client StartHost()      │
/// │  Multi      → Host chính spawn dungeon host (máy server)   │
/// │             → Lệnh JoinHost (ip:port) → client StartClient │
/// │             → Kick client ra khỏi session chính            │
/// └─────────────────────────────────────────────────────────────┘
/// </summary>
public class DungeonNetworkBridge : NetworkBehaviour
{
    private static DungeonNetworkBridge _instance;
    public static DungeonNetworkBridge Instance => _instance;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
    }

    public override void OnDestroy()
    {
        if (_instance == this) _instance = null;
        base.OnDestroy();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLIENT → HOST CHÍNH
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Client gửi yêu cầu tham gia phó bản lên HOST CHÍNH.
    /// Host chính xử lý hoàn toàn:
    ///   - Kiểm tra session DB
    ///   - Quyết định lệnh gửi về (solo host / multi host / join existing)
    ///   - Ghi nhận session vào DB nếu cần
    ///   - Remove client khỏi session chính
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestDungeonEntryServerRpc(int dungeonId, int mapId, string dungeonType,
                                              ulong requestingClientId,
                                              ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[DungeonBridge][HostChính] Client {requestingClientId} → dungeon {dungeonId} ({dungeonType})");
        StartCoroutine(HandleDungeonEntryOnServer(dungeonId, mapId, dungeonType, requestingClientId));
    }

    // ─────────────────────────────────────────────────────────────────────
    //  SERVER-SIDE LOGIC (chỉ chạy trên host chính)
    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator HandleDungeonEntryOnServer(int dungeonId, int mapId, string dungeonType,
                                                    ulong clientId)
    {
        if (dungeonType == "solo")
        {
            // Solo: Host chính ra lệnh cho client làm host solo dungeon trên máy của client đó
            Debug.Log($"[DungeonBridge][HostChính] Solo → lệnh StartSoloHost cho client {clientId}");
            SendCommandToClient(clientId, DungeonCommand.StartSoloHost, dungeonId, mapId, "", 0, -1);
            yield return new WaitForEndOfFrame();
            KickClient(clientId);
            yield break;
        }

        // Multi: kiểm tra session DB trước
        bool apiDone            = false;
        DungeonSessionData existingSession = null;

        APIClient.Instance.GetDungeonSession(dungeonId,
            s  => { existingSession = s; apiDone = true; },
            _  => { apiDone = true; });

        yield return new WaitUntil(() => apiDone);

        if (existingSession != null && existingSession.status != "ended"
            && existingSession.current_players < existingSession.max_players)
        {
            // Session đã có chỗ trống → Host chính ra lệnh join cho client
            Debug.Log($"[DungeonBridge][HostChính] Session {existingSession.session_id} exists → lệnh JoinHost cho client {clientId}");

            bool joinDone = false;
            APIClient.Instance.JoinDungeonSession(existingSession.session_id,
                _ => joinDone = true, _ => joinDone = true);
            yield return new WaitUntil(() => joinDone);

            SendCommandToClient(clientId, DungeonCommand.JoinHost,
                dungeonId, mapId,
                existingSession.host_ip, existingSession.host_port,
                existingSession.session_id);
        }
        else
        {
            // Chưa có session → Host chính TỰ spawn dungeon host riêng (máy server/host),
            // đăng ký session vào DB, rồi gửi lệnh JoinHost cho client.
            // ── SPAWN DUNGEON HOST ───────────────────────────────────────────────────
            // Trên dedicated server: Process.Start(headlessServerPath, $"--dungeon {dungeonId} --port ...").
            // Trong mô hình player-hosted hiện tại: host chính dùng port riêng cho dungeon.
            // TODO: thêm Process.Start + WaitForSeconds nếu dùng headless process.
            // ────────────────────────────────────────────────────────────────────────
            string dungeonHostIp   = DungeonManager.GetLocalIP();
            int    dungeonHostPort = 7778; // TODO: cấp phát port động nếu nhiều dungeon đồng thời

            Debug.Log($"[DungeonBridge][HostChính] No session → spawn dungeon host {dungeonHostIp}:{dungeonHostPort}, rồi JoinHost cho client {clientId}");

            bool createDone  = false;
            DungeonSessionData newSession = null;
            APIClient.Instance.CreateDungeonSession(dungeonId, dungeonHostIp, dungeonHostPort,
                s => { newSession = s; createDone = true; },
                _ => { createDone = true; });
            yield return new WaitUntil(() => createDone);

            if (newSession == null)
            {
                Debug.LogError($"[DungeonBridge][HostChính] Tạo dungeon session thất bại cho dungeon {dungeonId}");
                yield break;
            }

            SendCommandToClient(clientId, DungeonCommand.JoinHost, dungeonId, mapId,
                dungeonHostIp, dungeonHostPort, newSession.session_id);
        }

        yield return new WaitForEndOfFrame();
        KickClient(clientId);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HOST CHÍNH → CLIENT
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Host chính gửi lệnh về đúng một client.
    /// cmd: "start_solo_host" | "start_multi_host" | "join_host"
    /// </summary>
    [ClientRpc]
    public void DungeonCommandClientRpc(string cmd, int dungeonId, int mapId,
                                         string hostIp, int hostPort, int sessionId,
                                         ClientRpcParams rpcParams = default)
    {
        // Host chính nhận ClientRpc về chính mình nhưng không xử lý
        if (IsServer && !IsClient) return;

        Debug.Log($"[DungeonBridge][Client] Lệnh từ host chính: {cmd}, dungeon={dungeonId}, {hostIp}:{hostPort}");
        DungeonManager.Instance?.ExecuteDungeonCommand(cmd, dungeonId, mapId, hostIp, hostPort, sessionId);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HELPERS
    // ─────────────────────────────────────────────────────────────────────

    private void SendCommandToClient(ulong clientId, DungeonCommand cmd,
                                      int dungeonId, int mapId,
                                      string hostIp, int hostPort, int sessionId)
    {
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { clientId } }
        };
        DungeonCommandClientRpc(cmd.ToString(), dungeonId, mapId, hostIp, hostPort, sessionId, target);
    }

    private void KickClient(ulong clientId)
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkManager.Singleton.DisconnectClient(clientId);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  CLIENT → HOST RPCs
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Client gọi lên Host trước khi tự ngắt kết nối để vào phó bản thử thách 1 mình.
    /// Host sẽ:
    ///   1. Kick client ra khỏi session hiện tại
    ///   2. Gửi lại DungeonSoloReadyClientRpc để client biết đã được "release"
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestSoloDungeonEntryServerRpc(int dungeonId, int mapId, ulong requestingClientId,
                                                  ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[DungeonBridge] Client {requestingClientId} requests SOLO entry for dungeon {dungeonId} (map {mapId}).");

        // Thông báo lại đúng client: "Bạn được phép rời, hãy tự start Host"
        var target = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { requestingClientId } }
        };
        DungeonSoloReadyClientRpc(dungeonId, mapId, target);

        // Kick client sau một frame để client nhận được RPC trước
        StartCoroutine(KickClientAfterRpc(requestingClientId));
    }

    /// <summary>
    /// Client gọi lên Host khi muốn vào phó bản multi mà chưa có session nào.
    /// Host sẽ:
    ///   1. Tạo session qua REST API (host_ip = IP của host machine)
    ///   2. Gửi DungeonMultiReadyClientRpc về client kèm thông tin host mới
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestMultiDungeonHostCreationServerRpc(int dungeonId, int mapId, ulong requestingClientId,
                                                          ServerRpcParams rpcParams = default)
    {
        Debug.Log($"[DungeonBridge] Client {requestingClientId} requests HOST creation for multi dungeon {dungeonId}.");
        // DungeonManager.Instance sẽ xử lý: tạo session rồi gọi ClientRpc về
        DungeonManager.Instance?.OnClientRequestedMultiHost(dungeonId, mapId, requestingClientId);
    }

    // ─────────────────────────────────────────────────────────────────────
    //  HOST → CLIENT RPCs
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// HOST → CLIENT: Phó bản solo đã sẵn sàng. Client ngắt kết nối và tự StartHost().
    /// </summary>
    [ClientRpc]
    public void DungeonSoloReadyClientRpc(int dungeonId, int mapId, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner && IsServer) return; // host nhận nhưng bỏ qua
        Debug.Log($"[DungeonBridge] Solo dungeon {dungeonId} ready — starting local host.");
        DungeonManager.Instance?.OnSoloDungeonApproved(dungeonId, mapId);
    }

    /// <summary>
    /// HOST → CLIENT: Session multi đã được tạo. Client ngắt kết nối rồi connect tới host mới.
    /// </summary>
    [ClientRpc]
    public void DungeonMultiSessionReadyClientRpc(int sessionId, int dungeonId, int mapId,
                                                   string hostIp, int hostPort,
                                                   ClientRpcParams rpcParams = default)
    {
        if (!IsOwner && IsServer) return;
        Debug.Log($"[DungeonBridge] Multi dungeon session {sessionId} ready at {hostIp}:{hostPort}.");
        DungeonManager.Instance?.OnMultiSessionReady(sessionId, dungeonId, mapId, hostIp, hostPort);
    }

    private System.Collections.IEnumerator KickClientAfterRpc(ulong clientId)
    {
        yield return new WaitForEndOfFrame();
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            NetworkManager.Singleton.DisconnectClient(clientId);
    }
}

/// <summary>Các loại lệnh host chính gửi về client.</summary>
public enum DungeonCommand
{
    /// <summary>Client tạo host solo trên máy mình (sau khi host chính ra lệnh)</summary>
    StartSoloHost,

    /// <summary>Client connect vào dungeon host (host chính đã spawn sẵn hoặc đã tồn tại)</summary>
    JoinHost
}
