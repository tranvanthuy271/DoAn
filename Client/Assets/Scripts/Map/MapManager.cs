using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Map Manager - Quản lý thông tin map hiện tại.
/// Khi scene load, tự động gọi GET /api/map/by-scene?scene=... để lấy mapId + mapName.
/// Nếu API thất bại, fallback về giá trị mapId được set trong Inspector.
/// </summary>
public class MapManager : MonoBehaviour
{
    [Header("Map Configuration")]
    [Tooltip("Map ID fallback nếu API không trả về (0 = Main map, 1, 2, 3...)")]
    public int mapId = 0;

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
        if (string.IsNullOrWhiteSpace(apiBase)) apiBase = ServerAddressConfig.Instance.ApiRoot;
        // Load map info cho scene khởi đầu
        StartCoroutine(FetchMapConfigByScene(SceneManager.GetActiveScene().name));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(FetchMapConfigByScene(scene.name));
    }

    private IEnumerator FetchMapConfigByScene(string sceneName)
    {
        string url = $"{apiBase}/api/map/by-scene?scene={UnityWebRequest.EscapeURL(sceneName)}";
        using var req = UnityWebRequest.Get(url);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<MapConfigResponse>(req.downloadHandler.text);
            mapId   = resp.map_id;
            mapName = resp.map_name;
            Debug.Log($"[MapManager] Map loaded via API: {mapName} (id={mapId})");
        }
        else
        {
            // Fallback: nếu scene name là số, dùng làm mapId
            if (int.TryParse(sceneName, out int parsed))
                mapId = parsed;
            Debug.LogWarning($"[MapManager] API thất bại cho scene '{sceneName}', dùng mapId={mapId}");
        }
    }

    public int    GetMapId()   => mapId;
    public string GetMapName() => mapName;

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
