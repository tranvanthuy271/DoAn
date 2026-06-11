using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HybridFusionPanel — Xác nhận Hybrid Fusion khi cả 2 hệ đạt Tier 5.
// INSPECTOR SETUP:
// 1.  HybridNameText       → TMP_Text (tên hybrid ví dụ "Kim Phong Thoán Thế")
// 2.  HybridDescText       → TMP_Text
// 3.  ElementAIcon         → Image (hệ chính)
// 4.  ElementBIcon         → Image (hệ phụ)
// 5.  ElementANameText     → TMP_Text "Hỏa Tier 5"
// 6.  ElementBNameText     → TMP_Text "Thủy Tier 5"
// 7.  StatHpText           → TMP_Text "+2000 HP"
// 8.  StatMpText           → TMP_Text "+500 MP"
// 9.  StatAtkText          → TMP_Text "+500 ATK"
// 10.  StatDefText          → TMP_Text "+200 DEF"
// 11.  ImmuneElementsText   → TMP_Text "Thủy, Kim"
// 12.  BonusTargetsText     → TMP_Text "Thổ, Hỏa"
// 13.  GoldCostText         → TMP_Text "2,000,000 Vàng"
// 14.  ItemIcon             → Image (icon Lõi Đột Biến)
// 15.  ItemCostText         → TMP_Text "x5 Lõi Đột Biến"
// 16.  ItemCountText        → TMP_Text "Bạn có: 3/5 Lõi Đột Biến"
// 17.  FuseButton           → Button
// 18.  CloseButton          → Button
// 19.  StatusText           → TMP_Text
// 20.  LoadingOverlay       → GameObject
// 21.  SuccessEffect        → GameObject (Particle/animation, ẩn mặc định)
public class HybridFusionPanel : MonoBehaviour
{
    public static HybridFusionPanel Instance { get; private set; }

    [Header("Hybrid Preview")]
    [SerializeField] private TMP_Text   hybridNameText;
    [SerializeField] private TMP_Text   hybridDescText;
    [SerializeField] private Image      elementAIcon;
    [SerializeField] private Image      elementBIcon;
    [SerializeField] private TMP_Text   elementANameText;
    [SerializeField] private TMP_Text   elementBNameText;

    [Header("Stat & Effect Preview")]
    [SerializeField] private TMP_Text statHpText;
    [SerializeField] private TMP_Text statMpText;
    [SerializeField] private TMP_Text statAtkText;
    [SerializeField] private TMP_Text statDefText;
    [SerializeField] private TMP_Text immuneElementsText;
    [SerializeField] private TMP_Text bonusTargetsText;

    [Header("Cost")]
    [SerializeField] private TMP_Text goldCostText;
    [SerializeField] private Image    itemIcon;
    [SerializeField] private TMP_Text itemCostText;
    [SerializeField] private TMP_Text itemCountText;

    [Header("Buttons & Status")]
    [SerializeField] private Button     fuseButton;
    [SerializeField] private Button     closeButton;
    [SerializeField] private TMP_Text   statusText;
    [SerializeField] private GameObject loadingOverlay;
    [SerializeField] private GameObject successEffect;   // Particle/animation play khi thành công

    [Header("Shared Element Visuals")]
    [SerializeField] private ElementIconConfig elementIconConfig;

    // Runtime
    private HybridConfigDto _config;
    private PlayerDataResponse _playerData;

    // Hàm vòng đời của Unity hoặc ASP.NET được gọi tự động.
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        fuseButton.onClick.AddListener(OnFuseClicked);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        if (successEffect != null) successEffect.SetActive(false);
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    public void Open()
    {
        // Bật root canvas nếu đang bị tắt (prefab root có thể bị SetActive(false) hoặc scale=0)
        var root = transform.root.gameObject;
        if (root.transform.localScale == Vector3.zero)
            root.transform.localScale = Vector3.one;
        if (!root.activeSelf) root.SetActive(true);
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (successEffect != null) successEffect.SetActive(false);

        // Xoá các giá trị Inspector mặc định, prefill từ player data ngay lập tức
        ClearAndPrefill();

        SetStatus("", Color.white);
        StartCoroutine(LoadHybridConfig());
    }

