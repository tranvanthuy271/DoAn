using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkObject))]
public class NetworkPlayerController : NetworkBehaviour
{
    [Header("Components")]
    private PlayerMovement movement;
    private PlayerController controller;
    private Rigidbody2D rb;
    private Animator animator;
    private NetworkTransform networkTransform;

    // Lưu prefab Y scale gốc để không bị reset thành 1 khi flip/sync
    private float _prefabScaleY = 1f;

    [Header("Network Movement")]
    // Dùng để detect GetKeyDown trong Update rồi consume trong FixedUpdate
    private bool pendingJump = false;
    private bool pendingFallThrough = false;

    [Header("Network Sync")]
    // NetworkVariable để sync flip direction (localScale.x) cho non-owner clients
    private NetworkVariable<float> networkScaleX = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // NetworkVariable sync position cho non-owner clients.
    // Player prefab vẫn có NetworkTransform trong asset, nhưng component đó bị tắt ở runtime
    // để tránh xung đột authority với luồng custom movement của controller này.
    private NetworkVariable<Vector3> syncPosition = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        networkTransform = GetComponent<NetworkTransform>();
        _prefabScaleY = Mathf.Abs(transform.localScale.y); // cache trước khi bị ghi đè
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        DisableConflictingNetworkTransform();

        // Subscribe to networkScaleX changes để sync flip direction
        networkScaleX.OnValueChanged += OnScaleXChanged;

        // Khởi tạo scale theo giá trị hiện tại của NetworkVariable (giữ Y scale gốc từ prefab)
        transform.localScale = new Vector3(networkScaleX.Value, _prefabScaleY, 1);

