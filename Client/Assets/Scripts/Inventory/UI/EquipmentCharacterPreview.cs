using UnityEngine;

/// <summary>
/// Hiển thị prefab nhân vật idle ở giữa Equipment Panel.
/// Nhân vật chỉ để xem, không điều khiển được.
///
/// Setup:
/// 1. Tạo 1 GameObject "CharPreviewSlot" ở giữa Equipment Panel
/// 2. Gắn script này lên
/// 3. Kéo prefab nhân vật vào characterPrefab
/// 4. (Tuỳ) gắn RenderTexture nếu muốn render riêng layer
/// </summary>
public class EquipmentCharacterPreview : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab nhân vật (sẽ spawn Idle, tắt mọi input)")]
    [SerializeField] private GameObject characterPrefab;

    [Header("Spawn Settings")]
    [Tooltip("Vị trí spawn local (so với transform này)")]
    [SerializeField] private Vector3 localOffset = Vector3.zero;

    [Tooltip("Scale nhân vật preview")]
    [SerializeField] private Vector3 previewScale = Vector3.one;

    [Tooltip("Rotation Y ban đầu (độ)")]
    [SerializeField] private float initialRotationY = 180f;

    [Header("Layer (tuỳ chọn)")]
    [Tooltip("Layer riêng cho preview (VD: UICharacter). -1 = giữ nguyên layer gốc.")]
    [SerializeField] private int overrideLayer = -1;

    private GameObject previewInstance;

    private void OnEnable()
    {
        SpawnPreview();
    }

    private void OnDisable()
    {
        DestroyPreview();
    }

    /// <summary>
    /// Thay đổi prefab nhân vật (khi đổi class hoặc costume).
    /// </summary>
    public void SetCharacterPrefab(GameObject newPrefab)
    {
        characterPrefab = newPrefab;
        DestroyPreview();
        SpawnPreview();
    }

    private void SpawnPreview()
    {
        if (characterPrefab == null || previewInstance != null) return;

        previewInstance = Instantiate(characterPrefab, transform);
        previewInstance.transform.localPosition = localOffset;
        previewInstance.transform.localScale = previewScale;
        previewInstance.transform.localRotation = Quaternion.Euler(0f, initialRotationY, 0f);

        // Tắt tất cả script điều khiển — chỉ giữ Animator
        DisableAllControlScripts(previewInstance);

        // Force Idle
        var animator = previewInstance.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.Play("Idle", 0, 0f);
            animator.speed = 1f;
        }

        // Override layer
        if (overrideLayer >= 0)
            SetLayerRecursive(previewInstance, overrideLayer);
    }

    private void DestroyPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
    }

    /// <summary>
    /// Tắt mọi MonoBehaviour gameplay để nhân vật chỉ hiển thị,
    /// không di chuyển và không nhận input trong khung preview.
    /// Animator không nằm trong danh sách MonoBehaviour nên vẫn hoạt động.
    /// </summary>
    private void DisableAllControlScripts(GameObject root)
    {
        var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in behaviours)
        {
            // Giữ chính script preview; các Behaviour không phải MonoBehaviour
            // như Animator sẽ không bị lấy vào danh sách này.
            if (mb == this) continue;

            mb.enabled = false;
        }

        // Tắt Rigidbody / Rigidbody2D nếu có
        var rb = root.GetComponentInChildren<Rigidbody>(true);
        if (rb != null) { rb.isKinematic = true; rb.velocity = Vector3.zero; }

        var rb2d = root.GetComponentInChildren<Rigidbody2D>(true);
        if (rb2d != null) { rb2d.bodyType = RigidbodyType2D.Kinematic; rb2d.velocity = Vector2.zero; }

        // Tắt Collider
        foreach (var col in root.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
        foreach (var col2d in root.GetComponentsInChildren<Collider2D>(true))
            col2d.enabled = false;
    }

    private void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
