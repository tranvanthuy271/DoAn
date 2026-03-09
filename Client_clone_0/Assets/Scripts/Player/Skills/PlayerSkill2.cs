using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerSkill2 : NetworkBehaviour
{
    [Header("Components")]
    private PlayerController controller;
    private Animator skillEffectAnimator; // Animator của SkillEffect (dùng cho skill2 animation)
    private Unity.Netcode.Components.NetworkAnimator skillEffectNetworkAnimator; // NetworkAnimator của SkillEffect (nếu có)
    private NetworkObject networkObject;

    [Header("Skill2 Settings")]
    [Tooltip("Phím để kích hoạt Skill2 (mặc định: K)")]
    [SerializeField] private KeyCode skill2Key = KeyCode.K;
    
    [Tooltip("Cooldown giữa các lần sử dụng skill2 (seconds)")]
    [SerializeField] private float skill2Cooldown = 2f;

    [Header("Projectile Settings")]
    [Tooltip("Prefab của quả cầu projectile")]
    [SerializeField] private GameObject projectilePrefab;
    
    [Tooltip("Tốc độ bay của projectile (units/second). Tăng giá trị này để projectile bay xa hơn")]
    [SerializeField] private float projectileSpeed = 10f;
    
    [Tooltip("Khoảng cách spawn projectile từ vị trí player")]
    [SerializeField] private float spawnOffset = 0.5f;
    
    [Tooltip("Thời gian sống của projectile (seconds). Projectile sẽ tự hủy sau thời gian này. Đặt 0 để không tự hủy")]
    [SerializeField] private float projectileLifetime = 3f;

    [Header("Animation")]
    [Tooltip("Tên Trigger trong Animator để phát animation Skill2. Nếu để trống sẽ dùng 'Skill2'")]
    [SerializeField] private string skill2TriggerName = "Skill2";

    [Header("Skill Effect")]
    [Tooltip("Object SkillEffect để hiển thị khi dùng skill2. Nếu để trống sẽ tự tìm child có tên 'SkillEffect'")]
    [SerializeField] private GameObject skillEffectObject;
    
    [Tooltip("Prefab SkillEffect để gắn vào projectile (nếu muốn animation di chuyển theo projectile). Để trống nếu muốn animation hiển thị ở player")]
    [SerializeField] private GameObject skillEffectPrefabForProjectile;
    
    [Tooltip("Nếu true, sẽ không trigger animation trên SkillEffect của player (chỉ hiển thị trên projectile)")]
    [SerializeField] private bool disablePlayerSkillEffectAnimation = false;

    [Header("Skill2 State")]
    private float cooldownTimer = 0f;
    private bool canUseSkill2 = true;
    private bool isUsingSkill2 = false;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        networkObject = GetComponent<NetworkObject>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        InitializeSkillEffect();
    }

    private void Update()
    {
        UpdateCooldown();
        HandleSkill2Input();
        EnsureNetworkAnimatorState(); // Đảm bảo NetworkAnimator state đúng
    }

    #region Initialization

    private void InitializeSkillEffect()
    {
        // Tìm SkillEffect object nếu chưa gán
        if (skillEffectObject == null)
        {
            Transform skillEffectTransform = transform.Find("SkillEffect");
            if (skillEffectTransform != null)
            {
                skillEffectObject = skillEffectTransform.gameObject;
            }
        }

        if (skillEffectObject == null)
        {
            Debug.LogWarning("[PlayerSkill2] Không tìm thấy SkillEffect object!");
            return;
        }

        // Tìm và kiểm tra Animator
        skillEffectAnimator = skillEffectObject.GetComponent<Animator>();
        if (skillEffectAnimator == null)
        {
            Debug.LogWarning("[PlayerSkill2] SkillEffect object không có Animator component!");
        }
        else if (skillEffectAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("[PlayerSkill2] SkillEffect Animator KHÔNG CÓ Controller được gán!");
        }

        // Tìm NetworkAnimator (nếu có) - cần disable khi SkillEffect inactive để tránh lỗi
        skillEffectNetworkAnimator = skillEffectObject.GetComponent<Unity.Netcode.Components.NetworkAnimator>();
        if (skillEffectNetworkAnimator != null)
        {
            // Disable NetworkAnimator nếu SkillEffect đang inactive để tránh lỗi
            if (!skillEffectObject.activeSelf)
            {
                skillEffectNetworkAnimator.enabled = false;
            }
        }
    }

    #endregion

    #region Update Methods

    /// <summary>
    /// Đảm bảo NetworkAnimator được enable/disable đúng với trạng thái của SkillEffect
    /// </summary>
    private void EnsureNetworkAnimatorState()
    {
        if (skillEffectNetworkAnimator != null && skillEffectObject != null)
        {
            if (skillEffectObject.activeSelf && !skillEffectNetworkAnimator.enabled)
            {
                skillEffectNetworkAnimator.enabled = true;
            }
            else if (!skillEffectObject.activeSelf && skillEffectNetworkAnimator.enabled)
            {
                skillEffectNetworkAnimator.enabled = false;
            }
        }
    }

    private void UpdateCooldown()
    {
        // Chỉ owner mới xử lý cooldown
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return;
        }

        if (!canUseSkill2)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                canUseSkill2 = true;
            }
        }
    }

    private void HandleSkill2Input()
    {
        // Chỉ owner mới xử lý input
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return;
        }

        if (Input.GetKeyDown(skill2Key) && canUseSkill2 && !isUsingSkill2)
        {
            UseSkill2();
        }
    }

    #endregion

    #region Skill2 Logic

    private void UseSkill2()
    {
        if (!canUseSkill2 || isUsingSkill2) return;

        // Gửi lên server để đồng bộ
        if (networkObject != null && NetworkManager.Singleton != null)
        {
            if (networkObject.IsOwner)
            {
                UseSkill2ServerRpc();
            }
        }
        else
        {
            UseSkill2Local();
        }
    }

    [ServerRpc]
    private void UseSkill2ServerRpc()
    {
        UseSkill2ClientRpc();
    }

    [ClientRpc]
    private void UseSkill2ClientRpc()
    {
        UseSkill2Local();
    }

    private void UseSkill2Local()
    {
        // Set skill2 state
        isUsingSkill2 = true;
        canUseSkill2 = false;
        cooldownTimer = skill2Cooldown;

        // Validate SkillEffect
        if (skillEffectObject == null)
        {
            Debug.LogError("[PlayerSkill2] SkillEffect object là NULL!");
            isUsingSkill2 = false;
            return;
        }

        // Ensure SkillEffect is active
        if (!skillEffectObject.activeSelf)
        {
            skillEffectObject.SetActive(true);
        }

        // Enable NetworkAnimator nếu có (sau khi SetActive(true))
        if (skillEffectNetworkAnimator != null && !skillEffectNetworkAnimator.enabled)
        {
            skillEffectNetworkAnimator.enabled = true;
        }

        // Get Animator if needed
        if (skillEffectAnimator == null)
        {
            skillEffectAnimator = skillEffectObject.GetComponent<Animator>();
            if (skillEffectAnimator == null)
            {
                Debug.LogError("[PlayerSkill2] Không tìm thấy Animator component trên SkillEffect object!");
                isUsingSkill2 = false;
                return;
            }
        }

        // Get NetworkAnimator if needed
        if (skillEffectNetworkAnimator == null)
        {
            skillEffectNetworkAnimator = skillEffectObject.GetComponent<Unity.Netcode.Components.NetworkAnimator>();
            if (skillEffectNetworkAnimator != null)
            {
                skillEffectNetworkAnimator.enabled = true;
            }
        }

        // Trigger animation trên player SkillEffect (nếu không disable)
        if (!disablePlayerSkillEffectAnimation)
        {
            TriggerSkill2Animation();
        }

        // Spawn projectile
        SpawnProjectile();

        // Reset skill2 state sau một khoảng thời gian ngắn (để animation chạy)
        // Animation sẽ tự động quay về Empty state khi kết thúc
        Invoke(nameof(ResetSkill2State), 0.1f);

        // Ẩn SkillEffect sau khi animation kết thúc
        // Lấy độ dài của animation "skill2" từ Animator Controller
        float animationLength = GetAnimationLength("skill2");
        if (animationLength > 0)
        {
            Invoke(nameof(HideSkillEffect), animationLength);
        }
        else
        {
            // Fallback nếu không tìm thấy độ dài animation
            Invoke(nameof(HideSkillEffect), 0.5f); // Mặc định 0.5 giây
        }
    }

    private float GetAnimationLength(string animationName)
    {
        if (skillEffectAnimator == null || skillEffectAnimator.runtimeAnimatorController == null)
        {
            return 0f;
        }

        foreach (AnimationClip clip in skillEffectAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length;
            }
        }
        return 0f;
    }

    private void TriggerSkill2Animation(string triggerName = null)
    {
        string triggerToUse = triggerName ?? skill2TriggerName;

        if (skillEffectAnimator == null || string.IsNullOrEmpty(triggerToUse))
        {
            if (skillEffectAnimator == null)
            {
                Debug.LogError("[PlayerSkill2] skillEffectAnimator là NULL!");
            }
            return;
        }

        if (skillEffectAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("[PlayerSkill2] SkillEffect Animator KHÔNG CÓ Controller!");
            return;
        }

        // Kiểm tra parameter có tồn tại không
        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in skillEffectAnimator.parameters)
        {
            if (param.name == triggerToUse && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasParameter = true;
                break;
            }
        }

        if (!hasParameter)
        {
            Debug.LogError($"[PlayerSkill2] Parameter '{triggerToUse}' KHÔNG TỒN TẠI trong Animator Controller!");
            return;
        }

        // Enable và trigger animation
        if (!skillEffectAnimator.enabled)
        {
            skillEffectAnimator.enabled = true;
        }

        skillEffectAnimator.SetTrigger(triggerToUse);
    }

    private void SpawnProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("[PlayerSkill2] Projectile prefab chưa được gán!");
            return;
        }

        // Xác định hướng player đang nhìn
        bool facingRight = transform.localScale.x >= 0f;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;

        // Tính vị trí spawn (từ vị trí player + offset)
        Vector3 spawnPosition = transform.position + new Vector3(
            facingRight ? spawnOffset : -spawnOffset,
            0f,
            0f
        );

        // Spawn projectile
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);

        // Trigger animation trực tiếp trên projectile nếu có Animator component
        Animator projectileAnimator = projectile.GetComponent<Animator>();
        if (projectileAnimator != null)
        {
            // QUAN TRỌNG: Tắt Apply Root Motion để animation không override transform của projectile
            // Animation chỉ để hiển thị visual, không ảnh hưởng đến movement
            projectileAnimator.applyRootMotion = false;
            
            // Đảm bảo Animator enabled và có controller
            if (!projectileAnimator.enabled)
            {
                projectileAnimator.enabled = true;
            }
            
            // Kiểm tra controller đã được gán chưa
            if (projectileAnimator.runtimeAnimatorController == null)
            {
                Debug.LogWarning("[PlayerSkill2] Projectile có Animator nhưng chưa có Controller được gán! Vui lòng gán SkillEffect.controller vào Animator component của Projectile Prefab.");
            }
            else
            {
                // Kiểm tra xem có parameter "Skill2" không
                bool hasSkill2Parameter = false;
                foreach (AnimatorControllerParameter param in projectileAnimator.parameters)
                {
                    if (param.name == skill2TriggerName && param.type == AnimatorControllerParameterType.Trigger)
                    {
                        hasSkill2Parameter = true;
                        break;
                    }
                }
                
                if (hasSkill2Parameter)
                {
                    // Đảm bảo Animator đã được update ít nhất 1 frame trước khi trigger
                    // Sử dụng coroutine hoặc delay nhỏ để đảm bảo Animator sẵn sàng
                    StartCoroutine(TriggerProjectileAnimationDelayed(projectileAnimator, skill2TriggerName));
                }
                else
                {
                    Debug.LogWarning($"[PlayerSkill2] Projectile có Animator và Controller nhưng không có parameter '{skill2TriggerName}' (Trigger)! Kiểm tra lại Animator Controller.");
                }
            }
        }

        // Spawn SkillEffect instance gắn vào projectile (nếu có prefab)
        if (skillEffectPrefabForProjectile != null)
        {
            GameObject projectileSkillEffect = Instantiate(skillEffectPrefabForProjectile, projectile.transform);
            projectileSkillEffect.transform.localPosition = Vector3.zero;
            
            // Trigger animation skill2 trên SkillEffect của projectile
            Animator projectileSkillEffectAnimator = projectileSkillEffect.GetComponent<Animator>();
            if (projectileSkillEffectAnimator != null)
            {
                // Đảm bảo SkillEffect active
                if (!projectileSkillEffect.activeSelf)
                {
                    projectileSkillEffect.SetActive(true);
                }
                
                // Trigger animation
                if (projectileSkillEffectAnimator.runtimeAnimatorController != null)
                {
                    bool hasParameter = false;
                    foreach (AnimatorControllerParameter param in projectileSkillEffectAnimator.parameters)
                    {
                        if (param.name == skill2TriggerName && param.type == AnimatorControllerParameterType.Trigger)
                        {
                            hasParameter = true;
                            break;
                        }
                    }
                    
                    if (hasParameter)
                    {
                        projectileSkillEffectAnimator.SetTrigger(skill2TriggerName);
                    }
                }
            }
        }

        // QUAN TRỌNG: Đảm bảo Rigidbody2D được cấu hình đúng cho projectile
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb == null)
        {
            // Nếu không có Rigidbody2D, thêm vào
            projectileRb = projectile.AddComponent<Rigidbody2D>();
            Debug.LogWarning("[PlayerSkill2] Projectile không có Rigidbody2D, đã tự động thêm vào. Vui lòng cấu hình Rigidbody2D trong Prefab!");
        }
        
        if (projectileRb != null)
        {
            // Đảm bảo Rigidbody2D không bị freeze position X (để projectile có thể di chuyển)
            if (projectileRb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionX))
            {
                projectileRb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
                Debug.LogWarning("[PlayerSkill2] Rigidbody2D của projectile bị freeze position X, đã tự động bỏ freeze!");
            }
            
            // Đảm bảo Rigidbody2D là Kinematic hoặc Dynamic (không phải Static)
            if (projectileRb.bodyType == RigidbodyType2D.Static)
            {
                projectileRb.bodyType = RigidbodyType2D.Dynamic;
                Debug.LogWarning("[PlayerSkill2] Rigidbody2D của projectile là Static, đã đổi sang Dynamic!");
            }
            
            // Set gravity scale = 0 để projectile bay ngang (không rơi)
            projectileRb.gravityScale = 0f;
        }

        // Tìm hoặc thêm ProjectileMovement script để đảm bảo projectile di chuyển đúng
        ProjectileMovement projectileMovement = projectile.GetComponent<ProjectileMovement>();
        if (projectileMovement == null)
        {
            projectileMovement = projectile.AddComponent<ProjectileMovement>();
        }

        // Set tốc độ và hướng cho projectile
        if (projectileMovement != null)
        {
            projectileMovement.SetMovement(projectileSpeed, direction.x);
            // Set lifetime nếu có
            if (projectileLifetime > 0f)
            {
                projectileMovement.SetLifetime(projectileLifetime);
            }
        }
        else
        {
            // Fallback: Set velocity trực tiếp nếu không có ProjectileMovement
            if (projectileRb != null)
            {
                projectileRb.velocity = direction * projectileSpeed;
            }
            else
            {
                Debug.LogError("[PlayerSkill2] Không thể setup movement cho projectile! Thiếu cả ProjectileMovement và Rigidbody2D!");
            }

            // Tự động hủy projectile sau một khoảng thời gian (nếu có set lifetime)
            if (projectileLifetime > 0f)
            {
                Destroy(projectile, projectileLifetime);
            }
        }

        // Flip sprite của projectile nếu cần
        SpriteRenderer projectileSprite = projectile.GetComponent<SpriteRenderer>();
        if (projectileSprite != null && !facingRight)
        {
            projectileSprite.flipX = true;
        }
    }

    private void ResetSkill2State()
    {
        isUsingSkill2 = false;
        
        // Disable NetworkAnimator khi SkillEffect sắp inactive để tránh lỗi
        // Lưu ý: Không SetActive(false) ngay vì animation có thể chưa kết thúc
        // Animation sẽ tự động quay về Empty state, sau đó có thể ẩn SkillEffect nếu cần
    }

    /// <summary>
    /// Gọi khi muốn ẩn SkillEffect (sau khi animation kết thúc)
    /// Disable NetworkAnimator trước khi SetActive(false) để tránh lỗi
    /// </summary>
    private void HideSkillEffect()
    {
        if (skillEffectObject == null) return;

        // Reset animation về Empty state trước khi ẩn
        if (skillEffectAnimator != null && skillEffectAnimator.runtimeAnimatorController != null)
        {
            try
            {
                skillEffectAnimator.ResetTrigger(skill2TriggerName);
                skillEffectAnimator.Play("Empty", 0, 0f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerSkill2] Không thể reset animation: {e.Message}");
            }
        }

        // Disable NetworkAnimator trước khi SetActive(false)
        if (skillEffectNetworkAnimator != null && skillEffectNetworkAnimator.enabled)
        {
            skillEffectNetworkAnimator.enabled = false;
        }

        // Ẩn SkillEffect
        skillEffectObject.SetActive(false);
    }

    #endregion

    #region Public API

    public bool IsUsingSkill2() => isUsingSkill2;
    public bool CanUseSkill2() => canUseSkill2;
    public float GetCooldownPercent() => canUseSkill2 ? 1f : Mathf.Clamp01(1f - (cooldownTimer / skill2Cooldown));

    #endregion

    #region Coroutines

    /// <summary>
    /// Trigger animation trên projectile sau một frame delay để đảm bảo Animator đã sẵn sàng
    /// </summary>
    private IEnumerator TriggerProjectileAnimationDelayed(Animator animator, string triggerName)
    {
        // Đợi một frame để đảm bảo Animator đã được khởi tạo đầy đủ
        yield return null;
        
        if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
        {
            try
            {
                animator.SetTrigger(triggerName);
                Debug.Log($"[PlayerSkill2] Đã trigger animation '{triggerName}' trên projectile!");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PlayerSkill2] Lỗi khi trigger animation trên projectile: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerSkill2] Không thể trigger animation trên projectile: Animator không hợp lệ!");
        }
    }

    #endregion
}
