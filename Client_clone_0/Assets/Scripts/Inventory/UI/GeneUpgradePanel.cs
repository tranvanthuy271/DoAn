using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

/// <summary>
/// GeneUpgradePanel — Panel nâng cấp Gene.
///
/// ══════════════════════════════════════════════════════════
/// CÁCH MỞ PANEL (gọi từ bất kỳ script nào):
///
///   GeneUpgradePanel.Instance.Open();
///
/// ══════════════════════════════════════════════════════════
/// INSPECTOR SETUP — kéo đúng thứ tự (xem [Header] bên dưới):
///   1. GeneTierDisplay   ← TMP_Text
///   2. ElementIcon       ← Image
///   3. GeneExpBar        ← Slider (readonly)
///   4. GeneExpText       ← TMP_Text "1000 / 5000 exp"
///   5. GoldCostText      ← TMP_Text
///   6. ItemCostText      ← TMP_Text
///   7. ItemIcon          ← Image (icon vật liệu)
///   8. SuccessRateText   ← TMP_Text
///   9. ItemCountSlider   ← Slider (người dùng kéo)
///  10. ItemCountText     ← TMP_Text "3 item"
///  11. StatHpText        ← TMP_Text
///  12. StatMpText        ← TMP_Text
///  13. StatAtkText       ← TMP_Text
///  14. StatDefText       ← TMP_Text
///  15. SkillsContainer   ← Transform (parent chứa skill row prefab)
///  16. SkillRowPrefab    ← GameObject (prefab 1 dòng skill)
///  17. UpgradeButton     ← Button
///  18. CloseButton       ← Button
///  19. StatusText        ← TMP_Text (kết quả)
///  20. LoadingOverlay    ← GameObject (che UI khi đang tải)
/// ══════════════════════════════════════════════════════════
/// </summary>
public class GeneUpgradePanel : MonoBehaviour
{
    public static GeneUpgradePanel Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Gene Tier Info")]
    [SerializeField] private TMP_Text tierDisplayText;   // "Gene Tier 1 → 2"
    [SerializeField] private Image    elementIcon;        // icon nguyên tố
    [SerializeField] private Slider   geneExpBar;         // gel progress bar (interactable=false)
    [SerializeField] private TMP_Text geneExpText;        // "1000 / 5000 exp"

    [Header("Chi phí")]
    [SerializeField] private TMP_Text goldCostText;       // "Cần: 5,000 vàng"
    [SerializeField] private TMP_Text goldPlayerText;     // "Bạn có: 10,000 vàng" (tuỳ chọn)
    [SerializeField] private TMP_Text itemCostText;       // "x2 Linh Thạch Sơ Cấp (tối đa x5)"
    [SerializeField] private Image    itemIcon;           // icon vật liệu nâng cấp

    [Header("Tỉ lệ thành công")]
    [SerializeField] private TMP_Text successRateText;    // "Tỉ lệ: 48%"
    [SerializeField] private Slider   itemCountSlider;    // min=stone_min, max=stone_needed
    [SerializeField] private TMP_Text itemCountText;      // "3 item"

    [Header("Stat Bonus Preview")]
    [SerializeField] private TMP_Text statHpText;         // "+200 HP"
    [SerializeField] private TMP_Text statMpText;         // "+50 MP"
    [SerializeField] private TMP_Text statAtkText;        // "+20 ATK"
    [SerializeField] private TMP_Text statDefText;        // "+10 DEF"

    [Header("Skills To Unlock")]
    [SerializeField] private Transform skillsContainer;   // parent chứa các dòng skill
    [SerializeField] private GameObject skillRowPrefab;   // prefab 1 dòng skill (TMP_Text đủ)

    [Header("Buttons & Status")]
    [SerializeField] private Button    upgradeButton;
    [SerializeField] private Button    closeButton;
    [SerializeField] private TMP_Text  statusText;        // thông báo kết quả
    [SerializeField] private GameObject loadingOverlay;   // che UI khi đang gọi API

    [Header("Shared Element Visuals")]
    [SerializeField] private ElementIconConfig elementIconConfig;

