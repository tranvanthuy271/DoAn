using UnityEngine;
using Unity.Netcode;
using System.Collections;

// Skill 3 của hệ Phong (Wind) — "Phong Thoái Bộ"
// Luồng hoạt động:
// 1. Ẩn SpriteRenderer player trên tất cả client (ClientRpc)
// 2. Trigger animation "Skill3" trên SkillEffect_Phong (server → NetworkAnimator tự sync)
// 3. Chờ animationDuration giây
// 4. Hiện lại SpriteRenderer player (ClientRpc)
// 5. Dịch chuyển player đến vị trí đích qua custom ClientRpc visual sync
// Cách gắn vào player:
// - Gắn component này vào cùng GameObject với PlayerSkillManager
// - Gán playerSpriteRenderer (SpriteRenderer của sprite nhân vật)
// - Gán skillEffectObject (GameObject SkillEffect_Phong)
// - PlayerSkillManager sẽ tự phát hiện và uỷ quyền khi skill type = WindStep
public class WindStepSkill : NetworkBehaviour
{
    [Header("Wind Step Settings")]
    [Tooltip("Cooldown giữa các lần dùng skill (giây). Gợi ý từ DB levels_json[currentLevel].cooldown_sec")]
    [SerializeField] public float cooldown = 6f;

    [Tooltip("Khoảng cách dịch chuyển tối đa (units). Nên khớp với effect_value ở level cao nhất player hiện tại")]
    [SerializeField] public float dashDistance = 3f;

    [Tooltip("Thời gian smooth-move đến vị trí đích sau khi animation xong (giây)")]
    [SerializeField] private float dashDuration = 0.2f;

    [Tooltip("Kiểm tra collision trước khi di chuyển để tránh xuyên tường")]
    [SerializeField] private bool checkCollision = true;

    [Tooltip("Layer mask dùng để raycast kiểm tra vật cản (Wall, Obstacle ...)")]
    [SerializeField] private LayerMask obstacleLayerMask = 1;

    [Header("Animation")]
    [Tooltip("Object SkillEffect_Phong (child của player). Để trống → tự tìm child tên 'SkillEffect'")]
    [SerializeField] private GameObject skillEffectObject;

    [Tooltip("Thời gian animation Skill3 chạy trước khi player hiện lại và dịch chuyển (giây). Chỉnh theo độ dài clip Skill3 trong Animator")]
    [SerializeField] private float animationDuration = 0.8f;

    [Header("References")]
    [Tooltip("SpriteRenderer của hình nhân vật. Sẽ bị ẩn trong lúc animation. Để trống → tự tìm GetComponentInChildren")]
    [SerializeField] private SpriteRenderer playerSpriteRenderer;

    // Internal state
    private Rigidbody2D rb2D;
    private float cooldownTimer;
    private bool canUse = true;
    private bool isUsing;
    private PlayerAnimator playerAnimator;
    private Coroutine dashVisualCoroutine;

    // PlayerSkillManager và SkillSlotUI dùng cái này để kiểm tra trước khi trigger.
    public bool CanUseNow => canUse && !isUsing;

    // Phần trăm cooldown sẵn sàng (0 = đang CD, 1 = ready). Dùng cho hotbar overlay.
    public float GetCooldownPercent() => canUse ? 1f : Mathf.Clamp01(1f - cooldownTimer / cooldown);

    // Giây CD còn lại. Dùng cho text hiển thị trên hotbar.
    public float GetCooldownRemaining() => canUse ? 0f : Mathf.Max(0f, cooldownTimer);

