using UnityEngine;
using Unity.Netcode;

/// <summary>
/// ItemSpawner - Helper class để spawn item pickup dễ dàng
/// Có thể dùng để spawn item từ code hoặc trong Inspector
/// </summary>
public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab của ItemPickup")]
    [SerializeField] private GameObject itemPickupPrefab;
    
    [Tooltip("Vị trí spawn (nếu để trống sẽ dùng transform.position)")]
    [SerializeField] private Transform spawnPoint;

    /// <summary>
    /// Spawn item tại vị trí hiện tại
    /// </summary>
    public void SpawnItem(int itemID, int quantity)
    {
        SpawnItemAt(itemID, quantity, spawnPoint != null ? spawnPoint.position : transform.position);
    }

    /// <summary>
    /// Spawn item tại vị trí cụ thể
    /// </summary>
    public void SpawnItemAt(int itemID, int quantity, Vector3 position)
    {
        if (itemPickupPrefab == null)
        {
            Debug.LogError("[ItemSpawner] ItemPickupPrefab chưa được gán!");
            return;
        }

        ItemData itemData = GetItemData(itemID);
        if (itemData == null)
        {
            Debug.LogError($"[ItemSpawner] ItemID {itemID} không tồn tại!");
            return;
        }

        // Chỉ server mới spawn được
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            Debug.LogWarning("[ItemSpawner] Chỉ server mới spawn được item!");
            return;
        }

        GameObject itemObj = Instantiate(itemPickupPrefab, position, Quaternion.identity);
        
        // Spawn network object nếu đang ở network mode
        NetworkObject networkObject = itemObj.GetComponent<NetworkObject>();
        if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            networkObject.Spawn();
        }

        // Set item data
        ItemPickup itemPickup = itemObj.GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.SetItemData(itemData, quantity);
        }

        Debug.Log($"[ItemSpawner] Spawned {quantity}x {itemData.itemName} at {position}");
    }

    /// <summary>
    /// Spawn item với ItemData
    /// </summary>
    public void SpawnItem(ItemData itemData, int quantity)
    {
        if (itemData == null) return;
        SpawnItemAt(itemData.itemID, quantity, spawnPoint != null ? spawnPoint.position : transform.position);
    }

    /// <summary>
    /// Lấy ItemData từ ItemID
    /// </summary>
    private ItemData GetItemData(int itemID)
    {
        if (ItemManager.Instance != null)
        {
            return ItemManager.Instance.GetItemData(itemID);
        }
        
        // Fallback
        ItemData[] allItems = Resources.LoadAll<ItemData>("Items");
        foreach (var item in allItems)
        {
            if (item.itemID == itemID)
                return item;
        }
        return null;
    }

    // Gizmos để visualize spawn point
    private void OnDrawGizmos()
    {
        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(pos, 0.5f);
    }
}
