using UnityEngine;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Skill 3 của hệ Thủy — "Thủy Giáp Hộ Thể" (Buff Giáp Cho Đồng Đội)
///
/// Cơ chế:
///   1. Trigger animation Skill3 trên SkillEffect cho tất cả client.
///   2. Server quét bán kính buffRadius xung quanh người dùng (kể cả bản thân).
///   3. Mỗi PlayerHealth tìm thấy nhận armorValue điểm giáp tạm thời trong buffDuration giây.
///      Giáp hấp thụ sát thương trước khi trừ HP (xem PlayerHealth.ApplyArmorBuff).
///   4. Visual: tô màu xanh nước (cyan) cho TẤT CẢ người được buff (qua ClientRpc).
///   5. Sau buffDuration, tô màu trắng trở lại.
///
/// Setup trong Unity:
///   - Gắn component này vào cùng GameObject với PlayerSkillManager (Thuy.prefab).
///   - Không cần gán gì thêm — tự detect PlayerHealth trong bán kính.
///   - Điều chỉnh buffRadius, buffDuration, armorValue trong Inspector.
/// </summary>
public class WaterArmorBuffSkill : NetworkBehaviour
{
    private const float SkillEffectClearDelay = 1.05f;
    private static readonly Color BuffTintColor  = new Color(0.2f, 0.8f, 1f, 0.9f);  // cyan-blue

    [Header("Buff Settings")]
    [Tooltip("Cooldown giữa các lần dùng skill (giây)")]
    [SerializeField] public float cooldown = 12f;

    [Tooltip("Bán kính phát hiện đồng đội xung quanh (units)")]
    [SerializeField] private float buffRadius = 4f;

    [Tooltip("Thời gian buff giáp duy trì (giây)")]
    [SerializeField] private float buffDuration = 5f;

    [Tooltip("Lượng giáp tạm thời thêm vào (hấp thụ sát thương trước khi trừ HP)")]
    [SerializeField] private int armorValue = 20;

    [Header("Visual")]
    [Tooltip("Trigger name trong Animator SkillEffect để phát animation Skill3")]
    [SerializeField] private string animTriggerName = "Skill3";

    // ── Internal state ──────────────────────────────────────────────────────
    private float cooldownTimer;
    private bool canUse = true;
    private bool isUsing;
    private PlayerAnimator playerAnimator;
    private Coroutine clearSkillEffectCoroutine;

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

    // ════════════════════════════════════════════════════════════════════════
    //  Public API — gọi từ PlayerSkillManager
    // ════════════════════════════════════════════════════════════════════════

    public void UseWaterArmorBuff()
    {
        if (!CanUseNow) return;

        canUse = false;
        isUsing = true;
        cooldownTimer = cooldown;

        bool facingRight = transform.localScale.x >= 0f;

        if (IsServer)
            StartCoroutine(WaterArmorBuffSequence(facingRight));
        else
        {
            // Pre-trigger locally để tránh delay round-trip ServerRpc
            if (playerAnimator == null)
                playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
            playerAnimator?.TriggerAttack();
            StartWaterArmorBuffServerRpc(facingRight);
        }
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Network RPCs
    // ════════════════════════════════════════════════════════════════════════

    [ServerRpc]
    private void StartWaterArmorBuffServerRpc(bool facingRight)
    {
        StartCoroutine(WaterArmorBuffSequence(facingRight));
    }

    [ClientRpc]
    private void TriggerBuffAnimationClientRpc(bool facingRight)
    {
        // Trigger animation nhân vật (owner đã trigger locally rồi — chỉ trigger cho các client khác)
        if (IsServer || !IsOwner)
        {
            if (playerAnimator == null)
                playerAnimator = GetComponent<PlayerAnimator>() ?? GetComponentInParent<PlayerAnimator>();
            playerAnimator?.TriggerAttack();
        }

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
        Debug.LogWarning($"[WaterArmorBuffSkill] Animator không có trigger '{animTriggerName}'.");
    }

    /// <summary>Tô màu xanh / khôi phục màu trắng cho tất cả người chơi được buff.</summary>
    [ClientRpc]
    private void UpdateBuffedPlayersVisualClientRpc(ulong[] networkObjectIds, bool active)
    {
        if (NetworkManager.Singleton == null) return;

        foreach (ulong id in networkObjectIds)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id, out var netObj))
                continue;

            // Tìm SpriteRenderer chính của nhân vật (bỏ qua SkillEffect)
            SpriteRenderer sr = netObj.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                foreach (var candidate in netObj.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (candidate.gameObject.name != "SkillEffect")
                    {
                        sr = candidate;
                        break;
                    }
                }
            }

            if (sr != null)
                sr.color = active ? BuffTintColor : Color.white;
        }
    }

    [ClientRpc]
    private void ResetIsUsingClientRpc()
    {
        isUsing = false;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Core sequence (server-only)
    // ════════════════════════════════════════════════════════════════════════

    private IEnumerator WaterArmorBuffSequence(bool facingRight)
    {
        // 1. Phát animation cho tất cả client
        TriggerBuffAnimationClientRpc(facingRight);

        // 2. Server: tìm tất cả PlayerHealth trong cùng physics scene của map hiện tại
        int playerLayer = 1 << 8;
        Collider2D[] hits = MapPhysicsQuery2D.OverlapCircleAll(gameObject, transform.position, buffRadius, playerLayer);

        Debug.Log($"[WaterArmorBuffSkill] Overlap buffRadius={buffRadius} hits={hits.Length} at pos={transform.position}");

        var buffedIds = new HashSet<ulong>();

        // Luôn bao gồm bản thân người dùng skill dù vật lý có tìm thấy hay không
        var selfNetObj = GetComponent<NetworkObject>();
        if (selfNetObj != null)
            buffedIds.Add(selfNetObj.NetworkObjectId);

        foreach (var col in hits)
        {
            // Thu thập NetworkObject ID trước — độc lập với PlayerHealth
            NetworkObject netObj = col.GetComponent<NetworkObject>()
                                ?? col.GetComponentInParent<NetworkObject>();
            if (netObj != null)
                buffedIds.Add(netObj.NetworkObjectId);

            // Áp dụng buff giáp nếu có PlayerHealth
            PlayerHealth ph = col.GetComponent<PlayerHealth>()
                           ?? col.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.ApplyArmorBuff(armorValue, buffDuration);
                Debug.Log($"[WaterArmorBuffSkill] Đã buff giáp cho: {col.name}");
            }
        }

        // 3. Bật aura xanh cho tất cả client (luôn gọi vì buffedIds luôn có ít nhất bản thân)
        UpdateBuffedPlayersVisualClientRpc(new List<ulong>(buffedIds).ToArray(), true);

        // 4. Chờ hết thời gian buff
        yield return new WaitForSeconds(buffDuration);

        // 5. Tắt aura buff
        UpdateBuffedPlayersVisualClientRpc(new List<ulong>(buffedIds).ToArray(), false);

        // 6. Reset isUsing
        ResetIsUsingClientRpc();
    }

    private IEnumerator ClearSkillEffectAfterDelay()
    {
        yield return new WaitForSeconds(SkillEffectClearDelay);

        Transform root = transform.root;
        GameObject skillEffect = root.Find("SkillEffect")?.gameObject
                              ?? transform.Find("SkillEffect")?.gameObject;
        // Dùng SetActive(false) thay vì sprite=null để dừng Animator;
        // nếu dùng sprite=null thì Animator sẽ override lại ngay frame sau.
        if (skillEffect != null)
            skillEffect.SetActive(false);

        clearSkillEffectCoroutine = null;
    }
}