    // Xoá tất cả text cứng từ Inspector và điền ngay tên hệ chính/phụ từ GameManager
    // trước khi API trả về. Tránh hiển thị dữ liệu ví dụ (ví dụ "Hỏa Tier 5") của prefab.
    private void ClearAndPrefill()
    {
        // Reset tên hybrid
        if (hybridNameText != null) hybridNameText.text = "...";
        if (hybridDescText != null) hybridDescText.text = "";

        // Reset stat
        if (statHpText  != null) statHpText.text  = "";
        if (statMpText  != null) statMpText.text  = "";
        if (statAtkText != null) statAtkText.text = "";
        if (statDefText != null) statDefText.text = "";

        // Reset immunity/bonus
        if (immuneElementsText != null) immuneElementsText.text = "";
        if (bonusTargetsText   != null) bonusTargetsText.text   = "";

        // Reset cost
        if (goldCostText  != null) goldCostText.text  = "";
        if (itemCostText  != null) itemCostText.text  = "";
        if (itemCountText != null) itemCountText.text = "";

        // Prefill từ player data thực sự (ngay cả trước khi API xong)
        var pd = GameManager.Instance?.GetPlayerData();
        if (pd == null) return;

        string elemA = pd.element_type ?? "";
        string elemB = string.IsNullOrEmpty(pd.secondary_element)
            ? ElementHelper.GetFixedSecondary(elemA) ?? ""
            : pd.secondary_element;

        int idA = ElementHelper.ToId(elemA);
        int idB = ElementHelper.ToId(elemB);

        ApplyElementIcon(elementAIcon, idA, "element A prefill");
        ApplyElementIcon(elementBIcon, idB, "element B prefill");

        if (elementANameText != null)
            elementANameText.text = $"{ElementHelper.ToVietnamese(elemA)} Tier {pd.gene_tier}";
        if (elementBNameText != null)
        {
            int secTier = pd.secondary_gene_tier > 0 ? pd.secondary_gene_tier : 0;
            elementBNameText.text = $"{ElementHelper.ToVietnamese(elemB)} Tier {secTier}";
        }
    }

    // Load

    private IEnumerator LoadHybridConfig()
    {
        SetLoading(true);
        fuseButton.interactable = false;

        _playerData = GameManager.Instance?.GetPlayerData();
        if (_playerData == null)
        {
            SetStatus("Không tải được dữ liệu nhân vật.", Color.red);
            SetLoading(false);
            yield break;
        }

        string url = $"{APIClient.BASE_URL}/api/gene/hybrid/config?playerId={_playerData.player_id}";
        using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
        ApplyAuthHeader(req);
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            SetStatus(BuildRequestError(req), Color.red);
            yield break;
        }

