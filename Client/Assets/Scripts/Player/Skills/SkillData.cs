using UnityEngine;

/// <summary>Loại skill — dùng để phân biệt cách kích hoạt trong PlayerSkillManager.</summary>
public enum SkillType
{
    Projectile,    // Bắn đạn theo hướng player
    Teleport,      // Dịch chuyển tức thời (delegate sang TeleportSkill)
    Melee,         // Cận chiến: chỉ trigger animation tại vị trí player, không spawn projectile
    WindStep,      // Ẩn thân + animation + dash (delegate sang WindStepSkill)
    MetalShield,   // Khiên bất tử: miễn nhiễm mọi sát thương + xóa projectile chạm vào (delegate sang MetalShieldSkill)
    WaterPillar,   // Cây thánh từ trên rơi xuống: spawn projectile rơi từ trên không (delegate sang WaterPillarSkill)
    WaterArmorBuff, // Buff giáp thủy: tăng giáp tạm thời cho bản thân và đồng đội xung quanh (delegate sang WaterArmorBuffSkill)
    FireRain,       // Thiên Hỏa: mưa lửa từ trên trời rơi xuống (delegate sang FireRainSkill)
    EarthAura,      // Địa Uy Khí: buff tấn công cho bản thân và đồng đội xung quanh (delegate sang EarthAttackBuffSkill)
    EarthBoomerang, // Địa Phong Đao: bắn đạn boomerang quay về (delegate sang EarthBoomerangSkill)
    EarthBlinkStrike, // Địa Độn Thuật: dịch chuyển + DoT projectile (delegate sang EarthBlinkStrikeSkill)
    HybridBarrage,   // Kim Phong Liên Tiễn: bắn 5 đạn nhỏ theo ngang (delegate sang HybridMetalWindBarrageSkill)
    HybridLavaAura,  // Hỏa Thổ Dung Nham: dung nham bao quanh player, DoT + chặn hồi HP (delegate sang HybridFireEarthLavaAuraSkill)
    HybridVenom      // Băng Độc Vĩnh Cửu (Water + Wood): hồ nước độc, Slow + DoT + giảm ATK (delegate sang HybridWaterWoodVenomSkill)
}

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

    [Tooltip("Mã skill khớp với skill_code trong DB (VD: WIND_STRIKE). Dùng để load thống kê từ server.")]
    public string skillCode = "";

    [Tooltip("Loại skill: Projectile (bắn đạn) hay Teleport (dịch chuyển)")]
    public SkillType skillType = SkillType.Projectile;
    
    [Tooltip("Phím để kích hoạt skill")]
    public KeyCode activationKey = KeyCode.K;
    
    [Tooltip("Cooldown giữa các lần sử dụng skill (seconds) — sẽ bị ghi đè bởi DB khi StartHost")]
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

    [Tooltip("Bật nếu sprite gốc của projectile đang quay sang trái. Hệ thống sẽ flip lại theo hướng player khi bắn.")]
    public bool projectileSpriteFacesLeft = false;
    
    [Tooltip("Nếu true, sẽ không trigger animation trên SkillEffect của player")]
    public bool disablePlayerSkillEffectAnimation = false;

    [Header("Runtime Stats (load từ DB — không chỉnh tay)")]
    [Tooltip("Sát thương / hiệu ứng tại level hiện tại. Được set bởi SkillRuntimeLoader sau StartHost.")]
    public float currentEffectValue = 0f;

    [Tooltip("MP tiêu tốn khi dùng skill tại level hiện tại. Được set bởi SkillRuntimeLoader sau StartHost.")]
    public int currentMpCost = 0;
    
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
    /// Lấy thời gian cooldown còn lại (giây). Trả về 0 nếu skill sẵn sàng.
    /// </summary>
    public float GetCooldownRemaining() => canUse ? 0f : Mathf.Max(0f, cooldownTimer);
    
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
