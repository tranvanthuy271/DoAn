using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PotentialTabUI – Tab "Tiềm Năng" trong CharacterPanel.
///
/// Cấu trúc GameObject gợi ý:
/// ┌─ ContentPotential
/// │   ├─ TxtPotentialPoints  [TMP_Text] – "Điểm tiềm năng: X"
/// │   ├─ ScrollView (hoặc LayoutGroup thẳng)
/// │   │   └─ Viewport/Content ← gán vào statListContainer
/// │   └─ TxtStatus           [TMP_Text] – trạng thái
///
/// Prefab PotentialStatRowPrefab phải có component PotentialStatRowUI.
///
/// Setup:
/// 1. Tạo Layout (VerticalLayoutGroup) cho statListContainer.
/// 2. Tạo Prefab dòng chỉ số (gắn PotentialStatRowUI), kéo vào potentialRowPrefab.
/// 3. Kéo container vào statListContainer.
/// 4. Kéo TMP_Text điểm tiềm năng vào txtPotentialPoints.
/// 5. Gắn PotentialTabUI vào ContentPotential.
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

    // ── Internal ───────────────────────────────────────────
    private int _playerId = -1;
    private readonly List<PotentialStatRowUI> _rows = new List<PotentialStatRowUI>();

    // ───────────────────────────────────────────────────────
    #region Public API

    public void SetPlayerId(int id) => _playerId = id;

    /// <summary>Load dữ liệu tiềm năng từ server và render.</summary>
    public void Load()
    {
        Debug.Log($"[PotentialTabUI] >>> Load() gọi – _playerId={_playerId}");

        if (_playerId <= 0)
        {
            Debug.LogWarning("[PotentialTabUI] Chưa có playerId, bỏ qua load.");
            SetStatus("Chưa có playerId.");
            return;
        }
        if (APIClient.Instance == null)
        {
            Debug.LogWarning("[PotentialTabUI] APIClient.Instance == null!");
            SetStatus("APIClient không tồn tại.");
            return;
        }
        SetStatus("Đang tải tiềm năng...");
        ClearRows();

        APIClient.Instance.GetPlayerPotential(
            _playerId,
            onSuccess: response =>
            {
                if (response == null)
                {
                    Debug.LogWarning("[PotentialTabUI] Server trả về null!");
                    SetStatus("Lỗi: phản hồi null.");
                    return;
                }

                // ── LOG RAW RESPONSE ──────────────────────────────────
                Debug.Log($"[PotentialTabUI] ✅ Server response nhận được:");
                Debug.Log($"  potential_points_available = {response.potential_points_available}");
                if (response.stats == null)
                {
                    Debug.LogWarning("[PotentialTabUI]   stats = NULL");
                }
                else
                {
                    Debug.Log($"  stats.Length = {response.stats.Length}");
                    foreach (var s in response.stats)
                        Debug.Log($"    stat_name={s.stat_name} | display_name={s.display_name} | current_points={s.current_points} | value_per_point={s.value_per_point} | total_value={s.total_value}");
                }
                // ─────────────────────────────────────────────────────

                PopulateStats(response);
            },
            onError: err =>
            {
                Debug.LogError($"[PotentialTabUI] ❌ Load error: {err}");
                SetStatus($"Lỗi: {err}");
            }
        );
    }

    #endregion

    // ───────────────────────────────────────────────────────
    #region Private helpers

    private void PopulateStats(PlayerPotentialResponse response)
    {
        ClearRows();
        SetStatus("");

        // Hiển thị điểm tiềm năng còn lại
        if (txtPotentialPoints != null)
            txtPotentialPoints.text = $"Điểm tiềm năng: <b>{response.potential_points_available}</b>";
        else
            Debug.LogWarning("[PotentialTabUI] txtPotentialPoints == NULL – chưa gán vào Inspector!");

        if (response.stats == null || response.stats.Length == 0)
        {
            SetStatus("Không có chỉ số tiềm năng.");
            return;
        }

        if (potentialRowPrefab == null)
        {
            Debug.LogError("[PotentialTabUI] potentialRowPrefab == NULL – chưa kéo prefab vào Inspector!");
            return;
        }
        if (statListContainer == null)
        {
            Debug.LogError("[PotentialTabUI] statListContainer == NULL – chưa kéo container vào Inspector!");
            return;
        }

        Debug.Log($"[PotentialTabUI] Instantiate {response.stats.Length} rows vào container '{statListContainer.name}' (active={statListContainer.gameObject.activeInHierarchy})");

        foreach (var stat in response.stats)
        {
            var row = Instantiate(potentialRowPrefab, statListContainer);
            Debug.Log($"[PotentialTabUI]   + Spawned row for '{stat.stat_name}', row.active={row.gameObject.activeSelf}");
            row.SetData(stat, _playerId, response.potential_points_available, onUpgraded: Load);
            _rows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
            if (row != null) Destroy(row.gameObject);
        _rows.Clear();

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
