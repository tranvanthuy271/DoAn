using UnityEngine;
using UnityEngine.SceneManagement;

// Tự động tìm và cấu hình tất cả nền nổi (platform) trong scene khi game bắt đầu.
// LOGIC phát hiện platform:
// - Collider2D thuộc layer "Ground"
// - Bề ngang > bề dọc * 1.5 (vật nằm ngang, không phải tường dọc)
// → Chỉ apply cho collider nằm ngang để tường dọc vẫn solid bình thường.
// Kết quả:
// - Player nhảy từ dưới lên xuyên qua.
// - Đứng từ trên xuống được.
// - Đi NGANG qua cũng được (không bị dính cạnh bên).
// CÁCH DÙNG:
// 1. Tạo GameObject rỗng trong scene (ví dụ "MapSetup")
// 2. Add Component → PlatformSetupManager
// 3. Lưu scene
public class PlatformSetupManager : MonoBehaviour
{
    private static PlatformSetupManager instance;

    [Tooltip("Góc solid arc của PlatformEffector2D. 150 = mặt trên solid, bên hông và dưới pass-through.")]
    [SerializeField] private float surfaceArc = 150f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoBootstrap()
    {
        if (instance != null) return;

        var existing = FindFirstObjectByType<PlatformSetupManager>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject go = new GameObject("PlatformSetupManager [Auto]");
        instance = go.AddComponent<PlatformSetupManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        ConfigurePlatforms();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ConfigurePlatforms();
    }

    private void ConfigurePlatforms()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            { /* Cảnh báo: Không tìm thấy layer 'Ground'. Kiểm tra lại Tags & Layers */ }
            return;
        }

        int count = 0;
        Collider2D[] allCols = FindObjectsOfType<Collider2D>(true);

        foreach (var col in allCols)
        {
            if (col.gameObject.layer != groundLayer) continue;

            Bounds b = col.bounds;
            // Chỉ áp dụng cho collider nằm ngang (để tường dọc vẫn solid)
            bool isHorizontal = b.size.x > b.size.y * 1.5f;

            // Pass 1: collider đã có PlatformEffector2D trong scene
            // → chỉ cần bật usedByEffector và cập nhật thông số
            var existingEffector = col.gameObject.GetComponent<PlatformEffector2D>();
            if (existingEffector != null)
            {
                existingEffector.useOneWay = true;
                existingEffector.useOneWayGrouping = false;
                existingEffector.surfaceArc = surfaceArc;
                existingEffector.sideArc = 0f;
                existingEffector.rotationalOffset = 0f;
                col.usedByEffector = true;
                count++;
                continue;
            }

            // Pass 2: collider chưa có PlatformEffector2D → chỉ thêm mới khi nằm ngang
            if (!isHorizontal) continue;

            var newEffector = col.gameObject.AddComponent<PlatformEffector2D>();
            newEffector.useOneWay = true;
            newEffector.useOneWayGrouping = false;
            newEffector.surfaceArc = surfaceArc;
            newEffector.sideArc = 0f;
            newEffector.rotationalOffset = 0f;
            col.usedByEffector = true;
            count++;
        }

        // Đảm bảo physics engine nhận biết tất cả thay đổi effector ngay lập tức
        Physics2D.SyncTransforms();

        { /* Đã cấu hình {count} platform nổi (one-way) */ }
    }
}
