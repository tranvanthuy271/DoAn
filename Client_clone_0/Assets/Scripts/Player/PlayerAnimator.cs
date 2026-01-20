using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [Header("Components")]
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Animation Parameters")]
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int VelocityY = Animator.StringToHash("VelocityY");
    private static readonly int IsFlying = Animator.StringToHash("IsFlying");

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
    }

    public void UpdateAnimation(float horizontalInput, float velocityY, bool isGrounded, bool isFlying)
    {
        if (animator == null) return;

        // Update animator parameters
        animator.SetFloat(Speed, Mathf.Abs(horizontalInput));
        animator.SetBool(IsGrounded, isGrounded);
        animator.SetFloat(VelocityY, velocityY);
        animator.SetBool(IsFlying, isFlying);
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

