using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Overlay panel hiá»ƒn thá»‹ chi tiáº¿t ká»¹ nÄƒng Ä‘Æ°á»£c chá»n.
///
/// Cáº¥u trÃºc tá»± build trong RebuildLayout():
///   Root (transparent, full-stretch)
///   â”œâ”€â”€ Backdrop (dark semi-transparent, full-stretch, click â†’ Hide)
///   â””â”€â”€ ContentBox (centered ~96% width/height)
///       â”œâ”€â”€ Header  (HLG: IconFrame + TitleArea)
///       â”œâ”€â”€ BtnClose (top-right, "âœ•")
///       â”œâ”€â”€ SkillInfoScrollView
///       â””â”€â”€ BtnUpgrade (bottom-right)
///
/// Gá»i Show() khi chá»n skill, Hide() khi Ä‘Ã³ng hoáº·c tab bá»‹ táº¯t.
/// </summary>
public class SkillDetailPanelUI : MonoBehaviour
{
    private const float HeaderHeight      = 100f;
    private const float DetailIconSize    = 84f;
    private const float DetailTitleFont   = 32f;
    private const float DetailBodyFont    = 28f;
    private const float UpgradeBtnWidth   = 160f;
    private const float UpgradeBtnHeight  = 60f;
    private const float CloseBtnSize      = 52f;

    [SerializeField] private Image   iconImage;
    [SerializeField] private TMP_Text txtTitle;
    [SerializeField] private TMP_Text txtBody;
    [SerializeField] private Button  btnUpgrade;
    [SerializeField] private TMP_Text txtUpgrade;

    private PlayerSkillInfo          _info;
    private bool                     _readOnly;
    private Action<PlayerSkillInfo>  _onUpgrade;

