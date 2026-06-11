using UnityEngine;

// Test animator của player thủ công bằng phím tắt.
// Gắn script này vào cùng GameObject với PlayerAnimator để test local.
// Phím tắt (chỉ hoạt động khi KHÔNG có NetworkObject, hoặc là Owner):
// ← → Arrow / A D  : test Run / idle (Speed)
// W / ↑             : test Jump (IsGrounded = false, VelocityY > 0)
// N                 : test Attack trigger
// K                 : test Die (IsDead = true)
// R                 : Reset về idle (IsDead = false, IsGrounded = true)
// [Space]           : in ra trạng thái hiện tại vào Console
public class PlayerAnimatorTester : MonoBehaviour
{
    [Header("Test Settings")]
    [Tooltip("Bật/tắt tester này. Nên tắt trước khi build release.")]
    public bool enableTester = true;

    [Header("Simulated Values")]
    [Range(0f, 10f)] public float simulatedSpeed    = 0f;
    [Range(-20f, 20f)] public float simulatedVelocityY = 0f;
    public bool simulatedIsGrounded = true;
    public bool simulatedIsFlying   = false;

    private PlayerAnimator playerAnimator;
    private Animator       animator;

    // state nội bộ – dùng để giả lập nhảy 1 frame rồi rơi
    private float jumpTimer = 0f;
    private bool  isJumping = false;

    private void Awake()
    {
        playerAnimator = GetComponent<PlayerAnimator>();
        animator       = GetComponent<Animator>();
    }

    private void Start()
    {
        if (!enableTester) return;

        if (playerAnimator == null)
            Debug.LogWarning("[PlayerAnimatorTester] Không tìm thấy PlayerAnimator trên GameObject này!");

        if (animator == null)
            Debug.LogWarning("[PlayerAnimatorTester] Không tìm thấy Animator trên GameObject này!");

        Debug.Log("[PlayerAnimatorTester] Đang chạy. Xem header của script để biết phím tắt.");
    }

    private void Update()
    {
        if (!enableTester || playerAnimator == null) return;

        HandleJumpSimulation();
        HandleKeyInput();

        // Đẩy giá trị vào animator mỗi frame để phản ánh thay đổi từ Inspector
        playerAnimator.UpdateAnimation(simulatedSpeed, simulatedVelocityY, simulatedIsGrounded, simulatedIsFlying);
    }

    // Giả lập vòng bay lên rồi rơi xuống
    private void HandleJumpSimulation()
    {
        if (!isJumping) return;

        jumpTimer -= Time.deltaTime;

        if (jumpTimer > 0.25f)
        {
            // Đang bay lên
            simulatedVelocityY  =  8f;
            simulatedIsGrounded = false;
        }
        else if (jumpTimer > 0f)
        {
            // Bắt đầu rơi
            simulatedVelocityY  = -5f;
            simulatedIsGrounded = false;
        }
        else
        {
            // Chạm đất
            simulatedVelocityY  = 0f;
            simulatedIsGrounded = true;
            isJumping           = false;
        }
    }

    // Xử lý phím bấm
    private void HandleKeyInput()
    {
        // Chạy
        bool left  = Input.GetKey(KeyCode.LeftArrow)  || Input.GetKey(KeyCode.A);
        bool right = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);

        if (!isJumping)  // không ghi đè khi đang nhảy
        {
            if (right)
            {
                simulatedSpeed = 5f;
                transform.localScale = new Vector3(1, 1, 1);
            }
            else if (left)
            {
                simulatedSpeed = 5f;
                transform.localScale = new Vector3(-1, 1, 1);
            }
            else
            {
                simulatedSpeed = 0f;
            }
        }

        // Nhảy (W / ↑) – chỉ cho nhảy khi đang đứng đất
        if ((Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            && simulatedIsGrounded && !isJumping)
        {
            isJumping  = true;
            jumpTimer  = 0.5f;   // tổng thời gian bay lên + rơi xuống (giây)
            Debug.Log("[PlayerAnimatorTester] Jump!");
        }

        // Tấn công (N)
        if (Input.GetKeyDown(KeyCode.N))
        {
            playerAnimator.TriggerAttack();
            Debug.Log("[PlayerAnimatorTester] Attack triggered!");
        }

        // Die (K)
        if (Input.GetKeyDown(KeyCode.K))
        {
            playerAnimator.SetDead(true);
            simulatedSpeed      = 0f;
            simulatedVelocityY  = 0f;
            simulatedIsGrounded = true;
            Debug.Log("[PlayerAnimatorTester] Die!");
        }

        // Reset (R)
        if (Input.GetKeyDown(KeyCode.R))
        {
            playerAnimator.SetDead(false);
            simulatedSpeed      = 0f;
            simulatedVelocityY  = 0f;
            simulatedIsGrounded = true;
            simulatedIsFlying   = false;
            isJumping           = false;
            Debug.Log("[PlayerAnimatorTester] Reset → Idle");
        }

        // In trạng thái hiện tại ra Console (Space)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (animator != null)
            {
                var info = animator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"[PlayerAnimatorTester] Current state hash={info.shortNameHash} | " +
                          $"Speed={simulatedSpeed} VelocityY={simulatedVelocityY} " +
                          $"IsGrounded={simulatedIsGrounded} IsFlying={simulatedIsFlying}");
            }
        }
    }

    // Hiển thị UI nhanh trên màn hình Game View khi đang Play
    private void OnGUI()
    {
        if (!enableTester) return;

        GUILayout.BeginArea(new Rect(10, 10, 280, 200));
        GUILayout.Box("── PlayerAnimatorTester ──\n" +
                      "← → / A D : Chạy\n" +
                      "W / ↑      : Nhảy\n" +
                      "N          : Tấn công\n" +
                      "K          : Die\n" +
                      "R          : Reset → Idle\n" +
                      "Space      : In state ra Console");
        GUILayout.EndArea();
    }
}
