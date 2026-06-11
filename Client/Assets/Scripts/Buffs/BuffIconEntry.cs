using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// BuffIconEntry — 1 ô buff icon trong HUD bar.
// Hiển thị icon, countdown ring (radial fill Image), khi click mở BuffDetailTooltip.
// Cấu trúc Prefab cần tạo trong Unity Editor:
// BuffIconEntry (RectTransform 48×48) ← gắn script này
// ├── Background   (Image – Color dark semi-transparent)
// ├── Icon         (Image – sprite buff, PreserveAspect=true)
// ├── CountdownRing(Image – Type=Filled, FillMethod=Radial360, FillOrigin=Top)
// └── TimeLabel    (TMP_Text – FontSize=10, Anchor BottomCenter)
// Tham khảo: StatusEffect.renderHudIcon() trong LangLa Client_base
public class BuffIconEntry : MonoBehaviour, IPointerClickHandler
{
    [Header("UI References")]
    [Tooltip("Image hiển thị sprite của buff")]
    [SerializeField] private Image iconImage;

    [Tooltip("Image dùng làm countdown ring (Type=Filled, Radial360)")]
    [SerializeField] private Image countdownRing;

    [Tooltip("Text nhỏ hiển thị thời gian còn lại (30s / 5m / 1h)")]
    [SerializeField] private TMP_Text timeLabel;

    [Header("Tooltip")]
    [Tooltip("Prefab BuffDetailTooltip — popup hiển thị khi click icon. Có thể kéo thẳng prefab vào đây "
           + "HOẶC để null nếu BuffHudPanel xử lý qua callback OnClicked.")]
    [SerializeField] private BuffDetailTooltip tooltipPrefab;

    [Tooltip("Transform (thường là một Panel/GO cố định trong Canvas) nơi tooltip sẽ được spawn vào. "
           + "Tooltip sẽ hiện ngay tại vị trí của object này.")]
    [SerializeField] public Transform tooltipParent;

    [Header("Settings")]
    [Tooltip("Thư mục Resources/ chứa sprite icon buff — dùng chung với ItemIcons (tên file = số iconId, ví dụ 151.png)")]
    [SerializeField] private string buffIconsFolder = "ItemIcons";

    // Dữ liệu buff đang bind
    private ActiveBuffDto _buffData;

    // Tổng thời gian duration (giây) tính khi Bind() — dùng làm mẫu cho ring
    private float _totalDuration;

    // Tooltip instance đã tạo (dùng lại, không Instantiate liên tục)
    private BuffDetailTooltip _tooltipInstance;

    // Callback tùy chọn — BuffHudPanel có thể đăng ký để tập trung xử lý tooltip.
    // Nếu null và tooltipPrefab được gán, BuffIconEntry tự show tooltip.
    public System.Action<ActiveBuffDto, RectTransform> OnClicked;

    // Track entry nào đang mở tooltip (dùng chung giữa tất cả entry trong scene)
    private static BuffIconEntry _currentOpenEntry;

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Gán dữ liệu buff mới và khởi động vòng update.
    // Gọi từ BuffHudPanel.OnBuffListChanged().
    public void Bind(ActiveBuffDto buff)
    {
        _buffData = buff;

        // Tính tổng duration (giây) từ thời điểm còn lại khi Bind
        float remaining = buff.GetRemainingSeconds();
        _totalDuration = remaining > 0 ? remaining : 1f; // tránh chia 0

        LoadIcon(buff.iconId);

        // Cập nhật visuals ngay lập tức, sau đó bật coroutine
        UpdateVisuals();

        StopAllCoroutines();
        StartCoroutine(UpdateLoop());
    }

    private void OnDisable()
    {
        // Khi entry bị ẩn (pool reuse / buff hết hạn) → đóng tooltip nếu đang mở
        if (_currentOpenEntry == this)
            CloseTooltip();
    }

