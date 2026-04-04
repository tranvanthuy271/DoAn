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
#pragma warning disable CS0414
    [SerializeField] private float dropForce = 3f;
#pragma warning restore CS0414
    
    [Tooltip("Random spread khi drop")]
    [SerializeField] private float dropSpread = 1f;

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
        if (dropItems == null || dropItems.Count == 0)
        {
            Debug.LogWarning($"[EnemyItemDrop] {gameObject.name}: DropItems() — dropItems rỗng! Kiểm tra SetDropsFromConfig hoặc Inspector.");
            return;
        }
        if (itemPickupPrefab == null)
        {
            Debug.LogWarning("[EnemyItemDrop] ItemPickupPrefab chưa được gán!");
            return;
        }

        Vector3 dropPosition = transform.position;
        int droppedCount = 0;

        foreach (var dropItem in dropItems)
        {
            if (dropItem.itemId <= 0) continue;

            // Random theo drop rate (dropRate đã là 0–100)
            float roll = Random.Range(0f, 100f);
            bool passed = roll <= dropItem.dropRate;
            Debug.Log($"[EnemyItemDrop] item_id={dropItem.itemId} rate={dropItem.dropRate:F1}% roll={roll:F1} → {(passed ? "DROP" : "miss")}");
            if (!passed) continue;

            // Random số lượng trong khoảng qty_min ~ qty_max
            int quantity = Random.Range(dropItem.minQuantity, dropItem.maxQuantity + 1);
            if (quantity <= 0) continue;

            // Spawn item pickup
            SpawnItemPickup(dropItem.itemId, quantity, dropPosition);
            droppedCount++;
        }

        Debug.Log($"[EnemyItemDrop] {gameObject.name}: Dropped {droppedCount}/{dropItems.Count} entries (0 = tất cả miss rate check, bình thường).");
    }

    /// <summary>
    /// Spawn ItemPickup tại vị trí, dùng item_id trực tiếp (không cần ItemData ScriptableObject).
    /// </summary>
    private void SpawnItemPickup(int itemId, int quantity, Vector3 position)
    {
        // Random offset để item không spawn chồng lên nhau
        Vector3 spawnPosition = position + new Vector3(
            Random.Range(-dropSpread, dropSpread),
            Random.Range(0f, dropSpread * 0.5f),
            0f
        );

        // Instantiate trước
        GameObject itemObj = Instantiate(itemPickupPrefab, spawnPosition, Quaternion.identity);

        // QUAN TRỌNG: Set item data TRƯỚC khi Spawn() để initial spawn packet gửi đến client
        // đã có networkItemId đúng — tránh client nhận id=0 rồi mới nhận delta update.
        ItemPickup itemPickup = itemObj.GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.SetItemId(itemId, quantity);
        }

        // Spawn network object sau khi data đã được set
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            NetworkObject networkObject = itemObj.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
            }
        }

        // Không dùng AddForce — item ở nguyên vị trí spawn (gravityScale=0)
        // Nếu muốn item rơi xuống ground: bật gravityScale=1 trong ItemPickup prefab
        // và đảm bảo ground có Collider2D không phải trigger.

        Debug.Log($"[EnemyItemDrop] Dropped {quantity}x item_id={itemId} at {spawnPosition}");
    }

    /// <summary>
    /// Thêm item vào drop list (dùng trong Inspector hoặc code)
    /// </summary>
    public void AddDropItem(int itemId, float dropRate, int minQuantity, int maxQuantity)
    {
        if (dropItems == null)
            dropItems = new List<DropItem>();

        dropItems.Add(new DropItem
        {
            itemId      = itemId,
            dropRate    = dropRate,
            minQuantity = minQuantity,
            maxQuantity = maxQuantity
        });
    }

    /// <summary>
    /// Ghi đè toàn bộ drop list bằng dữ liệu từ DB config (gọi bởi HostSpawnConfigLoader).
    /// Không cần ItemData ScriptableObject — lưu item_id trực tiếp.
    /// </summary>
    /// <param name="configItems">Danh sách DropItemEntry đã được validate bởi HostSpawnConfigLoader.</param>
    public void SetDropsFromConfig(System.Collections.Generic.List<DropItemEntry> configItems)
    {
        if (configItems == null || configItems.Count == 0)
        {
            Debug.LogWarning($"[EnemyItemDrop] {gameObject.name}: SetDropsFromConfig nhận null/empty — enemy này không có drop config trong DB!");
            return;
        }

        var newList = new List<DropItem>();
        foreach (var entry in configItems)
        {
            if (entry.item_id <= 0)
            {
                Debug.LogWarning($"[EnemyItemDrop] SetDropsFromConfig: item_id={entry.item_id} không hợp lệ → bỏ qua.");
                continue;
            }

            newList.Add(new DropItem
            {
                itemId      = entry.item_id,
                dropRate    = entry.rate * 100f,   // chuyển từ 0–1 sang 0–100%
                minQuantity = entry.qty_min,
                maxQuantity = entry.qty_max
            });
        }

        if (newList.Count > 0)
        {
            dropItems = newList;
            Debug.Log($"[EnemyItemDrop] {gameObject.name}: SetDropsFromConfig: đã cập nhật {newList.Count} drop rules từ DB.");
        }
        else
        {
            Debug.LogWarning($"[EnemyItemDrop] {gameObject.name}: SetDropsFromConfig: không có item_id hợp lệ nào trong config!");
        }
    }
}

/// <summary>
/// Struct để định nghĩa item drop — dùng itemId thay vì ItemData ScriptableObject
/// để hoạt động với mọi item trong DB mà không cần tạo asset thủ công.
/// </summary>
[System.Serializable]
public class DropItem
{
    [Tooltip("ID của item (item_template.id trong DB)")]
    public int itemId;
    
    [Tooltip("Tỷ lệ drop (0-100%)")]
    [Range(0f, 100f)]
    public float dropRate = 50f;
    
    [Tooltip("Số lượng tối thiểu")]
    public int minQuantity = 1;
    
    [Tooltip("Số lượng tối đa")]
    public int maxQuantity = 3;
}
