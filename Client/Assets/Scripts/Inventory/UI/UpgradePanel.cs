using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// UpgradePanel v2 – Panel cường hóa trang bị với layout mới.
///
/// ══════════════════════════════════════════════════════════════════
/// LAYOUT TỔNG QUAN:
///   ┌─ [Ô Trang Bị]     [Ô Bùa id=8]  ─┐
///   │  [Btn Lấy Ra][Xem]  [Btn Lấy Ra]  │
///   ├──────────────────────────────────  ┤
///   │         16 ÔĐÁ (4 × 4)            │
///   ├──────────────────────────────────  ┤
///   │  [XEM TRƯỚC]    [CƯỜNG HÓA]        │
///   │  ▓▓▓▓░░░ 72%   Bạc cần: 5000      │
///   └────────────────────────────────── ┘
///
/// LUỒNG CHỌN ĐÁ (type=21):
///   Click ô đá trống
///     → BlacksmithTabPanel.SwitchTabToInventoryWithFilter(filterItemType=21)
///     → InventoryUI vào StoneSelectMode
///     → item type=21 hiện btn "Chọn"
///     → Chọn → đá vào ô, slotId ghi lại
///
/// LUỒNG CHỌN TRANG BỊ:
///   Click ô Trang Bị trống → tab Trang Bị
///   Khi ô có item → [Lấy Ra] + [Xem TT]
///
/// LUỒNG BÙA (itemId=8):
///   Click ô Bùa trống → tab Túi filter id=8
///   Khi ô có bùa → [Lấy Ra] + [Xem] + +3% rate
///
/// PREVIEW:
///   Chưa có trang bị → không làm gì
///   Có trang bị → hiện stats dự đoán +1
/// ══════════════════════════════════════════════════════════════════
/// </summary>
public class UpgradePanel : MonoBehaviour
{
    public static UpgradePanel Instance { get; private set; }

    [Header("── Ô Trang Bị ─────────────────────────────────────")]
    [SerializeField] private Button    equipSlotButton;       // Click khi trống → tab Trang Bị
    [SerializeField] private Image     equipSlotIcon;         // Icon item đang chọn
    [SerializeField] private TMP_Text  equipSlotNameText;     // Tên item / "Chọn trang bị..."
    [SerializeField] private TMP_Text  upgradeLevelText;      // "+3"
    [SerializeField] private Button    equipRemoveButton;     // [Lấy Ra]
    [SerializeField] private Button    equipViewStatsButton;  // [Xem TT]

    [Header("── Ô Bùa Cường Hóa (itemId=8) ──────────────────────")]
    [SerializeField] private Button    charmSlotButton;       // Click trống → tab Túi filter id=8
    [SerializeField] private Image     charmSlotIcon;         // Icon bùa
    [SerializeField] private TMP_Text  charmSlotNameText;     // Tên bùa / "Bùa cường hóa"
    [SerializeField] private Button    charmRemoveButton;     // [Lấy Ra]
    [SerializeField] private Button    charmViewButton;       // [Xem]

    [Header("── Preview Panel ─────────────────────────────────────")]
    [SerializeField] private GameObject previewPanel;         // ẩn/hiện
    [SerializeField] private TMP_Text   previewNameText;
    [SerializeField] private TMP_Text   previewStatsText;     // hiển thị chỉ số (tạm thời thay prefab)
    [SerializeField] private Button     previewCloseButton;   // [X] đóng preview panel

    [Header("── 16 Ô Đá (Stone Grid 4×4) ──────────────────────────")]
    [SerializeField] private UpgradeStoneSlot[] stoneSlots = new UpgradeStoneSlot[16];

    [Header("── Nút Chính ─────────────────────────────────────────")]
    [SerializeField] private Button    previewButton;        // XEM TRƯỚC
    [SerializeField] private Button    upgradeButton;        // CƯỜNG HÓA
    [SerializeField] private Button    cancelButton;         // HỦY

    [Header("── Rate & Cost ───────────────────────────────────────")]
    [SerializeField] private Slider    rateBar;
    [SerializeField] private TMP_Text  rateText;
    [SerializeField] private TMP_Text  silverCostText;
    [SerializeField] private TMP_Text  silverOwnText;
    [SerializeField] private GameObject failWarningObj;

    [Header("── Stone Config (ScriptableObject) ────────────────────")]
    [SerializeField] private UpgradeStoneConfig upgradeStoneConfig;

    [Header("── Status ────────────────────────────────────────────")]
    [SerializeField] private TMP_Text statusText;

    [Header("── Item Detail Popup ──────────────────────────────────")]
    [Tooltip("Prefab ItemDetailPanel dùng để hiện thông tin khi click ô trang bị / ô bùa.")]
    [SerializeField] private ItemDetailPanel itemDetailPanelPrefab;
    [Header("── Equip Info Box (mini popup khi click ô trang bị) ──────────")]
    [Tooltip("Container GO chứa tên item + btn Lấy Ra + btn Xem. Ẩn mặc định.")]
    [SerializeField] private GameObject equipInfoBox;
    [Tooltip("TMP_Text hiển thị tên item trong popup, vd 'Áo Nhẫn Giả Base (+19)'.")]
    [SerializeField] private TMP_Text   equipInfoTitleText;
    [Tooltip("Nút X để đóng popup mini.")]
    [SerializeField] private Button     equipInfoCloseButton;

