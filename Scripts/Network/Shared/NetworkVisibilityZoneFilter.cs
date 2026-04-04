using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gắn vào bất kỳ NetworkObject nào cần lọc visibility theo zone.
/// Ví dụ: Player prefab, Enemy prefab, NPCShop, ChestItem.
///
/// Nguyên lý: NGO gọi CheckObjectVisibility(clientId) khi cần quyết định
/// có gửi object này đến client đó không. Ta override bằng logic:
///   "client được thấy object NẾU cùng zone với owner của object".
///
/// QUAN TRỌNG:
/// - Gắn lên NetworkObject prefab, KHÔNG phải spawn manual.
/// - Chạy server-side only (CheckObjectVisibility chỉ chạy trên server).
/// - Gọi RefreshVisibility() sau mỗi zone transfer để NGO re-evaluate.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[DisallowMultipleComponent]
public class NetworkVisibilityZoneFilter : NetworkBehaviour
{
    [Tooltip("Nếu true: server luôn thấy object này (dedicated server không render nên ok).")]
    [SerializeField] private bool _alwaysVisibleToServer = true;

    private NetworkObject _netObj;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        _netObj = GetComponent<NetworkObject>();

        // Đăng ký delegate CheckObjectVisibility với NGO
        // Delegate này sẽ được NGO gọi cho mỗi connected client để quyết định visibility.
        _netObj.CheckObjectVisibility = IsVisibleTo;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core visibility logic
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Trả về true nếu clientId được phép thấy object này.
    /// Logic: cùng zone với owner của object.
    /// </summary>
    private bool IsVisibleTo(ulong clientId)
    {
        // Server có thể xử lý mà không cần visibility
        if (_alwaysVisibleToServer && clientId == NetworkManager.ServerClientId)
            return true;

        var registry = ZoneRoomRegistry.Instance;
        if (registry == null) return true; // Default open khi chưa init

        ulong ownerClientId = _netObj.OwnerClientId;

        // Object không có owner rõ ràng (ví dụ: server-owned NPC)
        // → dùng chính clientId để kiểm tra zone của NPC
        // (NPC nằm ở zone nào thì chỉ client zone đó thấy)
        if (ownerClientId == NetworkManager.ServerClientId ||
            ownerClientId == ulong.MaxValue)
        {
            // Cho NPC/server-owned objects: lấy zone từ component tag
            var zoneTag = GetComponent<ZoneOwnerTag>();
            if (zoneTag == null) return true; // static object, visible to all

            ZoneRoom objectZone = registry.GetRoom(zoneTag.MapId, zoneTag.ZoneId);
            if (objectZone == null) return true;

            return objectZone.Contains(clientId);
        }

        // Player-owned object: cùng zone thì thấy nhau
        return registry.AreInSameZone(ownerClientId, clientId);
    }

    /// <summary>
    /// Gọi sau zone transfer để NGO cập nhật visibility ngay lập tức.
    /// NGO sẽ NetworkHide/NetworkShow tự động dựa theo CheckObjectVisibility.
    /// </summary>
    public void RefreshVisibility()
    {
        if (!IsServer || _netObj == null) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            bool shouldBeVisible = IsVisibleTo(clientId);
            bool isCurrentlyVisible = _netObj.IsNetworkVisibleTo(clientId);

            if (shouldBeVisible && !isCurrentlyVisible)
                _netObj.NetworkShow(clientId);
            else if (!shouldBeVisible && isCurrentlyVisible)
                _netObj.NetworkHide(clientId);
        }
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// ZoneOwnerTag — gắn Vào server-owned objects (NPC, Enemy, Chest, v.v.)
// để khai báo chúng thuộc zone nào.
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Metadata component: khai báo NPC/Enemy/Item này thuộc zone (mapId, zoneId).
/// Được NetworkVisibilityZoneFilter dùng để lọc visibility.
/// </summary>
public class ZoneOwnerTag : MonoBehaviour
{
    [SerializeField] public int MapId;
    [SerializeField] public int ZoneId;

    /// <summary>Gọi khi spawn enemy/NPC vào zone cụ thể.</summary>
    public void SetZone(int mapId, int zoneId)
    {
        MapId  = mapId;
        ZoneId = zoneId;
    }
}
