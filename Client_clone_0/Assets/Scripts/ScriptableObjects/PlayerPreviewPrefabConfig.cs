using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject ánh xạ element_type + gender → prefab nhân vật dùng cho
/// Equipment Preview (hiển thị idle animation trong tab Trang Bị).
///
/// ═══════════════════════════════════════════════════════════════
/// CÁCH TẠO ASSET:
///   1. Project window → chuột phải → Create → DoAn → Player Preview Prefab Config
///   2. Đặt tên: PlayerPreviewPrefabConfig
///   3. Lưu vào: Assets/Resources/ScriptableObjects/PlayerPreviewPrefabConfig
///      (bắt buộc vào Resources/ để script có thể Resources.Load tự động)
///   4. Thêm các entry cho từng hệ / giới tính:
///      elementType = "Fire"  | gender = "Female" | prefab = FireFemale.prefab
///      elementType = "Metal" | gender = "Male"   | prefab = MetalMale.prefab
///      ...
///   5. (Tuỳ chọn) Gán hybridPrefabMap để hỗ trợ nhân vật Hybrid/Fusion
/// ═══════════════════════════════════════════════════════════════
/// </summary>
[CreateAssetMenu(fileName = "PlayerPreviewPrefabConfig",
                 menuName = "DoAn/Player Preview Prefab Config")]
public class PlayerPreviewPrefabConfig : ScriptableObject
{
    public const string DefaultResourcesPath =
        "ScriptableObjects/PlayerPreviewPrefabConfig";

    // ──────────────────────────────────────────────────────────
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

    // ──────────────────────────────────────────────────────────
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

    // ──────────────────────────────────────────────────────────
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

    // ──────────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Tra cứu prefab phù hợp dựa trên PlayerDataResponse của local player.
    /// Ưu tiên: exact match (element+gender) → element only → fallback.
    /// Hybrid: dùng HybridPrefabMap nếu được cấu hình.
    /// </summary>
    public GameObject Resolve(PlayerDataResponse data)
    {
        if (data == null) return fallbackPrefab;

        // ── Hybrid ──
        if (data.is_hybrid && hybridPrefabMap != null)
        {
            // hybrid_prefab_path dạng "Earth_Fire" hoặc 2 element riêng
            string hPath = data.hybrid_prefab_path;
            if (!string.IsNullOrEmpty(hPath))
            {
                var hyGo = hybridPrefabMap.GetByPath(hPath);
                if (hyGo != null) return hyGo;
            }
        }

        string elementType = data.element_type ?? "";
        string gender      = data.gender       ?? "";

        if (_cache == null) RebuildCache();

        // Pass 1: exact element + gender
        if (_cache.TryGetValue(MakeKey(elementType, gender), out var go1) && go1 != null)
            return go1;

        // Pass 2: element only (gender trống)
        if (_cache.TryGetValue(MakeKey(elementType, ""), out var go2) && go2 != null)
            return go2;

        // Pass 3: fallback
        if (fallbackPrefab != null)
            return fallbackPrefab;

        Debug.LogWarning($"[PlayerPreviewPrefabConfig] Không tìm thấy prefab cho " +
                         $"elementType='{elementType}' gender='{gender}'. Kiểm tra lại config asset.");
        return null;
    }

    /// <summary>
    /// Load singleton từ Resources (không cần kéo tay vào Inspector).
    /// Trả về null nếu asset chưa được tạo.
    /// </summary>
    public static PlayerPreviewPrefabConfig Load()
        => Resources.Load<PlayerPreviewPrefabConfig>(DefaultResourcesPath);

    #endregion
}
