using UnityEngine;

// Gắn script này vào GameObject có BoxCollider2D để biến nó thành
// "nền một chiều": nhảy xuyên qua từ dưới lên, đứng được từ trên xuống,
// đi ngang qua cũng được (không bị dính cạnh bên).
// CÁCH DÙNG:
// 1. Chọn GameObject có BoxCollider2D (nền nổi, platform, ...)
// 2. Add Component → OneWayPlatform
// 3. Lưu scene/prefab
// Script tự động thêm PlatformEffector2D với cài đặt phù hợp.
[RequireComponent(typeof(Collider2D))]
public class OneWayPlatform : MonoBehaviour
{
    [Tooltip("Góc bên của platform vẫn chặn player (0 = chỉ mặt trên là solid, bên hông pass-through)")]
    [Range(0f, 90f)]
    [SerializeField] private float sideArc = 0f;

    private void Awake()
    {
        SetupEffector();
    }

#if UNITY_EDITOR
    private void Reset()
    {
        SetupEffector();
    }

    private void OnValidate()
    {
        var effector = GetComponent<PlatformEffector2D>();
        if (effector != null)
            effector.sideArc = sideArc;
    }
#endif

    private void SetupEffector()
    {
        // Thêm PlatformEffector2D nếu chưa có
        var effector = GetComponent<PlatformEffector2D>();
        if (effector == null)
            effector = gameObject.AddComponent<PlatformEffector2D>();

        effector.useOneWay = true;
        effector.useOneWayGrouping = false;
        // 150: half-arc = 75° → contact nằm ngang (90°) nằm NGOÀI vùng solid 15°
        // → player đi ngang xuyên qua được, nhảy từ dưới lên xuyên, đứng trên thì solid
        effector.surfaceArc = 150f;
        effector.sideArc = sideArc;   // 0 = hai bên pass-through
        effector.rotationalOffset = 0f;

        // Bật usedByEffector trên tất cả collider của GameObject này
        foreach (var col in GetComponents<Collider2D>())
        {
            col.usedByEffector = true;
        }
    }
}
