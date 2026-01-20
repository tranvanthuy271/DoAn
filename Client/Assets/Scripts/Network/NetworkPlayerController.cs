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
        
        Debug.Log($"[NetworkPlayerController] OnNetworkSpawn called! GameObject: {gameObject.name}, " +
            $"IsOwner: {IsOwner}, OwnerClientId: {OwnerClientId}, " +
            $"LocalClientId: {NetworkManager.Singleton?.LocalClientId ?? 0}, " +
            $"IsSpawned: {IsSpawned}");

        // Đảm bảo controller được enable (PlayerController đã có check IsOwner)
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
            Debug.Log($"[NetworkPlayerController] Enabled PlayerController for {gameObject.name}");
        }

        // Chỉ owner mới điều khiển input
        if (IsOwner)
        {
            Debug.Log($"[NetworkPlayerController] ✓✓✓ I am the OWNER of this player! ✓✓✓ " +
                $"ClientId: {NetworkManager.Singleton?.LocalClientId ?? 0}, " +
                $"NetworkObject.IsOwner: {GetComponent<NetworkObject>()?.IsOwner ?? false}, " +
                $"IsSpawned: {GetComponent<NetworkObject>()?.IsSpawned ?? false}");
            
            // Set camera to follow local player
            CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.SetTarget(transform);
                Debug.Log($"[NetworkPlayerController] Camera set to follow local player");
            }
            else
            {
                Debug.LogWarning($"[NetworkPlayerController] CameraFollow not found!");
            }
        }
        else
        {
            Debug.Log($"[NetworkPlayerController] ✗✗✗ I am NOT the owner. ✗✗✗ " +
                $"Owner: {OwnerClientId}, " +
                $"Local: {NetworkManager.Singleton?.LocalClientId ?? 0}, " +
                $"NetworkObject.IsOwner: {GetComponent<NetworkObject>()?.IsOwner ?? false}");
        }
    }

    private void Update()
    {
        // Debug: Log mỗi 60 frames để kiểm tra Update() có chạy không
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[NetworkPlayerController] Update running. IsOwner={IsOwner}, OwnerClientId={OwnerClientId}, LocalClientId={NetworkManager.Singleton?.LocalClientId ?? 0}, IsSpawned={IsSpawned}");
        }

        // Debug: Kiểm tra IsOwner
        if (!IsOwner)
        {
            return;
        }

        // Đọc input và gửi lên server qua ServerRpc
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        bool up = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool down = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        // Debug: Log input mỗi frame khi có input (để test)
        if (Mathf.Abs(horizontalInput) > 0.01f || up || down)
        {
            Debug.Log($"[NetworkPlayerController] Input detected! h={horizontalInput}, up={up}, down={down}, lastH={lastHorizontalInput}");
        }

        // Chỉ gửi khi input thay đổi để tiết kiệm bandwidth
        if (Mathf.Abs(horizontalInput - lastHorizontalInput) > 0.01f || 
            up != lastUpInput || 
            down != lastDownInput)
        {
            Debug.Log($"[NetworkPlayerController] ✓✓✓ Sending MoveServerRpc: h={horizontalInput}, up={up}, down={down} ✓✓✓");
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

    /// <summary>
    /// ServerRpc để gửi input từ client lên server
    /// Server sẽ xử lý movement và NetworkTransform sẽ sync lại cho tất cả clients
    /// </summary>
    [ServerRpc]
    private void MoveServerRpc(float horizontalInput, bool up, bool down)
    {
        Debug.Log($"[NetworkPlayerController] MoveServerRpc RECEIVED on SERVER: h={horizontalInput}, up={up}, down={down}, OwnerClientId={OwnerClientId}");
        
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
        Debug.Log($"[NetworkPlayerController] MoveServerRpc: Processing movement. moveSpeed={stats.moveSpeed}, currentVelocity={rb.velocity}");

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
        Debug.Log($"[NetworkPlayerController] MoveServerRpc: Set velocity to {newVelocity}");

        // Flip sprite (server cần flip để sync cho tất cả clients)
        if (horizontalInput > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (horizontalInput < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

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
    }
}




