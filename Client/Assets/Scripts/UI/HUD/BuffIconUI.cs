using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// BuffIconUI – Hiển thị 1 buff icon trong HUD.
///
/// Layout GameObject (prefab gợi ý):
///   BuffIcon (BuffIconUI component)
///   ├── IconImage   (Image)                  ← icon buff
///   ├── TimerFill   (Image – radial fill)     ← pie-chart countdown
///   ├── TimeLabel   (TMP_Text)                ← "30:00" / "hết"
///   └── TooltipRoot (GameObject, ẩn mặc định)
///       ├── NameLabel (TMP_Text)
///       └── DetailLabel (TMP_Text)
///
/// Radial countdown giống LangLa client_base:
///   - fillAmount = remainSec / totalDurationSec
///   - Khi fill về 0 → gọi OnExpired
/// </summary>
public class BuffIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Icon")]
    [SerializeField] private Image iconImage;

    [Header("Timer")]
    [SerializeField] private Image timerFill;    // fillMethod = Radial360, fillClockwise = false
    [SerializeField] private TMP_Text timeLabel; // "29:59"

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text   nameLabel;
    [SerializeField] private TMP_Text   detailLabel;

    // ── State ──────────────────────────────────────────────────────────────
    private ActiveBuffDto   _buff;
    private float           _totalDurationSec;  // để tính fill %
    private bool            _expired;

    public event Action<BuffIconUI> OnExpired;

    // ── Public API ─────────────────────────────────────────────────────────

    /// <summary>Khởi tạo icon với dữ liệu buff.</summary>
    public void Setup(ActiveBuffDto buff)
    {
        _buff    = buff;
        _expired = false;

        // Icon
        if (iconImage != null)
        {
            Sprite icon = IconDatabase.Instance != null
                ? IconDatabase.Instance.GetIcon(buff.iconId.ToString())
                : null;
            iconImage.sprite  = icon;
            iconImage.enabled = icon != null;
        }

        // Tính tổng thời gian buff (để radial fill chính xác)
        _totalDurationSec = buff.GetRemainingSeconds();
        if (_totalDurationSec <= 0) _totalDurationSec = -1; // permanent

        // Tooltip
        if (nameLabel   != null) nameLabel.text   = buff.name;
        if (detailLabel != null) detailLabel.text  = buff.detail;
        if (tooltipRoot != null) tooltipRoot.SetActive(false);

        gameObject.SetActive(true);
        Refresh();
    }

    /// <summary>Ẩn và reset icon về trạng thái trống.</summary>
    public void Clear()
    {
        _buff    = null;
        _expired = false;
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
        gameObject.SetActive(false);
    }

    // ── MonoBehaviour ──────────────────────────────────────────────────────

    private void Update()
    {
        if (_buff == null || _expired) return;
        Refresh();
    }

    // ── Hover Tooltip ──────────────────────────────────────────────────────

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipRoot != null) tooltipRoot.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipRoot != null) tooltipRoot.SetActive(false);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private void Refresh()
    {
        float remain = _buff.GetRemainingSeconds();

        // Permanent buff
        if (_totalDurationSec < 0)
        {
            if (timerFill  != null) timerFill.fillAmount = 1f;
            if (timeLabel  != null) timeLabel.text       = "∞";
            return;
        }

        // Buff hết hạn
        if (remain <= 0)
        {
            _expired = true;
            if (timerFill != null) timerFill.fillAmount = 0f;
            if (timeLabel != null) timeLabel.text       = "Hết";
            OnExpired?.Invoke(this);
            return;
        }

        // Radial fill: giảm theo thời gian
        if (timerFill != null)
            timerFill.fillAmount = Mathf.Clamp01(remain / _totalDurationSec);

        // Label định dạng mm:ss
        if (timeLabel != null)
        {
            int mins = Mathf.FloorToInt(remain / 60f);
            int secs = Mathf.FloorToInt(remain % 60f);
            timeLabel.text = remain >= 3600
                ? $"{Mathf.FloorToInt(remain / 3600f)}h{mins % 60:D2}m"
                : $"{mins}:{secs:D2}";
        }
    }
}
