using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Compact skill row used by the character skill tab.
// Runtime rebuild keeps old prefabs usable even when their hierarchy is stale.
public class SkillRowUI : MonoBehaviour
{
    private const float RowHeight = 104f;
    private const float IconSize = 84f;
    private const float RowSpacing = 12f;
    private const float SkillNameFontSize = 32f;
    private const float SkillLevelFontSize = 26f;
    private const float SkillNameLabelHeight = 48f;
    private const float SkillLevelLabelHeight = 34f;
    private const float TextLineSpacing = 2f;

    [Header("UI References")]
    [SerializeField] private TMP_Text txtSkillName;
    [SerializeField] private TMP_Text txtLevel;
    [SerializeField] private Image iconImage;

    private Image _background;
    private Button _rowButton;
    private PlayerSkillInfo _info;
    private Action<PlayerSkillInfo> _onSelected;
    private bool _isReadOnlyView;
    private bool _isSelected;

    private static readonly Color RowNormal = new Color(0.20f, 0.10f, 0.04f, 0.82f);
    private static readonly Color RowSelected = new Color(0.50f, 0.25f, 0.08f, 0.95f);
    private static readonly Color RowDisabled = new Color(0.13f, 0.13f, 0.13f, 0.72f);

    private void Awake()
    {
        // Luôn rebuild để đảm bảo layout compact đúng, không phụ thuộc prefab wire
        RebuildCompactLayout();
    }

    public void SetData(
        PlayerSkillInfo info,
        int playerId,
        Action onUpgraded,
        bool readOnly = false)
    {
        SetData(info, readOnly, null);
    }

    public void SetData(PlayerSkillInfo info, bool readOnly, Action<PlayerSkillInfo> onSelected)
    {
        _info = info;
        _isReadOnlyView = readOnly;
        _onSelected = onSelected;
        RefreshUI();
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        RefreshBackground();
    }

