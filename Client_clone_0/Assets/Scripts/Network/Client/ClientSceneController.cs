using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using Unity.Netcode;

// Client-side: nhận lệnh teleport từ server và chuyển scene mà KHÔNG reconnect.
// Giống LangLa: server gọi zone.removeChar() + addChar() — client chỉ cần reload scene.
// Gắn vào: "ClientBootstrap" GameObject, DontDestroyOnLoad.
// KHÔNG cần NetworkBehaviour — là MonoBehaviour thuần.
[DisallowMultipleComponent]
public class ClientSceneController : MonoBehaviour
{
    public static ClientSceneController Instance { get; private set; }
    private const float PostTeleportTriggerGraceSeconds = 0.75f;

    [Header("UI (optional — assign hoặc để null)")]
    [SerializeField] private GameObject _loadingScreenPrefab;

    private bool       _isTransitioning;
    private bool       _hasPendingTransferRequest;
    private float      _lastTransferRequestAt = -999f;
    private float      _triggerGraceUntil;

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

        _hasPendingTransferRequest = false;
        Debug.Log($"[ClientSceneController] HandleZoneTeleport | fromScene={SceneManager.GetActiveScene().name} toScene={sceneName} target=({x:F2}, {y:F2}) map={mapId} zone={zoneId}", this);
        ShowLoadingScreen("Đang chuyển map...");
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
        _hasPendingTransferRequest = false;
        _lastTransferRequestAt = -999f;
        _triggerGraceUntil = 0f;
        HideLoadingScreen();
    }

    public static void MarkTransferRequestStarted()
    {
        if (Instance == null)
            return;

        Instance._hasPendingTransferRequest = true;
        Instance._lastTransferRequestAt = Time.unscaledTime;
    }

    public static void MarkTransferRequestFinished()
    {
        if (Instance == null)
            return;

        Instance._hasPendingTransferRequest = false;
    }

    public static bool ShouldSuppressTransferCooldownFeedback()
    {
        if (Instance == null)
            return false;

        if (Instance._isTransitioning)
            return true;

        return Instance._hasPendingTransferRequest &&
               Time.unscaledTime - Instance._lastTransferRequestAt <= 1.5f;
    }

    public static bool IsTransferTriggerBlocked()
    {
        if (Instance == null)
            return false;

        return Instance._isTransitioning ||
               Instance._hasPendingTransferRequest ||
               Time.unscaledTime < Instance._triggerGraceUntil;
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

    // Core coroutine

    private IEnumerator LoadSceneAndReposition(string sceneName, Vector3 targetPos, int mapId, int zoneId)
    {
        _isTransitioning = true;

        // 1 — Hiển thị loading screen
        ShowLoadingScreen("Đang tải khu vực...");

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

            LogSceneColliderDiagnostics("AfterAdditiveLoad", newScene);

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
            LogSceneColliderDiagnostics("AfterSetActiveScene", newScene);

            // 3d — Unload scene cũ (NetworkObjects đã chuyển sang scene mới, an toàn)
            var unloadOp = SceneManager.UnloadSceneAsync(oldScene);
            if (unloadOp != null)
                while (!unloadOp.isDone)
                    yield return null;

            CleanupDuplicateEventSystems();
            EnablePreferredEventSystem();
            LogSceneColliderDiagnostics("AfterUnloadOldScene", SceneManager.GetActiveScene());
        }

        // 4 — Cập nhật trạng thái zone
        CurrentMapId  = mapId;
        CurrentZoneId = zoneId;

        // 5 — Reposition local player
        ShowLoadingScreen("Đang đồng bộ nhân vật...");
        yield return StartCoroutine(RepositionLocalPlayer(targetPos));
        _triggerGraceUntil = Time.unscaledTime + PostTeleportTriggerGraceSeconds;

        // 6 — Ẩn loading screen
        HideLoadingScreen();
        _hasPendingTransferRequest = false;
        _isTransitioning = false;

        Debug.Log($"[ClientSceneController] ✓ Zone transfer hoàn thành → map{mapId}_zone{zoneId}");
    }

    private IEnumerator RepositionLocalPlayer(Vector3 pos)
    {
        // Chờ vài frame cho scene ổn định
        for (int i = 0; i < 5; i++)
        {
            yield return null;
        }

        NetworkObject playerNetObj = null;

        float deadline = Time.unscaledTime + 6f;
        while (Time.unscaledTime < deadline)
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
            LogPlayerTransitionDiagnostics("BeforeReposition", playerNetObj, pos);
            Vector3 resolvedPos = ResolveSafeTeleportPosition(playerNetObj, pos);
            ApplyTeleportPosition(playerNetObj, resolvedPos);

            // Cập nhật camera — refresh bounds trước rồi mới snap để bounds mới được dùng
            var cam = CameraFollow.Instance ?? FindAnyObjectByType<CameraFollow>();
            cam?.RefreshMaxMapBounds();
            cam?.SetTarget(playerNetObj.transform, true);

            Debug.Log($"[ClientSceneController] Player repositioned → requested={pos} resolved={resolvedPos}");
            LogPlayerTransitionDiagnostics("AfterRepositionImmediate", playerNetObj, resolvedPos);
            StartCoroutine(LogPostRepositionDiagnostics(playerNetObj, resolvedPos));
        }
        else
        {
            Debug.LogWarning("[ClientSceneController] Không tìm thấy local player NetworkObject trong thời gian chờ reposition.");
        }
    }

    // Loading Screen

    private void ShowLoadingScreen(string status = null)
    {
        LoginLoadingManager.ShowLoadingStatic(status);
    }

    private void HideLoadingScreen()
    {
        LoginLoadingManager.HideLoadingStatic();
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

    private IEnumerator LogPostRepositionDiagnostics(NetworkObject playerNetObj, Vector3 expectedPos)
    {
        if (playerNetObj == null)
            yield break;

        yield return null;
        if (playerNetObj != null)
            LogPlayerTransitionDiagnostics("AfterReposition+1Frame", playerNetObj, expectedPos);

        yield return new WaitForFixedUpdate();
        if (playerNetObj != null)
            LogPlayerTransitionDiagnostics("AfterReposition+1FixedUpdate", playerNetObj, expectedPos);

        yield return new WaitForFixedUpdate();
        if (playerNetObj != null)
            LogPlayerTransitionDiagnostics("AfterReposition+2FixedUpdate", playerNetObj, expectedPos);
    }

    private static void ApplyTeleportPosition(NetworkObject playerNetObj, Vector3 targetPos)
    {
        if (playerNetObj == null)
            return;

        Rigidbody2D playerRb = playerNetObj.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.position = targetPos;
            playerRb.WakeUp();
        }

        playerNetObj.transform.position = targetPos;
        Physics2D.SyncTransforms();

        PlayerMovement movement = playerNetObj.GetComponent<PlayerMovement>();
        if (movement != null)
            movement.RefreshGroundCheck();
    }

    private static Vector3 ResolveSafeTeleportPosition(NetworkObject playerNetObj, Vector3 requestedPos)
    {
        if (playerNetObj == null)
            return requestedPos;

        PlayerMovement movement = playerNetObj.GetComponent<PlayerMovement>();
        Collider2D playerCollider = playerNetObj.GetComponent<Collider2D>();
        LayerMask groundMask = movement != null ? movement.GroundLayerMask : LayerMask.GetMask("Ground");
        if (groundMask.value == 0)
            return requestedPos;

        float playerHalfHeight = playerCollider != null ? Mathf.Max(playerCollider.bounds.extents.y, 0.5f) : 0.9f;
        float snapPadding = 0.05f;

        if (TryFindGroundedTeleportPosition(requestedPos, playerHalfHeight, groundMask, out Vector3 groundedPos))
        {
            if ((groundedPos - requestedPos).sqrMagnitude > 0.0001f)
            {
                Debug.LogWarning($"[ClientSceneController] Teleport target {requestedPos} không có ground hợp lệ. Snap sang {groundedPos}.", playerNetObj);
            }

            return groundedPos;
        }

        float[] searchOffsets =
        {
            0f,
            -1f, 1f,
            -2f, 2f,
            -4f, 4f,
            -6f, 6f,
            -8f, 8f,
            -12f, 12f,
            -16f, 16f,
            -20f, 20f,
            -24f, 24f,
            -28f, 28f,
            -32f, 32f,
            -40f, 40f
        };

        bool foundCandidate = false;
        Vector3 bestCandidate = requestedPos;
        float bestScore = float.MaxValue;

        foreach (float offset in searchOffsets)
        {
            Vector3 probeTarget = new Vector3(requestedPos.x + offset, requestedPos.y, requestedPos.z);
            if (!TryFindGroundedTeleportPosition(probeTarget, playerHalfHeight, groundMask, out Vector3 candidate))
                continue;

            float score = Mathf.Abs(offset) * 1000f + Mathf.Abs(candidate.y - requestedPos.y);
            if (score >= bestScore)
                continue;

            bestScore = score;
            bestCandidate = candidate;
            foundCandidate = true;
        }

        if (foundCandidate)
        {
            Debug.LogWarning($"[ClientSceneController] Teleport target {requestedPos} nằm ngoài nền. Dùng ground gần nhất {bestCandidate}.", playerNetObj);
            return new Vector3(bestCandidate.x, bestCandidate.y + snapPadding, bestCandidate.z);
        }

        Debug.LogWarning($"[ClientSceneController] Không tìm thấy ground hợp lệ quanh target teleport {requestedPos}. Giữ nguyên tọa độ gốc.", playerNetObj);
        return requestedPos;
    }

    private static bool TryFindGroundedTeleportPosition(Vector3 targetPos, float playerHalfHeight, LayerMask groundMask, out Vector3 resolvedPos)
    {
        float rayStartHeight = Mathf.Max(playerHalfHeight + 6f, 8f);
        float rayDistance = rayStartHeight + playerHalfHeight + 24f;
        Vector2 rayOrigin = new Vector2(targetPos.x, targetPos.y + rayStartHeight);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, rayDistance, groundMask);

        if (hit.collider == null)
        {
            resolvedPos = targetPos;
            return false;
        }

        float groundedY = hit.point.y + playerHalfHeight + 0.02f;
        resolvedPos = new Vector3(targetPos.x, groundedY, targetPos.z);
        return true;
    }

    private static void LogSceneColliderDiagnostics(string phase, Scene scene)
    {
        if (!scene.IsValid())
        {
            Debug.LogWarning($"[ClientSceneController] {phase} | scene không hợp lệ.");
            return;
        }

        int groundLayerId = LayerMask.NameToLayer("Ground");
        int rootCount = 0;
        int activeRootCount = 0;
        int totalColliders = 0;
        int enabledColliders = 0;
        int activeColliders = 0;
        int groundColliders = 0;
        int enabledGroundColliders = 0;
        int activeGroundColliders = 0;
        int boxColliders = 0;
        int enabledBoxColliders = 0;
        StringBuilder samples = new StringBuilder();
        int sampleCount = 0;

        foreach (GameObject rootObj in scene.GetRootGameObjects())
        {
            if (rootObj == null)
                continue;

            rootCount++;
            if (rootObj.activeInHierarchy)
                activeRootCount++;

            Collider2D[] colliders = rootObj.GetComponentsInChildren<Collider2D>(true);
            foreach (Collider2D collider in colliders)
            {
                if (collider == null)
                    continue;

                totalColliders++;
                if (collider.enabled)
                    enabledColliders++;
                if (collider.gameObject.activeInHierarchy)
                    activeColliders++;
                if (collider is BoxCollider2D)
                {
                    boxColliders++;
                    if (collider.enabled)
                        enabledBoxColliders++;
                }

                bool isGroundCollider = groundLayerId >= 0 && collider.gameObject.layer == groundLayerId;
                if (!isGroundCollider)
                    continue;

                groundColliders++;
                if (collider.enabled)
                    enabledGroundColliders++;
                if (collider.gameObject.activeInHierarchy)
                    activeGroundColliders++;

                if (sampleCount < 6)
                {
                    if (samples.Length > 0)
                        samples.Append(" | ");
                    samples.Append(DescribeCollider(collider));
                    sampleCount++;
                }
            }
        }

        string groundLayerLabel = groundLayerId >= 0 ? $"Ground({groundLayerId})" : "Ground(<missing layer>)";
        string sampleText = sampleCount > 0 ? samples.ToString() : "<không có collider nào ở layer Ground>";
        Debug.Log(
            $"[ClientSceneController] {phase} | scene={scene.name} activeScene={SceneManager.GetActiveScene().name} loadedScenes={DescribeLoadedScenes()} root={activeRootCount}/{rootCount} colliders={enabledColliders}/{totalColliders} activeColliderObjects={activeColliders} groundLayer={groundLayerLabel} groundColliders={enabledGroundColliders}/{groundColliders} activeGroundObjects={activeGroundColliders} boxColliders={enabledBoxColliders}/{boxColliders} physicsGravity={Physics2D.gravity} samples={sampleText}");
    }

    private static void LogPlayerTransitionDiagnostics(string phase, NetworkObject playerNetObj, Vector3 expectedPos)
    {
        if (playerNetObj == null)
        {
            Debug.LogWarning($"[ClientSceneController] {phase} | playerNetObj=null");
            return;
        }

        Transform playerTransform = playerNetObj.transform;
        Rigidbody2D playerRb = playerNetObj.GetComponent<Rigidbody2D>();
        Collider2D playerCollider = playerNetObj.GetComponent<Collider2D>();
        PlayerMovement movement = playerNetObj.GetComponent<PlayerMovement>();

        if (movement != null)
            movement.RefreshGroundCheck();

        Vector3 probePosition = movement?.GroundCheckTransform != null
            ? movement.GroundCheckTransform.position
            : playerTransform.position;
        float probeRadius = movement != null ? movement.GroundCheckRadius : 0.2f;
        LayerMask groundMask = movement != null ? movement.GroundLayerMask : 0;
        Collider2D[] groundHits = groundMask.value != 0
            ? Physics2D.OverlapCircleAll((Vector2)probePosition, probeRadius, groundMask)
            : System.Array.Empty<Collider2D>();
        Collider2D[] anyHits = Physics2D.OverlapCircleAll((Vector2)probePosition, probeRadius);
        RaycastHit2D downHit = Physics2D.Raycast((Vector2)probePosition, Vector2.down, 3f);
        Scene playerScene = playerNetObj.gameObject.scene;
        Scene activeScene = SceneManager.GetActiveScene();
        bool sceneMismatch = playerScene.handle != activeScene.handle;
        bool layerMismatch = anyHits.Length > 0 && groundHits.Length == 0;

        string hint = string.Empty;
        if (sceneMismatch)
            hint += " | hint=player vẫn chưa nằm trong active scene";
        if (layerMismatch)
            hint += " | hint=có collider gần chân nhưng không thuộc Ground Layer";
        if (groundMask.value == 0)
            hint += " | hint=groundLayer trên PlayerMovement đang = Nothing";

        Debug.Log(
            $"[ClientSceneController] {phase} | player={playerNetObj.name} playerScene={playerScene.name} activeScene={activeScene.name} loadedScenes={DescribeLoadedScenes()} pos={FormatVector3(playerTransform.position)} target={FormatVector3(expectedPos)} delta={(playerTransform.position - expectedPos).magnitude:F3} rb={DescribeRigidbody(playerRb)} playerCollider={DescribePlayerCollider(playerCollider)} movement={(movement != null ? $"grounded={movement.IsGrounded()} probe={FormatVector3(probePosition)} radius={probeRadius:F2} groundMask={DescribeLayerMask(groundMask)}" : "<không có PlayerMovement>")} groundHits={DescribeColliders(groundHits)} anyHits={DescribeColliders(anyHits)} downHit={DescribeRaycastHit(downHit)}{hint}",
            playerNetObj);
    }

    private static string DescribeLoadedScenes()
    {
        StringBuilder builder = new StringBuilder();
        Scene activeScene = SceneManager.GetActiveScene();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene loadedScene = SceneManager.GetSceneAt(i);
            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(loadedScene.name);
            if (loadedScene.handle == activeScene.handle)
                builder.Append("*");
        }

        return builder.Length > 0 ? builder.ToString() : "<none>";
    }

    private static string DescribeRigidbody(Rigidbody2D rigidbody)
    {
        if (rigidbody == null)
            return "<none>";

        return $"type={rigidbody.bodyType} simulated={rigidbody.simulated} gravityScale={rigidbody.gravityScale:F2} velocity={FormatVector3(rigidbody.velocity)} position={FormatVector3(rigidbody.position)}";
    }

    private static string DescribePlayerCollider(Collider2D collider)
    {
        if (collider == null)
            return "<none>";

        Bounds bounds = collider.bounds;
        return $"{collider.GetType().Name}(enabled={collider.enabled}, trigger={collider.isTrigger}, layer={DescribeLayer(collider.gameObject.layer)}, min={FormatVector3(bounds.min)}, max={FormatVector3(bounds.max)})";
    }

    private static string DescribeColliders(Collider2D[] colliders)
    {
        if (colliders == null || colliders.Length == 0)
            return "0[]";

        StringBuilder builder = new StringBuilder();
        builder.Append(colliders.Length).Append('[');

        int appended = 0;
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider2D collider = colliders[i];
            if (collider == null)
                continue;

            if (appended > 0)
                builder.Append(" | ");

            builder.Append(DescribeCollider(collider));
            appended++;

            if (appended >= 4)
                break;
        }

        if (colliders.Length > appended)
            builder.Append(" | ...");

        builder.Append(']');
        return builder.ToString();
    }

    private static string DescribeCollider(Collider2D collider)
    {
        if (collider == null)
            return "<null>";

        return $"{collider.name}:{collider.GetType().Name}(layer={DescribeLayer(collider.gameObject.layer)}, enabled={collider.enabled}, trigger={collider.isTrigger}, scene={collider.gameObject.scene.name})";
    }

    private static string DescribeRaycastHit(RaycastHit2D hit)
    {
        if (hit.collider == null)
            return "<none>";

        return $"{DescribeCollider(hit.collider)} point={FormatVector3(hit.point)} distance={hit.distance:F3}";
    }

    private static string DescribeLayerMask(LayerMask mask)
    {
        if (mask.value == 0)
            return "Nothing(0)";

        StringBuilder builder = new StringBuilder();
        builder.Append(mask.value).Append('[');
        bool appended = false;

        for (int layer = 0; layer < 32; layer++)
        {
            if ((mask.value & (1 << layer)) == 0)
                continue;

            if (appended)
                builder.Append(',');

            builder.Append(DescribeLayer(layer));
            appended = true;
        }

        builder.Append(']');
        return builder.ToString();
    }

    private static string DescribeLayer(int layer)
    {
        string layerName = LayerMask.LayerToName(layer);
        return string.IsNullOrEmpty(layerName) ? layer.ToString() : $"{layerName}({layer})";
    }

    private static string FormatVector3(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }

    private static string FormatVector3(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
