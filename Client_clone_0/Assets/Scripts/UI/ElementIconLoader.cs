using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Tải icon + màu sắc cho tất cả hệ nguyên tố từ API,
/// cache lại và cung cấp GetIcon / GetColor cho các UI khác.
///
/// Dùng JsonUtility (không phụ thuộc Newtonsoft.Json).
///
/// Setup:
///   1. Gắn script này lên GameObject persistent (ví dụ: GameManager).
///   2. Đặt apiBaseUrl trỏ đúng server.
///   3. Đặt file ảnh vào Assets/Resources/Elements/icon_hoa.png, v.v.
///   4. Gọi ElementIconLoader.Instance.GetIcon("Hoa") từ HUD enemy.
/// </summary>
public class ElementIconLoader : MonoBehaviour
{
    public static ElementIconLoader Instance { get; private set; }

    [SerializeField] private string apiBaseUrl = "";

    /// <summary>Cache sprite — element_key → Sprite (từ Resources)</summary>
    public Dictionary<string, Sprite> Icons  { get; private set; } = new Dictionary<string, Sprite>();

    /// <summary>Cache màu — element_key → Color (parse từ hex trong DB)</summary>
    public Dictionary<string, Color>  Colors { get; private set; } = new Dictionary<string, Color>();

    public bool IsLoaded { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        apiBaseUrl = ServerAddressConfig.Instance.ResolveApiRoot(apiBaseUrl);
        StartCoroutine(LoadAllElements());
    }

    // ──────────────────────────────────────────────────────────────────

    private IEnumerator LoadAllElements()
    {
        string url = $"{apiBaseUrl}/api/element-type";
        using var req = UnityWebRequest.Get(url);
        AuthHelper.AddAuthHeader(req);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[ElementIconLoader] Lỗi tải hệ nguyên tố: {req.error}");
            yield break;
        }

        ElementListWrapper wrapper;
        try
        {
            // API trả về JSON array → bọc thành object
            wrapper = JsonUtility.FromJson<ElementListWrapper>(
                "{\"elements\":" + req.downloadHandler.text + "}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ElementIconLoader] Parse thất bại: {ex.Message}");
            yield break;
        }

        if (wrapper?.elements == null) yield break;

        foreach (var elem in wrapper.elements)
        {
            if (string.IsNullOrEmpty(elem.element_key)) continue;

            // Load sprite từ Resources (đường dẫn không có đuôi .png)
            if (!string.IsNullOrEmpty(elem.icon_path))
            {
                var sprite = Resources.Load<Sprite>(elem.icon_path);
                if (sprite != null)
                    Icons[elem.element_key] = sprite;
                else
                    Debug.LogWarning($"[ElementIconLoader] Thiếu sprite: Resources/{elem.icon_path}.png");
            }

            // Parse màu hex (#FF4500)
            if (!string.IsNullOrEmpty(elem.color_hex) &&
                ColorUtility.TryParseHtmlString(elem.color_hex, out Color c))
                Colors[elem.element_key] = c;
        }

        IsLoaded = true;
        Debug.Log($"[ElementIconLoader] Tải xong {Icons.Count} icon hệ nguyên tố.");
    }

    // ──────────────────────────────────────────────────────────────────
    //  Public API
    // ──────────────────────────────────────────────────────────────────

    /// <summary>Lấy Sprite của hệ. Trả null nếu chưa load hoặc không tìm thấy.</summary>
    public Sprite GetIcon(string elementKey)
        => Icons.TryGetValue(elementKey, out var s) ? s : null;

    /// <summary>Lấy Color của hệ. Trả Color.white nếu không tìm thấy.</summary>
    public Color GetColor(string elementKey)
        => Colors.TryGetValue(elementKey, out var c) ? c : Color.white;

    // ──────────────────────────────────────────────────────────────────
    //  DTOs — phải dùng [System.Serializable] + field public/snake_case
    //         để JsonUtility hoạt động đúng
    // ──────────────────────────────────────────────────────────────────

    [System.Serializable]
    private class ElementListWrapper
    {
        public ElementDto[] elements;
    }

    [System.Serializable]
    private class ElementDto
    {
        public string element_key;
        public string display_name;
        public string icon_path;
        public string color_hex;
    }
}
