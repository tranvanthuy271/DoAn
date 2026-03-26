using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;

/// <summary>
/// Panel UI tương tác NPC. Hiển thị dialogue + menu tuỳ theo npc_type.
///
/// Setup trong Inspector:
///   - Tạo Canvas → Panel "NpcMenuPanel" → gắn script này
///   - Assign đủ tất cả [SerializeField] fields
///   - Tạo một "ShopItemRow" prefab riêng (xem comment bên dưới)
///
/// ShopItemRow Prefab gồm:
///   ├── ItemIcon   (Image)       — optional
///   ├── ItemName   (TMP_Text)
///   ├── ItemDetail (TMP_Text)    — optional
///   ├── Price      (TMP_Text)
///   ├── Stock      (TMP_Text)    — optional
///   └── BtnBuy     (Button)
/// </summary>
public class NpcMenuUI : MonoBehaviour
{
    public static NpcMenuUI Instance { get; private set; }

    // ── Main panel ────────────────────────────────────────────
    [Header("Panel chính")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private TMP_Text   npcNameText;
    [SerializeField] private TMP_Text   dialogueText;

    [Header("Nút menu")]
    [SerializeField] private Button     btnBuy;
    [SerializeField] private Button     btnSell;    // TODO: implement sell flow
    [SerializeField] private Button     btnClose;

    // ── Shop panel ────────────────────────────────────────────
    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Transform  shopItemContainer;   // Content của ScrollRect
    [SerializeField] private GameObject shopItemRowPrefab;   // Prefab 1 dòng item

    // ── Feedback ──────────────────────────────────────────────
    [Header("Thông báo (tuỳ chọn)")]
    [SerializeField] private TMP_Text   feedbackText;        // text "Mua thành công!" / lỗi
    [SerializeField] private float      feedbackDuration = 2f;

    [Header("API")]
    [SerializeField] private string apiBase = "http://localhost:5000";

    private NpcSpawner.NpcData currentNpc;
    private Coroutine feedbackCoroutine;

    // ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        btnClose.onClick.AddListener(Close);
        btnBuy.onClick.AddListener(OpenShop);
        btnSell.onClick.AddListener(OnSellClick);

        mainPanel.SetActive(false);
        shopPanel.SetActive(false);
        if (feedbackText) feedbackText.gameObject.SetActive(false);
    }

    /// <summary>Gọi từ NpcInteraction khi player click NPC.</summary>
    public void Open(NpcSpawner.NpcData npc)
    {
        currentNpc = npc;
        npcNameText.text  = npc.npc_name;
        dialogueText.text = "...";

        // Hiển thị nút tuỳ loại NPC
        bool isShop = npc.npc_type is "shop" or "exchange";
        btnBuy.gameObject.SetActive(isShop);
        btnSell.gameObject.SetActive(isShop);

        shopPanel.SetActive(false);
        mainPanel.SetActive(true);

        StartCoroutine(FetchDialogue(npc.npc_id));
    }

    public void Close()
    {
        mainPanel.SetActive(false);
        shopPanel.SetActive(false);
        currentNpc = null;
    }

    // ── Dialogue ─────────────────────────────────────────────

