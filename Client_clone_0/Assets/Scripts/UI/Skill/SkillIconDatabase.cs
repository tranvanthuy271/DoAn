using System.Collections.Generic;
using UnityEngine;

// SkillIconDatabase — Singleton tải tất cả sprite từ Resources/SkillIcons/
// và tra cứu theo iconId hoặc skillCode.
// Cách đặt file icon
// Đặt file ảnh .png vào:   Assets/Resources/SkillIcons/
// Đặt tên file trùng với:
// • icon_id trả về từ API (field PlayerSkillInfo.icon_id), ví dụ: "skill_wind_strike"
// • hoặc skill_code của SkillData, ví dụ: "WIND_STRIKE"
// (Cả 2 đều được kiểm tra; icon_id ưu tiên trước)
// Setup trong Unity
// Tạo một GameObject rỗng trong scene, đặt tên "SkillIconDatabase",
// gắn component này vào. Không cần config gì thêm.
// Script cũng tự tạo instance qua RuntimeInitializeOnLoadMethod nếu scene chưa có.
public class SkillIconDatabase : MonoBehaviour
{
    public static SkillIconDatabase Instance { get; private set; }

    [Tooltip("Thư mục bên trong Resources/ chứa sprite icon skill")]
    [SerializeField] private string resourcesFolder = "SkillIcons";

    private readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);

    // Auto-bootstrap: nếu scene không có SkillIconDatabase, tự tạo khi game bắt đầu.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoBootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("SkillIconDatabase [auto]");
        go.AddComponent<SkillIconDatabase>();
        DontDestroyOnLoad(go);
        { /* Auto-bootstrapped (không tìm thấy instance trong scene) */ }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent != null)
            transform.SetParent(null, true);
        DontDestroyOnLoad(gameObject);
        LoadAllIcons();
    }

    private void LoadAllIcons()
    {
        var sprites = Resources.LoadAll<Sprite>(resourcesFolder);
        _icons.Clear();
        foreach (var sprite in sprites)
            _icons[sprite.name] = sprite;

        { /* Loaded {_icons.Count} skill icon(s) from Resources/{resourcesFolder}/ */ }
    }

    // Trả về Sprite theo iconId hoặc skillCode. Không phân biệt hoa thường.
    // Trả về null nếu không tìm thấy.
    public Sprite GetIcon(string iconId)
    {
        if (string.IsNullOrEmpty(iconId)) return null;
        _icons.TryGetValue(iconId, out var sprite);
        return sprite;
    }
}
