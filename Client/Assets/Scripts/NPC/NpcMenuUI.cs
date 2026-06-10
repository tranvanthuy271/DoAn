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
    [SerializeField] private GameObject filterBarScene; // Kéo ElementFilterBar scene object vào đây (dùng cho cả hệ và loại trang bị)

    // Kiểm tra filterBarScene an toàn, tránh UnassignedReferenceException của Unity
    private bool HasFilterBar => filterBarScene != null && filterBarScene;
    private void HideFilterBar() { if (HasFilterBar) filterBarScene.SetActive(false); }

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
    private bool           _isUtilityMode = false;
    private Coroutine      _feedbackCoroutine;

    // ── Filter bar state (dùng filterBarScene cho cả 2 chế độ) ───────────────────────
    private readonly List<(GameObject go, int elemClass)>  _shopCellsWithClass     = new List<(GameObject, int)>();
    private readonly List<(GameObject go, int equipType)>  _shopCellsWithEquipType = new List<(GameObject, int)>();
    private readonly List<(Button btn, int value)>         _filterButtons          = new List<(Button, int)>();
    private int   _activeElementFilter   = 0;
    private int   _activeEquipTypeFilter = -1;
    private float _originalScrollOffsetTop = 0f;
    private bool  _scrollOffsetModified    = false;
    // ──────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        UIPanelManager.Register(gameObject, Close);
    }

    private void OnDestroy()
    {
        UIPanelManager.Unregister(gameObject);
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

        // Quest NPC: mở panel nhiệm vụ riêng — KHÔNG kích hoạt root NpcMenuUI
        if (string.Equals(npc.npc_type, "quest", StringComparison.OrdinalIgnoreCase))
        {
            var questPanel = QuestNpcPanel.GetOrCreate();
            if (questPanel != null)
            {
                Debug.Log($"{LogPrefix} Route -> QuestNpcPanel for npcId={npc.npc_id}.", this);
                questPanel.Open(npc);
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} Không tìm thấy QuestNpcPanel trong scene!", this);
            }
            return;
        }

        // Non-blacksmith: kích hoạt root và hiện mainPanel
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        UIPanelManager.CloseOthers(gameObject);
        mainPanel.SetActive(true);
        UIPanelManager.NotifyOpened(gameObject);
        ShowShopTab();
    }

    public void Close()
    {
        Debug.Log($"{LogPrefix} Close root NPC menu.", this);
        if (_isUtilityMode)
        {
            GameplayCommandService.OnUtilityShopReceived  -= ShowShop;
            GameplayCommandService.OnUtilityShopBuyResult -= OnUtilityBuyResult;
            _isUtilityMode = false;
        }
        mainPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (bagPanel)  bagPanel.SetActive(false);
        BlacksmithFunctionMenuPanel.Instance?.Close();
        HideItemDetailPanelIfOpen();
        _currentInteraction = null;
        _activeElementFilter = 0;
        _activeEquipTypeFilter = -1;
        UIPanelManager.NotifyClosed(gameObject);
    }

    /// <summary>
    /// Mở trực tiếp shop panel (không qua tab selection) — gọi từ NpcInteraction.ShowShopClientRpc
    /// sau khi dynamic menu đã đóng và shop data đã sẵn sàng.
    /// </summary>
    public void OpenShopDirect(NpcInteraction interaction)
    {
        EnsureInitialized();
        _currentInteraction = interaction;
        UIPanelManager.CloseOthers(gameObject);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        mainPanel.SetActive(true);
        if (shopPanel) shopPanel.SetActive(true);
        if (bagPanel)  bagPanel.SetActive(false);
        HideItemDetailPanelIfOpen();
        ClearShopItems();
        UIPanelManager.NotifyOpened(gameObject);
        Debug.Log($"{LogPrefix} OpenShopDirect called.", this);
    }
    /// <summary>
    /// Mở shop tiện ích (không cần NPC) từ HUD. Gọi từ UtilityDrawerAutoInstaller khi nhấn nút "Shop".
    /// </summary>
    public void OpenUtilityMode()
    {
        EnsureInitialized();
        _isUtilityMode    = true;
        _currentInteraction = null;
        GameplayCommandService.OnUtilityShopReceived  += ShowShop;
        GameplayCommandService.OnUtilityShopBuyResult += OnUtilityBuyResult;
        UIPanelManager.CloseOthers(gameObject);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        mainPanel.SetActive(true);
        if (shopPanel) shopPanel.SetActive(true);
        if (bagPanel)  bagPanel.SetActive(false);
        HideItemDetailPanelIfOpen();
        ClearShopItems();
        if (npcNameText)  npcNameText.text  = "Cửa Hàng Tiện Ích";
        if (dialogueText) dialogueText.text = "Mua sắm không nào?";
        UIPanelManager.NotifyOpened(gameObject);
        GameplayCommandService.Instance?.LoadUtilityShopServerRpc();
        Debug.Log($"{LogPrefix} OpenUtilityMode called.", this);
    }

    /// <summary>Called via GameplayCommandService.OnUtilityShopBuyResult when server responds to a utility buy.</summary>
    private void OnUtilityBuyResult(string json)
    {
        BuyResultDto result = null;
        try { result = JsonUtility.FromJson<BuyResultDto>(json); } catch { }
        bool success = result != null && result.success;
        string msg   = result?.message ?? (success ? "Mua thành công!" : "Mua thất bại!");
        ShowFeedback(msg, success ? Color.green : new Color(1f, 0.4f, 0.4f));
        if (success)
        {
            GameplayCommandService.Instance?.LoadUtilityShopServerRpc();
            ItemUseHandler.Instance?.RequestRefreshInventory();
        }
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
        // Đóng NPC menu và mở InventoryPanel độc lập
        Close();
        var infoPanel = UnityEngine.Object.FindObjectOfType<InformationPanelController>(true);
        if (infoPanel != null)
            infoPanel.ShowTuiDo();
        else
            UnityEngine.Object.FindObjectOfType<InventoryUI>(true)?.ShowInventory();
    }

    // ── Shop ──────────────────────────────────────────────────────────

    private void ClearShopItems()
    {
        foreach (Transform child in shopItemContainer)
            Destroy(child.gameObject);

        _shopCellsWithClass.Clear();
        _shopCellsWithEquipType.Clear();
        _filterButtons.Clear();

        HideFilterBar();
        RestoreShopScrollOffset();
    }

    /// <summary>Called by NpcInteraction.ShowShopClientRpc with a JSON array of shop items.</summary>
    public void ShowShop(string shopItemsJson)
    {
        Debug.Log($"[NpcMenuUI] ShowShop called. JSON length={shopItemsJson?.Length}. shopItemContainer={(shopItemContainer==null?"NULL":shopItemContainer.name)}. shopItemRowPrefab={(shopItemRowPrefab==null?"NULL":shopItemRowPrefab.name)}. filterBarScene={(HasFilterBar?filterBarScene.name:"NULL")}");
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
            Debug.LogWarning("[NpcMenuUI] ShowShop: items list is empty or null!");
            ShowFeedback("This shop has no items.", new Color(1f, 0.85f, 0f));
            return;
        }

        Debug.Log($"[NpcMenuUI] ShowShop: parsed {resp.items.Length} items");
        for (int di = 0; di < Mathf.Min(resp.items.Length, 5); di++)
            Debug.Log($"  item[{di}] name={resp.items[di].item_name} element_class={resp.items[di].element_class} equip_type={resp.items[di].equip_type}");

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

        // Weapon NPC: hiển thị filter hệ (element). Equipment NPC: hiển thị filter loại trang bị.
        // Hai filter bar loại trừ nhau — element filter ưu tiên.
        var presentElements   = new System.Collections.Generic.HashSet<int>();
        var presentEquipTypes = new System.Collections.Generic.HashSet<int>();
        foreach (var i in resp.items)
        {
            if (i.element_class > 0) presentElements.Add(i.element_class);
            if (i.equip_type >= 0 && i.equip_type <= 5) presentEquipTypes.Add(i.equip_type);
        }

        bool hasElements = presentElements.Count > 0;
        if (!hasElements) _activeElementFilter = 0;
        Debug.Log($"[NpcMenuUI] Filter decision: hasElements={hasElements} presentElements=[{string.Join(",",presentElements)}] presentEquipTypes=[{string.Join(",",presentEquipTypes)}]");
        CreateElementFilterBar(hasElements, presentElements);

        // Filter loại trang bị chỉ hiện khi shop KHÔNG có item theo hệ (loại trừ lẫn nhau)
        bool hasEquipTypes = !hasElements && presentEquipTypes.Count > 0;
        if (!hasEquipTypes) _activeEquipTypeFilter = -1;
        Debug.Log($"[NpcMenuUI] Filter decision: hasEquipTypes={hasEquipTypes}");
        CreateEquipTypeFilterBar(hasEquipTypes, presentEquipTypes);

        _shopCellsWithEquipType.Clear();

        int cellCount = 0;
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
            cellCount++;
        }

        Debug.Log($"[NpcMenuUI] ShowShop: spawned {cellCount} cells. _activeElementFilter={_activeElementFilter} _activeEquipTypeFilter={_activeEquipTypeFilter}");

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

        Debug.Log($"[Shop] Gửi mua: shopItemId={shopItem.shop_item_id} '{shopItem.item_name}' utilityMode={_isUtilityMode}");
        if (_isUtilityMode)
        {
            GameplayCommandService.Instance?.BuyUtilityShopItemServerRpc(shopItem.shop_item_id, 1);
        }
        else
        {
            _currentInteraction?.BuyItemServerRpc(shopItem.shop_item_id, 1);
        }
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
            },
            requiredLevelOverride: shopItem.required_level);
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

    private void CreateElementFilterBar(bool enabled, System.Collections.Generic.HashSet<int> presentClasses = null)
    {
        Debug.Log($"[NpcMenuUI] CreateElementFilterBar enabled={enabled} filterBarScene={(HasFilterBar ? filterBarScene.name : "NULL")}");
        if (!enabled || shopPanel == null) return;  // ClearShopItems already hid bar + restored scroll

        if (!HasFilterBar)
        {
            Debug.LogWarning("[NpcMenuUI] filterBarScene chưa được gán trong Inspector!", this);
            return;
        }

        filterBarScene.SetActive(true);
        filterBarScene.transform.SetAsLastSibling();
        _filterButtons.Clear();

        // Cấu hình nút theo hệ: 0=Tất Cả 1=Hỏa 2=Thủy 3=Thổ 4=Lôi 5=Mộc 6=Phong
        string[] labels = { "Tất Cả", "Hỏa", "Thủy", "Thổ", "Lôi", "Mộc", "Phong" };
        int idx = 0;
        foreach (Transform child in filterBarScene.transform)
        {
            if (idx < labels.Length)
            {
                child.gameObject.SetActive(true);
                SetFilterButtonLabel(child, labels[idx]);
                var btn = child.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    int captured = idx;
                    btn.onClick.AddListener(() => ApplyElementFilter(captured));
                    _filterButtons.Add((btn, captured));
                }
                if (idx > 0 && presentClasses != null && !presentClasses.Contains(idx))
                    child.gameObject.SetActive(false);
            }
            else
                child.gameObject.SetActive(false);
            idx++;
        }

        ApplyElementFilter(_activeElementFilter);
        AdjustScrollForFilterBar(filterBarScene);
    }

    private static void SetFilterButtonLabel(Transform btnTransform, string text)
    {
        var tmp = btnTransform.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) tmp.text = text;
    }

    private void AdjustScrollForFilterBar(GameObject bar)
    {
        if (shopItemContainer == null || bar == null) return;
        var grid = shopItemContainer.GetComponent<GridLayoutGroup>();
        if (grid == null) return;
        if (!_scrollOffsetModified)
            _originalScrollOffsetTop = grid.padding.top;
        var barRt = bar.GetComponent<RectTransform>();
        float barH = (barRt != null) ? Mathf.Abs(barRt.sizeDelta.y) : 48f;
        var p = grid.padding;
        p.top = (int)(_originalScrollOffsetTop + barH);
        grid.padding = p;
        _scrollOffsetModified = true;
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

        HighlightFilterButton(elementClass);
    }

    private void HighlightFilterButton(int activeValue)
    {
        foreach (var (btn, value) in _filterButtons)
        {
            if (btn == null) continue;
            var img = btn.GetComponent<Image>();
            if (img != null)
            {
                float a = (value == activeValue) ? 1f : 0.5f;
                img.color = new Color(img.color.r, img.color.g, img.color.b, a);
            }
        }
    }

    // ── Buy result ────────────────────────────────────────────────────

    // ── Equip type filter bar ─────────────────────────────────────────

    private void CreateEquipTypeFilterBar(bool enabled, System.Collections.Generic.HashSet<int> presentTypes = null)
    {
        Debug.Log($"[NpcMenuUI] CreateEquipTypeFilterBar enabled={enabled} filterBarScene={(HasFilterBar ? filterBarScene.name : "NULL")} presentTypes=[{(presentTypes == null ? "null" : string.Join(",", presentTypes))}]");
        _filterButtons.Clear();
        if (!enabled || shopPanel == null) return;  // ClearShopItems already hid bar + restored scroll

        if (!HasFilterBar)
        {
            Debug.LogWarning("[NpcMenuUI] filterBarScene chưa được gán trong Inspector!", this);
            return;
        }

        filterBarScene.SetActive(true);
        filterBarScene.transform.SetAsLastSibling();

        // Cấu hình nút theo loại trang bị: equip_type → nhãn
        // equip_type: 0=Mũ 2=Áo 3=Quần 4=Giày 5=Nhẫn 1=Vũ Khí
        var entries = new (string label, int type)[]
        {
            ("Tất Cả", -1),
            ("Mũ",     0),
            ("Áo",     2),
            ("Quần",   3),
            ("Giày",   4),
            ("Nhẫn",   5),
            ("Vũ Khí", 1),
        };

        int idx = 0;
        foreach (Transform child in filterBarScene.transform)
        {
            if (idx < entries.Length)
            {
                var (label, type) = entries[idx];
                bool show = type == -1 || presentTypes == null || presentTypes.Contains(type);
                child.gameObject.SetActive(show);
                SetFilterButtonLabel(child, label);
                if (show)
                {
                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        int captured = type;
                        btn.onClick.AddListener(() => ApplyEquipTypeFilter(captured));
                        _filterButtons.Add((btn, captured));
                    }
                }
            }
            else
                child.gameObject.SetActive(false);
            idx++;
        }

        ApplyEquipTypeFilter(_activeEquipTypeFilter);
        AdjustScrollForFilterBar(filterBarScene);
    }

    private void ApplyEquipTypeFilter(int equipType)
    {
        _activeEquipTypeFilter = equipType;

        foreach (var (go, et) in _shopCellsWithEquipType)
        {
            if (go == null) continue;
            go.SetActive(equipType == -1 || et == equipType);
        }

        HighlightFilterButton(equipType);
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

    [System.Serializable] private class BuyResultDto { public bool success; public string message; public int playerGold; }

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
