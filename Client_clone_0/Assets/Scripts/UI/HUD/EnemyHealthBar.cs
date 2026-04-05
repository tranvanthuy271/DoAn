using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// EnemyHealthBar - Hiển thị HP bar trên đầu enemy
/// Tự động follow enemy và update khi HP thay đổi
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider; // ⭐ Slider chính (QUAN TRỌNG)
    [SerializeField] private Image fillImage; // Tự động tìm từ Slider > Fill Area > Fill
    [SerializeField] private Image backgroundImage; // Tự động tìm từ Slider > Background
    [SerializeField] private TextMeshProUGUI healthText; // Text trên UI Canvas (optional)
    [SerializeField] private TextMesh healthText3D; // Text 3D trên enemy (optional)

    [Header("Target")]
    [SerializeField] private NetworkEnemyHealth networkEnemyHealth; // Ưu tiên dùng NetworkEnemyHealth
    [SerializeField] private EnemyHealth enemyHealth; // Fallback cho EnemyHealth (không network)
    [SerializeField] private Transform enemyTransform; // Transform của enemy để follow

    [Header("Colors")]
    [SerializeField] private Color healthColor = Color.red; // Màu đỏ cho HP còn lại (Fill)
    [SerializeField] private Color damageColor = Color.white; // Màu trắng cho phần đã mất (Background)

    [Header("Position Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 1.5f, 0); // Offset từ enemy
    [SerializeField] private float followSmoothness = 10f; // Độ mượt khi follow
    [SerializeField] private bool alwaysFaceCamera = true; // Luôn quay về camera (cho 3D text)

    [Header("Visibility")]
    [SerializeField] private bool hideWhenFull = false; // Ẩn khi HP đầy
    [SerializeField] private bool hideWhenDead = true; // Ẩn khi chết

    private Camera mainCamera;
    private bool _setupDone = false; // flag tránh double subscribe khi cả Setup() lẫn Start() đều chạy
    private Canvas canvas;
    private RectTransform rectTransform;
    private Vector2 preservedSize; // Lưu size ban đầu để preserve

    private void ApplyVisualDefaults()
    {
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
            healthSlider.value = 1;
        }

        if (fillImage != null)
        {
            fillImage.color = healthColor;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = damageColor;
        }
    }

    private void UnregisterHealthEvents()
    {
        if (networkEnemyHealth != null)
        {
            networkEnemyHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            networkEnemyHealth.OnDeath.RemoveListener(OnEnemyDeath);
        }

        if (enemyHealth != null)
        {
            enemyHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            enemyHealth.OnDeath.RemoveListener(OnEnemyDeath);
        }
    }

    private void RegisterNetworkHealthEvents()
    {
        if (networkEnemyHealth == null)
            return;

        networkEnemyHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        networkEnemyHealth.OnDeath.RemoveListener(OnEnemyDeath);
        networkEnemyHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        networkEnemyHealth.OnDeath.AddListener(OnEnemyDeath);
    }

    private void RegisterEnemyHealthEvents()
    {
        if (enemyHealth == null)
            return;

        enemyHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        enemyHealth.OnDeath.RemoveListener(OnEnemyDeath);
        enemyHealth.OnHealthChanged.AddListener(UpdateHealthBar);
        enemyHealth.OnDeath.AddListener(OnEnemyDeath);
    }

    private void Awake()
    {
        // Luôn lấy NetworkEnemyHealth từ root enemy (tránh lấy nhầm component duplicate trên HP bar canvas)
        // transform.root trả về đỉnh hierarchy (Enemy1), bỏ qua component cùng cấp với HP bar
        networkEnemyHealth = transform.root.GetComponent<NetworkEnemyHealth>();
        if (networkEnemyHealth == null)
            networkEnemyHealth = GetComponentInParent<NetworkEnemyHealth>();

        // Fallback: Tìm EnemyHealth nếu không có NetworkEnemyHealth
        if (networkEnemyHealth == null && enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        // Tìm enemy transform nếu chưa gán
        if (enemyTransform == null)
        {
            if (networkEnemyHealth != null)
            {
                enemyTransform = networkEnemyHealth.transform;
            }
            else if (enemyHealth != null)
            {
                enemyTransform = enemyHealth.transform;
            }
        }

        // Tìm canvas
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = GetComponent<Canvas>();
        }

        rectTransform = GetComponent<RectTransform>();
        
        // Preserve size ban đầu từ prefab/scene
        if (rectTransform != null)
        {
            preservedSize = rectTransform.sizeDelta;
        }

        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            mainCamera = FindObjectOfType<Camera>();
        }

        // ⭐ TỰ ĐỘNG TÌM SLIDER VÀ CÁC COMPONENT TỪ SLIDER
        if (healthSlider == null)
        {
            healthSlider = GetComponentInChildren<Slider>();
        }

        if (healthSlider != null)
        {
            // Tự động tìm Fill Image từ Slider
            if (fillImage == null)
            {
                Transform fillArea = healthSlider.transform.Find("Fill Area");
                if (fillArea != null)
                {
                    Transform fill = fillArea.Find("Fill");
                    if (fill != null)
                    {
                        fillImage = fill.GetComponent<Image>();
                    }
                }
            }

            // Tự động tìm Background Image từ Slider
            if (backgroundImage == null)
            {
                Transform background = healthSlider.transform.Find("Background");
                if (background != null)
                {
                    backgroundImage = background.GetComponent<Image>();
                }
            }
        }
    }

    private void Start()
    {
        // Nếu Setup() đã được gọi từ EnemyHealthBarSpawner thì không subscribe lại (tránh double-call)
        if (_setupDone && (networkEnemyHealth != null || enemyHealth != null))
        {
            ApplyVisualDefaults();
            return;
        }

        // Ưu tiên dùng NetworkEnemyHealth
        if (networkEnemyHealth != null)
        {
            RegisterNetworkHealthEvents();

            // Initialize
            UpdateHealthBar(networkEnemyHealth.GetCurrentHealth(), networkEnemyHealth.GetMaxHealth());
            _setupDone = true;
        }
        else if (enemyHealth != null)
        {
            RegisterEnemyHealthEvents();

            // Initialize
            UpdateHealthBar(enemyHealth.GetCurrentHealth(), enemyHealth.GetMaxHealth());
            _setupDone = true;
        }
        else
        {
            // Debug.LogWarning($"[EnemyHealthBar] NetworkEnemyHealth or EnemyHealth not found on {gameObject.name}!");
        }

        ApplyVisualDefaults();

        // ⭐ DISABLED: Không set position ban đầu - Giữ nguyên position từ prefab/scene
        // if (enemyTransform != null)
        // {
        //     Vector3 initialPosition = enemyTransform.position + offset;
        //     if (!float.IsNaN(initialPosition.x) && !float.IsInfinity(initialPosition.x))
        //     {
        //         transform.position = initialPosition;
        //     }
        // }
    }

    private void Update()
    {
        // ⭐ DISABLED: Không follow enemy position - Health bar cố định vị trí
        // if (enemyTransform != null && canvas != null && rectTransform != null)
        // {
        //     UpdatePosition();
        // }

        // ⭐ QUAN TRỌNG: Preserve size để không bị Unity tự động thay đổi
        if (rectTransform != null && preservedSize != Vector2.zero)
        {
            if (rectTransform.sizeDelta != preservedSize)
            {
                rectTransform.sizeDelta = preservedSize;
            }
        }

        // Face camera (cho 3D text)
        if (alwaysFaceCamera && mainCamera != null && healthText3D != null)
        {
            healthText3D.transform.LookAt(mainCamera.transform);
            healthText3D.transform.Rotate(0, 180, 0); // Flip để đọc được
        }
    }

    /// <summary>
    /// Update vị trí health bar để follow enemy
    /// Với World Space Canvas, set position trực tiếp trong world space
    /// </summary>
    private void UpdatePosition()
    {
        if (enemyTransform == null || canvas == null) return;

        // ⭐ Với World Space Canvas, set position trực tiếp trong world space
        // Không cần convert qua screen space
        Vector3 targetWorldPosition = enemyTransform.position + offset;

        // Validate position (tránh NaN/Infinity)
        if (float.IsNaN(targetWorldPosition.x) || float.IsNaN(targetWorldPosition.y) || float.IsNaN(targetWorldPosition.z) ||
            float.IsInfinity(targetWorldPosition.x) || float.IsInfinity(targetWorldPosition.y) || float.IsInfinity(targetWorldPosition.z))
        {
            // Debug.LogWarning($"[EnemyHealthBar] Invalid target position: {targetWorldPosition}");
            return;
        }

        // Smooth follow trong world space
        transform.position = Vector3.Lerp(
            transform.position,
            targetWorldPosition,
            Time.deltaTime * followSmoothness
        );

        // ⭐ QUAN TRỌNG: Preserve size để không bị Unity tự động thay đổi
        if (rectTransform != null && preservedSize != Vector2.zero)
        {
            if (rectTransform.sizeDelta != preservedSize)
            {
                rectTransform.sizeDelta = preservedSize;
            }
        }
    }

    /// <summary>
    /// Update health bar UI
    /// </summary>
    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        float healthPercent = (float)currentHealth / maxHealth;

        // Hide/show based on settings
        if (hideWhenFull && healthPercent >= 1f)
        {
            gameObject.SetActive(false);
            return;
        }

        if (hideWhenDead && currentHealth <= 0)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Update slider (Slider tự động xử lý fill và background)
        if (healthSlider != null)
        {
            // Slider tự động hiển thị:
            // - Fill (màu đỏ) = healthPercent
            // - Background (màu trắng) = phần còn lại (1 - healthPercent)
            healthSlider.value = healthPercent;
        }

        // Đảm bảo màu sắc đúng (nếu chưa được set)
        if (fillImage != null)
        {
            fillImage.color = healthColor; // Màu đỏ
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = damageColor; // Màu trắng cho phần đã mất
        }

        // Update text (UI Canvas)
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }

        // Update text (3D trên enemy)
        if (healthText3D != null)
        {
            healthText3D.text = $"{currentHealth}/{maxHealth}";
        }
    }

    /// <summary>
    /// Callback khi enemy chết
    /// </summary>
    private void OnEnemyDeath()
    {
        if (hideWhenDead)
        {
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        UnregisterHealthEvents();
    }

    /// <summary>
    /// Setup health bar cho enemy mới với NetworkEnemyHealth (gọi từ script khác)
    /// </summary>
    public void Setup(NetworkEnemyHealth health, Transform enemyTransform)
    {
        if (_setupDone && networkEnemyHealth == health && this.enemyTransform == enemyTransform)
            return;

        UnregisterHealthEvents();
        this.networkEnemyHealth = health;
        this.enemyHealth = null;
        this.enemyTransform = enemyTransform;

        // ⭐ DISABLED: Không set position - Giữ nguyên position từ prefab/scene
        // if (enemyTransform != null)
        // {
        //     Vector3 initialPosition = enemyTransform.position + offset;
        //     if (!float.IsNaN(initialPosition.x) && !float.IsInfinity(initialPosition.x))
        //     {
        //         transform.position = initialPosition;
        //     }
        // }

        if (networkEnemyHealth != null)
        {
            RegisterNetworkHealthEvents();
            UpdateHealthBar(networkEnemyHealth.GetCurrentHealth(), networkEnemyHealth.GetMaxHealth());
        }

        ApplyVisualDefaults();
        _setupDone = true;
    }

    /// <summary>
    /// Setup health bar cho enemy mới với EnemyHealth (fallback, không network)
    /// </summary>
    public void Setup(EnemyHealth health, Transform enemyTransform)
    {
        if (_setupDone && enemyHealth == health && this.enemyTransform == enemyTransform)
            return;

        UnregisterHealthEvents();
        this.enemyHealth = health;
        this.networkEnemyHealth = null;
        this.enemyTransform = enemyTransform;

        // ⭐ DISABLED: Không set position - Giữ nguyên position từ prefab/scene
        // if (enemyTransform != null)
        // {
        //     Vector3 initialPosition = enemyTransform.position + offset;
        //     if (!float.IsNaN(initialPosition.x) && !float.IsInfinity(initialPosition.x))
        //     {
        //         transform.position = initialPosition;
        //     }
        // }

        if (enemyHealth != null)
        {
            RegisterEnemyHealthEvents();
            UpdateHealthBar(enemyHealth.GetCurrentHealth(), enemyHealth.GetMaxHealth());
        }

        ApplyVisualDefaults();
        _setupDone = true;
    }
}
