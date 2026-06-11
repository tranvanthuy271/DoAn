using UnityEngine;
using UnityEngine.UI;

// GeneUpgradePanelToggleButton — Nút mở/đóng GeneUpgradePanel.
// Setup:
// 1. Gắn script này lên Button trong UI (ví dụ: nút "Gene" trên CharacterPanel).
// 2. Kéo GeneUpgradePanel (GameObject) vào slot genePanel.
[RequireComponent(typeof(Button))]
public class GeneUpgradePanelToggleButton : MonoBehaviour
{
    [Tooltip("Tham chiếu tới GeneUpgradePanel (GameObject chứa component GeneUpgradePanel)")]
    [SerializeField] private GeneUpgradePanel genePanel;

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
        if (genePanel == null)
        {
            Debug.LogWarning("[GeneUpgradePanelToggleButton] Chưa gán GeneUpgradePanel vào Inspector.");
            return;
        }
        genePanel.Open();
    }
}
