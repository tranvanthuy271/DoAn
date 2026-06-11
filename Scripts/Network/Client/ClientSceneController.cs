using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

// Client-side: nhận lệnh teleport từ server và chuyển scene mà KHÔNG reconnect.
// Giống LangLa: server gọi zone.removeChar() + addChar() — client chỉ cần reload scene.
// Gắn vào: "ClientBootstrap" GameObject, DontDestroyOnLoad.
// KHÔNG cần NetworkBehaviour — là MonoBehaviour thuần.
[DisallowMultipleComponent]
public class ClientSceneController : MonoBehaviour
{
    public static ClientSceneController Instance { get; private set; }

    [Header("UI (optional — assign hoặc để null)")]
    [SerializeField] private GameObject _loadingScreenPrefab;

    private GameObject _loadingScreenInstance;
    private bool       _isTransitioning;

    // Thông tin zone hiện tại của client
    public int CurrentMapId  { get; private set; } = -1;
    public int CurrentZoneId { get; private set; } = -1;

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Gọi từ ZoneTransitionController.TeleportToZoneClientRpc().
    // Thực hiện: show loading → load scene (nếu khác) → reposition player → hide loading.
    // KHÔNG shutdown NetworkManager, KHÔNG reconnect.
    public void HandleZoneTeleport(string sceneName, float x, float y, int mapId, int zoneId)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("[ClientSceneController] Đang trong quá trình chuyển zone, bỏ qua yêu cầu mới.");
            return;
        }
        StartCoroutine(LoadSceneAndReposition(sceneName, new Vector3(x, y, 0), mapId, zoneId));
    }

    // Core coroutine

    private IEnumerator LoadSceneAndReposition(string sceneName, Vector3 targetPos, int mapId, int zoneId)
    {
        _isTransitioning = true;

        // 1 — Hiển thị loading screen
        ShowLoadingScreen();

        // 2 — Chờ 1 frame để UI render
        yield return null;

        // 3 — Chỉ load scene nếu khác scene hiện tại
        string currentScene = SceneManager.GetActiveScene().name;
        if (!string.IsNullOrEmpty(sceneName) && currentScene != sceneName)
        {
            Debug.Log($"[ClientSceneController] Loading scene: {sceneName}");
            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
            asyncOp.allowSceneActivation = false;

            // Chờ load xong (progress >= 0.9 = "gần hoàn thành")
            while (asyncOp.progress < 0.9f)
                yield return null;

            asyncOp.allowSceneActivation = true;

            // Chờ scene activate hoàn toàn
            while (!asyncOp.isDone)
                yield return null;
        }

        // 4 — Cập nhật trạng thái zone
        CurrentMapId  = mapId;
        CurrentZoneId = zoneId;

        // 5 — Reposition local player
        yield return StartCoroutine(RepositionLocalPlayer(targetPos));

        // 6 — Ẩn loading screen
        HideLoadingScreen();
        _isTransitioning = false;

        Debug.Log($"[ClientSceneController] ✓ Zone transfer hoàn thành → map{mapId}_zone{zoneId}");
    }

    private IEnumerator RepositionLocalPlayer(Vector3 pos)
    {
        // Chờ vài frame cho NetworkObjects spawn sau khi scene load
        for (int i = 0; i < 3; i++) yield return null;

        // Tìm NetworkObject của local player (chính là owner)
        ulong localId = NetworkManager.Singleton?.LocalClientId ?? ulong.MaxValue;
        if (localId == ulong.MaxValue) yield break;

        NetworkObject playerNetObj = null;

        // Tìm trong tất cả spawned NetworkObjects
        foreach (var netObj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
        {
            if (netObj.IsOwner && netObj.IsLocalPlayer)
            {
                playerNetObj = netObj;
                break;
            }
        }

        if (playerNetObj != null)
        {
            playerNetObj.transform.position = pos;
            Debug.Log($"[ClientSceneController] Player repositioned → {pos}");
        }
        else
        {
            Debug.LogWarning("[ClientSceneController] Không tìm thấy local player NetworkObject.");
        }
    }

    // Loading Screen

    private void ShowLoadingScreen()
    {
        if (_loadingScreenPrefab != null && _loadingScreenInstance == null)
        {
            _loadingScreenInstance = Instantiate(_loadingScreenPrefab);
            DontDestroyOnLoad(_loadingScreenInstance);
        }
        else if (_loadingScreenInstance != null)
        {
            _loadingScreenInstance.SetActive(true);
        }
    }

    private void HideLoadingScreen()
    {
        if (_loadingScreenInstance != null)
            _loadingScreenInstance.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