    [Header("── Charm Info Box (mini popup khi click ô bùa) ─────────────")]
    [Tooltip("Container GO cho bùa. Ẩn mặc định.")]
    [SerializeField] private GameObject charmInfoBox;
    [Tooltip("TMP_Text hiển thị tên bùa trong popup.")]
    [SerializeField] private TMP_Text   charmInfoTitleText;
    [Tooltip("Nút X để đóng popup bùa.")]
    [SerializeField] private Button     charmInfoCloseButton;
    // ══════════════════════════════════════════════════════════════
    // CONSTANTS
    // ══════════════════════════════════════════════════════════════
    public const int CHARM_ITEM_ID   = 8;
    public const int STONE_ITEM_TYPE = 21;

    // ══════════════════════════════════════════════════════════════
    // RUNTIME STATE
    // ══════════════════════════════════════════════════════════════
    private EquipmentItemDto        _equippedItem;
    private string                  _slotKey;
    private bool                    _isFromInventory;
    private UpgradeConfigDto        _config;
    private List<OptionTemplateDto> _optionCache;
    private InventorySlotDto[]      _inventoryCache;
    private InventorySlotDto        _charmSlot;
    private UpgradeStoneSlot        _pendingStoneSlot;

    // slotIndex trong inventory → index trong stoneSlots array (để gửi lên server)
    private readonly Dictionary<int,int> _stoneArrayIdxToInvSlotIdx = new();
    private readonly Dictionary<int,int> _reservedMaterialCounts = new();

    private ItemDetailPanel _detailPanelInstance;

    // ══════════════════════════════════════════════════════════════
    // LIFECYCLE
    // ══════════════════════════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        NormalizeStoneSlots();
        DisableTransparentRootRaycast();

