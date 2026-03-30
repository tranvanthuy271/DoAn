using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

/// <summary>
/// AutoEquipDebugger – Nhấn Q để thêm đủ 6 món trang bị (type 0-5) vào túi đồ.
/// Tự động tìm item đầu tiên của mỗi loại từ ItemTemplateManager cache – không phụ thuộc vào ID cứng.
/// Setup: Gắn script này lên GameObject bất kỳ trong Game Scene (vd: DebugManager).
/// </summary>
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

    private void Update()
    {
        if (Input.GetKeyDown(triggerKey) && !isBusy)
            StartCoroutine(AddAndEquipAll());

        if (Input.GetKeyDown(stoneKey) && !isBusy)
            StartCoroutine(AddStonesToInventory());

        if (Input.GetKeyDown(geneStoneKey) && !isBusy)
            StartCoroutine(AddGeneStonesToInventory());
    }

    // ──────────────────────────────────────────────────────────────
    //  Phím T: Thêm đá nâng cấp vào túi đồ
    // ──────────────────────────────────────────────────────────────
    private IEnumerator AddStonesToInventory()
    {
        isBusy = true;
        Debug.Log("[AutoEquipDebugger] ===== THÊM ĐÁ NÂNG CẤP =====\n" +
                  $"  Đá nâng cấp (id=7): x{upgradeStoneCount}\n" +
                  $"  Đá may mắn   (id=8): x{luckyStoneCount}\n" +
                  $"  Đá bảo vệ   (id=9): x{protectionStoneCount}");

        int playerId = GetPlayerId();
        if (playerId == 0)
        {
            Debug.LogError("[AutoEquipDebugger] Không lấy được playerId!");
            isBusy = false;
            yield break;
        }

        // Lookup icon từ ItemTemplateManager (nếu có)
        string GetIcon(int itemId)
        {
            var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(itemId);
            return tmpl != null ? tmpl.idIcon.ToString() : itemId.ToString();
        }

        var stones = new List<APIClient.AddInventoryItemRequest>();
        if (upgradeStoneCount > 0)
            stones.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = 1,
                quantity       = upgradeStoneCount
            });
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
            Debug.LogWarning("[AutoEquipDebugger] Tất cả count đều = 0, không thêm gì.");
            isBusy = false;
            yield break;
        }

        bool done = false;
        APIClient.Instance.AddItemsToInventory(
            playerId,
            stones.ToArray(),
            (_)   => { Debug.Log($"[AutoEquipDebugger] ✅ Đã thêm {stones.Count} loại đá vào túi!"); done = true; },
            (err) => { Debug.LogError($"[AutoEquipDebugger] Thêm đá thất bại: {err}"); done = true; }
        );
        yield return new WaitUntil(() => done);

        // Refresh UI
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        bridge?.RefreshInventoryFromDB();

        Debug.Log("[AutoEquipDebugger] ===== XONG =====\nNhấn T lần nữa để thêm tiếp.");
        isBusy = false;
    }

    // ──────────────────────────────────────────────────────────────
    //  Phím Y: Thêm Linh Thạch gene (id 17-20) vào túi đồ
    // ──────────────────────────────────────────────────────────────
    private IEnumerator AddGeneStonesToInventory()
    {
        isBusy = true;
        Debug.Log($"[AutoEquipDebugger] ===== THÊM LINH THẠCH GENE =====\n" +
                  $"  Linh Thạch Sơ Cấp    (id=17): x{geneStoneCount}\n" +
                  $"  Linh Thạch Trung Cấp (id=18): x{geneStoneCount}\n" +
                  $"  Linh Thạch Cao Cấp   (id=19): x{geneStoneCount}\n" +
                  $"  Linh Thạch Thượng Cấp(id=20): x{geneStoneCount}");

        int playerId = GetPlayerId();
        if (playerId == 0)
        {
            Debug.LogError("[AutoEquipDebugger] Không lấy được playerId!");
            isBusy = false;
            yield break;
        }

        string GetIcon(int itemId)
        {
            var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(itemId);
            return tmpl != null ? tmpl.idIcon.ToString() : itemId.ToString();
        }

        var stones = new APIClient.AddInventoryItemRequest[]
        {
            new APIClient.AddInventoryItemRequest { itemTemplateId = 17, quantity = geneStoneCount },
            new APIClient.AddInventoryItemRequest { itemTemplateId = 18, quantity = geneStoneCount },
            new APIClient.AddInventoryItemRequest { itemTemplateId = 19, quantity = geneStoneCount },
            new APIClient.AddInventoryItemRequest { itemTemplateId = 20, quantity = geneStoneCount },
        };

        bool done = false;
        APIClient.Instance.AddItemsToInventory(
            playerId,
            stones,
            (_)   => { Debug.Log($"[AutoEquipDebugger] ✅ Đã thêm 4 loại Linh Thạch x{geneStoneCount} vào túi!"); done = true; },
            (err) => { Debug.LogError($"[AutoEquipDebugger] Thêm Linh Thạch thất bại: {err}"); done = true; }
        );
        yield return new WaitUntil(() => done);

        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        bridge?.RefreshInventoryFromDB();

        Debug.Log("[AutoEquipDebugger] ===== XONG =====\nNhấn Y lần nữa để thêm tiếp.");
        isBusy = false;
    }

    private IEnumerator AddAndEquipAll()
    {
        isBusy = true;
        Debug.Log("[AutoEquipDebugger] ===== THÊM ITEM VÀO TÚI ĐỒ =====");

        // ── 1. Lấy playerId ──────────────────────────────────────────────
        int playerId = GetPlayerId();
        if (playerId == 0)
        {
            Debug.LogError("[AutoEquipDebugger] Không lấy được playerId! Chưa đăng nhập?");
            isBusy = false;
            yield break;
        }
        Debug.Log($"[AutoEquipDebugger] playerId = {playerId}");

        // ── 2. Đợi ItemTemplateManager load xong ─────────────────────────
        float timeout = 8f, elapsed = 0f;
        while (ItemTemplateManager.Instance == null || !ItemTemplateManager.Instance.IsLoaded())
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogError("[AutoEquipDebugger] ItemTemplateManager chưa load xong sau 8s!");
                isBusy = false;
                yield break;
            }
            yield return null;
        }

        // ── 3. Tìm item đầu tiên của mỗi loại trang bị (type 0-5) ───────
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
                Debug.LogWarning($"[AutoEquipDebugger] Không tìm thấy item nào cho type={equipType}!");
                continue;
            }

            toAdd.Add(new APIClient.AddInventoryItemRequest
            {
                itemTemplateId = tmpl.id,
                quantity       = quantityPerItem
            });
            Debug.Log($"[AutoEquipDebugger] Sẽ thêm: [{tmpl.name}] id={tmpl.id} type={equipType}");
        }

        if (toAdd.Count == 0)
        {
            Debug.LogError("[AutoEquipDebugger] Không có item nào để thêm!");
            isBusy = false;
            yield break;
        }

        // ── 4. Xóa inventory cũ trước ────────────────────────────────────
        bool clearDone = false;
        APIClient.Instance.ClearInventory(
            playerId,
            () => { clearDone = true; },
            (err) => { Debug.LogWarning($"[AutoEquipDebugger] Clear inventory warning: {err}"); clearDone = true; }
        );
        yield return new WaitUntil(() => clearDone);
        Debug.Log("[AutoEquipDebugger] Đã clear inventory cũ!");

        // ── 5. Gọi API thêm vào inventory ────────────────────────────────
        bool addDone = false, addSuccess = false;
        APIClient.Instance.AddItemsToInventory(
            playerId,
            toAdd.ToArray(),
            (_)   => { addSuccess = true; addDone = true; },
            (err) => { Debug.LogError($"[AutoEquipDebugger] Thêm item thất bại: {err}"); addDone = true; }
        );
        yield return new WaitUntil(() => addDone);
        if (!addSuccess) { isBusy = false; yield break; }
        Debug.Log($"[AutoEquipDebugger] Đã thêm {toAdd.Count} item vào túi đồ!");

        // ── 6. Refresh UI ─────────────────────────────────────────────────
        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (bridge != null)
        {
            bridge.RefreshInventoryFromDB();
            Debug.Log("[AutoEquipDebugger] UI túi đồ đã refresh!");
        }

        Debug.Log("[AutoEquipDebugger] ===== HOÀN THÀNH =====");
        isBusy = false;
    }

    private int GetPlayerId()
    {
        int playerId = 0;

        // Ưu tiên 1: GameManager in-memory
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            playerId = GameManager.Instance.GetPlayerData().user_id;

        // Ưu tiên 2: ServerPlayerDataManager (khi chạy dưới dạng host)
        if (playerId == 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var serverDataMgr = ServerPlayerDataManager.Instance;
            if (serverDataMgr != null)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                var playerData = serverDataMgr.GetPlayerDataForClient(localClientId);
                if (playerData != null)
                    playerId = playerData.user_id;
            }
        }

        // Fallback: PlayerPrefs
        if (playerId == 0)
            playerId = PlayerPrefs.GetInt("USER_ID", 0);

        if (playerId == 0)
            Debug.LogWarning("[AutoEquipDebugger] playerId = 0! GameManager, ServerPlayerDataManager, và PlayerPrefs đều không có dữ liệu.");

        return playerId;
    }
}
