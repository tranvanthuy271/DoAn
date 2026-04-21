using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private bool instantFollow = true;
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;

    [Header("Network Settings")]
    [SerializeField] private bool followLocalPlayerOnly = true;

    [Header("Map Bounds")]
    [Tooltip("Bật để camera không di chuyển ra ngoài giới hạn map.")]
    [SerializeField] private bool useBounds = false;
    [Tooltip("Tự động tìm bounds từ các object/collider thuộc layer 'MaxMap'. " +
             "Nếu tắt, dùng min/maxBounds bên dưới.")]
    [SerializeField] private bool autoDetectMaxMap = true;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    // Camera component (luôn gắn cùng GameObject với script này)
    private Camera cam;
    private NetworkManager networkManager;

    // Singleton: chỉ có 1 CameraFollow tồn tại xuyên suốt game
    public static CameraFollow Instance { get; private set; }

    private void Awake()
    {
        cam = GetComponent<Camera>();

        // Singleton + DontDestroyOnLoad: Camera sống sót khi chuyển scene/map
        if (Instance != null && Instance != this)
        {
            var duplicateCamera = GetComponent<Camera>();
            if (duplicateCamera != null)
                duplicateCamera.enabled = false;

            var duplicateListener = GetComponent<AudioListener>();
            if (duplicateListener != null)
                duplicateListener.enabled = false;

            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Refresh bounds + target sau mỗi scene load (camera có thể ở scene cũ khi scene mới load additive)
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(RefreshAfterSceneLoad(scene, mode));
    }

    private System.Collections.IEnumerator RefreshAfterSceneLoad(Scene scene, LoadSceneMode mode)
    {
        // Additive transition sẽ SetActiveScene ở frame kế tiếp.
        yield return null;

        if (autoDetectMaxMap)
            DetectMaxMapBounds();

        if (target == null)
            FindLocalPlayer();

        if (target != null && instantFollow)
            transform.position = GetClampedPosition(target.position + offset);

        Debug.Log($"[CameraFollow] OnSceneLoaded scene={scene.name} mode={mode} | activeScene={SceneManager.GetActiveScene().name} | target={(target != null ? target.name : "null")}");
    }

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        // Tự động phát hiện bounds từ layer MaxMap
        if (autoDetectMaxMap)
        {
            DetectMaxMapBounds();
        }

        // Auto-find player nếu chưa được gán
        if (target == null)
        {
            FindLocalPlayer();
        }
    }

    // ---------------------------------------------------------------------------
    // Phát hiện giới hạn map từ các collider
    // Ưu tiên layer "MaxMap", fallback sang layer "Ground"
    //
    // Khi dùng MaxMap:
    //   – Tính inner edges (mặt trong) của các bức tường biên
    //   – Tường dọc: trái → bounds.max.x, phải → bounds.min.x
    //   – Tường ngang: trên → bounds.min.y
    //   – Nếu KHÔNG có tường dưới → dùng đáy của tường dọc làm minBounds.y
    // ---------------------------------------------------------------------------
    private void DetectMaxMapBounds()
    {
        // Reset trước — tránh bounds cũ của map trước còn hiệu lực khi map mới không có layer
        useBounds = false;

        Scene boundsScene = gameObject.scene.IsValid() && gameObject.scene.name != "DontDestroyOnLoad"
            ? gameObject.scene
            : SceneManager.GetActiveScene();

        int layerId = LayerMask.NameToLayer("MaxMap");
        string layerUsed = "MaxMap";
        if (layerId < 0)
        {
            layerId = LayerMask.NameToLayer("Ground");
            layerUsed = "Ground";
        }
        if (layerId < 0)
        {
            Debug.LogWarning("[CameraFollow] Không tìm thấy layer 'MaxMap' lẫn 'Ground'. " +
                "Camera sẽ không có giới hạn map.");
            return;
        }

        Collider2D[] allCols = FindObjectsOfType<Collider2D>(true);
        Bounds combined = new Bounds();
        bool found = false;
        var mapCols = new System.Collections.Generic.List<Collider2D>();

        foreach (var col in allCols)
        {
            if (col == null || col.gameObject.scene != boundsScene)
                continue;

            if (col.gameObject.layer == layerId)
            {
                mapCols.Add(col);
                if (!found) { combined = col.bounds; found = true; }
                else combined.Encapsulate(col.bounds);
            }
        }

        if (!found)
        {
            if (ShouldWarnMissingBounds(boundsScene.name))
                Debug.LogWarning($"[CameraFollow] Không tìm thấy Collider2D nào trên layer '{layerUsed}' trong scene '{boundsScene.name}'.");
            return;
        }

        useBounds = true;

        if (layerUsed == "MaxMap" && mapCols.Count >= 2)
        {
            Vector2 center = combined.center;
            float innerMinX = float.MinValue;
            float innerMaxX = float.MaxValue;
            float innerMinY = float.MaxValue;   // tường dưới (nếu có)
            float innerMaxY = float.MaxValue;   // tường trên

            foreach (var col in mapCols)
            {
                Bounds b = col.bounds;
                bool isVertical   = b.size.y > b.size.x * 1.5f;
                bool isHorizontal = b.size.x > b.size.y * 1.5f;

                if (isVertical)
                {
                    if (b.center.x < center.x)
                        innerMinX = Mathf.Max(innerMinX, b.max.x);
                    else
                        innerMaxX = Mathf.Min(innerMaxX, b.min.x);
                }
                if (isHorizontal)
                {
                    if (b.center.y > center.y)
                        innerMaxY = Mathf.Min(innerMaxY, b.min.y);
                    else
                        innerMinY = Mathf.Min(innerMinY, b.max.y);
                }
            }

            minBounds.x = (innerMinX > float.MinValue) ? innerMinX : combined.min.x;
            maxBounds.x = (innerMaxX < float.MaxValue) ? innerMaxX : combined.max.x;
            maxBounds.y = (innerMaxY < float.MaxValue) ? innerMaxY : combined.max.y;
            // Nếu có tường dưới thì dùng inner edge, không thì dùng đáy combined
            minBounds.y = (innerMinY < float.MaxValue) ? innerMinY : combined.min.y;
        }
        else
        {
            minBounds = new Vector2(combined.min.x, combined.min.y);
            maxBounds = new Vector2(combined.max.x, combined.max.y);
        }

        Debug.Log($"[CameraFollow] Map bounds ({layerUsed}, scene={boundsScene.name}): min={minBounds}, max={maxBounds}");
    }

    private static bool ShouldWarnMissingBounds(string sceneName)
    {
        return sceneName != "Login"
            && sceneName != "Register"
            && sceneName != "SelectElement";
    }

    // ---------------------------------------------------------------------------
    // Tìm player cục bộ (hỗ trợ cả NetworkPlayerController và PlayerController)
    // ---------------------------------------------------------------------------
    private void FindLocalPlayer()
    {
        // Refresh network manager nếu chưa có (có thể NetworkManager khởi động muộn hơn Camera)
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;

        // Ưu tiên: player network có IsOwner = true (cả player thường lẫn fusion prefab)
        if (networkManager != null && networkManager.IsClient && followLocalPlayerOnly)
        {
            NetworkPlayerController[] netPlayers = FindObjectsOfType<NetworkPlayerController>();
            foreach (var p in netPlayers)
            {
                if (p.IsOwner)
                {
                    target = p.transform;
                    Debug.Log($"[CameraFollow] Theo dõi network player (ClientId: {networkManager.LocalClientId})");
                    return;
                }
            }
        }

        // Fallback: PlayerController đầu tiên tìm thấy (standalone hoặc Fusion prefab không có NetworkPlayerController)
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
            Debug.Log($"[CameraFollow] Theo dõi PlayerController: {playerController.gameObject.name}");
        }
    }

    // ---------------------------------------------------------------------------
    // API công khai để player/fusion gán camera target khi spawn
    // ---------------------------------------------------------------------------

    /// <summary>
    /// Gán target mới cho camera. Gọi từ NetworkPlayerController.OnNetworkSpawn()
    /// hoặc từ bất kỳ PlayerController nào (kể cả Fusion F_Phong, F_Kim, ...).
    /// </summary>
    public void SetTarget(Transform newTarget, bool snapImmediately = false)
    {
        bool wasNull = (target == null);
        target = newTarget;

        // Lần đầu tiên nhận được target: snap camera đến đúng vị trí ngay lập tức
        // tránh hiệu ứng camera "bay" từ xa đến chỗ player
        if ((wasNull || snapImmediately) && newTarget != null)
        {
            transform.position = GetClampedPosition(newTarget.position + offset);
        }

        Debug.Log($"[CameraFollow] Target đã được gán: {newTarget?.name}");
    }

    /// <summary>
    /// Yêu cầu camera quét lại layer MaxMap và cập nhật bounds.
    /// Gọi khi chuyển map hoặc tải scene mới.
    /// </summary>
    public void RefreshMaxMapBounds()
    {
        if (autoDetectMaxMap) DetectMaxMapBounds();
    }

    // ---------------------------------------------------------------------------
    // Update camera mỗi frame
    // ---------------------------------------------------------------------------
    private void LateUpdate()
    {
        // Nếu target bị destroy/null, thử tìm lại
        if (target == null)
        {
            FindLocalPlayer();
            if (target == null) return;
        }

        Vector3 desiredPosition = target.position + offset;

        if (!followX) desiredPosition.x = transform.position.x;
        if (!followY) desiredPosition.y = transform.position.y;

        desiredPosition = GetClampedPosition(desiredPosition);

        if (instantFollow)
        {
            transform.position = desiredPosition;
            return;
        }

        // Smooth follow – dùng Time.deltaTime để tốc độ không phụ thuộc framerate
        // smoothSpeed = 8 → camera bắt kịp player trong ~0.2 giây
        float lerpFactor = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        Vector3 smoothed = Vector3.Lerp(transform.position, desiredPosition, lerpFactor);

        // Giới hạn theo bounds – trừ đi bán kích viewport để cạnh màn hình không nhìn ra ngoài map
        if (useBounds)
        {
            float halfH = 0f, halfW = 0f;
            if (cam != null && cam.orthographic)
            {
                halfH = cam.orthographicSize;
                halfW = halfH * cam.aspect;
            }

            // Nếu map nhỏ hơn viewport: căn giữa; ngược lại: clamp bình thường
            float clampMinX = minBounds.x + halfW;
            float clampMaxX = maxBounds.x - halfW;
            float clampMinY = minBounds.y + halfH;
            float clampMaxY = maxBounds.y - halfH;

            if (clampMinX > clampMaxX)
                smoothed.x = (minBounds.x + maxBounds.x) * 0.5f;
            else
                smoothed.x = Mathf.Clamp(smoothed.x, clampMinX, clampMaxX);

            if (clampMinY > clampMaxY)
                smoothed.y = (minBounds.y + maxBounds.y) * 0.5f;
            else
                smoothed.y = Mathf.Clamp(smoothed.y, clampMinY, clampMaxY);
        }

        transform.position = smoothed;
    }

    private Vector3 GetClampedPosition(Vector3 desiredPosition)
    {
        if (!useBounds)
            return desiredPosition;

        float halfH = 0f;
        float halfW = 0f;
        if (cam != null && cam.orthographic)
        {
            halfH = cam.orthographicSize;
            halfW = halfH * cam.aspect;
        }

        float clampMinX = minBounds.x + halfW;
        float clampMaxX = maxBounds.x - halfW;
        float clampMinY = minBounds.y + halfH;
        float clampMaxY = maxBounds.y - halfH;

        desiredPosition.x = (clampMinX > clampMaxX)
            ? (minBounds.x + maxBounds.x) * 0.5f
            : Mathf.Clamp(desiredPosition.x, clampMinX, clampMaxX);
        desiredPosition.y = (clampMinY > clampMaxY)
            ? (minBounds.y + maxBounds.y) * 0.5f
            : Mathf.Clamp(desiredPosition.y, clampMinY, clampMaxY);

        return desiredPosition;
    }

    // ---------------------------------------------------------------------------
    // Gizmos hiển thị bounds trong Editor
    // ---------------------------------------------------------------------------
    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2f,
            (minBounds.y + maxBounds.y) / 2f,
            0f);
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            1f);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}

