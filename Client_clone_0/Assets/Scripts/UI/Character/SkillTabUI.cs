using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tab "Ká»¹ NÄƒng" trong CharacterPanel.
///
/// Layout:
///   - Danh sÃ¡ch skill chiáº¿m toÃ n bá»™ chiá»u rá»™ng (ScrollView full-width).
///   - Khi nháº¥n vÃ o má»™t dÃ²ng â†’ SkillDetailPanel hiá»‡n lÃªn nhÆ° overlay riÃªng
///     (Ä‘Æ°á»£c Ä‘áº·t á»Ÿ cáº¥p parent cá»§a ContentSkill, khÃ´ng pháº£i bÃªn trong).
/// </summary>
public class SkillTabUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text txtSkillPoints;
    [SerializeField] private Transform skillListContainer;
    [SerializeField] private SkillRowUI skillRowPrefab;
    [SerializeField] private TMP_Text txtStatus;
    [SerializeField] private SkillDetailPanelUI skillDetailPanel;
    [SerializeField] private SkillDetailPanelUI skillDetailPanelPrefab;

    private int _playerId = -1;
    private readonly List<SkillRowUI> _rows = new List<SkillRowUI>();
    private PlayerSkillInfo[] _currentSkills;
    private PlayerSkillInfo _selectedSkill;
    private bool _isExternalProfileView;
    private PlayerSkillInfo[] _externalSkills;
    private string _externalCharacterName;

    private void Awake()
    {
        EnsureRuntimeLayout();
    }

    private void OnDisable()
    {
        // áº¨n overlay detail khi tab bá»‹ táº¯t (chuyá»ƒn sang tab khÃ¡c)
        skillDetailPanel?.Hide();
    }

    private void OnDestroy()
    {
        GameplayCommandService.OnSkillsReceived -= HandleSkillsReceived;
        GameplayCommandService.OnSkillUpgraded  -= HandleSkillUpgraded;
    }

    // â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void SetPlayerId(int id) => _playerId = id;

    public void ShowFriendSkills(PlayerSkillInfo[] skills, string characterName)
    {
        _isExternalProfileView    = true;
        _externalSkills           = skills;
        _externalCharacterName    = characterName;
        Load();
    }

    public void ClearFriendSkills()
    {
        if (!_isExternalProfileView && _externalSkills == null) return;
        _isExternalProfileView = false;
        _externalSkills        = null;
        _externalCharacterName = null;
    }

    public void Load()
    {
        EnsureRuntimeLayout();

        if (_isExternalProfileView)
        {
            RenderFriendSkills();
            return;
        }

        if (_playerId <= 0)
        {
            SetStatus("ChÆ°a cÃ³ playerId.");
            return;
        }

        // DÃ¹ng cache náº¿u server Ä‘Ã£ push lÃºc spawn
        if (PlayerSkillCache.Instance != null && PlayerSkillCache.Instance.HasData)
        {
            Debug.Log("[SkillTabUI] Load tá»« PlayerSkillCache.");
            PopulateSkills(PlayerSkillCache.Instance.CachedData);
            return;
        }

        // Fallback: gá»i server RPC
        if (GameplayCommandService.Instance == null)
        {
            SetStatus("Server chÆ°a sáºµn sÃ ng.");
            return;
        }

        SetStatus("Äang táº£i ká»¹ nÄƒng...");
        ClearRows();

        GameplayCommandService.OnSkillsReceived -= HandleSkillsReceived;
        GameplayCommandService.OnSkillsReceived += HandleSkillsReceived;
        GameplayCommandService.Instance.GetPlayerSkillsServerRpc();
    }

    // â”€â”€ Private helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void HandleSkillsReceived(string json)
    {
        GameplayCommandService.OnSkillsReceived -= HandleSkillsReceived;
        try
        {
            if (json.Contains("\"error\"")) { SetStatus($"Lá»—i: {json}"); return; }
            PlayerSkillsResponse response = JsonUtility.FromJson<PlayerSkillsResponse>(json);
            if (response == null)            { SetStatus("Lá»—i: pháº£n há»“i null."); return; }
            PopulateSkills(response);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SkillTabUI] Parse error: {ex.Message}");
            SetStatus($"Lá»—i: {ex.Message}");
        }
    }

    private void PopulateSkills(PlayerSkillsResponse response)
    {
        ClearRows();
        SetStatus("");
        _currentSkills = response.skills;

        if (txtSkillPoints != null)
            txtSkillPoints.text = $"Äiá»ƒm ká»¹ nÄƒng: <b>{response.skill_points_available}</b>";

        if (response.skills == null || response.skills.Length == 0)
        {
            SetStatus("ChÆ°a cÃ³ skill nÃ o.");
            return;
        }

        BuildRows(response.skills, readOnly: false);
    }

    private void RenderFriendSkills()
    {
        ClearRows();
        SetStatus("");
        _currentSkills = _externalSkills;

        if (txtSkillPoints != null)
            txtSkillPoints.text = string.IsNullOrWhiteSpace(_externalCharacterName)
                ? "Ká»¹ nÄƒng"
                : $"Ká»¹ nÄƒng cá»§a {_externalCharacterName}";

        if (_externalSkills == null || _externalSkills.Length == 0)
        {
            SetStatus("NgÆ°á»i chÆ¡i nÃ y chÆ°a cÃ³ ká»¹ nÄƒng nÃ o.");
            return;
        }

        BuildRows(_externalSkills, readOnly: true);
    }

    private void BuildRows(PlayerSkillInfo[] skills, bool readOnly)
    {
        if (skillRowPrefab == null || skillListContainer == null)
        {
            Debug.LogError("[SkillTabUI] Thiáº¿u skillRowPrefab hoáº·c skillListContainer.");
            return;
        }

        foreach (PlayerSkillInfo skill in skills)
        {
            SkillRowUI row = Instantiate(skillRowPrefab, skillListContainer);
            row.gameObject.SetActive(true);
            row.SetData(skill, readOnly, SelectSkill);
            _rows.Add(row);
        }
    }

    private void SelectSkill(PlayerSkillInfo skill)
    {
        _selectedSkill = skill;

        for (int i = 0; i < _rows.Count; i++)
        {
            SkillRowUI row = _rows[i];
            if (row == null) continue;
            PlayerSkillInfo rowInfo = i < (_currentSkills?.Length ?? 0) ? _currentSkills[i] : null;
            row.SetSelected(rowInfo != null && skill != null && rowInfo.skill_id == skill.skill_id);
        }

        if (skillDetailPanel != null || EnsureDetailPanel())
        {
            skillDetailPanel.SetData(skill, _isExternalProfileView, OnClickUpgradeSelected);
            skillDetailPanel.Show();
        }
    }

    private void OnClickUpgradeSelected(PlayerSkillInfo skill)
    {
        if (_isExternalProfileView || skill == null) return;
        bool maxed = skill.current_level >= skill.max_level && skill.max_level > 0;
        if (maxed || !skill.can_upgrade) return;
        if (GameplayCommandService.Instance == null) { SetStatus("Server chÆ°a sáºµn sÃ ng."); return; }

        skillDetailPanel?.SetUpgradeInteractable(false);
        SetStatus("");

        GameplayCommandService.OnSkillUpgraded -= HandleSkillUpgraded;
        GameplayCommandService.OnSkillUpgraded += HandleSkillUpgraded;
        GameplayCommandService.Instance.UpgradeSkillServerRpc(skill.skill_id);
    }

    private void HandleSkillUpgraded(string json)
    {
        GameplayCommandService.OnSkillUpgraded -= HandleSkillUpgraded;

        if (json.Contains("\"error\""))
        {
            SetStatus($"Lá»—i: {json}");
            skillDetailPanel?.SetUpgradeInteractable(_selectedSkill != null && _selectedSkill.can_upgrade);
            return;
        }

        PlayerSkillCache.Instance?.Invalidate();

        // ÄÃ³ng overlay, reload list
        skillDetailPanel?.Hide();
        Load();
    }

    private void ClearRows()
    {
        for (int i = 0; i < _rows.Count; i++)
            if (_rows[i] != null) Destroy(_rows[i].gameObject);
        _rows.Clear();

        if (skillListContainer == null) return;
        for (int i = skillListContainer.childCount - 1; i >= 0; i--)
            Destroy(skillListContainer.GetChild(i).gameObject);
    }

    private void SetStatus(string msg)
    {
        if (txtStatus == null) return;
        txtStatus.text    = msg;
        txtStatus.enabled = !string.IsNullOrEmpty(msg);
    }

    // â”€â”€ Layout bootstrap â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void EnsureRuntimeLayout()
    {
        ResolveExistingReferences();
        EnsureSkillList();
        EnsureRowPrefab();
        ConfigureLabels();
    }

    private void ResolveExistingReferences()
    {
        if (txtSkillPoints == null)
            txtSkillPoints = FindTextByPartialName("SkillPoints");

        if (txtStatus == null)
            txtStatus = FindTextByPartialName("Status");

        if (skillListContainer == null)
        {
            ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
            if (scroll != null && scroll.content != null)
                skillListContainer = scroll.content;
        }

        // Overlay panel vá»‘n lÃ  con cá»§a parent (sibling vá»›i ContentSkill), khÃ´ng pháº£i child trá»±c tiáº¿p
        if (skillDetailPanel == null)
        {
            Transform overlayParent = transform.parent ?? transform;
            Transform existing = overlayParent.Find("SkillDetailOverlay");
            if (existing != null)
                skillDetailPanel = existing.GetComponent<SkillDetailPanelUI>();
        }
    }

    private void EnsureSkillList()
    {
        ScrollRect scrollRect = skillListContainer != null
            ? skillListContainer.GetComponentInParent<ScrollRect>(true)
            : null;

        if (scrollRect == null)
            scrollRect = CreateSkillListScroll();

        // Full-width â€“ chiáº¿m toÃ n bá»™ ContentSkill
        RectTransform scrollRt = scrollRect.GetComponent<RectTransform>();
        scrollRt.anchorMin    = new Vector2(0f, 0f);
        scrollRt.anchorMax    = new Vector2(1f, 1f);
        scrollRt.offsetMin    = new Vector2(6f, 42f);   // bottom: nhÆ°á»ng cho label Ä‘iá»ƒm
        scrollRt.offsetMax    = new Vector2(-6f, -6f);
        scrollRt.localScale   = Vector3.one;

        scrollRect.horizontal        = false;
        scrollRect.vertical          = true;
        scrollRect.scrollSensitivity = 20f;

        if (skillListContainer != null)
            EnsureListContainerLayout(skillListContainer);
    }

    private ScrollRect CreateSkillListScroll()
    {
        Transform root = CreatePanel(transform, "SkillListScrollView", new Color(0.24f, 0.11f, 0.04f, 0.9f));
        var scrollRect = root.gameObject.AddComponent<ScrollRect>();

        Transform viewport = CreatePanel(root, "Viewport", new Color(0f, 0f, 0f, 0f));
        Stretch(viewport.GetComponent<RectTransform>());
        var viewportImage = viewport.GetComponent<Image>();
        viewportImage.raycastTarget = true;
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        Transform content = CreateRect(viewport, "Content");
        var contentRt = content.GetComponent<RectTransform>();
        contentRt.anchorMin        = new Vector2(0f, 1f);
        contentRt.anchorMax        = new Vector2(1f, 1f);
        contentRt.pivot            = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta        = Vector2.zero;

        skillListContainer = content;
        EnsureListContainerLayout(content);

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content  = contentRt;
        return scrollRect;
    }

    private bool EnsureDetailPanel()
    {
        // Panel Ä‘áº·t á»Ÿ cáº¥p parent (cÃ¹ng cáº¥p ContentSkill) Ä‘á»ƒ cÃ³ thá»ƒ overlay toÃ n bá»™ panel nhÃ¢n váº­t
        Transform overlayParent = transform.parent ?? transform;

        if (skillDetailPanel == null)
        {
            Transform existing = overlayParent.Find("SkillDetailOverlay");
            if (existing != null)
                skillDetailPanel = existing.GetComponent<SkillDetailPanelUI>();
        }

        if (skillDetailPanel == null)
        {
            if (skillDetailPanelPrefab == null)
            {
                Debug.LogError("[SkillTabUI] Chua gan SkillDetailOverlay prefab. Hay gan SkillDetailOverlay.prefab vao skillDetailPanelPrefab.");
                return false;
            }

            skillDetailPanel = Instantiate(skillDetailPanelPrefab, overlayParent);
            skillDetailPanel.name = "SkillDetailOverlay";
            SetLayerRecursively(skillDetailPanel.gameObject, gameObject.layer);
        }

        // Phá»§ kÃ­n toÃ n bá»™ parent (= character panel content area)
        RectTransform rect = skillDetailPanel.GetComponent<RectTransform>();
        rect.anchorMin  = Vector2.zero;
        rect.anchorMax  = Vector2.one;
        rect.offsetMin  = Vector2.zero;
        rect.offsetMax  = Vector2.zero;
        rect.localScale = Vector3.one;

        // Render trÃªn cÃ¹ng
        skillDetailPanel.transform.SetAsLastSibling();
        skillDetailPanel.gameObject.SetActive(false);
        return true;
    }

    private void EnsureRowPrefab()
    {
        if (skillRowPrefab != null) return;

        Transform existing = transform.Find("__RuntimeSkillRowPrefab");
        if (existing != null)
        {
            skillRowPrefab = existing.GetComponent<SkillRowUI>();
            return;
        }

        var go = new GameObject("__RuntimeSkillRowPrefab", typeof(RectTransform), typeof(Image), typeof(SkillRowUI));
        go.transform.SetParent(transform, false);
        go.layer = gameObject.layer;
        go.SetActive(false);
        skillRowPrefab = go.GetComponent<SkillRowUI>();
    }

    private void ConfigureLabels()
    {
        if (txtSkillPoints != null)
        {
            UIRuntimeAssetHelper.ApplyNotoSans(txtSkillPoints);
            txtSkillPoints.fontSize      = 15f;
            txtSkillPoints.fontStyle     = FontStyles.Bold;
            txtSkillPoints.color         = Color.white;
            txtSkillPoints.alignment     = TextAlignmentOptions.Left;
            txtSkillPoints.enableWordWrapping = false;
            txtSkillPoints.overflowMode  = TextOverflowModes.Ellipsis;

            RectTransform rt = txtSkillPoints.rectTransform;
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(6f, 6f);
            rt.sizeDelta        = new Vector2(-12f, 34f);
        }

        if (txtStatus != null)
        {
            UIRuntimeAssetHelper.ApplyNotoSans(txtStatus);
            txtStatus.fontSize  = 17f;
            txtStatus.fontStyle = FontStyles.Bold;
            txtStatus.alignment = TextAlignmentOptions.Center;
            txtStatus.color     = new Color(1f, 0.82f, 0.3f, 1f);

            RectTransform rt = txtStatus.rectTransform;
            rt.anchorMin        = new Vector2(0.1f, 0.5f);
            rt.anchorMax        = new Vector2(0.9f, 0.5f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta        = new Vector2(0f, 44f);
        }
    }

    private void EnsureListContainerLayout(Transform content)
    {
        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding              = new RectOffset(6, 6, 6, 6);
        layout.spacing              = 6f;
        layout.childAlignment       = TextAnchor.UpperCenter;
        layout.childControlWidth    = true;
        layout.childControlHeight   = true;
        layout.childForceExpandWidth  = true;
        layout.childForceExpandHeight = false;

        var fitter = content.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private TMP_Text FindTextByPartialName(string partialName)
    {
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
            if (labels[i] != null && labels[i].name.Contains(partialName))
                return labels[i];
        return null;
    }

    private static Transform CreateRect(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go.transform;
    }

    private static Transform CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        var image = go.GetComponent<Image>();
        image.color          = color;
        image.raycastTarget  = false;
        return go.transform;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin  = Vector2.zero;
        rect.anchorMax  = Vector2.one;
        rect.offsetMin  = Vector2.zero;
        rect.offsetMax  = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null) return;
        root.layer = layer;

        Transform rootTransform = root.transform;
        for (int i = 0; i < rootTransform.childCount; i++)
            SetLayerRecursively(rootTransform.GetChild(i).gameObject, layer);
    }
}
