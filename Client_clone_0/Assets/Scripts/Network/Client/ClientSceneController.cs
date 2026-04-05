using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Client-side: nhận lệnh teleport từ server và chuyển scene mà KHÔNG reconnect.
/// Giống LangLa: server gọi zone.removeChar() + addChar() — client chỉ cần reload scene.
///
/// Gắn vào: "ClientBootstrap" GameObject, DontDestroyOnLoad.
/// KHÔNG cần NetworkBehaviour — là MonoBehaviour thuần.
/// </summary>
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

    // ─────────────────────────────────────────────────────────────────────────
    // Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

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

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi từ ZoneTransitionController.TeleportToZoneClientRpc().
    /// Thực hiện: show loading → load scene (nếu khác) → reposition player → hide loading.
    /// KHÔNG shutdown NetworkManager, KHÔNG reconnect.
    /// </summary>
    public void HandleZoneTeleport(string sceneName, float x, float y, int mapId, int zoneId)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("[ClientSceneController] Đang trong quá trình chuyển zone, bỏ qua yêu cầu mới.");
            return;
        }
        StartCoroutine(LoadSceneAndReposition(sceneName, new Vector3(x, y, 0), mapId, zoneId));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core coroutine
    // ─────────────────────────────────────────────────────────────────────────

    private IEnumerator LoadSceneAndReposition(string sceneName, Vector3 targetPos, int mapId, int zoneId)
    {
        _isTransitioning = true;

        // 1 — Hiển thị loading screen
        ShowLoadingScreen();

        // 2 — Chờ 1 frame để UI render
        yield return null;

        Scene oldScene = SceneManager.GetActiveScene();
        string oldSceneName = oldScene.name;

        // 3 — Load scene mới theo kiểu ADDITIVE
        //     Giữ scene cũ nguyên vẹn để NetworkObjects (player, NPC, enemy)
        //     không bị Unity destroy. Sau đó move + unload.
        if (!string.IsNullOrEmpty(sceneName) && oldSceneName != sceneName)
        {
            Debug.Log($"[ClientSceneController] Loading scene (additive): {sceneName}");
            var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!loadOp.isDone)
                yield return null;

            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (!newScene.IsValid())
            {
                Debug.LogError($"[ClientSceneController] Scene '{sceneName}' không hợp lệ sau khi load!");
                HideLoadingScreen();
                _isTransitioning = false;
                yield break;
            }

            // 3a — Di chuyển tất cả root NetworkObjects từ scene cũ sang scene mới
            //       để chúng sống sót khi unload scene cũ.
            int moved = 0;
            foreach (var rootObj in oldScene.GetRootGameObjects())
            {
                if (rootObj == null) continue;
                if (rootObj.GetComponent<NetworkObject>() != null)
                {
                    SceneManager.MoveGameObjectToScene(rootObj, newScene);
                    moved++;
                }
            }
            Debug.Log($"[ClientSceneController] Moved {moved} NetworkObject(s) → {sceneName}");

            // 3b — Đặt scene mới làm active
            SceneManager.SetActiveScene(newScene);

            // 3c — Unload scene cũ (NetworkObjects đã chuyển sang scene mới, an toàn)
            var unloadOp = SceneManager.UnloadSceneAsync(oldScene);
            if (unloadOp != null)
                while (!unloadOp.isDone)
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
        // Chờ vài frame cho scene ổn định
        for (int i = 0; i < 5; i++) yield return null;

        NetworkObject playerNetObj = null;

        for (int attempt = 0; attempt < 60; attempt++)
        {
            // Cách 1: LocalClient.PlayerObject — đây là player NetworkObject được đăng ký chính thức
            try { playerNetObj = NetworkManager.Singleton?.LocalClient?.PlayerObject; }
            catch { /* ignore */ }
            if (playerNetObj != null) break;

            // Cách 2: SpawnedObjectsList — bắt buộc phải có NetworkPlayerController
            // (tránh nhầm NetworkInventory hay NetworkPlayerDataSync cũng là IsOwner)
            try
            {
                var spawnedList = NetworkManager.Singleton?.SpawnManager?.SpawnedObjectsList;
                if (spawnedList != null)
                {
                    foreach (var so in spawnedList)
                    {
                        if (so != null && so.IsOwner && so.GetComponent<NetworkPlayerController>() != null)
                        {
                            playerNetObj = so;
                            break;
                        }
                    }
                }
            }
            catch { /* ignore */ }
            if (playerNetObj != null) break;

            // Cách 3: FindObjectsByType — ưu tiên NetworkPlayerController
            foreach (var ctrl in FindObjectsByType<NetworkPlayerController>(FindObjectsSortMode.None))
            {
                if (ctrl != null && ctrl.IsOwner)
                {
                    playerNetObj = ctrl.GetComponent<NetworkObject>();
                    if (playerNetObj != null) break;
                }
            }
            if (playerNetObj != null) break;

            yield return null;
        }

        if (playerNetObj != null)
        {
            playerNetObj.transform.position = pos;

            // Cập nhật camera — refresh bounds trước rồi mới snap để bounds mới được dùng
            var cam = FindAnyObjectByType<CameraFollow>();
            cam?.RefreshMaxMapBounds();
            cam?.SetTarget(playerNetObj.transform, true);

            Debug.Log($"[ClientSceneController] Player repositioned → {pos}");
        }
        else
        {
            Debug.LogWarning("[ClientSceneController] Không tìm thấy local player NetworkObject sau 60 frames.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Loading Screen
    // ─────────────────────────────────────────────────────────────────────────

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
