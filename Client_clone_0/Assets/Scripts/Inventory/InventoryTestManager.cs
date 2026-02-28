using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// InventoryTestManager - Quản lý test thêm item vào inventory bằng phím Q
/// 
/// Chức năng:
/// - Khi nhấn phím Q, tự động thêm các item có sẵn trong data vào túi
/// - Gửi request lên host để host update lên DB
/// - Hỗ trợ cả Host và Client
/// 
/// Setup:
/// 1. Gắn script này vào GameObject trong scene (có thể cùng với NetworkManager)
/// 2. Gán các item template trong Inspector (các item mẫu để test)
/// 3. Ấn Q trong game để test
/// </summary>
public class InventoryTestManager : MonoBehaviour
{
    [Header("Test Items Configuration")]
    [Tooltip("Danh sách các item mẫu để test - cấu hình trong Inspector")]
    [SerializeField] private List<TestItemData> testItems = new List<TestItemData>
    {
        new TestItemData { itemTemplateId = 1, itemCode = "ITEM_ICON_1", iconId = "client_icon_1", quantity = 5 },
        new TestItemData { itemTemplateId = 2, itemCode = "ITEM_ICON_2", iconId = "client_icon_2", quantity = 3 },
        new TestItemData { itemTemplateId = 3, itemCode = "ITEM_ICON_3", iconId = "client_icon_3", quantity = 10 },
        new TestItemData { itemTemplateId = 4, itemCode = "ITEM_ICON_4", iconId = "client_icon_4", quantity = 1 }
    };

    [Header("Settings")]
    [Tooltip("Phím để test thêm item (mặc định: Q)")]
    [SerializeField] private KeyCode testKey = KeyCode.Q;

    [Tooltip("Có bật chế độ debug log không")]
    [SerializeField] private bool enableDebugLog = true;

    private void Update()
    {
        // Chỉ xử lý nếu đã connect network
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            return;
        }

