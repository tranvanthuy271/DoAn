using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Database cấu hình aura Gene Tối Thượng theo từng **cặp Fusion (Hybrid)**.
/// Fusion chỉ có 3 cặp (Fire↔Earth, Water↔Wood, Metal↔Wind); mỗi cặp dùng 1 aura riêng.
/// Mỗi entry liệt kê các hệ thuộc cùng cặp (vd Fire + Earth) trỏ chung 1 prefab aura.
/// Tạo asset: chuột phải trong Project → Create → Game → Ultimate Aura Database.
/// Gắn asset này vào trường "Aura Database" của <see cref="UltimateAuraVisual"/> trên player prefab.
/// </summary>
[CreateAssetMenu(fileName = "UltimateAuraDatabase", menuName = "Game/Ultimate Aura Database", order = 0)]
public class UltimateAuraDatabase : ScriptableObject
{
    [Serializable]
    public class AuraEntry
    {
        [Tooltip("Các hệ thuộc cùng 1 cặp Fusion. Vd cặp 1: Fire, Earth. Cặp 2: Water, Wood. Cặp 3: Metal, Wind.")]
        public List<string> elementKeys = new List<string>();

        [Tooltip("Prefab aura dùng chung cho cả cặp này.")]
        public GameObject auraPrefab;
    }

    [Tooltip("Danh sách aura theo từng cặp Fusion (chỉ cần 3 entry).")]
    [SerializeField] private List<AuraEntry> auras = new List<AuraEntry>();

    [Tooltip("Aura dùng chung khi không tìm thấy entry khớp hệ (tùy chọn).")]
    [SerializeField] private GameObject defaultAuraPrefab;

    /// <summary>
    /// Lấy prefab aura cho hệ <paramref name="key"/> (so khớp không phân biệt hoa thường).
    /// Tìm entry nào có hệ nằm trong <see cref="AuraEntry.elementKeys"/>.
    /// Trả về <see cref="defaultAuraPrefab"/> nếu không khớp, hoặc null nếu cũng không có default.
    /// </summary>
    public GameObject GetAura(string key)
    {
        if (!string.IsNullOrEmpty(key))
        {
            foreach (var e in auras)
            {
                if (e == null || e.auraPrefab == null || e.elementKeys == null)
                    continue;

                foreach (var k in e.elementKeys)
                {
                    if (string.Equals(k, key, StringComparison.OrdinalIgnoreCase))
                        return e.auraPrefab;
                }
            }
        }

        return defaultAuraPrefab;
    }
}
