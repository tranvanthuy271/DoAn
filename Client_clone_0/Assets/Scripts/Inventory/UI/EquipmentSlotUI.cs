using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// EquipmentSlotUI - Hiển thị 1 ô trang bị trong UI Equipment Panel
// Mỗi slot đại diện cho 1 loại trang bị (Weapon, Helmet, Armor, Pants, Boots, Accessory)
// Setup trong Unity:
// 1. Tạo prefab với Image (icon), TMP_Text (slot label), Button (click)
// 2. Gắn script này lên prefab
// 3. Kéo reference vào Inspector
public class EquipmentSlotUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Image hiển thị icon của item đang trang bị")]
    [SerializeField] private Image iconImage;

    [Tooltip("Image placeholder khi chưa có item (icon mờ)")]
    [SerializeField] private Image placeholderImage;

    [Tooltip("Text hiển thị tên loại slot (Vũ khí, Mũ, Giáp, ...)")]
    [SerializeField] private TMP_Text slotLabelText;

    [Tooltip("Text hiển thị tên item đang trang bị")]
    [SerializeField] private TMP_Text itemNameText;

    [Tooltip("Nút mở panel nâng cấp (ẩn khi slot trống)")]
    [SerializeField] private Button upgradeButton;

    [Header("Settings")]
    [Tooltip("Loại slot trang bị")]
    [SerializeField] private EquipmentSlotType slotType = EquipmentSlotType.Weapon;

    [Header("Tier Effect")]
    [Tooltip("ScriptableObject config viền/bg theo level — kéo thẳng EquipmentTierConfig vào đây")]
    [SerializeField] private EquipmentTierConfig tierConfig;

    [Tooltip("Image viền slot (con của slot này)")]
    [SerializeField] private Image borderImage;

    [Tooltip("Image background slot (con của slot này)")]
    [SerializeField] private Image bgImage;

    // Animator components cho hiệu ứng viền + bg theo tier (tự tạo runtime)
    private Animator _borderAnim;
    private Animator _bgAnim;
    private SpriteRenderer _borderSR;
    private SpriteRenderer _bgSR;
    private int _currentTierLevel = -1;
    private bool _tierAppliedWhileActive;
    private bool _tierNeedsActiveRestart;

    [Header("Icon Layout")]
    [Tooltip("Padding để icon không chạm viền slot.")]
    [SerializeField] private Vector2 iconPadding = new Vector2(16f, 16f);
    [Tooltip("Kích thước fallback nếu RectTransform icon chưa sẵn sàng.")]
    [SerializeField] private Vector2 fallbackIconMaxSize = new Vector2(84f, 84f);

    // Data hiện tại
    private EquipmentItemDto currentItem;
    private Vector2 iconMaxSize;

    // Event khi click vào slot (để mở chi tiết hoặc tháo trang bị)
    public event Action<EquipmentSlotType, EquipmentItemDto> OnSlotClicked;

    public EquipmentSlotType SlotType => slotType;

    private void Awake()
    {
        CacheIconBounds();
        NormalizeTierImageReferences();
        ApplyTierLayerOrder();
        ApplyTheme();

        // Gán label theo slot type
        UpdateSlotLabel();
    }

    private void OnEnable()
    {
        StartCoroutine(DelayedReplayTierEffect());
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    private IEnumerator DelayedReplayTierEffect()
    {
        // Wait 1 frame so the Canvas hierarchy is fully initialized
        // and Animator.Rebind() can properly resolve Image.sprite bindings.
        yield return null;
        ReplayTierEffectWhenActive();
    }

    private void Update()
    {
        if (_tierNeedsActiveRestart && gameObject.activeInHierarchy)
        {
            _tierNeedsActiveRestart = false;
            StartCoroutine(DelayedReplayTierEffect());
        }
    }

    private void LateUpdate()
    {
        if (_borderSR != null && borderImage != null && _borderSR.sprite != null)
            borderImage.sprite = _borderSR.sprite;
        if (_bgSR != null && bgImage != null && _bgSR.sprite != null)
            bgImage.sprite = _bgSR.sprite;
    }

    // Khởi tạo slot với loại trang bị
    public void Init(EquipmentSlotType type)
    {
        slotType = type;
        CacheIconBounds();
        NormalizeTierImageReferences();
        ApplyTierLayerOrder();
        ApplyTheme();
        UpdateSlotLabel();
        Clear();
    }

    // Cập nhật label của slot
    private void UpdateSlotLabel()
    {
        if (slotLabelText != null)
        {
            slotLabelText.text = PlayerEquipmentDto.GetSlotDisplayName(slotType);
        }
    }

    // Xóa item khỏi slot (hiển thị trống)
    public void Clear()
    {
        currentItem = null;
        _currentTierLevel = -1; // Invalidate cache
        _tierAppliedWhileActive = false;
        _tierNeedsActiveRestart = false;

        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            iconImage.preserveAspect = true;
        }

        if (placeholderImage != null)
        {
            placeholderImage.enabled = true;
        }

        if (itemNameText != null)
        {
            itemNameText.text = "";
        }

        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(false);

        // Ẩn viền + bg khi slot trống (không gọi ApplyTierEffect để tránh hiện trắng)
        HideTierImage(borderImage);
        HideTierImage(bgImage);
        // Dừng animator viền + bg khi slot trống
        if (_borderAnim != null) { _borderAnim.runtimeAnimatorController = null; _borderAnim.enabled = false; }
        if (_bgAnim     != null) { _bgAnim.runtimeAnimatorController = null;     _bgAnim.enabled    = false; }
        _borderSR = null;
        _bgSR = null;
    }

    // Gán item vào slot
    public void SetItem(EquipmentItemDto item)
    {
        currentItem = item;

        if (item == null || item.itemTemplateId <= 0)
        {
            Clear();
            return;
        }

        // Hiển thị icon
        if (iconImage != null)
        {
            Sprite icon = null;
            if (IconDatabase.Instance != null && !string.IsNullOrEmpty(item.iconId))
            {
                icon = IconDatabase.Instance.GetIcon(item.iconId);
            }

            if (icon != null)
            {
                UIRuntimeAssetHelper.SetSpriteWithNativeFit(iconImage, icon, iconMaxSize);
            }
            else
            {
                iconImage.enabled = false;
                Debug.LogWarning($"[EquipmentSlotUI] Không tìm thấy icon: {item.iconId} cho slot {slotType}");
            }
        }

        // Ẩn placeholder khi có item
        if (placeholderImage != null)
        {
            placeholderImage.enabled = false;
        }

        // Hiển thị tên item
        if (itemNameText != null)
        {
            itemNameText.text = !string.IsNullOrEmpty(item.itemName) ? item.itemName : item.itemCode;
        }

        if (upgradeButton != null)
            upgradeButton.gameObject.SetActive(true);

        // Cập nhật viền + bg theo upgrade level
        ApplyTierEffect(item.upgradeLevel);

        Debug.Log($"[EquipmentSlotUI] Slot {slotType}: Đã gán {item.itemName} (id={item.itemTemplateId})");
    }

    // Lấy item đang trang bị
    public EquipmentItemDto GetCurrentItem()
    {
        return currentItem;
    }

    // Kiểm tra slot có item không
    public bool HasItem()
    {
        return currentItem != null && currentItem.itemTemplateId > 0;
    }

    // Gọi từ Button OnClick trên prefab
    public void OnClick()
    {
        Debug.Log($"[EquipmentSlotUI] Click slot {slotType}, hasItem={HasItem()}");
        OnSlotClicked?.Invoke(slotType, currentItem);
    }

    // Gọi từ nút "Nâng Cấp" – mở UpgradePanel cho item đang trang bị
    public void OnUpgradeClick()
    {
        if (!HasItem()) return;
        if (UpgradePanel.Instance == null)
        {
            Debug.LogWarning("[EquipmentSlotUI] UpgradePanel.Instance chưa được tạo!");
            return;
        }

        var bridge = FindObjectOfType<InventoryNetworkBridge>();
        var inventory = bridge != null ? bridge.CurrentInventory : null;

        // slotKey phải khớp với key server lưu trong DB (weapon/helmet/armor/pants/boots/accessory)
        string slotKey = slotType.ToString().ToLower();

        UpgradePanel.Instance.OpenForEquipped(currentItem, slotKey, inventory);
    }

    private void ApplyTierEffect(int level)
    {
        NormalizeTierImageReferences();
        ApplyTierLayerOrder();

        // Guard: thiếu config
        if (tierConfig == null)
        {
            HideTierImage(borderImage);
            HideTierImage(bgImage);
            Debug.LogWarning($"[TierEffect] {name} ({slotType}): tierConfig chưa gán trong Inspector!");
            return;
        }

        // Guard: thiếu image references
        if (borderImage == null)
            Debug.LogWarning($"[TierEffect] {name} ({slotType}): borderImage chưa gán!");
        if (bgImage == null)
            Debug.LogWarning($"[TierEffect] {name} ({slotType}): bgImage chưa gán!");

        if (borderImage == null && bgImage == null) return;

        // Cache chỉ hợp lệ khi tier đã được bind lúc slot active trong hierarchy.
        // Nếu controller được gán khi parent còn inactive, Animator cần rebind lại khi panel hiện.
        if (level == _currentTierLevel && _currentTierLevel >= 0 && _tierAppliedWhileActive && !_tierNeedsActiveRestart)
            return;
        _currentTierLevel = level;

        var tier = tierConfig.GetTier(level);
        if (tier == null) tier = tierConfig.defaultTier;
        if (tier == null)
        {
            HideTierImage(borderImage);
            HideTierImage(bgImage);
            Debug.LogWarning($"[TierEffect] {name} ({slotType}): không tìm thấy tier cho level={level} và defaultTier cũng null!");
            return;
        }

        Debug.Log($"[TierEffect] {name} ({slotType}): level={level} → tier.minLevel={tier.minLevel}, " +
                  $"borderSprite={(tier.borderSprite != null ? tier.borderSprite.name : "NULL")}, " +
                  $"bgSprite={(tier.bgSprite != null ? tier.bgSprite.name : "NULL")}, " +
                  $"borderAnimator={(tier.borderAnimator != null ? tier.borderAnimator.name : "NULL")}, " +
                  $"bgAnimator={(tier.bgAnimator != null ? tier.bgAnimator.name : "NULL")}");

        // Border
        if (borderImage != null)
        {
            bool hasBorderAnim   = tier.borderAnimator != null;
            bool hasBorderSprite = tier.borderSprite   != null;

            if (hasBorderAnim || hasBorderSprite)
            {
                borderImage.color = ResolveVisibleTierColor(tier.borderColor);
                borderImage.enabled = true;

                // Pre-set sprite để tránh 1-frame trống trước khi Animator chạy
                if (hasBorderSprite)
                    borderImage.sprite = tier.borderSprite;

                EnableTierAnimator(borderImage.gameObject, ref _borderAnim, tier.borderAnimator);
            }
            else
            {
                HideTierImage(borderImage);
                DisableTierAnimator(ref _borderAnim);
            }
        }

        // Background
        if (bgImage != null)
        {
            bool hasBgAnim   = tier.bgAnimator != null;
            bool hasBgSprite = tier.bgSprite   != null;

            if (hasBgAnim || hasBgSprite)
            {
                bgImage.color = ResolveVisibleTierColor(tier.bgColor);
                bgImage.enabled = true;

                if (hasBgSprite)
                    bgImage.sprite = tier.bgSprite;

                EnableTierAnimator(bgImage.gameObject, ref _bgAnim, tier.bgAnimator);
            }
            else
            {
                HideTierImage(bgImage);
                DisableTierAnimator(ref _bgAnim);
            }
        }

        bool hasTierAnimator = tier.borderAnimator != null || tier.bgAnimator != null;
        _tierAppliedWhileActive = !hasTierAnimator || AreTierAnimatorTargetsActive(tier);
        _tierNeedsActiveRestart = hasTierAnimator && !_tierAppliedWhileActive;

        if (_tierNeedsActiveRestart)
        {
            Debug.Log($"[TierEffect] {name} ({slotType}): đã gán Animator khi slot chưa activeInHierarchy; sẽ re-apply khi panel active.");
        }
    }

    private static Color ResolveVisibleTierColor(Color configuredColor)
    {
        return (configuredColor.a < 0.01f || configuredColor == Color.black)
            ? Color.white
            : configuredColor;
    }

    private static void HideTierImage(Image image)
    {
        if (image == null) return;

        image.enabled = false;
        image.sprite = null;
        image.color = WithAlpha(image.color, 0f);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private void ReplayTierEffectWhenActive()
    {
        if (currentItem == null || currentItem.itemTemplateId <= 0)
            return;
        if (!gameObject.activeInHierarchy)
            return;

        _currentTierLevel = -1;
        _tierAppliedWhileActive = false;
        _tierNeedsActiveRestart = false;
        ApplyTierEffect(currentItem.upgradeLevel);
    }

    public void ReplayPendingTierEffectIfActive()
    {
        if (!_tierNeedsActiveRestart)
            return;

        ReplayTierEffectWhenActive();
    }

    private bool AreTierAnimatorTargetsActive(EquipmentTierConfig.TierEntry tier)
    {
        if (tier == null)
            return false;

        bool borderReady = tier.borderAnimator == null ||
                           (borderImage != null && borderImage.gameObject.activeInHierarchy);
        bool bgReady = tier.bgAnimator == null ||
                       (bgImage != null && bgImage.gameObject.activeInHierarchy);

        return gameObject.activeInHierarchy && borderReady && bgReady;
    }

    private void EnableTierAnimator(GameObject target, ref Animator anim, RuntimeAnimatorController controller)
    {
        if (target == null)
            return;

        EnsureActiveInHierarchy(target);

        // Animation clips target SpriteRenderer (classID 212).
        // UI objects only have Image, so add a hidden SpriteRenderer for Animator binding.
        // LateUpdate syncs SpriteRenderer.sprite → Image.sprite each frame.
        var sr = target.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = target.AddComponent<SpriteRenderer>();
        sr.enabled = false;

        if (target == (borderImage != null ? borderImage.gameObject : null))
            _borderSR = sr;
        else if (target == (bgImage != null ? bgImage.gameObject : null))
            _bgSR = sr;

        if (anim == null || anim.gameObject != target)
        {
            anim = target.GetComponent<Animator>();
            if (anim == null)
                anim = target.AddComponent<Animator>();
        }

        if (controller != null)
        {
            anim.enabled = false;
            anim.runtimeAnimatorController = controller;
            anim.updateMode = AnimatorUpdateMode.UnscaledTime;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            anim.enabled = true;

            if (target.activeInHierarchy)
            {
                anim.Rebind();
                anim.Update(0f);
            }

            Debug.Log($"[TierEffect] {name} ({slotType}): Animator target={target.name}, " +
                      $"controller={controller.name}, clips={controller.animationClips?.Length ?? 0}, " +
                      $"activeSelf={target.activeSelf}, activeInHierarchy={target.activeInHierarchy}, enabled={anim.enabled}");
        }
        else
        {
            if (anim != null)
            {
                anim.runtimeAnimatorController = null;
                anim.enabled = false;
            }
        }
    }

    private void EnsureActiveInHierarchy(GameObject target)
    {
        if (target == null || target.activeInHierarchy) return;

        var stack = new System.Collections.Generic.Stack<Transform>();
        Transform t = target.transform;
        while (t != null && t != this.transform)
        {
            if (!t.gameObject.activeSelf)
                stack.Push(t);
            t = t.parent;
        }
        while (stack.Count > 0)
            stack.Pop().gameObject.SetActive(true);

        if (!target.activeSelf)
            target.SetActive(true);
    }

    private void DisableTierAnimator(ref Animator anim)
    {
        if (anim != null)
        {
            anim.runtimeAnimatorController = null;
            anim.enabled = false;
        }
    }

    private void ApplyTierLayerOrder()
    {
        if (bgImage != null)
            bgImage.transform.SetAsFirstSibling();

        if (iconImage != null)
            iconImage.transform.SetAsLastSibling();

        if (borderImage != null)
            borderImage.transform.SetAsLastSibling();

        if (slotLabelText != null)
            slotLabelText.transform.SetAsLastSibling();

        if (itemNameText != null)
            itemNameText.transform.SetAsLastSibling();

        if (upgradeButton != null)
            upgradeButton.transform.SetAsLastSibling();
    }

    private void NormalizeTierImageReferences()
    {
        if (borderImage != null && bgImage != null)
        {
            bool borderLooksLikeBg = ImageNameMatches(borderImage, "BG", "Background");
            bool bgLooksLikeBorder = ImageNameMatches(bgImage, "Vien", "Border", "Frame");

            if (borderLooksLikeBg && bgLooksLikeBorder)
            {
                Image tmp = borderImage;
                borderImage = bgImage;
                bgImage = tmp;
                Debug.LogWarning($"[TierEffect] {name} ({slotType}): borderImage/bgImage đang gán ngược, đã tự swap theo tên BG/Vien.");
            }
        }

        if (borderImage == null)
        {
            Image foundBorder = FindTierImageByName("Vien", "Border", "Frame");
            if (foundBorder != null)
                borderImage = foundBorder;
        }

        if (bgImage == null)
        {
            Image foundBg = FindTierImageByName("BG", "Background");
            if (foundBg != null)
                bgImage = foundBg;
        }

        if (borderImage != null && bgImage != null && borderImage == bgImage)
        {
            Image foundBorder = FindTierImageByName("Vien", "Border", "Frame");
            Image foundBg = FindTierImageByName("BG", "Background");

            if (foundBorder != null)
                borderImage = foundBorder;
            if (foundBg != null && foundBg != borderImage)
                bgImage = foundBg;
        }
    }

    private Image FindTierImageByName(params string[] names)
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null || image == iconImage || image == placeholderImage)
                continue;

            foreach (string nameToken in names)
            {
                if (string.Equals(image.gameObject.name, nameToken, StringComparison.OrdinalIgnoreCase))
                    return image;
            }
        }

        foreach (Image image in images)
        {
            if (image == null || image == iconImage || image == placeholderImage)
                continue;

            if (ImageNameMatches(image, names))
                return image;
        }

        return null;
    }

    private static bool ImageNameMatches(Image image, params string[] names)
    {
        if (image == null) return false;

        string objectName = image.gameObject.name;
        foreach (string nameToken in names)
        {
            if (objectName.IndexOf(nameToken, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    // Debug trong Editor: Right-click component → "Debug Tier State"
    [ContextMenu("Debug Tier State")]
    private void DebugTierState()
    {
        Debug.Log($"---- [TierDebug] {name} ({slotType}) ----");
        Debug.Log($"  tierConfig    : {(tierConfig != null ? tierConfig.name : "NULL ← chưa gán!")}");
        Debug.Log($"  borderImage   : {(borderImage != null ? borderImage.name : "NULL ← chưa gán!")}");
        Debug.Log($"  bgImage       : {(bgImage != null ? bgImage.name : "NULL ← chưa gán!")}");
        Debug.Log($"  borderAnimator: {DescribeAnimator(borderImage, _borderAnim)}");
        Debug.Log($"  bgAnimator    : {DescribeAnimator(bgImage, _bgAnim)}");
        Debug.Log($"  currentItem   : {(currentItem != null ? $"{currentItem.itemName} level={currentItem.upgradeLevel}" : "null (trống)")}");
        Debug.Log($"  _currentTierLevel: {_currentTierLevel}");

        if (tierConfig != null)
        {
            int level = currentItem?.upgradeLevel ?? 0;
            var tier = tierConfig.GetTier(level);
            if (tier == null) tier = tierConfig.defaultTier;
            if (tier != null)
            {
                Debug.Log($"  Tier sẽ dùng  : minLevel={tier.minLevel}, " +
                          $"border={(tier.borderSprite != null ? tier.borderSprite.name : "NULL")}, " +
                          $"bg={(tier.bgSprite != null ? tier.bgSprite.name : "NULL")}, " +
                          $"borderAnim={(tier.borderAnimator != null ? tier.borderAnimator.name : "NULL")}, " +
                          $"bgAnim={(tier.bgAnimator != null ? tier.bgAnimator.name : "NULL")}");
            }
            else
            {
                Debug.Log($"  Tier sẽ dùng  : NULL (defaultTier cũng chưa set)");
            }
        }
        Debug.Log($"------------------------------------------");
    }

    private static string DescribeAnimator(Image image, Animator cachedAnimator)
    {
        if (image == null)
            return "no image";

        Animator actualAnimator = image.GetComponent<Animator>();
        Animator animator = cachedAnimator != null ? cachedAnimator : actualAnimator;
        if (animator == null)
            return "no Animator component";

        string controllerName = animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.name
            : "NULL controller";

        int clipCount = animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips != null
            ? animator.runtimeAnimatorController.animationClips.Length
            : 0;

        return $"component={(actualAnimator != null ? "yes" : "no")}, enabled={animator.enabled}, controller={controllerName}, clips={clipCount}";
    }

    private void ApplyTheme()
    {
        UIRuntimeAssetHelper.ApplyNotoSans(slotLabelText, itemNameText);
    }

    private void CacheIconBounds()
    {
        if (iconImage == null)
        {
            iconMaxSize = fallbackIconMaxSize;
            return;
        }

        iconImage.preserveAspect = true;

        Vector2 rectSize = iconImage.rectTransform.rect.size;
        if (rectSize.x <= 0f || rectSize.y <= 0f)
        {
            rectSize = iconImage.rectTransform.sizeDelta;
        }

        if (rectSize.x <= 0f || rectSize.y <= 0f)
        {
            rectSize = fallbackIconMaxSize;
        }

        iconMaxSize = new Vector2(
            Mathf.Max(0f, rectSize.x - iconPadding.x),
            Mathf.Max(0f, rectSize.y - iconPadding.y));

        if (iconMaxSize.x <= 0f || iconMaxSize.y <= 0f)
        {
            iconMaxSize = fallbackIconMaxSize;
        }
    }
}
