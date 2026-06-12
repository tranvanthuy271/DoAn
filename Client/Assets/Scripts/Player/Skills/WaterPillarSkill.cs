using UnityEngine;
using Unity.Netcode;
using System.Collections;

// Skill 2 của hệ Thủy — "Thánh Mộc Hạ" (Cây Thánh Rơi Xuống)
// Cơ chế:
// 1. Trigger animation Skill2 trên SkillEffect cho tất cả client.
// 2. Server spawn pillarPrefab ở vị trí phía trước và phía trên player.
// 3. Pillar rơi thẳng xuống với tốc độ pillarFallSpeed.
// 4. Gây sát thương khi chạm enemy (qua FireballDamage component trên prefab).
// 5. Tự hủy sau pillarLifetime giây.
// Setup trong Unity:
// - Gắn component này vào GameObject chứa PlayerSkillManager (cùng Thuy.prefab).
// - Gán pillarPrefab: prefab projectile có Rigidbody2D + Collider2D trigger + FireballDamage.
// - PlayerSkillManager tự phát hiện qua GetComponent khi skillType = WaterPillar.
public class WaterPillarSkill : NetworkBehaviour
{
    [Header("Pillar Settings")]
    [Tooltip("Cooldown giữa các lần dùng skill (giây)")]
    [SerializeField] public float cooldown = 6f;

    [Tooltip("Chiều cao spawn pillar tính từ vị trí player (units). Pillar sẽ rơi từ đây xuống.")]
    [SerializeField] private float spawnHeightOffset = 5f;

    [Tooltip("Khoảng cách ngang tính từ player đến vị trí rơi (units). 0 = ngay dưới chân player.")]
    [SerializeField] private float horizontalOffset = 2f;

    [Tooltip("Tốc độ rơi của pillar (units/giây)")]
    [SerializeField] private float pillarFallSpeed = 14f;

    [Tooltip("Thời gian sống của pillar kể từ khi spawn (giây). Hủy sau thời gian này.")]
    [SerializeField] private float pillarLifetime = 2.5f;

    [Header("Visual")]
    [Tooltip("Prefab của pillar projectile. Cần có: NetworkObject, Rigidbody2D, Collider2D (trigger), FireballDamage.")]
    [SerializeField] private GameObject pillarPrefab;

    [Tooltip("Trigger name trong Animator SkillEffect để phát animation Skill2")]
    [SerializeField] private string animTriggerName = "Skill2";

    // Internal state
    private float cooldownTimer;
    private bool canUse = true;
    private bool isUsing;
    private PlayerAnimator playerAnimator;
    private Coroutine clearSkillEffectCoroutine;

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

    public void UseWaterPillar()
    {
        if (!CanUseNow) return;

        canUse = false;
        isUsing = true;
        cooldownTimer = cooldown;

        bool facingRight = transform.localScale.x >= 0f;

        if (IsServer)
            StartCoroutine(WaterPillarSequence(facingRight));
        else
        {
            // Pre-trigger locally để tránh delay round-trip ServerRpc
            if (playerAnimator == null)
                playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
            playerAnimator?.TriggerAttack();

            // Trigger SkillEffect animation locally
            TriggerWaterPillarSkillEffectLocally();

            // ĐÃ TẮT prediction visual: tránh "thấy 2 cây trụ" trên VPS (predicted + networked).
            // Chỉ dùng bản networked do server spawn.

            StartWaterPillarServerRpc(facingRight);
        }
    }