    private void Awake()
    {
        BindPrefabReferences();
        BindButtons();
        Refresh();
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        BindPrefabReferences();
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    // â”€â”€ Public API â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void Show()
    {
        BindPrefabReferences();
        BindButtons();
        gameObject.SetActive(true);
        transform.SetAsLastSibling();   // render trÃªn cÃ¹ng
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetData(PlayerSkillInfo info, bool readOnly, Action<PlayerSkillInfo> onUpgrade)
    {
        _info     = info;
        _readOnly = readOnly;
        _onUpgrade = onUpgrade;
        BindPrefabReferences();
        BindButtons();
        Refresh();
    }

    public void SetUpgradeInteractable(bool interactable)
    {
        BindPrefabReferences();
        if (btnUpgrade != null)
            btnUpgrade.interactable = interactable;
    }

    private void BindPrefabReferences()
    {
        if (iconImage == null)
            iconImage = FindChildComponent<Image>("IconImage");

        if (txtTitle == null)
            txtTitle = FindChildComponent<TMP_Text>("TxtTitle");

        if (txtBody == null)
            txtBody = FindChildComponent<TMP_Text>("TxtBody");

        if (btnUpgrade == null)
            btnUpgrade = FindChildComponent<Button>("BtnUpgrade");

        if (txtUpgrade == null && btnUpgrade != null)
            txtUpgrade = btnUpgrade.GetComponentInChildren<TMP_Text>(true);
    }

    private void BindButtons()
    {
        if (btnUpgrade != null)
        {
            btnUpgrade.onClick.RemoveListener(HandleUpgradeClicked);
            btnUpgrade.onClick.AddListener(HandleUpgradeClicked);
        }

        BindHideButton("BtnClose");
        BindHideButton("Backdrop");
    }

    private void UnbindButtons()
    {
        if (btnUpgrade != null)
            btnUpgrade.onClick.RemoveListener(HandleUpgradeClicked);

        UnbindHideButton("BtnClose");
        UnbindHideButton("Backdrop");
    }

    private void BindHideButton(string buttonName)
    {
        Button button = FindChildComponent<Button>(buttonName);
        if (button == null) return;

        button.onClick.RemoveListener(Hide);
        button.onClick.AddListener(Hide);
    }

    private void UnbindHideButton(string buttonName)
    {
        Button button = FindChildComponent<Button>(buttonName);
        if (button != null)
            button.onClick.RemoveListener(Hide);
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            T component = components[i];
            if (component != null && component.gameObject.name == childName)
                return component;
        }

        return null;
    }

    private void RebuildLayout()
    {
      
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }

        // Root: trong suá»‘t, khÃ´ng cháº·n click (backdrop sáº½ cháº·n)
        var rootImg = GetComponent<Image>();
        if (rootImg == null) rootImg = gameObject.AddComponent<Image>();
        rootImg.color         = new Color(0f, 0f, 0f, 0f);
        rootImg.raycastTarget = false;

        // Backdrop
        var backdropGo = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
        backdropGo.transform.SetParent(transform, false);
        backdropGo.layer = gameObject.layer;
        Stretch(backdropGo.GetComponent<RectTransform>());
        var backdropImg = backdropGo.GetComponent<Image>();
        backdropImg.color         = new Color(0f, 0f, 0f, 0.65f);
        backdropImg.raycastTarget = true;
        var backdropBtn = backdropGo.AddComponent<Button>();
        backdropBtn.transition = Selectable.Transition.None;
        backdropBtn.onClick.AddListener(Hide);

     
        var boxGo = new GameObject("ContentBox", typeof(RectTransform), typeof(Image));
        boxGo.transform.SetParent(transform, false);
        boxGo.layer = gameObject.layer;

        var boxRect = boxGo.GetComponent<RectTransform>();
        boxRect.anchorMin  = new Vector2(0.03f, 0.03f);
        boxRect.anchorMax  = new Vector2(0.97f, 0.97f);
        boxRect.offsetMin  = Vector2.zero;
        boxRect.offsetMax  = Vector2.zero;
        boxRect.localScale = Vector3.one;

        var boxImg = boxGo.GetComponent<Image>();
        boxImg.color         = new Color(0.20f, 0.09f, 0.03f, 0.97f);
        boxImg.raycastTarget = true;   // cháº·n click xuyÃªn qua

        var boxOutline = boxGo.AddComponent<Outline>();
        boxOutline.effectColor    = new Color(0.93f, 0.78f, 0.48f, 0.9f);
        boxOutline.effectDistance = new Vector2(2f, -2f);

        Transform box = boxGo.transform;

       
        Transform header    = CreateRect(box, "Header");
        var headerRect      = header.GetComponent<RectTransform>();
        headerRect.anchorMin        = new Vector2(0f, 1f);
        headerRect.anchorMax        = new Vector2(1f, 1f);
        headerRect.pivot            = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -6f);
        headerRect.sizeDelta        = new Vector2(-14f, HeaderHeight);

        var headerLayout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
        headerLayout.padding              = new RectOffset(12, 60, 8, 8);  // right=60 Ä‘á»ƒ trÃ¡nh CloseBtn
        headerLayout.spacing              = 12f;
        headerLayout.childAlignment       = TextAnchor.MiddleLeft;
        headerLayout.childControlWidth    = true;
        headerLayout.childControlHeight   = true;
        headerLayout.childForceExpandWidth  = false;
        headerLayout.childForceExpandHeight = false;

        // Icon frame
        Transform iconFrame = CreatePanel(header, "IconFrame", new Color(0.05f, 0.04f, 0.03f, 1f));
        var iconLe = iconFrame.gameObject.AddComponent<LayoutElement>();
        iconLe.minWidth      = DetailIconSize;
        iconLe.preferredWidth  = DetailIconSize;
        iconLe.minHeight     = DetailIconSize;
        iconLe.preferredHeight = DetailIconSize;

        var iconOutline = iconFrame.gameObject.AddComponent<Outline>();
        iconOutline.effectColor    = new Color(1f, 0.88f, 0.55f, 0.95f);
        iconOutline.effectDistance = new Vector2(2f, -2f);

