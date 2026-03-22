using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// Thanh HP thế giới (World Space) hiển thị trên đầu mỗi player.
///
/// Cách cài đặt trong Unity:
///   1. Tạo child Canvas (World Space) trong Player Prefab
///   2. Đặt tên "PlayerHpBarCanvas" và Scale nhỏ (ví dụ 0.01, 0.01, 0.01)
///   3. LocalPosition = (0, 1.2, 0) để hiện trên đầu
///   4. Gắn script này lên Canvas đó
///   5. Kéo Slider + Text vào Inspector
///
/// Tính năng:
///   - Tự động ẩn cho local player (hideForLocalPlayer = true)
///   - Hiển thị tên nhân vật (tùy chọn)
///   - Cập nhật real-time khi HP thay đổi
/// </summary>
public class PlayerWorldHpBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Slider hiển thị thanh HP (Min=0, Max=1)")]
    [SerializeField] private Slider hpSlider;

    [Tooltip("Image fill của Slider để đổi màu")]
    [SerializeField] private Image fillImage;

    [Tooltip("Text hiển thị số HP (ví dụ: 80/100)")]
    [SerializeField] private TextMeshProUGUI hpText;

    [Tooltip("Text hiển thị tên nhân vật (tùy chọn)")]
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor  = Color.green;
    [SerializeField] private Color lowHealthColor   = Color.red;
    [SerializeField] [Range(0f, 1f)] private float lowHealthThreshold = 0.3f;

    [Header("Visibility")]
    [Tooltip("Ẩn thanh HP của chính mình (chỉ hiện HP của người khác)")]
    [SerializeField] private bool hideForLocalPlayer = true;

    [Header("Billboard")]
    [Tooltip("Canvas luôn quay về phía camera")]
    [SerializeField] private bool faceCamera = true;

    // ── Internal ──────────────────────────────────────────────────────────────
    private NetworkObject networkObject;
    private NetworkPlayerDataSync dataSync;
    private Camera mainCamera;
    private float retryTimer;
    private const float RetryInterval = 0.5f;
    private bool isBound = false;
    private Vector3 initialLocalScale;

    private void Awake()
    {
        networkObject = GetComponentInParent<NetworkObject>();
        mainCamera    = Camera.main;
        initialLocalScale = transform.localScale;

        if (hpSlider != null)
        {
            hpSlider.minValue     = 0f;
            hpSlider.maxValue     = 1f;
            hpSlider.interactable = false;
        }
    }

    private void Start()
    {
        TryBind();
    }

    private void Update()
    {
        if (!isBound)
        {
            retryTimer -= Time.deltaTime;
            if (retryTimer > 0f) return;
            retryTimer = RetryInterval;
            TryBind();
            return;
        }

        // Billboard: canvas luôn quay về camera
        if (faceCamera && mainCamera != null)
            transform.rotation = mainCamera.transform.rotation;

        // Counteract parent flip: khi player quay trái (localScale.x = -1),
        // canvas con sẽ bị scale âm → text bị soi gương. Fix: đảo lại localScale.x
        Transform parentTr = transform.parent;
        if (parentTr != null)
        {
            float parentWorldScaleX = parentTr.lossyScale.x;
            float correctedX = parentWorldScaleX < 0f
                ? -Mathf.Abs(initialLocalScale.x)
                :  Mathf.Abs(initialLocalScale.x);
            transform.localScale = new Vector3(correctedX, initialLocalScale.y, initialLocalScale.z);
        }
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    // ── Binding ───────────────────────────────────────────────────────────────

    private void TryBind()
    {
        dataSync = GetComponentInParent<NetworkPlayerDataSync>();
        if (dataSync == null) return;

        // Ẩn nếu là local player
        if (hideForLocalPlayer && networkObject != null && networkObject.IsOwner)
        {
            gameObject.SetActive(false);
            isBound = true;
            return;
        }

        SubscribeEvents();
        UpdateBar(dataSync.networkHp.Value, dataSync.networkMaxHp.Value);

        if (playerNameText != null)
            playerNameText.text = dataSync.networkCharacterName.Value.ToString();

        isBound = true;
    }

    private void SubscribeEvents()
    {
        if (dataSync == null) return;
        dataSync.networkHp.OnValueChanged          += OnHpChanged;
        dataSync.networkMaxHp.OnValueChanged       += OnMaxHpChanged;
        dataSync.networkCharacterName.OnValueChanged += OnNameChanged;
    }

    private void UnsubscribeEvents()
    {
        if (dataSync == null) return;
        dataSync.networkHp.OnValueChanged          -= OnHpChanged;
        dataSync.networkMaxHp.OnValueChanged       -= OnMaxHpChanged;
        dataSync.networkCharacterName.OnValueChanged -= OnNameChanged;
    }

    // ── Callbacks ─────────────────────────────────────────────────────────────

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

    private void OnNameChanged(FixedString64Bytes prev, FixedString64Bytes current)
    {
        if (playerNameText != null)
            playerNameText.text = current.ToString();
    }

    // ── UI Update ─────────────────────────────────────────────────────────────

    private void UpdateBar(int current, int max)
    {
        if (max <= 0) return;

        float pct = (float)current / max;

        if (hpSlider != null)
            hpSlider.value = pct;

        if (fillImage != null)
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor,
                Mathf.Clamp01((pct - lowHealthThreshold) / (1f - lowHealthThreshold)));

        if (hpText != null)
            hpText.text = $"{current}/{max}";
    }
}
