using UnityEngine;

/// <summary>
/// Tự động tìm và cấu hình tất cả nền nổi (platform) trong scene khi game bắt đầu.
///
/// LOGIC phát hiện platform:
///   - Collider2D thuộc layer "Ground"
///   - Vị trí center trong world > platformMinY (mặc định 1.0)
///   - Bề ngang > bề dọc * 2 (vật nằm ngang, không phải tường dọc)
///
/// Kết quả: player nhảy từ dưới lên xuyên qua, đứng từ trên xuống được,
///          đi ngang qua cũng được (không bị chặn bởi cạnh bên).
///
/// CÁCH DÙNG:
/// 1. Tạo GameObject rỗng trong scene (ví dụ "MapSetup")
/// 2. Add Component → PlatformSetupManager
/// 3. Lưu scene
/// </summary>
public class PlatformSetupManager : MonoBehaviour
{
    [Tooltip("Chỉ coi là platform nếu center của collider cao hơn giá trị này (world Y)")]
    [SerializeField] private float platformMinY = 1.0f;

    [Tooltip("Góc solid arc của PlatformEffector2D. 150 = bên hông pass-through, " +
             "mặt trên solid, mặt dưới pass-through. " +
             "Giá trị nhỏ hơn → dễ đi ngang qua platform hơn.")]
    [SerializeField] private float surfaceArc = 150f;

    private void Start()
    {
        int groundLayer = LayerMask.NameToLayer("Ground");
        if (groundLayer < 0)
        {
            Debug.LogWarning("[PlatformSetupManager] Không tìm thấy layer 'Ground'. Kiểm tra lại Tags & Layers.");
            return;
        }

        int count = 0;
        Collider2D[] allCols = FindObjectsOfType<Collider2D>(true);

        foreach (var col in allCols)
        {
            if (col.gameObject.layer != groundLayer) continue;

            Bounds b = col.bounds;
            bool isThinHorizontal = b.size.x > b.size.y * 2f;

            // Pass 1: collider đã có PlatformEffector2D trong scene (dù ở độ cao nào)
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

            // Pass 2: collider chưa có PlatformEffector2D
            // → chỉ thêm mới khi đủ điều kiện: đủ cao + nằm ngang
            if (b.center.y <= platformMinY) continue;
            if (!isThinHorizontal) continue;

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

        Debug.Log($"[PlatformSetupManager] Đã cấu hình {count} platform nổi (one-way).");
    }
}
