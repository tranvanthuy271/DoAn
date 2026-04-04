using System;
using System.Text;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Connection Approval v2 — dùng với kiến trúc 1-port (MapWorldBootstrap).
/// Thay thế ZoneConnectionApproval cũ (per-process model).
///
/// Validate:
///   1. Payload tối thiểu (JWT token + mapId + zoneId)
///   2. JWT hợp lệ + chưa hết hạn + secret đúng (HS256)
///   3. Zone tồn tại trong ZoneRoomRegistry
///   4. Zone chưa đầy (hoặc có fallback)
///
/// DTLS (Issue #6): Bật UnityTransport.UseEncryption = true ở MapWorldBootstrap
/// để mã hóa toàn bộ UDP traffic (DTLS 1.2/1.3).
///
/// Gắn vào: "ServerBootstrap" GameObject.
/// </summary>
[DisallowMultipleComponent]
public class ZoneConnectionApprovalV2 : MonoBehaviour
{
    private MapWorldConfig _config;

    // Max size payload để phòng buffer overflow
    private const int MaxPayloadBytes = 2048;

    /// <summary>Gọi từ MapWorldBootstrap.StartServerRoutine()</summary>
    public void Initialize(MapWorldConfig config)
    {
        _config = config;
        NetworkManager.Singleton.ConnectionApprovalCallback = HandleApproval;
        Debug.Log("[ZoneConnectionApprovalV2] Connection Approval đã đăng ký.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Payload format (JSON, UTF-8):
    //   { "token": "<JWT>", "mapId": 0, "zoneId": 0 }
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleApproval(
        NetworkManager.ConnectionApprovalRequest  request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        // 1 — Kiểm tra payload size (DoS prevention)
        if (request.Payload.Length > MaxPayloadBytes)
        {
            Reject(response, "Payload quá lớn");
            return;
        }

        // 2 — Parse payload
        string json;
        try   { json = Encoding.UTF8.GetString(request.Payload); }
        catch { Reject(response, "Payload không phải UTF-8"); return; }

        if (!TryParsePayload(json, out string token, out int mapId, out int zoneId))
        {
            Reject(response, "Payload JSON không hợp lệ");
            return;
        }

        // 3 — Validate JWT
        string secret = _config.GetJwtSecret();
        var    result = JwtValidator.Validate(token, secret);
        if (!result.IsValid)
        {
            Reject(response, $"JWT không hợp lệ: {result.ErrorReason}");
            return;
        }

        // 4 — Kiểm tra zone tồn tại
        var registry = ZoneRoomRegistry.Instance;
        if (registry == null)
        {
            Reject(response, "Server chưa sẵn sàng (registry null)");
            return;
        }

        ZoneRoom room = registry.GetRoom(mapId, zoneId);
        if (room == null)
        {
            // Fallback: map 0, zone 0 (spawn mặc định)
            room = registry.GetRoom(0, 0);
            if (room == null)
            {
                Reject(response, $"Zone ({mapId},{zoneId}) không tồn tại");
                return;
            }
            Debug.Log($"[ZoneConnectionApprovalV2] Client {request.ClientNetworkId}: zone ({mapId},{zoneId}) → fallback (0,0)");
        }

        // 5 — Kiểm tra room đầy
        if (room.IsFull)
        {
            ZoneRoom fallback = registry.FindLeastLoadedZone(room.MapId);
            if (fallback == null || fallback.IsFull)
            {
                Reject(response, "Server đầy");
                return;
            }
            room = fallback;
        }

        // 6 — Assign client vào room ngay lúc connect
        ulong clientId = request.ClientNetworkId;
        registry.AssignClientToRoom(clientId, room);

        // 7 — Lưu session (userId, username)
        ZonePlayerSessionManager.Instance?.RegisterSession(clientId, result.UserId, result.Username,
            room.MapId, room.ZoneId);

        Debug.Log($"[ZoneConnectionApprovalV2] ✓ Client {clientId} ({result.Username}) " +
                  $"→ map{room.MapId}_zone{room.ZoneId}");

        // 8 — Approve
        response.Approved          = true;
        response.CreatePlayerObject = true;
        // Vị trí spawn = entry point 0 của zone
        Vector2 entry = room.GetEntryPoint(0);
        response.Position = new Vector3(entry.x, entry.y, 0f);
        response.Rotation = Quaternion.identity;
    }

    private static void Reject(NetworkManager.ConnectionApprovalResponse response, string reason)
    {
        response.Approved = false;
        response.Reason   = reason;
        Debug.LogWarning($"[ZoneConnectionApprovalV2] Từ chối kết nối: {reason}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Minimal JSON parser (không dùng thư viện ngoài)
    // ─────────────────────────────────────────────────────────────────────────

    private static bool TryParsePayload(string json, out string token, out int mapId, out int zoneId)
    {
        token = ""; mapId = 0; zoneId = 0;
        try
        {
            token  = ExtractString(json, "token");
            mapId  = ExtractInt(json, "mapId");
            zoneId = ExtractInt(json, "zoneId");
            return !string.IsNullOrEmpty(token);
        }
        catch { return false; }
    }

    private static string ExtractString(string json, string key)
    {
        string search = $"\"{key}\"";
        int ki = json.IndexOf(search, StringComparison.Ordinal);
        if (ki < 0) return "";
        int colon  = json.IndexOf(':', ki + search.Length);
        int open   = json.IndexOf('"', colon  + 1);
        int close  = json.IndexOf('"', open   + 1);
        return json.Substring(open + 1, close - open - 1);
    }

    private static int ExtractInt(string json, string key)
    {
        string search = $"\"{key}\"";
        int ki = json.IndexOf(search, StringComparison.Ordinal);
        if (ki < 0) return 0;
        int colon = json.IndexOf(':', ki + search.Length);
        int start = colon + 1;
        while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
        int end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
        return int.TryParse(json.Substring(start, end - start), out int v) ? v : 0;
    }
}
