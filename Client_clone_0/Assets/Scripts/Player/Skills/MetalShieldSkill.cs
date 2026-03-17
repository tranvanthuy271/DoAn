using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Skill 3 của hệ Kim — "Kim Cang Khiên" (Bất Tử Khiên)
///
/// Cơ chế:
///   1. Kích hoạt khiên: player bất tử hoàn toàn trong shieldDuration giây.
///   2. Bất kỳ projectile nào (tag "Projectile" hoặc "EnemyProjectile") chạm vào collider
///      của shield sẽ bị xóa ngay lập tức.
///   3. Sau khi hết thời gian, khiên tắt và cooldown bắt đầu.
///
/// Gắn vào player:
///   - Gắn component này vào cùng GameObject với PlayerSkillManager
///   - Gán shieldVisualObject (GameObject hiệu ứng khiên, sẽ bật/tắt theo skill)
///   - Gán shieldCollider (CircleCollider2D trigger bao quanh player, dùng để xóa projectile)
///   - PlayerSkillManager sẽ tự phát hiện qua GetComponent khi skillType = MetalShield
/// </summary>
public class MetalShieldSkill : NetworkBehaviour
{
    [Header("Shield Settings")]
    [Tooltip("Cooldown giữa các lần dùng skill (giây)")]
    [SerializeField] public float cooldown = 10f;

    [Tooltip("Thời gian khiên duy trì (giây)")]
    [SerializeField] private float shieldDuration = 4f;

    [Header("Projectile Removal")]
    [Tooltip("Tag của projectile đối thủ sẽ bị xóa khi chạm khiên. Có thể thêm nhiều tag.")]
    [SerializeField] private string[] projectileTags = { "Projectile", "EnemyProjectile", "Fireball" };

    [Header("Visual")]
    [Tooltip("GameObject hiệu ứng khiên (vd: vòng sáng, particle). Để trống nếu không có.")]
    [SerializeField] private GameObject shieldVisualObject;

    [Tooltip("Animator trigger để phát animation khiên (tùy chọn)")]
    [SerializeField] private string shieldTriggerName = "Skill3";

    [Header("References")]
    [Tooltip("Collider trigger bao quanh player để detect projectile chạm khiên. " +
             "Nếu để trống, sẽ tự tìm CircleCollider2D/CapsuleCollider2D có isTrigger=true trên child 'ShieldCollider'.")]
    [SerializeField] private Collider2D shieldCollider;

    // ── Internal state ────────────────────────────────────────────────────────
    private float cooldownTimer;
    private bool canUse = true;
    private bool isUsing;
    private PlayerHealth playerHealth;
    private PlayerAnimator playerAnimator;

    public bool CanUseNow => canUse && !isUsing;
    public float GetCooldownPercent() => canUse ? 1f : Mathf.Clamp01(1f - cooldownTimer / cooldown);
    public float GetCooldownRemaining() => canUse ? 0f : Mathf.Max(0f, cooldownTimer);

    // ════════════════════════════════════════════════════════════════════════
    //  Unity lifecycle
    // ════════════════════════════════════════════════════════════════════════

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Start()
    {
        if (!IsSpawned) Initialize();
    }

