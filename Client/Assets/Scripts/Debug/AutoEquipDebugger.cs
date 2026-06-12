using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

// AutoEquipDebugger – Nhấn Q để thêm đủ 6 món trang bị (type 0-5) vào túi đồ.
// Tự động tìm item đầu tiên của mỗi loại từ ItemTemplateManager cache – không phụ thuộc vào ID cứng.
// Setup: Gắn script này lên GameObject bất kỳ trong Game Scene (vd: DebugManager).
public class AutoEquipDebugger : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Phím bấm để thêm 6 món vào túi đồ")]
    [SerializeField] private KeyCode triggerKey = KeyCode.Q;

    [Tooltip("Phím bấm để thêm đá nâng cấp vào túi đồ")]
    [SerializeField] private KeyCode stoneKey = KeyCode.T;

    [Tooltip("Phím bấm để thêm Linh Thạch gene (id 17-20) vào túi đồ")]
    [SerializeField] private KeyCode geneStoneKey = KeyCode.Y;

    [Tooltip("Số Linh Thạch mỗi loại thêm vào túi khi nhấn Y")]
    [SerializeField] private int geneStoneCount = 10;

    [Tooltip("Số lượng mỗi item thêm vào túi")]
    [SerializeField] private int quantityPerItem = 1;

    [Tooltip("Số viên đá nâng cấp (id=7) thêm vào túi")]
    [SerializeField] private int upgradeStoneCount = 10;

    [Tooltip("Số viên đá may mắn (id=8) thêm vào túi")]
    [SerializeField] private int luckyStoneCount = 5;

    [Tooltip("Số viên đá bảo vệ (id=9) thêm vào túi")]
    [SerializeField] private int protectionStoneCount = 3;

    private bool isBusy = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapRuntimeDebugger()
    {
        if (FindObjectOfType<AutoEquipDebugger>() != null)
            return;

        var runtimeObject = new GameObject("AutoEquipDebugger_Runtime");
        runtimeObject.AddComponent<AutoEquipDebugger>();
        DontDestroyOnLoad(runtimeObject);
        { /* Runtime bootstrap created automatically */ }
    }

    private void Update()
    {
        if (InputManager.Instance != null && InputManager.Instance.IsGameplayInputBlocked) return;

        if (Input.GetKeyDown(triggerKey) && !isBusy)
            StartCoroutine(AddAndEquipAll());

        if (Input.GetKeyDown(stoneKey) && !isBusy)
            StartCoroutine(AddStonesToInventory());

        if (Input.GetKeyDown(geneStoneKey) && !isBusy)
            StartCoroutine(AddGeneStonesToInventory());
    }

    //  Phím T: Thêm đá nâng cấp vào túi đồ
    private IEnumerator AddStonesToInventory()
    {
        isBusy = true;
        { /* ===== THÊM ĐÁ NÂNG CẤP =====\n */ }

        int playerId = GetPlayerId();
        if (playerId == 0)
        {
            { /* Lỗi: Không lấy được playerId */ }
            isBusy = false;
            yield break;
        }

        int upgradeStoneItemId = FindFirstUpgradeStoneTemplateId();

        var stones = new List<APIClient.AddInventoryItemRequest>();
        if (upgradeStoneCount > 0)
        {
            if (upgradeStoneItemId <= 0)
            {
                { /* Lỗi: Không tìm thấy item đá nâng cấp type=21 trong ItemTemplateManager */ }
                isBusy = false;
                yield break;
            }

            stones.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = upgradeStoneItemId,
                quantity       = upgradeStoneCount
            });
        }
        if (luckyStoneCount > 0)
            stones.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = 8,
                quantity       = luckyStoneCount
            });
        if (protectionStoneCount > 0)
            stones.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = 9,
                quantity       = protectionStoneCount
            });

        if (stones.Count == 0)
        {
            { /* Cảnh báo: Tất cả count đều = 0, không thêm gì */ }
            isBusy = false;
            yield break;
        }

        bool done = false;
        yield return StartCoroutine(AddItemsToInventoryDirect(playerId, stones.ToArray(),
            _ => { { /* Đã thêm {stones.Count} loại đá vào túi */ } done = true; },
            (err) => { { /* Lỗi: Thêm đá thất bại: {err} */ } done = true; }
        ));
        yield return new WaitUntil(() => done);

        // Refresh UI
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        bridge?.RefreshInventoryFromDB();

        { /* ===== XONG =====\nNhấn T lần nữa để thêm tiếp */ }
        isBusy = false;
    }

    private int FindFirstUpgradeStoneTemplateId()
    {
        var allTemplates = ItemTemplateManager.Instance?.GetAllItemTemplates();
        if (allTemplates == null)
            return 0;

        var stoneTemplate = allTemplates
            .Where(t => t != null && t.type == UpgradePanel.STONE_ITEM_TYPE && t.id != UpgradePanel.CHARM_ITEM_ID)
            .OrderBy(t => t.levelNeed)
            .ThenBy(t => t.id)
            .FirstOrDefault();

        return stoneTemplate != null ? stoneTemplate.id : 0;
    }

    //  Phím Y: Thêm Linh Thạch gene (id 17-20) vào túi đồ
    private IEnumerator AddGeneStonesToInventory()
    {
        isBusy = true;
        { /* ===== THÊM LINH THẠCH GENE =====\n */ }

        int playerId = GetPlayerId();
        if (playerId == 0)
        {
            { /* Lỗi: Không lấy được playerId */ }
            isBusy = false;
            yield break;
        }

        var stones = new APIClient.AddInventoryItemRequest[]
        {
            new APIClient.AddInventoryItemRequest { itemTemplateId = 17, quantity = geneStoneCount },
            new APIClient.AddInventoryItemRequest { itemTemplateId = 18, quantity = geneStoneCount },
            new APIClient.AddInventoryItemRequest { itemTemplateId = 19, quantity = geneStoneCount },
            new APIClient.AddInventoryItemRequest { itemTemplateId = 20, quantity = geneStoneCount },
        };

        bool done = false;
        yield return StartCoroutine(AddItemsToInventoryDirect(playerId, stones,
            (_) => { { /* Đã thêm 4 loại Linh Thạch x{geneStoneCount} vào túi */ } done = true; },
            (err) => { { /* Lỗi: Thêm Linh Thạch thất bại: {err} */ } done = true; }
        ));
        yield return new WaitUntil(() => done);

        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        bridge?.RefreshInventoryFromDB();

        { /* ===== XONG =====\nNhấn Y lần nữa để thêm tiếp */ }
        isBusy = false;
    }

    private IEnumerator AddAndEquipAll()
    {
        isBusy = true;
        { /* ===== THÊM ITEM VÀO TÚI ĐỒ ===== */ }

        // 1. Lấy playerId
        int playerId = GetPlayerId();
        if (playerId == 0)
        {
            { /* Lỗi: Không lấy được playerId! Chưa đăng nhập? */ }
            isBusy = false;
            yield break;
        }
        { /* playerId = {playerId} */ }

        // 2. Đợi ItemTemplateManager load xong
        float timeout = 8f, elapsed = 0f;
        while (ItemTemplateManager.Instance == null || !ItemTemplateManager.Instance.IsLoaded())
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                { /* Lỗi: ItemTemplateManager chưa load xong sau 8s */ }
                isBusy = false;
                yield break;
            }
            yield return null;
        }

        // 3. Tìm item đầu tiên của mỗi loại trang bị (type 0-5)
        // type: 0=Helmet, 1=Weapon, 2=Armor, 3=Pants, 4=Boots, 5=Ring
        int[] equipTypes = { 0, 1, 2, 3, 4, 5 };
        var toAdd = new List<APIClient.AddInventoryItemRequest>();

        var allTemplates = ItemTemplateManager.Instance.GetAllItemTemplates();

        foreach (int equipType in equipTypes)
        {
            var tmpl = allTemplates
                .Where(t => t.type == equipType)
                .OrderBy(t => t.levelNeed)
                .ThenBy(t => t.id)
                .FirstOrDefault();

            if (tmpl == null)
            {
                { /* Cảnh báo: Không tìm thấy item nào cho type={equipType} */ }
                continue;
            }

            toAdd.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = tmpl.id,
                quantity       = quantityPerItem
            });
            { /* Sẽ thêm: [{tmpl.name}] id={tmpl.id} type={equipType} */ }
        }

        if (toAdd.Count == 0)
        {
            { /* Lỗi: Không có item nào để thêm */ }
            isBusy = false;
            yield break;
        }

        // 4. Xóa inventory cũ trước
        bool clearDone = false;
        yield return StartCoroutine(ClearInventoryDirect(playerId,
            () => { clearDone = true; },
            (err) => { { /* Cảnh báo: Clear inventory warning: {err} */ } clearDone = true; }
        ));
        yield return new WaitUntil(() => clearDone);
        { /* Đã clear inventory cũ */ }

        // 5. Gọi API thêm vào inventory
        bool addDone = false, addSuccess = false;
        yield return StartCoroutine(AddItemsToInventoryDirect(playerId, toAdd.ToArray(),
            (_) => { addSuccess = true; addDone = true; },
            (err) => { { /* Lỗi: Thêm item thất bại: {err} */ } addDone = true; }
        ));
        yield return new WaitUntil(() => addDone);
        if (!addSuccess) { isBusy = false; yield break; }
        { /* Đã thêm {toAdd.Count} item vào túi đồ */ }

        // 6. Refresh UI
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge != null)
        {
            bridge.RefreshInventoryFromDB();
            { /* UI túi đồ đã refresh */ }
        }

        { /* ===== HOÀN THÀNH ===== */ }
        isBusy = false;
    }

    private int GetPlayerId()
    {
        int playerId = 0;

        // Ưu tiên 1: GameManager in-memory (user_id được set từ login hoặc server response)
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
        {
            var pd = GameManager.Instance.GetPlayerData();
            // user_id được set khi login; player_id được set khi load player data
            playerId = pd.user_id != 0 ? pd.user_id : pd.player_id;
        }

        // Ưu tiên 2: ServerPlayerDataManager – dùng clientIdToUserId (chính xác hơn playerData.user_id)
        if (playerId == 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var serverDataMgr = ServerPlayerDataManager.Instance;
            if (serverDataMgr != null)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                // GetUserIdFromClientId trả về userId đã được lưu từ login, KHÔNG phụ thuộc
                // vào việc server có trả về user_id trong response hay không
                int userIdFromCache = serverDataMgr.GetUserIdFromClientId(localClientId);
                if (userIdFromCache != 0)
                    playerId = userIdFromCache;
            }
        }

        // Fallback: PlayerPrefs (set tại login)
        if (playerId == 0)
            playerId = PlayerPrefs.GetInt("USER_ID", 0);

        if (playerId == 0)
            { /* Cảnh báo: playerId = 0! GameManager, ServerPlayerDataManager, và PlayerPrefs đều không có dữ liệu */ }

        return playerId;
    }

    private IEnumerator AddItemsToInventoryDirect(int playerId, APIClient.AddInventoryItemRequest[] items,
        System.Action<string> onSuccess, System.Action<string> onError)
    {
        string baseUrl = ServerAddressConfig.Instance != null ? ServerAddressConfig.Instance.ApiUrl : "http://localhost:3000/api";
        string url = $"{baseUrl}/player/{playerId}/inventory/add-items";
        var wrapper = new APIClient.AddInventoryItemsRequest { items = items };
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
            onSuccess?.Invoke(req.downloadHandler.text);
        else
            onError?.Invoke(req.error);
    }

    private IEnumerator ClearInventoryDirect(int playerId,
        System.Action onSuccess, System.Action<string> onError)
    {
        string baseUrl = ServerAddressConfig.Instance != null ? ServerAddressConfig.Instance.ApiUrl : "http://localhost:3000/api";
        string url = $"{baseUrl}/player/{playerId}/inventory/clear";
        using var req = UnityEngine.Networking.UnityWebRequest.Delete(url);
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        string token = APIClient.Instance != null ? APIClient.Instance.GetToken() : AuthHelper.GetToken();
        if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("Authorization", $"Bearer {token}");
        yield return req.SendWebRequest();
        if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            onSuccess?.Invoke();
        else
            onError?.Invoke(req.error);
    }
}
