using UnityEngine;

/// <summary>
/// Script điều khiển di chuyển của projectile cho Skill2
/// Đảm bảo projectile di chuyển với tốc độ cố định theo hướng đã set
/// </summary>
public class ProjectileMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("Tốc độ di chuyển của projectile (units/second). Nếu <= 0 sẽ dùng velocity từ Rigidbody2D")]
    [SerializeField] private float moveSpeed = 10f;
    
    [Tooltip("Hướng di chuyển (1 = phải, -1 = trái). Nếu = 0 sẽ tự động xác định từ velocity")]
    [SerializeField] private float direction = 1f;
    
    [Tooltip("Thời gian sống của projectile (seconds). Đặt 0 để không tự hủy")]
    [SerializeField] private float lifetime = 3f;
    
    [Tooltip("Khoảng cách tối đa projectile có thể bay (units). Đặt 0 để không giới hạn")]
    [SerializeField] private float maxDistance = 0f;
    
    [Header("Auto Setup")]
    [Tooltip("Tự động set velocity từ Rigidbody2D nếu có")]
    [SerializeField] private bool autoSetupFromRigidbody = true;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        // Nếu có Rigidbody2D và autoSetupFromRigidbody = true
        if (rb != null && autoSetupFromRigidbody)
        {
            // Xác định hướng từ velocity hiện tại
            if (rb.velocity.x != 0f)
            {
                direction = Mathf.Sign(rb.velocity.x);
                if (moveSpeed <= 0f)
                {
                    moveSpeed = Mathf.Abs(rb.velocity.x);
                }
            }
        }

        // Set velocity nếu có Rigidbody2D
        if (rb != null && moveSpeed > 0f)
        {
            // Set velocity Y = 0 để projectile bay ngang (không rơi)
            rb.velocity = new Vector2(direction * moveSpeed, 0f);
            
            // Đảm bảo gravity scale = 0
            rb.gravityScale = 0f;
        }

        // Tự động hủy sau lifetime
        if (lifetime > 0f)
        {
            Destroy(gameObject, lifetime);
        }

        isInitialized = true;
    }

    private void FixedUpdate()
    {
        // Đảm bảo velocity luôn đúng (tránh bị override bởi script khác hoặc animation)
        if (rb != null && moveSpeed > 0f)
        {
            // Force velocity X và Y = 0 (bay ngang, không rơi)
            // Điều này đảm bảo animation không thể override movement
            rb.velocity = new Vector2(direction * moveSpeed, 0f);
            
            // Đảm bảo gravity scale = 0
            if (rb.gravityScale != 0f)
            {
                rb.gravityScale = 0f;
            }
        }
        else if (rb == null && moveSpeed > 0f)
        {
            // Nếu không có Rigidbody2D, dùng transform.position
            transform.position += new Vector3(direction * moveSpeed * Time.fixedDeltaTime, 0f, 0f);
        }

        // Kiểm tra khoảng cách tối đa
        if (maxDistance > 0f)
        {
            float distanceTraveled = Vector2.Distance(startPosition, transform.position);
            if (distanceTraveled >= maxDistance)
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Set tốc độ di chuyển của projectile
    /// </summary>
    public void SetSpeed(float speed)
    {
        moveSpeed = speed;
        if (rb != null && isInitialized)
        {
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        }
    }

    /// <summary>
    /// Set hướng di chuyển (1 = phải, -1 = trái)
    /// </summary>
    public void SetDirection(float dir)
    {
        direction = Mathf.Sign(dir);
        if (rb != null && isInitialized)
        {
            rb.velocity = new Vector2(direction * moveSpeed, rb.velocity.y);
        }
    }

    /// <summary>
    /// Set cả tốc độ và hướng
    /// </summary>
    public void SetMovement(float speed, float dir)
    {
        moveSpeed = speed;
        direction = Mathf.Sign(dir);
        
        // Đảm bảo đã initialize trước khi set movement
        if (!isInitialized)
        {
            Initialize();
        }
        
        if (rb != null)
        {
            // Force velocity ngay lập tức
            rb.velocity = new Vector2(direction * moveSpeed, 0f); // Set Y = 0 để bay ngang
        }
        else if (moveSpeed > 0f)
        {
            // Nếu không có Rigidbody2D, dùng transform.position
            // Nhưng tốt nhất là nên có Rigidbody2D
            Debug.LogWarning("[ProjectileMovement] Projectile không có Rigidbody2D! Nên thêm Rigidbody2D vào Prefab.");
        }
    }

    /// <summary>
    /// Set thời gian sống của projectile
    /// </summary>
    public void SetLifetime(float time)
    {
        lifetime = time;
        if (lifetime > 0f)
        {
            // Hủy timer cũ nếu có và tạo timer mới
            CancelInvoke(nameof(DestroyProjectile));
            Invoke(nameof(DestroyProjectile), lifetime);
        }
    }

    private void DestroyProjectile()
    {
        Destroy(gameObject);
    }
}
