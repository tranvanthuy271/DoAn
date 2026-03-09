using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Skill dịch chuyển tức thời (Teleport/Blink) cho player
/// Cho phép player dịch chuyển một khoảng cách theo hướng đang nhìn
/// </summary>
public class TeleportSkill : NetworkBehaviour
{
    [Header("Components")]
    private PlayerController controller;
    private NetworkObject networkObject;
    private CharacterController characterController; // Nếu dùng CharacterController
    private Rigidbody2D rb2D; // Nếu dùng Rigidbody2D

    [Header("Teleport Settings")]
    [Tooltip("Phím để kích hoạt skill teleport")]
    [SerializeField] private KeyCode teleportKey = KeyCode.T;
    
    [Tooltip("Cooldown giữa các lần sử dụng teleport (seconds)")]
    [SerializeField] private float cooldown = 3f;
    
    [Tooltip("Khoảng cách dịch chuyển (units)")]
    [SerializeField] private float teleportDistance = 5f;
    
    [Tooltip("Thời gian thực hiện teleport (seconds). Đặt 0 để teleport tức thời")]
    [SerializeField] private float teleportDuration = 0f;
    
    [Tooltip("Có kiểm tra collision trước khi teleport không (tránh teleport vào tường)")]
    [SerializeField] private bool checkCollision = true;
    
    [Tooltip("Layer mask để kiểm tra collision (ví dụ: Wall, Obstacle)")]
    [SerializeField] private LayerMask obstacleLayerMask = 1; // Default layer

    [Header("Animation (Optional)")]
    [Tooltip("Tên Trigger trong Animator để phát animation teleport. Để trống nếu không cần")]
    [SerializeField] private string animationTriggerName = "";
    
    [Tooltip("Object SkillEffect để hiển thị animation. Nếu để trống sẽ tự tìm child có tên 'SkillEffect'")]
    [SerializeField] private GameObject skillEffectObject;
    
    private Animator skillEffectAnimator;
    private Unity.Netcode.Components.NetworkAnimator skillEffectNetworkAnimator;

    [Header("Visual Effects (Optional)")]
    [Tooltip("Prefab hiệu ứng khi bắt đầu teleport (ví dụ: particle effect)")]
    [SerializeField] private GameObject teleportStartEffect;
    
    [Tooltip("Prefab hiệu ứng khi kết thúc teleport (ví dụ: particle effect)")]
    [SerializeField] private GameObject teleportEndEffect;

