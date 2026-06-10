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
    private bool _isDead;

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

        if (_isDead)
        {
            animator.SetFloat(Speed, 0f);
            animator.SetFloat(VelocityY, 0f);
            SetBoolIfExists("IsDead", true);
            return;
        }

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
        _isDead = dead;
        if (animator == null) return;

        if (dead)
        {
            TriggerDie();
            return;
        }

        ResetToIdleAfterRespawn();
    }

    public void ResetToIdleAfterRespawn()
    {
        _isDead = false;
        if (animator == null) return;

        animator.enabled = true;
        animator.speed = 1f;

        SetBoolIfExists("IsDead", false);
        SetBoolIfExists("IsGrounded", true);
        SetBoolIfExists("IsFlying", false);
        SetFloatIfExists("Speed", 0f);
        SetFloatIfExists("VelocityY", 0f);
        ResetTriggerIfExists("Die", "die", "Death", "death", "Attack", "attack");

        if (!PlayStateIfExists("idle", "Idle"))
        {
            animator.Rebind();
            SetBoolIfExists("IsDead", false);
            SetBoolIfExists("IsGrounded", true);
            SetBoolIfExists("IsFlying", false);
            SetFloatIfExists("Speed", 0f);
            SetFloatIfExists("VelocityY", 0f);
        }

        animator.Update(0f);
        ResetDeathTint();
    }

    public void TriggerDie()
    {
        if (animator == null) return;

        _isDead = true;
        animator.SetFloat(Speed, 0f);
        animator.SetFloat(VelocityY, 0f);
        SetBoolIfExists("IsDead", true);

        foreach (var p in animator.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Trigger) continue;
            if (p.name == "Die" || p.name == "die" || p.name == "Death" || p.name == "death")
            {
                animator.SetTrigger(p.name);
                return;
            }
        }

        if (HasState("Die")) animator.CrossFade("Die", 0.05f);
        else if (HasState("die")) animator.CrossFade("die", 0.05f);
        else if (HasState("Death")) animator.CrossFade("Death", 0.05f);
        else if (HasState("death")) animator.CrossFade("death", 0.05f);
    }

    private bool HasState(string stateName)
    {
        return animator != null
               && (animator.HasState(0, Animator.StringToHash(stateName))
                   || animator.HasState(0, Animator.StringToHash("Base Layer." + stateName)));
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (animator == null) return;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == parameterName)
            {
                animator.SetBool(p.name, value);
                return;
            }
        }
    }

    private void SetFloatIfExists(string parameterName, float value)
    {
        if (animator == null) return;
        foreach (var p in animator.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Float && p.name == parameterName)
            {
                animator.SetFloat(p.name, value);
                return;
            }
        }
    }

    private void ResetTriggerIfExists(params string[] parameterNames)
    {
        if (animator == null) return;
        foreach (var p in animator.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Trigger) continue;
            foreach (var name in parameterNames)
            {
                if (p.name == name)
                {
                    animator.ResetTrigger(p.name);
                    break;
                }
            }
        }
    }

    private bool PlayStateIfExists(params string[] stateNames)
    {
        if (animator == null) return false;
        foreach (var stateName in stateNames)
        {
            if (animator.HasState(0, Animator.StringToHash(stateName)))
            {
                animator.Play(stateName, 0, 0f);
                return true;
            }

            string baseLayerStateName = "Base Layer." + stateName;
            if (animator.HasState(0, Animator.StringToHash(baseLayerStateName)))
            {
                animator.Play(baseLayerStateName, 0, 0f);
                return true;
            }
        }

        return false;
    }

    private void ResetDeathTint()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var renderer in renderers)
        {
            Color color = renderer.color;
            bool looksLikeDeathTint = Mathf.Abs(color.r - color.g) < 0.03f
                                      && Mathf.Abs(color.g - color.b) < 0.03f
                                      && color.r < 0.95f;

            if (renderer == spriteRenderer || looksLikeDeathTint)
                renderer.color = new Color(1f, 1f, 1f, color.a);
        }
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

