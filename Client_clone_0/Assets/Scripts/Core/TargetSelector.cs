using UnityEngine;

/// <summary>
/// TargetSelector — Static registry theo dõi mục tiêu đang được chọn (enemy hoặc NPC).
/// Dùng để hệ thống auto-move (PlayerSkillManager) biết cần di chuyển về đâu
/// khi người chơi nhấn phím tấn công / Enter.
/// </summary>
public static class TargetSelector
{
    public static Transform CurrentTarget { get; private set; }

    public static bool HasTarget =>
        CurrentTarget != null && CurrentTarget.gameObject != null;

    /// <summary>Đặt mục tiêu mới. Gọi từ EnemyClickHandler.Select() hoặc NpcInteraction.SelectThis().</summary>
    public static void SetTarget(Transform t)
    {
        CurrentTarget = t;
    }

    /// <summary>Xóa mục tiêu. Chỉ xóa nếu mục tiêu hiện tại trùng với <paramref name="ifSameAs"/> (hoặc null = luôn xóa).</summary>
    public static void ClearTarget(Transform ifSameAs = null)
    {
        if (ifSameAs == null || CurrentTarget == ifSameAs)
            CurrentTarget = null;
    }
}
