using UnityEngine;

/// <summary>
/// Class chứa thông tin của một skill projectile
/// Mỗi skill có thể có projectile prefab, key, cooldown, animation riêng
/// </summary>
[System.Serializable]
public class SkillData
{
    [Header("Skill Info")]
    [Tooltip("Tên skill (để dễ quản lý)")]
    public string skillName = "New Skill";
    
    [Tooltip("Phím để kích hoạt skill")]
    public KeyCode activationKey = KeyCode.K;
    
    [Tooltip("Cooldown giữa các lần sử dụng skill (seconds)")]
    public float cooldown = 2f;
    
    [Header("Projectile Settings")]
    [Tooltip("Prefab của projectile cho skill này")]
    public GameObject projectilePrefab;
    
    [Tooltip("Tốc độ bay của projectile (units/second)")]
    public float projectileSpeed = 10f;
    
    [Tooltip("Khoảng cách spawn projectile từ vị trí player")]
    public float spawnOffset = 0.5f;
    
    [Tooltip("Thời gian sống của projectile (seconds). Đặt 0 để không tự hủy")]
    public float projectileLifetime = 3f;
    
    [Header("Animation Settings")]
    [Tooltip("Tên Trigger trong Animator để phát animation. Nếu để trống sẽ không trigger animation")]
    public string animationTriggerName = "";
    
    [Tooltip("Object SkillEffect để hiển thị animation trên player (nếu có). Để trống nếu không cần")]
    public GameObject playerSkillEffectObject;
    
    [Tooltip("Prefab SkillEffect để gắn vào projectile (nếu muốn animation di chuyển theo projectile). Để trống nếu không cần")]
    public GameObject projectileSkillEffectPrefab;
    
    [Tooltip("Nếu true, sẽ không trigger animation trên SkillEffect của player")]
    public bool disablePlayerSkillEffectAnimation = false;
    
    [Header("Internal State (Không chỉnh sửa)")]
    [SerializeField] private float cooldownTimer = 0f;
    [SerializeField] private bool canUse = true;
    [SerializeField] private bool isUsing = false;
    
    /// <summary>
    /// Kiểm tra xem skill có thể sử dụng không
    /// </summary>
    public bool CanUse() => canUse;
    
    /// <summary>
    /// Kiểm tra xem skill đang được sử dụng không
    /// </summary>
    public bool IsUsing() => isUsing;
    
    /// <summary>
    /// Lấy phần trăm cooldown (0 = đang cooldown, 1 = sẵn sàng)
    /// </summary>
    public float GetCooldownPercent() => canUse ? 1f : Mathf.Clamp01(1f - (cooldownTimer / cooldown));
    
    /// <summary>
    /// Update cooldown timer
    /// </summary>
    public void UpdateCooldown(float deltaTime)
    {
        if (!canUse)
        {
            cooldownTimer -= deltaTime;
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                canUse = true;
            }
        }
    }
    
    /// <summary>
    /// Bắt đầu sử dụng skill
    /// </summary>
    public void StartUsing()
    {
        isUsing = true;
        canUse = false;
        cooldownTimer = cooldown;
    }
    
    /// <summary>
    /// Kết thúc sử dụng skill
    /// </summary>
    public void StopUsing()
    {
        isUsing = false;
    }
    
    /// <summary>
    /// Reset skill state
    /// </summary>
    public void Reset()
    {
        isUsing = false;
        canUse = true;
        cooldownTimer = 0f;
    }
}