        gameObject.SetActive(false);
    }

    private void Start()
    {
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        cancelButton .onClick.AddListener(OnCancelClicked);
        previewButton.onClick.AddListener(OnPreviewClicked);

        equipSlotButton  .onClick.AddListener(OnEquipSlotClicked);
        equipRemoveButton.onClick.AddListener(OnEquipRemoveClicked);
        if (equipViewStatsButton) equipViewStatsButton.onClick.AddListener(OnEquipViewStatsClicked);

        charmSlotButton  .onClick.AddListener(OnCharmSlotClicked);
        charmRemoveButton.onClick.AddListener(OnCharmRemoveClicked);
        if (charmViewButton) charmViewButton.onClick.AddListener(OnCharmViewClicked);

        if (equipInfoCloseButton)  equipInfoCloseButton.onClick.AddListener(HideEquipInfoBox);
        if (charmInfoCloseButton)  charmInfoCloseButton.onClick.AddListener(HideCharmInfoBox);
        if (previewCloseButton)    previewCloseButton.onClick.AddListener(HidePreview);

        ResetEquipDisplay();
        ResetCharmDisplay();
        HidePreview();
        ClearAllStoneSlots();
        RefreshRateDisplay();
    }

    // ══════════════════════════════════════════════════════════════
    // PUBLIC OPEN API
    // ══════════════════════════════════════════════════════════════

    /// <summary>Mở panel trống – từ NPC blacksmith.</summary>
    public void OpenEmpty(InventorySlotDto[] inventory)
    {
        ClearReservedMaterialCounts();
        EnsureBlacksmithUpgradeTabVisible();
        _inventoryCache   = GetFreshInventorySnapshot(inventory);
        _equippedItem     = null;
        _charmSlot        = null;
        _pendingStoneSlot = null;
        _stoneArrayIdxToInvSlotIdx.Clear();
        ClearAllStoneSlots();
        ResetEquipDisplay();
        ResetCharmDisplay();
        HidePreview();
        SetStatus("Chọn trang bị cần cường hóa.", Color.white);
        RefreshRateDisplay();
        gameObject.SetActive(true);
    }

    /// <summary>Mở từ trang bị đang mặc.</summary>
    public void OpenForEquipped(EquipmentItemDto item, string equipSlotKey, InventorySlotDto[] inventory)
    {
        ClearReservedMaterialCounts();
        EnsureBlacksmithUpgradeTabVisible();
        _inventoryCache   = GetFreshInventorySnapshot(inventory);
        _isFromInventory  = false;
        _slotKey          = equipSlotKey;
        _charmSlot        = null;
        _pendingStoneSlot = null;
        _stoneArrayIdxToInvSlotIdx.Clear();
        ClearAllStoneSlots();
        ResetCharmDisplay();
        HidePreview();
        ApplyEquippedItem(item);
        gameObject.SetActive(true);
        StartCoroutine(LoadConfigAndRefresh());
    }

    /// <summary>Mở từ túi đồ.</summary>
    public void OpenForInventory(InventorySlotDto slot, InventorySlotDto[] inventory)
    {
        ClearReservedMaterialCounts();
        EnsureBlacksmithUpgradeTabVisible();
        _inventoryCache   = GetFreshInventorySnapshot(inventory);
        _isFromInventory  = true;
        _slotKey          = slot.slotIndex.ToString();
        _charmSlot        = null;
        _pendingStoneSlot = null;
        _stoneArrayIdxToInvSlotIdx.Clear();
        ClearAllStoneSlots();
        ResetCharmDisplay();
        HidePreview();
        var dto = new EquipmentItemDto
        {
            id           = slot.id,
            upgradeLevel = slot.upgradeLevel,
            strOptions   = slot.strOptions
        };
        ApplyEquippedItem(dto);
        gameObject.SetActive(true);
        StartCoroutine(LoadConfigAndRefresh());
    }

    /// <summary>Gọi từ BlacksmithTabPanel khi đóng.</summary>
    public void CloseFromTabPanel()
    {
        ClearReservedMaterialCounts();
        _equippedItem     = null;
        _charmSlot        = null;
        _pendingStoneSlot = null;
        _stoneArrayIdxToInvSlotIdx.Clear();
        ClearAllStoneSlots();
        ResetEquipDisplay();
        ResetCharmDisplay();
        HidePreview();
    }

    // ══════════════════════════════════════════════════════════════
    // Ô TRANG BỊ
    // ══════════════════════════════════════════════════════════════

    private void OnEquipSlotClicked()
    {
        if (_equippedItem == null)
            BlacksmithTabPanel.Instance?.SwitchTab(1);
        else
            ShowEquipInfoBox();
    }

    private void OnEquipRemoveClicked()
    {
        HideEquipInfoBox();
        ClearReservedMaterialCounts();
        _equippedItem = null;
        _slotKey      = null;
        _config       = null;
        ClearAllStoneSlots();
        ResetEquipDisplay();
        HidePreview();
        RefreshRateDisplay();
    }

    private void OnEquipViewStatsClicked()
    {
        if (_equippedItem == null) return;
        HideEquipInfoBox();
        var dp = GetOrCreateDetailPanel();
        if (dp != null)
            dp.ShowEquipmentItem(_equippedItem, _optionCache);
        else
            ShowPreview(showCurrentLevel: true);
    }

    /// <summary>
    /// Gọi từ tab Trang Bị khi player chọn trang bị để nâng.
    /// </summary>
    public void SetChosenEquipItem(EquipmentItemDto item, string slotKey, bool fromInventory, InventorySlotDto[] inventory)
    {
        ClearReservedMaterialCounts();
        _inventoryCache  = inventory ?? _inventoryCache;
        _isFromInventory = fromInventory;
        _slotKey         = slotKey;
        _stoneArrayIdxToInvSlotIdx.Clear();
        ClearAllStoneSlots();
        HidePreview();
        ApplyEquippedItem(item);
        StartCoroutine(LoadConfigAndRefresh());
        BlacksmithTabPanel.Instance?.SwitchTab(0);
    }

    private void ApplyEquippedItem(EquipmentItemDto item)
    {
        _equippedItem = item;
        if (item == null) { ResetEquipDisplay(); return; }

        var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(item.id);
        string name = tmpl != null ? tmpl.name : $"Item #{item.id}";

        equipSlotNameText.text = $"{name}";
        if (upgradeLevelText)  upgradeLevelText.text = $"+{item.upgradeLevel}";

        if (equipSlotIcon != null && tmpl != null && IconDatabase.Instance != null)
        {
            var sp = IconDatabase.Instance.GetIcon(tmpl.idIcon.ToString());
            equipSlotIcon.sprite  = sp;
            equipSlotIcon.enabled = sp != null;
        }

        // Buttons are inside equipInfoBox; shown only when popup is opened via OnEquipSlotClicked
        HideEquipInfoBox();
        upgradeButton.interactable = false;
    }

    private void ResetEquipDisplay()
    {
        equipSlotNameText.text = "Chọn trang bị...";
        if (upgradeLevelText) upgradeLevelText.text = "";
        if (equipSlotIcon)    equipSlotIcon.enabled = false;
        HideEquipInfoBox();
        upgradeButton.interactable = false;
    }

    // ══════════════════════════════════════════════════════════════
    // Ô BÙA (itemId=8)
    // ══════════════════════════════════════════════════════════════

    private void OnCharmSlotClicked()
    {
        if (_charmSlot == null)
            BlacksmithTabPanel.Instance?.SwitchTabToInventoryWithFilter(filterItemId: CHARM_ITEM_ID);
        else
            ShowCharmInfoBox();
    }

    private void OnCharmRemoveClicked()
    {
        HideCharmInfoBox();
        ReleaseReservedMaterial(_charmSlot);
        _charmSlot = null;
        ResetCharmDisplay();
        RefreshRateDisplay();
    }

    private void OnCharmViewClicked()
    {
        if (_charmSlot == null) return;
        HideCharmInfoBox();
        GetOrCreateDetailPanel()?.ShowItem(_charmSlot);
    }

    /// <summary>Người chơi chọn bùa (id=8) từ túi đồ.</summary>
    public void SetCharmFromInventory(InventorySlotDto slot)
    {
        TrySetCharmFromInventory(slot);
    }

    private bool TrySetCharmFromInventory(InventorySlotDto slot)
    {
        if (slot == null) return false;

        RememberInventorySlot(slot);

        if (_charmSlot != null)
            ReleaseReservedMaterial(_charmSlot);

        if (!ReserveReservedMaterial(slot))
        {
            SetStatus("Không đủ bùa trong túi!", Color.red);
            return false;
        }

        _charmSlot = slot;

        var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(slot.id);
        charmSlotNameText.text = GetInventorySlotDisplayName(slot, tmpl);

        if (charmSlotIcon != null && IconDatabase.Instance != null)
        {
            var sp = ResolveInventorySlotIcon(slot, tmpl);
            charmSlotIcon.sprite  = sp;
            charmSlotIcon.enabled = sp != null;
        }

        // Buttons are inside charmInfoBox; shown only when popup is opened via OnCharmSlotClicked
        RefreshRateDisplay();
        BlacksmithTabPanel.Instance?.SwitchTab(0);
        return true;
    }

    private void ResetCharmDisplay()
    {
        charmSlotNameText.text = "Bùa cường hóa";
        if (charmSlotIcon) charmSlotIcon.enabled = false;
        HideCharmInfoBox();
    }

    // ══════════════════════════════════════════════════════════════
    // 16 Ô ĐÁ – callback từ UpgradeStoneSlot
    // ══════════════════════════════════════════════════════════════

    /// <summary>Click ô đá trống → chuyển sang tab Túi (stone selection mode).</summary>
    public void OnStoneSlotClicked(UpgradeStoneSlot slot)
    {
        _pendingStoneSlot = slot;
        BlacksmithTabPanel.Instance?.SwitchTabToInventoryWithFilter(filterItemType: STONE_ITEM_TYPE);
    }

    /// <summary>Click ô đá có đá → tháo ra.</summary>
    public void OnStoneSlotRemoved(UpgradeStoneSlot slot)
    {
        int idx = System.Array.IndexOf(stoneSlots, slot);
        if (idx >= 0) _stoneArrayIdxToInvSlotIdx.Remove(idx);
        ReleaseReservedMaterial(slot?.ItemData);
        slot.Clear();
        RefreshRateDisplay();
    }

    /// <summary>
    /// InventoryUI (stone-selection mode) gọi khi người chơi bấm "Chọn" trên đá.
    /// </summary>
    public void OnStoneSelectedFromInventory(InventorySlotDto stone)
    {
        TrySetStoneFromInventory(stone);
    }

    private bool TrySetStoneFromInventory(InventorySlotDto stone)
    {
        if (stone == null) return false;

        RememberInventorySlot(stone);

        if (_pendingStoneSlot == null)
            _pendingStoneSlot = FindFirstEmptyStoneSlot();

        if (_pendingStoneSlot == null)
        {
            SetStatus("Đã đầy 16 ô đá rồi.", Color.yellow);
            return false;
        }

        if (!ReserveReservedMaterial(stone))
        {
            SetStatus($"Không đủ {GetInventorySlotDisplayName(stone)}!", Color.red);
            return false;
        }

        int arrayIdx = System.Array.IndexOf(stoneSlots, _pendingStoneSlot);
        _pendingStoneSlot.SetItem(stone);
        if (arrayIdx >= 0)
            _stoneArrayIdxToInvSlotIdx[arrayIdx] = stone.slotIndex;

        _pendingStoneSlot = null;
        RefreshRateDisplay();
        BlacksmithTabPanel.Instance?.SwitchTab(0);
        return true;
    }

    public bool TryUseInventoryItemForUpgrade(InventorySlotDto slot)
    {
        if (slot == null || slot.quantity <= 0) return false;

        RememberInventorySlot(slot);

        ItemTemplateDto template = ItemTemplateManager.Instance?.GetItemTemplate(slot.id);
        int itemType = template?.type ?? -1;

        if (slot.id == CHARM_ITEM_ID)
            return TrySetCharmFromInventory(slot);

        if (itemType == STONE_ITEM_TYPE)
            return TrySetStoneFromInventory(slot);

        if (itemType >= 0 && itemType <= 5)
        {
            OpenForInventory(slot, GetFreshInventorySnapshot(_inventoryCache));
            BlacksmithTabPanel.Instance?.SwitchTab(0);
            return true;
        }

        return false;
    }

    private int CountStonesFromInvSlot(int invSlotIndex)
    {
        int count = 0;
        foreach (var s in stoneSlots)
            if (s != null && !s.IsEmpty && s.InventorySlotIndex == invSlotIndex) count++;
        return count;
    }

    private static List<UpgradeMaterialUsageDto> BuildMaterialUsageList(IEnumerable<int> slotIndices)
    {
        var usageMap = new Dictionary<int, int>();
        if (slotIndices != null)
        {
            foreach (int slotIndex in slotIndices)
            {
                if (!usageMap.ContainsKey(slotIndex))
                    usageMap[slotIndex] = 0;
                usageMap[slotIndex]++;
            }
        }

        var result = new List<UpgradeMaterialUsageDto>(usageMap.Count);
        foreach (var usage in usageMap)
        {
            result.Add(new UpgradeMaterialUsageDto
            {
                slotIndex = usage.Key,
                count = usage.Value,
            });
        }

        return result;
    }

    // ══════════════════════════════════════════════════════════════
    // XEM TRƯỚC
    // ══════════════════════════════════════════════════════════════

    private void OnPreviewClicked()
    {
        if (_equippedItem == null)
        {
            SetStatus("Chọn trang bị trước để xem trước.", Color.yellow);
            return;
        }
        ShowPreview(showCurrentLevel: false);
    }

    private void ShowPreview(bool showCurrentLevel)
    {
        if (previewPanel == null) return;

        int displayLevel = showCurrentLevel
            ? _equippedItem.upgradeLevel
            : _equippedItem.upgradeLevel + 1;

        if (previewNameText != null)
        {
            var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(_equippedItem.id);
            string name = tmpl != null ? tmpl.name : $"Item #{_equippedItem.id}";
            previewNameText.text = showCurrentLevel
                ? $"{name} +{displayLevel} (hiện tại)"
                : $"{name} +{displayLevel} (dự đoán sau cường hóa)";
        }

        if (previewStatsText != null)
            previewStatsText.text = BuildPreviewStatsString(displayLevel, showCurrentLevel);

        previewPanel.SetActive(true);
    }

    private string BuildPreviewStatsString(int displayLevel, bool isCurrentLevel)
    {
        if (string.IsNullOrEmpty(_equippedItem?.strOptions) || _optionCache == null)
            return string.Empty;
        var sb = new System.Text.StringBuilder();
        string hexColor = isCurrentLevel ? "#ffffff" : "#ffd700";
        var parsed = EquippedOptionDisplay.ParseAll(_equippedItem.strOptions);
        foreach (var opt in parsed)
        {
            var tmpl = _optionCache.Find(t => t.id == opt.optionId);
            if (tmpl == null) continue;
            int val = isCurrentLevel ? opt.value : tmpl.GetValueAt(displayLevel);
            sb.AppendLine($"<color={hexColor}>{tmpl.BuildLabel(val)}</color>");
        }
        return sb.ToString().TrimEnd();
    }

    private void HidePreview()
    {
        if (previewPanel != null) previewPanel.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    // LOAD CONFIG
    // ══════════════════════════════════════════════════════════════

    private IEnumerator LoadConfigAndRefresh()
    {
        SetStatus("Đang tải config...", Color.gray);
        upgradeButton.interactable = false;

        if (_optionCache == null)
            yield return StartCoroutine(LoadOptionTemplates());

        bool ok = false;
        yield return StartCoroutine(LoadUpgradeConfig(_equippedItem.upgradeLevel + 1, r => ok = r));

        if (ok)
        {
            AutoFillStones();
            RefreshRateDisplay();
            SetStatus("", Color.white);
        }
        else
        {
            SetStatus("Không tải được config.", Color.red);
        }
    }

    private IEnumerator LoadOptionTemplates()
    {
        bool done = false;
        APIClient.Instance.GetOptionTemplates(
            onSuccess: arr => { _optionCache = new List<OptionTemplateDto>(arr); done = true; },
            onError:   _   => { _optionCache = new List<OptionTemplateDto>(); done = true; }
        );
        yield return new WaitUntil(() => done);
    }

    private IEnumerator LoadUpgradeConfig(int targetLevel, System.Action<bool> onDone)
    {
        bool done = false, success = false;
        APIClient.Instance.GetUpgradeConfig(
            itemId: _equippedItem.id, targetLevel: targetLevel,
            onSuccess: cfg => { _config = cfg; success = true; done = true; },
            onError:   _   => { _config = null; done = true; }
        );
        yield return new WaitUntil(() => done);
        onDone?.Invoke(success);
    }

    // ══════════════════════════════════════════════════════════════
    // TỰ ĐIỀN ĐÁ
    // ══════════════════════════════════════════════════════════════

    private void AutoFillStones()
    {
        if (_inventoryCache == null || _config == null) return;
        int slotIdx = 0, filled = 0;
        foreach (var inv in _inventoryCache)
        {
            if (inv.id != _config.stoneId || inv.quantity <= 0) continue;
            int toFill = Mathf.Min(inv.quantity, _config.stoneNeeded - filled);
            for (int i = 0; i < toFill && slotIdx < stoneSlots.Length; i++, slotIdx++, filled++)
            {
                stoneSlots[slotIdx].SetItem(inv);
                _stoneArrayIdxToInvSlotIdx[slotIdx] = inv.slotIndex;
            }
            if (filled >= _config.stoneNeeded) break;
        }
    }

    private void ClearAllStoneSlots()
    {
        _stoneArrayIdxToInvSlotIdx.Clear();
        foreach (var s in stoneSlots) s?.Clear();
    }

    // ══════════════════════════════════════════════════════════════
    // RATE DISPLAY
    // ══════════════════════════════════════════════════════════════

    private void RefreshRateDisplay()
    {
        bool hasEquip  = _equippedItem != null;
        bool hasCharm  = _charmSlot != null;
        int  stoneCnt  = CountTotalStones();
        int  percent   = 0;

        if (hasEquip)
        {
            if (upgradeStoneConfig != null)
            {
                percent = upgradeStoneConfig.CalcSuccessPercent(
                    _equippedItem.id, _equippedItem.upgradeLevel,
                    CollectPlacedStoneIds(), hasCharm);
            }
            else if (_config != null)
            {
                float ratio = _config.stoneNeeded > 0
                    ? Mathf.Min((float)stoneCnt / _config.stoneNeeded, 1f) : 0f;
                float raw = _config.baseSuccessRate * ratio + (hasCharm ? 0.03f : 0f);
                percent = Mathf.RoundToInt(Mathf.Clamp01(raw) * 100f);
            }
        }

        if (rateBar)  rateBar.value = percent / 100f;
        if (rateText) rateText.text = $"{percent}%";

        if (_config != null)
        {
            int silver = GameManager.Instance?.currentPlayerData?.silver ?? 0;
            bool enoughSilver = silver >= _config.silverCost;
            if (silverCostText) { silverCostText.text = $"Bạc cần: {_config.silverCost:N0}"; silverCostText.color = enoughSilver ? Color.white : Color.red; }
            if (silverOwnText)  silverOwnText.text  = $"Bạn có: {silver:N0}";
            if (failWarningObj) failWarningObj.SetActive(_config.failPolicy > 0);
            upgradeButton.interactable = hasEquip && stoneCnt >= _config.stoneMin && enoughSilver;
        }
        else
        {
            if (silverCostText) silverCostText.text = "";
            if (silverOwnText)  silverOwnText.text  = "";
            if (failWarningObj) failWarningObj.SetActive(false);
            upgradeButton.interactable = false;
        }
    }

    private int CountTotalStones()
    {
        int n = 0;
        foreach (var s in stoneSlots) if (s != null && !s.IsEmpty) n++;
        return n;
    }

    private List<int> CollectPlacedStoneIds()
    {
        var ids = new List<int>();
        foreach (var s in stoneSlots)
            if (s != null && !s.IsEmpty && s.ItemData != null)
                ids.Add(s.ItemData.id);
        return ids;
    }

    // ══════════════════════════════════════════════════════════════
    // CƯỜNG HÓA
    // ══════════════════════════════════════════════════════════════

    public void OnUpgradeClicked()
    {
        if (_equippedItem == null) return;
        upgradeButton.interactable = false;
        SetStatus("Đang cường hóa...", Color.gray);

        int playerId = GameManager.Instance?.currentPlayerData?.player_id ?? 0;

        var stoneIndices = new List<int>(_stoneArrayIdxToInvSlotIdx.Values);
        var stoneUsages = BuildMaterialUsageList(stoneIndices);
        var charmIndices = new List<int>();
        if (_charmSlot != null) charmIndices.Add(_charmSlot.slotIndex);
        var charmUsages = BuildMaterialUsageList(charmIndices);

        int clientPercent = upgradeStoneConfig != null
            ? upgradeStoneConfig.CalcSuccessPercent(
                _equippedItem.id,
                _equippedItem.upgradeLevel,
                CollectPlacedStoneIds(),
                _charmSlot != null)
            : 0;

        if (clientPercent <= 0 && rateText != null && int.TryParse(rateText.text.Replace("%", "").Trim(), out int p))
            clientPercent = p;

        var request = new UpgradeRequestDto
        {
            playerId          = playerId,
            slotKey           = _slotKey,
            isFromInventory   = _isFromInventory,
            stoneSlotIndices  = stoneIndices,
            charmSlotIndices  = charmIndices,
            stoneUsages       = stoneUsages,
            charmUsages       = charmUsages,
            clientRatePercent = clientPercent
        };

        APIClient.Instance.UpgradeEquipment(
            request,
            onSuccess: HandleUpgradeResponse,
            onError:   err => { upgradeButton.interactable = true; SetStatus($"Lỗi: {err}", Color.red); }
        );
    }

    private void HandleUpgradeResponse(UpgradeResponseDto resp)
    {
        if (resp.success)
        {
            SetStatus($"✨ Thành công! Đạt +{resp.newUpgradeLevel}", new Color(1f, 0.85f, 0f));
            _equippedItem.upgradeLevel = resp.newUpgradeLevel;
            _equippedItem.strOptions   = resp.updatedStrOptions;
            ApplyEquippedItem(_equippedItem);
        }
        else
        {
            string msg = resp.downgraded
                ? $"💔 Thất bại! Xuống +{resp.newUpgradeLevel}"
                : "😞 Thất bại! Trang bị không đổi.";
            SetStatus(msg, resp.downgraded ? Color.red : new Color(1f, 0.5f, 0f));
            if (resp.downgraded)
            {
                _equippedItem.upgradeLevel = resp.newUpgradeLevel;
                _equippedItem.strOptions   = resp.updatedStrOptions;
                ApplyEquippedItem(_equippedItem);
            }
        }

        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        bridge?.InvalidateInventoryCache();

        if (resp.updatedInventory != null) _inventoryCache = resp.updatedInventory;
        ClearReservedMaterialCounts();

        var invUI = FindObjectOfType<InventoryUI>(true);
        if (invUI != null && resp.updatedInventory != null)
            invUI.SetInventoryData(resp.updatedInventory);
        else
            bridge?.RefreshInventoryFromDB();

        var pd = GameManager.Instance?.currentPlayerData;
        if (pd != null)
        {
            pd.silver = resp.silver;
            if (resp.final_stats != null)
            {
                pd.final_stats = resp.final_stats;
                var fs = resp.final_stats;
                foreach (var sync in FindObjectsOfType<NetworkPlayerDataSync>())
                    if (sync.IsOwner)
                    {
                        sync.UpdatePlayerDataServerRpc(
                            pd.player_id, pd.element_type ?? "Fire", pd.gender ?? "Male",
                            pd.character_name ?? "", pd.level,
                            fs.hp, fs.max_hp, fs.mp, fs.max_mp,
                            fs.attack, fs.defense, fs.move_speed, pd.gene_tier);
                        break;
                    }
                FindObjectOfType<StatsTabUI>()?.Load();
            }
        }

        _charmSlot = null;
        ResetCharmDisplay();
        StartCoroutine(ReloadAfterDelay(1.0f));
    }

    private IEnumerator ReloadAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ClearAllStoneSlots();
        bool ok = false;
        yield return StartCoroutine(LoadUpgradeConfig(_equippedItem.upgradeLevel + 1, r => ok = r));
        if (ok) AutoFillStones();
        RefreshRateDisplay();
    }

    // ══════════════════════════════════════════════════════════════
    // HỦY
    // ══════════════════════════════════════════════════════════════

    private void OnCancelClicked()
    {
        CloseFromTabPanel();
        gameObject.SetActive(false);
    }

    // ══════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════

    private void NormalizeStoneSlots()
    {
        Transform stoneGrid = FindStoneGridTransform();
        if (stoneGrid == null)
        {
            if (stoneSlots == null || stoneSlots.Length != 16)
                stoneSlots = new UpgradeStoneSlot[16];
            return;
        }

        var orderedSlots = new List<UpgradeStoneSlot>();
        for (int childIndex = 0; childIndex < stoneGrid.childCount; childIndex++)
        {
            Transform child = stoneGrid.GetChild(childIndex);
            UpgradeStoneSlot slot = child.GetComponent<UpgradeStoneSlot>() ?? child.GetComponentInChildren<UpgradeStoneSlot>(true);
            if (slot != null)
                orderedSlots.Add(slot);
        }

        if (orderedSlots.Count == 0)
            orderedSlots.AddRange(stoneGrid.GetComponentsInChildren<UpgradeStoneSlot>(true));

        stoneSlots = new UpgradeStoneSlot[16];
        for (int index = 0; index < orderedSlots.Count; index++)
        {
            bool shouldStayVisible = index < stoneSlots.Length;
            orderedSlots[index].gameObject.SetActive(shouldStayVisible);
            if (shouldStayVisible)
                stoneSlots[index] = orderedSlots[index];
        }

        var gridLayout = stoneGrid.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 4;
        }

        if (orderedSlots.Count > stoneSlots.Length)
            Debug.LogWarning($"[UpgradePanel] StoneGrid dang co {orderedSlots.Count} slot. Runtime se chi dung 16 slot dau tien va an phan du.");
    }

    private Transform FindStoneGridTransform()
    {
        if (stoneSlots != null)
        {
            foreach (var slot in stoneSlots)
            {
                if (slot != null && slot.transform.parent != null)
                    return slot.transform.parent;
            }
        }

        Transform stoneGrid = transform.Find("StoneGrid");
        if (stoneGrid != null) return stoneGrid;

        UpgradeStoneSlot firstSlot = GetComponentInChildren<UpgradeStoneSlot>(true);
        return firstSlot != null ? firstSlot.transform.parent : null;
    }

    private void DisableTransparentRootRaycast()
    {
        var rootImage = GetComponent<Image>();
        if (rootImage != null && rootImage.color.a <= 0.001f)
            rootImage.raycastTarget = false;
    }

    private void EnsureBlacksmithUpgradeTabVisible()
    {
        if (BlacksmithTabPanel.Instance != null)
        {
            if (!BlacksmithTabPanel.Instance.gameObject.activeInHierarchy)
                BlacksmithTabPanel.Instance.Open(0);
            else
                BlacksmithTabPanel.Instance.SwitchTab(0);
        }

        Transform current = transform;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private UpgradeStoneSlot FindFirstEmptyStoneSlot()
    {
        foreach (var stoneSlot in stoneSlots)
        {
            if (stoneSlot != null && stoneSlot.IsEmpty)
                return stoneSlot;
        }
        return null;
    }

    private bool ReserveReservedMaterial(InventorySlotDto slot)
    {
        if (slot == null) return false;

        RememberInventorySlot(slot);

        InventorySlotDto source = FindInventorySlot(slot.slotIndex) ?? slot;
        int sourceQuantity = Mathf.Max(source.quantity, slot.quantity);
        if (sourceQuantity <= 0)
            return false;

        int reserved = _reservedMaterialCounts.TryGetValue(slot.slotIndex, out int currentReserved)
            ? currentReserved
            : 0;

        int available = Mathf.Max(0, sourceQuantity - reserved);
        if (available <= 0)
            return false;

        _reservedMaterialCounts[slot.slotIndex] = reserved + 1;
        SyncReservedMaterialCountsToInventoryUI();
        return true;
    }

    private void ReleaseReservedMaterial(InventorySlotDto slot)
    {
        if (slot == null) return;

        if (_reservedMaterialCounts.TryGetValue(slot.slotIndex, out int reserved) && reserved > 0)
        {
            reserved--;
            if (reserved <= 0)
                _reservedMaterialCounts.Remove(slot.slotIndex);
            else
                _reservedMaterialCounts[slot.slotIndex] = reserved;

            SyncReservedMaterialCountsToInventoryUI();
        }
    }

    private void ClearReservedMaterialCounts()
    {
        if (_reservedMaterialCounts.Count == 0)
            return;

        _reservedMaterialCounts.Clear();
        SyncReservedMaterialCountsToInventoryUI();
    }

    private void SyncReservedMaterialCountsToInventoryUI()
    {
        var invUI = FindObjectOfType<InventoryUI>(true);
        invUI?.SetReservedQuantities(_reservedMaterialCounts);
    }

    private InventorySlotDto FindInventorySlot(int slotIndex)
    {
        _inventoryCache = GetFreshInventorySnapshot(_inventoryCache);
        if (_inventoryCache == null) return null;
        foreach (var slot in _inventoryCache)
        {
            if (slot != null && slot.slotIndex == slotIndex)
                return slot;
        }
        return null;
    }

    private void RememberInventorySlot(InventorySlotDto slot)
    {
        if (slot == null)
            return;

        InventorySlotDto[] snapshot = GetFreshInventorySnapshot(_inventoryCache);
        if (snapshot == null || snapshot.Length == 0)
        {
            _inventoryCache = new[] { slot };
            return;
        }

        _inventoryCache = snapshot;
    }

    private InventorySlotDto[] GetFreshInventorySnapshot(InventorySlotDto[] preferred)
    {
        if (HasInventorySnapshot(preferred))
            return preferred;

        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        if (HasInventorySnapshot(bridge?.CurrentInventory))
            return bridge.CurrentInventory;

        var inventoryUI = FindObjectOfType<InventoryUI>(true);
        if (HasInventorySnapshot(inventoryUI?.CurrentSlots))
            return inventoryUI.CurrentSlots;

        return preferred ?? _inventoryCache;
    }

    private static bool HasInventorySnapshot(InventorySlotDto[] inventory)
    {
        return inventory != null && inventory.Length > 0;
    }

    private string GetInventorySlotDisplayName(InventorySlotDto slot, ItemTemplateDto template = null)
    {
        template ??= ItemTemplateManager.Instance?.GetItemTemplate(slot.id);
        if (template != null && !string.IsNullOrEmpty(template.name))
            return template.name;

        if (!string.IsNullOrEmpty(slot?.itemCode))
            return slot.itemCode;

        return slot != null && slot.id > 0 ? $"Item #{slot.id}" : "vật liệu";
    }

    private Sprite ResolveInventorySlotIcon(InventorySlotDto slot, ItemTemplateDto template = null)
    {
        if (slot == null || IconDatabase.Instance == null)
            return null;

        if (!string.IsNullOrEmpty(slot.iconId))
        {
            var icon = IconDatabase.Instance.GetIcon(slot.iconId);
            if (icon != null)
                return icon;
        }

        template ??= ItemTemplateManager.Instance?.GetItemTemplate(slot.id);
        if (template != null && template.idIcon > 0)
            return IconDatabase.Instance.GetIcon(template.idIcon.ToString());

        return null;
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    // ── Item Detail Popup ─────────────────────────────────────────

    private ItemDetailPanel GetOrCreateDetailPanel()
    {
        if (_detailPanelInstance != null) return _detailPanelInstance;
        if (itemDetailPanelPrefab == null) return null;
        _detailPanelInstance = Instantiate(itemDetailPanelPrefab, transform.root);
        return _detailPanelInstance;
    }

    // ── Equip Info Box ────────────────────────────────────────────

    private void ShowEquipInfoBox()
    {
        if (equipInfoBox == null || _equippedItem == null) return;
        var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(_equippedItem.id);
        string name = tmpl != null ? tmpl.name : $"Item #{_equippedItem.id}";
        if (equipInfoTitleText) equipInfoTitleText.text = $"{name} (+{_equippedItem.upgradeLevel})";
        equipInfoBox.SetActive(true);
    }

    private void HideEquipInfoBox()
    {
        if (equipInfoBox != null) equipInfoBox.SetActive(false);
    }

    // ── Charm Info Box ────────────────────────────────────────────

    private void ShowCharmInfoBox()
    {
        if (charmInfoBox == null || _charmSlot == null) return;
        var tmpl = ItemTemplateManager.Instance?.GetItemTemplate(_charmSlot.id);
        string name = tmpl != null ? tmpl.name : (_charmSlot.itemCode ?? $"Item #{_charmSlot.id}");
        if (charmInfoTitleText) charmInfoTitleText.text = name;
        charmInfoBox.SetActive(true);
    }

    private void HideCharmInfoBox()
    {
        if (charmInfoBox != null) charmInfoBox.SetActive(false);
    }
}

// ════════════════════════════════════════════════════════════════
// INSPECTOR CHECKLIST – UpgradePanel (gắn lên BlacksmithPanel/PanelCuongHoa)
// ════════════════════════════════════════════════════════════════
//
// [Ô Trang Bị]
//   Equip Slot Button       → Button (root ô, click khi trống → tab Trang Bị)
//   Equip Slot Icon         → Image
//   Equip Slot Name Text    → TMP_Text "Chọn trang bị..."
//   Upgrade Level Text      → TMP_Text "+3"
//   Equip Remove Button     → Button "Lấy Ra"   (ẩn mặc định)
//   Equip View Stats Button → Button "Xem TT"   (ẩn mặc định)
//
// [Ô Bùa]
//   Charm Slot Button       → Button
//   Charm Slot Icon         → Image
//   Charm Slot Name Text    → TMP_Text "Bùa cường hóa"
//   Charm Remove Button     → Button "Lấy Ra"   (ẩn mặc định)
//   Charm View Button       → Button "Xem"      (ẩn mặc định)
//
// [Preview Panel]
//   Preview Panel           → GameObject (ẩn mặc định)
//   Preview Name Text       → TMP_Text
//   Preview Stats Container → Transform (VerticalLayoutGroup)
//   Preview Stat Row Prefab → StatRowEntry prefab
//
// [Stone Grid 16]
//   Stone Slots [0..15]     → 16 x UpgradeStoneSlot
//
// [Nút Chính]
//   Preview Button  → Button "XEM TRƯỚC"
//   Upgrade Button  → Button "CƯỜNG HÓA"
//   Cancel Button   → Button "HỦY"
//
// [Rate & Cost]
//   Rate Bar         → Slider (Interactable=false)
//   Rate Text        → TMP_Text
//   Silver Cost Text → TMP_Text
//   Silver Own Text  → TMP_Text
//   Fail Warning Obj → GameObject
//
// [Config]
//   Upgrade Stone Config → UpgradeStoneConfig asset
//
// [Status]
//   Status Text → TMP_Text