        iconImage = CreateImage(iconFrame, "IconImage");
        Stretch(iconImage.rectTransform);
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;

        // Title (direct child of header HLG, after iconFrame)
        txtTitle = CreateLabel(header, "TxtTitle", DetailTitleFont, FontStyles.Bold, new Color(0f, 1f, 0.62f, 1f));
        txtTitle.alignment        = TextAlignmentOptions.Left;
        txtTitle.enableWordWrapping = true;
        txtTitle.overflowMode     = TextOverflowModes.Overflow;
        var titleLe = txtTitle.gameObject.AddComponent<LayoutElement>();
        titleLe.flexibleWidth = 1f;
        titleLe.minHeight     = 80f;

        
        var closeBtnGo = CreateButtonGo(box, "BtnClose", "âœ•", new Color(0.65f, 0.15f, 0.10f, 0.95f));
        var closeBtnRect = closeBtnGo.GetComponent<RectTransform>();
        closeBtnRect.anchorMin        = new Vector2(1f, 1f);
        closeBtnRect.anchorMax        = new Vector2(1f, 1f);
        closeBtnRect.pivot            = new Vector2(1f, 1f);
        closeBtnRect.anchoredPosition = new Vector2(-6f, -6f);
        closeBtnRect.sizeDelta        = new Vector2(CloseBtnSize, CloseBtnSize);
        closeBtnGo.GetComponent<Button>().onClick.AddListener(Hide);

      
        Transform scrollRoot = CreatePanel(box, "SkillInfoScrollView", new Color(0.28f, 0.12f, 0.04f, 0.8f));
        var scrollRootRect = scrollRoot.GetComponent<RectTransform>();
        scrollRootRect.anchorMin  = new Vector2(0f, 0f);
        scrollRootRect.anchorMax  = new Vector2(1f, 1f);
        scrollRootRect.offsetMin  = new Vector2(8f,  UpgradeBtnHeight + 18f);
        scrollRootRect.offsetMax  = new Vector2(-22f, -(HeaderHeight + 14f));

        var scrollRect = scrollRoot.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal      = false;
        scrollRect.vertical        = true;
        scrollRect.movementType    = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 30f;

        Transform viewport = CreatePanel(scrollRoot, "Viewport", new Color(0f, 0f, 0f, 0f));
        Stretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().raycastTarget = true;
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        Transform content    = CreateRect(viewport, "Content");
        var contentRect      = content.GetComponent<RectTransform>();
        contentRect.anchorMin        = new Vector2(0f, 1f);
        contentRect.anchorMax        = new Vector2(1f, 1f);
        contentRect.pivot            = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta        = Vector2.zero;

        var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding              = new RectOffset(12, 12, 8, 8);
        contentLayout.childControlWidth    = true;
        contentLayout.childControlHeight   = true;
        contentLayout.childForceExpandWidth  = true;
        contentLayout.childForceExpandHeight = false;

        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        txtBody = CreateLabel(content, "TxtBody", DetailBodyFont, FontStyles.Normal, Color.white);
        txtBody.enableWordWrapping = true;
        txtBody.overflowMode       = TextOverflowModes.Overflow;
        txtBody.lineSpacing        = 6f;
        txtBody.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.content  = contentRect;

        // Scrollbar
        Transform scrollbar   = CreatePanel(box, "VertScrollbar", new Color(0.18f, 0.08f, 0.03f, 0.9f));
        var scrollbarRect     = scrollbar.GetComponent<RectTransform>();
        scrollbarRect.anchorMin        = new Vector2(1f, 0f);
        scrollbarRect.anchorMax        = new Vector2(1f, 1f);
        scrollbarRect.pivot            = new Vector2(1f, 0.5f);
        scrollbarRect.anchoredPosition = new Vector2(-6f, -(HeaderHeight + 8f) / 2f);
        scrollbarRect.sizeDelta        = new Vector2(12f, -(HeaderHeight + UpgradeBtnHeight + 32f));

