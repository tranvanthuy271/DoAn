using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// SecondaryGeneUpgradePanel — Nâng cấp hệ gene phụ (layout giống GeneUpgradePanel).
// Gọi /api/gene/multi/config và /api/gene/secondary/upgrade.
// INSPECTOR SETUP:
// 1. TierDisplayText    → TMP_Text "Hệ Phụ [Tên] — Tier 1 → 2"
// 2. SecondaryElemIcon  → Image (icon hệ phụ)
// 3. GeneExpBar         → Slider (readonly)
// 4. GeneExpText        → TMP_Text "1000 / 5000 exp"
// 5. GoldCostText       → TMP_Text
// 6. ItemCostText       → TMP_Text
// 7. ItemIcon           → Image
// 8. SuccessRateText    → TMP_Text
// 9. ItemCountSlider    → Slider (tương tác)
// 10. ItemCountText      → TMP_Text
// 11. StatHpText         → TMP_Text
// 12. StatMpText         → TMP_Text
// 13. StatAtkText        → TMP_Text
// 14. StatDefText        → TMP_Text
// 15. UpgradeButton      → Button
// 16. CloseButton        → Button
// 17. StatusText         → TMP_Text
// 18. LoadingOverlay     → GameObject
public class SecondaryGeneUpgradePanel : MonoBehaviour
{
    public static SecondaryGeneUpgradePanel Instance { get; private set; }

    [Header("Header")]
    [SerializeField] private TMP_Text tierDisplayText;
    [SerializeField] private Image    secondaryElemIcon;
    [SerializeField] private Slider   geneExpBar;
    [SerializeField] private TMP_Text geneExpText;

    [Header("Chi phí")]
    [SerializeField] private TMP_Text goldCostText;
    [SerializeField] private TMP_Text itemCostText;
    [SerializeField] private Image    itemIcon;

    [Header("Tỉ lệ thành công")]
    [SerializeField] private TMP_Text successRateText;
    [SerializeField] private Slider   itemCountSlider;
    [SerializeField] private TMP_Text itemCountText;

    [Header("Stat Bonus Preview (+50% vì là hệ phụ)")]
    [SerializeField] private TMP_Text statHpText;
    [SerializeField] private TMP_Text statMpText;
    [SerializeField] private TMP_Text statAtkText;
    [SerializeField] private TMP_Text statDefText;

    [Header("Buttons & Status")]
    [SerializeField] private Button     upgradeButton;
    [SerializeField] private Button     closeButton;
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private GameObject loadingOverlay;

    [Header("Shared Element Visuals")]
    [SerializeField] private ElementIconConfig elementIconConfig;

    // Runtime
    private GeneMultiConfigDto _config;
    private PlayerDataResponse _playerData;
    private int _itemCount;

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        if (geneExpBar != null) geneExpBar.interactable = false;
        if (itemCountSlider != null)
            itemCountSlider.onValueChanged.AddListener(OnItemCountChanged);
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    public void Open()
    {
        // Bật cả canvas cha nếu đang bị tắt
        var root = transform.root.gameObject;
        if (!root.activeSelf) root.SetActive(true);
        gameObject.SetActive(true);
        // Awake() may have fired on first activation and called SetActive(false); re-ensure visible.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        StartCoroutine(LoadAndRefresh());
    }

    // Load + Refresh

    private IEnumerator LoadAndRefresh()
    {
        SetLoading(true);
        SetStatus("", Color.white);

        // Refresh player data
        _playerData = GameManager.Instance?.GetPlayerData();
        if (_playerData == null || string.IsNullOrEmpty(_playerData.secondary_element))
        {
            SetStatus("Chưa chọn hệ phụ.", Color.red);
            SetLoading(false);
            yield break;
        }

        if (_playerData.secondary_gene_tier >= 5)
        {
            if (tierDisplayText != null)
                tierDisplayText.text = $"Hệ Phụ {ElementHelper.ToVietnamese(_playerData.secondary_element)} — Tier 5 (MAX)";
            if (geneExpText != null) geneExpText.text = "Đã đạt tối đa";
            upgradeButton.interactable = false;
            SetLoading(false);
            yield break;
        }

        // Tải config từ server
        bool ok = false;
        yield return StartCoroutine(LoadConfig(success => ok = success));

        if (!ok)
        {
            SetStatus("Không tải được config hệ phụ.", Color.red);
            SetLoading(false);
            yield break;
        }

        RefreshUI();
        SetLoading(false);
    }

