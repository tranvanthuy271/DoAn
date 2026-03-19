using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject lưu ánh xạ từ hybrid key → Prefab GameObject.
/// Key format: "{ElementA}_{ElementB}" (alphabetically sorted, English).
/// Ví dụ: "Earth_Fire", "Metal_Wind", "Wind_Wood".
///
/// ═══════════════════════════════════════════════════════════
/// CÁCH TẠO ASSET:
///   1. Project window → chuột phải → Create → Game → HybridPrefabMap
///   2. Đặt tên: HybridPrefabMap (trong Assets/ScriptableObjects/)
///   3. Trong Inspector, bấm "+" để thêm từng entry:
///      Key     = "Earth_Fire"     (phải khớp NormalizeKey từ DB)
///      Prefab  = Hybrid_Earth_Fire.prefab
///   4. Kéo HybridPrefabMap asset vào field [HybridPrefabMap] của CharacterLoader/PlayerSpawner
/// ═══════════════════════════════════════════════════════════
/// </summary>
[CreateAssetMenu(fileName = "HybridPrefabMap", menuName = "Game/HybridPrefabMap")]
public class HybridPrefabMap : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [Tooltip("\"ElementA_ElementB\" — alphabetically sorted, English. Ví dụ: Earth_Fire, Metal_Wind")]
        public string key;
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries = new();

    // Cache để tra cứu O(1)
    private Dictionary<string, GameObject> _cache;

    private void OnEnable() => RebuildCache();

    private void OnValidate() => RebuildCache();

    private void RebuildCache()
    {
        _cache = new Dictionary<string, GameObject>(entries.Count, System.StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
            if (!string.IsNullOrEmpty(e.key) && e.prefab != null)
                _cache[e.key] = e.prefab;
    }

    /// <summary>
    /// Lấy hybrid prefab theo 2 element (thứ tự không quan trọng).
    /// Trả về null nếu không tìm thấy.
    /// </summary>
    public GameObject Get(string elementA, string elementB)
    {
        if (_cache == null) RebuildCache();
        string key = NormalizeKey(elementA, elementB);
        _cache.TryGetValue(key, out var go);
        return go;
    }

    /// <summary>Lấy hybrid prefab theo key đã chuẩn hoá từ server (prefab_path cuối cùng).</summary>
    public GameObject GetByPath(string prefabPath)
    {
        if (_cache == null) RebuildCache();
        // prefabPath từ DB: "Prefabs/Player/Hybrid/Hybrid_Earth_Fire"
        // Lấy phần sau dấu "/" cuối cùng: "Hybrid_Earth_Fire" rồi strip "Hybrid_"
        if (string.IsNullOrEmpty(prefabPath)) return null;
        int slash = prefabPath.LastIndexOf('/');
        string file = slash >= 0 ? prefabPath[(slash + 1)..] : prefabPath;
        // Strip tiền tố "Hybrid_"
        if (file.StartsWith("Hybrid_", System.StringComparison.OrdinalIgnoreCase))
            file = file[7..];  // "Earth_Fire"

        _cache.TryGetValue(file, out var go);
        return go;
    }

    public static string NormalizeKey(string a, string b) =>
        string.Compare(a, b, System.StringComparison.OrdinalIgnoreCase) <= 0
            ? $"{a}_{b}"
            : $"{b}_{a}";
}
