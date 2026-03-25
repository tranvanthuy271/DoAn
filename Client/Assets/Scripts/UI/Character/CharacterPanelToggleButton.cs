using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CharacterPanelToggleButton – Nút mở/đóng toàn bộ panel nhân vật + túi.
///
/// Setup:
/// 1. Gắn script này lên Button trong UI (ví dụ: nút hình nhân vật/kiếm).
/// 2. Kéo InformationPanelController vào slot informationPanel (ưu tiên).
///    Nếu không dùng InformationPanelController, kéo CharacterPanelController vào slot characterPanel.
/// </summary>
[RequireComponent(typeof(Button))]
public class CharacterPanelToggleButton : MonoBehaviour
{
    [Tooltip("Tham chiếu tới InformationPanelController (quản lý cả ThongTin + TuiDo)")]
    [SerializeField] private InformationPanelController informationPanel;

    [Tooltip("Fallback: dùng khi không có InformationPanelController")]
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
        // Ưu tiên dùng InformationPanelController để đồng bộ state cả 2 tab
        if (informationPanel != null)
        {
            if (informationPanel.IsAnyPanelVisible)
                informationPanel.HideAll();
            else
                informationPanel.ShowPanel();
            return;
        }

        // Fallback khi chưa gán InformationPanelController
        if (characterPanel != null)
        {
            characterPanel.Toggle();
        }
        else
        {
            Debug.LogWarning("[CharacterPanelToggleButton] Chưa gán InformationPanelController hoặc CharacterPanelController.");
        }
    }
}