    private IEnumerator LoadConfig(System.Action<bool> cb)
    {
        string secondary = _playerData.secondary_element;
        int    tier      = _playerData.secondary_gene_tier;
        string url       = $"{APIClient.BASE_URL}/api/gene/multi/config?elementType={secondary}&tier={tier}";

        using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
        AuthHelper.AddAuthHeader(req);
        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"[SecondaryGeneUpgradePanel] config error: {req.downloadHandler.text}");
            cb(false);
            yield break;
        }

        _config = JsonUtility.FromJson<GeneMultiConfigDto>(req.downloadHandler.text);
        cb(_config != null);
    }

    private void RefreshUI()
    {
        if (_config == null || _playerData == null) return;

        string secondaryViet = ElementHelper.ToVietnamese(_playerData.secondary_element);
        if (tierDisplayText != null)
            tierDisplayText.text = $"Hệ Phụ {secondaryViet} — Tier {_config.tierFrom} → {_config.tierTo}";

        int elemId = ElementHelper.ToId(_playerData.secondary_element);
        ApplyElementIcon(secondaryElemIcon, elemId, "secondary element");

        int   exp     = _playerData.secondary_gene_exp;
        int   expReq  = _config.geneExpRequired;
        float expPct  = expReq > 0 ? Mathf.Clamp01((float)exp / expReq) : 0f;
        if (geneExpBar != null) { geneExpBar.minValue = 0; geneExpBar.maxValue = 1; geneExpBar.value = expPct; }
        if (geneExpText != null) geneExpText.text = $"{exp:N0} / {expReq:N0} exp";

        if (goldCostText != null)  goldCostText.text  = $"Vàng: {_config.goldCost:N0}";
        if (itemCostText != null)  itemCostText.text  = $"×{_config.itemsMin}~{_config.itemsNeeded} {_config.itemName}";

        if (itemCountSlider != null)
        {
            itemCountSlider.minValue     = _config.itemsMin;
            itemCountSlider.maxValue     = _config.itemsNeeded;
            itemCountSlider.wholeNumbers = true;
            itemCountSlider.value        = _config.itemsMin;
        }
        _itemCount = _config.itemsMin;
        UpdateSuccessRate();

        bool hasExp  = exp >= expReq;
        upgradeButton.interactable = hasExp && exp >= 0;

        if (statHpText  != null) statHpText.text  = $"+{_config.statBonus?.hp  / 2} HP (×0.5)";
        if (statMpText  != null) statMpText.text  = $"+{_config.statBonus?.mp  / 2} MP";
        if (statAtkText != null) statAtkText.text = $"+{_config.statBonus?.attack / 2} ATK";
        if (statDefText != null) statDefText.text = $"+{_config.statBonus?.defense/ 2} DEF";
    }

    private void OnItemCountChanged(float val)
    {
        _itemCount = Mathf.RoundToInt(val);
        if (itemCountText != null) itemCountText.text = $"{_itemCount} item";
        UpdateSuccessRate();
    }

    private void UpdateSuccessRate()
    {
        if (_config == null) return;
        float rate = _config.baseSuccessRate * Mathf.Min((float)_itemCount / _config.itemsNeeded, 1f);
        if (successRateText != null)
            successRateText.text = $"Tỉ lệ: {rate * 100f:F0}%";
    }

    private void ApplyElementIcon(Image targetImage, int elementId, string logContext)
    {
        if (targetImage == null)
            return;

        var config = ResolveElementIconConfig();
        if (config == null)
            return;

        var sprite = config.GetSpriteOrLog(elementId, ElementIconConfig.SpriteKind.Icon, this, nameof(SecondaryGeneUpgradePanel));
        if (sprite == null)
        {
            Debug.LogWarning($"[SecondaryGeneUpgradePanel] Không apply được icon cho {logContext}.", this);
            return;
        }

        targetImage.sprite = sprite;
        targetImage.color = Color.white;
    }

    private ElementIconConfig ResolveElementIconConfig()
    {
        if (elementIconConfig == null)
            elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(SecondaryGeneUpgradePanel));

        return elementIconConfig;
    }

    // Upgrade

    private void OnUpgradeClicked()
    {
        StartCoroutine(UpgradeCoroutine());
    }

    private IEnumerator UpgradeCoroutine()
    {
        SetLoading(true);
        upgradeButton.interactable = false;

        string body = JsonUtility.ToJson(new SecondaryUpgradeRequest
        {
            playerId  = _playerData.player_id,
            itemCount = _itemCount,
        });

        using var req = new UnityEngine.Networking.UnityWebRequest(
            $"{APIClient.BASE_URL}/api/gene/secondary/upgrade", "POST");
        req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        AuthHelper.AddAuthHeader(req);
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            SetStatus($"Lỗi: {req.downloadHandler.text}", Color.red);
            upgradeButton.interactable = true;
            yield break;
        }

        var resp = JsonUtility.FromJson<SecondaryUpgradeResponse>(req.downloadHandler.text);
        if (resp.success)
        {
            SetStatus($"✨ {resp.message}", Color.green);
            _playerData.secondary_gene_tier = resp.newSecondaryTier;
            _playerData.secondary_gene_exp  = resp.newSecondaryExp;
            _playerData.gold                = resp.gold;
            GameManager.Instance?.SetPlayerData(_playerData);

            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(LoadAndRefresh());
        }
        else
        {
            SetStatus($"😞 {resp.message}", Color.red);
            upgradeButton.interactable = true;
        }
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    private void SetLoading(bool show)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(show);
    }

    // DTOs

    [System.Serializable]
    private class SecondaryUpgradeRequest   { public int playerId; public int itemCount; }

    [System.Serializable]
    private class SecondaryUpgradeResponse
    {
        public bool   success;
        public string message;
        public int    newSecondaryTier;
        public int    newSecondaryExp;
        public int    gold;
    }

    [System.Serializable]
    private class StatBonusDto { public int hp; public int mp; public int attack; public int defense; }

    [System.Serializable]
    private class GeneMultiConfigDto
    {
        public int          tierFrom;
        public int          tierTo;
        public int          geneExpRequired;
        public int          goldCost;
        public string       itemName;
        public int          itemsMin;
        public int          itemsNeeded;
        public float        baseSuccessRate;
        public StatBonusDto statBonus;
    }
}
