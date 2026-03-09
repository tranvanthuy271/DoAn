using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CharacterPanelToggleButton – Nút mở/đóng CharacterPanel (3 tab).
///
/// Setup:
/// 1. Gắn script này lên Button trong UI (ví dụ: nút hình nhân vật/kiếm).
/// 2. Kéo CharacterPanelController vào slot characterPanel.
/// </summary>
[RequireComponent(typeof(Button))]
public class CharacterPanelToggleButton : MonoBehaviour
{
    [Tooltip("Tham chiếu tới CharacterPanelController trong scene")]
    [SerializeField] private CharacterPanelController characterPanel;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
            _button.onClick.AddListener(OnButtonClicked);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        if (characterPanel == null)
        {
            Debug.LogWarning("[CharacterPanelToggleButton] Chưa gán CharacterPanelController vào Inspector.");
            return;
        }
        characterPanel.Toggle();
    }
}
