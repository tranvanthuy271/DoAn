using UnityEngine;
using Unity.Netcode.Components;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Components")]
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Animation Parameters")]
    private static readonly int Speed      = Animator.StringToHash("Speed");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int VelocityY  = Animator.StringToHash("VelocityY");
    private static readonly int IsFlying   = Animator.StringToHash("IsFlying");
    private static readonly int IsDead     = Animator.StringToHash("IsDead");
    private static readonly int Attack     = Animator.StringToHash("Attack");
    private static readonly int AttackLower = Animator.StringToHash("attack");

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // If no animator, add one for future use
        if (animator == null)
        {
            animator = gameObject.AddComponent<Animator>();
        }

        // If no sprite renderer, add one
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // Nếu NetworkAnimator có mặt nhưng chưa được gán Animator → auto-assign
        var networkAnimator = GetComponent<NetworkAnimator>();
        if (networkAnimator != null && networkAnimator.Animator == null)
        {
            networkAnimator.Animator = animator;
        }
    }

    public void UpdateAnimation(float speed, float velocityY, bool isGrounded, bool isFlying)
    {
        if (animator == null) return;

        // QUAN TRỌNG: Khi đang ở mặt đất, force VelocityY = 0 để tránh animation Jump chạy liên tục
        // Vì physics engine có thể tạo ra velocityY nhỏ dù đang ở mặt đất
        float finalVelocityY = isGrounded ? 0f : velocityY;
        
        // Tính toán Speed - khi đứng yên trên mặt đất, đảm bảo Speed = 0
        float finalSpeed = Mathf.Abs(speed);
        // Nếu đang ở mặt đất và không di chuyển, force Speed = 0 (tránh giá trị nhỏ do physics)
        if (isGrounded && finalSpeed < 0.1f)
        {
            finalSpeed = 0f;
        }

        // Update animator parameters
        // Speed: Dùng giá trị tốc độ thực tế (velocity) thay vì input để transition hoạt động đúng
        animator.SetFloat(Speed, finalSpeed);
        animator.SetBool(IsGrounded, isGrounded);
        animator.SetFloat(VelocityY, finalVelocityY);
        animator.SetBool(IsFlying, isFlying);

        // Fallback: nếu animator bị kẹt cuối attack clip, cưỡng bức quay lại state di chuyển.
        RecoverFromStuckAttack(finalSpeed, isGrounded);
        
        // Debug log đã tắt
        // Nếu cần debug, uncomment dòng dưới:
        // if (animator.GetCurrentAnimatorStateInfo(0).IsName("Jump") && isGrounded)
        // {
        //     Debug.LogWarning($"[PlayerAnimator] ĐANG Ở JUMP STATE KHI ĐỨNG YÊN! Speed: {finalSpeed:F3}, VelocityY: {finalVelocityY:F3}, IsGrounded: {isGrounded}, IsFlying: {isFlying}");
        // }
    }

    private void RecoverFromStuckAttack(float finalSpeed, bool isGrounded)
    {
        if (animator == null) return;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        bool inAttackState = state.shortNameHash == Attack || state.shortNameHash == AttackLower || state.IsName("attack") || state.IsName("Attack");

        if (!inAttackState || animator.IsInTransition(0))
            return;

        if (state.normalizedTime < 0.98f)
            return;

        if (!isGrounded)
            animator.CrossFade("jump", 0.05f);
        else if (finalSpeed > 0.1f)
            animator.CrossFade("run", 0.05f);
        else
            animator.CrossFade("idle", 0.05f);
    }
    
    private string GetCurrentStateName()
    {
        if (animator == null) return "No Animator";
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName("Jump") ? "Jump" : (stateInfo.IsName("Run") ? "Run" : (stateInfo.IsName("Idle") ? "Idle" : "Unknown"));
    }

    public void SetDead(bool dead)
    {
        if (animator != null)
            animator.SetBool(IsDead, dead);
    }

    public void TriggerAttack()
    {
        if (animator == null) return;

        // Thử cả hai biến thể "Attack" và "attack" vì Animator Controller có thể dùng bất kỳ
        foreach (var p in animator.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Trigger) continue;
            if (p.name == "Attack" || p.name == "attack")
            {
                animator.SetTrigger(p.name);
                return; // chỉ trigger một lần
            }
        }
        // Fallback nếu không tìm thấy — dùng hash gốc
        animator.SetTrigger(Attack);
    }

    public void PlayAnimation(string animationName)
    {
        if (animator != null)
        {
            animator.Play(animationName);
        }
    }

    public void SetAnimationSpeed(float speed)
    {
        if (animator != null)
        {
            animator.speed = speed;
        }
    }
}

