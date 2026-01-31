using UnityEngine;
using Unity.Netcode;

public class PlayerDash : NetworkBehaviour
{
    [Header("Components")]
    private PlayerController controller;
    private Rigidbody2D rb;
    private Animator animator; // Animator của player (không dùng cho Dash)
    private Animator skillEffectAnimator; // Animator của SkillEffect (dùng cho Dash animation)
    private NetworkObject networkObject;

    [Header("Dash Settings")]
    [Tooltip("Phím để kích hoạt Dash (mặc định: LeftShift)")]
    [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
    
    [Tooltip("Tốc độ dash (units/second)")]
    [SerializeField] private float dashSpeed = 15f;
    
    [Tooltip("Thời gian dash (seconds)")]
    [SerializeField] private float dashDuration = 0.3f;
    
    [Tooltip("Cooldown giữa các lần dash (seconds)")]
    [SerializeField] private float dashCooldown = 1f;

    [Header("Animation")]
    [Tooltip("Tên Trigger trong Animator để phát animation Dash. Nếu để trống sẽ dùng 'Dash'")]
    [SerializeField] private string dashTriggerName = "Dash";
    
    [Tooltip("Tên Trigger cho animation Dash hướng phải (tùy chọn). Nếu để trống sẽ dùng dashTriggerName")]
    [SerializeField] private string dashRightTriggerName = "";
    
    [Tooltip("Tên Trigger cho animation Dash hướng trái (tùy chọn). Nếu để trống sẽ dùng dashTriggerName")]
    [SerializeField] private string dashLeftTriggerName = "";

    [Header("Skill Effect")]
    [Tooltip("Object SkillEffect để hiển thị khi dash. Nếu để trống sẽ tự tìm child có tên 'SkillEffect'")]
    [SerializeField] private GameObject skillEffectObject;

    [Header("Dash State")]
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private bool canDash = true;
    private Vector2 dashDirection = Vector2.right;
    private bool justStartedDash = false;
    private float timeSinceDashEnded = 0f;
    private bool isDashRequested = false;
    private float lastScaleX = 1f;

    // Constants
    private const float SKILL_EFFECT_POSITION_OFFSET_RIGHT = 1f; // Position offset cho bên phải
    private const float SKILL_EFFECT_POSITION_OFFSET_LEFT = -1f; // Position offset cho bên trái
    private const float SCALE_THRESHOLD = 0.01f;
    private const float DASH_END_COOLDOWN = 0.1f;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        networkObject = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        InitializeSkillEffect();
    }

    private void Update()
    {
        UpdateFlags();
        UpdateDashTimer();
        UpdateSkillEffectDirection();
        UpdatePlayerScaleTracking();
        UpdateCooldown();
        HandleDashInput();
    }

    private void FixedUpdate()
    {
        if (isDashing && rb != null)
        {
            rb.velocity = dashDirection * dashSpeed;
        }
    }

    private void LateUpdate()
    {
        if (isDashing && skillEffectObject != null && skillEffectObject.activeSelf)
        {
            EnsureSkillEffectTransform();
        }
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

        if (skillEffectObject == null) return;

        // Tìm và kiểm tra Animator
        skillEffectAnimator = skillEffectObject.GetComponent<Animator>();
        if (skillEffectAnimator == null)
        {
            Debug.LogWarning("[PlayerDash] SkillEffect object không có Animator component!");
        }
        else if (skillEffectAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("[PlayerDash] SkillEffect Animator KHÔNG CÓ Controller được gán!");
        }

        // Khởi tạo position và scale của SkillEffect
        UpdateSkillEffectDirection();
        lastScaleX = transform.localScale.x;

        // Ẩn SkillEffect ban đầu
        skillEffectObject.SetActive(false);
    }

    #endregion

    #region Update Methods

    private void UpdateFlags()
    {
        // Reset justStartedDash flag
        if (justStartedDash)
        {
            justStartedDash = false;
        }

        // Reset isDashRequested nếu đã quá lâu (fallback safety)
        if (isDashRequested && !isDashing && timeSinceDashEnded > DASH_END_COOLDOWN)
        {
            isDashRequested = false;
        }

        // Update timeSinceDashEnded
        if (!isDashing && timeSinceDashEnded >= 0f)
        {
            timeSinceDashEnded += Time.deltaTime;
        }
        else if (isDashing)
        {
            timeSinceDashEnded = 0f;
        }
    }

    private void UpdateDashTimer()
    {
        if (!isDashing) return;

        dashTimer -= Time.deltaTime;
        if (dashTimer <= 0f)
        {
            if (networkObject != null && NetworkManager.Singleton != null)
            {
                if (networkObject.IsOwner)
                {
                    StopDashServerRpc();
                }
            }
            else
            {
                StopDash();
            }
        }
    }