    // ── Runtime data ──────────────────────────────────────────────────────
    private GeneConfigDto _config;
    private PlayerDataResponse _playerData;
    private bool _isSecondary;   // true = đang nâng tier hệ phụ

    // ── Lifecycle ─────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    private void Start()
    {
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        closeButton.onClick.AddListener(OnCloseClicked);

        // Slider chỉ dùng để hiển thị gene exp — không tương tác
        if (geneExpBar != null) geneExpBar.interactable = false;

        // Slider chọn số item — lắng nghe sự kiện kéo
        if (itemCountSlider != null)
            itemCountSlider.onValueChanged.AddListener(OnItemCountChanged);
    }

    // ── Mở panel ──────────────────────────────────────────────────────────

    /// <summary>Mở panel nâng cấp gene. Gọi từ bất kỳ Button/script nào.</summary>
    public void Open()
    {
        _isSecondary = false;
        // Bật cả canvas cha nếu đang bị tắt (HideOtherBlacksmithFlows dùng root.SetActive(false))
        var root = transform.root.gameObject;
        if (!root.activeSelf) root.SetActive(true);
        gameObject.SetActive(true);
        // Awake() may have fired on first activation and called SetActive(false); re-ensure visible.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        StartCoroutine(LoadAndRefresh());
    }

    /// <summary>Mở panel nâng cấp gene HỆ PHỤ (dùng chung panel, gọi API secondary).</summary>
    public void OpenForSecondary()
    {
        _isSecondary = true;
        var root = transform.root.gameObject;
        if (!root.activeSelf) root.SetActive(true);
        gameObject.SetActive(true);
        // Awake() may have fired on first activation and called SetActive(false); re-ensure visible.
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        StartCoroutine(LoadAndRefresh());
    }

    private void OnCloseClicked()
    {
        gameObject.SetActive(false);
    }

    // ── Load data ─────────────────────────────────────────────────────────

    private IEnumerator LoadAndRefresh()
    {
        SetLoading(true);
        SetStatus("", Color.white);

        if (_isSecondary)
        {
            yield return StartCoroutine(LoadAndRefreshSecondary());
            yield break;
        }

        // 1. Lấy player data mới nhất từ server
        yield return StartCoroutine(RefreshPlayerData());

        _playerData = GameManager.Instance.GetPlayerData();
        if (_playerData == null)
        {
            Debug.LogError("[GeneUpgradePanel] _playerData == null sau RefreshPlayerData!");
            SetStatus("Không tải được dữ liệu nhân vật.", Color.red);
            SetLoading(false);
            yield break;
        }

        Debug.Log($"[GeneUpgradePanel] PlayerData loaded — " +
                  $"player_id={_playerData.player_id} | " +
                  $"element={_playerData.element_type} | " +
                  $"gene_tier={_playerData.gene_tier} | " +
                  $"gene_exp={_playerData.gene_exp} | " +
                  $"gold={_playerData.gold}");

        // 2. Kiểm tra đã max tier chưa
        if (_playerData.gene_tier >= 5)
        {
            SetStatus("Gene đã đạt Tier 5 tối đa!", Color.yellow);
            upgradeButton.interactable = false;
            SetLoading(false);
            // Vẫn hiển thị thông tin tier hiện tại
            tierDisplayText.text = "Gene Tier 5 (MAX)";
            UpdateExpBar(_playerData.gene_exp, 0);
            yield break;
        }

        // 3. Tải config từ server
        bool configOk = false;
        yield return StartCoroutine(LoadGeneConfig(ok => configOk = ok));

        if (!configOk)
        {
            SetStatus("Không tải được config gene. Kiểm tra server.", Color.red);
            SetLoading(false);
            yield break;
        }

        // 4. Cập nhật toàn bộ UI
        RefreshUI();
        SetLoading(false);
    }

