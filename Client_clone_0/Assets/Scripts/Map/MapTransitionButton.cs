using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Gắn vào Button UI "← Trái" hoặc "Phải →" để chuyển map qua portal.
///
/// Setup trong Inspector:
///   - isRightButton: true = nút phải (map tiếp), false = nút trái (map trước)
///   - currentMapId: mapId của scene hiện tại
///   - button: kéo Button component vào
///   - loadingPanel (tuỳ chọn): panel loading hiện khi đang chuyển
/// </summary>
public class MapTransitionButton : MonoBehaviour
{
    [Header("Loại nút")]
    [Tooltip("true = nút bên phải (đi tới), false = nút bên trái (quay lại)")]
    [SerializeField] private bool isRightButton = true;

    [Tooltip("Map ID của scene hiện tại (tự lấy từ MapManager nếu để 0)")]
    [SerializeField] private int currentMapId = 0;

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
        if (string.IsNullOrWhiteSpace(apiBase)) apiBase = ServerAddressConfig.Instance.ApiRoot;

        if (currentMapId == 0 && MapManager.Instance != null)
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
        if (loadingPanel) loadingPanel.SetActive(true);

        // 1. Lấy portal trái hoặc phải của map hiện tại
        string direction = isRightButton ? "right" : "left";
        string url = $"{apiBase}/api/map/portal/direction?mapId={currentMapId}&direction={direction}";

        using var portalReq = UnityWebRequest.Get(url);
        yield return portalReq.SendWebRequest();

        if (portalReq.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[MapTransitionButton] Không tìm được portal: {portalReq.error}");
            ShowError("Không có đường đi " + (isRightButton ? "sang phải" : "sang trái") + ".");
            ResetButton();
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
        travelReq.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return travelReq.SendWebRequest();

        if (travelReq.result != UnityWebRequest.Result.Success)
        {
            string errBody = travelReq.downloadHandler.text;
            Debug.LogError($"[MapTransitionButton] Travel từ chối: {errBody}");
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
            ResetButton();
            yield break;
        }

        var resp = JsonUtility.FromJson<TravelResponse>(travelReq.downloadHandler.text);

        // 3. Gửi yêu cầu chuyển map theo kiến trúc 1-port
        var transitionController = FindAnyObjectByType<ZoneTransitionController>();
        if (transitionController == null)
        {
            Debug.LogError("[MapTransitionButton] Không tìm thấy ZoneTransitionController trong scene.");
            ShowError("Không thể chuyển map lúc này.");
            ResetButton();
            yield break;
        }

        transitionController.RequestMapPortalTransferServerRpc(
            resp.dest_map_id,
            preferredZoneId,
            resp.dest_x,
            resp.dest_y);

        ResetButton();
    }

    private void ResetButton()
    {
        button.interactable = true;
        if (loadingPanel) loadingPanel.SetActive(false);
    }

    private void ShowError(string message)
    {
        if (errorText == null) { Debug.LogWarning($"[MapTransitionButton] {message}"); return; }
        StopAllCoroutines();
        errorText.text = message;
        errorText.gameObject.SetActive(true);
        StartCoroutine(HideErrorAfterDelay());
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

    // ── DTOs ─────────────────────────────────────────────────

    [System.Serializable]
    private class PortalData
    {
        public int    portal_id;
        public string portal_name;
        public float  src_x;
        public int    dest_map_id;
        public string dest_scene_name;
        public float  dest_x;
        public float  dest_y;
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
