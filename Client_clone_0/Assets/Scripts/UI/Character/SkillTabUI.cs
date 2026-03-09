using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SkillTabUI – Tab "Kỹ Năng" trong CharacterPanel.
///
/// Cấu trúc GameObject gợi ý:
/// ┌─ ContentSkill
/// │   ├─ TxtSkillPoints     [TMP_Text] – "Điểm kỹ năng: X"
/// │   ├─ ScrollView
/// │   │   └─ Viewport
/// │   │       └─ Content    ← gán vào skillListContainer
/// │   └─ TxtStatus          [TMP_Text] – trạng thái loading/lỗi
///
/// Prefab SkillRowPrefab phải có component SkillRowUI.
///
/// Setup:
/// 1. Tạo Scroll View có Content với VerticalLayoutGroup + ContentSizeFitter.
/// 2. Tạo Prefab dòng skill (gắn SkillRowUI), kéo vào skillRowPrefab.
/// 3. Kéo Content transform vào skillListContainer.
/// 4. Kéo TMP_Text hiển thị điểm kỹ năng vào txtSkillPoints.
/// 5. Gắn SkillTabUI vào ContentSkill.
/// </summary>
public class SkillTabUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text hiển thị số điểm kỹ năng còn lại")]
    [SerializeField] private TMP_Text txtSkillPoints;

    [Tooltip("Transform Content trong ScrollView – các dòng skill sẽ được spawn vào đây")]
    [SerializeField] private Transform skillListContainer;

    [Tooltip("Prefab của 1 dòng skill (phải có SkillRowUI)")]
    [SerializeField] private SkillRowUI skillRowPrefab;

    [Tooltip("Text trạng thái (loading, lỗi, rỗng...)")]
    [SerializeField] private TMP_Text txtStatus;

    // ── Internal ───────────────────────────────────────────
    private int _playerId = -1;
    private readonly List<SkillRowUI> _rows = new List<SkillRowUI>();

    // ───────────────────────────────────────────────────────
    #region Public API

    public void SetPlayerId(int id) => _playerId = id;

    /// <summary>Load skills từ server và render danh sách.</summary>
    public void Load()
    {
        if (_playerId <= 0)
        {
            SetStatus("Chưa có playerId.");
            return;
        }
        if (APIClient.Instance == null)
        {
            SetStatus("APIClient không tồn tại.");
            return;
        }

        SetStatus("Đang tải kỹ năng...");
        ClearRows();

        APIClient.Instance.GetPlayerSkills(
            _playerId,
            onSuccess: response =>
            {
                if (response == null)
                {
                    SetStatus("Lỗi: phản hồi null.");
                    return;
                }
                PopulateSkills(response);
            },
            onError: err =>
            {
                SetStatus($"Lỗi: {err}");
                Debug.LogError($"[SkillTabUI] Load error: {err}");
            }
        );
    }

    #endregion

    // ───────────────────────────────────────────────────────
    #region Private helpers

    private void PopulateSkills(PlayerSkillsResponse response)
    {
        ClearRows();
        SetStatus("");

        // Hiển thị điểm kỹ năng
        if (txtSkillPoints != null)
            txtSkillPoints.text = $"Điểm kỹ năng: <b>{response.skill_points_available}</b>";

        if (response.skills == null || response.skills.Length == 0)
        {
            SetStatus("Chưa có skill nào trong database.");
            return;
        }

        if (skillRowPrefab == null || skillListContainer == null)
        {
            Debug.LogError("[SkillTabUI] Thiếu skillRowPrefab hoặc skillListContainer!");
            return;
        }

        foreach (var skill in response.skills)
        {
            var row = Instantiate(skillRowPrefab, skillListContainer);
            row.SetData(skill, _playerId, onUpgraded: Load); // Load lại sau mỗi nâng cấp
            _rows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
            if (row != null) Destroy(row.gameObject);
        _rows.Clear();

        // Xóa luôn con mồ côi (ví dụ test thủ công trong editor)
        if (skillListContainer != null)
            for (int i = skillListContainer.childCount - 1; i >= 0; i--)
                Destroy(skillListContainer.GetChild(i).gameObject);
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