    private void Initialize()
    {
        playerHealth  = GetComponent<PlayerHealth>() ?? GetComponentInParent<PlayerHealth>();
        playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();

        // Tự tìm shield collider nếu chưa gán
        if (shieldCollider == null)
        {
            var shieldChild = transform.Find("ShieldCollider");
            if (shieldChild != null)
                shieldCollider = shieldChild.GetComponent<Collider2D>();
        }

        // Tắt visual và collider lúc khởi động
        SetShieldActive(false);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (!canUse)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                canUse = true;
            }
        }
    }

    // Shield collider xóa projectile kẻ địch khi chạm (chỉ chạy trên owner/server)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!isUsing) return;

        // Chỉ xử lý trên server (hoặc host) để tránh xóa đối tượng không có thẩm quyền
        if (!IsServer) return;

        foreach (var tag in projectileTags)
        {
            if (other.CompareTag(tag))
            {
                // Network object → dùng Despawn; non-network → Destroy
                var netObj = other.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
                else
                    Destroy(other.gameObject);

                Debug.Log($"[MetalShieldSkill] Đã xóa projectile: {other.name} (tag: {tag})");
                return;
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Public API — gọi từ PlayerSkillManager
    // ════════════════════════════════════════════════════════════════════════

    public void UseMetalShield()
    {
        if (!CanUseNow) return;

        canUse = false;
        isUsing = true;
        cooldownTimer = cooldown;

        if (IsServer)
            StartCoroutine(MetalShieldSequence());
        else
            StartMetalShieldServerRpc();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Network RPCs
    // ════════════════════════════════════════════════════════════════════════

    [ServerRpc]
    private void StartMetalShieldServerRpc()
    {
        StartCoroutine(MetalShieldSequence());
    }

    [ClientRpc]
    private void ShowShieldClientRpc(bool active)
    {
        SetShieldActive(active);
    }

    [ClientRpc]
    private void TriggerShieldAnimationClientRpc()
    {
        // Trigger animation nhân vật (phong/kim controller)
        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
        playerAnimator?.TriggerAttack();

        // Trigger SkillEffect animation — tìm SkillEffect trực tiếp (khác với shieldVisualObject)
        if (string.IsNullOrEmpty(shieldTriggerName)) return;

        // Tìm SkillEffect từ root của prefab để tránh miss khi MetalShieldSkill gắn trên child object
        Transform root = transform.root;
        GameObject skillEffect = null;
        Transform found = root.Find("SkillEffect");
        if (found != null) skillEffect = found.gameObject;
        if (skillEffect == null) skillEffect = transform.Find("SkillEffect")?.gameObject;
        if (skillEffect == null) return;

        if (!skillEffect.activeSelf)
            skillEffect.SetActive(true);

        SpriteRenderer sr = skillEffect.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = true;

        Animator anim = skillEffect.GetComponent<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null)
        {
            Debug.LogWarning("[MetalShieldSkill] SkillEffect không có Animator hoặc AnimatorController.");
            return;
        }

        foreach (var p in anim.parameters)
        {
            if (p.name == shieldTriggerName && p.type == AnimatorControllerParameterType.Trigger)
            {
                anim.SetTrigger(shieldTriggerName);
                return;
            }
        }
        Debug.LogWarning($"[MetalShieldSkill] Animator không có Trigger '{shieldTriggerName}'.");
    }

    [ClientRpc]
    private void ResetIsUsingClientRpc()
    {
        isUsing = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Core sequence (runs on server)
    // ════════════════════════════════════════════════════════════════════════

    private IEnumerator MetalShieldSequence()
    {
        // 1. Bật visual + animation cho tất cả client
        TriggerShieldAnimationClientRpc();
        ShowShieldClientRpc(true);
        TintPlayerClientRpc(true);   // màu vàng kim loại để thấy rõ shield đang active

        // 2. Bật invincibility trên owner
        ActivateShieldOnOwnerClientRpc();

        // 3. Chờ hết thời gian
        yield return new WaitForSeconds(shieldDuration);

        // 4. Tắt khiên
        ShowShieldClientRpc(false);
        TintPlayerClientRpc(false);
        DeactivateShieldOnOwnerClientRpc();

        // 5. Clear SkillEffect sprite
        ClearSkillEffectClientRpc();

        // 6. Reset isUsing về owner
        ResetIsUsingClientRpc();
    }

    /// <summary>Tô màu vàng kim loại khi khiên bật — visual feedback tức thì, không cần art asset.</summary>
    [ClientRpc]
    private void TintPlayerClientRpc(bool active)
    {
        // Tìm SpriteRenderer chính của nhân vật (không phải SkillEffect)
        SpriteRenderer sr = GetComponent<SpriteRenderer>() ?? GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.color = active ? new Color(1f, 0.85f, 0.2f, 0.9f) : Color.white;
    }

    /// <summary>Xóa sprite SkillEffect sau khi animation kết thúc.</summary>
    [ClientRpc]
    private void ClearSkillEffectClientRpc()
    {
        Transform root = transform.root;
        GameObject skillEffect = root.Find("SkillEffect")?.gameObject
                              ?? transform.Find("SkillEffect")?.gameObject;
        if (skillEffect == null) return;
        SpriteRenderer sr = skillEffect.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = null;
    }

    [ClientRpc]
    private void ActivateShieldOnOwnerClientRpc()
    {
        if (!IsOwner) return;
        playerHealth?.ActivateShield();
        Debug.Log("[MetalShieldSkill] Khiên bật — bất tử!");
    }

    [ClientRpc]
    private void DeactivateShieldOnOwnerClientRpc()
    {
        if (!IsOwner) return;
        playerHealth?.DeactivateShield();
        Debug.Log("[MetalShieldSkill] Khiên tắt — cooldown bắt đầu.");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Helpers
    // ════════════════════════════════════════════════════════════════════════

    private void SetShieldActive(bool active)
    {
        if (shieldVisualObject != null)
            shieldVisualObject.SetActive(active);

        // Bật/tắt collider xóa projectile
        if (shieldCollider != null)
            shieldCollider.enabled = active;
    }
}
