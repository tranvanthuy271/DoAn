using System.Collections.Generic;
using UnityEngine;

// Server-side registry của tất cả ZoneRooms — tương đương Map[] maps tĩnh của LangLa.
// LangLa pattern:
// Map.maps[mapId].listZone[zoneId]  →  ZoneRoomRegistry.Instance.GetRoom(mapId, zoneId)
// Được khởi tạo 1 lần khi server start từ MapWorldConfig asset.
// Tất cả operations là O(1) lookup qua Dictionary.
// Gắn vào: "ZoneManagers" GameObject trong ServerScene (persistent).
public class ZoneRoomRegistry : MonoBehaviour
{
    public static ZoneRoomRegistry Instance { get; private set; }

    // [mapId → [zoneId → ZoneRoom]]  — giống Map[].listZone[]
    private readonly Dictionary<int, Dictionary<int, ZoneRoom>> _rooms = new();

    // Fast reverse lookup: clientId → ZoneRoom hiện tại (giống LangLa client.zone)
    private readonly Dictionary<ulong, ZoneRoom> _clientRoom = new();

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // Initialization

    // Khởi tạo tất cả ZoneRooms từ MapWorldConfig.
    // Gọi bởi MapWorldBootstrap trước StartServer().
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
                { /* Loaded {room} */ }
            }
            _rooms[mapDef.mapId] = zoneDict;
        }

        { /* ✓ Initialized {_rooms.Count} maps */ }
    }

    // Room lookup

    // Lấy ZoneRoom theo mapId + zoneId. Null nếu không tồn tại.
    public ZoneRoom GetRoom(int mapId, int zoneId)
    {
        if (_rooms.TryGetValue(mapId, out var zones) &&
            zones.TryGetValue(zoneId, out var room))
            return room;
        return null;
    }

    // Lấy zone hiện tại của client. Null nếu chưa assign.
    public ZoneRoom GetClientRoom(ulong clientId)
        => _clientRoom.TryGetValue(clientId, out var r) ? r : null;

    // Tìm zone ít người nhất trong map — dùng khi zone đầy (zone capacity fallback).
    // Tương đương LangLa tìm zone ít người nhất để balance load.
    // Trả về: Zone còn chỗ, ưu tiên zone ít player nhất. Null nếu tất cả đều đầy.
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

    // Kiểm tra 2 client có ở cùng zone không — dùng cho visibility filter.
    public bool AreInSameZone(ulong clientA, ulong clientB)
    {
        var roomA = GetClientRoom(clientA);
        var roomB = GetClientRoom(clientB);
        return roomA != null && roomB != null && roomA.ZoneKey == roomB.ZoneKey;
    }

    // Client movement (như LangLa zone.removeChar + newZone.addChar)

    // Chuyển client từ zone hiện tại sang zone mới — atomic.
    // Gọi khi player transfer zone hoặc spawn lần đầu.
    public void AssignClientToRoom(ulong clientId, ZoneRoom newRoom)
    {
        // Xóa khỏi zone cũ nếu có
        if (_clientRoom.TryGetValue(clientId, out var oldRoom))
            oldRoom.RemoveClient(clientId);

        // Thêm vào zone mới
        newRoom.AddClient(clientId);
        _clientRoom[clientId] = newRoom;
    }

    // Xóa client khỏi registry khi disconnect.
    public void UnregisterClient(ulong clientId)
    {
        if (_clientRoom.TryGetValue(clientId, out var room))
            room.RemoveClient(clientId);
        _clientRoom.Remove(clientId);
    }

    // Stats

    private int TotalZoneCount
    {
        get
        {
            int count = 0;
            foreach (var zones in _rooms.Values) count += zones.Count;
            return count;
        }
    }

    // Tổng số player đang online trên toàn bộ server.
    public int TotalPlayerCount => _clientRoom.Count;

    // Config đã dùng để khởi tạo — dùng cho Heartbeat.
    public MapWorldConfig Config { get; private set; }

    // Log trạng thái tất cả zones — dùng cho debug.
    public void LogStatus()
    {
        foreach (var zones in _rooms.Values)
            foreach (var room in zones.Values)
                { /* Status: {room} */ }
    }
}