        _config = JsonUtility.FromJson<HybridConfigDto>(req.downloadHandler.text);
        RefreshUI();
        fuseButton.interactable = _config?.canFuse ?? false;
        UpdateRequirementStatus();
    }

    private void RefreshUI()
    {
        if (_config == null || _playerData == null) return;

        // Tên + mô tả
        if (hybridNameText != null) hybridNameText.text = _config.hybridName;
        if (hybridDescText != null) hybridDescText.text = _config.hybridDescription;

        // Icons hệ
        int idA = ElementHelper.ToId(_config.elementA);
        int idB = ElementHelper.ToId(_config.elementB);
        ApplyElementIcon(elementAIcon, idA, "element A config");
        ApplyElementIcon(elementBIcon, idB, "element B config");
        if (elementANameText != null) elementANameText.text = $"{ElementHelper.ToVietnamese(_config.elementA)} Tier {_config.elementATier}";
        if (elementBNameText != null) elementBNameText.text = $"{ElementHelper.ToVietnamese(_config.elementB)} Tier {_config.elementBTier}";

        // Stat bonus (riêng từng dòng)
        if (statHpText  != null) statHpText.text  = $"+{_config.statBonus?.hp} HP";
        if (statMpText  != null) statMpText.text  = $"+{_config.statBonus?.mp} MP";
        if (statAtkText != null) statAtkText.text = $"+{_config.statBonus?.attack} ATK";
        if (statDefText != null) statDefText.text = $"+{_config.statBonus?.defense} DEF";

        // Immunity + bonus targets
        if (immuneElementsText != null)
            immuneElementsText.text = FriendlyElements(string.Join(",", _config.immuneElements));
        if (bonusTargetsText != null)
            bonusTargetsText.text   = FriendlyElements(string.Join(",", _config.bonusTargets));

        // Cost
        if (goldCostText  != null) goldCostText.text  = $"{_config.fusionGoldCost:N0} Vàng";
        if (itemIcon != null && _config.fusionItemIcon > 0)
        {
            var iconSprite = Resources.Load<Sprite>($"ItemIcons/{_config.fusionItemIcon}");
            if (iconSprite != null) itemIcon.sprite = iconSprite;
        }
        if (itemCostText  != null) itemCostText.text  = $"x{_config.fusionItemCount} {_config.fusionItemName}";
        if (itemCountText != null)
        {
            itemCountText.text  = $"Bạn có: {_config.availableItems}/{_config.fusionItemCount} {_config.fusionItemName}";
            itemCountText.color = _config.availableItems >= _config.fusionItemCount ? Color.green : Color.red;
        }
    }

    // Fuse

    private void OnFuseClicked()
    {
        StartCoroutine(FuseCoroutine());
    }

    private IEnumerator FuseCoroutine()
    {
        SetLoading(true);
        fuseButton.interactable = false;

        string body = JsonUtility.ToJson(new FuseRequest
        {
            playerId  = _playerData.player_id,
            itemCount = _config?.fusionItemCount ?? 5,
        });

        using var req = new UnityEngine.Networking.UnityWebRequest(
            $"{APIClient.BASE_URL}/api/gene/hybrid/fuse", "POST");
        req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        ApplyAuthHeader(req);
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            SetStatus(BuildRequestError(req), Color.red);
            fuseButton.interactable = true;
            yield break;
        }

        var resp = JsonUtility.FromJson<FuseResponse>(req.downloadHandler.text);

        // Cập nhật player data local
        if (_playerData != null)
        {
            _playerData.is_hybrid              = true;
            _playerData.hybrid_id              = resp.hybridId;
            _playerData.hybrid_element_a       = resp.hybridElementA;
            _playerData.hybrid_element_b       = resp.hybridElementB;
            _playerData.hybrid_bonus_targets   = string.Join(",", resp.bonusTargets ?? new string[0]);
            _playerData.hybrid_immune_elements = string.Join(",", resp.immuneElements ?? new string[0]);
            _playerData.hybrid_atk_bonus_pct   = resp.atkBonusPercent;
            _playerData.hybrid_prefab_path     = resp.prefabPath;
            _playerData.gold                   = resp.gold;
            GameManager.Instance?.SetPlayerData(_playerData);
        }

        // Hiệu ứng thành công
        SetStatus(resp.message, Color.yellow);
        if (successEffect != null)
        {
            successEffect.SetActive(true);
            if (successEffect.TryGetComponent<ParticleSystem>(out var ps)) ps.Play();
        }

        yield return new WaitForSeconds(2f);

        // Đóng panel - characters sẽ respawn với prefab mới (qua CharacterLoader)
        gameObject.SetActive(false);
    }

    private void ApplyElementIcon(Image targetImage, int elementId, string logContext)
    {
        if (targetImage == null)
            return;

        var config = ResolveElementIconConfig();
        if (config == null)
            return;

        var sprite = config.GetSpriteOrLog(elementId, ElementIconConfig.SpriteKind.Icon, this, nameof(HybridFusionPanel));
        if (sprite == null)
        {
            { /* Cảnh báo: Không apply được icon cho {logContext} */ }
            return;
        }

        targetImage.sprite = sprite;
        targetImage.color = Color.white;
    }

    private ElementIconConfig ResolveElementIconConfig()
    {
        if (elementIconConfig == null)
            elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(HybridFusionPanel));

        return elementIconConfig;
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private string FriendlyElements(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return "—";
        var parts = csv.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        var names = new System.Collections.Generic.List<string>();
        foreach (var p in parts)
            names.Add(ElementHelper.ToVietnamese(p.Trim()));
        return string.Join(", ", names);
    }

    private void UpdateRequirementStatus()
    {
        if (_config == null)
        {
            return;
        }

        if (_config.canFuse)
        {
            SetStatus("Đủ điều kiện Hybrid Fusion.", Color.green);
            return;
        }

        var messages = new System.Collections.Generic.List<string>();
        if (!_config.itemSufficient)
            messages.Add($"Thiếu {_config.fusionItemName}: cần {_config.fusionItemCount}, có {_config.availableItems}.");
        if (!_config.goldSufficient)
            messages.Add($"Thiếu Vàng: cần {_config.fusionGoldCost:N0}, có {_config.playerGold:N0}.");

        SetStatus(messages.Count > 0 ? string.Join(" | ", messages) : "Chưa đủ điều kiện Hybrid Fusion.", Color.red);
    }

    private static void ApplyAuthHeader(UnityEngine.Networking.UnityWebRequest req)
    {
        string token = APIClient.Instance != null
            ? APIClient.Instance.GetToken()
            : PlayerPrefs.GetString("JWT_TOKEN", "");

        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", $"Bearer {token}");
    }

    private static string BuildRequestError(UnityEngine.Networking.UnityWebRequest req)
    {
        string errMsg = req.downloadHandler?.text ?? "";
        try
        {
            var errObj = JsonUtility.FromJson<ErrorResponse>(errMsg);
            if (!string.IsNullOrEmpty(errObj?.message))
                errMsg = errObj.message;
        }
        catch { }

        if (!string.IsNullOrWhiteSpace(errMsg))
            return errMsg;

        return req.responseCode switch
        {
            401 => "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.",
            403 => "Không có quyền thực hiện thao tác này.",
            _   => $"Yêu cầu thất bại (HTTP {req.responseCode})."
        };
    }

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

    [System.Serializable] private class FuseRequest { public int playerId; public int itemCount; }
    [System.Serializable] private class ErrorResponse { public string message; }

    [System.Serializable]
    private class FuseResponse
    {
        public bool     success;
        public string   message;
        public int      hybridId;
        public string   hybridElementA;
        public string   hybridElementB;
        public string   prefabPath;
        public string[] bonusTargets;
        public string[] immuneElements;
        public float    atkBonusPercent;
        public int      gold;
        public StatDto  statBonus;
    }

    [System.Serializable]
    private class StatDto { public int hp; public int mp; public int attack; public int defense; }

    [System.Serializable]
    private class HybridConfigDto
    {
        public string   hybridName;
        public string   hybridDescription;
        public string   elementA;
        public string   elementB;
        public int      elementATier;
        public int      elementBTier;
        public string[] bonusTargets;
        public string[] immuneElements;
        public float    atkBonusPercent;
        public int      fusionGoldCost;
        public string   fusionItemName;
        public int      fusionItemIcon;
        public int      fusionItemCount;
        public int      availableItems;
        public bool     itemSufficient;
        public bool     goldSufficient;
        public long     playerGold;
        public bool     canFuse;
        public StatDto  statBonus;
    }
}
