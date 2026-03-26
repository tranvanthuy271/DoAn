using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

/// <summary>
/// Gắn vào Player Prefab.
/// Xử lý yêu cầu đổi zone từ client → server → cập nhật room assignment + teleport player.
///
/// Dùng FixedString64Bytes thay vì string vì NGO NetworkVariable/ServerRpc
/// yêu cầu kiểu dữ liệu unmanaged (string là managed type).
///
/// Setup:
///   - Kéo script này vào Player Prefab (cùng GameObject với NetworkObject).
///   - Không cần setup thêm gì khác.
/// </summary>
public class PlayerZoneHandler : NetworkBehaviour
{
    // ──────────────────────────────────────────────────────────────────
    //  NetworkVariable — sync room hiện tại xuống tất cả client
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// room_id của zone player đang đứng. Server write, tất cả client đọc được.
    /// Mặc định "" = chưa assign (giống lobby/default zone).
    /// </summary>
    public NetworkVariable<FixedString64Bytes> CurrentRoomId = new NetworkVariable<FixedString64Bytes>(
        new FixedString64Bytes(""),
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    // ──────────────────────────────────────────────────────────────────
    //  Public helpers
    // ──────────────────────────────────────────────────────────────────

    /// <summary>Lấy room_id dạng string (tiện hơn FixedString64Bytes).</summary>
    public string RoomId => CurrentRoomId.Value.ToString();

    /// <summary>Kiểm tra player có đang ở cùng zone với player khác không.</summary>
    public bool IsSameRoom(PlayerZoneHandler other) => RoomId == other.RoomId;

    // ──────────────────────────────────────────────────────────────────
    //  ServerRpc — gọi từ ZoneTrigger.cs phía client
    // ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Client gọi khi player bước qua ZoneTrigger.
    /// Server xác nhận, cập nhật room assignment và teleport player đến spawn point.
    /// </summary>
    /// <param name="newRoomId">room_id của zone đích (lấy từ API map/zone)</param>
    /// <param name="spawnX">Tọa độ X spawn trong zone mới</param>
    /// <param name="spawnY">Tọa độ Y spawn trong zone mới</param>
    [ServerRpc(RequireOwnership = true)]
    public void RequestZoneChangeServerRpc(FixedString64Bytes newRoomId, float spawnX, float spawnY)
    {
        string roomIdStr = newRoomId.ToString();

        // Cập nhật room assignment trong ZoneRoomManager
        var roomMgr = ZoneRoomManager.Instance;
        if (roomMgr != null)
            roomMgr.AssignClientToRoom(OwnerClientId, roomIdStr);
        else
            Debug.LogWarning("[PlayerZoneHandler] ZoneRoomManager chưa được khởi tạo trên server!");

        // Cập nhật NetworkVariable để tất cả client biết room mới
        CurrentRoomId.Value = newRoomId;

        // Teleport player đến spawn point của zone mới (server-authoritative)
        transform.position = new UnityEngine.Vector3(spawnX, spawnY, 0f);

        // Thông báo cho chính client đó biết zone đã đổi thành công
        OnZoneChangedClientRpc(newRoomId, spawnX, spawnY,
            new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } } });

        Debug.Log($"[PlayerZoneHandler] Player {OwnerClientId} → zone '{roomIdStr}' @ ({spawnX},{spawnY})");
    }

    // ──────────────────────────────────────────────────────────────────
    //  ClientRpc — callback về client sau khi đổi zone thành công
    // ──────────────────────────────────────────────────────────────────

    [ClientRpc]
    private void OnZoneChangedClientRpc(FixedString64Bytes newRoomId, float spawnX, float spawnY,
        ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[PlayerZoneHandler] Đã vào zone '{newRoomId}' tại ({spawnX:F1},{spawnY:F1})");
        // Tuỳ chọn: ẩn loading panel, hiện thông báo "Đến Khu B"...
    }
}