    // IPointerClickHandler

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_buffData == null) return;

        var rt = GetComponent<RectTransform>();

        // Nếu parent đăng ký callback → ưu tiên dùng callback (BuffHudPanel xử lý)
        if (OnClicked != null)
        {
            OnClicked.Invoke(_buffData, rt);
            return;
        }

        // Toggle / Switch logic
        // Click lại cùng icon đang mở → đóng lại
        if (_currentOpenEntry == this)
        {
            CloseTooltip();
            return;
        }

        // Click sang icon khác → đóng cái cũ trước
        if (_currentOpenEntry != null)
            _currentOpenEntry.CloseTooltip();

        // Mở tooltip của icon này
        ShowTooltipSelf(rt);
    }

    // Đóng tooltip đang mở của entry này (nếu có).
    public void CloseTooltip()
    {
        if (_tooltipInstance != null)
            _tooltipInstance.Close();

        if (_currentOpenEntry == this)
            _currentOpenEntry = null;
    }

    // Hiển thị BuffDetailTooltip trực tiếp từ BuffIconEntry (chế độ standalone).
    // Dùng khi icon không được quản lý bởi BuffHudPanel.
    public void ShowTooltipSelf(RectTransform iconRt = null)
    {
        if (_buffData == null || tooltipPrefab == null) return;

        // Tạo instance một lần, tái sử dụng; parent = tooltipParent (vị trí cố định)
        if (_tooltipInstance == null)
        {
            Transform parent = tooltipParent != null ? tooltipParent : transform.root;
            _tooltipInstance = Instantiate(tooltipPrefab, parent);
        }
        else if (tooltipParent != null && _tooltipInstance.transform.parent != tooltipParent)
        {
            _tooltipInstance.transform.SetParent(tooltipParent, false);
        }

        // Đăng ký entry này là entry đang mở
        _currentOpenEntry = this;

        // Hiển thị nội dung tại vị trí tooltipParent — không cần tính canvas-space
        _tooltipInstance.Show(_buffData);
    }

    // Xử lý nội bộ phục vụ các hàm public.

    // Load sprite icon từ Resources/ItemIcons/ theo iconId (dùng chung folder với icon item).
    private void LoadIcon(int iconId)
    {
        if (iconImage == null) return;

        // Tên file = iconId (ví dụ: 151.png) — cùng quy ước với icon item trong Resources/ItemIcons/
        Sprite sprite = Resources.Load<Sprite>($"{buffIconsFolder}/{iconId}");

        if (sprite != null)
            iconImage.sprite = sprite;
        // Nếu không tìm thấy, giữ nguyên sprite mặc định đã gán trong Prefab
    }

    // Coroutine cập nhật countdown ring và time label mỗi 0.5 giây.
    private IEnumerator UpdateLoop()
    {
        while (true)
        {
            UpdateVisuals();

            // Nếu buff hết hạn → thông báo ra ngoài (BuffHudPanel xử lý)
            if (_buffData != null && _buffData.GetRemainingSeconds() == 0f)
                yield break;

            yield return new WaitForSeconds(0.5f);
        }
    }

    // Cập nhật fillAmount của CountdownRing và text của TimeLabel.
    private void UpdateVisuals()
    {
        if (_buffData == null) return;

        float remaining = _buffData.GetRemainingSeconds();

        // Countdown Ring
        // Tương đương 4-quadrant clock-wipe trong LangLa (icon 315 overlay)
        if (countdownRing != null)
        {
            if (remaining < 0f) // permanent buff
            {
                countdownRing.gameObject.SetActive(false);
            }
            else
            {
                countdownRing.gameObject.SetActive(true);
                countdownRing.fillAmount = Mathf.Clamp01(remaining / _totalDuration);
            }
        }

        // Time Label
        if (timeLabel != null)
        {
            if (remaining < 0f)
            {
                timeLabel.text = ""; // permanent: không hiển thị thời gian
            }
            else if (remaining >= 3600f)
            {
                timeLabel.text = $"{(int)(remaining / 3600f)}h";
            }
            else if (remaining >= 60f)
            {
                timeLabel.text = $"{(int)(remaining / 60f)}m";
            }
            else
            {
                timeLabel.text = $"{(int)remaining}s";
            }
        }
    }
}
