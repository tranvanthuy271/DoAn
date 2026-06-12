using UnityEngine;
using Unity.Netcode;
using System.Collections;

// Skill 3 của hệ Hỏa — "Thiên Hỏa" (Mưa Lửa Từ Trên Trời)
// Cơ chế:
// 1. Trigger animation Skill3 trên SkillEffect cho tất cả client.
// 2. Server spawn nhiều fireball prefab từ trên cao rơi xuống vùng phía trước player.
// 3. Mỗi fireball rơi thẳng xuống, gây sát thương khi chạm enemy.
// 4. Tự hủy sau lifetime.
// Setup trong Unity:
// - Gắn component này vào Hoa.prefab.
// - Gán firePrefab: prefab có Rigidbody2D + Collider2D trigger + FireballDamage.
// - PlayerSkillManager tự phát hiện qua GetComponent khi skillType = FireRain.
public class FireRainSkill : NetworkBehaviour
{
    [Header("Fire Rain Settings")]
    [Tooltip("Cooldown giữa các lần dùng skill (giây)")]
    [SerializeField] public float cooldown = 8f;

    [Tooltip("Số lượng cầu lửa rơi xuống")]
    [SerializeField] private int fireballCount = 5;

    [Tooltip("Chiều cao spawn các cầu lửa tính từ vị trí player (units)")]
    [SerializeField] private float spawnHeightOffset = 6f;

    [Tooltip("Khoảng cách ngang tối đa từ player đến vị trí rơi (units)")]
    [SerializeField] private float spreadRadius = 3f;

    [Tooltip("Tốc độ rơi của cầu lửa (units/giây)")]
    [SerializeField] private float fallSpeed = 16f;

    [Tooltip("Thời gian sống của mỗi cầu lửa (giây)")]
    [SerializeField] private float fireballLifetime = 2.5f;

    [Tooltip("Độ trễ giữa các cầu lửa (giây)")]
    [SerializeField] private float spawnInterval = 0.12f;

    [Header("Visual")]
    [Tooltip("Prefab của cầu lửa. Cần có: NetworkObject, Rigidbody2D, Collider2D (trigger), FireballDamage.")]
    [SerializeField] private GameObject firePrefab;

    [Tooltip("Trigger name trong Animator SkillEffect để phát animation Skill3")]
    [SerializeField] private string animTriggerName = "Skill3";

    // Internal state
    private float cooldownTimer;
    private bool canUse = true;
    private bool isUsing;
    private PlayerAnimator playerAnimator;

    public bool CanUseNow => canUse && !isUsing;
    public float GetCooldownPercent() => canUse ? 1f : Mathf.Clamp01(1f - cooldownTimer / cooldown);
    public float GetCooldownRemaining() => canUse ? 0f : Mathf.Max(0f, cooldownTimer);

    //  Unity lifecycle
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
            if (cooldownTimer <= 0f)
            {
                cooldownTimer = 0f;
                canUse = true;
            }
        }
    }

    //  Public API — gọi từ PlayerSkillManager
    public void UseFireRain()
    {
        if (!CanUseNow) return;

        canUse = false;
        isUsing = true;
        cooldownTimer = cooldown;

        bool facingRight = transform.localScale.x >= 0f;

        if (IsServer)
            StartCoroutine(FireRainSequence(facingRight));
        else
        {
            if (playerAnimator == null)
                playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
            playerAnimator?.TriggerAttack();

            // Trigger SkillEffect locally
            TriggerFireRainSkillEffectLocally();

            // ĐÃ TẮT prediction visual: bản predicted + bản networked của server làm
            // owner thấy "mưa lửa 2 lần" trên VPS. Chỉ dùng fireball networked do server spawn.

            StartFireRainServerRpc(facingRight);
        }
    }

    private void TriggerFireRainSkillEffectLocally()
    {
        if (string.IsNullOrEmpty(animTriggerName)) return;

        Transform root = transform.root;
        GameObject skillEffect = root.Find("SkillEffect")?.gameObject
                              ?? transform.Find("SkillEffect")?.gameObject;
        if (skillEffect == null) return;

        if (!skillEffect.activeSelf)
            skillEffect.SetActive(true);

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

    //  Network RPCs
    [ServerRpc]
    private void StartFireRainServerRpc(bool facingRight)
    {
        StartCoroutine(FireRainSequence(facingRight));
    }

    [ClientRpc]
    private void TriggerFireRainAnimationClientRpc(bool facingRight)
    {
        if (IsServer || !IsOwner)
        {
            if (playerAnimator == null)
                playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
            playerAnimator?.TriggerAttack();
        }

        // Owner đã trigger SkillEffect locally rồi — tránh double trigger
        if (!IsServer && IsOwner) return;

        if (string.IsNullOrEmpty(animTriggerName)) return;

        Transform root = transform.root;
        GameObject skillEffect = root.Find("SkillEffect")?.gameObject
                              ?? transform.Find("SkillEffect")?.gameObject;
        if (skillEffect == null) return;

        if (!skillEffect.activeSelf)
            skillEffect.SetActive(true);

        SpriteRenderer sr = skillEffect.GetComponent<SpriteRenderer>();
        // flipX=true: sprite gốc nhìn TRÁI, parent localScale.x=±1 điều khiển hướng thế giới.
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
        { /* Cảnh báo: Animator không có trigger '{animTriggerName}' */ }
    }

    [ClientRpc]
    private void ResetIsUsingClientRpc()
    {
        isUsing = false;
    }

    //  Core sequence (server-only)
    private IEnumerator FireRainSequence(bool facingRight)
    {
        TriggerFireRainAnimationClientRpc(facingRight);

        if (firePrefab != null)
        {
            float dir = facingRight ? 1f : -1f;

            for (int i = 0; i < fireballCount; i++)
            {
                float xOffset = dir * Random.Range(0.3f, spreadRadius);
                Vector3 spawnPos = transform.position + new Vector3(xOffset, spawnHeightOffset, 0f);

                GameObject fireball = Instantiate(firePrefab, spawnPos, Quaternion.identity);
                Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
                if (rb == null) rb = fireball.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.velocity = new Vector2(0f, -fallSpeed);

                NetworkObject netObj = fireball.GetComponent<NetworkObject>();
                if (netObj == null) netObj = fireball.AddComponent<NetworkObject>();

                // Di chuyển vào physics scene của map player — TRƯỚC Spawn()
                var _fireRoom = ZoneRoomRegistry.Instance?.GetClientRoom(OwnerClientId);
                MapSceneManager.Instance?.MoveToMapScene(fireball, _fireRoom?.MapId ?? -999);
                netObj.Spawn();

                if (fireballLifetime > 0f)
                    Destroy(fireball, fireballLifetime);

                yield return new WaitForSeconds(spawnInterval);
            }
        }
        else
        {
            { /* Cảnh báo: firePrefab chưa được gán! Hãy gán trong Unity Inspector */ }
        }

        yield return new WaitForSeconds(0.2f);
        ResetIsUsingClientRpc();
    }
}