        // Đảm bảo controller được enable (PlayerController đã có check IsOwner)
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }

        // ── SERVER: tắt physics hoàn toàn — ServerScene không có ground collider ──
        // Server chỉ relay position từ Owner client, không simulate physics.
        if (IsServer && !IsOwner && rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        // Non-owner CLIENT: tắt physics cục bộ, để syncPosition drive transform
        if (!IsOwner && !IsServer && rb != null)
        {
            rb.gravityScale = 0f;
            rb.velocity = Vector2.zero;
        }

        // Chỉ owner mới điều khiển input
        if (IsOwner)
        {
            // Đặt player vào đúng vị trí đích nếu vừa chuyển map qua portal
            PortalArrivalHandler.ApplyPendingArrival(transform);

            CameraFollow cameraFollow = CameraFollow.Instance ?? FindAnyObjectByType<CameraFollow>();
            if (cameraFollow != null)
            {
                cameraFollow.RefreshMaxMapBounds();
                cameraFollow.SetTarget(transform, true);
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

    private void DisableConflictingNetworkTransform()
    {
        if (networkTransform == null || !networkTransform.enabled)
            return;

        // Player movement của project này đã dùng custom owner-prediction + ServerRpc +
        // syncPosition. Giữ thêm NetworkTransform mặc định sẽ tạo double-authority và dễ
        // gây snap-back khi owner di chuyển.
        networkTransform.enabled = false;
    }

    private void OnScaleXChanged(float oldValue, float newValue)
    {
        // Chỉ non-owner mới cập nhật từ NetworkVariable.
        // Owner tự điều khiển flip qua client-side prediction trong FixedUpdate
        // để tránh stale server update ghi đè lên local prediction.
        if (!IsOwner)
            transform.localScale = new Vector3(newValue, _prefabScaleY, transform.localScale.z);
    }

    private void Update()
    {
        // Chỉ owner mới xử lý input
        if (!IsOwner) return;

        // Detect jump trong Update để không bị miss giữa 2 FixedUpdate
        var im = InputManager.Instance;
        if (im != null ? im.GetJumpPressed() : (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)))
            pendingJump = true;

        // Detect fall-through trong Update (cần GetKeyDown, không thể dùng GetKey)
        if (im != null ? im.GetFallThroughPressed()
                       : (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)))
            pendingFallThrough = true;
    }

    private bool _moveDiagLogged = false;
    private float _diagTimer = 0f;

    private void FixedUpdate()
    {
        // Non-owner: chỉ client thuần (không phải server) mới cần interpolate về server position.
        // Server KHÔNG chạy physics cho player — chỉ relay position.
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

        // === MOVEMENT DIAGNOSTICS — log chi tiết MỖI 2 giây để debug tại sao không di chuyển ===
        _diagTimer += Time.fixedDeltaTime;
        if (!_moveDiagLogged || _diagTimer >= 2f)
        {
            _moveDiagLogged = true;
            _diagTimer = 0f;
            bool statsOk = controller?.stats != null;
           // Debug.Log($"[NPC-DIAG] owner={IsOwner} server={IsServer} | ctrl={controller != null} stats={statsOk} moveSpeed={controller?.stats?.moveSpeed ?? -1f} | mv={movement != null} rb={rb != null} simulated={rb?.simulated} bodyType={rb?.bodyType} | input={horizontalInput:F2} inputEnabled={im?.inputEnabled} | pos={transform.position} vel={rb?.velocity} | scene={gameObject.scene.name}");
        }

        // === CLIENT-SIDE PREDICTION ===
        // Apply movement cục bộ ngay lập tức để owner thấy di chuyển không có độ trễ
        // Server sẽ relay position cho non-owner clients qua syncPosition
        bool isGrounded = false;
        if (controller?.stats != null && movement != null && rb != null)
        {
            movement.RefreshGroundCheck();
            isGrounded = movement.IsGrounded();
            PlayerStats stats = controller.stats;

            // Step climb: leo bậc thang nhỏ trước khi set velocity ngang
            movement.HandleStepClimb(horizontalInput);

            // Horizontal
            rb.velocity = new Vector2(horizontalInput * stats.moveSpeed, rb.velocity.y);

            // Instant flip cục bộ (giữ prefab Y scale, không chờ server roundtrip)
            if (horizontalInput > 0.01f)
                transform.localScale = new Vector3(1f, _prefabScaleY, 1f);
            else if (horizontalInput < -0.01f)
                transform.localScale = new Vector3(-1f, _prefabScaleY, 1f);

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

            // Fall-through one-way platform (S/DownArrow hoặc nút mobile ↓)
            if (pendingFallThrough)
            {
                pendingFallThrough = false;
                if (isGrounded)
                    movement.TryFallThroughPlatform();
            }

            // Update animation cục bộ ngay (không chờ UpdateAnimationClientRpc)
            var playerAnimator = movement.GetComponent<PlayerAnimator>();
            playerAnimator?.UpdateAnimation(rb.velocity.x, rb.velocity.y, isGrounded, movement.IsFlying());
        }
        else if (!_moveDiagLogged)
        {
            //Debug.LogError($"[NPC-DIAG] *** MOVEMENT BLOCKED *** ctrl={controller != null} stats={controller?.stats != null} mv={movement != null} rb={rb != null}");
        }

        // Gửi input + position thực tế (từ client có ground) lên server
        MoveServerRpc(horizontalInput, jump, down,
            rb != null ? rb.position : (Vector2)transform.position,
            rb != null ? rb.velocity.y : 0f,
            isGrounded);
    }

    private void LateUpdate()
    {
        // Server: sync position để tất cả clients có thể theo dõi vị trí player
        // Server KHÔNG dùng rb.position (vì rb đã bị Kinematic/disabled) mà dùng transform.position
        // (được set từ client report trong MoveServerRpc)
        if (IsServer)
        {
            syncPosition.Value = transform.position;
        }

        // Non-owner: đảm bảo scale luôn đúng mỗi frame (poll thay vì chỉ dùng event)
        if (!IsOwner)
        {
            float targetX = networkScaleX.Value;
            if (!Mathf.Approximately(transform.localScale.x, targetX))
                transform.localScale = new Vector3(targetX, _prefabScaleY, transform.localScale.z);
        }

        // Update animation trên owner (non-owner client dùng UpdateAnimationClientRpc)
        if (IsOwner && rb != null && movement != null)
        {
            PlayerAnimator playerAnimator = movement.GetComponent<PlayerAnimator>();
            if (playerAnimator != null)
            {
                movement.RefreshGroundCheck();
                playerAnimator.UpdateAnimation(rb.velocity.x, rb.velocity.y, movement.IsGrounded(), movement.IsFlying());
            }
        }
    }

    /// <summary>
    /// ServerRpc để gửi input + position từ client lên server.
    /// Server KHÔNG simulate physics (ServerScene không có ground collider).
    /// Thay vào đó, server relay position từ Owner → syncPosition → non-owner clients.
    /// </summary>
    [ServerRpc]
    private void MoveServerRpc(float horizontalInput, bool up, bool down,
        Vector2 clientPosition, float clientVelocityY, bool clientIsGrounded)
    {
        if (controller == null || controller.stats == null)
            return;

        PlayerStats stats = controller.stats;

        // ── 1. Basic anti-cheat: validate horizontal speed ──
        // (tránh client hack speed, chỉ check cơ bản)
        // Không block, chỉ clamp để tránh false-positive khi lag spike
        // TODO: nâng cấp anti-cheat với accumulation & threshold

        // ── 2. Update server transform từ client-reported position ──
        // Server tin tưởng client position (có ground collider) thay vì simulate
        transform.position = new Vector3(clientPosition.x, clientPosition.y, 0f);

        // ── 3. Flip sprite (server → sync cho tất cả clients qua NetworkVariable) ──
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

        // ── 4. Sync animation cho non-owner clients ──
        float velocityX = horizontalInput * stats.moveSpeed;
        bool isFlying = controller.godMode;
        UpdateAnimationClientRpc(velocityX, clientVelocityY, clientIsGrounded, isFlying);
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




