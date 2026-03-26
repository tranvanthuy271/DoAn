using UnityEngine;
using Unity.Netcode;
using Unity.Collections;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Đặt BoxCollider2D (isTrigger) tại ranh giới giữa hai zone trong cùng map.
///
/// Kiến trúc 1 port (không disconnect/reconnect):
///   - Player bước qua trigger → fetch room_id của zone đích từ API
///   - Gửi PlayerZoneHandler.RequestZoneChangeServerRpc(room_id, spawnX, spawnY)
///   - Server cập nhật room assignment + teleport player
///   - KHÔNG cần shutdown NGO hay reconnect sang port khác
///
/// Setup:
///   1. Tạo GameObject, thêm BoxCollider2D (Is Trigger = true).
///   2. Gắn script này.
///   3. Điền targetZoneIndex, mapId (hoặc để 0 để tự lấy từ MapManager),
///      spawnX/spawnY (vị trí player khi vào zone mới).
///   4. Player Prefab phải có PlayerZoneHandler.cs gắn kèm.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class ZoneTrigger : MonoBehaviour
{
    [Header("Zone đích")]
    [Tooltip("ZoneIndex của zone muốn chuyển đến (theo map_zone_config.zone_index)")]
    [SerializeField] private int targetZoneIndex;

    [Tooltip("Map ID của scene này — tự lấy từ MapManager nếu để 0")]
    [SerializeField] private int mapId = 0;

    [Header("Vị trí spawn khi vào zone mới")]
    [SerializeField] private float spawnX;
    [SerializeField] private float spawnY;

    [Header("API")]
    [SerializeField] private string apiBase = "http://localhost:5000";

    private bool triggered = false;

    private void Start()
    {
        if (mapId == 0 && MapManager.Instance != null)
            mapId = MapManager.Instance.GetMapId();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;

        // Chỉ xử lý cho player local (owner)
        if (!other.TryGetComponent<NetworkObject>(out var netObj)) return;
        if (!netObj.IsOwner) return;

        triggered = true;
        StartCoroutine(FetchAndSwitchZone(other.gameObject));
    }

    private IEnumerator FetchAndSwitchZone(GameObject playerObj)
    {
        // 1. Lấy room_id của zone đích từ API
        string url = $"{apiBase}/api/map/zone?mapId={mapId}&zoneIndex={targetZoneIndex}";
        using var req = UnityWebRequest.Get(url);
        AuthHelper.AddAuthHeader(req);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ZoneTrigger] Lấy zone config thất bại: {req.error}");
            triggered = false;
            yield break;
        }

        ZoneData zoneData;
        try
        {
            zoneData = JsonUtility.FromJson<ZoneData>(req.downloadHandler.text);
        }
        catch
        {
            Debug.LogError("[ZoneTrigger] Parse zone data thất bại.");
            triggered = false;
            yield break;
        }

        if (string.IsNullOrEmpty(zoneData.room_id))
        {
            Debug.LogError($"[ZoneTrigger] Zone {targetZoneIndex} chưa có room_id trong DB!");
            triggered = false;
            yield break;
        }

        // 2. Gửi yêu cầu đổi zone lên server qua PlayerZoneHandler
        //    KHÔNG cần disconnect/reconnect — server tự route client vào room đúng
        if (playerObj.TryGetComponent<PlayerZoneHandler>(out var handler))
        {
            handler.RequestZoneChangeServerRpc(
                new FixedString64Bytes(zoneData.room_id),
                spawnX,
                spawnY);

            Debug.Log($"[ZoneTrigger] Gửi yêu cầu → zone '{zoneData.room_id}' @ ({spawnX},{spawnY})");
        }
        else
        {
            Debug.LogError("[ZoneTrigger] Player Prefab thiếu PlayerZoneHandler component!");
            triggered = false;
            yield break;
        }

        // Reset sau delay để không trigger lại ngay nếu player bước lùi
        Invoke(nameof(ResetTrigger), 2f);
    }

    private void ResetTrigger() => triggered = false;

    [System.Serializable]
    private class ZoneData
    {
        public string room_id;
        public string zone_name;
        public string host_ip;   // chỉ để log, không dùng để connect
        public int    host_port; // luôn là 7777, không dùng để reconnect
    }
}
