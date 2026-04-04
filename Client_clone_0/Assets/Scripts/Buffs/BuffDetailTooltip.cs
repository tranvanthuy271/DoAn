using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BuffDetailTooltip — popup hiển thị khi người chơi click vào buff icon trong HUD.
/// Hiển thị: tên buff, mô tả chi tiết, thời gian còn lại (live countdown).
/// Tự động đóng sau autoCloseSeconds giây.
///
/// Cấu trúc Prefab cần tạo trong Unity Editor:
///   BuffDetailTooltip (Panel, Canvas overrideSorting=250, RectTransform 220×110)
///   ├── Background   (Image – dark semi-transparent, optional rounded sprite)
///   ├── NameText     (TMP_Text – FontSize=14, Bold, Anchor=TopLeft + padding 8)
///   ├── DetailText   (TMP_Text – FontSize=11, WordWrap=On)
///   ├── TimeText     (TMP_Text – FontSize=11, Color=yellow, Anchor=BottomLeft + padding 8)
///   └── CloseBtn     (Button – optional, nhỏ, góc phải trên)
///
/// Tham khảo: BuffTooltip.java trong LangLa Client_base
/// </summary>
public class BuffDetailTooltip : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Text tên buff (ví dụ: EXP Gene +20%)")]
    [SerializeField] private TMP_Text nameText;

    [Tooltip("Text mô tả chi tiết (ví dụ: +20% EXP Gene trong 30 phút)")]
    [SerializeField] private TMP_Text detailText;

    [Tooltip("Text thời gian còn lại (live countdown)")]
    [SerializeField] private TMP_Text timeText;

    [Tooltip("Nút đóng tooltip (tuỳ chọn)")]
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [Tooltip("Giây trước khi tự động đóng tooltip")]
    [SerializeField] private float autoCloseSeconds = 5f;

    [Tooltip("Offset X từ vị trí click (pixel) — đẩy tooltip sang phải icon")]
    [SerializeField] private float xOffset = 8f;

    [Tooltip("Offset Y từ vị trí click (pixel) — điều chỉnh dọc (0 = ngang với icon)")]
    [SerializeField] private float yOffset = 0f;

    // Buff đang hiển thị
    private ActiveBuffDto _buff;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    private void Awake()
    {
        // Đảm bảo render đè lên tất cả UI khác
        var canvas = GetComponent<Canvas>();
        if (canvas == null) canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 250;

        if (GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // Bỏ qua mọi LayoutGroup cha (HorizontalLayoutGroup, VerticalLayoutGroup...)
        // để tránh bị kéo giãn/dịch chuyển sai vị trí
        var le = GetComponent<LayoutElement>() ?? gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        gameObject.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Hiển thị tooltip tại vị trí cố định (GO đã được parent vào đúng chỗ từ Inspector).
    /// Reset về giữa parent — không cần tính vị trí theo icon.
    /// </summary>
    public void Show(ActiveBuffDto buff)
    {
        gameObject.SetActive(true);
        _buff = buff;

        // Đưa tooltip về đúng gữa tooltipParent: pivot=center, anchoredPosition=(0,0)
        var rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.pivot           = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }

        if (nameText   != null) nameText.text   = buff.name;
        if (detailText != null) detailText.text  = buff.detail;

        StopAllCoroutines();
        StartCoroutine(UpdateTimeLoop());
        StartCoroutine(AutoCloseAfter(autoCloseSeconds));
    }

    /// <summary>
    /// Hiển thị tooltip sang phải của iconRt, top của panel = top của icon.
    /// Gọi từ BuffHudPanel khi icon được click.
    /// </summary>
    public void Show(ActiveBuffDto buff, RectTransform iconRt, Canvas parentCanvas = null)
    {
        // Phải SetActive(true) TRƯỚC khi gọi StartCoroutine
        gameObject.SetActive(true);

        _buff = buff;

        // Chế độ icon-relative: pivot=(0,1) để anchoredPosition điều khiển góc trên-trái
        var rt = GetComponent<RectTransform>();
        if (rt != null) rt.pivot = new Vector2(0f, 1f);

        if (nameText  != null) nameText.text   = buff.name;
        if (detailText != null) detailText.text = buff.detail;

        // Đặt panel sang phải, top = top của icon
        PositionNearIcon(iconRt, parentCanvas);

        StopAllCoroutines();
        StartCoroutine(UpdateTimeLoop());
        StartCoroutine(AutoCloseAfter(autoCloseSeconds));
    }

    /// <summary>
    /// Tính vị trí trong canvas-space từ RectTransform của icon.
    /// - X: cạnh phải của icon + xOffset
    /// - Y: cạnh trên của icon + yOffset (pivot=(0,1) → top-left của panel khớp)
    /// Tương thích cả Screen Space Overlay lẫn Camera mode.
    /// </summary>
    private void PositionNearIcon(RectTransform iconRt, Canvas parentCanvas)
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null || iconRt == null) return;

        // GetWorldCorners: [0]=BL, [1]=TL, [2]=TR, [3]=BR
        Vector3[] corners = new Vector3[4];
        iconRt.GetWorldCorners(corners);
        // corners[2] = top-right của icon
        Vector3 iconTopRight = corners[2];

        if (parentCanvas != null)
        {
            Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : parentCanvas.worldCamera;

            // Chuyển world top-right → screen → canvas-local
            Vector2 screenPt = RectTransformUtility.WorldToScreenPoint(cam, iconTopRight);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                parentCanvas.GetComponent<RectTransform>(),
                screenPt,
                cam,
                out Vector2 localPoint
            );

            // Pivot=(0,1): anchoredPosition = góc trên-trái của panel
            // → panel top = icon top, panel left = icon right + xOffset
            rt.anchoredPosition = new Vector2(localPoint.x + xOffset, localPoint.y + yOffset);

            // Clamp để không ra ngoài canvas
            ClampToCanvas(rt, parentCanvas.GetComponent<RectTransform>());
        }
        else
        {
            // Fallback Screen Space Overlay: world pos = screen pos
            rt.position = new Vector3(iconTopRight.x + xOffset, iconTopRight.y + yOffset, 0f);
        }
    }

    /// <summary>Clamp anchoredPosition để tooltip không bị cắt ra ngoài màn hình.</summary>
    private void ClampToCanvas(RectTransform rt, RectTransform canvasRt)
    {
        if (canvasRt == null) return;

        Vector2 canvasSize = canvasRt.rect.size;
        Vector2 tooltipSize = rt.rect.size;
        Vector2 pos = rt.anchoredPosition;

        // Đảm bảo tooltip nằm trong bounds của canvas
        float minX = -canvasSize.x * 0.5f;
        float maxX =  canvasSize.x * 0.5f - tooltipSize.x;
        float minY = -canvasSize.y * 0.5f + tooltipSize.y;
        float maxY =  canvasSize.y * 0.5f;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        rt.anchoredPosition = pos;
    }

    /// <summary>Đóng tooltip. Gọi khi click nút đóng hoặc hết thời gian auto-close.</summary>
    public void Close()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
        _buff = null;
    }

    // ── Internal ──────────────────────────────────────────────────────────

    /// <summary>Cập nhật timeText mỗi giây — giống LangLa BuffTooltip live countdown.</summary>
    private IEnumerator UpdateTimeLoop()
    {
        while (_buff != null)
        {
            UpdateTimeDisplay();
            yield return new WaitForSeconds(1f);
        }
    }

    private void UpdateTimeDisplay()
    {
        if (timeText == null || _buff == null) return;

        float remaining = _buff.GetRemainingSeconds();

        if (remaining < 0f)
        {
            timeText.text = "Vĩnh viễn";
            return;
        }

        if (remaining <= 0f)
        {
            timeText.text = "Đã hết hạn";
            Close();
            return;
        }

        // Format: HH:MM:SS hoặc MM:SS
        int totalSec = (int)remaining;
        int hours   = totalSec / 3600;
        int minutes = (totalSec % 3600) / 60;
        int seconds = totalSec % 60;

        if (hours > 0)
            timeText.text = $"Còn lại: {hours:D2}:{minutes:D2}:{seconds:D2}";
        else
            timeText.text = $"Còn lại: {minutes:D2}:{seconds:D2}";
    }

    private IEnumerator AutoCloseAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Close();
    }
}
