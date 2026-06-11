using UnityEngine;
using Unity.Netcode;

// ItemPickup - Component để nhặt item từ ground
// Gắn vào GameObject item drop trên ground
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : NetworkBehaviour
{
    private const int PickupTraceItemId = 27;

    [Header("Item Settings")]
    [Tooltip("ItemData của item này")]
    [SerializeField] private ItemData itemData;
    
    [Tooltip("Số lượng item")]
    [SerializeField] private int quantity = 1;

    // Sync item_id + quantity đến tất cả client để hiển thị sprite + pickup logic
    private NetworkVariable<int> networkItemId = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<int> networkQuantity = new NetworkVariable<int>(
        1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    [Header("Pickup Settings")]
    [Tooltip("Khoảng cách để nhặt item (units)")]
    [SerializeField] private float pickupRange = 1.5f;
    
    [Tooltip("Layer của player")]
    [SerializeField] private LayerMask playerLayer = 1 << 8; // Layer 6 = Player
    
    [Tooltip("Tự động nhặt khi player vào range")]
    [SerializeField] private bool autoPickup = true;
    
    [Tooltip("Có thể nhặt được không")]
    private NetworkVariable<bool> canPickup = new NetworkVariable<bool>(true);

    private static bool ShouldTracePickup(int itemId) => itemId == PickupTraceItemId;

    private static void TracePickup(int itemId, string message)
    {
        if (ShouldTracePickup(itemId))
            { /* [ItemPickup] {message} */ }
    }

    [Header("Visual")]
    [Tooltip("SpriteRenderer để hiển thị item")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    
    [Tooltip("Animation khi spawn (optional)")]
    [SerializeField] private Animator animator;

    [Header("Selection Effect")]
    [Tooltip("Child GameObject chứa sprite mũi tên chỉ chọn (tạo trong Inspector, xem HUONG_DAN_ITEM_SELECTION_ARROW.md)")]
    [SerializeField] private GameObject selectionIndicator;

    private void Awake()
    {
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
        if (animator == null)       animator = GetComponent<Animator>();

        // Bật trọng lực — item rơi xuống ground
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale  = 1f;
            rb.freezeRotation = true;
        }

        // BoxCollider2D nhỏ ở đáy item — đủ để đáp xuống ground, ít chặn player nhất
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            col.isTrigger = false;
            col.size      = new Vector2(0.4f, 0.2f);
            col.offset    = new Vector2(0f, -0.2f);
        }

        // CircleCollider2D trigger — phát hiện player đi qua để auto-pickup
        if (GetComponent<CircleCollider2D>() == null)
        {
            var trigger      = gameObject.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius    = 0.55f;
        }

        // Ẩn indicator lúc spawn
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Client: resolve ItemData từ networkItemId khi NetworkVariable đã được sync
        networkItemId.OnValueChanged += OnNetworkItemIdChanged;
        networkQuantity.OnValueChanged += (o, n) => quantity = n;

        // Nếu đã có giá trị (ví dụ: host), resolve ngay
        if (networkItemId.Value > 0)
            ResolveItemDataFromId(networkItemId.Value);

        quantity = networkQuantity.Value;

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

    public override void OnNetworkDespawn()
    {
        networkItemId.OnValueChanged -= OnNetworkItemIdChanged;
        base.OnNetworkDespawn();
    }

    private void OnNetworkItemIdChanged(int oldId, int newId)
    {
        ResolveItemDataFromId(newId);
    }

    private void ResolveItemDataFromId(int id)
    {
        if (id <= 0) return;

        // Thử ItemManager trước (cho item có ScriptableObject)
        var mgr = ItemManager.Instance;
        if (mgr != null)
            itemData = mgr.GetItemData(id);

        if (itemData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = itemData.icon;
            return;
        }

        // Fallback: dùng ItemTemplateManager + Resources.Load (cho item từ DB)
        if (spriteRenderer != null)
        {
            var tmgr = ItemTemplateManager.Instance;
            if (tmgr != null)
            {
                var template = tmgr.GetItemTemplate(id);
                if (template != null && template.idIcon > 0)
                {
                    var sprite = Resources.Load<Sprite>($"ItemIcons/{template.idIcon}");
                    if (sprite != null)
                        spriteRenderer.sprite = sprite;
                    else
                        { /* Cảnh báo: Không tìm thấy sprite ItemIcons/{template.idIcon} cho item_id={id} */ }
                }
            }
        }
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (Camera.main == null || NetworkManager.Singleton == null) return;

        if (!IsClickingOnMe()) return;

        ShowSelectionIndicator();
        DoPickupByLocalPlayer();
    }

    // Kiểm tra click chuột có trúng vào item này không (dùng sprite bounds + fallback collider).
    private bool IsClickingOnMe()
    {
        Vector3 mw = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 mp = new Vector2(mw.x, mw.y);

        // Ưu tiên: dùng bounds của SpriteRenderer (bao toàn bộ sprite)
        if (spriteRenderer != null)
        {
            Bounds b = spriteRenderer.bounds;
            b.Expand(0.15f); // bù thêm 15cm để dễ click hơn
            if (b.Contains(new Vector3(mp.x, mp.y, b.center.z)))
                return true;
        }

        // Fallback: kiểm tra Physics2D.OverlapPoint trên mọi collider của object này
        foreach (var c in GetComponents<Collider2D>())
            if (c.OverlapPoint(mp)) return true;

        return false;
    }

    // Gửi pickup request — server sẽ tự biết ai gửi qua SenderClientId.
    private void DoPickupByLocalPlayer()
    {
        if (!canPickup.Value)
        {
            TracePickup(networkItemId.Value, "ClickIgnored reason=locked_or_processing");
            return;
        }

        TracePickup(networkItemId.Value, $"ClickSendRpc item={networkItemId.Value} qty={networkQuantity.Value}");
        PickupByClickServerRpc();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        var netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        // Chỉ hiển thị indicator trên client của player local
        if (netObj.IsLocalPlayer)
            ShowSelectionIndicator();

        // autoPickup chỉ server thực hiện để tránh double-call trên HOST
        if (!IsServer || !autoPickup || !canPickup.Value) return;

        int idToPickup = networkItemId.Value > 0 ? networkItemId.Value
                        : (itemData != null ? itemData.itemID : 0);
        if (idToPickup <= 0) return;

        var inv = netObj.GetComponent<NetworkInventory>();
        if (inv == null) return;

        TracePickup(idToPickup, $"AutoPickupTrigger playerNetObj={netObj.NetworkObjectId} item={idToPickup} qty={networkQuantity.Value}");

        ExecutePickup(netObj);
    }

    // Player đi qua item không bị đẩy — BoxCollider2D solid với ground nhưng bỏ qua player
    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Player"))
        {
            var boxCol = GetComponent<BoxCollider2D>();
            if (boxCol != null)
                Physics2D.IgnoreCollision(boxCol, col.collider, true);
        }
    }

    // Kiểm tra player có trong range không
    // (dùng tag \"Player\", KHÔNG phụ thuộc layer để đỡ lỗi cấu hình)
    private void CheckPlayerInRange()
    {
        // Tìm tất cả collider trong bán kính, rồi lọc theo tag \"Player\"
        Collider2D[] players = MapPhysicsQuery2D.OverlapCircleAll(
            gameObject,
            transform.position,
            pickupRange
        );

        foreach (Collider2D playerCollider in players)
        {
            if (playerCollider.CompareTag("Player"))
            {
                NetworkObject playerNetObj = playerCollider.GetComponentInParent<NetworkObject>();
                if (playerNetObj != null)
                {
                    TryPickupItemServerRpc(playerNetObj.NetworkObjectId);
                    break; // Chỉ pickup cho player đầu tiên
                }
            }
        }
    }

    // ServerRpc được gọi khi player CLICK vào item.
    // Server tự biết ai gửi qua rpcParams.Receive.SenderClientId — không cần truyền playerObjectId.
    [ServerRpc(RequireOwnership = false)]
    private void PickupByClickServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;
        TracePickup(networkItemId.Value, $"ServerClickRpc sender={senderClientId} item={networkItemId.Value} canPickup={canPickup.Value}");

        if (!canPickup.Value) return;

        // Tìm player NetworkObject thuộc sự hữu của sender và có NetworkInventory
        NetworkObject localPlayer = FindPlayerObjectByOwner(senderClientId);
        if (localPlayer == null)
        {
            { /* Cảnh báo: [Server] Không tìm thấy player có NetworkInventory cho clientId={senderClientId} */ }
            return;
        }

        ExecutePickup(localPlayer);
    }

    // Tìm NetworkObject thuộc sự hữu của client và có NetworkInventory (chạy trên server).
    private NetworkObject FindPlayerObjectByOwner(ulong clientId)
    {
        foreach (var kvp in NetworkManager.Singleton.SpawnManager.SpawnedObjects)
        {
            var no = kvp.Value;
            if (no.OwnerClientId == clientId
                && no.GetComponent<NetworkInventory>() != null
                && no.GetComponent<ItemPickup>() == null)  // bỏ qua item drop có NetworkInventory
                return no;
        }
        return null;
    }

    // ServerRpc được gọi khi AUTO-PICKUP (trigger) hoặc phím tắt, truyền rõ NetworkObjectId.
    [ServerRpc(RequireOwnership = false)]
    private void TryPickupItemServerRpc(ulong playerNetworkObjectId)
    {
        TracePickup(networkItemId.Value, $"ServerTryPickup item={networkItemId.Value} canPickup={canPickup.Value} playerObjId={playerNetworkObjectId}");

        if (!canPickup.Value) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
                .TryGetValue(playerNetworkObjectId, out NetworkObject playerObject))
        {
            { /* Cảnh báo: [Server] Không tìm thấy player NetworkObjectId={playerNetworkObjectId} */ }
            return;
        }

        ExecutePickup(playerObject);
    }

    // Lõi xử lý pickup từ server — dùng chung cho cả click và auto-pickup.
    private void ExecutePickup(NetworkObject playerObject)
    {
        int itemIdToPickup = networkItemId.Value > 0 ? networkItemId.Value
                           : (itemData != null ? itemData.itemID : 0);
        if (itemIdToPickup <= 0)
        {
            { /* Cảnh báo: [Server] item_id không hợp lệ (networkItemId={networkItemId.Value}) */ }
            return;
        }

        NetworkInventory inventory = playerObject.GetComponent<NetworkInventory>();
        if (inventory == null)
        {
            { /* Cảnh báo: [Server] Player {playerObject.NetworkObjectId} không có NetworkInventory */ }
            return;
        }

        canPickup.Value = false;
        if (!inventory.TryAddItemOnServer(itemIdToPickup, networkQuantity.Value))
        {
            canPickup.Value = true;
            { /* Cảnh báo: [ItemPickup] PickupFail item={itemIdToPickup} qty={networkQuantity.Value} playerNetObj={playerObject.NetworkObjectId} reason=network_inventory_rejected */ }
            return;
        }

        // Dedicated server does not play its own ClientRpc, so schedule local despawn too.
        DespawnItemClientRpc();
        Invoke(nameof(DespawnItem), 0.3f);
        TracePickup(itemIdToPickup, $"PickupSuccess item={itemIdToPickup} qty={networkQuantity.Value} playerNetObj={playerObject.NetworkObjectId}");
    }

    // ClientRpc: Despawn item và play effect
    [ClientRpc]
    private void DespawnItemClientRpc()
    {
        HideSelectionIndicator();

        if (animator != null)
            animator.SetTrigger("Pickup");
    }

    // Despawn item — chỉ server mới được Despawn NetworkObject.
    // Client không làm gì — server Despawn sẽ tự động Destroy trên tất cả client.
    private void DespawnItem()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // true = destroy gameObject sau khi despawn
        }
        // Client: KHÔNG gọi Destroy() — NGO tự xử lý khi server Despawn
    }

    // Thử nhặt item (local method)
    private void TryPickupItem(GameObject player)
    {
        int itemIdToPickup = networkItemId.Value > 0 ? networkItemId.Value : (itemData != null ? itemData.itemID : 0);
        if (itemIdToPickup <= 0) return;

        NetworkObject playerNetObj = player.GetComponent<NetworkObject>();
        if (playerNetObj != null)
        {
            TryPickupItemServerRpc(playerNetObj.NetworkObjectId);
        }
    }

    // Cho phép player nhấn chuột vào item trên mặt đất để nhặt.
    // Yêu cầu: Collider2D trên GameObject này (non-trigger để OnMouseDown hoạt động).
    private void OnMouseDown()
    {
        // Handled via Update() + Physics2D.OverlapPoint để bypass EventSystem UI blocking
    }

    // Public API để player gọi nhặt item (dùng cho phím tắt P)
    public void RequestPickup(ulong playerNetworkObjectId)
    {
        TryPickupItemServerRpc(playerNetworkObjectId);
    }

    // Hiển thị mũi tên chỉ chọn item, tự ẩn sau 3 giây
    private void ShowSelectionIndicator()
    {
        if (selectionIndicator == null) return;
        selectionIndicator.SetActive(true);
        CancelInvoke(nameof(HideSelectionIndicator));
        Invoke(nameof(HideSelectionIndicator), 3f);
    }

    private void HideSelectionIndicator()
    {
        if (selectionIndicator != null)
            selectionIndicator.SetActive(false);
    }

    // Set item data và quantity (dùng khi spawn item drop với ItemData ScriptableObject)
    public void SetItemData(ItemData data, int qty)
    {
        itemData = data;
        quantity = qty;
        
        if (spriteRenderer != null && data != null)
        {
            spriteRenderer.sprite = data.icon;
        }

        // Dùng NetworkManager.Singleton.IsServer thay vì IsServer để hoạt động
        // cả trước lẫn sau khi NetworkObject.Spawn() — tránh client nhận id=0
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            networkItemId.Value  = data != null ? data.itemID : 0;
            networkQuantity.Value = qty;
        }
    }

    // Set item bằng ID trực tiếp (không cần ItemData ScriptableObject).
    // Sử dụng cho enemy drop sử dụng item_id từ DB.
    // ItemPickup sẽ tự resolve ItemData qua ItemManager nếu có (cho hiển thị sprite).
    public void SetItemId(int itemId, int qty)
    {
        quantity = qty;

        // Cố gắng resolve ItemData để hiển thị sprite (không bắt buộc)
        if (ItemManager.Instance != null)
            itemData = ItemManager.Instance.GetItemData(itemId);

        if (itemData != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = itemData.icon;
        }
        else if (spriteRenderer != null)
        {
            // Fallback: ItemTemplateManager + Resources.Load cho item từ DB
            var tmgr = ItemTemplateManager.Instance;
            if (tmgr != null)
            {
                var template = tmgr.GetItemTemplate(itemId);
                if (template != null && template.idIcon > 0)
                {
                    var sprite = Resources.Load<Sprite>($"ItemIcons/{template.idIcon}");
                    if (sprite != null)
                        spriteRenderer.sprite = sprite;
                }
            }
        }

        // Set NetworkVariable để sync đến tất cả client
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            networkItemId.Value   = itemId;
            networkQuantity.Value = qty;
        }
    }

    // Gizmos để visualize pickup range
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
