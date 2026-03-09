using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// UpgradePanel – Panel nâng cấp trang bị chính.
///
/// ═══════════════════════════════════════════════════════
/// CÁCH MỞ PANEL (gọi từ EquipmentSlotUI hoặc InventorySlotUI):
///
///   // Từ trang bị đang mặc:
///   UpgradePanel.Instance.OpenForEquipped(equipItem, "weapon");
///
///   // Từ túi đồ:
///   UpgradePanel.Instance.OpenForInventory(inventorySlot, allInventorySlots);
///
/// ═══════════════════════════════════════════════════════
/// INSPECTOR SETUP (xem cuối file):
/// ═══════════════════════════════════════════════════════
/// </summary>
public class UpgradePanel : MonoBehaviour
{
    public static UpgradePanel Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────

    [Header("Item Cards")]
    [SerializeField] private UpgradeItemCard currentCard;    // card trái – trang bị hiện tại
    [SerializeField] private UpgradeItemCard previewCard;    // card phải – preview sau nâng cấp

    [Header("Stone Matrix (kéo đủ 16 slot theo thứ tự)")]
    [SerializeField] private UpgradeStoneSlot[] stoneSlots;  // 16 ô đá

    [Header("Stone Picker (panel chọn đá từ túi đồ)")]
    [SerializeField] private GameObject       stonePickerPanel;   // panel con chứa danh sách đá
    [SerializeField] private Transform        stonePickerContent; // ScrollView Content
    [SerializeField] private InventorySlotUI  stonePickerItemPrefab; // prefab 1 ô đá trong picker

    [Header("Rate & Cost")]
    [SerializeField] private Slider    rateBar;
    [SerializeField] private TMP_Text  rateText;
    [SerializeField] private TMP_Text  silverCostText;
    [SerializeField] private TMP_Text  silverOwnText;
    [SerializeField] private GameObject failWarningObj;   // "⚠ Thất bại có thể giảm bậc"

    [Header("Buttons")]
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button cancelButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;  // kết quả sau khi nâng cấp (tuỳ chọn)

    // ── Runtime data ──────────────────────────────────────────────
    private EquipmentItemDto     currentItem;
    private string               slotKey;
    private bool                 isFromInventory;
    private UpgradeConfigDto     config;
    private List<OptionTemplateDto> optionCache;   // cache option templates

    private InventorySlotDto[]   inventoryCache;  // túi đồ hiện tại (để lọc đá)
    private UpgradeStoneSlot     pendingStoneSlot; // slot đang chờ user chọn đá

