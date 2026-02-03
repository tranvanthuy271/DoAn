using UnityEngine;
using Unity.Netcode;

/// <summary>
/// ItemPickup - Component để nhặt item từ ground
/// Gắn vào GameObject item drop trên ground
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : NetworkBehaviour
{
    [Header("Item Settings")]
    [Tooltip("ItemData của item này")]
    [SerializeField] private ItemData itemData;
    
    [Tooltip("Số lượng item")]
    [SerializeField] private int quantity = 1;
    
    [Header("Pickup Settings")]
    [Tooltip("Khoảng cách để nhặt item (units)")]
    [SerializeField] private float pickupRange = 1.5f;
    
    [Tooltip("Layer của player")]
    [SerializeField] private LayerMask playerLayer = 1 << 8; // Layer 6 = Player
    
    [Tooltip("Tự động nhặt khi player vào range")]
    [SerializeField] private bool autoPickup = true;
    
    [Tooltip("Có thể nhặt được không")]
    private NetworkVariable<bool> canPickup = new NetworkVariable<bool>(true);

    [Header("Visual")]
    [Tooltip("SpriteRenderer để hiển thị item")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Tooltip("Animation khi spawn (optional)")]
    [SerializeField] private Animator animator;

    private void Awake()
    {
        // Tìm SpriteRenderer nếu chưa gán
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        // Tìm Animator nếu chưa gán
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Set sprite từ ItemData
        if (itemData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = itemData.icon;
        }

        // Play spawn animation nếu có
        if (animator != null)
        {
            animator.SetTrigger("Spawn");
        }
    }

    private void Update()
    {
        // Chỉ server mới check auto-pickup
        if (!IsServer) return;
        if (!canPickup.Value) return;

        // Tìm player trong range
        if (autoPickup)
        {
            CheckPlayerInRange();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Nếu không auto pickup, chỉ pickup khi player trigger
        if (!autoPickup && other.CompareTag("Player"))
        {
            TryPickupItem(other.gameObject);
        }
    }

    /// <summary>
    /// Kiểm tra player có trong range không
    /// (dùng tag \"Player\", KHÔNG phụ thuộc layer để đỡ lỗi cấu hình)
    /// </summary>
    private void CheckPlayerInRange()
    {
        // Tìm tất cả collider trong bán kính, rồi lọc theo tag \"Player\"
        Collider2D[] players = Physics2D.OverlapCircleAll(
            transform.position,
            pickupRange
        );

        foreach (Collider2D playerCollider in players)
        {
            if (playerCollider.CompareTag("Player"))
            {
                NetworkObject playerNetObj = playerCollider.GetComponent<NetworkObject>();
                if (playerNetObj != null)
                {
                    TryPickupItemServerRpc(playerNetObj.NetworkObjectId);
                    break; // Chỉ pickup cho player đầu tiên
                }
            }
        }
    }

    /// <summary>
    /// ServerRpc: Thử nhặt item
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void TryPickupItemServerRpc(ulong playerNetworkObjectId)
    {
        if (!canPickup.Value) return;

        // Tìm player NetworkObject
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject playerObject))
        {
            NetworkInventory inventory = playerObject.GetComponent<NetworkInventory>();
            if (inventory != null && itemData != null)
            {
                // Thêm item vào inventory
                inventory.AddItem(itemData.itemID, quantity);
                
                // Despawn item
                canPickup.Value = false;
                DespawnItemClientRpc();
                
                Debug.Log($"[ItemPickup] Player {playerNetworkObjectId} picked up {quantity}x {itemData.itemName}");
            }
        }
    }

    /// <summary>
    /// ClientRpc: Despawn item và play effect
    /// </summary>
    [ClientRpc]
    private void DespawnItemClientRpc()
    {
        // Play pickup effect/sound
        if (animator != null)
        {
            animator.SetTrigger("Pickup");
        }

        // Despawn sau delay ngắn để animation chạy
        Invoke(nameof(DespawnItem), 0.3f);
    }

    /// <summary>
    /// Despawn item
    /// </summary>
    private void DespawnItem()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Thử nhặt item (local method)
    /// </summary>
    private void TryPickupItem(GameObject player)
    {
        NetworkInventory inventory = player.GetComponent<NetworkInventory>();
        if (inventory != null && itemData != null)
        {
            ulong playerNetworkObjectId = player.GetComponent<NetworkObject>().NetworkObjectId;
            TryPickupItemServerRpc(playerNetworkObjectId);
        }
    }

    /// <summary>
    /// Public API để player gọi nhặt item (dùng cho phím tắt P)
    /// </summary>
    public void RequestPickup(ulong playerNetworkObjectId)
    {
        TryPickupItemServerRpc(playerNetworkObjectId);
    }

    /// <summary>
    /// Set item data và quantity (dùng khi spawn item drop)
    /// </summary>
    public void SetItemData(ItemData data, int qty)
    {
        itemData = data;
        quantity = qty;
        
        if (spriteRenderer != null && data != null)
        {
            spriteRenderer.sprite = data.icon;
        }
    }

    // Gizmos để visualize pickup range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
