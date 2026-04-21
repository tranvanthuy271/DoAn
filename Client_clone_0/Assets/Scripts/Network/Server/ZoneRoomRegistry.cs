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

    // Những custom room rỗng nhưng vẫn phải giữ lại để reconnect vào đúng instance cũ.
    private readonly Dictionary<string, ZoneRoom> _preservedEmptyCustomRooms = new();

    // mapId → next negative zone id cho private/custom room
    private readonly Dictionary<int, int> _nextCustomZoneIdByMap = new();

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
        _preservedEmptyCustomRooms.Clear();
        _nextCustomZoneIdByMap.Clear();

        foreach (var mapDef in config.maps)
        {
            var zoneDict = new Dictionary<int, ZoneRoom>();
            _rooms[mapDef.mapId] = zoneDict;
            _nextCustomZoneIdByMap[mapDef.mapId] = -1;

            if (!mapDef.UsesPublicZones(config))
            {
                Debug.Log($"[ZoneRoomRegistry] Map {mapDef.mapId} ({mapDef.mapName}) không auto-create public zones.");
                continue;
            }

            int publicZoneCount = mapDef.GetPublicZoneCount(config);
            for (int zoneId = 0; zoneId < publicZoneCount; zoneId++)
            {
                var room = new ZoneRoom(mapDef, mapDef.CreatePublicZone(config, zoneId));
                zoneDict[zoneId] = room;
                Debug.Log($"[ZoneRoomRegistry] Loaded {room}");
            }
        }

        Debug.Log($"[ZoneRoomRegistry] ✓ Initialized {_rooms.Count} maps, " +
                  $"{TotalRoomCount} total rooms.");
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

    public ZoneRoom EnsureRoomRegistered(ZoneRoom room)
    {
        if (room == null)
            return null;

        if (!_rooms.TryGetValue(room.MapId, out var zones))
        {
            zones = new Dictionary<int, ZoneRoom>();
            _rooms[room.MapId] = zones;
        }

        if (!zones.ContainsKey(room.ZoneId))
        {
            zones[room.ZoneId] = room;
            Debug.Log($"[ZoneRoomRegistry] Re-registered custom room {room.ZoneKey} for active wave session.");
        }

        if (room.IsCustom)
            _preservedEmptyCustomRooms[room.ZoneKey] = room;

        return zones[room.ZoneId];
    }

    public ZoneRoom EnsureCustomRoomRegistered(int mapId, int zoneId, string customRoomName = null, int? maxPlayersOverride = null)
    {
        ZoneRoom existingRoom = GetRoom(mapId, zoneId);
        if (existingRoom != null)
            return existingRoom;

        string zoneKey = BuildZoneKey(mapId, zoneId);
        if (_preservedEmptyCustomRooms.TryGetValue(zoneKey, out ZoneRoom preservedRoom) && preservedRoom != null)
            return EnsureRoomRegistered(preservedRoom);

        var mapDef = Config?.GetMap(mapId);
        if (mapDef == null)
        {
            Debug.LogWarning($"[ZoneRoomRegistry] Không thể khôi phục custom room: map {mapId} không tồn tại.");
            return null;
        }

        if (!mapDef.SupportsCustomZones)
        {
            Debug.LogWarning($"[ZoneRoomRegistry] Map {mapId} không hỗ trợ custom room để restore zone {zoneId}.");
            return null;
        }

        if (!_rooms.TryGetValue(mapId, out var zones))
        {
            zones = new Dictionary<int, ZoneRoom>();
            _rooms[mapId] = zones;
        }

        var room = new ZoneRoom(mapDef, mapDef.CreateCustomZone(Config, zoneId, customRoomName, maxPlayersOverride));
        zones[zoneId] = room;
        _preservedEmptyCustomRooms[zoneKey] = room;

        if (!_nextCustomZoneIdByMap.TryGetValue(mapId, out int nextZoneId) || zoneId <= nextZoneId)
            _nextCustomZoneIdByMap[mapId] = zoneId - 1;

        Debug.Log($"[ZoneRoomRegistry] Recreated custom room {room.ZoneKey} from preserved wave session.");
        return room;
    }

    public void MarkRoomPreserved(ZoneRoom room, string reason = null)
    {
        if (room == null || !room.IsCustom)
            return;

        EnsureRoomRegistered(room);
        _preservedEmptyCustomRooms[room.ZoneKey] = room;

        string suffix = string.IsNullOrWhiteSpace(reason) ? string.Empty : $" | reason={reason}";
        Debug.Log($"[ZoneRoomRegistry] Preserving custom room {room.ZoneKey}{suffix}");
    }

    public void ReleasePreservedRoom(int mapId, int zoneId)
    {
        string zoneKey = BuildZoneKey(mapId, zoneId);
        _preservedEmptyCustomRooms.Remove(zoneKey);

        if (_rooms.TryGetValue(mapId, out var zones) &&
            zones.TryGetValue(zoneId, out var room) &&
            room != null &&
            room.IsCustom &&
            room.PlayerCount <= 0)
        {
            zones.Remove(zoneId);
            Debug.Log($"[ZoneRoomRegistry] Released preserved empty custom room {zoneKey}");
        }
    }

    public List<ZoneRoom> GetAllRoomsSnapshot()
    {
        var snapshot = new List<ZoneRoom>();
        foreach (var zones in _rooms.Values)
            snapshot.AddRange(zones.Values);
        return snapshot;
    }

    /// <summary>Lấy zone hiện tại của client. Null nếu chưa assign.</summary>
    public ZoneRoom GetClientRoom(ulong clientId)
        => _clientRoom.TryGetValue(clientId, out var r) ? r : null;

    public ZoneRoom GetFallbackRoom()
    {
        if (Config == null)
            return null;

        var fallback = GetRoom(Config.fallbackMapId, Config.fallbackZoneId);
        if (fallback != null)
            return fallback;

        foreach (var mapDef in Config.maps)
        {
            if (!mapDef.UsesPublicZones(Config))
                continue;

            fallback = FindLeastLoadedZone(mapDef.mapId);
            if (fallback != null)
                return fallback;
        }

        return null;
    }

    public ZoneRoom ResolveLoginRoom(int mapId, int zoneId)
    {
        var requestedRoom = GetRoom(mapId, zoneId);
        if (requestedRoom != null)
            return requestedRoom;

        var mapDef = Config?.GetMap(mapId);
        if (mapDef != null && mapDef.UsesPublicZones(Config))
            return FindLeastLoadedZone(mapId, zoneId);

        return GetFallbackRoom();
    }

    public bool CanPlayerChangePublicZone(int mapId)
    {
        var mapDef = Config?.GetMap(mapId);
        return mapDef != null && mapDef.CanPlayerChangePublicZone(Config);
    }

    /// <summary>
    /// Tìm zone ít người nhất trong map — dùng khi zone đầy (zone capacity fallback).
    /// Tương đương LangLa tìm zone ít người nhất để balance load.
    /// </summary>
    /// <returns>Zone còn chỗ, ưu tiên zone ít player nhất. Null nếu tất cả đều đầy.</returns>
    public ZoneRoom FindLeastLoadedZone(int mapId, int preferredZoneId = 0)
    {
        if (!_rooms.TryGetValue(mapId, out var zones)) return null;

        if (zones.TryGetValue(preferredZoneId, out var preferredRoom) &&
            !preferredRoom.IsCustom && !preferredRoom.IsFull)
            return preferredRoom;

        ZoneRoom best = null;
        foreach (var room in zones.Values)
        {
            if (room.IsCustom) continue;
            if (room.IsFull) continue;
            if (best == null || room.PlayerCount < best.PlayerCount)
                best = room;
        }
        return best;
    }

    public ZoneRoom CreateCustomRoom(int mapId, string customRoomName = null, int? maxPlayersOverride = null)
    {
        var mapDef = Config?.GetMap(mapId);
        if (mapDef == null)
        {
            Debug.LogWarning($"[ZoneRoomRegistry] Không tạo được custom room: map {mapId} không tồn tại.");
            return null;
        }

        if (!mapDef.SupportsCustomZones)
        {
            Debug.LogWarning($"[ZoneRoomRegistry] Map {mapId} không cho phép custom/private zones.");
            return null;
        }

        if (!_rooms.TryGetValue(mapId, out var zones))
        {
            zones = new Dictionary<int, ZoneRoom>();
            _rooms[mapId] = zones;
        }

        int zoneId = _nextCustomZoneIdByMap.TryGetValue(mapId, out var nextZoneId) ? nextZoneId : -1;
        while (zones.ContainsKey(zoneId)) zoneId--;
        _nextCustomZoneIdByMap[mapId] = zoneId - 1;

        var room = new ZoneRoom(mapDef, mapDef.CreateCustomZone(Config, zoneId, customRoomName, maxPlayersOverride));
        zones[zoneId] = room;
        Debug.Log($"[ZoneRoomRegistry] Created {room}");
        return room;
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
        {
            oldRoom.RemoveClient(clientId);
            CleanupRoomIfEmpty(oldRoom);
        }

        // Thêm vào zone mới
        newRoom.AddClient(clientId);
        _clientRoom[clientId] = newRoom;
    }

    /// <summary>Xóa client khỏi registry khi disconnect.</summary>
    public void UnregisterClient(ulong clientId)
    {
        if (_clientRoom.TryGetValue(clientId, out var room))
        {
            room.RemoveClient(clientId);
            CleanupRoomIfEmpty(room);
        }
        _clientRoom.Remove(clientId);
    }

    private void CleanupRoomIfEmpty(ZoneRoom room)
    {
        if (room == null || !room.IsCustom || room.PlayerCount > 0)
        {
            Debug.Log($"[RECONNECT-DEBUG][3-Cleanup] Skip: room={room?.ZoneKey ?? "null"} IsCustom={room?.IsCustom} PlayerCount={room?.PlayerCount}");
            return;
        }

        bool isPreserved     = _preservedEmptyCustomRooms.ContainsKey(room.ZoneKey);
        bool hasActiveSession = WaveSessionManager.Instance != null && WaveSessionManager.Instance.HasActiveSessionRoom(room);

        Debug.Log($"[RECONNECT-DEBUG][3-Cleanup] room={room.ZoneKey} isEmpty=true " +
                  $"isPreservedInDict={isPreserved} " +
                  $"hasActiveSessionRoom={hasActiveSession} " +
                  $"WaveSessionManager.Instance={(WaveSessionManager.Instance != null ? "OK" : "NULL!")}");

        if (isPreserved || hasActiveSession)
        {
            MarkRoomPreserved(room, "active wave session");
            Debug.Log($"[RECONNECT-DEBUG][3-Cleanup] PRESERVED room {room.ZoneKey} → room giữ lại, reconnect sẽ thấy room này.");
            Debug.Log($"[ZoneRoomRegistry] Preserve empty custom room {room.ZoneKey} vì vẫn còn active wave session.");
            return;
        }

        _preservedEmptyCustomRooms.Remove(room.ZoneKey);

        if (_rooms.TryGetValue(room.MapId, out var zones) && zones.Remove(room.ZoneId))
        {
            Debug.LogWarning($"[RECONNECT-DEBUG][3-Cleanup] REMOVED room {room.ZoneKey} → reconnect vào room này sẽ THẤT BẠI, sẽ tạo zone mới!");
            Debug.Log($"[ZoneRoomRegistry] Removed empty custom room {room.ZoneKey}");
        }
    }

    private static string BuildZoneKey(int mapId, int zoneId)
        => $"map{mapId}_zone{zoneId}";

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

    public int TotalRoomCount => TotalZoneCount;

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
