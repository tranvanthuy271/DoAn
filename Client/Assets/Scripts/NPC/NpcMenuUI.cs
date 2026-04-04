using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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
        EnsureInitialized();   // hides mainPanel on first call; safe to call on inactive objects
        _currentInteraction = interaction;
        npcNameText.text  = npc.npc_name;
        dialogueText.text = !string.IsNullOrEmpty(npc.dialogue_text)
            ? npc.dialogue_text
            : "Xin chao, ta co the giup gi cho nguoi?";

        // Blacksmith NPC: chỉ mở BlacksmithTabPanel — KHÔNG kích hoạt root NpcMenuUI
        if (npc.npc_type == "blacksmith")
        {
            if (BlacksmithTabPanel.Instance != null)
            {
                BlacksmithTabPanel.Instance.Open(0);  // mặc định tab Cường Hóa
            }
            else if (UpgradePanel.Instance != null)
            {
                // Fallback nếu chưa có BlacksmithTabPanel trong scene
                var bridge = FindObjectOfType<InventoryNetworkBridge>();
                var inv = bridge != null ? bridge.CurrentInventory : null;
                UpgradePanel.Instance.OpenEmpty(inv);
            }
            else
            {
                Debug.LogWarning("[NpcMenuUI] BlacksmithTabPanel.Instance và UpgradePanel.Instance đều chưa có trong scene!");
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
        mainPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (bagPanel)  bagPanel.SetActive(false);
        HideItemDetailPanelIfOpen();
        _currentInteraction = null;
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

        // Clear any stale 'no items' feedback before spawning new items
        if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        if (_feedbackCoroutine != null) { StopCoroutine(_feedbackCoroutine); _feedbackCoroutine = null; }

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

            // Nhấn vào icon item -> hiện ItemDetailPanel dùng chung với túi đồ.
            if (cell.itemIcon != null)
            {
                var iconBtn = cell.itemIcon.gameObject.GetComponent<Button>()
                              ?? cell.itemIcon.gameObject.AddComponent<Button>();
                iconBtn.transition = Selectable.Transition.None;
                iconBtn.targetGraphic = cell.itemIcon;
                var capturedForInfo = item;
                iconBtn.onClick.RemoveAllListeners();
                iconBtn.onClick.AddListener(() => ShowShopItemDetail(capturedForInfo));
            }
        }
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

    // ── Buy result ────────────────────────────────────────────────────

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
        public int    shop_item_id;
        public int    item_template_id;
        public int    icon_id;
        public string item_name;
        public string item_detail;
        public int    price_silver;
        public int    price_gold;
        public int    stock;
        public int    required_level;
        public bool   can_afford;
        public bool   meets_level;
    }
}