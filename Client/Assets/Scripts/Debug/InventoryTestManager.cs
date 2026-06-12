using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// InventoryTestManager - Quản lý test thêm item vào inventory bằng phím Q
// Chức năng:
// - Khi nhấn phím Q, tự động thêm các item có sẵn trong data vào túi
// - Gửi request lên host để host update lên DB
// - Hỗ trợ cả Host và Client
// Setup:
// 1. Gắn script này vào GameObject trong scene (có thể cùng với NetworkManager)
// 2. Gán các item template trong Inspector (các item mẫu để test)
// 3. Ấn Q trong game để test
public class InventoryTestManager : MonoBehaviour
{
    [Header("Test Items Configuration")]
    [Tooltip("Danh sách các item mẫu để test - cấu hình trong Inspector")]
    [SerializeField] private List<TestItemData> testItems = new List<TestItemData>
    {
        // === Equipment Items (category=1) ===
        new TestItemData { itemTemplateId = 1,  itemCode = "SWORD_001",      iconId = "client_icon_8",  quantity = 1 }, // Weapon - Iron Sword
        new TestItemData { itemTemplateId = 11, itemCode = "HELMET_IRON",    iconId = "client_icon_10", quantity = 1 }, // Helmet - Iron Helmet
        new TestItemData { itemTemplateId = 12, itemCode = "ARMOR_IRON",     iconId = "client_icon_11", quantity = 1 }, // Armor - Iron Armor
        new TestItemData { itemTemplateId = 13, itemCode = "PANTS_IRON",     iconId = "client_icon_12", quantity = 1 }, // Pants - Iron Pants
        new TestItemData { itemTemplateId = 14, itemCode = "BOOTS_IRON",     iconId = "client_icon_13", quantity = 1 }, // Boots - Iron Boots
        new TestItemData { itemTemplateId = 15, itemCode = "ACCESSORY_IRON", iconId = "client_icon_14", quantity = 1 }, // Accessory - Iron Accessory
    };

    [Header("Settings")]
    [Tooltip("Phím để test thêm item (mặc định: Q)")]
    [SerializeField] private KeyCode testKey = KeyCode.Q;

    [Tooltip("Có bật chế độ debug log không")]
    [SerializeField] private bool enableDebugLog = true;

    private void Awake()
    {
        // Force gán lại test items đúng với DB (tránh Unity dùng giá trị cũ đã serialize trong Inspector)
        testItems = new List<TestItemData>
        {
            new TestItemData { itemTemplateId = 1,  itemCode = "SWORD_001",      iconId = "client_icon_8",  quantity = 1 },
            new TestItemData { itemTemplateId = 11, itemCode = "HELMET_IRON",    iconId = "client_icon_10", quantity = 1 },
            new TestItemData { itemTemplateId = 12, itemCode = "ARMOR_IRON",     iconId = "client_icon_11", quantity = 1 },
            new TestItemData { itemTemplateId = 13, itemCode = "PANTS_IRON",     iconId = "client_icon_12", quantity = 1 },
            new TestItemData { itemTemplateId = 14, itemCode = "BOOTS_IRON",     iconId = "client_icon_13", quantity = 1 },
            new TestItemData { itemTemplateId = 15, itemCode = "ACCESSORY_IRON", iconId = "client_icon_14", quantity = 1 },
        };
    }

