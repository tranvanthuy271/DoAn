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

    // Kim Shield: invincibility vô hạn (đến khi DeactivateShield() được gọi)
    private bool isShieldActive = false;

    // Thủy Armor Buff: hấp thụ một lượng sát thương (reset sau khi hết thời gian)
    private int temporaryArmor = 0;
    private float armorBuffTimer = 0f;

    // Thổ Attack Buff: tăng sát thương tấn công theo %
    private int attackBonusPercent = 0;
    private float attackBuffTimer = 0f;

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

        // Update armor buff timer
        if (armorBuffTimer > 0f)
        {
            armorBuffTimer -= Time.deltaTime;
            if (armorBuffTimer <= 0f)
            {
                temporaryArmor = 0;
            }
        }

        // Update attack buff timer
        if (attackBuffTimer > 0f)
        {
            attackBuffTimer -= Time.deltaTime;
            if (attackBuffTimer <= 0f)
            {
                attackBonusPercent = 0;
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

        // Kim Shield — bất tử hoàn toàn
        if (isShieldActive)
        {
            Debug.Log("[PlayerHealth] Shield active — damage blocked!");
            return;
        }

        // Invincibility prevents damage
        if (isInvincible)
        {
            return;
        }

        // Thủy Armor Buff — hấp thụ một phần sát thương
        if (temporaryArmor > 0)
        {
            int absorbed = Mathf.Min(temporaryArmor, damage);
            temporaryArmor -= absorbed;
            damage -= absorbed;
            if (damage <= 0) return;
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

        // Kích hoạt animation die
        var playerAnimator = GetComponent<PlayerAnimator>();
        if (playerAnimator != null)
            playerAnimator.SetDead(true);

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

    /// <summary>Kích hoạt Khiên Kim — bất tử cho đến khi DeactivateShield() được gọi.</summary>
    public void ActivateShield() => isShieldActive = true;

    /// <summary>Tắt Khiên Kim sau khi hết thời gian.</summary>
    public void DeactivateShield() => isShieldActive = false;

    /// <summary>Khiên Kim đang bật không?  Dùng để MetalShieldSkill kiểm tra.</summary>
    public bool IsShieldActive() => isShieldActive;

    /// <summary>Áp dụng buff giáp tạm thời (Thủy skill 3). Server gọi trực tiếp.</summary>
    public void ApplyArmorBuff(int armorValue, float duration)
    {
        temporaryArmor += armorValue;   // cộng dồn nếu buff nhiều lần
        armorBuffTimer = Mathf.Max(armorBuffTimer, duration);
    }

    /// <summary>Lấy giáp buff hiện tại (để hiển thị UI nếu cần).</summary>
    public int GetTemporaryArmor() => temporaryArmor;

    /// <summary>Áp dụng buff tấn công tạm thời (Thổ skill 1). Server gọi trực tiếp.</summary>
    public void ApplyAttackBuff(int bonusPercent, float duration)
    {
        attackBonusPercent = Mathf.Max(attackBonusPercent, bonusPercent); // lấy giá trị cao nhất
        attackBuffTimer = Mathf.Max(attackBuffTimer, duration);
    }

    /// <summary>Lấy % buff tấn công hiện tại (để FireballDamage áp dụng).</summary>
    public int GetAttackBonusPercent() => attackBonusPercent;
}

