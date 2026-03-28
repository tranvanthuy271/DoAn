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
        if (shopPanel) shopPanel.SetActive(false);
        if (bagPanel)  bagPanel.SetActive(false);
        if (feedbackText) feedbackText.gameObject.SetActive(false);
    }

    private void Start()
    {
        EnsureInitialized();
        mainPanel.SetActive(false);
    }

    // ── Open / Close ──────────────────────────────────────────────────

    /// <summary>Called by NpcInteraction.OpenMenuClientRpc.</summary>
    public void Open(NpcData npc, NpcInteraction interaction)
    {
        EnsureInitialized();   // covers the inactive-at-start case
        _currentInteraction = interaction;
        npcNameText.text  = npc.npc_name;
        dialogueText.text = !string.IsNullOrEmpty(npc.dialogue_text)
            ? npc.dialogue_text
            : "Xin chao, ta co the giup gi cho nguoi?";
        mainPanel.SetActive(true);
        ShowShopTab();
    }

    public void Close()
    {
        mainPanel.SetActive(false);
        if (shopPanel) shopPanel.SetActive(false);
        if (bagPanel)  bagPanel.SetActive(false);
        _currentInteraction = null;
    }

    // ── Tabs ──────────────────────────────────────────────────────────

    private void ShowShopTab()
    {
        if (shopPanel) shopPanel.SetActive(true);
        if (bagPanel)  bagPanel.SetActive(false);
        ClearShopItems();
        _currentInteraction?.LoadShopServerRpc();
    }

    private void ShowBagTab()
    {
        if (shopPanel) shopPanel.SetActive(false);
        if (bagPanel)  bagPanel.SetActive(true);
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

            if (cell.itemIcon != null)
            {
                var loaded = Resources.Load<Sprite>($"ItemIcons/{item.icon_id}");
                cell.itemIcon.sprite  = loaded != null ? loaded : defaultItemIcon;
                cell.itemIcon.enabled = cell.itemIcon.sprite != null;
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
                cell.btnBuy.onClick.AddListener(() =>
                {
                    if (!capturedItem.can_afford)
                    {
                        Debug.Log($"[Shop] Không đủ tiền mua '{capturedItem.item_name}'. Cần: {(capturedItem.price_gold > 0 ? capturedItem.price_gold + "g" : capturedItem.price_silver + "s")}");
                        return;
                    }
                    if (!capturedItem.meets_level)
                    {
                        Debug.Log($"[Shop] Chưa đủ level. '{capturedItem.item_name}' yêu cầu level {capturedItem.required_level}.");
                        return;
                    }
                    Debug.Log($"[Shop] Gửi mua: shopItemId={capturedItem.shop_item_id} '{capturedItem.item_name}'");
                    _currentInteraction?.BuyItemServerRpc(capturedItem.shop_item_id, 1);
                });
            }
        }
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