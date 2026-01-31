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

    [Header("Flight System")]
    private bool isFlying;
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
    }

    public void HandleInput()
    {
        // Chỉ xử lý input nếu là owner hoặc không có network
        // QUAN TRỌNG: Chỉ check IsOwner, không check IsClient (vì có thể gây lỗi timing)
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return; // Remote player không xử lý input
        }

        // ===== ĐỌC INPUT (CHỈ ĐỌC, KHÔNG QUYẾT ĐỊNH LOGIC) =====
        horizontalInput = Input.GetAxisRaw("Horizontal");
        hasHorizontalInput = Mathf.Abs(horizontalInput) > 0.1f;

        // Vertical input (W/S)
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        hasVerticalInput = up || down;

        // Any movement input
        hasAnyInput = hasHorizontalInput || hasVerticalInput;

        // Jump/Fly input (cho jump từ mặt đất)
        jumpPressed = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
        jumpHeld = up;

        // Check if grounded
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        // Reset flight when grounded
        if (isGrounded)
        {
            isFlying = false;
            flightTime = 0f;
            canFly = true;
            flightCooldown = 0f;
        }

        // Update flight cooldown
        if (!canFly && !controller.unlimitedFlight)
        {
            flightCooldown -= Time.deltaTime;
            if (flightCooldown <= 0f)
            {
                canFly = true;
            }
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
        
        // Nếu không có network (standalone), xử lý movement local như bình thường

        PlayerStats stats = controller.stats;
        if (stats == null) return;

        // ===== THỨ TỰ XỬ LÝ (QUAN TRỌNG) =====
        // 1. Horizontal movement (A/D) - Chỉ owner mới set velocity
        float targetVelocityX = horizontalInput * stats.moveSpeed;
        rb.velocity = new Vector2(targetVelocityX, rb.velocity.y);

        // Flip sprite based on direction
        if (horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        // 2. Vertical movement (W/S) - Xử lý bay lên/xuống
        // Dùng biến đã đọc từ HandleInput() thay vì đọc Input trực tiếp
        bool up = jumpHeld; // jumpHeld đã được set trong HandleInput()
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow); // Vẫn cần đọc vì không có biến lưu
        
        if (controller.godMode)
        {
            // God mode - full control
            if (up)
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
        }
        else
        {
            // Normal mode
            if (up && (canFly || controller.unlimitedFlight))
            {
                // Nhấn W → Bay lên ngay (ở mặt đất hoặc trên không đều được)
                rb.velocity = new Vector2(rb.velocity.x, stats.flySpeed);
                isFlying = true;

                // Update flight time
                if (!controller.unlimitedFlight)
                {
                    flightTime += Time.deltaTime;
                    if (flightTime >= stats.maxFlightTime)
                    {
                        canFly = false;
                        flightCooldown = stats.flightCooldown;
                        isFlying = false;
                    }
                }
            }
            else if (down)
            {
                // Nhấn S → Bay xuống
                rb.velocity = new Vector2(rb.velocity.x, -stats.flySpeed);
                isFlying = false;
            }
            else if (hasHorizontalInput && !isGrounded)
            {
                // Nhấn A/D trên không (không nhấn W/S) → Treo trên không
                // QUAN TRỌNG: Nếu đang bay lên (velocity.y > 0), GIỮ NGUYÊN để tiếp tục bay
                // Nếu đang rơi (velocity.y < 0), set về 0 để treo
                if (rb.velocity.y > 0)
                {
                    // Đang bay lên → Giữ nguyên velocity.y để tiếp tục bay
                    // KHÔNG set về 0
                }
                else
                {
                    // Đang rơi hoặc đứng yên → Set về 0 để treo trên không
                    rb.velocity = new Vector2(rb.velocity.x, 0);
                }
                isFlying = false;
            }
            // Nếu không nhấn gì → Để gravity tự xử lý (sẽ set ở bước 4)
        }

        // 4. GRAVITY DECISION (QUYẾT ĐỊNH DUY NHẤT - Ở CUỐI CÙNG)
        // Gravity phụ thuộc DUY NHẤT vào INPUT, không phụ thuộc velocity hay state
        if (controller.godMode)
        {
            rb.gravityScale = 0;
        }
        else
        {
            if (isGrounded)
            {
                // Ở mặt đất → Bình thường
                rb.gravityScale = stats.gravity;
            }
            else
            {
                // Ở trên không
                // NGUYÊN TẮC: Có bất kỳ input nào (A/D/W/S) → gravity = 0
                //              Không có input → gravity = stats.gravity
                if (hasAnyInput)
                {
                    // Có input → Treo trên không (không rơi)
                    rb.gravityScale = 0;
                }
                else
                {
                    // Không có input → Rơi xuống
                    rb.gravityScale = stats.gravity;
                }
            }
        }

        // Update animator
        // QUAN TRỌNG: Truyền velocity.x thay vì horizontalInput để Speed parameter phản ánh tốc độ thực tế
        // Điều này giúp transition Jump -> Run/Idle hoạt động đúng khi player chạm đất
        if (playerAnimator != null)
        {
            playerAnimator.UpdateAnimation(rb.velocity.x, rb.velocity.y, isGrounded, isFlying);
        }
    }

    public bool IsGrounded() => isGrounded;
    public bool IsFlying() => isFlying;
    public float GetHorizontalInput() => horizontalInput;
    public float GetFlightTime() => flightTime;
    public float GetMaxFlightTime() => controller?.stats?.maxFlightTime ?? 0f;
    public float GetFlightPercent() => controller?.stats != null ? 1f - (flightTime / controller.stats.maxFlightTime) : 1f;
    public bool CanFly() => canFly;
    public float GetFlightCooldown() => flightCooldown;

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
