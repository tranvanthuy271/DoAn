using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// EnemyItemDrop - Component để drop item khi enemy chết
/// Gắn vào enemy GameObject
/// </summary>
public class EnemyItemDrop : MonoBehaviour
{
    [Header("Drop Settings")]
    [Tooltip("Danh sách item có thể drop")]
    [SerializeField] private List<DropItem> dropItems = new List<DropItem>();
    
    [Tooltip("Prefab của ItemPickup để spawn")]
    [SerializeField] private GameObject itemPickupPrefab;
    
    [Tooltip("Force khi drop item (để item bay ra xa)")]
    [SerializeField] private float dropForce = 3f;
    
    [Tooltip("Random spread khi drop")]
    [SerializeField] private float dropSpread = 1f;

    [Header("Auto Pickup")]
    [Tooltip("Bán kính tìm player gần nhất để tự động nhặt item ngay khi drop")]
    [SerializeField] private float autoPickupRange = 3f;

    private NetworkEnemyHealth enemyHealth;
    private EnemyHealth standaloneEnemyHealth;
    private bool hasDropped = false;

    private void Awake()
    {
        // Tìm EnemyHealth component
        enemyHealth = GetComponent<NetworkEnemyHealth>();
        standaloneEnemyHealth = GetComponent<EnemyHealth>();

        // Subscribe to death events
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath.AddListener(OnEnemyDeath);
        }
        else if (standaloneEnemyHealth != null)
        {
            standaloneEnemyHealth.OnDeath.AddListener(OnEnemyDeath);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe
        if (enemyHealth != null)
        {
            enemyHealth.OnDeath.RemoveListener(OnEnemyDeath);
        }
        if (standaloneEnemyHealth != null)
        {
            standaloneEnemyHealth.OnDeath.RemoveListener(OnEnemyDeath);
        }
    }

    /// <summary>
    /// Callback khi enemy chết
    /// </summary>
    private void OnEnemyDeath()
    {
        if (hasDropped) return;
        hasDropped = true;

        // Chỉ server mới spawn item
        if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer)
        {
            return;
        }

        DropItems();
    }

    /// <summary>
    /// Drop các item theo tỷ lệ
    /// </summary>
    private void DropItems()
    {
        if (dropItems == null || dropItems.Count == 0) return;
        if (itemPickupPrefab == null)
        {
            Debug.LogWarning("[EnemyItemDrop] ItemPickupPrefab chưa được gán!");
            return;
        }

        Vector3 dropPosition = transform.position;

        foreach (var dropItem in dropItems)
        {
            if (dropItem.itemData == null) continue;

            // Random theo drop rate
            float randomValue = Random.Range(0f, 100f);
            if (randomValue > dropItem.dropRate) continue;

            // Random số lượng
            int quantity = Random.Range(dropItem.minQuantity, dropItem.maxQuantity + 1);
            if (quantity <= 0) continue;

            // Spawn item pickup
            SpawnItemPickup(dropItem.itemData, quantity, dropPosition);
        }
    }

    /// <summary>
    /// Spawn ItemPickup tại vị trí
    /// </summary>
    private void SpawnItemPickup(ItemData itemData, int quantity, Vector3 position)
    {
        // Random offset để item không spawn chồng lên nhau
        Vector3 spawnPosition = position + new Vector3(
            Random.Range(-dropSpread, dropSpread),
            Random.Range(0f, dropSpread * 0.5f),
            0f
        );

        GameObject itemObj;
        
        // Spawn network object nếu đang ở network mode
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            itemObj = Instantiate(itemPickupPrefab, spawnPosition, Quaternion.identity);
            NetworkObject networkObject = itemObj.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
        }
        else
        {
            // Standalone mode
            itemObj = Instantiate(itemPickupPrefab, spawnPosition, Quaternion.identity);
        }

        // Set item data
        ItemPickup itemPickup = itemObj.GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.SetItemData(itemData, quantity);
        }

        // Add force để item bay ra
        Rigidbody2D rb = itemObj.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 force = new Vector2(
                Random.Range(-dropForce, dropForce),
                Random.Range(dropForce * 0.5f, dropForce)
            );
            rb.AddForce(force, ForceMode2D.Impulse);
        }

        Debug.Log($"[EnemyItemDrop] Dropped {quantity}x {itemData.itemName} at {spawnPosition}");

        // Tự động nhặt luôn cho player gần nhất (chỉ trên server)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer && itemPickup != null)
        {
            AutoPickupForNearestPlayer(itemPickup, spawnPosition);
        }
    }

    /// <summary>
    /// Tìm player gần nhất quanh vị trí spawn và yêu cầu nhặt item ngay lập tức
    /// </summary>
    private void AutoPickupForNearestPlayer(ItemPickup itemPickup, Vector3 spawnPosition)
    {
        // Tìm tất cả player trong bán kính autoPickupRange
        Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPosition, autoPickupRange);

        float closestDist = float.MaxValue;
        NetworkObject closestPlayerNetObj = null;

        foreach (var col in colliders)
        {
            if (!col.CompareTag("Player")) continue;

            NetworkObject netObj = col.GetComponent<NetworkObject>();
            if (netObj == null) continue;

            float dist = Vector2.Distance(spawnPosition, col.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestPlayerNetObj = netObj;
            }
        }

        if (closestPlayerNetObj != null)
        {
            // Gọi RequestPickup để server cho item vào túi player đó
            itemPickup.RequestPickup(closestPlayerNetObj.NetworkObjectId);
            Debug.Log($"[EnemyItemDrop] Auto-picked {itemPickup.name} for player {closestPlayerNetObj.NetworkObjectId}");
        }
    }

    /// <summary>
    /// Thêm item vào drop list (dùng trong Inspector hoặc code)
    /// </summary>
    public void AddDropItem(ItemData itemData, float dropRate, int minQuantity, int maxQuantity)
    {
        if (dropItems == null)
        {
            dropItems = new List<DropItem>();
        }

        dropItems.Add(new DropItem
        {
            itemData = itemData,
            dropRate = dropRate,
            minQuantity = minQuantity,
            maxQuantity = maxQuantity
        });
    }
}

/// <summary>
/// Struct để định nghĩa item drop
/// </summary>
[System.Serializable]
public class DropItem
{
    [Tooltip("ItemData của item sẽ drop")]
    public ItemData itemData;
    
    [Tooltip("Tỷ lệ drop (0-100%)")]
    [Range(0f, 100f)]
    public float dropRate = 50f;
    
    [Tooltip("Số lượng tối thiểu")]
    public int minQuantity = 1;
    
    [Tooltip("Số lượng tối đa")]
    public int maxQuantity = 3;
}
