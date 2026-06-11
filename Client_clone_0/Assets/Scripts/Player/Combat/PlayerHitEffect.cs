using System.Collections;
using UnityEngine;
using Unity.Netcode;

// Hiệu ứng khi player bị trúng skill:
// 1. Sprite đổi màu xám trong grayOverlayDuration giây (tất cả client thấy)
// 2. Bất động (stun) trong stunDuration giây — chỉ ảnh hưởng owner
// Cách dùng:
// - Gắn component này lên Player Prefab (cùng object với NetworkPlayerHealth)
// - Không cần config thêm - tự động lắng nghe OnTakeDamage event
public class PlayerHitEffect : MonoBehaviour
{
    [Header("Gray Overlay Settings")]
    [Tooltip("Màu tint khi bị hit (đổi sprite sang màu xám)")]
    [SerializeField] private Color hitTintColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    [Tooltip("Thời gian hiệu ứng xám (giây)")]
    [SerializeField] private float grayOverlayDuration = 0.5f;

    [Header("Stun Settings")]
    [Tooltip("Thời gian bất động khi bị trúng skill - chỉ áp dụng cho owner (giây)")]
    [SerializeField] private float stunDuration = 0.5f;

    // Components
    private NetworkObject networkObject;
    private SpriteRenderer[] spriteRenderers;
    private PlayerMovement playerMovement;

    // State
    private Coroutine grayCoroutine;

    private void Awake()
    {
        networkObject   = GetComponent<NetworkObject>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        playerMovement  = GetComponent<PlayerMovement>();
    }

    private void Start()
    {
        // Subscribe tự động vào NetworkPlayerHealth.OnTakeDamage
        var netHealth = GetComponent<NetworkPlayerHealth>();
        if (netHealth != null)
            netHealth.OnTakeDamage.AddListener(OnHit);

        // Fallback standalone
        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnTakeDamage.AddListener(OnHit);
    }

    private void OnDestroy()
    {
        var netHealth = GetComponent<NetworkPlayerHealth>();
        if (netHealth != null)
            netHealth.OnTakeDamage.RemoveListener(OnHit);

        var health = GetComponent<PlayerHealth>();
        if (health != null)
            health.OnTakeDamage.RemoveListener(OnHit);
    }

    // Event Handler

    private void OnHit()
    {
        // Gray overlay hiển thị trên TẤT CẢ client
        ApplyGrayOverlay();

        // Stun chỉ ảnh hưởng owner của player đó
        bool isLocalOwner = networkObject != null ? networkObject.IsOwner : true;
        if (isLocalOwner)
            ApplyStun();
    }

    // Hàm public để script hoặc hệ thống khác gọi vào.

    // Flash màu xám lên tất cả SpriteRenderer của player.
    public void ApplyGrayOverlay()
    {
        if (grayCoroutine != null)
            StopCoroutine(grayCoroutine);
        grayCoroutine = StartCoroutine(GrayOverlayCoroutine());
    }

    // Bất động player (chặn input) trong stunDuration giây.
    public void ApplyStun()
    {
        if (playerMovement != null)
            playerMovement.SetStunned(stunDuration);
    }

    // Private

    private IEnumerator GrayOverlayCoroutine()
    {
        foreach (var sr in spriteRenderers)
            if (sr != null) sr.color = hitTintColor;

        yield return new WaitForSeconds(grayOverlayDuration);

        foreach (var sr in spriteRenderers)
            if (sr != null) sr.color = Color.white;

        grayCoroutine = null;
    }
}
