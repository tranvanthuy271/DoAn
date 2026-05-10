using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkObject), typeof(Rigidbody2D), typeof(NetworkTransform))]
public class NetworkEnemyController : NetworkBehaviour
{
    private const string AttackBoolParameter = "isAttacking";
    private const string LegacyAttackTriggerParameter = "Attack";

    [Header("Components")]
    private EnemyAI enemyAI;
    private BossAI bossAI;
    private Rigidbody2D rb;
    private Animator animator;
    private float initialGravityScale = 1f;

    [Header("Network Sync")]
    private NetworkVariable<float> networkScaleX = new NetworkVariable<float>(1f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<Vector2> networkVelocity = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Awake()
    {
        enemyAI = GetComponent<EnemyAI>();
        bossAI = GetComponent<BossAI>();
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb != null)
            initialGravityScale = rb.gravityScale > 0f ? rb.gravityScale : 1f;
        
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
            ApplyGravityMode();
        }

        // Enemy không đẩy nhau (di chuyển xuyên qua nhau như đặc trưng RPG 2D)
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        ApplyGravityMode();

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

        if (bossAI == null)
            bossAI = GetComponent<BossAI>();

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

    private void ApplyGravityMode()
    {
        if (rb == null)
            return;

        bool disableGravity = true;

        if (bossAI != null && bossAI.UsesGroundPhysics)
        {
            disableGravity = false;
        }
        else if (enemyAI != null && enemyAI.canFly)
        {
            disableGravity = true;
        }

        rb.gravityScale = disableGravity ? 0f : initialGravityScale;
    }

    /// <summary>
    /// Server RPC để trigger attack animation (melee attack)
    /// </summary>
    [ServerRpc]
    public void TriggerAttackServerRpc()
    {
        SetAttackAnimationStateClientRpc(true);
    }

    public void SetAttackAnimationState(bool isAttacking)
    {
        if (IsServer)
        {
            SetAttackAnimationStateClientRpc(isAttacking);
            return;
        }

        SetAttackAnimationStateServerRpc(isAttacking);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetAttackAnimationStateServerRpc(bool isAttacking)
    {
        SetAttackAnimationStateClientRpc(isAttacking);
    }

    [ClientRpc]
    private void SetAttackAnimationStateClientRpc(bool isAttacking)
    {
        ApplyAttackAnimationState(isAttacking);
    }
    
    /// <summary>
    /// Reset attack animation state (local)
    /// </summary>
    private void ResetAttackAnimation()
    {
        ApplyAttackAnimationState(false);
    }

    private void ApplyAttackAnimationState(bool isAttacking)
    {
        if (animator == null)
            return;

        CancelInvoke(nameof(ResetAttackAnimation));

        if (HasAnimatorParameter(AttackBoolParameter, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(AttackBoolParameter, isAttacking);
        }
        else if (isAttacking && HasAnimatorParameter(LegacyAttackTriggerParameter, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(LegacyAttackTriggerParameter);
        }

        if (isAttacking)
        {
            Debug.Log($"[NetworkEnemyController] Attack animation triggered on client for {gameObject.name}");
            Invoke(nameof(ResetAttackAnimation), 0.5f);
        }
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (var parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
                return true;
        }

        return false;
    }
    
    /// <summary>
    /// ClientRpc để reset attack animation trên tất cả clients
    /// </summary>
    [ClientRpc]
    public void ResetAttackAnimationClientRpc()
    {
        ApplyAttackAnimationState(false);
    }

}
