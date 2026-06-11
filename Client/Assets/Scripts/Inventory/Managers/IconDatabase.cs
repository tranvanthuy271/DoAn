using System.Collections.Generic;
using UnityEngine;

// IconDatabase
// - Load toàn bộ Sprite icon item từ Resources/ItemIcons (mặc định).
// - Tra cứu Sprite theo iconId (trùng với tên sprite hoặc key bạn quy ước).
// - Nên để 1 instance duy nhất trong scene (DontDestroyOnLoad).
public class IconDatabase : MonoBehaviour
{
    public static IconDatabase Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Folder trong Resources chứa icon item")]
    [SerializeField] private string resourcesFolder = "ItemIcons";

    private readonly Dictionary<string, Sprite> _icons = new Dictionary<string, Sprite>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllIcons();
    }

    // Load toàn bộ Sprite trong Resources/{resourcesFolder} vào dictionary
    private void LoadAllIcons()
    {
        _icons.Clear();

        Sprite[] sprites = Resources.LoadAll<Sprite>(resourcesFolder);
        foreach (var sprite in sprites)
        {
            if (sprite == null) continue;

            // Mặc định dùng tên sprite làm iconId
            if (!_icons.ContainsKey(sprite.name))
            {
                _icons[sprite.name] = sprite;
            }
        }

        Debug.Log($"[IconDatabase] Loaded {_icons.Count} item icons from Resources/{resourcesFolder}");
    }

    // Lấy Sprite theo iconId.
    // iconId nên trùng tên sprite (hoặc bạn map theo quy ước riêng).
    public Sprite GetIcon(string iconId)
    {
        if (string.IsNullOrEmpty(iconId))
            return null;

        if (_icons.TryGetValue(iconId, out var sprite))
        {
            return sprite;
        }

        Debug.LogWarning($"[IconDatabase] IconId '{iconId}' not found in cache.");
        return null;
    }
}

