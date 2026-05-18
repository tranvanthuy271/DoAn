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

        if (!TryParsePayload(json, out string token, out int mapId, out int zoneId, out int geneSlot))
        {
            Reject(response, "Payload JSON không hợp lệ");
            return;
        }

        // 3 — Validate JWT
        string secret = _config.GetJwtSecret();
        var    result = JwtValidator.Validate(token, secret);
        if (!result.IsValid)
        {
            // JwtValidator.Result exposes ErrorMessage (not ErrorReason)
            Reject(response, $"JWT không hợp lệ: {result.ErrorMessage}");
            return;
        }

        // 4 — Kiểm tra zone tồn tại
        var registry = ZoneRoomRegistry.Instance;
        if (registry == null)
        {
            Reject(response, "Server chưa sẵn sàng (registry null)");
            return;
        }

        ZoneRoom room = registry.ResolveLoginRoom(mapId, zoneId);
        if (room == null)
        {
            Reject(response, $"Không tìm được zone hợp lệ cho map={mapId}, zone={zoneId}");
            return;
        }

        if (room.MapId != mapId || room.ZoneId != zoneId)
        {
            Debug.Log($"[ZoneConnectionApprovalV2] Client {request.ClientNetworkId}: zone ({mapId},{zoneId}) → fallback ({room.MapId},{room.ZoneId})");
        }

        // 5 — Kiểm tra room đầy
        if (room.IsFull)
        {
            ZoneRoom fallback = room.IsCustom ? null : registry.FindLeastLoadedZone(room.MapId, room.ZoneId);
            if (fallback == null || fallback.IsFull)
            {
                Reject(response, room.IsCustom ? "Zone riêng đã đầy" : "Server đầy");
                return;
            }
            room = fallback;
        }

        // 6 — Assign client vào room ngay lúc connect
        ulong clientId = request.ClientNetworkId;
        registry.AssignClientToRoom(clientId, room);

        // 7 — Lưu session (userId, username)
        Debug.Log($"[ZoneConnectionApprovalV2] geneSlot={geneSlot} parsed from payload for client {clientId}");
        ZonePlayerSessionManager.RegisterSessionOrQueue(clientId, result.UserId, result.Username,
            room.MapId, room.ZoneId, token, geneSlot);

        Debug.Log($"[ZoneConnectionApprovalV2] ✓ Client {clientId} ({result.Username}) " +
                  $"→ map{room.MapId}_zone{room.ZoneId}");

        // 8 — Approve
        response.Approved           = true;
        response.CreatePlayerObject = false;
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

    private static bool TryParsePayload(string json, out string token, out int mapId, out int zoneId, out int geneSlot)
    {
        token = ""; mapId = 0; zoneId = 0; geneSlot = 1;
        try
        {
            token    = ExtractString(json, "token");
            mapId    = ExtractInt(json, "mapId");
            zoneId   = ExtractInt(json, "zoneId");
            geneSlot = ExtractInt(json, "geneSlot");
            if (geneSlot < 1 || geneSlot > 2) geneSlot = 1; // sanity
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
