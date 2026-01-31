using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Components")]
    private PlayerMovement movement;
    private PlayerController controller;
    private Rigidbody2D rb;
    private Animator animator;

    [Header("Network Movement")]
    private Vector2 lastInput = Vector2.zero;
    private float lastHorizontalInput = 0f;
    private bool lastUpInput = false;
    private bool lastDownInput = false;

    [Header("Network Sync")]
    // NetworkVariable để sync flip direction (localScale.x)
    private NetworkVariable<float> networkScaleX = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to networkScaleX changes để sync flip direction
        networkScaleX.OnValueChanged += OnScaleXChanged;

        // Đảm bảo controller được enable (PlayerController đã có check IsOwner)
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }

        // Chỉ owner mới điều khiển input
        if (IsOwner)
        {
            CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(transform);
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayerController] CameraFollow not found!");
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        networkScaleX.OnValueChanged -= OnScaleXChanged;
        base.OnNetworkDespawn();
    }

    private void OnScaleXChanged(float oldValue, float newValue)
    {
        // Sync flip direction khi networkScaleX thay đổi
        Vector3 scale = transform.localScale;
        scale.x = newValue;
        transform.localScale = scale;
    }

    private void Update()
    {
        // Chỉ owner mới xử lý input
        if (!IsOwner)
        {
            return;
        }

        // Đọc input và gửi lên server qua ServerRpc
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // Chỉ gửi khi input thay đổi để tiết kiệm bandwidth
        if (Mathf.Abs(horizontalInput - lastHorizontalInput) > 0.01f || 
            up != lastUpInput || 
            down != lastDownInput)
        {
            MoveServerRpc(horizontalInput, up, down);
            lastHorizontalInput = horizontalInput;
            lastUpInput = up;
            lastDownInput = down;
        }
    }

    private void FixedUpdate()
    {
        // Chỉ owner mới cần gửi data
        if (!IsOwner) return;

        // Movement được xử lý bởi ServerRpc
    }

    private void LateUpdate()
    {
        // Update animation trên TẤT CẢ clients (bao gồm remote clients)
        // Dựa trên velocity từ NetworkTransform để update animation
        if (rb != null && movement != null && movement.GetComponent<PlayerAnimator>() != null)
        {
            PlayerAnimator playerAnimator = movement.GetComponent<PlayerAnimator>();
            
            // Tính toán animation parameters dựa trên velocity và state hiện tại
            // QUAN TRỌNG: Truyền velocity.x thay vì input để Speed parameter phản ánh tốc độ thực tế
            float horizontalVelocity = rb.velocity.x;
            float velocityY = rb.velocity.y;
            bool isGrounded = movement.IsGrounded();
            bool isFlying = movement.IsFlying();
            
            // Update animation (UpdateAnimation sẽ tự xử lý Mathf.Abs cho Speed)
            playerAnimator.UpdateAnimation(horizontalVelocity, velocityY, isGrounded, isFlying);
        }
    }

    /// <summary>
    /// ServerRpc để gửi input từ client lên server
    /// Server sẽ xử lý movement và NetworkTransform sẽ sync lại cho tất cả clients
    /// </summary>
    [ServerRpc]
    private void MoveServerRpc(float horizontalInput, bool up, bool down)
    {
        // Server xử lý movement
        if (movement == null || controller == null || rb == null)
        {
            Debug.LogError($"[NetworkPlayerController] MoveServerRpc: Components null! movement={movement}, controller={controller}, rb={rb}");
            return;
        }
        if (controller.stats == null)
        {
            Debug.LogError($"[NetworkPlayerController] MoveServerRpc: PlayerStats is null!");
            return;
        }

        PlayerStats stats = controller.stats;

        // Check ground (server cần tự check)
        bool isGrounded = false;
        if (movement != null)
        {
            // Sử dụng method từ PlayerMovement nếu có
            isGrounded = movement.IsGrounded();
        }
        else
        {
            // Fallback: Check ground trực tiếp
            Collider2D groundCheck = Physics2D.OverlapCircle(transform.position + Vector3.down * 0.5f, 0.2f);
            isGrounded = groundCheck != null;
        }

        // 1. Horizontal movement
        float targetVelocityX = horizontalInput * stats.moveSpeed;
        Vector2 newVelocity = new Vector2(targetVelocityX, rb.velocity.y);
        rb.velocity = newVelocity;

        // Flip sprite (server cần flip để sync cho tất cả clients)
        // QUAN TRỌNG: Chỉ flip khi có input, giữ nguyên khi input = 0
        if (horizontalInput > 0.01f)
        {
            // Di chuyển sang phải → scale.x = 1
            if (Mathf.Abs(networkScaleX.Value - 1f) > 0.01f)
            {
                networkScaleX.Value = 1f;
                transform.localScale = new Vector3(1f, 1, 1);
            }
        }
        else if (horizontalInput < -0.01f)
        {
            // Di chuyển sang trái → scale.x = -1
            if (Mathf.Abs(networkScaleX.Value - (-1f)) > 0.01f)
            {
                networkScaleX.Value = -1f;
                transform.localScale = new Vector3(-1f, 1, 1);
            }
        }
        // Nếu horizontalInput = 0 → Giữ nguyên scale hiện tại (không flip)

        // 2. Vertical movement
        if (controller.godMode)
        {
            if (up)
            {
                rb.velocity = new Vector2(rb.velocity.x, stats.flySpeed);
            }
            else if (down)
            {
                rb.velocity = new Vector2(rb.velocity.x, -stats.flySpeed);
            }
            else
            {
                rb.velocity = new Vector2(rb.velocity.x, 0);
            }
            rb.gravityScale = 0;
        }
        else
        {
            // Normal mode - xử lý flight logic
            // Lưu ý: Flight time và cooldown được quản lý bởi PlayerMovement trên client
            // Server chỉ xử lý movement dựa trên input
            bool hasAnyInput = Mathf.Abs(horizontalInput) > 0.1f || up || down;

            if (up)
            {
                // Bay lên (server không check canFly, để client quản lý)
                rb.velocity = new Vector2(rb.velocity.x, stats.flySpeed);
            }
            else if (down)
            {
                // Bay xuống
                rb.velocity = new Vector2(rb.velocity.x, -stats.flySpeed);
            }
            else if (Mathf.Abs(horizontalInput) > 0.1f && !isGrounded)
            {
                // Treo trên không khi có horizontal input
                if (rb.velocity.y <= 0)
                {
                    rb.velocity = new Vector2(rb.velocity.x, 0);
                }
            }

            // Gravity
            if (isGrounded)
            {
                rb.gravityScale = stats.gravity;
            }
            else
            {
                // Có input → gravity = 0, không có input → gravity = stats.gravity
                rb.gravityScale = hasAnyInput ? 0 : stats.gravity;
            }
        }

        // Update animation trên server (sẽ được sync qua NetworkAnimator)
        UpdateAnimationClientRpc(horizontalInput, rb.velocity.y, isGrounded, up || down);
    }

    /// <summary>
    /// ClientRpc để sync animation parameters cho tất cả clients
    /// </summary>
    [ClientRpc]
    private void UpdateAnimationClientRpc(float horizontalInput, float velocityY, bool isGrounded, bool isFlying)
    {
        // Update animation trên TẤT CẢ clients (không chỉ owner)
        // QUAN TRỌNG: Dùng velocity.x thay vì horizontalInput để Speed parameter phản ánh tốc độ thực tế
        if (movement != null && movement.GetComponent<PlayerAnimator>() != null && rb != null)
        {
            PlayerAnimator playerAnimator = movement.GetComponent<PlayerAnimator>();
            playerAnimator.UpdateAnimation(rb.velocity.x, velocityY, isGrounded, isFlying);
        }
    }
}




