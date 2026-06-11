using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Một hàng trong NpcDynamicMenuUI — icon chat bubble + label text + nút bấm.
// Inspector setup (trên prefab NpcMenuItemRow):
// labelText — TMP_Text hiển thị tên mục menu
// iconImage — Image chat bubble (optional)
// btn       — Button (có thể là component trên root GO)
// Prefab layout (HorizontalLayoutGroup trên root):
// [Icon 28×28] | [Label (flex)]
[RequireComponent(typeof(Button))]
public class NpcMenuItemRow : MonoBehaviour
{
    private const string LogPrefix = "[NpcMenuItemRow]";

    [Header("UI References")]
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image    iconImage;

    private Button _button;
    private Action _onClick;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null) _button = GetComponentInChildren<Button>(true);

        if (labelText == null) labelText = GetComponentInChildren<TMP_Text>(true);
        if (iconImage == null)
        {
            var images = GetComponentsInChildren<Image>(true);
            if (images.Length > 1) iconImage = images[1]; // root bg = [0], icon = [1]
        }

        // Diagnostics: check all raycasting components on this row
        var mask    = GetComponentInParent<Mask>();
        var cg      = GetComponentInParent<CanvasGroup>();
        var canvas  = GetComponentInParent<Canvas>();
        var raycaster = canvas != null ? canvas.GetComponent<GraphicRaycaster>() : null;
        { /* {LogPrefix} Awake '{gameObject.name}' | */ }
    }

    // Khởi tạo hàng menu với nhãn và callback khi bấm.
    public void Init(string label, Action onClick)
    {
        _onClick = onClick;

        // Awake có thể chưa chạy nếu parent đang inactive khi Instantiate → tự fetch lại
        if (_button == null)
        {
            _button = GetComponent<Button>();
            if (_button == null) _button = GetComponentInChildren<Button>(true);
        }
        if (labelText == null) labelText = GetComponentInChildren<TMP_Text>(true);

        if (labelText != null)
        {
            labelText.text = label;
            UIRuntimeAssetHelper.ApplyNotoSans(new[] { labelText });
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(HandleClick);
            { /* {LogPrefix} Init '{label}' | button OK interactable={_button.interactable} enabled={_button.enabled} go.active={gameObject.activeSelf} */ }
        }
        else
        {
            { /* Lỗi: {LogPrefix} Init '{label}' | _button is NULL  click sẽ không hoạt động */ }
        }
    }

    private void HandleClick()
    {
        { /* {LogPrefix} HandleClick '{(labelText != null ? labelText.text */ }
        _onClick?.Invoke();
    }
}