    private void RebuildCompactLayout()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }

        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(0f, RowHeight);
        }

        _background = GetComponent<Image>();
        if (_background == null)
            _background = gameObject.AddComponent<Image>();
        _background.color = RowNormal;
        _background.raycastTarget = true;

        var outline = GetComponent<Outline>();
        if (outline == null)
            outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.78f, 0.32f, 0.72f);
        outline.effectDistance = new Vector2(1f, -1f);

        _rowButton = GetComponent<Button>();
        if (_rowButton == null)
            _rowButton = gameObject.AddComponent<Button>();
        _rowButton.targetGraphic = _background;
        _rowButton.transition = Selectable.Transition.ColorTint;
        _rowButton.onClick.RemoveAllListeners();
        _rowButton.onClick.AddListener(HandleRowClicked);

        var layout = GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 12, 10, 10);
        layout.spacing = RowSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var rootLe = GetComponent<LayoutElement>();
        if (rootLe == null)
            rootLe = gameObject.AddComponent<LayoutElement>();
        rootLe.minHeight = RowHeight;
        rootLe.preferredHeight = RowHeight;
        rootLe.flexibleWidth = 1f;

        Transform iconFrame = CreatePanel(transform, "IconFrame", new Color(0.05f, 0.04f, 0.03f, 1f));
        var iconLe = iconFrame.gameObject.AddComponent<LayoutElement>();
        iconLe.minWidth = IconSize;
        iconLe.preferredWidth = IconSize;
        iconLe.minHeight = IconSize;
        iconLe.preferredHeight = IconSize;
        iconLe.flexibleWidth = 0f;

        var iconOutline = iconFrame.gameObject.AddComponent<Outline>();
        iconOutline.effectColor = new Color(1f, 0.88f, 0.55f, 0.95f);
        iconOutline.effectDistance = new Vector2(2f, -2f);

        iconImage = CreateImage(iconFrame, "IconImage");
        Stretch(iconImage.rectTransform);
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;

        Transform textBlock = CreateRect(transform, "TextBlock");
        var textLe = textBlock.gameObject.AddComponent<LayoutElement>();
        textLe.minWidth = 1f;
        textLe.preferredWidth = 1f;
        textLe.flexibleWidth = 1f;
        textLe.minHeight = IconSize;
        textLe.preferredHeight = IconSize;

        var textLayout = textBlock.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayout.spacing = TextLineSpacing;
        textLayout.childAlignment = TextAnchor.MiddleLeft;
        textLayout.childControlWidth = true;
        textLayout.childControlHeight = true;
        textLayout.childForceExpandWidth = true;
        textLayout.childForceExpandHeight = false;

        txtSkillName = CreateLabel(textBlock, "TxtSkillName", SkillNameFontSize, SkillNameLabelHeight, FontStyles.Bold, new Color(0f, 1f, 0.62f, 1f));
        txtLevel = CreateLabel(textBlock, "TxtLevel", SkillLevelFontSize, SkillLevelLabelHeight, FontStyles.Bold, Color.white);
    }

    private void RefreshUI()
    {
        if (_info == null)
        {
            Debug.Log("[SkillRow] RefreshUI: _info là null, bỏ qua.");
            return;
        }

        Debug.Log($"[SkillRow] RefreshUI skill_id={_info.skill_id} name='{_info.skill_name}' " +
                  $"code='{_info.skill_code}' icon_id='{_info.icon_id}' " +
                  $"lv={_info.current_level}/{_info.max_level}");

        if (txtSkillName != null)
        {
            txtSkillName.text = _info.skill_name;
            txtSkillName.enableWordWrapping = false;
            txtSkillName.overflowMode = TextOverflowModes.Overflow;
            RefreshTextMesh(txtSkillName);
        }

        bool maxed = _info.current_level >= _info.max_level && _info.max_level > 0;
        if (txtLevel != null)
        {
            txtLevel.text = _info.current_level <= 0
                ? $"Khóa - mở ở Lv {Mathf.Max(1, _info.level_to_unlock)}"
                : maxed
                ? "<color=#FFE000>Đã đạt cấp tối đa</color>"
                : $"Lv {_info.current_level}/{_info.max_level}";
            txtLevel.enableWordWrapping = false;
            txtLevel.overflowMode = TextOverflowModes.Overflow;
            RefreshTextMesh(txtLevel);
        }

        if (iconImage != null)
        {
            string iconKey;
            Sprite icon = ResolveSkillIcon(_info, out iconKey);

            if (icon != null)
            {
                Debug.Log($"[SkillRow] Icon OK: '{iconKey}' cho '{_info.skill_name}'");
                iconImage.sprite = icon;
                iconImage.color  = Color.white;
                iconImage.enabled = true;
            }
            else
            {
                // icon_id có thể chưa có trong DB — thử load trực tiếp từ Resources
                Sprite direct = null;
                if (!string.IsNullOrWhiteSpace(_info.icon_id))
                    direct = Resources.Load<Sprite>($"SkillIcons/{_info.icon_id.Trim()}");
                if (direct == null && !string.IsNullOrWhiteSpace(_info.skill_code))
                    direct = Resources.Load<Sprite>($"SkillIcons/{_info.skill_code.Trim()}");

                if (direct != null)
                {
                    Debug.Log($"[SkillRow] Icon direct-load OK: '{_info.icon_id}' cho '{_info.skill_name}'");
                    iconImage.sprite  = direct;
                    iconImage.color   = Color.white;
                    iconImage.enabled = true;
                }
                else
                {
                    Debug.LogWarning($"[SkillRow] Không tìm thấy icon cho '{_info.skill_name}' " +
                                     $"(icon_id='{_info.icon_id}', code='{_info.skill_code}', " +
                                     $"SkillIconDB={(SkillIconDatabase.Instance != null ? "OK" : "NULL")})");
                    iconImage.sprite  = null;
                    iconImage.color   = new Color(0.55f, 0.55f, 0.55f, 1f);
                    iconImage.enabled = true;

                    // Nếu SkillIconDatabase chưa khởi tạo xong, thử lại sau 1 frame
                    if (SkillIconDatabase.Instance == null)
                        StartCoroutine(RetryIconNextFrame());
                }
            }
        }

        RefreshBackground();
    }

    private System.Collections.IEnumerator RetryIconNextFrame()
    {
        yield return null; // chờ 1 frame
        if (_info != null && iconImage != null && iconImage.sprite == null)
        {
            string iconKey;
            Sprite icon = ResolveSkillIcon(_info, out iconKey);
            if (icon != null)
            {
                iconImage.sprite  = icon;
                iconImage.color   = Color.white;
                iconImage.enabled = true;
                Debug.Log($"[SkillRow] Retry icon OK: '{iconKey}' cho '{_info.skill_name}'");
            }
        }
    }

    private void RefreshBackground()
    {
        if (_background == null)
            return;

        if (_isReadOnlyView)
            _background.color = _isSelected ? RowSelected : RowDisabled;
        else
            _background.color = _isSelected ? RowSelected : RowNormal;
    }

    private void HandleRowClicked()
    {
        if (_info == null)
            return;

        _onSelected?.Invoke(_info);
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
        image.color = color;
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

    private static TMP_Text CreateLabel(Transform parent, string name, float fontSize, float height, FontStyles style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(0f, height);
        rect.localScale = Vector3.one;

        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = string.Empty;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Overflow;
        text.margin = Vector4.zero;
        text.raycastTarget = false;
        UIRuntimeAssetHelper.ApplyNotoSans(text);

        var le = go.AddComponent<LayoutElement>();
        le.minWidth = 1f;
        le.preferredWidth = 1f;
        le.minHeight = height;
        le.preferredHeight = height;
        le.flexibleWidth = 1f;
        le.flexibleHeight = 0f;
        RefreshTextMesh(text);
        return text;
    }

    private static void RefreshTextMesh(TMP_Text text)
    {
        if (text == null)
            return;

        text.enabled = true;
        text.gameObject.SetActive(true);
        text.raycastTarget = false;
        text.alpha = 1f;
        text.canvasRenderer.SetAlpha(1f);
        text.SetLayoutDirty();
        text.SetVerticesDirty();
        text.SetMaterialDirty();
        text.ForceMeshUpdate();
    }

    private static Sprite ResolveSkillIcon(PlayerSkillInfo info, out string resolvedKey)
    {
        resolvedKey = null;
        if (info == null)
            return null;

        Sprite icon = TryLoadSkillIcon(info.icon_id, out resolvedKey);
        if (icon != null)
            return icon;

        icon = TryLoadSkillIcon(info.skill_code, out resolvedKey);
        if (icon != null)
            return icon;

        string idKey = info.skill_id > 0 ? info.skill_id.ToString() : null;
        return TryLoadSkillIcon(idKey, out resolvedKey);
    }

    private static Sprite TryLoadSkillIcon(string key, out string resolvedKey)
    {
        resolvedKey = null;
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string trimmedKey = key.Trim();
        if (trimmedKey == "0")
            return null;

        Sprite icon = SkillIconDatabase.Instance != null
            ? SkillIconDatabase.Instance.GetIcon(trimmedKey)
            : null;

        if (icon == null)
            icon = Resources.Load<Sprite>($"SkillIcons/{trimmedKey}");

        if (icon != null)
            resolvedKey = trimmedKey;

        return icon;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }
}
