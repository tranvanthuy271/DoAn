using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Server-side registry của tất cả ZoneRooms — tương đương Map[] maps tĩnh của LangLa.
///
/// LangLa pattern:
///   Map.maps[mapId].listZone[zoneId]  →  ZoneRoomRegistry.Instance.GetRoom(mapId, zoneId)
///
/// Được khởi tạo 1 lần khi server start từ MapWorldConfig asset.
/// Tất cả operations là O(1) lookup qua Dictionary.
///
/// Gắn vào: "ZoneManagers" GameObject trong ServerScene (persistent).
/// </summary>
public class ZoneRoomRegistry : MonoBehaviour
{
    public static ZoneRoomRegistry Instance { get; private set; }

    // [mapId → [zoneId → ZoneRoom]]  — giống Map[].listZone[]
    private readonly Dictionary<int, Dictionary<int, ZoneRoom>> _rooms = new();

    // Fast reverse lookup: clientId → ZoneRoom hiện tại (giống LangLa client.zone)
    private readonly Dictionary<ulong, ZoneRoom> _clientRoom = new();

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Initialization ────────────────────────────────────────────────────────

    /// <summary>
    /// Khởi tạo tất cả ZoneRooms từ MapWorldConfig.
    /// Gọi bởi MapWorldBootstrap trước StartServer().
    /// </summary>
    public void Initialize(MapWorldConfig config)
    {
        Config = config;
        _rooms.Clear();
        _clientRoom.Clear();

        foreach (var mapDef in config.maps)
        {
            var zoneDict = new Dictionary<int, ZoneRoom>();
            foreach (var zoneDef in mapDef.zones)
            {
                var room = new ZoneRoom(mapDef, zoneDef);
                zoneDict[zoneDef.zoneId] = room;
                Debug.Log($"[ZoneRoomRegistry] Loaded {room}");
            }
            _rooms[mapDef.mapId] = zoneDict;
        }

        Debug.Log($"[ZoneRoomRegistry] ✓ Initialized {_rooms.Count} maps, " +
                  $"{TotalZoneCount} total zones.");
    }

    // ── Room lookup ───────────────────────────────────────────────────────────

    /// <summary>Lấy ZoneRoom theo mapId + zoneId. Null nếu không tồn tại.</summary>
    public ZoneRoom GetRoom(int mapId, int zoneId)
    {
        if (_rooms.TryGetValue(mapId, out var zones) &&
            zones.TryGetValue(zoneId, out var room))
            return room;
        return null;
    }

    /// <summary>Lấy zone hiện tại của client. Null nếu chưa assign.</summary>
    public ZoneRoom GetClientRoom(ulong clientId)
        => _clientRoom.TryGetValue(clientId, out var r) ? r : null;

    /// <summary>
    /// Tìm zone ít người nhất trong map — dùng khi zone đầy (zone capacity fallback).
    /// Tương đương LangLa tìm zone ít người nhất để balance load.
    /// </summary>
    /// <returns>Zone còn chỗ, ưu tiên zone ít player nhất. Null nếu tất cả đều đầy.</returns>
    public ZoneRoom FindLeastLoadedZone(int mapId, int preferredZoneId = 0)
    {
        if (!_rooms.TryGetValue(mapId, out var zones)) return null;

        ZoneRoom best = null;
        foreach (var room in zones.Values)
        {
            if (room.IsFull) continue;
            if (best == null || room.PlayerCount < best.PlayerCount)
                best = room;
        }
        return best;
    }

    /// <summary>Kiểm tra 2 client có ở cùng zone không — dùng cho visibility filter.</summary>
    public bool AreInSameZone(ulong clientA, ulong clientB)
    {
        var roomA = GetClientRoom(clientA);
        var roomB = GetClientRoom(clientB);
        return roomA != null && roomB != null && roomA.ZoneKey == roomB.ZoneKey;
    }

    // ── Client movement (như LangLa zone.removeChar + newZone.addChar) ─────────

    /// <summary>
    /// Chuyển client từ zone hiện tại sang zone mới — atomic.
    /// Gọi khi player transfer zone hoặc spawn lần đầu.
    /// </summary>
    public void AssignClientToRoom(ulong clientId, ZoneRoom newRoom)
    {
        // Xóa khỏi zone cũ nếu có
        if (_clientRoom.TryGetValue(clientId, out var oldRoom))
            oldRoom.RemoveClient(clientId);

        // Thêm vào zone mới
        newRoom.AddClient(clientId);
        _clientRoom[clientId] = newRoom;
    }

    /// <summary>Xóa client khỏi registry khi disconnect.</summary>
    public void UnregisterClient(ulong clientId)
    {
        if (_clientRoom.TryGetValue(clientId, out var room))
            room.RemoveClient(clientId);
        _clientRoom.Remove(clientId);
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    private int TotalZoneCount
    {
        get
        {
            int count = 0;
            foreach (var zones in _rooms.Values) count += zones.Count;
            return count;
        }
    }

    /// <summary>Tổng số player đang online trên toàn bộ server.</summary>
    public int TotalPlayerCount => _clientRoom.Count;

    /// <summary>Config đã dùng để khởi tạo — dùng cho Heartbeat.</summary>
    public MapWorldConfig Config { get; private set; }

    /// <summary>Log trạng thái tất cả zones — dùng cho debug.</summary>
    public void LogStatus()
    {
        foreach (var zones in _rooms.Values)
            foreach (var room in zones.Values)
                Debug.Log($"[ZoneRoomRegistry] Status: {room}");
    }
}
