using System;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Overlay panel hiển thị chi tiết kỹ năng được chọn.
// Cấu trúc tự build trong RebuildLayout():
// Root (transparent, full-stretch)
// ├── Backdrop (dark semi-transparent, full-stretch, click → Hide)
// └── ContentBox (centered ~96% width/height)
// ├── Header  (HLG: IconFrame + TitleArea)
// ├── BtnClose (top-right, "✕")
// ├── SkillInfoScrollView
// └── BtnUpgrade (bottom-right)
// Gọi Show() khi chọn skill, Hide() khi đóng hoặc tab bị tắt.
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

    // Hàm public để script hoặc hệ thống khác gọi vào.

    public void Show()
    {
        BindPrefabReferences();
        BindButtons();
        gameObject.SetActive(true);
        Refresh();
        transform.SetAsLastSibling();   // render trên cùng
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

        if (iconImage == null || txtTitle == null || txtBody == null || btnUpgrade == null)
        {
            RebuildLayout();
            if (txtUpgrade == null && btnUpgrade != null)
                txtUpgrade = btnUpgrade.GetComponentInChildren<TMP_Text>(true);
        }

        EnsureScrollLayout();
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

    private Transform FindChildTransform(string childName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];
            if (child != null && child.name == childName)
                return child;
        }

        return null;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private void EnsureScrollLayout()
    {
        Transform scrollRoot = FindChildTransform("SkillInfoScrollView");
        Transform viewport = FindChildTransform("Viewport");
        Transform content = FindChildTransform("Content");

        if (txtBody != null && content != null && txtBody.transform.parent != content)
            txtBody.transform.SetParent(content, false);

        if (scrollRoot != null)
        {
            var scrollRect = GetOrAdd<ScrollRect>(scrollRoot.gameObject);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 30f;
            if (viewport is RectTransform viewportRect)
                scrollRect.viewport = viewportRect;
            if (content is RectTransform contentRect)
                scrollRect.content = contentRect;
        }

        if (viewport != null)
        {
            var viewportRect = viewport.GetComponent<RectTransform>();
            if (viewportRect != null)
                Stretch(viewportRect);

            var viewportImage = GetOrAdd<Image>(viewport.gameObject);
            viewportImage.color = Color.white;
            viewportImage.raycastTarget = true;

            var mask = GetOrAdd<Mask>(viewport.gameObject);
            mask.showMaskGraphic = false;
        }

        if (content != null)
        {
            var contentRect = content.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                contentRect.anchorMin = new Vector2(0f, 1f);
                contentRect.anchorMax = new Vector2(1f, 1f);
                contentRect.pivot = new Vector2(0.5f, 1f);
                contentRect.anchoredPosition = Vector2.zero;
                contentRect.localScale = Vector3.one;
            }

            var contentLayout = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
            contentLayout.padding = new RectOffset(12, 12, 8, 8);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        EnsureBodyTextVisible();
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

        // Root: trong suốt, không chặn click (backdrop sẽ chặn)
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
        boxImg.raycastTarget = true;   // chặn click xuyên qua

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
        headerLayout.padding              = new RectOffset(12, 60, 8, 8);  // right=60 để tránh CloseBtn
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

        
        var closeBtnGo = CreateButtonGo(box, "BtnClose", "✕", new Color(0.65f, 0.15f, 0.10f, 0.95f));
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

        Transform viewport = CreatePanel(scrollRoot, "Viewport", Color.white);
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
        txtBody.richText = true;
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

    
        var upgradeGo  = CreateButtonGo(box, "BtnUpgrade", "Nâng cấp", new Color(0.55f, 0.32f, 0.06f, 1f));
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
            if (txtTitle   != null) txtTitle.text = "Chọn kỹ năng";
            if (txtBody    != null) txtBody.text  = "Chọn một kỹ năng để xem chi tiết.";
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
                Debug.LogWarning($"[SkillDetail] Không tìm thấy icon cho '{_info.skill_name}' " +
                                 $"(icon_id='{_info.icon_id}', skill_code='{_info.skill_code}')");
        }

        // Body
        if (txtBody != null)
        {
            EnsureBodyTextVisible();
            txtBody.text = BuildBody(_info);
            UpdateBodyTextLayout();

            var contentRect = txtBody.transform.parent.GetComponent<RectTransform>();
            if (contentRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
                Canvas.ForceUpdateCanvases();
                var scrollRectComp = contentRect.GetComponentInParent<ScrollRect>();
                if (scrollRectComp != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectComp.GetComponent<RectTransform>());
                    scrollRectComp.verticalNormalizedPosition = 1f;
                }
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

    private void EnsureBodyTextVisible()
    {
        if (txtBody == null)
            return;

        txtBody.gameObject.SetActive(true);
        txtBody.enabled = true;
        txtBody.color = Color.white;
        txtBody.alpha = 1f;
        txtBody.fontSize = DetailBodyFont;
        txtBody.enableWordWrapping = true;
        txtBody.richText = true;
        txtBody.overflowMode = TextOverflowModes.Overflow;
        txtBody.alignment = TextAlignmentOptions.TopLeft;
        txtBody.raycastTarget = false;
        txtBody.canvasRenderer.SetAlpha(1f);
        UIRuntimeAssetHelper.ApplyNotoSans(txtBody);

        RectTransform rect = txtBody.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.localScale = Vector3.one;

        var layoutElement = txtBody.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = txtBody.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = 120f;
    }

    private void UpdateBodyTextLayout()
    {
        if (txtBody == null)
            return;

        txtBody.ForceMeshUpdate();
        float preferredHeight = Mathf.Max(120f, txtBody.preferredHeight + 24f);

        var layoutElement = txtBody.GetComponent<LayoutElement>();
        if (layoutElement == null)
            layoutElement = txtBody.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.minHeight = preferredHeight;
        layoutElement.preferredHeight = preferredHeight;

        var bodyRect = txtBody.rectTransform;
        bodyRect.sizeDelta = new Vector2(bodyRect.sizeDelta.x, preferredHeight);

        var contentRect = txtBody.transform.parent as RectTransform;
        if (contentRect != null)
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, preferredHeight + 16f);
    }

  

    private static string BuildBody(PlayerSkillInfo info)
    {
        var sb    = new StringBuilder(512);
        bool maxed = info.current_level >= info.max_level && info.max_level > 0;

        string description = string.IsNullOrWhiteSpace(info.description) ? "Không có mô tả." : info.description;
        sb.AppendLine(description);
        sb.AppendLine();
        sb.AppendLine($"Cấp hiện tại:  {info.current_level} / {info.max_level}");
        sb.AppendLine($"Level yêu cầu mở: {Mathf.Max(1, info.level_to_unlock)}");
        if (info.gene_tier_required > 0)
            sb.AppendLine($"Gene yêu cầu: Tier {info.gene_tier_required}");
        sb.AppendLine(maxed
            ? "<color=#FFE000>Đã đạt cấp tối đa</color>"
            : $"Cấp tiếp: cần lv.{info.next_level_player_req}, {info.next_level_sp_cost} điểm");
        sb.AppendLine($"MP sử dụng: {info.current_mp_cost}");
        sb.AppendLine($"Hồi chiêu: {FormatNumber(info.current_cooldown_sec)} giây");
        sb.AppendLine();
        sb.AppendLine("<color=#FFE000>Chỉ số hiện tại</color>");
        AppendCurrentStats(sb, info);
        sb.AppendLine();
        sb.AppendLine("<color=#FFE000>─── Thuộc tính theo cấp ───</color>");

        SkillLevelInfo[] levelDetails = ResolveLevelDetails(info);
        if (levelDetails == null || levelDetails.Length == 0)
        {
            sb.AppendLine("Chưa có cấu hình level trong DB.");
            return sb.ToString();
        }

        string label  = ResolveEffectLabel(info.skill_code);
        string suffix = ResolveEffectSuffix(info.skill_code);
        foreach (var lv in levelDetails)
        {
            string desc   = string.IsNullOrWhiteSpace(lv.desc) ? string.Empty : $" — {lv.desc}";
            string effect = $"{label}: {FormatNumber(lv.effect_value)}{suffix}";
            sb.AppendLine($"Lv.{lv.level}: {effect}, MP {lv.mp_cost}, hồi {FormatNumber(lv.cooldown_sec)}s" +
                          $", cần lv.{lv.level_req}, {lv.sp_cost} điểm{desc}");
        }

        return sb.ToString();
    }

    private static SkillLevelInfo[] ResolveLevelDetails(PlayerSkillInfo info)
    {
        if (info == null)
            return Array.Empty<SkillLevelInfo>();

        if (info.level_details != null && info.level_details.Length > 0)
            return info.level_details;

        return new[]
        {
            new SkillLevelInfo
            {
                level = Mathf.Max(1, info.current_level),
                level_req = Mathf.Max(1, info.level_to_unlock),
                sp_cost = Mathf.Max(0, info.next_level_sp_cost),
                effect_value = info.current_effect_value,
                mp_cost = info.current_mp_cost,
                cooldown_sec = info.current_cooldown_sec,
                desc = string.IsNullOrWhiteSpace(info.next_level_desc) ? "Dữ liệu từ chỉ số hiện tại" : info.next_level_desc
            }
        };
    }

    private static void AppendCurrentStats(StringBuilder sb, PlayerSkillInfo info)
    {
        float baseEffect = info.current_effect_value;
        float totalEffect = info.current_total_effect_value > 0f
            ? info.current_total_effect_value
            : baseEffect + Mathf.Max(0f, info.current_attack_bonus);

        string label = ResolveEffectLabel(info.skill_code);
        string suffix = ResolveEffectSuffix(info.skill_code);
        string attackSuffix = IsPercentAttackBuff(info.skill_code) ? "%" : string.Empty;

        sb.AppendLine($"Loại hiệu ứng: {label}");
        AppendStatLine(sb, $"{label} cơ bản", baseEffect, suffix, false);
        AppendStatLine(sb, "Cộng tấn công", info.current_attack_bonus, attackSuffix, true);
        AppendStatLine(sb, "Cộng / hồi HP", info.current_hp_bonus, string.Empty, true);
        AppendStatLine(sb, "Cộng / hồi MP", info.current_mp_bonus, string.Empty, true);
        AppendStatLine(sb, "Cộng phòng thủ", info.current_defense_bonus, string.Empty, true);
        AppendStatLine(sb, "Né tránh / di chuyển", info.current_evasion_bonus, suffix, true);
        AppendStatLine(sb, "Tổng hiệu lực", totalEffect, suffix, false);
    }

    private static void AppendStatLine(StringBuilder sb, string label, float value, string suffix, bool signed)
    {
        string sign = signed && value > 0.001f ? "+" : string.Empty;
        sb.AppendLine($"{label}: {sign}{FormatNumber(value)}{suffix}");
    }

    private static void AppendNonZero(StringBuilder sb, string label, float value, string suffix)
    {
        if (Mathf.Abs(value) <= 0.001f)
            return;

        sb.AppendLine($"{label}: +{FormatNumber(value)}{suffix}");
    }

    private static bool IsPercentAttackBuff(string skillCode)
    {
        if (string.IsNullOrWhiteSpace(skillCode))
            return false;

        return skillCode.ToUpperInvariant().Contains("AURA");
    }

    private static string ResolveEffectLabel(string skillCode)
    {
        if (string.IsNullOrWhiteSpace(skillCode)) return "Hiệu lực";
        string code = skillCode.ToUpperInvariant();
        if (code.Contains("DASH") || code.Contains("STEP"))  return "Khoảng cách";
        if (code.Contains("VINE"))                            return "Thời gian trói";
        if (code.Contains("HEAL"))                            return "Hồi HP";
        if (code.Contains("WATER_ARMOR") || code.Contains("EARTH_SHIELD")) return "Giáp cộng";
        if (code.Contains("METAL_SHIELD"))                    return "Bất tử";
        if (code.Contains("AURA"))                            return "Tăng tấn công";
        if (code.Contains("BLINK"))                           return "Sát thương mỗi tick";
        return "Sát thương";
    }

    private static string ResolveEffectSuffix(string skillCode)
    {
        if (string.IsNullOrWhiteSpace(skillCode)) return string.Empty;
        string code = skillCode.ToUpperInvariant();
        if (code.Contains("DASH") || code.Contains("STEP"))  return " ô";
        if (code.Contains("VINE") || code.Contains("METAL_SHIELD")) return " giây";
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

    // UI factory helpers

    // Creates a button GO with centered text label. Returns the GO (not the button).
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
        text.richText      = true;
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
