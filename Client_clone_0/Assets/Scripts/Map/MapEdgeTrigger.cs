using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Trigger biên map trái/phải — đặt tại rìa scene, player bước vào tự chuyển map.
/// Không cần config portalId thủ công — tự lookup từ API theo direction.
///
/// Inspector:
///   direction       = "left" hoặc "right"
///   currentMapId    = -1 (auto-detect từ MapManager) | 0 = GameScene | 1 = Map1 | ...
///   transitionDelay = 0.5 (giây chờ trước khi load)
///
/// LƯU Ý: currentMapId = -1 chỉ đáng tin khi player KHÔNG đứng ngay biên từ đầu game
/// (MapManager cần vài giây fetch API). Khi không chắc, hãy set số cụ thể.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MapEdgeTrigger : MonoBehaviour
{
    [Header("Hướng di chuyển")]
    [Tooltip("'left' = đi map trước, 'right' = đi map tiếp theo")]
    [SerializeField] private string direction = "right";

    [Tooltip("MapId của scene này.\n-1 = tự lấy từ MapManager (chỉ dùng nếu MapManager đã fetch xong)\n 0 = GameScene\n 1 = Map1 ...")]
    [SerializeField] private int currentMapId = -1;

    [Tooltip("Zone đích ưu tiên trong map mới. 0 = zone public mặc định đầu tiên")]
    [SerializeField] private int preferredZoneId = 0;

    [Header("UX")]
    [SerializeField] private GameObject loadingPanel;
#pragma warning disable CS0414
    [SerializeField] private float transitionDelay = 0.5f;
#pragma warning restore CS0414

    [Header("API")]
    [SerializeField] private string apiBase = "";

    private bool _isTransitioning = false;

    // Không dùng Start() để resolve mapId nữa — resolve tại thời điểm trigger
    // (tránh race condition với MapManager.FetchMapConfigByScene coroutine)

    private void Awake()
    {
        apiBase = ServerAddressConfig.Instance.ResolveApiRoot(apiBase);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isTransitioning) return;
        if (ClientSceneController.IsTransferTriggerBlocked()) return;

        // ── Khớp cách ZoneTrigger detect: chỉ cần NetworkObject + IsOwner ──
        // KHÔNG check CompareTag vì Player có thể chưa được tag đúng
        if (!other.TryGetComponent<NetworkObject>(out var netObj)) return;
        if (!netObj.IsOwner) return;

        Debug.Log($"[MapEdgeTrigger] Trigger! obj={other.name} direction={direction} mapId={ResolveMapId()}");
        StartCoroutine(DoTravel(other.gameObject));
    }

    /// <summary>
    /// Resolve mapId tại thời điểm cần (không phải Start) để MapManager kịp fetch API.
    /// </summary>
    private int ResolveMapId()
    {
        if (currentMapId >= 0) return currentMapId;                      // Inspector value
        return MapManager.Instance != null ? MapManager.Instance.GetMapId() : 0;
    }

    private IEnumerator DoTravel(GameObject player)
    {
        _isTransitioning = true;
        LoginLoadingManager.ShowLoadingStatic("Đang chuyển map...");
        if (loadingPanel) loadingPanel.SetActive(false);

        int mapId = ResolveMapId();

        // ── Bước 1: tìm portal theo direction ──
        string url = $"{apiBase}/api/map/portal/direction?mapId={mapId}&direction={direction}";
        Debug.Log($"[MapEdgeTrigger] Bước 1 — GET {url}");
        using var portalReq = UnityWebRequest.Get(url);
        portalReq.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return portalReq.SendWebRequest();

        if (portalReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[MapEdgeTrigger] Bước 1 FAIL — portal '{direction}' map {mapId}: {portalReq.error} | HTTP={portalReq.responseCode}");
            ResetTrigger(hideGlobalLoading: true);
            yield break;
        }

        Debug.Log($"[MapEdgeTrigger] Bước 1 OK — portal response: {portalReq.downloadHandler.text}");

        var portal = JsonUtility.FromJson<PortalInfo>(portalReq.downloadHandler.text);

        // ── Bước 2: validate travel với server ──
        Vector3 pos = player.transform.position;
        var payload = new TravelPayload
        {
            portal_id      = portal.portal_id,
            player_id      = GetLocalPlayerId(player),
            current_map_id = mapId,
            player_x       = pos.x,
            player_y       = pos.y
        };

        string json = JsonUtility.ToJson(payload);
        Debug.Log($"[MapEdgeTrigger] Bước 2 — POST travel: {json}");
        using var travelReq = new UnityWebRequest($"{apiBase}/api/map/travel", "POST");
        travelReq.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        travelReq.downloadHandler = new DownloadHandlerBuffer();
        travelReq.SetRequestHeader("Content-Type", "application/json");
        travelReq.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return travelReq.SendWebRequest();

        if (travelReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[MapEdgeTrigger] Bước 2 FAIL — Travel lỗi HTTP={travelReq.responseCode}: {travelReq.downloadHandler.text}");
            ResetTrigger(hideGlobalLoading: true);
            yield break;
        }

        Debug.Log($"[MapEdgeTrigger] Bước 2 OK — Travel response: {travelReq.downloadHandler.text}");
        var resp = JsonUtility.FromJson<TravelResponse>(travelReq.downloadHandler.text);
        if (!resp.success)
        {
            Debug.LogWarning($"[MapEdgeTrigger] Bước 2 — Server từ chối: {resp.message}");
            ResetTrigger(hideGlobalLoading: true);
            yield break;
        }

        // ── Bước 3: gửi ServerRpc portal-transfer theo kiến trúc 1-port ──
        var transitionController = FindAnyObjectByType<ZoneTransitionController>();
        if (transitionController == null)
        {
            Debug.LogWarning("[MapEdgeTrigger] Không tìm thấy ZoneTransitionController để chuyển map.");
            ResetTrigger(hideGlobalLoading: true);
            yield break;
        }

        ClientSceneController.MarkTransferRequestStarted();
        transitionController.RequestMapPortalTransferServerRpc(
            resp.dest_map_id,
            preferredZoneId,
            resp.dest_x,
            resp.dest_y);

        Debug.Log($"[MapEdgeTrigger] Bước 3 — RequestMapPortalTransfer → map={resp.dest_map_id} zone={preferredZoneId} pos=({resp.dest_x},{resp.dest_y})");
        ResetTrigger(hideGlobalLoading: false);
    }

    private void ResetTrigger(bool hideGlobalLoading)
    {
        _isTransitioning = false;
        if (hideGlobalLoading)
        {
            LoginLoadingManager.HideLoadingStatic();
        }

        if (loadingPanel) loadingPanel.SetActive(false);
    }

    private int GetLocalPlayerId(GameObject player)
    {
        var pd = player.GetComponent<PlayerDataHolder>();
        return pd != null ? pd.PlayerId : PlayerPrefs.GetInt("USER_ID", -1);
    }

    // ── DTOs ──

    [Serializable]
    private class PortalInfo { public int portal_id; }

    [Serializable]
    private class TravelPayload
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
    }
}
