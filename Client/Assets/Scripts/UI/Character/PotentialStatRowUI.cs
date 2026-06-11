using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PotentialStatRowUI – Một dòng hiển thị 1 chỉ số tiềm năng.
// Cấu trúc GameObject (HorizontalLayoutGroup trên root):
// ┌─ PotentialStatRow   [Image bg + HLG]
// ├─ TxtStatName    [TMP_Text] – "Tấn Công:"
// ├─ TxtPoints      [TMP_Text] – "10"  (hiển thị giá trị pending)
// ├─ BtnMinus       [Button]   – "-"
// ├─ BtnPlus        [Button]   – "+"
// └─ BtnMax         [Button]   – "▲"  (tăng max bằng điểm còn lại)
// Không gọi API trực tiếp – mọi thay đổi là pending cho đến khi
// PotentialTabUI gửi lên server qua nút "Cộng".
public class PotentialStatRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text txtStatName;
    [SerializeField] private TMP_Text txtPoints;
    [SerializeField] private Button   btnMinus;
    [SerializeField] private Button   btnPlus;
    [SerializeField] private Button   btnMax;

    // Internal state
    private PotentialStatInfo _info;
    private int               _pendingDelta;          // điểm đã cộng/trừ (chưa gửi server)
    private Func<int>         _getAvailablePoints;    // hỏi parent số điểm còn
    private Action<int>       _onPointsChanged;       // báo parent: âm = dùng, dương = trả
    private bool              _isReadOnlyView;

    // Hàm public để script hoặc hệ thống khác gọi vào.
    public string StatName    => _info?.stat_name;
    public int    PendingDelta => _pendingDelta;

    // Khởi tạo dữ liệu dòng. Không gọi API; thay đổi chỉ là pending.
    public void SetData(PotentialStatInfo info,
                        Func<int>   getAvailablePoints,
                        Action<int> onPointsChanged)
    {
        _info               = info;
        _getAvailablePoints = getAvailablePoints;
        _onPointsChanged    = onPointsChanged;
        _pendingDelta       = 0;
        _isReadOnlyView     = false;

        RefreshUI();

        btnMinus?.onClick.RemoveAllListeners();
        btnPlus?.onClick.RemoveAllListeners();
        btnMax?.onClick.RemoveAllListeners();

        btnMinus?.onClick.AddListener(OnClickMinus);
        btnPlus?.onClick.AddListener(OnClickPlus);
        btnMax?.onClick.AddListener(OnClickMax);
    }

    public void SetReadOnlyData(PotentialStatInfo info)
    {
        _info = info;
        _getAvailablePoints = null;
        _onPointsChanged = null;
        _pendingDelta = 0;
        _isReadOnlyView = true;

        btnMinus?.onClick.RemoveAllListeners();
        btnPlus?.onClick.RemoveAllListeners();
        btnMax?.onClick.RemoveAllListeners();

        RefreshUI();
    }

    // Hủy mọi thay đổi pending về 0, cập nhật UI.
    public void ResetPending()
    {
        _pendingDelta = 0;
        RefreshUI();
    }

    // Gọi khi parent thay đổi điểm còn để cập nhật trạng thái nút.
    public void RefreshButtonStates() => UpdateButtonStates();

    // Private helpers

    private void RefreshUI()
    {
        if (_info == null) return;

        if (txtStatName != null)
            txtStatName.text = CleanDisplayName(_info.display_name);

        if (txtPoints != null)
            txtPoints.text = (_info.current_points + _pendingDelta).ToString();

        SetButtonsVisible(!_isReadOnlyView);
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        if (_isReadOnlyView)
        {
            if (btnPlus != null) btnPlus.interactable = false;
            if (btnMax != null) btnMax.interactable = false;
            if (btnMinus != null) btnMinus.interactable = false;
            return;
        }

        int available = _getAvailablePoints?.Invoke() ?? 0;

        if (btnPlus  != null) btnPlus.interactable  = available > 0;
        if (btnMax   != null) btnMax.interactable   = available > 0;
        // Chỉ cho giảm những điểm đã cộng trong phiên này
        if (btnMinus != null) btnMinus.interactable = _pendingDelta > 0;
    }

    private void SetButtonsVisible(bool visible)
    {
        if (btnMinus != null) btnMinus.gameObject.SetActive(visible);
        if (btnPlus != null) btnPlus.gameObject.SetActive(visible);
        if (btnMax != null) btnMax.gameObject.SetActive(visible);
    }

    private void OnClickPlus()
    {
        if ((_getAvailablePoints?.Invoke() ?? 0) <= 0) return;
        _pendingDelta++;
        _onPointsChanged?.Invoke(-1);   // dùng 1 điểm
        RefreshUI();
    }

    private void OnClickMinus()
    {
        if (_pendingDelta <= 0) return;
        _pendingDelta--;
        _onPointsChanged?.Invoke(+1);   // trả lại 1 điểm
        RefreshUI();
    }

    private void OnClickMax()
    {
        int available = _getAvailablePoints?.Invoke() ?? 0;
        if (available <= 0) return;
        _pendingDelta += available;
        _onPointsChanged?.Invoke(-available);   // dùng hết điểm còn
        RefreshUI();
    }

    // Bỏ phần trong ngoặc đơn khỏi tên hiển thị và thêm ":".
    // Ví dụ: "Máu (HP)" → "Máu:", "Tấn Công" → "Tấn Công:"
    private static string CleanDisplayName(string name)
    {
        if (string.IsNullOrEmpty(name)) return ":";
        int paren = name.IndexOf('(');
        string clean = paren >= 0 ? name.Substring(0, paren).TrimEnd() : name.TrimEnd();
        return clean + ":";
    }
}
