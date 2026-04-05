using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Netcode;

/// <summary>
/// MapPortalTrigger — Cổng dịch chuyển giữa các map/phòng trong phó bản.
///
/// CÁCH HOẠT ĐỘNG (theo pattern WayPoint của LangLa):
///   1. Đặt GameObject này tại vị trí cổng trong Unity scene
///   2. Thêm BoxCollider2D (isTrigger = true) để define vùng trigger
///   3. Khi LocalPlayer bước vào trigger → gọi API POST /api/map/travel
///   4. API validate (vị trí hợp lệ, có chìa khóa không)
///   5. Nếu được phép → load scene mới + dịch chuyển player đến dest_x, dest_y
///
/// SETUP TRONG EDITOR:
///   - portalId: lấy từ DB bảng map_portal.portal_id
///   - currentMapId: map hiện tại
///   - Hoặc dùng DungeonManager.LoadPortalsFromServer() để auto-populate
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MapPortalTrigger : MonoBehaviour
{
    [Header("Portal Data (từ DB)")]
    public int portalId;
    public int currentMapId;
    [Tooltip("Loại: enter_dungeon | room_transition | exit_dungeon")]
    public string portalType = "room_transition";

    [Header("Destination (auto-filled từ API)")]
    public int destMapId;
    public string destSceneName;
    public float destX;
    public float destY;

    [Header("Zone Transfer")]
    [Tooltip("Zone đích ưu tiên trong map mới. 0 = zone public mặc định đầu tiên")]
    public int preferredZoneId = 0;

    [Header("Điều kiện vào")]
    [Tooltip("0 = không cần item. Ngược lại = item_template.id của Chìa Khóa")]
    public int requiredItemId = 0;

    [Header("Visual & UX")]
    public GameObject portalVisual;            // Particle/sprite cổng
    public GameObject keyRequiredPrompt;       // UI hiện khi chưa có chìa khóa
    [Tooltip("Giây delay trước khi bắt đầu chuyển scene (animation fade out)")]
    public float transitionDelay = 0.8f;

    private bool _isTransitioning = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTransitioning) return;
        if (!other.CompareTag("Player")) return;

        // Chỉ Local Player mới kích hoạt
        var netObj = other.GetComponent<NetworkObject>();
        if (netObj != null && !netObj.IsOwner) return;

        StartCoroutine(TryTravel(other.gameObject));
    }

    private IEnumerator TryTravel(GameObject player)
    {
        _isTransitioning = true;

        // Lấy playerId từ local player data
        int playerId = GetLocalPlayerId();
        Vector3 pos   = player.transform.position;

        // Gọi API validate
        string url  = $"{ServerConfig.BaseUrl}/api/map/travel";
        string body = JsonUtility.ToJson(new TravelRequestPayload
        {
            portal_id      = portalId,
            player_id      = playerId,
            current_map_id = currentMapId,
            player_x       = pos.x,
            player_y       = pos.y
        });

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[MapPortalTrigger] API lỗi: {req.error}");
            ShowDenied("Không thể kết nối máy chủ.");
            _isTransitioning = false;
            yield break;
        }

        var response = JsonUtility.FromJson<TravelResponse>(req.downloadHandler.text);

        if (!response.success)
        {
            ShowDenied(response.message);
            _isTransitioning = false;
            yield break;
        }

        // Fade out rồi gửi ServerRpc chuyển map theo kiến trúc 1-port
        yield return new WaitForSeconds(transitionDelay);

        var transitionController = FindAnyObjectByType<ZoneTransitionController>();
        if (transitionController == null)
        {
            Debug.LogError("[MapPortalTrigger] Không tìm thấy ZoneTransitionController trong scene.");
            ShowDenied("Không thể chuyển khu lúc này.");
            _isTransitioning = false;
            yield break;
        }

        transitionController.RequestMapPortalTransferServerRpc(
            response.dest_map_id,
            preferredZoneId,
            response.dest_x,
            response.dest_y);

        _isTransitioning = false;
    }

    private void ShowDenied(string msg)
    {
        Debug.Log($"[Portal] Không thể đi qua: {msg}");
        if (keyRequiredPrompt != null)
        {
            keyRequiredPrompt.SetActive(true);
            StartCoroutine(HideAfter(keyRequiredPrompt, 2.5f));
        }
    }

    private IEnumerator HideAfter(GameObject go, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (go != null) go.SetActive(false);
    }

    private int GetLocalPlayerId()
    {
        // Tìm component PlayerData trên local player
        var localPlayer = FindLocalPlayer();
        if (localPlayer != null)
        {
            var pd = localPlayer.GetComponent<PlayerDataHolder>();
            if (pd != null) return pd.PlayerId;
        }
        return -1;
    }

    private GameObject FindLocalPlayer()
    {
        foreach (var go in GameObject.FindGameObjectsWithTag("Player"))
        {
            var net = go.GetComponent<NetworkObject>();
            if (net != null && net.IsOwner) return go;
        }
        return null;
    }

    // ── Serializable payloads ──

    [Serializable]
    private class TravelRequestPayload
    {
        public int   portal_id;
        public int   player_id;
        public int   current_map_id;
        public float player_x;
        public float player_y;
    }

    [Serializable]
    private class TravelResponse
    {
        public bool   success;
        public string message;
        public int    dest_map_id;
        public string dest_scene_name;
        public float  dest_x;
        public float  dest_y;
        public string portal_type;
        public string portal_name;
    }
}

/// <summary>
/// Static holder: lưu vị trí đến khi cổng dịch chuyển thành công.
/// PortalArrivalHandler.cs đọc sau khi scene mới load.
/// </summary>
public static class PortalArrivalHandler
{
    public static float PendingDestX  = 0f;
    public static float PendingDestY  = 0f;
    public static int   PendingMapId  = -1;

    /// <summary>
    /// Gọi từ PlayerSpawner hoặc Start() để đặt player vào đúng vị trí đến.
    /// </summary>
    public static void ApplyPendingArrival(Transform playerTransform)
    {
        if (PendingMapId >= 0)
        {
            playerTransform.position = new Vector3(PendingDestX, PendingDestY, 0f);
            PendingMapId = -1;  // reset
        }
    }
}

/// <summary>
/// Placeholder — thay bằng script quản lý PlayerId thực tế trong dự án.
/// </summary>
public class PlayerDataHolder : MonoBehaviour
{
    public int PlayerId = -1;
}

/// <summary>
/// Placeholder config URL server.
/// </summary>
public static class ServerConfig
{
    public static string BaseUrl = "http://localhost:5000";
}