    [Header("Internal State")]
    private float cooldownTimer = 0f;
    private bool canUseTeleport = true;
    private bool isTeleporting = false;
    private Vector3 targetPosition;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Start()
    {
        if (!IsSpawned)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        // Tìm PlayerController
        controller = GetComponent<PlayerController>();
        if (controller == null)
        {
            controller = GetComponentInParent<PlayerController>();
        }

        networkObject = GetComponent<NetworkObject>();

        // Tìm CharacterController hoặc Rigidbody2D
        characterController = GetComponent<CharacterController>();
        rb2D = GetComponent<Rigidbody2D>();

        // Tìm SkillEffect object
        if (skillEffectObject == null)
        {
            skillEffectObject = transform.Find("SkillEffect")?.gameObject;
        }

        // Tìm Animator
        if (skillEffectObject != null)
        {
            skillEffectAnimator = skillEffectObject.GetComponent<Animator>();
            skillEffectNetworkAnimator = skillEffectObject.GetComponent<Unity.Netcode.Components.NetworkAnimator>();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        UpdateCooldown();
        HandleTeleportInput();
    }

    private void UpdateCooldown()
    {
        if (!canUseTeleport)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                canUseTeleport = true;
            }
        }
    }

    private void HandleTeleportInput()
    {
        if (Input.GetKeyDown(teleportKey) && canUseTeleport && !isTeleporting)
        {
            UseTeleport();
        }
    }

    private void UseTeleport()
    {
        if (isTeleporting) return;

        // Chỉ owner mới teleport
        if (IsOwner)
        {
            UseTeleportLocal();
        }
    }

    private void UseTeleportLocal()
    {
        isTeleporting = true;
        canUseTeleport = false;
        cooldownTimer = cooldown;

        // Xác định hướng player đang nhìn
        bool facingRight = transform.localScale.x >= 0f;
        Vector2 direction = facingRight ? Vector2.right : Vector2.left;

        // Tính vị trí đích
        Vector3 currentPosition = transform.position;
        Vector3 teleportDirection = new Vector3(direction.x, 0f, 0f) * teleportDistance;
        targetPosition = currentPosition + teleportDirection;

        // Kiểm tra collision nếu cần
        if (checkCollision)
        {
            targetPosition = CheckCollisionBeforeTeleport(currentPosition, targetPosition);
        }

        // Trigger animation nếu có
        if (!string.IsNullOrEmpty(animationTriggerName))
        {
            TriggerTeleportAnimation();
        }

        // Spawn hiệu ứng bắt đầu
        if (teleportStartEffect != null)
        {
            Instantiate(teleportStartEffect, currentPosition, Quaternion.identity);
        }

        // Thực hiện teleport
        if (teleportDuration > 0f)
        {
            // Teleport với thời gian (smooth teleport)
            StartCoroutine(TeleportSmooth(currentPosition, targetPosition));
        }
        else
        {
            // Teleport tức thời
            TeleportInstant(targetPosition);
        }
    }

    private Vector3 CheckCollisionBeforeTeleport(Vector3 startPos, Vector3 endPos)
    {
        // Raycast để kiểm tra có vật cản không
        Vector3 direction = (endPos - startPos).normalized;
        float distance = Vector3.Distance(startPos, endPos);

        // Raycast 2D nếu dùng Rigidbody2D
        if (rb2D != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(startPos, direction, distance, obstacleLayerMask);
            if (hit.collider != null)
            {
                // Dịch chuyển đến vị trí trước vật cản (trừ đi một chút để không bị kẹt)
                float safeDistance = hit.distance - 0.5f; // Trừ 0.5 units để an toàn
                return startPos + direction * Mathf.Max(0f, safeDistance);
            }
        }
        // Raycast 3D nếu dùng CharacterController
        else if (characterController != null)
        {
            RaycastHit hit;
            if (Physics.Raycast(startPos, direction, out hit, distance, obstacleLayerMask))
            {
                float safeDistance = hit.distance - 0.5f;
                return startPos + direction * Mathf.Max(0f, safeDistance);
            }
        }

        return endPos; // Không có vật cản, teleport đến vị trí đích
    }

    private void TeleportInstant(Vector3 targetPos)
    {
        // Dịch chuyển tức thời
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = targetPos;
            characterController.enabled = true;
        }
        else
        {
            transform.position = targetPos;
        }

        // Spawn hiệu ứng kết thúc
        if (teleportEndEffect != null)
        {
            Instantiate(teleportEndEffect, targetPos, Quaternion.identity);
        }

        // Reset state
        Invoke(nameof(ResetTeleportState), 0.1f);
    }

    private System.Collections.IEnumerator TeleportSmooth(Vector3 startPos, Vector3 endPos)
    {
        float elapsed = 0f;
        
        while (elapsed < teleportDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / teleportDuration;
            
            // Có thể dùng curve để smooth hơn
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, t);
            
            if (characterController != null)
            {
                characterController.enabled = false;
                transform.position = currentPos;
                characterController.enabled = true;
            }
            else
            {
                transform.position = currentPos;
            }
            
            yield return null;
        }

        // Đảm bảo đến đúng vị trí đích
        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = endPos;
            characterController.enabled = true;
        }
        else
        {
            transform.position = endPos;
        }

        // Spawn hiệu ứng kết thúc
        if (teleportEndEffect != null)
        {
            Instantiate(teleportEndEffect, endPos, Quaternion.identity);
        }

        // Reset state
        ResetTeleportState();
    }

    private void TriggerTeleportAnimation()
    {
        if (skillEffectObject == null || skillEffectAnimator == null) return;

        // Đảm bảo SkillEffect active
        if (!skillEffectObject.activeSelf)
        {
            skillEffectObject.SetActive(true);
        }

        // Enable NetworkAnimator nếu có
        if (skillEffectNetworkAnimator != null && !skillEffectNetworkAnimator.enabled)
        {
            skillEffectNetworkAnimator.enabled = true;
        }

        // Trigger animation
        if (skillEffectAnimator.runtimeAnimatorController != null)
        {
            bool hasParameter = false;
            foreach (AnimatorControllerParameter param in skillEffectAnimator.parameters)
            {
                if (param.name == animationTriggerName && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasParameter = true;
                    break;
                }
            }

            if (hasParameter)
            {
                skillEffectAnimator.SetTrigger(animationTriggerName);
            }
        }
    }

    private void ResetTeleportState()
    {
        isTeleporting = false;
    }

    // Public API
    public bool IsTeleporting() => isTeleporting;
    public bool CanUseTeleport() => canUseTeleport;
    public float GetCooldownPercent() => canUseTeleport ? 1f : Mathf.Clamp01(1f - (cooldownTimer / cooldown));
}
