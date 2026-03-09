using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D), typeof(NetworkTransform))]
public class NetworkEnemyController : NetworkBehaviour
{
    [Header("Components")]
    private EnemyAI enemyAI;
    private Rigidbody2D rb;
    private Animator animator;

    [Header("Network Sync")]
    private NetworkVariable<float> networkScaleX = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> networkVelocity = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // Đảm bảo có NetworkTransform component
        if (GetComponent<NetworkTransform>() == null)
        {
            var networkTransform = gameObject.AddComponent<NetworkTransform>();
            // Chỉ sync position X và Y (2D game)
            networkTransform.SyncPositionX = true;
            networkTransform.SyncPositionY = true;
            networkTransform.SyncPositionZ = false;
            // Không sync rotation (2D game thường không cần)
            // Không sync scale (dùng NetworkVariable thay vì NetworkTransform)
            // Các setting khác sẽ dùng mặc định
        }
        
        // Đảm bảo Rigidbody2D không bị freeze
        if (rb != null)
        {
            // Chỉ freeze rotation Z (2D game)
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            // Đảm bảo body type là Dynamic
            if (rb.bodyType != RigidbodyType2D.Dynamic)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to networkScaleX changes để sync flip direction
        networkScaleX.OnValueChanged += OnScaleXChanged;
        networkVelocity.OnValueChanged += OnVelocityChanged;

        // KHÔNG spawn health bar ở đây nữa
        // EnemyHealthBarSpawner sẽ tự động spawn trong OnNetworkSpawn() của nó
        // Việc spawn ở đây gây duplicate
    }

    public override void OnNetworkDespawn()
    {
        networkScaleX.OnValueChanged -= OnScaleXChanged;
        networkVelocity.OnValueChanged -= OnVelocityChanged;
        base.OnNetworkDespawn();
    }

    private void OnScaleXChanged(float oldValue, float newValue)
    {
        // Sync flip direction khi networkScaleX thay đổi
        Vector3 scale = transform.localScale;
        scale.x = newValue;
        transform.localScale = scale;
    }

    private void OnVelocityChanged(Vector2 oldValue, Vector2 newValue)
    {
        // Sync velocity cho remote clients (chỉ khi không phải server)
        // NetworkTransform sẽ tự động sync position, nên không cần sync velocity nữa
        // Giữ lại để tương thích nếu cần
    }

    private void FixedUpdate()
    {
        // Chỉ server mới xử lý movement logic
        if (!IsServer) return;

        // EnemyAI sẽ xử lý movement, chúng ta chỉ cần sync scale (flip direction)
        // NetworkTransform sẽ tự động sync position và rotation
        if (enemyAI != null)
        {
            // Sync scale (flip direction)
            float currentScaleX = transform.localScale.x;
            if (Mathf.Abs(networkScaleX.Value - currentScaleX) > 0.01f)
            {
                networkScaleX.Value = currentScaleX;
            }
        }
    }

    /// <summary>
    /// Server RPC để trigger attack animation (melee attack)
    /// </summary>
    [ServerRpc]
    public void TriggerAttackServerRpc()
    {
        TriggerAttackClientRpc();
    }

    [ClientRpc]
    private void TriggerAttackClientRpc()
    {
        // Trigger attack animation trên TẤT CẢ clients (bao gồm cả server)
        if (animator != null)
        {
            animator.SetBool("isAttacking", true);
            Debug.Log($"[NetworkEnemyController] Attack animation triggered on client for {gameObject.name}");
        }
        
        // Reset animation state sau một khoảng thời gian ngắn
        // (Animation sẽ tự động reset khi kết thúc, nhưng đảm bảo reset sau 0.5s)
        Invoke(nameof(ResetAttackAnimation), 0.5f);
    }
    
    /// <summary>
    /// Reset attack animation state (local)
    /// </summary>
    private void ResetAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("isAttacking", false);
        }
    }
    
    /// <summary>
    /// ClientRpc để reset attack animation trên tất cả clients
    /// </summary>
    [ClientRpc]
    public void ResetAttackAnimationClientRpc()
    {
        ResetAttackAnimation();
    }

}
