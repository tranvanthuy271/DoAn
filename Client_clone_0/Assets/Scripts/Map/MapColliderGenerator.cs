using UnityEngine;
using System.Collections.Generic;

// Tự động tạo EdgeCollider2D từ pixel alpha của 1 sprite PNG.
// Dùng khi terrain là 1 ảnh PNG lớn (không phải nhiều sprite nhỏ).
// YÊU CẦU BẮT BUỘC:
// → Chọn PNG trong Project → Inspector → Advanced → Read/Write Enabled ✅ → Apply
// Nếu không bật, sẽ báo lỗi "Texture ... is not readable"
// CÁCH DÙNG:
// 1. Tạo GameObject → gắn SpriteRenderer + script này
// 2. Kéo sprite vào SpriteRenderer
// 3. Chuột phải Inspector → Generate Collider From Sprite
// 4. EdgeCollider2D sẽ tự trace đường viền địa hình
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class MapColliderGenerator : MonoBehaviour
{
    [Header("Alpha Detection")]
    [Tooltip("Pixel có alpha >= threshold này được coi là solid")]
    [Range(1, 255)]
    [SerializeField] private int alphaThreshold = 10;

    [Tooltip("Bước nhảy cột pixel (giảm để chi tiết hơn, tăng để nhanh hơn)")]
    [Range(1, 8)]
    [SerializeField] private int stepX = 2;

    [Header("Simplification")]
    [Tooltip("Loại bỏ điểm thẳng hàng trong khoảng tolerance này (0 = không simplify)")]
    [Range(0f, 0.5f)]
    [SerializeField] private float simplifyTolerance = 0.05f;

    [Tooltip("Kéo dài 2 đầu edge ra ngoài để không bị lỗ tại viền map")]
    [Range(0f, 2f)]
    [SerializeField] private float edgeExtension = 0.5f;

    [Header("Info (readonly)")]
    [SerializeField] private int generatedPointCount = 0;


    [ContextMenu("Generate Collider From Sprite")]
    public void GenerateCollider()
    {
        var sr  = GetComponent<SpriteRenderer>();
        var col = GetComponent<EdgeCollider2D>();

        if (sr.sprite == null)
        {
            Debug.LogError("[MapColliderGenerator] Không có sprite — gán sprite vào SpriteRenderer trước.");
            return;
        }

        Texture2D tex = sr.sprite.texture;
        if (!tex.isReadable)
        {
            Debug.LogError(
                $"[MapColliderGenerator] Texture '{tex.name}' chưa bật Read/Write.\n" +
                "→ Project Window → chọn PNG → Inspector → Advanced → Read/Write Enabled ✅ → Apply");
            return;
        }

        var points = BuildTopSurfacePoints(sr.sprite, tex);

        if (points.Count < 2)
        {
            Debug.LogWarning("[MapColliderGenerator] Không đủ điểm để tạo EdgeCollider2D. Kiểm tra Alpha Threshold.");
            return;
        }

        if (simplifyTolerance > 0f)
            points = Simplify(points, simplifyTolerance);

        // Kéo dài 2 đầu
        if (edgeExtension > 0f)
        {
            points[0]               = points[0]               + new Vector2(-edgeExtension, 0);
            points[points.Count - 1] = points[points.Count - 1] + new Vector2(edgeExtension, 0);
        }

        col.SetPoints(points);
        generatedPointCount = points.Count;

        Debug.Log($"[MapColliderGenerator] Tạo EdgeCollider2D: {generatedPointCount} điểm.");
    }

    [ContextMenu("Clear Collider")]
    public void ClearCollider()
    {
        GetComponent<EdgeCollider2D>().SetPoints(new List<Vector2>());
        generatedPointCount = 0;
    }

    // Core: duyệt từng cột pixel, tìm pixel solid cao nhất → lấy top surface

    private List<Vector2> BuildTopSurfacePoints(Sprite sprite, Texture2D tex)
    {
        // Vùng pixel thuộc sprite trong texture (nếu texture là atlas)
        Rect rect = sprite.textureRect; // pixel coords

        int startX = Mathf.RoundToInt(rect.x);
        int startY = Mathf.RoundToInt(rect.y);
        int width  = Mathf.RoundToInt(rect.width);
        int height = Mathf.RoundToInt(rect.height);

        float ppu    = sprite.pixelsPerUnit;
        // Pivot của sprite tính từ góc trái-dưới của textureRect (0..1)
        Vector2 pivot = sprite.pivot; // pixel units từ góc trái-dưới của rect

        var points = new List<Vector2>(width / stepX + 2);

        for (int x = 0; x < width; x += stepX)
        {
            int topY = FindTopSolidPixel(tex, startX + x, startY, height);
            if (topY < 0) continue; // cột này không có pixel solid

            // Chuyển pixel → local space (tâm sprite là gốc)
            float localX = (x - pivot.x) / ppu;
            float localY = (topY - pivot.y + 1) / ppu; // +1 để đứng trên đỉnh pixel

            points.Add(new Vector2(localX, localY));
        }

        return points;
    }

    // Trả về: Y tương đối (0 = đáy rect) của pixel solid cao nhất, hoặc -1 nếu không có.
    private int FindTopSolidPixel(Texture2D tex, int texX, int startY, int height)
    {
        // Quét từ trên xuống (Unity: Y tăng lên trên)
        for (int y = height - 1; y >= 0; y--)
        {
            Color c = tex.GetPixel(texX, startY + y);
            if ((int)(c.a * 255) >= alphaThreshold)
                return y;
        }
        return -1;
    }

    // Simplification: xóa điểm gần thẳng hàng (Ramer–Douglas–Peucker lite)

    private static List<Vector2> Simplify(List<Vector2> points, float tolerance)
    {
        if (points.Count <= 2) return points;

        var result = new List<Vector2> { points[0] };
        for (int i = 1; i < points.Count - 1; i++)
        {
            float dist = PerpendicularDistance(points[i], points[i - 1], points[i + 1]);
            if (dist > tolerance)
                result.Add(points[i]);
        }
        result.Add(points[points.Count - 1]);
        return result;
    }

    private static float PerpendicularDistance(Vector2 pt, Vector2 lineStart, Vector2 lineEnd)
    {
        float dx = lineEnd.x - lineStart.x;
        float dy = lineEnd.y - lineStart.y;
        float mag = Mathf.Sqrt(dx * dx + dy * dy);
        if (mag < 1e-6f) return 0f;
        return Mathf.Abs(dy * pt.x - dx * pt.y + lineEnd.x * lineStart.y - lineEnd.y * lineStart.x) / mag;
    }
}
