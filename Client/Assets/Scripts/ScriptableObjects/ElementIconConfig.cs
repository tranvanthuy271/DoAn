using UnityEngine;

/// <summary>
/// ScriptableObject ánh xạ elementId → Sprite icon của hệ nguyên tố.
///
/// Thứ tự index phải khớp với ElementHelper:
///   0 = Kim  | 1 = Mộc  | 2 = Thủy
///   3 = Hỏa  | 4 = Thổ  | 5 = Phong
///
/// ═══════════════════════════════════════════════════════════
/// CÁCH TẠO ASSET:
///   1. Project window → chuột phải → Create → Game → ElementIconConfig
///   2. Đặt tên: ElementIconConfig (trong Assets/ScriptableObjects/)
///   3. Trong Inspector, kéo thả từng Sprite vào đúng slot hệ tương ứng
///   4. Kéo asset này vào field [elementIconConfig] của các script cần dùng
///      (hoặc dùng ElementIconConfig.Instance nếu đã assign vào GameSettings)
/// ═══════════════════════════════════════════════════════════
/// </summary>
[CreateAssetMenu(fileName = "ElementIconConfig", menuName = "Game/ElementIconConfig")]
public class ElementIconConfig : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        [HideInInspector] public string label; // chỉ để inspector dễ đọc
        public Sprite icon;
        public Color  color = Color.white;     // màu đại diện cho HUD (HP bar, glow, v.v.)
    }

    [Tooltip("Index 0=Kim 1=Mộc 2=Thủy 3=Hỏa 4=Thổ 5=Phong — phải đủ 6 phần tử")]
    [SerializeField] private Entry[] entries = new Entry[6];

    private void OnValidate()
    {
        // Tự điền label để Inspector dễ nhìn
        var names = ElementHelper.VietnameseNames;
        for (int i = 0; i < entries.Length && i < names.Length; i++)
            entries[i].label = $"[{i}] {names[i]}";
    }

    /// <summary>
    /// Trả về Sprite icon của hệ theo elementId (0–5).
    /// Trả về null nếu chưa gán hoặc id ngoài phạm vi.
    /// </summary>
    public Sprite GetIcon(int elementId)
    {
        if (elementId < 0 || elementId >= entries.Length) return null;
        return entries[elementId].icon;
    }

    /// <summary>
    /// Trả về Color đại diện của hệ theo elementId (0–5).
    /// Trả về Color.white nếu ngoài phạm vi.
    /// </summary>
    public Color GetColor(int elementId)
    {
        if (elementId < 0 || elementId >= entries.Length) return Color.white;
        return entries[elementId].color;
    }
}
