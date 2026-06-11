using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

// [SUPERSEDED — Kiến trúc nhiều port]
// Class này quản lý việc shutdown NGO cũ và reconnect sang host mới khi đổi zone
// trong kiến trúc mỗi zone = 1 port/process riêng.
// PHIÊN BẢN MỚI dùng kiến trúc 1 port + room_id:
// - Xem ZoneTrigger.cs  — gọi PlayerZoneHandler.RequestZoneChangeServerRpc
// - Xem PlayerZoneHandler.cs — ServerRpc, cập nhật room assignment
// - Xem ZoneRoomManager.cs — server-side, theo dõi client trong từng zone
// - Xem RoomBroadcast.cs  — lọc ClientRpc theo zone
// Class này giữ lại để tham khảo nếu muốn quay lại kiến trúc nhiều port.
// Trong kiến trúc 1 port, class này KHÔNG được dùng.
public class ZoneConnectionManager : MonoBehaviour
{
    public static ZoneConnectionManager Instance { get; private set; }

    [Header("UI thông báo (tuỳ chọn)")]
    [SerializeField] private GameObject zoneErrorPanel;
    [SerializeField] private TMP_Text   zoneErrorText;
    [SerializeField] private GameObject loadingPanel;

    [Header("Cấu hình")]
    [Tooltip("Thời gian tối đa (giây) chờ NGO shutdown trước khi bỏ qua")]
    [SerializeField] private float shutdownTimeout = 2f;

    [Tooltip("Giây tự ẩn thông báo lỗi")]
    [SerializeField] private float errorDisplayTime = 4f;

    private bool isSwitching = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (zoneErrorPanel) zoneErrorPanel.SetActive(false);
        if (loadingPanel)   loadingPanel.SetActive(false);
    }

    //  Public API — gọi từ ZoneTrigger

    // Tự động lấy zone config từ API, shutdown NGO hiện tại (đợi thật sự),
    // sau đó connect sang host mới.
    // Tham số apiBase: Base URL của API, ví dụ "http://localhost:5000"
    // Tham số mapId: Map ID hiện tại
    // Tham số targetZoneIndex: Zone index muốn chuyển đến
    // Tham số spawnX: Tọa độ X player sau khi vào zone mới
    // Tham số spawnY: Tọa độ Y player sau khi vào zone mới
    public void SwitchToZone(string apiBase, int mapId, int targetZoneIndex, float spawnX, float spawnY)
    {
        if (isSwitching) return;
        isSwitching = true;
        StartCoroutine(DoSwitchZone(apiBase, mapId, targetZoneIndex, spawnX, spawnY));
    }

    //  Internal coroutine

    private IEnumerator DoSwitchZone(string apiBase, int mapId, int targetZoneIndex, float spawnX, float spawnY)
    {
        LoginLoadingManager.ShowLoadingStatic("Đang chuyển map...");
        if (loadingPanel) loadingPanel.SetActive(false);
        HideError();

        // 1. Lấy zone config từ API
        string url = $"{apiBase}/api/map/zone?mapId={mapId}&zoneIndex={targetZoneIndex}";
        string authToken = PlayerPrefs.GetString("JWT_TOKEN", "");

        using var req = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(authToken))
            req.SetRequestHeader("Authorization", $"Bearer {authToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            ShowError($"Khu vực {targetZoneIndex} không khả dụng.\n({req.error})");
            FinishSwitch(false);
            yield break;
        }

        ZoneConfigData zoneData;
        try
        {
            zoneData = JsonUtility.FromJson<ZoneConfigData>(req.downloadHandler.text);
        }
        catch
        {
            ShowError($"Dữ liệu zone {targetZoneIndex} bị lỗi.");
            FinishSwitch(false);
            yield break;
        }

        if (string.IsNullOrEmpty(zoneData.host_ip) || zoneData.host_port == 0)
        {
            ShowError($"Zone {targetZoneIndex} chưa có server host.");
            FinishSwitch(false);
            yield break;
        }

        // 2. Lưu spawn dest
        PortalArrivalHandler.PendingDestX = spawnX;
        PortalArrivalHandler.PendingDestY = spawnY;
        PortalArrivalHandler.PendingMapId = mapId;

        // ── 3. Shutdown NGO — đợi thật sự thay vì WaitForSeconds cứng ─
        var nm = NetworkManager.Singleton;
        if (nm != null && (nm.IsClient || nm.IsHost || nm.IsServer))
        {
            nm.Shutdown();

            float waited = 0f;
            while (nm.IsListening && waited < shutdownTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            if (nm.IsListening)
                { /* Cảnh báo: NGO chưa shutdown sau timeout, tiếp tục */ }
        }

        // 4. Connect sang host mới
        if (nm != null)
        {
            var transport = nm.GetComponent<UnityTransport>();
            if (transport != null)
                transport.SetConnectionData(zoneData.host_ip, (ushort)zoneData.host_port);

            nm.StartClient();
            { /* Kết nối Zone {targetZoneIndex}  {zoneData.host_ip}:{zoneData.host_port} */ }
        }

        LoginLoadingManager.HideLoadingStatic();
        if (loadingPanel) loadingPanel.SetActive(false);
        isSwitching = false;
    }

    //  UI helpers

    public void ShowError(string message)
    {
        { /* Cảnh báo: {message} */ }
        if (zoneErrorText)  zoneErrorText.text = message;
        if (zoneErrorPanel) zoneErrorPanel.SetActive(true);
        StartCoroutine(AutoHideError());
    }

    private IEnumerator AutoHideError()
    {
        yield return new WaitForSecondsRealtime(errorDisplayTime);
        HideError();
    }

    private void HideError()
    {
        if (zoneErrorPanel) zoneErrorPanel.SetActive(false);
    }

    private void FinishSwitch(bool success)
    {
        LoginLoadingManager.HideLoadingStatic();
        if (loadingPanel) loadingPanel.SetActive(false);
        isSwitching = false;
    }

    //  DTO

    [System.Serializable]
    private class ZoneConfigData
    {
        public int    zone_id;
        public string host_ip;
        public int    host_port;
        public string zone_name;
    }
}
