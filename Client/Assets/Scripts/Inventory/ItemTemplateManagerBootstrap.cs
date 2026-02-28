using UnityEngine;

/// <summary>
/// Bootstrap để tự động tạo APIClient và ItemTemplateManager nếu chưa có
/// Attach script này vào GameObject trong scene (ví dụ: GameManager hoặc bất kỳ GameObject nào)
/// QUAN TRỌNG: Script này phải được thực thi TRƯỚC các script khác cần dùng APIClient/ItemTemplateManager
/// </summary>
public class ItemTemplateManagerBootstrap : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Tự động tạo APIClient và ItemTemplateManager nếu chưa có")]
    [SerializeField] private bool autoCreate = true;

    void Awake()
    {
        if (!autoCreate) return;

        // 1. Kiểm tra và tạo APIClient nếu chưa có (QUAN TRỌNG!)
        if (APIClient.Instance == null)
        {
            Debug.Log("[ItemTemplateManagerBootstrap] APIClient chưa có, đang tạo...");
            
            GameObject apiClientObj = new GameObject("APIClient");
            apiClientObj.AddComponent<APIClient>();
            
            Debug.Log("[ItemTemplateManagerBootstrap] ✅ Đã tạo APIClient");
        }
        else
        {
            Debug.Log("[ItemTemplateManagerBootstrap] APIClient đã tồn tại");
        }

        // 2. Kiểm tra và tạo ItemTemplateManager nếu chưa có
        if (ItemTemplateManager.Instance == null)
        {
            Debug.Log("[ItemTemplateManagerBootstrap] ItemTemplateManager chưa có, đang tạo...");
            
            GameObject obj = new GameObject("ItemTemplateManager");
            obj.AddComponent<ItemTemplateManager>();
            
            Debug.Log("[ItemTemplateManagerBootstrap] ✅ Đã tạo ItemTemplateManager");
        }
        else
        {
            Debug.Log("[ItemTemplateManagerBootstrap] ItemTemplateManager đã tồn tại");
        }
    }
}