    private void UpdateSkillEffectDirection()
    {
        // Chỉ update khi không đang dash và SkillEffect không active
        if (skillEffectObject == null || isDashing || skillEffectObject.activeSelf) return;
        if (justStartedDash || timeSinceDashEnded <= DASH_END_COOLDOWN || isDashRequested) return;

        float currentScaleX = transform.localScale.x;
        if (Mathf.Abs(currentScaleX - lastScaleX) > SCALE_THRESHOLD)
        {
            bool facingRight = currentScaleX >= 0f;
            SetSkillEffectTransform(facingRight);
            lastScaleX = currentScaleX;
        }
    }

    private void UpdatePlayerScaleTracking()
    {
        // Cập nhật lastScaleX khi đang dash để tránh false positive sau khi dash xong
        if (skillEffectObject != null && isDashing)
        {
            float currentScaleX = transform.localScale.x;
            if (Mathf.Abs(currentScaleX - lastScaleX) > SCALE_THRESHOLD)
            {
                lastScaleX = currentScaleX;
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

        if (!canDash)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                canDash = true;
            }
        }
    }

    private void HandleDashInput()
    {
        // Chỉ owner mới xử lý input
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return;
        }

        if (Input.GetKeyDown(dashKey) && canDash && !isDashing)
        {
            Dash();
        }
    }

    #endregion

    #region SkillEffect Transform

    /// <summary>
    /// Set SkillEffect transform (scale và position) dựa trên hướng player
    /// </summary>
    private void SetSkillEffectTransform(bool facingRight)
    {
        if (skillEffectObject == null) return;

        // Set scale: Animation được thiết kế cho hướng PHẢI
        // Khi player hướng phải → scale.x = 1 (giữ nguyên animation phải)
        // Khi player hướng trái → scale.x = -1 (flip animation phải thành trái)
        // Vector3 scale = skillEffectObject.transform.localScale;
        // scale.x = facingRight ? 1f : -1f;
        // skillEffectObject.transform.localScale = scale;

        // Set position: bên phải = 2, bên trái = -2  
        // Vector3 localPos = skillEffectObject.transform.localPosition;
        // localPos.x = facingRight ? SKILL_EFFECT_POSITION_OFFSET_RIGHT : SKILL_EFFECT_POSITION_OFFSET_LEFT;
        // skillEffectObject.transform.localPosition = localPos;

        // Đảm bảo SpriteRenderer.flipX = false (dùng scale.x để flip)
        // SpriteRenderer spriteRenderer = skillEffectObject.GetComponent<SpriteRenderer>();
        // if (spriteRenderer != null && spriteRenderer.flipX)
        // {
        //     spriteRenderer.flipX = false;
        // }
    }


    /// <summary>
    /// Đảm bảo SkillEffect transform không bị animation override (trong LateUpdate)
    /// </summary>
    private void EnsureSkillEffectTransform()
    {
        if (skillEffectObject == null) return;

        bool facingRight = dashDirection.x >= 0f;
        // Animation được thiết kế cho hướng PHẢI: phải = 1, trái = -1
        float expectedScaleX = facingRight ? 1f : -1f;
        float expectedPosX = facingRight ? SKILL_EFFECT_POSITION_OFFSET_RIGHT : SKILL_EFFECT_POSITION_OFFSET_LEFT;

        // Fix scale nếu bị override
        // float currentScaleX = skillEffectObject.transform.localScale.x;
        // if (Mathf.Abs(currentScaleX - expectedScaleX) > SCALE_THRESHOLD)
        // {
        //     Vector3 scale = skillEffectObject.transform.localScale;
        //     scale.x = expectedScaleX;
        //     skillEffectObject.transform.localScale = scale;
        // }

        // // Fix position nếu bị override
        // Vector3 localPos = skillEffectObject.transform.localPosition;
        // if (Mathf.Abs(localPos.x - expectedPosX) > SCALE_THRESHOLD)
        // {
        //     localPos.x = expectedPosX;
        //     skillEffectObject.transform.localPosition = localPos;
        // }
    }

    #endregion

    #region Dash Logic

    private void Dash()
    {
        if (!canDash || isDashing) return;

        isDashRequested = true;

        // Xác định hướng dash
        bool facingRight = transform.localScale.x >= 0f;
        dashDirection = facingRight ? Vector2.right : Vector2.left;

        // Gửi lên server để đồng bộ
        if (networkObject != null && NetworkManager.Singleton != null)
        {
            if (networkObject.IsOwner)
            {
                DashServerRpc(dashDirection);
            }
        }
        else
        {
            StartDashLocal(dashDirection);
        }
    }

    [ServerRpc]
    private void DashServerRpc(Vector2 direction)
    {
        StartDashClientRpc(direction);
    }

    [ClientRpc]
    private void StartDashClientRpc(Vector2 direction)
    {
        StartDashLocal(direction);
    }

    private void StartDashLocal(Vector2 direction)
    {
        // Set dash state
        isDashing = true;
        dashTimer = dashDuration;
        dashDirection = direction;
        canDash = false;
        cooldownTimer = dashCooldown;
        justStartedDash = true;
        isDashRequested = false;
        lastScaleX = transform.localScale.x;

        // Validate SkillEffect
        if (skillEffectObject == null)
        {
            Debug.LogError("[PlayerDash] SkillEffect object là NULL!");
            return;
        }

        // Ensure SkillEffect is active
        if (!skillEffectObject.activeSelf)
        {
            skillEffectObject.SetActive(true);
        }

        // Get Animator if needed
        if (skillEffectAnimator == null)
        {
            skillEffectAnimator = skillEffectObject.GetComponent<Animator>();
            if (skillEffectAnimator == null)
            {
                Debug.LogError("[PlayerDash] Không tìm thấy Animator component trên SkillEffect object!");
                return;
            }
        }

        // Xác định hướng
        bool facingRight = direction.x >= 0f;
        if (Mathf.Abs(direction.x) < SCALE_THRESHOLD)
        {
            facingRight = transform.localScale.x >= 0f;
        }

        // Set transform TRƯỚC KHI trigger animation
        SetSkillEffectTransform(facingRight);

        // Enable SpriteRenderer
        SpriteRenderer spriteRenderer = skillEffectObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
        }

        // Trigger animation
        string triggerToUse = GetDashTriggerName(facingRight);
        TriggerDashAnimation(triggerToUse);

        // Set velocity
        if (rb != null)
        {
            rb.velocity = direction * dashSpeed;
        }
    }

    private string GetDashTriggerName(bool facingRight)
    {
        if (facingRight && !string.IsNullOrEmpty(dashRightTriggerName))
        {
            return dashRightTriggerName;
        }
        else if (!facingRight && !string.IsNullOrEmpty(dashLeftTriggerName))
        {
            return dashLeftTriggerName;
        }
        return dashTriggerName;
    }

    private void TriggerDashAnimation(string triggerName)
    {
        if (skillEffectAnimator == null || string.IsNullOrEmpty(triggerName))
        {
            if (skillEffectAnimator == null)
            {
                Debug.LogError("[PlayerDash] skillEffectAnimator là NULL!");
            }
            return;
        }

        if (skillEffectAnimator.runtimeAnimatorController == null)
        {
            Debug.LogError("[PlayerDash] SkillEffect Animator KHÔNG CÓ Controller!");
            return;
        }

        // Kiểm tra parameter có tồn tại không
        bool hasParameter = false;
        foreach (AnimatorControllerParameter param in skillEffectAnimator.parameters)
        {
            if (param.name == triggerName && param.type == AnimatorControllerParameterType.Trigger)
            {
                hasParameter = true;
                break;
            }
        }

        if (!hasParameter)
        {
            Debug.LogError($"[PlayerDash] Parameter '{triggerName}' KHÔNG TỒN TẠI trong Animator Controller!");
            return;
        }

        // Enable và trigger animation
        if (!skillEffectAnimator.enabled)
        {
            skillEffectAnimator.enabled = true;
        }

        skillEffectAnimator.SetTrigger(triggerName);
    }

    #endregion

    #region Stop Dash

    [ServerRpc]
    private void StopDashServerRpc()
    {
        StopDashClientRpc();
    }

    [ClientRpc]
    private void StopDashClientRpc()
    {
        StopDash();
    }

    private void StopDash()
    {
        isDashing = false;
        dashTimer = 0f;
        timeSinceDashEnded = 0f;

        // Reset animation
        ResetDashAnimation();

        // Ẩn SkillEffect
        if (skillEffectObject != null)
        {
            skillEffectObject.SetActive(false);
        }

        // Reset velocity (chỉ owner)
        if (rb != null)
        {
            bool isOwner = networkObject != null && NetworkManager.Singleton != null && networkObject.IsOwner;
            bool isStandalone = networkObject == null || NetworkManager.Singleton == null;

            if (isOwner || isStandalone)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }
    }

    private void ResetDashAnimation()
    {
        if (skillEffectAnimator == null || skillEffectAnimator.runtimeAnimatorController == null)
        {
            return;
        }

        try
        {
            skillEffectAnimator.ResetTrigger(dashTriggerName);
            skillEffectAnimator.Play("Empty", 0, 0f);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerDash] Không thể reset animation: {e.Message}");
            try
            {
                skillEffectAnimator.ResetTrigger(dashTriggerName);
            }
            catch { }
        }
    }

    #endregion

    #region Public API

    public bool IsDashing() => isDashing;
    public bool CanDash() => canDash;
    public float GetCooldownPercent() => canDash ? 1f : Mathf.Clamp01(1f - (cooldownTimer / dashCooldown));

    #endregion
}
