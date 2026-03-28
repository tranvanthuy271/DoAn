using System.Collections;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Helper tạm thời (DontDestroyOnLoad) để xử lý quá trình chuyển map an toàn.
///
/// Flow:
///   1. Lưu vị trí đến (PortalArrivalHandler)
///   2. Unregister host trên map cũ (nếu đang là host)
///   3. Persist Canvas UI
///   4. Shutdown NGO, LoadScene
///   5. POST /api/map/host/register → server nói "you_are_host" hay không
///      - Nếu you_are_host=true  → StartHost() → đăng ký heartbeat
///      - Nếu you_are_host=false → StartClient(host_ip, host_port)
/// </summary>
public class MapTravelHelper : MonoBehaviour
{
    // ── Static tracking: player này hiện đang là host của map nào ──
    public static bool IsCurrentHost    { get; private set; } = false;
    public static int  CurrentHostMapId { get; private set; } = -1;

    private string _destScene;
    private float  _destX;
    private float  _destY;
    private int    _destMapId;
    private int    _srcMapId;

    private string ApiBase =>
        APIClient.Instance != null
            ? APIClient.Instance.baseURL.TrimEnd('/')
            : "http://localhost:5000/api";

    /// <summary>
    /// Gọi ngay sau khi tạo component. Bắt đầu coroutine chuyển scene.
    /// </summary>
    /// <param name="srcMapId">MapId của scene HIỆN TẠI (để unregister host nếu cần).</param>
    public void Execute(string destScene, float destX, float destY, int destMapId, int srcMapId = -1)
    {
        _destScene = destScene;
        _destX     = destX;
        _destY     = destY;
        _destMapId = destMapId;
        _srcMapId  = srcMapId;
        StartCoroutine(DoTravel());
    }

