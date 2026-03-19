using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// HybridFusionPanel — Xác nhận Hybrid Fusion khi cả 2 hệ đạt Tier 5.
///
/// ═══════════════════════════════════════════════════════════════
/// INSPECTOR SETUP:
///   1.  HybridNameText       → TMP_Text (tên hybrid ví dụ "Kim Phong Thoán Thế")
///   2.  HybridDescText       → TMP_Text
///   3.  ElementAIcon         → Image (hệ chính)
///   4.  ElementBIcon         → Image (hệ phụ)
///   5.  ElementANameText     → TMP_Text "Hỏa Tier 5"
///   6.  ElementBNameText     → TMP_Text "Thủy Tier 5"
///   7.  StatHpText           → TMP_Text "+2000 HP"
///   8.  StatMpText           → TMP_Text "+500 MP"
///   9.  StatAtkText          → TMP_Text "+500 ATK"
///  10.  StatDefText          → TMP_Text "+200 DEF"
///  11.  ImmuneElementsText   → TMP_Text "Thủy, Kim"
///  12.  BonusTargetsText     → TMP_Text "Thổ, Hỏa"
///  13.  GoldCostText         → TMP_Text "2,000,000 Vàng"
///  14.  ItemIcon             → Image (icon Lõi Đột Biến)
///  15.  ItemCostText         → TMP_Text "x5 Lõi Đột Biến"
///  16.  ItemCountText        → TMP_Text "Bạn có: 3/5 Lõi Đột Biến"
///  17.  FuseButton           → Button
///  18.  CloseButton          → Button
///  19.  StatusText           → TMP_Text
///  20.  LoadingOverlay       → GameObject
///  21.  SuccessEffect        → GameObject (Particle/animation, ẩn mặc định)
/// ═══════════════════════════════════════════════════════════════
/// </summary>
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

    [Header("Element Sprites (Fire/Water/Earth/Metal/Wood/Wind)")]
    [SerializeField] private Sprite[] elementSprites;

    // ── Runtime ──────────────────────────────────────────────────
    private HybridConfigDto _config;
    private PlayerDataResponse _playerData;

    // ── Lifecycle ────────────────────────────────────────────────
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

    // ── Public API ───────────────────────────────────────────────

    public void Open()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (successEffect != null) successEffect.SetActive(false);
        SetStatus("", Color.white);
        StartCoroutine(LoadHybridConfig());
    }

    // ── Load ─────────────────────────────────────────────────────

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
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            SetStatus($"Chưa đủ điều kiện: {req.downloadHandler.text}", Color.red);
            yield break;
        }

        _config = JsonUtility.FromJson<HybridConfigDto>(req.downloadHandler.text);
        RefreshUI();
        fuseButton.interactable = _config?.canFuse ?? false;

        if (_config != null && !_config.canFuse)
        {
            if (!_config.itemSufficient)
                SetStatus($"Thiếu {_config.fusionItemName}: cần {_config.fusionItemCount}, có {_config.availableItems}.", Color.red);
            else if (!_config.goldSufficient)
                SetStatus($"Thiếu Vàng: cần {_config.fusionGoldCost:N0}, có {_config.playerGold:N0}.", Color.red);
        }
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
        if (elementAIcon != null && elementSprites != null && idA >= 0 && idA < elementSprites.Length)
            elementAIcon.sprite = elementSprites[idA];
        if (elementBIcon != null && elementSprites != null && idB >= 0 && idB < elementSprites.Length)
            elementBIcon.sprite = elementSprites[idB];
        if (elementANameText != null) elementANameText.text = ElementHelper.ToVietnamese(_config.elementA);
        if (elementBNameText != null) elementBNameText.text = ElementHelper.ToVietnamese(_config.elementB);

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
        if (itemCostText  != null) itemCostText.text  = $"x{_config.fusionItemCount} {_config.fusionItemName}";
        if (itemCountText != null)
        {
            itemCountText.text  = $"Bạn có: {_config.availableItems}/{_config.fusionItemCount} {_config.fusionItemName}";
            itemCountText.color = _config.availableItems >= _config.fusionItemCount ? Color.green : Color.red;
        }
    }

    // ── Fuse ─────────────────────────────────────────────────────

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
        yield return req.SendWebRequest();

        SetLoading(false);

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            SetStatus($"Fusion thất bại: {req.downloadHandler.text}", Color.red);
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

    // ── Helpers ──────────────────────────────────────────────────

    private string FriendlyElements(string csv)
    {
        if (string.IsNullOrEmpty(csv)) return "—";
        var parts = csv.Split(',', System.StringSplitOptions.RemoveEmptyEntries);
        var names = new System.Collections.Generic.List<string>();
        foreach (var p in parts)
            names.Add(ElementHelper.ToVietnamese(p.Trim()));
        return string.Join(", ", names);
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

    // ── DTOs ─────────────────────────────────────────────────────

    [System.Serializable] private class FuseRequest { public int playerId; public int itemCount; }

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
        public string[] bonusTargets;
        public string[] immuneElements;
        public float    atkBonusPercent;
        public int      fusionGoldCost;
        public string   fusionItemName;
        public int      fusionItemCount;
        public int      availableItems;
        public bool     itemSufficient;
        public bool     goldSufficient;
        public long     playerGold;
        public bool     canFuse;
        public StatDto  statBonus;
    }
}
