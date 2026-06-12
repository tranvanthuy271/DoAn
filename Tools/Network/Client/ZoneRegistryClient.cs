using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Client-side: fetch địa chỉ (IP:port) của một zone server từ API registry.
// Dùng khi client cần tự tìm địa chỉ zone server (ví dụ: lần đầu login vào game).
// Khi đã trong game, địa chỉ zone đích sẽ được server gửi qua ClientRpc
// (ZoneTransitionManager.BeginZoneTransferClientRpc) — KHÔNG cần gọi class này.
// Không cần gắn vào scene — tạo Instance singleton hoặc gọi trực tiếp static method.
public class ZoneRegistryClient : MonoBehaviour
{
    public static ZoneRegistryClient Instance { get; private set; }

    [Header("API")]
    [SerializeField] private string _apiBaseUrl = "http://localhost:5247/api";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Fetch địa chỉ của spawn zone mặc định sau khi login.
    // Gọi từ MainSceneNetworkInitializer sau khi có JWT và playerData.
    // Tham số mapId: Map ID của player's current map
    // Tham số zoneId: Zone ID của player's current zone
    // Tham số callback: Kết quả: ZoneAddress hoặc null nếu thất bại
    public IEnumerator FetchZoneAddress(int mapId, int zoneId, Action<ZoneAddress> callback)
    {
        string url = $"{_apiBaseUrl.TrimEnd('/')}/zone/address?mapId={mapId}&zoneId={zoneId}";

        using var request = UnityWebRequest.Get(url);
        // Client dùng JWT Bearer thay vì API key
        string jwt = PlayerPrefs.GetString("JWT_TOKEN", "");
        if (!string.IsNullOrEmpty(jwt))
            request.SetRequestHeader("Authorization", $"Bearer {jwt}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            { /* Lỗi: Fetch zone address thất bại */ }
            callback?.Invoke(null);
            yield break;
        }

        ZoneAddress addr = null;
        try
        {
            addr = JsonUtility.FromJson<ZoneAddress>(request.downloadHandler.text);
        }
        catch (Exception ex)
        {
            { /* Lỗi: Parse ZoneAddress thất bại: {ex.Message} */ }
        }

        if (addr == null || string.IsNullOrEmpty(addr.ip))
        {
            { /* Lỗi: Zone server map={mapId} zone={zoneId} không có trong registry */ }
            callback?.Invoke(null);
            yield break;
        }

        { /* ✓ Zone map={mapId} zone={zoneId} → {addr.ip}:{addr.port} */ }
        callback?.Invoke(addr);
    }

    // DTO

    [Serializable]
    public class ZoneAddress
    {
        public string ip;
        public int    port;
        public string sceneName;
        public bool   isOnline;
        public int    currentPlayers;
        public int    maxPlayers;
    }
}
