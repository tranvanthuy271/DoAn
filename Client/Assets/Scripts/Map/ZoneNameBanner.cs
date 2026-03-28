using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// UI banner hiện tên zone khi player bước vào khu vực mới.
///
/// Setup:
///   1. Tạo Panel trong Canvas tên "ZoneNameBanner" — đặt ở góc trên giữa màn hình.
///   2. Trong Panel: thêm TMP_Text "ZoneNameText".
///   3. Gắn ZoneNameBanner.cs lên Panel, kéo zoneNameText.
///   4. Panel này mặc định để Inactive — script tự Show/Hide.
///
/// Dùng: ZoneNameBanner.Instance?.Show("Khu Rừng Băng");
/// </summary>
public class ZoneNameBanner : MonoBehaviour
{
    public static ZoneNameBanner Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private TMP_Text zoneNameText;

    [Header("Thời gian hiển thị (giây)")]
    [SerializeField] private float displayDuration = 3f;

    [Header("Fade (tuỳ chọn)")]
    [SerializeField] private CanvasGroup canvasGroup;

    private Coroutine _hideRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        gameObject.SetActive(false);
    }

    /// <summary>Hiện banner với tên zone. Gọi từ ZoneTrigger (client-side).</summary>
    public void Show(string name)
    {
        if (zoneNameText != null)
            zoneNameText.text = name;

        if (_hideRoutine != null) StopCoroutine(_hideRoutine);

        gameObject.SetActive(true);

        if (canvasGroup != null) canvasGroup.alpha = 1f;

        _hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        if (canvasGroup != null)
        {
            yield return new WaitForSeconds(displayDuration - 0.8f);

            // Fade out trong 0.8 giây cuối
            float t = 0f;
            while (t < 0.8f)
            {
                t += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.8f);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(displayDuration);
        }

        gameObject.SetActive(false);
    }
}
