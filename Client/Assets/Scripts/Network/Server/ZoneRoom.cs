using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Đại diện cho 1 logical zone trong 1 map — tương đương class Zone của LangLa.
///
/// KHÔNG phải separate process hay port.
/// Tất cả zones tồn tại trong cùng 1 NGO server instance, cùng 1 port.
///
/// Responsibilities:
///   - Giữ danh sách clientId đang ở zone này (như LangLa Zone.listChar)
///   - Expose ZoneKey = "map{M}_zone{Z}" để identify
///   - Thread-safe với lock đơn giản (chỉ được gọi từ NGO main thread)
/// </summary>
public class ZoneRoom
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public int MapId  { get; }
    public int ZoneId { get; }
    public string ZoneName  { get; }
    public string SceneName { get; }
    public bool IsCustom { get; }

    /// <summary>Unique key — dùng giống roomId LangLa: "map1_zone0"</summary>
    public string ZoneKey => $"map{MapId}_zone{ZoneId}";

    // ── Capacity ──────────────────────────────────────────────────────────────
    public int MaxPlayers   { get; }
    public int PlayerCount  => _clientIds.Count;
    public bool IsFull      => MaxPlayers > 0 && PlayerCount >= MaxPlayers;

    // ── Entry points ──────────────────────────────────────────────────────────
    public Vector2[] EntryPoints { get; }

    // ── Player list (like LangLa Zone.listChar) ───────────────────────────────
    // NGO chỉ chạy trên main thread → không cần thread-safe lock phức tạp
    // Dùng HashSet để O(1) Contains/Add/Remove
    private readonly HashSet<ulong> _clientIds = new();

    // ── Constructor ───────────────────────────────────────────────────────────

    public ZoneRoom(MapDefinition map, ZoneDefinition zone)
    {
        MapId      = map.mapId;
        ZoneId     = zone.zoneId;
        ZoneName   = zone.zoneName;
        SceneName  = map.sceneName;
        IsCustom   = zone.isCustom;
        MaxPlayers = zone.maxPlayers;
        EntryPoints = zone.entryPoints ?? new[] { Vector2.zero };
    }

    // ── Player management (server main thread only) ───────────────────────────

    /// <summary>Thêm player vào zone. Gọi khi player spawn hoặc transfer đến.</summary>
    public void AddClient(ulong clientId)
    {
        _clientIds.Add(clientId);
        Debug.Log($"[ZoneRoom] {ZoneKey}: +client {clientId} ({PlayerCount}/{MaxPlayers})");
    }

    /// <summary>Xóa player khỏi zone. Gọi khi transfer đi hoặc disconnect.</summary>
    public void RemoveClient(ulong clientId)
    {
        _clientIds.Remove(clientId);
        Debug.Log($"[ZoneRoom] {ZoneKey}: -client {clientId} ({PlayerCount}/{MaxPlayers})");
    }

    /// <summary>Kiểm tra client có ở trong zone này không.</summary>
    public bool Contains(ulong clientId) => _clientIds.Contains(clientId);

    /// <summary>
    /// Snapshot danh sách clientIds trong zone.
    /// Trả về copy để tránh modification-during-iteration.
    /// </summary>
    public ulong[] GetClientSnapshot() => new List<ulong>(_clientIds).ToArray();

    // ── Entry point ───────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về entry point position theo index.
    /// entryPointId = 0 → spawn default.
    /// Nếu index out-of-range → trả về (0,0).
    /// </summary>
    public Vector2 GetEntryPoint(int entryPointId)
    {
        if (EntryPoints != null && entryPointId >= 0 && entryPointId < EntryPoints.Length)
            return EntryPoints[entryPointId];
        return Vector2.zero;
    }

    public override string ToString() =>
        $"ZoneRoom({ZoneKey}, type={(IsCustom ? "custom" : "public")}, scene={SceneName}, players={PlayerCount}/{MaxPlayers})";
}
