using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Skill 1 của hệ Thổ — "Địa Uy Khí" (Aura Buff Tấn Công)
///
/// Cơ chế:
///   1. Trigger animation Skill1 trên SkillEffect.
///   2. Quét bán kính buffRadius tìm tất cả player (kể cả bản thân).
///   3. Gọi ApplyAttackBuff() trên PlayerHealth của mỗi player tìm thấy.
///
/// Setup trong Unity:
///   - Gắn component này vào Tho.prefab.
///   - PlayerSkillManager tự phát hiện qua GetComponent khi skillType = EarthAura.
/// </summary>
public class EarthAttackBuffSkill : NetworkBehaviour
{
    [Header("Attack Buff Settings")]
    [Tooltip("Cooldown giữa các lần dùng skill (giây)")]
    [SerializeField] public float cooldown = 10f;

    [Tooltip("Bán kính buff (units)")]
    [SerializeField] private float buffRadius = 5f;

    [Tooltip("Thời gian buff kéo dài (giây)")]
    [SerializeField] private float buffDuration = 6f;

    [Tooltip("% tăng sát thương tấn công")]
    [SerializeField] private int attackBonusPercent = 30;

    [Header("Visual")]
    [SerializeField] private string animTriggerName = "Skill1";

    // ── Internal state ────────────────────────────────────────────────────────
    private float cooldownTimer;
    private bool canUse = true;
    private bool isUsing;
    private PlayerAnimator playerAnimator;

    public bool CanUseNow => canUse && !isUsing;
    public float GetCooldownPercent() => canUse ? 1f : Mathf.Clamp01(1f - cooldownTimer / cooldown);
    public float GetCooldownRemaining() => canUse ? 0f : Mathf.Max(0f, cooldownTimer);

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
        playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (!canUse)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f) { cooldownTimer = 0f; canUse = true; }
        }
    }

    public void UseEarthAura()
    {
        if (!CanUseNow) return;
        canUse = false;
        isUsing = true;
        cooldownTimer = cooldown;

        if (IsServer)
            StartCoroutine(EarthAuraSequence());
        else
            StartEarthAuraServerRpc();
    }

    [ServerRpc]
    private void StartEarthAuraServerRpc() => StartCoroutine(EarthAuraSequence());

    [ClientRpc]
    private void TriggerAuraAnimationClientRpc()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
        playerAnimator?.TriggerAttack();

        if (string.IsNullOrEmpty(animTriggerName)) return;
        Transform root = transform.root;
        GameObject skillEffect = root.Find("SkillEffect")?.gameObject
                              ?? transform.Find("SkillEffect")?.gameObject;
        if (skillEffect == null) return;
        if (!skillEffect.activeSelf) skillEffect.SetActive(true);
        // sprite gốc nhìn TRÁI, parent localScale.x=±1 điều khiển hướng thế giới
        SpriteRenderer sr = skillEffect.GetComponent<SpriteRenderer>();
        if (sr != null) sr.flipX = true;
        Animator anim = skillEffect.GetComponent<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null) return;
        foreach (var p in anim.parameters)
        {
            if (p.name == animTriggerName && p.type == AnimatorControllerParameterType.Trigger)
            {
                anim.SetTrigger(animTriggerName);
                return;
            }
        }
    }

    [ClientRpc]
    private void ResetIsUsingClientRpc() => isUsing = false;

    private IEnumerator EarthAuraSequence()
    {
        TriggerAuraAnimationClientRpc();

        // Tìm tất cả player trong bán kính và áp dụng buff
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, buffRadius);
        foreach (var hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;
            PlayerHealth ph = hit.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.ApplyAttackBuff(attackBonusPercent, buffDuration);
        }

        Debug.Log($"[EarthAttackBuffSkill] Áp dụng buff +{attackBonusPercent}% tấn công trong {buffRadius} units.");

        yield return new WaitForSeconds(0.2f);
        ResetIsUsingClientRpc();
    }
}
