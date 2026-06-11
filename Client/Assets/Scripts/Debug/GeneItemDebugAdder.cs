using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

// GeneItemDebugAdder — Nhấn M để thêm x10 item lỗi đột biến của mỗi hệ (Fire/Water/Earth/Metal/Wood)
// vào túi đồ của player.
// CÁCH SỬ DỤNG:
// 1. Gắn script này lên bất kỳ GameObject debug nào trong scene
// (hoặc cùng chỗ với InventoryTestManager).
// 2. Nhấn M trong game → x10 item gene upgrade của cả 5 hệ được thêm vào túi.
// PHÍM TẮT: M (có thể đổi trong Inspector)
// Gọi: GET /api/gene/config?elementType=X&tier=1 cho mỗi hệ → lấy itemId → add vào inventory
public class GeneItemDebugAdder : MonoBehaviour
{
    [Header("Phím tắt")]
    [SerializeField] private KeyCode hotkey = KeyCode.M;

    [Header("Settings")]
    [Tooltip("Số lượng item thêm mỗi lần nhấn theo mỗi hệ")]
    [SerializeField] private int amountPerElement = 10;

    [Tooltip("Tier gene dùng để lấy config (thường là 1)")]
    [SerializeField] private int geneTier = 1;

    private static readonly string[] ElementTypes = { "Fire", "Water", "Earth", "Metal", "Wood", "Wind" };

    private bool _isBusy;

    private void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked) return;

        if (!Input.GetKeyDown(hotkey) || _isBusy) return;

        // Chỉ chạy khi đã kết nối network
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient) return;

        StartCoroutine(AddGeneItemsCoroutine());
    }

    private IEnumerator AddGeneItemsCoroutine()
    {
        _isBusy = true;
        Debug.Log("[GeneItemDebugAdder] ===== Bắt đầu thêm x10 item đột biến mỗi hệ =====");

        // Tìm NetworkInventory của local player
        var localPlayerObj = GetLocalPlayerObject();
        if (localPlayerObj == null)
        {
            Debug.LogWarning("[GeneItemDebugAdder] Không tìm thấy local player!");
            _isBusy = false;
            yield break;
        }

        var networkInventory = localPlayerObj.GetComponent<NetworkInventory>();
        if (networkInventory == null)
        {
            Debug.LogWarning("[GeneItemDebugAdder] Local player không có NetworkInventory!");
            _isBusy = false;
            yield break;
        }

        if (APIClient.Instance == null && ServerAddressConfig.Instance == null)
        {
            Debug.LogWarning("[GeneItemDebugAdder] ServerAddressConfig chưa sẵn sàng!");
            _isBusy = false;
            yield break;
        }

        // Fetch gene config cho từng hệ và thêm item
        var addedItems = new List<APIClient.AddInventoryItemRequest>();

        string baseUrl = ServerAddressConfig.Instance != null ? ServerAddressConfig.Instance.ApiUrl : "http://localhost:3000/api";

        foreach (var elementType in ElementTypes)
        {
            GeneConfigDto cfg = null;

            // Direct HTTP call instead of APIClient.GetGeneConfig
            IEnumerator fetchConfig()
            {
                string url = $"{baseUrl}/gene/config?elementType={elementType}&tier={geneTier}";
                using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
                string token = APIClient.Instance != null ? APIClient.Instance.GetToken() : PlayerPrefs.GetString("JWT_TOKEN", "");
                if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("Authorization", $"Bearer {token}");
                yield return req.SendWebRequest();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    cfg = JsonUtility.FromJson<GeneConfigDto>(req.downloadHandler.text);
                else
                    Debug.LogWarning($"[GeneItemDebugAdder] Không lấy được config hệ {elementType}: {req.error}");
            }
            yield return StartCoroutine(fetchConfig());

            if (cfg == null || cfg.itemId <= 0)
            {
                Debug.LogWarning($"[GeneItemDebugAdder] Bỏ qua hệ {elementType} — itemId không hợp lệ.");
                continue;
            }

            string itemCode = $"GENE_{elementType.ToUpper()}";
            string iconId   = cfg.itemIcon > 0 ? cfg.itemIcon.ToString() : "0";

            // Thêm vào NetworkVariable (không sync DB từng cái)
            networkInventory.AddItemWithoutDBSyncServerRpc(
                cfg.itemId,
                itemCode,
                iconId,
                amountPerElement
            );

            addedItems.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = cfg.itemId,
                quantity       = amountPerElement
            });

            Debug.Log($"[GeneItemDebugAdder] ✓ Đã thêm x{amountPerElement} {cfg.itemName} (id={cfg.itemId}) — hệ {elementType}");
        }

        Debug.Log($"[GeneItemDebugAdder] ✅ Đã thêm {addedItems.Count}/6 loại item đột biến vào túi!");

        if (addedItems.Count > 0)
        {
            // Đợi một chút để NetworkVariable cập nhật xong trước khi sync DB
            yield return new WaitForSeconds(1f);
            yield return StartCoroutine(SyncToDB(addedItems));
        }

        _isBusy = false;
    }

    private IEnumerator SyncToDB(List<APIClient.AddInventoryItemRequest> items)
    {
        int playerId = 0;
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            playerId = GameManager.Instance.GetPlayerData().user_id;
        if (playerId == 0)
            playerId = PlayerPrefs.GetInt("USER_ID", 0);

        if (playerId == 0)
        {
            Debug.LogWarning("[GeneItemDebugAdder] playerId = 0, không thể sync DB!");
            yield break;
        }

        string baseUrl2 = ServerAddressConfig.Instance != null ? ServerAddressConfig.Instance.ApiUrl : "http://localhost:3000/api";
        string url2 = $"{baseUrl2}/player/{playerId}/inventory/add-items";
        var wrapper = new APIClient.AddInventoryItemsRequest { items = items.ToArray() };
        string body = JsonUtility.ToJson(wrapper);
        byte[] bodyBytes = System.Text.Encoding.UTF8.GetBytes(body);

        bool done2 = false;
        IEnumerator doSync()
        {
            using var req = new UnityEngine.Networking.UnityWebRequest(url2, "POST");
            req.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyBytes);
            req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            string token = APIClient.Instance != null ? APIClient.Instance.GetToken() : PlayerPrefs.GetString("JWT_TOKEN", "");
            if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("Authorization", $"Bearer {token}");
            yield return req.SendWebRequest();
            if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                Debug.Log($"[GeneItemDebugAdder] ✅ Đã sync {items.Count} item gene vào DB!");
            else
                Debug.LogError($"[GeneItemDebugAdder] ❌ Lỗi sync DB: {req.error}");
            done2 = true;
        }
        yield return StartCoroutine(doSync());
        yield return new WaitUntil(() => done2);
    }

    private GameObject GetLocalPlayerObject()
    {
        if (NetworkManager.Singleton?.SpawnManager == null) return null;

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            if (netObj == null) continue;
            if (netObj.IsLocalPlayer || (netObj.IsPlayerObject && netObj.OwnerClientId == localClientId))
                return netObj.gameObject;
        }

        // Fallback theo tag
        foreach (var playerObj in GameObject.FindGameObjectsWithTag("Player"))
        {
            var netObj = playerObj.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
                return playerObj;
        }

        return null;
    }
}
