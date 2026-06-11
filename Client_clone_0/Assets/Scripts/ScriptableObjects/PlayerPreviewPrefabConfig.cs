using System;
using System.Collections.Generic;
using UnityEngine;

// ScriptableObject ánh xạ element_type + gender → prefab nhân vật dùng cho
// Equipment Preview (hiển thị idle animation trong tab Trang Bị).
// CÁCH TẠO ASSET:
// 1. Project window → chuột phải → Create → DoAn → Player Preview Prefab Config
// 2. Đặt tên: PlayerPreviewPrefabConfig
// 3. Lưu vào: Assets/Resources/ScriptableObjects/PlayerPreviewPrefabConfig
// (bắt buộc vào Resources/ để script có thể Resources.Load tự động)
// 4. Thêm các entry cho từng hệ / giới tính:
// elementType = "Fire"  | gender = "Female" | prefab = FireFemale.prefab
// elementType = "Metal" | gender = "Male"   | prefab = MetalMale.prefab
// ...
// 5. (Tuỳ chọn) Gán hybridPrefabMap để hỗ trợ nhân vật Hybrid/Fusion
[CreateAssetMenu(fileName = "PlayerPreviewPrefabConfig",
                 menuName = "DoAn/Player Preview Prefab Config")]
public class PlayerPreviewPrefabConfig : ScriptableObject
{
    public const string DefaultResourcesPath =
        "ScriptableObjects/PlayerPreviewPrefabConfig";

    #region Data structures

    [Serializable]
    public class ElementGenderEntry
    {
        [Tooltip("Giá trị element_type từ DB: Fire / Water / Earth / Wood / Metal / Wind")]
        public string elementType;

        [Tooltip("Giá trị gender từ DB: Male / Female (hoặc He / Nu). Để trống = khớp mọi giới tính.")]
        public string gender;

        [Tooltip("Prefab nhân vật preview — KHÔNG cần NetworkObject. Chỉ cần có Animator.")]
        public GameObject prefab;
    }

    #endregion

    #region Inspector fields

    [Header("Ánh xạ element_type + gender → Prefab preview")]
    [SerializeField] private ElementGenderEntry[] entries = Array.Empty<ElementGenderEntry>();

    [Header("Hybrid / Fusion (tuỳ chọn)")]
    [Tooltip("Nếu không null, nhân vật is_hybrid sẽ dùng HybridPrefabMap để tra cứu.")]
    [SerializeField] private HybridPrefabMap hybridPrefabMap;

    [Header("Fallback")]
    [Tooltip("Prefab dùng khi không tìm được entry phù hợp.")]
    [SerializeField] private GameObject fallbackPrefab;

    #endregion

    #region Runtime cache

    private Dictionary<string, GameObject> _cache; // key = "ElementType|Gender" lowercase

    private void OnEnable()  => RebuildCache();
    private void OnValidate() => RebuildCache();

    private void RebuildCache()
    {
        _cache = new Dictionary<string, GameObject>(
            StringComparer.OrdinalIgnoreCase);

        if (entries == null) return;

        foreach (var e in entries)
        {
            if (e == null || e.prefab == null) continue;
            string key = MakeKey(e.elementType, e.gender);
            if (!_cache.ContainsKey(key))
                _cache[key] = e.prefab;
        }
    }

    private static string MakeKey(string elementType, string gender)
        => $"{elementType ?? ""}|{gender ?? ""}".ToLowerInvariant();

    #endregion

    #region Public API

    // Tra cứu prefab phù hợp dựa trên PlayerDataResponse của local player.
    // Ưu tiên: exact match (element+gender) → element only → fallback.
    // Hybrid: dùng HybridPrefabMap nếu được cấu hình.
    public GameObject Resolve(PlayerDataResponse data)
    {
        if (data == null) return fallbackPrefab;

        return Resolve(data.element_type, data.gender, data.is_hybrid, data.hybrid_prefab_path);
    }

    public GameObject Resolve(string elementType, string gender, bool isHybrid = false, string hybridPrefabPath = null)
    {
        if (isHybrid && hybridPrefabMap != null && !string.IsNullOrEmpty(hybridPrefabPath))
        {
            var hybridPrefab = hybridPrefabMap.GetByPath(hybridPrefabPath);
            if (hybridPrefab != null)
                return hybridPrefab;
        }

        elementType ??= string.Empty;
        gender ??= string.Empty;

        if (_cache == null) RebuildCache();

        if (_cache.TryGetValue(MakeKey(elementType, gender), out var exactMatch) && exactMatch != null)
            return exactMatch;

        if (_cache.TryGetValue(MakeKey(elementType, string.Empty), out var elementOnlyMatch) && elementOnlyMatch != null)
            return elementOnlyMatch;

        if (!string.IsNullOrEmpty(elementType) && entries != null)
        {
            foreach (var entry in entries)
            {
                if (entry == null || entry.prefab == null)
                    continue;

                if (string.Equals(entry.elementType, elementType, StringComparison.OrdinalIgnoreCase))
                    return entry.prefab;
            }
        }

        if (fallbackPrefab != null)
            return fallbackPrefab;

        Debug.LogWarning($"[PlayerPreviewPrefabConfig] Không tìm thấy prefab cho " +
                         $"elementType='{elementType}' gender='{gender}'. Kiểm tra lại config asset.");
        return null;
    }

    // Load singleton từ Resources (không cần kéo tay vào Inspector).
    // Trả về null nếu asset chưa được tạo.
    public static PlayerPreviewPrefabConfig Load()
        => Resources.Load<PlayerPreviewPrefabConfig>(DefaultResourcesPath);

    #endregion
}
