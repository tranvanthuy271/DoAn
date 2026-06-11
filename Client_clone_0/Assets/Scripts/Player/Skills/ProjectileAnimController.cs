using UnityEngine;

// Gắn vào projectile prefab cùng với Animator.
// Hoạt động với Cách 2 "Animation Event":
// - Clip chứa toàn bộ frames: [bay/fly] rồi đến [phát nổ/explode].
// - Tại frame đầu tiên của phần explode, đặt Animation Event gọi OnAnimationCheckpoint().
// - Nếu chưa có hit: tua về frame 0 → tiếp tục lặp phần fly.
// - Nếu đã có hit: không làm gì → animation tự chạy tiếp phần explode.
// Cách kích hoạt:
// Script damage (DotDamage, FireballDamage...) gọi MarkHit() khi va chạm.
[RequireComponent(typeof(Animator))]
public class ProjectileAnimController : MonoBehaviour
{
    // Cờ trạng thái — DotDamage hoặc FireballDamage sẽ gọi MarkHit() để set true
    private bool hasHit = false;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Force-start animation on ALL instances (server, host, and client).
        // Ensures the fly-loop plays even if Animator "Play On Awake" is disabled
        // or the default state needs an explicit evaluation to begin.
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    // Gọi từ script damage khi projectile trúng mục tiêu.
    public void MarkHit()
    {
        hasHit = true;
    }

    // Được gọi bởi Animation Event tại frame checkpoint trong clip.
    // Đặt event này tại frame đầu tiên của phần explosion.
    public void OnAnimationCheckpoint()
    {
        if (!hasHit)
        {
            // Chưa trúng → tua về frame 0, tiếp tục lặp phần fly
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);
        }
        // Đã trúng → không làm gì, animation tự chạy tiếp phần explosion
    }
}
