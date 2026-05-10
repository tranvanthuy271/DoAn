using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;
using System.Collections.Generic;

/// <summary>
/// NPC shop UI panel -- pure UI layer, no direct API calls.
///
/// Data flow:
///   Server pushes NpcData via NpcInteraction.OpenMenuClientRpc -> Open()
///   Tab "Cua hang" -> LoadShopServerRpc -> ShowShopClientRpc -> ShowShop()
///   Click item cell -> BuyItemServerRpc -> BuyResultClientRpc -> OnBuyResult()
///   Tab "Tui" -> shows player bag panel (connects to inventory system)
///
/// Inspector setup: see HUONG_DAN_NPC_SHOP_UNITY.md section 5.
/// </summary>
public class NpcMenuUI : MonoBehaviour
{
    private const string LogPrefix = "[NpcMenuUI]";

    public static NpcMenuUI Instance { get; private set; }

    // ── Main panel ──────────────────────────────────────────────────────
    [Header("Panel chinh")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TMP_Text   npcNameText;
    [SerializeField] private TMP_Text   dialogueText;
    [SerializeField] private Button     btnClose;

    // ── Tab buttons ──────────────────────────────────────────────────────
    [Header("Tabs (Cua hang | Tui)")]
    [SerializeField] private Button     btnTabShop;
    [SerializeField] private Button     btnTabBag;

    // ── Shop panel ──────────────────────────────────────────────────────
    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform  shopItemContainer; // Content with GridLayoutGroup
    [SerializeField] private GameObject shopItemRowPrefab; // ShopItemCell prefab
    [SerializeField] private GameObject elementFilterBarPrefab; // ElementFilterBar.prefab
    [SerializeField] private GameObject equipTypeFilterBarPrefab; // EquipTypeFilterBar.prefab

    // ── Bag panel ──────────────────────────────────────────────────────
    [Header("Tui Panel")]
    [SerializeField] private GameObject bagPanel;

    // ── Icons ──────────────────────────────────────────────────────────
    [Header("Icons")]
    [SerializeField] private Sprite     defaultItemIcon;
    // ── Item Detail Panel (dùng chung với túĩ đồ) ──────────────────────
    [Header("Item Detail (dùng chung với túĩ đồ)")]
    [Tooltip("Kéo ItemDetailPanel prefab/instance vào đây. Khi nhấn vào icon item trong shop sẽ hiện panel này.")]
    [SerializeField] private ItemDetailPanel itemDetailPanel;
    // ── Feedback ──────────────────────────────────────────────────────
    [Header("Thong bao (tuy chon)")]
    [SerializeField] private TMP_Text   feedbackText;
    [SerializeField] private float      feedbackDuration = 2f;

    /// <summary>True khi panel NPC đang hiển thị — dùng để ngăn NpcInteraction nhận click xuyên.</summary>
    public bool IsOpen => mainPanel != null && mainPanel.activeSelf;

    private NpcInteraction _currentInteraction;
    private Coroutine      _feedbackCoroutine;

    // ── Element filter state ───────────────────────────────────────
    private GameObject                        _filterBarGo;
    private readonly List<(GameObject go, int elemClass)> _shopCellsWithClass = new List<(GameObject, int)>();
    private int   _activeElementFilter    = 0;
    private float _originalScrollOffsetTop = 0f;
    private bool  _scrollOffsetModified    = false;
    // ── Equip type filter state ──────────────────────────
    private GameObject                        _equipFilterBarGo;
    private readonly List<(GameObject go, int equipType)> _shopCellsWithEquipType = new List<(GameObject, int)>();
    private int   _activeEquipTypeFilter   = -1;  // -1 = Tất Cả
    private float _originalScrollOffsetTop2 = 0f;
    private bool  _scrollOffsetModified2    = false;
    // ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        // Re-assign Instance in case this GameObject was inactive at scene start
        if (Instance == null) Instance = this;
    }

    /// <summary>
    /// Lazy singleton fallback — Awake() never fires on inactive GameObjects.
    /// NpcInteraction uses this instead of Instance directly.
    /// </summary>
    public static NpcMenuUI GetOrFind()
    {
        if (Instance != null) return Instance;
        Instance = FindObjectOfType<NpcMenuUI>(true); // true = include inactive
        if (Instance != null) Instance.gameObject.SetActive(false); // keep hidden until Open()
        return Instance;
    }