        Transform handleArea = CreateRect(scrollbar, "SlidingArea");
        Stretch(handleArea.GetComponent<RectTransform>());
        Transform handle = CreatePanel(handleArea, "Handle", new Color(1f, 0.78f, 0.35f, 1f));
        Stretch(handle.GetComponent<RectTransform>());

        var scrollbarComp = scrollbar.gameObject.AddComponent<Scrollbar>();
        scrollbarComp.direction      = Scrollbar.Direction.BottomToTop;
        scrollbarComp.targetGraphic  = handle.GetComponent<Image>();
        scrollbarComp.handleRect     = handle.GetComponent<RectTransform>();
        scrollRect.verticalScrollbar            = scrollbarComp;
        scrollRect.verticalScrollbarVisibility  = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing     = -3f;

    
        var upgradeGo  = CreateButtonGo(box, "BtnUpgrade", "NÃ¢ng cáº¥p", new Color(0.55f, 0.32f, 0.06f, 1f));
        var upgradeRect = upgradeGo.GetComponent<RectTransform>();
        upgradeRect.anchorMin        = new Vector2(1f, 0f);
        upgradeRect.anchorMax        = new Vector2(1f, 0f);
        upgradeRect.pivot            = new Vector2(1f, 0f);
        upgradeRect.anchoredPosition = new Vector2(-10f, 10f);
        upgradeRect.sizeDelta        = new Vector2(UpgradeBtnWidth, UpgradeBtnHeight);

        btnUpgrade = upgradeGo.GetComponent<Button>();
        btnUpgrade.onClick.RemoveAllListeners();
        btnUpgrade.onClick.AddListener(HandleUpgradeClicked);

