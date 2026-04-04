using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Thanh MP (Mana) dùng Slider — tương tự HealthBar.cs nhưng đọc từ NetworkPlayerDataSync.
///
/// Cấu trúc Hierarchy gợi ý:
///   MpBar (gắn script này)
///   ├── Slider          — component Slider, Min=0, Max=1, Interactable = false
///   │   └── Fill Area / Fill   — Image, đây là fillImage
///   └── MpText (TMP)    — hiển thị "50 / 100" (tuỳ chọn)
/// </summary>
public class MpBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider mpSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI mpText;

    [Header("Colors")]
    [SerializeField] private Color fullMpColor = new Color(0.2f, 0.4f, 1f);    // xanh dương
    [SerializeField] private Color lowMpColor  = new Color(0.6f, 0.2f, 0.8f);  // tím khi cạn
    [SerializeField] [Range(0f, 1f)] private float lowMpThreshold = 0.25f;

    // ── Internal ────────────────────────────────────────────────────────────
    private NetworkPlayerDataSync dataSync;
    private float retryTimer = 0f;
    private const float RetryInterval = 0.3f;

    // ════════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ════════════════════════════════════════════════════════════════════════

    private void Start()
    {
        if (mpSlider == null)
            Debug.LogError("[MpBar] THIẾU: 'Mp Slider' chưa được gán trong Inspector!", this);
        if (fillImage == null)
            Debug.LogWarning("[MpBar] THIẾU: 'Fill Image' chưa được gán — slider sẽ không đổi màu.", this);

        if (mpSlider != null)
        {
            mpSlider.minValue     = 0f;
            mpSlider.maxValue     = 1f;
            mpSlider.interactable = false;
        }

        TryBind();
    }

    private void Update()
    {
        // Phát hiện dataSync không còn hợp lệ (player bị despawn khi NGO shutdown hoặc chuyển scene)
        if (dataSync != null && !dataSync.IsSpawned)
        {
            dataSync.networkMp.OnValueChanged    -= OnMpChanged;
            dataSync.networkMaxMp.OnValueChanged -= OnMaxMpChanged;
            dataSync = null;
        }

        if (dataSync != null) return;

        retryTimer -= Time.deltaTime;
        if (retryTimer > 0f) return;
        retryTimer = RetryInterval;

        TryBind();
    }

    private void TryBind()
    {
        // Tìm đúng NetworkPlayerDataSync của local player (IsOwner=true)
        // Dùng FindObjectsOfType để tránh lấy nhầm của player khác trong multiplayer
        foreach (var s in FindObjectsOfType<NetworkPlayerDataSync>())
        {
            if (s.IsSpawned && s.IsOwner) { dataSync = s; break; }
        }
        if (dataSync == null) return;

        Debug.Log($"[MpBar] Bind local player '{dataSync.gameObject.name}' — MP: {dataSync.networkMp.Value}/{dataSync.networkMaxMp.Value}");
        dataSync.networkMp.OnValueChanged    += OnMpChanged;
        dataSync.networkMaxMp.OnValueChanged += OnMaxMpChanged;
        UpdateBar(dataSync.networkMp.Value, dataSync.networkMaxMp.Value);
    }

    private void OnDestroy()
    {
        if (dataSync != null)
        {
            dataSync.networkMp.OnValueChanged    -= OnMpChanged;
            dataSync.networkMaxMp.OnValueChanged -= OnMaxMpChanged;
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Public API
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gán thủ công NetworkPlayerDataSync (nếu không dùng auto-find)
    /// </summary>
    public void SetDataSync(NetworkPlayerDataSync sync)
    {
        if (dataSync != null)
        {
            dataSync.networkMp.OnValueChanged    -= OnMpChanged;
            dataSync.networkMaxMp.OnValueChanged -= OnMaxMpChanged;
        }

        dataSync = sync;

        if (dataSync != null)
        {
            dataSync.networkMp.OnValueChanged    += OnMpChanged;
            dataSync.networkMaxMp.OnValueChanged += OnMaxMpChanged;
            UpdateBar(dataSync.networkMp.Value, dataSync.networkMaxMp.Value);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Private helpers
    // ════════════════════════════════════════════════════════════════════════

    private void OnMpChanged(int prev, int current)
    {
        int max = dataSync != null ? dataSync.networkMaxMp.Value : current;
        UpdateBar(current, max);
    }

    private void OnMaxMpChanged(int prev, int current)
    {
        int mp = dataSync != null ? dataSync.networkMp.Value : 0;
        UpdateBar(mp, current);
    }

    private void UpdateBar(int current, int max)
    {
        if (max <= 0)
        {
            Debug.LogWarning($"[MpBar] networkMaxMp = {max} — chưa có data từ server, bỏ qua update.");
            return;
        }

        float pct = (float)current / max;
        Debug.Log($"[MpBar] Cập nhật: {current}/{max} ({pct * 100:F0}%)");

        if (mpSlider != null)
            mpSlider.value = pct;
        else
            Debug.LogWarning("[MpBar] mpSlider = null khi UpdateBar được gọi!");

        if (fillImage != null)
            fillImage.color = Color.Lerp(lowMpColor, fullMpColor,
                Mathf.Clamp01((pct - lowMpThreshold) / (1f - lowMpThreshold)));

        if (mpText != null)
            mpText.text = $"{current} / {max}";
    }
}
