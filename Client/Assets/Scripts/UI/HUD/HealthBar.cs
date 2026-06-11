using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Thanh HP dùng Slider — đọc từ NetworkPlayerDataSync (giống tab thông tin PlayerInfoUI).
// Script tự động tìm NetworkPlayerDataSync trong scene, không cần gán thủ công.
public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI healthTextTMP;

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;

    private NetworkPlayerDataSync dataSync;
    private float retryTimer = 0f;
    private const float RetryInterval = 0.3f;
    private int _retryCount = 0;

    private void Start()
    {
        // Kiểm tra UI references
        if (healthSlider == null)
            Debug.LogError("[HealthBar] THIẾU: 'Health Slider' chưa được gán trong Inspector!", this);
        if (fillImage == null)
            Debug.LogWarning("[HealthBar] THIẾU: 'Fill Image' chưa được gán — slider sẽ không đổi màu.", this);

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
        }

        TryBind();
    }

    private void Update()
    {
        // Phát hiện dataSync không còn hợp lệ (player bị despawn khi NGO shutdown hoặc chuyển scene)
        if (dataSync != null && !dataSync.IsSpawned)
        {
            dataSync.networkHp.OnValueChanged    -= OnHpChanged;
            dataSync.networkMaxHp.OnValueChanged -= OnMaxHpChanged;
            dataSync = null;
        }

        if (dataSync != null) return; // đã bind và còn valid

        retryTimer -= Time.deltaTime;
        if (retryTimer > 0f) return;
        retryTimer = RetryInterval;

        TryBind();
    }

    private void TryBind()
    {
        // Tìm đúng NetworkPlayerDataSync của local player (IsOwner=true)
        // Dùng FindObjectsOfType để tránh lấy nhầm của player khác trong multiplayer
        var allSyncs = FindObjectsOfType<NetworkPlayerDataSync>();
        foreach (var s in allSyncs)
        {
            if (s.IsSpawned && s.IsOwner) { dataSync = s; break; }
        }
        if (dataSync == null)
        {
            _retryCount++;
            // Log every ~3 s (10 retries × 0.3 s)
            if (_retryCount % 10 == 1)
                Debug.LogWarning($"[HealthBar] Chờ player spawn... retry #{_retryCount} | " +
                                 $"totalFound={allSyncs.Length} " +
                                 $"spawned={System.Array.FindAll(allSyncs, s => s.IsSpawned).Length} " +
                                 $"owned={System.Array.FindAll(allSyncs, s => s.IsSpawned && s.IsOwner).Length}");
            return;
        }

        Debug.Log($"[HealthBar] Bind local player '{dataSync.gameObject.name}' — HP: {dataSync.networkHp.Value}/{dataSync.networkMaxHp.Value}");
        dataSync.networkHp.OnValueChanged    += OnHpChanged;
        dataSync.networkMaxHp.OnValueChanged += OnMaxHpChanged;
        UpdateBar(dataSync.networkHp.Value, dataSync.networkMaxHp.Value);
    }

    private void OnDestroy()
    {
        if (dataSync != null)
        {
            dataSync.networkHp.OnValueChanged    -= OnHpChanged;
            dataSync.networkMaxHp.OnValueChanged -= OnMaxHpChanged;
        }
    }

    private void OnHpChanged(int prev, int current)
    {
        int max = dataSync != null ? dataSync.networkMaxHp.Value : current;
        UpdateBar(current, max);
    }

    private void OnMaxHpChanged(int prev, int current)
    {
        int hp = dataSync != null ? dataSync.networkHp.Value : 0;
        UpdateBar(hp, current);
    }

    private void UpdateBar(int current, int max)
    {
        if (max <= 0)
        {
            Debug.LogWarning($"[HealthBar] networkMaxHp = {max} — chưa có data từ server, bỏ qua update.");
            return;
        }

        float pct = (float)current / max;
        Debug.Log($"[HealthBar] Cập nhật: {current}/{max} ({pct * 100:F0}%)");

        if (healthSlider != null)
            healthSlider.value = pct;

        if (fillImage != null)
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor,
                Mathf.Clamp01((pct - lowHealthThreshold) / (1f - lowHealthThreshold)));

        if (healthTextTMP != null)
            healthTextTMP.text = $"{current} / {max}";
    }
}