        Debug.Log("[SkillDetail] RebuildLayout xong.");
    }

   

    private void Refresh()
    {
        if (_info == null)
        {
            if (txtTitle   != null) txtTitle.text = "Chá»n ká»¹ nÄƒng";
            if (txtBody    != null) txtBody.text  = "Chá»n má»™t ká»¹ nÄƒng Ä‘á»ƒ xem chi tiáº¿t.";
            if (btnUpgrade != null) btnUpgrade.gameObject.SetActive(false);
            if (iconImage  != null) iconImage.enabled = false;
            return;
        }

      
        if (txtTitle != null)
            txtTitle.text = _info.skill_name;

        // Icon
        if (iconImage != null)
        {
            string iconKey;
            Sprite icon = ResolveSkillIcon(_info, out iconKey);
            iconImage.sprite  = icon;
            iconImage.color   = icon != null ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            iconImage.enabled = true;

            if (icon == null)
                Debug.LogWarning($"[SkillDetail] KhÃ´ng tÃ¬m tháº¥y icon cho '{_info.skill_name}' " +
                                 $"(icon_id='{_info.icon_id}', skill_code='{_info.skill_code}')");
        }

        // Body
        if (txtBody != null)
        {
            txtBody.text = BuildBody(_info);

            var contentRect = txtBody.transform.parent.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                var scrollRectComp = contentRect.GetComponentInParent<ScrollRect>();
                if (scrollRectComp != null)
                    scrollRectComp.verticalNormalizedPosition = 1f;
            }
        }

      
        if (btnUpgrade != null)
        {
            bool maxed = _info.current_level >= _info.max_level && _info.max_level > 0;
            bool show  = !_readOnly;
            btnUpgrade.gameObject.SetActive(show);
            btnUpgrade.interactable = !_readOnly && _info.can_upgrade && !maxed;
        }

        if (txtUpgrade != null)
            txtUpgrade.text = "Nâng cấp";
    }

    private void HandleUpgradeClicked()
    {
        if (_info == null || _readOnly) return;
        _onUpgrade?.Invoke(_info);
    }

  

    private static string BuildBody(PlayerSkillInfo info)
    {
        var sb    = new StringBuilder(512);
        bool maxed = info.current_level >= info.max_level && info.max_level > 0;

        string description = string.IsNullOrWhiteSpace(info.description) ? "KhÃ´ng cÃ³ mÃ´ táº£." : info.description;
        sb.AppendLine(description);
        sb.AppendLine();
        sb.AppendLine($"Cáº¥p hiá»‡n táº¡i:  {info.current_level} / {info.max_level}");
        sb.AppendLine($"Level yÃªu cáº§u má»Ÿ: {Mathf.Max(1, info.level_to_unlock)}");
        if (info.gene_tier_required > 0)
            sb.AppendLine($"Gene yÃªu cáº§u: Tier {info.gene_tier_required}");
        sb.AppendLine(maxed
            ? "<color=#FFE000>ÄÃ£ Ä‘áº¡t cáº¥p tá»‘i Ä‘a</color>"
            : $"Cáº¥p tiáº¿p: cáº§n lv.{info.next_level_player_req}, {info.next_level_sp_cost} Ä‘iá»ƒm");
        sb.AppendLine($"MP sá»­ dá»¥ng: {info.current_mp_cost}");
        sb.AppendLine($"Há»“i chiÃªu: {FormatNumber(info.current_cooldown_sec)} giÃ¢y");
        sb.AppendLine();
        sb.AppendLine("<color=#FFE000>â”€â”€â”€ Thuá»™c tÃ­nh theo cáº¥p â”€â”€â”€</color>");

        if (info.level_details == null || info.level_details.Length == 0)
        {
            sb.AppendLine("ChÆ°a cÃ³ cáº¥u hÃ¬nh level trong DB.");
            return sb.ToString();
        }

        string label  = ResolveEffectLabel(info.skill_code);
        string suffix = ResolveEffectSuffix(info.skill_code);
        foreach (var lv in info.level_details)
        {
            string desc   = string.IsNullOrWhiteSpace(lv.desc) ? string.Empty : $" â€” {lv.desc}";
            string effect = $"{label}: {FormatNumber(lv.effect_value)}{suffix}";
            sb.AppendLine($"Lv.{lv.level}: {effect}, MP {lv.mp_cost}, há»“i {FormatNumber(lv.cooldown_sec)}s" +
                          $", cáº§n lv.{lv.level_req}, {lv.sp_cost} Ä‘iá»ƒm{desc}");
        }

        return sb.ToString();
    }

    private static string ResolveEffectLabel(string skillCode)
    {
        if (string.IsNullOrWhiteSpace(skillCode)) return "Hiá»‡u lá»±c";
        string code = skillCode.ToUpperInvariant();
        if (code.Contains("DASH") || code.Contains("STEP"))  return "Khoáº£ng cÃ¡ch";
        if (code.Contains("VINE"))                            return "Thá»i gian trÃ³i";
        if (code.Contains("HEAL"))                            return "Há»“i HP";
        if (code.Contains("WATER_ARMOR") || code.Contains("EARTH_SHIELD")) return "GiÃ¡p cá»™ng";
        if (code.Contains("METAL_SHIELD"))                    return "Báº¥t tá»­";
        if (code.Contains("AURA"))                            return "TÄƒng táº¥n cÃ´ng";
        if (code.Contains("BLINK"))                           return "SÃ¡t thÆ°Æ¡ng má»—i tick";
        return "SÃ¡t thÆ°Æ¡ng";
    }

    private static string ResolveEffectSuffix(string skillCode)
    {
        if (string.IsNullOrWhiteSpace(skillCode)) return string.Empty;
        string code = skillCode.ToUpperInvariant();
        if (code.Contains("DASH") || code.Contains("STEP"))  return " Ã´";
        if (code.Contains("VINE") || code.Contains("METAL_SHIELD")) return " giÃ¢y";
        if (code.Contains("AURA"))                            return "%";
        return string.Empty;
    }

    private static string FormatNumber(float value) =>
        Mathf.Abs(value - Mathf.Round(value)) < 0.001f
            ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

    private static Sprite ResolveSkillIcon(PlayerSkillInfo info, out string resolvedKey)
    {
        resolvedKey = null;
        if (info == null) return null;

        Sprite icon = TryLoadSkillIcon(info.icon_id, out resolvedKey);
        if (icon != null) return icon;

        icon = TryLoadSkillIcon(info.skill_code, out resolvedKey);
        if (icon != null) return icon;

        string idKey = info.skill_id > 0 ? info.skill_id.ToString() : null;
        return TryLoadSkillIcon(idKey, out resolvedKey);
    }

    private static Sprite TryLoadSkillIcon(string key, out string resolvedKey)
    {
        resolvedKey = null;
        if (string.IsNullOrWhiteSpace(key)) return null;
        string k = key.Trim();
        if (k == "0") return null;

        Sprite icon = SkillIconDatabase.Instance != null
            ? SkillIconDatabase.Instance.GetIcon(k)
            : null;
        if (icon == null)
            icon = Resources.Load<Sprite>($"SkillIcons/{k}");

        if (icon != null) resolvedKey = k;
        return icon;
    }

    // â”€â”€ UI factory helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Creates a button GO with centered text label. Returns the GO (not the button).</summary>
    private static GameObject CreateButtonGo(Transform parent, string name, string label, Color bgColor)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        var img = go.GetComponent<Image>();
        img.color = bgColor;

        var btn = go.GetComponent<Button>();
        var cs  = btn.colors;
        cs.highlightedColor = new Color(
            Mathf.Min(bgColor.r + 0.15f, 1f),
            Mathf.Min(bgColor.g + 0.15f, 1f),
            Mathf.Min(bgColor.b + 0.15f, 1f), 1f);
        cs.pressedColor = new Color(
            Mathf.Max(bgColor.r - 0.1f, 0f),
            Mathf.Max(bgColor.g - 0.1f, 0f),
            Mathf.Max(bgColor.b - 0.1f, 0f), 1f);
        btn.colors = cs;
        btn.targetGraphic = img;

        var btnOutline = go.AddComponent<Outline>();
        btnOutline.effectColor    = new Color(1f, 0.88f, 0.55f, 0.9f);
        btnOutline.effectDistance = new Vector2(1f, -1f);

        // Label
        var lblGo = new GameObject("Lbl", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGo.transform.SetParent(go.transform, false);
        lblGo.layer = go.layer;
        Stretch(lblGo.GetComponent<RectTransform>());

        var txt = lblGo.GetComponent<TextMeshProUGUI>();
        txt.text           = label;
        txt.fontSize       = 26f;
        txt.fontStyle      = FontStyles.Bold;
        txt.color          = Color.white;
        txt.alignment      = TextAlignmentOptions.Center;
        txt.raycastTarget  = false;
        UIRuntimeAssetHelper.ApplyNotoSans(txt);

        // Store label ref for upgrades
        if (name == "BtnUpgrade")
        {
            // txtUpgrade is referenced later via GetComponentInChildren
        }

        return go;
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
        image.color         = color;
        image.raycastTarget = false;
        return go.transform;
    }

    private static Image CreateImage(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go.GetComponent<Image>();
    }

    private static TMP_Text CreateLabel(Transform parent, string name, float fontSize, FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text          = string.Empty;
        text.fontSize      = fontSize;
        text.fontStyle     = style;
        text.color         = color;
        text.alignment     = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.overflowMode  = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        UIRuntimeAssetHelper.ApplyNotoSans(text);
        return text;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin  = Vector2.zero;
        rect.anchorMax  = Vector2.one;
        rect.offsetMin  = Vector2.zero;
        rect.offsetMax  = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