    // Lucky stone id = 8, protection stone id = 9 (theo gamedb.sql)
    private const int LUCKY_STONE_ID      = 8;
    private const int PROTECTION_STONE_ID = 9;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);

        // Nếu Inspector không kéo slot vào mảng, tự động tìm các UpgradeStoneSlot trong children
        if (stoneSlots == null || stoneSlots.Length == 0)
            stoneSlots = GetComponentsInChildren<UpgradeStoneSlot>(true);
    }

    private void Start()
    {
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        cancelButton.onClick.AddListener(OnCancelClicked);
        if (stonePickerPanel) stonePickerPanel.SetActive(false);
    }

    // ── Mở panel ──────────────────────────────────────────────────

    /// <summary>Mở từ trang bị ĐANG MẶC</summary>
    public void OpenForEquipped(EquipmentItemDto item, string equipSlotKey, InventorySlotDto[] inventory)
    {
        currentItem     = item;
        slotKey         = equipSlotKey;
        isFromInventory = false;
        inventoryCache  = inventory;
        gameObject.SetActive(true);
        StartCoroutine(LoadAndOpen());
    }

    /// <summary>Mở từ trang bị TRONG TÚI ĐỒ</summary>
    public void OpenForInventory(InventorySlotDto slot, InventorySlotDto[] inventory)
    {
        currentItem = new EquipmentItemDto
        {
            id           = slot.id,
            upgradeLevel = slot.upgradeLevel,
            strOptions   = slot.strOptions
        };
        slotKey         = slot.slotIndex.ToString();
        isFromInventory = true;
        inventoryCache  = inventory;
        gameObject.SetActive(true);
        StartCoroutine(LoadAndOpen());
    }

    // ── Load data & hiển thị ─────────────────────────────────────

    private IEnumerator LoadAndOpen()
    {
        SetStatus("Đang tải...", Color.gray);
        upgradeButton.interactable = false;

        // 0. Refresh player data (silver có thể stale từ lúc login)
        yield return StartCoroutine(RefreshPlayerData());

        // 1. Load option templates nếu chưa có
        if (optionCache == null)
            yield return StartCoroutine(LoadOptionTemplates());

        // 2. Load upgrade config cho bậc target
        int targetLevel = currentItem.upgradeLevel + 1;
        bool configOk = false;
        yield return StartCoroutine(LoadUpgradeConfig(targetLevel, (ok) => configOk = ok));

        if (!configOk)
        {
            SetStatus("Không tải được config nâng cấp.", Color.red);
            yield break;
        }

        // 3. Hiển thị
        ClearStoneSlots();
        AutoFillStones();
        currentCard.Display(currentItem, optionCache, false);
        previewCard.Display(currentItem, optionCache, true);
        RefreshRateAndCost();
        SetStatus("", Color.white);
    }

    private IEnumerator RefreshPlayerData()
    {
        // 3-step fallback giống InventoryNetworkBridge.GetCurrentPlayerId()
        int playerId = 0;

        // 1. GameManager (in-memory sau login)
        if (GameManager.Instance != null && GameManager.Instance.HasPlayerData())
            playerId = GameManager.Instance.GetPlayerData().user_id;

        // 2. ServerPlayerDataManager (Netcode host)
        if (playerId == 0 && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            var spdm = ServerPlayerDataManager.Instance;
            if (spdm != null)
            {
                ulong localClientId = NetworkManager.Singleton.LocalClientId;
                var pd = spdm.GetPlayerDataForClient(localClientId);
                if (pd != null) playerId = pd.user_id;
            }
        }

        // 3. PlayerPrefs fallback
        if (playerId == 0)
            playerId = PlayerPrefs.GetInt("USER_ID", 0);

        Debug.Log($"[UpgradePanel] RefreshPlayerData: resolved playerId={playerId}");

        if (playerId <= 0 || APIClient.Instance == null)
        {
            Debug.LogWarning("[UpgradePanel] RefreshPlayerData: không có playerId hợp lệ hoặc APIClient null");
            yield break;
        }

        bool done = false;
        APIClient.Instance.LoadPlayerData(
            playerId,
            onSuccess: (data) =>
            {
                GameManager.Instance.SetPlayerData(data);
                Debug.Log($"[UpgradePanel] ✅ Refresh OK – player_id={data.player_id}, silver={data.silver}");
                done = true;
            },
            onError: (err) =>
            {
                Debug.LogWarning($"[UpgradePanel] Refresh thất bại: {err} (dùng data cũ)");
                done = true;
            }
        );
        yield return new WaitUntil(() => done);
    }

    private IEnumerator LoadOptionTemplates()
    {
        bool done = false;
        APIClient.Instance.GetOptionTemplates(
            onSuccess: (OptionTemplateDto[] arr) =>
            {
                optionCache = new List<OptionTemplateDto>(arr);
                done = true;
            },
            onError: (err) =>
            {
                Debug.LogWarning($"[UpgradePanel] Không load được option templates: {err}");
                optionCache = new List<OptionTemplateDto>();
                done = true;
            }
        );
        yield return new WaitUntil(() => done);
    }

    private IEnumerator LoadUpgradeConfig(int targetLevel, System.Action<bool> onDone)
    {
        bool done    = false;
        bool success = false;
        APIClient.Instance.GetUpgradeConfig(
            itemId: currentItem.id,
            targetLevel: targetLevel,
            onSuccess: (UpgradeConfigDto cfg) => { config = cfg; done = true; success = true; },
            onError: (err) =>
            {
                Debug.LogError($"[UpgradePanel] GetUpgradeConfig l\u1ed7i: {err}");
                config = null;
                done = true;
            }
        );
        yield return new WaitUntil(() => done);
        onDone?.Invoke(success);
    }

    // ── Stone Picker ──────────────────────────────────────────────

    /// <summary>Gọi từ UpgradeStoneSlot khi click ô trống</summary>
    public void OnStoneSlotClicked(UpgradeStoneSlot slot)
    {
        pendingStoneSlot = slot;
        OpenStonePicker();
    }

    /// <summary>Gọi từ UpgradeStoneSlot khi click ô có đá → tháo ra</summary>
    public void OnStoneSlotRemoved(UpgradeStoneSlot slot)
    {
        slot.Clear();
        RefreshRateAndCost();
    }

    private void OpenStonePicker()
    {
        if (stonePickerPanel == null || stonePickerContent == null) return;

        // Xoá danh sách cũ
        foreach (Transform child in stonePickerContent)
            Destroy(child.gameObject);

        // Lọc các đá có thể đặt từ túi đồ (stoneId từ config, lucky=8, protection=9)
        foreach (var slot in inventoryCache ?? new InventorySlotDto[0])
        {
            // Loại đá hợp lệ: upgrade stone (id == config.stoneId), lucky, protection
            bool isStone = slot.id == config.stoneId || slot.id == LUCKY_STONE_ID || slot.id == PROTECTION_STONE_ID;
            if (!isStone || slot.amount <= 0) continue;

            // Lucky / Protection: tối đa 1 loại mỗi loại
            if (slot.id == LUCKY_STONE_ID     && CountStoneType(LUCKY_STONE_ID)      >= 1) continue;
            if (slot.id == PROTECTION_STONE_ID && CountStoneType(PROTECTION_STONE_ID) >= 1) continue;

            // Đá nâng cấp: chỉ ẩn khi đã dùng hết amount
            if (slot.id == config.stoneId && CountPlacedFromSlot(slot.slotIndex) >= slot.amount) continue;

            var item = Instantiate(stonePickerItemPrefab, stonePickerContent);
            // Gán data hiển thị
            var captured = slot;
            var btn = item.GetComponent<Button>();
            if (btn == null) btn = item.gameObject.AddComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnPickStone(captured));
        }

        stonePickerPanel.SetActive(true);
    }

    /// <summary>Đếm bao nhiêu ô đá đang dùng đúng inventory slotIndex này.</summary>
    private int CountPlacedFromSlot(int invSlotIndex)
    {
        int count = 0;
        foreach (var s in stoneSlots)
            if (!s.IsEmpty && s.InventorySlotIndex == invSlotIndex)
                count++;
        return count;
    }

    private void OnPickStone(InventorySlotDto slot)
    {
        if (pendingStoneSlot != null)
        {
            pendingStoneSlot.SetItem(slot);
            pendingStoneSlot = null;
        }
        if (stonePickerPanel) stonePickerPanel.SetActive(false);
        RefreshRateAndCost();
    }

    // ── Rate & Cost ───────────────────────────────────────────────

    private void ClearStoneSlots()
    {
        foreach (var s in stoneSlots) s.Clear();
    }

    /// <summary>Tự động điền đá từ túi đồ vào các ô slot khi mở panel.</summary>
    private void AutoFillStones()
    {
        if (inventoryCache == null || config == null || stoneSlots == null) return;

        int slotIdx      = 0;
        int upgradeCount = 0;

        // 1. Đá nâng cấp: 1 stack có thể điền nhiều ô theo amount
        foreach (var inv in inventoryCache)
        {
            if (inv.id != config.stoneId || inv.amount <= 0) continue;
            int toFill = Mathf.Min(inv.amount, config.stoneNeeded - upgradeCount);
            for (int i = 0; i < toFill; i++)
            {
                if (slotIdx >= stoneSlots.Length) break;
                stoneSlots[slotIdx++].SetItem(inv);
                upgradeCount++;
            }
            if (upgradeCount >= config.stoneNeeded) break;
        }

        // 2. Đá may mắn: tối đa 1
        foreach (var inv in inventoryCache)
        {
            if (inv.id != LUCKY_STONE_ID || inv.amount <= 0) continue;
            if (slotIdx >= stoneSlots.Length) break;
            stoneSlots[slotIdx++].SetItem(inv);
            break;
        }

        // 3. Đá bảo vệ: tối đa 1
        foreach (var inv in inventoryCache)
        {
            if (inv.id != PROTECTION_STONE_ID || inv.amount <= 0) continue;
            if (slotIdx >= stoneSlots.Length) break;
            stoneSlots[slotIdx].SetItem(inv);
            break;
        }
    }

    private void RefreshRateAndCost()
    {
        if (config == null)
        {
            Debug.LogWarning("[UpgradePanel] RefreshRateAndCost: config=null, b\u1ecf qua.");
            return;
        }

        int   upgradeStones  = CountStoneType(config.stoneId);
        int   luckyStones    = CountStoneType(LUCKY_STONE_ID);
        bool  hasProtection  = CountStoneType(PROTECTION_STONE_ID) > 0;
        bool  enoughStones   = upgradeStones >= config.stoneMin;

        int   silverOwned    = GetPlayerSilver();
        bool  enoughSilver   = silverOwned >= config.silverCost;

        Debug.Log($"[UpgradePanel] Rate calc: stoneId={config.stoneId} upgradeStones={upgradeStones}/min={config.stoneMin}/need={config.stoneNeeded} " +
                  $"lucky={luckyStones} silver={silverOwned}/{config.silverCost} " +
                  $"enoughStones={enoughStones} enoughSilver={enoughSilver} slots={stoneSlots?.Length}");

        // ── Tỉ lệ ────────────────────────────────────────────────
        float rate = 0f;
        if (upgradeStones > 0)
        {
            float stoneRatio = Mathf.Min((float)upgradeStones / config.stoneNeeded, 1f);
            rate = config.baseSuccessRate * stoneRatio;
            rate += luckyStones * 0.15f;
            rate  = Mathf.Clamp01(rate);
        }

        // ── Cập nhật UI ───────────────────────────────────────────
        if (rateBar)  rateBar.value  = rate;
        if (rateText) rateText.text  = $"{rate * 100f:F0}%";

        if (silverCostText)
        {
            silverCostText.text  = $"Bạc cần: {config.silverCost:N0}";
            silverCostText.color = enoughSilver ? Color.white : Color.red;
        }
        if (silverOwnText)
            silverOwnText.text = $"Bạn có: {silverOwned:N0}";

        // Ẩn cảnh báo vỡ khi có đá bảo vệ
        if (failWarningObj)
            failWarningObj.SetActive(config.failPolicy > 0 && !hasProtection);

        upgradeButton.interactable = enoughStones && enoughSilver && upgradeStones > 0;
    }

    private int CountStoneType(int stoneItemId)
    {
        int count = 0;
        foreach (var s in stoneSlots)
            if (!s.IsEmpty && s.ItemData != null && s.ItemData.id == stoneItemId)
                count++;
        return count;
    }

    private int GetPlayerSilver()
    {
        var pd = GameManager.Instance?.currentPlayerData;
        if (pd == null)
        {
            Debug.LogWarning("[UpgradePanel] GetPlayerSilver: currentPlayerData=null");
            return 0;
        }
        Debug.Log($"[UpgradePanel] GetPlayerSilver: silver={pd.silver}, gold={pd.gold}, player_id={pd.player_id}");
        return pd.silver;
    }

    // ── Nâng cấp ──────────────────────────────────────────────────

    public void OnUpgradeClicked()
    {
        upgradeButton.interactable = false;
        SetStatus("Đang nâng cấp...", Color.gray);

        int playerId = GameManager.Instance?.currentPlayerData?.player_id ?? 0;

        var stoneIndices = new List<int>();
        foreach (var s in stoneSlots)
            if (!s.IsEmpty) stoneIndices.Add(s.InventorySlotIndex);

        var request = new UpgradeRequestDto
        {
            playerId        = playerId,
            slotKey         = slotKey,
            isFromInventory = isFromInventory,
            stoneSlotIndices = stoneIndices
        };

        APIClient.Instance.UpgradeEquipment(
            request,
            onSuccess: (UpgradeResponseDto resp) => HandleUpgradeResponse(resp),
            onError:   (err) =>
            {
                upgradeButton.interactable = true;
                SetStatus($"Lỗi: {err}", Color.red);
            }
        );
    }

    private void HandleUpgradeResponse(UpgradeResponseDto resp)
    {
        if (resp.success)
        {
            SetStatus($"✨ Thành công! Đạt +{resp.newUpgradeLevel}", new Color(1f, 0.85f, 0f));
            currentItem.upgradeLevel  = resp.newUpgradeLevel;
            currentItem.strOptions    = resp.updatedStrOptions;
        }
        else
        {
            string msg = resp.downgraded
                ? $"💔 Thất bại! Về +{resp.newUpgradeLevel}"
                : "😞 Thất bại! Trang bị không đổi.";
            SetStatus(msg, resp.downgraded ? Color.red : new Color(1f, 0.5f, 0f));

            if (resp.downgraded)
            {
                currentItem.upgradeLevel = resp.newUpgradeLevel;
                currentItem.strOptions   = resp.updatedStrOptions;
            }
        }

        // Cập nhật túi đồ (đá đã trừ)
        if (resp.updatedInventory != null)
            inventoryCache = resp.updatedInventory;
        // Cập nhật silver trong GameManager
        if (resp.silver > 0 || resp.message != null)
        {
            var pd = GameManager.Instance?.currentPlayerData;
            if (pd != null) pd.silver = resp.silver;
        }
        // Reload panel với dữ liệu mới
        StartCoroutine(ReloadAfterDelay(1.2f));
    }

    private IEnumerator ReloadAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearStoneSlots();
        currentCard.Display(currentItem, optionCache, false);
        previewCard.Display(currentItem, optionCache, true);

        int targetLevel = currentItem.upgradeLevel + 1;
        bool configOk = false;
        yield return StartCoroutine(LoadUpgradeConfig(targetLevel, (ok) => configOk = ok));

        AutoFillStones();
        RefreshRateAndCost();
    }

    // ── Đóng panel ────────────────────────────────────────────────

    public void OnCancelClicked()
    {
        if (stonePickerPanel && stonePickerPanel.activeSelf)
        {
            stonePickerPanel.SetActive(false);
            return;
        }
        ClearStoneSlots();
        gameObject.SetActive(false);
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }
}