    private IEnumerator DoTravel()
    {
        int userId = PlayerPrefs.GetInt("USER_ID", 0);

        // ── 1. Lưu vị trí đến (static, survive qua scene load) ──
        PortalArrivalHandler.PendingDestX = _destX;
        PortalArrivalHandler.PendingDestY = _destY;
        PortalArrivalHandler.PendingMapId = _destMapId;

        // ── 2. Unregister host trên map cũ ──
        if (IsCurrentHost && _srcMapId >= 0 && userId > 0)
        {
            Debug.Log($"[MapTravelHelper] Unregister host: map={_srcMapId} player={userId}");
            yield return UnregisterHostRequest(_srcMapId, userId);
            IsCurrentHost    = false;
            CurrentHostMapId = -1;
        }

        // ── 3. Persist Canvas UI trước khi scene unload ──
        PersistCanvasObjects();

        // ── 4. Shutdown NGO — chờ thực sự ──
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsClient || nm.IsHost || nm.IsServer))
        {
            nm.Shutdown();
            float waited = 0f;
            while (nm != null && nm.IsListening && waited < 2f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // ── 5. Load scene mới ──
        Debug.Log($"[MapTravelHelper] Loading scene '{_destScene}'...");
        yield return SceneManager.LoadSceneAsync(_destScene);
        yield return null; // để Awake/Start của scene mới chạy trước

        // ── 6. Reset auth flag ──
        ClientAuthSender.Reset();

        // ── 7. Thử đăng ký làm host (atomic: server quyết định ai là host) ──
        string localIp   = GetLocalIP();
        ushort localPort = GetNetworkPort();

        var regResult = new HostRegisterResult();
        if (userId > 0)
        {
            yield return RegisterHostRequest(_destMapId, localIp, localPort, userId, regResult);
        }
        else
        {
            // Không có user id → cứ làm host (fallback)
            regResult.youAreHost = true;
            regResult.hostIp     = localIp;
            regResult.hostPort   = localPort;
            regResult.done       = true;
        }

        // ── 8. Khởi động NGO ──
        nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[MapTravelHelper] NetworkManager không tồn tại sau khi load scene!");
            Destroy(gameObject);
            yield break;
        }

        var transport = nm.GetComponent<UnityTransport>();
        var nmCustom  = Object.FindObjectOfType<NetworkManagerCustom>();

        if (regResult.youAreHost)
        {
            // ─── Trở thành HOST ───
            if (transport != null)
            {
                transport.ConnectionData.Address = "0.0.0.0";
                transport.ConnectionData.Port    = localPort;
            }

            nm.StartHost();
            IsCurrentHost    = true;
            CurrentHostMapId = _destMapId;
            Debug.Log($"[MapTravelHelper] ★ StartHost() trong scene '{_destScene}' port={localPort}. Đến ({_destX},{_destY})");

            if (nmCustom != null) nmCustom.RegisterAuthMessageHandler();

            // Pre-populate ServerPlayerDataManager cho host (clientId = 0)
            if (userId > 0 && ServerPlayerDataManager.Instance != null)
            {
                ulong hostClientId = nm.LocalClientId; // = 0 cho host
                ServerPlayerDataManager.Instance.LoadPlayerDataForClient(
                    hostClientId, userId,
                    onSuccess: (data) => Debug.Log($"[MapTravelHelper] ✓ Player data clientId={hostClientId}: {data.character_name} ({data.element_type})"),
                    onError:   (err)  => Debug.LogWarning($"[MapTravelHelper] ⚠ Player data load error: {err}")
                );
            }

            // Bắt đầu heartbeat để giữ session sống
            if (userId > 0)
                StartCoroutine(HeartbeatCoroutine(_destMapId, userId));
        }
        else
        {
            // ─── Join HOST khác làm CLIENT ───
            string targetIp   = regResult.hostIp;
            ushort targetPort = regResult.hostPort;

            if (transport != null)
            {
                transport.ConnectionData.Address = targetIp;
                transport.ConnectionData.Port    = targetPort;
            }

            nm.StartClient();
            Debug.Log($"[MapTravelHelper] ★ StartClient() → {targetIp}:{targetPort} trong scene '{_destScene}'. Đến ({_destX},{_destY})");
        }

        // ── 9. Self-destroy ──
        Destroy(gameObject);
    }

    // ────────────────────────────────────────────────────────────────────────
    //  HTTP helpers
    // ────────────────────────────────────────────────────────────────────────

    private IEnumerator RegisterHostRequest(int mapId, string ip, ushort port, int playerId, HostRegisterResult result)
    {
        var body = JsonUtility.ToJson(new MapHostRegisterDto
        {
            map_id    = mapId,
            host_ip   = ip,
            host_port = (int)port,
            player_id = playerId
        });

        string url = $"{ApiBase}/map/host/register";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<MapHostRegisterResponse>(req.downloadHandler.text);
            result.youAreHost = resp.you_are_host;
            result.hostIp     = resp.host_ip;
            result.hostPort   = (ushort)resp.host_port;
            Debug.Log($"[MapTravelHelper] Register host → you_are_host={resp.you_are_host}, host={resp.host_ip}:{resp.host_port}");
        }
        else
        {
            // Lỗi mạng / server chưa khởi động → cứ làm host (fallback an toàn)
            Debug.LogWarning($"[MapTravelHelper] Register host request failed ({req.responseCode}): {req.error}. Falling back to host.");
            result.youAreHost = true;
            result.hostIp     = ip;
            result.hostPort   = port;
        }
        result.done = true;
    }

    private IEnumerator UnregisterHostRequest(int mapId, int playerId)
    {
        var body = JsonUtility.ToJson(new MapHostUnregisterDto
        {
            map_id    = mapId,
            player_id = playerId
        });

        string url = $"{ApiBase}/map/host/unregister";
        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
            Debug.Log($"[MapTravelHelper] Unregistered host: map={mapId}");
        else
            Debug.LogWarning($"[MapTravelHelper] Unregister host failed: {req.error}");
    }

    /// <summary>Gửi heartbeat mỗi 30s để giữ host entry sống (timeout = 120s trên server).</summary>
    private IEnumerator HeartbeatCoroutine(int mapId, int playerId)
    {
        var wait = new WaitForSeconds(30f);
        while (IsCurrentHost && CurrentHostMapId == mapId)
        {
            yield return wait;
            if (!IsCurrentHost || CurrentHostMapId != mapId) yield break;

            var body = JsonUtility.ToJson(new MapHostUnregisterDto { map_id = mapId, player_id = playerId });
            string url = $"{ApiBase}/map/host/heartbeat";
            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
            yield return req.SendWebRequest();
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Utility
    // ────────────────────────────────────────────────────────────────────────

    private static string GetLocalIP()
    {
        try
        {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[MapTravelHelper] GetLocalIP failed: " + ex.Message);
        }
        return "127.0.0.1";
    }

    private static ushort GetNetworkPort()
    {
        // Ưu tiên đọc từ NetworkManagerCustom (inspector field)
        var nmCustom = Object.FindObjectOfType<NetworkManagerCustom>();
        if (nmCustom != null) return nmCustom.serverPort;
        // Fallback: đọc từ UnityTransport nếu NM đang chạy
        var transport = NetworkManager.Singleton?.GetComponent<UnityTransport>();
        if (transport != null && transport.ConnectionData.Port > 0)
            return transport.ConnectionData.Port;
        return 2003; // default
    }

    /// <summary>
    /// Đánh dấu DontDestroyOnLoad cho các Canvas/EventSystem cần tồn tại qua scene load.
    /// Gọi TRƯỚC Shutdown() để objects chưa bị destroy.
    /// </summary>
    private static void PersistCanvasObjects()
    {
        string[] persistNames =
        {
            "ScreenSpaceCanvas",
            "InformationCanvas",
            "SkillHotbar",
            "DungeonCanvas",
            "GeneUpgradeCanvas",
            "HybridFusionCanvas",
            "UpgradleCanvas",
            "Canvas",
            "EventSystem",
        };

        foreach (var name in persistNames)
        {
            var go = GameObject.Find(name);
            if (go == null) continue;
            if (go.scene.name == "DontDestroyOnLoad") continue;
            DontDestroyOnLoad(go);
            Debug.Log($"[MapTravelHelper] Canvas persist: '{name}'");
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Inner DTOs (JsonUtility-compatible: phải là class với fields)
    // ────────────────────────────────────────────────────────────────────────

    private class HostRegisterResult
    {
        public bool   youAreHost;
        public string hostIp   = "127.0.0.1";
        public ushort hostPort = 2003;
        public bool   done;
    }

    [System.Serializable]
    private class MapHostRegisterDto
    {
        public int    map_id;
        public string host_ip;
        public int    host_port;
        public int    player_id;
    }

    [System.Serializable]
    private class MapHostRegisterResponse
    {
        public bool   success;
        public bool   you_are_host;
        public string host_ip;
        public int    host_port;
    }

    [System.Serializable]
    private class MapHostUnregisterDto
    {
        public int map_id;
        public int player_id;
    }
}

