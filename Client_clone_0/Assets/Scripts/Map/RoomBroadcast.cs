using Unity.Netcode;
using System.Collections.Generic;

// Utility gửi ClientRpc chỉ đến các client trong cùng zone (room).
// Thay vì broadcast tới TẤT CẢ client, dùng ClientRpcParams để lọc
// chỉ những client có cùng room_id.
// Ví dụ dùng trong NetworkEnemyHealth:
// <code>
// string myRoom = GetComponent&lt;EnemyZoneTag&gt;().RoomId;
// var rpcTarget = RoomBroadcast.ToRoom(myRoom, ZoneRoomManager.Instance);
// SyncDamageClientRpc(damage, rpcTarget);
// </code>
public static class RoomBroadcast
{
    // Tạo ClientRpcParams nhắm đúng các client trong room chỉ định.
    // Dùng cho tham số cuối của [ClientRpc] method.
    public static ClientRpcParams ToRoom(string roomId, ZoneRoomManager roomMgr)
    {
        if (roomMgr == null)
        {
            UnityEngine.Debug.LogWarning("[RoomBroadcast] ZoneRoomManager null — broadcast tới all client.");
            return default;
        }

        var clients = roomMgr.GetClientsInRoom(roomId);
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new List<ulong>(clients)
            }
        };
    }

    // Tạo ClientRpcParams nhắm đúng các client trong room của một NetworkObject cụ thể.
    // Tiện lợi khi gọi từ chính object đó (vd: Enemy gọi từ NetworkBehaviour của nó).
    public static ClientRpcParams ToSameRoomAs(Unity.Netcode.NetworkBehaviour sender, ZoneRoomManager roomMgr)
    {
        if (roomMgr == null) return default;

        // Lấy room_id hiện tại của sender qua ZoneRoomManager
        // (server đã assign khi enemy spawn hoặc player vào zone)
        string room = roomMgr.GetClientRoom(sender.OwnerClientId);
        if (string.IsNullOrEmpty(room))
        {
            UnityEngine.Debug.LogWarning($"[RoomBroadcast] Không tìm thấy room cho client {sender.OwnerClientId}");
            return default;
        }

        return ToRoom(room, roomMgr);
    }
}
