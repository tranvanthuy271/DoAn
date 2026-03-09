using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    private PlayerController controller;
    private int currentHealth;
    private int maxHealth;

    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1f;
    private float invincibilityTimer;
    private bool isInvincible;

    [Header("Events")]
    public UnityEvent<int, int> OnHealthChanged; // current, max
    public UnityEvent OnDeath;
    public UnityEvent OnTakeDamage;
    public UnityEvent OnHeal;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        if (controller != null && controller.stats != null)
        {
            maxHealth = controller.stats.maxHealth;
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    private void Update()
    {
        // Update invincibility timer
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        // God mode prevents damage
        if (controller != null && controller.godMode)
        {
            Debug.Log("God Mode: Damage blocked!");
            return;
        }

        // Invincibility prevents damage
        if (isInvincible)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnTakeDamage?.Invoke();

        Debug.Log($"Player took {damage} damage. Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // Start invincibility frames
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
        }
    }

    public void Heal(int amount)
    {
        if (currentHealth >= maxHealth)
        {
            return;
        }

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHeal?.Invoke();

        Debug.Log($"Player healed {amount}. Health: {currentHealth}/{maxHealth}");
    }

    public void HealFull()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHeal?.Invoke();
    }

    private void Die()
    {
        Debug.Log("Player died!");
        OnDeath?.Invoke();

        // Notify GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercent() => (float)currentHealth / maxHealth;
    public bool IsInvincible() => isInvincible;
}

