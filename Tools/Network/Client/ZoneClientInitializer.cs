using System.Collections;
using UnityEngine;

// Thay thế MainSceneNetworkInitializer trong kiến trúc server+client mới.
// Trách nhiệm:
// 1. Đọc PlayerData đã lưu (GameManager hoặc PlayerPrefs)
// 2. Fetch địa chỉ zone server từ ZoneRegistryClient
// 3. Gọi ZoneConnectionHandler.ConnectToZone()
// Gắn vào: persistent NetworkClient GameObject (DontDestroyOnLoad).
// Chạy khi chuyển từ LoginScene → MainScene.
[DisallowMultipleComponent]
public class ZoneClientInitializer : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private ZoneConnectionHandler _connectionHandler;
    [SerializeField] private ZoneRegistryClient    _registryClient;

    [Header("Fallback — Dev Only")]
    [Tooltip("Nếu API không trả về zone address, dùng IP này (chỉ khi dev/test)")]
    [SerializeField] private string _devFallbackIp = "127.0.0.1";
    [SerializeField] private ushort _devFallbackPort = 7777;
    [SerializeField] private bool   _enableDevFallback = false;

    private void Start()
    {
        // Validate dependencies
        if (_connectionHandler == null)
            _connectionHandler = ZoneConnectionHandler.Instance;
        if (_registryClient == null)
            _registryClient = ZoneRegistryClient.Instance;

        if (_connectionHandler == null || _registryClient == null)
        {
            { /* Lỗi: Thiếu dependency */ }
            return;
        }

        StartCoroutine(InitializeConnection());
    }

    private IEnumerator InitializeConnection()
    {
        // 1 — Đọc JWT
        string jwt = PlayerPrefs.GetString("JWT_TOKEN", "");
        if (string.IsNullOrEmpty(jwt))
        {
            { /* Lỗi: JWT_TOKEN rỗng  chuyển về màn Login */ }
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginScene");
            yield break;
        }

        // 2 — Đọc thông tin map/zone hiện tại của player từ PlayerPrefs hoặc GameManager
        //     (được lưu khi Login → load PlayerData)
        int mapId  = PlayerPrefs.GetInt("PLAYER_MAP_ID",  0);
        int zoneId = PlayerPrefs.GetInt("PLAYER_ZONE_ID", 0);

        { /* Kết nối → map={mapId} zone={zoneId} */ }

        // 3 — Fetch zone server address
        ZoneRegistryClient.ZoneAddress addr = null;
        yield return StartCoroutine(_registryClient.FetchZoneAddress(mapId, zoneId,
            result => addr = result));

        if (addr == null || !addr.isOnline)
        {
            if (_enableDevFallback)
            {
                { /* Cảnh báo: API không trả về zone address */ }
                _connectionHandler.ConnectToZone(_devFallbackIp, _devFallbackPort, jwt);
            }
            else
            {
                { /* Lỗi: Zone server không online */ }
                // TODO: hiện thông báo lỗi cho user
            }
            yield break;
        }

        // 4 — Kết nối
        _connectionHandler.ConnectToZone(addr.ip, (ushort)addr.port, jwt);
    }
}
