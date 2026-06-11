using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// StatusIconEntry – Một ô icon hiệu ứng (buff hoặc debuff) hiển thị TRÊN ĐẦU player/enemy.
// Khác với BuffIconEntry (dùng trong BuffHudPanel HUD):
// • Không có tooltip khi click (world-space, khó tương tác).
// • Nhỏ hơn: 32×32 px.
// • Luôn hiện cả countdown ring lẫn text giây.
// Cấu trúc Prefab (tạo qua GameTools → Skill Effects → Create Status Icon Prefab):
// StatusIconEntry (RectTransform 32×32) ← gắn script này
// ├── Background   (Image – màu tối bán trong suốt)
// ├── Icon         (Image – sprite hiệu ứng)
// ├── CountdownRing(Image – Type=Filled, FillMethod=Radial360, FillOrigin=Top)
// └── TimeLabel    (TMP_Text – FontSize=8, Anchor BottomCenter)
public class StatusIconEntry : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image    iconImage;
    [SerializeField] private Image    countdownRing;
    [SerializeField] private TMP_Text timeLabel;

    [Header("Settings")]
    [SerializeField] private string iconsFolder = "ItemIcons";

    // Xử lý nội bộ phục vụ các hàm public.
    private float _totalDuration;
    private float _startTime;
    private bool  _isActive;

    private Coroutine _updateCoroutine;

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Bind dữ liệu và bắt đầu countdown.
    // Tham số iconId: ID icon trong Resources/ItemIcons/.
    // Tham số duration: Tổng thời gian hiệu ứng (giây).
    public void Bind(int iconId, float duration)
    {
        _totalDuration = Mathf.Max(0.1f, duration);
        _startTime     = Time.time;
        _isActive      = true;

        LoadIcon(iconId);

        if (_updateCoroutine != null) StopCoroutine(_updateCoroutine);
        _updateCoroutine = StartCoroutine(UpdateLoop());
    }

    // Update countdown từ bên ngoài (dùng khi parent poll mỗi frame).
    // Gọi thay cho coroutine nếu parent đã có Update loop.
    public void UpdateCountdown(float remainingSeconds)
    {
        if (!_isActive) return;
        float ratio = _totalDuration > 0f ? Mathf.Clamp01(remainingSeconds / _totalDuration) : 0f;
        SetVisuals(remainingSeconds, ratio);
    }

    public bool IsActive => _isActive;

    private void OnDisable()
    {
        _isActive = false;
        if (_updateCoroutine != null)
        {
            StopCoroutine(_updateCoroutine);
            _updateCoroutine = null;
        }
    }

    // Xử lý nội bộ phục vụ các hàm public.

    private IEnumerator UpdateLoop()
    {
        while (_isActive)
        {
            float elapsed   = Time.time - _startTime;
            float remaining = Mathf.Max(0f, _totalDuration - elapsed);
            float ratio     = Mathf.Clamp01(remaining / _totalDuration);

            SetVisuals(remaining, ratio);

            if (remaining <= 0f)
            {
                _isActive = false;
                gameObject.SetActive(false);
                yield break;
            }

            yield return new WaitForSeconds(0.1f); // cập nhật mỗi 100 ms
        }
    }

    private void SetVisuals(float remaining, float ratio)
    {
        if (countdownRing != null)
            countdownRing.fillAmount = ratio;

        if (timeLabel != null)
            timeLabel.text = FormatTime(remaining);
    }

    private void LoadIcon(int iconId)
    {
        if (iconImage == null) return;
        var sprite = Resources.Load<Sprite>($"{iconsFolder}/{iconId}");
        if (sprite != null)
            iconImage.sprite = sprite;
        else
            { /* Cảnh báo: Không tìm thấy icon: {iconsFolder}/{iconId} */ }
    }

    private static string FormatTime(float seconds)
    {
        if (seconds >= 60f) return $"{Mathf.CeilToInt(seconds / 60f)}m";
        return $"{Mathf.CeilToInt(seconds)}s";
    }
}
