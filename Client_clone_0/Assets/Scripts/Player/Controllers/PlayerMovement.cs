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

    [Header("Fall-through Platform")]
    [Tooltip("Khoảng cách tìm kiếm ground bên dưới platform hiện tại để kiểm tra có ground nào nữa không")]
    [SerializeField] private float fallThroughSearchDistance = 15f;
    [Tooltip("Thời gian ignore collision với platform khi nhấn S/Down. Tăng x3 để player rơi hẳn xuống dưới.")]
    [SerializeField] private float fallThroughIgnoreDuration = 1.05f;
    [Tooltip("Khoảng đẩy xuống ngay khi bắt đầu fall-through để tránh bị snap ngược về platform cũ")]
    [SerializeField] private float fallThroughInitialDrop = 0.2f;
    [Tooltip("Vận tốc rơi tối thiểu khi bắt đầu fall-through")]
    [SerializeField] private float fallThroughMinVelocity = 4f;
    private bool shouldFallThrough; // detectt ở HandleInput, consume ở HandleMovement / NetworkPlayerController

    [Header("Stun (bất động khi trúng skill)")]
    private bool isStunned;
    private float stunTimer;

    [Header("Flight System (God Mode only)")]
    private float flightTime;
    private bool canFly = true;
    private float flightCooldown = 0f;

    private NetworkPlayerController _networkPlayerController;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        playerAnimator = GetComponent<PlayerAnimator>();
        networkObject = GetComponent<NetworkObject>();
        _networkPlayerController = GetComponent<NetworkPlayerController>();
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

        // Fall-through: nhấn S/DownArrow (hay nút ↓ mobile) khi đang đứng trên one-way platform
        bool fallThroughKey = im != null ? im.GetFallThroughPressed()
                                         : (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow));
        if (fallThroughKey && isGrounded)
            shouldFallThrough = true;

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
        // Khi có network VÀ có NetworkPlayerController, movement của owner được xử lý bởi
        // NetworkPlayerController.MoveServerRpc() — không xử lý ở đây để tránh conflict.
        // Nhưng nếu KHÔNG có NetworkPlayerController (Fusion prefab F_Phong, F_Kim...),
        // owner vẫn phải tự xử lý movement local ở đây.
        if (networkObject != null && NetworkManager.Singleton != null && _networkPlayerController != null)
        {
            // Có NetworkPlayerController: movement của owner dùng ServerRpc, non-owner dùng NetworkTransform
            return;
        }

        // Không phải owner → NetworkTransform xử lý vị trí, không chạy physics local
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return;
        }

        PlayerStats stats = controller.stats;
        if (stats == null) return;

        // Step climb: leo bậc thang nhỏ trước khi set velocity ngang
        HandleStepClimb(horizontalInput);

        // 1. Horizontal movement (A/D) – luôn hoạt động kể cả khi trên không
        float slowFactor = 1f;
        var debuffMgr = GetComponent<DebuffManager>();
        if (debuffMgr != null) slowFactor = debuffMgr.GetSlowFactor();
        float targetVelocityX = horizontalInput * stats.moveSpeed * slowFactor;
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
            // Fall-through one-way platform
            if (shouldFallThrough)
            {
                shouldFallThrough = false;
                TryFallThroughPlatform();
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
    public Transform GroundCheckTransform => groundCheck;
    public float GroundCheckRadius => groundCheckRadius;
    public LayerMask GroundLayerMask => groundLayer;
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

    // ─────────────────────────────────────────────────────────────────────────
    // Fall-through one-way platform (S / ↓ / mobile button)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dùng bởi NetworkPlayerController để consume flag shouldFallThrough và gọi TryFallThroughPlatform.
    /// </summary>
    public bool ConsumePendingFallThrough()
    {
        bool v = shouldFallThrough;
        shouldFallThrough = false;
        return v;
    }

    /// <summary>
    /// Thực hiện rơi xuyên qua one-way platform đang đứng trên, nếu có ground phía dưới nó.
    /// Gọi từ HandleMovement() (standalone) hoặc NetworkPlayerController.FixedUpdate() (network).
    /// </summary>
    public void TryFallThroughPlatform()
    {
        Collider2D platform = GetCurrentOneWayPlatform();
        if (platform == null) return;
        if (!HasGroundBelow(platform))
        {
            Debug.Log("[PlayerMovement] Fall-through bị chặn: không có ground bên dưới platform này.");
            return;
        }
        StartCoroutine(FallThroughCoroutine(platform));
    }

    /// <summary>Tìm one-way platform trực tiếp bên dưới player (nơi đang đứng).</summary>
    private Collider2D GetCurrentOneWayPlatform()
    {
        if (groundCheck == null) return null;
        // Raycast ngắn từ groundCheck xuống để tìm collider có PlatformEffector2D
        RaycastHit2D[] hits = Physics2D.RaycastAll(
            groundCheck.position,
            Vector2.down,
            groundCheckRadius + 0.2f,
            groundLayer);
        foreach (var hit in hits)
        {
            if (hit.collider == null) continue;
            var eff = hit.collider.GetComponent<PlatformEffector2D>();
            if (eff != null && eff.useOneWay)
                return hit.collider;
        }
        return null;
    }

    /// <summary>
    /// Kiểm tra có ground nào khác bên dưới platform hiện tại không.
    /// Nếu KHÔNG có → đây là ground cuối cùng → không cho rơi xuống.
    /// </summary>
    private bool HasGroundBelow(Collider2D currentPlatform)
    {
        float startY = currentPlatform.bounds.min.y - 0.05f;
        RaycastHit2D[] downHits = Physics2D.RaycastAll(
            new Vector2(transform.position.x, startY),
            Vector2.down,
            fallThroughSearchDistance,
            groundLayer);
        foreach (var hit in downHits)
        {
            if (hit.collider != null && hit.collider != currentPlatform)
                return true;
        }
        return false;
    }

    /// <summary>Tắt collision với platform 0.35s để player rơi xuyên qua, rồi bật lại.</summary>
    private System.Collections.IEnumerator FallThroughCoroutine(Collider2D platform)
    {
        Collider2D playerCol = GetComponent<Collider2D>();
        if (playerCol == null || platform == null || rb == null) yield break;

        Bounds platformBounds = platform.bounds;

        Physics2D.IgnoreCollision(playerCol, platform, true);
        rb.position = new Vector2(rb.position.x, rb.position.y - fallThroughInitialDrop);
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Min(rb.velocity.y, -fallThroughMinVelocity));

        float elapsed = 0f;
        float maxWait = fallThroughIgnoreDuration + 0.75f;

        while (elapsed < maxWait)
        {
            elapsed += Time.deltaTime;

            bool minimumDurationElapsed = elapsed >= fallThroughIgnoreDuration;
            bool fullyBelowPlatform = playerCol != null && platform != null
                ? playerCol.bounds.max.y < platformBounds.min.y - 0.05f
                : true;

            if (minimumDurationElapsed && fullyBelowPlatform)
                break;

            yield return null;
        }

        // Guard: collider vẫn còn tồn tại sau khi chờ
        if (playerCol != null && platform != null)
            Physics2D.IgnoreCollision(playerCol, platform, false);
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
