using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// PotentialTabUI – Tab "Tiềm Năng" trong CharacterPanel.
// Luồng hoạt động:
// 1. Load() gọi server lấy dữ liệu, tạo tất cả các dòng stat.
// 2. Người chơi nhấn +/-/▲ trên từng dòng → pending delta thay đổi, txtPotentialPoints cập nhật.
// 3. Nhấn "Hủy"  → reset toàn bộ pending về 0, khôi phục điểm gốc.
// 4. Nhấn "Cộng" → gom tất cả delta gửi lên server, server validate → DB → gửi lại → Load().
// Cấu trúc GameObject gợi ý:
// ┌─ ContentPotential
// ├─ SubTabBar  (optional)
// ├─ ScrollView → Viewport/Content  ← statListContainer
// ├─ TxtPotentialPoints  [TMP_Text]
// ├─ TxtStatus           [TMP_Text]
// ├─ BtnHuy              [Button]   "Hủy"
// └─ BtnCong             [Button]   "Cộng"
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


    // Xử lý nội bộ phục vụ các hàm public.
    private int _playerId                = -1;
    private int _originalAvailablePoints;
    private int _pendingAvailablePoints;
    private int _loadGen                 = 0; // tăng mỗi lần Load() để bỏ qua response cũ
    private bool _isExternalProfileView;
    private PotentialStatInfo[] _externalStats;
    private string _externalCharacterName;
    private ScrollRect _scrollRect;

    private readonly List<PotentialStatRowUI> _allRows = new List<PotentialStatRowUI>();

    private void Awake()
    {
        btnHuy?.onClick.AddListener(OnClickHuy);
        btnCong?.onClick.AddListener(OnClickCong);
        EnsureScrollLayout();
    }

    private void OnDestroy()
    {
        btnHuy?.onClick.RemoveAllListeners();
        btnCong?.onClick.RemoveAllListeners();
    }

    #region Public API

    public void SetPlayerId(int id) => _playerId = id;

    public void ShowFriendPotential(PotentialStatInfo[] stats, string characterName)
    {
        _isExternalProfileView = true;
        _externalStats = stats;
        _externalCharacterName = characterName;
        { /* ShowFriendPotential characterName='{characterName}' stats={stats?.Length ?? 0} */ }
        Load();
    }

    public void ClearFriendPotential()
    {
        if (!_isExternalProfileView && _externalStats == null)
            return;

        { /* ClearFriendPotential() */ }
        _isExternalProfileView = false;
        _externalStats = null;
        _externalCharacterName = null;
        SetActionButtonsVisible(true);
    }

    // Load dữ liệu tiềm năng từ server và render toàn bộ các dòng.
    public void Load()
    {
        EnsureScrollLayout();

        if (_isExternalProfileView)
        {
            RenderFriendPotential();
            return;
        }

        if (_playerId <= 0)            { SetStatus("Chưa có playerId."); return; }
        if (GameplayCommandService.Instance == null) { SetStatus("Server chưa sẵn sàng."); return; }

        SetStatus("Đang tải tiềm năng...");
        ClearAllRows();
        SetActionButtonsVisible(true);
        SetActionButtonsEnabled(false);

        int requestGen = ++_loadGen; // snapshot thế hệ hiện tại

        GameplayCommandService.OnPotentialReceived -= HandlePotentialReceived;
        GameplayCommandService.OnPotentialReceived += HandlePotentialReceived;
        GameplayCommandService.Instance.GetPlayerPotentialServerRpc();
    }

    private void HandlePotentialReceived(string json)
    {
        GameplayCommandService.OnPotentialReceived -= HandlePotentialReceived;
        PlayerPotentialResponse response = null;
        try
        {
            if (json.Contains("\"error\"")) { SetStatus($"Lỗi: {json}"); SetActionButtonsEnabled(true); return; }
            response = JsonUtility.FromJson<PlayerPotentialResponse>(json);
        }
        catch (System.Exception ex) { SetStatus($"Lỗi: {ex.Message}"); return; }

        if (response == null) { SetStatus("Lỗi: phản hồi null."); return; }
        // Gọi lại Load nội bộ để populate (tái dùng code cũ)
        InternalPopulate(response);
    }

    private void InternalPopulate(PlayerPotentialResponse response)
    {
        _originalAvailablePoints = response.potential_points_available;
        _pendingAvailablePoints  = _originalAvailablePoints;
        BuildAllRows(response);
        RebuildScrollLayout(resetToTop: true);
        UpdatePointsLabel();
        SetStatus("");
        SetActionButtonsEnabled(true);
    }

    #endregion

    #region Button handlers

    // Hủy: hoàn tác toàn bộ pending, khôi phục điểm gốc.
    private void OnClickHuy()
    {
        foreach (var row in _allRows)
            row.ResetPending();

        _pendingAvailablePoints = _originalAvailablePoints;
        UpdatePointsLabel();
        RefreshAllRowButtonStates();
    }

    // Cộng: gom tất cả delta, gửi server validate → DB → reload UI.
    private void OnClickCong()
    {
        if (GameplayCommandService.Instance == null) return;

        var allocations = new System.Collections.Generic.List<PotentialAllocationEntry>();
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

        string json = JsonUtility.ToJson(new PotentialAllocationRequest { allocations = allocations.ToArray() });
        GameplayCommandService.OnPotentialAllocated -= HandlePotentialAllocated;
        GameplayCommandService.OnPotentialAllocated += HandlePotentialAllocated;
        GameplayCommandService.Instance.AllocatePotentialStatsServerRpc(json);
    }

    private void HandlePotentialAllocated(string json)
    {
        GameplayCommandService.OnPotentialAllocated -= HandlePotentialAllocated;
        if (json.Contains("\"error\""))
        {
            { /* Lỗi: Cộng tiềm năng lỗi: {json} */ }
            SetStatus($"Lỗi: {json}");
            SetActionButtonsEnabled(true);
            return;
        }
        Load();
    }

    #endregion

    #region Row callbacks

    // Callback từ hàng khi người chơi nhấn nút:
    // delta âm = dùng điểm, delta dương = trả điểm.
    private void OnRowPointsChanged(int delta)
    {
        _pendingAvailablePoints += delta;
        UpdatePointsLabel();
        RefreshAllRowButtonStates();
    }

    #endregion

    #region Private helpers

    private void BuildAllRows(PlayerPotentialResponse response)
    {
        EnsureScrollLayout();

        if (potentialRowPrefab == null || statListContainer == null)
        {
            { /* Lỗi: potentialRowPrefab hoặc statListContainer == NULL */ }
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

    private void SetActionButtonsVisible(bool visible)
    {
        if (btnHuy != null) btnHuy.gameObject.SetActive(visible);
        if (btnCong != null) btnCong.gameObject.SetActive(visible);
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

    private void EnsureScrollLayout()
    {
        if (statListContainer == null)
        {
            ScrollRect existingScroll = GetComponentInChildren<ScrollRect>(true);
            if (existingScroll != null && existingScroll.content != null)
                statListContainer = existingScroll.content;
        }

        if (statListContainer == null)
            return;

        RectTransform contentRect = statListContainer as RectTransform;
        if (contentRect == null)
            return;

        _scrollRect = statListContainer.GetComponentInParent<ScrollRect>(true);
        if (_scrollRect != null)
        {
            if (_scrollRect.content == null)
                _scrollRect.content = contentRect;

            if (_scrollRect.viewport == null)
            {
                Mask viewportMask = _scrollRect.GetComponentInChildren<Mask>(true);
                if (viewportMask != null)
                    _scrollRect.viewport = viewportMask.transform as RectTransform;
            }
        }
    }

    private void RebuildScrollLayout(bool resetToTop)
    {
        EnsureScrollLayout();

        RectTransform contentRect = statListContainer as RectTransform;
        if (contentRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        Canvas.ForceUpdateCanvases();

        if (_scrollRect != null)
        {
            _scrollRect.StopMovement();
            if (resetToTop)
                _scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void SetStatus(string msg)
    {
        if (txtStatus != null)
        {
            txtStatus.text    = msg;
            txtStatus.enabled = !string.IsNullOrEmpty(msg);
        }
    }

    private void RenderFriendPotential()
    {
        ClearAllRows();
        SetActionButtonsVisible(false);
        SetActionButtonsEnabled(false);
        SetStatus("");

        if (txtPotentialPoints != null)
            txtPotentialPoints.text = string.IsNullOrWhiteSpace(_externalCharacterName)
                ? "Tiềm năng"
                : $"Tiềm năng của {_externalCharacterName}";

        if (_externalStats == null || _externalStats.Length == 0)
        {
            SetStatus("Người chơi này chưa có dữ liệu tiềm năng.");
            return;
        }

        if (potentialRowPrefab == null || statListContainer == null)
        {
            { /* Lỗi: potentialRowPrefab hoặc statListContainer == NULL khi render friend profile */ }
            return;
        }

        foreach (var stat in _externalStats)
        {
            var row = Instantiate(potentialRowPrefab, statListContainer);
            row.SetReadOnlyData(stat);
            _allRows.Add(row);
        }

        RebuildScrollLayout(resetToTop: true);
    }

    #endregion
}
