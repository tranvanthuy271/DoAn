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
    private bool _isExternalProfileView;
    private PlayerSkillInfo[] _externalSkills;
    private string _externalCharacterName;
    private bool _hasPresentationSnapshots;
    private TextPresentationSnapshot _skillPointsSnapshot;
    private TextPresentationSnapshot _statusSnapshot;

    private struct TextPresentationSnapshot
    {
        public float FontSize;
        public FontStyles FontStyle;
        public TextAlignmentOptions Alignment;
        public bool WordWrapping;
        public TextOverflowModes OverflowMode;
        public Vector4 Margin;
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;
        public Vector2 Pivot;
    }

    private void Awake()
    {
        CaptureTextPresentation(txtSkillPoints, ref _skillPointsSnapshot);
        CaptureTextPresentation(txtStatus, ref _statusSnapshot);
        _hasPresentationSnapshots = true;
    }

    // ───────────────────────────────────────────────────────
    #region Public API

    public void SetPlayerId(int id) => _playerId = id;

    public void ShowFriendSkills(PlayerSkillInfo[] skills, string characterName)
    {
        _isExternalProfileView = true;
        _externalSkills = skills;
        _externalCharacterName = characterName;
        Debug.Log($"[SkillTabUI] ShowFriendSkills characterName='{characterName}' skills={skills?.Length ?? 0}");
        Load();
    }

    public void ClearFriendSkills()
    {
        if (!_isExternalProfileView && _externalSkills == null)
            return;

        Debug.Log("[SkillTabUI] ClearFriendSkills()");
        _isExternalProfileView = false;
        _externalSkills = null;
        _externalCharacterName = null;
        ApplyCurrentPresentation();
    }

    /// <summary>Load skills từ server và render danh sách.</summary>
    public void Load()
    {
        ApplyCurrentPresentation();

        if (_isExternalProfileView)
        {
            RenderFriendSkills();
            return;
        }

        if (_playerId <= 0) { SetStatus("Chưa có playerId."); return; }
        if (GameplayCommandService.Instance == null) { SetStatus("Server chưa sẵn sàng."); return; }

        SetStatus("Đang tải kỹ năng...");
        ClearRows();

        // Unsubscribe trước để tránh double-subscribe khi Load() gọi nhiều lần
        GameplayCommandService.OnSkillsReceived -= HandleSkillsReceived;
        GameplayCommandService.OnSkillsReceived += HandleSkillsReceived;
        GameplayCommandService.Instance.GetPlayerSkillsServerRpc();
    }

    private void HandleSkillsReceived(string json)
    {
        GameplayCommandService.OnSkillsReceived -= HandleSkillsReceived;
        try
        {
            if (json.Contains("\"error\"")) { SetStatus($"Lỗi: {json}"); return; }
            var response = JsonUtility.FromJson<PlayerSkillsResponse>(json);
            if (response == null) { SetStatus("Lỗi: phản hồi null."); return; }
            PopulateSkills(response);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SkillTabUI] Parse error: {ex.Message}");
            SetStatus($"Lỗi: {ex.Message}");
        }
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

    private void ApplyCurrentPresentation()
    {
        if (!_hasPresentationSnapshots)
            return;

        if (_isExternalProfileView)
        {
            ApplyExternalTextPresentation(txtSkillPoints, _skillPointsSnapshot, 60f, 64f, _skillPointsSnapshot.FontSize * 2f, FontStyles.Bold);
            ApplyExternalTextPresentation(txtStatus, _statusSnapshot, 108f, 56f, _statusSnapshot.FontSize * 2f, FontStyles.Bold);
            return;
        }

        RestoreTextPresentation(txtSkillPoints, _skillPointsSnapshot);
        RestoreTextPresentation(txtStatus, _statusSnapshot);
    }

    private static void CaptureTextPresentation(TMP_Text label, ref TextPresentationSnapshot snapshot)
    {
        if (label == null)
            return;

        var rect = label.rectTransform;
        snapshot.FontSize = label.fontSize;
        snapshot.FontStyle = label.fontStyle;
        snapshot.Alignment = label.alignment;
        snapshot.WordWrapping = label.enableWordWrapping;
        snapshot.OverflowMode = label.overflowMode;
        snapshot.Margin = label.margin;
        snapshot.AnchorMin = rect.anchorMin;
        snapshot.AnchorMax = rect.anchorMax;
        snapshot.AnchoredPosition = rect.anchoredPosition;
        snapshot.SizeDelta = rect.sizeDelta;
        snapshot.Pivot = rect.pivot;
    }

    private static void RestoreTextPresentation(TMP_Text label, TextPresentationSnapshot snapshot)
    {
        if (label == null)
            return;

        var rect = label.rectTransform;
        label.fontSize = snapshot.FontSize;
        label.fontStyle = snapshot.FontStyle;
        label.alignment = snapshot.Alignment;
        label.enableWordWrapping = snapshot.WordWrapping;
        label.overflowMode = snapshot.OverflowMode;
        label.margin = snapshot.Margin;

        rect.anchorMin = snapshot.AnchorMin;
        rect.anchorMax = snapshot.AnchorMax;
        rect.anchoredPosition = snapshot.AnchoredPosition;
        rect.sizeDelta = snapshot.SizeDelta;
        rect.pivot = snapshot.Pivot;
    }

    private static void ApplyExternalTextPresentation(TMP_Text label, TextPresentationSnapshot snapshot, float bottomOffset, float height, float fontSize, FontStyles fontStyle)
    {
        if (label == null)
            return;

        var rect = label.rectTransform;
        label.fontSize = Mathf.Max(fontSize, snapshot.FontSize);
        label.fontStyle = fontStyle;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = Vector4.zero;

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, bottomOffset);
        rect.sizeDelta = new Vector2(-96f, height);
    }

    private void RenderFriendSkills()
    {
        ClearRows();
        SetStatus("");

        if (txtSkillPoints != null)
            txtSkillPoints.text = string.IsNullOrWhiteSpace(_externalCharacterName)
                ? "Kỹ năng"
                : $"Kỹ năng của {_externalCharacterName}";

        if (_externalSkills == null || _externalSkills.Length == 0)
        {
            SetStatus("Người chơi này chưa có kỹ năng nào.");
            return;
        }

        if (skillRowPrefab == null || skillListContainer == null)
        {
            Debug.LogError("[SkillTabUI] Thiếu skillRowPrefab hoặc skillListContainer khi render friend profile.");
            return;
        }

        foreach (var skill in _externalSkills)
        {
            var row = Instantiate(skillRowPrefab, skillListContainer);
            row.SetData(skill, 0, onUpgraded: null, readOnly: true);
            _rows.Add(row);
        }
    }

    #endregion
}
