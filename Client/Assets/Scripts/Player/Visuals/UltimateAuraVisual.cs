using UnityEngine;

// Quản lý aura "Gene Tối Thượng" hiển thị phía sau lưng nhân vật.
// Được điều khiển bởi NetworkPlayerDataSync qua Apply
// nên aura xuất hiện đồng bộ trên mọi client (host + remote).
// Cách hoạt động:
// - Khi kích hoạt, lấy aura prefab theo HỆ từ UltimateAuraDatabase
// (fallback Resources path nếu database không có entry) và gắn làm con của player.
// - Aura được đặt lệch về phía sau (localPosition = backOffset) và sorting order
// thấp hơn sprite nhân vật để nằm sau lưng.
// - Khi tắt, huỷ instance aura.
[DisallowMultipleComponent]
public class UltimateAuraVisual : MonoBehaviour
{
    private const string AuraDatabaseResourcePath = "ScriptableObjects/UltimateAuraDatabase";

    [Header("Aura theo hệ (ScriptableObject)")]
    [Tooltip("Database map mỗi hệ Fusion → 1 prefab aura riêng. Ưu tiên dùng cái này.")]
    [SerializeField] private UltimateAuraDatabase auraDatabase;

    [Header("Fallback")]
    [Tooltip("Đường dẫn Resources dùng khi database không có entry cho hệ và server không gửi path.")]
    [SerializeField] private string defaultAuraResourcePath = "Prefabs/Player/Aura/UltimateAura";

    [Tooltip("Vị trí cục bộ của aura so với nhân vật (đẩy ra sau lưng).")]
    [SerializeField] private Vector3 backOffset = new Vector3(0f, 0f, 0f);

    [Tooltip("Sorting order của aura = sorting order nhân vật + offset (âm = phía sau).")]
    [SerializeField] private int sortingOrderOffset = -1;

    private GameObject auraInstance;
    private string currentKey;
    private string currentPath;
    private bool currentActive;

    // Bật/tắt aura. Gọi mỗi khi NetworkVariable Ultimate (hoặc hệ) thay đổi.
    // Tham số isUltimate: True để hiện aura, false để ẩn.
    // Tham số elementKey: Hệ của nhân vật (Fire/Earth/...) — key tra trong auraDatabase.
    // Tham số auraResourcePathOverride: Resources path do server gửi (fallback khi database không có entry).
    public void Apply(bool isUltimate, string elementKey, string auraResourcePathOverride = null)
    {
        // Không có thay đổi → bỏ qua
        if (isUltimate == currentActive && elementKey == currentKey &&
            auraResourcePathOverride == currentPath && (auraInstance != null || !isUltimate))
            return;

        currentActive = isUltimate;
        currentKey = elementKey;
        currentPath = auraResourcePathOverride;

        if (!isUltimate)
        {
            DestroyAura();
            return;
        }

        SpawnAura(elementKey, auraResourcePathOverride);
    }

    private void SpawnAura(string elementKey, string auraResourcePathOverride)
    {
        if (auraInstance != null)
            DestroyAura();

        GameObject prefab = null;

        // 1) Ưu tiên database ScriptableObject: mỗi hệ Fusion 1 aura riêng.
        if (auraDatabase == null)
            auraDatabase = Resources.Load<UltimateAuraDatabase>(AuraDatabaseResourcePath);

        if (auraDatabase != null)
            prefab = auraDatabase.GetAura(elementKey);

        // 2) Fallback: load theo Resources path (server gửi hoặc default).
        if (prefab == null)
        {
            string path = string.IsNullOrEmpty(auraResourcePathOverride) ? defaultAuraResourcePath : auraResourcePathOverride;
            prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                { /* Cảnh báo: Không có aura cho hệ '{elementKey}' trong database và không load được Resources/'{path}' */ }
                return;
            }
        }

        auraInstance = Instantiate(prefab, transform);
        auraInstance.transform.localPosition = backOffset;
        auraInstance.transform.localRotation = Quaternion.identity;
        auraInstance.name = "UltimateAura";

        ApplySortingBehind();
    }

    // Đặt sorting order của aura thấp hơn sprite nhân vật để nằm sau lưng.
    private void ApplySortingBehind()
    {
        if (auraInstance == null)
            return;

        SpriteRenderer playerSprite = GetComponentInChildren<SpriteRenderer>();
        if (playerSprite == null)
            return;

        int baseOrder = playerSprite.sortingOrder;
        int layerId = playerSprite.sortingLayerID;

        foreach (var sr in auraInstance.GetComponentsInChildren<SpriteRenderer>(true))
        {
            sr.sortingLayerID = layerId;
            sr.sortingOrder = baseOrder + sortingOrderOffset;
        }

        foreach (var ps in auraInstance.GetComponentsInChildren<ParticleSystemRenderer>(true))
        {
            ps.sortingLayerID = layerId;
            ps.sortingOrder = baseOrder + sortingOrderOffset;
        }
    }

    private void DestroyAura()
    {
        if (auraInstance != null)
        {
            Destroy(auraInstance);
            auraInstance = null;
        }
    }

    private void OnDestroy()
    {
        DestroyAura();
    }
}
