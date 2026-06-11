using UnityEngine;
using UnityEngine.UI;

// HybridFusionPanelToggleButton — Nút mở HybridFusionPanel.
// INSPECTOR SETUP:
// 1. Gắn script này lên Button "HybridFusion" trong UI
// (ví dụ: nút ⚡ trên CharacterPanel / Gene sub-menu).
// 2. Kéo HybridFusionPanel (GameObject) vào slot hybridPanel.
[RequireComponent(typeof(Button))]
public class HybridFusionPanelToggleButton : MonoBehaviour
{
    [Tooltip("Tham chiếu tới HybridFusionPanel (GameObject chứa component HybridFusionPanel)")]
    [SerializeField] private HybridFusionPanel hybridPanel;

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
        if (hybridPanel == null)
        {
            { /* Cảnh báo: Chưa gán HybridFusionPanel vào Inspector */ }
            return;
        }
        hybridPanel.Open();
    }
}
