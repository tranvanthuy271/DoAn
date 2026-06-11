using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

// Đặt BoxCollider2D (isTrigger) tại ranh giới giữa hai zone trong cùng map.
// Kiến trúc 1 port — toàn bộ cấu hình được set trong Inspector, KHÔNG cần DB:
// - roomId:  định danh zone đích (VD: "map1_zone1") — set trong Inspector
// - spawnX/spawnY: vị trí player khi vào zone mới
// - Player bước qua → PlayerZoneHandler.RequestZoneChangeServerRpc(roomId, spawnX, spawnY)
// - Server route client vào room đúng, KHÔNG reconnect/shutdown NGO
// Setup:
// 1. Thêm Empty GameObject tại ranh giới zone, đặt tên "ZoneTrigger_A_to_B".
// 2. Add Component: BoxCollider2D (Is Trigger = true) + ZoneTrigger.
// 3. Điền roomId (VD: "map0_zone1"), spawnX / spawnY trong Inspector.
// 4. Player Prefab phải có PlayerZoneHandler.cs gắn kèm.
[RequireComponent(typeof(BoxCollider2D))]
public class ZoneTrigger : MonoBehaviour
{
    [Header("Zone đích")]
    [Tooltip("Tên định danh zone — khớp với PlayerZoneHandler trên server.\nVD: \"map0_zone0\", \"map0_zone1\"")]
    [SerializeField] private string roomId = "map0_zone0";

    [Tooltip("Tên hiển thị lên UI khi player vào zone này.\nVD: \"Khu Rừng Băng\", \"Đồng Bằng Lửa\"")]
    [SerializeField] private string zoneName = "";

    [Header("Vị trí spawn khi vào zone mới")]
    [SerializeField] private float spawnX;
    [SerializeField] private float spawnY;

    private bool _triggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_triggered) return;

        // Chỉ xử lý cho local owner
        if (!other.TryGetComponent<NetworkObject>(out var netObj)) return;
        if (!netObj.IsOwner) return;

        _triggered = true;
        SwitchZone(other.gameObject);
    }

    private void SwitchZone(GameObject playerObj)
    {
        if (string.IsNullOrEmpty(roomId))
        {
            { /* Lỗi: roomId chưa được set trong Inspector */ }
            _triggered = false;
            return;
        }

        if (playerObj.TryGetComponent<PlayerZoneHandler>(out var handler))
        {
            handler.RequestZoneChangeServerRpc(new FixedString64Bytes(roomId), spawnX, spawnY);
            { /* Chuyển zone → '{roomId}' @ ({spawnX},{spawnY}) */ }

            // Hiện tên zone trên UI nếu đã set
            if (!string.IsNullOrEmpty(zoneName))
                ZoneNameBanner.Instance?.Show(zoneName);
        }
        else
        {
            { /* Lỗi: Player Prefab thiếu PlayerZoneHandler component */ }
            _triggered = false;
            return;
        }

        // Reset sau 2 giây để không trigger lại ngay nếu player bước lùi
        Invoke(nameof(ResetTrigger), 2f);
    }

    private void ResetTrigger() => _triggered = false;
}
