using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

// Gắn vào Button UI "← Trái" hoặc "Phải →" để chuyển map qua portal.
// Setup trong Inspector:
// - isRightButton: true = nút phải (map tiếp), false = nút trái (map trước)
// - currentMapId: mapId của scene hiện tại
// - button: kéo Button component vào
// - loadingPanel (tuỳ chọn): panel loading hiện khi đang chuyển
public class MapTransitionButton : MonoBehaviour
{
    [Header("Loại nút")]
    [Tooltip("true = nút bên phải (đi tới), false = nút bên trái (quay lại)")]
    [SerializeField] private bool isRightButton = true;

    [Tooltip("Map ID của scene hiện tại. Để -1 sẽ tự lấy từ MapManager.")]
    [SerializeField] private int currentMapId = -1;

    [Tooltip("Zone đích ưu tiên trong map mới. 0 = zone public mặc định đầu tiên")]
    [SerializeField] private int preferredZoneId = 0;

    [Header("UI")]
    [SerializeField] private Button      button;
    [SerializeField] private TMP_Text    buttonLabel;
    [SerializeField] private GameObject  loadingPanel;
    [SerializeField] private TMP_Text    errorText;          // hiện lỗi cho player (tuỳ chọn)
    [SerializeField] private float       errorDisplayTime = 3f;

    [Header("API")]
    [SerializeField] private string apiBase = "";

    private void Start()
    {
        apiBase = ServerAddressConfig.Instance.ResolveApiRoot(apiBase);

        if (currentMapId < 0 && MapManager.Instance != null)
            currentMapId = MapManager.Instance.GetMapId();

        if (buttonLabel != null)
            buttonLabel.text = isRightButton ? "→" : "←";

        button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        StartCoroutine(DoTravel());
    }

    private IEnumerator DoTravel()
    {
        button.interactable = false;
        LoginLoadingManager.ShowLoadingStatic("Đang chuyển map...");
        if (loadingPanel) loadingPanel.SetActive(false);

        // 1. Lấy portal trái hoặc phải của map hiện tại
        string direction = isRightButton ? "right" : "left";
        yield return StartCoroutine(ResolveMapIdByActiveScene(currentMapId, resolved => currentMapId = resolved));
        string url = $"{apiBase}/api/map/portal/direction?mapId={currentMapId}&direction={direction}";

        using var portalReq = UnityWebRequest.Get(url);
        yield return portalReq.SendWebRequest();

        if (portalReq.result != UnityWebRequest.Result.Success)
        {
            { /* Lỗi: Không tìm được portal: {portalReq.error} */ }
            ShowError("Không có đường đi " + (isRightButton ? "sang phải" : "sang trái") + ".");
            ResetButton(hideGlobalLoading: true);
            yield break;
        }

        var portal = JsonUtility.FromJson<PortalData>(portalReq.downloadHandler.text);

        // 2. Xác nhận travel với server
        var travelPayload = new TravelPayload
        {
            portal_id      = portal.portal_id,
            player_id      = PlayerPrefs.GetInt("USER_ID"),
            current_map_id = currentMapId,
            // Vị trí player thực tế — lấy từ local player object
            player_x       = GetPlayerX(),
            player_y       = GetPlayerY()
        };

        string travelJson = JsonUtility.ToJson(travelPayload);
        using var travelReq = new UnityWebRequest($"{apiBase}/api/map/travel", "POST");
        travelReq.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(travelJson));
        travelReq.downloadHandler = new DownloadHandlerBuffer();
        travelReq.SetRequestHeader("Content-Type", "application/json");
        travelReq.SetRequestHeader("Authorization", $"Bearer {AuthHelper.GetToken()}");
        yield return travelReq.SendWebRequest();

        if (travelReq.result != UnityWebRequest.Result.Success)
        {
            string errBody = travelReq.downloadHandler.text;
            { /* Lỗi: Travel từ chối: {errBody} */ }
            // Thử parse message lỗi từ server JSON {"message":"..."}
            string displayErr = "Không thể chuyển map.";
            try
            {
                var errResp = JsonUtility.FromJson<ErrorResponse>(errBody);
                if (!string.IsNullOrEmpty(errResp?.message))
                    displayErr = errResp.message;
            }
            catch { /* giữ displayErr mặc định */ }
            ShowError(displayErr);
            ResetButton(hideGlobalLoading: true);
            yield break;
        }

        var resp = JsonUtility.FromJson<TravelResponse>(travelReq.downloadHandler.text);
        Vector2 arrivalPos = new Vector2(resp.dest_x, resp.dest_y);
        yield return StartCoroutine(ResolveDirectionalArrivalPosition(resp.dest_map_id, direction, arrivalPos, resolved => arrivalPos = resolved));

