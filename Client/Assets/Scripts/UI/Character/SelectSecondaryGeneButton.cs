using UnityEngine;
using UnityEngine.UI;
using TMPro;

// SelectSecondaryGeneButton — Nút mở khoá hệ gene phụ.
// Click → mở SecondaryGeneSelectPanel để xem thông tin cặp gene và xác nhận.
// Sau khi xác nhận trong panel → hệ phụ được thêm vào DB.
// Button sẽ disabled khi đã có hệ phụ.
// INSPECTOR SETUP:
// 1. Gắn script lên Button "Mở Hệ Phụ"
// 2. (Tuỳ chọn) infoText → TMP_Text hiển thị trạng thái
[RequireComponent(typeof(Button))]
public class SelectSecondaryGeneButton : MonoBehaviour
{
    [Tooltip("Text hiển thị trạng thái (tuỳ chọn)")]
    [SerializeField] private TMP_Text infoText;

    [Tooltip("Kéo SecondaryGeneSelectPanel (GameObject trong Hierarchy) vào đây")]
    [SerializeField] private SecondaryGeneSelectPanel selectPanel;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnEnable() => RefreshDisplay();

    private void OnDestroy()
    {
        if (_button) _button.onClick.RemoveListener(OnClick);
    }

    private void RefreshDisplay()
    {
        var pd = GameManager.Instance?.GetPlayerData();
        if (pd == null) return;

        if (!string.IsNullOrEmpty(pd.secondary_element))
        {
            _button.interactable = false;
            SetInfo($"Đã mở khoá hệ phụ: {ElementHelper.ToVietnamese(pd.secondary_element)}", Color.green);
        }
        else
        {
            _button.interactable = true;
            string partner = ElementHelper.GetFixedSecondary(pd.element_type) ?? "";
            SetInfo(string.IsNullOrEmpty(partner)
                ? "Hệ chính không hỗ trợ Hybrid."
                : $"Mở khoá hệ phụ: {ElementHelper.ToVietnamese(partner)}", Color.white);
        }
    }

    private void OnClick()
    {
        var pd = GameManager.Instance?.GetPlayerData();
        if (pd == null) return;

        if (!string.IsNullOrEmpty(pd.secondary_element))
        {
            _button.interactable = false;
            return;
        }

        // Tìm và mở SecondaryGeneSelectPanel
        var panel = selectPanel
            ?? SecondaryGeneSelectPanel.Instance
            ?? FindObjectOfType<SecondaryGeneSelectPanel>(true);

        if (panel == null)
        {
            SetInfo("Không tìm thấy SecondaryGeneSelectPanel trong scene.", Color.red);
            return;
        }

        panel.Open();
    }

    private void SetInfo(string msg, Color color)
    {
        if (infoText == null) return;
        infoText.text  = msg;
        infoText.color = color;
    }
}
