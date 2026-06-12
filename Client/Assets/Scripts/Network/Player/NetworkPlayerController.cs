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
    private NetworkPlayerHealth health;

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

    // NetworkVariable sync velocity cho non-owner clients — dùng để extrapolate (dự đoán) vị trí
    // giữa các lần nhận syncPosition, giúp di chuyển mượt mà hơn.
    private NetworkVariable<Vector2> syncVelocity = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Snapshot interpolation: thay vì extrapolate (dự đoán tương lai → dễ overshoot rồi
    // giật ngược khi latency VPS cao), ta render trễ một nhịp nhỏ và NỘI SUY giữa 2 mốc
    // vị trí đã thực sự nhận được. Mượt và không bao giờ giật lùi.
    private struct PosSnapshot { public float time; public Vector2 pos; }

    [Header("Remote Interpolation")]
    private readonly System.Collections.Generic.List<PosSnapshot> _snapshots = new System.Collections.Generic.List<PosSnapshot>(32);
    // Độ trễ render (giây). ~120ms đủ che giấu jitter mạng của VPS mà vẫn phản hồi nhanh.
    private const float InterpolationDelay = 0.12f;
    // Khoảng cách coi là teleport → snap ngay thay vì nội suy.
    private const float SnapDistance = 5f;

    private void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        controller = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        networkTransform = GetComponent<NetworkTransform>();
        health = GetComponent<NetworkPlayerHealth>();
        _prefabScaleY = Mathf.Abs(transform.localScale.y); // cache trước khi bị ghi đè

        if (GetComponent<PlayerClickHandler>() == null)
            gameObject.AddComponent<PlayerClickHandler>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
        bool isLocalPlayer = NetworkObject != null && NetworkObject.IsLocalPlayer;
        bool isPlayerObject = NetworkObject != null && NetworkObject.IsPlayerObject;
        { /* OnNetworkSpawn obj={gameObject.name}, scene={gameObject.scene.name}, netId={NetworkObjectId}, owner={OwnerClientId}, localClient={localClientId}, isServer={IsServer}, isClient={IsClient}, isOwner={IsOwner}, isLocalPlayer={isLocalPlayer}, isPlayerObject={isPlayerObject} */ }

        DisableConflictingNetworkTransform();

        // Subscribe to networkScaleX changes để sync flip direction
        networkScaleX.OnValueChanged += OnScaleXChanged;

        // Đăng ký nhận sự thay đổi position để nạp snapshot cho nội suy
        syncPosition.OnValueChanged += OnSyncPositionChanged;
        // Nạp mốc khởi đầu
        _snapshots.Clear();
        _snapshots.Add(new PosSnapshot { time = Time.time, pos = syncPosition.Value });

        // Khởi tạo scale theo giá trị hiện tại của NetworkVariable (giữ Y scale gốc từ prefab)
        transform.localScale = new Vector3(networkScaleX.Value, _prefabScaleY, 1);

        // Đảm bảo controller được enable (PlayerController đã có check IsOwner)
        if (controller != null && !controller.enabled)
        {
            controller.enabled = true;
        }

        // SERVER: player non-owner phải GIỮ collider sống trong physics scene để
        // enemy/boss/fireball/melee dò trúng được (OverlapCircle + OnTriggerEnter2D).
        // Dùng Kinematic để server KHÔNG tự simulate gravity/forces (vị trí do client
        // report qua MoveServerRpc), nhưng simulated=true để collider vẫn nằm trong
        // physics scene → skill/projectile mới trừ được HP.
        // (Trước đây simulated=false làm collider biến mất → trên VPS không ai ăn damage.)
        if (IsServer && !IsOwner && rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
            rb.gravityScale = 0f;
        }

        // Non-owner CLIENT: tắt physics hoàn toàn - transform do InterpolateRemotePlayer() drive.
        // Giữ rb.simulated=false để physics engine không can thiệp vào vị trí (tránh
        // micro-jitter khi 2 player overlap và collider đẩy nhau).
        if (!IsOwner && !IsServer && rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
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
                { /* Camera target set to {gameObject.name} */ }
            }
            else
            {
                { /* Cảnh báo: CameraFollow not found! Client player will not be followed by camera */ }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        { /* OnNetworkDespawn obj={gameObject.name}, scene={gameObject.scene.name}, netId={NetworkObjectId}, owner={OwnerClientId}, isServer={IsServer}, isClient={IsClient}, isOwner={IsOwner} */ }
        networkScaleX.OnValueChanged -= OnScaleXChanged;
        syncPosition.OnValueChanged -= OnSyncPositionChanged;
        base.OnNetworkDespawn();
    }

    private void OnSyncPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        // Nạp mốc vị trí mới kèm thời điểm nhận. InterpolateRemotePlayer() sẽ nội suy
        // giữa các mốc này với độ trễ InterpolationDelay.
        _snapshots.Add(new PosSnapshot { time = Time.time, pos = newPos });

        // Giữ buffer gọn: chỉ cần lịch sử ~1s.
        float cutoff = Time.time - 1f;
        while (_snapshots.Count > 2 && _snapshots[0].time < cutoff)
            _snapshots.RemoveAt(0);
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
        if (health != null && health.IsDead()) return;

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
    private float _diagTimer;

    private void FixedUpdate()
    {
        // Non-owner: interpolation giờ được xử lý trong Update() để chạy mỗi frame render.
        // FixedUpdate chỉ dùng cho owner physics.
        if (!IsOwner)
        {
            return;
        }

        if (health != null && health.IsDead())
        {
            if (rb != null)
                rb.velocity = Vector2.zero;
            pendingJump = false;
            pendingFallThrough = false;
            return;
        }

        // Owner gửi input lên server MỖI FixedUpdate để velocity luôn được apply liên tục

        var im = InputManager.Instance;
        float horizontalInput = im != null ? im.GetHorizontalInput() : Input.GetAxisRaw("Horizontal");
        float verticalAxis = im != null ? im.GetVerticalInput() : Input.GetAxisRaw("Vertical");
        bool down = verticalAxis < -0.1f || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool jump = pendingJump;
        pendingJump = false; // consume flag
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

    /// <summary>
    /// Non-owner: nội suy mượt cho remote player bằng SNAPSHOT INTERPOLATION.
    /// Render ở thời điểm (now - InterpolationDelay) bằng cách nội suy tuyến tính giữa 2
    /// snapshot bao quanh thời điểm đó. Không extrapolate nên không bao giờ overshoot/giật lùi.
    /// Chạy trong LateUpdate (mỗi frame render) để mượt theo framerate.
    /// </summary>
    private void InterpolateRemotePlayer()
    {
        if (IsOwner || IsServer) return;
        if (_snapshots.Count == 0) return;

        float renderTime = Time.time - InterpolationDelay;
        Vector2 currentPos = (Vector2)transform.position;

        // Mốc mới nhất — dùng để snap khi teleport và làm fallback.
        Vector2 latestPos = _snapshots[_snapshots.Count - 1].pos;

        // Snap khi teleport / respawn / first sync (khoảng cách quá lớn).
        if (Vector2.Distance(currentPos, latestPos) > SnapDistance)
        {
            transform.position = new Vector3(latestPos.x, latestPos.y, transform.position.z);
            return;
        }

        Vector2 targetPos;

        if (renderTime <= _snapshots[0].time)
        {
            // Chưa đủ lịch sử để render quá khứ → bám mốc cũ nhất.
            targetPos = _snapshots[0].pos;
        }
        else if (renderTime >= _snapshots[_snapshots.Count - 1].time)
        {
            // renderTime vượt mốc mới nhất (mạng đang trễ hơn cả buffer) → giữ mốc mới nhất,
            // KHÔNG dự đoán tiếp để tránh overshoot.
            targetPos = latestPos;
        }
        else
        {
            // Tìm cặp snapshot [i, i+1] bao quanh renderTime rồi nội suy tuyến tính.
            targetPos = latestPos;
            for (int i = 0; i < _snapshots.Count - 1; i++)
            {
                if (renderTime >= _snapshots[i].time && renderTime <= _snapshots[i + 1].time)
                {
                    float span = _snapshots[i + 1].time - _snapshots[i].time;
                    float t = span > 0.0001f ? (renderTime - _snapshots[i].time) / span : 1f;
                    targetPos = Vector2.Lerp(_snapshots[i].pos, _snapshots[i + 1].pos, t);
                    break;
                }
            }
        }

        transform.position = new Vector3(targetPos.x, targetPos.y, transform.position.z);
    }

    private void LateUpdate()
    {
        // Non-owner: interpolation mượt mà — chạy mỗi frame render
        InterpolateRemotePlayer();

        // Server: sync position + velocity để tất cả clients có thể theo dõi vị trí player
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
            if (health != null && health.IsDead())
                return;

            PlayerAnimator playerAnimator = movement.GetComponent<PlayerAnimator>();
            if (playerAnimator != null)
            {
                movement.RefreshGroundCheck();
                playerAnimator.UpdateAnimation(rb.velocity.x, rb.velocity.y, movement.IsGrounded(), movement.IsFlying());
            }
        }
    }

    // ServerRpc để gửi input + position từ client lên server.
    // Server KHÔNG simulate physics (ServerScene không có ground collider).
    // Thay vào đó, server relay position từ Owner → syncPosition → non-owner clients.
    [ServerRpc]
    private void MoveServerRpc(float horizontalInput, bool up, bool down,
        Vector2 clientPosition, float clientVelocityY, bool clientIsGrounded)
    {
        if (controller == null || controller.stats == null)
            return;

        PlayerStats stats = controller.stats;

        // 1. Server nhận input + position từ client owner
        transform.position = new Vector3(clientPosition.x, clientPosition.y, 0f);

        // 2. Sync velocity cho remote clients (dùng để extrapolate vị trí giữa network ticks)
        // Khi không có input ngang, force velocity.x = 0 để remote player dừng ngay
        // thay vì trôi do stale velocity.
        float velX = Mathf.Abs(horizontalInput) > 0.01f ? horizontalInput * stats.moveSpeed : 0f;
        Vector2 vel = new Vector2(velX, clientVelocityY);
        syncVelocity.Value = vel;

        // 3. Flip sprite (server → sync cho tất cả clients qua NetworkVariable)
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

        // 4. Sync animation cho non-owner clients
        float velocityX = horizontalInput * stats.moveSpeed;
        bool isFlying = controller.godMode;
        UpdateAnimationClientRpc(velocityX, clientVelocityY, clientIsGrounded, isFlying);
    }

    // ClientRpc để sync animation parameters cho tất cả clients
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




