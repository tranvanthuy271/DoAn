using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// DungeonManager - Singleton quan ly luong vao/ra pho ban phia CLIENT.
///
/// NGUYEN TAC QUAN TRONG:
///   Client KHONG BAO GIO tu quyet dinh StartHost() hay StartClient().
///   Chi thuc thi khi nhan duoc lenh tu HOST CHINH qua DungeonCommandClientRpc.
///
/// SOLO DUNGEON:
///   Client -> RequestDungeonEntryServerRpc(solo) -> Host chinh kiem tra
///   <- DungeonCommandClientRpc("StartSoloHost") - host chinh ra lenh
///   Host chinh kick client
///   Client: Shutdown -> LoadScene -> StartHost()  [tren may cua client do]
///
/// MULTI - Chua co session:
///   Client -> RequestDungeonEntryServerRpc(multi) -> Host chinh kiem tra DB
///   Host chinh TU spawn dungeon host (may server), POST /session/create vao DB
///   <- DungeonCommandClientRpc("JoinHost", ip, port, sessionId)
///   Host chinh kick client
///   Client: Shutdown -> LoadScene -> StartClient(ip:port)
///
/// MULTI - Da co session:
///   Client -> RequestDungeonEntryServerRpc(multi) -> Host chinh kiem tra DB
///   Host chinh: API JoinSession(sessionId)
///   <- DungeonCommandClientRpc("JoinHost", ip, port, sessionId)
///   Host chinh kick client
///   Client: Shutdown -> LoadScene -> StartClient(ip:port)
/// </summary>
public class DungeonManager : MonoBehaviour
{
    public static DungeonManager Instance { get; private set; }

    [Header("Dungeon State")]
    [SerializeField] private bool _isInDungeon;
    [SerializeField] private int  _activeDungeonId = -1;
    [SerializeField] private int  _activeSessionId = -1;
    [SerializeField] private bool _isHostingDungeon;

    public bool IsInDungeon      => _isInDungeon;
    public int  ActiveDungeonId  => _activeDungeonId;
    public int  ActiveSessionId  => _activeSessionId;
    public bool IsHostingDungeon => _isHostingDungeon;

    public event Action<string> OnDungeonStatusMessage;
    public event Action         OnDungeonEntered;
    public event Action         OnDungeonExited;

    private DungeonConfigData _pendingConfig;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========================================================================
    //  ENTRY POINT - Gui yeu cau len host chinh (CLIENT GOI)
    // ========================================================================

    /// <summary>
    /// Gui yeu cau vao pho ban len HOST CHINH qua ServerRpc.
    /// Client KHONG tu xu ly - chi gui request va doi lenh tra ve.
    /// </summary>
    public void EnterDungeon(DungeonConfigData config)
    {
        if (config == null) return;

        var nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogWarning("[DungeonManager] Khong tim thay NetworkManager.");
            return;
        }

        var bridge = DungeonNetworkBridge.Instance;
        if (bridge == null)
        {
            Debug.LogWarning("[DungeonManager] DungeonNetworkBridge khong ton tai. Kiem tra lai scene.");
            return;
        }

        _pendingConfig = config;
        Notify("Dang gui yeu cau vao " + config.dungeon_name + "...");

