using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

/// <summary>
/// Script helper để kiểm tra và cảnh báo về NetworkAnimator setup
/// Gán script này vào NetworkPlayer prefab để tự động check
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkAnimatorChecker : MonoBehaviour
{
    private void Start()
    {
        CheckNetworkAnimator();
    }

    private void CheckNetworkAnimator()
    {
        NetworkAnimator networkAnimator = GetComponent<NetworkAnimator>();
        Animator animator = GetComponent<Animator>();

        if (networkAnimator != null)
        {
            // Có NetworkAnimator component
            if (animator == null)
            {
                Debug.LogError($"[NetworkAnimatorChecker] {gameObject.name}: Có NetworkAnimator nhưng KHÔNG CÓ Animator component! " +
                    "Hãy thêm Animator component hoặc xóa NetworkAnimator.", this);
            }
            else if (networkAnimator.Animator == null)
            {
                Debug.LogError($"[NetworkAnimatorChecker] {gameObject.name}: NetworkAnimator.Animator chưa được gán! " +
                    "Hãy gán Animator component vào NetworkAnimator → Animator field trong Inspector.", this);
            }
            else
            {
                Debug.Log($"[NetworkAnimatorChecker] {gameObject.name}: NetworkAnimator setup đúng ✓", this);
            }
        }
        else
        {
            // Không có NetworkAnimator - OK nếu không dùng animation sync
            if (animator != null)
            {
                Debug.Log($"[NetworkAnimatorChecker] {gameObject.name}: Có Animator nhưng không có NetworkAnimator. " +
                    "Animation sẽ chỉ chạy local (không sync network). Nếu muốn sync animation, thêm NetworkAnimator component.", this);
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Check NetworkAnimator Setup")]
    private void CheckNetworkAnimatorEditor()
    {
        CheckNetworkAnimator();
    }
#endif
}

