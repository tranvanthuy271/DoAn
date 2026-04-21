using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
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
        EnsureZoneStateFromRuntimeData();
    }

    private void OnEnable()
    {
        GameManager.OnPlayerDataSet += HandlePlayerDataSet;
    }

    private void OnDisable()
    {
        GameManager.OnPlayerDataSet -= HandlePlayerDataSet;
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

        Debug.Log($"[ClientSceneController] HandleZoneTeleport | fromScene={SceneManager.GetActiveScene().name} toScene={sceneName} target=({x:F2}, {y:F2}) map={mapId} zone={zoneId}", this);
        StartCoroutine(LoadSceneAndReposition(sceneName, new Vector3(x, y, 0), mapId, zoneId));
    }

    public bool EnsureZoneStateFromRuntimeData()
    {
        int fallbackMapId = CurrentMapId >= 0 ? CurrentMapId : ResolveFallbackMapId();
        int fallbackZoneId = CurrentZoneId >= 0 ? CurrentZoneId : ResolveFallbackZoneId();

        if (fallbackMapId < 0 && fallbackZoneId < 0)
            return false;

        int oldMapId = CurrentMapId;
        int oldZoneId = CurrentZoneId;
        SetCurrentZoneState(fallbackMapId, fallbackZoneId);
        return oldMapId != CurrentMapId || oldZoneId != CurrentZoneId;
    }

    public void SetCurrentZoneState(int mapId, int zoneId)
    {
        int oldMapId = CurrentMapId;
        int oldZoneId = CurrentZoneId;

        if (mapId >= 0)
            CurrentMapId = mapId;

        if (zoneId >= 0)
            CurrentZoneId = zoneId;

        if (oldMapId != CurrentMapId || oldZoneId != CurrentZoneId)
        {
            Debug.Log($"[ClientSceneController] SetCurrentZoneState | map {oldMapId} -> {CurrentMapId}, zone {oldZoneId} -> {CurrentZoneId}", this);
        }
    }

    public void ResetZoneState()
    {
        Debug.Log($"[ClientSceneController] ResetZoneState | map={CurrentMapId} zone={CurrentZoneId}", this);
        CurrentMapId = -1;
        CurrentZoneId = -1;
        _isTransitioning = false;
        HideLoadingScreen();
    }

    private void HandlePlayerDataSet(PlayerDataResponse data)
    {
        if (data == null)
            return;

        int zoneId = data.zone_id >= 0 ? data.zone_id : ResolveFallbackZoneId();
        SetCurrentZoneState(data.map_id, zoneId);
    }

    private static int ResolveFallbackMapId()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var playerData = GameManager.Instance.GetPlayerData();
            if (playerData != null && playerData.map_id >= 0)
                return playerData.map_id;
        }

        if (MapManager.Instance != null && MapManager.Instance.GetMapId() >= 0)
            return MapManager.Instance.GetMapId();

        int selectedMapId = PlayerPrefs.GetInt("SelectedMapId", -1);
        return selectedMapId >= 0 ? selectedMapId : -1;
    }

    private static int ResolveFallbackZoneId()
    {
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var playerData = GameManager.Instance.GetPlayerData();
            if (playerData != null && playerData.zone_id >= 0)
                return playerData.zone_id;
        }

        int savedZoneId = PlayerPrefs.GetInt("PLAYER_ZONE_ID", -1);
        return savedZoneId >= 0 ? savedZoneId : -1;
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
            // Tắt tạm EventSystem hiện tại để scene mới không bật trùng EventSystem
            // trong lúc additive load.
            SetAllEventSystemsEnabled(false);

            Debug.Log($"[ClientSceneController] Loading scene (additive): {sceneName}");
            var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!loadOp.isDone)
                yield return null;

            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (!newScene.IsValid())
            {
                Debug.LogError($"[ClientSceneController] Scene '{sceneName}' không hợp lệ sau khi load!");
                EnablePreferredEventSystem();
                HideLoadingScreen();
                _isTransitioning = false;
                yield break;
            }

            // 3a — Di chuyển tất cả root NetworkObjects từ scene cũ sang scene mới
            //       để chúng sống sót khi unload scene cũ.
            //       Nếu không move, Unity sẽ destroy scene-local clone của NGO trên client
            //       khi UnloadSceneAsync(oldScene), gây lỗi Invalid Destroy / MissingReference.
            int moved = 0;
            foreach (var rootObj in oldScene.GetRootGameObjects())
            {
                if (rootObj == null) continue;
                var netObj = rootObj.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    SceneManager.MoveGameObjectToScene(rootObj, newScene);
                    moved++;
                    continue;
                }
                // Safety net: bảo vệ Canvas/EventSystem còn sót trong scene cũ
                // (trường hợp GameUIPersist chưa được gán trong Inspector)
                bool isCanvas    = rootObj.GetComponent<Canvas>() != null;
                bool isEventSys  = rootObj.GetComponent<EventSystem>() != null;
                bool hasUIPersist = rootObj.GetComponent<GameUIPersist>() != null;
                if ((isCanvas || isEventSys) && !hasUIPersist)
                {
                    ProtectLegacyUiRoot(rootObj);
                }
            }
            Debug.Log($"[ClientSceneController] Moved {moved} NetworkObject(s) → {sceneName}");

            // 3b — Protect canvas/event system roots của scene mới.
            // Nếu quay lại GameScene, bước này sẽ gắn GameUIPersist lên canvas mới
            // và tự hủy duplicate nếu đã có canvas persistent cùng tên.
            ProtectSceneUiRoots(newScene);

            // 3c — Đặt scene mới làm active
            SceneManager.SetActiveScene(newScene);
            CleanupDuplicateEventSystems();
            EnablePreferredEventSystem();

            // Camera persistent phải refresh bounds theo active scene mới,
            // không dùng scene cũ vừa unload.
            CameraFollow.Instance?.RefreshMaxMapBounds();

            // 3d — Unload scene cũ (NetworkObjects đã chuyển sang scene mới, an toàn)
            var unloadOp = SceneManager.UnloadSceneAsync(oldScene);
            if (unloadOp != null)
                while (!unloadOp.isDone)
                    yield return null;

            CleanupDuplicateEventSystems();
            EnablePreferredEventSystem();
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
            var cam = CameraFollow.Instance ?? FindAnyObjectByType<CameraFollow>();
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

    private static void ProtectLegacyUiRoot(GameObject rootObj)
    {
        if (rootObj == null)
            return;

        EventSystem eventSystem = rootObj.GetComponent<EventSystem>();
        if (eventSystem != null)
        {
            EventSystem existingPersistentEventSystem = FindPreferredEventSystem(eventSystem);
            if (existingPersistentEventSystem != null)
            {
                Debug.LogWarning($"[ClientSceneController] Duplicate EventSystem '{rootObj.name}' detected while transitioning scene — destroying scene-local copy.");
                Destroy(rootObj);
                return;
            }
        }

        Debug.LogWarning($"[ClientSceneController] '{rootObj.name}' là Canvas/EventSystem chưa có GameUIPersist — tự động thêm GameUIPersist để tránh mất UI.");
        rootObj.AddComponent<GameUIPersist>();
    }

    private static void ProtectSceneUiRoots(Scene scene)
    {
        if (!scene.IsValid())
            return;

        foreach (var rootObj in scene.GetRootGameObjects())
        {
            if (rootObj == null)
                continue;

            bool isCanvas = rootObj.GetComponent<Canvas>() != null;
            bool isEventSystem = rootObj.GetComponent<EventSystem>() != null;
            bool hasPersist = rootObj.GetComponent<GameUIPersist>() != null;

            if ((isCanvas || isEventSystem) && !hasPersist)
                ProtectLegacyUiRoot(rootObj);
        }
    }

    private static void CleanupDuplicateEventSystems()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems == null || eventSystems.Length <= 1)
            return;

        EventSystem preferred = FindPreferredEventSystem();
        preferred ??= eventSystems[0];

        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem == null || eventSystem == preferred)
                continue;

            Debug.LogWarning($"[ClientSceneController] Destroying duplicate EventSystem '{eventSystem.gameObject.name}' after scene transition.");
            Destroy(eventSystem.gameObject);
        }
    }

    private static void SetAllEventSystemsEnabled(bool enabled)
    {
        foreach (EventSystem eventSystem in FindObjectsByType<EventSystem>(FindObjectsSortMode.None))
        {
            if (eventSystem == null)
                continue;

            eventSystem.enabled = enabled;
        }
    }

    private static void EnablePreferredEventSystem()
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (eventSystems == null || eventSystems.Length == 0)
            return;

        EventSystem preferred = FindPreferredEventSystem() ?? eventSystems[0];
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem == null)
                continue;

            eventSystem.enabled = eventSystem == preferred;
        }
    }

    private static EventSystem FindPreferredEventSystem(EventSystem exclude = null)
    {
        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem == null || eventSystem == exclude)
                continue;

            if (eventSystem.GetComponent<GameUIPersist>() != null || IsPersistentObject(eventSystem.gameObject))
                return eventSystem;
        }

        return null;
    }

    private static bool IsPersistentObject(GameObject obj)
    {
        return obj != null && obj.scene.buildIndex < 0;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
