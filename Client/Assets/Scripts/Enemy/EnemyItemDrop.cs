using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// EnemyItemDrop - Component để drop item khi enemy chết
// Gắn vào enemy GameObject
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

    // Callback khi enemy chết
    private void OnEnemyDeath()
    {
        HandleDeathDrop();
    }

    // Dedicated server không nhận ClientRpc như host/client, nên NetworkEnemyHealth
    // gọi trực tiếp method này để đảm bảo nhánh drop luôn chạy trên server.
    public void HandleDeathDrop()
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

    // Drop các item theo tỷ lệ
    private void DropItems()
    {
        if (dropItems == null || dropItems.Count == 0)
        {
            { /* Cảnh báo: {gameObject.name}: DropItems()  dropItems rỗng! Kiểm tra SetDropsFromConfig hoặc Inspector */ }
            return;
        }
        if (itemPickupPrefab == null)
        {
            { /* Cảnh báo: ItemPickupPrefab chưa được gán */ }
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
            { /* item_id={dropItem.itemId} rate={dropItem.dropRate:F1}% roll={roll:F1} → {(passed ? */ }
            if (!passed) continue;

            // Random số lượng trong khoảng qty_min ~ qty_max
            int quantity = Random.Range(dropItem.minQuantity, dropItem.maxQuantity + 1);
            if (quantity <= 0) continue;

            // Spawn item pickup
            SpawnItemPickup(dropItem.itemId, quantity, dropPosition);
            droppedCount++;
        }

        { /* {gameObject.name}: Dropped {droppedCount}/{dropItems.Count} entries (0 = tất cả miss rate check, bình thường) */ }
    }

    // Spawn ItemPickup tại vị trí, dùng item_id trực tiếp (không cần ItemData ScriptableObject).
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

        // Spawn network object TRƯỚC, sau đó mới set data
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            // Kế thừa zone tag từ enemy → item chỉ visible cho player cùng map
            var enemyZoneTag = GetComponent<ZoneOwnerTag>();
            if (enemyZoneTag != null)
            {
                MapSceneManager.Instance?.MoveToMapScene(itemObj, enemyZoneTag.MapId);

                var itemZoneTag = itemObj.GetComponent<ZoneOwnerTag>() ?? itemObj.AddComponent<ZoneOwnerTag>();
                itemZoneTag.SetZone(enemyZoneTag.MapId, enemyZoneTag.ZoneId);

                var filter = itemObj.GetComponent<NetworkVisibilityZoneFilter>() ?? itemObj.AddComponent<NetworkVisibilityZoneFilter>();
                filter.InitializeForServer();

                { /* Move dropped item item_id={itemId} vào mapId={enemyZoneTag.MapId}, zoneId={enemyZoneTag.ZoneId} */ }
            }

            NetworkObject networkObject = itemObj.GetComponent<NetworkObject>();
            if (networkObject != null)
            {
                networkObject.Spawn();
                itemObj.GetComponent<NetworkVisibilityZoneFilter>()?.RefreshVisibility();
            }
        }

        // Set item data SAU khi Spawn() — tránh warning "NetworkVariable written before spawn"
        ItemPickup itemPickup = itemObj.GetComponent<ItemPickup>();
        if (itemPickup != null)
        {
            itemPickup.SetItemId(itemId, quantity);
        }

        // Không dùng AddForce — item ở nguyên vị trí spawn (gravityScale=0)
        // Nếu muốn item rơi xuống ground: bật gravityScale=1 trong ItemPickup prefab
        // và đảm bảo ground có Collider2D không phải trigger.

        { /* Dropped {quantity}x item_id={itemId} at {spawnPosition} */ }
    }

    // Thêm item vào drop list (dùng trong Inspector hoặc code)
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

    // Ghi đè toàn bộ drop list bằng dữ liệu từ DB config (gọi bởi HostSpawnConfigLoader).
    // Không cần ItemData ScriptableObject — lưu item_id trực tiếp.
    // Tham số configItems: Danh sách DropItemEntry đã được validate bởi HostSpawnConfigLoader.
    public void SetDropsFromConfig(System.Collections.Generic.List<DropItemEntry> configItems)
    {
        if (configItems == null || configItems.Count == 0)
        {
            { /* Cảnh báo: {gameObject.name}: SetDropsFromConfig nhận null/empty  enemy này không có drop config trong DB */ }
            return;
        }

        var newList = new List<DropItem>();
        foreach (var entry in configItems)
        {
            if (entry.item_id <= 0)
            {
                { /* Cảnh báo: SetDropsFromConfig: item_id={entry.item_id} không hợp lệ → bỏ qua */ }
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
            { /* {gameObject.name}: SetDropsFromConfig: đã cập nhật {newList.Count} drop rules từ DB */ }
        }
        else
        {
            { /* Cảnh báo: {gameObject.name}: SetDropsFromConfig: không có item_id hợp lệ nào trong config */ }
        }
    }
}

// Struct để định nghĩa item drop — dùng itemId thay vì ItemData ScriptableObject
// để hoạt động với mọi item trong DB mà không cần tạo asset thủ công.
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
