using UnityEngine;
using UnityEngine.UI;

public class FlightMeter : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider flightSlider;
    [SerializeField] private Image fillImage;
    [SerializeField] private Text statusText;

    [Header("Colors")]
    [SerializeField] private Color availableColor = Color.cyan;
    [SerializeField] private Color depletedColor = Color.gray;
    [SerializeField] private Color cooldownColor = Color.yellow;

    [Header("Target")]
    [SerializeField] private PlayerController playerController;
    private PlayerMovement playerMovement;
    private PlayerStats stats;

    private void Start()
    {
        // Auto-find player if not assigned
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (playerController != null)
        {
            playerMovement = playerController.GetMovement();
            stats = playerController.stats;
        }

        // Setup slider
        if (flightSlider != null)
        {
            flightSlider.minValue = 0;
            flightSlider.maxValue = 1;
        }
    }

    private void Update()
    {
        if (playerMovement == null || stats == null) return;

        // For now, we'll add public properties to PlayerMovement to access flight state
        // This is a simplified version - you may want to refactor PlayerMovement
        // to expose flight time and cooldown

        UpdateFlightMeter();
    }

    private void UpdateFlightMeter()
    {
        float flightPercent = playerMovement.GetFlightPercent();
        bool isFlying = playerMovement.IsFlying();
        bool canFly = playerMovement.CanFly();

        if (flightSlider != null)
        {
            flightSlider.value = flightPercent;
        }

        if (fillImage != null)
        {
            if (playerController.unlimitedFlight)
            {
                fillImage.color = availableColor;
            }
            else if (isFlying)
            {
                fillImage.color = availableColor;
            }
            else if (!canFly)
            {
                fillImage.color = cooldownColor;
            }
            else
            {
                fillImage.color = depletedColor;
            }
        }

        if (statusText != null)
        {
            if (playerController.unlimitedFlight)
            {
                statusText.text = "UNLIMITED";
            }
            else if (isFlying)
            {
                statusText.text = $"FLYING {(flightPercent * 100):F0}%";
            }
            else if (!canFly)
            {
                statusText.text = $"COOLDOWN {playerMovement.GetFlightCooldown():F1}s";
            }
            else
            {
                statusText.text = "READY";
            }
        }
    }
}

