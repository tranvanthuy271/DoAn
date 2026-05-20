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
            GlobalNotificationUI.Show("Không tìm thấy đường đi ở hướng này.", "Không thể chuyển map", 3f);
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
            // Đọc message lỗi từ body (server trả HTTP 400 với JSON {"message":"..."})
            string deniedMsg = "Không thể chuyển map lúc này.";
            if (!string.IsNullOrEmpty(travelReq.downloadHandler.text))
            {
                try
                {
                    var errBody = JsonUtility.FromJson<TravelResponse>(travelReq.downloadHandler.text);
                    string parsed = errBody?.GetErrorMessage();
                    if (!string.IsNullOrEmpty(parsed))
                        deniedMsg = parsed;
                }
                catch { /* giữ message mặc định */ }
            }
            Debug.LogWarning($"[MapEdgeTrigger] Bước 2 FAIL — Travel lỗi HTTP={travelReq.responseCode}: {travelReq.downloadHandler.text}");
            GlobalNotificationUI.Show(deniedMsg, "Không thể vào khu vực này", 4f);
            ResetTrigger(hideGlobalLoading: true);
            yield break;
        }

        Debug.Log($"[MapEdgeTrigger] Bước 2 OK — Travel response: {travelReq.downloadHandler.text}");
        var resp = JsonUtility.FromJson<TravelResponse>(travelReq.downloadHandler.text);
        if (!resp.success)
        {
            string errMsg = resp.GetErrorMessage() ?? "Server từ chối chuyển map.";
            Debug.LogWarning($"[MapEdgeTrigger] Bước 2 — Server từ chối: {errMsg}");
            GlobalNotificationUI.Show(errMsg, "Không thể vào khu vực này", 4f);
            ResetTrigger(hideGlobalLoading: true);
            yield break;
        }

        Vector2 arrivalPos = new Vector2(resp.dest_x, resp.dest_y);
        yield return StartCoroutine(ResolveDirectionalArrivalPosition(resp.dest_map_id, direction, arrivalPos, resolved => arrivalPos = resolved));

        // ── Bước 3: gửi ServerRpc portal-transfer theo kiến trúc 1-port ──
        var transitionController = FindAnyObjectByType<ZoneTransitionController>();
        if (transitionController == null)
        {
            Debug.LogWarning("[MapEdgeTrigger] Không tìm thấy ZoneTransitionController để chuyển map.");
            GlobalNotificationUI.Show("Không thể chuyển map lúc này.", "Lỗi", 3f);
            ResetTrigger(hideGlobalLoading: true);
            yield break;
        }

        ClientSceneController.MarkTransferRequestStarted();
        transitionController.RequestMapPortalTransferServerRpc(
            resp.dest_map_id,
            preferredZoneId,
            arrivalPos.x,
            arrivalPos.y);

        Debug.Log($"[MapEdgeTrigger] Bước 3 — RequestMapPortalTransfer → map={resp.dest_map_id} zone={preferredZoneId} pos=({arrivalPos.x},{arrivalPos.y})");
        ResetTrigger(hideGlobalLoading: false);
    }

    private IEnumerator ResolveDirectionalArrivalPosition(int targetMapId, string travelDirection, Vector2 fallbackPos, Action<Vector2> onResolved)
    {
        string oppositeDirection = travelDirection == "left" ? "right" : "left";
        string url = $"{apiBase}/api/map/portal/direction?mapId={targetMapId}&direction={oppositeDirection}";
        Debug.Log($"[MapEdgeTrigger] Resolve arrival — GET {url}");

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var portal = JsonUtility.FromJson<PortalInfo>(req.downloadHandler.text);
            // Offset 1.5 units inward from the edge trigger so the player lands
            // on solid ground instead of exactly at the boundary collider.
            // oppositeDirection == "right"  → player arrives from left, move left  (x - offset)
            // oppositeDirection == "left"   → player arrives from right, move right (x + offset)
            const float inwardOffset = 1.5f;
            float arrX = oppositeDirection == "right" ? portal.src_x - inwardOffset : portal.src_x + inwardOffset;
            Vector2 resolved = new Vector2(arrX, portal.src_y);
            Debug.Log($"[MapEdgeTrigger] Resolve arrival OK — target {oppositeDirection} portal map={targetMapId} raw=({portal.src_x},{portal.src_y}) adjusted=({resolved.x},{resolved.y})");
            onResolved?.Invoke(resolved);
        }
        else
        {
            Debug.LogWarning($"[MapEdgeTrigger] Resolve arrival FAIL — target {oppositeDirection} portal map={targetMapId}. Fallback dest_x/dest_y. HTTP={req.responseCode} err={req.error}");
            onResolved?.Invoke(fallbackPos);
        }
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
    private class PortalInfo
    {
        public int portal_id;
        public float src_x;
        public float src_y;
    }

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
        public string message;   // BadRequest 400: {"message":"..."}
        public string error;     // Exception 500: {"error":"..."}
        public int    dest_map_id;
        public string dest_scene_name;
        public float  dest_x;
        public float  dest_y;

        /// <summary>Lấy nội dung lỗi từ bất kỳ field nào có giá trị.</summary>
        public string GetErrorMessage()
        {
            if (!string.IsNullOrEmpty(message)) return message;
            if (!string.IsNullOrEmpty(error))   return error;
            return null;
        }
    }
}