    //  Unity lifecycle

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Initialize();
    }

    private void Start()
    {
        if (!IsSpawned)
            Initialize();
    }

    private void Initialize()
    {
        rb2D = GetComponent<Rigidbody2D>();

        if (skillEffectObject == null)
            skillEffectObject = transform.Find("SkillEffect")?.gameObject;

        if (playerSpriteRenderer == null)
            playerSpriteRenderer = GetComponentInChildren<SpriteRenderer>();

        playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
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

    //  Public API — gọi từ PlayerSkillManager

    // Kích hoạt Wind Step từ bên ngoài (PlayerSkillManager).
    // Chỉ có Owner mới được gọi hàm này.
    public void UseWindStep()
    {
        if (!CanUseNow) return;

        // Tính vị trí đích ngay tại đây (trên owner) để có đúng hướng player
        bool facingRight = transform.localScale.x >= 0f;
        Vector3 dir = facingRight ? Vector3.right : Vector3.left;
        Vector3 from = transform.position;
        Vector3 tentativeTarget = from + dir * dashDistance;
        Vector3 to = checkCollision ? ComputeSafeTarget(from, tentativeTarget) : tentativeTarget;

        // Bắt đầu cooldown ngay lập tức trên owner
        canUse = false;
        isUsing = true;
        cooldownTimer = cooldown;

        // Gửi lên server để thực thi (server kiểm soát chuyển động + animation sync)
        if (IsServer)
            StartCoroutine(DoWindStepSequence(to));
        else
            StartWindStepServerRpc(to);
    }

    //  Network RPCs

    [ServerRpc]
    private void StartWindStepServerRpc(Vector3 targetPos)
    {
        StartCoroutine(DoWindStepSequence(targetPos));
    }

    [ClientRpc]
    private void SetPlayerSpriteVisibleClientRpc(bool visible)
    {
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.enabled = visible;
    }

    // Reset isUsing trên tất cả client sau khi DoWindStepSequence kết thúc trên server.
    [ClientRpc]
    private void ResetIsUsingClientRpc()
    {
        isUsing = false;
    }

    [ClientRpc]
    private void PlayDashMovementClientRpc(Vector3 targetPos, float duration)
    {
        if (IsServer)
            return;

        if (dashVisualCoroutine != null)
            StopCoroutine(dashVisualCoroutine);

        dashVisualCoroutine = StartCoroutine(PlayDashMovementVisual(targetPos, duration));
    }
    [ClientRpc]
    private void ClearSkillEffectSpriteClientRpc()
    {
        if (skillEffectObject == null)
            skillEffectObject = transform.Find("SkillEffect")?.gameObject;
        if (skillEffectObject == null) return;
        SpriteRenderer sr = skillEffectObject.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = null;
    }

    // Phát animation Skill3 trên SkillEffect cho tất cả client.
    [ClientRpc]
    private void TriggerSkill3AnimationClientRpc()
    {
        if (skillEffectObject == null)
            skillEffectObject = transform.Find("SkillEffect")?.gameObject;
        if (skillEffectObject == null) return;

        if (!skillEffectObject.activeSelf)
            skillEffectObject.SetActive(true);

        // flipX cho hướng nhân vật
        SpriteRenderer sr = skillEffectObject.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = true;

        var anim = skillEffectObject.GetComponent<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null) return;

        foreach (var p in anim.parameters)
        {
            if (p.name == "Skill3" && p.type == AnimatorControllerParameterType.Trigger)
            {
                anim.SetTrigger("Skill3");
                return;
            }
        }
        Debug.LogWarning("[WindStepSkill] Animator không có Trigger 'Skill3'.");
    }
    // Kích hoạt animation attack của nhân vật (phong.controller) trên TẤT CẢ client.
    [ClientRpc]
    private void TriggerPlayerAttackClientRpc()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
        playerAnimator?.TriggerAttack();
    }
    //  Core sequence (runs on server)

    private IEnumerator DoWindStepSequence(Vector3 targetPos)
    {
        // 0. Trigger animation attack của nhân vật (phong.controller) trên tất cả client
        TriggerPlayerAttackClientRpc();

        // 1. Ẩn sprite player trên tất cả client
        SetPlayerSpriteVisibleClientRpc(false);

        // 2. Trigger animation "Skill3" trên SkillEffect_Phong
        //    NetworkAnimator tự đồng bộ sang tất cả client
        TriggerSkill3Animation();

        // 3. Chờ animation chạy xong
        yield return new WaitForSeconds(animationDuration);

        // Xóa sprite SkillEffect trên tất cả client
        ClearSkillEffectSpriteClientRpc();

        // 4. Hiện lại sprite player
        SetPlayerSpriteVisibleClientRpc(true);

        // 5. Di chuyển player đến vị trí đích.
        //    Server vẫn cập nhật vị trí authoritative cho gameplay, còn client replica
        //    tự lerp qua ClientRpc để không phụ thuộc vào NetworkTransform.
        Vector3 startPos = transform.position;
        PlayDashMovementClientRpc(targetPos, dashDuration);

        float elapsed = 0f;
        while (elapsed < dashDuration && dashDuration > 0f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(elapsed / dashDuration));
            yield return null;
        }
        transform.position = targetPos;

        // 6. Kết thúc — reset isUsing trên TẤT CẢ client (quan trọng: owner client cần reset này)
        //    isUsing chỉ set true trên owner, nhưng DoWindStepSequence chạy trên server.
        //    Nếu không broadcast ClientRpc, owner sẽ có isUsing=true mãi mãi sau lần dùng đầu.
        ResetIsUsingClientRpc();
    }

    // Hàm hỗ trợ dùng nội bộ để tách nhỏ xử lý chính.

    private void TriggerSkill3Animation()
    {
        // Phát animation trên TẤT CẢ client qua ClientRpc
        TriggerSkill3AnimationClientRpc();
    }

    private Vector3 ComputeSafeTarget(Vector3 from, Vector3 to)
    {
        Vector3 dir = (to - from).normalized;
        float dist = Vector3.Distance(from, to);

        if (rb2D != null)
        {
            RaycastHit2D hit = Physics2D.Raycast(from, dir, dist, obstacleLayerMask);
            if (hit.collider != null)
                return from + dir * Mathf.Max(0f, hit.distance - 0.5f);
        }

        return to;
    }

    private IEnumerator PlayDashMovementVisual(Vector3 targetPos, float duration)
    {
        Vector3 startPos = transform.position;

        if (duration <= 0f)
        {
            transform.position = targetPos;
            dashVisualCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, targetPos, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        transform.position = targetPos;
        dashVisualCoroutine = null;
    }
}
