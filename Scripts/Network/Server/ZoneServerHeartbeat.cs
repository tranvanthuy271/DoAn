using System.Collections;
using System.Text;
using UnityEngine;

// Gửi heartbeat đến API định kỳ để API biết server còn sống.
// Giải quyết Issue #3: API tự đánh dấu server offline nếu không nhận heartbeat trong 2× interval.
// API endpoint: PUT {apiBaseUrl}/zone/server/heartbeat
// Body: { "ip":"...", "port":..., "playerCount":..., "zoneStats":[...] }
// Gắn vào: "ServerBootstrap" GameObject (cùng với MapWorldBootstrap).
// MapWorldBootstrap sẽ gọi Initialize() tự động.
[DisallowMultipleComponent]
public class ZoneServerHeartbeat : MonoBehaviour
{
    private MapWorldConfig _config;
    private string         _apiBaseUrl;
    private ushort         _serverPort;

    private Coroutine _heartbeatCoroutine;

    // Init (gọi từ MapWorldBootstrap)

    public void Initialize(MapWorldConfig config, string apiBaseUrl, ushort serverPort)
    {
        _config     = config;
        _apiBaseUrl = apiBaseUrl;
        _serverPort = serverPort;

        if (_heartbeatCoroutine != null)
            StopCoroutine(_heartbeatCoroutine);

        _heartbeatCoroutine = StartCoroutine(HeartbeatLoop());
        Debug.Log($"[ZoneServerHeartbeat] Bắt đầu heartbeat mỗi {_config.heartbeatInterval}s");
    }

    // Heartbeat loop

    private IEnumerator HeartbeatLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(_config.heartbeatInterval);
            yield return StartCoroutine(SendHeartbeat());
        }
    }

    private IEnumerator SendHeartbeat()
    {
        var registry = ZoneRoomRegistry.Instance;

        // Build zone stats
        var zoneStatsBuilder = new StringBuilder("[");
        bool first = true;
        if (registry != null && _config != null)
        {
            foreach (var mapDef in _config.maps)
            {
                foreach (var zoneDef in mapDef.zones)
                {
                    var room = registry.GetRoom(mapDef.mapId, zoneDef.zoneId);
                    if (room == null) continue;

                    if (!first) zoneStatsBuilder.Append(',');
                    zoneStatsBuilder.Append(
                        $"{{\"mapId\":{room.MapId},\"zoneId\":{room.ZoneId}," +
                        $"\"players\":{room.PlayerCount},\"max\":{room.MaxPlayers}}}");
                    first = false;
                }
            }
        }
        zoneStatsBuilder.Append(']');

        int totalPlayers = registry?.TotalPlayerCount ?? 0;
        string body = $"{{\"port\":{_serverPort},\"playerCount\":{totalPlayers}," +
                      $"\"zoneStats\":{zoneStatsBuilder}}}";

        string url = $"{_apiBaseUrl.TrimEnd('/')}/zone/server/heartbeat";
        using var req = new UnityEngine.Networking.UnityWebRequest(url, "PUT")
        {
            uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
            downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer()
        };
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-Zone-Api-Key", _config.GetZoneApiKey());

        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ZoneServerHeartbeat] Heartbeat thất bại: {req.error}");
        }
        else
        {
            Debug.Log($"[ZoneServerHeartbeat] ✓ Heartbeat ok — {totalPlayers} players online");
        }
    }

    // Cleanup

    private void OnApplicationQuit()
    {
        if (_heartbeatCoroutine != null)
            StopCoroutine(_heartbeatCoroutine);

        // Gửi deregister (best-effort, có thể không kịp trước khi process kill)
        StartCoroutine(SendDeregister());
    }

    private IEnumerator SendDeregister()
    {
        if (_config == null) yield break;
        string url = $"{_apiBaseUrl.TrimEnd('/')}/zone/server/deregister?port={_serverPort}";
        using var req = UnityEngine.Networking.UnityWebRequest.Delete(url);
        req.SetRequestHeader("X-Zone-Api-Key", _config.GetZoneApiKey());
        yield return req.SendWebRequest();
        Debug.Log("[ZoneServerHeartbeat] Đã gửi deregister.");
    }
}
