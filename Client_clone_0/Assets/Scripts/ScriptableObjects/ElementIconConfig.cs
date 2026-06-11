using UnityEngine;

// ScriptableObject ánh xạ elementId → Sprite icon của hệ nguyên tố.
// Thứ tự index phải khớp với ElementHelper:
// 0 = Kim  | 1 = Mộc  | 2 = Thủy
// 3 = Hỏa  | 4 = Thổ  | 5 = Phong
// CÁCH TẠO ASSET:
// 1. Project window → chuột phải → Create → Game → ElementIconConfig
// 2. Đặt tên: ElementIconConfig (trong Assets/ScriptableObjects/)
// 3. Trong Inspector, kéo thả từng Sprite vào đúng slot hệ tương ứng
// 4. Kéo asset này vào field [elementIconConfig] của các script cần dùng
// (hoặc dùng ElementIconConfig.Instance nếu đã assign vào GameSettings)
[CreateAssetMenu(fileName = "ElementIconConfig", menuName = "Game/ElementIconConfig")]
public class ElementIconConfig : ScriptableObject
{
    public const string DefaultResourcesPath = "ScriptableObjects/ElementIconConfig";

    public enum SpriteKind
    {
        Icon,
        Avatar,
    }

    [System.Serializable]
    public class Entry
    {
        [HideInInspector] public string label; // chỉ để inspector dễ đọc
        public Sprite icon;
        public Sprite avatar;
        public Color  color = Color.white;     // màu đại diện cho HUD (HP bar, glow, v.v.)
    }

    [Tooltip("Index 0=Kim 1=Mộc 2=Thủy 3=Hỏa 4=Thổ 5=Phong — phải đủ 6 phần tử")]
    [SerializeField] private Entry[] entries = new Entry[6];

    private void OnValidate()
    {
        EnsureEntries();

        // Tự điền label để Inspector dễ nhìn
        var names = ElementHelper.VietnameseNames;
        for (int i = 0; i < entries.Length && i < names.Length; i++)
            entries[i].label = $"[{i}] {names[i]}";
    }

    public static ElementIconConfig Resolve(ElementIconConfig assignedConfig, Object context, string owner)
    {
        if (assignedConfig != null)
            return assignedConfig;

        var loadedConfig = Resources.Load<ElementIconConfig>(DefaultResourcesPath);
        if (loadedConfig == null)
        {
            Debug.LogWarning(
                $"[{owner}] Chưa gán ElementIconConfig và không tìm thấy asset mặc định tại Resources/{DefaultResourcesPath}.",
                context);
        }

        return loadedConfig;
    }

    // Trả về Sprite icon của hệ theo elementId (0–5).
    // Trả về null nếu chưa gán hoặc id ngoài phạm vi.
    public Sprite GetIcon(int elementId)
    {
        var entry = GetEntry(elementId);
        return entry != null ? entry.icon : null;
    }

    public Sprite GetIcon(string elementKey) => GetIcon(ElementHelper.ToId(elementKey));

    public Sprite GetAvatar(int elementId)
    {
        var entry = GetEntry(elementId);
        if (entry == null)
            return null;

        return entry.avatar != null ? entry.avatar : entry.icon;
    }

    public Sprite GetAvatar(string elementKey) => GetAvatar(ElementHelper.ToId(elementKey));

    public Sprite GetSprite(int elementId, SpriteKind spriteKind)
        => spriteKind == SpriteKind.Avatar ? GetAvatar(elementId) : GetIcon(elementId);

    public Sprite GetSpriteOrLog(int elementId, SpriteKind spriteKind, Object context, string owner)
    {
        var sprite = GetSprite(elementId, spriteKind);
        if (sprite == null)
        {
            string spriteLabel = spriteKind == SpriteKind.Avatar ? "avatar" : "icon";
            string elementLabel = ElementHelper.IsValid(elementId)
                ? ElementHelper.ToVietnamese(elementId)
                : $"ID {elementId}";

            Debug.LogWarning(
                $"[{owner}] Thiếu sprite {spriteLabel} cho hệ {elementLabel} trong asset '{name}'.",
                context);
        }

        return sprite;
    }

    // Trả về Color đại diện của hệ theo elementId (0–5).
    // Trả về Color.white nếu ngoài phạm vi.
    public Color GetColor(int elementId)
    {
        if (elementId < 0 || elementId >= entries.Length) return Color.white;
        return entries[elementId].color;
    }

    public Color GetColor(string elementKey) => GetColor(ElementHelper.ToId(elementKey));

    private void EnsureEntries()
    {
        int targetCount = Mathf.Max(ElementHelper.Count, 6);
        if (entries == null)
            entries = new Entry[targetCount];

        if (entries.Length != targetCount)
        {
            var resizedEntries = new Entry[targetCount];
            int copyCount = Mathf.Min(entries.Length, resizedEntries.Length);
            for (int i = 0; i < copyCount; i++)
                resizedEntries[i] = entries[i];

            entries = resizedEntries;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i] == null)
                entries[i] = new Entry();
        }
    }

    private Entry GetEntry(int elementId)
    {
        if (elementId < 0 || elementId >= entries.Length)
            return null;

        return entries[elementId];
    }
}
