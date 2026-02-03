using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text healthText; // Legacy Text (deprecated)
    [SerializeField] private TextMeshProUGUI healthTextTMP; // TextMeshPro (recommended)

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Header("Target")]
    [SerializeField] private PlayerHealth playerHealth; // Local health (single-player)
    [SerializeField] private NetworkPlayerHealth networkPlayerHealth; // Network health (multiplayer)

    private void Start()
    {
        // Auto-find player health if not assigned (ưu tiên NetworkPlayerHealth)
        if (networkPlayerHealth == null)
        {
            networkPlayerHealth = FindObjectOfType<NetworkPlayerHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (networkPlayerHealth != null)
        {
            networkPlayerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
            UpdateHealthBar(networkPlayerHealth.GetCurrentHealth(), networkPlayerHealth.GetMaxHealth());
        }
        else if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
            UpdateHealthBar(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }

        // Setup slider if exists
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = 1;
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        float healthPercent = (float)currentHealth / maxHealth;

        // Update slider
        if (healthSlider != null)
        {
            healthSlider.value = healthPercent;
        }

        // Update color
        if (fillImage != null)
        {
            fillImage.color = Color.Lerp(lowHealthColor, fullHealthColor, 
                (healthPercent - 0) / (1 - lowHealthThreshold));
        }

        // Update text
        if (healthTextTMP != null)
        {
            healthTextTMP.text = $"{currentHealth} / {maxHealth}";
        }
        else if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (networkPlayerHealth != null)
        {
            networkPlayerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }
    }
}