    private IEnumerator RefreshPlayerData()
    {
        if (GameplayCommandService.Instance == null) yield break;

        bool done = false;
        GameplayCommandService.OnPlayerDataReceived -= HandleRefreshPlayerData;
        GameplayCommandService.OnPlayerDataReceived += HandleRefreshPlayerData;
        GameplayCommandService.Instance.RequestPlayerDataServerRpc();

        void HandleRefreshPlayerData(string json)
        {
            GameplayCommandService.OnPlayerDataReceived -= HandleRefreshPlayerData;
            if (!json.Contains("\"error\""))
            {
                var data = JsonUtility.FromJson<PlayerDataResponse>(json);
                if (data != null) GameManager.Instance.SetPlayerData(data);
            }
            done = true;
        }

        yield return new WaitUntil(() => done);
    }

    private IEnumerator LoadGeneConfig(System.Action<bool> onDone)
    {
        if (GameplayCommandService.Instance == null) { onDone?.Invoke(false); yield break; }

        bool done    = false;
        bool success = false;

        Debug.Log($"[GeneUpgradePanel] GetGeneConfig → elementType={_playerData.element_type} tier={_playerData.gene_tier}");

        GameplayCommandService.OnGeneConfigReceived -= HandleGeneConfig;
        GameplayCommandService.OnGeneConfigReceived += HandleGeneConfig;
        GameplayCommandService.Instance.GetGeneConfigServerRpc(_playerData.element_type, _playerData.gene_tier);

        void HandleGeneConfig(string json)
        {
            GameplayCommandService.OnGeneConfigReceived -= HandleGeneConfig;
            if (json.Contains("\"error\""))
            {
                Debug.LogError($"[GeneUpgradePanel] GetGeneConfig error: {json}");
            }
            else
            {
                _config = JsonUtility.FromJson<GeneConfigDto>(json);
                success = _config != null;
                if (success)
                    Debug.Log($"[GeneUpgradePanel] Config loaded — tier {_config.tierFrom}→{_config.tierTo} | geneExpRequired={_config.geneExpRequired}");
            }
            done = true;
        }

        yield return new WaitUntil(() => done);
        onDone?.Invoke(success);
    }

    // ── UI refresh ────────────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (_config == null || _playerData == null) return;

        // Tier display
        tierDisplayText.text = $"Gene Tier {_config.tierFrom} → {_config.tierTo}";

        // Element icon
        SetElementIcon(_playerData.element_type);

        // Gene exp bar
        UpdateExpBar(_playerData.gene_exp, _config.geneExpRequired);

        Debug.Log($"[GeneUpgradePanel] RefreshUI — " +
                  $"gene_exp={_playerData.gene_exp}/{_config.geneExpRequired} | " +
                  $"gold={_playerData.gold} (need {_config.goldCost}) | " +
                  $"itemsMin={_config.itemsMin} itemsNeeded={_config.itemsNeeded} itemName='{_config.itemName}'");

        // Chi phí vàng
        goldCostText.text = $"Bạn Cần: {_config.goldCost:N0} vàng";
        if (goldPlayerText != null)
            goldPlayerText.text = $"Bạn có: {_playerData.gold:N0} vàng";

        // Chi phí item: hiện "x{min} ~ x{needed} <tên item>" để đúng với DB stone_needed
        itemCostText.text = $"x{_config.itemsMin} {_config.itemName}";

        // Icon vật liệu (load từ Resources nếu có, quy ước: "ItemIcons/<id>")
        if (itemIcon != null)
        {
            var sprite = Resources.Load<Sprite>($"ItemIcons/{_config.itemIcon}");
            if (sprite != null) itemIcon.sprite = sprite;
        }

        // Slider số item: min=stone_min, max=stone_needed (theo DB: stone_min / stone_needed)
        itemCountSlider.minValue   = _config.itemsMin;
        itemCountSlider.maxValue   = _config.itemsNeeded;
        itemCountSlider.wholeNumbers = true;
        itemCountSlider.value      = _config.itemsMin;
        OnItemCountChanged(_config.itemsMin); // cập nhật rate + text ngay

