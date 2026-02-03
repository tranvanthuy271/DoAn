using UnityEngine;
using Unity.Netcode;

/// <summary>
/// PlayerPickup - Nhấn phím P để nhặt tất cả item xung quanh player trong một bán kính.
/// Gắn script này lên cùng GameObject có NetworkObject (thường là Player prefab).
/// </summary>
public class PlayerPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    [Tooltip("Bán kính nhặt item xung quanh player")]
    [SerializeField] private float pickupRange = 1.8f;

    [Tooltip("Layer của item (có thể để All và lọc bằng ItemPickup)")]
    [SerializeField] private LayerMask itemLayer = ~0; // mặc định: tất cả layer

    private NetworkObject networkObject;

    private void Awake()
    {
        networkObject = GetComponent<NetworkObject>();
    }

    private void Update()
    {
        // Chỉ owner mới được nhấn phím P để nhặt
        if (networkObject != null && NetworkManager.Singleton != null && !networkObject.IsOwner)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            TryPickupAround();
        }
    }

    /// <summary>
    /// Tìm tất cả ItemPickup quanh player và yêu cầu nhặt
    /// </summary>
    private void TryPickupAround()
    {
        if (networkObject == null || NetworkManager.Singleton == null) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            pickupRange,
            itemLayer
        );

        foreach (Collider2D hit in hits)
        {
            ItemPickup item = hit.GetComponent<ItemPickup>();
            if (item != null)
            {
                // Gửi request nhặt item cho server thông qua ItemPickup
                item.RequestPickup(networkObject.NetworkObjectId);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}

