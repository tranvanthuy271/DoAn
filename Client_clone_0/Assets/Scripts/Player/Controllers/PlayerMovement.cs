using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : MonoBehaviour
{
    [Header("Components")]
    private PlayerController controller;
    private Rigidbody2D rb;
    private PlayerAnimator playerAnimator;
    private NetworkObject networkObject;

    [Header("Movement")]
    private float horizontalInput;
    private bool jumpPressed;
    private bool jumpHeld;
    
    // Input flags (chỉ đọc input, không quyết định logic)
    private bool hasHorizontalInput;  // A/D
    private bool hasVerticalInput;     // W/S
    private bool hasAnyInput;          // A/D/W/S

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    [Header("Step Climb (leo bậc thang nhỏ)")]
    [Tooltip("Chiều cao bậc tối đa mà player có thể leo qua khi đi ngang (tính bằng unit)")]
    [SerializeField] private float stepHeight = 0.3f;
    [Tooltip("Khoảng cách probe ngang để phát hiện bậc")]
    [SerializeField] private float stepProbeDistance = 0.1f;
    // Dùng lại groundLayer để phát hiện bậc

    [Header("Jump State")]
    private bool isFlying;       // chỉ true trong god mode
    private bool shouldJump;    // được set ở Update, consume ở FixedUpdate

    [Header("Stun (bất động khi trúng skill)")]
    private bool isStunned;
    private float stunTimer;

    [Header("Flight System (God Mode only)")]
    private float flightTime;
    private bool canFly = true;
    private float flightCooldown = 0f;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<PlayerAnimator>();
        networkObject = GetComponent<NetworkObject>();
    }

    private void Start()
    {
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.SetParent(transform);
            groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
            groundCheck = groundCheckObj.transform;
        }

        // Nếu groundLayer chưa được gán trong Inspector (= 0 / Nothing),
        // tự động gán vào layer "Ground" nếu tồn tại
        if (groundLayer.value == 0)
        {
            int layerId = LayerMask.NameToLayer("Ground");
            if (layerId >= 0)
            {
                groundLayer = LayerMask.GetMask("Ground");
                Debug.Log("[PlayerMovement] groundLayer tự động gán vào layer 'Ground'");
            }
            else
            {
                Debug.LogWarning("[PlayerMovement] groundLayer chưa được gán và không tìm thấy layer 'Ground'. " +
                    "Hãy tạo layer 'Ground', gán cho Tilemap/Ground objects, " +
                    "rồi chọn nó trong PlayerMovement → Ground Layer.");
            }
        }

        // Gán PhysicsMaterial2D ma sát = 0 để player không bị "dính" vào cạnh của tiles/collider khi di chuyển ngang
        if (rb != null && rb.sharedMaterial == null)
        {
            PhysicsMaterial2D zeroFriction = new PhysicsMaterial2D("PlayerZeroFriction");
            zeroFriction.friction = 0f;
            zeroFriction.bounciness = 0f;
            rb.sharedMaterial = zeroFriction;
            Debug.Log("[PlayerMovement] Đã gán PhysicsMaterial2D friction=0 cho Rigidbody2D.");
        }
    }

    /// <summary>Thực hiện ground check và trả về kết quả.</summary>
    private bool DoGroundCheck()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    public void HandleInput()
    {
        // Ground check chạy cho TẤT CẢ players (cần thiết cho animation remote client)
        isGrounded = DoGroundCheck();

        // Chỉ xử lý input nếu là owner hoặc không có network
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return; // Remote player không xử lý input
        }
        // Stun: không nhận input khi bị bất động
        if (isStunned)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0f) isStunned = false;
            horizontalInput = 0f;
            hasHorizontalInput = false;
            hasVerticalInput = false;
            hasAnyInput = false;
            jumpPressed = false;
            jumpHeld = false;
            return;
        }
        var im = InputManager.Instance;
        horizontalInput = im != null ? im.GetHorizontalInput() : Input.GetAxisRaw("Horizontal");
        hasHorizontalInput = Mathf.Abs(horizontalInput) > 0.1f;

        // Vertical input (W/S or mobile joystick)
        float verticalAxis = im != null ? im.GetVerticalInput() : Input.GetAxisRaw("Vertical");
        bool up   = verticalAxis > 0.1f  || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = verticalAxis < -0.1f || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        hasVerticalInput = up || down;

        // Any movement input
        hasAnyInput = hasHorizontalInput || hasVerticalInput;

        // Jump input
        jumpPressed = im != null ? im.GetJumpPressed()
                                 : (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W));
        jumpHeld    = im != null ? im.GetJumpHeld()
                                 : (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W));

        // isGrounded đã được update ở đầu HandleInput() rồi, không cần check lại

        // Cho phép nhảy nếu đang ở dưới đất và nhLetấn jump
        if (jumpPressed && isGrounded)
        {
            shouldJump = true;
        }

        // God mode: reset flight khi chạm đất
        if (isGrounded)
        {
            isFlying = false;
            flightTime = 0f;
            canFly = true;
            flightCooldown = 0f;
        }

        // Cập nhật flight cooldown (god mode)
        if (!canFly && !controller.unlimitedFlight)
        {
            flightCooldown -= Time.deltaTime;
            if (flightCooldown <= 0f) canFly = true;
        }
    }

    public void HandleMovement()
    {
        // QUAN TRỌNG: Khi có network, movement được xử lý bởi ServerRpc trong NetworkPlayerController
        // Chỉ xử lý movement local nếu không có network (standalone mode)
        if (networkObject != null && NetworkManager.Singleton != null)
        {
            // Có network: 
            // - Owner: Movement được xử lý bởi NetworkPlayerController.MoveServerRpc()
            // - Non-owner: Để NetworkTransform tự sync
            return;
        }
        
        // Nếu không có network (standalone), xử lý movement local

        PlayerStats stats = controller.stats;
        if (stats == null) return;

        // Step climb: leo bậc thang nhỏ trước khi set velocity ngang
        HandleStepClimb(horizontalInput);

        // 1. Horizontal movement (A/D) – luôn hoạt động kể cả khi trên không
        float targetVelocityX = horizontalInput * stats.moveSpeed;
        rb.velocity = new Vector2(targetVelocityX, rb.velocity.y);

        if (horizontalInput > 0)       transform.localScale = new Vector3(1, 1, 1);
        else if (horizontalInput < 0)  transform.localScale = new Vector3(-1, 1, 1);

        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // 2. Vertical / jump
        if (controller.godMode)
        {
            // God mode: bay tự do với W/S
            if (jumpHeld)
            {
                rb.velocity = new Vector2(rb.velocity.x, stats.flySpeed);
                isFlying = true;
            }
            else if (down)
            {
                rb.velocity = new Vector2(rb.velocity.x, -stats.flySpeed);
                isFlying = false;
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
                isFlying = false;
            }
            rb.gravityScale = 0;
        }
        else
        {
            // Normal mode: nhảy impulse, trọng lực luôn tác động khi trên không
            if (shouldJump)
            {
                rb.AddForce(Vector2.up * stats.jumpForce, ForceMode2D.Impulse);
                shouldJump = false;
            }
            isFlying = false;
            // Trọng lực luôn bật – không treo lơ lừng khi nhấn A/D trên không
            rb.gravityScale = stats.gravity;
        }

        // Update animator
        // QUAN TRỌNG: Truyền velocity.x thay vì horizontalInput để Speed parameter phản ánh tốc độ thực tế
        // Điều này giúp transition Jump -> Run/Idle hoạt động đúng khi player chạm đất
        if (playerAnimator != null)
        {
            playerAnimator.UpdateAnimation(rb.velocity.x, rb.velocity.y, isGrounded, isFlying);
        }
    }

    /// <summary>
    /// Refresh ground check ngay lập tức (dùng cho server-side check trong NetworkPlayerController)
    /// </summary>
    public void RefreshGroundCheck()
    {
        isGrounded = DoGroundCheck();
    }

    public bool IsGrounded() => isGrounded;
    public bool IsFlying() => isFlying;
    public float GetHorizontalInput() => horizontalInput;
    public float GetFlightTime() => flightTime;
    public float GetMaxFlightTime() => controller?.stats?.maxFlightTime ?? 0f;
    public float GetFlightPercent() => controller?.stats != null ? 1f - (flightTime / controller.stats.maxFlightTime) : 1f;
    public bool CanFly() => canFly;
    public float GetFlightCooldown() => flightCooldown;

    /// <summary>
    /// Leo qua bậc thang nhỏ (step climb): khi player đang ở mặt đất và đi ngang mà gặp 
    /// một collider thấp hơn stepHeight, tự động đẩy player lên trên để di chuyển qua được.
    /// Gọi method này từ FixedUpdate TRƯỚC khi gán rb.velocity ngang.
    /// </summary>
    public void HandleStepClimb(float horizInput)
    {
        if (!isGrounded) return;
        if (Mathf.Abs(horizInput) < 0.1f) return;
        if (rb == null) return;

        float dir = Mathf.Sign(horizInput);
        Collider2D col = GetComponent<Collider2D>();
        float halfW = col != null ? col.bounds.extents.x + stepProbeDistance : 0.3f + stepProbeDistance;
        float botY  = col != null ? col.bounds.min.y + 0.05f : transform.position.y - 0.45f;

        // Ray ngang ở gần sát đáy collider – phát hiện bậc thang
        var lowHit = Physics2D.Raycast(
            new Vector2(transform.position.x, botY),
            new Vector2(dir, 0f),
            halfW,
            groundLayer);

        if (lowHit.collider == null) return; // Không có chướng ngại vật → không cần leo

        // Nếu collider vừa hit có PlatformEffector2D (one-way platform) → bỏ qua, không step-climb
        if (lowHit.collider.GetComponent<PlatformEffector2D>() != null) return;

        // Ray ngang ở độ cao bậc thang – nếu trống thì có thể leo qua
        var highHit = Physics2D.Raycast(
            new Vector2(transform.position.x, botY + stepHeight),
            new Vector2(dir, 0f),
            halfW,
            groundLayer);

        if (highHit.collider != null) return; // Bức tường cao hơn stepHeight → không leo được (bị chặn bởi wall)

        // Đẩy player lên trên để vượt qua bậc, giữ nguyên velocity ngang
        rb.position = new Vector2(rb.position.x, rb.position.y + stepHeight + 0.02f);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }

    /// <summary>Áp dụng bất động cho player (chặn input) trong thời gian duration giây.</summary>
    public void SetStunned(float duration)
    {
        isStunned = true;
        stunTimer = Mathf.Max(stunTimer, duration);
    }

    /// <summary>Kiểm tra player có đang bị stun không.</summary>
    public bool IsStunned => isStunned;
}
