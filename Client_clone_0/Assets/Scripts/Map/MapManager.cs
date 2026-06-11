using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System;
using UnityEngine.Networking;

// Map Manager - Quản lý thông tin map hiện tại.
// Khi scene load, tự động gọi GET /api/map/by-scene?scene=... để lấy mapId + mapName.
// Nếu API thất bại, fallback về giá trị mapId được set trong Inspector.
public class MapManager : MonoBehaviour
{
    private const int UnknownMapId = -1;

    [Header("Map Configuration")]
    [Tooltip("Map ID fallback nếu không resolve được từ runtime/API. -1 = chưa biết, 0 = Main map, 1, 2, 3...")]
    public int mapId = UnknownMapId;

    [Tooltip("Tên map fallback")]
    public string mapName = "Main Map";

    [Header("API")]
    [SerializeField] private string apiBase = "";

    private static MapManager instance;
    public static MapManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<MapManager>();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SeedKnownMapId();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        apiBase = ServerAddressConfig.Instance.ResolveApiRoot(apiBase);
        StartCoroutine(ResolveMapConfig(SceneManager.GetActiveScene().name));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SeedKnownMapId();
        StartCoroutine(ResolveMapConfig(scene.name));
    }

    private void SeedKnownMapId()
    {
        if (TryResolveKnownMapId(out int knownMapId))
            mapId = knownMapId;
    }

    private bool TryResolveKnownMapId(out int knownMapId)
    {
        if (ClientSceneController.Instance != null && ClientSceneController.Instance.CurrentMapId >= 0)
        {
            knownMapId = ClientSceneController.Instance.CurrentMapId;
            return true;
        }

        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var playerData = GameManager.Instance.GetPlayerData();
            if (playerData != null && playerData.map_id >= 0)
            {
                knownMapId = playerData.map_id;
                return true;
            }
        }

        int selectedMapId = PlayerPrefs.GetInt("SelectedMapId", UnknownMapId);
        if (selectedMapId >= 0)
        {
            knownMapId = selectedMapId;
            return true;
        }

        knownMapId = UnknownMapId;
        return false;
    }

    private IEnumerator ResolveMapConfig(string sceneName)
    {
        // After a scene transfer, PlayerData.map_id can still be stale for a moment.
        // Resolve by the active scene first so Map00 maps to map_id=99, not the previous map_id=0.
        bool loadedByScene = false;
        yield return StartCoroutine(FetchMapConfigByScene(sceneName, success => loadedByScene = success));
        if (loadedByScene)
            yield break;

        if (TryResolveKnownMapId(out int knownMapId))
        {
            bool loadedById = false;
            yield return StartCoroutine(FetchMapConfigById(knownMapId, success => loadedById = success));
            if (loadedById)
                yield break;

            { /* Cảnh báo: Không load được mapId={knownMapId} từ runtime state cho scene '{sceneName}' */ }
        }
    }

    private IEnumerator FetchMapConfigById(int targetMapId, Action<bool> onCompleted)
    {
        string url = $"{apiBase}/api/map/{targetMapId}/config";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<MapConfigResponse>(req.downloadHandler.text);
            mapId = resp.map_id;
            if (!string.IsNullOrWhiteSpace(resp.map_name))
                mapName = resp.map_name;

            { /* Map loaded via mapId: {mapName} (id={mapId}) */ }
            onCompleted?.Invoke(true);
            yield break;
        }

        { /* Cảnh báo: API thất bại cho mapId={targetMapId}, sẽ thử resolve theo scene. Error: {req.error} */ }
        onCompleted?.Invoke(false);
    }

    private IEnumerator FetchMapConfigByScene(string sceneName, Action<bool> onCompleted = null)
    {
        string url = $"{apiBase}/api/map/by-scene?scene={UnityWebRequest.EscapeURL(sceneName)}";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<MapConfigResponse>(req.downloadHandler.text);
            mapId   = resp.map_id;
            mapName = resp.map_name;
            { /* Map loaded via API: {mapName} (id={mapId}) */ }
            onCompleted?.Invoke(true);
        }
        else
        {
            // Fallback: nếu scene name là số, dùng làm mapId
            if (int.TryParse(sceneName, out int parsed))
                mapId = parsed;
            { /* Cảnh báo: API thất bại cho scene '{sceneName}', dùng mapId={mapId} */ }
            onCompleted?.Invoke(false);
        }
    }

    public int    GetMapId()   => mapId;
    public string GetMapName() => mapName;

    public void ResetRuntimeState()
    {
        mapId = UnknownMapId;
        mapName = "Main Map";
    }

    [System.Serializable]
    private class MapConfigResponse
    {
        public int    map_id;
        public string map_name;
        public string scene_name;
        public int    min_level;
        public int    max_level;
    }
}
