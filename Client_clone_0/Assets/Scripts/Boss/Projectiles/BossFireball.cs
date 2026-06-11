using UnityEngine;
using Unity.Netcode;

//  BossFireball  —  Hỏa cầu rơi từ trên trời xuống
//
//  QUY TẮC HỦY:
//    • Chạm người chơi    → damage + hủy
//    • Chạm "GroundFinal" → hủy (tầng đất cuối cùng)
//    • Chạm ground khác   → xuyên qua (không hủy)
//
//  SETUP:
//    • Tag tầng cuối = "GroundFinal"  (đặt trong Unity cho Tilemap/Collider cuối)
//    • Tag tầng trung gian = "Ground" hoặc bất kỳ — fireball tự xuyên qua
//    • Prefab cần: Collider2D (isTrigger), Rigidbody2D (gravity on)

[RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
public class BossFireball : MonoBehaviour
{
    [Header("Cài Đặt (auto set từ BossController.Init)")]
    [SerializeField] private int   damage    = 30;
    [SerializeField] private float fallSpeed = 5f;
    [Tooltip("Thời gian tối đa tồn tại (giây) — bảo vệ memory leak")]
    [SerializeField] private float maxLifetime = 10f;

    private Rigidbody2D _rb;
    private bool        _hasHit = false;


    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        // Đảm bảo collider là trigger
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    // Khởi tạo do BossController gọi ngay sau Instantiate.
    public void Init(int dmg, float speed)
    {
        damage    = dmg;
        fallSpeed = speed;
        _rb.gravityScale = 0f;                          // Dùng velocity thay gravity
        _rb.velocity = new Vector2(0f, -fallSpeed); // Rơi thẳng xuống
        Invoke(nameof(SelfDestroy), maxLifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Chỉ server xử lý damage
        if (!ShouldRunServer()) return;
        if (_hasHit) return;

        // Chạm người chơi
        if (other.CompareTag("Player"))
        {
            DamagePlayer(other);
            _hasHit = true;
            DestroyFireball();
            return;
        }

        // Chạm tầng đất cuối cùng → hủy
        if (other.CompareTag("GroundFinal"))
        {
            _hasHit = true;
            DestroyFireball();
            return;
        }

        // Chạm ground trung gian → xuyên qua (không làm gì)
    }

    private void DamagePlayer(Collider2D playerCol)
    {
        var netPH = playerCol.GetComponentInParent<NetworkPlayerHealth>();
        if (netPH != null) { netPH.TakeDamage(damage); return; }
        var ph = playerCol.GetComponentInParent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(damage);
    }

    private void DestroyFireball()
    {
        CancelInvoke();
        var net = GetComponent<NetworkObject>();
        if (net != null && net.IsSpawned)
            net.Despawn(true);
        else
            Destroy(gameObject);
    }

    private void SelfDestroy() => DestroyFireball();

    private static bool ShouldRunServer()
        => NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
}
