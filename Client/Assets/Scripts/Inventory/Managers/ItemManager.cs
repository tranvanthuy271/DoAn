using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ItemManager - Singleton để quản lý ItemData database
/// Load tất cả ItemData từ Resources và cung cấp API để truy cập
/// </summary>
public class ItemManager : MonoBehaviour
{
    private static ItemManager instance;
    private Dictionary<int, ItemData> itemDatabase = new Dictionary<int, ItemData>();

    [Header("Settings")]
    [Tooltip("Đường dẫn trong Resources để load ItemData")]
    [SerializeField] private string itemsResourcePath = "Items";

    public static ItemManager Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            LoadItemDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Load tất cả ItemData từ Resources
    /// </summary>
    private void LoadItemDatabase()
    {
        itemDatabase.Clear();
        
        ItemData[] items = Resources.LoadAll<ItemData>(itemsResourcePath);
        foreach (var item in items)
        {
            if (item != null && item.itemID != 0)
            {
                if (itemDatabase.ContainsKey(item.itemID))
                {
                    Debug.LogWarning($"[ItemManager] Duplicate ItemID {item.itemID} found! Item: {item.itemName}");
                }
                else
                {
                    itemDatabase[item.itemID] = item;
                }
            }
        }
        
        Debug.Log($"[ItemManager] Loaded {itemDatabase.Count} items from Resources/{itemsResourcePath}");
    }

    /// <summary>
    /// Lấy ItemData theo ItemID
    /// </summary>
    public ItemData GetItemData(int itemID)
    {
        if (itemDatabase.TryGetValue(itemID, out ItemData item))
        {
            return item;
        }
        
        Debug.LogWarning($"[ItemManager] ItemID {itemID} not found in database!");
        return null;
    }

    /// <summary>
    /// Kiểm tra ItemID có tồn tại không
    /// </summary>
    public bool HasItem(int itemID)
    {
        return itemDatabase.ContainsKey(itemID);
    }

    /// <summary>
    /// Lấy tất cả ItemData
    /// </summary>
    public Dictionary<int, ItemData> GetAllItems()
    {
        return new Dictionary<int, ItemData>(itemDatabase);
    }

    /// <summary>
    /// Reload database (dùng khi thêm item mới trong runtime)
    /// </summary>
    public void ReloadDatabase()
    {
        LoadItemDatabase();
    }
}