// ═══════════════════════════════════════════════════════════════
// INSPECTOR CONFIG CHECKLIST
// ───────────────────────────────────────────────────────────────
//
// Gắn UpgradePanel.cs lên: Canvas/UpgradePanel (GameObject)
//
// [Item Cards]
//   Current Card  → UpgradeItemCard trên CurrentCard gameObject
//   Preview Card  → UpgradeItemCard trên PreviewCard gameObject
//
// [Stone Matrix]
//   Stone Slots   → kéo 16 StoneSlot_00 → StoneSlot_15 vào array
//                   (theo thứ tự, mỗi slot có UpgradeStoneSlot.cs)
//
// [Stone Picker]
//   Stone Picker Panel   → Panel "StonePicker" (ẩn mặc định)
//   Stone Picker Content → ScrollRect/Viewport/Content Transform
//   Stone Picker Item Prefab → Prefab InventorySlotUI (có Button)
//
// [Rate & Cost]
//   Rate Bar        → Slider (Interactable = false)
//   Rate Text       → TMP_Text "87%"
//   Silver Cost Text → TMP_Text "Bạc cần: ..."
//   Silver Own Text  → TMP_Text "Bạn có: ..."
//   Fail Warning Obj → GameObject Text "⚠ Thất bại có thể giảm bậc"
//
// [Buttons]
//   Upgrade Button  → Button "NÂNG CẤP"
//   Cancel Button   → Button "HỦY"
//
// [Status]
//   Status Text     → TMP_Text hiện kết quả (tuỳ chọn)
//
// ── Cách gọi từ EquipmentSlotUI khi player click vào slot: ──────
//
//   void OnClickUpgrade()
//   {
//       var inv = FindObjectOfType<InventoryNetworkBridge>()?.CurrentInventory;
//       UpgradePanel.Instance.OpenForEquipped(equippedItem, slotKey, inv);
//   }
//
// ═══════════════════════════════════════════════════════════════
