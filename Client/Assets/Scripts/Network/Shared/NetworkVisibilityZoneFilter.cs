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
/// - Đây là component server-side thuần, có thể add runtime TRƯỚC khi Spawn().
/// - Không dùng NetworkBehaviour vì NPC/enemy đang được tạo từ prefab instance runtime.
/// - Gọi InitializeForServer() trước Spawn(), rồi RefreshVisibility() sau khi client đổi zone.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[DisallowMultipleComponent]
public class NetworkVisibilityZoneFilter : MonoBehaviour
{
    [Tooltip("Nếu true: server luôn thấy object này (dedicated server không render nên ok).")]
    [SerializeField] private bool _alwaysVisibleToServer = true;

    private NetworkObject _netObj;
    private bool _initialized;

    private void Awake()
    {
        _netObj = GetComponent<NetworkObject>();
    }

    public void InitializeForServer()
    {
        _netObj ??= GetComponent<NetworkObject>();
        if (_netObj == null)
        {
            Debug.LogWarning("[NetworkVisibilityZoneFilter] Thiếu NetworkObject, không thể khởi tạo visibility filter.");
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        // Đăng ký delegate CheckObjectVisibility với NGO
        // Delegate này sẽ được NGO gọi cho mỗi connected client để quyết định visibility.
        _netObj.CheckObjectVisibility = IsVisibleTo;
        _initialized = true;

        var zoneTag = GetComponent<ZoneOwnerTag>();
        Debug.Log($"[NetworkVisibilityZoneFilter] Initialized on '{gameObject.name}' " +
                  $"(netId={_netObj.NetworkObjectId}, zoneTag={zoneTag?.MapId}_{zoneTag?.ZoneId})");
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
        _netObj ??= GetComponent<NetworkObject>();
        if (_netObj == null)
            return false;

        // Server có thể xử lý mà không cần visibility
        if (_alwaysVisibleToServer && clientId == NetworkManager.ServerClientId)
            return true;

        var registry = ZoneRoomRegistry.Instance;
        if (registry == null)
        {
            Debug.LogWarning($"[NetworkVisibilityZoneFilter] registry=null → default visible for '{gameObject.name}' client={clientId}");
            return true; // Default open khi chưa init
        }

        ulong ownerClientId = _netObj.OwnerClientId;

        // Server-owned objects (NPC, Enemy, Chest, etc.)
        // → dùng MAP-BASED visibility: tất cả player cùng map đều thấy, bất kể zone
        if (ownerClientId == NetworkManager.ServerClientId ||
            ownerClientId == ulong.MaxValue)
        {
            var zoneTag = GetComponent<ZoneOwnerTag>();
            if (zoneTag == null)
                return true; // static object, visible to all

            // Lấy zone hiện tại của client → kiểm tra cùng MAP
            var clientRoom = registry.GetClientRoom(clientId);
            if (clientRoom == null)
                return false; // client chưa được assign vào zone nào

            return clientRoom.MapId == zoneTag.MapId;
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
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (!_initialized)
            InitializeForServer();

        if (_netObj == null) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            bool shouldBeVisible = IsVisibleTo(clientId);
            bool isCurrentlyVisible = _netObj.IsNetworkVisibleTo(clientId);

            if (shouldBeVisible && !isCurrentlyVisible)
            {
                _netObj.NetworkShow(clientId);
                Debug.Log($"[NetworkVisibilityZoneFilter] SHOW '{gameObject.name}' to client {clientId}");
            }
            else if (!shouldBeVisible && isCurrentlyVisible)
            {
                _netObj.NetworkHide(clientId);
                Debug.Log($"[NetworkVisibilityZoneFilter] HIDE '{gameObject.name}' from client {clientId}");
            }
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