        // Kiểm tra phím Q được nhấn
        if (Input.GetKeyDown(testKey))
        {
            OnTestKeyPressed();
        }
    }

    /// <summary>
    /// Xử lý khi phím test được nhấn
    /// </summary>
    private void OnTestKeyPressed()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[InventoryTestManager] Phím {testKey} được nhấn - Bắt đầu thêm test items...");
        }

        // Tìm NetworkInventory của local player
        var localPlayer = GetLocalPlayerObject();
        if (localPlayer == null)
        {
            Debug.LogWarning("[InventoryTestManager] Không tìm thấy local player object!");
            return;
        }

        var networkInventory = localPlayer.GetComponent<NetworkInventory>();
        if (networkInventory == null)
        {
            Debug.LogWarning("[InventoryTestManager] Local player không có NetworkInventory component!");
            return;
        }

        // Gọi hàm để thêm test items
        AddTestItemsToInventory(networkInventory);
    }

    /// <summary>
    /// Thêm các test items vào inventory
    /// </summary>
    private void AddTestItemsToInventory(NetworkInventory inventory)
    {
        if (testItems == null || testItems.Count == 0)
        {
            Debug.LogWarning("[InventoryTestManager] testItems rỗng! Vui lòng cấu hình trong Inspector.");
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log($"[InventoryTestManager] Đang thêm {testItems.Count} test items vào inventory...");
        }

        // Gọi ServerRpc để thêm items (không sync DB từng item - tránh race condition!)
        foreach (var testItem in testItems)
        {
            if (testItem == null)
            {
                continue;
            }

            // Gọi ServerRpc để thêm item (KHÔNG sync DB ở đây!)
            inventory.AddItemWithDBSyncServerRpc(
                testItem.itemTemplateId,
                testItem.itemCode,
                testItem.iconId,
                testItem.quantity
            );

            if (enableDebugLog)
            {
                Debug.Log($"[InventoryTestManager] Đã gửi request thêm: {testItem.itemCode} x{testItem.quantity}");
            }
        }

        Debug.Log($"[InventoryTestManager] ✅ Đã gửi {testItems.Count} items lên server để thêm vào inventory!");

        // ✅ SAU KHI THÊM HẾT ITEMS, SYNC TOÀN BỘ VỚI DB (1 LẦN DUY NHẤT)
        // Delay một chút để đảm bảo tất cả items đã được add xong
        StartCoroutine(SyncInventoryToDBAfterDelay(testItems));
    }

    /// <summary>
    /// Lấy GameObject của local player
    /// </summary>
    private GameObject GetLocalPlayerObject()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("[InventoryTestManager] NetworkManager.Singleton is null");
            return null;
        }

        var spawnManager = NetworkManager.Singleton.SpawnManager;
        if (spawnManager == null || spawnManager.SpawnedObjectsList == null)
        {
            Debug.LogWarning("[InventoryTestManager] SpawnManager or SpawnedObjectsList is null");
            return null;
        }

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        
        if (enableDebugLog)
        {
            Debug.Log($"[InventoryTestManager] Tìm local player với LocalClientId={localClientId}, Spawned objects count={spawnManager.SpawnedObjectsList.Count}");
        }

        // Tìm NetworkObject thuộc về local client
        foreach (var netObj in spawnManager.SpawnedObjectsList)
        {
            if (netObj != null)
            {
                // Check cả IsLocalPlayer và OwnerClientId
                if (netObj.IsLocalPlayer || (netObj.IsPlayerObject && netObj.OwnerClientId == localClientId))
                {
                    if (enableDebugLog)
                    {
                        Debug.Log($"[InventoryTestManager] ✅ Tìm thấy local player: {netObj.name}, IsLocalPlayer={netObj.IsLocalPlayer}, OwnerClientId={netObj.OwnerClientId}");
                    }
                    return netObj.gameObject;
                }
            }
        }

        // Fallback: Tìm theo tag "Player"
        var playerObjects = GameObject.FindGameObjectsWithTag("Player");
        foreach (var playerObj in playerObjects)
        {
            var netObj = playerObj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[InventoryTestManager] ✅ Tìm thấy local player qua Tag: {playerObj.name}");
                }
                return playerObj;
            }
        }

        Debug.LogWarning($"[InventoryTestManager] Không tìm thấy local player trong {spawnManager.SpawnedObjectsList.Count} spawned objects");
        return null;
    }

    /// <summary>
    /// Coroutine để sync inventory với DB sau một khoảng delay (tránh race condition)
    /// </summary>
    private System.Collections.IEnumerator SyncInventoryToDBAfterDelay(List<TestItemData> items)
    {
        // Đợi 1 giây để đảm bảo tất cả items đã được add vào NetworkInventory
        yield return new WaitForSeconds(1f);

        Debug.Log("[InventoryTestManager] 🔄 Bắt đầu sync toàn bộ inventory với DB...");

        // ✅ FIX: Lấy playerId từ GameManager (in-memory) thay vì PlayerPrefs
        int playerId = 0;
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            playerId = GameManager.Instance.GetPlayerData().user_id;
        }
        if (playerId == 0)
            playerId = PlayerPrefs.GetInt("USER_ID", 0);
        
        if (playerId == 0)
        {
            Debug.LogWarning("[InventoryTestManager] playerId = 0, không thể sync DB!");
            yield break;
        }

        // Convert test items sang AddInventoryItemRequest array
        var itemRequests = new List<APIClient.AddInventoryItemRequest>();
        foreach (var item in items)
        {
            itemRequests.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = item.itemTemplateId,
                itemCode = item.itemCode,
                iconId = item.iconId,
                quantity = item.quantity
            });
        }

        // Gọi API để sync
        if (APIClient.Instance != null)
        {
            APIClient.Instance.AddItemsToInventory(
                playerId,
                itemRequests.ToArray(),
                (response) =>
                {
                    Debug.Log($"[InventoryTestManager] ✅ Đã sync {itemRequests.Count} items với DB thành công!");
                },
                (error) =>
                {
                    Debug.LogError($"[InventoryTestManager] ❌ Lỗi khi sync DB: {error}");
                }
            );
        }
        else
        {
            Debug.LogWarning("[InventoryTestManager] APIClient.Instance is null!");
        }
    }

    /// <summary>
    /// Thêm test item thủ công (gọi từ Button UI nếu cần)
    /// </summary>
    public void AddSingleTestItem(int index)
    {
        if (testItems == null || index < 0 || index >= testItems.Count)
        {
            Debug.LogWarning($"[InventoryTestManager] Index {index} không hợp lệ!");
            return;
        }

        var localPlayer = GetLocalPlayerObject();
        if (localPlayer == null)
        {
            Debug.LogWarning("[InventoryTestManager] Không tìm thấy local player!");
            return;
        }

        var networkInventory = localPlayer.GetComponent<NetworkInventory>();
        if (networkInventory == null)
        {
            Debug.LogWarning("[InventoryTestManager] Local player không có NetworkInventory!");
            return;
        }

        var testItem = testItems[index];
        networkInventory.AddItemWithDBSyncServerRpc(
            testItem.itemTemplateId,
            testItem.itemCode,
            testItem.iconId,
            testItem.quantity
        );

        Debug.Log($"[InventoryTestManager] Đã thêm item: {testItem.itemCode} x{testItem.quantity}");
    }
}

/// <summary>
/// Data class cho test item
/// </summary>
[System.Serializable]
public class TestItemData
{
    public int itemTemplateId;
    public string itemCode;
    public string iconId;
    public int quantity;
}