    private void TriggerWaterPillarSkillEffectLocally()
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
                if (clearSkillEffectCoroutine != null)
                    StopCoroutine(clearSkillEffectCoroutine);
                clearSkillEffectCoroutine = StartCoroutine(ClearSkillEffectAfterDelay());
                return;
            }
        }
    }

    //  Network RPCs

    [ServerRpc]
    private void StartWaterPillarServerRpc(bool facingRight)
    {
        StartCoroutine(WaterPillarSequence(facingRight));
    }

    [ClientRpc]
    private void TriggerPillarAnimationClientRpc(bool facingRight)
    {
        // Trigger animation nhân vật (owner đã trigger locally rồi — chỉ trigger cho các client khác)
        if (IsServer || !IsOwner)
        {
            if (playerAnimator == null)
                playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
            playerAnimator?.TriggerAttack();
        }

        // Owner đã trigger SkillEffect locally rồi — tránh double trigger
        if (!IsServer && IsOwner) return;

        // Trigger SkillEffect animation — tìm SkillEffect từ root
        if (string.IsNullOrEmpty(animTriggerName)) return;

        Transform root = transform.root;
        GameObject skillEffect = root.Find("SkillEffect")?.gameObject
                              ?? transform.Find("SkillEffect")?.gameObject;
        if (skillEffect == null) return;

        if (!skillEffect.activeSelf)
            skillEffect.SetActive(true);

        SpriteRenderer sr = skillEffect.GetComponent<SpriteRenderer>();
        // flipX=true: sprite gốc nhìn TRÁI, parent localScale.x=±1 điều khiển hướng thế giới.
        // Không dùng facingRight vì parent scale đã xử lý flip → tránh double-flip "tấm bìa".
        if (sr != null) sr.flipX = true;

        Animator anim = skillEffect.GetComponent<Animator>();
        if (anim == null || anim.runtimeAnimatorController == null) return;

        foreach (var p in anim.parameters)
        {
            if (p.name == animTriggerName && p.type == AnimatorControllerParameterType.Trigger)
            {
                anim.SetTrigger(animTriggerName);
                // Xóa SkillEffect sau khi animation kết thúc
                if (clearSkillEffectCoroutine != null)
                    StopCoroutine(clearSkillEffectCoroutine);
                clearSkillEffectCoroutine = StartCoroutine(ClearSkillEffectAfterDelay());
                return;
            }
        }
        { /* Cảnh báo: Animator không có trigger '{animTriggerName}' */ }
    }

    private IEnumerator ClearSkillEffectAfterDelay()
    {
        // Chờ bằng thời gian animation Skill2
        yield return new WaitForSeconds(1.1f);

        Transform root = transform.root;
        GameObject skillEffect = root.Find("SkillEffect")?.gameObject
                              ?? transform.Find("SkillEffect")?.gameObject;
        if (skillEffect != null)
            skillEffect.SetActive(false);

        clearSkillEffectCoroutine = null;
    }

    [ClientRpc]
    private void ResetIsUsingClientRpc()
    {
        isUsing = false;
    }

    // Đồng bộ velocity của pillar sang tất cả client.
    // Server đã set velocity trực tiếp — ClientRpc chỉ chạy trên các client còn lại.
    [ClientRpc]
    private void SetPillarVelocityClientRpc(ulong pillarNetId, Vector2 velocity)
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.IsServer) return;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(pillarNetId, out var netObj)) return;
        var rb = netObj.GetComponent<Rigidbody2D>();
        if (rb != null) rb.velocity = velocity;
    }

    // Trigger animation trên pillar projectile đã spawn — dùng coroutine để chờ SpawnedObjects
    // đăng ký đủ trước khi truy cập (tránh race condition giữa spawn-message và RPC).
    [ClientRpc]
    private void TriggerPillarProjectileAnimationClientRpc(ulong pillarNetworkObjectId)
    {
        if (IsServer) return; // server đã xử lý trực tiếp
        StartCoroutine(WaitAndApplyPillarAnimation(pillarNetworkObjectId));
    }

    private System.Collections.IEnumerator WaitAndApplyPillarAnimation(ulong pillarNetId)
    {
        float elapsed = 0f;
        const float timeout = 2f;
        while (elapsed < timeout)
        {
            if (NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                    .TryGetValue(pillarNetId, out var netObj) == true)
            {
                Animator anim = netObj.GetComponent<Animator>();
                if (anim != null && anim.runtimeAnimatorController != null)
                {
                    // Rebind restores default state; Update(0f) forces first evaluation
                    anim.Rebind();
                    anim.Update(0f);
                    if (!string.IsNullOrEmpty(animTriggerName))
                    {
                        foreach (var p in anim.parameters)
                        {
                            if (p.name == animTriggerName &&
                                p.type == AnimatorControllerParameterType.Trigger)
                            {
                                anim.SetTrigger(animTriggerName);
                                break;
                            }
                        }
                    }
                }
                yield break;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        { /* Cảnh báo: Timeout chờ pillar {pillarNetId} trong SpawnedObjects */ }
    }

    //  Core sequence (server-only)

    private IEnumerator WaterPillarSequence(bool facingRight)
    {
        // 1. Phát animation cho tất cả client
        TriggerPillarAnimationClientRpc(facingRight);

        // 2. Spawn pillar rơi từ trên (server)
        if (pillarPrefab != null)
        {
            float dir = facingRight ? 1f : -1f;
            Vector3 spawnPos = transform.position + new Vector3(dir * horizontalOffset, spawnHeightOffset, 0f);

            GameObject pillar = Instantiate(pillarPrefab, spawnPos, Quaternion.identity);

            // Dùng localScale.x để flip (NetworkTransform có SyncScaleX nên client nhận đúng)
            Vector3 pillarScale = pillar.transform.localScale;
            pillar.transform.localScale = new Vector3(
                facingRight ? Mathf.Abs(pillarScale.x) : -Mathf.Abs(pillarScale.x),
                pillarScale.y, pillarScale.z);

            // Thiết lập Rigidbody2D để rơi xuống
            Rigidbody2D rb = pillar.GetComponent<Rigidbody2D>();
            if (rb == null) rb = pillar.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.velocity = new Vector2(0f, -pillarFallSpeed);

            // Đảm bảo có NetworkObject
            NetworkObject netObj = pillar.GetComponent<NetworkObject>();
            if (netObj == null) netObj = pillar.AddComponent<NetworkObject>();

            // Di chuyển vào physics scene của map player — TRƯỚC Spawn()
            var _pillarRoom = ZoneRoomRegistry.Instance?.GetClientRoom(OwnerClientId);
            MapSceneManager.Instance?.MoveToMapScene(pillar, _pillarRoom?.MapId ?? -999);

            netObj.Spawn();

            // Chờ 1 frame để client nhận spawn message, rồi gửi velocity + animation RPC
            yield return null;
            Vector2 fallVelocity = new Vector2(0f, -pillarFallSpeed);
            SetPillarVelocityClientRpc(netObj.NetworkObjectId, fallVelocity);
            TriggerPillarProjectileAnimationClientRpc(netObj.NetworkObjectId);

            // Đợi hết lifetime rồi Despawn đúng cách (không dùng Destroy trên NetworkObject)
            if (pillarLifetime > 0f)
            {
                yield return new WaitForSeconds(pillarLifetime);
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
            }
        }
        else
        {
            { /* Cảnh báo: pillarPrefab chưa được gán! Hãy gán trong Unity Inspector */ }
        }

        // 3. Chờ ngắn rồi reset isUsing
        yield return new WaitForSeconds(0.2f);
        ResetIsUsingClientRpc();
    }
}
