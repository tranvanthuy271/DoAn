using UnityEngine;
using Unity.Netcode;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2, -10);
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private bool followX = true;
    [SerializeField] private bool followY = true;

    [Header("Network Settings")]
    [SerializeField] private bool followLocalPlayerOnly = true;

    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    private NetworkManager networkManager;

    private void Start()
    {
        networkManager = NetworkManager.Singleton;

        // Auto-find player if not assigned
        if (target == null)
        {
            FindLocalPlayer();
        }
    }

    private void FindLocalPlayer()
    {
        // Nếu có network và cần follow local player
        if (networkManager != null && networkManager.IsClient && followLocalPlayerOnly)
        {
            // Tìm local player (player có IsOwner = true)
            NetworkPlayerController[] players = FindObjectsOfType<NetworkPlayerController>();
            foreach (var player in players)
            {
                if (player.IsOwner)
                {
                    target = player.transform;
                    Debug.Log($"Camera following local player (ClientId: {networkManager.LocalClientId})");
                    return;
                }
            }
        }

        // Fallback: tìm player đầu tiên (cho single player hoặc khi không có network)
        var playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            target = playerController.transform;
        }
    }

    // Method để player gọi khi spawn (từ NetworkPlayerController)
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;

        // Optional: Don't follow on certain axes
        if (!followX)
        {
            desiredPosition.x = transform.position.x;
        }
        if (!followY)
        {
            desiredPosition.y = transform.position.y;
        }

        // Smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Apply bounds if enabled
        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
        }

        transform.position = smoothedPosition;
    }

    // Gizmos to visualize bounds
    private void OnDrawGizmosSelected()
    {
        if (!useBounds) return;

        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(
            (minBounds.x + maxBounds.x) / 2,
            (minBounds.y + maxBounds.y) / 2,
            0
        );
        Vector3 size = new Vector3(
            maxBounds.x - minBounds.x,
            maxBounds.y - minBounds.y,
            1
        );
        Gizmos.DrawWireCube(center, size);
    }
}

