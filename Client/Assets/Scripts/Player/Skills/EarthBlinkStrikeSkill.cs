using UnityEngine;
using Unity.Netcode;
using System.Collections;

/// <summary>
/// Skill 3 của hệ Thổ — "Địa Độn Thuật" (Dịch Chuyển + DoT Projectile)
///
/// Cơ chế:
///   1. Trigger animation Skill3 trên SkillEffect.
///   2. Dịch chuyển player về phía trước một khoảng blinkDistance.
///   3. Ngay sau đó spawn dotProjectilePrefab tại vị trí cũ, bay về phía trước.
///   4. DotDamage component trên prefab xử lý sát thương theo thời gian khi chạm.
///
/// Setup trong Unity:
///   - Gắn component này vào Tho.prefab.
///   - dotProjectilePrefab cần có: Rigidbody2D, Collider2D trigger, DotDamage.
/// </summary>
public class EarthBlinkStrikeSkill : NetworkBehaviour
{
    [Header("Blink Settings")]
    [Tooltip("Cooldown giữa các lần dùng skill (giây)")]
    [SerializeField] public float cooldown = 7f;

    [Tooltip("Khoảng cách dịch chuyển (units)")]
    [SerializeField] private float blinkDistance = 4f;

    [Header("DoT Projectile")]
    [SerializeField] private GameObject dotProjectilePrefab;
    [Tooltip("Tốc độ bay của DoT projectile (units/giây)")]
    [SerializeField] private float projectileSpeed = 10f;
    [Tooltip("Thời gian sống của DoT projectile nếu không chạm (giây)")]
    [SerializeField] private float projectileLifetime = 3f;

    [Header("Visual")]
    [SerializeField] private string animTriggerName = "Skill3";

    // ── Internal state ────────────────────────────────────────────────────────
    private float cooldownTimer;
    private bool canUse = true;
    private bool isUsing;
    private PlayerAnimator playerAnimator;

    public bool CanUseNow => canUse && !isUsing;
    public float GetCooldownPercent() => canUse ? 1f : Mathf.Clamp01(1f - cooldownTimer / cooldown);
    public float GetCooldownRemaining() => canUse ? 0f : Mathf.Max(0f, cooldownTimer);

    public override void OnNetworkSpawn() { base.OnNetworkSpawn(); Initialize(); }
    private void Start() { if (!IsSpawned) Initialize(); }
    private void Initialize() { playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>(); }

    private void Update()
    {
        if (!IsOwner) return;
        if (!canUse)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0f) { cooldownTimer = 0f; canUse = true; }
        }
    }

    public void UseEarthBlinkStrike()
    {
        if (!CanUseNow) return;
        canUse = false;
        isUsing = true;
        cooldownTimer = cooldown;

        bool facingRight = transform.localScale.x >= 0f;
        Vector3 origin = transform.position;

        if (IsServer)
            StartCoroutine(BlinkStrikeSequence(origin, facingRight));
        else
            StartBlinkStrikeServerRpc(origin, facingRight);
    }

    [ServerRpc]
    private void StartBlinkStrikeServerRpc(Vector3 origin, bool facingRight)
        => StartCoroutine(BlinkStrikeSequence(origin, facingRight));

    [ClientRpc]
    private void TriggerBlinkAnimationClientRpc()
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
    private void TeleportPlayerClientRpc(Vector3 targetPos)
    {
        // Di chuyển player trên tất cả client (visual sync)
        transform.position = targetPos;
    }

    [ClientRpc]
    private void ResetIsUsingClientRpc() => isUsing = false;

    private IEnumerator BlinkStrikeSequence(Vector3 origin, bool facingRight)
    {
        TriggerBlinkAnimationClientRpc();

        // Dịch chuyển
        float dir = facingRight ? 1f : -1f;
        Vector3 target = origin + new Vector3(dir * blinkDistance, 0f, 0f);
        TeleportPlayerClientRpc(target);

        // Spawn DoT projectile tại vị trí cũ, bay về phía trước
        if (dotProjectilePrefab != null)
        {
            Vector3 spawnPos = origin + new Vector3(dir * 0.3f, 0f, 0f);
            GameObject proj = Instantiate(dotProjectilePrefab, spawnPos, Quaternion.identity);

            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb == null) rb = proj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            Vector2 dotVelocity = new Vector2(dir * projectileSpeed, 0f);
            rb.velocity = dotVelocity;

            // Set owner để DotDamage không tự gây damage cho caster
            var dotDmg = proj.GetComponent<DotDamage>();
            if (dotDmg != null) dotDmg.SetOwner(NetworkObjectId);

            NetworkObject netObj = proj.GetComponent<NetworkObject>();
            if (netObj == null) netObj = proj.AddComponent<NetworkObject>();

            // Di chuyển vào physics scene của map player — TRƯỚC Spawn()
            var _blinkRoom = ZoneRoomRegistry.Instance?.GetClientRoom(OwnerClientId);
            MapSceneManager.Instance?.MoveToMapScene(proj, _blinkRoom?.MapId ?? -999);

            netObj.Spawn();

            // NetworkTransform đồng bộ vị trí sang client,
            // Rigidbody2D kinematic trên client (DotDamage.OnNetworkSpawn).

            // Đợi hết lifetime rồi Despawn đúng cách
            if (projectileLifetime > 0f)
            {
                yield return new WaitForSeconds(projectileLifetime);
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
            }
        }
        else
        {
            Debug.LogWarning("[EarthBlinkStrikeSkill] dotProjectilePrefab chưa được gán!");
        }

        yield return new WaitForSeconds(0.2f);
        ResetIsUsingClientRpc();
    }
}
