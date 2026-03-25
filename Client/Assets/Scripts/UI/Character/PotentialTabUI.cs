using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PotentialTabUI – Tab "Tiềm Năng" trong CharacterPanel.
///
/// Luồng hoạt động:
///   1. Load() gọi server lấy dữ liệu, tạo tất cả các dòng stat.
///   2. Người chơi nhấn +/-/▲ trên từng dòng → pending delta thay đổi, txtPotentialPoints cập nhật.
///   3. Nhấn "Hủy"  → reset toàn bộ pending về 0, khôi phục điểm gốc.
///   4. Nhấn "Cộng" → gom tất cả delta gửi lên server, server validate → DB → gửi lại → Load().
///
/// Cấu trúc GameObject gợi ý:
/// ┌─ ContentPotential
/// │   ├─ SubTabBar  (optional)
/// │   ├─ ScrollView → Viewport/Content  ← statListContainer
/// │   ├─ TxtPotentialPoints  [TMP_Text]
/// │   ├─ TxtStatus           [TMP_Text]
/// │   ├─ BtnHuy              [Button]   "Hủy"
/// │   └─ BtnCong             [Button]   "Cộng"
/// </summary>
public class PotentialTabUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text hiển thị số điểm tiềm năng còn dư")]
    [SerializeField] private TMP_Text txtPotentialPoints;

    [Tooltip("Transform chứa các dòng chỉ số tiềm năng (VerticalLayoutGroup)")]
    [SerializeField] private Transform statListContainer;

    [Tooltip("Prefab 1 dòng chỉ số tiềm năng (phải có PotentialStatRowUI)")]
    [SerializeField] private PotentialStatRowUI potentialRowPrefab;

    [Tooltip("Text trạng thái (loading, lỗi…)")]
    [SerializeField] private TMP_Text txtStatus;

    [Tooltip("Nút Hủy – hủy toàn bộ thay đổi pending")]
    [SerializeField] private Button btnHuy;

    [Tooltip("Nút Cộng – xác nhận gửi điểm lên server")]
    [SerializeField] private Button btnCong;


    // ── Internal ───────────────────────────────────────────
    private int _playerId                = -1;
    private int _originalAvailablePoints;
    private int _pendingAvailablePoints;
    private int _loadGen                 = 0; // tăng mỗi lần Load() để bỏ qua response cũ

    private readonly List<PotentialStatRowUI> _allRows = new List<PotentialStatRowUI>();

    // ───────────────────────────────────────────────────────
    private void Awake()
    {
        btnHuy?.onClick.AddListener(OnClickHuy);
        btnCong?.onClick.AddListener(OnClickCong);
    }

    private void OnDestroy()
    {
        btnHuy?.onClick.RemoveAllListeners();
        btnCong?.onClick.RemoveAllListeners();
    }

    // ───────────────────────────────────────────────────────
    #region Public API

    public void SetPlayerId(int id) => _playerId = id;

    /// <summary>Load dữ liệu tiềm năng từ server và render toàn bộ các dòng.</summary>
    public void Load()
    {
        if (_playerId <= 0)            { SetStatus("Chưa có playerId."); return; }
        if (APIClient.Instance == null){ SetStatus("APIClient không tồn tại."); return; }

        SetStatus("Đang tải tiềm năng...");
        ClearAllRows();
        SetActionButtonsEnabled(false);

        int requestGen = ++_loadGen; // snapshot thế hệ hiện tại

        APIClient.Instance.GetPlayerPotential(
            _playerId,
            onSuccess: response =>
            {
                if (requestGen != _loadGen) return; // response cũ, bỏ qua
                if (response == null) { SetStatus("Lỗi: phản hồi null."); return; }

                _originalAvailablePoints = response.potential_points_available;
                _pendingAvailablePoints  = _originalAvailablePoints;

                BuildAllRows(response);
                UpdatePointsLabel();
                SetStatus("");
                SetActionButtonsEnabled(true);
            },
            onError: err =>
            {
                if (requestGen != _loadGen) return;
                Debug.LogError($"[PotentialTabUI] Load error: {err}");
                SetStatus($"Lỗi: {err}");
            }
        );
    }

    #endregion

    // ───────────────────────────────────────────────────────
    #region Button handlers

    /// <summary>Hủy: hoàn tác toàn bộ pending, khôi phục điểm gốc.</summary>
    private void OnClickHuy()
    {
        foreach (var row in _allRows)
            row.ResetPending();

        _pendingAvailablePoints = _originalAvailablePoints;
        UpdatePointsLabel();
        RefreshAllRowButtonStates();
    }

    /// <summary>Cộng: gom tất cả delta, gửi server validate → DB → reload UI.</summary>
    private void OnClickCong()
    {
        if (APIClient.Instance == null) return;

        var allocations = new List<PotentialAllocationEntry>();
        foreach (var row in _allRows)
        {
            if (row.PendingDelta > 0)
                allocations.Add(new PotentialAllocationEntry
                {
                    stat_name = row.StatName,
                    points    = row.PendingDelta
                });
        }

        if (allocations.Count == 0) return;

        SetActionButtonsEnabled(false);

        APIClient.Instance.AllocatePotentialStats(
            _playerId,
            allocations,
            onSuccess: _ => Load(),
            onError: err =>
            {
                Debug.LogError($"[PotentialTabUI] Cộng tiềm năng lỗi: {err}");
                SetStatus($"Lỗi: {err}");
                SetActionButtonsEnabled(true);
            }
        );
    }

    #endregion

    // ───────────────────────────────────────────────────────
    #region Row callbacks

    /// <summary>
    /// Callback từ hàng khi người chơi nhấn nút:
    ///   delta âm = dùng điểm, delta dương = trả điểm.
    /// </summary>
    private void OnRowPointsChanged(int delta)
    {
        _pendingAvailablePoints += delta;
        UpdatePointsLabel();
        RefreshAllRowButtonStates();
    }

    #endregion

    // ───────────────────────────────────────────────────────
    #region Private helpers

    private void BuildAllRows(PlayerPotentialResponse response)
    {
        if (potentialRowPrefab == null || statListContainer == null)
        {
            Debug.LogError("[PotentialTabUI] potentialRowPrefab hoặc statListContainer == NULL!");
            return;
        }
        if (response.stats == null || response.stats.Length == 0)
        {
            SetStatus("Không có chỉ số tiềm năng.");
            return;
        }

        foreach (var stat in response.stats)
        {
            var row = Instantiate(potentialRowPrefab, statListContainer);
            row.SetData(stat, () => _pendingAvailablePoints, OnRowPointsChanged);
            _allRows.Add(row);
        }
    }

    private void RefreshAllRowButtonStates()
    {
        foreach (var row in _allRows)
            row.RefreshButtonStates();
    }

    private void UpdatePointsLabel()
    {
        if (txtPotentialPoints != null)
            txtPotentialPoints.text = $"Điểm còn: <b>{_pendingAvailablePoints}</b> điểm";
    }

    private void SetActionButtonsEnabled(bool enabled)
    {
        if (btnHuy  != null) btnHuy.interactable  = enabled;
        if (btnCong != null) btnCong.interactable = enabled;
    }

    private void ClearAllRows()
    {
        foreach (var row in _allRows)
            if (row != null) Destroy(row.gameObject);
        _allRows.Clear();

        if (statListContainer != null)
            for (int i = statListContainer.childCount - 1; i >= 0; i--)
                Destroy(statListContainer.GetChild(i).gameObject);
    }

    private void SetStatus(string msg)
    {
        if (txtStatus != null)
        {
            txtStatus.text    = msg;
            txtStatus.enabled = !string.IsNullOrEmpty(msg);
        }
    }

    #endregion
}
