using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// PotentialStatRowUI – Một dòng hiển thị thông tin 1 chỉ số tiềm năng.
///
/// Cấu trúc GameObject gợi ý:
/// ┌─ PotentialStatRow
/// │   ├─ TxtStatName    [TMP_Text] – "Tấn Công"
/// │   ├─ TxtPoints      [TMP_Text] – "3 điểm"
/// │   ├─ TxtValue       [TMP_Text] – "Tổng: +15"
/// │   └─ BtnUpgrade     [Button]  – nút "+"
///
/// Lưu ý: Prefab này được PotentialTabUI instantiate tự động.
/// </summary>
public class PotentialStatRowUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [SerializeField] private TMP_Text txtStatName;
    [SerializeField] private TMP_Text txtPoints;
    [SerializeField] private TMP_Text txtValue;
    [SerializeField] private Button   btnUpgrade;

    // ── Internal state ─────────────────────────────────────
    private PotentialStatInfo _info;
    private int               _playerId;
    private Action            _onUpgraded;       // callback để reload tab
    // ───────────────────────────────────────
    private void Awake()
    {
        // Tắt raycastTarget trên Image và TMP_Text không phải Button graphic
        // – tránh bị chặn click xuống BtnUpgrade
        foreach (var img in GetComponentsInChildren<Image>(includeInactive: true))
        {
            bool isButtonTarget = img.GetComponent<Button>() != null
                               || (img.transform.parent != null &&
                                   img.transform.parent.GetComponent<Button>() != null &&
                                   img.transform.parent.GetComponent<Button>().targetGraphic == img);
            if (!isButtonTarget)
                img.raycastTarget = false;
        }
        foreach (var tmp in GetComponentsInChildren<TMP_Text>(includeInactive: true))
            tmp.raycastTarget = false;
    }

    // IPointerClickHandler: debug – xác nhận click có tới row không
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[PotentialStatRowUI][PointerClick] Click tới row '{_info?.stat_name}'");
    }
    // ───────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Khởi tạo dòng chỉ số tiềm năng.
    /// </summary>
    public void SetData(PotentialStatInfo info, int playerId, int availablePoints, Action onUpgraded)
    {
        _info       = info;
        _playerId   = playerId;
        _onUpgraded = onUpgraded;

        RefreshUI(availablePoints);

        btnUpgrade?.onClick.RemoveAllListeners();
        btnUpgrade?.onClick.AddListener(OnClickUpgrade);
    }

    /// <summary>Cập nhật lại UI khi số điểm tiềm năng thay đổi (không reload từ API).</summary>
    public void UpdateAvailablePoints(int availablePoints)
    {
        if (btnUpgrade != null)
            btnUpgrade.interactable = availablePoints > 0;
    }

    #endregion

    // ───────────────────────────────────────────────────────
    #region Private helpers

    private void RefreshUI(int availablePoints)
    {
        if (_info == null) return;

        if (txtStatName != null)
            txtStatName.text = _info.display_name;

        if (txtPoints != null)
            txtPoints.text = $"{_info.current_points} điểm";

        if (txtValue != null)
        {
            string unit  = GetUnit(_info.stat_name);
            string total = FormatValue(_info.stat_name, _info.total_value);
            string perPt = FormatValue(_info.stat_name, _info.value_per_point);
            txtValue.text = $"Tổng: <b>+{total}{unit}</b>  (+{perPt}{unit}/điểm)";
        }

        if (btnUpgrade != null)
            btnUpgrade.interactable = availablePoints > 0;
    }

    private void OnClickUpgrade()
    {
        if (_info == null || APIClient.Instance == null) return;

        if (btnUpgrade != null) btnUpgrade.interactable = false;

        APIClient.Instance.UpgradePotentialStat(
            _playerId,
            _info.stat_name,
            onSuccess: _ =>
            {
                Debug.Log($"[PotentialStatRowUI] Đã tăng {_info.display_name}");
                _onUpgraded?.Invoke();
            },
            onError: err =>
            {
                Debug.LogError($"[PotentialStatRowUI] Lỗi tăng tiềm năng: {err}");
                if (btnUpgrade != null) btnUpgrade.interactable = true;
            }
        );
    }

    /// <summary>Đơn vị hiển thị theo loại stat.</summary>
    private static string GetUnit(string statName) => statName switch
    {
        "attack"  => "",
        "defense" => "",
        "hp"      => " HP",
        "mp"      => " MP",
        "gene"    => "",
        _         => ""
    };

    private static string FormatValue(string statName, float val) =>
        (statName == "hp" || statName == "mp")
            ? val.ToString("F0")
            : val.ToString("F0");

    #endregion
}
