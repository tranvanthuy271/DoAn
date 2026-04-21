using System.Collections;
using UnityEngine;
using Unity.Netcode;

// ─────────────────────────────────────────────────────────────────────────────
//  BossLightningBolt  —  Tia sét tạo ra bởi skill sét liên tiếp
//
//  HIỆU ỨNG KHI TRÚNG NGƯỜI CHƠI:
//    • Trừ HP (damage)
//    • Stun (đứng im) trong stunDuration giây
//    • Tia sét tự hủy sau boltDuration giây
//
//  SETUP:
//    • Prefab cần: Collider2D (isTrigger), Animator (tùy chọn)
//    • Không cần Rigidbody2D (tia sét không di chuyển)
// ─────────────────────────────────────────────────────────────────────────────

[RequireComponent(typeof(Collider2D))]
public class BossLightningBolt : MonoBehaviour
{
    [Header("Cài Đặt (auto set từ BossController.Init)")]
    [SerializeField] private int   damage       = 15;
    [SerializeField] private float boltDuration = 2f;
    [SerializeField] private float stunDuration = 2f;

    // Theo dõi player đã bị hit để không spam damage
    private readonly System.Collections.Generic.HashSet<uint> _hitPlayers = new();

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    /// <summary>Khởi tạo do BossController gọi ngay sau Instantiate.</summary>
    public void Init(int dmg, float duration, float stun)
    {
        damage       = dmg;
        boltDuration = duration;
        stunDuration = stun;
        StartCoroutine(LifetimeCoroutine());
    }

    private IEnumerator LifetimeCoroutine()
    {
        yield return new WaitForSeconds(boltDuration);
        DestroyBolt();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!ShouldRunServer()) return;
        if (!other.CompareTag("Player")) return;

        // Lấy NetworkObject để track đã hit chưa
        var netObj = other.GetComponentInParent<NetworkObject>();
        uint netId = netObj != null ? (uint)netObj.NetworkObjectId : 0u;
        if (_hitPlayers.Contains(netId)) return;
        _hitPlayers.Add(netId);

        // Damage
        var netPH = other.GetComponentInParent<NetworkPlayerHealth>();
        if (netPH != null)
        {
            netPH.TakeDamage(damage);
        }
        else
        {
            var ph = other.GetComponentInParent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damage);
        }

        // Stun — gửi ClientRpc đến owner của player để apply stun local
        if (netObj != null)
        {
            ApplyStunClientRpc(stunDuration, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { netObj.OwnerClientId }
                }
            });
        }
        else
        {
            // Standalone / local
            ApplyStunLocal(other.gameObject, stunDuration);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Stun helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Server → Owner client: áp dụng stun movement.</summary>
    [ClientRpc]
    private void ApplyStunClientRpc(float duration, ClientRpcParams rpcParams = default)
    {
        // Tìm player local (IsOwner) và apply stun
        var allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        foreach (var pm in allPlayers)
        {
            var netObj = pm.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsOwner)
            {
                pm.SetStunned(duration);
                break;
            }
        }
    }

    private static void ApplyStunLocal(GameObject target, float duration)
    {
        var pm = target.GetComponentInParent<PlayerMovement>();
        if (pm != null) pm.SetStunned(duration);
    }

    private void DestroyBolt()
    {
        StopAllCoroutines();
        var net = GetComponent<NetworkObject>();
        if (net != null && net.IsSpawned)
            net.Despawn(true);
        else
            Destroy(gameObject);
    }

    private static bool ShouldRunServer()
        => NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer;
}