    private IEnumerator FetchDialogue(int npcId)
    {
        string body = JsonUtility.ToJson(new InteractPayload
        {
            npc_id    = npcId,
            player_id = PlayerPrefs.GetInt("USER_ID")
        });

        using var req = PostJson($"{apiBase}/api/npc/interact", body);
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            var resp = JsonUtility.FromJson<InteractResponse>(req.downloadHandler.text);
            dialogueText.text = resp.dialogue_text;
        }
        else
        {
            dialogueText.text = "Xin chào, ta có thể giúp gì cho ngươi?";
        }
    }

    // ── Shop ─────────────────────────────────────────────────

    private void OpenShop()
    {
        shopPanel.SetActive(true);
        StartCoroutine(LoadShopItems());
    }

    private IEnumerator LoadShopItems()
    {
        // Xóa danh sách cũ
        foreach (Transform child in shopItemContainer)
            Destroy(child.gameObject);

        int playerId = PlayerPrefs.GetInt("USER_ID");
        string url = $"{apiBase}/api/npc/shop?npcId={currentNpc.npc_id}&playerId={playerId}";

        using var req = UnityWebRequest.Get(url);
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[NpcMenuUI] Load shop thất bại: {req.error}");
            yield break;
        }

        ShopListWrapper resp;
        try
        {
            string raw = req.downloadHandler.text;
            // API trả về JSON array trực tiếp → bọc lại thành object
            // Bảo vệ: nếu API trả HTML/lỗi, JsonUtility sẽ throw → bắt ở catch
            resp = JsonUtility.FromJson<ShopListWrapper>("{\"items\":" + raw + "}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[NpcMenuUI] Parse shop data thất bại: {ex.Message}");
            ShowFeedback("Không thể tải cửa hàng. Thử lại sau!", Color.red);
            yield break;
        }

        if (resp?.items == null || resp.items.Length == 0)
        {
            ShowFeedback("Cửa hàng chưa có hàng.", Color.yellow);
            yield break;
        }

        foreach (var item in resp.items)
        {
            var row = Instantiate(shopItemRowPrefab, shopItemContainer);

            // Tên item
            var nameText = row.transform.Find("ItemName")?.GetComponent<TMP_Text>();
            if (nameText) nameText.text = item.item_name;

            // Giá
            var priceText = row.transform.Find("Price")?.GetComponent<TMP_Text>();
            if (priceText)
                priceText.text = item.price_gold > 0
                    ? $"{item.price_gold} Vàng"
                    : $"{item.price_silver} Bạc";

            // Stock
            var stockText = row.transform.Find("Stock")?.GetComponent<TMP_Text>();
            if (stockText)
                stockText.text = item.stock == -1 ? "∞" : item.stock.ToString();

            // Nút mua
            var buyBtn = row.transform.Find("BtnBuy")?.GetComponent<Button>();
            if (buyBtn != null)
            {
                bool canBuy = item.can_afford && item.meets_level;
                buyBtn.interactable = canBuy;

                var capturedItem = item;
                buyBtn.onClick.AddListener(() => StartCoroutine(BuyItem(capturedItem)));
            }
        }
    }

    private IEnumerator BuyItem(ShopItem item)
    {
        string body = JsonUtility.ToJson(new BuyPayload
        {
            player_id = PlayerPrefs.GetInt("USER_ID"),
            npc_id    = currentNpc.npc_id,
            item_id   = item.item_template_id,
            quantity  = 1
        });

        using var req = PostJson($"{apiBase}/api/npc/shop/buy", body);
        req.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("JWT_TOKEN")}");
        yield return req.SendWebRequest();

        if (req.result == UnityWebRequest.Result.Success)
        {
            ShowFeedback($"Đã mua: {item.item_name}!", Color.green);
            // Refresh lại danh sách shop
            StartCoroutine(LoadShopItems());
        }
        else
        {
            string err = req.downloadHandler.text;
            ShowFeedback($"Mua thất bại: {err}", Color.red);
            Debug.LogError($"[NpcMenuUI] Buy error: {err}");
        }
    }

    // ── Sell (placeholder) ────────────────────────────────────

    private void OnSellClick()
    {
        // TODO: mở inventory panel để player chọn item bán
        Debug.Log("[NpcMenuUI] Sell panel chưa implement.");
        ShowFeedback("Chức năng bán đồ sẽ cập nhật sau!", Color.yellow);
    }

    // ── Feedback ─────────────────────────────────────────────

    private void ShowFeedback(string message, Color color)
    {
        if (feedbackText == null) return;
        if (feedbackCoroutine != null) StopCoroutine(feedbackCoroutine);
        feedbackCoroutine = StartCoroutine(FeedbackCoroutine(message, color));
    }

    private IEnumerator FeedbackCoroutine(string message, Color color)
    {
        feedbackText.text  = message;
        feedbackText.color = color;
        feedbackText.gameObject.SetActive(true);
        yield return new WaitForSeconds(feedbackDuration);
        feedbackText.gameObject.SetActive(false);
    }

    // ── Helper ───────────────────────────────────────────────

    private static UnityWebRequest PostJson(string url, string json)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler   = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        return req;
    }

    // ── Serializable DTOs ─────────────────────────────────────

    [System.Serializable] private class InteractPayload  { public int npc_id; public int player_id; }
    [System.Serializable] private class InteractResponse { public string dialogue_text; }

    [System.Serializable] private class ShopListWrapper  { public ShopItem[] items; }

    [System.Serializable]
    public class ShopItem
    {
        public int    item_template_id;
        public string item_name;
        public string item_detail;
        public int    price_silver;
        public int    price_gold;
        public int    stock;
        public int    required_level;
        public bool   can_afford;      // server tính dựa trên túi tiền player
        public bool   meets_level;     // server tính dựa trên level player
    }

    [System.Serializable]
    private class BuyPayload
    {
        public int player_id;
        public int npc_id;
        public int item_id;
        public int quantity;
    }
}
