using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text healthText; // Optional

    [Header("Colors")]
    [SerializeField] private Color fullHealthColor = Color.green;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] private float lowHealthThreshold = 0.3f;

    [Header("Target")]
    [SerializeField] private PlayerHealth playerHealth;

    private void Start()
    {
        // Auto-find player health if not assigned
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            // Subscribe to health changed event
            playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);

            // Initialize
            UpdateHealthBar(playerHealth.GetCurrentHealth(), playerHealth.GetMaxHealth());
        }
        else
        {
            Debug.LogWarning("PlayerHealth not found!");
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
        if (healthText != null)
        {
            healthText.text = $"{currentHealth} / {maxHealth}";
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from event
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
        }
    }
}

