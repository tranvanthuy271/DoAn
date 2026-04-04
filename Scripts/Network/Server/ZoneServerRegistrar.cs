using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Đăng ký / hủy đăng ký zone server với GameServerApi.
///
/// API endpoints cần có (thêm vào GameServerApi):
///   POST /api/zone/register    { mapId, zoneId, ip, port }  → 200 OK
///   DELETE /api/zone/deregister?mapId=X&zoneId=Y            → 200 OK
/// </summary>
public class ZoneServerRegistrar : MonoBehaviour
{
    private string _apiBaseUrl;

    public void Initialize(string apiBaseUrl)
    {
        _apiBaseUrl = apiBaseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Đăng ký zone server với API. Gọi sau khi StartServer() thành công.
    /// </summary>
    public IEnumerator Register(int mapId, int zoneId, string publicIp, ushort port,
                                 Action<bool> callback)
    {
        string url = $"{_apiBaseUrl}/zone/register";
        string body = $"{{\"mapId\":{mapId},\"zoneId\":{zoneId}," +
                      $"\"ip\":\"{EscapeJson(publicIp)}\",\"port\":{port}}}";

        using var request = new UnityWebRequest(url, "POST")
        {
            uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body)),
            downloadHandler = new DownloadHandlerBuffer()
        };
        request.SetRequestHeader("Content-Type", "application/json");
        // Dùng server-to-server API key thay vì JWT user token
        string apiKey = Environment.GetEnvironmentVariable("ZONE_API_KEY") ?? "dev-zone-key";
        request.SetRequestHeader("X-Zone-Api-Key", apiKey);

        yield return request.SendWebRequest();

        bool ok = request.result == UnityWebRequest.Result.Success;
        if (!ok)
        {
            Debug.LogWarning($"[ZoneServerRegistrar] Register thất bại: " +
                             $"HTTP {request.responseCode} — {request.error}");
        }
        callback?.Invoke(ok);
    }

    /// <summary>
    /// Hủy đăng ký zone server khỏi API. Gọi khi server tắt.
    /// </summary>
    public IEnumerator Deregister(int mapId, int zoneId)
    {
        string url = $"{_apiBaseUrl}/zone/deregister?mapId={mapId}&zoneId={zoneId}";
        string apiKey = Environment.GetEnvironmentVariable("ZONE_API_KEY") ?? "dev-zone-key";

        using var request = UnityWebRequest.Delete(url);
        request.SetRequestHeader("X-Zone-Api-Key", apiKey);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ZoneServerRegistrar] Deregister thất bại: {request.error}");
        }
        else
        {
            Debug.Log($"[ZoneServerRegistrar] ✓ Đã hủy đăng ký zone map={mapId} zone={zoneId}");
        }
    }

    private static string EscapeJson(string s) =>
        s.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
