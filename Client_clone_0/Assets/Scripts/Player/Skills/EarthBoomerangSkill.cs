using UnityEngine;
using Unity.Netcode;
using System.Collections;

// Skill 2 của hệ Thổ — "Địa Phong Đao" (Boomerang)
// Cơ chế:
// 1. Trigger animation Skill2 trên SkillEffect.
// 2. Server spawn boomerang prefab bay về phía trước player.
// 3. EarthBoomerangProjectile component điều khiển quay về.
// 4. Damage xử lý qua FireballDamage component trên prefab.
// Setup trong Unity:
// - Gắn component này vào Tho.prefab.
// - boomerangPrefab cần có: NetworkObject, Rigidbody2D, Collider2D trigger,
// FireballDamage, EarthBoomerangProjectile.
public class EarthBoomerangSkill : NetworkBehaviour
{
    [Header("Boomerang Settings")]
    [Tooltip("Cooldown giữa các lần dùng skill (giây)")]
    [SerializeField] public float cooldown = 5f;

    [Tooltip("Tốc độ bay ban đầu (units/giây)")]
    [SerializeField] private float launchSpeed = 14f;

    [Header("Visual")]
    [SerializeField] private GameObject boomerangPrefab;
    [SerializeField] private string animTriggerName = "Skill2";

    // Internal state
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

    public void UseEarthBoomerang(float effectValue = 0f)
    {
        if (!CanUseNow) return;
        canUse = false;
        isUsing = true;
        cooldownTimer = cooldown;

        bool facingRight = transform.localScale.x >= 0f;

        if (IsServer)
            StartCoroutine(BoomerangSequence(facingRight, effectValue));
        else
            StartBoomerangServerRpc(facingRight, effectValue);
    }

    [ServerRpc]
    private void StartBoomerangServerRpc(bool facingRight, float effectValue) => StartCoroutine(BoomerangSequence(facingRight, effectValue));

    [ClientRpc]
    private void TriggerBoomerangAnimationClientRpc()
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

    private IEnumerator BoomerangSequence(bool facingRight, float effectValue)
    {
        TriggerBoomerangAnimationClientRpc();

        if (boomerangPrefab != null)
        {
            float dir = facingRight ? 1f : -1f;
            Vector3 spawnPos = transform.position + new Vector3(dir * 0.6f, 0f, 0f);
            Vector2 velocity = new Vector2(dir * launchSpeed, 0f);

            GameObject boomerang = Instantiate(boomerangPrefab, spawnPos, Quaternion.identity);

            // Flip sprite TRƯỚC khi Spawn() — NetworkTransform (SyncScaleX) sử đồng bộ sang client
            Vector3 bScale = boomerang.transform.localScale;
            boomerang.transform.localScale = new Vector3(
                facingRight ? Mathf.Abs(bScale.x) : -Mathf.Abs(bScale.x),
                bScale.y, bScale.z);

            // Server-side init TRƯỚC khi Spawn() để server physics bắt đầu đúng
            EarthBoomerangProjectile proj = boomerang.GetComponent<EarthBoomerangProjectile>();
            if (proj != null)
            {
                proj.InitializeOnServer(transform, velocity);
                proj.ownerNetworkObjectId = NetworkObjectId;
                if (effectValue > 0f) proj.damage = (int)effectValue;
            }
            else
            {
                Rigidbody2D rb = boomerang.GetComponent<Rigidbody2D>();
                if (rb != null) { rb.gravityScale = 0f; rb.velocity = velocity; }
            }

            NetworkObject netObj = boomerang.GetComponent<NetworkObject>();
            if (netObj == null) netObj = boomerang.AddComponent<NetworkObject>();

            // Di chuyển vào physics scene của map player — TRƯỚC Spawn()
            var _boomRoom = ZoneRoomRegistry.Instance?.GetClientRoom(OwnerClientId);
            MapSceneManager.Instance?.MoveToMapScene(boomerang, _boomRoom?.MapId ?? -999);

            netObj.Spawn();

            // NetworkTransform đồng bộ vị trí sang client,
            // Rigidbody2D kinematic trên client (EarthBoomerangProjectile.OnNetworkSpawn).
        }
        else
        {
            Debug.LogWarning("[EarthBoomerangSkill] boomerangPrefab chưa được gán!");
        }

        yield return new WaitForSeconds(0.2f);
        ResetIsUsingClientRpc();
    }
}