        // 3. Gửi yêu cầu chuyển map theo kiến trúc 1-port
        var transitionController = FindAnyObjectByType<ZoneTransitionController>();
        if (transitionController == null)
        {
            { /* Lỗi: Không tìm thấy ZoneTransitionController trong scene */ }
            ShowError("Không thể chuyển map lúc này.");
            ResetButton(hideGlobalLoading: true);
            yield break;
        }

        ClientSceneController.MarkTransferRequestStarted();
        transitionController.RequestMapPortalTransferServerRpc(
            resp.dest_map_id,
            preferredZoneId,
            arrivalPos.x,
            arrivalPos.y);

        ResetButton(hideGlobalLoading: false);
    }

    private IEnumerator ResolveDirectionalArrivalPosition(int targetMapId, string travelDirection, Vector2 fallbackPos, System.Action<Vector2> onResolved)
    {
        string oppositeDirection = travelDirection == "left" ? "right" : "left";
        string url = $"{apiBase}/api/map/portal/direction?mapId={targetMapId}&direction={oppositeDirection}";

        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var portal = JsonUtility.FromJson<PortalData>(req.downloadHandler.text);
            Vector2 resolved = new Vector2(portal.src_x, portal.src_y);
            { /* Arrival resolved from target {oppositeDirection} portal → map={targetMapId} pos=({resolved.x},{resolved.y}) */ }
            onResolved?.Invoke(resolved);
        }
        else
        {
            { /* Cảnh báo: Không resolve được portal '{oppositeDirection}' của map {targetMapId}. Fallback về dest_x/dest_y từ travel response. HTTP={req.responseCode} err={req.error} */ }
            onResolved?.Invoke(fallbackPos);
        }
    }

    private void ResetButton(bool hideGlobalLoading)
    {
        button.interactable = true;
        if (hideGlobalLoading)
        {
            LoginLoadingManager.HideLoadingStatic();
        }

        if (loadingPanel) loadingPanel.SetActive(false);
    }

    private void ShowError(string message)
    {
        if (errorText == null) { { /* Cảnh báo: {message} */ } }
        else
        {
            StopAllCoroutines();
            errorText.text = message;
            errorText.gameObject.SetActive(true);
            StartCoroutine(HideErrorAfterDelay());
        }
        // Hiển thị thêm thông báo nổi bật qua GlobalNotificationUI
        GlobalNotificationUI.Show(message, "Không thể vào khu vực này", 3f);
    }

    private IEnumerator ResolveMapIdByActiveScene(int fallbackMapId, System.Action<int> onResolved)
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string url = $"{apiBase}/api/map/by-scene?scene={UnityWebRequest.EscapeURL(sceneName)}";

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", $"Bearer {AuthHelper.GetToken()}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var map = JsonUtility.FromJson<MapConfigData>(req.downloadHandler.text);
            if (map != null && map.map_id >= 0)
            {
                if (map.map_id != fallbackMapId)
                    { /* Resolve mapId by scene '{sceneName}': {fallbackMapId} -> {map.map_id} */ }
                onResolved?.Invoke(map.map_id);
                yield break;
            }
        }

        { /* Cảnh báo: Không resolve được mapId theo scene '{sceneName}', dùng fallback mapId={fallbackMapId}. HTTP={req.responseCode} err={req.error} */ }
        onResolved?.Invoke(fallbackMapId);
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(errorDisplayTime);
        if (errorText) errorText.gameObject.SetActive(false);
    }

    [System.Serializable] private class ErrorResponse { public string message; }

    // Lấy vị trí player local — tìm GameObject có tag "Player" + là owner
    private float GetPlayerX()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            if (p.TryGetComponent<NetworkObject>(out var no) && no.IsOwner)
                return p.transform.position.x;
        }
        return 0f;
    }

    private float GetPlayerY()
    {
        var players = GameObject.FindGameObjectsWithTag("Player");
        foreach (var p in players)
        {
            if (p.TryGetComponent<NetworkObject>(out var no) && no.IsOwner)
                return p.transform.position.y;
        }
        return 0f;
    }

    // DTOs

    [System.Serializable]
    private class PortalData
    {
        public int    portal_id;
        public string portal_name;
        public float  src_x;
        public float  src_y;
        public int    dest_map_id;
        public string dest_scene_name;
        public float  dest_x;
        public float  dest_y;
    }

    [System.Serializable]
    private class MapConfigData
    {
        public int map_id;
        public string map_name;
        public string scene_name;
    }

    [System.Serializable]
    private class TravelPayload
    {
        public int   portal_id;
        public int   player_id;
        public int   current_map_id;
        public float player_x;
        public float player_y;
    }

    [System.Serializable]
    private class TravelResponse
    {
        public bool   success;
        public int    dest_map_id;
        public string dest_scene_name;
        public float  dest_x;
        public float  dest_y;
    }
}