        // Gui request len host chinh - host chinh se kiem tra DB va gui lenh lai
        bridge.RequestDungeonEntryServerRpc(
            config.dungeon_id,
            config.map_id,
            config.dungeon_type,
            nm.LocalClientId);
    }

    /// <summary>Roi pho ban va quay ve overworld.</summary>
    public void ExitDungeon(string overworldSceneName = "GameScene")
    {
        StartCoroutine(ExitDungeonCoroutine(overworldSceneName));
    }

    // ========================================================================
    //  NHAN LENH TU HOST CHINH (goi boi DungeonNetworkBridge.DungeonCommandClientRpc)
    // ========================================================================

    /// <summary>
    /// Thuc thi lenh nhan duoc tu host chinh.
    /// Client KHONG tu quyet dinh - chi thuc thi lenh nay.
    /// </summary>
    public void ExecuteDungeonCommand(string cmd, int dungeonId, int mapId,
                                       string hostIp, int hostPort, int sessionId)
    {
        if (_pendingConfig == null || _pendingConfig.dungeon_id != dungeonId)
        {
            StartCoroutine(FetchConfigThenExecute(cmd, dungeonId, mapId, hostIp, hostPort, sessionId));
            return;
        }

        var cfg = _pendingConfig;
        _pendingConfig = null;
        StartCoroutine(ExecuteCommand(cmd, cfg, hostIp, hostPort, sessionId));
    }

    private IEnumerator FetchConfigThenExecute(string cmd, int dungeonId, int mapId,
                                                string hostIp, int hostPort, int sessionId)
    {
        bool done = false;
        DungeonConfigData found = null;
        APIClient.Instance.GetDungeonList(
            list => { foreach (var d in list) if (d.dungeon_id == dungeonId) { found = d; break; } done = true; },
            _    => done = true);
        yield return new WaitUntil(() => done);
        if (found != null) yield return ExecuteCommand(cmd, found, hostIp, hostPort, sessionId);
    }

    private IEnumerator ExecuteCommand(string cmd, DungeonConfigData config,
                                        string hostIp, int hostPort, int sessionId)
    {
        if (cmd == DungeonCommand.StartSoloHost.ToString())
        {
            // Host chinh ra lenh: lam host solo tren may cua minh
            yield return StartCoroutine(DoShutdownAndStartHost(config, isMulti: false));
        }
        else if (cmd == DungeonCommand.JoinHost.ToString())
        {
            // Host chinh ra lenh: join session da co san
            _isInDungeon      = true;
            _isHostingDungeon = false;
            _activeDungeonId  = config.dungeon_id;
            _activeSessionId  = sessionId;
            Notify("Dang vao " + config.dungeon_name + "...");
            yield return StartCoroutine(DoShutdownAndStartClient(config.scene_name, hostIp, hostPort));
        }
        else
        {
            Debug.LogError("[DungeonManager] Lenh khong hop le: " + cmd);
        }
    }

    // ========================================================================
    //  SHUTDOWN + START HOST (CHI CHAY KHI HOST CHINH RA LENH)
    // ========================================================================

    private IEnumerator DoShutdownAndStartHost(DungeonConfigData config, bool isMulti)
    {
        Notify("Dang khoi dong " + config.dungeon_name + "...");

        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsHost || nm.IsClient || nm.IsServer))
        {
            nm.Shutdown();
            yield return new WaitForSeconds(0.35f);
        }

        if (string.IsNullOrEmpty(config.scene_name))
        {
            Debug.LogError("[DungeonManager] dungeon_config.scene_name trong! Kiem tra DB.");
            yield break;
        }

        _isInDungeon      = true;
        _isHostingDungeon = true;
        _activeDungeonId  = config.dungeon_id;

        Notify("Dang tai scene " + config.scene_name + "...");
        yield return SceneManager.LoadSceneAsync(config.scene_name);
        yield return null; // cho Awake/Start chay

        nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[DungeonManager] NetworkManager khong ton tai sau khi load scene!");
            yield break;
        }

        nm.StartHost();
        Debug.Log("[DungeonManager] StartHost() - lenh tu host chinh. Dungeon: " + config.dungeon_name);

        // Neu la multi host: dang ky session vao DB de player khac co the join
        if (isMulti) RegisterMultiSession(config);

        OnDungeonEntered?.Invoke();
        Notify("Da vao pho ban!");
    }

    // ========================================================================
    //  SHUTDOWN + JOIN HOST (CHI CHAY KHI HOST CHINH RA LENH)
    // ========================================================================

    private IEnumerator DoShutdownAndStartClient(string sceneName, string hostIp, int hostPort)
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsHost || nm.IsClient || nm.IsServer))
        {
            nm.Shutdown();
            yield return new WaitForSeconds(0.35f);
        }

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[DungeonManager] scene_name trong!");
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(sceneName);
        yield return null;

        nm = NetworkManager.Singleton;
        if (nm == null) { Debug.LogError("[DungeonManager] NetworkManager khong ton tai!"); yield break; }

        var transport = nm.GetComponent<UnityTransport>();
        if (transport != null)
            transport.SetConnectionData(hostIp, (ushort)hostPort);

        nm.StartClient();
        Debug.Log("[DungeonManager] StartClient() -> " + hostIp + ":" + hostPort + " - lenh tu host chinh.");

        OnDungeonEntered?.Invoke();
        Notify("Da vao pho ban!");
    }

    // ========================================================================
    //  DANG KY SESSION SAU KHI StartHost (multi only)
    // ========================================================================

    private void RegisterMultiSession(DungeonConfigData config)
    {
        string ip   = GetLocalIP();
        int    port = 7777;

        APIClient.Instance.CreateDungeonSession(config.dungeon_id, ip, port,
            s =>
            {
                _activeSessionId = s.session_id;
                Debug.Log("[DungeonManager] Session " + s.session_id + " da dang ky: " + ip + ":" + port);
            },
            err => Debug.LogWarning("[DungeonManager] Dang ky session that bai: " + err)
        );
    }

    // ========================================================================
    //  EXIT DUNGEON
    // ========================================================================

    private IEnumerator ExitDungeonCoroutine(string overworldScene)
    {
        Notify("Dang roi pho ban...");

        if (_activeSessionId > 0)
        {
            bool done = false;
            if (_isHostingDungeon)
                APIClient.Instance.EndDungeonSession(_activeSessionId, _ => done = true, _ => done = true);
            else
                APIClient.Instance.LeaveDungeonSession(_activeSessionId, _ => done = true, _ => done = true);
            yield return new WaitUntil(() => done);
        }

        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsHost || nm.IsClient || nm.IsServer))
        {
            nm.Shutdown();
            yield return new WaitForSeconds(0.35f);
        }

        _isInDungeon      = false;
        _isHostingDungeon = false;
        _activeDungeonId  = -1;
        _activeSessionId  = -1;

        SceneManager.LoadScene(overworldScene);
        OnDungeonExited?.Invoke();
    }

    // ========================================================================
    //  UTILITIES
    // ========================================================================

    private void Notify(string msg)
    {
        Debug.Log("[DungeonManager] " + msg);
        OnDungeonStatusMessage?.Invoke(msg);
    }

    public static string GetLocalIP()
    {
        try
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[DungeonManager] GetLocalIP failed: " + ex.Message);
        }
        return "127.0.0.1";
    }

    // ========================================================================
    //  CALLBACKS TU DungeonNetworkBridge
    // ========================================================================

    /// <summary>
    /// Gọi bởi DungeonSoloReadyClientRpc — host chính đã cho phép, client tự StartHost solo.
    /// </summary>
    public void OnSoloDungeonApproved(int dungeonId, int mapId)
    {
        ExecuteDungeonCommand(DungeonCommand.StartSoloHost.ToString(), dungeonId, mapId, "", 0, -1);
    }

    /// <summary>
    /// Gọi bởi DungeonMultiSessionReadyClientRpc — session multi đã sẵn sàng, client join vào.
    /// </summary>
    public void OnMultiSessionReady(int sessionId, int dungeonId, int mapId, string hostIp, int hostPort)
    {
        ExecuteDungeonCommand(DungeonCommand.JoinHost.ToString(), dungeonId, mapId, hostIp, hostPort, sessionId);
    }

    /// <summary>
    /// Gọi bởi RequestMultiDungeonHostCreationServerRpc — chạy trên host chính.
    /// Tạo session rồi gửi DungeonMultiSessionReadyClientRpc về đúng client.
    /// </summary>
    public void OnClientRequestedMultiHost(int dungeonId, int mapId, ulong requestingClientId)
    {
        StartCoroutine(CreateMultiSessionAndNotify(dungeonId, mapId, requestingClientId));
    }

    private IEnumerator CreateMultiSessionAndNotify(int dungeonId, int mapId, ulong requestingClientId)
    {
        string hostIp   = GetLocalIP();
        int    hostPort = 7778;

        bool done = false;
        DungeonSessionData newSession = null;
        APIClient.Instance.CreateDungeonSession(dungeonId, hostIp, hostPort,
            s => { newSession = s; done = true; },
            _ => done = true);
        yield return new WaitUntil(() => done);

        if (newSession == null)
        {
            Debug.LogError("[DungeonManager] Tao dungeon session that bai cho dungeon " + dungeonId);
            yield break;
        }

        var bridge = DungeonNetworkBridge.Instance;
        if (bridge == null) yield break;

        var target = new Unity.Netcode.ClientRpcParams
        {
            Send = new Unity.Netcode.ClientRpcSendParams { TargetClientIds = new[] { requestingClientId } }
        };
        bridge.DungeonMultiSessionReadyClientRpc(newSession.session_id, dungeonId, mapId, hostIp, hostPort, target);

        yield return new WaitForEndOfFrame();
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.IsServer)
            nm.DisconnectClient(requestingClientId);
    }
}
