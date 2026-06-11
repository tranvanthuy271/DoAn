using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// [SERVER-SIDE] Singleton quản lý việc phân nhóm client theo zone (room) trong 1 NGO server duy nhất.
// Kiến trúc 1 port:
// - Tất cả zone chạy trên cùng 1 NGO server process + 1 port (vd: 7777).
// - Zone được phân biệt bằng room_id (chuỗi logic, vd: "map1_zone0").
// - Khi player đổi zone → gửi ServerRpc → server cập nhật room assignment.
// - Tất cả broadcast (damage, enemy spawn...) dùng RoomBroadcast.ToRoom() để lọc đúng zone.
// Setup:
// - Gắn script này lên GameObject persistent trên SERVER scene (DontDestroyOnLoad).
// - Không cần setup gì trên client.
public class ZoneRoomManager : MonoBehaviour
{
    public static ZoneRoomManager Instance { get; private set; }

    // room_id → danh sách clientId trong zone đó
    private readonly Dictionary<string, HashSet<ulong>> _rooms = new Dictionary<string, HashSet<ulong>>();

    // clientId → room_id (tra cứu ngược)
    private readonly Dictionary<ulong, string> _clientRoom = new Dictionary<ulong, string>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        // Khi server khởi động, theo dõi client disconnect để dọn dẹp
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    //  Public API — gọi từ PlayerZoneHandler.RequestZoneChangeServerRpc

    // Đặt client vào room mới, tự động xóa khỏi room cũ.
    // Chỉ gọi trên server.
    public void AssignClientToRoom(ulong clientId, string newRoomId)
    {
        // Xóa khỏi room cũ
        if (_clientRoom.TryGetValue(clientId, out string oldRoom) && _rooms.ContainsKey(oldRoom))
            _rooms[oldRoom].Remove(clientId);

        // Thêm vào room mới
        if (!_rooms.ContainsKey(newRoomId))
            _rooms[newRoomId] = new HashSet<ulong>();

        _rooms[newRoomId].Add(clientId);
        _clientRoom[clientId] = newRoomId;

        Debug.Log($"[ZoneRoomManager] Client {clientId} → room '{newRoomId}' " +
                  $"(tổng trong room: {_rooms[newRoomId].Count})");
    }

    // Trả về tất cả clientId trong room.
    public HashSet<ulong> GetClientsInRoom(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var clients)
            ? clients
            : new HashSet<ulong>();
    }

    // Trả về room hiện tại của client, null nếu chưa assign.
    public string GetClientRoom(ulong clientId)
    {
        return _clientRoom.TryGetValue(clientId, out var room) ? room : null;
    }

    // Số lượng client đang online trong room.
    public int GetRoomCount(string roomId)
    {
        return _rooms.TryGetValue(roomId, out var c) ? c.Count : 0;
    }

    // Xử lý nội bộ phục vụ các hàm public.

    private void OnClientDisconnect(ulong clientId)
    {
        if (!_clientRoom.TryGetValue(clientId, out string room)) return;
        if (_rooms.ContainsKey(room)) _rooms[room].Remove(clientId);
        _clientRoom.Remove(clientId);
        Debug.Log($"[ZoneRoomManager] Client {clientId} rời room '{room}'");
    }
}
