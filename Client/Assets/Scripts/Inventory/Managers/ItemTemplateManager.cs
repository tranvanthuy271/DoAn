using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

// ItemTemplateManager - Quản lý item templates từ server/DB
// Chức năng:
// - Load tất cả item templates từ API khi Start
// - Cache trong RAM để truy cập nhanh
// - Cung cấp API để lấy item template theo ID/Code
// - Singleton pattern để dễ dàng truy cập từ bất kỳ đâu
// Setup:
// 1. Gắn script này vào GameObject trong scene (hoặc tạo GameObject mới tên "ItemTemplateManager")
// 2. Script sẽ tự động load item templates từ API khi Start
// 3. Các script khác có thể lấy item template: ItemTemplateManager.Instance.GetItemTemplate(id)
public class ItemTemplateManager : MonoBehaviour
{
    public static ItemTemplateManager Instance { get; private set; }

    // Đảm bảo singleton tồn tại (dành cho dedicated server, tự tạo nếu chưa có).
    public static void EnsureInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("ItemTemplateManager_AutoCreated");
        go.AddComponent<ItemTemplateManager>();
        Debug.Log("[ItemTemplateManager] EnsureInstance: tự tạo singleton cho dedicated server.");
    }

    [Header("Settings")]
    [Tooltip("Có tự động load item templates khi Start không")]
    [SerializeField] private bool autoLoadOnStart = true;

    [Tooltip("Có bật debug log không")]
    [SerializeField] private bool enableDebugLog = true;

    // Cache item templates trong RAM
    private Dictionary<int, ItemTemplateDto> itemTemplatesById = new Dictionary<int, ItemTemplateDto>();
    private Dictionary<string, ItemTemplateDto> itemTemplatesByCode = new Dictionary<string, ItemTemplateDto>();

    // Trạng thái load
    private bool isLoaded = false;
    private bool isLoading = false;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[ItemTemplateManager] ✅ Singleton initialized - GameObject: {gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"[ItemTemplateManager] ⚠️ Duplicate instance detected! Destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        Debug.Log($"[ItemTemplateManager] 🚀 Start() called - autoLoadOnStart={autoLoadOnStart}");
        if (autoLoadOnStart)
        {
            // Delay một chút để đảm bảo APIClient đã khởi tạo
            StartCoroutine(LoadItemTemplatesWhenReady());
        }
        else
        {
            Debug.Log("[ItemTemplateManager] AutoLoad is disabled, call LoadItemTemplatesFromAPI() manually");
        }
    }

    // Đợi APIClient sẵn sàng rồi mới load item templates
    private System.Collections.IEnumerator LoadItemTemplatesWhenReady()
    {
        Debug.Log("[ItemTemplateManager] ⏳ Đang đợi APIClient sẵn sàng...");
        
        // Đợi tối đa 10 giây
        float timeout = 10f;
        float elapsed = 0f;
        
        while (APIClient.Instance == null && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (APIClient.Instance == null)
        {
            Debug.LogWarning("[ItemTemplateManager] APIClient null (dedicated server?) → dùng UnityWebRequest trực tiếp.");
            StartCoroutine(LoadItemTemplatesDirect());
        }
        else
        {
            Debug.Log($"[ItemTemplateManager] ✅ APIClient đã sẵn sàng sau {elapsed:F1}s");
            LoadItemTemplatesFromAPI();
        }
    }

    // Fallback: load templates trực tiếp bằng UnityWebRequest (dành cho dedicated server không có APIClient).
    private System.Collections.IEnumerator LoadItemTemplatesDirect()
    {
        if (isLoading || isLoaded) yield break;
        isLoading = true;

        string apiBase = ServerAddressConfig.Instance.ApiUrl;
        // Thử lấy API URL từ MapWorldConfig nếu có
        var bootstrap = FindObjectOfType<MapWorldBootstrap>();
        if (bootstrap != null)
        {
            var configField = bootstrap.GetType().GetField("_apiBaseUrl",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (configField != null)
            {
                string val = configField.GetValue(bootstrap) as string;
                if (!string.IsNullOrEmpty(val)) apiBase = val;
            }
        }

        string url = $"{apiBase.TrimEnd('/')}/item/templates";
        Debug.Log($"[ItemTemplateManager] LoadItemTemplatesDirect: GET {url}");

        using (var www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[ItemTemplateManager] LoadItemTemplatesDirect FAILED: {www.error}");
                isLoading = false;
                yield break;
            }

            string json = www.downloadHandler.text;
            try
            {
                var response = JsonUtility.FromJson<ItemTemplatesResponse>(json);
                if (response != null && response.item_templates != null)
                {
                    OnItemTemplatesLoaded(response.item_templates);
                    isLoaded = true;
                    Debug.Log($"[ItemTemplateManager] ✅ LoadItemTemplatesDirect: loaded {response.item_templates.Length} templates.");
                }
                else
                {
                    Debug.LogError("[ItemTemplateManager] LoadItemTemplatesDirect: response hoặc item_templates null.");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ItemTemplateManager] LoadItemTemplatesDirect parse error: {ex.Message}");
            }
            isLoading = false;
        }
    }

    [System.Serializable]
    private class ItemTemplatesResponse
    {
        public ItemTemplateDto[] item_templates;
    }

    // Load tất cả item templates từ API
    public void LoadItemTemplatesFromAPI()
    {
        Debug.Log($"[ItemTemplateManager] 📥 LoadItemTemplatesFromAPI() called - isLoading={isLoading}, isLoaded={isLoaded}");
        
        if (isLoading)
        {
            Debug.LogWarning("[ItemTemplateManager] ⏳ Đang loading item templates, vui lòng đợi...");
            return;
        }

        if (isLoaded)
        {
            Debug.Log($"[ItemTemplateManager] ✅ Item templates đã được load trước đó ({itemTemplatesById.Count} items)");
            return;
        }

        // Hybrid: dùng UnityWebRequest trực tiếp để tránh phụ thuộc gameplay APIClient.
        StartCoroutine(LoadItemTemplatesDirect());
    }

    // Callback khi item templates được load thành công
    private void OnItemTemplatesLoaded(ItemTemplateDto[] templates)
    {
        Debug.Log($"[ItemTemplateManager] 📦 OnItemTemplatesLoaded() - Received {templates.Length} templates");
        
        itemTemplatesById.Clear();
        itemTemplatesByCode.Clear();

        foreach (var template in templates)
        {
            itemTemplatesById[template.id] = template;
            
            if (!string.IsNullOrEmpty(template.code))
            {
                itemTemplatesByCode[template.code] = template;
            }
        }

        Debug.Log($"[ItemTemplateManager] ✅ Đã load {templates.Length} item templates thành công!");
        Debug.Log($"[ItemTemplateManager] 📊 Dictionary Stats - ById: {itemTemplatesById.Count}, ByCode: {itemTemplatesByCode.Count}");
        
        // Log top 10 items để debug
        int logCount = Mathf.Min(10, templates.Length);
        Debug.Log($"[ItemTemplateManager] 📋 Logging first {logCount} items:");
        for (int i = 0; i < logCount; i++)
        {
            var template = templates[i];
            Debug.Log($"  [{i+1}] ID={template.id}, Name='{template.name}', Code='{template.code}', IconId='{template.icon_id}', Type={template.item_type}, Stackable={template.stackable}");
        }
    }

    // Lấy item template theo ID
    public ItemTemplateDto GetItemTemplate(int id)
    {
        if (!isLoaded)
        {
            Debug.LogWarning($"[ItemTemplateManager] ⚠️ Item templates chưa được load! Gọi LoadItemTemplatesFromAPI() trước.");
            return null;
        }

        if (itemTemplatesById.TryGetValue(id, out var template))
        {
            if (enableDebugLog)
            {
                Debug.Log($"[ItemTemplateManager] ✅ Found item template ID={id}: '{template.name}' (code={template.code})");
            }
            return template;
        }

        Debug.LogWarning($"[ItemTemplateManager] ❌ Item template với ID {id} không tồn tại! Total loaded: {itemTemplatesById.Count}");
        return null;
    }

    // Lấy item template theo code
    public ItemTemplateDto GetItemTemplateByCode(string code)
    {
        if (!isLoaded)
        {
            Debug.LogWarning($"[ItemTemplateManager] Item templates chưa được load! Gọi LoadItemTemplatesFromAPI() trước.");
            return null;
        }

        if (string.IsNullOrEmpty(code))
        {
            return null;
        }

        if (itemTemplatesByCode.TryGetValue(code, out var template))
        {
            return template;
        }

        Debug.LogWarning($"[ItemTemplateManager] Item template với code '{code}' không tồn tại!");
        return null;
    }

    // Lấy tất cả item templates
    public ItemTemplateDto[] GetAllItemTemplates()
    {
        if (!isLoaded)
        {
            Debug.LogWarning($"[ItemTemplateManager] Item templates chưa được load!");
            return new ItemTemplateDto[0];
        }

        return itemTemplatesById.Values.ToArray();
    }

    // Kiểm tra item template có tồn tại không
    public bool HasItemTemplate(int id)
    {
        return isLoaded && itemTemplatesById.ContainsKey(id);
    }

    // Kiểm tra item template có tồn tại không (theo code)
    public bool HasItemTemplateByCode(string code)
    {
        return isLoaded && !string.IsNullOrEmpty(code) && itemTemplatesByCode.ContainsKey(code);
    }

    // Lấy số lượng item templates đã load
    public int GetItemTemplateCount()
    {
        return itemTemplatesById.Count;
    }

    // Kiểm tra đã load xong chưa
    public bool IsLoaded()
    {
        return isLoaded;
    }

    // Kiểm tra đang loading không
    public bool IsLoading()
    {
        return isLoading;
    }

    // Force reload item templates từ API (reset cả isLoading để tránh bị block)
    public void Reload()
    {
        isLoaded = false;
        isLoading = false;
        LoadItemTemplatesFromAPI();
    }
}