    private bool _initialized;

    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        btnClose.onClick.AddListener(Close);
        if (btnTabShop) btnTabShop.onClick.AddListener(ShowShopTab);
        if (btnTabBag)  btnTabBag.onClick.AddListener(ShowBagTab);
        ApplyTheme();
        // Hide all sub-panels here so the guard runs on the first Open() call
        // regardless of whether the GameObject was active at scene start.
        if (mainPanel)   mainPanel.SetActive(false);
        if (shopPanel)   shopPanel.SetActive(false);
        if (bagPanel)    bagPanel.SetActive(false);
        if (feedbackText) feedbackText.gameObject.SetActive(false);
    }

    private void ApplyTheme()
    {
        if (mainPanel == null)
        {
            return;
        }

        UIRuntimeAssetHelper.ApplyNotoSans(mainPanel.GetComponentsInChildren<TMP_Text>(true));
    }

    private void Start()
    {
        EnsureInitialized();
        // mainPanel is hidden inside EnsureInitialized() so Start() has nothing extra to do.
    }

    // ── Open / Close ──────────────────────────────────────────────────

    /// <summary>Called by NpcInteraction.OpenMenuClientRpc.</summary>
    public void Open(NpcData npc, NpcInteraction interaction)
    {
        if (npc == null)
        {
            Debug.LogWarning($"{LogPrefix} Open called with null npc.", this);
            return;
        }

        EnsureInitialized();   // hides mainPanel on first call; safe to call on inactive objects
        _currentInteraction = interaction;
        npcNameText.text  = npc.npc_name;
        dialogueText.text = !string.IsNullOrEmpty(npc.dialogue_text)
            ? npc.dialogue_text
            : "Xin chao, ta co the giup gi cho nguoi?";

        Debug.Log(
            $"{LogPrefix} Open | npcId={npc.npc_id} name='{npc.npc_name}' type='{npc.npc_type}' interactionFound={interaction != null}",
            this);

        BlacksmithFunctionMenuPanel.Instance?.Close();

        // Dungeon NPC: mở panel phó bản riêng — KHÔNG kích hoạt root NpcMenuUI
        if (string.Equals(npc.npc_type, "dungeon", StringComparison.OrdinalIgnoreCase))
        {
            var dungeonMenu = DungeonNpcMenuUI.GetOrCreate();
            if (dungeonMenu != null)
            {
                Debug.Log($"{LogPrefix} Route -> DungeonNpcMenuUI for npcId={npc.npc_id}.", this);
                dungeonMenu.Open(npc);
            }
            else
                Debug.LogWarning($"{LogPrefix} Không tìm thấy DungeonNpcMenuUI trong scene!", this);
            return;
        }

        // Blacksmith NPC: mở menu chức năng riêng — KHÔNG kích hoạt root NpcMenuUI
        if (string.Equals(npc.npc_type, "blacksmith", StringComparison.OrdinalIgnoreCase))
        {
            BlacksmithFunctionMenuPanel menu = BlacksmithFunctionMenuPanel.GetOrCreate();
            if (menu != null)
            {
                menu.Open();
            }
            else if (BlacksmithTabPanel.Instance != null)
            {
                BlacksmithTabPanel.Instance.Open(0);
            }
            else
            {
                var upgradePanel = FindObjectOfType<UpgradePanel>(true);
                if (upgradePanel != null)
                {
                    var bridge = FindObjectOfType<InventoryNetworkBridge>();
                    var inv = bridge != null ? bridge.CurrentInventory : null;
                    upgradePanel.OpenEmpty(inv);
                }
                else
                {
                    Debug.LogWarning("[NpcMenuUI] Không tìm thấy menu thợ rèn hoặc panel fallback trong scene!");
                }
            }
            return; // không mở NPC menu thông thường
        }

        // Non-blacksmith: kích hoạt root và hiện mainPanel
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        mainPanel.SetActive(true);
        ShowShopTab();
    }

    public void Close()
    {
        Debug.Log($"{LogPrefix} Close root NPC menu.", this);
        mainPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (bagPanel)  bagPanel.SetActive(false);
        BlacksmithFunctionMenuPanel.Instance?.Close();
        HideItemDetailPanelIfOpen();
        _currentInteraction = null;
        _activeElementFilter = 0;
        _activeEquipTypeFilter = -1;
    }

    /// <summary>
    /// Mở trực tiếp shop panel (không qua tab selection) — gọi từ NpcInteraction.ShowShopClientRpc
    /// sau khi dynamic menu đã đóng và shop data đã sẵn sàng.
    /// </summary>
    public void OpenShopDirect(NpcInteraction interaction)
    {
        EnsureInitialized();
        _currentInteraction = interaction;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        mainPanel.SetActive(true);
        if (shopPanel) shopPanel.SetActive(true);
        if (bagPanel)  bagPanel.SetActive(false);
        HideItemDetailPanelIfOpen();
        ClearShopItems();
        Debug.Log($"{LogPrefix} OpenShopDirect called.", this);
    }

    // ── Tabs ──────────────────────────────────────────────────────────

    private void ShowShopTab()
    {
        if (shopPanel) shopPanel.SetActive(true);
        if (bagPanel)  bagPanel.SetActive(false);
        HideItemDetailPanelIfOpen();
        ClearShopItems();
        _currentInteraction?.LoadShopServerRpc();
    }

    private void ShowBagTab()
    {
        if (shopPanel) shopPanel.SetActive(false);
        if (bagPanel)  bagPanel.SetActive(true);
        HideItemDetailPanelIfOpen();
        // TODO: connect to inventory system to display player bag
    }

    // ── Shop ──────────────────────────────────────────────────────────

    private void ClearShopItems()
    {
        foreach (Transform child in shopItemContainer)
            Destroy(child.gameObject);

        _shopCellsWithClass.Clear();
        _shopCellsWithEquipType.Clear();

        // Destroy element filter bar and restore scroll position
        if (_filterBarGo != null)
        {
            Destroy(_filterBarGo);
            _filterBarGo = null;
        }
        RestoreShopScrollOffset();

        // Destroy equip type filter bar and restore scroll position
        if (_equipFilterBarGo != null)
        {
            Destroy(_equipFilterBarGo);
            _equipFilterBarGo = null;
        }
        RestoreShopScrollOffset2();
    }

    /// <summary>Called by NpcInteraction.ShowShopClientRpc with a JSON array of shop items.</summary>
    public void ShowShop(string shopItemsJson)
    {
        ClearShopItems();

        ShopListWrapper resp;
        try
        {
            resp = JsonUtility.FromJson<ShopListWrapper>("{\"items\":" + shopItemsJson + "}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NpcMenuUI] Parse shop data error: {ex.Message}");
            ShowFeedback("Cannot load shop. Try again later!", new Color(1f, 0.4f, 0.4f));
            return;
        }

        if (resp?.items == null || resp.items.Length == 0)
        {
            ShowFeedback("This shop has no items.", new Color(1f, 0.85f, 0f));
            return;
        }

        // Clear stale feedback
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        if (_feedbackCoroutine != null) { StopCoroutine(_feedbackCoroutine); _feedbackCoroutine = null; }

        // Update shop tab title from first item's shop_name
        string shopName = resp.items[0].shop_name;
        if (!string.IsNullOrWhiteSpace(shopName) && btnTabShop != null)
        {
            var tabTxt = btnTabShop.GetComponentInChildren<TMP_Text>(true);
            if (tabTxt != null) tabTxt.text = shopName;
        }

        // Check whether any item has an element class (triggers element filter bar)
        bool hasElements = false;
        foreach (var i in resp.items)
            if (i.element_class > 0) { hasElements = true; break; }
        if (!hasElements) _activeElementFilter = 0;
        CreateElementFilterBar(hasElements);

        // Check whether any item is equippable (type 0-5) — triggers equip-type filter bar
        bool hasEquipTypes = false;
        foreach (var i in resp.items)
            if (i.equip_type >= 0 && i.equip_type <= 5) { hasEquipTypes = true; break; }
        if (!hasEquipTypes) _activeEquipTypeFilter = -1;
        CreateEquipTypeFilterBar(hasEquipTypes);

        _shopCellsWithEquipType.Clear();

        foreach (var item in resp.items)
        {
            var cellGO = Instantiate(shopItemRowPrefab, shopItemContainer);
            var cell   = cellGO.GetComponent<ShopItemRowUI>();
            if (cell == null)
            {
                Debug.LogError("[NpcMenuUI] ShopItemCell prefab missing ShopItemRowUI component!");
                continue;
            }

            cell.EnsureVisualsConfigured();
            cell.ElementClass = item.element_class;

            if (cell.itemIcon != null)
            {
                var loaded = Resources.Load<Sprite>($"ItemIcons/{item.icon_id}");
                cell.SetItemIcon(loaded != null ? loaded : defaultItemIcon);
            }

            if (cell.itemName != null)
                cell.itemName.text = item.item_name;

            if (cell.price != null)
                cell.price.text = item.price_gold > 0
                    ? item.price_gold.ToString()
                    : item.price_silver.ToString();

            if (cell.btnBuy != null)
            {
                cell.btnBuy.interactable = item.can_afford && item.meets_level;
                var capturedItem = item;
                cell.btnBuy.onClick.AddListener(() => TryBuyShopItem(capturedItem));
            }

            // Nhấn vào icon item -> hiện ItemDetailPanel
            if (cell.itemIcon != null)
            {
                var iconBtn = cell.itemIcon.gameObject.GetComponent<Button>()
                              ?? cell.itemIcon.gameObject.AddComponent<Button>();
                iconBtn.transition   = Selectable.Transition.None;
                iconBtn.targetGraphic = cell.itemIcon;
                var capturedForInfo  = item;
                iconBtn.onClick.RemoveAllListeners();
                iconBtn.onClick.AddListener(() => ShowShopItemDetail(capturedForInfo));
            }

            _shopCellsWithClass.Add((cellGO, item.element_class));
            _shopCellsWithEquipType.Add((cellGO, item.equip_type));
        }

        // Re-apply filters after rebuild (e.g. shop reload following a purchase)
        if (_activeElementFilter != 0)
            ApplyElementFilter(_activeElementFilter);
        if (_activeEquipTypeFilter >= 0)
            ApplyEquipTypeFilter(_activeEquipTypeFilter);
    }

    private bool TryBuyShopItem(ShopItem shopItem)
    {
        if (!shopItem.can_afford)
        {
            string needText = shopItem.price_gold > 0
                ? shopItem.price_gold + "g"
                : shopItem.price_silver + "s";
            Debug.Log($"[Shop] Không đủ tiền mua '{shopItem.item_name}'. Cần: {needText}");
            ShowFeedback($"Không đủ tiền mua {shopItem.item_name}.", new Color(1f, 0.4f, 0.4f));
            return false;
        }

        if (!shopItem.meets_level)
        {
            Debug.Log($"[Shop] Chưa đủ level. '{shopItem.item_name}' yêu cầu level {shopItem.required_level}.");
            ShowFeedback($"Cần cấp {shopItem.required_level} để mua {shopItem.item_name}.", new Color(1f, 0.4f, 0.4f));
            return false;
        }

        Debug.Log($"[Shop] Gửi mua: shopItemId={shopItem.shop_item_id} '{shopItem.item_name}'");
        _currentInteraction?.BuyItemServerRpc(shopItem.shop_item_id, 1);
        return true;
    }

    private void ShowShopItemDetail(ShopItem shopItem)
    {
        var inventoryUi = FindObjectOfType<InventoryUI>(true);
        var detailPanel = inventoryUi != null
            ? inventoryUi.GetSharedItemDetailPanel()
            : itemDetailPanel;

        if (detailPanel == null)
        {
            detailPanel = FindObjectOfType<ItemDetailPanel>(true);
        }

        if (detailPanel == null)
        {
            Debug.LogWarning("[NpcMenuUI] Không tìm thấy ItemDetailPanel để hiển thị thông tin item trong shop.");
            return;
        }

        itemDetailPanel = detailPanel;

        var stub = new InventorySlotDto
        {
            id = shopItem.item_template_id,
            iconId = shopItem.icon_id.ToString(),
            itemCode = shopItem.item_name,
            quantity = 1,
            slotIndex = -1
        };

        detailPanel.ShowItem(
            stub,
            showUseButton: true,
            buttonTextOverride: "Mua",
            primaryButtonAction: () =>
            {
                if (TryBuyShopItem(shopItem))
                    detailPanel.Hide();
            });
    }

    private void HideItemDetailPanelIfOpen()
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.Hide();
            return;
        }

        var existingPanel = FindObjectOfType<ItemDetailPanel>(true);
        if (existingPanel != null)
            existingPanel.Hide();
    }

    // ── Element filter bar ───────────────────────────────────────────

    private void CreateElementFilterBar(bool enabled)
    {
        RestoreShopScrollOffset();
        if (_filterBarGo != null) { Destroy(_filterBarGo); _filterBarGo = null; }
        if (!enabled || shopPanel == null) return;

        if (elementFilterBarPrefab == null)
        {
            Debug.LogWarning("[NpcMenuUI] elementFilterBarPrefab chua duoc gan trong Inspector!", this);
            return;
        }

        _filterBarGo = Instantiate(elementFilterBarPrefab, shopPanel.transform);
        _filterBarGo.transform.SetAsLastSibling();

        // Wire up button click callbacks -- direct children: 0=TatCa 1=Hoa 2=Thuy 3=Tho 4=Loi 5=Moc 6=Phong
        int idx = 0;
        foreach (Transform child in _filterBarGo.transform)
        {
            int captured = idx;
            var btn = child.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => ApplyElementFilter(captured));
            idx++;
        }

        // Highlight the currently active filter button
        ApplyElementFilter(_activeElementFilter);

        // Push grid content down so first row clears the filter bar (read height from prefab)
        if (shopItemContainer != null)
        {
            var grid = shopItemContainer.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                if (!_scrollOffsetModified)
                    _originalScrollOffsetTop = grid.padding.top;
                var barRt = _filterBarGo.GetComponent<RectTransform>();
                float barH = (barRt != null) ? Mathf.Abs(barRt.sizeDelta.y) : 48f;
                var p = grid.padding;
                p.top = (int)(_originalScrollOffsetTop + barH);
                grid.padding = p;
                _scrollOffsetModified = true;
            }
        }
    }

    private void RestoreShopScrollOffset()
    {
        if (!_scrollOffsetModified || shopItemContainer == null) return;
        var grid = shopItemContainer.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            var p = grid.padding;
            p.top = (int)_originalScrollOffsetTop;
            grid.padding = p;
        }
        _scrollOffsetModified = false;
    }

    private void ApplyElementFilter(int elementClass)
    {
        _activeElementFilter = elementClass;

        foreach (var (go, elemClass) in _shopCellsWithClass)
        {
            if (go == null) continue;
            go.SetActive(elementClass == 0 || elemClass == elementClass);
        }

        // Update button alphas: selected = full, others = dim
        if (_filterBarGo == null) return;
        int idx = 0;
        foreach (Transform child in _filterBarGo.transform)
        {
            var img = child.GetComponent<Image>();
            if (img != null)
            {
                float a = (idx == elementClass) ? 1f : 0.5f;
                img.color = new Color(img.color.r, img.color.g, img.color.b, a);
            }
            idx++;
        }
    }

    // ── Buy result ────────────────────────────────────────────────────

    // ── Equip type filter bar ─────────────────────────────────────────

    private void CreateEquipTypeFilterBar(bool enabled)
    {
        RestoreShopScrollOffset2();
        if (_equipFilterBarGo != null) { Destroy(_equipFilterBarGo); _equipFilterBarGo = null; }
        if (!enabled || shopPanel == null) return;

        if (equipTypeFilterBarPrefab == null)
        {
            Debug.LogWarning("[NpcMenuUI] equipTypeFilterBarPrefab chua duoc gan trong Inspector!", this);
            return;
        }

        _equipFilterBarGo = Instantiate(equipTypeFilterBarPrefab, shopPanel.transform);
        _equipFilterBarGo.transform.SetAsLastSibling();

        // Children order: 0=TatCa(-1) 1=VuKhi(1) 2=Mu(0) 3=Giap(2) 4=Quan(3) 5=Giay(4) 6=Nhan(5)
        int[] typeMap = new int[] { -1, 1, 0, 2, 3, 4, 5 };
        int idx = 0;
        foreach (Transform child in _equipFilterBarGo.transform)
        {
            if (idx >= typeMap.Length) break;
            int captured = typeMap[idx];
            var btn = child.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => ApplyEquipTypeFilter(captured));
            idx++;
        }

        ApplyEquipTypeFilter(_activeEquipTypeFilter);

        if (shopItemContainer != null)
        {
            var grid = shopItemContainer.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                if (!_scrollOffsetModified2)
                    _originalScrollOffsetTop2 = grid.padding.top;
                var barRt = _equipFilterBarGo.GetComponent<RectTransform>();
                float barH = (barRt != null) ? Mathf.Abs(barRt.sizeDelta.y) : 48f;
                var p = grid.padding;
                p.top = (int)(_originalScrollOffsetTop2 + barH);
                grid.padding = p;
                _scrollOffsetModified2 = true;
            }
        }
    }

    private void RestoreShopScrollOffset2()
    {
        if (!_scrollOffsetModified2 || shopItemContainer == null) return;
        var grid = shopItemContainer.GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            var p = grid.padding;
            p.top = (int)_originalScrollOffsetTop2;
            grid.padding = p;
        }
        _scrollOffsetModified2 = false;
    }

    private void ApplyEquipTypeFilter(int equipType)
    {
        _activeEquipTypeFilter = equipType;

        foreach (var (go, et) in _shopCellsWithEquipType)
        {
            if (go == null) continue;
            go.SetActive(equipType == -1 || et == equipType);
        }

        if (_equipFilterBarGo == null) return;
        int[] typeMap = new int[] { -1, 1, 0, 2, 3, 4, 5 };
        int idx = 0;
        foreach (Transform child in _equipFilterBarGo.transform)
        {
            if (idx >= typeMap.Length) break;
            var img = child.GetComponent<Image>();
            if (img != null)
            {
                float a = (typeMap[idx] == equipType) ? 1f : 0.5f;
                img.color = new Color(img.color.r, img.color.g, img.color.b, a);
            }
            idx++;
        }
    }

    /// <summary>Called by NpcInteraction.BuyResultClientRpc after server processes purchase.</summary>
    public void OnBuyResult(bool success, string message, int newGold)
    {
        if (success)
        {
            ShowFeedback(!string.IsNullOrEmpty(message) ? message : "Mua thanh cong!", Color.green);
            _currentInteraction?.LoadShopServerRpc();   // reload shop (stock, can_afford)
            ItemUseHandler.Instance?.RequestRefreshInventory(); // refresh inventory bag
        }
        else
        {
            ShowFeedback(!string.IsNullOrEmpty(message) ? message : "Mua that bai!",
                new Color(1f, 0.4f, 0.4f));
        }
    }

    // ── Feedback ──────────────────────────────────────────────────────

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        if (_feedbackCoroutine != null) StopCoroutine(_feedbackCoroutine);
        _feedbackCoroutine = StartCoroutine(FeedbackCoroutine(message, color));
    }

    private IEnumerator FeedbackCoroutine(string message, Color color)
    {
        feedbackText.text  = message;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(feedbackDuration);
        feedbackText.gameObject.SetActive(false);
    }

    // ── Serializable DTOs ─────────────────────────────────────────────

    [System.Serializable] private class ShopListWrapper { public ShopItem[] items; }

    [System.Serializable]
    public class ShopItem
    {
        public int    shop_item_id;       // = item_template_id khi dùng JSON config
        public int    item_template_id;
        public int    icon_id;
        public string item_name;
        public string item_detail;
        public int    price_silver;
        public int    price_gold;
        public int    stock;
        public int    required_level;
        public int    element_class;     // idClass: 0=Tất Cả 1=Hỏa 2=Thủy 3=Thổ 4=Lôi 5=Mộc 6=Phong
        public int    equip_type;         // type: 0=Mũ 1=Vũ Khí 2=Giáp 3=Quần 4=Giày 5=Nhẫn; -1 hoặc không phải trang bị
        public string shop_name;         // Tên loại shop, hiển thị trên tab tiêu đề
        public bool   can_afford;
        public bool   meets_level;
    }
}