    private void Update()
    {
        // Chỉ xử lý nếu đã connect network
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            return;
        }

        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked) return;

        // Kiểm tra phím Q được nhấn
        if (Input.GetKeyDown(testKey))
        {
            OnTestKeyPressed();
        }
    }

    // Xử lý khi phím test được nhấn
    private void OnTestKeyPressed()
    {
        if (enableDebugLog)
        {
            { /* Phím {testKey} được nhấn - Bắt đầu thêm test items */ }
        }

        // Tìm NetworkInventory của local player
        var localPlayer = GetLocalPlayerObject();
        if (localPlayer == null)
        {
            { /* Cảnh báo: Không tìm thấy local player object */ }
            return;
        }

        var networkInventory = localPlayer.GetComponent<NetworkInventory>();
        if (networkInventory == null)
        {
            { /* Cảnh báo: Local player không có NetworkInventory component */ }
            return;
        }

        // Gọi hàm để thêm test items
        AddTestItemsToInventory(networkInventory);
    }

    // Thêm các test items vào inventory
    private void AddTestItemsToInventory(NetworkInventory inventory)
    {
        if (testItems == null || testItems.Count == 0)
        {
            { /* Cảnh báo: testItems rỗng! Vui lòng cấu hình trong Inspector */ }
            return;
        }

        if (enableDebugLog)
        {
            { /* Đang thêm {testItems.Count} test items vào inventory */ }
        }

        // ✅ Bước 1: Thêm tất cả items vào NetworkVariable (KHÔNG sync DB từng item)
        foreach (var testItem in testItems)
        {
            if (testItem == null)
            {
                continue;
            }

            inventory.AddItemWithoutDBSyncServerRpc(
                testItem.itemTemplateId,
                testItem.itemCode,
                testItem.iconId,
                testItem.quantity
            );

            if (enableDebugLog)
            {
                { /* Đã gửi request thêm: {testItem.itemCode} x{testItem.quantity} */ }
            }
        }

        { /* Đã gửi {testItems.Count} items lên server */ }

        // ✅ Bước 2: Sync TẤT CẢ items với DB trong 1 request duy nhất (tránh race condition)
        StartCoroutine(SyncInventoryToDBAfterDelay(testItems));
    }

    // Lấy GameObject của local player
    private GameObject GetLocalPlayerObject()
    {
        if (NetworkManager.Singleton == null)
        {
            { /* Cảnh báo: NetworkManager.Singleton is null */ }
            return null;
        }

        var spawnManager = NetworkManager.Singleton.SpawnManager;
        if (spawnManager == null || spawnManager.SpawnedObjectsList == null)
        {
            { /* Cảnh báo: SpawnManager or SpawnedObjectsList is null */ }
            return null;
        }

        ulong localClientId = NetworkManager.Singleton.LocalClientId;
        
        if (enableDebugLog)
        {
            { /* Tìm local player với LocalClientId={localClientId}, Spawned objects count={spawnManager.SpawnedObjectsList.Count} */ }
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
                        { /* Tìm thấy local player: {netObj.name}, IsLocalPlayer={netObj.IsLocalPlayer}, OwnerClientId={netObj.OwnerClientId} */ }
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
                    { /* Tìm thấy local player qua Tag: {playerObj.name} */ }
                }
                return playerObj;
            }
        }

        { /* Cảnh báo: Không tìm thấy local player trong {spawnManager.SpawnedObjectsList.Count} spawned objects */ }
        return null;
    }

    // Coroutine để sync inventory với DB sau một khoảng delay (tránh race condition)
    private System.Collections.IEnumerator SyncInventoryToDBAfterDelay(List<TestItemData> items)
    {
        // Đợi 1 giây để đảm bảo tất cả items đã được add vào NetworkInventory
        yield return new WaitForSeconds(1f);

        { /* 🔄 Bắt đầu sync toàn bộ inventory với DB */ }

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
            { /* Cảnh báo: playerId = 0, không thể sync DB */ }
            yield break;
        }

        // Convert test items sang AddInventoryItemRequest array
        var itemRequests = new List<APIClient.AddInventoryItemRequest>();
        foreach (var item in items)
        {
            itemRequests.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = item.itemTemplateId,
                quantity = item.quantity
            });
        }

        // Gọi API để sync
        string baseUrl = ServerAddressConfig.Instance != null ? ServerAddressConfig.Instance.ApiUrl : "http://localhost:3000/api";
        string url = $"{baseUrl}/player/{playerId}/inventory/add-items";
        var wrapper = new APIClient.AddInventoryItemsRequest { items = itemRequests.ToArray() };
        string body = JsonUtility.ToJson(wrapper);
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);
        using var req = new UnityEngine.Networking.UnityWebRequest(url, "POST");
        req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyBytes);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        string token = APIClient.Instance != null ? APIClient.Instance.GetToken() : AuthHelper.GetToken();
        if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("Authorization", $"Bearer {token}");
        yield return req.SendWebRequest();
        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            { /* \u2705 \u0110\u00e3 sync {itemRequests.Count} items v\u1edbi DB th\u00e0nh c\u00f4ng */ }
        else
            { /* Lỗi: \u274c L\u1ed7i khi sync DB: {req.error} */ }
    }

    // Thêm test item thủ công (gọi từ Button UI nếu cần)
    public void AddSingleTestItem(int index)
    {
        if (testItems == null || index < 0 || index >= testItems.Count)
        {
            { /* Cảnh báo: Index {index} không hợp lệ */ }
            return;
        }

        var localPlayer = GetLocalPlayerObject();
        if (localPlayer == null)
        {
            { /* Cảnh báo: Không tìm thấy local player */ }
            return;
        }

        var networkInventory = localPlayer.GetComponent<NetworkInventory>();
        if (networkInventory == null)
        {
            { /* Cảnh báo: Local player không có NetworkInventory */ }
            return;
        }

        var testItem = testItems[index];
        networkInventory.AddItemWithDBSyncServerRpc(
            testItem.itemTemplateId,
            testItem.itemCode,
            testItem.iconId,
            testItem.quantity
        );

        { /* Đã thêm item: {testItem.itemCode} x{testItem.quantity} */ }
    }
}

// Data class cho test item
[System.Serializable]
public class TestItemData
{
    public int itemTemplateId;
    public string itemCode;
    public string iconId;
    public int quantity;
}
