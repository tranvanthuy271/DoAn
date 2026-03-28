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
    // Dùng để detect GetKeyDown trong Update rồi consume trong FixedUpdate
    private bool pendingJump = false;

    [Header("Network Sync")]
    // NetworkVariable để sync flip direction (localScale.x) cho non-owner clients
    private NetworkVariable<float> networkScaleX = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // NetworkVariable để sync position khi không có NetworkTransform component
    private NetworkVariable<Vector3> syncPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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

        // Khởi tạo scale theo giá trị hiện tại của NetworkVariable
        transform.localScale = new Vector3(networkScaleX.Value, 1, 1);

        // Đảm bảo controller được enable (PlayerController đã có check IsOwner)
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }

        // Non-owner: tắt physics cục bộ, để server-position drive transform
        if (!IsOwner && rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        // Chỉ owner mới điều khiển input
        if (IsOwner)
        {
            // Đặt player vào đúng vị trí đích nếu vừa chuyển map qua portal
            PortalArrivalHandler.ApplyPendingArrival(transform);

            CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(transform);
                Debug.Log($"[NetworkPlayerController] Camera target set to {gameObject.name}");
            }
            else
            {
                Debug.LogWarning("[NetworkPlayerController] CameraFollow not found! Client player will not be followed by camera.");
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
        // Chỉ non-owner mới cập nhật từ NetworkVariable.
        // Owner tự điều khiển flip qua client-side prediction trong FixedUpdate
        // để tránh stale server update ghi đè lên local prediction.
        if (!IsOwner)
            transform.localScale = new Vector3(newValue, transform.localScale.y, transform.localScale.z);
    }

    private void Update()
    {
        // Chỉ owner mới xử lý input
        if (!IsOwner) return;

        // Detect jump trong Update để không bị miss giữa 2 FixedUpdate
        var im = InputManager.Instance;
        if (im != null ? im.GetJumpPressed() : (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            pendingJump = true;
    }

    private bool _moveDiagLogged = false;

    private void FixedUpdate()
    {
        // Non-owner: chỉ client thuần (không phải server) mới cần interpolate về server position.
        // Server KHÔNG chạy MovePosition cho player của client — physics đã được drive bởi MoveServerRpc.
        if (!IsOwner)
        {
            if (!IsServer && rb != null && syncPosition.Value != Vector3.zero)
            {
                Vector2 target = new Vector2(syncPosition.Value.x, syncPosition.Value.y);
                rb.MovePosition(Vector2.Lerp(rb.position, target, Time.fixedDeltaTime * 20f));
            }
            return;
        }

        // Owner gửi input lên server MỖI FixedUpdate để velocity luôn được apply liên tục

        var im = InputManager.Instance;
        float horizontalInput = im != null ? im.GetHorizontalInput() : Input.GetAxisRaw("Horizontal");
        float verticalAxis = im != null ? im.GetVerticalInput() : Input.GetAxisRaw("Vertical");
        bool down = verticalAxis < -0.1f || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool jump = pendingJump;
        pendingJump = false; // consume flag

        // === MOVEMENT DIAGNOSTICS (only log once) ===
        if (!_moveDiagLogged)
        {
            _moveDiagLogged = true;
            Debug.Log($"[NetworkPlayerController] DIAG | controller={controller != null} | controller.stats={controller?.stats != null} | movement={movement != null} | rb={rb != null} | IsOwner={IsOwner} | IsServer={IsServer}");
        }

        // === CLIENT-SIDE PREDICTION ===
        // Apply movement cục bộ ngay lập tức để owner thấy di chuyển không có độ trễ
        // Server sẽ xác nhận và NetworkTransform sẽ correct nếu có sai lệch
        if (controller?.stats != null && movement != null && rb != null)
        {
            movement.RefreshGroundCheck();
            bool isGrounded = movement.IsGrounded();
            PlayerStats stats = controller.stats;

            // Step climb: leo bậc thang nhỏ trước khi set velocity ngang
            movement.HandleStepClimb(horizontalInput);

            // Horizontal
            rb.velocity = new Vector2(horizontalInput * stats.moveSpeed, rb.velocity.y);

            // Instant flip cục bộ (không chờ server roundtrip)
            if (horizontalInput > 0.01f)
                transform.localScale = new Vector3(1f, 1f, 1f);
            else if (horizontalInput < -0.01f)
                transform.localScale = new Vector3(-1f, 1f, 1f);

            // Vertical
            if (controller.godMode)
            {
                if (jump)
                    rb.velocity = new Vector2(rb.velocity.x, stats.flySpeed);
                else if (down)
                    rb.velocity = new Vector2(rb.velocity.x, -stats.flySpeed);
                else
                    rb.velocity = new Vector2(rb.velocity.x, 0);
                rb.gravityScale = 0;
            }
            else
            {
                if (jump && isGrounded && rb.velocity.y < 1f)
                    rb.AddForce(Vector2.up * stats.jumpForce, ForceMode2D.Impulse);
                rb.gravityScale = stats.gravity;
            }

            // Update animation cục bộ ngay (không chờ UpdateAnimationClientRpc)
            var playerAnimator = movement.GetComponent<PlayerAnimator>();
            playerAnimator?.UpdateAnimation(rb.velocity.x, rb.velocity.y, isGrounded, movement.IsFlying());
        }
        MoveServerRpc(horizontalInput, jump, down);
    }

    private void LateUpdate()
    {
        // Server: sync position để tất cả clients có thể theo dõi vị trí player
        if (IsServer && rb != null)
        {
            syncPosition.Value = new Vector3(rb.position.x, rb.position.y, transform.position.z);
        }

        // Non-owner: đảm bảo scale luôn đúng mỗi frame (poll thay vì chỉ dùng event)
        // Giúp loại bỏ triệt để hiệu ứng bìa giấy kể cả khi có ngoại lực khác ghi đè scale giữa các frame.
        if (!IsOwner)
        {
            float targetX = networkScaleX.Value;
            if (!Mathf.Approximately(transform.localScale.x, targetX))
                transform.localScale = new Vector3(targetX, transform.localScale.y, transform.localScale.z);
        }

        // Update animation trên owner và server (non-owner client dùng UpdateAnimationClientRpc)
        if ((IsOwner || IsServer) && rb != null && movement != null && movement.GetComponent<PlayerAnimator>() != null)
        {
            PlayerAnimator playerAnimator = movement.GetComponent<PlayerAnimator>();

            // Refresh ground check trước khi lấy giá trị – đảm bảo chính xác trên mọi client/server
            movement.RefreshGroundCheck();

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
            Debug.LogError($"[NetworkPlayerController] MoveServerRpc: Components null! movement={movement != null}, controller={controller != null}, rb={rb != null}");
            return;
        }
        if (controller.stats == null)
        {
            Debug.LogError("[NetworkPlayerController] MoveServerRpc: PlayerStats is null!");
            return;
        }

        PlayerStats stats = controller.stats;

        // CRITICAL: Refresh ground check trên server mỗi frame trước khi dùng
        // (HandleInput() không chạy trên server vì server không phải IsOwner)
        movement.RefreshGroundCheck();
        bool isGrounded = movement.IsGrounded();

        // Step climb: leo bậc thang nhỏ trước khi set velocity ngang (server-side)
        movement.HandleStepClimb(horizontalInput);

        // 1. Horizontal movement
        float targetVelocityX = horizontalInput * stats.moveSpeed;
        Vector2 newVelocity = new Vector2(targetVelocityX, rb.velocity.y);
        rb.velocity = newVelocity;

        // Flip sprite (server cần flip để sync cho tất cả clients)
        // QUAN TRỌNG: Chỉ flip khi có input, giữ nguyên khi input = 0
        if (horizontalInput > 0.01f)
        {
            if (Mathf.Abs(networkScaleX.Value - 1f) > 0.01f)
                networkScaleX.Value = 1f;
        }
        else if (horizontalInput < -0.01f)
        {
            if (Mathf.Abs(networkScaleX.Value - (-1f)) > 0.01f)
                networkScaleX.Value = -1f;
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
            // Normal mode: jump impulse từ mặt đất, trọng lực luôn tác động khi trên không
            if (up && isGrounded && rb.velocity.y < 1f)
            {
                // Áp dụng lực nhảy 1 lần duy nhất khi đang đứng đất và nhấn W
                rb.AddForce(Vector2.up * stats.jumpForce, ForceMode2D.Impulse);
            }

            // Trọng lực luôn bật – không treo lơ lửng khi nhấn A/D trên không
            rb.gravityScale = stats.gravity;
        }

        // Update animation trên server — truyền velocity thực tế để ClientRpc dùng đúng Speed
        UpdateAnimationClientRpc(rb.velocity.x, rb.velocity.y, isGrounded, movement.IsFlying());
    }

    /// <summary>
    /// ClientRpc để sync animation parameters cho tất cả clients
    /// </summary>
    [ClientRpc]
    private void UpdateAnimationClientRpc(float velocityX, float velocityY, bool isGrounded, bool isFlying)
    {
        // Owner tự update animation trong FixedUpdate/LateUpdate — không cần ClientRpc
        if (IsOwner) return;
        // Non-owner client: dùng velocityX do server truyền xuống (rb.velocity.x cục bộ luôn = 0 với non-owner)
        if (movement != null && movement.GetComponent<PlayerAnimator>() != null)
        {
            PlayerAnimator playerAnimator = movement.GetComponent<PlayerAnimator>();
            playerAnimator.UpdateAnimation(velocityX, velocityY, isGrounded, isFlying);
        }
    }
}




