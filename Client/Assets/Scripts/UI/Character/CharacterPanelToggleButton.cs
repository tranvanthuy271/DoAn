using UnityEngine;
using UnityEngine.UI;

// CharacterPanelToggleButton – Nút mở/đóng toàn bộ panel nhân vật + túi.
// Setup:
// 1. Gắn script này lên Button trong UI (ví dụ: nút hình nhân vật/kiếm).
// 2. Kéo InformationPanelController vào slot informationPanel (ưu tiên).
// Nếu không dùng InformationPanelController, kéo CharacterPanelController vào slot characterPanel.
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

    private void Start()
    {
        ResolveControllers();
        if (informationPanel == null && characterPanel == null)
            { /* Lỗi: Không tìm thấy InformationPanelController hay CharacterPanelController trong scene! Hãy gán thủ công trong Inspector */ }
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        ResolveControllers();
        { /* Button clicked */ }
        
        // Ưu tiên dùng InformationPanelController để đồng bộ state cả 2 tab
        if (informationPanel != null)
        {
            { /* Sử dụng InformationPanelController */ }

            if (informationPanel.IsAnyPanelVisible && !informationPanel.IsShowingInventory)
            {
                { /* Panel đang hiện → đóng */ }
                informationPanel.HideAll();
            }
            else
            {
                { /* Panel đang ẩn → mở CharacterPanel tab Thông Tin */ }
                informationPanel.ShowThongTin();
            }
            return;
        }

        // Fallback khi chưa gán InformationPanelController
        { /* Cảnh báo: Không tìm thấy InformationPanelController, dùng fallback */ }
        
        if (characterPanel != null)
        {
            { /* Toggle CharacterPanel trực tiếp */ }
            characterPanel.Toggle();
        }
        else
        {
            { /* Lỗi: Chưa gán InformationPanelController hoặc CharacterPanelController */ }
        }
    }

    private void ResolveControllers()
    {
        if (characterPanel == null)
        {
            characterPanel = FindObjectOfType<CharacterPanelController>(includeInactive: true);
            if (characterPanel != null)
                { /* Auto-found CharacterPanelController: {characterPanel.gameObject.name} */ }
        }

        if (informationPanel == null)
        {
            informationPanel = InformationPanelController.GetOrCreate(characterPanel, null);
            if (informationPanel != null)
                { /* Auto-found/created InformationPanelController: {informationPanel.gameObject.name} */ }
        }
    }
}