        // Stat bonus preview
        if (_config.statBonus != null)
        {
            statHpText.text  = $"+{_config.statBonus.hp} HP";
            statMpText.text  = $"+{_config.statBonus.mp} MP";
            statAtkText.text = $"+{_config.statBonus.attack} ATK";
            statDefText.text = $"+{_config.statBonus.defense} DEF";
        }

        // Skills sẽ mở
        RefreshSkillsList();

        // Nút Upgrade: chỉ cho bấm nếu đủ gene_exp
        bool enoughExp  = _playerData.gene_exp >= _config.geneExpRequired;
        bool enoughGold = _playerData.gold >= _config.goldCost;
        upgradeButton.interactable = enoughExp && enoughGold;

        if (!enoughExp)
            SetStatus($"Cần {_config.geneExpRequired:N0} Gene Exp (đang có: {_playerData.gene_exp:N0})", Color.yellow);
        else if (!enoughGold)
            SetStatus($"Không đủ vàng (cần {_config.goldCost:N0})", Color.yellow);
        else
            SetStatus("", Color.white);
    }

    private void UpdateExpBar(int currentExp, int required)
    {
        if (geneExpBar != null)
        {
            geneExpBar.maxValue = (required > 0) ? required : 1;
            geneExpBar.value    = Mathf.Min(currentExp, geneExpBar.maxValue);
        }
        if (geneExpText != null)
            geneExpText.text = $"{currentExp:N0} / {required:N0} exp";
    }

    /// <summary>
    /// Cập nhật gene_exp hiển thị ngay từ GameManager (không cần RPC).
    /// Gọi sau khi dùng item nâng gene exp.
    /// </summary>
    public void RefreshFromLocalCache()
    {
        if (!gameObject.activeSelf) return;
        var pd = GameManager.Instance?.GetPlayerData();
        if (pd == null) return;

        // Cập nhật _playerData để goldText, slider, v.v. cũng dùng giá trị mới
        _playerData = pd;

        if (_config != null)
        {
            UpdateExpBar(pd.gene_exp, _config.geneExpRequired);
            if (goldPlayerText != null)
                goldPlayerText.text = $"Bạn có: {pd.gold:N0} vàng";
        }
        else
        {
            // Config chưa tải — chỉ cập nhật text exp tạm
            if (geneExpText != null)
                geneExpText.text = $"{pd.gene_exp:N0} exp";
        }
    }

    private void SetElementIcon(string elementType)
    {
        if (elementIcon == null) return;

        var config = ResolveElementIconConfig();
        if (config == null)
            return;

        int elementId = ElementHelper.ToId(elementType);
        var sprite = config.GetSpriteOrLog(elementId, ElementIconConfig.SpriteKind.Icon, this, nameof(GeneUpgradePanel));
        if (sprite == null)
        {
            Debug.LogWarning($"[GeneUpgradePanel] Thiếu icon cho hệ '{elementType}'.", this);
            return;
        }

        elementIcon.sprite = sprite;
        elementIcon.color = Color.white;
    }

    private ElementIconConfig ResolveElementIconConfig()
    {
        if (elementIconConfig == null)
            elementIconConfig = ElementIconConfig.Resolve(elementIconConfig, this, nameof(GeneUpgradePanel));

        return elementIconConfig;
    }

    private void RefreshSkillsList()
    {
        if (skillsContainer == null) return;

        // Xoá các row cũ
        foreach (Transform child in skillsContainer)
            Destroy(child.gameObject);

        if (_config.skillsToUnlock == null || _config.skillsToUnlock.Length == 0) return;

        foreach (var skill in _config.skillsToUnlock)
        {
            if (skillRowPrefab == null) break;
            var row = Instantiate(skillRowPrefab, skillsContainer);

            // Nếu prefab chỉ có TMP_Text, set text trực tiếp
            var label = row.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = $"🔓 {skill.skillName}";

            // Nếu prefab có Image con đầu tiên, load icon skill
            var icon = row.GetComponentInChildren<Image>();
            if (icon != null && !string.IsNullOrEmpty(skill.iconId))
            {
                var sp = Resources.Load<Sprite>($"SkillIcons/{skill.iconId}");
                if (sp != null) icon.sprite = sp;
            }
        }
    }

    // ── Slider callback ───────────────────────────────────────────────────

    private void OnItemCountChanged(float value)
    {
        if (_config == null) return;
        int count = Mathf.RoundToInt(value);
        float rate = _config.baseSuccessRate * Mathf.Min((float)count / _config.itemsNeeded, 1f);
        successRateText.text = $"Tỉ lệ: {rate * 100:F0}%";
        // itemCountText: hiện "3 / 5 item" (x / stone_needed)
        itemCountText.text   = $"{count} / {_config.itemsNeeded} item";
        // cập nhật itemCostText theo số kéo
        itemCostText.text    = $"x{count} {_config.itemName}";
    }

    // ── Upgrade ───────────────────────────────────────────────────────────

    private void OnUpgradeClicked()
    {
        int itemCount = Mathf.RoundToInt(itemCountSlider.value);
        if (_isSecondary)
            StartCoroutine(DoUpgradeSecondary(itemCount));
        else
            StartCoroutine(DoUpgrade(itemCount));
    }

    private IEnumerator DoUpgrade(int itemCount)
    {
        SetLoading(true);
        upgradeButton.interactable = false;

        var request = new GeneUpgradeRequest
        {
            playerId  = _playerData.player_id,
            itemCount = itemCount
        };

        bool done = false;
        GeneUpgradeResponse response = null;
        string errorMsg = null;

        if (GameplayCommandService.Instance == null)
        {
            SetStatus("Lỗi: Server chưa sẵn sàng.", Color.red);
            upgradeButton.interactable = true;
            SetLoading(false);
            yield break;
        }

        GameplayCommandService.OnGeneUpgraded -= HandleGeneUpgraded;
        GameplayCommandService.OnGeneUpgraded += HandleGeneUpgraded;
        GameplayCommandService.Instance.UpgradeGeneServerRpc(JsonUtility.ToJson(request));

        void HandleGeneUpgraded(string resultJson)
        {
            GameplayCommandService.OnGeneUpgraded -= HandleGeneUpgraded;
            if (resultJson.Contains("\"error\""))
                errorMsg = resultJson;
            else
                response = JsonUtility.FromJson<GeneUpgradeResponse>(resultJson);
            done = true;
        }

        yield return new WaitUntil(() => done);

        if (errorMsg != null)
        {
            SetStatus($"Lỗi: {errorMsg}", Color.red);
            upgradeButton.interactable = true;
            SetLoading(false);
            yield break;
        }

        // Cập nhật dữ liệu local (không cần reload từ server)
        _playerData.gold               = response.gold;
        _playerData.gene_tier          = response.newGeneTier;
        _playerData.gene_exp           = response.newGeneExp;

        // Áp dụng final_stats từ server (base + equipment + potential) vào GameManager
        if (response.final_stats != null)
            _playerData.final_stats = response.final_stats;
        else if (response.success && response.newStats != null)
        {
            _playerData.base_stats.max_hp  = response.newStats.maxHp;
            _playerData.base_stats.max_mp  = response.newStats.maxMp;
            _playerData.base_stats.attack  = response.newStats.attack;
        }
        GameManager.Instance.SetPlayerData(_playerData);

        // Đồng bộ stats lên host qua Network → tự động broadcast tới tất cả client
        var fs = _playerData.final_stats;
        if (fs != null)
        {
            var sync = FindLocalDataSync();
            if (sync != null)
                sync.UpdatePlayerDataServerRpc(
                    _playerData.player_id,
                    _playerData.element_type  ?? "Fire",
                    _playerData.gender        ?? "Male",
                    _playerData.character_name ?? "",
                    _playerData.level,
                    fs.hp, fs.max_hp, fs.mp, fs.max_mp,
                    fs.attack, fs.defense, fs.move_speed,
                    _playerData.gene_tier
                );
        }

        // Cập nhật StatsTabUI nếu đang mở
        FindObjectOfType<StatsTabUI>()?.Load();

        if (response.success)
        {
            SetStatus($"✨ Thành công! Gene Tier {response.newGeneTier}", Color.green);

            // Hiển thị skill mới mở khoá
            if (response.newlyUnlockedSkills != null)
                foreach (var skill in response.newlyUnlockedSkills)
                    Debug.Log($"[GeneUpgradePanel] Skill mới mở khoá: {skill.skillName}");

            // Tier 5 → ẩn nút
            if (response.newGeneTier >= 5)
            {
                upgradeButton.interactable = false;
                SetLoading(false);
                tierDisplayText.text = "Gene Tier 5 (MAX)";
                UpdateExpBar(0, 0);
                yield break;
            }

            // Tải config tier mới
            yield return StartCoroutine(LoadGeneConfig(_ => { }));
            RefreshUI();
        }
        else
        {
            SetStatus("Thất bại. Gene Exp đã reset về 0.", Color.red);
            upgradeButton.interactable = true;
            // Cập nhật exp bar về 0
            if (_config != null)
                UpdateExpBar(0, _config.geneExpRequired);
        }

        SetLoading(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    // ══════════════════════════════════════════════════════════════
    //  CHẾ ĐỘ HỆ PHỤ — dùng REST trực tiếp thay vì GameplayCommandService
    // ══════════════════════════════════════════════════════════════

    private IEnumerator LoadAndRefreshSecondary()
    {
        _playerData = GameManager.Instance?.GetPlayerData();
        if (_playerData == null)
        {
            SetStatus("Không tải được dữ liệu nhân vật.", Color.red);
            SetLoading(false);
            yield break;
        }

        if (string.IsNullOrEmpty(_playerData.secondary_element))
        {
            SetStatus("Chưa chọn hệ phụ. Hãy chọn hệ thứ 2 trước.", Color.yellow);
            upgradeButton.interactable = false;
            SetLoading(false);
            yield break;
        }

        if (_playerData.secondary_gene_tier >= 5)
        {
            SetStatus("Hệ phụ đã đạt Tier 5 tối đa!", Color.yellow);
            upgradeButton.interactable = false;
            tierDisplayText.text = $"{ElementHelper.ToVietnamese(_playerData.secondary_element)} Tier 5 (MAX)";
            UpdateExpBar(_playerData.secondary_gene_exp, 0);
            SetLoading(false);
            yield break;
        }

        // Load config từ REST
        string url = $"{APIClient.BASE_URL}/api/gene/multi/config" +
                     $"?elementType={_playerData.secondary_element}&tier={_playerData.secondary_gene_tier}";
        using var req = UnityEngine.Networking.UnityWebRequest.Get(url);
        AuthHelper.AddAuthHeader(req);
        yield return req.SendWebRequest();

        if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            SetStatus($"Không tải được config hệ phụ: {req.downloadHandler.text}", Color.red);
            SetLoading(false);
            yield break;
        }

        _config = JsonUtility.FromJson<GeneConfigDto>(req.downloadHandler.text);
        if (_config == null)
        {
            SetStatus("Dữ liệu config hệ phụ không hợp lệ.", Color.red);
            SetLoading(false);
            yield break;
        }

        RefreshUISecondary();
        SetLoading(false);
    }

    private void RefreshUISecondary()
    {
        string secondaryViet = ElementHelper.ToVietnamese(_playerData.secondary_element);
        tierDisplayText.text = $"Hệ Phụ {secondaryViet} — Tier {_config.tierFrom} → {_config.tierTo}";

        SetElementIcon(_playerData.secondary_element);
        UpdateExpBar(_playerData.secondary_gene_exp, _config.geneExpRequired);

        goldCostText.text = $"Bạn Cần: {_config.goldCost:N0} vàng";
        if (goldPlayerText != null)
            goldPlayerText.text = $"Bạn có: {_playerData.gold:N0} vàng";

        itemCostText.text = $"x{_config.itemsMin} {_config.itemName}";
        if (itemIcon != null)
        {
            var sprite = Resources.Load<Sprite>($"ItemIcons/{_config.itemIcon}");
            if (sprite != null) itemIcon.sprite = sprite;
        }

        itemCountSlider.minValue     = _config.itemsMin;
        itemCountSlider.maxValue     = _config.itemsNeeded;
        itemCountSlider.wholeNumbers = true;
        itemCountSlider.value        = _config.itemsMin;
        OnItemCountChanged(_config.itemsMin);

        if (_config.statBonus != null)
        {
            statHpText.text  = $"+{_config.statBonus.hp / 2} HP (×0.5 hệ phụ)";
            statMpText.text  = $"+{_config.statBonus.mp / 2} MP";
            statAtkText.text = $"+{_config.statBonus.attack / 2} ATK";
            statDefText.text = $"+{_config.statBonus.defense / 2} DEF";
        }

        bool enoughExp  = _playerData.secondary_gene_exp >= _config.geneExpRequired;
        bool enoughGold = _playerData.gold >= _config.goldCost;
        upgradeButton.interactable = enoughExp && enoughGold;

        if (!enoughExp)
            SetStatus($"Cần {_config.geneExpRequired:N0} Gene Exp hệ phụ (đang có: {_playerData.secondary_gene_exp:N0})", Color.yellow);
        else if (!enoughGold)
            SetStatus($"Không đủ vàng (cần {_config.goldCost:N0})", Color.yellow);
        else
            SetStatus("", Color.white);
    }

    private IEnumerator DoUpgradeSecondary(int itemCount)
    {
        SetLoading(true);
        upgradeButton.interactable = false;

        string body = JsonUtility.ToJson(new SecondaryUpgradeReq
        {
            playerId  = _playerData.player_id,
            itemCount = itemCount,
        });

        using var req = new UnityEngine.Networking.UnityWebRequest(
            $"{APIClient.BASE_URL}/api/gene/secondary/upgrade", "POST");
        req.uploadHandler   = new UnityEngine.Networking.UploadHandlerRaw(
            System.Text.Encoding.UTF8.GetBytes(body));
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

        var resp = JsonUtility.FromJson<SecondaryUpgradeResp>(req.downloadHandler.text);
        if (resp == null)
        {
            SetStatus("Response không hợp lệ.", Color.red);
            upgradeButton.interactable = true;
            yield break;
        }

        _playerData.gold               = resp.gold;
        _playerData.secondary_gene_tier = resp.newSecondaryTier;
        _playerData.secondary_gene_exp  = resp.newSecondaryExp;
        GameManager.Instance?.SetPlayerData(_playerData);
        FindObjectOfType<StatsTabUI>()?.Load();

        if (resp.success)
        {
            SetStatus($"✨ Thành công! Hệ Phụ Tier {resp.newSecondaryTier}", Color.green);
            if (resp.newSecondaryTier >= 5)
            {
                upgradeButton.interactable = false;
                tierDisplayText.text = $"{ElementHelper.ToVietnamese(_playerData.secondary_element)} Tier 5 (MAX)";
                UpdateExpBar(0, 0);
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
            yield return StartCoroutine(LoadAndRefreshSecondary());
        }
        else
        {
            SetStatus("Thất bại. Gene Exp hệ phụ đã reset về 0.", Color.red);
            upgradeButton.interactable = true;
            if (_config != null) UpdateExpBar(0, _config.geneExpRequired);
        }
    }

    [System.Serializable]
    private class SecondaryUpgradeReq  { public int playerId; public int itemCount; }
    [System.Serializable]
    private class SecondaryUpgradeResp { public bool success; public int newSecondaryTier; public int newSecondaryExp; public int gold; public string message; }


    /// <summary>Tìm NetworkPlayerDataSync của local player (IsOwner=true)</summary>
    private static NetworkPlayerDataSync FindLocalDataSync()
    {
        foreach (var s in FindObjectsOfType<NetworkPlayerDataSync>())
            if (s.IsOwner) return s;
        return null;
    }

    private void SetStatus(string msg, Color color)
    {
        if (statusText == null) return;
        statusText.text  = msg;
        statusText.color = color;
    }

    private void SetLoading(bool on)
    {
        if (loadingOverlay != null) loadingOverlay.SetActive(on);
    }
}
